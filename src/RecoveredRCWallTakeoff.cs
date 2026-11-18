using System;
using System.Globalization;

namespace BeamScheduleFormatter
{
	internal class RCWallTakeoff
	{
		public string Id;

		public string Description;

		public double Length;

		public double Height;

		public double Thickness;

		public double Cover;

		public double Concrete;

		public double Forms;

		public double Fc;

		public int Quantity;

		public int Layers;

		public int FormFaces;

		public int EndFaces;

		public bool BottomForm;

		public EstimateInput Steel;

		public static RCWallTakeoff Calculate(string id, string description, double length, double height, double thicknessMm, int quantity, double coverMm, int layers, FootingBarSpec vertical, FootingBarSpec horizontal, double verticalEndsMm, double horizontalEndsMm, int formFaces, int endFaces, bool bottomForm, LengthConfiguration cfg)
		{
			if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(description) || cfg == null || vertical == null || horizontal == null)
			{
				throw new Exception("Enter wall ID, description and reinforcement details.");
			}
			SteelWeight.Positive(length, height, thicknessMm);
			SteelWeight.Count(quantity);
			QuantityMath.Finite(coverMm, verticalEndsMm, horizontalEndsMm);
			if (coverMm < 0.0 || verticalEndsMm < 0.0 || horizontalEndsMm < 0.0 || layers < 1 || layers > 2 || formFaces < 0 || formFaces > 2 || endFaces < 0 || endFaces > 2)
			{
				throw new Exception("Select 1 or 2 rebar layers; cover/end allowances must be nonnegative and formed faces/ends 0 to 2.");
			}
			cfg.Validate();
			if (2.0 * coverMm + (double)(layers * (vertical.Diameter + horizontal.Diameter)) > thicknessMm)
			{
				double need = 2.0 * coverMm + (double)(layers * (vertical.Diameter + horizontal.Diameter));
				/* Owner (18 November 2026): say which numbers do not fit, and where the cover comes from. */
				throw new Exception(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Wall {0:0.#} mm is less than 2 x {1:0.#} mm cover + {2} layer(s) x ({3} + {4} mm bars) = {5:0.#} mm. Set a smaller RC wall cover in Reinforced Concrete Works > Start, then Apply covers to existing entries (NSCP 2015 Table 420.6.1.3.1: 20 mm for walls not exposed to weather or ground), or use a thicker wall.", thicknessMm, coverMm, layers, vertical.Diameter, horizontal.Diameter, need));
			}
			RCWallTakeoff rCWallTakeoff = new RCWallTakeoff
			{
				Id = id,
				Description = description,
				Length = length,
				Height = height,
				Thickness = thicknessMm / 1000.0,
				Cover = coverMm / 1000.0,
				Quantity = quantity,
				Layers = layers,
				FormFaces = formFaces,
				EndFaces = endFaces,
				BottomForm = bottomForm,
				Fc = cfg.Input.Fc
			};
			rCWallTakeoff.Concrete = length * height * rCWallTakeoff.Thickness * (double)quantity;
			rCWallTakeoff.Forms = ((double)formFaces * length * height + (double)endFaces * height * rCWallTakeoff.Thickness + (bottomForm ? (length * rCWallTakeoff.Thickness) : 0.0)) * (double)quantity;
			rCWallTakeoff.Steel = new EstimateInput
			{
				Beam = id,
				Description = description,
				Quantity = quantity.ToString(CultureInfo.InvariantCulture),
				Length = length.ToString("R", CultureInfo.InvariantCulture),
				Result = new BeamEstimate()
			};
			for (int i = 0; i < 2; i++)
			{
				FootingBarSpec footingBarSpec = ((i == 0) ? vertical : horizontal);
				double num = ((i == 0) ? length : height);
				double num2 = ((i == 0) ? height : length);
				double num3 = ((i == 0) ? verticalEndsMm : horizontalEndsMm);
				if (footingBarSpec.Diameter <= 0 || footingBarSpec.Count < 0 || footingBarSpec.Spacing < 0.0 || footingBarSpec.Count > 0 == footingBarSpec.Spacing > 0.0)
				{
					throw new Exception("For each direction enter a diameter and either spacing OR number of bars per layer.");
				}
				double num4 = EstimatorEngine.UnitMass(footingBarSpec.Diameter);
				QuantityMath.Finite(footingBarSpec.Spacing);
				double num5 = num - 2.0 * rCWallTakeoff.Cover - (double)footingBarSpec.Diameter / 1000.0;
				if (num5 < 0.0 || num2 - 2.0 * rCWallTakeoff.Cover <= 0.0)
				{
					throw new Exception("Cover and bar diameter must fit the wall length and height.");
				}
				int num6 = ((footingBarSpec.Count > 0) ? footingBarSpec.Count : checked((int)Math.Ceiling(num5 / (footingBarSpec.Spacing / 1000.0) - 1E-10) + 1));
				SteelWeight.Count(num6);
				double num7 = num2 - 2.0 * rCWallTakeoff.Cover + num3 / 1000.0;
				double num8 = Math.Ceiling(num7 / 0.025 - 1E-10) * 0.025;
				for (int j = 0; j < layers; j++)
				{
					double num9 = num8 * (double)num6 * (double)quantity;
					string part = ((i == 0) ? "VERTICAL" : "HORIZONTAL") + " / layer " + (j + 1);
					rCWallTakeoff.Steel.Result.Lines.Add(new EstimateLine
					{
						Beam = id,
						Part = part,
						Diameter = footingBarSpec.Diameter,
						Grade = ((footingBarSpec.Diameter <= 12) ? cfg.GradeSmall : cfg.GradeLarge),
						PiecesPerBeam = num6,
						CutLength = num8,
						Metres = num9,
						Kg = num9 * num4,
						Formula = ((footingBarSpec.Count > 0) ? ("Entered " + num6 + " bars/layer") : ("ceil((across - 2 cover - diameter)/spacing) + 1 = " + num6 + " bars/layer")) + "; cut = run - 2 cover + entered total end allowance = " + num7.ToString("0.########", CultureInfo.InvariantCulture) + "m, rounded up to 25mm; x " + quantity + " wall(s)"
					});
				}
			}
			return rCWallTakeoff;
		}
	}
}
