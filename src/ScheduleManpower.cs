using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Manpower (owner, 18 November 2026: "can you determine the manpower needed for this?"). Each scheduled pay item uses the
// labor crew of its DUPA (Output per Hour Summary, column Crew, e.g. "Foreman x1; Skilled Laborer x2; Unskilled Laborer
// x2") times its number of crews, on every working day of its (levelled) schedule. Workers of an item = the crew without
// its foreman; daily manpower = the workers of the items working that day plus the foremen needed to supervise them, one
// foreman per 15 workers or part of it (owner, 18 November 2026: "makatotohanan" - not one foreman for every small item).
// Man-days of an item = workers per day x days. Items without a DUPA crew (LS, items without output per hour) are not
// counted and are listed as such.
sealed class ManpowerTrade {internal string Trade;internal int Peak,ManDays;internal double Average;}
static class ScheduleManpower {
 static readonly CultureInfo IC=CultureInfo.InvariantCulture;
 /* "Foreman x1; Skilled Laborer x2" -> (Foreman, 1), (Skilled Laborer, 2) */
 internal static List<KeyValuePair<string,int>> Parse(string crew){var list=new List<KeyValuePair<string,int>>();if(string.IsNullOrWhiteSpace(crew))return list;
  foreach(var part in crew.Split(';')){var p=part.Trim();if(p=="")continue;int x=p.LastIndexOf(" x",StringComparison.OrdinalIgnoreCase);int n;
   if(x>0&&int.TryParse(p.Substring(x+2).Trim(),NumberStyles.Integer,IC,out n)&&n>0)list.Add(new KeyValuePair<string,int>(p.Substring(0,x).Trim(),n));else list.Add(new KeyValuePair<string,int>(p,1));}
  return list;}
 /* same trades added together: "1 Foreman, 4 Skilled Laborer, 6 Unskilled Laborer" */
 internal static string Compact(string crew){var sum=new List<KeyValuePair<string,int>>();foreach(var k in Parse(crew).Where(k=>!IsForeman(k.Key))){int i=sum.FindIndex(x=>string.Equals(x.Key,k.Key,StringComparison.OrdinalIgnoreCase));if(i<0)sum.Add(k);else sum[i]=new KeyValuePair<string,int>(sum[i].Key,sum[i].Value+k.Value);}
  return string.Join(", ",sum.Select(k=>k.Value+" "+k.Key));}
 internal const int ForemanRatio=15;
 internal static bool IsForeman(string trade){return trade.IndexOf("foreman",StringComparison.OrdinalIgnoreCase)>=0;}
 internal static int Foremen(int workers){return workers<=0?0:(workers+ForemanRatio-1)/ForemanRatio;}
 /* workers of one item per working day (all its crews, foreman not included) */
 internal static int Labor(ScheduleRow r){return Counted(r)?Parse(r.Crew).Where(k=>!IsForeman(k.Key)).Sum(k=>k.Value)*Math.Max(1,r.Crews):0;}
 internal static bool Counted(ScheduleRow r){return r.Days>0&&Parse(r.Crew).Count>0;}
 /* men per working day of one item (all its crews) */
 internal static int Men(ScheduleRow r){return Labor(r);}
 internal static int ManDays(ScheduleRow r){return Men(r)*Math.Max(0,r.Days);}
 internal static int Total(List<ScheduleRow> rows){return rows.Where(r=>r.Days>0).Select(r=>r.Finish).DefaultIfEmpty(0).Max();}
 /* daily[d] = men on working day d (1-based; index 0 unused) */
 internal static int[] Daily(List<ScheduleRow> rows){int total=Total(rows);var daily=new int[total+1];foreach(var r in rows.Where(Counted)){int m=Men(r);for(int d=r.Start;d<=r.Finish&&d<=total;d++)if(d>=1)daily[d]+=m;}for(int d=1;d<=total;d++)daily[d]+=Foremen(daily[d]);return daily;}
 internal static List<ManpowerTrade> Trades(List<ScheduleRow> rows){int total=Total(rows);var per=new Dictionary<string,int[]>(StringComparer.OrdinalIgnoreCase);var order=new List<string>();
  var workers=new int[total+1];foreach(var r in rows.Where(Counted))foreach(var k in Parse(r.Crew).Where(k=>!IsForeman(k.Key))){int[] a;if(!per.TryGetValue(k.Key,out a)){a=new int[total+1];per[k.Key]=a;order.Add(k.Key);}int m=k.Value*Math.Max(1,r.Crews);for(int d=Math.Max(1,r.Start);d<=r.Finish&&d<=total;d++){a[d]+=m;workers[d]+=m;}}
  var fm=new int[total+1];for(int d=1;d<=total;d++)fm[d]=Foremen(workers[d]);if(fm.Sum()>0){per["Foreman (1 per "+ForemanRatio+" workers)"]=fm;order.Insert(0,"Foreman (1 per "+ForemanRatio+" workers)");}
  return order.Select(t=>{var a=per[t];int md=a.Sum();return new ManpowerTrade{Trade=t,Peak=a.Max(),ManDays=md,Average=total>0?md/(double)total:0};}).ToList();}
 internal static string Summary(List<ScheduleRow> rows){var daily=Daily(rows);if(daily.Length<=1)return "No manpower yet (no scheduled items with a DUPA crew).";int peak=daily.Max(),md=daily.Sum();int day=Array.IndexOf(daily,peak);
  return "Peak manpower "+peak+" workers (day "+day+");  average "+(md/(double)(daily.Length-1)).ToString("0.0",IC)+" workers per working day;  total "+md.ToString("N0",IC)+" man-days.";}

