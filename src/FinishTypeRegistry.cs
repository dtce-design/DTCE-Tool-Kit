using System;using System.Linq;using System.Xml;using System.Collections.Generic;using System.Text.RegularExpressions;
namespace BeamScheduleFormatter {
class FinishTypeRow {public string Key,Name;public int Row;}
class FinishTypeTotal {public string Key,Name;public double Area;public string Locations;}
class FinishTypeRegistry {
 public const string Sheet="POW Finish Types";public readonly List<FinishTypeRow> Rows=new List<FinishTypeRow>();
 public static string Clean(string name){return Regex.Replace((name??"").Trim(),@"\s+"," ");}
 public static string Key(string name){return Clean(name).ToUpperInvariant();}
 public void Include(IEnumerable<string> names){foreach(string name in names){string clean=Clean(name),key=Key(name);if(key==""||Rows.Any(r=>r.Key==key))continue;if(Rows.Count>=10000)throw new Exception("Too many registered finish types.");Rows.Add(new FinishTypeRow{Key=key,Name=clean,Row=Rows.Count==0?2:Rows.Max(r=>r.Row)+1});}}
 public static List<FinishTypeTotal> Totals(IEnumerable<FinishItem> items,Func<FinishItem,double> area){return items.Where(f=>Key(f.MaterialName)!="").GroupBy(f=>Key(f.MaterialName)).Select(g=>new FinishTypeTotal{Key=g.Key,Name=Clean(g.First().MaterialName),Area=g.Sum(area),Locations=string.Join(" / ",g.Select(f=>f.Category).Distinct().OrderBy(c=>c))}).ToList();}
 public WorksExcelTable Table(List<FinishTypeTotal> totals){var rows=new List<object[]>();int last=Rows.Count==0?1:Rows.Max(r=>r.Row);for(int r=2;r<=last;r++)rows.Add(new object[]{null,null,null});foreach(var item in Rows){var total=totals.FirstOrDefault(t=>t.Key==item.Key);rows[item.Row-2]=new object[]{item.Name,total==null?0:total.Area,total==null?"No current entries":total.Locations};}return new WorksExcelTable(Sheet,new[]{"Material / Finish Type","Total area m2","Locations"},rows);}
 public void Write(XmlElement root){var section=root.OwnerDocument.CreateElement("FinishTypeCells");root.AppendChild(section);foreach(var item in Rows.OrderBy(r=>r.Row)){var e=root.OwnerDocument.CreateElement("Type");e.SetAttribute("name",item.Name);e.SetAttribute("row",item.Row.ToString(System.Globalization.CultureInfo.InvariantCulture));section.AppendChild(e);}}
 public static FinishTypeRegistry Read(XmlElement root){var registry=new FinishTypeRegistry();var section=root["FinishTypeCells"];if(section==null)return registry;var names=new HashSet<string>();var cells=new HashSet<int>();foreach(XmlElement e in section.SelectNodes("Type")){int row;string name=Clean(e.GetAttribute("name")),key=Key(name);if(key==""||!int.TryParse(e.GetAttribute("row"),out row)||row<2||row>10001||!names.Add(key)||!cells.Add(row))throw new Exception("Invalid or duplicate saved finish-type cell assignment.");registry.Rows.Add(new FinishTypeRow{Name=name,Key=key,Row=row});}return registry;}
}
}
