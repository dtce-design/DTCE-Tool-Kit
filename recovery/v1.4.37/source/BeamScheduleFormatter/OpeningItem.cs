using System;

namespace BeamScheduleFormatter
{
	internal class OpeningItem
	{
		public string Id;

		public string Description;

		public string Wall;

		public string Material;

		public string Kind;

		public double Width;

		public double Height;

		public double JambDepth;

		public double JambThickness;

		public double LeafThickness;

		public int Quantity;

		public int Faces;

		public int HingesPerUnit;

		public int LocksPerUnit;

		public int Leaves = 1;

		public string Operation = "Swing";

		public bool Exterior;

		public string MouldingProfile = "";

		public double MouldingLength
		{
			get
			{
				if (!(Material == "Manual"))
				{
					return OpeningMoulding.Length(Kind, Width, Height, Quantity, Exterior);
				}
				return 0.0;
			}
		}

		public double Area
		{
			get
			{
				return Width * Height * (double)Quantity;
			}
		}

		public bool Wood
		{
			get
			{
				return Material == "Wood";
			}
		}

		public double LeafWidth
		{
			get
			{
				return Width - 2.0 * JambThickness;
			}
		}

		public double LeafHeight
		{
			get
			{
				return Height - (double)((!(Kind == "Window")) ? 1 : 2) * JambThickness;
			}
		}

		public double EffectiveThickness
		{
			get
			{
				if (!Wood || !(Kind == "Door"))
				{
					return LeafThickness;
				}
				return 0.045;
			}
		}

		public double LeafPaint
		{
			get
			{
				if (!Wood)
				{
					return 0.0;
				}
				return ((double)Faces * LeafWidth * LeafHeight + 2.0 * (LeafWidth + LeafHeight * (double)((!(Kind == "Door")) ? 1 : Leaves)) * EffectiveThickness) * (double)Quantity;
			}
		}

		public double JambPaint
		{
			get
			{
				if (!Wood)
				{
					return 0.0;
				}
				return (2.0 * Height + Width + ((Kind == "Window") ? Width : 0.0)) * (JambDepth + 2.0 * JambThickness) * (double)Quantity;
			}
		}

		public int EffectiveHinges
		{
			get
			{
				if (Material == "Manual" || Operation == "Fixed" || Operation == "Sliding")
				{
					return 0;
				}
				if (Kind == "Door")
				{
					return checked(OpeningHardware.PerLeaf(Wood ? LeafHeight : Height, (Wood ? LeafWidth : Width) / (double)Leaves) * Leaves);
				}
				return HingesPerUnit;
			}
		}

		public int EffectiveLocks
		{
			get
			{
				if (Material == "Manual" || Operation == "Fixed")
				{
					return 0;
				}
				if (!(Kind == "Door"))
				{
					return LocksPerUnit;
				}
				return 1;
			}
		}

		public int Hinges
		{
			get
			{
				return checked(EffectiveHinges * Quantity);
			}
		}

		public int Locks
		{
			get
			{
				return checked(EffectiveLocks * Quantity);
			}
		}

		public void Validate()
		{
			if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(Description) || (Material == "Manual" && string.IsNullOrWhiteSpace(Wall)))
			{
				throw new Exception("Enter Opening ID and description; manual CHB openings also require Wall ID.");
			}
			if (Material != "Wood" && Material != "Aluminum" && Material != "Manual")
			{
				throw new Exception("Select Wood or Aluminum.");
			}
			if (Leaves < 1 || Leaves > 8)
			{
				throw new Exception("Door leaves/unit must be between 1 and 8.");
			}
			if (Operation != "Swing" && Operation != "Sliding" && Operation != "Fixed")
			{
				throw new Exception("Select Swing, Sliding or Fixed operation.");
			}
			if (Kind != "Door" && Kind != "Window")
			{
				throw new Exception("Select Door or Window.");
			}
			double[] array = new double[5] { Width, Height, JambDepth, JambThickness, LeafThickness };
			foreach (double num in array)
			{
				if (double.IsNaN(num) || double.IsInfinity(num) || num < 0.0 || num > 10000.0)
				{
					throw new Exception("Check opening dimensions.");
				}
			}
			if (Width <= 0.0 || Height <= 0.0 || Quantity < 1 || Quantity > 100000 || Faces < 0 || Faces > 2 || HingesPerUnit < 0 || HingesPerUnit > 100 || LocksPerUnit < 0 || LocksPerUnit > 100)
			{
				throw new Exception("Check dimensions, quantity, faces and hardware counts.");
			}
			if (Wood && (JambDepth <= 0.0 || JambThickness <= 0.0 || LeafThickness <= 0.0 || LeafWidth <= 0.0 || LeafHeight <= 0.0))
			{
				throw new Exception("Enter wooden leaf and jamb dimensions.");
			}
		}
	}
}
