using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BeamScheduleFormatter
{
	internal static class RevisedSchedule
	{
		public static string Plain(string raw)
		{
			return Regex.Replace(Regex.Replace(raw.Replace("\\P", " ").Replace("\\U+2205", "%%C"), "\\\\[A-Za-z][^;]*;", "").Replace("{", "").Replace("}", ""), "\\s+", " ").Trim();
		}

		public static string Serialize(Dxf d)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (Entity record in d.Records)
			{
				foreach (Pair item in record.P)
				{
					stringBuilder.AppendLine(item.Code.ToString());
					stringBuilder.AppendLine(item.Value);
				}
			}
			return stringBuilder.ToString();
		}

		public static Analysis Read(string raw, string name)
		{
			List<Entity> source = Dxf.Parse(raw).ModelEntities();
			List<Entity> source2 = source.Where((Entity entity3) => entity3.Type == "MTEXT" || entity3.Type == "TEXT").ToList();
			List<Entity> marks = (from entity3 in source2
				where Regex.IsMatch(Plain(entity3.Get(1)), "^\\d*[A-Z]+-\\d{3,}$", RegexOptions.IgnoreCase)
				orderby entity3.Num(20) descending
				select entity3).ToList();
			if (marks.Count == 0)
			{
				throw new Exception("Load the REVISED DXF exported by Beam Formatter (marks such as RD-001), not the original schedule.");
			}
			if (marks.Select((Entity entity3) => Plain(entity3.Get(1)).ToUpperInvariant()).Distinct().Count() != marks.Count)
			{
				throw new Exception("Duplicate revised beam marks. Load one schedule only.");
			}
			List<Entity> headers = source2.Where((Entity entity3) => entity3.Num(20) > marks[0].Num(20) + 1.0).ToList();
			Func<string, Entity> func = delegate(string s)
			{
				List<Entity> list3 = headers.Where((Entity entity3) => Plain(entity3.Get(1)).Equals(s, StringComparison.OrdinalIgnoreCase)).ToList();
				if (list3.Count != 1)
				{
					throw new Exception("Missing/ambiguous revised schedule header: " + s);
				}
				return list3[0];
			};
			List<Entity> list = (from entity3 in headers
				where new string[3] { "LEFT", "MIDSPAN", "RIGHT" }.Contains(Plain(entity3.Get(1)).ToUpperInvariant())
				orderby entity3.Num(10)
				select entity3).ToList();
			if (list.Count != 6)
			{
				throw new Exception("Expected six reinforcement columns in revised DXF.");
			}
			Entity entity = headers.SingleOrDefault((Entity entity3) => new string[3] { "%%C", "Ø", "O" }.Contains(Plain(entity3.Get(1)).ToUpperInvariant()));
			if (entity == null)
			{
				throw new Exception("Missing stirrup diameter header.");
			}
			double[] xs = new double[2]
			{
				func("B").Num(10),
				func("D").Num(10)
			}.Concat(list.Select((Entity entity3) => entity3.Num(10))).Concat(new double[4]
			{
				func("LEG").Num(10),
				entity.Num(10),
				func("SPACING").Num(10),
				func("SFR").Num(10)
			}).ToArray();
			Entity title = headers.FirstOrDefault((Entity entity3) => Plain(entity3.Get(1)).StartsWith("SCHEDULE OF BEAMS"));
			Entity entity2 = ((title == null) ? null : (from entity3 in headers
				where entity3.Num(20) > title.Num(20)
				orderby entity3.Num(20)
				select entity3).FirstOrDefault());
			Analysis analysis = new Analysis();
			analysis.Path = name;
			analysis.Prefix = Plain(marks[0].Get(1)).Split('-')[0];
			analysis.Floor = ((entity2 == null) ? "" : Plain(entity2.Get(1)));
			analysis.BeamCount = marks.Count;
			Analysis analysis2 = analysis;
			List<double> source3 = (from entity3 in source
				where entity3.Type == "LWPOLYLINE"
				where (from p in entity3.P
					where p.Code == 20
					select p.Value).Distinct().Count() == 1 && entity3.P.Where((Pair p) => p.Code == 10).Any()
				where entity3.P.Where((Pair p) => p.Code == 10).Max((Pair p) => double.Parse(p.Value, CultureInfo.InvariantCulture)) - entity3.P.Where((Pair p) => p.Code == 10).Min((Pair p) => double.Parse(p.Value, CultureInfo.InvariantCulture)) > xs[11] - xs[0]
				select entity3.Num(20)).ToList();
			Entity refHeader = headers.FirstOrDefault((Entity entity3) => Plain(entity3.Get(1)) == "ORIGINAL BEAM NUMBERS");
			for (int num = 0; num < marks.Count; num++)
			{
				double y = marks[num].Num(20);
				double top = source3.Where((double v) => v > y + 0.001).DefaultIfEmpty(y + 4.0).Min();
				double bottom = source3.Where((double v) => v < y - 0.001).DefaultIfEmpty(y - 4.0).Max();
				if (top - bottom > 12.0 || top <= bottom)
				{
					throw new Exception("Cannot locate revised row boundaries for " + Plain(marks[num].Get(1)));
				}
				BeamRow beamRow = new BeamRow
				{
					Mark = Plain(marks[num].Get(1)).ToUpperInvariant()
				};
				beamRow.Numbers.Add(beamRow.Mark);
				beamRow.SourceRows.Add(num + 1);
				List<Entity> list2 = (from entity3 in source2
					where entity3.Num(20) < top - 0.001 && entity3.Num(20) > bottom + 0.001
					orderby entity3.Num(20) descending
					select entity3).ToList();
				foreach (Entity e in list2)
				{
					if (e.Num(10) < xs[0] - 1.0 || e.Num(10) > xs[11] + 2.0)
					{
						continue;
					}
					string text = Plain(e.Get(1));
					if (!(text == "+") && !(text == ""))
					{
						int num2 = (from c in Enumerable.Range(0, xs.Length)
							orderby Math.Abs(xs[c] - e.Num(10))
							select c).First();
						beamRow.Cells[num2].Add(text);
					}
				}
				for (int num3 = 0; num3 < 12; num3++)
				{
					if (beamRow.Cells[num3].Count == 0)
					{
						throw new Exception("Missing revised schedule cell " + num3 + " for " + beamRow.Mark);
					}
				}
				beamRow.Leg = beamRow.Cell(8);
				beamRow.Diameter = beamRow.Cell(9);
				beamRow.Spacing = (beamRow.RevisedSpacing = beamRow.Cell(10));
				beamRow.Cells[12].Add("-");
				beamRow.Cells[13].Add((beamRow.Spacing.IndexOf("TO FREE END", StringComparison.OrdinalIgnoreCase) >= 0) ? "CANTILEVERED" : "-");
				if (refHeader != null)
				{
					beamRow.RevisedReferences = string.Join(" ", from entity3 in list2
						where Math.Abs(entity3.Num(10) - refHeader.Num(10)) < 1.0
						select Plain(entity3.Get(1)));
					if (beamRow.RevisedReferences.Contains("DIAGONAL:"))
					{
						beamRow.Cells[12][0] = "DIAGONAL reinforcement in revised side notes; review separately";
					}
				}
				analysis2.Source.Add(beamRow);
			}
			return analysis2;
		}

		public static void AddTies(BeamEstimate result, BeamRow row, string beam, double length, EstimateOptions o, int? countOverride, double? cutOverride)
		{
			Match match = Regex.Match(row.Leg, "^(\\d+)L$");
			Match match2 = Regex.Match(Engine.Normal(row.Diameter).Replace("#", "O").Replace("Ø", "O"), "^O(\\d+)$");
			if (!match.Success || !match2.Success)
			{
				throw new Exception("Mixed/unknown stirrup legs or diameters in revised mark " + row.Mark + "; a project-specific tie detail is required.");
			}
			int num = int.Parse(match.Groups[1].Value);
			int num2 = int.Parse(match2.Groups[1].Value);
			if (num < 2 || num > 20)
			{
				throw new Exception("Invalid revised stirrup leg count.");
			}
			o.GradeFor(num2);
			string pattern = "^1\\s*-\\s*\\d+mm,\\s*(?:(?<n>\\d+)\\s*-\\s*(?<s>\\d+)mm,?\\s+)?AND REST AT\\s+(?<rest>\\d+)mm,?\\s+(?<end>C/C|TO FREE END)$";
			Match match3 = Regex.Match(row.RevisedSpacing, pattern, RegexOptions.IgnoreCase);
			double num3 = 0.0;
			double num4 = 0.0;
			double num5 = 0.0;
			int num6 = 0;
			int num7 = 0;
			if (match3.Success)
			{
				num5 = double.Parse(match3.Groups["rest"].Value) / 1000.0;
				if (match3.Groups["n"].Success)
				{
					num6 = (num7 = int.Parse(match3.Groups["n"].Value));
					num3 = (num4 = double.Parse(match3.Groups["s"].Value) / 1000.0);
				}
				if (match3.Groups["end"].Value.Equals("TO FREE END", StringComparison.OrdinalIgnoreCase))
				{
					num6 = (num7 = 0);
					result.Notes.Add("Cantilever: revised AND REST AT spacing applied toward free end.");
				}
			}
			else
			{
				match3 = Regex.Match(row.RevisedSpacing, "^1\\s*-\\s*\\d+mm,\\s*LEFT\\s+(?<ln>\\d+)\\s*-\\s*(?<ls>\\d+)mm\\s*/\\s*RIGHT\\s+(?<rn>\\d+)\\s*-\\s*(?<rs>\\d+)mm,?\\s+AND REST AT\\s+(?<rest>\\d+)mm,?\\s+C/C$", RegexOptions.IgnoreCase);
				if (!match3.Success)
				{
					throw new Exception("Unsupported revised spacing: " + row.RevisedSpacing);
				}
				num6 = int.Parse(match3.Groups["ln"].Value);
				num7 = int.Parse(match3.Groups["rn"].Value);
				num3 = double.Parse(match3.Groups["ls"].Value) / 1000.0;
				num4 = double.Parse(match3.Groups["rs"].Value) / 1000.0;
				num5 = double.Parse(match3.Groups["rest"].Value) / 1000.0;
			}
			if (num5 <= 0.0 || (num6 > 0 && num3 <= 0.0) || (num7 > 0 && num4 <= 0.0))
			{
				throw new Exception("Zero/invalid revised spacing.");
			}
			double num8 = length - 2.0 * o.Offset / 1000.0;
			if (num8 <= 0.0)
			{
				throw new Exception("Beam is shorter than first/last stirrup offsets.");
			}
			int num9;
			if (countOverride.HasValue)
			{
				num9 = countOverride.Value;
				result.Notes.Add("Manual stirrup set-count override per beam.");
			}
			else if (num6 == 0 && num7 == 0)
			{
				num9 = (int)Math.Ceiling(num8 / num5 - 1E-10) + 1;
			}
			else
			{
				double num10 = (double)num6 * num3 + (double)num7 * num4;
				double num11 = num8 - num10;
				if (num11 > 1E-08)
				{
					num9 = 2 + num6 + num7 + Math.Max(0, (int)Math.Ceiling(num11 / num5 - 1E-10) - 1);
				}
				else if (Math.Abs(num11) <= 1E-08)
				{
					num9 = 1 + num6 + num7;
				}
				else
				{
					double num12 = new double[3] { num3, num4, num5 }.Where((double s) => s > 0.0).Min();
					num9 = (int)Math.Ceiling(num8 / num12 - 1E-10) + 1;
					result.Notes.Add("Revised end zones overlap at entered length: smallest specified spacing used throughout; shared stations counted once.");
				}
			}
			double num13 = cutOverride ?? HookGeometry.TieCut(double.Parse(row.Cell(0), CultureInfo.InvariantCulture), double.Parse(row.Cell(1), CultureInfo.InvariantCulture), num2, num, o);
			double num14 = (double)num9 * num13;
			result.Ties = num9;
			result.Lines.Add(new EstimateLine
			{
				Beam = beam,
				Part = "Stirrups",
				Diameter = num2,
				Grade = o.GradeFor(num2),
				Metres = num14,
				Kg = num14 * EstimatorEngine.UnitMass(num2),
				Formula = num9 + " sets x " + num13.ToString("0.00", CultureInfo.InvariantCulture) + "m/set = " + num14.ToString("0.00", CultureInfo.InvariantCulture) + "m; " + num + "L"
			});
			TiePartsValue tiePartsValue = (cutOverride.HasValue ? null : HookGeometry.TieParts(double.Parse(row.Cell(0), CultureInfo.InvariantCulture), double.Parse(row.Cell(1), CultureInfo.InvariantCulture), num2, num, o));
			EstimateLine estimateLine = result.Lines[result.Lines.Count - 1];
			estimateLine.TieSetsPerBeam = num9;
			estimateLine.MainHoopCut = ((tiePartsValue == null) ? 0.0 : tiePartsValue.MainHoop);
			estimateLine.AdditionalLegCut = ((tiePartsValue == null) ? 0.0 : tiePartsValue.AdditionalLeg);
			estimateLine.AdditionalLegs = num - 2;
			estimateLine.CutLength = num13;
			estimateLine.TieOverride = cutOverride.HasValue;
			if (tiePartsValue != null)
			{
				EstimateLine estimateLine2 = estimateLine;
				estimateLine2.Formula = estimateLine2.Formula + "; MAIN HOOP=" + tiePartsValue.MainHoop.ToString("0.00") + "m + " + tiePartsValue.AdditionalCount + " additional legs x " + tiePartsValue.AdditionalLeg.ToString("0.00") + "m = " + num13.ToString("0.00") + "m/set";
			}
			result.Notes.Add("Stirrups follow REVISED DXF spacing: " + row.RevisedSpacing + ". End-offset setting=" + o.Offset + "mm each. N end ties means N intervals AFTER the first tie; middle stations fill the remaining gap without duplicate boundaries.");
			result.Notes.Add("Main bars use selected zone lengths and each layer's counts; EF side-face bars counted on both faces. Hook/anchorage allowances remain project inputs.");
			result.Notes.Add(cutOverride.HasValue ? "Manual total steel cut length per stirrup set." : "Standard shape model: outer hoop (3x90deg,2x135deg hooks) + (legs-2) depth crossties (135deg both ends), centerline bends and seismic tails. Verify against project detail.");
		}
	}
}
