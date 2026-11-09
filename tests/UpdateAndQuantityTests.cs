using System;using System.Linq;using System.Threading.Tasks;using System.Collections.Generic;
using BeamScheduleFormatter;
class UpdateAndQuantityTests {
 static int checks;
 static void Check(bool value,string why){if(!value)throw new Exception(why);checks++;}
 static void Near(double a,double b,string why){Check(Math.Abs(a-b)<1e-8,why+": "+a+" != "+b);}
 static async Task Policy(){var p=new StoreUpdatePolicy();var now=new DateTime(2026,11,9,0,0,0,DateTimeKind.Utc);Check(!p.Blocked,"Startup does not wait for network");
  await p.CheckAsync(()=>Task.FromResult(StoreUpdateResult.Required),now);Check(p.Blocked,"Confirmed pending Store update blocks");
  bool called=false;await p.CheckAsync(()=>{called=true;return Task.FromResult(StoreUpdateResult.Current);},now.AddSeconds(1));Check(!called&&p.Blocked,"Repeated ticks are throttled");
  await p.CheckAsync(()=>Task.FromResult(StoreUpdateResult.Unavailable),now.AddSeconds(2),true);Check(!p.Blocked,"Offline retry releases a previous update gate");
  await p.CheckAsync(()=>{throw new Exception("Store unavailable");},now.AddSeconds(3),true);Check(!p.Blocked,"Store errors allow offline work");
  var pending=new TaskCompletionSource<StoreUpdateResult>();var first=p.CheckAsync(()=>pending.Task,now.AddSeconds(4),true);bool duplicate=false;await p.CheckAsync(()=>{duplicate=true;return Task.FromResult(StoreUpdateResult.Required);},now,true);Check(!duplicate,"No overlapping Store checks");pending.SetResult(StoreUpdateResult.Current);await first;Check(!p.Blocked,"Current Store package permits work");
  await p.CheckAsync(()=>Task.FromResult(StoreUpdateResult.NotPackaged),now,true);Check(!p.Blocked,"Portable editions do not acquire a Store lock");
 }
 static void Quantities(){var angle=new SteelCutDemand{Member="A1",Specification="Angle 50x50x5",Length=4,Quantity=1,KgM=3.8};var fascia=new SteelCutDemand{Member="F1",Specification=angle.Specification,Length=1,Quantity=2,KgM=3.8};var stock=SteelCutPlanner.Allocate(new[]{angle,fascia});Check(stock.Count==1,"Fascia reuses both angle offcuts");Near(SteelCutPlanner.PurchaseKg(stock,angle),15.2,"Angle allocation is its share of stock weight");Near(SteelCutPlanner.PurchaseKg(stock,fascia),7.6,"Fascia allocation counts repeated cuts");Near(SteelCutPlanner.PurchaseKg(stock,angle)+SteelCutPlanner.PurchaseKg(stock,fascia),22.8,"Shared 6m stock charged once");
  fascia.Quantity=1;stock=SteelCutPlanner.Allocate(new[]{angle,fascia});Near(SteelCutPlanner.PurchaseKg(stock,angle)+SteelCutPlanner.PurchaseKg(stock,fascia),22.8,"Purchase weight includes final unused length");fascia.Specification="Different finish";stock=SteelCutPlanner.Allocate(new[]{angle,fascia});Check(stock.Count==2,"Different specifications cannot share stock");Near(stock.Sum(s=>6*s.KgM),45.6,"Two whole stocks are charged");
  Check(MouldingTakeoff.Key("Concrete 150 x 50 mm")==MouldingTakeoff.Key("concrete 150×50MM"),"Equivalent moulding size spellings group");Check(MouldingTakeoff.Key("150x50")!=MouldingTakeoff.Key("100x50"),"Different sizes stay separate");Near(new MouldingTakeoff{Id="M1",Description="Cornice",Profile="150x50",Length=4,Quantity=3}.Total,12,"Additional moulding length x qty");bool invalid=false;try{var unused=new MouldingTakeoff{Id="M2",Description="Bad",Profile="",Length=1,Quantity=1}.Total;}catch{invalid=true;}Check(invalid,"Manual moulding requires a size/profile");
  var roof=RoofSheetQuantity.Calculate(5,2,1,1.1,1,0,0,false);Near(roof.BuyArea,11,"Roof purchase area uses full sheet width including side lap");Near(roof.CoveredArea,10,"Net roof area stays distinct from purchase quantity");Near(RoofAccessoryQuantity.Calculate(3,1,.5,2.44,.15).BuyLength,4.88,"Gutter purchase is whole 2.44m stock length");Near(InsulationRollQuantity.Calculate(10.1,1,10).PurchaseArea,20,"Insulation purchase coverage includes whole-roll rounding internally");
  var item=new BillQuantity{Section="Moulding",Description="Concrete 150x50",Quantity=12,Unit="lm"};item.Sources.Add(new BillQuantity{Description="M1",Quantity=12,Unit="lm"});var rows=BillQuantityLayout.Rows(new[]{item});Check(rows.Count==3&&rows[0].Section&&rows[2].Source,"BOQ has section, total, and source directly below");Near(rows[1].Quantity.Value,12,"Source is not added to its total twice");Check(rows[1].Unit=="m","Moulding display uses metres");Check(BillQuantityLayout.Unit("m2")=="sq.m.","Template area unit");Check(BillQuantityLayout.ItemNumber(item)=="","Unconfirmed pay item is not invented");
 }
 [STAThread]static int Main(){try{Policy().GetAwaiter().GetResult();Quantities();
 #if WINDOWS_TESTS
 WindowsUi();
 #endif
 Console.WriteLine("PASS: "+checks+" update-policy, stock-weight and BOQ/moulding assertions");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
 #if WINDOWS_TESTS
 static System.Windows.Forms.DataGridView Grid(object form,string field){return (System.Windows.Forms.DataGridView)form.GetType().GetField(field,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(form);}
 static void WindowsUi(){var context=Type.GetType("Windows.Services.Store.StoreContext, Windows.Services.Store, ContentType=WindowsRuntime",true);var operation=context.GetMethod("GetAppAndOptionalStorePackageUpdatesAsync").ReturnType;Check(operation.GetMethod("GetResults")!=null,"Framework Store async metadata resolves without an SDK build reference");var info=Type.GetType("Windows.Foundation.IAsyncInfo, Windows.Foundation, ContentType=WindowsRuntime",true);Check(info.GetProperty("Status")!=null&&info.GetMethod("Cancel")!=null,"WinRT async status and cancellation metadata resolve");using(var form=new ProgramOfWorksForm()){
  var moulding=Grid(form,"mouldingEntries");moulding.Rows.Add("M1","Cornice","Concrete 150 x 50 mm","4","3");form.Recalculate();Near(Convert.ToDouble(moulding.Rows[0].Cells[5].Value),12,"Manual moulding UI computes");
  var bill=form.BillOfQuantities();Check(bill.Any(q=>q.Section=="Moulding"&&Math.Abs(q.Quantity-12)<1e-8),"BOQ includes manual grouped moulding");
  var tables=form.FinalQuantityTables();Check(!tables.Any(t=>Enumerable.Range(0,t.Rows.GetLength(0)).Any(r=>Convert.ToString(t.Rows[r,0]).Contains("rolls to buy")||Convert.ToString(t.Rows[r,0]).Contains("stock bars to buy"))),"Final Excel exports omit roll/stock counts");
  var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;string xml=(string)form.GetType().GetMethod("DataXml",flags).Invoke(form,null);Check(xml.Contains("version=\"15\"")&&xml.Contains("MouldingEntries"),"New schema persists moulding inputs");
  using(var copy=new ProgramOfWorksForm()){copy.OpenProjectXml(xml);Near(Convert.ToDouble(Grid(copy,"mouldingEntries").Rows[0].Cells[5].Value),12,"Moulding project save/open round-trip");}
  var legacy=new System.Xml.XmlDocument();legacy.LoadXml(xml);legacy.DocumentElement.SetAttribute("version","14");legacy.DocumentElement.RemoveChild(legacy.DocumentElement["MouldingEntries"]);using(var copy=new ProgramOfWorksForm())copy.OpenProjectXml(legacy.OuterXml);

 }}
 #endif
}
