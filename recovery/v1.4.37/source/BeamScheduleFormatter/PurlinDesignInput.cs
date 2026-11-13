using System;
using System.Linq;

namespace BeamScheduleFormatter
{
	internal class PurlinDesignInput
	{
		public SteelProfile Profile;

		public PurlinProperties Properties;

		public string Name = "Purlin";

		public string Support = "Simply supported";

		public string Ceiling = "Not supporting ceiling";

		public int SagRods;

		public double Span = 5.0;

		public double Spacing = 0.4;

		public double Angle = 13.0;

		public double Fy = 248.0;

		public double E = 200000.0;

		public double Dead = 0.35;

		public double Live = 1.0;

		public double WindPressure;

		public double WindSuction;

		public bool AlternateIncrease = true;

		public void Validate()
		{
			if (Profile == null || Profile.Kind != "Purlin" || Properties == null)
			{
				throw new Exception("Choose a profile and section properties.");
			}
			Properties.Validate();
			SteelWeight.Positive(Span, Spacing, Fy, E);
			if (Span > 100.0 || Spacing > 20.0 || Fy > 1000.0 || E < 10000.0 || E > 500000.0 || double.IsNaN(Angle) || double.IsInfinity(Angle) || Angle < 0.0 || Angle >= 90.0 || SagRods < 0 || SagRods > 2)
			{
				throw new Exception("Check span, spacing, material, roof angle and sag rods (0-2).");
			}
			double[] array = new double[4] { Dead, Live, WindPressure, WindSuction };
			foreach (double num in array)
			{
				if (double.IsNaN(num) || double.IsInfinity(num) || Math.Abs(num) > 1000.0)
				{
					throw new Exception("Check finite loads in kPa.");
				}
			}
			if (Dead < 0.0 || Live < 0.0)
			{
				throw new Exception("Dead and live area loads must be nonnegative.");
			}
			if (!new string[2] { "Simply supported", "Cantilever" }.Contains(Support) || !new string[3] { "Not supporting ceiling", "Supporting nonplaster ceiling", "Supporting plaster ceiling" }.Contains(Ceiling))
			{
				throw new Exception("Choose support and ceiling type.");
			}
		}
	}
}
