import copy, importlib.util, unittest, tempfile, zipfile
from unittest.mock import patch
from pathlib import Path
spec = importlib.util.spec_from_file_location('store', Path(__file__).parents[1] / 'scripts' / 'submit_store.py')
store = importlib.util.module_from_spec(spec)
spec.loader.exec_module(store)

class StoreTests(unittest.TestCase):
    def test_payload_preserves_audience_pricing_and_mode(self):
        original = {'visibility': 'Private', 'targetPublishMode': 'Manual', 'pricing': {'priceId': 'Free'},
            'applicationPackages': [{'id': 'old', 'version': '1.4.23.0', 'fileStatus': 'Uploaded'}],
            'listings': {'en-us': {'baseListing': {'images': [{'id': 'logo'}]}}}}
        result = store.prepare(copy.deepcopy(original), 'update.msix', '1.4.24.0', 'Reviewer instructions')
        for field in ('visibility', 'targetPublishMode', 'pricing'):
            self.assertEqual(original[field], result[field])
        self.assertEqual('PendingDelete', result['applicationPackages'][0]['fileStatus'])
        self.assertEqual({'fileName': 'update.msix', 'fileStatus': 'PendingUpload'}, result['applicationPackages'][1])
        self.assertEqual([{'id': 'logo'}], result['listings']['en-us']['baseListing']['images'])
        self.assertEqual('Reviewer instructions', result['notesForCertification'])

    def test_same_or_older_version_refused(self):
        for version in ('1.4.23.0', '1.4.22.0'):
            with self.assertRaises(ValueError):
                store.prepare({'applicationPackages': [{'version': '1.4.23.0'}]}, 'update.msix', version, 'notes')

class ListingTests(unittest.TestCase):
    def test_both_editions_update_features_and_description_preserving_other_fields(self):
        for edition in ('User','Admin'):
            original={'applicationPackages':[], 'listings':{'en-us':{'baseListing':{'description':'Old','features':['Old'],'images':[{'id':'screenshot'}],'keywords':['DTCE']},'platformOverrides':{'Windows.Desktop':{'description':'Keep override'}}},'fr-fr':{'baseListing':{'description':'French','features':['French']}}}}
            with patch.dict(store.os.environ,{'STORE_EDITION':edition,'STORE_RELEASE_NOTES':'Old generic note'},clear=True):
                result=store.prepare(copy.deepcopy(original),'update.msix','1.4.34.0','reviewer')
            english=result['listings']['en-us']['baseListing']
            self.assertIn('final quantities',english['description'])
            self.assertIn('13.5', ' '.join(english['features']))
            self.assertIn('separate access',english['releaseNotes'])
            self.assertNotIn('Old generic note',english['releaseNotes'])
            self.assertEqual([{'id':'screenshot'}],english['images'])
            self.assertEqual(['DTCE'],english['keywords'])
            self.assertEqual(original['listings']['fr-fr'],result['listings']['fr-fr'])
            self.assertEqual(english['description'],result['listings']['en-us']['platformOverrides']['Windows.Desktop']['description'])
            self.assertLessEqual(len(english['features']),20)
            self.assertTrue(all(len(f)<=200 for f in english['features']))
            self.assertEqual(edition=='Admin',any('Access Code Generator' in f for f in english['features']))

