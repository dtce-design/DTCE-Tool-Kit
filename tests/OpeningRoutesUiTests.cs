using System;using System.Collections.Generic;using System.Linq;using System.Reflection;using System.Windows.Forms;using BeamScheduleFormatter;
// Owner (18 November 2026): leversets / hinges and doors / windows are mapped in the Bill of Quantities automatically;
// a mapping the user changed stays as chosen.
class OpeningRoutesUiTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}static T Get<T>(object o,string name){return (T)o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);}
 [STAThread]static void Main(){AccessCodes.Granted=true;using(var f=new ProgramOfWorksForm()){var g=Get<DataGridView>(f,"wood");
  Action<string,string,string> add=(id,kind,mat)=>{var r=g.Rows[g.Rows.Add()];r.Cells[0].Value=id;r.Cells[1].Value="Test "+mat;r.Cells[3].Value=kind;r.Cells[25].Value=mat;r.Cells[4].Value="0.9";r.Cells[5].Value="2.1";r.Cells[6].Value="2";};
  add("D-01","Door","Wood");add("D-02","Door","Aluminum");add("W-01","Window","Aluminum");add("D-03","Door","PVC");add("D-04","Door","Steel");add("W-04","Window","Steel");f.Recalculate();
  var bill=Get<List<BackupBillItem>>(f,"backupBill");var routes=Get<Dictionary<string,string>>(f,"backupBillRoutes");var demands=f.BackupQuantities();
  Func<string,string> code=prefix=>{var d=demands.FirstOrDefault(x=>x.Key.StartsWith(prefix,StringComparison.Ordinal));if(d==null||!routes.ContainsKey(d.Key))return "";return bill.First(b=>b.Slot==routes[d.Key]).Code;};
  Check(code("Doors & Windows|Leversets|")=="1004(2)"&&code("Doors & Windows|Hinges|")=="1004(2)","Leversets and hinges go to Finishing Hardware");
  Check(code("Door / window schedule|D-01 |")=="1010(4)"&&code("Door / window jambs|D-01 |")=="1010(4)","Wood door and jamb go to Wooden Doors and Windows");
  Check(code("Door / window schedule|D-02 |")=="1007(2)"&&code("Door / window schedule|W-01 |")=="1008(2)","Aluminum door / window items");
  Check(code("Door / window schedule|D-03 |")=="1043(1)"&&code("Door / window schedule|D-04 |")=="1006(6)"&&code("Door / window schedule|W-04 |")=="1005(5)","PVC door, steel door and steel window items");
  string key=demands.First(x=>x.Key.StartsWith("Doors & Windows|Hinges|",StringComparison.Ordinal)).Key;string other=bill.First(b=>b.Code=="1004(1)").Slot;
  typeof(ProgramOfWorksForm).GetMethod("SetBoqRoute",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(f,new object[]{key,other});f.BackupQuantities();Check(routes[key]==other,"A mapping changed by the user is kept");
  typeof(ProgramOfWorksForm).GetMethod("SetBoqRoute",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(f,new object[]{key,null});f.BackupQuantities();Check(!routes.ContainsKey(key),"An unmapped quantity stays unmapped");
  /* "Opening" type (owner, 18 November 2026): dimensions only - no material, hardware or BOQ item */
  {var op=g.Rows[g.Rows.Add()];op.Cells[0].Value="OP-1";op.Cells[1].Value="";op.Cells[3].Value="Opening";op.Cells[4].Value="1.2";op.Cells[5].Value="1";op.Cells[6].Value="2";f.Recalculate();
   Check(Convert.ToString(op.Cells[19].Value)=="Ready"&&Convert.ToString(op.Cells[25].Value)==""&&op.Cells[25].ReadOnly,"Opening needs no material or description");Check(Convert.ToString(op.Cells[13].Value)=="2.40"&&Convert.ToString(op.Cells[17].Value)=="0"&&Convert.ToString(op.Cells[18].Value)=="0","Opening area without hardware");
   Check(!f.BackupQuantities().Any(x=>x.Key.Contains("OP-1")),"Opening has no BOQ item");}
  /* Construction Schedule (owner, 18 November 2026): working days = quantity / (output per hour x 8 h x crews) */
  {var rows=(List<ScheduleRow>)typeof(ProgramOfWorksForm).GetMethod("ScheduleRows",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(f,null);var pvc=rows.FirstOrDefault(r=>r.Code=="1043(1)");
   Check(pvc!=null&&Math.Abs(pvc.Rate-0.315)<1e-9&&pvc.Days==(int)Math.Ceiling(pvc.Quantity/(0.315*8)-1e-9),"PVC door working days from the DUPA output per hour");var pay=(System.Collections.IList)typeof(ProgramOfWorksForm).GetMethod("SchedulePayQuantities",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(f,null);int lsPay=pay.Cast<object>().Count(p=>ScheduleRow.IsLumpSum(((BackupBillItem)p.GetType().GetProperty("Key").GetValue(p,null)).Unit));
   Check(rows.Count(r=>ScheduleRow.IsLumpSum(r.Unit))==lsPay&&rows.Where(r=>ScheduleRow.IsLumpSum(r.Unit)).All(r=>r.Rate==0&&r.Days==0),"LS items are listed with the output per hour left blank ("+lsPay+")");
   var net=PdmNetwork.Build(rows);Check(net.Nodes.Count>0&&net.Nodes.Min(n=>n.ES)==0&&net.Nodes.All(n=>n.EF==n.ES+n.Row.Days&&n.TF==n.LS-n.ES&&n.LS==n.LF-n.Row.Days),"PDM uses the standard ES = 0, EF = ES + D, TF = LS - ES convention");
   Check(!net.Nodes.Any(n=>ScheduleRow.IsLumpSum(n.Row.Unit)),"LS items without a duration stay out of the network");
   var pert=PertResult.Build(net);Check(pert.Activities.All(a=>Math.Abs(a.A-0.8*a.M)<1e-9&&Math.Abs(a.B-1.2*a.M)<1e-9),"PERT optimistic / pessimistic = -/+20 %");
   var crew=ScheduleManpower.Parse("Foreman x1; Skilled Laborer x2; Unskilled Laborer x4; Foreman x1");Check(crew.Sum(k=>k.Value)==8&&ScheduleManpower.Compact("Foreman x1; Skilled Laborer x2; Foreman x1")=="2 Foreman, 2 Skilled Laborer","DUPA crew parsed and same trades added");
   var mr=new List<ScheduleRow>{new ScheduleRow{Code="A",Unit="m",Days=3,Start=1,Finish=3,Crews=2,Crew="Foreman x1; Unskilled Laborer x2"},new ScheduleRow{Code="B",Unit="m",Days=2,Start=2,Finish=3,Crews=1,Crew="Foreman x1; Skilled Laborer x3"},new ScheduleRow{Code="C",Unit="ls",Days=0,Crew="Foreman x1"}};
   var md=ScheduleManpower.Daily(mr);Check(md.Length==4&&md[1]==6&&md[2]==10&&md[3]==10&&ScheduleManpower.ManDays(mr[0])==18&&ScheduleManpower.Men(mr[2])==0,"daily manpower = DUPA crew x crews on each working day; LS not counted");
   var tr=ScheduleManpower.Trades(mr);Check(tr.First(t=>t.Trade=="Foreman").Peak==3&&tr.Sum(t=>t.ManDays)==md.Sum(),"manpower per trade adds up to the daily totals");
   var set=new ScheduleSet(rows,null);Check(set.Sheets.Count>=4&&ScheduleSet.PageW==1654&&ScheduleSet.PageH==1169&&set.Sheets.Any(k=>k.Key=="MANPOWER LOADING")&&set.Sheets.Any(k=>k.Key=="MANPOWER SCHEDULE"),"issue set on A3 landscape with the manpower sheets");}}
  Console.WriteLine("PASS: "+checks+" automatic door / window BOQ mapping checks");}
}
