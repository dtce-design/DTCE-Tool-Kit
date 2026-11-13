using System;
using System.Collections.Generic;
using System.Linq;

namespace BeamScheduleFormatter
{
	internal static class RebarOffcuts
	{
		public static void Allocate(List<StockAllocation> rows)
		{
			foreach (StockAllocation row in rows)
			{
				row.Purchase = (row.Splice = (row.Waste = 0.0));
				row.Stocks.Clear();
			}
			foreach (var item in from stockAllocation in rows
				where stockAllocation.Choice != null
				group stockAllocation by new
				{
					stockAllocation.Demand.Grade,
					stockAllocation.Demand.Diameter
				})
			{
				List<RebarStock> list = new List<RebarStock>();
				foreach (StockAllocation a in item.OrderByDescending((StockAllocation stockAllocation) => stockAllocation.Choice.SegmentCut))
				{
					double cut = a.Choice.SegmentCut;
					long num = checked(a.Demand.Pieces * a.Choice.Segments);
					foreach (RebarStock item2 in (from s in list
						where s.Remaining + 1E-10 >= cut && (a.Allowed == null || a.Allowed.Contains(s.Length))
						orderby s.Remaining, s.Length
						select s).ToList())
					{
						if (num == 0L)
						{
							break;
						}
						long num2 = (long)Math.Floor((item2.Remaining + 1E-10) / cut);
						while (num2 > 0 && (double)num2 * cut > item2.Remaining + 1E-09)
						{
							num2--;
						}
						if (num2 > 0)
						{
							long num3 = Math.Min(item2.Count, num / num2);
							if (num3 > 0)
							{
								RebarStock rebarStock = item2.Copy(num3);
								item2.Count -= num3;
								rebarStock.Remaining = Math.Max(0.0, rebarStock.Remaining - (double)num2 * cut);
								rebarStock.Used[a] = (double)num2 * cut;
								list.Add(rebarStock);
								num -= checked(num3 * num2);
							}
							if (num > 0 && item2.Count > 0)
							{
								long num4 = Math.Min(num2, num);
								RebarStock rebarStock2 = item2.Copy(1L);
								item2.Count--;
								rebarStock2.Remaining = Math.Max(0.0, rebarStock2.Remaining - (double)num4 * cut);
								rebarStock2.Used[a] = (double)num4 * cut;
								list.Add(rebarStock2);
								num -= num4;
							}
						}
					}
					if (num > 0)
					{
						long num5 = (long)Math.Floor((a.Choice.Length + 1E-10) / cut);
						if (num5 < 1)
						{
							throw new Exception("Rebar segment does not fit selected stock.");
						}
						long num6 = num / num5;
						long num7 = num % num5;
						if (num6 > 0)
						{
							RebarStock rebarStock3 = new RebarStock
							{
								Grade = item.Key.Grade,
								Diameter = item.Key.Diameter,
								Length = a.Choice.Length,
								Remaining = Math.Max(0.0, a.Choice.Length - (double)num5 * cut),
								Count = num6
							};
							rebarStock3.Used.Add(a, (double)num5 * cut);
							list.Add(rebarStock3);
						}
						if (num7 > 0)
						{
							RebarStock rebarStock4 = new RebarStock
							{
								Grade = item.Key.Grade,
								Diameter = item.Key.Diameter,
								Length = a.Choice.Length,
								Remaining = Math.Max(0.0, a.Choice.Length - (double)num7 * cut),
								Count = 1L
							};
							rebarStock4.Used.Add(a, (double)num7 * cut);
							list.Add(rebarStock4);
						}
					}
				}
				foreach (RebarStock item3 in list.Where((RebarStock s) => s.Count > 0))
				{
					double num8 = item3.Used.Values.Sum();
					double unitMass = item.First().UnitMass;
					foreach (KeyValuePair<StockAllocation, double> item4 in item3.Used)
					{
						StockAllocation key = item4.Key;
						key.Stocks.Add(item3);
						key.Waste += item3.Remaining * (double)item3.Count * item4.Value / num8 * unitMass;
					}
				}
				foreach (StockAllocation item5 in item)
				{
					item5.Splice = (double)(item5.Demand.Pieces * (item5.Choice.Segments - 1)) * item5.Choice.Lap * item5.UnitMass;
					item5.Purchase = item5.Net + item5.Splice + item5.Waste;
				}
			}
		}
	}
}
