using System;
using System.Globalization;

namespace BeamScheduleFormatter
{
	internal class FootingTakeoff
	{
		public string Id;

		public string Description;

		public double Length;

		public double Width;

		public double Thickness;

		public double Depth;

		public double Concrete;

		public double Forms;

		public double Excavation;

		public double Gravel;

		public double Backfill;

		public int Quantity;

		public EstimateInput Steel;

		public EarthworkMaterial Soil;

		public static FootingTakeoff Calculate(string id, string description, double length, double width, double thickness, int quantity, double depth, double coverMm, bool formed, FootingBarSpec[] bars, double endExtra, LengthConfiguration configuration, double swell, double shrink, double reusable)
		{
			QuantityMath.Finite(length, width, thickness, depth, coverMm, endExtra);
			if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(description) || length <= 0.0 || width <= 0.0 || thickness <= 0.0 || quantity < 1 || quantity > 100000 || depth < thickness + 0.1 - 1E-08 || 2.0 * coverMm / 1000.0 >= Math.Min(length, width) || 2.0 * coverMm / 1000.0 >= thickness || bars.Length != 4 || configuration == null)
			{
				throw new Exception("Check footing ID/description, positive dimensions/quantity, depth >= T+0.10m and cover fitting footing dimensions.");
			}
			FootingTakeoff footingTakeoff = new FootingTakeoff
			{
				Id = id,
				Description = description,
				Length = length,
				Width = width,
				Thickness = thickness,
				Depth = depth,
				Quantity = quantity
			};
			footingTakeoff.Concrete = length * width * thickness * (double)quantity;
			footingTakeoff.Forms = (formed ? (2.0 * (length + width) * thickness * (double)quantity) : 0.0);
			footingTakeoff.Excavation = (length + 0.6) * (width + 0.6) * depth * (double)quantity;
			footingTakeoff.Gravel = length * width * 0.1 * (double)quantity;
			footingTakeoff.Backfill = footingTakeoff.Excavation - footingTakeoff.Concrete - footingTakeoff.Gravel;
			footingTakeoff.Soil = new EarthworkMaterial
			{
				Id = id,
				Source = "Footing",
				Bank = footingTakeoff.Excavation,
				Fill = footingTakeoff.Backfill,
				Swell = swell,
				Shrink = shrink,
				Reusable = reusable
			};
			footingTakeoff.Soil.Validate();
			footingTakeoff.Steel = new EstimateInput
			{
				Beam = id,
				Description = description,
				Quantity = quantity.ToString(CultureInfo.InvariantCulture),
				Length = length.ToString("R", CultureInfo.InvariantCulture),
				Result = new BeamEstimate()
			};
			for (int i = 0; i < 4; i++)
			{
				FootingBarSpec footingBarSpec = bars[i];
				QuantityMath.Finite(footingBarSpec.Diameter, footingBarSpec.Spacing, footingBarSpec.Count);
				if (footingBarSpec.Diameter == 0)
				{
					if (footingBarSpec.Count != 0 || footingBarSpec.Spacing != 0.0)
					{
						throw new Exception("Unused bar group must have blank spacing/count.");
					}
					continue;
				}
				if (footingBarSpec.Count > 0 == footingBarSpec.Spacing > 0.0)
				{
					throw new Exception("Enter spacing OR quantity for each bar group, not both.");
				}
				double num = ((i % 2 == 0) ? length : width);
				double num2 = ((i % 2 == 0) ? width : length) - 2.0 * coverMm / 1000.0 - (double)footingBarSpec.Diameter / 1000.0;
				if (num2 < 0.0)
				{
					throw new Exception("Bar diameter does not fit footing cover.");
				}
				int num3 = ((footingBarSpec.Count > 0) ? footingBarSpec.Count : checked((int)Math.Ceiling(num2 / (footingBarSpec.Spacing / 1000.0) - 1E-10) + 1));
				if (num3 < 1 || num3 > 100000)
				{
					throw new Exception("Bar count outside supported range.");
				}
				double num4 = num - 2.0 * coverMm / 1000.0 + endExtra;
				if (num4 <= 0.0)
				{
					throw new Exception("Invalid bar cut length.");
				}
				int grade = ((footingBarSpec.Diameter <= 12) ? configuration.GradeSmall : configuration.GradeLarge);
				double num5 = (double)num3 * num4 * (double)quantity;
				string part = ((i < 2) ? "Bottom " : "Top ") + ((i % 2 == 0) ? "X" : "Y");
				footingTakeoff.Steel.Result.Lines.Add(new EstimateLine
				{
					Beam = id,
					Part = part,
					Diameter = footingBarSpec.Diameter,
					Grade = grade,
					PiecesPerBeam = num3,
					CutLength = num4,
					Metres = num5,
					Kg = num5 * EstimatorEngine.UnitMass(footingBarSpec.Diameter),
					Formula = num3 + " pcs/footing x " + num4.ToString("0.00", CultureInfo.InvariantCulture) + "m x " + quantity
				});
			}
			if (footingTakeoff.Steel.Result.Lines.Count == 0)
			{
				throw new Exception("Enter at least one reinforcement group.");
			}
			return footingTakeoff;
		}
	}
}
