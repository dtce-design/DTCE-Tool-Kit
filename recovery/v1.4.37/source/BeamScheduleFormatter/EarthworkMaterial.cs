using System;

namespace BeamScheduleFormatter
{
	internal class EarthworkMaterial
	{
		public string Id;

		public string Source;

		public double Bank;

		public double Fill;

		public double Swell = 20.0;

		public double Shrink = 10.0;

		public double Reusable = 100.0;

		public double LooseAvailable
		{
			get
			{
				return Bank * Reusable / 100.0 * (1.0 + Swell / 100.0);
			}
		}

		public double CompactedAvailable
		{
			get
			{
				return Bank * Reusable / 100.0 * (1.0 - Shrink / 100.0);
			}
		}

		public void Validate()
		{
			QuantityMath.Finite(Bank, Fill, Swell, Shrink, Reusable);
			if (Swell > 200.0 || Shrink >= 100.0 || Reusable > 100.0)
			{
				throw new Exception("Soil factors: swell 0-200%, bank shrinkage below 100%, suitable soil 0-100%.");
			}
		}
	}
}
