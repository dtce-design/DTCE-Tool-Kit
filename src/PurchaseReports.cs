using System;using System.Linq;using System.Text;using System.Collections.Generic;using System.Globalization;
namespace BeamScheduleFormatter {
static class PurchaseReports {
 public static void Summary(StringBuilder b,List<StockAllocation> plan){plan=plan.Where(a=>a.Choice!=null).ToList();
  b.AppendLine("01  REBAR PURCHASE SUMMARY");b.AppendLine(new string('-',92));b.AppendLine(string.Format("{0,-8}{1,6}{2,15}{3,15}{4,15}{5,15}","GRADE","DIA","CUT STEEL kg","ADDED LAP kg","OFFCUT kg","BUY kg"));foreach(int grade in new[]{40,60}){var steel=plan.Where(a=>a.Demand.Grade==grade).ToList();foreach(var g in steel.GroupBy(a=>a.Demand.Diameter).OrderBy(g=>g.Key))b.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0,-8}{1,6}{2,15:0.00}{3,15:0.00}{4,15:0.00}{5,15:0.00}","G"+grade,g.Key,g.Sum(a=>a.Net),g.Sum(a=>a.Splice),g.Sum(a=>a.Waste),g.Sum(a=>a.Purchase)));b.AppendLine(string.Format(CultureInfo.InvariantCulture,"GRADE {0} TOTAL TO BUY: {1:0.00} kg",grade,steel.Sum(a=>a.Purchase)));b.AppendLine();}b.AppendLine("GRAND TOTAL TO BUY: "+plan.Sum(a=>a.Purchase).ToString("0.00",CultureInfo.InvariantCulture)+" kg");
  b.AppendLine();b.AppendLine("SELECTED COMMERCIAL BARS TO ORDER");b.AppendLine(string.Format("{0,-8}{1,6}{2,13}{3,13}{4,16}","GRADE","DIA","LENGTH m","WHOLE BARS","BUY kg"));var stock=plan.Where(a=>a.Choice!=null).GroupBy(a=>a.Choice).Select(g=>g.First());foreach(var g in stock.GroupBy(a=>new{a.Demand.Grade,a.Demand.Diameter,a.Choice.Length}).OrderBy(g=>g.Key.Grade).ThenBy(g=>g.Key.Diameter).ThenBy(g=>g.Key.Length))b.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0,-8}{1,6}{2,13:0.00}{3,13}{4,16:0.00}","G"+g.Key.Grade,g.Key.Diameter,g.Key.Length,g.Sum(a=>a.Choice.Bars),g.Sum(a=>a.Choice.Purchased*EstimatorEngine.UnitMass(a.Demand.Diameter))));
 }
 public static void Basis(StringBuilder b){
  b.AppendLine();b.AppendLine("REPORT BASIS");
  b.AppendLine("Buy kg = cut steel + added splice steel + stock offcuts. No percentage factor.");
  b.AppendLine("Entry weights share pooled whole-bar orders; order quantities appear in the summary.");
  b.AppendLine("Only identical cuts are pooled. Different-cut offcuts are not reused; kerf = 0.");
  b.AppendLine("Gross concrete excludes opening/overlap deductions. Forms exclude top/end faces.");
  b.AppendLine("Splice positions follow the structural drawings. Totals use unrounded values.");
 }
 static string Part(StockAllocation a){return a.Line.Part=="Side-face"?"Side-face / each face":a.Line.Part=="Stirrups"?(a.Line.TieOverride?"Manual stirrup set":Math.Abs(a.Demand.Cut-a.Line.MainHoopCut)<1e-9?"Main stirrup / hoop":"Additional leg (90+130)"):EstimatorEngine.DisplayPart(a.Line.Part);}
 static string Group(StockAllocation a){string p=a.Line.Part;if(p=="Stirrups")return "STIRRUPS";if(p=="Side-face")return "SIDE-FACE BARS";if(p.StartsWith("Top distribution",StringComparison.OrdinalIgnoreCase))return "TOP DISTRIBUTION";if(p.StartsWith("Top",StringComparison.OrdinalIgnoreCase))return "TOP BARS";if(p.StartsWith("Bottom",StringComparison.OrdinalIgnoreCase))return "BOTTOM BARS";return "MAIN BARS";}
 public static void Entry(StringBuilder b,List<StockAllocation> items,int quantity,bool extensive){
  b.AppendLine();b.AppendLine("REINFORCEMENT TAKEOFF");
  b.AppendLine(string.Format("{0,-33}{1,8}{2,10}{3,10}{4,13}{5,13}","BAR / POSITION","GR/DIA","PCS/UNIT","TOTAL PCS","CUT mm/pc","BUY kg"));b.AppendLine(new string('-',90));
  foreach(var group in items.GroupBy(Group)){
   b.AppendLine(group.Key);
   foreach(var a in group){
    b.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0,-33}{1,8}{2,10}{3,10}{4,13:0.00}{5,13:0.00}",Part(a),"G"+a.Demand.Grade+"/"+a.Demand.Diameter,a.Demand.Pieces/quantity,a.Demand.Pieces,a.Demand.Cut*1000,a.Purchase));
    if(a.Choice==null)b.AppendLine("  REVIEW: commercial cut unresolved; excluded from purchase.");
    else if(a.Choice.Segments>1)b.AppendLine(string.Format(CultureInfo.InvariantCulture,"  Split: {0} segments x {1:0.00}m; {2} lap(s) x {3:0.00}m per piece.",a.Choice.Segments,a.Choice.SegmentCut,a.Choice.Segments-1,a.Choice.Lap));
   }
   b.AppendLine();
  }
  b.AppendLine(new string('-',90));b.AppendLine("TOTAL TO BUY: "+items.Where(a=>a.Choice!=null).Sum(a=>a.Purchase).ToString("0.00",CultureInfo.InvariantCulture)+" kg");
  if(extensive){b.AppendLine();b.AppendLine("CALCULATION TRACE");foreach(var a in items){b.AppendLine(Part(a)+" | G"+a.Demand.Grade+" / D"+a.Demand.Diameter);b.AppendLine("  "+a.Line.Formula);b.AppendLine(string.Format(CultureInfo.InvariantCulture,"  {0} pcs x {1:0.00}mm / 1000 x {2:0.000}kg/m = {3:0.00}kg cut steel",a.Demand.Pieces,a.Demand.Cut*1000,EstimatorEngine.UnitMass(a.Demand.Diameter),a.Net));if(a.Splice>0||a.Waste>0)b.AppendLine(string.Format(CultureInfo.InvariantCulture,"  Added lap {0:0.00}kg | offcut {1:0.00}kg",a.Splice,a.Waste));b.AppendLine();}}
 }
}
}
