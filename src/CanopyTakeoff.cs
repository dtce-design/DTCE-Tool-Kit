using System;using System.Linq;using System.Globalization;
namespace BeamScheduleFormatter {
class CanopyTakeoff {
 public string Id,Description;public double Length,Projection,Thickness,Concrete,SoffitForms,EdgeForms,Fc;public int Quantity;public EstimateInput Steel;
 public double Forms{get{return SoffitForms+EdgeForms;}}
 public static CanopyTakeoff Calculate(string id,string description,double length,double projection,double thicknessMm,int quantity,double coverMm,FootingBarSpec main,FootingBarSpec distribution,double anchorageMm,double distributionEndsMm,bool bottomMat,LengthConfiguration cfg){
  if(string.IsNullOrWhiteSpace(id)||string.IsNullOrWhiteSpace(description)||cfg==null||main==null||distribution==null)throw new Exception("Enter canopy/ledge ID, description and reinforcement.");
  SteelWeight.Positive(length,projection,thicknessMm,anchorageMm);SteelWeight.Count(quantity);QuantityMath.Finite(coverMm,distributionEndsMm);if(coverMm<0||distributionEndsMm<0||2*coverMm/1000>=Math.Min(length,projection))throw new Exception("Cover must fit length/projection; end allowance cannot be negative.");
  if(main.Diameter<=0||distribution.Diameter<=0||2*coverMm+(main.Diameter+distribution.Diameter)*(bottomMat?2:1)>thicknessMm)throw new Exception("Thickness must fit cover and the specified crossing rebar layers.");
  var result=new CanopyTakeoff{Id=id,Description=description,Length=length,Projection=projection,Thickness=thicknessMm/1000,Quantity=quantity,Fc=cfg.Input.Fc};
  result.Concrete=length*projection*result.Thickness*quantity;result.SoffitForms=length*projection*quantity;result.EdgeForms=(length+2*projection)*result.Thickness*quantity;
  result.Steel=new EstimateInput{Beam=id,Description=description,Quantity=quantity.ToString(CultureInfo.InvariantCulture),Length=length.ToString("R",CultureInfo.InvariantCulture),Result=new BeamEstimate()};
  for(int layer=0;layer<(bottomMat?2:1);layer++)for(int direction=0;direction<2;direction++){
   var spec=direction==0?main:distribution;double across=direction==0?length:projection;if(spec.Count<0||spec.Spacing<0||(spec.Count>0)==(spec.Spacing>0))throw new Exception("Enter spacing OR bar count for each direction, not both.");QuantityMath.Finite(spec.Spacing);double range=across-2*coverMm/1000-spec.Diameter/1000.0;if(range<0)throw new Exception("Rebar diameter does not fit the available width and cover.");int count=spec.Count>0?spec.Count:checked((int)Math.Ceiling(range/(spec.Spacing/1000)-1e-10)+1);SteelWeight.Count(count);
   double raw=direction==0?projection-coverMm/1000+anchorageMm/1000:length-2*coverMm/1000+distributionEndsMm/1000;double cut=Math.Ceiling(raw/.025-1e-10)*.025,mass=EstimatorEngine.UnitMass(spec.Diameter),metres=count*cut*quantity;string part=(layer==0?"Top ":"Bottom ")+(direction==0?"MAIN / projection":"DISTRIBUTION / along wall");
   result.Steel.Result.Lines.Add(new EstimateLine{Beam=id,Part=part,Diameter=spec.Diameter,Grade=spec.Diameter<=12?cfg.GradeSmall:cfg.GradeLarge,PiecesPerBeam=count,CutLength=cut,Metres=metres,Kg=metres*mass,Formula=(direction==0?"Main cut = projection - front cover + entered wall anchorage":"Distribution cut = length - 2 end covers + total end allowance")+"; raw "+raw.ToString("0.000",CultureInfo.InvariantCulture)+"m; rounded up to 25mm; "+count+" pcs/unit x "+quantity});
  }
  return result;
 }
}
}
