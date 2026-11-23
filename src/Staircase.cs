using System;using System.Collections.Generic;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
/* Staircase design sheet (owner, 22 November 2026: "staircase design, lahat ng possible stairs design option. gawin interactive my
   diagrams models etc"). NSCP 2015 (ACI 318-14) reinforced concrete stairs of every usual arrangement:
   - waist-slab stairs spanning along the flight: straight flight, dog-legged (two flights and a half landing), open-well (three
     flights and two landings) - each flight with its landings is a one-way slab on its supports (simply supported, one end or both ends
     continuous), loads per plan area;
   - stairs spanning across the flight: between walls / beams (each step spans the stair width), on two stringer beams (the waist spans
     across, the stringers span along the flight), cantilever steps from a wall or a spine beam;
   - slabless (folded plate / sawtooth) stairs spanning along the flight;
   - spiral stairs of cantilever treads from a central column.
   Loads: self weight (waist on the slope, steps, landings, unit weight 23.6 kN/m3), finishes on the treads and risers, live load per
   NSCP Table 205-1 (stairs and exitways 4.8 kPa) and the 1.33 kN concentrated tread load; U = 1.2D + 1.6L (405.3.1). Design per metre
   width or per step: flexure (422.2, 409.5), minimum steel 0.0018 bh (407.6.1 / 424.4.3) and spacing 3h / 450 (407.7.2.3), one-way shear
   without stirrups (422.5.5), minimum thickness for deflection (Table 407.3.1.1), distribution bars 5h / 450; geometry (riser,
   tread, 2R + T, width, headroom) against the limits the user sets (PD 1096 / RA 9514 practice). */
class StairInput {
 public string Type="Dog-legged (two flights, half landing)",Support="Simply supported",Landing="Landing spans with the flight";
 public double Riser=175,Tread=275,Width=1200,Waist=175,LandingT=150,BottomLanding=0,TopLanding=0,MidLanding=1300,Gap=100,Nosing=0;
 public int N1=9,N2=9,N3=6;
 public double LL=4.8,Finish=1.1,Gamma=23.6,Point=1.33,Rail=0.5;
 public double Fc=21,Fy=414,Fyt=275,Cover=20,MainDia=12,DistDia=10;
 public double StringerB=250,StringerH=450,StringerDia=16,StirrupDia=10;
 public double ColumnDia=300,OuterRadius=1200,TreadsPerTurn=16;
 /* supports (owner, 23 November 2026: "pwede mag assign ng beam as support tapos sa location niya sa landing"): beams b x h, and the
    landing beam at a distance from the flight (0 = at the landing end); the part of a landing beyond its beam is an overhang */
 /* top bars along the whole flight (owner, 23 November 2026: "wala na talagang straight bar yung top bars?") or at the supports only */public string TopBars="Full length";public bool TopFull{get{return !(TopBars??"").StartsWith("At the supports");}}
 public string Supports="Beams";public double BeamB=250,BeamH=400,LandingBeamAt=0;
 public bool OnBeams{get{return (Supports??"").StartsWith("Beams");}}
 /* the span part of a landing of the given depth (mm) and its overhang beyond the beam */
 public double LandingSpan(double depth){return LandingBeamAt>0&&LandingBeamAt<depth?LandingBeamAt:depth;}public double LandingOver(double depth){return depth-LandingSpan(depth);}
 public double MaxRiser=190,MinTread=250,MinWidth=1100,Min2RT=550,Max2RT=700,Headroom=2100;
 public bool Spiral{get{return Type.StartsWith("Spiral");}}
 public bool Cantilever{get{return Type.StartsWith("Cantilever");}}
 public bool Transverse{get{return Type.StartsWith("Spanning across");}}
 public bool Stringer{get{return Type.StartsWith("Stringer");}}
 public bool Slabless{get{return Type.StartsWith("Slabless");}}
 public int Flights{get{return Type.StartsWith("Open-well")?3:Type.StartsWith("Dog-legged")?2:1;}}
 public double Theta{get{return Math.Atan2(Riser,Tread);}}
 public static readonly string[] Types={"Straight flight (waist slab)","Dog-legged (two flights, half landing)","Open-well (three flights, two landings)","Spanning across (each step between walls / beams)","Stringer beams (waist spans between two stringers)","Cantilever steps (from a wall / spine beam)","Slabless / folded plate (sawtooth)","Spiral (cantilever treads from a central column)"};
}
/* one span along the flight: plan segments with their factored and service loads */
class StairSegment {public string Name;public double Length,Wd,Wl;/* kN/m per metre width (dead, live) */public bool Flight;}
class StairMember {public string Name;public List<StairSegment> Segs=new List<StairSegment>();public string Support;/* overhangs beyond the end beams (m) and their loads (kN/m dead, live) */public double OverStart,OverEnd,OverWd,OverWl;public double Length{get{return Segs.Sum(s=>s.Length);}}
 public double X0(StairSegment s){double x=0;foreach(var q in Segs){if(q==s)return x;x+=q.Length;}return x;}}
