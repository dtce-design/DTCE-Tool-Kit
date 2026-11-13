using System;
using System.Globalization;
using System.Text;

namespace BeamScheduleFormatter
{
	internal static class RebarLengths
	{
		public static LengthResult Calculate(LengthInput i)
		{
			double[] array = new double[10] { i.Db, i.Fy, i.Fc, i.Cover, i.Spacing, i.Ktr, i.AsRatio, i.Spliced, i.Round, i.Straight };
			foreach (double d in array)
			{
				if (double.IsNaN(d) || double.IsInfinity(d))
				{
					throw new Exception("All inputs must be finite numbers.");
				}
			}
			if (i.Db < 10.0 || i.Db > 36.0 || i.Fy < 200.0 || i.Fy > 420.0 || i.Fc < 17.0 || i.Fc > 70.0 || i.Cover < 0.0 || i.Spacing <= 0.0 || i.Ktr < 0.0 || i.AsRatio <= 0.0 || i.Spliced <= 0.0 || i.Spliced > 100.0 || i.Round < 1.0 || i.Straight < 0.0)
			{
				throw new Exception("Check input ranges: db10-36mm,fy200-420MPa,fc17-70MPa; positive spacing, splice percent and rounding.");
			}
			LengthResult lengthResult = new LengthResult();
			lengthResult.Lambda = (i.Lightweight ? 0.75 : 1.0);
			lengthResult.PsiT = (i.Top ? 1.3 : 1.0);
			lengthResult.PsiE = ((!i.Epoxy) ? 1.0 : ((i.Cover < 3.0 * i.Db || i.Spacing < 6.0 * i.Db) ? 1.5 : 1.2));
			lengthResult.PsiS = ((i.Db <= 19.0) ? 0.8 : 1.0);
			double num = Math.Min(i.Cover + i.Db / 2.0, (i.Spacing + i.Db) / 2.0);
			double num2 = Math.Min(Math.Sqrt(i.Fc), 8.3);
			lengthResult.BondRatio = Math.Min((num + i.Ktr) / i.Db, 2.5);
			if (lengthResult.BondRatio <= 0.0)
			{
				throw new Exception("Invalid bond ratio.");
			}
			if (i.Simplified && i.Db > 20.0 && i.Db < 25.0)
			{
				throw new Exception("The sample method uses PNS sizes <=20mm or >=25mm.");
			}
			lengthResult.LdRaw = (i.Simplified ? (i.Fy * Math.Min(lengthResult.PsiT * lengthResult.PsiE, 1.7) * i.Db / (((i.Db <= 20.0) ? 1.4 : 1.1) * lengthResult.Lambda * num2)) : (i.Fy / (1.1 * lengthResult.Lambda * num2) * Math.Min(lengthResult.PsiT * lengthResult.PsiE, 1.7) * lengthResult.PsiS / lengthResult.BondRatio * i.Db));
			lengthResult.Ld = Math.Max(300.0, lengthResult.LdRaw);
			lengthResult.LapClass = ((i.AsRatio >= 2.0 && i.Spliced <= 50.0) ? "A" : "B");
			lengthResult.LapA = Math.Max(300.0, lengthResult.LdRaw);
			lengthResult.LapB = Math.Max(300.0, 1.3 * lengthResult.LdRaw);
			lengthResult.Lap = ((lengthResult.LapClass == "A") ? lengthResult.LapA : lengthResult.LapB);
			lengthResult.Ldh = Math.Max(Math.Max((i.Simplified ? (100.0 / 417.0) : 0.24) * i.Fy * (i.Epoxy ? 1.2 : 1.0) / (lengthResult.Lambda * num2) * i.Db, 8.0 * i.Db), 150.0);
			lengthResult.Ldc = Math.Max(Math.Max(0.24 * i.Fy / (lengthResult.Lambda * num2) * i.Db, 0.043 * i.Fy * i.Db), 200.0);
			lengthResult.CompressionLap = Math.Max(0.071 * i.Fy * i.Db, 300.0) * ((i.Fc < 21.0) ? 1.3333333333333333 : 1.0);
			lengthResult.MainInside = HookGeometry.Inside((int)i.Db, false);
			lengthResult.TieInside = ((i.Db <= 25.0) ? HookGeometry.Inside((int)i.Db, true) : 0.0);
			lengthResult.Tail90 = 12.0 * i.Db;
			lengthResult.Tail180 = Math.Max(4.0 * i.Db, 65.0);
			lengthResult.Tail135 = Math.Max(6.0 * i.Db, 75.0);
			double num3 = (lengthResult.MainInside + i.Db) / 2.0;
			lengthResult.Cut90 = i.Straight + Math.PI / 2.0 * num3 + lengthResult.Tail90;
			lengthResult.Cut180 = i.Straight + Math.PI * num3 + lengthResult.Tail180;
			lengthResult.Cut135 = ((lengthResult.TieInside == 0.0) ? 0.0 : (i.Straight + Math.PI * 3.0 / 4.0 * ((lengthResult.TieInside + i.Db) / 2.0) + lengthResult.Tail135));
			return lengthResult;
		}

