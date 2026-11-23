using System;using System.Collections.Generic;using System.Linq;
namespace BeamScheduleFormatter {
// Automatic Bill of Quantities mapping for Doors & Windows (owner, 18 November 2026: "pwede ba automatic na pagmap yung
// leverset hinges sa finishing hardware, tapos yung doors and windows automatic na if wooden tapos aluminum door /
// aluminum window"):
//  - Leversets and hinges            -> 1004(2) Finishing Hardware
//  - Wood doors / windows and jambs  -> 1010(4) Wooden Doors and Windows (KD Tanguile)
//  - Aluminum door                   -> 1007(2) Powder-Coated Aluminum-Framed Glass Door
//  - Aluminum window                 -> 1008(2) Powder-Coated Aluminum-Framed Glass Window
//  - PVC door                        -> 1043(1) PVC Doors and Frames
//  - Steel door                      -> 1006(6) Steel Doors and Frames
//  - Steel window                    -> 1005(5) Steel Window (the one steel window item of the BACKUP)
// These are defaults only: a quantity the user already mapped, changed or unmapped in the BOQ keeps that choice.
partial class ProgramOfWorksForm {
 static string OpeningPayCode(string key){
  if(key.StartsWith("Doors & Windows|Leversets|",StringComparison.Ordinal)||key.StartsWith("Doors & Windows|Hinges|",StringComparison.Ordinal)||IsDoorHardwareKey(key))return "1004(2)";
  if(key.StartsWith("Door / window jambs|",StringComparison.Ordinal))return "1010(4)";
  if(!key.StartsWith("Door / window schedule|",StringComparison.Ordinal))return null;
  string[] parts=key.Split('|');if(parts.Length<3)return null;string kind=parts[2].Trim();
  if(kind=="Wood Door"||kind=="Wood Window")return "1010(4)";
  if(kind=="Aluminum Door")return "1007(2)";if(kind=="Aluminum Window")return "1008(2)";if(kind=="PVC Door")return "1043(1)";if(kind=="Steel Door")return "1006(6)";if(kind=="Steel Window")return SteelWindowCode;
  return null;}
 const string SteelWindowCode="steel-window";
 BackupTarget OpeningPayTarget(string code){if(code==SteelWindowCode){var steel=backupBill.Select(b=>b.Target).Where(t=>t!=null&&PayText(t.Description).Contains("STEEL")&&PayText(t.Description).Contains("WINDOW")).GroupBy(t=>t.SlotId).Select(g=>g.First()).ToList();return steel.Count==1?steel[0]:null;}var catalogue=BackupPayItemCatalog.Items().FirstOrDefault(t=>t.Code==code);if(catalogue==null)return null;
  var candidates=backupBill.Select(b=>b.Target).Where(t=>t!=null&&t.Code==code).ToList();if(candidates.Count==1)return candidates[0];
  var same=candidates.Where(t=>PayText(t.Description)==PayText(catalogue.Description)).ToList();return same.Count==1?same[0]:null;}
 void DefaultOpeningRoutes(List<BackupDemand> list){if(backupBill==null||backupBill.Count==0)return;var targets=new Dictionary<string,BackupTarget>(StringComparer.Ordinal);
  foreach(var d in list){if(d.TargetSlotId!=null||backupBillRoutes.ContainsKey(d.Key)||boqUnmapped.Contains(d.Key))continue;string code=OpeningPayCode(d.Key);if(code==null)continue;
   BackupTarget target;if(!targets.TryGetValue(code,out target)){target=OpeningPayTarget(code);targets[code]=target;}if(target==null)continue;
   backupBillRoutes[d.Key]=target.SlotId;boqRouteOverrides[d.Key]=target.SlotId;}}
}
}
