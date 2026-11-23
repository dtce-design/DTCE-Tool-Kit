using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
/* Purlin Design report with its figures (owner, 23 November 2026: "complete calculation lahat sa design sheets reports, including
   diagrams, illustrations, everything"): the section, how the loads resolve on the roof slope, the span with the sag rods and the
   moment diagrams about both axes, and the utilisation of every load case and deflection; the complete step-by-step calculation follows
   the figures section by section. */
static class PurlinReport {
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;static string N(double v,string f="0.###"){return v.ToString(f,Ci);}
 static readonly Color Ink=Color.FromArgb(40,52,66),Grid=Color.FromArgb(220,225,230),Blue=Color.FromArgb(39,110,160),Red=Color.FromArgb(190,45,45),Orange=Color.FromArgb(205,120,20),Green=Color.FromArgb(60,150,110);
 internal static CalculationDocument Build(PurlinDesignInput i,PurlinDesignResult r){var rep=new RichReport("Purlin Design - "+i.Name+" ("+i.Profile.Name+")");
  var gov=r.Cases.OrderByDescending(c=>c.Ratio).First();
  rep.Heading("SUMMARY").KeyValues(new[]{new[]{"Purlin",i.Profile.Name+", "+i.Support.ToLowerInvariant()+", span "+N(i.Span,"0.##")+" m, spacing "+N(i.Spacing,"0.##")+" m, roof "+N(i.Angle,"0.#")+"°, "+i.SagRods+" sag rod(s)"},new[]{"Steel","Fy "+N(i.Fy,"0")+" MPa, E "+N(i.E,"0")+" MPa (ASD, workbook allowable stresses)"},
   new[]{"Loads","dead "+N(i.Dead)+" kPa + self weight, live "+N(i.Live)+" kPa, wind pressure "+N(i.WindPressure)+" / suction "+N(i.WindSuction)+" kPa"},new[]{"Flexure","basic envelope "+N(r.Basic,"0.00")+", alternate "+N(r.Alternate,"0.00")+"; largest case "+gov.Name+" "+N(gov.Ratio,"0.00")},
   new[]{"Deflection","live "+N(r.LiveDeflection,"0.0")+" / "+N(r.LiveLimit,"0.0")+" mm, total "+N(r.TotalDeflection,"0.0")+" / "+N(r.TotalLimit,"0.0")+" mm"},new[]{"Result",r.WithinWorkbookChecks?"PASS for the listed workbook checks":"REVIEW - a listed check is exceeded"}});
  rep.Figure(165,(g,rc)=>SteelSectionPreview.Render(i.Profile,false,0,g,rc),"Figure 1 - purlin section, handbook dimensions");
  rep.Figure(190,RichReport.Raster((g,rc)=>Resolution(g,rc,i,r)),"Figure 2 - loads on the sloping roof resolved into wx (normal to the roof, about the strong axis) and wy (along the slope, about the weak axis); wind acts normal to the roof");
  rep.Figure(250,RichReport.Raster((g,rc)=>Moments(g,rc,i,r,gov)),"Figure 3 - span, supports and sag rods; bending moment about each axis for "+gov.Name+" (Mx = Cx wx L², My = Cy wy L²)");
  rep.Figure(Math.Max(130,30+18*(r.Cases.Count+2)),RichReport.Raster((g,rc)=>Utilisation(g,rc,r)),"Figure 4 - utilisation of every load case (interaction ratio) and the deflections against their limits; 1.00 is the limit");
  RichText.Add(rep,DetailedDesignReports.Purlin(i,r),null,3);
  rep.Signatures("Prepared by","Checked by");return rep.Build();}
 static void Arrow(Graphics g,Color c,PointF a,PointF b,string label,Font f,bool labelAtStart=false){using(var p=new Pen(c,2.2f){EndCap=LineCap.ArrowAnchor})g.DrawLine(p,a,b);using(var br=new SolidBrush(c))g.DrawString(label,f,br,labelAtStart?a.X+4:b.X+4,labelAtStart?a.Y-14:b.Y-6);}
 static void Resolution(Graphics g,RectangleF rc,PurlinDesignInput i,PurlinDesignResult r){g.SmoothingMode=SmoothingMode.AntiAlias;double th=i.Angle*Math.PI/180;float cx=rc.X+rc.Width*0.34f,cy=rc.Y+rc.Height*0.58f,len=rc.Width*0.3f;
  using(var f=new Font("Segoe UI",7.5f))using(var fb=new Font("Segoe UI",8f,FontStyle.Bold))using(var ink=new SolidBrush(Ink))using(var roof=new Pen(Ink,2.4f)){
   var a=new PointF(cx-len*(float)Math.Cos(th),cy+len*(float)Math.Sin(th));var b=new PointF(cx+len*(float)Math.Cos(th),cy-len*(float)Math.Sin(th));g.DrawLine(roof,a,b);g.DrawString("roof sheeting at "+N(i.Angle,"0.#")+"°",f,ink,b.X-90,b.Y-18);
   using(var dash=new Pen(Grid,1){DashStyle=DashStyle.Dash})g.DrawLine(dash,a.X,a.Y,b.X,a.Y);
   /* purlin glyph under the roof at the centre */var st=g.Save();g.TranslateTransform(cx,cy);g.RotateTransform(-(float)i.Angle);using(var pb=new SolidBrush(Color.FromArgb(200,214,230)))using(var pp=new Pen(Ink,1.2f)){var box=new RectangleF(-10,0,20,34);g.FillRectangle(pb,box);g.DrawRectangle(pp,box.X,box.Y,box.Width,box.Height);}g.Restore(st);
   float L=70;double w=Math.Sqrt(Math.Pow(r.Dx+r.Lx,2)+Math.Pow(r.Dy+r.Ly,2));
   Arrow(g,Red,new PointF(cx,cy-L-18),new PointF(cx,cy-18),"w = D + L = "+N(w,"0.###")+" kN/m",fb,true);
   var nrm=new PointF((float)Math.Sin(th),(float)Math.Cos(th));/* into the roof, normal */var tan=new PointF(-(float)Math.Cos(th),(float)Math.Sin(th));/* down the slope */
   float lx=L*(float)Math.Cos(th),ly=L*(float)Math.Sin(th);var o=new PointF(cx+rc.Width*0.25f,cy-10);
   Arrow(g,Blue,new PointF(o.X-nrm.X*lx,o.Y-nrm.Y*lx),o,"wx = w cos θ = "+N(r.Dx+r.Lx,"0.###")+" kN/m",fb,true);{var e=new PointF(o.X+tan.X*Math.Max(ly*1.6f,30),o.Y+tan.Y*Math.Max(ly*1.6f,30));using(var p=new Pen(Orange,2.2f){EndCap=LineCap.ArrowAnchor})g.DrawLine(p,o,e);using(var br=new SolidBrush(Orange))g.DrawString("wy = w sin θ = "+N(r.Dy+r.Ly,"0.###")+" kN/m",fb,br,e.X-20,e.Y+6);}
   if(i.WindSuction!=0||i.WindPressure!=0){var wq=new PointF(rc.X+rc.Width*0.08f,rc.Y+rc.Height*0.12f);Arrow(g,Green,wq,new PointF(wq.X+nrm.X*40,wq.Y+nrm.Y*40),"wind normal to the roof: "+N(i.WindPressure,"0.##")+" / "+N(i.WindSuction,"0.##")+" kPa × "+N(i.Spacing,"0.##")+" m",f);}
   g.DrawString("D: Dx = "+N(r.Dx,"0.###")+", Dy = "+N(r.Dy,"0.###")+" kN/m     L: Lx = "+N(r.Lx,"0.###")+", Ly = "+N(r.Ly,"0.###")+" kN/m",f,ink,rc.X+10,rc.Bottom-20);}}
 /* moment along the span from the support (x in m) for w per m; continuous over the sag rods for the weak axis */
 static double M(double x,double L,int spans,bool cantilever,double w){if(cantilever)return -w*(L-x)*(L-x)/2;double a=L/spans;if(spans==1)return w*x*(L-x)/2;
  if(spans==2){double xs=x<=a?x:L-x;return 3*w*a/8*xs-w*xs*xs/2;}
  double xm=x<=a?x:x>=2*a?L-x:-1;if(xm>=0)return 0.4*w*a*xm-w*xm*xm/2;double u=x-a;return -0.1*w*a*a+0.5*w*a*u-w*u*u/2;}
 static void Moments(Graphics g,RectangleF rc,PurlinDesignInput i,PurlinDesignResult r,PurlinLoadCase c){g.SmoothingMode=SmoothingMode.AntiAlias;bool cant=i.Support=="Cantilever";int spans=i.SagRods+1;float x0=rc.X+60,x1=rc.Right-170;double L=i.Span;Func<double,float> X=x=>x0+(float)(x/L)*(x1-x0);
  using(var f=new Font("Segoe UI",7.5f))using(var fb=new Font("Segoe UI",8f,FontStyle.Bold))using(var ink=new SolidBrush(Ink))using(var beam=new Pen(Ink,3)){
   float by=rc.Y+34;g.DrawLine(beam,x0,by,x1,by);g.DrawString("span L = "+N(L,"0.##")+" m",f,ink,(x0+x1)/2-30,by-28);
   using(var ld=new Pen(Red,1))for(int k=0;k<=20;k++){float x=x0+(x1-x0)*k/20;g.DrawLine(ld,x,by-16,x,by-3);}using(var ld=new Pen(Red,1))g.DrawLine(ld,x0,by-16,x1,by-16);
   if(cant){using(var hb=new HatchBrush(HatchStyle.BackwardDiagonal,Color.Gray,Color.White))g.FillRectangle(hb,x0-10,by-14,10,28);}else foreach(var x in new[]{x0,x1})g.FillPolygon(Brushes.DimGray,new[]{new PointF(x,by+2),new PointF(x-7,by+14),new PointF(x+7,by+14)});
   for(int k=1;k<spans;k++){float x=X(L*k/spans);using(var sr=new Pen(Orange,1.5f){DashStyle=DashStyle.Dash})g.DrawLine(sr,x,by-20,x,by+12);using(var ob=new SolidBrush(Orange))g.DrawString("sag rod",f,ob,x-18,by+14);}
   Action<string,double,bool,float,float,Color,double> diagram=(name,w,weak,top,h,col,peak)=>{var pts=new List<PointF>();double max=0;var vals=new List<double>();for(int k=0;k<=200;k++){double x=L*k/200;double m=M(x,L,weak?spans:1,cant,w);vals.Add(m);max=Math.Max(max,Math.Abs(m));}if(max<=0)max=1;
    float mid=top+h/2;using(var axis=new Pen(Grid,1))g.DrawLine(axis,x0,mid,x1,mid);for(int k=0;k<=200;k++)pts.Add(new PointF(X(L*k/200),mid+(float)(vals[k]/max)*h/2));
    using(var fill=new SolidBrush(Color.FromArgb(50,col))){var poly=new List<PointF>{new PointF(x0,mid)};poly.AddRange(pts);poly.Add(new PointF(x1,mid));g.FillPolygon(fill,poly.ToArray());}using(var p=new Pen(col,1.8f))g.DrawLines(p,pts.ToArray());
    int at=vals.Select((v,k)=>new{v,k}).OrderByDescending(q=>Math.Abs(q.v)).First().k;var pk=pts[at];using(var b=new SolidBrush(col))g.DrawString(N(vals[at],"0.###")+" kN·m",fb,b,pk.X+8,pk.Y+(vals[at]>0?2:-16));
    using(var b=new SolidBrush(col))g.DrawString(name,fb,b,x1+12,mid-14);g.DrawString(weak?"Cy = "+N(r.Cy,"0.#####")+" ("+i.SagRods+" sag rod(s))":"Cx = "+N(r.Cx,"0.###"),f,ink,x1+12,mid+2);g.DrawString("workbook max "+N(peak,"0.###"),f,ink,x1+12,mid+14);};
   float band=(rc.Bottom-by-62)/2;diagram("Mx (strong axis), wx = "+N(c.Normal,"0.###"),c.Normal,false,by+30,band-22,Blue,c.Mx);diagram("My (weak axis), wy = "+N(c.Tangential,"0.###"),c.Tangential,true,by+44+band,band-22,Orange,c.My);
   g.DrawString("Sagging drawn below the axis; the weak-axis moment is continuous over the sag rods (support moment = Cy wy L²).",f,ink,new RectangleF(rc.X+10,rc.Bottom-24,rc.Width-20,24));}}
 static void Utilisation(Graphics g,RectangleF rc,PurlinDesignResult r){g.SmoothingMode=SmoothingMode.AntiAlias;var items=r.Cases.Select(c=>new{N=c.Name+(c.Alternate?" (alternate)":""),V=c.Ratio}).ToList();items.Add(new{N="Live-load deflection",V=r.LiveDeflection/r.LiveLimit});items.Add(new{N="Total-load deflection",V=r.TotalDeflection/r.TotalLimit});
  float label=rc.Width*0.38f,x0=rc.X+label,w=rc.Width-label-60,y=rc.Y+12;double max=Math.Max(1.2,items.Max(q=>q.V));
  using(var f=new Font("Segoe UI",7.5f))using(var ink=new SolidBrush(Ink))using(var ok=new SolidBrush(Green))using(var ng=new SolidBrush(Red)){foreach(var q in items){g.DrawString(q.N,f,ink,new RectangleF(rc.X+8,y,label-12,16));g.FillRectangle(q.V<=1?ok:ng,x0,y+3,(float)(w*Math.Min(q.V,max)/max),10);g.DrawString(N(q.V,"0.00"),f,ink,x0+(float)(w*Math.Min(q.V,max)/max)+4,y);y+=18;}
   float one=x0+(float)(w/max);using(var lim=new Pen(Ink,1.2f){DashStyle=DashStyle.Dash})g.DrawLine(lim,one,rc.Y+6,one,y+2);g.DrawString("1.00",f,ink,one-10,y+2);}}
}
}
