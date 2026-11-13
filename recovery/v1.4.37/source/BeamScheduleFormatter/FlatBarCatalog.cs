using System;
using System.Collections.Generic;
using System.Globalization;

namespace BeamScheduleFormatter
{
	internal static class FlatBarCatalog
	{
		public static void AddTo(List<SteelProfile> profiles)
		{
			int[] array = new int[17]
			{
				2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
				12, 13, 14, 15, 16, 18, 20
			};
			for (int i = 0; i < array.Length; i++)
			{
				int num = array[i];
				double[] array2 = new double[12]
				{
					3.0, 4.5, 6.0, 8.0, 9.0, 9.3, 10.0, 12.0, 15.0, 18.0,
					20.0, 24.0
				};
				for (int j = 0; j < array2.Length; j++)
				{
					double num2 = array2[j];
					int num3 = ((num <= 7) ? 20 : ((num <= 13) ? 21 : 22));
					double num4 = (double)num * 25.4;
					profiles.Add(new SteelProfile
					{
						Name = "FB " + num + " in x " + num2.ToString("0.###", CultureInfo.InvariantCulture) + " mm",
						Kind = "Flat bar",
						H = num4,
						B = num4,
						T = num2,
						KgM = Math.Round(num4 * num2 * 0.00785, 2, MidpointRounding.AwayFromZero),
						Source = "Regan 2026 handbook PDF p." + num3 + ", printed p." + (num3 - 8) + "; ASTM A36"
					});
				}
			}
		}
	}
}
