using System;using System.Collections.Generic;using System.Linq;using BeamScheduleFormatter;
// Owner (19 November 2026): Contract Duration tab, checked against the owner's "COMPUTATION OF ESTIMATED PROJECT DURATION" sheet.
class ContractDurationTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 [STAThread]static void Main(){
  /* the owner's PAGASA table (rainfall above 7.5 mm), years 2020-2026 */
  var rec=new Dictionary<int,int?[]>{
   {2020,new int?[]{null,null,null,null,null,null,null,null,null,null,9,13}},
   {2021,new int?[]{8,3,2,0,0,7,6,4,4,6,5,5}},{2022,new int?[]{3,2,1,1,6,4,5,12,5,15,4,16}},{2023,new int?[]{5,7,0,1,3,3,9,5,5,6,8,7}},
   {2024,new int?[]{5,0,0,0,2,2,6,5,11,9,13,8}},{2025,new int?[]{10,6,2,3,4,0,11,7,11,8,14,10}},{2026,new int?[]{9,1,0,1,0,2,9,8,null,null,null,null}}};
  int[] used;var avg=ContractDurationCalc.Averages(rec,out used);
  Check(avg.SequenceEqual(new[]{6,3,1,1,3,2,8,7,7,9,9,9}),"monthly averages of the last five years: "+string.Join(",",avg));
  /* the sheet: start May 3, 2027, 72 working days, holidays in range: Eid'l Adha May 17 and June 12 -> Aug 17, 2027, 107 days */
  var holidays=new[]{new DateTime(2027,5,1),new DateTime(2027,5,17),new DateTime(2027,6,12)};
  var r=ContractDurationCalc.Compute(new DateTime(2027,5,3),72,holidays,avg);
  Check(r.End==new DateTime(2027,8,17),"end date "+r.End.ToString("yyyy-MM-dd"));
  Check(r.Sundays==15&&r.Holidays==2&&r.RainyDays==18&&r.ContractDuration==107,"totals "+r.Sundays+"/"+r.Holidays+"/"+r.RainyDays+"/"+r.ContractDuration);
  var m=r.Months.Select(x=>new[]{x.Working,x.Sundays,x.Holidays,x.Rainy,x.RevisedRainy,x.TotalDays,x.Weeks}).ToList();
  Check(m.Count==4&&m[0].SequenceEqual(new[]{21,4,1,3,3,29,5})&&m[1].SequenceEqual(new[]{23,4,1,2,2,30,5})&&m[2].SequenceEqual(new[]{20,4,0,8,7,31,5})&&m[3].SequenceEqual(new[]{8,3,0,7,6,17,3}),"month rows as in the sheet");
  /* the app's national holiday list gives the same result as the sheet */
  var withStandard=ContractDurationCalc.Compute(new DateTime(2027,5,3),72,ContractDurationCalc.Standard(2027,2028).Select(h=>h.Date),avg);Check(withStandard.End==new DateTime(2027,8,17)&&withStandard.ContractDuration==107,"national holiday list: "+withStandard.End.ToString("yyyy-MM-dd")+" / "+withStandard.ContractDuration);
  /* standard holidays: fixed, Easter-based and National Heroes Day */
  var std=ContractDurationCalc.Standard(2027,2027);
  Check(std.Any(h=>h.Date==new DateTime(2027,3,25)&&h.Name=="Maundy Thursday")&&std.Any(h=>h.Date==new DateTime(2027,3,26)&&h.Name=="Good Friday")&&std.Any(h=>h.Date==new DateTime(2027,8,30)&&h.Name=="National Heroes Day")&&std.Any(h=>h.Date==new DateTime(2027,12,30))&&std.Any(h=>h.Date==new DateTime(2027,6,12)),"national holidays of 2027");
  Check(ContractDurationCalc.Easter(2026)==new DateTime(2026,4,5)&&ContractDurationCalc.Easter(2028)==new DateTime(2028,4,16),"Easter dates");
  /* a holiday on a Sunday is not counted twice */
  var sunday=ContractDurationCalc.Months(new DateTime(2027,6,1),new DateTime(2027,6,30),new HashSet<DateTime>{new DateTime(2027,6,13)},new int[12]);Check(sunday[0].Sundays==4&&sunday[0].Holidays==0,"holiday on a Sunday");
  int[] u2;Check(ContractDurationCalc.Averages(ContractDurationCalc.AparriRecords(),out u2).SequenceEqual(avg),"built-in Aparri records give the sheet averages");
  /* a project without records starts from the last records entered on this computer, else the Aparri records */
  ContractDurationCalc.RainStorePath=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"dtce-rain-"+Guid.NewGuid().ToString("N")+".txt");
  Check(ContractDurationCalc.LastRecords().Count==ContractDurationCalc.AparriRecords().Count,"no saved records: the Aparri records");
  ContractDurationCalc.SaveRecords(new Dictionary<int,int?[]>{{2026,new int?[]{1,2,3,4,5,6,7,8,9,10,11,12}}});var last=ContractDurationCalc.LastRecords();
  Check(last.Count==1&&last[2026][11]==12,"the last records entered are used");System.IO.File.Delete(ContractDurationCalc.RainStorePath);
  /* moving average: a month without the newest year uses the five years before it */
  Check(avg[8]==7&&used[8]==5,"September 2026 not recorded yet: 2021-2025 are averaged");
  /* only rainy-day values entered on or before the POW preparation date are used */
  var entered=ContractDurationCalc.AparriEntered();Check(entered[2026][7]==new DateTime(2026,9,1)&&entered[2021][0]==new DateTime(2021,2,1),"built-in records count as entered after their month");
  var early=ContractDurationCalc.Filter(ContractDurationCalc.AparriRecords(),entered,new DateTime(2026,6,15));Check(!early[2026][5].HasValue&&early[2026][4]==0&&early[2025][11]==10,"a POW prepared on June 15, 2026 uses the records entered by then");
  var rec2=new Dictionary<int,int?[]>(ContractDurationCalc.AparriRecords());var ent2=ContractDurationCalc.AparriEntered();rec2[2026]=(int?[])rec2[2026].Clone();rec2[2026][8]=20;ent2[2026]=(DateTime?[])ent2[2026].Clone();ent2[2026][8]=new DateTime(2026,12,5);
  int[] u3;var before=ContractDurationCalc.Averages(ContractDurationCalc.Filter(rec2,ent2,new DateTime(2026,11,19)),out u3);var after=ContractDurationCalc.Averages(ContractDurationCalc.Filter(rec2,ent2,new DateTime(2026,12,10)),out u3);
  Check(before[8]==7&&after[8]!=7,"a value entered after the POW was prepared does not change its rainy days");
  /* v1.50.19: opening the Contract Duration tab does not mark a new Program of Works as changed (CI hung on the save prompt) */
  {AccessCodes.Granted=true;var F=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;var w=new ProgramOfWorksForm();string saved=(string)typeof(ProgramOfWorksForm).GetField("saved",F).GetValue(w);
   w.Show();System.Windows.Forms.Application.DoEvents();var tabs=(System.Windows.Forms.TabControl)typeof(ProgramOfWorksForm).GetField("tabs",F).GetValue(w);foreach(System.Windows.Forms.TabPage page in tabs.TabPages){tabs.SelectedTab=page;System.Windows.Forms.Application.DoEvents();}
   string now=(string)typeof(ProgramOfWorksForm).GetMethod("DataXml",F).Invoke(w,null);Check(saved==now&&now.Contains("<Holiday "),"a new POW stays unchanged after its tabs are opened; holidays are part of the saved project");w.Hide();}
  /* Progress Photographs follow the Contract Duration of the Bill of Quantities (owner, 23 November 2026) */
  {var F=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;var w=new ProgramOfWorksForm();var T=typeof(ProgramOfWorksForm);
   var bill=(List<BackupBillItem>)T.GetField("backupBill",F).GetValue(w);bill.Clear();var photo=new BackupBillItem{Slot="ph-1",Code="A.1.4(1)",Description="Provision of Progress Photographs",Unit="mo",WorkbookQuantity=9};bill.Add(photo);
   T.GetField("durationStartAuto",F).SetValue(w,false);T.GetField("durationStartDate",F).SetValue(w,new DateTime(2027,5,3));T.GetField("durationWorkingOverride",F).SetValue(w,72);
   Func<List<BackupDemand>> q=()=>(List<BackupDemand>)T.GetMethod("BackupQuantities",F).Invoke(w,new object[]{false});
   var rainyArgs=new object[]{null};var rainy=(int[])T.GetMethod("DurationRainy",F).Invoke(w,rainyArgs);var hol=(List<DurationHoliday>)T.GetField("durationHolidayList",F).GetValue(w);
   int days=ContractDurationCalc.Compute(new DateTime(2027,5,3),72,hol.Select(h=>h.Date),rainy).ContractDuration;var d=q().Where(x=>x.TargetSlotId=="ph-1").ToList();
   var r107=ContractDurationCalc.Compute(new DateTime(2027,5,3),72,hol.Select(h=>h.Date),rainy);Check(days>72&&d.Count==1&&d[0].Quantity==Math.Round(ProgramOfWorksForm.PhotoMonths(new DateTime(2027,5,3),r107.End),2)&&d[0].Quantity!=9,"Progress Photographs = Contract Duration "+days+" days / 30 = "+(d.Count==1?d[0].Quantity.ToString():"none")+" months, not the BACKUP value");
   photo.ManualQuantity=2;Check(!q().Any(x=>x.TargetSlotId=="ph-1"&&x.Key.StartsWith(ProgramOfWorksForm.ProgressPhotoPrefix)),"a quantity typed in the app wins over the Contract Duration");photo.ManualQuantity=null;
   T.GetField("durationWorkingOverride",F).SetValue(w,0);d=q().Where(x=>x.TargetSlotId=="ph-1").ToList();Check(d.Count==1&&d[0].Quantity==9,"no Contract Duration yet: the BACKUP quantity is kept");}
  /* the owner's POW workbook: FRONT PAGE!CB7 start date and BACKUP!N64 months of Progress Photographs */
  Check(ProgramOfWorksForm.FormulaStart(new DateTime(2026,11,12),4950000m,2027)==new DateTime(2027,5,3),"start = first Monday on or after May 1, 2027 (prepared + 90 days is earlier)");
  Check(ProgramOfWorksForm.FormulaStart(new DateTime(2027,3,1),60000000m,2027)==new DateTime(2027,5,15)&&ProgramOfWorksForm.FormulaStart(new DateTime(2027,3,1),1000000m,2027)==new DateTime(2027,5,30),"prepared + 75 days (ABC of PHP 50 M or more) or + 90 days");
  Check(ProgramOfWorksForm.FormulaStart(new DateTime(2026,11,12),0m,0)==new DateTime(2027,5,3),"FY 0 = the year after the date prepared");
  Check(Math.Abs(ProgramOfWorksForm.PhotoMonths(new DateTime(2027,5,3),new DateTime(2027,8,27))-3.774193548387097)<1e-9,"117 calendar days = 3.774 months as BACKUP!N64");
  Check(Math.Abs(ProgramOfWorksForm.PhotoMonths(new DateTime(2027,1,31),new DateTime(2027,3,1))-(1+1/31.0))<1e-9||Math.Abs(ProgramOfWorksForm.PhotoMonths(new DateTime(2027,1,31),new DateTime(2027,3,1))-1.0357142857)<1e-6,"month-end start dates");
  Console.WriteLine("PASS: "+checks+" contract duration checks");}
}
