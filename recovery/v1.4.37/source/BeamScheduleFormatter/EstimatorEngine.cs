using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace BeamScheduleFormatter
{
	internal static class EstimatorEngine
	{
		private class Tie
		{
			public int N;

			public int Leg;

			public int Dia;

			public double Spacing;
		}

		public static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

		public static string DisplayPart(string part)
		{
			if (part.EndsWith(" CONTINUOUS"))
			{
				return part.Replace(" CONTINUOUS", " STRAIGHT / CONTINUOUS");
			}
			switch (part)
			{
			case "Bottom CUT MIDSPAN":
				return "Bottom CURTAILED MIDSPAN";
			case "Bottom CUT LEFT":
			case "Bottom CUT RIGHT":
				return part.Replace("CUT", "END EXTRA");
			default:
				if (part.StartsWith("Top CUT "))
				{
					return part.Replace("Top CUT ", "Top EXTRA ");
				}
				return part;
			}
		}

		public static double UnitMass(int diameter)
		{
			switch (diameter)
			{
			case 10:
				return 0.617;
			case 12:
				return 0.888;
			case 16:
				return 1.578;
			case 20:
				return 2.466;
			case 25:
				return 3.853;
			case 28:
				return 4.834;
			case 32:
				return 6.313;
			case 36:
				return 7.99;
			case 40:
				return 9.865;
			case 50:
				return 15.41;
			default:
				throw new Exception("No configured nominal rebar mass for " + diameter + "mm.");
			}
		}

		public static Dictionary<int, int> Bars(IEnumerable<string> raw, bool eachFace = false)
		{
			Dictionary<int, int> dictionary = new Dictionary<int, int>();
			foreach (string item in raw)
			{
				string text = Engine.Normal(item).Replace("#", "O").Replace("Ø", "O");
				switch (text)
				{
				case "-":
				case "":
				case "+":
					continue;
				}
				Match match = Regex.Match(text, "^(\\d+)-O(\\d+)(EF)?$");
				if (!match.Success)
				{
					throw new Exception("Unsupported bar notation: " + item);
				}
				int num = int.Parse(match.Groups[2].Value);
				int num2 = int.Parse(match.Groups[1].Value) * ((!match.Groups[3].Success) ? 1 : 2);
				if (num <= 0 || num2 <= 0)
				{
					throw new Exception("Invalid bar size/count: " + item);
				}
				if (!dictionary.ContainsKey(num))
				{
					dictionary[num] = 0;
				}
				dictionary[num] += num2;
			}
			return dictionary;
		}

		private static Tie ParseTie(string raw)
		{
			if (raw == "-" || raw == "--" || string.IsNullOrWhiteSpace(raw))
			{
				return null;
			}
			Match match = Regex.Match(Engine.Normal(raw).Replace("#", "O").Replace("Ø", "O"), "^(\\d+)-(\\d+)L-O(\\d+)@(\\d+)C/C$");
			if (!match.Success)
			{
				throw new Exception("Unsupported stirrup: " + raw);
			}
			Tie tie = new Tie
			{
				N = int.Parse(match.Groups[1].Value),
				Leg = int.Parse(match.Groups[2].Value),
				Dia = int.Parse(match.Groups[3].Value),
				Spacing = double.Parse(match.Groups[4].Value, Inv) / 1000.0
			};
			if (tie.Leg < 2 || tie.Leg > 20 || tie.Dia <= 0 || tie.Spacing <= 0.0)
			{
				throw new Exception("Invalid stirrup geometry: " + raw);
			}
			return tie;
		}

		private static string F(double n)
		{
			return n.ToString("0.00", Inv);
		}

		private static void Add(BeamEstimate r, string beam, string part, int dia, double metres, string formula, EstimateOptions o)
		{
			if (double.IsNaN(metres) || double.IsInfinity(metres) || metres < 0.0)
			{
				throw new Exception("Invalid calculated length.");
			}
			o.GradeFor(dia);
			r.Lines.Add(new EstimateLine
			{
				Beam = beam,
				Part = part,
				Diameter = dia,
				Grade = o.GradeFor(dia),
				Metres = metres,
				Kg = metres * UnitMass(dia),
				Formula = formula
			});
		}

		private static void AddMainPiece(BeamEstimate result, string beam, string face, int dia, int pieces, int start, int end, double length, double[] zones, EstimateOptions o)
		{
			if (pieces > 0)
			{
				bool flag = face == "Top";
				LengthConfiguration lengthConfig = o.LengthConfig;
				LengthResult lengthResult = ((lengthConfig == null) ? null : lengthConfig.Values(dia, flag));
				double num = Enumerable.Range(start, end - start + 1).Sum((int z) => zones[z]);
				if (o.FixedCurtailment && (start != 0 || end != 2))
				{
					double[] array = ((!flag) ? new double[3]
					{
						0.0,
						1.0 / 6.0,
						0.75
					} : new double[3] { 0.0, 0.25, 0.75 });
					num = (((!flag) ? new double[3]
					{
						0.25,
						5.0 / 6.0,
						1.0
					} : new double[3] { 0.25, 0.75, 1.0 })[end] - array[start]) * length;
				}
				int num2 = ((start == 0) ? 1 : 0) + ((end == 2) ? 1 : 0);
				int num3 = 2 - num2;
				double num4 = ((o.MainHook == 0) ? 0.0 : ((lengthConfig == null) ? HookGeometry.MainEnd(dia, o.MainHook) : (lengthConfig.Rounded((o.MainHook == 90) ? lengthResult.Cut90 : lengthResult.Cut180) / 1000.0)));
				double num5 = (double)num2 * num4;
				double num6 = HookGeometry.Development(dia, flag, o);
				double num7 = 0.0;
				if (o.Anchorage == 1)
				{
					num7 = 2.0 * num6;
				}
				else if (o.Anchorage == 2)
				{
					num7 = ((lengthConfig == null) ? (1.3 * num6) : (lengthConfig.Rounded(lengthResult.Lap) / 1000.0));
				}
				else if (o.Anchorage == 3)
				{
					num7 = (double)num2 * Math.Max(0.0, lengthConfig.Rounded(lengthResult.Ldh) - (lengthResult.MainInside / 2.0 + (double)dia)) / 1000.0 + (double)num3 * num6;
				}
				double num8 = num + o.Extra + num5 + num7;
				string[] source = new string[3] { "LEFT", "MIDSPAN", "RIGHT" };
				string text = ((start == 0 && end == 2) ? "CONTINUOUS" : ("CUT " + string.Join("+", source.Skip(start).Take(end - start + 1))));
				Add(result, beam, face + " " + text, dia, (double)pieces * num8, pieces + " pcs x (" + F(num) + "m body + " + F(o.Extra) + "m extra + " + F(num5) + "m external hooks + " + F(num7) + "m anchorage/lap) = " + F((double)pieces * num8) + "m", o);
				EstimateLine estimateLine = result.Lines[result.Lines.Count - 1];
				estimateLine.PiecesPerBeam = pieces;
				estimateLine.CutLength = num8;
			}
		}

		private static void AddMainBars(BeamEstimate result, BeamRow row, string beam, double length, double[] zones, EstimateOptions o)
		{
			int face;
			for (face = 0; face < 2; face++)
			{
				Dictionary<int, int>[] source = (from z in Enumerable.Range(0, 3)
					select Bars(row.Cells[2 + face * 3 + z])).ToArray();
				foreach (int dia in from d in source.SelectMany((Dictionary<int, int> x) => x.Keys).Distinct()
					orderby d
					select d)
				{
					int[] array = source.Select((Dictionary<int, int> x) => x.ContainsKey(dia) ? x[dia] : 0).ToArray();
					int num = 0;
					foreach (int item in from n in array.Where((int n) => n > 0).Distinct()
						orderby n
						select n)
					{
						int pieces = item - num;
						int num2 = 0;
						while (num2 < 3)
						{
							if (array[num2] < item)
							{
								num2++;
								continue;
							}
							int start = num2;
							for (; num2 + 1 < 3 && array[num2 + 1] >= item; num2++)
							{
							}
							AddMainPiece(result, beam, (face == 0) ? "Bottom" : "Top", dia, pieces, start, num2, length, zones, o);
							num2++;
						}
						num = item;
					}
				}
			}
			result.Notes.Add("Continuous bars = minimum L/M/R count for the SAME diameter. Remaining counts are cut bars; adjacent occupied zones share a piece. No continuity is inferred between different diameters.");
			result.Notes.Add("Fixed cut bodies: top support L/4; bottom support L/4; bottom MIDSPAN cutoffs L/6 from each end (2L/3 body). Hooks apply only at beam ends; internal cut ends use straight Ld in hooked-anchorage mode. Confirm actual cutoffs in the structural details.");
		}

		public static BeamEstimate Calculate(BeamRow row, string beam, double length, EstimateOptions o, int? countOverride = null, double? cutOverride = null)
		{
			o.Validate();
			if (double.IsNaN(length) || double.IsInfinity(length) || length <= 0.0 || length > 1000.0)
			{
				throw new Exception("Enter beam length greater than 0 and at most 1000 m.");
			}
			double result;
			double result2;
			if (!double.TryParse(row.Cell(0), NumberStyles.Float, Inv, out result) || !double.TryParse(row.Cell(1), NumberStyles.Float, Inv, out result2) || result <= 0.0 || result2 <= 0.0)
			{
				throw new Exception("Beam size must be numeric millimetres.");
			}
			if (countOverride.HasValue && (countOverride <= 0 || countOverride > 100000))
			{
				throw new Exception("Tie count override must be 1 to 100000.");
			}
			if (cutOverride.HasValue && (cutOverride <= 0.0 || double.IsNaN(cutOverride.Value) || double.IsInfinity(cutOverride.Value)))
			{
				throw new Exception("Tie cut length must be greater than zero.");
			}
			BeamEstimate beamEstimate = new BeamEstimate();
			double[] zones = new double[3]
			{
				length * o.Left,
				length * (1.0 - o.Left - o.Right),
				length * o.Right
			};
			string[] array = new string[3] { "LEFT", "MIDSPAN", "RIGHT" };
			AddMainBars(beamEstimate, row, beam, length, zones, o);
			foreach (KeyValuePair<int, int> item in Bars(row.Cells[11], true))
			{
				double num = 0.0;
				double num2 = 0.0;
				if (o.LengthConfig != null)
				{
					LengthConfiguration lengthConfig = o.LengthConfig;
					LengthResult lengthResult = lengthConfig.Values(item.Key, false);
					num = ((o.MainHook == 0) ? 0.0 : (2.0 * lengthConfig.Rounded((o.MainHook == 90) ? lengthResult.Cut90 : lengthResult.Cut180) / 1000.0));
					num2 = ((o.Anchorage == 1) ? (2.0 * lengthConfig.Rounded(lengthResult.Ld) / 1000.0) : ((o.Anchorage == 2) ? (lengthConfig.Rounded(lengthResult.Lap) / 1000.0) : ((o.Anchorage == 3) ? (2.0 * Math.Max(0.0, lengthConfig.Rounded(lengthResult.Ldh) - (lengthResult.MainInside / 2.0 + (double)item.Key)) / 1000.0) : 0.0)));
				}
				Add(beamEstimate, beam, "Side-face", item.Key, (double)item.Value * (length + o.Extra + num + num2), item.Value + " bars (EF doubled) x (" + F(length) + "m span + " + F(o.Extra) + "m extra + " + F(num) + "m hooks + " + F(num2) + "m Ld/lap)m", o);
				EstimateLine estimateLine = beamEstimate.Lines[beamEstimate.Lines.Count - 1];
				estimateLine.PiecesPerBeam = item.Value;
				estimateLine.CutLength = length + o.Extra + num + num2;
			}
			int[] array2 = new int[1] { 12 };
			foreach (int num3 in array2)
			{
				if (row.Cells[num3].Any((string x) => Engine.Normal(x) != "-" && Engine.Normal(x) != ""))
				{
					throw new Exception("Unrecognised additional reinforcement: " + row.Cell(num3) + ". Review separately.");
				}
			}
			if (!string.IsNullOrEmpty(row.RevisedSpacing))
			{
				RevisedSchedule.AddTies(beamEstimate, row, beam, length, o, countOverride, cutOverride);
				return beamEstimate;
			}
			Tie[] array3 = (from c in Enumerable.Range(8, 3)
				select ParseTie(row.Cell(c))).ToArray();
			if (array3.All((Tie t) => t == null))
			{
				throw new Exception("No stirrup spacing found.");
			}
			if (array3[0] == null)
			{
				array3[0] = array3[1] ?? array3[2];
			}
			if (array3[2] == null)
			{
				array3[2] = array3[1] ?? array3[0];
			}
			if (array3[1] == null)
			{
				array3[1] = ((array3[0].Spacing <= array3[2].Spacing) ? array3[0] : array3[2]);
				beamEstimate.Notes.Add("MIDSPAN blank: smaller end spacing/shape used for estimate.");
			}
			if (row.Cantilever)
			{
				array3[1] = (array3[2] = array3[0]);
				beamEstimate.Notes.Add("Cantilever assumption: LEFT stirrup spacing/shape applied throughout. Main-bar zones still use chosen percentages.");
			}
			double num4 = o.Offset / 1000.0;
			if (length <= 2.0 * num4)
			{
				throw new Exception("Beam length is too short for first/last tie offsets.");
			}
			double[] array4 = new double[4]
			{
				num4,
				length * o.Left,
				length * (1.0 - o.Right),
				length - num4
			};
			if (array4[1] < array4[0] || array4[2] > array4[3])
			{
				throw new Exception("End zones shorter than tie offset; adjust percentages/offset.");
			}
			int[] array5 = new int[3];
			for (int num5 = 0; num5 < 3; num5++)
			{
				array5[num5] = (int)Math.Ceiling(Math.Max(0.0, array4[num5 + 1] - array4[num5]) / array3[num5].Spacing - 1E-10);
			}
			array5[2]++;
			if (countOverride.HasValue)
			{
				if (array3.Select((Tie t) => t.Leg + "/" + t.Dia).Distinct().Count() != 1)
				{
					throw new Exception("Tie count override requires identical leg/diameter in all zones.");
				}
				array5 = new int[3] { 0, countOverride.Value, 0 };
				beamEstimate.Notes.Add("Manual stirrup count override: " + countOverride.Value + " sets.");
			}
			for (int num6 = 0; num6 < 3; num6++)
			{
				if (array5[num6] > 0)
				{
					Tie tie = array3[num6];
					double num7 = (result - 2.0 * o.Cover - (double)tie.Dia) / 1000.0;
					double num8 = (result2 - 2.0 * o.Cover - (double)tie.Dia) / 1000.0;
					if (num7 <= 0.0 || num8 <= 0.0)
					{
						throw new Exception("Cover is too large for the beam/tie diameter.");
					}
					double num9 = cutOverride ?? HookGeometry.TieCut(result, result2, tie.Dia, tie.Leg, o);
					double num10 = (double)array5[num6] * num9;
					Add(beamEstimate, beam, "Stirrups", tie.Dia, num10, array[num6] + ": " + array5[num6] + " sets x " + F(num9) + "m/set = " + F(num10) + "m; " + tie.Leg + "L" + (cutOverride.HasValue ? " (manual cut)" : ("; w=" + F(num7) + "m,h=" + F(num8) + "m,r=" + F((HookGeometry.Inside(tie.Dia, true) + (double)tie.Dia) / 2000.0) + "m,tail=" + F(Math.Max(Math.Max(6.0, o.Hook) * (double)tie.Dia, 75.0) / 1000.0) + "m")), o);
					EstimateLine estimateLine2 = beamEstimate.Lines[beamEstimate.Lines.Count - 1];
					TiePartsValue tiePartsValue = (cutOverride.HasValue ? null : HookGeometry.TieParts(result, result2, tie.Dia, tie.Leg, o));
					estimateLine2.TieSetsPerBeam = array5[num6];
					estimateLine2.CutLength = num9;
					estimateLine2.TieOverride = cutOverride.HasValue;
					estimateLine2.MainHoopCut = ((tiePartsValue == null) ? 0.0 : tiePartsValue.MainHoop);
					estimateLine2.AdditionalLegCut = ((tiePartsValue == null) ? 0.0 : tiePartsValue.AdditionalLeg);
					estimateLine2.AdditionalLegs = tie.Leg - 2;
					beamEstimate.Ties += array5[num6];
				}
			}
			beamEstimate.Notes.Add("Main bars are decomposed per diameter into continuous and contiguous cut pieces. SFR EF is counted on both faces.");
			beamEstimate.Notes.Add("Stirrups recounted from entered length and source spacings; source piece counts are reference only. Shared zone boundaries counted once; end offsets included.");
			beamEstimate.Notes.Add(cutOverride.HasValue ? "Manual total steel length per stirrup set overrides standard shape calculation." : "Tie model: one outer hoop (three90deg corners, two135deg hooks) + (legs-2) depth crossties (90deg + custom130deg). Centerline arcs/tangent deductions included. Crosstie90 tail=max(6db,75mm) up to16mm,12db for20-25mm; custom130 tail=max(6db,75mm). Verify this shape matches project.");
			beamEstimate.Notes.Add("Hook geometry is the same for Grade 40/60; development and lap lengths are NOT hook tail lengths and must come from the project detail. Main hooks apply at exterior beam ends only.");
			beamEstimate.Notes.Add("Straight Ld reference: ACI 318M-14 Eq 25.4.2.3a, normal-weight uncoated bars, lambda=1, psi_e=1, psi_s=0.8 up to 19mm else 1; top psi_t=1.3 conservatively, others=1.0; minimum 300mm. User bond ratio must match actual cover/spacing/confinement. No excess-steel reduction.");
			beamEstimate.Notes.Add("Ld/lap allowance applies per actual main-bar piece. SFR uses full length and configured allowances. Verify splice locations, support embedment and continuity. Hooked development/seismic joint anchorage are not checked.");
			beamEstimate.Notes.Add("Commercial stock cutting is calculated separately from net reinforcement and percentage allowance. Support widths and chairs are not inferred.");
			return beamEstimate;
		}
	}
}
