using System;using System.Linq;using System.Collections.Generic;
namespace BeamScheduleFormatter {
class RebarStock {
 public int Grade,Diameter;public long Count;public double Length,Remaining;
 // Used lengths are PER stock; Count identical stock patterns are represented together.
 public readonly Dictionary<StockAllocation,double> Used=new Dictionary<StockAllocation,double>();
 public RebarStock Copy(long count){var copy=new RebarStock{Grade=Grade,Diameter=Diameter,Count=count,Length=Length,Remaining=Remaining};foreach(var pair in Used)copy.Used.Add(pair.Key,pair.Value);return copy;}
}
static class RebarOffcuts {
 public static void Allocate(List<StockAllocation> rows){foreach(var a in rows){a.Purchase=a.Splice=a.Waste=0;a.Stocks.Clear();}
  foreach(var material in rows.Where(a=>a.Choice!=null).GroupBy(a=>new{a.Demand.Grade,a.Demand.Diameter})){
   var stock=new List<RebarStock>();
   foreach(var a in material.OrderByDescending(a=>a.Choice.SegmentCut)){
    double cut=a.Choice.SegmentCut;long pieces=checked(a.Demand.Pieces*a.Choice.Segments);
    foreach(var bin in stock.Where(s=>s.Remaining+1e-10>=cut&&(a.Allowed==null||a.Allowed.Contains(s.Length))).OrderBy(s=>s.Remaining).ThenBy(s=>s.Length).ToList()){
     if(pieces==0)break;long fit=(long)Math.Floor((bin.Remaining+1e-10)/cut);while(fit>0&&fit*cut>bin.Remaining+1e-9)fit--;if(fit<=0)continue;
     long full=Math.Min(bin.Count,pieces/fit);
     if(full>0){var used=bin.Copy(full);bin.Count-=full;used.Remaining=Math.Max(0,used.Remaining-fit*cut);used.Used[a]=fit*cut;stock.Add(used);pieces-=checked(full*fit);}
     if(pieces>0&&bin.Count>0){long take=Math.Min(fit,pieces);var used=bin.Copy(1);bin.Count--;used.Remaining=Math.Max(0,used.Remaining-take*cut);used.Used[a]=take*cut;stock.Add(used);pieces-=take;}
    }
    if(pieces>0){long fit=(long)Math.Floor((a.Choice.Length+1e-10)/cut);if(fit<1)throw new Exception("Rebar segment does not fit selected stock.");long full=pieces/fit,tail=pieces%fit;if(full>0){var bin=new RebarStock{Grade=material.Key.Grade,Diameter=material.Key.Diameter,Length=a.Choice.Length,Remaining=Math.Max(0,a.Choice.Length-fit*cut),Count=full};bin.Used.Add(a,fit*cut);stock.Add(bin);}if(tail>0){var bin=new RebarStock{Grade=material.Key.Grade,Diameter=material.Key.Diameter,Length=a.Choice.Length,Remaining=Math.Max(0,a.Choice.Length-tail*cut),Count=1};bin.Used.Add(a,tail*cut);stock.Add(bin);}}
   }
   foreach(var bin in stock.Where(s=>s.Count>0)){
    double used=bin.Used.Values.Sum(),mass=material.First().UnitMass;foreach(var pair in bin.Used){var a=pair.Key;a.Stocks.Add(bin);a.Waste+=bin.Remaining*bin.Count*pair.Value/used*mass;}
   }
   foreach(var a in material){a.Splice=a.Demand.Pieces*(a.Choice.Segments-1)*a.Choice.Lap*a.UnitMass;a.Purchase=a.Net+a.Splice+a.Waste;}
  }
 }
}
}
