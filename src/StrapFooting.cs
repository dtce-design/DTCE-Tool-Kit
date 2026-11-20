using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Text.RegularExpressions;
namespace BeamScheduleFormatter {
/* Strap (cantilever) foundation design sheet (owner, 19 November 2026: "Strap Foundation. Ready input ng reaction kay staad. Yung
   foundation niya adjustable lahat yung pagka eccentric. Anong direction ng strap or double or triple any. Directions is same kay
   staad"). Footings sit under STAAD support nodes in the STAAD global axes (Y up by default, plan X-Z; or Z up, plan X-Y). Each
   footing can be off-centre (ex, ez) from its column. Straps join any two columns in any direction; a footing can have one, two,
   three or more straps.
   Statics (rigid footings and straps, straps clear of the soil): the strap end moments at a footing whose moment goes to the
   straps are chosen so that the soil pressure under that footing is as uniform as the straps allow (least squares over the strap
   directions; two straps in different directions make it uniform, one strap leaves the part across the strap to the footing).
   A footing whose moment goes to the footing is the fulcrum of the strap (no strap moment there). Each strap carries a constant
   shear between the columns, S = (Mi + Mj) / L, which unloads the eccentric footing's neighbours: the classic two-footing case
   gives R = P L / (L - e). Design to NSCP 2015 (ACI 318-14): 413 footings, 409 strap beam, 422 shear and punching. */
class StrapNode {public string Node="";public double X,Z,Cx=.4,Cz=.4,B=1.5,L=1.5,T=.5,Ex,Ez;public string Mode="Auto",Bars="16@150";
 internal bool ToStraps(int straps){return straps>0&&(Mode=="Straps"||Mode=="Auto"&&Math.Sqrt(Ex*Ex+Ez*Ez)>.001);}}
class StrapBeam {public string From="",To="",Top="4-20",Bottom="3-20",Stirrups="10@150";public double Width=.4,Depth=.75;}
class StrapInput {public List<StrapNode> Nodes=new List<StrapNode>();public List<StrapBeam> Straps=new List<StrapBeam>();
 public double Fc=21,Fy=415,Fyt=275,Cover=75,StrapCover=40,Qa=150,Df=1.5,GammaSoil=18,GammaConc=24,SupportHeight=1.0,Friction=.4,Aggregate=20,SlidingFS=1.5,OverturningFS=1.5;public string Vertical="Y";
 public Func<string,bool> Factored=StrapFooting.LooksFactored;}
class StrapCase {public string Case;public bool Factored;public double[] P,R,Mp,Mq,Qmax,Qmin,Qavg,H,Hp,Hq,Mrp,Mrq,Wf;public double[] MuA,MuB,Shear,W;}
static class StrapFooting {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 internal static string F(double v,string f="0.0"){return double.IsInfinity(v)?"∞":v.ToString(f,Inv);}
 /* a load combination with load factors (1.2D, 1.4D, 1.6L, 0.9D ...) is factored; otherwise service */
 internal static bool LooksFactored(string name){return Regex.IsMatch(name??"",@"(^|[^0-9.])(1\.2|1\.4|1\.6|0\.9|1\.0E|1\.0\s*E)",RegexOptions.IgnoreCase);}
 sealed class Load {public double P,Hp,Hq,Mp,Mq;}
 /* plan coordinates and loads in one right-handed frame: Y up -> plan (X, Z), moments about X and Z; Z up -> plan (X, Y), the
    moments are turned so that the same formulas apply */
 static Load LoadOf(SupportReaction r,string vertical){if(vertical=="Z")return new Load{P=r.Fz,Hp=r.Fx,Hq=r.Fy,Mp=-r.Mx,Mq=-r.My};return new Load{P=r.Fy,Hp=r.Fx,Hq=r.Fz,Mp=r.Mx,Mq=r.Mz};}
 internal static void Validate(StrapInput i){if(i.Nodes.Count==0)throw new Exception("Add the footings (one row per STAAD support node).");
  var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);foreach(var n in i.Nodes){if(!names.Add(n.Node))throw new Exception("Footing node "+n.Node+" is listed twice.");if(n.B<=0||n.L<=0||n.T<=0||n.Cx<=0||n.Cz<=0)throw new Exception("Footing "+n.Node+": sizes must be more than 0.");
   if(Math.Abs(n.Ex)+n.Cx/2>n.B/2+1e-9||Math.Abs(n.Ez)+n.Cz/2>n.L/2+1e-9)throw new Exception("Footing "+n.Node+": the column must sit on the footing (|e| + c/2 <= B/2 and L/2).");}
  foreach(var s in i.Straps){var a=i.Nodes.FirstOrDefault(n=>n.Node.Equals(s.From,StringComparison.OrdinalIgnoreCase));var b=i.Nodes.FirstOrDefault(n=>n.Node.Equals(s.To,StringComparison.OrdinalIgnoreCase));if(a==null||b==null)throw new Exception("Strap "+s.From+" - "+s.To+": both ends must be footing nodes.");if(a==b)throw new Exception("Strap "+s.From+" - "+s.To+": the ends must differ.");
   if(Length(a,b)<.5)throw new Exception("Strap "+s.From+" - "+s.To+": the columns are less than 0.5 m apart.");if(s.Width<=0||s.Depth<=0)throw new Exception("Strap "+s.From+" - "+s.To+": enter the width and depth.");}
  if(i.Fc<17||i.Fy<200||i.Qa<=0||i.Df<0)throw new Exception("Check fc' (>= 17 MPa), fy, allowable bearing and Df.");}
 internal static double Length(StrapNode a,StrapNode b){return Math.Sqrt((b.X-a.X)*(b.X-a.X)+(b.Z-a.Z)*(b.Z-a.Z));}
 internal static int Index(StrapInput i,string node){return i.Nodes.FindIndex(n=>n.Node.Equals(node,StringComparison.OrdinalIgnoreCase));}
 internal static double FootingWeight(StrapInput i,StrapNode n){return n.B*n.L*(n.T*i.GammaConc+Math.Max(0,i.Df-n.T)*i.GammaSoil);}
 internal static double StrapWeight(StrapInput i,StrapBeam s){return s.Width*s.Depth*i.GammaConc;}
 /* one load case: soil resultants R, strap end moments and shears, soil pressures */
 internal static StrapCase Solve(StrapInput inp,string name,Dictionary<string,SupportReaction> rows,bool factored){int n=inp.Nodes.Count,m=inp.Straps.Count,N=n+2*m;var A=new double[N,N];var rhs=new double[N];var load=new Load[n];var W=new double[n];var res=new StrapCase{Case=name,Factored=factored,P=new double[n],R=new double[n],Mp=new double[n],Mq=new double[n],Qmax=new double[n],Qmin=new double[n],Qavg=new double[n],H=new double[n],Hp=new double[n],Hq=new double[n],Mrp=new double[n],Mrq=new double[n],Wf=new double[n],MuA=new double[m],MuB=new double[m],Shear=new double[m],W=new double[m]};
  for(int k=0;k<n;k++){SupportReaction r;if(!rows.TryGetValue(inp.Nodes[k].Node,out r))throw new Exception("Load case "+name+" has no reaction at node "+inp.Nodes[k].Node+".");load[k]=LoadOf(r,inp.Vertical);res.P[k]=load[k].P;res.H[k]=Math.Sqrt(load[k].Hp*load[k].Hp+load[k].Hq*load[k].Hq);res.Hp[k]=load[k].Hp;res.Hq[k]=load[k].Hq;res.Mrp[k]=load[k].Mp;res.Mrq[k]=load[k].Mq;W[k]=factored?0:FootingWeight(inp,inp.Nodes[k]);res.Wf[k]=W[k];}
  var u=new double[m,2];var L=new double[m];var ia=new int[m];var ib=new int[m];double[] wStrap=new double[m];
  for(int s=0;s<m;s++){ia[s]=Index(inp,inp.Straps[s].From);ib[s]=Index(inp,inp.Straps[s].To);var a=inp.Nodes[ia[s]];var b=inp.Nodes[ib[s]];L[s]=Length(a,b);u[s,0]=(b.X-a.X)/L[s];u[s,1]=(b.Z-a.Z)/L[s];wStrap[s]=(factored?1.2:1.0)*StrapWeight(inp,inp.Straps[s]);res.W[s]=wStrap[s];}
  Func<int,double[]> t=s=>new[]{u[s,1],-u[s,0]};
  var straps=new List<int>[n];for(int k=0;k<n;k++)straps[k]=new List<int>();for(int s=0;s<m;s++){straps[ia[s]].Add(s);straps[ib[s]].Add(s);}
  Func<int,int,int> mu=(s,k)=>n+2*s+(ia[s]==k?0:1);
  for(int k=0;k<n;k++){var nd=inp.Nodes[k];var ld=load[k];
   /* vertical: R - S_strap terms = P + W + half the strap weights */
   A[k,k]=1;foreach(int s in straps[k]){double sign=ia[s]==k?-1:1;/* S on footing: i end -(mi+mj)/L, j end +(mi+mj)/L; R = P + W - S */A[k,n+2*s]+=sign/L[s];A[k,n+2*s+1]+=sign/L[s];rhs[k]+=wStrap[s]*L[s]/2;}rhs[k]+=ld.P+W[k];
   /* moments about the base point under the column, plan components (p, q) */
   double[] baseConst={W[k]*nd.Ez+(-ld.Mp)+inp.SupportHeight*(-ld.Hq),-W[k]*nd.Ex+(-ld.Mq)-inp.SupportHeight*(-ld.Hp)};double[] rCoef={-nd.Ez,nd.Ex};
   bool toStraps=nd.ToStraps(straps[k].Count);
   foreach(int s in straps[k]){int row=mu(s,k);if(!toStraps){A[row,row]=1;continue;}var tk=t(s);A[row,k]+=tk[0]*rCoef[0]+tk[1]*rCoef[1];foreach(int l in straps[k]){var tl=t(l);A[row,mu(l,k)]+=tk[0]*tl[0]+tk[1]*tl[1];}A[row,row]+=1e-7;rhs[row]-=tk[0]*baseConst[0]+tk[1]*baseConst[1];}}
  var x=Gauss(A,rhs);
  for(int s=0;s<m;s++){res.MuA[s]=x[n+2*s];res.MuB[s]=x[n+2*s+1];res.Shear[s]=(res.MuA[s]+res.MuB[s])/L[s];}
  for(int k=0;k<n;k++){var nd=inp.Nodes[k];var ld=load[k];double R=x[k];res.R[k]=R;
   double mp=R*(-nd.Ez)+W[k]*nd.Ez-ld.Mp-inp.SupportHeight*ld.Hq,mq=R*nd.Ex-W[k]*nd.Ex-ld.Mq+inp.SupportHeight*ld.Hp;foreach(int s in straps[k]){var ts=t(s);double v=x[mu(s,k)];mp+=v*ts[0];mq+=v*ts[1];}
   /* the soil supplies the rest: moment about the footing centre */
   res.Mp[k]=-mp;res.Mq[k]=-mq;double area=nd.B*nd.L,Ip=nd.B*nd.B*nd.B*nd.L/12,Iq=nd.B*nd.L*nd.L*nd.L/12;double gp=res.Mq[k]/Ip,gq=-res.Mp[k]/Iq;double q0=R/area,dq=Math.Abs(gp)*nd.B/2+Math.Abs(gq)*nd.L/2;res.Qavg[k]=q0;res.Qmax[k]=q0+dq;res.Qmin[k]=q0-dq;}
  return res;}
 internal static double[] Gauss(double[,] a,double[] b){int n=b.Length;var m=(double[,])a.Clone();var r=(double[])b.Clone();for(int c=0;c<n;c++){int p=c;for(int i=c+1;i<n;i++)if(Math.Abs(m[i,c])>Math.Abs(m[p,c]))p=i;if(Math.Abs(m[p,c])<1e-12)throw new Exception("The strap system cannot be solved (a footing or strap is not connected properly).");
   if(p!=c){for(int j=0;j<n;j++){double tmp=m[c,j];m[c,j]=m[p,j];m[p,j]=tmp;}double t2=r[c];r[c]=r[p];r[p]=t2;}for(int i=c+1;i<n;i++){double f=m[i,c]/m[c,c];if(f==0)continue;for(int j=c;j<n;j++)m[i,j]-=f*m[c,j];r[i]-=f*r[c];}}
  var x=new double[n];for(int i=n-1;i>=0;i--){double s=r[i];for(int j=i+1;j<n;j++)s-=m[i,j]*x[j];x[i]=s/m[i,i];}return x;}
 /* all load cases present at every footing node */
 internal static List<StrapCase> Cases(StrapInput inp,List<SupportReaction> reactions,List<string> notes){var list=new List<StrapCase>();var nodes=new HashSet<string>(inp.Nodes.Select(n=>n.Node),StringComparer.OrdinalIgnoreCase);
  foreach(var g in reactions.GroupBy(r=>r.Case)){var map=new Dictionary<string,SupportReaction>(StringComparer.OrdinalIgnoreCase);foreach(var r in g){string node=r.Node.Split(' ')[0];if(nodes.Contains(node))map[node]=r;}
   if(map.Count<nodes.Count){if(map.Count>0)notes.Add("Load case "+g.Key+" skipped: no reaction at "+string.Join(", ",nodes.Where(x=>!map.ContainsKey(x)))+".");continue;}list.Add(Solve(inp,g.Key,map,inp.Factored(g.Key)));}
  if(list.Count==0)throw new Exception("No load case has reactions at all the footing nodes ("+string.Join(", ",nodes)+").");return list;}
 /* ---------- design ---------- */
 internal sealed class Bars {public int Dia,Count;public double Spacing;public double Area{get{return Count>0?Count*Math.PI*Dia*Dia/4:Math.PI*Dia*Dia/4*1000/Spacing;}}}
 internal static Bars ParseBars(string s){var t=(s??"").Replace(" ","").Replace("Ø","").Replace("#","").ToUpperInvariant();var m=Regex.Match(t,@"^(\d+)-(\d+)$");if(m.Success)return new Bars{Count=int.Parse(m.Groups[1].Value,Inv),Dia=int.Parse(m.Groups[2].Value,Inv)};m=Regex.Match(t,@"^(\d+)@(\d+)$");if(m.Success)return new Bars{Dia=int.Parse(m.Groups[1].Value,Inv),Spacing=double.Parse(m.Groups[2].Value,Inv)};throw new Exception("Bars \""+s+"\": write n-dia (4-20) or dia@spacing (16@150).");}
 static double AsReq(double MuKnm,double bMm,double dMm,double fc,double fy){if(MuKnm<=0)return 0;double Mu=MuKnm*1e6,phi=.9;double Rn=Mu/(phi*bMm*dMm*dMm);double k=1-2*Rn/(.85*fc);if(k<0)return double.PositiveInfinity;return .85*fc/fy*(1-Math.Sqrt(k))*bMm*dMm;}
 static void Add(List<SupportCheck> list,Dictionary<SupportCheck,List<string>> steps,string name,string clause,string node,string cas,double demand,double capacity,string unit,params string[] lines){var c=new SupportCheck{Name=name,Clause=clause,Node=node,Case=cas,Demand=demand,Capacity=capacity,Unit=unit,Ratio=capacity<=0?(demand<=0?0:double.PositiveInfinity):demand/capacity};list.Add(c);steps[c]=lines.ToList();}
 public static List<SupportCheck> Check(StrapInput inp,List<StrapCase> cases,List<string> notes,Dictionary<SupportCheck,List<string>> steps){Validate(inp);var list=new List<SupportCheck>();var service=cases.Where(c=>!c.Factored).ToList();var factored=cases.Where(c=>c.Factored).ToList();
  if(service.Count==0)notes.Add("No service load case found: bearing is not checked. Load cases with factors (1.2, 1.4, 1.6, 0.9) are taken as factored; mark the service cases in the load case list.");
  if(factored.Count==0)notes.Add("No factored load case found: footing and strap strength are not checked.");
  double sq=Math.Sqrt(inp.Fc);
  for(int k=0;k<inp.Nodes.Count;k++){var nd=inp.Nodes[k];string id="F"+nd.Node;double area=nd.B*nd.L;
   if(service.Count>0){var g=service.OrderByDescending(c=>c.Qmax[k]).First();double w=FootingWeight(inp,nd);
    Add(list,steps,"Bearing pressure (service)","413.3.1",id,g.Case,g.Qmax[k],inp.Qa,"kPa","R = P + W(footing + soil) +/- strap shears = "+F(g.P[k])+" + "+F(w)+" -> R = "+F(g.R[k])+" kN","q = R / A +/- M / S = "+F(g.R[k])+" / "+F(area,"0.00")+" +/- ...  ->  q avg "+F(g.Qavg[k])+", q max "+F(g.Qmax[k])+" kPa","residual moment on the footing (not taken by straps): Mx "+F(g.Mp[k])+", Mz "+F(g.Mq[k])+" kN·m","q max <= qa (gross allowable bearing) = "+F(inp.Qa)+" kPa");
    var u2=service.OrderBy(c=>c.Qmin[k]).First();double ratio=u2.Qavg[k]<=0?double.PositiveInfinity:(u2.Qmax[k]-u2.Qavg[k])/u2.Qavg[k];var cc=new SupportCheck{Name="Full contact, no uplift (service)",Clause="413.3",Node=id,Case=u2.Case,Demand=u2.Qmax[k]-u2.Qavg[k],Capacity=u2.Qavg[k],Unit="kPa",Ratio=ratio};list.Add(cc);steps[cc]=new List<string>{"q min = q avg - Δq = "+F(u2.Qavg[k])+" - "+F(u2.Qmax[k]-u2.Qavg[k])+" = "+F(u2.Qmin[k])+" kPa (must be >= 0)","Δq / q avg <= 1 keeps the whole footing in contact"};}
   if(service.Count>0){double fsS=inp.SlidingFS;var gs=service.OrderByDescending(c=>c.R[k]<=0?double.PositiveInfinity:fsS*c.H[k]/(inp.Friction*c.R[k])).First();double resist=inp.Friction*Math.Max(0,gs.R[k]);
    Add(list,steps,"Sliding of the footing (service), FS >= "+F(fsS,"0.0#"),"Geotechnical (μ R / H)",id,gs.Case,fsS*gs.H[k],resist,"kN","H = √(Hx² + H"+(inp.Vertical=="Z"?"y":"z")+"²) = "+F(gs.H[k])+" kN at the column","friction μ R = "+F(inp.Friction,"0.00")+" x "+F(gs.R[k])+" = "+F(resist)+" kN (passive soil and the strap not counted)","FS = μ R / H = "+(gs.H[k]>1e-9?F(resist/gs.H[k],"0.00"):"∞")+";  "+F(fsS,"0.0#")+" H <= μ R");}
   if(factored.Count==0)continue;
   var gf=factored.OrderByDescending(c=>c.Qmax[k]).First();double qu=gf.Qmax[k];var bars=ParseBars(nd.Bars);double T=nd.T*1000,d=T-inp.Cover-bars.Dia;if(d<=0){notes.Add(id+": footing too thin for the cover and bars.");continue;}
   foreach(int dir in new[]{0,1}){double B=dir==0?nd.B:nd.L,Lw=dir==0?nd.L:nd.B,e=dir==0?nd.Ex:nd.Ez,c=dir==0?nd.Cx:nd.Cz;double l1=B/2-e-c/2,l2=B/2+e-c/2,lc=Math.Max(l1,l2);string ax=dir==0?"X":(inp.Vertical=="Z"?"Y":"Z");
    double Mu=qu*lc*lc/2*Lw;double asReq=Math.Max(AsReq(Mu,Lw*1000,d,inp.Fc,inp.Fy),(inp.Fy>=400?.0018:.002)*Lw*1000*T);double asProv=bars.Area*Lw;
    Add(list,steps,"Footing flexure, bars along "+ax,"413.2.6.4 / 422.2",id,gf.Case,asReq,asProv,"mm²","cantilever from the column face: l = max("+F(l1,"0.000")+", "+F(l2,"0.000")+") = "+F(lc,"0.000")+" m (column off-centre by "+F(e,"0.000")+" m)","Mu = qu l² / 2 x width = "+F(qu)+" x "+F(lc,"0.000")+"² / 2 x "+F(Lw,"0.00")+" = "+F(Mu)+" kN·m (qu = max factored net pressure)","d = h - cover - db = "+F(T,"0")+" - "+F(inp.Cover,"0")+" - "+bars.Dia+" = "+F(d,"0")+" mm","As req = max(flexure, "+(inp.Fy>=400?"0.0018":"0.0020")+" b h) = "+F(asReq,"0")+" mm²;  provided Ø"+bars.Dia+" @ "+F(bars.Spacing,"0")+" -> "+F(asProv,"0")+" mm²");
    double Vu=qu*Math.Max(0,lc-d/1000)*Lw,phiVc=.75*.17*sq*Lw*1000*d/1000;Add(list,steps,"Footing one-way shear, along "+ax,"422.5.5.1",id,gf.Case,Vu,phiVc,"kN","Vu = qu (l - d) width = "+F(qu)+" x ("+F(lc,"0.000")+" - "+F(d/1000,"0.000")+") x "+F(Lw,"0.00")+" = "+F(Vu)+" kN","φVc = 0.75 x 0.17 √fc' b d = 0.75 x 0.17 x "+F(sq,"0.00")+" x "+F(Lw*1000,"0")+" x "+F(d,"0")+" / 1000 = "+F(phiVc)+" kN");}
   /* punching (422.6): critical section d/2 from the column, clipped at the footing edges (edge / corner column); shear plus the
      eccentric shear of the unbalanced moment, gamma_v M c / Jc (422.6.3.6 / 408.4.4.2) */
   {double dd=d/1000;double x1=-nd.Ex-(nd.Cx+dd)/2,x2=-nd.Ex+(nd.Cx+dd)/2,z1=-nd.Ez-(nd.Cz+dd)/2,z2=-nd.Ez+(nd.Cz+dd)/2;bool L1=x1>-nd.B/2,R1=x2<nd.B/2,B1=z1>-nd.L/2,T1=z2<nd.L/2;x1=Math.Max(x1,-nd.B/2);x2=Math.Min(x2,nd.B/2);z1=Math.Max(z1,-nd.L/2);z2=Math.Min(z2,nd.L/2);
    var segs=new List<double[]>();/* x1,z1,x2,z2 */if(L1)segs.Add(new[]{x1,z1,x1,z2});if(R1)segs.Add(new[]{x2,z1,x2,z2});if(B1)segs.Add(new[]{x1,z1,x2,z1});if(T1)segs.Add(new[]{x1,z2,x2,z2});int sides=segs.Count;
    double b0=segs.Sum(q=>Math.Abs(q[2]-q[0])+Math.Abs(q[3]-q[1]));double xc=segs.Sum(q=>(Math.Abs(q[2]-q[0])+Math.Abs(q[3]-q[1]))*(q[0]+q[2])/2)/b0,zc=segs.Sum(q=>(Math.Abs(q[2]-q[0])+Math.Abs(q[3]-q[1]))*(q[1]+q[3])/2)/b0;
    double Jx=0,Jz=0;foreach(var q in segs){bool alongX=Math.Abs(q[3]-q[1])<1e-12;double len=Math.Abs(q[2]-q[0])+Math.Abs(q[3]-q[1]);if(alongX){Jx+=dd*(Math.Pow(Math.Max(q[0],q[2])-xc,3)-Math.Pow(Math.Min(q[0],q[2])-xc,3))/3+len*dd*dd*dd/12;Jz+=dd*len*Math.Pow(q[1]-zc,2);}else{Jz+=dd*(Math.Pow(Math.Max(q[1],q[3])-zc,3)-Math.Pow(Math.Min(q[1],q[3])-zc,3))/3+len*dd*dd*dd/12;Jx+=dd*len*Math.Pow(q[0]-xc,2);}}
    double cX=Math.Max(Math.Abs(x1-xc),Math.Abs(x2-xc)),cZ=Math.Max(Math.Abs(z1-zc),Math.Abs(z2-zc));double bx=x2-x1,bz=z2-z1;double gvX=1-1/(1+2.0/3*Math.Sqrt(bx/bz)),gvZ=1-1/(1+2.0/3*Math.Sqrt(bz/bx));
    bool strapsTake=nd.ToStraps(inp.Straps.Count(st=>st.From==nd.Node||st.To==nd.Node));double Ru=gf.R[k];double mp=gf.Mp[k]+(strapsTake?0:zc*Ru),mq=gf.Mq[k]+(strapsTake?0:-xc*Ru);
    double Vu=Math.Max(0,Ru*(1-bx*bz/area));double vu=Vu/(b0*dd)/1000+gvX*Math.Abs(mq)*cX/Jx/1000+gvZ*Math.Abs(mp)*cZ/Jz/1000;
    double alpha=sides>=4?40:sides==3?30:20;double beta=Math.Max(nd.Cx,nd.Cz)/Math.Min(nd.Cx,nd.Cz);double vc=Math.Min(.33*sq,Math.Min(.17*(1+2/beta)*sq,.083*(alpha*d/(b0*1000)+2)*sq));
    Add(list,steps,"Footing punching shear (two-way)","422.6.5 / 422.6.3.6",id,gf.Case,vu,.75*vc,"MPa","critical section d/2 from the column: "+(sides>=4?"interior":sides==3?"edge":"corner")+" column, b0 = "+F(b0*1000,"0")+" mm, d = "+F(d,"0")+" mm, αs = "+F(alpha,"0"),"Vu = Ru (1 - A0 / A) = "+F(Ru)+" x (1 - "+F(bx*bz,"0.000")+" / "+F(area,"0.000")+") = "+F(Vu)+" kN","unbalanced moment at the critical centroid "+(strapsTake?"(eccentricity taken by the straps)":"(soil about the critical section)")+": Mx "+F(mp)+", Mz "+F(mq)+" kN·m;  γv = "+F(gvX,"0.00")+" / "+F(gvZ,"0.00")+";  Jc = "+F(Jx*1e12/1e9,"0")+" / "+F(Jz*1e12/1e9,"0")+" x10⁹ mm⁴","vu = Vu / (b0 d) + γv Mu c / Jc = "+F(Vu/(b0*dd)/1000,"0.000")+" + "+F(gvX*Math.Abs(mq)*cX/Jx/1000+gvZ*Math.Abs(mp)*cZ/Jz/1000,"0.000")+" = "+F(vu,"0.000")+" MPa","vc = min(0.33, 0.17(1 + 2/β), 0.083(αs d / b0 + 2)) √fc' = "+F(vc,"0.000")+" MPa;  φvc = 0.75 vc = "+F(.75*vc,"0.000")+" MPa");}
   /* depth, spacing, development, bearing at the column, sliding */
   Add(list,steps,"Footing depth above the bottom bars","413.3.1.2",id,"-",150,d,"mm","d >= 150 mm: d = "+F(d,"0")+" mm");
   {double smax=Math.Min(3*T,450);Add(list,steps,"Footing bar spacing","413.3.2 / 407.7.2.3",id,"-",bars.Spacing,smax,"mm","s <= min(3h, 450) = "+F(smax,"0")+" mm; provided "+F(bars.Spacing,"0")+" mm");}
   foreach(int dir in new[]{0,1}){double B=dir==0?nd.B:nd.L,e=dir==0?nd.Ex:nd.Ez,c=dir==0?nd.Cx:nd.Cz;double lc=Math.Max(B/2-e-c/2,B/2+e-c/2)*1000;double db=bars.Dia;double cb=Math.Min(inp.Cover+db/2,bars.Spacing/2);double conf=Math.Min(2.5,cb/db);double psiS=db<=19?.8:1.0;double ld=Math.Max(300,inp.Fy/(1.1*sq)*psiS/conf*db);double avail=lc-75;string ax=dir==0?"X":(inp.Vertical=="Z"?"Y":"Z");
    Add(list,steps,"Development of bars along "+ax+" from the column face","413.2.8 / 425.4.2.3",id,"-",ld,avail,"mm","ld = fy ψt ψe ψs / (1.1 λ √fc' (cb + Ktr)/db) db = "+F(inp.Fy,"0")+" x "+F(psiS,"0.0")+" / (1.1 x "+F(sq,"0.00")+" x "+F(conf,"0.00")+") x "+db+" = "+F(ld,"0")+" mm (>= 300; cb = "+F(cb,"0")+" mm, Ktr = 0)","available = cantilever "+F(lc,"0")+" - 75 end cover = "+F(avail,"0")+" mm"+(ld>avail?" - use standard hooks at the bar ends":""));}
   {double A1=nd.Cx*nd.Cz;double ext=Math.Min(Math.Min(Math.Min(nd.B/2-nd.Ex-nd.Cx/2,nd.B/2+nd.Ex-nd.Cx/2),Math.Min(nd.L/2-nd.Ez-nd.Cz/2,nd.L/2+nd.Ez-nd.Cz/2)),2*nd.T);ext=Math.Max(0,ext);double A2=(nd.Cx+2*ext)*(nd.Cz+2*ext);double ratio=Math.Min(2,Math.Sqrt(A2/A1));double phiBn=.65*.85*inp.Fc*A1*1e6*ratio/1000;double Pu=factored.Max(c=>c.P[k]);
    Add(list,steps,"Bearing at the column base","422.8.3",id,factored.OrderByDescending(c=>c.P[k]).First().Case,Pu,phiBn,"kN","A1 = "+F(nd.Cx*1000,"0")+" x "+F(nd.Cz*1000,"0")+" mm;  A2 = (c + 2e)² with e = min(edge distances, 2h) = "+F(ext*1000,"0")+" mm -> √(A2/A1) = "+F(ratio,"0.00")+" (<= 2)","φBn = 0.65 x 0.85 fc' A1 √(A2/A1) = "+F(phiBn)+" kN;  Pu = "+F(Pu)+" kN (dowels per 416.3.4: at least 0.005 Ag across the interface)");}
   foreach(var c in cases)if(c.R[k]<0){notes.Add(id+": the soil reaction is negative in load case "+c.Case+" ("+F(c.R[k])+" kN) - the strap lifts this footing; add weight or change the layout.");break;}}
  for(int s=0;s<inp.Straps.Count;s++){var st=inp.Straps[s];var a=inp.Nodes[Index(inp,st.From)];var b=inp.Nodes[Index(inp,st.To)];double L=Length(a,b);string id="S"+st.From+"-"+st.To;if(factored.Count==0)continue;
   double ux=(b.X-a.X)/L,uz=(b.Z-a.Z)/L;double ha=(Math.Abs(ux)*a.Cx+Math.Abs(uz)*a.Cz)/2,hb=(Math.Abs(ux)*b.Cx+Math.Abs(uz)*b.Cz)/2;
   double posMax=0,negMax=0,vMax=0;string posCase="-",negCase="-",vCase="-";
   foreach(var c in factored){for(int k=0;k<=40;k++){double x=ha+(L-ha-hb)*k/40.0;double M=Moment(c,s,L,x);if(M>posMax){posMax=M;posCase=c.Case;}if(-M>negMax){negMax=-M;negCase=c.Case;}}double v=Math.Abs(c.Shear[s])+c.W[s]*L/2;if(v>vMax){vMax=v;vCase=c.Case;}}
   var top=ParseBars(st.Top);var bot=ParseBars(st.Bottom);var stir=ParseBars(st.Stirrups);double bw=st.Width*1000,h=st.Depth*1000;
   foreach(var face in new[]{true,false}){var bars=face?top:bot;double Mu=face?negMax:posMax;var rows=Rows(bars,bw,inp,stir.Dia);double d=h-inp.StrapCover-stir.Dia-Centroid(rows,bars.Dia);double asMin=Math.Max(.25*sq/inp.Fy,1.4/inp.Fy)*bw*d;double req=Math.Max(AsReq(Mu,bw,d,inp.Fc,inp.Fy),Mu>0?asMin:0);double prov=bars.Count>0?bars.Area:0;
    Add(list,steps,"Strap "+(face?"top bars (hogging)":"bottom bars (sagging)"),"409.5 / 409.6.1",id,face?negCase:posCase,req,prov,"mm²","strap moment varies linearly between the columns: M(column "+st.From+") = "+F(-factored.Max(c=>Math.Abs(c.MuA[s])))+" .. M(column "+st.To+"), plus wu x (L - x) / 2 (self weight)","Mu = "+F(Mu)+" kN·m at the column faces / within the clear span (L = "+F(L,"0.000")+" m)",(rows.Length>1?"bars in "+rows.Length+" layers ("+string.Join(" + ",rows)+"), Ø25 spacer between layers: d = h - cover - stirrup - centroid of the bars = "+F(h,"0")+" - "+F(inp.StrapCover,"0")+" - "+stir.Dia+" - "+F(Centroid(rows,bars.Dia),"0")+" = ":"d = "+F(h,"0")+" - "+F(inp.StrapCover,"0")+" - "+stir.Dia+" - "+bars.Dia+"/2 = ")+F(d,"0")+" mm;  As min = max(0.25√fc', 1.4) bw d / fy = "+F(asMin,"0")+" mm²","As req = "+F(req,"0")+" mm²;  provided "+(face?st.Top:st.Bottom)+" -> "+F(prov,"0")+" mm²");}
   {double d=h-inp.StrapCover-stir.Dia-top.Dia/2.0;double Vc=.17*sq*bw*d/1000;double legs=2;double Av=legs*Math.PI*stir.Dia*stir.Dia/4;double smax=Math.Min(d/2,600);double Vs=Av*inp.Fyt*d/stir.Spacing/1000;double phiVn=.75*(Vc+Math.Min(Vs,.66*sq*bw*d/1000));
    Add(list,steps,"Strap shear","422.5 / 409.7.6",id,vCase,vMax,phiVn,"kN","strap shear S = (Mi + Mj) / L plus wu L / 2 = "+F(vMax)+" kN","Vc = 0.17 √fc' bw d = "+F(Vc)+" kN;  Vs = Av fyt d / s = "+F(Av,"0")+" x "+F(inp.Fyt,"0")+" x "+F(d,"0")+" / "+F(stir.Spacing,"0")+" = "+F(Vs)+" kN (2 legs Ø"+stir.Dia+")","φVn = 0.75 (Vc + Vs) = "+F(phiVn)+" kN");
    bool heavyVs=Vs>.33*sq*bw*d/1000;double sLim=heavyVs?Math.Min(d/4,300):smax;Add(list,steps,"Strap stirrup spacing","409.7.6.2.2",id,"-",stir.Spacing,sLim,"mm",heavyVs?"Vs > 0.33 √fc' bw d: s <= min(d/4, 300) = "+F(sLim,"0")+" mm":"s <= min(d/2, 600) = "+F(sLim,"0")+" mm");
    double avMin=Math.Max(.062*sq*bw/inp.Fyt,.35*bw/inp.Fyt)*stir.Spacing;Add(list,steps,"Strap minimum stirrups","409.6.3.3",id,vCase,vMax>.5*.75*Vc?avMin:0,Av,"mm²","Av,min / s = max(0.062 √fc' bw / fyt, 0.35 bw / fyt) = "+F(avMin/stir.Spacing,"0.000")+" mm²/mm -> "+F(avMin,"0")+" mm² at s = "+F(stir.Spacing,"0")+(vMax>.5*.75*Vc?" (Vu > 0.5 φVc)":" (not required: Vu <= 0.5 φVc)"),"provided 2 legs Ø"+stir.Dia+" = "+F(Av,"0")+" mm²");}
   foreach(var bars in new[]{top,bot}){double inner=bw-2*inp.StrapCover-2*stir.Dia;int per=BeamCutting.PerLayer(inner,bars.Dia,inp.Aggregate);var rows=Rows(bars,bw,inp,stir.Dia);
    Add(list,steps,"Strap "+(bars==top?"top":"bottom")+" bar arrangement","425.2.1 / 425.2.2",id,"-",rows.Length,3,"layers","clear spacing >= max(25, db, 4/3 aggregate) = "+F(Math.Max(25,Math.Max(bars.Dia,4.0/3*inp.Aggregate)),"0")+" mm -> "+per+" bars of Ø"+bars.Dia+" per layer in "+F(inner,"0")+" mm",bars.Count+" bars in "+rows.Length+" layer"+(rows.Length>1?"s ("+string.Join(" + ",rows)+"), upper bars directly above the lower, Ø25 spacer bars between layers (25 mm clear)":" ("+bars.Count+")"),rows.Length>3?"more than three layers: widen or deepen the strap, or use bigger bars":"d of the flexure check uses the centroid of the layers");}
   if(h>900)notes.Add(id+": strap deeper than 900 mm - provide skin reinforcement on both side faces over h/2 (NSCP 409.7.2.3).");}
  SystemStability(inp,service,list,steps,notes);
  notes.Add("Straps are designed as beams clear of the soil: place them on a compressible fill or a void form so they do not bear on the ground.");
  notes.Add("Footing flexure uses the largest factored net pressure over the whole footing (conservative). Punching does not include the eccentric shear transfer of an unbalanced moment (422.6.3.6).");
  notes.Add("The strap moment is taken at the column faces with the footing reactions concentrated at the columns: conservative within the footing length.");
  return list;}
 /* overturning and sliding of each group of footings tied by straps (owner, 20 November 2026: "sa strap design include check in
    sliding overturning"): the group tips about the outer edge of its footings in +X, -X, +Z and -Z; resisting = the vertical
    loads (column loads, footing, soil and strap weights) times their distance to the edge; overturning = the horizontal
    reactions times the support height plus the column moments, toward that edge (service cases) */
 static void SystemStability(StrapInput inp,List<StrapCase> service,List<SupportCheck> list,Dictionary<SupportCheck,List<string>> steps,List<string> notes){if(service.Count==0)return;int n=inp.Nodes.Count;
  var group=new int[n];for(int k=0;k<n;k++)group[k]=k;Func<int,int> find=null;find=k=>group[k]==k?k:(group[k]=find(group[k]));foreach(var st in inp.Straps){int a=Index(inp,st.From),b=Index(inp,st.To);if(a>=0&&b>=0)group[find(a)]=find(b);}
  string q=inp.Vertical=="Z"?"Y":"Z";
  foreach(var members in Enumerable.Range(0,n).GroupBy(k=>find(k)).Select(gr=>gr.ToList())){var ids=members.Select(k=>"F"+inp.Nodes[k].Node).ToList();string id=members.Count>1?"Group "+string.Join("+",ids):ids[0];
   var straps=inp.Straps.Where(st=>members.Contains(Index(inp,st.From))).ToList();
   double xmin=members.Min(k=>inp.Nodes[k].X+inp.Nodes[k].Ex-inp.Nodes[k].B/2),xmax=members.Max(k=>inp.Nodes[k].X+inp.Nodes[k].Ex+inp.Nodes[k].B/2),zmin=members.Min(k=>inp.Nodes[k].Z+inp.Nodes[k].Ez-inp.Nodes[k].L/2),zmax=members.Max(k=>inp.Nodes[k].Z+inp.Nodes[k].Ez+inp.Nodes[k].L/2);
   SupportCheck worst=null;List<string> worstLines=null;double worstFs=double.PositiveInfinity;
   foreach(var c in service){
    /* vertical loads: column loads at the columns, footing + soil at the footing centres, strap weights at the strap middles */
    var loads=new List<double[]>();foreach(int k in members){var nd=inp.Nodes[k];loads.Add(new[]{nd.X,nd.Z,c.P[k]});loads.Add(new[]{nd.X+nd.Ex,nd.Z+nd.Ez,c.Wf[k]});}
    foreach(var st in straps){var a=inp.Nodes[Index(inp,st.From)];var b=inp.Nodes[Index(inp,st.To)];loads.Add(new[]{(a.X+b.X)/2,(a.Z+b.Z)/2,StrapWeight(inp,st)*Length(a,b)});}
    double V=loads.Sum(l=>l[2]);
    double mo1=members.Sum(k=>c.Mrq[k]-inp.SupportHeight*c.Hp[k]);/* toward +X */double mo2=members.Sum(k=>-c.Mrp[k]-inp.SupportHeight*c.Hq[k]);/* toward +plan q */
    foreach(var dir in new[]{new{Name="+X",Mo=mo1,Mr=loads.Sum(l=>l[2]*(xmax-l[0]))},new{Name="-X",Mo=-mo1,Mr=loads.Sum(l=>l[2]*(l[0]-xmin))},new{Name="+"+q,Mo=mo2,Mr=loads.Sum(l=>l[2]*(zmax-l[1]))},new{Name="-"+q,Mo=-mo2,Mr=loads.Sum(l=>l[2]*(l[1]-zmin))}}){
     if(dir.Mo<=1e-6)continue;double fs=dir.Mr/dir.Mo;if(fs<worstFs){worstFs=fs;worst=new SupportCheck{Name="Overturning (service), FS >= "+F(inp.OverturningFS,"0.0#"),Clause="Geotechnical (Mr / Mo)",Node=id,Case=c.Case,Demand=inp.OverturningFS*dir.Mo,Capacity=dir.Mr,Unit="kN·m",Ratio=dir.Mr<=0?double.PositiveInfinity:inp.OverturningFS*dir.Mo/dir.Mr};
      worstLines=new List<string>{"tipping toward "+dir.Name+" about the outer edge of "+(members.Count>1?"the strapped group ("+string.Join(", ",ids)+")":id),"overturning Mo = Σ (column moment + H x support height "+F(inp.SupportHeight,"0.00")+" m) = "+F(dir.Mo)+" kN·m","resisting Mr = Σ V x distance to the edge (column loads, footing + soil "+(straps.Count>0?"and strap ":"")+"weights, V = "+F(V)+" kN) = "+F(dir.Mr)+" kN·m","FS = Mr / Mo = "+F(fs,"0.00")+" >= "+F(inp.OverturningFS,"0.0#")};}}}
   if(worst!=null){list.Add(worst);steps[worst]=worstLines;}else notes.Add(id+": no overturning moment in the service cases (no horizontal reactions or column moments).");
   if(members.Count>1){var gs=service.Select(c=>new{C=c,H=Math.Sqrt(Math.Pow(members.Sum(k=>c.Hp[k]),2)+Math.Pow(members.Sum(k=>c.Hq[k]),2)),R=members.Sum(k=>Math.Max(0,c.R[k]))}).OrderByDescending(x=>x.R<=0?double.PositiveInfinity:x.H/x.R).First();if(gs.H>1e-9){double resist=inp.Friction*gs.R;
     Add(list,steps,"Sliding of the strapped group (service), FS >= "+F(inp.SlidingFS,"0.0#"),"Geotechnical (μ ΣR / ΣH)",id,gs.C.Case,inp.SlidingFS*gs.H,resist,"kN","ΣH = "+F(gs.H)+" kN (the straps tie the footings together)","μ ΣR = "+F(inp.Friction,"0.00")+" x "+F(gs.R)+" = "+F(resist)+" kN","FS = "+F(resist/gs.H,"0.00"));}}}}
 /* bars of one face in layers: as many per layer as the clear spacing allows (NSCP 425.2.1), the rest in the next layer */
 internal static int[] Rows(Bars bars,double bwMm,StrapInput inp,int stirrupDia){int n=Math.Max(0,bars.Count);if(n==0)return new int[0];int per=BeamCutting.PerLayer(bwMm-2*inp.StrapCover-2*stirrupDia,bars.Dia,inp.Aggregate);var list=new List<int>();while(n>0){int k=Math.Min(per,n);list.Add(k);n-=k;}return list.ToArray();}
 /* distance from the inner face of the stirrup to the centroid of the layers, 25 mm clear between layers */
 internal static double Centroid(int[] rows,int db){if(rows.Length==0)return db/2.0;double sum=0,n=0;for(int r=0;r<rows.Length;r++){sum+=rows[r]*(db/2.0+r*(db+25.0));n+=rows[r];}return sum/n;}
 /* sagging + moment in strap s at x from the From column */
 internal static double Moment(StrapCase c,int s,double L,double x){return -c.MuA[s]*(1-x/L)+c.MuB[s]*(x/L)+c.W[s]*x*(L-x)/2;}
}
}
