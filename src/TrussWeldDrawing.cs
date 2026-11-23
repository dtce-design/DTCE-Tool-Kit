using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;
namespace BeamScheduleFormatter {
 // A dimension reference, deliberately not a fabrication/detailing model.
 sealed class TrussWeldDrawing : Control {
  readonly IDictionary<string,Control> inputs;
  readonly Dictionary<string,RectangleF> targets=new Dictionary<string,RectangleF>();
  string active="LL1";
  public TrussWeldDrawing(IDictionary<string,Control> values,bool live=true){
   inputs=values;DoubleBuffered=true;Height=420;Width=1000;Tag="FullWidth";if(!live)return;
   AccessibleName="Interactive weld length reference";AccessibleDescription="Click a labelled weld to focus its matching length input.";
   foreach(string key in new[]{"LT","LL1","LL2","Weld","Family","Connection","Profile","GussetThickness"}){
    string selected=key;inputs[key].TextChanged+=(s,e)=>Invalidate();
    inputs[key].Enter+=(s,e)=>{active=selected;Invalidate();};
   }
   MouseMove+=(s,e)=>{Cursor=Hit(e.Location)==null?Cursors.Default:Cursors.Hand;};
   MouseClick+=(s,e)=>{string key=Hit(e.Location);if(key==null)return;active=key;inputs[key].Focus();var text=inputs[key] as TextBox;if(text!=null)text.SelectAll();Invalidate();};
  }
  string Hit(Point p){foreach(var pair in targets)if(pair.Value.Contains(p))return pair.Key;return null;}
  double Read(string key){double value;return double.TryParse(inputs[key].Text,NumberStyles.Float,CultureInfo.InvariantCulture,out value)&&!double.IsNaN(value)&&!double.IsInfinity(value)&&value>=0?value:double.NaN;}
  string Value(string key){double value=Read(key);return double.IsNaN(value)?"enter valid mm":value.ToString("0.###",CultureInfo.InvariantCulture)+" mm";}
  Color Ink {get{return ForeColor.GetBrightness()>.65f?Color.Gainsboro:Color.FromArgb(40,52,66);}}
  protected override void OnPaint(PaintEventArgs e){/* drawings stay paper-white with dark ink in dark mode (owner, 21 November 2026: "pangit ng dark mode") */if(DesktopAppearance.Palette.Dark)e.Graphics.Clear(Color.White);using(new ReportPaperScope())PaintPaper(e);}
 void PaintPaper(PaintEventArgs e){
   base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;targets.Clear();
   using(var font=new Font("Segoe UI",9))using(var title=new Font("Segoe UI",10,FontStyle.Bold)){
    /* the slotted HSS / pipe end has its own detail (owner, 23 November 2026: "syempre yung detail ng hss or circular din magiiba") */if(inputs["Family"].Text.StartsWith("HSS")){PaintHss(g,font,title);return;}
    DrawText(g,"WELD LENGTH REFERENCE  |  "+inputs["Family"].Text,title,Ink,12,8);
    DrawText(g,"Click a weld label or segment to edit it. Dimensions update as you type.",font,Ink,12,33);
    bool welded=inputs["Connection"].Text=="Weld";
    if(!welded){DrawText(g,"Bolted connection selected: the weld lengths below are inactive in the calculation.",font,Color.DarkOrange,12,55);}
    float left=65,right=Math.Max(230,Width-260),top=125,bottom=265;
    double a=Read("LL1"),b=Read("LL2"),t=Read("LT"),max=Math.Max(double.IsNaN(a)?0:a,double.IsNaN(b)?0:b);
    if(max<=0)max=1;
    using(var fill=new SolidBrush(Color.FromArgb(35,116,139,160)))g.FillRectangle(fill,left-20,top-20,right-left+65,bottom-top+40);
    using(var outline=new Pen(Color.SlateGray,1)){g.DrawRectangle(outline,left-20,top-20,right-left+65,bottom-top+40);g.DrawLine(outline,left-32,(top+bottom)/2,right+60,(top+bottom)/2);}
    DrawText(g,"Member over gusset — plan schematic",font,Ink,left,82);
    float end1=left+(float)((double.IsNaN(a)?0:a)/max)*(right-left),end2=left+(float)((double.IsNaN(b)?0:b)/max)*(right-left);
    Segment(g,"LL1",new PointF(left,top),new PointF(end1,top),!double.IsNaN(a)&&a>0,welded);
    Segment(g,"LL2",new PointF(left,bottom),new PointF(end2,bottom),!double.IsNaN(b)&&b>0,welded);
    Segment(g,"LT",new PointF(left,top),new PointF(left,bottom),!double.IsNaN(t)&&t>0,welded);
    Label(g,font,"LL1",new RectangleF(left+20,top-28,Math.Max(130,right-left-20),24));
    Label(g,font,"LL2",new RectangleF(left+20,bottom+12,Math.Max(130,right-left-20),24));
    Label(g,font,"LT",new RectangleF(left+12,(top+bottom)/2-16,190,26));
    float cross=right+80,cy=205;
    using(var plate=new Pen(Color.SlateGray,3)){g.DrawLine(plate,cross-5,cy,cross+85,cy);g.DrawLine(plate,cross,cy,cross,cy-70);}
    using(var fill=new SolidBrush(active=="Weld"?Color.FromArgb(29,99,177):Color.SteelBlue))g.FillPolygon(fill,new[]{new PointF(cross,cy),new PointF(cross+34,cy),new PointF(cross,cy-34)});
    Label(g,font,"Weld",new RectangleF(cross-12,cy+15,170,52));
    DrawText(g,"Fillet cross-section\nw = equal leg size",font,Ink,cross-12,cy-105);
    string total=double.IsNaN(a+b+t)?"Enter valid nonnegative lengths.":"Entered total = LT + LL1 + LL2 = "+(a+b+t).ToString("0.###",CultureInfo.InvariantCulture)+" mm per member";
    DrawText(g,total,title,Ink,12,325);
    DrawText(g,"0 mm = no weld on that segment (dashed). Sketch proportions are illustrative; LT is not inferred from section width.\nDouble angles: the calculator applies multiplicity once. Effective-length reductions remain in the calculation.\nReference only: weld location, access, eccentricity and fabrication detail must be checked for the actual joint.",font,Ink,12,353);
   }
  }
  /* report figure: drawn at 1400 px and scaled into the page */
  internal static void Render(IDictionary<string,Control> values,Graphics g,RectangleF rc){if(rc.Width<=1||rc.Height<=1)return;int w=1400,h=Math.Max(200,(int)(w*rc.Height/rc.Width));
   using(var d=new TrussWeldDrawing(values,false){Width=w,Height=h})using(var bmp=new Bitmap(w,h)){using(var bg=Graphics.FromImage(bmp)){bg.Clear(Color.White);d.PaintPaper(new PaintEventArgs(bg,new Rectangle(0,0,w,h)));}var st=g.Save();g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(bmp,rc);g.Restore(st);}}
  /* slotted HSS / pipe over the gusset: plan with the slot and the weld lines at the two walls, and the cross-section with the plate
     through the slot and the four fillet welds */
  void PaintHss(Graphics g,Font font,Font title){
   bool pipe=inputs["Family"].Text.Contains("circle");var p=SteelCatalog.Find(inputs.ContainsKey("Profile")?inputs["Profile"].Text:"");
   DrawText(g,"SLOTTED "+(pipe?"PIPE":"HSS")+" WELD DETAIL  |  "+(p!=null?p.Name:inputs["Family"].Text),title,Ink,12,8);
   DrawText(g,"Click a weld label or line to edit it. The gusset passes through slots in two opposite walls; LL1 / LL2 are the welds along the slot at each wall.",font,Ink,12,33);
   bool welded=inputs["Connection"].Text=="Weld";
   double D=p!=null?p.H:150,B=p!=null?(pipe?p.H:p.B):150,T=p!=null?p.T:5,a=Read("LL1"),b=Read("LL2"),lt=Read("LT"),tg=Read("GussetThickness");if(double.IsNaN(tg)||tg<=0)tg=10;
   double Lw=Math.Max(double.IsNaN(a)?0:a,double.IsNaN(b)?0:b);if(Lw<=0)Lw=100;
   /* plan: the gusset from the left, the member from the right, its slotted end over the plate */
   float left=40,top=95,availW=Math.Max(320,Width*0.6f),availH=Height-190;double spanX=Lw*2.2+200,spanY=Math.Max(D,B)+120;float s=(float)Math.Min(availW/spanX,availH/spanY);
   Func<double,double,PointF> P=(x,y)=>new PointF(left+(float)(x*s),top+(float)(y*s));double cy=(Math.Max(D,B)+120)/2,x0=150;/* x0 = member end */
   using(var gb=new SolidBrush(Color.FromArgb(228,228,222)))using(var mb=new SolidBrush(Color.FromArgb(200,214,230)))using(var pen=new Pen(Color.FromArgb(40,52,66),1.2f)){
    var plate=new RectangleF(P(0,cy-D*0.25-40).X,P(0,cy-D*0.25-40).Y,(float)((x0+Lw+30)*s),(float)((D*0.5+80)*s));g.FillRectangle(gb,plate);g.DrawRectangle(pen,plate.X,plate.Y,plate.Width,plate.Height);
    var mem=new RectangleF(P(x0,cy-D/2).X,P(x0,cy-D/2).Y,(float)((Lw*2.2+40)*s),(float)(D*s));using(var m2=new SolidBrush(Color.FromArgb(170,200,214,230)))g.FillRectangle(m2,mem);g.DrawRectangle(pen,mem.X,mem.Y,mem.Width,mem.Height);
    using(var hidden=new Pen(Color.FromArgb(40,52,66),1){DashStyle=DashStyle.Dash}){g.DrawLine(hidden,P(x0,cy-tg/2),P(x0+Lw+10,cy-tg/2));g.DrawLine(hidden,P(x0,cy+tg/2),P(x0+Lw+10,cy+tg/2));g.DrawLine(hidden,P(x0+Lw+10,cy-tg/2),P(x0+Lw+10,cy+tg/2));}
    DrawText(g,"gusset",font,Ink,plate.X+6,plate.Y+4);DrawText(g,(pipe?"pipe":"HSS")+" member, slot "+Math.Round(tg+2)+" mm (gusset + 2)",font,Ink,mem.X+(float)((Lw+40)*s),mem.Y+4);}
   Segment(g,"LL1",P(x0,cy-tg/2-6),P(x0+(double.IsNaN(a)?0:a),cy-tg/2-6),!double.IsNaN(a)&&a>0,welded);Label(g,font,"LL1",new RectangleF(P(x0,0).X,P(0,cy-D/2).Y-26,220,22));
   Segment(g,"LL2",P(x0,cy+tg/2+6),P(x0+(double.IsNaN(b)?0:b),cy+tg/2+6),!double.IsNaN(b)&&b>0,welded);Label(g,font,"LL2",new RectangleF(P(x0,0).X,P(0,cy+D/2).Y+6,220,22));
   Segment(g,"LT",P(x0-4,cy-D/2),P(x0-4,cy+D/2),!double.IsNaN(lt)&&lt>0,welded);Label(g,font,"LT",new RectangleF(P(x0,0).X-150,P(0,cy).Y-10,140,22));
   /* section through the slot */
   float sx=Math.Max(P(x0+Lw*2.2+60,0).X+30,Width*0.66f),sw=Width-sx-20;if(sw>150){float ss=(float)Math.Min(sw/(B+80),(Height-200)/(D+80));float cx=sx+sw/2,cyy=top+30+(float)((D+80)*ss/2);
    DrawText(g,"SECTION THROUGH THE SLOT",title,Ink,sx,top-20);
    using(var mb=new SolidBrush(Color.FromArgb(200,214,230)))using(var gb=new SolidBrush(Color.FromArgb(228,228,222)))using(var pen=new Pen(Color.FromArgb(40,52,66),1.2f))using(var wb=new SolidBrush(active=="Weld"?Color.FromArgb(29,99,177):Color.SteelBlue)){
     var outer=new RectangleF(cx-(float)(B*ss/2),cyy-(float)(D*ss/2),(float)(B*ss),(float)(D*ss));var inner=RectangleF.Inflate(outer,-(float)(T*ss),-(float)(T*ss));
     using(var path=new GraphicsPath(FillMode.Alternate)){if(pipe){path.AddEllipse(outer);path.AddEllipse(inner);}else{path.AddRectangle(outer);path.AddRectangle(inner);}g.FillPath(mb,path);g.DrawPath(pen,path);}
     var pl=new RectangleF(cx-(float)((B/2+30)*ss),cyy-(float)(tg*ss/2),(float)((B+60)*ss),(float)(tg*ss));g.FillRectangle(gb,pl);g.DrawRectangle(pen,pl.X,pl.Y,pl.Width,pl.Height);
     float w=Math.Max(5,(float)(Read("Weld")*ss));if(float.IsNaN(w))w=5;foreach(int sxn in new[]{-1,1})foreach(int syn in new[]{-1,1}){float wx=cx+sxn*(float)((B/2)*ss),wy=cyy+syn*(float)(tg*ss/2);g.FillPolygon(wb,new[]{new PointF(wx,wy),new PointF(wx+sxn*w,wy),new PointF(wx,wy+syn*w)});}
     DrawText(g,"4 fillet welds: the plate both faces, at both walls\nLL1 at one wall, LL2 at the other",font,Ink,sx,cyy+(float)(D*ss/2)+16);Label(g,font,"Weld",new RectangleF(sx,cyy+(float)(D*ss/2)+52,200,22));}}
   string total=double.IsNaN(a+b+lt)?"Enter valid nonnegative lengths.":"Entered total = LT + LL1 + LL2 = "+(a+b+lt).ToString("0.###",CultureInfo.InvariantCulture)+" mm";
   DrawText(g,total,title,Ink,12,Height-78);
   DrawText(g,"No block-shear path for a gusset slotted into the member: check the "+(pipe?"pipe":"HSS")+" wall and gusset shear along the welds and the net section at the slot end (shear lag U) separately.",font,Ink,12,Height-52);
  }
  void Segment(Graphics g,string key,PointF p,PointF q,bool present,bool enabled){
   Color color=active==key?Color.FromArgb(25,102,192):Color.FromArgb(75,124,142);if(!enabled)color=Color.Gray;
   // Keep a missing segment visible as a reference instead of silently drawing a weld.
   if(Math.Abs(p.X-q.X)+Math.Abs(p.Y-q.Y)<1)q=new PointF(p.X+35,p.Y);
   using(var pen=new Pen(color,active==key?7:4)){if(!present||!enabled)pen.DashStyle=DashStyle.Dash;g.DrawLine(pen,p,q);}
   targets[key]=new RectangleF(Math.Min(p.X,q.X)-9,Math.Min(p.Y,q.Y)-9,Math.Abs(p.X-q.X)+18,Math.Abs(p.Y-q.Y)+18);
  }
  void Label(Graphics g,Font font,string key,RectangleF box){
   string caption=key=="Weld"?"w = "+Value(key):key+" = "+Value(key);
   // Label and line are both clickable; preserve their union for convenient selection.
   RectangleF old;if(targets.TryGetValue(key,out old))targets[key]=RectangleF.Union(old,box);else targets[key]=box;
   using(var brush=new SolidBrush(active==key?Color.FromArgb(25,102,192):Ink))g.DrawString(caption,font,brush,box);
  }
  static void DrawText(Graphics g,string text,Font font,Color color,float x,float y){using(var brush=new SolidBrush(color))g.DrawString(text,font,brush,x,y);}
 }
}
