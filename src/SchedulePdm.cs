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
 internal HashSet<int> Widen=new HashSet<int>();internal const float JoinRoom=80,JoinAt=52;
 internal static PdmNetwork Build(List<ScheduleRow> rows){var n=new PdmNetwork();List<PdmNode> nodes;n.Links=ScheduleLogic.Network(rows,out nodes,true);n.Nodes.AddRange(nodes);n.Total=nodes.Count==0?0:nodes.Max(x=>x.EF);
  foreach(var nd in nodes){int lv=1;foreach(var l in n.Links.Where(x=>x.To==nd))lv=Math.Max(lv,l.From.Level+1);nd.Level=lv;}
  n.Milestones.Add(new PdmMilestone{Name="START",Caption="Start of project",Day=0});n.Milestones.Add(new PdmMilestone{Name="FINISH",Caption="Project completion",Day=n.Total});return n;}
 internal bool Driving(ScheduleLink l){var p=l.From;int d=l.To.Row.Days;int b=l.Type=="FS"?p.EF+l.Lag:l.Type=="SS"?p.ES+l.Lag:p.EF+l.Lag-d;return p.Critical&&l.To.Critical&&b==l.To.ES;}
 /* nodes of each level ordered by the average position of their predecessors / successors (fewer crossings) */
 internal static void Order<T>(List<List<T>> levels,Func<T,IEnumerable<T>> preds,Func<T,IEnumerable<T>> succs,Func<T,int> id){
  var pos=new Dictionary<T,double>();Action number=()=>{foreach(var lv in levels)for(int i=0;i<lv.Count;i++)pos[lv[i]]=(i+0.5)/lv.Count;};number();
  for(int sweep=0;sweep<4;sweep++){for(int k=1;k<levels.Count;k++){var lv=levels[k];var key=lv.ToDictionary(n=>n,n=>{var ps=preds(n).Where(pos.ContainsKey).ToList();return ps.Count==0?pos[n]:ps.Average(x=>pos[x]);});levels[k]=lv.OrderBy(n=>key[n]).ThenBy(id).ToList();number();}
   for(int k=levels.Count-2;k>=0;k--){var lv=levels[k];var key=lv.ToDictionary(n=>n,n=>{var ss=succs(n).Where(pos.ContainsKey).ToList();return ss.Count==0?pos[n]:ss.Average(x=>pos[x]);});levels[k]=lv.OrderBy(n=>key[n]).ThenBy(id).ToList();number();}}}
 internal SizeF Layout(float height){Height=height;float step=NodeH+RowGap;float usable=height-Foot;int perCol=Math.Max(1,(int)((usable-Head-10)/step));float mid=Head+(usable-Head)/2;float x=6;Cuts.Clear();
  int gap=0;Func<float> gw=()=>LinkGap+(Widen.Contains(gap)?JoinRoom:0);
  Milestones[0].X=x;Milestones[0].Y=mid-NodeH/2;x+=NodeW;Cuts.Add(x+JoinAt);x+=gw();gap++;int levels=Nodes.Count==0?0:Nodes.Max(nd=>nd.Level);
  var lists=new List<List<PdmNode>>();for(int lv=1;lv<=levels;lv++){var list=Nodes.Where(nd=>nd.Level==lv).OrderBy(nd=>nd.Id).ToList();if(list.Count>0)lists.Add(list);}
  Order(lists,nd=>Links.Where(l=>l.To==nd).Select(l=>l.From),nd=>Links.Where(l=>l.From==nd).Select(l=>l.To),nd=>nd.Id);
  foreach(var list in lists){int cols=(int)Math.Ceiling(list.Count/(double)perCol);int rows=Math.Min(list.Count,perCol);
   float blockH=rows*step-RowGap+(cols>1?step/2:0);float top=Math.Max(Head+6,mid-blockH/2);
   for(int i=0;i<list.Count;i++){int c=i/perCol,r=i%perCol;list[i].X=x+c*(NodeW+LinkGap);list[i].Y=top+r*step+(c%2==1?step/2:0);}x+=cols*(NodeW+LinkGap)-LinkGap;Cuts.Add(x+JoinAt);x+=gw();gap++;}
  Milestones[1].X=x;Milestones[1].Y=mid-NodeH/2;Right=x+NodeW+6;return new SizeF(Right,height);}
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
 internal Plan MakePlan(Graphics g,Font font,IList<float> joins=null){var plan=new Plan();var IC=CultureInfo.InvariantCulture;var start=Milestones[0];var finish=Milestones[1];
  Func<object,RectangleF> box=o=>{var nd=o as PdmNode;if(nd!=null)return new RectangleF(nd.X,nd.Y,NodeW,NodeH);var m=(PdmMilestone)o;return new RectangleF(m.X,m.Y,NodeW,NodeH);};
  var edges=new List<Edge>();var ins=Nodes.ToDictionary(nd=>nd,nd=>Links.Where(l=>l.To==nd).ToList());var outs=Nodes.ToDictionary(nd=>nd,nd=>Links.Where(l=>l.From==nd).ToList());
  foreach(var nd in Nodes.Where(x=>ins[x].Count==0))edges.Add(new Edge{From=start,To=nd,Critical=nd.Critical&&nd.ES==0});
  /* FS without lag is not labelled (the usual convention); other relations show their type and lag at the arrowhead */
  foreach(var l in Links)edges.Add(new Edge{From=l.From,To=l.To,Label=l.Type=="FS"&&l.Lag==0?null:l.Label,Critical=Driving(l)});
  /* every activity whose finish has no successor (no relation, or only SS relations from its start) runs to FINISH along
     one collector line below the network, so no finish is left open */
  foreach(var nd in Nodes.Where(x=>!outs[x].Any(l=>l.Type!="SS")))edges.Add(new Edge{From=nd,To=finish,Critical=nd.Critical&&nd.EF==Total,Collector=true});
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
   var pts=NetRoute.Sweep(e.FromBox.Right,py,xa,xb,e.ToBox.X,sy,hard,used,joins,0,Height-Foot,out cost);
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
 internal const string LegendText="Precedence diagram (activity on node). FS = finish-to-start, SS = start-to-start (overlap), FF = finish-to-finish; the label at the arrowhead gives the type and lag in working days (FS without lag is not labelled). M1, M2 ... = milestones (D = 0) where the items of one trade meet. ES of the first activity = 0; EF = ES + D; LS = LF - D; TF = LS - ES. Red = critical path (TF = 0). The sheets join at the match lines (trim along a match line and butt the next sheet to it).";
 internal const string PertLegendText="PERT-CPM on the same network: a = 0.8 m (optimistic), m = most likely, b = 1.2 m (pessimistic); te = (a + 4m + b) / 6; ES / EF with te. Labels at the arrowheads = predecessor no., relation and lag. Red = critical path.";
 internal const float KeyH=NodeH+14;
 /* standard node key, drawn once at the top of each sheet */
 internal static void DrawKey(Graphics g,float x,float y,Font font,Font bold,bool pert=false){
  using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted))using(var cell=new SolidBrush(Color.FromArgb(234,238,242)))using(var white=new SolidBrush(Color.White))using(var grid=new Pen(Color.FromArgb(180,188,198),0.6f))using(var edge=new Pen(Color.FromArgb(70,78,88),0.9f)){
   g.DrawString("NODE KEY",bold,ink,x,y);var r=new RectangleF(x+70,y,NodeW,NodeH);
   Box(g,r,pert?new[]{"a","m","b"}:new[]{"ES","D","EF"},"Activity no.   Item no.","Description (up to two lines)",pert?new[]{"ES","te","EF"}:new[]{"LS","TF","LF"},false,ink,bold,bold,ink,muted,cell,white,grid,edge);
   string[] k=pert?new[]{"a / m / b = optimistic / most likely / pessimistic duration","te = expected duration = (a + 4m + b) / 6","ES / EF = early start / finish with te (working days)","\"SS +3\" = starts 3 days after the predecessor starts"}
    :new[]{"ES = early start, EF = early finish","LS = late start, LF = late finish","D = duration, TF = total float = LS - ES (working days)","\"SS +3\" = starts 3 days after the predecessor starts; FS = after it finishes; FF = finishes after it finishes"};
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
 internal static List<PointF> Sweep(float x0,float y0,float xa,float xb,float x1,float y1,List<RectangleF> hard,List<RectangleF> soft,IList<float> joins,float top,float bottom,out int cost){
  cost=0;joins=joins??new float[0];var obs=hard.Where(b=>b.Right>xa&&b.X<xb).ToList();var sobs=soft.Where(b=>b.Right>xa&&b.X<xb).ToList();
  Func<float,float,float,bool> free=(y,from,to)=>{foreach(var b in obs)if(b.Right>from&&b.X<to&&y>b.Y&&y<b.Bottom)return false;return true;};
  Func<float,float,float,int> softHits=(y,from,to)=>sobs.Count(b=>b.Right>from&&b.X<to&&y>b.Y-0.01f&&y<b.Bottom+0.01f);
  Func<float,float,float,float,bool> vfree=(x,ya,yb,dummy)=>{float lo=Math.Min(ya,yb),hi=Math.Max(ya,yb);foreach(var b in hard)if(x>b.X&&x<b.Right&&hi>b.Y&&lo<b.Bottom)return false;return true;};
  Func<float,bool> nearJoin=x=>joins.Any(j=>Math.Abs(j-x)<10);
  var pts=new List<PointF>{new PointF(x0,y0),new PointF(xa,y0)};
  /* straight at the head height or at the tail height when the whole run is free */
  foreach(var y in new[]{y1,y0})if(free(y,xa,xb)&&vfree(xa,y0,y,0)&&vfree(xb,y,y1,0)){cost=softHits(y,xa,xb);if(cost==0||y==y0){pts.Add(new PointF(xa,y));pts.Add(new PointF(xb,y));pts.Add(new PointF(xb,y1));pts.Add(new PointF(x1,y1));return pts;}}
  /* candidate heights: the gaps between the boxes of every column on the way, plus the two end heights */
  var cols=obs.GroupBy(b=>Math.Round(b.X/4)).Select(g=>g.ToList()).OrderBy(g=>g.Min(b=>b.X)).ToList();
  var ys=new List<float>{y0,y1,top+4,bottom-4};foreach(var c in cols){var l=c.OrderBy(b=>b.Y).ToList();ys.Add(Math.Max(top+4,l[0].Y-3));ys.Add(Math.Min(bottom-4,l[l.Count-1].Bottom+3));for(int i=0;i+1<l.Count;i++)if(l[i+1].Y>l[i].Bottom+1)ys.Add((l[i].Bottom+l[i+1].Y)/2);}
  ys=ys.Distinct().ToList();
  /* first height: the one that runs free the farthest, then nearest to the head */
  Func<float,float,float> reach=(y,from)=>{float x=from;foreach(var b in obs.Where(b=>b.Right>from&&y>b.Y&&y<b.Bottom).OrderBy(b=>b.X)){return Math.Max(from,b.X);}return xb;};
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
 /* drop repeated / collinear points */
 internal static PointF[] Path(PointF[] pts){var list=new List<PointF>();foreach(var p in pts){if(list.Count>0&&Math.Abs(list[list.Count-1].X-p.X)<0.01f&&Math.Abs(list[list.Count-1].Y-p.Y)<0.01f)continue;
   if(list.Count>=2){var a=list[list.Count-2];var b=list[list.Count-1];if((Math.Abs(a.X-b.X)<0.01f&&Math.Abs(b.X-p.X)<0.01f)||(Math.Abs(a.Y-b.Y)<0.01f&&Math.Abs(b.Y-p.Y)<0.01f)){list[list.Count-1]=p;continue;}}list.Add(p);}
  if(list.Count<2)list.Add(list.Count==0?new PointF(0,0):list[0]);return list.ToArray();}
}
}
