using System.Collections.Generic;

namespace BeamScheduleFormatter
{
	internal class RebarStock
	{
		public int Grade;

		public int Diameter;

		public long Count;

		public double Length;

		public double Remaining;

		public readonly Dictionary<StockAllocation, double> Used = new Dictionary<StockAllocation, double>();

		public RebarStock Copy(long count)
		{
			RebarStock rebarStock = new RebarStock
			{
				Grade = Grade,
				Diameter = Diameter,
				Count = count,
				Length = Length,
				Remaining = Remaining
			};
			foreach (KeyValuePair<StockAllocation, double> item in Used)
			{
				rebarStock.Used.Add(item.Key, item.Value);
			}
			return rebarStock;
		}
	}
}
