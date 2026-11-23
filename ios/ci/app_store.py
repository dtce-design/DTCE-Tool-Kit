"""Minimal App Store Connect preflight/processing check; never logs credentials."""
import base64
import json
import os
import re
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

BUNDLE_ID = 'com.dtce.accessadmin'
API = 'https://api.appstoreconnect.apple.com/v1/'


def required(name):
    value = os.environ.get(name, '').strip()
    if not value:
        raise ValueError('Missing GitHub secret/configuration: ' + name)
    return value


def b64url(value):
    return base64.urlsafe_b64encode(value).rstrip(b'=').decode('ascii')


def der_signature_to_raw(data):
    """Convert OpenSSL ASN.1 ECDSA signature to the ES256 JWS 64-byte form."""
    offset = 0

    def tlv(tag):
        nonlocal offset
        if offset + 2 > len(data) or data[offset] != tag:
            raise ValueError('Invalid signing response')
        offset += 1
        length = data[offset]
        offset += 1
        if length & 128:
            count = length & 127
            if not 1 <= count <= 2 or offset + count > len(data):
                raise ValueError('Invalid signing response')
            length = int.from_bytes(data[offset:offset + count], 'big')
            offset += count
        if offset + length > len(data):
            raise ValueError('Invalid signing response')
        value = data[offset:offset + length]
        offset += length
        return value

    sequence = tlv(0x30)
    if offset != len(data):
        raise ValueError('Invalid signing response')
    data, offset = sequence, 0
    values = []
    for _ in range(2):
        value = tlv(0x02)
        if not value or value[0] & 128:
            raise ValueError('Invalid signing response')
        value = value.lstrip(b'\0') or b'\0'
        if len(value) > 32:
            raise ValueError('API key must use the P-256 curve')
        values.append(value.rjust(32, b'\0'))
    if offset != len(data):
        raise ValueError('Invalid signing response')
    return b''.join(values)


def jwt(key_path, key_id, issuer_id, now=None):
    if not re.fullmatch(r'[A-Z0-9]{10}', key_id):
        raise ValueError('ASC_KEY_ID must be the 10-character Apple Key ID')
    if not re.fullmatch(r'[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}', issuer_id):
        raise ValueError('ASC_ISSUER_ID must be the Team API Issuer ID UUID')
    now = int(time.time()) if now is None else int(now)
    header = b64url(json.dumps({'alg': 'ES256', 'kid': key_id, 'typ': 'JWT'}, separators=(',', ':')).encode())
    payload = b64url(json.dumps({'iss': issuer_id, 'iat': now, 'exp': now + 600, 'aud': 'appstoreconnect-v1'}, separators=(',', ':')).encode())
    message = (header + '.' + payload).encode()
    signed = subprocess.run(['openssl', 'dgst', '-sha256', '-sign', str(key_path)], input=message, capture_output=True)
    if signed.returncode:
        raise ValueError('ASC_PRIVATE_KEY is not a usable Apple .p8 private key')
    return message.decode() + '.' + b64url(der_signature_to_raw(signed.stdout))


def api_get(resource, params=None):
    token = jwt(required('ASC_KEY_PATH'), required('ASC_KEY_ID'), required('ASC_ISSUER_ID'))
    url = API + resource
    if params:
        url += '?' + urllib.parse.urlencode(params)
    request = urllib.request.Request(url, headers={'Authorization': 'Bearer ' + token, 'Accept': 'application/json'})
    try:
        with urllib.request.urlopen(request, timeout=60) as response:
            return json.load(response)
    except urllib.error.HTTPError as error:
        if error.code == 401:
            raise RuntimeError('Apple rejected API authentication (401). Check Key ID, Issuer ID and the .p8 secret.') from None
        if error.code == 403:
            raise RuntimeError('Apple denied this API operation (403). Check API role, team permissions and pending Apple agreements.') from None
        raise RuntimeError('Apple API request failed (HTTP %d). No credentials were logged.' % error.code) from None
    except urllib.error.URLError:
        raise RuntimeError('Apple API connection failed. Check the current upload before retrying.') from None


