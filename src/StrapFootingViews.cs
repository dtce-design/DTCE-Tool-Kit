using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* 3D view and footing details of the strap foundation (owner, 19 November 2026: "Full compliant kay NSCP 2015 interactive may
   visualization pati report. Yung magandang visuals") */
class View3D {public double Yaw=45,Pitch=35,Zoom=1;public float PanX,PanY;public bool Rebar;}
static class StrapVisuals {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 static string F(double v,string f="0"){return v.ToString(f,Inv);}
 sealed class Box {public double[][] C;public Color Color;public Color? Top;public string Label;public double Depth;}
 /* corners of a box: centre (x, z), half sizes along u and across, y from y0 to y1 */
 static double[][] Corners(double cx,double cz,double ux,double uz,double half,double halfW,double y0,double y1){var list=new List<double[]>();foreach(double y in new[]{y0,y1})foreach(var s in new[]{new[]{-1,-1},new[]{1,-1},new[]{1,1},new[]{-1,1}})list.Add(new[]{cx+s[0]*half*ux-s[1]*halfW*uz,y,cz+s[0]*half*uz+s[1]*halfW*ux});return list.ToArray();}
 static readonly int[][] Faces={new[]{4,5,6,7},new[]{0,1,5,4},new[]{1,2,6,5},new[]{2,3,7,6},new[]{3,0,4,7},new[]{0,3,2,1}};
 /* isometric view, Y up (plan q drawn as Z); failing parts red */
 /* pressure colour ramp: q / qa from light blue (low) through green and yellow to red (over qa) */
 internal static Color Ramp(double t){var stops=new[]{new{T=0.0,C=Color.FromArgb(205,225,245)},new{T=.5,C=Color.FromArgb(165,215,170)},new{T=.85,C=Color.FromArgb(250,222,120)},new{T=1.0,C=Color.FromArgb(245,160,80)},new{T=1.15,C=Color.FromArgb(220,70,70)}};if(double.IsNaN(t))t=0;t=Math.Max(0,Math.Min(1.15,t));for(int i=1;i<stops.Length;i++)if(t<=stops[i].T){var a=stops[i-1];var b=stops[i];double f=(t-a.T)/(b.T-a.T);return Color.FromArgb((int)(a.C.R+(b.C.R-a.C.R)*f),(int)(a.C.G+(b.C.G-a.C.G)*f),(int)(a.C.B+(b.C.B-a.C.B)*f));}return stops[stops.Length-1].C;}
 internal static void RampLegend(Graphics g,float x,float y,float w,Font f,string title){g.DrawString(title,f,new SolidBrush(PowTheme.Muted),x,y-f.GetHeight(g)-1);for(int i=0;i<(int)w;i++){using(var b=new SolidBrush(Ramp(1.15*i/w)))g.FillRectangle(b,x+i,y,1.2f,8);}g.DrawRectangle(Pens.Gray,x,y,w,8);foreach(var t in new[]{0.0,.5,1.0}){float px=x+(float)(t/1.15*w);g.DrawLine(Pens.Gray,px,y+8,px,y+11);g.DrawString(t==1?"qa":(t*100).ToString("0",Inv)+"%",f,new SolidBrush(PowTheme.Muted),px-8,y+11);}}
 /* 3D view: yaw / pitch camera (drag to turn in the app), optional reinforcement inside see-through concrete, footing tops
    coloured by the soil pressure of the shown load case */
 public static void Iso(Graphics g,RectangleF area,StrapInput inp,ICollection<string> failing,float fontPt,StrapCase c=null,View3D v=null){g.SmoothingMode=SmoothingMode.AntiAlias;if(inp==null||inp.Nodes.Count==0)return;if(v==null)v=new View3D();bool zUp=inp.Vertical=="Z";double sq=zUp?-1:1;
  double th=v.Yaw*Math.PI/180,ph=v.Pitch*Math.PI/180;Func<double[],double[]> cam=p=>{double xr=p[0]*Math.Cos(th)-p[2]*Math.Sin(th),zr=p[0]*Math.Sin(th)+p[2]*Math.Cos(th);return new[]{xr,zr*Math.Sin(ph)-p[1]*Math.Cos(ph),zr*Math.Cos(ph)+p[1]*Math.Sin(ph)};};
  var boxes=new List<Box>();
  foreach(var n in inp.Nodes){bool bad=failing!=null&&failing.Contains("F"+n.Node);var bx=new Box{C=Corners(n.X+n.Ex,sq*(n.Z+n.Ez),1,0,n.B/2,n.L/2,-inp.Df,-inp.Df+n.T),Color=bad?Color.FromArgb(225,120,120):Color.FromArgb(200,204,210),Label="F"+n.Node};
   if(c!=null&&!v.Rebar){int i=inp.Nodes.IndexOf(n);bx.Top=Ramp(c.Qmax[i]/inp.Qa);}boxes.Add(bx);
   boxes.Add(new Box{C=Corners(n.X,sq*n.Z,1,0,n.Cx/2,n.Cz/2,-inp.Df+n.T,.6),Color=Color.FromArgb(120,124,132),Label="N"+n.Node});}
  foreach(var st in inp.Straps){int ia=StrapFooting.Index(inp,st.From),ib=StrapFooting.Index(inp,st.To);if(ia<0||ib<0)continue;var a=inp.Nodes[ia];var b=inp.Nodes[ib];double L=StrapFooting.Length(a,b);if(L<1e-6)continue;double ux=(b.X-a.X)/L,uz=sq*(b.Z-a.Z)/L;bool bad=failing!=null&&failing.Contains("S"+st.From+"-"+st.To);
   boxes.Add(new Box{C=Corners((a.X+b.X)/2,sq*(a.Z+b.Z)/2,ux,uz,L/2,st.Width/2,-inp.Df,-inp.Df+st.Depth),Color=bad?Color.FromArgb(225,110,110):Color.FromArgb(90,140,200),Label="S"+st.From+"-"+st.To});}
  var all=boxes.SelectMany(bx=>bx.C).Select(cam).ToList();double minX=all.Min(p=>p[0]),maxX=all.Max(p=>p[0]),minY=all.Min(p=>p[1]),maxY=all.Max(p=>p[1]);double k=v.Zoom*Math.Min((area.Width-40)/Math.Max(.5,maxX-minX),(area.Height-50)/Math.Max(.5,maxY-minY));
  float cxs=area.Left+area.Width/2+v.PanX,cys=area.Top+(area.Height-20)/2+v.PanY;double mx=(minX+maxX)/2,my=(minY+maxY)/2;Func<double[],PointF> S=p=>{var q=cam(p);return new PointF((float)(cxs+(q[0]-mx)*k),(float)(cys+(q[1]-my)*k));};
  var clip=g.Clip;g.SetClip(area);
  /* grade plane */
  {double gx0=inp.Nodes.Min(n=>n.X+n.Ex-n.B/2)-.6,gx1=inp.Nodes.Max(n=>n.X+n.Ex+n.B/2)+.6,gz0=inp.Nodes.Min(n=>sq*(n.Z+n.Ez)-n.L/2)-.6,gz1=inp.Nodes.Max(n=>sq*(n.Z+n.Ez)+n.L/2)+.6;var gp=new[]{S(new[]{gx0,0,gz0}),S(new[]{gx1,0,gz0}),S(new[]{gx1,0,gz1}),S(new[]{gx0,0,gz1})};using(var b=new SolidBrush(Color.FromArgb(v.Rebar?14:24,140,110,70)))g.FillPolygon(b,gp);using(var pen=new Pen(Color.FromArgb(110,140,110,70),.8f){DashStyle=DashStyle.Dash})g.DrawPolygon(pen,gp);}
  foreach(var bx in boxes)bx.Depth=bx.C.Select(cam).Average(p=>p[2]);int alpha=v.Rebar?70:255;
  using(var font=new Font("Segoe UI",fontPt,FontStyle.Bold,GraphicsUnit.Point)){
   foreach(var bx in boxes.OrderBy(b=>b.Depth)){var ctr=new[]{bx.C.Average(p=>p[0]),bx.C.Average(p=>p[1]),bx.C.Average(p=>p[2])};
    foreach(var f in Faces){var pts=f.Select(i=>bx.C[i]).ToArray();var e1=Sub(pts[1],pts[0]);var e2=Sub(pts[2],pts[0]);var nrm=new[]{e1[1]*e2[2]-e1[2]*e2[1],e1[2]*e2[0]-e1[0]*e2[2],e1[0]*e2[1]-e1[1]*e2[0]};var fc=new[]{pts.Average(p=>p[0])-ctr[0],pts.Average(p=>p[1])-ctr[1],pts.Average(p=>p[2])-ctr[2]};if(nrm[0]*fc[0]+nrm[1]*fc[1]+nrm[2]*fc[2]<0)nrm=new[]{-nrm[0],-nrm[1],-nrm[2]};
     double len=Math.Sqrt(nrm[0]*nrm[0]+nrm[1]*nrm[1]+nrm[2]*nrm[2]);var nc=cam(new[]{nrm[0]/len,nrm[1]/len,nrm[2]/len});if(nc[2]<=1e-9&&!v.Rebar)continue;
     bool topFace=nrm[1]/len>.5;double light=Math.Max(.45,Math.Min(1,.55+.45*(-nc[1]*.6+nc[2]*.5+nc[0]*.2)));var basec=topFace&&bx.Top.HasValue?bx.Top.Value:bx.Color;var col=Color.FromArgb(alpha,(int)(basec.R*light),(int)(basec.G*light),(int)(basec.B*light));
     var poly=pts.Select(S).ToArray();using(var b=new SolidBrush(col))g.FillPolygon(b,poly);using(var pen=new Pen(Color.FromArgb(v.Rebar?110:160,60,60,70),.6f))g.DrawPolygon(pen,poly);}
    if(bx.Label.StartsWith("F")||bx.Label.StartsWith("S")){var topc=S(new[]{ctr[0],bx.C.Max(p=>p[1]),ctr[2]});var sz=g.MeasureString(bx.Label,font);using(var bg=new SolidBrush(Color.FromArgb(200,255,255,255)))g.FillRectangle(bg,topc.X-sz.Width/2,topc.Y-sz.Height/2,sz.Width,sz.Height);g.DrawString(bx.Label,font,Brushes.Black,topc.X-sz.Width/2,topc.Y-sz.Height/2);}}
   if(v.Rebar)DrawRebar(g,inp,S,sq);}
  g.Clip=clip;
  using(var small=new Font("Segoe UI",fontPt,FontStyle.Regular,GraphicsUnit.Point)){g.DrawString("3D (STAAD axes, "+(zUp?"Z":"Y")+" up)"+(v.Rebar?" - concrete see-through, reinforcement shown":c!=null?" - footing tops coloured by soil pressure, load case "+c.Case:""),small,new SolidBrush(PowTheme.Muted),area.Left+6,area.Bottom-fontPt*2);
   if(c!=null&&!v.Rebar)RampLegend(g,area.Right-170,area.Top+fontPt*3f,150,small,"soil pressure q max / qa");}}
 /* reinforcement in 3D: footing bottom mesh both ways, strap top and bottom bars and the stirrups */
 static void DrawRebar(Graphics g,StrapInput inp,Func<double[],PointF> S,double sq){
  using(var px=new Pen(Color.FromArgb(200,45,45),1.1f))using(var pz=new Pen(Color.FromArgb(30,100,190),1.1f))foreach(var n in inp.Nodes){StrapFooting.Bars bars;try{bars=StrapFooting.ParseBars(n.Bars);}catch(Exception){continue;}double cv=inp.Cover/1000,sp=Math.Max(.05,bars.Spacing/1000),y1=-inp.Df+cv,y2=y1+bars.Dia/1000.0;double cx=n.X+n.Ex,cz=sq*(n.Z+n.Ez);
   for(double z=-n.L/2+cv;z<=n.L/2-cv+1e-9;z+=sp)g.DrawLine(px,S(new[]{cx-n.B/2+cv,y1,cz+z}),S(new[]{cx+n.B/2-cv,y1,cz+z}));for(double x=-n.B/2+cv;x<=n.B/2-cv+1e-9;x+=sp)g.DrawLine(pz,S(new[]{cx+x,y2,cz-n.L/2+cv}),S(new[]{cx+x,y2,cz+n.L/2-cv}));}
  foreach(var st in inp.Straps){int ia=StrapFooting.Index(inp,st.From),ib=StrapFooting.Index(inp,st.To);if(ia<0||ib<0)continue;var a=inp.Nodes[ia];var b=inp.Nodes[ib];double L=StrapFooting.Length(a,b);if(L<1e-6)continue;double ux=(b.X-a.X)/L,uz=sq*(b.Z-a.Z)/L,nx=-uz,nz=ux;
   StrapFooting.Bars top,bot,stir;try{top=StrapFooting.ParseBars(st.Top);bot=StrapFooting.ParseBars(st.Bottom);stir=StrapFooting.ParseBars(st.Stirrups);}catch(Exception){continue;}double cv=inp.StrapCover/1000,sd=stir.Dia/1000.0,w=st.Width,h=st.Depth,y0=-inp.Df;
   Func<double,double,double,double[]> P=(along,across,y)=>new[]{a.X+ux*along+nx*across,y,sq*a.Z+uz*along+nz*across};
   foreach(var set in new[]{new{B=top,Y=y0+h-cv-sd-top.Dia/2000.0,C=Color.FromArgb(200,45,45)},new{B=bot,Y=y0+cv+sd+bot.Dia/2000.0,C=Color.FromArgb(20,120,90)}})using(var pen=new Pen(set.C,1.6f)){int nb=Math.Max(2,set.B.Count);for(int i=0;i<nb;i++){double off=-w/2+cv+sd+set.B.Dia/2000.0+(w-2*(cv+sd)-set.B.Dia/1000.0)*i/(nb-1);g.DrawLine(pen,S(P(0,off,set.Y)),S(P(L,off,set.Y)));}}
   using(var pen=new Pen(Color.FromArgb(150,90,90,90),.7f)){double ss=Math.Max(.05,stir.Spacing/1000);int count=0;for(double x=.05;x<=L-.05&&count<200;x+=ss,count++){var q=new[]{S(P(x,-w/2+cv,y0+cv)),S(P(x,w/2-cv,y0+cv)),S(P(x,w/2-cv,y0+h-cv)),S(P(x,-w/2+cv,y0+h-cv))};g.DrawPolygon(pen,q);}}}}

 /* strap cross-section with the bars in their layers, the spacer bars between layers and the stirrup (owner, 20 November 2026:
    "yung arrangement din ng bars sana if ilan layers") */
 public static void StrapSection(Graphics g,RectangleF area,StrapInput inp,StrapBeam st,float fontPt){g.SmoothingMode=SmoothingMode.AntiAlias;StrapFooting.Bars top,bot,stir;try{top=StrapFooting.ParseBars(st.Top);bot=StrapFooting.ParseBars(st.Bottom);stir=StrapFooting.ParseBars(st.Stirrups);}catch(Exception){return;}
  double bw=st.Width*1000,h=st.Depth*1000,cv=inp.StrapCover;var rt=StrapFooting.Rows(top,bw,inp,stir.Dia);var rb=StrapFooting.Rows(bot,bw,inp,stir.Dia);
  using(var font=new Font("Segoe UI",fontPt,FontStyle.Regular,GraphicsUnit.Point))using(var bold=new Font("Segoe UI",fontPt,FontStyle.Bold,GraphicsUnit.Point))using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted))using(var dimPen=new Pen(PowTheme.Muted,.6f)){
   g.DrawString("STRAP S"+st.From+"-"+st.To+" SECTION  "+F(bw)+" x "+F(h),bold,ink,area.Left+6,area.Top+4);
   float top0=area.Top+fontPt*6,avH=area.Height-fontPt*12,avW=area.Width-fontPt*22;double k=Math.Min(avW/bw,avH/h);float w=(float)(bw*k),hh=(float)(h*k),x0=area.Left+fontPt*7+(avW-w)/2,y0=top0+(avH-hh)/2;
   using(var b=new HatchBrush(HatchStyle.Percent10,Color.Gray,Color.FromArgb(238,238,234)))g.FillRectangle(b,x0,y0,w,hh);g.DrawRectangle(Pens.Black,x0,y0,w,hh);
   float c=(float)((cv+stir.Dia/2.0)*k),rad=(float)Math.Max(2,2*stir.Dia*k);using(var sp=new Pen(Color.FromArgb(80,80,80),(float)Math.Max(1,stir.Dia*k))){var path=new GraphicsPath();float L=x0+c,T=y0+c,R=x0+w-c,B=y0+hh-c;path.AddArc(L,T,2*rad,2*rad,180,90);path.AddArc(R-2*rad,T,2*rad,2*rad,270,90);path.AddArc(R-2*rad,B-2*rad,2*rad,2*rad,0,90);path.AddArc(L,B-2*rad,2*rad,2*rad,90,90);path.CloseFigure();g.DrawPath(sp,path);path.Dispose();float hk=(float)Math.Max(5,75*k);g.DrawLine(sp,L+rad,T+rad*.3f,L+rad+hk*.7f,T+rad*.3f+hk*.7f);}
   Action<int[],StrapFooting.Bars,bool,Color> layers=(rows,bars,isTop,col)=>{double inner=bw-2*cv-2*stir.Dia;for(int r=0;r<rows.Length;r++){int n=rows[r];double off=cv+stir.Dia+bars.Dia/2.0+r*(bars.Dia+25.0);float cy=isTop?y0+(float)(off*k):y0+hh-(float)(off*k);float d=(float)Math.Max(3,bars.Dia*k);
     int n0=rows[0];for(int i=0;i<n;i++){/* upper layers directly above bars of the first layer (NSCP 425.2.2) */int at=n==1?0:(int)Math.Round((double)i*(n0-1)/(n-1));double xm=n0==1?bw/2:cv+stir.Dia+bars.Dia/2.0+(inner-bars.Dia)*at/(n0-1);using(var b=new SolidBrush(col))g.FillEllipse(b,x0+(float)(xm*k)-d/2,cy-d/2,d,d);}
     if(r>0){double offS=cv+stir.Dia+bars.Dia+(r-1)*(bars.Dia+25.0)+12.5;float sy=isTop?y0+(float)(offS*k):y0+hh-(float)(offS*k);using(var spc=new Pen(Color.FromArgb(150,110,60),(float)Math.Max(1.5,25*k)))g.DrawLine(spc,x0+(float)((cv+stir.Dia)*k),sy,x0+w-(float)((cv+stir.Dia)*k),sy);g.DrawString("spacer Ø25",font,new SolidBrush(Color.FromArgb(150,110,60)),x0+w+6,sy-fontPt*.7f);}}};
   layers(rt,top,true,Color.FromArgb(200,45,45));layers(rb,bot,false,Color.FromArgb(20,120,90));
   Func<int[],string> rowsText=rows=>rows.Length>1?" in "+rows.Length+" layers ("+string.Join(" + ",rows)+")":" in one layer";
   g.DrawString("top "+st.Top+rowsText(rt),font,new SolidBrush(Color.FromArgb(200,45,45)),area.Left+6,y0-fontPt*3.2f);g.DrawString("bottom "+st.Bottom+rowsText(rb),font,new SolidBrush(Color.FromArgb(20,120,90)),area.Left+6,y0+hh+fontPt*2.6f);
   g.DrawString("stirrups 2L-"+st.Stirrups+", cover "+F(cv),font,muted,area.Left+6,y0+hh+fontPt*4.3f);
   g.DrawLine(dimPen,x0,y0+hh+fontPt*1.2f,x0+w,y0+hh+fontPt*1.2f);var bt=F(bw);var bsz=g.MeasureString(bt,font);g.DrawString(bt,font,muted,x0+w/2-bsz.Width/2,y0+hh+fontPt*1.2f);
   float vx=x0-fontPt*1.2f;g.DrawLine(dimPen,vx,y0,vx,y0+hh);var stt=g.Save();g.TranslateTransform(vx-2,y0+hh/2);g.RotateTransform(-90);var hsz=g.MeasureString(F(h),font);g.DrawString(F(h),font,muted,-hsz.Width/2,-hsz.Height);g.Restore(stt);}}
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
class StrapPlanView:Control {internal StrapInput Input;internal StrapCase Case;internal readonly HashSet<string> Failing=new HashSet<string>();internal bool ThreeD;internal string Selected;internal readonly View3D View=new View3D();bool rotating;Point rotStart;double yaw0,pitch0;
 internal event Action<string,double,double> FootingMoved;internal event Action<string> FootingSelected;PlanMap map;string dragging;PointF dragStart;double ex0,ez0;
 public StrapPlanView(){DoubleBuffered=true;ResizeRedraw=true;BackColor=Color.White;Width=1000;Height=460;Tag="FullWidth";}
 protected override void OnPaint(PaintEventArgs e){/* drawings stay paper-white with dark ink in dark mode (owner, 21 November 2026: "pangit ng dark mode") */if(DesktopAppearance.Palette.Dark)e.Graphics.Clear(Color.White);using(new ReportPaperScope())PaintPaper(e);}
 void PaintPaper(PaintEventArgs e){base.OnPaint(e);if(Input==null){TextRenderer.DrawText(e.Graphics,"Enter the footings and straps to see the plan.",ModernUi.Body,new Point(12,12),PowTheme.Muted);return;}
  try{var r=new RectangleF(0,0,Width,Height-26);if(ThreeD){map=null;StrapVisuals.Iso(e.Graphics,r,Input,Failing,8.5f,Case,View);}else map=StrapDrawing.Plan(e.Graphics,r,Input,Case,8.5f,Failing,Selected);
   TextRenderer.DrawText(e.Graphics,ThreeD?"3D view - drag to turn, wheel to zoom, double-click for the plan":"Plan - drag a footing to change its eccentricity (25 mm steps), click to select its row, double-click for 3D",ModernUi.Body,new Point(10,Height-24),PowTheme.Muted);}
  catch(Exception ex){TextRenderer.DrawText(e.Graphics,ex.Message,ModernUi.Body,new Point(12,12),Color.Firebrick);}}
 StrapNode Hit(PointF p){if(map==null||Input==null)return null;var w=map.Inverse(p);return Input.Nodes.LastOrDefault(n=>Math.Abs(w[0]-(n.X+n.Ex))<=n.B/2&&Math.Abs(w[1]-(n.Z+n.Ez))<=n.L/2);}
 protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);Focus();if(ThreeD){rotating=true;rotStart=e.Location;yaw0=View.Yaw;pitch0=View.Pitch;return;}var n=Hit(e.Location);if(n==null)return;dragging=n.Node;dragStart=e.Location;ex0=n.Ex;ez0=n.Ez;Selected=n.Node;if(FootingSelected!=null)FootingSelected(n.Node);Invalidate();}
 protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(ThreeD){if(rotating){View.Yaw=yaw0+(e.X-rotStart.X)*.5;View.Pitch=Math.Max(5,Math.Min(85,pitch0+(e.Y-rotStart.Y)*.3));Invalidate();}return;}if(dragging==null){Cursor=Hit(e.Location)!=null?Cursors.SizeAll:Cursors.Default;return;}if(map==null)return;var a=map.Inverse(dragStart);var b=map.Inverse(e.Location);var n=Input.Nodes.FirstOrDefault(x=>x.Node==dragging);if(n==null)return;
  double ex=Math.Round((ex0+b[0]-a[0])/.025)*.025,ez=Math.Round((ez0+b[1]-a[1])/.025)*.025;double mx=Math.Max(0,n.B/2-n.Cx/2),mz=Math.Max(0,n.L/2-n.Cz/2);ex=Math.Max(-mx,Math.Min(mx,ex));ez=Math.Max(-mz,Math.Min(mz,ez));if(Math.Abs(ex-n.Ex)<1e-9&&Math.Abs(ez-n.Ez)<1e-9)return;n.Ex=ex;n.Ez=ez;Invalidate();if(FootingMoved!=null)FootingMoved(n.Node,ex,ez);}
 protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=null;rotating=false;}
 protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);if(!ThreeD)return;View.Zoom=Math.Max(.4,Math.Min(5,View.Zoom*(e.Delta>0?1.15:1/1.15)));Invalidate();}
 protected override void OnDoubleClick(EventArgs e){base.OnDoubleClick(e);ThreeD=!ThreeD;Invalidate();}
}
class StrapSectionView:Control {internal StrapInput Input;internal int Index;
 public StrapSectionView(){DoubleBuffered=true;ResizeRedraw=true;BackColor=Color.White;}
 protected override void OnPaint(PaintEventArgs e){/* drawings stay paper-white with dark ink in dark mode (owner, 21 November 2026: "pangit ng dark mode") */if(DesktopAppearance.Palette.Dark)e.Graphics.Clear(Color.White);using(new ReportPaperScope())PaintPaper(e);}
 void PaintPaper(PaintEventArgs e){base.OnPaint(e);if(Input==null||Input.Straps.Count==0){TextRenderer.DrawText(e.Graphics,"Select a strap to see its section and bar layers.",ModernUi.Body,new Point(10,10),PowTheme.Muted);return;}try{StrapVisuals.StrapSection(e.Graphics,new RectangleF(0,0,Width,Height),Input,Input.Straps[Math.Max(0,Math.Min(Input.Straps.Count-1,Index))],8.5f);}catch(Exception ex){TextRenderer.DrawText(e.Graphics,ex.Message,ModernUi.Body,new Point(10,10),Color.Firebrick);}}
}
}
