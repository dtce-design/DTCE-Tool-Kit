using System;
using System.Globalization;

namespace BeamScheduleFormatter
{
	internal class PipeMeasure
	{
		public string Id;

		public string Location;

		public string Specification;

		public double Length;

		public int Runs;

		public double Total
		{
			get
			{
				if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(Location) || string.IsNullOrWhiteSpace(Specification))
				{
					throw new Exception("Enter Pipe ID, location and full diameter/specification.");
				}
				SteelWeight.Positive(Length);
				SteelWeight.Count(Runs);
				return Length * (double)Runs;
			}
		}

		public string Computation
		{
			get
			{
				return Length.ToString("0.########", CultureInfo.InvariantCulture) + " m x " + Runs + " run(s) = " + Total.ToString("0.########", CultureInfo.InvariantCulture) + " m";
			}
		}
	}
}
