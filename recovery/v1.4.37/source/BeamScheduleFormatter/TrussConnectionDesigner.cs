using System;
using System.Globalization;
using System.Text;

namespace BeamScheduleFormatter
{
	internal static class TrussConnectionDesigner
	{
		private static double Available(double nominal, string method, double phi, double omega)
		{
			if (!(method == "LRFD"))
			{
				return nominal / omega;
			}
			return nominal * phi;
		}

		internal static double WeldLength(double length, double size)
		{
			double num = length / size;
			if (!(num <= 100.0))
			{
				if (!(num <= 300.0))
				{
					return 180.0 * size;
				}
				return (1.2 - 0.002 * num) * length;
			}
			return length;
		}

		public static ConnectionResult Calculate(ConnectionInput input)
		{
			input.Validate();
			ConnectionResult r = new ConnectionResult();
			int num = ((!(input.Family == "Double angle")) ? 1 : 2);
			double num2 = input.Area * (double)num;
			double num3 = Math.Min(input.Thickness, input.GussetThickness);
			Action<string, double, double, string> action = delegate(string name, double nominal, double phi, string note)
			{
				r.Checks.Add(new ConnectionCheck
				{
					Name = name,
					Capacity = Available(nominal, input.Method, phi, (phi == 0.9) ? 1.67 : 2.0),
					Demand = input.Demand,
					Note = note
				});
			};
			action("Member gross tension yielding", input.Fy * num2 / 1000.0, 0.9, "Screened against absolute axial envelope, including compression-only imports conservatively.");
			double num4 = ((input.Connection == "Weld") ? ((input.LL1 + input.LL2) / 2.0) : ((double)(input.Bolts - 1) * input.Spacing));
			if (input.AllElementsConnected)
			{
				r.U = 1.0;
			}
			else if (input.VerifiedU > 0.0)
			{
				r.U = input.VerifiedU;
			}
			else
			{
				if (num4 <= 0.0)
				{
					throw new Exception("Supply verified U for a nonstandard/transverse-only or one-bolt connection, or confirm all cross-section elements are connected.");
				}
				r.U = 1.0 - input.Offset / num4;
				if (r.U <= 0.0 || r.U > 1.0)
				{
					throw new Exception("Derived U=1-xbar/L must be >0 and <=1; review connection length/offset.");
				}
			}
			if (input.Connection == "Weld")
			{
				double num5 = Math.Max(input.Thickness, input.GussetThickness);
				double num6 = ((num5 <= 6.0) ? 3 : ((num5 <= 13.0) ? 5 : ((num5 <= 19.0) ? 6 : 8)));
				double num7 = ((input.WeldEdgeThickness > 0.0) ? input.WeldEdgeThickness : input.Thickness);
				double num8 = ((num7 < 6.0) ? num7 : (num7 - 2.0));
				if (input.Weld < num6 || input.Weld > num8)
				{
					r.DetailErrors.Add("Fillet size outside minimum " + num6 + "mm / edge maximum " + num8 + "mm. Verify actual weld detail and applicable exceptions.");
				}
				double[] array = new double[3] { input.LT, input.LL1, input.LL2 };
				foreach (double num10 in array)
				{
					if (num10 > 0.0 && num10 < 4.0 * input.Weld)
					{
						r.DetailErrors.Add("Each effective fillet segment must be at least 4 x weld size.");
					}
				}
				double num11 = 0.707 * input.Weld * (WeldLength(input.LL1, input.Weld) + WeldLength(input.LL2, input.Weld)) * 0.6 * input.Fexx / 1000.0;
				double num12 = 0.707 * input.Weld * WeldLength(input.LT, input.Weld) * 0.6 * input.Fexx / 1000.0;
				r.NominalWeld = (input.DirectionalWeldIncrease ? Math.Max(num11 + num12, 0.85 * num11 + 1.5 * num12) : (num11 + num12)) * (double)num;
				action("Fillet weld metal", r.NominalWeld, 0.75, "0.707w x effective weld length x 0.60Fexx; phi=.75 / Omega=2.00.");
				r.NominalRupture = input.Fu * r.U * num2 / 1000.0;
				action("Member tensile rupture / shear lag", r.NominalRupture, 0.75, "U=" + r.U.ToString("0.######", CultureInfo.InvariantCulture) + "; area per angle, then assembly multiplicity once.");
			}
			else
			{
				double num13 = 2.6666666666666665 * input.BoltDiameter;
				double num14 = (input.Exposed ? Math.Min(14.0 * num3, 180.0) : Math.Min(24.0 * num3, 300.0));
				double num15 = Math.Min(12.0 * num3, 150.0);
				if (input.Spacing < num13 || input.Spacing > num14)
				{
					r.DetailErrors.Add("Bolt spacing outside " + num13.ToString("0.###") + " to " + num14 + "mm.");
				}
				if (input.EdgeDistance < 1.75 * input.BoltDiameter || input.EdgeDistance > num15)
				{
					r.DetailErrors.Add("Edge distance outside conservative 1.75db to min(12t,150mm). Confirm edge preparation/table exceptions.");
				}
				double num16 = Math.Min(input.Spacing - input.HoleDiameter, input.EdgeDistance - input.HoleDiameter / 2.0);
				if (num16 <= 0.0)
				{
					throw new Exception("Hole diameter leaves no clear edge/spacing distance.");
				}
				r.NominalBolt = input.Fnv * Math.PI * input.BoltDiameter * input.BoltDiameter / 4.0 * (double)input.Bolts * (double)input.ShearPlanes / 1000.0;
				action("Bolt shear", r.NominalBolt, 0.75, "One shear plane for a single angle; two only for a verified double-shear joint.");
				double num17 = (double)input.Bolts * Math.Min(1.2 * num16 * input.Thickness * input.Fu, 2.4 * input.BoltDiameter * input.Thickness * input.Fu) * (double)num / 1000.0;
				double num18 = (double)input.Bolts * Math.Min(1.2 * num16 * input.GussetThickness * input.GussetFu, 2.4 * input.BoltDiameter * input.GussetThickness * input.GussetFu) / 1000.0;
				r.NominalBearing = Math.Min(num17, num18);
				action("Member bearing", num17, 0.75, "Clear distance=min(s-hole, edge-hole/2); one longitudinal bolt row.");
				action("Gusset bearing", num18, 0.75, "Gusset counted once, even with double angles.");
				double num19 = num2 - input.HoleDiameter * input.Thickness * (double)num;
				if (num19 <= 0.0)
				{
					throw new Exception("Bolt hole consumes the net section.");
				}
				r.NominalRupture = input.Fu * r.U * num19 / 1000.0;
				action("Member net tensile rupture", r.NominalRupture, 0.75, "One hole per angle across net section; no stagger assumed.");
				double num20 = (input.GussetWidth - input.HoleDiameter) * input.GussetThickness;
				if (num20 <= 0.0)
				{
					throw new Exception("Gusset width must exceed the hole diameter.");
				}
				action("Gusset gross yielding", input.GussetFy * input.GussetWidth * input.GussetThickness / 1000.0, 0.9, "Gusset width must represent the effective load-transfer width.");
				action("Gusset net rupture", input.GussetFu * num20 / 1000.0, 0.75, "One hole across gusset net width; no stagger.");
			}
			if (input.Agv > 0.0)
			{
				double arg = Math.Min(0.6 * input.Fu * input.Anv + input.Fu * input.Ant, 0.6 * input.Fy * input.Agv + input.Fu * input.Ant) / 1000.0;
				action("Verified member block-shear path", arg, 0.75, "Agv/Anv/Ant are TOTAL assembly path areas; Ubs=1 assumes uniform tensile stress. Check gusset paths separately.");
			}
			return r;
		}

