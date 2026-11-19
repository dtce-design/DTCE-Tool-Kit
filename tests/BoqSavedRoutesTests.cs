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
   Check(missing.Count==0&&routes.Count==before.Count&&routes.All(p=>before[p.Key]==p.Value),"relinking the same workbook keeps every mapping as it is");}
  Console.WriteLine("PASS: "+checks+" saved BACKUP mapping checks");}
}
