using System;
using System.Collections.Generic;
using System.Linq;

namespace BeamScheduleFormatter
{
	internal class WallItem
	{
		public string Id;

		public string Description;

		public string Material = "CHB";

		public string Plaster = "2 sides";

		public string Paint = "Same as plaster";

		public double ThicknessMm = 150.0;

		public double Length;

		public double Height;

		public double BlockWidth;

		public double BlockHeight;

		public int Quantity;

		public double? EnteredArea;

		public double Gross
		{
			get
			{
				if (!EnteredArea.HasValue)
				{
					return Length * Height * (double)Quantity;
				}
				return EnteredArea.Value;
			}
		}

		public void Validate()
		{
			if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(Description))
			{
				throw new Exception("Enter Wall ID and description.");
			}
			if (EnteredArea.HasValue && (double.IsNaN(EnteredArea.Value) || double.IsInfinity(EnteredArea.Value) || EnteredArea.Value <= 0.0))
			{
				throw new Exception("Enter a positive gross wall area in m2, before opening deductions.");
			}
			double[] array = ((!EnteredArea.HasValue) ? new double[4] { Length, Height, BlockWidth, BlockHeight } : new double[2] { BlockWidth, BlockHeight });
			foreach (double num in array)
			{
				if (double.IsNaN(num) || double.IsInfinity(num) || num <= 0.0 || num > 10000.0)
				{
					throw new Exception("Wall/block dimensions must be positive.");
				}
			}
			if (double.IsNaN(ThicknessMm) || double.IsInfinity(ThicknessMm) || (ThicknessMm != 100.0 && ThicknessMm != 150.0))
			{
				throw new Exception("Select CHB thickness: 100 or 150 mm.");
			}
			if (Quantity < 1 || Quantity > 100000)
			{
				throw new Exception("Enter a positive whole wall quantity.");
			}
		}

		public double Deduction(IEnumerable<OpeningItem> openings)
		{
			return openings.Where((OpeningItem o) => string.Equals(o.Wall.Trim(), Id.Trim(), StringComparison.OrdinalIgnoreCase)).Sum((OpeningItem o) => o.Area);
		}

		public double Net(IEnumerable<OpeningItem> openings)
		{
			Validate();
			if (openings.Count((OpeningItem o) => string.Equals(o.Wall.Trim(), Id.Trim(), StringComparison.OrdinalIgnoreCase)) > 3)
			{
				throw new Exception("Maximum 3 opening entries per wall group (linked + manual); each entry can have multiple units.");
			}
			double num = Gross - Deduction(openings);
			if (num < -1E-07)
			{
				throw new Exception("Openings exceed gross wall area. Check dimensions and total opening quantities.");
			}
			return Math.Max(0.0, num);
		}

		public long Blocks(IEnumerable<OpeningItem> openings)
		{
			return checked((long)Math.Ceiling(Math.Round(Net(openings) / (BlockWidth * BlockHeight), 10)));
		}
	}
}