def api_send(method, resource, body=None, params=None):
    """POST / PATCH / GET with a JSON body; Apple error titles and details (never credentials) are reported."""
    token = jwt(required('ASC_KEY_PATH'), required('ASC_KEY_ID'), required('ASC_ISSUER_ID'))
    url = API + resource
    if params:
        url += '?' + urllib.parse.urlencode(params)
    data = None if body is None else json.dumps(body).encode()
    headers = {'Authorization': 'Bearer ' + token, 'Accept': 'application/json'}
    if data is not None:
        headers['Content-Type'] = 'application/json'
    request = urllib.request.Request(url, data=data, method=method, headers=headers)
    try:
        with urllib.request.urlopen(request, timeout=60) as response:
            raw = response.read()
            return json.loads(raw) if raw else {}
    except urllib.error.HTTPError as error:
        try:
            errors = json.loads(error.read() or b'{}').get('errors', [])
            text = '; '.join((e.get('title') or '') + ': ' + (e.get('detail') or '') for e in errors)[:1500]
        except ValueError:
            text = ''
        raise RuntimeError('Apple API %s %s failed (HTTP %d). %s' % (method, resource.split('?')[0], error.code, text)) from None
    except urllib.error.URLError:
        raise RuntimeError('Apple API connection failed. Check App Store Connect before retrying.') from None


SUBMITTED_STATES = {'WAITING_FOR_REVIEW', 'IN_REVIEW', 'PENDING_DEVELOPER_RELEASE', 'PENDING_APPLE_RELEASE', 'PROCESSING_FOR_APP_STORE', 'READY_FOR_SALE', 'READY_FOR_DISTRIBUTION'}


def submit(app_id, version, build_number):
    """Submit a processed TestFlight build for App Store review (owner request only). The version is released MANUALLY after
    approval, so approval never publishes it by itself."""
    if not re.fullmatch(r'\d+', app_id) or not re.fullmatch(r'\d+\.\d+(?:\.\d+)?', version) or not re.fullmatch(r'\d+(?:\.\d+)*', build_number):
        raise ValueError('Invalid app ID, version or build number')
    builds = api_send('GET', 'builds', params={'filter[app]': app_id, 'filter[version]': build_number, 'filter[preReleaseVersion.version]': version, 'limit': '5'}).get('data', [])
    if len(builds) != 1:
        raise ValueError('Build %s of version %s was not found in App Store Connect' % (build_number, version))
    build = builds[0]
    state = build['attributes'].get('processingState')
    if state != 'VALID':
        raise RuntimeError('Build %s is %s, not VALID; it cannot be submitted yet' % (build_number, state))
    if build['attributes'].get('usesNonExemptEncryption') is None:
        raise RuntimeError('Export compliance (encryption) is not answered for build %s. Answer it in App Store Connect > TestFlight, then run this again.' % build_number)
    versions = api_send('GET', 'apps/%s/appStoreVersions' % app_id, params={'filter[platform]': 'IOS', 'filter[versionString]': version, 'limit': '5'}).get('data', [])
    if versions:
        store_version = versions[0]
        current = store_version['attributes'].get('appStoreState')
        if current in SUBMITTED_STATES:
            print('Version %s is already %s; nothing submitted.' % (version, current))
            return
    else:
        store_version = api_send('POST', 'appStoreVersions', {'data': {'type': 'appStoreVersions', 'attributes': {'platform': 'IOS', 'versionString': version, 'releaseType': 'MANUAL'}, 'relationships': {'app': {'data': {'type': 'apps', 'id': app_id}}}}})['data']
        print('App Store version %s created (release: manual).' % version)
    version_id = store_version['id']
    api_send('PATCH', 'appStoreVersions/%s' % version_id, {'data': {'type': 'appStoreVersions', 'id': version_id, 'attributes': {'releaseType': 'MANUAL'}}})
    api_send('PATCH', 'appStoreVersions/%s/relationships/build' % version_id, {'data': {'type': 'builds', 'id': build['id']}})
    print('Build %s attached to version %s.' % (build_number, version))
    open_submissions = api_send('GET', 'reviewSubmissions', params={'filter[app]': app_id, 'filter[platform]': 'IOS', 'filter[state]': 'READY_FOR_REVIEW', 'limit': '5'}).get('data', [])
    submission = open_submissions[0] if open_submissions else api_send('POST', 'reviewSubmissions', {'data': {'type': 'reviewSubmissions', 'attributes': {'platform': 'IOS'}, 'relationships': {'app': {'data': {'type': 'apps', 'id': app_id}}}}})['data']
    try:
        api_send('POST', 'reviewSubmissionItems', {'data': {'type': 'reviewSubmissionItems', 'relationships': {'reviewSubmission': {'data': {'type': 'reviewSubmissions', 'id': submission['id']}}, 'appStoreVersion': {'data': {'type': 'appStoreVersions', 'id': version_id}}}}})
    except RuntimeError as error:
        if 'HTTP 409' not in str(error):
            raise
        print('The version is already an item of the review submission.')
    result = api_send('PATCH', 'reviewSubmissions/%s' % submission['id'], {'data': {'type': 'reviewSubmissions', 'id': submission['id'], 'attributes': {'submitted': True}}})
    final = result.get('data', {}).get('attributes', {}).get('state', 'submitted')
    print('Submitted version %s (build %s) for App Store review: %s. Release after approval: manual.' % (version, build_number, final))
    summary = os.environ.get('GITHUB_STEP_SUMMARY')
    if summary:
        with open(summary, 'a') as file:
            file.write('## App Store review\n\nVersion `%s` build `%s` submitted for review (state `%s`). Release after approval is manual.\n' % (version, build_number, final))


