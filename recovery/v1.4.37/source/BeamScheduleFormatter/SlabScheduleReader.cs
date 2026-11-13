using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace BeamScheduleFormatter
{
	internal static class SlabScheduleReader
	{
		public static readonly string[] Columns = new string[9] { "Bottom short full", "Bottom short curtailed", "Bottom long full", "Bottom long curtailed", "Top over long support continuous", "Top over long support end", "Top over short support continuous", "Top over short support end", "Distribution" };

		private static string Text(Entity e)
		{
			return string.Concat(from p in e.P
				where p.Code == 1 || p.Code == 3
				select p.Value).Trim();
		}

		public static List<SlabScheduleRow> Read(string raw)
		{
			Dxf dxf = Dxf.Parse(raw);
			if (dxf.ModelEntities().Any((Entity e) => (e.Type == "TEXT" || e.Type == "MTEXT") && SlabFormatter.Plain(e) == "SCHEDULE OF SLAB"))
			{
				return SlabFormatter.ReadRevised(dxf);
			}
			List<Entity> source = (from e in dxf.ModelEntities()
				where e.Type == "TEXT" || e.Type == "MTEXT"
				select e).ToList();
			if (!source.Any((Entity e) => Text(e).ToUpperInvariant().Contains("SLAB SCHEDULE")))
			{
				throw new Exception("No slab schedule title found.");
			}
			List<Entity> list = (from e in source
				where Text(e).Equals("FULL LENGTH", StringComparison.OrdinalIgnoreCase)
				orderby e.Num(10)
				select e).ToList();
			if (list.Count != 2)
			{
				throw new Exception("Expected short/long full-length bottom column headers.");
			}
			double start = list[0].Num(10);
			double step = (list[1].Num(10) - start) / 2.0;
			if (step <= 0.0)
			{
				throw new Exception("Invalid slab column geometry.");
			}
			List<Entity> list2 = source.Where((Entity e) => Regex.IsMatch(Text(e), "^S\\d+(?:\\s*,\\s*S\\d+)*$", RegexOptions.IgnoreCase)).ToList();
			List<SlabScheduleRow> list3 = new List<SlabScheduleRow>();
			foreach (Entity mark in list2)
			{
				Entity entity = (from e in source
					where e.Num(10) > mark.Num(10) && e.Num(10) < start - step / 2.0 && Regex.IsMatch(Text(e), "^\\d+(?:\\.\\d+)?$") && Math.Abs(e.Num(20) - mark.Num(20)) < step / 5.0
					orderby Math.Abs(e.Num(20) - mark.Num(20))
					select e).FirstOrDefault();
				if (entity == null)
				{
					throw new Exception("Missing thickness for " + Text(mark));
				}
				double y = entity.Num(20);
				SlabBar[] array = new SlabBar[9];
				for (int num = 0; num < 9; num++)
				{
					double x = start + (double)num * step;
					List<Entity> list4 = source.Where((Entity e) => Math.Abs(e.Num(20) - y) < step / 20.0 && Math.Abs(e.Num(10) - x) < step * 0.49).ToList();
					if (list4.Count != 1)
					{
						throw new Exception("Missing/ambiguous " + Columns[num] + " for " + Text(mark));
					}
					array[num] = SlabBar.Parse(Text(list4[0]));
				}
				string[] array2 = Text(mark).Split(',');
				foreach (string text in array2)
				{
					string key = text.Trim().ToUpperInvariant();
					if (list3.Any((SlabScheduleRow r) => r.Mark == key))
					{
						throw new Exception("Duplicate slab mark " + key);
					}
					list3.Add(new SlabScheduleRow
					{
						Mark = key,
						Thickness = double.Parse(Text(entity), CultureInfo.InvariantCulture),
						Bars = array
					});
				}
			}
			if (list3.Count == 0)
			{
				throw new Exception("No slab marks found.");
			}
			return list3;
		}
	}
}
