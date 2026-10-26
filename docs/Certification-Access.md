# Microsoft Store certification access

Available in desktop User/Admin v1.4.27+, DTCE Access iOS v1.1.0 and the updated mobile website.

1. Upload the updated **User** MSIX to the existing User Store submission. Old User packages cannot validate BC1 certification codes.
2. In Windows Admin > Access Code Generator, check **Store certification (30 days)**. On the mobile website choose Code type > Store certification. In the native iPhone app choose **Certification** after unlocking your existing key with Face ID.
3. Enter the exact three-part User version submitted to Microsoft (currently **1.4.27**). Generate and copy the code. No test-PC request is needed. All tools, including Project Information, are enabled.
4. Paste it in **Submission options > Notes for certification** with the instructions below. Never commit the generated code or signing key to GitHub.
5. Submit while the credential has enough time remaining. Generate a fresh code if review is delayed. Expiry is 30 days from generation, including time while closed. Offline codes cannot be revoked individually before expiry.

The credential works on multiple test PCs, only for the exact target User version, until its signed expiry. Anyone holding it can unlock that version; keep it in certification notes. The same production tools are exposed to reviewers and regular users. Regular BA3 codes remain request-bound and expire eight hours after generation. The Admin first-use password and Face ID protection are unchanged.

## Notes for certification (replace brackets)

DTCE Tool Kit does not require an online account. To test User version [1.4.27], launch the app, paste the certification access code below in the access-code input, and select Unlock DTCE Tool Kit. The request code displayed above the input can be ignored for this credential.

Certification access code: [PASTE FULL BC1 CODE]
Valid until: [EXPIRY DATE AND TIME, WITH TIME ZONE]

This credential enables Beam Schedule Formatter, Beam Estimator, Splice / Development / Hooks calculations, and Project Information on your test PCs. No separate administrator app, request-code exchange, or key import is required. Access remains available after closing/reopening until the credential expires. It is restricted to the package version listed above. The normal customer credential lasts eight hours; this review credential lasts 30 days from generation.

For DXF workflows, use a beam schedule DXF with beam marks and reinforcement values. [Provide a non-confidential sample DXF link if available.] Calculations and Project Information can be opened directly from the launcher.

## Protocol / verification

BC1.issued.expiry.7.version-with-hyphens.base64signature

RSA SHA-256 PKCS#1 v1.5 signs UTF-8: DTCE-CERTIFICATION-V1|issued.expiry|7|version-with-hyphens.
Expiry minus issued must be exactly 2,592,000 seconds; version must match the compiled three-part Engine.Version; the existing owner public key verifies the signature. Device request text is not part of BC1. Normal BA3 protocol is unchanged. Test fixtures use disposable keys only.
