using System;using System.Collections.Generic;using System.Drawing;using System.Globalization;using System.Linq;using System.Windows.Forms;using System.Xml;
namespace BeamScheduleFormatter {
// Construction Schedule (owner, 18 November 2026: "bar chart for computation of working days, based sa quantities and pay
// items"; output per hour from the owner's DUPA summary, see ScheduleRates). One row per Bill of Quantities pay item with a
// quantity: working days = quantity / (output per hour x hours per day x crews), rounded up. Each item starts after the
// previous one unless a start day is entered (to run items at the same time). Total working days = latest finish; 8 hours per day,
// working days only (owner: "fix at 8 hrs/day, working days lang"). LS items are left out for now (owner: "hayaan mo muna
// yung mga LS"). Output per hour, crews and start day can be changed per item and are saved with the project.
sealed class ScheduleOverride {internal double? Rate;internal int? Crews;internal int? Start;}
sealed class ScheduleRow {internal string Code,Description,Unit,Key,Note,Phase,Crew="",Predecessors="";internal double Quantity,Rate;internal int Crews,Days,Start,Finish,PhaseIndex,Order;internal bool Critical;
 internal static bool IsLumpSum(string unit){return BackupMapping.Unit(unit)=="ls";}}
// Automatic sequence with parallel works (owner, 18 November 2026: "automate na yung pagkakasunod-sunod, yung pwedeng
// mag-parallel works"): each pay item belongs to a work phase by its DPWH item no.; the items of one phase run at the same
// time (they start together) and a phase starts the day after the previous phase is finished.
static class SchedulePhases {
 internal static readonly string[] Names={"Site clearing and removal","Excavation","Piling and ground improvement","Foundation and structural concrete (rebar, forms, concrete, fill)","Masonry and structural steel","Roofing, insulation, waterproofing and plumbing / electrical rough-in","Finishes, doors and windows, site works","Painting and final works"};
 internal static int Of(string code){string c=(code??"").Trim();int n=0,i=0;while(i<c.Length&&char.IsDigit(c[i])){n=n*10+(c[i]-'0');i++;}if(i==0)return 0;string rest=c.Substring(i);
  if(n==800||n==801||n==100||n==101)return 0;if(n==802||n==803||n==1600||n==102)return 1;if(n==1052||n==808||n==1049||n==1717)return 2;
  if(n==900||n==901||n==902||n==903||n==1000||n==103||n==804||n==104||n==1601||n==1061||n==1034||n==404)return 3;if(n==1046||n==1047||n==506||n==507||n==511)return 4;
  if(n==1003&&(rest.StartsWith("(5)")||rest.StartsWith("(6)")))return 5;if(n==1013||n==1014||n==1038||n==1016||n==1001||n==1002||(n>=1100&&n<1200))return 5;
  if(n==1032)return 7;return 6;}
}
sealed class ScheduleChart:Control {
 /* One chart (owner, 18 November 2026: "chart na lang siya, include item no., description, quantity, unit, output per
    hour, no. of days, tapos yung bar chart"; "printable, tapos ilagay lahat ng working days"): the item columns on the
    left and one numbered column per working day on the right. The same drawing is used on screen and on paper. */
 internal List<ScheduleRow> Rows=new List<ScheduleRow>();internal int Total;
 internal ScheduleChart(){DoubleBuffered=true;BackColor=Color.White;Font=new Font("Segoe UI",8.5f);}
 /* owner (19 November 2026, "hindi ba pwede paiksiin yung working days"): double-click an item to set its crews / output */
 internal Action<ScheduleRow> RowDoubleClicked;
 internal ScheduleRow RowAt(Point p){var lines=Lines(Rows);using(var g=CreateGraphics()){float head=HeaderHeight(g,Font,ScreenDay,Math.Max(1,Total),ScreenHead);var hs=Heights(g,lines,Font,ScreenWidths[1],ScreenRow);float y=head;for(int i=0;i<lines.Count;i++){if(p.Y>=y&&p.Y<y+hs[i])return lines[i].Item;y+=hs[i];}}return null;}
 protected override void OnMouseDoubleClick(MouseEventArgs e){base.OnMouseDoubleClick(e);var r=RowAt(e.Location);if(r!=null&&RowDoubleClicked!=null)RowDoubleClicked(r);}
 internal static readonly string[] Captions={"Item no.","Description","Quantity","Unit","Output / hour","No. of days"};
 readonly float[] ScreenWidths={90,300,90,60,90,70};float ScreenRow=24;/* base row height; long descriptions get two lines */const float ScreenDay=24,ScreenHead=44;
 /* description column sized to its longest text (owner, 18 November 2026: "adjust mo yung description box para magkasya"):
    widened up to maxW; a text still longer wraps to two lines and the rows are made tall enough for them */
 internal static void FitDescription(Graphics g,IEnumerable<ScheduleRow> rows,Font font,float[] widths,float maxW,ref float rowH){
  float need=rows.Select(r=>g.MeasureString(r.Description??"",font).Width).DefaultIfEmpty(0).Max()+10;widths[1]=Math.Max(widths[1],Math.Min(maxW,need));
}
 /* row heights: one line, or two lines for a description longer than its column */
 internal static float[] Heights(Graphics g,List<Line> lines,Font font,float descW,float rowH){float two=Math.Max(rowH,font.GetHeight(g)*2+6);
  return lines.Select(l=>l.Item!=null&&g.MeasureString(l.Item.Description??"",font).Width>descW-6?two:rowH).ToArray();}
 /* a phase heading (Item == null) or an item line */
 internal sealed class Line {internal string Phase;internal ScheduleRow Item;}
 internal static List<Line> Lines(List<ScheduleRow> rows){return rows.Select(r=>new Line{Item=r}).ToList();}/* item order as in Bill of Quantities, no phase headings */
 /* owner (19 November 2026, "ifix mo yung left side tapos scrollable yung working days"): the item columns stay in place and only
    the working days scroll (scroll bar under the chart, or Shift + mouse wheel) */
 internal int FirstDay=1;internal ScrollBar DayScroll;
 internal int VisibleDays{get{return Math.Max(1,(int)((Width-ScreenWidths.Sum()-24)/ScreenDay));}}
 internal void SyncDays(){int total=Math.Max(1,Total),vis=VisibleDays;FirstDay=Math.Max(1,Math.Min(FirstDay,Math.Max(1,total-vis+1)));
  if(DayScroll!=null){DayScroll.Minimum=1;DayScroll.SmallChange=1;DayScroll.LargeChange=Math.Max(1,Math.Min(vis,total));DayScroll.Maximum=total;DayScroll.Enabled=total>vis;DayScroll.Value=Math.Max(1,Math.Min(FirstDay,DayScroll.Maximum-DayScroll.LargeChange+1));}Invalidate();}
 internal void ScrollDays(int day){FirstDay=day;SyncDays();}
 protected override void OnMouseWheel(MouseEventArgs e){if((ModifierKeys&Keys.Shift)!=0){ScrollDays(FirstDay-Math.Sign(e.Delta)*Math.Max(1,VisibleDays/4));((HandledMouseEventArgs)e).Handled=true;return;}base.OnMouseWheel(e);}
 bool fitting;
 internal void Fit(){if(fitting||IsDisposed)return;fitting=true;try{FitCore();}finally{fitting=false;}}
 /* re-entrancy guarded: setting the size can show or hide the host scroll bars, which resizes the host and asks for Fit again */
 void FitCore(){var lines=Lines(Rows);ScreenWidths[1]=300;float h;using(var g=CreateGraphics()){FitDescription(g,Rows,Font,ScreenWidths,380,ref ScreenRow);h=Heights(g,lines,Font,ScreenWidths[1],ScreenRow).Sum();}
  int full=(int)(ScreenWidths.Sum()+Math.Max(1,Total)*ScreenDay+40);int width=Parent!=null?Math.Max((int)ScreenWidths.Sum()+(int)ScreenDay*8,Math.Min(full,Parent.Width-SystemInformation.VerticalScrollBarWidth-6)):full;int height=(int)(ScreenHead+h+40+ScreenRow+10);if(Width!=width)Width=width;if(Height!=height)Height=height;SyncDays();}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);int last=Math.Min(Math.Max(1,Total),FirstDay+VisibleDays-1);using(var bold=new Font(Font,FontStyle.Bold))Draw(e.Graphics,Lines(Rows),0,0,ScreenWidths,ScreenDay,ScreenRow,ScreenHead,FirstDay,Math.Max(FirstDay,last),Font,bold,ScheduleManpower.Daily(Rows));}
 /* header height: caption row plus the day-number row; numbers too wide for their column (3 digits) are written
    vertically (owner, 18 November 2026: "rotate mo siguro days sa barchart para magkasya if 3 digits na") */
 internal static bool RotateDays(Graphics g,Font font,float dayW,int dayTo){return g.MeasureString(dayTo.ToString(CultureInfo.InvariantCulture),font).Width>dayW-1;}
 internal static float HeaderHeight(Graphics g,Font font,float dayW,int dayTo,float headH){if(!RotateDays(g,font,dayW,dayTo))return headH;return headH/2+Math.Max(headH/2,g.MeasureString(dayTo.ToString(CultureInfo.InvariantCulture),font).Width+6);}
 internal static void Draw(Graphics g,List<Line> lines,float x0,float y0,float[] widths,float dayW,float rowH,float headH,int dayFrom,int dayTo,Font font,Font bold,int[] manpower=null){
  float top=headH/2;bool rotate=RotateDays(g,font,dayW,dayTo);headH=HeaderHeight(g,font,dayW,dayTo,headH);
  var cols=new float[widths.Length+1];float x=x0;for(int c=0;c<widths.Length;c++){cols[c]=x;x+=widths[c];}cols[widths.Length]=x;float left=x;int days=dayTo-dayFrom+1;var hs=Heights(g,lines,font,widths[1],rowH);var ys=new float[lines.Count+1];ys[0]=y0+headH;for(int i=0;i<lines.Count;i++)ys[i+1]=ys[i]+hs[i];float right=left+days*dayW,bottom=ys[lines.Count];
  var near=new StringFormat{Alignment=StringAlignment.Near,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap};var far=new StringFormat(near){Alignment=StringAlignment.Far};var mid=new StringFormat(near){Alignment=StringAlignment.Center};var wrap=new StringFormat{LineAlignment=StringAlignment.Center};
  using(var head=new SolidBrush(Color.FromArgb(236,240,244)))g.FillRectangle(head,x0,y0,right-x0,headH);
  using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted))using(var teal=new SolidBrush(PowTheme.Teal))using(var band=new SolidBrush(Color.FromArgb(244,246,248)))using(var grid=new Pen(Color.FromArgb(205,211,218),0.5f))using(var orange=new SolidBrush(Color.FromArgb(176,96,0))){
   for(int c=0;c<Captions.Length;c++)g.DrawString(Captions[c],bold,ink,new RectangleF(cols[c]+2,y0,widths[c]-4,headH),wrap);
   g.DrawString("Working days",bold,ink,new RectangleF(left+2,y0,Math.Max(dayW*4,right-left),top),near);/* days only, no week row (owner, 18 November 2026: "alisin mo yung weeks, days lang") */
   for(int d=dayFrom;d<=dayTo;d++){float dx=left+(d-dayFrom)*dayW;string t=d.ToString(CultureInfo.InvariantCulture);if(!rotate){g.DrawString(t,font,ink,new RectangleF(dx,y0+top,dayW,headH-top),mid);continue;}
    /* read from bottom to top, centred in the day column */var st=g.Save();g.TranslateTransform(dx+dayW/2,y0+headH-3);g.RotateTransform(-90);var sz=g.MeasureString(t,font);g.DrawString(t,font,ink,0,-sz.Height/2);g.Restore(st);}
   for(int i=0;i<lines.Count;i++){var l=lines[i];float y=ys[i];float rowH0=rowH;rowH=hs[i];
    if(l.Item==null){g.FillRectangle(band,x0,y,right-x0,rowH);g.DrawString(l.Phase,bold,ink,new RectangleF(x0+2,y,right-x0-4,rowH),near);rowH=rowH0;continue;}
    var r=l.Item;string[] cells={r.Code,r.Description,r.Quantity.ToString("N2"),r.Unit,r.Rate>0?r.Rate.ToString("0.###",CultureInfo.InvariantCulture):ScheduleRow.IsLumpSum(r.Unit)?"":"-",r.Days>0?r.Days.ToString(CultureInfo.InvariantCulture):ScheduleRow.IsLumpSum(r.Unit)?"":"-"};
    var desc=new StringFormat{LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisWord};for(int c=0;c<cells.Length;c++)g.DrawString(cells[c],font,r.Rate<=0&&c==4&&!ScheduleRow.IsLumpSum(r.Unit)?(Brush)orange:ink,new RectangleF(cols[c]+2,y,widths[c]-4,rowH),c==1?desc:c==2||c==4||c==5?far:near);
    if(r.Days>0){int s=Math.Max(r.Start,dayFrom),f=Math.Min(r.Finish,dayTo);if(s<=f)g.FillRectangle(r.Critical?(Brush)Brushes.Firebrick:teal,left+(s-dayFrom)*dayW+1,y+rowH/2-rowH0/4,(f-s+1)*dayW-2,rowH0*0.5f);}rowH=rowH0;}
   foreach(float cx in cols)g.DrawLine(grid,cx,y0,cx,bottom);for(int d=0;d<=days;d++){float dx=left+d*dayW;g.DrawLine(grid,dx,y0+top,dx,bottom);}
   g.DrawLine(grid,x0,y0,right,y0);g.DrawLine(grid,left,y0+top,right,y0+top);for(int i=0;i<=lines.Count;i++){float y=ys[i];g.DrawLine(grid,x0,y,right,y);}g.DrawLine(grid,right,y0,right,bottom);
   /* manpower per working day under the bars (owner, 18 November 2026: "determine the manpower needed") */
   if(manpower!=null){float fh=Math.Max(rowH,16);for(int d=dayFrom;d<=dayTo&&d<manpower.Length;d++){var w=g.MeasureString(manpower[d].ToString(CultureInfo.InvariantCulture),font).Width;if(w>dayW-4)fh=Math.Max(fh,w+6);}g.FillRectangle(band,x0,bottom,right-x0,fh);g.DrawString("Manpower per day (workers)",bold,ink,new RectangleF(x0+2,bottom,left-x0-4,fh),near);
    for(int d=dayFrom;d<=dayTo;d++){int m=d<manpower.Length?manpower[d]:0;if(m<=0)continue;float dx=left+(d-dayFrom)*dayW;string t=m.ToString(CultureInfo.InvariantCulture);var sz=g.MeasureString(t,font);
     if(sz.Width<=dayW-4)g.DrawString(t,font,ink,new RectangleF(dx,bottom,dayW,fh),mid);else{var st=g.Save();g.TranslateTransform(dx+dayW/2,bottom+fh-2);g.RotateTransform(-90);g.DrawString(t,font,ink,0,-sz.Height/2);g.Restore(st);}}
    for(int d=0;d<=days;d++){float dx=left+d*dayW;g.DrawLine(grid,dx,bottom,dx,bottom+fh);}g.DrawLine(grid,x0,bottom,x0,bottom+fh);g.DrawLine(grid,left,bottom,left,bottom+fh);g.DrawLine(grid,x0,bottom+fh,right,bottom+fh);}}}
}
partial class ProgramOfWorksForm {
 readonly Dictionary<string,ScheduleOverride> scheduleOverrides=new Dictionary<string,ScheduleOverride>(StringComparer.Ordinal);
 ScheduleChart scheduleChart;Label scheduleSummary;TabControl scheduleViews;PdmView pdmView;PertView pertView;ManpowerView manpowerView;
 static string ScheduleKey(string code,string description){return code+"|"+description;}
 const double ScheduleHoursPerDay=8;/* fixed (owner, 18 November 2026: "fix at 8 hrs/day") */
 void AddConstructionSchedule(){var page=new TabPage("Construction Schedule");
  /* owner (19 November 2026, "ayusin itong sa taas"): one command row - print buttons and the summary - and one short hint; the long
     explanation opens from "How the schedule is computed" */
  scheduleSummary=new Label{AutoSize=true,Padding=new Padding(14,9,8,0),Font=new Font("Segoe UI",10f,FontStyle.Bold),ForeColor=PowTheme.Ink};
  const string explanation="No. of days = quantity / (output per hour x 8 hours x crews), rounded up; working days only. The output per hour is fixed at the DUPA value; double-click an item in the Bar Chart to add crews and shorten it.\r\n\r\nItems keep the Bill of Quantities order. Construction logic: each item follows the trades it depends on (FS / SS / FF with lags, e.g. formworks overlap the rebar, trusses 7 days after the frame is poured); when an item is missing it follows the next work it stands on. Non-critical items are started within their float so the daily manpower stays level. LS items are listed without a duration.\r\n\r\nPrint / Print preview: the complete set for issue - summary, bar chart with manpower per day, manpower loading and schedule, PDM, PERT table and PERT-CPM network (A3 landscape, title block). Red = critical path.";
scheduleChart=new ScheduleChart{Height=200,Width=1200,Cursor=Cursors.Hand};scheduleChart.RowDoubleClicked=EditScheduleItem;var host=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.White};host.Controls.Add(scheduleChart);var dayScroll=new HScrollBar{Dock=DockStyle.Bottom,Height=SystemInformation.HorizontalScrollBarHeight};scheduleChart.DayScroll=dayScroll;dayScroll.Scroll+=(s,e)=>scheduleChart.ScrollDays(dayScroll.Value);host.Resize+=(s,e)=>{if(scheduleChart.Rows!=null&&!scheduleChart.IsDisposed&&!host.Disposing&&!IsDisposed&&Math.Abs(scheduleChart.Width-(host.Width-SystemInformation.VerticalScrollBarWidth-6))>1)scheduleChart.Fit();};
  var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=44,Padding=new Padding(10,4,10,0),WrapContents=false};
  Button(bar,"Print preview",()=>PrintSchedule(true));Button(bar,"Print",()=>PrintSchedule(false));Button(bar,"?",()=>MessageBox.Show(this,explanation,"Construction Schedule",MessageBoxButtons.OK,MessageBoxIcon.Information));
  var printTip=new ToolTip();foreach(Control c in bar.Controls)if(c.Text=="?")printTip.SetToolTip(c,"How the schedule is computed");else if(c.Text.StartsWith("Print"))printTip.SetToolTip(c,"Prints the complete set for issue (A3 landscape): summary, bar chart with manpower per day, manpower loading and schedule, PDM, PERT table and PERT-CPM network.");page.Disposed+=(s,e)=>printTip.Dispose();
  bar.Controls.Add(scheduleSummary);
  /* Bar Chart and Precedence Diagram (PDM) views (owner, 18 November 2026) */
  scheduleViews=new TabControl{Dock=DockStyle.Fill};var barPage=new TabPage("Bar Chart");barPage.Controls.Add(host);barPage.Controls.Add(dayScroll);var pdmPage=new TabPage("Precedence Diagram (PDM)");
  pdmView=new PdmView{Width=800,Height=400};var pdmHost=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.White};pdmHost.Controls.Add(pdmView);pdmPage.Controls.Add(pdmHost);var pertPage=new TabPage("PERT-CPM");pertView=new PertView{Width=800,Height=400};var pertHost=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.White};pertHost.Controls.Add(pertView);pertPage.Controls.Add(pertHost);
  var manPage=new TabPage("Manpower");manpowerView=new ManpowerView{Width=1000,Height=600};var manHost=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.White};manHost.Controls.Add(manpowerView);manPage.Controls.Add(manHost);
  scheduleViews.TabPages.Add(barPage);scheduleViews.TabPages.Add(pdmPage);scheduleViews.TabPages.Add(pertPage);scheduleViews.TabPages.Add(manPage);
  page.Controls.Add(scheduleViews);page.Controls.Add(bar);tabs.TabPages.Add(page);}
 /* crews and output per hour of one item (saved with the Program of Works); more crews shorten the item and add to the daily manpower */
 void EditScheduleItem(ScheduleRow row){if(row==null||ScheduleRow.IsLumpSum(row.Unit))return;string key=row.Key??ScheduleKey(row.Code,row.Description);ScheduleOverride o;scheduleOverrides.TryGetValue(key,out o);
  var standard=ScheduleRates.Find(row.Code,row.Description);
  using(var f=new Form{Text="Schedule - "+row.Code,FormBorderStyle=FormBorderStyle.FixedDialog,MinimizeBox=false,MaximizeBox=false,StartPosition=FormStartPosition.CenterParent,ClientSize=new Size(520,250),Font=Font}){AppBrand.Apply(f);
   var info=new Label{Left=14,Top=12,Width=490,Height=40,Text=row.Code+"  "+row.Description+"\r\nQuantity "+row.Quantity.ToString("N2")+" "+row.Unit+" - now "+row.Days+" working day(s)"};
   var crews=new NumericUpDown{Left=260,Top=62,Width=90,Minimum=1,Maximum=50,Value=Math.Max(1,Math.Min(50,row.Crews))};
   var preview=new Label{Left=14,Top=132,Width=490,Height=44,ForeColor=PowTheme.Muted};
   Func<double> perHour=()=>standard!=null?standard.PerHour:row.Rate;
   Action show=()=>{double r=perHour();int days=r>0?(int)Math.Ceiling(row.Quantity/(r*ScheduleHoursPerDay*(int)crews.Value)-1e-9):0;preview.Text="New duration: "+days+" working day(s) with "+crews.Value+" crew(s) at "+r.ToString("0.###",CultureInfo.InvariantCulture)+" "+row.Unit+"/hour each. Each crew adds its workers to the daily manpower.";};
   crews.ValueChanged+=(s,e)=>show();show();
   var ok=new Button{Text="Apply",Left=300,Top=200,Width=100,DialogResult=DialogResult.OK};var reset=new Button{Text="Reset",Left=192,Top=200,Width=100};var cancel=new Button{Text="Cancel",Left=408,Top=200,Width=100,DialogResult=DialogResult.Cancel};
   reset.Click+=(s,e)=>{crews.Value=1;};
   f.Controls.AddRange(new Control[]{info,new Label{Left=14,Top=65,Width=240,Text="No. of crews working at the same time"},crews,new Label{Left=14,Top=99,Width=490,Text="Output per hour per crew (fixed, DUPA): "+perHour().ToString("0.###",CultureInfo.InvariantCulture)+" "+row.Unit+"/hour"},preview,reset,ok,cancel});f.AcceptButton=ok;f.CancelButton=cancel;
   if(f.ShowDialog(this)!=DialogResult.OK)return;
   var next=new ScheduleOverride{Crews=crews.Value>1?(int?)crews.Value:null,Start=o!=null?o.Start:null};
   if(!next.Crews.HasValue&&!next.Start.HasValue)scheduleOverrides.Remove(key);else scheduleOverrides[key]=next;
   dashboardDirty=reportPreviewDirty=true;RefreshSchedule();}}
 /* Quantity of each pay item as shown in Bill of Quantities */
 List<KeyValuePair<BackupBillItem,double>> SchedulePayQuantities(){var result=new List<KeyValuePair<BackupBillItem,double>>();if(backupBill==null||backupBill.Count==0)return result;
  var demands=BackupQuantities(false).Where(d=>d.Quantity>1e-10||d.ManualOverride||backupBillRoutes.ContainsKey(d.Key)).ToList();List<BackupDemand> unmapped;var grouped=SourcesByItem(demands,out unmapped);
  foreach(var item in backupBill){List<BackupDemand> sources;if(!grouped.TryGetValue(item.Slot,out sources))sources=new List<BackupDemand>();var matching=sources.Where(d=>BackupMapping.Unit(d.Unit)==BackupMapping.Unit(item.Unit)).ToList();
   bool computed=matching.Count>0&&BackupMapping.Unit(item.Unit)!="ls";double? q=item.ManualQuantity??(computed||matching.Any(d=>d.ManualPriority>=1)?(double?)BackupMapping.FinalQuantity(matching,item.Unit):item.WorkbookQuantity);
   if(q.HasValue&&q.Value>1e-9)result.Add(new KeyValuePair<BackupBillItem,double>(item,q.Value));}return result;}
 internal List<ScheduleRow> ScheduleRows(){var rows=new List<ScheduleRow>();double hours=ScheduleHoursPerDay;int finish=0;
  foreach(var pair in SchedulePayQuantities()){var item=pair.Key;string u=(item.Unit??"").Trim().ToLowerInvariant().Replace(".","");bool lumpSum=BackupMapping.Unit(item.Unit)=="ls";if(!lumpSum&&(u=="ps"||u=="mo"||u=="veh-mo"))continue;/* time-based items (months) are not work output; LS items are listed (owner, 18 November 2026: "isama pa din yung mga LS, pero leave muna yung output per hour") with the output per hour left blank */
   string key=ScheduleKey(item.Code,item.Description);ScheduleOverride o;scheduleOverrides.TryGetValue(key,out o);var rate=ScheduleRates.Find(item.Code,item.Description);
   var r=new ScheduleRow{Code=item.Code,Description=item.Description,Unit=item.Unit,Key=key,Quantity=pair.Value,Crews=o!=null&&o.Crews.HasValue?o.Crews.Value:1,/* output per hour fixed at the DUPA value (owner, 19 November 2026: "fix yung output per hour"); only crews change */Rate=rate!=null&&!lumpSum?rate.PerHour:0,Crew=rate!=null?rate.Crew??"":""};
   r.Note=o!=null&&o.Rate.HasValue?"Output per hour entered for this project":lumpSum?"Lump sum - output per hour left blank for now (not counted)":rate!=null?"DUPA: "+rate.Description+(rate.Crew==""?"":" | "+rate.Crew):"No output per hour found - enter it";
   r.PhaseIndex=SchedulePhases.Of(item.Code);r.Phase=SchedulePhases.Names[r.PhaseIndex];
   /* 804(7) gravel: the bedding under the footings and tie beams (automatic) goes in before the lean concrete, the gravel
      of the other areas (manual entries: slab on fill, walks) after the backfill - two bars of the same pay item, the
      quantity split in proportion (the Bill of Quantities is not changed) */
   var parts=new string((item.Code??"").Where(char.IsLetterOrDigit).ToArray())=="8047"?GravelParts():null;
   if(parts!=null&&parts[1]>1e-9){string desc=r.Description??"";if(parts[0]>1e-9){double q=r.Quantity,t=parts[0]+parts[1];
     var r2=new ScheduleRow{Code=r.Code,Description=desc+" - slab on fill / other areas",Unit=r.Unit,Key=r.Key,Quantity=q*parts[1]/t,Crews=r.Crews,Rate=r.Rate,Crew=r.Crew,Note=r.Note,PhaseIndex=r.PhaseIndex,Phase=r.Phase};
     r.Quantity=q*parts[0]/t;r.Description=desc+" - bedding under footings / tie beams";foreach(var x in new[]{r,r2}){x.Days=x.Rate>0?(int)Math.Ceiling(x.Quantity/(x.Rate*hours*x.Crews)-1e-9):0;x.Order=rows.Count;rows.Add(x);}continue;}
    r.Description=desc+" - slab on fill / other areas";}
   r.Days=r.Rate>0?(int)Math.Ceiling(r.Quantity/(r.Rate*hours*r.Crews)-1e-9):0;r.Order=rows.Count;rows.Add(r);}
  /* phases one after another; the items of a phase in parallel */
  return Plan(rows);}
 /* installed gravel volumes: [bedding under beams and footings, other areas entered by hand] */
 double[] GravelParts(){try{double bed=(beam!=null?beam.WorksGravel():0)+footingResults.Sum(q=>q.Gravel)+wallFootingResults.Sum(q=>q.Gravel);return new[]{Math.Max(0,bed),Math.Max(0,ManualGravelVolume())};}catch(Exception){return null;}}
 /* start / finish days from the realistic construction logic (FS / SS / FF relations), CPM and manpower levelling within
    the float (ScheduleLogic); rows stay in Bill of Quantities order (owner: "huwag i-rearrange") */
 internal static List<ScheduleRow> Plan(List<ScheduleRow> rows){ScheduleLogic.Schedule(rows);return rows;}
 void RefreshSchedule(){if(scheduleChart==null||scheduleChart.IsDisposed)return;var rows=ScheduleRows();int total=rows.Where(r=>r.Days>0).Select(r=>r.Finish).DefaultIfEmpty(0).Max();
  scheduleSummary.Text=rows.Count==0?"No pay item quantities yet.":"Total: "+total+" working days"+(rows.Any(r=>r.Rate<=0&&!ScheduleRow.IsLumpSum(r.Unit))?"  |  "+rows.Count(r=>r.Rate<=0&&!ScheduleRow.IsLumpSum(r.Unit))+" item(s) without DUPA output":"");/* owner (19 November 2026): "alisin ang unnecessary wordings" */
  var network=PdmNetwork.Build(rows);
  scheduleChart.Rows=rows;scheduleChart.Total=total;scheduleChart.Fit();if(pdmView!=null){pdmView.Network=network;pdmView.Fit();}if(pertView!=null){var pertNet=PdmNetwork.Build(rows);pertView.Pert=PertResult.Build(pertNet);pertView.Diagram=PertAoa.Build(pertNet,pertView.Pert);pertView.Fit();}if(manpowerView!=null){manpowerView.Rows=rows;manpowerView.Fit();}
  if(rows.Count>0)scheduleSummary.Text+="  |  "+ScheduleManpower.Summary(rows).Split(';')[0];}
 void WriteSchedule(XmlElement root){var e=root.OwnerDocument.CreateElement("ConstructionSchedule");root.AppendChild(e);
  foreach(var pair in scheduleOverrides.OrderBy(p=>p.Key,StringComparer.Ordinal)){var i=root.OwnerDocument.CreateElement("Item");i.SetAttribute("key",pair.Key);if(pair.Value.Rate.HasValue)i.SetAttribute("rate",pair.Value.Rate.Value.ToString("R",CultureInfo.InvariantCulture));if(pair.Value.Crews.HasValue)i.SetAttribute("crews",pair.Value.Crews.Value.ToString(CultureInfo.InvariantCulture));if(pair.Value.Start.HasValue)i.SetAttribute("start",pair.Value.Start.Value.ToString(CultureInfo.InvariantCulture));e.AppendChild(i);}WriteContractDuration(e);}
 void ReadSchedule(XmlElement root){scheduleOverrides.Clear();var e=root["ConstructionSchedule"];ReadContractDuration(e);if(e==null)return;
  foreach(XmlElement i in e.SelectNodes("Item")){string key=i.GetAttribute("key");if(key=="")continue;double r;int c,st;scheduleOverrides[key]=new ScheduleOverride{Rate=double.TryParse(i.GetAttribute("rate"),NumberStyles.Float,CultureInfo.InvariantCulture,out r)&&r>0?(double?)r:null,Crews=int.TryParse(i.GetAttribute("crews"),out c)&&c>=1?(int?)c:null,Start=int.TryParse(i.GetAttribute("start"),out st)&&st>=1?(int?)st:null};}}
}
}
