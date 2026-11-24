using System;using System.Collections.Generic;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
/* Static lateral force procedure, NSCP 2015 Section 208.5.2 (owner, 24 November 2026: "sa earthquake load dapat lahat ng parameters sa
   Lateral Force Procedure ibigay din sa report"):
     T  = Ct hn^(3/4)                          (208-12, Method A)
     V  = Cv I W / (R T)                       (208-8)
     V <= 2.5 Ca I W / R                       (208-9)
     V >= 0.11 Ca I W                          (208-10)
     V >= 0.8 Z Nv I W / R  (Zone 4)           (208-11)
     Ft = 0.07 T V <= 0.25 V, Ft = 0 for T <= 0.7 s   (Section 208.5.5)
     Fx = (V - Ft) wx hx / Σ wi hi              (Section 208.5.5)
     E  = ρ Eh + Ev, Ev = 0.5 Ca I D, Em = Ω0 Eh   (208-1, 208-2, 208-3)
   Levels are entered from the first level above the base up as "height m @ weight kN". */
sealed class LateralLevel {public string Name;public double H,W,WH,F,Shear,Moment;}
sealed class StaticLateralResult {
 public double Z,Ca,Cv,Na,Nv,I,R,Ct,Hn,W,T,V1,Vmax,Vmin,VminZone4,V,Ft,SumWH,Omega,Rho,Overturning,RequiredDynamic,DynamicV,DynamicFactor;
 public int Zone;public bool Regular;public string Governing;public List<LateralLevel> Levels=new List<LateralLevel>();
 public double Cs{get{return W>0?V/W:0;}}
}
static class StaticLateralForce {
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;
 internal static readonly string[] Systems={"Steel moment-resisting frame (Ct = 0.0853)","Reinforced concrete moment-resisting frame or eccentrically braced frame (Ct = 0.0731)","All other buildings (Ct = 0.0488)"};
 internal static double CtOf(string system){return system!=null&&system.StartsWith("Steel")?0.0853:system!=null&&system.StartsWith("Reinforced")?0.0731:0.0488;}
 /* "3.5@1200, 7@1200, 10@900" -> levels from the base up */
 internal static List<LateralLevel> Parse(string text){var list=new List<LateralLevel>();int n=0;
  foreach(var part in (text??"").Split(new[]{',',';','\n'},StringSplitOptions.RemoveEmptyEntries)){var p=part.Trim();if(p=="")continue;var hw=p.Split('@');double h,w;
   if(hw.Length!=2||!double.TryParse(hw[0].Trim(),NumberStyles.Float,Ci,out h)||!double.TryParse(hw[1].Trim(),NumberStyles.Float,Ci,out w))throw new Exception("Storey levels: write each level as height m @ weight kN, e.g. 3.5@1200, 7@1200, 10@900.");
   if(h<=0||w<0)throw new Exception("Storey levels: heights must be above the base and weights zero or more.");list.Add(new LateralLevel{Name="Level "+(++n).ToString(Ci),H=h,W=w});}
  if(list.Count==0)throw new Exception("Enter the storey levels (height m @ weight kN).");
  list=list.OrderBy(l=>l.H).ToList();for(int i=0;i<list.Count;i++)list[i].Name="Level "+(i+1).ToString(Ci);return list;}
 internal static StaticLateralResult Compute(int zone,double ca,double cv,double na,double nv,double i,double r,string system,string levels,double omega,double rho,bool regular,double dynamicV){
  var s=new StaticLateralResult{Zone=zone,Z=zone==4?0.40:0.20,Ca=ca,Cv=cv,Na=na,Nv=nv,I=i,R=r,Ct=CtOf(system),Omega=omega,Rho=rho,Regular=regular,DynamicV=dynamicV};
  if(omega<=0)throw new Exception("Ω0 must be greater than zero.");if(rho<1)throw new Exception("ρ must be at least 1.0.");
  s.Levels=Parse(levels);s.Hn=s.Levels.Max(l=>l.H);s.W=s.Levels.Sum(l=>l.W);if(s.W<=0)throw new Exception("The total seismic weight W must be greater than zero.");
  s.T=s.Ct*Math.Pow(s.Hn,0.75);
  s.V1=cv*i*s.W/(r*s.T);s.Vmax=2.5*ca*i*s.W/r;s.Vmin=0.11*ca*i*s.W;s.VminZone4=zone==4?0.8*s.Z*nv*i*s.W/r:0;
  double v=Math.Min(s.V1,s.Vmax);s.Governing=s.V1<=s.Vmax?"Eq. 208-8 (Cv I W / R T)":"Eq. 208-9 upper limit (2.5 Ca I W / R)";
  if(s.Vmin>v){v=s.Vmin;s.Governing="Eq. 208-10 lower limit (0.11 Ca I W)";}
  if(s.VminZone4>v){v=s.VminZone4;s.Governing="Eq. 208-11 Zone 4 lower limit (0.8 Z Nv I W / R)";}
  s.V=v;s.Ft=s.T<=0.7?0:Math.Min(0.07*s.T*v,0.25*v);
  foreach(var l in s.Levels)l.WH=l.W*l.H;s.SumWH=s.Levels.Sum(l=>l.WH);
  foreach(var l in s.Levels)l.F=s.SumWH>0?(v-s.Ft)*l.WH/s.SumWH:0;
  /* storey shear and overturning moment at each level: Ft acts at the top (hn) */
  for(int k=0;k<s.Levels.Count;k++){var l=s.Levels[k];var above=s.Levels.Skip(k).ToList();l.Shear=s.Ft+above.Sum(a=>a.F);l.Moment=s.Ft*(s.Hn-l.H)+above.Sum(a=>a.F*(a.H-l.H));}
  s.Overturning=s.Ft*s.Hn+s.Levels.Sum(l=>l.F*l.H);
  s.RequiredDynamic=(regular?0.9:1.0)*v;s.DynamicFactor=dynamicV>0?Math.Max(1,s.RequiredDynamic/dynamicV):0;
  return s;}
}
}
