using System;

namespace BeamScheduleFormatter
{
	internal class BeamQuantities
	{
		public double Concrete;

		public double SideForms;

		public double BottomForms;

		public double Formwork
		{
			get
			{
				return SideForms + BottomForms;
			}
		}

		public static BeamQuantities Calculate(double widthMm, double depthMm, double length, int quantity, bool gravel, double slabMm)
		{
			if (double.IsNaN(widthMm + depthMm + length) || double.IsInfinity(widthMm + depthMm + length) || widthMm <= 0.0 || depthMm <= 0.0 || length <= 0.0 || quantity <= 0)
			{
				throw new Exception("Check B, D, length, quantity.");
			}
			double num = widthMm / 1000.0;
			double num2 = depthMm / 1000.0;
			return new BeamQuantities
			{
				Concrete = num * num2 * length * (double)quantity,
				SideForms = 2.0 * num2 * length * (double)quantity,
				BottomForms = (gravel ? 0.0 : (num * length * (double)quantity))
			};
		}
	}
}
