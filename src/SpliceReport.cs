using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
/* Splice, Development & Hooks report with its figures (owner, 23 November 2026: "complete calculation lahat sa design sheets reports,
   including diagrams, illustrations, everything"): the adopted lengths of all eight diameters in one table, then for each diameter the
   development and lap-splice diagram, the standard hooks drawn to scale with their bend, tail and cutting length, and the complete
   calculation. */
static class SpliceReport {
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;static string N(double v,string f="0"){return v.ToString(f,Ci);}
 static readonly int[] Diameters={10,12,16,20,25,28,32,36};
 static readonly Color Ink=Color.FromArgb(40,52,66),Bar=Color.FromArgb(190,40,40),Bar2=Color.FromArgb(39,110,160),Concrete=Color.FromArgb(232,232,228);
 internal static CalculationDocument Build(LengthConfiguration c,bool top,double straight){var rep=new RichReport("Splice, Development & Hooks - "+(string.IsNullOrWhiteSpace(c.Name)?"configuration":c.Name));
  rep.Heading("SUMMARY OF ADOPTED LENGTHS (mm)");var rows=new List<string[]>();
  foreach(int d in Diameters){var bi=c.ForDiameter(d,false);bi.Straight=straight;var ti=c.ForDiameter(d,true);ti.Straight=straight;var b=RebarLengths.Calculate(bi);var t=RebarLengths.Calculate(ti);
   rows.Add(new[]{"Ø"+d,N(bi.Fy),N(c.Rounded(b.Ld)),N(c.Rounded(t.Ld)),N(c.Rounded(b.LapA))+" / "+N(c.Rounded(b.LapB)),N(c.Rounded(t.LapA))+" / "+N(c.Rounded(t.LapB)),N(c.Rounded(b.Ldh)),N(c.Rounded(b.Ldc)),N(c.Rounded(b.CompressionLap)),N(b.Cut90)+" / "+N(b.Cut180)});}
  rep.Table(new[]{"Bar","fy MPa","Ld other","Ld top","Lap A / B other","Lap A / B top","Ldh","Ldc","Comp. lap","Hook cut 90° / 180°"},rows,new[]{.6f,.6f,.7f,.7f,1.1f,1.1f,.6f,.6f,.7f,1.2f});
  rep.Paragraph("Top bars: more than 300 mm of fresh concrete below (ψt = 1.3). Class A lap when As provided ≥ 2 As required and no more than half the bars are spliced at one place; otherwise Class B. Lengths are rounded up to "+N(c.Input.Round)+" mm; the cutting lengths are along the bar centreline.",true);
  rep.Figure(150,RichReport.Raster((g,rc)=>Chart(g,rc,c,straight)),"Figure 1 - adopted tension development Ld, Class B lap and hooked development Ldh of every diameter (other bars)");
  RichText.Add(rep,c.Review(),null,1);
  foreach(int d in Diameters){var i=c.ForDiameter(d,top);i.Straight=straight;var r=RebarLengths.Calculate(i);rep.PageBreak();rep.Heading("BAR Ø"+d+" - "+(top?"TOP":"OTHER")+" BARS");
   var ii=i;var rr=r;double round=c.Input.Round;
   rep.Figure(170,RichReport.Raster((g,rc)=>Lengths(g,rc,ii,rr,round)),"Ø"+d+": straight development Ld into the support, tension lap splice (Class "+r.LapClass+") and hooked development Ldh - to scale");
   rep.Figure(190,RichReport.Raster((g,rc)=>Hooks(g,rc,ii,rr)),"Ø"+d+": standard hooks to scale - inside bend diameter, tail and centreline cutting length (straight leg "+N(straight)+" mm)");
   RichText.Add(rep,RebarLengths.Report(i),null,2);}
  rep.Signatures("Prepared by","Checked by");return rep.Build();}
 static void Chart(Graphics g,RectangleF rc,LengthConfiguration c,double straight){g.SmoothingMode=SmoothingMode.AntiAlias;var data=Diameters.Select(d=>{var i=c.ForDiameter(d,false);i.Straight=straight;var r=RebarLengths.Calculate(i);return new{D=d,Ld=c.Rounded(r.Ld),Lap=c.Rounded(r.LapB),Ldh=c.Rounded(r.Ldh)};}).ToList();
  double max=data.Max(q=>Math.Max(q.Ld,q.Lap))*1.1;float left=rc.X+40,bottom=rc.Bottom-22,h=rc.Height-40,slot=(rc.Width-60)/data.Count,bw=slot/4.2f;
  using(var f=new Font("Segoe UI",7f))using(var ink=new SolidBrush(Ink))using(var grid=new Pen(Color.FromArgb(220,225,230))){for(int k=0;k<=4;k++){float y=bottom-h*k/4;g.DrawLine(grid,left,y,rc.Right-10,y);g.DrawString(N(max*k/4),f,ink,rc.X+2,y-6);}
   var cols=new[]{Bar,Bar2,Color.FromArgb(60,150,110)};for(int k=0;k<data.Count;k++){var q=data[k];double[] v={q.Ld,q.Lap,q.Ldh};float x=left+k*slot+slot*0.15f;for(int j=0;j<3;j++){float bh=(float)(v[j]/max*h);using(var b=new SolidBrush(cols[j]))g.FillRectangle(b,x+j*bw,bottom-bh,bw-1,bh);}g.DrawString("Ø"+q.D,f,ink,x+bw*0.6f,bottom+4);}
   string[] names={"Ld","Lap B","Ldh"};for(int j=0;j<3;j++){using(var b=new SolidBrush(cols[j]))g.FillRectangle(b,rc.Right-190+j*60,rc.Y+4,10,10);g.DrawString(names[j],f,ink,rc.Right-176+j*60,rc.Y+2);}}}
 /* bar drawn as a band db wide (minimum hairline) on the concrete */
 static void BarLine(Graphics g,Color col,float w,params PointF[] pts){using(var p=new Pen(col,Math.Max(1.2f,w)){LineJoin=LineJoin.Round,StartCap=LineCap.Round,EndCap=LineCap.Round})g.DrawLines(p,pts);}
 static void Dim(Graphics g,Font f,PointF a,PointF b,string label,float off){using(var p=new Pen(Ink,0.8f)){g.DrawLine(p,a.X,a.Y+off,b.X,b.Y+off);g.DrawLine(p,a.X,a.Y+off-4,a.X,a.Y+off+4);g.DrawLine(p,b.X,b.Y+off-4,b.X,b.Y+off+4);}using(var br=new SolidBrush(Ink)){var sz=g.MeasureString(label,f);g.DrawString(label,f,br,(a.X+b.X)/2-sz.Width/2,a.Y+off-sz.Height-1);}}
 static void Lengths(Graphics g,RectangleF rc,LengthInput i,LengthResult r,double round){g.SmoothingMode=SmoothingMode.AntiAlias;double ld=RebarLengths.Up(r.Ld,round),lap=RebarLengths.Up(r.Lap,round),ldh=RebarLengths.Up(r.Ldh,round);
  /* three panels, one scale for all so the lengths compare: straight development, lap splice, hooked development */
  float pw=rc.Width/3,y=rc.Y+rc.Height*0.55f;float s=(float)Math.Min((pw-50)/(Math.Max(ld,ldh)+120),(pw-50)/(lap+120));float w=Math.Max(1.5f,(float)(i.Db*s)*1.5f);
  using(var f=new Font("Segoe UI",7.5f))using(var fb=new Font("Segoe UI",8f,FontStyle.Bold))using(var ink=new SolidBrush(Ink))using(var conc=new SolidBrush(Concrete))using(var edge=new Pen(Ink,1))using(var cs=new Pen(Color.Gray,1){DashStyle=DashStyle.Dash}){
   g.DrawString("Ø"+N(i.Db)+", fy "+N(i.Fy)+" MPa, f'c "+N(i.Fc)+" MPa, "+(i.Top?"top bar":"other bar"),f,ink,rc.X+10,rc.Y+4);
   /* a: straight development into the support */float x0=rc.X+20;var sup=new RectangleF(x0,y-36,(float)(ld*s)+16,72);g.FillRectangle(conc,sup);g.DrawRectangle(edge,sup.X,sup.Y,sup.Width,sup.Height);float crit=sup.Right-8;g.DrawLine(cs,crit,sup.Y-8,crit,sup.Bottom+8);
   BarLine(g,Bar,w,new PointF(crit-(float)(ld*s),y),new PointF(crit+40,y));Dim(g,f,new PointF(crit-(float)(ld*s),y),new PointF(crit,y),"Ld = "+N(ld),-46);g.DrawString("critical section",f,ink,crit-40,sup.Bottom+10);g.DrawString("a) straight development",fb,ink,x0,rc.Y+18);
   /* b: lap splice */float lx=rc.X+pw+40;BarLine(g,Bar,w,new PointF(lx-30,y-w),new PointF(lx+(float)(lap*s),y-w));BarLine(g,Bar2,w,new PointF(lx,y+w),new PointF(lx+(float)(lap*s)+30,y+w));Dim(g,f,new PointF(lx,y),new PointF(lx+(float)(lap*s),y),"Class "+r.LapClass+" lap = "+N(lap),-14);g.DrawString("b) tension lap splice",fb,ink,rc.X+pw+10,rc.Y+20);
   /* c: hooked development */float hx=rc.X+2*pw+20;var sup2=new RectangleF(hx,y-36,(float)(ldh*s)+24,72);g.FillRectangle(conc,sup2);g.DrawRectangle(edge,sup2.X,sup2.Y,sup2.Width,sup2.Height);float c2=sup2.Right-8;g.DrawLine(cs,c2,sup2.Y-8,c2,sup2.Bottom+8);
   float hk=c2-(float)(ldh*s)+(float)(i.Db*s/2);BarLine(g,Bar,w,new PointF(c2+40,y),new PointF(hk,y),new PointF(hk,y+Math.Min(30,(float)(r.Tail90*s)+6)));Dim(g,f,new PointF(c2-(float)(ldh*s),y),new PointF(c2,y),"Ldh = "+N(ldh),-46);g.DrawString("c) hooked development",fb,ink,hx,rc.Y+20);}}
 static void Hooks(Graphics g,RectangleF rc,LengthInput i,LengthResult r){g.SmoothingMode=SmoothingMode.AntiAlias;double Rm=(r.MainInside+i.Db)/2,Rt=r.TieInside>0?(r.TieInside+i.Db)/2:0;
  double tailMax=Math.Max(r.Tail90,Math.Max(r.Tail180,r.Tail135));float s=(float)Math.Min((rc.Width/3-60)/(Math.Max(120,Rm*2)+2*Rm+tailMax),(rc.Height-70)/(r.Tail90+2*Rm+20));float w=Math.Max(1.5f,(float)(i.Db*s));
  using(var f=new Font("Segoe UI",7.5f))using(var fb=new Font("Segoe UI",8f,FontStyle.Bold))using(var ink=new SolidBrush(Ink)){
   Action<int,string,double,double,double,double> hook=(slot,name,angle,R,tail,cut)=>{float ox=rc.X+slot*rc.Width/3+20,oy=rc.Y+rc.Height-40;g.DrawString(name,fb,ink,ox,rc.Y+4);
    float legL=(float)(Math.Max(120,R*2)*s);var start=new PointF(ox,oy);var tStart=new PointF(ox+legL,oy);/* straight leg then the bend (centre above the leg end), then the tail */
    var path=new GraphicsPath();path.AddLine(start,tStart);float cx=tStart.X,cy=oy-(float)(R*s);double a0=90,sweep=-angle;/* from the bottom of the circle, counter-clockwise up */
    path.AddArc(cx-(float)(R*s),cy-(float)(R*s),(float)(2*R*s),(float)(2*R*s),(float)a0,(float)sweep);var end=path.GetLastPoint();double dir=(a0+sweep-90)*Math.PI/180;/* tangent direction at the end of the arc */
    var tEnd=new PointF(end.X+(float)(Math.Cos(dir)*tail*s),end.Y+(float)(Math.Sin(dir)*tail*s));path.AddLine(end,tEnd);using(var p=new Pen(Bar,w){LineJoin=LineJoin.Round,EndCap=LineCap.Round,StartCap=LineCap.Round})g.DrawPath(p,path);
    using(var c0=new Pen(Color.Gray,0.6f){DashStyle=DashStyle.Dot})g.DrawEllipse(c0,cx-(float)((R-i.Db/2)*s),cy-(float)((R-i.Db/2)*s),(float)((2*R-i.Db)*s),(float)((2*R-i.Db)*s));
    g.DrawString("inside bend ID = "+N(2*R-i.Db)+" mm",f,ink,ox,oy+26);g.DrawString("tail "+N(tail),f,ink,Math.Max(end.X,tEnd.X)+8,(end.Y+tEnd.Y)/2-16);g.DrawString("cut = "+N(cut)+" mm",fb,ink,ox,oy+10);path.Dispose();};
   hook(0,"90° main hook",90,Rm,r.Tail90,r.Cut90);hook(1,"180° main hook",180,Rm,r.Tail180,r.Cut180);if(Rt>0)hook(2,"135° seismic tie hook",135,Rt,r.Tail135,r.Cut135);else g.DrawString("135° tie hook: above Ø25 a project detail is needed",f,ink,rc.X+2*rc.Width/3+20,rc.Y+30);}}
}
}
