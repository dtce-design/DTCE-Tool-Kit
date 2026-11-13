using System.Collections.Generic;

namespace BeamScheduleFormatter
{
	internal class SlabTakeoff
	{
		public EstimateInput Steel;

		public SlabScheduleRow Schedule;

		public string GeometryBasis;

		public double Thickness;

		public SlabQuantities Measures;

		public List<string> Notes = new List<string>();
	}
}
