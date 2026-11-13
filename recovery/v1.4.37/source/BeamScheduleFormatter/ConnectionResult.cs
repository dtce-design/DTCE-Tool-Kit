using System.Collections.Generic;
using System.Linq;

namespace BeamScheduleFormatter
{
	internal class ConnectionResult
	{
		public readonly List<ConnectionCheck> Checks = new List<ConnectionCheck>();

		public readonly List<string> DetailErrors = new List<string>();

		public double U;

		public double NominalWeld;

		public double NominalBolt;

		public double NominalBearing;

		public double NominalRupture;

		public bool ListedChecksPass
		{
			get
			{
				if (DetailErrors.Count == 0)
				{
					return Checks.All((ConnectionCheck c) => c.Pass);
				}
				return false;
			}
		}
	}
}
