using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace BeamScheduleFormatter
{
	internal class TemplateLayout
	{
		public Entity Title;

		public Entity FloorTitle;

		public Entity Mark;

		public Entity Width;

		public Entity Depth;

		public Entity Sfr;

		public Entity Leg;

		public Entity Diameter;

		public Entity Spacing;

		public Entity Lower;

		public Entity Plus;

		public Entity[] Bars;

		public List<Entity> Rules;

		public double Left;

		public double Right;

		public double Top;

		public double Bottom;

		public double Center;

		public double Height;

		public double LowerOffset;

		public double PlusOffset;

		public TemplateLayout(Dxf d)
		{
			List<Entity> source = d.ModelEntities();
			List<Entity> text = source.Where((Entity e) => e.Type == "MTEXT").ToList();
			Title = text.Single((Entity e) => e.Get(1).StartsWith("SCHEDULE OF BEAMS"));
			FloorTitle = (from e in text
				where e.Num(20) > Title.Num(20) && Regex.IsMatch(e.Get(1), "FLOOR|ROOF|LEVEL", RegexOptions.IgnoreCase)
				orderby e.Num(20)
				select e).FirstOrDefault();
			Mark = (from e in text
				where Regex.IsMatch(e.Get(1), "^\\d*[A-Z]+-\\d{3,}$")
				orderby e.Num(20) descending
				select e).First();
			List<Entity> source2 = source.Where((Entity e) => e.Type == "LINE").ToList();
			Left = source2.Min((Entity e) => Math.Min(e.Num(10), e.Num(11)));
			Right = source2.Max((Entity e) => Math.Max(e.Num(10), e.Num(11)));
			Bottom = source2.Min((Entity e) => Math.Min(e.Num(20), e.Num(21)));
			Rules = (from e in source
				where e.Type == "LWPOLYLINE" && (from p in e.P
					where p.Code == 20
					select double.Parse(p.Value, CultureInfo.InvariantCulture)).Distinct().Count() == 1 && e.P.Where((Pair p) => p.Code == 10).Max((Pair p) => double.Parse(p.Value, CultureInfo.InvariantCulture)) - e.P.Where((Pair p) => p.Code == 10).Min((Pair p) => double.Parse(p.Value, CultureInfo.InvariantCulture)) > Right - Left - 0.01
				orderby e.Num(20) descending
				select e).ToList();
			Top = Rules.Where((Entity e) => e.Num(20) > Mark.Num(20)).Min((Entity e) => e.Num(20));
			Rules = Rules.Where((Entity e) => e.Num(20) < Top - 0.001).ToList();
			Height = Top - Rules[0].Num(20);
			List<Entity> body = text.Where((Entity e) => e.Num(20) < Top && e.Num(20) > Top - Height).ToList();
			Func<string, Entity> func = (string name) => text.Single((Entity e) => e.Num(20) > Top && e.Get(1) == name);
			Func<double, Entity> func2 = (double x) => (from e in body
				where Math.Abs(e.Num(10) - x) < 1.0 && Math.Abs(e.Num(20) - Mark.Num(20)) < 1.0 && e.Get(1) != "+"
				orderby Math.Abs(e.Num(20) - Mark.Num(20))
				select e).First();
			Width = func2(func("B").Num(10));
			Depth = func2(func("D").Num(10));
			Center = Width.Num(20);
			Sfr = func2(func("SFR").Num(10));
			Leg = func2(func("LEG").Num(10));
			Diameter = func2(func("%%C").Num(10));
			Spacing = func2(func("SPACING").Num(10));
			Bars = (from e in text
				where e.Num(20) > Top && new string[3] { "LEFT", "MIDSPAN", "RIGHT" }.Contains(e.Get(1))
				orderby e.Num(10)
				select (from b in body
					where Math.Abs(b.Num(10) - e.Num(10)) < 1.0 && b.Get(71).Trim() == "8" && b.Get(1) != "+"
					orderby b.Num(20) descending
					select b).First()).ToArray();
			if (Bars.Length != 6)
			{
				throw new Exception("Template needs six upper reinforcement cells.");
			}
			Lower = body.First((Entity e) => e.Get(71).Trim() == "2");
			Plus = body.First((Entity e) => e.Get(1) == "+");
			Entity entity = Bars.OrderBy((Entity e) => Math.Abs(e.Num(10) - Lower.Num(10))).First();
			LowerOffset = Lower.Num(10) - entity.Num(10);
			PlusOffset = Plus.Num(10) - entity.Num(10);
		}

		public string LegText(string value)
		{
			string text = Leg.Get(1);
			if (!text.StartsWith("{") || !text.Contains(";"))
			{
				return value;
			}
			return text.Substring(0, text.LastIndexOf(';') + 1) + value + "}";
		}
	}
}