		public static double Up(double value, double step)
		{
			return Math.Ceiling(value / step - 1E-10) * step;
		}

		public static string Report(LengthInput i)
		{
			LengthResult lengthResult = Calculate(i);
			StringBuilder b = new StringBuilder();
			Func<double, string> n = (double x) => x.ToString("0.00", CultureInfo.InvariantCulture);
			Func<double, string> adopt = (double x) => n(Up(x, i.Round));
			double num = Math.Min(Math.Sqrt(i.Fc), 8.3);
			double num2 = Math.Min(i.Cover + i.Db / 2.0, (i.Spacing + i.Db) / 2.0);
			double arg = Math.Min(lengthResult.PsiT * lengthResult.PsiE, 1.7);
			double arg2 = ((i.Db <= 20.0) ? 1.4 : 1.1);
			b.AppendLine("SPLICE, DEVELOPMENT & HOOKS - COMPLETE CALCULATION");
			b.AppendLine("Selected db=" + n(i.Db) + "mm | " + (i.Top ? "TOP" : "OTHER") + " bar | fy=" + n(i.Fy) + "MPa | fc=" + n(i.Fc) + "MPa");
			b.AppendLine("## 01  INPUTS & MODIFICATION FACTORS");
			b.AppendLine("Method: " + (i.Simplified ? "NSCP425.4.2.2 simplified OTHER CASES (supplied sample)" : "ACI318M-14 general bond-ratio equation"));
			b.AppendLine("Clear cover TO BAR=" + n(i.Cover) + "mm; clear spacing=" + n(i.Spacing) + "mm; Ktr=" + n(i.Ktr) + "mm.");
			b.AppendLine("lambda=" + n(lengthResult.Lambda) + "; psi_t=" + n(lengthResult.PsiT) + "; psi_e=" + n(lengthResult.PsiE) + "; psi_s=" + n(lengthResult.PsiS) + ".");
			b.AppendLine("psi_t*psi_e = " + n(lengthResult.PsiT * lengthResult.PsiE) + "; cap1.70 -> USE " + n(arg) + ".");
			b.AppendLine("sqrt(fc)=" + n(Math.Sqrt(i.Fc)) + "; cap8.30 -> USE " + n(num) + ".");
			b.AppendLine("cb=min(cover+db/2,(spacing+db)/2)=min(" + n(i.Cover + i.Db / 2.0) + "," + n((i.Spacing + i.Db) / 2.0) + ")=" + n(num2) + "mm.");
			b.AppendLine("Bond ratio=min((cb+Ktr)/db,2.50)=min(" + n((num2 + i.Ktr) / i.Db) + ",2.50)=" + n(lengthResult.BondRatio) + ".");
			b.AppendLine("Bond ratio and psi_s enter the GENERAL method; simplified method uses k=" + n(arg2) + ".");
			b.AppendLine();
			b.AppendLine("## 02  TENSION DEVELOPMENT Ld");
			if (i.Simplified)
			{
				b.AppendLine("Ld raw=fy*capped(psi_t*psi_e)*db/(k*lambda*sqrt(fc)).");
				b.AppendLine("      =" + n(i.Fy) + "*" + n(arg) + "*" + n(i.Db) + "/(" + n(arg2) + "*" + n(lengthResult.Lambda) + "*" + n(num) + ")=" + n(lengthResult.LdRaw) + "mm.");
			}
			else
			{
				b.AppendLine("Ld raw=fy*capped(psi_t*psi_e)*psi_s*db/(1.10*lambda*sqrt(fc)*bond ratio).");
				b.AppendLine("      =" + n(i.Fy) + "*" + n(arg) + "*" + n(lengthResult.PsiS) + "*" + n(i.Db) + "/(1.10*" + n(lengthResult.Lambda) + "*" + n(num) + "*" + n(lengthResult.BondRatio) + ")=" + n(lengthResult.LdRaw) + "mm.");
			}
			b.AppendLine("Ld=max(raw,300)=max(" + n(lengthResult.LdRaw) + ",300.00)=" + n(lengthResult.Ld) + " -> USE " + adopt(lengthResult.Ld) + "mm.");
			b.AppendLine();
			b.AppendLine("## 03  TENSION LAP SPLICES - CLASS A / B");
			b.AppendLine("As provided/required=" + n(i.AsRatio) + "; bars spliced=" + n(i.Spliced) + "%.");
			b.AppendLine("Class A requires As ratio>=2.00 AND <=50.00% bars spliced; otherwise Class B.");
			b.AppendLine("Lap A=max(1.00*RAW Ld,300)=max(" + n(lengthResult.LdRaw) + ",300.00)=" + n(lengthResult.LapA) + " -> USE " + adopt(lengthResult.LapA) + "mm.");
			b.AppendLine("Lap B=max(1.30*RAW Ld,300)=max(" + n(1.3 * lengthResult.LdRaw) + ",300.00)=" + n(lengthResult.LapB) + " -> USE " + adopt(lengthResult.LapB) + "mm.");
			b.AppendLine("GOVERNING: CLASS " + lengthResult.LapClass + " -> " + adopt(lengthResult.Lap) + "mm. Round only after the raw-length check.");
			LengthResult lengthResult2 = Calculate(new LengthInput
			{
				Db = i.Db,
				Fy = i.Fy,
				Fc = i.Fc,
				Cover = i.Cover,
				Spacing = i.Spacing,
				Ktr = i.Ktr,
				AsRatio = i.AsRatio,
				Spliced = i.Spliced,
				Round = i.Round,
				Top = !i.Top,
				Epoxy = i.Epoxy,
				Lightweight = i.Lightweight,
				Simplified = i.Simplified
			});
			b.AppendLine((i.Top ? "OTHER" : "TOP") + "-position check: rawLd=" + n(lengthResult2.LdRaw) + "; USE Ld=" + adopt(lengthResult2.Ld) + ", LapA=" + adopt(lengthResult2.LapA) + ", LapB=" + adopt(lengthResult2.LapB) + "mm.");
			b.AppendLine();
			b.AppendLine("## 04  COMPRESSION DEVELOPMENT & SPLICE");
			double arg3 = 0.24 * i.Fy / (lengthResult.Lambda * num) * i.Db;
			double arg4 = 0.043 * i.Fy * i.Db;
			double arg5 = 0.071 * i.Fy * i.Db;
			double arg6 = ((i.Fc < 21.0) ? 1.3333333333333333 : 1.0);
			b.AppendLine("Ldc=max(0.24*fy*db/(lambda*sqrt(fc)),0.043*fy*db,200).");
			b.AppendLine("   =max(" + n(arg3) + "," + n(arg4) + ",200.00)=" + n(lengthResult.Ldc) + " -> USE " + adopt(lengthResult.Ldc) + "mm.");
			b.AppendLine("Compression lap=max(0.071*fy*db,300)*low-fc factor; valid here for fy<=420MPa.");
			b.AppendLine("   =max(0.071*" + n(i.Fy) + "*" + n(i.Db) + ",300.00)*" + n(arg6));
			b.AppendLine("   =max(" + n(arg5) + ",300.00)*" + n(arg6) + "=" + n(lengthResult.CompressionLap) + " -> USE " + adopt(lengthResult.CompressionLap) + "mm.");
			b.AppendLine("Low-fc factor=4/3 when fc<21MPa, otherwise1.00.");
			b.AppendLine();
			b.AppendLine("All calculated lengths are mm. USE values round UP to " + n(i.Round) + "mm; display2decimals.");
			b.AppendLine("Full precision is retained internally; displayed substitutions are rounded for readability.");
			b.AppendLine("\f");
			b.AppendLine("HOOK DEVELOPMENT, STANDARD GEOMETRY & CUTTING LENGTH");
			b.AppendLine("db=" + n(i.Db) + "mm | fy=" + n(i.Fy) + "MPa | fc=" + n(i.Fc) + "MPa | " + (i.Epoxy ? "EPOXY" : "UNCOATED"));
			b.AppendLine("## 05  HOOKED TENSION DEVELOPMENT Ldh");
			double num3 = (i.Simplified ? (100.0 / 417.0) : 0.24);
			double num4 = (i.Epoxy ? 1.2 : 1.0);
			double arg7 = num3 * i.Fy * num4 / (lengthResult.Lambda * num) * i.Db;
			b.AppendLine("Ldh raw=" + (i.Simplified ? "(1/4.17)" : "0.24") + "*fy*hook epoxy factor*db/(lambda*sqrt(fc)).");
			b.AppendLine("       =" + (i.Simplified ? "(1/4.17)" : "0.24") + "*" + n(i.Fy) + "*" + n(num4) + "*" + n(i.Db) + "/(" + n(lengthResult.Lambda) + "*" + n(num) + ")=" + n(arg7) + "mm.");
			b.AppendLine("Ldh=max(raw,8db,150)=max(" + n(arg7) + "," + n(8.0 * i.Db) + ",150.00)=" + n(lengthResult.Ldh) + "mm.");
			b.AppendLine("ADOPT Ldh=" + adopt(lengthResult.Ldh) + "mm. No cover/confinement reduction applied.");
			b.AppendLine("Ldh is embedment; it is not hook tail length or centerline cutting length.");
			b.AppendLine();
			b.AppendLine("## 06  MINIMUM BENDS & HOOK TAILS");
			b.AppendLine("Main inside bend ID=" + ((i.Db <= 25.0) ? "6db" : "8db") + "=" + n(lengthResult.MainInside) + "mm.");
			b.AppendLine("Main centerline radius R=(ID+db)/2=(" + n(lengthResult.MainInside) + "+" + n(i.Db) + ")/2=" + n((lengthResult.MainInside + i.Db) / 2.0) + "mm.");
			b.AppendLine("90deg main tail=12db=12*" + n(i.Db) + "=" + n(lengthResult.Tail90) + "mm.");
			b.AppendLine("180deg main tail=max(4db,65)=max(" + n(4.0 * i.Db) + ",65.00)=" + n(lengthResult.Tail180) + "mm.");
			b.AppendLine("135deg seismic tie tail=max(6db,75)=max(" + n(6.0 * i.Db) + ",75.00)=" + n(lengthResult.Tail135) + "mm.");
			b.AppendLine("Entered straight TANGENT leg S=" + n(i.Straight) + "mm (one hook only).");
			b.AppendLine();
			b.AppendLine("## 07  CENTERLINE CUT = STRAIGHT + ARC + TAIL");
			Action<string, double, double, double> action = delegate(string name, double angle, double radius, double tail)
			{
				double num9 = angle * Math.PI / 180.0 * radius;
				double arg9 = i.Straight + num9 + tail;
				b.AppendLine(name + ": arc=angle*pi*R/180=" + n(angle) + "*pi*" + n(radius) + "/180=" + n(num9) + "mm.");
				b.AppendLine("  Cut=" + n(i.Straight) + "+" + n(num9) + "+" + n(tail) + "=" + n(arg9) + " -> USE " + adopt(arg9) + "mm.");
			};
			double arg8 = (lengthResult.MainInside + i.Db) / 2.0;
			action("90deg MAIN", 90.0, arg8, lengthResult.Tail90);
			action("180deg MAIN", 180.0, arg8, lengthResult.Tail180);
			if (lengthResult.TieInside > 0.0)
			{
				double num5 = (lengthResult.TieInside + i.Db) / 2.0;
				b.AppendLine("Tie inside bend ID=" + n(lengthResult.TieInside) + "mm; tie centerline R=" + n(num5) + "mm.");
				action("135deg SEISMIC TIE", 135.0, num5, lengthResult.Tail135);
				b.AppendLine();
				b.AppendLine("## 08  ADDITIONAL-LEG90/130 CUSTOM DETAIL (ESTIMATOR)");
				double num6 = ((i.Db <= 16.0) ? Math.Max(6.0 * i.Db, 75.0) : (12.0 * i.Db));
				double num7 = Math.PI / 2.0 * num5;
				double num8 = Math.PI * 13.0 / 18.0 * num5;
				b.AppendLine("Requested crosstie: one90deg + oneCUSTOM130deg hook, using the tie bend radius.");
				b.AppendLine("90deg arc=" + n(num7) + "mm; tail=" + n(num6) + "mm. 130deg arc=" + n(num8) + "mm; tail=" + n(lengthResult.Tail135) + "mm.");
				b.AppendLine("Cut/body relation = depth centerline -2R + arc90 + arc130 + tail90 + tail130.");
				b.AppendLine("                 = depth centerline + " + n(-2.0 * num5 + num7 + num8 + num6 + lengthResult.Tail135) + "mm.");
				b.AppendLine("Beam depth comes from DXF in Estimator.130deg is a custom project detail, not the standard135deg hook.");
			}
			else
			{
				b.AppendLine("Tie geometry above25mm is outside the supported standard tie-bend model; project detail required.");
			}
			b.AppendLine();
			b.AppendLine("## 09  SCOPE & REFERENCES");
			b.AppendLine("Selected NSCP2015 / ACI318M-14 Ch25 basis: hooks25.3; tension25.4.2; hooked development25.4.3;");
			b.AppendLine("compression25.4.9; tension laps25.5.2; compression laps25.5.5. Simplified mode follows supplied sample.");
			b.AppendLine("Same-size individual bars, db10-36mm, fy200-420MPa. No bundled-bar, excess-steel or confinement reduction.");
			b.AppendLine("Verify actual splice locations, seismic restrictions and support geometry. Estimator excludes entered S.");
			b.AppendLine("Calculations retain full precision; outputs display2decimals and design lengths round UP to " + n(i.Round) + "mm.");
			return b.ToString();
		}
	}
}
