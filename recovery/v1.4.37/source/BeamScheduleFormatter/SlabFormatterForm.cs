using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal class SlabFormatterForm : Form
	{
		private readonly DataGridView grid = new DataGridView
		{
			Dock = DockStyle.Fill,
			ReadOnly = true,
			AllowUserToAddRows = false,
			RowHeadersVisible = false,
			AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells
		};

		private readonly TextBox prefix = new TextBox
		{
			Width = 85,
			Text = "2S",
			CharacterCasing = CharacterCasing.Upper
		};

		private readonly TextBox floor = new TextBox
		{
			Width = 240,
			Text = "SECOND FLOOR",
			CharacterCasing = CharacterCasing.Upper
		};

		private readonly CheckBox merge = new CheckBox
		{
			Text = "Merge identical rows",
			Checked = true,
			AutoSize = true
		};

		private readonly Label source = new Label
		{
			Dock = DockStyle.Top,
			Height = 45,
			Padding = new Padding(12),
			Text = "Loaded DXF: none"
		};

		private readonly Label status = new Label
		{
			Dock = DockStyle.Bottom,
			Height = 34,
			Padding = new Padding(10)
		};

		private readonly PictureBox cad = new PictureBox
		{
			SizeMode = PictureBoxSizeMode.AutoSize
		};

		private readonly Button export = new Button
		{
			Text = "Export revised DXF",
			AutoSize = true,
			Enabled = false
		};

		private string raw;

		private SlabFormatResult result;

		private bool busy;

		public SlabFormatterForm()
		{
			if (!AccessCodes.CanUseApp(64))
			{
				throw new Exception("Schedule Formatter access required.");
			}
			AppBrand.Apply(this);
			Text = "Slab Schedule Formatter | DTCE Tool Kit";
			base.Size = new Size(1420, 900);
			MinimumSize = new Size(1100, 700);
			Font = new Font("Segoe UI", 9.5f);
			base.StartPosition = FormStartPosition.CenterScreen;
			Label value = new Label
			{
				Text = "SLAB SCHEDULE FORMATTER",
				Dock = DockStyle.Top,
				Height = 54,
				Padding = new Padding(12),
				Font = new Font("Segoe UI", 18f, FontStyle.Bold),
				ForeColor = PowTheme.Ink
			};
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				Height = 88,
				Padding = new Padding(8),
				AutoScroll = true
			};
			Button button = new Button
			{
				Text = "Load source DXF",
				AutoSize = true
			};
			button.Click += delegate
			{
				Choose();
			};
			flowLayoutPanel.Controls.Add(button);
			flowLayoutPanel.Controls.Add(new Label
			{
				Text = "Prefix",
				AutoSize = true,
				Padding = new Padding(4, 8, 0, 0)
			});
			flowLayoutPanel.Controls.Add(prefix);
			flowLayoutPanel.Controls.Add(new Label
			{
				Text = "Floor title",
				AutoSize = true,
				Padding = new Padding(4, 8, 0, 0)
			});
			flowLayoutPanel.Controls.Add(floor);
			flowLayoutPanel.Controls.Add(merge);
			Button button2 = new Button
			{
				Text = "Build preview",
				AutoSize = true
			};
			button2.Click += delegate
			{
				Build();
			};
			flowLayoutPanel.Controls.Add(button2);
			flowLayoutPanel.Controls.Add(export);
			export.Click += delegate
			{
				Export();
			};
			Label value2 = new Label
			{
				Dock = DockStyle.Top,
				Height = 47,
				Padding = new Padding(10),
				Text = "Matching thickness and all 9 reinforcement cells are grouped. New marks use prefix-001; original slab marks appear outside the table on the right. Headers follow your slab template; body text is regular. No stirrup processing."
			};
			TabControl tabControl = new TabControl
			{
				Dock = DockStyle.Fill
			};
			TabPage tabPage = new TabPage("Schedule values");
			TabPage tabPage2 = new TabPage("DXF preview");
			Panel panel = new Panel
			{
				Dock = DockStyle.Fill,
				AutoScroll = true,
				BackColor = Color.FromArgb(33, 40, 46)
			};
			panel.Controls.Add(cad);
			tabPage2.Controls.Add(panel);
			tabPage.Controls.Add(grid);
			tabControl.TabPages.Add(tabPage);
			tabControl.TabPages.Add(tabPage2);
			foreach (string item in new string[2] { "New slab mark", "Thickness mm" }.Concat(SlabScheduleReader.Columns).Concat(new string[1] { "Original slab marks" }))
			{
				grid.Columns.Add(item, item);
			}
			grid.Columns[0].Width = 115;
			grid.Columns[0].Frozen = true;
			grid.Columns[1].Width = 95;
			for (int num = 2; num < 11; num++)
			{
				grid.Columns[num].Width = 155;
			}
			grid.Columns[11].Visible = false;
			TextBox originals = new TextBox
			{
				Dock = DockStyle.Bottom,
				Height = 62,
				Multiline = true,
				ReadOnly = true,
				Font = new Font("Consolas", 10f),
				ScrollBars = ScrollBars.Vertical
			};
			tabPage.Controls.Add(originals);
			grid.SelectionChanged += delegate
			{
				originals.Text = ((grid.CurrentRow == null) ? "" : ("Original slab marks: " + Convert.ToString(grid.CurrentRow.Cells[11].Value)));
			};
			grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
			GridAppearance.Apply(grid);
			base.Controls.Add(tabControl);
			base.Controls.Add(value2);
			base.Controls.Add(source);
			base.Controls.Add(flowLayoutPanel);
			base.Controls.Add(value);
			base.Controls.Add(status);
			prefix.Leave += delegate
			{
				string text = prefix.Text.Trim().ToUpperInvariant();
				if (Regex.IsMatch(text, "^\\d+S$"))
				{
					floor.Text = Engine.FloorFor(text.Substring(0, text.Length - 1) + "B");
				}
			};
			prefix.TextChanged += delegate
			{
				InvalidateOutput();
			};
			floor.TextChanged += delegate
			{
				InvalidateOutput();
			};
			merge.CheckedChanged += delegate
			{
				InvalidateOutput();
			};
			base.FormClosed += delegate
			{
				if (cad.Image != null)
				{
					cad.Image.Dispose();
				}
			};
		}

		private void Guard()
		{
			if (!AccessCodes.CanUseApp(64))
			{
				throw new Exception("Renew Schedule Formatter access.");
			}
		}

		private void InvalidateOutput()
		{
			result = null;
			export.Enabled = false;
			grid.Rows.Clear();
			if (cad.Image != null)
			{
				cad.Image.Dispose();
				cad.Image = null;
			}
			status.Text = "Build preview to apply the current prefix, floor title and grouping.";
		}

		private void Choose()
		{
			try
			{
				Guard();
				using (OpenFileDialog openFileDialog = new OpenFileDialog
				{
					Filter = "Slab schedule DXF|*.dxf"
				})
				{
					if (openFileDialog.ShowDialog(this) == DialogResult.OK)
					{
						string text = File.ReadAllText(openFileDialog.FileName);
						SlabScheduleReader.Read(text);
						raw = text;
						source.Text = "Loaded DXF: " + Path.GetFileName(openFileDialog.FileName);
						InvalidateOutput();
						Build();
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.Message, "Slab source");
			}
		}

		internal void LoadSource(string input, string name)
		{
			SlabScheduleReader.Read(input);
			raw = input;
			source.Text = "Loaded DXF: " + name;
			Build();
		}

		private void Build()
		{
			if (busy)
			{
				return;
			}
			busy = true;
			try
			{
				Guard();
				if (raw == null)
				{
					throw new Exception("Load an original slab schedule DXF first.");
				}
				InvalidateOutput();
				SlabFormatResult slabFormatResult = SlabFormatter.Build(raw, prefix.Text, floor.Text, merge.Checked);
				foreach (SlabFormatGroup group in slabFormatResult.Groups)
				{
					object[] array = new object[12]
					{
						group.Mark,
						group.Schedule.Thickness.ToString("0.00"),
						null,
						null,
						null,
						null,
						null,
						null,
						null,
						null,
						null,
						null
					};
					for (int i = 0; i < 9; i++)
					{
						array[i + 2] = SlabFormatter.BarText(group.Schedule.Bars[i]).Replace("%%C", "Ø");
					}
					array[11] = group.Originals;
					grid.Rows.Add(array);
				}
				cad.Image = Preview.Render(slabFormatResult.Drawing);
				result = slabFormatResult;
				export.Enabled = true;
				status.Text = slabFormatResult.SourceCount + " original slab marks > " + slabFormatResult.Groups.Count + " output rows | " + prefix.Text + " | " + floor.Text;
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.Message, "Slab preview");
			}
			finally
			{
				busy = false;
			}
		}

		private void Export()
		{
			try
			{
				Guard();
				if (result == null)
				{
					throw new Exception("Build preview first.");
				}
				using (SaveFileDialog saveFileDialog = new SaveFileDialog
				{
					Filter = "Revised slab DXF|*.dxf",
					FileName = prefix.Text + " Slab Schedule - Revised.dxf"
				})
				{
					if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
					{
						SlabFormatter.Export(result, saveFileDialog.FileName);
						status.Text = "Exported: " + saveFileDialog.FileName;
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.Message, "Slab export");
			}
		}
	}
}
