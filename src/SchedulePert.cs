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
  /* a critical last activity ends in the FINISH event; any other last activity ends in its own event, joined to FINISH by a
     dummy that runs along one collector line below the network (no long lines across it) */
  foreach(var nd in n.Nodes.Where(x=>!n.Links.Any(l=>l.From==x))){if(!(nd.Critical&&nd.EF==n.Total)){ownEnd.Add(nd);continue;}int t=find(S(nd));if(finishTails.Add(t))union(E(nd),1);else sinkDummies.Add(nd);}
  var ev=new Dictionary<int,AoaEvent>();Func<int,AoaEvent> Ev=x=>{x=find(x);AoaEvent e;if(!ev.TryGetValue(x,out e)){e=new AoaEvent();ev[x]=e;}return e;};
  Action<int,int,int> time=(x,te,tl)=>{var e=Ev(x);e.TE=Math.Max(e.TE,te);e.TL=Math.Min(e.TL,tl);};
  time(0,0,0);time(1,n.Total,n.Total);foreach(var nd in n.Nodes){time(S(nd),nd.ES,nd.LS);time(E(nd),nd.EF,nd.LF);}
  foreach(var nd in n.Nodes.Where(x=>!x.IsMilestone))a.Arrows.Add(new AoaArrow{Tail=Ev(S(nd)),Head=Ev(E(nd)),Activity=pert.Activities.First(x=>x.Node==nd),Critical=nd.Critical});
  foreach(var l in n.Links.Where(l=>!merged.Contains(l))){int t=l.Type=="SS"?S(l.From):E(l.From),h=l.Type=="FF"?E(l.To):S(l.To);var tail=Ev(t);var head=Ev(h);if(tail==head)continue;
   var dup=a.Arrows.FirstOrDefault(x=>x.Activity==null&&x.Tail==tail&&x.Head==head);if(dup!=null){if(l.Lag>dup.Lag){dup.Lag=l.Lag;dup.Type=l.Type;}dup.Critical|=n.Driving(l);continue;}
   a.Arrows.Add(new AoaArrow{Tail=tail,Head=head,Type=l.Type,Lag=l.Lag,Critical=n.Driving(l)});}
  foreach(var nd in sinkDummies)a.Arrows.Add(new AoaArrow{Tail=Ev(E(nd)),Head=Ev(1),Type="FS",Lag=0,Critical=nd.Critical&&nd.EF==n.Total});
  /* where an FF relation is what sets the start of an activity (EF(p) + lag - D(s) is more than its other relations
     give), the arrow network needs that limit on the start event too: a lag arrow from the start of p with lag
     D(p) + lag - D(s); otherwise the start event could come earlier than ES and the activity would stretch */
  foreach(var nd in n.Nodes.Where(x=>!x.IsMilestone)){var inc=n.Links.Where(l=>l.To==nd).ToList();var ff=inc.Where(l=>l.Type=="FF").ToList();if(ff.Count==0)continue;
   int other=inc.Where(l=>l.Type!="FF").Select(l=>l.Type=="FS"?l.From.EF+l.Lag:l.From.ES+l.Lag).DefaultIfEmpty(0).Max();
   var drive=ff.OrderByDescending(l=>l.From.EF+l.Lag-nd.Row.Days).First();int bound=drive.From.EF+drive.Lag-nd.Row.Days;if(bound<=other)continue;
   var st=Ev(S(nd));var tail=Ev(S(drive.From));if(tail==st)continue;int lag=drive.From.Row.Days+drive.Lag-nd.Row.Days;
   var dup=a.Arrows.FirstOrDefault(x=>x.Activity==null&&x.Tail==tail&&x.Head==st);if(dup!=null){if(dup.Type!="SS"||lag>dup.Lag){dup.Type="SS";dup.Lag=Math.Max(lag,dup.Type=="SS"?dup.Lag:lag);}continue;}
   a.Arrows.Add(new AoaArrow{Tail=tail,Head=st,Type="SS",Lag=lag,Critical=n.Driving(drive)&&nd.Critical});}
  foreach(var nd in ownEnd)a.Arrows.Add(new AoaArrow{Tail=Ev(E(nd)),Head=Ev(1),Type="FS",Lag=0,Collector=true});
  /* every other event without a successor (the end of an activity followed only by SS work) also joins FINISH, so no
     finish is left open (standard: every activity except the start and finish has a predecessor and a successor) */
  var fin1=Ev(1);foreach(var e in ev.Values.Distinct().ToList())if(e!=fin1&&!a.Arrows.Any(x=>x.Tail==e))a.Arrows.Add(new AoaArrow{Tail=e,Head=fin1,Type="FS",Lag=0,Collector=true});
  a.Events.AddRange(ev.Values.Distinct());
  /* levels: longest path in arrows; numbering by level, top to bottom after ordering */
  foreach(var e in a.Events)e.Level=0;for(bool changed=true;changed;){changed=false;foreach(var ar in a.Arrows)if(ar.Head.Level<ar.Tail.Level+1){ar.Head.Level=ar.Tail.Level+1;changed=true;}}
  a.Finish=Ev(1);int top=a.Events.Where(e=>e!=a.Finish).Select(e=>e.Level).DefaultIfEmpty(0).Max();a.Finish.Level=Math.Max(a.Finish.Level,top+1);/* FINISH is the last event */
  return a;}
 internal SizeF Layout(float height){Height=height;float usable=height-Foot;int levels=Events.Count==0?0:Events.Max(e=>e.Level);var lists=new List<List<AoaEvent>>();for(int lv=0;lv<=levels;lv++)lists.Add(Events.Where(e=>e.Level==lv).ToList());
  var order=new Dictionary<AoaEvent,int>();int k0=0;foreach(var e in Events)order[e]=k0++;
  PdmNetwork.Order(lists,e=>Arrows.Where(x=>x.Head==e&&!x.Collector).Select(x=>x.Tail),e=>Arrows.Where(x=>x.Tail==e&&!x.Collector).Select(x=>x.Head),e=>order[e]);
  /* a level with more events than fit one above the other (each needs room for the labels of its arrows) is split into
     staggered sub-columns; the next level starts after the last sub-column */
  int no=1;float mid=Head+(usable-Head)/2;Cuts.Clear();const float minStep=64;int perCol=Math.Max(1,(int)((usable-Head-2*R-10)/minStep)+1);float cx=20,sub=2*R+(LevelW-2*R)*0.5f;
  for(int lv=0;lv<lists.Count;lv++){var list=lists[lv];int cols=Math.Max(1,(int)Math.Ceiling(list.Count/(double)perCol));
   for(int c=0;c<cols;c++){var part=list.Skip(c*perCol).Take(perCol).ToList();float step=part.Count<=1?0:Math.Min(78,(usable-Head-2*R-10)/(part.Count-1));float top=mid-step*(part.Count-1)/2;
    for(int i=0;i<part.Count;i++){var e=part[i];e.No=no++;e.X=cx+c*sub;e.Y=top+i*step+(cols>1&&c%2==1?Math.Min(step,78)/2:0);}}
   float last=cx+(cols-1)*sub;Cuts.Add(last+2*R+40);cx=last+LevelW;}
  Right=cx-LevelW+2*R+20;return new SizeF(Right,height);}

 static void Event(Graphics g,RectangleF c,string no,string te,string tl,bool crit,Font font,Font bold,Pen ring,Pen critRing,Brush ink,Brush red,Brush white){var mid=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,FormatFlags=StringFormatFlags.NoWrap};
  g.FillEllipse(white,c);var pen=crit?critRing:ring;g.DrawEllipse(pen,c);float cy=c.Y+c.Height/2;g.DrawLine(pen,c.X,cy,c.Right,cy);g.DrawLine(pen,c.X+c.Width/2,cy,c.X+c.Width/2,c.Bottom);
  g.DrawString(no,bold,crit?red:ink,new RectangleF(c.X-4,c.Y+2,c.Width+8,c.Height/2-2),mid);g.DrawString(te,font,ink,new RectangleF(c.X+1,cy,c.Width/2-1,c.Height/2-4),mid);g.DrawString(tl,font,ink,new RectangleF(c.X+c.Width/2,cy,c.Width/2-1,c.Height/2-4),mid);}
 /* drawing plan in layout coordinates for the whole network (owner, 18 November 2026: "yung diagram continuity ang need
    ko, hindi off-page connector"): every sheet shows a slice of the same continuous drawing. Activity arrows are routed
    first (critical first), then dummies; each horizontal run takes the nearest lane where neither the line nor its labels
    touch an event, another line or another label; labels are centred on the longest piece of their run that lies on one
    sheet, so no text is split by a sheet join */
 internal sealed class Plan {internal readonly List<KeyValuePair<PointF[],int>> Lines=new List<KeyValuePair<PointF[],int>>();/* 0 activity, 1 critical activity, 2 dummy, 3 critical dummy, 5 dashed collector piece without arrowhead, 6 lag arrow, 7 critical lag arrow */
  internal readonly List<PlanLabel> Labels=new List<PlanLabel>();internal readonly List<RectangleF> Boxes=new List<RectangleF>();}
 internal sealed class PlanLabel {internal RectangleF Rect;internal string Text;internal bool Bold,Critical,Muted,Truncated;}
 internal Plan LastPlan;
 internal Plan MakePlan(Graphics g,Font font,Font bold,IList<float> joins){var plan=new Plan();joins=joins??new float[0];float th=g.MeasureString("0",font).Height;
  var boxes=Events.ToDictionary(e=>e,e=>new RectangleF(e.X-6,e.Y-R-6,2*R+12,2*R+12));plan.Boxes.AddRange(Events.Select(e=>new RectangleF(e.X,e.Y-R,2*R,2*R)));
  var lanes=NetRoute.Corridors(boxes.Values.ToList(),0,Height-Foot);for(float y=Head+th+2;y<Height-Foot-th-2;y+=5)lanes.Add(y);/* labels must stay inside the drawing */
  float collector=Math.Min(Height-8,(Events.Count==0?Height-Foot:Events.Max(e=>e.Y+R))+16);
  var bands=new List<RectangleF>();var texts=new List<RectangleF>();var vlines=new List<RectangleF>();
  Func<float,float,float,float,PointF> edge=(cx,cy,px,py)=>{float vx=px-cx,vy=py-cy,len=(float)Math.Sqrt(vx*vx+vy*vy);return len<0.01f?new PointF(cx+R,cy):new PointF(cx+vx/len*R,cy+vy/len*R);};
  Func<RectangleF,IEnumerable<RectangleF>,int> hits=(r,list)=>list.Count(o=>o.IntersectsWith(r));
  foreach(var ar in Arrows.OrderBy(x=>x.Collector).ThenBy(x=>x.Activity==null).ThenBy(x=>!x.Critical).ThenBy(x=>x.Head.X-x.Tail.X)){
   float tx=ar.Tail.X+R,ty=ar.Tail.Y,hx=ar.Head.X+R,hy=ar.Head.Y;float xa=tx+R+14,xb=hx-R-14;if(xb<xa+10){xa=(tx+hx)/2-5;xb=xa+10;}
   bool timed=ar.Activity==null&&(ar.Lag!=0||ar.Type!="FS");/* a lag arrow carries time but no work: dash-dot; a plain dummy (no time) is dashed */
   int kind=ar.Activity!=null?(ar.Critical?1:0):timed?(ar.Critical?7:6):(ar.Critical?3:2);
   if(ar.Collector)continue;/* drawn below as one collector */
   string name=null,te=null,lag=null;if(ar.Activity!=null){var pa=ar.Activity;name=pa.Node.Name+"  "+pa.Node.Row.Code;te="te "+pa.Te.ToString("0.#",IC)+"  ("+pa.A.ToString("0.0",IC)+" / "+pa.M.ToString("0.#",IC)+" / "+pa.B.ToString("0.0",IC)+")";}
   else if(ar.Lag!=0||ar.Type!="FS")lag=ar.Type+(ar.Lag>0?" +"+ar.Lag:ar.Lag<0?" "+ar.Lag:"");
   float wn=name==null?0:g.MeasureString(name,bold).Width,wt=te==null?0:g.MeasureString(te,font).Width,wl=lag==null?0:g.MeasureString(lag,font).Width;
   /* orthogonal arrow: out of the right side of the tail event, a bend just past its column (xa), one horizontal run, a
      bend just before the head column (xb), into the left side of the head event. The run takes the nearest height where
      the run and both bends touch no event, no label and no other line; the labels go on the longest piece of the run that
      lies on one sheet, where they touch nothing. If no single height is free, the run steps between free heights in the
      gaps before the columns that block it (NetRoute.Sweep). */
   float need=Math.Max(wn,Math.Max(wt,wl));var obsEvents=boxes.Where(k=>k.Key!=ar.Tail&&k.Key!=ar.Head).Select(k=>k.Value).ToList();var allEvents=boxes.Values.ToList();
   Func<float,float,float,List<RectangleF>> labelRects=(y,cx,room)=>{var list=new List<RectangleF>();if(name!=null){float w=Math.Min(wn,room);list.Add(new RectangleF(cx-w/2,y-th-1,w,th));}if(te!=null){float w=Math.Min(wt,room);list.Add(new RectangleF(cx-w/2,y+1,w,th));}if(lag!=null){float w=Math.Min(wl,room);list.Add(new RectangleF(cx-w/2,y-th-1,w,th));}return list;};
   Func<List<RectangleF>,int> labelHits=lr=>lr.Sum(r=>hits(r,allEvents)*10+hits(r,texts)*3+hits(r,bands)*2+hits(r,vlines)*2);
   Func<float,float,float,RectangleF> vr=(x,ya,yb)=>new RectangleF(x-1.5f,Math.Min(ya,yb),3,Math.Abs(yb-ya));
   /* best label place on the horizontal pieces {y, from, to}: returns {hits, centre, room, y} */
   Func<List<float[]>,float[]> place=runs=>{if(need<=0)return new float[]{0,0,0,runs.Count>0?runs[0][0]:ty};float[] best=null;
    var pieces=new List<float[]>();foreach(var run in runs){float p0=run[1]+4;foreach(var c in joins.Where(c=>c>run[1]+4&&c<run[2]-4).OrderBy(c=>c).Concat(new[]{run[2]-4})){pieces.Add(new[]{run[0],p0,c});p0=c;}}
    foreach(var pc in pieces.OrderByDescending(pc=>Math.Min(pc[2]-pc[1],need))){float len=pc[2]-pc[1];if(len<=1)continue;float room=Math.Min(len,need);float lo=pc[1]+room/2,hi=pc[2]-room/2;float m=(lo+hi)/2;
     for(int k=0;k<=60;k++){float off=(k+1)/2*6f*(k%2==0?1:-1);float cx=m+off;if(cx<lo-0.01f||cx>hi+0.01f){if(k>1&&Math.Abs(off)>(hi-lo)/2+6)break;continue;}int h=labelHits(labelRects(pc[0],cx,room))+(room<need-0.5f?6:0);
      if(best==null||h<best[0]){best=new float[]{h,cx,room,pc[0]};if(h==0)return best;}}}
    return best??new float[]{50,(xa+xb)/2,Math.Max(1,xb-xa),runs.Count>0?runs[0][0]:ty};};
   var cands=new List<float>{ty,hy};cands.AddRange(lanes);float pick=float.NaN,pickCost=float.MaxValue;int pickHits=int.MaxValue;float[] pickPlace=null;
   foreach(var y in cands.Distinct().OrderBy(y=>Math.Abs(y-ty)+Math.Abs(y-hy)-(ar.Activity==null&&Math.Abs(y-ty)<0.5f?1:0))){
    var band=new RectangleF(xa,y-2,xb-xa,4);var v1=vr(xa,ty,y);var v2=vr(xb,y,hy);
    int bh=hits(band,obsEvents)*20+hits(band,texts)*4+hits(band,bands)*2+(hits(v1,obsEvents)+hits(v2,obsEvents))*20+(hits(v1,texts)+hits(v2,texts))*4;if(bh>pickHits)continue;var pl=place(new List<float[]>{new[]{y,xa,xb}});
    int h=bh+(int)pl[0];if(h==0){pick=y;pickHits=0;pickPlace=pl;break;}float cost=Math.Abs(y-ty)+Math.Abs(y-hy);if(h<pickHits||(h==pickHits&&cost<pickCost)){pickHits=h;pickCost=cost;pick=y;pickPlace=pl;}}
   List<PointF> route=new List<PointF>{new PointF(tx+R,ty),new PointF(xa,ty),new PointF(xa,pick),new PointF(xb,pick),new PointF(xb,hy),new PointF(hx-R,hy)};
   if(pickHits>=4){/* no free single height: step between free heights (NetRoute.Sweep) and keep it if it touches less */int cost;var hard=obsEvents.Concat(texts.Select(t=>RectangleF.Inflate(t,1,1))).ToList();
    var alt=NetRoute.Sweep(tx+R,ty,xa,xb,hx-R,hy,hard,bands,joins,0,Height-Foot,out cost);var ap=NetRoute.Path(alt.ToArray());var runs=new List<float[]>();int score=0;
    for(int k=0;k+1<ap.Length;k++){var p=ap[k];var q=ap[k+1];if(Math.Abs(p.Y-q.Y)<0.01f){var rr=new RectangleF(Math.Min(p.X,q.X),p.Y-2,Math.Abs(q.X-p.X),4);score+=hits(rr,obsEvents)*20+hits(rr,texts)*4+hits(rr,bands)*2;if(k>0&&k+2<ap.Length&&q.X>p.X)runs.Add(new[]{p.Y,p.X,q.X});}
     else{var rr=vr(p.X,p.Y,q.Y);score+=hits(rr,obsEvents)*20+hits(rr,texts)*4;}}
    if(runs.Count==0)runs.Add(new[]{ty,xa,xb});var pl=place(runs);score+=(int)pl[0]+2;if(score<pickHits){route=alt;pickPlace=pl;pickHits=score;}}
   ar.Score=pickHits;var path=NetRoute.Path(route.ToArray());
   for(int k=0;k+1<path.Length;k++){var p=path[k];var q=path[k+1];if(Math.Abs(p.Y-q.Y)<0.01f)bands.Add(new RectangleF(Math.Min(p.X,q.X),p.Y-2,Math.Abs(q.X-p.X),4));else if(Math.Abs(p.X-q.X)<0.01f)vlines.Add(vr(p.X,p.Y,q.Y));}
   float ly=pickPlace[3];var rects=need>0?labelRects(ly,pickPlace[1],pickPlace[2]):new List<RectangleF>();texts.AddRange(rects);
   plan.Lines.Add(new KeyValuePair<PointF[],int>(path,kind));
   int ri=0;float rm=pickPlace[2];if(name!=null)plan.Labels.Add(new PlanLabel{Rect=rects[ri++],Text=name,Bold=true,Critical=ar.Critical,Truncated=rm<wn-0.5f});if(te!=null)plan.Labels.Add(new PlanLabel{Rect=rects[ri++],Text=te,Muted=true,Truncated=rm<wt-0.5f});if(lag!=null)plan.Labels.Add(new PlanLabel{Rect=rects[ri++],Text=lag,Critical=ar.Critical,Muted=!ar.Critical,Truncated=rm<wl-0.5f});}
  /* collector: from each last event a short dummy to the right, one vertical line per event column down to the collector
     and one collector line to the finish event, drawn once so the dashes stay readable */
  var coll=Arrows.Where(x=>x.Collector).ToList();if(coll.Count>0){var fin=coll[0].Head;float fx=fin.X+R-R-14;var colX=new List<float>();
   foreach(var grp in coll.GroupBy(x=>x.Tail.X)){float cx0=grp.Key+2*R+10;colX.Add(cx0);foreach(var ar in grp)plan.Lines.Add(new KeyValuePair<PointF[],int>(new[]{new PointF(ar.Tail.X+2*R,ar.Tail.Y),new PointF(cx0,ar.Tail.Y)},5));
    plan.Lines.Add(new KeyValuePair<PointF[],int>(new[]{new PointF(cx0,grp.Min(x=>x.Tail.Y)),new PointF(cx0,collector)},5));}
   plan.Lines.Add(new KeyValuePair<PointF[],int>(NetRoute.Path(new[]{new PointF(colX.Min(),collector),new PointF(fx,collector),edge(fin.X+R,fin.Y,fx,collector)}),2));}
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
 internal const string LegendText="PERT-CPM, activity on arrow (working days). a = 0.8 m, b = 1.2 m, so te = (a + 4m + b) / 6 = m and the PERT duration equals the CPM duration; the standard deviation and the 90 % / 95 % completion rest on this assumed +/-20 % spread. Red = critical path. The sheets join at the match lines (trim along a match line and butt the next sheet to it).";
 internal static void DrawKey(Graphics g,float x,float y,Font font,Font bold){using(var ink=new SolidBrush(PowTheme.Ink))using(var red=new SolidBrush(Color.FromArgb(192,40,40)))using(var white=new SolidBrush(Color.White))using(var ring=new Pen(PowTheme.Ink,1f))using(var crit=new Pen(Color.FromArgb(192,40,40),1.6f))using(var act=new Pen(Color.FromArgb(110,120,132),1f){CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)})using(var dummy=new Pen(Color.FromArgb(110,120,132),0.9f){DashStyle=DashStyle.Dash,CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)}){
  g.DrawString("EVENT KEY",bold,ink,x,y);Event(g,new RectangleF(x+76,y,2*R,2*R),"i","TE","TL",false,font,bold,ring,crit,ink,red,white);
  string[] k={"i = event no. (each arrow runs from a lower to a higher no.)","TE = earliest event time, TL = latest event time (working days); TE = TL: critical"};for(int i=0;i<k.Length;i++)g.DrawString(k[i],font,ink,x+76+2*R+12,y+4+i*15);
  float ax=x+560;g.DrawLine(act,ax,y+8,ax+90,y+8);g.DrawString("activity: no. and item no. above, te (a / m / b) below",font,ink,ax+100,y+1);g.DrawLine(dummy,ax,y+24,ax+90,y+24);g.DrawString("dummy: logic only, no time; ends of work join the finish event along the collector line",font,ink,ax+100,y+17);
  using(var lag=new Pen(Color.FromArgb(110,120,132),0.9f){DashStyle=DashStyle.DashDot,CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)}){g.DrawLine(lag,ax,y+40,ax+90,y+40);}g.DrawString("lag arrow: time, no work - SS +3 = 3 days after the start of the tail activity, FF +1 = finish 1 day after its finish, FS +7 = 7 days after its finish",font,ink,ax+100,y+33);}}
}
sealed class PertView:Control {internal PertAoa Diagram;internal PertResult Pert;internal PertView(){DoubleBuffered=true;BackColor=Color.White;Font=new Font("Segoe UI",8f);}
 internal void Fit(){if(Diagram==null)return;var size=Diagram.Layout(Math.Max(500,(Parent==null?700:Parent.ClientSize.Height)-60-PertAoa.KeyH));Width=Math.Max(900,(int)size.Width+20);Height=(int)(size.Height+34+PertAoa.KeyH);Invalidate();}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);if(Diagram==null||Diagram.Events.Count==0){TextRenderer.DrawText(e.Graphics,"No activities yet.",Font,new Point(10,10),PowTheme.Muted);return;}
  using(var bold=new Font(Font,FontStyle.Bold)){TextRenderer.DrawText(e.Graphics,"PERT: Te = "+Pert.Te.ToString("0.0",CultureInfo.InvariantCulture)+" working days, s = "+Pert.Sigma.ToString("0.00",CultureInfo.InvariantCulture)+"; 90%: "+Pert.P90+" days; 95%: "+Pert.P95+" days.   "+PertAoa.LegendText,Font,new Point(8,4),PowTheme.Ink);
   PertAoa.DrawKey(e.Graphics,8,24,Font,bold);Diagram.Draw(e.Graphics,0,28+PertAoa.KeyH,Font,bold);}}}
}
