using System;
using System.Globalization;

namespace BeamScheduleFormatter
{
	internal static class ChamberInputs
	{
		public static ChamberTakeoff Read(string[] v)
		{
			Func<int, double> n = delegate(int c)
			{
				if (v[c] == "")
				{
					return 0.0;
				}
				double result;
				if (!double.TryParse(v[c], NumberStyles.Float, CultureInfo.InvariantCulture, out result) || double.IsNaN(result) || double.IsInfinity(result))
				{
					throw new Exception("Enter valid numeric dimensions.");
				}
				return result;
			};
			Func<int, int> func = delegate(int c)
			{
				double num = n(c);
				if (num != Math.Truncate(num) || num < 0.0 || num > 1000000.0)
				{
					throw new Exception("Counts and face selections must be nonnegative whole numbers.");
				}
				return (int)num;
			};
			ChamberTakeoff chamberTakeoff = new ChamberTakeoff();
			chamberTakeoff.Id = v[0];
			chamberTakeoff.Description = v[1];
			chamberTakeoff.Kind = v[2];
			chamberTakeoff.L = n(3);
			chamberTakeoff.W = n(4);
			chamberTakeoff.H = n(5);
			chamberTakeoff.Count = func(6);
			chamberTakeoff.Wall = n(7);
			chamberTakeoff.Base = n(8);
			chamberTakeoff.Cover = n(9);
			chamberTakeoff.OpeningL = n(10);
			chamberTakeoff.OpeningW = n(11);
			chamberTakeoff.PartitionLength = n(12);
			chamberTakeoff.PartitionThickness = n(13);
			chamberTakeoff.WallOpeningArea = n(14);
			chamberTakeoff.Gravel = n(15);
			chamberTakeoff.Allowance = n(16);
			chamberTakeoff.SoilCover = n(17);
			chamberTakeoff.OtherConcrete = n(18);
			chamberTakeoff.OtherForms = n(19);
			chamberTakeoff.PlasterFaces = func(20);
			chamberTakeoff.PlasterFloor = v[21].Equals("True", StringComparison.OrdinalIgnoreCase);
			chamberTakeoff.WaterproofFaces = func(22);
			chamberTakeoff.WaterproofFloor = v[23].Equals("True", StringComparison.OrdinalIgnoreCase);
			chamberTakeoff.LsQuantity = n(24);
			chamberTakeoff.Calculate();
			return chamberTakeoff;
		}
	}
}
