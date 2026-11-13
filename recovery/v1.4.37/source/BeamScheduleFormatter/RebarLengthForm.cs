using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal class RebarLengthForm : Form
	{
		private NumericUpDown dia;

		private NumericUpDown fy;

		private NumericUpDown fySmall;

		private NumericUpDown fc;

		private NumericUpDown cover;

		private NumericUpDown spacing;

		private NumericUpDown ktr;

		private NumericUpDown asRatio;

		private NumericUpDown spliced;

		private NumericUpDown round;

		private NumericUpDown straight;

		private ComboBox grade;

		private ComboBox gradeSmall;

		private ComboBox concrete;

		private ComboBox method;

		private ComboBox sheetScope;

		private TextBox configName;

		private TextBox output;

		private CheckBox top;

		private CheckBox epoxy;

		private CheckBox light;

		private Label headline;

		private Label status;

		private DataGridView results;

		private DataGridView hooks;

		private DataGridView summary;

		private bool applying;

		public RebarLengthForm()
		{
			AppBrand.Apply(this);
			if (!AccessCodes.CanUseApp(4))
			{
				throw new Exception("This app is not included in your current access.");
			}
			base.Shown += delegate
			{
				InputFocus.Apply(this);
			};
			Text = "Splice, Development & Hook Calculator v1.4.37";
			base.ClientSize = new Size(1260, 850);
			MinimumSize = new Size(1120, 760);
			base.StartPosition = FormStartPosition.CenterParent;
			Font = new Font("Segoe UI", 9f);
			BackColor = Color.FromArgb(242, 245, 249);
			Panel panel = new Panel
			{
				Dock = DockStyle.Top,
				Height = 85,
				BackColor = Color.FromArgb(23, 43, 67)
			};
			panel.Controls.Add(new Label
			{
				Text = "SPLICE, DEVELOPMENT & HOOKS",
				UseMnemonic = false,
				Location = new Point(22, 12),
				Size = new Size(1100, 35),
				Font = new Font("Segoe UI", 19f, FontStyle.Bold),
				ForeColor = Color.White
			});
			panel.Controls.Add(new Label
			{
				Text = "Calculation workspace  /  SI units  /  Saved in app / automatic material matching in Beam Estimator",
				Location = new Point(24, 53),
				Size = new Size(1100, 25),
				ForeColor = Color.FromArgb(192, 212, 233)
			});
			FlowLayoutPanel tools = new FlowLayoutPanel
			{
				Dock = DockStyle.Bottom,
				Height = 83,
				Padding = new Padding(15, 10, 8, 6),
				BackColor = Color.White
			};
			tools.Controls.Add(new Label
			{
				Text = "Configuration",
				Width = 91,
				Height = 28,
				TextAlign = ContentAlignment.MiddleLeft
			});
			configName = new TextBox
			{
				Text = "FC21-G40-G60",
				Width = 210,
				Margin = new Padding(3, 5, 12, 3)
			};
			tools.Controls.Add(configName);
			Action<string, Action> obj = delegate(string text, Action run)
			{
				Button button = new Button
				{
					Text = text,
					Height = 34,
					Width = 140,
					FlatStyle = FlatStyle.Flat,
					BackColor = Color.FromArgb(235, 241, 248)
				};
				button.Click += delegate
				{
					if (!AccessCodes.CanUseApp(4))
					{
						return;
					}
					try
					{
						run();
					}
					catch (Exception ex)
					{
						MessageBox.Show(this, ex.Message, "Calculation");
					}
				};
				tools.Controls.Add(button);
			};
			obj("Save in app", delegate
			{
				LengthConfiguration lengthConfiguration = Configuration();
				CalculationStore.Save(lengthConfiguration);
				status.Text = "Saved in app: " + lengthConfiguration.Name + ". Estimator selects it automatically for matching materials.";
			});
			obj("Saved configurations", delegate
			{
				LengthConfiguration lengthConfiguration = CalculationStore.Choose(this);
				if (lengthConfiguration != null)
				{
					ApplyConfiguration(lengthConfiguration);
				}
			});
			obj("Calculation sheet PDF", delegate
			{
				using (SaveFileDialog saveFileDialog = new SaveFileDialog
				{
					Filter = "PDF (*.pdf)|*.pdf",
					FileName = "Splice-Development-Hooks.pdf"
				})
				{
					if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
					{
						EstimatePdf.Write(saveFileDialog.FileName, Sheet());
					}
				}
			});
			obj("Print Preview", delegate
			{
				CalculationPreview.Show(this, Sheet());
			});
			obj("Copy sheet", delegate
			{
				LoginForm.Copy(Sheet().Replace("## ", ""));
			});
			Panel panel2 = new Panel
			{
				Dock = DockStyle.Fill,
				Padding = new Padding(14, 12, 14, 10)
			};
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Left,
				Width = 405,
				FlowDirection = FlowDirection.TopDown,
				WrapContents = false,
				AutoScroll = true,
				Padding = new Padding(0, 0, 8, 0)
			};
			FlowLayoutPanel p = Group(flowLayoutPanel, "01  METHOD & SELECTED BAR", 150);
			method = Select(p, "Development method", new string[2] { "Sample: simplified other cases", "General: bond-ratio method" });
			dia = Number(p, "Selected diameter (mm)", 10m, 36m, 20m, 0);
			top = Check(p, "Top bar: >300mm fresh concrete below");
			p = Group(flowLayoutPanel, "02  MATERIAL STRENGTHS", 210);
			gradeSmall = Select(p, "Grade: 12mm & below", new string[2] { "Grade 40", "Grade 60" });
			fySmall = Number(p, "Actual fy <=12mm (MPa)", 200m, 420m, 275m, 0);
			grade = Select(p, "Grade: 16mm & above", new string[2] { "Grade 60", "Grade 40" });
			fy = Number(p, "Actual fy >=16mm (MPa)", 200m, 420m, 420m, 0);
			concrete = Select(p, "Concrete quick presets", new string[3] { "21 MPa", "28 MPa", "35 MPa" });
			fc = Number(p, "Actual f'c (MPa)", 17m, 70m, 21m, 1);
			p = Group(flowLayoutPanel, "03  BOND & CONCRETE CONDITIONS", 215);
			cover = Number(p, "Clear cover TO MAIN BAR (mm)", 0m, 500m, 40m, 1);
			spacing = Number(p, "Clear bar spacing (mm)", 1m, 1000m, 40m, 1);
			ktr = Number(p, "Ktr (mm; 0 if ignored)", 0m, 500m, 0m, 1);
			round = Number(p, "Round UP to (mm)", 1m, 100m, 5m, 0);
			epoxy = Check(p, "Epoxy-coated bars");
			light = Check(p, "Lightweight concrete: lambda = 0.75");
			p = Group(flowLayoutPanel, "04  TENSION LAP CLASS", 94);
			asRatio = Number(p, "As provided / As required", 0.1m, 20m, 1m, 2);
			spliced = Number(p, "Bars spliced at section (%)", 1m, 100m, 100m, 0);
			p = Group(flowLayoutPanel, "05  HOOK CUTTING GEOMETRY", 125);
			straight = Number(p, "Straight tangent leg (mm)", 0m, 20000m, 0m, 0);
			p.Controls.Add(new Label
			{
				Text = "For one hook's centerline cut only. This leg is excluded from estimator allowances.",
				Width = 350,
				Height = 36,
				ForeColor = Color.FromArgb(85, 101, 120)
			});
			Panel panel3 = new Panel
			{
				Dock = DockStyle.Fill,
				Padding = new Padding(14, 0, 0, 0)
			};
			headline = new Label
			{
				Dock = DockStyle.Top,
				Height = 74,
				Font = new Font("Segoe UI", 12f, FontStyle.Bold),
				ForeColor = Color.FromArgb(23, 43, 67),
				Padding = new Padding(8)
			};
			status = new Label
			{
				Dock = DockStyle.Bottom,
				Height = 56,
				Padding = new Padding(10, 5, 5, 0),
				ForeColor = Color.FromArgb(67, 84, 104),
				BackColor = Color.FromArgb(229, 237, 247)
			};
			TabControl tabControl = new TabControl
			{
				Dock = DockStyle.Fill,
				Font = new Font("Segoe UI", 10f)
			};
			TabPage tabPage = new TabPage("Selected bar")
			{
				BackColor = Color.White,
				Padding = new Padding(10)
			};
			TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				ColumnCount = 1,
				RowCount = 4
			};
			tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 64f));
			tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
			tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 36f));
			tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
			results = Table(new string[4] { "Calculation", "Minimum mm", "USE mm", "Basis" }, true);
			results.RowTemplate.Height = 28;
			results.ColumnHeadersHeight = 38;
			hooks = Table(new string[4] { "Hook", "Bend ID mm", "Tail mm", "Cut mm" }, true);
			hooks.RowTemplate.Height = 27;
			hooks.ColumnHeadersHeight = 36;
			tableLayoutPanel.Controls.Add(results, 0, 0);
			tableLayoutPanel.Controls.Add(new Label
			{
				Text = "STANDARD HOOK GEOMETRY  /  ONE HOOK",
				Dock = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleLeft,
				Font = new Font("Segoe UI", 10f, FontStyle.Bold)
			}, 0, 1);
			tableLayoutPanel.Controls.Add(hooks, 0, 2);
			tableLayoutPanel.Controls.Add(new Label
			{
				Text = "Ldh is hook embedment. Hook cut = straight tangent leg + centerline arc + tail. They are separate quantities.",
				Dock = DockStyle.Fill,
				Padding = new Padding(0, 8, 0, 0),
				ForeColor = Color.FromArgb(73, 91, 113)
			}, 0, 3);
			tabPage.Controls.Add(tableLayoutPanel);
			tabControl.TabPages.Add(tabPage);
			TabPage tabPage2 = new TabPage("All diameters")
			{
				Padding = new Padding(10),
				BackColor = Color.White
			};
			summary = Table(new string[10] { "db mm", "Ld other", "Ldh", "Lap A", "Lap B", "Ld top", "Top A", "Top B", "Ldc", "Comp lap" }, false);
			tabPage2.Controls.Add(summary);
			tabPage2.Controls.Add(new Label
			{
				Dock = DockStyle.Bottom,
				Height = 50,
				Text = "All lengths in mm, rounded UP. Estimator uses TOP values for top reinforcement and OTHER values for bottom bars / SFR.",
				Padding = new Padding(0, 8, 0, 0)
			});
			tabControl.TabPages.Add(tabPage2);
			TabPage tabPage3 = new TabPage("Calculation sheet")
			{
				Padding = new Padding(8)
			};
			output = new TextBox
			{
				Dock = DockStyle.Fill,
				Multiline = true,
				ReadOnly = true,
				ScrollBars = ScrollBars.Both,
				WordWrap = false,
				Font = new Font("Consolas", 10f),
				BackColor = Color.White,
				BorderStyle = BorderStyle.None
			};
			sheetScope = new ComboBox
			{
				Dock = DockStyle.Top,
				DropDownStyle = ComboBoxStyle.DropDownList,
				Font = new Font("Segoe UI", 10f)
			};
			sheetScope.Items.AddRange(new object[1] { "Complete sheet: ALL diameters (10, 12, 16, 20, 25, 28, 32, 36mm)" });
			sheetScope.SelectedIndex = 0;
			sheetScope.SelectedIndexChanged += delegate
			{
				UpdateResult();
			};
			tabPage3.Controls.Add(output);
			tabPage3.Controls.Add(sheetScope);
			tabControl.TabPages.Add(tabPage3);
			panel3.Controls.Add(tabControl);
			panel3.Controls.Add(status);
			panel3.Controls.Add(headline);
			panel2.Controls.Add(panel3);
			panel2.Controls.Add(flowLayoutPanel);
			base.Controls.Add(panel2);
			base.Controls.Add(tools);
			base.Controls.Add(panel);
			NumericUpDown[] array = new NumericUpDown[11]
			{
				dia, fy, fySmall, fc, cover, spacing, ktr, asRatio, spliced, round,
				straight
			};
			for (int num = 0; num < array.Length; num++)
			{
				array[num].ValueChanged += delegate
				{
					UpdateResult();
				};
			}
			CheckBox[] array2 = new CheckBox[3] { top, epoxy, light };
			for (int num = 0; num < array2.Length; num++)
			{
				array2[num].CheckedChanged += delegate
				{
					UpdateResult();
				};
			}
			method.SelectedIndexChanged += delegate
			{
				UpdateResult();
			};
			configName.TextChanged += delegate
			{
				UpdateResult();
			};
			grade.SelectedIndexChanged += delegate
			{
				if (!applying)
				{
					fy.Value = ((grade.SelectedIndex == 0) ? 420 : 275);
				}
			};
			gradeSmall.SelectedIndexChanged += delegate
			{
				if (!applying)
				{
					fySmall.Value = ((gradeSmall.SelectedIndex == 0) ? 275 : 420);
				}
			};
			concrete.SelectedIndexChanged += delegate
			{
				if (!applying)
				{
					fc.Value = (new int[3] { 21, 28, 35 })[concrete.SelectedIndex];
				}
			};
			base.Shown += delegate
			{
				dia.Focus();
				output.Select(0, 0);
			};
			UpdateResult();
		}

		private static FlowLayoutPanel Group(FlowLayoutPanel host, string name, int height)
		{
			GroupBox groupBox = new GroupBox
			{
				Text = name.Replace("&", "&&"),
				Width = 378,
				Height = height,
				Margin = new Padding(0, 0, 0, 10),
				Padding = new Padding(8, 23, 8, 5),
				ForeColor = Color.FromArgb(35, 57, 82),
				BackColor = Color.White,
				Font = new Font("Segoe UI", 9f, FontStyle.Bold)
			};
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				Font = new Font("Segoe UI", 9f),
				WrapContents = true
			};
			groupBox.Controls.Add(flowLayoutPanel);
			host.Controls.Add(groupBox);
			return flowLayoutPanel;
		}

		private static NumericUpDown Number(FlowLayoutPanel p, string label, decimal min, decimal max, decimal value, int digits)
		{
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Width = 172,
				Height = 55,
				FlowDirection = FlowDirection.TopDown,
				WrapContents = false,
				Margin = new Padding(2, 1, 3, 2)
			};
			flowLayoutPanel.Controls.Add(new Label
			{
				Text = label,
				UseMnemonic = false,
				Width = 172,
				Height = 21,
				Font = new Font("Segoe UI", 8.5f)
			});
			NumericUpDown numericUpDown = new NumericUpDown
			{
				Minimum = min,
				Maximum = max,
				Value = value,
				DecimalPlaces = digits,
				Width = 164,
				Height = 27,
				ThousandsSeparator = true
			};
			flowLayoutPanel.Controls.Add(numericUpDown);
			p.Controls.Add(flowLayoutPanel);
			return numericUpDown;
		}

		private static ComboBox Select(FlowLayoutPanel p, string label, string[] items)
		{
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Width = 172,
				Height = 55,
				FlowDirection = FlowDirection.TopDown,
				WrapContents = false,
				Margin = new Padding(2, 1, 3, 2)
			};
			flowLayoutPanel.Controls.Add(new Label
			{
				Text = label,
				UseMnemonic = false,
				Width = 172,
				Height = 21,
				Font = new Font("Segoe UI", 8.5f)
			});
			ComboBox comboBox = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				Width = 164
			};
			comboBox.Items.AddRange(items);
			comboBox.SelectedIndex = 0;
			flowLayoutPanel.Controls.Add(comboBox);
			p.Controls.Add(flowLayoutPanel);
			return comboBox;
		}

		private static CheckBox Check(FlowLayoutPanel p, string label)
		{
			CheckBox checkBox = new CheckBox
			{
				Text = label,
				Width = 345,
				Height = 25,
				Margin = new Padding(4, 2, 0, 0)
			};
			p.Controls.Add(checkBox);
			return checkBox;
		}

		private static DataGridView Table(string[] names, bool fill)
		{
			DataGridView dataGridView = new DataGridView
			{
				Dock = DockStyle.Fill,
				ReadOnly = true,
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = false,
				AllowUserToResizeRows = false,
				RowHeadersVisible = false,
				BackgroundColor = Color.White,
				BorderStyle = BorderStyle.None,
				AutoSizeColumnsMode = ((!fill) ? DataGridViewAutoSizeColumnsMode.None : DataGridViewAutoSizeColumnsMode.Fill),
				SelectionMode = DataGridViewSelectionMode.CellSelect,
				MultiSelect = false,
				EnableHeadersVisualStyles = false,
				ColumnHeadersHeight = 48,
				ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
			};
			dataGridView.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(232, 239, 247);
			dataGridView.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(23, 43, 67);
			dataGridView.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
			dataGridView.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
			dataGridView.DefaultCellStyle.Font = new Font("Segoe UI", 9f);
			dataGridView.DefaultCellStyle.SelectionBackColor = Color.FromArgb(218, 232, 247);
			dataGridView.DefaultCellStyle.SelectionForeColor = Color.FromArgb(23, 43, 67);
			dataGridView.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 249, 252);
			dataGridView.RowTemplate.Height = 34;
			foreach (string text in names)
			{
				dataGridView.Columns.Add(text, text);
			}
			if (fill)
			{
				dataGridView.Columns[0].FillWeight = 165f;
				dataGridView.Columns[dataGridView.Columns.Count - 1].FillWeight = 145f;
			}
			else
			{
				foreach (DataGridViewColumn column in dataGridView.Columns)
				{
					column.Width = 83;
					if (column.Index > 0)
					{
						column.DefaultCellStyle.Format = "N2";
					}
				}
			}
			GridAppearance.Apply(dataGridView);
			return dataGridView;
		}

		private LengthInput Input()
		{
			return new LengthInput
			{
				Simplified = (method.SelectedIndex == 0),
				Db = (double)dia.Value,
				Fy = (double)((dia.Value <= 12m) ? fySmall.Value : fy.Value),
				Fc = (double)fc.Value,
				Cover = (double)cover.Value,
				Spacing = (double)spacing.Value,
				Ktr = (double)ktr.Value,
				AsRatio = (double)asRatio.Value,
				Spliced = (double)spliced.Value,
				Round = (double)round.Value,
				Straight = (double)straight.Value,
				Top = top.Checked,
				Epoxy = epoxy.Checked,
				Lightweight = light.Checked
			};
		}

		public LengthConfiguration Configuration()
		{
			LengthConfiguration lengthConfiguration = new LengthConfiguration();
			lengthConfiguration.Name = configName.Text.Trim();
			lengthConfiguration.Input = Input();
			lengthConfiguration.FySmall = (double)fySmall.Value;
			lengthConfiguration.FyLarge = (double)fy.Value;
			lengthConfiguration.GradeSmall = ((gradeSmall.SelectedIndex == 0) ? 40 : 60);
			lengthConfiguration.GradeLarge = ((grade.SelectedIndex == 0) ? 60 : 40);
			lengthConfiguration.Validate();
			return lengthConfiguration;
		}

		public void ApplyConfiguration(LengthConfiguration c)
		{
			c.Validate();
			applying = true;
			try
			{
				method.SelectedIndex = ((!c.Input.Simplified) ? 1 : 0);
				configName.Text = c.Name;
				gradeSmall.SelectedIndex = ((c.GradeSmall != 40) ? 1 : 0);
				grade.SelectedIndex = ((c.GradeLarge != 60) ? 1 : 0);
				fySmall.Value = (decimal)c.FySmall;
				fy.Value = (decimal)c.FyLarge;
				if (c.Input.Fc == 21.0 || c.Input.Fc == 28.0 || c.Input.Fc == 35.0)
				{
					concrete.SelectedIndex = ((c.Input.Fc != 21.0) ? ((c.Input.Fc == 28.0) ? 1 : 2) : 0);
				}
				fc.Value = (decimal)c.Input.Fc;
				dia.Value = (decimal)c.Input.Db;
				top.Checked = c.Input.Top;
				straight.Value = (decimal)c.Input.Straight;
				cover.Value = (decimal)c.Input.Cover;
				spacing.Value = (decimal)c.Input.Spacing;
				ktr.Value = (decimal)c.Input.Ktr;
				asRatio.Value = (decimal)c.Input.AsRatio;
				spliced.Value = (decimal)c.Input.Spliced;
				round.Value = (decimal)c.Input.Round;
				epoxy.Checked = c.Input.Epoxy;
				light.Checked = c.Input.Lightweight;
			}
			finally
			{
				applying = false;
			}
			UpdateResult();
		}

		public string Sheet()
		{
			LengthConfiguration lengthConfiguration = Configuration();
			StringBuilder stringBuilder = new StringBuilder(lengthConfiguration.Review());
			stringBuilder.AppendLine("Full numeric checks: all 8 diameters. Each includes development, splice and hook calculations.");
			int[] array = new int[8] { 10, 12, 16, 20, 25, 28, 32, 36 };
			foreach (int diameter in array)
			{
				LengthInput lengthInput = lengthConfiguration.ForDiameter(diameter, top.Checked);
				lengthInput.Straight = (double)straight.Value;
				stringBuilder.Append("\f\n" + RebarLengths.Report(lengthInput));
			}
			return stringBuilder.ToString();
		}

		private void UpdateResult()
		{
			if (applying || output == null)
			{
				return;
			}
			try
			{
				LengthInput i = Input();
				LengthResult lengthResult = RebarLengths.Calculate(i);
				headline.Text = "Diameter " + i.Db + " mm  |  " + (i.Top ? "TOP BAR" : "OTHER BAR") + "  |  Tension lap: Class " + lengthResult.LapClass + "\nfy " + i.Fy + " MPa  /  f'c " + i.Fc + " MPa  /  round UP " + i.Round + " mm";
				results.Rows.Clear();
				Action<string, double, string> obj = delegate(string name, double v, string basis)
				{
					results.Rows.Add(name, v.ToString("0.00"), RebarLengths.Up(v, i.Round).ToString("0.00"), basis);
				};
				obj("Development - tension Ld", lengthResult.Ld, "min 300mm");
				obj("Development - hooked Ldh", lengthResult.Ldh, "min max(8db,150)");
				obj("Development - compression", lengthResult.Ldc, "min 200mm");
				obj("Splice - tension Class A", lengthResult.LapA, "As>=2 & <=50% spliced");
				obj("Splice - tension Class B", lengthResult.LapB, "1.3 x raw Ld");
				obj("Splice - compression", lengthResult.CompressionLap, "same-size bars");
				hooks.Rows.Clear();
				hooks.Rows.Add("90deg main", lengthResult.MainInside.ToString("0.00"), lengthResult.Tail90.ToString("0.00"), RebarLengths.Up(lengthResult.Cut90, i.Round).ToString("0.00"));
				hooks.Rows.Add("180deg main", lengthResult.MainInside.ToString("0.00"), lengthResult.Tail180.ToString("0.00"), RebarLengths.Up(lengthResult.Cut180, i.Round).ToString("0.00"));
				hooks.Rows.Add("135deg seismic tie", (lengthResult.TieInside == 0.0) ? "N/A" : lengthResult.TieInside.ToString("0.00"), lengthResult.Tail135.ToString("0.00"), (lengthResult.Cut135 == 0.0) ? "N/A" : RebarLengths.Up(lengthResult.Cut135, i.Round).ToString("0.00"));
				summary.Rows.Clear();
				LengthConfiguration lengthConfiguration = Configuration();
				int[] array = new int[8] { 10, 12, 16, 20, 25, 28, 32, 36 };
				foreach (int num2 in array)
				{
					LengthResult lengthResult2 = lengthConfiguration.Values(num2, false);
					LengthResult lengthResult3 = lengthConfiguration.Values(num2, true);
					summary.Rows.Add(num2, lengthConfiguration.Rounded(lengthResult2.Ld), lengthConfiguration.Rounded(lengthResult2.Ldh), lengthConfiguration.Rounded(lengthResult2.LapA), lengthConfiguration.Rounded(lengthResult2.LapB), lengthConfiguration.Rounded(lengthResult3.Ld), lengthConfiguration.Rounded(lengthResult3.LapA), lengthConfiguration.Rounded(lengthResult3.LapB), lengthConfiguration.Rounded(lengthResult2.Ldc), lengthConfiguration.Rounded(lengthResult2.CompressionLap));
				}
				output.Text = Sheet().Replace("## ", "");
				status.Text = "Beam Estimator automatically selects saved values by concrete strength and steel grades.\n" + (i.Simplified ? "Simplified method: bond-ratio inputs do not reduce Ld." : "General method: cover, spacing and Ktr determine the capped bond ratio.");
				results.Rows[(lengthResult.LapClass == "A") ? 3 : 4].DefaultCellStyle.BackColor = Color.FromArgb(221, 241, 232);
				results.ClearSelection();
				hooks.ClearSelection();
				summary.ClearSelection();
			}
			catch (Exception ex)
			{
				results.Rows.Clear();
				hooks.Rows.Clear();
				summary.Rows.Clear();
				headline.Text = "Check calculation inputs";
				output.Text = ex.Message;
				status.Text = ex.Message;
			}
		}
	}
}
