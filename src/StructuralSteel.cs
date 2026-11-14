using System;using System.Linq;using System.Collections.Generic;
namespace BeamScheduleFormatter {
static class SteelWeight {
 public const double Density=7850;
 public static double Plate(double area,double thicknessMm,int quantity){Positive(area,thicknessMm);Count(quantity);return area*thicknessMm/1000*Density*quantity;}
 public static double Rod(double diameterMm,double overallMetres,int quantity){Positive(diameterMm,overallMetres);Count(quantity);return Math.PI*Math.Pow(diameterMm/1000,2)/4*overallMetres*Density*quantity;}
 public static double Diagonal(double x,double y,double height){foreach(double value in new[]{x,y,height})if(double.IsNaN(value)||double.IsInfinity(value)||value<0||value>100000)throw new Exception("Bracing distances must be nonnegative finite metres.");double diagonal=Math.Sqrt(x*x+y*y+height*height);Positive(diagonal);return diagonal;}
 public static void Positive(params double[] values){if(values.Any(v=>double.IsNaN(v)||double.IsInfinity(v)||v<=0||v>1e12))throw new Exception("Enter positive finite dimensions and unit weights.");}
 public static void Count(int quantity){if(quantity<1||quantity>100000)throw new Exception("Quantity must be 1 to 100000 whole pieces.");}
}
class SteelCutDemand {public string Member,Specification;public double Length,KgM;public int Quantity;}
class SteelCut {public SteelCutDemand Demand;public int Piece;public double RemainingAfter;}
class SteelStock {public int Number;public string Specification;public double KgM,Length=6,Remaining=6;public readonly List<SteelCut> Cuts=new List<SteelCut>();public double Net{get{return Cuts.Sum(c=>c.Demand.Length);}}}
class SteelStockComparer:IComparer<SteelStock>{public int Compare(SteelStock a,SteelStock b){int c=a.Remaining.CompareTo(b.Remaining);return c!=0?c:a.Number.CompareTo(b.Number);}}
static class SteelCutPlanner {
 public const double StockLength=6;
 public static double PurchaseKg(IEnumerable<SteelStock> stocks,SteelCutDemand demand){return stocks.Sum(stock=>stock.Net<=0?0:stock.Length*stock.KgM*stock.Cuts.Where(c=>c.Demand==demand).Sum(c=>c.Demand.Length)/stock.Net);}

 public static List<SteelStock> Allocate(IEnumerable<SteelCutDemand> source,double stockLength=StockLength){SteelWeight.Positive(stockLength);var demands=source.ToList();if(demands.Sum(d=>(long)d.Quantity)>100000)throw new Exception("Cut plan supports up to 100000 pieces.");foreach(var d in demands){SteelWeight.Positive(d.Length,d.KgM);SteelWeight.Count(d.Quantity);if(string.IsNullOrWhiteSpace(d.Member)||string.IsNullOrWhiteSpace(d.Specification)||d.Length>stockLength+1e-9)throw new Exception("Each cut must fit the selected commercial stock length. Define an approved splice separately for longer members.");}var result=new List<SteelStock>();int number=1;
  foreach(var group in demands.GroupBy(d=>d.Specification,StringComparer.OrdinalIgnoreCase)){if(group.Any(d=>Math.Abs(d.KgM-group.First().KgM)>1e-8))throw new Exception("The same specification must have the same kg/m.");var bins=new SortedSet<SteelStock>(new SteelStockComparer());foreach(var demand in group.OrderByDescending(d=>d.Length).ThenBy(d=>d.Member,StringComparer.Ordinal)){for(int piece=1;piece<=demand.Quantity;piece++){var low=new SteelStock{Remaining=demand.Length-1e-9,Number=int.MinValue};var high=new SteelStock{Remaining=double.MaxValue,Number=int.MaxValue};var fit=bins.GetViewBetween(low,high);SteelStock stock=fit.Min;if(stock==null){stock=new SteelStock{Number=number++,Specification=group.Key,KgM=demand.KgM,Length=stockLength,Remaining=stockLength};result.Add(stock);}else bins.Remove(stock);stock.Remaining=Math.Max(0,stock.Remaining-demand.Length);stock.Cuts.Add(new SteelCut{Demand=demand,Piece=piece,RemainingAfter=stock.Remaining});bins.Add(stock);}}}
  return result;
 }
}
}

