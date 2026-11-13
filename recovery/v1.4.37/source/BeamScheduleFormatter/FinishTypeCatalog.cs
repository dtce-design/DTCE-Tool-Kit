using System;
using System.Collections.Generic;
using System.Linq;

namespace BeamScheduleFormatter
{
	internal static class FinishTypeCatalog
	{
		public const string Schema = "DTCEFinishCatalogV1";

		public const int HeaderRow = 35;

		public const int FirstRow = 36;

		public const int LastRow = 56;

		public static readonly string[] Names = new string[21]
		{
			"Two-Component Cementitious Waterproofing System, 3 Coats", "One-Component Polyurethane-Acrylic Liquid Applied Flexible Membrane, 3 Coats", "4mm thk. Slated Bituminous Membrane Waterproofing", "3mm thk. Sanded Bituminous Membrane Waterproofing", "Porcelain Glazed Tiles and Trims", "Porcelain Unglazed Tiles", "Granite Stone Countertop", "Textured Unglazed Tiles (Yellow Warning Tactile Tiles)", "Vinyl Floor Tile", "Plain Cement Floor Finish",
			"Plain Cement Floor Finish with Floor Hardener", "Floor Topping (Trowelled Finish)", "Cement Broom Finish", "Nonslip Finish", "Bush Hammered Finish", "Cement Plaster Finish (Paper Finish)", "Cement Plaster Finish (Float Finish)", "Decorative Stone (Bricks)", "Damp Proofing (Polyethylene Sheet)", "Moisture Barrier and Pest Control Film, M168",
			"Aluminum Cladding"
		};

		public static string KindFor(string name)
		{
			int num = Array.FindIndex(Names, (string n) => FinishTypeRegistry.Key(n) == FinishTypeRegistry.Key(name));
			if (num < 0)
			{
				return null;
			}
			if (num != 4 && num != 5 && num != 7)
			{
				return "Other";
			}
			return "Tiles";
		}

		public static bool IsPlaster(string name)
		{
			string text = FinishTypeRegistry.Key(name);
			if (!(text == FinishTypeRegistry.Key(Names[15])))
			{
				return text == FinishTypeRegistry.Key(Names[16]);
			}
			return true;
		}

		public static WorksExcelTable Table(List<FinishTypeTotal> totals)
		{
			List<object[]> list = new List<object[]>();
			int n;
			for (n = 0; n < Names.Length; n++)
			{
				FinishTypeTotal finishTypeTotal = totals.FirstOrDefault((FinishTypeTotal t) => t.Key == FinishTypeRegistry.Key(Names[n]));
				list.Add(new object[3]
				{
					Names[n],
					(finishTypeTotal == null) ? 0.0 : finishTypeTotal.Area,
					"m2"
				});
			}
			return new WorksExcelTable("DTCE Tool Kit - POW", new string[3] { "FINISH / WATERPROOFING TYPE", "TOTAL AREA m2", "UNIT" }, list, 35, 4);
		}

		public static IEnumerable<object[]> Map()
		{
			for (int n = 0; n < Names.Length; n++)
			{
				yield return new object[5]
				{
					"FinishType" + (n + 1).ToString("00"),
					Names[n],
					"DTCE Tool Kit - POW",
					"E" + (36 + n),
					"m2"
				};
			}
		}
	}
}
