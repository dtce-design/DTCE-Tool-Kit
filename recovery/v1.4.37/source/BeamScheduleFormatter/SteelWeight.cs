using System;
using System.Linq;

namespace BeamScheduleFormatter
{
	internal static class SteelWeight
	{
		public const double Density = 7850.0;

		public static double Plate(double area, double thicknessMm, int quantity)
		{
			Positive(area, thicknessMm);
			Count(quantity);
			return area * thicknessMm / 1000.0 * 7850.0 * (double)quantity;
		}

		public static double Rod(double diameterMm, double overallMetres, int quantity)
		{
			Positive(diameterMm, overallMetres);
			Count(quantity);
			return Math.PI * Math.Pow(diameterMm / 1000.0, 2.0) / 4.0 * overallMetres * 7850.0 * (double)quantity;
		}

		public static double Diagonal(double x, double y, double height)
		{
			double[] array = new double[3] { x, y, height };
			foreach (double num in array)
			{
				if (double.IsNaN(num) || double.IsInfinity(num) || num < 0.0 || num > 100000.0)
				{
					throw new Exception("Bracing distances must be nonnegative finite metres.");
				}
			}
			double num2 = Math.Sqrt(x * x + y * y + height * height);
			Positive(num2);
			return num2;
		}

		public static void Positive(params double[] values)
		{
			if (values.Any((double v) => double.IsNaN(v) || double.IsInfinity(v) || v <= 0.0 || v > 1000000000000.0))
			{
				throw new Exception("Enter positive finite dimensions and unit weights.");
			}
		}

		public static void Count(int quantity)
		{
			if (quantity < 1 || quantity > 100000)
			{
				throw new Exception("Quantity must be 1 to 100000 whole pieces.");
			}
		}
	}
}
