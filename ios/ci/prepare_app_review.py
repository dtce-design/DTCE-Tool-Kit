"""Prepare a tested native build for App Review; preserve the manual Unlisted release hold."""
import hashlib
import json
import os
import re
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path
from app_store import API, api_get, jwt, required

APP_ID = '6826852061'
NOTES = '''This utility is intended for Unlisted distribution to authorized DTCE Tool Kit administrators. We will submit Apple's separate Unlisted distribution request. Release remains manual while that request is pending.

Review without any signing-key import or account:
1. Open the app and tap Try demo mode below the Signing key section.
2. A sample request and program selections are provided. Change selections and tap Generate demo access; scroll to Demo output and try Copy or Share.
3. Choose Certification and tap Generate demo certification to preview the version-specific flow.
4. Exit demo mode returns to locked normal mode and restores the original input choices. Backgrounding or restarting also ends the demo.

Demo output is visibly prefixed DEMO. A fresh in-memory RSA identity is used for each demo session. These sample codes cannot activate the Windows app, even if the DEMO marker is removed. Demo mode does not use, save, replace or remove an administrator's real key.

Real approvals require an existing encrypted mobile export from an authorized Windows DTCE administrator and an iPhone with enrolled Face ID. No online sign-in account exists, and no credentials or private signing key are needed to review demo mode. Face ID and the device-only Keychain protect real signing-key storage.'''


def write(resource, method, body=None):
    token = jwt(required('ASC_KEY_PATH'), required('ASC_KEY_ID'), required('ASC_ISSUER_ID'))
    headers = {'Authorization':'Bearer '+token, 'Content-Type':'application/json'}
    request = urllib.request.Request(API+resource, data=json.dumps(body).encode() if body else None, method=method, headers=headers)
    try:
        with urllib.request.urlopen(request, timeout=60) as response:
            value = response.read()
            return json.loads(value) if value else None
    except urllib.error.HTTPError as error:
        try:
            errors = json.loads(error.read()).get('errors', [])
            def messages(value):
                if isinstance(value, dict):
                    if 'code' in value:
                        yield str(value.get('code',''))+': '+str(value.get('detail',value.get('title','')))
                    for key, nested in value.items():
                        if key not in ('code','detail','title'): yield from messages(nested)
                elif isinstance(value, list):
                    for nested in value: yield from messages(nested)
            detail = '; '.join(messages(errors))
        except (ValueError, AttributeError):
            detail = ''
        raise RuntimeError('Apple API HTTP '+str(error.code)+' '+detail[:3000]) from None


def patch(kind, identifier, attributes=None, relationships=None):
    data = {'type':kind, 'id':identifier}
    if attributes: data['attributes'] = attributes
    if relationships: data['relationships'] = relationships
    return write(kind+'/'+identifier, 'PATCH', {'data':data})


def draft():
    rows = api_get('apps/'+APP_ID+'/appStoreVersions').get('data', [])
    rows = [v for v in rows if v['attributes'].get('platform')=='IOS' and v['attributes'].get('appStoreState') in ('PREPARE_FOR_SUBMISSION','READY_FOR_REVIEW','REJECTED','METADATA_REJECTED','DEVELOPER_REJECTED')]
    if len(rows)!=1: raise ValueError('Expected one editable iOS version; no existing submission was changed.')
    if rows[0]['attributes'].get('releaseType')!='MANUAL': raise ValueError('Unlisted release must remain manual.')
    return rows[0]


def prepare(expected_build):
    if not re.fullmatch(r'\d+\.\d+', expected_build): raise ValueError('Invalid expected build number')
    app = api_get('apps/'+APP_ID)['data']
    if app['attributes']['bundleId']!='com.dtce.accessadmin': raise ValueError('Unexpected application')
    builds = api_get('builds', {'filter[app]':APP_ID,'filter[version]':expected_build,'limit':'5'}).get('data', [])
    if len(builds)!=1 or builds[0]['attributes'].get('processingState')!='VALID' or builds[0]['attributes'].get('expired'): raise ValueError('Expected tested build has not completed Apple processing')
    build = builds[0]
    version = draft()
    train = api_get('builds/'+build['id']+'/preReleaseVersion')['data']['attributes']['version']
    if version['attributes']['versionString']!=train:
        patch('appStoreVersions',version['id'],{'versionString':train})
    if build['attributes'].get('usesNonExemptEncryption') is None:
        # Reuse the declaration already supplied for the prior build of this app.
        prior = api_get('builds/852e5dde-a097-47d3-9b12-8960b68031ec')['data']['attributes']
        if prior.get('usesNonExemptEncryption') is not False: raise ValueError('An encryption declaration is required')
        patch('builds',build['id'],{'usesNonExemptEncryption':False})
    write('appStoreVersions/'+version['id']+'/relationships/build','PATCH',{'data':{'type':'builds','id':build['id']}})
    review = api_get('appStoreVersions/'+version['id']+'/appStoreReviewDetail').get('data')
    if not review: raise ValueError('Review contact is missing')
    for field in ('contactFirstName','contactLastName','contactEmail','contactPhone'):
        value = review['attributes'].get(field)
        if not value: raise ValueError('Review contact field is missing: '+field)
        if os.environ.get('GITHUB_ACTIONS')=='true': print('::add-mask::'+value)
    patch('appStoreReviewDetails',review['id'],{'demoAccountRequired':False,'notes':NOTES})
    if not app['attributes'].get('contentRightsDeclaration'):
        patch('apps',APP_ID,{'contentRightsDeclaration':'DOES_NOT_USE_THIRD_PARTY_CONTENT'})
    print('Tested iOS build '+expected_build+' selected for version '+train+'. Reviewer instructions saved. Release remains MANUAL.')


