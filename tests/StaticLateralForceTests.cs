using System;using System.Linq;using BeamScheduleFormatter;
/* NSCP 2015 Section 208.5.2 static lateral force procedure, checked by hand */
static class StaticLateralForceTests {
 static int checks;
 static void Check(bool ok,string what){checks++;if(!ok){Console.WriteLine("FAIL: "+what);Environment.Exit(1);}}
 static void Near(double a,double b,double tol,string what){Check(Math.Abs(a-b)<=tol,what+" ("+a+" vs "+b+")");}
 static void Main(){
  /* Zone 4, SD, Na = Nv = 1: Ca 0.44, Cv 0.64; RC frame; W = 3300 kN; hn = 10.5 m */
  var s=StaticLateralForce.Compute(4,0.44,0.64,1,1,1,8.5,StaticLateralForce.Systems[1],"3.5@1200, 7@1200, 10.5@900",2.8,1.0,true,0);
  Near(s.Ct,0.0731,1e-9,"Ct of an RC moment frame");Near(s.Hn,10.5,1e-9,"hn = top level");Near(s.W,3300,1e-9,"W = sum of the level weights");
  Near(s.T,0.0731*Math.Pow(10.5,0.75),1e-9,"T = Ct hn^0.75");Near(s.T,0.4264,0.001,"T value");
  Near(s.V1,0.64*3300/(8.5*s.T),1e-6,"Eq. 208-8");Near(s.Vmax,2.5*0.44*3300/8.5,1e-6,"Eq. 208-9");Near(s.Vmin,0.11*0.44*3300,1e-6,"Eq. 208-10");Near(s.VminZone4,0.8*0.4*3300/8.5,1e-6,"Eq. 208-11");
  Near(s.V,427.0588,0.001,"V governed by the 208-9 upper limit");Check(s.Governing.Contains("208-9"),"governing equation named");
  Near(s.Ft,0,1e-12,"Ft = 0 for T <= 0.7 s");Near(s.SumWH,22050,1e-9,"sum wi hi");
  Near(s.Levels[2].F,427.0588*9450/22050,0.001,"F at the top level");Near(s.Levels.Sum(l=>l.F)+s.Ft,s.V,1e-6,"forces add up to V");
  Near(s.Levels[0].Shear,s.V,1e-6,"storey shear at the first storey = V");Near(s.Overturning,427.0588*172725/22050,0.01,"base overturning moment");
  Near(s.RequiredDynamic,0.9*s.V,1e-9,"regular: 90% of V");
  /* tall steel frame: Ft = 0.07 T V */
  var t=StaticLateralForce.Compute(4,0.44,0.64,1,1,1,8.0,StaticLateralForce.Systems[0],"10@2000, 20@2000, 30@2000, 40@1500",2.8,1.0,false,500);
  Near(t.T,0.0853*Math.Pow(40,0.75),1e-9,"steel frame period");Check(t.T>0.7,"long period");Near(t.Ft,Math.Min(0.07*t.T*t.V,0.25*t.V),1e-9,"Ft = 0.07 T V <= 0.25 V");
  Near(t.Levels.Sum(l=>l.F)+t.Ft,t.V,1e-6,"forces with Ft add up to V");Near(t.RequiredDynamic,t.V,1e-9,"irregular: 100% of V");Near(t.DynamicFactor,Math.Max(1,t.V/500),1e-9,"STAAD scale factor");
  /* Zone 2: no 208-11 limit */
  var z2=StaticLateralForce.Compute(2,0.28,0.4,1,1,1.5,8.5,StaticLateralForce.Systems[2],"4@500",2.8,1.0,true,0);Near(z2.VminZone4,0,1e-12,"Zone 2 has no Eq. 208-11");Near(z2.Ct,0.0488,1e-9,"other buildings Ct");
  bool threw=false;try{StaticLateralForce.Parse("3.5-1200");}catch(Exception){threw=true;}Check(threw,"bad level text is refused");
  Console.WriteLine("PASS: "+checks+" static lateral force checks");}
}
