using System;
using System.Globalization;

namespace BeamScheduleFormatter
{
	internal class EstimateOptions
	{
		public LengthConfiguration LengthConfig;

		public bool FixedCurtailment = true;

		public double Left = 0.25;

		public double Right = 0.25;

		public double Cover = 40.0;

		public double Hook = 6.0;

		public double Extra;

		public double Waste;

		public double Offset = 50.0;

		public double Fc = 21.0;

		public int GradeSmall = 40;

		public int Grade = 60;

		public int MainHook;

		public int Anchorage;

		public double BondRatio = 1.0;

		public int GradeFor(int dia)
		{
			if (dia <= 12)
			{
				return GradeSmall;
			}
			if (dia >= 16)
			{
				return Grade;
			}
			throw new Exception("13-15mm bars need a project-specific grade assignment.");
		}

		public void Validate()
		{
			if (Left <= 0.0 || Right <= 0.0 || Left + Right >= 1.0)
			{
				throw new Exception("Left + right zone percentages must be below 100%.");
			}
			if (Cover < 0.0 || Hook < 6.0 || Extra < 0.0 || Waste < 0.0 || Offset < 0.0)
			{
				throw new Exception("Allowances cannot be negative; seismic tail factor must be at least 6.");
			}
			bool num;
			if (LengthConfig != null)
			{
				if (Fc < 17.0)
				{
					goto IL_010f;
				}
				num = Fc > 70.0;
			}
			else
			{
				if (Fc == 21.0 || Fc == 28.0)
				{
					goto IL_011a;
				}
				num = Fc != 35.0;
			}
			if (num)
			{
				goto IL_010f;
			}
			goto IL_011a;
			IL_010f:
			throw new Exception("Select concrete strength 21, 28 or 35 MPa.");
			IL_011a:
			if ((Grade != 40 && Grade != 60) || (GradeSmall != 40 && GradeSmall != 60))
			{
				throw new Exception("Select Grade 40 or 60 steel.");
			}
			if (BondRatio <= 0.0 || BondRatio > 2.5)
			{
				throw new Exception("Bond ratio must be >0 and <=2.5.");
			}
			if ((Anchorage == 1 || Anchorage == 2) && MainHook != 0)
			{
				throw new Exception("Straight Ld/lap allowances cannot be combined with hooked ends.");
			}
			if (Anchorage == 3 && (MainHook == 0 || LengthConfig == null))
			{
				throw new Exception("Hooked Ldh allowance requires a saved calculation configuration and 90/180deg main hooks.");
			}
		}

		public string Description()
		{
			return string.Format(CultureInfo.InvariantCulture, "NSCP 2015 / ACI 318-14 basis; 16mm+ Grade {8}; main beam ends: {9}. Zones L/M/R: {0:0.##}% / {1:0.##}% / {2:0.##}%; cover {3:0.##}mm; seismic tie tails max({4:0.##}d,75mm); extra {5:0.###}m per actual main-bar piece; waste {6:0.##}%; first/last tie offset {7:0.##}mm.", Left * 100.0, (1.0 - Left - Right) * 100.0, Right * 100.0, Cover, Hook, Extra, Waste, Offset, Grade, (MainHook == 0) ? "straight" : (MainHook + "deg hooks at both ends")) + " 12mm and below Grade " + GradeSmall + ". Concrete f'c=" + Fc + "MPa; (cb+Ktr)/db=" + BondRatio.ToString("0.##", CultureInfo.InvariantCulture) + "; main-piece allowance=" + ((Anchorage == 0) ? "none" : ((Anchorage == 1) ? "2 x straight Ld" : "1 x Class B lap (1.3Ld)")) + ".";
		}
	}
}
