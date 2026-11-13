using System.Collections.Generic;

namespace BeamScheduleFormatter
{
	internal class SteelStockComparer : IComparer<SteelStock>
	{
		public int Compare(SteelStock a, SteelStock b)
		{
			int num = a.Remaining.CompareTo(b.Remaining);
			if (num == 0)
			{
				return a.Number.CompareTo(b.Number);
			}
			return num;
		}
	}
}
