using System;using System.Collections.Generic;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
/* The calculation behind every source quantity of the Bill of Quantities reports (owner, 23 November 2026: "sa calculation reports
   naman ng boq, ayusin. summary detailed, extensive. ilagay lahat ng calculation"): each source line of the Detailed / Extensive reports
   shows its formula with the values, not only the result. */
static class BoqComputations {
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;
 internal static string N(double v){return v.ToString("0.###",Ci);}
 static string Q(int n){return n.ToString(Ci);}
 /* footings: concrete, forms, gravel bed, excavation with 0.30 m working space each side, backfill */
 internal static string FootingConcrete(FootingTakeoff f){return "L × W × T × n = "+N(f.Length)+" × "+N(f.Width)+" × "+N(f.Thickness)+" × "+Q(f.Quantity)+" = "+N(f.Concrete)+" m³";}
 internal static string FootingForms(FootingTakeoff f){return f.Forms<=0?"not formed (cast against the soil)":"2 (L + W) × T × n = 2 × ("+N(f.Length)+" + "+N(f.Width)+") × "+N(f.Thickness)+" × "+Q(f.Quantity)+" = "+N(f.Forms)+" m²";}
 internal static string FootingGravel(FootingTakeoff f){return "L × W × 0.10 m bed × n = "+N(f.Length)+" × "+N(f.Width)+" × 0.10 × "+Q(f.Quantity)+" = "+N(f.Gravel)+" m³";}
 internal static string FootingExcavation(FootingTakeoff f){return "(L + 0.60) × (W + 0.60) × depth × n = ("+N(f.Length)+" + 0.60) × ("+N(f.Width)+" + 0.60) × "+N(f.Depth)+" × "+Q(f.Quantity)+" = "+N(f.Excavation)+" m³ (0.30 m working space each side)";}
 internal static string FootingBackfill(FootingTakeoff f){return "excavation - concrete - gravel = "+N(f.Excavation)+" - "+N(f.Concrete)+" - "+N(f.Gravel)+" = "+N(f.Backfill)+" m³";}
 /* beams: concrete b d L n; forms both sides 2 d L n plus the soffit b L n when elevated */
 internal static string BeamConcrete(BeamQuantities m){return m.L<=0?"":"b × d × L × n = "+N(m.B)+" × "+N(m.D)+" × "+N(m.L)+" × "+Q(m.N)+" = "+N(m.Concrete)+" m³";}
 internal static string BeamForms(BeamQuantities m){return m.L<=0?"":"sides 2 d L n = 2 × "+N(m.D)+" × "+N(m.L)+" × "+Q(m.N)+" = "+N(m.SideForms)+(m.BottomForms>0?"; soffit b L n = "+N(m.B)+" × "+N(m.L)+" × "+Q(m.N)+" = "+N(m.BottomForms):"; no soffit form (on grade)")+"; total "+N(m.Formwork)+" m²";}
 internal static string RcWallConcrete(RCWallTakeoff w){return "L × H × t × n = "+N(w.Length)+" × "+N(w.Height)+" × "+N(w.Thickness)+" × "+Q(w.Quantity)+" = "+N(w.Concrete)+" m³";}
 internal static string RcWallForms(RCWallTakeoff w){return "("+Q(w.FormFaces)+" faces × L × H + "+Q(w.EndFaces)+" ends × H × t"+(w.BottomForm?" + L × t bottom":"")+") × n = ("+Q(w.FormFaces)+" × "+N(w.Length)+" × "+N(w.Height)+" + "+Q(w.EndFaces)+" × "+N(w.Height)+" × "+N(w.Thickness)+(w.BottomForm?" + "+N(w.Length)+" × "+N(w.Thickness):"")+") × "+Q(w.Quantity)+" = "+N(w.Forms)+" m²";}
 internal static string CanopyConcrete(CanopyTakeoff c){return "L × projection × t × n = "+N(c.Length)+" × "+N(c.Projection)+" × "+N(c.Thickness)+" × "+Q(c.Quantity)+" = "+N(c.Concrete)+" m³";}
 internal static string CanopyForms(CanopyTakeoff c){return "soffit L × P × n = "+N(c.SoffitForms)+"; edges (L + 2P) × t × n = ("+N(c.Length)+" + 2 × "+N(c.Projection)+") × "+N(c.Thickness)+" × "+Q(c.Quantity)+" = "+N(c.EdgeForms)+"; total "+N(c.Forms)+" m²";}
 /* walls */
 internal static string WallNet(WallItem w,IEnumerable<OpeningItem> openings){var mine=openings.Where(o=>string.Equals(o.Wall.Trim(),w.Id.Trim(),StringComparison.OrdinalIgnoreCase)).ToList();double ded=mine.Sum(o=>o.Area);
  string gross=w.EnteredArea.HasValue?"area "+N(w.EnteredArea.Value)+" × "+Q(w.Quantity):"L × H × n = "+N(w.Length)+" × "+N(w.Height)+" × "+Q(w.Quantity);return gross+" = "+N(w.Gross)+" m² gross"+(ded>0?" - openings "+string.Join(" + ",mine.Select(o=>o.Id+" "+N(o.Area)))+" = "+N(w.Gross-ded):"")+" m² net";}
 internal static string PlasterText(PlasterTakeoff p,string sides){return "gross "+N(p.Gross)+" m² ("+sides+")"+(p.TileDeduction>0?" - wall tiles "+N(p.TileDeduction):"")+" = "+N(p.Net)+" m²";}
 /* earthworks: bank volume, loose = bank × reusable × (1 + swell), compacted = bank × reusable × (1 - shrink) */
 internal static string Earth(EarthworkMaterial m){return "bank "+N(m.Bank)+" m³; reusable "+N(m.Reusable)+"%, swell "+N(m.Swell)+"%, shrink "+N(m.Shrink)+"% → loose "+N(m.LooseAvailable)+" m³, compacted "+N(m.CompactedAvailable)+" m³";}
 /* rebar: pieces × cut × kg/m = net, + laps, + allocated offcut = purchase */
 internal static string Rebar(StockAllocation a){return (string.IsNullOrWhiteSpace(a.Line.Formula)?"":a.Line.Formula+"; ")+"G"+a.Demand.Grade+" "+RebarDiameterText.Format(a.Demand.Diameter)+": "+a.Demand.Pieces+" pcs × "+N(a.Demand.Cut)+" m × "+N(a.UnitMass)+" kg/m = "+N(a.Net)+" kg net + laps "+N(a.Splice)+" + allocated offcut "+N(a.Waste)+" = "+N(a.Purchase)+" kg";}
}
}
