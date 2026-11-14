using System;using System.Linq;using System.Collections.Generic;using System.Globalization;
namespace BeamScheduleFormatter {
static class RequiredPayRouting {
 public static BackupTarget Resolve(string code,string description,string unit,IEnumerable<BackupTarget> source){Func<string,string> normal=value=>new string((value??"").ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());var candidates=source.Where(t=>t.Code==code&&BackupMapping.Unit(t.Unit)==BackupMapping.Unit(unit)).ToList();var matching=candidates.Where(t=>normal(t.Description)==normal(description)).ToList();if(matching.Count==1)return matching[0];bool distinctDescriptionRequired=code.StartsWith("900")||code.StartsWith("901")||BackupPayItemCatalog.Items().Count(t=>t.Code==code)>1;return !distinctDescriptionRequired&&candidates.Count==1?candidates[0]:null;}
}
partial class ProgramOfWorksForm {
 static string PayText(string value){return new string((value??"").ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());}
 void InitializePayItemCatalogue(){if(backupBill.Count==0)backupBill=BackupBill.Capture(BackupPayItemCatalog.Items(),new BackupBillItem[0]);}
 static List<BackupBillItem> RebindCatalogueManuals(List<BackupTarget> targets,List<BackupBillItem> previous){var mapped=new List<BackupBillItem>();foreach(var p in previous.Where(x=>x.ManualQuantity.HasValue)){var matches=targets.Where(t=>BackupMapping.Identity(t)==p.Signature).ToList();if(matches.Count!=1)throw new Exception("Saved manual pay item is missing or ambiguous in the linked BACKUP: "+p.Description);var target=matches[0];mapped.Add(new BackupBillItem{Slot=target.SlotId,Signature=BackupMapping.Identity(target),Code=target.Code,Description=target.Description,Unit=target.Unit,Row=target.Row,ManualQuantity=p.ManualQuantity});}return mapped;}
 BackupTarget ConcretePayItem(string group){bool ready=readyMixGroups.ContainsKey(group)&&readyMixGroups[group].Checked;var templates=BackupPayItemCatalog.Items();var ordinary=BackupPayItemCatalog.Concrete(group,false);var standard=BackupPayItemCatalog.Concrete(group,ready);if(standard!=null){var exact=backupBill.Select(x=>x.Target).Where(t=>BackupMapping.Identity(t)==BackupMapping.Identity(standard)).ToList();if(exact.Count==1)return exact[0];}
  Func<string,bool> role=d=>group=="SlabOnFill"?d.Contains("SLABONFILL"):group=="Structural"?d.Contains("TIEBEAM"):group=="RoofWaterproof"?d.Contains("WATERPROOFINGADMIXTURE"):group=="Footing"?d.Contains("FORFOOTING"):group=="Miscellaneous"?d.Contains("STIFFENERCOLUMN"):group=="WallFooting"?d.Contains("FORWALLFOOTING"):group=="Seal"?d.Contains("SEALCONCRETE"):group=="Lean"&&d.StartsWith("LEANCONCRETE");
  var choices=backupBill.Select(x=>x.Target).Where(t=>BackupMapping.Unit(t.Unit)=="m3"&&(t.Code.StartsWith("900")||t.Code.StartsWith("901"))&&(PayText(t.Description).Contains("READYMIX")==ready)&&role(PayText(t.Description))).ToList();if(choices.Count==1)return choices[0];
  return standard??new BackupTarget{Code=ordinary==null?"":ordinary.Code,Description=(ready?"Ready Mix — ":"Ordinary Mix — ")+ConcreteLabels[Array.IndexOf(ConcreteGroups,group)],Unit="m3",SlotId="missing-concrete:"+group+":"+ready};
 }
 List<BillQuantity> ConcretePayQuantities(){var result=new List<BillQuantity>();Action<string,string,double> add=(group,description,quantity)=>{if(quantity<=1e-10)return;var target=ConcretePayItem(group);var item=result.FirstOrDefault(q=>q.Section=="Concrete pay-item groups"&&q.PayCode==target.Code&&q.PayDescription==target.Description);if(item==null){item=new BillQuantity{Section="Concrete pay-item groups",Description=target.Description,Unit="m3",PayCode=target.Code,PayDescription=target.Description};result.Add(item);}item.Quantity+=quantity;item.Sources.Add(new BillQuantity{Description=description,Quantity=quantity,Unit="m3",Computation="Computed volume routed by concrete member group and the Ready Mix setting"});};
  var beams=beam.ConcreteExcelRows();for(int i=0;i<beams.GetLength(0);i++)add("Structural","Beam "+beams[i,1]+" | "+beams[i,0],Convert.ToDouble(beams[i,4]));
  var slabs=slab.WorksMeasureRows();for(int i=0;i<slabs.GetLength(0);i++){string support=Convert.ToString(slabs[i,2]);add(support.IndexOf("waterproof",StringComparison.OrdinalIgnoreCase)>=0?"RoofWaterproof":support=="On gravel fill"?"SlabOnFill":"Structural","Slab "+slabs[i,1]+" | "+slabs[i,0],Convert.ToDouble(slabs[i,4]));}
  foreach(var q in footingResults)add("Footing","Footing "+q.Id+" | "+q.Description,q.Concrete);
  foreach(var q in rcWallResults)add("Structural","RC wall "+q.Id+" | "+q.Description,q.Concrete);
  foreach(var q in canopyResults)add("Miscellaneous","Canopy "+q.Id+" | "+q.Description,q.Concrete);
  foreach(var q in miscQuantities)add("Miscellaneous",q.Kind+" "+q.Id+" | "+q.Description,q.Concrete);foreach(var q in wallFootingResults)add("WallFooting","Wall footing "+q.Id+" | "+q.Description,q.Concrete);foreach(var q in leanSources)add("Lean",q.Description,q.Quantity);return result;
 }
 void RouteRequiredQuantities(List<BackupDemand> demands){if(backupBill.Count==0)return;
  string grade60;try{grade60=SiteWorkTakeoff.Grade60Item(projectRebarPlan.Where(a=>!slopePlan.Contains(a)&&a.Demand.Grade==60).Select(a=>a.Demand.Diameter));}catch{grade60="Unsupported Grade 60 diameter above 42mm — review required";}
  foreach(var demand in demands){
   if(demand.Key.StartsWith("Rebar purchase by size|")){bool g60=PayText(demand.Description).Contains("GRADE60");demand.PayCode=g60?"902(1)a2":"902(1)a1";demand.PayDescription=g60?grade60:"Reinforcing Steel (Deformed), Grade 40";}
   if(demand.Key.StartsWith("Earthworks|")&&BackupMapping.Unit(demand.Unit)=="m3"&&PayText(demand.Description).Contains("EXCAVATION")&&!PayText(demand.Description).Contains("EMBANKMENT")&&!PayText(demand.Description).Contains("UNSUITABLE")&&!PayText(demand.Description).Contains("SURPLUS")){demand.PayCode=excavationMaterial.Text=="Soft Rock"?"803(1)b":excavationMaterial.Text=="Hard Rock"?"803(1)c":"803(1)a";demand.PayDescription="Structure Excavation ("+excavationMaterial.Text+")";}
   if(string.IsNullOrEmpty(demand.PayCode)&&BackupMapping.Unit(demand.Unit)=="m2"&&PayText(demand.Description).Contains("FORM")&&(demand.Key.StartsWith("Reinforced Concrete Works|")||demand.Key.StartsWith("Canopy / Ledge|"))){demand.PayCode="903(2)";demand.PayDescription="Formworks and Falseworks";}
   if(string.IsNullOrEmpty(demand.PayCode))continue;BackupTarget target=RequiredPayRouting.Resolve(demand.PayCode,demand.PayDescription,demand.Unit,backupBill.Select(b=>b.Target));
   demand.TargetSlotId=target==null?"missing:"+demand.PayCode+":"+PayText(demand.PayDescription):target.SlotId;demand.TargetIdentity=target==null?"":BackupMapping.Identity(target);
  }
 }
}
}
