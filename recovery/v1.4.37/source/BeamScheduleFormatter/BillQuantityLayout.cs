using System.Collections.Generic;
using System.Linq;

namespace BeamScheduleFormatter
{
	internal static class BillQuantityLayout
	{
		public static string ItemNumber(BillQuantity item)
		{
			switch (item.Description ?? "")
			{
			case "Grade 40 rebar TOTAL TO BUY":
				return "902(1)a1";
			case "Grade 60 rebar TOTAL TO BUY":
				return "902(1)a2";
			case "Beam forms":
			case "Slab forms":
			case "Footing forms":
			case "Total forms":
				return "903(2)";
			case "Purlins PURCHASE weight":
				return "1047(8)b";
			default:
				return "";
			}
		}

		public static string Unit(string value)
		{
			switch (value)
			{
			default:
				return value;
			case "lm":
				return "m";
			case "m3":
				return "cu.m.";
			case "m2":
				return "sq.m.";
			}
		}

		public static List<BillDisplayRow> Rows(IEnumerable<BillQuantity> quantities)
		{
			List<BillDisplayRow> list = new List<BillDisplayRow>();
			foreach (IGrouping<string, BillQuantity> item in from q in quantities
				group q by q.Section)
			{
				list.Add(new BillDisplayRow
				{
					Section = true,
					Description = item.Key
				});
				foreach (BillQuantity item2 in item)
				{
					list.Add(new BillDisplayRow
					{
						Item = ItemNumber(item2),
						Description = item2.Description,
						Quantity = item2.Quantity,
						Unit = Unit(item2.Unit)
					});
					foreach (BillQuantity source in item2.Sources)
					{
						list.Add(new BillDisplayRow
						{
							Source = true,
							Description = (source.Basis ? "Basis: " : "Source: ") + source.Description,
							Quantity = source.Quantity,
							Unit = Unit(source.Unit)
						});
					}
				}
			}
			return list;
		}
	}
}
