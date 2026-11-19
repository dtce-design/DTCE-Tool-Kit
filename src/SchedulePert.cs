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
 internal static PertResult Build(PdmNetwork n){var r=new PertResult();foreach(var nd in n.Nodes.Where(x=>!x.IsMilestone)){double m=nd.Row.Days,a=m*Optimistic,b=m*Pessimistic;r.Activities.Add(new PertActivity{Node=nd,A=a,M=m,B=b,Te=(a+4*m+b)/6,Variance=Math.Pow((b-a)/6,2)});}
  var act=r.Activities.ToDictionary(x=>x.Node);var end=n.Nodes.Where(x=>x.Critical&&x.EF==n.Total).OrderByDescending(x=>x.Row.Days).FirstOrDefault();/* the longest critical last activity */
  /* trace the driving path back: state = node and whether we stand at its finish (true) or its start (false) */
  var node=end;bool atFinish=true;int guard=0;
  while(node!=null&&guard++<10000){int d=node.Row.Days;var tight=n.Links.Where(l=>l.To==node).FirstOrDefault(l=>{var p=l.From;int b=l.Type=="FS"?p.EF+l.Lag:l.Type=="SS"?p.ES+l.Lag:p.EF+l.Lag-d;return b==node.ES;});
   bool counts=atFinish?(tight==null||tight.Type!="FF"):(tight!=null&&tight.Type=="FF");if(counts&&act.ContainsKey(node))r.Chain.Add(act[node]);/* milestones have no duration */
   if(tight==null)break;atFinish=tight.Type!="SS";node=tight.From;}
  r.Chain.Reverse();r.Te=n.Total;r.Sigma=Math.Sqrt(r.Chain.Sum(x=>x.Variance));r.P90=(int)Math.Ceiling(r.Te+1.2816*r.Sigma-1e-9);r.P95=(int)Math.Ceiling(r.Te+1.6449*r.Sigma-1e-9);return r;}
}
sealed class AoaEvent {internal int No,Level,TE=int.MinValue,TL=int.MaxValue;internal float X,Y;internal bool Critical{get{return TE==TL;}}}
sealed class AoaArrow {internal AoaEvent Tail,Head;internal PertActivity Activity;internal string Type;internal int Lag;internal bool Critical,Collector;internal int Score;/* routing score (0 = touches nothing) */}
sealed class PertAoa {
 internal readonly List<AoaEvent> Events=new List<AoaEvent>();internal readonly List<AoaArrow> Arrows=new List<AoaArrow>();internal float Height;internal AoaEvent Finish;
 internal const float R=21,Head=8,KeyH=60,Foot=30;internal float LevelW=250;/* narrowed on paper to save sheets */
 internal readonly List<float[]> Groups=new List<float[]>();/* x extent of every event column group */
 internal readonly List<float> Cuts=new List<float>();internal float Right;/* x positions where a sheet may end (between event columns), right end */
 static readonly CultureInfo IC=CultureInfo.InvariantCulture;
 internal static PertAoa Build(PdmNetwork n,PertResult pert){var a=new PertAoa();int count=2+2*n.Nodes.Count;var parent=Enumerable.Range(0,count).ToArray();
  Func<int,int> find=null;find=x=>parent[x]==x?x:(parent[x]=find(parent[x]));Action<int,int> union=(x,y)=>{x=find(x);y=find(y);if(x!=y)parent[x]=y;};
  var idx=new Dictionary<PdmNode,int>();for(int i=0;i<n.Nodes.Count;i++)idx[n.Nodes[i]]=i;Func<PdmNode,int> S=nd=>2+2*idx[nd],E=nd=>3+2*idx[nd];
  var merged=new HashSet<ScheduleLink>();
  /* a milestone is an event, not an arrow */
  foreach(var nd in n.Nodes.Where(x=>x.IsMilestone))union(S(nd),E(nd));
  foreach(var nd in n.Nodes){var inc=n.Links.Where(l=>l.To==nd).ToList();if(inc.Count==0)union(S(nd),0);else if(inc.Count==1&&inc[0].Type=="FS"&&inc[0].Lag==0){union(S(nd),E(inc[0].From));merged.Add(inc[0]);}}
  var sinkDummies=new List<PdmNode>();var finishTails=new HashSet<int>();
  var ownEnd=new List<PdmNode>();
  /* every last activity (nothing follows its finish) ends directly in the FINISH event, as in the textbook arrow network
     (owner, 18 November 2026: "very obvious naman na ito" - no detour); only where two such activities would share both
     events, or an FF relation ends at the activity's finish, does it keep its own end event with a dummy to FINISH */
  int lastLevel=n.Nodes.Count==0?0:n.Nodes.Max(x=>x.Level);
  foreach(var nd in n.Nodes.Where(x=>!x.IsMilestone&&!n.Links.Any(l=>l.From==x&&l.Type!="SS")).OrderBy(x=>!x.Critical).ThenByDescending(x=>x.EF)){
   bool near=(nd.Critical&&nd.EF==n.Total)||nd.Level>=lastLevel-1;/* far ones end in their own event and join FINISH along the collector below */
   bool ffIn=n.Links.Any(l=>l.To==nd&&l.Type=="FF");int t=find(S(nd));if(near&&!ffIn&&finishTails.Add(t))union(E(nd),1);else sinkDummies.Add(nd);}
  var ev=new Dictionary<int,AoaEvent>();Func<int,AoaEvent> Ev=x=>{x=find(x);AoaEvent e;if(!ev.TryGetValue(x,out e)){e=new AoaEvent();ev[x]=e;}return e;};
  Action<int,int,int> time=(x,te,tl)=>{var e=Ev(x);e.TE=Math.Max(e.TE,te);e.TL=Math.Min(e.TL,tl);};
  time(0,0,0);time(1,n.Total,n.Total);foreach(var nd in n.Nodes){time(S(nd),nd.ES,nd.LS);time(E(nd),nd.EF,nd.LF);}
  foreach(var nd in n.Nodes.Where(x=>!x.IsMilestone))a.Arrows.Add(new AoaArrow{Tail=Ev(S(nd)),Head=Ev(E(nd)),Activity=pert.Activities.First(x=>x.Node==nd),Critical=nd.Critical});
  foreach(var l in n.Links.Where(l=>!merged.Contains(l))){int t=l.Type=="SS"?S(l.From):E(l.From),h=l.Type=="FF"?E(l.To):S(l.To);var tail=Ev(t);var head=Ev(h);if(tail==head)continue;
   var dup=a.Arrows.FirstOrDefault(x=>x.Activity==null&&x.Tail==tail&&x.Head==head);if(dup!=null){if(l.Lag>dup.Lag){dup.Lag=l.Lag;dup.Type=l.Type;}dup.Critical|=n.Driving(l);continue;}
   a.Arrows.Add(new AoaArrow{Tail=tail,Head=head,Type=l.Type,Lag=l.Lag,Critical=n.Driving(l)});}
  foreach(var nd in sinkDummies)if(Ev(E(nd))!=Ev(1))a.Arrows.Add(new AoaArrow{Tail=Ev(E(nd)),Head=Ev(1),Type="FS",Lag=0,Collector=true});
  /* where an FF relation is what sets the start of an activity (EF(p) + lag - D(s) is more than its other relations
     give), the arrow network needs that limit on the start event too: a lag arrow from the start of p with lag
     D(p) + lag - D(s); otherwise the start event could come earlier than ES and the activity would stretch */
  foreach(var nd in n.Nodes.Where(x=>!x.IsMilestone)){var inc=n.Links.Where(l=>l.To==nd).ToList();var ff=inc.Where(l=>l.Type=="FF").ToList();if(ff.Count==0)continue;
   int other=inc.Where(l=>l.Type!="FF").Select(l=>l.Type=="FS"?l.From.EF+l.Lag:l.From.ES+l.Lag).DefaultIfEmpty(0).Max();
   var drive=ff.OrderByDescending(l=>l.From.EF+l.Lag-nd.Row.Days).First();int bound=drive.From.EF+drive.Lag-nd.Row.Days;if(bound<=other)continue;
   var st=Ev(S(nd));var tail=Ev(S(drive.From));if(tail==st)continue;int lag=drive.From.Row.Days+drive.Lag-nd.Row.Days;
   var dup=a.Arrows.FirstOrDefault(x=>x.Activity==null&&x.Tail==tail&&x.Head==st);if(dup!=null){if(dup.Type!="SS"||lag>dup.Lag){dup.Type="SS";dup.Lag=Math.Max(lag,dup.Type=="SS"?dup.Lag:lag);}continue;}
   a.Arrows.Add(new AoaArrow{Tail=tail,Head=st,Type="SS",Lag=lag,Critical=n.Driving(drive)&&nd.Critical});}
  /* every other event without a successor (the end of an activity followed only by SS work) also joins FINISH, so no
     finish is left open (standard: every activity except the start and finish has a predecessor and a successor) */
  var fin1=Ev(1);foreach(var e in ev.Values.Distinct().ToList())if(e!=fin1&&!a.Arrows.Any(x=>x.Tail==e))a.Arrows.Add(new AoaArrow{Tail=e,Head=fin1,Type="FS",Lag=0,Collector=true});
  a.Events.AddRange(ev.Values.Distinct());
  /* levels: longest path in arrows; numbering by level, top to bottom after ordering */
  foreach(var e in a.Events)e.Level=0;for(bool changed=true;changed;){changed=false;foreach(var ar in a.Arrows)if(ar.Head.Level<ar.Tail.Level+1){ar.Head.Level=ar.Tail.Level+1;changed=true;}}
  a.Finish=Ev(1);int top=a.Events.Where(e=>e!=a.Finish).Select(e=>e.Level).DefaultIfEmpty(0).Max();a.Finish.Level=Math.Max(a.Finish.Level,top+1);/* FINISH is the last event */
  /* a dummy to FINISH from an event in the last two columns is drawn straight to it; only the ones from farther back run
     along the collector below the network */
  foreach(var ar in a.Arrows.Where(x=>x.Collector&&x.Tail.Level>=a.Finish.Level-2))ar.Collector=false;
  /* event times by a forward and a backward pass on the arrows as drawn (activity = m, lag arrow = its lag, dummy = 0);
     critical = zero float on the drawn network */
  var order=a.Events.OrderBy(e=>e.Level).ToList();Func<AoaArrow,int> dur=x=>x.Activity!=null?(int)Math.Round(x.Activity.M):x.Lag;
  foreach(var e in order)e.TE=0;foreach(var e in order)foreach(var x in a.Arrows.Where(z=>z.Tail==e))x.Head.TE=Math.Max(x.Head.TE,e.TE+dur(x));
  int total=Math.Max(n.Total,a.Finish.TE);foreach(var e in order)e.TL=total;for(int i=order.Count-1;i>=0;i--){var e=order[i];foreach(var x in a.Arrows.Where(z=>z.Tail==e))e.TL=Math.Min(e.TL,x.Head.TL-dur(x));}
  foreach(var x in a.Arrows)x.Critical=x.Tail.TE==x.Tail.TL&&x.Head.TE==x.Head.TL&&x.Head.TL-x.Tail.TE-dur(x)==0;
  return a;}
 /* heights of the events (and the sub-column of a crowded level) for one drawing height; the x positions follow LevelW */
 float placedFor=float.NaN;readonly Dictionary<AoaEvent,int> subColumn=new Dictionary<AoaEvent,int>();readonly List<List<AoaEvent>> levelLists=new List<List<AoaEvent>>();
 internal SizeF Layout(float height){Height=height;layoutStamp++;float usable=height-Foot;if(placedFor!=height){placedFor=height;Place(usable);}return PlaceX();}
 /* events numbered column by column, top to bottom (every arrow runs from a lower to a higher number) */
 SizeF PlaceX(){int no=1;Cuts.Clear();Groups.Clear();float cx=20,sub=2*R+(LevelW-2*R)*0.5f;
  foreach(var list in levelLists){int cols=list.Count==0?1:list.Max(e=>subColumn[e])+1;foreach(var e in list.OrderBy(e=>subColumn[e]).ThenBy(e=>e.Y)){e.No=no++;e.X=cx+subColumn[e]*sub;}
   float last=cx+(cols-1)*sub;Groups.Add(new[]{cx,last+2*R});Cuts.Add(last+2*R+40);cx=last+LevelW;}
  Right=cx-LevelW+2*R+20;return new SizeF(Right,Height);}
 /* a level with more events than fit one above the other (each needs room for the labels of its arrows) is split into
    sub-columns, the events that only lead to FINISH in the last one; the order and the heights come from the layered
    placement (NetLayout): arrows of the same height run straight, long arrows keep a free lane through the columns, an
    event whose only successor is FINISH stays at the bottom of its column (short drop to the collector). A few weightings
    are tried and the one whose routed drawing has the fewest crossings and bends is kept (for networks of moderate size). */
 void Place(float usable){int levels=Events.Count==0?0:Events.Max(e=>e.Level);levelLists.Clear();subColumn.Clear();const float minStep=64,step=78;
  /* a big network keeps a row of room above and below its events for the lines that go round its crowded columns */
  bool big=Arrows.Count>300;float margin=big?step:0;int perCol=Math.Max(1,(int)((usable-Head-2*R-10-2*margin)/minStep)+1);int n=Events.Count;var index=new Dictionary<AoaEvent,int>();for(int i=0;i<n;i++)index[Events[i]]=i;
  var layerOf=new int[n];var order=new List<List<int>>();
  for(int lv=0;lv<=levels;lv++){var list=Events.Where(e=>e.Level==lv).ToList();levelLists.Add(list);if(list.Count==0)continue;int cols=(int)Math.Ceiling(list.Count/(double)perCol);
   var sorted=list.OrderBy(e=>Arrows.Any(x=>x.Tail==e&&!x.Collector)?0:1).ToList();int per=(int)Math.Ceiling(sorted.Count/(double)cols);
   for(int c=0;c<cols;c++){var part=sorted.Skip(c*per).Take(per).ToList();foreach(var e in part){subColumn[e]=c;layerOf[index[e]]=order.Count;}order.Add(part.Select(e=>index[e]).ToList());}}
  var net=new NetLayout(n,layerOf,order);foreach(var e in Events.Where(e=>Arrows.Any(x=>x.Tail==e&&x.Collector)))net.Bottom(index[e]);
  /* routing order of MakePlan (RouteOrder) */
  if(Environment.GetEnvironmentVariable("NOSPINE")==null)net.SpineBand=new[]{Head+R+5+margin,usable-R-5-margin,step,step/2,step/2};
  net.TailLanes=true;var routed=RouteOrder();
  for(int i=0;i<routed.Count;i++)net.Edge(index[routed[i].Tail],index[routed[i].Head],routed[i].Activity!=null?2:1,routed[i].Critical,i);
  net.Order(24);
  var t=new NetLayout.Tune{Mu=0.1f,Crit=1,Pass=step/2,CritRise=1,Margin=margin};{var ov=Environment.GetEnvironmentVariable("NETTUNE");if(ov!=null){var q=ov.Split(',').Select(z=>float.Parse(z,IC)).ToArray();t.Mu=q[0];t.Crit=q[1];t.CritRise=q[2];if(q.Length>3)t.Lam=q[3];if(q.Length>4)t.Near=q[4];}}net.Assign(Head+R+5,usable-R-5,step,step/2,minStep,k=>0,t);
  for(int i=0;i<n;i++)Events[i].Y=net.Y[i];if(Environment.GetEnvironmentVariable("NETDBG")!=null){PlaceX();Console.Error.WriteLine(net.Dump(i=>Events[i].No+(Events[i].Critical?"*":"")));}}
 static void Event(Graphics g,RectangleF c,string no,string te,string tl,bool crit,Font font,Font bold,Pen ring,Pen critRing,Brush ink,Brush red,Brush white){var mid=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,FormatFlags=StringFormatFlags.NoWrap};
  g.FillEllipse(white,c);var pen=crit?critRing:ring;g.DrawEllipse(pen,c);float cy=c.Y+c.Height/2;g.DrawLine(pen,c.X,cy,c.Right,cy);g.DrawLine(pen,c.X+c.Width/2,cy,c.X+c.Width/2,c.Bottom);
  g.DrawString(no,bold,crit?red:ink,new RectangleF(c.X-4,c.Y+2,c.Width+8,c.Height/2-2),mid);g.DrawString(te,font,ink,new RectangleF(c.X+1,cy,c.Width/2-1,c.Height/2-4),mid);g.DrawString(tl,font,ink,new RectangleF(c.X+c.Width/2,cy,c.Width/2-1,c.Height/2-4),mid);}
 /* drawing plan in layout coordinates for the whole network (owner, 18 November 2026: "yung diagram continuity ang need
    ko, hindi off-page connector"): every sheet shows a slice of the same continuous drawing. Activity arrows are routed
    first (critical first), then dummies; each horizontal run takes the nearest lane where neither the line nor its labels
    touch an event, another line or another label; labels are centred on the longest piece of their run that lies on one
    sheet, so no text is split by a sheet join */
 internal sealed class Plan {internal readonly List<KeyValuePair<PointF[],int>> Lines=new List<KeyValuePair<PointF[],int>>();/* 0 activity, 1 critical activity, 2 dummy, 3 critical dummy, 5 dashed collector piece without arrowhead, 6 lag arrow, 7 critical lag arrow */
  internal readonly List<PlanLabel> Labels=new List<PlanLabel>();internal readonly List<RectangleF> Boxes=new List<RectangleF>();
  internal readonly List<int> LabelLine=new List<int>();/* the line each label belongs to */}
 internal sealed class PlanLabel {internal RectangleF Rect;internal string Text;internal bool Bold,Critical,Muted,Truncated;}
 internal Plan LastPlan;
 /* x ranges printed on two sheets (the repeated event column of a continuation sheet and the arrows in front of it): an
    arrow label placed there shows on both sheets, so the arrow into a repeated event keeps its no. and te / lag */
 internal List<float[]> Repeats=new List<float[]>();
 /* the plan is made once per layout (Layout sets a new stamp), event positions, sheet joins, repeated ranges and text size,
    and is then reused by every sheet, every repaint and the drawing checks */
 int layoutStamp;readonly List<KeyValuePair<string,Plan>> plans=new List<KeyValuePair<string,Plan>>();
 string PlanKey(Graphics g,Font font,Font bold,IList<float> joins){var sb=new System.Text.StringBuilder();long h=17;
  foreach(var e in Events){h=h*31+e.X.GetHashCode();h=h*31+e.Y.GetHashCode();h=h*31+e.No;}foreach(var x in Arrows){h=h*31+x.Tail.No;h=h*31+x.Head.No;h=h*31+x.Lag;}
  sb.Append(layoutStamp).Append('|').Append(h).Append('|').Append(Events.Count).Append('|').Append(Arrows.Count).Append('|').Append(Height.ToString("R",IC)).Append('|').Append(Right.ToString("R",IC)).Append('|');
  foreach(var j in joins??new float[0])sb.Append(j.ToString("R",IC)).Append(',');sb.Append('|');foreach(var r in Repeats)sb.Append(r[0].ToString("R",IC)).Append('-').Append(r[1].ToString("R",IC)).Append(',');
  var m=g.Transform.Elements;sb.Append('|').Append(font.Name).Append(font.SizeInPoints.ToString("R",IC)).Append(font.Style).Append(bold.Name).Append(bold.SizeInPoints.ToString("R",IC)).Append(bold.Style).Append('|').Append(g.PageUnit).Append(g.PageScale.ToString("R",IC)).Append(g.DpiX.ToString("R",IC))
   .Append('|').Append(m[0].ToString("R",IC)).Append(',').Append(m[1].ToString("R",IC)).Append(',').Append(m[2].ToString("R",IC)).Append(',').Append(m[3].ToString("R",IC))
   .Append('|').Append(g.MeasureString("0",font).Height.ToString("R",IC)).Append('|').Append(g.MeasureString("te 98  (78.4 / 98 / 117.6)",font).Width.ToString("R",IC)).Append('|').Append(g.MeasureString("38  1032(1)a",bold).Width.ToString("R",IC));
  return sb.ToString();}
 internal Plan MakePlan(Graphics g,Font font,Font bold,IList<float> joins){var key=PlanKey(g,font,bold,joins);foreach(var kv in plans)if(kv.Key==key)return kv.Value;
  var plan=Route(g,font,bold,joins);plans.Insert(0,new KeyValuePair<string,Plan>(key,plan));if(plans.Count>2)plans.RemoveAt(2);return plan;}
 /* routing order: the critical arrows first (activities, then lag arrows and dummies), so the critical path keeps the
    straight lane at the height of its events; then the other activities, then the other dummies and lag arrows; shorter
    ones first. The layout (NetLayout) uses the same order for its cost of the lanes out of an event. */
 internal List<AoaArrow> RouteOrder(){return Arrows.Where(x=>!x.Collector).OrderBy(x=>!x.Critical).ThenBy(x=>x.Activity==null).ThenBy(x=>x.Head.X-x.Tail.X).ThenBy(x=>x.Tail.No).ThenBy(x=>x.Head.No).ToList();}
 /* least horizontal room kept between two labels (room for the lines that pass between them) */
 const float LabelGap=24;
 /* lane choice of the single run: a crossing of another line and a bend in drawing units of vertical travel */
 static float LaneCross=Environment.GetEnvironmentVariable("LANECROSS")==null?60:float.Parse(Environment.GetEnvironmentVariable("LANECROSS")),LaneBend=Environment.GetEnvironmentVariable("LANEBEND")==null?20:float.Parse(Environment.GetEnvironmentVariable("LANEBEND"));
 /* costs of the route by stages (in drawing units of vertical travel) */
 const float BendCost=50,CrossCost=90,ShareCost=20,OverlapCost=2000,LabelCost=5e4f,TextCost=1e5f,EventCost=1e6f;
 Plan Route(Graphics g,Font font,Font bold,IList<float> joins){var plan=new Plan();joins=joins??new float[0];float th=g.MeasureString("0",font).Height;
  var boxes=Events.ToDictionary(e=>e,e=>new RectangleF(e.X-6,e.Y-R-6,2*R+12,2*R+12));plan.Boxes.AddRange(Events.Select(e=>new RectangleF(e.X,e.Y-R,2*R,2*R)));
  /* lanes: never below the last event row (the collector to FINISH runs there) and never above the top of the drawing */
  float lowest=Events.Count==0?Height-Foot:Events.Max(e=>e.Y+R);float yTop=Head+th+2,yBottom=Math.Min(Height-Foot-th-2,lowest+10);
  var lanes=NetRoute.Corridors(boxes.Values.ToList(),0,Height-Foot).Where(y=>y>=yTop&&y<=yBottom).ToList();for(float y=yTop;y<=yBottom;y+=5)lanes.Add(y);lanes=lanes.Distinct().ToList();
  float collector=Math.Min(Height-8,lowest+16);
  var bands=new List<RectangleF>();var texts=new List<RectangleF>();var vlines=new List<RectangleF>();var allBoxes=boxes.Values.ToList();var colXs=Events.Select(e=>e.X).Distinct().OrderBy(x=>x).ToList();/* event columns (and sub-columns) */
  var groups=new List<Tuple<int,int,int,Func<float,float,float,List<RectangleF>>,float,float>>();/* line, first label, labels, label rectangles (y, centre, room), room, need */var groupFlags=new List<bool[]>();var groupLead=new List<float[]>();/* lead-in, lag label (may go below its line) */
  /* the collector to the finish event (drawn last) is reserved first, so no arrow or label is placed on it */
  {var cl=Arrows.Where(x=>x.Collector).ToList();if(cl.Count>0){float fx0=cl[0].Head.X-26;foreach(var grp in cl.GroupBy(x=>x.Tail.X)){float cx0=grp.Key+2*R+10;vlines.Add(new RectangleF(cx0-1.5f,grp.Min(x=>x.Tail.Y),3,collector-grp.Min(x=>x.Tail.Y)));}
    float c0=cl.Min(x=>x.Tail.X)+2*R+10;bands.Add(new RectangleF(c0,collector-2,fx0-c0,4));vlines.Add(new RectangleF(fx0-1.5f,Math.Min(collector,cl[0].Head.Y),3,Math.Abs(collector-cl[0].Head.Y)));}}
  Func<RectangleF,List<RectangleF>,int> hits=(r,list)=>{int c=0;foreach(var o in list)if(o.IntersectsWith(r))c++;return c;};
  Func<float,float,float,RectangleF> vr=(x,ya,yb)=>new RectangleF(x-1.5f,Math.Min(ya,yb),3,Math.Abs(yb-ya));
  foreach(var ar in RouteOrder()){
   float tx=ar.Tail.X+R,ty=ar.Tail.Y,hx=ar.Head.X+R,hy=ar.Head.Y;float xa=tx+R+14,xb=hx-R-14;if(xb<xa+10){xa=(tx+hx)/2-5;xb=xa+10;}
   bool timed=ar.Activity==null&&(ar.Lag!=0||ar.Type!="FS");/* a lag arrow carries time but no work: dash-dot; a plain dummy (no time) is dashed */
   int kind=ar.Activity!=null?(ar.Critical?1:0):timed?(ar.Critical?7:6):(ar.Critical?3:2);
   string name=null,te=null,lag=null;if(ar.Activity!=null){var pa=ar.Activity;name=pa.Node.Name+"  "+pa.Node.Row.Code;te="te "+pa.Te.ToString("0.#",IC)+"  ("+pa.A.ToString("0.0",IC)+" / "+pa.M.ToString("0.#",IC)+" / "+pa.B.ToString("0.0",IC)+")";}
   else if(timed)lag=ar.Type+(ar.Lag>0?" +"+ar.Lag:ar.Lag<0?" "+ar.Lag:"");
   float wn=name==null?0:g.MeasureString(name,bold).Width,wt=te==null?0:g.MeasureString(te,font).Width,wl=lag==null?0:g.MeasureString(lag,font).Width;
   /* orthogonal arrow: out of the right side of the tail event, a bend just past its column (xa), one horizontal run, a
      bend just before the head column (xb), into the left side of the head event. The run takes the nearest height where
      the run and both bends touch no event, no label and no other line and its labels find a free place; the labels go on
      the piece of the run that lies on one sheet, at the free place nearest its middle. If no single height is free, the
      run steps between free heights in the gaps before the columns that block it (NetRoute.Sweep). Only the obstacles
      near the arrow are looked at. */
   float need=Math.Max(wn,Math.Max(wt,wl));float lx0=xa-LabelGap-12,lx1=xb+LabelGap+12;var leadRange=Repeats.FirstOrDefault(rp=>ar.Head.X>=rp[0]&&ar.Head.X+2*R<=rp[1]&&ar.Tail.X<rp[0]);bool leadIn=leadRange!=null;
   Func<List<RectangleF>,List<RectangleF>> nearby=list=>{var r=new List<RectangleF>();foreach(var o in list)if(o.Right>lx0&&o.Left<lx1)r.Add(o);return r;};
   var obsEvents=boxes.Where(k=>k.Key!=ar.Tail&&k.Key!=ar.Head&&k.Value.Right>lx0&&k.Value.Left<lx1).Select(k=>k.Value).ToList();var nEvents=nearby(allBoxes);var nTexts=nearby(texts);var nBands=nearby(bands);var nV=nearby(vlines);
   Func<float,float,float,List<RectangleF>> labelRects=(y,cx,room)=>{var list=new List<RectangleF>();if(name!=null){float w=Math.Min(wn,room);list.Add(new RectangleF(cx-w/2,y-th-1,w,th));}if(te!=null){float w=Math.Min(wt,room);list.Add(new RectangleF(cx-w/2,y+1,w,th));}if(lag!=null){float w=Math.Min(wl,room);list.Add(new RectangleF(cx-w/2,y-th-1,w,th));}return list;};
   Func<List<RectangleF>,int> labelHits=lr=>{int c=0;foreach(var r in lr)c+=hits(r,nEvents)*10+hits(r,nTexts)*3+hits(r,nBands)*2+hits(r,nV)*2;return c;};
   /* free label centre on the piece p0..p1 of a run at height y: the label rectangles keep clear of every event (2),
      every other label (gap, sideways) and every line (3 from a vertical); the free centre nearest the middle of the piece,
      NaN when there is none */
   Func<float,float,float,float,float> slot=(y,p0,p1,gap)=>{if(p1-p0<need-0.01f)return float.NaN;float lo=p0+need/2,hi=p1-need/2,mid=(lo+hi)/2;var strips=new List<float[]>();
    if(name!=null)strips.Add(new[]{y-th-1,y-1,wn});if(te!=null)strips.Add(new[]{y+1,y+1+th,wt});if(lag!=null)strips.Add(new[]{y-th-1,y-1,wl});
    var blocked=new List<float[]>();Action<List<RectangleF>,float> block=(list,gx)=>{foreach(var st in strips){float hw=st[2]/2;foreach(var o in list)if(o.Top<st[1]&&o.Bottom>st[0]&&o.Right+gx+hw>lo&&o.Left-gx-hw<hi)blocked.Add(new[]{o.Left-gx-hw,o.Right+gx+hw});}};
    block(nEvents,2);block(nTexts,gap);block(nBands,0);block(nV,3);
    if(blocked.Count==0)return mid;blocked.Sort((u,v)=>u[0].CompareTo(v[0]));float best=float.NaN,bd=float.MaxValue,at=lo;
    Action<float,float> free=(a,b)=>{if(b-a<0.2f)return;float c=Math.Max(a+0.1f,Math.Min(b-0.1f,mid));if(Math.Abs(c-mid)<bd){bd=Math.Abs(c-mid);best=c;}};
    foreach(var b in blocked){if(b[0]>at)free(at,Math.Min(b[0],hi));at=Math.Max(at,b[1]);if(at>hi)break;}if(at<hi)free(at,hi);return best;};
   /* best label place on the horizontal pieces {y, from, to}: returns {hits, centre, room, y}. A piece printed on two
      sheets (in front of a repeated event column) is taken first when the labels fit on it */
   /* the pieces of a run {y, from, to} that lie on one sheet; a lag label may also go below its line (label height y + th + 2) */
   Func<float,float,float,List<float[]>> piecesOf=(y,x0,x1)=>{var list=new List<float[]>();float p0=x0+4;foreach(var c in joins.Where(c=>c>x0+4&&c<x1-4).OrderBy(c=>c).Concat(new[]{x1-4})){list.Add(new[]{y,p0,c});p0=c;}return list;};
   Func<float[],bool> repeated=pc=>pc[2]-pc[1]>=need+8&&(leadIn?new[]{leadRange}:Repeats.ToArray()).Any(rp=>pc[1]>=rp[0]-0.5f&&pc[2]<=rp[1]+0.5f);
   Func<float[],float,float> freeAt=(pc,gap)=>{float c=slot(pc[0],pc[1],pc[2],gap);if(float.IsNaN(c)&&lag!=null&&name==null){c=slot(pc[0]+th+2,pc[1],pc[2],gap);if(!float.IsNaN(c))pc[0]+=th+2;}return c;};
   /* best label place on the horizontal pieces {y, from, to}: returns {hits, centre, room, label y}. A piece printed on two
      sheets (in front of a repeated event column) is taken first when the labels fit on it; an arrow into a repeated
      event column (lead-in) has its labels there, elsewhere they count as a touch, so another lane is looked for */
   int away=leadIn?2:0;
   Func<List<float[]>,float[]> place=runs=>{if(need<=0)return new float[]{0,0,0,runs.Count>0?runs[0][0]:ty};float[] best=null;
    var pieces=new List<float[]>();foreach(var run in runs)pieces.AddRange(piecesOf(run[0],run[1],run[2]));
    var ordered=pieces.OrderByDescending(pc=>repeated(pc)?1:0).ThenByDescending(pc=>Math.Min(pc[2]-pc[1],need)).ToList();
    foreach(var set in new[]{ordered.Where(repeated).ToList(),ordered.Where(pc=>!repeated(pc)).ToList()}){foreach(var gap in new[]{LabelGap,6f})foreach(var pc0 in set){var pc=(float[])pc0.Clone();float c=freeAt(pc,gap);if(!float.IsNaN(c))return new float[]{repeated(pc0)?0:away,c,need,pc[0]};}}
    foreach(var pc in ordered){float len=pc[2]-pc[1];if(len<=1)continue;float room=Math.Min(len,need);float lo=pc[1]+room/2,hi=pc[2]-room/2;float m=(lo+hi)/2;
     for(int k=0;k<=60;k++){float off=(k+1)/2*6f*(k%2==0?1:-1);float cx=m+off;if(cx<lo-0.01f||cx>hi+0.01f){if(k>1&&Math.Abs(off)>(hi-lo)/2+6)break;continue;}int h=labelHits(labelRects(pc[0],cx,room))+(room<need-0.5f?6:0)+1+(repeated(pc)?0:away);
      if(best==null||h<best[0])best=new float[]{h,cx,room,pc[0]};}}
    return best??new float[]{50,(xa+xb)/2,Math.Max(1,xb-xa),runs.Count>0?runs[0][0]:ty};};
   bool dbgArrow=Environment.GetEnvironmentVariable("NETDBG2")==((int)ar.Tail.X)+"-"+((int)ar.Head.X)+"-"+((int)ty);if(dbgArrow)Console.Error.WriteLine("ARROW "+ar.Tail.No+">"+ar.Head.No+" leadIn="+leadIn+" need="+need);
   var cands=new List<float>{ty,hy};cands.AddRange(lanes);float pick=float.NaN,pickCost=float.MaxValue;int pickHits=int.MaxValue,pickTouch=0;float[] pickPlace=null;
   /* lines crossed by the run at height y and by its two bends */
   Func<float,int> crossingsAt=y=>{int c=0;foreach(var v in nV){float vx=v.X+1.5f;if(vx>xa+0.5f&&vx<xb-0.5f&&y>v.Top+0.5f&&y<v.Bottom-0.5f)c++;}
    float a1=Math.Min(ty,y),b1=Math.Max(ty,y),a2=Math.Min(y,hy),b2=Math.Max(y,hy);foreach(var b in nBands){float by=b.Y+b.Height/2;if(b.Left<xa-0.5f&&b.Right>xa+0.5f&&by>a1+0.5f&&by<b1-0.5f)c++;if(b.Left<xb-0.5f&&b.Right>xb+0.5f&&by>a2+0.5f&&by<b2-0.5f)c++;}return c;};
   /* the lane: fewest touches first, then the least of crossings, bends and vertical travel */
   double laneCost=double.MaxValue;
   foreach(var y in cands.Distinct().OrderBy(y=>Math.Abs(y-ty)+Math.Abs(y-hy))){
    if(y>yBottom+0.01f&&Math.Abs(y-ty)>0.01f&&Math.Abs(y-hy)>0.01f)continue;float dist=Math.Abs(y-ty)+Math.Abs(y-hy);if(pickHits==0&&dist>=laneCost)break;
    var band=new RectangleF(xa,y-2,xb-xa,4);var v1=vr(xa,ty,y);var v2=vr(xb,y,hy);
    int bh=hits(band,obsEvents)*20+hits(band,nTexts)*4+hits(band,nBands)*2+(hits(v1,obsEvents)+hits(v2,obsEvents))*20+(hits(v1,nTexts)+hits(v2,nTexts))*4;if(dbgArrow&&Math.Abs(y-ty)<60)Console.Error.WriteLine("   try "+y+" bh="+bh+" ev="+hits(band,obsEvents)+" tx="+hits(band,nTexts)+" bd="+hits(band,nBands)+" v1="+hits(v1,obsEvents)+"/"+hits(v1,nTexts)+" v2="+hits(v2,obsEvents)+"/"+hits(v2,nTexts));if(bh>pickHits)continue;
    double cost=crossingsAt(y)*LaneCross+dist+((Math.Abs(y-ty)>0.5f?2:0)+(Math.Abs(y-hy)>0.5f?2:0))*LaneBend-(ar.Activity==null&&Math.Abs(y-ty)<0.5f?1:0);if(bh==pickHits&&cost>=laneCost)continue;
    var pl=place(new List<float[]>{new[]{y,xa,xb}});int h=bh+(int)pl[0];if(h>pickHits||h==pickHits&&cost>=laneCost)continue;
    int touch=hits(band,obsEvents)+hits(band,nTexts)+hits(v1,obsEvents)+hits(v2,obsEvents)+hits(v1,nTexts)+hits(v2,nTexts)+(pl[0]>0?1:0);if(dbgArrow)Console.Error.WriteLine("  lane "+y+" bh="+bh+" pl="+pl[0]+" cost="+cost);
    pickHits=h;laneCost=cost;pick=y;pickPlace=pl;pickTouch=touch;}
   List<PointF> route=new List<PointF>{new PointF(tx+R,ty),new PointF(xa,ty),new PointF(xa,pick),new PointF(xb,pick),new PointF(xb,hy),new PointF(hx-R,hy)};
   /* no clean single height (a touch, or no free place for the labels): the cheapest route by stages (NetRoute.Staged),
      one height per stage and a step only just in front of a column it passes; it is taken when it touches no event, no
      label and no other line and its labels find a free place */
   if(pickHits>0){
    var ys=lanes.Concat(new[]{ty,hy}).Select(y=>(float)Math.Round(y,2)).Distinct().OrderBy(y=>y).ToArray();int L=ys.Length;
    /* stage boundaries: a step just in front of every event column passed and, for a lead-in, just after the join where
       the continuation sheet starts (so the last stage can take a height where the labels fit on that sheet) */
    var steps=colXs.Where(cx=>cx>ar.Tail.X+2*R+1&&cx<ar.Head.X-1).Select(cx=>cx-26).ToList();if(leadIn)steps.Add(leadRange[0]+12);
    var bx=new List<float>{xa};foreach(var x0 in steps.OrderBy(x=>x)){float x=x0;int tries=0;while(joins.Any(j=>Math.Abs(j-x)<10)&&tries++<10)x+=x0>=(leadIn?leadRange[0]:float.MaxValue)?2:-6;if(x>bx[bx.Count-1]+30&&x<xb-30)bx.Add(x);}bx.Add(xb);int S=bx.Count-1;
    var hc=new float[S,L];var labC=new float[S,L];
    for(int st=0;st<S;st++){float x0=bx[st],x1=bx[st+1];var diff=new double[L+1];for(int i=0;i<L;i++)labC[st,i]=float.NaN;
     foreach(var o in obsEvents)if(o.Right>x0&&o.Left<x1)NetRoute.AddOpen(diff,ys,o.Top-2,o.Bottom+2,EventCost);
     foreach(var o in nTexts)if(o.Right>x0&&o.Left<x1)NetRoute.AddOpen(diff,ys,o.Top-2,o.Bottom+2,TextCost);
     foreach(var o in nBands)if(Math.Min(o.Right,x1)-Math.Max(o.Left,x0)>1){float c=o.Y+o.Height/2;NetRoute.AddOpen(diff,ys,c-4,c+4,OverlapCost);}
     foreach(var v in nV){float vx=v.X+1.5f;if(vx>x0+0.5f&&vx<x1-0.5f)NetRoute.AddOpen(diff,ys,v.Top+0.5f,v.Bottom-0.5f,CrossCost);}
     double run=0;for(int i=0;i<L;i++){run+=diff[i];hc[st,i]=(float)run;}}
    Func<int,int,float> lab=(st,i)=>{if(!float.IsNaN(labC[st,i]))return labC[st,i];float v=LabelCost;
     if(hc[st,i]<TextCost)foreach(var pc in piecesOf(ys[i],bx[st],bx[st+1])){if(leadIn&&!repeated(pc))continue;if(!float.IsNaN(freeAt((float[])pc.Clone(),6))){v=0;break;}}
     labC[st,i]=v;return v;};
    Func<float,int,Func<float,float,float>> vertical=(x,role)=>{var hard=new List<float[]>();foreach(var o in obsEvents)if(o.Left<x+1.5f&&o.Right>x-1.5f)hard.Add(new[]{o.Top,o.Bottom,EventCost});foreach(var o in nTexts)if(o.Left<x+1.5f&&o.Right>x-1.5f)hard.Add(new[]{o.Top,o.Bottom,TextCost});
     var cy=nBands.Where(b=>b.Left<x-0.5f&&b.Right>x+0.5f).Select(b=>b.Y+b.Height/2).OrderBy(v=>v).ToArray();var ov=new List<float[]>();foreach(var v in nV)if(Math.Abs(v.X+1.5f-x)<3)ov.Add(new[]{v.Top,v.Bottom});
     return (ya,yb)=>{float lo=Math.Min(ya,yb),hi=Math.Max(ya,yb);if(hi-lo<0.01f)return 0;float c=hi-lo+2*BendCost;foreach(var h in hard)if(h[1]>lo&&h[0]<hi)c+=h[2];c+=CrossCost*NetRoute.Between(cy,lo+0.5f,hi-0.5f);foreach(var o in ov)if(Math.Min(o[1],hi)-Math.Max(o[0],lo)>1)c+=ShareCost;return c;};};
    float total;var laneOf=NetRoute.Staged(ys,bx,ty,hy,hc,need>0?lab:null,vertical,BendCost,i=>0,out total);
    if(dbgArrow)Console.Error.WriteLine("  staged total="+total+" S="+S+" lanes="+string.Join(",",laneOf.Select(q=>ys[q])));
    /* taken when it is clean (no event, label or overlap) and better, or when the single height touches an event or a
       label or leaves the labels without a free place and the stepped route does not (crossings and overlaps aside) */
    if(total<OverlapCost||total<LabelCost&&pickTouch>0){var sr=new List<PointF>{new PointF(tx+R,ty),new PointF(xa,ty)};var sruns=new List<float[]>();
     for(int st=0;st<S;st++){float y=ys[laneOf[st]];sr.Add(new PointF(bx[st],y));sr.Add(new PointF(bx[st+1],y));if(sruns.Count>0&&Math.Abs(sruns[sruns.Count-1][0]-y)<0.01f)sruns[sruns.Count-1][2]=bx[st+1];else sruns.Add(new[]{y,bx[st],bx[st+1]});}
     sr.Add(new PointF(xb,hy));sr.Add(new PointF(hx-R,hy));var spl=place(sruns);if(dbgArrow)Console.Error.WriteLine("  staged place "+string.Join(",",spl)+" pickHits="+pickHits);if(spl[0]<pickHits||pickTouch>0&&spl[0]==0){route=sr;pickPlace=spl;pickHits=(int)spl[0];}}}
   if(pickHits>=4){/* no free single height: step between free heights (NetRoute.Sweep) and keep it if it touches less */int cost;var hard=obsEvents.Concat(nTexts.Select(t=>RectangleF.Inflate(t,1,1))).ToList();
    var alt=NetRoute.Sweep(tx+R,ty,xa,xb,hx-R,hy,hard,nBands,joins,yTop-4,yBottom+4,out cost,lanes);if(cost>=100){/* no way round the labels: keep clear of the events at least */alt=NetRoute.Sweep(tx+R,ty,xa,xb,hx-R,hy,obsEvents,nBands.Concat(nTexts).ToList(),joins,yTop-4,yBottom+4,out cost,lanes);}var ap=NetRoute.Path(alt.ToArray());var runs=new List<float[]>();int score=0;
    for(int k=0;k+1<ap.Length;k++){var p=ap[k];var q=ap[k+1];if(Math.Abs(p.Y-q.Y)<0.01f){var rr=new RectangleF(Math.Min(p.X,q.X),p.Y-2,Math.Abs(q.X-p.X),4);score+=hits(rr,obsEvents)*20+hits(rr,nTexts)*4+hits(rr,nBands)*2;if(k>0&&k+2<ap.Length&&q.X>p.X)runs.Add(new[]{p.Y,p.X,q.X});}
     else{var rr=vr(p.X,p.Y,q.Y);score+=hits(rr,obsEvents)*20+hits(rr,nTexts)*4;}}
    if(runs.Count==0)runs.Add(new[]{ty,xa,xb});var pl=place(runs);score+=(int)pl[0]+2;
    /* crossing an event is never accepted when the stepped route avoids it */
    Func<IList<PointF>,int> eventHits=pts=>{int c=0;for(int k=0;k+1<pts.Count;k++){var p=pts[k];var q=pts[k+1];var rr=Math.Abs(p.Y-q.Y)<0.01f?new RectangleF(Math.Min(p.X,q.X),p.Y-2,Math.Abs(q.X-p.X),4):vr(p.X,p.Y,q.Y);c+=hits(rr,obsEvents);}return c;};
    int pe=eventHits(NetRoute.Path(route.ToArray())),ae=eventHits(ap);if(ae<pe||(ae==pe&&score<pickHits)){route=alt;pickPlace=pl;pickHits=score;}}
   if(dbgArrow)Console.Error.WriteLine("  final "+string.Join(" ",route.Select(q=>(int)q.X+","+(int)q.Y))+" label "+string.Join(",",pickPlace));
   ar.Score=pickHits;var path=NetRoute.Path(route.ToArray());
   for(int k=0;k+1<path.Length;k++){var p=path[k];var q=path[k+1];if(Math.Abs(p.Y-q.Y)<0.01f)bands.Add(new RectangleF(Math.Min(p.X,q.X),p.Y-2,Math.Abs(q.X-p.X),4));else if(Math.Abs(p.X-q.X)<0.01f)vlines.Add(vr(p.X,p.Y,q.Y));}
   float ly=pickPlace[3];var rects=need>0?labelRects(ly,pickPlace[1],pickPlace[2]):new List<RectangleF>();texts.AddRange(rects);
   plan.Lines.Add(new KeyValuePair<PointF[],int>(path,kind));
   if(rects.Count>0){groups.Add(Tuple.Create(plan.Lines.Count-1,plan.Labels.Count,rects.Count,labelRects,pickPlace[2],need));groupFlags.Add(new[]{leadIn,lag!=null&&name==null});groupLead.Add(leadRange);}
   int ri=0;float rm=pickPlace[2];if(name!=null)plan.Labels.Add(new PlanLabel{Rect=rects[ri++],Text=name,Bold=true,Critical=ar.Critical,Truncated=rm<wn-0.5f});if(te!=null)plan.Labels.Add(new PlanLabel{Rect=rects[ri++],Text=te,Muted=true,Truncated=rm<wt-0.5f});if(lag!=null)plan.Labels.Add(new PlanLabel{Rect=rects[ri++],Text=lag,Critical=ar.Critical,Muted=!ar.Critical,Truncated=rm<wl-0.5f});
   while(plan.LabelLine.Count<plan.Labels.Count)plan.LabelLine.Add(plan.Lines.Count-1);}
  /* collector: from each last event a short dummy to the right, one vertical line per event column down to the collector
     and one collector line to the finish event, drawn once so the dashes stay readable */
  var coll=Arrows.Where(x=>x.Collector).ToList();if(coll.Count>0){var fin=coll[0].Head;float fx=fin.X-26;var colX=new List<float>();
   foreach(var grp in coll.GroupBy(x=>x.Tail.X)){float cx0=grp.Key+2*R+10;colX.Add(cx0);foreach(var ar in grp)plan.Lines.Add(new KeyValuePair<PointF[],int>(new[]{new PointF(ar.Tail.X+2*R,ar.Tail.Y),new PointF(cx0,ar.Tail.Y)},5));
    plan.Lines.Add(new KeyValuePair<PointF[],int>(new[]{new PointF(cx0,grp.Min(x=>x.Tail.Y)),new PointF(cx0,collector)},5));}
   plan.Lines.Add(new KeyValuePair<PointF[],int>(NetRoute.Path(new[]{new PointF(colX.Min(),collector),new PointF(fx,collector),new PointF(fx,fin.Y),new PointF(fin.X,fin.Y)}),2));}
  /* lead-in repair: an arrow into a repeated event column whose labels found no place on the continuation sheet steps,
     just after the join where that sheet starts, to the nearest height where the step and the run touch no event and no
     label and the labels fit; the run then goes on to the arrowhead as before */
  for(int gi=0;gi<groups.Count;gi++){var lr=groupLead[gi];if(lr==null)continue;var gp=groups[gi];int own=gp.Item1;var mine=plan.Labels.Skip(gp.Item2).Take(gp.Item3).Select(l=>l.Rect).ToList();
   if(mine.Any(r=>r.Left>=lr[0]&&r.Right<=lr[1]))continue;var path=plan.Lines[own].Key;float x0s=lr[0]+12;int seg=-1;
   for(int k=1;k+2<path.Length;k++)if(Math.Abs(path[k].Y-path[k+1].Y)<0.01f&&path[k].X<x0s-10&&path[k+1].X>x0s+gp.Item6+8){seg=k;break;}
   if(seg<0)continue;var p=path[seg];var q=path[seg+1];var nx=path[seg+2];if(Math.Abs(nx.X-q.X)>0.01f)continue;/* the run is followed by the drop into the head */
   var otherLabels=plan.Labels.Where((l,i)=>i<gp.Item2||i>=gp.Item2+gp.Item3).Select(l=>l.Rect).ToList();var evs=boxes.Values.Where(b=>b.Right>x0s-3&&b.Left<q.X+3).ToList();
   var hs=new List<RectangleF>();var vs=new List<RectangleF>();for(int i=0;i<plan.Lines.Count;i++){if(i==own)continue;var pl=plan.Lines[i].Key;for(int k=0;k+1<pl.Length;k++){var a0=pl[k];var b0=pl[k+1];if(Math.Abs(a0.Y-b0.Y)<0.01f)hs.Add(new RectangleF(Math.Min(a0.X,b0.X),a0.Y-2,Math.Abs(b0.X-a0.X),4));else if(Math.Abs(a0.X-b0.X)<0.01f)vs.Add(vr(a0.X,a0.Y,b0.Y));}}
   Func<RectangleF,bool> clear=r=>!evs.Any(b=>b.IntersectsWith(r))&&!otherLabels.Any(b=>b.IntersectsWith(r));
   PointF[] np=null;List<RectangleF> nl=null;
   for(float xs=x0s;np==null&&xs<q.X-gp.Item6-12;xs+=6){if(joins.Any(j=>Math.Abs(j-xs)<10))continue;
    foreach(var y2 in lanes.Where(y=>Math.Abs(y-p.Y)>3).OrderBy(y=>Math.Abs(y-p.Y))){
     /* the drop into the head below / above the new height is part of the old drop when y2 lies between */
     var v1=vr(xs,p.Y,y2);var run=new RectangleF(xs,y2-2,q.X-xs,4);bool inside=y2>Math.Min(p.Y,nx.Y)&&y2<Math.Max(p.Y,nx.Y);var v2=vr(q.X,y2,nx.Y);
     if(!clear(v1)||!clear(run)||!inside&&!clear(v2)||hs.Any(b=>b.IntersectsWith(run)))continue;
     var rs=new List<RectangleF>();float lo=xs+4+gp.Item6/2,hi=q.X-4-gp.Item6/2;bool ok=false;if(hi<lo)continue;
     for(float st=0;Math.Abs(st)<=(hi-lo)/2+0.01f;st=st<=0?-st+3:-st){rs=gp.Item4(y2,(lo+hi)/2+st,gp.Item5);if(rs.Any(r=>!clear(r)||r.Left<lr[0]||r.Right>lr[1]||hs.Any(b=>b.IntersectsWith(r))||vs.Any(b=>b.IntersectsWith(r))||joins.Any(j=>r.Left<j&&r.Right>j)))continue;ok=true;break;}
     if(!ok)continue;var list=path.Take(seg+1).ToList();list.Add(new PointF(xs,p.Y));list.Add(new PointF(xs,y2));list.Add(new PointF(q.X,y2));list.AddRange(path.Skip(seg+2));np=NetRoute.Path(list.ToArray());nl=rs;break;}}
   if(np==null)continue;plan.Lines[own]=new KeyValuePair<PointF[],int>(np,plan.Lines[own].Value);for(int i=0;i<gp.Item3;i++)plan.Labels[gp.Item2+i].Rect=nl[i];}
  /* a label crossed by a line routed after it is moved along its own arrow to a place no other line, label or event
     touches and no sheet join cuts (the text would otherwise hide part of that line) */
  for(int gi=0;gi<groups.Count;gi++){var gp=groups[gi];int own=gp.Item1;bool lead=groupFlags[gi][0],flip=groupFlags[gi][1];
   Func<List<RectangleF>,bool> crossed=rs=>rs.Any(r=>{var inner=RectangleF.Inflate(r,-1.5f,-1.5f);for(int i=0;i<plan.Lines.Count;i++){if(i==own)continue;var pl=plan.Lines[i].Key;for(int k=0;k+1<pl.Length;k++)if(ScheduleSet.Crosses(pl[k],pl[k+1],inner))return true;}return false;});
   if(!crossed(plan.Labels.Skip(gp.Item2).Take(gp.Item3).Select(l=>l.Rect).ToList()))continue;
   var others=plan.Labels.Where((l,i)=>i<gp.Item2||i>=gp.Item2+gp.Item3).Select(l=>l.Rect).ToList();var circles=plan.Boxes.Select(b=>RectangleF.Inflate(b,3,3)).ToList();
   /* the pieces of the arrow's runs between the sheet joins, those printed on two sheets first (a lead-in label stays
      there when it can); a lag label may also go below its line */
   var path=plan.Lines[own].Key;List<RectangleF> found=null;var cands=new List<float[]>();
   for(int k=0;k+1<path.Length;k++){var p=path[k];var q=path[k+1];if(Math.Abs(p.Y-q.Y)>0.01f||q.X<p.X+1)continue;float p0=p.X+4;
    foreach(var c in joins.Where(c=>c>p.X+4&&c<q.X-4).OrderBy(c=>c).Concat(new[]{q.X-4})){cands.Add(new[]{p.Y,p0,c});if(flip)cands.Add(new[]{p.Y+th+2,p0,c});p0=c;}}
   Func<float[],bool> rep=pc=>(lead?new[]{groupLead[gi]}:Repeats.ToArray()).Any(rp=>pc[1]>=rp[0]-0.5f&&pc[2]<=rp[1]+0.5f);
   foreach(var pc in cands.OrderByDescending(pc=>rep(pc)?1:0)){if(found!=null)break;if(lead&&!rep(pc)&&cands.Any(rep))continue;float len=pc[2]-pc[1],room=Math.Min(len,gp.Item6);if(room<gp.Item5-0.5f)continue;
    float lo=pc[1]+room/2,hi=pc[2]-room/2;for(float step=0;Math.Abs(step)<=(hi-lo)/2+0.01f;step=step<=0?-step+3:-step){var rs=gp.Item4(pc[0],(lo+hi)/2+step,room);
     if(rs.Any(r=>others.Any(o=>o.IntersectsWith(r))||circles.Any(o=>o.IntersectsWith(r))||joins.Any(j=>r.Left<j&&r.Right>j))||crossed(rs))continue;found=rs;break;}}
   if(found!=null)for(int i=0;i<gp.Item3;i++)plan.Labels[gp.Item2+i].Rect=found[i];}
  return plan;}
 internal void Draw(Graphics g,float dx,float dy,Font font,Font bold,IList<float> joins=null){var smooth=g.SmoothingMode;g.SmoothingMode=SmoothingMode.AntiAlias;Color redC=Color.FromArgb(192,40,40),lineC=Color.FromArgb(110,120,132);var state=g.Save();g.TranslateTransform(dx,dy);
  var plan=MakePlan(g,font,bold,joins);LastPlan=plan;var near=new StringFormat{Alignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap};
  using(var ink=new SolidBrush(PowTheme.Ink))using(var red=new SolidBrush(redC))using(var muted=new SolidBrush(PowTheme.Muted))using(var white=new SolidBrush(Color.White))
  using(var act=new Pen(lineC,1f){CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)})using(var critAct=new Pen(redC,1.7f){CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)})
  using(var dummy=new Pen(lineC,0.9f){DashStyle=DashStyle.Dash,CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)})using(var critDummy=new Pen(redC,1.4f){DashStyle=DashStyle.Dash,CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)})using(var ring=new Pen(PowTheme.Ink,1f))using(var critRing=new Pen(redC,1.6f)){
   using(var plain=new Pen(lineC,0.9f){DashStyle=DashStyle.Dash})using(var lagPen=new Pen(lineC,0.9f){DashStyle=DashStyle.DashDot,CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)})using(var critLag=new Pen(redC,1.4f){DashStyle=DashStyle.DashDot,CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)}){
    var pens=new[]{act,critAct,dummy,critDummy,act,plain,lagPen,critLag};foreach(var k in new[]{5,2,6,0,3,7,1})foreach(var l in plan.Lines.Where(x=>x.Value==k))g.DrawLines(pens[k],l.Key);}
   foreach(var l in plan.Labels){g.FillRectangle(white,l.Rect);g.DrawString(l.Text,l.Bold?bold:font,l.Critical?(Brush)red:l.Muted?muted:ink,l.Rect,near);}
   foreach(var e in Events)Event(g,new RectangleF(e.X,e.Y-R,2*R,2*R),e.No.ToString(IC),e.TE.ToString(IC),e.TL.ToString(IC),e.Critical,font,bold,ring,critRing,ink,red,white);}
  g.Restore(state);g.SmoothingMode=smooth;}
 internal const string LegendText="PERT-CPM, activity on arrow, working days. Red = critical path. Shaded column on a continuation sheet = the last event column of the sheet before, repeated; the lines carry on from it.";
 internal static void DrawKey(Graphics g,float x,float y,Font font,Font bold){using(var ink=new SolidBrush(PowTheme.Ink))using(var red=new SolidBrush(Color.FromArgb(192,40,40)))using(var white=new SolidBrush(Color.White))using(var ring=new Pen(PowTheme.Ink,1f))using(var crit=new Pen(Color.FromArgb(192,40,40),1.6f))using(var act=new Pen(Color.FromArgb(110,120,132),1f){CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)})using(var dummy=new Pen(Color.FromArgb(110,120,132),0.9f){DashStyle=DashStyle.Dash,CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)}){
  g.DrawString("EVENT KEY",bold,ink,x,y);Event(g,new RectangleF(x+76,y,2*R,2*R),"i","TE","TL",false,font,bold,ring,crit,ink,red,white);
  string[] k={"i = event no. (each arrow runs from a lower to a higher no.)","TE = earliest event time, TL = latest event time; TE = TL: critical","a = 0.8 m, b = 1.2 m, so te = (a + 4m + b) / 6 = m: PERT duration = CPM duration;","the standard deviation and the 90 % / 95 % completion rest on that assumed +/-20 % spread"};for(int i=0;i<k.Length;i++)g.DrawString(k[i],font,ink,x+76+2*R+12,y+i*14);
  float ax=x+600;g.DrawLine(act,ax,y+8,ax+90,y+8);g.DrawString("activity: no. and item no. above, te (a / m / b) below",font,ink,ax+100,y+1);g.DrawLine(dummy,ax,y+24,ax+90,y+24);g.DrawString("dummy: logic only, no time",font,ink,ax+100,y+17);
  using(var lag=new Pen(Color.FromArgb(110,120,132),0.9f){DashStyle=DashStyle.DashDot,CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)}){g.DrawLine(lag,ax,y+40,ax+90,y+40);}g.DrawString("lag arrow: time, no work - SS +3 = 3 days after the start of the tail activity, FF +1 = finish 1 day after its finish, FS +7 = 7 days after its finish",font,ink,ax+100,y+33);}}
}
sealed class PertView:Control {internal PertAoa Diagram;internal PertResult Pert;internal PertView(){DoubleBuffered=true;BackColor=Color.White;Font=new Font("Segoe UI",8f);}
 internal void Fit(){if(Diagram==null)return;var size=Diagram.Layout(Math.Max(500,(Parent==null?700:Parent.ClientSize.Height)-60-PertAoa.KeyH));Width=Math.Max(900,(int)size.Width+20);Height=(int)(size.Height+34+PertAoa.KeyH);Invalidate();}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);if(Diagram==null||Diagram.Events.Count==0){TextRenderer.DrawText(e.Graphics,"No activities yet.",Font,new Point(10,10),PowTheme.Muted);return;}
  using(var bold=new Font(Font,FontStyle.Bold)){TextRenderer.DrawText(e.Graphics,"PERT: Te = "+Pert.Te.ToString("0.0",CultureInfo.InvariantCulture)+" working days, s = "+Pert.Sigma.ToString("0.00",CultureInfo.InvariantCulture)+"; 90%: "+Pert.P90+" days; 95%: "+Pert.P95+" days.   "+PertAoa.LegendText,Font,new Point(8,4),PowTheme.Ink);
   PertAoa.DrawKey(e.Graphics,8,24,Font,bold);Diagram.Draw(e.Graphics,0,28+PertAoa.KeyH,Font,bold);}}}
}
