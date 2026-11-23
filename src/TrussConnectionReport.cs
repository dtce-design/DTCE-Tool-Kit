using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* The truss connection report as an illustrated engineering document (owner, 23 November 2026: "mas magagandang report at detalyado as
   may diagrams as much as possible, para maiintindihan", "at maganda sa mata"): the section, the load path, the weld or bolt detail with
   its block-shear paths, every limit state as a check card with its clause, a utilisation chart and the complete step-by-step
   calculation with the values. */
static class TrussConnectionReport {
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;
 static string N(double v,string f="0.##"){return v.ToString(f,Ci);}
 /* the clause of each limit state (AISC 360-16 / NSCP 2015 Chapter 5) */
 internal static string Clause(string name){string n=name.ToLowerInvariant();
  if(n.Contains("block shear"))return "J4.3 / NSCP 510.4.3";if(n.Contains("weld"))return "J2.4 / NSCP 510.2.4";if(n.Contains("bolt shear"))return "J3.6 / NSCP 510.3.6";if(n.Contains("bearing"))return "J3.10 / NSCP 510.3.10";
  if(n.Contains("gusset gross"))return "J4.1(a) / NSCP 510.4.1";if(n.Contains("gusset net"))return "J4.1(b) / NSCP 510.4.1";if(n.Contains("yield"))return "D2(a) / NSCP 504.2";if(n.Contains("rupture"))return "D2(b), D3 / NSCP 504.2";return "AISC 360-16";}
 internal static CalculationDocument Build(ConnectionInput i,ConnectionResult r,SteelProfile p,string audit,IDictionary<string,Control> inputs){
  bool weld=i.Connection=="Weld",pair=i.Family=="Double angle";int legs=pair?2:1;double gross=i.Area*legs;
  var rep=new RichReport("Truss Connection - "+(weld?"Welded":"Bolted")+" "+i.Family.ToLowerInvariant());
  rep.Heading("1  DESIGN BASIS").Paragraph("AISC 360-16 / NSCP 2015 Chapter 5, "+(i.Method=="LRFD"?"LRFD: available strength = φRn (yielding φ = 0.90, rupture / welds / bolts / block shear φ = 0.75)":"ASD: available strength = Rn / Ω (yielding Ω = 1.67, rupture / welds / bolts / block shear Ω = 2.00)")+". The truss member carries an axial force P through a concentric "+(weld?"welded":"bolted")+" connection to the gusset plate; every listed limit state must be at least P.");
  rep.Heading("2  MEMBER, MATERIALS AND DEMAND").KeyValues(new[]{new[]{"Member",p.Name+(pair?" - two angles back to back":"")+" ("+p.Source+")"},new[]{"Area Ag",N(i.Area,"0")+" mm² per "+(pair?"angle × 2 = "+N(gross,"0")+" mm²":"member")},new[]{"Thickness t / centroid x̄",N(i.Thickness,"0.#")+" mm / "+N(i.Offset,"0.#")+" mm"},new[]{"Member steel Fy / Fu",N(i.Fy,"0")+" / "+N(i.Fu,"0")+" MPa"},
   new[]{"Gusset plate",N(i.GussetThickness,"0.#")+" mm thick"+(pair?" (= back-to-back spacing)":"")+", effective width "+N(i.GussetWidth,"0")+" mm, Fy / Fu "+N(i.GussetFy,"0")+" / "+N(i.GussetFu,"0")+" MPa"},new[]{"Design axial force P",N(i.Demand)+" kN ("+i.Method+")"}});
  rep.Figure(175,(g,rc)=>SteelSectionPreview.Render(p,pair,pair?i.GussetThickness:0,g,rc),"Figure 1 - member section"+(pair?", the two angles spaced by the gusset plate":""));
  rep.Figure(170,(g,rc)=>LoadPath(g,rc,i),"Figure 2 - load path: member force P → "+(weld?"welds":"bolts")+" → gusset plate");
  rep.Heading("3  CONNECTION DETAIL");
  if(weld)rep.KeyValues(new[]{new[]{"Fillet weld",N(i.Weld,"0.#")+" mm leg, throat 0.707w = "+N(.707*i.Weld,"0.##")+" mm, FEXX "+N(i.Fexx,"0.##")+" MPa"},new[]{"Weld lengths",i.Family.StartsWith("HSS")?"LL1 = "+N(i.LL1,"0")+" / LL2 = "+N(i.LL2,"0")+" mm along the slot, LT = "+N(i.LT,"0")+" mm":"LL1 (heel) = "+N(i.LL1,"0")+", LL2 (toe) = "+N(i.LL2,"0")+", LT (end) = "+N(i.LT,"0")+" mm"+(pair?", each angle":"")}});
  else rep.KeyValues(new[]{new[]{"Bolts",i.Bolts+" × Ø"+N(i.BoltDiameter,"0")+" "+i.Grade+", threads "+(i.Fnv>=457?"excluded":"included")+", Fnv = "+N(i.Fnv,"0")+" MPa"},new[]{"Holes / layout","hole "+N(i.HoleDiameter,"0")+" mm; end distance "+N(i.EdgeDistance,"0")+" mm; spacing "+N(i.Spacing,"0")+" mm; one row"+(r.Gauge>0?" at g = "+N(r.Gauge,"0")+" mm from the heel":"")},new[]{"Shear planes",i.ShearPlanes+(i.ShearPlanes==2?" (double shear: angles on both faces of the gusset)":" (single shear)")}});
  rep.Figure(240,(g,rc)=>{if(weld)TrussWeldDrawing.Render(inputs,g,rc);else TrussBoltDrawing.Render(inputs,g,rc);},"Figure 3 - "+(weld?"weld detail":"bolt layout with the block-shear paths, drawn to scale"));
  /* shear lag */
  double L=weld?(i.LL1+i.LL2)/2:(i.Bolts-1)*i.Spacing;
  rep.Heading("4  SHEAR LAG").Paragraph(i.AllElementsConnected?"All elements of the section are connected: U = 1.0.":i.VerifiedU>0?"Shear-lag factor U = "+N(r.U,"0.###")+" (verified input).":"U = 1 - x̄ / L = 1 - "+N(i.Offset,"0.#")+" / "+N(L,"0.#")+" = "+N(r.U,"0.###")+"   (L = "+(weld?"(LL1 + LL2) / 2":"(n - 1) s")+" = "+N(L,"0.#")+" mm; AISC Table D3.1 case 2).");
  rep.Figure(120,(g,rc)=>ShearLag(g,rc,i,r,L),"Figure 4 - shear lag: only the connected leg is loaded at the joint; U reduces the effective net area");
  /* block shear */
  rep.Heading("5  BLOCK SHEAR");
  rep.Paragraph("Block shear is the tearing out of a block of steel along the "+(weld?"weld outline":"line of bolt holes")+": shear on the planes along the force (Agv gross, Anv net of holes) plus tension on the plane across it (Ant). Rn = min(0.6Fu Anv + Ubs Fu Ant, 0.6Fy Agv + Ubs Fu Ant), Ubs = 1.");
  var paths=r.BlockPaths.ToList();if(i.Agv>0)paths.Add(new BlockShearPath{Name="Verified member path (entered)",Basis="entered areas",Agv=i.Agv,Anv=i.Anv,Ant=i.Ant,Fy=i.Fy,Fu=i.Fu});
  if(paths.Count>0){rep.Table(new[]{"Path","Agv mm²","Anv mm²","Ant mm²","0.6FuAnv+FuAnt","0.6FyAgv+FuAnt","Rn kN"},paths.Select(q=>new[]{q.Name,N(q.Agv,"0"),N(q.Anv,"0"),N(q.Ant,"0"),N((.6*q.Fu*q.Anv+q.Fu*q.Ant)/1000,"0.0"),N((.6*q.Fy*q.Agv+q.Fu*q.Ant)/1000,"0.0"),N(q.Nominal,"0.0")}).ToList(),new[]{2.4f,.9f,.9f,.9f,1.2f,1.2f,.8f});
   foreach(var q in paths)rep.Paragraph(q.Name+": "+q.Basis+".",true);
   if(weld&&!i.Family.StartsWith("HSS"))rep.Figure(170,(g,rc)=>WeldBlock(g,rc,i),"Figure 5 - gusset block shear around the welds (no holes: Anv = Agv)");}
  else rep.Paragraph(i.Family.StartsWith("HSS")?"A gusset slotted into an HSS / pipe has no block-shear path; check the wall and gusset shear along the welds separately.":"No block-shear path could be formed from the inputs.",true);
  /* limit states */
  rep.Heading("6  LIMIT STATES");
  foreach(var c in r.Checks){double phi=c.Name.IndexOf("yield",StringComparison.OrdinalIgnoreCase)>=0?.9:.75,om=phi==.9?1.67:2,rn=i.Method=="LRFD"?c.Capacity/phi:c.Capacity*om;
   rep.Check(c.Name,Clause(c.Name),new[]{"Rn = "+N(rn,"0.0")+" kN; "+(i.Method=="LRFD"?"φRn = "+N(phi)+" × "+N(rn,"0.0"):"Rn/Ω = "+N(rn,"0.0")+" / "+N(om))+" = "+N(c.Capacity,"0.0")+" kN",c.Note},"",c.Demand,c.Capacity,"kN",c.Pass);}
  foreach(var e in r.DetailErrors)rep.Paragraph("DETAIL: "+e,false,true);
  /* summary and chart */
  rep.Heading("7  SUMMARY");var ordered=r.Checks.OrderByDescending(c=>c.Demand/c.Capacity).ToList();
  rep.Table(new[]{"Limit state","Available kN","P kN","Ratio","Result"},ordered.Select(c=>new[]{c.Name,N(c.Capacity,"0.0"),N(c.Demand,"0.0"),N(c.Demand/c.Capacity,"0.00"),c.Pass?"OK":"NOT OK"}).ToList(),new[]{3f,1f,.8f,.7f,.8f},row=>row[4]=="OK"?(Color?)null:Color.FromArgb(252,232,232));
  rep.Figure(Math.Max(120,24+18*ordered.Count),(g,rc)=>Utilisation(g,rc,ordered),"Figure 6 - utilisation of every limit state (P / available strength); 1.00 is the limit");
  var gov=ordered.FirstOrDefault();rep.Paragraph(r.ListedChecksPass?"All listed limit states pass. Governing: "+(gov==null?"-":gov.Name+", ratio "+N(gov.Demand/gov.Capacity,"0.00"))+".":"The connection does NOT pass: "+string.Join("; ",ordered.Where(c=>!c.Pass).Select(c=>c.Name).Concat(r.DetailErrors.Count>0?new[]{"detail limits"}:new string[0]))+".",false,true);
  /* the complete calculation with the values */
  rep.Heading("8  DETAILED CALCULATION");
  foreach(var line in DetailedDesignReports.Truss(i,r,p.Name,p.Source).Split('\n').Select(x=>x.TrimEnd('\r')).Where(x=>x.Trim()!="").Skip(3)){bool head=line.Length>3&&char.IsDigit(line[0])&&char.IsDigit(line[1])&&line[2]==' ';rep.Paragraph(head?line.Trim():line,false,head);}
  rep.Heading("9  DEMAND SOURCE");foreach(var line in audit.Split('\n').Where(x=>x.Trim()!=""))rep.Paragraph(line.Trim(),true);
  rep.Paragraph("Scope: axial, concentric connection only. Member buckling, combined bending / shear, gusset buckling (Whitmore), HSS chord / joint limit states, prying, fatigue and seismic detailing are outside this sheet.",true);
  rep.Signatures("Prepared by","Checked by");return rep.Build();}
 static readonly Color Ink=Color.FromArgb(40,52,66),Member=Color.FromArgb(200,214,230),Gusset=Color.FromArgb(228,228,222),Force=Color.FromArgb(190,45,45);
 /* member, connection and gusset with the force arrows */
 static void LoadPath(Graphics g,RectangleF rc,ConnectionInput i){g.SmoothingMode=SmoothingMode.AntiAlias;float cy=rc.Y+rc.Height/2,x0=rc.X+30,gw=rc.Width*0.28f,mx=x0+gw*0.55f,mw=rc.Width*0.55f;
  using(var gb=new SolidBrush(Gusset))using(var mb=new SolidBrush(Member))using(var pen=new Pen(Ink,1.2f))using(var f=new Font("Segoe UI",8f))using(var fb=new Font("Segoe UI",9f,FontStyle.Bold))using(var ink=new SolidBrush(Ink))using(var red=new SolidBrush(Force)){
   var gp=new[]{new PointF(x0,cy-55),new PointF(x0+gw,cy-30),new PointF(x0+gw,cy+30),new PointF(x0,cy+55)};g.FillPolygon(gb,gp);g.DrawPolygon(pen,gp);g.DrawString("gusset plate",f,ink,x0+4,cy+58);
   var m=new RectangleF(mx,cy-14,mw,28);g.FillRectangle(mb,m);g.DrawRectangle(pen,m.X,m.Y,m.Width,m.Height);g.DrawString(i.Family+" member",f,ink,mx+gw*0.6f,cy-30);
   if(i.Connection=="Weld")using(var w=new Pen(Color.SteelBlue,4)){g.DrawLine(w,mx,cy-14,x0+gw,cy-14);g.DrawLine(w,mx,cy+14,x0+gw,cy+14);}
   else using(var b=new SolidBrush(Color.FromArgb(90,100,115))){int n=Math.Max(1,Math.Min(8,i.Bolts));float step=(x0+gw-mx-16)/Math.Max(1,n-1);for(int k=0;k<n;k++){float bx=mx+8+(n==1?0:k*step);g.FillEllipse(b,bx-5,cy-5,10,10);}}
   using(var ar=new Pen(Force,3){EndCap=LineCap.ArrowAnchor}){g.DrawLine(ar,mx+mw-60,cy,mx+mw+40,cy);}g.DrawString("P = "+N(i.Demand)+" kN",fb,red,mx+mw-70,cy+18);
   g.DrawString(i.Connection=="Weld"?"welds carry P into the gusset":i.Bolts+" bolts carry P into the gusset",f,ink,mx,cy+22);}}
 /* shear lag: the connected leg, the outstanding leg, x̄ and L */
 static void ShearLag(Graphics g,RectangleF rc,ConnectionInput i,ConnectionResult r,double L){g.SmoothingMode=SmoothingMode.AntiAlias;float x=rc.X+40,y=rc.Y+28,w=rc.Width*0.5f;
  using(var mb=new SolidBrush(Member))using(var pen=new Pen(Ink,1.2f))using(var f=new Font("Segoe UI",8f))using(var ink=new SolidBrush(Ink))using(var dim=new Pen(Ink,1)){
   g.FillRectangle(mb,x,y,w,40);g.DrawRectangle(pen,x,y,w,40);g.DrawString("connected leg (on the gusset)",f,ink,x+6,y+12);
   using(var c=new Pen(Color.SteelBlue,4))g.DrawLine(c,x,y+40,x+w*0.45f,y+40);g.DrawString("L = "+N(L,"0")+" mm",f,ink,x,y+46);
   float sx=x+w+60;g.FillRectangle(mb,sx,y,10,60);g.FillRectangle(mb,sx,y+50,60,10);g.DrawRectangle(pen,sx,y,10,60);g.DrawRectangle(pen,sx,y+50,60,10);
   float xb=Math.Min(55,(float)(i.Offset*0.6));using(var d=new Pen(Force,1.2f){DashStyle=DashStyle.Dash})g.DrawLine(d,sx+xb,y-6,sx+xb,y+70);g.DrawString("x̄ = "+N(i.Offset,"0.#"),f,ink,sx+xb+4,y-8);
   using(var fb=new Font("Segoe UI",10f,FontStyle.Bold))g.DrawString("U = "+N(r.U,"0.###"),fb,ink,sx+90,y+20);}}
 /* gusset tear-out around the weld outline */
 static void WeldBlock(Graphics g,RectangleF rc,ConnectionInput i){g.SmoothingMode=SmoothingMode.AntiAlias;double len=Math.Max(i.LL1,i.LL2),wd=Math.Max(20,i.ConnectedWidth);float s=(float)Math.Min((rc.Width-200)/(len+150),(rc.Height-50)/(wd+60));float x0=rc.X+40,y0=rc.Y+25;
  using(var gb=new SolidBrush(Gusset))using(var mb=new SolidBrush(Color.FromArgb(150,Member)))using(var pen=new Pen(Ink,1.2f))using(var f=new Font("Segoe UI",8f))using(var ink=new SolidBrush(Ink)){
   g.FillRectangle(gb,x0,y0,(float)((len+60)*s),(float)((wd+30)*s));g.DrawRectangle(pen,x0,y0,(float)((len+60)*s),(float)((wd+30)*s));float mx=x0+(float)(60*s),my=y0+(float)(15*s);
   g.FillRectangle(mb,mx,my,(float)((len+90)*s),(float)(wd*s));g.DrawRectangle(pen,mx,my,(float)((len+90)*s),(float)(wd*s));
   using(var w=new Pen(Color.SteelBlue,4)){g.DrawLine(w,mx,my,mx+(float)(i.LL1*s),my);g.DrawLine(w,mx,my+(float)(wd*s),mx+(float)(i.LL2*s),my+(float)(wd*s));}
   using(var t=new Pen(Force,2.2f){DashStyle=DashStyle.Dash})g.DrawLines(t,new[]{new PointF(mx+(float)(i.LL1*s),my),new PointF(mx,my),new PointF(mx,my+(float)(wd*s)),new PointF(mx+(float)(i.LL2*s),my+(float)(wd*s))});
   g.DrawString("shear Agv = (LL1 + LL2) tg",f,ink,mx+(float)(len*s)+10,my-4);g.DrawString("tension Ant = width "+N(wd,"0")+" × tg",f,ink,x0+2,my+(float)(wd*s)+8);}}
 /* horizontal bars of P / available strength */
 static void Utilisation(Graphics g,RectangleF rc,List<ConnectionCheck> checks){g.SmoothingMode=SmoothingMode.AntiAlias;float label=rc.Width*0.42f,x0=rc.X+label,w=rc.Width-label-60,y=rc.Y+10;double max=Math.Max(1.2,checks.Count==0?1:checks.Max(c=>c.Demand/c.Capacity));
  using(var f=new Font("Segoe UI",7.5f))using(var ink=new SolidBrush(Ink))using(var ok=new SolidBrush(Color.FromArgb(60,150,110)))using(var ng=new SolidBrush(Force)){
   foreach(var c in checks){double ratio=c.Demand/c.Capacity;g.DrawString(c.Name,f,ink,new RectangleF(rc.X+6,y,label-10,16));g.FillRectangle(ratio<=1?ok:ng,x0,y+3,(float)(w*Math.Min(ratio,max)/max),10);g.DrawString(N(ratio,"0.00"),f,ink,x0+(float)(w*Math.Min(ratio,max)/max)+4,y);y+=18;}
   float one=x0+(float)(w/max);using(var lim=new Pen(Ink,1.2f){DashStyle=DashStyle.Dash})g.DrawLine(lim,one,rc.Y+4,one,y+2);g.DrawString("1.00",f,ink,one-10,y+2);}}
}
}
