using System;using System.Collections.Generic;using System.Drawing;using System.Globalization;using System.Linq;using System.Windows.Forms;using System.Xml;
namespace BeamScheduleFormatter {
// Construction Schedule (owner, 18 November 2026: "bar chart for computation of working days, based sa quantities and pay
// items"; output per hour from the owner's DUPA summary, see ScheduleRates). One row per Bill of Quantities pay item with a
// quantity: working days = quantity / (output per hour x hours per day x crews), rounded up. Each item starts after the
// previous one unless a start day is entered (to run items at the same time). Total working days = latest finish; 8 hours per day,
// working days only (owner: "fix at 8 hrs/day, working days lang"). LS items are left out for now (owner: "hayaan mo muna
// yung mga LS"). Output per hour, crews and start day can be changed per item and are saved with the project.
sealed class ScheduleOverride {internal double? Rate;internal int? Crews;internal int? Start;}
sealed class ScheduleRow {internal string Code,Description,Unit,Key,Note,Phase;internal double Quantity,Rate;internal int Crews,Days,Start,Finish,PhaseIndex,Order;internal bool Critical;
 internal static bool IsLumpSum(string unit){return BackupMapping.Unit(unit)=="ls";}}
// Automatic sequence with parallel works (owner, 18 November 2026: "automate na yung pagkakasunod-sunod, yung pwedeng
// mag-parallel works"): each pay item belongs to a work phase by its DPWH item no.; the items of one phase run at the same
// time (they start together) and a phase starts the day after the previous phase is finished.
static class SchedulePhases {
 internal static readonly string[] Names={"Site clearing and removal","Excavation","Piling and ground improvement","Foundation and structural concrete (rebar, forms, concrete, fill)","Masonry and structural steel","Roofing, insulation, waterproofing and plumbing / electrical rough-in","Finishes, doors and windows, site works","Painting and final works"};
 internal static int Of(string code){string c=(code??"").Trim();int n=0,i=0;while(i<c.Length&&char.IsDigit(c[i])){n=n*10+(c[i]-'0');i++;}if(i==0)return 0;string rest=c.Substring(i);
  if(n==800||n==801)return 0;if(n==802||n==803||n==1600||n==102)return 1;if(n==1052||n==808||n==1049||n==1717)return 2;
  if(n==900||n==901||n==902||n==903||n==1000||n==103||n==804||n==1601||n==1034||n==404)return 3;if(n==1046||n==1047||n==506)return 4;
  if(n==1003&&(rest.StartsWith("(5)")||rest.StartsWith("(6)")))return 5;if(n==1013||n==1014||n==1038||n==1016||n==1001||n==1002||(n>=1100&&n<1200))return 5;
  if(n==1032)return 7;return 6;}
}
sealed class ScheduleChart:Control {
 /* One chart (owner, 18 November 2026: "chart na lang siya, include item no., description, quantity, unit, output per
    hour, no. of days, tapos yung bar chart"; "printable, tapos ilagay lahat ng working days"): the item columns on the
    left and one numbered column per working day on the right. The same drawing is used on screen and on paper. */
 internal List<ScheduleRow> Rows=new List<ScheduleRow>();internal int Total;
 internal ScheduleChart(){DoubleBuffered=true;BackColor=Color.White;Font=new Font("Segoe UI",8.5f);}
 internal static readonly string[] Captions={"Item no.","Description","Quantity","Unit","Output / hour","No. of days"};
 static readonly float[] ScreenWidths={90,300,90,60,90,70};const float ScreenDay=24,ScreenRow=24,ScreenHead=44;
 /* a phase heading (Item == null) or an item line */
 internal sealed class Line {internal string Phase;internal ScheduleRow Item;}
 internal static List<Line> Lines(List<ScheduleRow> rows){return rows.Select(r=>new Line{Item=r}).ToList();}/* item order as in Bill of Quantities, no phase headings */
 internal void Fit(){var lines=Lines(Rows);Width=(int)(ScreenWidths.Sum()+Math.Max(1,Total)*ScreenDay+40);Height=(int)(ScreenHead+lines.Count*ScreenRow+20);Invalidate();}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);using(var bold=new Font(Font,FontStyle.Bold))Draw(e.Graphics,Lines(Rows),0,0,ScreenWidths,ScreenDay,ScreenRow,ScreenHead,1,Math.Max(1,Total),Font,bold);}
 internal static void Draw(Graphics g,List<Line> lines,float x0,float y0,float[] widths,float dayW,float rowH,float headH,int dayFrom,int dayTo,Font font,Font bold){
  var cols=new float[widths.Length+1];float x=x0;for(int c=0;c<widths.Length;c++){cols[c]=x;x+=widths[c];}cols[widths.Length]=x;float left=x;int days=dayTo-dayFrom+1;float right=left+days*dayW,bottom=y0+headH+lines.Count*rowH;
  var near=new StringFormat{Alignment=StringAlignment.Near,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap};var far=new StringFormat(near){Alignment=StringAlignment.Far};var mid=new StringFormat(near){Alignment=StringAlignment.Center};var wrap=new StringFormat{LineAlignment=StringAlignment.Center};
  using(var head=new SolidBrush(Color.FromArgb(236,240,244)))g.FillRectangle(head,x0,y0,right-x0,headH);
  using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted))using(var teal=new SolidBrush(PowTheme.Teal))using(var band=new SolidBrush(Color.FromArgb(244,246,248)))using(var grid=new Pen(Color.FromArgb(205,211,218),0.5f))using(var orange=new SolidBrush(Color.FromArgb(176,96,0))){
   for(int c=0;c<Captions.Length;c++)g.DrawString(Captions[c],bold,ink,new RectangleF(cols[c]+2,y0,widths[c]-4,headH),wrap);
   /* standard time scale: week row (6 working days per week, Sundays not counted) over the numbered working days */
   for(int w=(dayFrom-1)/6;w*6+1<=dayTo;w++){int a=Math.Max(dayFrom,w*6+1),b=Math.Min(dayTo,w*6+6);float wx=left+(a-dayFrom)*dayW,ww=(b-a+1)*dayW;g.DrawString((ww>=dayW*4?"Week ":"W")+(w+1),bold,ink,new RectangleF(wx,y0,ww,headH/2),mid);}
   for(int d=dayFrom;d<=dayTo;d++){float dx=left+(d-dayFrom)*dayW;g.DrawString(d.ToString(CultureInfo.InvariantCulture),font,ink,new RectangleF(dx,y0+headH/2,dayW,headH/2),mid);}
   for(int i=0;i<lines.Count;i++){var l=lines[i];float y=y0+headH+i*rowH;
    if(l.Item==null){g.FillRectangle(band,x0,y,right-x0,rowH);g.DrawString(l.Phase,bold,ink,new RectangleF(x0+2,y,right-x0-4,rowH),near);continue;}
    var r=l.Item;string[] cells={r.Code,r.Description,r.Quantity.ToString("N2"),r.Unit,r.Rate>0?r.Rate.ToString("0.###",CultureInfo.InvariantCulture):ScheduleRow.IsLumpSum(r.Unit)?"":"-",r.Days>0?r.Days.ToString(CultureInfo.InvariantCulture):ScheduleRow.IsLumpSum(r.Unit)?"":"-"};
    for(int c=0;c<cells.Length;c++)g.DrawString(cells[c],font,r.Rate<=0&&c==4&&!ScheduleRow.IsLumpSum(r.Unit)?(Brush)orange:ink,new RectangleF(cols[c]+2,y,widths[c]-4,rowH),c==2||c==4||c==5?far:near);
    if(r.Days>0){int s=Math.Max(r.Start,dayFrom),f=Math.Min(r.Finish,dayTo);if(s<=f)g.FillRectangle(r.Critical?(Brush)Brushes.Firebrick:teal,left+(s-dayFrom)*dayW+1,y+rowH*0.25f,(f-s+1)*dayW-2,rowH*0.5f);}}
   foreach(float cx in cols)g.DrawLine(grid,cx,y0,cx,bottom);for(int d=0;d<=days;d++){float dx=left+d*dayW;g.DrawLine(grid,dx,y0+headH/2,dx,bottom);}using(var week=new Pen(Color.FromArgb(120,130,142),0.9f))for(int d=dayFrom;d<=dayTo+1;d++)if((d-1)%6==0&&d>dayFrom&&d<=dayTo)g.DrawLine(week,left+(d-dayFrom)*dayW,y0,left+(d-dayFrom)*dayW,bottom);
   g.DrawLine(grid,x0,y0,right,y0);g.DrawLine(grid,left,y0+headH/2,right,y0+headH/2);for(int i=0;i<=lines.Count;i++){float y=y0+headH+i*rowH;g.DrawLine(grid,x0,y,right,y);}g.DrawLine(grid,right,y0,right,bottom);}}
}
partial class ProgramOfWorksForm {
 readonly Dictionary<string,ScheduleOverride> scheduleOverrides=new Dictionary<string,ScheduleOverride>(StringComparer.Ordinal);
 ScheduleChart scheduleChart;Label scheduleSummary;TabControl scheduleViews;PdmView pdmView;PertView pertView;
 static string ScheduleKey(string code,string description){return code+"|"+description;}
 const double ScheduleHoursPerDay=8;/* fixed (owner, 18 November 2026: "fix at 8 hrs/day") */
 void AddConstructionSchedule(){var page=new TabPage("Construction Schedule");
  scheduleSummary=new Label{Dock=DockStyle.Top,Height=34,Padding=new Padding(14,8,14,2),Font=new Font("Segoe UI",10f,FontStyle.Bold),ForeColor=PowTheme.Ink};
  var note=new Label{Dock=DockStyle.Top,Height=28,Padding=new Padding(14,2,14,2),ForeColor=PowTheme.Muted,Text="No. of days = quantity / (output per hour x 8 hours), rounded up; working days only. Items keep the Bill of Quantities order; works of the same phase run at the same time and each phase starts after the one before it. LS items are not included."};
  scheduleChart=new ScheduleChart{Height=200,Width=1200};var host=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.White};host.Controls.Add(scheduleChart);
  var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=44,Padding=new Padding(10,4,10,0)};
  Button(bar,"Print preview",()=>PrintSchedule(true));Button(bar,"Print",()=>PrintSchedule(false));bar.Controls.Add(new Label{AutoSize=true,Padding=new Padding(10,10,0,0),ForeColor=PowTheme.Muted,Text="Prints the complete set for issue: summary, bar chart, PDM, PERT table and PERT-CPM network (A3 landscape, title block). Red = critical path."});
  /* Bar Chart and Precedence Diagram (PDM) views (owner, 18 November 2026) */
  scheduleViews=new TabControl{Dock=DockStyle.Fill};var barPage=new TabPage("Bar Chart");barPage.Controls.Add(host);var pdmPage=new TabPage("Precedence Diagram (PDM)");
  pdmView=new PdmView{Width=800,Height=400};var pdmHost=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.White};pdmHost.Controls.Add(pdmView);pdmPage.Controls.Add(pdmHost);var pertPage=new TabPage("PERT-CPM");pertView=new PertView{Width=800,Height=400};var pertHost=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.White};pertHost.Controls.Add(pertView);pertPage.Controls.Add(pertHost);
  scheduleViews.TabPages.Add(barPage);scheduleViews.TabPages.Add(pdmPage);scheduleViews.TabPages.Add(pertPage);
  page.Controls.Add(scheduleViews);page.Controls.Add(note);page.Controls.Add(scheduleSummary);page.Controls.Add(bar);tabs.TabPages.Add(page);}
 /* Quantity of each pay item as shown in Bill of Quantities */
 List<KeyValuePair<BackupBillItem,double>> SchedulePayQuantities(){var result=new List<KeyValuePair<BackupBillItem,double>>();if(backupBill==null||backupBill.Count==0)return result;
  var demands=BackupQuantities(false).Where(d=>d.Quantity>1e-10||d.ManualOverride||backupBillRoutes.ContainsKey(d.Key)).ToList();List<BackupDemand> unmapped;var grouped=SourcesByItem(demands,out unmapped);
  foreach(var item in backupBill){List<BackupDemand> sources;if(!grouped.TryGetValue(item.Slot,out sources))sources=new List<BackupDemand>();var matching=sources.Where(d=>BackupMapping.Unit(d.Unit)==BackupMapping.Unit(item.Unit)).ToList();
   bool computed=matching.Count>0&&BackupMapping.Unit(item.Unit)!="ls";double? q=item.ManualQuantity??(computed||matching.Any(d=>d.ManualPriority>=1)?(double?)BackupMapping.FinalQuantity(matching,item.Unit):item.WorkbookQuantity);
   if(q.HasValue&&q.Value>1e-9)result.Add(new KeyValuePair<BackupBillItem,double>(item,q.Value));}return result;}
 internal List<ScheduleRow> ScheduleRows(){var rows=new List<ScheduleRow>();double hours=ScheduleHoursPerDay;int finish=0;
  foreach(var pair in SchedulePayQuantities()){var item=pair.Key;string u=(item.Unit??"").Trim().ToLowerInvariant().Replace(".","");bool lumpSum=BackupMapping.Unit(item.Unit)=="ls";if(!lumpSum&&(u=="ps"||u=="mo"||u=="veh-mo"))continue;/* time-based items (months) are not work output; LS items are listed (owner, 18 November 2026: "isama pa din yung mga LS, pero leave muna yung output per hour") with the output per hour left blank */
   string key=ScheduleKey(item.Code,item.Description);ScheduleOverride o;scheduleOverrides.TryGetValue(key,out o);var rate=ScheduleRates.Find(item.Code,item.Description);
   var r=new ScheduleRow{Code=item.Code,Description=item.Description,Unit=item.Unit,Key=key,Quantity=pair.Value,Crews=o!=null&&o.Crews.HasValue?o.Crews.Value:1,Rate=o!=null&&o.Rate.HasValue?o.Rate.Value:rate!=null&&!lumpSum?rate.PerHour:0};
   r.Note=o!=null&&o.Rate.HasValue?"Output per hour entered for this project":lumpSum?"Lump sum - output per hour left blank for now (not counted)":rate!=null?"DUPA: "+rate.Description+(rate.Crew==""?"":" | "+rate.Crew):"No output per hour found - enter it";
   r.Days=r.Rate>0?(int)Math.Ceiling(r.Quantity/(r.Rate*hours*r.Crews)-1e-9):0;r.PhaseIndex=SchedulePhases.Of(item.Code);r.Phase=SchedulePhases.Names[r.PhaseIndex];r.Order=rows.Count;rows.Add(r);}
  /* phases one after another; the items of a phase in parallel */
  return Plan(rows);}
 /* start / finish days: phases one after another, items of a phase in parallel */
 internal static List<ScheduleRow> Plan(List<ScheduleRow> rows){int finish=0;
  /* rows stay in Bill of Quantities order (owner: "magkakasunod-sunod pa din ang item no., huwag i-rearrange"); only the
     start days follow the phases */
  foreach(var phase in rows.GroupBy(r=>r.PhaseIndex).OrderBy(p=>p.Key)){int start=finish+1;foreach(var r in phase){r.Start=start;r.Finish=r.Days>0?r.Start+r.Days-1:r.Start-1;}if(phase.Any(r=>r.Days>0))finish=phase.Where(r=>r.Days>0).Max(r=>r.Finish);}
  return rows;}
 void RefreshSchedule(){if(scheduleChart==null||scheduleChart.IsDisposed)return;var rows=ScheduleRows();int total=rows.Where(r=>r.Days>0).Select(r=>r.Finish).DefaultIfEmpty(0).Max();
  scheduleSummary.Text=rows.Count==0?"No pay item quantities yet. Map the Tool Kit quantities to the BACKUP pay items first.":"Total: "+total+" working days"+(rows.Any(r=>r.Rate<=0&&!ScheduleRow.IsLumpSum(r.Unit))?"  |  "+rows.Count(r=>r.Rate<=0&&!ScheduleRow.IsLumpSum(r.Unit))+" item(s) without output per hour (not counted)":"")+(rows.Any(r=>r.Rate<=0&&ScheduleRow.IsLumpSum(r.Unit))?"  |  "+rows.Count(r=>r.Rate<=0&&ScheduleRow.IsLumpSum(r.Unit))+" LS item(s) listed, output per hour left blank":"");
  var network=PdmNetwork.Build(rows);var critical=new HashSet<ScheduleRow>(network.Nodes.Where(x=>x.Critical).Select(x=>x.Row));foreach(var r in rows)r.Critical=critical.Contains(r);
  scheduleChart.Rows=rows;scheduleChart.Total=total;scheduleChart.Fit();if(pdmView!=null){pdmView.Network=network;pdmView.Fit();}if(pertView!=null){pertView.Network=network;pertView.Pert=PertResult.Build(network);pertView.Fit();}}
 void WriteSchedule(XmlElement root){var e=root.OwnerDocument.CreateElement("ConstructionSchedule");root.AppendChild(e);
  foreach(var pair in scheduleOverrides.OrderBy(p=>p.Key,StringComparer.Ordinal)){var i=root.OwnerDocument.CreateElement("Item");i.SetAttribute("key",pair.Key);if(pair.Value.Rate.HasValue)i.SetAttribute("rate",pair.Value.Rate.Value.ToString("R",CultureInfo.InvariantCulture));if(pair.Value.Crews.HasValue)i.SetAttribute("crews",pair.Value.Crews.Value.ToString(CultureInfo.InvariantCulture));if(pair.Value.Start.HasValue)i.SetAttribute("start",pair.Value.Start.Value.ToString(CultureInfo.InvariantCulture));e.AppendChild(i);}}
 void ReadSchedule(XmlElement root){scheduleOverrides.Clear();var e=root["ConstructionSchedule"];if(e==null)return;
  foreach(XmlElement i in e.SelectNodes("Item")){string key=i.GetAttribute("key");if(key=="")continue;double r;int c,st;scheduleOverrides[key]=new ScheduleOverride{Rate=double.TryParse(i.GetAttribute("rate"),NumberStyles.Float,CultureInfo.InvariantCulture,out r)&&r>0?(double?)r:null,Crews=int.TryParse(i.GetAttribute("crews"),out c)&&c>=1?(int?)c:null,Start=int.TryParse(i.GetAttribute("start"),out st)&&st>=1?(int?)st:null};}}
}
}
