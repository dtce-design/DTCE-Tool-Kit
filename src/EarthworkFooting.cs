using System;using System.Collections.Generic;using System.Linq;using System.Globalization;using System.Text.RegularExpressions;
namespace BeamScheduleFormatter {
static class QuantityMath {
 public static void Finite(params double[] values){if(values.Any(x=>double.IsNaN(x)||double.IsInfinity(x)||x<0||x>1e9))throw new Exception("Use nonnegative finite quantities.");}
 public static double Station(string text){var m=Regex.Match((text??"").Trim(),@"^(\d+)\+(\d+(?:\.\d+)?)$");double value;if(m.Success){double offset=double.Parse(m.Groups[2].Value,CultureInfo.InvariantCulture);if(offset>=1000)throw new Exception("Station offset must be below 1000m.");value=double.Parse(m.Groups[1].Value,CultureInfo.InvariantCulture)*1000+offset;}else if(!double.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out value))throw new Exception("Station: enter metres or 0+020.");Finite(value);return value;}
}
class EarthworkMaterial {
 public string Id,Source;public double Bank,Fill,Swell=20,Shrink=10,Reusable=100;
 public double LooseAvailable{get{return Bank*Reusable/100*(1+Swell/100);}}
 public double CompactedAvailable{get{return Bank*Reusable/100*(1-Shrink/100);}}
 public void Validate(){QuantityMath.Finite(Bank,Fill,Swell,Shrink,Reusable);if(Swell>200||Shrink>=100||Reusable>100)throw new Exception("Soil factors: swell 0-200%, bank shrinkage below 100%, suitable soil 0-100%.");}
}
class CutFillInterval {
 public string Alignment;public double Start,End,CutStart,CutEnd,FillStart,FillEnd;
 public double Cut{get{return (CutStart+CutEnd)/2*(End-Start);}}public double Fill{get{return (FillStart+FillEnd)/2*(End-Start);}}
 public void Validate(){QuantityMath.Finite(Start,End,CutStart,CutEnd,FillStart,FillEnd);if(string.IsNullOrWhiteSpace(Alignment)||End<=Start)throw new Exception("Enter alignment ID and station end greater than start.");}
 public static void CheckOverlap(IEnumerable<CutFillInterval> list){foreach(var group in list.GroupBy(x=>x.Alignment.Trim(),StringComparer.OrdinalIgnoreCase)){double previous=-1;foreach(var row in group.OrderBy(x=>x.Start)){row.Validate();if(row.Start<previous-1e-8)throw new Exception("Overlapping station intervals: "+group.Key);previous=row.End;}}}
}
class EarthworkBalance {
 public double Excavation,Fill,LooseFromExcavation,LooseCommonBorrow,LooseSurplus,UnsuitableLoose,AvailableCompacted;public double BorrowSwell,BorrowShrink;
 public static EarthworkBalance Calculate(IEnumerable<EarthworkMaterial> data,double borrowSwell=20,double borrowShrink=10){var rows=data.ToList();foreach(var row in rows)row.Validate();new EarthworkMaterial{Swell=borrowSwell,Shrink=borrowShrink}.Validate();var b=new EarthworkBalance{BorrowSwell=borrowSwell,BorrowShrink=borrowShrink,Excavation=rows.Sum(x=>x.Bank),Fill=rows.Sum(x=>x.Fill),AvailableCompacted=rows.Sum(x=>x.CompactedAvailable)};double remaining=b.Fill;foreach(var row in rows){double use=Math.Min(remaining,row.CompactedAvailable);b.LooseFromExcavation+=use/(1-row.Shrink/100)*(1+row.Swell/100);remaining=Math.Max(0,remaining-use);b.LooseSurplus+=Math.Max(0,row.LooseAvailable-use/(1-row.Shrink/100)*(1+row.Swell/100));b.UnsuitableLoose+=row.Bank*(1-row.Reusable/100)*(1+row.Swell/100);}b.LooseCommonBorrow=remaining/(1-borrowShrink/100)*(1+borrowSwell/100);return b;}
}
class FootingBarSpec {public int Diameter;public double Spacing;public int Count;}
class FootingTakeoff {
 public string Id,Description;public double Length,Width,Thickness,Depth,Concrete,Forms,Excavation,Gravel,Backfill;public int Quantity;public EstimateInput Steel;public EarthworkMaterial Soil;
 public static FootingTakeoff Calculate(string id,string description,double length,double width,double thickness,int quantity,double depth,double coverMm,bool formed,FootingBarSpec[] bars,double endExtra,LengthConfiguration configuration,double swell,double shrink,double reusable){
  QuantityMath.Finite(length,width,thickness,depth,coverMm,endExtra);if(string.IsNullOrWhiteSpace(id)||string.IsNullOrWhiteSpace(description)||length<=0||width<=0||thickness<=0||quantity<1||quantity>100000||depth<thickness+.1-1e-8||2*coverMm/1000>=Math.Min(length,width)||2*coverMm/1000>=thickness||bars.Length!=4||configuration==null)throw new Exception("Check footing ID/description, positive dimensions/quantity, depth >= T+0.10m and cover fitting footing dimensions.");
  var r=new FootingTakeoff{Id=id,Description=description,Length=length,Width=width,Thickness=thickness,Depth=depth,Quantity=quantity};r.Concrete=length*width*thickness*quantity;r.Forms=formed?2*(length+width)*thickness*quantity:0;r.Excavation=(length+.6)*(width+.6)*depth*quantity;r.Gravel=length*width*.1*quantity;r.Backfill=r.Excavation-r.Concrete-r.Gravel;
  r.Soil=new EarthworkMaterial{Id=id,Source="Footing",Bank=r.Excavation,Fill=r.Backfill,Swell=swell,Shrink=shrink,Reusable=reusable};r.Soil.Validate();r.Steel=new EstimateInput{Beam=id,Description=description,Quantity=quantity.ToString(CultureInfo.InvariantCulture),Length=length.ToString("R",CultureInfo.InvariantCulture),Result=new BeamEstimate()};
  for(int n=0;n<4;n++){var bar=bars[n];QuantityMath.Finite(bar.Diameter,bar.Spacing,bar.Count);if(bar.Diameter==0){if(bar.Count!=0||bar.Spacing!=0)throw new Exception("Unused bar group must have blank spacing/count.");continue;}if((bar.Count>0)==(bar.Spacing>0))throw new Exception("Enter spacing OR quantity for each bar group, not both.");double run=n%2==0?length:width,across=n%2==0?width:length;double centreRange=across-2*coverMm/1000-bar.Diameter/1000.0;if(centreRange<0)throw new Exception("Bar diameter does not fit footing cover.");int count=bar.Count>0?bar.Count:checked((int)Math.Ceiling(centreRange/(bar.Spacing/1000)-1e-10)+1);if(count<1||count>100000)throw new Exception("Bar count outside supported range.");double cut=run-2*coverMm/1000+endExtra;if(cut<=0)throw new Exception("Invalid bar cut length.");int grade=bar.Diameter<=12?configuration.GradeSmall:configuration.GradeLarge;double metres=count*cut*quantity;string part=(n<2?"Bottom ":"Top ")+(n%2==0?"X":"Y");r.Steel.Result.Lines.Add(new EstimateLine{Beam=id,Part=part,Diameter=bar.Diameter,Grade=grade,PiecesPerBeam=count,CutLength=cut,Metres=metres,Kg=metres*EstimatorEngine.UnitMass(bar.Diameter),Formula=count+" pcs/footing x "+cut.ToString("0.00",CultureInfo.InvariantCulture)+"m x "+quantity});}
  if(r.Steel.Result.Lines.Count==0)throw new Exception("Enter at least one reinforcement group.");return r;
 }
}
}
