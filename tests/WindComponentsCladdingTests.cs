using System;using System.Linq;using BeamScheduleFormatter;
/* NSCP 2015 Section 207E Part 1 components and cladding, checked against the figure values */
static class WindComponentsCladdingTests {
 static int checks;static void Check(bool ok,string what){checks++;if(!ok){Console.WriteLine("FAIL: "+what);Environment.Exit(1);}}
 static void Near(double a,double b,double tol,string what){Check(Math.Abs(a-b)<=tol,what+" ("+a+" vs "+b+")");}
 static void Main(){
  /* GCp end points and log interpolation */
  Near(WindComponentsCladding.RoofNeg(0.93,3,20),-2.6,1e-3,"roof 7-27 deg zone 3 at 10 sf");Near(WindComponentsCladding.RoofNeg(9.3,3,20),-2.0,1e-9,"roof 7-27 deg zone 3 at 100 sf");
  Near(WindComponentsCladding.RoofNeg(Math.Sqrt(0.9290304*9.290304),2,20),-1.45,1e-6,"zone 2 at the log midpoint");
  Near(WindComponentsCladding.RoofNeg(0.5,3,5),-2.8,1e-9,"flat roof zone 3");Near(WindComponentsCladding.RoofPos(0.5,35),0.9,1e-9,"steep roof positive");
  Near(WindComponentsCladding.WallNeg(0.93,5,20),-1.4,1e-3,"wall zone 5");Near(WindComponentsCladding.WallNeg(0.93,5,8),-1.26,1e-3,"wall x 0.9 for slope <= 10 deg");Near(WindComponentsCladding.WallPos(100,20),0.7,1e-9,"wall positive at 500 sf");
  /* edge width a */
  Near(WindComponentsCladding.EdgeWidth(12,7),1.2,1e-9,"a = 0.1 least");Near(WindComponentsCladding.EdgeWidth(40,4),1.6,1e-9,"a = 0.4 h");Near(WindComponentsCladding.EdgeWidth(6,2),0.9,1e-9,"a >= 0.9 m");
  /* full calculation: roof elements get roof zones only, walls wall zones only; p = qh (GCp - GCpi) */
  var dirs=WindAssessment.Directions.Select((n,i)=>new WindDirection{Name=n,Exposure=i==5?"D":"C",Shape="Low relief",K1=0,K2=0,K3=0,Kzt=1}).ToList();
  var b=new WindBuildingInput{Width=12,Length=20,Eave=6,Apex=8,System="MRF Concrete",AutoG=false,UserG=0.85};var rows=WindBuildingLoads.Calculate(b,324.1,dirs);
  var s4=WindComponentsCladding.Calculate(b,rows,"Roof sheet @ 0.93, Girt @ 9.3");Check(s4.Exposure=="C","four principal axes only: exposure C");var s=WindComponentsCladding.Calculate(b,rows,"Roof sheet @ 0.93, Girt @ 9.3",dirs,324.1);Check(s.Exposure=="D"&&s.Direction=="SW"&&s.Qh>s4.Qh,"all eight directions: exposure D from SW governs");
  Check(s.Rows.Count(r=>r.Element=="Roof sheet")==3&&s.Rows.Where(r=>r.Element=="Roof sheet").All(r=>r.Zone.StartsWith("Roof")),"roof sheet: roof zones 1-3");
  Check(s.Rows.Count(r=>r.Element=="Girt")==2&&s.Rows.Where(r=>r.Element=="Girt").All(r=>r.Zone.StartsWith("Wall")),"girt: wall zones 4-5");
  var corner=s.Rows.First(r=>r.Zone.StartsWith("Roof zone 3"));Near(corner.PNeg,s.Qh*(-2.6-0.18),0.01,"corner uplift p = qh (GCp - GCpi)");Near(corner.PPos,s.Qh*(0.5+0.18),0.01,"positive p = qh (GCp + GCpi)");
  bool threw=false;try{WindComponentsCladding.Calculate(new WindBuildingInput{Width=20,Length=30,Eave=20,Apex=22,System="MRF Concrete"},rows,"Roof @ 1");}catch(Exception){threw=true;}Check(threw,"h > 18 m is refused (Part 1)");
  Console.WriteLine("PASS: "+checks+" components and cladding checks");}
}
