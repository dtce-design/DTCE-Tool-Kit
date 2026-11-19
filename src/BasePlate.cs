using System;using System.Collections.Generic;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
/* Column base plate and anchor bolts (owner, 19 November 2026: "Design ng baseplate tapos anchor bolt"). LRFD with factored STAAD
   support reactions: AISC Design Guide 1 (2nd ed.) for bearing, small / large moment and plate thickness; NSCP 2015 Section 417
   (ACI 318-14 Ch. 17) for the cast-in anchors. The moment is taken about the plate's B axis (bending along N). */
class BasePlateInput {
 public string ColumnType="I / W section";public double ColumnD=300,ColumnBf=200,ColumnTf=12;
 public double B=400,N=450,Tp=25,Fy=248,Fc=21,PedestalB=600,PedestalN=650,PedestalDepth=600;
 public double BoltDia=24;public int BoltsPerSide=2;public string BoltName="";public double ThreadLength;public double EdgeN=50,Gauge=250,Hef=400,Futa=400,Fya=250,HookEh=100;public bool Headed=true,Cracked=true,Grout=true,Friction=false;
 public string Vertical="Y",MomentAxis="Mz";
}
class BasePlateRow {public SupportReaction Reaction;public double Pu,Vu,Mu,E,Ecrit,Y,Tu,Fp,TpBearing,TpTension;public bool LargeMoment,Uplift;}
static class BasePlate {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 static string F(double v,string f="0.0"){return v.ToString(f,Inv);}
 internal static double[] Cantilevers(BasePlateInput i){double m,n,lp;string t=i.ColumnType;
  if(t.StartsWith("Pipe")){m=n=(i.N-.8*i.ColumnD)/2;lp=0;}
  else if(t.StartsWith("HSS")){m=(i.N-.95*i.ColumnD)/2;n=(i.B-.95*i.ColumnBf)/2;lp=0;}
  else{m=(i.N-.95*i.ColumnD)/2;n=(i.B-.8*i.ColumnBf)/2;lp=Math.Sqrt(i.ColumnD*i.ColumnBf)/4;}
  if(m<=0||n<0)throw new Exception("The plate must be larger than the column.");return new[]{m,n,lp};}
 public static List<SupportCheck> Check(BasePlateInput i,IList<SupportReaction> reactions,List<string> notes,List<BasePlateRow> rows){
  if(reactions==null||reactions.Count==0)throw new Exception("Enter the STAAD support reactions.");
  foreach(var v in new[]{i.ColumnD,i.ColumnBf,i.B,i.N,i.Tp,i.Fy,i.Fc,i.PedestalB,i.PedestalN,i.PedestalDepth,i.EdgeN,i.Gauge,i.Hef,i.Futa,i.Fya})if(!(v>0))throw new Exception("Every dimension and strength must be greater than zero.");
  if(i.B>i.PedestalB+1e-9||i.N>i.PedestalN+1e-9)throw new Exception("The plate is larger than the pedestal.");if(i.BoltsPerSide<1||i.BoltsPerSide>4)throw new Exception("Use 1 to 4 anchors on each side.");if(i.EdgeN*2>=i.N)throw new Exception("The anchor edge distance is too large for the plate length.");if(i.BoltsPerSide>1&&i.Gauge>=i.B)throw new Exception("The anchor gauge must fit inside the plate width.");
  var c=Cantilevers(i);double m=c[0],n=c[1],lp=c[2];double A1=i.B*i.N,ratio=Math.Min(2,Math.Min(i.PedestalB/i.B,i.PedestalN/i.N));double fpMax=.65*.85*i.Fc*ratio;/* MPa */double qMax=fpMax*i.B;/* N/mm */
  double f=i.N/2-i.EdgeN;/* plate centre to the tension anchors */int nt=i.BoltsPerSide,nAll=2*i.BoltsPerSide;double ase=TrussSupport.StressArea(i.BoltDia),da=i.BoltDia,futa=Math.Min(i.Futa,Math.Min(1.9*i.Fya,860));double sq=Math.Sqrt(i.Fc);
  double across=nt>1?i.Gauge:0;/* outer anchors across B */double cy=(i.PedestalB-across)/2,cx=(i.PedestalN-2*f)/2;if(cy<=0||cx<=0)throw new Exception("The anchors do not fit on the pedestal.");
  notes.Add("Plate "+F(i.B,"0")+" x "+F(i.N,"0")+" x "+F(i.Tp,"0")+" mm on a "+F(i.PedestalB,"0")+" x "+F(i.PedestalN,"0")+" mm pedestal: √(A2/A1) = "+F(ratio,"0.00")+", fp,max = φc 0.85 fc' √(A2/A1) = "+F(fpMax,"0.00")+" MPa, qmax = "+F(qMax/1000*1000,"0")+" N/mm.");
  notes.Add("Cantilevers: m = "+F(m)+" mm, n = "+F(n)+" mm"+(lp>0?", λn' (λ = 1, conservative) = "+F(lp)+" mm":"")+"; anchors "+nAll+" x "+(i.BoltName!=""?i.BoltName:AnchorBolts.Short(i.BoltDia))+", "+nt+" per side at "+F(i.EdgeN,"0")+" mm from the plate ends (f = "+F(f)+" mm), gauge "+F(across,"0")+" mm; pedestal edges cx = "+F(cx)+" mm, cy = "+F(cy)+" mm.");
  /* anchor capacities */
  double h15=1.5*i.Hef;double Nb=10*sq*Math.Pow(i.Hef,1.5)/1000;if(i.Headed&&i.Hef>=280&&i.Hef<=635)Nb=Math.Min(Nb,3.9*sq*Math.Pow(i.Hef,5.0/3)/1000);double psiC=i.Cracked?1:1.25;
  Func<double,double,double,double> breakout=(ancX,ancY,count)=>{double ANco=9*i.Hef*i.Hef;double ca=Math.Min(cx,cy);double psiEd=ca>=h15?1:.7+.3*ca/h15;return .70*Math.Min(ancX*ancY/ANco,count)*psiEd*psiC*Nb;};
  double row=Math.Min(cx,h15)+Math.Min(h15,f);/* tension row only: toward the free edge and back toward the other row */double width=Math.Min(cy,h15)*2+across;
  double phiNcbgRow=breakout(row,width,nt),phiNcbgAll=breakout(Math.Min(cx,h15)*2+2*f,width,nAll);
  double Np=i.Headed?8*TrussSupport.BearingArea(i.BoltDia)*i.Fc/1000:.9*i.Fc*Math.Max(3*da,Math.Min(4.5*da,i.HookEh))*da/1000;double phiNp1=.70*(i.Cracked?1:1.4)*Np;double phiNsa1=.75*ase*futa/1000;
  double phiVsa=.65*.6*ase*futa*(i.Grout?.8:1)/1000*nAll;double ca1=cx,ca2=cy,le=Math.Min(i.Hef,8*da);double Vb=Math.Min(.6*Math.Pow(le/da,.2)*Math.Sqrt(da)*sq*Math.Pow(ca1,1.5),3.7*sq*Math.Pow(ca1,1.5))/1000;double AVc=(Math.Min(ca2,1.5*ca1)*2+across)*Math.Min(1.5*ca1,i.PedestalDepth),AVco=4.5*ca1*ca1;double psiEdV=ca2>=1.5*ca1?1:.7+.3*ca2/(1.5*ca1),psiH=Math.Max(1,Math.Sqrt(1.5*ca1/i.PedestalDepth));double phiVcbg=.70*Math.Min(AVc/AVco,nt)*psiEdV*(i.Cracked?1:1.4)*psiH*Vb;double phiVcpg=.70*(i.Hef<65?1:2)*phiNcbgAll/.70;
  notes.Add("Shear breakout toward the pedestal edge in the moment direction (ca1 = "+F(ca1)+" mm) by the front anchors; "+(i.Friction?"friction 0.55 Pu on grout is also taken (AISC DG1 3.5) when the column is in compression":"no friction taken")+".");
  var best=new Dictionary<string,SupportCheck>();Action<string,string,SupportReaction,double,double,string,string> keep=(name,clause,rx,demand,capacity,unit,note)=>{double r=capacity<=0?double.PositiveInfinity:demand/capacity;SupportCheck s;if(!best.TryGetValue(name,out s)||r>s.Ratio)best[name]=new SupportCheck{Name=name,Clause=clause,Node=rx.Node,Case=rx.Case,Demand=demand,Capacity=capacity,Ratio=r,Unit=unit,Note=note};};
  foreach(var rx in reactions){var d=new BasePlateRow{Reaction=rx};d.Pu=(i.Vertical=="Z"?rx.Fz:rx.Fy)*1000;/* N, + compression */double h1=i.Vertical=="Z"?rx.Fx:rx.Fx,h2=i.Vertical=="Z"?rx.Fy:rx.Fz;d.Vu=Math.Sqrt(h1*h1+h2*h2)*1000;double mk=i.MomentAxis=="Mx"?rx.Mx:i.MomentAxis=="My"?rx.My:rx.Mz;d.Mu=Math.Abs(mk)*1e6;/* N.mm */
   if(d.Pu>0){d.E=d.Mu/d.Pu;d.Ecrit=i.N/2-d.Pu/(2*qMax);
    if(d.E<=d.Ecrit+1e-9){d.Y=i.N-2*d.E;d.Fp=d.Pu/(i.B*d.Y);d.Tu=0;}
    else{d.LargeMoment=true;double a=f+i.N/2,need=2*d.Pu*(d.E+f)/qMax;if(a*a<need){d.Y=double.NaN;d.Tu=double.PositiveInfinity;keep("Bearing / plate size (large moment)","AISC DG1 3.4",rx,need,a*a,"mm²","(f + N/2)² ≥ 2Pu(e + f)/qmax fails - enlarge the plate");}else{d.Y=a-Math.Sqrt(a*a-need);d.Tu=qMax*d.Y-d.Pu;d.Fp=fpMax;}}
    if(!double.IsNaN(d.Y)){double l=Math.Max(m,Math.Max(n,lp));d.TpBearing=d.E<1e-9&&!d.LargeMoment?l*Math.Sqrt(2*d.Pu/(.9*i.Fy*i.B*i.N)):Math.Max(d.Y>=m?1.5*m*Math.Sqrt(d.Fp/i.Fy):2.11*Math.Sqrt(d.Fp*d.Y*(m-d.Y/2)/i.Fy),1.5*n*Math.Sqrt(d.Fp/i.Fy));keep("Concrete bearing","AISC DG1 / J8",rx,d.Fp,fpMax,"MPa",d.LargeMoment?"large moment: fp = fp,max over Y = "+F(d.Y)+" mm":"e = "+F(d.E)+" mm ≤ ecrit = "+F(d.Ecrit)+" mm, Y = "+F(d.Y)+" mm");keep("Plate thickness - bearing","AISC DG1 3.3 / 3.4",rx,d.TpBearing,i.Tp,"mm","m = "+F(m)+", n = "+F(n)+(lp>0?", λn' = "+F(lp):"")+" mm");}}
   else{d.Uplift=true;d.Tu=-d.Pu+(d.Mu>0?d.Mu/(2*f):0);}
   if(d.Tu>0&&!double.IsInfinity(d.Tu)){double x=f-i.ColumnD/2+(i.ColumnType.StartsWith("I")?i.ColumnTf/2:0);if(x>0){d.TpTension=2.11*Math.Sqrt(d.Tu/(d.Uplift?2:1)*x/(i.B*i.Fy));keep("Plate thickness - tension","AISC DG1 3.4",rx,d.TpTension,i.Tp,"mm","x = "+F(x)+" mm from the column face to the anchors");}
    double perBolt=d.Uplift?d.Tu/nAll:d.Tu/nt;/* N */double groupN=d.Uplift?d.Tu:d.Tu;keep("Anchor steel - tension","417.4.1",rx,perBolt/1000,phiNsa1,"kN","per anchor, φ 0.75 Ase futa");keep("Concrete breakout - tension","417.4.2",rx,groupN/1000,d.Uplift?phiNcbgAll:phiNcbgRow,"kN",d.Uplift?"all anchors (uplift)":"tension row ("+nt+" anchors)");keep("Anchor pullout","417.4.3",rx,perBolt/1000,phiNp1,"kN","per anchor");}
   else foreach(var name in new[]{"Anchor steel - tension","Concrete breakout - tension","Anchor pullout"})keep(name,name.StartsWith("Anchor steel")?"417.4.1":name.StartsWith("Concrete")?"417.4.2":"417.4.3",rx,0,1,"kN","no anchor tension");
   double friction=i.Friction&&d.Pu>0?.75*.55*d.Pu/1000:0;double vu=Math.Max(0,d.Vu/1000-friction);keep("Anchor steel - shear","417.5.1",rx,vu,phiVsa,"kN","φ 0.65 n 0.6 Ase futa"+(i.Grout?" x 0.8 (grout)":"")+(friction>0?"; friction "+F(friction,"0.0")+" kN deducted":""));keep("Concrete breakout - shear","417.5.2",rx,vu,phiVcbg,"kN","toward the pedestal edge, ca1 = "+F(ca1)+" mm");keep("Concrete pryout","417.5.3",rx,vu,phiVcpg,"kN","φ 0.70 kcp Ncbg");
   double tPer=d.Tu>0&&!double.IsInfinity(d.Tu)?(d.Uplift?d.Tu/nAll:d.Tu/nt)/1000:0;double nr=tPer==0?0:Math.Max(tPer/phiNsa1,Math.Max(tPer/phiNp1,(d.Uplift?d.Tu:d.Tu)/1000/(d.Uplift?phiNcbgAll:phiNcbgRow)));double vr=vu/Math.Min(phiVsa,Math.Min(phiVcbg,phiVcpg));double inter=nr<=.2?vr:vr<=.2?nr:(nr+vr)/1.2;keep("Tension-shear interaction","417.6",rx,inter,1,"ratio","governing tension and shear ratios");
   if(rows!=null)rows.Add(d);}
  if(i.ThreadLength>0){double need=AnchorBolts.ThreadNeeded(i.BoltDia,i.Tp);keep("Anchor thread length","detail",reactions[0],need,i.ThreadLength,"mm","plate "+F(i.Tp,"0")+" + washer + nut + 2 threads");}
  string[] order={"Bearing / plate size (large moment)","Concrete bearing","Plate thickness - bearing","Plate thickness - tension","Anchor steel - tension","Concrete breakout - tension","Anchor pullout","Anchor steel - shear","Concrete breakout - shear","Concrete pryout","Tension-shear interaction","Anchor thread length"};
  return order.Where(best.ContainsKey).Select(k=>best[k]).ToList();}
}
}
