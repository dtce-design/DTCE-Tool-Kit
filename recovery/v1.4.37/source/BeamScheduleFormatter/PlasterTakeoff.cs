using System;
using System.Collections.Generic;
using System.Linq;

namespace BeamScheduleFormatter
{
	internal class PlasterTakeoff
	{
		public double Gross;

		public double TileDeduction;

		public double Net;

		public double TileA;

		public double TileB;

		public static PlasterTakeoff Calculate(WallItem wall, IEnumerable<OpeningItem> openings, IEnumerable<FinishItem> finishes, string selectedSides = null)
		{
			double num = wall.Net(openings);
			string text = selectedSides ?? wall.Plaster;
			bool flag = text == "1 side (A)" || text == "2 sides";
			bool flag2 = text == "1 side (B)" || text == "2 sides";
			if (!flag && !flag2 && (selectedSides == null || !(text == "None")))
			{
				throw new Exception("Select 1 side (A), 1 side (B), or 2 sides for plastering.");
			}
			double num2 = 0.0;
			double num3 = 0.0;
			foreach (FinishItem item in finishes.Where((FinishItem f) => f.Category == "Wall" && f.Kind == "Tiles" && string.Equals(f.Wall.Trim(), wall.Id.Trim(), StringComparison.OrdinalIgnoreCase)))
			{
				double num4 = item.PerFace(wall, openings);
				if (item.OnA)
				{
					num2 += num4;
				}
				if (item.OnB)
				{
					num3 += num4;
				}
			}
			if (num2 > num + 1E-07 || num3 > num + 1E-07)
			{
				throw new Exception("Wall tile area exceeds net wall area on a face. Check duplicate/overlapping entries.");
			}
			double num5 = num * (double)((flag ? 1 : 0) + (flag2 ? 1 : 0));
			double num6 = (flag ? num2 : 0.0) + (flag2 ? num3 : 0.0);
			return new PlasterTakeoff
			{
				Gross = num5,
				TileA = num2,
				TileB = num3,
				TileDeduction = num6,
				Net = Math.Max(0.0, num5 - num6)
			};
		}
	}
}
