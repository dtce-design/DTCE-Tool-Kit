using System;
using System.Collections.Generic;

namespace BeamScheduleFormatter
{
	internal static class WorksSummaryLayout
	{
		public const string Sheet = "DTCE Tool Kit - POW";

		public const string Schema = "DTCEPOWSummaryV1";

		public const int LastRow = 38;

		public static readonly string[][] Map = new string[24][]
		{
			new string[4] { "Grade40", "Grade 40 rebar TOTAL TO BUY", "5", "kg" },
			new string[4] { "Grade60", "Grade 60 rebar TOTAL TO BUY", "6", "kg" },
			new string[4] { "BeamConcrete", "Beam concrete (gross)", "9", "m3" },
			new string[4] { "SlabConcrete", "Slab concrete (gross)", "10", "m3" },
			new string[4] { "ConcreteTotal", "Total concrete (gross)", "11", "m3" },
			new string[4] { "BeamForms", "Beam forms", "14", "m2" },
			new string[4] { "SlabForms", "Slab forms", "15", "m2" },
			new string[4] { "FormsTotal", "Total forms", "16", "m2" },
			new string[4] { "CHBArea", "Net CHB wall area", "19", "m2" },
			new string[4] { "CHBCount", "CHB blocks", "20", "pcs" },
			new string[4] { "Plaster", "Net plaster", "21", "m2" },
			new string[4] { "MasonryPaint", "Masonry paint", "22", "m2" },
			new string[4] { "CHB100Area", "CHB 100 mm net area", "23", "m2" },
			new string[4] { "CHB150Area", "CHB 150 mm net area", "24", "m2" },
			new string[4] { "WoodPaint", "Wood paint including jamb", "25", "m2" },
			new string[4] { "Leversets", "Leversets", "26", "pcs" },
			new string[4] { "Hinges", "Hinges", "27", "pcs" },
			new string[4] { "WallTiles", "Wall tiles", "30", "m2" },
			new string[4] { "CeilingFinishes", "Ceiling finishes", "31", "m2" },
			new string[4] { "FloorFinishes", "Floor finishes", "32", "m2" },
			new string[4] { "FootingConcrete", "Footing concrete", "34", "m3" },
			new string[4] { "FootingForms", "Footing forms", "35", "m2" },
			new string[4] { "RCWallConcrete", "RC wall concrete", "37", "m3" },
			new string[4] { "RCWallForms", "RC wall forms", "38", "m2" }
		};

		public static WorksExcelTable Build(IDictionary<string, double> values)
		{
			List<object[]> list = new List<object[]>();
			for (int i = 2; i <= 38; i++)
			{
				list.Add(new object[3]);
			}
			list[0][0] = "PROGRAM OF WORKS";
			list[1][0] = "Fixed summary cells | DTCEPOWSummaryV1";
			string[][] map = Map;
			foreach (string[] array in map)
			{
				int index = int.Parse(array[2]) - 2;
				double value;
				if (!values.TryGetValue(array[0], out value))
				{
					if (!(array[0] == "CHB100Area") && !(array[0] == "CHB150Area") && !(array[0] == "FootingConcrete") && !(array[0] == "FootingForms") && !(array[0] == "RCWallConcrete") && !(array[0] == "RCWallForms"))
					{
						throw new ArgumentException("Missing fixed summary value: " + array[0]);
					}
					value = 0.0;
				}
				list[index] = new object[3]
				{
					array[1],
					value,
					array[3]
				};
			}
			return new WorksExcelTable("DTCE Tool Kit - POW", new string[3] { "Item", "Quantity", "Unit" }, list);
		}

		public static WorksExcelTable CellMap(FinishTypeRegistry registry = null)
		{
			List<object[]> list = new List<object[]>();
			string[][] map = Map;
			foreach (string[] array in map)
			{
				list.Add(new object[5]
				{
					array[0],
					array[1],
					"DTCE Tool Kit - POW",
					"B" + array[2],
					array[3]
				});
			}
			string[] array2 = new string[9] { "Excavation bank", "Backfill compacted", "Gravel bedding", "Excavated loose", "Backfill loose required", "Surplus loose", "Common borrow loose", "Embankment from excavation loose", "Embankment common borrow loose" };
			for (int j = 0; j < array2.Length; j++)
			{
				list.Add(new object[5]
				{
					"Earthworks:" + j,
					array2[j],
					"POW Earthwork Summary",
					"B" + (j + 2),
					"m3"
				});
			}
			string[] array3 = new string[10] { "Excavation bank", "Compacted space reference", "Embankment from excavation LOOSE", "Embankment common borrow LOOSE", "Suitable surplus loose", "Unsuitable disposal loose", "Beam gravel installed", "Footing gravel installed", "Total gravel installed", "GRAVEL TO BUY loose" };
			for (int k = 0; k < array3.Length; k++)
			{
				list.Add(new object[5]
				{
					"OverallEarthworks:" + k,
					array3[k],
					"POW Overall Earthworks",
					"B" + (k + 2),
					"m3"
				});
			}
			list.AddRange(FinishTypeCatalog.Map());
			if (registry != null)
			{
				foreach (FinishTypeRow row in registry.Rows)
				{
					list.Add(new object[5]
					{
						"FinishType:" + row.Key,
						row.Name,
						"POW Finish Types",
						"B" + row.Row,
						"m2"
					});
				}
			}
			return new WorksExcelTable("POW Cell Map", new string[5] { "Key", "Value", "Worksheet", "Fixed cell", "Unit" }, list);
		}
	}
}
