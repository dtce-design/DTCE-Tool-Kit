using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal class TrussConnectionForm : DesignSheetForm
	{
		private readonly ComboBox family;

		private readonly ComboBox profile;

		private readonly ComboBox grade;

		private readonly ComboBox threads;

		private readonly CheckBox verified;

		private readonly TextBox area;

		private readonly TextBox thickness;

		private readonly TextBox offset;

		private readonly TextBox table;

		private bool updating;

		public TrussConnectionForm()
			: base("Truss Connections | Workbook axial checks", "Truss-Connections")
		{
			FlowLayoutPanel panel = Section("Member & demand", "Axial/concentric truss connection checks based on the four supplied PS workbooks. Select a handbook profile and verify design section area/thickness. HSS sheets cover welded connections; complete chord/joint limit states and member buckling remain separate.");
			family = Choice(panel, "Family", "Member family", new string[4] { "Angle", "Double angle", "HSS rectangle", "HSS circle / pipe" });
			profile = Choice(panel, "Profile", "Regan handbook profile", (from p in SteelCatalog.Profiles
				where p.Kind == "Angle bar" || p.Kind.StartsWith("HSS")
				select p.Name).ToArray());
			Choice(panel, "Method", "Design method / demand basis", new string[2] { "LRFD", "ASD" });
			Choice(panel, "Connection", "Connection", new string[2] { "Weld", "Bolts" });
			NumberField(panel, "Tension", "Axial tension demand kN", "0");
			NumberField(panel, "Compression", "Axial compression demand kN", "0");
			NumberField(panel, "Fy", "Member Fy MPa", "248");
			NumberField(panel, "Fu", "Member Fu MPa", "400");
			verified = Toggle(panel, "VerifiedProperties", "Use verified member geometry", false);
			area = NumberField(panel, "Area", "Area mm2 PER single angle/member", "1");
			thickness = NumberField(panel, "Thickness", "Member design thickness mm", "6");
			offset = NumberField(panel, "Offset", "Centroid-to-connection offset mm", "20");
			NumberField(panel, "GussetThickness", "Gusset / connected part thickness mm", "6");
			NumberField(panel, "GussetWidth", "Effective gusset width mm", "150");
			NumberField(panel, "GussetFy", "Gusset Fy MPa", "248");
			NumberField(panel, "GussetFu", "Gusset Fu MPa", "400");
			Toggle(panel, "AllConnected", "All cross-section elements connected", false);
			NumberField(panel, "U", "Verified shear-lag U (0 = derive)", "0");
			FlowLayoutPanel panel2 = Section("Weld detail", "Lengths are per single angle/member connection; double-angle multiplicity is applied once. Directional strength increase is optional for a verified concentric uniform-leg longitudinal/transverse weld group. Fillet phi=.75 / Omega=2.00; verify applicable weld detail and exceptions.");
			NumberField(panel2, "Weld", "Fillet leg size mm", "4");
			NumberField(panel2, "WeldEdge", "Thickness at welded edge mm (0=member)", "0");
			NumberField(panel2, "Fexx", "Electrode Fexx MPa (E70 approx 482.65)", "482.65");
			NumberField(panel2, "LT", "Transverse weld length mm", "0");
			NumberField(panel2, "LL1", "Longitudinal weld length 1 mm", "100");
			NumberField(panel2, "LL2", "Longitudinal weld length 2 mm", "100");
			Toggle(panel2, "Directional", "Verified directional weld increase", false);
			FlowLayoutPanel panel3 = Section("Bolt detail", "Single longitudinal bolt row for angle / double-angle to gusset. HSS through-wall bolt and chord-joint checks are not supplied by the source workbooks. Explicit hole diameter is distinct from minimum edge distance. Shear planes must match the actual joint; double angle does not automatically double gusset strength.");
			grade = Choice(panel3, "Grade", "Bolt grade", new string[2] { "A325", "A307" });
			threads = Choice(panel3, "Threads", "Threads at shear plane", new string[2] { "Included", "Excluded" });
			NumberField(panel3, "Bolts", "Number of bolts in one row", "3");
			Choice(panel3, "ShearPlanes", "Shear planes per bolt", new string[2] { "1", "2" });
			NumberField(panel3, "Diameter", "Bolt diameter mm", "16");
			NumberField(panel3, "Hole", "Specified hole diameter mm", "18");
			NumberField(panel3, "Edge", "End / edge distance mm", "35");
			NumberField(panel3, "BoltSpacing", "Bolt spacing mm", "50");
			Toggle(panel3, "Exposed", "Weather exposed", true);
			TextBox shear = NumberField(panel3, "Fnv", "Nominal bolt shear stress MPa", "372");
			Toggle(panel3, "VerifiedFnv", "Use verified nominal shear stress", false);
			Action updateShear = delegate
			{
				bool flag = ((CheckBox)Inputs["VerifiedFnv"]).Checked;
				shear.ReadOnly = !flag;
				if (!flag)
				{
					shear.Text = ((grade.Text == "A307") ? "186" : ((threads.Text == "Included") ? "372" : "457"));
				}
			};
			grade.SelectedIndexChanged += delegate
			{
				if (!Restoring)
				{
					updateShear();
				}
			};
			threads.SelectedIndexChanged += delegate
			{
				if (!Restoring)
				{
					updateShear();
				}
			};
			((CheckBox)Inputs["VerifiedFnv"]).CheckedChanged += delegate
			{
				if (!Restoring)
				{
					updateShear();
				}
			};
			updateShear();
			FlowLayoutPanel panel4 = Section("Block shear", "Optional verified MEMBER block-shear path: enter TOTAL assembly Agv, Anv and Ant in mm2 (do not double again for double angles). Ubs=1 assumes uniform tensile stress. All gusset paths must still be checked separately. Zero entries leave this limit state unverified.");
			NumberField(panel4, "Agv", "Gross shear path Agv mm2", "0");
			NumberField(panel4, "Anv", "Net shear path Anv mm2", "0");
			NumberField(panel4, "Ant", "Net tension path Ant mm2", "0");
			FlowLayoutPanel flowLayoutPanel = Section("STAAD forces", "STAAD: run analysis > Post Processing > Beam End Forces / Envelope; select required members and matching load cases/combinations; set displayed force/moment units; copy the table INCLUDING headers. Paste below or import a CSV/TSV export. Unlike the original workbook, import reads headers and all selected rows rather than assuming the first two Fx cells are the envelope.");
			Choice(flowLayoutPanel, "ForceUnit", "STAAD force unit", new string[3] { "kN", "N", "kip" });
			Choice(flowLayoutPanel, "MomentUnit", "STAAD moment unit", new string[3] { "kN-m", "N-mm", "kip-ft" });
			NumberField(flowLayoutPanel, "Members", "Member IDs / design group (comma-separated)", "");
			Toggle(flowLayoutPanel, "UseStaad", "Use the pasted STAAD force table", false);
			Toggle(flowLayoutPanel, "LoadBasis", "Selected loads match LRFD/ASD basis", false);
			table = new TextBox
			{
				Multiline = true,
				ScrollBars = ScrollBars.Both,
				WordWrap = false,
				Width = 1030,
				Height = 260,
				Tag = "FullWidth",
				Font = new Font("Consolas", 9f)
			};
			Inputs.Add("STAADTable", table);
			flowLayoutPanel.Controls.Add(table);
			FlowLayoutPanel flowLayoutPanel2 = new FlowLayoutPanel
			{
				Width = 1030,
				Height = 48,
				Tag = "FullWidth"
			};
			Button button = new Button
			{
				Text = "Paste STAAD table",
				AutoSize = true
			};
			button.Click += delegate
			{
				if (Clipboard.ContainsText())
				{
					table.Text = Clipboard.GetText();
				}
			};
			Button button2 = new Button
			{
				Text = "Import CSV / TSV",
				AutoSize = true
			};
			button2.Click += delegate
			{
				using (OpenFileDialog openFileDialog = new OpenFileDialog
				{
					Filter = "STAAD table|*.csv;*.tsv;*.txt"
				})
				{
					if (openFileDialog.ShowDialog(this) == DialogResult.OK)
					{
						table.Text = File.ReadAllText(openFileDialog.FileName);
					}
				}
			};
			flowLayoutPanel2.Controls.AddRange(new Control[2] { button, button2 });
			flowLayoutPanel.Controls.Add(flowLayoutPanel2);
			family.SelectedIndexChanged += delegate
			{
				if (!Restoring && !updating)
				{
					updating = true;
					try
					{
						string kind = ((family.Text == "Angle" || family.Text == "Double angle") ? "Angle bar" : family.Text);
						profile.SelectedItem = SteelCatalog.Profiles.First((SteelProfile p) => p.Kind == kind).Name;
						if (family.Text.StartsWith("HSS"))
						{
							Inputs["Connection"].Text = "Weld";
						}
					}
					finally
					{
						updating = false;
					}
					UpdateProperties();
				}
			};
			profile.SelectedIndexChanged += delegate
			{
				if (!Restoring && !updating)
				{
					UpdateProperties();
				}
			};
			verified.CheckedChanged += delegate
			{
				if (!Restoring)
				{
					UpdateProperties();
				}
			};
			profile.SelectedItem = "L75x75x6mm (PNS)";
			UpdateProperties();
		}

		private void UpdateProperties()
		{
			SteelProfile steelProfile = SteelCatalog.Find(profile.Text);
			if (steelProfile != null)
			{
				double num;
				double num2;
				if (steelProfile.Kind == "Angle bar")
				{
					num = (steelProfile.H + steelProfile.B - steelProfile.T) * steelProfile.T;
					num2 = (steelProfile.B * steelProfile.T * steelProfile.B / 2.0 + (steelProfile.H - steelProfile.T) * steelProfile.T * steelProfile.T / 2.0) / num;
				}
				else if (steelProfile.Kind == "HSS rectangle")
				{
					num = steelProfile.H * steelProfile.B - (steelProfile.H - 2.0 * steelProfile.T) * (steelProfile.B - 2.0 * steelProfile.T);
					num2 = (steelProfile.B * steelProfile.B + 2.0 * steelProfile.B * steelProfile.H) / (4.0 * (steelProfile.B + steelProfile.H));
				}
				else
				{
					num = Math.PI * (steelProfile.H * steelProfile.H - Math.Pow(steelProfile.H - 2.0 * steelProfile.T, 2.0)) / 4.0;
					num2 = steelProfile.H / Math.PI;
				}
				if (!verified.Checked)
				{
					area.Text = num.ToString("R", CultureInfo.InvariantCulture);
					thickness.Text = steelProfile.T.ToString("R", CultureInfo.InvariantCulture);
					offset.Text = num2.ToString("R", CultureInfo.InvariantCulture);
				}
				TextBox[] array = new TextBox[3] { area, thickness, offset };
				for (int i = 0; i < array.Length; i++)
				{
					array[i].ReadOnly = !verified.Checked;
				}
			}
		}

		protected override void AfterRestore()
		{
			UpdateProperties();
			((TextBox)Inputs["Fnv"]).ReadOnly = !((CheckBox)Inputs["VerifiedFnv"]).Checked;
		}

		protected override string BuildReport()
		{
			SteelProfile steelProfile = SteelCatalog.Find(profile.Text);
			string text = ((family.Text == "Angle" || family.Text == "Double angle") ? "Angle bar" : family.Text);
			if (steelProfile == null || steelProfile.Kind != text)
			{
				throw new Exception("Select a handbook profile matching the member family.");
			}
			double num = Number("Tension");
			double num2 = Number("Compression");
			if (num < 0.0 || num2 < 0.0)
			{
				throw new Exception("Enter nonnegative demand magnitudes.");
			}
			string text2 = "Manual axial demand magnitudes.\n";
			if (((CheckBox)Inputs["UseStaad"]).Checked)
			{
				if (!((CheckBox)Inputs["LoadBasis"]).Checked)
				{
					throw new Exception("Confirm the STAAD load selection matches LRFD/ASD demand basis.");
				}
				List<StaadForceRow> list = StaadForces.Select(StaadForces.Parse(table.Text, Value("ForceUnit"), Value("MomentUnit")), Value("Members"));
				double num3 = list.Max((StaadForceRow r) => Math.Abs(r.Fx));
				num = (num2 = num3);
				text2 = "STAAD: end-sign-independent absolute Fx envelope = " + num3.ToString("0.###", CultureInfo.InvariantCulture) + "kN applied conservatively to the listed axial checks.\n";
				foreach (StaadForceRow item in list)
				{
					text2 += string.Format(CultureInfo.InvariantCulture, "Member {0}, node {1}, LC {2}: Fx {3:0.######}kN; Fy/Fz {4:0.######}/{5:0.######}kN; Mx/My/Mz {6:0.######}/{7:0.######}/{8:0.######}kN-m\n", item.Member, item.Node, item.LoadCase, item.Fx, item.Fy, item.Fz, item.Mx, item.My, item.Mz);
				}
				if (list.Any((StaadForceRow r) => Math.Abs(r.Fy) + Math.Abs(r.Fz) + Math.Abs(r.Mx) + Math.Abs(r.My) + Math.Abs(r.Mz) > 1E-08))
				{
					text2 += "NONAXIAL COMPONENTS PRESENT: this sheet does not check combined shear/moment/eccentric demand; listed axial results are not a complete connection approval.\n";
				}
			}
			double num4 = Math.Max(num, num2);
			if (num4 <= 0.0)
			{
				throw new Exception("Enter a positive axial demand or import matching STAAD forces.");
			}
			ConnectionInput input = new ConnectionInput
			{
				Family = family.Text,
				Method = Value("Method"),
				Connection = Value("Connection"),
				Grade = Value("Grade"),
				Area = Number("Area"),
				Thickness = Number("Thickness"),
				Offset = Number("Offset"),
				Fy = Number("Fy"),
				Fu = Number("Fu"),
				GussetFy = Number("GussetFy"),
				GussetFu = Number("GussetFu"),
				GussetThickness = Number("GussetThickness"),
				GussetWidth = Number("GussetWidth"),
				Demand = num4,
				VerifiedU = Number("U"),
				AllElementsConnected = ((CheckBox)Inputs["AllConnected"]).Checked,
				VerifiedProperties = verified.Checked,
				Weld = Number("Weld"),
				WeldEdgeThickness = Number("WeldEdge"),
				Fexx = Number("Fexx"),
				LT = Number("LT"),
				LL1 = Number("LL1"),
				LL2 = Number("LL2"),
				DirectionalWeldIncrease = ((CheckBox)Inputs["Directional"]).Checked,
				Bolts = Whole("Bolts"),
				ShearPlanes = int.Parse(Value("ShearPlanes")),
				BoltDiameter = Number("Diameter"),
				HoleDiameter = Number("Hole"),
				EdgeDistance = Number("Edge"),
				Spacing = Number("BoltSpacing"),
				Fnv = Number("Fnv"),
				Exposed = ((CheckBox)Inputs["Exposed"]).Checked,
				Agv = Number("Agv"),
				Anv = Number("Anv"),
				Ant = Number("Ant")
			};
			return TrussConnectionDesigner.Report(input, TrussConnectionDesigner.Calculate(input), steelProfile.Name, steelProfile.Source) + "\nSTAAD / DEMAND AUDIT\n" + text2 + "Bentley sign reference: https://bentleysystems.service-now.com/community?id=kb_article_view&sysparm_article=KB0115216";
		}

		private int Whole(string key)
		{
			double num = Number(key);
			if (num != Math.Truncate(num) || num < 1.0 || num > 100000.0)
			{
				throw new Exception(key + " must be a positive whole number.");
			}
			return (int)num;
		}
	}
}
