using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Precedence Diagram Method (owner, 18 November 2026: "idagdag ang PDM", "use standard layouting", "makatotohanan").
// Activity on node with the realistic construction logic of ScheduleLogic: every relation is drawn as its own arrow and
// labelled at its arrowhead with the predecessor no., the relation type and the lag (e.g. "12 SS +3"). Nodes are placed
// in columns by network level (START, then each activity one column after its latest predecessor, FINISH). Times in the
// textbook convention: ES of the first activity = 0, EF = ES + D, LS = LF - D, TF = LS - ES. Node: ES | D | EF, activity
// no. and item no., description (two lines), LS | TF | LF. Red = critical (TF = 0) and driving critical relations.
// The same network drawn with PERT values (a | m | b on top, ES | te | EF at the bottom) is the PERT-CPM network.
// Units are hundredths of an inch on paper (pixels on screen).
sealed class PdmNode {internal ScheduleRow Row;internal string Trade;internal int Id,ES,EF,LS,LF,TF,Level;internal bool Critical{get{return TF==0;}}internal float X,Y;}
sealed class NetEdge {internal object From,To;internal float FromX,ToX,Exit,Entry,Xa,Xb;internal string FromName,ToName,Label;internal bool Critical;}
sealed class PdmMilestone {internal string Name,Caption;internal int Day;internal float X,Y;}
sealed class PdmNetwork {
 internal readonly List<PdmNode> Nodes=new List<PdmNode>();internal readonly List<PdmMilestone> Milestones=new List<PdmMilestone>();internal List<ScheduleLink> Links=new List<ScheduleLink>();internal int Total;internal float Height;
 internal const float NodeW=190,NodeH=84,RowGap=30,ColGap=50,MileD=NodeW,Head=10;internal float LinkGap=120;/* gap between columns; narrowed on paper to save sheets */
 internal static PdmNetwork Build(List<ScheduleRow> rows){var n=new PdmNetwork();List<PdmNode> nodes;n.Links=ScheduleLogic.Network(rows,out nodes);n.Nodes.AddRange(nodes);n.Total=nodes.Count==0?0:nodes.Max(x=>x.EF);
  foreach(var nd in nodes){int lv=1;foreach(var l in n.Links.Where(x=>x.To==nd))lv=Math.Max(lv,l.From.Level+1);nd.Level=lv;}
  n.Milestones.Add(new PdmMilestone{Name="START",Caption="Start of project",Day=0});n.Milestones.Add(new PdmMilestone{Name="FINISH",Caption="Project completion",Day=n.Total});return n;}
 internal bool Driving(ScheduleLink l){var p=l.From;int d=l.To.Row.Days;int b=l.Type=="FS"?p.EF+l.Lag:l.Type=="SS"?p.ES+l.Lag:p.EF+l.Lag-d;return p.Critical&&l.To.Critical&&b==l.To.ES;}
 /* nodes of each level ordered by the average position of their predecessors / successors (fewer crossings) */
 internal static void Order<T>(List<List<T>> levels,Func<T,IEnumerable<T>> preds,Func<T,IEnumerable<T>> succs,Func<T,int> id){
  var pos=new Dictionary<T,double>();Action number=()=>{foreach(var lv in levels)for(int i=0;i<lv.Count;i++)pos[lv[i]]=(i+0.5)/lv.Count;};number();
  for(int sweep=0;sweep<4;sweep++){for(int k=1;k<levels.Count;k++){var lv=levels[k];var key=lv.ToDictionary(n=>n,n=>{var ps=preds(n).Where(pos.ContainsKey).ToList();return ps.Count==0?pos[n]:ps.Average(x=>pos[x]);});levels[k]=lv.OrderBy(n=>key[n]).ThenBy(id).ToList();number();}
   for(int k=levels.Count-2;k>=0;k--){var lv=levels[k];var key=lv.ToDictionary(n=>n,n=>{var ss=succs(n).Where(pos.ContainsKey).ToList();return ss.Count==0?pos[n]:ss.Average(x=>pos[x]);});levels[k]=lv.OrderBy(n=>key[n]).ThenBy(id).ToList();number();}}}
 internal SizeF Layout(float height){Height=height;float step=NodeH+RowGap;int perCol=Math.Max(1,(int)((height-Head-10)/step));float mid=Head+(height-Head)/2;float x=6;
  Milestones[0].X=x;Milestones[0].Y=mid-NodeH/2;x+=NodeW+LinkGap;int levels=Nodes.Count==0?0:Nodes.Max(nd=>nd.Level);
  var lists=new List<List<PdmNode>>();for(int lv=1;lv<=levels;lv++){var list=Nodes.Where(nd=>nd.Level==lv).OrderBy(nd=>nd.Id).ToList();if(list.Count>0)lists.Add(list);}
  Order(lists,nd=>Links.Where(l=>l.To==nd).Select(l=>l.From),nd=>Links.Where(l=>l.From==nd).Select(l=>l.To),nd=>nd.Id);
  foreach(var list in lists){int cols=(int)Math.Ceiling(list.Count/(double)perCol);int rows=Math.Min(list.Count,perCol);
   float blockH=rows*step-RowGap+(cols>1?step/2:0);float top=Math.Max(Head+6,mid-blockH/2);
   for(int i=0;i<list.Count;i++){int c=i/perCol,r=i%perCol;list[i].X=x+c*(NodeW+ColGap);list[i].Y=top+r*step+(c%2==1?step/2:0);}x+=cols*(NodeW+ColGap)-ColGap+LinkGap;}
  Milestones[1].X=x;Milestones[1].Y=mid-NodeH/2;return new SizeF(x+NodeW+6,height);}
 static void Box(Graphics g,RectangleF r,string[] top,string title,string text,string[] bottom,bool crit,Brush titleInk,Font font,Font bold,Brush ink,Brush muted,Brush fill,Brush white,Pen grid,Pen edge){
  var mid=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap};var left=new StringFormat(mid){Alignment=StringAlignment.Near};var wrap=new StringFormat{LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisWord};
  float rh=r.Height/5,cw=r.Width/3;g.FillRectangle(white,r);g.FillRectangle(fill,r.X,r.Y,r.Width,rh);g.FillRectangle(fill,r.X,r.Bottom-rh,r.Width,rh);
  g.DrawLine(grid,r.X,r.Y+rh,r.Right,r.Y+rh);g.DrawLine(grid,r.X,r.Bottom-rh,r.Right,r.Bottom-rh);for(int c=1;c<3;c++){g.DrawLine(grid,r.X+c*cw,r.Y,r.X+c*cw,r.Y+rh);g.DrawLine(grid,r.X+c*cw,r.Bottom-rh,r.X+c*cw,r.Bottom);}
  for(int c=0;c<3;c++){g.DrawString(top[c],font,ink,new RectangleF(r.X+c*cw,r.Y,cw,rh),mid);g.DrawString(bottom[c],font,ink,new RectangleF(r.X+c*cw,r.Bottom-rh,cw,rh),mid);}
  g.DrawString(title,bold,titleInk,new RectangleF(r.X+4,r.Y+rh,r.Width-8,rh),left);g.DrawString(text,font,muted,new RectangleF(r.X+4,r.Y+2*rh,r.Width-8,2*rh),wrap);g.DrawRectangle(edge,r.X,r.Y,r.Width,r.Height);}
 /* pert == null: PDM values; otherwise PERT values */
 static bool InPage(float x,float from,float to){return x+NodeW/2>=from&&x+NodeW/2<to;}
 /* small text with a white background just above a line, ending (right) or starting (left) at x */
 internal static void Tag(Graphics g,string t,Font font,Brush ink,Brush white,float x,float y,bool alignRight){var sz=g.MeasureString(t,font);var r=new RectangleF(alignRight?x-sz.Width:x,y-sz.Height-1,sz.Width,sz.Height-1);g.FillRectangle(white,r);g.DrawString(t,font,ink,r.X,r.Y);}
 /* pageFrom / pageTo: the part of the network on this sheet (layout x); relations to other sheets end at the sheet edge with
    the activity no. and sheet no. they continue to (owner, 18 November 2026: "yung continuity per page magulo") */
 internal void Draw(Graphics g,float dx,float dy,Font font,Font bold,PertResult pert=null,float pageFrom=float.MinValue/4,float pageTo=float.MaxValue/4,Func<float,int> sheetAt=null,float edgeFrom=float.NaN,float edgeTo=float.NaN){if(float.IsNaN(edgeFrom)){edgeFrom=pageFrom;edgeTo=pageTo;}var smooth=g.SmoothingMode;g.SmoothingMode=SmoothingMode.AntiAlias;var IC=CultureInfo.InvariantCulture;
  Color redC=Color.FromArgb(192,40,40),line=Color.FromArgb(120,130,142);var far=new StringFormat{Alignment=StringAlignment.Far,LineAlignment=StringAlignment.Far,FormatFlags=StringFormatFlags.NoWrap};
  using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted))using(var red=new SolidBrush(redC))using(var labelInk=new SolidBrush(Color.FromArgb(80,90,104)))
  using(var arrow=new Pen(line,0.9f){CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)})using(var critArrow=new Pen(redC,1.5f){CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)})using(var edge=new Pen(Color.FromArgb(70,78,88),0.9f))using(var critEdge=new Pen(redC,1.8f))
  using(var cellCrit=new SolidBrush(Color.FromArgb(251,233,231)))using(var cell=new SolidBrush(Color.FromArgb(234,238,242)))using(var white=new SolidBrush(Color.White))using(var grid=new Pen(Color.FromArgb(180,188,198),0.6f)){
   /* relations: exits spread along the right side of the predecessor, entries along the left side of the successor */
   var outs=Nodes.ToDictionary(nd=>nd,nd=>Links.Where(l=>l.From==nd).OrderBy(l=>l.To.Y).ToList());var ins=Nodes.ToDictionary(nd=>nd,nd=>Links.Where(l=>l.To==nd).OrderBy(l=>l.From.Y).ToList());
   var start=Milestones[0];var finish=Milestones[1];var roots=Nodes.Where(nd=>ins[nd].Count==0).ToList();var sinks=Nodes.Where(nd=>outs[nd].Count==0).ToList();
   Func<int,int,float> spread=(i,n)=>NodeH*(i+1)/(n+1);
   /* one list of drawn relations: START -> first activities, the relations, critical last activities -> FINISH;
      non-critical last activities get a short FINISH connector */
   foreach(var nd in sinks.Where(x=>!(x.Critical&&x.EF==Total))){if(!InPage(nd.X,pageFrom,pageTo))continue;float sy=dy+nd.Y+NodeH/2,x0=dx+nd.X+NodeW;g.DrawLine(arrow,x0,sy,x0+16,sy);var fs=g.MeasureString("FINISH",font);var st0=g.Save();g.TranslateTransform(x0+18,sy-fs.Width/2);g.RotateTransform(90);g.DrawString("FINISH",font,labelInk,0,-fs.Height);g.Restore(st0);}/* written downwards so it needs little room */
   var edges=new List<NetEdge>();
   foreach(var nd in roots)edges.Add(new NetEdge{From=start,FromX=start.X,FromName="START",Exit=dy+start.Y+NodeH/2,To=nd,ToX=nd.X,ToName=nd.Id.ToString(IC),Entry=dy+nd.Y+NodeH/2,Critical=nd.Critical&&nd.ES==0,Xa=dx+start.X+NodeW+12,Xb=dx+nd.X-LinkGap/2});
   foreach(var l in Links){var p=l.From;var t=l.To;int oi=outs[p].IndexOf(l),ii=ins[t].IndexOf(l);
    edges.Add(new NetEdge{From=p,FromX=p.X,FromName=p.Id.ToString(IC),Exit=dy+p.Y+NodeH/2,To=t,ToX=t.X,ToName=t.Id.ToString(IC),Entry=dy+t.Y+spread(ii,ins[t].Count),Critical=Driving(l),Label=p.Id+" "+l.Label,Xa=dx+p.X+NodeW+12+Math.Min(oi,6)*5,Xb=dx+t.X-18-Math.Min(ii,6)*7});}
   foreach(var nd in sinks.Where(x=>x.Critical&&x.EF==Total))edges.Add(new NetEdge{From=nd,FromX=nd.X,FromName=nd.Id.ToString(IC),Exit=dy+nd.Y+NodeH/2,To=finish,ToX=finish.X,ToName="FINISH",Entry=dy+finish.Y+NodeH/2,Critical=true,Xa=dx+nd.X+NodeW+12,Xb=dx+finish.X-LinkGap/2});
   var boxes=Nodes.Select(nd=>new KeyValuePair<object,RectangleF>(nd,new RectangleF(dx+nd.X-6,dy+nd.Y-6,NodeW+12,NodeH+12))).Concat(new[]{start,finish}.Select(m=>new KeyValuePair<object,RectangleF>(m,new RectangleF(dx+m.X-6,dy+m.Y-6,NodeW+12,NodeH+12)))).ToList();
   var lanes=NetRoute.Corridors(boxes.Select(k=>k.Value).ToList(),dy,dy+Height);
   float left=dx+edgeFrom+3,right=dx+edgeTo-3;var tags=new List<Action>();
   foreach(var e in edges.OrderBy(x=>x.Critical)){bool fin=InPage(e.FromX,pageFrom,pageTo),tin=InPage(e.ToX,pageFrom,pageTo);if(!fin&&!tin)continue;var pen=e.Critical?critArrow:arrow;
    float x0=dx+e.FromX+NodeW,x1=dx+e.ToX;if(e.Xb<e.Xa+4)e.Xb=e.Xa+4;
    if(fin&&tin){float lane=NetRoute.Lane(e.Exit,e.Entry,e.Xa,e.Xb,boxes.Where(k=>k.Key!=e.From&&k.Key!=e.To).Select(k=>k.Value),lanes);
     g.DrawLines(pen,NetRoute.Path(new[]{new PointF(x0,e.Exit),new PointF(e.Xa,e.Exit),new PointF(e.Xa,lane),new PointF(e.Xb,lane),new PointF(e.Xb,e.Entry),new PointF(x1,e.Entry)}));}
    else if(fin){/* continues on a later sheet: run to the right edge and name the successor and its sheet */
     float lane=NetRoute.Lane(e.Exit,e.Exit,e.Xa,right,boxes.Where(k=>k.Key!=e.From).Select(k=>k.Value),lanes);g.DrawLines(pen,NetRoute.Path(new[]{new PointF(x0,e.Exit),new PointF(e.Xa,e.Exit),new PointF(e.Xa,lane),new PointF(right,lane)}));
     string t="to "+e.ToName+(sheetAt==null?"":" (sheet "+sheetAt(e.ToX)+")");float ly=lane;Brush b=e.Critical?(Brush)red:labelInk;tags.Add(()=>Tag(g,t,font,b,white,right-2,ly,true));}
    else{/* comes from an earlier sheet: start at the left edge with the predecessor and its sheet */
     float lane=NetRoute.Lane(e.Entry,e.Entry,left,e.Xb,boxes.Where(k=>k.Key!=e.To).Select(k=>k.Value),lanes);g.DrawLines(pen,NetRoute.Path(new[]{new PointF(left,lane),new PointF(e.Xb,lane),new PointF(e.Xb,e.Entry),new PointF(x1,e.Entry)}));
     string t="from "+e.FromName+(sheetAt==null?"":" (sheet "+sheetAt(e.FromX)+")")+(e.Label!=null?" "+e.Label.Substring(e.Label.IndexOf(' ')+1):"");e.Label=null;float ly=lane;Brush b=e.Critical?(Brush)red:labelInk;tags.Add(()=>Tag(g,t,font,b,white,left+2,ly,false));}
    if(e.Label!=null&&tin){string t=e.Label;float ey=e.Entry,ex=x1;Brush b=e.Critical?(Brush)red:labelInk;tags.Add(()=>Tag(g,t,font,b,white,ex-3,ey,true));}}
   foreach(var t in tags)t();
   foreach(var m in new[]{start,finish}.Where(x=>InPage(x.X,pageFrom,pageTo))){string d=m.Day.ToString(IC);Box(g,new RectangleF(dx+m.X,dy+m.Y,NodeW,NodeH),new[]{d,"0",d},m.Name+"  (milestone)",m.Caption,new[]{d,"0",d},true,red,font,bold,ink,muted,cellCrit,white,grid,critEdge);}
   foreach(var nd in Nodes.Where(x=>InPage(x.X,pageFrom,pageTo))){string[] top,bottom;if(pert==null){top=new[]{nd.ES.ToString(IC),nd.Row.Days.ToString(IC),nd.EF.ToString(IC)};bottom=new[]{nd.LS.ToString(IC),nd.TF.ToString(IC),nd.LF.ToString(IC)};}
    else{var a=pert.Activities.First(x=>x.Node==nd);top=new[]{a.A.ToString("0.0",IC),a.M.ToString("0.#",IC),a.B.ToString("0.0",IC)};bottom=new[]{nd.ES.ToString(IC),a.Te.ToString("0.#",IC),nd.EF.ToString(IC)};}
    Box(g,new RectangleF(dx+nd.X,dy+nd.Y,NodeW,NodeH),top,nd.Id+"   "+nd.Row.Code,nd.Row.Description,bottom,nd.Critical,nd.Critical?(Brush)red:ink,font,bold,ink,muted,nd.Critical?cellCrit:cell,white,grid,nd.Critical?critEdge:edge);}}
  g.SmoothingMode=smooth;}
 internal const string LegendText="Activity on node with the construction logic: FS = finish-to-start, SS = start-to-start (overlap), FF = finish-to-finish; label at each arrowhead = predecessor no., type and lag in working days. ES of the first activity = 0; EF = ES + D; LS = LF - D; TF = LS - ES. Red = critical path (TF = 0).";
 internal const string PertLegendText="PERT-CPM on the same network: a = 0.8 m (optimistic), m = most likely, b = 1.2 m (pessimistic); te = (a + 4m + b) / 6; ES / EF with te. Labels at the arrowheads = predecessor no., relation and lag. Red = critical path.";
 internal const float KeyH=NodeH+14;
 /* standard node key, drawn once at the top of each sheet */
 internal static void DrawKey(Graphics g,float x,float y,Font font,Font bold,bool pert=false){
  using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted))using(var cell=new SolidBrush(Color.FromArgb(234,238,242)))using(var white=new SolidBrush(Color.White))using(var grid=new Pen(Color.FromArgb(180,188,198),0.6f))using(var edge=new Pen(Color.FromArgb(70,78,88),0.9f)){
   g.DrawString("NODE KEY",bold,ink,x,y);var r=new RectangleF(x+70,y,NodeW,NodeH);
   Box(g,r,pert?new[]{"a","m","b"}:new[]{"ES","D","EF"},"Activity no.   Item no.","Description (up to two lines)",pert?new[]{"ES","te","EF"}:new[]{"LS","TF","LF"},false,ink,bold,bold,ink,muted,cell,white,grid,edge);
   string[] k=pert?new[]{"a / m / b = optimistic / most likely / pessimistic duration","te = expected duration = (a + 4m + b) / 6","ES / EF = early start / finish with te (working days)","\"12 SS +3\" = starts 3 days after no. 12 starts"}
    :new[]{"ES = early start, EF = early finish","LS = late start, LF = late finish","D = duration, TF = total float = LS - ES (working days)","\"12 SS +3\" = starts 3 days after no. 12 starts; FS = after it finishes; FF = finishes after it finishes"};
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
 /* drop repeated / collinear points */
 internal static PointF[] Path(PointF[] pts){var list=new List<PointF>();foreach(var p in pts){if(list.Count>0&&Math.Abs(list[list.Count-1].X-p.X)<0.01f&&Math.Abs(list[list.Count-1].Y-p.Y)<0.01f)continue;
   if(list.Count>=2){var a=list[list.Count-2];var b=list[list.Count-1];if((Math.Abs(a.X-b.X)<0.01f&&Math.Abs(b.X-p.X)<0.01f)||(Math.Abs(a.Y-b.Y)<0.01f&&Math.Abs(b.Y-p.Y)<0.01f)){list[list.Count-1]=p;continue;}}list.Add(p);}
  if(list.Count<2)list.Add(list.Count==0?new PointF(0,0):list[0]);return list.ToArray();}
}
}
