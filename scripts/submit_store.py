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
        detail = ''
        try:
            fault = json.loads(error.read())
            if isinstance(fault.get('error'), dict): fault = fault['error']
            detail = str(fault.get('code', '')) + ': ' + str(fault.get('message', ''))
            for name in ('STORE_CLIENT_SECRET', 'STORE_CERTIFICATION_NOTES'):
                value = os.environ.get(name, '')
                if value: detail = detail.replace(value, '[redacted]')
            detail = re.sub(r'https?://\S+', '[URL]', detail)[:500]
        except (ValueError, AttributeError): pass
        raise RuntimeError('Store request failed: HTTP %s during %s. %s' % (error.code, method, detail)) from None
    except urllib.error.URLError:
        raise RuntimeError('Store connection failed; credentials were not logged.') from None

def listing_content(edition):
    if edition not in ('User', 'Admin'):
        raise ValueError('Invalid edition')
    path = Path(__file__).parents[1] / 'store' / 'listings.json'
    content = json.loads(path.read_text(encoding='utf-8'))
    if content.get('schemaVersion') != 1:
        raise ValueError('Unsupported Store listing content')
    specific = content['editions'][edition]
    description = content['description'] + '\n\n' + specific['descriptionSuffix']
    features = content['features'] + specific['features']
    if not description.strip() or len(description) > 10000 or not 1 <= len(features) <= 20:
        raise ValueError('Invalid Store description or feature count')
    if any(not feature.strip() or len(feature) > 200 for feature in features):
        raise ValueError('Store features must contain 1 to 200 characters')
    return content, description, features

SCREENSHOT_NAMES = ('Main-Dashboard','Bill-of-Quantities','RC-Settings','Canopy-Ledge','Earthworks-Gravel','Doors-Windows','Steel-Plates','Metal-Fascia','Moulding')

def screenshot_assets(version):
    folder = os.environ.get('STORE_SCREENSHOT_DIR', '').strip()
    if not folder: return []
    edition = os.environ.get('STORE_EDITION', 'User')
    assets = []
    for name in SCREENSHOT_NAMES:
        source = Path(folder) / (name + '.png')
        data = source.read_bytes()
        if len(data) < 24 or data[:8] != b'\x89PNG\r\n\x1a\n' or data[12:16] != b'IHDR':
            raise ValueError('Invalid screenshot PNG: ' + name)
        width, height = int.from_bytes(data[16:20], 'big'), int.from_bytes(data[20:24], 'big')
        if width < 1366 or height < 768:
            raise ValueError('Screenshot must be at least 1366 x 768: ' + name)
        assets.append((source, 'DTCE-' + edition + '-' + version + '-' + name + '.png', name.replace('-', ' ')))
    return assets

def update_screenshots(listing, assets):
    if not assets: return
    names = {filename for _, filename, _ in assets}
    kept = []
    for image in listing.get('images', []):
        if image.get('fileName') in names and image.get('fileStatus') == 'PendingUpload': continue
        image = dict(image)
        # Submission API: desktop images use Screenshot (Microsoft Learn, Image resource).
        if image.get('imageType') == 'Screenshot': image['fileStatus'] = 'PendingDelete'
        kept.append(image)
    listing['images'] = kept + [dict(fileName=filename, fileStatus='PendingUpload', imageType='Screenshot', description=description) for _, filename, description in assets]

def prepare(submission, filename, version, notes):
    content, description, features = listing_content(os.environ.get('STORE_EDITION', 'User'))
    release_notes = (content['releaseNotesByVersion'].get(version)
                     or os.environ.get('STORE_RELEASE_NOTES', '').strip()
                     or ('DTCE Tool Kit ' + version + ': maintenance update.'))
    if len(release_notes) > 1500:
        raise ValueError('Store release notes exceed 1500 characters')
    assets = screenshot_assets(version)
    old = submission.get('applicationPackages', [])
    for package in old:
        if package.get('version') and tuple(map(int, package['version'].split('.'))) >= tuple(map(int, version.split('.'))):
            raise ValueError('Increase package version above the existing Store version')
    for package in old: package['fileStatus'] = 'PendingDelete'
    submission['applicationPackages'] = old + [{'fileName': filename, 'fileStatus': 'PendingUpload'}]
    submission['notesForCertification'] = notes
    for locale, listing in submission.get('listings', {}).items():
        # Do not replace localized text with English or modify images/product settings.
        if locale.lower() in content['locales']:
            listing['baseListing'].update(description=description, features=list(features), releaseNotes=release_notes)
            update_screenshots(listing['baseListing'], assets)
            for override in listing.get('platformOverrides', {}).values():
                if isinstance(override, dict):
                    override.update(description=description, features=list(features), releaseNotes=release_notes)
                    if 'images' in override: update_screenshots(override, assets)
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
            if os.environ.get('STORE_REPLACE_PENDING', '').strip().lower() != 'true':
                raise ValueError('Existing submission is not editable: ' + state + '. It was not changed.')
            # Owner request (18 November 2026): cancel the submission still in review and submit the new build.
            # Validate the new package first so nothing is cancelled for a package that would be rejected.
            prepare(request(base + '/submissions/' + pending['id'], headers=headers), package.name, version, notes)
            print('Cancelling existing submission ' + pending['id'] + ' (' + state + ') as requested by the owner', flush=True)
            try: request(url, 'DELETE', headers)
            except RuntimeError as error:
                raise ValueError('The Store did not allow cancelling the submission in ' + state + ' (' + str(error) + '). Cancel it in Partner Center, then run the upload again.')
            app = request(base, headers=headers)
            if app.get('pendingApplicationSubmission'):
                raise ValueError('The previous submission is still pending after the cancel request. Cancel it in Partner Center, then run the upload again.')
            print('Previous submission cancelled.')
            pending = None
    if pending:
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
    print('Updating submission metadata and requesting upload URL', flush=True)
    updated = request(url, 'PUT', headers, body=prepare(submission, package.name, version, notes))
    upload = (updated or {}).get('fileUploadUrl')
    if not upload: raise ValueError('Updated draft has no upload URL; package not uploaded or committed')
    parsed = urllib.parse.urlsplit(upload)
    if parsed.scheme != 'https' or not (parsed.hostname or '').endswith('.blob.core.windows.net'):
        raise ValueError('Unexpected upload host; draft left uncommitted')
    if os.environ.get('GITHUB_ACTIONS') == 'true': print('::add-mask::' + upload)
    with tempfile.TemporaryDirectory() as folder:
        archive = Path(folder) / 'submission.zip'
        with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED) as z:
            z.write(package, package.name)
            for source, filename, _ in screenshot_assets(version): z.write(source, filename)
        print('Uploading package archive', flush=True)
        request(upload, 'PUT', {'x-ms-blob-type': 'BlockBlob', 'Content-Type': 'application/zip'}, archive.read_bytes())
    print('Committing updated submission', flush=True)
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

