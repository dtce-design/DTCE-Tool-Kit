using System;

namespace BeamScheduleFormatter
{
	internal static class OpeningHardware
	{
		public const double WoodDoorThickness = 0.045;

		public const string Source = "https://hagerco.freshdesk.com/support/solutions/articles/73000552399-determine-the-size-of-hinge";

		public static int PerLeaf(double height, double width)
		{
			if (double.IsNaN(height + width) || double.IsInfinity(height + width) || height <= 0.0 || width <= 0.0)
			{
				throw new Exception("Check door leaf height and width.");
			}
			int num = Math.Max(2, checked((int)Math.Ceiling(Math.Round(height / 0.762, 10))));
			if (width > 0.94)
			{
				num++;
			}
			return num;
		}
	}
}