def upload_screenshots(expected_build, folder):
    version = draft()
    loc = next(v for v in api_get('appStoreVersions/'+version['id']+'/appStoreVersionLocalizations').get('data', []) if v['attributes']['locale']=='en-US')
    sets = api_get('appStoreVersionLocalizations/'+loc['id']+'/appScreenshotSets').get('data', [])
    group = next((g for g in sets if g['attributes']['screenshotDisplayType']=='APP_IPHONE_67'),None)
    if group is None:
        group = write('appScreenshotSets','POST',{'data':{'type':'appScreenshotSets','attributes':{'screenshotDisplayType':'APP_IPHONE_67'},'relationships':{'appStoreVersionLocalization':{'data':{'type':'appStoreVersionLocalizations','id':loc['id']}}}}})['data']
    files = sorted(folder.glob('DTCE-*.jpg'))
    if len(files)!=4: raise ValueError('Four tested UI screenshots are required')
    current = api_get('appScreenshotSets/'+group['id']+'/appScreenshots').get('data', [])
    old = list(current); wanted = []
    for path in files:
        filename = 'DTCE-Review-'+expected_build+'-'+path.name
        existing = next((v for v in current if v['attributes']['fileName']==filename),None)
        if existing:
            wanted.append(existing['id']); continue
        data = path.read_bytes()
        image = write('appScreenshots','POST',{'data':{'type':'appScreenshots','attributes':{'fileName':filename,'fileSize':len(data)},'relationships':{'appScreenshotSet':{'data':{'type':'appScreenshotSets','id':group['id']}}}}})['data']
        for operation in image['attributes']['uploadOperations']:
            headers = {v['name']:v['value'] for v in operation.get('requestHeaders', [])}
            request = urllib.request.Request(operation['url'],data=data[operation['offset']:operation['offset']+operation['length']],headers=headers,method=operation['method'])
            try:
                with urllib.request.urlopen(request,timeout=120) as response: response.read()
            except urllib.error.HTTPError as error: raise RuntimeError('Screenshot part upload failed: HTTP '+str(error.code)) from None
        patch('appScreenshots',image['id'],{'uploaded':True,'sourceFileChecksum':hashlib.md5(data).hexdigest()})
        wanted.append(image['id'])
    for _ in range(60):
        images = api_get('appScreenshotSets/'+group['id']+'/appScreenshots').get('data', [])
        states = [(v['attributes'].get('assetDeliveryState') or {}).get('state') for v in images if v['id'] in wanted]
        print('Review screenshot states: '+', '.join(str(v) for v in states))
        if 'FAILED' in states: raise RuntimeError('Apple screenshot processing failed')
        if len(states)==4 and all(v=='COMPLETE' for v in states): break
        time.sleep(5)
    else: raise RuntimeError('Screenshots uploaded but Apple processing is still pending; retain the existing assets and check before retrying')
    for image in old:
        name = image['attributes']['fileName']
        if image['id'] not in wanted and (name=='DTCE-Access-iPhone.jpg' or name.startswith('DTCE-Review-')):
            write('appScreenshots/'+image['id'],'DELETE')
    print('Four actual tested iPhone screenshots are complete. Earlier app-owned screenshots replaced.')


def submit(expected_build):
    version = draft()
    selected = api_get('appStoreVersions/'+version['id']+'/build')['data']
    if selected['attributes']['version']!=expected_build: raise ValueError('A different build is selected; review was not submitted')
    submissions = api_get('reviewSubmissions',{'filter[app]':APP_ID,'limit':'50'}).get('data', [])
    active = [s for s in submissions if s['attributes'].get('platform')=='IOS' and s['attributes'].get('state') not in ('COMPLETE','CANCELED')]
    if len(active)>1: raise ValueError('Multiple active submissions require review')
    if active:
        submission = active[0]
        if submission['attributes'].get('state') in ('WAITING_FOR_REVIEW','IN_REVIEW','COMPLETING'):
            print('An Apple review submission is already in progress; no duplicate created.'); return
    else:
        submission = write('reviewSubmissions','POST',{'data':{'type':'reviewSubmissions','attributes':{'platform':'IOS'},'relationships':{'app':{'data':{'type':'apps','id':APP_ID}}}}})['data']
    items = api_get('reviewSubmissions/'+submission['id']+'/items').get('data', [])
    if not items:
        write('reviewSubmissionItems','POST',{'data':{'type':'reviewSubmissionItems','relationships':{'reviewSubmission':{'data':{'type':'reviewSubmissions','id':submission['id']}},'appStoreVersion':{'data':{'type':'appStoreVersions','id':version['id']}}}}})
    else:
        print('Existing draft review items retained.')
    updated = patch('reviewSubmissions',submission['id'],{'submitted':True})
    print('Apple review submission state: '+str(updated['data']['attributes'].get('state')))
    print('Unlisted approval remains a separate Apple request. Release remains held manually.')


if __name__=='__main__':
    if sys.argv[1]=='prepare': prepare(sys.argv[2])
    elif sys.argv[1]=='screenshots': upload_screenshots(sys.argv[2],Path(sys.argv[3]))
    elif sys.argv[1]=='submit': submit(sys.argv[2])
    else: raise ValueError('Unknown operation')
