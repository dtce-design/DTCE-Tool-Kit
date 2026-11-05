using System;using BeamScheduleFormatter;
class BeamQuantitiesTests {
 static void Near(double actual,double expected,string message){if(Math.Abs(actual-expected)>1e-9)throw new Exception(message);}
 static void Main(){
  var elevated=BeamQuantities.Calculate(300,600,5,2,false,150);
  Near(elevated.Concrete,1.8,"Concrete unchanged");Near(elevated.SideForms,12,"Both beam sides use full depth");Near(elevated.BottomForms,3,"Elevated beam soffit");Near(elevated.Formwork,15,"Full sides plus soffit");
  var ground=BeamQuantities.Calculate(300,600,5,2,true,0);Near(ground.Formwork,12,"Ground beam sides only");Near(ground.BottomForms,0,"No ground soffit");
  Near(BeamQuantities.Calculate(300,600,5,2,false,600).Formwork,15,"Legacy slab deduction ignored");
  foreach(double width in new[]{0,-1,double.NaN,double.PositiveInfinity}){bool rejected=false;try{BeamQuantities.Calculate(width,600,5,2,false,0);}catch{rejected=true;}if(!rejected)throw new Exception("Invalid width accepted");}
  Console.WriteLine("PASS: full-depth beam side forms, soffit by support, legacy deduction and invalid geometry.");
 }
}
