using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal class SlabEntryOptionsForm : Form
	{
		private readonly ComboBox type;

		private readonly TextBox[] fields = new TextBox[7];

		private readonly Label[] labels = new Label[7];

		private readonly CheckBox manual;

		private readonly Label explanation;

		private readonly SlabScheduleRow slab;

		private readonly ToolTip help = new ToolTip
		{
			ShowAlways = true,
			InitialDelay = 350,
			ReshowDelay = 150,
			AutoPopDelay = 20000
		};

		public string SelectedType
		{
			get
			{
				return Convert.ToString(type.SelectedItem);
			}
		}

		public string[] Values { get; private set; }

		public SlabEntryOptionsForm(string support, string[] initial, SlabScheduleRow schedule)
		{
			slab = schedule;
			Text = "Slab type and optional settings";
			base.ClientSize = new Size(760, 655);
			base.StartPosition = FormStartPosition.CenterParent;
			base.FormBorderStyle = FormBorderStyle.FixedDialog;
			base.MaximizeBox = false;
			base.MinimizeBox = false;
			Font = new Font("Segoe UI", 10f);
			BackColor = Color.FromArgb(245, 248, 251);
			base.Controls.Add(new Label
			{
				Text = "Piliin ang slab type. Automatic ang reinforcement settings mula sa DXF.\nAng disabled fields ay hindi kailangang sagutan. I-hover ang label para sa paliwanag.",
				Location = new Point(20, 15),
				Size = new Size(720, 48)
			});
			Label label = new Label
			{
				Text = "Slab type",
				Location = new Point(20, 72),
				Size = new Size(120, 25)
			};
			base.Controls.Add(label);
			type = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				Location = new Point(145, 68),
				Width = 570
			};
			type.Items.AddRange(SlabEntryOptions.Types.Cast<object>().ToArray());
			type.SelectedItem = (SlabEntryOptions.Types.Contains(support) ? support : "Elevated");
			base.Controls.Add(type);
			explanation = new Label
			{
				Location = new Point(20, 108),
				Size = new Size(710, 45),
				ForeColor = Color.FromArgb(24, 75, 95)
			};
			base.Controls.Add(explanation);
			manual = new CheckBox
			{
				Text = "Baguhin ang automatic reinforcement settings (optional)",
				Location = new Point(20, 155),
				Size = new Size(710, 30),
				Checked = SlabEntryOptions.IsManual(initial, slab)
			};
			base.Controls.Add(manual);
			string[] array = new string[7] { "Haba ng exposed cantilever edges, m bawat panel", "Continuous sides sa LONG support (automatic)", "End sides sa LONG support (automatic)", "Continuous sides sa SHORT support (automatic)", "End sides sa SHORT support (automatic)", "Katabing panel span: short direction, m", "Katabing panel span: long direction, m" };
			string[] array2 = new string[7] { "Para lang sa cantilever-edge forms. Kabuuang haba ng free edges ng ISANG panel. Forms area = haba x thickness x quantity. Kapag elevated o gravel fill, disabled ito at zero ang gagamitin.", "Bilang ng continuous supports sa long edge. Galing sa DXF: may CONT at END = isa bawat uri. Baguhin lang para hindi madoble ang shared bars ng dalawang slab entries.", "Bilang ng end supports sa long edge. Automatic sa DXF. Hindi ito bilang ng rebars at hindi ito beam quantity.", "Bilang ng continuous supports sa short edge. Automatic sa DXF. Baguhin lang kung na-estimate na ang shared support sa ibang entry.", "Bilang ng end supports sa short edge. Automatic sa DXF. Ang 0/1/2 ay bilang ng supports na kasama sa takeoff.", "Optional kung mas maikli o mahaba ang katabing panel sa shared support. Kapag blank at automatic, gagamitin ang Short Side ng current panel para sa kabilang L/4 extension.", "Optional para sa unequal neighboring panel. Kapag blank at automatic, gagamitin ang Long Side ng current panel. Hindi ito kailangang sagutan sa normal inputs." };
			for (int i = 0; i < 7; i++)
			{
				labels[i] = new Label
				{
					Text = array[i],
					Location = new Point(20, 195 + i * 46),
					Size = new Size(570, 40)
				};
				fields[i] = new TextBox
				{
					Text = initial[i],
					Location = new Point(605, 195 + i * 46),
					Width = 125
				};
				base.Controls.Add(labels[i]);
				base.Controls.Add(fields[i]);
				help.SetToolTip(labels[i], array2[i]);
				help.SetToolTip(fields[i], array2[i]);
			}
			help.SetToolTip(type, "Elevated: bottom/soffit forms. Gravel fill: no bottom or exposed-edge forms. Cantilever edge: soffit plus specified edge forms. This changes formwork only; reinforcement still follows the DXF schedule.");
			help.SetToolTip(label, help.GetToolTip(type));
			help.SetToolTip(manual, "Leave unchecked for normal estimates. Enables the support and neighboring-span overrides only when you intentionally need to change the DXF defaults.");
			base.Controls.Add(new Label
			{
				Text = "Hover the labels even when their input is disabled.\nA slab-type change does not remove reinforcement bars from the schedule.",
				Location = new Point(20, 525),
				Size = new Size(710, 55)
			});
			Button button = new Button
			{
				Text = "Apply",
				Location = new Point(605, 600),
				Size = new Size(125, 35)
			};
			button.Click += delegate
			{
				try
				{
					Values = SlabEntryOptions.Prepare(SelectedType, fields.Select((TextBox f) => f.Text).ToArray(), manual.Checked, slab);
					base.DialogResult = DialogResult.OK;
					Close();
				}
				catch (Exception ex)
				{
					MessageBox.Show(this, ex.Message, "Check optional settings");
				}
			};
			Button button2 = new Button
			{
				Text = "Cancel",
				DialogResult = DialogResult.Cancel,
				Location = new Point(470, 600),
				Size = new Size(125, 35)
			};
			base.Controls.Add(button);
			base.Controls.Add(button2);
			base.AcceptButton = button;
			base.CancelButton = button2;
			type.SelectedIndexChanged += delegate
			{
				RefreshEnabled();
			};
			manual.CheckedChanged += delegate
			{
				RefreshEnabled();
			};
			base.FormClosed += delegate
			{
				help.Dispose();
			};
			RefreshEnabled();
		}

		private void RefreshEnabled()
		{
			bool flag = SelectedType == SlabEntryOptions.Types[1];
			fields[0].Enabled = flag;
			if (!flag)
			{
				fields[0].Text = "0";
			}
			string[] array = SlabEntryOptions.Defaults(slab);
			for (int i = 1; i < 7; i++)
			{
				fields[i].Enabled = manual.Checked;
				if (!manual.Checked)
				{
					fields[i].Text = array[i];
				}
			}
			explanation.Text = ((SelectedType == SlabEntryOptions.Types[2]) ? "Gravel fill: walang bottom forms at walang exposed-edge forms.\nReinforcement settings ay automatic pa rin mula sa DXF." : (flag ? "Cantilever edge: naka-on ang haba ng exposed edge. Bottom/soffit forms kasama.\nIbang inputs ay automatic maliban kung pipili ka ng manual override." : "Elevated: bottom/soffit forms lang; exposed-edge input ay OFF.\nIbang optional inputs ay OFF at gumagamit ng automatic DXF defaults."));
		}
	}
}
