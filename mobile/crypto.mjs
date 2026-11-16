const utf8 = new TextEncoder();
export const FORMAT = 'DTCE-MOBILE-KEY-V1';
export const ITERATIONS = 600000;
export const encode64 = bytes => btoa(String.fromCharCode(...new Uint8Array(bytes)));
const decode64 = text => Uint8Array.from(atob(text), c => c.charCodeAt(0));
export const normalizeRequest = value => String(value).replace(/\s/g, '').toUpperCase();
export function validateEnvelope(value) {
  if (!value || value.format !== FORMAT || value.iterations !== ITERATIONS) throw new Error('Use a DTCE mobile key exported by the updated Windows Admin app.');
  for (const field of ['salt', 'iv', 'ciphertext', 'mac']) {
    if (typeof value[field] !== 'string' || !/^[A-Za-z0-9+/]+={0,2}$/.test(value[field]) || value[field].length > 24000) throw new Error('Invalid mobile key file.');
  }
  if (decode64(value.salt).length !== 16 || decode64(value.iv).length !== 16 || decode64(value.mac).length !== 32) throw new Error('Invalid mobile key file.');
  const cipher = decode64(value.ciphertext);
  if (cipher.length < 16 || cipher.length % 16) throw new Error('Invalid mobile key file.');
  return value;
}
export async function unlockEnvelope(envelope, password, expected) {
  validateEnvelope(envelope);
  const source = await crypto.subtle.importKey('raw', utf8.encode(password), 'PBKDF2', false, ['deriveBits']);
  const derived = new Uint8Array(await crypto.subtle.deriveBits({name:'PBKDF2', hash:'SHA-256', salt:decode64(envelope.salt), iterations:ITERATIONS}, source, 512));
  let plain;
  try {
    const macKey = await crypto.subtle.importKey('raw', derived.slice(32), {name:'HMAC', hash:'SHA-256'}, false, ['verify']);
    const message = [FORMAT, ITERATIONS, envelope.salt, envelope.iv, envelope.ciphertext].join('|');
    if (!await crypto.subtle.verify('HMAC', macKey, decode64(envelope.mac), utf8.encode(message))) throw new Error('Wrong password or damaged key file.');
    const aes = await crypto.subtle.importKey('raw', derived.slice(0,32), 'AES-CBC', false, ['decrypt']);
    plain = new Uint8Array(await crypto.subtle.decrypt({name:'AES-CBC',iv:decode64(envelope.iv)}, aes, decode64(envelope.ciphertext)));
    const payload = JSON.parse(new TextDecoder().decode(plain));
    if (payload.keyId !== expected.keyId || payload.jwk.n !== expected.n || payload.jwk.e !== expected.e || payload.jwk.kty !== 'RSA') throw new Error('This key does not match the DTCE desktop app.');
    return await crypto.subtle.importKey('jwk', payload.jwk, {name:'RSASSA-PKCS1-v1_5',hash:'SHA-256'}, false, ['sign']);
  } finally { derived.fill(0); if (plain) plain.fill(0); }
}
export async function generateCode(key, input, apps, now = Date.now()) {
  const request = normalizeRequest(input);
  if (!/^BEAM-[A-F0-9]{40}$/.test(request)) throw new Error('Paste the complete BEAM request code from the User app.');
  if (!Number.isInteger(apps) || apps < 1 || apps > 503 || (apps & ~503) !== 0) throw new Error('Select at least one app.');
  if (!Number.isFinite(now) || now < 0) throw new Error('Check the date and time on your phone.');
  const issued = Math.floor(now / 1000), expiry = issued + 28800;
  const signature = await crypto.subtle.sign('RSASSA-PKCS1-v1_5',key,utf8.encode(`BEAM-ACCESS-V4|${request}|${issued}.${expiry}|${apps}`));
  return {code:`BA4.${issued}.${expiry}.${apps}.${encode64(signature)}`, expiry, request};
}

// Store review credential: no device binding; exact User version; all apps; 30 days.
export async function generateCertificationCode(key, version, now = Date.now()) {
  if (typeof version !== 'string' || !/^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$/.test(version) || version.split('.').some(x => Number(x) > 65535)) throw new Error('Enter the exact User version, for example 1.4.27 (three numbers).');
  if (!Number.isFinite(now) || now < 0 || now > 253399708799000) throw new Error('Check the date and time on your phone.');
  const issued = Math.floor(now / 1000), expiry = issued + 2592000, target = version.replaceAll('.', '-');
  const message = `DTCE-CERTIFICATION-V1|${issued}.${expiry}|7|${target}`;
  const signature = await crypto.subtle.sign('RSASSA-PKCS1-v1_5', key, utf8.encode(message));
  return {code:`BC1.${issued}.${expiry}.7.${target}.${encode64(signature)}`, expiry, version};
}

