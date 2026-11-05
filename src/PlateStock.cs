using System;using System.Collections.Generic;using System.Linq;using System.Globalization;
namespace BeamScheduleFormatter {
class PlateFormat {
 public string Name;public double Width,Length;
 public double Area{get{return Width*Length;}}
}
static class PlateCatalog {
 public const string Source="Regan 2026 handbook PDF pp.36-38, printed pp.25-27; mild steel ASTM A36 / JIS G3101";
 public static readonly double[] Thicknesses={1.2,1.4,1.5,1.8,1.9,2,2.2,2.3,2.4,2.5,2.6,2.7,2.8,2.9,3,3.2,3.4,3.5,3.7,3.8,4,4.2,4.3,4.5,4.7,4.8,5,5.2,5.3,5.5,5.7,5.8,6,6.3,6.5,7,7.5,7.8,8,8.5,8.7,9,9.5,9.7,9.8,10,10.5,10.8,11,11.5,11.8,12,13,14,15,16,17,18,19,20,22,24,25,28,30,32,35,36,38,40,45,50,60,63,75,90,100};
 public static readonly PlateFormat[] Formats={Format(4,8),Format(5,20),Format(6,20),Format(8,20)};
 static PlateFormat Format(int width,int length){return new PlateFormat{Name=width+" ft x "+length+" ft",Width=width*.3048,Length=length*.3048};}
 public static bool HasThickness(double mm){return Thicknesses.Any(t=>Math.Abs(t-mm)<1e-8);}
}
class PlateDemand {public string Member;public double Length,Width,ThicknessMm;public int Quantity;public double Area{get{return Length*Width;}}}
class PlateRectangle {public double X,Y,Width,Length;}
class PlatePlacement:PlateRectangle {public PlateDemand Demand;}
class PlateSheet {
 public PlateFormat Format;public double ThicknessMm;
 public readonly List<PlateRectangle> Free=new List<PlateRectangle>();
 public readonly List<PlatePlacement> Cuts=new List<PlatePlacement>();
 public double NetArea{get{return Cuts.Sum(c=>c.Width*c.Length);}}
 public double BuyKg{get{return SteelWeight.Plate(Format.Area,ThicknessMm,1);}}
 public double WasteKg{get{return Math.Max(0,SteelWeight.Plate(Format.Area,ThicknessMm,1)-NetArea*ThicknessMm/1000*SteelWeight.Density);}}
}
static class PlateCutPlanner {
 const double Epsilon=1e-9;
 // Guillotine rectangles never overlap. Compare multiple orderings, split strategies and stock formats.
 // The best compared feasible layout is an estimate, not proof of a globally optimal nesting.
 public static List<PlateSheet> Allocate(IEnumerable<PlateDemand> source){
  var demands=source.ToList();if(demands.Sum(d=>(long)d.Quantity)>5000)throw new Exception("Plate fitting supports up to 5000 cuts per project.");
  foreach(var d in demands){SteelWeight.Positive(d.Length,d.Width,d.ThicknessMm);SteelWeight.Count(d.Quantity);if(!PlateCatalog.HasThickness(d.ThicknessMm))throw new Exception("Plate thickness "+d.ThicknessMm.ToString(CultureInfo.InvariantCulture)+" mm is not listed in the handbook.");if(!PlateCatalog.Formats.Any(f=>Fits(f.Width,f.Length,d.Width,d.Length)||Fits(f.Width,f.Length,d.Length,d.Width)))throw new Exception("Plate "+d.Member+" does not fit any handbook sheet, even when rotated.");}
  var result=new List<PlateSheet>();foreach(var group in demands.GroupBy(d=>d.ThicknessMm)){
   List<PlateSheet> best=null;double bestKg=double.MaxValue;
   for(int ordering=0;ordering<3;ordering++)for(int split=0;split<2;split++)for(int format=-1;format<PlateCatalog.Formats.Length;format++){
    var ordered=ordering==0?group.OrderByDescending(d=>d.Area):ordering==1?group.OrderByDescending(d=>Math.Max(d.Width,d.Length)):group.OrderByDescending(d=>Math.Min(d.Width,d.Length));
    var plan=Pack(ordered.ThenBy(d=>d.Member,StringComparer.Ordinal).ToList(),format,split);if(plan==null)continue;double kg=plan.Sum(s=>s.BuyKg);if(kg<bestKg-Epsilon||(Math.Abs(kg-bestKg)<Epsilon&&(best==null||plan.Count<best.Count))){best=plan;bestKg=kg;}
   }
   if(best==null)throw new Exception("Unable to fit plate cuts in the handbook sheets.");result.AddRange(best);
  }return result;
 }
 static bool Fits(double width,double length,double cutWidth,double cutLength){return cutWidth<=width+Epsilon&&cutLength<=length+Epsilon;}
 static List<PlateSheet> Pack(List<PlateDemand> demands,int fixedFormat,int split){
  var sheets=new List<PlateSheet>();foreach(var demand in demands)for(int count=0;count<demand.Quantity;count++){
   PlateSheet sheet=null;int freeIndex=-1;bool rotate=false;double best=double.MaxValue;
   foreach(var current in sheets)for(int n=0;n<current.Free.Count;n++){var free=current.Free[n];for(int turn=0;turn<2;turn++){double w=turn==0?demand.Width:demand.Length,l=turn==0?demand.Length:demand.Width;if(!Fits(free.Width,free.Length,w,l))continue;double score=free.Width*free.Length-demand.Area+Math.Min(free.Width-w,free.Length-l)*1e-6;if(score<best){best=score;sheet=current;freeIndex=n;rotate=turn==1;}}}
   if(sheet==null){
    var options=fixedFormat<0?PlateCatalog.Formats:new[]{PlateCatalog.Formats[fixedFormat]};
    // Mixed-stock candidate selects the smallest stock per repeatable grid yield for remaining cuts.
    var available=options.Where(f=>Fits(f.Width,f.Length,demand.Width,demand.Length)||Fits(f.Width,f.Length,demand.Length,demand.Width)).OrderBy(f=>f.Area/Math.Max(1,Math.Min(demand.Quantity-count,Math.Max(Math.Floor((f.Width+Epsilon)/demand.Width)*Math.Floor((f.Length+Epsilon)/demand.Length),Math.Floor((f.Width+Epsilon)/demand.Length)*Math.Floor((f.Length+Epsilon)/demand.Width))))).ThenBy(f=>f.Area).FirstOrDefault();
    if(available==null)return null;sheet=new PlateSheet{Format=available,ThicknessMm=demand.ThicknessMm};sheet.Free.Add(new PlateRectangle{Width=available.Width,Length=available.Length});sheets.Add(sheet);freeIndex=0;
    bool normal=Fits(available.Width,available.Length,demand.Width,demand.Length),turned=Fits(available.Width,available.Length,demand.Length,demand.Width);
    double normalYield=Math.Floor((available.Width+Epsilon)/demand.Width)*Math.Floor((available.Length+Epsilon)/demand.Length),turnedYield=Math.Floor((available.Width+Epsilon)/demand.Length)*Math.Floor((available.Length+Epsilon)/demand.Width);rotate=!normal||(turned&&turnedYield>normalYield);
   }
   var space=sheet.Free[freeIndex];sheet.Free.RemoveAt(freeIndex);double width=rotate?demand.Length:demand.Width,length=rotate?demand.Width:demand.Length;
   sheet.Cuts.Add(new PlatePlacement{Demand=demand,X=space.X,Y=space.Y,Width=width,Length=length});
   double remainingWidth=Math.Max(0,space.Width-width),remainingLength=Math.Max(0,space.Length-length);
   if(split==0){AddFree(sheet,space.X+width,space.Y,remainingWidth,space.Length);AddFree(sheet,space.X,space.Y+length,width,remainingLength);}
   else{AddFree(sheet,space.X+width,space.Y,remainingWidth,length);AddFree(sheet,space.X,space.Y+length,space.Width,remainingLength);}
  }return sheets;
 }
 static void AddFree(PlateSheet sheet,double x,double y,double width,double length){if(width>Epsilon&&length>Epsilon)sheet.Free.Add(new PlateRectangle{X=x,Y=y,Width=width,Length=length});}
}
}