class StairCheck {public string Name,Clause,Member,Unit,Detail="";public double Demand,Capacity;public bool Ok{get{return Demand<=Capacity*1.0000001;}}public double Ratio{get{return Capacity<=0?99:Demand/Capacity;}}}
class StairResult {
 public StairInput In;public List<StairMember> Members=new List<StairMember>();public List<StairCheck> Checks=new List<StairCheck>();public List<string> Notes=new List<string>();
 public List<string[]> Steel=new List<string[]>();/* member, location, bars */
 public double Wd,Wl,WdLanding,WlLanding;/* kPa on plan */
 public Dictionary<StairMember,double[][]> Diagrams=new Dictionary<StairMember,double[][]>();
 /* the step-by-step calculation of each member for the report: member, step, formula with values, result */public List<string[]> Calc=new List<string[]>();/* x, V, M (factored) */
}
static class Staircase {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 internal static string F(double v,string f){return v.ToString(f,Inv);}
 internal static double Ab(double d){return Math.PI*d*d/4;}
 internal static double Beta1(double fc){return fc<=28?0.85:Math.Max(0.65,0.85-0.05*(fc-28)/7);}
 /* flight dead load per plan area: waist on the slope, half the step, finishes on the tread and riser */
 internal static void Loads(StairInput i,out double wd,out double wl,out double wdL,out double wlL){double R=i.Riser/1000,T=i.Tread/1000,t=i.Waist/1000,k=Math.Sqrt(R*R+T*T)/T;
  wd=i.Gamma*(t*k+R/2)+i.Finish*(R+T)/T;wl=i.LL;wdL=i.Gamma*i.LandingT/1000+i.Finish;wlL=i.LL;}
 internal static void Validate(StairInput i){if(i.Riser<=0||i.Tread<=0||i.Width<=0)throw new Exception("Enter the riser, tread and width.");if(i.Waist<=0||i.LandingT<=0)throw new Exception("Enter the waist and landing thicknesses.");
  if(i.N1<2||(i.Flights>1&&i.N2<2)||(i.Flights>2&&i.N3<2))throw new Exception("Each flight needs at least 2 risers.");if(i.Fc<=0||i.Fy<=0)throw new Exception("Enter f'c and fy.");if(i.MainDia<=0||i.DistDia<=0)throw new Exception("Enter the bar diameters.");
  if(i.Spiral&&(i.OuterRadius<=i.ColumnDia/2||i.TreadsPerTurn<4))throw new Exception("Spiral: the outer radius must exceed the column radius; at least 4 treads per turn.");}
 static double MinThickness(string support,double L){double div=support.StartsWith("Both")?28:support.StartsWith("One")?24:20;return L*1000/div;}
 /* simply supported piecewise-uniform beam: x, V, M at fine stations; end moments from the continuity (ACI coefficients) */
 internal static double[][] Analyse(StairMember m,bool factored,out double mpos,out double mneg,out double vmax){double L=m.Length;int n=Math.Max(60,(int)(L*100));Func<StairSegment,double> w=s=>factored?1.2*s.Wd+1.6*s.Wl:s.Wd+s.Wl;
  double W=0,Mo=0;foreach(var s in m.Segs){double a=m.X0(s);W+=w(s)*s.Length;Mo+=w(s)*s.Length*(a+s.Length/2);}double Rb=Mo/L,Ra=W-Rb;
  double wavg=W/L;double end=m.Support.StartsWith("Both")?wavg*L*L/12:m.Support.StartsWith("One")?wavg*L*L/10:0;/* continuous: the end moment(s) over the support(s) */
  var xs=new double[n+1];var vs=new double[n+1];var ms=new double[n+1];bool both=m.Support.StartsWith("Both"),one=m.Support.StartsWith("One");double Ml=both?-end:0,Mr=(both||one)?-end:0;
  /* a landing overhang beyond its beam hogs the end: the larger of the continuity and the overhang moment */double wo=factored?1.2*m.OverWd+1.6*m.OverWl:m.OverWd+m.OverWl;if(m.OverStart>0)Ml=Math.Min(Ml,-wo*m.OverStart*m.OverStart/2);if(m.OverEnd>0)Mr=Math.Min(Mr,-wo*m.OverEnd*m.OverEnd/2);double dV=(Mr-Ml)/L;
  for(int k=0;k<=n;k++){double x=L*k/n,V=Ra+dV,M=Ra*x+Ml+dV*x;/* end moments vary linearly from Ml at x = 0 to Mr at x = L */foreach(var s in m.Segs){double a=m.X0(s),b=a+s.Length;if(x<=a)continue;double len=Math.Min(x,b)-a;V-=w(s)*len;M-=w(s)*len*(x-a-len/2);}xs[k]=x;vs[k]=V;ms[k]=M;}
  mpos=Math.Max(0,ms.Max());mneg=Math.Max(0,-ms.Min());vmax=Math.Max(vs.Select(v=>Math.Abs(v)).Max(),wo*Math.Max(m.OverStart,m.OverEnd));return new[]{xs,vs,ms};}
 /* the steel of a slab strip b wide and h thick for Mu (kN·m) */
 internal static double AsRequired(double Mu,double b,double d,double fc,double fy,out double rho){double Rn=Mu*1e6/(0.9*b*d*d);double q=1-2*Rn/(0.85*fc);if(q<0){rho=double.NaN;return double.NaN;}rho=0.85*fc/fy*(1-Math.Sqrt(q));return rho*b*d;}
 internal static double PhiMn(double As,double b,double d,double fc,double fy){double a=As*fy/(0.85*fc*b);double c=a/Beta1(fc);double et=0.003*(d-c)/Math.Max(c,1e-9);double phi=et>=0.005?0.9:et<=fy/200000?0.65:0.65+0.25*(et-fy/200000)/(0.005-fy/200000);return phi*As*fy*(d-a/2)/1e6;}
 /* 90° standard hook (NSCP 425.4.3.1): ldh = 0.24 fy / (λ √f'c) db, at least 8 db and 150 mm; straight bars (425.4.2.3, db <= 19 mm):
    ld = fy / (2.1 λ √f'c) db (2.1 -> 1.7 above 19 mm), at least 300 mm; rounded up to 25 mm */
 internal static double HookLength(StairInput i){double v=Math.Max(Math.Max(0.24*i.Fy/Math.Sqrt(i.Fc)*i.MainDia,8*i.MainDia),150);return Math.Ceiling(v/25)*25;}
 internal static double DevLength(StairInput i){double v=Math.Max(i.Fy/((i.MainDia<=19?2.1:1.7)*Math.Sqrt(i.Fc))*i.MainDia,300);return Math.Ceiling(v/25)*25;}
 static double Spacing(double As,double dia,double h){double s=Ab(dia)*1000/Math.Max(As,1);s=Math.Min(s,Math.Min(3*h,450));return Math.Max(75,Math.Floor(s/25)*25);}
 static double MinSteel(StairInput i,double b,double h){return (i.Fy<420?0.0020:Math.Max(0.0014,0.0018*420/i.Fy))*b*h;}
 public static StairResult Design(StairInput i){Validate(i);var r=new StairResult{In=i};double wd,wl,wdL,wlL;Loads(i,out wd,out wl,out wdL,out wlL);r.Wd=wd;r.Wl=wl;r.WdLanding=wdL;r.WlLanding=wlL;
  Geometry(i,r);
  if(i.Cantilever)DesignCantilever(i,r);else if(i.Transverse)DesignTransverse(i,r,false);else if(i.Stringer)DesignTransverse(i,r,true);else if(i.Spiral)DesignSpiral(i,r);else DesignAlong(i,r);
  return r;}
 static void Geometry(StairInput i,StairResult r){double R=i.Riser,T=i.Tread;r.Checks.Add(new StairCheck{Name="Riser height",Clause="Geometry (PD 1096 / RA 9514)",Member="All flights",Demand=R,Capacity=i.MaxRiser,Unit="mm"});
  r.Checks.Add(new StairCheck{Name="Tread (going) - minimum",Clause="Geometry (PD 1096 / RA 9514)",Member="All flights",Demand=i.MinTread,Capacity=T,Unit="mm",Detail="tread "+F(T,"0")+" >= "+F(i.MinTread,"0")});
  r.Checks.Add(new StairCheck{Name="Comfort 2R + T - lower limit",Clause="Blondel rule",Member="All flights",Demand=i.Min2RT,Capacity=2*R+T,Unit="mm",Detail="2R + T = "+F(2*R+T,"0")});
  r.Checks.Add(new StairCheck{Name="Comfort 2R + T - upper limit",Clause="Blondel rule",Member="All flights",Demand=2*R+T,Capacity=i.Max2RT,Unit="mm"});
  r.Checks.Add(new StairCheck{Name="Stair width - minimum",Clause="Geometry (RA 9514 exits)",Member="All flights",Demand=i.MinWidth,Capacity=i.Width,Unit="mm",Detail="width "+F(i.Width,"0")});
  if(i.Flights>=2&&!i.Spiral){/* headroom under the upper flight of a dog-leg / open-well stair: the floor-to-floor rise less the flight above */double storey=(i.N1+i.N2+(i.Flights>2?i.N3:0))*R,clear=storey-i.Waist/Math.Cos(i.Theta);r.Checks.Add(new StairCheck{Name="Headroom under the flight above",Clause="Geometry",Member="Flight 1 under the next storey",Demand=i.Headroom,Capacity=clear,Unit="mm",Detail="storey rise "+F(storey,"0")+" less the waist on the slope"});}}
 static List<StairSegment> FlightSegs(StairInput i,int risers,double before,double after,double wd,double wl,double wdL,double wlL){var list=new List<StairSegment>();double going=(risers-1)*i.Tread/1000;
  if(before>0)list.Add(new StairSegment{Name="Landing",Length=i.LandingSpan(before)/1000,Wd=wdL,Wl=wlL});list.Add(new StairSegment{Name="Flight ("+risers+" risers)",Length=going,Wd=wd,Wl=wl,Flight=true});if(after>0)list.Add(new StairSegment{Name="Landing",Length=i.LandingSpan(after)/1000,Wd=wdL,Wl=wlL});return list;}
 /* a member along the flight with the landing beams where they are */
 static StairMember Along(StairInput i,string name,int risers,double before,double after,double wd,double wl,double wdL,double wlL){var m=new StairMember{Name=name,Support=i.Support,Segs=FlightSegs(i,risers,before,after,wd,wl,wdL,wlL)};
  if(before>0)m.OverStart=i.LandingOver(before)/1000;if(after>0)m.OverEnd=i.LandingOver(after)/1000;m.OverWd=wdL;m.OverWl=wlL;return m;}
 /* waist-slab and slabless stairs spanning along the flight */
 static void DesignAlong(StairInput i,StairResult r){double wd=r.Wd,wl=r.Wl,wdL=r.WdLanding,wlL=r.WlLanding;bool spans=i.Landing.StartsWith("Landing spans");
  if(i.Slabless){double R=i.Riser/1000,T=i.Tread/1000,t=i.Waist/1000;wd=i.Gamma*t*(R+T)/T+i.Finish*(R+T)/T;r.Wd=wd;r.Notes.Add("Slabless stair: the folded plate (riser and tread plates "+F(i.Waist,"0")+" mm thick) spans along the flight; the effective depth is the plate thickness less the cover; bars follow the profile with closed links at the inner corners.");}
  var members=new List<StairMember>();
  if(i.Flights==1)members.Add(Along(i,"Flight 1",i.N1,i.BottomLanding,i.TopLanding,wd,wl,wdL,wlL));
  else if(i.Flights==2){members.Add(Along(i,"Flight 1 with the half landing",i.N1,i.BottomLanding,spans?i.MidLanding:0,wd,wl,wdL,wlL));members.Add(Along(i,"Flight 2 with the half landing",i.N2,spans?i.MidLanding:0,i.TopLanding,wd,wl,wdL,wlL));
   if(!spans){members.Add(new StairMember{Name="Half landing (on its beams / walls)",Support=i.Support,Segs=new List<StairSegment>{new StairSegment{Name="Landing",Length=(2*i.Width+i.Gap)/1000,Wd=wdL+wd*(i.N1-1)*i.Tread/2000/(i.MidLanding/1000),Wl=wlL+wl*(i.N1-1)*i.Tread/2000/(i.MidLanding/1000)}}});r.Notes.Add("The half landing spans across between its own beams or walls and carries the reaction of the flights (half the flight load, spread over the landing depth and added to its design load).");}}
  else{members.Add(Along(i,"Flight 1 with landing 1",i.N1,i.BottomLanding,i.Width,wd,wl,wdL,wlL));members.Add(Along(i,"Flight 2 between the landings",i.N2,i.Width*0.5,i.Width*0.5,wd,wl,wdL,wlL));members.Add(Along(i,"Flight 3 with landing 2",i.N3,i.Width,i.TopLanding,wd,wl,wdL,wlL));
   r.Notes.Add("Open-well stair: each flight spans along its line with the landing it meets (half of the landing width where two flights share it, the middle flight between the two landings).");}
  if(!i.Slabless&&i.OnBeams)r.Notes.Add("Supports: beams "+F(i.BeamB,"0")+" x "+F(i.BeamH,"0")+" mm"+(i.LandingBeamAt>0?"; each landing beam "+F(i.LandingBeamAt,"0")+" mm from the flight - the landing beyond it is an overhang that hogs the end over the beam (top bars)":" at the landing ends")+". Spans are measured to the beam lines.");
  foreach(var m in members)DesignStrip(i,r,m);}
 static void DesignStrip(StairInput i,StairResult r,StairMember m){double mp,mn,v;var dg=Analyse(m,true,out mp,out mn,out v);r.Members.Add(m);r.Diagrams[m]=dg;double L=m.Length;
  double h=m.Segs.Any(s=>s.Flight)?i.Waist:i.LandingT,b=1000,d=h-i.Cover-i.MainDia/2;double rho;double As=AsRequired(mp,b,d,i.Fc,i.Fy,out rho);double Amin=MinSteel(i,b,h);
  if(double.IsNaN(As)){r.Checks.Add(new StairCheck{Name="Flexure - positive (section too small)",Clause="422.2",Member=m.Name,Demand=mp,Capacity=0,Unit="kN·m/m",Detail="increase the waist"});return;}
  double sp=Spacing(Math.Max(As,Amin),i.MainDia,h),Aprov=Ab(i.MainDia)*1000/sp;double phiMn=PhiMn(Aprov,b,d,i.Fc,i.Fy);
  r.Checks.Add(new StairCheck{Name="Flexure - midspan (bottom bars)",Clause="422.2 / 407.5",Member=m.Name,Demand=mp,Capacity=phiMn,Unit="kN·m/m",Detail="d = "+F(d,"0")+" mm, As req = "+F(Math.Max(As,Amin),"0")+" mm²/m, Ø"+F(i.MainDia,"0")+" @ "+F(sp,"0")+" = "+F(Aprov,"0")+" mm²/m"});
  r.Steel.Add(new[]{m.Name,"Main bottom bars along the flight","Ø"+F(i.MainDia,"0")+" @ "+F(sp,"0")+" mm"});
  if(mn>0){double An=AsRequired(mn,b,d,i.Fc,i.Fy,out rho);double sn=Spacing(Math.Max(double.IsNaN(An)?1e9:An,Amin),i.MainDia,h);double An2=Ab(i.MainDia)*1000/sn;r.Checks.Add(new StairCheck{Name="Flexure - support (top bars)",Clause="422.2 / 407.5",Member=m.Name,Demand=mn,Capacity=PhiMn(An2,b,d,i.Fc,i.Fy),Unit="kN·m/m",Detail="Ø"+F(i.MainDia,"0")+" @ "+F(sn,"0")+" top, extend L/4 past the support face"});r.Steel.Add(new[]{m.Name,i.TopFull&&!i.Slabless?"Top bars, full length (over the continuous supports)":"Top bars over the continuous support(s), L/4","Ø"+F(i.MainDia,"0")+" @ "+F(sn,"0")+" mm"+(i.TopFull&&!i.Slabless?", full length":"")});}
  else r.Steel.Add(new[]{m.Name,i.TopFull&&!i.Slabless?"Top bars, full length (nominal, 0.0018bh)":"Top bars at the supports and at the kinks (nominal, 0.0018bh)","Ø"+F(i.MainDia,"0")+" @ "+F(Spacing(Amin,i.MainDia,h),"0")+" mm"+(i.TopFull&&!i.Slabless?", full length":", L/5 from the support")});
  /* anchorage, nosing bars and chair bars (owner, 23 November 2026: "proper anchoring ... walang detail ng chair bar and nosing bar") */
  r.Steel.Add(new[]{m.Name,"Anchorage of the bottom bars","into the support beam / landing at least 150 mm past the support face (407.7.3.8.1)"});
  r.Steel.Add(new[]{m.Name,"Anchorage of the top bars","90° hook into the support beam, ldh = "+F(HookLength(i),"0")+" mm with a 12db = "+F(12*i.MainDia,"0")+" mm leg (425.4.3); straight ld = "+F(DevLength(i),"0")+" mm where there is no hook (425.4.2)"});
  if(!i.Slabless&&m.Segs.Any(q=>q.Flight)){r.Steel.Add(new[]{m.Name,"Step bars (chair bars), bent to the riser and tread of every step","Ø"+F(i.DistDia,"0")+" @ 300 mm across the width, at the cover inside the riser and under the tread, both legs lapped onto the waist top bars"});
   r.Steel.Add(new[]{m.Name,"Nosing bars, one at each step nosing","1 - Ø"+F(i.DistDia,"0")+" per step, full width, in the bend of the step bars"});}
  r.Steel.Add(new[]{m.Name,"Bar supports (spacers) under the top bars","Ø"+F(i.MainDia,"0")+" @ 1000 mm each way"});
  double phiVc=0.75*0.17*Math.Sqrt(i.Fc)*b*d/1000;r.Checks.Add(new StairCheck{Name="One-way shear",Clause="422.5.5.1",Member=m.Name,Demand=v,Capacity=phiVc,Unit="kN/m",Detail="φVc = 0.75 × 0.17 √f'c b d"});
  double hmin=MinThickness(m.Support,L)*(0.4+i.Fy/700);r.Checks.Add(new StairCheck{Name="Minimum thickness (deflection)",Clause="Table 407.3.1.1",Member=m.Name,Demand=hmin,Capacity=h,Unit="mm",Detail="L = "+F(L*1000,"0")+" mm, "+m.Support.ToLowerInvariant()+(i.Fy!=420?", × (0.4 + fy/700)":"")});
  double sd=Math.Min(Math.Floor(Math.Min(Ab(i.DistDia)*1000/(0.0018*b*h),Math.Min(5*h,450))/25)*25,450);r.Steel.Add(new[]{m.Name,"Distribution bars across the flight","Ø"+F(i.DistDia,"0")+" @ "+F(sd,"0")+" mm"});
  r.Checks.Add(new StairCheck{Name="Main bar spacing",Clause="407.7.2.3",Member=m.Name,Demand=sp,Capacity=Math.Min(3*h,450),Unit="mm"});
  /* complete calculation for the report (owner, 23 November 2026: "complete detailed calculation report") */
  Action<string,string,string> c=(step,formula,result)=>r.Calc.Add(new[]{m.Name,step,formula,result});
  foreach(var sg in m.Segs)c("Load - "+sg.Name.ToLowerInvariant(),"wu = 1.2 wD + 1.6 wL = 1.2 × "+F(sg.Wd,"0.00")+" + 1.6 × "+F(sg.Wl,"0.00")+" (length "+F(sg.Length,"0.000")+" m)",F(1.2*sg.Wd+1.6*sg.Wl,"0.00")+" kN/m per m width");
  if(m.OverStart>0||m.OverEnd>0)c("Landing overhang beyond the beam","Mu,over = wu a² / 2, a = "+F(Math.Max(m.OverStart,m.OverEnd),"0.000")+" m",F((1.2*m.OverWd+1.6*m.OverWl)*Math.Pow(Math.Max(m.OverStart,m.OverEnd),2)/2,"0.00")+" kN·m/m");
  {double W=m.Segs.Sum(q=>(1.2*q.Wd+1.6*q.Wl)*q.Length),Mo=0;foreach(var q in m.Segs)Mo+=(1.2*q.Wd+1.6*q.Wl)*q.Length*(m.X0(q)+q.Length/2);c("Reactions (simple span)","W = Σ wu l = "+F(W,"0.00")+" kN; Rb = Σ wu l x / L = "+F(Mo,"0.00")+" / "+F(L,"0.000"),"Ra = "+F(W-Mo/L,"0.00")+", Rb = "+F(Mo/L,"0.00")+" kN/m");}
  c("Design moments and shear","from the load, shear and moment diagram (span "+F(L,"0.000")+" m, "+m.Support.ToLowerInvariant()+")","Mu+ = "+F(mp,"0.00")+" kN·m/m, Mu- = "+F(mn,"0.00")+" kN·m/m, Vu = "+F(v,"0.00")+" kN/m");
  c("Effective depth","d = h - cover - db/2 = "+F(h,"0")+" - "+F(i.Cover,"0")+" - "+F(i.MainDia/2,"0.#"),F(d,"0")+" mm");
  {double Rn=mp*1e6/(0.9*b*d*d);c("Bottom bars - Rn","Rn = Mu / (φ b d²) = "+F(mp,"0.00")+"×10⁶ / (0.9 × 1000 × "+F(d,"0")+"²)",F(Rn,"0.000")+" MPa");
   c("Bottom bars - ρ","ρ = 0.85 f'c / fy [1 - √(1 - 2Rn / (0.85 f'c))]",F(rho0(i,Rn),"0.00000"));}
  c("Bottom bars - As required","As = ρ b d = "+F(As,"0")+" mm²/m; As,min = "+(i.Fy<420?"0.0020":"0.0018 × 420 / fy")+" b h = "+F(Amin,"0")+" mm²/m",F(Math.Max(As,Amin),"0")+" mm²/m");
  c("Bottom bars - spacing","s = Ab × 1000 / As = "+F(Ab(i.MainDia),"0.0")+" × 1000 / "+F(Math.Max(As,Amin),"0")+", ≤ 3h and 450 mm, rounded down to 25 mm","Ø"+F(i.MainDia,"0")+" @ "+F(sp,"0")+" mm, As,prov = "+F(Aprov,"0")+" mm²/m");
  {double a=Aprov*i.Fy/(0.85*i.Fc*b);c("Bottom bars - capacity","a = As fy / (0.85 f'c b) = "+F(a,"0.0")+" mm; φMn = φ As fy (d - a/2)","φMn = "+F(phiMn,"0.00")+" ≥ Mu+ = "+F(mp,"0.00")+" kN·m/m "+(phiMn>=mp?"OK":"NG"));}
  if(mn>0){double An=AsRequired(mn,b,d,i.Fc,i.Fy,out rho);double sn=Spacing(Math.Max(double.IsNaN(An)?1e9:An,Amin),i.MainDia,h);double An2=Ab(i.MainDia)*1000/sn;double pn=PhiMn(An2,b,d,i.Fc,i.Fy);
   c("Top bars","Mu- = "+F(mn,"0.00")+" kN·m/m; As = "+(double.IsNaN(An)?"(section too small)":F(An,"0"))+" mm²/m, As,min = "+F(Amin,"0"),"Ø"+F(i.MainDia,"0")+" @ "+F(sn,"0")+" mm, φMn = "+F(pn,"0.00")+" kN·m/m "+(pn>=mn?"OK":"NG"));}
  else c("Top bars (nominal)","no hogging moment: As,min = "+F(Amin,"0")+" mm²/m","Ø"+F(i.MainDia,"0")+" @ "+F(Spacing(Amin,i.MainDia,h),"0")+" mm");
  c("One-way shear","φVc = 0.75 × 0.17 √f'c b d = 0.75 × 0.17 × √"+F(i.Fc,"0.#")+" × 1000 × "+F(d,"0")+" / 1000","φVc = "+F(phiVc,"0.00")+" ≥ Vu = "+F(v,"0.00")+" kN/m "+(phiVc>=v?"OK":"NG"));
  c("Minimum thickness","h,min = L / "+(m.Support.StartsWith("Both")?"28":m.Support.StartsWith("One")?"24":"20")+(i.Fy!=420?" × (0.4 + fy/700)":"")+" = "+F(L*1000,"0")+" / ...",F(hmin,"0")+" mm ≤ h = "+F(h,"0")+" mm "+(h>=hmin?"OK":"NG"));
  c("Distribution bars","As = 0.0018 b h = "+F(0.0018*b*h,"0")+" mm²/m; s ≤ 5h and 450 mm","Ø"+F(i.DistDia,"0")+" @ "+F(sd,"0")+" mm");
  c("Anchorage of the top bars","ldh = 0.24 fy / √f'c × db = 0.24 × "+F(i.Fy,"0")+" / √"+F(i.Fc,"0.#")+" × "+F(i.MainDia,"0")+" (≥ 8db, 150 mm); ld = fy / (2.1 √f'c) db",F(HookLength(i),"0")+" mm hook, "+F(DevLength(i),"0")+" mm straight");}
 static double rho0(StairInput i,double Rn){double q=1-2*Rn/(0.85*i.Fc);return q<0?double.NaN:0.85*i.Fc/i.Fy*(1-Math.Sqrt(q));}
 /* each step spans the width between walls / beams, or the waist spans between two stringer beams */
 static void DesignTransverse(StairInput i,StairResult r,bool stringer){double R=i.Riser,T=i.Tread,L=i.Width/1000;double heff=i.Waist+R/2;/* step and waist acting together (mean depth) */
  double w=(r.Wd*1.2+r.Wl*1.6)*T/1000;/* kN/m along the step (per step) */double Mu=Math.Max(w*L*L/8,(r.Wd*1.2*T/1000)*L*L/8+1.6*i.Point*L/4),Vu=Math.Max(w*L/2,(r.Wd*1.2*T/1000)*L/2+1.6*i.Point);
  var m=new StairMember{Name=stringer?"Waist across the stringers (per step)":"Step across the width (per step)",Support="Simply supported",Segs=new List<StairSegment>{new StairSegment{Name="Step",Length=L,Wd=r.Wd*T/1000,Wl=r.Wl*T/1000}}};double mp,mn,v;r.Diagrams[m]=Analyse(m,true,out mp,out mn,out v);r.Members.Add(m);
  double b=T,d=heff-i.Cover-i.MainDia/2,rho;double As=AsRequired(Mu,b,d,i.Fc,i.Fy,out rho),Amin=MinSteel(i,b,heff);int n=Math.Max(2,(int)Math.Ceiling(Math.Max(double.IsNaN(As)?1e9:As,Amin)/Ab(i.MainDia)));double Ap=n*Ab(i.MainDia);
  r.Checks.Add(new StairCheck{Name="Flexure of the step (bottom bars)",Clause="422.2",Member=m.Name,Demand=Mu,Capacity=PhiMn(Ap,b,d,i.Fc,i.Fy),Unit="kN·m",Detail="b = tread "+F(T,"0")+", mean depth "+F(heff,"0")+" mm; governing of the uniform load and the 1.33 kN tread load"});
  r.Checks.Add(new StairCheck{Name="Shear of the step",Clause="422.5.5.1",Member=m.Name,Demand=Vu,Capacity=0.75*0.17*Math.Sqrt(i.Fc)*b*d/1000,Unit="kN"});
  r.Steel.Add(new[]{m.Name,"Bottom bars per step, across the width",n+" - Ø"+F(i.MainDia,"0")});r.Steel.Add(new[]{m.Name,"Bars along the flight (distribution)","Ø"+F(i.DistDia,"0")+" @ "+F(Math.Min(5*i.Waist,450),"0")+" mm"});
  if(!stringer){r.Notes.Add("Steps spanning across bear on the walls / beams on both sides (bearing at least 100 mm); the waist carries no flexure along the flight.");return;}
  /* the stringer beams: half the flight load each, inclined span on plan */
  double going=(i.N1-1)*T/1000,ws=(1.2*(r.Wd*i.Width/2000+24*i.StringerB*i.StringerH/1e6)+1.6*r.Wl*i.Width/2000);var sm=new StairMember{Name="Stringer beam (each side)",Support=i.Support,Segs=new List<StairSegment>{new StairSegment{Name="Stringer",Length=going,Wd=r.Wd*i.Width/2000+24*i.StringerB*i.StringerH/1e6,Wl=r.Wl*i.Width/2000,Flight=true}}};
  double smp,smn,sv;r.Diagrams[sm]=Analyse(sm,true,out smp,out smn,out sv);r.Members.Add(sm);double sd=i.StringerH-40-i.StirrupDia-i.StringerDia/2;double sAs=AsRequired(smp,i.StringerB,sd,i.Fc,i.Fy,out rho);double sAmin=Math.Max(0.25*Math.Sqrt(i.Fc)/i.Fy,1.4/i.Fy)*i.StringerB*sd;
  int sn=Math.Max(2,(int)Math.Ceiling(Math.Max(double.IsNaN(sAs)?1e9:sAs,sAmin)/Ab(i.StringerDia)));r.Checks.Add(new StairCheck{Name="Stringer flexure",Clause="409.5 / 422.2",Member=sm.Name,Demand=smp,Capacity=PhiMn(sn*Ab(i.StringerDia),i.StringerB,sd,i.Fc,i.Fy),Unit="kN·m",Detail=F(i.StringerB,"0")+" x "+F(i.StringerH,"0")+", "+sn+" - Ø"+F(i.StringerDia,"0")+" bottom"});
  double Vc=0.17*Math.Sqrt(i.Fc)*i.StringerB*sd/1000,Vs=Math.Max(0,sv/0.75-Vc);double Av=2*Ab(i.StirrupDia);double ss=Vs<=0?Math.Min(sd/2,600):Math.Min(Av*i.Fyt*sd/(Vs*1000),Math.Min(sd/2,600));ss=Math.Max(75,Math.Floor(ss/25)*25);
  r.Checks.Add(new StairCheck{Name="Stringer shear",Clause="422.5",Member=sm.Name,Demand=sv,Capacity=0.75*(Vc+Av*i.Fyt*sd/ss/1000),Unit="kN",Detail="Ø"+F(i.StirrupDia,"0")+" stirrups @ "+F(ss,"0")});
  r.Steel.Add(new[]{sm.Name,"Bottom bars",sn+" - Ø"+F(i.StringerDia,"0")});r.Steel.Add(new[]{sm.Name,"Top bars (hangers)","2 - Ø"+F(i.StringerDia,"0")});r.Steel.Add(new[]{sm.Name,"Stirrups","Ø"+F(i.StirrupDia,"0")+" @ "+F(ss,"0")+" mm"});
  r.Checks.Add(new StairCheck{Name="Stringer minimum depth (deflection)",Clause="Table 409.3.1.1",Member=sm.Name,Demand=going*1000/(i.Support.StartsWith("Both")?21:i.Support.StartsWith("One")?18.5:16)*(0.4+i.Fy/700),Capacity=i.StringerH,Unit="mm"});}
 /* each step cantilevers from the wall / spine beam: top bars, the uniform load or the 1.33 kN tread load at the tip */
 static void DesignCantilever(StairInput i,StairResult r){double T=i.Tread,L=i.Width/1000,heff=i.Waist+i.Riser/2;double wD=r.Wd*T/1000,wL=r.Wl*T/1000+0;/* per step */
  double Mu=Math.Max(1.2*wD*L*L/2+1.6*wL*L*L/2,1.2*wD*L*L/2+1.6*i.Point*L)+1.6*i.Rail*0*L,Vu=Math.Max(1.2*wD*L+1.6*wL*L,1.2*wD*L+1.6*i.Point);
  var m=new StairMember{Name="Cantilever step",Support="Cantilever",Segs=new List<StairSegment>{new StairSegment{Name="Step",Length=L,Wd=wD,Wl=wL}}};r.Members.Add(m);int n0=60;var xs=new double[n0+1];var vs=new double[n0+1];var ms=new double[n0+1];
  for(int k=0;k<=n0;k++){double x=L*k/n0,a=L-x;xs[k]=x;vs[k]=(1.2*wD+1.6*wL)*a;ms[k]=-(1.2*wD+1.6*wL)*a*a/2;}r.Diagrams[m]=new[]{xs,vs,ms};
  double b=T,d=heff-i.Cover-i.MainDia/2,rho;double As=AsRequired(Mu,b,d,i.Fc,i.Fy,out rho),Amin=MinSteel(i,b,heff);int n=Math.Max(2,(int)Math.Ceiling(Math.Max(double.IsNaN(As)?1e9:As,Amin)/Ab(i.MainDia)));
  r.Checks.Add(new StairCheck{Name="Flexure at the support (top bars)",Clause="422.2",Member=m.Name,Demand=Mu,Capacity=PhiMn(n*Ab(i.MainDia),b,d,i.Fc,i.Fy),Unit="kN·m",Detail="per step, b = "+F(T,"0")+", mean depth "+F(heff,"0")+" mm; the larger of the uniform load and the 1.33 kN tread load at the tip"});
  r.Checks.Add(new StairCheck{Name="Shear at the support",Clause="422.5.5.1",Member=m.Name,Demand=Vu,Capacity=0.75*0.17*Math.Sqrt(i.Fc)*b*d/1000,Unit="kN"});
  r.Checks.Add(new StairCheck{Name="Minimum depth of the cantilever",Clause="Table 407.3.1.1",Member=m.Name,Demand=L*1000/10*(0.4+i.Fy/700),Capacity=heff,Unit="mm",Detail="L/10 (one-way cantilever) against the mean depth"});
  r.Steel.Add(new[]{m.Name,"Top bars per step, anchored Ld + 90° hook into the wall / spine beam",n+" - Ø"+F(i.MainDia,"0")});r.Steel.Add(new[]{m.Name,"Bottom bars per step (nominal)","2 - Ø"+F(i.DistDia,"0")});r.Steel.Add(new[]{m.Name,"Links / distribution","Ø"+F(i.DistDia,"0")+" @ 200 mm"});
  r.Notes.Add("Each step cantilevers "+F(i.Width,"0")+" mm from the wall or spine beam; the support takes the torsion of the steps (design the spine beam for it). Anchor the top bars a full development length into the support.");}
 /* spiral stair: tapered treads cantilever from the central column */
 static void DesignSpiral(StairInput i,StairResult r){double rc=i.ColumnDia/2000,Ro=i.OuterRadius/1000,ang=2*Math.PI/i.TreadsPerTurn,L=Ro-rc;double q=1.2*(i.Gamma*(i.Waist+i.Riser/2)/1000+i.Finish)+1.6*i.LL;/* kPa */
  /* tread width grows with the radius: w(x) = q × ang × (rc + x) per metre of the cantilever */double Mu=0,Vu=0;int n0=60;var xs=new double[n0+1];var vs=new double[n0+1];var ms=new double[n0+1];
  for(int k=0;k<=n0;k++){double x=L*k/n0;double V=0,M=0;for(int j=0;j<200;j++){double u=x+(L-x)*(j+0.5)/200,dx=(L-x)/200;double ww=q*ang*(rc+u);V+=ww*dx;M+=ww*dx*(u-x);}xs[k]=x;vs[k]=V;ms[k]=-M;}
  double Mpt=Math.Max(-ms[0],1.2*(i.Gamma*(i.Waist+i.Riser/2)/1000+i.Finish)*ang*(rc*L*L/2+L*L*L/3)+1.6*i.Point*L);Mu=Mpt;Vu=Math.Max(vs[0],1.6*i.Point+vs[0]*0.6);
  double band=ang*(rc+L/2);var mem=new StairMember{Name="Spiral tread (cantilever from the column)",Support="Cantilever",Segs=new List<StairSegment>{new StairSegment{Name="Tread (mean width)",Length=L,Wd=(i.Gamma*(i.Waist+i.Riser/2)/1000+i.Finish)*band,Wl=i.LL*band}}};r.Members.Add(mem);r.Diagrams[mem]=new[]{xs,vs,ms};
  double b=ang*(rc+0.05)*1000,heff=i.Waist+i.Riser/2,d=heff-i.Cover-i.MainDia/2,rho;b=Math.Max(b,150);double As=AsRequired(Mu,b,d,i.Fc,i.Fy,out rho),Amin=MinSteel(i,b,heff);int n=Math.Max(2,(int)Math.Ceiling(Math.Max(double.IsNaN(As)?1e9:As,Amin)/Ab(i.MainDia)));
  r.Checks.Add(new StairCheck{Name="Tread flexure at the column face (top bars)",Clause="422.2",Member=mem.Name,Demand=Mu,Capacity=PhiMn(n*Ab(i.MainDia),b,d,i.Fc,i.Fy),Unit="kN·m",Detail="tread angle "+F(360/i.TreadsPerTurn,"0.0")+"°, root width "+F(b,"0")+" mm, depth "+F(heff,"0")+" mm"});
  r.Checks.Add(new StairCheck{Name="Tread shear at the column face",Clause="422.5.5.1",Member=mem.Name,Demand=Vu,Capacity=0.75*0.17*Math.Sqrt(i.Fc)*b*d/1000,Unit="kN"});
  double turn=i.TreadsPerTurn*i.Riser/1000,Pu=q*Math.PI*(Ro*Ro-rc*rc)*Math.Max(1,(i.N1*i.Riser/1000)/turn),Ag=Math.PI*i.ColumnDia*i.ColumnDia/4;double phiPn=0.65*0.8*(0.85*i.Fc*(Ag*0.99)+i.Fy*Ag*0.01)/1000;
  r.Checks.Add(new StairCheck{Name="Central column axial (1% steel, tied)",Clause="422.4.2",Member="Central column",Demand=Pu,Capacity=phiPn,Unit="kN",Detail="Ø"+F(i.ColumnDia,"0")+" column, the stair load of the storey"});
  r.Steel.Add(new[]{mem.Name,"Top bars per tread into the column",n+" - Ø"+F(i.MainDia,"0")});r.Steel.Add(new[]{mem.Name,"Bottom bars per tread","2 - Ø"+F(i.DistDia,"0")});r.Steel.Add(new[]{"Central column","Vertical bars / ties","1% of Ag (min. 6 - Ø16), Ø10 ties @ 150"});
  r.Notes.Add("Spiral stair: the treads are cantilevers from the central column; the column takes the out-of-balance moment and torsion of a partly loaded stair (check with the structural model).");}
}
}
