# Working on DTCE Tool Kit

This repository is the source of truth for future DTCE Tool Kit changes.
Edit `src/`, run `scripts/Test.ps1`, `scripts/Build-User.ps1` and `scripts/Build-Admin.ps1`, then commit and
push authorized successful changes to `main` so the automatic User/Admin release runs.
If using the GitHub connector, use one commit and an expected-head lease when
updating the branch. Do not overwrite unrelated remote changes.

Never copy/commit `AdminPrivateKey.dat`, Owner packages, signing certificates or
credentials. CI only needs `AccessPublicKey.xml`; never regenerate/rotate keys.
Build both editions from shared src/. Admin Store/CI packages must be key-free.
Never include the owner's key in GitHub artifacts, releases or MSIX packages.
Report watermark: DTCE logo in both Admin and User editions (owner decision, 16 November 2026;
supersedes the earlier Admin DTCE / User DPWH rule).
Store submission is not enabled until MSIX identity/packaging and account API
setup are complete. Do not claim a GitHub release is a Microsoft Store release.

Prepare and test requested changes first. Push or submit only when the user explicitly
asks for that task; do not assume future changes are authorized for automatic upload.
An instruction to push after finishing authorizes that task's tested release. This does
not authorize repository visibility changes or disclosure of the owner's key.

Mobile app source is mobile/. Never commit/export real .dtcekey files or owner keys.
Test.ps1 exercises .NET/Web Crypto interoperability using ephemeral keys.
Build-Mobile.ps1 packages only an allowlist of key-free static assets.
Pages hosting is opt-in; preserve repository visibility and keep exported keys local.

Native iPhone source is ios/. Native crypto must pass the macOS interop test and
Xcode simulator/device builds. Never commit Apple signing files or private keys.
Unsigned archives cannot be installed on a physical iPhone. No automatic claim
of Face ID hardware verification; device enrollment/unlock tests require iPhone.


## Current owner instructions (14 November 2026, Asia/Manila)

Push each completed, checked update and its source to GitHub so workspace resets
do not lose the work. Keep explicit recovery checkpoints in GitHub when needed.
Do not upload to Microsoft Store unless the owner separately requests it.
A GitHub push is not authorization for Store, TestFlight or Pages publication.
Publication workflows must default to off and require explicit manual inputs.
Never include signing keys in a recovery checkpoint or source archive.

## Desktop presentation preference (14 November 2026)

All navigation tabs belong at the top. Display complete tab captions without
abbreviations, ellipses or compressed fixed widths; wrap into additional rows
when needed. Keep this preference for Admin and User and for nested tabs.

## Visual direction (14 November 2026)

Use a Microsoft 365 style for the app: white/light gray workspace, restrained
blue accents, compact command bars and full top-tab captions. Reports use a
professional engineering-document layout with numbered sections, structured
tables, equations, traceable inputs and clear figures. Do not infer an engineer
approval or signature from a generated report.

## Report printing requirements (14 November 2026)

Use A4 paper and a minimum printed text size of 7pt, including metadata,
footers, table headers and graph labels. Favor compact spacing and automatic
page flow to reduce paper use. Preserve calculations and avoid splitting
individual rows or figures. Verify the rendered PDF rather than CSS alone.

## Updated visual preference (14 November 2026)

The owner explicitly relaxed the Microsoft 365 requirement: prioritize an
art-directed, polished DTCE interface rather than imitating Microsoft. Keep
full top tab names, readable tables and compact professional reports. The
current desktop direction uses a navy workspace header, restrained teal
actions and neutral light worksheet surfaces.

## Active draft instructions (14 November 2026)

Owner selected palette option3: dark charcoal and soft gray, with muted
accents and clearer column dividers. Navigation should clearly distinguish
main tab, subtab and nested subtab, all with full captions at the top.
Remove duplicate commercial-stock selectors from RC Start; keep them in
member settings. Formatter tabs should open their tools directly.
The owner said not to finalize yet because more requests are coming. Keep
source checkpoints in GitHub for recovery, but do not package/publish a new
finished build until instructed to finish. See Desktop-Draft-2026-11-14.txt.

## Finalization authorization (14 November 2026)

The owner has now explicitly authorized finalization, superseding the earlier
hold on packaging. Add Workspaces settings with Light, Dark and Use system
settings; persist the selection outside the versioned installation directory.
Finalize v1.4.47 Admin/User/source ZIPs and push the checked source to GitHub.
No Microsoft Store, TestFlight or Pages upload is authorized. Windows native
visual/performance/Excel verification limits must be reported honestly.

## Report and deflection preferences (16 November 2026)

Report body text uses the 7pt minimum to save paper (headings may be larger); keep A4 and
verify rendered output. Beam deflection checks follow NSCP 2015: RC Table 424.2.2 with
Section 424.2.4 long-term deflection, steel/other members per the NSCP Chapter 1 limits.

## Release hold (16 November 2026)

Owner: do not release v1.4.63/v1.4.64 yet, and do not submit to the Microsoft Store.
Keep work on the development branch; do not merge to main (which triggers the automatic
GitHub release) until the owner explicitly asks.

## Plumbing structures (16 November 2026)

Canal, septic vault and catch basin are their own BOQ items. Their materials (excavation, embankment,
gravel, concrete, rebar, forms, CHB, plaster, waterproofing) are listed as sub item numbers under the
structure item and are NOT added to the general item quantities. Do not route them into the general
items (a v1.4.65 test build did this by mistake and was reverted in v1.4.66).

## Beam estimator ties (16 November 2026)

Owner decision: tie sets and tie cut lengths stay automatic (count from the beam schedule, cut length
from section, cover, bend diameter and 135° hooks). Do not add manual tie count / cut length override
columns; the unused TieCount/TieCut input fields may remain as they are.

## Store release authorization (17 November 2026)

Owner explicitly asked to publish to the Microsoft Store: both User and Admin editions of the current
build (requested as v1.4.67; renumbered v1.4.68, now v1.4.105, at the owner's request after further fixes), after the Windows
CI build passes and the owner has checked the test build. This lifts the 16 November release hold for that build only:
merge the development branch to main (automatic GitHub release), then run the workflow manually on
main with publish_microsoft_store=true. Store packages stay key-free. Upload is not certification;
report Partner Center status honestly. TestFlight and Pages remain unauthorized. Later versions need
a new explicit owner request.
