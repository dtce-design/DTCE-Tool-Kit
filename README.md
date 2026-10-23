# DTCE Tool Kit

Private source repository for DTCE Tool Kit. Includes Beam Schedule Formatter,
Beam Estimator, Splice/Development/Hooks, and Project Information.

## Automatic builds

Each push to `main` runs calculation and access-protocol tests on Windows, then
builds a User-only ZIP. Successful builds are uploaded to **Actions artifacts**
and **GitHub Releases**. Pull requests run checks without publishing a release.
Future local edits must be committed and pushed here to trigger automation.

Download the latest User ZIP from this repository's Releases page. This is a
private repository: downloads require repository access. No public distribution
or visibility change has been configured.

## Local commands

```powershell
./scripts/Test.ps1
./scripts/Build-User.ps1
```

Requires Windows with .NET Framework 4.8. The existing public verification key
is required and is safe to include. Builds fail rather than create/rotate keys.
Never commit `AdminPrivateKey.dat`, Owner ZIPs, certificates, or Store secrets.
The Admin signing key remains offline with its original Windows account.

## Microsoft Store status

GitHub build/upload automation is separate from Store publication. This repo
currently produces a portable User ZIP, not an MSIX. Store package identity,
User-only MSIX packaging, first submission/certification and Partner Center API
credentials must be configured before Store uploads can be automated. No Store
credentials or automatic Store submission are enabled here.

Current application release: 1.4.23. Admin watermark is DTCE; User watermark is
DPWH. Original DTCE logo and app name remain in both editions. Code approval is
scoped to installation/update and expires after eight hours.

Unit weights currently use theoretical steel density 7850 kg/m3; NSCP unit-mass
table verification remains pending. Native Excel live-link testing is required
on a desktop with Excel installed.
