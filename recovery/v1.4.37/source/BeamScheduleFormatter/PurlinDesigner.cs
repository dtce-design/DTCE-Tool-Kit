using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace BeamScheduleFormatter
{
	internal static class PurlinDesigner
	{
		public static PurlinDesignResult Calculate(PurlinDesignInput input)
		{
			input.Validate();
			PurlinProperties p = input.Properties;
			double num = input.Angle * Math.PI / 180.0;
			PurlinDesignResult r = new PurlinDesignResult();
			double num2 = input.Dead * input.Spacing + input.Profile.KgM * 9.81 / 1000.0;
			r.Dx = num2 * Math.Cos(num);
			r.Dy = num2 * Math.Sin(num);
			r.Lx = input.Live * input.Spacing * Math.Cos(num);
			r.Ly = input.Live * input.Spacing * Math.Sin(num);
			double num3 = input.WindPressure * input.Spacing;
			double num4 = input.WindSuction * input.Spacing;
			bool flag = input.Support == "Cantilever";
			r.Cx = (flag ? 0.5 : 0.125);
			r.Cy = ((!flag) ? new double[3]
			{
				0.125,
				1.0 / 32.0,
				1.0 / 90.0
			} : new double[3] { 0.5, 0.028544, 0.014288 })[input.SagRods];
			r.Fbx = ((input.Profile.B / input.Profile.T < 0.38 * Math.Sqrt(input.E / input.Fy)) ? 0.66 : 0.6) * input.Fy;
			r.Fby = (((input.Profile.H - 2.0 * input.Profile.T) / input.Profile.T < 3.76 * Math.Sqrt(input.E / input.Fy)) ? 0.66 : 0.6) * input.Fy;
			Action<string, double, double, bool> obj = delegate(string name, double x, double y, bool flag2)
			{
				double mx2 = r.Cx * x * input.Span * input.Span;
				double my2 = r.Cy * y * input.Span * input.Span;
				double num9 = ((flag2 && input.AlternateIncrease) ? 1.3333333333333333 : 1.0);
				r.Cases.Add(new PurlinLoadCase
				{
					Name = name,
					Normal = x,
					Tangential = y,
					Mx = mx2,
					My = my2,
					Alternate = flag2,
					Ratio = Interaction(p, mx2, my2, r.Fbx * num9, r.Fby * num9)
				});
			};
			obj("D + L", r.Dx + r.Lx, r.Dy + r.Ly, false);
			obj("D + 0.6Wp", r.Dx + 0.6 * num3, r.Dy, false);
			obj("D + 0.6Ws", r.Dx + 0.6 * num4, r.Dy, false);
			obj("D + 0.75(L + 0.6Wp)", r.Dx + 0.75 * (r.Lx + 0.6 * num3), r.Dy + 0.75 * r.Ly, true);
			obj("D + 0.75(L + 0.6Ws)", r.Dx + 0.75 * (r.Lx + 0.6 * num4), r.Dy + 0.75 * r.Ly, true);
			obj("0.6D + 0.6Wp", 0.6 * r.Dx + 0.6 * num3, 0.6 * r.Dy, true);
			obj("0.6D + 0.6Ws", 0.6 * r.Dx + 0.6 * num4, 0.6 * r.Dy, true);
			bool[] array = new bool[2] { false, true };
			foreach (bool alternate in array)
			{
				List<PurlinLoadCase> source = r.Cases.Where((PurlinLoadCase c) => c.Alternate == alternate).ToList();
				double mx = source.Max((PurlinLoadCase c) => Math.Abs(c.Mx));
				double my = source.Max((PurlinLoadCase c) => Math.Abs(c.My));
				double num6 = ((alternate && input.AlternateIncrease) ? 1.3333333333333333 : 1.0);
				double num7 = Interaction(p, mx, my, r.Fbx * num6, r.Fby * num6);
				if (alternate)
				{
					r.Alternate = num7;
				}
				else
				{
					r.Basic = num7;
				}
			}
			double coefficient = (flag ? 0.125 : (5.0 / 384.0));
			r.LiveDeflection = Deflection(p, r.Lx, r.Ly, input.Span, input.E, coefficient);
			r.TotalDeflection = Deflection(p, r.Dx + r.Lx, r.Dy + r.Ly, input.Span, input.E, coefficient);
			int num8 = ((!(input.Ceiling == "Supporting plaster ceiling")) ? ((input.Ceiling == "Supporting nonplaster ceiling") ? 1 : 2) : 0);
			int[] array2 = new int[3] { 360, 240, 180 };
			int[] array3 = new int[3] { 240, 180, 120 };
			r.LiveLimit = input.Span * 1000.0 / (double)array2[num8];
			r.TotalLimit = input.Span * 1000.0 / (double)array3[num8];
			return r;
		}

		internal static double Interaction(PurlinProperties p, double mx, double my, double fbx, double fby)
		{
			if (Math.Abs(p.Ixy) < 1E-08)
			{
				return Math.Abs(mx) * 1000000.0 / p.Sx / fbx + Math.Abs(my) * 1000000.0 / p.Sy / fby;
			}
			double determinant = p.Ix * p.Iy - p.Ixy * p.Ixy;
			return p.Points.Max((double[] point) => Math.Abs(mx * 1000000.0 * ((0.0 - p.Ixy) * point[0] + p.Iy * point[1]) / determinant) / fbx + Math.Abs(my * 1000000.0 * (p.Ix * point[0] - p.Ixy * point[1]) / determinant) / fby);
		}

		internal static double Deflection(PurlinProperties p, double normal, double tangential, double span, double e, double coefficient)
		{
			double num = p.Ix * p.Iy - p.Ixy * p.Ixy;
			double num2 = coefficient * Math.Pow(span * 1000.0, 4.0) / e;
			double num3 = num2 * (p.Ix * tangential - p.Ixy * normal) / num;
			double num4 = num2 * (p.Iy * normal - p.Ixy * tangential) / num;
			return Math.Sqrt(num3 * num3 + num4 * num4);
		}

		public static string Report(PurlinDesignInput input, PurlinDesignResult r)
		{
			return DetailedDesignReports.Purlin(input, r);
		}

		internal static string ReferenceSummary(PurlinDesignInput input, PurlinDesignResult r)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("PURLIN DESIGN | WORKBOOK ASD FLEXURE & DEFLECTION");
			stringBuilder.AppendLine(input.Name + " | " + input.Profile.Name + " | " + input.Support);
			stringBuilder.AppendLine(input.Profile.Source);
			stringBuilder.AppendLine("METHOD SCOPE: workbook gross-section checks. Effective-width, local/distortional/lateral buckling, shear, web crippling, connections and restraint capacity are not checked here.");
			stringBuilder.AppendLine("Section properties: " + (input.Properties.Verified ? "user-supplied verified inputs" : "calculated nonoverlapping rectangular plates; bends/radii neglected, NOT manufacturer-certified"));
			stringBuilder.AppendLine("Overall member adequacy requires the remaining cold-formed steel checks.");
			stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "L={0:0.###}m; spacing={1:0.###}m; angle={2:0.###}deg; sag rods={3}; Fy={4:0.###}MPa; E={5:0.###}MPa", input.Span, input.Spacing, input.Angle, input.SagRods, input.Fy, input.E));
			stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "Area={0:0.###}mm2; Ix={1:0.###}mm4; Iy={2:0.###}mm4; Ixy={3:0.###}mm4; Sx={4:0.###}mm3; Sy={5:0.###}mm3", input.Properties.Area, input.Properties.Ix, input.Properties.Iy, input.Properties.Ixy, input.Properties.Sx, input.Properties.Sy));
			stringBuilder.AppendLine("Mass from handbook: " + input.Profile.KgM.ToString("0.###", CultureInfo.InvariantCulture) + "kg/m; self-weight=mass x 9.81 / 1000 kN/m.");
			stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "Loads kPa: superimposed D={0:0.###}, L={1:0.###}, Wp={2:0.###}, Ws={3:0.###}. Wind is signed normal pressure; suction normally negative.", input.Dead, input.Live, input.WindPressure, input.WindSuction));
			stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "D normal/tangential={0:0.######}/{1:0.######} kN/m; L={2:0.######}/{3:0.######} kN/m", r.Dx, r.Dy, r.Lx, r.Ly));
			stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "Workbook Fbx={0:0.###}MPa; Fby={1:0.###}MPa; Cx={2:0.######}; Cy={3:0.######}", r.Fbx, r.Fby, r.Cx, r.Cy));
			stringBuilder.AppendLine("LOAD COMBINATIONS | kN/m, kN.m, ratio");
			foreach (PurlinLoadCase @case in r.Cases)
			{
				stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,-26} Wx={1,10:0.######} Wy={2,10:0.######} Mx={3,10:0.######} My={4,10:0.######} interaction={5:0.######}", @case.Name, @case.Normal, @case.Tangential, @case.Mx, @case.My, @case.Ratio));
			}
			stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "Independent component envelopes (conservative): basic={0:0.######}; alternate={1:0.######}; limit <=1", r.Basic, r.Alternate));
			stringBuilder.AppendLine("Alternate allowable stress increase: " + (input.AlternateIncrease ? "4/3, as in supplied workbook" : "disabled"));
			stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "Live deflection={0:0.###}mm / limit {1:0.###}mm; D+L deflection={2:0.###}mm / limit {3:0.###}mm", r.LiveDeflection, r.LiveLimit, r.TotalDeflection, r.TotalLimit));
			stringBuilder.AppendLine(r.WithinWorkbookChecks ? "LISTED WORKBOOK CHECKS SATISFIED; additional member checks remain." : "LISTED WORKBOOK CHECKS EXCEEDED.");
			stringBuilder.AppendLine("SOURCE / ADJUSTMENTS");
			stringBuilder.AppendLine("PS Design of Purlin.xlsm: Y101:Y155 (ASD combinations, allowable stresses, interaction and ceiling limits). No macros or license/project metadata are included.");
			stringBuilder.AppendLine("Normal-axis Cx uses 1/8 for simply supported and 1/2 for cantilever. Sag rods do not reduce the normal-axis span (workbook 2-rod 1/9 is not used). Tangential coefficients follow the supplied workbook and assume effective sag-rod restraint.");
			stringBuilder.AppendLine("Deflection uses full-span 5wL^4/(384EI) for simply supported or wL^4/(8EI) for cantilever; workbook cantilever 5/384 is corrected. No sag-rod deflection reduction. Nonzero Ixy uses coupled unsymmetric bending for Z sections.");
			stringBuilder.AppendLine("Regan 2026 handbook lists dimensions/mass only. Calculated properties do not reproduce the workbook's inconsistent centroid denominator; verified properties may be entered separately.");
			stringBuilder.AppendLine("Cantilever reference: NASA Mechanical Design Reliability Monograph, Eq.1.19, https://extapps.ksc.nasa.gov/Reliability/Documents/Mechanical-Design-Reliability-Monograph.pdf");
			return stringBuilder.ToString();
		}
	}
}
