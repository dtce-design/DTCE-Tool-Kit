using System;

namespace BeamScheduleFormatter
{
	internal static class RoofMath
	{
		public static void Positive(params double[] values)
		{
			foreach (double num in values)
			{
				if (double.IsNaN(num) || double.IsInfinity(num) || num <= 0.0 || num > 1000000.0)
				{
					throw new Exception("Enter positive finite dimensions.");
				}
			}
		}

		public static int Ceil(double v)
		{
			if (v > 1000000.0)
			{
				throw new Exception("Quantity exceeds supported limit.");
			}
			return Math.Max(1, checked((int)Math.Ceiling(v - 1E-10)));
		}
	}
}
