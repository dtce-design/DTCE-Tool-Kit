using System;using System.Collections.Generic;using System.Drawing;using System.Globalization;using System.Linq;using System.Windows.Forms;using System.Xml;
namespace BeamScheduleFormatter {
// Owner (19 November 2026, "gumawa ng ganitong tab sa pow, ilagay lahat ng holidays, at input ng rainy days", with the
// "COMPUTATION OF ESTIMATED PROJECT DURATION" sheet of Book1.xlsm): the contract duration from the working days, the start
// date, Sundays, holidays and rainy days, month by month, with the same rules as the owner's sheet:
//  - total days of a month = the days of that month inside [start date, end date];
//  - Sundays in that range; holidays in that range that are not Sundays;
//  - rainy days = the average number of rainy days of that month (rainfall above 7.5 mm) over the last five recorded years;
//  - revised rainy days = MIN(total - Sundays - holidays, ROUNDUP(rainy days x (1 - (Sundays + holidays) / total days))),
//    0 for a month part with no Sunday (as in the sheet);
//  - workable days = total - Sundays - holidays - revised rainy days;
//  - the end date is the first date at which the workable days reach the working days of the construction schedule;
//  - contract duration = working days + Sundays + holidays + revised rainy days (= calendar days start..end).
sealed class DurationHoliday {internal DateTime Date;internal string Name="",Type="";}
sealed class DurationMonth {internal int Year,Month,TotalDays,Sundays,Holidays,Rainy,RevisedRainy,Weeks;internal int Working{get{return Math.Max(0,TotalDays-RevisedRainy-Holidays-Sundays);}}}
sealed class DurationResult {internal DateTime Start,End;internal int WorkingDays;internal readonly List<DurationMonth> Months=new List<DurationMonth>();
 internal int Sundays{get{return Months.Sum(m=>m.Sundays);}}internal int Holidays{get{return Months.Sum(m=>m.Holidays);}}internal int RainyDays{get{return Months.Sum(m=>m.RevisedRainy);}}internal int ContractDuration{get{return Months.Sum(m=>m.Working)+Sundays+Holidays+RainyDays;}}}
static class ContractDurationCalc {
 internal static readonly string[] MonthNames=CultureInfo.InvariantCulture.DateTimeFormat.MonthNames.Take(12).Select(m=>m.ToUpperInvariant()).ToArray();
 /* Easter Sunday (anonymous Gregorian algorithm) */
 internal static DateTime Easter(int y){int a=y%19,b=y/100,c=y%100,d=b/4,e=b%4,f=(b+8)/25,g=(b-f+1)/3,h=(19*a+b-d-g+15)%30,i=c/4,k=c%4,l=(32+2*e+2*i-h-k)%7,m=(a+11*h+22*l)/451;int month=(h+l-7*m+114)/31,day=(h+l-7*m+114)%31+1;return new DateTime(y,month,day);}
 static DateTime LastMonday(int y,int month){var d=new DateTime(y,month,DateTime.DaysInMonth(y,month));while(d.DayOfWeek!=DayOfWeek.Monday)d=d.AddDays(-1);return d;}
 /* lunar / Islamic holidays: dates fixed each year by proclamation; these are the expected dates (edit them to the proclamation) */
 static readonly Dictionary<int,string> ChineseNewYear=new Dictionary<int,string>{{2024,"02-10"},{2025,"01-29"},{2026,"02-17"},{2027,"02-06"},{2028,"01-26"},{2029,"02-13"},{2030,"02-03"},{2031,"01-23"},{2032,"02-11"},{2033,"01-31"},{2034,"02-19"},{2035,"02-08"}};
 static readonly Dictionary<int,string> EidFitr=new Dictionary<int,string>{{2024,"04-10"},{2025,"04-01"},{2026,"03-20"},{2027,"03-10"},{2028,"02-27"},{2029,"02-15"},{2030,"02-05"},{2031,"01-25"},{2032,"01-14"},{2033,"01-03"},{2034,"12-12"},{2035,"12-01"}};
 static readonly Dictionary<int,string> EidAdha=new Dictionary<int,string>{{2024,"06-17"},{2025,"06-06"},{2026,"05-27"},{2027,"05-17"},{2028,"05-05"},{2029,"04-24"},{2030,"04-13"},{2031,"04-02"},{2032,"03-22"},{2033,"03-11"},{2034,"03-01"},{2035,"02-18"}};
 /* national regular holidays and special non-working days (RA 9492 and the yearly proclamations) */
 internal static List<DurationHoliday> Standard(int fromYear,int toYear){var list=new List<DurationHoliday>();Action<DateTime,string,string> add=(d,n,t)=>list.Add(new DurationHoliday{Date=d,Name=n,Type=t});
  for(int y=fromYear;y<=toYear;y++){var easter=Easter(y);
   add(new DateTime(y,1,1),"New Year's Day","Regular");string md;
   if(ChineseNewYear.TryGetValue(y,out md))add(DateTime.ParseExact(y+"-"+md,"yyyy-MM-dd",CultureInfo.InvariantCulture),"Chinese New Year","Special (per proclamation)");
   add(easter.AddDays(-3),"Maundy Thursday","Regular");add(easter.AddDays(-2),"Good Friday","Regular");add(easter.AddDays(-1),"Black Saturday","Special");
   add(new DateTime(y,4,9),"Araw ng Kagitingan","Regular");
   if(EidFitr.TryGetValue(y,out md))add(DateTime.ParseExact(y+"-"+md,"yyyy-MM-dd",CultureInfo.InvariantCulture),"Eid'l Fitr","Regular (per proclamation)");
   add(new DateTime(y,5,1),"Labor Day","Regular");
   if(EidAdha.TryGetValue(y,out md))add(DateTime.ParseExact(y+"-"+md,"yyyy-MM-dd",CultureInfo.InvariantCulture),"Eid'l Adha","Regular (per proclamation)");
   add(new DateTime(y,6,12),"Independence Day","Regular");add(new DateTime(y,8,21),"Ninoy Aquino Day","Special");add(LastMonday(y,8),"National Heroes Day","Regular");
   add(new DateTime(y,11,1),"All Saints' Day","Special");add(new DateTime(y,11,2),"All Souls' Day","Special");add(new DateTime(y,11,30),"Bonifacio Day","Regular");
   add(new DateTime(y,12,8),"Feast of the Immaculate Conception","Special");add(new DateTime(y,12,24),"Christmas Eve","Special");add(new DateTime(y,12,25),"Christmas Day","Regular");
   add(new DateTime(y,12,30),"Rizal Day","Regular");add(new DateTime(y,12,31),"Last Day of the Year","Special");}
  return list.OrderBy(h=>h.Date).ToList();}
 /* average rainy days per month: the last five recorded years of each month (rounded), as in the owner's sheet; fewer
    years use what is recorded */
 internal static int[] Averages(IEnumerable<KeyValuePair<int,int?[]>> records,out int[] yearsUsed){var avg=new int[12];yearsUsed=new int[12];var rows=records.OrderBy(r=>r.Key).ToList();
  for(int m=0;m<12;m++){var values=rows.Where(r=>r.Value!=null&&r.Value.Length>m&&r.Value[m].HasValue).Select(r=>(double)r.Value[m].Value).ToList();var last=values.Skip(Math.Max(0,values.Count-5)).ToList();yearsUsed[m]=last.Count;avg[m]=last.Count==0?0:(int)Math.Round(last.Average(),MidpointRounding.AwayFromZero);}return avg;}
 /* days with rainfall above 7.5 mm, PAGASA Aparri Radar and Synoptic Station, from the owner's duration sheet (19 November 2026) */
 internal static Dictionary<int,int?[]> AparriRecords(){return new Dictionary<int,int?[]>{
  {2020,new int?[]{null,null,null,null,null,null,null,null,null,null,9,13}},{2021,new int?[]{8,3,2,0,0,7,6,4,4,6,5,5}},{2022,new int?[]{3,2,1,1,6,4,5,12,5,15,4,16}},
  {2023,new int?[]{5,7,0,1,3,3,9,5,5,6,8,7}},{2024,new int?[]{5,0,0,0,2,2,6,5,11,9,13,8}},{2025,new int?[]{10,6,2,3,4,0,11,7,11,8,14,10}},{2026,new int?[]{9,1,0,1,0,2,9,8,null,null,null,null}}};}
 internal static List<DurationMonth> Months(DateTime start,DateTime end,ICollection<DateTime> holidays,int[] rainy){var result=new List<DurationMonth>();if(end<start)return result;
  for(var m=new DateTime(start.Year,start.Month,1);m<=end;m=m.AddMonths(1)){var from=m<start?start:m;var last=m.AddMonths(1).AddDays(-1);var to=last>end?end:last;var row=new DurationMonth{Year=m.Year,Month=m.Month,TotalDays=(to-from).Days+1};
   for(var d=from;d<=to;d=d.AddDays(1)){if(d.DayOfWeek==DayOfWeek.Sunday)row.Sundays++;else if(holidays.Contains(d.Date))row.Holidays++;}
   row.Rainy=rainy==null?0:rainy[m.Month-1];int open=Math.Max(0,row.TotalDays-row.Sundays-row.Holidays);
   row.RevisedRainy=row.Sundays==0?0:Math.Max(0,Math.Min(open,(int)Math.Ceiling(row.Rainy*(1-(row.Sundays+row.Holidays)/(double)row.TotalDays)-1e-9)));
   row.Weeks=(int)Math.Ceiling(row.TotalDays/7.0);result.Add(row);}
  return result;}
 /* the first end date whose workable days reach the working days */
 internal static DurationResult Compute(DateTime start,int workingDays,IEnumerable<DateTime> holidayDates,int[] rainy){var holidays=new HashSet<DateTime>(holidayDates.Select(d=>d.Date));var result=new DurationResult{Start=start.Date,WorkingDays=workingDays,End=start.Date.AddDays(-1)};
  if(workingDays<=0)return result;
  for(var end=start.Date;end<=start.Date.AddYears(10);end=end.AddDays(1)){var months=Months(start.Date,end,holidays,rainy);if(months.Sum(m=>m.Working)>=workingDays){result.End=end;result.Months.AddRange(months);return result;}}
  throw new Exception("The working days cannot be reached within ten years of the start date.");}
}
partial class ProgramOfWorksForm {
 DateTimePicker durationStart;NumericUpDown durationWorkingDays;DataGridView durationMonths,durationHolidays,durationRain;Label durationSummary,durationRainNote;bool durationFilling;
 /* saved with the Program of Works */
 DateTime durationStartDate=DateTime.Today;int durationWorkingOverride;readonly List<DurationHoliday> durationHolidayList=new List<DurationHoliday>();readonly SortedDictionary<int,int?[]> durationRainRecords=new SortedDictionary<int,int?[]>();
 void AddContractDuration(){var page=new TabPage("Contract Duration");
  var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=44,Padding=new Padding(10,6,10,0),WrapContents=false};
  durationStart=new DateTimePicker{Format=DateTimePickerFormat.Custom,CustomFormat="MMMM d, yyyy",Width=170,Value=durationStartDate};
  durationWorkingDays=new NumericUpDown{Minimum=0,Maximum=5000,Width=80,Value=0};
  top.Controls.Add(new Label{Text="Start date",AutoSize=true,Padding=new Padding(0,6,4,0)});top.Controls.Add(durationStart);
  top.Controls.Add(new Label{Text="Working days (0 = construction schedule)",AutoSize=true,Padding=new Padding(16,6,4,0)});top.Controls.Add(durationWorkingDays);
  durationSummary=new Label{AutoSize=true,Padding=new Padding(16,6,0,0),Font=new Font("Segoe UI",10f,FontStyle.Bold),ForeColor=PowTheme.Ink};top.Controls.Add(durationSummary);
  var views=new TabControl{Dock=DockStyle.Fill};
  /* computation */
  durationMonths=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill};
  foreach(var h in new[]{"Year","Month","Working days","Sundays","Holidays","Rainy days","Revised rainy days","Total days","No. of weeks"})durationMonths.Columns.Add(h.Replace(" ",""),h);GridAppearance.Apply(durationMonths);
  var compute=new TabPage("Computation");compute.Controls.Add(durationMonths);views.TabPages.Add(compute);
  /* holidays */
  durationHolidays=new DataGridView{Dock=DockStyle.Fill,AllowUserToAddRows=true,AllowUserToDeleteRows=true,RowHeadersVisible=true,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill};
  durationHolidays.Columns.Add("Date","Date (yyyy-mm-dd)");durationHolidays.Columns.Add("Holiday","Holiday");durationHolidays.Columns.Add("Type","Type");durationHolidays.Columns[0].FillWeight=40;durationHolidays.Columns[2].FillWeight=50;GridAppearance.Apply(durationHolidays);
  var holidayBar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=40,Padding=new Padding(6,4,6,0)};var fill=new Button{Text="Add national holidays for the project years",AutoSize=true};fill.Click+=(s,e)=>{FillStandardHolidays(true);};holidayBar.Controls.Add(fill);
  holidayBar.Controls.Add(new Label{AutoSize=true,Padding=new Padding(8,8,0,0),ForeColor=PowTheme.Muted,Text="Holidays on Sundays are not counted twice. Lunar and Islamic holidays: edit the dates to the proclamation."});
  var holidayPage=new TabPage("Holidays");holidayPage.Controls.Add(durationHolidays);holidayPage.Controls.Add(holidayBar);views.TabPages.Add(holidayPage);
  /* rainy days */
  durationRain=new DataGridView{Dock=DockStyle.Fill,AllowUserToAddRows=true,AllowUserToDeleteRows=true,RowHeadersVisible=true,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill};
  durationRain.Columns.Add("Year","Year");foreach(var m in CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthNames.Take(12))durationRain.Columns.Add(m,m);GridAppearance.Apply(durationRain);
  durationRainNote=new Label{Dock=DockStyle.Top,Height=44,Padding=new Padding(8,4,8,0),ForeColor=PowTheme.Muted};
  var rainBar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=40,Padding=new Padding(6,4,6,0)};var aparri=new Button{Text="Use PAGASA Aparri records (Nov 2020 - Aug 2026)",AutoSize=true};
  aparri.Click+=(s,e)=>{foreach(var p in ContractDurationCalc.AparriRecords())durationRainRecords[p.Key]=(int?[])p.Value.Clone();ShowRainGrid();DurationChanged();};rainBar.Controls.Add(aparri);
  var rainPage=new TabPage("Rainy days");rainPage.Controls.Add(durationRain);rainPage.Controls.Add(durationRainNote);rainPage.Controls.Add(rainBar);views.TabPages.Add(rainPage);
  page.Controls.Add(views);page.Controls.Add(top);tabs.TabPages.Add(page);
  durationStart.ValueChanged+=(s,e)=>{if(durationFilling)return;durationStartDate=durationStart.Value.Date;if(durationHolidayList.Count>0&&durationHolidayList.Max(h=>h.Date.Year)<durationStartDate.Year+1)FillStandardHolidays(false);DurationChanged();};
  durationWorkingDays.ValueChanged+=(s,e)=>{if(durationFilling)return;durationWorkingOverride=(int)durationWorkingDays.Value;DurationChanged();};
  durationHolidays.CellEndEdit+=(s,e)=>{if(!durationFilling){ReadHolidayGrid();DurationChanged();}};durationHolidays.UserDeletedRow+=(s,e)=>{if(!durationFilling){ReadHolidayGrid();DurationChanged();}};
  durationRain.CellEndEdit+=(s,e)=>{if(!durationFilling){ReadRainGrid();DurationChanged();}};durationRain.UserDeletedRow+=(s,e)=>{if(!durationFilling){ReadRainGrid();DurationChanged();}};
  tabs.SelectedIndexChanged+=(s,e)=>{if(tabs.SelectedTab==page)RefreshContractDuration();};}
 void DurationChanged(){dashboardDirty=reportPreviewDirty=true;RefreshContractDuration();}
 int DurationWorkingDays(){if(durationWorkingOverride>0)return durationWorkingOverride;var rows=ScheduleRows();return rows.Where(r=>r.Days>0).Select(r=>r.Finish).DefaultIfEmpty(0).Max();}
 void FillStandardHolidays(bool replace){int from=durationStartDate.Year,to=from+3;var standard=ContractDurationCalc.Standard(from,to);if(replace)durationHolidayList.RemoveAll(h=>h.Date.Year>=from&&h.Date.Year<=to&&standard.Any(x=>x.Date==h.Date));
  foreach(var h in standard)if(!durationHolidayList.Any(x=>x.Date==h.Date))durationHolidayList.Add(h);durationHolidayList.Sort((a,b)=>a.Date.CompareTo(b.Date));ShowHolidayGrid();DurationChanged();}
 void ShowHolidayGrid(){if(durationHolidays==null)return;durationFilling=true;try{durationHolidays.Rows.Clear();foreach(var h in durationHolidayList)durationHolidays.Rows.Add(h.Date.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)+" ("+h.Date.ToString("ddd",CultureInfo.InvariantCulture)+")",h.Name,h.Type);}finally{durationFilling=false;}}
 void ReadHolidayGrid(){var list=new List<DurationHoliday>();foreach(DataGridViewRow r in durationHolidays.Rows){if(r.IsNewRow)continue;string text=Convert.ToString(r.Cells[0].Value).Trim();if(text=="")continue;DateTime d;if(!DateTime.TryParseExact(text.Split(' ')[0],new[]{"yyyy-MM-dd","yyyy-M-d","M/d/yyyy","MM/dd/yyyy"},CultureInfo.InvariantCulture,DateTimeStyles.None,out d)){r.ErrorText="Enter the date as yyyy-mm-dd";continue;}r.ErrorText="";list.Add(new DurationHoliday{Date=d.Date,Name=Convert.ToString(r.Cells[1].Value),Type=Convert.ToString(r.Cells[2].Value)});}
  durationHolidayList.Clear();durationHolidayList.AddRange(list.OrderBy(h=>h.Date));BeginInvoke((Action)ShowHolidayGrid);}
 void ShowRainGrid(){if(durationRain==null)return;durationFilling=true;try{durationRain.Rows.Clear();foreach(var p in durationRainRecords){var cells=new object[13];cells[0]=p.Key;for(int m=0;m<12;m++)cells[m+1]=p.Value[m].HasValue?(object)p.Value[m].Value:"";durationRain.Rows.Add(cells);}}finally{durationFilling=false;}}
 void ReadRainGrid(){durationRainRecords.Clear();foreach(DataGridViewRow r in durationRain.Rows){if(r.IsNewRow)continue;int year;if(!int.TryParse(Convert.ToString(r.Cells[0].Value).Trim(),out year)||year<1900||year>2200){r.ErrorText=Convert.ToString(r.Cells[0].Value).Trim()==""?"":"Enter the year";continue;}r.ErrorText="";var values=new int?[12];
   for(int m=0;m<12;m++){int v;string t=Convert.ToString(r.Cells[m+1].Value).Trim();if(t!=""&&int.TryParse(t,out v)&&v>=0&&v<=31)values[m]=v;}durationRainRecords[year]=values;}}
 void RefreshContractDuration(){if(durationMonths==null||durationMonths.IsDisposed)return;
  if(durationHolidayList.Count==0){foreach(var h in ContractDurationCalc.Standard(durationStartDate.Year,durationStartDate.Year+3))durationHolidayList.Add(h);ShowHolidayGrid();}
  if(durationHolidays.Rows.Count<=1&&durationHolidayList.Count>0)ShowHolidayGrid();if(durationRain.Rows.Count<=1&&durationRainRecords.Count>0)ShowRainGrid();
  int[] used;var rainy=ContractDurationCalc.Averages(durationRainRecords,out used);
  durationRainNote.Text="Days with rainfall above 7.5 mm per month, one row per year (e.g. PAGASA station records). Average used: "+string.Join("  ",CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthNames.Take(12).Select((m,i)=>m+" "+rainy[i]))+(used.Any(u=>u>0&&u<5)?"  (fewer than 5 years for some months)":"")+". The last five recorded years of each month are averaged.";
  durationFilling=true;try{durationMonths.Rows.Clear();int working=DurationWorkingDays();if(durationWorkingDays.Value!=durationWorkingOverride)durationWorkingDays.Value=durationWorkingOverride;
   DurationResult result;try{result=ContractDurationCalc.Compute(durationStartDate,working,durationHolidayList.Select(h=>h.Date),rainy);}catch(Exception ex){durationSummary.Text=ex.Message;return;}
   foreach(var m in result.Months)durationMonths.Rows.Add(m.Year,ContractDurationCalc.MonthNames[m.Month-1],m.Working,m.Sundays,m.Holidays,m.Rainy,m.RevisedRainy,m.TotalDays,m.Weeks);
   int t=durationMonths.Rows.Add("TOTAL","",result.Months.Sum(m=>m.Working),result.Sundays,result.Holidays,"",result.RainyDays,result.Months.Sum(m=>m.TotalDays),result.Months.Sum(m=>m.Weeks));durationMonths.Rows[t].DefaultCellStyle.Font=new Font(durationMonths.Font,FontStyle.Bold);
   durationSummary.Text=working<=0?"No working days yet (construction schedule is empty).":"Workable "+working+" + Sundays "+result.Sundays+" + Holidays "+result.Holidays+" + Rainy days "+result.RainyDays+" = Contract duration "+result.ContractDuration+" calendar days  |  End date "+result.End.ToString("MMMM d, yyyy",CultureInfo.InvariantCulture);}
  finally{durationFilling=false;}}
 void WriteContractDuration(XmlElement schedule){var doc=schedule.OwnerDocument;var e=doc.CreateElement("ContractDuration");e.SetAttribute("start",durationStartDate.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture));if(durationWorkingOverride>0)e.SetAttribute("workingDays",durationWorkingOverride.ToString(CultureInfo.InvariantCulture));
  foreach(var h in durationHolidayList){var x=doc.CreateElement("Holiday");x.SetAttribute("date",h.Date.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture));x.SetAttribute("name",h.Name??"");x.SetAttribute("type",h.Type??"");e.AppendChild(x);}
  foreach(var p in durationRainRecords){var x=doc.CreateElement("Rain");x.SetAttribute("year",p.Key.ToString(CultureInfo.InvariantCulture));x.SetAttribute("days",string.Join(",",p.Value.Select(v=>v.HasValue?v.Value.ToString(CultureInfo.InvariantCulture):"")));e.AppendChild(x);}
  schedule.AppendChild(e);}
 void ReadContractDuration(XmlElement schedule){durationHolidayList.Clear();durationRainRecords.Clear();durationWorkingOverride=0;durationStartDate=DateTime.Today;var e=schedule==null?null:schedule["ContractDuration"];
  if(e!=null){DateTime d;if(DateTime.TryParseExact(e.GetAttribute("start"),"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out d))durationStartDate=d;int w;if(int.TryParse(e.GetAttribute("workingDays"),out w)&&w>0)durationWorkingOverride=w;
   foreach(XmlElement x in e.SelectNodes("Holiday"))if(DateTime.TryParseExact(x.GetAttribute("date"),"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out d))durationHolidayList.Add(new DurationHoliday{Date=d,Name=x.GetAttribute("name"),Type=x.GetAttribute("type")});
   foreach(XmlElement x in e.SelectNodes("Rain")){int year;if(!int.TryParse(x.GetAttribute("year"),out year))continue;var parts=x.GetAttribute("days").Split(',');var values=new int?[12];for(int m=0;m<12&&m<parts.Length;m++){int v;if(int.TryParse(parts[m],out v))values[m]=v;}durationRainRecords[year]=values;}}
  if(durationStart!=null){durationFilling=true;try{durationStart.Value=durationStartDate;durationWorkingDays.Value=durationWorkingOverride;}finally{durationFilling=false;}ShowHolidayGrid();ShowRainGrid();}}
}
}
