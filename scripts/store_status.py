"""Read-only Microsoft Store status: the last published submission and any pending one, with its status details.
Nothing is created, changed or deleted. Credentials are read only from environment."""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from submit_store import required, request
import urllib.parse

def versions(submission):
    return ', '.join(sorted({str(p.get('version', '?')) for p in submission.get('applicationPackages', []) if p.get('fileStatus') != 'PendingDelete'})) or '?'

def main():
    edition = os.environ.get('STORE_EDITION', 'User')
    tenant, client, secret = (required(k) for k in ('STORE_TENANT_ID', 'STORE_CLIENT_ID', 'STORE_CLIENT_SECRET'))
    app_id = required('STORE_APP_ID')
    token = request('https://login.microsoftonline.com/' + urllib.parse.quote(tenant, safe='') + '/oauth2/token', 'POST',
        {'Content-Type': 'application/x-www-form-urlencoded'}, urllib.parse.urlencode({'grant_type': 'client_credentials',
         'client_id': client, 'client_secret': secret, 'resource': 'https://manage.devcenter.microsoft.com'}).encode())['access_token']
    if os.environ.get('GITHUB_ACTIONS') == 'true': print('::add-mask::' + token)
    headers = {'Authorization': 'Bearer ' + token}
    base = 'https://manage.devcenter.microsoft.com/v1.0/my/applications/' + app_id
    app = request(base, headers=headers)
    print('Edition: ' + edition)
    for label, key in (('Last published', 'lastPublishedApplicationSubmission'), ('Pending', 'pendingApplicationSubmission')):
        ref = app.get(key)
        if not ref:
            print(label + ': none')
            continue
        sub = request(base + '/submissions/' + ref['id'], headers=headers)
        status = request(base + '/submissions/' + ref['id'] + '/status', headers=headers)
        print('%s: submission %s, package version %s, status %s' % (label, ref['id'], versions(sub), status.get('status')))
        details = status.get('statusDetails') or {}
        for kind in ('errors', 'warnings'):
            for item in details.get(kind) or []:
                print('  %s: %s %s' % (kind[:-1], item.get('code', ''), item.get('details', '')))
        for report in details.get('certificationReports') or []:
            print('  certification report: %s' % report.get('date', ''))

if __name__ == '__main__':
    main()
