using System;
using System.Globalization;

namespace BeamScheduleFormatter
{
	internal static class SlabEstimatorEngine
	{
		public static int Count(double width, double spacingMm, double coverMm)
		{
			if (spacingMm <= 0.0 || double.IsNaN(spacingMm) || double.IsInfinity(spacingMm) || width * 1000.0 <= 2.0 * coverMm)
			{
				throw new Exception("Check reinforcement spacing, width and cover.");
			}
			return checked((int)Math.Ceiling((width - 2.0 * coverMm / 1000.0) / (spacingMm / 1000.0) - 1E-10) + 1);
		}

		public static SlabTakeoff Calculate(SlabScheduleRow row, string description, double shorter, double longer, int quantity, bool elevated, double edge, double cover, LengthConfiguration config, int longCont, int longEnd, int shortCont, int shortEnd, double adjacentShort, double adjacentLong, bool cantileverEdge = false)
		{
			if (row == null || config == null)
			{
				throw new Exception("Choose a slab schedule and material configuration.");
			}
			config.Validate();
			if (adjacentShort < 0.0 || adjacentLong < 0.0 || double.IsNaN(adjacentShort + adjacentLong) || double.IsInfinity(adjacentShort + adjacentLong))
			{
				throw new Exception("Adjacent spans must be blank or nonnegative finite lengths.");
			}
			if (cover < 0.0 || double.IsNaN(cover) || double.IsInfinity(cover))
			{
				throw new Exception("Cover must be nonnegative.");
			}
			int[] array = new int[4] { longCont, longEnd, shortCont, shortEnd };
			foreach (int num in array)
			{
				if (num < 0 || num > 2)
				{
					throw new Exception("Support counts must be 0, 1 or 2.");
				}
			}
			if (longCont + longEnd > 2 || shortCont + shortEnd > 2)
			{
				throw new Exception("At most two opposite supports per direction.");
			}
			SlabTakeoff result = new SlabTakeoff
			{
				Schedule = row,
				GeometryBasis = "Short " + shorter.ToString("0.00") + "m; long " + longer.ToString("0.00") + "m; cover " + cover.ToString("0.00") + "mm; over LONG support CONT/END=" + longCont + "/" + longEnd + "; over SHORT CONT/END=" + shortCont + "/" + shortEnd,
				Thickness = row.Thickness,
				Measures = SlabQuantities.Calculate(shorter, longer, row.Thickness, quantity, elevated, edge, cantileverEdge),
				Steel = new EstimateInput
				{
					Description = description,
					Beam = row.Mark,
					Length = shorter.ToString(CultureInfo.InvariantCulture) + " x " + longer.ToString(CultureInfo.InvariantCulture),
					Quantity = quantity.ToString(),
					Result = new BeamEstimate()
				}
			};
			double countWidth = 0.0;
			int supportMultiplier = 1;
			Action<SlabBar, string, double, int, bool> action = delegate(SlabBar bar, string name, double body, int count, bool top)
			{
				if (bar != null && count != 0)
				{
					if (body <= 0.0)
					{
						throw new Exception("Cutoff leaves no positive bar body.");
					}
					LengthResult lengthResult = config.Values(bar.Diameter, top);
					double num15 = config.Rounded(lengthResult.Ld) / 1000.0;
					double num16 = SlabCurtailment.Up25(body + 2.0 * num15);
					int grade = ((bar.Diameter <= 12) ? config.GradeSmall : config.GradeLarge);
					double num17 = num16 * (double)count * (double)quantity;
					result.Steel.Result.Lines.Add(new EstimateLine
					{
						Part = name,
						Beam = row.Mark,
						Diameter = bar.Diameter,
						Grade = grade,
						PiecesPerBeam = count,
						CutLength = num16,
						Metres = num17,
						Kg = num17 * EstimatorEngine.UnitMass(bar.Diameter),
						Formula = supportMultiplier + " support(s) x [ceil((" + (countWidth * 1000.0).ToString("0.00") + "mm - 2 x " + cover.ToString("0.00") + "mm) / " + bar.SpacingMm.ToString("0.00") + "mm) + 1] = " + count + " pcs/unit; body " + (body * 1000.0).ToString("0.00") + "mm + 2Ld " + (2000.0 * num15).ToString("0.00") + "mm; final cut UP25mm"
					});
				}
			};
			for (int num2 = 0; num2 < 2; num2++)
			{
				double num3 = ((num2 == 0) ? shorter : longer);
				double num4 = ((num2 == 0) ? longer : shorter);
				int num5 = num2 * 2;
				countWidth = num4;
				supportMultiplier = 1;
				SlabBar slabBar = row.Bars[num5];
				if (slabBar != null)
				{
					action(slabBar, "Bottom " + ((num2 == 0) ? "short FULL" : "long FULL"), num3, Count(num4, slabBar.SpacingMm, cover), false);
				}
				slabBar = row.Bars[num5 + 1];
				if (slabBar != null)
				{
					action(slabBar, "Bottom " + ((num2 == 0) ? "short CURTAILED" : "long CURTAILED"), SlabCurtailment.BottomMiddleBody(num3), Count(num4, slabBar.SpacingMm, cover), false);
				}
			}
			for (int num6 = 0; num6 < 2; num6++)
			{
				double num7 = ((num6 == 0) ? shorter : longer);
				double num8 = ((num6 == 0) ? longer : shorter);
				double num9 = ((num6 == 0) ? adjacentShort : adjacentLong);
				int num10 = ((num6 == 0) ? longCont : shortCont);
				int num11 = ((num6 == 0) ? longEnd : shortEnd);
				int num12 = ((num6 == 0) ? 4 : 6);
				string text = ((num6 == 0) ? "LONG" : "SHORT");
				if (num9 <= 0.0)
				{
					num9 = num7;
					if (num10 > 0)
					{
						result.Notes.Add("Over " + text + " support: adjacent span assumed equal to entered " + num7.ToString("0.00") + "m. Override adjacent-span input for unequal panels.");
					}
				}
				bool[] array2 = new bool[2] { true, false };
				foreach (bool flag in array2)
				{
					int num13 = (flag ? num10 : num11);
					SlabBar slabBar2 = row.Bars[num12 + ((!flag) ? 1 : 0)];
					checked
					{
						if (num13 != 0)
						{
							if (slabBar2 == null)
							{
								throw new Exception("Selected " + text + " support type has no reinforcement in DXF.");
							}
							double num14 = (flag ? SlabCurtailment.TopContinuousBody(num7, num9, 0.0) : SlabCurtailment.TopPanelExtension(num7));
							countWidth = num8;
							supportMultiplier = num13;
							action(slabBar2, "Top over " + text + " " + (flag ? "CONT" : "END"), num14, num13 * Count(num8, slabBar2.SpacingMm, cover), true);
							SlabBar slabBar3 = row.Bars[8];
							if (slabBar3 != null)
							{
								countWidth = num14;
								action(slabBar3, "Top distribution " + text + " " + (flag ? "CONT" : "END"), num8, num13 * Count(num14, slabBar3.SpacingMm, cover), true);
							}
						}
					}
				}
			}
			result.Notes.Add("Span basis: entered shorter/longer sides; no automatic support-width deduction. All cuts include 2 straight Ld allowances; no bent-up bars or automatic end hooks.");
			result.Notes.Add("Continuous-support bars include both panel extensions. Count each shared support once; reduce support count in the neighboring entry when already included here.");
			result.Notes.Add("Distribution is additional top-strip reinforcement perpendicular to top main bars. No automatic corner/overlap deductions; verify against slab detailing.");
			return result;
		}
	}
}
