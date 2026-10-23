# User Store automatic upload setup

The existing tested main build now has an optional Store API upload/commit job.
It is OFF until repository variable STORE_AUTO_UPLOAD_ENABLED is true.
Every successful main build then attempts a submission. Increase the version in
src/AssemblyInfo.cs first; identical/older packages are refused.
Existing pending submissions are never overwritten or deleted.

One-time prerequisites: publish the initial Store product with age ratings and
working reviewer access. Associate an organizational Entra directory with Partner
Center. Add an Entra app under Account settings / Users, with the Manager role.
Obtain its tenant ID, client ID and client secret. The consumer tenant ID
9188040d-6c67-4c5b-b112-36a304b66dad is not the automation tenant.

GitHub Settings > Secrets and variables > Actions > repository secrets:
- STORE_TENANT_ID
- STORE_CLIENT_ID
- STORE_CLIENT_SECRET
- STORE_CERTIFICATION_NOTES (actual usable reviewer access instructions)

Repository variables:
- Product IDs are fixed and edition-validated in the workflow.
- STORE_AUTO_UPLOAD_ENABLED = true (enable only when all prerequisites are ready)
- STORE_RELEASE_NOTES = optional version-specific release notes

Do not paste secrets in chat or commit them. Rotate the secret before expiry.
This API credential is separate from AdminPrivateKey.dat; no Admin key is needed.

The script preserves audience, pricing, images, and the existing publication mode.
Manual publication mode still waits for publication. Immediate mode releases only
following Microsoft approval. Upload success is not certification approval.
Do not manually edit an API-created draft while automation is processing it.
If a request fails the draft is kept for investigation; no automatic deletion.

Reference: https://learn.microsoft.com/en-us/windows/uwp/monetize/manage-app-submissions

## Admin edition (now configured)
Store ID: 9P53CR08ZSZ3
Identity: DTCE.DTCEToolKit-Admin
Publisher: CN=DBEDF414-F57B-457B-A9EC-C38F24535F31
Publisher display name: DTCE

Both editions build from shared src/ at the same version. GitHub artifacts and
releases now include separate User and key-free Admin ZIP/MSIX packages.
The per-edition jobs validate fixed product IDs; STORE_APP_ID is no longer needed.
Admin uses the same STORE_TENANT_ID / STORE_CLIENT_ID / STORE_CLIENT_SECRET.
Add secret STORE_ADMIN_CERTIFICATION_NOTES with usable Admin reviewer access,
and optional variable STORE_ADMIN_RELEASE_NOTES. Enable Admin independently with
STORE_ADMIN_AUTO_UPLOAD_ENABLED=true after its initial Store release is published.
An Admin build never contains AdminPrivateKey.dat. Open Access Code Generator and
click Import existing key. Select the original matching DAT under the Windows
account that protected it. The encrypted copy is saved under
%LOCALAPPDATA%\DTCE Tool Kit\Admin\AdminPrivateKey.dat, outside the read-only
Store installation. No new key is generated. Back up your original DAT.
Admin generator cannot work on another Windows account with the original
CurrentUser DPAPI-protected file. Import checks both decryption and public key.
