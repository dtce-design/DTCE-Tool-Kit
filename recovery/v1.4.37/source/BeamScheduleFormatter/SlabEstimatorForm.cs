using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace BeamScheduleFormatter
{
	internal class SlabEstimatorForm : Form
	{
		private readonly Dictionary<string, SlabScheduleRow> schedules = new Dictionary<string, SlabScheduleRow>(StringComparer.OrdinalIgnoreCase);

		private readonly List<string[]> sourceFiles = new List<string[]>();

		private List<double> stockLengths = new List<double>(CommercialBars.Lengths);

		private readonly List<SlabTakeoff> results = new List<SlabTakeoff>();

		private readonly List<string> errors = new List<string>();

		private readonly ToolTip help = new ToolTip
		{
			ShowAlways = true,
			InitialDelay = 350,
			ReshowDelay = 150,
			AutoPopDelay = 20000
		};

		private DataGridView grid;

		private DataGridView gradeTotals;

		private TextBox review;

		private TextBox selectedDetail;

		private List<StockAllocation> currentPlan = new List<StockAllocation>();

		private ComboBox fc;

		private ComboBox small;

		private ComboBox large;

		private ComboBox mode;

		private NumericUpDown cover;

		private Label status;

		private readonly Label loadedDxf = new Label
		{
			Dock = DockStyle.Top,
			Height = 56,
			Padding = new Padding(10, 8, 10, 4),
			AutoEllipsis = true,
			BackColor = Color.White,
			Text = "Loaded DXF: none"
		};

		private LengthConfiguration configuration;

		private ExcelLiveLink excel;

		private Timer timer = new Timer
		{
			Interval = 1000
		};

		private bool filling;

		private bool storedProfile;

		private string saved = "";

		private static readonly string[] Names = new string[18]
		{
			"Description", "Slab", "Short m", "Long m", "Qty", "Support", "Exposed edge m/panel", "Long CONT count", "Long END count", "Short CONT count",
			"Short END count", "Adjacent short m", "Adjacent long m", "Thickness mm", "Concrete m3", "Forms m2", "Buy kg", "Status"
		};

		public Control WorksSettings { get; private set; }

		internal decimal WorksCoverMm
		{
			get
			{
				return cover.Value;
			}
			set
			{
				cover.Value = value;
			}
		}

		internal bool WorksHasEntries
		{
			get
			{
				return grid.Rows.Cast<DataGridViewRow>().Any((DataGridViewRow r) => !r.IsNewRow && Cell(r, 0) != "");
			}
		}

		internal List<double> WorksStockLengths
		{
			get
			{
				return new List<double>(stockLengths);
			}
		}

		public event EventHandler WorksChanged;

		public SlabEstimatorForm()
		{
			if (!AccessCodes.CanUseApp(2))
			{
				throw new Exception("Estimator access required.");
			}
			AppBrand.Apply(this);
			Text = "Slab Estimator - local preview";
			base.Width = 1460;
			base.Height = 900;
			MinimumSize = new Size(1100, 700);
			Font = new Font("Segoe UI", 9f);
			base.StartPosition = FormStartPosition.CenterScreen;
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				Height = 110,
				AutoScroll = true,
				Padding = new Padding(8)
			};
			base.Controls.Add(flowLayoutPanel);
			Button(flowLayoutPanel, "Load / replace DXF", delegate
			{
				Guard();
				if (ReplaceOkay())
				{
					LoadDxf(true);
				}
			});
			Button(flowLayoutPanel, "Add DXF files", LoadDxf);
			Button(flowLayoutPanel, "Manage DXF files", ManageSources);
			Button(flowLayoutPanel, "Commercial lengths", ChooseStock);
			Button(flowLayoutPanel, "Special cases (optional)", EditAdvanced);
			Button(flowLayoutPanel, "Curtailment settings", ShowCurtailment);
			Button(flowLayoutPanel, "Splice / development / hooks", delegate
			{
				Guard();
				RebarLengthForm rebarLengthForm = new RebarLengthForm();
				rebarLengthForm.ApplyConfiguration(configuration);
				rebarLengthForm.Show(this);
			});
			Button(flowLayoutPanel, "Project Information", delegate
			{
				Guard();
				new ProjectInformationForm().Show(this);
			});
			Button(flowLayoutPanel, "Add entry", delegate
			{
				Guard();
				if (schedules.Count == 0)
				{
					throw new Exception("Load a slab schedule DXF first.");
				}
				int index = grid.Rows.Add();
				grid.Rows[index].Cells[4].Value = "1";
				grid.Rows[index].Cells[5].Value = "Elevated";
				grid.Rows[index].Cells[6].Value = "0";
			});
			Button(flowLayoutPanel, "Remove entry", delegate
			{
				Guard();
				foreach (DataGridViewRow item in (from r in (from DataGridViewCell c in grid.SelectedCells
						select c.OwningRow).Distinct()
					where !r.IsNewRow
					orderby r.Index descending
					select r).ToList())
				{
					grid.Rows.Remove(item);
				}
				Recalculate();
			});
			Button(flowLayoutPanel, "Print preview", delegate
			{
				CalculationPreview.Show(this, Report());
			});
			Button(flowLayoutPanel, "Save PDF", delegate
			{
				using (SaveFileDialog saveFileDialog = new SaveFileDialog
				{
					Filter = "PDF|*.pdf",
					FileName = "Slab-Estimate-Backup.pdf"
				})
				{
					if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
					{
						EstimatePdf.Write(saveFileDialog.FileName, Report());
					}
				}
			});
			Button(flowLayoutPanel, "Save estimate", Save);
			Button(flowLayoutPanel, "Open estimate", Open);
			Button(flowLayoutPanel, "Open / Link Excel", Link);
			Button(flowLayoutPanel, "Disconnect Excel", Disconnect);
			FlowLayoutPanel settings = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				Height = 84,
				Padding = new Padding(8),
				AutoScroll = true
			};
			fc = Select(settings, "Concrete MPa", new string[3] { "28", "21", "35" });
			small = Select(settings, "<=12mm steel", new string[2] { "G40 / fy276", "G60 / fy420" });
			large = Select(settings, ">=16mm steel", new string[2] { "G60 / fy420", "G40 / fy276" });
			mode = Select(settings, "Report", new string[3] { "Summary", "Detailed", "Extensive" });
			FlowLayoutPanel flowLayoutPanel2 = new FlowLayoutPanel
			{
				Width = 120,
				Height = 60,
				FlowDirection = FlowDirection.TopDown
			};
			flowLayoutPanel2.Controls.Add(new Label
			{
				Text = "Cover mm",
				Height = 20
			});
			cover = new NumericUpDown
			{
				Minimum = 0m,
				Maximum = 100m,
				Value = 20m,
				Width = 100
			};
			flowLayoutPanel2.Controls.Add(cover);
			settings.Controls.Add(flowLayoutPanel2);
			Button(settings, "Saved calculation config", delegate
			{
				LengthConfiguration lengthConfiguration = CalculationStore.Choose(this);
				if (lengthConfiguration != null)
				{
					configuration = lengthConfiguration;
					storedProfile = true;
					ComboBox comboBox = fc;
					ComboBox comboBox2 = small;
					bool flag2 = (large.Enabled = false);
					bool enabled = (comboBox2.Enabled = flag2);
					comboBox.Enabled = enabled;
					Recalculate();
				}
			});
			Button(settings, "Use material selections", delegate
			{
				storedProfile = false;
				ComboBox comboBox = fc;
				ComboBox comboBox2 = small;
				bool flag2 = (large.Enabled = true);
				bool enabled = (comboBox2.Enabled = flag2);
				comboBox.Enabled = enabled;
				Recalculate();
			});
			base.Controls.Add(settings);
			base.Controls.Add(loadedDxf);
			Label label = new Label
			{
				Dock = DockStyle.Top,
				Height = 68,
				Padding = new Padding(8),
				Text = "1. Load the slab schedule DXF.   2. Add an entry and select its slab mark.\n3. Enter SHORT SIDE, LONG SIDE and QUANTITY. The app calculates the quantities.\nDefault: elevated slab. Use Special cases only for gravel fill, cantilever edges or different neighboring spans."
			};
			base.Controls.Add(label);
			grid = new DataGridView
			{
				Dock = DockStyle.Fill,
				AllowUserToAddRows = true,
				AllowUserToDeleteRows = true,
				RowHeadersVisible = false,
				AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
				BackgroundColor = Color.White
			};
			string[] names = Names;
			foreach (string text in names)
			{
				if (text == "Slab")
				{
					DataGridViewComboBoxColumn dataGridViewColumn = new DataGridViewComboBoxColumn
					{
						Name = text,
						HeaderText = text,
						Width = 100
					};
					grid.Columns.Add(dataGridViewColumn);
				}
				else if (text == "Support")
				{
					DataGridViewComboBoxColumn dataGridViewComboBoxColumn = new DataGridViewComboBoxColumn
					{
						Name = text,
						HeaderText = text,
						Width = 130
					};
					dataGridViewComboBoxColumn.Items.AddRange("Elevated", "Elevated / cantilever edge", "On gravel fill");
					grid.Columns.Add(dataGridViewComboBoxColumn);
				}
				else
				{
					grid.Columns.Add(text, text);
				}
			}
			for (int num2 = 13; num2 < 18; num2++)
			{
				grid.Columns[num2].ReadOnly = true;
			}
			grid.Columns[0].Width = 190;
			grid.Columns[17].Width = 300;
			grid.ColumnHeadersHeight = 54;
			grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
			grid.DefaultCellStyle.Padding = new Padding(4);
			grid.EnableHeadersVisualStyles = false;
			grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(20, 33, 51);
			grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
			for (int num3 = 0; num3 < 13; num3++)
			{
				grid.Columns[num3].DefaultCellStyle.BackColor = Color.FromArgb(255, 249, 222);
			}
			grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
			grid.MultiSelect = true;
			grid.Columns[0].Frozen = (grid.Columns[1].Frozen = true);
			for (int num4 = 6; num4 <= 12; num4++)
			{
				grid.Columns[num4].Visible = false;
			}
			grid.Columns[5].Width = 190;
			grid.Columns[5].HeaderText = "SLAB SUPPORT";
			grid.Columns[5].ToolTipText = "Default Elevated: bottom/soffit forms included. On gravel fill: no bottom forms. Cantilever edges are configured under Special cases.";
			grid.Columns[0].HeaderText = "DESCRIPTION";
			grid.Columns[1].HeaderText = "SLAB MARK";
			grid.Columns[2].HeaderText = "SHORT SIDE (m)";
			grid.Columns[3].HeaderText = "LONG SIDE (m)";
			grid.Columns[4].HeaderText = "QUANTITY";
			grid.Columns[13].HeaderText = "THICKNESS (mm)";
			grid.Columns[14].HeaderText = "CONCRETE (m3)";
			grid.Columns[15].HeaderText = "FORMS (m2)";
			grid.Columns[16].HeaderText = "REBAR TO BUY (kg)";
			GridAppearance.Apply(grid);
			base.Controls.Add(grid);
			TabControl tabControl = new TabControl
			{
				Dock = DockStyle.Bottom,
				Height = 235
			};
			TabPage tabPage = new TabPage("Selected slab / source schedule");
			TabPage tabPage2 = new TabPage("Report preview");
			TabPage tabPage3 = new TabPage("Grade 40 / Grade 60");
			selectedDetail = new TextBox
			{
				Dock = DockStyle.Fill,
				Multiline = true,
				ReadOnly = true,
				ScrollBars = ScrollBars.Both,
				WordWrap = false,
				Font = new Font("Consolas", 9f)
			};
			review = new TextBox
			{
				Dock = DockStyle.Fill,
				Multiline = true,
				ReadOnly = true,
				ScrollBars = ScrollBars.Both,
				WordWrap = false,
				Font = new Font("Consolas", 9f)
			};
			gradeTotals = new DataGridView
			{
				Dock = DockStyle.Fill,
				ReadOnly = true,
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = false,
				RowHeadersVisible = false,
				AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
			};
			names = new string[6] { "Grade", "Dia", "Cut steel kg", "Added lap kg", "Offcut kg", "BUY kg" };
			foreach (string text2 in names)
			{
				gradeTotals.Columns.Add(text2, text2);
			}
			GridAppearance.Apply(gradeTotals);
			tabPage.Controls.Add(selectedDetail);
			tabPage2.Controls.Add(review);
			tabPage3.Controls.Add(gradeTotals);
			tabControl.TabPages.AddRange(new TabPage[3] { tabPage, tabPage2, tabPage3 });
			status = new Label
			{
				Dock = DockStyle.Bottom,
				Height = 30,
				Padding = new Padding(8)
			};
			base.Controls.Add(tabControl);
			base.Controls.Add(status);
			base.Controls.SetChildIndex(grid, 0);
			base.Controls.SetChildIndex(tabControl, 1);
			base.Controls.SetChildIndex(status, 2);
			base.Controls.SetChildIndex(label, 3);
			base.Controls.SetChildIndex(settings, 4);
			base.Controls.SetChildIndex(flowLayoutPanel, 5);
			grid.SelectionChanged += delegate
			{
				if (!filling)
				{
					ShowSelected();
				}
			};
			grid.DefaultValuesNeeded += delegate(object s, DataGridViewRowEventArgs e)
			{
				e.Row.Cells[4].Value = "1";
				e.Row.Cells[5].Value = "Elevated";
				e.Row.Cells[6].Value = "0";
			};
			grid.CurrentCellDirtyStateChanged += delegate
			{
				if (grid.IsCurrentCellDirty && grid.CurrentCell is DataGridViewComboBoxCell)
				{
					grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
				}
			};
			grid.CellValueChanged += delegate(object s, DataGridViewCellEventArgs e)
			{
				if (!filling && e.RowIndex >= 0)
				{
					if (e.ColumnIndex == 1)
					{
						SetSupportDefaults(grid.Rows[e.RowIndex]);
					}
					else if (e.ColumnIndex == 5)
					{
						if (Cell(grid.Rows[e.RowIndex], 5) != "Elevated / cantilever edge")
						{
							grid.Rows[e.RowIndex].Cells[6].Value = "0";
						}
						Recalculate();
					}
				}
			};
			object originalEditValue = null;
			grid.CellBeginEdit += delegate(object s, DataGridViewCellCancelEventArgs e)
			{
				originalEditValue = grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
			};
			grid.CellEndEdit += delegate(object s, DataGridViewCellEventArgs e)
			{
				if (!filling && !object.Equals(originalEditValue, grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value))
				{
					Recalculate();
				}
			};
			grid.RowsRemoved += delegate
			{
				if (!filling)
				{
					Recalculate();
				}
			};
			grid.DataError += delegate(object s, DataGridViewDataErrorEventArgs e)
			{
				e.ThrowException = false;
				status.Text = "Select a valid slab mark/support option.";
			};
			ComboBox[] array = new ComboBox[3] { fc, small, large };
			for (int num = 0; num < array.Length; num++)
			{
				array[num].SelectedIndexChanged += delegate
				{
					storedProfile = false;
					Recalculate();
				};
			}
			mode.SelectedIndexChanged += delegate
			{
				if (!filling)
				{
					Recalculate();
				}
			};
			cover.ValueChanged += delegate
			{
				Recalculate();
			};
			timer.Tick += delegate
			{
				timer.Stop();
				try
				{
					SyncExcel();
				}
				catch (Exception ex)
				{
					status.Text = "Excel paused: " + ex.Message;
				}
			};
			base.FormClosing += delegate(object s, FormClosingEventArgs e)
			{
				grid.EndEdit();
				Recalculate();
				timer.Stop();
				if (excel != null)
				{
					try
					{
						SyncExcel();
					}
					catch (Exception ex)
					{
						MessageBox.Show("Excel save failed: " + ex.Message);
						e.Cancel = true;
						return;
					}
				}
				if (State() != saved && sourceFiles.Count > 0)
				{
					switch (MessageBox.Show(this, "Save the slab estimate before closing?", "Slab estimate", MessageBoxButtons.YesNoCancel))
					{
					case DialogResult.Cancel:
						e.Cancel = true;
						break;
					case DialogResult.Yes:
						if (!SaveDialog())
						{
							e.Cancel = true;
						}
						break;
					}
				}
			};
			base.FormClosed += delegate
			{
				help.Dispose();
				timer.Dispose();
				if (excel != null)
				{
					excel.Dispose();
				}
			};
			WorksSettings = settings;
			settings.Visible = false;
			Button(flowLayoutPanel, "Settings", delegate
			{
				SettingsWorkspace.Show(this, "Slab", new string[1] { "Materials & calculation" }, new Control[1] { settings });
			});
			SettingsWorkspace.MoveButtons(flowLayoutPanel, settings, "Commercial lengths", "Curtailment settings", "Splice / development / hooks", "Special cases (optional)");
			ConfigureHelp(flowLayoutPanel);
			base.Shown += delegate
			{
				InputFocus.Apply(this);
			};
			Resolve();
			saved = State();
		}

		internal void WriteRCDefaults(XmlElement root)
		{
			XmlElement xmlElement = root.OwnerDocument.CreateElement("SlabDefaults");
			root.AppendChild(xmlElement);
			xmlElement.SetAttribute("stock", CommercialBars.FormatLengths(stockLengths));
			XmlElement xmlElement2 = root.OwnerDocument.CreateElement("Configuration");
			xmlElement.AppendChild(xmlElement2);
			configuration.Write(xmlElement2);
		}

		internal void ReadRCDefaults(XmlElement e)
		{
			if (e != null)
			{
				stockLengths = CommercialBars.ParseLengths(e.GetAttribute("stock"));
				configuration = LengthConfiguration.Read(e["Configuration"]);
				storedProfile = true;
				ComboBox comboBox = fc;
				ComboBox comboBox2 = small;
				bool flag = (large.Enabled = false);
				bool enabled = (comboBox2.Enabled = flag);
				comboBox.Enabled = enabled;
				Recalculate();
			}
		}

		internal void SetWorksStockLengths(List<double> lengths)
		{
			stockLengths = CommercialBars.ParseLengths(CommercialBars.FormatLengths(lengths));
			Recalculate();
		}

		private void ConfigureHelp(FlowLayoutPanel tools)
		{
			string[] tips = new string[18]
			{
				"Pangalan o location ng takeoff entry.", "Slab mark mula sa loaded DXF. Automatic ang thickness at bar schedule.", "Haba ng shorter side ng ISANG panel, in metres.", "Haba ng longer side ng ISANG panel, in metres.", "Bilang ng magkakaparehong slab panels.", "Default Elevated includes bottom forms. On gravel fill excludes bottom forms.", "Exposed cantilever edge length for one panel. Disabled for ordinary elevated/gravel.", "Automatic continuous support count from DXF.", "Automatic end support count from DXF.", "Automatic continuous support count from DXF.",
				"Automatic end support count from DXF.", "Neighbor span for unequal adjacent panels; blank uses current shorter side.", "Neighbor span for unequal adjacent panels; blank uses current longer side.", "Automatic slab thickness from DXF, millimetres.", "Gross concrete = short x long x thickness/1000 x quantity. No automatic openings deduction.", "Elevated: soffit area. Cantilever edge option adds side-edge forms. Gravel: no bottom forms.", "Actual commercial rebar purchase weight, including computed lap steel and stock offcuts; no waste factor.", "Shows missing inputs or calculation results."
			};
			for (int i = 0; i < tips.Length; i++)
			{
				grid.Columns[i].ToolTipText = tips[i];
			}
			grid.RowsAdded += delegate(object s, DataGridViewRowsAddedEventArgs e)
			{
				for (int j = e.RowIndex; j < e.RowIndex + e.RowCount && j < grid.Rows.Count; j++)
				{
					for (int k = 0; k < tips.Length; k++)
					{
						grid.Rows[j].Cells[k].ToolTipText = tips[k];
					}
				}
			};
			help.SetToolTip(mode, "Summary = purchase totals. Detailed = source schedule, counts and cuts. Extensive = formulas, configured lengths and stock comparisons.");
			help.SetToolTip(cover, "Concrete cover used for bar-count spacing limits, in millimetres.");
			foreach (Control control in tools.Controls)
			{
				if (control.Text == "Commercial lengths")
				{
					help.SetToolTip(control, "Check only stock lengths available to buy. The least-waste choice is selected from the checked lengths.");
				}
				if (control.Text == "Special cases (optional)")
				{
					help.SetToolTip(control, "Choose elevated, gravel fill or cantilever-edge forms. Fields switch on/off automatically; support overrides are optional.");
				}
			}
		}

		private void Guard()
		{
			if (!AccessCodes.CanUseApp(2))
			{
				throw new Exception("Renew estimator access to continue.");
			}
		}

		private static void Button(FlowLayoutPanel p, string label, Action action)
		{
			Button button = new Button
			{
				Text = label,
				AutoSize = true,
				Height = 32
			};
			button.Click += delegate
			{
				try
				{
					action();
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "Slab Estimator");
				}
			};
			p.Controls.Add(button);
		}

		private static ComboBox Select(FlowLayoutPanel p, string label, string[] values)
		{
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Width = 165,
				Height = 64,
				FlowDirection = FlowDirection.TopDown
			};
			flowLayoutPanel.Controls.Add(new Label
			{
				Text = label,
				Height = 20
			});
			ComboBox comboBox = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				Width = 155
			};
			comboBox.Items.AddRange(values);
			comboBox.SelectedIndex = 0;
			flowLayoutPanel.Controls.Add(comboBox);
			p.Controls.Add(flowLayoutPanel);
			return comboBox;
		}

		private static string Cell(DataGridViewRow r, int n)
		{
			return Convert.ToString(r.Cells[n].Value).Trim();
		}

		private static double Number(string value, string name)
		{
			double result;
			if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) || double.IsNaN(result) || double.IsInfinity(result))
			{
				throw new Exception("Invalid " + name);
			}
			return result;
		}

		private static int Whole(string value, string name)
		{
			int result;
			if (!int.TryParse(value, out result))
			{
				throw new Exception("Enter whole " + name);
			}
			return result;
		}

		private void Resolve()
		{
			if (!storedProfile)
			{
				configuration = CalculationStore.Resolve(double.Parse(fc.Text, CultureInfo.InvariantCulture), (small.SelectedIndex == 0) ? 40 : 60, (small.SelectedIndex == 0) ? 276 : 420, (large.SelectedIndex == 0) ? 60 : 40, (large.SelectedIndex == 0) ? 420 : 276);
			}
		}

		private void AddSource(string raw, string name)
		{
			raw = raw.Replace("\r\n", "\n").Replace("\r", "\n");
			List<SlabScheduleRow> list = SlabScheduleReader.Read(raw);
			if (list.Any((SlabScheduleRow r) => schedules.ContainsKey(r.Mark)))
			{
				throw new Exception("Duplicate slab marks across source files; use unique identifiers.");
			}
			foreach (SlabScheduleRow item in list)
			{
				schedules.Add(item.Mark, item);
			}
			sourceFiles.Add(new string[2] { name, raw });
			UpdateLoadedDxf();
			DataGridViewComboBoxColumn dataGridViewComboBoxColumn = (DataGridViewComboBoxColumn)grid.Columns[1];
			foreach (SlabScheduleRow item2 in list)
			{
				dataGridViewComboBoxColumn.Items.Add(item2.Mark);
			}
		}

		private void UpdateLoadedDxf()
		{
			loadedDxf.Text = "Loaded DXF (" + sourceFiles.Count + "): " + string.Join("; ", sourceFiles.Select((string[] file) => file[0])) + "\n" + schedules.Count + " slab marks available";
			help.SetToolTip(loadedDxf, string.Join("\n", sourceFiles.Select((string[] file) => file[0])));
		}

		private void ManageSources()
		{
			Guard();
			grid.EndEdit();
			List<HashSet<string>> source = sourceFiles.Select((string[] file) => new HashSet<string>(from row in SlabScheduleReader.Read(file[1])
				select row.Mark, StringComparer.OrdinalIgnoreCase)).ToList();
			int num = DxfSourceManager.ChooseRemoval(this, sourceFiles.Select((string[] file) => file[0]).ToArray(), source.Select((HashSet<string> set) => grid.Rows.Cast<DataGridViewRow>().Count((DataGridViewRow row) => !row.IsNewRow && set.Contains(Cell(row, 1)))).ToArray());
			if (num >= 0)
			{
				RemoveSourceAt(num);
			}
		}

		internal void RemoveSourceAt(int index)
		{
			if (index < 0 || index >= sourceFiles.Count)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			List<HashSet<string>> marks = sourceFiles.Select((string[] file) => new HashSet<string>(from row in SlabScheduleReader.Read(file[1])
				select row.Mark, StringComparer.OrdinalIgnoreCase)).ToList();
			filling = true;
			try
			{
				foreach (DataGridViewRow item in (from DataGridViewRow row in grid.Rows
					where !row.IsNewRow && marks[index].Contains(Cell(row, 1))
					select row).ToList())
				{
					grid.Rows.Remove(item);
				}
				foreach (string item2 in marks[index])
				{
					schedules.Remove(item2);
				}
				sourceFiles.RemoveAt(index);
				DataGridViewComboBoxColumn dataGridViewComboBoxColumn = (DataGridViewComboBoxColumn)grid.Columns[1];
				dataGridViewComboBoxColumn.Items.Clear();
				foreach (string key in schedules.Keys)
				{
					dataGridViewComboBoxColumn.Items.Add(key);
				}
			}
			finally
			{
				filling = false;
			}
			UpdateLoadedDxf();
			Recalculate();
		}

		private void LoadDxf()
		{
			LoadDxf(false);
		}

		private void LoadDxf(bool replace)
		{
			Guard();
			using (OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = "Slab schedule DXF|*.dxf",
				Multiselect = true
			})
			{
				if (openFileDialog.ShowDialog(this) != DialogResult.OK)
				{
					return;
				}
				List<string[]> list = openFileDialog.FileNames.Select((string f) => new string[2]
				{
					Path.GetFileName(f),
					File.ReadAllText(f)
				}).ToList();
				IEnumerable<string> collection;
				if (!replace)
				{
					IEnumerable<string> keys = schedules.Keys;
					collection = keys;
				}
				else
				{
					collection = Enumerable.Empty<string>();
				}
				HashSet<string> hashSet = new HashSet<string>(collection, StringComparer.OrdinalIgnoreCase);
				foreach (string[] item in list)
				{
					foreach (SlabScheduleRow item2 in SlabScheduleReader.Read(item[1]))
					{
						if (!hashSet.Add(item2.Mark))
						{
							throw new Exception("Duplicate slab mark " + item2.Mark);
						}
					}
				}
				if (replace)
				{
					filling = true;
					try
					{
						grid.Rows.Clear();
						schedules.Clear();
						sourceFiles.Clear();
						((DataGridViewComboBoxColumn)grid.Columns[1]).Items.Clear();
					}
					finally
					{
						filling = false;
					}
				}
				foreach (string[] item3 in list)
				{
					AddSource(item3[1], item3[0]);
				}
				Recalculate();
			}
		}

		private void SetSupportDefaults(DataGridViewRow row)
		{
			SlabScheduleRow value;
			if (!schedules.TryGetValue(Cell(row, 1), out value))
			{
				return;
			}
			filling = true;
			try
			{
				for (int i = 0; i < 2; i++)
				{
					bool flag = value.Bars[4 + 2 * i] != null;
					bool flag2 = value.Bars[5 + 2 * i] != null;
					row.Cells[7 + 2 * i].Value = (flag ? (flag2 ? "1" : "2") : "0");
					row.Cells[8 + 2 * i].Value = (flag2 ? (flag ? "1" : "2") : "0");
				}
			}
			finally
			{
				filling = false;
			}
			Recalculate();
		}

		public void Recalculate()
		{
			if (filling || grid == null)
			{
				return;
			}
			filling = true;
			try
			{
				Resolve();
				results.Clear();
				errors.Clear();
				foreach (DataGridViewRow row in (IEnumerable)grid.Rows)
				{
					if (row.IsNewRow)
					{
						continue;
					}
					row.Tag = null;
					for (int i = 13; i < 18; i++)
					{
						row.Cells[i].Value = "";
					}
					if (Enumerable.Range(0, 4).All((int n) => Cell(row, n) == ""))
					{
						continue;
					}
					try
					{
						SlabScheduleRow value;
						if (!schedules.TryGetValue(Cell(row, 1), out value))
						{
							throw new Exception("Choose a slab mark");
						}
						if (Cell(row, 0) == "")
						{
							throw new Exception("Enter description");
						}
						SlabTakeoff slabTakeoff = SlabEstimatorEngine.Calculate(value, Cell(row, 0), Number(Cell(row, 2), "short side"), Number(Cell(row, 3), "long side"), Whole(Cell(row, 4), "quantity"), Cell(row, 5) != "On gravel fill", (Cell(row, 6) == "") ? 0.0 : Number(Cell(row, 6), "exposed edge"), (double)cover.Value, configuration, Whole(Cell(row, 7), "long CONT count"), Whole(Cell(row, 8), "long END count"), Whole(Cell(row, 9), "short CONT count"), Whole(Cell(row, 10), "short END count"), (Cell(row, 11) == "") ? 0.0 : Number(Cell(row, 11), "adjacent short span"), (Cell(row, 12) == "") ? 0.0 : Number(Cell(row, 12), "adjacent long span"), Cell(row, 5) == "Elevated / cantilever edge");
						results.Add(slabTakeoff);
						row.Tag = slabTakeoff;
						row.Cells[13].Value = value.Thickness.ToString("0.00");
						row.Cells[14].Value = slabTakeoff.Measures.ConcreteTotal.ToString("0.00");
						row.Cells[15].Value = slabTakeoff.Measures.FormworkTotal.ToString("0.00");
						row.Cells[17].Value = "Estimated";
					}
					catch (Exception ex)
					{
						errors.Add(Cell(row, 0) + " / " + Cell(row, 1) + ": " + ex.Message);
						row.Cells[17].Value = ex.Message;
					}
				}
				List<StockAllocation> list = (currentPlan = CommercialBars.Allocate(results.Select((SlabTakeoff r) => r.Steel), configuration, stockLengths));
				UpdateGradeTotals();
				foreach (DataGridViewRow item in (IEnumerable)grid.Rows)
				{
					SlabTakeoff result = item.Tag as SlabTakeoff;
					if (result != null)
					{
						List<StockAllocation> source = list.Where((StockAllocation a) => a.Entry == result.Steel).ToList();
						item.Cells[16].Value = (source.Any((StockAllocation a) => a.Choice == null) ? "REVIEW" : source.Sum((StockAllocation a) => a.Purchase).ToString("0.00"));
					}
				}
				status.Text = ((errors.Count > 0 || list.Any((StockAllocation a) => a.Choice == null)) ? "PARTIAL | " : "") + "Concrete " + results.Sum((SlabTakeoff r) => r.Measures.ConcreteTotal).ToString("0.00") + " m3 | Forms " + results.Sum((SlabTakeoff r) => r.Measures.FormworkTotal).ToString("0.00") + " m2 | Rebar to buy " + list.Sum((StockAllocation a) => a.Purchase).ToString("0.00") + " kg";
				review.Text = BuildReport(list, false);
				if (excel != null)
				{
					timer.Stop();
					timer.Start();
				}
			}
			finally
			{
				filling = false;
				EventHandler eventHandler = this.WorksChanged;
				if (eventHandler != null)
				{
					eventHandler(this, EventArgs.Empty);
				}
			}
			ShowSelected();
		}

		public string Report()
		{
			Guard();
			grid.EndEdit();
			Recalculate();
			if (results.Count == 0)
			{
				throw new Exception("Add at least one valid slab estimate entry.");
			}
			return BuildReport(CommercialBars.Allocate(results.Select((SlabTakeoff r) => r.Steel), configuration, stockLengths), true);
		}

		private string BuildReport(List<StockAllocation> plan, bool extensive)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("SLAB ESTIMATE | PURCHASE BACKUP");
			stringBuilder.AppendLine("Report: " + mode.Text + " | " + configuration.Name);
			stringBuilder.AppendLine("Actual materials: fc=" + configuration.Input.Fc + "MPa; fy <=12=" + configuration.FySmall + "MPa; fy >=16=" + configuration.FyLarge + "MPa.");
			stringBuilder.AppendLine("Commercial lengths: " + CommercialBars.FormatLengths(stockLengths) + " m");
			stringBuilder.AppendLine("Cuts: body + 2 Ld, rounded up 25mm. No waste factor.");
			stringBuilder.AppendLine("Blank adjacent span = current span. Count shared supports once.");
			if (mode.SelectedIndex == 2)
			{
				stringBuilder.AppendLine("Unit mass kg/m: " + string.Join(", ", from d in plan.Select((StockAllocation a) => a.Demand.Diameter).Distinct()
					orderby d
					select "D" + d + "=" + EstimatorEngine.UnitMass(d).ToString("0.000", CultureInfo.InvariantCulture)));
			}
			if (errors.Count > 0 || plan.Any((StockAllocation a) => a.Choice == null))
			{
				stringBuilder.AppendLine("PARTIAL / REVIEW: some entries or stock demands are excluded.");
			}
			foreach (string error in errors)
			{
				stringBuilder.AppendLine("NOT ESTIMATED: " + error);
			}
			stringBuilder.AppendLine();
			PurchaseReports.Summary(stringBuilder, plan);
			stringBuilder.AppendLine("GROSS CONCRETE: " + results.Sum((SlabTakeoff r) => r.Measures.ConcreteTotal).ToString("0.00") + " m3 | FORMWORK: " + results.Sum((SlabTakeoff r) => r.Measures.FormworkTotal).ToString("0.00") + " m2");
			stringBuilder.AppendLine();
			foreach (SlabTakeoff result in results)
			{
				EstimateInput entry = result.Steel;
				List<StockAllocation> list = plan.Where((StockAllocation a) => a.Entry == entry).ToList();
				stringBuilder.AppendLine();
				stringBuilder.AppendLine(entry.Description + " | " + entry.Beam + " | " + entry.Length + " m | t=" + result.Thickness.ToString("0.00") + "mm | Qty " + entry.Quantity);
				if (mode.SelectedIndex == 0)
				{
					stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Concrete {0:0.00} m3 | Forms {1:0.00} m2 | Rebar BUY {2:0.00} kg", result.Measures.ConcreteTotal, result.Measures.FormworkTotal, list.Sum((StockAllocation a) => a.Purchase)));
				}
				if (mode.SelectedIndex > 0)
				{
					if (mode.SelectedIndex == 2)
					{
						AppendSourceSchedule(stringBuilder, result);
						stringBuilder.AppendLine(result.GeometryBasis);
					}
					stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "Concrete: {0:0.00} m2 x ({1:0.00}mm / 1000) x {2} panels = {3:0.00} m3", result.Measures.AreaPerPanel, result.Thickness, entry.Quantity, result.Measures.ConcreteTotal));
					stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "Forms: soffit {0:0.00}{1} = {2:0.00} m2", result.Measures.SoffitTotal, (result.Measures.EdgeFormsTotal > 0.0) ? (" + edges " + result.Measures.EdgeFormsTotal.ToString("0.00", CultureInfo.InvariantCulture)) : "", result.Measures.FormworkTotal));
					PurchaseReports.Entry(stringBuilder, list, int.Parse(entry.Quantity), mode.SelectedIndex == 2);
				}
			}
			PurchaseReports.Basis(stringBuilder);
			if (mode.SelectedIndex == 2)
			{
				stringBuilder.AppendLine();
				stringBuilder.AppendLine("DETAILING NOTES");
				foreach (string item in results.SelectMany((SlabTakeoff r) => r.Notes).Distinct())
				{
					stringBuilder.AppendLine(item);
				}
			}
			if (extensive && mode.SelectedIndex == 2)
			{
				stringBuilder.AppendLine();
				stringBuilder.AppendLine(configuration.Review());
				stringBuilder.AppendLine(CommercialBars.Report(results.Select((SlabTakeoff r) => r.Steel), configuration, stockLengths));
			}
			return stringBuilder.ToString();
		}

		private static void AppendSourceSchedule(StringBuilder b, SlabTakeoff r)
		{
			b.AppendLine("ORIGINAL DXF REINFORCEMENT SCHEDULE");
			for (int i = 0; i < SlabScheduleReader.Columns.Length; i++)
			{
				b.AppendLine(string.Format("  {0,-36}{1}", SlabScheduleReader.Columns[i], (r.Schedule.Bars[i] == null) ? "--" : r.Schedule.Bars[i].Raw));
			}
		}

		private void UpdateGradeTotals()
		{
			gradeTotals.Rows.Clear();
			int[] array = new int[2] { 40, 60 };
			for (int i = 0; i < array.Length; i++)
			{
				int grade = array[i];
				List<StockAllocation> source = currentPlan.Where((StockAllocation a) => a.Demand.Grade == grade && a.Choice != null).ToList();
				foreach (IGrouping<int, StockAllocation> item in from a in source
					group a by a.Demand.Diameter into g
					orderby g.Key
					select g)
				{
					gradeTotals.Rows.Add("G" + grade, "D" + item.Key, item.Sum((StockAllocation a) => a.Net).ToString("0.00"), item.Sum((StockAllocation a) => a.Splice).ToString("0.00"), item.Sum((StockAllocation a) => a.Waste).ToString("0.00"), item.Sum((StockAllocation a) => a.Purchase).ToString("0.00"));
				}
				gradeTotals.Rows.Add("G" + grade + " TOTAL", "", source.Sum((StockAllocation a) => a.Net).ToString("0.00"), source.Sum((StockAllocation a) => a.Splice).ToString("0.00"), source.Sum((StockAllocation a) => a.Waste).ToString("0.00"), source.Sum((StockAllocation a) => a.Purchase).ToString("0.00"));
			}
		}

		private void ShowSelected()
		{
			if (selectedDetail == null)
			{
				return;
			}
			DataGridViewRow currentRow = grid.CurrentRow;
			SlabTakeoff result = ((currentRow == null) ? null : (currentRow.Tag as SlabTakeoff));
			if (result == null)
			{
				selectedDetail.Text = ((currentRow == null) ? "Select a slab entry." : Cell(currentRow, 17));
				return;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(result.Steel.Description + " | " + result.Steel.Beam + " | " + result.Steel.Length + "m | Qty " + result.Steel.Quantity);
			AppendSourceSchedule(stringBuilder, result);
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(result.GeometryBasis);
			PurchaseReports.Entry(stringBuilder, currentPlan.Where((StockAllocation a) => a.Entry == result.Steel).ToList(), int.Parse(result.Steel.Quantity), true);
			selectedDetail.Text = stringBuilder.ToString();
		}

		private void ChooseStock()
		{
			Guard();
			List<double> list = StockLengthPicker.Choose(this, stockLengths);
			if (list != null)
			{
				stockLengths = list;
				Recalculate();
			}
		}

		private void EditAdvanced()
		{
			Guard();
			DataGridViewRow row = grid.CurrentRow;
			if (row == null || row.IsNewRow)
			{
				throw new Exception("Select a slab entry first.");
			}
			SlabScheduleRow value;
			if (!schedules.TryGetValue(Cell(row, 1), out value))
			{
				throw new Exception("Choose a slab mark first.");
			}
			using (SlabEntryOptionsForm slabEntryOptionsForm = new SlabEntryOptionsForm(Cell(row, 5), (from n in Enumerable.Range(6, 7)
				select Cell(row, n)).ToArray(), value))
			{
				if (slabEntryOptionsForm.ShowDialog(this) != DialogResult.OK)
				{
					return;
				}
				filling = true;
				try
				{
					row.Cells[5].Value = slabEntryOptionsForm.SelectedType;
					for (int num = 0; num < 7; num++)
					{
						row.Cells[6 + num].Value = slabEntryOptionsForm.Values[num];
					}
				}
				finally
				{
					filling = false;
				}
				Recalculate();
			}
		}

		private void ShowCurtailment()
		{
			Guard();
			MessageBox.Show(this, "CURTAILED SLAB DETAILING\n\nBottom end / continuous support offset: entered span / 6.\nTop end / continuous support extension: entered span / 4.\nLocations and final cuts round UP to 25 mm.\n\nBottom curtailed body = span minus both rounded L/6 offsets.\nContinuous top body = this-panel L/4 + adjacent-panel L/4.\nStraight end allowance = 2 x computed Ld per cut piece.\nAdditional stock splices use computed lap from saved configuration.\n\nShort/long input spans are used directly; no support-width deduction.", "Slab curtailment reference");
		}

		private string State()
		{
			return Snapshot();
		}

		internal string[] WorksValidationIssues()
		{
			return errors.Select((string e) => "Slab: " + e).Concat(from a in WorksPlan()
				where a.Choice == null
				select "Slab " + a.Entry.Beam + " / " + a.Line.Part + ": unresolved stock cut; review checked commercial lengths and splice settings.").ToArray();
		}

		internal List<StockAllocation> WorksPlan()
		{
			return CommercialBars.Allocate(results.Select((SlabTakeoff r) => r.Steel), configuration, stockLengths);
		}

		internal object[,] WorksExcelRows(List<StockAllocation> shared = null)
		{
			if (errors.Count > 0)
			{
				throw new Exception("Resolve invalid slab entries before syncing Program of Works.");
			}
			List<StockAllocation> source = ((shared == null) ? WorksPlan() : shared.Where((StockAllocation a) => results.Any((SlabTakeoff r) => r.Steel == a.Entry)).ToList());
			if (source.Any((StockAllocation a) => a.Choice == null))
			{
				throw new Exception("Resolve slab stock cuts before syncing.");
			}
			var list = (from a in source
				group a by new
				{
					a.Entry,
					a.Demand.Grade,
					a.Demand.Diameter
				}).ToList();
			object[,] array = new object[list.Count, 12];
			for (int num = 0; num < list.Count; num++)
			{
				var grouping = list[num];
				array[num, 0] = grouping.Key.Entry.Description;
				array[num, 1] = grouping.Key.Entry.Beam;
				array[num, 2] = "Slab schedules";
				array[num, 3] = grouping.Key.Grade;
				array[num, 4] = grouping.Key.Diameter;
				array[num, 5] = grouping.Sum((StockAllocation a) => a.Net);
				array[num, 6] = 0;
				array[num, 7] = 0;
				array[num, 8] = grouping.Sum((StockAllocation a) => a.Splice + a.Waste);
				array[num, 9] = grouping.Sum((StockAllocation a) => a.Purchase);
				array[num, 10] = 0;
				array[num, 11] = int.Parse(grouping.Key.Entry.Quantity);
			}
			return array;
		}

		internal object[,] WorksMeasureRows()
		{
			object[,] array = new object[results.Count, 8];
			for (int i = 0; i < results.Count; i++)
			{
				SlabTakeoff slabTakeoff = results[i];
				array[i, 0] = slabTakeoff.Steel.Description;
				array[i, 1] = slabTakeoff.Steel.Beam;
				array[i, 2] = ((slabTakeoff.Measures.SoffitTotal > 0.0) ? "Elevated slab" : "On gravel fill");
				array[i, 3] = int.Parse(slabTakeoff.Steel.Quantity);
				array[i, 4] = slabTakeoff.Measures.ConcreteTotal;
				array[i, 5] = slabTakeoff.Measures.EdgeFormsTotal;
				array[i, 6] = slabTakeoff.Measures.SoffitTotal;
				array[i, 7] = slabTakeoff.Measures.FormworkTotal;
			}
			return array;
		}

		internal string WorksSnapshot()
		{
			grid.EndEdit();
			Recalculate();
			if (sourceFiles.Count != 0)
			{
				return Snapshot();
			}
			return null;
		}

		internal void WorksRestore(string xml)
		{
			Restore(xml);
			saved = State();
		}

		internal void WorksSaved()
		{
			saved = State();
		}

		private string Snapshot()
		{
			Resolve();
			XmlDocument xmlDocument = new XmlDocument();
			XmlElement xmlElement = xmlDocument.CreateElement("SlabEstimate");
			xmlElement.SetAttribute("version", "1");
			xmlElement.SetAttribute("stockLengths", CommercialBars.FormatLengths(stockLengths));
			xmlElement.SetAttribute("cover", cover.Value.ToString(CultureInfo.InvariantCulture));
			xmlElement.SetAttribute("report", mode.SelectedIndex.ToString());
			xmlElement.SetAttribute("fc", fc.Text);
			xmlElement.SetAttribute("small", small.SelectedIndex.ToString());
			xmlElement.SetAttribute("large", large.SelectedIndex.ToString());
			xmlDocument.AppendChild(xmlElement);
			XmlElement xmlElement2 = xmlDocument.CreateElement("Configuration");
			configuration.Write(xmlElement2);
			xmlElement.AppendChild(xmlElement2);
			foreach (string[] sourceFile in sourceFiles)
			{
				XmlElement xmlElement3 = xmlDocument.CreateElement("Source");
				xmlElement3.SetAttribute("name", sourceFile[0]);
				xmlElement3.InnerText = sourceFile[1];
				xmlElement.AppendChild(xmlElement3);
			}
			foreach (DataGridViewRow item in (IEnumerable)grid.Rows)
			{
				if (!item.IsNewRow)
				{
					XmlElement xmlElement4 = xmlDocument.CreateElement("Entry");
					for (int i = 0; i < 13; i++)
					{
						xmlElement4.SetAttribute("c" + i, Cell(item, i));
					}
					xmlElement.AppendChild(xmlElement4);
				}
			}
			return xmlDocument.OuterXml;
		}

		private void Restore(string xml)
		{
			XmlDocument xmlDocument = new XmlDocument
			{
				XmlResolver = null
			};
			using (StringReader input = new StringReader(xml))
			{
				using (XmlReader reader = XmlReader.Create(input, new XmlReaderSettings
				{
					XmlResolver = null,
					DtdProcessing = DtdProcessing.Prohibit,
					MaxCharactersInDocument = 100000000L
				}))
				{
					xmlDocument.Load(reader);
				}
			}
			XmlElement documentElement = xmlDocument.DocumentElement;
			if (documentElement == null || documentElement.Name != "SlabEstimate" || documentElement.GetAttribute("version") != "1")
			{
				throw new Exception("This is not a slab estimate. Use a separate slab workbook.");
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (XmlElement item in documentElement.SelectNodes("Source"))
			{
				foreach (SlabScheduleRow item2 in SlabScheduleReader.Read(item.InnerText))
				{
					if (!hashSet.Add(item2.Mark))
					{
						throw new Exception("Duplicate marks in saved slab estimate.");
					}
				}
			}
			List<double> list = CommercialBars.ParseLengths(documentElement.GetAttribute("stockLengths"));
			LengthConfiguration lengthConfiguration = LengthConfiguration.Read(documentElement["Configuration"]);
			int num = int.Parse(documentElement.GetAttribute("report"));
			int num2 = int.Parse(documentElement.GetAttribute("small"));
			int num3 = int.Parse(documentElement.GetAttribute("large"));
			decimal num4 = decimal.Parse(documentElement.GetAttribute("cover"), CultureInfo.InvariantCulture);
			if (num < 0 || num > 2 || num2 < 0 || num2 > 1 || num3 < 0 || num3 > 1 || num4 < 0m || num4 > 100m)
			{
				throw new Exception("Invalid slab estimate settings.");
			}
			filling = true;
			try
			{
				stockLengths = list;
				grid.Rows.Clear();
				schedules.Clear();
				sourceFiles.Clear();
				((DataGridViewComboBoxColumn)grid.Columns[1]).Items.Clear();
				foreach (XmlElement item3 in documentElement.SelectNodes("Source"))
				{
					AddSource(item3.InnerText, item3.GetAttribute("name"));
				}
				fc.SelectedItem = documentElement.GetAttribute("fc");
				small.SelectedIndex = num2;
				large.SelectedIndex = num3;
				cover.Value = num4;
				mode.SelectedIndex = num;
				configuration = lengthConfiguration;
				storedProfile = true;
				ComboBox comboBox = fc;
				ComboBox comboBox2 = small;
				bool flag = (large.Enabled = false);
				bool enabled = (comboBox2.Enabled = flag);
				comboBox.Enabled = enabled;
				foreach (XmlElement item4 in documentElement.SelectNodes("Entry"))
				{
					int index = grid.Rows.Add();
					for (int i = 0; i < 13; i++)
					{
						grid.Rows[index].Cells[i].Value = item4.GetAttribute("c" + i);
					}
				}
			}
			finally
			{
				filling = false;
			}
			Recalculate();
			saved = State();
		}

		private bool SaveDialog()
		{
			Guard();
			grid.EndEdit();
			Recalculate();
			if (sourceFiles.Count == 0)
			{
				throw new Exception("Load a slab schedule first.");
			}
			using (SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				Filter = "Slab estimate XML|*.slab-estimate.xml",
				FileName = "Slab-Estimate.slab-estimate.xml"
			})
			{
				if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
				{
					return false;
				}
				string text = saveFileDialog.FileName + "." + Guid.NewGuid().ToString("N") + ".tmp";
				try
				{
					File.WriteAllText(text, Snapshot(), new UTF8Encoding(false));
					File.Copy(text, saveFileDialog.FileName, true);
				}
				finally
				{
					if (File.Exists(text))
					{
						File.Delete(text);
					}
				}
				saved = State();
				return true;
			}
		}

		private void Save()
		{
			SaveDialog();
		}

		private void Open()
		{
			Guard();
			if (!ReplaceOkay())
			{
				return;
			}
			using (OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = "Slab estimate XML|*.slab-estimate.xml"
			})
			{
				if (openFileDialog.ShowDialog(this) == DialogResult.OK)
				{
					Restore(File.ReadAllText(openFileDialog.FileName));
				}
			}
		}

		private bool ReplaceOkay()
		{
			if (sourceFiles.Count != 0 && !(State() == saved))
			{
				return MessageBox.Show(this, "Replace current slab estimate? Save current work first if needed.", "Open slab estimate", MessageBoxButtons.OKCancel) == DialogResult.OK;
			}
			return true;
		}

		private void Link()
		{
			Guard();
			using (SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				Filter = "Excel macro-enabled workbook|*.xlsm|Excel workbook|*.xlsx",
				FileName = "Slab-Summary.xlsm",
				OverwritePrompt = false
			})
			{
				if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
				{
					return;
				}
				ExcelLiveLink excelLiveLink = new ExcelLiveLink(saveFileDialog.FileName);
				try
				{
					string text = excelLiveLink.ReadEstimate();
					bool flag = text != null;
					if (flag && sourceFiles.Count > 0)
					{
						DialogResult dialogResult = MessageBox.Show(this, "Load the slab estimate saved in this workbook?\\nYes: load saved estimate. No: replace workbook data with current estimate.", "Slab workbook", MessageBoxButtons.YesNoCancel);
						if (dialogResult == DialogResult.Cancel)
						{
							excelLiveLink.Dispose();
							return;
						}
						flag = dialogResult == DialogResult.Yes;
					}
					if (text != null)
					{
						XmlDocument xmlDocument = new XmlDocument
						{
							XmlResolver = null
						};
						using (StringReader input = new StringReader(text))
						{
							using (XmlReader reader = XmlReader.Create(input, new XmlReaderSettings
							{
								DtdProcessing = DtdProcessing.Prohibit,
								XmlResolver = null
							}))
							{
								xmlDocument.Load(reader);
							}
						}
						if (xmlDocument.DocumentElement.Name != "SlabEstimate")
						{
							throw new Exception("Use a separate Excel workbook for slabs; beam estimates were not overwritten.");
						}
					}
					ExcelLiveLink excelLiveLink2 = excel;
					excel = null;
					try
					{
						if (flag)
						{
							Restore(text);
						}
						else if (sourceFiles.Count == 0)
						{
							throw new Exception("Load a slab schedule or choose a workbook with a saved slab estimate.");
						}
					}
					catch
					{
						excel = excelLiveLink2;
						throw;
					}
					if (excelLiveLink2 != null)
					{
						excelLiveLink2.Dispose();
					}
					excel = excelLiveLink;
					Recalculate();
					SyncExcel();
				}
				catch
				{
					if (excel != excelLiveLink)
					{
						excelLiveLink.Dispose();
					}
					throw;
				}
			}
		}

		private void Disconnect()
		{
			timer.Stop();
			if (excel == null)
			{
				return;
			}
			try
			{
				SyncExcel();
			}
			catch (Exception ex)
			{
				if (MessageBox.Show(this, ex.Message + "\\nDisconnect without saving latest edits?", "Excel", MessageBoxButtons.YesNo) != DialogResult.Yes)
				{
					return;
				}
			}
			excel.Dispose();
			excel = null;
			status.Text = "Excel disconnected; saved workbook retained.";
		}

		private void SyncExcel()
		{
			if (excel == null)
			{
				return;
			}
			Guard();
			List<StockAllocation> source = CommercialBars.Allocate(results.Select((SlabTakeoff r) => r.Steel), configuration, stockLengths);
			if (source.Any((StockAllocation a) => a.Choice == null))
			{
				throw new Exception("Resolve stock cuts before Excel sync.");
			}
			var list = (from a in source
				group a by new
				{
					a.Entry,
					a.Demand.Grade,
					a.Demand.Diameter
				}).ToList();
			object[,] array = new object[list.Count, 12];
			for (int num = 0; num < list.Count; num++)
			{
				var grouping = list[num];
				array[num, 0] = grouping.Key.Entry.Description;
				array[num, 1] = grouping.Key.Entry.Beam;
				array[num, 2] = "Slab schedules";
				array[num, 3] = grouping.Key.Grade;
				array[num, 4] = grouping.Key.Diameter;
				array[num, 5] = grouping.Sum((StockAllocation a) => a.Net);
				array[num, 6] = 0;
				array[num, 7] = 0;
				array[num, 8] = grouping.Sum((StockAllocation a) => a.Splice + a.Waste);
				array[num, 9] = grouping.Sum((StockAllocation a) => a.Purchase);
				array[num, 10] = 0;
				array[num, 11] = int.Parse(grouping.Key.Entry.Quantity);
			}
			object[,] array2 = new object[results.Count, 8];
			for (int num2 = 0; num2 < results.Count; num2++)
			{
				SlabTakeoff slabTakeoff = results[num2];
				array2[num2, 0] = slabTakeoff.Steel.Description;
				array2[num2, 1] = slabTakeoff.Steel.Beam;
				array2[num2, 2] = ((slabTakeoff.Measures.SoffitTotal > 0.0) ? "Elevated slab" : "On gravel fill");
				array2[num2, 3] = int.Parse(slabTakeoff.Steel.Quantity);
				array2[num2, 4] = slabTakeoff.Measures.ConcreteTotal;
				array2[num2, 5] = slabTakeoff.Measures.EdgeFormsTotal;
				array2[num2, 6] = slabTakeoff.Measures.SoffitTotal;
				array2[num2, 7] = slabTakeoff.Measures.FormworkTotal;
			}
			excel.Update(array, ((errors.Count > 0) ? "PARTIAL | " : "") + "Slab purchase kg; Main=all slab bars; allowance=lap+offcut; forms sheet includes slab edges/soffit", Snapshot(), array2);
			saved = State();
			status.Text = "Slab Excel synced " + DateTime.Now.ToString("HH:mm:ss");
		}
	}
}
