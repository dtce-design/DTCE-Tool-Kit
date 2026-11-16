using System;using System.Linq;using System.Collections.Generic;
namespace BeamScheduleFormatter {
// Canal, septic vault and catch basin materials are part of the project's general pay items (owner decision,
// 16 Nov 2026). Each structure is listed under the item number it contributes to; there is no separate lump-sum line.
//  - Excavation / embankment: added to the overall earthworks balance (803(1)a, 804(1)a, borrow, surplus).
//  - Gravel: added to the total gravel installed and to buy (804(7)).
//  - Concrete: routed with the other miscellaneous structures by the concrete pay-item groups.
//  - Rebar: added to rebar purchase by size (allocated within the plumbing structures' own cutting plan).
//  - Forms, CHB, plaster, waterproofing: item-numbered rows with one source line per structure.
partial class ProgramOfWorksForm {
 internal const string PlumbingStructureSection="Plumbing Structures";
 static readonly string[] PlumbingRoutedElsewhere={"803(1)a","804(1)a","804(7)","900(1)i","902(1)a1"};
 static string StructureName(ChamberTakeoff q){return q.Kind+" "+q.Id+(string.IsNullOrWhiteSpace(q.Description)?"":" | "+q.Description);}
 double PlumbingGravel(){return chamberMeasures.Sum(q=>q.GravelVolume);}
 IEnumerable<BillQuantity> PlumbingGravelSources(){return chamberMeasures.Where(q=>q.GravelVolume>0).Select(q=>SourceQuantity(StructureName(q),q.GravelVolume,"m3"));}
 // Runs after RecalculatePlumbing: the earthworks balance and gravel summary were computed earlier in Recalculate().
 void IntegratePlumbingEarthworks(){if(chamberMeasures.Count==0||borrowSwell==null||borrowShrink==null||suitableSoil==null)return;
  try{foreach(var q in chamberMeasures){var soil=new EarthworkMaterial{Id=q.Id,Source=q.Kind,Bank=q.Excavation,Fill=q.Backfill,Swell=(double)borrowSwell.Value,Shrink=(double)borrowShrink.Value,Reusable=(double)suitableSoil.Value};soil.Validate();earthSources.Add(soil);}
   earthBalance=EarthworkBalance.Calculate(earthSources,(double)borrowSwell.Value,(double)borrowShrink.Value);if(earthBalanceGrid!=null){earthBalanceGrid.Rows.Clear();foreach(var row in EarthSummaryRows().Take(6))earthBalanceGrid.Rows.Add(row);}}
  catch(Exception ex){errors.Add("Plumbing structure earthworks: "+ex.Message);}
  UpdateGravelSummary();}
 List<BillQuantity> PlumbingStructurePayQuantities(){var result=new List<BillQuantity>();
  foreach(var q in chamberMeasures)foreach(var m in q.Materials()){string code=m.Section;if(PlumbingRoutedElsewhere.Contains(code)||m.Quantity<=1e-10)continue;
   string name=m.Description.StartsWith(code+" - ")?m.Description.Substring(code.Length+3):m.Description;var item=result.FirstOrDefault(r=>r.PayCode==code&&r.Unit==m.Unit);
   if(item==null){item=new BillQuantity{Section=PlumbingStructureSection,Description=code+" "+name,Unit=m.Unit,PayCode=code,PayDescription=name};result.Add(item);}
   item.Quantity+=m.Quantity;item.Sources.Add(new BillQuantity{Description=StructureName(q),Quantity=m.Quantity,Unit=m.Unit,Computation=m.Computation});}
  return result;}
}
}
