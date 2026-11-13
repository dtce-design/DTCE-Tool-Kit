using System.Collections.Generic;
using System.Linq;

namespace BeamScheduleFormatter
{
	internal class SteelStock
	{
		public int Number;

		public string Specification;

		public double KgM;

		public double Remaining = 6.0;

		public readonly List<SteelCut> Cuts = new List<SteelCut>();

		public double Net
		{
			get
			{
				return Cuts.Sum((SteelCut c) => c.Demand.Length);
			}
		}
	}
}
