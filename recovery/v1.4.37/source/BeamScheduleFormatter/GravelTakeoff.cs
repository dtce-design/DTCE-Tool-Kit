namespace BeamScheduleFormatter
{
	internal class GravelTakeoff
	{
		public static double Volume(double length, double width, double thicknessMm, int quantity)
		{
			SteelWeight.Positive(length, width, thicknessMm);
			SteelWeight.Count(quantity);
			return length * width * thicknessMm / 1000.0 * (double)quantity;
		}
	}
}
