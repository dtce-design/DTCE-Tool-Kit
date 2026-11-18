using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Imaging;using System.Drawing.Printing;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Construction schedule set for issue (owner, 18 November 2026: "printable", "ready to issue sa construction"): A3 landscape (owner, 18 November 2026: "sa A3 iprint")
// sheets with a border and a title block (project, location, office, sheet title, prepared / checked / approved lines left
// blank for the engineers - nothing is signed or approved by the app, date, sheet n of N). Sheets: summary (durations,
// PERT figures, critical path, basis), bar chart, precedence diagram (PDM), PERT table and PERT-CPM network. Text at least
// 7.5 pt; the User edition carries the DPWH logo watermark like the other reports. Units: hundredths of an inch.
sealed class ScheduleSet {
 internal const float PageW=1654,PageH=1169,Margin=36,Block=84;
 internal static RectangleF Content{get{return new RectangleF(Margin+10,Margin+10,PageW-2*Margin-20,PageH-2*Margin-Block-26);}}
 internal readonly List<ScheduleRow> Rows;internal readonly int Total;internal readonly PdmNetwork Network;internal readonly PertResult Pert;internal readonly ProjectInformation Info;
 internal readonly List<KeyValuePair<string,Action<Graphics,RectangleF,Font,Font>>> Sheets=new List<KeyValuePair<string,Action<Graphics,RectangleF,Font,Font>>>();
 internal ScheduleSet(List<ScheduleRow> rows,ProjectInformation info){Rows=rows;Info=info??new ProjectInformation();Total=rows.Where(r=>r.Days>0).Select(r=>r.Finish).DefaultIfEmpty(0).Max();Network=PdmNetwork.Build(rows);Pert=PertResult.Build(Network);
  var crit=new HashSet<ScheduleRow>(Network.Nodes.Where(n=>n.Critical).Select(n=>n.Row));foreach(var r in rows)r.Critical=crit.Contains(r);
  var c=Content;Sheets.Add(new KeyValuePair<string,Action<Graphics,RectangleF,Font,Font>>("SCHEDULE SUMMARY",DrawSummary));
  /* bar chart: days side by side, lines top to bottom */
  float[] widths={62,228,62,36,52,42};float dayW=17,headH=34;var lines0=ScheduleChart.Lines(rows);float rowH=15;var lines=ScheduleChart.Lines(rows);var daily=ScheduleManpower.Daily(rows);float[] hs;using(var bmp=new Bitmap(1,1)){bmp.SetResolution(100,100);using(var mg=Graphics.FromImage(bmp))using(var mf=new Font("Segoe UI",7.5f)){ScheduleChart.FitDescription(mg,rows,mf,widths,400,ref rowH);hs=ScheduleChart.Heights(mg,lines,mf,widths[1],rowH);float spare=c.Height-ScheduleChart.HeaderHeight(mg,mf,dayW,Math.Max(1,Total),headH)-4-30-hs.Sum();if(spare>0&&lines.Count>0){rowH=Math.Min(22,15+spare/lines.Count);hs=ScheduleChart.Heights(mg,lines,mf,widths[1],rowH);}}}int perDays=Math.Max(5,(int)((c.Width-widths.Sum())/dayW));/* the same number of days on every sheet (no short last sheet) */int daySheets=(int)Math.Ceiling(Math.Max(1,Total)/(double)perDays);perDays=(int)Math.Ceiling(Math.Max(1,Total)/(double)daySheets);
  /* line ranges per sheet by the actual row heights */
  var chunks=new List<KeyValuePair<int,int>>();float hh;using(var bmp=new Bitmap(1,1)){bmp.SetResolution(100,100);using(var mg=Graphics.FromImage(bmp))using(var mf=new Font("Segoe UI",7.5f))hh=ScheduleChart.HeaderHeight(mg,mf,dayW,Math.Max(1,Total),headH);}float avail=c.Height-hh-4-30;/* room for the manpower row */for(int i=0;i<lines.Count;){int j=i;float used=0;while(j<lines.Count&&(j==i||used+hs[j]<=avail)){used+=hs[j];j++;}chunks.Add(new KeyValuePair<int,int>(i,j-i));i=j;}if(chunks.Count==0)chunks.Add(new KeyValuePair<int,int>(0,0));
  for(int d=1;d<=Math.Max(1,Total);d+=perDays)foreach(var chunk in chunks){int from=d,to=Math.Min(Math.Max(1,Total),d+perDays-1),li=chunk.Key,ln=chunk.Value;
   Sheets.Add(new KeyValuePair<string,Action<Graphics,RectangleF,Font,Font>>("BAR CHART - DAYS "+from+" TO "+to,(g,r,f,b)=>ScheduleChart.Draw(g,lines.GetRange(li,ln),r.X,r.Y,widths,dayW,rowH,headH,from,to,f,b,daily)));}
  /* manpower: loading histogram and trade summary, then the activity table */
  Sheets.Add(new KeyValuePair<string,Action<Graphics,RectangleF,Font,Font>>("MANPOWER LOADING",(g,r,f,b)=>{using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted)){g.DrawString(ScheduleManpower.Summary(rows),b,ink,r.X,r.Y);g.DrawString(ScheduleManpower.Basis,f,muted,new RectangleF(r.X,r.Y+15,r.Width,28));}
   float th=(ScheduleManpower.Trades(rows).Count+2)*16+6;float hh2=Math.Max(260,r.Height-50-th-30);ScheduleManpower.DrawHistogram(g,new RectangleF(r.X,r.Y+46,r.Width,hh2),rows,f,b);ScheduleManpower.DrawTrades(g,r.X,r.Y+46+hh2+20,rows,f,b);}));
  int perItems=Math.Max(5,(int)((c.Height-ScheduleManpower.ItemHead-ScheduleManpower.ItemRow-4)/ScheduleManpower.ItemRow));for(int i=0;i<Math.Max(1,rows.Count);i+=perItems){int s0=i,n0=Math.Min(rows.Count,i+perItems)-i;bool last=i+perItems>=rows.Count;
   Sheets.Add(new KeyValuePair<string,Action<Graphics,RectangleF,Font,Font>>("MANPOWER SCHEDULE",(g,r,f,b)=>ScheduleManpower.DrawItems(g,r.X,r.Y,rows,s0,n0,f,b,last)));}
  /* PDM sheets (owner, 18 November 2026: "alisin mo na yung sheet sa continuation", "baka mas okay yung last box sa previous
     sheet uulitin sa next page", "huwag isang drawing, masyadong maliit kapag sa A3"): the network keeps its full size and is
     printed column by column; each continuation sheet starts with the last column of the sheet before it (shaded), so every
     line can be followed from a box that is on both sheets. The sheet edges fall in widened gaps where only straight
     horizontal lines run - no bend, box or text is ever cut by an edge. */
  if(Network.Nodes.Count>0){float legendH=16+PdmNetwork.KeyH;
   Func<List<Window>> pdmWindows=()=>{List<Window> w=null;Network.Widen=new HashSet<int>();
    for(int it=0;it<8;it++){Network.Layout(c.Height-legendH);var gaps=new HashSet<int>();w=Windows(Network.Cuts,Network.Groups,Network.Right,c.Width,gaps);if(gaps.All(Network.Widen.Contains))break;foreach(var i in gaps)Network.Widen.Add(i);}
    return w;};
   float bestGap=130;int fewest=int.MaxValue;foreach(var gap in new float[]{140,130,122,116,110,104}){Network.LinkGap=gap;int count=pdmWindows().Count;if(count<fewest){fewest=count;bestGap=gap;}}Network.LinkGap=bestGap;var pw=pdmWindows();PdmWindows=pw;
   var pdmEdges=Edges(pw);
   for(int i=0;i<pw.Count;i++){var win=pw[i];int n=pw.Count;
    Sheets.Add(new KeyValuePair<string,Action<Graphics,RectangleF,Font,Font>>("PRECEDENCE DIAGRAM (PDM)"+(n>1?" - "+(i+1)+" OF "+n:""),(g,r,f,b)=>{using(var ink=new SolidBrush(PowTheme.Muted))g.DrawString(PdmNetwork.LegendText,f,ink,r.X,r.Y);PdmNetwork.DrawKey(g,r.X,r.Y+16,f,b);
     var area=new RectangleF(r.X,r.Y+legendH,Math.Min(r.Width,win.X1-win.X0),r.Height-legendH);Repeated(g,area,win);var st=g.Save();g.SetClip(area);Network.Draw(g,r.X-win.X0,area.Y,f,b,null,pdmEdges);g.Restore(st);}));}
  /* PERT table */
   int perRows=Math.Max(5,(int)((c.Height-60)/15));for(int i=0;i<Pert.Activities.Count;i+=perRows){int s=i,n=Math.Min(Pert.Activities.Count,i+perRows)-i;Sheets.Add(new KeyValuePair<string,Action<Graphics,RectangleF,Font,Font>>("PERT TIME ESTIMATES",(g,r,f,b)=>DrawPertTable(g,r,f,b,s,n)));}
  /* PERT-CPM network: activity on arrow, printed column by column in the same way (the last event column of a sheet is
     repeated, shaded, at the start of the next) */
   var aoa=PertAoa.Build(Network,Pert);float aoaH=16+PertAoa.KeyH;
   Func<List<Window>> aoaWindows=()=>{aoa.Layout(c.Height-aoaH);return Windows(aoa.Cuts,aoa.Groups,aoa.Right,c.Width,new HashSet<int>(),false);/* the arrow labels run across the gaps, so the sheet starts right after the event column before */};
   float bestW=250;int fewestP=int.MaxValue;foreach(var w in new float[]{260,250,236,224,212}){aoa.LevelW=w;int count=aoaWindows().Count;if(count<fewestP){fewestP=count;bestW=w;}}aoa.LevelW=bestW;var aw=aoaWindows();PertWindows=aw;
   var pertEdges=Edges(aw);
   for(int i=0;i<aw.Count;i++){var win=aw[i];int n=aw.Count;
    Sheets.Add(new KeyValuePair<string,Action<Graphics,RectangleF,Font,Font>>("PERT-CPM NETWORK"+(n>1?" - "+(i+1)+" OF "+n:""),(g,r,f,b)=>{using(var ink=new SolidBrush(PowTheme.Muted))g.DrawString(PertAoa.LegendText,f,ink,r.X,r.Y);PertAoa.DrawKey(g,r.X,r.Y+16,f,b);
     var area=new RectangleF(r.X,r.Y+aoaH,Math.Min(r.Width,win.X1-win.X0),r.Height-aoaH);Repeated(g,area,win);var st=g.Save();g.SetClip(area);aoa.Draw(g,r.X-win.X0,area.Y,f,b,pertEdges);g.Restore(st);}));}
   Aoa=aoa;}}
 /* part of the drawing shown on one sheet: X0..X1; on a continuation sheet X0..Shade is the repeated last column */
 internal sealed class Window {internal float X0,X1,ShadeFrom=float.NaN,ShadeTo=float.NaN;}
 internal List<Window> PdmWindows=new List<Window>(),PertWindows=new List<Window>();
 /* sheets made of whole column groups: group k spans start(k)..end(k); a continuation sheet starts with the last group of the
    sheet before; gaps = the gaps that hold a sheet edge (they are widened so that only straight lines cross them) */
 static List<Window> Windows(List<float> cuts,List<float[]> groupX,float right,float width,HashSet<int> gaps,bool tight=true){var list=new List<Window>();int groups=groupX.Count;if(groups<=0)return list;
  /* a continuation sheet starts just clear of the bends in front of its repeated group (half way between the cut before it
     and the group), so little room is spent on the repeat */
  Func<int,float> start=k=>k==0?2:tight?Math.Max(cuts[k-1],(cuts[k-1]+groupX[k][0])/2-10):cuts[k-1];Func<int,float> end=k=>k>=groups-1?right:cuts[k];int a=0;
  while(true){int b=Math.Min(groups-1,a+1);if(a==0&&end(0)-start(0)>width)b=0;while(b+1<=groups-1&&end(b+1)-start(a)<=width)b++;
   var w=new Window{X0=start(a),X1=end(b)};if(a>0){w.ShadeFrom=groupX[a][0]-10;w.ShadeTo=groupX[a][1]+10;gaps.Add(a-1);gaps.Add(a);}if(b<groups-1)gaps.Add(b);list.Add(w);
   if(b>=groups-1)break;a=Math.Max(b,a+1);}
  return list;}
 /* every sheet edge inside the drawing (the routing keeps bends and labels away from them) */
 static List<float> Edges(List<Window> w){var e=new List<float>();for(int i=0;i<w.Count;i++){if(i>0)e.Add(w[i].X0);if(i+1<w.Count)e.Add(w[i].X1);}return e.Distinct().OrderBy(x=>x).ToList();}
 /* the repeated column of a continuation sheet: a light band, so it is clear which boxes were already on the sheet before */
 static void Repeated(Graphics g,RectangleF area,Window w){if(float.IsNaN(w.ShadeFrom))return;float x0=area.X+Math.Max(0,w.ShadeFrom-w.X0),x1=area.X+Math.Min(area.Width,w.ShadeTo-w.X0);
  using(var band=new SolidBrush(Color.FromArgb(234,238,244)))g.FillRectangle(band,x0,area.Y,x1-x0,area.Height);}
 /* drawing checks (owner, 18 November 2026: "nagkaroon na ng overlapping ng text", "yung diagram continuity ang need ko"):
    no label overlaps another label or a node / event, no label is split by a sheet join, and every line crosses a join
    horizontally (a bend never falls on a join), so the sheets laid side by side read as one continuous diagram */
 internal List<string> DrawingProblems(){var problems=new List<string>();
  using(var bmp=new Bitmap(1,1)){bmp.SetResolution(100,100);using(var g=Graphics.FromImage(bmp))using(var f=new Font("Segoe UI",7.5f))using(var b=new Font("Segoe UI",7.5f,FontStyle.Bold)){
   if(Network.Nodes.Count>0){var inner=Edges(PdmWindows);var plan=Network.MakePlan(g,f,inner);
    Check("PDM",plan.Labels.Select(k=>k.Rect).ToList(),plan.Boxes,plan.Lines.Select(k=>k.Key).ToList(),inner,problems,false);}
   if(Aoa!=null){var inner=Edges(PertWindows);var plan=Aoa.MakePlan(g,f,b,inner);
    foreach(var l in plan.Labels.Where(k=>k.Truncated))problems.Add("PERT: label cut short: "+l.Text);
    Check("PERT",plan.Labels.Select(k=>k.Rect).ToList(),plan.Boxes,plan.Lines.Select(k=>k.Key).ToList(),inner,problems,true);}}}
  return problems;}
 static bool Crosses(PointF p,PointF q,RectangleF r){if(Math.Abs(p.Y-q.Y)<0.01f){return p.Y>r.Top&&p.Y<r.Bottom&&Math.Max(p.X,q.X)>r.Left&&Math.Min(p.X,q.X)<r.Right;}if(Math.Abs(p.X-q.X)<0.01f){return p.X>r.Left&&p.X<r.Right&&Math.Max(p.Y,q.Y)>r.Top&&Math.Min(p.Y,q.Y)<r.Bottom;}
  for(int k=0;k<=20;k++){float t=k/20f;var x=p.X+(q.X-p.X)*t;var y=p.Y+(q.Y-p.Y)*t;if(r.Contains(x,y))return true;}return false;}
 static bool CrossesCircle(PointF p,PointF q,RectangleF c){float cx=c.X+c.Width/2,cy=c.Y+c.Height/2,r=c.Width/2-2;float vx=q.X-p.X,vy=q.Y-p.Y;float len2=vx*vx+vy*vy;float t=len2<1e-6f?0:Math.Max(0,Math.Min(1,((cx-p.X)*vx+(cy-p.Y)*vy)/len2));float dx=p.X+vx*t-cx,dy=p.Y+vy*t-cy;return dx*dx+dy*dy<r*r;}
 static void Check(string what,List<RectangleF> labels,List<RectangleF> boxes,List<PointF[]> lines,List<float> joins,List<string> problems,bool circles){
  for(int i=0;i<labels.Count;i++){var a=RectangleF.Inflate(labels[i],-0.5f,-0.5f);for(int j=i+1;j<labels.Count;j++)if(a.IntersectsWith(labels[j]))problems.Add(what+": labels overlap at x "+(int)a.X+", y "+(int)a.Y);
   foreach(var bx in boxes)if(a.IntersectsWith(bx))problems.Add(what+": label over a node at x "+(int)a.X+", y "+(int)a.Y);
   foreach(var x in joins)if(a.Left<x&&a.Right>x)problems.Add(what+": label cut by a sheet edge at x "+(int)x);
   var inner=RectangleF.Inflate(labels[i],-1.5f,-1.5f);foreach(var pl in lines)for(int k=0;k+1<pl.Length;k++)if(Crosses(pl[k],pl[k+1],inner)){problems.Add(what+": line through a label at x "+(int)a.X+", y "+(int)a.Y);goto nextLabel;}
   nextLabel:;}
  foreach(var bx in boxes){var inner=RectangleF.Inflate(bx,-2,-2);foreach(var pl in lines)for(int k=0;k+1<pl.Length;k++)if(circles?CrossesCircle(pl[k],pl[k+1],bx):Crosses(pl[k],pl[k+1],inner)){problems.Add(what+": line through a node at x "+(int)bx.X+", y "+(int)bx.Y);goto nextBox;}
   nextBox:;}
  foreach(var x in joins){foreach(var bx in boxes)if(bx.Left<x&&bx.Right>x)problems.Add(what+": node cut by a sheet edge at x "+(int)x);
   foreach(var pl in lines)for(int k=0;k+1<pl.Length;k++){var p=pl[k];var q=pl[k+1];if(Math.Min(p.X,q.X)<=x+8&&Math.Max(p.X,q.X)>=x-8&&Math.Abs(p.Y-q.Y)>0.5f)problems.Add(what+": line bends at a sheet edge at x "+(int)x);}}}
 /* the arrow drawing, kept for the tests */
 internal PertAoa Aoa;
 void DrawSummary(Graphics g,RectangleF r,Font f,Font b){using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted))using(var red=new SolidBrush(Color.FromArgb(192,40,40)))using(var big=new Font("Segoe UI",14f,FontStyle.Bold))using(var mid=new Font("Segoe UI",9.5f,FontStyle.Bold))using(var grid=new Pen(Color.FromArgb(190,198,206),0.6f)){
  float y=r.Y;g.DrawString("CONSTRUCTION SCHEDULE",big,ink,r.X,y);y+=30;
  string[,] facts={{"Project duration (CPM)",Total+" working days"},{"Working time","8 hours per day; working days only (no Sundays, holidays or unworkable days)"},{"Scheduled pay items",Network.Nodes.Count(n=>!n.IsMilestone)+" activities, "+Network.Links.Count+" relations (FS / SS / FF with lags)"+(Network.Nodes.Any(n=>n.IsMilestone)?", "+Network.Nodes.Count(n=>n.IsMilestone)+" trade milestones":"")},
   {"PERT expected time Te",Pert.Te.ToString("0.0",CultureInfo.InvariantCulture)+" working days  (standard deviation "+Pert.Sigma.ToString("0.00",CultureInfo.InvariantCulture)+" days)"},{"Completion at 90% confidence",Pert.P90+" working days"},{"Completion at 95% confidence",Pert.P95+" working days"},{"Manpower (DUPA crews)",ScheduleManpower.Summary(Rows)}};
  for(int i=0;i<facts.GetLength(0);i++){g.DrawString(facts[i,0],b,ink,r.X,y);g.DrawString(facts[i,1],f,ink,r.X+210,y);y+=16;}y+=10;
  g.DrawString("CRITICAL PATH",mid,red,r.X,y);y+=18;float[] w={40,80,420,60,60,60};string[] h={"No.","Item no.","Description","Days","Start day","Finish day"};float x=r.X;for(int i=0;i<h.Length;i++){g.DrawString(h[i],b,ink,x,y);x+=w[i];}y+=15;g.DrawLine(grid,r.X,y,r.X+w.Sum(),y);y+=2;
  foreach(var nd in Network.Nodes.Where(n=>n.Critical&&!n.IsMilestone)){if(y>r.Bottom-140)break;x=r.X;string[] v={nd.Id.ToString(CultureInfo.InvariantCulture),nd.Row.Code,nd.Row.Description,nd.Row.Days.ToString(CultureInfo.InvariantCulture),nd.Row.Start.ToString(CultureInfo.InvariantCulture),nd.Row.Finish.ToString(CultureInfo.InvariantCulture)};for(int i=0;i<v.Length;i++){g.DrawString(v[i],f,ink,new RectangleF(x,y,w[i]-6,14),new StringFormat{Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap});x+=w[i];}y+=14;}
  y=Math.Max(y+12,r.Bottom-130);g.DrawString("BASIS AND NOTES",mid,ink,r.X,y);y+=18;
  string[] notes={"1. Durations: quantity from the Bill of Quantities / (output per hour from the DUPA x 8 hours x crews), rounded up to whole working days.",
   "2. Logic: each item follows the trades it depends on - clearing, excavation, lean concrete, rebar, formworks (overlapping the rebar), footing and frame concrete, backfill, soil poisoning, gravel, slab on fill, masonry, steel (7 days after the frame: curing), roofing, plaster, ceiling, tiles, doors, painting - with FS / SS / FF relations and lags (see PDM). Non-critical items start within their float on the day that keeps the daily manpower level; the duration is not changed.",
   "3. PERT: optimistic a = 0.8 m, most likely m = activity duration, pessimistic b = 1.2 m; te = (a + 4m + b) / 6; variance = ((b - a) / 6)^2; normal distribution for the confidence levels.",
   "4. Lump-sum (LS) pay items are listed in the bar chart with the output per hour left blank (no duration yet); time-based (per month) pay items are not scheduled. Calendar days, Sundays, holidays and unworkable days are not included.",
   "5. PDM / CPM times follow the standard convention: ES of the first activity = 0, EF = ES + D, LS = LF - D, TF = LS - ES; bar chart day n = the n-th working day; the bar chart shows the levelled start (between ES + 1 and LS + 1).",
   "6. Manpower: "+ScheduleManpower.Basis};
  foreach(var n in notes){g.DrawString(n,f,muted,new RectangleF(r.X,y,r.Width,26));y+=17;}}}
 void DrawPertTable(Graphics g,RectangleF r,Font f,Font b,int start,int count){float[] w={36,80,330,250,56,60,56,52,60,44,44,52};string[] h={"No.","Item no.","Description","Predecessors","a (opt.)","m (likely)","b (pess.)","te","Variance","ES","EF","Critical"};
  using(var ink=new SolidBrush(PowTheme.Ink))using(var red=new SolidBrush(Color.FromArgb(192,40,40)))using(var head=new SolidBrush(Color.FromArgb(236,240,244)))using(var grid=new Pen(Color.FromArgb(205,211,218),0.5f)){var near=new StringFormat{LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap};var far=new StringFormat(near){Alignment=StringAlignment.Far};
   g.FillRectangle(head,r.X,r.Y,w.Sum(),22);float x=r.X;for(int i=0;i<h.Length;i++){g.DrawString(h[i],b,ink,new RectangleF(x+3,r.Y,w[i]-6,22),i>=4&&i<=10?far:near);x+=w[i];}float y=r.Y+22;
   foreach(var a in Pert.Activities.Skip(start).Take(count)){var nd=a.Node;string[] v={nd.Id.ToString(CultureInfo.InvariantCulture),nd.Row.Code,nd.Row.Description,nd.Row.Predecessors==""?"START":nd.Row.Predecessors,a.A.ToString("0.0",CultureInfo.InvariantCulture),a.M.ToString("0.0",CultureInfo.InvariantCulture),a.B.ToString("0.0",CultureInfo.InvariantCulture),a.Te.ToString("0.0",CultureInfo.InvariantCulture),a.Variance.ToString("0.00",CultureInfo.InvariantCulture),nd.ES.ToString(CultureInfo.InvariantCulture),nd.EF.ToString(CultureInfo.InvariantCulture),nd.Critical?"Yes":""};
    x=r.X;for(int i=0;i<v.Length;i++){g.DrawString(v[i],f,nd.Critical&&i==11?(Brush)red:ink,new RectangleF(x+3,y,w[i]-6,15),i>=4&&i<=10?far:near);x+=w[i];}y+=15;g.DrawLine(grid,r.X,y,r.X+w.Sum(),y);}
   x=r.X;for(int i=0;i<=w.Length;i++){g.DrawLine(grid,x,r.Y,x,y);if(i<w.Length)x+=w[i];}g.DrawLine(grid,r.X,r.Y,r.X+w.Sum(),r.Y);
   if(start+count>=Pert.Activities.Count)g.DrawString("Project: Te = "+Pert.Te.ToString("0.0",CultureInfo.InvariantCulture)+" days, standard deviation = "+Pert.Sigma.ToString("0.00",CultureInfo.InvariantCulture)+" days (activities driving the finish), 90% = "+Pert.P90+" days, 95% = "+Pert.P95+" days.",b,ink,r.X,y+8);}}
 internal void DrawSheet(Graphics g,int index){using(var f=new Font("Segoe UI",7.5f))using(var b=new Font("Segoe UI",7.5f,FontStyle.Bold)){Watermark(g);var c=Content;Sheets[index].Value(g,c,f,b);Frame(g,index,f,b);}}
 void Frame(Graphics g,int index,Font f,Font b){using(var pen=new Pen(PowTheme.Ink,1.2f))using(var thin=new Pen(PowTheme.Ink,0.6f))using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted))using(var head=new Font("Segoe UI",10f,FontStyle.Bold)){
  var outer=new RectangleF(Margin,Margin,PageW-2*Margin,PageH-2*Margin);g.DrawRectangle(pen,outer.X,outer.Y,outer.Width,outer.Height);float top=outer.Bottom-Block;g.DrawLine(pen,outer.X,top,outer.Right,top);
  float[] w={0.38f,0.24f,0.24f,0.14f};float x=outer.X;var cells=new List<RectangleF>();foreach(var k in w){var cell=new RectangleF(x,top,outer.Width*k,Block);cells.Add(cell);x+=cell.Width;if(x<outer.Right-1)g.DrawLine(thin,x,top,x,outer.Bottom);}
  g.DrawString("PROJECT",b,muted,cells[0].X+6,top+4);g.DrawString(Info.Name==""?"(project name not set)":Info.Name,b,ink,new RectangleF(cells[0].X+6,top+16,cells[0].Width-12,30));g.DrawString("Location: "+(Info.Location==""?"-":Info.Location),f,ink,cells[0].X+6,top+46);if(Info.Office!="")g.DrawString(Info.Office,f,ink,new RectangleF(cells[0].X+6,top+58,cells[0].Width-12,14));
  g.DrawString("SHEET TITLE",b,muted,cells[1].X+6,top+4);g.DrawString("CONSTRUCTION SCHEDULE",head,ink,cells[1].X+6,top+18);g.DrawString(Sheets[index].Key,b,ink,new RectangleF(cells[1].X+6,top+40,cells[1].Width-12,30));
  string[] sign={"Prepared by:","Checked by:","Approved by:"};for(int i=0;i<3;i++){float y=top+6+i*22;g.DrawString(sign[i],f,ink,cells[2].X+6,y);g.DrawLine(thin,cells[2].X+70,y+12,cells[2].Right-8,y+12);}
  g.DrawString("DATE",b,muted,cells[3].X+6,top+4);g.DrawString(DateTime.Now.ToString("dd MMMM yyyy",CultureInfo.InvariantCulture),f,ink,cells[3].X+6,top+16);g.DrawString("SHEET",b,muted,cells[3].X+6,top+36);g.DrawString((index+1)+" of "+Sheets.Count,head,ink,cells[3].X+6,top+48);}}
 static Image logo;
 static void Watermark(Graphics g){var tiles=ReportWatermarkPattern.Active(PageW*0.72,PageH*0.72).ToList();if(tiles.Count==0)return;if(logo==null){using(var s=typeof(ScheduleSet).Assembly.GetManifestResourceStream("DpwhLogo.png")){if(s==null)return;logo=Image.FromStream(s);}}
  /* lighter than the other reports (owner, 18 November 2026: "medyo lighter watermark") */var cm=new ColorMatrix{Matrix33=(float)(ReportWatermarkPattern.Opacity*0.55)};using(var ia=new ImageAttributes()){ia.SetColorMatrix(cm);float k=100f/72f;foreach(var t in tiles){float x=(float)t[0]*k,y=(float)t[1]*k,s=(float)t[2]*k;if(x>PageW||y>PageH||x+s<0||y+s<0)continue;g.DrawImage(logo,new Rectangle((int)x,(int)y,(int)s,(int)s),0,0,logo.Width,logo.Height,GraphicsUnit.Pixel,ia);}}}
}
partial class ProgramOfWorksForm {
 void PrintSchedule(bool preview){Guard();var rows=ScheduleRows();if(rows.Count==0){MessageBox.Show(this,"No pay item quantities to schedule yet.","Construction Schedule",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
  var set=new ScheduleSet(rows,ProjectInfoStore.Read());
  using(var doc=new PrintDocument{DocumentName="Construction Schedule"}){doc.DefaultPageSettings.Landscape=true;var a3=doc.PrinterSettings.PaperSizes.Cast<PaperSize>().FirstOrDefault(p=>p.Kind==PaperKind.A3);doc.DefaultPageSettings.PaperSize=a3??new PaperSize("A3",1169,1654);doc.DefaultPageSettings.Margins=new Margins(0,0,0,0);doc.OriginAtMargins=false;
   int page=0;doc.BeginPrint+=(s,e)=>{page=0;};
   doc.PrintPage+=(s,e)=>{/* sheet drawn in hundredths of an inch from the paper corner, scaled to the printable page */float sx=e.PageBounds.Width/ScheduleSet.PageW,sy=e.PageBounds.Height/ScheduleSet.PageH;var st=e.Graphics.Save();e.Graphics.TranslateTransform(-e.PageSettings.HardMarginX,-e.PageSettings.HardMarginY);e.Graphics.ScaleTransform(Math.Min(sx,sy),Math.Min(sx,sy));set.DrawSheet(e.Graphics,page);e.Graphics.Restore(st);page++;e.HasMorePages=page<set.Sheets.Count;};
   if(preview){using(var dialog=new PrintPreviewDialog{Document=doc,Width=1200,Height=850,StartPosition=FormStartPosition.CenterParent}){dialog.ShowDialog(this);}}
   else{using(var dialog=new PrintDialog{Document=doc,UseEXDialog=true}){if(dialog.ShowDialog(this)==DialogResult.OK)doc.Print();}}}
  RefreshSchedule();}
}
}