class EditionTests(unittest.TestCase):
    def make_package(self, folder, edition, private=False):
        package=Path(folder)/'app.msix'
        name='DTCE.DTCEToolKit-Admin' if edition=='Admin' else 'DTCE.DTCEToolKit'
        exe='DTCE-Tool-Kit-Admin.exe' if edition=='Admin' else 'DTCE-Tool-Kit.exe'
        manifest='<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"><Identity Name="'+name+'" Publisher="CN=DBEDF414-F57B-457B-A9EC-C38F24535F31" Version="1.4.24.0" /></Package>'
        with zipfile.ZipFile(package,'w') as z:
            z.writestr('AppxManifest.xml',manifest)
            z.writestr(exe,b'test fixture')
            if private:z.writestr('AdminPrivateKey.dat',b'not a key')
        return package

    def test_both_editions_pass_identity_then_require_credentials(self):
        for edition in ('User','Admin'):
            with tempfile.TemporaryDirectory(dir=Path(__file__).parents[1] / "test-results") as folder:
                package=self.make_package(folder,edition)
                with patch.dict(store.os.environ,{'STORE_EDITION':edition},clear=True),patch.object(store.sys,'argv',['submit',str(package)]):
                    with self.assertRaisesRegex(ValueError,'STORE_TENANT_ID'):store.main()

    def test_wrong_edition_and_private_material_refused(self):
        for edition,private,expected in [('User',False,'edition executable'),('Admin',True,'Private/Admin')]:
            with tempfile.TemporaryDirectory(dir=Path(__file__).parents[1] / "test-results") as folder:
                package=self.make_package(folder,'Admin',private)
                with patch.dict(store.os.environ,{'STORE_EDITION':edition},clear=True),patch.object(store.sys,'argv',['submit',str(package)]):
                    with self.assertRaisesRegex(ValueError,expected):store.main()

    def test_wrong_store_product_refused_without_network(self):
        with tempfile.TemporaryDirectory(dir=Path(__file__).parents[1] / "test-results") as folder:
            package=self.make_package(folder,'Admin')
            env={'STORE_EDITION':'Admin','STORE_TENANT_ID':'fixture','STORE_CLIENT_ID':'fixture','STORE_CLIENT_SECRET':'fixture','STORE_APP_ID':'9NSQN0157W5K','STORE_CERTIFICATION_NOTES':'fixture'}
            with patch.dict(store.os.environ,env,clear=True),patch.object(store.sys,'argv',['submit',str(package)]),patch.object(store,'request') as network:
                with self.assertRaisesRegex(ValueError,'product does not match'):store.main()
                network.assert_not_called()

class DraftTests(EditionTests):
    def test_existing_submission_status_gate(self):
        for status in ('PendingCommit', 'CertificationFailed', 'Certification', 'Publishing'):
            with tempfile.TemporaryDirectory(dir=Path(__file__).parents[1] / 'test-results') as folder:
                package=self.make_package(folder,'Admin')
                env={'STORE_EDITION':'Admin','STORE_TENANT_ID':'fixture','STORE_CLIENT_ID':'fixture','STORE_CLIENT_SECRET':'fixture','STORE_APP_ID':'9P53CR08ZSZ3','STORE_CERTIFICATION_NOTES':'notes'}
                calls=[]
                def fake(url, method='GET', headers=None, data=None, body=None):
                    calls.append((url,method,body))
                    if 'oauth2/token' in url:return {'access_token':'fixture'}
                    if url.endswith('/applications/9P53CR08ZSZ3'):return {'pendingApplicationSubmission':{'id':'draft'}}
                    if url.endswith('/status'):return {'status':status}
                    if method=='GET':return {'id':'draft','applicationPackages':[{'version':'1.4.23.0'}]}
                    if method=='PUT' and body is not None:
                        self.assertEqual('notes',body['notesForCertification'])
                        self.assertEqual('PendingDelete',body['applicationPackages'][0]['fileStatus'])
                        raise RuntimeError('STOP_AFTER_VERIFIED_PUT')
                    raise AssertionError('Unexpected mutation')
                with patch.dict(store.os.environ,env,clear=True),patch.object(store.sys,'argv',['submit',str(package)]),patch.object(store,'request',side_effect=fake):
                    with self.assertRaisesRegex((ValueError,RuntimeError),'STOP_AFTER_VERIFIED_PUT' if status in ('PendingCommit','CertificationFailed') else 'not editable'):
                        store.main()
                self.assertFalse(any(method in ('POST','DELETE') and 'oauth2/token' not in url for url,method,_ in calls))
                if status in ('Certification','Publishing'):self.assertFalse(any(method=='PUT' for _,method,_ in calls))

if __name__ == '__main__': unittest.main()

