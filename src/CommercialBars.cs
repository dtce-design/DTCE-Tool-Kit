using System;using System.Linq;using System.Text;using System.Collections.Generic;using System.Globalization;
namespace BeamScheduleFormatter {
class StockDemand {public int Grade,Diameter;public long Pieces;public double Cut,Lap;}
class StockChoice {public double Length,Cut,Lap;public long Required,PiecesPerStock,Bars,Segments=1;public double SpliceMetres{get{return Required*(Segments-1)*Lap;}}public double SegmentCut{get{return (Cut+(Segments-1)*Lap)/Segments;}}public double Purchased{get{return Bars*Length;}}public double Used{get{return Required*Cut+SpliceMetres;}}public double Waste{get{return Math.Max(0,Purchased-Used);}}}
class StockAllocation {public EstimateInput Entry;public EstimateLine Line;public StockDemand Demand;public StockChoice Choice;public double Purchase,Splice,Waste;public double? UnitMassOverride;public double UnitMass{get{return UnitMassOverride??EstimatorEngine.UnitMass(Demand.Diameter);}}public List<double> Allowed;public readonly List<RebarStock> Stocks=new List<RebarStock>();public double Net{get{return Demand.Cut*Demand.Pieces*UnitMass;}}}
static class CommercialBars {
 public static List<StockAllocation> Allocate(IEnumerable<EstimateInput> inputs,LengthConfiguration config,IEnumerable<double> available=null){
  var rows=new List<StockAllocation>();foreach(var entry in inputs.Where(i=>i.Result!=null))foreach(var line in entry.Result.Lines){var single=new EstimateInput{Quantity=entry.Quantity,Result=new BeamEstimate()};single.Result.Lines.Add(line);foreach(var demand in Demands(new[]{single},config))rows.Add(new StockAllocation{Entry=entry,Line=line,Demand=demand,Allowed=Available(available)});}
  foreach(var group in rows.GroupBy(a=>new{a.Demand.Grade,a.Demand.Diameter,a.Demand.Cut,a.Demand.Lap})){long count=group.Sum(a=>a.Demand.Pieces);var best=Best(Choices(group.Key.Cut,count,group.Key.Lap,available));foreach(var a in group)a.Choice=best;}
  RebarOffcuts.Allocate(rows);
  return rows;
 }

 public static readonly double[] Lengths={6,7.5,9,10.5,12,13.5,15};
 public static List<double> Available(IEnumerable<double> values){var list=(values??Lengths).Distinct().OrderBy(x=>x).ToList();if(list.Count==0||list.Any(x=>!Lengths.Contains(x)))throw new ArgumentException("Select at least one supported commercial length.");return list;}
 public static string FormatLengths(IEnumerable<double> values){return string.Join(",",values.Select(v=>v.ToString("0.##",CultureInfo.InvariantCulture)));}
 public static List<double> ParseLengths(string text){return string.IsNullOrWhiteSpace(text)?Available(null):Available(text.Split(',').Select(x=>double.Parse(x,CultureInfo.InvariantCulture)));}
 public static List<StockChoice> Choices(double cut,long pieces,double lap=0,IEnumerable<double> available=null){
  if(double.IsNaN(cut)||double.IsInfinity(cut)||cut<=0||pieces<=0)throw new ArgumentException("Invalid stock cutting demand.");
  var rows=new List<StockChoice>();foreach(double stock in Available(available)){if(cut>stock){if(lap<=0||lap>=stock||double.IsNaN(lap)||double.IsInfinity(lap))continue;long segments=(long)Math.Ceiling((cut-lap)/(stock-lap));while((cut+(segments-1)*lap)/segments>stock)segments++;rows.Add(new StockChoice{Length=stock,Cut=cut,Lap=lap,Segments=segments,Required=pieces,PiecesPerStock=1,Bars=checked(pieces*segments)});continue;}long fit=(long)Math.Floor(stock/cut);if(fit<1)continue;while(fit>0&&fit*cut>stock)fit--;if(fit==0)continue;long bars=pieces/fit+(pieces%fit==0?0:1);rows.Add(new StockChoice{Length=stock,Cut=cut,Required=pieces,PiecesPerStock=fit,Bars=bars});}return rows;
 }
 public static StockChoice Best(IEnumerable<StockChoice> choices){return choices.OrderBy(c=>Math.Round(c.Waste,9)).ThenBy(c=>c.Bars).ThenBy(c=>c.Length).FirstOrDefault();}
 public static List<StockDemand> Demands(IEnumerable<EstimateInput> inputs,LengthConfiguration configuration=null){
  var result=new List<StockDemand>();foreach(var i in inputs.Where(x=>x.Result!=null)){long quantity=long.Parse(i.Quantity,CultureInfo.InvariantCulture);foreach(var line in i.Result.Lines){Action<double,long> add=(cut,n)=>{if(n>0&&cut>0)result.Add(new StockDemand{Grade=line.Grade,Diameter=line.Diameter,Cut=cut,Pieces=n,Lap=cut>6&&line.Part!="Stirrups"&&configuration!=null?configuration.Rounded(configuration.Values(line.Diameter,line.Part.StartsWith("Top")).Lap)/1000:0});};
   if(line.Part=="Stirrups"){long sets=checked((long)line.TieSetsPerBeam*quantity);if(line.TieOverride)add(line.CutLength,sets);else{add(line.MainHoopCut,sets);add(line.AdditionalLegCut,checked(sets*line.AdditionalLegs));}}
   else add(line.CutLength,checked((long)line.PiecesPerBeam*quantity));
  }}return result.GroupBy(d=>new{d.Grade,d.Diameter,d.Cut,d.Lap}).Select(g=>new StockDemand{Grade=g.Key.Grade,Diameter=g.Key.Diameter,Cut=g.Key.Cut,Lap=g.Key.Lap,Pieces=g.Sum(x=>x.Pieces)}).OrderBy(d=>d.Grade).ThenBy(d=>d.Diameter).ThenByDescending(d=>d.Cut).ToList();
 }
 public static string Report(IEnumerable<EstimateInput> inputs,LengthConfiguration configuration=null,IEnumerable<double> available=null){
  var b=new StringBuilder();var plan=Allocate(inputs,configuration,available);
  b.AppendLine("COMMERCIAL REBAR CUTTING / PURCHASE PLAN");b.AppendLine("Available stock lengths: "+FormatLengths(Available(available))+" m.");
  b.AppendLine("Compatible offcuts are checked across different cut lengths and entries; grades and diameters stay separate.");
  b.AppendLine("Longer segments are allocated first; available offcuts are fitted before a new bar is purchased.");
  b.AppendLine("New-bar length uses the per-cut minimum-unused-length comparison; this is a feasible heuristic, not a global optimum.");
  b.AppendLine("Zero kerf. Spliced cuts include configured laps; actual splice locations require approved detailing.");
  PurchaseReports.Summary(b,plan);PurchaseReports.Basis(b);
  foreach(var a in plan){b.AppendLine(string.Format(CultureInfo.InvariantCulture,"G{0} D{1} | cut {2:0.000}m | {3} pcs | cut steel {4:0.00}kg | lap {5:0.00}kg | final offcut share {6:0.00}kg | BUY {7:0.00}kg",a.Demand.Grade,a.Demand.Diameter,a.Demand.Cut,a.Demand.Pieces,a.Net,a.Splice,a.Waste,a.Purchase));if(a.Choice==null)b.AppendLine("REVIEW: unresolved cut excluded from purchase.");}
  return b.ToString();
 }
}
}
