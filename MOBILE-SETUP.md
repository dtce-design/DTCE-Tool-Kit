# DTCE Access Admin for iPhone

Source: mobile/. The desktop and mobile projects share the existing public key
and BA3 request-specific 8-hour access protocol. No signing key, password or
exported owner file is committed, packaged, or uploaded by CI.

## GitHub hosting
1. Open repository Settings > Pages. Select GitHub Actions as the build source.
2. In Settings > Secrets and variables > Actions > Variables, add
   MOBILE_PAGES_ENABLED = true.
3. Run the Build, test and upload both editions workflow from Actions > Run workflow
   (main branch), or push the next tested change to main.
4. The mobile-pages job reports the actual deployed HTTPS URL. Do not treat an
   assumed github.io address as live until that deployment succeeds.

The source repository stays private. GitHub Pages from a private repository
requires an eligible GitHub plan. The resulting Pages site may be publicly
readable depending on plan/access settings, but it contains no signing secret.
Anyone can see the UI; only someone holding your key file AND password can sign.
Do not make the whole desktop source repository public to enable free hosting.
Only the Mobile ZIP contents are published; no EXE or MSIX is served by this job.
If Pages is unavailable for your plan, the key-free Mobile ZIP can be hosted on
another HTTPS static host. The app cannot be used by opening index.html in Files.

## One-time key transfer
1. Open Admin v1.4.26 or newer, Access Code Generator.
2. Import the existing AdminPrivateKey.dat if it is not loaded yet.
3. Click Export key for iPhone. Set and confirm a strong password (12+ characters).
4. Save the .dtcekey file outside the repository and transfer it privately to the
   iPhone Files app. Do not commit it, put it in GitHub Secrets, or upload it to Pages.
5. In Safari, open the deployed HTTPS URL. Choose mobile key and select that file.
6. Enter the export password and tap Unlock. The file is decrypted on the phone.
7. Optional: keep only the encrypted key in browser storage. Password is required
   after locking/reopening. Do not use a shared phone/browser profile.
8. Safari Share > Add to Home Screen. Launch the icon and import the encrypted
   file again if Safari and the installed web app use separate storage.

## Generate access
Paste the request shown by the current User app. Select allowed apps and tap
Generate 8-hour access. Project Information is automatic. Copy or Share the code.
Eight hours begins at generation, not activation. Keep iPhone automatic date/time
on. Existing desktop User versions using BA3 can verify these codes unchanged.
The key locks after 5 minutes without interaction and when the app is hidden.
Previously issued codes still expire according to their embedded expiry time.
After first online load, service worker caches only static app assets for offline
use. iOS may clear website data; retain the encrypted file and password safely.

## Protection and tests
Export: PBKDF2-HMAC-SHA256 (600000 iterations), random 16-byte salt, AES-256-CBC
with random 16-byte IV and HMAC-SHA256 encrypt-then-MAC with an independent key.
Private key is imported as a non-extractable Web Crypto RSA signing key in memory.
No analytics, third-party scripts, signing server, or external fonts are used.
The public key ID/modulus/exponent are checked before signing is enabled.
Windows DPAPI original DAT is neither modified nor directly used on iPhone.
Losing the encrypted export password requires another export from Windows.
A compromised phone, malicious hosted update, or stolen unlocked session can
still misuse a signing key. Keep GitHub account and owner phone protected.

Tests use ephemeral RSA keys: actual .NET export is decrypted with Web Crypto,
then mobile code is accepted by the existing .NET BA3 verifier. Wrong password,
tampered ciphertext, wrong key identity, app masks and duration are checked.
An actual iPhone Safari install/Files picker test is still needed on your device.
