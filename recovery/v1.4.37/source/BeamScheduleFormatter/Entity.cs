using System.Collections.Generic;
using System.Globalization;

namespace BeamScheduleFormatter
{
	internal class Entity
	{
		public List<Pair> P = new List<Pair>();

		public string Type
		{
			get
			{
				return Get(0);
			}
		}

		public string Get(int code, string fallback = "")
		{
			foreach (Pair item in P)
			{
				if (item.Code == code)
				{
					return item.Value;
				}
			}
			return fallback;
		}

		public double Num(int c)
		{
			double result;
			if (!double.TryParse(Get(c), NumberStyles.Float, CultureInfo.InvariantCulture, out result))
			{
				return 0.0;
			}
			return result;
		}

		public void Set(int c, string v)
		{
			foreach (Pair item in P)
			{
				if (item.Code == c)
				{
					item.Value = v;
					return;
				}
			}
			P.Add(new Pair(c, v));
		}

		public void Set(int c, double v)
		{
			Set(c, v.ToString("R", CultureInfo.InvariantCulture));
		}

		public Entity Copy(bool clean = false)
		{
			Entity entity = new Entity();
			foreach (Pair item in P)
			{
				if (clean && item.Code == 101)
				{
					break;
				}
				entity.P.Add(new Pair(item.Code, item.Value));
			}
			return entity;
		}
	}
}
