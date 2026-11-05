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


## Listing content

`store/listings.json` contains the reviewed description, Features list, edition-specific
text and version-specific release notes. The submission script applies these fields
only to existing supported English listings (`en-us`, `en-gb`, `en-ph`); it does not
create/remove locales or replace non-English translations. Pricing, audience, product names and publication mode are preserved. With
STORE_SCREENSHOT_DIR configured, the submission archive includes eight current
screenshots from that edition's compiled UI. Existing desktop screenshots are marked
for replacement; logos and other image types are preserved. Without that setting,
all images are preserved. CI captures User and Admin separately using synthetic
test projects and retains both galleries as the DTCE-Store-Screenshots artifact. Version-specific release notes in
this file take precedence over repository-variable release notes.

Normal access codes now use BA4 program masks. Existing BA3 signatures remain
verifiable, including the original combined beam/slab formatter permission; they do
not implicitly approve the new Purlin Design or Truss Connections programs. BC1
certification codes retain their version-bound all-program review access. Keys are
unchanged. Update Windows and phone generators together.

Program of Works Excel links export final quantities to one output worksheet.
Project inputs/DXF data remain in Save Program of Works files; they are no longer
embedded in Excel by the UI. Older workbook snapshots are left untouched and are
not used to restore projects through Open / Link Excel.

Version 1.4.35 adds group Start settings and the Bill of Quantities with source rows.
Plate thickness inputs use mm; whole-sheet weights use nominal handbook dimensions
and 7850 kg/m3. Plate fitting compares rotated guillotine layouts and reuses
compatible offcuts. It is an estimating heuristic, not a global optimum guarantee.
Area-only entries calculate net weight but need cut dimensions before final
procurement export. Soil defaults (100% suitable, 20% swell, 10% bank shrink) are
editable estimating assumptions at Earthworks & Gravel > Start.
