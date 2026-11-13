namespace BeamScheduleFormatter
{
	internal class EstimateInput
	{
		public string ExcavationDepth = "";

		public string WorkingAllowance = "0.30";

		public string Swell = "20";

		public string Shrink = "10";

		public string OtherBuriedConcrete = "0";

		public BeamEarthworks Earthworks;

		public string EarthworkError = "";

		public string Support = "Elevated slab";

		public string SlabThickness = "0";

		public string MeasureError = "";

		public BeamQuantities Measures;

		public string Description = "";

		public string ScheduleLabel = "";

		public string Beam;

		public BeamRow Source;

		public string Length = "";

		public string Quantity = "1";

		public string TieCount = "";

		public string TieCut = "";

		public BeamEstimate Result;

		public string Error = "";
	}
}
