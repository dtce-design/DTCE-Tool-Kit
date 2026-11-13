using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal class ChamberDimensionsDialog : Form
	{
		private readonly Dictionary<int, TextBox> inputs = new Dictionary<int, TextBox>();

		private readonly Dictionary<int, CheckBox> checks = new Dictionary<int, CheckBox>();

		private readonly string kind;

		public string[] Values;

		public ChamberDimensionsDialog(string type, string[] existing)
		{
			ChamberDimensionsDialog chamberDimensionsDialog = this;
			kind = type;
			Text = type + " | Dimensions";
			base.ClientSize = new Size(980, 760);
			MinimumSize = new Size(780, 600);
			base.StartPosition = FormStartPosition.CenterParent;
			Font = new Font("Segoe UI", 10f);
			AppBrand.Apply(this);
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Bottom,
				Height = 55,
				Padding = new Padding(8),
				FlowDirection = FlowDirection.RightToLeft
			};
			Button button = new Button
			{
				Text = "Save & compute",
				AutoSize = true,
				Height = 34
			};
			Button button2 = new Button
			{
				Text = "Cancel",
				DialogResult = DialogResult.Cancel,
				AutoSize = true,
				Height = 34
			};
			flowLayoutPanel.Controls.Add(button);
			flowLayoutPanel.Controls.Add(button2);
			TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				AutoScroll = true,
				ColumnCount = 4,
				Padding = new Padding(12)
			};
			for (int i = 0; i < 4; i++)
			{
				tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, (i % 2 == 0) ? 30 : 20));
			}
			base.Controls.Add(tableLayoutPanel);
			base.Controls.Add(flowLayoutPanel);
			Dictionary<int, string> defaults = new Dictionary<int, string>
			{
				{ 2, type },
				{ 6, "1" },
				{
					7,
					(type == "Catch Basin (CHB)") ? "0.1" : "0.15"
				},
				{ 8, "0.15" },
				{ 9, "0" },
				{ 10, "0" },
				{ 11, "0" },
				{ 12, "0" },
				{
					13,
					(type == "Catch Basin (CHB)") ? "0.1" : "0.15"
				},
				{ 14, "0" },
				{ 15, "0.1" },
				{ 16, "0.3" },
				{ 17, "0" },
				{ 18, "0" },
				{ 19, "0" },
				{ 20, "1" },
				{ 21, "False" },
				{
					22,
					(type == "Catch Basin (CHB)") ? "0" : "1"
				},
				{ 23, "False" },
				{
					24,
					(type == "Catch Basin (CHB)") ? "1" : "0"
				}
			};
			if (type == "Lined Canal")
			{
				defaults[13] = "0";
				defaults[22] = "0";
			}
			string[] array = new string[25]
			{
				"Structure ID", "Description", "", "Outside length m", "Outside width m", "Clear wall height m", "Structure count", "Wall thickness m", "Base thickness m", "RC cover thickness m (0=open)",
				"Cover opening length m", "Cover opening width m", "Net partition length m", "Partition thickness m", "External wall openings m2", "Gravel thickness m", "Working allowance m / side", "Soil cover m", "Additional RC m3 / structure", "Additional forms m2 / structure",
				"Plaster wall faces (1/2)", "Plaster floor", "Waterproof wall faces (1/2)", "Waterproof floor", "Septic quantity LS (manual)"
			};
			if (type == "Lined Canal")
			{
				array[20] = "Plaster wall faces (0/1/2)";
				array[22] = "Waterproof wall faces (0/1/2)";
			}
			int num = 0;
			for (int j = 0; j < 25; j++)
			{
				if (j == 2 || j == 24 || (type == "Catch Basin (CHB)" && (j == 22 || j == 23)) || (type == "Lined Canal" && new int[6] { 9, 10, 11, 12, 13, 17 }.Contains(j)))
				{
					continue;
				}
				string text = ((existing != null) ? existing[j] : (defaults.ContainsKey(j) ? defaults[j] : ""));
				Label control = new Label
				{
					Text = array[j],
					AutoSize = true,
					Anchor = AnchorStyles.Left,
					Margin = new Padding(5, 10, 5, 8)
				};
				int row = num / 2;
				int num2 = num % 2 * 2;
				tableLayoutPanel.Controls.Add(control, num2, row);
				Control control2;
				if (j == 21 || j == 23)
				{
					CheckBox checkBox = new CheckBox
					{
						Checked = text.Equals("True", StringComparison.OrdinalIgnoreCase),
						AutoSize = true,
						Anchor = AnchorStyles.Left
					};
					checks[j] = checkBox;
					control2 = checkBox;
				}
				else
				{
					TextBox textBox = new TextBox
					{
						Text = text,
						Dock = DockStyle.Top,
						Margin = new Padding(5, 7, 12, 7)
					};
					if ((type == "Septic Vault, CHB" || type == "Catch Basin (CHB)") && (j == 7 || j == 13))
					{
						textBox.ReadOnly = true;
					}
					inputs[j] = textBox;
					control2 = textBox;
				}
				tableLayoutPanel.Controls.Add(control2, num2 + 1, row);
				num++;
			}
			Label control3 = new Label
			{
				Text = ((type == "Lined Canal") ? "Open U-channel: two longitudinal walls, no cover/end walls. " : "") + "All dimensions are in metres. Save computes excavation, backfill space, gravel, concrete, forms, CHB and finishes. Outer dimensions define the excavation/backfill envelope. Use Members for slab, slab-on-fill, RC wall and CHB dimensions and bar spacing. Profile member dimensions are copied as saved; review Members if outer geometry changes. Extra rebar is added separately. Enter the final LS quantity in Bill of Quantities.",
				AutoSize = true,
				MaximumSize = new Size(880, 0),
				Padding = new Padding(4, 12, 4, 4)
			};
			tableLayoutPanel.Controls.Add(control3, 0, (num + 1) / 2);
			tableLayoutPanel.SetColumnSpan(control3, 4);
			button.Click += delegate
			{
				try
				{
					chamberDimensionsDialog.Values = (from c in Enumerable.Range(0, 25)
						select (existing == null) ? ((!defaults.ContainsKey(c)) ? "" : defaults[c]) : existing[c]).ToArray();
					chamberDimensionsDialog.Values[2] = chamberDimensionsDialog.kind;
					foreach (KeyValuePair<int, TextBox> input in chamberDimensionsDialog.inputs)
					{
						chamberDimensionsDialog.Values[input.Key] = input.Value.Text.Trim();
					}
					foreach (KeyValuePair<int, CheckBox> check in chamberDimensionsDialog.checks)
					{
						chamberDimensionsDialog.Values[check.Key] = (check.Value.Checked ? "True" : "False");
					}
					if (chamberDimensionsDialog.kind == "Catch Basin (CHB)")
					{
						chamberDimensionsDialog.Values[22] = "0";
						chamberDimensionsDialog.Values[23] = "False";
						chamberDimensionsDialog.Values[24] = chamberDimensionsDialog.Values[6];
					}
					Read(chamberDimensionsDialog.Values);
					chamberDimensionsDialog.DialogResult = DialogResult.OK;
					chamberDimensionsDialog.Close();
				}
				catch (Exception ex)
				{
					MessageBox.Show(chamberDimensionsDialog, ex.Message, "Review structure dimensions");
				}
			};
			base.AcceptButton = button;
			base.CancelButton = button2;
			PowTheme.Apply(this);
		}

		internal static ChamberTakeoff Read(string[] values)
		{
			return ChamberInputs.Read(values);
		}
	}
}
