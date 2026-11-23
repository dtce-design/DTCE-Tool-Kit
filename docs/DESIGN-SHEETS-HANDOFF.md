# DTCE Tool Kit — Design Sheets: hand-off notes

Status as of v7.2.4 (23 November 2026). For engineers, reviewers or developers taking over or using the design sheets.
Generated reports are calculation records only — they are **not** an engineer's approval; every sheet ends with blank
"Prepared by / Checked by" signature lines.

## Common to every design sheet

| Item | Behaviour |
|---|---|
| Where | Workspaces → **Design Sheets**; each sheet opens in its own window with full-caption tabs at the top. |
| Layout | Each input page: compact input form on the left, drawings / tables on the right. Info icon explains the open tab. |
| Commands | **Calculate**, **Print preview**, **Save sheet XML**, **Open sheet XML**, **Export ▾** (PDF, report file, report to Excel). |
| Report pages | A4, **1 inch left margin for binding**, 0.5 inch right, body text ≥ 7 pt, numbered sections, tables, check cards (demand / capacity / ratio / OK or NG), figures drawn to the report scale. User edition carries the DPWH logo watermark; Admin none. |
| Saved files | `*.xml` design-sheet files; older files keep opening (new inputs are optional). |
| Access | Each sheet is its own program in the access code. In the **User** edition a sheet without access opens **view only** (tabs and scrolling; no inputs, Calculate or exports) with an "Enter access code" banner. |
| Code | Window: `src/*Window.cs`; engine: `src/<Sheet>.cs`; rich report builder: `RichReport` (+ `RichText` for step-by-step text, `RichReport.Raster` for figures). |

## The sheets

### 1. Wind Load (NSCP 2015)
- **Basis:** NSCP 2015 Section 207 (ASCE 7-10 method); wind speed from the owner's RCB Wind v3.3 map (Figure 207A.5-1).
- **Inputs:** site coordinates, terrain / exposure per direction (8 directions), topography (Kzt), building geometry, risk category; STAAD wind input tab.
- **Report:** site and wind-speed summary, eight-direction velocity-pressure table (Kz, Kzt, qz), source map with the project location, terrain profiles, building pressure basis, surface pressures by direction / zone, pressure substitution example, data sources.
- **Files:** `src/WindDesignWindow.cs`, `src/WindCalculationDocument.cs`, `src/WindAuditPages.cs`.

### 2. Earthquake Load & RSA Curve Builder (NSCP 2015)
- **Basis:** NSCP 2015 Section 208 — Tables 208-1 to 208-8, Figure 208-3 spectrum; Wells & Coppersmith (1994) Mmax from rupture length.
- **Inputs:** site coordinates (looks up HazardHunterPH fault distance and USGS earthquake history), fault Mmax / slip rate, zone, soil profile, source type, distance, importance, R, scale factor, period range, STAAD units / combination.
- **Outputs:** Ca, Cv, Na, Nv, T0, Ts, spectrum ordinates, STAAD `SPECTRUM` input ready to paste.
- **Report:** design basis; site table + **map** (fault traces, site fault, historical quakes); seismic source (Mmax worked out, Table 208-3 with the chosen type highlighted, 15 largest earthquakes, **magnitude vs distance chart**); coefficients (Na/Nv table rows with **interpolation written out** + chart; Ca/Cv for all soils, used row highlighted); **spectrum chart** and formulas; STAAD scale factor; all ordinates; STAAD input; notes.
- **Files:** `src/RsaCurveBuilder.cs`, `src/EarthquakeReport.cs`, `src/EarthquakeSite.cs`, `src/EarthquakeMap.cs`.

### 3. Purlin Design
- **Basis:** owner's PS purlin workbook (ASD, workbook allowable stresses), biaxial bending with sag rods, deflection limits.
- **Inputs:** handbook C/Z section, span, spacing, roof angle, sag rods (0–2), support, loads (dead, live, wind pressure / suction), Fy, E.
- **Report:** summary table; Fig. 1 section; Fig. 2 **load resolution on the roof slope** (wx, wy, wind normal); Fig. 3 **span with sag rods and Mx / My moment diagrams**; Fig. 4 **utilisation chart** (every load case + deflections); then the complete step-by-step calculation (properties, loads, allowable stresses, each load case, envelopes, deflection) as Step | Formula tables.
- **Files:** `src/PurlinDesign.cs`, `src/PurlinReport.cs`, `src/DesignSheetWindow.cs` (PurlinDesignForm).

