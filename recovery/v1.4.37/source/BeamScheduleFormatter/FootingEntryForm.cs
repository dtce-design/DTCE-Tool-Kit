using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal class FootingEntryForm : Form
	{
		private readonly TextBox[] fields = new TextBox[28];

		private readonly ComboBox[] modes = new ComboBox[4];

		private readonly Label result = new Label
		{
			Dock = DockStyle.Bottom,
			Height = 100,
			Padding = new Padding(14)
		};

		public string[] Values;

		public FootingEntryForm(string[] initial)
		{
			AppBrand.Apply(this);
			Text = "Footing estimate | Entry";
			base.ClientSize = new Size(840, 670);
			MinimumSize = new Size(740, 600);
			Font = new Font("Segoe UI", 10f);
			base.StartPosition = FormStartPosition.CenterParent;
			TabControl tabs = new TabControl
			{
				Dock = DockStyle.Fill
			};
			base.Controls.Add(tabs);
			FlowLayoutPanel[] array = new string[3] { "Geometry & earthworks", "Reinforcement", "Materials" }.Select(delegate(string n)
			{
				TabPage tabPage = new TabPage(n);
				FlowLayoutPanel value = new FlowLayoutPanel
				{
					Dock = DockStyle.Fill,
					AutoScroll = true,
					Padding = new Padding(14),
					WrapContents = true
				};
				tabPage.Controls.Add(value);
				tabs.TabPages.Add(tabPage);
				return value;
			}).ToArray();
			string[] array2 = new string[28]
			{
				"Footing ID", "Description", "Length X (m)", "Width Y (m)", "Thickness (m)", "Identical footings (pcs)", "Excavation depth (m; blank = T+0.10)", "Clear cover (mm)", "Bottom X diameter (mm)", "Bottom X spacing (mm)",
				"Bottom X bars per footing", "Bottom Y diameter (mm)", "Bottom Y spacing (mm)", "Bottom Y bars per footing", "Top X diameter (mm)", "Top X spacing (mm)", "Top X bars per footing", "Top Y diameter (mm)", "Top Y spacing (mm)", "Top Y bars per footing",
				"Extra length, both ends total (m/bar)", "Side forms", "Suitable excavated soil (%)", "Soil swell (%)", "Bank shrinkage (%)", "Concrete strength MPa", "Grade <=12mm", "Grade >=16mm"
			};
			for (int num = 0; num < 28; num++)
			{
				fields[num] = new TextBox
				{
					Text = (initial[num] ?? ""),
					Width = 340
				};
			}
			int[] array3 = new int[8] { 0, 1, 2, 3, 4, 5, 6, 7 };
			foreach (int num3 in array3)
			{
				AddField(array[0], array2[num3], fields[num3]);
			}
			fields[21].ReadOnly = true;
			ComboBox forms = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				Width = 340
			};
			ComboBox.ObjectCollection items = forms.Items;
			object[] items2 = new string[2] { "Formed sides", "Earth cast" };
			items.AddRange(items2);
			forms.SelectedIndex = ((initial[21] == "Earth cast") ? 1 : 0);
			forms.SelectedIndexChanged += delegate
			{
				fields[21].Text = forms.Text;
			};
			fields[21].Text = forms.Text;
			AddField(array[0], "Formwork basis", forms);
			for (int num4 = 0; num4 < 4; num4++)
			{
				int b = 8 + 3 * num4;
				FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
				{
					Width = 370,
					Height = 220,
					FlowDirection = FlowDirection.TopDown,
					WrapContents = false
				};
				flowLayoutPanel.Controls.Add(new Label
				{
					Text = (new string[4] { "BOTTOM X - runs along L", "BOTTOM Y - runs along B", "TOP X - optional", "TOP Y - optional" })[num4],
					Font = new Font("Segoe UI", 10f, FontStyle.Bold),
					Width = 355,
					Height = 26
				});
				ComboBox mode = new ComboBox
				{
					Width = 340,
					DropDownStyle = ComboBoxStyle.DropDownList
				};
				ComboBox.ObjectCollection items3 = mode.Items;
				items2 = new string[3] { "Spacing (mm)", "Number of bars", "Not used" };
				items3.AddRange(items2);
				mode.SelectedIndex = (string.IsNullOrWhiteSpace(initial[b]) ? 2 : ((!string.IsNullOrWhiteSpace(initial[b + 2])) ? 1 : 0));
				modes[num4] = mode;
				flowLayoutPanel.Controls.Add(mode);
				flowLayoutPanel.Controls.Add(new Label
				{
					Text = "Diameter mm",
					Width = 340,
					Height = 22
				});
				flowLayoutPanel.Controls.Add(fields[b]);
				flowLayoutPanel.Controls.Add(new Label
				{
					Text = "Spacing mm OR number of bars",
					Width = 340,
					Height = 22
				});
				flowLayoutPanel.Controls.Add(fields[b + 1]);
				flowLayoutPanel.Controls.Add(fields[b + 2]);
				Action update = delegate
				{
					fields[b].Enabled = mode.SelectedIndex != 2;
					fields[b + 1].Visible = mode.SelectedIndex == 0;
					fields[b + 2].Visible = mode.SelectedIndex == 1;
				};
				mode.SelectedIndexChanged += delegate
				{
					update();
				};
				update();
				array[1].Controls.Add(flowLayoutPanel);
			}
			AddField(array[1], array2[20], fields[20]);
			array3 = new int[3] { 25, 26, 27 };
			foreach (int num5 in array3)
			{
				fields[num5].ReadOnly = true;
				AddField(array[2], array2[num5], fields[num5]);
			}
			array[2].Controls.Add(new Label
			{
				Text = "Change concrete strength and steel grades in Reinforced Concrete Works > Start > Foundation / Footing Settings. Soil factors are set in Earthworks & Gravel > Start.\nExcavation assumes 0.30m working space on each side and 0.10m gravel under the footprint. Backfill excludes concrete and gravel. Overall Earthworks shares reusable soil before buying borrow.",
				Width = 750,
				Height = 125
			});
			FlowLayoutPanel flowLayoutPanel2 = new FlowLayoutPanel
			{
				Dock = DockStyle.Bottom,
				Height = 56,
				Padding = new Padding(12),
				FlowDirection = FlowDirection.RightToLeft
			};
			Button button = new Button
			{
				Text = "Save footing",
				Width = 130,
				Height = 32
			};
			Button button2 = new Button
			{
				Text = "Calculate",
				Width = 110,
				Height = 32
			};
			Button button3 = new Button
			{
				Text = "Cancel",
				Width = 100,
				Height = 32,
				DialogResult = DialogResult.Cancel
			};
			flowLayoutPanel2.Controls.AddRange(new Control[3] { button, button2, button3 });
			base.Controls.Add(result);
			base.Controls.Add(flowLayoutPanel2);
			base.AcceptButton = button;
			base.CancelButton = button3;
			button2.Click += delegate
			{
				Evaluate(false);
			};
			button.Click += delegate
			{
				Evaluate(true);
			};
		}

		private static void AddField(FlowLayoutPanel parent, string label, Control input)
		{
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Width = 370,
				Height = 76,
				FlowDirection = FlowDirection.TopDown,
				WrapContents = false
			};
			flowLayoutPanel.Controls.Add(new Label
			{
				Text = label,
				Width = 355,
				Height = 27
			});
			flowLayoutPanel.Controls.Add(input);
			parent.Controls.Add(flowLayoutPanel);
		}

		private static double Number(string text, double fallback = 0.0)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return fallback;
			}
			double d;
			if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out d) || double.IsNaN(d) || double.IsInfinity(d))
			{
				throw new Exception("Enter finite numbers using a decimal point.");
			}
			return d;
		}

		private void Evaluate(bool save)
		{
			try
			{
				string[] array = fields.Select((TextBox f) => f.Text.Trim()).ToArray();
				FootingBarSpec[] array2 = new FootingBarSpec[4];
				for (int num = 0; num < 4; num++)
				{
					int num2 = 8 + 3 * num;
					if (modes[num].SelectedIndex == 2)
					{
						array[num2] = (array[num2 + 1] = (array[num2 + 2] = ""));
					}
					else
					{
						array[num2 + ((modes[num].SelectedIndex != 0) ? 1 : 2)] = "";
					}
					double num3 = Number(array[num2]);
					double num4 = Number(array[num2 + 2]);
					if (num3 != Math.Truncate(num3) || num4 != Math.Truncate(num4))
					{
						throw new Exception("Bar diameter and count must be whole numbers.");
					}
					array2[num] = new FootingBarSpec
					{
						Diameter = checked((int)num3),
						Spacing = Number(array[num2 + 1]),
						Count = checked((int)num4)
					};
				}
				double num5 = Number(array[5]);
				double num6 = Number(array[4]);
				if (num5 != Math.Truncate(num5))
				{
					throw new Exception("Footing quantity must be a whole number.");
				}
				int num7 = (int)Number(array[26]);
				int num8 = (int)Number(array[27]);
				LengthConfiguration configuration = CalculationStore.Resolve(Number(array[25]), num7, (num7 == 40) ? 275 : 420, num8, (num8 == 40) ? 275 : 420);
				FootingTakeoff footingTakeoff = FootingTakeoff.Calculate(array[0], array[1], Number(array[2]), Number(array[3]), num6, checked((int)num5), Number(array[6], num6 + 0.1), Number(array[7]), array[21] != "Earth cast", array2, Number(array[20]), configuration, Number(array[23]), Number(array[24]), Number(array[22]));
				result.ForeColor = PowTheme.Ink;
				result.Text = string.Format(CultureInfo.InvariantCulture, "Concrete {0:0.000} m3    Side formwork {1:0.000} m2    Net steel {2:0.00} kg\nExcavation {3:0.000} bank m3    Backfill space {4:0.000} compacted m3    Gravel {5:0.000} m3\nPurchase steel and loose embankment are calculated in the project backup.", footingTakeoff.Concrete, footingTakeoff.Forms, footingTakeoff.Steel.Result.Total, footingTakeoff.Excavation, footingTakeoff.Backfill, footingTakeoff.Gravel);
				if (save)
				{
					Values = array;
					base.DialogResult = DialogResult.OK;
				}
			}
			catch (Exception ex)
			{
				result.ForeColor = Color.Firebrick;
				result.Text = ex.Message;
			}
		}
	}
}
