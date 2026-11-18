using System;using System.Collections.Generic;using System.Linq;using BeamScheduleFormatter;
// Owner (17 November 2026): BACKUP mapping may be partial. Unmapped quantities and LS items without a manual LS
// quantity are left unmapped instead of blocking the mapping.
class BackupPartialMappingTests {
 static int checks;static void Check(bool ok,string m){checks++;if(!ok)throw new Exception(m);}
 static void Main(){
  var lsItem=new BackupTarget{Row=40,EndRow=40,Code="1047(5)",Description="Finishing Hardware",Unit="l.s.",SlotId="A"};var kgItem=new BackupTarget{Row=10,EndRow=10,Code="1047",Description="Steel",Unit="kg",SlotId="B"};
  var hardware=new BackupDemand{Key="Doors|Hardware",Description="Locksets and hinges",Unit="pcs",Quantity=12};var steel=new BackupDemand{Key="Steel|kg",Description="Steel kg",Unit="kg",Quantity=100};var loose=new BackupDemand{Key="Other|m2",Description="Unmapped",Unit="m2",Quantity=5};
  var demands=new List<BackupDemand>{hardware,steel,loose};var map=new Dictionary<string,BackupTarget>{{hardware.Key,lsItem},{steel.Key,kgItem}};
  bool threw=false;try{BackupMapping.Validate(demands,map);}catch(Exception){threw=true;}Check(threw,"Full validation reports the unselected quantity");
  var left=BackupMapping.DropIncomplete(demands,map);
  Check(left.Count==1&&left.Any(x=>x.Key==loose),"Only the unselected quantity is left unmapped");Check(map.ContainsKey(hardware.Key)&&BackupMapping.BasisOnly(lsItem,new[]{hardware}),"LS mapping kept as basis; no LS quantity written without a manual LS quantity");
  Check(map.Count==2&&map.ContainsKey(steel.Key),"Complete routes are kept");Check(!BackupMapping.BasisOnly(lsItem,new[]{hardware,new BackupDemand{Key="Bill of Quantities|x",Description="Finishing Hardware",Unit="l.s.",Quantity=1}}),"Written once a manual LS quantity exists");
  BackupMapping.Validate(demands.Where(d=>map.ContainsKey(d.Key)),map);Check(true,"Remaining routes validate");
  Console.WriteLine("PASS: "+checks+" partial BACKUP mapping checks");
 }
}
