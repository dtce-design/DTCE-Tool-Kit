using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Precedence Diagram Method (owner, 18 November 2026: "idagdag ang PDM", "use standard layouting", "makatotohanan").
// Activity on node with the realistic construction logic of ScheduleLogic: every relation is drawn as its own arrow and
// labelled at its arrowhead with the predecessor no., the relation type and the lag (e.g. "12 SS +3"). Nodes are placed
// in columns by network level (START, then each activity one column after its latest predecessor, FINISH). Times in the
// textbook convention: ES of the first activity = 0, EF = ES + D, LS = LF - D, TF = LS - ES. Node: ES | D | EF, activity
// no. and item no., description (two lines), LS | TF | LF. Red = critical (TF = 0) and driving critical relations.
// Wide networks are printed on sheets that join edge to edge (owner, 18 November 2026: "yung diagram continuity ang need
// ko, hindi off-page connector ... yung connector lang ng lines arrows"): the drawing is one continuous plan, every sheet
// shows the next slice of it, the joins fall in the gaps between columns, and a line leaving the right border of a sheet
// enters the next sheet at the same height. No text is split by a join and no label overlaps another label or a node.
// Units are hundredths of an inch on paper (pixels on screen).
sealed class PdmNode {internal ScheduleRow Row;internal string Trade;internal int Id,ES,EF,LS,LF,TF,Level,MilestoneNo;internal bool Critical{get{return TF==0;}}internal float X,Y;
 internal bool IsMilestone{get{return MilestoneNo>0;}}internal string Name{get{return IsMilestone?"M"+MilestoneNo:Id.ToString(CultureInfo.InvariantCulture);}}}
