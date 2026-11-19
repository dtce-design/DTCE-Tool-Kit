using System;using System.Collections.Generic;using System.Linq;using System.Reflection;using System.Windows.Forms;using BeamScheduleFormatter;
// Owner (19 November 2026): "huwag buburahin, yung naka save na mapping ang susundin sa dtce app" / "ang susundan e yung nasa pow na
// app hindi sa excel" - linking or refreshing the BACKUP workbook keeps the Bill of Quantities mapping saved in the Program of Works.
class BoqSavedRoutesTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static T Get<T>(object o,string name){return (T)o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).GetValue(o);}
 static void Set(object o,string name,object value){o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).SetValue(o,value);}
 static object Call(object o,string name,params object[] args){return o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Invoke(o,args);}
 static List<BackupTarget> Targets(string prefix,int shift,params int[] skip){var items=new[]{new[]{"1032(1)a","Painting (Concrete)","sq.m"},new[]{"1032(1)c","Painting (Metal)","sq.m"},new[]{"1018(1)","Glazed Tiles","sq.m"},new[]{"1004(2)","Finishing Hardware","l.s."},new[]{"1200(1)","Plumbing Fixtures","l.s."}};
  var list=new List<BackupTarget>();for(int i=0;i<items.Length;i++){if(skip.Contains(i))continue;list.Add(new BackupTarget{Row=40+i*3+shift,EndRow=40+i*3+shift,Code=items[i][0],Description=items[i][1],Unit=items[i][2],SlotId=prefix+i});}return list;}
 [STAThread]static void Main(){Application.EnableVisualStyles();AccessCodes.Granted=true;
  using(var f=new ProgramOfWorksForm()){
   var oldTargets=Targets("old-",0);Set(f,"backupBill",BackupBill.Capture(oldTargets,new List<BackupBillItem>()));
   var routes=Get<Dictionary<string,string>>(f,"backupBillRoutes");var overrides=Get<Dictionary<string,string>>(f,"boqRouteOverrides");var unmapped=Get<HashSet<string>>(f,"boqUnmapped");
   var includes=Get<Dictionary<string,bool>>(f,"boqIncludes");var lsAuto=Get<HashSet<string>>(f,"lsAutoSlots");
   const string metal="Structural Steel Works|Metal painting - purlins|m2",tiles="Finishes|Glazed tiles|m2",hardware="Doors & Windows|Leversets|set",gone="Finishes|Unmapped by hand|m2";
   routes[metal]="old-1";routes[tiles]="old-2";routes[hardware]="old-3";routes[gone]="old-0";overrides[hardware]="old-3";unmapped.Add(gone);overrides[gone]=null;
   includes[ProgramOfWorksForm.LsIncludePrefix+"old-4"]=false;lsAuto.Add("old-4");
   var saved=Call(f,"CaptureSavedBoqMapping");
   /* a copy of the workbook without the app's names: every item gets a new id, rows moved, tiles removed */
   var newTargets=Targets("new-",2,2);var bill=BackupBill.Capture(newTargets,new List<BackupBillItem>());Set(f,"backupBill",bill);
   routes.Clear();routes[metal]="new-0";/* the workbook suggests another item */
   var missing=(List<string>)Call(f,"RestoreSavedBoqMapping",saved);
   Check(routes[metal]=="new-1"&&overrides[metal]=="new-1","the app's mapping wins over the workbook's suggestion and follows the moved item");
   Check(routes[hardware]=="new-3"&&overrides[hardware]=="new-3","a manual override follows its item to the new id");
   Check(missing.Count==1&&missing[0]==tiles&&!routes.ContainsKey(tiles),"a mapping whose item is missing in the workbook is reported, not moved to another item");
   Check(!routes.ContainsKey(gone)&&overrides.ContainsKey(gone)&&overrides[gone]==null&&unmapped.Contains(gone),"an item unmapped by hand stays unmapped");
   Check(includes.ContainsKey(ProgramOfWorksForm.LsIncludePrefix+"new-4")&&!includes[ProgramOfWorksForm.LsIncludePrefix+"new-4"]&&!includes.ContainsKey(ProgramOfWorksForm.LsIncludePrefix+"old-4")&&lsAuto.Contains("new-4")&&!lsAuto.Contains("old-4"),"LS include choices follow their item");
   var plan=new BackupWritePlan{Demands=new List<BackupDemand>{new BackupDemand{Key=metal,Description="Metal painting - purlins",Unit="m2",Quantity=25}},Targets=newTargets,Routes=new Dictionary<string,BackupTarget>{{metal,newTargets.First(t=>t.SlotId=="new-0")}}};
   Call(f,"ApplyBoqRouteOverrides",plan);Check(plan.Routes[metal].SlotId=="new-1","the next write to Excel uses the app's mapping");
   /* the same workbook again (ids kept): nothing changes */
   saved=Call(f,"CaptureSavedBoqMapping");var before=new Dictionary<string,string>(routes);missing=(List<string>)Call(f,"RestoreSavedBoqMapping",saved);
   Check(missing.Count==0&&routes.Count==before.Count&&routes.All(p=>before[p.Key]==p.Value),"relinking the same workbook keeps every mapping as it is");
   /* v1.30.0 report: a mapping made on the app's catalogue (other wording) is kept when the BACKUP workbook is linked */
   var catalogue=new List<BackupTarget>{new BackupTarget{Row=0,EndRow=0,Code="1032(1)c",Description="Painting Works, Metal",Unit="sq.m.",SlotId="cat-0"},new BackupTarget{Row=0,EndRow=0,Code="1018(1)",Description="Glazed Tiles and Trims",Unit="sq.m.",SlotId="cat-1"},new BackupTarget{Row=0,EndRow=0,Code="1200(1)",Description="Plumbing Fixtures",Unit="l.s.",SlotId="cat-2"}};
   Set(f,"backupBill",BackupBill.Capture(catalogue,new List<BackupBillItem>()));routes.Clear();overrides.Clear();unmapped.Clear();includes.Clear();lsAuto.Clear();routes[metal]="cat-0";routes[tiles]="cat-1";overrides[tiles]="cat-1";
   saved=Call(f,"CaptureSavedBoqMapping");
   var book=new List<BackupTarget>{new BackupTarget{Row=40,EndRow=40,Code="1032 (1) c",Description="Painting (Metal)",Unit="m2",SlotId="wb-0"},new BackupTarget{Row=43,EndRow=43,Code="1018(1)",Description="Glazed Tiles",Unit="sq.m.",SlotId="wb-1"},new BackupTarget{Row=46,EndRow=46,Code="1018(1)",Description="Glazed Tiles",Unit="ea",SlotId="wb-2"}};
   Set(f,"backupBill",BackupBill.Capture(book,new List<BackupBillItem>()));routes.Clear();
   missing=(List<string>)Call(f,"RestoreSavedBoqMapping",saved);
   Check(missing.Count==0&&routes[metal]=="wb-0"&&routes[tiles]=="wb-1"&&overrides[tiles]=="wb-1","a mapping made with other wording follows the item no. and unit of the linked workbook");
   var typed=BackupBill.Capture(catalogue,new List<BackupBillItem>());typed[0].ManualQuantity=12;
   var rebound=(List<BackupBillItem>)typeof(ProgramOfWorksForm).GetMethod("RebindCatalogueManuals",BindingFlags.Static|BindingFlags.NonPublic,null,new[]{typeof(List<BackupTarget>),typeof(List<BackupBillItem>)},null).Invoke(null,new object[]{book,typed});
   Check(rebound.Count==1&&rebound[0].Slot=="wb-0"&&rebound[0].ManualQuantity==12,"a typed quantity follows its item into a workbook with other wording");
   var lostTyped=new List<BackupBillItem>();var typedMissing=BackupBill.Capture(catalogue,new List<BackupBillItem>());typedMissing[2].ManualQuantity=1;
   var kept=(List<BackupBillItem>)typeof(ProgramOfWorksForm).GetMethod("RebindCatalogueManuals",BindingFlags.Static|BindingFlags.NonPublic,null,new[]{typeof(List<BackupTarget>),typeof(List<BackupBillItem>),typeof(List<BackupBillItem>)},null).Invoke(null,new object[]{book,typedMissing,lostTyped});
   Check(kept.Count==0&&lostTyped.Count==1&&lostTyped[0].Code=="1200(1)","a typed quantity whose item is not in the workbook is reported instead of stopping the link");
   /* Excel busy (edit mode / dialog): the call is tried again instead of pausing the sync */
   int calls=0;int result=ExcelBusyFilter.Retry(()=>{calls++;if(calls<3)throw new System.Runtime.InteropServices.COMException("busy",ExcelBusyFilter.ExcelBusy);return 7;});
   Check(result==7&&calls==3,"a busy Excel call is retried");bool other=false;try{ExcelBusyFilter.Retry<int>(()=>{throw new InvalidOperationException("x");});}catch(InvalidOperationException){other=true;}Check(other,"other errors are not retried");
   var filter=new ExcelBusyFilter();Check(filter.RetryRejectedCall(IntPtr.Zero,1000,2)==250&&filter.RetryRejectedCall(IntPtr.Zero,ExcelBusyFilter.RetryMilliseconds+1,2)==-1&&filter.RetryRejectedCall(IntPtr.Zero,0,1)==-1,"rejected calls wait and retry for up to a minute");
   /* "naka uncheck yung check box pero may qty": an LS item whose mapped quantities are all unchecked is 0, not the workbook's old quantity */
   var lsBill=BackupBill.Capture(new List<BackupTarget>{new BackupTarget{Row=50,EndRow=50,Code="1200(1)",Description="Plumbing Fixtures",Unit="l.s.",SlotId="ls-0"}},new List<BackupBillItem>());lsBill[0].WorkbookQuantity=5;Set(f,"backupBill",lsBill);
   routes.Clear();overrides.Clear();unmapped.Clear();includes.Clear();lsAuto.Clear();const string closet="Plumbing|Water closet|set";routes[closet]="ls-0";includes[closet]=false;
   var lsList=new List<BackupDemand>{new BackupDemand{Key=closet,Description="Water closet",Unit="set",Quantity=4}};Call(f,"ApplyBoqIncludes",lsList);
   var control=lsList.FirstOrDefault(d=>d.Key==ProgramOfWorksForm.LsIncludePrefix+"ls-0");
   Check(control!=null&&control.Quantity==0&&control.ManualPriority==1&&lsList[0].Quantity==0&&BackupMapping.FinalQuantity(lsList.Where(d=>d.TargetSlotId=="ls-0"||d.Key==closet),"l.s.")==0,"unchecking every mapped quantity of an LS item gives 0 LS");
   /* "The manual BOQ item was removed or changed: Project Billboard" when mapping: a BACKUP row without the app's name keeps the same id on every read */
   string id1=ExcelLiveLink.StableSlotId(120,"B.5(1)","Project Billboard / Signboard (DPWH Billboard)","ea"),id2=ExcelLiveLink.StableSlotId(120,"B.5(1)","Project Billboard / Signboard (DPWH Billboard)","ea");
   Check(id1==id2&&id1.Length==32&&id1.All(c=>"0123456789abcdef".IndexOf(c)>=0)&&id1!=ExcelLiveLink.StableSlotId(121,"B.5(1)","Project Billboard / Signboard (COA Billboard)","ea"),"BACKUP rows without a name keep a stable id between reads");
   /* "hindi pa din niya namamap sa BACKUP": a typed quantity whose item is read again under another id is still written to it */
   var typedDemand=new BackupDemand{Key="BOQ manual|old-id",Description="Project Billboard",Unit="ea",Quantity=2,TargetSlotId="old-id",TargetIdentity=BackupMapping.Identity(new BackupTarget{Code="B.5(1)",Description="Project Billboard",Unit="ea"}),ManualOverride=true,ManualPriority=2};
   var reread=new List<BackupTarget>{new BackupTarget{Row=60,EndRow=60,Code="B.5(1)",Description="Project Billboard",Unit="ea",SlotId="new-id"},new BackupTarget{Row=63,EndRow=63,Code="B.5(1)",Description="COA Billboard",Unit="ea",SlotId="coa"}};
   var resolved=BackupMapping.ResolveTarget(typedDemand,reread);bool valid=true;try{BackupMapping.Validate(new[]{typedDemand},new Dictionary<string,BackupTarget>{{typedDemand.Key,resolved}});}catch(Exception){valid=false;}
   Check(resolved!=null&&resolved.SlotId=="new-id"&&valid,"a typed quantity follows its BACKUP item read under another id and passes the write check");
   /* owner's Book1.xlsm: units come from another workbook through external links and read as "ls"; the mapping follows item no. and description */
   var withUnits=new List<BackupTarget>{new BackupTarget{Row=437,EndRow=437,Code="903(2)",Description="Formworks and Falseworks",Unit="sq.m.",SlotId="f-1"},new BackupTarget{Row=452,EndRow=452,Code="903(2)",Description="Formworks and Falseworks (Steel Decking)",Unit="sq.m.",SlotId="f-2"}};
   Set(f,"backupBill",BackupBill.Capture(withUnits,new List<BackupBillItem>()));routes.Clear();overrides.Clear();unmapped.Clear();includes.Clear();lsAuto.Clear();const string beamForms="Reinforced Concrete Works|Beam forms|m2";routes[beamForms]="f-1";
   saved=Call(f,"CaptureSavedBoqMapping");
   var lsUnits=new List<BackupTarget>{new BackupTarget{Row=437,EndRow=437,Code="903(2)",Description="Formworks and Falseworks",Unit="ls",SlotId="g-1"},new BackupTarget{Row=452,EndRow=452,Code="903(2)",Description="Formworks and Falseworks (Steel Decking)",Unit="ls",SlotId="g-2"}};
   Set(f,"backupBill",BackupBill.Capture(lsUnits,new List<BackupBillItem>()));routes.Clear();missing=(List<string>)Call(f,"RestoreSavedBoqMapping",saved);
   Check(missing.Count==0&&routes[beamForms]=="g-1","a mapping follows its item when the workbook reads another unit");
   /* "kapag nagseselect ako kung saan imamap di naalis sa status" / "yung mga not mapped di pwedeng mamili": an app quantity with its own
      pay item (not found in this workbook) can be mapped to another item, and that choice is used in the BOQ and in the write */
   var slabForms=new BackupDemand{Key="Reinforced Concrete Works|Slab forms|m2",Description="Slab forms",Unit="m2",Quantity=13.44,TargetSlotId="catalogue-slab",TargetIdentity="no-such-item"};
   Check(BackupMapping.ResolveTarget(slabForms,lsUnits)==null,"its own pay item is not in the workbook");
   bool accepted=true;try{BackupMapping.Validate(new[]{slabForms},new Dictionary<string,BackupTarget>{{slabForms.Key,lsUnits[0]}});}catch(Exception){accepted=false;}Check(accepted,"the mapping dialog accepts another item for an app quantity");
   overrides[slabForms.Key]="g-1";var listed=new List<BackupDemand>{slabForms};List<BackupDemand> notMapped;var byItem=(Dictionary<string,List<BackupDemand>>)typeof(ProgramOfWorksForm).GetMethod("SourcesByItem",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(f,new object[]{listed,null});
   var args0=new object[]{listed,null};byItem=(Dictionary<string,List<BackupDemand>>)typeof(ProgramOfWorksForm).GetMethod("SourcesByItem",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(f,args0);notMapped=(List<BackupDemand>)args0[1];
   Check(notMapped.Count==0&&byItem.ContainsKey("g-1"),"the chosen item is used and the quantity leaves the Not mapped list");
   var plan2=new BackupWritePlan{Demands=new List<BackupDemand>{slabForms},Targets=lsUnits,Routes=new Dictionary<string,BackupTarget>()};Call(f,"ApplyBoqRouteOverrides",plan2);
   Check(plan2.Routes.ContainsKey(slabForms.Key)&&plan2.Routes[slabForms.Key].SlotId=="g-1"&&slabForms.TargetSlotId==null,"the chosen item is written to Excel");
   /* "lahat ng formworks ... formworks and falseworks" / RSB Grade 40 / 60: the app's own pay items are found when the workbook shows "ls" */
   var bookItems=new List<BackupTarget>{new BackupTarget{Code="902(1)a1",Description="Reinforcing Steel (Deformed), Grade 40",Unit="ls",SlotId="a1"},new BackupTarget{Code="902(1)a2",Description="Reinforcing Steel (Deformed), Grade 60, max 25mm dia.",Unit="ls",SlotId="a25"},new BackupTarget{Code="902(1)a2",Description="Reinforcing Steel (Deformed), Grade 60, max 42mm dia.",Unit="ls",SlotId="a42"},new BackupTarget{Code="903(2)",Description="Formworks and Falseworks",Unit="ls",SlotId="fw"},new BackupTarget{Code="903(2)",Description="Formworks and Falseworks (Steel Decking)",Unit="ls",SlotId="sd"}};
   Func<string,string,string,string> pay=(c,d,u)=>{var t=RequiredPayRouting.Resolve(c,d,u,bookItems);return t==null?null:t.SlotId;};
   Check(pay("903(2)","Formworks and Falseworks","m2")=="fw"&&pay("902(1)a1","Reinforcing Steel (Deformed), Grade 40","kg")=="a1"&&pay("902(1)a2","Reinforcing Steel (Deformed), Grade 60, max 25mm dia.","kg")=="a25"&&pay("902(1)a2","Reinforcing Steel (Deformed), Grade 60, max 42mm dia.","kg")=="a42","forms and RSB find their pay items in a workbook that shows ls");
   Check(SiteWorkTakeoff.Grade60Item(new[]{16,20})=="Reinforcing Steel (Deformed), Grade 60, max 25mm dia."&&SiteWorkTakeoff.Grade60Item(new[]{16,28})=="Reinforcing Steel (Deformed), Grade 60, max 42mm dia.","Grade 60 goes to max 42mm when a bar larger than 25mm is used");
   /* "hindi ls ang yunit niyan kg ang unit niyan": a catalogue item read as ls uses its catalogue unit; true LS items stay LS; names written with ls still match */
   Check(BackupMapping.Unit(BackupMapping.EffectiveUnit("902(1)a1","Reinforcing Steel (Deformed), Grade 40","ls"))=="kg"&&BackupMapping.Unit(BackupMapping.EffectiveUnit("903(2)","Formworks and Falseworks","ls"))=="m2"&&BackupMapping.Unit(BackupMapping.EffectiveUnit("B.9(1)","Mobilization / Demobilization","ls"))=="ls"&&BackupMapping.Unit(BackupMapping.EffectiveUnit("999(9)","Owner item","ls"))=="ls","units read through external links");
   var rsb=new BackupTarget{Code="902(1)a1",Description="Reinforcing Steel (Deformed), Grade 40",Unit=BackupMapping.EffectiveUnit("902(1)a1","Reinforcing Steel (Deformed), Grade 40","ls"),ReadUnit="ls",SlotId="r",Row=395};
   string oldIdentity=BackupMapping.Identity(new BackupTarget{Code="902(1)a1",Description="Reinforcing Steel (Deformed), Grade 40",Unit="ls"});
   Check(BackupMapping.SameIdentity(rsb,oldIdentity)&&BackupMapping.ResolveSaved(395,oldIdentity,new[]{rsb})==rsb,"mappings saved in the workbook with the ls unit still resolve");
   Check(BackupMapping.FinalQuantity(new[]{new BackupDemand{Key="a",Unit="kg",Quantity=8153.655},new BackupDemand{Key="b",Unit="kg",Quantity=603.396}},rsb.Unit)>8757&&!BackupMapping.BasisOnly(rsb,new[]{new BackupDemand{Key="a",Unit="kg",Quantity=1}}),"kg quantities are written to the RSB item, not kept as LS basis");}
  Console.WriteLine("PASS: "+checks+" saved BACKUP mapping checks");}
}
