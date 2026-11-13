using System;

namespace BeamScheduleFormatter
{
	internal static class OpeningMoulding
	{
		public static double Length(string kind, double width, double height, int quantity, bool exterior)
		{
			if (kind != "Door" && kind != "Window")
			{
				throw new Exception("Select Door or Window for moulding measurement.");
			}
			SteelWeight.Positive(width, height);
			SteelWeight.Count(quantity);
			if (!exterior)
			{
				return 0.0;
			}
			return (2.0 * height + ((kind == "Window") ? (2.0 * width) : width)) * (double)quantity;
		}
	}
}
