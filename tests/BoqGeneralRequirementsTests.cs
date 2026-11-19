using System;using System.Collections.Generic;using System.Linq;using System.Reflection;using System.Windows.Forms;using BeamScheduleFormatter;
// Owner (19 November 2026, "part A at Part B allow na ikaw ang maginput ng data sa BOQ ng app" - "Pareho"): the app fills the usual
// Part A / B quantities and every one of them can still be typed over in the Bill of Quantities.
class BoqGeneralRequirementsTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static T Get<T>(object o,string name){return (T)o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).GetValue(o);}
 static void Set(object o,string name,object value){o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).SetValue(o,value);}
 static object Call(object o,string name,params object[] args){return o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Invoke(o,args);}
 static double Final(ProgramOfWorksForm f,BackupBillItem item){var all=f.BackupQuantities().Where(d=>d.TargetSlotId==item.Slot).ToList();double? q=item.ManualQuantity??(all.Any(d=>d.ManualPriority>=1)||BackupMapping.Unit(item.Unit)!="ls"&&all.Count>0?(double?)BackupMapping.FinalQuantity(all,item.Unit):item.WorkbookQuantity);return q??0;}
 [STAThread]static void Main(){Application.EnableVisualStyles();AccessCodes.Granted=true;
  using(var f=new ProgramOfWorksForm()){var bill=Get<List<BackupBillItem>>(f,"backupBill");
   Func<string,string,BackupBillItem> item=(code,word)=>bill.First(b=>b.Code==code&&(word==null||b.Description.IndexOf(word,StringComparison.OrdinalIgnoreCase)>=0));
   Check(!f.BackupQuantities().Any(ProgramOfWorksForm.IsGeneralSuggestion),"an empty project gets no Part A / B quantities");
   var g=Get<DataGridView>(f,"wood");var r=g.Rows[g.Rows.Add()];r.Cells[0].Value="D-01";r.Cells[1].Value="Test door";r.Cells[3].Value="Door";r.Cells[25].Value="Wood";r.Cells[4].Value="0.9";r.Cells[5].Value="2.1";r.Cells[6].Value="2";
   f.Recalculate();Set(f,"projectQuantitiesKnown",true);f.Recalculate();
   /* owner (19 November 2026, "iallow na lang maginput ng qty sa boq ng app for part A and B"): the app does not fill Part A / B; typed quantities go to the same BACKUP item */
   Check(!ProgramOfWorksForm.FillGeneralRequirements&&!f.BackupQuantities().Any(ProgramOfWorksForm.IsGeneralSuggestion),"Part A / B are not filled by the app");
   {var board=item("B.5(1)","DPWH");board.ManualQuantity=2;var mobilization=item("B.9(1)",null);mobilization.ManualQuantity=1;f.Recalculate();
    var typedDemands=f.BackupQuantities().Where(d=>d.ManualOverride&&(d.TargetSlotId==board.Slot||d.TargetSlotId==mobilization.Slot)).ToList();
    Check(typedDemands.Count==2&&typedDemands.Any(d=>d.TargetSlotId==board.Slot&&d.Quantity==2&&d.TargetIdentity==board.Signature),"typed Part A / B quantities are written to their own BACKUP item");
    Call(f,"UpdateBackupBillDashboard");var g0=Get<DataGridView>(f,"boqGrid");var boardRow=g0.Rows.Cast<DataGridViewRow>().First(x=>x.Tag==board);var photoRow=g0.Rows.Cast<DataGridViewRow>().First(x=>x.Tag==item("A.1.4(1)",null));
    Check(!boardRow.Cells[2].ReadOnly&&!photoRow.Cells[2].ReadOnly&&Convert.ToString(boardRow.Cells[2].Value)=="2","Part A / B quantities can be typed in the Bill of Quantities");
    board.ManualQuantity=null;mobilization.ManualQuantity=null;f.Recalculate();}
   /* the earlier fill rules, kept switched off, still give the same values when switched on */
   ProgramOfWorksForm.FillGeneralRequirements=true;
   var suggested=f.BackupQuantities().Where(ProgramOfWorksForm.IsGeneralSuggestion).ToList();Func<BackupBillItem,double> s=b=>suggested.Where(d=>d.TargetSlotId==b.Slot).Sum(d=>d.Quantity);
   Check(s(item("B.3(1)",null))==1&&s(item("B.4(11)","Staking"))==1&&s(item("B.7(1)",null))==1&&s(item("B.9(1)",null))==1,"permits, survey and staking, OSH and mobilization: 1 ls each");
   Check(s(item("B.5(1)","DPWH"))==1&&s(item("B.5(1)","COA"))==1,"one DPWH and one COA billboard");
   Check(s(item("B.11(1)",null))==0&&s(item("B.15(1)",null))==0&&s(item("A.1.1(3)",null))==0&&s(item("A.1.2(1)",null))==0,"provisional sums, detours, field offices and vehicles are left to the engineer");
   var rows=f.ScheduleRows();int days=rows.Where(x=>x.Days>0).Select(x=>x.Finish).DefaultIfEmpty(0).Max();int months=(int)Math.Ceiling(days/30.0-1e-9);
   Check(days>0&&s(item("A.1.4(1)",null))==months,"progress photographs = schedule months ("+days+" days -> "+months+")");
   Check(s(item("A.1.1(16)",null))==0,"field office operation needs a field office");
   item("A.1.1(3)",null).ManualQuantity=1;item("A.1.2(3)",null).ManualQuantity=2;f.Recalculate();suggested=f.BackupQuantities().Where(ProgramOfWorksForm.IsGeneralSuggestion).ToList();
   Check(s(item("A.1.1(16)",null))==months&&s(item("A.1.2(6)",null))==2*months&&s(item("A.1.1(15)",null))==0,"typed field office and vehicles bring their operation months");
   Check(suggested.All(d=>d.Sources.Count==1&&d.Sources[0].Description.StartsWith("Filled by the app")),"each filled quantity explains where it comes from");
   /* typed quantities win; 0 removes; blank returns to the app's value */
   var mob=item("B.9(1)",null);Check(Final(f,mob)==1,"mobilization shows 1 ls");mob.ManualQuantity=0;f.Recalculate();Check(Final(f,mob)==0,"typing 0 removes it");
   var photos=item("A.1.4(1)",null);photos.ManualQuantity=12;f.Recalculate();Check(Final(f,photos)==12,"a typed number of months wins");photos.ManualQuantity=null;f.Recalculate();Check(Final(f,photos)==months,"blank returns to the app's months");
   Call(f,"UpdateBackupBillDashboard");var grid=Get<DataGridView>(f,"boqGrid");var row=grid.Rows.Cast<DataGridViewRow>().First(x=>x.Tag==photos);
   Check(!row.Cells[2].ReadOnly&&Convert.ToString(row.Cells[2].Value)==months.ToString()&&row.Cells[2].ToolTipText.StartsWith("Filled by the app"),"the filled months stay editable in the Bill of Quantities");
   Check(f.ScheduleRows().Any(x=>x.Code=="B.5(1)"&&x.Days>0),"billboards appear in the construction schedule");
   /* "hindi ba pwede paiksiin yung working days": more crews on an item shorten it */
   {var item0=f.ScheduleRows().First(x=>x.Rate>0&&x.Days>0);var overrides=Get<System.Collections.IDictionary>(f,"scheduleOverrides");var type=typeof(ProgramOfWorksForm).Assembly.GetType("BeamScheduleFormatter.ScheduleOverride");
    Func<double,int?,object> make=(rate,crews)=>{var o=Activator.CreateInstance(type,true);type.GetField("Rate",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,(double?)rate);type.GetField("Crews",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,crews);return o;};
    /* output per hour stays fixed at the DUPA value ("fix yung output per hour"); a saved output from v1.32.0 is ignored */
    overrides[item0.Key]=make(item0.Rate/10,null);var fixedRate=f.ScheduleRows().First(x=>x.Key==item0.Key);Check(fixedRate.Rate==item0.Rate&&fixedRate.Days==item0.Days,"output per hour is fixed");
    overrides[item0.Key]=make(0,3);var three=f.ScheduleRows().First(x=>x.Key==item0.Key);
    Check(three.Crews==3&&three.Rate==item0.Rate&&three.Days==(int)Math.Ceiling(item0.Quantity/(item0.Rate*8*3)-1e-9)&&three.Days<=item0.Days,"3 crews: "+item0.Code+" "+item0.Days+" -> "+three.Days+" days");overrides.Remove(item0.Key);}}
  ProgramOfWorksForm.FillGeneralRequirements=false;Console.WriteLine("PASS: "+checks+" Part A / B checks");}
}
