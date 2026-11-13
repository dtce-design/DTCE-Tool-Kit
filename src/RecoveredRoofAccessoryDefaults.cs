namespace BeamScheduleFormatter
{
	internal static class RoofAccessoryDefaults
	{
		public static decimal LapMm(string type)
		{
			return (type == "Gutter") ? 150 : ((type == "Ridge Roll") ? 300 : 100);
		}
	}
}
