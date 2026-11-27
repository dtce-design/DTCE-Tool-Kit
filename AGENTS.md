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
Report watermark: none in the Admin edition; DPWH logo in the User edition, for official use (owner
decision, 18 November 2026; supersedes the 16 November DTCE-in-both rule).
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
build (requested as v1.4.67; renumbered v1.4.68, now v1.5.0, at the owner's request after further fixes), after the Windows
CI build passes and the owner has checked the test build. This lifts the 16 November release hold for that build only:
merge the development branch to main (automatic GitHub release), then run the workflow manually on
main with publish_microsoft_store=true. Store packages stay key-free. Upload is not certification;
report Partner Center status honestly. TestFlight and Pages remain unauthorized. Later versions need
a new explicit owner request.
Done 18 November 2026: v1.4.127 merged to main (GitHub release) and both User and Admin uploaded;
Partner Center reported "accepted for processing" (PreProcessing). v1.4.128 and later need a new request.
New owner request (18 November 2026, "paki push sa Microsoft Store"): publish v1.4.129 (User and Admin) the
same way after its Windows CI passes. Later versions again need a new explicit request.
Result 18 November 2026: v1.4.129 merged to main (GitHub release OK), but both Store uploads were refused because
the v1.4.127 submissions were still in Certification ("Existing submission is not editable"). Re-run the failed
Store jobs of run 44231420200 only after v1.4.127 certification finishes (or the owner cancels it in Partner Center).
New owner request (18 November 2026, "pakipush sa Microsoft"): publish v1.5.0 (User and Admin) the same way after
its Windows CI passes; it supersedes the blocked v1.4.129 upload. Later versions again need a new explicit request.
Owner (18 November 2026, "icancel mo tapos ulitin", "cancel certification"): cancel the v1.4.127 submissions still
in certification and submit v1.5.0 instead. Run the workflow with publish_microsoft_store=true and
replace_pending_store_submission=true for this release only; that input stays false by default.
Done 18 November 2026: v1.5.0 merged to main (GitHub release OK). The owner had cancelled the v1.4.127
certification in Partner Center; the canceled submissions were removed and new User and Admin submissions were
created and committed; Partner Center reported "accepted for processing" (PreProcessing). v1.5.1 and later need a
new explicit request.
New owner request (18 November 2026, "upload sa microsoft store"): publish v1.19.4 (User and Admin) the same way after
its Windows CI passes (publish_microsoft_store=true; replace_pending_store_submission stays false unless the owner asks).
Owner (18 November 2026, "isama ang bago"): include the newer changes and publish v1.20.0 instead of v1.19.4.
Result 18 November 2026: v1.20.0 merged to main (GitHub release OK, run 44271865377). Store run 44271896215: both User and
Admin uploads were refused because the v1.5.0 submissions are still in Certification ("Existing submission is not
editable: Certification"). Nothing was changed in Partner Center. Re-run the failed Store jobs of run 44271896215 after
v1.5.0 certification finishes, or - only if the owner asks - with replace_pending_store_submission=true after the owner
cancels the v1.5.0 certification in Partner Center.
Later versions again need a new explicit request.
New owner request (19 November 2026, "paki upload na sa microsoft store"): publish v1.32.0 (User and Admin) the same way after
its Windows CI passes (publish_microsoft_store=true; replace_pending_store_submission stays false unless the owner asks).
Later versions again need a new explicit request.
New owner request (20 November 2026, "release mo na sa microdoft store"): publish v1.50.18 (User and Admin) the same way after
its Windows CI passes (merge PR #11 to main, then publish_microsoft_store=true; replace_pending_store_submission stays false
unless the owner asks). It supersedes the v1.32.0 request. Owner (20 November 2026) chose to release v1.50.19 instead (includes the BOQ status
change), renumbered v5.0.19 at the owner's request (versions since v1.44.0 renumbered, see docs/VERSION-RENUMBER-2026-11-20.txt).
The owner then asked for the "(No Top Coat)" wording change as its own PATCH: the Store release is v5.0.20.
Owner (20 November 2026, "yung mga susunod huwag na muna iupload"): after v5.0.20, do not upload later versions to the
Microsoft Store until the owner asks again (GitHub pushes to the development branch are still fine).
Result 20 November 2026: v5.0.20 merged to main (GitHub release OK). Store run 44688569445: both uploads were refused with
HTTP 409 InvalidState (a leftover uncommitted draft in state 'None'); nothing was changed in Partner Center.
New owner request (20 November 2026, "ito na ang iupload sa microsoft store"; chose v5.1.7): publish v5.1.7 (User and Admin)
instead of v5.0.20, the same way after its Windows CI passes. Owner then added Location Other / Other Finishes and said
"final na yan tapos i-upload na sa Microsoft Store": v5.1.8 would not open older files (no
OtherFinishes section), fixed in v5.1.9; owner "upload mo na yan": the Store release is v5.1.9 (User and Admin). scripts/submit_store.py now deletes only an uncommitted draft that
the Store refuses with 409 InvalidState and creates a new submission; a submission in certification is still never cancelled
unless replace_pending_store_submission=true is asked for. Later versions again need a new explicit request.
Owner (20 November 2026, "yan na muna ang pinaka final upload sa microsoft store"): v5.1.9 is the last Store upload for now; do not
upload v5.1.10 or later until the owner asks again (GitHub pushes to the development branch are still fine).
Result 20 November 2026: v5.1.9 merged to main (GitHub release OK, run 228). Store run 44698831499 (build re-run once after a
flaky EarthworkFootingTests exit code): both uploads were refused. The pending drafts were created in Partner Center, not through
the API, so the API may not update or delete them ("Ingestion API can only update, delete, and commit submissions that are created
through the API"). The owner must delete the in-progress User and Admin submissions in Partner Center; then re-run the failed
Store jobs of run 44698831499. Nothing was changed in Partner Center.
Done 20 November 2026: the owner deleted both drafts; the failed Store jobs of run 44698831499 were re-run (attempt 3). New User
(1152921505702458279) and Admin (1152921505702458540) submissions were created and committed; Partner Center reported
"accepted for processing" (PreProcessing). Certification is not guaranteed. v5.1.10 and later need a new explicit request.
New owner request (21 November 2026, "Pakipush sa store"): publish v6.0.48 (User and Admin) the same way after its Windows CI passes
(merge the development branch to main for the GitHub release, then publish_microsoft_store=true; replace_pending_store_submission
stays false unless the owner asks). Later versions again need a new explicit request.
Done 21 November 2026: v6.0.48 merged to main (PR #13; GitHub release OK, run 44892757194). Store run 44892800305 (build job
re-run once after the flaky EarthworkFootingTests exit code; all 124 checks had passed): new User (1152921505702467105) and Admin
(1152921505702467242) submissions were created and committed; Partner Center reported "accepted for processing" (PreProcessing).
Certification is not guaranteed. v6.0.49 and later need a new explicit request.
Owner (21 November 2026, "Iupload sa apple store app"): upload the native iPhone app (DTCE Access Admin, ios/) to App Store
Connect: run the workflow on main with publish_ios=true (publish_microsoft_store stays false). This authorizes this upload only;
App Store review submission/release in App Store Connect and later iPhone uploads need a new explicit request. Pages stays off.
Done 21 November 2026: 1.1.0 was refused (version already approved, train closed), so the iPhone app is now 1.1.1 (PR #14).
Run 44913008843 (Windows build job re-run once after the EarthworkFootingTests exit code, all 124 checks passed): DTCE Access Admin
1.1.1 build 239.2 uploaded; App Store Connect processing VALID (available for TestFlight). Not submitted for App Store review.
Apple warned that from April 2027 iPhone apps must be built with the iOS 27 SDK (Xcode 27) or later.
Owner (23 November 2026, "Submit for apple review ang test flight"): submit the TestFlight build DTCE Access Admin 1.1.1 (build 239.2)
for App Store review. The workflow input submit_ios_review (default false; ios_review_version / ios_review_build) runs
ios/ci/app_store.py submit: it attaches the processed build to the App Store version with release MANUAL and submits it; it never
uploads or releases. Releasing after approval and later submissions need a new explicit request. Export compliance is answered only
with the input ios_exempt_encryption=true, which the owner chose for build 239.2 ("Set it as exempt for me": the app only signs and
verifies access codes - signatures, SHA-256, HMAC, Face ID - and encrypts no data).
Done 23 November 2026: run 45135053286 (development branch, submit_ios_review=true) set the export compliance (exempt), created App
Store version 1.1.1 (release MANUAL), attached build 239.2, set What's New "Bug fixes and performance improvements." (owner's choice)
and submitted it: state WAITING_FOR_REVIEW. Approval is not guaranteed; releasing after approval needs a new explicit request.
Owner (23 November 2026, "Update mo na lang sa test flight now"): upload a new iPhone build to TestFlight. It is 1.1.2 (1.1.1 is in
review; 1.1.2 adds Staircase Design, program 8192, to the access-code generator): merge the development branch to main (GitHub release
of the desktop app v7.1.24 follows automatically), then run the workflow on main with publish_ios=true (publish_microsoft_store stays
false). TestFlight only - App Store review of 1.1.2 and later uploads need a new explicit request.
New owner request (23 November 2026, "upload mo na din sa Microsoft Store yung bago"; chose "Replace v7.1.17"): publish v7.1.24 (User
and Admin) to the Microsoft Store after its Windows CI passes (PR #17 to main), with publish_microsoft_store=true and
replace_pending_store_submission=true for this release only - the v7.1.17 submissions still in certification are cancelled and
replaced. Later versions again need a new explicit request.
Result 23 November 2026: PR #17 merged to main (GitHub release v7.1.24 OK, run 45138604556). Upload run 45138610075 (first attempt):
- TestFlight: DTCE Access Admin 1.1.2 build 272.1 uploaded; Apple processing VALID. Not submitted for App Store review.
- Store Admin: no submission was pending; new submission 1152921505702480415 created and committed, "accepted for processing"
  (PreProcessing). Certification is not guaranteed.
- Store User: refused. An in-progress User submission 1152921505702479990 (PendingCommit) was created in Partner Center, not through
  the API, so the API may not update or delete it. The owner must delete that in-progress User submission in Partner Center; then
  re-run the failed store-user-upload job of run 45138610075. Nothing was changed in Partner Center.
New owner request (23 November 2026, "Pakipublish sa microsoft store"): publish v7.0.4 (User and Admin) the same way after its Windows
CI passes (merge the development branch to main for the GitHub release, then publish_microsoft_store=true;
replace_pending_store_submission stays false unless the owner asks). The Admin edition includes the PHIVOLCS active fault lines
(DPWH ArcGIS layer, copyright PHIVOLCS; no stated license); the owner was told this before asking. Later versions again need a new
explicit request.
Owner (23 November 2026, chose "Include: upload v7.0.5"): the Store release is v7.0.5 (v7.0.4 plus the Lintel Beam equal split and the
lintels in the Miscellaneous Structures rebar summary), the same way after its Windows CI passes.
Owner (23 November 2026, chose "Upload v7.1.0"): the Store release is v7.1.0 (v7.0.5 plus the project title in every window, the works
tabs in construction sequence, manual canopy schedules, rows estimated only with description / deduction, editable moulding quantity).
Owner (23 November 2026, "PAKI PUSH NA AFTER SA MICROSOFT STORE"; "bakit same pa din version no?"): the fixes added after v7.1.0 got
their own PATCH numbers (v7.1.1 entry ID prefixes, v7.1.2 shared settings / project kept, v7.1.3 go to an input error); the Store
release is v7.1.7 (v7.1.3, v7.1.4 no project selected on open / after closing, v7.1.5 POW workbook disposed-tabs crash fix,
v7.1.6 no default pipe size, v7.1.7 chosen pipe size kept), the same way after its Windows CI passes. Every later change gets its own version number, released or not.
Result 23 November 2026: v7.1.7 merged to main (PR #15); GitHub release run 45098961581 OK on attempt 2 (one re-run after the
EarthworkFootingTests exit code). Store run 45098975713 failed twice on the same exit code (all 124 checks passed) before any upload;
nothing was uploaded and nothing changed in Partner Center.
New owner request (23 November 2026, "yan na ang iupload mo sa microsoft store"): publish v7.1.15 (User and Admin) instead of v7.1.7,
the same way after its Windows CI passes (merge the development branch to main, then publish_microsoft_store=true;
replace_pending_store_submission stays false unless the owner asks). Later versions again need a new explicit request.
Owner (23 November 2026, chose "Upload v7.1.16" and added "kapag nagdouble click sa saved project information magclose na din yung
window"): the Store release is v7.1.17 (v7.1.15, v7.1.16 FY / ABC kept when opening older Bill of Quantities files, v7.1.17
double-click a saved project to use it and close), the same way after its Windows CI passes.
Done 23 November 2026: v7.1.17 merged to main (PR #16; GitHub release OK, run 45106720078, first attempt). Store run 45106734729
(first attempt, no re-run needed): new User (1152921505702478667) and Admin (1152921505702478466) submissions were created and
committed; Partner Center reported "accepted for processing" (PreProcessing). Certification is not guaranteed. v7.1.18 and later
need a new explicit request.
New owner request (23 November 2026, "upload to microsoft store"; chose "Replace pending"): publish v7.2.3 (User and Admin) after its
Windows CI passes (PR to main for the GitHub release, then publish_microsoft_store=true with replace_pending_store_submission=true for
this release only - a v7.1.24 submission still in certification is cancelled and replaced). The owner has not yet deleted the
Partner Center User draft 1152921505702479990, so the User upload may be refused again until it is deleted. Later versions again need a
new explicit request. Owner then: "deleted all submission" (Partner Center drafts deleted); "user app available or searchable in store":
the User upload sets visibility Public (STORE_VISIBILITY, User job only). The release is v7.2.4 (v7.2.3 plus the Windows tab fix).
Done 23 November 2026: v7.2.4 merged to main (PR #18; GitHub release v7.2.4-build-279). Store run 45191282769 (build re-run once after the
EarthworkFootingTests exit code -1073740771; all 124 checks had passed): new User (1152921505702483442; visibility Private -> Public) and
Admin (1152921505702482946; visibility kept Private) submissions were created and committed; Partner Center reported "accepted for
processing" (PreProcessing). Certification is not guaranteed. v7.2.5 and later need a new explicit request. The test runner fix for that exit
code (EarthworkFootingTests closes its windows and exits 0) is on the development branch for the next release.
New owner request (23 November 2026, "Upload sa store"; chose "Replace v7.2.4"): publish v7.2.5 (User and Admin) after its Windows CI passes
(PR to main, then publish_microsoft_store=true with replace_pending_store_submission=true for this release only - the v7.2.4 submissions
still pending are cancelled and replaced; User visibility stays Public). Later versions again need a new explicit request.
Done 23 November 2026: v7.2.5 merged to main (PR #19; GitHub release v7.2.5-build-282). Store run 45284210023 (first attempt, no re-run):
new User (1152921505702487129; visibility Public) and Admin (1152921505702487130; visibility Private) submissions were created and
committed; Partner Center reported "accepted for processing". Certification is not guaranteed. v7.2.6 and later need a new explicit request.
New owner request (24 November 2026, "push sa microsoft store"; chose "Replace v7.2.5"): publish v7.2.6 (User and Admin) after its Windows CI
passes (PR to main, then publish_microsoft_store=true with replace_pending_store_submission=true for this release only; User visibility
stays Public). Later versions again need a new explicit request.
Done 24 November 2026: v7.2.6 merged to main (PR #20; GitHub release OK, run 45303839797). Store run 45303878664 (first attempt): new User
(1152921505702487508; visibility Public) and Admin (1152921505702488152; visibility Private) submissions were created and committed;
Partner Center reported "accepted for processing" (PreProcessing). Certification is not guaranteed. v7.2.7 and later need a new explicit request.
New owner request (24 November 2026, "iupload sa store"; chose "Replace v7.2.6"): publish v7.2.13 (User and Admin) after its Windows CI passes
(PR to main, then publish_microsoft_store=true with replace_pending_store_submission=true for this release only; User visibility stays
Public). Later versions again need a new explicit request.
Result 24 November 2026: v7.2.13 merged to main (PR #21; GitHub release OK, run 45324634624). Its Store run 45324670010 was cancelled
before it started (nothing changed in Partner Center): every Store MSIX so far lacked tools/qpdf, so PDF export failed in the Store app.
Owner (24 November 2026, chose "Upload v7.2.14 instead"): publish v7.2.14 (PDF tools in the MSIX, Earthquake lateral force procedure,
geotechnical Mmax) instead, the same way (PR to main, then publish_microsoft_store=true with replace_pending_store_submission=true for
this release only). Later versions again need a new explicit request.
Result 24 November 2026: v7.2.14 merged to main (PR #22; GitHub release OK, run 45329154479). Store run 45329200717 failed in both upload jobs
before any upload: scripts/submit_store.py refused tools/qpdf/qpdf.exe ("Unexpected executable in package"). Nothing changed in Partner Center.
The check now allows that one file (development branch, v7.2.24); a Store retry needs main updated and the owner's go-ahead for the version.
Owner (24 November 2026, chose "Upload v7.2.24"): publish v7.2.24 (User and Admin) instead, after its Windows CI passes (PR to main, then
publish_microsoft_store=true with replace_pending_store_submission=true for this release only; User visibility stays Public). Later versions
again need a new explicit request.
Owner (24 November 2026, chose "Upload v7.2.25 instead"): the Store release is v7.2.25 (v7.2.24 plus separate WW / LW / SW STAAD wind types),
the same way (PR #23, then publish_microsoft_store=true with replace_pending_store_submission=true for this release only).
Owner (24 November 2026, chose "Upload v7.2.26"): the Store release is v7.2.26 (v7.2.25 plus TYPE n WIND ... names and the equation margin),
the same way. Later versions again need a new explicit request.
Owner (24 November 2026, chose "Upload v7.2.27"): the Store release is v7.2.27 (v7.2.26 plus STAAD lines kept to 72 columns), the same way.
Later versions again need a new explicit request.
Done 24 November 2026: v7.2.27 merged to main (PR #23; GitHub release OK, run 45339252942). Store run 45339292532 (first attempt): new User
(1152921505702490583; visibility Public) and Admin (1152921505702490204; visibility Private) submissions were created and committed, replacing
the pending v7.2.6 submissions; Partner Center reported "accepted for processing" (PreProcessing). Certification is not guaranteed.
v7.2.28 and later need a new explicit request.
New owner request (24 November 2026, "upload to microsoft office" = Microsoft Store; chose "Replace v7.2.27"): publish v7.2.32 (User and
Admin) after its Windows CI passes (PR to main, then publish_microsoft_store=true with replace_pending_store_submission=true for this release
only; User visibility stays Public). Later versions again need a new explicit request.
Owner (24 November 2026, "Hold it"): the v7.2.32 Store upload is on hold while the stair top bars are reviewed; PR #24 is not merged.
Owner (24 November 2026, "upload to microsof store"): the Store release is v7.2.34 (v7.2.32 plus the stair changes of v7.2.33 and the key-file
button row of v7.2.34), the same way (PR #24 to main, then publish_microsoft_store=true with replace_pending_store_submission=true for this
release only - the pending v7.2.27 submissions are replaced; User visibility stays Public). Later versions again need a new explicit request.
Done 24 November 2026: v7.2.34 merged to main (PR #24; GitHub release v7.2.34-build-306, run 45416912305). Store run 45416991179 (first
attempt): new User (1152921505702494633; visibility Public) and Admin (1152921505702494670; visibility Private) submissions were created and
committed, replacing the pending v7.2.27 submissions; Partner Center reported "accepted for processing" (PreProcessing). Certification is not
guaranteed. v7.2.35 and later need a new explicit request.
New owner request (25 November 2026, "publsih to store after nyan"; chose "Replace v7.2.34"): publish v7.2.37 (User and Admin) after its
Windows CI passes (PR to main, then publish_microsoft_store=true with replace_pending_store_submission=true for this release only; User
visibility stays Public). Later versions again need a new explicit request.
Owner (25 November 2026, chose "Upload v7.2.38"): the Store release is v7.2.38 (v7.2.37 plus the single-value Building inputs), the same way
(PR #25, then publish_microsoft_store=true with replace_pending_store_submission=true for this release only).
Owner (25 November 2026, chose "Upload v7.2.39"): the Store release is v7.2.39 (v7.2.38 plus C&C from spacings), the same way.
Done 25 November 2026: v7.2.39 merged to main (PR #25; GitHub release v7.2.39-build-311, run 45580077710). Store run 45580111705 (first
attempt): new User (1152921505702502448; visibility Public) and Admin (1152921505702502447; visibility Private) submissions were created and
committed, replacing the pending v7.2.34 submissions; Partner Center reported "accepted for processing". Certification is not guaranteed.
v7.2.40 / v8.0.0 and later need a new explicit request.
New owner request (26 November 2026, "PUBLISH TO MICROSOFT STORE"; then "before upload" add all spaces and the Dead Load sub tab): publish v10.0.6 (v10.0.0 plus the Live Load spaces unchecked by default, no separate building tab, and the national government office types) (User and Admin) after its Windows CI passes (PR to main for
the GitHub release, then publish_microsoft_store=true; owner chose "Replace v7.2.39": replace_pending_store_submission=true for this release only;
User visibility stays Public).
Done 26 November 2026: v10.0.6 merged to main (PR #26; GitHub release v10.0.6-build-325, run 45739164218). Store run 45739183832: new User
(1152921505702510795; visibility Public) and Admin (1152921505702510586; visibility Private) submissions were created and committed, replacing
the pending v7.2.39 submissions; Partner Center reported "accepted for processing" (PreProcessing). Certification is not guaranteed.
New owner request (26 November 2026, "publish"; chose "Replace v10.0.6 with v10.0.7"): publish v10.0.7 (button alignment) the same way after its
Windows CI passes (PR to main, then publish_microsoft_store=true with replace_pending_store_submission=true for this release only; User
visibility stays Public). Later versions again need a new explicit request.
Done 26 November 2026: v10.0.7 merged to main (PR #27; GitHub release v10.0.7-build-328). Store run 45745731189 (first attempt): new User
(1152921505702510936; visibility Public) and Admin (1152921505702511099; visibility Private) submissions were created and committed, replacing the
v10.0.6 submissions (the owner had cancelled them in Partner Center); Partner Center reported "accepted for processing". Certification is not
guaranteed. v10.0.8 and later need a new explicit request. Later versions again need a new explicit request.
New owner request (26 November 2026, "publish to microsoft store"): publish v10.0.18 (User and Admin) after its Windows CI passes (PR to main
for the GitHub release, then publish_microsoft_store=true). Asked whether to replace the pending v10.0.7 submissions, the owner gave no
preference, so replace_pending_store_submission stays false; User visibility stays Public. Later versions again need a new explicit request.
Done 26 November 2026: v10.0.18 merged to main (PR #28, Windows CI green; GitHub release OK). Store run 45787825007 (first attempt,
replace_pending_store_submission=false): new User (1152921505702514078; visibility Public) and Admin (1152921505702514277; visibility Private)
submissions were created and committed; Partner Center reported "accepted for processing" (PreProcessing). Certification is not guaranteed.
v10.0.19 and later need a new explicit request.
New owner request (26 November 2026, "publish to microsoft store"): publish v10.0.24 (User and Admin) after its Windows CI passes (PR to main for
the GitHub release, then publish_microsoft_store=true; nothing is pending in the Store, so replace_pending_store_submission stays false; User
visibility stays Public). Later versions again need a new explicit request.
Result 26 November 2026: PR #29 merged after three CI fixes to the new tests (Windows runner: shown Bill of Quantities window crashed in its tooltip;
exit with open windows); Store run 45834493117 started for v10.0.24.
Done 26 November 2026: Store run 45834493117 (first attempt, replace_pending_store_submission=false): new User (1152921505702517009;
visibility Public) and Admin (1152921505702516812; visibility Private) submissions were created and committed; Partner Center reported
"accepted for processing" (PreProcessing). Certification is not guaranteed. v10.0.25 and later need a new explicit request.
Checked 27 November 2026 (read-only store_status, run 45915650622): v10.0.24 is Published for User (1152921505702517009) and Admin
(1152921505702516812); nothing pending; the public catalog lists DTCE.DTCEToolKit_10.0.24.0 / DTCE.DTCEToolKit-Admin_10.0.24.0.
New owner request (27 November 2026, "publish mo na den sa staadf" = Microsoft Store, confirmed): publish v10.0.33 (User and Admin) after its
Windows CI passes (PR to main for the GitHub release, then publish_microsoft_store=true; nothing is pending, so replace_pending_store_submission
stays false; User visibility stays Public). Later versions again need a new explicit request.
Checked 26 November 2026 (read-only store_status job, run 45825954870): v10.0.18 is Published for User and Admin; no submission pending;
the public catalog lists DTCE.DTCEToolKit_10.0.18.0 / DTCE.DTCEToolKit-Admin_10.0.18.0.
Update check (owner, 26 November 2026: "di namang gumagana yung pagcheck ng updates"; "ayaw ko ng check for updates, gusto ko automatic na",
v10.0.24): automatic only, no button. StoreUpdateQuery reads the published version from the public Store catalog
(displaycatalog.mp.microsoft.com/v7.0/products/<id>, PackageFullName "<identity>_x.y.z.0_") and compares it with Engine.Version; the
WinRT Store API is only a fallback. A newer version opens the Store page once per version and shows the update notice. The workflow input
store_status (default false) is read-only: it prints the published / pending submissions and the catalog version, changing nothing.

## Shared settings and the open project (23 November 2026)

Owner: "kahit sana app from store or downloaded lang exe file sana naoopen pa din yung saved settings and project". User settings and
saved projects live in Documents\DTCE Tool Kit (src/SharedData.cs), shared by the Store (MSIX, AppData virtualized) and the .exe; old
AppData / Store-private copies are brought over once. Owner (23 November 2026, "kapag bagong open or naka close huwag mag select ng
project information"): no project is selected when the app opens or after the Bill of Quantities closes (v7.1.4); projects are
picked from the saved list (shared) or loaded with a Bill of Quantities file. Owner (26 November 2026, "sa start na lang magoopen ng saved file tapos doon na din magsasave. Nakasave na din doon yung project", v10.0.20): the Workspaces window has "Open saved project" (LauncherForm.OpenSavedProject): the .boq.xml project information becomes the current project and the Bill of Quantities opens the file (ProgramOfWorksForm.OpenPath), so Save writes back to it. Owner ("Kapag may click ako ng project sa project information magopen na din lahat ng files ko na sinave ko doon sa kahit saan workspace", v10.0.21): src/ProjectFiles.cs keeps the last saved / opened file per workspace (boq, pow, design:loads, design:<type>) under the current project; using a project in Project Information (double-click / Save) opens them all (LauncherForm.OpenProjectFiles). Owner ("1 file na lang ang isave"; "or open project, or from list kapag nawala sa default saved list", v10.0.22): one project file *.dtce (src/ProjectPackage.cs: <DtceProject> with ProjectInformation and a base64 Part per workspace); workspaces save to working copies (ProjectPackage.PathFor when they have no path) and every save repacks the file; opened from Open saved project, the saved list, or Project Information "Open project file…" (puts it back on the list). v10.0.23 (owner: "gagana pa din yung mga lumang files ... ayusin mo yung ibang tabs"): the Bar Bending Schedule (bbs, Save / Ctrl+S, "Save as" separate) and Splice, Development & Hooks (lengths, Ctrl+S / Save in app) are in the project file too; older loose files still open and save back to themselves.

## The app's quantities go to BACKUP (23 November 2026)

Owner: "yung quantities na nasa app ang mapupunta sa pow backup" (not the other way around). Quantities already in the linked
BACKUP (.xlsm) are never shown or used as app quantities (BackupBillItem.WorkbookQuantity is not a fallback); the app writes its
computed / typed quantities to the mapped BACKUP items. BACKUP!BC of items without an app quantity is emptied, typed or formula, also
while rows have input issues (owner: "pati yung mga may type values sa excel ioverride niya din"; "bakit di pa din naalis yung mga
quantities sa xlsm yung wala sa boq", v7.1.15); the built-in POW does the same and keeps the template formula aside for New / Open. Progress Photographs follow the Bill of
Quantities Contract Duration (owner: "progress photo graph naka link sa contract duration ng boq"): months from the start to the end
date as the owner's BACKUP!N64 (DATEDIF whole months + remaining days / days of that month, 2 decimals); typed app value wins; the
BACKUP value is kept only while there is no contract duration. The Contract Duration start date follows FRONT PAGE!CB7 by default
(MAX(date prepared + 90 days, or 75 when ABC >= PHP 50 M; first Monday on or after May 1 of the FY)) (v7.1.13, owner: "yung sa
formula sa excel na ginawa ko"). FY and ABC are Project Information fields (v7.1.14, owner: "sa project setting maglagay ng year doon
at ABC").
Sheets the app did not make are still not overwritten (v7.1.8 reverted).

## Staircase Design page (23 November 2026)

Owner: "pagsamasamahin yung stair type at yung staircase design" / "stair type and geometry at model": the stair type and geometry
inputs and the four Model views (section, 3D, plan, load / shear / moment) are one tab, "Stair type, geometry and model" (v7.1.12).
Owner ("i drawing ng tama yung reinforcement ng stairs as to standard"; "di pa naka drawn to scale", v7.1.26): the section is drawn to
scale (bars at their diameters, graphic scale bar), chairs stand square to the slab between the mats, crossing bars at re-entrant
corners run ld past the crossing, and the shear diagram is positive above the axis.
Owner (24 November 2026, v7.2.8): top bars always full length (no L/4 choice); flight 2 of a dog-legged stair (flight 3 of open-well)
drawn mirrored; inputs shown follow the stair type; 3D shows the support beams; step bars end in 90° hooks tied to the top mat.
Owner ("reinforcement ng steps kako", v7.1.27): "chair bars" of a stair means the step bars bent to each riser and tread (legs lapped
onto the waist top bars, nosing bar in the bend), not support chairs; do not draw support chairs in the section.
Owner (24 November 2026, v7.2.33, supersedes the v7.2.8 "top bars always full length"): top bars are a choice - "At the supports and
kinks, L/4 (Philippine practice)" is the default, "Full length" optional. Each flight may have its own width (Width2 / Width3, 0 = flight 1);
landing beams at a landing end are monolithic with the slab and anchor the bars; wall supports are drawn as walls (WallT). Chair bars follow
the owner's detail: riser leg down through the waist and tread leg back into the waist, both bent along the bottom bars and lapped 300 mm
(30db), nosing bar in the bend - not hooked into the waist top mat.

## Load Combinations sheet (25 November 2026)

Owner: "create load combinations for superstructure including orthogonal effect for staad input" - its own design sheet "Load Combinations
(NSCP 2015)" (program 16384, v8.0.0; src/StaadCombinations.cs, src/LoadCombinationsWindow.cs). Primary loads are pasted as in STAAD and
the combinations use the same titles (DL, LL1, LL2, LLR, WL+X, EQ+X ...); numbered as the owner marked the NSCP pages (v8.0.3): 1000 strength
design 203-1..7, 2000 alternate basic ASD 203-13..18, 3000 special seismic 203-19/20 (Em = Omega0 Eh), 4000 basic ASD 203-8..12 (off by
default), 5000 wind drift, 6000 seismic drift, each labelled with its equation; Ev only in strength design (208-18: zero for ASD unless chosen); f1 per live case; seismic cases by the
owner's STAAD notes - A regular / symmetrical, B regular / unsymmetrical (accidental torsion), C irregular / unsymmetrical (orthogonal
1.0 + 0.3 as REPEAT LOAD cases, Load Type Seismic, the EQ primaries Load Type None). Owner ("diba may basic at alternative tapos may additional loads sa eq?"): the
alternative basic ASD and special seismic sets (v8.0.1), regrouped in v8.0.2.
Owner ("How do you compute redundancy or reliability factor"; "Yes with automatic staad computation"): the sheet has a "Redundancy factor ρ" tab
(src/RedundancyFactor.cs, src/LoadCombinationsRho.cs): the pasted .std and the EQ member end forces give the story shears and r_i (bay of two
columns, 70 % interior; brace horizontal component; dual 80 %; walls typed), A_B from the base joints, ρ = 2 - 6.1/(r_max √A_B) per direction
with the limits; "Use ρ" fills Factors; reported as its own section (v8.0.4). Owner (26 November 2026, "automatic na sana di na ako maglalagay ng primary loads"; "nauuna yung Earthquake
loads"; "perform analysis change every eq primary case", v10.0.9): "Automatic primary loads" (default on) builds the primaries from the structure
class, EQ first (A 4, B 8 with +E / -E, C 4 Load Type None), then DL, LL1.., LLR, WL; the STAAD output then starts with the primary section
(UBC / IBC LOAD command, ACC +-0.05 for B, PERFORM ANALYSIS / CHANGE after each EQ case as a choice). Older sheets keep their pasted list. v10.0.10 (owner: "bago naman staad ko"): PERFORM ANALYSIS / CHANGE after each EQ case defaults
off (older STAAD only); titles with + / - inside are bracketed (1.3(EQ+X+E)). v10.0.11: case B notes natural torsion (rigid diaphragms, CM / CR offset) and
ACC = 0.05 Ax (Ax input 1.0-3.0); seismic drift 1.0 Eh by default, "D + 0.5L + 1.0 Eh" choice for P-Delta (owner: "sure ka na eq loads lang?"). v10.0.12 (owner: "automatic computation of torsional amplification factor ... 1 muna ...
after run papalitan ... include lahat sa report"): "Torsional amplification Ax" tab (src/TorsionAmplification.cs) - Ax = 1.0 until the STAAD joint
displacements are pasted, then Ax = (dmax / 1.2 davg)^2 (1.0-3.0) per EQ case / level from the edge displacements replaces it (ACC = 0.05 Ax);
reported with the table, and the basis table lists all the new settings. v10.0.13: DEFINE ENVELOPE per group at the end (strength / special
STRENGTH, ASD / drift SERVICEABILITY) and an analysis + print block (CG, DIAPHRAGM CR, STORY STIFFNESS, STORY DRIFT of the drift cases, EQ
displacements / member forces, reactions) with LOAD LISTs (owner: "add define envelope"; "include also the reports needed to print"). v10.0.14: "Structural irregularities" tab (Tables 208-9 / 208-10 from the
STAAD data + short inputs, suggested structure class; src/StructuralIrregularity.cs); options on a "STAAD output" tab as ordinary fields (owner:
"apakagulo naman nito"); Ca, I, Omega0 linked from the Earthquake Load sheet (EarthquakeLink, "EqLinked" default on); Design Loads saved / opened as
one file <DesignLoads> with the five sheets (src/DesignLoadsFile.cs; owner: "isang xml sheet na lang yung design loads"). v10.0.15: Live Loads marks each space LL1 (f1 0.5) or
LL2 (f1 1.0) and reports them apart; Load Combinations' f1 = 1.0 default is LL2 (owner: "ilagay if LL1 or LL2 ... ipaghiwalay"). v10.0.16 (owner: "link mo lahat ng kailangan inputs sa isat isa"): DesignLoadsLink (Links\<name>-link.xml) - Load Combinations sends the computed ρ and the structure class to Earthquake (Rho / Regularity, "LcLinked"), Earthquake Levels fill RegWeights, Live Loads checked spaces set LiveCases / F1One ("LlLinked"); linked fields are locked. v10.0.17 (owner: "yung lat and long, bat paulit ulit pa din?"): Latitude / Longitude are one shared site for every design sheet that has them (DesignSheetForm HookSite / PublishSite, Links\site-link.xml): typed once, filled in Wind and Earthquake. v10.0.18 (owner: "DI GUMAGANA ANG CTRL S"): design sheets take Ctrl+S (same file after the first save; Design Loads share one path), Ctrl+Shift+S, Ctrl+O (DesignSheetForm.ProcessCmdKey). Owner ("DIBA DAPAT ITO AY NAKA BASE NA SA DIRECTION NG BUILDING"): the Wind exposure / Kzt pages list the building-face directions first, titled with their load case and face (WindLoadCasesReport.FaceLabels); other directions are marked reference. v10.0.19 (owner: "automatic na ganito WIND LOAD X 1 TYPE 1 YR 0 12.6"; "kapag may paloob sa gitna e di naloload"): with "WindLinked" the automatic WL+X / -X / +Z / -Z cases carry the WIND LOAD lines of the Wind sheet TYPEs (WindStaadInput.FaceTypes, link wind-staad; leeward and side walls with factor -1) and MEMBER LOADs on recessed faces found from the pasted model (src/WindEdgeLoads.cs: outline per level from the beams, faces shielded along their normal). v10.0.25: shared parameters (DesignSheetForm.Share / SharedParameters, Links\shared-link.xml: occupancy, system) between Wind and Earthquake; Load Combinations has one Settings tab in four groups (ShowWhen hides linked fields); the Primary loads tab has the STAAD input box for the wind loads (key StaadInput, pasted before the run; wind recess loads no longer need the rho tab); Structural irregularities has a Summary and one sub tab per irregularity, re-entrant corners / floor area / columns on beams from the STAAD model (StructuralIrregularity.ReEntrant / DiscontinuedColumns, WindEdgeLoads.FloorPlans), storeys by real floor levels; the class is worded as the seismic load cases of the combinations (owner: "for load combo lang naman yun"). Owner (26 November 2026, "lahat ng pakita ko sample lang yun pati yung sa mga truss support, alisin mo iyon", v10.0.26): what the owner shows is sample material - do not build it into the app as presets or defaults (the Truss Support "Example A / B (from a sample drawing)" buttons were removed; the cases stay as test data only). v10.0.27: DesignLoadsLink encodes key names (XmlConvert.EncodeLocalName: "+X") and keeps line breaks (U+2028) - before, the wind-staad link never saved; the Wind sheet STAAD commands end with the wind load cases to paste (link wind-cases from Load Combinations: LOAD numbers + recess member loads); DesignSheetForm keeps each input panel in the order the controls were added (KeepOrder). v10.0.28 (owner: "2 inputs na lang joint from table and beams from table. or staad command file or .std"): the Primary loads tab has two boxes, STAAD Nodes table (key StaadInput, also the whole .std) and Beams table (StaadBeams); RedundancyFactor.ReadTables builds the model; LoadCombinationsForm.InputModel feeds the wind cases and the irregularity checks. v10.0.29 (owner: "bakit member load? dapat windload pa din"): recessed walls get WIND LOAD lines limited by XR / ZR / YR around each wall (WindEdgeLoads.RecessLoads; WW type windward factor 1, LW / SW types factor -1), never MEMBER LOADs. v10.0.30 (owner: "isama mo din yung figure"; "colored lang yung text at arrows ... lagyan na lang ng legend"; "as much possible sa mga cases na hindi pa natin nadidiscover"; "hindi same ang outline ng taas at baba"): src/WindCaseFigure.cs draws each wind case (Load Combinations tab "Wind load cases" + report section) - legend rows, R tags, no boxes or leader lines; WindEdgeLoads.Plans finds the outline by flood fill (ray test fallback on gaps), faces carry Below / Index / Covered, RecessLines makes one line per run of levels (storey = outline at its top level; base level skipped); tests/WindEdgeShapesTests.cs holds the shape cases. v10.0.31 (owner: "do what the code says"; "depende ... if anong procedure"): the directional procedure Part 1 (the Wind sheet) needs the design wind load cases of NSCP Fig. 207B.4-8 for every building - Load Combinations "WindAllCases" (default on) adds Case 2 WL2±X±T, Case 3 WL3±X±Z, Case 4 WL4±X±Z±T (StaadCombinations.AutoPrimary / WindKey; WindEdgeLoads.CaseTexts allCases: windward / leeward only, scaled 0.75 / 0.563, torsion MT = f (PW+PL) B h 0.15B per level as a JOINT LOAD FX couple, WindEdgeLoads.Torsion); "WindTorsionExempt" (default off) leaves Cases 2 and 4 out for buildings the code exempts. The envelope procedure is not generated. v10.0.32 (owner: "pano ang loading neto?"): WindDesignCasesFigure (src/WindCaseFigure.cs) draws Cases 1-4 on the plan with the torsion couple, first on the Wind load cases tab and in the report; couple joints sit at the middle of the longest outer face at each Z edge. v10.0.33 (owner: "add the roof, determine also the overhangs"; "icheck kung ano ang tama at hinihingi ng code"): Case 1 carries the roof (Fig. 207B.4-1, net of GCpi): WL±X / WL±Z with the uplift (Cp branch 1, +GCpi) and their twins WL1B±X / ±Z (branch 2, -GCpi); side walls stay in Case 1 (207B.4-1 surfaces), Cases 2-4 walls only (projected area of Fig. 207B.4-8). WindFaceTypes carries the roof (Pack field 8; WindStaadInput.Roof); WindEdgeLoads.RoofText loads slab roofs by FLOOR LOAD per roof cell and truss top chords by UNI Y x spacing (TrussRoof), overhangs = edges without columns / chord parts beyond the columns with external top pressure and the windward underside 0.8 qh G (207B.4.4). v10.0.34: WL1B cases only with the STAAD model (they were empty without it: "bat walang laman ito"); titles name the case ("WL+X CASE 1", "WL+X CASE 1 -GCPI", "WL+X CASE 2 +T", "WL+X+Z CASE 3", "WL+X+Z CASE 4 +T"; StaadCombinations.WindKey reads both forms) with a comment line per case; the Wind sheet STAAD Wind Input is DEFINE WIND LOAD only (owner: "huwag ilagay doon yung wind load cases"). Also v10.0.34: headerless STAAD tables are read (RedundancyFactor.ReadTables fallback); roofs at every level (WindEdgeLoads.Plans allLevels, Levels minShare 0; roof = cell not covered by any higher level); the "Wind load cases" tab and report pages live in the Wind Load sheet (src/WindLoadCasesTab.cs) fed by the link wind-model from Load Combinations (nodes, beams, all, exempt, titles). Automatic (owner: "automatic na dapat itong mga ito ... as per nscp 2015 code"): AllWindCases() is always true, the Wind sheet lines are always used, TorsionExempt() = one storey with h <= 9.1 m from the model (ASCE 7-10 App. D D1.1; other D1.1 groups not detectable, so torsion stays); the WindAllCases / WindTorsionExempt / WindLinked toggles stay hidden for older sheets. One model (owner: "bat kailangan kong magpaste ng multiple std file?"): RhoTexts falls back to PrimaryModelText() (the Primary loads .std or the tables written as STAAD input with the primary LOAD lines); the rho STAAD input box is hidden unless an older sheet filled it. v10.0.35 (owner: "yung report sa design loads minsanan na lang"; "calculation report sa tabi ng load combo, nandon lahat ng design loads"): Design Loads sub tab "Calculation report" (LauncherForm.ShowLoadsReport / BuildLoadsReport, src/DesignLoadsReport.cs): contents + the five sheets' reports (DesignSheetForm.ReportForBundle), CalculationDocument.PageTitles keeps each page's sheet title; combination titles use the short wind names (StaadCombinations.Short). v10.0.36: tab colour by depth has a fourth level (DesktopPalette.Tab / ActiveTab depth >= 3, owner: "ibang color yung pang apat na sub tab"); TopTabLayout.AttachCommands keeps a sheet's command bar right above its own tab control (owner: "yung mga button na common per group tabbed ilipat sa taas nila"), not in the innermost page. v10.0.37 (owner: "naka align na ba ito sa building? bakit naka N, NE?"; "pati sa kzt"): WindDesignForm.RealignTerrain reloads the terrain by itself when terrain.Offset differs from TerrainOffset() (ridge azimuth mod 45), so exposure / Kzt / profiles follow the building faces. v10.0.38 (owner: "anlalaki masyado ng spacing"): CalculationDocument.Table sizes the header and rows to their text (capped at the given row height) and returns the table bottom; the Wind report pages chain their parts after tables. v10.0.39 (owner: "isa isahin mo based din kung ilan wind load cases"): one figure per wind load case (src/WindLoadCaseFigure.cs, drawn from the case's own STAAD text: arrows with the case factors, R tags, torsion couple at its joints, every value in the legend, wrapped title); the Wind load cases tab / report use them (two per report page); WindDesignCasesFigure is no longer shown. v10.0.40 (owner: "e irregular yung shape ng building"): the Case 2 / 4 torsion of each level is spread over the outline joints (TorsionLevel.Edge: F = MT r / sum r2, FX and FZ, no net force; two-joint couple only below three joints), so it works for irregular plans with or without a FLOOR DIAPHRAGM. v10.0.41 (owner: "di nagtutugma yung LW at SW"): STAAD WIND LOAD signs - "X f" = pressure on the near side, "-X f" = suction on the far side, the factor sign = load direction (STAAD Technical Reference). Every WIND LOAD line comes from WindEdgeLoads.Spec(normal, suction, magnitude): toward +X WW "X 1", LW "-X 1", SW "-Z 1" / "-Z -1"; toward -X WW "X -1", LW "-X -1". The earlier "factor -1" leeward / side-wall lines (v10.0.19-v10.0.40) were wrong. v10.0.42 (owner: "pero yung kzt di pa din naka align sa faces ng building"): RealignTerrain waits for the end of Restore (AfterRestore starts it), so saved sheets with compass-sampled terrain are reloaded from the faces; the report notes a terrain that is not aligned yet (WindAuditPages.Add alignNote).

## Save project (27 November 2026)

Owner: "mas ayusin yung buttons ... magoopen ako ng multiple design loads pow etc tapos pano isasave sa isang file na lang". src/ProjectSave.cs: every workspace implements IProjectWorkspace (ProjectKind, SaveToProject, ProjectDirty) and registers with ProjectSave.Track; "Save project" (Workspaces window, each workspace, Ctrl+S) saves all open workspaces into the one *.dtce (ProjectPackage), separate-file saves live in each workspace's menu (Ctrl+Shift+S); closing the Workspaces window asks to save unsaved workspaces (ProjectSave.ConfirmClose). Without complete Project Information, Ctrl+S saves the workspace alone as before. Add new workspaces to this interface. Owner ("ikaw na bahala sa redundancy basta paste ko na lang yung required",
v8.0.7): the owner only pastes - the boxes are recognised either way (.ANL echo too), EQ cases / directions come from the input's LOAD cases,
the lateral system is automatic (braces = floor-to-floor inclined members, rafters excluded), and the computed ρ goes into Factors by itself.
Owner (26 November 2026, "Put Windload and Earthquake in a new main tab Design Loads including Load Combinations as sub tab"): the Design Sheets
window has a main tab "Design Loads" with the sub tabs Wind Load, Earthquake Load & RSA Curve Builder and Load Combinations (src/Launcher.cs
loadTabs / LoadSheets) (v8.0.5). Owner ("Design Loads isang access code", v8.0.6): Design Loads is one grant - ProgramAccess.DesignLoads =
WindDesign | EarthquakeRsa | LoadCombinations; any of the three bits opens all three (Allows), and the desktop generator lists one "Design Loads"
choice (ProgramAccess.Choices / ChoiceNames). The iPhone app and web generator still list the three separately (unchanged; each opens all three).
Owner (26 November 2026, "include Live Loads ... Government Buildings all spaces and Rooms ... summarize them per space. based on NSCP 2015";
"INCLUDE SEARCH BAR"): new design sheet "Live Loads (NSCP 2015)" (v9.0.0), first sub tab of Design Loads, under the Design Loads grant (sheet
type "Live-Loads" -> LoadCombinations bit; no new program). src/NscpLiveLoads.cs holds Table 205-1 / 205-3 and the spaces of 14 government
building types (row id per space); src/LiveLoadsWindow.cs shows them per space with f1 / reduction and search bars. Values entered without the
code text at hand: correct them from the owner's NSCP pages when given. v9.0.1: Table 205-1 as printed (owner's photos of pp. 2-17 / 2-18,
23 categories, notes 1-9); "In report" check boxes choose the spaces in the report (owner: "ADD CHECK ARROW TO BE INCLUDED IN THE REPORT");
Table 205-3 still not compared with the printed page. Owner ("LAHAT NG PWEDENG SPACES OR ROOMS ILAGAY MO SA LIVE LOAD"; "ANOTHER SUB TAB FOR DEAD LOAD, DEAD
LOAD FLOOR LOAD", v10.0.0): Live Loads has 29 building types incl. "Common rooms (any building)"; new design sheet "Dead Loads (NSCP 2015)"
(src/DeadLoadsWindow.cs, sheet type "Dead-Loads", Design Loads grant) laid out like the owner's Excel: floor components x floor types with
total and STAAD input without the slab, roof top chord / bottom chord / purlin with the purlin weight, wall line loads (height x wall kPa),
elevator with impact. v10.0.1: Live Load spaces start unchecked, the owner checks them ("ako ang mag check"); building type and partitions sit on the
per-space page (no "Building and spaces" tab). v10.0.2: "National Government Office" building type (owner: "bat walang national government offices?"). v10.0.3: the national agencies (owner: "any type of National Government Office"), each the office core + its own rooms. v10.0.4: janitor's closet / utility room 2.4 kPa as the occupancy served (owner: "sure ka ... 6kpa?"). v10.0.5: Dead Loads Elevator tab of its own, Add / Delete buttons on every table (owner: "hiwalay mo naman yung Elevator ..."). v10.0.6: Live Loads is one sorted list of spaces, not grouped by building (owner: "huwag mo na igroup per building").
Owner (26 November 2026, "paki check yung alignment ng buttons, pati na rin sa iba, always laging ganyan ang problem"): toolbar rows are
centred on one line app-wide (src/BarAlign.cs via PowTheme.Apply, v10.0.7); build new bars from plain controls so it applies. Live Loads (v10.0.8, owner: duplicates "Bids and Awards ..." / "Catwalks"
/ "Conference rooms" / "Corridors above ground floor"; "paki check na din po lahat"): the same room is one row with one name (NscpLiveLoads.SameRoom;
add new rooms there when a similar one exists); the occupancy is added only where one name stands on different Table 205-1 rows. Report pages: the page number ends at the right end of the footer line in every report (CalculationDocument).

## Door hardware (23 November 2026)

Owner: hinges are "Ball Bearing Stainless Door Hinge" in 2 pcs/pack, sizes 3.0x3.0x2.5mm, 3.5x3.5x2.5mm, 4.0x3.0x3.0mm, 4.0x4.0x3.0mm
(BOQ lines in packs per size; Hinge size column Automatic or chosen); double-leaf doors get 1 leverset, one 4" Chrome Plated Chain Bolt
and one 4" Chrome Plated Foot Bolt; optional Deadbolt (Double Cylinder / Single Cylinder with Thumbturn, Satin Stainless Steel); leaves of
different widths are typed in Leaf widths (e.g. 0.90+0.30) and each leaf gets its own hinges (src/DoorHardware.cs, v7.1.18). All go to
1004(2) Finishing Hardware.

## ACI beam schedule cases (24 November 2026)

Owner: "lahat ng pwedeng mangyari sa schedule ilagay mo". The ACI beam schedule (src/AciBeamSchedule.cs) has a G column (added bottom
bars at the supports; not a letter of ACI MNL-66 BM-1_1 - say so) and, besides TYPE 1, a placing diagram for G and one for cantilevers,
drawn only when the schedule has them. The hidden row data that the BBS / BOQ read back stays unchanged (v7.2.35).

## Dropdowns, truss schedules and offcut sources (23 November 2026)

Owner: dropdown cells must not change under the arrow keys or the mouse wheel while the list is closed (src/ComboKeyGuard.cs, every grid
styled by GridAppearance; v7.1.19). Angle Bars has "Truss schedules": truss types with members; their Angle Bars rows are generated and
locked (src/TrussSchedules.cs, v7.1.19). Detailed / Extensive BOQ reports show where reused offcuts come from for rebar, 6 m steel stock
and railings (src/OffcutReuseReport.cs, v7.1.20).

## Complete rows only (23 November 2026)

Owner: "naeestimate pa din kahit di kumpleto"; "pati yung sa deduction dapat may input". A Bill of Quantities row with a blank
Description / location, or a blank deduction where the deduction is used, is not estimated (src/RequiredDescription.cs); 0 is typed
when there is nothing to deduct.

## Program of Works name (20 November 2026)

Owner: "palitan yung Program of Works ng Bill of Quantities". The workspace and every user-visible "Program of Works" text are named
Bill of Quantities. Internal class names (ProgramOfWorksForm), the .pow.xml file format and the linked workbook sheet names stay.

## POW workbook workspace (20 November 2026)

Owner: the DPWH POW/ABC workbook (V6.02 .xlsm) is reproduced in the app ("buong worksheet sa app, kamukha niyan mismo, para iprint
na lang thru the app"), keeping every formula and where labor, equipment and materials come from, as a separate workspace that needs
no password or access code. src/Wb*.cs read and recalculate the workbook; PowWorkbookForm shows and prints it.
Owner (20 November 2026, "independent na siya sa .xlsm"; "saka lang mapapalitan kapag binago ko, masasave na parang xml"): the
workbook is built in as src/PowTemplate.xlsx (slim copy of the owner's V6.02 Piggatan workbook made for the app: no macros/printer
settings; embedded resource "PowTemplate.xlsx" in every build script). Its values stay until the owner types new ones; typed values
are saved as POW project files *.pow.xml (root <PowWorkbook>). Do not commit the original .xlsm. BACKUP!BC follows the open Bill
of Quantities workspace (src/BoqPowLink.cs; owner: "yung backup sa pow nakalink sa boq workspace"). Bill of Quantities files are now
*.boq.xml (root <ProgramOfWorks>) and older *.pow.xml Bill of Quantities files must keep opening. Next: Project Information to
FRONT PAGE.

## Version numbers (18 November 2026)

Owner: use MAJOR.MINOR.PATCH properly. New features or noticeable behaviour changes raise MINOR and reset PATCH
(1.5.0 -> 1.6.0); bug-fix-only builds raise PATCH (1.5.0 -> 1.5.1); MAJOR only for incompatible changes. The next
build after v1.4.135 is v1.5.0. Keep src/Program.cs Version, src/AssemblyInfo.cs (x.y.z.0) and docs/TEST-BUILD-*.txt in step.
Owner (19 November 2026, "Bakit di nasusunod ang version no naming major.minor.bug"; chose "Fixes as PATCH only"): keep the
numbers already given (v1.48.0). A build that only fixes, adjusts or extends an existing workspace or design sheet raises PATCH
(1.48.0 -> 1.48.1); MINOR goes up only when a whole new workspace or design sheet is added (then PATCH resets).
Owner (20 November 2026, "hindi ba major yung bagong design sheet?"; chose v2.0.0): a whole new design sheet or workspace now
raises MAJOR (then MINOR and PATCH reset, e.g. 5.0.19 -> 6.0.0). Fixes, adjustments and extensions of an existing workspace or
design sheet still raise PATCH (5.0.19 -> 5.0.20); MINOR is for other new features. Owner ("icheck mo din yung mga previous para
marenumber sila"): versions since v1.44.0 were renumbered by this rule (v1.44 -> 2.0, v1.45-1.47 -> 2.1-2.3, v1.48 -> 3.0,
v1.49 -> 4.0, v1.50.x -> 5.0.x); v1.50.19 is v5.0.19. Earlier versions and published GitHub tags keep their numbers.

## Linked Excel workbook and version names (19 November 2026)

Owner: "hindi isasave ng app, manual save ang excel". The Program of Works writes quantities into the open linked
workbook but never saves it; the owner saves it in Excel (the status bar and the close message say so). Only creating a
new workbook file at a chosen path and the explicit design-report export still write the file. GitHub releases are named
after the app version they carry (tag v<version>-build-<run>). The Store package version is the app version (x.y.z.0);
the Store still shows v1.5.0 because no later upload was accepted - a newer Store upload needs a new explicit request.
Macros: a workbook the app opens for a visible link follows the Excel Trust Center (AutomationSecurity by UI, events on,
Excel shown first); never force macros off for the owner's linked workbook.
BOQ mapping: the mapping saved in the Program of Works wins over the linked workbook's names and suggestions; linking or
refreshing BACKUP must never erase it (items are followed by item no., description and unit).
Part A / B (owner, 19 November 2026, superseding "Pareho": "iallow na lang maginput ng qty sa boq ng app for part A and B"): the
owner types Part A / B quantities in the app's Bill of Quantities and they are copied to the same BACKUP item; the app does not
fill them (ProgramOfWorksForm.FillGeneralRequirements stays false).
Workspaces (owner, 19 November 2026): each workspace (Formatters, Program of Works, Design Sheets, Project Information, Access
Code Generator) opens in its own window; the Workspaces window stays open.

## Truss Connections, plate reuse and view-only apps (23 November 2026)

Owner: Truss Connections has two main tabs, Weld design and Bolt design, each with Member and demand / detail / Block shear / STAAD
forces / Report sub tabs (shared inputs move to the open design; the open tab sets weld or bolts). Interactive bolt layout
(src/TrussBoltDrawing.cs); slotted HSS / pipe weld detail; double-angle back-to-back spacing = gusset thickness; block shear computed
automatically (bolts: member and gusset paths; welded angles: gusset around the welds); illustrated report (src/TrussConnectionReport.cs)
- owner: "mas magagandang report at detalyado as may diagrams as much as possible ... maganda sa mata" applies to reports generally
(v7.1.28). Steel plate sheets are marked PL<t>-<n> and their shared use is shown in the grid and the BOQ reports (v7.1.29). User edition:
apps without access open view only (src/ViewOnly.cs; tabs and scrolling, no input or commands, "Enter access code" banner) (v7.2.0).
Owner (24 November 2026, "pwede nila maaccess yung app pero di nila pwede mag input, mag request na lang sila ng code sa workspace"): the
User edition never asks for the access code at start or when an app opens; without a code everything is view only, and the code is
requested / entered from the "Access Code" card of the Workspaces window (or the banner) (v7.2.6).

## Complete reports (23 November 2026)

Owner: "complete calculation lahat sa design sheets reports, including diagrams, illustrations, everything"; "lagyan mo pala ng 1inch
margin sa left for binding"; "sa calculation reports naman ng boq ... ilagay lahat ng calculation". Report pages keep a 1 inch left
margin (CalculationDocument.L / CW, EngineeringQuantityPdf.Left, HTML @page). Design-sheet reports are illustrated documents with the
complete step-by-step calculation (RichText for text calculations, RichReport.Raster for figures): Earthquake (v7.2.1), Purlin and
Splice / Development / Hooks (v7.2.2), Truss Connections (v7.1.28). Every Bill of Quantities source line carries its calculation
(src/BoqComputations.cs; SourceQuantity with computation; MergeSources keeps them) and every section table has a Basis column (v7.2.3).

## Strap Foundation water table and contact (23 November 2026)

Owner: "naconsider ba GWT sa strap design?"; "include permissible loss of contact". Strap Foundation has a ground water table input
(buoyancy deducted from the footing + soil weight, flotation check) and a no-tension soil pressure: gravity service cases need full
contact; wind / seismic service cases (recognised from the load case name) may lose contact down to a minimum % of the base (input,
default 50); the redistributed peak is used for bearing and the footing design (src/StrapFooting.cs, v7.2.5).

## iPhone app and web generator updates (23 November 2026)

Owner: "saka lang iupdate yung ios and web if may bagong dagdag na worksheet or design sheet or workspace". Change the iPhone app (ios/)
and the web access-code generator (mobile/) only when a new worksheet, design sheet or workspace (a new program in
src/ProgramAccess.cs) is added - then add its program to both; otherwise leave them as they are. Uploads (TestFlight / App Store / Pages)
still need their own explicit request.

## BOQ report layout, hinges and wording (24 November 2026)

Owner: "masmaayos na presentation ng boq report" / "di ko gusto yung ganyan layout": the Bill of Quantities Summary / Detailed /
Extensive reports use the design-sheet layout (src/QuantityReportLayout.cs, RichReport Compact 7 pt) with one calculation line per part,
a structured reinforcing steel record, and no repeated total breakdowns (v7.2.9). Automatic hinge size follows the door material (PVC
3.0x3.0 / 3.5x3.5 over 0.80 m, steel 4.0x4.0, others by width / exterior; v7.2.10). Owner: "alisin mo yung handbook profile na wording
regan etc": no supplier or handbook names in user-visible text (v7.2.11); internal comments and test labels may keep the data source.
Owner (24 November 2026, "lahat irestructure mo"; "pangit siguro yung table form"): BOQ calculation reports are numbered sections with a
k.1 Quantities table and the calculations as calculation-sheet lines (RichReport.Calc: members bold, formula under, result right, no grid),
identical sources / members merged ("40 × 4.5"), inputs in Appendix A as column tables (v7.2.12). Keep tables for summaries only.
Owner (24 November 2026, "yung sa bbs maglagyan din ng calculation sheet kagaya niyan"): the BBS report has a calculation sheet
(cut length from the shape dimensions, pieces, weight per bar mark, member totals) in the same style (v7.2.13).

## Earthquake lateral force procedure and geotechnical Mmax (24 November 2026)

Owner: "sa earthquake load dapat lahat ng parameters sa Lateral Force Procedure ibigay din sa report"; "override Mmax based on
Geotechnical Report". The Earthquake sheet computes the NSCP 2015 208.5.2 static lateral force procedure (src/StaticLateralForce.cs:
T by Method A, V with 208-8 to 208-11, Ft, Fx, storey shears / moments, E / Em, RSA scaling) and reports every parameter (section 6).
A Mmax typed from the geotechnical report overrides every automatic Mmax estimate and is cited in the report (v7.2.14).
The Store MSIX must carry tools/qpdf (scripts/Build-MSIX.ps1); without it PDF export fails in the Store app (fixed v7.2.14).
Owner (24 November 2026, "hindi makita yung color"): the Wind site map draws the roughness cells at about 30% opacity (not covering the map; owner "huwag naman OA takpan"), nearest-neighbour
(src/WindSiteMap.cs RoughnessAlpha), so B / C / D colours are visible (v7.2.15).
Owner (24 November 2026, "if naka check dapat di na pwede maginput, gawin din ito sa iba"): design-sheet inputs that a checkbox or choice
makes unused are locked through DesignSheetForm.EnableWhen (wind gust G, truss connection U / STAAD / verified values, truss support
hook / anchor grade / chord, earthquake Mmax); add such rules for new toggles. Report text and tables stay inside the 1 inch left
margin (CalculationDocument.L / CW, no fixed x positions) (v7.2.16).
Owner (24 November 2026, "yung ridge alignment dapat mag suggest"): the Wind building inputs suggest the ridge alignment (N–S, NE–SW, E–W,
NW–SE) with the largest design pressure (WindBuildingLoads.RidgePeaks); it is a suggestion only, the user picks it (v7.2.17).
Owner (24 November 2026, "ipakita din yung diagram for surface roughness"): every wind-direction exposure page of the Wind report draws the
five upwind rays coloured by roughness with the fetch limits (WindAuditPages.RoughnessRays, v7.2.18).
Owner ("siguro sa circle or sa map"): the Wind report also draws the roughness as a circle (src/WindRoughnessCircle.cs): a full page with
the 8 sectors and exposures, and a small circle with the upwind sectors highlighted on every direction page (v7.2.19).
Owner ("pano ang way para maorient yung building sa windload"): the building outline is drawn on the wind site map turned to the ridge
alignment (WindSiteMap.SetBuilding), so the orientation is matched against the satellite image (v7.2.20).
Owner (24 November 2026): the Wind report lists the load cases W1-W4 (a = +GCpi, b = -GCpi) with every face loaded at once - windward,
leeward, both side walls, roof - with a plan per case (src/WindLoadCasesReport.cs); STAAD wind types are named "TYPE n Wn FROM dir" (v7.2.21).
Owner ("nawala yung map at yung contour"): the wind report map is a fixed 1046 x 590 view (WindSiteMap.ReportSize, VW / VH), waits up to 20 s
for tiles, draws the tiles it has and states the nearest wind contours (v7.2.22).
Owner ("bakit di nagpapakita sa STAAD Wind Input"): the STAAD Wind Input has "Pressures on every face" (every face of W1a-W4b) and load
case templates with the side-wall and roof member loads (WindStaadInput.FacePressures / LoadCaseTemplates, v7.2.23).
Owner ("ilagay yung x z dimension ... WW+X LW+X"): the Wind sheet takes the STAAD +X bearing, the ridge axis and the X / Z dimensions; the
STAAD output gives each load case its WIND LOAD axis / sign and each face its GX / GZ direction (WindStaadInput.AxisOf, v7.2.24).
Owner ("define windload lang ... windward, leeward and sidewall"; "WW+X LW+X SW+X"; "dapat hiwalay sila"): STAAD output is DEFINE WIND LOAD
only, one type per face (WW / LW / SW) per direction, named by the model direction (WW+X ...), never WW and LW combined
(WindStaadInput.FaceCommands, v7.2.25); names follow STAAD's "TYPE n WIND ..." format (TYPE 1 WIND WW+X, v7.2.26).
Owner ("fix input width 72col para pwede din sa 79"): generated STAAD input lines are at most 72 columns (StaadLines.Wrap: " -" continuation,
wrapped "*" comments) (v7.2.27).
Owner ("gawa ka naman ng for C&C"): the Wind sheet computes components and cladding by NSCP 2015 207E Part 1 (src/WindComponentsCladding.cs;
qh from the worst of the 8 directions, zones 1-5, GCp by effective area, minimum 0.77 kPa), with its own tab and report pages (v7.2.28).
Owner ("automatic link ng loads sa purlin design"): Purlin Design takes its wind pressure / suction from the Wind Load sheet's C&C (chosen roof
zone, purlin effective area) through the shared link file (src/WindLink.cs); the Wind sheet groups results as MWFRS and C&C (v7.2.29).
Owner ("pakicheck yung Determination niya ng Hill kasi flat lang naman itsura"): Kzt checks condition 2 from the profile, labels a
non-qualifying shape "No qualifying hill (Kzt = 1)", warns when H < 10 m (DEM accuracy), and "Site topography = Flat" forces Kzt = 1
(WindTerrain.UserFlat, v7.2.30).
Owner ("bakit yung upwind base below 0?"): in the Kzt profile, elevations below 0 m (sea floor, shore blending) are taken as
mean sea level, 0 m, and the checks say so (WindTopographicAudit Ground, v7.2.31). Inland lakes / rivers keep their surface elevation;
water cover alone never sets 0 m (a lake at 700 m is not an escarpment) (v7.2.32).
Owner (25 November 2026, "All yung building rotation ialign sa different azimuth"): the building / ridge may have any azimuth (Ridge alignment
"Other azimuth" + RidgeAzimuth; STAAD "+X" "Other bearing" + StaadXBearing). WindBuildingInput.Azimuth / Ridge, WindPressureRow.Bearing;
a face normal between two of the eight assessed directions uses the neighbour with the larger qh (WindBuildingLoads.Pick); names are
compass points on 45° multiples, otherwise bearings ("120°") (v7.2.36).
Owner ("dapat yung faces ng building, pati yung Topographic factor"): with building pressures on, Load terrain samples the eight directions
from the ridge azimuth (WindTerrain.Offset = azimuth mod 45; faces + diagonals), so exposure and Kzt are per face; directions carry Index /
Bearing and are named by bearing. Owner ("diko gets yung +x axis"): STAAD +X may be chosen along / across the ridge (relative to the azimuth)
(v7.2.37).
Owner ("mas maayos na building inputs"; "doble doble ang dimension ... yung bearing magulo"): Building inputs are numbered groups with one value
each - dimensions once, one RidgeAzimuth box, one StaadPlusX choice relative to the ridge; RidgeDirection / StaadXDir / StaadXBearing /
StaadRidgeAxis / StaadX / StaadZ stay hidden only to read older sheets (LegacyBuildingInputs converts them) (v7.2.38).
Owner ("diko magets sa components and claddings"): C&C elements come from spacings (CcFrameSpacing, CcPurlinSpacing, CcGirtSpan, CcGirtSpacing;
A = span x max(spacing, span/3)); CCElements is only "Other elements (optional)" (CcElementsText) (v7.2.39).
