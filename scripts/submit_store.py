"""Store submission API integration. Credentials are read only from environment."""
import json, os, re, sys, tempfile, time, urllib.request, urllib.parse, urllib.error, zipfile
from pathlib import Path
import xml.etree.ElementTree as ET

def required(name):
    value = os.environ.get(name, '').strip()
    if not value: raise ValueError('Missing configuration: ' + name)
    return value

def request(url, method='GET', headers=None, data=None, body=None):
    headers = dict(headers or {})
    if body is not None:
        data = json.dumps(body).encode()
        headers['Content-Type'] = 'application/json'
    try:
        with urllib.request.urlopen(urllib.request.Request(url, data=data, headers=headers, method=method), timeout=180) as response:
            raw = response.read()
            return json.loads(raw) if raw else None
    except urllib.error.HTTPError as error:
        raise RuntimeError('Store request failed: HTTP %s. Check permissions and submission status.' % error.code) from None
    except urllib.error.URLError:
        raise RuntimeError('Store connection failed; credentials were not logged.') from None

def prepare(submission, filename, version, notes):
    old = submission.get('applicationPackages', [])
    for package in old:
        if package.get('version') and tuple(map(int, package['version'].split('.'))) >= tuple(map(int, version.split('.'))):
            raise ValueError('Increase package version above the existing Store version')
    for package in old: package['fileStatus'] = 'PendingDelete'
    submission['applicationPackages'] = old + [{'fileName': filename, 'fileStatus': 'PendingUpload'}]
    submission['notesForCertification'] = notes
    for listing in submission.get('listings', {}).values():
        listing['baseListing']['releaseNotes'] = os.environ.get('STORE_RELEASE_NOTES', '').strip() or ('DTCE Tool Kit ' + version + ': maintenance update.')
    return submission

def main():
    package = Path(sys.argv[1]).resolve()
    edition = os.environ.get('STORE_EDITION', 'User')
    identities = {'User': ('DTCE.DTCEToolKit', '9NSQN0157W5K'), 'Admin': ('DTCE.DTCEToolKit-Admin', '9P53CR08ZSZ3')}
    if edition not in identities: raise ValueError('Invalid edition')
    identity_name, expected_id = identities[edition]
    with zipfile.ZipFile(package) as z:
        if any(re.search(r'(?i)(AdminPrivateKey|\.dat$|\.pfx$|\.cs$)', n) for n in z.namelist()):
            raise ValueError('Private/Admin content found in User MSIX')
        executable = 'DTCE-Tool-Kit-Admin.exe' if edition == 'Admin' else 'DTCE-Tool-Kit.exe'
        if executable not in z.namelist(): raise ValueError('Missing edition executable')
        if any(n.lower().endswith('.exe') and n != executable for n in z.namelist()): raise ValueError('Unexpected executable in package')
        identity = ET.fromstring(z.read('AppxManifest.xml')).find('{*}Identity')
        if identity.get('Name') != identity_name or identity.get('Publisher') != 'CN=DBEDF414-F57B-457B-A9EC-C38F24535F31':
            raise ValueError('Unexpected User identity')
        version = identity.get('Version')
    tenant, client, secret = (required(k) for k in ('STORE_TENANT_ID', 'STORE_CLIENT_ID', 'STORE_CLIENT_SECRET'))
    app_id, notes = required('STORE_APP_ID'), required('STORE_CERTIFICATION_NOTES')
    if app_id != expected_id: raise ValueError('Store product does not match package edition')
    token = request('https://login.microsoftonline.com/' + urllib.parse.quote(tenant, safe='') + '/oauth2/token', 'POST',
        {'Content-Type': 'application/x-www-form-urlencoded'}, urllib.parse.urlencode({'grant_type': 'client_credentials',
         'client_id': client, 'client_secret': secret, 'resource': 'https://manage.devcenter.microsoft.com'}).encode())['access_token']
    if os.environ.get('GITHUB_ACTIONS') == 'true': print('::add-mask::' + token)
    headers = {'Authorization': 'Bearer ' + token}
    base = 'https://manage.devcenter.microsoft.com/v1.0/my/applications/' + app_id
    app = request(base, headers=headers)
    pending = app.get('pendingApplicationSubmission')
    if pending:
        url = base + '/submissions/' + pending['id']
        state = request(url + '/status', headers=headers)['status']
        print('Existing submission status: ' + state)
        if state not in ('PendingCommit', 'CommitFailed', 'PreProcessingFailed', 'CertificationFailed'):
            raise ValueError('Existing submission is not editable: ' + state + '. It was not changed.')
        submission = request(url, headers=headers)
        # Validate before any PUT; preserve the same submission and Store settings.
        prepare(json.loads(json.dumps(submission)), package.name, version, notes)
        print('Updating existing editable submission: ' + pending['id'])
    else:
        published = app.get('lastPublishedApplicationSubmission')
        if not published: raise ValueError('Complete the initial submission in Partner Center first')
        prepare(request(base + '/submissions/' + published['id'], headers=headers), package.name, version, notes)
        submission = request(base + '/submissions', 'POST', headers)
        print('Created Store submission: ' + submission['id'])
    sid = submission['id']
    url = base + '/submissions/' + sid
    updated = request(url, 'PUT', headers, body=prepare(submission, package.name, version, notes))
    upload = (updated or {}).get('fileUploadUrl')
    if not upload: raise ValueError('Updated draft has no upload URL; package not uploaded or committed')
    parsed = urllib.parse.urlsplit(upload)
    if parsed.scheme != 'https' or not (parsed.hostname or '').endswith('.blob.core.windows.net'):
        raise ValueError('Unexpected upload host; draft left uncommitted')
    if os.environ.get('GITHUB_ACTIONS') == 'true': print('::add-mask::' + upload)
    with tempfile.TemporaryDirectory() as folder:
        archive = Path(folder) / 'submission.zip'
        with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED) as z: z.write(package, package.name)
        request(upload, 'PUT', {'x-ms-blob-type': 'BlockBlob', 'Content-Type': 'application/zip'}, archive.read_bytes())
    request(url + '/commit', 'POST', headers)
    for _ in range(30):
        state = request(url + '/status', headers=headers)['status']
        print('Store status: ' + state)
        if state.endswith('Failed') or state == 'Canceled': raise RuntimeError('Store submission failed; check certification report')
        if state in ('PreProcessing', 'Certification', 'PendingPublication', 'Publishing', 'Published', 'Release'):
            print('Submission accepted for processing. Certification/publication is not guaranteed.')
            return
        time.sleep(10)
    raise RuntimeError('Status polling timed out; check the existing submission before retrying')

if __name__ == '__main__':
    try: main()
    except (ValueError, RuntimeError) as error:
        print(str(error), file=sys.stderr)
        sys.exit(1)
