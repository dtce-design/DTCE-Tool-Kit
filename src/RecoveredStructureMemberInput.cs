using System;
using System.Globalization;

namespace BeamScheduleFormatter
{
	internal class StructureMemberInput
	{
		public const string Slab = "Slab";

		public const string OnFill = "Slab on fill";

		public const string RCWall = "RC wall";

		public const string CHB = "CHB wall";

		public static readonly string[] Types = new string[4] { "Slab", "Slab on fill", "RC wall", "CHB wall" };

		public string Id;

		public string StructureId;

		public string Kind;

		public string Description;

		public double L;

		public double B;

		public double ThicknessMm;

		public double CoverMm;

		public double End1Mm;

		public double End2Mm;

		public double GravelMm;

		public double Stock;

		public double? FormEdge;

		public int Quantity;

		public int Layers;

		public int FormFaces;

		public int PlasterFaces;

		public int WaterproofFaces;

		public FootingBarSpec Axis1;

		public FootingBarSpec Axis2;

		public static StructureMemberInput Read(string[] values)
		{
			if (values.Length != 24)
			{
				throw new Exception("Incomplete member input.");
			}
			Func<int, double> number = delegate(int i)
			{
				if (values[i] == "")
				{
					return 0.0;
				}
				double result;
				if (!double.TryParse(values[i], NumberStyles.Float, CultureInfo.InvariantCulture, out result) || double.IsNaN(result) || double.IsInfinity(result))
				{
					throw new Exception("Enter finite numeric member inputs.");
				}
				return result;
			};
			Func<int, int> func = delegate(int i)
			{
				double num = number(i);
				if (num < 0.0 || num > 1000000.0 || num != Math.Truncate(num))
				{
					throw new Exception("Member/bar quantities must be nonnegative whole numbers.");
				}
				return (int)num;
			};
			return new StructureMemberInput
			{
				Id = values[0],
				StructureId = values[1],
				Kind = values[2],
				Description = values[3],
				L = number(4),
				B = number(5),
				ThicknessMm = number(6),
				Quantity = func(7),
				CoverMm = number(8),
				Layers = func(9),
				Axis1 = new FootingBarSpec
				{
					Diameter = func(10),
					Spacing = number(11),
					Count = func(12)
				},
				Axis2 = new FootingBarSpec
				{
					Diameter = func(13),
					Spacing = number(14),
					Count = func(15)
				},
				End1Mm = number(16),
				End2Mm = number(17),
				FormFaces = func(18),
				FormEdge = ((values[19] == "") ? ((double?)null) : new double?(number(19))),
				GravelMm = number(20),
				PlasterFaces = func(21),
				WaterproofFaces = func(22),
				Stock = number(23)
			};
		}
	}
}
