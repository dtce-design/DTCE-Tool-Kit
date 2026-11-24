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
  double going=(n-1)*T,L=before+going+after,H=n*R;bool fitBeams=i.OnBeams&&!i.Slabless;double padL=fitBeams?(before<=0?i.BeamB+350:0):0,padR=fitBeams?(after<=0?i.BeamB+350:0):0,padZ=fitBeams?i.BeamH:0;
  float s=(float)Math.Min((r.Width-140)/Math.Max(L+600+padL+padR,1),(r.Height-175)/Math.Max(H+t+300+padZ,1));float ox=70+(float)(padL*s),oy=r.Height-125-(float)(padZ*s)+(float)(300*s);
  /* a flight that goes back the other way on plan (dog-legged flight 2, open-well flight 3) is drawn mirrored, as seen from the same side
     as flight 1 (owner, 24 November 2026: "hindi ba dapat yung flight 2 naka mirror?") */bool mirror=(i.Flights==2&&Member==1)||(i.Flights==3&&Member==2);
  float mirrorSum=2*ox+(float)((600-padL+L+padR)*s);/* left + right edge of the drawing: mirrored about its own centre */
  Func<double,double,PointF> P=(px,pz)=>{float sx=ox+(float)((px+300)*s);return new PointF(mirror?mirrorSum-sx:sx,oy-(float)((pz+300)*s));};
  /* top bars run the full length (owner, 24 November 2026: "ano ba ang tama yung top bars L/4 or full length isa lang ilagay") */bool topFull=!i.Slabless;
  double k=Math.Sqrt(R*R+T*T)/T,ts=i.Slabless?0:t*k;Func<double,double> ext0=gg=>gg*0.25+300;var outline=new List<PointF>();/* top profile */outline.Add(P(0,0));outline.Add(P(before,0));double x=before;for(int j=0;j<n-1;j++){outline.Add(P(x,(j+1)*R));x+=T;outline.Add(P(x,(j+1)*R));}outline.Add(P(x,H));outline.Add(P(L,H));
  /* soffit: the waist runs straight at the slope until it meets the underside of each landing (owner, 23 November 2026: "di pa maayos
     yung section ng stairs rebar") - no wedge under the landing */
  Func<double,double> slope=xx=>(xx-before)*R/T-ts;Func<double,double> bot=xx=>{double z=slope(xx);if(before>0&&z<-tl)z=-tl;if(after>0&&z>H-tl)z=H-tl;return z;};
  double xTop=before+(H-tl+ts)*T/R,xBot=before+(ts-tl)*T/R;
  var soff=new List<PointF>();if(i.Slabless){/* the underside of the folded plate: every tread and riser plate t thick */if(after>0){soff.Add(P(L,H-tl));soff.Add(P(before+going+t,H-tl));}else soff.Add(P(L+t,H));
   for(int j=n-1;j>=0;j--){double xj=before+j*T;if(j<n-1)soff.Add(P(xj+T+t,(j+1)*R-t));soff.Add(P(xj+t,(j+1)*R-t));soff.Add(P(xj+t,Math.Max(j*R-t,before>0||j>0?j*R-t:-tl)));}
   soff.Add(P(before+t,-tl));soff.Add(P(0,-tl));}
  else{foreach(var xx in new[]{L,xTop,xBot,0.0}.Where(v=>v>=0&&v<=L).Distinct().OrderByDescending(v=>v))soff.Add(P(xx,bot(xx)));}
  /* support beams where they are (owner, 23 November 2026: "pwede mag assign ng beam as support tapos sa location niya sa landing"):
     a landing beam at its distance from the flight, a floor beam (with the floor slab) where the flight has no landing */
  double xs0=before>0?before-i.LandingSpan(before):0,xe0=after>0?before+going+i.LandingSpan(after):L;bool beams=i.OnBeams&&!i.Slabless;
  var poly=outline.Concat(soff).ToArray();using(var b=new SolidBrush(Concrete))g.FillPolygon(b,poly);using(var p=new Pen(Ink,1.4f/Zoom))g.DrawPolygon(p,poly);
  if(beams)using(var bb=new SolidBrush(Color.FromArgb(214,216,212)))using(var bp=new Pen(Ink,1.2f/Zoom)){
   /* x0, x1 of the beam and its top: under a landing (below the slab, flush with the landing end or centred where it is placed), or
      beyond a flight end without a landing (the floor beam, the last riser at its face) */
   var list=new List<double[]>();
   if(before>0){double c0=i.LandingBeamAt>0&&i.LandingBeamAt<before?xs0:i.BeamB/2;list.Add(new[]{c0-i.BeamB/2,c0+i.BeamB/2,-tl});}else list.Add(new[]{-i.BeamB,0,0.0});
   if(after>0){double c1=i.LandingBeamAt>0&&i.LandingBeamAt<after?xe0:L-i.BeamB/2;list.Add(new[]{c1-i.BeamB/2,c1+i.BeamB/2,H-tl});}else list.Add(new[]{L,L+i.BeamB,H});
   foreach(var bm in list){var r0=P(bm[0],bm[2]);var r1=P(bm[1],bm[2]-(bm[2]==0||bm[2]==H?i.BeamH:i.BeamH-tl));var rect=new RectangleF(Math.Min(r0.X,r1.X),Math.Min(r0.Y,r1.Y),Math.Abs(r1.X-r0.X),Math.Abs(r1.Y-r0.Y));g.FillRectangle(bb,rect);g.DrawRectangle(bp,rect.X,rect.Y,rect.Width,rect.Height);
    /* the beam's own stirrup and corner bars (owner, 24 November 2026: "pati yung connection sa beam"): the stair bars anchor inside the
       stirrup - bottom bars past the face, top bars hooked down inside the core */
    {float cv=(float)(40*s),bd=Math.Max(2.2f/Zoom,(float)(16*s/2));var core=RectangleF.Inflate(rect,-cv,-cv);if(core.Width>4&&core.Height>4){using(var sp=new Pen(Color.FromArgb(150,45,45),Math.Max(1f/Zoom,(float)(10*s))))g.DrawRectangle(sp,core.X,core.Y,core.Width,core.Height);
      using(var cb=new SolidBrush(Steel))foreach(var cp in new[]{new PointF(core.Left+bd,core.Top+bd),new PointF(core.Right-bd,core.Top+bd),new PointF(core.Left+bd,core.Bottom-bd),new PointF(core.Right-bd,core.Bottom-bd)})g.FillEllipse(cb,cp.X-bd,cp.Y-bd,2*bd,2*bd);}}
    Text(g,"Beam "+Staircase.F(i.BeamB,"0")+" x "+Staircase.F(i.BeamH,"0"),rect.X+rect.Width/2,rect.Bottom+9,7.5f);
    /* anchorage under each beam label: a floor beam takes the hooked top bars (ldh) and the bottom bars (>= 150 mm) */bool floorBeam=bm[2]==0||bm[2]==H;
    Text(g,floorBeam?"B: 90° hook, ldh "+Staircase.F(Staircase.HookLength(i),"0"):"A, B continuous over the beam",rect.X+rect.Width/2,rect.Bottom+21,7f);if(floorBeam)Text(g,"A: ≥ 150 into the beam",rect.X+rect.Width/2,rect.Bottom+32,7f);}
   /* the floor slab beyond a flight end without a landing */
   using(var dp=new Pen(Ink,1f/Zoom){DashStyle=DashStyle.Dash}){if(after<=0){var f0=P(L+i.BeamB,H);var f1=P(L+i.BeamB+350,H-tl);var fr=new RectangleF(f0.X,f0.Y,f1.X-f0.X,f1.Y-f0.Y);g.FillRectangle(bb,fr);g.DrawRectangle(dp,fr.X,fr.Y,fr.Width,fr.Height);Text(g,"floor",fr.X+fr.Width/2,fr.Y-8,7.5f);}
    if(before<=0){var f0=P(-i.BeamB-350,0);var f1=P(-i.BeamB,-tl);var fr=new RectangleF(f0.X,f0.Y,f1.X-f0.X,f1.Y-f0.Y);g.FillRectangle(bb,fr);g.DrawRectangle(dp,fr.X,fr.Y,fr.Width,fr.Height);Text(g,"floor",fr.X+fr.Width/2,fr.Y-8,7.5f);}}}

  /* supports (simple supports when not on beams) */if(!beams)foreach(var sx in new[]{xs0,xe0}){var a=P(sx,sx==xs0?(before>0?-tl:bot(0)):H-tl);g.FillPolygon(Brushes.DimGray,new[]{a,new PointF(a.X-8,a.Y+14),new PointF(a.X+8,a.Y+14)});}
  /* bars following the slab: bottom bars along the soffit, top bars over the supports and through the kinks (sampled along the profile) */
  double c=i.Cover+i.MainDia/2,tsl=i.Slabless?t:ts;Func<double,double> topLine=xx=>i.Slabless?(xx<before?-c:xx>before+going?H-c:(xx-before)*R/T-c):Math.Min(H-c,Math.Max(-c,(xx-before)*R/T-c));/* the top face of the waist runs on at the slope until it meets the landing top (no step up the last riser) */Func<double,double> botLine=xx=>i.Slabless?(xx<before?-tl+c:xx>before+going?H-tl+c:(xx-before)*R/T-tsl+c):bot(xx)+c;
  Func<double,double,Func<double,double>,PointF[]> run=(a,b2,fz)=>{var pts=new List<PointF>();int nn=60;for(int q=0;q<=nn;q++){double xx=a+(b2-a)*q/nn;pts.Add(P(xx,fz(xx)));}return pts.ToArray();};
  /* drawn to scale (owner, 23 November 2026: "di pa naka drawn to scale"): bar lines and dots are their real diameters at the drawing
     scale (never thinner than a hairline), the same scale across and up */
  float barW=Math.Max(1.2f/Zoom,(float)(i.MainDia*s)),distR=Math.Max(1.6f/Zoom,(float)(i.DistDia*s/2));double dOff=(i.MainDia+i.DistDia)/2;
  Action<Brush,PointF,float> dotAt=(br,q,rad)=>g.FillEllipse(br,q.X-rad,q.Y-rad,2*rad,2*rad);
  using(var bar=new Pen(Steel,barW){LineJoin=LineJoin.Round}){
   if(i.Slabless){/* folded plate (owner, 23 November 2026: "fix"): both bar layers follow the treads and risers - the top layer at the cover
       below each tread / inside each riser, the bottom layer at the cover above the soffit - with distribution bars at every fold */
    Func<double,List<PointF>> zig=o=>{var pts=new List<PointF>();if(before>0)pts.Add(P(c,o==c?-c:-tl+c));for(int j=0;j<n;j++){double xj=before+j*T;pts.Add(P(xj+o,j*R-(o==c?c:t-c)));pts.Add(P(xj+o,(j+1)*R-(o==c?c:t-c)));}
     if(after>0)pts.Add(P(L-c,o==c?H-c:H-tl+c));return pts;};
    g.DrawLines(bar,zig(c).ToArray());g.DrawLines(bar,zig(t-c).ToArray());
    using(var dot=new SolidBrush(Steel))for(int j=0;j<n-1;j++){double xm=before+j*T+T/2;foreach(var q in new[]{P(xm,(j+1)*R-c-dOff),P(xm+t-2*c,(j+1)*R-t+c+dOff)})dotAt(dot,q,distR);}}
   else{/* bottom bars: the flight bar bends round the lower (convex) corner; at the upper landing, where the soffit turns inward, the
           flight bar runs straight on into the landing to its top cover and the landing bar runs on into the flight - the bars cross */
    Func<double,double> flight=xx=>{double z=slope(xx);if(before>0&&z<-tl)z=-tl;return z+c;};
    double xEnd=after>0?Math.Min(L-c,before+(H-2*c+ts)*T/R):L-c;g.DrawLines(bar,run(c,xEnd,flight));
    /* at a flight end on a floor beam the bottom bars run on into the beam */if(beams&&after<=0)g.DrawLines(bar,new[]{P(L-c,flight(L-c)),P(L+i.BeamB-c,flight(L-c))});if(beams&&before<=0)g.DrawLines(bar,new[]{P(c,flight(c)),P(-i.BeamB+c,flight(c))});
    if(after>0){double xCross=Math.Max(c,before+(H-tl+2*c)*T/R);g.DrawLines(bar,new[]{P(L-c,H-tl+c),P(xCross,H-tl+c)});
     /* standard detail at a re-entrant corner (ACI SP-66 / NSCP 425): each crossing bar runs ld past the crossing; where the concrete is
        shorter the bar is bent along the opposite face for the rest of ld */
     double ldm=Staircase.DevLength(i),kk=Math.Sqrt(R*R+T*T)/T,xi=before+(H-tl+ts)*T/R;double rem1=ldm-(xEnd-xi)*kk;if(rem1>1)g.DrawLines(bar,new[]{P(xEnd,H-c),P(Math.Min(L-c,xEnd+rem1),H-c)});
     double rem2=ldm-(xi-xCross);if(rem2>1){double xl=Math.Max(c,xCross-rem2/kk);g.DrawLines(bar,new[]{P(xCross,H-tl+c),P(xl,(xl-before)*R/T-c)});}
     var lp=P(xi,H-tl+c);Text(g,"ld "+Staircase.F(ldm,"0"),lp.X+28,lp.Y+12,7f,false,Brushes.DimGray);}}double ext=topFull?L:going*0.25+300;
   /* top bars: the end legs always bend down into the support; where a lower landing meets the flight the top face turns inward, so
      the landing top bar runs straight on into the flight to its bottom cover and the flight top bar runs on into the landing - they
      cross (as the bottom bars at the upper landing) */
   if(!i.Slabless&&before>0){double xa=Math.Min(L-c,before+(ts-2*c)*T/R);g.DrawLines(bar,new[]{P(c,-c-tl*0.6),P(c,-c),P(Math.Max(c,xa),-c)});
    double xb=Math.Max(c,before+(2*c-tl)*T/R);g.DrawLines(bar,run(xb,Math.Min(L-c,before+ext),xx=>(xx-before)*R/T-c));
    /* ld past the crossing at the lower re-entrant corner (top face), bent along the opposite face where needed */
    double ldm=Staircase.DevLength(i),kk=Math.Sqrt(R*R+T*T)/T;double rem1=ldm-(xa-before);if(rem1>1){double xe=Math.Min(L-c,xa+rem1/kk);g.DrawLines(bar,new[]{P(xa,-c),P(xe,slope(xe)+c)});}
    double rem2=ldm-(before-xb)*kk;if(rem2>1)g.DrawLines(bar,new[]{P(xb,-tl+c),P(Math.Max(c,xb-rem2),-tl+c)});
    var lp=P(before,-c);Text(g,"ld "+Staircase.F(ldm,"0"),lp.X-30,lp.Y-12,7f,false,Brushes.DimGray);}
   else if(!i.Slabless){var left=beams&&before<=0?new List<PointF>{P(-i.BeamB+c,topLine(c)-i.BeamH*0.6),P(-i.BeamB+c,topLine(c))}:new List<PointF>{P(c,topLine(c)-tl*0.6)};left.AddRange(run(c,Math.Min(L-c,before+ext),topLine));g.DrawLines(bar,left.ToArray());}
   if(!i.Slabless){var right=run(topFull?(before>0?Math.Max(c,before+(2*c-tl)*T/R):c):Math.Max(c,L-after-ext),L-c,topLine).ToList();if(beams&&after<=0){/* into the floor beam, hooked down at its far face */right.Add(P(L+i.BeamB-c,topLine(L-c)));right.Add(P(L+i.BeamB-c,topLine(L-c)-i.BeamH*0.6));}else right.Add(P(L-c,topLine(L-c)-tl*0.6));g.DrawLines(bar,right.ToArray());}}
  /* distribution bars under the top bars too */if(!i.Slabless)using(var dot=new SolidBrush(Steel)){double ext=topFull?L:going*0.25+300,sp=Math.Max(150,i.DistDia*20)*1.5;foreach(var seg in new[]{new[]{c+100,Math.Min(L-c,before+ext)},new[]{Math.Max(c,L-after-ext),L-c-100}})for(double xx=seg[0];xx<seg[1];xx+=sp){var q=P(xx,topLine(xx)-dOff);dotAt(dot,q,distR);}}
  if(!i.Slabless)using(var dot=new SolidBrush(Steel)){double sp=Math.Max(150,i.DistDia*20);for(double xx=200;xx<L-100;xx+=sp*1.5){var q=P(xx,botLine(xx)+dOff);dotAt(dot,q,distR);}}
  /* step bars and nosing bars (owner, 23 November 2026: "proper anchoring ... walang detail ng chair bar and nosing bar") */
  PointF noseAt=PointF.Empty,chairAt=PointF.Empty;
  if(!i.Slabless){/* step bars (the "chair bars" of a stair, owner, 23 November 2026: "reinforcement ng steps kako"): in every step a bar
      bent to the step - up the riser at the cover, round the nosing, along the tread at the cover - with both legs lapped onto the waist
      top bars; a nosing bar runs the full width in its bend. Drawn to scale. */
   double cn=i.Cover+i.DistDia/2,leg=12*i.DistDia,hook=Math.Max(60,6*i.DistDia);float sw=Math.Max(1.2f/Zoom,(float)(i.DistDia*s));
   using(var sb=new Pen(Color.FromArgb(160,30,30),sw){LineJoin=LineJoin.Round})using(var dot=new SolidBrush(Steel))for(int j=0;j<n-1;j++){
     double xr=before+j*T+cn,zt=(j+1)*R-cn,xb=before+(j+1)*T-cn;if(xb-xr<50)continue;
     /* standard step bar (owner, 24 November 2026: "ayusin din yung chair bars ganiyan ba ang tama?"): up the riser and along the tread at
        the cover; each leg ends 12db past the waist top mat with a 90° hook (6db) tied to it, pointing into the step */
     double z0=topLine(xr)-dOff-leg,zb=Math.Min(zt,topLine(xb))-dOff-leg;
     g.DrawLines(sb,new[]{P(xr+hook,z0),P(xr,z0),P(xr,zt),P(xb,zt),P(xb,zb),P(xb-hook,zb)});
     var q=P(xr+dOff,zt-dOff);dotAt(dot,q,distR);if(j==Math.Min(n-2,Math.Max(1,n/2))){noseAt=q;chairAt=P((xr+xb)/2,zt);}}}
  /* bar callouts A / B / C, the same marks as the reinforcement table (owner, 23 November 2026: "sa reports yung section plus yung
     reinforcement kung saan yung tinutukoy") */
  if(Result!=null&&!i.Slabless){string name=Result.Members.Count>0?Result.Members[Math.Min(Member,Result.Members.Count-1)].Name:"";
   Func<string,string> provide=key=>{var row=Result.Steel.FirstOrDefault(q=>q[0]==name&&q[1].StartsWith(key,StringComparison.OrdinalIgnoreCase));return row==null?"":row[2];};
   /* leaders never cross: the label rows follow the height of their anchors (owner, 24 November 2026: "pakicheck yung bar A and C");
      A points at the main bottom bar, C at a distribution bar dot of the bottom mat, well apart */
   var marks=new List<Tuple<string,PointF,string>>();Action<string,PointF,string> mark=(m,anchor,text)=>{if(text!="")marks.Add(Tuple.Create(m,anchor,text));};
   double xa=before+going*0.32;mark("A",P(xa,slope(xa)+c),provide("Main bottom"));
   double spd=Math.Max(150,i.DistDia*20)*1.5,xd=200;while(xd<before+going*0.7&&xd+spd<L-100)xd+=spd;mark("C",P(xd,botLine(xd)+dOff),provide("Distribution"));
   if(!chairAt.IsEmpty){string st=provide("Step bars");int cs=st.IndexOf(", at the cover",StringComparison.Ordinal);mark("D",chairAt,"Step bars (chair bars) "+(cs>0?st.Substring(0,cs):st));}
   if(!noseAt.IsEmpty){string nb=provide("Nosing");int cut=nb.IndexOf(", full width",StringComparison.Ordinal);mark("E",noseAt,"Nosing bars "+(cut>0?nb.Substring(0,cut):nb)+", in the bend");}
   float colX=P(before+going*0.82,0).X,rowY=P(0,H*0.2).Y;int lineNo=0;
   foreach(var mk in marks.OrderBy(q=>q.Item2.Y)){var at=new PointF(colX,rowY+18*lineNo++);using(var lp=new Pen(Dim,1/Zoom))g.DrawLine(lp,mk.Item2,at);using(var dot=new SolidBrush(Ink))g.FillEllipse(dot,mk.Item2.X-2.5f,mk.Item2.Y-2.5f,5,5);
    Text(g,mk.Item1+"  "+mk.Item3,at.X+(mirror?-3:3),at.Y,8f,true,null,mirror?StringAlignment.Far:StringAlignment.Near);}
   double xt=before+Math.Min(going*0.18,ext0(going));var topAt=P(xt,topLine(xt));string tb=provide("Top bars");if(tb!=""){float dx=mirror?18:-18;var at=new PointF(topAt.X+dx,topAt.Y-46);using(var lp=new Pen(Dim,1/Zoom))g.DrawLine(lp,topAt,at);using(var dot=new SolidBrush(Ink))g.FillEllipse(dot,topAt.X-2.5f,topAt.Y-2.5f,5,5);Text(g,"B  "+tb,at.X+(dx>=0?3:-3),at.Y,8f,true,null,dx>=0?StringAlignment.Near:StringAlignment.Far);}}
  /* dimensions: spaced on the screen, not in mm, so the two lines never overlap at a small scale (owner screenshot, 23 November 2026) */
  using(var dp=new Pen(Dim,1/Zoom)){float y0=P(0,-tl).Y+20;var c1=new PointF(P(before,0).X,y0);var c2=new PointF(P(before+going,0).X,y0);g.DrawLine(dp,c1,c2);g.DrawLine(dp,c1.X,y0-4,c1.X,y0+4);g.DrawLine(dp,c2.X,y0-4,c2.X,y0+4);Text(g,(n-1)+" treads x "+Staircase.F(T,"0")+" = "+Staircase.F(going,"0"),(c1.X+c2.X)/2,y0+10);
   float y1=y0+26;var a=new PointF(P(0,0).X,y1);var b=new PointF(P(L,0).X,y1);g.DrawLine(dp,a,b);g.DrawLine(dp,a.X,y1-4,a.X,y1+4);g.DrawLine(dp,b.X,y1-4,b.X,y1+4);Text(g,"L = "+Staircase.F(L,"0")+" mm (plan)",(a.X+b.X)/2,y1+10,8.5f,true);}
  /* graphic scale: true at any zoom or print size */
  using(var sp=new Pen(Ink,1/Zoom))using(var fill=new SolidBrush(Ink)){float sx0=Math.Min(P(0,0).X,P(L,0).X),sy=14,unit=(float)(500*s);for(int q=0;q<2;q++){var cell=new RectangleF(sx0+q*unit,sy,unit,4);if(q==0)g.FillRectangle(fill,cell);g.DrawRectangle(sp,cell.X,cell.Y,cell.Width,cell.Height);}
   Text(g,"0",sx0,sy+12,7f);Text(g,"500",sx0+unit,sy+12,7f);Text(g,"1000 mm",sx0+2*unit,sy+12,7f);Text(g,"Drawn to scale, horizontal = vertical; bars at their diameters",sx0+2*unit+40,sy+2,7f,false,Brushes.DimGray,StringAlignment.Near);}
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
 protected override void Draw(Graphics g,RectangleF r){var i=Input;var boxes=Plan(i);if(i.Spiral){Spiral(g,r);return;}double minX=boxes.Min(b=>b.X0),maxX=boxes.Max(b=>b.X1),minY=boxes.Min(b=>b.Y0),maxY=boxes.Max(b=>b.Y1);float s=(float)Math.Min((r.Width-150)/(maxX-minX),(r.Height-150)/(maxY-minY));
  Func<double,double,PointF> P=(x,y)=>new PointF(100+(float)((x-minX)*s),r.Height-95-(float)((y-minY)*s));
  foreach(var b in boxes){var a=P(b.X0,b.Y1);var c=P(b.X1,b.Y0);var rc=new RectangleF(a.X,a.Y,c.X-a.X,c.Y-a.Y);using(var br=new SolidBrush(b.Landing?Color.FromArgb(240,240,236):Color.FromArgb(226,232,238)))g.FillRectangle(br,rc);g.DrawRectangle(Pens.Black,rc.X,rc.Y,rc.Width,rc.Height);
   if(!b.Landing){for(int k=1;k<b.Treads;k++){double f=(double)k/b.Treads;if(b.Dx!=0){double x=b.Dx>0?b.X0+(b.X1-b.X0)*f:b.X1-(b.X1-b.X0)*f;g.DrawLine(Pens.Gray,P(x,b.Y0),P(x,b.Y1));}else{double y=b.Dy>0?b.Y0+(b.Y1-b.Y0)*f:b.Y1-(b.Y1-b.Y0)*f;g.DrawLine(Pens.Gray,P(b.X0,y),P(b.X1,y));}}
    PointF from=b.Dx!=0?P(b.Dx>0?b.X0:b.X1,(b.Y0+b.Y1)/2):P((b.X0+b.X1)/2,b.Dy>0?b.Y0:b.Y1),to=b.Dx!=0?P(b.Dx>0?b.X1:b.X0,(b.Y0+b.Y1)/2):P((b.X0+b.X1)/2,b.Dy>0?b.Y1:b.Y0);using(var ar=new Pen(Color.FromArgb(28,100,160),1.6f/Zoom){CustomEndCap=new AdjustableArrowCap(4,5)})g.DrawLine(ar,from,to);Text(g,"UP",from.X+(to.X-from.X)*.15f,from.Y+(to.Y-from.Y)*.15f-10,8,true,Brushes.SteelBlue);
    Text(g,b.Name,(rc.Left+rc.Right)/2,(rc.Top+rc.Bottom)/2+12,8);}else Text(g,b.Name,(rc.Left+rc.Right)/2,(rc.Top+rc.Bottom)/2,8,false,Brushes.DimGray);}
  /* dimensions (owner, 23 November 2026: "reflect din yung dimensions"): a chain of the flights, landings and gap along the bottom and the
     left side, and the overall length and width, in mm */
  using(var dp=new Pen(Dim,1f/Zoom)){Func<IEnumerable<double>,List<double>> marks=v=>v.Select(q=>Math.Round(q,3)).Distinct().OrderBy(q=>q).ToList();var xs=marks(boxes.SelectMany(b=>new[]{b.X0,b.X1}));var ys=marks(boxes.SelectMany(b=>new[]{b.Y0,b.Y1}));
   Action<double,double,float,bool,bool> along=(a0,a1,off,horizontal,bold)=>{if(a1-a0<1)return;PointF p0,p1;if(horizontal){float y=P(0,minY).Y+off;p0=new PointF(P(a0,0).X,y);p1=new PointF(P(a1,0).X,y);g.DrawLine(dp,p0,p1);foreach(var q in new[]{p0,p1})g.DrawLine(dp,q.X,q.Y-4,q.X,q.Y+4);Text(g,Staircase.F(a1-a0,"0"),(p0.X+p1.X)/2,y+9,bold?8.5f:7.5f,bold);}
    else{float x=P(minX,0).X-off;p0=new PointF(x,P(0,a0).Y);p1=new PointF(x,P(0,a1).Y);g.DrawLine(dp,p0,p1);foreach(var q in new[]{p0,p1})g.DrawLine(dp,q.X-4,q.Y,q.X+4,q.Y);var st=g.Save();g.TranslateTransform(x-10,(p0.Y+p1.Y)/2);g.RotateTransform(-90);Text(g,Staircase.F(a1-a0,"0"),0,0,bold?8.5f:7.5f,bold);g.Restore(st);}};
   for(int k=0;k+1<xs.Count;k++)along(xs[k],xs[k+1],22,true,false);along(minX,maxX,48,true,true);
   for(int k=0;k+1<ys.Count;k++)along(ys[k],ys[k+1],22,false,false);along(minY,maxY,48,false,true);
   /* tread count and going on each flight */foreach(var b in boxes.Where(q=>!q.Landing)){var c0=P((b.X0+b.X1)/2,(b.Y0+b.Y1)/2);Text(g,b.Treads+" treads x "+Staircase.F(i.Tread,"0")+" = "+Staircase.F(b.Treads*i.Tread,"0"),c0.X,c0.Y+26,7.5f,false,Brushes.DimGray);}}
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
 /* a view that shows the flights side by side (owner, 24 November 2026: "paki ayos lahat ng model") */double yaw=0.6,pitch=0.5;
 protected override string Hint{get{return "Drag: rotate   Wheel: zoom   Double-click: reset";}}
 protected override void Dragged(int dx,int dy,MouseButtons b){if(b==MouseButtons.Right){base.Dragged(dx,dy,b);return;}yaw+=dx*0.01;pitch=Math.Max(-1.4,Math.Min(1.4,pitch+dy*0.01));}
 protected override void Reset(){yaw=0.6;pitch=0.5;}
 class Face {public double[][] P;public Color C;}
 static void Prism(List<Face> f,double[][] bottom,double h,Color c){int n=bottom.Length;var top=bottom.Select(p=>new[]{p[0],p[1],p[2]+h}).ToArray();f.Add(new Face{P=bottom.Reverse().ToArray(),C=c});f.Add(new Face{P=top,C=c});for(int k=0;k<n;k++){var a=bottom[k];var b=bottom[(k+1)%n];f.Add(new Face{P=new[]{a,b,new[]{b[0],b[1],b[2]+h},new[]{a[0],a[1],a[2]+h}},C=c});}}
 static void Box(List<Face> f,double x0,double x1,double y0,double y1,double z0,double z1,Color c){Prism(f,new[]{new[]{x0,y0,z0},new[]{x1,y0,z0},new[]{x1,y1,z0},new[]{x0,y1,z0}},z1-z0,c);}
 List<Face> Model(){var i=Input;var f=new List<Face>();Color step=Color.FromArgb(205,208,212),land=Color.FromArgb(188,196,204),wall=Color.FromArgb(214,206,196),col=Color.FromArgb(150,150,160);double R=i.Riser,T=i.Tread,t=i.Waist,tl=i.LandingT;
  if(i.Spiral){int n=(int)i.TreadsPerTurn*Math.Max(1,(int)Math.Ceiling(i.N1/(double)i.TreadsPerTurn));double rc=i.ColumnDia/2,Ro=i.OuterRadius;for(int k=0;k<Math.Max(i.N1,4);k++){double a0=2*Math.PI*k/i.TreadsPerTurn,a1=a0+2*Math.PI/i.TreadsPerTurn;double z=(k+1)*R;Prism(f,new[]{new[]{rc*Math.Cos(a0),rc*Math.Sin(a0),z-t},new[]{Ro*Math.Cos(a0),Ro*Math.Sin(a0),z-t},new[]{Ro*Math.Cos(a1),Ro*Math.Sin(a1),z-t},new[]{rc*Math.Cos(a1),rc*Math.Sin(a1),z-t}},t,step);}
   var ring=Enumerable.Range(0,12).Select(k=>new[]{rc*Math.Cos(k*Math.PI/6),rc*Math.Sin(k*Math.PI/6),0.0}).ToArray();Prism(f,ring,(Math.Max(i.N1,4)+2)*R,col);return f;}
  foreach(var b in StairPlanView.Plan(i)){if(b.Landing){Box(f,b.X0,b.X1,b.Y0,b.Y1,b.Z-tl,b.Z,land);continue;}
   int n=b.Treads+1;double k=Math.Sqrt(R*R+T*T)/T,ts=i.Slabless?t:t*k;
   /* waist-slab flights: one solid with the steps on top and a straight sloped underside (owner, 23 November 2026: "yung 3d view
      dapat deretso lang yung underside") */
   if(!i.Slabless&&!i.Cantilever&&!i.Transverse&&!i.Spiral){
    /* one convex block per step - tread on top, the straight sloped soffit underneath - so the faces sort correctly (owner, 23 November
       2026, open-well view: "ayusin") */
    Func<double,double,double,double[]> at=(sAlong,across,z)=>b.Dx!=0?new[]{b.Dx>0?b.X0+sAlong:b.X1-sAlong,b.Y0+across,z}:new[]{b.X0+across,b.Dy>0?b.Y0+sAlong:b.Y1-sAlong,z};
    double wid=b.Dx!=0?b.Y1-b.Y0:b.X1-b.X0;Func<double,double> soffit=sAlong=>b.Z+sAlong*R/T-ts;
    for(int j=0;j<b.Treads;j++){double s0=j*T,s1=(j+1)*T,zt=b.Z+(j+1)*R;var q=new[]{new[]{s0,soffit(s0)},new[]{s0,zt},new[]{s1,zt},new[]{s1,soffit(s1)}};
     f.Add(new Face{P=q.Select(p=>at(p[0],0,p[1])).ToArray(),C=step});f.Add(new Face{P=q.Select(p=>at(p[0],wid,p[1])).Reverse().ToArray(),C=step});
     for(int e=0;e<4;e++){var p0=q[e];var p1=q[(e+1)%4];bool tread=p0[1]==zt&&p1[1]==zt;f.Add(new Face{P=new[]{at(p0[0],0,p0[1]),at(p1[0],0,p1[1]),at(p1[0],wid,p1[1]),at(p0[0],wid,p0[1])},C=tread?step:Color.FromArgb(178,182,188)});}}}
   else
   for(int j=0;j<b.Treads;j++){double s0=j*T,s1=(j+1)*T,z=b.Z+(j+1)*R;double xa,xb,ya,yb;if(b.Dx!=0){xa=b.Dx>0?b.X0+s0:b.X1-s1;xb=b.Dx>0?b.X0+s1:b.X1-s0;ya=b.Y0;yb=b.Y1;}else{ya=b.Dy>0?b.Y0+s0:b.Y1-s1;yb=b.Dy>0?b.Y0+s1:b.Y1-s0;xa=b.X0;xb=b.X1;}
    double zb=i.Slabless?z-R-t:b.Z+(j*T)*R/T-ts;if(i.Cantilever||i.Transverse)zb=z-R-t*0.6;Box(f,xa,xb,ya,yb,zb,z,step);}
   if(i.Stringer||i.Transverse||i.Cantilever){double len=b.Treads*T;if(i.Stringer)foreach(var yy in new[]{b.Y0-i.StringerB,b.Y1}){/* stringer along the flight */double x0=b.X0,x1=b.X1;f.Add(new Face{P=new[]{new[]{x0,yy,b.Z-i.StringerH},new[]{x1,yy,b.Z+b.Treads*R-i.StringerH},new[]{x1,yy,b.Z+b.Treads*R+R},new[]{x0,yy,b.Z+R}},C=col});f.Add(new Face{P=new[]{new[]{x0,yy+i.StringerB,b.Z-i.StringerH},new[]{x1,yy+i.StringerB,b.Z+b.Treads*R-i.StringerH},new[]{x1,yy+i.StringerB,b.Z+b.Treads*R+R},new[]{x0,yy+i.StringerB,b.Z+R}},C=col});}
    else{/* the wall(s) along the flight, following its slope */double hi=b.Treads*R;foreach(var yy in i.Cantilever?new[]{b.Y0}:new[]{b.Y0-1,b.Y1+1})f.Add(new Face{P=new[]{new[]{b.X0,yy,b.Z-400},new[]{b.X1,yy,b.Z+hi-400},new[]{b.X1,yy,b.Z+hi+R+500},new[]{b.X0,yy,b.Z+R+500}},C=wall});}}}
  /* the support beams: under the outer edge of each landing, and at a flight end that has no landing (floor beam) */
  bool along=!i.Cantilever&&!i.Transverse&&!i.Stringer&&!i.Spiral&&!i.Slabless;if(along&&i.OnBeams){Color beam=Color.FromArgb(170,176,184);var boxes=StairPlanView.Plan(i);double bw=i.BeamB,bh=i.BeamH;
   foreach(var b in boxes){if(b.Landing){bool flightAtX1=boxes.Any(o=>!o.Landing&&o.Dx!=0&&Math.Abs((o.Dx>0?o.X0:o.X1)-b.X1)<1&&o.Y0<b.Y1&&o.Y1>b.Y0||!o.Landing&&o.Dx!=0&&Math.Abs((o.Dx>0?o.X1:o.X0)-b.X1)<1&&o.Y0<b.Y1&&o.Y1>b.Y0);
     double ex=flightAtX1?b.X0:b.X1;Box(f,flightAtX1?ex:ex-bw,flightAtX1?ex+bw:ex,b.Y0,b.Y1,b.Z-bh,b.Z,beam);continue;}
    if(b.Dx==0)continue;double xs=b.Dx>0?b.X0:b.X1,xe=b.Dx>0?b.X1:b.X0;double zTop=b.Z+(b.Treads+1)*R;
    bool startOk=boxes.Any(o=>o.Landing&&o.Y0<b.Y1&&o.Y1>b.Y0&&(Math.Abs(o.X0-xs)<1||Math.Abs(o.X1-xs)<1)&&Math.Abs(o.Z-b.Z)<1),endOk=boxes.Any(o=>o.Landing&&o.Y0<b.Y1&&o.Y1>b.Y0&&(Math.Abs(o.X0-xe)<1||Math.Abs(o.X1-xe)<1)&&Math.Abs(o.Z-zTop)<1);
    if(!startOk)Box(f,b.Dx>0?xs-bw:xs,b.Dx>0?xs:xs+bw,b.Y0,b.Y1,b.Z-bh,b.Z,beam);if(!endOk)Box(f,b.Dx>0?xe:xe-bw,b.Dx>0?xe+bw:xe,b.Y0,b.Y1,zTop-bh,zTop,beam);}}
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
  /* two lines so the title fits the narrower view beside the inputs */Text(g,m.Name,r.Width/2,8,9f,true);Text(g,"factored 1.2D + 1.6L, span "+Staircase.F(L,"0.00")+" m"+(m.Support=="Cantilever"?", cantilever from x = 0":", "+m.Support.ToLowerInvariant()),r.Width/2,21,8f);
  /* loads */float y0=30;g.DrawLine(Pens.Black,left,y0+band*.7f,left+w,y0+band*.7f);double wmax=m.Segs.Max(s=>1.2*s.Wd+1.6*s.Wl);if(wmax<=0)wmax=1;
  foreach(var s in m.Segs){double a=m.X0(s);float xa=left+(float)(a/L*w),xb=left+(float)((a+s.Length)/L*w);double q=1.2*s.Wd+1.6*s.Wl;float hgt=(float)(band*.5*q/wmax);using(var br=new SolidBrush(Color.FromArgb(s.Flight?70:40,30,100,170)))g.FillRectangle(br,xa,y0+band*.7f-hgt,xb-xa,hgt);g.DrawRectangle(Pens.SteelBlue,xa,y0+band*.7f-hgt,xb-xa,hgt);Text(g,Staircase.F(q,"0.00")+" kN/m",(xa+xb)/2,y0+band*.7f-hgt-8,8);Text(g,s.Name,(xa+xb)/2,y0+band*.7f+10,7.5f,false,Brushes.DimGray);}
  var joints=m.Segs.Select(q=>m.X0(q)).Where(q=>q>1e-6).ToList();
  Curve(g,xs,d[1],left,w,y0+band,band,"V (kN"+(Input.Cantilever||Input.Spiral||Input.Transverse||Input.Stringer?"":"/m")+")",Color.FromArgb(28,122,84),true,joints.ToArray());Curve(g,xs,d[2],left,w,y0+2*band,band,"M (kN·m"+(Input.Cantilever||Input.Spiral||Input.Transverse||Input.Stringer?"":"/m")+")",Color.FromArgb(190,60,40),false,joints.ToArray());
  Text(g,"V: + above the axis   ·   M: drawn on the tension side (sagging below, hogging above)",left+w/2,y0+3*band+8,7f,false,Brushes.DimGray);}
 /* sign: positive shear is drawn above the axis (owner, 23 November 2026: "sure ka dito na drawn correctly?" - it was drawn below);
    the moment is drawn on the tension side (sagging below, hogging above) */
 static void Curve(Graphics g,double[] xs,double[] ys,float left,float w,float top,float band,string name,Color c,bool positiveUp=false,double[] marks=null){double L=xs[xs.Length-1],m=Math.Max(1e-9,ys.Max(v=>Math.Abs(v)));float mid=top+band/2;g.DrawLine(Pens.Black,left,mid,left+w,mid);float sg=positiveUp?-1f:1f;
  var pts=new List<PointF>{new PointF(left,mid)};for(int k=0;k<xs.Length;k++)pts.Add(new PointF(left+(float)(xs[k]/L*w),mid+sg*(float)(ys[k]/m*band*.42)));pts.Add(new PointF(left+w,mid));using(var b=new SolidBrush(Color.FromArgb(55,c)))g.FillPolygon(b,pts.ToArray());using(var p=new Pen(c,1.6f))g.DrawLines(p,pts.ToArray());
  int imax=Array.IndexOf(ys,ys.Max()),imin=Array.IndexOf(ys,ys.Min());foreach(int k in new[]{imax,imin}.Distinct()){if(Math.Abs(ys[k])<1e-6)continue;var p=new PointF(left+(float)(xs[k]/L*w)+(k==0?48:k==xs.Length-1?-48:0),mid+sg*(float)(ys[k]/m*band*.42));bool end=k==0||k==xs.Length-1;Text(g,Staircase.F(ys[k],"0.00")+" @ "+Staircase.F(xs[k],"0.00")+" m",p.X,end?p.Y:p.Y+(sg*ys[k]>0?10:-10),8,true);}
  /* values at the segment ends (landing / flight junctions) and where the shear is zero */
  if(marks!=null)foreach(var xm in marks){if(xm<=1e-6||xm>=L-1e-6)continue;int k=0;while(k<xs.Length-1&&xs[k]<xm)k++;var p=new PointF(left+(float)(xs[k]/L*w),mid+sg*(float)(ys[k]/m*band*.42));using(var dot=new SolidBrush(c))g.FillEllipse(dot,p.X-2.5f,p.Y-2.5f,5,5);Text(g,Staircase.F(ys[k],"0.00")+" @ "+Staircase.F(xs[k],"0.00")+" m",p.X+4,p.Y+(sg*ys[k]>=0?11:-11),7.5f,false,null,StringAlignment.Near);}
  Text(g,name,left-36,mid,8.5f,true);}
}
}
