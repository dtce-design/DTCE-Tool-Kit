using System;using System.Globalization;using System.Linq;using System.Text;
namespace BeamScheduleFormatter {
// Quantities use bank -> loose swell and bank -> compacted shrinkage independently.
class BeamEarthworks {
 public const double WorkingSpacePerSide=.30,DefaultSwellPercent=20;
 public double FromExcavation,CommonBorrow;
 public double LooseFromExcavation{get{return Math.Min(LooseExcavation,LooseRequired);}}
 public double Depth,Width,Length,Allowance,Swell,Shrink,Excavation,LooseExcavation,Concrete,Gravel,OtherConcrete,Backfill,LooseRequired,LooseSurplus,LooseImport;
 public static BeamEarthworks Calculate(double b,double d,double length,int qty,double depth,double allowance,double swell,double shrink,double other){
  double[] values={b,d,length,depth,allowance,swell,shrink,other};
  if(values.Any(x=>double.IsNaN(x)||double.IsInfinity(x))||b<=0||d<=0||length<=0||qty<=0||qty>100000||depth<d+.10-1e-9||allowance<0||swell<0||swell>200||shrink<0||shrink>=100||other<0)throw new Exception("Earthworks: depth must be at least D + 0.10m; allowance >=0; swell 0-200%; bank shrinkage 0 to <100%; check length and quantity.");
  var r=new BeamEarthworks{Depth=depth,Width=b+2*allowance,Length=length,Allowance=allowance,Swell=swell,Shrink=shrink};
  r.Excavation=r.Width*depth*length*qty;r.Concrete=b*d*length*qty;r.Gravel=b*.10*length*qty;r.OtherConcrete=other*qty;
  if(r.Concrete+r.Gravel+r.OtherConcrete>r.Excavation+1e-8)throw new Exception("Concrete + gravel exceed excavation. Check additional buried concrete volume.");
  r.Backfill=Math.Max(0,r.Excavation-r.Concrete-r.Gravel-r.OtherConcrete);
  r.LooseExcavation=r.Excavation*(1+swell/100);r.LooseRequired=r.Backfill/(1-shrink/100)*(1+swell/100);
  r.FromExcavation=Math.Min(r.Backfill,r.Excavation*(1-shrink/100));r.CommonBorrow=Math.Max(0,r.Backfill-r.FromExcavation);
  r.LooseSurplus=Math.Max(0,r.LooseExcavation-r.LooseRequired);r.LooseImport=Math.Max(0,r.LooseRequired-r.LooseExcavation);return r;
 }
 public void Report(StringBuilder b,bool detail){
  b.AppendLine(string.Format(CultureInfo.InvariantCulture,"  Excavation {0:0.00} bank m3 | Gravel {2:0.00} m3",Excavation,Backfill,Gravel));
  b.AppendLine(string.Format(CultureInfo.InvariantCulture,"  Embankment: from excavation {0:0.00} + COMMON BORROW {1:0.00} = {2:0.00} loose m3",LooseFromExcavation,LooseImport,LooseRequired));
  if(detail){b.AppendLine(string.Format(CultureInfo.InvariantCulture,"  Volume basis: compacted space {0:0.00} m3; loose required {1:0.00} m3; loose surplus {2:0.00} m3",Backfill,LooseRequired,LooseSurplus));b.AppendLine(string.Format(CultureInfo.InvariantCulture,"  Trench W = B + 2({0:0.00}) = {1:0.00}m; depth {2:0.00}m; L {3:0.00}m per beam",Allowance,Width,Depth,Length));b.AppendLine(string.Format(CultureInfo.InvariantCulture,"  Backfill = {0:0.00} excavation - {1:0.00} beam - {2:0.00} gravel - {3:0.00} other concrete",Excavation,Concrete,Gravel,OtherConcrete));b.AppendLine(string.Format(CultureInfo.InvariantCulture,"  Loose = bank x {0:0.00}; compacted = bank x {1:0.00}",1+Swell/100,1-Shrink/100));}
 }
}
}
