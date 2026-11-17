using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal class StructureMemberDialog : Form
	{
		private readonly Dictionary<int, TextBox> inputs = new Dictionary<int, TextBox>();

		private readonly ComboBox type = new ComboBox
		{
			DropDownStyle = ComboBoxStyle.DropDownList
		};

		public string[] Values;

		public StructureMemberDialog(ChamberTakeoff owner, string[] existing, LengthConfiguration cfg)
		{
			StructureMemberDialog structureMemberDialog = this;
			Text = "Structure member | Dimensions and reinforcement";
			base.ClientSize = new Size(1050, 780);
			MinimumSize = new Size(850, 650);
			base.StartPosition = FormStartPosition.CenterParent;
			Font = new Font("Segoe UI", 10f);
			TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				AutoScroll = true,
				ColumnCount = 4,
				Padding = new Padding(12)
			};
			for (int i = 0; i < 4; i++)
			{
				tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, (i % 2 == 0) ? 31 : 19));
			}
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
				AutoSize = true,
				Height = 34,
				DialogResult = DialogResult.Cancel
			};
			flowLayoutPanel.Controls.Add(button);
			flowLayoutPanel.Controls.Add(button2);
			base.Controls.Add(tableLayoutPanel);
			base.Controls.Add(flowLayoutPanel);
			string[] array = new string[24]
			{
				Guid.NewGuid().ToString("N"),
				owner.Id,
				"Slab on fill",
				"",
				owner.L.ToString(CultureInfo.InvariantCulture),
				owner.W.ToString(CultureInfo.InvariantCulture),
				(owner.Base * 1000.0).ToString(CultureInfo.InvariantCulture),
				"1",
				"40",
				"1",
				"10",
				"",
				"",
				"10",
				"",
				"",
				"0",
				"0",
				"0",
				"",
				"100",
				"1",
				(owner.Kind == "Catch Basin (CHB)" || owner.Kind == "Lined Canal") ? "0" : "1",
				"6"
			};
			string[] initial = existing ?? array;
			string[] array2 = new string[24]
			{
				"", "", "Member type", "Description", "Length m", "Width / height m", "Thickness mm", "Members per structure", "Concrete cover mm", "Rebar layers (1 or 2)",
				"X / vertical diameter mm", "X / vertical bar spacing mm (or use bars per layer)", "X / vertical bars per layer", "Y / horizontal diameter mm", "Y / horizontal bar spacing mm (or use bars per layer)", "Y / horizontal bars per layer", "X / vertical extra ends mm (total)", "Y / horizontal extra ends mm (total)", "Formed main faces (0/1/2)", "Formed edge length m (blank=full)",
				"Gravel thickness mm (on fill)", "Plaster faces (0/1/2)", "Waterproof faces (0/1/2)", "Commercial rebar length m"
			};
			ComboBox.ObjectCollection items = type.Items;
			object[] types = StructureMemberInput.Types;
			items.AddRange(types);
			type.SelectedItem = initial[2];
			for (int j = 2; j < 24; j++)
			{
				int num = j - 2;
				tableLayoutPanel.Controls.Add(new Label
				{
					Text = array2[j],
					AutoSize = true,
					Anchor = AnchorStyles.Left,
					Margin = new Padding(5, 10, 5, 8)
				}, num % 2 * 2, num / 2);
				Control control;
				if (j == 2)
				{
					control = type;
					type.Dock = DockStyle.Top;
				}
				else
				{
					TextBox textBox = new TextBox
					{
						Text = initial[j],
						Dock = DockStyle.Top,
						Margin = new Padding(5, 7, 12, 7)
					};
					inputs[j] = textBox;
					control = textBox;
				}
				tableLayoutPanel.Controls.Add(control, num % 2 * 2 + 1, num / 2);
			}
			type.SelectedIndexChanged += delegate
			{
				string text = structureMemberDialog.type.Text;
				structureMemberDialog.inputs[5].Text = ((text == "RC wall" || text == "CHB wall") ? owner.H : owner.W).ToString(CultureInfo.InvariantCulture);
				structureMemberDialog.inputs[6].Text = (((text == "RC wall" || text == "CHB wall") ? owner.Wall : owner.Base) * 1000.0).ToString(CultureInfo.InvariantCulture);
				structureMemberDialog.inputs[18].Text = ((text == "RC wall") ? "2" : ((text == "Slab") ? "1" : "0"));
				structureMemberDialog.inputs[20].Text = ((text == "Slab on fill") ? "100" : "0");
				if (text == "CHB wall")
				{
					structureMemberDialog.inputs[9].Text = "1";
				}
			};
			if (owner.Kind == "Catch Basin (CHB)")
			{
				inputs[22].Text = "0";
				inputs[22].ReadOnly = true;
			}
			Label control2 = new Label
			{
				AutoSize = true,
				MaximumSize = new Size(960, 0),
				Padding = new Padding(4, 12, 4, 4),
				Text = "Solid rectangular members only: split around openings and avoid double-counting corners. Slab direction X runs along L; wall vertical bars run along height. Enter spacing OR bar count per direction, plus approved hook/anchorage allowances. Gravel comes from slab-on-fill rows. Extra rebar is added separately; review any legacy cuts before adding members."
			};
			tableLayoutPanel.Controls.Add(control2, 0, 11);
			tableLayoutPanel.SetColumnSpan(control2, 4);
			button.Click += delegate
			{
				try
				{
					structureMemberDialog.Values = (string[])initial.Clone();
					structureMemberDialog.Values[2] = structureMemberDialog.type.Text;
					foreach (KeyValuePair<int, TextBox> input in structureMemberDialog.inputs)
					{
						structureMemberDialog.Values[input.Key] = input.Value.Text.Trim();
					}
					StructureMemberTakeoff.Calculate(StructureMemberInput.Read(structureMemberDialog.Values), owner.Count, cfg);
					structureMemberDialog.DialogResult = DialogResult.OK;
					structureMemberDialog.Close();
				}
				catch (Exception ex)
				{
					MessageBox.Show(structureMemberDialog, ex.Message, "Review member inputs");
				}
			};
			base.AcceptButton = button;
			base.CancelButton = button2;
			PowTheme.Apply(this);
		}
	}
}
