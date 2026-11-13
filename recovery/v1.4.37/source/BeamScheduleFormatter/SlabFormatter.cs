using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BeamScheduleFormatter
{
	internal static class SlabFormatter
	{
		internal static string Plain(Entity e)
		{
			return Regex.Replace(string.Concat(from p in e.P
				where p.Code == 1 || p.Code == 3
				select p.Value), "\\\\[fFHWAQ][^;]*;", "").Replace("{", "").Replace("}", "")
				.Replace("\\P", " ")
				.Trim();
		}

		internal static string Signature(SlabScheduleRow row)
		{
			return row.Thickness.ToString("R", CultureInfo.InvariantCulture) + "|" + string.Join("|", row.Bars.Select((SlabBar b) => (b != null) ? (b.Diameter + "@" + b.SpacingMm.ToString("R", CultureInfo.InvariantCulture)) : "--"));
		}

		public static List<SlabFormatGroup> Group(IEnumerable<SlabScheduleRow> rows, string prefix, bool merge)
		{
			prefix = (prefix ?? "").Trim().ToUpperInvariant();
			if (!Regex.IsMatch(prefix, "^[A-Z0-9]*S$") || prefix.Length > 12)
			{
				throw new Exception("Enter slab prefix, e.g. 2S for Second Floor.");
			}
			List<SlabFormatGroup> list = new List<SlabFormatGroup>();
			Dictionary<string, SlabFormatGroup> dictionary = new Dictionary<string, SlabFormatGroup>();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (SlabScheduleRow row in rows)
			{
				if (!hashSet.Add(row.Mark))
				{
					throw new Exception("Duplicate slab mark: " + row.Mark);
				}
				string key = Signature(row);
				SlabFormatGroup value;
				if (!merge || !dictionary.TryGetValue(key, out value))
				{
					value = new SlabFormatGroup();
					list.Add(value);
					dictionary[key] = value;
				}
				value.Source.Add(row);
			}
			for (int i = 0; i < list.Count; i++)
			{
				list[i].Mark = prefix + "-" + (i + 1).ToString("000");
			}
			return list;
		}

		public static string BarText(SlabBar bar)
		{
			if (bar != null)
			{
				return "%%C" + bar.Diameter + " @ " + bar.SpacingMm.ToString("0.##", CultureInfo.InvariantCulture) + "mm C/C";
			}
			return "--";
		}

		public static SlabFormatResult Build(string raw, string prefix, string floor, bool merge)
		{
			if (string.IsNullOrWhiteSpace(floor))
			{
				throw new Exception("Enter floor title.");
			}
			List<SlabScheduleRow> list = SlabScheduleReader.Read(raw);
			List<SlabFormatGroup> list2 = Group(list, prefix, merge);
			if (list2.Count == 0)
			{
				throw new Exception("No slab rows to format.");
			}
			Dxf dxf;
			using (Stream stream = typeof(SlabFormatter).Assembly.GetManifestResourceStream("SlabTemplate.dxf"))
			{
				if (stream == null)
				{
					throw new Exception("Slab template resource missing.");
				}
				using (StreamReader streamReader = new StreamReader(stream, Encoding.UTF8, true))
				{
					dxf = Dxf.Parse(streamReader.ReadToEnd());
				}
			}
			List<Entity> source = dxf.ModelEntities();
			Entity title = source.FirstOrDefault((Entity e) => e.Type == "MTEXT" && Plain(e) == "SCHEDULE OF SLAB");
			if (title == null)
			{
				throw new Exception("Template SCHEDULE OF SLAB title not found.");
			}
			List<Entity> list3 = (from e in source
				where e.Type == "MTEXT" && Plain(e) == "FULL LENGTH" && Math.Abs(e.Num(10) - title.Num(10)) < 400.0 && Math.Abs(e.Num(20) - title.Num(20)) < 80.0
				orderby e.Num(10)
				select e).ToList();
			if (list3.Count != 2)
			{
				throw new Exception("Template full-length columns not found.");
			}
			double left = title.Num(10);
			double top = title.Num(20) - 3.0;
			double dataTop = list3[0].Num(20) - 7.0;
			double num9 = (list3[1].Num(10) - list3[0].Num(10)) / 2.0;
			double[] x = new double[13]
			{
				left,
				left + 25.0,
				left + 50.0,
				left + 80.0,
				left + 110.0,
				left + 140.0,
				left + 170.0,
				left + 200.0,
				left + 230.0,
				left + 260.0,
				left + 290.0,
				left + 317.0,
				left + 377.0
			};
			Entity textPrototype = source.First((Entity e) => e.Type == "MTEXT" && Regex.IsMatch(Plain(e), "^\\d+S-\\d+$") && e.Num(10) < left + 25.0);
			Entity linePrototype = source.First((Entity e) => e.Type == "LINE" && Math.Abs(e.Num(10) - left) < 0.01 && Math.Abs(e.Num(20) - top) < 0.01);
			List<Entity> output = new List<Entity>();
			long next = 1L;
			foreach (Entity record in dxf.Records)
			{
				long result;
				if (long.TryParse(record.Get(5), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result))
				{
					next = Math.Max(next, result + 1);
				}
			}
			Action<Entity> add = delegate(Entity e)
			{
				e.P.RemoveAll((Pair p) => p.Code == 5 || p.Code == 102);
				e.Set(5, next++.ToString("X"));
				output.Add(e);
			};
			Action<double, double, double, double> action = delegate(double a, double b, double c, double d)
			{
				Entity entity3 = linePrototype.Copy(true);
				entity3.Set(10, a);
				entity3.Set(20, b);
				entity3.Set(11, c);
				entity3.Set(21, d);
				add(entity3);
			};
			Action<string, double, double, double, double, bool> action2 = delegate(string value, double a, double b, double width, double height, bool bold)
			{
				Entity entity3 = textPrototype.Copy(true);
				entity3.P.RemoveAll((Pair p) => p.Code == 1 || p.Code == 3);
				entity3.Set(1, value);
				entity3.Set(10, a);
				entity3.Set(20, b);
				entity3.Set(40, height);
				entity3.Set(41, width);
				entity3.Set(71, "5");
				entity3.Set(7, bold ? "A 2.50B" : "A 2.0");
				add(entity3);
			};
			foreach (Entity item in source.Where((Entity e) => e.Type == "HATCH"))
			{
				List<double> list4 = new List<double>();
				List<double> list5 = new List<double>();
				bool flag = false;
				foreach (Pair item2 in item.P)
				{
					if (item2.Code == 92)
					{
						flag = true;
					}
					if (item2.Code == 97)
					{
						flag = false;
					}
					double result2;
					if (flag && double.TryParse(item2.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out result2))
					{
						if (item2.Code == 10)
						{
							list4.Add(result2);
						}
						if (item2.Code == 20)
						{
							list5.Add(result2);
						}
					}
				}
				if (list4.Count >= 4 && list5.Count >= 4 && list4.Min() >= left - 0.1 && list4.Max() <= x[11] + 0.1 && list5.Min() >= dataTop - 0.1 && list5.Max() <= top + 0.1)
				{
					Entity entity = item.Copy(true);
					entity.Set(71, "0");
					add(entity);
				}
			}
			foreach (Entity item3 in source.Where((Entity e) => e.Type == "MTEXT" && e.Num(10) >= left - 0.01 && e.Num(10) <= x[11] + 0.01 && e.Num(20) > dataTop && e.Num(20) < top + 18.0))
			{
				Entity entity2 = item3.Copy(true);
				if (Plain(item3).Contains("SECOND FLOOR"))
				{
					entity2.P.RemoveAll((Pair p) => p.Code == 1 || p.Code == 3);
					entity2.Set(1, "{\\fArial|b1|i0|c0|p34;" + floor.Trim().Replace("\\", "").Replace("{", "")
						.Replace("}", "") + "}");
				}
				add(entity2);
			}
			Action<string, double, bool> action3 = delegate(string value, double y, bool bold)
			{
				Entity entity3 = textPrototype.Copy(true);
				entity3.P.RemoveAll((Pair p) => p.Code == 1 || p.Code == 3);
				entity3.Set(1, value);
				entity3.Set(10, x[11] + 5.0);
				entity3.Set(20, y);
				entity3.Set(40, bold ? 2.5 : 1.8);
				entity3.Set(41, 60.0);
				entity3.Set(71, "4");
				entity3.Set(7, bold ? "A 2.50B" : "A 2.0");
				add(entity3);
			};
			action3("ORIGINAL SLAB MARKS", (top + dataTop) / 2.0, true);
			double num = dataTop;
			foreach (SlabFormatGroup item4 in list2)
			{
				double num2 = Math.Max(7.0, Math.Ceiling((double)item4.Originals.Length / 45.0) * 2.5 + 2.0);
				double num3 = num - num2 / 2.0;
				action2(item4.Mark, (x[0] + x[1]) / 2.0, num3, 23.0, 2.0, false);
				action2(item4.Schedule.Thickness.ToString("0.##", CultureInfo.InvariantCulture) + "mm", (x[1] + x[2]) / 2.0, num3, 23.0, 2.0, false);
				for (int num4 = 0; num4 < 9; num4++)
				{
					action2(BarText(item4.Schedule.Bars[num4]), (x[num4 + 2] + x[num4 + 3]) / 2.0, num3, 27.0, 2.0, false);
				}
				action3(item4.Originals, num3, false);
				action(x[0], num, x[11], num);
				num -= num2;
			}
			action(x[0], num, x[11], num);
			action(x[0], top, x[11], top);
			action(x[2], top - 7.0, x[10], top - 7.0);
			action(x[2], top - 14.0, x[10], top - 14.0);
			for (int num5 = 0; num5 < x.Length - 1; num5++)
			{
				double arg = (new int[4] { 3, 5, 7, 9 }.Contains(num5) ? (top - 14.0) : (new int[2] { 4, 8 }.Contains(num5) ? (top - 7.0) : top));
				action(x[num5], arg, x[num5], num);
			}
			int num6 = dxf.Records.FindIndex((Entity e) => e.Type == "SECTION" && e.Get(2) == "ENTITIES");
			int num7;
			for (num7 = num6 + 1; dxf.Records[num7].Type != "ENDSEC"; num7++)
			{
			}
			dxf.Records.RemoveRange(num6 + 1, num7 - num6 - 1);
			dxf.Records.InsertRange(num6 + 1, output);
			foreach (Entity record2 in dxf.Records)
			{
				for (int num8 = 0; num8 < record2.P.Count - 1; num8++)
				{
					if (record2.P[num8].Code == 9 && record2.P[num8].Value == "$HANDSEED")
					{
						record2.P[num8 + 1].Value = next.ToString("X");
					}
				}
			}
			return new SlabFormatResult
			{
				Drawing = dxf,
				Groups = list2,
				SourceCount = list.Count
			};
		}

		public static void Export(SlabFormatResult result, string path)
		{
			string text = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
			try
			{
				result.Drawing.Write(text);
				List<SlabScheduleRow> list = ReadRevised(Dxf.Read(text));
				if (list.Count != result.Groups.Count)
				{
					throw new Exception("Export verification failed.");
				}
				for (int i = 0; i < list.Count; i++)
				{
					if (list[i].Mark != result.Groups[i].Mark || Signature(list[i]) != Signature(result.Groups[i].Schedule))
					{
						throw new Exception("Slab schedule changed during export.");
					}
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

		public static List<SlabScheduleRow> ReadRevised(Dxf drawing)
		{
			List<Entity> source = (from e in drawing.ModelEntities()
				where e.Type == "MTEXT" || e.Type == "TEXT"
				select e).ToList();
			List<Entity> list = (from e in source
				where Plain(e) == "FULL LENGTH"
				orderby e.Num(10)
				select e).ToList();
			if (list.Count != 2)
			{
				throw new Exception("Expected two full-length headers in revised slab schedule.");
			}
			double first = list[0].Num(10);
			double step = (list[1].Num(10) - first) / 2.0;
			List<Entity> list2 = (from e in source
				where Regex.IsMatch(Plain(e), "^[A-Z0-9]*S-\\d{3,}$")
				orderby e.Num(20) descending
				select e).ToList();
			List<SlabScheduleRow> list3 = new List<SlabScheduleRow>();
			foreach (Entity mark in list2)
			{
				List<Entity> source2 = source.Where((Entity e) => Math.Abs(e.Num(20) - mark.Num(20)) < step / 10.0).ToList();
				List<Entity> list4 = source2.Where((Entity e) => e.Num(10) > mark.Num(10) && e.Num(10) < first - step / 2.0).ToList();
				if (list4.Count != 1)
				{
					throw new Exception("Missing/ambiguous slab thickness: " + Plain(mark));
				}
				double result;
				if (!double.TryParse(Plain(list4[0]).Replace("mm", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out result) || result <= 0.0)
				{
					throw new Exception("Invalid slab thickness.");
				}
				SlabBar[] array = new SlabBar[9];
				int c;
				for (c = 0; c < 9; c++)
				{
					List<Entity> list5 = source2.Where((Entity e) => Math.Abs(e.Num(10) - (first + (double)c * step)) < step * 0.4).ToList();
					if (list5.Count != 1)
					{
						throw new Exception("Missing/ambiguous revised reinforcement: " + Plain(mark));
					}
					array[c] = SlabBar.Parse(Plain(list5[0]).Replace("mm", ""));
				}
				if (list3.Any((SlabScheduleRow r) => r.Mark == Plain(mark)))
				{
					throw new Exception("Duplicate revised slab mark.");
				}
				list3.Add(new SlabScheduleRow
				{
					Mark = Plain(mark),
					Thickness = result,
					Bars = array
				});
			}
			if (list3.Count == 0)
			{
				throw new Exception("No revised slab marks found.");
			}
			return list3;
		}
	}
}
