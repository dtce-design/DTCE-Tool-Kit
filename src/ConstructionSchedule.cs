using System;using System.Collections.Generic;using System.Drawing;using System.Globalization;using System.Linq;using System.Windows.Forms;using System.Xml;
namespace BeamScheduleFormatter {
// Construction Schedule (owner, 18 November 2026: "bar chart for computation of working days, based sa quantities and pay
// items"; output per hour from the owner's DUPA summary, see ScheduleRates). One row per Bill of Quantities pay item with a
// quantity: working days = quantity / (output per hour x hours per day x crews), rounded up. Each item starts after the
// previous one unless a start day is entered (to run items at the same time). Total working days = latest finish; calendar
// days = working days x 7 / working days per week + unworkable days. LS items are left out for now (owner: "hayaan mo muna
// yung mga LS"). Output per hour, crews and start day can be changed per item and are saved with the project.
sealed class ScheduleOverride {internal double? Rate;internal int? Crews;internal int? Start;}
sealed class ScheduleRow {internal string Code,Description,Unit,Key,Note;internal double Quantity,Rate;internal int Crews,Days,Start,Finish;}
sealed class ScheduleChart:Control {
 internal List<ScheduleRow> Rows=new List<ScheduleRow>();internal int Total;
 internal ScheduleChart(){DoubleBuffered=true;ResizeRedraw=true;BackColor=Color.White;Font=new Font("Segoe UI",8.5f);}
 internal const int RowHeight=22,Top=30;
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(BackColor);int labelW=Math.Min(360,Width/3),left=labelW+16,right=Width-60,span=Math.Max(1,Total);double scale=(right-left)/(double)span;
  using(var bold=new Font(Font,FontStyle.Bold))TextRenderer.DrawText(g,"BAR CHART (working days)",bold,new Rectangle(8,4,labelW,20),PowTheme.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);
  int step=span<=30?5:span<=120?10:span<=400?30:60;using(var grid=new Pen(Color.FromArgb(225,229,234)))for(int d=0;d<=span;d+=step){int x=left+(int)(d*scale);g.DrawLine(grid,x,Top-4,x,Top+Rows.Count*RowHeight);TextRenderer.DrawText(g,d.ToString(CultureInfo.InvariantCulture),Font,new Rectangle(x-20,8,40,18),PowTheme.Muted,TextFormatFlags.HorizontalCenter);}
  for(int i=0;i<Rows.Count;i++){var r=Rows[i];int y=Top+i*RowHeight;TextRenderer.DrawText(g,r.Code+"  "+r.Description,Font,new Rectangle(8,y,labelW,RowHeight),PowTheme.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);
   if(r.Days<=0)continue;int x0=left+(int)((r.Start-1)*scale),x1=left+(int)(r.Finish*scale);using(var b=new SolidBrush(PowTheme.Teal))g.FillRectangle(b,x0,y+5,Math.Max(2,x1-x0),RowHeight-10);
   TextRenderer.DrawText(g,r.Days+" d",Font,new Rectangle(x1+3,y,60,RowHeight),PowTheme.Muted,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);}}
}
partial class ProgramOfWorksForm {
 readonly Dictionary<string,ScheduleOverride> scheduleOverrides=new Dictionary<string,ScheduleOverride>(StringComparer.Ordinal);
 DataGridView scheduleGrid;ScheduleChart scheduleChart;Label scheduleSummary;NumericUpDown scheduleHours,scheduleWeekDays,scheduleUnworkable;bool scheduleFilling;
 static string ScheduleKey(string code,string description){return code+"|"+description;}
 void AddConstructionSchedule(){var page=new TabPage("Construction Schedule");
  var top=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(12,10,12,4),WrapContents=true};
  Func<string,decimal,decimal,decimal,NumericUpDown> number=(caption,min,max,value)=>{top.Controls.Add(new Label{Text=caption,AutoSize=true,Padding=new Padding(0,7,0,0)});var n=new NumericUpDown{Minimum=min,Maximum=max,Value=value,Width=70,Margin=new Padding(4,3,18,3)};top.Controls.Add(n);n.ValueChanged+=(s,e)=>{if(!scheduleFilling)RefreshSchedule();};return n;};
  scheduleHours=number("Working hours per day",1,24,8);scheduleWeekDays=number("Working days per week",1,7,6);scheduleUnworkable=number("Unworkable days (rain, holidays)",0,2000,0);
  scheduleSummary=new Label{Dock=DockStyle.Top,Height=52,Padding=new Padding(14,6,14,6),Font=new Font("Segoe UI",10f,FontStyle.Bold),ForeColor=PowTheme.Ink};
  var note=new Label{Dock=DockStyle.Top,Height=40,Padding=new Padding(14,2,14,2),ForeColor=PowTheme.Muted,Text="Working days = quantity / (output per hour x hours per day x crews), rounded up. Output per hour comes from the DUPA summary (change it per item). Leave Start day blank to start after the item above; enter a day to run items at the same time. LS items are not included."};
  scheduleGrid=new DataGridView{Name="ConstructionSchedule",Dock=DockStyle.Fill,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,BackgroundColor=Color.White};
  foreach(var h in new[]{"Item no.","Description","Unit","Quantity","Output / hour","Crews","Working days","Start day","Finish day","Basis"})scheduleGrid.Columns.Add(h,h);
  foreach(DataGridViewColumn c in scheduleGrid.Columns){c.SortMode=DataGridViewColumnSortMode.NotSortable;c.ReadOnly=!(c.Index==4||c.Index==5||c.Index==7);if(!c.ReadOnly)c.DefaultCellStyle.BackColor=ModernUi.Input;}
  scheduleGrid.Columns[1].Width=360;foreach(int i in new[]{3,4,5,6,7,8})scheduleGrid.Columns[i].DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;scheduleGrid.Columns[9].Width=260;
  scheduleGrid.CellEndEdit+=(s,e)=>{if(scheduleFilling||e.RowIndex<0)return;var row=scheduleGrid.Rows[e.RowIndex];var key=row.Tag as string;if(key==null)return;ScheduleOverride o;if(!scheduleOverrides.TryGetValue(key,out o)){o=new ScheduleOverride();scheduleOverrides[key]=o;}
   string text=Convert.ToString(row.Cells[e.ColumnIndex].Value).Trim();double v;bool ok=double.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out v);
   if(e.ColumnIndex==4)o.Rate=ok&&v>0?(double?)v:null;else if(e.ColumnIndex==5)o.Crews=ok&&v>=1?(int?)Math.Round(v):null;else if(e.ColumnIndex==7)o.Start=ok&&v>=1?(int?)Math.Round(v):null;
   if(!o.Rate.HasValue&&!o.Crews.HasValue&&!o.Start.HasValue)scheduleOverrides.Remove(key);BeginInvoke((Action)RefreshSchedule);};
  GridAppearance.Apply(scheduleGrid);
  scheduleChart=new ScheduleChart{Height=200,Width=900};var chartHost=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.White};chartHost.Controls.Add(scheduleChart);chartHost.Resize+=(s,e)=>{scheduleChart.Width=Math.Max(600,chartHost.ClientSize.Width-4);};
  var split=new SplitContainer{Dock=DockStyle.Fill,Orientation=Orientation.Horizontal};split.Panel1.Controls.Add(scheduleGrid);split.Panel2.Controls.Add(chartHost);
  bool placed=false;split.SizeChanged+=(s,e)=>{if(placed||split.Height<300)return;placed=true;try{split.SplitterDistance=(int)(split.Height*0.5);}catch(InvalidOperationException){}};
  page.Controls.Add(split);page.Controls.Add(note);page.Controls.Add(scheduleSummary);page.Controls.Add(top);tabs.TabPages.Add(page);}
 /* Quantity of each pay item as shown in Bill of Quantities */
 List<KeyValuePair<BackupBillItem,double>> SchedulePayQuantities(){var result=new List<KeyValuePair<BackupBillItem,double>>();if(backupBill==null||backupBill.Count==0)return result;
  var demands=BackupQuantities(false).Where(d=>d.Quantity>1e-10||d.ManualOverride||backupBillRoutes.ContainsKey(d.Key)).ToList();List<BackupDemand> unmapped;var grouped=SourcesByItem(demands,out unmapped);
  foreach(var item in backupBill){List<BackupDemand> sources;if(!grouped.TryGetValue(item.Slot,out sources))sources=new List<BackupDemand>();var matching=sources.Where(d=>BackupMapping.Unit(d.Unit)==BackupMapping.Unit(item.Unit)).ToList();
   bool computed=matching.Count>0&&BackupMapping.Unit(item.Unit)!="ls";double? q=item.ManualQuantity??(computed||matching.Any(d=>d.ManualPriority>=1)?(double?)BackupMapping.FinalQuantity(matching,item.Unit):item.WorkbookQuantity);
   if(q.HasValue&&q.Value>1e-9)result.Add(new KeyValuePair<BackupBillItem,double>(item,q.Value));}return result;}
 internal List<ScheduleRow> ScheduleRows(){var rows=new List<ScheduleRow>();double hours=scheduleHours==null?8:(double)scheduleHours.Value;int finish=0;
  foreach(var pair in SchedulePayQuantities()){var item=pair.Key;if(BackupMapping.Unit(item.Unit)=="ls"||string.Equals((item.Unit??"").Trim(),"Is",StringComparison.OrdinalIgnoreCase))continue;/* LS left out (owner) */
   string key=ScheduleKey(item.Code,item.Description);ScheduleOverride o;scheduleOverrides.TryGetValue(key,out o);var rate=ScheduleRates.Find(item.Code,item.Description);
   var r=new ScheduleRow{Code=item.Code,Description=item.Description,Unit=item.Unit,Key=key,Quantity=pair.Value,Crews=o!=null&&o.Crews.HasValue?o.Crews.Value:1,Rate=o!=null&&o.Rate.HasValue?o.Rate.Value:rate!=null?rate.PerHour:0};
   r.Note=o!=null&&o.Rate.HasValue?"Output per hour entered for this project":rate!=null?"DUPA: "+rate.Description+(rate.Crew==""?"":" | "+rate.Crew):"No output per hour found - enter it";
   r.Days=r.Rate>0?(int)Math.Ceiling(r.Quantity/(r.Rate*hours*r.Crews)-1e-9):0;r.Start=o!=null&&o.Start.HasValue?o.Start.Value:finish+1;r.Finish=r.Days>0?r.Start+r.Days-1:r.Start-1;if(r.Days>0)finish=Math.Max(finish,r.Finish);rows.Add(r);}
  return rows;}
 void RefreshSchedule(){if(scheduleGrid==null||scheduleGrid.IsDisposed)return;var rows=ScheduleRows();scheduleFilling=true;try{scheduleGrid.Rows.Clear();
   foreach(var r in rows){int n=scheduleGrid.Rows.Add(r.Code,r.Description,r.Unit,r.Quantity.ToString("N2"),r.Rate>0?r.Rate.ToString("0.######",CultureInfo.InvariantCulture):"",r.Crews,r.Days>0?r.Days.ToString(CultureInfo.InvariantCulture):"",r.Start,r.Days>0?r.Finish.ToString(CultureInfo.InvariantCulture):"",r.Note);scheduleGrid.Rows[n].Tag=r.Key;if(r.Rate<=0)scheduleGrid.Rows[n].Cells[9].Style.ForeColor=Color.FromArgb(176,96,0);}}finally{scheduleFilling=false;}
  int total=rows.Where(r=>r.Days>0).Select(r=>r.Finish).DefaultIfEmpty(0).Max();int week=(int)scheduleWeekDays.Value;int calendar=(int)Math.Ceiling(total*7.0/week)+(int)scheduleUnworkable.Value;
  scheduleSummary.Text=rows.Count==0?"No pay item quantities yet (LS items are not included). Map the Tool Kit quantities to the BACKUP pay items first.":"Total: "+total+" working days  |  about "+calendar+" calendar days ("+week+" working days per week"+(scheduleUnworkable.Value>0?" + "+scheduleUnworkable.Value+" unworkable days":"")+")"+(rows.Any(r=>r.Rate<=0)?"  |  "+rows.Count(r=>r.Rate<=0)+" item(s) without output per hour":"");
  scheduleChart.Rows=rows;scheduleChart.Total=total;scheduleChart.Height=ScheduleChart.Top+rows.Count*ScheduleChart.RowHeight+20;scheduleChart.Invalidate();}
 void WriteSchedule(XmlElement root){var e=root.OwnerDocument.CreateElement("ConstructionSchedule");root.AppendChild(e);
  if(scheduleHours!=null){e.SetAttribute("hours",scheduleHours.Value.ToString(CultureInfo.InvariantCulture));e.SetAttribute("weekDays",scheduleWeekDays.Value.ToString(CultureInfo.InvariantCulture));e.SetAttribute("unworkable",scheduleUnworkable.Value.ToString(CultureInfo.InvariantCulture));}
  foreach(var pair in scheduleOverrides.OrderBy(p=>p.Key,StringComparer.Ordinal)){var i=root.OwnerDocument.CreateElement("Item");i.SetAttribute("key",pair.Key);if(pair.Value.Rate.HasValue)i.SetAttribute("rate",pair.Value.Rate.Value.ToString("R",CultureInfo.InvariantCulture));if(pair.Value.Crews.HasValue)i.SetAttribute("crews",pair.Value.Crews.Value.ToString(CultureInfo.InvariantCulture));if(pair.Value.Start.HasValue)i.SetAttribute("start",pair.Value.Start.Value.ToString(CultureInfo.InvariantCulture));e.AppendChild(i);}}
 void ReadSchedule(XmlElement root){scheduleOverrides.Clear();var e=root["ConstructionSchedule"];if(scheduleHours==null)return;scheduleFilling=true;try{
   Func<string,decimal,NumericUpDown,decimal> value=(name,fallback,box)=>{decimal v;return e!=null&&decimal.TryParse(e.GetAttribute(name),NumberStyles.Float,CultureInfo.InvariantCulture,out v)?Math.Max(box.Minimum,Math.Min(box.Maximum,v)):fallback;};
   scheduleHours.Value=value("hours",8,scheduleHours);scheduleWeekDays.Value=value("weekDays",6,scheduleWeekDays);scheduleUnworkable.Value=value("unworkable",0,scheduleUnworkable);
   if(e!=null)foreach(XmlElement i in e.SelectNodes("Item")){string key=i.GetAttribute("key");if(key=="")continue;double r;int c,st;var o=new ScheduleOverride{Rate=double.TryParse(i.GetAttribute("rate"),NumberStyles.Float,CultureInfo.InvariantCulture,out r)&&r>0?(double?)r:null,Crews=int.TryParse(i.GetAttribute("crews"),out c)&&c>=1?(int?)c:null,Start=int.TryParse(i.GetAttribute("start"),out st)&&st>=1?(int?)st:null};scheduleOverrides[key]=o;}}
  finally{scheduleFilling=false;}}
}
}
