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

## Shared settings and the open project (23 November 2026)

Owner: "kahit sana app from store or downloaded lang exe file sana naoopen pa din yung saved settings and project". User settings and
saved projects live in Documents\DTCE Tool Kit (src/SharedData.cs), shared by the Store (MSIX, AppData virtualized) and the .exe; old
AppData / Store-private copies are brought over once. Owner (23 November 2026, "kapag bagong open or naka close huwag mag select ng
project information"): no project is selected when the app opens or after the Bill of Quantities closes (v7.1.4); projects are
picked from the saved list (shared) or loaded with a Bill of Quantities file.

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
Owner ("reinforcement ng steps kako", v7.1.27): "chair bars" of a stair means the step bars bent to each riser and tread (legs lapped
onto the waist top bars, nosing bar in the bend), not support chairs; do not draw support chairs in the section.

## Door hardware (23 November 2026)

Owner: hinges are "Ball Bearing Stainless Door Hinge" in 2 pcs/pack, sizes 3.0x3.0x2.5mm, 3.5x3.5x2.5mm, 4.0x3.0x3.0mm, 4.0x4.0x3.0mm
(BOQ lines in packs per size; Hinge size column Automatic or chosen); double-leaf doors get 1 leverset, one 4" Chrome Plated Chain Bolt
and one 4" Chrome Plated Foot Bolt; optional Deadbolt (Double Cylinder / Single Cylinder with Thumbturn, Satin Stainless Steel); leaves of
different widths are typed in Leaf widths (e.g. 0.90+0.30) and each leaf gets its own hinges (src/DoorHardware.cs, v7.1.18). All go to
1004(2) Finishing Hardware.

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
