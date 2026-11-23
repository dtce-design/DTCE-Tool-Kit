using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* Where reused offcuts come from (owner, 23 November 2026: "sa reports ng BOQ dapat nakikita din kung saan nanggaling yung offcuts reuse
   bara sa RSB, angle bars at ibang member na may commercial length"). For every commercial bar / 6 m stock that is shared, the report
   names the member it was opened for (its first and longest cut) and every member that reuses the offcut, with pieces and lengths.
   Identical stocks are counted together. Detailed and Extensive Bill of Quantities reports. */
partial class ProgramOfWorksForm {
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;
 static string M(double v){return v.ToString("0.###",Ci);}
 /* rebar: one row per shared bar pattern */
 internal static List<string[]> RebarOffcutRows(IEnumerable<StockAllocation> plan){var rows=new List<string[]>();
  var shared=plan.Where(a=>a.Choice!=null).SelectMany(a=>a.Stocks).Distinct().Where(s=>s.Count>0&&s.Used.Count>1).ToList();
  Func<StockAllocation,double,string> part=(a,used)=>{double cut=a.Choice.SegmentCut;long pcs=cut>0?(long)Math.Round(used/cut):0;return RebarSource(a)+" — "+pcs+" pc(s) x "+M(cut)+" m";};
  foreach(var g in shared.Select(s=>new{s,opener=s.Used.First(),others=s.Used.Skip(1).ToList()})
    .GroupBy(x=>"G"+x.s.Grade+" / "+RebarDiameterText.Format(x.s.Diameter)+" / "+M(x.s.Length)+" m\u0001"+part(x.opener.Key,x.opener.Value)+"\u0001"+string.Join("\n",x.others.Select(o=>part(o.Key,o.Value)))+"\u0001"+M(x.s.Remaining))
    .OrderBy(g=>g.Key,StringComparer.Ordinal)){
   var p=g.Key.Split('\u0001');rows.Add(new[]{p[0],g.Sum(x=>x.s.Count).ToString(Ci),p[1],p[2],p[3]});}
  return rows;}
 /* 6 m steel stocks (purlins, angle bars, fascia frame, railings, sag rods): one row per shared stock pattern */
 internal static List<string[]> SteelOffcutRows(IEnumerable<SteelStock> stocks,Func<string,string> name){var rows=new List<string[]>();if(stocks==null)return rows;
  Func<SteelCut,SteelCutDemand> src=c=>c.Demand.Source??c.Demand;
  foreach(var g in stocks.Where(s=>s.Cuts.Count>0&&s.Cuts.Select(src).Distinct().Count()>1).Select(s=>{var opener=src(s.Cuts[0]);
     Func<IGrouping<SteelCutDemand,SteelCut>,string> text=x=>name(x.Key.Member)+" — "+x.Count()+" pc(s) x "+M(x.First().Demand.Length)+" m";var groups=s.Cuts.GroupBy(src).ToList();
     return new{key=s.Specification+" / "+M(s.Length)+" m\u0001"+text(groups.First(x=>x.Key==opener))+"\u0001"+string.Join("\n",groups.Where(x=>x.Key!=opener).Select(text))+"\u0001"+M(s.Remaining)};})
    .GroupBy(x=>x.key).OrderBy(g=>g.Key,StringComparer.Ordinal)){var p=g.Key.Split('\u0001');rows.Add(new[]{p[0],g.Count().ToString(Ci),p[1],p[2],p[3]});}
  return rows;}
 static readonly string[] OffcutHeaders={"Material / commercial length","Stocks","Opened for (first cut)","Offcut reused by","Final offcut per stock (m)"};
 /* member label: ID and description of its row on the steel sheets */
 string SteelMemberName(string id){if(string.IsNullOrWhiteSpace(id))return "(no ID)";foreach(var g in SteelTables().Values)foreach(DataGridViewRow r in g.Rows){if(r.IsNewRow)continue;if(string.Equals(Cell(r,0),id,StringComparison.OrdinalIgnoreCase)){string d=Cell(r,1);return d==""?id:id+" "+d;}}return id;}
 void AddOffcutReuse(EngineeringReport report,string scope,bool overall,IEnumerable<StockAllocation> reinforcement){
  if(overall||scope=="Reinforced Concrete Works"||scope=="Slope Protection Works"){var rows=RebarOffcutRows(reinforcement);if(rows.Count>0){var s=report.Section("Reinforcing steel — offcut reuse");s.Note("Each commercial bar shared by more than one bar mark: the bar it was bought for and the bars cut from its offcut. Weights are already in the purchase totals above.");s.Table(OffcutHeaders,rows);}}
  if(overall||scope=="Structural Steel Works"){var rows=SteelOffcutRows(steelStocks,SteelMemberName).Concat(SteelOffcutRows(sagRodStocks,x=>x)).ToList();if(rows.Count>0){var s=report.Section("Structural steel — offcut reuse (6 m stock)");s.Note("Each 6 m purlin / angle bar / fascia / sag rod stock shared by more than one member: the member it was opened for and the members cut from its offcut.");s.Table(OffcutHeaders,rows);}}
  if(overall||scope=="Finishing Works"){var rows=SteelOffcutRows(railingStocks,x=>x);if(rows.Count>0){var s=report.Section("Railings — offcut reuse (6 m stock)");s.Note("Each 6 m railing stock shared by more than one member: the member it was opened for and the members cut from its offcut.");s.Table(OffcutHeaders,rows);}}}
}
}
