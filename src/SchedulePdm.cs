using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Precedence Diagram Method (owner, 18 November 2026: "idagdag ang PDM", "gandahan mo naman yung presentation ng PDM").
// Activities are the scheduled pay items (Bill of Quantities order = activity no.). Relations are Finish-to-Start through a
// zero-duration milestone at the end of each work phase - the same automatic sequence as the bar chart. Forward pass: ES /
// EF; backward pass: LS / LF; total float = LF - EF; zero float = critical (red).
// Standard layout (owner, 18 November 2026: "use standard layouting"): activity-on-node, times in the textbook
// convention (ES of the first activity = 0, EF = ES + D, LS = LF - D, TF = LS - ES), nodes laid out left to right
// from the START node through one milestone node per phase (zero duration) to the FINISH node, every relation drawn as its
// own orthogonal arrow from the right side of the predecessor to the left side of the successor. Node key: ES | D | EF,
// activity no. and item no., description, LS | TF | LF. Units are hundredths of an inch on paper (pixels on screen).
sealed class PdmNode {internal ScheduleRow Row;internal int Id,ES,EF,LS,LF,TF;internal bool Critical{get{return TF==0;}}internal float X,Y;}
sealed class PdmMilestone {internal string Name,Caption;internal int Day;internal float X,Y;}
sealed class PdmPhaseBox {internal string Name;internal float Left,Right,BusIn,BusOut;}
sealed class PdmNetwork {
 internal readonly List<PdmNode> Nodes=new List<PdmNode>();internal readonly List<PdmMilestone> Milestones=new List<PdmMilestone>();internal readonly List<List<PdmNode>> Phases=new List<List<PdmNode>>();internal readonly List<PdmPhaseBox> Boxes=new List<PdmPhaseBox>();internal int Total;internal float Height;
 internal const float NodeW=170,NodeH=64,RowGap=22,ColGap=46,MileD=NodeW,Head=10,LinkGap=40;
 internal static PdmNetwork Build(List<ScheduleRow> rows){var n=new PdmNetwork();int id=0;var nodes=new List<PdmNode>();
  foreach(var r in rows){id++;if(r.Days<=0)continue;nodes.Add(new PdmNode{Row=r,Id=id,ES=r.Start-1,EF=r.Finish});}
  foreach(var phase in nodes.GroupBy(x=>x.Row.PhaseIndex).OrderBy(p=>p.Key)){var list=phase.ToList();int finish=list.Max(x=>x.EF);foreach(var x in list){x.LF=finish;x.LS=x.LF-x.Row.Days;x.TF=x.LF-x.EF;}n.Phases.Add(list);}
  n.Nodes.AddRange(nodes);n.Total=nodes.Count==0?0:nodes.Max(x=>x.EF);
  n.Milestones.Add(new PdmMilestone{Name="START",Caption="Start of project",Day=0});foreach(var p in n.Phases){int k=n.Phases.IndexOf(p)+1;n.Milestones.Add(new PdmMilestone{Name=p==n.Phases.Last()?"FINISH":"M"+k,Caption=p==n.Phases.Last()?"Project completion":"End of phase "+k+" - "+p[0].Row.Phase,Day=p.Max(x=>x.EF)});}return n;}
 internal SizeF Layout(float height){Height=height;float step=NodeH+RowGap;int perCol=Math.Max(1,(int)((height-Head-NodeH/2-10)/step));float x=6;Boxes.Clear();float mid=Head+(height-Head)/2;
  Milestones[0].X=x;x+=NodeW+LinkGap;
  for(int p=0;p<Phases.Count;p++){var list=Phases[p];int cols=(int)Math.Ceiling(list.Count/(double)perCol);var box=new PdmPhaseBox{Name="Phase "+(p+1)+" - "+list[0].Row.Phase,Left=x,BusIn=x};
   int rows=Math.Min(list.Count,perCol);float blockH=rows*step-RowGap+(cols>1?step/2:0);float top=Math.Max(Head+6,mid-blockH/2);
   for(int i=0;i<list.Count;i++){int c=i/perCol,r=i%perCol;list[i].X=x+c*(NodeW+ColGap);list[i].Y=top+r*step+(c%2==1?step/2:0);}
   x+=cols*(NodeW+ColGap)-ColGap;box.BusOut=x;box.Right=x;Boxes.Add(box);x+=LinkGap;Milestones[p+1].X=x;x+=NodeW+LinkGap;}
  foreach(var m in Milestones)m.Y=mid-NodeH/2;return new SizeF(x-LinkGap+6,height);}
 internal void Draw(Graphics g,float dx,float dy,Font font,Font bold){var smooth=g.SmoothingMode;g.SmoothingMode=SmoothingMode.AntiAlias;
  var mid=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap};var left=new StringFormat(mid){Alignment=StringAlignment.Near};
  Color redC=Color.FromArgb(192,40,40),line=Color.FromArgb(120,130,142);
  using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted))using(var red=new SolidBrush(redC))using(var bandA=new SolidBrush(Color.FromArgb(246,248,250)))using(var bandB=new SolidBrush(Color.FromArgb(238,242,246)))
  using(var bus=new Pen(line,1f))using(var stub=new Pen(line,1f){CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)})using(var critStub=new Pen(redC,1.6f){CustomEndCap=new AdjustableArrowCap(3.5f,4.5f)})using(var edge=new Pen(Color.FromArgb(70,78,88),0.9f))using(var critEdge=new Pen(redC,1.8f))
  using(var cellCrit=new SolidBrush(Color.FromArgb(251,233,231)))using(var cell=new SolidBrush(Color.FromArgb(234,238,242)))using(var white=new SolidBrush(Color.White))using(var grid=new Pen(Color.FromArgb(180,188,198),0.6f)){
   /* relations: one orthogonal arrow per link, bending halfway between the two nodes */
   Action<float,float,float,float,bool> link=(x1,y1,x2,y2,crit)=>{float bx=x2-ColGap/2+6;if(bx<x1+8)bx=(x1+x2)/2;var path=new[]{new PointF(x1,y1),new PointF(bx,y1),new PointF(bx,y2),new PointF(x2,y2)};g.DrawLines(crit?critStub:stub,path);};
   for(int p=0;p<Phases.Count;p++){var a=Milestones[p];var z=Milestones[p+1];float ay=dy+a.Y+NodeH/2,zy=dy+z.Y+NodeH/2;
    foreach(var nd in Phases[p].Where(n=>!n.Critical).Concat(Phases[p].Where(n=>n.Critical))){float cy=dy+nd.Y+NodeH/2;
     g.DrawLines(nd.Critical?critEdge:bus,new[]{new PointF(dx+a.X+NodeW,ay),new PointF(dx+a.X+NodeW+LinkGap/2,ay),new PointF(dx+a.X+NodeW+LinkGap/2,cy)});link(dx+a.X+NodeW+LinkGap/2,cy,dx+nd.X,cy,nd.Critical);
     g.DrawLines(nd.Critical?critEdge:bus,new[]{new PointF(dx+nd.X+NodeW,cy),new PointF(dx+z.X-LinkGap/2,cy),new PointF(dx+z.X-LinkGap/2,zy)});link(dx+z.X-LinkGap/2,zy,dx+z.X,zy,nd.Critical);}}
   foreach(var m in Milestones){var r=new RectangleF(dx+m.X,dy+m.Y,NodeW,NodeH);float rh=NodeH/4,cw=NodeW/3;
    g.FillRectangle(white,r);g.FillRectangle(cellCrit,r.X,r.Y,r.Width,rh);g.FillRectangle(cellCrit,r.X,r.Bottom-rh,r.Width,rh);
    g.DrawLine(grid,r.X,r.Y+rh,r.Right,r.Y+rh);g.DrawLine(grid,r.X,r.Bottom-rh,r.Right,r.Bottom-rh);for(int c=1;c<3;c++){g.DrawLine(grid,r.X+c*cw,r.Y,r.X+c*cw,r.Y+rh);g.DrawLine(grid,r.X+c*cw,r.Bottom-rh,r.X+c*cw,r.Bottom);}
    string d=m.Day.ToString(CultureInfo.InvariantCulture);string[] tv={d,"0",d},bv={d,"0",d};for(int c=0;c<3;c++){g.DrawString(tv[c],font,ink,new RectangleF(r.X+c*cw,r.Y,cw,rh),mid);g.DrawString(bv[c],font,ink,new RectangleF(r.X+c*cw,r.Bottom-rh,cw,rh),mid);}
    g.DrawString(m.Name+"  (milestone)",bold,red,new RectangleF(r.X+4,r.Y+rh,NodeW-8,rh),left);g.DrawString(m.Caption,font,muted,new RectangleF(r.X+4,r.Y+2*rh,NodeW-8,rh),left);
    g.DrawRectangle(critEdge,r.X,r.Y,r.Width,r.Height);}
   foreach(var nd in Nodes){var r=new RectangleF(dx+nd.X,dy+nd.Y,NodeW,NodeH);float rh=NodeH/4,cw=NodeW/3;var fill=nd.Critical?(Brush)cellCrit:cell;
    g.FillRectangle(white,r);g.FillRectangle(fill,r.X,r.Y,r.Width,rh);g.FillRectangle(fill,r.X,r.Bottom-rh,r.Width,rh);
    g.DrawLine(grid,r.X,r.Y+rh,r.Right,r.Y+rh);g.DrawLine(grid,r.X,r.Bottom-rh,r.Right,r.Bottom-rh);for(int c=1;c<3;c++){g.DrawLine(grid,r.X+c*cw,r.Y,r.X+c*cw,r.Y+rh);g.DrawLine(grid,r.X+c*cw,r.Bottom-rh,r.X+c*cw,r.Bottom);}
    string[] top={nd.ES.ToString(CultureInfo.InvariantCulture),nd.Row.Days.ToString(CultureInfo.InvariantCulture),nd.EF.ToString(CultureInfo.InvariantCulture)},bottom={nd.LS.ToString(CultureInfo.InvariantCulture),nd.TF.ToString(CultureInfo.InvariantCulture),nd.LF.ToString(CultureInfo.InvariantCulture)};
    for(int c=0;c<3;c++){g.DrawString(top[c],font,ink,new RectangleF(r.X+c*cw,r.Y,cw,rh),mid);g.DrawString(bottom[c],font,ink,new RectangleF(r.X+c*cw,r.Bottom-rh,cw,rh),mid);}
    g.DrawString(nd.Id+"   "+nd.Row.Code,bold,nd.Critical?(Brush)red:ink,new RectangleF(r.X+4,r.Y+rh,NodeW-8,rh),left);g.DrawString(nd.Row.Description,font,muted,new RectangleF(r.X+4,r.Y+2*rh,NodeW-8,rh),left);
    g.DrawRectangle(nd.Critical?critEdge:edge,r.X,r.Y,r.Width,r.Height);}}
  g.SmoothingMode=smooth;}
 internal const string LegendText="Activity on node, all relations Finish-to-Start (FS), times in working days (ES of the first activity = 0; EF = ES + D; LS = LF - D; TF = LS - ES). Red = critical path (TF = 0). M1, M2, ... = end-of-phase milestones (D = 0).";
 internal const float KeyH=NodeH+14;
 /* standard node key, drawn once at the top of each sheet */
 internal static void DrawKey(Graphics g,float x,float y,Font font,Font bold){var mid=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,FormatFlags=StringFormatFlags.NoWrap};var left=new StringFormat(mid){Alignment=StringAlignment.Near};
  using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted))using(var cell=new SolidBrush(Color.FromArgb(234,238,242)))using(var grid=new Pen(Color.FromArgb(180,188,198),0.6f))using(var edge=new Pen(Color.FromArgb(70,78,88),0.9f)){
   g.DrawString("NODE KEY",bold,ink,x,y);var r=new RectangleF(x+70,y,NodeW,NodeH);float rh=NodeH/4,cw=NodeW/3;g.FillRectangle(cell,r.X,r.Y,r.Width,rh);g.FillRectangle(cell,r.X,r.Bottom-rh,r.Width,rh);
   g.DrawLine(grid,r.X,r.Y+rh,r.Right,r.Y+rh);g.DrawLine(grid,r.X,r.Bottom-rh,r.Right,r.Bottom-rh);for(int c=1;c<3;c++){g.DrawLine(grid,r.X+c*cw,r.Y,r.X+c*cw,r.Y+rh);g.DrawLine(grid,r.X+c*cw,r.Bottom-rh,r.X+c*cw,r.Bottom);}
   string[] t={"ES","D","EF"},b2={"LS","TF","LF"};for(int c=0;c<3;c++){g.DrawString(t[c],bold,ink,new RectangleF(r.X+c*cw,r.Y,cw,rh),mid);g.DrawString(b2[c],bold,ink,new RectangleF(r.X+c*cw,r.Bottom-rh,cw,rh),mid);}
   g.DrawString("Activity no.   Item no.",bold,ink,new RectangleF(r.X+4,r.Y+rh,NodeW-8,rh),left);g.DrawString("Description",font,muted,new RectangleF(r.X+4,r.Y+2*rh,NodeW-8,rh),left);g.DrawRectangle(edge,r.X,r.Y,r.Width,r.Height);
   string[] k={"ES = early start, EF = early finish","LS = late start, LF = late finish","D = duration (working days)","TF = total float = LS - ES"};for(int i=0;i<k.Length;i++)g.DrawString(k[i],font,ink,r.Right+14,y+2+i*15);}}
}
sealed class PdmView:Control {internal PdmNetwork Network;internal PdmView(){DoubleBuffered=true;BackColor=Color.White;Font=new Font("Segoe UI",8f);}
 internal void Fit(){if(Network==null)return;var size=Network.Layout(Math.Max(500,(Parent==null?700:Parent.ClientSize.Height)-50-PdmNetwork.KeyH));Width=Math.Max(900,(int)size.Width+20);Height=(int)(size.Height+30+PdmNetwork.KeyH);Invalidate();}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);if(Network==null||Network.Nodes.Count==0){TextRenderer.DrawText(e.Graphics,"No activities yet.",Font,new Point(10,10),PowTheme.Muted);return;}
  using(var bold=new Font(Font,FontStyle.Bold)){TextRenderer.DrawText(e.Graphics,PdmNetwork.LegendText,Font,new Point(8,4),PowTheme.Muted);PdmNetwork.DrawKey(e.Graphics,8,24,Font,bold);Network.Draw(e.Graphics,0,24+PdmNetwork.KeyH,Font,bold);}}}
}
