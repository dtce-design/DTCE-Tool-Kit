using System;

namespace BeamScheduleFormatter
{
	internal static class HookGeometry
	{
		public static double Development(int dia, bool top, EstimateOptions o)
		{
			if (o.LengthConfig != null)
			{
				return o.LengthConfig.Rounded(o.LengthConfig.Values(dia, top).Ld) / 1000.0;
			}
			double num = ((o.GradeFor(dia) == 40) ? 280 : 420);
			return Math.Max(300.0, num / (1.1 * Math.Sqrt(o.Fc)) * (top ? 1.3 : 1.0) * ((dia <= 19) ? 0.8 : 1.0) / o.BondRatio * (double)dia) / 1000.0;
		}

		public static double Inside(int dia, bool tie)
		{
			if (dia < 10 || dia > 36)
			{
				throw new Exception("Standard geometry supports 10-36mm bars only.");
			}
			if (tie)
			{
				if (dia > 25)
				{
					throw new Exception("Tie diameters above 25mm require project bend detail.");
				}
				return ((dia <= 16) ? 4 : 6) * dia;
			}
			return ((dia <= 25) ? 6 : 8) * dia;
		}

		public static double MainEnd(int dia, int angle)
		{
			switch (angle)
			{
			case 0:
				return 0.0;
			default:
				throw new Exception("Use straight, 90 or 180 degree main hooks.");
			case 90:
			case 180:
			{
				double num = (Inside(dia, false) + (double)dia) / 2.0;
				double num2 = ((angle == 90) ? (12 * dia) : Math.Max(4 * dia, 65));
				return (num * (double)angle * Math.PI / 180.0 + num2) / 1000.0;
			}
			}
		}

		public static TiePartsValue TieParts(double width, double depth, int dia, int legs, EstimateOptions o)
		{
			LengthResult lengthResult = ((o.LengthConfig == null) ? null : o.LengthConfig.Values(dia, false));
			double num = (((lengthResult == null) ? Inside(dia, true) : lengthResult.TieInside) + (double)dia) / 2000.0;
			if (lengthResult != null && lengthResult.TieInside <= 0.0)
			{
				throw new Exception("Configuration needs project-specific tie bend diameter.");
			}
			double num2 = (width - 2.0 * o.Cover - (double)dia) / 1000.0;
			double num3 = (depth - 2.0 * o.Cover - (double)dia) / 1000.0;
			if (num2 < 2.0 * num || num3 < 2.0 * num)
			{
				throw new Exception("Beam/tie dimensions cannot fit the minimum bend diameter.");
			}
			double num4 = ((lengthResult == null) ? Math.Max(Math.Max(6.0, o.Hook) * (double)dia, 75.0) : lengthResult.Tail135) / 1000.0;
			double mainHoop = 2.0 * (num2 + num3) - 8.0 * num + Math.PI * 3.0 * num + 2.0 * num4;
			double num5 = ((dia <= 16) ? ((double)Math.Max(6 * dia, 75) / 1000.0) : ((double)(12 * dia) / 1000.0));
			double additionalLeg = num3 - 2.0 * num + 3.839724354387525 * num + num5 + num4;
			return new TiePartsValue
			{
				MainHoop = mainHoop,
				AdditionalLeg = additionalLeg,
				AdditionalCount = legs - 2
			};
		}

		public static double TieCut(double width, double depth, int dia, int legs, EstimateOptions o)
		{
			return TieParts(width, depth, dia, legs, o).Total;
		}
	}
}
