using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal class PurlinDesignForm : DesignSheetForm
	{
		private readonly ComboBox profiles;

		private readonly CheckBox verified;

		private readonly TextBox ix;

		private readonly TextBox iy;

		private readonly TextBox ixy;

		private readonly TextBox sx;

		private readonly TextBox sy;

		public PurlinDesignForm()
			: base("Purlin Design | Workbook ASD checks", "Purlin-Design")
		{
			FlowLayoutPanel panel = Section("Geometry & loads", "Based on PS Design of Purlin.xlsm. Signed wind normal to roof; negative values represent uplift/suction. This sheet evaluates the listed workbook flexure/deflection checks, not the complete cold-formed member design.");
			NumberField(panel, "Name", "Member / purlin name", "Purlin");
			profiles = Choice(panel, "Profile", "Regan handbook purlin profile", (from p in SteelCatalog.Profiles
				where p.Kind == "Purlin"
				select p.Name).ToArray());
			Choice(panel, "Support", "Support", new string[2] { "Simply supported", "Cantilever" });
			NumberField(panel, "Span", "Span / cantilever length m", "5");
			NumberField(panel, "Spacing", "Purlin spacing m", "0.4");
			NumberField(panel, "Angle", "Roof angle deg", "13");
			Choice(panel, "SagRods", "Effective sag rods", new string[3] { "0", "1", "2" });
			Choice(panel, "Ceiling", "Ceiling type", new string[3] { "Not supporting ceiling", "Supporting nonplaster ceiling", "Supporting plaster ceiling" });
			NumberField(panel, "Dead", "Superimposed dead load kPa", "0.35");
			NumberField(panel, "Live", "Roof live load kPa", "1");
			NumberField(panel, "WindPressure", "Signed pressure kPa", "0");
			NumberField(panel, "WindSuction", "Signed suction kPa", "0");
			NumberField(panel, "Fy", "Specified yield strength MPa", "248");
			NumberField(panel, "E", "Modulus of elasticity MPa", "200000");
			Toggle(panel, "AlternateIncrease", "Workbook alternate 4/3 allowable increase", true);
			FlowLayoutPanel panel2 = Section("Section properties", "Handbook PDF pp.43-44 supplies dimensions and mass only. Auto properties idealize nonoverlapping rectangular plates and neglect bend radii. For final use enter verified properties with the same geometric axes/centroid; nonzero Ixy must be retained for Z shapes. Additional buckling, shear and connection checks remain outside this sheet.");
			verified = Toggle(panel2, "Verified", "Use verified section properties", false);
			ix = NumberField(panel2, "Ix", "Ix mm4", "1");
			iy = NumberField(panel2, "Iy", "Iy mm4", "1");
			ixy = NumberField(panel2, "Ixy", "Ixy mm4 (signed)", "0");
			sx = NumberField(panel2, "Sx", "Sx mm3", "1");
			sy = NumberField(panel2, "Sy", "Sy mm3", "1");
			profiles.SelectedIndexChanged += delegate
			{
				if (!Restoring)
				{
					verified.Checked = false;
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
			UpdateProperties();
		}

		private void UpdateProperties()
		{
			PurlinProperties purlinProperties = PurlinProperties.Calculate(SteelCatalog.Find(profiles.Text));
			if (!verified.Checked)
			{
				ix.Text = purlinProperties.Ix.ToString("R", CultureInfo.InvariantCulture);
				iy.Text = purlinProperties.Iy.ToString("R", CultureInfo.InvariantCulture);
				ixy.Text = purlinProperties.Ixy.ToString("R", CultureInfo.InvariantCulture);
				sx.Text = purlinProperties.Sx.ToString("R", CultureInfo.InvariantCulture);
				sy.Text = purlinProperties.Sy.ToString("R", CultureInfo.InvariantCulture);
			}
			TextBox[] array = new TextBox[5] { ix, iy, ixy, sx, sy };
			for (int i = 0; i < array.Length; i++)
			{
				array[i].ReadOnly = !verified.Checked;
			}
		}

		protected override void AfterRestore()
		{
			UpdateProperties();
		}

		protected override string BuildReport()
		{
			SteelProfile steelProfile = SteelCatalog.Find(Value("Profile"));
			PurlinProperties purlinProperties = PurlinProperties.Calculate(steelProfile);
			if (verified.Checked)
			{
				purlinProperties.Ix = Number("Ix");
				purlinProperties.Iy = Number("Iy");
				purlinProperties.Ixy = Number("Ixy");
				purlinProperties.Sx = Number("Sx");
				purlinProperties.Sy = Number("Sy");
				purlinProperties.Verified = true;
			}
			PurlinDesignInput input = new PurlinDesignInput
			{
				Name = Value("Name"),
				Profile = steelProfile,
				Properties = purlinProperties,
				Support = Value("Support"),
				Span = Number("Span"),
				Spacing = Number("Spacing"),
				Angle = Number("Angle"),
				SagRods = int.Parse(Value("SagRods")),
				Ceiling = Value("Ceiling"),
				Dead = Number("Dead"),
				Live = Number("Live"),
				WindPressure = Number("WindPressure"),
				WindSuction = Number("WindSuction"),
				Fy = Number("Fy"),
				E = Number("E"),
				AlternateIncrease = ((CheckBox)Inputs["AlternateIncrease"]).Checked
			};
			return PurlinDesigner.Report(input, PurlinDesigner.Calculate(input));
		}
	}
}
