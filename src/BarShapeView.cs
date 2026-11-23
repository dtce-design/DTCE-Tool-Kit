using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* Bar shapes of the bar bending schedule (owner, 19 November 2026: "Ganon din sana sa BBS may visualization", "Iscale naman yung
   drawing"): a small sketch in each schedule row and a to-scale drawing of the selected bar with its A / B / C dimensions. */
static class BarShapes {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 /* the bar bending sketch (owner, 20 November 2026: "di accurate"): long straight parts are shortened the same way everywhere
    (schedule grid, report, site sheets) so that hooks and cranks keep their real proportions to each other; A, B, C ... give the
    lengths */
 internal static CutPiece Sketch(CutPiece p){var q=new CutPiece{ShapeCode=p.ShapeCode,DimA=p.DimA,DimB=p.DimB,DimC=p.DimC,DimD=p.DimD,DimE=p.DimE,Diameter=p.Diameter,SecB=p.SecB,SecH=p.SecH};
  if(p.ShapeCode.StartsWith("CRANK"))q.DrawnStraight=Math.Min(p.DimD,2*Math.Max(p.DimA,1));
  double hook=Math.Max(p.ShapeCode=="HOOK1"?p.DimB:p.ShapeCode=="HOOK2"?Math.Max(p.DimA,p.DimC):0,1);if(p.ShapeCode=="HOOK1"&&p.DimA>4*hook)q.DrawnStraight=4*hook;if(p.ShapeCode=="HOOK2"&&p.DimB>4*hook)q.DrawnStraight=4*hook;
  if(p.ShapeCode=="STRAIGHT"&&p.DimA<=0)q.DimA=p.Length*1000;return q;}
 /* outline points in mm (y up) for the shape of a piece */
 internal static PointF[][] Outline(CutPiece p){double a=p.DimA,b=p.DimB,c=p.DimC;if(p.DrawnStraight>0){if(p.ShapeCode=="HOOK1")a=p.DrawnStraight;else if(p.ShapeCode=="HOOK2")b=p.DrawnStraight;}switch(p.ShapeCode){
  case "HOOK1":return new[]{new[]{new PointF(0,(float)b),new PointF(0,0),new PointF((float)a,0)}};
  case "HOOK2":return new[]{new[]{new PointF(0,(float)a),new PointF(0,0),new PointF((float)b,0),new PointF((float)b,(float)c)}};
  case "HOOP":{/* owner (20 November 2026, "iisa lang ang hook"): both ends of the hoop end in 135° hooks at the same corner */float w=(float)a,h=(float)b,t=(float)Math.Max(c*.7071,Math.Min(w,h)*.36),gap=(float)Math.Max(Math.Min(w,h)*.07,1);/* longer tails, set further apart, so the two 135° hooks read clearly (owner, 22 November 2026: "pakiaayos yung hook neto") *//* tails drawn long enough to read as 135° hooks into the core (owner, 21 November 2026: "paki fix yung hook ng stirrup") */
   return new[]{new[]{new PointF(t,h-gap-t),new PointF(0,h-gap),new PointF(0,0),new PointF(w,0),new PointF(w,h),new PointF(gap,h),new PointF(gap+t,h-t)}};}
  case "CROSSTIE":{float h=(float)a,t=(float)(b*.7071);return new[]{new[]{new PointF(t,h-t),new PointF(0,h),new PointF(0,0),new PointF((float)c,0)}};}
  case "CRANK":{/* lap A at the offset, crank B, straight D; the crank is drawn larger but keeps a visible slope (owner, 21 November 2026:
     "dapat pinapakita yung slope ng cranked bar") */float d=(float)(p.DrawnStraight>0?p.DrawnStraight:p.DimD),run=(float)Math.Max(6*Math.Max(c,1),0.12*(a+d)),off=(float)Math.Max(c,run*0.4);
   return new[]{new[]{new PointF(0,off),new PointF((float)a,off),new PointF((float)a+run,0),new PointF((float)a+run+Math.Max(d,1),0)}};}
  case "CRANKHOOK":{/* as CRANK, with the 90° hook leg E at the far end */float d=(float)(p.DrawnStraight>0?p.DrawnStraight:p.DimD),run=(float)Math.Max(6*Math.Max(c,1),0.12*(a+d)),off=(float)Math.Max(c,run*0.4),x=(float)a+run+Math.Max(d,1);
   return new[]{new[]{new PointF(0,off),new PointF((float)a,off),new PointF((float)a+run,0),new PointF(x,0),new PointF(x,(float)Math.Max(p.DimE,1))}};}
  default:return new[]{new[]{new PointF(0,0),new PointF((float)Math.Max(a,1),0)}};}}
 /* a bar line with its bends drawn round (radius r in pixels, limited by the straight parts) */
 static void Rounded(Graphics g,Pen pen,PointF[] pts,float r){if(pts.Length<3){g.DrawLines(pen,pts);return;}using(var path=new GraphicsPath()){PointF cur=pts[0];
  for(int i=1;i<pts.Length-1;i++){var P=pts[i];var A=pts[i-1];var C=pts[i+1];float l1=(float)Math.Sqrt((A.X-P.X)*(A.X-P.X)+(A.Y-P.Y)*(A.Y-P.Y)),l2=(float)Math.Sqrt((C.X-P.X)*(C.X-P.X)+(C.Y-P.Y)*(C.Y-P.Y));float k=Math.Min(r,Math.Min(l1,l2)*.45f);
   if(k<.5f||l1<1e-3||l2<1e-3){path.AddLine(cur,P);cur=P;continue;}var p1=new PointF(P.X+(A.X-P.X)/l1*k,P.Y+(A.Y-P.Y)/l1*k);var p2=new PointF(P.X+(C.X-P.X)/l2*k,P.Y+(C.Y-P.Y)/l2*k);path.AddLine(cur,p1);path.AddBezier(p1,P,P,p2);cur=p2;}
  path.AddLine(cur,pts[pts.Length-1]);g.DrawPath(pen,path);}}
 /* draws the outline fitted in the box; scaled = the same mm-to-pixel scale both ways and dimension labels */
 internal static void Draw(Graphics g,RectangleF box,CutPiece p,bool labels,Color color,Font font,bool plainLabels=false,float lineWidth=0,bool lettersOnly=false){var lines=Outline(p);var all=lines.SelectMany(l=>l).ToList();float minX=all.Min(q=>q.X),maxX=all.Max(q=>q.X),minY=all.Min(q=>q.Y),maxY=all.Max(q=>q.Y);
  float w=Math.Max(1,maxX-minX),h=Math.Max(1,maxY-minY);float rx=labels?Math.Min(70,box.Width*.45f):8,ry=labels?Math.Min(50,box.Height*.45f):8;float s=Math.Min((box.Width-rx)/w,(box.Height-ry)/h);if(p.ShapeCode=="STRAIGHT")s=(box.Width-rx)/w;float ox=box.Left+(box.Width-w*s)/2-minX*s,oy=box.Top+(box.Height+h*s)/2+minY*s;
  Func<PointF,PointF> map=q=>new PointF(ox+q.X*s,oy-q.Y*s);g.SmoothingMode=SmoothingMode.AntiAlias;float rp=Math.Max(4f,Math.Min(10f,(float)((p.ShapeCode=="HOOP"||p.ShapeCode=="CROSSTIE"?2:3)*Math.Max(p.Diameter,8)*s)));using(var pen=new Pen(color,lineWidth>0?lineWidth:labels?3f:2f){LineJoin=LineJoin.Round,StartCap=LineCap.Round,EndCap=LineCap.Round})foreach(var l in lines)Rounded(g,pen,l.Select(map).ToArray(),rp);
  if(!labels)return;
  /* the bend radius R at one bend, with a short leader from the inside of the bend (owner, 21 November 2026: "paki fix yung R") */
  if(lettersOnly){var o=lines[0];int at=p.ShapeCode=="HOOK1"||p.ShapeCode=="HOOK2"?1:p.ShapeCode=="HOOP"?3:p.ShapeCode=="CROSSTIE"?2:p.ShapeCode=="CRANKHOOK"?3:-1;
   if(at>0&&at<o.Length-1){var P=map(o[at]);var A=map(o[at-1]);var C=map(o[at+1]);Func<PointF,PointF,PointF> unit=(a,b)=>{float dx=b.X-a.X,dy=b.Y-a.Y,l=(float)Math.Sqrt(dx*dx+dy*dy);return l<1e-3?new PointF(0,0):new PointF(dx/l,dy/l);};var d1=unit(P,A);var d2=unit(P,C);var bis=unit(new PointF(0,0),new PointF(d1.X+d2.X,d1.Y+d2.Y));
    var arcMid=new PointF(P.X+bis.X*rp*.45f,P.Y+bis.Y*rp*.45f);var end=new PointF(P.X+bis.X*(rp+14),P.Y+bis.Y*(rp+14));using(var lead=new Pen(color,.8f))g.DrawLine(lead,arcMid,end);
    var sz=g.MeasureString("R",font);using(var rb=new SolidBrush(PowTheme.Ink))g.DrawString("R",font,rb,end.X+bis.X*sz.Width*.6f-sz.Width/2,end.Y+bis.Y*sz.Height*.6f-sz.Height/2);}}
  using(var b=new SolidBrush(PowTheme.Ink)){Action<string,double,PointF,int,int> label=(name,v,at,dx,dy)=>{if(v<=0)return;string t=lettersOnly?name:(plainLabels?"":name+" = ")+v.ToString("0",Inv);var sz=g.MeasureString(t,font);float cx=at.X+dx*(sz.Width/2+6),cy=at.Y+dy*(sz.Height/2+3);float x=Math.Max(box.Left,Math.Min(box.Right-sz.Width,cx-sz.Width/2)),y=Math.Max(box.Top,Math.Min(box.Bottom-sz.Height,cy-sz.Height/2));g.DrawString(t,font,b,x,y);};
   double sa=p.ShapeCode=="HOOK1"&&p.DrawnStraight>0?p.DrawnStraight:p.DimA,sb=p.ShapeCode=="HOOK2"&&p.DrawnStraight>0?p.DrawnStraight:p.DimB;
   switch(p.ShapeCode){case "HOOK1":label("A",p.DimA,map(new PointF((float)sa/2,0)),0,1);label("B",p.DimB,map(new PointF(0,(float)p.DimB/2)),-1,0);break;
    case "HOOK2":label("A",p.DimA,map(new PointF(0,(float)p.DimA/2)),-1,0);label("B",p.DimB,map(new PointF((float)sb/2,0)),0,1);label("C",p.DimC,map(new PointF((float)sb,(float)p.DimC/2)),1,0);break;
    case "HOOP":{/* C is the 135° hook (owner, 22 November 2026: "bat walang sukat yung hook?"): labelled on the hook tail, not on the top side */var o=lines[0];label("A",p.DimA,map(new PointF((float)p.DimA/2,0)),0,1);label("B",p.DimB,map(new PointF((float)p.DimA,(float)p.DimB/2)),1,0);
     var tailEnd=map(o[o.Length-1]);var tailMid=map(new PointF((o[o.Length-2].X+o[o.Length-1].X)/2,(o[o.Length-2].Y+o[o.Length-1].Y)/2));
     string ct=lettersOnly?"C":(plainLabels?"":"C = ")+p.DimC.ToString("0",Inv);var csz=g.MeasureString(ct,font);g.DrawString(ct,font,b,tailEnd.X+4,tailEnd.Y-csz.Height*.2f);
     var corner=map(new PointF(0,(float)p.DimB));string ang="135°";var asz=g.MeasureString(ang,font);g.DrawString(ang,font,b,Math.Max(box.Left,corner.X-asz.Width-2),Math.Max(box.Top,corner.Y-asz.Height));break;}
    case "CROSSTIE":label("A",p.DimA,map(new PointF(0,(float)p.DimA/2)),-1,0);label("B",p.DimB,map(new PointF(0,(float)p.DimA)),1,-1);label("C",p.DimC,map(new PointF((float)p.DimC/2,0)),0,1);break;
    case "CRANK":case "CRANKHOOK":{var o=Outline(p)[0];label("A",p.DimA,map(new PointF(o[1].X/2,o[1].Y)),0,-1);label("B",p.DimB,map(new PointF((o[1].X+o[2].X)/2,(o[1].Y+o[2].Y)/2)),0,1);label("C",p.DimC,map(new PointF(0,o[1].Y/2)),-1,0);label("D",p.DimD,map(new PointF((o[2].X+o[3].X)/2,0)),0,1);if(o.Length>4)label("E",p.DimE,map(new PointF(o[4].X,o[4].Y/2)),1,0);break;}
    default:label("A",p.DimA,map(new PointF((float)p.DimA/2,0)),0,1);break;}}}
}
class BarShapeView:Control {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;CutPiece piece;
 public BarShapeView(){DoubleBuffered=true;ResizeRedraw=true;BackColor=Color.White;Dock=DockStyle.Right;Width=330;}
 public void Show(CutPiece p){piece=p;Invalidate();}
 protected override void OnPaint(PaintEventArgs e){/* drawings stay paper-white with dark ink in dark mode (owner, 21 November 2026: "pangit ng dark mode") */if(DesktopAppearance.Palette.Dark)e.Graphics.Clear(Color.White);using(new ReportPaperScope())PaintPaper(e);}
 void PaintPaper(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;var font=new Font("Segoe UI",8.5f);using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted)){
  using(var line=new Pen(PowTheme.Line))g.DrawLine(line,0,0,0,Height);if(piece==null){g.DrawString("Select a bar to see its shape.",font,muted,12,12);return;}var p=piece;
  g.DrawString(p.BarMark,ModernUi.Strong,ink,12,10);g.DrawString(BeamCutting.ShapeName(p.ShapeCode)+"\r\nG"+p.Grade+" "+p.Diameter+" mm, cut length "+(p.Length*1000).ToString("0",Inv)+" mm\r\n"+p.Pieces+" pcs ("+p.PerLine+" each x "+p.Lines+")  "+p.Kg.ToString("0.00",Inv)+" kg\r\n"+p.Location,font,muted,new RectangleF(12,32,Width-24,80));
  BarShapes.Draw(g,new RectangleF(10,120,Width-20,Height-170),BarShapes.Sketch(p),true,Color.FromArgb(25,90,160),font);
  g.DrawString("Long straight parts are drawn shorter; A, B, C, D, E are the outside dimensions in mm.",font,muted,new RectangleF(12,Height-44,Width-24,40));}}
}
}
