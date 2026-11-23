import base64
import importlib.util
import json
import os
import plistlib
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path

spec = importlib.util.spec_from_file_location('asc', Path(__file__).parents[1] / 'ios' / 'ci' / 'app_store.py')
asc = importlib.util.module_from_spec(spec)
spec.loader.exec_module(asc)

class AppleTokenTests(unittest.TestCase):
    def test_real_es256_token_signature_and_claims(self):
        with tempfile.TemporaryDirectory() as folder:
            folder = Path(folder)
            key, pub, sig = folder/'fixture.p8', folder/'public.pem', folder/'signature.der'
            subprocess.run(['openssl','genpkey','-algorithm','EC','-pkeyopt','ec_paramgen_curve:P-256','-out',str(key)],check=True,capture_output=True)
            token = asc.jwt(key,'TESTKEY123','11111111-2222-3333-4444-555555555555',now=1000)
            header,payload,signature=token.split('.')
            decode=lambda value:base64.urlsafe_b64decode(value+'='*((4-len(value)%4)%4))
            self.assertEqual(json.loads(decode(header))['alg'],'ES256')
            claims=json.loads(decode(payload))
            self.assertEqual(claims['aud'],'appstoreconnect-v1')
            self.assertEqual(claims['exp']-claims['iat'],600)
            raw=decode(signature);self.assertEqual(len(raw),64)
            def integer(value):
                value=value.lstrip(b'\0') or b'\0'
                if value[0]&128:value=b'\0'+value
                return b'\x02'+bytes([len(value)])+value
            body=integer(raw[:32])+integer(raw[32:]);der=b'\x30'+bytes([len(body)])+body
            sig.write_bytes(der)
            self.assertEqual(asc.der_signature_to_raw(der),raw)
            subprocess.run(['openssl','pkey','-in',str(key),'-pubout','-out',str(pub)],check=True,capture_output=True)
            verified=subprocess.run(['openssl','dgst','-sha256','-verify',str(pub),'-signature',str(sig)],input=(header+'.'+payload).encode(),capture_output=True)
            self.assertEqual(verified.returncode,0)

    def test_identifiers_and_corrupt_der_are_rejected(self):
        with self.assertRaises(ValueError):asc.jwt('unused','bad','bad')
        for value in (b'',b'\x30\x01\x00',b'\x30\x06\x02\x01\xff\x02\x01\x01'):
            with self.assertRaises(ValueError):asc.der_signature_to_raw(value)

class UploadScriptTests(unittest.TestCase):
    def test_archive_export_and_rerun_build_number_without_apple_access(self):
        repo=Path(__file__).parents[1]
        with tempfile.TemporaryDirectory() as folder:
            folder=Path(folder); (folder/'ios'/'DTCEAccess').mkdir(parents=True)
            shutil.copyfile(repo/'ios/DTCEAccess/Info.plist',folder/'ios/DTCEAccess/Info.plist')
            binary=folder/'bin';binary.mkdir()
            mock=binary/'xcodebuild';mock.write_text('#!/bin/sh\nprintf "%s\\n" "$*" >> "$MOCK_XCODE_LOG"\n');mock.chmod(0o700)
            env=dict(os.environ,PATH=str(binary)+os.pathsep+os.environ['PATH'],MOCK_XCODE_LOG=str(folder/'calls.txt'),RUNNER_TEMP=str(folder),ASC_KEY_PATH=str(folder/'fixture.p8'),ASC_KEY_ID='TESTKEY123',ASC_ISSUER_ID='11111111-2222-3333-4444-555555555555',GITHUB_RUN_NUMBER='42',GITHUB_RUN_ATTEMPT='2')
            run=subprocess.run(['bash',str(repo/'ios/ci/upload_testflight.sh')],cwd=folder,env=env,capture_output=True,text=True)
            self.assertEqual(run.returncode,0,run.stderr)
            self.assertEqual(plistlib.loads((folder/'ios/DTCEAccess/Info.plist').read_bytes())['CFBundleVersion'],'42.2')
            export=plistlib.loads((folder/'DTCE-ExportOptions.plist').read_bytes())
            self.assertEqual(export['destination'],'upload')
            self.assertEqual(export['teamID'],'8R5VUGW834')
            self.assertEqual(export['signingStyle'],'automatic')
            calls=(folder/'calls.txt').read_text().splitlines()
            self.assertEqual(len(calls),2)
            self.assertIn('-exportArchive',calls[1])
            self.assertIn('-allowProvisioningUpdates',calls[1])
            self.assertIn('-authenticationKeyPath',calls[1])

if __name__=='__main__':unittest.main()