 /* manpower loading histogram: one column per working day; dashed line = average, label = peak */
 internal static void DrawHistogram(Graphics g,RectangleF r,List<ScheduleRow> rows,Font font,Font bold){var daily=Daily(rows);int total=daily.Length-1;
  using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted))using(var bar=new SolidBrush(Color.FromArgb(70,110,140)))using(var peakB=new SolidBrush(Color.FromArgb(192,40,40)))using(var grid=new Pen(Color.FromArgb(215,220,226),0.5f))using(var axis=new Pen(PowTheme.Ink,0.8f))using(var avgPen=new Pen(Color.FromArgb(176,96,0),1f){DashStyle=DashStyle.Dash}){
   g.DrawString("MANPOWER LOADING (workers per working day)",bold,ink,r.X,r.Y);if(total<=0){g.DrawString("No scheduled items with a DUPA crew yet.",font,muted,r.X,r.Y+18);return;}
   int peak=Math.Max(1,daily.Max());int step=peak<=10?1:peak<=25?5:peak<=60?10:peak<=150?20:50;int top=(int)Math.Ceiling(peak/(double)step)*step;
   float ax=r.X+34,ay=r.Y+22,aw=r.Width-44,ah=r.Height-22-30;float bw=aw/total;var right=new StringFormat{Alignment=StringAlignment.Far,LineAlignment=StringAlignment.Center};var mid=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Near};
   for(int v=0;v<=top;v+=step){float y=ay+ah-ah*v/top;g.DrawLine(grid,ax,y,ax+aw,y);g.DrawString(v.ToString(IC),font,ink,new RectangleF(r.X,y-7,30,14),right);}
   int pd=Array.IndexOf(daily,daily.Max());for(int d=1;d<=total;d++){float h=ah*daily[d]/top;if(h>0)g.FillRectangle(daily[d]==daily[pd]?peakB:bar,ax+(d-1)*bw+bw*0.08f,ay+ah-h,Math.Max(0.6f,bw*0.84f),h);}
   double avg=daily.Skip(1).Average();float yAvg=ay+ah-(float)(ah*avg/top);g.DrawLine(avgPen,ax,yAvg,ax+aw,yAvg);g.DrawString("average "+avg.ToString("0.0",IC),font,new SolidBrush(Color.FromArgb(176,96,0)),ax+aw-90,yAvg-14);
   float px=ax+(pd-0.5f)*bw;g.DrawString("peak "+daily[pd]+" (day "+pd+")",bold,peakB,Math.Min(px+4,ax+aw-110),ay+ah-ah*daily[pd]/top-14);
   g.DrawLine(axis,ax,ay,ax,ay+ah);g.DrawLine(axis,ax,ay+ah,ax+aw,ay+ah);
   int lab=total<=30?1:total<=60?5:total<=150?10:total<=400?20:50;for(int d=1;d<=total;d++)if(d==1||d%lab==0||d==total){float x=ax+(d-0.5f)*bw;g.DrawLine(axis,x,ay+ah,x,ay+ah+3);g.DrawString(d.ToString(IC),font,ink,new RectangleF(x-16,ay+ah+4,32,14),mid);}
   g.DrawString("Working day",font,muted,ax+aw/2-30,ay+ah+17);}}
 /* trade summary table; returns the height used */
 internal static float DrawTrades(Graphics g,float x,float y,List<ScheduleRow> rows,Font font,Font bold){var trades=Trades(rows);float[] w={220,90,110,110};string[] h={"Trade","Peak / day","Average / day","Man-days"};
  using(var ink=new SolidBrush(PowTheme.Ink))using(var head=new SolidBrush(Color.FromArgb(236,240,244)))using(var grid=new Pen(Color.FromArgb(205,211,218),0.5f)){var near=new StringFormat{LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap};var far=new StringFormat(near){Alignment=StringAlignment.Far};
   float rh=16,y0=y;g.FillRectangle(head,x,y,w.Sum(),rh+2);float cx=x;for(int i=0;i<h.Length;i++){g.DrawString(h[i],bold,ink,new RectangleF(cx+3,y,w[i]-6,rh+2),i==0?near:far);cx+=w[i];}y+=rh+2;
   foreach(var t in trades.Concat(new[]{new ManpowerTrade{Trade="All trades",Peak=Daily(rows).DefaultIfEmpty(0).Max(),ManDays=trades.Sum(k=>k.ManDays),Average=trades.Sum(k=>k.Average)}})){bool all=t.Trade=="All trades";string[] v={t.Trade,t.Peak.ToString(IC),t.Average.ToString("0.0",IC),t.ManDays.ToString("N0",IC)};cx=x;
    for(int i=0;i<v.Length;i++){g.DrawString(v[i],all?bold:font,ink,new RectangleF(cx+3,y,w[i]-6,rh),i==0?near:far);cx+=w[i];}y+=rh;g.DrawLine(grid,x,y,x+w.Sum(),y);}
   cx=x;for(int i=0;i<=w.Length;i++){g.DrawLine(grid,cx,y0,cx,y);if(i<w.Length)cx+=w[i];}g.DrawLine(grid,x,y0,x+w.Sum(),y0);
   return y-y0;}}
 internal static readonly float[] ItemWidths={36,76,400,420,44,56,44,84,70};
 internal const float ItemRow=15,ItemHead=20;
 /* activity table: lines [start, start + count) */
 internal static void DrawItems(Graphics g,float x,float y,List<ScheduleRow> rows,int start,int count,Font font,Font bold,bool totals){float[] w=ItemWidths;string[] h={"No.","Item no.","Description","Workers per DUPA crew (foreman counted separately)","Crews","Workers / day","Days","Working days","Man-days"};
  using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted))using(var head=new SolidBrush(Color.FromArgb(236,240,244)))using(var grid=new Pen(Color.FromArgb(205,211,218),0.5f)){var near=new StringFormat{LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap};var far=new StringFormat(near){Alignment=StringAlignment.Far};var mid=new StringFormat(near){Alignment=StringAlignment.Center};
   float y0=y;g.FillRectangle(head,x,y,w.Sum(),ItemHead);float cx=x;for(int i=0;i<h.Length;i++){g.DrawString(h[i],bold,ink,new RectangleF(cx+3,y,w[i]-6,ItemHead),i>=4?far:near);cx+=w[i];}y+=ItemHead;
   for(int k=start;k<start+count&&k<rows.Count;k++){var r=rows[k];bool c=Counted(r);string crew=Parse(r.Crew).Count==0?(ScheduleRow.IsLumpSum(r.Unit)?"LS - not counted":"no DUPA crew - not counted"):(c?"":ScheduleRow.IsLumpSum(r.Unit)?"LS, no duration yet - not counted: ":"no duration - not counted: ")+Compact(r.Crew);
    string[] v={(k+1).ToString(IC),r.Code,r.Description,crew,c?Math.Max(1,r.Crews).ToString(IC):"",c?Men(r).ToString(IC):"",r.Days>0?r.Days.ToString(IC):"",r.Days>0?r.Start+" - "+r.Finish:"",c?ManDays(r).ToString("N0",IC):""};cx=x;
    for(int i=0;i<v.Length;i++){g.DrawString(v[i],font,i==3&&!c?(Brush)muted:ink,new RectangleF(cx+3,y,w[i]-6,ItemRow),i==7?mid:i>=4?far:near);cx+=w[i];}y+=ItemRow;g.DrawLine(grid,x,y,x+w.Sum(),y);}
   if(totals){int fd=Trades(rows).Where(t=>IsForeman(t.Trade)).Sum(t=>t.ManDays);g.DrawString("Foremen (1 per "+ForemanRatio+" workers on each day)",font,ink,new RectangleF(x+3,y,w.Take(5).Sum(),ItemRow),near);g.DrawString(fd.ToString("N0",IC),font,ink,new RectangleF(x,y,w.Sum()-3,ItemRow),far);y+=ItemRow;g.DrawLine(grid,x,y,x+w.Sum(),y);
    g.DrawString("Total",bold,ink,new RectangleF(x+3,y,w.Take(5).Sum(),ItemRow),near);g.DrawString((rows.Sum(r=>ManDays(r))+fd).ToString("N0",IC)+" man-days",bold,ink,new RectangleF(x,y,w.Sum()-3,ItemRow),far);y+=ItemRow;g.DrawLine(grid,x,y,x+w.Sum(),y);}
   cx=x;for(int i=0;i<=w.Length;i++){g.DrawLine(grid,cx,y0,cx,y);if(i<w.Length)cx+=w[i];}g.DrawLine(grid,x,y0,x+w.Sum(),y0);}}
 internal const string Basis="Workers per item = DUPA crew (Output per Hour Summary) without its foreman x number of crews, on every working day of the levelled schedule; daily manpower = workers of the items working that day + 1 foreman per 15 workers (or part). LS items and items without a DUPA crew are not counted.";
}
sealed class ManpowerView:Control {internal List<ScheduleRow> Rows=new List<ScheduleRow>();internal ManpowerView(){DoubleBuffered=true;BackColor=Color.White;Font=new Font("Segoe UI",8f);}
 internal void Fit(){Width=Math.Max(1000,(int)ScheduleManpower.ItemWidths.Sum()+40);Height=(int)(30+280+20+(ScheduleManpower.Trades(Rows).Count+2)*16+30+ScheduleManpower.ItemHead+(Rows.Count+1)*ScheduleManpower.ItemRow+40);Invalidate();}
 protected override void OnPaint(PaintEventArgs e){/* drawings stay paper-white with dark ink in dark mode (owner, 21 November 2026: "pangit ng dark mode") */if(DesktopAppearance.Palette.Dark)e.Graphics.Clear(Color.White);using(new ReportPaperScope())PaintPaper(e);}
 void PaintPaper(PaintEventArgs e){var g=e.Graphics;g.Clear(BackColor);using(var bold=new Font(Font,FontStyle.Bold)){
  TextRenderer.DrawText(g,ScheduleManpower.Summary(Rows),bold,new Point(8,6),PowTheme.Ink);TextRenderer.DrawText(g,ScheduleManpower.Basis,Font,new Point(8,24),PowTheme.Muted);
  ScheduleManpower.DrawHistogram(g,new RectangleF(8,46,Width-30,260),Rows,Font,bold);float y=320;y+=ScheduleManpower.DrawTrades(g,8,y,Rows,Font,bold)+20;
  ScheduleManpower.DrawItems(g,8,y,Rows,0,Rows.Count,Font,bold,true);}}}
}
