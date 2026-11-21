using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* Section across the truss and the anchor bolt detail (owner, 19 November 2026: "Imodel mo din naman yung itsura ng angle bar",
   "yung dimension itsura ng anchor bolt with the embedment depth everything"). Both are drawn at a standard scale for the screen
   or for paper and carry their dimensions. */
static class TrussSupportDetail {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 static string F(double v){return v.ToString("0",Inv);}
 static int Ratio(double fit,double unitsPerMm){foreach(int n in new[]{1,2,5,10,15,20,25,30,40,50,75,100})if(unitsPerMm/n<=fit)return n;return 200;}
 sealed class Ctx {public Graphics G;public Font Font,Bold;public float K,Ox,Oy;public PointF P(double x,double y){return new PointF(Ox+(float)(x*K),Oy-(float)(y*K));}}
 static void DimH(Ctx c,double x0,double x1,double y,string text,float off){var a=c.P(x0,y);var b=c.P(x1,y);a.Y+=off;b.Y+=off;using(var p=new Pen(PowTheme.Muted,.8f)){c.G.DrawLine(p,a,b);c.G.DrawLine(p,a.X,a.Y-3,a.X,a.Y+3);c.G.DrawLine(p,b.X,b.Y-3,b.X,b.Y+3);}var sz=c.G.MeasureString(text,c.Font);using(var br=new SolidBrush(PowTheme.Ink))c.G.DrawString(text,c.Font,br,(a.X+b.X)/2-sz.Width/2,a.Y-sz.Height-(off<0?0:-sz.Height-1)+(off<0?0:0));}
 static void DimV(Ctx c,double x,double y0,double y1,string text,float off,bool textLeft=true){var a=c.P(x,y0);var b=c.P(x,y1);a.X+=off;b.X+=off;using(var p=new Pen(PowTheme.Muted,.8f)){c.G.DrawLine(p,a,b);c.G.DrawLine(p,a.X-3,a.Y,a.X+3,a.Y);c.G.DrawLine(p,b.X-3,b.Y,b.X+3,b.Y);}var sz=c.G.MeasureString(text,c.Font);using(var br=new SolidBrush(PowTheme.Ink))c.G.DrawString(text,c.Font,br,textLeft?a.X-sz.Width-3:a.X+4,(a.Y+b.Y)/2-sz.Height/2);}
 static void Box(Ctx c,double x0,double y0,double x1,double y1,Brush fill,Pen edge){var a=c.P(Math.Min(x0,x1),Math.Max(y0,y1));float w=(float)(Math.Abs(x1-x0)*c.K),h=(float)(Math.Abs(y1-y0)*c.K);if(fill!=null)c.G.FillRectangle(fill,a.X,a.Y,Math.Max(.6f,w),Math.Max(.6f,h));if(edge!=null)c.G.DrawRectangle(edge,a.X,a.Y,Math.Max(.6f,w),Math.Max(.6f,h));}
 /* unitsPerPaperMm: drawing units (pixels or points) per millimetre of the screen or paper */
 public static void Draw(Graphics g,RectangleF area,TrussSupportInput i,double gussetH,double unitsPerPaperMm,float fontPt,string scaleNote){
  using(var font=new Font("Segoe UI",fontPt,FontStyle.Regular,GraphicsUnit.Point))using(var bold=new Font("Segoe UI",fontPt+1,FontStyle.Bold,GraphicsUnit.Point)){
   var left=new RectangleF(area.Left,area.Top,area.Width*.56f,area.Height);var right=new RectangleF(area.Left+area.Width*.56f,area.Top,area.Width*.44f,area.Height);
   using(var line=new Pen(PowTheme.Line))g.DrawLine(line,right.Left,area.Top+6,right.Left,area.Bottom-6);
   Section(g,left,i,gussetH,unitsPerPaperMm,font,bold,scaleNote);Bolt(g,right,i,unitsPerPaperMm,font,bold,scaleNote);}}
 static void Section(Graphics g,RectangleF area,TrussSupportInput i,double gussetH,double upm,Font font,Font bold,string note){
  double grout=i.GroutPad?25:0,t=i.PlateT,top=grout+t;double chordH=i.ChordOnPlate&&i.Chord!=null?i.Chord.H:0;double above=top+Math.Max(chordH,0)+Math.Min(gussetH,Math.Max(80,chordH+60));double below=i.Hef+140;
  double wid=Math.Max(i.SupportB,i.PlateB)+360,hei=above+below+110;double fit=Math.Min((area.Width-20)/wid,(area.Height-50)/hei);int n=Ratio(fit,upm);float k=(float)(upm/n);
  var c=new Ctx{G=g,Font=font,Bold=bold,K=k,Ox=area.Left+area.Width*.45f,Oy=area.Top+44+(float)(above*k)+10};
  using(var ink=new SolidBrush(PowTheme.Ink)){g.DrawString("SECTION A-A",bold,ink,area.Left+6,area.Top+4);using(var m=new SolidBrush(PowTheme.Muted))g.DrawString("across the truss, at the anchors - scale 1:"+n+" "+note,font,m,area.Left+6,area.Top+18);}
  /* concrete with a break line */
  using(var conc=new HatchBrush(HatchStyle.Percent10,Color.Gray,Color.FromArgb(242,242,238)))using(var edge=new Pen(PowTheme.Muted,.8f)){Box(c,-i.SupportB/2,0,i.SupportB/2,-below+20,conc,null);var a=c.P(-i.SupportB/2,0);var b=c.P(i.SupportB/2,0);g.DrawLine(edge,a,b);g.DrawLine(edge,a,c.P(-i.SupportB/2,-below+20));g.DrawLine(edge,b,c.P(i.SupportB/2,-below+20));var z0=c.P(-i.SupportB/2,-below+20);var z1=c.P(i.SupportB/2,-below+20);float mid=(z0.X+z1.X)/2;g.DrawLines(edge,new[]{z0,new PointF(mid-6,z0.Y),new PointF(mid-2,z0.Y-6),new PointF(mid+2,z0.Y+6),new PointF(mid+6,z0.Y),z1});}
  using(var steel=new SolidBrush(Color.FromArgb(120,140,170)))using(var dark=new SolidBrush(Color.FromArgb(70,85,110)))using(var edge=new Pen(PowTheme.Ink,.8f)){
   if(grout>0)using(var gr=new HatchBrush(HatchStyle.DottedGrid,Color.Gray,Color.FromArgb(225,225,220)))Box(c,-i.PlateB/2-15,0,i.PlateB/2+15,grout,gr,edge);
   Box(c,-i.PlateB/2,grout,i.PlateB/2,top,steel,edge);
   /* chord angles: horizontal legs on the plate, vertical legs up, back to back on the gusset */
   if(i.ChordOnPlate&&i.Chord!=null){double B=i.Chord.B,H=i.Chord.H,tl=i.Chord.T;foreach(int side in i.ChordAngles==2?new[]{-1,1}:new[]{1}){double heel=i.ChordAngles==2?side*i.GussetT/2:0;double toe=heel+side*B;var pts=new[]{c.P(heel,top),c.P(toe,top),c.P(toe,top+tl),c.P(heel+side*tl,top+tl),c.P(heel+side*tl,top+H),c.P(heel,top+H)};g.FillPolygon(dark,pts);g.DrawPolygon(edge,pts);
     double w=i.ChordWeld;var tri=new[]{c.P(toe,top),c.P(toe+side*w,top),c.P(toe,top+w)};using(var weld=new SolidBrush(Color.FromArgb(200,60,60)))g.FillPolygon(weld,tri);}
    double gTop=top+Math.Min(gussetH,H+60);Box(c,-i.GussetT/2,top,i.GussetT/2,gTop,steel,edge);
    using(var ink=new SolidBrush(PowTheme.Ink)){string lab=(i.ChordAngles==2?"2":"1")+i.Chord.Name.Split(' ')[0]+" bottom chord";var p=c.P((i.ChordAngles==2?i.GussetT/2:0)+tl,top+H);var lp=new PointF(p.X+40,p.Y-22);using(var lead=new Pen(PowTheme.Muted,.6f))g.DrawLine(lead,p,lp);g.DrawString(lab,font,ink,lp.X+2,lp.Y-6);var q=c.P(i.ChordAngles==2?i.GussetT/2+B:B,top);var lq=new PointF(q.X+30,q.Y+34);using(var lead=new Pen(PowTheme.Muted,.6f))g.DrawLine(lead,q,lq);g.DrawString(F(i.ChordWeld)+" mm fillet weld (typ.)",font,ink,lq.X+2,lq.Y-2);}
    DimH(c,-i.Footprint/2,i.Footprint/2,top+H,F(i.Footprint),-10);}
   else{double gTop=top+Math.Min(gussetH,160);Box(c,-i.GussetT/2,top,i.GussetT/2,gTop,steel,edge);}
   /* anchors */
   double d=i.BoltDia,nut=AnchorBolts.NutFlats(d),wash=Math.Max(3,.15*d);foreach(int side in new[]{-1,1}){double x=side*i.BoltS2/2;var shank=new[]{c.P(x-d/2,top+wash+d+6),c.P(x+d/2,top+wash+d+6),c.P(x+d/2,-i.Hef),c.P(x-d/2,-i.Hef)};using(var bolt=new SolidBrush(Color.FromArgb(55,55,60)))g.FillPolygon(bolt,shank);
    if(i.Headed)Box(c,x-nut/2,-i.Hef-d,x+nut/2,-i.Hef,Brushes.DimGray,edge);else{double hx=x-side*i.HookEh;Box(c,Math.Min(x+side*d/2,hx),-i.Hef-d,Math.Max(x+side*d/2,hx),-i.Hef,Brushes.DimGray,null);}
    Box(c,x-1.1*nut/2*1.4,top,x+1.1*nut/2*1.4,top+wash,Brushes.Silver,edge);Box(c,x-nut/2,top+wash,x+nut/2,top+wash+d,Brushes.Gray,edge);}
   DimH(c,-i.BoltS2/2,i.BoltS2/2,top+Math.Max(chordH,0),"anchor gauge "+F(i.BoltS2),-34);DimH(c,-i.PlateB/2,i.PlateB/2,0,"plate "+F(i.PlateB),18);
   DimV(c,-i.SupportB/2,0,-i.Hef,"hef "+F(i.Hef),-12);DimV(c,i.SupportB/2,grout,top,"tp "+F(t),22,false);if(grout>0)DimV(c,i.SupportB/2,0,grout,"grout "+F(grout),56,false);
   using(var ink=new SolidBrush(PowTheme.Muted)){var p=c.P(-i.SupportB/2,-below+20);g.DrawString("RC support "+F(i.SupportB)+" mm wide, fc' "+i.Fc.ToString("0.#",Inv)+" MPa",font,ink,p.X,p.Y+4);}}}
 static void Bolt(Graphics g,RectangleF area,TrussSupportInput i,double upm,Font font,Font bold,string note){
  double d=i.BoltDia,grout=i.GroutPad?25:0,t=i.PlateT,wash=Math.Max(3,.15*d),nut=AnchorBolts.NutFlats(d),proj=AnchorBolts.Projection(d,t,i.GroutPad);double total=proj+i.Hef+d,hook=i.Headed?0:i.HookEh;double thread=i.ThreadLength>0?i.ThreadLength:AnchorBolts.ThreadNeeded(d,t);
  double wid=Math.Max(hook,nut*2)+420,hei=total+150;double fit=Math.Min((area.Width-20)/wid,(area.Height-60)/hei);int n=Ratio(fit,upm);float k=(float)(upm/n);
  var c=new Ctx{G=g,Font=font,Bold=bold,K=k,Ox=area.Left+area.Width*.52f,Oy=area.Top+44+(float)(proj*k)+6};
  using(var ink=new SolidBrush(PowTheme.Ink)){g.DrawString("ANCHOR BOLT DETAIL",bold,ink,area.Left+8,area.Top+4);using(var m=new SolidBrush(PowTheme.Muted))g.DrawString("scale 1:"+n+" "+note,font,m,area.Left+8,area.Top+18);}
  using(var edge=new Pen(PowTheme.Ink,.8f))using(var steel=new SolidBrush(Color.FromArgb(120,140,170))){
   using(var conc=new HatchBrush(HatchStyle.Percent10,Color.Gray,Color.FromArgb(242,242,238)))Box(c,-90,0,90+hook,-(i.Hef+d+50),conc,null);using(var pen=new Pen(PowTheme.Muted,.8f))g.DrawLine(pen,c.P(-90,0),c.P(90+hook,0));
   if(grout>0)using(var gr=new HatchBrush(HatchStyle.DottedGrid,Color.Gray,Color.FromArgb(225,225,220)))Box(c,-80,0,80,grout,gr,edge);Box(c,-70,grout,70,grout+t,steel,edge);
   /* bolt: threaded top (hatched), shank, hook */
   using(var bolt=new SolidBrush(Color.FromArgb(55,55,60)))Box(c,-d/2,proj,d/2,-i.Hef,bolt,null);using(var thr=new HatchBrush(HatchStyle.DarkDownwardDiagonal,Color.White,Color.FromArgb(55,55,60)))Box(c,-d/2,proj,d/2,proj-thread,thr,edge);
   if(i.Headed)Box(c,-nut/2,-i.Hef-d,nut/2,-i.Hef,Brushes.DimGray,edge);else using(var bolt=new SolidBrush(Color.FromArgb(55,55,60)))Box(c,-d/2,-i.Hef-d,hook,-i.Hef,bolt,edge);
   Box(c,-1.4*nut/2,grout+t,1.4*nut/2,grout+t+wash,Brushes.Silver,edge);Box(c,-nut/2,grout+t+wash,nut/2,grout+t+wash+d,Brushes.Gray,edge);}
  /* dimensions */
  DimV(c,-90,proj,0,"projection "+F(proj),-4);DimV(c,-90,0,-i.Hef,"hef "+F(i.Hef),-4);DimV(c,-90,proj,-i.Hef-d,"B "+F(total),-70);DimV(c,nut,proj,proj-thread,"thread "+F(thread),6,false);
  if(!i.Headed)DimH(c,-d/2,hook,-i.Hef-d,"hook eh "+F(hook),24);
  using(var ink=new SolidBrush(PowTheme.Ink)){string spec=(i.BoltName!=""?i.BoltName:AnchorBolts.Short(d)+" anchor")+(i.Headed?", headed":", L-bolt")+"\r\nØ "+d.ToString("0.0",Inv)+" mm, Ase "+AnchorBolts.StressArea(d).ToString("0.0",Inv)+" mm²\r\nwasher + heavy hex nut, plate "+F(t)+(grout>0?", grout "+F(grout):"")+" mm";g.DrawString(spec,font,ink,new RectangleF(area.Left+8,area.Bottom-44,area.Width-16,44));}}
}
class TrussSupportDetailView:Control {
 TrussSupportInput input;double gussetH=250;
 public TrussSupportDetailView(){DoubleBuffered=true;ResizeRedraw=true;Width=1000;Height=460;Tag="FullWidth";BackColor=Color.White;}
 public void Show(TrussSupportInput i,double h){input=i;gussetH=h;Invalidate();}
 protected override void OnPaint(PaintEventArgs e){/* drawings stay paper-white with dark ink in dark mode (owner, 21 November 2026: "pangit ng dark mode") */if(DesktopAppearance.Palette.Dark)e.Graphics.Clear(Color.White);using(new ReportPaperScope())PaintPaper(e);}
 void PaintPaper(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;if(input==null){using(var b=new SolidBrush(PowTheme.Muted))e.Graphics.DrawString("Enter the plate, anchor and chord sizes to see the section and the anchor bolt detail.",Font,b,12,12);return;}
  try{TrussSupportDetail.Draw(e.Graphics,new RectangleF(0,0,Width,Height),input,gussetH,e.Graphics.DpiX/25.4,8.5f,"(on screen)");}catch(Exception ex){using(var b=new SolidBrush(Color.Firebrick))e.Graphics.DrawString(ex.Message,Font,b,12,12);}}
}
}
