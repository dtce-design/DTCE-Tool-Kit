using System.Collections.Generic;
using System.Linq;

namespace BeamScheduleFormatter
{
	internal static class ManualEntryQuantities
	{
		public static List<BillQuantity> Group(IEnumerable<BillQuantity> inputs)
		{
			return (from q in inputs
				group q by new
				{
					Description = FinishTypeRegistry.Key(q.Description),
					Unit = BackupMapping.Unit(q.Unit)
				}).Select(g =>
			{
				BillQuantity billQuantity = new BillQuantity();
				billQuantity.Section = "Bill of Quantities";
				billQuantity.Description = g.First().Description;
				billQuantity.Unit = g.Key.Unit;
				billQuantity.Quantity = g.Sum((BillQuantity x) => x.Quantity);
				billQuantity.Sources.AddRange(g.SelectMany((BillQuantity x) => x.Sources));
				return billQuantity;
			}).ToList();
		}
	}
}