class AppStoreReviewSubmitTests(unittest.TestCase):
    """Owner request (23 November 2026, "Submit for apple review ang test flight"): the processed TestFlight build is attached to
    its App Store version (release manual) and submitted for review; nothing is uploaded or released."""
    def run_submit(self, build_attrs, versions, exempt=False):
        calls = []
        def fake(method, resource, body=None, params=None):
            calls.append((method, resource, body, params))
            if method == 'GET' and resource == 'builds':
                return {'data': [{'id': 'b1', 'attributes': build_attrs}]}
            if method == 'GET' and resource.endswith('/appStoreVersions'):
                return {'data': versions}
            if method == 'POST' and resource == 'appStoreVersions':
                return {'data': {'id': 'v1', 'attributes': {}}}
            if method == 'GET' and resource == 'reviewSubmissions':
                return {'data': []}
            if method == 'POST' and resource == 'reviewSubmissions':
                return {'data': {'id': 'r1'}}
            if method == 'PATCH' and resource == 'reviewSubmissions/r1':
                return {'data': {'attributes': {'state': 'WAITING_FOR_REVIEW'}}}
            return {}
        original = asc.api_send
        asc.api_send = fake
        try:
            asc.submit('123', '1.1.1', '239.2', exempt)
        finally:
            asc.api_send = original
        return calls

    def test_submits_valid_build_with_manual_release(self):
        calls = self.run_submit({'processingState': 'VALID', 'usesNonExemptEncryption': False}, [])
        created = [c for c in calls if c[0] == 'POST' and c[1] == 'appStoreVersions'][0]
        self.assertEqual(created[2]['data']['attributes']['releaseType'], 'MANUAL')
        self.assertIn(('PATCH', 'appStoreVersions/v1/relationships/build', {'data': {'type': 'builds', 'id': 'b1'}}, None), calls)
        self.assertTrue(any(c[0] == 'POST' and c[1] == 'reviewSubmissionItems' for c in calls))
        self.assertEqual(calls[-1][2]['data']['attributes']['submitted'], True)

    def test_refuses_unanswered_export_compliance(self):
        with self.assertRaises(RuntimeError):
            self.run_submit({'processingState': 'VALID', 'usesNonExemptEncryption': None}, [])

    def test_already_in_review_is_left_alone(self):
        calls = self.run_submit({'processingState': 'VALID', 'usesNonExemptEncryption': False}, [{'id': 'v0', 'attributes': {'appStoreState': 'WAITING_FOR_REVIEW'}}])
        self.assertFalse(any(c[0] in ('POST', 'PATCH') for c in calls))

    def test_owner_exempt_answer_is_set_before_submitting(self):
        calls = self.run_submit({'processingState': 'VALID', 'usesNonExemptEncryption': None}, [], exempt=True)
        self.assertEqual(calls[1][0:2], ('PATCH', 'builds/b1'))
        self.assertEqual(calls[1][2]['data']['attributes']['usesNonExemptEncryption'], False)
        self.assertEqual(calls[-1][2]['data']['attributes']['submitted'], True)

    def test_whats_new_is_filled_where_empty(self):
        calls = []
        def fake(method, resource, body=None, params=None):
            calls.append((method, resource, body))
            if method == 'GET' and resource == 'builds':
                return {'data': [{'id': 'b1', 'attributes': {'processingState': 'VALID', 'usesNonExemptEncryption': False}}]}
            if method == 'GET' and resource.endswith('/appStoreVersions'):
                return {'data': [{'id': 'v1', 'attributes': {'appStoreState': 'PREPARE_FOR_SUBMISSION'}}]}
            if method == 'GET' and resource.endswith('/appStoreVersionLocalizations'):
                return {'data': [{'id': 'l1', 'attributes': {'locale': 'en-US', 'whatsNew': None}}, {'id': 'l2', 'attributes': {'locale': 'fil', 'whatsNew': 'Typed'}}]}
            if method == 'POST' and resource == 'reviewSubmissions':
                return {'data': {'id': 'r1'}}
            return {'data': []} if method == 'GET' else {}
        original = asc.api_send
        asc.api_send = fake
        try:
            asc.submit('123', '1.1.1', '239.2', False, 'Bug fixes and performance improvements.')
        finally:
            asc.api_send = original
        patched = [c for c in calls if c[0] == 'PATCH' and c[1].startswith('appStoreVersionLocalizations/')]
        self.assertEqual([c[1] for c in patched], ['appStoreVersionLocalizations/l1'])
        self.assertEqual(patched[0][2]['data']['attributes']['whatsNew'], 'Bug fixes and performance improvements.')
