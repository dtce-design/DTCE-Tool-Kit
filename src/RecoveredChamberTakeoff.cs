using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BeamScheduleFormatter
{
	internal class ChamberTakeoff
	{
		public const string Concrete = "Septic Vault, Concrete";

		public const string Chb = "Septic Vault, CHB";

		public const string Basin = "Catch Basin (CHB)";

		public const string Canal = "Lined Canal";

		public bool ComponentMode;

		public double MemberCHB100;

		public double MemberCHB150;

		public string Id;

		public string Description;

		public string Kind;

		public int Count;

		public int PlasterFaces;

		public int WaterproofFaces;

		public bool PlasterFloor;

		public bool WaterproofFloor;

		public double L;

		public double W;

		public double H;

		public double Wall;

		public double Base;

		public double Cover;

		public double OpeningL;

		public double OpeningW;

		public double PartitionLength;

		public double PartitionThickness;

		public double WallOpeningArea;

		public double Gravel;

		public double Allowance;

		public double SoilCover;

		public double OtherConcrete;

		public double OtherForms;

		public double LsQuantity;

		public double RebarPurchase;

		public double Excavation;

		public double Backfill;

		public double GravelVolume;

		public double ConcreteVolume;

		public double FormArea;

		public double ChbArea;

		public double PlasterArea;

		public double WaterproofArea;

		public double BaseConcrete;

		public double CoverConcrete;

		public double WallConcrete;

		public double AdditionalConcrete;

		public double WallForms;

		public double BaseForms;

		public double CoverForms;

		public double AdditionalForms;

		public void Calculate()
		{
			if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(Description) || !new string[4] { "Septic Vault, Concrete", "Septic Vault, CHB", "Catch Basin (CHB)", "Lined Canal" }.Contains(Kind))
			{
				throw new Exception("Enter Structure ID, description and type.");
			}
			SteelWeight.Count(Count);
			SteelWeight.Positive(L, W, H, Base);
			if (double.IsNaN(LsQuantity) || double.IsInfinity(LsQuantity) || LsQuantity < 0.0)
			{
				throw new Exception("Invalid summary quantity.");
			}
			double[] array = new double[12]
			{
				Wall, Cover, OpeningL, OpeningW, PartitionLength, PartitionThickness, WallOpeningArea, Gravel, Allowance, SoilCover,
				OtherConcrete, OtherForms
			};
			foreach (double num in array)
			{
				if (double.IsNaN(num) || double.IsInfinity(num) || num < 0.0 || num > 100000.0)
				{
					throw new Exception("Chamber dimensions must be finite nonnegative SI values.");
				}
			}
			if (Kind == "Septic Vault, CHB")
			{
				Wall = 0.15;
			}
			if (Kind == "Catch Basin (CHB)")
			{
				Wall = 0.1;
			}
			SteelWeight.Positive(Wall);
			if ((Kind != "Lined Canal" && L <= 2.0 * Wall) || W <= 2.0 * Wall)
			{
				throw new Exception("Outer dimensions must exceed two wall thicknesses.");
			}
			if (Kind != "Septic Vault, Concrete" && PartitionLength > 0.0)
			{
				PartitionThickness = Wall;
			}
			if (PartitionLength > 0.0 && PartitionThickness <= 0.0)
			{
				throw new Exception("Enter partition thickness for the measured partition length.");
			}
			if (Kind == "Lined Canal")
			{
				CalculateCanal();
				return;
			}
			double num2 = L - 2.0 * Wall;
			double num3 = W - 2.0 * Wall;
			double num4 = 2.0 * (L + W);
			double num5 = 2.0 * (num2 + num3);
			double num6 = num2 * num3;
			if (OpeningL == 0.0 != (OpeningW == 0.0) || OpeningL > num2 || OpeningW > num3)
			{
				throw new Exception("Enter both cover-opening dimensions within the clear chamber dimensions, or zero for both.");
			}
			double num7 = OpeningL * OpeningW;
			double num8 = ((Kind == "Septic Vault, Concrete") ? num4 : (2.0 * (L - Wall) + 2.0 * (W - Wall))) * H + PartitionLength * H;
			if (WallOpeningArea > Math.Min(num8, num5 * H + PartitionLength * H))
			{
				throw new Exception("Wall openings exceed the measured wall area.");
			}
			if (PlasterFaces < 1 || PlasterFaces > 2)
			{
				throw new Exception("Select one interior plaster face or interior plus exterior faces.");
			}
			if (Kind == "Catch Basin (CHB)")
			{
				WaterproofFaces = 0;
				WaterproofFloor = false;
			}
			else if (WaterproofFaces < 1 || WaterproofFaces > 2)
			{
				throw new Exception("Select one or two waterproofed wall faces.");
			}
			Excavation = (L + 2.0 * Allowance) * (W + 2.0 * Allowance) * (H + Base + Cover + Gravel + SoilCover) * (double)Count;
			GravelVolume = L * W * Gravel * (double)Count;
			Backfill = Math.Max(0.0, Excavation - L * W * (H + Base + Cover) * (double)Count - GravelVolume);
			double num9 = ((Kind == "Septic Vault, Concrete") ? ((L * W - num2 * num3) * H + PartitionLength * PartitionThickness * H - WallOpeningArea * Wall) : 0.0);
			BaseConcrete = L * W * Base * (double)Count;
			CoverConcrete = (L * W - num7) * Cover * (double)Count;
			WallConcrete = num9 * (double)Count;
			AdditionalConcrete = OtherConcrete * (double)Count;
			ConcreteVolume = BaseConcrete + CoverConcrete + WallConcrete + AdditionalConcrete;
			double num10 = ((Kind == "Septic Vault, Concrete") ? (num4 * H + num5 * H + 2.0 * PartitionLength * H - 2.0 * WallOpeningArea) : 0.0);
			// Cover soffit is formed only over the clear chamber area (the cover bears on the walls and partitions), less the hatch opening.
			double num11 = ((Cover > 0.0) ? (Math.Max(0.0, num6 - PartitionLength * PartitionThickness - num7) + (num4 + ((num7 > 0.0) ? (2.0 * (OpeningL + OpeningW)) : 0.0)) * Cover) : 0.0);
			WallForms = num10 * (double)Count;
			BaseForms = num4 * Base * (double)Count;
			CoverForms = num11 * (double)Count;
			AdditionalForms = OtherForms * (double)Count;
			FormArea = WallForms + BaseForms + CoverForms + AdditionalForms;
			ChbArea = ((Kind == "Septic Vault, Concrete") ? 0.0 : ((num8 - WallOpeningArea) * (double)Count));
			double num12 = num5 * H + 2.0 * PartitionLength * H - WallOpeningArea;
			double num13 = num4 * H - WallOpeningArea;
			PlasterArea = (num12 + ((PlasterFaces == 2) ? num13 : 0.0) + (PlasterFloor ? num6 : 0.0)) * (double)Count;
			WaterproofArea = ((WaterproofFaces > 0) ? (num12 + ((WaterproofFaces == 2) ? num13 : 0.0) + (WaterproofFloor ? num6 : 0.0)) : 0.0) * (double)Count;
		}

		private void CalculateCanal()
		{
			if (W <= 2.0 * Wall)
			{
				throw new Exception("Canal outside width must exceed both lining walls.");
			}
			if (Cover != 0.0 || OpeningL != 0.0 || OpeningW != 0.0 || PartitionLength != 0.0 || PartitionThickness != 0.0 || SoilCover != 0.0)
			{
				throw new Exception("Open lined canals do not use a chamber cover, hatch or partition. Enter zero for these fields.");
			}
			if (PlasterFaces < 0 || PlasterFaces > 2 || WaterproofFaces < 0 || WaterproofFaces > 2)
			{
				throw new Exception("Select 0, 1 inside, or 2 inside/outside finish faces.");
			}
			double num = 2.0 * L * H;
			if (WallOpeningArea > num)
			{
				throw new Exception("Wall openings exceed the two canal wall faces.");
			}
			LsQuantity = L * (double)Count;
			Excavation = (L + 2.0 * Allowance) * (W + 2.0 * Allowance) * (H + Base + Gravel + SoilCover) * (double)Count;
			GravelVolume = L * W * Gravel * (double)Count;
			Backfill = Math.Max(0.0, Excavation - L * W * (H + Base) * (double)Count - GravelVolume);
			BaseConcrete = L * W * Base * (double)Count;
			WallConcrete = (2.0 * L * Wall * H - WallOpeningArea * Wall) * (double)Count;
			CoverConcrete = 0.0;
			AdditionalConcrete = OtherConcrete * (double)Count;
			ConcreteVolume = BaseConcrete + WallConcrete + AdditionalConcrete;
			WallForms = (4.0 * L * H + 4.0 * Wall * H - 2.0 * WallOpeningArea) * (double)Count;
			BaseForms = 2.0 * (L + W) * Base * (double)Count;
			CoverForms = 0.0;
			AdditionalForms = OtherForms * (double)Count;
			FormArea = WallForms + BaseForms + AdditionalForms;
			ChbArea = 0.0;
			double num2 = L * (W - 2.0 * Wall);
			PlasterArea = (((PlasterFaces > 0) ? (num - WallOpeningArea) : 0.0) + ((PlasterFaces == 2) ? (num - WallOpeningArea) : 0.0) + (PlasterFloor ? num2 : 0.0)) * (double)Count;
			WaterproofArea = (((WaterproofFaces > 0) ? (num - WallOpeningArea) : 0.0) + ((WaterproofFaces == 2) ? (num - WallOpeningArea) : 0.0) + (WaterproofFloor ? num2 : 0.0)) * (double)Count;
		}

		public List<BillQuantity> Materials()
		{
			Func<string, string, double, string, string, BillQuantity> func = (string code, string name, double q, string unit, string formula) => new BillQuantity
			{
				Section = code,
				Description = code + " - " + name,
				Quantity = q,
				Unit = unit,
				Basis = true,
				Computation = formula
			};
			string text = string.Format(CultureInfo.InvariantCulture, "L={0}, W={1}, clear H={2}, wall={3}, base={4}, cover={5}, gravel={6}, working allowance={7} m; count={8}", L, W, H, Wall, Base, Cover, Gravel, Allowance, Count);
			List<BillQuantity> list = new List<BillQuantity>
			{
				func("803(1)a", "Structure Excavation (Common Soil)", Excavation, "m3", "(L+2a)*(W+2a)*(H+base+cover+gravel+soil cover)*count; " + text),
				func("804(1)a", "Embankment from Structure Excavation", Backfill, "m3", "Geometric backfill = excavation - outside chamber envelope - gravel; compacted space, before project soil supply conversion"),
				func("804(7)", "Gravel Fill", GravelVolume, "m3", "L*W*gravel thickness*count; " + text),
				func("900(1)i", "Structural Concrete (Class A, 28 days)", ConcreteVolume, "m3", "Base + cover less access opening + RC walls/partitions (concrete type only) + entered additional RC volume; " + text),
				func("902(1)a1", "Reinforcing Steel (Deformed), Grade 40", RebarPurchase, "kg", "Developed cut lengths x bars/structure x structure count x unit mass, plus allocated commercial-stock offcuts; approved cut schedule below"),
				func("903(2)", "Formworks and Falseworks", FormArea, "m2", "RC wall faces (concrete type only) + base edges + cover soffit/edges and access-opening edges; entered additional RC requires its separately entered form area")
			};
			if (ComponentMode)
			{
				if (MemberCHB100 > 0.0 || Kind == "Catch Basin (CHB)")
				{
					list.Add(func("1046(2)a1", "100mm thk. CHB Non-Load Bearing (including Reinforcing Steel)", MemberCHB100, "m2", "Sum of 100mm CHB member length x height x member count x structure count"));
				}
				if (MemberCHB150 > 0.0 || Kind == "Septic Vault, CHB")
				{
					list.Add(func("1046(2)a2", "150mm thk. CHB Non-Load Bearing (including Reinforcing Steel)", MemberCHB150, "m2", "Sum of 150mm CHB member length x height x member count x structure count"));
				}
			}
			else if (Kind == "Septic Vault, CHB" || Kind == "Catch Basin (CHB)")
			{
				list.Add(func((Kind == "Catch Basin (CHB)") ? "1046(2)a1" : "1046(2)a2", (Kind == "Catch Basin (CHB)") ? "100mm thk. CHB Non-Load Bearing (including Reinforcing Steel)" : "150mm thk. CHB Non-Load Bearing (including Reinforcing Steel)", ChbArea, "m2", "Centreline perimeter*clear wall height + clear partition length*height - wall openings; x structure count"));
			}
			list.Add(func("1027(1)", "Cement Plaster Finish", PlasterArea, "m2", "Selected interior/exterior wall faces + both partition faces, less wall openings; floor only if selected; x count"));
			if (Kind != "Catch Basin (CHB)" && (Kind != "Lined Canal" || WaterproofArea > 0.0))
			{
				list.Add(func("1016(1)b", "Liquid Waterproofing", WaterproofArea, "m2", "Selected wall faces + partitions, less wall openings, plus floor if selected; x count"));
			}
			if (ComponentMode)
			{
				foreach (BillQuantity item in list.Where((BillQuantity r) => r.Section != "803(1)a" && r.Section != "804(1)a"))
				{
					item.Computation = "Sum of entered slab, slab-on-fill, RC wall and CHB members (including structure count); selected finish faces and member reinforcement; additional entered RC/forms are included once.";
				}
			}
			else if (Kind == "Lined Canal")
			{
				list.Single((BillQuantity r) => r.Section == "900(1)i").Computation = "Open U-channel: L*outside W*base + 2*L*wall t*clear H - wall openings*wall t + additional RC; x count. No end walls or cover.";
				list.Single((BillQuantity r) => r.Section == "903(2)").Computation = "Both faces of two longitudinal walls + four exposed wall ends + slab edges + additional forms; x count.";
				list.Single((BillQuantity r) => r.Section == "1027(1)").Computation = "Selected longitudinal wall faces and optional clear floor: L*(outside W-2 wall t); x count.";
			}
			return list;
		}
	}
}
