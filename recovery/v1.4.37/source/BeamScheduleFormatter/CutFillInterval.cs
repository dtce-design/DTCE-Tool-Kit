using System;
using System.Collections.Generic;
using System.Linq;

namespace BeamScheduleFormatter
{
	internal class CutFillInterval
	{
		public string Alignment;

		public double Start;

		public double End;

		public double CutStart;

		public double CutEnd;

		public double FillStart;

		public double FillEnd;

		public double Cut
		{
			get
			{
				return (CutStart + CutEnd) / 2.0 * (End - Start);
			}
		}

		public double Fill
		{
			get
			{
				return (FillStart + FillEnd) / 2.0 * (End - Start);
			}
		}

		public void Validate()
		{
			QuantityMath.Finite(Start, End, CutStart, CutEnd, FillStart, FillEnd);
			if (string.IsNullOrWhiteSpace(Alignment) || End <= Start)
			{
				throw new Exception("Enter alignment ID and station end greater than start.");
			}
		}

		public static void CheckOverlap(IEnumerable<CutFillInterval> list)
		{
			foreach (IGrouping<string, CutFillInterval> item in list.GroupBy((CutFillInterval x) => x.Alignment.Trim(), StringComparer.OrdinalIgnoreCase))
			{
				double num = -1.0;
				foreach (CutFillInterval item2 in item.OrderBy((CutFillInterval x) => x.Start))
				{
					item2.Validate();
					if (item2.Start < num - 1E-08)
					{
						throw new Exception("Overlapping station intervals: " + item.Key);
					}
					num = item2.End;
				}
			}
		}
	}
}
