using System;using BeamScheduleFormatter;
class ChamberTakeoffTests {
 static int checks;static void Near(double a,double b,string why){checks++;if(Math.Abs(a-b)>1e-9)throw new Exception(why+" (got "+a+", expected "+b+")");}
 static void Main(){
  // Concrete septic vault 2.0 × 1.5 m outside, 1.2 m clear height, 0.15 m walls/base/cover, 0.6 × 0.6 m hatch.
  var t=new ChamberTakeoff{Id="SV1",Description="Septic vault",Kind="Septic Vault, Concrete",L=2,W=1.5,H=1.2,Count=1,Wall=.15,Base=.15,Cover=.15,OpeningL=.6,OpeningW=.6,PlasterFaces=1,WaterproofFaces=1};t.Calculate();
  double clear=1.7*1.2;Near(t.CoverConcrete,(3-.36)*.15,"cover concrete over full footprint less hatch");
  Near(t.CoverForms,(clear-.36)+7*.15+2.4*.15,"cover soffit over clear area only, plus outer and hatch edges");
  Near(t.BaseConcrete,3*.15,"base concrete");Near(t.WallConcrete,(3-clear)*1.2,"wall concrete");Near(t.BaseForms,7*.15,"base edge forms");
  Near(t.WallForms,7*1.2+5.8*1.2,"inner and outer wall faces");Near(t.PlasterArea,5.8*1.2,"interior plaster");
  Console.WriteLine("PASS: "+checks+" chamber concrete/forms/plaster checks");}
}
