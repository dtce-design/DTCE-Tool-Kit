import assert from 'node:assert/strict';
import fs from 'node:fs';import path from 'node:path';import {webcrypto} from 'node:crypto';
import {validateEnvelope,unlockEnvelope,generateCode,generateCertificationCode} from '../mobile/crypto.mjs';
if(!globalThis.crypto)globalThis.crypto=webcrypto;
const folder=process.argv[2];
const envelope=JSON.parse(fs.readFileSync(path.join(folder,'mobile-fixture.json'),'utf8'));
const expected=JSON.parse(fs.readFileSync(path.join(folder,'mobile-public.json'),'utf8'));
const password='test-only Unicode passphrase \u00f1 2026';
validateEnvelope(envelope);
const key=await unlockEnvelope(envelope,password,expected);assert.equal(key.extractable,false);
await assert.rejects(()=>unlockEnvelope(envelope,'incorrect password',expected),/Wrong password/);
const tampered={...envelope,ciphertext:(envelope.ciphertext[0]==='A'?'B':'A')+envelope.ciphertext.slice(1)};
await assert.rejects(()=>unlockEnvelope(tampered,password,expected),/Wrong password/);
await assert.rejects(()=>unlockEnvelope(envelope,password,{...expected,keyId:'WRONG'}),/does not match/);
assert.throws(()=>validateEnvelope({...envelope,iterations:1}));
const request='BEAM-0123456789ABCDEF0123456789ABCDEF01234567';
for(const apps of [0,8,15,120,128,4294967296,-1,1.5])await assert.rejects(()=>generateCode(key,request,apps),/Select at least/);
await assert.rejects(()=>generateCode(key,'BEAM-INVALID',1),/complete/);
for(const apps of [1,2,4,16,32,64,119]){const result=await generateCode(key,request,apps);assert.equal(result.code.split('.')[3],String(apps));}
const generated=await generateCode(key,request.toLowerCase(),5);
const parts=generated.code.split('.');assert.equal(parts[0],'BA4');assert.equal(parts[3],'5');assert.equal(Number(parts[2])-Number(parts[1]),28800);
const verifyKey=await crypto.subtle.importKey('jwk',{kty:'RSA',n:expected.n,e:expected.e},{name:'RSASSA-PKCS1-v1_5',hash:'SHA-256'},false,['verify']);
const data=new TextEncoder().encode(`BEAM-ACCESS-V4|${request}|${parts[1]}.${parts[2]}|5`);
assert.equal(await crypto.subtle.verify('RSASSA-PKCS1-v1_5',verifyKey,Buffer.from(parts[4],'base64'),data),true);
fs.writeFileSync(path.join(folder,'mobile-code.txt'),generated.code);
console.log('PASS: .NET encrypted export and Web Crypto signing; password, tamper, key identity, mask and expiry checks.');

const reference=fs.readFileSync(path.join(folder,'cert-reference.txt'),'utf8');
const certParts=reference.split('.'), version=certParts[4].replaceAll('-','.');
const cert=await generateCertificationCode(key,version,Number(certParts[1])*1000);
assert.equal(cert.code,reference);assert.equal(cert.expiry-Number(certParts[1]),2592000);
for(const invalid of ['', '1.4', '1.4.27.0', '01.4.27', '1.4.-1', '1.4.65536', '1-4-27'])await assert.rejects(()=>generateCertificationCode(key,invalid));
for(const invalidTime of [NaN,Infinity,-1])await assert.rejects(()=>generateCertificationCode(key,version,invalidTime));
fs.writeFileSync(path.join(folder,'mobile-cert.txt'),cert.code);
console.log('PASS: 30-day version-bound certification signature matches desktop; invalid versions and dates rejected.');

