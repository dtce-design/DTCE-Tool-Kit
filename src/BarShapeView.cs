using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* Bar shapes of the bar bending schedule (owner, 19 November 2026: "Ganon din sana sa BBS may visualization", "Iscale naman yung
   drawing"): a small sketch in each schedule row and a to-scale drawing of the selected bar with its A / B / C dimensions. */
static class BarShapes {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 /* outline points in mm (y up) for the shape of a piece */
 internal static PointF[][] Outline(CutPiece p){double a=p.DimA,b=p.DimB,c=p.DimC;if(p.DrawnStraight>0){if(p.ShapeCode=="HOOK1")a=p.DrawnStraight;else if(p.ShapeCode=="HOOK2")b=p.DrawnStraight;}switch(p.ShapeCode){
  case "HOOK1":return new[]{new[]{new PointF(0,(float)b),new PointF(0,0),new PointF((float)a,0)}};
  case "HOOK2":return new[]{new[]{new PointF(0,(float)a),new PointF(0,0),new PointF((float)b,0),new PointF((float)b,(float)c)}};
  case "HOOP":{float w=(float)a,h=(float)b,t=(float)(c*.7071);return new[]{new[]{new PointF(t,h-t),new PointF(0,h),new PointF(w,h),new PointF(w,0),new PointF(0,0),new PointF(0,h),new PointF(t,h-t+0)},new[]{new PointF(0,h),new PointF(t*1.0f,h-t*1.0f)}};}
  case "CROSSTIE":{float h=(float)a,t=(float)(b*.7071);return new[]{new[]{new PointF(t,h-t),new PointF(0,h),new PointF(0,0),new PointF((float)c,0)}};}
  case "CRANK":{/* lap A at the offset, crank B, straight D; the offset is drawn larger so the bend reads */float run=(float)(6*Math.Max(c,1)),off=(float)Math.Max(c,0.14*(a+(p.DrawnStraight>0?p.DrawnStraight:p.DimD)+run)),d=(float)(p.DrawnStraight>0?p.DrawnStraight:p.DimD);
   return new[]{new[]{new PointF(0,off),new PointF((float)a,off),new PointF((float)a+run,0),new PointF((float)a+run+Math.Max(d,1),0)}};}
  case "CRANKHOOK":{/* as CRANK, with the 90° hook leg E at the far end */float run=(float)(6*Math.Max(c,1)),d=(float)(p.DrawnStraight>0?p.DrawnStraight:p.DimD),off=(float)Math.Max(c,0.14*(a+d+run)),x=(float)a+run+Math.Max(d,1);
   return new[]{new[]{new PointF(0,off),new PointF((float)a,off),new PointF((float)a+run,0),new PointF(x,0),new PointF(x,(float)Math.Max(p.DimE,1))}};}
  default:return new[]{new[]{new PointF(0,0),new PointF((float)Math.Max(a,1),0)}};}}
 /* draws the outline fitted in the box; scaled = the same mm-to-pixel scale both ways and dimension labels */
 internal static void Draw(Graphics g,RectangleF box,CutPiece p,bool labels,Color color,Font font,bool plainLabels=false,float lineWidth=0,bool lettersOnly=false){var lines=Outline(p);var all=lines.SelectMany(l=>l).ToList();float minX=all.Min(q=>q.X),maxX=all.Max(q=>q.X),minY=all.Min(q=>q.Y),maxY=all.Max(q=>q.Y);
  float w=Math.Max(1,maxX-minX),h=Math.Max(1,maxY-minY);float rx=labels?Math.Min(70,box.Width*.45f):8,ry=labels?Math.Min(50,box.Height*.45f):8;float s=Math.Min((box.Width-rx)/w,(box.Height-ry)/h);if(p.ShapeCode=="STRAIGHT")s=(box.Width-rx)/w;float ox=box.Left+(box.Width-w*s)/2-minX*s,oy=box.Top+(box.Height+h*s)/2+minY*s;
  Func<PointF,PointF> map=q=>new PointF(ox+q.X*s,oy-q.Y*s);g.SmoothingMode=SmoothingMode.AntiAlias;using(var pen=new Pen(color,lineWidth>0?lineWidth:labels?3f:2f){LineJoin=LineJoin.Round,StartCap=LineCap.Round,EndCap=LineCap.Round})foreach(var l in lines)g.DrawLines(pen,l.Select(map).ToArray());
  if(!labels)return;using(var b=new SolidBrush(PowTheme.Ink)){Action<string,double,PointF,int,int> label=(name,v,at,dx,dy)=>{if(v<=0)return;string t=lettersOnly?name:(plainLabels?"":name+" = ")+v.ToString("0",Inv);var sz=g.MeasureString(t,font);float cx=at.X+dx*(sz.Width/2+6),cy=at.Y+dy*(sz.Height/2+3);float x=Math.Max(box.Left,Math.Min(box.Right-sz.Width,cx-sz.Width/2)),y=Math.Max(box.Top,Math.Min(box.Bottom-sz.Height,cy-sz.Height/2));g.DrawString(t,font,b,x,y);};
   double sa=p.ShapeCode=="HOOK1"&&p.DrawnStraight>0?p.DrawnStraight:p.DimA,sb=p.ShapeCode=="HOOK2"&&p.DrawnStraight>0?p.DrawnStraight:p.DimB;
   switch(p.ShapeCode){case "HOOK1":label("A",p.DimA,map(new PointF((float)sa/2,0)),0,1);label("B",p.DimB,map(new PointF(0,(float)p.DimB/2)),-1,0);break;
    case "HOOK2":label("A",p.DimA,map(new PointF(0,(float)p.DimA/2)),-1,0);label("B",p.DimB,map(new PointF((float)sb/2,0)),0,1);label("C",p.DimC,map(new PointF((float)sb,(float)p.DimC/2)),1,0);break;
    case "HOOP":label("A",p.DimA,map(new PointF((float)p.DimA/2,0)),0,1);label("B",p.DimB,map(new PointF((float)p.DimA,(float)p.DimB/2)),1,0);label("C",p.DimC,map(new PointF((float)p.DimC,(float)p.DimB)),1,-1);break;
    case "CROSSTIE":label("A",p.DimA,map(new PointF(0,(float)p.DimA/2)),-1,0);label("B",p.DimB,map(new PointF(0,(float)p.DimA)),1,-1);label("C",p.DimC,map(new PointF((float)p.DimC/2,0)),0,1);break;
    case "CRANK":case "CRANKHOOK":{var o=Outline(p)[0];label("A",p.DimA,map(new PointF(o[1].X/2,o[1].Y)),0,-1);label("B",p.DimB,map(new PointF((o[1].X+o[2].X)/2,(o[1].Y+o[2].Y)/2)),0,1);label("C",p.DimC,map(new PointF(0,o[1].Y/2)),-1,0);label("D",p.DimD,map(new PointF((o[2].X+o[3].X)/2,0)),0,1);if(o.Length>4)label("E",p.DimE,map(new PointF(o[4].X,o[4].Y/2)),1,0);break;}
    default:label("A",p.DimA,map(new PointF((float)p.DimA/2,0)),0,1);break;}}}
}
class BarShapeView:Control {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;CutPiece piece;
 public BarShapeView(){DoubleBuffered=true;ResizeRedraw=true;BackColor=Color.White;Dock=DockStyle.Right;Width=330;}
 public void Show(CutPiece p){piece=p;Invalidate();}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;var font=new Font("Segoe UI",8.5f);using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted)){
  using(var line=new Pen(PowTheme.Line))g.DrawLine(line,0,0,0,Height);if(piece==null){g.DrawString("Select a bar to see its shape.",font,muted,12,12);return;}var p=piece;
  g.DrawString(p.BarMark,ModernUi.Strong,ink,12,10);g.DrawString(BeamCutting.ShapeName(p.ShapeCode)+"\r\nG"+p.Grade+" "+p.Diameter+" mm, cut length "+(p.Length*1000).ToString("0",Inv)+" mm\r\n"+p.Pieces+" pcs ("+p.PerLine+" each x "+p.Lines+")  "+p.Kg.ToString("0.00",Inv)+" kg\r\n"+p.Location,font,muted,new RectangleF(12,32,Width-24,80));
  BarShapes.Draw(g,new RectangleF(10,120,Width-20,Height-170),p,true,Color.FromArgb(25,90,160),font);
  g.DrawString("Shape drawn to scale; A, B, C are outside dimensions in mm.",font,muted,new RectangleF(12,Height-44,Width-24,40));}}
}
}
