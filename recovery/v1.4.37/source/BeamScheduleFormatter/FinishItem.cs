using System;
using System.Collections.Generic;
using System.Linq;

namespace BeamScheduleFormatter
{
	internal class FinishItem
	{
		public string Id;

		public string Description;

		public string Code;

		public string Category;

		public string Wall;

		public string Kind;

		public string Side;

		public string Basis;

		public string MaterialName;

		public double Length;

		public double Width;

		public double Deduction;

		public int Quantity;

		public bool OnA
		{
			get
			{
				if (!(Side == "A"))
				{
					return Side == "Both";
				}
				return true;
			}
		}

		public bool OnB
		{
			get
			{
				if (!(Side == "B"))
				{
					return Side == "Both";
				}
				return true;
			}
		}

		public int Faces
		{
			get
			{
				if (!(Category == "Wall") || !(Side == "Both"))
				{
					return 1;
				}
				return 2;
			}
		}

		public bool AutoSurface
		{
			get
			{
				if (!AutoPaint)
				{
					if (Category == "Wall" && Basis == "Whole wall (auto)")
					{
						return FinishTypeCatalog.IsPlaster(MaterialName);
					}
					return false;
				}
				return true;
			}
		}

		public bool AutoPaint
		{
			get
			{
				if (Category == "Wall" && Basis == "Whole wall (auto)")
				{
					return Kind == "Paint";
				}
				return false;
			}
		}

		public void Validate()
		{
			if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(Description) || string.IsNullOrWhiteSpace(Code))
			{
				throw new Exception("Enter Entry ID, description and finish/tile code.");
			}
			if (Category != "Wall" && Category != "Ceiling" && Category != "Floor")
			{
				throw new Exception("Invalid finish category.");
			}
			if (Category == "Wall")
			{
				if (string.IsNullOrWhiteSpace(Wall))
				{
					throw new Exception("Select a linked Wall ID.");
				}
				if (Side != "A" && Side != "B" && Side != "Both")
				{
					throw new Exception("Select wall Side A, B or Both.");
				}
				if (Kind != "Tiles" && Kind != "Paint" && Kind != "Other")
				{
					throw new Exception("Select finish type.");
				}
				if (Basis != "Dimensions" && Basis != "Whole wall (auto)")
				{
					throw new Exception("Select Dimensions or Whole wall (auto).");
				}
			}
			if (!(Category != "Wall") && !(Basis != "Whole wall (auto)"))
			{
				return;
			}
			double[] array = new double[3] { Length, Width, Deduction };
			foreach (double num in array)
			{
				if (double.IsNaN(num) || double.IsInfinity(num) || num < 0.0 || num > 100000000.0)
				{
					throw new Exception("Check finish dimensions and deduction.");
				}
			}
			if (Length <= 0.0 || Width <= 0.0 || Quantity < 1 || Quantity > 100000)
			{
				throw new Exception("Enter positive finish dimensions and whole quantity.");
			}
			if (Deduction > Length * Width * (double)Quantity + 1E-07)
			{
				throw new Exception("Finish deduction exceeds gross area.");
			}
		}

		public double GrossPerFace(WallItem wall, IEnumerable<OpeningItem> openings)
		{
			Validate();
			if (Category == "Wall" && Basis == "Whole wall (auto)")
			{
				if (wall == null)
				{
					throw new Exception("Wall ID not found.");
				}
				return wall.Gross;
			}
			return Length * Width * (double)Quantity;
		}

		public double DeductPerFace(WallItem wall, IEnumerable<OpeningItem> openings)
		{
			if (Category == "Wall" && Basis == "Whole wall (auto)")
			{
				if (wall == null)
				{
					throw new Exception("Wall ID not found.");
				}
				return wall.Deduction(openings);
			}
			return Deduction;
		}

		public double PerFace(WallItem wall, IEnumerable<OpeningItem> openings)
		{
			return Math.Max(0.0, GrossPerFace(wall, openings) - DeductPerFace(wall, openings));
		}

		public double PaintFace(WallItem wall, IEnumerable<OpeningItem> openings, IEnumerable<FinishItem> all, bool sideA)
		{
			double num = PerFace(wall, openings);
			double num2 = all.Where((FinishItem f) => f.Category == "Wall" && f.Kind == "Tiles" && string.Equals(f.Wall.Trim(), Wall.Trim(), StringComparison.OrdinalIgnoreCase) && ((!sideA) ? f.OnB : f.OnA)).Sum((FinishItem f) => f.PerFace(wall, openings));
			return Math.Max(0.0, num - num2);
		}

		public double Total(WallItem wall, IEnumerable<OpeningItem> openings, IEnumerable<FinishItem> all = null)
		{
			if (AutoSurface && all != null)
			{
				return (OnA ? PaintFace(wall, openings, all, true) : 0.0) + (OnB ? PaintFace(wall, openings, all, false) : 0.0);
			}
			return PerFace(wall, openings) * (double)Faces;
		}
	}
}
