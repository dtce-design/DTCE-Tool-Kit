using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BeamScheduleFormatter
{
	internal class StructureMemberTakeoff
	{
		public StructureMemberInput Input;

		public double Concrete;

		public double Forms;

		public double CHBArea;

		public double Gravel;

		public double Plaster;

		public double Waterproof;

		public int TotalQuantity;

		public EstimateInput Steel;

		public List<StockAllocation> Plan = new List<StockAllocation>();

		public static StructureMemberTakeoff Calculate(StructureMemberInput x, int structures, LengthConfiguration cfg)
		{
			if (x == null || !StructureMemberInput.Types.Contains(x.Kind) || string.IsNullOrWhiteSpace(x.Id) || string.IsNullOrWhiteSpace(x.StructureId) || string.IsNullOrWhiteSpace(x.Description))
			{
				throw new Exception("Enter member type and description.");
			}
			SteelWeight.Positive(x.L, x.B, x.ThicknessMm, x.Stock);
			SteelWeight.Count(x.Quantity);
			SteelWeight.Count(structures);
			QuantityMath.Finite(x.CoverMm, x.End1Mm, x.End2Mm, x.GravelMm);
			if (x.CoverMm < 0.0 || x.End1Mm < 0.0 || x.End2Mm < 0.0 || x.GravelMm < 0.0 || x.Layers < 1 || x.Layers > 2 || x.FormFaces < 0 || x.FormFaces > 2 || x.PlasterFaces < 0 || x.PlasterFaces > 2 || x.WaterproofFaces < 0 || x.WaterproofFaces > 2 || (x.FormEdge.HasValue && (double.IsNaN(x.FormEdge.Value) || double.IsInfinity(x.FormEdge.Value) || x.FormEdge.Value < 0.0)))
			{
				throw new Exception("Check covers, layers, faces and edge lengths.");
			}
			if (!CommercialBars.Lengths.Contains(x.Stock))
			{
				throw new Exception("Select a supported commercial rebar length.");
			}
			if (cfg == null || cfg.GradeSmall != 40 || cfg.GradeLarge != 40 || cfg.Input.Fc != 21.0)
			{
				throw new Exception("Structure quantities use the specified Class A / Grade40 profile.");
			}
			cfg.Validate();
			bool flag = x.Kind == "RC wall" || x.Kind == "CHB wall";
			bool flag2 = x.Kind == "CHB wall";
			if (flag2 && ((x.ThicknessMm != 100.0 && x.ThicknessMm != 150.0) || x.Layers != 1 || x.FormFaces != 0))
			{
				throw new Exception("CHB uses 100/150mm thickness, one reinforcement plane and no shutter faces.");
			}
			if ((x.Kind == "Slab on fill" && x.FormFaces != 0) || (x.Kind == "Slab" && x.FormFaces > 1))
			{
				throw new Exception("Slab-on-fill has no soffit forms; elevated slab can use one formed soffit face.");
			}
			if (x.Axis1 == null || x.Axis2 == null || x.Axis1.Diameter <= 0 || x.Axis2.Diameter <= 0 || 2.0 * x.CoverMm + (double)(x.Layers * (x.Axis1.Diameter + x.Axis2.Diameter)) > x.ThicknessMm)
			{
				throw new Exception("Thickness must fit cover and the crossing bars in the selected layers.");
			}
			StructureMemberTakeoff structureMemberTakeoff = new StructureMemberTakeoff
			{
				Input = x,
				TotalQuantity = checked(x.Quantity * structures)
			};
			SteelWeight.Count(structureMemberTakeoff.TotalQuantity);
			double num = x.L * x.B * (double)structureMemberTakeoff.TotalQuantity;
			double num2 = x.ThicknessMm / 1000.0;
			double num3 = x.FormEdge ?? (flag ? (2.0 * x.B) : (2.0 * (x.L + x.B)));
			structureMemberTakeoff.Concrete = (flag2 ? 0.0 : (num * num2));
			structureMemberTakeoff.CHBArea = (flag2 ? num : 0.0);
			structureMemberTakeoff.Forms = (flag2 ? 0.0 : (num * (double)x.FormFaces + num3 * num2 * (double)structureMemberTakeoff.TotalQuantity));
			structureMemberTakeoff.Gravel = ((x.Kind == "Slab on fill") ? (num * x.GravelMm / 1000.0) : 0.0);
			structureMemberTakeoff.Plaster = num * (double)x.PlasterFaces;
			structureMemberTakeoff.Waterproof = num * (double)x.WaterproofFaces;
			structureMemberTakeoff.Steel = new EstimateInput
			{
				Beam = x.StructureId,
				Description = x.Description + " | " + x.Kind,
				Quantity = structureMemberTakeoff.TotalQuantity.ToString(CultureInfo.InvariantCulture),
				Length = x.L.ToString("R", CultureInfo.InvariantCulture),
				Result = new BeamEstimate()
			};
			for (int i = 0; i < 2; i++)
			{
				FootingBarSpec footingBarSpec = ((i == 0) ? x.Axis1 : x.Axis2);
				double num4 = ((!flag) ? ((i == 0) ? x.L : x.B) : ((i == 0) ? x.B : x.L));
				double num5 = ((!flag) ? ((i == 0) ? x.B : x.L) : ((i == 0) ? x.L : x.B));
				double num6 = ((i == 0) ? x.End1Mm : x.End2Mm);
				if (footingBarSpec.Count < 0 || footingBarSpec.Spacing < 0.0 || footingBarSpec.Count > 0 == footingBarSpec.Spacing > 0.0)
				{
					throw new Exception("Enter spacing OR bar count in each direction.");
				}
				QuantityMath.Finite(footingBarSpec.Spacing);
				double num7 = num5 - 2.0 * x.CoverMm / 1000.0 - (double)footingBarSpec.Diameter / 1000.0;
				double num8 = num4 - 2.0 * x.CoverMm / 1000.0;
				if (num7 < 0.0 || num8 <= 0.0)
				{
					throw new Exception("Cover and diameter must fit both member dimensions.");
				}
				int num9 = ((footingBarSpec.Count > 0) ? footingBarSpec.Count : checked((int)Math.Ceiling(num7 / (footingBarSpec.Spacing / 1000.0) - 1E-10) + 1));
				SteelWeight.Count(num9);
				double num10 = Math.Ceiling((num8 + num6 / 1000.0) / 0.025 - 1E-10) * 0.025;
				for (int j = 0; j < x.Layers; j++)
				{
					double num11 = num10 * (double)num9 * (double)structureMemberTakeoff.TotalQuantity;
					string text = (flag ? ((i == 0) ? "Vertical" : "Horizontal") : ((i == 0) ? "Along length" : "Along width"));
					structureMemberTakeoff.Steel.Result.Lines.Add(new EstimateLine
					{
						Beam = x.StructureId,
						Part = x.Description + " | " + text + " layer " + (j + 1),
						Diameter = footingBarSpec.Diameter,
						Grade = 40,
						PiecesPerBeam = num9,
						CutLength = num10,
						Metres = num11,
						Kg = num11 * EstimatorEngine.UnitMass(footingBarSpec.Diameter),
						Formula = ((footingBarSpec.Count > 0) ? "Entered bars/plane" : "ceil((across - 2 cover - diameter)/spacing)+1") + " = " + num9 + "; cut = run - 2 cover + entered end allowance, UP25mm; " + structureMemberTakeoff.TotalQuantity + " member(s)"
					});
				}
			}
			structureMemberTakeoff.Plan = CommercialBars.Allocate(new EstimateInput[1] { structureMemberTakeoff.Steel }, cfg, new double[1] { x.Stock });
			if (structureMemberTakeoff.Plan.Any((StockAllocation p) => p.Choice == null))
			{
				throw new Exception("Unresolved member rebar purchase length.");
			}
			return structureMemberTakeoff;
		}
	}
}
