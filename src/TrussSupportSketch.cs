using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* Interactive support model (owner, 19 November 2026: "Gawin mong interactive ang input may model"). Plan and elevation drawn from the
   inputs; dragging the plate edges, RC support edges, anchors, gusset ends / top or the anchor tips changes the inputs (5 mm steps),
   double-clicking a dimension types its value, and the parts whose checks fail are drawn red. */
class TrussSupportSketch:Control {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 TrussSupportInput input;double gussetH;public readonly HashSet<string> Failing=new HashSet<string>();
 /* raised with the input key and its new value in mm */
 public event Action<string,double> ValueChanged;
 sealed class Handle {public string Key;public RectangleF Area;public Cursor Cursor;public Func<PointF,double> Value;}
 sealed class Label {public string Key;public RectangleF Area;public double Current;}
 readonly List<Handle> handles=new List<Handle>();readonly List<Label> labels=new List<Label>();Handle active,hover;TextBox editor;
 public TrussSupportSketch(){DoubleBuffered=true;ResizeRedraw=true;Width=1000;Height=500;Tag="FullWidth";BackColor=Color.White;}
 public void Show(TrussSupportInput i,double h){input=i;gussetH=h;if(active==null)Invalidate();else Invalidate();}
 static string F(double v){return v.ToString("0",Inv);}
 static double Snap(double v){return Math.Max(5,Math.Round(v/5)*5);}
 void Dim(Graphics g,Font f,Brush b,Pen p,PointF a,PointF c,double value,string key,bool vertical,string prefix=""){g.DrawLine(p,a,c);string text=prefix+F(value);var size=g.MeasureString(text,f);RectangleF area;
  if(vertical){g.DrawLine(p,a.X-3,a.Y,a.X+3,a.Y);g.DrawLine(p,c.X-3,c.Y,c.X+3,c.Y);area=new RectangleF(a.X-size.Width-4,(a.Y+c.Y)/2-size.Height/2,size.Width,size.Height);}
  else{g.DrawLine(p,a.X,a.Y-3,a.X,a.Y+3);g.DrawLine(p,c.X,c.Y-3,c.X,c.Y+3);area=new RectangleF((a.X+c.X)/2-size.Width/2,a.Y-size.Height-1,size.Width,size.Height);}
  bool editable=key!=null;g.DrawString(text,f,editable?Brushes.SteelBlue:b,area.Location);if(editable){using(var u=new Pen(Color.SteelBlue){DashStyle=DashStyle.Dot})g.DrawLine(u,area.Left,area.Bottom-1,area.Right,area.Bottom-1);labels.Add(new Label{Key=key,Area=area,Current=value});}}
 void Grip(Graphics g,string key,RectangleF area,Cursor cursor,Func<PointF,double> value){handles.Add(new Handle{Key=key,Area=area,Cursor=cursor,Value=value});if(hover!=null&&hover.Key==key||active!=null&&active.Key==key)using(var b=new SolidBrush(Color.FromArgb(70,30,120,220)))g.FillRectangle(b,area);}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);handles.Clear();labels.Clear();var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;var font=new Font("Segoe UI",8.5f);var bold=ModernUi.Strong;
  using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted))using(var line=new Pen(PowTheme.Ink,1.3f))using(var dim=new Pen(PowTheme.Muted)){
   if(input==null){g.DrawString("Enter the plate, anchor and gusset sizes to see the model.",font,muted,12,12);return;}var i=input;Color bad=Color.FromArgb(205,40,40);
   float W1=Width*.34f,W2=Width*.30f;g.DrawString("PLAN",bold,ink,12,8);g.DrawString("ELEVATION",bold,ink,W1+12,8);g.DrawString("3D VIEW",bold,ink,W1+W2+12,8);using(var sep=new Pen(PowTheme.Line)){g.DrawLine(sep,W1,6,W1,Height-6);g.DrawLine(sep,W1+W2,6,W1+W2,Height-6);}g.DrawString("Drag the plate and support edges, the anchors, the gusset ends and top, and the anchor tips; double-click a blue dimension to type it.",font,muted,new RectangleF(12,28,W1-20,34));
   /* plan: N along the truss (horizontal), B across */
   double spanX=Math.Max(i.SupportN,Math.Max(i.PlateN,i.GussetL))+160,spanY=Math.Max(i.SupportB,i.PlateB)+150;double ew=Math.Max(i.SupportN,i.GussetL)+180,eh=gussetH+i.PlateT+i.Hef+150;
   /* plan and elevation share one standard drawing scale (owner: "Iscale naman yung drawing") */
   int ratio;float k=StandardScale(g,Math.Min((W1-90)/spanX,Math.Min((W2-60)/ew,Math.Min((Height-110)/spanY,(Height-90)/eh))),out ratio);float cx=W1/2+20,cy=Height/2f+18;
   Func<double,float> X=x=>cx+(float)(x*k);Func<double,float> Y=y=>cy+(float)(y*k);Func<PointF,double> px=p=>Math.Abs(p.X-cx)/k,py=p=>Math.Abs(p.Y-cy)/k;
   using(var dash=new Pen(Color.Gray){DashStyle=DashStyle.Dash})g.DrawRectangle(dash,X(-i.SupportN/2),Y(-i.SupportB/2),(float)(i.SupportN*k),(float)(i.SupportB*k));g.DrawString("RC support",font,muted,X(-i.SupportN/2)+2,Y(i.SupportB/2)+2);
   bool plateBad=Failing.Contains("plate");using(var plate=new SolidBrush(plateBad?Color.FromArgb(250,220,220):Color.FromArgb(225,232,240)))g.FillRectangle(plate,X(-i.PlateN/2),Y(-i.PlateB/2),(float)(i.PlateN*k),(float)(i.PlateB*k));using(var edge=new Pen(plateBad?bad:PowTheme.Ink,1.4f))g.DrawRectangle(edge,X(-i.PlateN/2),Y(-i.PlateB/2),(float)(i.PlateN*k),(float)(i.PlateB*k));
   bool gussetBad=Failing.Contains("gusset");using(var gus=new SolidBrush(gussetBad?bad:Color.FromArgb(90,110,140)))g.FillRectangle(gus,X(-i.GussetL/2),Y(-i.GussetT/2)-1,(float)(i.GussetL*k),Math.Max(3,(float)(i.GussetT*k)));
   double run=(i.BoltColumns-1)*i.BoltS1;bool anchorBad=Failing.Contains("anchors");
   for(int c=0;c<i.BoltColumns;c++)foreach(int side in new[]{-1,1}){double x=-run/2+c*i.BoltS1,y=side*i.BoltS2/2;float r=Math.Max(4,(float)(i.BoltDia*k/2));using(var bolt=new SolidBrush(anchorBad?bad:Color.Black))g.FillEllipse(bolt,X(x)-r,Y(y)-r,2*r,2*r);
    int col=c;Grip(g,"Bolt",new RectangleF(X(x)-r-4,Y(y)-r-4,2*r+8,2*r+8),Cursors.SizeAll,p=>0);handles[handles.Count-1].Value=p=>col;}
   Grip(g,"PlateN",new RectangleF(X(i.PlateN/2)-4,Y(-i.PlateB/2),8,(float)(i.PlateB*k)),Cursors.SizeWE,p=>2*px(p));Grip(g,"PlateN",new RectangleF(X(-i.PlateN/2)-4,Y(-i.PlateB/2),8,(float)(i.PlateB*k)),Cursors.SizeWE,p=>2*px(p));
   Grip(g,"PlateB",new RectangleF(X(-i.PlateN/2),Y(-i.PlateB/2)-4,(float)(i.PlateN*k),8),Cursors.SizeNS,p=>2*py(p));Grip(g,"PlateB",new RectangleF(X(-i.PlateN/2),Y(i.PlateB/2)-4,(float)(i.PlateN*k),8),Cursors.SizeNS,p=>2*py(p));
   Grip(g,"SupportN",new RectangleF(X(i.SupportN/2)-3,Y(-i.SupportB/2),6,(float)(i.SupportB*k)),Cursors.SizeWE,p=>2*px(p));Grip(g,"SupportB",new RectangleF(X(-i.SupportN/2),Y(i.SupportB/2)-3,(float)(i.SupportN*k),6),Cursors.SizeNS,p=>2*py(p));
   Grip(g,"GussetL",new RectangleF(X(i.GussetL/2)-5,Y(0)-6,10,12),Cursors.SizeWE,p=>2*px(p));Grip(g,"GussetL",new RectangleF(X(-i.GussetL/2)-5,Y(0)-6,10,12),Cursors.SizeWE,p=>2*px(p));
   Dim(g,font,muted,dim,new PointF(X(-i.PlateN/2),Y(-Math.Max(i.PlateB,i.SupportB)/2)-18),new PointF(X(i.PlateN/2),Y(-Math.Max(i.PlateB,i.SupportB)/2)-18),i.PlateN,"PlateN",false);
   float left=Math.Min(X(-i.PlateN/2),X(-i.SupportN/2));Dim(g,font,muted,dim,new PointF(left-20,Y(-i.PlateB/2)),new PointF(left-20,Y(i.PlateB/2)),i.PlateB,"PlateB",true);
   double edge2=(i.PlateB-i.BoltS2)/2;Dim(g,font,muted,dim,new PointF(left-62,Y(-i.PlateB/2)),new PointF(left-62,Y(-i.BoltS2/2)),edge2,null,true);Dim(g,font,muted,dim,new PointF(left-62,Y(-i.BoltS2/2)),new PointF(left-62,Y(i.BoltS2/2)),i.BoltS2,"BoltS2",true);
   if(i.BoltColumns>1)Dim(g,font,muted,dim,new PointF(X(-run/2),Y(Math.Max(i.PlateB,i.SupportB)/2)+34),new PointF(X(-run/2+i.BoltS1),Y(Math.Max(i.PlateB,i.SupportB)/2)+34),i.BoltS1,"BoltS1",false,"pitch ");
   g.DrawString(F(i.PlateN)+" x "+F(i.PlateB)+" x "+F(i.PlateT)+" mm plate, "+(2*i.BoltColumns)+" - "+i.BoltDia+" mm "+(i.Headed?"headed":"L")+" anchors ("+i.BoltColumns+" each side)",font,ink,12,Height-22);
   planScale=k;planCenter=new PointF(cx,cy);
   /* elevation: along the truss */
   float ke=k;float ex=W1+W2/2+10,ey=60+(float)(gussetH*ke)+20;
   Func<double,float> EX=x=>ex+(float)(x*ke);Func<double,float> EY=y=>ey+(float)(y*ke);
   using(var conc=new HatchBrush(HatchStyle.Percent10,Color.Gray,Color.FromArgb(240,240,236)))g.FillRectangle(conc,EX(-i.SupportN/2),EY(i.PlateT),(float)(i.SupportN*ke),(float)((i.Hef+90)*ke));g.DrawRectangle(dim,EX(-i.SupportN/2),EY(i.PlateT),(float)(i.SupportN*ke),(float)((i.Hef+90)*ke));
   using(var plate=new SolidBrush(plateBad?bad:Color.FromArgb(120,140,170)))g.FillRectangle(plate,EX(-i.PlateN/2),EY(0),(float)(i.PlateN*ke),Math.Max(3,(float)(i.PlateT*ke)));
   using(var gus=new HatchBrush(HatchStyle.BackwardDiagonal,gussetBad?bad:Color.FromArgb(150,160,180),Color.FromArgb(235,238,244)))g.FillRectangle(gus,EX(-i.GussetL/2),EY(-gussetH),(float)(i.GussetL*ke),(float)(gussetH*ke));using(var edge=new Pen(gussetBad?bad:PowTheme.Ink,1.3f))g.DrawRectangle(edge,EX(-i.GussetL/2),EY(-gussetH),(float)(i.GussetL*ke),(float)(gussetH*ke));
   using(var bolt=new Pen(anchorBad?bad:Color.Black,2.2f))for(int c=0;c<i.BoltColumns;c++){float x=EX(-run/2+c*i.BoltS1);g.DrawLine(bolt,x,EY(-25),x,EY(i.PlateT+i.Hef));if(i.Headed)g.DrawLine(bolt,x-6,EY(i.PlateT+i.Hef),x+6,EY(i.PlateT+i.Hef));else g.DrawLine(bolt,x,EY(i.PlateT+i.Hef),x+(float)(i.HookEh*ke)*(c==0&&i.BoltColumns>1?-1:1),EY(i.PlateT+i.Hef));}
   float tipX=EX(-run/2);Grip(g,"Hef",new RectangleF(tipX-8,EY(i.PlateT+i.Hef)-6,16,12),Cursors.SizeNS,p=>(p.Y-ey)/ke-i.PlateT);
   Grip(g,"GussetH",new RectangleF(EX(-i.GussetL/2),EY(-gussetH)-4,(float)(i.GussetL*ke),8),Cursors.SizeNS,p=>(ey-p.Y)/ke);Grip(g,"GussetL",new RectangleF(EX(i.GussetL/2)-4,EY(-gussetH),8,(float)(gussetH*ke)),Cursors.SizeWE,p=>2*Math.Abs(p.X-ex)/ke);
   Dim(g,font,muted,dim,new PointF(EX(-i.GussetL/2),EY(-gussetH)-14),new PointF(EX(i.GussetL/2),EY(-gussetH)-14),i.GussetL,"GussetL",false);Dim(g,font,muted,dim,new PointF(EX(-Math.Max(i.SupportN,i.GussetL)/2)-18,EY(-gussetH)),new PointF(EX(-Math.Max(i.SupportN,i.GussetL)/2)-18,EY(0)),gussetH,"GussetH",true);
   Dim(g,font,muted,dim,new PointF(EX(-Math.Max(i.SupportN,i.GussetL)/2)-18,EY(i.PlateT)),new PointF(EX(-Math.Max(i.SupportN,i.GussetL)/2)-18,EY(i.PlateT+i.Hef)),i.Hef,"Hef",true,"hef ");
   g.DrawString("gusset "+F(i.GussetT)+" mm, "+F(i.PlateWeld)+" mm fillet both sides"+(Failing.Count>0?"   -   red: "+string.Join(", ",Failing)+" checks NG":""),font,Failing.Count>0?new SolidBrush(bad):ink,W1+12,Height-22);
   elevScale=ke;elevOrigin=new PointF(ex,ey);
   ScaleBar(g,font,ink,12,Height-44,k,ratio);
   Draw3D(g,font,muted,new RectangleF(W1+W2+6,30,Width-W1-W2-12,Height-60));view3D=new RectangleF(W1+W2,0,Width-W1-W2,Height);}}
 /* largest standard scale (1:n) that fits, at the screen's dpi */
 static float StandardScale(Graphics g,double fit,out int ratio){double pxPerMm=g.DpiX/25.4;foreach(int n in new[]{1,2,5,10,15,20,25,30,40,50,75,100,150,200,250,500}){if(pxPerMm/n<=fit){ratio=n;return (float)(pxPerMm/n);}}ratio=1000;return (float)(pxPerMm/1000);}
 static void ScaleBar(Graphics g,Font f,Brush ink,float x,float y,float k,int ratio){double len=ratio<=10?100:ratio<=25?200:500;float w=(float)(len*k);using(var p=new Pen(PowTheme.Ink,1.2f)){g.DrawLine(p,x,y,x+w,y);g.DrawLine(p,x,y-4,x,y+4);g.DrawLine(p,x+w/2,y-3,x+w/2,y+3);g.DrawLine(p,x+w,y-4,x+w,y+4);}g.DrawString("0",f,ink,x-3,y+4);g.DrawString(len.ToString("0",Inv)+" mm",f,ink,x+w-14,y+4);g.DrawString("Scale 1:"+ratio+" (plan and elevation, on screen)",f,ink,x+w+40,y-7);}
 /* isometric view - drag to turn it */
 double yaw=-.75,pitch=.55;RectangleF view3D;bool rotating;
 sealed class Face {public PointF[] P;public double Depth;public Color C;}
 void Draw3D(Graphics g,Font font,Brush muted,RectangleF area){var i=input;var faces=new List<Face>();double cy=Math.Cos(yaw),sy=Math.Sin(yaw),cp=Math.Cos(pitch),sp=Math.Sin(pitch);
  Func<double,double,double,double[]> P=(x,y,z)=>{double xr=x*cy-y*sy,yr=x*sy+y*cy;return new[]{xr,z*cp+yr*sp,yr*cp-z*sp};};
  Action<double,double,double,double,double,double,Color,int> box=(x0,y0,z0,x1,y1,z1,c,alpha)=>{var v=new double[8][];int n=0;foreach(double z in new[]{z0,z1})foreach(double y in new[]{y0,y1})foreach(double x in new[]{x0,x1})v[n++]=P(x,y,z);
   int[][] quads={new[]{0,1,3,2},new[]{4,5,7,6},new[]{0,1,5,4},new[]{2,3,7,6},new[]{0,2,6,4},new[]{1,3,7,5}};double[] shade={.55,1,.8,.7,.75,.9};
   for(int q=0;q<6;q++){var f=quads[q];faces.Add(new Face{P=f.Select(k=>new PointF((float)v[k][0],(float)-v[k][1])).ToArray(),Depth=f.Average(k=>v[k][2]),C=Color.FromArgb(alpha,(int)(c.R*shade[q]),(int)(c.G*shade[q]),(int)(c.B*shade[q]))});}};
  double depthShown=Math.Min(i.SupportDepth,i.Hef+120);bool plateBad=Failing.Contains("plate"),anchorBad=Failing.Contains("anchors"),gussetBad=Failing.Contains("gusset");
  box(-i.SupportN/2,-i.SupportB/2,-depthShown,i.SupportN/2,i.SupportB/2,0,Color.FromArgb(200,200,195),110);
  box(-i.PlateN/2,-i.PlateB/2,0,i.PlateN/2,i.PlateB/2,i.PlateT,plateBad?Color.FromArgb(230,90,90):Color.FromArgb(120,140,170),255);
  box(-i.GussetL/2,-i.GussetT/2,i.PlateT,i.GussetL/2,i.GussetT/2,i.PlateT+gussetH,gussetBad?Color.FromArgb(230,90,90):Color.FromArgb(150,165,190),255);
  double run=(i.BoltColumns-1)*i.BoltS1,r=i.BoltDia/2.0;for(int c=0;c<i.BoltColumns;c++)foreach(int side in new[]{-1,1}){double x=-run/2+c*i.BoltS1,y=side*i.BoltS2/2;var col=anchorBad?Color.FromArgb(200,40,40):Color.FromArgb(40,40,40);box(x-r,y-r,-i.Hef,x+r,y+r,i.PlateT+30,col,255);box(x-1.6*r,y-1.6*r,i.PlateT,x+1.6*r,y+1.6*r,i.PlateT+1.2*i.BoltDia,col,255);if(!i.Headed)box(x-r,y-r,-i.Hef-2*r,x+r+i.HookEh*(c==0&&i.BoltColumns>1?-1:1)*(1),y+r,-i.Hef,col,255);}
  if(faces.Count==0)return;float minX=faces.Min(f=>f.P.Min(p=>p.X)),maxX=faces.Max(f=>f.P.Max(p=>p.X)),minY=faces.Min(f=>f.P.Min(p=>p.Y)),maxY=faces.Max(f=>f.P.Max(p=>p.Y));float s=Math.Min((area.Width-20)/(maxX-minX),(area.Height-30)/(maxY-minY));float ox=area.Left+area.Width/2-(minX+maxX)/2*s,oy=area.Top+area.Height/2-(minY+maxY)/2*s;
  using(var edge=new Pen(Color.FromArgb(90,40,40,40),.6f))foreach(var f in faces.OrderByDescending(f=>f.Depth)){var pts=f.P.Select(p=>new PointF(ox+p.X*s,oy+p.Y*s)).ToArray();using(var b=new SolidBrush(f.C))g.FillPolygon(b,pts);g.DrawPolygon(edge,pts);}
  g.DrawString("drag to turn  •  double-click to reset",font,muted,area.Left,area.Bottom+6);}
 float planScale=1,elevScale=1;PointF planCenter,elevOrigin;PointF down;int boltColumn;
 Handle HitHandle(PointF p){for(int n=handles.Count-1;n>=0;n--)if(handles[n].Area.Contains(p))return handles[n];return null;}
 protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);Focus();if(e.Button!=MouseButtons.Left)return;active=HitHandle(e.Location);down=e.Location;rotating=active==null&&view3D.Contains(e.Location);if(rotating){Capture=true;return;}if(active!=null&&active.Key=="Bolt")boltColumn=(int)active.Value(e.Location);Capture=active!=null;}
 protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(rotating){yaw+=(e.X-down.X)*.01;pitch=Math.Max(.05,Math.Min(1.45,pitch+(e.Y-down.Y)*.01));down=e.Location;Invalidate();return;}if(active==null){var h=HitHandle(e.Location);Cursor=h!=null?h.Cursor:labels.Any(l=>l.Area.Contains(e.Location))?Cursors.Hand:Cursors.Default;if(h!=hover){hover=h;Invalidate();}return;}
  if(input==null||ValueChanged==null)return;
  if(active.Key=="Bolt"){bool across=Math.Abs(e.Y-down.Y)>=Math.Abs(e.X-down.X);if(across)ValueChanged("BoltS2",Snap(2*Math.Abs(e.Y-planCenter.Y)/planScale));else if(input.BoltColumns>1){double factor=Math.Abs(2*boltColumn-(input.BoltColumns-1));if(factor>0)ValueChanged("BoltS1",Snap(2*Math.Abs(e.X-planCenter.X)/planScale/factor));}return;}
  double v=active.Value(e.Location);if(double.IsNaN(v)||double.IsInfinity(v))return;ValueChanged(active.Key,Snap(v));}
 protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);rotating=false;active=null;Capture=false;Invalidate();}
 protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);if(hover!=null){hover=null;Invalidate();}}
 protected override void OnMouseDoubleClick(MouseEventArgs e){base.OnMouseDoubleClick(e);if(view3D.Contains(e.Location)){yaw=-.75;pitch=.55;Invalidate();return;}var l=labels.FirstOrDefault(x=>x.Area.Contains(e.Location));if(l==null||ValueChanged==null)return;if(editor!=null){Controls.Remove(editor);editor.Dispose();}
  editor=new TextBox{Text=F(l.Current),Left=(int)l.Area.Left-6,Top=(int)l.Area.Top-3,Width=(int)Math.Max(60,l.Area.Width+20)};string key=l.Key;Controls.Add(editor);editor.Focus();editor.SelectAll();
  Action commit=()=>{if(editor==null)return;double v;string text=editor.Text;var box=editor;editor=null;Controls.Remove(box);box.Dispose();if(double.TryParse(text,NumberStyles.Float,Inv,out v)&&v>0)ValueChanged(key,v);};
  editor.KeyDown+=(s,a)=>{if(a.KeyCode==Keys.Enter){a.SuppressKeyPress=true;commit();}else if(a.KeyCode==Keys.Escape){a.SuppressKeyPress=true;var box=editor;editor=null;Controls.Remove(box);box.Dispose();}};editor.LostFocus+=(s,a)=>commit();}
}
}
