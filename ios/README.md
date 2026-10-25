# DTCE Access Admin - native iPhone app

Native SwiftUI, iOS 16+, Face ID-equipped iPhone. No embedded browser or signing
server. Source is in this GitHub repository; Mac GitHub Actions compiles it.

## Implemented
- Import existing password-protected .dtcekey from Windows Admin v1.4.26+ via Files.
- Password verification and decryption happen locally. The key must match the
  desktop public key. No key or password is sent to GitHub or a server.
- Signing key is stored in a device-only iPhone Keychain item protected by
  biometryCurrentSet and WhenPasscodeSetThisDeviceOnly. Passcode fallback is not
  used for app unlock. The existing RSA key cannot be moved into Secure Enclave;
  its protection is the Keychain ACL, not a Secure Enclave RSA key claim.
- Unlock using Face ID. Session locks on background and after 5 minutes without
  interaction. An inactive-scene cover hides sensitive app content.
- App checkboxes, request-specific BA3 codes, 8-hour expiry, copy and native share.
- Face ID enrollment changes invalidate access to the saved item. Remove the saved
  key and import the original encrypted export again. Keep that export backed up.

## CI verification
The Windows build generates a disposable encrypted RSA fixture. The macOS job
uses it to test native decryption/signing, compares the exact deterministic RSA
signature to Web Crypto's code already accepted by the .NET verifier, and checks
wrong password, tampering, identity, mask, request syntax and expiry.
The job builds an iPhone simulator app and unsigned physical-device archive.
Only test keys are used; native device Face ID/Keychain behavior still requires
physical-device validation. The simulator build does not prove Face ID hardware.

## Install on your iPhone
An unsigned ZIP or xcarchive is NOT installable on iPhone. Apple signing is still
required. Open ios/DTCEAccess.xcodeproj in Xcode on a Mac (or hosted Mac), select
DTCEAccess > Signing & Capabilities > your Apple Developer Team, and register a
unique bundle identifier (initial value com.dtce.accessadmin). Keep the same
identifier/team for updates so the saved key remains accessible.

For TestFlight, create an iOS App Store Connect record, use Product > Archive >
Distribute App > App Store Connect, and add yourself as a tester. This requires
Apple Developer Program membership. Complete Apple's privacy/encryption export
compliance questions accurately. This app imports private signing keys and uses
system cryptography; no compliance approval is assumed by this project.

If you already have an Apple Developer account, the next setup inputs are your
Team ID and chosen registered bundle identifier; do not send passwords, private
certificates or App Store Connect private keys in chat. Apple credentials are
not needed for unsigned CI tests. This repository currently does NOT upload
builds to TestFlight automatically.

## First device test
1. Install the signed app; enroll Face ID in iPhone Settings.
2. Import your .dtcekey and enter its export password. Approve Face ID.
3. Generate a code and verify it on the existing Windows User app.
4. Background/reopen: Face ID must be required again.
5. Cancel/deny Face ID: code generation must stay locked.
6. Check 5-minute idle lock and permission selection.
7. With a safely backed-up export, verify removal/re-import and the behavior
   after a changed Face ID enrollment. Never use the only key backup for testing.

The existing Safari web app remains available during native signing setup.
