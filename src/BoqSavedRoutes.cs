using System;using System.Collections.Generic;using System.Linq;
namespace BeamScheduleFormatter {
// Owner (19 November 2026): "bat nawawala yung map kapag naglink ako ng excel file" and "huwag buburahin, yung naka save na mapping
// ang susundin sa dtce app". Linking (or refreshing) the BACKUP workbook replaced the Bill of Quantities mapping saved in the
// Program of Works with the mapping stored in the workbook (its _DTCE_BACKUP_MAP_ names) plus automatic suggestions. A workbook
// without those names (not saved after mapping, another copy, a newer version) also gets new item ids on every link, so every
// mapping made in the app was lost. The mapping saved in the app now wins: each BACKUP item is followed by its item no.,
// description and unit (its identity), routes, manual overrides and LS include choices move to the same item in the linked
// workbook, and routes whose item is not found once in the workbook are reported, not silently replaced.
partial class ProgramOfWorksForm {
 sealed class SavedBoqMapping {internal Dictionary<string,string> Routes=new Dictionary<string,string>(StringComparer.Ordinal);internal Dictionary<string,string> SlotIdentity=new Dictionary<string,string>(StringComparer.Ordinal);}
 /* before the catalogue is replaced: the app's routes and the identity of every slot they use */
 SavedBoqMapping CaptureSavedBoqMapping(){var saved=new SavedBoqMapping();if(backupBill==null)return saved;foreach(var item in backupBill)if(!string.IsNullOrEmpty(item.Slot)&&!string.IsNullOrEmpty(item.Signature))saved.SlotIdentity[item.Slot]=item.Signature;
  foreach(var pair in backupBillRoutes)if(saved.SlotIdentity.ContainsKey(pair.Value))saved.Routes[pair.Key]=pair.Value;return saved;}
 /* after the catalogue of the linked workbook is loaded and its own routes are in backupBillRoutes; returns the number of saved
    routes whose BACKUP item could not be found exactly once in the workbook (their pay item keys) */
 List<string> RestoreSavedBoqMapping(SavedBoqMapping saved){var missing=new List<string>();if(saved==null||saved.SlotIdentity.Count==0)return missing;
  var byIdentity=backupBill.Where(b=>!string.IsNullOrEmpty(b.Signature)).GroupBy(b=>b.Signature,StringComparer.Ordinal).Where(g=>g.Count()==1).ToDictionary(g=>g.Key,g=>g.First().Slot,StringComparer.Ordinal);
  Func<string,string> move=slot=>{string identity,next;if(slot==null||!saved.SlotIdentity.TryGetValue(slot,out identity))return slot;return byIdentity.TryGetValue(identity,out next)?next:null;};
  /* manual overrides and LS include choices follow their item */
  foreach(var key in boqRouteOverrides.Keys.ToList()){string slot=boqRouteOverrides[key];if(slot==null)continue;string next=move(slot);if(next!=null)boqRouteOverrides[key]=next;}
  foreach(var key in boqIncludes.Keys.Where(k=>k.StartsWith(LsIncludePrefix,StringComparison.Ordinal)).ToList()){string next=move(key.Substring(LsIncludePrefix.Length));if(next==null||LsIncludePrefix+next==key)continue;bool value=boqIncludes[key];boqIncludes.Remove(key);boqIncludes[LsIncludePrefix+next]=value;}
  foreach(var slot in lsAutoSlots.ToList()){string next=move(slot);if(next!=null&&next!=slot){lsAutoSlots.Remove(slot);lsAutoSlots.Add(next);}}
  /* the routes saved in the app win over the workbook's; kept as overrides so the next write to Excel uses them too */
  foreach(var pair in saved.Routes){if(boqUnmapped.Contains(pair.Key))continue;string chosen;if(boqRouteOverrides.TryGetValue(pair.Key,out chosen)&&chosen==null)continue;string next=move(pair.Value);if(next==null){missing.Add(pair.Key);continue;}
   string own;if(boqRouteOverrides.TryGetValue(pair.Key,out own)&&own!=null&&backupBill.Any(b=>b.Slot==own))next=own;else boqRouteOverrides[pair.Key]=next;backupBillRoutes[pair.Key]=next;}
  return missing;}
}
}
