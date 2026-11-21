using System;using System.Collections.Generic;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
/* Standard cutting list: how each commercial bar is cut (owner, 19 November 2026: "Standard presentation ng bbs at cutting list").
   Pieces of the same grade and diameter are packed into stock bars by best-fit decreasing (longest first, into the bar it fills best); each bar is then bought at the shortest
   commercial length that holds its cuts. The plan with the least bought length is kept (mixed lengths, or one length only).
   Identical bars are grouped as one cutting pattern. No saw kerf is taken (rebar is sheared). */
class PatternCut {public double Length;public int Count;}
class CutPattern {public string Id;public int Grade,Diameter,Bars;public double Stock;public List<PatternCut> Cuts=new List<PatternCut>();public Dictionary<string,int> Marks=new Dictionary<string,int>();
 public double Used{get{return Cuts.Sum(c=>c.Length*c.Count);}}public double Offcut{get{return Stock-Used;}}}
class PatternSummary {public int Grade,Diameter;public Dictionary<double,int> BarsByStock=new Dictionary<double,int>();public double Bought,Used,Kg;public double WastePercent{get{return Bought<=0?0:(Bought-Used)/Bought*100;}}}
static class CuttingPatterns {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 public const double ReusableOffcut=1.0;
 sealed class Bin {public double Stock,Used;public List<KeyValuePair<string,double>> Cuts=new List<KeyValuePair<string,double>>();}
 static List<Bin> Pack(List<KeyValuePair<string,double>> items,double stock,List<double> stocks,bool shrink){var bins=new List<Bin>();
  foreach(var it in items){Bin best=null;foreach(var b in bins)if(b.Stock-b.Used+1e-9>=it.Value&&(best==null||b.Stock-b.Used<best.Stock-best.Used))best=b;if(best==null){best=new Bin{Stock=stock};bins.Add(best);}best.Cuts.Add(it);best.Used+=it.Value;}
  if(shrink)foreach(var b in bins)b.Stock=stocks.Where(x=>x+1e-9>=b.Used).DefaultIfEmpty(b.Stock).Min();return bins;}
 /* pieces longer than every commercial length are left out (the cutting list reports them) */
 public static List<CutPattern> Plan(IEnumerable<CutPiece> pieces,List<double> stocks,List<string> errors){var result=new List<CutPattern>();var sorted=stocks.Distinct().OrderBy(x=>x).ToList();if(sorted.Count==0)return result;double longest=sorted.Last();
  foreach(var grp in pieces.Where(p=>p.Pieces>0&&p.Length>0).GroupBy(p=>new{p.Grade,p.Diameter}).OrderBy(x=>x.Key.Grade).ThenBy(x=>x.Key.Diameter)){
   var items=new List<KeyValuePair<string,double>>();foreach(var p in grp){if(p.Length>longest+1e-9)continue;/* already reported as longer than the bar */if(p.Pieces>200000){errors.Add(p.BarMark+": too many pieces for a cutting pattern.");continue;}for(long k=0;k<p.Pieces;k++)items.Add(new KeyValuePair<string,double>(p.BarMark,Math.Round(p.Length,3)));}
   if(items.Count==0)continue;items=items.OrderByDescending(x=>x.Value).ThenBy(x=>x.Key,StringComparer.Ordinal).ToList();
   List<Bin> best=null;foreach(var plan in new[]{Pack(items,longest,sorted,true)}.Concat(sorted.Where(st=>st+1e-9>=items[0].Value).Select(st=>Pack(items,st,sorted,false)))){if(best==null||plan.Sum(b=>b.Stock)<best.Sum(b=>b.Stock)-1e-9||(Math.Abs(plan.Sum(b=>b.Stock)-best.Sum(b=>b.Stock))<1e-9&&plan.Count<best.Count))best=plan;}
   int no=0;foreach(var same in best.GroupBy(b=>b.Stock.ToString("0.###",Inv)+"|"+string.Join(",",b.Cuts.Select(c=>c.Value).OrderByDescending(v=>v).Select(v=>v.ToString("0.###",Inv)))).OrderByDescending(x=>x.First().Stock).ThenByDescending(x=>x.Count())){var bin=same.First();no++;
    var pat=new CutPattern{Id=grp.Key.Diameter+"-P"+no.ToString(Inv),Grade=grp.Key.Grade,Diameter=grp.Key.Diameter,Bars=same.Count(),Stock=bin.Stock};foreach(var c in bin.Cuts.GroupBy(c=>c.Value).OrderByDescending(c=>c.Key))pat.Cuts.Add(new PatternCut{Length=c.Key,Count=c.Count()});foreach(var c in same.SelectMany(x=>x.Cuts)){int v;pat.Marks[c.Key]=(pat.Marks.TryGetValue(c.Key,out v)?v:0)+1;}result.Add(pat);}}
  return result;}
 public static List<PatternSummary> Summary(List<CutPattern> patterns){return patterns.GroupBy(p=>new{p.Grade,p.Diameter}).Select(g=>{var s=new PatternSummary{Grade=g.Key.Grade,Diameter=g.Key.Diameter};foreach(var p in g){int v;s.BarsByStock[p.Stock]=(s.BarsByStock.TryGetValue(p.Stock,out v)?v:0)+p.Bars;s.Bought+=p.Stock*p.Bars;s.Used+=p.Used*p.Bars;}s.Kg=s.Bought*EstimatorEngine.UnitMass(g.Key.Diameter);return s;}).ToList();}
 public static string Describe(CutPattern p){return string.Join(" + ",p.Cuts.Select(c=>c.Count+" x "+c.Length.ToString("0.000",Inv)+" m"));}
 public static string MarksOf(CutPattern p){return string.Join(", ",p.Marks.OrderBy(m=>m.Key,StringComparer.Ordinal).Select(m=>m.Key+" ("+m.Value+")"));}
}
}
