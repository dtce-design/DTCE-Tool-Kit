using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* 3D view and footing details of the strap foundation (owner, 19 November 2026: "Full compliant kay NSCP 2015 interactive may
   visualization pati report. Yung magandang visuals") */
static class StrapVisuals {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 static string F(double v,string f="0"){return v.ToString(f,Inv);}
 sealed class Box {public double[][] C;public Color Color;public string Label;public double Depth;}
 /* corners of a box: centre (x, z), half sizes along u and across, y from y0 to y1 */
 static double[][] Corners(double cx,double cz,double ux,double uz,double half,double halfW,double y0,double y1){var list=new List<double[]>();foreach(double y in new[]{y0,y1})foreach(var s in new[]{new[]{-1,-1},new[]{1,-1},new[]{1,1},new[]{-1,1}})list.Add(new[]{cx+s[0]*half*ux-s[1]*halfW*uz,y,cz+s[0]*half*uz+s[1]*halfW*ux});return list.ToArray();}
 static readonly int[][] Faces={new[]{4,5,6,7},new[]{0,1,5,4},new[]{1,2,6,5},new[]{2,3,7,6},new[]{3,0,4,7},new[]{0,3,2,1}};
 /* isometric view, Y up (plan q drawn as Z); failing parts red */
 public static void Iso(Graphics g,RectangleF area,StrapInput inp,ICollection<string> failing,float fontPt){g.SmoothingMode=SmoothingMode.AntiAlias;if(inp==null||inp.Nodes.Count==0)return;bool zUp=inp.Vertical=="Z";double sq=zUp?-1:1;
  var boxes=new List<Box>();double top=-inp.Df;
  foreach(var n in inp.Nodes){bool bad=failing!=null&&failing.Contains("F"+n.Node);boxes.Add(new Box{C=Corners(n.X+n.Ex,sq*(n.Z+n.Ez),1,0,n.B/2,n.L/2,-inp.Df,-inp.Df+n.T),Color=bad?Color.FromArgb(225,120,120):Color.FromArgb(200,204,210),Label="F"+n.Node});
   boxes.Add(new Box{C=Corners(n.X,sq*n.Z,1,0,n.Cx/2,n.Cz/2,-inp.Df+n.T,.6),Color=Color.FromArgb(120,124,132),Label="N"+n.Node});}
  foreach(var st in inp.Straps){int ia=StrapFooting.Index(inp,st.From),ib=StrapFooting.Index(inp,st.To);if(ia<0||ib<0)continue;var a=inp.Nodes[ia];var b=inp.Nodes[ib];double L=StrapFooting.Length(a,b);if(L<1e-6)continue;double ux=(b.X-a.X)/L,uz=sq*(b.Z-a.Z)/L;bool bad=failing!=null&&failing.Contains("S"+st.From+"-"+st.To);
   boxes.Add(new Box{C=Corners((a.X+b.X)/2,sq*(a.Z+b.Z)/2,ux,uz,L/2,st.Width/2,-inp.Df,-inp.Df+st.Depth),Color=bad?Color.FromArgb(225,110,110):Color.FromArgb(90,140,200),Label="S"+st.From+"-"+st.To});}
  Func<double[],double[]> proj=p=>new[]{(p[0]-p[2])*.866,(p[0]+p[2])*.5-p[1]};
  var all=boxes.SelectMany(bx=>bx.C).Select(proj).ToList();double minX=all.Min(p=>p[0]),maxX=all.Max(p=>p[0]),minY=all.Min(p=>p[1]),maxY=all.Max(p=>p[1]);double k=Math.Min((area.Width-40)/Math.Max(.5,maxX-minX),(area.Height-40)/Math.Max(.5,maxY-minY));
  Func<double[],PointF> S=p=>{var q=proj(p);return new PointF((float)(area.Left+20+(q[0]-minX)*k+((area.Width-40)-(maxX-minX)*k)/2),(float)(area.Top+20+(q[1]-minY)*k));};
  /* grade plane outline */
  {double gx0=inp.Nodes.Min(n=>n.X+n.Ex-n.B/2)-.5,gx1=inp.Nodes.Max(n=>n.X+n.Ex+n.B/2)+.5,gz0=inp.Nodes.Min(n=>sq*(n.Z+n.Ez)-n.L/2)-.5,gz1=inp.Nodes.Max(n=>sq*(n.Z+n.Ez)+n.L/2)+.5;var gp=new[]{S(new[]{gx0,0,gz0}),S(new[]{gx1,0,gz0}),S(new[]{gx1,0,gz1}),S(new[]{gx0,0,gz1})};using(var b=new SolidBrush(Color.FromArgb(28,140,110,70)))g.FillPolygon(b,gp);using(var pen=new Pen(Color.FromArgb(120,140,110,70),.8f){DashStyle=DashStyle.Dash})g.DrawPolygon(pen,gp);}
  foreach(var bx in boxes)bx.Depth=bx.C.Average(p=>p[0]+p[1]+p[2]);
  using(var font=new Font("Segoe UI",fontPt,FontStyle.Bold,GraphicsUnit.Point))foreach(var bx in boxes.OrderBy(b=>b.Depth)){foreach(var f in Faces){var pts=f.Select(i=>bx.C[i]).ToArray();var e1=Sub(pts[1],pts[0]);var e2=Sub(pts[2],pts[0]);var nrm=new[]{e1[1]*e2[2]-e1[2]*e2[1],e1[2]*e2[0]-e1[0]*e2[2],e1[0]*e2[1]-e1[1]*e2[0]};var ctr=new[]{bx.C.Average(p=>p[0]),bx.C.Average(p=>p[1]),bx.C.Average(p=>p[2])};var fc=new[]{pts.Average(p=>p[0])-ctr[0],pts.Average(p=>p[1])-ctr[1],pts.Average(p=>p[2])-ctr[2]};if(nrm[0]*fc[0]+nrm[1]*fc[1]+nrm[2]*fc[2]<0){nrm=new[]{-nrm[0],-nrm[1],-nrm[2]};}
    if(nrm[0]+nrm[1]+nrm[2]<=1e-9)continue;double len=Math.Sqrt(nrm[0]*nrm[0]+nrm[1]*nrm[1]+nrm[2]*nrm[2]);double shade=nrm[1]/len>.5?1.0:nrm[0]/len>.3?.82:.66;var col=Color.FromArgb((int)(bx.Color.R*shade),(int)(bx.Color.G*shade),(int)(bx.Color.B*shade));var poly=pts.Select(S).ToArray();using(var b=new SolidBrush(col))g.FillPolygon(b,poly);using(var pen=new Pen(Color.FromArgb(70,70,80),.6f))g.DrawPolygon(pen,poly);}
   if(bx.Label.StartsWith("F")||bx.Label.StartsWith("S")){var topc=S(new[]{bx.C.Average(p=>p[0]),bx.C.Max(p=>p[1]),bx.C.Average(p=>p[2])});var sz=g.MeasureString(bx.Label,font);g.DrawString(bx.Label,font,Brushes.Black,topc.X-sz.Width/2,topc.Y-sz.Height/2);}}
  using(var small=new Font("Segoe UI",fontPt,FontStyle.Regular,GraphicsUnit.Point))g.DrawString("3D view (STAAD axes, "+(zUp?"Z":"Y")+" up); grade shaded; straps clear of the soil",small,new SolidBrush(PowTheme.Muted),area.Left+6,area.Bottom-fontPt*2);}
 static double[] Sub(double[] a,double[] b){return new[]{a[0]-b[0],a[1]-b[1],a[2]-b[2]};}
 /* footing detail: plan with the bars both ways and a section along X with bars, cover and column */
 public static void Footing(Graphics g,RectangleF area,StrapInput inp,StrapNode n,float fontPt){g.SmoothingMode=SmoothingMode.AntiAlias;var bars=StrapFooting.ParseBars(n.Bars);
  using(var font=new Font("Segoe UI",fontPt,FontStyle.Regular,GraphicsUnit.Point))using(var bold=new Font("Segoe UI",fontPt,FontStyle.Bold,GraphicsUnit.Point))using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted))using(var dim=new Pen(PowTheme.Muted,.6f)){
   string q=inp.Vertical=="Z"?"Y":"Z";g.DrawString("FOOTING F"+n.Node+"  -  "+F(n.B,"0.00")+" x "+F(n.L,"0.00")+" x "+F(n.T*1000)+" mm, Ø"+bars.Dia+" @ "+F(bars.Spacing)+" both ways, bottom",bold,ink,area.Left+6,area.Top+4);
   /* plan */
   var pa=new RectangleF(area.Left+20,area.Top+26,area.Width*.42f,area.Height-46);double k=Math.Min(pa.Width/n.B,pa.Height/n.L);float w=(float)(n.B*k),h=(float)(n.L*k),x0=pa.Left+(pa.Width-w)/2,y0=pa.Top+(pa.Height-h)/2;
   using(var fill=new SolidBrush(Color.FromArgb(232,234,238)))g.FillRectangle(fill,x0,y0,w,h);g.DrawRectangle(Pens.Black,x0,y0,w,h);float c=(float)(inp.Cover/1000*k);
   using(var bp=new Pen(Color.FromArgb(30,100,190),.7f)){for(double s=inp.Cover/1000+bars.Dia/2000.0;s<=n.L-inp.Cover/1000;s+=bars.Spacing/1000)g.DrawLine(bp,x0+c,y0+(float)(s*k),x0+w-c,y0+(float)(s*k));}
   using(var bp=new Pen(Color.FromArgb(200,45,45),.7f)){for(double s=inp.Cover/1000+bars.Dia/2000.0;s<=n.B-inp.Cover/1000;s+=bars.Spacing/1000)g.DrawLine(bp,x0+(float)(s*k),y0+c,x0+(float)(s*k),y0+h-c);}
   float colX=x0+w/2-(float)(n.Ex*k),colY=y0+h/2-(float)((inp.Vertical=="Z"?-n.Ez:n.Ez)*k);float cw=(float)(n.Cx*k),ch=(float)(n.Cz*k);g.FillRectangle(Brushes.DimGray,colX-cw/2,colY-ch/2,cw,ch);
   g.DrawLine(dim,x0,y0+h+10,x0+w,y0+h+10);g.DrawString("B = "+F(n.B*1000),font,muted,x0+w/2-20,y0+h+11);var st=g.Save();g.TranslateTransform(x0-12,y0+h/2);g.RotateTransform(-90);g.DrawString("L = "+F(n.L*1000),font,muted,-20,-12);g.Restore(st);g.DrawLine(dim,x0-6,y0,x0-6,y0+h);
   if(Math.Abs(n.Ex)>1e-4||Math.Abs(n.Ez)>1e-4)g.DrawString("column off-centre ex "+F(n.Ex*1000)+", e"+q.ToLowerInvariant()+" "+F(n.Ez*1000)+" mm",font,new SolidBrush(Color.FromArgb(200,110,20)),x0,y0-fontPt*1.6f);
   g.DrawString("bars along X (red) and along "+q+" (blue)",font,muted,pa.Left,pa.Bottom+6);
   /* section along X */
   var sa=new RectangleF(area.Left+area.Width*.5f,area.Top+26,area.Width*.48f,area.Height-46);double ks=Math.Min(sa.Width/n.B,(sa.Height-40)/(n.T+.5));float sw=(float)(n.B*ks),sh=(float)(n.T*ks),sx=sa.Left+(sa.Width-sw)/2,sy=sa.Bottom-sh-24;
   using(var fill=new HatchBrush(HatchStyle.Percent10,Color.Gray,Color.FromArgb(236,236,232)))g.FillRectangle(fill,sx,sy,sw,sh);g.DrawRectangle(Pens.Black,sx,sy,sw,sh);float scol=sx+sw/2-(float)(n.Ex*ks),scw=(float)(n.Cx*ks);g.FillRectangle(Brushes.DimGray,scol-scw/2,sy-(float)(.5*ks),scw,(float)(.5*ks));
   float cv=(float)(inp.Cover/1000*ks);using(var bp=new Pen(Color.FromArgb(200,45,45),Math.Max(1f,(float)(bars.Dia/1000.0*ks))))g.DrawLine(bp,sx+cv,sy+sh-cv,sx+sw-cv,sy+sh-cv);
   using(var dot=new SolidBrush(Color.FromArgb(30,100,190))){float d=(float)Math.Max(2,bars.Dia/1000.0*ks);for(double s=inp.Cover/1000;s<=n.B-inp.Cover/1000;s+=bars.Spacing/1000)g.FillEllipse(dot,sx+(float)(s*ks)-d/2,sy+sh-cv-d*1.5f,d,d);}
   g.DrawLine(dim,sx+sw+8,sy,sx+sw+8,sy+sh);g.DrawString("h "+F(n.T*1000),font,muted,sx+sw+10,sy+sh/2-6);g.DrawString("cover "+F(inp.Cover),font,muted,sx+sw+10,sy+sh-12);g.DrawLine(dim,sx,sy+sh+10,sx+sw,sy+sh+10);g.DrawString("B = "+F(n.B*1000),font,muted,sx+sw/2-20,sy+sh+11);
   g.DrawString("SECTION ALONG X",bold,ink,sa.Left,sa.Top);g.DrawString("bottom bars: along X (red, lower), along "+q+" (blue dots, upper)",font,muted,sa.Left,sa.Top+fontPt*1.6f);}}
}
/* interactive plan / 3D view: drag a footing to move it off its column (ex, ez, 25 mm steps); click to select its row */
class StrapPlanView:Control {internal StrapInput Input;internal StrapCase Case;internal readonly HashSet<string> Failing=new HashSet<string>();internal bool ThreeD;internal string Selected;
 internal event Action<string,double,double> FootingMoved;internal event Action<string> FootingSelected;PlanMap map;string dragging;PointF dragStart;double ex0,ez0;
 public StrapPlanView(){DoubleBuffered=true;ResizeRedraw=true;BackColor=Color.White;Width=1000;Height=460;Tag="FullWidth";}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(Input==null){TextRenderer.DrawText(e.Graphics,"Enter the footings and straps to see the plan.",ModernUi.Body,new Point(12,12),PowTheme.Muted);return;}
  try{var r=new RectangleF(0,0,Width,Height-26);if(ThreeD){map=null;StrapVisuals.Iso(e.Graphics,r,Input,Failing,8.5f);}else map=StrapDrawing.Plan(e.Graphics,r,Input,Case,8.5f,Failing,Selected);
   TextRenderer.DrawText(e.Graphics,ThreeD?"3D view - double-click for the plan":"Plan - drag a footing to change its eccentricity (25 mm steps), click to select its row, double-click for 3D",ModernUi.Body,new Point(10,Height-24),PowTheme.Muted);}
  catch(Exception ex){TextRenderer.DrawText(e.Graphics,ex.Message,ModernUi.Body,new Point(12,12),Color.Firebrick);}}
 StrapNode Hit(PointF p){if(map==null||Input==null)return null;var w=map.Inverse(p);return Input.Nodes.LastOrDefault(n=>Math.Abs(w[0]-(n.X+n.Ex))<=n.B/2&&Math.Abs(w[1]-(n.Z+n.Ez))<=n.L/2);}
 protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);var n=Hit(e.Location);if(n==null)return;dragging=n.Node;dragStart=e.Location;ex0=n.Ex;ez0=n.Ez;Selected=n.Node;if(FootingSelected!=null)FootingSelected(n.Node);Invalidate();}
 protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging==null){Cursor=Hit(e.Location)!=null?Cursors.SizeAll:Cursors.Default;return;}if(map==null)return;var a=map.Inverse(dragStart);var b=map.Inverse(e.Location);var n=Input.Nodes.FirstOrDefault(x=>x.Node==dragging);if(n==null)return;
  double ex=Math.Round((ex0+b[0]-a[0])/.025)*.025,ez=Math.Round((ez0+b[1]-a[1])/.025)*.025;double mx=Math.Max(0,n.B/2-n.Cx/2),mz=Math.Max(0,n.L/2-n.Cz/2);ex=Math.Max(-mx,Math.Min(mx,ex));ez=Math.Max(-mz,Math.Min(mz,ez));if(Math.Abs(ex-n.Ex)<1e-9&&Math.Abs(ez-n.Ez)<1e-9)return;n.Ex=ex;n.Ez=ez;Invalidate();if(FootingMoved!=null)FootingMoved(n.Node,ex,ez);}
 protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=null;}
 protected override void OnDoubleClick(EventArgs e){base.OnDoubleClick(e);ThreeD=!ThreeD;Invalidate();}
}
}