def prepare():
    path = Path(required('ASC_KEY_PATH'))
    key = required('ASC_PRIVATE_KEY').replace('\\n', '\n').replace('\r\n', '\n')
    if not key.startswith('-----BEGIN PRIVATE KEY-----') or not key.endswith('-----END PRIVATE KEY-----'):
        raise ValueError('ASC_PRIVATE_KEY must contain the complete .p8 text including BEGIN/END lines')
    path.parent.mkdir(parents=True, exist_ok=True)
    with open(path, 'w', opener=lambda name, flags: os.open(name, flags, 0o600)) as file:
        file.write(key + '\n')
    path.chmod(0o600)
    apps = api_get('apps', {'filter[bundleId]': BUNDLE_ID, 'limit': '2'}).get('data', [])
    if len(apps) != 1 or apps[0].get('attributes', {}).get('bundleId') != BUNDLE_ID:
        raise ValueError('No accessible App Store Connect app record matches ' + BUNDLE_ID)
    app_id = apps[0]['id']
    if not re.fullmatch(r'\d+', app_id):
        raise ValueError('Unexpected Apple app identifier')
    with open(required('GITHUB_OUTPUT'), 'a') as file:
        file.write('app_id=' + app_id + '\n')
    print('Apple API authentication and native app record verified.')


def check_build(app_id, version):
    if not re.fullmatch(r'\d+', app_id) or not re.fullmatch(r'\d+\.\d+', version):
        raise ValueError('Invalid app ID or build number')
    for _ in range(30):
        builds = api_get('builds', {'filter[app]': app_id, 'filter[version]': version, 'limit': '5'}).get('data', [])
        if builds:
            state = builds[0]['attributes']['processingState']
            print('Apple build processing: ' + state)
            if state in ('FAILED', 'INVALID'):
                raise RuntimeError('Apple received the build but processing failed. See TestFlight and the Apple notification for details.')
            if state == 'VALID':
                print('Build processed. Open TestFlight in App Store Connect to complete compliance and add your internal tester.')
                summary = os.environ.get('GITHUB_STEP_SUMMARY')
                if summary:
                    with open(summary, 'a') as file:
                        file.write('## Native iPhone build uploaded\n\nBuild `' + version + '` processed successfully by Apple.\n\nOpen [App Store Connect](https://appstoreconnect.apple.com/apps/' + app_id + '/testflight) to complete any encryption compliance and add yourself as an internal tester. This workflow does not publish to the public App Store or add external testers.\n')
                return
        else:
            print('Waiting for the uploaded build to appear in App Store Connect…')
        time.sleep(15)
    raise RuntimeError('Upload command completed, but Apple processing is not confirmed yet. Check TestFlight before uploading again.')


if __name__ == '__main__':
    try:
        if sys.argv[1] == 'prepare':
            prepare()
        elif sys.argv[1] == 'check':
            check_build(sys.argv[2], sys.argv[3])
        elif sys.argv[1] == 'submit':
            submit(sys.argv[2], sys.argv[3], sys.argv[4])
        else:
            raise ValueError('Unknown operation')
    except (ValueError, RuntimeError, OSError) as error:
        # Controlled error messages only; never print an HTTP body, JWT or key.
        print(str(error), file=sys.stderr)
        sys.exit(1)
