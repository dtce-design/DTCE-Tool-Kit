using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* interactive views of the staircase design sheet: mouse wheel zooms, dragging pans (rotates in 3D), double-click fits */
abstract class StairZoomView:Control {
 internal StairInput Input;internal StairResult Result;protected float Zoom=1;protected PointF Pan;Point last;bool drag;
 protected StairZoomView(){DoubleBuffered=true;BackColor=Color.White;Dock=DockStyle.Fill;ResizeRedraw=true;Font=new Font("Segoe UI",8.5f);}
 protected override void OnMouseWheel(MouseEventArgs e){float k=e.Delta>0?1.15f:1/1.15f;Pan=new PointF(e.X-(e.X-Pan.X)*k,e.Y-(e.Y-Pan.Y)*k);Zoom*=k;Invalidate();}
 protected override void OnMouseDown(MouseEventArgs e){Focus();drag=true;last=e.Location;}
 protected override void OnMouseUp(MouseEventArgs e){drag=false;}
 protected override void OnMouseMove(MouseEventArgs e){if(!drag)return;Dragged(e.X-last.X,e.Y-last.Y,e.Button);last=e.Location;Invalidate();}
 protected virtual void Dragged(int dx,int dy,MouseButtons b){Pan=new PointF(Pan.X+dx,Pan.Y+dy);}
 protected override void OnDoubleClick(EventArgs e){Zoom=1;Pan=PointF.Empty;Reset();Invalidate();}
 protected virtual void Reset(){}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.White);if(Input==null){g.DrawString("Enter the stair inputs.",Font,Brushes.Gray,12,12);return;}
  try{var st=g.Save();g.TranslateTransform(Pan.X,Pan.Y);g.ScaleTransform(Zoom,Zoom);Draw(g,new RectangleF(0,0,Width,Height));g.Restore(st);}catch(Exception ex){g.DrawString(ex.Message,Font,Brushes.Firebrick,12,12);}
  using(var f=new Font("Segoe UI",8f))g.DrawString(Hint,f,Brushes.Gray,8,Height-18);}
 protected virtual string Hint{get{return "Wheel: zoom   Drag: pan   Double-click: fit";}}
 protected abstract void Draw(Graphics g,RectangleF r);
 /* for the report: the view at its fitted scale */
 internal void RenderTo(Graphics g,RectangleF r){float z=Zoom;var p=Pan;Zoom=1;Pan=PointF.Empty;g.SmoothingMode=SmoothingMode.AntiAlias;try{Draw(g,r);}finally{Zoom=z;Pan=p;}}
 internal static readonly Color Ink=Color.FromArgb(35,40,48),Concrete=Color.FromArgb(232,232,228),Steel=Color.FromArgb(190,40,40),Dim=Color.FromArgb(90,100,115);
 protected static void Text(Graphics g,string t,float x,float y,float size=8.5f,bool bold=false,Brush b=null,StringAlignment al=StringAlignment.Center){using(var f=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular))using(var fmt=new StringFormat{Alignment=al,LineAlignment=StringAlignment.Center})g.DrawString(t,f,b??Brushes.Black,x,y,fmt);}
}
/* the longitudinal section of a flight (waist / slabless types) or the cross-section of a step (the others), with the bars */
class StairSectionView:StairZoomView {
 internal int Member;
 protected override void Draw(Graphics g,RectangleF r){var i=Input;if(i.Cantilever||i.Transverse||i.Stringer||i.Spiral){Across(g,r);return;}
  int n=Member==1?i.N2:Member==2?i.N3:i.N1;if(i.Flights==1)n=i.N1;double R=i.Riser,T=i.Tread,t=i.Waist,tl=i.LandingT;double before=Member==0?i.BottomLanding:(i.Flights==3&&Member==2?i.Width:i.Flights==3?i.Width*.5:i.Landing.StartsWith("Landing spans")?i.MidLanding:0);
  double after=Member==0&&i.Flights>1?(i.Flights==3?i.Width:i.Landing.StartsWith("Landing spans")?i.MidLanding:0):Member==1&&i.Flights==3?i.Width*.5:i.TopLanding;if(i.Flights==1)after=i.TopLanding;
  double going=(n-1)*T,L=before+going+after,H=n*R;float s=(float)Math.Min((r.Width-140)/Math.Max(L+600,1),(r.Height-120)/Math.Max(H+t+600,1));float ox=70,oy=r.Height-60;Func<double,double,PointF> P=(px,pz)=>new PointF(ox+(float)((px+300)*s),oy-(float)((pz+300)*s));
  double k=Math.Sqrt(R*R+T*T)/T,ts=i.Slabless?0:t*k;var outline=new List<PointF>();/* top profile */outline.Add(P(0,0));outline.Add(P(before,0));double x=before;for(int j=0;j<n-1;j++){outline.Add(P(x,(j+1)*R));x+=T;outline.Add(P(x,(j+1)*R));}outline.Add(P(x,H));outline.Add(P(L,H));
  /* soffit */var soff=new List<PointF>();soff.Add(P(L,H-tl));if(i.Slabless){x=before+going;for(int j=n-1;j>=1;j--){soff.Add(P(x,j*R+(j==n-1?R-tl:0)-t));x-=T;soff.Add(P(x+T-t,j*R-t));}soff.Add(P(before,-tl));}else{soff.Add(P(before+going+R/2*0,H-ts-tl*0));soff.Add(P(before,-ts+0));soff.Add(P(before,-tl));}soff.Add(P(0,-tl));
  var poly=outline.Concat(soff).ToArray();using(var b=new SolidBrush(Concrete))g.FillPolygon(b,poly);using(var p=new Pen(Ink,1.4f/Zoom))g.DrawPolygon(p,poly);
  /* supports */foreach(var sx in new[]{0.0,L}){var a=P(sx,sx==0?-tl:H-tl);g.FillPolygon(Brushes.DimGray,new[]{a,new PointF(a.X-8,a.Y+14),new PointF(a.X+8,a.Y+14)});}
  /* bars following the slab: bottom bars along the soffit, top bars over the supports and through the kinks (sampled along the profile) */
  double c=i.Cover+i.MainDia/2,tsl=i.Slabless?t:ts;Func<double,double> topLine=xx=>xx<before?-c:xx>before+going?H-c:(xx-before)*R/T-c;Func<double,double> botLine=xx=>xx<before?-tl+c:xx>before+going?H-tl+c:(xx-before)*R/T-tsl+c;
  Func<double,double,Func<double,double>,PointF[]> run=(a,b2,fz)=>{var pts=new List<PointF>();int nn=60;for(int q=0;q<=nn;q++){double xx=a+(b2-a)*q/nn;pts.Add(P(xx,fz(xx)));}return pts.ToArray();};
  using(var bar=new Pen(Steel,2.2f/Zoom)){g.DrawLines(bar,run(150,L-150,botLine));double ext=going*0.25+300;var left=new List<PointF>{P(c,-c-tl*0.6)};left.AddRange(run(c,Math.Min(L-c,before+ext),topLine));g.DrawLines(bar,left.ToArray());var right=run(Math.Max(c,L-after-ext),L-c,topLine).ToList();right.Add(P(L-c,H-c-tl*0.6));g.DrawLines(bar,right.ToArray());}
  using(var dot=new SolidBrush(Steel)){double sp=Math.Max(150,i.DistDia*20);for(double xx=200;xx<L-100;xx+=sp*1.5){var q=P(xx,botLine(xx)+i.MainDia);g.FillEllipse(dot,q.X-2.2f,q.Y-2.2f,4.4f,4.4f);}}
  /* dimensions */using(var dp=new Pen(Dim,1/Zoom)){var a=P(0,-tl-180);var b=P(L,-tl-180);g.DrawLine(dp,a,b);Text(g,"L = "+Staircase.F(L,"0")+" mm (plan)",(a.X+b.X)/2,a.Y+10,8.5f,true);var c1=P(before,-tl-80);var c2=P(before+going,-tl-80);g.DrawLine(dp,c1,c2);Text(g,(n-1)+" treads x "+Staircase.F(T,"0")+" = "+Staircase.F(going,"0"),(c1.X+c2.X)/2,c1.Y+9);}
  Text(g,"Riser "+Staircase.F(R,"0")+" / tread "+Staircase.F(T,"0")+" / "+(i.Slabless?"plate ":"waist ")+Staircase.F(t,"0")+" mm",P(L/2,H).X,P(0,H+250).Y,9,true);}
 /* across the width: one step between walls / on stringers / cantilever, or a spiral tread */
 void Across(Graphics g,RectangleF r){var i=Input;double W=i.Spiral?i.OuterRadius-i.ColumnDia/2:i.Width,h=i.Waist+i.Riser;float s=(float)Math.Min((r.Width-200)/(W+800),(r.Height-140)/(h+900));float ox=120,oy=r.Height/2+40;Func<double,double,PointF> P=(x,z)=>new PointF(ox+(float)(x*s),oy-(float)(z*s));
  var rect=new[]{P(0,0),P(W,0),P(W,h),P(0,h)};using(var b=new SolidBrush(Concrete))g.FillPolygon(b,rect);using(var p=new Pen(Ink,1.4f/Zoom))g.DrawPolygon(p,rect);
  if(i.Cantilever||i.Spiral){var wall=new[]{P(-300,-400),P(0,-400),P(0,h+400),P(-300,h+400)};using(var hb=new HatchBrush(HatchStyle.BackwardDiagonal,Color.Gray,Color.White))g.FillPolygon(hb,wall);g.DrawPolygon(Pens.Black,wall);Text(g,i.Spiral?"COLUMN":"WALL / SPINE BEAM",P(-150,0).X,P(0,h+520).Y,8.5f,true);}
  else if(i.Stringer){foreach(var sx in new[]{-i.StringerB,W}){var st=new[]{P(sx,h-i.StringerH),P(sx+i.StringerB,h-i.StringerH),P(sx+i.StringerB,h),P(sx,h)};using(var b=new SolidBrush(Color.FromArgb(215,215,210)))g.FillPolygon(b,st);g.DrawPolygon(Pens.Black,st);}Text(g,"STRINGER",P(-i.StringerB/2,0).X,P(0,h-i.StringerH-120).Y);Text(g,"STRINGER",P(W+i.StringerB/2,0).X,P(0,h-i.StringerH-120).Y);}
  else foreach(var sx in new[]{-250.0,W}){var wl=new[]{P(sx,-500),P(sx+250,-500),P(sx+250,h+300),P(sx,h+300)};using(var hb=new HatchBrush(HatchStyle.BackwardDiagonal,Color.Gray,Color.White))g.FillPolygon(hb,wl);g.DrawPolygon(Pens.Black,wl);}
  double c=i.Cover+i.MainDia/2;using(var bar=new Pen(Steel,2.4f/Zoom)){if(i.Cantilever||i.Spiral)g.DrawLines(bar,new[]{P(-260,h-c-200),P(-260,h-c),P(W-c,h-c),P(W-c,h-c-120)});else g.DrawLine(bar,P(-100,c),P(W+100,c));}
  using(var dot=new SolidBrush(Steel))for(double xx=150;xx<W;xx+=200){var q=P(xx,(i.Cantilever||i.Spiral)?h-c-i.MainDia:c+i.MainDia);g.FillEllipse(dot,q.X-2.2f,q.Y-2.2f,4.4f,4.4f);}
  using(var dp=new Pen(Dim,1/Zoom)){var a=P(0,-180);var b=P(W,-180);g.DrawLine(dp,a,b);Text(g,(i.Cantilever||i.Spiral?"cantilever ":"span ")+Staircase.F(W,"0")+" mm",(a.X+b.X)/2,a.Y+10,8.5f,true);}
  Text(g,i.Cantilever?"CANTILEVER STEP - SECTION ACROSS THE STAIR":i.Spiral?"SPIRAL TREAD - SECTION ALONG THE TREAD":i.Stringer?"WAIST BETWEEN THE STRINGERS - SECTION ACROSS":"STEP SPANNING ACROSS - SECTION",P(W/2,0).X,P(0,h+700).Y,9.5f,true);}
}
/* the plan of the stair with the flights, landings, tread lines and the UP arrow */
class StairPlanView:StairZoomView {
 protected override void Draw(Graphics g,RectangleF r){var i=Input;var boxes=Plan(i);if(i.Spiral){Spiral(g,r);return;}double minX=boxes.Min(b=>b.X0),maxX=boxes.Max(b=>b.X1),minY=boxes.Min(b=>b.Y0),maxY=boxes.Max(b=>b.Y1);float s=(float)Math.Min((r.Width-80)/(maxX-minX),(r.Height-80)/(maxY-minY));
  Func<double,double,PointF> P=(x,y)=>new PointF(40+(float)((x-minX)*s),r.Height-40-(float)((y-minY)*s));
  foreach(var b in boxes){var a=P(b.X0,b.Y1);var c=P(b.X1,b.Y0);var rc=new RectangleF(a.X,a.Y,c.X-a.X,c.Y-a.Y);using(var br=new SolidBrush(b.Landing?Color.FromArgb(240,240,236):Color.FromArgb(226,232,238)))g.FillRectangle(br,rc);g.DrawRectangle(Pens.Black,rc.X,rc.Y,rc.Width,rc.Height);
   if(!b.Landing){for(int k=1;k<b.Treads;k++){double f=(double)k/b.Treads;if(b.Dx!=0){double x=b.Dx>0?b.X0+(b.X1-b.X0)*f:b.X1-(b.X1-b.X0)*f;g.DrawLine(Pens.Gray,P(x,b.Y0),P(x,b.Y1));}else{double y=b.Dy>0?b.Y0+(b.Y1-b.Y0)*f:b.Y1-(b.Y1-b.Y0)*f;g.DrawLine(Pens.Gray,P(b.X0,y),P(b.X1,y));}}
    PointF from=b.Dx!=0?P(b.Dx>0?b.X0:b.X1,(b.Y0+b.Y1)/2):P((b.X0+b.X1)/2,b.Dy>0?b.Y0:b.Y1),to=b.Dx!=0?P(b.Dx>0?b.X1:b.X0,(b.Y0+b.Y1)/2):P((b.X0+b.X1)/2,b.Dy>0?b.Y1:b.Y0);using(var ar=new Pen(Color.FromArgb(28,100,160),1.6f/Zoom){CustomEndCap=new AdjustableArrowCap(4,5)})g.DrawLine(ar,from,to);Text(g,"UP",from.X+(to.X-from.X)*.15f,from.Y+(to.Y-from.Y)*.15f-10,8,true,Brushes.SteelBlue);
    Text(g,b.Name,(rc.Left+rc.Right)/2,(rc.Top+rc.Bottom)/2+12,8);}else Text(g,b.Name,(rc.Left+rc.Right)/2,(rc.Top+rc.Bottom)/2,8,false,Brushes.DimGray);}
  Text(g,"PLAN - "+i.Type,r.Width/2,14,9.5f,true);}
 void Spiral(Graphics g,RectangleF r){var i=Input;float s=(float)((Math.Min(r.Width,r.Height)-80)/(2*i.OuterRadius));var c=new PointF(r.Width/2,r.Height/2);float Ro=(float)(i.OuterRadius*s),rc=(float)(i.ColumnDia/2*s);int n=(int)i.TreadsPerTurn;
  using(var br=new SolidBrush(Color.FromArgb(226,232,238)))g.FillEllipse(br,c.X-Ro,c.Y-Ro,2*Ro,2*Ro);g.DrawEllipse(Pens.Black,c.X-Ro,c.Y-Ro,2*Ro,2*Ro);for(int k=0;k<n;k++){double a=2*Math.PI*k/n;g.DrawLine(Pens.Gray,c.X+(float)(rc*Math.Cos(a)),c.Y-(float)(rc*Math.Sin(a)),c.X+(float)(Ro*Math.Cos(a)),c.Y-(float)(Ro*Math.Sin(a)));if(k<n)Text(g,(k+1).ToString(),c.X+(float)(Ro*.8*Math.Cos(a+Math.PI/n)),c.Y-(float)(Ro*.8*Math.Sin(a+Math.PI/n)),7.5f);}
  g.FillEllipse(Brushes.DimGray,c.X-rc,c.Y-rc,2*rc,2*rc);Text(g,"PLAN - SPIRAL STAIR ("+n+" TREADS PER TURN), UP COUNTER-CLOCKWISE",r.Width/2,14,9.5f,true);}
 internal class Box {public double X0,X1,Y0,Y1,Z;public bool Landing;public int Treads;public int Dx,Dy;public string Name;}
 /* plan rectangles in mm: flights with their direction, landings */
 internal static List<Box> Plan(StairInput i){var list=new List<Box>();double W=i.Width,T=i.Tread,R=i.Riser;double g1=(i.N1-1)*T;double x=i.BottomLanding;
  if(i.BottomLanding>0)list.Add(new Box{X0=0,X1=i.BottomLanding,Y0=0,Y1=W,Landing=true,Name="LANDING",Z=0});
  list.Add(new Box{X0=x,X1=x+g1,Y0=0,Y1=W,Treads=i.N1-1,Dx=1,Name="FLIGHT 1",Z=0});x+=g1;
  if(i.Flights==1||i.Cantilever||i.Transverse||i.Stringer||i.Slabless&&i.Flights==1){if(i.TopLanding>0)list.Add(new Box{X0=x,X1=x+i.TopLanding,Y0=0,Y1=W,Landing=true,Name="LANDING",Z=i.N1*R});return list;}
  if(i.Flights==2){list.Add(new Box{X0=x,X1=x+i.MidLanding,Y0=0,Y1=2*W+i.Gap,Landing=true,Name="HALF LANDING",Z=i.N1*R});double g2=(i.N2-1)*T;list.Add(new Box{X0=x-g2,X1=x,Y0=W+i.Gap,Y1=2*W+i.Gap,Treads=i.N2-1,Dx=-1,Name="FLIGHT 2",Z=i.N1*R});if(i.TopLanding>0)list.Add(new Box{X0=x-g2-i.TopLanding,X1=x-g2,Y0=W+i.Gap,Y1=2*W+i.Gap,Landing=true,Name="LANDING",Z=(i.N1+i.N2)*R});return list;}
  list.Add(new Box{X0=x,X1=x+W,Y0=0,Y1=W,Landing=true,Name="LANDING 1",Z=i.N1*R});double g2b=(i.N2-1)*T;list.Add(new Box{X0=x,X1=x+W,Y0=W,Y1=W+g2b,Treads=i.N2-1,Dy=1,Name="FLIGHT 2",Z=i.N1*R});list.Add(new Box{X0=x,X1=x+W,Y0=W+g2b,Y1=2*W+g2b,Landing=true,Name="LANDING 2",Z=(i.N1+i.N2)*R});
  double g3=(i.N3-1)*T;list.Add(new Box{X0=x-g3,X1=x,Y0=W+g2b,Y1=2*W+g2b,Treads=i.N3-1,Dx=-1,Name="FLIGHT 3",Z=(i.N1+i.N2)*R});if(i.TopLanding>0)list.Add(new Box{X0=x-g3-i.TopLanding,X1=x-g3,Y0=W+g2b,Y1=2*W+g2b,Landing=true,Name="LANDING",Z=(i.N1+i.N2+i.N3)*R});return list;}
}
/* a shaded 3D model: drag rotates, wheel zooms */
class Stair3DView:StairZoomView {
 double yaw=-0.75,pitch=0.55;
 protected override string Hint{get{return "Drag: rotate   Wheel: zoom   Double-click: reset";}}
 protected override void Dragged(int dx,int dy,MouseButtons b){if(b==MouseButtons.Right){base.Dragged(dx,dy,b);return;}yaw+=dx*0.01;pitch=Math.Max(-1.4,Math.Min(1.4,pitch+dy*0.01));}
 protected override void Reset(){yaw=-0.75;pitch=0.55;}
 class Face {public double[][] P;public Color C;}
 static void Prism(List<Face> f,double[][] bottom,double h,Color c){int n=bottom.Length;var top=bottom.Select(p=>new[]{p[0],p[1],p[2]+h}).ToArray();f.Add(new Face{P=bottom.Reverse().ToArray(),C=c});f.Add(new Face{P=top,C=c});for(int k=0;k<n;k++){var a=bottom[k];var b=bottom[(k+1)%n];f.Add(new Face{P=new[]{a,b,new[]{b[0],b[1],b[2]+h},new[]{a[0],a[1],a[2]+h}},C=c});}}
 static void Box(List<Face> f,double x0,double x1,double y0,double y1,double z0,double z1,Color c){Prism(f,new[]{new[]{x0,y0,z0},new[]{x1,y0,z0},new[]{x1,y1,z0},new[]{x0,y1,z0}},z1-z0,c);}
 List<Face> Model(){var i=Input;var f=new List<Face>();Color step=Color.FromArgb(205,208,212),land=Color.FromArgb(188,196,204),wall=Color.FromArgb(214,206,196),col=Color.FromArgb(150,150,160);double R=i.Riser,T=i.Tread,t=i.Waist,tl=i.LandingT;
  if(i.Spiral){int n=(int)i.TreadsPerTurn*Math.Max(1,(int)Math.Ceiling(i.N1/(double)i.TreadsPerTurn));double rc=i.ColumnDia/2,Ro=i.OuterRadius;for(int k=0;k<Math.Max(i.N1,4);k++){double a0=2*Math.PI*k/i.TreadsPerTurn,a1=a0+2*Math.PI/i.TreadsPerTurn;double z=(k+1)*R;Prism(f,new[]{new[]{rc*Math.Cos(a0),rc*Math.Sin(a0),z-t},new[]{Ro*Math.Cos(a0),Ro*Math.Sin(a0),z-t},new[]{Ro*Math.Cos(a1),Ro*Math.Sin(a1),z-t},new[]{rc*Math.Cos(a1),rc*Math.Sin(a1),z-t}},t,step);}
   var ring=Enumerable.Range(0,12).Select(k=>new[]{rc*Math.Cos(k*Math.PI/6),rc*Math.Sin(k*Math.PI/6),0.0}).ToArray();Prism(f,ring,(Math.Max(i.N1,4)+2)*R,col);return f;}
  foreach(var b in StairPlanView.Plan(i)){if(b.Landing){Box(f,b.X0,b.X1,b.Y0,b.Y1,b.Z-tl,b.Z,land);continue;}
   int n=b.Treads+1;double k=Math.Sqrt(R*R+T*T)/T,ts=i.Slabless?t:t*k;
   for(int j=0;j<b.Treads;j++){double s0=j*T,s1=(j+1)*T,z=b.Z+(j+1)*R;double xa,xb,ya,yb;if(b.Dx!=0){xa=b.Dx>0?b.X0+s0:b.X1-s1;xb=b.Dx>0?b.X0+s1:b.X1-s0;ya=b.Y0;yb=b.Y1;}else{ya=b.Dy>0?b.Y0+s0:b.Y1-s1;yb=b.Dy>0?b.Y0+s1:b.Y1-s0;xa=b.X0;xb=b.X1;}
    double zb=i.Slabless?z-R-t:b.Z+(j*T)*R/T-ts;if(i.Cantilever||i.Transverse)zb=z-R-t*0.6;Box(f,xa,xb,ya,yb,zb,z,step);}
   if(i.Stringer||i.Transverse||i.Cantilever){double len=b.Treads*T;if(i.Stringer)foreach(var yy in new[]{b.Y0-i.StringerB,b.Y1}){/* stringer along the flight */double x0=b.X0,x1=b.X1;f.Add(new Face{P=new[]{new[]{x0,yy,b.Z-i.StringerH},new[]{x1,yy,b.Z+b.Treads*R-i.StringerH},new[]{x1,yy,b.Z+b.Treads*R+R},new[]{x0,yy,b.Z+R}},C=col});f.Add(new Face{P=new[]{new[]{x0,yy+i.StringerB,b.Z-i.StringerH},new[]{x1,yy+i.StringerB,b.Z+b.Treads*R-i.StringerH},new[]{x1,yy+i.StringerB,b.Z+b.Treads*R+R},new[]{x0,yy+i.StringerB,b.Z+R}},C=col});}
    else{/* the wall(s) along the flight, following its slope */double hi=b.Treads*R;foreach(var yy in i.Cantilever?new[]{b.Y0}:new[]{b.Y0-1,b.Y1+1})f.Add(new Face{P=new[]{new[]{b.X0,yy,b.Z-400},new[]{b.X1,yy,b.Z+hi-400},new[]{b.X1,yy,b.Z+hi+R+500},new[]{b.X0,yy,b.Z+R+500}},C=wall});}}}
  return f;}
 protected override void Draw(Graphics g,RectangleF r){var faces=Model();if(faces.Count==0)return;var all=faces.SelectMany(q=>q.P).ToList();double cx=(all.Min(p=>p[0])+all.Max(p=>p[0]))/2,cy=(all.Min(p=>p[1])+all.Max(p=>p[1]))/2,cz=(all.Min(p=>p[2])+all.Max(p=>p[2]))/2;
  double cyw=Math.Cos(yaw),syw=Math.Sin(yaw),cp=Math.Cos(pitch),sp=Math.Sin(pitch);Func<double[],double[]> rot=p=>{double x=p[0]-cx,y=p[1]-cy,z=p[2]-cz;double x1=x*cyw-y*syw,y1=x*syw+y*cyw;return new[]{x1,y1*cp-z*sp,y1*sp+z*cp};};
  var pr=faces.Select(q=>new{F=q,R=q.P.Select(rot).ToArray()}).ToList();double ext=pr.SelectMany(q=>q.R).Max(p=>Math.Max(Math.Abs(p[0]),Math.Abs(p[2])));float s=(float)(Math.Min(r.Width,r.Height)*0.45/Math.Max(ext,1));float ox=r.Width/2,oy=r.Height/2;
  var light=new[]{-0.4,-0.5,0.77};foreach(var q in pr.OrderByDescending(q=>q.R.Average(p=>p[1]))){var p=q.R;if(p.Length<3)continue;double ax=p[1][0]-p[0][0],ay=p[1][1]-p[0][1],az=p[1][2]-p[0][2],bx=p[2][0]-p[0][0],by=p[2][1]-p[0][1],bz=p[2][2]-p[0][2];double nx=ay*bz-az*by,ny=az*bx-ax*bz,nz=ax*by-ay*bx,nl=Math.Sqrt(nx*nx+ny*ny+nz*nz);if(nl<1e-9)continue;
   double lam=Math.Abs((nx*light[0]+ny*light[1]+nz*light[2])/nl);float k=(float)(0.55+0.45*lam);var c=Color.FromArgb((int)Math.Min(255,q.F.C.R*k),(int)Math.Min(255,q.F.C.G*k),(int)Math.Min(255,q.F.C.B*k));var pts=p.Select(v=>new PointF(ox+(float)(v[0]*s),oy-(float)(v[2]*s))).ToArray();
   using(var b=new SolidBrush(c))g.FillPolygon(b,pts);using(var pen=new Pen(Color.FromArgb(90,95,105),0.7f/Zoom))g.DrawPolygon(pen,pts);}
  Text(g,"3D MODEL - "+Input.Type,r.Width/2,14,9.5f,true);}
}
/* factored load, shear and moment along the selected member */
class StairDiagramView:StairZoomView {
 internal int Member;
 protected override void Draw(Graphics g,RectangleF r){if(Result==null||Result.Members.Count==0){Text(g,"Results appear when the inputs are complete.",r.Width/2,r.Height/2);return;}var m=Result.Members[Math.Min(Member,Result.Members.Count-1)];double[][] d;if(!Result.Diagrams.TryGetValue(m,out d))return;var xs=d[0];double L=xs[xs.Length-1];float left=70,w=r.Width-120;float band=(r.Height-60)/3;
  Text(g,m.Name+" - factored 1.2D + 1.6L (span "+Staircase.F(L,"0.00")+" m"+(m.Support=="Cantilever"?", cantilever from x = 0":", "+m.Support.ToLowerInvariant())+")",r.Width/2,14,9.5f,true);
  /* loads */float y0=30;g.DrawLine(Pens.Black,left,y0+band*.7f,left+w,y0+band*.7f);double wmax=m.Segs.Max(s=>1.2*s.Wd+1.6*s.Wl);if(wmax<=0)wmax=1;
  foreach(var s in m.Segs){double a=m.X0(s);float xa=left+(float)(a/L*w),xb=left+(float)((a+s.Length)/L*w);double q=1.2*s.Wd+1.6*s.Wl;float hgt=(float)(band*.5*q/wmax);using(var br=new SolidBrush(Color.FromArgb(s.Flight?70:40,30,100,170)))g.FillRectangle(br,xa,y0+band*.7f-hgt,xb-xa,hgt);g.DrawRectangle(Pens.SteelBlue,xa,y0+band*.7f-hgt,xb-xa,hgt);Text(g,Staircase.F(q,"0.00")+" kN/m",(xa+xb)/2,y0+band*.7f-hgt-8,8);Text(g,s.Name,(xa+xb)/2,y0+band*.7f+10,7.5f,false,Brushes.DimGray);}
  Curve(g,xs,d[1],left,w,y0+band,band,"V (kN"+(Input.Cantilever||Input.Spiral||Input.Transverse||Input.Stringer?"":"/m")+")",Color.FromArgb(28,122,84));Curve(g,xs,d[2],left,w,y0+2*band,band,"M (kN·m"+(Input.Cantilever||Input.Spiral||Input.Transverse||Input.Stringer?"":"/m")+")",Color.FromArgb(190,60,40));}
 static void Curve(Graphics g,double[] xs,double[] ys,float left,float w,float top,float band,string name,Color c){double L=xs[xs.Length-1],m=Math.Max(1e-9,ys.Max(v=>Math.Abs(v)));float mid=top+band/2;g.DrawLine(Pens.Black,left,mid,left+w,mid);
  var pts=new List<PointF>{new PointF(left,mid)};for(int k=0;k<xs.Length;k++)pts.Add(new PointF(left+(float)(xs[k]/L*w),mid+(float)(ys[k]/m*band*.42)));pts.Add(new PointF(left+w,mid));using(var b=new SolidBrush(Color.FromArgb(55,c)))g.FillPolygon(b,pts.ToArray());using(var p=new Pen(c,1.6f))g.DrawLines(p,pts.ToArray());
  int imax=Array.IndexOf(ys,ys.Max()),imin=Array.IndexOf(ys,ys.Min());foreach(int k in new[]{imax,imin}.Distinct()){if(Math.Abs(ys[k])<1e-6)continue;var p=new PointF(left+(float)(xs[k]/L*w)+(k==0?48:k==xs.Length-1?-48:0),mid+(float)(ys[k]/m*band*.42));bool end=k==0||k==xs.Length-1;Text(g,Staircase.F(ys[k],"0.00")+" @ "+Staircase.F(xs[k],"0.00")+" m",p.X,end?p.Y:p.Y+(ys[k]>0?10:-10),8,true);}
  Text(g,name,left-36,mid,8.5f,true);}
}
}
