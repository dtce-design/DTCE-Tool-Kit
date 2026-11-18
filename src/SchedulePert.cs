using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// PERT-CPM (owner, 18 November 2026: "PERT CPM din, +/-20% ang optimistic at pessimistic"). Most likely time m = the
// activity duration from the output per hour; optimistic a = 0.8 m, pessimistic b = 1.2 m; expected te = (a + 4m + b) / 6;
// variance = ((b - a) / 6)^2. The project expected time is the CPM duration along the critical path; its standard deviation
// is the square root of the summed variances of one critical activity per phase (the largest), and the completion times
// at 90 % / 95 % confidence are Te + 1.2816 s and Te + 1.6449 s (normal distribution). The network is drawn activity-on-
// arrow in the standard form (owner, 18 November 2026: "use standard layouting"): every event is a circle split into
// event no. (top), earliest event time TE (bottom left) and latest event time TL (bottom right); events are numbered
// 1, 2, 3 ... from left to right so that each arrow runs from a lower to a higher number; the activity label (activity
// no. and item no.) is written above its arrow and te (a / m / b) below; parallel activities end in their own event and a
// dashed dummy (zero duration) joins the phase event. Critical activities and events are red.
sealed class PertActivity {internal PdmNode Node;internal double A,M,B,Te,Variance;}
sealed class PertResult {
 internal readonly List<PertActivity> Activities=new List<PertActivity>();internal double Te,Sigma;internal int P90,P95;internal readonly List<PertActivity> Chain=new List<PertActivity>();
 internal const double Optimistic=0.8,Pessimistic=1.2;
 internal static PertResult Build(PdmNetwork n){var r=new PertResult();foreach(var nd in n.Nodes){double m=nd.Row.Days,a=m*Optimistic,b=m*Pessimistic;r.Activities.Add(new PertActivity{Node=nd,A=a,M=m,B=b,Te=(a+4*m+b)/6,Variance=Math.Pow((b-a)/6,2)});}
  foreach(var phase in n.Phases){var crit=r.Activities.Where(x=>phase.Contains(x.Node)&&x.Node.Critical).OrderByDescending(x=>x.Variance).FirstOrDefault();if(crit!=null)r.Chain.Add(crit);}
  r.Te=r.Chain.Sum(x=>x.Te);r.Sigma=Math.Sqrt(r.Chain.Sum(x=>x.Variance));r.P90=(int)Math.Ceiling(r.Te+1.2816*r.Sigma-1e-9);r.P95=(int)Math.Ceiling(r.Te+1.6449*r.Sigma-1e-9);return r;}
}
sealed class PertDiagram {
 internal readonly PdmNetwork Network;internal readonly PertResult Pert;internal const float PhaseW=380,EventR=21,SmallR=9,Head=24;
 internal const float KeyH=58;
 internal PertDiagram(PdmNetwork network,PertResult pert){Network=network;Pert=pert;}
 internal float Width(int from,int to){return 30+(to-from)*PhaseW+EventR*2+20;}
 static readonly CultureInfo IC=CultureInfo.InvariantCulture;
 static string T(double v){return v.ToString("0.#",IC);}
 /* event no. of the start event of phase p: 1 + (activities + 1) for every earlier phase */
 int FirstEvent(int p){int n=1;for(int q=0;q<p;q++)n+=Own(q)+1;return n;}
 /* a phase with one activity needs no end event of its own and no dummy: the arrow goes straight to the phase event */
 int Own(int p){return Network.Phases[p].Count==1?0:Network.Phases[p].Count;}
 double PhaseTime(int p){double t=0;for(int q=0;q<p;q++)t+=Pert.Activities.Where(a=>Network.Phases[q].Contains(a.Node)).Max(a=>a.Te);return t;}
 static void Event(Graphics g,RectangleF c,string no,string te,string tl,bool crit,Font font,Font bold,Pen ring,Pen critRing,Brush ink,Brush red,Brush white){var mid=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,FormatFlags=StringFormatFlags.NoWrap};
  g.FillEllipse(white,c);var pen=crit?critRing:ring;g.DrawEllipse(pen,c);float cy=c.Y+c.Height/2;g.DrawLine(pen,c.X,cy,c.Right,cy);g.DrawLine(pen,c.X+c.Width/2,cy,c.X+c.Width/2,c.Bottom);
  g.DrawString(no,bold,crit?red:ink,new RectangleF(c.X-4,c.Y+2,c.Width+8,c.Height/2-2),mid);g.DrawString(te,font,ink,new RectangleF(c.X+1,cy,c.Width/2-1,c.Height/2-4),mid);g.DrawString(tl,font,ink,new RectangleF(c.X+c.Width/2,cy,c.Width/2-1,c.Height/2-4),mid);}
 /* standard event key */
 internal static void DrawKey(Graphics g,float x,float y,Font font,Font bold){using(var ink=new SolidBrush(PowTheme.Ink))using(var red=new SolidBrush(Color.FromArgb(192,40,40)))using(var white=new SolidBrush(Color.White))using(var ring=new Pen(PowTheme.Ink,1f))using(var crit=new Pen(Color.FromArgb(192,40,40),1.6f)){
  g.DrawString("EVENT KEY",bold,ink,x,y);Event(g,new RectangleF(x+76,y,2*EventR,2*EventR),"i","TE","TL",false,font,bold,ring,crit,ink,red,white);
  string[] k={"i = event no. (each arrow runs from a lower to a higher no.)","TE = earliest event time, TL = latest event time (working days)","Arrow = activity (activity no., item no. above; te and a / m / b below); dashed = dummy","Slack of an event = TL - TE; red = critical (zero slack)"};
  for(int i=0;i<k.Length;i++)g.DrawString(k[i],font,ink,x+76+2*EventR+14,y+i*13);}}
 /* phases [from, to) starting at x = dx */
 internal void Draw(Graphics g,float dx,float dy,float height,int from,int to,Font font,Font bold){var smooth=g.SmoothingMode;g.SmoothingMode=SmoothingMode.AntiAlias;
  var mid=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap};var near=new StringFormat(mid){Alignment=StringAlignment.Near};
  Color redC=Color.FromArgb(192,40,40),lineC=Color.FromArgb(110,120,132);float midY=dy+Head+(height-Head)/2;
  using(var ink=new SolidBrush(PowTheme.Ink))using(var red=new SolidBrush(redC))using(var muted=new SolidBrush(PowTheme.Muted))using(var white=new SolidBrush(Color.White))using(var rule=new Pen(Color.FromArgb(200,206,214),0.6f){DashStyle=DashStyle.Dot})
  using(var act=new Pen(lineC,1f){CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)})using(var critAct=new Pen(redC,1.7f){CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)})using(var lead=new Pen(lineC,1f))using(var critLead=new Pen(redC,1.7f))
  using(var dummy=new Pen(lineC,1f){DashStyle=DashStyle.Dash,CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)})using(var critDummy=new Pen(redC,1.5f){DashStyle=DashStyle.Dash,CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)})using(var ring=new Pen(PowTheme.Ink,1f))using(var critRing=new Pen(redC,1.6f)){
   float x=dx+20;var chain=new HashSet<PdmNode>(Pert.Chain.Select(c=>c.Node));var events=new List<Action>();
   for(int p=from;p<to;p++){var list=Network.Phases[p];float x0=x+EventR,x1=x+PhaseW;int e0=FirstEvent(p);double t0=PhaseTime(p),t1=PhaseTime(p+1);
    g.DrawString("Phase "+(p+1)+" - "+list[0].Row.Phase,bold,ink,new RectangleF(x0+6,dy,PhaseW-2*EventR-12,Head-4),near);if(p>from)g.DrawLine(rule,x,dy,x,dy+height);
    float step=Math.Min(58,(height-Head-30)/Math.Max(1,list.Count));float top=midY-step*(list.Count-1)/2;
    for(int i=0;i<list.Count;i++){var nd=list[i];var pa=Pert.Activities.First(a=>a.Node==nd);bool crit=chain.Contains(nd);float y=top+i*step;float sx=x0+24,ex=x1-EventR-70;
     if(list.Count==1){ex=x1+EventR;}
     g.DrawLine(crit?critLead:lead,x0,midY,sx,y);g.DrawLine(crit?critAct:act,sx,y,ex-EventR,y);
     g.DrawString(nd.Id+"  "+nd.Row.Code,bold,crit?(Brush)red:ink,new RectangleF(sx,y-15,ex-sx-EventR,14),near);g.DrawString("te "+T(pa.Te)+"  ("+pa.A.ToString("0.0",IC)+" / "+T(pa.M)+" / "+pa.B.ToString("0.0",IC)+")",font,muted,new RectangleF(sx,y+1,ex-sx-EventR,13),near);
     if(list.Count==1)continue;
     g.DrawLine(crit?critDummy:dummy,ex+EventR,y,x1,midY);
     var c=new RectangleF(ex-EventR,y-EventR,2*EventR,2*EventR);string no=(e0+i+1).ToString(IC),te=T(t0+pa.Te),tl=T(t1);events.Add(()=>Event(g,c,no,te,tl,crit,font,bold,ring,critRing,ink,red,white));}
    x=x1;}
   foreach(var e in events)e();
   x=dx+20;for(int p=from;p<=to;p++){var c=new RectangleF(x,midY-EventR,2*EventR,2*EventR);string t=T(PhaseTime(p));Event(g,c,FirstEvent(p).ToString(IC),t,t,true,font,bold,ring,critRing,ink,red,white);x+=PhaseW;}}
  g.SmoothingMode=smooth;}
 internal const string LegendText="Activity on arrow (AOA), times in working days; te = (a + 4m + b) / 6 with a = 0.8 m and b = 1.2 m. Red = critical path.";
}
sealed class PertView:Control {internal PdmNetwork Network;internal PertResult Pert;internal PertView(){DoubleBuffered=true;BackColor=Color.White;Font=new Font("Segoe UI",8f);}
 internal void Fit(){if(Network==null)return;int maxRows=Network.Phases.Count==0?1:Network.Phases.Max(p=>p.Count);Width=Network.Phases.Count==0?400:(int)new PertDiagram(Network,Pert).Width(0,Network.Phases.Count)+20;Height=(int)Math.Max(420,30+maxRows*58+80+PertDiagram.KeyH);Invalidate();}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);if(Network==null||Network.Nodes.Count==0){TextRenderer.DrawText(e.Graphics,"No activities yet.",Font,new Point(10,10),PowTheme.Muted);return;}
  using(var bold=new Font(Font,FontStyle.Bold)){TextRenderer.DrawText(e.Graphics,"PERT: Te = "+Pert.Te.ToString("0.0",CultureInfo.InvariantCulture)+" working days, s = "+Pert.Sigma.ToString("0.00",CultureInfo.InvariantCulture)+";  90% confidence: "+Pert.P90+" days;  95%: "+Pert.P95+" days.   "+PertDiagram.LegendText,Font,new Point(8,4),PowTheme.Ink);
   PertDiagram.DrawKey(e.Graphics,8,24,Font,bold);new PertDiagram(Network,Pert).Draw(e.Graphics,0,24+PertDiagram.KeyH,Height-30-PertDiagram.KeyH,0,Network.Phases.Count,Font,bold);}}}
}
