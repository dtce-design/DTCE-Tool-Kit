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
 public double Fc=21,Fy=415,Fyt=275,Cover=75,StrapCover=40,Qa=150,Df=1.5,GammaSoil=18,GammaConc=24,SupportHeight=1.0;public string Vertical="Y";
 public Func<string,bool> Factored=StrapFooting.LooksFactored;}
class StrapCase {public string Case;public bool Factored;public double[] P,R,Mp,Mq,Qmax,Qmin,Qavg;public double[] MuA,MuB,Shear,W;}
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
 internal static StrapCase Solve(StrapInput inp,string name,Dictionary<string,SupportReaction> rows,bool factored){int n=inp.Nodes.Count,m=inp.Straps.Count,N=n+2*m;var A=new double[N,N];var rhs=new double[N];var load=new Load[n];var W=new double[n];var res=new StrapCase{Case=name,Factored=factored,P=new double[n],R=new double[n],Mp=new double[n],Mq=new double[n],Qmax=new double[n],Qmin=new double[n],Qavg=new double[n],MuA=new double[m],MuB=new double[m],Shear=new double[m],W=new double[m]};
  for(int k=0;k<n;k++){SupportReaction r;if(!rows.TryGetValue(inp.Nodes[k].Node,out r))throw new Exception("Load case "+name+" has no reaction at node "+inp.Nodes[k].Node+".");load[k]=LoadOf(r,inp.Vertical);res.P[k]=load[k].P;W[k]=factored?0:FootingWeight(inp,inp.Nodes[k]);}
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
   if(factored.Count==0)continue;
   var gf=factored.OrderByDescending(c=>c.Qmax[k]).First();double qu=gf.Qmax[k];var bars=ParseBars(nd.Bars);double T=nd.T*1000,d=T-inp.Cover-bars.Dia;if(d<=0){notes.Add(id+": footing too thin for the cover and bars.");continue;}
   foreach(int dir in new[]{0,1}){double B=dir==0?nd.B:nd.L,Lw=dir==0?nd.L:nd.B,e=dir==0?nd.Ex:nd.Ez,c=dir==0?nd.Cx:nd.Cz;double l1=B/2-e-c/2,l2=B/2+e-c/2,lc=Math.Max(l1,l2);string ax=dir==0?"X":(inp.Vertical=="Z"?"Y":"Z");
    double Mu=qu*lc*lc/2*Lw;double asReq=Math.Max(AsReq(Mu,Lw*1000,d,inp.Fc,inp.Fy),(inp.Fy>=400?.0018:.002)*Lw*1000*T);double asProv=bars.Area*Lw;
    Add(list,steps,"Footing flexure, bars along "+ax,"413.2.6.4 / 422.2",id,gf.Case,asReq,asProv,"mm²","cantilever from the column face: l = max("+F(l1,"0.000")+", "+F(l2,"0.000")+") = "+F(lc,"0.000")+" m (column off-centre by "+F(e,"0.000")+" m)","Mu = qu l² / 2 x width = "+F(qu)+" x "+F(lc,"0.000")+"² / 2 x "+F(Lw,"0.00")+" = "+F(Mu)+" kN·m (qu = max factored net pressure)","d = h - cover - db = "+F(T,"0")+" - "+F(inp.Cover,"0")+" - "+bars.Dia+" = "+F(d,"0")+" mm","As req = max(flexure, "+(inp.Fy>=400?"0.0018":"0.0020")+" b h) = "+F(asReq,"0")+" mm²;  provided Ø"+bars.Dia+" @ "+F(bars.Spacing,"0")+" -> "+F(asProv,"0")+" mm²");
    double Vu=qu*Math.Max(0,lc-d/1000)*Lw,phiVc=.75*.17*sq*Lw*1000*d/1000;Add(list,steps,"Footing one-way shear, along "+ax,"422.5.5.1",id,gf.Case,Vu,phiVc,"kN","Vu = qu (l - d) width = "+F(qu)+" x ("+F(lc,"0.000")+" - "+F(d/1000,"0.000")+") x "+F(Lw,"0.00")+" = "+F(Vu)+" kN","φVc = 0.75 x 0.17 √fc' b d = 0.75 x 0.17 x "+F(sq,"0.00")+" x "+F(Lw*1000,"0")+" x "+F(d,"0")+" / 1000 = "+F(phiVc)+" kN");}
   {double dd=d/1000;double x1=-nd.Ex-(nd.Cx+dd)/2,x2=-nd.Ex+(nd.Cx+dd)/2,z1=-nd.Ez-(nd.Cz+dd)/2,z2=-nd.Ez+(nd.Cz+dd)/2;int sides=0;double b0=0;
    if(x1>-nd.B/2){sides++;}else x1=-nd.B/2;if(x2<nd.B/2){sides++;}else x2=nd.B/2;if(z1>-nd.L/2){sides++;}else z1=-nd.L/2;if(z2<nd.L/2){sides++;}else z2=nd.L/2;
    if(-nd.Ex-(nd.Cx+dd)/2>-nd.B/2)b0+=z2-z1;if(-nd.Ex+(nd.Cx+dd)/2<nd.B/2)b0+=z2-z1;if(-nd.Ez-(nd.Cz+dd)/2>-nd.L/2)b0+=x2-x1;if(-nd.Ez+(nd.Cz+dd)/2<nd.L/2)b0+=x2-x1;
    double alpha=sides>=4?40:sides==3?30:20;double beta=Math.Max(nd.Cx,nd.Cz)/Math.Min(nd.Cx,nd.Cz);double vc=Math.Min(.33*sq,Math.Min(.17*(1+2/beta)*sq,.083*(alpha*d/(b0*1000)+2)*sq));double phiVc=.75*vc*b0*1000*d/1000;
    double Ru=gf.R[k],Vu=Math.Max(0,Ru*(1-(x2-x1)*(z2-z1)/area));
    Add(list,steps,"Footing punching shear (two-way)","422.6.5",id,gf.Case,Vu,phiVc,"kN","critical section d/2 from the column: "+(sides>=4?"interior":sides==3?"edge":"corner")+" column, b0 = "+F(b0*1000,"0")+" mm, αs = "+F(alpha,"0"),"Vu = Ru (1 - A0 / A) = "+F(Ru)+" x (1 - "+F((x2-x1)*(z2-z1),"0.000")+" / "+F(area,"0.000")+") = "+F(Vu)+" kN","vc = min(0.33, 0.17(1 + 2/β), 0.083(αs d / b0 + 2)) √fc' = "+F(vc,"0.00")+" MPa;  φVc = 0.75 vc b0 d = "+F(phiVc)+" kN");}
   foreach(var c in cases)if(c.R[k]<0){notes.Add(id+": the soil reaction is negative in load case "+c.Case+" ("+F(c.R[k])+" kN) - the strap lifts this footing; add weight or change the layout.");break;}}
  for(int s=0;s<inp.Straps.Count;s++){var st=inp.Straps[s];var a=inp.Nodes[Index(inp,st.From)];var b=inp.Nodes[Index(inp,st.To)];double L=Length(a,b);string id="S"+st.From+"-"+st.To;if(factored.Count==0)continue;
   double ux=(b.X-a.X)/L,uz=(b.Z-a.Z)/L;double ha=(Math.Abs(ux)*a.Cx+Math.Abs(uz)*a.Cz)/2,hb=(Math.Abs(ux)*b.Cx+Math.Abs(uz)*b.Cz)/2;
   double posMax=0,negMax=0,vMax=0;string posCase="-",negCase="-",vCase="-";
   foreach(var c in factored){for(int k=0;k<=40;k++){double x=ha+(L-ha-hb)*k/40.0;double M=Moment(c,s,L,x);if(M>posMax){posMax=M;posCase=c.Case;}if(-M>negMax){negMax=-M;negCase=c.Case;}}double v=Math.Abs(c.Shear[s])+c.W[s]*L/2;if(v>vMax){vMax=v;vCase=c.Case;}}
   var top=ParseBars(st.Top);var bot=ParseBars(st.Bottom);var stir=ParseBars(st.Stirrups);double bw=st.Width*1000,h=st.Depth*1000;
   foreach(var face in new[]{true,false}){var bars=face?top:bot;double Mu=face?negMax:posMax;double d=h-inp.StrapCover-stir.Dia-bars.Dia/2.0;double asMin=Math.Max(.25*sq/inp.Fy,1.4/inp.Fy)*bw*d;double req=Math.Max(AsReq(Mu,bw,d,inp.Fc,inp.Fy),Mu>0?asMin:0);double prov=bars.Count>0?bars.Area:0;
    Add(list,steps,"Strap "+(face?"top bars (hogging)":"bottom bars (sagging)"),"409.5 / 409.6.1",id,face?negCase:posCase,req,prov,"mm²","strap moment varies linearly between the columns: M(column "+st.From+") = "+F(-factored.Max(c=>Math.Abs(c.MuA[s])))+" .. M(column "+st.To+"), plus wu x (L - x) / 2 (self weight)","Mu = "+F(Mu)+" kN·m at the column faces / within the clear span (L = "+F(L,"0.000")+" m)","d = "+F(h,"0")+" - "+F(inp.StrapCover,"0")+" - "+stir.Dia+" - "+bars.Dia+"/2 = "+F(d,"0")+" mm;  As min = max(0.25√fc', 1.4) bw d / fy = "+F(asMin,"0")+" mm²","As req = "+F(req,"0")+" mm²;  provided "+(face?st.Top:st.Bottom)+" -> "+F(prov,"0")+" mm²");}
   {double d=h-inp.StrapCover-stir.Dia-top.Dia/2.0;double Vc=.17*sq*bw*d/1000;double legs=2;double Av=legs*Math.PI*stir.Dia*stir.Dia/4;double smax=Math.Min(d/2,600);double Vs=Av*inp.Fyt*d/stir.Spacing/1000;double phiVn=.75*(Vc+Math.Min(Vs,.66*sq*bw*d/1000));
    Add(list,steps,"Strap shear","422.5 / 409.7.6",id,vCase,vMax,phiVn,"kN","strap shear S = (Mi + Mj) / L plus wu L / 2 = "+F(vMax)+" kN","Vc = 0.17 √fc' bw d = "+F(Vc)+" kN;  Vs = Av fyt d / s = "+F(Av,"0")+" x "+F(inp.Fyt,"0")+" x "+F(d,"0")+" / "+F(stir.Spacing,"0")+" = "+F(Vs)+" kN (2 legs Ø"+stir.Dia+")","φVn = 0.75 (Vc + Vs) = "+F(phiVn)+" kN");
    Add(list,steps,"Strap stirrup spacing","409.7.6.2.2",id,"-",stir.Spacing,smax,"mm","s <= min(d/2, 600) = "+F(smax,"0")+" mm");}}
  notes.Add("Straps are designed as beams clear of the soil: place them on a compressible fill or a void form so they do not bear on the ground.");
  notes.Add("Footing flexure uses the largest factored net pressure over the whole footing (conservative). Punching does not include the eccentric shear transfer of an unbalanced moment (422.6.3.6).");
  notes.Add("The strap moment is taken at the column faces with the footing reactions concentrated at the columns: conservative within the footing length.");
  return list;}
 /* sagging + moment in strap s at x from the From column */
 internal static double Moment(StrapCase c,int s,double L,double x){return -c.MuA[s]*(1-x/L)+c.MuB[s]*(x/L)+c.W[s]*x*(L-x)/2;}
}
}
