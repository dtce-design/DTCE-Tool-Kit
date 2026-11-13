using System;
using System.Globalization;

namespace BeamScheduleFormatter
{
	internal class CanopyTakeoff
	{
		public string Id;

		public string Description;

		public double Length;

		public double Projection;

		public double Thickness;

		public double Concrete;

		public double SoffitForms;

		public double EdgeForms;

		public double Fc;

		public int Quantity;

		public EstimateInput Steel;

		public double Forms
		{
			get
			{
				return SoffitForms + EdgeForms;
			}
		}

		public static CanopyTakeoff Calculate(string id, string description, double length, double projection, double thicknessMm, int quantity, double coverMm, FootingBarSpec main, FootingBarSpec distribution, double anchorageMm, double distributionEndsMm, bool bottomMat, LengthConfiguration cfg)
		{
			if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(description) || cfg == null || main == null || distribution == null)
			{
				throw new Exception("Enter canopy/ledge ID, description and reinforcement.");
			}
			SteelWeight.Positive(length, projection, thicknessMm, anchorageMm);
			SteelWeight.Count(quantity);
			QuantityMath.Finite(coverMm, distributionEndsMm);
			if (coverMm < 0.0 || distributionEndsMm < 0.0 || 2.0 * coverMm / 1000.0 >= Math.Min(length, projection))
			{
				throw new Exception("Cover must fit length/projection; end allowance cannot be negative.");
			}
			if (main.Diameter <= 0 || distribution.Diameter <= 0 || 2.0 * coverMm + (double)((main.Diameter + distribution.Diameter) * ((!bottomMat) ? 1 : 2)) > thicknessMm)
			{
				throw new Exception("Thickness must fit cover and the specified crossing rebar layers.");
			}
			CanopyTakeoff canopyTakeoff = new CanopyTakeoff
			{
				Id = id,
				Description = description,
				Length = length,
				Projection = projection,
				Thickness = thicknessMm / 1000.0,
				Quantity = quantity,
				Fc = cfg.Input.Fc
			};
			canopyTakeoff.Concrete = length * projection * canopyTakeoff.Thickness * (double)quantity;
			canopyTakeoff.SoffitForms = length * projection * (double)quantity;
			canopyTakeoff.EdgeForms = (length + 2.0 * projection) * canopyTakeoff.Thickness * (double)quantity;
			canopyTakeoff.Steel = new EstimateInput
			{
				Beam = id,
				Description = description,
				Quantity = quantity.ToString(CultureInfo.InvariantCulture),
				Length = length.ToString("R", CultureInfo.InvariantCulture),
				Result = new BeamEstimate()
			};
			for (int i = 0; i < ((!bottomMat) ? 1 : 2); i++)
			{
				for (int j = 0; j < 2; j++)
				{
					FootingBarSpec footingBarSpec = ((j == 0) ? main : distribution);
					double num = ((j == 0) ? length : projection);
					if (footingBarSpec.Count < 0 || footingBarSpec.Spacing < 0.0 || footingBarSpec.Count > 0 == footingBarSpec.Spacing > 0.0)
					{
						throw new Exception("Enter spacing OR bar count for each direction, not both.");
					}
					QuantityMath.Finite(footingBarSpec.Spacing);
					double num2 = num - 2.0 * coverMm / 1000.0 - (double)footingBarSpec.Diameter / 1000.0;
					if (num2 < 0.0)
					{
						throw new Exception("Rebar diameter does not fit the available width and cover.");
					}
					int num3 = ((footingBarSpec.Count > 0) ? footingBarSpec.Count : checked((int)Math.Ceiling(num2 / (footingBarSpec.Spacing / 1000.0) - 1E-10) + 1));
					SteelWeight.Count(num3);
					double num4 = ((j == 0) ? (projection - coverMm / 1000.0 + anchorageMm / 1000.0) : (length - 2.0 * coverMm / 1000.0 + distributionEndsMm / 1000.0));
					double num5 = Math.Ceiling(num4 / 0.025 - 1E-10) * 0.025;
					double num6 = EstimatorEngine.UnitMass(footingBarSpec.Diameter);
					double num7 = (double)num3 * num5 * (double)quantity;
					string part = ((i == 0) ? "Top " : "Bottom ") + ((j == 0) ? "MAIN / projection" : "DISTRIBUTION / along wall");
					canopyTakeoff.Steel.Result.Lines.Add(new EstimateLine
					{
						Beam = id,
						Part = part,
						Diameter = footingBarSpec.Diameter,
						Grade = ((footingBarSpec.Diameter <= 12) ? cfg.GradeSmall : cfg.GradeLarge),
						PiecesPerBeam = num3,
						CutLength = num5,
						Metres = num7,
						Kg = num7 * num6,
						Formula = ((j == 0) ? "Main cut = projection - front cover + entered wall anchorage" : "Distribution cut = length - 2 end covers + total end allowance") + "; raw " + num4.ToString("0.000", CultureInfo.InvariantCulture) + "m; rounded up to 25mm; " + num3 + " pcs/unit x " + quantity
					});
				}
			}
			return canopyTakeoff;
		}
	}
}
