using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace BeamScheduleFormatter
{
	internal static class QuantityMath
	{
		public static void Finite(params double[] values)
		{
			if (values.Any((double x) => double.IsNaN(x) || double.IsInfinity(x) || x < 0.0 || x > 1000000000.0))
			{
				throw new Exception("Use nonnegative finite quantities.");
			}
		}

		public static double Station(string text)
		{
			Match match = Regex.Match((text ?? "").Trim(), "^(\\d+)\\+(\\d+(?:\\.\\d+)?)$");
			double result;
			if (match.Success)
			{
				double num = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
				if (num >= 1000.0)
				{
					throw new Exception("Station offset must be below 1000m.");
				}
				result = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) * 1000.0 + num;
			}
			else if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
			{
				throw new Exception("Station: enter metres or 0+020.");
			}
			Finite(result);
			return result;
		}
	}
}
