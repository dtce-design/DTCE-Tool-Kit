using System;
using System.Collections.Generic;
using System.Linq;

namespace BeamScheduleFormatter
{
	internal static class SteelCutPlanner
	{
		public const double StockLength = 6.0;

		public static double PurchaseKg(IEnumerable<SteelStock> stocks, SteelCutDemand demand)
		{
			return stocks.Sum((SteelStock stock) => (!(stock.Net <= 0.0)) ? (6.0 * stock.KgM * stock.Cuts.Where((SteelCut c) => c.Demand == demand).Sum((SteelCut c) => c.Demand.Length) / stock.Net) : 0.0);
		}

		public static List<SteelStock> Allocate(IEnumerable<SteelCutDemand> source)
		{
			List<SteelCutDemand> list = source.ToList();
			if (((IEnumerable<SteelCutDemand>)list).Sum((Func<SteelCutDemand, long>)((SteelCutDemand d) => d.Quantity)) > 100000)
			{
				throw new Exception("Cut plan supports up to 100000 pieces.");
			}
			foreach (SteelCutDemand item in list)
			{
				SteelWeight.Positive(item.Length, item.KgM);
				SteelWeight.Count(item.Quantity);
				if (string.IsNullOrWhiteSpace(item.Member) || string.IsNullOrWhiteSpace(item.Specification) || item.Length > 6.000000001)
				{
					throw new Exception("Each steel cut must fit a 6m stock. Define an approved splice separately for longer members.");
				}
			}
			List<SteelStock> list2 = new List<SteelStock>();
			int num = 1;
			foreach (IGrouping<string, SteelCutDemand> group in list.GroupBy((SteelCutDemand d) => d.Specification, StringComparer.OrdinalIgnoreCase))
			{
				if (group.Any((SteelCutDemand d) => Math.Abs(d.KgM - group.First().KgM) > 1E-08))
				{
					throw new Exception("The same specification must have the same kg/m.");
				}
				SortedSet<SteelStock> sortedSet = new SortedSet<SteelStock>(new SteelStockComparer());
				foreach (SteelCutDemand item2 in group.OrderByDescending((SteelCutDemand d) => d.Length).ThenBy((SteelCutDemand d) => d.Member, StringComparer.Ordinal))
				{
					for (int num2 = 1; num2 <= item2.Quantity; num2++)
					{
						SteelStock lowerValue = new SteelStock
						{
							Remaining = item2.Length - 1E-09,
							Number = int.MinValue
						};
						SteelStock upperValue = new SteelStock
						{
							Remaining = double.MaxValue,
							Number = int.MaxValue
						};
						SteelStock steelStock = sortedSet.GetViewBetween(lowerValue, upperValue).Min;
						if (steelStock == null)
						{
							steelStock = new SteelStock
							{
								Number = num++,
								Specification = group.Key,
								KgM = item2.KgM
							};
							list2.Add(steelStock);
						}
						else
						{
							sortedSet.Remove(steelStock);
						}
						steelStock.Remaining = Math.Max(0.0, steelStock.Remaining - item2.Length);
						steelStock.Cuts.Add(new SteelCut
						{
							Demand = item2,
							Piece = num2,
							RemainingAfter = steelStock.Remaining
						});
						sortedSet.Add(steelStock);
					}
				}
			}
			return list2;
		}
	}
}
