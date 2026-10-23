import copy, importlib.util, unittest
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

if __name__ == '__main__': unittest.main()
