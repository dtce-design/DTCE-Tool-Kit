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
- STORE_APP_ID = 9NSQN0157W5K
- STORE_AUTO_UPLOAD_ENABLED = true (enable only when all prerequisites are ready)
- STORE_RELEASE_NOTES = optional version-specific release notes

Do not paste secrets in chat or commit them. Rotate the secret before expiry.
This API credential is separate from AdminPrivateKey.dat; no Admin key is needed.

The script preserves audience, pricing, images, and the existing publication mode.
Manual publication mode still waits for publication. Immediate mode releases only
following Microsoft approval. Upload success is not certification approval.
Do not manually edit an API-created draft while automation is processing it.
If a request fails the draft is kept for investigation; no automatic deletion.

Admin is a separate Store product. It requires its exact identity and a separately
validated key-free package. This workflow validates User identity and cannot
upload to an Admin product.

Reference: https://learn.microsoft.com/en-us/windows/uwp/monetize/manage-app-submissions
