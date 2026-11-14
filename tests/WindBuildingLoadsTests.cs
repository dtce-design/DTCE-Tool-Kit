using System;using System.Linq;using System.Collections.Generic;using BeamScheduleFormatter;
class WindBuildingLoadsTests {
 static int count;static void Check(bool ok,string why){if(!ok)throw new Exception(why);count++;}static void Near(double a,double b,double tolerance,string why){Check(Math.Abs(a-b)<=tolerance,why+": "+a+" vs "+b);}
 static List<WindDirection> Flat(string exposure="B"){return WindAssessment.Directions.Select(n=>new WindDirection{Name=n,Exposure=exposure,Shape="Low relief",K1=0,K2=0,K3=0,Kzt=1}).ToList();}
 static void Main(){var b=new WindBuildingInput{Width=12,Length=20,Eave=3,Apex=4,System="MRF Concrete",AutoG=false};var rows=WindBuildingLoads.Calculate(b,310,Flat());
  // Independent cached results from RCB Wind v.3.3.xlsm, Calculation sheet.
  // The workbook rounds Kz to .001 and pressures to Pa; this engine retains full precision.
  Near(b.Angle,9.462322208025617,1e-10,"Workbook C9 roof angle");Near(b.Height,3,1e-12,"Workbook C10 low-slope mean height");
  Near(WindBuildingLoads.Frequency(b),16.18,.005,"Workbook D17 frequency");
  Near(WindBuildingLoads.Gust(new WindBuildingInput{Width=12,Length=20,Eave=3,Apex=4,System="MRF Concrete",AutoG=true},"B",true,310),.861,.0005,"Workbook D42 normal gust");
  Near(WindBuildingLoads.Gust(new WindBuildingInput{Width=12,Length=20,Eave=3,Apex=4,System="MRF Concrete",AutoG=true},"B",false,310),.874,.0005,"Workbook D43 parallel gust");
  var wall=rows.First(r=>r.Surface=="Windward wall"&&Math.Abs(r.Z-3)<1e-8&&r.GCpi>0);Near(wall.Qh,2.222,.003,"Workbook D87 qh kPa");Near(wall.P,1.111,.003,"Workbook D114 wall positive internal");
  Near(rows.First(r=>r.Surface=="Windward wall"&&Math.Abs(r.Z-3)<1e-8&&r.GCpi<0).P,1.911,.003,"Workbook E114 negative internal sign");
  Near(rows.First(r=>r.Surface=="Leeward wall"&&r.Direction.StartsWith("E")&&r.GCpi>0).P,-1.344,.003,"Workbook C127 leeward");
  Check(rows.Select(r=>r.Direction).Distinct().Count()==4,"Four principal axes present");Check(rows.All(r=>Math.Abs(r.P-(r.Q*r.G*r.Cp-r.Qi*r.GCpi))<1e-12),"Every row satisfies net pressure equation");
  Check(rows.Where(r=>r.Surface.StartsWith("Roof zone")).All(r=>!r.Case.StartsWith("3–")&&!r.Case.StartsWith("6–")||r.Case.Contains("m from")),"Roof zones have distance ranges");
  Near(WindBuildingLoads.Leeward(1.5),-.4,1e-12,"Leeward interpolation 1–2");Near(WindBuildingLoads.Leeward(3),-.25,1e-12,"Leeward interpolation 2–4");
  Near(WindBuildingLoads.RoofNormal(20,.25,false),-.3,1e-12,"Workbook F140 roof negative branch");Near(WindBuildingLoads.RoofNormal(20,.25,true),.2,1e-12,"Workbook F141 roof positive branch");Near(WindBuildingLoads.RoofNormal(20,1,false),-.7,1e-12,"High-ratio row applies at h/L>=1");
  Near(WindBuildingLoads.RoofNormal(17.5,.375,false),-.475,1e-12,"Two-axis interpolation roof coefficients");
  Near(WindBuildingLoads.RoofZone(0,.75),-1.1,1e-12,"Roof zone height-ratio interpolation");
  Check(WindBuildingLoads.Calculate(b,310,Flat("Unresolved")).Count==0,"Missing exposure never yields pressures");
  var topography=Flat("C");foreach(var d in topography){d.Shape="Hill";d.H=20;d.Lh=60;d.X=0;var f=WindAssessment.Factors(20,60,0,3,"C","Hill");d.K1=f[0];d.K2=f[1];d.K3=f[2];d.Kzt=f[3];}
  var hill=WindBuildingLoads.Calculate(b,310,topography);Check(hill.First(r=>r.Z==0).Kzt>hill.First(r=>Math.Abs(r.Z-3)<1e-8).Kzt,"Kzt varies with wall elevation");
  b.Enclosure="Partially enclosed";b.OpeningHeight=2;rows=WindBuildingLoads.Calculate(b,310,Flat());Check(rows.All(r=>Math.Abs(Math.Abs(r.GCpi)-.55)<1e-12),"Partially enclosed internal coefficient");
  b.Eave=35;b.Apex=45;b.AutoG=true;b.FrequencyNormal=.5;b.FrequencyParallel=.7;Check(WindBuildingLoads.Calculate(b,310,Flat("C")).All(r=>WindTin.Finite(r.G)&&r.G>0),"Flexible gust response finite for both axes");
  b.Apex=10;bool rejected=false;try{WindBuildingLoads.Calculate(b,310,Flat());}catch{rejected=true;}Check(rejected,"Apex below eave rejected");
  Console.WriteLine("PASS: "+count+" wind pressure, workbook fixture, interpolation, sign and unresolved-data checks");
 }
}
