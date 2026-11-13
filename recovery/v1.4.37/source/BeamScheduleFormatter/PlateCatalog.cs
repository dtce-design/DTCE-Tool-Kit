using System;
using System.Linq;

namespace BeamScheduleFormatter
{
	internal static class PlateCatalog
	{
		public const string Source = "Regan 2026 handbook PDF pp.36-38, printed pp.25-27; mild steel ASTM A36 / JIS G3101";

		public static readonly double[] Thicknesses = new double[77]
		{
			1.2, 1.4, 1.5, 1.8, 1.9, 2.0, 2.2, 2.3, 2.4, 2.5,
			2.6, 2.7, 2.8, 2.9, 3.0, 3.2, 3.4, 3.5, 3.7, 3.8,
			4.0, 4.2, 4.3, 4.5, 4.7, 4.8, 5.0, 5.2, 5.3, 5.5,
			5.7, 5.8, 6.0, 6.3, 6.5, 7.0, 7.5, 7.8, 8.0, 8.5,
			8.7, 9.0, 9.5, 9.7, 9.8, 10.0, 10.5, 10.8, 11.0, 11.5,
			11.8, 12.0, 13.0, 14.0, 15.0, 16.0, 17.0, 18.0, 19.0, 20.0,
			22.0, 24.0, 25.0, 28.0, 30.0, 32.0, 35.0, 36.0, 38.0, 40.0,
			45.0, 50.0, 60.0, 63.0, 75.0, 90.0, 100.0
		};

		public static readonly PlateFormat[] Formats = new PlateFormat[4]
		{
			Format(4, 8),
			Format(5, 20),
			Format(6, 20),
			Format(8, 20)
		};

		private static PlateFormat Format(int width, int length)
		{
			return new PlateFormat
			{
				Name = width + " ft x " + length + " ft",
				Width = (double)width * 0.3048,
				Length = (double)length * 0.3048
			};
		}

		public static bool HasThickness(double mm)
		{
			return Thicknesses.Any((double t) => Math.Abs(t - mm) < 1E-08);
		}
	}
}
