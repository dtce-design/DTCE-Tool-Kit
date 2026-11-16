using System;using System.Linq;using System.Reflection;using System.Windows.Forms;using System.Collections.Generic;using BeamScheduleFormatter;
class PlumbingRoutingTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}static void Near(double a,double b,string why){Check(Math.Abs(a-b)<1e-6,why+" (got "+a+", expected "+b+")");}
 static T Field<T>(object o,string n){return (T)o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).GetValue(o);}
 static object Call(object o,string n,params object[] a){return o.GetType().GetMethod(n,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Invoke(o,a);}
 [STAThread]static void Main(){AccessCodes.Granted=true;using(var form=new ProgramOfWorksForm()){
  var grid=Field<DataGridView>(form,"plumbingChambers");int n=grid.Rows.Add();var r=grid.Rows[n];
  object[] v={"SV1","Septic vault","Septic Vault, Concrete","2","1.5","1.2","1","0.15","0.15","0.15","0.6","0.6","0","0","0","0.1","0.3","0","0","0","1",false,"1",false,""};
  for(int i=0;i<v.Length;i++)r.Cells[i].Value=v[i];
  Call(form,"Recalculate");
  var q=Field<List<ChamberTakeoff>>(form,"chamberMeasures").Single();Check(q.Excavation>0&&q.Backfill>0&&q.GravelVolume>0,"septic vault calculated without an LS quantity: "+Convert.ToString(r.Cells[34].Value));
  var earth=Field<EarthworkBalance>(form,"earthBalance");Near(earth.Excavation,q.Excavation,"septic excavation is in the overall earthworks balance");Near(earth.Fill,q.Backfill,"septic backfill is in the overall earthworks balance");
  Near((double)Call(form,"TotalGravelInstalled"),q.GravelVolume,"septic gravel is in total gravel installed");
  var bill=(List<BillQuantity>)Call(form,"BillOfQuantities");
  Check(!bill.Any(b=>b.Section=="Plumbing Works"&&b.Description=="Septic Vault, Concrete"),"no separate septic lump-sum line");
  var forms=bill.Single(b=>b.Section==ProgramOfWorksForm.PlumbingStructureSection&&b.PayCode=="903(2)");Near(forms.Quantity,q.FormArea,"forms listed under 903(2)");Check(forms.Sources.Single().Description.StartsWith("Septic Vault, Concrete SV1"),"structure listed under the item number");
  Check(bill.Any(b=>b.PayCode=="1027(1)"&&Math.Abs(b.Quantity-q.PlasterArea)<1e-9),"plaster listed under 1027(1)");Check(bill.Any(b=>b.PayCode=="1016(1)b"&&b.Quantity>0),"waterproofing listed under 1016(1)b");
  Check(!bill.Any(b=>b.Section==ProgramOfWorksForm.PlumbingStructureSection&&new[]{"803(1)a","804(1)a","804(7)","900(1)i","902(1)a1"}.Contains(b.PayCode)),"excavation, embankment, gravel, concrete and rebar are not listed twice");
  var concrete=(List<BillQuantity>)Call(form,"ConcretePayQuantities");Near(concrete.Sum(c=>c.Sources.Where(s=>s.Description.Contains("SV1")).Sum(s=>s.Quantity)),q.ConcreteVolume,"septic concrete is in the concrete pay-item groups");
  var gravelBuy=bill.Single(b=>b.Section=="Gravel"&&b.Description.ToUpperInvariant().Contains("TO BUY"));Check(gravelBuy.Sources.Any(s=>s.Description.Contains("SV1")),"septic gravel listed under gravel to buy");
  Console.WriteLine("PASS: "+checks+" canal/septic/catch basin routing into general items");}}
}
