using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace BeamScheduleFormatter
{
	internal class Engine
	{
		public const string Version = "1.4.37";

		private const string BeamToken = "\\d*[A-Z]+\\d+";

		private static Regex markPattern = new Regex("^\\d*[A-Z]+\\d+(?:,\\d*[A-Z]+\\d+)*$", RegexOptions.IgnoreCase);

		private static Regex numberPattern = new Regex("\\d*[A-Z]+\\d+", RegexOptions.IgnoreCase);

		public static string Normal(string s)
		{
			return Regex.Replace(s.ToUpperInvariant().Replace("%%C", "Ø").Replace("\\U+2205", "Ø")
				.Replace("--", "-"), "\\s+", "");
		}

		public static string Format(string s)
		{
			if (s == "-" || s == "--")
			{
				return "--";
			}
			s = s.Replace("#", "%%C").Replace("\\U+2205", "%%C").Replace("%%c", "%%C");
			s = Regex.Replace(s, "^(\\d+)-", "$1 - ");
			return Regex.Replace(s, "(%%C\\d+)(EF)$", "$1 EF");
		}

		public static bool ValidPrefix(string prefix)
		{
			if (!string.IsNullOrWhiteSpace(prefix) && prefix.Length <= 20)
			{
				return Regex.IsMatch(prefix, "^\\d*[A-Z]{1,12}$", RegexOptions.IgnoreCase);
			}
			return false;
		}

		public static Analysis Analyze(string path)
		{
			if (Path.GetExtension(path).ToLowerInvariant() != ".dxf")
			{
				throw new Exception("DXF ang kailangan. Sa AutoCAD, gamitin ang Save As > DXF para sa DWG file.");
			}
			Dxf dxf = Dxf.Read(path);
			List<Entity> list = (from e in dxf.ModelEntities()
				where e.Type == "TEXT" || e.Type == "MTEXT"
				select e.Copy(true)).ToList();
			if (list.Any((Entity e) => e.Get(1).Trim().ToUpperInvariant() == "ORIGINAL BEAM NUMBERS"))
			{
				throw new Exception("Formatted output DXF ito. Piliin ang original source schedule para hindi mabago ang original beam references.");
			}
			if (list.Count == 0)
			{
				throw new Exception("Walang readable text sa model space. Explode ang table/block sa isang COPY ng drawing, saka i-save bilang DXF.");
			}
			List<Entity> list2 = (from e in list
				where markPattern.IsMatch(e.Get(1).Replace(" ", ""))
				orderby e.Num(20) descending, e.Num(10)
				select e).ToList();
			if (list2.Count == 0)
			{
				throw new Exception("Hindi makita ang original beam marks (hal. B1, 2B1, 3B1, 4B1, RD1 o URFB1). Piliin ang original schedule DXF, hindi ang na-format na output.");
			}
			double markX = list2.Min((Entity e) => e.Num(10));
			list2 = list2.Where((Entity e) => Math.Abs(e.Num(10) - markX) < Math.Max(50.0, Math.Abs(markX) * 0.001)).ToList();
			if (list2.Count > 2000)
			{
				throw new Exception("Masyadong maraming rows. Maximum 2,000 rows bawat file.");
			}
			double highest = list2.Max((Entity e) => e.Num(20));
			List<Entity> source = list.Where((Entity e) => e.Num(20) > highest).ToList();
			Entity entity = (from e in source
				where e.Get(1).Trim().ToUpperInvariant() == "B"
				orderby e.Num(20) descending
				select e).FirstOrDefault();
			Entity entity2 = (from e in source
				where e.Get(1).Trim().ToUpperInvariant() == "D"
				orderby e.Num(20) descending
				select e).FirstOrDefault();
			List<Entity> list3 = (from e in source
				where new string[4] { "LEFT", "MID SPAN", "MIDSPAN", "RIGHT" }.Contains(e.Get(1).Trim().ToUpperInvariant())
				orderby e.Num(10)
				select e).ToList();
			if (entity == null || entity2 == null || list3.Count != 9)
			{
				throw new Exception("Hindi tugma ang source table. Kailangan ang B/D, 3 bottom, 3 top, at 3 shear columns (LEFT / MID SPAN / RIGHT), gaya ng original 4B schedule.");
			}
			Entity entity3 = source.FirstOrDefault((Entity e) => e.Get(1).Trim().ToUpperInvariant() == "SFR");
			Entity entity4 = source.FirstOrDefault((Entity e) => e.Get(1).Trim().ToUpperInvariant() == "DIAGONAL");
			Entity entity5 = source.FirstOrDefault((Entity e) => e.Get(1).Trim().ToUpperInvariant() == "REMARKS");
			if (entity3 == null)
			{
				throw new Exception("Hindi makita ang SFR column sa source table.");
			}
			List<double> centers = new List<double>
			{
				entity.Num(10),
				entity2.Num(10)
			};
			centers.AddRange(list3.Select((Entity e) => e.Num(10)));
			centers.Add(entity3.Num(10));
			centers.Add((entity4 != null) ? entity4.Num(10) : (entity3.Num(10) + (entity3.Num(10) - list3.Last().Num(10))));
			centers.Add((entity5 != null) ? entity5.Num(10) : (centers.Last() + (centers.Last() - entity3.Num(10))));
			if (!centers.SequenceEqual(centers.OrderBy((double v) => v)))
			{
				throw new Exception("Hindi inaasahan ang order ng table columns. I-check muna ang source DXF.");
			}
			double num = ((list2.Count > 1) ? (list2[0].Num(20) - list2[1].Num(20)) : 1120.0);
			List<double> list4 = (from e in dxf.ModelEntities()
				where e.Type == "LINE" && Math.Abs(e.Num(20) - e.Num(21)) < 0.01 && Math.Abs(e.Num(10) - e.Num(11)) > (centers[11] - centers[0]) * 0.8 && e.Num(20) > highest
				select e.Num(20) into v
				orderby v
				select v).ToList();
			double num2 = ((list4.Count > 0) ? (list4[0] - highest) : (num * 0.424107142857));
			if (num2 <= 0.0 || num2 > num)
			{
				num2 = num * 0.424107142857;
			}
			double num3 = (markX + centers[0]) / 2.0;
			Analysis analysis = new Analysis
			{
				Path = path
			};
			analysis.Floor = FloorFor(analysis.Prefix = Regex.Match(list2[0].Get(1).Replace(" ", ""), "^\\d*[A-Z]+", RegexOptions.IgnoreCase).Value.ToUpperInvariant());
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			for (int num4 = 0; num4 < list2.Count; num4++)
			{
				double num5 = list2[num4].Num(20);
				double top = num5 + num2;
				double bottom = ((num4 + 1 < list2.Count) ? (list2[num4 + 1].Num(20) + num2) : double.MinValue);
				List<Entity> list5 = (from e in list
					where e.Num(20) < top - 0.001 && e.Num(20) > bottom + 0.001
					orderby e.Num(20) descending
					select e).ToList();
				BeamRow beamRow = new BeamRow
				{
					SourceRows = { num4 + 1 }
				};
				foreach (Entity item in list5)
				{
					string text = item.Get(1).Trim();
					if (item.Num(10) < num3)
					{
						if (text.Length == 0)
						{
							continue;
						}
						if (!Regex.IsMatch(text.Replace(" ", ""), "^,?\\d*[A-Z]+\\d+(?:,\\d*[A-Z]+\\d+)*,?$", RegexOptions.IgnoreCase))
						{
							throw new Exception("May hindi inaasahang text sa beam-mark column: " + text + ". Isang schedule lang dapat ang nasa source.");
						}
						foreach (Match item2 in numberPattern.Matches(text))
						{
							if (!hashSet.Add(item2.Value))
							{
								throw new Exception("Paulit-ulit na original beam number: " + item2.Value + ". I-check ang source drawing.");
							}
							beamRow.Numbers.Add(item2.Value.ToUpperInvariant());
						}
						continue;
					}
					int num6 = 0;
					double num7 = double.MaxValue;
					for (int num8 = 0; num8 < centers.Count; num8++)
					{
						double num9 = Math.Abs(item.Num(10) - centers[num8]);
						if (num9 < num7)
						{
							num7 = num9;
							num6 = num8;
						}
					}
					if (text != "+")
					{
						beamRow.Cells[num6].Add(text);
					}
				}
				if (beamRow.Numbers.Count == 0)
				{
					throw new Exception("Walang beam number sa row " + (num4 + 1));
				}
				for (int num10 = 0; num10 < 8; num10++)
				{
					if (beamRow.Cells[num10].Count == 0)
					{
						throw new Exception("May kulang na size/reinforcement sa row " + (num4 + 1) + " (" + string.Join(",", beamRow.Numbers) + ").");
					}
				}
				if (beamRow.Cells.Skip(2).Take(6).Any((List<string> v) => v.Count > 3))
				{
					throw new Exception("Higit sa tatlong reinforcement layers sa row " + (num4 + 1) + ". Hindi ito suportado; i-check ang source bago mag-export.");
				}
				beamRow.OriginalShear.Add(new string[3]
				{
					beamRow.Cell(8),
					beamRow.Cell(9),
					beamRow.Cell(10)
				});
				ApplySpacing(beamRow);
				analysis.Source.Add(beamRow);
			}
			analysis.BeamCount = hashSet.Count;
			return analysis;
		}

		public static string FloorFor(string prefix)
		{
			prefix = (prefix ?? "").Trim().ToUpperInvariant();
			switch (prefix)
			{
			case "B":
			case "GB":
				return "GROUND FLOOR";
			case "RD":
				return "ROOF DECK";
			case "RFB":
			case "RF":
				return "ROOF FLOOR";
			case "URFB":
			case "URF":
				return "UPPER ROOF FLOOR";
			default:
			{
				int result;
				if (!int.TryParse(Regex.Match(prefix, "\\d+").Value, out result))
				{
					return "FLOOR";
				}
				string[] array = new string[11]
				{
					"", "FIRST FLOOR", "SECOND FLOOR", "THIRD FLOOR", "FOURTH FLOOR", "FIFTH FLOOR", "SIXTH FLOOR", "SEVENTH FLOOR", "EIGHTH FLOOR", "NINTH FLOOR",
					"TENTH FLOOR"
				};
				if (result >= array.Length)
				{
					return "LEVEL " + result;
				}
				return array[result];
			}
			}
		}

		public static void ApplySpacing(BeamRow r)
		{
			List<string[]> list = new List<string[]>();
			for (int i = 8; i <= 10; i++)
			{
				string text = r.Cell(i);
				Match match = Regex.Match(text, "^(\\d+)-(\\d+)L-(?:#|%%[Cc]|Ø|∅)(\\d+)@(\\d+)\\s*C/C$", RegexOptions.IgnoreCase);
				if (match.Success)
				{
					list.Add(new string[4]
					{
						match.Groups[1].Value,
						match.Groups[2].Value,
						match.Groups[3].Value,
						match.Groups[4].Value
					});
				}
				else
				{
					if (!(text == "-") && !(text == "--"))
					{
						throw new Exception("Hindi mabasa ang stirrup notation: " + text + ". Inaasahan: 17-4L-#10@100 C/C.");
					}
					list.Add(null);
				}
			}
			if (list.All((string[] t) => t == null))
			{
				throw new Exception("Walang stirrup data sa row " + r.SourceRows[0]);
			}
			r.Leg = string.Join("/", (from t in list
				where t != null
				select t[1] + "L").Distinct());
			r.Diameter = string.Join("/", (from t in list
				where t != null
				select "%%C" + t[2]).Distinct());
			r.Notes.Clear();
			string text2 = string.Join(", ", r.Numbers) + ": ";
			bool flag = list[0] != null && list[2] != null;
			string text3 = (flag ? Math.Min(int.Parse(list[0][3]), int.Parse(list[2][3])).ToString() : null);
			bool flag2 = flag && list[0][3] != list[2][3];
			string text4 = null;
			if (flag2)
			{
				int index = ((int.Parse(list[0][3]) >= int.Parse(list[2][3])) ? 2 : 0);
				text4 = list[index][0];
				r.Notes.Add(text2 + "LEFT " + list[0][0] + "pcs @ " + list[0][3] + "mm / RIGHT " + list[2][0] + "pcs @ " + list[2][3] + "mm; USED " + text4 + "pcs @ " + text3 + "mm " + (r.Cantilever ? "TO FREE END" : "AT BOTH ENDS") + " (SMALLER SPACING + ITS COUNT).");
				list[0][3] = text3;
				list[2][3] = text3;
				list[0][0] = text4;
				list[2][0] = text4;
			}
			if (list[1] == null)
			{
				r.Notes.Add(text2 + (flag ? ("MID BLANK IN SOURCE; USED " + text3 + "mm (SMALLER LEFT/RIGHT SPACING) FOR AND REST AT.") : "MID BLANK IN SOURCE; END DATA INCOMPLETE."));
			}
			bool flag3 = list.All((string[] t) => t != null);
			bool flag4 = flag3 && list.Select((string[] t) => t[3]).Distinct().Count() == 1;
			if (flag2)
			{
				r.Spacing = "1 - 50mm, " + text4 + " - " + text3 + "mm, AND REST AT " + ((r.Cantilever || list[1] == null) ? text3 : list[1][3]) + "mm " + (r.Cantilever ? "TO FREE END" : "C/C");
			}
			else if (r.Cantilever && flag)
			{
				r.Spacing = "1 - 50mm, AND REST AT " + text3 + "mm TO FREE END";
			}
			else if (flag4)
			{
				r.Spacing = "1 - 50mm, AND REST AT " + list[0][3] + "mm C/C";
			}
			else if (flag3 && list[0].SequenceEqual(list[2]))
			{
				r.Spacing = "1 - 50mm, " + list[0][0] + " - " + list[0][3] + "mm, AND REST AT " + list[1][3] + "mm C/C";
			}
			else if (flag3 && flag)
			{
				r.Spacing = "1 - 50mm, LEFT " + list[0][0] + " - " + list[0][3] + "mm / RIGHT " + list[2][0] + " - " + list[2][3] + "mm, AND REST AT " + list[1][3] + "mm C/C";
			}
			else if (flag && list[1] == null)
			{
				r.Spacing = "1 - 50mm, AND REST AT " + text3 + "mm C/C";
			}
			else
			{
				List<string> list2 = new List<string>();
				string[] array = new string[3] { "L", "MID", "R" };
				for (int num = 0; num < 3; num++)
				{
					list2.Add(array[num] + ": " + ((list[num] == null) ? "--" : (list[num][0] + " - " + list[num][3] + "mm")));
				}
				r.Spacing = string.Join("; ", list2) + " C/C";
			}
		}

		public static List<BeamRow> Consolidate(Analysis a, Settings s)
		{
			if (!ValidPrefix(s.Prefix))
			{
				throw new Exception("Ang prefix ay dapat gaya ng B, GB, 2B, 3B, 4B, RD o URFB; walang spaces o symbols.");
			}
			if (string.IsNullOrWhiteSpace(s.Floor) || s.Floor.Length > 60 || s.Floor.IndexOfAny(new char[5] { '{', '}', '\\', '\r', '\n' }) >= 0)
			{
				throw new Exception("Ilagay ang floor title, hal. FOURTH FLOOR.");
			}
			List<BeamRow> list = new List<BeamRow>();
			Dictionary<string, BeamRow> dictionary = new Dictionary<string, BeamRow>();
			foreach (BeamRow item in a.Source)
			{
				BeamRow beamRow = item.Copy();
				ApplySpacing(beamRow);
				string key = beamRow.Signature();
				BeamRow value;
				if (s.Merge && dictionary.TryGetValue(key, out value))
				{
					value.Numbers.AddRange(beamRow.Numbers);
					value.SourceRows.AddRange(beamRow.SourceRows);
					value.OriginalShear.AddRange(beamRow.OriginalShear);
					value.Notes.AddRange(beamRow.Notes);
				}
				else
				{
					list.Add(beamRow);
					if (!dictionary.ContainsKey(key))
					{
						dictionary.Add(key, beamRow);
					}
				}
			}
			for (int i = 0; i < list.Count; i++)
			{
				list[i].Mark = s.Prefix.ToUpperInvariant() + "-" + (i + 1).ToString("000");
			}
			return list;
		}

		public static Dxf Standard()
		{
			using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("StandardTemplate.dxf"))
			{
				using (StreamReader streamReader = new StreamReader(stream, Encoding.UTF8, true))
				{
					return Dxf.Parse(streamReader.ReadToEnd());
				}
			}
		}

		public static double RowHeight(BeamRow row)
		{
			return 8.0;
		}

		private static double LayerCenterY(Entity text)
		{
			int num = (int)text.Num(71);
			return text.Num(20) + ((num >= 7) ? (text.Num(40) / 2.0) : ((num <= 3) ? ((0.0 - text.Num(40)) / 2.0) : 0.0));
		}

		public static Result Build(Analysis a, Settings s)
		{
			List<BeamRow> list = Consolidate(a, s);
			Dxf dxf = Standard();
			List<Entity> list2 = dxf.ModelEntities();
			TemplateLayout layout = new TemplateLayout(dxf);
			long next = (from p in dxf.Records.SelectMany((Entity e) => e.P)
				where p.Code == 5
				select p).Max((Pair p) => long.Parse(p.Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)) + 1;
			double num = layout.Top - (double)list.Count * layout.Height;
			List<Entity> output = new List<Entity>();
			foreach (Entity item in list2)
			{
				if ((item.Type == "MTEXT" && item.Num(20) < layout.Top) || layout.Rules.Contains(item))
				{
					continue;
				}
				Entity entity = item.Copy();
				if (item == layout.Title)
				{
					entity.Set(1, Regex.Replace(entity.Get(1), "\\([^)]*\\)", "(" + s.Floor.ToUpperInvariant() + ")"));
				}
				if (item == layout.FloorTitle)
				{
					string text = entity.Get(1);
					int num2 = text.LastIndexOf("\\P", StringComparison.Ordinal);
					num2 = ((num2 >= 0) ? (num2 + 2) : (text.LastIndexOf(';') + 1));
					entity.Set(1, text.Substring(0, num2) + s.Floor.Trim().ToUpperInvariant() + (text.EndsWith("}") ? "}" : ""));
				}
				bool flag = entity.Type == "HATCH" && entity.P.Any((Pair p) => (p.Code == 20 || p.Code == 21) && Math.Abs(double.Parse(p.Value, CultureInfo.InvariantCulture) - layout.Bottom) < 1E-05);
				bool flag2 = false;
				foreach (Pair item2 in entity.P)
				{
					if (item2.Code == 98)
					{
						flag2 = true;
					}
					else if (item2.Code == 20 || item2.Code == 21)
					{
						double num3 = double.Parse(item2.Value, CultureInfo.InvariantCulture);
						if (flag && flag2 && item2.Code == 20)
						{
							item2.Value = ((layout.Top + num) / 2.0).ToString("R", CultureInfo.InvariantCulture);
						}
						else if (Math.Abs(num3 - layout.Bottom) < 1E-05)
						{
							item2.Value = num.ToString("R", CultureInfo.InvariantCulture);
						}
						else if (Math.Abs(num3 - (layout.Bottom - 2.0)) < 1E-05)
						{
							item2.Value = (num - 2.0).ToString("R", CultureInfo.InvariantCulture);
						}
					}
				}
				output.Add(entity);
			}
			Func<Entity, string, double?, double, Entity> text2 = delegate(Entity prototype, string value, double? x, double y)
			{
				Entity entity11 = prototype.Copy(true);
				entity11.Set(5, next++.ToString("X"));
				entity11.Set(1, value);
				if (x.HasValue)
				{
					entity11.Set(10, x.Value);
				}
				entity11.Set(20, y);
				return entity11;
			};
			Action<Entity, string, double> action = delegate(Entity prototype, string value, double center)
			{
				output.Add(text2(prototype, value, null, center + prototype.Num(20) - layout.Center));
			};
			for (int num4 = 0; num4 < list.Count; num4++)
			{
				BeamRow beamRow = list[num4];
				double num5 = (double)(-num4) * layout.Height;
				double num6 = layout.Center + num5;
				action(layout.Mark, beamRow.Mark, num6);
				action(layout.Width, beamRow.Cell(0), num6);
				action(layout.Depth, beamRow.Cell(1), num6);
				bool flag3 = beamRow.Cells.Skip(2).Take(6).Any((List<string> c) => c.Count == 3);
				for (int num7 = 0; num7 < 6; num7++)
				{
					List<string> list3 = beamRow.Cells[num7 + 2];
					if (list3.Count < 1 || list3.Count > 3)
					{
						throw new Exception("Unsupported reinforcement layers: " + beamRow.Mark);
					}
					double num8 = layout.Bars[num7].Num(10);
					double arg = (flag3 ? (num6 + 1.3) : (layout.Bars[num7].Num(20) + num5));
					Entity entity2 = text2(layout.Bars[num7], Format(list3[0]), null, arg);
					if (flag3)
					{
						entity2.Set(40, 1.5);
					}
					output.Add(entity2);
					Entity text3 = entity2;
					for (int num9 = 1; num9 < list3.Count; num9++)
					{
						double arg2 = (flag3 ? (num6 + ((num9 == 1) ? 1.1 : (-1.5))) : (layout.Lower.Num(20) + num5));
						Entity entity3 = text2(layout.Lower, Format(list3[num9]), num8 + layout.LowerOffset, arg2);
						if (flag3)
						{
							entity3.Set(40, 1.5);
						}
						double arg3 = (LayerCenterY(text3) + LayerCenterY(entity3)) / 2.0;
						Entity entity4 = text2(layout.Plus, "+", num8 + layout.PlusOffset, arg3);
						entity4.Set(71, "5");
						entity4.Set(40, entity3.Num(40));
						output.Add(entity3);
						output.Add(entity4);
						text3 = entity3;
					}
				}
				action(layout.Leg, layout.LegText(beamRow.Leg), num6);
				action(layout.Diameter, beamRow.Diameter, num6);
				action(layout.Spacing, beamRow.Spacing, num6);
				action(layout.Sfr, Format(beamRow.Cell(11)), num6);
				string text4 = string.Join(", ", beamRow.Numbers);
				List<string> list4 = new List<string>();
				if (beamRow.Cell(12) != "-" && beamRow.Cell(12) != "--")
				{
					list4.Add("DIAGONAL: " + Format(beamRow.Cell(12)));
				}
				if (beamRow.Cell(13) != "-" && beamRow.Cell(13) != "--")
				{
					list4.Add("REMARKS: " + beamRow.Cell(13));
				}
				if (list4.Count > 0)
				{
					text4 = text4 + "\\P" + string.Join("; ", list4);
				}
				Entity entity5 = text2(layout.Width, text4, layout.Right + 5.0, num6);
				entity5.Set(41, 130.0);
				entity5.Set(71, "4");
				output.Add(entity5);
				if (beamRow.Notes.Count > 0)
				{
					Entity entity6 = text2(layout.Width, string.Join("\\P", beamRow.Notes.Distinct()), layout.Right + 145.0, num6);
					entity6.Set(41, 190.0);
					entity6.Set(71, "4");
					output.Add(entity6);
				}
				Entity entity7 = layout.Rules[Math.Min(num4, layout.Rules.Count - 1)].Copy(true);
				entity7.Set(5, next++.ToString("X"));
				foreach (Pair item3 in entity7.P)
				{
					if (item3.Code == 20)
					{
						item3.Value = (layout.Top - (double)(num4 + 1) * layout.Height).ToString("R", CultureInfo.InvariantCulture);
					}
				}
				output.Add(entity7);
			}
			Entity entity8 = list2.Single((Entity e) => e.Type == "MTEXT" && e.Get(1) == "BEAM MARK");
			Entity entity9 = text2(entity8, "ORIGINAL BEAM NUMBERS", layout.Right + 5.0, entity8.Num(20));
			entity9.Set(41, 130.0);
			entity9.Set(71, "4");
			output.Add(entity9);
			if (list.Any((BeamRow r) => r.Notes.Count > 0))
			{
				Entity entity10 = text2(entity8, "SPACING ADJUSTMENT NOTES", layout.Right + 145.0, entity8.Num(20));
				entity10.Set(41, 190.0);
				entity10.Set(71, "4");
				output.Add(entity10);
			}
			foreach (Entity item4 in output.Where((Entity e) => (e.Type == "MTEXT" || e.Type == "TEXT") && e.Num(20) < layout.Top))
			{
				item4.Set(7, layout.Width.Get(7));
				item4.Set(1, Regex.Replace(item4.Get(1), "\\|b1(?=\\||;)", "|b0", RegexOptions.IgnoreCase));
			}
			int num10 = dxf.Records.FindIndex((Entity e) => e.Type == "SECTION" && e.Get(2) == "ENTITIES");
			int num11;
			for (num11 = num10 + 1; dxf.Records[num11].Type != "ENDSEC"; num11++)
			{
			}
			dxf.Records.RemoveRange(num10 + 1, num11 - num10 - 1);
			dxf.Records.InsertRange(num10 + 1, output);
			foreach (Entity record in dxf.Records)
			{
				for (int num12 = 0; num12 < record.P.Count; num12++)
				{
					Pair pair = record.P[num12];
					if (pair.Code == 9 && pair.Value == "$HANDSEED")
					{
						record.P[num12 + 1].Value = next.ToString("X");
					}
					if (pair.Code != 9 || (!(pair.Value == "$EXTMIN") && !(pair.Value == "$EXTMAX")))
					{
						continue;
					}
					for (int num13 = num12 + 1; num13 < record.P.Count && record.P[num13].Code != 9; num13++)
					{
						if (pair.Value == "$EXTMIN" && record.P[num13].Code == 20)
						{
							record.P[num13].Value = (num - 2.0).ToString("R", CultureInfo.InvariantCulture);
						}
						if (pair.Value == "$EXTMAX" && record.P[num13].Code == 10)
						{
							record.P[num13].Value = (layout.Right + (double)(list.Any((BeamRow r) => r.Notes.Count > 0) ? 340 : 140)).ToString("R", CultureInfo.InvariantCulture);
						}
					}
				}
			}
			List<string> list5 = (from e in dxf.Records
				where e.Get(5) != ""
				select e.Get(5)).ToList();
			if (list5.Distinct().Count() != list5.Count)
			{
				throw new Exception("DXF validation: duplicate handles.");
			}
			if (list.Sum((BeamRow r) => r.Numbers.Count) != a.BeamCount)
			{
				throw new Exception("DXF validation: missing original beam references.");
			}
			Result result = new Result
			{
				Rows = list,
				Drawing = dxf,
				SourceCount = a.Source.Count,
				BeamCount = a.BeamCount
			};
			foreach (BeamRow item5 in list.Where((BeamRow r) => r.SourceRows.Count > 1))
			{
				result.Changes.Add("Rows " + string.Join(", ", item5.SourceRows) + " -> " + item5.Mark + " (" + string.Join(", ", item5.Numbers) + ")");
			}
			return result;
		}

		private static string Csv(string s)
		{
			return "\"" + s.Replace("\"", "\"\"") + "\"";
		}

		public static string Mapping(Result r)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("New mark,Original source rows,Original beam numbers,Original left stirrups,Original mid stirrups,Original right stirrups,Final spacing,Adjustment notes");
			foreach (BeamRow row in r.Rows)
			{
				for (int i = 0; i < row.SourceRows.Count; i++)
				{
					string[] array = row.OriginalShear[i];
					stringBuilder.AppendLine(string.Join(",", new string[8]
					{
						row.Mark,
						row.SourceRows[i].ToString(),
						string.Join(", ", row.Numbers),
						array[0],
						array[1],
						array[2],
						row.Spacing,
						string.Join("; ", row.Notes)
					}.Select(Csv)));
				}
			}
			return stringBuilder.ToString();
		}

		public static void Export(Result r, string path)
		{
			string text = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path)), "." + Guid.NewGuid().ToString("N") + ".dxf");
			try
			{
				r.Drawing.Write(text);
				if (Dxf.Read(text).ModelEntities().Count((Entity e) => e.Type == "MTEXT" && Regex.IsMatch(e.Get(1), "^\\d*[A-Z]+-\\d{3,}$")) != r.Rows.Count)
				{
					throw new Exception("Hindi pumasa sa export verification ang DXF.");
				}
				File.Copy(text, path, true);
			}
			finally
			{
				if (File.Exists(text))
				{
					File.Delete(text);
				}
			}
		}
	}
}
