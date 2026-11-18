using System;using System.Collections.Generic;using System.Drawing;using System.Globalization;using System.Linq;using System.Windows.Forms;using System.Xml;
namespace BeamScheduleFormatter {
// Construction Schedule (owner, 18 November 2026: "bar chart for computation of working days, based sa quantities and pay
// items"; output per hour from the owner's DUPA summary, see ScheduleRates). One row per Bill of Quantities pay item with a
// quantity: working days = quantity / (output per hour x hours per day x crews), rounded up. Each item starts after the
// previous one unless a start day is entered (to run items at the same time). Total working days = latest finish; 8 hours per day,
// working days only (owner: "fix at 8 hrs/day, working days lang"). LS items are left out for now (owner: "hayaan mo muna
// yung mga LS"). Output per hour, crews and start day can be changed per item and are saved with the project.
sealed class ScheduleOverride {internal double? Rate;internal int? Crews;internal int? Start;}
sealed class ScheduleRow {internal string Code,Description,Unit,Key,Note,Phase;internal double Quantity,Rate;internal int Crews,Days,Start,Finish,PhaseIndex,Order;}
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
    hour, no. of days, tapos yung bar chart"): the item columns on the left, the bars on the right. */
 internal List<ScheduleRow> Rows=new List<ScheduleRow>();internal int Total;
 internal ScheduleChart(){DoubleBuffered=true;ResizeRedraw=true;BackColor=Color.White;Font=new Font("Segoe UI",8.5f);}
 internal const int RowHeight=24,Head=44;
 static readonly string[] Captions={"Item no.","Description","Quantity","Unit","Output / hour","No. of days"};
 static readonly int[] Widths={90,300,90,60,90,70};
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(BackColor);int x=0;var cols=new int[Widths.Length+1];for(int c=0;c<Widths.Length;c++){cols[c]=x;x+=Widths[c];}cols[Widths.Length]=x;
  int left=x+8,right=Width-50,span=Math.Max(1,Total);double scale=(right-left)/(double)span;int bottom=Head+(Rows.Count+Rows.Select(q=>q.PhaseIndex).Distinct().Count())*RowHeight;
  using(var head=new SolidBrush(Color.FromArgb(236,240,244)))g.FillRectangle(head,0,0,Width,Head);
  using(var bold=new Font(Font,FontStyle.Bold)){for(int c=0;c<Captions.Length;c++)TextRenderer.DrawText(g,Captions[c],bold,new Rectangle(cols[c]+4,0,Widths[c]-6,Head),PowTheme.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak);
   TextRenderer.DrawText(g,"Working days",bold,new Rectangle(left,2,200,18),PowTheme.Ink,TextFormatFlags.Left);}
  int step=span<=30?5:span<=120?10:span<=400?30:60;using(var grid=new Pen(Color.FromArgb(222,226,231))){for(int d=0;d<=span;d+=step){int gx=left+(int)(d*scale);g.DrawLine(grid,gx,Head-6,gx,bottom);TextRenderer.DrawText(g,d.ToString(CultureInfo.InvariantCulture),Font,new Rectangle(gx-20,Head-22,40,16),PowTheme.Muted,TextFormatFlags.HorizontalCenter);}
   foreach(int cx in cols)g.DrawLine(grid,cx,0,cx,bottom);g.DrawLine(grid,0,Head,Width,Head);}
  int line=0,lastPhase=-1;
  for(int i=0;i<Rows.Count;i++){var r=Rows[i];if(r.PhaseIndex!=lastPhase){lastPhase=r.PhaseIndex;int hy=Head+line*RowHeight;using(var band=new SolidBrush(Color.FromArgb(244,246,248)))g.FillRectangle(band,0,hy,Width,RowHeight);using(var bold=new Font(Font,FontStyle.Bold))TextRenderer.DrawText(g,r.Phase,bold,new Rectangle(4,hy,left-8,RowHeight),PowTheme.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);line++;}
   int y=Head+line*RowHeight;line++;string[] cells={r.Code,r.Description,r.Quantity.ToString("N2"),r.Unit,r.Rate>0?r.Rate.ToString("0.###",CultureInfo.InvariantCulture):"-",r.Days>0?r.Days.ToString(CultureInfo.InvariantCulture):"-"};
   using(var rule=new Pen(Color.FromArgb(236,239,242)))g.DrawLine(rule,0,y+RowHeight,Width,y+RowHeight);
   for(int c=0;c<cells.Length;c++){bool number=c==2||c==4||c==5;TextRenderer.DrawText(g,cells[c],Font,new Rectangle(cols[c]+4,y,Widths[c]-8,RowHeight),r.Rate<=0&&c==4?Color.FromArgb(176,96,0):PowTheme.Ink,(number?TextFormatFlags.Right:TextFormatFlags.Left)|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);}
   if(r.Days<=0)continue;int x0=left+(int)((r.Start-1)*scale),x1=left+(int)(r.Finish*scale);using(var b=new SolidBrush(PowTheme.Teal))g.FillRectangle(b,x0,y+6,Math.Max(2,x1-x0),RowHeight-12);
   TextRenderer.DrawText(g,r.Days+" d",Font,new Rectangle(x1+3,y,46,RowHeight),PowTheme.Muted,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);}}
}
partial class ProgramOfWorksForm {
 readonly Dictionary<string,ScheduleOverride> scheduleOverrides=new Dictionary<string,ScheduleOverride>(StringComparer.Ordinal);
 ScheduleChart scheduleChart;Label scheduleSummary;
 static string ScheduleKey(string code,string description){return code+"|"+description;}
 const double ScheduleHoursPerDay=8;/* fixed (owner, 18 November 2026: "fix at 8 hrs/day") */
 void AddConstructionSchedule(){var page=new TabPage("Construction Schedule");
  scheduleSummary=new Label{Dock=DockStyle.Top,Height=34,Padding=new Padding(14,8,14,2),Font=new Font("Segoe UI",10f,FontStyle.Bold),ForeColor=PowTheme.Ink};
  var note=new Label{Dock=DockStyle.Top,Height=28,Padding=new Padding(14,2,14,2),ForeColor=PowTheme.Muted,Text="No. of days = quantity / (output per hour x 8 hours), rounded up; working days only. Items of the same work phase run at the same time; each phase starts after the one before it. LS items are not included."};
  scheduleChart=new ScheduleChart{Height=200,Width=1200};var host=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.White};host.Controls.Add(scheduleChart);host.Resize+=(s,e)=>{scheduleChart.Width=Math.Max(1100,host.ClientSize.Width-4);};
  page.Controls.Add(host);page.Controls.Add(note);page.Controls.Add(scheduleSummary);tabs.TabPages.Add(page);}
 /* Quantity of each pay item as shown in Bill of Quantities */
 List<KeyValuePair<BackupBillItem,double>> SchedulePayQuantities(){var result=new List<KeyValuePair<BackupBillItem,double>>();if(backupBill==null||backupBill.Count==0)return result;
  var demands=BackupQuantities(false).Where(d=>d.Quantity>1e-10||d.ManualOverride||backupBillRoutes.ContainsKey(d.Key)).ToList();List<BackupDemand> unmapped;var grouped=SourcesByItem(demands,out unmapped);
  foreach(var item in backupBill){List<BackupDemand> sources;if(!grouped.TryGetValue(item.Slot,out sources))sources=new List<BackupDemand>();var matching=sources.Where(d=>BackupMapping.Unit(d.Unit)==BackupMapping.Unit(item.Unit)).ToList();
   bool computed=matching.Count>0&&BackupMapping.Unit(item.Unit)!="ls";double? q=item.ManualQuantity??(computed||matching.Any(d=>d.ManualPriority>=1)?(double?)BackupMapping.FinalQuantity(matching,item.Unit):item.WorkbookQuantity);
   if(q.HasValue&&q.Value>1e-9)result.Add(new KeyValuePair<BackupBillItem,double>(item,q.Value));}return result;}
 internal List<ScheduleRow> ScheduleRows(){var rows=new List<ScheduleRow>();double hours=ScheduleHoursPerDay;int finish=0;
  foreach(var pair in SchedulePayQuantities()){var item=pair.Key;string u=(item.Unit??"").Trim().ToLowerInvariant().Replace(".","");if(BackupMapping.Unit(item.Unit)=="ls"||u=="is"||u=="ps"||u=="mo"||u=="veh-mo")continue;/* LS left out (owner); time-based items (months) are not work output */
   string key=ScheduleKey(item.Code,item.Description);ScheduleOverride o;scheduleOverrides.TryGetValue(key,out o);var rate=ScheduleRates.Find(item.Code,item.Description);
   var r=new ScheduleRow{Code=item.Code,Description=item.Description,Unit=item.Unit,Key=key,Quantity=pair.Value,Crews=o!=null&&o.Crews.HasValue?o.Crews.Value:1,Rate=o!=null&&o.Rate.HasValue?o.Rate.Value:rate!=null?rate.PerHour:0};
   r.Note=o!=null&&o.Rate.HasValue?"Output per hour entered for this project":rate!=null?"DUPA: "+rate.Description+(rate.Crew==""?"":" | "+rate.Crew):"No output per hour found - enter it";
   r.Days=r.Rate>0?(int)Math.Ceiling(r.Quantity/(r.Rate*hours*r.Crews)-1e-9):0;r.PhaseIndex=SchedulePhases.Of(item.Code);r.Phase=SchedulePhases.Names[r.PhaseIndex];r.Order=rows.Count;rows.Add(r);}
  /* phases one after another; the items of a phase in parallel */
  rows=rows.OrderBy(r=>r.PhaseIndex).ThenBy(r=>r.Order).ToList();
  foreach(var phase in rows.GroupBy(r=>r.PhaseIndex)){int start=finish+1;foreach(var r in phase){r.Start=start;r.Finish=r.Days>0?r.Start+r.Days-1:r.Start-1;}if(phase.Any(r=>r.Days>0))finish=phase.Where(r=>r.Days>0).Max(r=>r.Finish);}
  return rows;}
 void RefreshSchedule(){if(scheduleChart==null||scheduleChart.IsDisposed)return;var rows=ScheduleRows();int total=rows.Where(r=>r.Days>0).Select(r=>r.Finish).DefaultIfEmpty(0).Max();
  scheduleSummary.Text=rows.Count==0?"No pay item quantities yet (LS items are not included). Map the Tool Kit quantities to the BACKUP pay items first.":"Total: "+total+" working days"+(rows.Any(r=>r.Rate<=0)?"  |  "+rows.Count(r=>r.Rate<=0)+" item(s) without output per hour (not counted)":"");
  scheduleChart.Rows=rows;scheduleChart.Total=total;scheduleChart.Height=ScheduleChart.Head+(rows.Count+rows.Select(q=>q.PhaseIndex).Distinct().Count())*ScheduleChart.RowHeight+20;scheduleChart.Invalidate();}
 void WriteSchedule(XmlElement root){var e=root.OwnerDocument.CreateElement("ConstructionSchedule");root.AppendChild(e);
  foreach(var pair in scheduleOverrides.OrderBy(p=>p.Key,StringComparer.Ordinal)){var i=root.OwnerDocument.CreateElement("Item");i.SetAttribute("key",pair.Key);if(pair.Value.Rate.HasValue)i.SetAttribute("rate",pair.Value.Rate.Value.ToString("R",CultureInfo.InvariantCulture));if(pair.Value.Crews.HasValue)i.SetAttribute("crews",pair.Value.Crews.Value.ToString(CultureInfo.InvariantCulture));if(pair.Value.Start.HasValue)i.SetAttribute("start",pair.Value.Start.Value.ToString(CultureInfo.InvariantCulture));e.AppendChild(i);}}
 void ReadSchedule(XmlElement root){scheduleOverrides.Clear();var e=root["ConstructionSchedule"];if(e==null)return;
  foreach(XmlElement i in e.SelectNodes("Item")){string key=i.GetAttribute("key");if(key=="")continue;double r;int c,st;scheduleOverrides[key]=new ScheduleOverride{Rate=double.TryParse(i.GetAttribute("rate"),NumberStyles.Float,CultureInfo.InvariantCulture,out r)&&r>0?(double?)r:null,Crews=int.TryParse(i.GetAttribute("crews"),out c)&&c>=1?(int?)c:null,Start=int.TryParse(i.GetAttribute("start"),out st)&&st>=1?(int?)st:null};}}
}
}
