using System;using System.Collections.Generic;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
// Owner (19 November 2026, "part A at Part B allow na ikaw ang maginput ng data sa BOQ ng app" - answered "Pareho"): the app fills
// the usual Part A (Facilities for the Engineer) and Part B (Other General Requirements) quantities in the Bill of Quantities, and
// the owner can type over any of them (a typed quantity always wins; 0 removes an item; blank returns to the app's value).
// What the app fills (only for a project that has measured quantities, and only items present in the BACKUP / catalogue):
//   B.3 Permits and Clearances, B.4 Construction Survey and Staking, B.7 Occupational Safety and Health, B.9 Mobilization /
//   Demobilization: 1 ls each; B.5 Project Billboard / Signboard: 1 ea per billboard row;
//   A.1.4 Progress Photographs: since 23 November 2026 always from the Contract Duration (ProgressPhotoDemands);
//   A.1.1(15)/(16) operation of the field office: months, only when its field office (A.1.1(1)/(2) or (3)) has a quantity;
//   A.1.2(5)/(6) operation of service vehicles: vehicles of A.1.2(1)/(3) x months.
// Field offices, vehicles, equipment, provisional sums, detours and tests are project decisions and are not filled.
partial class ProgramOfWorksForm {
 /* Owner (19 November 2026, after v1.30.2: "ako na lang ang magiinput ng quantities" / "iallow na lang maginput ng qty sa boq ng
    app for part A and B"): Part A / B quantities are typed by the owner in the Bill of Quantities and copied to the same BACKUP
    item; the app does not fill them. The rules below are kept but switched off. */
 internal static bool FillGeneralRequirements=false;
 internal const string GeneralSuggestionPrefix="Part A/B suggested|";
 internal static bool IsGeneralSuggestion(BackupDemand d){return d!=null&&d.Key!=null&&d.Key.StartsWith(GeneralSuggestionPrefix,StringComparison.Ordinal);}
 bool suggestingGeneral;string generalDaysStamp;int generalDays;
 static string GeneralKey(string code){return new string((code??"").ToUpperInvariant().Where(c=>char.IsLetterOrDigit(c)||c=='(').ToArray());}
 static bool GeneralIs(BackupBillItem item,string key){string k=GeneralKey(item.Code);return k==key||k.StartsWith(key+"(",StringComparison.Ordinal)||key.EndsWith("(")&&k.StartsWith(key,StringComparison.Ordinal);}
 /* typed or workbook quantity of the first item with this code */
 double GeneralEntered(string key){foreach(var item in backupBill)if(GeneralKey(item.Code)==key){double? q=item.ManualQuantity??item.WorkbookFallback;/* typed in the app; Progress Photographs from BACKUP (23 November 2026) */if(q.HasValue&&q.Value>0)return q.Value;}return 0;}
 /* the app's Part A / B quantities; months only when withMonths (the schedule itself is built without them) */
 List<BackupDemand> GeneralRequirementSuggestions(IEnumerable<BackupDemand> computed,bool withMonths){var result=new List<BackupDemand>();if(backupBill==null||backupBill.Count==0)return result;
  int months=0;string basis="";if(withMonths&&!suggestingGeneral){int days=ScheduleDaysForSuggestions(computed);if(days>0){months=(int)Math.Ceiling(days/30.0-1e-9);basis="construction schedule "+days+" calendar days / 30 = "+months+" month(s)";}}
  foreach(var item in backupBill){string unit=BackupMapping.Unit(item.Unit),desc=(item.Description??"").ToUpperInvariant(),why=null;double q=0;
   if(unit=="ls"&&(GeneralIs(item,"B3")||GeneralIs(item,"B7")||GeneralIs(item,"B9")||GeneralIs(item,"B4")&&desc.Contains("STAKING"))){q=1;why="1 lump sum filled by the app";}
   else if(GeneralIs(item,"B5")&&(unit=="ea"||unit=="each")){q=1;why="1 billboard filled by the app";}
   /* A.1.4 Progress Photographs: from the Contract Duration (ProgressPhotoDemands), not from these rules */
   else if(months>0&&GeneralKey(item.Code)=="A11(15"&&(GeneralEntered("A11(1")>0||GeneralEntered("A11(2")>0)){q=months;why=basis;}
   else if(months>0&&GeneralKey(item.Code)=="A11(16"&&GeneralEntered("A11(3")>0){q=months;why=basis;}
   else if(months>0&&GeneralKey(item.Code)=="A12(5"&&GeneralEntered("A12(1")>0){q=months*GeneralEntered("A12(1");why=GeneralEntered("A12(1").ToString("0.###",CultureInfo.InvariantCulture)+" vehicle(s) x "+basis;}
   else if(months>0&&GeneralKey(item.Code)=="A12(6"&&GeneralEntered("A12(3")>0){q=months*GeneralEntered("A12(3");why=GeneralEntered("A12(3").ToString("0.###",CultureInfo.InvariantCulture)+" vehicle(s) x "+basis;}
   if(q<=0)continue;var d=new BackupDemand{Key=GeneralSuggestionPrefix+item.Slot,Description=item.Description,Unit=item.Unit,Quantity=q,TargetSlotId=item.Slot,TargetIdentity=item.Signature,ManualPriority=1};
   d.Sources.Add(new BillQuantity{Section="General Requirements",Description="Filled by the app: "+why+" (type a quantity in the Bill of Quantities to change it, 0 to remove it)",Unit=item.Unit,Quantity=q});result.Add(d);}
  return result;}
 /* Progress Photographs follow the Contract Duration of the Bill of Quantities (owner, 23 November 2026: "progress photo graph naka
    link sa contract duration ng boq"): months = contract duration (calendar days) / 30, rounded up. A quantity typed in the app still
    wins; with no contract duration yet (empty construction schedule) the quantity in the linked BACKUP is kept. */
 internal const string ProgressPhotoPrefix="Progress Photographs|";
 /* filled by the app but still typed over in the Bill of Quantities: Part A / B suggestions and Progress Photographs */
 internal static bool IsAppFilled(BackupDemand d){return IsGeneralSuggestion(d)||d!=null&&d.Key!=null&&d.Key.StartsWith(ProgressPhotoPrefix,StringComparison.Ordinal);}
 internal static int DurationMonthsOf(int calendarDays){return calendarDays<=0?0:(int)Math.Ceiling(calendarDays/30.0-1e-9);}
 int ContractDurationDays(IEnumerable<BackupDemand> computed){int working=durationWorkingOverride>0?durationWorkingOverride:ScheduleDaysForSuggestions(computed);if(working<=0)return 0;
  try{int[] used;return ContractDurationCalc.Compute(durationStartDate,working,durationHolidayList.Select(h=>h.Date),DurationRainy(out used)).ContractDuration;}catch(Exception){return 0;}}
 List<BackupDemand> ProgressPhotoDemands(List<BackupDemand> computed){var result=new List<BackupDemand>();if(backupBill==null||suggestingGeneral)return result;
  var items=backupBill.Where(i=>i.FromWorkbook&&!i.ManualQuantity.HasValue&&string.IsNullOrEmpty(i.ManualInputText)).ToList();if(items.Count==0)return result;
  int days=ContractDurationDays(computed.Where(d=>d.Key==null||!d.Key.StartsWith("BOQ manual|",StringComparison.Ordinal)).ToList());int months=DurationMonthsOf(days);
  foreach(var item in items){double q;string why;if(months>0){q=months;why="Contract Duration "+days+" calendar days / 30 = "+months+" month(s)";}
   else if(item.WorkbookQuantity.HasValue&&item.WorkbookQuantity.Value>0){q=item.WorkbookQuantity.Value;why="no Contract Duration yet - quantity of the linked BACKUP";}else continue;
   var d=new BackupDemand{Key=ProgressPhotoPrefix+item.Slot,Description=item.Description,Unit=item.Unit,Quantity=q,TargetSlotId=item.Slot,TargetIdentity=item.Signature,ManualPriority=1};
   d.Sources.Add(new BillQuantity{Section="General Requirements",Description="Progress Photographs: "+why+" (type a quantity in the Bill of Quantities to change it)",Unit=item.Unit,Quantity=q});result.Add(d);}
  return result;}
 /* calendar days of the construction schedule; built once per change of the quantities, overrides or typed BOQ quantities */
 int ScheduleDaysForSuggestions(IEnumerable<BackupDemand> computed){string stamp=QuantityStamp(computed)+"|"+string.Join(";",scheduleOverrides.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>p.Key+"="+p.Value.Rate+"/"+p.Value.Crews+"/"+p.Value.Start))+"|"+string.Join(";",backupBill.Select(b=>b.Slot+"="+b.ManualQuantity+"/"+b.WorkbookQuantity+"/"+b.ManualInputText));
  if(stamp==generalDaysStamp)return generalDays;suggestingGeneral=true;try{generalDays=ScheduleRows().Where(r=>r.Days>0).Select(r=>r.Finish).DefaultIfEmpty(0).Max();}catch(Exception){generalDays=0;}finally{suggestingGeneral=false;}generalDaysStamp=stamp;return generalDays;}
}
}
