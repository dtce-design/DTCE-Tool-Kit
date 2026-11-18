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
sealed class PdmMilestone {internal string Name,Caption;internal int Day;internal float X,Y;}
sealed class PdmNetwork {
 internal readonly List<PdmNode> Nodes=new List<PdmNode>();internal readonly List<PdmMilestone> Milestones=new List<PdmMilestone>();internal List<ScheduleLink> Links=new List<ScheduleLink>();internal int Total;internal float Height;
 internal const float NodeW=190,NodeH=84,RowGap=26,ColGap=40,MileD=NodeW,Head=10,LinkGap=96;
 internal static PdmNetwork Build(List<ScheduleRow> rows){var n=new PdmNetwork();List<PdmNode> nodes;n.Links=ScheduleLogic.Network(rows,out nodes);n.Nodes.AddRange(nodes);n.Total=nodes.Count==0?0:nodes.Max(x=>x.EF);
  foreach(var nd in nodes){int lv=1;foreach(var l in n.Links.Where(x=>x.To==nd))lv=Math.Max(lv,l.From.Level+1);nd.Level=lv;}
  n.Milestones.Add(new PdmMilestone{Name="START",Caption="Start of project",Day=0});n.Milestones.Add(new PdmMilestone{Name="FINISH",Caption="Project completion",Day=n.Total});return n;}
 internal bool Driving(ScheduleLink l){var p=l.From;int d=l.To.Row.Days;int b=l.Type=="FS"?p.EF+l.Lag:l.Type=="SS"?p.ES+l.Lag:p.EF+l.Lag-d;return p.Critical&&l.To.Critical&&b==l.To.ES;}
 internal SizeF Layout(float height){Height=height;float step=NodeH+RowGap;int perCol=Math.Max(1,(int)((height-Head-10)/step));float mid=Head+(height-Head)/2;float x=6;
  Milestones[0].X=x;Milestones[0].Y=mid-NodeH/2;x+=NodeW+LinkGap;int levels=Nodes.Count==0?0:Nodes.Max(nd=>nd.Level);
  for(int lv=1;lv<=levels;lv++){var list=Nodes.Where(nd=>nd.Level==lv).OrderBy(nd=>nd.Id).ToList();if(list.Count==0)continue;int cols=(int)Math.Ceiling(list.Count/(double)perCol);int rows=Math.Min(list.Count,perCol);
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
 internal void Draw(Graphics g,float dx,float dy,Font font,Font bold,PertResult pert=null){var smooth=g.SmoothingMode;g.SmoothingMode=SmoothingMode.AntiAlias;var IC=CultureInfo.InvariantCulture;
  Color redC=Color.FromArgb(192,40,40),line=Color.FromArgb(120,130,142);var far=new StringFormat{Alignment=StringAlignment.Far,LineAlignment=StringAlignment.Far,FormatFlags=StringFormatFlags.NoWrap};
  using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted))using(var red=new SolidBrush(redC))using(var labelInk=new SolidBrush(Color.FromArgb(80,90,104)))
  using(var arrow=new Pen(line,0.9f){CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)})using(var critArrow=new Pen(redC,1.5f){CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)})using(var edge=new Pen(Color.FromArgb(70,78,88),0.9f))using(var critEdge=new Pen(redC,1.8f))
  using(var cellCrit=new SolidBrush(Color.FromArgb(251,233,231)))using(var cell=new SolidBrush(Color.FromArgb(234,238,242)))using(var white=new SolidBrush(Color.White))using(var grid=new Pen(Color.FromArgb(180,188,198),0.6f)){
   /* relations: exits spread along the right side of the predecessor, entries along the left side of the successor */
   var outs=Nodes.ToDictionary(nd=>nd,nd=>Links.Where(l=>l.From==nd).OrderBy(l=>l.To.Y).ToList());var ins=Nodes.ToDictionary(nd=>nd,nd=>Links.Where(l=>l.To==nd).OrderBy(l=>l.From.Y).ToList());
   var start=Milestones[0];var finish=Milestones[1];var roots=Nodes.Where(nd=>ins[nd].Count==0).ToList();var sinks=Nodes.Where(nd=>outs[nd].Count==0).ToList();
   Func<int,int,float> spread=(i,n)=>NodeH*(i+1)/(n+1);
   foreach(var nd in roots){int k=ins[nd].Count;float sy=dy+nd.Y+NodeH/2,ay=dy+start.Y+NodeH/2,xv=dx+nd.X-LinkGap/2;bool c=nd.Critical&&nd.ES==0;g.DrawLines(c?critArrow:arrow,new[]{new PointF(dx+start.X+NodeW,ay),new PointF(xv,ay),new PointF(xv,sy),new PointF(dx+nd.X,sy)});}
   foreach(var nd in sinks){float sy=dy+nd.Y+NodeH/2,fy=dy+finish.Y+NodeH/2,xv=dx+finish.X-LinkGap/2;bool c=nd.Critical&&nd.EF==Total;g.DrawLines(c?critArrow:arrow,new[]{new PointF(dx+nd.X+NodeW,sy),new PointF(xv,sy),new PointF(xv,fy),new PointF(dx+finish.X,fy)});}
   foreach(var l in Links.OrderBy(l=>Driving(l))){var p=l.From;var s=l.To;int oi=outs[p].IndexOf(l),ii=ins[s].IndexOf(l);float py=dy+p.Y+spread(oi,outs[p].Count),sy=dy+s.Y+spread(ii,ins[s].Count);
    float xv=dx+s.X-LinkGap+10+ii*Math.Min(10,(LinkGap-50)/Math.Max(1,ins[s].Count));bool c=Driving(l);g.DrawLines(c?critArrow:arrow,new[]{new PointF(dx+p.X+NodeW,py),new PointF(xv,py),new PointF(xv,sy),new PointF(dx+s.X,sy)});}
   /* relation labels on top of the lines */
   foreach(var l in Links){var s=l.To;int ii=ins[s].IndexOf(l);float sy=dy+s.Y+spread(ii,ins[s].Count);g.DrawString(l.From.Id+" "+l.Label,font,Driving(l)?(Brush)red:labelInk,new RectangleF(dx+s.X-LinkGap/2-6,sy-12,LinkGap/2+3,12),far);}
   foreach(var m in new[]{start,finish}){string d=m.Day.ToString(IC);Box(g,new RectangleF(dx+m.X,dy+m.Y,NodeW,NodeH),new[]{d,"0",d},m.Name+"  (milestone)",m.Caption,new[]{d,"0",d},true,red,font,bold,ink,muted,cellCrit,white,grid,critEdge);}
   foreach(var nd in Nodes){string[] top,bottom;if(pert==null){top=new[]{nd.ES.ToString(IC),nd.Row.Days.ToString(IC),nd.EF.ToString(IC)};bottom=new[]{nd.LS.ToString(IC),nd.TF.ToString(IC),nd.LF.ToString(IC)};}
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