sealed class PdmMilestone {internal string Name,Caption;internal int Day;internal float X,Y;}
sealed class PdmNetwork {
 internal readonly List<PdmNode> Nodes=new List<PdmNode>();internal readonly List<PdmMilestone> Milestones=new List<PdmMilestone>();internal List<ScheduleLink> Links=new List<ScheduleLink>();internal int Total;internal float Height;
 internal const float NodeW=190,NodeH=84,RowGap=30,MileD=NodeW,Head=10,Foot=34;internal float LinkGap=130;/* gap between columns; narrowed on paper to save sheets */
 internal readonly List<float> Cuts=new List<float>();internal float Right;/* x positions where a sheet may end (in the gaps between columns), right end of the drawing */
 /* gaps (0 = after START) that hold a sheet join: they are made wider so that only horizontal lines cross the join, clear
    of the bends next to the columns and of the relation labels at the arrowheads */
 internal HashSet<int> Widen=new HashSet<int>();internal const float JoinRoom=50,JoinAt=50;
 internal readonly List<float[]> Groups=new List<float[]>();/* x extent of every column group (START, each level, FINISH) */
 internal static PdmNetwork Build(List<ScheduleRow> rows){var n=new PdmNetwork();List<PdmNode> nodes;n.Links=ScheduleLogic.Network(rows,out nodes,true);n.Nodes.AddRange(nodes);n.Total=nodes.Count==0?0:nodes.Max(x=>x.EF);
  foreach(var nd in nodes){int lv=1;foreach(var l in n.Links.Where(x=>x.To==nd))lv=Math.Max(lv,l.From.Level+1);nd.Level=lv;}
  n.Milestones.Add(new PdmMilestone{Name="START",Caption="Start of project",Day=0});n.Milestones.Add(new PdmMilestone{Name="FINISH",Caption="Project completion",Day=n.Total});return n;}
 internal bool Driving(ScheduleLink l){var p=l.From;int d=l.To.Row.Days;int b=l.Type=="FS"?p.EF+l.Lag:l.Type=="SS"?p.ES+l.Lag:p.EF+l.Lag-d;return p.Critical&&l.To.Critical&&b==l.To.ES;}
 /* heights of the nodes (and the sub-column of a crowded level) for one drawing height; the x positions follow the gaps */
 float placedFor=float.NaN;readonly Dictionary<PdmNode,int> subColumn=new Dictionary<PdmNode,int>();readonly List<List<PdmNode>> levelLists=new List<List<PdmNode>>();
 internal SizeF Layout(float height){Height=height;layoutStamp++;float step=NodeH+RowGap;float usable=height-Foot;int perCol=Math.Max(1,(int)((usable-Head-10)/step));
  if(placedFor!=height){placedFor=height;Place(usable,perCol);}return PlaceX();}
 SizeF PlaceX(){float x=6;Cuts.Clear();int gap=0;Func<float> gw=()=>LinkGap+(Widen.Contains(gap)?JoinRoom:0);
  Groups.Clear();Milestones[0].X=x;Groups.Add(new[]{x,x+NodeW});x+=NodeW;Cuts.Add(x+JoinAt);x+=gw();gap++;
  foreach(var list in levelLists){int cols=list.Max(nd=>subColumn[nd])+1;float gx0=x;foreach(var nd in list)nd.X=x+subColumn[nd]*(NodeW+LinkGap);x+=cols*(NodeW+LinkGap)-LinkGap;Groups.Add(new[]{gx0,x});Cuts.Add(x+JoinAt);x+=gw();gap++;}
  Milestones[1].X=x;Groups.Add(new[]{x,x+NodeW});Right=x+NodeW+6;return new SizeF(Right,Height);}
 /* nodes in columns by network level (START, the levels, FINISH); a level with more nodes than fit one above the other is
    split into sub-columns, the nodes that only lead to FINISH in the last one; the order and the heights come from the
    layered placement (NetLayout), with every relation ending at the height of its arrowhead on the successor. A few
    weightings of the placement are tried and the one whose routed drawing has the fewest crossings and bends is kept. */
 void Place(float usable,int perCol){float step=NodeH+RowGap;int levels=Nodes.Count==0?0:Nodes.Max(nd=>nd.Level);levelLists.Clear();subColumn.Clear();
  var ins=Nodes.ToDictionary(nd=>nd,nd=>Links.Where(l=>l.To==nd).ToList());var outs=Nodes.ToDictionary(nd=>nd,nd=>Links.Where(l=>l.From==nd).ToList());
  Func<PdmNode,bool> toFinish=nd=>!outs[nd].Any(l=>l.Type!="SS");Func<PdmNode,bool> collector=nd=>nd.Level<levels-1&&!(nd.Critical&&nd.EF==Total);
  int n=Nodes.Count;var index=new Dictionary<object,int>();for(int i=0;i<n;i++)index[Nodes[i]]=i;index[Milestones[0]]=n;index[Milestones[1]]=n+1;
  var layerOf=new int[n+2];var order=new List<List<int>>{new List<int>{n}};
  for(int lv=1;lv<=levels;lv++){var list=Nodes.Where(nd=>nd.Level==lv).OrderBy(nd=>nd.Id).ToList();if(list.Count==0)continue;levelLists.Add(list);int cols=(int)Math.Ceiling(list.Count/(double)perCol);
   var sorted=list.OrderBy(nd=>toFinish(nd)?1:0).ToList();int per=(int)Math.Ceiling(sorted.Count/(double)cols);
   for(int c=0;c<cols;c++){var part=sorted.Skip(c*per).Take(per).ToList();foreach(var nd in part){subColumn[nd]=c;layerOf[index[nd]]=order.Count;}order.Add(part.Select(nd=>index[nd]).ToList());}}
  layerOf[n+1]=order.Count;order.Add(new List<int>{n+1});
  var net=new NetLayout(n+2,layerOf,order);if(Environment.GetEnvironmentVariable("PDMSPINE")!=null)net.SpineBand=new[]{Head+6+NodeH/2,usable-20-NodeH/2,step,step/2,14f};var heads=new List<int>();var kinds=new List<string>();var tails=new List<int>();
  Action<object,object,string,bool> edge=(a,b,kind,c)=>{net.Edge(index[a],index[b],1,c,0);tails.Add(index[a]);heads.Add(index[b]);kinds.Add(kind);};
  foreach(var nd in Nodes.Where(x=>ins[x].Count==0))edge(Milestones[0],nd,"",nd.Critical&&nd.ES==0);
  foreach(var l in Links)edge(l.From,l.To,l.Type=="FS"&&l.Lag==0?"":l.Label,Driving(l));
  foreach(var nd in Nodes.Where(x=>toFinish(x)&&!collector(x)))edge(nd,Milestones[1],"",nd.Critical&&nd.EF==Total);
  net.Order(24);
  /* arrowheads on a node: one per relation type and lag, spread over its height in the order of their tails (as drawn) */
  var offset=new float[heads.Count];float lo=Head+6+NodeH/2,hi=usable-20-NodeH/2;
  Action<float[]> ports=y=>{foreach(var grp in Enumerable.Range(0,heads.Count).GroupBy(k=>heads[k])){var ks=grp.GroupBy(k=>kinds[k]).OrderBy(g=>g.Min(k=>y==null?net.Layers[layerOf[tails[k]]].IndexOf(tails[k]):y[tails[k]])).ToList();
    for(int i=0;i<ks.Count;i++)foreach(var k in ks[i])offset[k]=NodeH*(i+1)/(ks.Count+1)-NodeH/2;}};
  Action<float[]> apply=y=>{for(int i=0;i<n;i++)Nodes[i].Y=y[i]-NodeH/2;Milestones[0].Y=y[n]-NodeH/2;Milestones[1].Y=y[n+1]-NodeH/2;};
  /* one weighting: trying several and routing each costs too much time for a window that must open at once */
  var t=new NetLayout.Tune{Mu=0.4f,Crit=2,Pass=14};{var ov=Environment.GetEnvironmentVariable("PDMTUNE");if(ov!=null){var q=ov.Split(',').Select(z=>float.Parse(z,CultureInfo.InvariantCulture)).ToArray();t.Mu=q[0];t.Crit=q[1];t.CritRise=q[2];if(q.Length>3)t.Lam=q[3];if(q.Length>4)t.Near=q[4];if(q.Length>5)t.Pass=q[5];}}ports(null);for(int pass=0;pass<(Environment.GetEnvironmentVariable("PORTP")==null?3:int.Parse(Environment.GetEnvironmentVariable("PORTP")));pass++){net.Assign(lo,hi,step,step/2,step,k=>offset[k],t);ports(net.Y);}
  apply(net.Y);}
 static void Box(Graphics g,RectangleF r,string[] top,string title,string text,string[] bottom,bool crit,Brush titleInk,Font font,Font bold,Brush ink,Brush muted,Brush fill,Brush white,Pen grid,Pen edge){
  var mid=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap};var left=new StringFormat(mid){Alignment=StringAlignment.Near};var wrap=new StringFormat{LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisWord};
  float rh=r.Height/5,cw=r.Width/3;g.FillRectangle(white,r);g.FillRectangle(fill,r.X,r.Y,r.Width,rh);g.FillRectangle(fill,r.X,r.Bottom-rh,r.Width,rh);
  g.DrawLine(grid,r.X,r.Y+rh,r.Right,r.Y+rh);g.DrawLine(grid,r.X,r.Bottom-rh,r.Right,r.Bottom-rh);for(int c=1;c<3;c++){g.DrawLine(grid,r.X+c*cw,r.Y,r.X+c*cw,r.Y+rh);g.DrawLine(grid,r.X+c*cw,r.Bottom-rh,r.X+c*cw,r.Bottom);}
  for(int c=0;c<3;c++){g.DrawString(top[c],font,ink,new RectangleF(r.X+c*cw,r.Y,cw,rh),mid);g.DrawString(bottom[c],font,ink,new RectangleF(r.X+c*cw,r.Bottom-rh,cw,rh),mid);}
  g.DrawString(title,bold,titleInk,new RectangleF(r.X+4,r.Y+rh,r.Width-8,rh),left);g.DrawString(text,font,muted,new RectangleF(r.X+4,r.Y+2*rh,r.Width-8,2*rh),wrap);g.DrawRectangle(edge,r.X,r.Y,r.Width,r.Height);}
 /* drawing plan in layout coordinates: polylines and labels, made once for the whole network so that every sheet shows
    the same continuous drawing */
 internal sealed class Plan {internal readonly List<KeyValuePair<PointF[],bool>> Lines=new List<KeyValuePair<PointF[],bool>>();internal readonly List<PertAoa.PlanLabel> Labels=new List<PertAoa.PlanLabel>();
  internal readonly List<RectangleF> Boxes=new List<RectangleF>();}
 sealed class Edge {internal object From,To;internal RectangleF FromBox,ToBox;internal string Label;internal bool Critical,Collector;internal int Out,OutN,In,InN;}
 /* the plan is made once per layout (Layout sets a new stamp), node positions, sheet joins and text size, and is then
    reused by every sheet, every repaint and the drawing checks */
 int layoutStamp;readonly List<KeyValuePair<string,Plan>> plans=new List<KeyValuePair<string,Plan>>();
 string PlanKey(Graphics g,Font font,IList<float> joins){var IC=CultureInfo.InvariantCulture;var sb=new System.Text.StringBuilder();long h=17;
  foreach(var nd in Nodes){h=h*31+nd.X.GetHashCode();h=h*31+nd.Y.GetHashCode();h=h*31+nd.Id;}foreach(var m in Milestones){h=h*31+m.X.GetHashCode();h=h*31+m.Y.GetHashCode();}foreach(var l in Links){h=h*31+l.From.Id;h=h*31+l.To.Id;h=h*31+l.Lag;}
  sb.Append(layoutStamp).Append('|').Append(h).Append('|').Append(Nodes.Count).Append('|').Append(Links.Count).Append('|').Append(Height.ToString("R",IC)).Append('|').Append(Right.ToString("R",IC)).Append('|');
  foreach(var j in joins??new float[0])sb.Append(j.ToString("R",IC)).Append(',');var m0=g.Transform.Elements;
  sb.Append('|').Append(font.Name).Append(font.SizeInPoints.ToString("R",IC)).Append(font.Style).Append('|').Append(g.PageUnit).Append(g.PageScale.ToString("R",IC)).Append(g.DpiX.ToString("R",IC))
   .Append('|').Append(m0[0].ToString("R",IC)).Append(',').Append(m0[1].ToString("R",IC)).Append(',').Append(m0[2].ToString("R",IC)).Append(',').Append(m0[3].ToString("R",IC))
   .Append('|').Append(g.MeasureString("0",font).Height.ToString("R",IC)).Append('|').Append(g.MeasureString("123 FF +10",font).Width.ToString("R",IC));
  return sb.ToString();}
 internal Plan MakePlan(Graphics g,Font font,IList<float> joins=null){var key=PlanKey(g,font,joins);foreach(var kv in plans)if(kv.Key==key)return kv.Value;
  var plan=Route(g,font,joins);plans.Insert(0,new KeyValuePair<string,Plan>(key,plan));if(plans.Count>2)plans.RemoveAt(2);return plan;}
 Plan Route(Graphics g,Font font,IList<float> joins){var plan=new Plan();var IC=CultureInfo.InvariantCulture;var start=Milestones[0];var finish=Milestones[1];
  Func<object,RectangleF> box=o=>{var nd=o as PdmNode;if(nd!=null)return new RectangleF(nd.X,nd.Y,NodeW,NodeH);var m=(PdmMilestone)o;return new RectangleF(m.X,m.Y,NodeW,NodeH);};
  var edges=new List<Edge>();var ins=Nodes.ToDictionary(nd=>nd,nd=>Links.Where(l=>l.To==nd).ToList());var outs=Nodes.ToDictionary(nd=>nd,nd=>Links.Where(l=>l.From==nd).ToList());
  foreach(var nd in Nodes.Where(x=>ins[x].Count==0))edges.Add(new Edge{From=start,To=nd,Critical=nd.Critical&&nd.ES==0});
  /* FS without lag is not labelled (the usual convention); other relations show their type and lag at the arrowhead */
  foreach(var l in Links)edges.Add(new Edge{From=l.From,To=l.To,Label=l.Type=="FS"&&l.Lag==0?null:l.Label,Critical=Driving(l)});
  /* every activity whose finish has no successor (no relation, or only SS relations from its start) is linked to FINISH:
     straight across when it is in one of the last two columns (owner, 18 November 2026: "very obvious naman na ito"),
     otherwise along one collector line below the network, so no long lines cross the diagram */
  int lastLevel=Nodes.Count==0?0:Nodes.Max(x=>x.Level);
  foreach(var nd in Nodes.Where(x=>!outs[x].Any(l=>l.Type!="SS")))edges.Add(new Edge{From=nd,To=finish,Critical=nd.Critical&&nd.EF==Total,Collector=nd.Level<lastLevel-1&&!(nd.Critical&&nd.EF==Total)});
  foreach(var e in edges){e.FromBox=box(e.From);e.ToBox=box(e.To);}
  foreach(var grp in edges.GroupBy(e=>e.From)){var list=grp.OrderBy(e=>e.Collector).ThenBy(e=>e.ToBox.Y).ToList();for(int i=0;i<list.Count;i++){list[i].Out=i;list[i].OutN=list.Count;}}
  /* relations of the same type and lag share one arrowhead on the successor: their lines join on its entry line */
  foreach(var grp in edges.Where(e=>!e.Collector).GroupBy(e=>e.To)){var kinds=grp.GroupBy(e=>e.Label??"").OrderBy(k=>k.Min(e=>e.FromBox.Y)).ToList();for(int i=0;i<kinds.Count;i++)foreach(var e in kinds[i]){e.In=i;e.InN=kinds.Count;}}
  var nodeBoxes=Nodes.Select(nd=>new KeyValuePair<object,RectangleF>(nd,box(nd))).Concat(new[]{start,finish}.Select(m=>new KeyValuePair<object,RectangleF>(m,box(m)))).ToList();
  foreach(var b in nodeBoxes)plan.Boxes.Add(b.Value);
  /* labels: just above the last horizontal piece of each relation, right-aligned at the arrowhead */
  float th=g.MeasureString("0",font).Height;var labelZone=new Dictionary<object,float>();
  /* the label zone is the same for every node of a column, so that entry lines to one node never cross the labels of the
     nodes above or below it */
  var zoneByColumn=edges.Where(e=>!e.Collector).GroupBy(e=>Math.Round(e.ToBox.X)).ToDictionary(gc=>gc.Key,gc=>gc.Select(e=>e.Label==null?0:g.MeasureString(e.Label,font).Width).DefaultIfEmpty(0).Max()+12);
  foreach(var e in edges.Where(x=>!x.Collector))labelZone[e.To]=zoneByColumn[Math.Round(e.ToBox.X)];
  Func<Edge,float> entryY=e=>e.ToBox.Y+NodeH*(e.In+1)/(e.InN+1);
  foreach(var grp in edges.Where(x=>x.Label!=null).GroupBy(x=>new KeyValuePair<object,string>(x.To,x.Label))){var e=grp.First();float w=g.MeasureString(e.Label,font).Width;var r=new RectangleF(e.ToBox.X-w-4,entryY(e)-th,w,th-1);plan.Labels.Add(new PertAoa.PlanLabel{Rect=r,Text=e.Label,Critical=grp.Any(x=>x.Critical)});}
  var obstacles=nodeBoxes.Select(b=>new KeyValuePair<object,RectangleF>(b.Key,RectangleF.Inflate(b.Value,6,6))).ToList();var used=new List<RectangleF>(plan.Labels.Select(k=>RectangleF.Inflate(k.Rect,1,1)));
  var lanes=NetRoute.Corridors(obstacles.Select(k=>k.Value).ToList(),0,Height-Foot);for(float y=Head;y<Height-Foot;y+=8)lanes.Add(y);
  float collector=Math.Min(Height-10,(Nodes.Count==0?Height-Foot:Nodes.Max(nd=>nd.Y+NodeH))+18);
  foreach(var e in edges.OrderBy(x=>x.Collector).ThenBy(x=>!x.Critical)){float py=e.FromBox.Y+NodeH/2,xa=e.FromBox.Right+10+Math.Min(e.Out,6)*5;
   if(e.Collector){float fx=finish.X-24,fy=finish.Y+NodeH/2;plan.Lines.Add(new KeyValuePair<PointF[],bool>(NetRoute.Path(new[]{new PointF(e.FromBox.Right,py),new PointF(xa,py),new PointF(xa,collector),new PointF(fx,collector),new PointF(fx,fy),new PointF(finish.X,fy)}),e.Critical));continue;}
   float sy=entryY(e),xb=e.ToBox.X-labelZone[e.To]-Math.Min(e.In,6)*6;if(xb<xa+6)xb=xa+6;
   var hard=obstacles.Where(k=>k.Key!=e.From&&k.Key!=e.To).Select(k=>k.Value).Concat(plan.Labels.Select(k=>RectangleF.Inflate(k.Rect,1,1))).ToList();int cost;
   var pts=NetRoute.Sweep(e.FromBox.Right,py,xa,xb,e.ToBox.X,sy,hard,used,joins,0,collector-2,out cost);/* nothing below the collector */
   for(int k=1;k+2<pts.Count;k++)if(Math.Abs(pts[k].Y-pts[k+1].Y)<0.01f)used.Add(new RectangleF(Math.Min(pts[k].X,pts[k+1].X),pts[k].Y-3,Math.Abs(pts[k+1].X-pts[k].X),6));
   plan.Lines.Add(new KeyValuePair<PointF[],bool>(NetRoute.Path(pts.ToArray()),e.Critical));}
  return plan;}
 /* pert == null: PDM values; otherwise PERT values */
 internal void Draw(Graphics g,float dx,float dy,Font font,Font bold,PertResult pert=null,IList<float> joins=null){var smooth=g.SmoothingMode;g.SmoothingMode=SmoothingMode.AntiAlias;var IC=CultureInfo.InvariantCulture;var state=g.Save();g.TranslateTransform(dx,dy);
  Color redC=Color.FromArgb(192,40,40),line=Color.FromArgb(120,130,142);var plan=MakePlan(g,font,joins);
  using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted))using(var red=new SolidBrush(redC))using(var labelInk=new SolidBrush(Color.FromArgb(80,90,104)))
  using(var arrow=new Pen(line,0.9f){CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)})using(var critArrow=new Pen(redC,1.5f){CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)})using(var edge=new Pen(Color.FromArgb(70,78,88),0.9f))using(var critEdge=new Pen(redC,1.8f))
  using(var cellCrit=new SolidBrush(Color.FromArgb(251,233,231)))using(var cell=new SolidBrush(Color.FromArgb(234,238,242)))using(var white=new SolidBrush(Color.White))using(var grid=new Pen(Color.FromArgb(180,188,198),0.6f)){
   foreach(var l in plan.Lines.Where(k=>!k.Value))g.DrawLines(arrow,l.Key);foreach(var l in plan.Lines.Where(k=>k.Value))g.DrawLines(critArrow,l.Key);
   foreach(var l in plan.Labels){g.FillRectangle(white,l.Rect);g.DrawString(l.Text,font,l.Critical?(Brush)red:labelInk,l.Rect.X,l.Rect.Y);}
   foreach(var m in Milestones){string d=m.Day.ToString(IC);Box(g,new RectangleF(m.X,m.Y,NodeW,NodeH),new[]{d,"0",d},m.Name+"  (milestone)",m.Caption,new[]{d,"0",d},true,red,font,bold,ink,muted,cellCrit,white,grid,critEdge);}
   foreach(var nd in Nodes.Where(x=>x.IsMilestone)){var IC2=IC;Box(g,new RectangleF(nd.X,nd.Y,NodeW,NodeH),new[]{nd.ES.ToString(IC2),"0",nd.EF.ToString(IC2)},nd.Name+"  (milestone)",nd.Row.Description,new[]{nd.LS.ToString(IC2),nd.TF.ToString(IC2),nd.LF.ToString(IC2)},nd.Critical,nd.Critical?(Brush)red:ink,font,bold,ink,muted,nd.Critical?cellCrit:cell,white,grid,nd.Critical?critEdge:edge);}
   foreach(var nd in Nodes.Where(x=>!x.IsMilestone)){string[] top,bottom;if(pert==null){top=new[]{nd.ES.ToString(IC),nd.Row.Days.ToString(IC),nd.EF.ToString(IC)};bottom=new[]{nd.LS.ToString(IC),nd.TF.ToString(IC),nd.LF.ToString(IC)};}
    else{var a=pert.Activities.First(x=>x.Node==nd);top=new[]{a.A.ToString("0.0",IC),a.M.ToString("0.#",IC),a.B.ToString("0.0",IC)};bottom=new[]{nd.ES.ToString(IC),a.Te.ToString("0.#",IC),nd.EF.ToString(IC)};}
    Box(g,new RectangleF(nd.X,nd.Y,NodeW,NodeH),top,nd.Name+"   "+nd.Row.Code,nd.Row.Description,bottom,nd.Critical,nd.Critical?(Brush)red:ink,font,bold,ink,muted,nd.Critical?cellCrit:cell,white,grid,nd.Critical?critEdge:edge);}}
  g.Restore(state);g.SmoothingMode=smooth;}
 internal const string LegendText="Precedence diagram (activity on node), working days. Red = critical path (TF = 0). Shaded column on a continuation sheet = the last column of the sheet before, repeated; the lines carry on from it.";
 internal const string PertLegendText="PERT-CPM on the same network: a = 0.8 m (optimistic), m = most likely, b = 1.2 m (pessimistic); te = (a + 4m + b) / 6; ES / EF with te. Labels at the arrowheads = predecessor no., relation and lag. Red = critical path.";
 internal const float KeyH=NodeH+14;
 /* standard node key, drawn once at the top of each sheet */
 internal static void DrawKey(Graphics g,float x,float y,Font font,Font bold,bool pert=false){
  using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted))using(var cell=new SolidBrush(Color.FromArgb(234,238,242)))using(var white=new SolidBrush(Color.White))using(var grid=new Pen(Color.FromArgb(180,188,198),0.6f))using(var edge=new Pen(Color.FromArgb(70,78,88),0.9f)){
   g.DrawString("NODE KEY",bold,ink,x,y);var r=new RectangleF(x+70,y,NodeW,NodeH);
   Box(g,r,pert?new[]{"a","m","b"}:new[]{"ES","D","EF"},"Activity no.   Item no.","Description (up to two lines)",pert?new[]{"ES","te","EF"}:new[]{"LS","TF","LF"},false,ink,bold,bold,ink,muted,cell,white,grid,edge);
   string[] k=pert?new[]{"a / m / b = optimistic / most likely / pessimistic duration","te = expected duration = (a + 4m + b) / 6","ES / EF = early start / finish with te (working days)","\"SS +3\" = starts 3 days after the predecessor starts"}
    :new[]{"ES = early start, EF = early finish; LS = late start, LF = late finish; D = duration; TF = total float = LS - ES","ES of the first activity = 0; EF = ES + D; LS = LF - D","FS = finish-to-start, SS = start-to-start (overlap), FF = finish-to-finish; the label at the arrowhead is the type and lag","(\"SS +3\" = starts 3 days after the predecessor starts); FS without lag is not labelled","M1, M2 ... = milestones (D = 0) where the items of one trade meet the next trade"};
   for(int i=0;i<k.Length;i++)g.DrawString(k[i],font,ink,r.Right+14,y+2+i*15);}}
}
sealed class PdmView:Control {internal PdmNetwork Network;internal PertResult Pert;internal PdmView(){DoubleBuffered=true;BackColor=Color.White;Font=new Font("Segoe UI",8f);}
 internal void Fit(){if(Network==null)return;var size=Network.Layout(Math.Max(500,(Parent==null?700:Parent.ClientSize.Height)-50-PdmNetwork.KeyH));Width=Math.Max(900,(int)size.Width+20);Height=(int)(size.Height+30+PdmNetwork.KeyH);Invalidate();}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);if(Network==null||Network.Nodes.Count==0){TextRenderer.DrawText(e.Graphics,"No activities yet.",Font,new Point(10,10),PowTheme.Muted);return;}
  using(var bold=new Font(Font,FontStyle.Bold)){string legend=Pert==null?PdmNetwork.LegendText:"PERT: Te = "+Pert.Te.ToString("0.0",CultureInfo.InvariantCulture)+" working days, s = "+Pert.Sigma.ToString("0.00",CultureInfo.InvariantCulture)+"; 90%: "+Pert.P90+" days; 95%: "+Pert.P95+" days.   "+PdmNetwork.PertLegendText;
   TextRenderer.DrawText(e.Graphics,legend,Font,new Point(8,4),PowTheme.Muted);PdmNetwork.DrawKey(e.Graphics,8,24,Font,bold,Pert!=null);Network.Draw(e.Graphics,0,24+PdmNetwork.KeyH,Font,bold,Pert);}}}
}
namespace BeamScheduleFormatter {
/* orthogonal routing helpers shared by the PDM and the PERT network: a horizontal run is placed in the free lane nearest to
   its two ends, so lines go between the boxes / events of the columns they cross instead of behind them */
static class NetRoute {
 internal static List<float> Corridors(List<RectangleF> boxes,float top,float bottom){var ys=new List<float>{top+4,bottom-4};
  foreach(var col in boxes.GroupBy(b=>Math.Round(b.X))){var list=col.OrderBy(b=>b.Y).ToList();for(int i=0;i+1<list.Count;i++)if(list[i+1].Y>list[i].Bottom)ys.Add((list[i].Bottom+list[i+1].Y)/2);if(list.Count>0){ys.Add(Math.Max(top+4,list[0].Y-6));ys.Add(Math.Min(bottom-4,list[list.Count-1].Bottom+6));}}
  return ys.Distinct().ToList();}
 static bool Free(float y,float x0,float x1,IEnumerable<RectangleF> obstacles){foreach(var b in obstacles)if(b.Right>x0&&b.X<x1&&y>b.Y&&y<b.Bottom)return false;return true;}
 internal static float Lane(float y0,float y1,float x0,float x1,IEnumerable<RectangleF> obstacles,List<float> corridors,bool startSide=false){var obs=obstacles.Where(b=>b.Right>x0&&b.X<x1).ToList();
  if(startSide&&Free(y0,x0,x1,obs))return y0;if(Free(y1,x0,x1,obs))return y1;if(Free(y0,x0,x1,obs))return y0;float best=y1,cost=float.MaxValue;foreach(var y in corridors){if(!Free(y,x0,x1,obs))continue;float c=Math.Abs(y-y0)+Math.Abs(y-y1);if(c<cost){cost=c;best=y;}}return best;}
 /* orthogonal route of one relation: out of the tail at (x0, y0), first bend at xa, last bend at xb, into the head at
    (x1, y1). The horizontal run keeps one height while it is free of every obstacle (node / event boxes and labels); where a
    column blocks it, it steps to the nearest free height in the gap just before that column. Bends never fall within 10 of a
    sheet join (joins), so lines cross the joins horizontally. soft = lines already drawn (avoided when possible). */
 internal static List<PointF> Sweep(float x0,float y0,float xa,float xb,float x1,float y1,List<RectangleF> hard,List<RectangleF> soft,IList<float> joins,float top,float bottom,out int cost,IList<float> extra=null){
  cost=0;joins=joins??new float[0];var obs=hard.Where(b=>b.Right>xa&&b.X<xb).ToList();var sobs=soft.Where(b=>b.Right>xa&&b.X<xb).ToList();
  Func<float,float,float,bool> free=(y,from,to)=>{foreach(var b in obs)if(b.Right>from&&b.X<to&&y>b.Y-2.5f&&y<b.Bottom+2.5f)return false;return true;};/* a line keeps 2.5 clear of every box */
  Func<float,float,float,int> softHits=(y,from,to)=>sobs.Count(b=>b.Right>from&&b.X<to&&y>b.Y-0.01f&&y<b.Bottom+0.01f);
  Func<float,float,float,float,bool> vfree=(x,ya,yb,dummy)=>{float lo=Math.Min(ya,yb),hi=Math.Max(ya,yb);foreach(var b in hard)if(x>b.X-2.5f&&x<b.Right+2.5f&&hi>b.Y&&lo<b.Bottom)return false;return true;};
  Func<float,bool> nearJoin=x=>joins.Any(j=>Math.Abs(j-x)<10);
  var pts=new List<PointF>{new PointF(x0,y0),new PointF(xa,y0)};
  /* straight at the head height or at the tail height when the whole run is free */
  foreach(var y in new[]{y1,y0})if(free(y,xa,xb)&&vfree(xa,y0,y,0)&&vfree(xb,y,y1,0)){cost=softHits(y,xa,xb);if(cost==0||y==y0){pts.Add(new PointF(xa,y));pts.Add(new PointF(xb,y));pts.Add(new PointF(xb,y1));pts.Add(new PointF(x1,y1));return pts;}}
  /* candidate heights: the gaps between the boxes of every column on the way, plus the two end heights */
  var cols=obs.GroupBy(b=>Math.Round(b.X/4)).Select(g=>g.ToList()).OrderBy(g=>g.Min(b=>b.X)).ToList();
  var ys=new List<float>{y0,y1,top+4,bottom-4};foreach(var c in cols){var l=c.OrderBy(b=>b.Y).ToList();ys.Add(Math.Max(top+4,l[0].Y-3));ys.Add(Math.Min(bottom-4,l[l.Count-1].Bottom+3));for(int i=0;i+1<l.Count;i++)if(l[i+1].Y>l[i].Bottom+1)ys.Add((l[i].Bottom+l[i+1].Y)/2);}
  if(extra!=null)ys.AddRange(extra.Where(y=>y>top&&y<bottom));ys=ys.Distinct().ToList();
  /* first height: the one that runs free the farthest, then nearest to the head */
  Func<float,float,float> reach=(y,from)=>{foreach(var b in obs.Where(b=>b.Right>from&&y>b.Y-2.5f&&y<b.Bottom+2.5f).OrderBy(b=>b.X)){return Math.Max(from,b.X);}return xb;};
  float cur=ys.Where(y=>vfree(xa,y0,y,0)).OrderByDescending(y=>reach(y,xa)).ThenBy(y=>softHits(y,xa,reach(y,xa))).ThenBy(y=>Math.Abs(y-y1)+Math.Abs(y-y0)*0.5f).DefaultIfEmpty(y0).First();
  pts.Add(new PointF(xa,cur));float at=xa;int guard=0;
  while(guard++<200){float stop=reach(cur,at);if(stop>=xb-0.01f)break;
   /* blocked by a column starting at stop: step in the gap before it */
   float gx=stop-8;float prev=pts[pts.Count-1].X;if(gx<prev+4)gx=(prev+stop)/2;int tries=0;while(nearJoin(gx)&&tries++<10)gx-=6;
   var next=ys.Where(y=>free(y,gx,Math.Min(xb,stop+1))&&vfree(gx,cur,y,0)).OrderByDescending(y=>reach(y,gx)).ThenBy(y=>softHits(y,gx,reach(y,gx))).ThenBy(y=>Math.Abs(y-y1)+Math.Abs(y-cur)*0.5f).ToList();
   if(next.Count==0){cost+=100;break;}float ny=next[0];pts.Add(new PointF(gx,cur));pts.Add(new PointF(gx,ny));cur=ny;at=gx;cost+=3;}
  cost+=softHits(cur,at,xb);
  if(!vfree(xb,cur,y1,0))cost+=50;
  pts.Add(new PointF(xb,cur));pts.Add(new PointF(xb,y1));pts.Add(new PointF(x1,y1));return pts;}
 /* cheapest orthogonal route by stages (dynamic programming): the run keeps one height ys[lane] in each stage s (bx[s] to
    bx[s+1]) and changes height only at the stage boundaries. hc[s, i] = cost of the run of stage s at ys[i]; lab[s, i] = cost
    of putting the labels on that run (null: no labels, otherwise they go on exactly one stage); vertical(x, role) = cost of a
    vertical piece at x from ya to yb (role 0 = out of the tail at bx[0], 1 = a step, 2 = into the head at the last
    boundary), at least its length plus two bends; start(i) = extra cost of the first stage at ys[i]. Returns the lane of
    every stage; total = the cost of the route. */
 internal static int[] Staged(float[] ys,IList<float> bx,float y0,float y1,float[,] hc,Func<int,int,float> lab,Func<float,int,Func<float,float,float>> vertical,float bend,Func<int,float> start,out float total){
  int S=bx.Count-1,L=ys.Length;bool labels=lab!=null;const float INF=1e30f;var c0=new float[S,L];var c1=new float[S,L];var b0=new int[S,L];var b1=new int[S,L];/* c0 / c1: cheapest route to stage s at lane j without / with the labels; back pointer = lane * 2 + flag of the stage before */
  var vs0=vertical(bx[0],0);var ve=vertical(bx[S],2);
  if(S==1){/* one stage: the lanes in the order of their cost without the labels, the labels checked only while they can still win */
   var bse=new float[L];for(int i=0;i<L;i++)bse[i]=vs0(y0,ys[i])+start(i)+hc[0,i]+ve(ys[i],y1);total=INF;int bi=0;
   foreach(var i in Enumerable.Range(0,L).OrderBy(i=>bse[i])){if(bse[i]>=total)break;float t=bse[i]+(labels?lab(0,i):0);if(t<total){total=t;bi=i;}}
   return new[]{bi};}
  for(int i=0;i<L;i++){float v=vs0(y0,ys[i])+start(i);c0[0,i]=v+hc[0,i];c1[0,i]=labels?v+hc[0,i]+lab(0,i):INF;b0[0,i]=b1[0,i]=-1;}
  for(int s=1;s<S;s++){var vs=vertical(bx[s],1);for(int j=0;j<L;j++){c0[s,j]=INF;c1[s,j]=INF;}
   for(int f=0;f<(labels?2:1);f++){var prev=f==0?c0:c1;int st=s;var order=Enumerable.Range(0,L).Where(i=>prev[st-1,i]<INF/2).OrderBy(i=>prev[st-1,i]).ToList();
    for(int j=0;j<L;j++){float best=prev[s-1,j];int bi=j;foreach(var i in order){float pc=prev[s-1,i];if(pc+2*bend>=best)break;if(i==j||pc+Math.Abs(ys[i]-ys[j])+2*bend>=best)continue;float v=pc+vs(ys[i],ys[j]);if(v<best){best=v;bi=i;}}
     if(best>=INF/2)continue;
     if(f==0){c0[s,j]=best+hc[s,j];b0[s,j]=bi*2;if(labels&&best+hc[s,j]<c1[s,j]){float w=best+hc[s,j]+lab(s,j);if(w<c1[s,j]){c1[s,j]=w;b1[s,j]=bi*2;}}}
     else{float w=best+hc[s,j];if(w<c1[s,j]){c1[s,j]=w;b1[s,j]=bi*2+1;}}}}}
  total=INF;int pj=0;{var fin=labels?c1:c0;for(int j=0;j<L;j++){if(fin[S-1,j]>=INF/2)continue;float v=fin[S-1,j]+ve(ys[j],y1);if(v<total){total=v;pj=j;}}}
  var lane=new int[S];{int j=pj,f=labels?1:0;for(int s=S-1;s>=0;s--){lane[s]=j;int bp=f==0?b0[s,j]:b1[s,j];if(bp<0)break;j=bp/2;f=bp%2;}}
  return lane;}
 internal static void AddOpen(double[] diff,float[] ys,float a,float b,float c){if(b<=a)return;int lo=0,hi=ys.Length;while(lo<hi){int m=(lo+hi)/2;if(ys[m]<=a)lo=m+1;else hi=m;}int i0=lo;hi=ys.Length;while(lo<hi){int m=(lo+hi)/2;if(ys[m]<b)lo=m+1;else hi=m;}if(i0<lo){diff[i0]+=c;diff[lo]-=c;}}
 internal static int Between(float[] sorted,float a,float b){if(b<=a)return 0;int lo=0,hi=sorted.Length;while(lo<hi){int m=(lo+hi)/2;if(sorted[m]<=a)lo=m+1;else hi=m;}int first=lo;hi=sorted.Length;while(lo<hi){int m=(lo+hi)/2;if(sorted[m]<b)lo=m+1;else hi=m;}return lo-first;}
 /* drop repeated / collinear points */
 internal static PointF[] Path(PointF[] pts){var list=new List<PointF>();foreach(var p in pts){if(list.Count>0&&Math.Abs(list[list.Count-1].X-p.X)<0.01f&&Math.Abs(list[list.Count-1].Y-p.Y)<0.01f)continue;
   if(list.Count>=2){var a=list[list.Count-2];var b=list[list.Count-1];if((Math.Abs(a.X-b.X)<0.01f&&Math.Abs(b.X-p.X)<0.01f)||(Math.Abs(a.Y-b.Y)<0.01f&&Math.Abs(b.Y-p.Y)<0.01f)){list[list.Count-1]=p;continue;}}list.Add(p);}
  if(list.Count<2)list.Add(list.Count==0?new PointF(0,0):list[0]);return list.ToArray();}
}
}
namespace BeamScheduleFormatter {
/* layered placement shared by the PDM and the PERT network (owner, 18 November 2026: "masmaganda yung layout ng arrows ng
   previous"). A relation that spans several columns gets a passing point in every column between its ends, so the order
   of a column also decides where its long lines run. The columns are ordered by barycentre sweeps with adjacent swaps
   (the order with the fewest crossings is kept); then every node and every passing point is set at the median height of
   its neighbours where the spacing allows (priority method: passing points and critical nodes first), so most arrows run
   straight and every long line keeps a free lane through the columns it passes. */
sealed class NetLayout {
 /* weights of the height placement: Mu = pull towards the middle (per node, per row step), Crit = weight of a critical
    relation, Near = extra cost of a line that just misses straight (a short jog), Grid > 0 = every height on a lattice of
    that step (the spacings are multiples of it, so lines meet exactly), Pass = least distance between two passing lines,
    CritRise = cost of the vertical travel of a critical relation (per row step), Margin = room kept free above the top and
    below the bottom node (lanes for the lines and their labels) */
 internal sealed class Tune {internal float Mu=0.4f,Crit=2,Lam=0.2f,Near,Grid,Pass,CritRise,Margin;
  internal Tune With(float mu,float crit,float near,float grid,float pass,float rise,float margin){return new Tune{Mu=mu,Crit=crit,Lam=Lam,Near=near,Grid=grid,Pass=pass,CritRise=rise,Margin=margin};}}
 readonly List<int> layer=new List<int>();readonly List<bool> virt=new List<bool>(),crit=new List<bool>();
 readonly List<List<int>> up=new List<List<int>>(),down=new List<List<int>>();/* proper edges (one column apart) by node */
 readonly List<int> ea=new List<int>(),eb=new List<int>(),eo=new List<int>();readonly List<float> ew=new List<float>();readonly List<bool> ec=new List<bool>();
 /* routing rank of every relation (lower = routed first): with TailLanes the first line routed out of a node takes the lane
    at the height of the node (PERT), so another line out of it runs straight only when that first one does */
 readonly List<int> rank=new List<int>();internal bool TailLanes;
 readonly int real;int edges;internal List<List<int>> Layers;internal float[] Y;int[] pos;bool[] bottom;
 /* n real nodes 0..n-1 in columns layerOf[i]; order = their initial order in every column */
 internal NetLayout(int n,int[] layerOf,List<List<int>> order){real=n;bottom=new bool[n];for(int i=0;i<n;i++)Add(layerOf[i],false,false);Layers=order.Select(l=>new List<int>(l)).ToList();}
 /* a node kept at the bottom of its column (its line to FINISH runs down to the collector below the network) */
 internal void Bottom(int node){bottom[node]=true;}
 bool Low(int x){return x<real&&bottom[x];}
 int Add(int l,bool v,bool c){layer.Add(l);virt.Add(v);crit.Add(c);up.Add(new List<int>());down.Add(new List<int>());return layer.Count-1;}
 void Link(int a,int b,float w,int orig,bool c){int k=ea.Count;ea.Add(a);eb.Add(b);ew.Add(w);eo.Add(orig);ec.Add(c);down[a].Add(k);up[b].Add(k);}
 /* relation from u to v (u in an earlier column) with a passing point in every column between; returns its number (for
    the head offsets) */
 internal readonly List<int[]> Ends=new List<int[]>();
 internal string Dump(Func<int,string> name){var sb=new System.Text.StringBuilder();for(int l=0;l<Layers.Count;l++){sb.Append("L"+l+":");foreach(var x in Layers[l]){if(x<real)sb.Append(" "+name(x)+"@"+(int)Y[x]);else{int o=eo[up[x][0]];sb.Append(" ["+name(Ends[o][0])+">"+name(Ends[o][1])+(crit[x]?"!":"")+"]@"+(int)Y[x]);}}sb.AppendLine();}return sb.ToString();}
 internal int Edge(int u,int v,float w,bool critical,int routeRank){int orig=edges++;rank.Add(routeRank);Ends.Add(new[]{u,v});if(layer[u]>=layer[v])return orig;int prev=u;
  if(critical&&SpineBand!=null){crit[u]=true;crit[v]=true;}
  for(int l=layer[u]+1;l<layer[v];l++){int id=Add(l,true,critical);Link(prev,id,w,orig,critical);prev=id;}Link(prev,v,w,orig,critical);return orig;}
 /* the critical path as a spine: with SpineBand = {lo, hi, sRR, sRV, pass} (the band and spacings of Assign) every column
    keeps its critical entries (critical nodes and the passing points of critical relations) where they can stand at one
    common height, the middle of the band, with the other entries of the column above and below them, so the critical path
    can run straight through the middle of the drawing; the entry moves as little as it must */
 internal float[] SpineBand;
 void FitSpine(int l){if(SpineBand==null)return;var lv=Layers[l];var sp=lv.Where(x=>crit[x]).ToList();if(sp.Count==0||sp.Count==lv.Count)return;
  float lo=SpineBand[0],hi=SpineBand[1],mid=(lo+hi)/2;Func<int,int,float> gap=(a,b)=>virt[a]&&virt[b]?SpineBand[4]:virt[a]||virt[b]?SpineBand[3]:SpineBand[2];
  var rest=lv.Where(x=>!crit[x]).ToList();int firstLow=rest.FindIndex(Low);if(firstLow<0)firstLow=rest.Count;int cur=lv.Take(lv.IndexOf(sp[0])).Count(x=>!crit[x]);
  int bestP=cur;double bestBad=double.MaxValue;
  for(int p=0;p<=firstLow;p++){var cand=rest.Take(p).Concat(sp).Concat(rest.Skip(p)).ToList();float above=0,below=0;for(int i=0;i<p;i++)above+=gap(cand[i],cand[i+1]);for(int i=p;i+1<cand.Count;i++)below+=gap(cand[i],cand[i+1]);
   double bad=Math.Max(0,above-(mid-lo))+Math.Max(0,below-(hi-mid));bad=bad*1000+Math.Abs(p-cur);if(bad<bestBad){bestBad=bad;bestP=p;}}
  if(bestP==cur&&lv.Skip(lv.IndexOf(sp[0])).Take(sp.Count).All(x=>crit[x]))return;
  Layers[l]=rest.Take(bestP).Concat(sp).Concat(rest.Skip(bestP)).ToList();for(int i=0;i<Layers[l].Count;i++)pos[Layers[l][i]]=i;}
 void Number(){if(pos==null||pos.Length<layer.Count)pos=new int[layer.Count];foreach(var lv in Layers)for(int i=0;i<lv.Count;i++)pos[lv[i]]=i;}
 long Crossings(){Number();long total=0;for(int l=0;l+1<Layers.Count;l++){var pairs=new List<KeyValuePair<int,int>>();foreach(var a in Layers[l])foreach(var k in down[a])pairs.Add(new KeyValuePair<int,int>(pos[a],pos[eb[k]]));
   pairs.Sort((p,q)=>p.Key!=q.Key?p.Key.CompareTo(q.Key):p.Value.CompareTo(q.Value));int m=Layers[l+1].Count+1;var bit=new int[m+1];int seen=0;
   foreach(var p in pairs){int le=0;for(int i=p.Value+1;i>0;i-=i&-i)le+=bit[i];total+=seen-le;seen++;for(int i=p.Value+1;i<=m;i+=i&-i)bit[i]++;}}
  return total;}
 /* crossings between the lines of u and those of v when u is just above v */
 int Pair(int u,int v){int c=0;foreach(var i in up[u])foreach(var j in up[v])if(pos[ea[i]]>pos[ea[j]])c++;foreach(var i in down[u])foreach(var j in down[v])if(pos[eb[i]]>pos[eb[j]])c++;return c;}
 void Sort(int l,bool fromUp){var lv=Layers[l];int adj=fromUp?l-1:l+1;if(adj<0||adj>=Layers.Count)return;double scale=lv.Count<=1?0:(Layers[adj].Count-1)/(double)(lv.Count-1);
  var key=new Dictionary<int,double>();foreach(var x in lv){var list=(fromUp?up[x].Select(k=>(double)pos[ea[k]]):down[x].Select(k=>(double)pos[eb[k]])).ToList();key[x]=list.Count==0?pos[x]*scale:list.Average();}
  Layers[l]=lv.OrderBy(x=>Low(x)?1:0).ThenBy(x=>key[x]).ToList();for(int i=0;i<Layers[l].Count;i++)pos[Layers[l][i]]=i;FitSpine(l);}
 void Transpose(){for(int pass=0;pass<8;pass++){bool any=false;foreach(var lv in Layers)for(int i=0;i+1<lv.Count;i++){int u=lv[i],v=lv[i+1];if(Low(u)!=Low(v))continue;if(Pair(v,u)<Pair(u,v)){lv[i]=v;lv[i+1]=u;pos[v]=i;pos[u]=i+1;any=true;}}if(!any)break;}for(int l=0;l<Layers.Count;l++)FitSpine(l);}
 /* column order with the fewest crossings: barycentre sweeps left to right and back, adjacent swaps, the best order kept */
 internal void Order(int rounds){var byLayer=new List<List<int>>();for(int l=0;l<Layers.Count;l++)byLayer.Add(new List<int>());for(int i=real;i<layer.Count;i++)byLayer[layer[i]].Add(i);
  /* a passing point starts next to the line it continues */
  Number();for(int l=0;l<Layers.Count;l++){var key=new Dictionary<int,double>();int cnt=Layers[l].Count;for(int i=0;i<cnt;i++)key[Layers[l][i]]=(i+0.5)/Math.Max(1,cnt);
   foreach(var v in byLayer[l]){int a=ea[up[v][0]];key[v]=(pos[a]+0.5)/Math.Max(1,Layers[layer[a]].Count)+1e-6;}
   Layers[l]=Layers[l].Concat(byLayer[l]).OrderBy(x=>Low(x)?1:0).ThenBy(x=>key[x]).ToList();for(int i=0;i<Layers[l].Count;i++)pos[Layers[l][i]]=i;}
  Transpose();long best=Crossings();var keep=Layers.Select(x=>new List<int>(x)).ToList();int still=0;
  for(int r=0;r<rounds&&still<3;r++){bool better=false;for(int dir=0;dir<2;dir++){if(dir==0)for(int l=1;l<Layers.Count;l++)Sort(l,true);else for(int l=Layers.Count-2;l>=0;l--)Sort(l,false);
    Transpose();long c=Crossings();if(c<best){best=c;keep=Layers.Select(x=>new List<int>(x)).ToList();better=true;}}
   still=better?0:still+1;}
  Layers=keep;Number();}
 /* heights of the centres: lo..hi = band; least distances between neighbours in a column: two nodes sRR (at least minRR when a
    column is crowded), a node and a passing line sRV, two passing lines t.Pass; headOffset(relation) = height of its arrowhead
    on the head node above (-) or below (+) the centre. Start: every column packed and centred, then median sweeps (priority
    method: passing points and critical nodes first), then the local search of Refine. */
 internal void Assign(float lo,float hi,float sRR,float sRV,float minRR,Func<int,float> headOffset,Tune t){int N=layer.Count;var y=new float[N];float grid=t.Grid;
  Func<float,float> snap=v=>grid>0?lo+(float)Math.Round((v-lo)/grid)*grid:v;if(grid>0)hi=lo+(float)Math.Floor((hi-lo)/grid+1e-4)*grid;
  /* the margin of the band, as far as the busiest column leaves room for it (its nodes minRR apart) */
  float mg=Math.Max(0,Math.Min(t.Margin,(hi-lo-Layers.Select(lv=>Math.Max(0,lv.Count(x=>x<real)-1)*minRR).DefaultIfEmpty(0).Max())/2));if(grid>0)mg=(float)Math.Floor(mg/grid+1e-4)*grid;
  var seps=new List<float[]>();var llo=new float[Layers.Count];var lhi=new float[Layers.Count];
  for(int l=0;l<Layers.Count;l++){var lv=Layers[l];var s=new float[Math.Max(0,lv.Count-1)];for(int i=0;i+1<lv.Count;i++){bool a=virt[lv[i]],b=virt[lv[i+1]];s[i]=a&&b?t.Pass:a||b?sRV:sRR;}
   llo[l]=lo+mg;lhi[l]=hi-mg;
   /* a crowded column: every spacing shrinks towards its least value (passing lines 0, a passing line between two nodes
      half of minRR from each, two nodes minRR) */
   float avail=lhi[l]-llo[l],total=s.Sum();if(total>avail){int first=lv.FindIndex(x=>!virt[x]),last=lv.FindLastIndex(x=>!virt[x]);var least=new float[s.Length];
    for(int i=0;i<s.Length;i++){bool a=virt[lv[i]],b=virt[lv[i+1]];least[i]=!a&&!b?minRR:a&&b?0:i>=first&&i<last?minRR/2:0;}
    float lsum=least.Sum(),k=lsum>=total?0:Math.Max(0,Math.Min(1,(avail-lsum)/(total-lsum)));for(int i=0;i<s.Length;i++)s[i]=least[i]+k*(s[i]-least[i]);total=s.Sum();
    if(total>avail)for(int i=0;i<s.Length;i++)s[i]*=avail/total;/* more nodes than the column holds: the callers split such levels */}
   seps.Add(s);float sum=s.Sum(),y0=snap((lo+hi)/2-sum/2);if(y0+sum>lhi[l]+0.5f)y0-=grid;if(y0<llo[l])y0=llo[l];for(int i=0;i<lv.Count;i++){y[lv[i]]=y0;if(i<s.Length)y0+=s[i];}}
  Func<int,float> off=k=>eb[k]<real?headOffset(eo[k]):0;
  Func<float[],double> cost=yy=>{double c=0;for(int k=0;k<ea.Count;k++){double d=Math.Abs(yy[eb[k]]+off(k)-yy[ea[k]]);c+=(d>0.5?1:0)*ew[k]+d/sRR*0.2*ew[k];}return c;};
  var best=(float[])y.Clone();double bestCost=cost(y);
  for(int r=0;r<2;r++)for(int dir=0;dir<2;dir++){if(dir==0)for(int l=1;l<Layers.Count;l++)PlaceLayer(l,true,y,seps[l],llo[l],lhi[l],off);else for(int l=Layers.Count-2;l>=0;l--)PlaceLayer(l,false,y,seps[l],llo[l],lhi[l],off);
   double c=cost(y);if(c<bestCost-1e-9){bestCost=c;best=(float[])y.Clone();}}
  y=best;Refine(y,seps,llo,lhi,(lo+hi)/2,off,sRR,t,Environment.GetEnvironmentVariable("REFP")==null?8:int.Parse(Environment.GetEnvironmentVariable("REFP")));
  /* the drawing as a whole centred in the band */
  if(N>0&&real>0){float rmn=Enumerable.Range(0,real).Min(i=>y[i]),rmx=Enumerable.Range(0,real).Max(i=>y[i]);float down0=Enumerable.Range(0,N).Max(i=>llo[layer[i]]-y[i]),up0=Enumerable.Range(0,N).Min(i=>lhi[layer[i]]-y[i]);
   float shift=(lo+hi)/2-(rmn+rmx)/2;if(grid>0)shift=(float)Math.Round(shift/grid)*grid;shift=Math.Max(down0,Math.Min(up0,shift));if(down0>up0)shift=0;for(int i=0;i<N;i++)y[i]+=shift;}
  Y=y;}
 /* local search on the drawing cost: a relation that does not run straight costs 1 (two bends), plus its vertical travel;
    a node also pays a little for its distance from the middle. A block (nodes joined by straight relations), the part of
    it left or right of one column, or one node is moved as a whole to the height where one more of its relations runs
    straight (or as near as the columns allow), the other nodes of the columns making room, when that lowers the cost */
 void Refine(float[] y,List<float[]> seps,float[] llo,float[] lhi,float mid,Func<int,float> off,float sRR,Tune t,int passes){int N=layer.Count;if(t.Grid>0&&llo.Length>0)mid=llo[0]+(float)Math.Round((mid-llo[0])/t.Grid)*t.Grid;Number();
  double lam=t.Lam/sRR,mu=t.Mu/sRR,critW=t.Crit,near=t.Near,rise=t.CritRise/sRR;const double nearD=20;
  /* a line that just misses straight (a short jog) is worse than one that clearly steps */
  Func<int,float[],double> ecost=(k,yy)=>{double d=Math.Abs(yy[eb[k]]+off(k)-yy[ea[k]]);return ((d>0.5?1:0)+(d>0.5&&d<nearD?near:0)+d*lam)*ew[k]*(ec[k]?critW:1)+(ec[k]?d*rise:0);};
  Func<int,float[],double> lcost=(l,yy)=>{double c=0;foreach(var x in Layers[l])if(x<real)c+=mu*Math.Abs(yy[x]-mid);return c;};
  /* a line out of a node that would run straight but finds the lane taken by the first line routed out of that node
     needs four bends instead of none */
  Func<int,float[],double> tcost=(tl,yy)=>{if(!TailLanes||tl>=real||down[tl].Count<2)return 0;int f=down[tl].OrderBy(k=>rank[eo[k]]).First();
   Func<int,bool> straight=k=>Math.Abs(yy[eb[k]]+off(k)-yy[ea[k]])<=0.5f;if(straight(f))return 0;double c=0;foreach(var k in down[tl])if(k!=f&&straight(k))c+=2*ew[k]*(ec[k]?critW:1);return c;};
  var moved=new List<int>();var stamp=new int[ea.Count];int tick=0;
  /* the other nodes of layer l make room for the fixed ones; false when the column cannot */
  Func<int,HashSet<int>,bool> resolve=(l,fix)=>{var lv=Layers[l];var sp=seps[l];int m=lv.Count;
   for(int i=1;i<m;i++){float need=y[lv[i-1]]+sp[i-1];if(y[lv[i]]<need-1e-3f){if(fix.Contains(lv[i]))return false;y[lv[i]]=need;moved.Add(lv[i]);}}
   for(int i=m-2;i>=0;i--){float need=y[lv[i+1]]-sp[i];if(y[lv[i]]>need+1e-3f){if(fix.Contains(lv[i]))return false;y[lv[i]]=need;moved.Add(lv[i]);}}
   return m==0||(y[lv[0]]>=llo[l]-0.5f&&y[lv[m-1]]<=lhi[l]+0.5f);};
  Func<List<int>,int,bool> tryMove=(set,x)=>{var inB=new HashSet<int>(set);var shifts=new List<float>();Action<float> add=d=>{if(Math.Abs(d)>0.5f&&!shifts.Any(z=>Math.Abs(z-d)<0.01f))shifts.Add(d);};
    foreach(var u in set)foreach(var k in up[u].Concat(down[u])){int v=ea[k]==u?eb[k]:ea[k];if(inB.Contains(v))continue;add((u==eb[k]?y[ea[k]]-off(k):y[eb[k]]+off(k))-y[u]);}
    if(x<real)add(mid-y[x]);
    /* the same shifts as far as they go without moving any other node */
    float free0=float.MinValue,free1=float.MaxValue;foreach(var u in set){var lv=Layers[layer[u]];int i=pos[u];float a=i>0&&!inB.Contains(lv[i-1])?y[lv[i-1]]+seps[layer[u]][i-1]:llo[layer[u]],b=i+1<lv.Count&&!inB.Contains(lv[i+1])?y[lv[i+1]]-seps[layer[u]][i]:lhi[layer[u]];free0=Math.Max(free0,a-y[u]);free1=Math.Min(free1,b-y[u]);}
    foreach(var d in shifts.ToList())add(Math.Max(free0,Math.Min(free1,d)));
    if(shifts.Count==0)return false;var y0=(float[])y.Clone();double bestD=-1e-6;float bestShift=float.NaN;var byLayer=set.GroupBy(u=>layer[u]).ToList();
    foreach(var d in shifts){moved.Clear();bool ok=true;foreach(var u in set){y[u]=y0[u]+d;moved.Add(u);}
     foreach(var g in byLayer){if(!resolve(g.Key,inB)){ok=false;break;}}
     if(ok){tick++;double delta=0;foreach(var u in moved.Distinct())foreach(var k in up[u].Concat(down[u])){if(stamp[k]==tick)continue;stamp[k]=tick;delta+=ecost(k,y)-ecost(k,y0);}
      foreach(var l in moved.Select(u=>layer[u]).Distinct())delta+=lcost(l,y)-lcost(l,y0);
      if(TailLanes)foreach(var tl in moved.Concat(moved.SelectMany(u=>up[u].Select(k=>ea[k]))).Distinct())delta+=tcost(tl,y)-tcost(tl,y0);
      if(delta<bestD){bestD=delta;bestShift=d;}}
     foreach(var u in moved)y[u]=y0[u];}
    if(float.IsNaN(bestShift))return false;moved.Clear();foreach(var u in set)y[u]=y0[u]+bestShift;foreach(var g in byLayer)resolve(g.Key,inB);return true;};
  for(int pass=0;pass<passes;pass++){bool improved=false;
   for(int x=0;x<N;x++){var block=new List<int>{x};var inB=new HashSet<int>{x};
    for(int q=0;q<block.Count&&block.Count<400;q++){int u=block[q];foreach(var k in up[u].Concat(down[u])){int v=ea[k]==u?eb[k]:ea[k];if(inB.Contains(v))continue;if(Math.Abs(y[eb[k]]+off(k)-y[ea[k]])<=0.5f){inB.Add(v);block.Add(v);}}}
    var sets=new List<List<int>>{block};if(block.Count>1){var left=block.Where(u=>layer[u]<=layer[x]).ToList();var right=block.Where(u=>layer[u]>=layer[x]).ToList();if(left.Count<block.Count)sets.Add(left);if(right.Count<block.Count)sets.Add(right);sets.Add(new List<int>{x});}
    foreach(var set in sets)if(tryMove(set,x)){improved=true;break;}}
   if(!improved)break;}}
 /* one column placed from the heights of its neighbours on one side: every node at the (weighted) median height of its
    neighbours, the nodes with the highest priority first, the others making room */
 void PlaceLayer(int l,bool fromUp,float[] y,float[] s,float lo,float hi,Func<int,float> off){var lv=Layers[l];int m=lv.Count;if(m==0)return;
  var c=new float[m];for(int i=1;i<m;i++)c[i]=c[i-1]+s[i-1];var want=new float[m];var prio=new double[m];
  for(int i=0;i<m;i++){int x=lv[i];var vals=new List<KeyValuePair<float,float>>();if(fromUp)foreach(var k in up[x])vals.Add(new KeyValuePair<float,float>(y[ea[k]]-off(k),ew[k]*(ec[k]?4:1)));else foreach(var k in down[x])vals.Add(new KeyValuePair<float,float>(y[eb[k]]+off(k),ew[k]*(ec[k]?4:1)));
   if(vals.Count==0){want[i]=y[x];prio[i]=-1;continue;}
   vals.Sort((p,q)=>p.Key.CompareTo(q.Key));float half=vals.Sum(v=>v.Value)/2,acc=0;want[i]=vals[vals.Count-1].Key;
   for(int j=0;j<vals.Count;j++){acc+=vals[j].Value;if(acc>half+1e-4f){want[i]=vals[j].Key;break;}if(Math.Abs(acc-half)<=1e-4f&&j+1<vals.Count){float a=vals[j].Key,b=vals[j+1].Key;want[i]=Math.Abs(a-y[x])<=Math.Abs(b-y[x])?a:b;break;}}
   prio[i]=(crit[x]?1e6:0)+(virt[x]?1e5:0)+vals.Sum(v=>v.Value);}
  var placed=new bool[m];foreach(int i in Enumerable.Range(0,m).OrderByDescending(i=>prio[i]).ThenBy(i=>i)){float lower=lo+c[i],upper=hi-(c[m-1]-c[i]);
   for(int j=i-1;j>=0;j--)if(placed[j]){lower=Math.Max(lower,y[lv[j]]+c[i]-c[j]);break;}for(int j=i+1;j<m;j++)if(placed[j]){upper=Math.Min(upper,y[lv[j]]-(c[j]-c[i]));break;}
   float v=upper<lower?lower:Math.Max(lower,Math.Min(upper,want[i]));y[lv[i]]=v;placed[i]=true;
   for(int j=i+1;j<m&&!placed[j];j++)y[lv[j]]=Math.Max(y[lv[j]],y[lv[j-1]]+s[j-1]);for(int j=i-1;j>=0&&!placed[j];j--)y[lv[j]]=Math.Min(y[lv[j]],y[lv[j+1]]-s[j]);}}
 /* labels that a line runs through */
 internal static int Crossed(List<PointF[]> lines,IEnumerable<RectangleF> labels){int c=0;foreach(var r in labels){var inner=RectangleF.Inflate(r,-1.5f,-1.5f);if(lines.Any(pl=>{for(int k=0;k+1<pl.Length;k++)if(ScheduleSet.Crosses(pl[k],pl[k+1],inner))return true;return false;}))c++;}return c;}
 /* how a routed drawing reads: crossings, bends and length (per 200; bends and vertical length three times on the red
    critical lines) and lines through a node (they must not happen) */
 internal static double Score(List<PointF[]> lines,List<bool> critical,List<RectangleF> boxes,bool circles){double len=0;int bends=0,cross=0,through=0;var hs=new List<PointF[]>();var vs=new List<PointF[]>();
  for(int li=0;li<lines.Count;li++){var pl=lines[li];bends+=Math.Max(0,pl.Length-2)*(critical[li]?3:1);for(int k=0;k+1<pl.Length;k++){var a=pl[k];var b=pl[k+1];len+=Math.Abs(a.X-b.X)+Math.Abs(a.Y-b.Y)*(critical[li]?3:1);if(Math.Abs(a.Y-b.Y)<0.01f)hs.Add(new[]{a,b});else if(Math.Abs(a.X-b.X)<0.01f)vs.Add(new[]{a,b});}}
  foreach(var h in hs){float y=h[0].Y,x0=Math.Min(h[0].X,h[1].X)+0.5f,x1=Math.Max(h[0].X,h[1].X)-0.5f;foreach(var v in vs){float x=v[0].X;if(x>x0&&x<x1&&y>Math.Min(v[0].Y,v[1].Y)+0.5f&&y<Math.Max(v[0].Y,v[1].Y)-0.5f)cross++;}}
  foreach(var bx in boxes){var r=circles?RectangleF.Inflate(bx,-4,-4):RectangleF.Inflate(bx,-2,-2);foreach(var s in hs.Concat(vs)){var a=s[0];var b=s[1];if(Math.Max(a.X,b.X)>r.Left&&Math.Min(a.X,b.X)<r.Right&&Math.Max(a.Y,b.Y)>r.Top&&Math.Min(a.Y,b.Y)<r.Bottom)through++;}}
  return cross+bends+len/200+through*50;}
}
}
