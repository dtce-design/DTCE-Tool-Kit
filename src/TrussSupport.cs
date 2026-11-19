using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Text;
namespace BeamScheduleFormatter {
/* Truss support on an RC column or beam (owner, 19 November 2026: "Design calculation sheet para sa support ng trusses sa rc column
   beam, angle bar 2L or 1L. Na pwede input yung design loads galing staad"). LRFD with factored STAAD support reactions:
   - bearing plate on concrete (NSCP 2015 Section 510.8 / AISC J8) and plate thickness for bearing and uplift (AISC Design Guide 1);
   - cast-in anchors per NSCP 2015 Section 417 (ACI 318-14 Chapter 17): steel tension and shear, concrete breakout in tension and
     shear, pullout, pryout, tension-shear interaction;
   - gusset-to-plate fillet welds; 1L / 2L angle welded to the gusset: balanced heel / toe welds, base metal, tension yielding and
     rupture with shear lag (AISC D2, D3), Whitmore section yielding / buckling of the gusset (AISC J4). */
class SupportReaction {public string Node="",Case="";public double Fx,Fy,Fz,Mx,My,Mz;}
class TrussSupportInput {
 public double PlateB,PlateN,PlateT,PlateFy=248,Fc=21,SupportB,SupportN,SupportDepth=400;
 public int BoltDia=20,BoltColumns=1;public double BoltS1=100,BoltS2=150,Hef=300,Futa=400,Fya=250,HookEh=80;public bool Headed=true,Cracked=true,GroutPad=true;
 public double GussetT=10,GussetL=200,GussetFy=248,GussetFu=400,PlateWeld=6,Fexx=482,Lc=150;public bool BearingCompression=true;
 public SteelProfile Angle;public int Angles=2;public double MemberForce=double.NaN,HeelWeld=100,ToeWeld=60,AngleWeld=6,AngleFy=248,AngleFu=400;
 public string Vertical="Y";public List<JointMember> Others=new List<JointMember>();
 /* the bottom chord sits on the plate: its horizontal legs are welded to the plate and the anchors are outside the angles (owner,
    19 November 2026: "Hindi ba yung anchor bolt naka install sa labas ng angle bar naka weld yung angle bar sa baseplate?") */
 public bool ChordOnPlate;public SteelProfile Chord;public int ChordAngles=2;public double ChordWeld=6,ChordWeldLength;
 internal double Footprint{get{return ChordOnPlate&&Chord!=null?(ChordAngles==2?2*Chord.B+GussetT:Chord.B):GussetT+2*PlateWeld;}}
}
/* other truss members welded to the same gusset - chord, vertical, diagonal (owner, 19 November 2026: "Yung kaya magdesign ng
   ganitong support", Detail 6: bottom chord, post and diagonal on a gusset over a base plate with 2-20 mm L anchor bolts each side) */
class JointMember {public string Name="Member";public int Angles=2;public SteelProfile Angle;public double Force,HeelWeld=100,ToeWeld=60,Weld=6;}
class SupportCheck {public string Name,Clause,Node="",Case="",Note="";public double Demand,Capacity,Ratio;public string Unit="kN";public bool Ok{get{return Ratio<=1+1e-9;}}}
static class TrussSupport {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 internal static readonly int[] Diameters={16,20,24,27,30,36};
 static readonly Dictionary<int,double> Ase=new Dictionary<int,double>{{16,157},{20,245},{24,353},{27,459},{30,561},{36,817}};
 static readonly Dictionary<int,double> NutFlats=new Dictionary<int,double>{{16,27},{20,34},{24,41},{27,46},{30,50},{36,60}};
 internal static double StressArea(int d){double a;if(!Ase.TryGetValue(d,out a))throw new Exception("Use an M16 to M36 anchor rod.");return a;}
 internal static double BearingArea(int d){double f=NutFlats[d];return Math.Sqrt(3)/2*f*f-Math.PI*d*d/4;}
 /* STAAD Support Reactions table: Node, L/C, Fx, Fy, Fz [, Mx, My, Mz]; the node may be blank on the rows that follow it */
 internal static List<SupportReaction> Reactions(string text,out string unitNote){var rows=StaadDisplacements.Rows(text);unitNote="kN (assumed)";int header=rows.FindIndex(r=>r.Any(c=>Has(c,"FX"))&&r.Any(c=>Has(c,"FY")));double factor=1;
  int node=0,lc=1,fx=2,fy=3,fz=4,mx=5,my=6,mz=7;int start=0;
  if(header>=0){var h=rows[header].Select(c=>c.ToUpperInvariant()).ToArray();var data=rows.Skip(header+1).FirstOrDefault(r=>r.Count(c=>StaadDisplacements.IsNumber(c.Trim()))>=3);string unitWords=string.Join(" ",h);if(data!=null&&h.Length>data.Length){var bare=h.Where(c=>!System.Text.RegularExpressions.Regex.IsMatch(c.Trim(),@"^(KN|KIP|KIPS|N|MTON|MT|KG|KN-M|KNM|KIP-FT|KIP-IN|N-MM|HORIZONTAL|VERTICAL|MOMENT|FORCE)$")).ToArray();if(bare.Length>=5)h=bare;}Func<string,int> col=k=>Array.FindIndex(h,c=>c.Replace(" ","").Contains(k));node=Math.Max(0,Array.FindIndex(h,c=>c.Contains("NODE")||c.Contains("JOINT")));lc=Array.FindIndex(h,c=>c.Contains("L/C")||c.Contains("LOAD")||c=="LC"||c.Contains("CASE"));fx=col("FX");fy=col("FY");fz=col("FZ");mx=col("MX");my=col("MY");mz=col("MZ");start=header+1;
   string all=unitWords;if(all.Contains("KIP")){factor=4.4482216;unitNote="kip (converted to kN)";}else if(Has(all,"MTON")||all.Contains("MT ")){factor=9.80665;unitNote="metric ton (converted to kN)";}else if(System.Text.RegularExpressions.Regex.IsMatch(all,@"\bN\b")&&!all.Contains("KN")){factor=.001;unitNote="N (converted to kN)";}else if(all.Contains("KN"))unitNote="kN";
   if(start<rows.Count&&rows[start].All(c=>c.Trim()==""||!StaadDisplacements.IsNumber(c.Trim())||c.ToUpperInvariant().Contains("KN"))&&!rows[start].Any(c=>StaadDisplacements.IsNumber(c.Trim())))start++;}
  if(fx<0||fy<0||fz<0)throw new Exception("The table needs Fx, Fy and Fz columns (STAAD Support Reactions).");
  var list=new List<SupportReaction>();string lastNode="";for(int r=start;r<rows.Count;r++){var p=rows[r];Func<int,string> cell=i=>i>=0&&i<p.Length?p[i].Trim():"";if(cell(fx)==""||!StaadDisplacements.IsNumber(cell(fx))||!StaadDisplacements.IsNumber(cell(fy))||!StaadDisplacements.IsNumber(cell(fz)))continue;
   string n=cell(node);if(n==""||!StaadDisplacements.IsNumber(n.Split(' ')[0]))n=lastNode;else lastNode=n;Func<int,double> num=i=>i>=0&&i<p.Length&&StaadDisplacements.IsNumber(p[i].Trim())?StaadDisplacements.Number(p[i])*factor:0;
   list.Add(new SupportReaction{Node=n,Case=lc>=0?cell(lc):"",Fx=num(fx),Fy=num(fy),Fz=num(fz),Mx=num(mx),My=num(my),Mz=num(mz)});}
  if(list.Count==0)throw new Exception("No reaction rows found. Copy the STAAD Support Reactions table with its headings, or type rows as: node, load case, Fx, Fy, Fz.");return list;}
 static bool Has(string cell,string key){return cell.ToUpperInvariant().Replace(" ","").Contains(key);}
 static double Vertical(SupportReaction r,string axis){return axis=="Z"?r.Fz:r.Fy;}
 static double Horizontal(SupportReaction r,string axis){return axis=="Z"?Math.Sqrt(r.Fx*r.Fx+r.Fy*r.Fy):Math.Sqrt(r.Fx*r.Fx+r.Fz*r.Fz);}
 internal static double[] AngleProperties(SteelProfile p){if(p==null||p.Kind!="Angle bar")throw new Exception("Choose an angle bar.");double a=(p.H+p.B-p.T)*p.T;double xBar=(p.B*p.T*p.B/2+(p.H-p.T)*p.T*p.T/2)/a;double yHeel=(p.H*p.T*p.H/2+(p.B-p.T)*p.T*p.T/2)/a;return new[]{a,xBar,yHeel};}
 public static List<SupportCheck> Check(TrussSupportInput i,IList<SupportReaction> reactions,List<string> notes){
  if(reactions==null||reactions.Count==0)throw new Exception("Enter the STAAD support reactions.");
  Func<double,bool> pos=v=>v>0&&!double.IsNaN(v)&&!double.IsInfinity(v);foreach(var v in new[]{i.PlateB,i.PlateN,i.PlateT,i.PlateFy,i.Fc,i.SupportB,i.SupportN,i.SupportDepth,i.BoltS2,i.Hef,i.Futa,i.Fya,i.GussetT,i.GussetL,i.GussetFy,i.GussetFu,i.PlateWeld,i.Fexx,i.AngleWeld,i.AngleFy,i.AngleFu})if(!pos(v))throw new Exception("Every dimension and strength must be greater than zero.");
  if(i.BoltColumns<1||i.BoltColumns>8)throw new Exception("Use 1 to 8 bolts on each side of the gusset.");if(i.BoltColumns>1&&!pos(i.BoltS1))throw new Exception("Enter the bolt spacing along the plate.");if(i.Angles!=1&&i.Angles!=2)throw new Exception("Choose 1L or 2L.");
  if(i.PlateB>i.SupportB+1e-9||i.PlateN>i.SupportN+1e-9)throw new Exception("The plate is larger than the top of the RC support.");if(i.ChordOnPlate){if(i.Chord==null||i.Chord.Kind!="Angle bar")throw new Exception("Choose the bottom chord angle section.");if(i.ChordAngles!=1&&i.ChordAngles!=2)throw new Exception("Choose 1L or 2L for the bottom chord.");double clear=i.BoltS2/2-i.Footprint/2-NutFlats[i.BoltDia]/2;if(clear<5)throw new Exception("The anchors are not clear of the chord: the "+i.BoltDia+" mm nuts reach the angle legs. Use an anchor gauge of at least "+F(i.Footprint+NutFlats[i.BoltDia]+10,"0")+" mm.");if(i.Footprint>=i.PlateB)throw new Exception("The chord is wider than the plate.");}
  else if(i.BoltS2<=i.GussetT+2*i.PlateWeld)throw new Exception("The bolt gauge across the plate must clear the gusset and its welds.");
  double futa=Math.Min(i.Futa,Math.Min(1.9*i.Fya,860));if(futa<i.Futa)notes.Add("futa limited to min(1.9 fya, 860 MPa) = "+F(futa)+" MPa (417.4.1.2).");
  int n=2*i.BoltColumns;double ase=StressArea(i.BoltDia),da=i.BoltDia,fc=i.Fc,sq=Math.Sqrt(fc),run=(i.BoltColumns-1)*i.BoltS1;
  double cB=(i.SupportB-i.BoltS2)/2,cN=(i.SupportN-run)/2;if(cB<=0||cN<=0)throw new Exception("The bolts do not fit on the RC support.");if(Math.Min(cB,cN)<6*da)notes.Add("Edge distance "+F(Math.Min(cB,cN))+" mm is less than 6da = "+F(6*da)+" mm (417.7): check that the anchors are torqued only lightly or provide larger edges.");
  if(i.Hef>i.SupportDepth-50)notes.Add("hef is close to the support depth; confirm the anchor fits inside the RC member.");
  if(i.ChordOnPlate)notes.Add("Bottom chord "+(i.ChordAngles==2?"2L ":"1L ")+i.Chord.Name+" on the plate (footprint "+F(i.Footprint,"0")+" mm across): its horizontal legs are welded to the plate and the anchors are outside the angles; the gusset sits between the vertical legs and is welded to them (checked with the truss connection, not here).");
  /* tension: steel, concrete breakout of the group, pullout */
  double phiNsa=.75*ase*futa/1000*n;
  double h15=1.5*i.Hef,ANc=(Math.Min(cB,h15)*2+i.BoltS2)*(Math.Min(cN,h15)*2+run),ANco=9*i.Hef*i.Hef,camin=Math.Min(cB,cN);double psiEdN=camin>=h15?1:.7+.3*camin/h15,psiCN=i.Cracked?1:1.25;
  double Nb=10*sq*Math.Pow(i.Hef,1.5)/1000;if(i.Headed&&i.Hef>=280&&i.Hef<=635)Nb=Math.Min(Nb,3.9*sq*Math.Pow(i.Hef,5.0/3)/1000);double phiNcbg=.70*Math.Min(ANc/ANco,n)*psiEdN*psiCN*Nb;
  double Np=i.Headed?8*BearingArea(i.BoltDia)*fc/1000:.9*fc*Math.Max(3*da,Math.Min(4.5*da,i.HookEh))*da/1000;double phiNpn=.70*(i.Cracked?1:1.4)*Np*n;
  /* shear: steel, concrete breakout toward the nearest edge (front bolts), pryout */
  double phiVsa=.65*.6*ase*futa*(i.GroutPad?.8:1)/1000*n;double ca1=camin,ca2=Math.Max(cB,cN),across=ca1==cB?run:i.BoltS2;double le=Math.Min(i.Hef,8*da);
  double Vb=Math.Min(.6*Math.Pow(le/da,.2)*Math.Sqrt(da)*sq*Math.Pow(ca1,1.5),3.7*sq*Math.Pow(ca1,1.5))/1000;double AVc=(Math.Min(ca2,1.5*ca1)*2+across)*Math.Min(1.5*ca1,i.SupportDepth),AVco=4.5*ca1*ca1;double psiEdV=ca2>=1.5*ca1?1:.7+.3*ca2/(1.5*ca1),psiCV=i.Cracked?1:1.4,psiH=Math.Max(1,Math.Sqrt(1.5*ca1/i.SupportDepth));
  double front=ca1==cB?i.BoltColumns:2;double phiVcbg=.70*Math.Min(AVc/AVco,front)*psiEdV*psiCV*psiH*Vb;double phiVcpg=.70*(i.Hef<65?1:2)*phiNcbg/.70;
  notes.Add("Anchors: "+n+" x M"+i.BoltDia+" ("+(i.Headed?"headed":"hooked J/L")+"), Ase = "+F(ase)+" mm2, hef = "+F(i.Hef)+" mm, gauge "+F(i.BoltS2)+" mm"+(i.BoltColumns>1?", pitch "+F(i.BoltS1)+" mm":"")+"; edges cB = "+F(cB)+" mm, cN = "+F(cN)+" mm; "+(i.Cracked?"cracked":"uncracked")+" concrete, condition B (no supplementary reinforcement).");
  notes.Add("Shear breakout is taken toward the nearest edge (ca1 = "+F(ca1)+" mm) whatever the reaction direction - conservative. Side-face blowout applies only to headed anchors with ca1 < 0.4 hef: "+(i.Headed&&ca1<.4*i.Hef?"CHECK SEPARATELY":"not governing")+".");
  /* bearing plate */
  double A1=i.PlateB*i.PlateN,k=Math.Min(2,Math.Min(i.SupportB/i.PlateB,i.SupportN/i.PlateN));double phiPp=.65*.85*fc*A1*k/1000;
  /* angle member and gusset */
  var ap=AngleProperties(i.Angle);double ag=ap[0],xbar=ap[1],yHeel=ap[2],legH=i.Angle.H,t=i.Angle.T;double phiRwA=.75*.6*i.Fexx*.707*i.AngleWeld/1000;/* kN per mm */double lw=(i.HeelWeld+i.ToeWeld)/2;
  double U=Math.Max(0,1-xbar/Math.Max(1,lw));double phiTy=.9*i.AngleFy*ag*i.Angles/1000,phiTr=.75*i.AngleFu*U*ag*i.Angles/1000;
  double weldCap=phiRwA*(i.HeelWeld+i.ToeWeld)*i.Angles,baseAngle=.75*.6*i.AngleFu*t*(i.HeelWeld+i.ToeWeld)*i.Angles/1000,baseGusset=.75*.6*i.GussetFu*i.GussetT*(i.HeelWeld+i.ToeWeld)*(i.Angles==2?1:1)/1000;
  double whitmore=Math.Min(legH+2*Math.Max(i.HeelWeld,i.ToeWeld)*Math.Tan(Math.PI/6),i.GussetL);double phiWy=.9*i.GussetFy*whitmore*i.GussetT/1000;double r=i.GussetT/Math.Sqrt(12),kl=.65*i.Lc/r,Fe=Math.PI*Math.PI*200000/(kl*kl),Fcr=kl<=4.71*Math.Sqrt(200000/i.GussetFy)?Math.Pow(.658,i.GussetFy/Fe)*i.GussetFy:.877*Fe;double phiWc=.9*Fcr*whitmore*i.GussetT/1000;
  double phiRwP=.75*.6*i.Fexx*.707*i.PlateWeld/1000*2;/* both sides, kN per mm of gusset */double weldOnPlate=Math.Min(i.GussetL,i.PlateN);/* a gusset longer than the plate is welded only over the plate */double gussetShear=1.0*.6*i.GussetFy*i.GussetT*weldOnPlate/1000;if(i.GussetL>i.PlateN+1e-9)notes.Add("The gusset ("+F(i.GussetL,"0")+" mm) is longer than the plate ("+F(i.PlateN,"0")+" mm): it is welded to the plate over "+F(weldOnPlate,"0")+" mm.");
  notes.Add("Angle "+i.Angle.Name+" ("+(i.Angles==2?"2L back to back":"1L")+"): Ag = "+F(ag)+" mm2 each, x̄ = "+F(xbar)+" mm, heel-to-centroid "+F(yHeel)+" mm on the "+F(legH)+" mm connected leg; U = 1 - x̄/l = "+F(U,"0.000")+" (AISC Table D3.1 case 4 with l = mean weld length "+F(lw)+" mm).");
  notes.Add("Gusset: Whitmore width "+F(whitmore)+" mm; compression KL/r = 0.65 x "+F(i.Lc)+" / "+F(r)+" = "+F(kl,"0.0")+", Fcr = "+F(Fcr)+" MPa.");
  /* each reaction row; the governing row is kept for every check */
  var best=new Dictionary<string,SupportCheck>();Action<string,string,SupportReaction,double,double,string,string> keep=(name,clause,rx,demand,capacity,unit,note)=>{double ratio=capacity<=0?double.PositiveInfinity:demand/capacity;SupportCheck c;if(!best.TryGetValue(name,out c)||ratio>c.Ratio)best[name]=new SupportCheck{Name=name,Clause=clause,Node=rx==null?"":rx.Node,Case=rx==null?"":rx.Case,Demand=demand,Capacity=capacity,Ratio=ratio,Unit=unit,Note=note};};
  foreach(var rx in reactions){double v=Vertical(rx,i.Vertical),h=Horizontal(rx,i.Vertical);double pu=Math.Max(0,v),tu=Math.Max(0,-v);
   if(Math.Abs(rx.Mx)+Math.Abs(rx.My)+Math.Abs(rx.Mz)>1e-6&&!notes.Any(z=>z.StartsWith("Support moments")))notes.Add("Support moments are in the reactions; this sheet treats the support as pinned (moments not designed for).");
   keep("Concrete bearing","510.8 / AISC J8",rx,pu,phiPp,"kN","φc 0.85 fc' A1 √(A2/A1), √(A2/A1) = "+F(k,"0.00"));
   double l=(i.PlateB-i.Footprint)/2;double treqB=pu<=0?0:l*Math.Sqrt(2*pu*1000/(.9*i.PlateFy*A1));keep("Plate thickness - bearing","AISC DG1",rx,treqB,i.PlateT,"mm","t req = l √(2Pu / (0.9 Fy B N)), l = "+F(l)+" mm");
   double x=i.BoltS2/2-i.Footprint/2;double beff=Math.Min(i.PlateN,2*x*i.BoltColumns);double treqU=tu<=0?0:Math.Sqrt(4*(tu*1000/2)*x/(.9*i.PlateFy*beff));keep("Plate thickness - uplift","AISC DG1",rx,treqU,i.PlateT,"mm","t req = √(4 (Tu/2) x / (0.9 Fy beff)), x = "+F(x)+" mm, beff = "+F(beff)+" mm");
   keep("Anchor steel - tension","417.4.1",rx,tu,phiNsa,"kN","φ 0.75 x n Ase futa");keep("Concrete breakout - tension","417.4.2",rx,tu,phiNcbg,"kN","φ 0.70 (ANc/ANco) ψed,N ψc,N Nb; ANc = "+F(ANc,"0")+" mm2, ψed,N = "+F(psiEdN,"0.00"));keep("Anchor pullout","417.4.3",rx,tu,phiNpn,"kN",i.Headed?"φ 0.70 x n x 8 Abrg fc'":"φ 0.70 x n x 0.9 fc' eh da");
   keep("Anchor steel - shear","417.5.1",rx,h,phiVsa,"kN","φ 0.65 x n 0.6 Ase futa"+(i.GroutPad?" x 0.8 (grout pad)":""));keep("Concrete breakout - shear","417.5.2",rx,h,phiVcbg,"kN","φ 0.70 (AVc/AVco) ψed,V ψc,V ψh,V Vb; ca1 = "+F(ca1)+" mm");keep("Concrete pryout","417.5.3",rx,h,phiVcpg,"kN","φ 0.70 kcp Ncbg");
   double phiN=Math.Min(phiNsa,Math.Min(phiNcbg,phiNpn)),phiV=Math.Min(phiVsa,Math.Min(phiVcbg,phiVcpg));double nr=tu/phiN,vr=h/phiV;double inter=nr<=.2?vr:vr<=.2?nr:(nr+vr)/1.2;keep("Tension-shear interaction","417.6",rx,inter,1,"ratio",nr<=.2||vr<=.2?"one ratio ≤ 0.2: the other governs":"(Nua/φNn + Vua/φVn) / 1.2");
   if(i.ChordOnPlate){double lw2=i.ChordWeldLength>0?Math.Min(i.ChordWeldLength,i.PlateN):i.PlateN,rwc=.75*.6*i.Fexx*.707*i.ChordWeld/1000;double cflow=Math.Sqrt(h*h+(i.BearingCompression?tu*tu:v*v))/(2*lw2);keep("Chord-to-plate welds","AISC J2.4",rx,cflow,rwc,"kN/mm","two fillet welds of "+F(i.ChordWeld,"0")+" mm x "+F(lw2,"0")+" mm along the chord legs; "+(i.BearingCompression?"compression by bearing":"all forces in the welds"));
    if(tu>0){double arm=Math.Max(1,i.Chord.B-i.Chord.T),mu=tu*1000/i.ChordAngles*arm;double treqLeg=Math.Sqrt(4*mu/(.9*i.AngleFy*lw2));keep("Chord leg bending - uplift","AISC F11",rx,treqLeg,i.Chord.T,"mm","uplift "+F(tu/i.ChordAngles,"0.0")+" kN per angle over "+F(arm,"0")+" mm from the vertical leg to the toe weld");}}
   else{double flow=Math.Sqrt(h*h+(i.BearingCompression?tu*tu:v*v))/(2*weldOnPlate);keep("Gusset-to-plate welds","AISC J2.4",rx,flow,phiRwP/2,"kN/mm","resultant per weld per mm; "+(i.BearingCompression?"compression by bearing":"all forces in the welds"));keep("Gusset shear yielding","AISC J4.2",rx,h,gussetShear,"kN","1.0 x 0.6 Fy tg Lg");}
   double P=double.IsNaN(i.MemberForce)?Math.Sqrt(v*v+h*h):i.MemberForce;bool tension=!double.IsNaN(i.MemberForce)?i.MemberForce>=0:tu>0;double pa=Math.Abs(P);
   keep("Angle welds (heel + toe)","AISC J2.4",rx,pa,weldCap,"kN","φ 0.6 FEXX 0.707 w (Lh + Lt) x "+i.Angles);keep("Angle base metal at welds","AISC J4.2",rx,pa,baseAngle,"kN","φ 0.6 Fu t (Lh + Lt) x "+i.Angles);keep("Gusset base metal at welds","AISC J4.2",rx,pa,baseGusset,"kN","φ 0.6 Fu tg (Lh + Lt)");
   if(tension){keep("Angle tension yielding","AISC D2",rx,pa,phiTy,"kN","φ 0.9 Fy Ag");keep("Angle tension rupture","AISC D2/D3",rx,pa,phiTr,"kN","φ 0.75 Fu U Ag");keep("Gusset Whitmore yielding","AISC J4.1",rx,pa,phiWy,"kN","φ 0.9 Fy Lw tg");}else keep("Gusset Whitmore buckling","AISC J4.4",rx,pa,phiWc,"kN","φ 0.9 Fcr Lw tg");}
  /* balanced welds: what each weld needs */
  double paMax=reactions.Max(rx=>double.IsNaN(i.MemberForce)?Math.Sqrt(Math.Pow(Vertical(rx,i.Vertical),2)+Math.Pow(Horizontal(rx,i.Vertical),2)):Math.Abs(i.MemberForce))/i.Angles;notes.Add("Balanced welds for "+F(paMax)+" kN per angle: heel "+F(paMax*(legH-yHeel)/legH/phiRwA,"0")+" mm, toe "+F(paMax*yHeel/legH/phiRwA,"0")+" mm (provided "+F(i.HeelWeld,"0")+" / "+F(i.ToeWeld,"0")+" mm).");
  if(double.IsNaN(i.MemberForce))notes.Add("No member force entered: the angle and gusset checks use the support reaction resultant (√(V² + H²)) of each row, taken as tension when the row is uplift.");
  string[] order={"Concrete bearing","Plate thickness - bearing","Plate thickness - uplift","Anchor steel - tension","Concrete breakout - tension","Anchor pullout","Anchor steel - shear","Concrete breakout - shear","Concrete pryout","Tension-shear interaction","Gusset-to-plate welds","Gusset shear yielding","Chord-to-plate welds","Chord leg bending - uplift","Angle welds (heel + toe)","Angle base metal at welds","Gusset base metal at welds","Angle tension yielding","Angle tension rupture","Gusset Whitmore yielding","Gusset Whitmore buckling"};
  var list=order.Where(best.ContainsKey).Select(k2=>best[k2]).ToList();
  /* the other members at the joint, each with its own STAAD force (+ tension, - compression) */
  foreach(var m in i.Others??new List<JointMember>()){if(m==null||m.Angle==null)continue;if(m.Angles!=1&&m.Angles!=2)throw new Exception(m.Name+": choose 1L or 2L.");if(!(m.HeelWeld>0)||m.ToeWeld<0||!(m.Weld>0))throw new Exception(m.Name+": enter the weld lengths and size.");
   var mp=AngleProperties(m.Angle);double rw=.75*.6*i.Fexx*.707*m.Weld/1000,ml=(m.HeelWeld+m.ToeWeld)/2,mu=Math.Max(0,1-mp[1]/Math.Max(1,ml)),force=Math.Abs(m.Force);bool mt=m.Force>=0;
   Func<string,string,double,double,string,SupportCheck> add=(name,clause,demand,capacity,note)=>new SupportCheck{Name=m.Name+": "+name,Clause=clause,Node="-",Case=(mt?"+":"")+F(m.Force,"0.0")+" kN",Demand=demand,Capacity=capacity,Ratio=capacity<=0?double.PositiveInfinity:demand/capacity,Unit="kN",Note=note};
   list.Add(add("angle welds","AISC J2.4",force,rw*(m.HeelWeld+m.ToeWeld)*m.Angles,"φ 0.6 FEXX 0.707 w (Lh + Lt) x "+m.Angles+"; balanced: heel "+F(force/m.Angles*(m.Angle.H-mp[2])/m.Angle.H/rw,"0")+" mm, toe "+F(force/m.Angles*mp[2]/m.Angle.H/rw,"0")+" mm"));
   list.Add(add("angle base metal","AISC J4.2",force,.75*.6*i.AngleFu*m.Angle.T*(m.HeelWeld+m.ToeWeld)*m.Angles/1000,"φ 0.6 Fu t (Lh + Lt)"));list.Add(add("gusset base metal","AISC J4.2",force,.75*.6*i.GussetFu*i.GussetT*(m.HeelWeld+m.ToeWeld)/1000,"φ 0.6 Fu tg (Lh + Lt)"));
   double lwm=Math.Min(m.Angle.H+2*Math.Max(m.HeelWeld,m.ToeWeld)*Math.Tan(Math.PI/6),i.GussetL);
   if(mt){list.Add(add("tension yielding","AISC D2",force,.9*i.AngleFy*mp[0]*m.Angles/1000,"φ 0.9 Fy Ag"));list.Add(add("tension rupture","AISC D2/D3",force,.75*i.AngleFu*mu*mp[0]*m.Angles/1000,"φ 0.75 Fu U Ag, U = "+F(mu,"0.000")));list.Add(add("Whitmore yielding","AISC J4.1",force,.9*i.GussetFy*lwm*i.GussetT/1000,"Lw = "+F(lwm,"0")+" mm"));}
   else list.Add(add("Whitmore buckling","AISC J4.4",force,.9*Fcr*lwm*i.GussetT/1000,"Lw = "+F(lwm,"0")+" mm, Fcr = "+F(Fcr,"0")+" MPa"));}
  return list;}
 internal static string F(double v,string format="0.0"){return v.ToString(format,Inv);}
}
}
