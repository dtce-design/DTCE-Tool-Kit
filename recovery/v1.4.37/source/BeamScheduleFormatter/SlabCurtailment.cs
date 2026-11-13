using System;

namespace BeamScheduleFormatter
{
	internal static class SlabCurtailment
	{
		public static double BottomOffset(double span)
		{
			Validate(span);
			return Up25(span / 6.0);
		}

		public static double BottomMiddleBody(double span)
		{
			Validate(span);
			return span - 2.0 * BottomOffset(span);
		}

		public static double TopPanelExtension(double span)
		{
			Validate(span);
			return Up25(span / 4.0);
		}

		public static double TopContinuousBody(double leftSpan, double rightSpan, double supportWidth)
		{
			Validate(leftSpan);
			Validate(rightSpan);
			if (double.IsNaN(supportWidth) || double.IsInfinity(supportWidth) || supportWidth < 0.0)
			{
				throw new ArgumentException("Support width must be nonnegative.");
			}
			return TopPanelExtension(leftSpan) + supportWidth + TopPanelExtension(rightSpan);
		}

		public static double Up25(double metres)
		{
			Validate(metres);
			return Math.Ceiling(Math.Round(metres * 1000.0 / 25.0, 10)) * 25.0 / 1000.0;
		}

		private static void Validate(double span)
		{
			if (double.IsNaN(span) || double.IsInfinity(span) || span <= 0.0)
			{
				throw new ArgumentException("Enter a positive slab span.");
			}
		}
	}
}
