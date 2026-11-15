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
  public TrussWeldDrawing(IDictionary<string,Control> values){
   inputs=values;DoubleBuffered=true;Height=420;Width=1000;Tag="FullWidth";
   AccessibleName="Interactive weld length reference";AccessibleDescription="Click a labelled weld to focus its matching length input.";
   foreach(string key in new[]{"LT","LL1","LL2","Weld","Family","Connection"}){
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
  protected override void OnPaint(PaintEventArgs e){
   base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;targets.Clear();
   using(var font=new Font("Segoe UI",9))using(var title=new Font("Segoe UI",10,FontStyle.Bold)){
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
