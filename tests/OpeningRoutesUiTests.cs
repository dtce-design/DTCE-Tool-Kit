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
  typeof(ProgramOfWorksForm).GetMethod("SetBoqRoute",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(f,new object[]{key,null});f.BackupQuantities();Check(!routes.ContainsKey(key),"An unmapped quantity stays unmapped");}
  Console.WriteLine("PASS: "+checks+" automatic door / window BOQ mapping checks");}
}
