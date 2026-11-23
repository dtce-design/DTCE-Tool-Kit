# Prompt: DTCE calculation reports (one uniform format for every report)

Use this prompt (for a developer, Claude or another AI) whenever ANY report of DTCE Tool Kit is made or changed - every design sheet,
the Bill of Quantities reports (Summary / Detailed / Extensive), the estimators, bar bending schedules and construction schedules - so
all reports look and read the same. The Staircase Design report is the reference example of the style.

---

## The prompt

> You are writing a calculation report of DTCE Tool Kit (C# / WinForms, `RichReport` in `src/RichReport.cs`) - a design sheet, a
> Bill of Quantities report, an estimator, a bar bending schedule or a schedule. The same format applies to all of them.
> Follow this format exactly; do not invent another layout.
>
> **Page and type**
> - A4 portrait (A3 landscape only for drawing sheets). **1 inch (72 pt) left margin for binding**, 0.5 inch right
>   (`CalculationDocument.L` / `CalculationDocument.CW`). Body text 7 pt minimum; headings may be larger.
> - Every page has the DTCE header ("DTCE / CALCULATION REPORT", report title, project and location) and the footer (tool kit
>   version, date, page n / N) — `CalculationDocument` draws these; do not draw your own.
> - Title: `"<Sheet name> - <type / member>"`, e.g. "Staircase Design - Dog-legged (two flights, half landing)".
>
> **Sections, numbered, in this order** (`rep.Heading("N  TITLE")`, capitals):
> 1. **DESIGN BASIS** — code and edition with section numbers (NSCP 2015 / ACI 318-14 / AISC 360-16 …), load combinations,
>    method (LRFD / ASD), assumptions and scope in one paragraph.
> 2. **GEOMETRY / INPUTS** — `KeyValues` table of every input with units; then the drawings (plan, section, 3D) as figures.
> 3. **LOADS** — table of dead / live / wind / seismic and factored loads, with the formula of each load written out with values.
> 4. **ANALYSIS AND DESIGN** — per member: span / support line, load–shear–moment diagram figure, section with reinforcement figure.
> 4A. **DETAILED CALCULATION** — every step as a table `Step | Formula and values | Result` (formula, substituted numbers,
>    result with unit, OK / NG where it is a check). Nothing is skipped; intermediate values are shown.
> 5. **CHECKS** — one **check card** per limit state (`rep.Check(name, clause, lines, governing, demand, capacity, unit, ok)`):
>    - header: check name - member on the left, **code clause on the right** (e.g. "Geometry (PD 1096 / RA 9514)", "J4.3 / NSCP 510.4.3");
>    - optional monospace line(s) with the governing formula / values;
>    - result line: **"Demand X ≤ capacity Y → ratio R"** with the **OK (green) / NG (red)** box on the right.
>    For a minimum (e.g. tread ≥ 250), demand is the limit and capacity the provided value, so the ratio is ≤ 1 when it passes.
> 6. **REINFORCEMENT / DETAILS** (where applicable) — table with marks (A, B, C …) that match the marks on the figures.
> 7. **SUMMARY** — table of every check (`Limit state | Available | Demand | Ratio | Result`), failing rows tinted red, and a
>    utilisation bar chart (1.00 limit line); one sentence naming the governing check.
> 8. **NOTES** — scope limits, items to verify, data sources (muted text).
> End with `rep.Signatures("Prepared by","Checked by")` — blank lines only; never imply an engineer approved the design.
>
> **Figures**
> - Every figure has a caption "Figure n - …" saying what it shows and the load case / member.
> - Drawn to scale where it is a drawing (equal x / y scale, graphic scale bar, bars at their diameters); diagrams show values at
>   peaks and supports with units. Shear positive up; moment on the tension side (say so in a note).
> - Draw custom figures through `RichReport.Raster((g, rc) => …)` (report points, 72 dpi bitmap) and screen views through
>   their `Render(…)` / `RenderTo(…)` helpers, so labels keep their size on screen, in print and in the PDF.
> - Colours: ink #283442, steel / bars red #BE2828, OK green #3C966E, NG red #BE2D2D, accent teal; paper white.
>
> **Quantity reports (Bill of Quantities, estimators, BBS)** use the same frame, headings, tables and figures; in place of design
> checks they show:
> - Summary: pay items with quantity, unit and a **Basis** column (formula, or the sum of the sources with their values);
> - Detailed: every source line with its **calculation written out** (dimensions × quantity = result; bars: pieces × cut × kg/m =
>   net + laps + allocated offcut = purchase), the measurement inputs, offcut reuse (where each offcut comes from);
> - Extensive: everything in Detailed plus the full bar-by-bar trace and the commercial stock to order;
> - figures where they help: member sketches, cutting patterns, bar shapes, sheet nesting, charts of totals;
> - totals that do not match their sources are an error - never show one without the other.
>
> **Text**
> - Units on every number (mm, m, kN, kN·m, MPa, kPa). Numbers to sensible precision (lengths 0 dp mm, forces 0.1 kN, ratios 0.00).
> - Formulas written once in symbols, then with the values substituted, then the result: "Ts = Cv / (2.5 Ca) = 0.64 / (2.5 × 0.44) = 0.582 s".
> - Short sentences; English; no abbreviations without their meaning the first time.
> - Text calculations kept as strings can be converted with `RichText.Add(rep, text)`: "## NN  TITLE" lines → sections,
>   a label line followed by indented formula lines → a `Step | Formula` table row, form feed → page break.
>
> **Do not**
> - change the calculation to fit the report; the report shows what the engine computed;
> - drop steps, inputs or failing checks; failing items stay visible and are tinted / marked NG;
> - add a signature, approval or "certified" wording.
>
> **Verify** by rendering the pages (`CalculationDocument.Draw`) and the PDF (`WritePdf`) and looking at them: margins, no text under
> 7 pt, no overlapping labels, figures not cut, cards and tables not split across pages.

---

## Where the reports are built

| Sheet | Builder |
|---|---|
| Staircase Design (reference) | `src/StaircaseWindow.cs` → `BuildDocument` |
| Truss Connections | `src/TrussConnectionReport.cs` |
| Earthquake Load & RSA | `src/EarthquakeReport.cs` |
| Purlin Design | `src/PurlinReport.cs` |
| Splice, Development & Hooks | `src/SpliceReport.cs` |
| Truss Support on RC | `src/TrussSupportWindow.cs` → `BuildDocument` |
| Strap Foundation | `src/StrapFootingWindow.cs` → `BuildDocument` |
| Wind Load | `src/WindCalculationDocument.cs`, `src/WindAuditPages.cs` |
| Beam Deflection Check | `src/BeamDeflectionReport.cs` |

Shared pieces: `src/RichReport.cs` (Heading, Paragraph, KeyValues, Table, Figure, Check, PageBreak, Signatures, Raster),
`src/RichText.cs`, `src/CalculationDocument.cs` (page frame, margins, report view, PDF).
