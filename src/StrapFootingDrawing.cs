using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* Plan of the strap foundation in the STAAD global axes and the elevation of each strap with its moment and shear diagrams. The
   plan is the view from the top: X to the right; with Y up, +Z points down the sheet (STAAD top view); with Z up, +Y points up. */
static class StrapDrawing {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 static string F(double v,string f="0.00"){return v.ToString(f,Inv);}
 public static PlanMap Plan(Graphics g,RectangleF area,StrapInput inp,StrapCase c,float fontPt,ICollection<string> failing,string selected=null){g.SmoothingMode=SmoothingMode.AntiAlias;if(inp==null||inp.Nodes.Count==0)return null;
  double minX=inp.Nodes.Min(n=>Math.Min(n.X+n.Ex-n.B/2,n.X-n.Cx/2)),maxX=inp.Nodes.Max(n=>Math.Max(n.X+n.Ex+n.B/2,n.X+n.Cx/2)),minQ=inp.Nodes.Min(n=>Math.Min(n.Z+n.Ez-n.L/2,n.Z-n.Cz/2)),maxQ=inp.Nodes.Max(n=>Math.Max(n.Z+n.Ez+n.L/2,n.Z+n.Cz/2));
  float pad=fontPt*5;double k=Math.Min((area.Width-2*pad)/Math.Max(.5,maxX-minX),(area.Height-2*pad)/Math.Max(.5,maxQ-minQ));bool zUp=inp.Vertical=="Z";
  float ox=(float)(area.Left+area.Width/2-(minX+maxX)/2*k),oy=area.Top+area.Height/2;double midQ=(minQ+maxQ)/2;
  var map=new PlanMap{Ox=ox,Oy=oy,K=k,MidQ=midQ,ZUp=zUp};Func<double,double,PointF> P=map.P;
  Func<double,double,double,double,RectangleF> Rect=(cx,cq,w,h)=>{var a=P(cx-w/2,cq-h/2);var b=P(cx+w/2,cq+h/2);return RectangleF.FromLTRB(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y),Math.Max(a.X,b.X),Math.Max(a.Y,b.Y));};
  using(var font=new Font("Segoe UI",fontPt,FontStyle.Regular,GraphicsUnit.Point))using(var bold=new Font("Segoe UI",fontPt,FontStyle.Bold,GraphicsUnit.Point))using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted)){
   /* footings */
   foreach(var n in inp.Nodes){bool bad=failing!=null&&failing.Contains("F"+n.Node);bool sel=selected==n.Node;var r=Rect(n.X+n.Ex,n.Z+n.Ez,n.B,n.L);using(var fill=new SolidBrush(bad?Color.FromArgb(70,220,60,60):sel?Color.FromArgb(205,225,240):Color.FromArgb(225,228,232)))g.FillRectangle(fill,r);using(var edge=new Pen(bad?Color.Firebrick:sel?Color.FromArgb(25,90,160):PowTheme.Ink,sel?2f:1.2f))g.DrawRectangle(edge,r.X,r.Y,r.Width,r.Height);
    string t="F"+n.Node+"  "+F(n.B)+" x "+F(n.L)+" x "+F(n.T*1000,"0");g.DrawString(t,bold,ink,r.Left,r.Top-fontPt*1.7f);
    if(Math.Abs(n.Ex)>1e-4||Math.Abs(n.Ez)>1e-4){var a=P(n.X,n.Z);var b=P(n.X+n.Ex,n.Z+n.Ez);using(var pen=new Pen(Color.FromArgb(200,110,20),1f){DashStyle=DashStyle.Dash,EndCap=LineCap.ArrowAnchor})g.DrawLine(pen,a,b);g.DrawString("e = "+F(n.Ex)+", "+F(n.Ez),font,new SolidBrush(Color.FromArgb(200,110,20)),r.Left,r.Bottom+2+fontPt*1.5f);}
    if(c!=null){int i=inp.Nodes.IndexOf(n);string q="R "+F(c.R[i],"0")+" kN, q "+F(c.Qmin[i],"0")+" to "+F(c.Qmax[i],"0")+" kPa";g.DrawString(q,font,muted,r.Left,r.Bottom+2);}}
   /* straps */
   foreach(var s in inp.Straps){int ia=StrapFooting.Index(inp,s.From),ib=StrapFooting.Index(inp,s.To);if(ia<0||ib<0)continue;var a=inp.Nodes[ia];var b=inp.Nodes[ib];double L=StrapFooting.Length(a,b);if(L<1e-6)continue;double ux=(b.X-a.X)/L,uq=(b.Z-a.Z)/L,nx=-uq,nq=ux,w=s.Width/2;
    var poly=new[]{P(a.X+nx*w,a.Z+nq*w),P(b.X+nx*w,b.Z+nq*w),P(b.X-nx*w,b.Z-nq*w),P(a.X-nx*w,a.Z-nq*w)};bool bad=failing!=null&&failing.Contains("S"+s.From+"-"+s.To);using(var fill=new SolidBrush(bad?Color.FromArgb(120,220,60,60):Color.FromArgb(150,40,110,170)))g.FillPolygon(fill,poly);using(var edge=new Pen(Color.FromArgb(30,80,140),1f))g.DrawPolygon(edge,poly);
    var mid=P((a.X+b.X)/2,(a.Z+b.Z)/2);string t="S"+s.From+"-"+s.To+"  "+F(s.Width*1000,"0")+" x "+F(s.Depth*1000,"0")+", L = "+F(L);var sz=g.MeasureString(t,font);/* label beside the strap */var side=P((a.X+b.X)/2+nx*(w+.05),(a.Z+b.Z)/2+nq*(w+.05));mid=P(a.X+(b.X-a.X)*.55,a.Z+(b.Z-a.Z)*.55);bool vertical=Math.Abs(ux)<Math.Abs(uq);var other=P((a.X+b.X)/2-nx*(w+.05),(a.Z+b.Z)/2-nq*(w+.05));float lx=vertical?Math.Max(area.Left+2,Math.Min(side.X,other.X)-sz.Width-4):mid.X-sz.Width/2,ly=vertical?mid.Y-sz.Height/2:Math.Min(side.Y,mid.Y)-sz.Height-2;using(var bg=new SolidBrush(Color.FromArgb(225,255,255,255)))g.FillRectangle(bg,lx,ly,sz.Width,sz.Height);g.DrawString(t,font,new SolidBrush(Color.FromArgb(30,80,140)),lx,ly);}
   /* columns and nodes */
   foreach(var n in inp.Nodes){var r=Rect(n.X,n.Z,n.Cx,n.Cz);g.FillRectangle(Brushes.DimGray,r);g.DrawString("N"+n.Node,bold,ink,r.Right+2,r.Bottom);}
   /* STAAD axes */
   float ax=area.Left+14,ay=zUp?area.Top+48:area.Top+14;using(var pen=new Pen(PowTheme.Ink,1.2f){EndCap=LineCap.ArrowAnchor}){g.DrawLine(pen,ax,ay,ax+34,ay);g.DrawLine(pen,ax,ay,ax,zUp?ay-34:ay+34);}
   g.DrawString("X",bold,ink,ax+36,ay-fontPt);g.DrawString(zUp?"Y":"Z",bold,ink,ax+4,zUp?ay-40:ay+22);
   g.DrawString("STAAD global axes, "+(zUp?"Z":"Y")+" up - plan from the top",font,muted,ax+50,ay-fontPt*.7f);}return map;}
 /* elevation along strap s with the factored moment and shear envelopes */
 public static void Strap(Graphics g,RectangleF area,StrapInput inp,int s,List<StrapCase> cases,float fontPt){g.SmoothingMode=SmoothingMode.AntiAlias;var st=inp.Straps[s];var a=inp.Nodes[StrapFooting.Index(inp,st.From)];var b=inp.Nodes[StrapFooting.Index(inp,st.To)];double L=StrapFooting.Length(a,b);double ux=(b.X-a.X)/L,uq=(b.Z-a.Z)/L;
  /* footing extents along the strap: projection of each footing on the strap axis */
  Func<StrapNode,double,double[]> ext=(n,s0)=>{double c=(n.X+n.Ex-a.X)*ux+(n.Z+n.Ez-a.Z)*uq;double half=(Math.Abs(ux)*n.B+Math.Abs(uq)*n.L)/2;return new[]{c-half,c+half};};var ea=ext(a,0);var eb=ext(b,0);double x0=Math.Min(ea[0],0)-.3,x1=Math.Max(eb[1],L)+.3;
  float left=area.Left+50,width=area.Width-70;Func<double,float> X=x=>(float)(left+(x-x0)/(x1-x0)*width);
  using(var font=new Font("Segoe UI",fontPt,FontStyle.Regular,GraphicsUnit.Point))using(var bold=new Font("Segoe UI",fontPt,FontStyle.Bold,GraphicsUnit.Point))using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted))using(var dim=new Pen(PowTheme.Muted,.6f)){
   g.DrawString("STRAP "+st.From+" - "+st.To+"  ("+F(st.Width*1000,"0")+" x "+F(st.Depth*1000,"0")+")",bold,ink,area.Left+6,area.Top+4);
   float ground=area.Top+30,elevH=area.Height*.34f;double maxDepth=Math.Max(inp.Df,Math.Max(a.T,b.T)+st.Depth)+.2;float vk=(float)(elevH/maxDepth);Func<double,float> Y=d=>ground+(float)(d*vk);
   using(var gp=new Pen(Color.FromArgb(120,90,60),1f){DashStyle=DashStyle.Dash})g.DrawLine(gp,X(x0),ground,X(x1),ground);g.DrawString("grade",font,muted,X(x0),ground-fontPt*1.6f);
   foreach(var pair in new[]{new{N=a,E=ea},new{N=b,E=eb}}){var n=pair.N;float top=Y(inp.Df-n.T),bot=Y(inp.Df);using(var fill=new SolidBrush(Color.FromArgb(225,228,232)))g.FillRectangle(fill,X(pair.E[0]),top,X(pair.E[1])-X(pair.E[0]),bot-top);g.DrawRectangle(Pens.Black,X(pair.E[0]),top,X(pair.E[1])-X(pair.E[0]),bot-top);
    double cx=(n.X-a.X)*ux+(n.Z-a.Z)*uq;double cw=(Math.Abs(ux)*n.Cx+Math.Abs(uq)*n.Cz);g.FillRectangle(Brushes.DimGray,X(cx-cw/2),ground-6,X(cx+cw/2)-X(cx-cw/2),top-ground+6);g.DrawString("F"+n.Node,bold,ink,X(pair.E[0])+2,bot+2);
    using(var arrow=new Pen(PowTheme.Ink,1.4f){EndCap=LineCap.ArrowAnchor})g.DrawLine(arrow,X(cx),ground-26,X(cx),ground-8);}
   {/* strap bottom flush with the footing bottoms */float top=Y(inp.Df-st.Depth),bot=Y(inp.Df);
    using(var fill=new SolidBrush(Color.FromArgb(120,40,110,170)))g.FillRectangle(fill,X(0),top,X(L)-X(0),bot-top);g.DrawRectangle(Pens.SteelBlue,X(0),top,X(L)-X(0),bot-top);}
   float dy=Y(maxDepth)+4;g.DrawLine(dim,X(0),dy,X(L),dy);g.DrawLine(dim,X(0),dy-3,X(0),dy+3);g.DrawLine(dim,X(L),dy-3,X(L),dy+3);var lt="L = "+F(L*1000,"0")+" mm";var lsz=g.MeasureString(lt,font);g.DrawString(lt,font,muted,(X(0)+X(L))/2-lsz.Width/2,dy-lsz.Height);
   /* diagrams */
   var fac=cases.Where(c=>c.Factored).ToList();if(fac.Count==0)return;int nPts=60;var mMax=new double[nPts+1];var mMin=new double[nPts+1];for(int k=0;k<=nPts;k++){double x=L*k/nPts;mMax[k]=fac.Max(c=>StrapFooting.Moment(c,s,L,x));mMin[k]=fac.Min(c=>StrapFooting.Moment(c,s,L,x));}
   double mAbs=Math.Max(1e-6,Math.Max(mMax.Max(),-mMin.Min()));var mBox=new RectangleF(area.Left,dy+14,area.Width,(area.Bottom-dy-20)*.55f);float mid=mBox.Top+mBox.Height/2;float mk=(float)(mBox.Height/2.2/mAbs);
   g.DrawString("Mu (kN·m)",font,muted,area.Left+6,mid-fontPt);g.DrawString("factored envelope, hogging up",font,muted,area.Left+6,mid+2);g.DrawLine(Pens.Gray,X(0),mid,X(L),mid);
   foreach(var arr in new[]{mMax,mMin}){var pts=Enumerable.Range(0,nPts+1).Select(k=>new PointF(X(L*k/nPts),mid+(float)(arr[k]*mk))).ToList();var poly=new List<PointF>{new PointF(X(0),mid)};poly.AddRange(pts);poly.Add(new PointF(X(L),mid));using(var fill=new SolidBrush(Color.FromArgb(60,200,60,60)))g.FillPolygon(fill,poly.ToArray());using(var pen=new Pen(Color.Firebrick,1.2f))g.DrawLines(pen,pts.ToArray());}
   int iMax=Array.IndexOf(mMax,mMax.Max()),iMin=Array.IndexOf(mMin,mMin.Min());if(mMin[iMin]<0)g.DrawString(F(mMin[iMin],"0.0"),bold,Brushes.Firebrick,X(L*iMin/nPts)+3,mid+(float)(mMin[iMin]*mk)-fontPt*1.4f);if(mMax[iMax]>0)g.DrawString("+"+F(mMax[iMax],"0.0"),bold,Brushes.Firebrick,X(L*iMax/nPts)+3,mid+(float)(mMax[iMax]*mk));
   var vBox=new RectangleF(area.Left,mBox.Bottom+12,area.Width,area.Bottom-mBox.Bottom-16);float vmid=vBox.Top+vBox.Height/2;var vEnv=new double[nPts+1];for(int k=0;k<=nPts;k++){double x=L*k/nPts;vEnv[k]=fac.Select(c=>c.Shear[s]+c.W[s]*(L/2-x)).OrderByDescending(v=>Math.Abs(v)).First();}double vAbs=Math.Max(1e-6,vEnv.Max(v=>Math.Abs(v)));float vk2=(float)(vBox.Height/2.2/vAbs);
   g.DrawString("Vu (kN)",font,muted,area.Left+6,vBox.Top-2);g.DrawLine(Pens.Gray,X(0),vmid,X(L),vmid);var vp=Enumerable.Range(0,nPts+1).Select(k=>new PointF(X(L*k/nPts),vmid-(float)(vEnv[k]*vk2))).ToList();var vpoly=new List<PointF>{new PointF(X(0),vmid)};vpoly.AddRange(vp);vpoly.Add(new PointF(X(L),vmid));using(var fill=new SolidBrush(Color.FromArgb(60,40,110,170)))g.FillPolygon(fill,vpoly.ToArray());using(var pen=new Pen(Color.SteelBlue,1.2f))g.DrawLines(pen,vp.ToArray());
   g.DrawString(F(vEnv[0],"0.0"),bold,Brushes.SteelBlue,X(0)+3,vmid-(float)(vEnv[0]*vk2)-(vEnv[0]>0?fontPt*1.5f:0));}}
}
class PlanMap {public float Ox,Oy;public double K,MidQ;public bool ZUp;
 public PointF P(double x,double q){return new PointF((float)(Ox+x*K),(float)(Oy+(ZUp?-(q-MidQ):(q-MidQ))*K));}
 public double[] Inverse(PointF p){double x=(p.X-Ox)/K,d=(p.Y-Oy)/K;return new[]{x,ZUp?MidQ-d:MidQ+d};}}
}
