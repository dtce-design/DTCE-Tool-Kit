using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace BeamScheduleFormatter
{
	internal static class DetailedDesignReports
	{
		private static string N(double value)
		{
			return value.ToString("0.######", CultureInfo.InvariantCulture);
		}

		private static void Line(StringBuilder b, string label, string formula)
		{
			b.AppendLine(label);
			b.AppendLine("  " + formula);
		}

		private static string Status(bool pass)
		{
			if (!pass)
			{
				return "REVIEW / EXCEEDED";
			}
			return "PASS (listed check)";
		}

		private static void Heading(StringBuilder b, string title)
		{
			b.AppendLine();
			b.AppendLine("## " + title);
			b.AppendLine(new string('-', Math.Min(96, title.Length)));
		}

		public static string Purlin(PurlinDesignInput i, PurlinDesignResult r)
		{
			StringBuilder stringBuilder = new StringBuilder();
			PurlinProperties properties = i.Properties;
			double num12 = i.Angle * Math.PI / 180.0;
			double num = i.Profile.KgM * 9.81 / 1000.0;
			double value = i.Dead * i.Spacing + num;
			double num2 = i.Span * 1000.0;
			double num3 = properties.Ix * properties.Iy - properties.Ixy * properties.Ixy;
			stringBuilder.AppendLine("DTCE | DETAILED PURLIN CALCULATION REPORT");
			stringBuilder.AppendLine(i.Name + " | " + i.Profile.Name + " | ASD | " + i.Support);
			stringBuilder.AppendLine("Units: dimensions mm; spans m; area loads kPa = kN/m2; line loads kN/m = N/mm.");
			stringBuilder.AppendLine("Moments kN.m are multiplied by 1,000,000 for stress calculations in N.mm and MPa.");
			Heading(stringBuilder, "01  DESIGN INPUTS AND SECTION BASIS");
			Line(stringBuilder, "Geometry", "L=" + N(i.Span) + " m; tributary spacing=" + N(i.Spacing) + " m; roof slope=" + N(i.Angle) + " deg.");
			Line(stringBuilder, "Handbook section", "h=" + N(i.Profile.H) + "; b=" + N(i.Profile.B) + "; lip=" + N(i.Profile.Lip) + "; t=" + N(i.Profile.T) + " mm; mass=" + N(i.Profile.KgM) + " kg/m.");
			Line(stringBuilder, "Materials / restraint", "Fy=" + N(i.Fy) + " MPa; E=" + N(i.E) + " MPa; effective sag rods=" + i.SagRods + "; " + i.Ceiling + ".");
			stringBuilder.AppendLine(properties.Verified ? "Ix/Iy/Ixy/Sx/Sy are user-verified inputs; area/centroid reference is nominal geometry." : "Gross-section properties use nonoverlapping rectangular plates; bend radii are neglected.");
			stringBuilder.AppendLine("A=sum(bi*hi); xbar=sum(Ai*xi)/A; ybar=sum(Ai*yi)/A.");
			stringBuilder.AppendLine("Ix=sum(bi*hi^3/12 + Ai*(yi-ybar)^2); Iy=sum(hi*bi^3/12 + Ai*(xi-xbar)^2).");
			stringBuilder.AppendLine("Ixy=sum(Ai*(xi-xbar)*(yi-ybar)); Sx=Ix/max(abs(y)); Sy=Iy/max(abs(x)).");
			Line(stringBuilder, "Adopted properties", "A=" + N(properties.Area) + " mm2; xbar=" + N(properties.Xbar) + " mm; ybar=" + N(properties.Ybar) + " mm.");
			Line(stringBuilder, "Inertia", "Ix=" + N(properties.Ix) + "; Iy=" + N(properties.Iy) + "; Ixy=" + N(properties.Ixy) + " mm4.");
			Line(stringBuilder, "Section moduli", "Sx=" + N(properties.Sx) + "; Sy=" + N(properties.Sy) + " mm3.");
			Heading(stringBuilder, "02  SELF-WEIGHT AND RESOLVED LINE LOADS");
			Line(stringBuilder, "Self-weight = mass * g / 1000", N(i.Profile.KgM) + " * 9.81 / 1000 = " + N(num) + " kN/m.");
			Line(stringBuilder, "D = superimposed D * spacing + self-weight", N(i.Dead) + " * " + N(i.Spacing) + " + " + N(num) + " = " + N(value) + " kN/m.");
			Line(stringBuilder, "Dx = D*cos(theta); Dy = D*sin(theta)", N(value) + " * cos(" + N(i.Angle) + ") = " + N(r.Dx) + "; " + N(value) + " * sin(" + N(i.Angle) + ") = " + N(r.Dy) + " kN/m.");
			Line(stringBuilder, "Lx = live*spacing*cos(theta); Ly = live*spacing*sin(theta)", N(i.Live) + " * " + N(i.Spacing) + " * cos(" + N(i.Angle) + ") = " + N(r.Lx) + "; Ly=" + N(r.Ly) + " kN/m.");
			Line(stringBuilder, "Wind acts normal to the roof; retain its entered sign", "Wp=" + N(i.WindPressure) + " * " + N(i.Spacing) + " = " + N(i.WindPressure * i.Spacing) + "; Ws=" + N(i.WindSuction) + " * " + N(i.Spacing) + " = " + N(i.WindSuction * i.Spacing) + " kN/m.");
			Heading(stringBuilder, "03  WORKBOOK ALLOWABLE STRESSES AND MOMENT COEFFICIENTS");
			Line(stringBuilder, "Flange ratio b/t versus 0.38*sqrt(E/Fy)", N(i.Profile.B / i.Profile.T) + " versus " + N(0.38 * Math.Sqrt(i.E / i.Fy)) + " -> Fbx=" + N(r.Fbx / i.Fy) + " * " + N(i.Fy) + " = " + N(r.Fbx) + " MPa.");
			Line(stringBuilder, "Web ratio (h-2t)/t versus 3.76*sqrt(E/Fy)", N((i.Profile.H - 2.0 * i.Profile.T) / i.Profile.T) + " versus " + N(3.76 * Math.Sqrt(i.E / i.Fy)) + " -> Fby=" + N(r.Fby / i.Fy) + " * " + N(i.Fy) + " = " + N(r.Fby) + " MPa.");
			stringBuilder.AppendLine("These are the supplied workbook stress rules; they are not a complete cold-formed buckling assessment.");
			Line(stringBuilder, "Mx=Cx*wx*L^2; My=Cy*wy*L^2", "Cx=" + N(r.Cx) + "; Cy=" + N(r.Cy) + "; L^2=" + N(i.Span * i.Span) + " m2.");
			stringBuilder.AppendLine("Sag rods affect the stated tangential coefficient only; normal-axis span and deflection use full span.");
			Heading(stringBuilder, "04  LOAD CASES, SUBSTITUTIONS AND INTERACTION");
			bool flag = Math.Abs(properties.Ixy) >= 1E-08;
			stringBuilder.AppendLine(flag ? ("Coupled section: Delta=Ix*Iy-Ixy^2=" + N(num3) + " mm8.") : "Uncoupled section: ratio=abs(Mx)*1e6/(Sx*Fbx*) + abs(My)*1e6/(Sy*Fby*).");
			if (flag)
			{
				stringBuilder.AppendLine("At each section corner (x,y): sx=Mx*1e6*(Iy*y-Ixy*x)/Delta.");
				stringBuilder.AppendLine("sy=My*1e6*(Ix*x-Ixy*y)/Delta; ratio=max(abs(sx)/Fbx* + abs(sy)/Fby*).");
			}
			foreach (PurlinLoadCase @case in r.Cases)
			{
				double num4 = ((@case.Alternate && i.AlternateIncrease) ? 1.3333333333333333 : 1.0);
				Heading(stringBuilder, "CASE: " + @case.Name);
				Line(stringBuilder, "Resolved design line loads", "wx=" + N(@case.Normal) + "; wy=" + N(@case.Tangential) + " kN/m.");
				Line(stringBuilder, "Moment substitutions", "Mx=" + N(r.Cx) + " * " + N(@case.Normal) + " * " + N(i.Span) + "^2 = " + N(@case.Mx) + " kN.m.");
				stringBuilder.AppendLine("  My=" + N(r.Cy) + " * " + N(@case.Tangential) + " * " + N(i.Span) + "^2 = " + N(@case.My) + " kN.m.");
				Line(stringBuilder, "Allowable stress factor", N(num4) + " -> Fbx*=" + N(r.Fbx * num4) + "; Fby*=" + N(r.Fby * num4) + " MPa.");
				if (!flag)
				{
					stringBuilder.AppendLine("  Ratio=" + N(Math.Abs(@case.Mx)) + "*1e6/(" + N(properties.Sx) + "*" + N(r.Fbx * num4) + ") + " + N(Math.Abs(@case.My)) + "*1e6/(" + N(properties.Sy) + "*" + N(r.Fby * num4) + ").");
				}
				stringBuilder.AppendLine("  Interaction=" + N(@case.Ratio) + " <= 1.0 : " + Status(@case.Ratio <= 1.0));
			}
			Heading(stringBuilder, "05  GOVERNING FLEXURE ENVELOPES");
			bool[] array = new bool[2] { false, true };
			foreach (bool alternate in array)
			{
				List<PurlinLoadCase> source = r.Cases.Where((PurlinLoadCase c) => c.Alternate == alternate).ToList();
				PurlinLoadCase purlinLoadCase = source.OrderByDescending((PurlinLoadCase c) => Math.Abs(c.Mx)).First();
				PurlinLoadCase purlinLoadCase2 = source.OrderByDescending((PurlinLoadCase c) => Math.Abs(c.My)).First();
				double num5 = (alternate ? r.Alternate : r.Basic);
				stringBuilder.AppendLine((alternate ? "Alternate" : "Basic") + " independent component envelope:");
				stringBuilder.AppendLine("  abs(Mx)=" + N(Math.Abs(purlinLoadCase.Mx)) + " from " + purlinLoadCase.Name + "; abs(My)=" + N(Math.Abs(purlinLoadCase2.My)) + " from " + purlinLoadCase2.Name + ".");
				stringBuilder.AppendLine("  Envelope interaction=" + N(num5) + " <= 1.0 : " + Status(num5 <= 1.0));
			}
			stringBuilder.AppendLine("Independent component maxima may come from different cases; the envelope is a conservative screen.");
			PurlinLoadCase purlinLoadCase3 = r.Cases.OrderByDescending((PurlinLoadCase c) => c.Ratio).First();
			stringBuilder.AppendLine("Largest individual-case ratio: " + purlinLoadCase3.Name + " = " + N(purlinLoadCase3.Ratio) + ".");
			Heading(stringBuilder, "06  SERVICEABILITY / VECTOR DEFLECTION");
			double num6 = ((i.Support == "Cantilever") ? 0.125 : (5.0 / 384.0));
			stringBuilder.AppendLine("K=" + N(num6) + "; span=" + N(num2) + " mm; Delta=Ix*Iy-Ixy^2=" + N(num3) + " mm8.");
			stringBuilder.AppendLine("delta_x=K*Lmm^4*(Ix*wy-Ixy*wx)/(E*Delta).");
			stringBuilder.AppendLine("delta_y=K*Lmm^4*(Iy*wx-Ixy*wy)/(E*Delta); resultant=sqrt(delta_x^2+delta_y^2).");
			array = new bool[2] { false, true };
			foreach (bool flag2 in array)
			{
				double num7 = (flag2 ? (r.Dx + r.Lx) : r.Lx);
				double num8 = (flag2 ? (r.Dy + r.Ly) : r.Ly);
				double num9 = num6 * Math.Pow(num2, 4.0) / i.E;
				double value2 = num9 * (properties.Ix * num8 - properties.Ixy * num7) / num3;
				double value3 = num9 * (properties.Iy * num7 - properties.Ixy * num8) / num3;
				double num10 = (flag2 ? r.TotalDeflection : r.LiveDeflection);
				double num11 = (flag2 ? r.TotalLimit : r.LiveLimit);
				stringBuilder.AppendLine((flag2 ? "D + L" : "Live only") + ": wx=" + N(num7) + "; wy=" + N(num8) + " N/mm.");
				stringBuilder.AppendLine("  delta_x=" + N(value2) + "; delta_y=" + N(value3) + " -> resultant=" + N(num10) + " mm.");
				stringBuilder.AppendLine("  Limit = " + N(num2) + " / " + N(num2 / num11) + " = " + N(num11) + " mm; DCR=" + N(num10 / num11) + " : " + Status(num10 <= num11));
			}
			Heading(stringBuilder, "07  DESIGN DISPOSITION AND REFERENCES");
			stringBuilder.AppendLine(r.WithinWorkbookChecks ? "PASS for the listed workbook checks only; remaining member checks require verification." : "REVIEW: one or more listed workbook checks exceed their limits.");
			stringBuilder.AppendLine("Full precision is used internally; displayed substitutions are rounded to six decimal places.");
			Heading(stringBuilder, "APPENDIX  INPUT AUDIT, METHOD SCOPE AND SOURCE ADJUSTMENTS");
			stringBuilder.Append(PurlinDesigner.ReferenceSummary(i, r));
			return stringBuilder.ToString();
		}

		public static string Truss(ConnectionInput i, ConnectionResult r, string profile, string source)
		{
			StringBuilder stringBuilder = new StringBuilder();
			int num = ((!(i.Family == "Double angle")) ? 1 : 2);
			double num2 = i.Area * (double)num;
			double value = ((i.Connection == "Weld") ? ((i.LL1 + i.LL2) / 2.0) : ((double)(i.Bolts - 1) * i.Spacing));
			stringBuilder.AppendLine("DTCE | DETAILED TRUSS CONNECTION CALCULATION REPORT");
			stringBuilder.AppendLine(profile + " | " + i.Family + " | " + i.Connection + " | " + i.Method);
			stringBuilder.AppendLine("Units: dimensions mm; areas mm2; stresses MPa=N/mm2; forces kN.");
			Heading(stringBuilder, "01  INPUTS AND LOAD BASIS");
			Line(stringBuilder, "Axial design demand", N(i.Demand) + " kN; use loads consistent with " + i.Method + ".");
			Line(stringBuilder, "Member material", "Fy=" + N(i.Fy) + "; Fu=" + N(i.Fu) + " MPa; thickness=" + N(i.Thickness) + " mm.");
			Line(stringBuilder, "Member assembly area", "Ag=" + N(i.Area) + " per member * " + num + " = " + N(num2) + " mm2.");
			Line(stringBuilder, "Gusset / connected part", "Fy=" + N(i.GussetFy) + "; Fu=" + N(i.GussetFu) + " MPa; t=" + N(i.GussetThickness) + " mm; effective width=" + N(i.GussetWidth) + " mm.");
			stringBuilder.AppendLine("Demand is an absolute axial envelope; sign, selected member/load cases and other force components are audited below.");
			Heading(stringBuilder, "02  DESIGN FACTORS AND MEMBER YIELDING");
			stringBuilder.AppendLine((i.Method == "LRFD") ? "Available strength=phi*Rn; yielding phi=0.90, other listed limit states phi=0.75." : "Available strength=Rn/Omega; yielding Omega=1.67, other listed limit states Omega=2.00.");
			Line(stringBuilder, "Member yield Rn=Fy*Ag/1000", N(i.Fy) + " * " + N(num2) + " / 1000 = " + N(i.Fy * num2 / 1000.0) + " kN.");
			Heading(stringBuilder, "03  CONNECTION LENGTH AND SHEAR LAG");
			Line(stringBuilder, (i.Connection == "Weld") ? "L=(LL1+LL2)/2" : "L=(number of bolts-1)*spacing", (i.Connection == "Weld") ? ("(" + N(i.LL1) + "+" + N(i.LL2) + ")/2 = " + N(value) + " mm.") : ("(" + i.Bolts + "-1)*" + N(i.Spacing) + " = " + N(value) + " mm."));
			if (i.AllElementsConnected)
			{
				stringBuilder.AppendLine("U=1.0: user confirms all cross-section elements are connected.");
			}
			else if (i.VerifiedU > 0.0)
			{
				stringBuilder.AppendLine("U=" + N(r.U) + ": user-supplied verified factor.");
			}
			else
			{
				stringBuilder.AppendLine("U=1-xbar/L = 1-" + N(i.Offset) + "/" + N(value) + " = " + N(r.U) + ".");
			}
			if (i.Connection == "Weld")
			{
				Heading(stringBuilder, "04  WELD DETAIL AND EFFECTIVE LENGTHS");
				double num3 = Math.Max(i.Thickness, i.GussetThickness);
				double value2 = ((num3 <= 6.0) ? 3 : ((num3 <= 13.0) ? 5 : ((num3 <= 19.0) ? 6 : 8)));
				double num4 = ((i.WeldEdgeThickness > 0.0) ? i.WeldEdgeThickness : i.Thickness);
				double value3 = ((num4 < 6.0) ? num4 : (num4 - 2.0));
				stringBuilder.AppendLine("w=" + N(i.Weld) + " mm; Fexx=" + N(i.Fexx) + " MPa; throat=0.707w=" + N(0.707 * i.Weld) + " mm.");
				stringBuilder.AppendLine("Thicker connected part=" + N(num3) + " mm -> minimum weld=" + N(value2) + " mm.");
				stringBuilder.AppendLine("Actual welded edge=" + N(num4) + " mm -> maximum weld=" + N(value3) + " mm; verify exceptions separately.");
				stringBuilder.AppendLine("Minimum segment length=4w=" + N(4.0 * i.Weld) + " mm.");
				var array = new[]
				{
					new
					{
						Label = "LL1",
						Length = i.LL1
					},
					new
					{
						Label = "LL2",
						Length = i.LL2
					},
					new
					{
						Label = "LT",
						Length = i.LT
					}
				};
				foreach (var anon in array)
				{
					double value4 = anon.Length / i.Weld;
					double value5 = TrussConnectionDesigner.WeldLength(anon.Length, i.Weld);
					stringBuilder.AppendLine(anon.Label + ": L=" + N(anon.Length) + " mm; L/w=" + N(value4) + "; Leff=" + N(value5) + " mm.");
				}
				stringBuilder.AppendLine("Leff=L for L/w<=100; (1.2-0.002L/w)*L for <=300; 180w above 300.");
				Heading(stringBuilder, "05  WELD METAL AND MEMBER RUPTURE");
				double num5 = TrussConnectionDesigner.WeldLength(i.LL1, i.Weld) + TrussConnectionDesigner.WeldLength(i.LL2, i.Weld);
				double num6 = TrussConnectionDesigner.WeldLength(i.LT, i.Weld);
				double value6 = 0.707 * i.Weld * num5 * 0.6 * i.Fexx / 1000.0;
				double value7 = 0.707 * i.Weld * num6 * 0.6 * i.Fexx / 1000.0;
				Line(stringBuilder, "Rn,long = 0.707*w*(Leff1+Leff2)*0.60*Fexx/1000", "0.707*" + N(i.Weld) + "*" + N(num5) + "*0.60*" + N(i.Fexx) + "/1000 = " + N(value6) + " kN.");
				Line(stringBuilder, "Rn,trans = 0.707*w*LeffT*0.60*Fexx/1000", "0.707*" + N(i.Weld) + "*" + N(num6) + "*0.60*" + N(i.Fexx) + "/1000 = " + N(value7) + " kN.");
				stringBuilder.AppendLine(i.DirectionalWeldIncrease ? "Directional option: Rn=max(Rlong+Rtrans,0.85Rlong+1.50Rtrans)*assembly count." : "Directional increase disabled: Rn=(Rlong+Rtrans)*assembly count.");
				stringBuilder.AppendLine("  Assembly weld Rn=" + N(r.NominalWeld) + " kN; assembly count=" + num + ".");
				Line(stringBuilder, "Member rupture Rn=Fu*U*Ag/1000", N(i.Fu) + "*" + N(r.U) + "*" + N(num2) + "/1000 = " + N(r.NominalRupture) + " kN.");
			}
			else
			{
				Heading(stringBuilder, "04  BOLT GEOMETRY AND DETAIL LIMITS");
				double num7 = Math.Min(i.Thickness, i.GussetThickness);
				double num8 = Math.Min(i.Spacing - i.HoleDiameter, i.EdgeDistance - i.HoleDiameter / 2.0);
				double value8 = Math.PI * i.BoltDiameter * i.BoltDiameter / 4.0;
				stringBuilder.AppendLine(i.Grade + "; db=" + N(i.BoltDiameter) + " mm; hole=" + N(i.HoleDiameter) + " mm; bolts=" + i.Bolts + "; shear planes=" + i.ShearPlanes + ".");
				stringBuilder.AppendLine("Spacing=" + N(i.Spacing) + " mm; end distance=" + N(i.EdgeDistance) + " mm; Fnv=" + N(i.Fnv) + " MPa.");
				stringBuilder.AppendLine("Minimum spacing=8db/3=" + N(8.0 * i.BoltDiameter / 3.0) + " mm; maximum=" + N(i.Exposed ? Math.Min(14.0 * num7, 180.0) : Math.Min(24.0 * num7, 300.0)) + " mm.");
				stringBuilder.AppendLine("Edge limits: 1.75db=" + N(1.75 * i.BoltDiameter) + " to min(12t,150)=" + N(Math.Min(12.0 * num7, 150.0)) + " mm.");
				Line(stringBuilder, "Lc=min(spacing-hole,edge-hole/2)", "min(" + N(i.Spacing - i.HoleDiameter) + "," + N(i.EdgeDistance - i.HoleDiameter / 2.0) + ") = " + N(num8) + " mm.");
				Heading(stringBuilder, "05  BOLT SHEAR, BEARING AND NET-SECTIONS");
				Line(stringBuilder, "Bolt area Ab=pi*db^2/4", "pi*" + N(i.BoltDiameter) + "^2/4 = " + N(value8) + " mm2.");
				Line(stringBuilder, "Bolt shear Rn=Fnv*Ab*n*shear planes/1000", N(i.Fnv) + "*" + N(value8) + "*" + i.Bolts + "*" + i.ShearPlanes + "/1000 = " + N(r.NominalBolt) + " kN.");
				bool[] array2 = new bool[2] { true, false };
				foreach (bool flag in array2)
				{
					double num9 = (flag ? i.Thickness : i.GussetThickness);
					double num10 = (flag ? i.Fu : i.GussetFu);
					double num11 = 1.2 * num8 * num9 * num10;
					double num12 = 2.4 * i.BoltDiameter * num9 * num10;
					int num13 = ((!flag) ? 1 : num);
					Line(stringBuilder, (flag ? "Member" : "Gusset") + " bearing Rn=n*min(1.2Lc*t*Fu,2.4db*t*Fu)*multiplicity/1000", i.Bolts + "*min(" + N(num11) + "," + N(num12) + ")*" + num13 + "/1000 = " + N((double)i.Bolts * Math.Min(num11, num12) * (double)num13 / 1000.0) + " kN.");
				}
				double value9 = num2 - i.HoleDiameter * i.Thickness * (double)num;
				double num14 = (i.GussetWidth - i.HoleDiameter) * i.GussetThickness;
				Line(stringBuilder, "Member An=Ag-hole*t*assembly count", N(num2) + "-" + N(i.HoleDiameter) + "*" + N(i.Thickness) + "*" + num + " = " + N(value9) + " mm2.");
				Line(stringBuilder, "Member rupture Rn=Fu*U*An/1000", N(i.Fu) + "*" + N(r.U) + "*" + N(value9) + "/1000 = " + N(r.NominalRupture) + " kN.");
				Line(stringBuilder, "Gusset yield Rn=Fy*width*t/1000", N(i.GussetFy) + "*" + N(i.GussetWidth) + "*" + N(i.GussetThickness) + "/1000 = " + N(i.GussetFy * i.GussetWidth * i.GussetThickness / 1000.0) + " kN.");
				Line(stringBuilder, "Gusset An=(width-hole)*t", "(" + N(i.GussetWidth) + "-" + N(i.HoleDiameter) + ")*" + N(i.GussetThickness) + " = " + N(num14) + " mm2.");
				Line(stringBuilder, "Gusset rupture Rn=Fu*An/1000", N(i.GussetFu) + "*" + N(num14) + "/1000 = " + N(i.GussetFu * num14 / 1000.0) + " kN.");
			}
			Heading(stringBuilder, "06  MEMBER BLOCK SHEAR");
			if (i.Agv > 0.0)
			{
				double num15 = (0.6 * i.Fu * i.Anv + i.Fu * i.Ant) / 1000.0;
				double num16 = (0.6 * i.Fy * i.Agv + i.Fu * i.Ant) / 1000.0;
				stringBuilder.AppendLine("Verified TOTAL assembly areas: Agv=" + N(i.Agv) + "; Anv=" + N(i.Anv) + "; Ant=" + N(i.Ant) + " mm2; Ubs=1.");
				Line(stringBuilder, "Rn=min(0.6Fu*Anv+Fu*Ant,0.6Fy*Agv+Fu*Ant)/1000", "min(" + N(num15) + "," + N(num16) + ") = " + N(Math.Min(num15, num16)) + " kN.");
				stringBuilder.AppendLine("Only the entered member path is checked; gusset and other paths remain separate.");
			}
			else
			{
				stringBuilder.AppendLine("NOT CHECKED: no verified member path areas entered. This omission prevents a complete connection approval.");
			}
			Heading(stringBuilder, "07  AVAILABLE STRENGTHS, DEMAND/CAPACITY RATIOS AND GOVERNING CHECK");
			foreach (ConnectionCheck check in r.Checks)
			{
				bool num17 = check.Name.IndexOf("yield", StringComparison.OrdinalIgnoreCase) >= 0;
				double num18 = (num17 ? 0.9 : 0.75);
				double num19 = (num17 ? 1.67 : 2.0);
				double value10 = ((i.Method == "LRFD") ? (check.Capacity / num18) : (check.Capacity * num19));
				stringBuilder.AppendLine(check.Name);
				stringBuilder.AppendLine("  " + ((i.Method == "LRFD") ? ("phi*Rn=" + N(num18) + "*" + N(value10)) : ("Rn/Omega=" + N(value10) + "/" + N(num19))) + " = " + N(check.Capacity) + " kN.");
				stringBuilder.AppendLine("  DCR=" + N(check.Demand) + "/" + N(check.Capacity) + " = " + N(check.Demand / check.Capacity) + " <= 1.0 : " + Status(check.Pass));
			}
			ConnectionCheck connectionCheck = r.Checks.OrderByDescending((ConnectionCheck c) => c.Demand / c.Capacity).First();
			stringBuilder.AppendLine("Governing listed strength check: " + connectionCheck.Name + "; DCR=" + N(connectionCheck.Demand / connectionCheck.Capacity) + ".");
			foreach (string detailError in r.DetailErrors)
			{
				stringBuilder.AppendLine("REVIEW DETAIL: " + detailError);
			}
			Heading(stringBuilder, "08  DISPOSITION, SCOPE AND SOURCE AUDIT");
			stringBuilder.AppendLine(r.ListedChecksPass ? "PASS for listed strengths/detail limits only; omitted limit states require separate verification." : "REVIEW: a listed strength or detail limit is exceeded.");
			stringBuilder.AppendLine("Full precision is used internally; displayed substitutions are rounded to six decimal places.");
			stringBuilder.Append(TrussConnectionDesigner.ReferenceSummary(i, r, profile, source));
			return stringBuilder.ToString();
		}
	}
}
