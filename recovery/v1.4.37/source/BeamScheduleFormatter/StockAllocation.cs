using System.Collections.Generic;

namespace BeamScheduleFormatter
{
	internal class StockAllocation
	{
		public EstimateInput Entry;

		public EstimateLine Line;

		public StockDemand Demand;

		public StockChoice Choice;

		public double Purchase;

		public double Splice;

		public double Waste;

		public double? UnitMassOverride;

		public List<double> Allowed;

		public readonly List<RebarStock> Stocks = new List<RebarStock>();

		public double UnitMass
		{
			get
			{
				return UnitMassOverride ?? EstimatorEngine.UnitMass(Demand.Diameter);
			}
		}

		public double Net
		{
			get
			{
				return Demand.Cut * (double)Demand.Pieces * UnitMass;
			}
		}
	}
}