### 4. Truss Connections
- **Basis:** AISC 360-16 / NSCP 2015 Chapter 5 (owner's PS truss workbooks, corrected): member yielding / rupture with shear lag, fillet welds, bolt shear and bearing, gusset yielding / rupture, **block shear (J4.3)**.
- **Layout:** two main tabs — **Weld design** and **Bolt design** — each with sub tabs *Member and demand*, *Weld / Bolt detail*, *Block shear*, *STAAD forces*, *Report*. Member, block-shear and STAAD inputs are shared; the open tab decides weld or bolts.
- **Details:** interactive weld drawing (angle; slotted HSS / pipe have their own detail) and **interactive bolt layout** drawn to scale (end distance, spacing, gauge, edges; click a dimension to edit). Double angles: gusset thickness = back-to-back spacing; bolts default to double shear.
- **Block shear:** automatic — bolts: member leg path and gusset path; welded angles: gusset around the welds (Anv = Agv); slotted HSS: none (noted).
- **Report:** basis; member & demand table; Fig. section, load path, weld/bolt detail, shear lag, block shear; check card per limit state with clause; summary table; utilisation chart; full calculation; STAAD demand audit.
- **Files:** `src/TrussConnections.cs`, `src/TrussConnectionWindow.cs`, `src/TrussConnectionReport.cs`, `src/TrussBoltDrawing.cs`, `src/TrussWeldDrawing.cs`, `src/DetailedDesignReports.cs`.

### 5. Truss Support on RC
- **Basis:** NSCP 2015 Ch. 5 (AISC 360-10: J8 bearing, J2 welds, J4, D2/D3) and Section 417 anchors (ACI 318-14 Ch. 17, cast-in, condition B); plate thickness AISC Design Guide 1. Pinned support.
- **Inputs:** STAAD support reactions (paste), base plate, RC support, anchor bolts (headed / L), chord, gusset, end member, welds.
- **Report:** basis; geometry & materials; **drawings to scale** (plan, elevation, 3D; section across the truss; anchor detail); reaction table (failing rows tinted); check card per limit state with step-by-step lines.
- **Files:** `src/TrussSupport.cs`, `src/TrussSupportWindow.cs`, `src/TrussSupportSketch.cs`, `src/TrussSupportDetail.cs`, `src/AnchorBolts.cs`.

### 6. Strap Foundation
- **Basis:** NSCP 2015 (ACI 318-14) Sections 413 (footings), 409 (strap beams), 422 (shear, punching); rigid footings and straps, straps clear of the soil.
- **Inputs:** STAAD reactions (all load combinations pasted), footings, columns, straps, soil (qa, Df), materials.
- **Report:** basis; materials / soil / load cases; footing and strap tables; Fig. plan with soil pressure, 3D view, reinforcement 3D; reactions; statics per load case; checks with steps.
- **Files:** `src/StrapFooting*.cs`.

### 7. Beam Deflection Check
- **Basis:** NSCP 2015 — RC Table 424.2.2 with Section 424.2.4 long-term deflection; steel / other members per NSCP Chapter 1 limits.
- **Inputs:** STAAD beam data / displacements.
- **Report:** engineering report with the checks and deflected-shape sheets per governing beam.
- **Files:** `src/BeamDeflection*.cs`.

### 8. Staircase Design
- **Basis:** NSCP 2015 (ACI 318-14); loads per NSCP Table 205-1; U = 1.2D + 1.6L; one-way slab along the flight (or step spanning across for cantilever / stringer / spiral types).
- **Inputs:** stair type, risers / treads, width, waist / landing, landings, supports (on beams, beam size / location), top bars full length or at supports, materials, covers, bars.
- **Drawings (one tab "Stair type, geometry and model"):** section **drawn to scale** (bars at their diameters, graphic scale), plan with dimension chains, 3D, load / shear / moment diagram (shear positive up). Standard detailing: crossing bars at re-entrant corners run ld past the crossing; **step bars ("chair bars") bent to each riser and tread** with the nosing bar in the bend; anchorage (90° hook ldh / 150 mm).
- **Report:** basis, geometry, plan and 3D figures, loads, analysis per member with diagrams and sections, **4A detailed calculation** (every step with values), checks, reinforcement table with marks A–E, sections with reinforcement, notes.
- **Files:** `src/Staircase.cs`, `src/StaircaseWindow.cs`, `src/StaircaseViews.cs`.

### 9. Splice, Development & Hooks
- **Basis:** NSCP 2015 / ACI 318M-14 Chapter 25 (25.3 hooks, 25.4.2 tension, 25.4.3 hooked, 25.4.9 compression, 25.5.2 / 25.5.5 laps); simplified or general method.
- **Inputs:** configuration (grades / fy small and large bars, f'c, covers, spacing, Ktr, splice %, rounding), top / other bars, straight leg.
- **Output:** adopted lengths saved in the app and used automatically by the estimators.
- **Report (Print preview / Export PDF):** summary table and chart of all eight diameters (Ld top/other, lap A/B, Ldh, Ldc, compression lap, hook cuts); configuration review; per diameter: **development / lap / hooked-development diagram** and **90°, 180°, 135° hooks to scale**, then the complete calculation.
- **Files:** `src/RebarLengths.cs`, `src/LengthConfiguration.cs`, `src/SpliceReport.cs`.

## Known limits / to verify
- Report text was checked on Windows CI only through tests; please review a printed PDF of each sheet before relying on the layout.
- Each sheet states its scope (e.g. Truss Connections: axial, concentric only — no member buckling, gusset Whitmore buckling, HSS chord limit states, prying, fatigue or seismic detailing).
- Earthquake look-ups need internet (HazardHunterPH, USGS); verify source type and distance against the PHIVOLCS fault map.
- Version history and the reason for each change: `docs/TEST-BUILD-v*.txt`; standing owner decisions: `AGENTS.md`.
