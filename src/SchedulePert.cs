using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// PERT-CPM (owner, 18 November 2026: "PERT CPM din, +/-20% ang optimistic at pessimistic"). Most likely time m = the
// activity duration from the output per hour; optimistic a = 0.8 m, pessimistic b = 1.2 m; expected te = (a + 4m + b) / 6;
// variance = ((b - a) / 6)^2. Same construction logic as the PDM, drawn the classic PERT way (owner, 18 November 2026: "bakit
// yung PERT CPM same na siya sa PDM"): activity on arrow. Every activity is an arrow from its start event to its end
// event; a start event is shared with the end event of its only predecessor when that relation is a plain FS; the other
// relations become dashed dummy / lag arrows (FS: end -> start, SS: start -> start, FF: end -> end, with the lag written
// on them). Events are circles numbered 1, 2, 3 ... from left to right (tail < head) with TE (earliest) and TL (latest). Project Te = CPM duration with te. Its variance is the sum of the variances of the activities whose
// durations drive the finish: the driving path is traced back from FINISH; an activity reached through an SS or FS link at
// its start and left at its finish counts, one only passed through by its start (SS) or its finish (FF) does not.
// Completion at 90 % / 95 % confidence = Te + 1.2816 s / Te + 1.6449 s (normal distribution).
sealed class PertActivity {internal PdmNode Node;internal double A,M,B,Te,Variance;}
sealed class PertResult {
 internal readonly List<PertActivity> Activities=new List<PertActivity>();internal double Te,Sigma;internal int P90,P95;internal readonly List<PertActivity> Chain=new List<PertActivity>();
 internal const double Optimistic=0.8,Pessimistic=1.2;
 internal static PertResult Build(PdmNetwork n){var r=new PertResult();foreach(var nd in n.Nodes){double m=nd.Row.Days,a=m*Optimistic,b=m*Pessimistic;r.Activities.Add(new PertActivity{Node=nd,A=a,M=m,B=b,Te=(a+4*m+b)/6,Variance=Math.Pow((b-a)/6,2)});}
  var act=r.Activities.ToDictionary(x=>x.Node);var end=n.Nodes.Where(x=>x.Critical&&x.EF==n.Total).OrderByDescending(x=>x.Row.Days).FirstOrDefault();
  /* trace the driving path back: state = node and whether we stand at its finish (true) or its start (false) */
  var node=end;bool atFinish=true;int guard=0;
  while(node!=null&&guard++<10000){int d=node.Row.Days;var tight=n.Links.Where(l=>l.To==node).FirstOrDefault(l=>{var p=l.From;int b=l.Type=="FS"?p.EF+l.Lag:l.Type=="SS"?p.ES+l.Lag:p.EF+l.Lag-d;return b==node.ES;});
   if(atFinish){if(tight==null||tight.Type!="FF")r.Chain.Add(act[node]);}else if(tight!=null&&tight.Type=="FF")r.Chain.Add(act[node]);
   if(tight==null)break;atFinish=tight.Type!="SS";node=tight.From;}
  r.Chain.Reverse();r.Te=n.Total;r.Sigma=Math.Sqrt(r.Chain.Sum(x=>x.Variance));r.P90=(int)Math.Ceiling(r.Te+1.2816*r.Sigma-1e-9);r.P95=(int)Math.Ceiling(r.Te+1.6449*r.Sigma-1e-9);return r;}
}
sealed class AoaEvent {internal bool ToFinish;internal int No,Level,TE=int.MinValue,TL=int.MaxValue;internal float X,Y;internal bool Critical{get{return TE==TL;}}}
sealed class AoaArrow {internal AoaEvent Tail,Head;internal PertActivity Activity;internal string Type;internal int Lag;internal bool Critical;}
sealed class PertAoa {
 internal readonly List<AoaEvent> Events=new List<AoaEvent>();internal readonly List<AoaArrow> Arrows=new List<AoaArrow>();internal float Height;internal AoaEvent Finish;
 internal const float R=21,Head=8,KeyH=60;internal float LevelW=250;/* narrowed on paper to save sheets */
 static readonly CultureInfo IC=CultureInfo.InvariantCulture;
 internal static PertAoa Build(PdmNetwork n,PertResult pert){var a=new PertAoa();int count=2+2*n.Nodes.Count;var parent=Enumerable.Range(0,count).ToArray();
  Func<int,int> find=null;find=x=>parent[x]==x?x:(parent[x]=find(parent[x]));Action<int,int> union=(x,y)=>{x=find(x);y=find(y);if(x!=y)parent[x]=y;};
  var idx=new Dictionary<PdmNode,int>();for(int i=0;i<n.Nodes.Count;i++)idx[n.Nodes[i]]=i;Func<PdmNode,int> S=nd=>2+2*idx[nd],E=nd=>3+2*idx[nd];
  var merged=new HashSet<ScheduleLink>();
  foreach(var nd in n.Nodes){var inc=n.Links.Where(l=>l.To==nd).ToList();if(inc.Count==0)union(S(nd),0);else if(inc.Count==1&&inc[0].Type=="FS"&&inc[0].Lag==0){union(S(nd),E(inc[0].From));merged.Add(inc[0]);}}
  var sinkDummies=new List<PdmNode>();var finishTails=new HashSet<int>();
  var ownEnd=new List<PdmNode>();
  /* a critical last activity ends in the FINISH event; any other last activity ends in its own event with a short
     "to FINISH" connector, so no long lines cross the whole network */
  foreach(var nd in n.Nodes.Where(x=>!n.Links.Any(l=>l.From==x))){if(!(nd.Critical&&nd.EF==n.Total)){ownEnd.Add(nd);continue;}int t=find(S(nd));if(finishTails.Add(t))union(E(nd),1);else sinkDummies.Add(nd);}
  var ev=new Dictionary<int,AoaEvent>();Func<int,AoaEvent> Ev=x=>{x=find(x);AoaEvent e;if(!ev.TryGetValue(x,out e)){e=new AoaEvent();ev[x]=e;}return e;};
  Action<int,int,int> time=(x,te,tl)=>{var e=Ev(x);e.TE=Math.Max(e.TE,te);e.TL=Math.Min(e.TL,tl);};
  time(0,0,0);time(1,n.Total,n.Total);foreach(var nd in n.Nodes){time(S(nd),nd.ES,nd.LS);time(E(nd),nd.EF,nd.LF);}
  foreach(var nd in n.Nodes)a.Arrows.Add(new AoaArrow{Tail=Ev(S(nd)),Head=Ev(E(nd)),Activity=pert.Activities.First(x=>x.Node==nd),Critical=nd.Critical});
  foreach(var l in n.Links.Where(l=>!merged.Contains(l))){int t=l.Type=="SS"?S(l.From):E(l.From),h=l.Type=="FF"?E(l.To):S(l.To);var tail=Ev(t);var head=Ev(h);if(tail==head)continue;
   var dup=a.Arrows.FirstOrDefault(x=>x.Activity==null&&x.Tail==tail&&x.Head==head);if(dup!=null){if(l.Lag>dup.Lag){dup.Lag=l.Lag;dup.Type=l.Type;}dup.Critical|=n.Driving(l);continue;}
   a.Arrows.Add(new AoaArrow{Tail=tail,Head=head,Type=l.Type,Lag=l.Lag,Critical=n.Driving(l)});}
  foreach(var nd in sinkDummies)a.Arrows.Add(new AoaArrow{Tail=Ev(E(nd)),Head=Ev(1),Type="FS",Lag=0,Critical=nd.Critical&&nd.EF==n.Total});
  /* an activity tied to its predecessors only by FF relations still needs a start event that follows them: the FF
     relation implies "start of s >= start of p + D(p) + lag - D(s)", drawn as a dashed SS lag arrow */
  foreach(var nd in n.Nodes){var st=Ev(S(nd));if(find(S(nd))==find(0)||a.Arrows.Any(x=>x.Head==st))continue;
   foreach(var l in n.Links.Where(l=>l.To==nd&&l.Type=="FF")){var tail=Ev(S(l.From));if(tail==st)continue;a.Arrows.Add(new AoaArrow{Tail=tail,Head=st,Type="SS",Lag=l.From.Row.Days+l.Lag-nd.Row.Days,Critical=n.Driving(l)&&nd.Critical});}}
  foreach(var nd in ownEnd)Ev(E(nd)).ToFinish=true;
  a.Events.AddRange(ev.Values.Distinct());
  /* levels: longest path in arrows; numbering by level, top to bottom after ordering */
  foreach(var e in a.Events)e.Level=0;for(bool changed=true;changed;){changed=false;foreach(var ar in a.Arrows)if(ar.Head.Level<ar.Tail.Level+1){ar.Head.Level=ar.Tail.Level+1;changed=true;}}
  a.Finish=Ev(1);int top=a.Events.Where(e=>e!=a.Finish).Select(e=>e.Level).DefaultIfEmpty(0).Max();a.Finish.Level=Math.Max(a.Finish.Level,top+1);/* FINISH is the last event */
  return a;}
 internal SizeF Layout(float height){Height=height;int levels=Events.Count==0?0:Events.Max(e=>e.Level);var lists=new List<List<AoaEvent>>();for(int lv=0;lv<=levels;lv++)lists.Add(Events.Where(e=>e.Level==lv).ToList());
  var order=new Dictionary<AoaEvent,int>();int k0=0;foreach(var e in Events)order[e]=k0++;
  PdmNetwork.Order(lists,e=>Arrows.Where(x=>x.Head==e).Select(x=>x.Tail),e=>Arrows.Where(x=>x.Tail==e).Select(x=>x.Head),e=>order[e]);
  int no=1;float mid=Head+(height-Head)/2;for(int lv=0;lv<lists.Count;lv++){var list=lists[lv];float step=list.Count<=1?0:Math.Min(78,(height-Head-2*R-10)/(list.Count-1));float top=mid-step*(list.Count-1)/2;
   for(int i=0;i<list.Count;i++){var e=list[i];e.No=no++;e.X=20+lv*LevelW;e.Y=top+i*step;}}
  return new SizeF(20+levels*LevelW+2*R+20,height);}
 static void Event(Graphics g,RectangleF c,string no,string te,string tl,bool crit,Font font,Font bold,Pen ring,Pen critRing,Brush ink,Brush red,Brush white){var mid=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,FormatFlags=StringFormatFlags.NoWrap};
  g.FillEllipse(white,c);var pen=crit?critRing:ring;g.DrawEllipse(pen,c);float cy=c.Y+c.Height/2;g.DrawLine(pen,c.X,cy,c.Right,cy);g.DrawLine(pen,c.X+c.Width/2,cy,c.X+c.Width/2,c.Bottom);
  g.DrawString(no,bold,crit?red:ink,new RectangleF(c.X-4,c.Y+2,c.Width+8,c.Height/2-2),mid);g.DrawString(te,font,ink,new RectangleF(c.X+1,cy,c.Width/2-1,c.Height/2-4),mid);g.DrawString(tl,font,ink,new RectangleF(c.X+c.Width/2,cy,c.Width/2-1,c.Height/2-4),mid);}
 static bool InPage(AoaEvent e,float from,float to){return e.X+R>=from&&e.X+R<to;}
 /* pageFrom / pageTo: the part of the network on this sheet (layout x); an arrow to or from another sheet ends at the sheet
    edge with the event no. and sheet no. it continues to (owner, 18 November 2026: "yung continuity per page magulo") */
 internal void Draw(Graphics g,float dx,float dy,Font font,Font bold,float pageFrom=float.MinValue/4,float pageTo=float.MaxValue/4,Func<float,int> sheetAt=null,float edgeFrom=float.NaN,float edgeTo=float.NaN){if(float.IsNaN(edgeFrom)){edgeFrom=pageFrom;edgeTo=pageTo;}var smooth=g.SmoothingMode;g.SmoothingMode=SmoothingMode.AntiAlias;Color redC=Color.FromArgb(192,40,40),lineC=Color.FromArgb(110,120,132);
  var mid=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Far,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap};var below=new StringFormat(mid){LineAlignment=StringAlignment.Near};
  using(var ink=new SolidBrush(PowTheme.Ink))using(var red=new SolidBrush(redC))using(var muted=new SolidBrush(PowTheme.Muted))using(var white=new SolidBrush(Color.White))
  using(var act=new Pen(lineC,1f){CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)})using(var critAct=new Pen(redC,1.7f){CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)})
  using(var dummy=new Pen(lineC,0.9f){DashStyle=DashStyle.Dash,CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)})using(var critDummy=new Pen(redC,1.4f){DashStyle=DashStyle.Dash,CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)})using(var ring=new Pen(PowTheme.Ink,1f))using(var critRing=new Pen(redC,1.6f)){
   var boxes=Events.Select(e=>new KeyValuePair<AoaEvent,RectangleF>(e,new RectangleF(dx+e.X-8,dy+e.Y-R-8,2*R+16,2*R+16))).ToList();var lanes=NetRoute.Corridors(boxes.Select(b=>b.Value).ToList(),dy,dy+Height);
   var labels=new List<Action>();
   /* routes are planned first - activities (critical first), then dummies - and every horizontal run reserves its band
      (with the labels of an activity) so that later arrows take another free lane instead of running on top of it */
   var grid=new List<float>(lanes);for(float y=dy+12;y<dy+Height-8;y+=13)grid.Add(y);var used=Events.Where(e=>e.ToFinish).Select(e=>new RectangleF(dx+e.X+2*R,dy+e.Y-9,74,18)).ToList();/* room for the "to FINISH" connectors */var route=new Dictionary<AoaArrow,float[]>();
   foreach(var ar in Arrows.OrderBy(x=>x.Activity==null).ThenBy(x=>!x.Critical).ThenBy(x=>x.Tail.Level)){float tx=dx+ar.Tail.X+R,ty=dy+ar.Tail.Y,hx=dx+ar.Head.X+R,hy=dy+ar.Head.Y;float xa=tx+R+14,xb=hx-R-14;if(xb<xa+10){xa=(tx+hx)/2-5;xb=xa+10;}
    bool isAct=ar.Activity!=null;var obs=boxes.Where(b=>b.Key!=ar.Tail&&b.Key!=ar.Head).Select(b=>b.Value).Concat(used).ToList();
    float lane=NetRoute.Lane(ty,hy,xa,xb,obs,grid,!isAct);route[ar]=new[]{xa,xb,lane};
    bool lab=ar.Lag!=0||ar.Type!="FS";used.Add(isAct?new RectangleF(xa,lane-17,xb-xa,33):new RectangleF(xa,lane-(lab?14:5),xb-xa,lab?28:10));}
   float left=dx+edgeFrom+3,right=dx+edgeTo-3;var ghosts=new Dictionary<AoaEvent,int>();
   foreach(var ar in Arrows.OrderBy(x=>x.Critical)){bool tin=InPage(ar.Tail,pageFrom,pageTo),hin=InPage(ar.Head,pageFrom,pageTo);if(!tin&&!hin)continue;
    float tx=dx+ar.Tail.X+R,ty=dy+ar.Tail.Y,hx=dx+ar.Head.X+R,hy=dy+ar.Head.Y;float xa=route[ar][0],xb=route[ar][1],lane=route[ar][2];
    Func<float,float,float,float,PointF> edge=(cx,cy,px,py)=>{float vx=px-cx,vy=py-cy,len=(float)Math.Sqrt(vx*vx+vy*vy);return len<0.01f?new PointF(cx+R,cy):new PointF(cx+vx/len*R,cy+vy/len*R);};
    bool isAct=ar.Activity!=null;var pen=isAct?(ar.Critical?critAct:act):(ar.Critical?critDummy:dummy);Brush tagInk=ar.Critical?(Brush)red:muted;
    string lag=isAct||(ar.Lag==0&&ar.Type=="FS")?"":ar.Type+(ar.Lag>0?" +"+ar.Lag:ar.Lag<0?" "+ar.Lag:"");float ly=lane;
    if(tin&&hin)g.DrawLines(pen,NetRoute.Path(new[]{edge(tx,ty,xa,lane),new PointF(xa,lane),new PointF(xb,lane),edge(hx,hy,xb,lane)}));
    else if(tin){/* continues on a later sheet: short run to the edge, named with the head event and its sheet; the activity
                    labels are written on the sheet where the arrow is drawn in full */
     g.DrawLines(pen,NetRoute.Path(new[]{edge(tx,ty,xa,lane),new PointF(xa,lane),new PointF(right,lane)}));string t=(lag==""?"":lag+"  ")+"to "+ar.Head.No+(sheetAt==null?"":" (sheet "+sheetAt(ar.Head.X+R)+")");
     labels.Add(()=>PdmNetwork.Tag(g,t,font,tagInk,white,right-2,ly,true));continue;}
    else{/* comes from an earlier sheet: the tail event is repeated at the left edge (dashed, with its sheet no.) and the
            arrow is drawn in full from it */
     float gx=left+6+R;ghosts[ar.Tail]=sheetAt==null?0:sheetAt(ar.Tail.X+R);xa=gx+R+14;if(xb<xa+10)xb=xa+10;
     g.DrawLines(pen,NetRoute.Path(new[]{edge(gx,ty,xa,lane),new PointF(xa,lane),new PointF(xb,lane),edge(hx,hy,xb,lane)}));}
    float w=xb-xa;if(isAct){var pa=ar.Activity;var nd=pa.Node;float x1=xa;labels.Add(()=>{g.DrawString(nd.Id+"  "+nd.Row.Code,bold,ar.Critical?(Brush)red:ink,new RectangleF(x1,ly-15,w,14),mid);
      g.DrawString("te "+pa.Te.ToString("0.#",IC)+"  ("+pa.A.ToString("0.0",IC)+" / "+pa.M.ToString("0.#",IC)+" / "+pa.B.ToString("0.0",IC)+")",font,muted,new RectangleF(x1,ly+1,w,13),below);});}
    else if(lag!=""){string t=lag;float x1=xa;labels.Add(()=>{var sz=g.MeasureString(t,font);var r=new RectangleF(x1+(w-sz.Width)/2,ly-sz.Height-1,sz.Width,sz.Height-2);g.FillRectangle(white,r);g.DrawString(t,font,ar.Critical?(Brush)red:muted,r.X,r.Y);});}}
   foreach(var l in labels)l();
   var fin=Finish;
   foreach(var e in Events.Where(x=>x.ToFinish&&InPage(x,pageFrom,pageTo))){float x0=dx+e.X+2*R,y0=dy+e.Y;g.DrawLine(dummy,x0,y0,x0+34,y0);g.DrawString("to "+(fin==null?"FINISH":fin.No.ToString(IC)),font,muted,x0+36,y0-7);}
   foreach(var gh in ghosts){var c=new RectangleF(left+6,dy+gh.Key.Y-R,2*R,2*R);using(var dash=new Pen(PowTheme.Ink,0.9f){DashStyle=DashStyle.Dash}){g.FillEllipse(white,c);g.DrawEllipse(dash,c);}
    g.DrawString(gh.Key.No.ToString(IC),bold,ink,new RectangleF(c.X-6,c.Y,c.Width+12,c.Height),new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center});if(gh.Value>0)g.DrawString("sheet "+gh.Value,font,muted,new RectangleF(c.X-14,c.Bottom+1,c.Width+28,12),new StringFormat{Alignment=StringAlignment.Center});}
   foreach(var e in Events.Where(x=>InPage(x,pageFrom,pageTo)))Event(g,new RectangleF(dx+e.X,dy+e.Y-R,2*R,2*R),e.No.ToString(IC),e.TE.ToString(IC),e.TL.ToString(IC),e.Critical,font,bold,ring,critRing,ink,red,white);}
  g.SmoothingMode=smooth;}
 internal const string LegendText="PERT-CPM, activity on arrow: te = (a + 4m + b) / 6 with a = 0.8 m, b = 1.2 m (working days). Dashed = dummy / lag arrow: FS = after the finish, SS = after the start, FF = finish after the finish, + lag in days. Red = critical path.";
 internal static void DrawKey(Graphics g,float x,float y,Font font,Font bold){using(var ink=new SolidBrush(PowTheme.Ink))using(var red=new SolidBrush(Color.FromArgb(192,40,40)))using(var white=new SolidBrush(Color.White))using(var ring=new Pen(PowTheme.Ink,1f))using(var crit=new Pen(Color.FromArgb(192,40,40),1.6f))using(var act=new Pen(Color.FromArgb(110,120,132),1f){CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)})using(var dummy=new Pen(Color.FromArgb(110,120,132),0.9f){DashStyle=DashStyle.Dash,CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)}){
  g.DrawString("EVENT KEY",bold,ink,x,y);Event(g,new RectangleF(x+76,y,2*R,2*R),"i","TE","TL",false,font,bold,ring,crit,ink,red,white);
  string[] k={"i = event no. (each arrow runs from a lower to a higher no.)","TE = earliest event time, TL = latest event time (working days); TE = TL: critical"};for(int i=0;i<k.Length;i++)g.DrawString(k[i],font,ink,x+76+2*R+12,y+4+i*15);
  float ax=x+560;g.DrawLine(act,ax,y+12,ax+90,y+12);g.DrawString("activity: no. and item no. above, te (a / m / b) below; \"to n\" = connector to the finish event n",font,ink,ax+100,y+5);g.DrawLine(dummy,ax,y+34,ax+90,y+34);g.DrawString("dummy / lag arrow (e.g. \"SS +3\"), no work; dashed circle = event repeated from the sheet named under it",font,ink,ax+100,y+27);}}
}
sealed class PertView:Control {internal PertAoa Diagram;internal PertResult Pert;internal PertView(){DoubleBuffered=true;BackColor=Color.White;Font=new Font("Segoe UI",8f);}
 internal void Fit(){if(Diagram==null)return;var size=Diagram.Layout(Math.Max(500,(Parent==null?700:Parent.ClientSize.Height)-60-PertAoa.KeyH));Width=Math.Max(900,(int)size.Width+20);Height=(int)(size.Height+34+PertAoa.KeyH);Invalidate();}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);if(Diagram==null||Diagram.Events.Count==0){TextRenderer.DrawText(e.Graphics,"No activities yet.",Font,new Point(10,10),PowTheme.Muted);return;}
  using(var bold=new Font(Font,FontStyle.Bold)){TextRenderer.DrawText(e.Graphics,"PERT: Te = "+Pert.Te.ToString("0.0",CultureInfo.InvariantCulture)+" working days, s = "+Pert.Sigma.ToString("0.00",CultureInfo.InvariantCulture)+"; 90%: "+Pert.P90+" days; 95%: "+Pert.P95+" days.   "+PertAoa.LegendText,Font,new Point(8,4),PowTheme.Ink);
   PertAoa.DrawKey(e.Graphics,8,24,Font,bold);Diagram.Draw(e.Graphics,0,28+PertAoa.KeyH,Font,bold);}}}
}