		public static string Report(ConnectionInput input, ConnectionResult r, string profile, string source)
		{
			return DetailedDesignReports.Truss(input, r, profile, source);
		}

		internal static string ReferenceSummary(ConnectionInput input, ConnectionResult r, string profile, string source)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("TRUSS CONNECTION | " + input.Family + " | " + input.Connection + " | " + input.Method);
			stringBuilder.AppendLine(profile + " | " + source);
			stringBuilder.AppendLine("AXIAL, CONCENTRIC CONNECTION CHECKS ONLY. Member compression/buckling, combined bending/shear, gusset buckling, complete HSS chord/joint limit states, prying, fatigue and seismic detailing are outside this sheet.");
			stringBuilder.AppendLine("Section input basis: " + (input.VerifiedProperties ? "user-confirmed verified geometry/properties" : "nominal sharp-corner geometry; manufacturer/section verification remains required"));
			stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "Axial demand={0:0.###}kN; area PER angle/member={1:0.###}mm2; t={2:0.###}mm; centroid offset={3:0.###}mm; U={4:0.######}", input.Demand, input.Area, input.Thickness, input.Offset, r.U));
			stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "Member Fy/Fu={0}/{1}MPa; gusset Fy/Fu={2}/{3}MPa; gusset t={4}mm", input.Fy, input.Fu, input.GussetFy, input.GussetFu, input.GussetThickness));
			foreach (ConnectionCheck check in r.Checks)
			{
				stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0}: capacity {1:0.###}kN; demand {2:0.###}kN; DCR {3:0.######}; {4}\n  {5}", check.Name, check.Capacity, check.Demand, check.Demand / check.Capacity, check.Pass ? "WITHIN" : "EXCEEDED", check.Note));
			}
			foreach (string detailError in r.DetailErrors)
			{
				stringBuilder.AppendLine("DETAIL CHECK: " + detailError);
			}
			stringBuilder.AppendLine((input.Agv > 0.0) ? "Block shear: only the entered verified MEMBER path is checked; assess all gusset paths separately." : "BLOCK SHEAR NOT CHECKED: provide verified path areas and assess all member/gusset paths before final design.");
			stringBuilder.AppendLine(r.ListedChecksPass ? "LISTED CHECKS WITHIN LIMITS; connection verification remains incomplete for omitted limit states." : "LISTED CHECKS OR DETAIL LIMITS EXCEEDED.");
			stringBuilder.AppendLine("WORKBOOK BASIS / CORRECTIONS");
			stringBuilder.AppendLine("Supplied PS Truss workbooks Y135:Y184: fillet strength, shear lag, bolt shear/bearing, member rupture. LRFD/ASD load demand must match the selected method.");
			stringBuilder.AppendLine("Fillet phi=.75/Omega=2.00 per AISC 360 Table J2.5, replacing workbook .9/1.67. Min fillet size follows thicker part, max follows the actual welded edge (default member edge); workbook weld-size/thickness lookup is corrected. Standard hole input is distinct from minimum edge distance; clear end distance subtracts half the hole.");
			stringBuilder.AppendLine("Double-angle area, weld lines and member bearing are multiplied once; bolt shear uses explicit shear planes; gusset bearing is never doubled. Shear-lag uses centroid offset, not the workbook double-angle radius-of-gyration reference.");
			stringBuilder.AppendLine("AISC 360-16: https://www.aisc.org/globalassets/aisc/publications/standards/a360-16-spec-and-commentary_march-2021.pdf");
			return stringBuilder.ToString();
		}
	}
}
