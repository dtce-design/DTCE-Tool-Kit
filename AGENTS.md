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
