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
	internal class EstimatorForm : Form
	{
		private ExcelLiveLink excelLink;

		private Timer excelTimer = new Timer
		{
			Interval = 1000
		};

		private Label excelStatus;

		private List<ScheduleSource> sources = new List<ScheduleSource>();

		private LengthConfiguration lengthProfile;

		private Analysis catalog;

		private string sourceText = "";

		private string sourceName = "";

		private bool filling;

		private bool profileSnapshot;

		private string savedState = "";

		private double? projectSwell;

		private double? projectShrink;

		private List<double> stockLengths = new List<double>(CommercialBars.Lengths);

		private List<EstimateInput> inputs = new List<EstimateInput>();

		private Dictionary<string, BeamRow> schedules = new Dictionary<string, BeamRow>(StringComparer.OrdinalIgnoreCase);

		private readonly ToolTip help = new ToolTip
		{
			ShowAlways = true,
			InitialDelay = 350,
			AutoPopDelay = 20000
		};

		private DataGridView beams;

		private DataGridView totals;

		private DataGridView gradeTotals;

		private TextBox details;

		private Label status;

		private Label sourceLabel;

		private Label calculationLabel;

		private ComboBox reportMode;

		private ComboBox gradeSmall;

		private ComboBox grade;

		private ComboBox concrete;

		private ComboBox mainHook;

		private ComboBox anchorage;

		private NumericUpDown cover;

		private NumericUpDown hook;

		private NumericUpDown extra;

		private NumericUpDown waste;

		private NumericUpDown bond;

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
				return beams.Rows.Cast<DataGridViewRow>().Any((DataGridViewRow r) => !r.IsNewRow && Cell(r, 0) != "");
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

		public EstimatorForm(Result initial)
		{
			AppBrand.Apply(this);
			if (!AccessCodes.CanUseApp(2))
			{
				throw new Exception("This app is not included in your current access.");
			}
			base.Shown += delegate
			{
				InputFocus.Apply(this);
			};
			Text = "Beam Estimator v1.4.37";
			base.Width = 1380;
			base.Height = 880;
			MinimumSize = new Size(1100, 740);
			base.StartPosition = FormStartPosition.CenterParent;
			Font = new Font("Segoe UI", 9f);
			BackColor = Color.FromArgb(243, 246, 250);
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				Height = 110,
				Padding = new Padding(8),
				WrapContents = true,
				AutoScroll = true
			};
			Button(flowLayoutPanel, "Load REVISED DXF", Choose);
			Button(flowLayoutPanel, "Add DXF files", ChooseAdditional);
			Button(flowLayoutPanel, "Manage DXF files", ManageSources);
			Button(flowLayoutPanel, "Curtailment Settings", ShowCurtailment);
			Button(flowLayoutPanel, "Add entry", delegate
			{
				AddEntry();
			});
			Button(flowLayoutPanel, "Add manual schedule", AddManualSchedule);
			Button(flowLayoutPanel, "Remove selected", RemoveSelected);
			Button(flowLayoutPanel, "Save estimate", SaveProject);
			Button(flowLayoutPanel, "Open estimate", OpenProject);
			Button(flowLayoutPanel, "Export / print PDF", ExportPdf);
			Button(flowLayoutPanel, "Commercial lengths", delegate
			{
				if (Allowed())
				{
					List<double> list = StockLengthPicker.Choose(this, stockLengths);
					if (list != null)
					{
						stockLengths = list;
						Recalculate();
					}
				}
			});
			Button(flowLayoutPanel, "Commercial bars", delegate
			{
				if (Allowed())
				{
					beams.EndEdit();
					Recalculate();
					CalculationPreview.Show(this, Report());
				}
			});
			Button(flowLayoutPanel, "Print Preview", delegate
			{
				if (Allowed())
				{
					CalculationPreview.Show(this, Report());
				}
			});
			Button(flowLayoutPanel, "Splice / development / hooks", delegate
			{
				RebarLengthForm rebarLengthForm = new RebarLengthForm();
				rebarLengthForm.ApplyConfiguration(lengthProfile);
				rebarLengthForm.Show(this);
			});
			Button(flowLayoutPanel, "Open / Link Excel", LinkExcel);
			Button(flowLayoutPanel, "Disconnect Excel", DisconnectExcel);
			excelStatus = new Label
			{
				Dock = DockStyle.Bottom,
				Height = 24,
				Text = "Excel: not linked",
				Padding = new Padding(10, 2, 0, 0)
			};
			base.Controls.Add(excelStatus);
			excelTimer.Tick += delegate
			{
				excelTimer.Stop();
				try
				{
					SyncExcel();
				}
				catch (Exception ex)
				{
					excelStatus.Text = "Excel link paused: " + ex.Message;
				}
			};
			base.FormClosed += delegate
			{
				excelTimer.Dispose();
				if (excelLink != null)
				{
					excelLink.Dispose();
				}
			};
			FlowLayoutPanel settings = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				Height = 142,
				Padding = new Padding(8),
				WrapContents = true,
				AutoScroll = true
			};
			gradeSmall = Select(settings, "12mm & below grade", new string[2] { "Grade 40 (280 MPa)", "Grade 60 (420 MPa)" });
			grade = Select(settings, "16mm & above grade", new string[2] { "Grade 60 (420 MPa)", "Grade 40 (280 MPa)" });
			concrete = Select(settings, "Concrete f'c", new string[3] { "21 MPa", "28 MPa", "35 MPa" });
			mainHook = Select(settings, "Main beam end hooks", new string[3] { "Straight (no hooks)", "90deg exterior ends", "180deg exterior ends" });
			anchorage = Select(settings, "Main piece allowance", new string[4] { "None (detail required)", "Ld at both piece ends", "1 x lap (config A/B)", "Hook Ldh / cut-end Ld" });
			bond = Number(settings, "(cb+Ktr)/db", 0.5m, 2.5m, 1m, 2);
			cover = Number(settings, "Cover mm", 0m, 200m, 40m, 0);
			hook = Number(settings, "Tie tail min x dia", 6m, 50m, 6m, 1);
			extra = Number(settings, "Extra m / piece", 0m, 10m, 0m, 2);
			waste = Number(settings, "Waste %", 0m, 100m, 0m, 1);
			reportMode = Select(settings, "Report detail", new string[3] { "Summary", "Detailed", "Extensive" });
			sourceLabel = new Label
			{
				Dock = DockStyle.Top,
				Height = 60,
				AutoEllipsis = true,
				BackColor = Color.White,
				Padding = new Padding(10, 3, 10, 0),
				Text = "Load the REVISED DXF exported by Beam Formatter.\nEnter each takeoff item: Description > revised beam mark > length (m) > quantity. Repeat marks freely for different items."
			};
			status = new Label
			{
				Dock = DockStyle.Bottom,
				Height = 30,
				Padding = new Padding(10, 6, 0, 0),
				BackColor = Color.White
			};
			beams = Grid();
			beams.Dock = DockStyle.Fill;
			beams.AllowUserToAddRows = true;
			beams.AllowUserToDeleteRows = true;
			beams.MultiSelect = true;
			beams.SelectionMode = DataGridViewSelectionMode.CellSelect;
			beams.Columns.Add("Description", "DESCRIPTION");
			DataGridViewComboBoxColumn dataGridViewComboBoxColumn = new DataGridViewComboBoxColumn
			{
				Name = "Mark",
				HeaderText = "BEAM MARK",
				DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
				FlatStyle = FlatStyle.Flat
			};
			dataGridViewComboBoxColumn.Items.Add("");
			beams.Columns.Add(dataGridViewComboBoxColumn);
			string[] array = new string[11]
			{
				"B mm", "D mm", "LENGTH m", "QUANTITY", "TIE SETS / beam override", "TIE CUT m/set override", "MAIN BARS kg", "STIRRUPS kg", "SFR kg", "TOTAL kg",
				"STATUS"
			};
			foreach (string headerText in array)
			{
				beams.Columns.Add("C" + beams.Columns.Count, headerText);
			}
			DataGridViewComboBoxColumn dataGridViewComboBoxColumn2 = new DataGridViewComboBoxColumn
			{
				Name = "Schedule",
				HeaderText = "FLOOR / SOURCE DXF",
				Width = 230,
				FlatStyle = FlatStyle.Flat,
				DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
			};
			beams.Columns.Add(dataGridViewComboBoxColumn2);
			dataGridViewComboBoxColumn2.DisplayIndex = 1;
			int[] array2 = new int[13]
			{
				210, 125, 60, 60, 90, 85, 125, 140, 110, 110,
				100, 110, 260
			};
			for (int num2 = 0; num2 < array2.Length; num2++)
			{
				beams.Columns[num2].Width = array2[num2];
				beams.Columns[num2].ReadOnly = num2 == 2 || num2 == 3 || num2 >= 8;
				if (!beams.Columns[num2].ReadOnly)
				{
					beams.Columns[num2].DefaultCellStyle.BackColor = Color.FromArgb(255, 249, 222);
				}
			}
			beams.Columns[2].Visible = false;
			beams.Columns[3].Visible = false;
			beams.Columns[0].Frozen = true;
			beams.Columns[1].Frozen = true;
			dataGridViewComboBoxColumn2.Frozen = true;
			dataGridViewComboBoxColumn2.Visible = false;
			DataGridViewComboBoxColumn dataGridViewComboBoxColumn3 = new DataGridViewComboBoxColumn
			{
				Name = "SupportCondition",
				HeaderText = "BEAM SUPPORT",
				Width = 145,
				DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
			};
			dataGridViewComboBoxColumn3.Items.AddRange("Elevated slab", "On gravel fill", "Ground beam");
			beams.Columns.Add(dataGridViewComboBoxColumn3);
			beams.Columns.Add("SlabThickness", "SLAB THICKNESS mm (side forms deduction)");
			beams.Columns[15].Width = 150;
			beams.Columns.Add("Concrete", "CONCRETE m3");
			beams.Columns.Add("Formwork", "FORMWORK m2");
			beams.Columns[16].ReadOnly = (beams.Columns[17].ReadOnly = true);
			dataGridViewComboBoxColumn3.DisplayIndex = beams.Columns[5].DisplayIndex + 1;
			beams.Columns[15].DisplayIndex = dataGridViewComboBoxColumn3.DisplayIndex + 1;
			dataGridViewComboBoxColumn3.DefaultCellStyle.BackColor = (beams.Columns[15].DefaultCellStyle.BackColor = Color.FromArgb(255, 249, 222));
			string[] array3 = new string[12]
			{
				"ExcDepth", "WorkingAllowance", "Swell", "Shrink", "OtherBuriedConcrete", "ExcBank", "Backfill", "LooseExc", "LooseFill", "Surplus",
				"Import", "EarthStatus"
			};
			string[] array4 = new string[12]
			{
				"EXC DEPTH m (blank=auto)", "ALLOWANCE m / SIDE", "SWELL % bank to loose", "SHRINK % bank to compacted", "OTHER BURIED CONCRETE m3/beam", "EXCAVATION bank m3", "BACKFILL compacted m3", "EXCAVATED loose m3", "FILL REQUIRED loose m3", "SURPLUS loose m3",
				"COMMON BORROW loose m3", "EARTHWORK STATUS"
			};
			for (int num3 = 0; num3 < array3.Length; num3++)
			{
				int index = beams.Columns.Add(array3[num3], array4[num3]);
				beams.Columns[index].Width = ((num3 == 11) ? 260 : 140);
				beams.Columns[index].ReadOnly = num3 >= 5;
				if (num3 < 5)
				{
					beams.Columns[index].DefaultCellStyle.BackColor = Color.FromArgb(255, 249, 222);
				}
			}
			beams.Columns["ExcDepth"].ToolTipText = "Ground beam only. Blank automatically follows beam D/1000 + 0.10m gravel. Enter actual depth from ground level if tie beams are lower. Clear to restore automatic depth.";
			beams.Columns["WorkingAllowance"].ToolTipText = "Horizontal working space EACH side: trench width = B + 2 x allowance. Fixed app estimating allowance: 0.30m EACH side (B + 0.60m total). Rectangular trench only; not a universal code minimum.";
			beams.Columns["Swell"].ToolTipText = "Loose volume = bank excavation x (1 + swell/100). Common estimating default: 20% (bank x 1.20). Editable if project soil data specifies another value.";
			beams.Columns["Shrink"].ToolTipText = "Compacted volume = bank soil x (1 - shrink/100). Default 10% is provisional. This is volume shrinkage, NOT a density compaction target.";
			beams.Columns["OtherBuriedConcrete"].ToolTipText = "Additional concrete displacing backfill, m3 per beam (e.g. a lower tie beam). Already excludes this row's beam concrete. Do not count shared trenches/concrete again in another row.";
			beams.CellFormatting += delegate(object sender, DataGridViewCellFormattingEventArgs args)
			{
				if (args.ColumnIndex == 18 && args.RowIndex >= 0)
				{
					DataGridViewRow r = beams.Rows[args.RowIndex];
					if (Cell(r, 18) == "" && (Cell(r, 14) == "Ground beam" || Cell(r, 14) == "On gravel fill"))
					{
						BeamRow beamRow = FindSchedule(Cell(r, 13), Cell(r, 1));
						double result;
						if (beamRow != null && double.TryParse(beamRow.Cell(1), NumberStyles.Float, CultureInfo.InvariantCulture, out result))
						{
							args.Value = (result / 1000.0 + 0.1).ToString("0.00") + " (auto)";
							args.FormattingApplied = true;
						}
					}
				}
			};
			array = new string[2] { "Embankment from excavation loose m3", "Embankment common borrow loose m3" };
			foreach (string text in array)
			{
				int index2 = beams.Columns.Add(text, text);
				beams.Columns[index2].ReadOnly = true;
				beams.Columns[index2].Width = 160;
			}
			for (int num4 = 24; num4 <= 28; num4++)
			{
				beams.Columns[num4].Visible = false;
			}
			beams.Columns[30].ToolTipText = "Loose excavated soil actually needed for backfill; excludes surplus. Quantity before compaction.";
			beams.Columns[31].ToolTipText = "Loose common borrow to obtain/deliver to complete backfill after compaction. Includes conversion allowance; do not add another factor.";
			beams.DefaultValuesNeeded += delegate(object o, DataGridViewRowEventArgs e)
			{
				e.Row.Cells[1].Value = "";
				e.Row.Cells[5].Value = "1";
				e.Row.Cells[14].Value = "Elevated slab";
				e.Row.Cells[15].Value = "0";
				if (sources.Count > 0)
				{
					e.Row.Cells[13].Value = sources[0].Label;
				}
			};
			beams.CellParsing += delegate(object o, DataGridViewCellParsingEventArgs e)
			{
				if (e.ColumnIndex == 18 && Convert.ToString(e.Value).EndsWith(" (auto)"))
				{
					e.Value = "";
					e.ParsingApplied = true;
				}
				if (e.ColumnIndex == 1 && e.Value != null)
				{
					e.Value = e.Value.ToString().Trim().ToUpperInvariant();
					e.ParsingApplied = true;
				}
			};
			object originalEditValue = null;
			beams.CellBeginEdit += delegate(object o, DataGridViewCellCancelEventArgs e)
			{
				originalEditValue = beams.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
			};
			beams.CellEndEdit += delegate(object o, DataGridViewCellEventArgs e)
			{
				if (!filling && !object.Equals(originalEditValue, beams.Rows[e.RowIndex].Cells[e.ColumnIndex].Value))
				{
					Recalculate();
				}
			};
			beams.CellValueChanged += delegate(object o, DataGridViewCellEventArgs e)
			{
				if (!filling && (e.ColumnIndex == 1 || e.ColumnIndex == 13 || e.ColumnIndex == 14))
				{
					if (e.ColumnIndex == 13 && e.RowIndex >= 0)
					{
						DataGridViewRow dataGridViewRow = beams.Rows[e.RowIndex];
						ConfigureMarkCell(dataGridViewRow);
						if (FindSchedule(Cell(dataGridViewRow, 13), Cell(dataGridViewRow, 1)) == null)
						{
							dataGridViewRow.Cells[1].Value = "";
						}
					}
					Recalculate();
				}
			};
			beams.RowsRemoved += delegate
			{
				if (!filling)
				{
					Recalculate();
				}
			};
			beams.SelectionChanged += delegate
			{
				ShowSelected();
			};
			beams.CurrentCellDirtyStateChanged += delegate
			{
				if (beams.IsCurrentCellDirty && beams.CurrentCell is DataGridViewComboBoxCell)
				{
					beams.CommitEdit(DataGridViewDataErrorContexts.Commit);
				}
			};
			beams.EditingControlShowing += delegate(object o, DataGridViewEditingControlShowingEventArgs e)
			{
				ComboBox comboBox = e.Control as ComboBox;
				if (comboBox != null)
				{
					comboBox.DropDownStyle = ((beams.CurrentCell.ColumnIndex != 13 && beams.CurrentCell.ColumnIndex != 14) ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList);
					comboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
					comboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
				}
			};
			beams.CellValidating += delegate(object o, DataGridViewCellValidatingEventArgs e)
			{
				if (!filling && e.ColumnIndex == 1 && !beams.Rows[e.RowIndex].IsNewRow)
				{
					string text4 = Convert.ToString(e.FormattedValue).Trim().ToUpperInvariant();
					if (text4 != "" && FindSchedule(Cell(beams.Rows[e.RowIndex], 13), text4) == null)
					{
						beams.Rows[e.RowIndex].ErrorText = "Beam mark is not in the loaded revised DXF.";
						e.Cancel = true;
					}
					else
					{
						beams.Rows[e.RowIndex].ErrorText = "";
					}
				}
			};
			beams.DataError += delegate(object o, DataGridViewDataErrorEventArgs e)
			{
				e.ThrowException = false;
				status.Text = "Choose a revised beam mark from the loaded schedule.";
			};
			SplitContainer splitContainer = new SplitContainer
			{
				Dock = DockStyle.Bottom,
				Height = 235,
				Width = 1300,
				SplitterDistance = 850,
				FixedPanel = FixedPanel.Panel2
			};
			details = new TextBox
			{
				Dock = DockStyle.Fill,
				ReadOnly = true,
				Multiline = true,
				ScrollBars = ScrollBars.Both,
				WordWrap = false,
				Font = new Font("Consolas", 9f),
				BackColor = Color.FromArgb(237, 243, 248)
			};
			splitContainer.Panel1.Controls.Add(details);
			totals = Grid();
			totals.Dock = DockStyle.Fill;
			totals.ReadOnly = true;
			totals.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
			array = new string[4] { "DIA mm", "NET kg", "WASTE kg", "TOTAL kg" };
			foreach (string text2 in array)
			{
				totals.Columns.Add(text2, text2);
			}
			TabControl tabControl = new TabControl
			{
				Dock = DockStyle.Fill
			};
			TabPage tabPage = new TabPage("Grade 40 / Grade 60");
			TabPage tabPage2 = new TabPage("By diameter");
			gradeTotals = Grid();
			gradeTotals.Dock = DockStyle.Fill;
			gradeTotals.ReadOnly = true;
			gradeTotals.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
			array = new string[5] { "GRADE", "DIA mm", "NET kg", "WASTE kg", "TOTAL kg" };
			foreach (string text3 in array)
			{
				gradeTotals.Columns.Add(text3, text3);
			}
			tabPage.Controls.Add(gradeTotals);
			tabPage2.Controls.Add(totals);
			tabControl.TabPages.Add(tabPage);
			tabControl.TabPages.Add(tabPage2);
			splitContainer.Panel2.Controls.Add(tabControl);
			calculationLabel = new Label
			{
				Dock = DockStyle.Top,
				Height = 44,
				Padding = new Padding(10, 5, 8, 0),
				BackColor = Color.FromArgb(227, 238, 249),
				ForeColor = Color.FromArgb(25, 57, 91)
			};
			base.Controls.Add(beams);
			base.Controls.Add(splitContainer);
			base.Controls.Add(sourceLabel);
			base.Controls.Add(calculationLabel);
			base.Controls.Add(settings);
			WorksSettings = settings;
			settings.Visible = false;
			beams.Columns[6].Visible = (beams.Columns[7].Visible = (beams.Columns[15].Visible = false));
			Button(flowLayoutPanel, "Settings", delegate
			{
				SettingsWorkspace.Show(this, "Beam", new string[1] { "Materials & calculation" }, new Control[1] { settings });
			});
			SettingsWorkspace.MoveButtons(flowLayoutPanel, settings, "Commercial lengths", "Curtailment settings", "Curtailment Settings", "Splice / development / hooks", "Special cases (optional)");
			base.Controls.Add(flowLayoutPanel);
			base.Controls.Add(status);
			reportMode.SelectedIndexChanged += delegate
			{
				if (!filling)
				{
					Recalculate();
				}
			};
			NumericUpDown[] array5 = new NumericUpDown[5] { cover, hook, extra, waste, bond };
			for (int num = 0; num < array5.Length; num++)
			{
				array5[num].ValueChanged += delegate
				{
					if (!filling)
					{
						Recalculate();
					}
				};
			}
			ComboBox[] array6 = new ComboBox[3] { grade, gradeSmall, concrete };
			for (int num = 0; num < array6.Length; num++)
			{
				array6[num].SelectedIndexChanged += delegate
				{
					if (!filling)
					{
						profileSnapshot = false;
						AutoConfiguration();
					}
				};
			}
			mainHook.SelectedIndexChanged += delegate
			{
				if (!filling)
				{
					filling = true;
					if (mainHook.SelectedIndex != 0 && anchorage.SelectedIndex == 1)
					{
						anchorage.SelectedIndex = 3;
					}
					else if (mainHook.SelectedIndex == 0 && anchorage.SelectedIndex == 3)
					{
						anchorage.SelectedIndex = 1;
					}
					filling = false;
					Recalculate();
				}
			};
			anchorage.SelectedIndexChanged += delegate
			{
				if (!filling)
				{
					Recalculate();
				}
			};
			base.FormClosing += delegate(object o, FormClosingEventArgs e)
			{
				beams.EndEdit();
				Recalculate();
				excelTimer.Stop();
				if (excelLink != null)
				{
					try
					{
						SyncExcel();
					}
					catch (Exception ex)
					{
						e.Cancel = true;
						MessageBox.Show(this, "Cannot save the estimate to Excel: " + ex.Message + "\nThe app will remain open. Save the workbook or disconnect after making a separate estimate backup.", "Excel save failed");
						return;
					}
				}
				if (inputs.Count > 0 && State() != savedState && AccessCodes.CanUseFormatter)
				{
					switch (MessageBox.Show(this, "Save estimate entries before closing?", "Beam estimate", MessageBoxButtons.YesNoCancel))
					{
					case DialogResult.Cancel:
						e.Cancel = true;
						break;
					case DialogResult.Yes:
						try
						{
							if (!SaveProjectDialog())
							{
								e.Cancel = true;
							}
							break;
						}
						catch (Exception ex2)
						{
							MessageBox.Show(ex2.Message);
							e.Cancel = true;
							break;
						}
					}
				}
			};
			help.SetToolTip(gradeSmall, "Steel grade for bars 12mm and below; used by the saved calculation configuration.");
			help.SetToolTip(grade, "Steel grade for bars 16mm and above.");
			help.SetToolTip(concrete, "Concrete strength used to obtain computed Ld and lap lengths.");
			help.SetToolTip(reportMode, "Summary, Detailed or Extensive purchase backup.");
			help.SetToolTip(cover, "Concrete cover in millimetres.");
			help.SetToolTip(extra, "Additional length per actual bar piece in metres; no percentage waste is added.");
			beams.Columns[14].ToolTipText = "Full-depth side forms. Elevated adds bottom forms; gravel/ground has side forms only.";
			beams.Columns[15].ToolTipText = "Retired input: side forms always use full beam depth.";
			base.FormClosed += delegate
			{
				help.Dispose();
			};
			InitializeMaterials();
			anchorage.SelectedIndex = 1;
			AutoConfiguration();
			CalculationStore.Changed += StoredConfigurationChanged;
			base.FormClosed += delegate
			{
				CalculationStore.Changed -= StoredConfigurationChanged;
			};
			if (initial != null)
			{
				LoadCatalog(RevisedSchedule.Serialize(initial.Drawing), "Current formatter revised preview");
			}
			else
			{
				Recalculate();
			}
		}

		private void LinkExcel()
		{
			if (!Allowed())
			{
				return;
			}
			using (SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				Filter = "Excel macro-enabled workbook|*.xlsm|Excel workbook|*.xlsx",
				FileName = "Beam-Summary.xlsm",
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
					if (flag && catalog != null)
					{
						DialogResult dialogResult = MessageBox.Show(this, "This workbook contains a saved estimate.\nYes: load the workbook estimate.\nNo: replace its saved estimate with the current app estimate.\nCancel: leave the connection unchanged.", "Choose estimate to use", MessageBoxButtons.YesNoCancel);
						if (dialogResult == DialogResult.Cancel)
						{
							excelLiveLink.Dispose();
							return;
						}
						flag = dialogResult == DialogResult.Yes;
					}
					excelTimer.Stop();
					ExcelLiveLink excelLiveLink2 = excelLink;
					excelLink = null;
					try
					{
						if (flag)
						{
							OpenEstimateXml(text);
						}
						else if (catalog == null)
						{
							throw new Exception("Load a DXF or choose a workbook with a saved estimate first.");
						}
					}
					catch
					{
						excelLink = excelLiveLink2;
						throw;
					}
					if (excelLiveLink2 != null)
					{
						excelLiveLink2.Dispose();
					}
					excelLink = excelLiveLink;
					beams.EndEdit();
					Recalculate();
					SyncExcel();
				}
				catch
				{
					if (excelLink != excelLiveLink)
					{
						excelLiveLink.Dispose();
					}
					throw;
				}
			}
		}

		private void DisconnectExcel()
		{
			beams.EndEdit();
			Recalculate();
			excelTimer.Stop();
			if (excelLink != null)
			{
				try
				{
					SyncExcel();
				}
				catch (Exception ex)
				{
					if (MessageBox.Show(this, "Excel save failed: " + ex.Message + "\nDisconnect without saving the latest changes to Excel? Save a separate estimate backup if needed.", "Disconnect Excel", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes)
					{
						return;
					}
				}
			}
			if (excelLink != null)
			{
				excelLink.Dispose();
			}
			excelLink = null;
			excelStatus.Text = "Excel: disconnected (workbook stays open)";
		}

		internal string[] WorksValidationIssues()
		{
			List<string> list = new List<string>();
			foreach (EstimateInput input in inputs)
			{
				if (input.Result == null)
				{
					list.Add("Beam " + input.Beam + ": " + input.Error);
				}
				if (input.Measures == null)
				{
					list.Add("Beam " + input.Beam + " concrete/formwork: " + input.MeasureError);
				}
				if (input.Support != "Elevated slab" && input.Earthworks == null)
				{
					list.Add("Beam " + input.Beam + " earthworks: " + input.EarthworkError);
				}
			}
			foreach (StockAllocation item in from a in WorksPlan()
				where a.Choice == null
				select a)
			{
				list.Add("Beam " + item.Entry.Beam + " / " + item.Line.Part + ": cut " + item.Demand.Cut.ToString("0.###", CultureInfo.InvariantCulture) + "m cannot be allocated to the checked stock lengths; review splice settings.");
			}
			return list.ToArray();
		}

		public object[,] ExcelRows(List<StockAllocation> shared = null)
		{
			List<StockAllocation> source = ((shared == null) ? CommercialBars.Allocate(inputs, lengthProfile, stockLengths) : shared.Where((StockAllocation a) => inputs.Contains(a.Entry)).ToList());
			if (source.Any((StockAllocation a) => a.Choice == null))
			{
				throw new Exception("Purchase plan has unresolved cuts; review Commercial bars.");
			}
			List<object[]> list = new List<object[]>();
			foreach (EstimateInput i in inputs.Where((EstimateInput x) => x.Result != null))
			{
				foreach (var item in from a in source
					where a.Entry == i
					group a by new
					{
						a.Demand.Grade,
						a.Demand.Diameter
					})
				{
					double num = item.Where((StockAllocation a) => a.Line.Part != "Stirrups" && a.Line.Part != "Side-face").Sum((StockAllocation a) => a.Net);
					double num2 = item.Where((StockAllocation a) => a.Line.Part == "Stirrups").Sum((StockAllocation a) => a.Net);
					double num3 = item.Where((StockAllocation a) => a.Line.Part == "Side-face").Sum((StockAllocation a) => a.Net);
					list.Add(new object[12]
					{
						i.Description,
						i.Beam,
						i.ScheduleLabel,
						item.Key.Grade,
						item.Key.Diameter,
						num,
						num2,
						num3,
						item.Sum((StockAllocation a) => a.Splice + a.Waste),
						item.Sum((StockAllocation a) => a.Purchase),
						Read(i.Length, "length"),
						int.Parse(i.Quantity)
					});
				}
			}
			object[,] array = new object[list.Count, 12];
			for (int num4 = 0; num4 < list.Count; num4++)
			{
				for (int num5 = 0; num5 < 12; num5++)
				{
					array[num4, num5] = list[num4][num5];
				}
			}
			return array;
		}

		private void SyncExcel()
		{
			if (excelLink != null)
			{
				excelLink.Update(ExcelRows(), inputs.Any((EstimateInput i) => i.Result == null) ? "PARTIAL - invalid entries excluded" : "Purchase kg; allowance column includes computed splice + stock offcut", EstimateXml(), ConcreteExcelRows());
				savedState = State();
				excelStatus.Text = "Excel synced " + DateTime.Now.ToString("HH:mm:ss") + " | " + excelLink.PathName;
			}
		}

		private static DataGridView Grid()
		{
			DataGridView obj = new DataGridView
			{
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = false,
				RowHeadersVisible = false,
				BackgroundColor = Color.White,
				BorderStyle = BorderStyle.None,
				EnableHeadersVisualStyles = false,
				SelectionMode = DataGridViewSelectionMode.CellSelect,
				ColumnHeadersHeight = 42,
				AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
				ColumnHeadersDefaultCellStyle = 
				{
					BackColor = Color.FromArgb(20, 33, 51),
					ForeColor = Color.White,
					Font = new Font("Segoe UI", 9f, FontStyle.Bold)
				},
				DefaultCellStyle = 
				{
					Padding = new Padding(4),
					WrapMode = DataGridViewTriState.True
				},
				AlternatingRowsDefaultCellStyle = 
				{
					BackColor = Color.FromArgb(247, 249, 252)
				}
			};
			GridAppearance.Apply(obj);
			return obj;
		}

		private static void Button(FlowLayoutPanel p, string label, Action action)
		{
			Button button = new Button
			{
				Text = label,
				AutoSize = true,
				Height = 30,
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.White
			};
			button.Click += delegate
			{
				try
				{
					action();
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "Beam estimator", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
			};
			p.Controls.Add(button);
		}

		private static NumericUpDown Number(FlowLayoutPanel p, string label, decimal min, decimal max, decimal value, int decimals)
		{
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Width = 120,
				Height = 58,
				FlowDirection = FlowDirection.TopDown,
				WrapContents = false,
				Margin = new Padding(3, 0, 3, 0)
			};
			flowLayoutPanel.Controls.Add(new Label
			{
				Text = label,
				UseMnemonic = false,
				Width = 120,
				Height = 20
			});
			NumericUpDown numericUpDown = new NumericUpDown
			{
				Minimum = min,
				Maximum = max,
				Value = value,
				DecimalPlaces = decimals,
				Increment = ((decimals == 0) ? 1m : 0.25m),
				Width = 114
			};
			flowLayoutPanel.Controls.Add(numericUpDown);
			p.Controls.Add(flowLayoutPanel);
			return numericUpDown;
		}

		private static ComboBox Select(FlowLayoutPanel p, string label, string[] values)
		{
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Width = 170,
				Height = 58,
				FlowDirection = FlowDirection.TopDown,
				WrapContents = false
			};
			flowLayoutPanel.Controls.Add(new Label
			{
				Text = label,
				UseMnemonic = false,
				Width = 170,
				Height = 20
			});
			ComboBox comboBox = new ComboBox
			{
				Width = 165,
				DropDownStyle = ComboBoxStyle.DropDownList
			};
			comboBox.Items.AddRange(values);
			comboBox.SelectedIndex = 0;
			flowLayoutPanel.Controls.Add(comboBox);
			p.Controls.Add(flowLayoutPanel);
			return comboBox;
		}

		private bool Allowed()
		{
			if (AccessCodes.CanUseApp(2))
			{
				return true;
			}
			MessageBox.Show("Renew the eight-hour access to continue.");
			return false;
		}

		private EstimateOptions Options()
		{
			return new EstimateOptions
			{
				LengthConfig = lengthProfile,
				GradeSmall = ((SteelSelection)gradeSmall.SelectedItem).Grade,
				Grade = ((SteelSelection)grade.SelectedItem).Grade,
				Fc = (double)concrete.SelectedItem,
				MainHook = ((mainHook.SelectedIndex != 0) ? ((mainHook.SelectedIndex == 1) ? 90 : 180) : 0),
				Anchorage = anchorage.SelectedIndex,
				BondRatio = (double)bond.Value,
				Left = 0.25,
				Right = 0.25,
				Cover = (double)cover.Value,
				Hook = (double)hook.Value,
				Extra = (double)extra.Value,
				Waste = 0.0,
				Offset = 50.0
			};
		}

		private bool ReplaceOkay()
		{
			beams.EndEdit();
			Recalculate();
			if (inputs.Count != 0 && !(State() == savedState))
			{
				return MessageBox.Show(this, "Replace the current estimate entries? Save them first if needed.", "Load schedule", MessageBoxButtons.OKCancel) == DialogResult.OK;
			}
			return true;
		}

		private void Choose()
		{
			if (!Allowed() || !ReplaceOkay())
			{
				return;
			}
			using (OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = "Beam Formatter revised DXF (*.dxf)|*.dxf",
				Multiselect = true
			})
			{
				if (openFileDialog.ShowDialog(this) != DialogResult.OK)
				{
					return;
				}
				List<string[]> list = openFileDialog.FileNames.Select((string path) => new string[2]
				{
					File.ReadAllText(path),
					Path.GetFileName(path)
				}).ToList();
				ValidateFiles(list, false);
				LoadCatalog(list[0][0], list[0][1]);
				foreach (string[] item in list.Skip(1))
				{
					AppendCatalog(item[0], item[1]);
				}
			}
		}

		private void ChooseAdditional()
		{
			if (!Allowed())
			{
				return;
			}
			using (OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = "Beam Formatter revised DXF (*.dxf)|*.dxf",
				Multiselect = true
			})
			{
				if (openFileDialog.ShowDialog(this) != DialogResult.OK)
				{
					return;
				}
				List<string[]> list = openFileDialog.FileNames.Select((string path) => new string[2]
				{
					File.ReadAllText(path),
					Path.GetFileName(path)
				}).ToList();
				ValidateFiles(list, true);
				foreach (string[] item in list)
				{
					AppendCatalog(item[0], item[1]);
				}
			}
		}

		public void LoadCatalog(string raw, string name)
		{
			ManualBeamSchedule.Read(raw, name);
			filling = true;
			try
			{
				beams.Rows.Clear();
				sources.Clear();
				schedules.Clear();
				inputs.Clear();
				((DataGridViewComboBoxColumn)beams.Columns[1]).Items.Clear();
				((DataGridViewComboBoxColumn)beams.Columns[13]).Items.Clear();
			}
			finally
			{
				filling = false;
			}
			AppendCatalog(raw, name);
			savedState = State();
		}

		public void AppendCatalog(string raw, string name)
		{
			if (sources.Any((ScheduleSource s) => s.Raw == raw))
			{
				return;
			}
			Analysis analysis = ManualBeamSchedule.Read(raw, name);
			CheckUniqueMarks(sources.Concat(new ScheduleSource[1]
			{
				new ScheduleSource
				{
					Name = name,
					Raw = raw,
					Catalog = analysis,
					Rows = analysis.Source.ToDictionary((BeamRow r) => r.Mark, StringComparer.OrdinalIgnoreCase)
				}
			}));
			string label = analysis.Floor + " | " + name;
			string text = label;
			int num = 2;
			while (sources.Any((ScheduleSource s) => s.Label == label))
			{
				label = text + " (" + num++ + ")";
			}
			ScheduleSource item = new ScheduleSource
			{
				Label = label,
				Name = name,
				Raw = raw,
				Catalog = analysis,
				Rows = analysis.Source.ToDictionary((BeamRow r) => r.Mark, StringComparer.OrdinalIgnoreCase)
			};
			filling = true;
			try
			{
				sources.Add(item);
				catalog = sources[0].Catalog;
				sourceText = sources[0].Raw;
				sourceName = string.Join("; ", sources.Select((ScheduleSource s) => s.Name));
				DataGridViewComboBoxColumn dataGridViewComboBoxColumn = (DataGridViewComboBoxColumn)beams.Columns[1];
				if (!dataGridViewComboBoxColumn.Items.Contains(""))
				{
					dataGridViewComboBoxColumn.Items.Add("");
				}
				foreach (BeamRow item2 in analysis.Source)
				{
					schedules[item2.Mark] = item2;
					if (!dataGridViewComboBoxColumn.Items.Contains(item2.Mark))
					{
						dataGridViewComboBoxColumn.Items.Add(item2.Mark);
					}
				}
				((DataGridViewComboBoxColumn)beams.Columns[13]).Items.Add(label);
				foreach (DataGridViewRow item3 in (IEnumerable)beams.Rows)
				{
					if (!item3.IsNewRow)
					{
						ConfigureMarkCell(item3);
					}
				}
			}
			finally
			{
				filling = false;
			}
			UpdateLoadedDxf();
			Recalculate();
		}

		internal void AddManual(BeamRow row)
		{
			ManualBeamSchedule.Validate(row);
			if (schedules.ContainsKey(row.Mark))
			{
				throw new Exception("Beam mark already exists: " + row.Mark);
			}
			ScheduleSource scheduleSource = sources.FirstOrDefault((ScheduleSource x) => x.Name == "Manual beam schedules");
			Analysis analysis = new Analysis();
			if (scheduleSource != null)
			{
				analysis.Source.AddRange(scheduleSource.Catalog.Source);
			}
			analysis.Source.Add(row);
			string raw = ManualBeamSchedule.Serialize(analysis);
			if (scheduleSource == null)
			{
				AppendCatalog(raw, "Manual beam schedules");
				return;
			}
			scheduleSource.Raw = raw;
			scheduleSource.Catalog = ManualBeamSchedule.Read(raw, scheduleSource.Name);
			scheduleSource.Rows = scheduleSource.Catalog.Source.ToDictionary((BeamRow x) => x.Mark, StringComparer.OrdinalIgnoreCase);
			schedules[row.Mark] = row;
			((DataGridViewComboBoxColumn)beams.Columns[1]).Items.Add(row.Mark);
			foreach (DataGridViewRow item in (IEnumerable)beams.Rows)
			{
				if (!item.IsNewRow)
				{
					ConfigureMarkCell(item);
				}
			}
			UpdateLoadedDxf();
			Recalculate();
		}

		private void AddManualSchedule()
		{
			if (Allowed())
			{
				BeamRow beamRow = ManualBeamSchedule.Choose(this);
				if (beamRow != null)
				{
					AddManual(beamRow);
				}
			}
		}

		internal List<EarthworkMaterial> WorksEarthworkMaterials()
		{
			List<EarthworkMaterial> list = new List<EarthworkMaterial>();
			foreach (EstimateInput item in inputs.Where((EstimateInput x) => x.Support != "Elevated slab"))
			{
				if (item.Earthworks == null)
				{
					throw new Exception("Invalid beam earthworks: " + item.Beam);
				}
				BeamEarthworks earthworks = item.Earthworks;
				list.Add(new EarthworkMaterial
				{
					Id = item.Beam + " " + item.Description,
					Source = "Beam",
					Bank = earthworks.Excavation,
					Fill = earthworks.Backfill,
					Swell = earthworks.Swell,
					Shrink = earthworks.Shrink,
					Reusable = 100.0
				});
			}
			return list;
		}

		internal double WorksGravel()
		{
			return inputs.Where((EstimateInput i) => i.Earthworks != null).Sum((EstimateInput i) => i.Earthworks.Gravel);
		}

		internal object[,] EarthworkExcelRows()
		{
			List<EstimateInput> list = inputs.Where((EstimateInput i) => i.Earthworks != null).ToList();
			object[,] array = new object[list.Count, 18];
			for (int num = 0; num < list.Count; num++)
			{
				EstimateInput estimateInput = list[num];
				BeamEarthworks earthworks = estimateInput.Earthworks;
				object[] array2 = new object[18]
				{
					estimateInput.Description,
					estimateInput.Beam,
					int.Parse(estimateInput.Quantity),
					earthworks.Width,
					earthworks.Depth,
					earthworks.Allowance,
					earthworks.Swell,
					earthworks.Shrink,
					earthworks.Excavation,
					earthworks.Backfill,
					earthworks.Gravel,
					earthworks.LooseExcavation,
					earthworks.LooseRequired,
					earthworks.LooseSurplus,
					earthworks.LooseImport,
					earthworks.OtherConcrete,
					earthworks.LooseFromExcavation,
					earthworks.LooseImport
				};
				for (int num2 = 0; num2 < array2.Length; num2++)
				{
					array[num, num2] = array2[num2];
				}
			}
			return array;
		}

		internal void AppendEarthworksReport(StringBuilder b, bool detailed)
		{
			List<EstimateInput> list = inputs.Where((EstimateInput i) => i.Support != "Elevated slab").ToList();
			if (list.Count == 0)
			{
				return;
			}
			b.AppendLine();
			b.AppendLine("GROUND BEAM EARTHWORKS");
			foreach (EstimateInput item in list)
			{
				b.AppendLine(item.Description + " | " + item.Beam + " | Qty " + item.Quantity);
				if (item.Earthworks == null)
				{
					b.AppendLine("  NOT MEASURED: " + item.EarthworkError);
				}
				else
				{
					item.Earthworks.Report(b, detailed);
				}
			}
			List<BeamEarthworks> source = (from i in list
				where i.Earthworks != null
				select i.Earthworks).ToList();
			b.AppendLine(string.Format(CultureInfo.InvariantCulture, "TOTAL: Excavation {0:0.00} bank m3 | Gravel {2:0.00} m3", source.Sum((BeamEarthworks x) => x.Excavation), source.Sum((BeamEarthworks x) => x.Backfill), source.Sum((BeamEarthworks x) => x.Gravel)));
			b.AppendLine(string.Format(CultureInfo.InvariantCulture, "EMBANKMENT TOTAL: from excavation {0:0.00} | COMMON BORROW {1:0.00} loose m3", source.Sum((BeamEarthworks x) => x.LooseFromExcavation), source.Sum((BeamEarthworks x) => x.LooseImport)));
			b.AppendLine("Embankment quantities are LOOSE material before compaction. Common borrow is the quantity to obtain/deliver; no additional factor.");
			b.AppendLine("Soil balance is per entry; all excavated soil assumed reusable. No transfer between rows. Verify factors with soil report.");
			b.AppendLine("Rectangular trenches; no slopes, dewatering or shared-trench deductions. Count overlapping excavation only once.");
			if (list.Any((EstimateInput i) => i.Earthworks == null))
			{
				b.AppendLine("PARTIAL EARTHWORKS: invalid rows excluded.");
			}
		}

		private void UpdateLoadedDxf()
		{
			sourceLabel.Text = "Loaded DXF (" + sources.Count + "): " + string.Join("; ", sources.Select((ScheduleSource s) => s.Name)) + "\n" + sources.Sum((ScheduleSource s) => s.Catalog.Source.Count) + " beam marks | Source is automatic from the selected beam mark.";
			help.SetToolTip(sourceLabel, string.Join("\n", sources.Select((ScheduleSource s) => s.Name)));
		}

		private void ManageSources()
		{
			if (!Allowed())
			{
				return;
			}
			beams.EndEdit();
			int num = DxfSourceManager.ChooseRemoval(this, sources.Select((ScheduleSource s) => s.Name).ToArray(), sources.Select((ScheduleSource s) => beams.Rows.Cast<DataGridViewRow>().Count((DataGridViewRow r) => !r.IsNewRow && s.Rows.ContainsKey(Cell(r, 1)))).ToArray());
			if (num >= 0)
			{
				RemoveSourceAt(num);
			}
		}

		internal void RemoveSourceAt(int index)
		{
			if (index < 0 || index >= sources.Count)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			ScheduleSource removed = sources[index];
			filling = true;
			try
			{
				foreach (DataGridViewRow item in (from DataGridViewRow r in beams.Rows
					where !r.IsNewRow && removed.Rows.ContainsKey(Cell(r, 1))
					select r).ToList())
				{
					beams.Rows.Remove(item);
				}
				sources.RemoveAt(index);
				schedules.Clear();
				DataGridViewComboBoxColumn dataGridViewComboBoxColumn = (DataGridViewComboBoxColumn)beams.Columns[1];
				dataGridViewComboBoxColumn.Items.Clear();
				dataGridViewComboBoxColumn.Items.Add("");
				DataGridViewComboBoxColumn dataGridViewComboBoxColumn2 = (DataGridViewComboBoxColumn)beams.Columns[13];
				dataGridViewComboBoxColumn2.Items.Clear();
				foreach (ScheduleSource source in sources)
				{
					dataGridViewComboBoxColumn2.Items.Add(source.Label);
					foreach (KeyValuePair<string, BeamRow> row in source.Rows)
					{
						schedules[row.Key] = row.Value;
						dataGridViewComboBoxColumn.Items.Add(row.Key);
					}
				}
				catalog = ((sources.Count == 0) ? null : sources[0].Catalog);
				sourceText = ((sources.Count == 0) ? "" : sources[0].Raw);
				sourceName = string.Join("; ", sources.Select((ScheduleSource s) => s.Name));
				foreach (DataGridViewRow item2 in (IEnumerable)beams.Rows)
				{
					if (!item2.IsNewRow)
					{
						ConfigureMarkCell(item2);
					}
				}
			}
			finally
			{
				filling = false;
			}
			UpdateLoadedDxf();
			Recalculate();
		}

		private static void CheckUniqueMarks(IEnumerable<ScheduleSource> candidates)
		{
			var list = (from g in (from s in candidates
					group s by s.Raw into g
					select g.First()).SelectMany((ScheduleSource s) => s.Rows.Keys.Select((string mark) => new
				{
					Mark = mark,
					Source = s.Name
				})).GroupBy(x => x.Mark, StringComparer.OrdinalIgnoreCase)
				where g.Count() > 1
				select g).Take(10).ToList();
			if (list.Count <= 0)
			{
				return;
			}
			throw new Exception("Duplicate beam marks across DXF files. Use unique marks before loading:\n" + string.Join("\n", list.Select(g => g.Key + ": " + string.Join(" / ", g.Select(x => x.Source)))));
		}

		private void ValidateFiles(List<string[]> files, bool append)
		{
			List<ScheduleSource> list = (append ? sources.ToList() : new List<ScheduleSource>());
			foreach (string[] file in files)
			{
				Analysis analysis = ManualBeamSchedule.Read(file[0], file[1]);
				list.Add(new ScheduleSource
				{
					Name = file[1],
					Raw = file[0],
					Catalog = analysis,
					Rows = analysis.Source.ToDictionary((BeamRow r) => r.Mark, StringComparer.OrdinalIgnoreCase)
				});
			}
			CheckUniqueMarks(list);
		}

		private BeamRow FindSchedule(string label, string mark)
		{
			BeamRow value;
			if (!schedules.TryGetValue(mark, out value))
			{
				return null;
			}
			return value;
		}

		private void ConfigureMarkCell(DataGridViewRow row)
		{
			DataGridViewComboBoxCell obj = (DataGridViewComboBoxCell)row.Cells[1];
			obj.Items.Clear();
			obj.Items.Add("");
			obj.Items.AddRange(schedules.Keys.OrderBy((string k) => k, StringComparer.OrdinalIgnoreCase).Cast<object>().ToArray());
		}

		public int AddEntry(string description = "", string mark = "", string length = "", string quantity = "1", string schedule = "")
		{
			if (!Allowed())
			{
				return -1;
			}
			if (sources.Count == 0)
			{
				throw new Exception("Load a revised schedule DXF first.");
			}
			if (schedule == "")
			{
				schedule = sources[0].Label;
			}
			int num = beams.Rows.Add(description, mark.ToUpperInvariant(), "", "", length, quantity, "", "", "", "", "", "", "", schedule);
			beams.Rows[num].Cells[14].Value = "Elevated slab";
			beams.Rows[num].Cells[15].Value = "0";
			ConfigureMarkCell(beams.Rows[num]);
			beams.CurrentCell = beams.Rows[num].Cells[0];
			Recalculate();
			return num;
		}

		public string[] ScheduleLabels()
		{
			return sources.Select((ScheduleSource s) => s.Label).ToArray();
		}

		private void RemoveSelected()
		{
			if (!Allowed())
			{
				return;
			}
			foreach (DataGridViewRow item in (from r in (from DataGridViewCell c in beams.SelectedCells
					select c.OwningRow).Distinct()
				where !r.IsNewRow
				orderby r.Index descending
				select r).ToList())
			{
				beams.Rows.Remove(item);
			}
			Recalculate();
		}

		private void ApplyBeamContext(DataGridViewRow row)
		{
			bool flag = Cell(row, 14) == "On gravel fill" || Cell(row, 14) == "Ground beam";
			string[] array = new string[4] { "ExcDepth", "Swell", "Shrink", "OtherBuriedConcrete" };
			foreach (string columnName in array)
			{
				row.Cells[columnName].ReadOnly = !flag;
				row.Cells[columnName].Style.BackColor = (flag ? Color.FromArgb(255, 249, 222) : SystemColors.Control);
			}
			if (Cell(row, 20) == "")
			{
				row.Cells[20].Value = 20.0.ToString("0.00", CultureInfo.InvariantCulture);
			}
			row.Cells[19].Value = 0.3.ToString("0.00", CultureInfo.InvariantCulture);
			row.Cells[19].ReadOnly = true;
			row.Cells[19].Style.BackColor = SystemColors.Control;
			row.Cells[15].ReadOnly = true;
			row.Cells[15].Style.BackColor = (flag ? SystemColors.Control : Color.FromArgb(255, 249, 222));
			row.Cells[15].ToolTipText = (flag ? "Full beam depth is used for both side forms." : "Full beam depth is used for both side forms.");
			row.Cells[14].ToolTipText = "Elevated: full sides + bottom. Gravel/ground: full sides only.";
			if (flag)
			{
				row.Cells[15].Value = "0";
			}
		}

		private static string Cell(DataGridViewRow r, int c)
		{
			return Convert.ToString(r.Cells[c].Value).Trim();
		}

		private static double Read(string s, string field)
		{
			double result;
			if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out result) || double.IsNaN(result) || double.IsInfinity(result))
			{
				throw new Exception("Invalid " + field + ". Use a decimal number.");
			}
			return result;
		}

		internal void SetProjectSoilFactors(double swell, double shrink)
		{
			if (projectSwell != swell || projectShrink != shrink)
			{
				projectSwell = swell;
				projectShrink = shrink;
				DataGridViewColumn dataGridViewColumn = beams.Columns[20];
				bool visible = (beams.Columns[21].Visible = false);
				dataGridViewColumn.Visible = visible;
				Recalculate();
			}
		}

		public void Recalculate()
		{
			if (filling)
			{
				return;
			}
			filling = true;
			try
			{
				inputs.Clear();
				EstimateOptions estimateOptions = Options();
				string text = "";
				try
				{
					estimateOptions.Validate();
				}
				catch (Exception ex)
				{
					text = ex.Message;
				}
				foreach (DataGridViewRow item in (IEnumerable)beams.Rows)
				{
					if (item.IsNewRow)
					{
						continue;
					}
					item.Tag = null;
					for (int i = 2; i < 4; i++)
					{
						item.Cells[i].Value = "";
					}
					for (int j = 8; j <= 12; j++)
					{
						item.Cells[j].Value = "";
					}
					string text2 = Cell(item, 0);
					string mark = Cell(item, 1).ToUpperInvariant();
					string text3 = Cell(item, 4);
					if (text2 == "" && mark == "" && text3 == "")
					{
						continue;
					}
					ScheduleSource scheduleSource = sources.FirstOrDefault((ScheduleSource s) => s.Rows.ContainsKey(mark));
					item.Cells[13].Value = ((scheduleSource == null) ? "" : scheduleSource.Label);
					ApplyBeamContext(item);
					if (projectSwell.HasValue)
					{
						item.Cells[20].Value = projectSwell.Value.ToString(CultureInfo.InvariantCulture);
						item.Cells[21].Value = projectShrink.Value.ToString(CultureInfo.InvariantCulture);
						DataGridViewCell dataGridViewCell = item.Cells[20];
						bool readOnly = (item.Cells[21].ReadOnly = true);
						dataGridViewCell.ReadOnly = readOnly;
					}
					EstimateInput estimateInput = new EstimateInput
					{
						Description = text2,
						ScheduleLabel = Cell(item, 13),
						Beam = mark,
						Length = text3,
						Quantity = Cell(item, 5),
						TieCount = "",
						TieCut = "",
						ExcavationDepth = Cell(item, 18),
						WorkingAllowance = 0.3.ToString("0.00", CultureInfo.InvariantCulture),
						Swell = ((Cell(item, 20) == "") ? 20.0.ToString("0.00", CultureInfo.InvariantCulture) : Cell(item, 20)),
						Shrink = ((Cell(item, 21) == "") ? "10" : Cell(item, 21)),
						OtherBuriedConcrete = ((Cell(item, 22) == "") ? "0" : Cell(item, 22)),
						Support = ((Cell(item, 14) == "") ? "Elevated slab" : Cell(item, 14)),
						SlabThickness = "0"
					};
					inputs.Add(estimateInput);
					item.Tag = estimateInput;
					DataGridViewCell dataGridViewCell2 = item.Cells[16];
					object value = (item.Cells[17].Value = "");
					dataGridViewCell2.Value = value;
					try
					{
						BeamRow beamRow = FindSchedule(estimateInput.ScheduleLabel, mark);
						if (beamRow == null)
						{
							throw new Exception("Choose a beam mark");
						}
						estimateInput.Measures = BeamQuantities.Calculate(Read(beamRow.Cell(0), "width"), Read(beamRow.Cell(1), "depth"), Read(text3, "length"), int.Parse(estimateInput.Quantity), estimateInput.Support != "Elevated slab", Read(estimateInput.SlabThickness, "slab thickness"));
						item.Cells[16].Value = estimateInput.Measures.Concrete.ToString("N2");
						item.Cells[17].Value = estimateInput.Measures.Formwork.ToString("N2");
					}
					catch (Exception ex2)
					{
						estimateInput.MeasureError = ex2.Message;
					}
					for (int num = 23; num <= 31; num++)
					{
						item.Cells[num].Value = "";
					}
					if (estimateInput.Support == "On gravel fill" || estimateInput.Support == "Ground beam")
					{
						try
						{
							BeamRow beamRow2 = FindSchedule(estimateInput.ScheduleLabel, mark);
							if (beamRow2 == null)
							{
								throw new Exception("Choose a beam mark");
							}
							double num2 = Read(beamRow2.Cell(1), "D") / 1000.0;
							BeamEarthworks beamEarthworks = (estimateInput.Earthworks = BeamEarthworks.Calculate(Read(beamRow2.Cell(0), "B") / 1000.0, num2, Read(text3, "length"), int.Parse(estimateInput.Quantity), (estimateInput.ExcavationDepth == "") ? (num2 + 0.1) : Read(estimateInput.ExcavationDepth, "excavation depth"), Read(estimateInput.WorkingAllowance, "side allowance"), Read(estimateInput.Swell, "swell"), Read(estimateInput.Shrink, "shrinkage"), Read(estimateInput.OtherBuriedConcrete, "other buried concrete")));
							item.Cells[30].Value = beamEarthworks.LooseFromExcavation.ToString("N2");
							item.Cells[31].Value = beamEarthworks.LooseImport.ToString("N2");
							double[] array = new double[6] { beamEarthworks.Excavation, beamEarthworks.Backfill, beamEarthworks.LooseExcavation, beamEarthworks.LooseRequired, beamEarthworks.LooseSurplus, beamEarthworks.LooseImport };
							for (int num3 = 0; num3 < array.Length; num3++)
							{
								item.Cells[23 + num3].Value = array[num3].ToString("N2");
							}
							item.Cells[29].Value = "Estimated | verify soil factors";
							if (estimateInput.ExcavationDepth == "")
							{
								item.Cells[18].ToolTipText = "Automatic depth now: " + beamEarthworks.Depth.ToString("0.00") + "m (D + 0.10m). Type an override or leave blank.";
							}
						}
						catch (Exception ex3)
						{
							estimateInput.EarthworkError = ex3.Message;
							item.Cells[29].Value = ex3.Message;
						}
					}
					try
					{
						if (text != "")
						{
							throw new Exception(text);
						}
						if (text2 == "")
						{
							throw new Exception("Enter a description");
						}
						BeamRow beamRow3 = FindSchedule(estimateInput.ScheduleLabel, mark);
						if (beamRow3 == null)
						{
							throw new Exception("Choose a beam mark from the revised DXF");
						}
						estimateInput.Source = beamRow3;
						item.Cells[2].Value = beamRow3.Cell(0);
						item.Cells[3].Value = beamRow3.Cell(1);
						if (text3 == "")
						{
							throw new Exception("Enter length in metres");
						}
						int result;
						if (!int.TryParse(estimateInput.Quantity, out result) || result <= 0 || result > 100000)
						{
							throw new Exception("Quantity must be 1 to 100000 whole beams");
						}
						int result2;
						if (estimateInput.TieCount != "" && !int.TryParse(estimateInput.TieCount, out result2))
						{
							throw new Exception("Tie sets must be a whole number");
						}
						if (!(estimateInput.TieCut == ""))
						{
							Read(estimateInput.TieCut, "tie cut m/set");
						}
						estimateInput.Result = EstimatorEngine.Calculate(beamRow3, mark, Read(text3, "beam length"), estimateOptions);
						foreach (EstimateLine line in estimateInput.Result.Lines)
						{
							if (result > 1)
							{
								line.Formula = line.Formula + " PER BEAM x " + result + " beams";
							}
							line.Metres *= result;
							line.Kg *= result;
						}
						estimateInput.Result.Ties = checked(estimateInput.Result.Ties * result);
						item.Cells[8].Value = estimateInput.Result.LongKg.ToString("N2");
						item.Cells[9].Value = estimateInput.Result.TieKg.ToString("N2");
						item.Cells[10].Value = estimateInput.Result.SfrKg.ToString("N2");
						item.Cells[11].Value = (estimateInput.Result.Total * (1.0 + estimateOptions.Waste / 100.0)).ToString("N2");
						item.Cells[12].Value = "Estimated | " + estimateInput.Result.Ties + " tie sets total";
					}
					catch (Exception ex4)
					{
						estimateInput.Result = null;
						estimateInput.Error = ex4.Message;
						item.Cells[12].Value = ex4.Message;
					}
				}
				totals.Rows.Clear();
				List<EstimateLine> source = inputs.Where((EstimateInput estimateInput2) => estimateInput2.Result != null).SelectMany((EstimateInput estimateInput2) => estimateInput2.Result.Lines).ToList();
				foreach (IGrouping<int, EstimateLine> item2 in from l in source
					group l by l.Diameter into g
					orderby g.Key
					select g)
				{
					double num4 = item2.Sum((EstimateLine l) => l.Kg);
					totals.Rows.Add("Ø" + item2.Key, num4.ToString("N2"), (num4 * estimateOptions.Waste / 100.0).ToString("N2"), (num4 * (1.0 + estimateOptions.Waste / 100.0)).ToString("N2"));
				}
				gradeTotals.Rows.Clear();
				int[] array2 = new int[2] { 40, 60 };
				for (int num5 = 0; num5 < array2.Length; num5++)
				{
					int steel = array2[num5];
					List<EstimateLine> source2 = source.Where((EstimateLine l) => l.Grade == steel).ToList();
					foreach (IGrouping<int, EstimateLine> item3 in from l in source2
						group l by l.Diameter into g
						orderby g.Key
						select g)
					{
						double num6 = item3.Sum((EstimateLine l) => l.Kg);
						gradeTotals.Rows.Add("G" + steel, "D" + item3.Key, num6.ToString("N2"), (num6 * estimateOptions.Waste / 100.0).ToString("N2"), (num6 * (1.0 + estimateOptions.Waste / 100.0)).ToString("N2"));
					}
					double num7 = source2.Sum((EstimateLine l) => l.Kg);
					int index = gradeTotals.Rows.Add("G" + steel + " TOTAL", "", num7.ToString("N2"), (num7 * estimateOptions.Waste / 100.0).ToString("N2"), (num7 * (1.0 + estimateOptions.Waste / 100.0)).ToString("N2"));
					gradeTotals.Rows[index].DefaultCellStyle.BackColor = Color.FromArgb(220, 233, 247);
				}
				int num8 = inputs.Count((EstimateInput estimateInput2) => estimateInput2.Result != null);
				int num9 = inputs.Where((EstimateInput estimateInput2) => estimateInput2.Result != null).Sum((EstimateInput estimateInput2) => int.Parse(estimateInput2.Quantity));
				status.Text = num8 + " / " + inputs.Count + " entries estimated | " + num9 + " beams | " + ((num8 < inputs.Count) ? "PARTIAL TOTAL: " : "TOTAL: ") + (source.Sum((EstimateLine l) => l.Kg) * (1.0 + estimateOptions.Waste / 100.0)).ToString("N2") + " kg";
				UpdatePurchaseTotals();
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
			if (excelLink != null)
			{
				excelTimer.Stop();
				excelTimer.Start();
			}
		}

		private void ShowSelected()
		{
			if (details == null || beams.CurrentRow == null || beams.CurrentRow.IsNewRow)
			{
				return;
			}
			DataGridViewRow currentRow = beams.CurrentRow;
			string text = Cell(currentRow, 0);
			string text2 = Cell(currentRow, 1).ToUpperInvariant();
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(text + " | " + text2 + " | L=" + Cell(currentRow, 4) + "m | Qty=" + Cell(currentRow, 5));
			BeamRow beamRow = FindSchedule(Cell(currentRow, 13), text2);
			if (beamRow != null)
			{
				stringBuilder.AppendLine("FLOOR / SOURCE: " + Cell(currentRow, 13));
				stringBuilder.AppendLine("REVISED SCHEDULE: " + beamRow.Cell(0) + " x " + beamRow.Cell(1) + "mm");
				for (int i = 0; i < 3; i++)
				{
					stringBuilder.AppendLine((new string[3] { "LEFT   ", "MIDSPAN", "RIGHT  " })[i] + " bottom: " + string.Join(" + ", beamRow.Cells[2 + i]) + " | top: " + string.Join(" + ", beamRow.Cells[5 + i]));
				}
				stringBuilder.AppendLine("Stirrups: " + beamRow.Leg + " / " + beamRow.Diameter + " / " + beamRow.RevisedSpacing);
				stringBuilder.AppendLine("SFR: " + beamRow.Cell(11));
			}
			stringBuilder.AppendLine("STATUS: " + Cell(currentRow, 12));
			EstimateInput estimateInput = currentRow.Tag as EstimateInput;
			if (estimateInput != null && estimateInput.Result != null)
			{
				foreach (EstimateLine line in estimateInput.Result.Lines)
				{
					stringBuilder.AppendLine(EstimatorEngine.DisplayPart(line.Part) + " Ø" + line.Diameter + ": " + line.Formula + " = " + line.Kg.ToString("0.00") + "kg");
				}
				foreach (string note in estimateInput.Result.Notes)
				{
					stringBuilder.AppendLine("NOTE: " + note);
				}
			}
			if (estimateInput != null && estimateInput.Earthworks != null)
			{
				estimateInput.Earthworks.Report(stringBuilder, true);
			}
			else if (estimateInput != null && estimateInput.EarthworkError != "")
			{
				stringBuilder.AppendLine("EARTHWORK ERROR: " + estimateInput.EarthworkError);
			}
			details.Text = stringBuilder.ToString().Replace("%%C", "Ø").Replace("%%c", "Ø");
		}

		private string State()
		{
			return reportMode.SelectedIndex + "|" + CommercialBars.FormatLengths(stockLengths) + "|" + string.Join("\n", sources.Select((ScheduleSource s) => s.Label + s.Raw)) + Options().Description() + ((lengthProfile == null) ? "" : ProfileState()) + string.Join("\n", inputs.Select((EstimateInput i) => i.ScheduleLabel + "|" + i.Description + "|" + i.Beam + "|" + i.Length + "|" + i.Quantity + "|" + i.TieCount + "|" + i.TieCut + "|" + i.Support + "|" + i.SlabThickness + "|" + i.ExcavationDepth + "|" + i.WorkingAllowance + "|" + i.Swell + "|" + i.Shrink + "|" + i.OtherBuriedConcrete));
		}

		private void UpdatePurchaseTotals()
		{
			List<StockAllocation> source = CommercialBars.Allocate(inputs, lengthProfile, stockLengths);
			foreach (DataGridViewRow item in (IEnumerable)beams.Rows)
			{
				EstimateInput i = item.Tag as EstimateInput;
				if (i == null || i.Result == null)
				{
					continue;
				}
				List<StockAllocation> source2 = source.Where((StockAllocation a) => a.Entry == i).ToList();
				if (source2.Any((StockAllocation a) => a.Choice == null))
				{
					for (int num = 8; num <= 11; num++)
					{
						item.Cells[num].Value = "REVIEW";
					}
					item.Cells[12].Value = "Unresolved commercial cut; purchase excluded";
					continue;
				}
				item.Cells[8].Value = source2.Where((StockAllocation a) => a.Line.Part != "Stirrups" && a.Line.Part != "Side-face").Sum((StockAllocation a) => a.Purchase).ToString("N2");
				item.Cells[9].Value = source2.Where((StockAllocation a) => a.Line.Part == "Stirrups").Sum((StockAllocation a) => a.Purchase).ToString("N2");
				item.Cells[10].Value = source2.Where((StockAllocation a) => a.Line.Part == "Side-face").Sum((StockAllocation a) => a.Purchase).ToString("N2");
				item.Cells[11].Value = source2.Sum((StockAllocation a) => a.Purchase).ToString("N2");
			}
			beams.Columns[8].HeaderText = "MAIN BUY kg";
			beams.Columns[9].HeaderText = "TIES BUY kg";
			beams.Columns[10].HeaderText = "SFR BUY kg";
			beams.Columns[11].HeaderText = "TOTAL BUY kg";
			totals.Columns[2].HeaderText = "LAP+OFFCUT kg";
			totals.Columns[3].HeaderText = "BUY kg";
			gradeTotals.Columns[3].HeaderText = "LAP+OFFCUT kg";
			gradeTotals.Columns[4].HeaderText = "BUY kg";
			totals.Rows.Clear();
			gradeTotals.Rows.Clear();
			foreach (IGrouping<int, StockAllocation> item2 in from a in source
				group a by a.Demand.Diameter into g
				orderby g.Key
				select g)
			{
				totals.Rows.Add("D" + item2.Key, item2.Sum((StockAllocation a) => a.Net).ToString("N2"), item2.Sum((StockAllocation a) => a.Splice + a.Waste).ToString("N2"), item2.Sum((StockAllocation a) => a.Purchase).ToString("N2"));
			}
			int[] array = new int[2] { 40, 60 };
			for (int num2 = 0; num2 < array.Length; num2++)
			{
				int grade = array[num2];
				List<StockAllocation> source3 = source.Where((StockAllocation a) => a.Demand.Grade == grade).ToList();
				foreach (IGrouping<int, StockAllocation> item3 in from a in source3
					group a by a.Demand.Diameter into g
					orderby g.Key
					select g)
				{
					gradeTotals.Rows.Add("G" + grade, "D" + item3.Key, item3.Sum((StockAllocation a) => a.Net).ToString("N2"), item3.Sum((StockAllocation a) => a.Splice + a.Waste).ToString("N2"), item3.Sum((StockAllocation a) => a.Purchase).ToString("N2"));
				}
				gradeTotals.Rows.Add("G" + grade + " TOTAL", "", source3.Sum((StockAllocation a) => a.Net).ToString("N2"), source3.Sum((StockAllocation a) => a.Splice + a.Waste).ToString("N2"), source3.Sum((StockAllocation a) => a.Purchase).ToString("N2"));
			}
			status.Text = ((source.Any((StockAllocation a) => a.Choice == null) || inputs.Any((EstimateInput estimateInput) => estimateInput.Result == null)) ? "PARTIAL / REVIEW | " : "") + "TOTAL TO BUY: " + source.Sum((StockAllocation a) => a.Purchase).ToString("N2") + " kg | shared stock/offcuts allocated by used length";
		}

		public string Report()
		{
			beams.EndEdit();
			Recalculate();
			List<StockAllocation> list = CommercialBars.Allocate(inputs, lengthProfile, stockLengths);
			StringBuilder stringBuilder = new StringBuilder();
			int selectedIndex = reportMode.SelectedIndex;
			stringBuilder.AppendLine("REINFORCING STEEL BARS | PURCHASE BACKUP");
			stringBuilder.AppendLine(new string('-', 92));
			stringBuilder.AppendLine("Report: " + reportMode.Text + " | " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
			stringBuilder.AppendLine("Available commercial lengths: " + CommercialBars.FormatLengths(stockLengths) + " m");
			if (selectedIndex == 2)
			{
				stringBuilder.AppendLine("Nominal masses: project-supplied table, kg/m.");
				stringBuilder.AppendLine("Compatible different-length cuts share stock and offcuts; grades/diameters stay separate. Kerf=0.");
			}
			if (list.Any((StockAllocation a) => a.Choice == null) || inputs.Any((EstimateInput estimateInput) => estimateInput.Result == null))
			{
				stringBuilder.AppendLine("PARTIAL: unresolved cuts / invalid entries require review; excluded from purchase.");
			}
			stringBuilder.AppendLine();
			AppendConcreteReport(stringBuilder);
			AppendEarthworksReport(stringBuilder, selectedIndex > 0);
			stringBuilder.AppendLine();
			AppendPurchaseSummary(stringBuilder, list);
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("02  BACKUP QUANTITIES BY DESCRIPTION");
			stringBuilder.AppendLine(new string('-', 92));
			for (int num = 0; num < inputs.Count; num++)
			{
				EstimateInput i = inputs[num];
				List<StockAllocation> list2 = list.Where((StockAllocation a) => a.Entry == i).ToList();
				stringBuilder.AppendLine((num + 1).ToString("00") + "  " + i.Description + " | " + i.Beam + " | B=" + ((i.Source == null) ? "?" : i.Source.Cell(0)) + " D=" + ((i.Source == null) ? "?" : i.Source.Cell(1)) + " mm | L=" + i.Length + "m | Qty=" + i.Quantity);
				if (i.Result == null)
				{
					stringBuilder.AppendLine("   NOT ESTIMATED: " + i.Error);
					continue;
				}
				if (selectedIndex == 0)
				{
					stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "   BUY: Main {0:0.00} | Ties {1:0.00} | SFR {2:0.00} | TOTAL {3:0.00} kg", list2.Where((StockAllocation a) => a.Line.Part != "Stirrups" && a.Line.Part != "Side-face").Sum((StockAllocation a) => a.Purchase), list2.Where((StockAllocation a) => a.Line.Part == "Stirrups").Sum((StockAllocation a) => a.Purchase), list2.Where((StockAllocation a) => a.Line.Part == "Side-face").Sum((StockAllocation a) => a.Purchase), list2.Sum((StockAllocation a) => a.Purchase)));
				}
				if (list2.Any((StockAllocation a) => a.Choice == null))
				{
					stringBuilder.AppendLine("   PARTIAL: unresolved cut demand.");
				}
				if (selectedIndex > 0)
				{
					stringBuilder.AppendLine();
					PurchaseReports.Entry(stringBuilder, list2, int.Parse(i.Quantity), selectedIndex == 2);
				}
				stringBuilder.AppendLine();
			}
			stringBuilder.AppendLine(new string('-', 92));
			stringBuilder.AppendLine("TOTAL REBAR TO PURCHASE: " + list.Sum((StockAllocation a) => a.Purchase).ToString("0.00", CultureInfo.InvariantCulture) + " kg");
			PurchaseReports.Basis(stringBuilder);
			if (selectedIndex == 2)
			{
				stringBuilder.AppendLine();
				stringBuilder.AppendLine(CommercialBars.Report(inputs, lengthProfile, stockLengths));
			}
			return stringBuilder.ToString();
		}

		private void AppendConcreteReport(StringBuilder b)
		{
			b.AppendLine("CONCRETE & FORMWORK SUMMARY");
			b.AppendLine("Side forms = 2 x full beam depth x length x quantity. Elevated beams add soffit; ground/gravel beams have no soffit forms. No slab thickness deduction.");
			b.AppendLine(string.Format("{0,-24}{1,-16}{2,8}{3,15}{4,15}", "DESCRIPTION / MARK", "SUPPORT", "QTY", "CONCRETE m3", "FORMWORK m2"));
			foreach (EstimateInput input in inputs)
			{
				if (input.Measures == null)
				{
					b.AppendLine(input.Description + " / " + input.Beam + ": NOT MEASURED - " + input.MeasureError);
				}
				else
				{
					b.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,-24}{1,-16}{2,8}{3,15:0.00}{4,15:0.00}", ((input.Description + " / " + input.Beam).Length > 23) ? (input.Description + " / " + input.Beam).Substring(0, 23) : (input.Description + " / " + input.Beam), input.Support, input.Quantity, input.Measures.Concrete, input.Measures.Formwork));
				}
			}
			b.AppendLine(string.Format(CultureInfo.InvariantCulture, "TOTAL CONCRETE: {0:0.00} m3 | TOTAL FORMWORK: {1:0.00} m2", inputs.Where((EstimateInput i) => i.Measures != null).Sum((EstimateInput i) => i.Measures.Concrete), inputs.Where((EstimateInput i) => i.Measures != null).Sum((EstimateInput i) => i.Measures.Formwork)));
			if (inputs.Any((EstimateInput i) => i.Measures == null))
			{
				b.AppendLine("PARTIAL concrete/formwork takeoff: invalid geometry excluded.");
			}
			b.AppendLine();
		}

		public object[,] ConcreteExcelRows()
		{
			List<EstimateInput> list = inputs.Where((EstimateInput i) => i.Measures != null).ToList();
			object[,] array = new object[list.Count, 8];
			for (int num = 0; num < list.Count; num++)
			{
				EstimateInput estimateInput = list[num];
				array[num, 0] = estimateInput.Description;
				array[num, 1] = estimateInput.Beam;
				array[num, 2] = estimateInput.Support;
				array[num, 3] = int.Parse(estimateInput.Quantity);
				array[num, 4] = estimateInput.Measures.Concrete;
				array[num, 5] = estimateInput.Measures.SideForms;
				array[num, 6] = estimateInput.Measures.BottomForms;
				array[num, 7] = estimateInput.Measures.Formwork;
			}
			return array;
		}

		private static string PurchasePart(StockAllocation a)
		{
			if (a.Line.Part == "Side-face")
			{
				return "SFR / EACH FACE";
			}
			if (a.Line.Part == "Stirrups")
			{
				if (!a.Line.TieOverride)
				{
					if (!(Math.Abs(a.Demand.Cut - a.Line.MainHoopCut) < 1E-09))
					{
						return "ADDITIONAL LEG (90+130)";
					}
					return "MAIN STIRRUP / HOOP";
				}
				return "STIRRUP MANUAL SET";
			}
			return EstimatorEngine.DisplayPart(a.Line.Part);
		}

		internal static void AppendPurchaseSummary(StringBuilder b, List<StockAllocation> plan)
		{
			PurchaseReports.Summary(b, plan);
		}

		private static void AppendTieBreakdown(StringBuilder b, EstimateInput input, int quantity)
		{
			b.AppendLine("STIRRUP CUT LENGTHS & QUANTITIES");
			b.AppendLine(string.Format("{0,-30}{1,7}{2,10}{3,13}{4,14}", "TYPE", "DIA", "PCS/BEAM", "CUT m/pc", "TOTAL m"));
			foreach (EstimateLine line in input.Result.Lines.Where((EstimateLine l) => l.Part == "Stirrups"))
			{
				int num = ((line.TieSetsPerBeam > 0) ? line.TieSetsPerBeam : (input.Result.Ties / quantity));
				Action<string, double, int> action = delegate(string name, double cut, int pcs)
				{
					double num2 = cut * (double)pcs * (double)quantity;
					b.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,-30}{1,7}{2,10}{3,13:0.00}{4,14:0.00}", name, line.Diameter, pcs, cut, num2));
				};
				if (line.TieOverride)
				{
					action("Manual combined set", line.CutLength, num);
					b.AppendLine("Manual cut override: component split is not available.");
				}
				else
				{
					action("Main stirrup / hoop", line.MainHoopCut, num);
					action("Additional leg (90+130)", (line.AdditionalLegs == 0) ? 0.0 : line.AdditionalLegCut, num * line.AdditionalLegs);
				}
			}
		}

		private void ExportPdf()
		{
			if (!Allowed())
			{
				return;
			}
			beams.EndEdit();
			Recalculate();
			if (!inputs.Any((EstimateInput i) => i.Result != null))
			{
				throw new Exception("Add at least one valid estimate entry.");
			}
			using (SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				Filter = "Printable PDF (*.pdf)|*.pdf",
				FileName = "Beam-Estimate-Backup.pdf"
			})
			{
				if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
				{
					EstimatePdf.Write(saveFileDialog.FileName, Report());
					MessageBox.Show("Saved the selected RSB purchase report.\n" + saveFileDialog.FileName);
				}
			}
		}

		private string EstimateXml()
		{
			if (catalog == null)
			{
				throw new Exception("Load a revised DXF first.");
			}
			XmlDocument xmlDocument = new XmlDocument();
			XmlElement xmlElement = xmlDocument.CreateElement("BeamEstimate");
			xmlElement.SetAttribute("version", "6");
			xmlElement.SetAttribute("reportMode", reportMode.SelectedIndex.ToString());
			xmlElement.SetAttribute("stockLengths", CommercialBars.FormatLengths(stockLengths));
			xmlElement.SetAttribute("sourceName", sourceName);
			xmlDocument.AppendChild(xmlElement);
			XmlElement xmlElement2 = xmlDocument.CreateElement("ScheduleSources");
			xmlElement.AppendChild(xmlElement2);
			foreach (ScheduleSource source in sources)
			{
				XmlElement xmlElement3 = xmlDocument.CreateElement("Source");
				xmlElement3.SetAttribute("label", source.Label);
				xmlElement3.SetAttribute("name", source.Name);
				xmlElement3.InnerText = source.Raw;
				xmlElement2.AppendChild(xmlElement3);
			}
			XmlElement xmlElement4 = xmlDocument.CreateElement("Settings");
			EstimateOptions estimateOptions = Options();
			double[] array = new double[13]
			{
				estimateOptions.GradeSmall, estimateOptions.Grade, estimateOptions.Fc, estimateOptions.MainHook, estimateOptions.Anchorage, estimateOptions.BondRatio, estimateOptions.Left, estimateOptions.Right, estimateOptions.Cover, estimateOptions.Hook,
				estimateOptions.Extra, estimateOptions.Waste, estimateOptions.Offset
			};
			string[] array2 = new string[13]
			{
				"gradeSmall", "grade", "fc", "mainHook", "anchorage", "bond", "left", "right", "cover", "hook",
				"extra", "waste", "offset"
			};
			for (int i = 0; i < array2.Length; i++)
			{
				xmlElement4.SetAttribute(array2[i], array[i].ToString("R", CultureInfo.InvariantCulture));
			}
			xmlElement.AppendChild(xmlElement4);
			if (lengthProfile != null)
			{
				XmlElement xmlElement5 = xmlDocument.CreateElement("LengthConfiguration");
				lengthProfile.Write(xmlElement5);
				xmlElement.AppendChild(xmlElement5);
			}
			foreach (EstimateInput input in inputs)
			{
				XmlElement xmlElement6 = xmlDocument.CreateElement("Entry");
				xmlElement6.SetAttribute("schedule", input.ScheduleLabel);
				xmlElement6.SetAttribute("description", input.Description);
				xmlElement6.SetAttribute("mark", input.Beam);
				xmlElement6.SetAttribute("length", input.Length);
				xmlElement6.SetAttribute("quantity", input.Quantity);
				xmlElement6.SetAttribute("tieCount", input.TieCount);
				xmlElement6.SetAttribute("tieCut", input.TieCut);
				xmlElement6.SetAttribute("support", input.Support);
				xmlElement6.SetAttribute("slabThickness", input.SlabThickness);
				xmlElement6.SetAttribute("excavationDepth", input.ExcavationDepth);
				xmlElement6.SetAttribute("workingAllowance", input.WorkingAllowance);
				xmlElement6.SetAttribute("swell", input.Swell);
				xmlElement6.SetAttribute("shrink", input.Shrink);
				xmlElement6.SetAttribute("otherBuriedConcrete", input.OtherBuriedConcrete);
				xmlElement.AppendChild(xmlElement6);
			}
			return xmlDocument.OuterXml;
		}

		internal void HideWorksExcelStatus()
		{
			if (excelStatus != null)
			{
				excelStatus.Visible = false;
			}
		}

		internal List<StockAllocation> WorksPlan()
		{
			return CommercialBars.Allocate(inputs, lengthProfile, stockLengths);
		}

		internal object[,] WorksExcelRows(List<StockAllocation> shared = null)
		{
			if (inputs.Any((EstimateInput i) => i.Result == null || i.Measures == null || (i.Support != "Elevated slab" && i.Earthworks == null)))
			{
				throw new Exception("Resolve invalid beam entries before syncing Program of Works.");
			}
			return ExcelRows(shared);
		}

		internal string WorksSnapshot()
		{
			beams.EndEdit();
			Recalculate();
			if (catalog != null)
			{
				return EstimateXml();
			}
			return null;
		}

		internal void WorksRestore(string xml)
		{
			OpenEstimateXml(xml);
		}

		internal void WorksSaved()
		{
			savedState = State();
		}

		public void SaveTo(string path)
		{
			beams.EndEdit();
			Recalculate();
			XmlDocument xmlDocument = new XmlDocument
			{
				XmlResolver = null
			};
			xmlDocument.LoadXml(EstimateXml());
			string text = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
			try
			{
				xmlDocument.Save(text);
				File.Copy(text, path, true);
			}
			finally
			{
				if (File.Exists(text))
				{
					File.Delete(text);
				}
			}
			savedState = State();
		}

		public void OpenFrom(string path)
		{
			OpenEstimateXml(File.ReadAllText(path));
		}

		private void OpenEstimateXml(string xml)
		{
			XmlDocument xmlDocument = new XmlDocument
			{
				XmlResolver = null
			};
			using (StringReader input = new StringReader(xml))
			{
				using (XmlReader reader = XmlReader.Create(input, new XmlReaderSettings
				{
					DtdProcessing = DtdProcessing.Prohibit,
					XmlResolver = null,
					MaxCharactersInDocument = 100000000L
				}))
				{
					xmlDocument.Load(reader);
				}
			}
			XmlElement documentElement = xmlDocument.DocumentElement;
			if (documentElement == null || documentElement.Name != "BeamEstimate" || !new string[4] { "3", "4", "5", "6" }.Contains(documentElement.GetAttribute("version")))
			{
				throw new Exception("Use a revised-DXF estimate project from v1.4.1 or later.");
			}
			List<ScheduleSource> list = new List<ScheduleSource>();
			if (documentElement["ScheduleSources"] != null)
			{
				foreach (XmlElement item in documentElement.SelectNodes("ScheduleSources/Source"))
				{
					Analysis analysis = ManualBeamSchedule.Read(item.InnerText, item.GetAttribute("name"));
					list.Add(new ScheduleSource
					{
						Label = item.GetAttribute("label"),
						Name = item.GetAttribute("name"),
						Raw = item.InnerText,
						Catalog = analysis,
						Rows = analysis.Source.ToDictionary((BeamRow r) => r.Mark, StringComparer.OrdinalIgnoreCase)
					});
				}
			}
			else
			{
				string innerText = documentElement["ScheduleDxf"].InnerText;
				string attribute = documentElement.GetAttribute("sourceName");
				Analysis analysis2 = ManualBeamSchedule.Read(innerText, attribute);
				list.Add(new ScheduleSource
				{
					Label = analysis2.Floor + " | " + attribute,
					Name = attribute,
					Raw = innerText,
					Catalog = analysis2,
					Rows = analysis2.Source.ToDictionary((BeamRow r) => r.Mark, StringComparer.OrdinalIgnoreCase)
				});
			}
			if (list.Count == 0 || list.Select((ScheduleSource s) => s.Label).Distinct().Count() != list.Count)
			{
				throw new Exception("Invalid or duplicate floor/source identifiers.");
			}
			CheckUniqueMarks(list);
			foreach (XmlElement item2 in documentElement.SelectNodes("Entry"))
			{
				string label = (item2.HasAttribute("schedule") ? item2.GetAttribute("schedule") : list[0].Label);
				ScheduleSource scheduleSource = list.FirstOrDefault((ScheduleSource s) => s.Label == label);
				if (scheduleSource == null || (item2.GetAttribute("mark") != "" && !scheduleSource.Rows.ContainsKey(item2.GetAttribute("mark"))))
				{
					throw new Exception("Project entry has a missing floor/source or beam mark.");
				}
			}
			stockLengths = CommercialBars.ParseLengths(documentElement.GetAttribute("stockLengths"));
			reportMode.SelectedIndex = (documentElement.HasAttribute("reportMode") ? Math.Max(0, Math.Min(2, int.Parse(documentElement.GetAttribute("reportMode")))) : 0);
			LoadCatalog(list[0].Raw, list[0].Name);
			sources[0].Label = list[0].Label;
			((DataGridViewComboBoxColumn)beams.Columns[13]).Items[0] = sources[0].Label;
			foreach (ScheduleSource item3 in list.Skip(1))
			{
				AppendCatalog(item3.Raw, item3.Name);
				sources[sources.Count - 1].Label = item3.Label;
				((DataGridViewComboBoxColumn)beams.Columns[13]).Items[sources.Count - 1] = item3.Label;
			}
			ApplyLengthConfiguration((documentElement["LengthConfiguration"] == null) ? null : LengthConfiguration.Read(documentElement["LengthConfiguration"]));
			filling = true;
			try
			{
				XmlElement xmlElement3 = documentElement["Settings"];
				if (documentElement["LengthConfiguration"] == null)
				{
					gradeSmall.SelectedIndex = ((xmlElement3.GetAttribute("gradeSmall") == "60") ? 1 : 0);
					grade.SelectedIndex = ((xmlElement3.GetAttribute("grade") == "40") ? 1 : 0);
					SetConcrete(Read(xmlElement3.GetAttribute("fc"), "fc"));
					lengthProfile = CalculationStore.Resolve((double)concrete.SelectedItem, ((SteelSelection)gradeSmall.SelectedItem).Grade, ((SteelSelection)gradeSmall.SelectedItem).Fy, ((SteelSelection)grade.SelectedItem).Grade, ((SteelSelection)grade.SelectedItem).Fy);
				}
				mainHook.SelectedIndex = ((xmlElement3.GetAttribute("mainHook") == "180") ? 2 : ((xmlElement3.GetAttribute("mainHook") == "90") ? 1 : 0));
				anchorage.SelectedIndex = (int)Read(xmlElement3.GetAttribute("anchorage"), "anchorage");
				NumericUpDown[] array = new NumericUpDown[5] { bond, cover, hook, extra, waste };
				string[] array2 = new string[5] { "bond", "cover", "hook", "extra", "waste" };
				for (int num = 0; num < array2.Length; num++)
				{
					array[num].Value = (decimal)Read(xmlElement3.GetAttribute(array2[num]), array2[num]) * (decimal)((!(array2[num] == "left") && !(array2[num] == "right")) ? 1 : 100);
				}
				CheckUniqueMarks(list);
				foreach (XmlElement item4 in documentElement.SelectNodes("Entry"))
				{
					string text = (item4.HasAttribute("schedule") ? item4.GetAttribute("schedule") : sources[0].Label);
					int index = beams.Rows.Add(item4.GetAttribute("description"), item4.GetAttribute("mark"), "", "", item4.GetAttribute("length"), item4.GetAttribute("quantity"), item4.GetAttribute("tieCount"), item4.GetAttribute("tieCut"), "", "", "", "", "", text);
					beams.Rows[index].Cells[14].Value = (item4.HasAttribute("support") ? item4.GetAttribute("support") : "Elevated slab");
					beams.Rows[index].Cells[15].Value = (item4.HasAttribute("slabThickness") ? item4.GetAttribute("slabThickness") : "0");
					string[][] array3 = new string[5][]
					{
						new string[2] { "18", "excavationDepth" },
						new string[2] { "19", "workingAllowance" },
						new string[2] { "20", "swell" },
						new string[2] { "21", "shrink" },
						new string[2] { "22", "otherBuriedConcrete" }
					};
					foreach (string[] array4 in array3)
					{
						beams.Rows[index].Cells[int.Parse(array4[0])].Value = item4.GetAttribute(array4[1]);
					}
					ConfigureMarkCell(beams.Rows[index]);
				}
			}
			finally
			{
				filling = false;
			}
			UpdateConfigurationLabel();
			Recalculate();
			savedState = State();
		}

		private string ProfileState()
		{
			XmlElement xmlElement = new XmlDocument().CreateElement("Configuration");
			lengthProfile.Write(xmlElement);
			return xmlElement.OuterXml;
		}

		internal void WriteRCDefaults(XmlElement root)
		{
			XmlElement xmlElement = root.OwnerDocument.CreateElement("BeamDefaults");
			root.AppendChild(xmlElement);
			xmlElement.SetAttribute("stock", CommercialBars.FormatLengths(stockLengths));
			XmlElement xmlElement2 = root.OwnerDocument.CreateElement("Configuration");
			xmlElement.AppendChild(xmlElement2);
			lengthProfile.Write(xmlElement2);
		}

		internal void ReadRCDefaults(XmlElement e)
		{
			if (e != null)
			{
				stockLengths = CommercialBars.ParseLengths(e.GetAttribute("stock"));
				ApplyLengthConfiguration(LengthConfiguration.Read(e["Configuration"]));
			}
		}

		internal void SetWorksStockLengths(List<double> lengths)
		{
			stockLengths = CommercialBars.ParseLengths(CommercialBars.FormatLengths(lengths));
			Recalculate();
		}

		private void InitializeMaterials()
		{
			waste.Value = 0m;
			waste.Parent.Visible = false;
			filling = true;
			try
			{
				gradeSmall.Items.Clear();
				grade.Items.Clear();
				concrete.Items.Clear();
				AddSteel(gradeSmall, 40, 275.0);
				AddSteel(gradeSmall, 60, 420.0);
				AddSteel(grade, 60, 420.0);
				AddSteel(grade, 40, 275.0);
				int[] array = new int[3] { 21, 28, 35 };
				foreach (int num in array)
				{
					concrete.Items.Add((double)num);
				}
				foreach (LengthConfiguration item in CalculationStore.All())
				{
					AddSteel(gradeSmall, item.GradeSmall, item.FySmall);
					AddSteel(grade, item.GradeLarge, item.FyLarge);
					if (!concrete.Items.Contains(item.Input.Fc))
					{
						concrete.Items.Add(item.Input.Fc);
					}
				}
				gradeSmall.SelectedIndex = 0;
				grade.SelectedIndex = 0;
				concrete.SelectedIndex = 0;
				bond.Enabled = false;
				hook.Enabled = false;
			}
			finally
			{
				filling = false;
			}
		}

		private static int AddSteel(ComboBox box, int grade, double fy)
		{
			foreach (SteelSelection item in box.Items)
			{
				if (item.Grade == grade && item.Fy == fy)
				{
					return box.Items.IndexOf(item);
				}
			}
			return box.Items.Add(new SteelSelection
			{
				Grade = grade,
				Fy = fy
			});
		}

		private void SetConcrete(double fc)
		{
			if (!concrete.Items.Contains(fc))
			{
				concrete.Items.Add(fc);
			}
			concrete.SelectedItem = fc;
		}

		private void AutoConfiguration()
		{
			SteelSelection steelSelection = (SteelSelection)gradeSmall.SelectedItem;
			SteelSelection steelSelection2 = (SteelSelection)grade.SelectedItem;
			lengthProfile = CalculationStore.Resolve((double)concrete.SelectedItem, steelSelection.Grade, steelSelection.Fy, steelSelection2.Grade, steelSelection2.Fy);
			UpdateConfigurationLabel();
			Recalculate();
		}

		private void StoredConfigurationChanged()
		{
			if (base.IsDisposed || profileSnapshot)
			{
				return;
			}
			double num = (double)concrete.SelectedItem;
			SteelSelection steelSelection = (SteelSelection)gradeSmall.SelectedItem;
			SteelSelection steelSelection2 = (SteelSelection)grade.SelectedItem;
			filling = true;
			try
			{
				foreach (LengthConfiguration item in CalculationStore.All())
				{
					AddSteel(gradeSmall, item.GradeSmall, item.FySmall);
					AddSteel(grade, item.GradeLarge, item.FyLarge);
					if (!concrete.Items.Contains(item.Input.Fc))
					{
						concrete.Items.Add(item.Input.Fc);
					}
				}
				gradeSmall.SelectedIndex = AddSteel(gradeSmall, steelSelection.Grade, steelSelection.Fy);
				grade.SelectedIndex = AddSteel(grade, steelSelection2.Grade, steelSelection2.Fy);
				SetConcrete(num);
			}
			finally
			{
				filling = false;
			}
			AutoConfiguration();
		}

		private void UpdateConfigurationLabel()
		{
			if (lengthProfile != null)
			{
				calculationLabel.Text = (profileSnapshot ? "Saved estimate calculation: " : "Automatic calculation: ") + lengthProfile.Name + " | " + (lengthProfile.Input.Simplified ? "Simplified" : "General bond ratio") + " | Round UP " + lengthProfile.Input.Round + "mm\nB and D come from the revised DXF. Concrete / steel selections choose the saved calculation values.";
				Text = "Beam Estimator v1.4.37 | " + lengthProfile.Name;
			}
		}

		public void ApplyLengthConfiguration(LengthConfiguration c)
		{
			if (c == null)
			{
				profileSnapshot = false;
				AutoConfiguration();
				return;
			}
			c.Validate();
			filling = true;
			try
			{
				lengthProfile = CalculationStore.Clone(c);
				profileSnapshot = true;
				gradeSmall.SelectedIndex = AddSteel(gradeSmall, c.GradeSmall, c.FySmall);
				grade.SelectedIndex = AddSteel(grade, c.GradeLarge, c.FyLarge);
				SetConcrete(c.Input.Fc);
				if (mainHook.SelectedIndex != 0 && anchorage.SelectedIndex == 1)
				{
					anchorage.SelectedIndex = 3;
				}
			}
			finally
			{
				filling = false;
			}
			UpdateConfigurationLabel();
			Recalculate();
		}

		private void ShowCurtailment()
		{
			using (Form form = new Form
			{
				Text = "Curtailment Settings - Fixed",
				ClientSize = new Size(660, 430),
				FormBorderStyle = FormBorderStyle.FixedDialog,
				MaximizeBox = false,
				MinimizeBox = false,
				StartPosition = FormStartPosition.CenterParent,
				Font = new Font("Segoe UI", 10f),
				BackColor = Color.FromArgb(242, 245, 249)
			})
			{
				GroupBox groupBox = new GroupBox
				{
					Text = "Top steel curtailment location",
					Location = new Point(18, 16),
					Size = new Size(300, 105)
				};
				groupBox.Controls.Add(new Label
				{
					Text = "End support                         L / 4\n\nContinuous support              L / 4",
					Dock = DockStyle.Fill,
					Padding = new Padding(12, 12, 0, 0)
				});
				GroupBox groupBox2 = new GroupBox
				{
					Text = "Bottom steel curtailment location",
					Location = new Point(18, 130),
					Size = new Size(300, 145)
				};
				groupBox2.Controls.Add(new Label
				{
					Text = "Midspan cutoff                     L / 6\n\nEnd support                         L / 4\n\nContinuous support              L / 4",
					Dock = DockStyle.Fill,
					Padding = new Padding(12, 12, 0, 0)
				});
				GroupBox groupBox3 = new GroupBox
				{
					Text = "Shear zone presets (reference)",
					Location = new Point(334, 16),
					Size = new Size(307, 259)
				};
				groupBox3.Controls.Add(new Label
				{
					Text = "Ductile beam\nDepth factor:  D x 2\n\nNon-ductile beam\nLength factor:  L / 3\n\nTie quantities follow the actual\nspacing/count pattern in revised DXF.",
					Dock = DockStyle.Fill,
					Padding = new Padding(12, 12, 0, 0)
				});
				Label label = new Label
				{
					Text = "Fixed from supplied curtailment reference. Bottom midspan bars start L/6 from each end (body=2L/3). Continuous bars remain full length. Top middle-only cuts use the central L/2. Development/hook allowances are added separately.",
					Location = new Point(20, 290),
					Size = new Size(620, 76)
				};
				Button button = new Button
				{
					Text = "OK",
					Location = new Point(535, 378),
					Size = new Size(105, 32),
					DialogResult = DialogResult.OK
				};
				form.Controls.AddRange(new Control[5] { groupBox, groupBox2, groupBox3, label, button });
				form.AcceptButton = button;
				form.CancelButton = button;
				form.ShowDialog(this);
			}
		}

		private bool SaveProjectDialog()
		{
			using (SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				Filter = "Beam estimate project (*.beam-estimate.xml)|*.beam-estimate.xml",
				FileName = "Beam-Estimate.beam-estimate.xml"
			})
			{
				if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
				{
					return false;
				}
				SaveTo(saveFileDialog.FileName);
				return true;
			}
		}

		private void SaveProject()
		{
			if (Allowed())
			{
				SaveProjectDialog();
			}
		}

		private void OpenProject()
		{
			if (!Allowed() || !ReplaceOkay())
			{
				return;
			}
			using (OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = "Beam estimate project (*.beam-estimate.xml)|*.beam-estimate.xml"
			})
			{
				if (openFileDialog.ShowDialog(this) == DialogResult.OK)
				{
					OpenFrom(openFileDialog.FileName);
				}
			}
		}
	}
}
