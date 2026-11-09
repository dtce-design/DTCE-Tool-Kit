using System;using System.Collections.Generic;using System.Linq;
namespace BeamScheduleFormatter {
class BillQuantity {public string Section,Description,Unit;public double Quantity;public bool Basis;public readonly List<BillQuantity> Sources=new List<BillQuantity>();}
class BillDisplayRow {public string Item,Description,Unit;public double? Quantity;public bool Section,Source;}
static class BillQuantityLayout {
 public static string ItemNumber(BillQuantity item){string d=item.Description??"";
  if(d=="Grade 40 rebar TOTAL TO BUY")return "902(1)a1";
  if(d=="Grade 60 rebar TOTAL TO BUY")return "902(1)a2";
  if(d=="Beam forms"||d=="Slab forms"||d=="Footing forms"||d=="Total forms")return "903(2)";
  if(d=="Purlins PURCHASE weight")return "1047(8)b";
  // Leave unconfirmed project specifications unassigned rather than guess a pay item.
  return "";
 }
 public static string Unit(string value){return value=="m2"?"sq.m.":value=="m3"?"cu.m.":value=="lm"?"m":value;}
 public static List<BillDisplayRow> Rows(IEnumerable<BillQuantity> quantities){var result=new List<BillDisplayRow>();foreach(var group in quantities.GroupBy(q=>q.Section)){result.Add(new BillDisplayRow{Section=true,Description=group.Key});foreach(var item in group){result.Add(new BillDisplayRow{Item=ItemNumber(item),Description=item.Description,Quantity=item.Quantity,Unit=Unit(item.Unit)});foreach(var source in item.Sources)result.Add(new BillDisplayRow{Source=true,Description=(source.Basis?"Basis: ":"Source: ")+source.Description,Quantity=source.Quantity,Unit=Unit(source.Unit)});}}return result;}
}
}
