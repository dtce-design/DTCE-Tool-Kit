# Working on DTCE Tool Kit

This repository is the source of truth for future DTCE Tool Kit changes.
Edit `src/`, run `scripts/Test.ps1` and `scripts/Build-User.ps1`, then commit and
push authorized successful changes to `main` so the automatic User release runs.
If using the GitHub connector, use one commit and an expected-head lease when
updating the branch. Do not overwrite unrelated remote changes.

Never copy/commit `AdminPrivateKey.dat`, Owner packages, signing certificates or
credentials. CI only needs `AccessPublicKey.xml`; never regenerate/rotate keys.
Admin is owner's offline tool. Public downloads contain User build only.
Keep edition watermark rules: Admin DTCE, User DPWH.
Store submission is not enabled until MSIX identity/packaging and account API
setup are complete. Do not claim a GitHub release is a Microsoft Store release.

The current user authorizes automatic tested GitHub uploads. This does not
authorize repository visibility changes or disclosure of the owner's key.
