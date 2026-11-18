using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Precedence Diagram Method (owner, 18 November 2026: "bar chart or precedence diagram method" - "idagdag ang PDM").
// Activities are the scheduled pay items (Bill of Quantities order = activity no.). Relations are Finish-to-Start through
// a zero-duration milestone at the end of each work phase: every activity of a phase precedes the phase milestone, and the
// milestone precedes every activity of the next phase - the same automatic sequence as the bar chart. Forward pass gives
// ES / EF, backward pass LS / LF; total float = LF - EF; activities with zero float are critical (red).
sealed class PdmNode {internal ScheduleRow Row;internal int Id,ES,EF,LS,LF,TF;internal bool Critical{get{return TF==0;}}internal float X,Y;}
sealed class PdmMilestone {internal string Name;internal int Day;internal float X,Y;}
sealed class PdmNetwork {
 internal readonly List<PdmNode> Nodes=new List<PdmNode>();internal readonly List<PdmMilestone> Milestones=new List<PdmMilestone>();internal readonly List<List<PdmNode>> Phases=new List<List<PdmNode>>();internal int Total;
 internal const float NodeW=150,NodeH=66,ColGap=40,RowGap=14,MileW=96,MileH=34;
 internal static PdmNetwork Build(List<ScheduleRow> rows){var n=new PdmNetwork();int id=0;var nodes=new List<PdmNode>();
  foreach(var r in rows){id++;if(r.Days<=0)continue;nodes.Add(new PdmNode{Row=r,Id=id,ES=r.Start,EF=r.Finish});}
  foreach(var phase in nodes.GroupBy(x=>x.Row.PhaseIndex).OrderBy(p=>p.Key)){var list=phase.ToList();int finish=list.Max(x=>x.EF);foreach(var x in list){x.LF=finish;x.LS=x.LF-x.Row.Days+1;x.TF=x.LF-x.EF;}n.Phases.Add(list);}
  n.Nodes.AddRange(nodes);n.Total=nodes.Count==0?0:nodes.Max(x=>x.EF);
  n.Milestones.Add(new PdmMilestone{Name="START",Day=0});foreach(var p in n.Phases)n.Milestones.Add(new PdmMilestone{Name=p==n.Phases.Last()?"FINISH":"Phase "+(n.Phases.IndexOf(p)+1)+" end",Day=p.Max(x=>x.EF)});return n;}
 /* columns: START | phase 1 (one or more sub-columns) | milestone | phase 2 | ... | FINISH */
 internal SizeF Layout(float maxHeight){int perCol=Math.Max(1,(int)((maxHeight-20)/(NodeH+RowGap)));float x=10;float height=0;
  var start=Milestones[0];start.X=x;x+=MileW+ColGap;
  for(int p=0;p<Phases.Count;p++){var list=Phases[p];int cols=(int)Math.Ceiling(list.Count/(double)perCol);for(int i=0;i<list.Count;i++){list[i].X=x+(i/perCol)*(NodeW+ColGap);list[i].Y=10+(i%perCol)*(NodeH+RowGap);}
   float colH=Math.Min(list.Count,perCol)*(NodeH+RowGap);height=Math.Max(height,colH);x+=cols*(NodeW+ColGap);var m=Milestones[p+1];m.X=x;x+=MileW+ColGap;}
  /* each milestone sits level with the activities next to it (short arrows) */
  Func<List<PdmNode>,float> centre=list=>list.Count==0?10+NodeH/2:list.Average(nd=>nd.Y+NodeH/2);
  for(int m=0;m<Milestones.Count;m++){var before=m>0?Phases[m-1]:null;var after=m<Phases.Count?Phases[m]:null;float c=before!=null&&after!=null?(centre(before)+centre(after))/2:centre(before??after??new List<PdmNode>());Milestones[m].Y=c-MileH/2;}
  return new SizeF(x,Math.Max(height,MileH)+30);}
 internal void Draw(Graphics g,float dx,float dy,Font font,Font bold,RectangleF? clip=null){g.SmoothingMode=SmoothingMode.AntiAlias;
  using(var ink=new SolidBrush(PowTheme.Ink))using(var red=new Pen(Color.Firebrick,1.6f){CustomEndCap=new AdjustableArrowCap(3,4)})using(var gray=new Pen(Color.FromArgb(150,158,168),0.8f){CustomEndCap=new AdjustableArrowCap(3,4)})using(var box=new Pen(PowTheme.Ink,0.8f))using(var critBox=new Pen(Color.Firebrick,1.8f))using(var fill=new SolidBrush(Color.White))using(var mfill=new SolidBrush(Color.FromArgb(236,240,244))){
   var mid=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap};
   /* arrows: previous milestone -> node -> next milestone */
   for(int p=0;p<Phases.Count;p++){var a=Milestones[p];var b=Milestones[p+1];foreach(var nd in Phases[p]){var pen=nd.Critical?red:gray;g.DrawLine(pen,dx+a.X+MileW,dy+a.Y+MileH/2,dx+nd.X,dy+nd.Y+NodeH/2);g.DrawLine(pen,dx+nd.X+NodeW,dy+nd.Y+NodeH/2,dx+b.X,dy+b.Y+MileH/2);}}
   foreach(var m in Milestones){var r=new RectangleF(dx+m.X,dy+m.Y,MileW,MileH);g.FillEllipse(mfill,r);g.DrawEllipse(critBox,r);g.DrawString(m.Name+"\nday "+m.Day,font,ink,r,new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center});}
   foreach(var nd in Nodes){var r=new RectangleF(dx+nd.X,dy+nd.Y,NodeW,NodeH);g.FillRectangle(fill,r);g.DrawRectangle(nd.Critical?critBox:box,r.X,r.Y,r.Width,r.Height);float rh=NodeH/3,cw=NodeW/3;
    g.DrawLine(box,r.X,r.Y+rh,r.Right,r.Y+rh);g.DrawLine(box,r.X,r.Y+2*rh,r.Right,r.Y+2*rh);for(int c=1;c<3;c++){g.DrawLine(box,r.X+c*cw,r.Y,r.X+c*cw,r.Y+rh);g.DrawLine(box,r.X+c*cw,r.Y+2*rh,r.X+c*cw,r.Bottom);}
    string[] top={nd.ES.ToString(CultureInfo.InvariantCulture),nd.Row.Days.ToString(CultureInfo.InvariantCulture),nd.EF.ToString(CultureInfo.InvariantCulture)},bottom={nd.LS.ToString(CultureInfo.InvariantCulture),nd.TF.ToString(CultureInfo.InvariantCulture),nd.LF.ToString(CultureInfo.InvariantCulture)};
    for(int c=0;c<3;c++){g.DrawString(top[c],font,ink,new RectangleF(r.X+c*cw,r.Y,cw,rh),mid);g.DrawString(bottom[c],font,ink,new RectangleF(r.X+c*cw,r.Y+2*rh,cw,rh),mid);}
    g.DrawString(nd.Id+". "+nd.Row.Code,bold,nd.Critical?(Brush)Brushes.Firebrick:ink,new RectangleF(r.X+2,r.Y+rh,NodeW-4,rh),mid);}}}
 internal static void Legend(Graphics g,float x,float y,Font font){using(var ink=new SolidBrush(PowTheme.Ink))g.DrawString("Node: ES | Duration | EF (top), activity no. and item no. (middle), LS | Total float | LF (bottom); days. Red = critical path (zero float). All relations Finish-to-Start through the phase milestones.",font,ink,x,y);}
}
sealed class PdmView:Control {internal PdmNetwork Network;internal PdmView(){DoubleBuffered=true;BackColor=Color.White;Font=new Font("Segoe UI",8f);}
 internal void Fit(){if(Network==null)return;var size=Network.Layout(Math.Max(400,(Parent==null?700:Parent.ClientSize.Height)-60));Width=(int)size.Width+20;Height=(int)size.Height+40;Invalidate();}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);if(Network==null||Network.Nodes.Count==0){TextRenderer.DrawText(e.Graphics,"No activities yet.",Font,new Point(10,10),PowTheme.Muted);return;}
  using(var bold=new Font(Font,FontStyle.Bold)){PdmNetwork.Legend(e.Graphics,10,4,Font);Network.Draw(e.Graphics,0,24,Font,bold);}}}
}
