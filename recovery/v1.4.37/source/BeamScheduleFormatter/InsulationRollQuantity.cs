using System;

namespace BeamScheduleFormatter
{
	internal class InsulationRollQuantity
	{
		public double NetArea;

		public double RollArea;

		public double PurchaseArea;

		public int Rolls;

		public static InsulationRollQuantity Calculate(double area, double width, double length)
		{
			RoofMath.Positive(width, length);
			if (double.IsNaN(area) || double.IsInfinity(area) || area < 0.0)
			{
				throw new Exception("Invalid insulation area.");
			}
			double num = width * length;
			int num2 = ((area != 0.0) ? RoofMath.Ceil(area / num) : 0);
			return new InsulationRollQuantity
			{
				NetArea = area,
				RollArea = num,
				Rolls = num2,
				PurchaseArea = (double)num2 * num
			};
		}
	}
}
