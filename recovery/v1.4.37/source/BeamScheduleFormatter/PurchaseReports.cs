using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace BeamScheduleFormatter
{
	internal static class PurchaseReports
	{
		public static void Summary(StringBuilder b, List<StockAllocation> plan, bool includeOrders = true)
		{
			plan = plan.Where((StockAllocation a) => a.Choice != null).ToList();
			b.AppendLine("01  REBAR PURCHASE SUMMARY");
			b.AppendLine(new string('-', 92));
			b.AppendLine(string.Format("{0,-8}{1,6}{2,15}{3,15}{4,15}{5,15}", "GRADE", "DIA", "CUT STEEL kg", "ADDED LAP kg", "OFFCUT kg", "BUY kg"));
			int[] array = new int[2] { 40, 60 };
			for (int num = 0; num < array.Length; num++)
			{
				int grade = array[num];
				List<StockAllocation> source = plan.Where((StockAllocation a) => a.Demand.Grade == grade).ToList();
				foreach (IGrouping<int, StockAllocation> item in from a in source
					group a by a.Demand.Diameter into g
					orderby g.Key
					select g)
				{
					b.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,-8}{1,6}{2,15:0.00}{3,15:0.00}{4,15:0.00}{5,15:0.00}", "G" + grade, item.Key, item.Sum((StockAllocation a) => a.Net), item.Sum((StockAllocation a) => a.Splice), item.Sum((StockAllocation a) => a.Waste), item.Sum((StockAllocation a) => a.Purchase)));
				}
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "GRADE {0} TOTAL TO BUY: {1:0.00} kg", grade, source.Sum((StockAllocation a) => a.Purchase)));
				b.AppendLine();
			}
			b.AppendLine("GRAND TOTAL TO BUY: " + plan.Sum((StockAllocation a) => a.Purchase).ToString("0.00", CultureInfo.InvariantCulture) + " kg");
			if (!includeOrders)
			{
				return;
			}
			b.AppendLine();
			b.AppendLine("SELECTED COMMERCIAL BARS TO ORDER");
			b.AppendLine(string.Format("{0,-8}{1,6}{2,13}{3,13}{4,16}", "GRADE", "DIA", "LENGTH m", "WHOLE BARS", "BUY kg"));
			foreach (var item2 in from a in plan.SelectMany((StockAllocation a) => a.Stocks).Distinct()
				group a by new { a.Grade, a.Diameter, a.Length } into g
				orderby g.Key.Grade, g.Key.Diameter, g.Key.Length
				select g)
			{
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,-8}{1,6}{2,13:0.00}{3,13}{4,16:0.00}", "G" + item2.Key.Grade, item2.Key.Diameter, item2.Key.Length, item2.Sum((RebarStock a) => a.Count), item2.Sum((RebarStock a) => a.Length * (double)a.Count * EstimatorEngine.UnitMass(a.Diameter))));
			}
		}

		public static void Basis(StringBuilder b)
		{
			b.AppendLine();
			b.AppendLine("REPORT BASIS");
			b.AppendLine("Buy kg = cut steel + added splice steel + stock offcuts. No percentage factor.");
			b.AppendLine("Entry weights share pooled whole-bar orders; order quantities appear in the summary.");
			b.AppendLine("Compatible different-length cuts share stock/offcuts; grade and diameter stay separate; kerf = 0.");
			b.AppendLine("Gross concrete excludes opening/overlap deductions. Forms exclude top/end faces.");
			b.AppendLine("Splice positions follow the structural drawings. Totals use unrounded values.");
		}

		private static string Part(StockAllocation a)
		{
			if (!(a.Line.Part == "Side-face"))
			{
				if (!(a.Line.Part == "Stirrups"))
				{
					return EstimatorEngine.DisplayPart(a.Line.Part);
				}
				if (!a.Line.TieOverride)
				{
					if (!(Math.Abs(a.Demand.Cut - a.Line.MainHoopCut) < 1E-09))
					{
						return "Additional leg (90+130)";
					}
					return "Main stirrup / hoop";
				}
				return "Manual stirrup set";
			}
			return "Side-face / each face";
		}

		private static string Group(StockAllocation a)
		{
			string part = a.Line.Part;
			if (part == "Stirrups")
			{
				return "STIRRUPS";
			}
			if (part == "Side-face")
			{
				return "SIDE-FACE BARS";
			}
			if (part.StartsWith("Top distribution", StringComparison.OrdinalIgnoreCase))
			{
				return "TOP DISTRIBUTION";
			}
			if (part.StartsWith("Top", StringComparison.OrdinalIgnoreCase))
			{
				return "TOP BARS";
			}
			if (part.StartsWith("Bottom", StringComparison.OrdinalIgnoreCase))
			{
				return "BOTTOM BARS";
			}
			return "MAIN BARS";
		}

		public static void Entry(StringBuilder b, List<StockAllocation> items, int quantity, bool extensive)
		{
			b.AppendLine();
			b.AppendLine("REINFORCEMENT TAKEOFF");
			b.AppendLine(string.Format("{0,-33}{1,8}{2,10}{3,10}{4,13}{5,13}", "BAR / POSITION", "GR/DIA", "PCS/UNIT", "TOTAL PCS", "CUT mm/pc", "BUY kg"));
			b.AppendLine(new string('-', 90));
			foreach (IGrouping<string, StockAllocation> item in items.GroupBy(Group))
			{
				b.AppendLine(item.Key);
				foreach (StockAllocation item2 in item)
				{
					b.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,-33}{1,8}{2,10}{3,10}{4,13:0.00}{5,13:0.00}", Part(item2), "G" + item2.Demand.Grade + "/" + item2.Demand.Diameter, item2.Demand.Pieces / quantity, item2.Demand.Pieces, item2.Demand.Cut * 1000.0, item2.Purchase));
					if (item2.Choice == null)
					{
						b.AppendLine("  REVIEW: commercial cut unresolved; excluded from purchase.");
					}
					else if (item2.Choice.Segments > 1)
					{
						b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Split: {0} segments x {1:0.00}m; {2} lap(s) x {3:0.00}m per piece.", item2.Choice.Segments, item2.Choice.SegmentCut, item2.Choice.Segments - 1, item2.Choice.Lap));
					}
				}
				b.AppendLine();
			}
			b.AppendLine(new string('-', 90));
			b.AppendLine("TOTAL TO BUY: " + items.Where((StockAllocation a) => a.Choice != null).Sum((StockAllocation a) => a.Purchase).ToString("0.00", CultureInfo.InvariantCulture) + " kg");
			if (!extensive)
			{
				return;
			}
			b.AppendLine();
			b.AppendLine("CALCULATION TRACE");
			foreach (StockAllocation item3 in items)
			{
				b.AppendLine(Part(item3) + " | G" + item3.Demand.Grade + " / D" + item3.Demand.Diameter);
				b.AppendLine("  " + item3.Line.Formula);
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  {0} pcs x {1:0.00}mm / 1000 x {2:0.000}kg/m = {3:0.00}kg cut steel", item3.Demand.Pieces, item3.Demand.Cut * 1000.0, EstimatorEngine.UnitMass(item3.Demand.Diameter), item3.Net));
				if (item3.Splice > 0.0 || item3.Waste > 0.0)
				{
					b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Added lap {0:0.00}kg | offcut {1:0.00}kg", item3.Splice, item3.Waste));
				}
				b.AppendLine();
			}
		}
	}
}
