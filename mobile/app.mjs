import {validateEnvelope, unlockEnvelope, generateCode} from './crypto.mjs';
const $ = id => document.getElementById(id);
const storageName = 'dtce-mobile-encrypted-key-v1';
let envelope = null, signingKey = null, deadline = 0, generation = 0, busy = false;
const expected = await fetch('./public-key.json', {cache:'no-cache'}).then(r => {if(!r.ok) throw new Error('Public key unavailable'); return r.json();}).catch(() => null);
const unavailable = !globalThis.isSecureContext || !globalThis.crypto?.subtle || !expected;
function status(message, error=false) { $('status').textContent=message; $('status').className=error?'error':''; }
function clearOutput() { generation++; $('output').value=''; $('expiry').textContent='Ready after unlocking and selecting apps.'; $('copy').disabled=$('share').disabled=true; }
function lock() { signingKey=null; deadline=0; generation++; $('password').value=''; $('generate').disabled=$('lock').disabled=true; $('unlock').disabled=unavailable; $('state').textContent='Locked'; $('state').className='badge'; document.body.classList.remove('unlocked'); clearOutput(); }
function touch() { if(signingKey) deadline=Date.now()+300000; }
function saveEncrypted() { try {if($('remember').checked && envelope) localStorage.setItem(storageName, JSON.stringify(envelope)); else localStorage.removeItem(storageName);} catch {status('Storage unavailable. Keep your encrypted key file in Files.');} }
try { const saved=localStorage.getItem(storageName); if(saved){envelope=validateEnvelope(JSON.parse(saved)); $('remember').checked=true; $('file-name').textContent='Saved encrypted key ready';} } catch {status('Choose your encrypted mobile key again.');}
$('key-id').textContent=expected?'Desktop key ID: '+expected.keyId:'Public key configuration unavailable';
if(unavailable){ $('unlock').disabled=true; status('Open this app over HTTPS in Safari. Key configuration and Web Crypto are required.',true); }
$('key-file').addEventListener('change',async event=>{const file=event.target.files[0]; if(!file)return;lock();envelope=null; try {if(file.size>40000)throw new Error('Key file is too large.'); envelope=validateEnvelope(JSON.parse(await file.text())); $('file-name').textContent=file.name; saveEncrypted(); status('Key file loaded. Enter its export password.'); $('password').focus();}catch(error){status(error.message,true);}finally{event.target.value='';}});
$('remember').addEventListener('change',saveEncrypted);
$('unlock').addEventListener('click',async()=>{if(busy)return;if(!envelope){status('Choose your exported mobile key first.',true);return;}const password=$('password').value; $('password').value='';if(!password){status('Enter your export password.',true);return;}busy=true;$('unlock').disabled=true;const attempt=++generation;status('Unlocking on this deviceâ€¦');try{const key=await unlockEnvelope(envelope,password,expected);if(attempt!==generation || document.hidden)return;signingKey=key;touch();$('generate').disabled=$('lock').disabled=false;$('state').textContent='Unlocked';$('state').className='badge ready';document.body.classList.add('unlocked');status('Key unlocked. Paste the user request and select apps.');}catch(error){status(error.message,true);}finally{busy=false;if(!signingKey)$('unlock').disabled=unavailable;}});
$('lock').addEventListener('click',()=>{lock();status('Key locked.');});
$('forget').addEventListener('click',()=>{lock();envelope=null;try{localStorage.removeItem(storageName);}catch{}$('file-name').textContent='No key selected';$('remember').checked=false;status('Saved key removed from this browser.');});
$('request').addEventListener('input',clearOutput);
document.querySelectorAll('.app').forEach(box=>box.addEventListener('change',clearOutput));
$('generate').addEventListener('click',async()=>{if(busy)return;clearOutput();if(!signingKey||Date.now()>=deadline){lock();status('Unlock your key to continue.',true);return;}touch();const attempt=generation;const apps=[...document.querySelectorAll('.app:checked')].reduce((n,x)=>n|Number(x.value),0);busy=true;$('generate').disabled=true;try{const result=await generateCode(signingKey,$('request').value,apps);if(attempt!==generation||!signingKey)return;$('output').value=result.code;$('expiry').textContent='Expires '+new Date(result.expiry*1000).toLocaleString()+'. Project Information included.';$('copy').disabled=$('share').disabled=false;status('Code generated. Send it to the user for this request.');}catch(error){status(error.message,true);}finally{busy=false;$('generate').disabled=!signingKey;}});
$('copy').addEventListener('click',async()=>{try{await navigator.clipboard.writeText($('output').value);status('Access code copied.');}catch{$('output').focus();$('output').select();status('Press and hold the selected code, then choose Copy.');}});
$('share').addEventListener('click',async()=>{const code=$('output').value;if(!code)return;if(!navigator.share){$('copy').click();return;}try{await navigator.share({title:'DTCE Tool Kit access',text:code});}catch(error){if(error.name!=='AbortError')status('Use Copy code to send the access code.',true);}});
for(const event of ['pointerdown','keydown'])document.addEventListener(event,touch,{passive:true});
document.addEventListener('visibilitychange',()=>{if(document.hidden){lock();status('Locked while the app is in the background.');}});
setInterval(()=>{if(signingKey&&Date.now()>=deadline){lock();status('Locked after inactivity.');}},1000);
if('serviceWorker' in navigator && globalThis.isSecureContext)navigator.serviceWorker.register('./sw.js').catch(()=>{});
