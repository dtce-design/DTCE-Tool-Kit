using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace BeamScheduleFormatter
{
	internal class FinishTypeRegistry
	{
		public const string Sheet = "POW Finish Types";

		public readonly List<FinishTypeRow> Rows = new List<FinishTypeRow>();

		public static string Clean(string name)
		{
			return Regex.Replace((name ?? "").Trim(), "\\s+", " ");
		}

		public static string Key(string name)
		{
			return Clean(name).ToUpperInvariant();
		}

		public void Include(IEnumerable<string> names)
		{
			foreach (string name2 in names)
			{
				string name = Clean(name2);
				string key = Key(name2);
				if (!(key == "") && !Rows.Any((FinishTypeRow r) => r.Key == key))
				{
					if (Rows.Count >= 10000)
					{
						throw new Exception("Too many registered finish types.");
					}
					Rows.Add(new FinishTypeRow
					{
						Key = key,
						Name = name,
						Row = ((Rows.Count == 0) ? 2 : (Rows.Max((FinishTypeRow r) => r.Row) + 1))
					});
				}
			}
		}

		public static List<FinishTypeTotal> Totals(IEnumerable<FinishItem> items, Func<FinishItem, double> area)
		{
			return (from f in items
				where Key(f.MaterialName) != ""
				group f by Key(f.MaterialName) into g
				select new FinishTypeTotal
				{
					Key = g.Key,
					Name = Clean(g.First().MaterialName),
					Area = g.Sum(area),
					Locations = string.Join(" / ", from c in g.Select((FinishItem f) => f.Category).Distinct()
						orderby c
						select c)
				}).ToList();
		}

		public WorksExcelTable Table(List<FinishTypeTotal> totals)
		{
			List<object[]> list = new List<object[]>();
			int num = ((Rows.Count == 0) ? 1 : Rows.Max((FinishTypeRow r) => r.Row));
			for (int num2 = 2; num2 <= num; num2++)
			{
				list.Add(new object[3]);
			}
			foreach (FinishTypeRow item in Rows)
			{
				FinishTypeTotal finishTypeTotal = totals.FirstOrDefault((FinishTypeTotal t) => t.Key == item.Key);
				list[item.Row - 2] = new object[3]
				{
					item.Name,
					(finishTypeTotal == null) ? 0.0 : finishTypeTotal.Area,
					(finishTypeTotal == null) ? "No current entries" : finishTypeTotal.Locations
				};
			}
			return new WorksExcelTable("POW Finish Types", new string[3] { "Material / Finish Type", "Total area m2", "Locations" }, list);
		}

		public void Write(XmlElement root)
		{
			XmlElement xmlElement = root.OwnerDocument.CreateElement("FinishTypeCells");
			root.AppendChild(xmlElement);
			foreach (FinishTypeRow item in Rows.OrderBy((FinishTypeRow r) => r.Row))
			{
				XmlElement xmlElement2 = root.OwnerDocument.CreateElement("Type");
				xmlElement2.SetAttribute("name", item.Name);
				xmlElement2.SetAttribute("row", item.Row.ToString(CultureInfo.InvariantCulture));
				xmlElement.AppendChild(xmlElement2);
			}
		}

		public static FinishTypeRegistry Read(XmlElement root)
		{
			FinishTypeRegistry finishTypeRegistry = new FinishTypeRegistry();
			XmlElement xmlElement = root["FinishTypeCells"];
			if (xmlElement == null)
			{
				return finishTypeRegistry;
			}
			HashSet<string> hashSet = new HashSet<string>();
			HashSet<int> hashSet2 = new HashSet<int>();
			foreach (XmlElement item in xmlElement.SelectNodes("Type"))
			{
				string name = Clean(item.GetAttribute("name"));
				string text = Key(name);
				int result;
				if (text == "" || !int.TryParse(item.GetAttribute("row"), out result) || result < 2 || result > 10001 || !hashSet.Add(text) || !hashSet2.Add(result))
				{
					throw new Exception("Invalid or duplicate saved finish-type cell assignment.");
				}
				finishTypeRegistry.Rows.Add(new FinishTypeRow
				{
					Name = name,
					Key = text,
					Row = result
				});
			}
			return finishTypeRegistry;
		}
	}
}
