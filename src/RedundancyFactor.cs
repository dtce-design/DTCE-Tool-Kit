using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Text.RegularExpressions;
namespace BeamScheduleFormatter {
/* Reliability / redundancy factor rho, NSCP 2015 Eq. 208-20, computed from the STAAD model (owner, 25 November 2026: "How do you compute
   redundancy or reliability factor"; "Yes with automatic staad computation"):
     rho = 2 - 6.1 / (r_max sqrt(A_B)),  A_B = ground floor area (m2), r_max = the largest element-story shear ratio r_i of the stories at or
     below two-thirds of the height;
     moment frames: r_i = largest sum of the shears in two adjacent columns of a bay / story shear (a column common to two bays with moment
       connections on opposite sides counts 70 % of its shear);
     braced frames: r_i = horizontal component of the most heavily loaded brace / story shear;
     dual systems: 80 % of the above;
     shear walls: r_i = Vwall x 3.05 / lw / story shear (typed, walls are plates in STAAD);
     limits: rho >= 1.0, need not exceed 1.5; special moment-resisting frames (not dual) rho <= 1.25 (add bays otherwise); rho = 1.0 for
     drift and in Seismic Zone 2.
   The model comes from the STAAD input (.std: UNIT, SET Z UP, JOINT COORDINATES, MEMBER INCIDENCES, simple generation), the forces from
   the member end forces (STAAD table copy "Beam L/C Node Fx Fy Fz Mx My Mz" or the .ANL MEMBER END FORCES block). Column shear is the
   horizontal resultant sqrt(Fy^2 + Fz^2) of the member end force; the story shear is the sum over the columns and braces of the story. */
sealed class StaadJoint {public int No;public double X,Y,Z;}
sealed class StaadMember {public int No,A,B;}
sealed class StaadModel {public Dictionary<int,StaadJoint> Joints=new Dictionary<int,StaadJoint>();public Dictionary<int,StaadMember> Members=new Dictionary<int,StaadMember>();public double ToMetre=1;public bool ZUp;public List<string> Notes=new List<string>();
 /* plan coordinates and the height, whatever axis is up */
 public double H(StaadJoint j){return ZUp?j.Z:j.Y;}public double P1(StaadJoint j){return j.X;}public double P2(StaadJoint j){return ZUp?j.Y:j.Z;}}
sealed class RhoStory {public double Bottom,Top,Shear,Element,Ratio;public string Element0="";public bool Counted;}
sealed class RhoResult {public string Case,Axis;public List<RhoStory> Stories=new List<RhoStory>();public double RMax,Rho;public string Governing="";}
static class RedundancyFactor {
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;
 static double N(string s){return double.Parse(s,NumberStyles.Float,Ci);}
 static bool IsNum(string s){double v;return double.TryParse(s,NumberStyles.Float,Ci,out v);}
 /* the STAAD input: units, joints and members (with the simple generation forms) */
 internal static StaadModel ReadModel(string text){var m=new StaadModel();string block="";double unit=1;
  foreach(var raw in (text??"").Replace("\r","").Split('\n')){/* the .ANL echoes the input with line numbers ("  12. JOINT COORDINATES") */string line=Regex.Replace(raw.Trim(),@"^\d+\.\s+","");if(line==""||line.StartsWith("*"))continue;string u=line.ToUpperInvariant();
   if(u.StartsWith("UNIT")){if(Regex.IsMatch(u,@"\bMMS?\b"))unit=0.001;else if(Regex.IsMatch(u,@"\bCMS?\b"))unit=0.01;else if(Regex.IsMatch(u,@"\b(METER|METRES?|METERS?|M)\b"))unit=1;else if(Regex.IsMatch(u,@"\b(FEET|FT)\b"))unit=0.3048;else if(Regex.IsMatch(u,@"\b(INCH|INCHES|IN)\b"))unit=0.0254;continue;}
   if(u.StartsWith("SET Z UP")){m.ZUp=true;continue;}
   if(u.StartsWith("JOINT COORD")){block="J";m.ToMetre=unit;line=Regex.Replace(line,@"^JOINT\s+COORD\w*(\s+CYL\w*)?","",RegexOptions.IgnoreCase).Trim();if(line=="")continue;}
   else if(u.StartsWith("MEMBER INCI")){block="M";line=Regex.Replace(line,@"^MEMBER\s+INCI\w*","",RegexOptions.IgnoreCase).Trim();if(line=="")continue;}
   else if(Regex.IsMatch(u,@"^[A-Z]")){if(u.StartsWith("REPEAT"))m.Notes.Add("REPEAT / REPEAT ALL generation in the joint or member list is not read: list the joints and members explicitly.");block="";continue;}
   if(block=="")continue;
   foreach(var part in line.TrimEnd('-').Split(';')){var t=part.Trim().Split(new[]{' ','\t',','},StringSplitOptions.RemoveEmptyEntries);if(t.Length==0||!t.All(IsNum))continue;var v=t.Select(N).ToArray();
    if(block=="J"){if(v.Length==4)m.Joints[(int)v[0]]=new StaadJoint{No=(int)v[0],X=v[1],Y=v[2],Z=v[3]};
     else if(v.Length>=8){int a=(int)v[0],b=(int)v[4],inc=v.Length>=9?(int)v[8]:1;int steps=Math.Max(1,(b-a)/Math.Max(1,inc));for(int k=0;k<=steps;k++){double f=(double)k/steps;int no=a+k*inc;m.Joints[no]=new StaadJoint{No=no,X=v[1]+(v[5]-v[1])*f,Y=v[2]+(v[6]-v[2])*f,Z=v[3]+(v[7]-v[3])*f};}}}
    else{if(v.Length==3)m.Members[(int)v[0]]=new StaadMember{No=(int)v[0],A=(int)v[1],B=(int)v[2]};
     else if(v.Length>=4){int m1=(int)v[0],n1=(int)v[1],n2=(int)v[2],m2=(int)v[3],minc=v.Length>=5?(int)v[4]:1,ninc=v.Length>=6?(int)v[5]:1;for(int k=0;m1+k*minc<=m2;k++)m.Members[m1+k*minc]=new StaadMember{No=m1+k*minc,A=n1+k*ninc,B=n2+k*ninc};}}}}
  if(m.Joints.Count==0||m.Members.Count==0)throw new Exception("Paste the STAAD input (.std): the JOINT COORDINATES and MEMBER INCIDENCES were not found.");
  return m;}
 /* the pasted text holds a STAAD model (.std or the .ANL echo) */
 internal static bool HasModel(string text){return Regex.IsMatch(text??"",@"JOINT\s+COORD",RegexOptions.IgnoreCase)&&Regex.IsMatch(text??"",@"MEMBER\s+INCI",RegexOptions.IgnoreCase);}
 /* the seismic load cases of the STAAD input: LOAD n LOADTYPE SEISMIC... TITLE ...; the direction from the title (EQ+X), else from the load
    data (UBC / IBC / 1893 LOAD X, JOINT LOAD FX / FZ, member loads GX / GZ) */
 internal static List<StaadPrimary> SeismicCases(string text){var list=new List<StaadPrimary>();StaadPrimary cur=null;
  foreach(var raw in (text??"").Replace("\r","").Split('\n')){string line=Regex.Replace(raw.Trim(),@"^\d+\.\s+","");string u=line.ToUpperInvariant();
   if(Regex.IsMatch(u,@"^LOAD\s+\d+")&&!Regex.IsMatch(u,@"\bCOMB")){var p=StaadCombinations.Parse(line).FirstOrDefault();cur=p!=null&&p.Kind=="E"?p:null;if(cur!=null)list.Add(cur);continue;}
   if(Regex.IsMatch(u,@"^(LOAD\s+COMB|PERFORM|FINISH|PRINT|PARAMETER|DEFINE)"))cur=null;
   if(cur!=null&&cur.Axis==""){var a=Regex.Match(u,@"\bLOAD\s+([XZ])\b|\b([FG])([XZ])\b");if(a.Success){cur.Axis=a.Groups[1].Success&&a.Groups[1].Value!=""?a.Groups[1].Value:a.Groups[3].Value;}}}
  return list;}
 /* load case titles of the STAAD table copy (L/C column "5 EQ+X") */
 internal static List<StaadPrimary> TableCases(string text){var lines=new List<string>();
  foreach(var raw in (text??"").Replace("\r","").Split('\n')){var c=raw.Split('\t');if(c.Length>=2){var m=Regex.Match(c[1].Trim(),@"^(\d+)\s*[:.]?\s+(\S.*)$");if(m.Success)lines.Add(m.Groups[1].Value+" "+m.Groups[2].Value);}}
  return StaadCombinations.Parse(string.Join("\n",lines.Distinct())).Where(p=>p.Kind=="E").ToList();}
 /* the model has inclined members (braces) */
 internal static bool HasBraces(StaadModel m){var cols=new List<StaadMember>();var inclined=new List<StaadMember>();
  foreach(var mem in m.Members.Values){StaadJoint a,b;if(!m.Joints.TryGetValue(mem.A,out a)||!m.Joints.TryGetValue(mem.B,out b))continue;double dv=Math.Abs(m.H(b)-m.H(a)),dh=Math.Sqrt(Math.Pow(m.P1(b)-m.P1(a),2)+Math.Pow(m.P2(b)-m.P2(a),2));if(dv>1e-6&&dh<=0.02*dv)cols.Add(mem);else if(dv>1e-6)inclined.Add(mem);}
  return inclined.Any(b=>AtLevel(m,cols,m.Joints[b.A])&&AtLevel(m,cols,m.Joints[b.B]));}
 static bool AtLevel(StaadModel m,List<StaadMember> cols,StaadJoint j){double h=m.H(j);return cols.Any(c=>{StaadJoint a,b;return m.Joints.TryGetValue(c.A,out a)&&Math.Abs(m.H(a)-h)<1e-3||m.Joints.TryGetValue(c.B,out b)&&Math.Abs(m.H(b)-h)<1e-3;});}
 /* member end forces: member, load case, joint -> Fx, Fy, Fz (local); from the STAAD table copy or the .ANL block */
 internal static Dictionary<string,double[]> ReadForces(string text){var f=new Dictionary<string,double[]>();int member=0,load=0;
  foreach(var raw in (text??"").Replace("\r","").Split('\n')){string line=raw.Trim();if(line=="")continue;
   if(raw.Contains("\t")){var c=raw.Split('\t').Select(x=>x.Trim()).ToArray();if(c.Length>=9){int mm,ll,jj;var lc=Regex.Match(c[1],@"^\d+");
     if(c[0]!=""&&int.TryParse(c[0],out mm))member=mm;if(lc.Success)load=int.Parse(lc.Value,Ci);if(int.TryParse(c[2],out jj)&&IsNum(c[3])&&IsNum(c[4])&&IsNum(c[5])&&member>0&&load>0)f[member+"|"+load+"|"+jj]=new[]{N(c[3]),N(c[4]),N(c[5])};}continue;}
   var t=line.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);if(!t.All(IsNum))continue;var v=t.Select(N).ToArray();
   if(v.Length==9){member=(int)v[0];load=(int)v[1];f[member+"|"+load+"|"+(int)v[2]]=new[]{v[3],v[4],v[5]};}
   else if(v.Length==8&&member>0){load=(int)v[0];f[member+"|"+load+"|"+(int)v[1]]=new[]{v[2],v[3],v[4]};}
   else if(v.Length==7&&member>0&&load>0)f[member+"|"+load+"|"+(int)v[0]]=new[]{v[1],v[2],v[3]};}
  if(f.Count==0)throw new Exception("Paste the member end forces of the EQ load cases (STAAD table Beam / L/C / Node / Fx ... or the .ANL MEMBER END FORCES).");
  return f;}
 /* the ground floor area: the convex hull, on plan, of the joints at the base level */
 internal static double BaseArea(StaadModel m){double baseH=m.Joints.Values.Min(j=>m.H(j));var pts=m.Joints.Values.Where(j=>Math.Abs(m.H(j)-baseH)<1e-6).Select(j=>new[]{m.P1(j)*m.ToMetre,m.P2(j)*m.ToMetre}).Distinct(new PtEq()).OrderBy(p=>p[0]).ThenBy(p=>p[1]).ToList();
  if(pts.Count<3)return 0;Func<double[],double[],double[],double> cross=(o,a,b)=>(a[0]-o[0])*(b[1]-o[1])-(a[1]-o[1])*(b[0]-o[0]);var hull=new List<double[]>();
  foreach(var p in pts){while(hull.Count>=2&&cross(hull[hull.Count-2],hull[hull.Count-1],p)<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}int lower=hull.Count+1;
  for(int i=pts.Count-2;i>=0;i--){var p=pts[i];while(hull.Count>=lower&&cross(hull[hull.Count-2],hull[hull.Count-1],p)<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}hull.RemoveAt(hull.Count-1);
  double a2=0;for(int i=0;i<hull.Count;i++){var p=hull[i];var q=hull[(i+1)%hull.Count];a2+=p[0]*q[1]-q[0]*p[1];}return Math.Abs(a2)/2;}
 sealed class PtEq:IEqualityComparer<double[]>{public bool Equals(double[] a,double[] b){return Math.Abs(a[0]-b[0])<1e-6&&Math.Abs(a[1]-b[1])<1e-6;}public int GetHashCode(double[] a){return Math.Round(a[0],4).GetHashCode()^Math.Round(a[1],4).GetHashCode();}}
 /* r_i per story for one load case; system: "Moment", "Braced", "Dual" */
 internal static RhoResult Compute(StaadModel m,Dictionary<string,double[]> forces,int load,string caseName,string axis,string system){var r=new RhoResult{Case=caseName,Axis=axis};
  Func<int,StaadJoint> J=n=>{StaadJoint j;if(!m.Joints.TryGetValue(n,out j))throw new Exception("Joint "+n+" of a member is not in the joint list.");return j;};
  var cols=new List<StaadMember>();var beams=new List<StaadMember>();var braces=new List<StaadMember>();
  var inclined=new List<StaadMember>();
  foreach(var mem in m.Members.Values){var a=J(mem.A);var b=J(mem.B);double dv=Math.Abs(m.H(b)-m.H(a)),dh=Math.Sqrt(Math.Pow(m.P1(b)-m.P1(a),2)+Math.Pow(m.P2(b)-m.P2(a),2));
   if(dv<1e-6&&dh>1e-6)beams.Add(mem);else if(dh<=0.02*dv)cols.Add(mem);else if(dv>1e-6)inclined.Add(mem);}
  /* a brace runs between two floor levels (column ends); sloping rafters and other inclined members are not braces */
  braces.AddRange(inclined.Where(b=>AtLevel(m,cols,J(b.A))&&AtLevel(m,cols,J(b.B))));
  if(cols.Count==0)throw new Exception("No columns (vertical members) found in the model.");
  Func<StaadMember,StaadJoint> low=mm=>m.H(J(mm.A))<=m.H(J(mm.B))?J(mm.A):J(mm.B);Func<StaadMember,StaadJoint> high=mm=>m.H(J(mm.A))<=m.H(J(mm.B))?J(mm.B):J(mm.A);
  Func<StaadMember,double[]> F=mm=>{double[] v;int jl=low(mm).No;if(forces.TryGetValue(mm.No+"|"+load+"|"+jl,out v))return v;int other=jl==mm.A?mm.B:mm.A;return forces.TryGetValue(mm.No+"|"+load+"|"+other,out v)?v:null;};
  double baseH=m.Joints.Values.Min(j=>m.H(j)),topH=m.Joints.Values.Max(j=>m.H(j)),limit=baseH+2.0/3*(topH-baseH);bool missing=false;
  foreach(var lvl in cols.Select(c=>Math.Round(m.H(low(c)),4)).Distinct().OrderBy(x=>x)){
   var sc=cols.Where(c=>Math.Abs(m.H(low(c))-lvl)<1e-3).ToList();var shear=new Dictionary<int,double>();var topOf=new Dictionary<int,int>();
   foreach(var c in sc){var v=F(c);if(v==null){missing=true;continue;}shear[c.No]=Math.Sqrt(v[1]*v[1]+v[2]*v[2]);topOf[c.No]=high(c).No;}
   var sb=braces.Where(b=>Math.Abs(m.H(low(b))-lvl)<1e-3).ToList();var bh=new Dictionary<int,double>();
   foreach(var b in sb){var v=F(b);if(v==null){missing=true;continue;}var a=J(b.A);var e=J(b.B);double L=Math.Sqrt(Math.Pow(m.P1(e)-m.P1(a),2)+Math.Pow(m.P2(e)-m.P2(a),2)+Math.Pow(m.H(e)-m.H(a),2)),h=Math.Sqrt(Math.Pow(m.P1(e)-m.P1(a),2)+Math.Pow(m.P2(e)-m.P2(a),2));bh[b.No]=Math.Abs(v[0])*h/L;}
   var st=new RhoStory{Bottom=(lvl-baseH)*m.ToMetre,Top=(sc.Max(c=>m.H(high(c)))-baseH)*m.ToMetre,Shear=shear.Values.Sum()+bh.Values.Sum()};st.Counted=lvl<=limit+1e-6;
   if(st.Shear<=1e-9){r.Stories.Add(st);continue;}
   /* moment-frame bays along the loading direction: two columns whose tops a beam joins */
   double best=0;string what="";bool alongX=axis!="Z";
   if(system!="Braced"){var tops=topOf.ToDictionary(p=>p.Value,p=>p.Key);
    Func<StaadMember,bool> along=bm=>{var a=J(bm.A);var e=J(bm.B);double d1=Math.Abs(m.P1(e)-m.P1(a)),d2=Math.Abs(m.P2(e)-m.P2(a));return alongX?d1>=d2:d2>d1;};
    var bayBeams=beams.Where(bm=>tops.ContainsKey(bm.A)&&tops.ContainsKey(bm.B)&&along(bm)).ToList();
    /* 70 %: a column with frame beams on both sides along the loading direction */
    Func<int,double> share=node=>{var at=bayBeams.Where(bm=>bm.A==node||bm.B==node).Select(bm=>{var o=J(bm.A==node?bm.B:bm.A);var n0=J(node);return Math.Sign(alongX?m.P1(o)-m.P1(n0):m.P2(o)-m.P2(n0));}).ToList();return at.Contains(1)&&at.Contains(-1)?0.7:1.0;};
    foreach(var bm in bayBeams){double v=shear[tops[bm.A]]*share(bm.A)+shear[tops[bm.B]]*share(bm.B);if(v>best){best=v;what="columns "+tops[bm.A]+" + "+tops[bm.B]+(share(bm.A)<1||share(bm.B)<1?" (70 % of an interior column)":"");}}
    if(bayBeams.Count==0&&shear.Count>0){var mx=shear.OrderByDescending(p=>p.Value).First();best=mx.Value;what="column "+mx.Key+" (no beam along the loading direction joins two column tops)";}}
   if(!system.StartsWith("Moment")&&bh.Count>0){var mb=bh.OrderByDescending(p=>p.Value).First();if(system=="Braced"||mb.Value>best){best=mb.Value;what="brace "+mb.Key+" (horizontal component)";}}
   st.Element=best;st.Element0=what;st.Ratio=best/st.Shear*(system=="Dual"?0.8:1.0);r.Stories.Add(st);}
  if(missing)r.Governing="Some members have no end forces for load case "+load+" (copy the end forces of every member for this case). ";
  var counted=r.Stories.Where(s=>s.Counted&&s.Shear>1e-9).ToList();if(counted.Count==0)throw new Exception("Load case "+load+": no story shear found at or below two-thirds of the height.");
  var g=counted.OrderByDescending(s=>s.Ratio).First();r.RMax=g.Ratio;r.Governing+="r_max = "+g.Ratio.ToString("0.000",Ci)+" at the story from "+g.Bottom.ToString("0.##",Ci)+" m ("+g.Element0+")";return r;}
 /* rho from r_max and A_B with the limits; returns the raw value through raw */
 internal static double Rho(double rMax,double area,string system,bool zone2,out double raw,out string note){raw=rMax>0&&area>0?2-6.1/(rMax*Math.Sqrt(area)):1;note="";
  if(zone2){note="Seismic Zone 2: ρ = 1.0";return 1.0;}double rho=Math.Max(1.0,Math.Min(1.5,raw));
  if(raw<1)note="below 1.0, so ρ = 1.0";else if(raw>1.5)note="above 1.5, so ρ = 1.5 (need not exceed 1.5)";
  if(system=="Moment SMRF"&&rho>1.25)note+=(note==""?"":"; ")+"special moment-resisting frame: ρ shall not exceed 1.25 - add moment-frame bays to reduce r_max (NG)";
  return rho;}
}
}
