using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace BeamScheduleFormatter
{
	internal class BeamEarthworks
	{
		public const double WorkingSpacePerSide = 0.3;

		public const double DefaultSwellPercent = 20.0;

		public double FromExcavation;

		public double CommonBorrow;

		public double Depth;

		public double Width;

		public double Length;

		public double Allowance;

		public double Swell;

		public double Shrink;

		public double Excavation;

		public double LooseExcavation;

		public double Concrete;

		public double Gravel;

		public double OtherConcrete;

		public double Backfill;

		public double LooseRequired;

		public double LooseSurplus;

		public double LooseImport;

		public double LooseFromExcavation
		{
			get
			{
				return Math.Min(LooseExcavation, LooseRequired);
			}
		}

		public static BeamEarthworks Calculate(double b, double d, double length, int qty, double depth, double allowance, double swell, double shrink, double other)
		{
			if (new double[8] { b, d, length, depth, allowance, swell, shrink, other }.Any((double x) => double.IsNaN(x) || double.IsInfinity(x)) || b <= 0.0 || d <= 0.0 || length <= 0.0 || qty <= 0 || qty > 100000 || depth < d + 0.1 - 1E-09 || allowance < 0.0 || swell < 0.0 || swell > 200.0 || shrink < 0.0 || shrink >= 100.0 || other < 0.0)
			{
				throw new Exception("Earthworks: depth must be at least D + 0.10m; allowance >=0; swell 0-200%; bank shrinkage 0 to <100%; check length and quantity.");
			}
			BeamEarthworks beamEarthworks = new BeamEarthworks
			{
				Depth = depth,
				Width = b + 2.0 * allowance,
				Length = length,
				Allowance = allowance,
				Swell = swell,
				Shrink = shrink
			};
			beamEarthworks.Excavation = beamEarthworks.Width * depth * length * (double)qty;
			beamEarthworks.Concrete = b * d * length * (double)qty;
			beamEarthworks.Gravel = b * 0.1 * length * (double)qty;
			beamEarthworks.OtherConcrete = other * (double)qty;
			if (beamEarthworks.Concrete + beamEarthworks.Gravel + beamEarthworks.OtherConcrete > beamEarthworks.Excavation + 1E-08)
			{
				throw new Exception("Concrete + gravel exceed excavation. Check additional buried concrete volume.");
			}
			beamEarthworks.Backfill = Math.Max(0.0, beamEarthworks.Excavation - beamEarthworks.Concrete - beamEarthworks.Gravel - beamEarthworks.OtherConcrete);
			beamEarthworks.LooseExcavation = beamEarthworks.Excavation * (1.0 + swell / 100.0);
			beamEarthworks.LooseRequired = beamEarthworks.Backfill / (1.0 - shrink / 100.0) * (1.0 + swell / 100.0);
			beamEarthworks.FromExcavation = Math.Min(beamEarthworks.Backfill, beamEarthworks.Excavation * (1.0 - shrink / 100.0));
			beamEarthworks.CommonBorrow = Math.Max(0.0, beamEarthworks.Backfill - beamEarthworks.FromExcavation);
			beamEarthworks.LooseSurplus = Math.Max(0.0, beamEarthworks.LooseExcavation - beamEarthworks.LooseRequired);
			beamEarthworks.LooseImport = Math.Max(0.0, beamEarthworks.LooseRequired - beamEarthworks.LooseExcavation);
			return beamEarthworks;
		}

		public void Report(StringBuilder b, bool detail)
		{
			b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Excavation {0:0.00} bank m3 | Gravel {2:0.00} m3", Excavation, Backfill, Gravel));
			b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Embankment: from excavation {0:0.00} + COMMON BORROW {1:0.00} = {2:0.00} loose m3", LooseFromExcavation, LooseImport, LooseRequired));
			if (detail)
			{
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Volume basis: compacted space {0:0.00} m3; loose required {1:0.00} m3; loose surplus {2:0.00} m3", Backfill, LooseRequired, LooseSurplus));
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Trench W = B + 2({0:0.00}) = {1:0.00}m; depth {2:0.00}m; L {3:0.00}m per beam", Allowance, Width, Depth, Length));
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Backfill = {0:0.00} excavation - {1:0.00} beam - {2:0.00} gravel - {3:0.00} other concrete", Excavation, Concrete, Gravel, OtherConcrete));
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Loose = bank x {0:0.00}; compacted = bank x {1:0.00}", 1.0 + Swell / 100.0, 1.0 - Shrink / 100.0));
			}
		}
	}
}
