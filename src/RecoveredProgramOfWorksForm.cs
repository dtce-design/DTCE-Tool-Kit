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
	internal partial class ProgramOfWorksForm : Form
	{
		private DataGridView roofProfiles;

		private readonly List<RoofProfile> savedRoofProfiles = new List<RoofProfile>();

		private Label aacTotals;

		private readonly Dictionary<string, DataGridView> masonryGrids = new Dictionary<string, DataGridView>();

		private static readonly string[] MasonrySections = new string[3] { "MasonryLoadBearing", "MasonryLouver", "MasonryAAC" };

		private static readonly string[] MasonryNames = new string[3] { "Load Bearing CHB", "Louver Blocks", "AAC Blocks" };

		private static readonly string[] MasonryTotalNames = new string[4] { "Load Bearing CHB", "Louver Blocks", "AAC Blocks 100 mm", "AAC Blocks 150 mm" };

		private DataGridView mouldingEntries;

		private readonly List<MouldingTakeoff> manualMouldings = new List<MouldingTakeoff>();

		private DataGridView plumbingPipes;

		private DataGridView plumbingLs;

		private DataGridView plumbingChambers;

		private DataGridView plumbingRebar;

		private DataGridView plumbingSummary;

		private readonly List<PipeMeasure> pipeMeasures = new List<PipeMeasure>();

		private readonly List<ChamberTakeoff> chamberMeasures = new List<ChamberTakeoff>();

		private readonly List<BillQuantity> manualPlumbingLs = new List<BillQuantity>();

		private readonly List<StockAllocation> chamberBarPlan = new List<StockAllocation>();

		private FlowLayoutPanel footingSettings;

		private ComboBox footingFc;

		private ComboBox footingSmall;

		private ComboBox footingLarge;





		private DataGridView canopies;

		private readonly List<CanopyTakeoff> canopyResults = new List<CanopyTakeoff>();

		private List<StockAllocation> canopyPlan = new List<StockAllocation>();

		private List<double> canopyStock = new List<double>(CommercialBars.Lengths);

		private FlowLayoutPanel canopySettings;

		private ComboBox canopyFc;

		private ComboBox canopySmall;

		private ComboBox canopyLarge;

		internal static readonly string[] InsulationTypes = new string[2] { "8mm thk. Double-Sided Aluminum Foil Bubble Insulation", "10mm thk. Aluminum Foil Double Faced Polyethylene Foam Insulation" };

		private DataGridView insulationRolls;

		private NumericUpDown flashingLap;

		private NumericUpDown gutterLap;

		private NumericUpDown ridgeLap;

		private DataGridView cutFill;

		private DataGridView footings;

		private DataGridView earthBalanceGrid;

		private readonly List<CutFillInterval> cutIntervals = new List<CutFillInterval>();

		private readonly List<EarthworkMaterial> earthSources = new List<EarthworkMaterial>();

		private readonly List<FootingTakeoff> footingResults = new List<FootingTakeoff>();

		private List<double> footingStock = new List<double>(CommercialBars.Lengths);

		private List<StockAllocation> footingPlan = new List<StockAllocation>();

		private NumericUpDown borrowSwell;

		private NumericUpDown borrowShrink;

		private NumericUpDown gravelAllowance;

		private NumericUpDown suitableSoil;

		private Label earthStatus;

		private EarthworkBalance earthBalance;

		private List<StockAllocation> projectRebarPlan = new List<StockAllocation>();
		private double rebarAlone, rebarShared;

		private Label roofLibraryStatus;

		private DataGridView steelPurlins;

		private DataGridView steelAngles;

		private DataGridView machineBolts;

		private DataGridView anchorBolts;

		private DataGridView steelPlates;

		private DataGridView crossBracing;

		private DataGridView fasciaFrame;

		private DataGridView steelCutGrid;

		private readonly List<SteelCutDemand> steelDemands = new List<SteelCutDemand>();

		private List<SteelStock> steelStocks = new List<SteelStock>();

		private List<PlateSheet> plateStocks = new List<PlateSheet>();

		private static readonly string[] SteelSections = new string[7] { "SteelPurlins", "SteelAngles", "MachineBolts", "AnchorBolts", "SteelPlates", "CrossBracing", "FasciaFrame" };

		private static readonly string[] SteelPageNames = new string[7] { "Purlin Estimate", "Angle Bars", "Machine Bolts", "Anchor Bolts", "Steel Plates", "Crossbracing", "Metal Fascia Frame" };

		private DataGridView roofSheets;

		private DataGridView roofAccessories;

		private readonly EstimatorForm beam;

		private readonly SlabEstimatorForm slab;

		private readonly TabControl tabs = new NavigationTabs
		{
			Dock = DockStyle.Fill
		};

		private readonly ComboBox reportScope = new ComboBox
		{
			DropDownStyle = ComboBoxStyle.DropDownList,
			Width = 255
		};

		private readonly ComboBox reportDetail = new ComboBox
		{
			DropDownStyle = ComboBoxStyle.DropDownList,
			Width = 150
		};

		private readonly DataGridView wood;

		private readonly DataGridView aluminum;

		private readonly DataGridView walls;

		private readonly DataGridView manualOpeningsGrid;

		private readonly DataGridView finishSchedule;

		private readonly DataGridView wallFinishes;

		private readonly DataGridView ceilingFinishes;

		private readonly DataGridView floorFinishes;

		private readonly DataGridView otherFinishes; /* Other Finishes sub tab (owner, 20 November 2026) */

		private readonly Label status = new Label
		{
			Dock = DockStyle.Bottom,
			Height = 32
		};

		private readonly CalculationReportView preview = new CalculationReportView{Dock=DockStyle.Fill};
        private bool renderingReportPreview;

		private readonly Dictionary<string, string> finishGroups = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		private FinishTypeRegistry finishTypeCells = new FinishTypeRegistry();

		private readonly List<FinishItem> finishItems = new List<FinishItem>();

		private readonly Dictionary<string, PlasterTakeoff> paintResults = new Dictionary<string, PlasterTakeoff>(StringComparer.OrdinalIgnoreCase);

		private readonly Dictionary<string, PlasterTakeoff> plasterResults = new Dictionary<string, PlasterTakeoff>(StringComparer.OrdinalIgnoreCase);

		private readonly List<OpeningLink> openingAssignments = new List<OpeningLink>();

		private readonly List<OpeningItem> wallOpenings = new List<OpeningItem>();

		private readonly List<OpeningItem> openings = new List<OpeningItem>();

		private readonly List<WallItem> wallItems = new List<WallItem>();

		private readonly List<string> errors = new List<string>();

		private int nextWallNumber = 1;

		private bool filling;

        // Control/default events can fire while tabs and their dictionaries are incomplete.
        private bool initializingWorks = true;

		private bool syncingExcel;

		private bool childWorksDirty;

		private bool snapshottingWorks;

		private bool dashboardDirty = true;

		private bool reportPreviewDirty = true;

		private bool plumbingSummaryDirty = true;

		private string saved = "";

		private string lastExcelXml = "";

		private ExcelLiveLink worksExcel;

		private readonly Timer worksExcelTimer = new Timer
		{
			Interval = 75
		};

		private readonly Label excelStatus = new Label
		{
			AutoSize = false,
			Width = 1100,
			Height = 64,
			Text = "",
			Padding = new Padding(8)
		};

		private readonly Label homeSummary = new Label
		{
			AutoSize = false,
			Width = 1100,
			Height = 90,
			Padding = new Padding(8)
		};

		private DataGridView boqGrid;

		private Label boqStatus;

		private readonly Dictionary<string, SteelProfile> projectSteelProfiles = new Dictionary<string, SteelProfile>(StringComparer.Ordinal);

		private DataGridView gravelEntries;

		private DataGridView gravelSummaryGrid;

		private FlowLayoutPanel doorSettings;

		private NumericUpDown jambDepthDefault;

		private NumericUpDown jambThicknessDefault;

		private DataGridView finishAssignments;

		private DataGridView rcWalls;

		private Label rcWallLayerSummary;

		private readonly List<RCWallTakeoff> rcWallResults = new List<RCWallTakeoff>();

		private List<StockAllocation> rcWallPlan = new List<StockAllocation>();

		private List<double> rcWallStock = new List<double>(CommercialBars.Lengths);

		private FlowLayoutPanel rcWallSettings;

		private ComboBox rcWallFc;

		private ComboBox rcWallSmall;

		private ComboBox rcWallLarge;

		private readonly List<ChamberSheetView> chamberSheets = new List<ChamberSheetView>();

		private long chamberRevision;

		private string activeChamberRebarId = "";

		private List<BackupBillItem> backupBill = new List<BackupBillItem>();

		private bool projectQuantitiesKnown;

		private readonly Dictionary<string, string> backupBillRoutes = new Dictionary<string, string>();

		private TabControl boqPages;

		private bool paintingBackupBill;

		private readonly Dictionary<string, NumericUpDown> rcCovers = new Dictionary<string, NumericUpDown>();

		private Label rcDefaultsStatus;

		private TabControl pipeGroupTabs;

		private bool changingPipeGroup;

		private DataGridView plumbingMembers;

		private string activeStructureMemberId = "";

		private readonly List<StructureMemberTakeoff> structureMemberResults = new List<StructureMemberTakeoff>();

		private readonly HashSet<string> structureMemberErrors = new HashSet<string>();

		private readonly Dictionary<string, LengthConfiguration> structureConfigurations = new Dictionary<string, LengthConfiguration>(StringComparer.OrdinalIgnoreCase);

		private IEnumerable<WallItem> RegularWalls
		{
			get
			{
				return wallItems.Where((WallItem w) => w.Material == "CHB");
			}
		}

		internal List<WorksExcelTable> FinalQuantityTables()
		{
			List<WorksExcelTable> source = WorksExcelTables();
			List<WorksExcelTable> list = source.Where((WorksExcelTable t) => t.Name == "DTCE Tool Kit - POW").ToList();
			List<object[]> list2 = new List<object[]>();
			string[] array = new string[5] { "POW Masonry Summary", "POW Roofing Summary", "POW Structural Steel Summary", "POW Overall Earthworks", "POW Plumbing Summary" };
			foreach (string name in array)
			{
				WorksExcelTable worksExcelTable = source.Single((WorksExcelTable t) => t.Name == name);
				for (int num2 = 0; num2 < ((name == "POW Overall Earthworks") ? Math.Min(6, worksExcelTable.Rows.GetLength(0)) : worksExcelTable.Rows.GetLength(0)); num2++)
				{
					list2.Add(new object[3]
					{
						Convert.ToString(worksExcelTable.Rows[num2, 0]),
						worksExcelTable.Rows[num2, 1],
						worksExcelTable.Rows[num2, 2]
					});
				}
			}
			list2.AddRange(from q in ManualBoqQuantities()
				select new object[3] { q.Description, q.Quantity, q.Unit });
			list2.AddRange(ExteriorMouldingTotals());
			list2.AddRange(CanopySummary());
			list2.AddRange(GravelSummary());
            list2.AddRange(ExtraBillQuantities().Select(q=>new object[]{q.Description,q.Quantity,q.Unit}));
			list.Add(new WorksExcelTable("DTCE Tool Kit - POW", new string[3] { "QUANTITY", "Quantity", "Unit" }, list2, 1, 8));
			return list;
		}

		private void EditFooting(bool add)
		{
			Guard();
			Commit();
			DataGridViewRow row = (add ? null : footings.CurrentRow);
			if (!add && (row == null || row.IsNewRow))
			{
				throw new Exception("Select a footing row first.");
			}
			string[] array = new string[28];
			if (row != null)
			{
				for (int i = 0; i < 28; i++)
				{
					array[i] = Cell(row, i);
				}
			}
			else
			{
				Array.Copy(new string[28]
				{
					"",
					"",
					"2",
					"2",
					"0.40",
					"",
					"",
					rcCovers["Footing"].Value.ToString(CultureInfo.InvariantCulture),
					"16",
					"150",
					"",
					"16",
					"150",
					"",
					"",
					"",
					"",
					"",
					"",
					"",
					"0",
					"Formed sides",
					"100",
					"20",
					"10",
					footingFc.Text,
					footingSmall.Text,
					footingLarge.Text
				}, array, 28);
			}
			if(add)foreach(int i in new[]{2,3,4,7,8,9,10,11,12,13,14,15,16,17,18,19,20})array[i]="";
            array[22] = suitableSoil.Value.ToString(CultureInfo.InvariantCulture);
			array[23] = borrowSwell.Value.ToString(CultureInfo.InvariantCulture);
			array[24] = borrowShrink.Value.ToString(CultureInfo.InvariantCulture);
			FootingEntryForm dialog = new FootingEntryForm(array,footingInputMethods.Select(m=>m==null?"Spacing (mm)":m.Text).ToArray(),(double)rcCovers["Footing"].Value);
			try
			{
				if (dialog.ShowDialog(this) != DialogResult.OK)
				{
					return;
				}
				if (footings.Rows.Cast<DataGridViewRow>().Any((DataGridViewRow r) => !r.IsNewRow && r != row && string.Equals(Cell(r, 0), dialog.Values[0], StringComparison.OrdinalIgnoreCase)))
				{
					throw new Exception("Duplicate footing ID.");
				}
				filling = true;
				try
				{
					if (row == null)
					{
						row = footings.Rows[footings.Rows.Add()];
					}
					for (int num = 0; num < 28; num++)
					{
						row.Cells[num].Value = dialog.Values[num];
					}
				}
				finally
				{
					filling = false;
				}
				Recalculate();
			}
			finally
			{
				if (dialog != null)
				{
					((IDisposable)dialog).Dispose();
				}
			}
		}

		private void AddRoofProfiles()
		{
			roofProfiles = Grid(new string[6] { "Saved profile name", "Family", "Full sheet width mm", "Side lap option", "Effective cover mm", "Status" }, 5);
			AddCombo(roofProfiles, 1, "Family", new string[2] { "Longspan Rib", "Longspan Corrugated" });
			roofProfiles.Columns[0].Width = 230;
			roofProfiles.Columns[1].Width = 210;
			roofProfiles.Columns[3].Width = 160;
			roofProfiles.Rows.Add("Longspan Rib", "Longspan Rib", "1090", "1 rib", "1030");
			roofProfiles.Rows.Add("Longspan Rib", "Longspan Rib", "1090", "2 ribs", "750");
			roofProfiles.Rows.Add("Longspan Corrugated", "Longspan Corrugated", "1070", "1-1/2", "970");
			roofProfiles.Rows.Add("Longspan Corrugated", "Longspan Corrugated", "1070", "2-1/2", "890");
			AddGrid("Roofing Start", roofProfiles, "Save supplier profiles here. Repeat the profile name for each allowed side-lap option. Widths are in mm; effective cover already accounts for side lap. The supplied DN reference profiles are editable estimating presets. Choose a saved profile and side lap in the sheet worksheet.");
		}

		private void RefreshRoofProfiles()
		{
			savedRoofProfiles.Clear();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (DataGridViewRow item in (IEnumerable)roofProfiles.Rows)
			{
				if (item.IsNewRow || Empty(item))
				{
					continue;
				}
				try
				{
					RoofProfile profile = new RoofProfile
					{
						Name = Cell(item, 0),
                        Material = Cell(item,6)==""?RoofingPaySpecification.LegacyMaterial(Cell(item,0)):Cell(item,6),
						Family = Cell(item, 1),
						Width = Number(item, 2) / 1000.0,
						SideLap = Cell(item, 3),
						Cover = Number(item, 4) / 1000.0
					};
					RoofMath.Positive(profile.Width, profile.Cover);
                    RoofingPaySpecification.ValidateMaterial(profile.MaterialType);item.Cells[6].Value=profile.MaterialType;
					if (profile.Name == "" || profile.SideLap == "" || !new string[2] { "Longspan Rib", "Longspan Corrugated" }.Contains(profile.Family) || profile.Cover > profile.Width)
					{
						throw new Exception("Enter profile, family, side lap and cover <= full width.");
					}
					if (!hashSet.Add(profile.Name + "|" + profile.SideLap))
					{
						throw new Exception("Duplicate profile / side lap option.");
					}
					if (savedRoofProfiles.Any((RoofProfile p) => string.Equals(p.Name, profile.Name, StringComparison.OrdinalIgnoreCase) && (p.MaterialType!=profile.MaterialType || p.Family != profile.Family || Math.Abs(p.Width - profile.Width) > 1E-08)))
					{
						throw new Exception("A profile name must have the same family and full width for all lap options.");
					}
					savedRoofProfiles.Add(profile);
					item.Cells[5].Value = "Saved in project";
				}
				catch (Exception ex)
				{
					item.Cells[5].Value = ex.Message;
					errors.Add("Roof profile: " + ex.Message);
				}
			}
            RefreshRoofProfileChoices(savedRoofProfiles.Select(p=>p.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
		}

		private void ApplyRoofProfile(DataGridViewRow row)
		{
			row.Cells[8].Value = "";
			row.Cells[9].Value = "0";
			List<RoofProfile> list = savedRoofProfiles.Where((RoofProfile p) => string.Equals(p.Name, Cell(row, 2), StringComparison.OrdinalIgnoreCase)).ToList();
			if (list.Count == 0)
			{
				row.Cells[2].ErrorText="Choose a saved profile from Roofing Start.";
                throw new Exception("Choose a saved profile from Roofing Start.");
			}
			string lap = Cell(row, 17);
			if (!list.Any((RoofProfile p) => p.SideLap == lap))
			{
				string legacy = Cell(row, 18);
				lap = (list.Any((RoofProfile p) => p.SideLap == legacy) ? legacy : list[0].SideLap);
			}
            RefreshRoofLapChoices(row,list,lap);
			RoofProfile roofProfile = list.First((RoofProfile p) => p.SideLap == lap);
			row.Cells[6].Value = roofProfile.Width.ToString("0.###", CultureInfo.InvariantCulture);
			row.Cells[7].Value = roofProfile.Cover.ToString("0.###", CultureInfo.InvariantCulture);
			DataGridViewCell dataGridViewCell = row.Cells[6];
			bool readOnly = (row.Cells[7].ReadOnly = true);
			dataGridViewCell.ReadOnly = readOnly;
		}

		private string RoofFamily(DataGridViewRow row)
		{
			RoofProfile roofProfile = savedRoofProfiles.FirstOrDefault((RoofProfile p) => string.Equals(p.Name, Cell(row, 2), StringComparison.OrdinalIgnoreCase));
			if (roofProfile != null)
			{
				return roofProfile.Family;
			}
			return Cell(row, 2);
		}

		private IEnumerable<DataGridView> MasonryGrids()
		{
			return new DataGridView[1] { walls }.Concat(masonryGrids.Values);
		}

		private static bool IsMasonrySection(string section)
		{
			if (!(section == "Walls"))
			{
				return MasonrySections.Contains(section);
			}
			return true;
		}

		private void NormalizeMasonryRow(DataGridViewRow row)
		{
			if (masonryGrids.FirstOrDefault((KeyValuePair<string, DataGridView> x) => x.Value == row.DataGridView).Key == "MasonryAAC" && Cell(row, 18) == "")
			{
				row.Cells[18].Value = "100";
			}
		}

		private string WallMaterial(DataGridViewRow row)
		{
			KeyValuePair<string, DataGridView> keyValuePair = masonryGrids.FirstOrDefault((KeyValuePair<string, DataGridView> x) => x.Value == row.DataGridView);
			int num = Array.IndexOf(MasonrySections, keyValuePair.Key);
			if (!(keyValuePair.Key == "MasonryAAC"))
			{
				if (num >= 0)
				{
					return MasonryNames[num];
				}
				return "CHB";
			}
			return "AAC Blocks " + Cell(row, 18) + " mm";
		}

		private void NormalizeLegacyAAC(XmlElement root)
		{
			if (root["MasonryAAC"] != null)
			{
				return;
			}
			XmlElement xmlElement = root["MasonryAAC100"];
			XmlElement xmlElement2 = root["MasonryAAC150"];
			if (xmlElement == null && xmlElement2 == null)
			{
				return;
			}
			XmlElement xmlElement3 = root.OwnerDocument.CreateElement("MasonryAAC");
			XmlElement[] array = new XmlElement[2] { xmlElement, xmlElement2 };
			foreach (XmlElement xmlElement4 in array)
			{
				if (xmlElement4 == null)
				{
					continue;
				}
				foreach (XmlElement item in xmlElement4.SelectNodes("Entry"))
				{
					XmlElement xmlElement5 = (XmlElement)item.CloneNode(true);
					xmlElement5.SetAttribute("c18", (xmlElement4 == xmlElement) ? "100" : "150");
					xmlElement3.AppendChild(xmlElement5);
				}
			}
			root.AppendChild(xmlElement3);
		}

		private void AddMasonryMaterials()
		{
			int[] array;
			for (int i = 0; i < MasonrySections.Length; i++)
			{
				int index = i;
				DataGridView grid = WallGrid();
				masonryGrids.Add(MasonrySections[i], grid);
				grid.Columns[9].HeaderText = "Net area m2";
				grid.Columns[18].HeaderText = "Thickness mm";
				array = new int[4] { 12, 15, 16, 17 };
				foreach (int index2 in array)
				{
					grid.Columns[index2].Visible = false;
				}
				if (i == 1)
				{
					grid.Columns[18].Visible = false;
				}
				Action<DataGridViewRow> defaults = delegate(DataGridViewRow row)
				{
					if (index >= 2)
					{
						row.Cells[18].Value = ((index == 2) ? "100" : "150");
					}
				};
				grid.DefaultValuesNeeded += delegate(object s, DataGridViewRowEventArgs e)
				{
					defaults(e.Row);
				};
				grid.RowsAdded += delegate(object s, DataGridViewRowsAddedEventArgs e)
				{
					if (!filling)
					{
						for (int k = e.RowIndex; k < e.RowIndex + e.RowCount; k++)
						{
							defaults(grid.Rows[k]);
						}
					}
				};
				AddGrid(MasonryNames[i], grid, "Area takeoff in m2. Enter Width x Height x Qty OR total gross area, then deduct selected openings. Material totals are kept separate. Load-bearing classification does not perform a structural design check.");
				if (i == 2)
				{
					aacTotals = new Label
					{
						Dock = DockStyle.Bottom,
						Height = 44,
						Padding = new Padding(10),
						ForeColor = PowTheme.Teal
					};
					tabs.TabPages.Cast<TabPage>().Single((TabPage p) => p.Text == "AAC Blocks").Controls.Add(aacTotals);
				}
			}
			array = new int[4] { 12, 15, 16, 17 };
			foreach (int index3 in array)
			{
				walls.Columns[index3].Visible = false;
			}
		}

		private void UpdateMasonryTotals()
		{
			if (aacTotals != null)
			{
				aacTotals.Text = ((errors.Count > 0) ? "Review partial inputs | " : "") + "AAC 100 mm: " + wallItems.Where((WallItem w) => w.Material == "AAC Blocks 100 mm").Sum((WallItem w) => w.Net(wallOpenings)).ToString("0.00", CultureInfo.InvariantCulture) + " m2     |     AAC 150 mm: " + wallItems.Where((WallItem w) => w.Material == "AAC Blocks 150 mm").Sum((WallItem w) => w.Net(wallOpenings)).ToString("0.00", CultureInfo.InvariantCulture) + " m2";
			}
		}

		private Dictionary<string, DataGridView> WithMasonry(Dictionary<string, DataGridView> tables)
		{
			foreach (KeyValuePair<string, DataGridView> masonryGrid in masonryGrids)
			{
				tables.Add(masonryGrid.Key, masonryGrid.Value);
			}
			foreach (KeyValuePair<string, DataGridView> item in SteelTables())
			{
				tables.Add(item.Key, item.Value);
			}
			return tables;
		}

		private void AppendMasonryAreas(StringBuilder b, bool detailed)
		{
			b.AppendLine();
			b.AppendLine("MASONRY WORKS | NET WALL AREAS");
			int[] array = new int[2] { 100, 150 };
			foreach (int thickness in array)
			{
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "CHB {0} mm TOTAL: {1:0.00} m2", thickness, RegularWalls.Where((WallItem w) => w.ThicknessMm == (double)thickness).Sum((WallItem w) => w.Net(wallOpenings))));
			}
			string[] masonryTotalNames = MasonryTotalNames;
			foreach (string material in masonryTotalNames)
			{
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0} TOTAL: {1:0.00} m2", material, wallItems.Where((WallItem w) => w.Material == material).Sum((WallItem w) => w.Net(wallOpenings))));
			}
			if (!detailed)
			{
				return;
			}
			foreach (WallItem wall in wallItems)
			{
				b.AppendLine();
				b.AppendLine(wall.Material + " | " + wall.Id + " | " + wall.Description);
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "Gross {0:0.00} - openings {1:0.00} = net {2:0.00} m2", wall.Gross, wall.Deduction(wallOpenings), wall.Net(wallOpenings)));
				foreach (OpeningItem item in wallOpenings.Where((OpeningItem o) => string.Equals(o.Wall, wall.Id, StringComparison.OrdinalIgnoreCase)))
				{
					b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Less {0}: {1:0.00} x {2:0.00} x {3} = {4:0.00} m2", item.Id, item.Width, item.Height, item.Quantity, item.Area));
				}
			}
		}

		private void MasonryExcel(List<WorksExcelTable> tables)
		{
			List<object[]> list = new List<object[]>();
			int[] array = new int[2] { 100, 150 };
			for (int i = 0; i < array.Length; i++)
			{
				int thickness = array[i];
				list.Add(new object[3]
				{
					"CHB " + thickness + " mm",
					RegularWalls.Where((WallItem w) => w.ThicknessMm == (double)thickness).Sum((WallItem w) => w.Net(wallOpenings)),
					"m2"
				});
			}
			for (int num = 0; num < MasonryTotalNames.Length; num++)
			{
				string material = MasonryTotalNames[num];
				List<WallItem> source = wallItems.Where((WallItem w) => w.Material == material).ToList();
				list.Add(new object[3]
				{
					material,
					source.Sum((WallItem w) => w.Net(wallOpenings)),
					"m2"
				});
				tables.Add(new WorksExcelTable((new string[4] { "POW Load Bearing CHB", "POW Louver Blocks", "POW AAC 100 mm", "POW AAC 150 mm" })[num], new string[5] { "Wall ID", "Description", "Gross m2", "Openings m2", "Net area m2" }, source.Select((WallItem w) => new object[5]
				{
					w.Id,
					w.Description,
					w.Gross,
					w.Deduction(wallOpenings),
					w.Net(wallOpenings)
				}).ToList()));
			}
			tables.Add(new WorksExcelTable("POW Masonry Summary", new string[3] { "Material", "Net area", "Unit" }, list));
		}

		private void AddMouldingFinishes()
		{
			mouldingEntries = Grid(new string[7] { "ID", "Description / location", "Moulding profile / size (mm)", "Length m / unit", "Qty", "Total length m", "Status" }, 5);
			mouldingEntries.Columns[2].Width = 260;
			SetDefaults(mouldingEntries, new Dictionary<int, string> { { 4, "1" } });
			AddGrid("Moulding", mouldingEntries, "Choose an Opening ID to add moulding around that door or window (length from its size and quantity), or leave it blank and enter other moulding lengths. Use the same profile and size in mm to combine totals. Final quantity is linear metres, grouped by profile/size.");
			AddMouldingOpeningColumn();
		}

		private void RecalculateMouldings()
		{
			manualMouldings.Clear();
			if (mouldingEntries == null)
			{
				return;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (DataGridViewRow item in (IEnumerable)mouldingEntries.Rows)
			{
				if (item.IsNewRow)
				{
					continue;
				}
				DataGridViewCell dataGridViewCell = item.Cells[5];
				object value = (item.Cells[6].Value = "");
				dataGridViewCell.Value = value;
				if (Empty(item))
				{
					continue;
				}
				if (OpeningMouldingRow(item))
				{
					continue;
				}
				item.Cells[3].ReadOnly = item.Cells[4].ReadOnly = false;
				item.Cells[3].Style.BackColor = item.Cells[4].Style.BackColor = ModernUi.Input;
				try
				{
					RequireDescription(item);
					MouldingTakeoff mouldingTakeoff = new MouldingTakeoff
					{
						Id = Cell(item, 0),
						Description = Cell(item, 1),
						Profile = Cell(item, 2),
						Length = Number(item, 3),
						Quantity = Count(item, 4)
					};
					if (!hashSet.Add(mouldingTakeoff.Id))
					{
						throw new Exception("Duplicate moulding ID.");
					}
					double total = mouldingTakeoff.Total;
					manualMouldings.Add(mouldingTakeoff);
					Measure(item, 5, total);
					item.Cells[6].Value = "Estimated";
				}
				catch (Exception ex)
				{
					item.Cells[6].Value = ex.Message;
					errors.Add("Moulding " + Cell(item, 0) + ": " + ex.Message);
				}
			}
		}

		private List<BillQuantity> MouldingQuantities()
		{
			List<BillQuantity> list = new List<BillQuantity>();
			foreach (OpeningItem item in openings.Where((OpeningItem o) => o.Exterior && o.Material != "Manual"))
			{
				string section = (string.IsNullOrWhiteSpace(item.MouldingProfile) ? "Unspecified size (assign opening moulding profile)" : item.MouldingProfile.Trim());
				list.Add(new BillQuantity
				{
					Section = section,
					Description = item.Id + " | " + item.Kind + " | " + item.Description,
					Quantity = item.MouldingLength,
					Unit = "m"
				});
			}
			foreach (MouldingTakeoff manualMoulding in manualMouldings)
			{
				list.Add(new BillQuantity
				{
					Section = manualMoulding.Profile.Trim(),
					Description = manualMoulding.Id + " | " + manualMoulding.Description,
					Quantity = manualMoulding.Total,
					Unit = "m"
				});
			}
			List<BillQuantity> list2 = new List<BillQuantity>();
			foreach (IGrouping<string, BillQuantity> item2 in from e in list
				group e by MouldingTakeoff.Key(e.Section) into g
				orderby g.Key
				select g)
			{
				BillQuantity billQuantity = new BillQuantity
				{
					Section = "Moulding",
					Description = "Moulding | " + item2.First().Section,
					Quantity = item2.Sum((BillQuantity e) => e.Quantity),
					Unit = "m"
				};
				billQuantity.Sources.AddRange(MergeSources(item2));
				list2.Add(billQuantity);
			}
			return list2;
		}

		private List<object[]> ExteriorMouldingTotals()
		{
			return (from q in MouldingQuantities()
				select new object[3] { q.Description, q.Quantity, q.Unit }).ToList();
		}

		private void AddMouldingColumns(DataGridView grid)
		{
			grid.Columns.Add(new DataGridViewCheckBoxColumn
			{
				Name = "ExteriorMoulding",
				HeaderText = "Exterior moulding",
				Width = 125,
				TrueValue = true,
				FalseValue = false
			});
			grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "MouldingLength",
				HeaderText = "Exterior moulding lm",
				Width = 145,
				ReadOnly = true
			});
			grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "MouldingProfile",
				HeaderText = "Moulding profile / size (mm)",
				Width = 230
			});
			grid.Columns[24].DefaultCellStyle.BackColor = ModernUi.Input;
			grid.Columns[24].ToolTipText = "Same profile and size combines with Finishing Works > Moulding. Enter size in mm, e.g. Concrete 150 x 50 mm.";
			grid.Columns[22].DefaultCellStyle.BackColor = ModernUi.Input;
			grid.Columns[22].ToolTipText = "Check exterior openings only. Window: all four sides. Door: top and two sides, excluding the bottom. One exterior face only.";
			grid.CurrentCellDirtyStateChanged += delegate
			{
				if (grid.IsCurrentCellDirty && grid.CurrentCell is DataGridViewCheckBoxCell)
				{
					grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
				}
			};
			grid.CellValueChanged += delegate(object s, DataGridViewCellEventArgs e)
			{
				if (!filling && e.RowIndex >= 0 && e.ColumnIndex == 22)
				{
					Recalculate();
				}
			};
		}

		private void AppendMouldingReport(StringBuilder report)
		{
			report.AppendLine();
			report.AppendLine("MOULDING | TOTAL LINEAR METRES BY PROFILE / SIZE");
			foreach (object[] item in ExteriorMouldingTotals())
			{
				report.AppendLine(Convert.ToString(item[0]) + ": " + Convert.ToDouble(item[1]).ToString("0.00", CultureInfo.InvariantCulture) + " lm");
			}
			foreach (MouldingTakeoff manualMoulding in manualMouldings)
			{
				report.AppendLine(manualMoulding.Id + " | " + manualMoulding.Description + " | " + manualMoulding.Profile + " | " + manualMoulding.Length.ToString("0.###", CultureInfo.InvariantCulture) + "m x " + manualMoulding.Quantity + " = " + manualMoulding.Total.ToString("0.00", CultureInfo.InvariantCulture) + " m");
			}
			foreach (OpeningItem item2 in openings.Where((OpeningItem o) => o.Exterior && o.Material != "Manual"))
			{
				report.AppendLine(item2.Id + " | " + item2.Kind + " | " + ((item2.Kind == "Window") ? "2(W+H)" : "W+2H") + " x quantity");
				report.AppendLine("  W=" + item2.Width.ToString("0.###", CultureInfo.InvariantCulture) + "m; H=" + item2.Height.ToString("0.###", CultureInfo.InvariantCulture) + "m; qty=" + item2.Quantity + " -> " + item2.MouldingLength.ToString("0.00", CultureInfo.InvariantCulture) + " lm");
			}
			report.AppendLine("Opening sources use overall dimensions on the exterior face only; doors exclude the bottom. Manual entries are additional lengths. No mitre or percentage waste allowance.");
		}

		internal List<BackupDemand> BackupQuantities(bool includeManual = true)
		{
			List<BackupDemand> list = new List<BackupDemand>();
			bool flag = projectQuantitiesKnown;
			foreach (BillQuantity item in BillOfQuantities())
			{
				if (!flag && item.Section != "Bill of Quantities")
				{
					continue;
				}
				bool flag2 = item.Section == "Bill of Quantities" || item.Section == "Rebar purchase by size" || item.Section == "Moulding" || item.Section == "Finish material breakdown";
				if (item.Section == "Reinforced Concrete Works")
				{
					flag2 = new string[8] { "Beam concrete (gross)", "Slab concrete (gross)", "Footing concrete", "Beam forms", "Slab forms", "Footing forms", "RC wall concrete", "RC wall forms" }.Contains(item.Description);
				}
				if (item.Section == "Canopy / Ledge")
				{
					flag2 = item.Unit == "m3" || (item.Description.ToLowerInvariant().Contains("total") && item.Unit == "m2");
				}
				if (item.Section == "Earthworks")
				{
					flag2 = !item.Description.ToLowerInvariant().Contains("reference");
				}
				if (item.Section == "Gravel")
				{
					flag2 = item.Description.ToUpperInvariant().Contains("TO BUY");
				}
				if (item.Section == "Masonry Works")
				{
					flag2 = !item.Description.Contains("Load Bearing");
				}
				if (item.Section == "Finishing Works")
				{
					flag2 = item.Description == "Net plaster" || item.Description == "Masonry paint";
				}
				if (item.Section == "Doors & Windows")
				{
					flag2 = true;
				}
				if (item.Section == "Roofing Works")
				{
					flag2 = !item.Description.EndsWith("net area") && !item.Description.Contains("total (net roof area)");
				}
				if (item.Section == "Structural Steel Works")
				{
					flag2 = new string[5] { "Purlins PURCHASE weight", "Angle bars PURCHASE weight", "Metal fascia PURCHASE weight", "Steel plates PURCHASE weight", "Crossbracing rods" }.Contains(item.Description) || item.Description.StartsWith("Metal painting - ") /* owner, 18 November 2026: painting broken down per member type */ || item.Description.StartsWith("Turnbuckles dia ");
				}
				if(item.Section=="Reinforced Concrete Works"&&item.Description.ToLowerInvariant().Contains("concrete"))flag2=false;
                if(item.Section=="Canopy / Ledge"&&item.Unit=="m3")flag2=false;
                if((item.Section=="Miscellaneous Structures"||item.Section=="Wall Footing"))flag2=item.Unit=="m2";
                if(new[]{"Pavement Construction","Slope Protection Works","Carpentry and Joinery Works","Waterproofing"}.Contains(item.Section))flag2=true;
                if(item.Section=="Railing"||item.Section=="Sag Rods")flag2=!item.Description.Contains("reference");
                if(item.Section=="Earthworks"&&!string.IsNullOrEmpty(item.PayCode))flag2=true;
                if (flag2)
				{
					BackupDemand backupDemand = BackupDemand.From(item);
					if (item.Section == "Bill of Quantities")
					{
						backupDemand.ManualOverride = true;
						backupDemand.ManualPriority = 1;
					}
					list.Add(backupDemand);
				}
			}
			if (flag)
			{
				foreach (var item2 in from w in wallItems
					where w.Material.Contains("Load Bearing")
					group w by new { w.Material, w.ThicknessMm })
				{
					BillQuantity billQuantity = new BillQuantity
					{
						Section = "Masonry Works",
						Description = item2.Key.ThicknessMm + " mm " + item2.Key.Material,
						Unit = "m2",
						Quantity = item2.Sum((WallItem w) => w.Net(wallOpenings))
					};
					billQuantity.Sources.AddRange(item2.Select((WallItem w) => SourceQuantity(w.Id + " | " + w.Description, w.Net(wallOpenings), "m2")));
					list.Add(BackupDemand.From(billQuantity));
				}
			}
			if (flag)
			{
				DataGridView[] array = new DataGridView[2] { machineBolts, anchorBolts };
				foreach (DataGridView grid in array)
				{
					string[] array2 = new string[2] { "A307", "A325" };
					foreach (string grade in array2)
					{
						IEnumerable<DataGridViewRow> enumerable = from DataGridViewRow r in grid.Rows
							where !r.IsNewRow && Cell(r, 2) == grade && Cell(r, grid.Columns.Count - 1) == "Estimated"
							select r;
						BillQuantity billQuantity2 = new BillQuantity
						{
							Section = "Structural Steel Works",
							Description = ((grid == machineBolts) ? "Machine bolts " : "Anchor bolts ") + grade + " shaft weight",
							Unit = "kg"
						};
						foreach (DataGridViewRow item3 in enumerable)
						{
							double num3 = Convert.ToDouble(item3.Cells[7].Value, CultureInfo.InvariantCulture);
							billQuantity2.Quantity += num3;
							billQuantity2.Sources.Add(SourceQuantity(Cell(item3, 0) + " | " + Cell(item3, 1) + " | diameter " + Cell(item3, 3) + " mm; overall length " + Cell(item3, 4) + " mm x " + SteelQtyText(item3, 5), num3, "kg"));
						}
						list.Add(BackupDemand.From(billQuantity2));
					}
				}
			}
			if (flag)
			{
				foreach (OpeningItem item4 in openings.Where((OpeningItem o) => o.Material != "Manual" && o.Material != "Opening"))
				{
					BillQuantity billQuantity3 = new BillQuantity();
					billQuantity3.Section = "Door / window schedule";
					billQuantity3.Description = item4.Id + " | " + item4.Material + " " + item4.Kind + " | " + item4.Description;
					billQuantity3.Unit = "m2";
					billQuantity3.Quantity = (item4.Wood ? (item4.LeafWidth * item4.LeafHeight * (double)item4.Quantity) : item4.Area);
					BillQuantity billQuantity4 = billQuantity3;
					billQuantity4.Sources.Add(SourceQuantity(item4.Id + " | " + (item4.Wood ? item4.LeafWidth : item4.Width).ToString("0.###", CultureInfo.InvariantCulture) + " m x " + (item4.Wood ? item4.LeafHeight : item4.Height).ToString("0.###", CultureInfo.InvariantCulture) + " m x " + item4.Quantity, billQuantity4.Quantity, "m2"));
					list.Add(BackupDemand.From(billQuantity4));
					if (item4.Wood)
					{
						list.Add(BackupDemand.From(new BillQuantity
						{
							Section = "Door / window jambs",
							Description = item4.Id + " | Wooden jamb frames | " + item4.Description,
							Unit = "sets",
							Quantity = item4.Quantity
						}));
					}
				}
			}
			if (flag)
			{
				foreach (BillQuantity item5 in PlumbingQuantities())
				{
					BackupDemand backupDemand2 = BackupDemand.From(item5);
					if (BackupMapping.Unit(backupDemand2.Unit) == "ls" && backupDemand2.Quantity > 0.0)
					{
						backupDemand2.ManualOverride = true;
						backupDemand2.ManualPriority = 1;
					}
					list.Add(backupDemand2);
				}
			}
			if (includeManual)
			{
				list.AddRange(from i in backupBill
					where i.ManualQuantity.HasValue || projectQuantitiesKnown
					select i.ManualDemand() into d
					where d != null
					select d);
			}
			if(flag)list.AddRange(ConcretePayQuantities().Select(BackupDemand.From));
            /* Part A / B quantities filled by the app; a typed BOQ quantity still wins (owner, 19 November 2026) */
            /* owner (19 November 2026, "iallow na lang maginput ng qty sa boq ng app for part A and B"): the owner types Part A / B; the app no longer fills them */
            /* Progress Photographs from the Contract Duration (owner, 23 November 2026) */
            list.AddRange(ProgressPhotoDemands(list));
            if(flag&&FillGeneralRequirements)list.AddRange(GeneralRequirementSuggestions(list.Where(d=>d.Key==null||!d.Key.StartsWith("BOQ manual|",StringComparison.Ordinal)).ToList(),true));
            RouteRequiredQuantities(list);
            ApplyBoqIncludes(list);
            return list;
		}

		private void AddPlumbingWorks()
		{
			plumbingSummary = Grid(new string[3] { "Required plumbing item / Pipe ID / component", "Quantity", "Unit" }, 0);
			plumbingSummary.ReadOnly = true;
			plumbingSummary.AllowUserToAddRows = false;
			plumbingSummary.AllowUserToDeleteRows = false;
			plumbingSummary.Columns[0].Width = 950;
			AddGrid("Plumbing Summary", plumbingSummary, "Automatically linked from the same Tool Kit pipe, manual LS and chamber computations. Each pipe specification totals measured metres. Septic/catch-basin component quantities appear below their parent item and are not added to its LS/each quantity.");
			plumbingPipes = Grid(new string[7] { "Pipe ID", "Location / description", "Diameter / material / series / PN specification", "Measured length m / run", "Runs", "Total length m", "Status" }, 5);
			plumbingPipes.Columns[2].Width = 520;
			SetDefaults(plumbingPipes, new Dictionary<int, string> { { 4, "1" } });
			PipeSuggestions(plumbingPipes, 2, PlumbingSpecs.Pipes);
			AddGrid("Pipe Takeoff", plumbingPipes, "Enter a unique Pipe ID per measured run/group. Select/type the full pipe diameter, material and series/PN. Total metres = measured length x runs. Matching specifications combine in Bill of Quantities and BACKUP; every Pipe ID remains in the Tool Kit report. No fitting equivalent length or waste allowance is assumed.");
			plumbingLs = Grid(new string[5] { "Entry ID", "Location / scope", "Exact lump-sum pay-item description", "Manual quantity LS", "Status" }, 4);
			plumbingLs.Columns[2].Width = 520;
			ConfigureManualEntries(plumbingLs);
			plumbingLs.Visible=false;Controls.Add(plumbingLs);
			plumbingChambers = Grid(new string[35]
			{
				"Structure ID", "Description", "Type", "Outside length m", "Outside width m", "Clear wall height m", "Structure count", "Wall thickness m", "Base thickness m", "RC cover thickness m (0=open)",
				"Access opening length m", "Access opening width m", "Net clear partition length m", "Partition thickness m", "External wall openings m2", "Gravel thickness m", "Working allowance m / side", "Soil cover depth m", "Other RC m3 / structure", "Other forms m2 / structure",
				"Plaster faces (1=inside;2=both)", "Plaster floor?", "Waterproof wall faces (1/2)", "Waterproof floor?", "Summary quantity (LS manual / basin each)", "Excavation m3", "Backfill space m3", "Gravel m3", "Concrete m3", "Grade40 purchase kg",
				"Formworks m2", "CHB m2", "Plaster m2", "Waterproof m2", "Status"
			}, 25);
			AddCombo(plumbingChambers, 2, "Type", new string[4] { "Septic Vault, Concrete", "Septic Vault, CHB", "Catch Basin (CHB)", "Lined Canal" });
			int[] array = new int[2] { 21, 23 };
			foreach (int num in array)
			{
				DataGridViewColumn dataGridViewColumn = plumbingChambers.Columns[num];
				plumbingChambers.Columns.RemoveAt(num);
				plumbingChambers.Columns.Insert(num, new DataGridViewCheckBoxColumn
				{
					Name = ((num == 21) ? "PlasterFloor" : "WaterproofFloor"),
					HeaderText = ((num == 21) ? "Plaster floor?" : "Waterproof floor?"),
					TrueValue = true,
					FalseValue = false
				});
			}
			SetDefaults(plumbingChambers, new Dictionary<int, string>
			{
				{ 2, "Septic Vault, Concrete" },
				{ 6, "1" }
			});
			plumbingChambers.Columns[2].Width = 230;
			plumbingChambers.Visible = false;
			base.Controls.Add(plumbingChambers);
			plumbingRebar = Grid(new string[10] { "Bar ID", "Structure ID", "Component / approved detail", "Diameter mm (Grade40)", "Developed cut length m", "Bars per structure", "Commercial length m", "Net weight kg", "Allocated purchase kg", "Status" }, 7);
			AddCombo(plumbingRebar, 6, "Commercial length m", CommercialBars.Lengths.Select((double x) => x.ToString("0.##", CultureInfo.InvariantCulture)).ToArray());
			SetDefaults(plumbingRebar, new Dictionary<int, string> { { 6, "6" } });
			AddGrid("Plumbing Rebar", plumbingRebar, "Enter the approved developed cut (including hooks/anchorage/laps already in the detail) and bars per structure. Structure count is applied automatically. Grade40 only; compatible offcuts are reused across these plumbing structures. Cuts must fit the chosen stock; segment longer bars only from an approved splice detail. Purchase kg, not stock piece counts, goes into each structure's Tool Kit material breakdown.");
			AddPipeGroups();
			AddStructureMemberInputs();
			AddChamberSheets();
		}

		private static void PipeSuggestions(DataGridView grid, int column, string[] values)
		{
			grid.EditingControlShowing += delegate(object s, DataGridViewEditingControlShowingEventArgs e)
			{
				TextBox textBox = e.Control as TextBox;
				if (textBox != null)
				{
					if (grid.CurrentCell != null && grid.CurrentCell.ColumnIndex == column)
					{
						AutoCompleteStringCollection autoCompleteStringCollection = new AutoCompleteStringCollection();
						autoCompleteStringCollection.AddRange(values);
						textBox.AutoCompleteCustomSource = autoCompleteStringCollection;
						textBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
						textBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
					}
					else
					{
						textBox.AutoCompleteMode = AutoCompleteMode.None;
						textBox.AutoCompleteSource = AutoCompleteSource.None;
					}
				}
			};
		}

		private static bool Checked(DataGridViewRow row, int column)
		{
			if (Cell(row, column) != "")
			{
				return bool.Parse(Cell(row, column));
			}
			return false;
		}

		private void RecalculatePlumbing()
		{
			chamberRevision++;
			pipeMeasures.Clear();
			manualPlumbingLs.Clear();
			chamberMeasures.Clear();
			chamberBarPlan.Clear();
			if (plumbingPipes == null)
			{
				return;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (DataGridViewRow item in (IEnumerable)plumbingPipes.Rows)
			{
				if (item.IsNewRow)
				{
					continue;
				}
				DataGridViewCell dataGridViewCell = item.Cells[5];
				object value = (item.Cells[6].Value = "");
				dataGridViewCell.Value = value;
				if (Empty(item))
				{
					continue;
				}
				try
				{
					FillPipeSpecFromSize(item);
					if (Cell(item, 2) == "") throw new Exception("Select the pipe size.");
					PipeMeasure pipeMeasure = new PipeMeasure
					{
						Id = Cell(item, 0),
						Location = Cell(item, 1),
						Specification = Cell(item, 2),
						Length = Number(item, 3),
						Runs = Count(item, 4)
					};
					if (!hashSet.Add(pipeMeasure.Id))
					{
						throw new Exception("Duplicate Pipe ID.");
					}
					double total = pipeMeasure.Total;
					pipeMeasures.Add(pipeMeasure);
					Measure(item, 5, total, "0.000");
					item.Cells[6].Value = "Estimated";
				}
				catch (Exception ex)
				{
					item.Cells[6].Value = ex.Message;
					errors.Add("Pipe " + Cell(item, 0) + ": " + ex.Message);
				}
			}
			ReadManualBoqEntries();
			hashSet.Clear();
			foreach (DataGridViewRow item2 in (IEnumerable)plumbingChambers.Rows)
			{
				if (item2.IsNewRow)
				{
					continue;
				}
				for (int i = 25; i < 35; i++)
				{
					item2.Cells[i].Value = "";
				}
				if (Empty(item2))
				{
					continue;
				}
				try
				{
					string text = Cell(item2, 2);
					if (text == "Catch Basin (CHB)")
					{
						item2.Cells[24].ReadOnly = true;
						item2.Cells[24].Value = Cell(item2, 6);
					}
					else if (item2.Cells[24].ReadOnly)
					{
						item2.Cells[24].ReadOnly = false;
						item2.Cells[24].Value = "";
					}
					if (text == "Septic Vault, CHB" || text == "Catch Basin (CHB)")
					{
						item2.Cells[7].Value = ((text == "Septic Vault, CHB") ? 0.15 : 0.1);
						item2.Cells[7].ReadOnly = true;
					}
					else
					{
						item2.Cells[7].ReadOnly = false;
					}
					ChamberTakeoff chamberTakeoff = new ChamberTakeoff
					{
						Id = Cell(item2, 0),
						Description = Cell(item2, 1),
						Kind = text,
						L = Number(item2, 3),
						W = Number(item2, 4),
						H = Number(item2, 5),
						Count = Count(item2, 6),
						Wall = Number(item2, 7),
						Base = Number(item2, 8),
						Cover = OptionalNumber(item2, 9),
						OpeningL = OptionalNumber(item2, 10),
						OpeningW = OptionalNumber(item2, 11),
						PartitionLength = OptionalNumber(item2, 12),
						PartitionThickness = OptionalNumber(item2, 13),
						WallOpeningArea = OptionalNumber(item2, 14),
						Gravel = OptionalNumber(item2, 15),
						Allowance = OptionalNumber(item2, 16),
						SoilCover = OptionalNumber(item2, 17),
						OtherConcrete = OptionalNumber(item2, 18),
						OtherForms = OptionalNumber(item2, 19),
						PlasterFaces = Count(item2, 20),
						PlasterFloor = Checked(item2, 21),
						WaterproofFaces = ((!(text == "Catch Basin (CHB)")) ? Count(item2, 22) : 0),
						WaterproofFloor = Checked(item2, 23),
						LsQuantity = ((text == "Catch Basin (CHB)") ? ((double)Count(item2, 6)) : Number(item2, 24))
					};
					chamberTakeoff.Calculate();
					if (text == "Lined Canal")
					{
						item2.Cells[24].Value = chamberTakeoff.LsQuantity;
					}
					if (!hashSet.Add(chamberTakeoff.Id))
					{
						throw new Exception("Duplicate Structure ID.");
					}
					chamberMeasures.Add(chamberTakeoff);
					Measure(item2, 25, chamberTakeoff.Excavation);
					Measure(item2, 26, chamberTakeoff.Backfill);
					Measure(item2, 27, chamberTakeoff.GravelVolume);
					Measure(item2, 28, chamberTakeoff.ConcreteVolume);
					Measure(item2, 30, chamberTakeoff.FormArea);
					Measure(item2, 31, chamberTakeoff.ChbArea);
					Measure(item2, 32, chamberTakeoff.PlasterArea);
					Measure(item2, 33, chamberTakeoff.WaterproofArea);
					item2.Cells[34].Value = "Geometry estimated; enter rebar cut schedule";
				}
				catch (Exception ex2)
				{
					item2.Cells[34].Value = ex2.Message;
					errors.Add("Plumbing structure " + Cell(item2, 0) + ": " + ex2.Message);
				}
			}
			RecalculateStructureMembers();
			hashSet.Clear();
			Dictionary<DataGridViewRow, StockAllocation> dictionary = new Dictionary<DataGridViewRow, StockAllocation>();
			checked
			{
				foreach (DataGridViewRow row in (IEnumerable)plumbingRebar.Rows)
				{
					if (row.IsNewRow)
					{
						continue;
					}
					DataGridViewCell dataGridViewCell2 = row.Cells[7];
					DataGridViewCell dataGridViewCell3 = row.Cells[8];
					object obj2 = (row.Cells[9].Value = "");
					object value = (dataGridViewCell3.Value = obj2);
					dataGridViewCell2.Value = value;
					if (Empty(row))
					{
						continue;
					}
					try
					{
						if (Cell(row, 0) == "" || Cell(row, 2) == "")
						{
							throw new Exception("Enter Bar ID and component/detail.");
						}
						if (!hashSet.Add(Cell(row, 0)))
						{
							throw new Exception("Duplicate Bar ID.");
						}
						ChamberTakeoff chamberTakeoff2 = chamberMeasures.SingleOrDefault((ChamberTakeoff x) => x.Id.Equals(Cell(row, 1), StringComparison.OrdinalIgnoreCase));
						if (chamberTakeoff2 == null)
						{
							throw new Exception("Structure ID not found or structure has invalid dimensions.");
						}
						int num = Count(row, 3);
						if (!RebarDiameters.IsSupported(num))
						{
							throw new Exception(RebarDiameters.Message(num.ToString(CultureInfo.InvariantCulture)));
						}
						double num2 = Number(row, 4);
						double num3 = Number(row, 6);
						int num4 = Count(row, 5);
						SteelWeight.Count(num4);
						if (!CommercialBars.Lengths.Contains(num3) || num2 > num3)
						{
							throw new Exception("Cut must fit the selected supported commercial length.");
						}
						StockDemand stockDemand = new StockDemand
						{
							Grade = 40,
							Diameter = num,
							Cut = num2,
							Pieces = unchecked((long)num4) * unchecked((long)chamberTakeoff2.Count)
						};
						StockChoice stockChoice = CommercialBars.Best(CommercialBars.Choices(num2, stockDemand.Pieces, 0.0, new double[1] { num3 }));
						if (stockChoice == null)
						{
							throw new Exception("Unresolved cut length.");
						}
						StockAllocation stockAllocation = new StockAllocation
						{
							Demand = stockDemand,
							Choice = stockChoice,
							Allowed = new List<double> { num3 },
							UnitMassOverride = SteelWeight.Rod(num, 1.0, 1),
							Entry = new EstimateInput
							{
								Beam = chamberTakeoff2.Id,
								Description = Cell(row, 2)
							}
						};
						chamberBarPlan.Add(stockAllocation);
						dictionary.Add(row, stockAllocation);
					}
					catch (Exception ex3)
					{
						row.Cells[9].Value = ex3.Message;
						errors.Add("Plumbing rebar " + Cell(row, 0) + ": " + ex3.Message);
					}
				}
				RebarOffcuts.Allocate(chamberBarPlan);
				UpdateStructureMemberPurchases();
				foreach (KeyValuePair<DataGridViewRow, StockAllocation> item3 in dictionary)
				{
					Measure(item3.Key, 7, item3.Value.Net);
					Measure(item3.Key, 8, item3.Value.Purchase);
					item3.Key.Cells[9].Value = "Estimated";
				}
				foreach (ChamberTakeoff q in chamberMeasures)
				{
					q.RebarPurchase = chamberBarPlan.Where((StockAllocation a) => a.Entry.Beam == q.Id).Sum((StockAllocation a) => a.Purchase);
					DataGridViewRow dataGridViewRow3 = plumbingChambers.Rows.Cast<DataGridViewRow>().First((DataGridViewRow r) => Cell(r, 0) == q.Id);
					Measure(dataGridViewRow3, 29, q.RebarPurchase);
					if (!chamberBarPlan.Any((StockAllocation a) => a.Entry.Beam == q.Id))
					{
						errors.Add(q.Id + ": enter member reinforcement or approved additional Grade40 cuts before exporting.");
					}
					else
					{
						dataGridViewRow3.Cells[34].Value = (structureMemberErrors.Contains(q.Id) ? "Review member inputs" : (q.ComponentMode ? "Estimated from members and additional cuts" : "Estimated"));
					}
				}
			}
		}

		private List<BillQuantity> PlumbingQuantities()
		{
			List<BillQuantity> list = new List<BillQuantity>();
			foreach (IGrouping<string, PipeMeasure> item in from p in pipeMeasures
				group p by FinishTypeRegistry.Key(p.Specification))
			{
				BillQuantity billQuantity = new BillQuantity
				{
					Section = "Plumbing Works",
					Description = item.First().Specification,
					Unit = "m",
					Quantity = item.Sum((PipeMeasure p) => p.Total)
				};
				billQuantity.Sources.AddRange(item.Select((PipeMeasure p) => new BillQuantity
				{
					Description = "Pipe ID " + p.Id + " | " + p.Location,
					Quantity = p.Total,
					Unit = "m",
					Computation = p.Computation
				}));
				list.Add(billQuantity);
			}
			foreach (IGrouping<string, ChamberTakeoff> item2 in from q in chamberMeasures
				group q by q.Kind)
			{
				BillQuantity billQuantity2 = new BillQuantity
				{
					Section = "Plumbing Works",
					Description = item2.Key,
					Unit = ((item2.Key == "Catch Basin (CHB)") ? "ea" : ((item2.Key == "Lined Canal") ? "m" : "ls")),
					Quantity = item2.Sum((ChamberTakeoff v) => v.LsQuantity)
				};
				foreach (ChamberTakeoff item3 in item2)
				{
					if (item3.LsQuantity > 0.0 || item2.Key == "Catch Basin (CHB)" || item2.Key == "Lined Canal")
					{
						billQuantity2.Sources.Add(new BillQuantity
						{
							Description = item3.Id + " | " + item3.Description,
							Quantity = item3.LsQuantity,
							Unit = billQuantity2.Unit,
							Computation = ((item2.Key == "Catch Basin (CHB)") ? "Computed structure count" : ((item2.Key == "Lined Canal") ? "Computed length x canal count" : "LS quantity is entered in Bill of Quantities")),
							Basis = false
						});
					}
					foreach (BillQuantity item4 in item3.Materials())
					{
						item4.Description = item3.Id + " | " + item4.Description;
						billQuantity2.Sources.Add(item4);
					}
				}
				list.Add(billQuantity2);
			}
			return list;
		}

		private void UpdatePlumbingSummary()
		{
			if (plumbingSummary == null)
			{
				return;
			}
			plumbingSummary.Rows.Clear();
			foreach (BillQuantity item in PlumbingQuantities())
			{
				int index = plumbingSummary.Rows.Add(item.Description, (BackupMapping.Unit(item.Unit) == "ls") ? "Set in BOQ" : ((object)item.Quantity), item.Unit);
				plumbingSummary.Rows[index].DefaultCellStyle.Font = ModernUi.Strong;
				foreach (BillQuantity source in item.Sources)
				{
					index = plumbingSummary.Rows.Add("    " + (source.Basis ? "Material breakdown: " : "") + source.Description, source.Quantity, source.Unit);
					plumbingSummary.Rows[index].DefaultCellStyle.ForeColor = PowTheme.Muted;
				}
			}
		}

		private void AppendPlumbingReport(StringBuilder b)
		{
			b.AppendLine();
			b.AppendLine("PLUMBING WORKS | TOOL KIT COMPUTATION");
			foreach (PipeMeasure pipeMeasure in pipeMeasures)
			{
				b.AppendLine("Pipe ID " + pipeMeasure.Id + " | " + pipeMeasure.Location + " | " + pipeMeasure.Specification + " | " + pipeMeasure.Computation);
			}
			foreach (ChamberTakeoff q in chamberMeasures)
			{
				b.AppendLine();
				b.AppendLine(q.Id + " | " + q.Kind + " | " + q.Description);
				b.AppendLine((q.Kind == "Catch Basin (CHB)") ? ("  Structure count: " + q.Count) : ((q.Kind == "Lined Canal") ? ("  Canal length: " + q.LsQuantity.ToString("0.########", CultureInfo.InvariantCulture) + " m") : "  Final manual LS quantity is entered in Bill of Quantities."));
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  INPUTS (m): outside L={0}, W={1}, clear H={2}, wall t={3}, base t={4}, cover t={5}, structure count={6}", q.L, q.W, q.H, q.Wall, q.Base, q.Cover, q.Count));
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Access opening={0} x {1}m; net clear partition length={2}m, thickness={3}m; external wall openings={4}m2", q.OpeningL, q.OpeningW, q.PartitionLength, q.PartitionThickness, q.WallOpeningArea));
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Gravel t={0}m, working allowance={1}m/side, soil cover={2}m; other RC={3}m3/structure, other forms={4}m2/structure", q.Gravel, q.Allowance, q.SoilCover, q.OtherConcrete, q.OtherForms));
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Plaster wall faces={0}, floor={1}; waterproof wall faces={2}, floor={3}", q.PlasterFaces, q.PlasterFloor, q.WaterproofFaces, q.WaterproofFloor));
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Excavation=({0}+2*{3})*({1}+2*{3})*({2}+{4}+{5}+{6}+{7})*{8} = {9:0.########}m3", q.L, q.W, q.H, q.Allowance, q.Base, q.Cover, q.Gravel, q.SoilCover, q.Count, q.Excavation));
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Backfill={0:0.########} - {1}*{2}*({3}+{4}+{5})*{6} - {7:0.########} = {8:0.########}m3 compacted space", q.Excavation, q.L, q.W, q.H, q.Base, q.Cover, q.Count, q.GravelVolume, q.Backfill));
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Concrete: base {0:0.########} + cover {1:0.########} + RC walls/partitions {2:0.########} + other RC {3:0.########} = {4:0.########}m3", q.BaseConcrete, q.CoverConcrete, q.WallConcrete, q.AdditionalConcrete, q.ConcreteVolume));
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Forms: walls {0:0.########} + base edges {1:0.########} + cover/opening soffit+edges {2:0.########} + other forms {3:0.########} = {4:0.########}m2", q.WallForms, q.BaseForms, q.CoverForms, q.AdditionalForms, q.FormArea));
				AppendStructureMemberReport(b, q);
				foreach (BillQuantity item in q.Materials())
				{
					b.AppendLine(item.Description + ": " + item.Quantity.ToString("0.########", CultureInfo.InvariantCulture) + " " + item.Unit);
					b.AppendLine("  " + item.Computation);
				}
				foreach (StockAllocation item2 in chamberBarPlan.Where((StockAllocation a) => a.Entry.Beam == q.Id))
				{
					b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Grade40 {0}mm | {1}: developed cut {2}m x {3} bars (all structures), unit mass {7:0.########}kg/m; net {4:0.########}kg + allocated offcut {5:0.########}kg = purchase {6:0.########}kg; commercial length {8}m", item2.Demand.Diameter, item2.Entry.Description, item2.Demand.Cut, item2.Demand.Pieces, item2.Net, item2.Waste, item2.Purchase, item2.UnitMass, item2.Choice.Length));
				}
			}
			b.AppendLine("Chamber component quantities appear under their parent septic/catch-basin pay item in Tool Kit reports. They are not added again to unrelated project BOQ totals. Additional RC/forms and cut schedules follow entered approved details; there is no structural or hydraulic sizing inference.");
		}

		private void AddWorksSettings()
		{
			Form[] array = new Form[2] { beam, slab };
			for (int i = 0; i < array.Length; i++)
			{
				foreach (FlowLayoutPanel item in array[i].Controls.OfType<FlowLayoutPanel>())
				{
					Button[] array2 = (from b in item.Controls.OfType<Button>()
						where b.Text == "Settings"
						select b).ToArray();
					for (int num = 0; num < array2.Length; num++)
					{
						array2[num].Visible = false;
					}
				}
			}
			footingSettings = new FlowLayoutPanel
			{
				Visible = false,
				AutoScroll = true,
				Padding = new Padding(12)
			};
			base.Controls.Add(footingSettings);
			footingFc = SettingChoice(footingSettings, "Concrete strength MPa", new string[3] { "21", "28", "35" });
			footingSmall = SettingChoice(footingSettings, "Rebar <=12 mm grade", new string[2] { "40", "60" });
			footingLarge = SettingChoice(footingSettings, "Rebar >=16 mm grade", new string[2] { "60", "40" });
			Button(footingSettings, "Commercial lengths", delegate { var selected=StockLengthPicker.Choose(this,footingStock);if(selected!=null){footingStock=selected;Recalculate();} });
			Button(footingSettings, "Apply to all footings", delegate
			{
				Commit();
				filling = true;
				try
				{
					foreach (DataGridViewRow item2 in (IEnumerable)footings.Rows)
					{
						if (!item2.IsNewRow)
						{
							item2.Cells[25].Value = footingFc.Text;
							item2.Cells[26].Value = footingSmall.Text;
							item2.Cells[27].Value = footingLarge.Text;
						}
					}
				}
				finally
				{
					filling = false;
				}
				Recalculate();
			});
			footingSettings.Controls.Add(new Label
			{
				Text = "Selections are defaults for new footings. Apply to all footings updates existing rows. Saved projects keep their individual materials until you apply changes.",
				Width = 780,
				Height = 60,
				Padding = new Padding(6)
			});
			footings.DefaultValuesNeeded += delegate(object s, DataGridViewRowEventArgs e)
			{
				SetFootingMaterials(e.Row);
			};
			footings.RowsAdded += delegate(object s, DataGridViewRowsAddedEventArgs e)
			{
				if (!filling)
				{
					for (int j = e.RowIndex; j < e.RowIndex + e.RowCount; j++)
					{
						if (!footings.Rows[j].IsNewRow)
						{
							SetFootingMaterials(footings.Rows[j]);
						}
					}
				}
			};
			for (int num2 = 25; num2 <= 27; num2++)
			{
				footings.Columns[num2].Visible = false;
			}
			TabPage tabPage = new TabPage("Reinforced Concrete Start");
			FlowLayoutPanel start = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				FlowDirection = FlowDirection.TopDown,
				WrapContents = false,
				AutoScroll = true,
				Padding = new Padding(20)
			};
			tabPage.Controls.Add(start);
			tabs.TabPages.Add(tabPage);
			start.Controls.Add(new Label
			{
				Text = "REINFORCED CONCRETE WORKS | SETTINGS",
				Width = 1100,
				Height = 52,
				Font = new Font("Segoe UI", 19f, FontStyle.Bold),
				ForeColor = PowTheme.Ink
			});
			start.Controls.Add(new Label
			{
				Text = "Set beam, slab, footing, canopy and RC wall materials and available rebar lengths before estimating.",
				Width = 1100,
				Height = 42,
				ForeColor = PowTheme.Muted
			});
			PowCard card = new PowCard
			{
				Width = 1120,
				Height = 112
			};
			Label value = new Label
			{
				Text = "CONCRETE & REBAR SETTINGS",
				Left = 16,
				Top = 12,
				Width = 900,
				Height = 26,
				Font = new Font("Segoe UI", 11f, FontStyle.Bold)
			};
			card.Controls.Add(value);
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Left = 12,
				Top = 42,
				Width = 1050,
				Height = 76
			};
			Button(flowLayoutPanel, "Beam Settings", delegate
			{
				ShowMemberSettings(0);
			});
			Button(flowLayoutPanel, "Slab Settings", delegate
			{
				ShowMemberSettings(1);
			});
			Button(flowLayoutPanel, "Foundation / Footing Settings", delegate
			{
				ShowMemberSettings(2);
			});
			AddCanopySettings(flowLayoutPanel);
			AddRCWallSettings(flowLayoutPanel);
			FlowLayoutPanel actions = tabs.TabPages.Cast<TabPage>().Single((TabPage p) => p.Text == "Door & Window Schedule").Controls.OfType<FlowLayoutPanel>().Single();
			AddDoorSettings(actions);
			card.Controls.Add(flowLayoutPanel);
			start.Controls.Add(card);
			AddRCCoverDefaults(start);
			RefreshStockChecks();
			start.VisibleChanged += delegate
			{
				if (start.Visible)
				{
					RefreshStockChecks();
				}
			};
		}

		private static ComboBox SettingChoice(FlowLayoutPanel panel, string name, string[] choices)
		{
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Width = 240,
				Height = 76,
				FlowDirection = FlowDirection.TopDown
			};
			flowLayoutPanel.Controls.Add(new Label
			{
				Text = name,
				Width = 235,
				Height = 26
			});
			ComboBox comboBox = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				Width = 220
			};
			comboBox.Items.AddRange(choices);
			comboBox.SelectedIndex = 0;
			flowLayoutPanel.Controls.Add(comboBox);
			panel.Controls.Add(flowLayoutPanel);
			return comboBox;
		}

		private void SetFootingMaterials(DataGridViewRow row)
		{
			row.Cells[25].Value = footingFc.Text;
			row.Cells[26].Value = footingSmall.Text;
			row.Cells[27].Value = footingLarge.Text;
		}

		private void ShowMemberSettings(int member)
		{
			Commit();
			Control[] array = new Control[3] { beam.WorksSettings, slab.WorksSettings, footingSettings };
			string[] array2 = new string[3] { "Beam", "Slab", "Footing" };
			SettingsWorkspace.Show(this, array2[member], new string[1] { array2[member] + " settings" }, new Control[1] { array[member] });
			Recalculate();
			RefreshStockChecks();
		}

		private void ShowWorksSettings()
		{
			Commit();
			SettingsWorkspace.Show(this, "Bill of Quantities", new string[3] { "Beam", "Slab", "Footing" }, new Control[3] { beam.WorksSettings, slab.WorksSettings, footingSettings });
			Recalculate();
			RefreshStockChecks();
		}

        private void RefreshStockChecks() { /* Stock selection belongs to member settings. */ }


		private void WriteMaterialDefaults(XmlElement root)
		{
			WriteDoorSettings(root);
            WriteExtraSettings(root);
			WriteCanopySettings(root);
			WriteRCWallSettings(root);
			WriteRCCovers(root);
			root.SetAttribute("footingFcDefault", footingFc.Text);
			root.SetAttribute("footingSmallDefault", footingSmall.Text);
			root.SetAttribute("footingLargeDefault", footingLarge.Text);
		}

		private void ReadMaterialDefaults(XmlElement root)
		{
			ReadDoorSettings(root);
            ReadExtraSettings(root);
			ReadCanopySettings(root);
			ReadRCWallSettings(root);
			ReadRCCovers(root);
			var array = new[]
			{
				new
				{
					Box = footingFc,
					Name = "footingFcDefault"
				},
				new
				{
					Box = footingSmall,
					Name = "footingSmallDefault"
				},
				new
				{
					Box = footingLarge,
					Name = "footingLargeDefault"
				}
			};
			foreach (var anon in array)
			{
				if (root.HasAttribute(anon.Name))
				{
					string attribute = root.GetAttribute(anon.Name);
					if (!anon.Box.Items.Contains(attribute))
					{
						throw new Exception("Invalid footing material default.");
					}
					anon.Box.SelectedItem = attribute;
				}
			}
		}

		private void FilterFasciaSections(DataGridViewRow row)
		{
			string kind = Cell(row, 2);
			DataGridViewComboBoxCell dataGridViewComboBoxCell = row.Cells[3] as DataGridViewComboBoxCell;
			if (dataGridViewComboBoxCell != null)
			{
				string[] array = (from p in SteelCatalog.Profiles.Concat(projectSteelProfiles.Values)
					where p.Kind == kind
					select p.Name).Distinct().ToArray();
				string text = Cell(row, 3);
				SteelProfile steelProfile = FindSteelProfile(text);
				if (text != "" && !array.Contains(text))
				{
					row.Cells[3].Value = ((steelProfile != null && array.Length != 0) ? array[0] : null);
					text = Cell(row, 3);
				}
				if (!dataGridViewComboBoxCell.Items.Cast<object>().Select(Convert.ToString).SequenceEqual(array))
				{
					dataGridViewComboBoxCell.Items.Clear();
					dataGridViewComboBoxCell.Items.AddRange(array.Cast<object>().ToArray());
				}
				dataGridViewComboBoxCell.ErrorText = "";
			}
		}

		private void CalculatePlatePurchases()
		{
			List<DataGridViewRow> list = (from DataGridViewRow r in steelPlates.Rows
				where r.Tag is PlateDemand
				select r).ToList();
			if (((IEnumerable<DataGridViewRow>)list).Sum((Func<DataGridViewRow, long>)((DataGridViewRow r) => ((PlateDemand)r.Tag).Quantity)) > 5000)
			{
				errors.Add("Steel plate fitting supports up to 5000 cuts per project.");
				return;
			}
			foreach (IGrouping<double, DataGridViewRow> item in from r in list
				group r by ((PlateDemand)r.Tag).ThicknessMm)
			{
				try
				{
					plateStocks.AddRange(PlateCutPlanner.Allocate(item.Select((DataGridViewRow r) => (PlateDemand)r.Tag)));
				}
				catch (Exception ex)
				{
					foreach (DataGridViewRow item2 in item)
					{
						item2.Cells[steelPlates.Columns.Count - 1].Value = ex.Message;
					}
					errors.Add("Steel plate purchasing: " + ex.Message);
				}
			}
			foreach (DataGridViewRow item3 in list)
			{
				PlateDemand demand = (PlateDemand)item3.Tag;
				List<PlateSheet> list2 = plateStocks.Where((PlateSheet s) => s.Cuts.Any((PlatePlacement c) => c.Demand == demand)).ToList();
				if (list2.Count == 0)
				{
					continue;
				}
				double num = list2.Sum((PlateSheet s) => s.BuyKg * s.Cuts.Where((PlatePlacement c) => c.Demand == demand).Sum((PlatePlacement c) => c.Width * c.Length) / s.NetArea);
				item3.Cells[9].Value = list2.Count;
				/* each sheet by its mark, and the other plates cut from its offcut (owner, 23 November 2026: "walang narereuse na steel plates") */
				item3.Cells[10].Value = PlateSheetText(plateStocks, list2, demand);
				Measure(item3, 11, num);
				Measure(item3, 12, Math.Max(0.0, num - demand.Area * (double)demand.Quantity * demand.ThicknessMm / 1000.0 * 7850.0));
			}
		}

		private static BillQuantity SourceQuantity(string name, double value, string unit, bool basis = false)
		{
			return new BillQuantity
			{
				Basis = basis,
				Section = "Source",
				Description = name,
				Quantity = value,
				Unit = unit
			};
		}

		/* a source with the calculation behind it, shown in the Detailed / Extensive reports (owner, 23 November 2026) */
		private static BillQuantity SourceQuantity(string name, double value, string unit, string computation)
		{
			var q = SourceQuantity(name, value, unit);
			q.Computation = computation;
			return q;
		}

		private static List<BillQuantity> MergeSources(IEnumerable<BillQuantity> values)
		{
			return (from v in values
				where Math.Abs(v.Quantity) > 1E-10
				group v by new { v.Description, v.Unit, v.Basis } into g
				select Keep(SourceQuantity(g.Key.Description, g.Sum((BillQuantity v) => v.Quantity), g.Key.Unit, g.Key.Basis), g)).ToList();
		}

		/* merged sources keep every calculation that went into them */
		private static BillQuantity Keep(BillQuantity merged, IEnumerable<BillQuantity> parts)
		{
			var list = parts.ToList();
			/* one line per part, the total on its own line (owner, 24 November 2026: "di ko gusto yung ganyan layout") */
			var texts = list.Where(p => !string.IsNullOrWhiteSpace(p.Computation)).GroupBy(p => p.Computation).Select(g => (g.Count() > 1 ? g.Count() + " × " + g.Key : g.Key)).ToList();
			merged.Computation = texts.Count == 0 ? null : texts.Count == 1 && list.Count == 1 ? texts[0] : string.Join("\n", texts.Select(t => "• " + t)) + (list.Count > 1 ? "\n= total " + BoqComputations.N(merged.Quantity) + " " + merged.Unit : "");
			return merged;
		}

		private static string RebarSource(StockAllocation cut)
		{
			return cut.Entry.Beam + " | " + cut.Entry.Description + (string.IsNullOrWhiteSpace(cut.Entry.ScheduleLabel) ? "" : (" | " + cut.Entry.ScheduleLabel));
		}

		private List<BillQuantity> TableSources(object[,] data, int quantityColumn, string unit, string prefix, string[] formulas = null)
		{
			List<BillQuantity> list = new List<BillQuantity>();
			for (int i = 0; i < data.GetLength(0); i++)
			{
				list.Add(SourceQuantity(prefix + Convert.ToString(data[i, 1]) + " | " + Convert.ToString(data[i, 0]), Convert.ToDouble(data[i, quantityColumn]), unit, formulas != null && i < formulas.Length ? formulas[i] : null));
			}
			return MergeSources(list);
		}

		private List<BillQuantity> FixedQuantitySources(string key)
		{
			if (key == "BeamConcrete" || key == "BeamForms")
			{
				return TableSources(beam.ConcreteExcelRows(), (key == "BeamConcrete") ? 4 : 7, (key == "BeamConcrete") ? "m3" : "m2", "Beam ", beam.ConcreteFormulas(key == "BeamConcrete"));
			}
			if (key == "SlabConcrete" || key == "SlabForms")
			{
				return TableSources(slab.WorksMeasureRows(), (key == "SlabConcrete") ? 4 : 7, (key == "SlabConcrete") ? "m3" : "m2", "Slab ");
			}
			if (key == "RCWallConcrete" || key == "RCWallForms")
			{
				return MergeSources(rcWallResults.Select((RCWallTakeoff q) => SourceQuantity("RC wall " + q.Id + " | " + q.Description, (key == "RCWallConcrete") ? q.Concrete : q.Forms, (key == "RCWallConcrete") ? "m3" : "m2", (key == "RCWallConcrete") ? BoqComputations.RcWallConcrete(q) : BoqComputations.RcWallForms(q))));
			}
			if (key == "FootingConcrete" || key == "FootingForms")
			{
				return MergeSources(footingResults.Select((FootingTakeoff q) => SourceQuantity("Footing " + q.Id + " | " + q.Description, (key == "FootingConcrete") ? q.Concrete : q.Forms, (key == "FootingConcrete") ? "m3" : "m2", (key == "FootingConcrete") ? BoqComputations.FootingConcrete(q) : BoqComputations.FootingForms(q))));
			}
			if (key == "ConcreteTotal" || key == "FormsTotal")
			{
				bool concrete = key == "ConcreteTotal";
				return MergeSources(FixedQuantitySources(concrete ? "BeamConcrete" : "BeamForms").Concat(FixedQuantitySources(concrete ? "SlabConcrete" : "SlabForms")).Concat(FixedQuantitySources(concrete ? "FootingConcrete" : "FootingForms")).Concat(FixedQuantitySources(concrete ? "RCWallConcrete" : "RCWallForms"))
					.Concat(canopyResults.Select((CanopyTakeoff q) => SourceQuantity("Canopy / ledge " + q.Id + " | " + q.Description, concrete ? q.Concrete : q.Forms, concrete ? "m3" : "m2", concrete ? BoqComputations.CanopyConcrete(q) : BoqComputations.CanopyForms(q)))).Concat(miscQuantities.Select(q=>SourceQuantity(q.Kind+" "+q.Id+" | "+q.Description,concrete?q.Concrete:q.Forms,concrete?"m3":"m2"))).Concat(wallFootingResults.Select(q=>SourceQuantity("Wall footing "+q.Id+" | "+q.Description,concrete?q.Concrete:q.Forms,concrete?"m3":"m2"))));
			}
			if (key == "Grade40" || key == "Grade60")
			{
				int grade = ((key == "Grade40") ? 40 : 60);
				return MergeSources(from a in projectRebarPlan
					where a.Demand.Grade == grade
					select SourceQuantity(RebarSource(a) + " | " + a.Demand.Diameter + "mm", a.Purchase, "kg", BoqComputations.Rebar(a)));
			}
			if (key == "WoodPaint")
			{
				return MergeSources(from o in openings
					where o.Wood
					select SourceQuantity(o.Id + " | " + o.Description, o.LeafPaint + o.JambPaint, "m2"));
			}
			if (key == "Leversets" || key == "Hinges")
			{
				return MergeSources(from o in openings
					where o.Material != "Manual" && o.Material != "Opening"
					select SourceQuantity(o.Id + " | " + o.Description, (key == "Hinges") ? o.Hinges : o.Locks, "pcs"));
			}
			if (key == "Plaster" || key == "MasonryPaint")
			{
				return MergeSources(((key == "Plaster") ? plasterResults : paintResults).Select((KeyValuePair<string, PlasterTakeoff> p) => SourceQuantity(p.Key, p.Value.Net, "m2", BoqComputations.PlasterText(p.Value, WallSides(p.Key, key == "Plaster")))));
			}
			string category = ((key == "WallTiles") ? "Wall" : ((key == "CeilingFinishes") ? "Ceiling" : ((key == "FloorFinishes") ? "Floor" : ((key == "OtherFinishes") ? "Other" : ""))));
			return MergeSources(from f in finishItems
				where f.Category == category && (key != "WallTiles" || f.Kind == "Tiles")
				select SourceQuantity(f.Id + " | " + f.Description, f.Total(FindWall(f.Wall), wallOpenings, finishItems), "m2"));
		}

		private List<BillQuantity> MasonryQuantitySources(int index)
		{
			return MergeSources(((index < 2) ? RegularWalls.Where((WallItem w) => w.ThicknessMm == (double)((index == 0) ? 100 : 150)) : wallItems.Where((WallItem w) => w.Material == MasonryTotalNames[index - 2])).Select((WallItem w) => SourceQuantity(w.Id + " | " + w.Description, w.Net(wallOpenings), "m2", BoqComputations.WallNet(w, wallOpenings))));
		}

		/* plaster / paint sides of a wall for the report */
		private string WallSides(string id, bool plaster)
		{
			var w = wallItems.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
			return w == null ? "" : plaster ? w.Plaster : (w.Paint == "Same as plaster" ? w.Plaster : w.Paint);
		}

		private List<BillQuantity> GravelQuantitySources(int index)
		{
			List<BillQuantity> list = TableSources(beam.EarthworkExcelRows(), 10, "m3", "Beam ");
			IEnumerable<BillQuantity> enumerable = footingResults.Select((FootingTakeoff q) => SourceQuantity("Footing " + q.Id + " | " + q.Description, q.Gravel, "m3", BoqComputations.FootingGravel(q))).Concat(wallFootingResults.Select(q=>SourceQuantity("Wall footing "+q.Id+" | "+q.Description,q.Gravel,"m3")));
			IEnumerable<BillQuantity> enumerable2 = ((gravelEntries == null) ? Enumerable.Empty<BillQuantity>() : (from DataGridViewRow r in gravelEntries.Rows
				where r.Tag is double
				select SourceQuantity(Cell(r, 0) + " | " + Cell(r, 1), (double)r.Tag, "m3")));
			object source;
			if (index != 0)
			{
				source = ((index == 1) ? enumerable : ((index == 2) ? enumerable2 : list.Concat(enumerable).Concat(enumerable2)));
			}
			else
			{
				IEnumerable<BillQuantity> enumerable3 = list;
				source = enumerable3;
			}
			double allowance = (index == 4) ? (1.0 + (double)gravelAllowance.Value / 100.0) : 1.0;
			return MergeSources(((IEnumerable<BillQuantity>)source).Select((BillQuantity q) => SourceQuantity(q.Description, q.Quantity * allowance, q.Unit, (q.Computation ?? "") + (index == 4 ? (string.IsNullOrEmpty(q.Computation) ? "" : "; ") + BoqComputations.N(q.Quantity) + " × (1 + " + BoqComputations.N((double)gravelAllowance.Value) + "% allowance) = " + BoqComputations.N(q.Quantity * allowance) + " m³" : ""))));
		}

		private List<BillQuantity> EarthQuantitySources(int index)
		{
			if (index == 0 || index == 1)
			{
				return MergeSources(earthSources.Select((EarthworkMaterial q) => SourceQuantity(q.Source + " | " + q.Id, (index == 0) ? q.Bank : q.Fill, "m3", EarthText(q, index == 0))));
			}
			if (index == 3)
			{
				return new List<BillQuantity> { SourceQuantity("External common borrow for the remaining compacted-space deficit", earthBalance.LooseCommonBorrow, "m3") };
			}
			return MergeSources(earthBalance.Sources.Select((EarthworkSourceUse q) => SourceQuantity(q.Material.Source + " | " + q.Material.Id, (index == 2) ? q.LooseUsed : ((index == 4) ? q.LooseSurplus : q.UnsuitableLoose), "m3", BoqComputations.Earth(q.Material))));
		}

		/* footing excavation / backfill with their formula; other sources with their bank volume and soil factors */
		private string EarthText(EarthworkMaterial q, bool bank)
		{
			var f = q.Source == "Footing" ? footingResults.FirstOrDefault(x => x.Soil == q) : null;
			if (f != null) return bank ? BoqComputations.FootingExcavation(f) : BoqComputations.FootingBackfill(f);
			return bank ? "bank (in-place) volume " + BoqComputations.N(q.Bank) + " m³" : "space to fill after compaction " + BoqComputations.N(q.Fill) + " m³";
		}

		private List<BillQuantity> MouldingQuantitySources(int index)
		{
			return MergeSources(from o in openings
				where o.Material != "Manual" && o.Exterior && (index == 2 || o.Kind == ((index == 0) ? "Door" : "Window"))
				select SourceQuantity(o.Id + " | " + o.Description, o.MouldingLength, "lm"));
		}

		private List<BillQuantity> CanopyQuantitySources(int index)
		{
			if (index == 4)
			{
				return MergeSources(canopyPlan.Select((StockAllocation a) => SourceQuantity(RebarSource(a), a.Purchase, "kg", BoqComputations.Rebar(a))));
			}
			return MergeSources(canopyResults.Select((CanopyTakeoff q) => SourceQuantity(q.Id + " | " + q.Description, (index == 0) ? q.Concrete : ((index == 1) ? q.SoffitForms : ((index == 2) ? q.EdgeForms : q.Forms)), (index == 0) ? "m3" : "m2")));
		}

		private List<BillQuantity> GridWeightSources(DataGridView grid, int column)
		{
			return MergeSources(from DataGridViewRow r in grid.Rows
				where !r.IsNewRow && Cell(r, grid.Columns.Count - 1) == "Estimated"
				select SourceQuantity(Cell(r, 0) + " | " + Cell(r, 1), Convert.ToDouble(r.Cells[column].Value, CultureInfo.InvariantCulture), "kg", RowInputs(grid, r, column)));
		}

		/* the inputs of a steel row and the column the weight is read from, as the basis of its quantity */
		private static string RowInputs(DataGridView grid, DataGridViewRow r, int column)
		{
			var parts = new List<string>();
			for (int c = 2; c < Math.Min(column, grid.Columns.Count); c++)
			{
				if (!grid.Columns[c].Visible) continue;
				string v = Convert.ToString(r.Cells[c].FormattedValue);
				if (string.IsNullOrWhiteSpace(v)) continue;
				parts.Add(grid.Columns[c].HeaderText.Replace("\r", " ").Replace("\n", " ").Trim() + " " + v);
			}
			return string.Join("; ", parts) + " → " + grid.Columns[column].HeaderText.Replace("\r", " ").Replace("\n", " ").Trim() + " " + Convert.ToString(r.Cells[column].FormattedValue);
		}

		private List<BillQuantity> StockPurchaseSources(IEnumerable<SteelStock> stocks)
		{
			return MergeSources(stocks.SelectMany((SteelStock stock) => stock.Cuts.Select((SteelCut cut) => SourceQuantity(cut.Demand.Member + " | " + stock.Specification, 6.0 * stock.KgM * cut.Demand.Length / stock.Net, "kg", "6 m stock × " + BoqComputations.N(stock.KgM) + " kg/m = " + BoqComputations.N(6.0 * stock.KgM) + " kg, shared by the cut length: × " + BoqComputations.N(cut.Demand.Length) + " / " + BoqComputations.N(stock.Net) + " m used = " + BoqComputations.N(6.0 * stock.KgM * cut.Demand.Length / stock.Net) + " kg"))));
		}

		private List<BillQuantity> SteelQuantitySources(string name)
		{
			if (name == "Purlins PURCHASE weight")
			{
				return GridWeightSources(steelPurlins, 7);
			}
			if (name == "Angle bars PURCHASE weight")
			{
				return GridWeightSources(steelAngles, 7);
			}
			if (name == "Metal fascia PURCHASE weight")
			{
				return GridWeightSources(fasciaFrame, 10);
			}
			if (name == "Machine bolts shaft-only")
			{
				return GridWeightSources(machineBolts, 7);
			}
			if (name == "Anchor bolts shaft-only")
			{
				return GridWeightSources(anchorBolts, 7);
			}
			if (name == "Steel plates")
			{
				return GridWeightSources(steelPlates, 8);
			}
			if (name == "Crossbracing rods")
			{
				return GridWeightSources(crossBracing, 9);
			}
			if (name == "Steel plates PURCHASE weight")
			{
				return GridWeightSources(steelPlates, 11);
			}
			if (name == "Steel plates final offcuts")
			{
				return GridWeightSources(steelPlates, 12);
			}
			if (name == "6m stock PURCHASE weight")
			{
				return StockPurchaseSources(steelStocks);
			}
			if (name == "6m stock bars to buy (purlin/angle/fascia)")
			{
				return (from s in steelStocks
					group s by s.Specification into g
					select SourceQuantity(g.Key, g.Count(), "pcs")).ToList();
			}
			if (name == "Final stock leftovers" || name == "Final stock leftover weight")
			{
				return (from s in steelStocks
					group s by s.Specification into g
					select SourceQuantity(g.Key, g.Sum((SteelStock s) => s.Remaining * ((name == "Final stock leftovers") ? 1.0 : s.KgM)), (name == "Final stock leftovers") ? "m" : "kg")).ToList();
			}
			IEnumerable<BillQuantity> second = GridWeightSources(machineBolts, 7).Concat(GridWeightSources(anchorBolts, 7)).Concat(GridWeightSources(steelPlates, 8)).Concat(GridWeightSources(crossBracing, 9));
			if (name.StartsWith("TOTAL installed"))
			{
				return MergeSources(GridWeightSources(steelPurlins, 7).Concat(GridWeightSources(steelAngles, 7)).Concat(GridWeightSources(fasciaFrame, 10)).Concat(second));
			}
			if (name.StartsWith("TOTAL purchase"))
			{
				return MergeSources(StockPurchaseSources(steelStocks).Concat(GridWeightSources(machineBolts, 7)).Concat(GridWeightSources(anchorBolts, 7)).Concat(GridWeightSources(steelPlates, 11))
					.Concat(GridWeightSources(crossBracing, 9)));
			}
			if (name == SteelPaintingTotal || name == SteelPaintingTotalLabel)
			{
				return steelPaintDetails.ToList();
			}
			if (name.StartsWith("Metal painting - "))
			{
				return steelPaintDetails.Where((BillQuantity d) => d.Description.StartsWith(name.Substring(17).Split(' ')[0], StringComparison.OrdinalIgnoreCase)).ToList();
			}
			if (name.StartsWith("Turnbuckles dia "))
			{
				return MergeSources(from DataGridViewRow r in crossBracing.Rows
					where !r.IsNewRow && Cell(r, crossBracing.Columns.Count - 1) == "Estimated" && name == "Turnbuckles dia " + Number(r, 5).ToString("0.###", CultureInfo.InvariantCulture) + "mm"
					select SourceQuantity(Cell(r, 0) + " | " + Cell(r, 1), SteelQty(r, 6), "pcs"));
			}
			return new List<BillQuantity>();
		}

		private List<BillQuantity> RoofingQuantitySources(string name)
		{
            var specification=RoofSheetPayQuantities().FirstOrDefault(q=>q.Description==name);if(specification!=null)return specification.Sources;
			List<BillQuantity> list = new List<BillQuantity>();
			foreach (DataGridViewRow item in (IEnumerable)roofSheets.Rows)
			{
				RoofSheetQuantity roofSheetQuantity = item.Tag as RoofSheetQuantity;
				if (roofSheetQuantity != null)
				{
					if (name == RoofFamily(item) + " to buy")
					{
						list.Add(SourceQuantity(Cell(item, 0) + " | " + Cell(item, 1), roofSheetQuantity.BuyArea, "m2"));
					}
					if (roofSheetQuantity.Insulation > 0.0 && (name.StartsWith(Cell(item, 11)) || name == "Insulation total (net roof area)"))
					{
						list.Add(SourceQuantity(Cell(item, 0) + " | " + Cell(item, 1) + ((name.EndsWith("net area") || name == "Insulation total (net roof area)") ? "" : " | net-area basis before whole-roll rounding"), roofSheetQuantity.Insulation, "m2", name.EndsWith("rolls to buy") || name.EndsWith("purchase coverage")));
					}
				}
			}
			foreach (DataGridViewRow item2 in (IEnumerable)roofAccessories.Rows)
			{
				RoofAccessoryQuantity roofAccessoryQuantity = item2.Tag as RoofAccessoryQuantity;
				if (roofAccessoryQuantity != null)
				{
					string text = Cell(item2, 2);
					if (name == text || name == text + " pieces" || name == text + " sheet area")
					{
						list.Add(SourceQuantity(Cell(item2, 0) + " | " + Cell(item2, 1), (name == text) ? roofAccessoryQuantity.BuyLength : ((name == text + " pieces") ? ((double)roofAccessoryQuantity.Pieces) : roofAccessoryQuantity.BuyArea), (name == text) ? "m" : ((name == text + " pieces") ? "pcs" : "m2")));
					}
				}
			}
			return MergeSources(list);
		}

		private void AddCanopyEstimates()
		{
			canopies = Grid(new string[26]
			{
				"Canopy / ledge ID", "Description / opening reference", "Length along wall m", "Projection / width m", "Thickness mm", "Qty", "Cover mm", "Main dia mm", "Main spacing mm", "Main count / unit",
				"Distribution dia mm", "Distribution spacing mm", "Distribution count / unit", "Main wall anchorage mm", "Distribution extra ends mm (total)", "Also bottom mat", "Concrete MPa", "Grade <=12 mm", "Grade >=16 mm", "Concrete m3",
				"Soffit forms m2", "3-edge side forms m2", "Total forms m2", "Net rebar kg", "Rebar BUY kg", "Status"
			}, 19);
			canopies.Columns.RemoveAt(15);
			canopies.Columns.Insert(15, new DataGridViewCheckBoxColumn
			{
				Name = "BottomMat",
				HeaderText = "Also bottom mat",
				Width = 125
			});
			for (int i = 16; i <= 18; i++)
			{
				canopies.Columns[i].Visible = false;
			}
			SetDefaults(canopies, new Dictionary<int, string>
			{
				{ 4, "150" },
				{ 5, "1" },
				{ 6, "25" },
				{ 7, "10" },
				{ 8, "150" },
				{ 10, "10" },
				{ 11, "150" },
				{ 14, "0" },
				{ 16, "21" },
				{ 17, "40" },
				{ 18, "60" }
			});
			canopies.CurrentCellDirtyStateChanged += delegate
			{
				if (canopies.IsCurrentCellDirty && canopies.CurrentCell is DataGridViewCheckBoxCell)
				{
					canopies.CommitEdit(DataGridViewDataErrorContexts.Commit);
				}
			};
			canopies.CellValueChanged += delegate(object s, DataGridViewCellEventArgs e)
			{
				if (!filling && e.RowIndex >= 0 && e.ColumnIndex == 15)
				{
					Recalculate();
				}
			};
			AddGrid("Canopy / Ledge Estimate", canopies, "Wall-attached rectangular canopy/ledge takeoff for doors/windows. Enter L, projection, thickness, quantity and approved rebar details. Soffit + front and 2 end forms; attached wall edge/top excluded. Enter main-bar wall anchorage from the drawings. Spacing OR count per direction. Top mat standard; bottom mat optional. Cuts round UP 25mm. Not a structural design check.");
		}

		private void AddCanopySettings(FlowLayoutPanel actions)
		{
			canopySettings = new FlowLayoutPanel
			{
				Visible = false,
				AutoScroll = true,
				Padding = new Padding(12)
			};
			base.Controls.Add(canopySettings);
			canopyFc = SettingChoice(canopySettings, "Canopy concrete MPa", new string[3] { "21", "28", "35" });
			canopySmall = SettingChoice(canopySettings, "Rebar <=12 mm grade", new string[2] { "40", "60" });
			canopyLarge = SettingChoice(canopySettings, "Rebar >=16 mm grade", new string[2] { "60", "40" });
			Button(canopySettings, "Available commercial lengths", delegate
			{
				List<double> list = StockLengthPicker.Choose(this, canopyStock);
				if (list != null)
				{
					canopyStock = list;
					Recalculate();
				}
			});
			Button(canopySettings, "Apply to all canopies / ledges", delegate
			{
				Commit();
				filling = true;
				try
				{
					foreach (DataGridViewRow item in (IEnumerable)canopies.Rows)
					{
						if (!item.IsNewRow)
						{
							CanopyMaterials(item);
						}
					}
				}
				finally
				{
					filling = false;
				}
				Recalculate();
			});
			Button(actions, "Canopy / Ledge Settings", delegate
			{
				SettingsWorkspace.Show(this, "Canopy / Ledge", new string[1] { "Materials & commercial lengths" }, new Control[1] { canopySettings });
			});
			canopies.DefaultValuesNeeded += delegate(object s, DataGridViewRowEventArgs e)
			{
				CanopyMaterials(e.Row);
			};
			canopies.RowsAdded += delegate(object s, DataGridViewRowsAddedEventArgs e)
			{
				if (!filling)
				{
					for (int i = e.RowIndex; i < e.RowIndex + e.RowCount; i++)
					{
						if (!canopies.Rows[i].IsNewRow)
						{
							CanopyMaterials(canopies.Rows[i]);
						}
					}
				}
			};
		}

		private void CanopyMaterials(DataGridViewRow row)
		{
			row.Cells[16].Value = canopyFc.Text;
			row.Cells[17].Value = canopySmall.Text;
			row.Cells[18].Value = canopyLarge.Text;
		}

		private void RecalculateCanopies()
		{
			canopyResults.Clear();
			canopyPlan.Clear();
			if (canopies == null)
			{
				return;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (DataGridViewRow item in (IEnumerable)canopies.Rows)
			{
				if (item.IsNewRow)
				{
					continue;
				}
				item.Tag = null;
				for (int i = 19; i <= 25; i++)
				{
					item.Cells[i].Value = "";
				}
				if (Empty(item))
				{
					continue;
				}
				try
				{
					int num = Count(item, 17);
					int num2 = Count(item, 18);
					LengthConfiguration lengthConfiguration = CalculationStore.Resolve(Number(item, 16), num, (num == 40) ? 275 : 420, num2, (num2 == 40) ? 275 : 420);
					CanopyTakeoff canopyTakeoff = CanopyTakeoff.Calculate(Cell(item, 0), Cell(item, 1), Number(item, 2), Number(item, 3), Number(item, 4), Count(item, 5), Number(item, 6), new FootingBarSpec
					{
						Diameter = Count(item, 7),
						Spacing = OptionalNumber(item, 8),
						Count = ((!(Cell(item, 9) == "")) ? Count(item, 9) : 0)
					}, new FootingBarSpec
					{
						Diameter = Count(item, 10),
						Spacing = OptionalNumber(item, 11),
						Count = ((!(Cell(item, 12) == "")) ? Count(item, 12) : 0)
					}, Number(item, 13), OptionalNumber(item, 14), Cell(item, 15) != "" && bool.Parse(Cell(item, 15)), lengthConfiguration);
					if (!hashSet.Add(canopyTakeoff.Id))
					{
						throw new Exception("Duplicate canopy / ledge ID.");
					}
					item.Tag = canopyTakeoff;
					canopyResults.Add(canopyTakeoff);
					canopyPlan.AddRange(CommercialBars.Allocate(new EstimateInput[1] { canopyTakeoff.Steel }, lengthConfiguration, canopyStock));
					item.Cells[19].Value = canopyTakeoff.Concrete;
					item.Cells[20].Value = canopyTakeoff.SoffitForms;
					item.Cells[21].Value = canopyTakeoff.EdgeForms;
					item.Cells[22].Value = canopyTakeoff.Forms;
					item.Cells[23].Value = canopyTakeoff.Steel.Result.Lines.Sum((EstimateLine l) => l.Kg);
					item.Cells[25].Value = "Estimated";
				}
				catch (Exception ex)
				{
					item.Cells[25].Value = ex.Message;
					errors.Add("Canopy / ledge " + Cell(item, 0) + ": " + ex.Message);
				}
			}
		}

		private void UpdateCanopyPurchases()
		{
			if (canopies == null)
			{
				return;
			}
			foreach (DataGridViewRow item in (IEnumerable)canopies.Rows)
			{
				CanopyTakeoff q = item.Tag as CanopyTakeoff;
				if (q != null)
				{
					List<StockAllocation> source = canopyPlan.Where((StockAllocation a) => a.Entry == q.Steel).ToList();
					item.Cells[24].Value = source.Sum((StockAllocation a) => a.Purchase);
					if (source.Any((StockAllocation a) => a.Choice == null))
					{
						item.Cells[25].Value = "Unresolved commercial rebar cut";
						errors.Add("Canopy / ledge " + q.Id + ": unresolved commercial rebar cut.");
					}
				}
			}
		}

		private List<object[]> CanopySummary()
		{
			List<object[]> list = new List<object[]>();
			list.Add(new object[3]
			{
				"Canopy / ledge concrete",
				canopyResults.Sum((CanopyTakeoff q) => q.Concrete),
				"m3"
			});
			list.Add(new object[3]
			{
				"Canopy / ledge soffit formwork",
				canopyResults.Sum((CanopyTakeoff q) => q.SoffitForms),
				"m2"
			});
			list.Add(new object[3]
			{
				"Canopy / ledge exposed side formwork",
				canopyResults.Sum((CanopyTakeoff q) => q.EdgeForms),
				"m2"
			});
			list.Add(new object[3]
			{
				"TOTAL canopy / ledge formwork",
				canopyResults.Sum((CanopyTakeoff q) => q.Forms),
				"m2"
			});
			list.Add(new object[3]
			{
				"Canopy / ledge rebar TO BUY (allocated)",
				canopyPlan.Sum((StockAllocation a) => a.Purchase),
				"kg"
			});
			return list;
		}

		private void AppendCanopyReport(StringBuilder b)
		{
			b.AppendLine();
			b.AppendLine("CANOPY / LEDGE TAKEOFF | DOORS & WINDOWS");
			foreach (object[] item in CanopySummary())
			{
				string[] array = new string[5];
				object obj = item[0];
				array[0] = ((obj != null) ? obj.ToString() : null);
				array[1] = ": ";
				array[2] = Convert.ToDouble(item[1]).ToString("0.00", CultureInfo.InvariantCulture);
				array[3] = " ";
				object obj2 = item[2];
				array[4] = ((obj2 != null) ? obj2.ToString() : null);
				b.AppendLine(string.Concat(array));
			}
			foreach (CanopyTakeoff q in canopyResults)
			{
				b.AppendLine(q.Id + " | " + q.Description + " | concrete " + q.Fc + " MPa");
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Concrete L*B*T*qty = {0}*{1}*{2}*{3} = {4:0.000}m3", q.Length, q.Projection, q.Thickness, q.Quantity, q.Concrete));
				b.AppendLine("  Soffit L*B*qty = " + q.SoffitForms.ToString("0.00", CultureInfo.InvariantCulture) + "m2; sides (L+2B)*T*qty = " + q.EdgeForms.ToString("0.00", CultureInfo.InvariantCulture) + "m2.");
				PurchaseReports.Entry(b, canopyPlan.Where((StockAllocation a) => a.Entry == q.Steel).ToList(), q.Quantity, true);
			}
			b.AppendLine("No top or attached rear-wall forms, excavation, slope, haunch, structural capacity or anchorage design is inferred. Anchorage is entered from approved details; avoid double-counting any separately entered slab.");
		}

		private void WriteCanopySettings(XmlElement root)
		{
			root.SetAttribute("canopyStock", CommercialBars.FormatLengths(canopyStock));
			root.SetAttribute("canopyFc", canopyFc.Text);
			root.SetAttribute("canopySmall", canopySmall.Text);
			root.SetAttribute("canopyLarge", canopyLarge.Text);
		}

		private void ReadCanopySettings(XmlElement root)
		{
			canopyStock = CommercialBars.ParseLengths(root.GetAttribute("canopyStock"));
			var array = new[]
			{
				new
				{
					Box = canopyFc,
					Key = "canopyFc",
					Fallback = "21"
				},
				new
				{
					Box = canopySmall,
					Key = "canopySmall",
					Fallback = "40"
				},
				new
				{
					Box = canopyLarge,
					Key = "canopyLarge",
					Fallback = "60"
				}
			};
			foreach (var anon in array)
			{
				string text = (root.HasAttribute(anon.Key) ? root.GetAttribute(anon.Key) : anon.Fallback);
				if (!anon.Box.Items.Contains(text))
				{
					throw new Exception("Invalid saved canopy material setting.");
				}
				anon.Box.SelectedItem = text;
			}
		}

		private void AddInsulationSettings()
		{
			insulationRolls = Grid(new string[4] { "Insulation type", "Roll width m", "Roll length m", "Roll area m2" }, 3);
			insulationRolls.AllowUserToAddRows = false;
			insulationRolls.AllowUserToDeleteRows = false;
			insulationRolls.Columns[0].ReadOnly = true;
			insulationRolls.Columns[0].Width = 570;
			string[] insulationTypes = InsulationTypes;
			foreach (string text in insulationTypes)
			{
				insulationRolls.Rows.Add(text, "1", "50", ""); /* supplier roll 1 m x 50 m by default (owner, 21 November 2026: "gawin roll width 1 m length 50m") */
			}
			TabPage tabPage = tabs.TabPages.Cast<TabPage>().First((TabPage p) => p.Text == "Roofing Start");
			TabPage tabPage2 = new TabPage("Roof profiles");
			tabPage.Controls.Remove(roofProfiles);
			tabPage2.Controls.Add(roofProfiles);
			TabPage tabPage3 = new TabPage("Insulation roll settings")
			{
				Controls = { (Control)insulationRolls }
			};
			TabControl tabControl = new NavigationTabs
			{
				Dock = DockStyle.Fill
			};
			tabControl.TabPages.Add(tabPage2);
			tabControl.TabPages.Add(tabPage3);
			tabPage.Controls.Add(tabControl);
			tabControl.BringToFront();
			Label value = new Label
			{
				Dock = DockStyle.Top,
				Height = 60,
				Padding = new Padding(10),
				Text = "Enter supplier roll WIDTH and LENGTH in metres for each insulation type used. Whole rolls are rounded up after pooling the net roof area of the same type. No overlap/waste factor is assumed."
			};
			tabPage3.Controls.Add(value);
			TabPage tabPage4 = new TabPage("Flashing & gutter settings");
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				Padding = new Padding(20),
				FlowDirection = FlowDirection.TopDown
			};
			flowLayoutPanel.Controls.Add(new Label
			{
				Text = "Commercial piece length: 2.44 m (fixed)",
				Width = 700,
				Height = 35
			});
			flashingLap = AddAccessoryLap(flowLayoutPanel, "Flashing lap (mm)", "Flashing");
			gutterLap = AddAccessoryLap(flowLayoutPanel, "Gutter lap (mm)", "Gutter");
			ridgeLap = AddAccessoryLap(flowLayoutPanel, "Ridge roll lap (mm)", "Ridge Roll");
			flowLayoutPanel.Controls.Add(new Label
			{
				Text = "Defaults: flashing 100 mm; gutter 150 mm; ridge roll 300 mm. Confirm the approved detail for each project.",
				Width = 730,
				Height = 65
			});
			tabPage4.Controls.Add(flowLayoutPanel);
			tabControl.TabPages.Add(tabPage4);
			AddCombo(roofSheets, 11, "Insulation type", InsulationTypes);
			roofSheets.Columns[11].Width = 310;
		}

		private NumericUpDown AddAccessoryLap(FlowLayoutPanel panel, string caption, string kind)
		{
			panel.Controls.Add(new Label
			{
				Text = caption,
				Width = 700,
				Height = 30
			});
			NumericUpDown numericUpDown = new NumericUpDown
			{
				Minimum = 0m,
				Maximum = 1000m,
				Value = RoofAccessoryDefaults.LapMm(kind),
				DecimalPlaces = 0,
				Width = 220
			};
			numericUpDown.ValueChanged += delegate
			{
				if (!filling)
				{
					Recalculate();
				}
			};
			panel.Controls.Add(numericUpDown);
			return numericUpDown;
		}

		private decimal AccessoryLapMm(string kind)
		{
			NumericUpDown numericUpDown = ((kind == "Gutter") ? gutterLap : ((kind == "Ridge Roll") ? ridgeLap : flashingLap));
			if (numericUpDown != null)
			{
				return numericUpDown.Value;
			}
			return RoofAccessoryDefaults.LapMm(kind);
		}

		private List<object[]> InsulationSummary()
		{
			List<object[]> list = new List<object[]>();
			for (int i = 0; i < InsulationTypes.Length; i++)
			{
				string type = InsulationTypes[i];
				double num = (from DataGridViewRow r in roofSheets.Rows
					where r.Tag is RoofSheetQuantity && Cell(r, 11) == type
					select r).Sum((DataGridViewRow r) => ((RoofSheetQuantity)r.Tag).Insulation);
				DataGridViewRow dataGridViewRow = insulationRolls.Rows.Cast<DataGridViewRow>().FirstOrDefault((DataGridViewRow r) => Cell(r, 0) == type);
				if (dataGridViewRow == null)
				{
					throw new Exception("Missing insulation roll settings: " + type);
				}
				if (Cell(dataGridViewRow, 1) == "" || Cell(dataGridViewRow, 2) == "") { bool wasFilling = filling; filling = true; try { if (Cell(dataGridViewRow, 1) == "") dataGridViewRow.Cells[1].Value = "1"; if (Cell(dataGridViewRow, 2) == "") dataGridViewRow.Cells[2].Value = "50"; } finally { filling = wasFilling; } }
				/* Owner (17 November 2026): show the roll area for every type with width and length, not only types used on a roof. */
				double rollWidth, rollLength;
				dataGridViewRow.Cells[3].Value = (double.TryParse(Cell(dataGridViewRow, 1), NumberStyles.Float, CultureInfo.InvariantCulture, out rollWidth) && double.TryParse(Cell(dataGridViewRow, 2), NumberStyles.Float, CultureInfo.InvariantCulture, out rollLength) && rollWidth > 0.0 && rollLength > 0.0) ? (rollWidth * rollLength).ToString("0.00", CultureInfo.InvariantCulture) : "";
				if (num == 0.0)
				{
					list.Add(new object[3]
					{
						type + " net area",
						0.0,
						"m2"
					});
					continue;
				}
				InsulationRollQuantity insulationRollQuantity = InsulationRollQuantity.Calculate(num, Number(dataGridViewRow, 1), Number(dataGridViewRow, 2));
				dataGridViewRow.Cells[3].Value = insulationRollQuantity.RollArea.ToString("0.00");
				list.Add(new object[3]
				{
					type + " net area",
					insulationRollQuantity.NetArea,
					"m2"
				});
				list.Add(new object[3]
				{
					type + " purchase coverage",
					insulationRollQuantity.PurchaseArea,
					"m2"
				});
			}
			return list;
		}

		private void AddEarthworkFootings()
		{
			cutFill = Grid(new string[15]
			{
				"Alignment / zone ID", "Description", "Start station", "End station", "Cut area start m2", "Cut area end m2", "Fill area start m2", "Fill area end m2", "Suitable soil %", "Swell %",
				"Bank shrink %", "Distance m", "Cut bank m3", "Fill compacted m3", "Status"
			}, 11);
			/* Owner (18 November 2026, "ang input na area is compacted, so dapat yung volume required is loose"): the sheet also
			   shows the loose volume needed for the compacted fill: compacted / (1 - bank shrink) x (1 + swell). */
			{ int looseColumn = cutFill.Columns.Add("FillRequiredLoose", "Fill required loose m3"); cutFill.Columns[looseColumn].ReadOnly = true; cutFill.Columns[looseColumn].ToolTipText = "Loose volume needed for the compacted fill = compacted / (1 - bank shrink %) x (1 + swell %). Embankment in Bill of Quantities uses loose volumes (from excavation first, then common borrow)."; try { cutFill.Columns[looseColumn].DisplayIndex = cutFill.Columns[13].DisplayIndex + 1; } catch (InvalidOperationException) { } }
			AddGrid("Cut & Fill", cutFill, "Average end area: volume = (start area + end area)/2 x station distance. Areas are m2, NOT depths or volumes. Station: metres or 0+020. Same alignment intervals must not overlap. Insert zero/change-point sections. Exclude beam/footing trenches already counted separately.");
			SetDefaults(cutFill, new Dictionary<int, string>
			{
				{ 8, "100" },
				{ 9, "20" },
				{ 10, "10" }
			});
			cutFill.Columns[8].ToolTipText = "Percentage of cut material suitable for reuse; 0 means all unsuitable. Subject to material approval.";
			List<string> list = new List<string> { "Footing ID", "Description", "L (m)", "B (m)", "D (m)", "Qty", "Excavation depth m (actual)", "Cover mm" };
			string[] array = new string[4] { "Bottom bars parallel to L", "Bottom bars parallel to B", "Top bars parallel to L", "Top bars parallel to B" }; /* owner, 18 November 2026: L, B, D and "parallel to" captions */
			foreach (string text in array)
			{
				list.AddRange(new string[3]
				{
					text + " dia mm",
					text + " spacing mm",
					text + " no. of bars/footing"
				});
			}
			list.AddRange(new string[13]
			{
				"Extra ends m/bar (both ends)", "Side forms", "Suitable soil %", "Swell %", "Bank shrink %", "Concrete MPa", "Grade <=12mm", "Grade >=16mm", "Concrete m3", "Forms m2",
				"Rebar BUY kg", "Excavation bank m3", "Status"
			});
			footings = Grid(list.ToArray(), 28);
			AddCombo(footings, 21, "Side forms", new string[2] { "Formed sides", "Earth cast" });
			AddCombo(footings, 25, "Concrete MPa", new string[3] { "21", "28", "35" });
			AddCombo(footings, 26, "Grade <=12mm", new string[2] { "40", "60" });
			AddCombo(footings, 27, "Grade >=16mm", new string[2] { "40", "60" });
			SetDefaults(footings, new Dictionary<int, string>
			{
				{ 2, "2" },
				{ 3, "2" },
				{ 4, "0.40" },
				{ 5, "1" },
				{ 7, "75" },
				{ 8, "16" },
				{ 9, "150" },
				{ 11, "16" },
				{ 12, "150" },
				{ 20, "0" },
				{ 21, "Formed sides" },
				{ 22, "100" },
				{ 23, "20" },
				{ 24, "10" },
				{ 25, "21" },
				{ 26, "40" },
				{ 27, "60" }
			});
			AddGrid("Footing Estimate", footings, "Rectangular flat footings; L = length, B = width, D = depth (m); cover in mm. Bars parallel to L run along the length; bars parallel to B run along the width. For EACH bar group enter spacing OR no. of bars; blank diameter = unused group. Cut = run - 2 cover + specified total end allowance. Top mat optional. Excavation allowance 0.30m each side, gravel 0.10m under footprint. Count shared pits only once.");
			footings.Columns[20].ToolTipText = "Total extra length at BOTH ends of each bar, from approved hook/development detail. 0 = straight cut. Not a structural anchorage check. Commercial splices use saved material configuration.";
			FlowLayoutPanel flowLayoutPanel = tabs.TabPages.Cast<TabPage>().First((TabPage p) => p.Text == "Footing Estimate").Controls.OfType<FlowLayoutPanel>().First();
			Button(flowLayoutPanel, "New footing", delegate
			{
				EditFooting(true);
			});
			Button(flowLayoutPanel, "Edit footing", delegate
			{
				EditFooting(false);
			});
			Button(flowLayoutPanel, "Settings", ShowWorksSettings);
			Button(flowLayoutPanel, "Commercial lengths", delegate
			{
				List<double> list2 = StockLengthPicker.Choose(this, footingStock);
				if (list2 != null)
				{
					footingStock = list2;
					Recalculate();
				}
			});
			array = new string[3] { "Rebar net kg", "Backfill compacted m3", "Gravel installed m3" };
			foreach (string text2 in array)
			{
				int index = footings.Columns.Add(text2, text2);
				footings.Columns[index].ReadOnly = true;
				footings.Columns[index].Width = 155;
			}
			for (int num = 6; num < 28; num++)
			{
				footings.Columns[num].Visible = false;
			}
			CheckBox advanced = new CheckBox
			{
				Text = "Show excavation and cover details",
				AutoSize = true,
				Padding = new Padding(8, 6, 0, 0)
			};
			advanced.CheckedChanged += delegate
			{
				for (int j = 6; j < 25; j++)
				{
					footings.Columns[j].Visible = j==6||(j>=8&&j<=19)||advanced.Checked && j != 22 && j != 23 && j != 24;
				}
			};
			flowLayoutPanel.Controls.Add(advanced);
			TabPage tabPage = new TabPage("Earthworks Summary");
			earthBalanceGrid = Grid(new string[3] { "Material / quantity", "Quantity", "Unit" }, 0);
			earthBalanceGrid.AllowUserToAddRows = false;
			earthBalanceGrid.Columns[0].Width = 540;
			earthBalanceGrid.Columns[1].Width = 160;
			earthBalanceGrid.Columns[2].Width = 130;
			FlowLayoutPanel flowLayoutPanel2 = new FlowLayoutPanel
			{
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Height = 1,
				Width = 1100,
				Padding = new Padding(4),
				Margin = new Padding(0, 4, 0, 10)
			};
			flowLayoutPanel2.Controls.Add(new Label
			{
				Text = "Suitable soil %",
				AutoSize = true,
				Padding = new Padding(0, 8, 0, 0)
			});
			suitableSoil = new NumericUpDown
			{
				Minimum = 0m,
				Maximum = 100m,
				DecimalPlaces = 2,
				Value = 100m,
				Width = 90
			};
			flowLayoutPanel2.Controls.Add(suitableSoil);
			suitableSoil.ValueChanged += delegate
			{
				if (!filling)
				{
					Recalculate();
				}
			};
			flowLayoutPanel2.Controls.Add(new Label
			{
				Text = "Default soil swell %",
				AutoSize = true,
				Padding = new Padding(0, 8, 0, 0)
			});
			borrowSwell = new NumericUpDown
			{
				Minimum = 0m,
				Maximum = 200m,
				DecimalPlaces = 2,
				Value = 20m,
				Width = 90
			};
			flowLayoutPanel2.Controls.Add(borrowSwell);
			flowLayoutPanel2.Controls.Add(new Label
			{
				Text = "Default bank shrink %",
				AutoSize = true,
				Padding = new Padding(0, 8, 0, 0)
			});
			borrowShrink = new NumericUpDown
			{
				Minimum = 0m,
				Maximum = 99m,
				DecimalPlaces = 2,
				Value = 10m,
				Width = 90
			};
			flowLayoutPanel2.Controls.Add(borrowShrink);
			flowLayoutPanel2.Controls.Add(new Label
			{
				Text = "Gravel purchase allowance %",
				AutoSize = true,
				Padding = new Padding(0, 8, 0, 0)
			});
			gravelAllowance = new NumericUpDown
			{
				Minimum = 0m,
				Maximum = 100m,
				DecimalPlaces = 2,
				Value = 5m,
				Width = 80
			};
			flowLayoutPanel2.Controls.Add(gravelAllowance);
			gravelAllowance.ValueChanged += delegate
			{
				if (!filling)
				{
					Recalculate();
				}
			};
			borrowSwell.ValueChanged += delegate
			{
				if (!filling)
				{
					Recalculate();
				}
			};
			borrowShrink.ValueChanged += delegate
			{
				if (!filling)
				{
					Recalculate();
				}
			};
			earthStatus = new Label
			{
				Dock = DockStyle.Top,
				Height = 80,
				Padding = new Padding(8),
				Text = "Overall material balance: suitable excavation from cut/fill, beams and footings is shared before common borrow. Final embankment quantities are LOOSE m3. Exclude overlapping excavation/fill from inputs; no automatic geometric overlap deduction."
			};
			ToolTip soilHelp = new ToolTip
			{
				ShowAlways = true
			};
			soilHelp.SetToolTip(borrowSwell, "Borrow bank-to-loose expansion; default 20% is an estimating assumption, not a universal soil standard.");
			soilHelp.SetToolTip(borrowShrink, "Borrow bank-to-compacted volume reduction; default 10% is an assumption, NOT a Proctor density target. Loose required = compacted gap / (1-shrink%) x (1+swell%).");
			soilHelp.SetToolTip(gravelAllowance, "Separate purchase ADDITION: default 5% means installed gravel x 1.05. Example basis: NTC CAR RFQ 036 (2020), G1 gravel bedding. Change to project specification. No soil swell/shrink added to gravel again.");
			base.FormClosed += delegate
			{
				soilHelp.Dispose();
			};
			tabPage.Controls.Add(earthBalanceGrid);
			tabPage.Controls.Add(earthStatus);
			tabs.TabPages.Add(tabPage);
			TabPage tabPage2 = new TabPage("Earthworks Start");
			FlowLayoutPanel start = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				AutoScroll = true,
				FlowDirection = FlowDirection.TopDown,
				WrapContents = false,
				Padding = new Padding(20)
			};
			start.Controls.Add(new Label
			{
				Text = "Earthworks | Defaults and quantities",
				Width = 1100,
				Height = 38,
				Font = new Font("Segoe UI", 16f, FontStyle.Bold),
				ForeColor = PowTheme.Ink
			});
			start.Controls.Add(new Label
			{
				Text = "These factors apply to every cut / fill row, beam trench, footing and common borrow. Defaults are estimating assumptions (reusable 100%, swell 20%, shrinkage 10%); confirm them against the project soil data.",
				Width = 1100,
				Height = 42
			});
			ArrangeSoilFactors(flowLayoutPanel2);
			start.Controls.Add(flowLayoutPanel2);
			start.Resize += delegate
			{
				foreach (Control control in start.Controls)
				{
					control.Width = Math.Max(400, Math.Min(1240, start.ClientSize.Width - 60));
				}
			};
			tabPage2.Controls.Add(start);
			tabs.TabPages.Add(tabPage2);
			for (int num2 = 8; num2 <= 10; num2++)
			{
				cutFill.Columns[num2].Visible = false;
			}
			AddGravelEstimate();
            MoveEarthworksSummaryToStart();
		}

		private void SetDefaults(DataGridView grid, Dictionary<int, string> values)
		{
			Action<DataGridViewRow> apply = delegate(DataGridViewRow row)
			{
				foreach (KeyValuePair<int, string> value in values)
				{
					if (!QuantityInputDefaults.IsMultiplier(grid.Columns[value.Key].HeaderText) && !QuantityInputDefaults.IsNumber(value.Value) && row.Cells[value.Key].Value == null)
					{
						row.Cells[value.Key].Value = value.Value;
					}
				}
			};
			grid.DefaultValuesNeeded += delegate(object s, DataGridViewRowEventArgs e)
			{
				apply(e.Row);
			};
			grid.RowsAdded += delegate(object s, DataGridViewRowsAddedEventArgs e)
			{
				if (!filling)
				{
					for (int i = e.RowIndex; i < e.RowIndex + e.RowCount; i++)
					{
						if (!grid.Rows[i].IsNewRow)
						{
							apply(grid.Rows[i]);
						}
					}
				}
			};
		}

		private static double OptionalNumber(DataGridViewRow r, int col, double value = 0.0)
		{
			if (!(Cell(r, col) == ""))
			{
				return Number(r, col);
			}
			return value;
		}

		private void RecalculateEarthworks()
		{
			if (cutFill == null || footings == null || earthStatus == null || borrowSwell == null || borrowShrink == null || gravelAllowance == null || suitableSoil == null)
			{
				return;
			}
			cutIntervals.Clear();
			earthSources.Clear();
			footingResults.Clear();
			earthBalance = null;
			foreach (DataGridViewRow item in (IEnumerable)cutFill.Rows)
			{
				if (!item.IsNewRow && !Empty(item))
				{
					item.Tag = null;
					for (int i = 11; i < 15; i++)
					{
						item.Cells[i].Value = "";
					}
					if (cutFill.Columns.Contains("FillRequiredLoose")) item.Cells["FillRequiredLoose"].Value = "";
					try
					{
						item.Cells[8].Value = suitableSoil.Value;
						item.Cells[9].Value = borrowSwell.Value;
						item.Cells[10].Value = borrowShrink.Value;
						CutFillInterval cutFillInterval = new CutFillInterval
						{
							Alignment = Cell(item, 0),
							Start = QuantityMath.Station(Cell(item, 2)),
							End = QuantityMath.Station(Cell(item, 3)),
							CutStart = OptionalNumber(item, 4),
							CutEnd = OptionalNumber(item, 5),
							FillStart = OptionalNumber(item, 6),
							FillEnd = OptionalNumber(item, 7)
						};
						cutFillInterval.Validate();
						EarthworkMaterial earthworkMaterial = new EarthworkMaterial();
						earthworkMaterial.Id = Cell(item, 0) + " " + Cell(item, 2) + "-" + Cell(item, 3);
						earthworkMaterial.Source = "Cut & fill";
						earthworkMaterial.Bank = cutFillInterval.Cut;
						earthworkMaterial.Fill = cutFillInterval.Fill;
						earthworkMaterial.Reusable = OptionalNumber(item, 8, 100.0);
						earthworkMaterial.Swell = OptionalNumber(item, 9, 20.0);
						earthworkMaterial.Shrink = OptionalNumber(item, 10, 10.0);
						EarthworkMaterial earthworkMaterial2 = earthworkMaterial;
						earthworkMaterial2.Validate();
						cutIntervals.Add(cutFillInterval);
						earthSources.Add(earthworkMaterial2);
						item.Tag = cutFillInterval;
						item.Cells[11].Value = (cutFillInterval.End - cutFillInterval.Start).ToString("0.00");
						item.Cells[12].Value = cutFillInterval.Cut.ToString("0.00");
						item.Cells[13].Value = cutFillInterval.Fill.ToString("0.00");
						if (cutFill.Columns.Contains("FillRequiredLoose")) item.Cells["FillRequiredLoose"].Value = (cutFillInterval.Fill / (1.0 - earthworkMaterial2.Shrink / 100.0) * (1.0 + earthworkMaterial2.Swell / 100.0)).ToString("0.00");
						item.Cells[14].Value = "Estimated";
					}
					catch (Exception ex)
					{
						errors.Add("Cut/fill " + Cell(item, 0) + ": " + ex.Message);
						item.Cells[14].Value = ex.Message;
					}
				}
			}
			try
			{
				CutFillInterval.CheckOverlap(cutIntervals);
			}
			catch (Exception ex2)
			{
				errors.Add(ex2.Message);
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            SyncFootingReinforcement();
			foreach (DataGridViewRow item2 in (IEnumerable)footings.Rows)
			{
				if (item2.IsNewRow || Empty(item2))
				{
					continue;
				}
				item2.Tag = null;
				for (int j = 28; j < 36; j++)
				{
					item2.Cells[j].Value = "";
				}
				try
				{
					item2.Cells[22].Value = suitableSoil.Value;
					item2.Cells[23].Value = borrowSwell.Value;
					item2.Cells[24].Value = borrowShrink.Value;
					if (!hashSet.Add(Cell(item2, 0)))
					{
						throw new Exception("Duplicate footing ID");
					}
					double num = OptionalNumber(item2, 25, 21.0);
					int num2 = (int)OptionalNumber(item2, 26, 40.0);
					int num3 = (int)OptionalNumber(item2, 27, 60.0);
					if (!new double[3] { 21.0, 28.0, 35.0 }.Contains(num) || !new int[2] { 40, 60 }.Contains(num2) || !new int[2] { 40, 60 }.Contains(num3))
					{
						throw new Exception("Choose concrete 21/28/35 and steel Grade 40/60.");
					}
					LengthConfiguration configuration = CalculationStore.Resolve(num, num2, (num2 == 40) ? 275 : 420, num3, (num3 == 40) ? 275 : 420);
					FootingBarSpec[] array = new FootingBarSpec[4];
					for (int k = 0; k < 4; k++)
					{
						ValidateFootingInputMethod(item2,k);
                        int num4 = 8 + 3 * k;
						double num5 = OptionalNumber(item2, num4);
						double num6 = OptionalNumber(item2, num4 + 2);
						if (num5 != Math.Truncate(num5) || num6 != Math.Truncate(num6))
						{
							throw new Exception("Bar diameter/count must be whole numbers.");
						}
						array[k] = new FootingBarSpec
						{
							Diameter = (int)num5,
							Spacing = OptionalNumber(item2, num4 + 1),
							Count = (int)num6
						};
					}
					double num7 = Number(item2, 4);
					FootingTakeoff footingTakeoff = FootingTakeoff.Calculate(Cell(item2, 0), Cell(item2, 1), Number(item2, 2), Number(item2, 3), num7, Count(item2, 5), Number(item2, 6), OptionalNumber(item2, 7, (double)rcCovers["Footing"].Value), Cell(item2, 21) != "Earth cast", array, OptionalNumber(item2, 20), configuration, OptionalNumber(item2, 23, 20.0), OptionalNumber(item2, 24, 10.0), OptionalNumber(item2, 22, 100.0));
					footingResults.Add(footingTakeoff);
					earthSources.Add(footingTakeoff.Soil);
					item2.Tag = footingTakeoff;
					item2.Cells[28].Value = footingTakeoff.Concrete.ToString("0.00");
					item2.Cells[29].Value = footingTakeoff.Forms.ToString("0.00");
					item2.Cells[31].Value = footingTakeoff.Excavation.ToString("0.00");
					item2.Cells[32].Value = "Estimated";
					item2.Cells[33].Value = footingTakeoff.Steel.Result.Total.ToString("0.00");
					item2.Cells[34].Value = footingTakeoff.Backfill.ToString("0.00");
					item2.Cells[35].Value = footingTakeoff.Gravel.ToString("0.00");
				}
				catch (Exception ex3)
				{
					errors.Add("Footing " + Cell(item2, 0) + ": " + ex3.Message);
					item2.Cells[32].Value = ex3.Message;
				}
			}
			footingPlan = new List<StockAllocation>();
			foreach (IGrouping<string, DataGridViewRow> item3 in from DataGridViewRow row in footings.Rows
				where row.Tag is FootingTakeoff
				group row by OptionalNumber(row, 25, 21.0).ToString(CultureInfo.InvariantCulture) + "|" + OptionalNumber(row, 26, 40.0) + "|" + OptionalNumber(row, 27, 60.0))
			{
				DataGridViewRow r = item3.First();
				int num8 = (int)OptionalNumber(r, 26, 40.0);
				int num9 = (int)OptionalNumber(r, 27, 60.0);
				LengthConfiguration config = CalculationStore.Resolve(OptionalNumber(r, 25, 21.0), num8, (num8 == 40) ? 275 : 420, num9, (num9 == 40) ? 275 : 420);
				footingPlan.AddRange(CommercialBars.Allocate(item3.Select((DataGridViewRow row) => ((FootingTakeoff)row.Tag).Steel), config, footingStock));
			}
			foreach (DataGridViewRow item4 in (IEnumerable)footings.Rows)
			{
				FootingTakeoff q = item4.Tag as FootingTakeoff;
				if (q != null)
				{
					List<StockAllocation> source = footingPlan.Where((StockAllocation x) => x.Entry == q.Steel).ToList();
					if (source.Any((StockAllocation x) => x.Choice == null))
					{
						errors.Add("Footing " + q.Id + ": unresolved commercial bar cut");
						item4.Cells[32].Value = "Unresolved stock cut";
					}
					item4.Cells[30].Value = source.Sum((StockAllocation x) => x.Purchase).ToString("0.00");
				}
			}
			try
			{
				List<EarthworkMaterial> list = beam.WorksEarthworkMaterials();
				foreach (EarthworkMaterial item5 in list)
				{
					item5.Reusable = (double)suitableSoil.Value;
				}
				earthSources.AddRange(list);
				earthBalance = EarthworkBalance.Calculate(earthSources, (double)borrowSwell.Value, (double)borrowShrink.Value);
			}
			catch (Exception ex4)
			{
				errors.Add("Overall earthworks: " + ex4.Message);
			}
			earthBalanceGrid.Rows.Clear();
			if (earthBalance != null)
			{
				foreach (object[] item6 in EarthSummaryRows().Take(6))
				{
					earthBalanceGrid.Rows.Add(item6);
				}
			}
			earthStatus.Text = ((errors.Count > 0) ? "REVIEW / PARTIAL INPUTS: fix errors before export.\n" : "") + "Shared suitable soil from cut/fill, beam trenches and footing pits. Final embankment quantities are LOOSE material before compaction. Sources used in entry order (cut/fill, footings, beams); no haul-distance optimization. Exclude overlapping trenches/fill from inputs.";
		}

		private List<object[]> EarthSummaryRows()
		{
			EarthworkBalance earthworkBalance = earthBalance;
			List<object[]> list = new List<object[]>();
			list.Add(new object[3] { "Excavation (in-place / bank)", earthworkBalance.Excavation, "m3" });
			list.Add(new object[3] { "Required space after compaction (reference)", earthworkBalance.Fill, "m3" });
			list.Add(new object[3] { "Embankment FROM EXCAVATION - loose material", earthworkBalance.LooseFromExcavation, "m3" });
			list.Add(new object[3] { "Embankment FROM COMMON BORROW - loose material to buy", earthworkBalance.LooseCommonBorrow, "m3" });
			list.Add(new object[3] { "Suitable surplus for disposal / stockpile - loose", earthworkBalance.LooseSurplus, "m3" });
			list.Add(new object[3] { "Unsuitable material for disposal - loose", earthworkBalance.UnsuitableLoose, "m3" });
			list.Add(new object[3]
			{
				"Beam gravel installed volume",
				beam.WorksGravel(),
				"m3"
			});
			list.Add(new object[3]
			{
				"Footing gravel installed volume",
				footingResults.Sum((FootingTakeoff x) => x.Gravel) + wallFootingResults.Sum(q=>q.Gravel),
				"m3"
			});
			list.Add(new object[3]
			{
				"TOTAL gravel installed volume (including other areas)",
				TotalGravelInstalled(),
				"m3"
			});
			list.Add(new object[3]
			{
				"TOTAL GRAVEL TO BUY - loose material",
				TotalGravelInstalled() * (1.0 + (double)gravelAllowance.Value / 100.0),
				"m3"
			});
			return list;
		}

		private void WriteEarthSettings(XmlElement root)
		{
			root.SetAttribute("suitableSoil", suitableSoil.Value.ToString(CultureInfo.InvariantCulture));
			WriteMaterialDefaults(root);
			root.SetAttribute("flashingLapMm", flashingLap.Value.ToString(CultureInfo.InvariantCulture));
			root.SetAttribute("gutterLapMm", gutterLap.Value.ToString(CultureInfo.InvariantCulture));
			root.SetAttribute("ridgeLapMm", ridgeLap.Value.ToString(CultureInfo.InvariantCulture));
			root.SetAttribute("gravelAllowance", gravelAllowance.Value.ToString(CultureInfo.InvariantCulture));
			root.SetAttribute("footingStock", CommercialBars.FormatLengths(footingStock));
			root.SetAttribute("borrowSwell", borrowSwell.Value.ToString(CultureInfo.InvariantCulture));
			root.SetAttribute("borrowShrink", borrowShrink.Value.ToString(CultureInfo.InvariantCulture));
		}

		private void ReadEarthSettings(XmlElement root)
		{
			suitableSoil.Value = (root.HasAttribute("suitableSoil") ? decimal.Parse(root.GetAttribute("suitableSoil"), CultureInfo.InvariantCulture) : 100m);
			ReadMaterialDefaults(root);
			flashingLap.Value = (root.HasAttribute("flashingLapMm") ? decimal.Parse(root.GetAttribute("flashingLapMm"), CultureInfo.InvariantCulture) : 100m);
			gutterLap.Value = (root.HasAttribute("gutterLapMm") ? decimal.Parse(root.GetAttribute("gutterLapMm"), CultureInfo.InvariantCulture) : 150m);
			ridgeLap.Value = (root.HasAttribute("ridgeLapMm") ? decimal.Parse(root.GetAttribute("ridgeLapMm"), CultureInfo.InvariantCulture) : 300m);
			gravelAllowance.Value = (root.HasAttribute("gravelAllowance") ? decimal.Parse(root.GetAttribute("gravelAllowance"), CultureInfo.InvariantCulture) : 5m);
			footingStock = CommercialBars.ParseLengths(root.GetAttribute("footingStock"));
			borrowSwell.Value = (root.HasAttribute("borrowSwell") ? decimal.Parse(root.GetAttribute("borrowSwell"), CultureInfo.InvariantCulture) : 20m);
			borrowShrink.Value = (root.HasAttribute("borrowShrink") ? decimal.Parse(root.GetAttribute("borrowShrink"), CultureInfo.InvariantCulture) : 10m);
		}

		private void AppendEarthworkReport(StringBuilder b, bool detailed)
		{
			if (earthBalance == null)
			{
				throw new Exception("Resolve earthwork inputs first.");
			}
			b.AppendLine();
			b.AppendLine("OVERALL EARTHWORK MATERIAL BALANCE | FINAL QUANTITIES");
			foreach (object[] item in EarthSummaryRows())
			{
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,-63}{1,13:0.00} {2}", item[0], item[1], item[2]));
			}
			b.AppendLine("Only the two EMBANKMENT quantities are material requirements before compaction. No additional allowance.");
			b.AppendLine("Shared soil balance supersedes individual beam/footing borrow estimates. Do not add both.");
			b.AppendLine("Gravel BUY = installed gravel x (1 + " + gravelAllowance.Value.ToString("0.00") + "% purchase allowance). Separate from soil swell/shrink; do not apply twice.");
			if (!detailed)
			{
				return;
			}
			b.AppendLine("CUT & FILL | average end areas x station distance");
			foreach (CutFillInterval cutInterval in cutIntervals)
			{
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0} | {1:0.00}-{2:0.00}m: cut ({3:0.00}+{4:0.00})/2 x {5:0.00} = {6:0.00}m3; fill {7:0.00}m3", cutInterval.Alignment, cutInterval.Start, cutInterval.End, cutInterval.CutStart, cutInterval.CutEnd, cutInterval.End - cutInterval.Start, cutInterval.Cut, cutInterval.Fill));
			}
			b.AppendLine("SOURCE CHECK | bank excavation / compacted space / suitable % / swell % / bank shrink %");
			foreach (EarthworkMaterial earthSource in earthSources)
			{
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0} {1}: {2:0.00} / {3:0.00} / {4:0.00} / {5:0.00} / {6:0.00}", earthSource.Source, earthSource.Id, earthSource.Bank, earthSource.Fill, earthSource.Reusable, earthSource.Swell, earthSource.Shrink));
			}
			b.AppendLine(string.Format(CultureInfo.InvariantCulture, "Common borrow loose = unfilled compacted space / {0:0.00} x {1:0.00}", 1.0 - earthBalance.BorrowShrink / 100.0, 1.0 + earthBalance.BorrowSwell / 100.0));
			b.AppendLine("No geometric overlap deductions, slopes, haul-distance optimization or material compatibility checks. Suitable-soil % requires project approval.");
		}

		private void RefreshProjectRebarPlan()
		{
			if (footings == null || beam == null || slab == null)
			{
				return;
			}
			var beamPlan = beam.WorksPlan(); var slabPlan = slab.WorksPlan();
			projectRebarPlan = beamPlan.Concat(slabPlan).Concat(footingPlan)
				.Concat(canopyPlan)
				.Concat(rcWallPlan).Concat(miscPlan).Concat(wallFootingPlan).Concat(slopePlan)
				.ToList();
			/* offcuts of every member are used by the others of the same grade and diameter (owner, 21 November 2026: "icheck din sa ibang
			   calculation baka may magamit pa"): the purchase of each workspace alone against the shared plan */
			rebarAlone = 0;
			foreach (var alone in new[] { beamPlan, slabPlan, footingPlan, canopyPlan, rcWallPlan, wallFootingPlan, slopePlan }.Concat(miscPlan.GroupBy((StockAllocation a) => a.Entry).Select((IGrouping<EstimateInput, StockAllocation> g) => g.ToList())))
			{
				var part = alone.Where((StockAllocation a) => a.Choice != null).ToList();
				if (part.Count == 0) continue;
				try { RebarOffcuts.Allocate(part); rebarAlone += part.Sum((StockAllocation a) => a.Purchase); } catch (Exception) { }
			}
			RebarOffcuts.Allocate(projectRebarPlan);
			rebarShared = projectRebarPlan.Where((StockAllocation a) => a.Choice != null).Sum((StockAllocation a) => a.Purchase);
			foreach (DataGridViewRow item in (IEnumerable)footings.Rows)
			{
				FootingTakeoff footing = item.Tag as FootingTakeoff;
				if (footing != null)
				{
					item.Cells[30].Value = footingPlan.Where((StockAllocation a) => a.Entry == footing.Steel).Sum((StockAllocation a) => a.Purchase).ToString("0.00");
				}
			}
		}

		private void AppendFootingReport(StringBuilder b, bool detailed)
		{
			b.AppendLine();
			b.AppendLine("FOOTING WORKS | QUANTITIES TO BUY");
			b.AppendLine("Selected commercial lengths: " + CommercialBars.FormatLengths(footingStock) + " m");
			b.AppendLine(string.Format(CultureInfo.InvariantCulture, "Concrete {0:0.00} m3 | Side forms {1:0.00} m2 | Rebar BUY {2:0.00} kg", footingResults.Sum((FootingTakeoff x) => x.Concrete), footingResults.Sum((FootingTakeoff x) => x.Forms), footingPlan.Sum((StockAllocation x) => x.Purchase)));
			PurchaseReports.Summary(b, footingPlan, false);
			foreach (FootingTakeoff f in footingResults)
			{
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0} | {1} | {2:0.00} x {3:0.00} x {4:0.00}m x {5} pcs", f.Id, f.Description, f.Length, f.Width, f.Thickness, f.Quantity));
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Concrete {0:0.00} m3 | Forms {1:0.00} m2 | Exc {2:0.00} bank m3", f.Concrete, f.Forms, f.Excavation));
				if (detailed)
				{
					b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Pit ({0:0.00}+0.60) x ({1:0.00}+0.60) x {2:0.00} x {3}; gravel {4:0.00}m3; backfill space {5:0.00}m3", f.Length, f.Width, f.Depth, f.Quantity, f.Gravel, f.Backfill));
					PurchaseReports.Entry(b, footingPlan.Where((StockAllocation a) => a.Entry == f.Steel).ToList(), f.Quantity, reportDetail.SelectedIndex == 2);
				}
			}
			b.AppendLine("Cuts use entered cover/end allowance. Splices use saved material profile. No pedestal, slope, dowel or anchorage design inferred. Earthwork material requirements: use OVERALL balance.");
		}

		private void EarthworksExcel(List<WorksExcelTable> tables)
		{
			if (earthBalance == null)
			{
				throw new Exception("Earthwork balance is unavailable.");
			}
			tables.Add(new WorksExcelTable("POW Overall Earthworks", new string[3] { "Item (permanent rows)", "Quantity", "Unit" }, EarthSummaryRows()));
			List<object[]> list = new List<object[]>();
			foreach (EarthworkMaterial earthSource in earthSources)
			{
				list.Add(new object[9] { earthSource.Source, earthSource.Id, earthSource.Bank, earthSource.Fill, earthSource.Reusable, earthSource.Swell, earthSource.Shrink, earthSource.LooseAvailable, earthSource.CompactedAvailable });
			}
			tables.Add(new WorksExcelTable("POW Earthwork Sources", new string[9] { "Source", "ID", "Excavation bank m3", "Compacted space m3", "Suitable %", "Swell %", "Bank shrink %", "Suitable loose m3", "Suitable compacted yield m3" }, list));
			list = new List<object[]>();
			foreach (CutFillInterval cutInterval in cutIntervals)
			{
				list.Add(new object[10]
				{
					cutInterval.Alignment,
					cutInterval.Start,
					cutInterval.End,
					cutInterval.End - cutInterval.Start,
					cutInterval.CutStart,
					cutInterval.CutEnd,
					cutInterval.FillStart,
					cutInterval.FillEnd,
					cutInterval.Cut,
					cutInterval.Fill
				});
			}
			tables.Add(new WorksExcelTable("POW Cut Fill", new string[10] { "Alignment", "Start m", "End m", "Distance m", "Cut start m2", "Cut end m2", "Fill start m2", "Fill end m2", "Cut bank m3", "Fill compacted m3" }, list));
			list = new List<object[]>();
			foreach (FootingTakeoff f in footingResults)
			{
				list.Add(new object[12]
				{
					f.Id,
					f.Description,
					f.Length,
					f.Width,
					f.Thickness,
					f.Quantity,
					f.Concrete,
					f.Forms,
					footingPlan.Where((StockAllocation x) => x.Entry == f.Steel).Sum((StockAllocation x) => x.Purchase),
					f.Excavation,
					f.Gravel,
					f.Backfill
				});
			}
			tables.Add(new WorksExcelTable("POW Footings", new string[12]
			{
				"ID", "Description", "L m", "B m", "D m", "Qty", "Concrete m3", "Forms m2", "Rebar BUY kg", "Excavation bank m3",
				"Gravel m3", "Compacted space m3"
			}, list));
		}

		private void SetupRoofProfileLibrary()
		{
			FlowLayoutPanel flowLayoutPanel = tabs.TabPages.Cast<TabPage>().Single((TabPage p) => p.Text == "Roofing Start").Controls.OfType<FlowLayoutPanel>().First();
			flowLayoutPanel.AutoSize = true;
			flowLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			Button(flowLayoutPanel, "Save profiles in app", delegate
			{
				SaveRoofProfileLibrary(true);
			});
			Button(flowLayoutPanel, "Export Profiles", ExportRoofProfiles);
			Button(flowLayoutPanel, "Import Profiles", ImportRoofProfiles);
			roofLibraryStatus = new Label
			{
				AutoSize = true,
				Padding = new Padding(8, 8, 0, 0),
				Text = "Save profiles for reuse; export the library to share uniform profiles with other users."
			};
			flowLayoutPanel.Controls.Add(roofLibraryStatus);
			try
			{
				RoofProfileLibrary.Migrate(RoofProfileLibrary.DefaultPath, RoofProfileLibrary.LegacyPaths());
			}
			catch (Exception ex)
			{
				roofLibraryStatus.Text = "Roof profile migration needs review: " + ex.Message;
			}
			if (File.Exists(RoofProfileLibrary.DefaultPath))
			{
				try
				{
					List<RoofProfile> list = RoofProfileLibrary.Load(RoofProfileLibrary.DefaultPath);
					bool flag = filling;
					filling = true;
					try
					{
						roofProfiles.Rows.Clear();
						foreach (RoofProfile item in list)
						{
							AddRoofProfileRow(item);
						}
					}
					finally
					{
						filling = flag;
					}
					roofLibraryStatus.Text = "App profile library loaded from Documents. Saved Bill of Quantities projects retain their own profile settings.";
				}
				catch (Exception ex2)
				{
					roofLibraryStatus.Text = "App profiles unavailable: " + ex2.Message;
				}
			}
			object previousValue = null;
			roofProfiles.CellBeginEdit += delegate(object s, DataGridViewCellCancelEventArgs e)
			{
				previousValue = roofProfiles.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
			};
			roofProfiles.CellEndEdit += delegate(object s, DataGridViewCellEventArgs e)
			{
				if (!filling && !object.Equals(previousValue, roofProfiles.Rows[e.RowIndex].Cells[e.ColumnIndex].Value))
				{
					SaveRoofProfileLibrary(false);
				}
			};
		}

		private void ExportRoofProfiles()
		{
			Guard();
			roofProfiles.EndEdit();
			SaveRoofProfileLibrary(true);
			using (SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				Filter = "Roof profile library|*.roofprofiles.xml",
				FileName = "DTCE-Roof-Profiles.roofprofiles.xml"
			})
			{
				if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
				{
					RoofProfileLibrary.Save(saveFileDialog.FileName, RoofProfileLibrary.Load(RoofProfileLibrary.DefaultPath));
					roofLibraryStatus.Text = "Profile library exported. Other users can select Import Profiles to use these dimensions and side laps.";
				}
			}
		}

		private void ImportRoofProfiles()
		{
			Guard();
			using (OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = "Roof profile library|*.roofprofiles.xml;*.xml"
			})
			{
				if (openFileDialog.ShowDialog(this) != DialogResult.OK)
				{
					return;
				}
				roofProfiles.EndEdit();
				Recalculate();
				if (roofProfiles.Rows.Cast<DataGridViewRow>().Any((DataGridViewRow row) => !row.IsNewRow && !Empty(row) && Cell(row, 5) != "Saved in project"))
				{
					throw new Exception("Complete or remove unfinished roof profile rows before importing a library.");
				}
				List<RoofProfile> incoming = RoofProfileLibrary.Load(openFileDialog.FileName);
                projectSourceFiles.CaptureFile(openFileDialog.FileName,"Roof profile library");
				List<RoofProfile> list = RoofProfileLibrary.Merge(RoofProfileLibrary.Merge(File.Exists(RoofProfileLibrary.DefaultPath) ? RoofProfileLibrary.Load(RoofProfileLibrary.DefaultPath) : new List<RoofProfile>(), savedRoofProfiles), incoming);
				RoofProfileLibrary.Save(RoofProfileLibrary.DefaultPath, list);
				filling = true;
				try
				{
					roofProfiles.Rows.Clear();
					foreach (RoofProfile item in list)
					{
						AddRoofProfileRow(item);
					}
				}
				finally
				{
					filling = false;
				}
				Recalculate();
				roofLibraryStatus.Text = "Imported profiles saved in Documents. Matching profile names now use the imported options; review existing roof selections.";
			}
		}

		private void SaveRoofProfileLibrary(bool explicitSave)
		{
			if (explicitSave)
			{
				roofProfiles.EndEdit();
				Recalculate();
			}
			/* Owner (18 November 2026: profile sizes kept disappearing in new projects). The auto-save ran before the
			   edited row was re-validated, and one incomplete row blocked saving every other profile. Re-read the rows
			   now and save every valid profile; incomplete rows are simply left out until they are complete. */
			if (!explicitSave)
			{
				int before = errors.Count;
				RefreshRoofProfiles();
				if (errors.Count > before) errors.RemoveRange(before, errors.Count - before);
			}
			if (savedRoofProfiles.Count <= 0 || explicitSave && roofProfiles.Rows.Cast<DataGridViewRow>().Any((DataGridViewRow row) => !row.IsNewRow && !Empty(row) && Cell(row, 5) != "Saved in project"))
			{
				roofLibraryStatus.Text = "Complete valid profile rows before saving to the app library.";
				if (explicitSave)
				{
					throw new Exception(roofLibraryStatus.Text);
				}
				return;
			}
			try
			{
				List<RoofProfile> existing = (File.Exists(RoofProfileLibrary.DefaultPath) ? RoofProfileLibrary.Load(RoofProfileLibrary.DefaultPath) : new List<RoofProfile>());
				RoofProfileLibrary.Save(RoofProfileLibrary.DefaultPath, RoofProfileLibrary.Merge(existing, savedRoofProfiles));
				roofLibraryStatus.Text = "Profiles saved in Documents > DTCE Tool Kit > Profiles. Save Bill of Quantities keeps estimate and DXF data.";
			}
			catch (Exception ex)
			{
				roofLibraryStatus.Text = "App profile save failed: " + ex.Message;
				if (explicitSave)
				{
					throw;
				}
			}
		}

		private void ReuseRoofAccessoryOffcuts()
		{
			foreach (IGrouping<string, DataGridViewRow> item in from DataGridViewRow r in roofAccessories.Rows
				where r.Tag is RoofAccessoryQuantity
				group r by (Cell(r, 2) + "|" + Cell(r, 1).Trim() + "|" + OptionalNumber(r, 5).ToString("R", CultureInfo.InvariantCulture)).ToUpperInvariant())
			{
				List<StockAllocation> cuts = new List<StockAllocation>();
				Dictionary<StockAllocation, DataGridViewRow> owners = new Dictionary<StockAllocation, DataGridViewRow>();
				foreach (DataGridViewRow row in item)
				{
					double num = Number(row, 3);
					double stock = Number(row, 6);
					double num2 = Number(row, 7) / 1000.0;
					int num3 = Count(row, 4);
					int num4 = ((num <= stock) ? 1 : RoofMath.Ceil((num - num2) / (stock - num2)));
					double arg = num - (double)(num4 - 1) * (stock - num2);
					Action<double, long> obj = delegate(double cut, long pieces)
					{
						if (pieces > 0)
						{
							StockAllocation stockAllocation = new StockAllocation
							{
								UnitMassOverride = 1.0,
								Allowed = new List<double> { stock },
								Demand = new StockDemand
								{
									Grade = 1,
									Diameter = 1,
									Cut = cut,
									Pieces = pieces
								},
								Choice = new StockChoice
								{
									Length = stock,
									Cut = cut,
									Required = pieces
								}
							};
							cuts.Add(stockAllocation);
							owners.Add(stockAllocation, row);
						}
					};
					obj(stock, (long)(num4 - 1) * (long)num3);
					obj(arg, num3);
					RoofAccessoryQuantity obj2 = (RoofAccessoryQuantity)row.Tag;
					obj2.Pieces = 0;
					obj2.BuyLength = 0.0;
					obj2.BuyArea = 0.0;
				}
				RebarOffcuts.Allocate(cuts);
				checked
				{
					foreach (RebarStock item2 in cuts.SelectMany((StockAllocation a) => a.Stocks).Distinct())
					{
						DataGridViewRow dataGridViewRow = owners[item2.Used.Keys.OrderByDescending((StockAllocation a) => a.Demand.Cut).First()];
						RoofAccessoryQuantity obj3 = (RoofAccessoryQuantity)dataGridViewRow.Tag;
						obj3.Pieces += (int)item2.Count;
						obj3.BuyLength += (double)item2.Count * item2.Length;
						obj3.BuyArea += (double)item2.Count * item2.Length * OptionalNumber(dataGridViewRow, 5) / 1000.0;
					}
					foreach (DataGridViewRow item3 in item)
					{
						RoofAccessoryQuantity roofAccessoryQuantity = (RoofAccessoryQuantity)item3.Tag;
						item3.Cells[8].Value = roofAccessoryQuantity.Pieces;
						item3.Cells[9].Value = roofAccessoryQuantity.BuyLength.ToString("0.00");
						item3.Cells[10].Value = roofAccessoryQuantity.BuyArea.ToString("0.00");
					}
				}
			}
		}

		private void AddStructuralSteel()
		{
			steelPurlins = SteelMemberGrid("Purlin");
			steelAngles = SteelMemberGrid("Angle bar");
			AddGrid(SteelPageNames[0], steelPurlins, "Select a profile, enter cut length and quantity. Stock is fixed at 6m. Matching profile/specification offcuts are shared across compatible members automatically. Cuts over 6m require an approved splice detail.");
			AddGrid(SteelPageNames[1], steelAngles, "Angle-bar quantities use 6m stock and compatible profile/material offcut sharing. Longer entered lengths are allocated as stock portions for the purchase takeoff. No splice/lap length is added. This is a quantity allocation, not a connection design.");
			machineBolts = SteelBoltGrid();
			anchorBolts = SteelBoltGrid();
			AddGrid(SteelPageNames[2], machineBolts, "A307 or A325. Overall developed length in mm. Weight = pi x diameter^2 / 4 x overall length x 7850 kg/m3 x quantity. Shaft-only estimate: heads, nuts and washers are excluded. Grade selection identifies material; this is not a connection design check.");
			AddGrid(SteelPageNames[3], anchorBolts, "A307 or A325. Overall developed length must include all straight and bent/hooked portions. Shaft weight only, excluding heads, nuts and washers. Anchorage/embedment strength is not inferred.");
			steelPlates = Grid(new string[14]
			{
				"ID", "Description", "Area m2 / pc (OR dimensions)", "Length m", "Width m", "Thickness (mm)", "Qty", "Total area m2", "Net weight kg", "Sheets used (may be shared)",
				"Selected stock sheets", "Purchase weight kg", "Allocated offcut kg", "Status"
			}, 7);
			SetDefaults(steelPlates, new Dictionary<int, string>
			{
				{ 5, "6" },
				{ 6, "1" }
			});
			AddGrid(SteelPageNames[4], steelPlates, "Enter area per plate OR length and width, then thickness in mm and quantity (enter 6 for a 6 mm plate). Weight (kg) = area (m2) x thickness (mm) / 1000 x 7850 (kg/m3) x quantity. For sheet purchasing, enter length and width. Matching-thickness cuts share offcuts; standard stock sizes are compared with 90-degree rotation. Lowest purchase weight among compared feasible layouts is selected; zero kerf. Area-only entries calculate net weight but need dimensions before purchase quantities can be finalized.");
			crossBracing = Grid(new string[13]
			{
				"ID", "Description", "X distance m", "Y offset m", "Height m", "Rod dia mm", "Bracing lines", "Diagonal m / line", "Total rod length m", "Rod weight kg",
				"Turnbuckles pcs", "Turnbuckle size mm", "Status"
			}, 7);
			SetDefaults(crossBracing, new Dictionary<int, string>
			{
				{ 3, "0" },
				{ 5, "12" },
				{ 6, "1" }
			});
			AddGrid(SteelPageNames[5], crossBracing, "Diagonal = sqrt(X^2 + Y^2 + height^2). Use Y=0 for planar bracing. Each line has one turnbuckle of the same nominal diameter. An X pair is TWO lines. Rod weight excludes turnbuckles and connection hardware; no turnbuckle length is deducted.");
			fasciaFrame = Grid(new string[12]
			{
				"ID", "Description", "Member type", "Section", "Material grade / finish", "Cut length m", "Qty", "Flat width mm", "Flat thickness mm", "Unit mass kg/m",
				"Required to buy kg", "Status"
			}, 9);
			AddCombo(fasciaFrame, 2, "Member type", new string[2] { "Angle bar", "Flat bar" });
			AddCombo(fasciaFrame, 3, "Section", (from p in SteelCatalog.Profiles
				where p.Kind == "Angle bar" || p.Kind == "Flat bar"
				select p.Name).ToArray());
			SetDefaults(fasciaFrame, new Dictionary<int, string>
			{
				{ 2, "Angle bar" },
				{
					3,
					SteelCatalog.Profiles.First((SteelProfile p) => p.Kind == "Angle bar").Name
				},
				{ 4, "Project specification" },
				{ 6, "1" }
			});
			AddGrid(SteelPageNames[6], fasciaFrame, "Angle and flat bars use fixed 6m stock. Select the member type first; only its sections are offered. Flat-bar width and thickness are read from the section. Compatible offcuts can serve other fascia rows and matching angle-bar entries.");
			DataGridViewColumn dataGridViewColumn = fasciaFrame.Columns[7];
			bool readOnly = (fasciaFrame.Columns[8].ReadOnly = true);
			dataGridViewColumn.ReadOnly = readOnly;
			AddSteelSetColumns();
			AddSteelPaintColumns();
			steelCutGrid = Grid(new string[8] { "Stock no.", "Profile / specification", "Cut order", "Member ID", "Cut m", "Remaining after cut m", "Next use", "Final leftover m" }, 0);
			steelCutGrid.ReadOnly = true;
			steelCutGrid.AllowUserToAddRows = false;
			steelCutGrid.Columns[1].Width = 350;
			DataGridView[] array = new DataGridView[5] { steelPurlins, steelAngles, machineBolts, anchorBolts, fasciaFrame };
			foreach (DataGridView grid in array)
			{
				grid.CurrentCellDirtyStateChanged += delegate
				{
					if (grid.IsCurrentCellDirty && grid.CurrentCell is DataGridViewComboBoxCell)
					{
						grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
					}
				};
				grid.CellValueChanged += delegate(object s, DataGridViewCellEventArgs e)
				{
					if (!filling && e.RowIndex >= 0 && (e.ColumnIndex == 2 || e.ColumnIndex == 3))
					{
						Recalculate();
					}
				};
			}
		}

		private DataGridView SteelMemberGrid(string kind)
		{
			DataGridView dataGridView = Grid(new string[9] { "ID", "Description", "Profile", "Material grade / finish", "Cut length m", "Qty", "Unit mass kg/m", "Required to buy kg", "Status" }, 6);
			AddCombo(dataGridView, 2, "Profile", (from p in SteelCatalog.Profiles
				where p.Kind == kind
				select p.Name).ToArray());
			dataGridView.Columns[2].Width = 260;
			SetDefaults(dataGridView, new Dictionary<int, string>
			{
				{ 3, "Project specification" },
				{ 5, "1" }
			});
			return dataGridView;
		}

		private DataGridView SteelBoltGrid()
		{
			DataGridView dataGridView = Grid(new string[9] { "ID", "Description", "Grade", "Diameter mm", "Overall developed length mm", "Qty", "Total length m", "Shaft weight kg", "Status" }, 6);
			AddCombo(dataGridView, 2, "Grade", new string[2] { "A307", "A325" });
			SetDefaults(dataGridView, new Dictionary<int, string>
			{
				{ 2, "A307" },
				{ 3, "16" },
				{ 5, "1" }
			});
			return dataGridView;
		}

		private Dictionary<string, DataGridView> SteelTables()
		{
			if (steelPurlins == null)
			{
				return new Dictionary<string, DataGridView>();
			}
			return new Dictionary<string, DataGridView>
			{
				{
					SteelSections[0],
					steelPurlins
				},
				{
					SteelSections[1],
					steelAngles
				},
				{
					SteelSections[2],
					machineBolts
				},
				{
					SteelSections[3],
					anchorBolts
				},
				{
					SteelSections[4],
					steelPlates
				},
				{
					SteelSections[5],
					crossBracing
				},
				{
					SteelSections[6],
					fasciaFrame
				}
			};
		}

		private static int SteelInputCount(string section)
		{
			switch (section)
			{
			default:
				return 6;
			case "FasciaFrame":
				return 9;
			case "SteelPlates":
			case "CrossBracing":
				return 7;
			}
		}

		private void RecalculateStructuralSteel()
		{
			if (steelPurlins == null)
			{
				return;
			}
			AssignStructuralSteelIds();
			steelDemands.Clear();
			steelPaintAreas.Clear();steelPaintDetails.Clear();
			steelStocks.Clear();
			plateStocks.Clear();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (KeyValuePair<string, DataGridView> item in SteelTables())
			{
				DataGridView value = item.Value;
				int num = SteelInputCount(item.Key);
				int num2 = value.Columns.Count - 1;
				foreach (DataGridViewRow item2 in (IEnumerable)value.Rows)
				{
					if (value == fasciaFrame)
					{
						FilterFasciaSections(item2);
					}
					if (item2.IsNewRow)
					{
						continue;
					}
					item2.Tag = null;
					for (int i = num; i <= num2; i++)
					{
						if (!IsSteelSetColumn(value.Columns[i]) && value.Columns[i].Name != SteelPaintOnlyColumn && value.Columns[i].Name != TrussMarkColumn) item2.Cells[i].Value = "";
					}
					if (Empty(item2))
					{
						continue;
					}
					try
					{
						if (Cell(item2, 0) == "" || Cell(item2, 1) == "")
						{
							throw new Exception("Enter ID and description.");
						}
						if (!hashSet.Add(Cell(item2, 0)))
						{
							throw new Exception("Steel IDs must be unique across tabs.");
						}
						if (value == steelPurlins || value == steelAngles)
						{
							SteelProfile steelProfile = FindSteelProfile(Cell(item2, 2));
							if (steelProfile == null || steelProfile.Kind != ((value == steelPurlins) ? "Purlin" : "Angle bar"))
							{
								throw new Exception("Select a matching profile.");
							}
							if (IsSteelPaintOnly(item2))
							{
								AddPaintOnlyRow(item2, steelProfile.Name, Number(item2, 4), SteelQty(item2, 5));
								continue;
							}
							SteelCutDemand steelCutDemand = AddSteelDemand(item2, steelProfile.Name, Cell(item2, 3), Number(item2, 4), SteelQty(item2, 5), steelProfile.KgM);
							item2.Cells[6].Value = steelProfile.KgM;
							Measure(item2, 7, steelCutDemand.Length * (double)steelCutDemand.Quantity * steelProfile.KgM);
						}
						else if (value == machineBolts || value == anchorBolts)
						{
							if (!new string[2] { "A307", "A325" }.Contains(Cell(item2, 2)))
							{
								throw new Exception("Select A307 or A325.");
							}
							double num3 = Number(item2, 4) / 1000.0;
							int num4 = SteelQty(item2, 5);
							Measure(item2, 6, num3 * (double)num4, "0.000");
							Measure(item2, 7, SteelWeight.Rod(Number(item2, 3), num3, num4));
						}
						else if (value == steelPlates)
						{
							bool flag = Cell(item2, 2) != "";
							if (flag && (Cell(item2, 3) != "" || Cell(item2, 4) != ""))
							{
								throw new Exception("Use area OR length and width, not both.");
							}
							double num5;
							if (flag)
							{
								num5 = Number(item2, 2);
							}
							else
							{
								double num6 = Number(item2, 3);
								double num7 = Number(item2, 4);
								SteelWeight.Positive(num6, num7);
								num5 = num6 * num7;
							}
							int num8 = SteelQty(item2, 6);
							Measure(item2, 7, num5 * (double)num8, "0.000");
							double thicknessMm = Number(item2, 5);
							Measure(item2, 8, SteelWeight.Plate(num5, thicknessMm, num8));
							if (!flag)
							{
								item2.Tag = new PlateDemand
								{
									Member = Cell(item2, 0),
									Length = Number(item2, 3),
									Width = Number(item2, 4),
									ThicknessMm = thicknessMm,
									Quantity = num8
								};
							}
							else
							{
								item2.Cells[10].Value = "Enter dimensions for sheet purchase";
								errors.Add("Steel " + Cell(item2, 0) + ": net plate weight calculated; length and width required to verify whole-sheet purchase.");
							}
						}
						else if (value == crossBracing)
						{
							double num9 = SteelWeight.Diagonal(Number(item2, 2), (Cell(item2, 3) == "") ? 0.0 : Number(item2, 3), Number(item2, 4));
							double num10 = Number(item2, 5);
							int num11 = SteelQty(item2, 6);
							Measure(item2, 7, num9, "0.000");
							Measure(item2, 8, num9 * (double)num11, "0.000");
							if (IsSteelPaintOnly(item2))
							{
								SteelWeight.Positive(num10, num9);
								SteelWeight.Count(num11);
								AddRodPaint(item2, num10, num9, num11);
								item2.Cells[num2].Value = SteelPaintOnlyStatus;
								continue;
							}
							Measure(item2, 9, SteelWeight.Rod(num10, num9, num11));
							item2.Cells[10].Value = num11;
							item2.Cells[11].Value = num10;
							AddRodPaint(item2, num10, num9, num11);
						}
						else
						{
							bool flag2 = Cell(item2, 2) == "Flat bar";
							if (!flag2 && Cell(item2, 2) != "Angle bar")
							{
								throw new Exception("Select angle or flat bar.");
							}
							item2.Cells[3].ReadOnly = false;
							DataGridViewCell dataGridViewCell = item2.Cells[7];
							bool readOnly = (item2.Cells[8].ReadOnly = true);
							dataGridViewCell.ReadOnly = readOnly;
							SteelProfile steelProfile2 = FindSteelProfile(Cell(item2, 3));
							if (steelProfile2 == null || steelProfile2.Kind != (flag2 ? "Flat bar" : "Angle bar"))
							{
								throw new Exception("Choose a matching section.");
							}
							string name = steelProfile2.Name;
							double kgM = steelProfile2.KgM;
							if (IsSteelPaintOnly(item2))
							{
								AddPaintOnlyRow(item2, name, Number(item2, 5), SteelQty(item2, 6));
								continue;
							}
							if (flag2)
							{
								Measure(item2, 7, steelProfile2.B, "0.###");
								Measure(item2, 8, steelProfile2.T, "0.###");
							}
							else
							{
								DataGridViewCell dataGridViewCell2 = item2.Cells[7];
								object value2 = (item2.Cells[8].Value = "");
								dataGridViewCell2.Value = value2;
							}
							SteelCutDemand steelCutDemand2 = AddSteelDemand(item2, name, Cell(item2, 4), Number(item2, 5), SteelQty(item2, 6), kgM);
							Measure(item2, 9, kgM, "0.000");
							Measure(item2, 10, kgM * steelCutDemand2.Length * (double)steelCutDemand2.Quantity);
						}
						item2.Cells[num2].Value = "Estimated";
					}
					catch (Exception ex)
					{
						item2.Cells[num2].Value = ex.Message;
						errors.Add("Steel " + Cell(item2, 0) + ": " + ex.Message);
					}
				}
			}
			CalculatePlatePurchases();
			try
			{
				steelStocks = SteelCutPlanner.Allocate(steelDemands);
			}
			catch (Exception ex2)
			{
				errors.Add("Steel cutting: " + ex2.Message);
			}
			DataGridView[] array = new DataGridView[3] { steelPurlins, steelAngles, fasciaFrame };
			foreach (DataGridView dataGridView in array)
			{
				foreach (DataGridViewRow item3 in (IEnumerable)dataGridView.Rows)
				{
					if (item3.Tag is SteelCutDemand)
					{
						Measure(item3, (dataGridView == fasciaFrame) ? 10 : 7, SteelCutPlanner.PurchaseKg(steelStocks, (SteelCutDemand)item3.Tag));
					}
				}
			}
			steelCutGrid.Rows.Clear();
            UpdateAngleQuantityView();
			foreach (SteelStock steelStock in steelStocks)
			{
				for (int k = 0; k < steelStock.Cuts.Count; k++)
				{
					SteelCut steelCut = steelStock.Cuts[k];
					steelCutGrid.Rows.Add(steelStock.Number, steelStock.Specification, k + 1, steelCut.Demand.Member, steelCut.Demand.Length, steelCut.RemainingAfter, (k + 1 < steelStock.Cuts.Count) ? ("Reused for " + steelStock.Cuts[k + 1].Demand.Member) : "Leftover / stockpile", (k + 1 == steelStock.Cuts.Count) ? ((object)steelStock.Remaining) : "");
				}
			}
		}

		private static void Measure(DataGridViewRow row, int column, double value, string format = "0.00")
		{
			row.Cells[column].Value = value;
			row.Cells[column].Style.Format = format;
		}

		private double SteelTotal(DataGridView grid, int column)
		{
			return (from DataGridViewRow row in grid.Rows
				where !row.IsNewRow && Cell(row, grid.Columns.Count - 1) == "Estimated"
				select row).Sum((DataGridViewRow row) => Convert.ToDouble(row.Cells[column].Value, CultureInfo.InvariantCulture));
		}

		private List<object[]> SteelSummary()
		{
			double num = SteelTotal(machineBolts, 7) + SteelTotal(anchorBolts, 7) + SteelTotal(steelPlates, 8) + SteelTotal(crossBracing, 9);
			List<object[]> list = new List<object[]>();
			list.Add(new object[3]
			{
				"Purlins PURCHASE weight",
				SteelTotal(steelPurlins, 7),
				"kg"
			});
			list.Add(new object[3]
			{
				"Angle bars PURCHASE weight",
				SteelTotal(steelAngles, 7),
				"kg"
			});
			list.Add(new object[3]
			{
				"Metal fascia PURCHASE weight",
				SteelTotal(fasciaFrame, 10),
				"kg"
			});
			list.Add(new object[3]
			{
				"6m stock PURCHASE weight",
				steelStocks.Sum((SteelStock stock) => 6.0 * stock.KgM),
				"kg"
			});
			list.Add(new object[3]
			{
				"Machine bolts shaft-only",
				SteelTotal(machineBolts, 7),
				"kg"
			});
			list.Add(new object[3]
			{
				"Anchor bolts shaft-only",
				SteelTotal(anchorBolts, 7),
				"kg"
			});
			list.Add(new object[3]
			{
				"Steel plates",
				SteelTotal(steelPlates, 8),
				"kg"
			});
			list.Add(new object[3]
			{
				"Steel plates PURCHASE weight",
				plateStocks.Sum((PlateSheet p) => p.BuyKg),
				"kg"
			});
			list.Add(new object[3]
			{
				"Steel plates final offcuts",
				plateStocks.Sum((PlateSheet p) => p.WasteKg),
				"kg"
			});
			list.Add(new object[3]
			{
				"Crossbracing rods",
				SteelTotal(crossBracing, 9),
				"kg"
			});
			list.Add(new object[3]
			{
				"TOTAL purchase steel (stock + nonstock items)",
				steelStocks.Sum((SteelStock stock) => 6.0 * stock.KgM) + num - SteelTotal(steelPlates, 8) + plateStocks.Sum((PlateSheet p) => p.BuyKg),
				"kg"
			});
			list.AddRange(SteelPaintingRows());
			List<object[]> list2 = list;
			foreach (IGrouping<double, DataGridViewRow> item in from DataGridViewRow row in crossBracing.Rows
				where !row.IsNewRow && Cell(row, crossBracing.Columns.Count - 1) == "Estimated" /* Status moved to the end when QTY/Units/Sets/Multiplier were added */
				group row by Number(row, 5))
			{
				list2.Add(new object[3]
				{
					"Turnbuckles dia " + item.Key.ToString("0.###", CultureInfo.InvariantCulture) + "mm",
					item.Sum((DataGridViewRow row) => SteelQty(row, 6)),
					"pcs"
				});
			}
			return list2;
		}

		private SteelCutDemand AddSteelDemand(DataGridViewRow row, string profile, string spec, double length, int qty, double mass)
		{
			if (string.IsNullOrWhiteSpace(spec))
			{
				throw new Exception("Enter material grade/finish to identify interchangeable stock.");
			}
			SteelWeight.Positive(length, mass);
			SteelWeight.Count(qty);
			if (length > 6.000000001 && row.DataGridView!=steelAngles)
			{
				throw new Exception("Cut length exceeds 6m; enter an approved segmented detail separately.");
			}
			SteelCutDemand steelCutDemand = new SteelCutDemand
			{
				Member = Cell(row, 0),
				Specification = profile + " | " + spec.Trim(),
				Length = length,
				Quantity = qty,
				KgM = mass
			};
			if(row.DataGridView==steelAngles)steelDemands.AddRange(AngleSegments.Expand(steelCutDemand));else steelDemands.Add(steelCutDemand);
			AddSteelPaint(row,profile,length,qty);
			row.Tag = steelCutDemand;
			return steelCutDemand;
		}

		private void AppendStructuralSteelReport(StringBuilder b, bool detailed)
		{
			b.AppendLine();
			b.AppendLine("STRUCTURAL STEEL | QUANTITIES & PURCHASE SUMMARY");
			foreach (object[] item in SteelSummary())
			{
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0}: {1:0.###} {2}", item[0], item[1], item[2]));
			}
			foreach (IGrouping<string, SteelStock> item2 in from s in steelStocks
				group s by s.Specification)
			{
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0}: required to buy {1:0.00} kg (6 m stock, compatible offcuts reused)", item2.Key, item2.Sum((SteelStock s) => 6.0 * s.KgM)));
			}
			foreach (KeyValuePair<string, DataGridView> item3 in SteelTables())
			{
				if (item3.Key == "SteelPurlins" || item3.Key == "SteelAngles" || item3.Key == "FasciaFrame")
				{
					continue;
				}
				DataGridView value = item3.Value;
				b.AppendLine(item3.Key);
				foreach (DataGridViewRow item4 in (IEnumerable)value.Rows)
				{
					if (!item4.IsNewRow && !Empty(item4) && Cell(item4, value.Columns.Count - 1) == "Estimated")
					{
						b.AppendLine("  " + Cell(item4, 0) + " | " + Cell(item4, 1) + " | " + ((value == crossBracing) ? ("rod " + Cell(item4, 9) + " kg; " + Cell(item4, 10) + " turnbuckles at dia " + Cell(item4, 11) + "mm; diagonal " + Cell(item4, 7) + "m") : ((value == steelPlates) ? (Cell(item4, 8) + " kg; area " + Cell(item4, 7) + "m2; thickness " + Cell(item4, 5) + " mm; purchase " + Cell(item4, 11) + " kg; sheets " + Cell(item4, 10)) : (Cell(item4, 2) + " dia " + Cell(item4, 3) + "mm; total length " + Cell(item4, 6) + "m; shaft " + Cell(item4, 7) + "kg"))));
					}
				}
			}
			b.AppendLine("Plates: mild steel ASTM A36 / JIS G3101. Thickness input is mm. Purchase masses use nominal sheet dimensions and 7850kg/m3; rounded catalogue masses may differ slightly. Cuts rotate 90 degrees; compatible offcuts are reused within each thickness. Lowest kg among compared feasible guillotine layouts, zero kerf; no global optimum guarantee.");
			foreach (var item5 in from p in plateStocks
				group p by new
				{
					p.ThicknessMm,
					p.Format.Name
				})
			{
				b.AppendLine(item5.Key.ThicknessMm.ToString("0.###", CultureInfo.InvariantCulture) + " mm | " + item5.Key.Name + " | " + item5.Count() + " whole sheets | buy " + item5.Sum((PlateSheet p) => p.BuyKg).ToString("0.00", CultureInfo.InvariantCulture) + " kg | offcut " + item5.Sum((PlateSheet p) => p.WasteKg).ToString("0.00", CultureInfo.InvariantCulture) + " kg");
			}
			b.AppendLine("Density 7850kg/m3. Purlin/angle kg/m from the stated profile; flat bars/plates/rods from dimensions. Hardware weights exclude bolt heads, nuts, washers and turnbuckles. Crossbracing: one same-diameter turnbuckle per line.");
			b.AppendLine("6m stock: best-fit decreasing, shared compatible offcuts, zero cutting kerf. Final leftover counted once per stock; not an optimality guarantee.");
		}

		private void StructuralSteelExcel(List<WorksExcelTable> tables)
		{
			tables.Add(new WorksExcelTable("POW Structural Steel Summary", new string[3] { "Item", "Quantity", "Unit" }, SteelSummary()));
			foreach (KeyValuePair<string, DataGridView> item in SteelTables())
			{
				DataGridView grid = item.Value;
				List<object[]> rows = (from DataGridViewRow r in grid.Rows
					where !r.IsNewRow && !Empty(r)
					select (from c in Enumerable.Range(0, grid.Columns.Count)
						select r.Cells[c].Value ?? "").ToArray()).ToList();
				tables.Add(new WorksExcelTable("POW " + item.Key, (from DataGridViewColumn c in grid.Columns
					select c.HeaderText).ToArray(), rows));
			}
		}

		private void AddRoofing()
		{
			roofSheets = Grid(new string[17]
			{
				"Roof ID", "Description", "Profile", "Total roof length m (slope)", "Total roof width m", "Roof Qty", "Full sheet width m", "Effective cover m", "Order sheet length m (blank=full slope)", "End lap mm",
				"Insulation?", "Insulation type", "Sheets pcs", "Roof area m2", "Sheet buy m2", "Insulation m2", "Status"
			}, 12);
			AddCombo(roofSheets, 2, "Profile", new string[2] { "Longspan Rib", "Longspan Corrugated" });
			roofSheets.Columns.RemoveAt(10);
			roofSheets.Columns.Insert(10, new DataGridViewCheckBoxColumn
			{
				Name = "Insulation",
				HeaderText = "Insulation?",
				TrueValue = "True",
				FalseValue = "False",
				ValueType = typeof(string)
			});
			roofSheets.Columns[1].Width = 220;
			roofSheets.Columns[2].Width = 180;
			roofSheets.Columns[8].Width = 190;
			roofSheets.Columns[11].Width = 180;
			DataGridViewComboBoxColumn dataGridViewComboBoxColumn = new DataGridViewComboBoxColumn
			{
				Name = "CorrugatedLap",
				HeaderText = "Side lap",
				Width = 150
			};
			dataGridViewComboBoxColumn.Items.AddRange("1-1/2", "2-1/2", "1 rib", "2 ribs");
			roofSheets.Columns.Add(dataGridViewComboBoxColumn);
			DataGridViewComboBoxColumn dataGridViewComboBoxColumn2 = new DataGridViewComboBoxColumn
			{
				Name = "RibSize",
				HeaderText = "Rib side lap",
				Width = 110
			};
			dataGridViewComboBoxColumn2.Items.AddRange("1 rib", "2 ribs");
			roofSheets.Columns.Add(dataGridViewComboBoxColumn2);
			dataGridViewComboBoxColumn.DisplayIndex = 3;
			dataGridViewComboBoxColumn2.Visible = false;
			int[] array = new int[5] { 6, 7, 8, 9, 12 };
			foreach (int index in array)
			{
				roofSheets.Columns[index].Visible = false;
			}
			roofSheets.Columns[6].ToolTipText = "DN Super Hi-Corr nominal: 1.070m; DN Hi-Rib nominal: 1.090m. Purchase area uses full nominal width, without deducting the overlap.";
			roofSheets.Columns[7].ToolTipText = "Reference: Hi-Corr 1.5 lap = 0.970m, 2.5 lap = 0.890m. Hi-Rib: 1-rib lap = 1.030m; 2-rib lap = 0.750m.";
			Action<DataGridViewRow> roofDefaults = delegate(DataGridViewRow r)
			{
				/* Owner (18 November 2026, "alisin default values"): a new roofing row starts blank. Profile, side lap,
				   Insulation? and insulation type are chosen by the user; only the hidden end lap keeps its 0. */
				if (!QuantityInputDefaults.IsMultiplier(r.DataGridView.Columns[5].HeaderText)) r.Cells[5].Value = "1";
				r.Cells[9].Value = "0";
			};
			roofSheets.DefaultValuesNeeded += delegate(object s, DataGridViewRowEventArgs e)
			{
				roofDefaults(e.Row);
			};
			roofSheets.RowsAdded += delegate(object s, DataGridViewRowsAddedEventArgs e)
			{
				if (!filling)
				{
					for (int j = e.RowIndex; j < e.RowIndex + e.RowCount; j++)
					{
						if (!roofSheets.Rows[j].IsNewRow && roofSheets.Rows[j].Cells[5].Value == null)
						{
							roofDefaults(roofSheets.Rows[j]);
						}
					}
				}
			};
			roofSheets.CurrentCellDirtyStateChanged += delegate
			{
				if (roofSheets.IsCurrentCellDirty)
				{
					roofSheets.CommitEdit(DataGridViewDataErrorContexts.Commit);
				}
			};
			roofSheets.CellValueChanged += delegate(object s, DataGridViewCellEventArgs e)
			{
				if (!filling && e.RowIndex >= 0 && (e.ColumnIndex == 2 || e.ColumnIndex == 10 || e.ColumnIndex == 17 || e.ColumnIndex == 18))
				{
					if (e.ColumnIndex == 2 && savedRoofProfiles.Any((RoofProfile p) => string.Equals(p.Name, Cell(roofSheets.Rows[e.RowIndex], 2), StringComparison.OrdinalIgnoreCase)))
					{
						filling = true;
						try
						{
							ApplyRoofProfile(roofSheets.Rows[e.RowIndex]);
						}
						finally
						{
							filling = false;
						}
					}
					Recalculate();
				}
			};
			AddGrid("Roofing Sheets & Insulation", roofSheets, "GUIDE: Slope length is the actual inclined roof length including overhang; eave width runs across sheets. Create profiles in Roofing Start, then select a saved profile and its side-lap option here. Full sheet width and effective cover follow the saved profile. Buy area = whole sheets x full nominal width x order length; overlap is not deducted from purchase area. Longspan sheets use the full roof slope length as their order length; there is no end lap. Widths come from the saved profile. Insulation net area uses roof area; whole rolls are pooled by type using Roofing Start roll settings.");
			roofAccessories = Grid(new string[12]
			{
				"Item ID", "Description / fabrication profile", "Type", "Run / blank length m", "Qty", "Developed width mm (optional)", "Commercial piece length m", "Lap mm", "New stock pcs", "Allocated buy length m",
				"Sheet buy m2", "Status"
			}, 8);
			AddCombo(roofAccessories, 2, "Type", new string[4] { "Flashing", "Gutter", "Ridge Roll", "End Flashing Gutter" });
			DataGridViewColumn dataGridViewColumn = roofAccessories.Columns[6];
			DataGridViewColumn dataGridViewColumn2 = roofAccessories.Columns[7];
			DataGridViewColumn dataGridViewColumn3 = roofAccessories.Columns[8];
			bool flag = (roofAccessories.Columns[10].Visible = false);
			bool flag3 = (dataGridViewColumn3.Visible = flag);
			bool visible = (dataGridViewColumn2.Visible = flag3);
			dataGridViewColumn.Visible = visible;
			roofAccessories.Columns[9].HeaderText = "Commercial length to buy m";
			roofAccessories.Columns[1].Width = 220;
			roofAccessories.Columns[2].Width = 190;
			Action<DataGridViewRow> accessoryDefaults = delegate(DataGridViewRow r)
			{
				r.Cells[2].Value = "Flashing";
				if (!QuantityInputDefaults.IsMultiplier(r.DataGridView.Columns[4].HeaderText)) r.Cells[4].Value = "1";
				r.Cells[6].Value = "2.44";
				r.Cells[7].Value = "0";
			};
			roofAccessories.DefaultValuesNeeded += delegate(object s, DataGridViewRowEventArgs e)
			{
				accessoryDefaults(e.Row);
			};
			roofAccessories.RowsAdded += delegate(object s, DataGridViewRowsAddedEventArgs e)
			{
				if (!filling)
				{
					for (int j = e.RowIndex; j < e.RowIndex + e.RowCount; j++)
					{
						if (!roofAccessories.Rows[j].IsNewRow && roofAccessories.Rows[j].Cells[4].Value == null)
						{
							accessoryDefaults(roofAccessories.Rows[j]);
						}
					}
				}
			};
			AddRoofProfiles();
            AddRoofingProjectSpecification();
			AddInsulationSettings();
			SetupRoofProfileLibrary();
			AddGrid("Flashing & Gutters", roofAccessories, "GUIDE: Enter total installation/run length. Commercial piece length is fixed at 2.44m. Lap follows Roofing Start > Flashing & gutter settings (gutter 150mm, ridge roll 300mm, flashing 100mm); end closures use zero lap. Developed width is optional, only for supplementary sheet area. End Flashing Gutter uses the actual blank length/width per closure and number of closures (lap=0). Offcuts are reused only for matching type, fabrication description and developed width. New-stock orders are charged to the longest cut using each stock; rows using shared offcuts may need zero new pieces. No percentage waste or steel kg.");
		}

		private void RecalculateRoofing()
		{
			if (roofProfiles == null)
			{
				return;
			}
			RefreshRoofProfiles();
			DataGridView[] array = new DataGridView[2] { roofSheets, roofAccessories };
			foreach (DataGridView dataGridView in array)
			{
				if (dataGridView == null)
				{
					continue;
				}
				bool flag = dataGridView == roofSheets;
				HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (DataGridViewRow item in (IEnumerable)dataGridView.Rows)
				{
					if (item.IsNewRow)
					{
						continue;
					}
					int num = (flag ? 12 : 8);
					int num2 = (flag ? 16 : 11);
					item.Tag = null;
					for (int j = num; j <= num2; j++)
					{
						item.Cells[j].Value = "";
					}
					if (Empty(item))
					{
						continue;
					}
					try
					{
						if (flag)
						{
							ApplyRoofProfile(item);
						}
						if (Cell(item, 0) == "" || Cell(item, 1) == "")
						{
							throw new Exception("Enter ID and description.");
						}
						if (!hashSet.Add(Cell(item, 0)))
						{
							throw new Exception("Duplicate roofing ID.");
						}
						if (flag)
						{
							bool flag2 = string.Equals(Cell(item, 10), "True", StringComparison.OrdinalIgnoreCase);
							if (flag2 && !InsulationTypes.Contains(Cell(item, 11)))
							{
								throw new Exception("Choose one of the two insulation types or uncheck Insulation.");
							}
							RoofSheetQuantity roofSheetQuantity = (RoofSheetQuantity)(item.Tag = RoofSheetQuantity.Calculate(Number(item, 3), Number(item, 4), Count(item, 5), Number(item, 6), Number(item, 7), 0.0, 0.0, flag2));
							item.Cells[12].Value = roofSheetQuantity.Pieces;
							item.Cells[13].Value = roofSheetQuantity.CoveredArea.ToString("0.00");
							item.Cells[14].Value = roofSheetQuantity.BuyArea.ToString("0.00");
							item.Cells[15].Value = roofSheetQuantity.Insulation.ToString("0.00");
						}
						else
						{
							if (!new string[4] { "Flashing", "Gutter", "Ridge Roll", "End Flashing Gutter" }.Contains(Cell(item, 2)))
							{
								throw new Exception("Choose accessory type.");
							}
							item.Cells[6].Value = "2.44";
							item.Cells[7].Value = AccessoryLapMm(Cell(item, 2)).ToString(CultureInfo.InvariantCulture);
							DataGridViewCell dataGridViewCell = item.Cells[6];
							bool readOnly = (item.Cells[7].ReadOnly = true);
							dataGridViewCell.ReadOnly = readOnly;
							RoofAccessoryQuantity roofAccessoryQuantity = (RoofAccessoryQuantity)(item.Tag = RoofAccessoryQuantity.Calculate(Number(item, 3), Count(item, 4), (Cell(item, 5) == "") ? 0.0 : (Number(item, 5) / 1000.0), Number(item, 6), (Cell(item, 7) == "") ? 0.0 : (Number(item, 7) / 1000.0)));
							item.Cells[8].Value = roofAccessoryQuantity.Pieces;
							item.Cells[9].Value = roofAccessoryQuantity.BuyLength.ToString("0.00");
							item.Cells[10].Value = roofAccessoryQuantity.BuyArea.ToString("0.00");
						}
						item.Cells[num2].Value = "Ready";
					}
					catch (Exception ex)
					{
						errors.Add("Roofing " + Cell(item, 0) + ": " + ex.Message);
						item.Cells[num2].Value = ex.Message;
					}
				}
			}
			ReuseRoofAccessoryOffcuts();
			if (insulationRolls == null)
			{
				return;
			}
			try
			{
				InsulationSummary();
			}
			catch (Exception ex2)
			{
				errors.Add("Insulation roll settings: " + ex2.Message);
			}
		}

		private List<object[]> RoofingSummary()
		{
			List<object[]> list = new List<object[]>();
            list.AddRange(RoofSheetPayQuantities().Select(q=>new object[]{q.Description,q.Quantity,q.Unit}));
			string[] array = new string[4] { "Flashing", "Gutter", "Ridge Roll", "End Flashing Gutter" };
			foreach (string type2 in array)
			{
				IEnumerable<DataGridViewRow> source2 = from DataGridViewRow r in roofAccessories.Rows
					where r.Tag is RoofAccessoryQuantity && Cell(r, 2) == type2
					select r;
				list.Add(new object[3]
				{
					type2,
					source2.Sum((DataGridViewRow r) => ((RoofAccessoryQuantity)r.Tag).BuyLength),
					"m"
				});
			}
			list.AddRange(InsulationSummary());
			list.Add(new object[3]
			{
				"Insulation total (net roof area)",
				(from DataGridViewRow r in roofSheets.Rows
					where r.Tag is RoofSheetQuantity
					select r).Sum((DataGridViewRow r) => ((RoofSheetQuantity)r.Tag).Insulation),
				"m2"
			});
			return list;
		}

		private void RoofingExcel(List<WorksExcelTable> tables)
		{
			tables.Add(new WorksExcelTable("POW Insulation Settings", new string[3] { "Insulation type", "Roll width m", "Roll length m" }, (from DataGridViewRow r in insulationRolls.Rows
				select new object[3]
				{
					Cell(r, 0),
					Cell(r, 1),
					Cell(r, 2)
				}).ToList()));
			tables.Add(new WorksExcelTable("POW Roofing Summary", new string[3] { "Item", "Quantity", "Unit" }, RoofingSummary()));
			List<object[]> list = new List<object[]>();
			DataGridView[] array = new DataGridView[2] { roofSheets, roofAccessories };
			for (int num = 0; num < array.Length; num++)
			{
				foreach (DataGridViewRow item in (IEnumerable)array[num].Rows)
				{
					if (item.Tag is RoofSheetQuantity)
					{
						RoofSheetQuantity roofSheetQuantity = (RoofSheetQuantity)item.Tag;
						list.Add(new object[9]
						{
							Cell(item, 0),
							Cell(item, 1),
							Cell(item, 2),
							roofSheetQuantity.Pieces,
							roofSheetQuantity.SheetLength,
							roofSheetQuantity.BuyArea,
							roofSheetQuantity.CoveredArea,
							roofSheetQuantity.Insulation,
							Cell(item, 11)
						});
					}
					if (item.Tag is RoofAccessoryQuantity)
					{
						RoofAccessoryQuantity roofAccessoryQuantity = (RoofAccessoryQuantity)item.Tag;
						list.Add(new object[9]
						{
							Cell(item, 0),
							Cell(item, 1),
							Cell(item, 2),
							roofAccessoryQuantity.Pieces,
							Number(item, 6),
							roofAccessoryQuantity.BuyArea,
							roofAccessoryQuantity.NetLength,
							0,
							""
						});
					}
				}
			}
			tables.Add(new WorksExcelTable("POW Roofing Detail", new string[9] { "ID", "Description", "Type", "Order pcs", "Order length m/pc", "Sheet buy m2", "Net roof m2 / accessory m", "Insulation m2", "Insulation type" }, list));
		}

		private void AppendRoofingReport(StringBuilder b, bool detailed)
		{
			b.AppendLine();
			b.AppendLine("ROOFING WORKS | PURCHASE SUMMARY");
			foreach (object[] item in RoofingSummary())
			{
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,-51}{1,14:0.00} {2}", item[0], item[1], item[2]));
			}
			b.AppendLine("Accessory offcuts are reused across matching type/description/developed width. New stock is charged once; a row using another row's offcut may show zero new stock. Kerf=0.");
			b.AppendLine("Insulation purchase area is rounded up to full configured roll coverage after pooling each type. Final quantities are m2; no overlap allowance.");
			foreach (DataGridViewRow item2 in (IEnumerable)insulationRolls.Rows)
			{
				b.AppendLine(Cell(item2, 0) + " | Roll " + Cell(item2, 1) + " m x " + Cell(item2, 2) + " m");
			}
			if (!detailed)
			{
				return;
			}
			b.AppendLine();
			b.AppendLine("SHEETS & INSULATION | BY DESCRIPTION");
			foreach (DataGridViewRow item3 in (IEnumerable)roofSheets.Rows)
			{
				RoofSheetQuantity roofSheetQuantity = item3.Tag as RoofSheetQuantity;
				if (roofSheetQuantity != null)
				{
					b.AppendLine(Cell(item3, 0) + " | " + Cell(item3, 1) + " | " + Cell(item3, 2));
					b.AppendLine("  Saved profile: " + Cell(item3, 2) + " | Side lap " + Cell(item3, 17) + " | Cover " + Cell(item3, 7) + " m | Nominal " + Cell(item3, 6) + " m");
					b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Roof: {0} m slope x {1} m eave x {2} = {3:0.00} m2", Cell(item3, 3), Cell(item3, 4), Cell(item3, 5), roofSheetQuantity.CoveredArea));
					b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  {0} columns x {1} rows x {2} roofs = {3} sheets @ {4:0.00} m", roofSheetQuantity.Columns, roofSheetQuantity.Rows, Cell(item3, 5), roofSheetQuantity.Pieces, roofSheetQuantity.SheetLength));
					b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Buy sheet: {0:0.00} m2 | Insulation: {1:0.00} m2 ({2})", roofSheetQuantity.BuyArea, roofSheetQuantity.Insulation, (roofSheetQuantity.Insulation == 0.0) ? "none" : Cell(item3, 11)));
				}
			}
			b.AppendLine();
			b.AppendLine("FLASHING & GUTTERS | BY DESCRIPTION");
			foreach (DataGridViewRow item4 in (IEnumerable)roofAccessories.Rows)
			{
				RoofAccessoryQuantity roofAccessoryQuantity = item4.Tag as RoofAccessoryQuantity;
				if (roofAccessoryQuantity != null)
				{
					b.AppendLine(Cell(item4, 0) + " | " + Cell(item4, 1) + " | " + Cell(item4, 2));
					b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Net run: {0:0.00} m | Commercial length to buy: {1:0.00} m", roofAccessoryQuantity.NetLength, roofAccessoryQuantity.BuyLength));
					b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Developed width: {0} mm | Sheet: {1:0.00} m2 | Lap: {2} mm", Cell(item4, 5), roofAccessoryQuantity.BuyArea, Cell(item4, 7)));
				}
			}
		}

		public ProgramOfWorksForm()
		{
			if (!ViewOnly.Opening && !AccessCodes.CanUseApp(2))
			{
				throw new Exception("Estimator access required.");
			}
			SuspendLayout();tabs.SuspendLayout();
			AppBrand.Apply(this);
			Text = "Bill of Quantities | DTCE Tool Kit";
			base.Width = 1500;
			base.Height = 960;
			MinimumSize = new Size(1100, 740);
			Font = new Font("Segoe UI", 9f);
			base.StartPosition = FormStartPosition.CenterScreen;
#if !ADMIN_APP
var userRenewal=new AccessRenewal(this);FormClosed+=(sender,args)=>userRenewal.Dispose();
#endif
			/* Command bar grows to fit wrapped rows (38 px buttons); a fixed 44 px height hid the second row on narrow windows. */
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				WrapContents = true,
				Padding = new Padding(8, 5, 8, 3),
				Name = "PowCommandBar"
			};
			base.Controls.Add(tabs);
			base.Controls.Add(flowLayoutPanel);
			GridEditHistory.AddCommands(this,flowLayoutPanel);
            Button(flowLayoutPanel, "Save Bill of Quantities", Save);
            /* owner (19 November 2026, "isummarize yung buttons"): Undo, Redo and Save stay on the bar; the other commands are grouped
               in a File menu and a Report menu */
            MenuButton(flowLayoutPanel,"File",new KeyValuePair<string,Action>("Open Bill of Quantities…\tCtrl+O",Open),new KeyValuePair<string,Action>("Save Bill of Quantities As…\tCtrl+Shift+S",SaveAs),new KeyValuePair<string,Action>("Close Bill of Quantities",CloseProgramOfWorks),null,new KeyValuePair<string,Action>("Project files…",ManageProjectFiles),new KeyValuePair<string,Action>("Restore backup…",RestoreBackup));
            /* No Report menu in the command bar: the Reports main tab has them (owner, 19 November 2026). *//* Save status lives in the bottom status bar (owner, 17 November 2026), not between the command buttons. */powSaveIndicator=new Label{Name="PowSaveStatus",AccessibleName="Bill of Quantities save status",Text=""};
			beam = new EstimatorForm(null);
			slab = new SlabEstimatorForm();
			AddHome();
			Embed("Beam Estimate", beam);
			Embed("Slab Estimate", slab);
			wood = OpeningGrid(true);
			aluminum = OpeningGrid(false);
			PlaceOpeningMaterialColumn(wood);
			manualOpeningsGrid = OpeningGrid(false);
			foreach (DataGridView g0 in new DataGridView[2] { aluminum, manualOpeningsGrid }) { g0.Columns[PaintLeafColumn].Visible = false; g0.Columns[PaintJambColumn].Visible = false; }
			manualOpeningsGrid.Columns[22].Visible = (manualOpeningsGrid.Columns[23].Visible = (manualOpeningsGrid.Columns[24].Visible = false));
			walls = WallGrid();
			wood.Columns[2].Visible = (aluminum.Columns[2].Visible = false);
			AddGrid("Door & Window Schedule", wood, "Overall opening dimensions in metres, INCLUDING jamb/frame. Leaf size = opening less jamb thickness. Paint: leaf faces/edges + exposed jamb. Window includes sill. Door thickness is fixed at 45mm. Door hinges/locksets are automatic; set Swing/Sliding/Fixed and leaves/unit. Select deductions in CHB Openings.");
			AddGrid("Aluminum Doors & Windows", aluminum, "Opening dimensions include the full rough opening/frame for CHB deduction. No aluminum painting is included. Select this schedule and quantity in CHB Openings to deduct it from a wall.");
			AddGrid("CHB Estimate", walls, "Enter Width x Height x Qty OR Gross Area (leave the other method blank), then choose 100 or 150 mm thickness. Add / edit openings to deduct up to 3 door/window schedules. Pale-yellow cells are inputs; net areas are automatic.");
			finishSchedule = FinishScheduleGrid();InstallFinishPayItemPicker();AddFinishLocationColumn(finishSchedule);
			AddGrid("Finish Schedule", finishSchedule, "GUIDE: Type a Group name for each Finish ID (example: Porcelain Glazed Tiles and Trims). Reuse the same spelling to combine areas across floors, walls and ceilings. Prescribed names use fixed Excel cells; custom names appear in POW Finish Types.");
			wallFinishes = FinishGrid(true);
			ceilingFinishes = FinishGrid(false);
			floorFinishes = FinishGrid(false);
			otherFinishes = FinishGrid(false);
			AddGrid("Wall Finishes", wallFinishes, "Wall ID links the finish to CHB. Side A/B identify the two wall faces. Choose the Finish ID in the Finish ID column (defined in Finishing Works > Start); its Group controls all summarized areas. Tiles deduct from plaster on selected faces only. Whole wall (auto) uses net CHB area; Dimensions requires the actual opening overlap as deduction per face.");
			AddGrid("Ceiling Finishes", ceilingFinishes, "Enter measurement ID, dimensions and quantity. Deduction is total openings/voids for this entry in m2; it is deducted once. Choose the Finish ID in the Finish ID column (defined in Finishing Works > Start).");
			AddGrid("Floor Finishes", floorFinishes, "Enter finish code, dimensions and quantity. Deduction is total excluded area for this entry in m2; it is deducted once. Choose the Finish ID in the Finish ID column (defined in Finishing Works > Start); its schedule Group controls area totals. No percentage factor is added.");
			AddGrid("Other Finishes", otherFinishes, "Finishes that are not wall, ceiling or floor (for example stair, counter, column or fascia finishes). Enter finish code, dimensions and quantity. Deduction is total excluded area for this entry in m2; it is deducted once. Choose the Finish ID in the Finish ID column; Finish IDs with Location Other (or blank) are listed. Its schedule Group controls area totals.");
			AddMasonryMaterials();
			AddStructuralSteel();
			AddRoofing();
			AddEarthworkFootings();
			AddCanopyEstimates();
			AddRCWalls();
			AddWorksSettings();
			AddFinishingStart();
			AddMouldingFinishes();
			AddPlumbingWorks();
            GridEditHistory.ConfigureExtra(plumbingChambers,()=>structureConfigurations.ToDictionary(pair=>pair.Key,pair=>CalculationStore.Clone(pair.Value),StringComparer.OrdinalIgnoreCase),snapshot=>{structureConfigurations.Clear();if(snapshot!=null)foreach(var pair in (Dictionary<string,LengthConfiguration>)snapshot)structureConfigurations[pair.Key]=CalculationStore.Clone(pair.Value);});
            ConfigureBoqUndo();
            InitializeExtraWorks();
            RefineRCStart();
			TabPage value = new TabPage("Backup computation")
			{
				Controls = { (Control)preview }
			};
			preview.Dock = DockStyle.Fill;
			tabs.TabPages.Add(value);
			tabs.Deselecting += delegate(object s, TabControlCancelEventArgs e)
			{
				if (!CommitPage(e.TabPage))
				{
					e.Cancel = true;
				}
			};
			tabs.SelectedIndexChanged += delegate
			{
				WorksTabChanged();
			};
			beam.WorksChanged += ChildWorksChanged;
			slab.WorksChanged += ChildWorksChanged;
			SetupExcelBackground();
worksExcelTimer.Tick += async delegate
			{
				worksExcelTimer.Stop();
				if (HasPendingWorksEdit())
				{
					worksExcelTimer.Start();
					return;
				}
				try
				{
					await SyncExcelAsync();
				}
				catch (Exception ex)
				{
					ExcelFailure(ex);
				}
			};
			base.FormClosed += delegate
			{
				worksExcelTimer.Dispose();
				DisposeExcelWorker();
			};
			base.FormClosing += OnWorksClosing;
			base.Shown += delegate
			{
				InputFocus.Apply(this);
			};
			OrganizeSections();
            ConfigureRcWorkspaces();
            AddOpeningLintelsTab();AddRebarSummaryTabs();ArrangeRcCommandBars();ArrangeCheckFirst();
            ConfigureAngleQuantityView();
            InstallRecovery();
            /* closing the Program of Works ends its project: the next one starts with blank project information */
            /* closing the Bill of Quantities ends its project: none is selected afterwards (owner, 23 November 2026) */
            FormClosed += (s0, e0) => { if (!Automated) ProjectInfoStore.ClearProject(); };
            InstallStandardSettings();
            InstallRowInclusion();
            AddConstructionSchedule();AddContractDuration();
            OrderWorksTabs();
            PlaceSteelSetColumns();
            if (wood != null) PlaceOpeningPaintColumns(wood);
            PlaceDoorHardwareColumns(wood);PlaceDoorHardwareColumns(aluminum);if(manualOpeningsGrid!=null&&manualOpeningsGrid.Columns.Contains(HingeSizeColumn))foreach(var hw in new[]{HingeSizeColumn,DeadboltColumn,LeafWidthsColumn})manualOpeningsGrid.Columns[hw].Visible=false;
			tabs.Multiline = true;
            ShowMasonryFinishSides();
            ExplainBarSpacingColumns();
			PowTheme.Apply(this);
            CompactMeasurementRows();
            InstallExcelStatusBar();
            InstallAutoSave();
			status.Height = 38;
			status.BackColor = ModernUi.Soft;
			status.ForeColor = PowTheme.Ink;
            tabs.ResumeLayout(false);ResumeLayout(false);
            initializingWorks = false;
			Recalculate();
			saved = DataXml();
			/* A new, empty POW has nothing to save (the beam estimator started with an empty saved state, so every fresh POW asked to save on close). */
			beam.WorksSaved();
			slab.WorksSaved();
		}

		private void OrganizeSections()
		{
			ModernUi.Navigation(tabs);
			Dictionary<string, TabPage> pages = tabs.TabPages.Cast<TabPage>().ToDictionary((TabPage p) => p.Text);
			sheetPages = pages;
			tabs.TabPages.Clear();
			tabs.TabPages.Add(pages["Bill of Quantities"]);
			pages["Reinforced Concrete Start"].Text = "Start";
			AddSection("Reinforced Concrete Works", new TabPage[8]
			{
				pages["Reinforced Concrete Start"],
				pages["Beam Estimate"],
                pages["Column Estimate"],
				pages["Slab Estimate"],
				pages["Footing Estimate"],
                pages["Wall Footing Estimate"],
				pages["RC Wall Estimate"],
                pages["Miscellaneous Structures"]
			});
            /* Owner (18 November 2026): Canopy / Ledge belongs under Miscellaneous Structures (first nested tab). */
            {
                /* Rebuild the order (TabPages.Insert is ignored before the tab control has a window handle). */
                var miscTabs = pages["Miscellaneous Structures"].Controls.OfType<TabControl>().Single();
                var existing = miscTabs.TabPages.Cast<TabPage>().ToList();
                miscTabs.TabPages.Clear();
                miscTabs.TabPages.Add(pages["Canopy / Ledge Estimate"]);
                foreach (var tab in existing) miscTabs.TabPages.Add(tab);
            }
			pages["Earthworks Start"].Text = "Start";
			AddSection("Earthworks", new TabPage[5]
			{
				pages["Earthworks Start"],
				pages["Cut & Fill"],
                pages["Clearing and Grubbing"],
                pages["Cutting of Trees"],
				pages["Gravel Estimate"]
			});
			AddSection("Masonry Works", new TabPage[1] { pages["CHB Estimate"] }.Concat(MasonryNames.Select((string name) => pages[name])).ToArray());
			/* Wood, Aluminum and PVC openings share one schedule (owner, 17 November 2026); the Aluminum grid only receives
			   rows from older saved projects, which are moved into the schedule on recalculation. */
			AddSection("Doors & Windows", new TabPage[1]
			{
				pages["Door & Window Schedule"]
			});
			pages["Finishing Start"].Text = "Start";
			AddSection("Finishing Works", new TabPage[7]
			{
				pages["Finishing Start"],
				pages["Wall Finishes"],
				pages["Ceiling Finishes"],
				pages["Floor Finishes"],
				pages["Other Finishes"],
				pages["Moulding"],
				pages["Railing Estimate"]
			});
			AddSection("Plumbing Works", new TabPage[7]
			{
				pages["Plumbing Summary"],
				pages["Pipe Takeoff"],
				pages["Plumbing Fixtures"],
				pages["Septic Vault - Concrete"],
				pages["Septic Vault - CHB"],
				pages["Catch Basin - CHB"],
				pages["Lined Canal"]
			});
			AddSection("Structural Steel Works", SteelPageNames.Select((string name) => pages[name]).Concat(pages.ContainsKey(SagRodPage) ? new TabPage[1] { pages[SagRodPage] } : new TabPage[0]).ToArray());
			AddSection("Roofing Works", new TabPage[3]
			{
				pages["Roofing Start"],
				pages["Roofing Sheets & Insulation"],
				pages["Flashing & Gutters"]
			});
            AddSection("Carpentry and Joinery Works", CarpentryNames.Select(n=>pages[n]).ToArray());
            AddSection("Pavement Construction", Enumerable.Range(0,6).Select(i=>pages[extraGrids["Pavement"+i].Kind]).ToArray());
            AddSection("Slope Protection Works", new[]{pages["Foundation Fill"],pages["Slope RC Wall"],pages["Slope Slab"],pages["Stone Masonry"]});
			TabPage tabPage = pages["Backup computation"];
			tabPage.Text = "Reports";
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				Height = 52,
				Padding = new Padding(8)
			};
			reportScope.Items.AddRange(new object[] { "Bill of Quantities Summary", "Reinforced Concrete Works", "Masonry Works", "Doors & Windows", "Finishing Works", "Roofing Works", "Earthworks", "Structural Steel Works", "Plumbing Works", "Carpentry and Joinery Works", "Pavement Construction", "Slope Protection Works" });
			reportDetail.Items.AddRange(new object[3] { "Summary", "Detailed", "Extensive" });
			reportScope.SelectedIndex = 0;
			reportDetail.SelectedIndex = 1;
			flowLayoutPanel.Controls.Add(new Label
			{
				Text = "Report",
				Width = 55,
				Padding = new Padding(3, 8, 0, 0)
			});
			flowLayoutPanel.Controls.Add(reportScope);
			flowLayoutPanel.Controls.Add(reportDetail);
			Button(flowLayoutPanel, "Print preview", delegate
			{
				PreviewQuantityEngineeringReport();
			});
			tabPage.Controls.Add(flowLayoutPanel);
			reportScope.SelectedIndexChanged += delegate
			{
				reportPreviewDirty = true;
				RefreshVisibleWorksViews();
			};
			reportDetail.SelectedIndexChanged += delegate
			{
				reportPreviewDirty = true;
				RefreshVisibleWorksViews();
			};
			tabs.TabPages.Add(tabPage);
			tabs.SelectedIndex = 0;
		}

		private void AddSection(string name, TabPage[] pages)
		{
			TabPage tabPage = new TabPage(name);
			TabControl tabControl = new NavigationTabs
			{
				Dock = DockStyle.Fill
			};
			tabControl.TabPages.AddRange(pages);
			if (pages.Length == 1) ((NavigationTabs)tabControl).StripHidden = true;
			tabControl.Deselecting += delegate(object s, TabControlCancelEventArgs e)
			{
				if (!CommitPage(e.TabPage))
				{
					e.Cancel = true;
				}
			};
			tabControl.SelectedIndexChanged += delegate
			{
				WorksTabChanged();
			};
			tabPage.Controls.Add(tabControl);
			tabs.TabPages.Add(tabPage);
		}

		private void ChildWorksChanged(object sender, EventArgs e)
		{
			if (!filling && !snapshottingWorks)
			{
				childWorksDirty = true;
				ScheduleWorksExcel();
			}
		}

		private bool CommitPage(Control page)
		{
			if (page == null)
			{
				return true;
			}
			DataGridView dataGridView = page as DataGridView;
			if (dataGridView != null)
			{
				if (dataGridView.IsCurrentCellInEditMode)
				{
					return dataGridView.EndEdit();
				}
				return true;
			}
			foreach (Control control in page.Controls)
			{
				if (!CommitPage(control))
				{
					return false;
				}
			}
			return true;
		}

		private void WorksTabChanged()
		{
			if (!initializingWorks && !filling)
			{
				if (childWorksDirty)
				{
					Recalculate();
				}
				TabPage tabPage = ActiveLeaf(tabs);
                if(tabPage!=null){PowTheme.Apply(tabPage);foreach(Form embedded in tabPage.Controls.OfType<Form>())if(!embedded.Visible)embedded.Show();}
				if (tabPage != null && tabPage.Parent == pipeGroupTabs)
				{
					ShowPipeGroup();
				}
				RefreshVisibleWorksViews();
			}
		}

		private void RefreshVisibleWorksViews()
		{
			if (tabs.SelectedTab != null)
			{
				string text = tabs.SelectedTab.Text;
				if (text == "Bill of Quantities" && dashboardDirty && true)
				{
					dashboardDirty = false;
					UpdateDashboard();
				}
				if (text == "Construction Schedule")
				{
					RefreshSchedule();
				}
				if (text == "Reports" && reportPreviewDirty)
				{
					UpdateReportPreview();
				}
				TabPage tabPage = ActiveLeaf(tabs);
				if (tabPage != null && tabPage.Text == "Plumbing Summary" && plumbingSummaryDirty)
				{
					plumbingSummaryDirty = false;
					UpdatePlumbingSummary();
				}
				RefreshChamberSheet(tabPage);
			}
		}

		private static TabPage ActiveLeaf(TabControl control)
		{
			TabPage selectedTab = control.SelectedTab;
			if (selectedTab == null)
			{
				return null;
			}
			TabControl tabControl = selectedTab.Controls.OfType<TabControl>().FirstOrDefault();
			if (tabControl != null)
			{
				return ActiveLeaf(tabControl);
			}
			return selectedTab;
		}

		private void UpdateReportPreview(){
            if(renderingReportPreview||reportScope.SelectedItem==null)return;renderingReportPreview=true;try{preview.SetDocument(CalculationDocument.FromEngineering(BuildQuantityEngineeringReport()));}
            catch(Exception ex){var project=ProjectInfoStore.Read();var report=ProfessionalReport.Create("Quantity take-off / input review",project.Name,project.Location,project.Office);report.Status="Review required";report.Section("Pending inputs").Note(ex.Message);preview.SetDocument(CalculationDocument.FromEngineering(report));}
            finally{reportPreviewDirty=false;renderingReportPreview=false;}
        }

		private bool HasPendingWorksEdit()
		{
			TabPage tabPage = ActiveLeaf(tabs);
			if (tabPage == null)
			{
				return false;
			}
			return PendingGridEdit(tabPage);
		}

		private static bool PendingGridEdit(Control root)
		{
			DataGridView dataGridView = root as DataGridView;
			if (dataGridView != null)
			{
				return dataGridView.IsCurrentCellDirty;
			}
			foreach (Control control in root.Controls)
			{
				if (PendingGridEdit(control))
				{
					return true;
				}
			}
			return false;
		}


		internal List<string> WorksValidationIssues()
		{
			return errors.Concat(beam.WorksValidationIssues()).Concat(slab.WorksValidationIssues()).Concat(backupBill.Where(b=>b.ManualInputText!=null&&!BackupBill.ReadQuantity(b.ManualInputText).HasValue).Select(b=>"BOQ "+b.Code+" / "+b.Description+": invalid manual quantity: "+b.ManualInputText)).Distinct()
				.ToList();
		}

		private void ShowWorksIssues()
		{
			Commit();
			Recalculate();
			List<string> list = WorksValidationIssues();
			using (Form form = new Form
			{
				Text = "Review project inputs",
				ClientSize = new Size(900, 600),
				StartPosition = FormStartPosition.CenterParent
			})
			{
				AppBrand.Apply(form);
				form.Controls.Add(new TextBox
				{
					Dock = DockStyle.Fill,
					ReadOnly = true,
					Multiline = true,
					ScrollBars = ScrollBars.Both,
					Font = new Font("Segoe UI", 10f),
					Text = ((list.Count == 0) ? "No input issues found." : ("Use Save Bill of Quantities to retain incomplete inputs. Partial sync/export includes the valid quantities and lists these issues:\r\n\r\n" + string.Join("\r\n\r\n", list)))
				});
				form.ShowDialog(this);
			}
		}


		internal List<WorksExcelTable> WorksExcelTables()
		{
			List<WorksExcelTable> list = new List<WorksExcelTable>();
			List<object[]> list2 = new List<object[]>();
			object[,] value = beam.WorksExcelRows(projectRebarPlan,false);
			object[,] value2 = slab.WorksExcelRows(projectRebarPlan,false);
			KeyValuePair<string, object[,]>[] array = new KeyValuePair<string, object[,]>[2]
			{
				new KeyValuePair<string, object[,]>("Beam", value),
				new KeyValuePair<string, object[,]>("Slab", value2)
			};
			for (int i = 0; i < array.Length; i++)
			{
				KeyValuePair<string, object[,]> keyValuePair = array[i];
				for (int j = 0; j < keyValuePair.Value.GetLength(0); j++)
				{
					object[] array2 = new object[13]
					{
						keyValuePair.Key, null, null, null, null, null, null, null, null, null,
						null, null, null
					};
					for (int k = 0; k < 12; k++)
					{
						array2[k + 1] = keyValuePair.Value[j, k];
					}
					list2.Add(array2);
				}
			}
			list.Add(new WorksExcelTable("POW Rebar", new string[13]
			{
				"Category", "Description", "Mark", "Source", "Grade", "Diameter mm", "Main cut kg", "Ties cut kg", "SFR cut kg", "Lap + offcut kg",
				"Total to buy kg", "Beam length m", "Quantity"
			}, list2));
			object[,] array3 = beam.ConcreteExcelRows();
			object[,] array4 = slab.WorksMeasureRows();
			array = new KeyValuePair<string, object[,]>[2]
			{
				new KeyValuePair<string, object[,]>("POW Beam Measures", array3),
				new KeyValuePair<string, object[,]>("POW Slab Measures", array4)
			};
			for (int i = 0; i < array.Length; i++)
			{
				KeyValuePair<string, object[,]> keyValuePair2 = array[i];
				List<object[]> list3 = new List<object[]>();
				for (int l = 0; l < keyValuePair2.Value.GetLength(0); l++)
				{
					object[] array5 = new object[8];
					for (int m = 0; m < 8; m++)
					{
						array5[m] = keyValuePair2.Value[l, m];
					}
					list3.Add(array5);
				}
				list.Add(new WorksExcelTable(keyValuePair2.Key, new string[8] { "Description", "Mark", "Support", "Quantity", "Concrete m3", "Side / edge forms m2", "Soffit m2", "Total forms m2" }, list3));
			}
			List<object[]> list4 = new List<object[]>();
			foreach (OpeningItem opening in openings)
			{
				list4.Add(new object[14]
				{
					opening.Id,
					opening.Description,
					opening.Wall,
					opening.Material,
					opening.Kind,
					opening.Width,
					opening.Height,
					opening.Quantity,
					opening.Area,
					opening.LeafPaint,
					opening.JambPaint,
					opening.LeafPaint + opening.JambPaint,
					opening.Locks,
					opening.Hinges
				});
			}
			list.Add(new WorksExcelTable("POW Openings", new string[14]
			{
				"Opening ID", "Description", "Wall ID", "Material", "Type", "Width m", "Height m", "Quantity", "Opening m2", "Leaf paint m2",
				"Jamb paint m2", "Total wood paint m2", "Locksets pcs", "Hinges pcs"
			}, list4));
			List<object[]> list5 = new List<object[]>();
			foreach (WallItem regularWall in RegularWalls)
			{
				PlasterTakeoff value3;
				plasterResults.TryGetValue(regularWall.Id, out value3);
				PlasterTakeoff value4;
				paintResults.TryGetValue(regularWall.Id, out value4);
				list5.Add(new object[16]
				{
					regularWall.Id,
					regularWall.Description,
					regularWall.Length,
					regularWall.Height,
					regularWall.Quantity,
					regularWall.Gross,
					regularWall.Deduction(wallOpenings),
					regularWall.Net(wallOpenings),
					regularWall.Blocks(wallOpenings),
					regularWall.Plaster,
					(value3 == null) ? 0.0 : value3.Gross,
					(value3 == null) ? 0.0 : value3.TileDeduction,
					(value3 == null) ? 0.0 : value3.Net,
					regularWall.Paint,
					(value4 == null) ? 0.0 : value4.Net,
					regularWall.ThicknessMm
				});
			}
			list.Add(new WorksExcelTable("POW CHB", new string[16]
			{
				"Wall ID", "Description", "Length m", "Height m", "Quantity", "Gross wall m2", "Openings m2", "Net CHB m2", "CHB pcs", "Plaster sides",
				"Plaster base m2", "Tiles deducted m2", "Net plaster m2", "Paint sides", "Masonry paint m2", "CHB thickness mm"
			}, list5));
			List<object[]> list6 = new List<object[]>();
			int[] array6 = new int[2] { 100, 150 };
			foreach (int thickness in array6)
			{
				list6.Add(new object[2]
				{
					thickness,
					RegularWalls.Where((WallItem w) => w.ThicknessMm == (double)thickness).Sum((WallItem w) => w.Net(wallOpenings))
				});
			}
			list.Add(new WorksExcelTable("POW CHB by Thickness", new string[2] { "CHB thickness mm", "Net area m2" }, list6));
			List<object[]> list7 = new List<object[]>();
			foreach (DataGridViewRow item in (IEnumerable)finishSchedule.Rows)
			{
				if (!item.IsNewRow && !Empty(item))
				{
					list7.Add(new object[3]
					{
						Cell(item, 0),
						Cell(item, 1),
						Cell(item, 2)
					});
				}
			}
			list.Add(new WorksExcelTable("POW Finish Schedule", new string[3] { "Finish ID", "Description", "Group" }, list7));
			List<object[]> list8 = new List<object[]>();
			foreach (FinishItem finishItem in finishItems)
			{
				list8.Add(new object[10]
				{
					finishItem.Id,
					finishItem.Description,
					finishItem.Category,
					finishItem.Code,
					finishItem.Wall,
					finishItem.Kind,
					finishItem.Side,
					finishItem.Basis,
					finishItem.MaterialName ?? "",
					finishItem.Total(FindWall(finishItem.Wall), wallOpenings, finishItems)
				});
			}
			list.Add(new WorksExcelTable("POW Finishes", new string[10] { "Entry ID", "Description", "Category", "Finish code", "Wall ID", "Type", "Side", "Area basis", "Material / Finish Type", "Net finish m2" }, list8));
			foreach (StockAllocation item2 in canopyPlan)
			{
				list2.Add(new object[13]
				{
					"Canopy / Ledge",
					item2.Entry.Description,
					item2.Entry.Beam,
					"Manual canopy / ledge",
					item2.Demand.Grade,
					item2.Demand.Diameter,
					item2.Net,
					0,
					0,
					item2.Splice + item2.Waste,
					item2.Purchase,
					0,
					int.Parse(item2.Entry.Quantity)
				});
			}
			foreach (StockAllocation item3 in footingPlan)
			{
				list2.Add(new object[13]
				{
					"Footing",
					item3.Entry.Description,
					item3.Entry.Beam,
					"Manual footing",
					item3.Demand.Grade,
					item3.Demand.Diameter,
					item3.Net,
					0,
					0,
					item3.Splice + item3.Waste,
					item3.Purchase,
					0,
					int.Parse(item3.Entry.Quantity)
				});
			}
			foreach (StockAllocation item4 in rcWallPlan)
			{
				list2.Add(new object[13]
				{
					"RC Wall",
					item4.Entry.Description,
					item4.Entry.Beam,
					"Manual RC wall",
					item4.Demand.Grade,
					item4.Demand.Diameter,
					item4.Net,
					0,
					0,
					item4.Splice + item4.Waste,
					item4.Purchase,
					0,
					int.Parse(item4.Entry.Quantity)
				});
			}
			AddExtraExcelRows(list2,list);
            MasonryExcel(list);
			StructuralSteelExcel(list);
			Dictionary<string, double> dictionary = new Dictionary<string, double>();
			array6 = new int[2] { 40, 60 };
			for (int i = 0; i < array6.Length; i++)
			{
				int grade = array6[i];
				dictionary["Grade" + grade] = list2.Where((object[] r) => Convert.ToInt32(r[4]) == grade).Sum((object[] r) => Convert.ToDouble(r[10]));
			}
			Func<object[,], int, double> func = (object[,] data, int col) => Enumerable.Range(0, data.GetLength(0)).Sum((int r) => Convert.ToDouble(data[r, col]));
			dictionary["RCWallConcrete"] = rcWallResults.Sum((RCWallTakeoff q) => q.Concrete);
			dictionary["RCWallForms"] = rcWallResults.Sum((RCWallTakeoff q) => q.Forms);
			dictionary["BeamConcrete"] = func(array3, 4);
			dictionary["SlabConcrete"] = func(array4, 4);
			dictionary["FootingConcrete"] = footingResults.Sum((FootingTakeoff x) => x.Concrete);
			dictionary["ConcreteTotal"] = dictionary["BeamConcrete"] + dictionary["SlabConcrete"] + dictionary["FootingConcrete"] + canopyResults.Sum((CanopyTakeoff q) => q.Concrete) + dictionary["RCWallConcrete"] + miscQuantities.Sum(q=>q.Concrete) + wallFootingResults.Sum(q=>q.Concrete);
			dictionary["BeamForms"] = func(array3, 7);
			dictionary["SlabForms"] = func(array4, 7);
			dictionary["FootingForms"] = footingResults.Sum((FootingTakeoff x) => x.Forms);
			dictionary["FormsTotal"] = dictionary["BeamForms"] + dictionary["SlabForms"] + dictionary["FootingForms"] + canopyResults.Sum((CanopyTakeoff q) => q.Forms) + dictionary["RCWallForms"] + miscQuantities.Sum(q=>q.Forms) + wallFootingResults.Sum(q=>q.Forms);
			dictionary["CHBArea"] = RegularWalls.Sum((WallItem w) => w.Net(wallOpenings));
			dictionary["CHBCount"] = wallItems.Sum((WallItem w) => w.Blocks(wallOpenings));
			dictionary["CHB100Area"] = RegularWalls.Where((WallItem w) => w.ThicknessMm == 100.0).Sum((WallItem w) => w.Net(wallOpenings));
			dictionary["CHB150Area"] = RegularWalls.Where((WallItem w) => w.ThicknessMm == 150.0).Sum((WallItem w) => w.Net(wallOpenings));
			dictionary["Plaster"] = plasterResults.Values.Sum((PlasterTakeoff p) => p.Net);
			dictionary["MasonryPaint"] = paintResults.Values.Sum((PlasterTakeoff p) => p.Net);
			dictionary["WoodPaint"] = openings.Sum((OpeningItem o) => o.LeafPaint + o.JambPaint);
			dictionary["Leversets"] = openings.Sum((OpeningItem o) => o.Locks);
			dictionary["Hinges"] = openings.Sum((OpeningItem o) => o.Hinges);
			dictionary["WallTiles"] = finishItems.Where((FinishItem f) => f.Category == "Wall" && f.Kind == "Tiles").Sum((FinishItem f) => f.Total(FindWall(f.Wall), wallOpenings, finishItems));
			dictionary["CeilingFinishes"] = finishItems.Where((FinishItem f) => f.Category == "Ceiling").Sum((FinishItem f) => f.Total(null, openings, finishItems));
			dictionary["FloorFinishes"] = finishItems.Where((FinishItem f) => f.Category == "Floor").Sum((FinishItem f) => f.Total(null, openings, finishItems));
			object[,] array7 = beam.EarthworkExcelRows();
			List<object[]> list9 = new List<object[]>();
			for (int num = 0; num < array7.GetLength(0); num++)
			{
				object[] array8 = new object[18];
				for (int num2 = 0; num2 < 18; num2++)
				{
					array8[num2] = array7[num, num2];
				}
				list9.Add(array8);
			}
			list.Add(new WorksExcelTable("POW Beam Earthworks", new string[18]
			{
				"Description", "Mark", "Qty", "Trench width m", "Depth m", "Allowance m/side", "Swell %", "Bank shrink %", "Excavation bank m3", "Backfill compacted m3",
				"Gravel m3", "Excavated loose m3", "Required loose m3", "Surplus loose m3", "Common borrow loose m3", "Other concrete m3", "Embankment from excavation loose m3", "Embankment common borrow loose m3"
			}, list9));
			List<object[]> list10 = new List<object[]>();
			int[] array9 = new int[9] { 8, 9, 10, 11, 12, 13, 14, 16, 17 };
			string[] array10 = new string[9] { "Excavation (bank)", "Backfill / embankment (compacted)", "Gravel bedding (compacted)", "Excavated soil (loose)", "Backfill required (loose)", "Surplus soil (loose)", "Common borrow required (loose)", "Embankment from excavation (loose)", "Embankment common borrow (loose)" };
			for (int num3 = 0; num3 < array9.Length; num3++)
			{
				double num4 = 0.0;
				for (int num5 = 0; num5 < array7.GetLength(0); num5++)
				{
					num4 += Convert.ToDouble(array7[num5, array9[num3]]);
				}
				list10.Add(new object[3]
				{
					array10[num3],
					num4,
					"m3"
				});
			}
			list.Add(new WorksExcelTable("POW Earthwork Summary", new string[3] { "Item (fixed rows)", "Quantity", "Unit" }, list10));
			list.Insert(0, WorksSummaryLayout.Build(dictionary));
			list.Add(FinishTypeCatalog.Table(FinishTypeRegistry.Totals(finishItems, (FinishItem f) => f.Total(FindWall(f.Wall), wallOpenings, finishItems))));
			list.Add(finishTypeCells.Table(FinishTypeRegistry.Totals(finishItems, (FinishItem f) => f.Total(FindWall(f.Wall), wallOpenings, finishItems))));
			list.Add(WorksSummaryLayout.CellMap(finishTypeCells));
			list.RemoveAll((WorksExcelTable t) => t.Name == "POW Rebar");
			list.Add(new WorksExcelTable("POW Rebar", new string[13]
			{
				"Category", "Description", "Mark", "Source", "Grade", "Diameter mm", "Main cut kg", "Ties cut kg", "SFR cut kg", "Lap + offcut kg",
				"Total to buy kg", "Length m", "Qty"
			}, list2));
			RoofingExcel(list);
			EarthworksExcel(list);
			PlumbingExcel(list);
			return list;
		}

		private void Guard()
		{
			if (!AccessCodes.CanUseApp(2))
			{
				throw new Exception("Renew Bill of Quantities / estimator access.");
			}
		}

		private void Embed(string title, Form form)
		{
			HideEmbeddedExcelControls(form);
			foreach (Label item in from label in form.Controls.OfType<Label>()
				where label.Dock == DockStyle.Bottom
				select label)
			{
				item.Visible = false;
			}
			EstimatorForm estimatorForm = form as EstimatorForm;
			if (estimatorForm != null)
			{
				estimatorForm.HideWorksExcelStatus();
                estimatorForm.sourceAttachments.BeforeAdd=file=>{var all=ProjectSources();all.Add(file);};
			}
			var slabForm=form as SlabEstimatorForm;if(slabForm!=null)slabForm.sourceAttachments.BeforeAdd=file=>{var all=ProjectSources();all.Add(file);};
            TabPage tabPage = new TabPage(title);
			form.TopLevel = false;
			form.FormBorderStyle = FormBorderStyle.None;
			form.MinimumSize = Size.Empty;
			form.Dock = DockStyle.Fill;
			tabPage.Controls.Add(form);
			tabs.TabPages.Add(tabPage);
			// Native estimator windows are shown only when their worksheet is selected.
		}

		private static void HideEmbeddedExcelControls(Control owner)
		{
			foreach (Control control in owner.Controls)
			{
				Button button = control as Button;
				if (button != null && (button.Text.IndexOf("Excel", StringComparison.OrdinalIgnoreCase) >= 0 || button.Text == "Save estimate" || button.Text == "Open estimate"))
				{
					control.Visible = false;
				}
				else
				{
					HideEmbeddedExcelControls(control);
				}
			}
		}

		private void AddGrid(string title, DataGridView grid, string note)
		{
			TabPage tabPage = new TabPage(title) { Padding = new Padding(14, 10, 14, 14) };
			Label value = new Label
			{
				Dock = DockStyle.Top,
				Height = 54,
				Text = note,
                Visible = false,
                ForeColor = PowTheme.Muted,
				Padding = new Padding(8)
			};
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				Height = 46,
				Padding = new Padding(5, 2, 5, 2)
			};
            if (!grid.ReadOnly && grid.AllowUserToAddRows)
                RowDeleteAffordance.Attach(grid);

			flowLayoutPanel.Visible = flowLayoutPanel.Controls.Count > 0;
			tabPage.Controls.Add(grid);
			tabPage.Controls.Add(flowLayoutPanel);
			tabPage.Controls.Add(value);
            var heading = new WorksheetHeading { Text=title, Subtitle=grid.ReadOnly?"Computed quantities and source breakdown":"Yellow cells: enter values  ·  Other cells: computed automatically" };
            heading.SetGuide(note);
            tabPage.Controls.Add(heading);
			tabs.TabPages.Add(tabPage);
		}

		/* a command-bar button that opens a menu (null = separator); the menu is kept in Tag */
		private static void MenuButton(FlowLayoutPanel panel, string text, params KeyValuePair<string,Action>?[] items)
		{
			var menu=new ContextMenuStrip();
			foreach(var item in items){if(item==null){menu.Items.Add(new ToolStripSeparator());continue;}var pair=item.Value;string[] parts=pair.Key.Split('\t');var entry=new ToolStripMenuItem(parts[0]);if(parts.Length>1)entry.ShortcutKeyDisplayString=parts[1];entry.Click+=(s,e)=>{try{pair.Value();}catch(Exception ex){MessageBox.Show(ex.Message,"Bill of Quantities");}};menu.Items.Add(entry);}
			var button=new Button{Text=text+"  ▾",AutoSize=true,Height=30,Tag=menu,AccessibleName=text+" menu"};
			button.Click+=(s,e)=>menu.Show(button,new System.Drawing.Point(0,button.Height));
			button.Disposed+=(s,e)=>menu.Dispose();
			panel.Controls.Add(button);
		}

		private static void Button(FlowLayoutPanel panel, string text, Action action)
		{
			Button button = new Button
			{
				Text = text,
				AutoSize = true,
				Height = 30
			};
			button.Click += delegate
			{
				try
				{
					action();
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "Bill of Quantities");
				}
			};
			panel.Controls.Add(button);
		}

		private DataGridView Grid(string[] headers, int inputCount)
		{
			WorksDataGridView g = new WorksDataGridView
			{
                DefaultValuesScope=()=>new GridInputInitialization(()=>filling,v=>filling=v),BlankNumericInputs=true,
				Dock = DockStyle.Fill,
				AllowUserToAddRows = true,
				RowHeadersVisible = false,
				BackgroundColor = Color.White,
				ColumnHeadersHeight = 55,
				AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
				SelectionMode = DataGridViewSelectionMode.CellSelect
			};
			/* a dropdown or checkbox applies at once, not only after leaving the cell (owner, 21 November 2026: "wala naman nangyayare sa rebar layer
			   if ginawa kong 1 and 2") */
			g.CurrentCellDirtyStateChanged += delegate { if (!filling && g.IsCurrentCellDirty && (g.CurrentCell is DataGridViewComboBoxCell || g.CurrentCell is DataGridViewCheckBoxCell)) { g.CommitEdit(DataGridViewDataErrorContexts.Commit); g.EndEdit(); } };
			foreach (string text in headers)
			{
				g.Columns.Add(text, text);
			}
			g.DefaultCellStyle.Padding = new Padding(4);
			g.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
			for (int j = 0; j < g.Columns.Count; j++)
			{
				g.Columns[j].Width = ((j == 1) ? 200 : 110);
				g.Columns[j].ReadOnly = j >= inputCount;
				if (j < inputCount)
				{
					g.Columns[j].DefaultCellStyle.BackColor = ModernUi.Input;
				}
			}
			g.Columns[g.Columns.Count - 1].Width = 270;
			GridAppearance.Apply(g);
            GridEditHistory.Configure(g,()=>filling||initializingWorks,value=>filling=value,Recalculate);
            // Hidden worksheets are measured only when their tab is first prepared.
            g.ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			object originalCellValue = null;
			g.CellBeginEdit += delegate(object s, DataGridViewCellCancelEventArgs e)
			{
				originalCellValue = g.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
			};
			g.CellEndEdit += delegate(object s, DataGridViewCellEventArgs e)
			{
				if (inputCount > 0 && !filling && !object.Equals(originalCellValue, g.Rows[e.RowIndex].Cells[e.ColumnIndex].Value))
				{
					if (g == plumbingLs)
					{
						CommitManualEntryEdit(g.Rows[e.RowIndex]);
					}
					Recalculate();
				}
			};
			g.RowsRemoved += delegate
			{
				if (inputCount > 0 && !filling)
				{
					Recalculate();
				}
			};
			g.DataError += delegate(object s, DataGridViewDataErrorEventArgs e)
			{
				e.ThrowException = false;
                if(g==roofSheets&&updatingRoofChoices&&(e.ColumnIndex==2||e.ColumnIndex==17))return;
				if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
				{
					g.Rows[e.RowIndex].Cells[e.ColumnIndex].ErrorText = "Check the selected type or numeric value.";
				}
			};
			return g;
		}

		private DataGridView OpeningGrid(bool isWood)
		{
			string[] headers = new string[20]
			{
				"Opening ID", "Description", "Wall ID", "Type", "Width m", "Height m", "Qty (group total)", "Jamb depth m", "Jamb thick m", "Leaf thick m",
				"Paint faces", "Leversets/unit", "Hinges/unit", "Opening m2", "Leaf paint m2", "Jamb paint m2", "Total paint m2", "Locksets pcs", "Hinges pcs", "Status"
			};
			DataGridView g = Grid(headers, 13);
			DataGridViewComboBoxColumn dataGridViewComboBoxColumn = new DataGridViewComboBoxColumn
			{
				HeaderText = "Type",
				Name = "Type",
				Width = 100
			};
			dataGridViewComboBoxColumn.Items.AddRange("Door", "Window", "Opening"); /* "Opening": dimensions only, deducted from its wall (owner, 18 November 2026) */
			g.Columns.RemoveAt(3);
			g.Columns.Insert(3, dataGridViewComboBoxColumn);
			for (int i = 7; i <= 10; i++)
			{
				g.Columns[i].Visible = isWood;
			}
			DataGridViewColumn dataGridViewColumn = g.Columns[14];
			DataGridViewColumn dataGridViewColumn2 = g.Columns[15];
			bool flag = (g.Columns[16].Visible = isWood);
			bool visible = (dataGridViewColumn2.Visible = flag);
			dataGridViewColumn.Visible = visible;
			Action<DataGridViewRow> defaults = delegate(DataGridViewRow r)
			{
				r.Cells[3].Value = "Door";
				if (!QuantityInputDefaults.IsMultiplier(r.DataGridView.Columns[6].HeaderText)) r.Cells[6].Value = "1";
				r.Cells[7].Value = "0.15";
				r.Cells[8].Value = "0.05";
				r.Cells[9].Value = "45";
				r.Cells[10].Value = "2";
				r.Cells[11].Value = (isWood ? "1" : "0");
				r.Cells[12].Value = (isWood ? "3" : "0");
			};
			g.Columns.Add("Leaves/unit", "Door leaves/unit");
			DataGridViewComboBoxColumn dataGridViewComboBoxColumn2 = new DataGridViewComboBoxColumn
			{
				Name = "Operation",
				HeaderText = "Operation",
				Width = 120
			};
			dataGridViewComboBoxColumn2.Items.AddRange("Swing", "Sliding", "Fixed");
			g.Columns.Add(dataGridViewComboBoxColumn2);
			/* Owner (18 November 2026, "huwag mo na ilagay yung operation"): the Operation column is hidden and every opening is
			   counted as Swing; window hardware follows Locksets/unit and Hinges/unit (enter 0 for sliding or fixed). */
			dataGridViewComboBoxColumn2.Visible = false;
			AddMouldingColumns(g);
			AddOpeningMaterialColumn(g, isWood ? "Wood" : "Aluminum");
			AddOpeningPaintColumns(g);
			AddDoorHardwareColumns(g); /* hinge size, leaf widths, deadbolt (owner, 23 November 2026) */
			DataGridViewCellStyle defaultCellStyle = g.Columns[20].DefaultCellStyle;
			Color backColor = (g.Columns[21].DefaultCellStyle.BackColor = ModernUi.Input);
			defaultCellStyle.BackColor = backColor;
			g.DefaultValuesNeeded += delegate(object s, DataGridViewRowEventArgs e)
			{
				defaults(e.Row);
				if (!QuantityInputDefaults.IsMultiplier(e.Row.DataGridView.Columns[20].HeaderText)) e.Row.Cells[20].Value = "1";
				e.Row.Cells[21].Value = "Swing";
			};
			g.RowsAdded += delegate(object s, DataGridViewRowsAddedEventArgs e)
			{
				if (!filling)
				{
					for (int j = e.RowIndex; j < e.RowIndex + e.RowCount; j++)
					{
						if (!g.Rows[j].IsNewRow && g.Rows[j].Cells[3].Value == null)
						{
							defaults(g.Rows[j]);
							if (!QuantityInputDefaults.IsMultiplier(g.Rows[j].DataGridView.Columns[20].HeaderText)) g.Rows[j].Cells[20].Value = "1";
							g.Rows[j].Cells[21].Value = "Swing";
						}
					}
				}
			};
			g.CurrentCellDirtyStateChanged += delegate
			{
				if (g.IsCurrentCellDirty && g.CurrentCell is DataGridViewComboBoxCell)
				{
					g.CommitEdit(DataGridViewDataErrorContexts.Commit);
				}
			};
			g.CellValueChanged += delegate(object s, DataGridViewCellEventArgs e)
			{
				if (!filling && e.ColumnIndex == 3 && e.RowIndex >= 0)
				{
					DataGridViewRow dataGridViewRow = g.Rows[e.RowIndex];
					bool rowWood = OpeningMaterial(dataGridViewRow, isWood) == "Wood";
					dataGridViewRow.Cells[11].Value = ((rowWood && Cell(dataGridViewRow, 3) == "Door") ? "1" : "0");
					dataGridViewRow.Cells[12].Value = ((!rowWood) ? "0" : ((Cell(dataGridViewRow, 3) == "Door") ? "3" : "2"));
					Recalculate();
				}
			};
			g.Columns[4].ToolTipText = "Overall rough-opening width including jamb. Wooden leaf width = opening width - 2 jamb thicknesses.";
			g.Columns[5].ToolTipText = "Overall rough-opening height. Door leaf height subtracts top jamb; window subtracts top and sill.";
			g.Columns[2].ToolTipText = "Must match one CHB / masonry Wall ID. Each opening belongs to exactly one wall group. Wooden jamb depth follows this wall's thickness automatically.";
			g.Columns[6].ToolTipText = "Total number of this opening in the linked wall group; already includes repeated walls.";
			g.Columns[9].HeaderText = "Leaf thickness mm";
			/* Owner (20 November 2026, "pwede ba alisin ito? sa system na lang? yung jamb depth based na lang sa chb or masonry"):
			   jamb depth, jamb thickness and leaf thickness are set by the program and hidden. Jamb depth = thickness of the CHB /
			   masonry wall the opening is assigned to (Unassigned CHB thickness setting when none); jamb thickness from Settings;
			   wooden door leaf 45 mm. The values are still stored in the same columns, so saved projects open unchanged. */
			foreach (int hidden in new[] { 7, 8, 9 }) g.Columns[hidden].Visible = false;
			g.Columns[11].HeaderText = "Locksets/unit";
			g.Columns[11].ToolTipText = "Automatic: one lockset per non-fixed door opening; windows use entered hardware quantities.";
			g.Columns[12].ToolTipText = "Door hinges: minimum 2 per leaf; one per 762mm height or fraction, plus 1 for leaf width >940mm. Multiply by door leaves. Sliding/fixed: zero. This is a quantity allowance; final hinge model depends on load/specification.";
			g.CellValueChanged += delegate(object sender, DataGridViewCellEventArgs args)
			{
				if (!filling && args.RowIndex >= 0 && (args.ColumnIndex == 20 || args.ColumnIndex == 21))
				{
					Recalculate();
				}
			};
			int num = 0;
			int[] array = new int[24]
			{
				0, 1, 3, 22, 20, 21, 4, 5, 6, 7,
				8, 9, 10, 11, 12, 23, 13, 14, 15, 16,
				17, 18, 19, 2
			};
			foreach (int index in array)
			{
				g.Columns[index].DisplayIndex = num++;
			}
			g.Columns[24].DisplayIndex = 4;
			g.Columns[20].Width = 130;
			g.Columns[20].HeaderText = "Leaves / unit";
			g.Columns[20].ToolTipText = "Number of leaves in one door unit. Width is split across leaves for the hinge allowance.";
			g.Columns[21].Width = 130;
			if (isWood)
			{
				array = new int[3] { 7, 8, 9 };
				foreach (int index2 in array)
				{
					g.Columns[index2].ReadOnly = true;
					g.Columns[index2].DefaultCellStyle.BackColor = Color.FromArgb(239, 244, 248);
				}
			}
			g.Columns[7].ToolTipText = "Automatic: thickness of the assigned CHB wall. Use separate opening IDs for different wall thicknesses.";
			g.Columns[8].ToolTipText = "Automatic project jamb thickness from Doors & Windows > Door / Window Settings.";
			return g;
		}

		private string NextWallId(DataGridView grid)
		{
			HashSet<string> hashSet = new HashSet<string>(from r in (from g in MasonryGrids()
					where g != null
					select g).SelectMany((DataGridView g) => g.Rows.Cast<DataGridViewRow>()).Concat(grid.Rows.Cast<DataGridViewRow>())
				select Cell(r, 0), StringComparer.OrdinalIgnoreCase);
			/* Lowest unused W number across all masonry sheets, including the new row's pending ID (owner, 17 November 2026: no gaps). */
			int number = 1;
			string text;
			do
			{
				text = "W" + number++.ToString("000");
			}
			while (hashSet.Contains(text));
			return text;
		}

		private void RefreshWallIds()
		{
			DataGridViewComboBoxColumn dataGridViewComboBoxColumn = wallFinishes.Columns[3] as DataGridViewComboBoxColumn;
			if (dataGridViewComboBoxColumn == null)
			{
				dataGridViewComboBoxColumn = new DataGridViewComboBoxColumn
				{
					Name = "Wall ID",
					HeaderText = "Wall ID",
					Width = 130,
					DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
				};
				wallFinishes.Columns.RemoveAt(3);
				wallFinishes.Columns.Insert(3, dataGridViewComboBoxColumn);
				dataGridViewComboBoxColumn.DefaultCellStyle.BackColor = ModernUi.Input;
				dataGridViewComboBoxColumn.Visible = false; /* Wall ID not shown on Wall Finishes (owner, 20 November 2026) */
			}
			string[] source = (from id in (from r in MasonryGrids().SelectMany((DataGridView g) => g.Rows.Cast<DataGridViewRow>())
					where !r.IsNewRow
					select Cell(r, 0)).Concat(from DataGridViewRow r in wallFinishes.Rows
					where !r.IsNewRow
					select Cell(r, 3))
				where id != ""
				select id).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
			dataGridViewComboBoxColumn.Items.Clear();
			dataGridViewComboBoxColumn.Items.Add("");
			dataGridViewComboBoxColumn.Items.AddRange(source.Cast<object>().ToArray());
		}

		private DataGridView WallGrid()
		{
			DataGridView g = Grid(new string[12]
			{
				"Wall ID", "Description", "Length m", "Height m", "Wall Qty", "Nominal block W m", "Nominal block H m", "Gross m2", "Openings m2", "Net CHB m2",
				"CHB pcs", "Status"
			}, 7);
			Action<DataGridViewRow> defaults = delegate(DataGridViewRow r)
			{
				if (Cell(r, 0) == "")
				{
					r.Cells[0].Value = NextWallId(g);
				}
				if (!QuantityInputDefaults.IsMultiplier(r.DataGridView.Columns[4].HeaderText)) r.Cells[4].Value = "1";
				r.Cells[5].Value = "0.40";
				r.Cells[6].Value = "0.20";
			};
			g.DefaultValuesNeeded += delegate(object s, DataGridViewRowEventArgs e)
			{
				defaults(e.Row);
			};
			g.RowsAdded += delegate(object s, DataGridViewRowsAddedEventArgs e)
			{
				if (!filling)
				{
					for (int i = e.RowIndex; i < e.RowIndex + e.RowCount; i++)
					{
						if (!g.Rows[i].IsNewRow && g.Rows[i].Cells[4].Value == null)
						{
							defaults(g.Rows[i]);
						}
					}
				}
			};
			DataGridViewComboBoxColumn dataGridViewComboBoxColumn = new DataGridViewComboBoxColumn
			{
				Name = "Plaster sides",
				HeaderText = "Plaster sides",
				Width = 125
			};
			dataGridViewComboBoxColumn.Items.AddRange("None", "1 side (A)", "1 side (B)", "2 sides");
			g.Columns.Add(dataGridViewComboBoxColumn);
			g.Columns[12].DefaultCellStyle.BackColor = ModernUi.Input;
			string[] array = new string[3] { "Plaster base m2", "Tiles deducted m2", "Net plaster m2" };
			foreach (string text in array)
			{
				int index = g.Columns.Add(text, text);
				g.Columns[index].ReadOnly = true;
				g.Columns[index].Width = 130;
			}
			g.DefaultValuesNeeded += delegate(object s, DataGridViewRowEventArgs e)
			{
				e.Row.Cells[12].Value = "2 sides";
			};
			g.RowsAdded += delegate(object s, DataGridViewRowsAddedEventArgs e)
			{
				if (!filling)
				{
					for (int i = e.RowIndex; i < e.RowIndex + e.RowCount; i++)
					{
						if (!g.Rows[i].IsNewRow && g.Rows[i].Cells[12].Value == null)
						{
							g.Rows[i].Cells[12].Value = "2 sides";
						}
					}
				}
			};
			g.CurrentCellDirtyStateChanged += delegate
			{
				if (g.IsCurrentCellDirty && g.CurrentCell is DataGridViewComboBoxCell)
				{
					g.CommitEdit(DataGridViewDataErrorContexts.Commit);
				}
			};
			g.CellValueChanged += delegate(object s, DataGridViewCellEventArgs e)
			{
				if (!filling && e.ColumnIndex == 12)
				{
					Recalculate();
				}
			};
			g.Columns[12].ToolTipText = "Two wall faces: A and B. Tiles deduct only from faces selected for plastering.";
			DataGridViewComboBoxColumn dataGridViewComboBoxColumn2 = new DataGridViewComboBoxColumn
			{
				Name = "Paint sides",
				HeaderText = "Masonry paint sides",
				Width = 145
			};
			dataGridViewComboBoxColumn2.Items.AddRange("Same as plaster", "None", "1 side (A)", "1 side (B)", "2 sides");
			g.Columns.Add(dataGridViewComboBoxColumn2);
			dataGridViewComboBoxColumn2.DefaultCellStyle.BackColor = ModernUi.Input;
			g.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "Masonry paint m2",
				HeaderText = "Masonry paint m2",
				ReadOnly = true,
				Width = 140
			});
			g.DefaultValuesNeeded += delegate(object s, DataGridViewRowEventArgs e)
			{
				e.Row.Cells[16].Value = "Same as plaster";
			};
			g.RowsAdded += delegate(object s, DataGridViewRowsAddedEventArgs e)
			{
				if (!filling)
				{
					for (int i = e.RowIndex; i < e.RowIndex + e.RowCount; i++)
					{
						if (!g.Rows[i].IsNewRow && g.Rows[i].Cells[16].Value == null)
						{
							g.Rows[i].Cells[16].Value = "Same as plaster";
						}
					}
				}
			};
			g.CellValueChanged += delegate(object s, DataGridViewCellEventArgs e)
			{
				if (!filling && e.ColumnIndex == 16)
				{
					Recalculate();
				}
			};
			dataGridViewComboBoxColumn2.ToolTipText = "Automatic masonry painting area, excluding openings and tiles. Defaults to plaster sides. Choose None for unpainted walls.";
			g.Columns.Add("CHB thickness mm", "CHB thickness mm");
			AddCombo(g, 18, "CHB thickness mm", new string[2] { "100", "150" });
			g.Columns[18].DefaultCellStyle.BackColor = ModernUi.Input;
			g.DefaultValuesNeeded += delegate(object s, DataGridViewRowEventArgs e)
			{
				e.Row.Cells[18].Value = "150";
			};
			g.RowsAdded += delegate(object s, DataGridViewRowsAddedEventArgs e)
			{
				if (!filling)
				{
					for (int i = e.RowIndex; i < e.RowIndex + e.RowCount; i++)
					{
						if (!g.Rows[i].IsNewRow && g.Rows[i].Cells[18].Value == null)
						{
							g.Rows[i].Cells[18].Value = "150";
						}
					}
				}
			};
			g.Columns.Add("GrossAreaInput", "Gross area m2");
			g.Columns[19].DefaultCellStyle.BackColor = ModernUi.Input;
			g.Columns[19].ToolTipText = "Total area of this wall group BEFORE door/window deductions. Enter once; no quantity multiplier.";
			int[] array2 = new int[6] { 5, 6, 7, 10, 13, 14 };
			foreach (int index2 in array2)
			{
				g.Columns[index2].Visible = false;
			}
			int num2 = 0;
			array2 = new int[14]
			{
				0, 1, 18, 2, 3, 4, 19, 8, 9, 12,
				15, 16, 17, 11
			};
			foreach (int index3 in array2)
			{
				g.Columns[index3].DisplayIndex = num2++;
			}
			g.Columns[0].ReadOnly = true;
			g.Columns[0].HeaderText = "Wall ID (auto)";
			g.Columns[2].HeaderText = "Width m";
			g.Columns[19].HeaderText = "OR Gross area m2";
			g.Columns[19].Width = 145;
			DataGridViewColumn dataGridViewColumn = g.Columns[2];
			string toolTipText = (g.Columns[3].ToolTipText = "Use Width x Height x Qty OR Gross Area. Leave the other method blank.");
			dataGridViewColumn.ToolTipText = toolTipText;
			g.Columns[0].Frozen = true;
			g.Columns[1].Frozen = true;
			g.Columns[18].Frozen = true;
			g.Columns[1].Width = 240;
			g.Columns[11].Width = 250;
			g.Columns[18].Width = 105;
			array2 = new int[5] { 8, 9, 15, 17, 19 };
			foreach (int index4 in array2)
			{
				g.Columns[index4].Width = 125;
				g.Columns[index4].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
			}
			g.Columns[8].HeaderText = "Less openings m2";
			int[] array3 = new int[9] { 0, 1, 18, 2, 3, 4, 19, 8, 9 };
			int[] array4 = new int[9] { 90, 180, 90, 75, 75, 60, 110, 100, 110 };
			for (int num3 = 0; num3 < array3.Length; num3++)
			{
				g.Columns[array3[num3]].Width = array4[num3];
			}
            WallOpeningRowAction.Attach(g, row => {
                g.CurrentCell = row.Cells[1];
                EditWallOpenings(g);
            }, g.Columns[19]);
			EnsureOpeningsIncludedColumn(g);
			/* final display order: ... OR Gross area, Openings button, openings included, Less openings ... (data indexes unchanged) */
			{
				var order = new List<DataGridViewColumn> { g.Columns[0], g.Columns[1], g.Columns[18], g.Columns[2], g.Columns[3], g.Columns[4], g.Columns[19], g.Columns["EditWallOpeningsAction"], g.Columns[OpeningsIncludedColumn], g.Columns[8], g.Columns[9], g.Columns[12], g.Columns[15], g.Columns[16], g.Columns[17], g.Columns[11] };
				foreach (var c in g.Columns.Cast<DataGridViewColumn>()) if (!order.Contains(c)) order.Add(c);
				for (int i = 0; i < order.Count; i++) try { order[i].DisplayIndex = i; } catch (InvalidOperationException) { }
			}
			return g;
		}

		private void ChooseFinishType(DataGridView grid)
		{
			Guard();
			if (grid.CurrentRow == null || grid.CurrentRow.IsNewRow)
			{
				throw new Exception("Select a finish estimate row first.");
			}
			Commit();
			Recalculate();
			Form dialog = new Form
			{
				Text = "Choose Finish ID",
				Width = 950,
				Height = 250,
				StartPosition = FormStartPosition.CenterParent,
				Font = new Font("Segoe UI", 10f)
			};
			try
			{
				Label label = new Label
				{
					Text = "Select an ID defined in Finish Schedule. Group and summary totals follow that ID.",
					Dock = DockStyle.Top,
					Height = 45,
					Padding = new Padding(8)
				};
				ComboBox ids = new ComboBox
				{
					DropDownStyle = ComboBoxStyle.DropDownList,
					Left = 15,
					Top = 60,
					Width = 900
				};
				foreach (string item in finishGroups.Keys.Where(k => FinishFor(k, CategoryOf(grid))).OrderBy((string k) => k))
				{
					ids.Items.Add(item);
				}
				if (ids.Items.Contains(Cell(grid.CurrentRow, 2)))
				{
					ids.SelectedItem = Cell(grid.CurrentRow, 2);
				}
				Label group = new Label
				{
					Left = 15,
					Top = 100,
					Width = 900,
					Height = 45
				};
				ids.SelectedIndexChanged += delegate
				{
					group.Text = ((ids.SelectedItem == null) ? "" : finishGroups[(string)ids.SelectedItem]);
				};
				Button button = new Button
				{
					Text = "Apply",
					Left = 700,
					Top = 150,
					Width = 100
				};
				button.Click += delegate
				{
					if (ids.SelectedItem != null)
					{
						grid.CurrentRow.Cells[2].Value = (string)ids.SelectedItem;
						grid.EndEdit();
						Recalculate();
						dialog.DialogResult = DialogResult.OK;
					}
				};
				dialog.Controls.AddRange(new Control[4] { label, ids, group, button });
				dialog.AcceptButton = button;
				dialog.ShowDialog(this);
			}
			finally
			{
				if (dialog != null)
				{
					((IDisposable)dialog).Dispose();
				}
			}
		}

		private DataGridView FinishScheduleGrid()
		{
			DataGridView dataGridView = Grid(new string[4] { "Finish ID", "Description", "Group (type a name)", "Status" }, 3);
			dataGridView.Columns[2].Width = 330;
			dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; dataGridView.Columns[0].FillWeight = 16; dataGridView.Columns[1].FillWeight = 34; dataGridView.Columns[2].FillWeight = 50; dataGridView.Columns[3].FillWeight = 14;
			dataGridView.Columns[2].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
			dataGridView.Columns[2].ToolTipText = "Type a group name. Matching names combine across all finish IDs. Use the exact prescribed name when linking to a fixed Excel finish group; other names appear in POW Finish Types.";
			dataGridView.CellEndEdit += delegate
			{
				if (!filling)
				{
					Recalculate();
				}
			};
			return dataGridView;
		}

		private void ReadFinishSchedule()
		{
			finishGroups.Clear();
			ReadFinishLocations();
			foreach (DataGridViewRow item in (IEnumerable)finishSchedule.Rows)
			{
				if (item.IsNewRow)
				{
					continue;
				}
				item.Cells[3].Value = "";
				if (!Empty(item))
				{
					string text = Cell(item, 0);
					string text2 = FinishTypeRegistry.Clean(Cell(item, 2));
					if (text == "" || finishGroups.ContainsKey(text))
					{
						item.Cells[3].Value = ((text == "") ? "Enter Finish ID" : "Duplicate Finish ID");
						List<string> list = errors;
						object value = item.Cells[3].Value;
						list.Add("Finish Schedule: " + ((value != null) ? value.ToString() : null));
					}
					else
					{
						finishGroups[text] = text2;
						item.Cells[3].Value = ((text2 == "") ? "Type group to include in grouped totals" : "Ready");
					}
				}
			}
		}

		private void RefreshFinishIDs()
		{
			DataGridView[] array = new DataGridView[4] { wallFinishes, ceilingFinishes, floorFinishes, otherFinishes };
			foreach (DataGridView dataGridView in array)
			{
				DataGridViewComboBoxColumn dataGridViewComboBoxColumn = dataGridView.Columns[2] as DataGridViewComboBoxColumn;
				if (dataGridViewComboBoxColumn == null)
				{
					dataGridViewComboBoxColumn = new DataGridViewComboBoxColumn
					{
						Name = "Finish ID",
						HeaderText = "Finish ID",
						Width = 125,
						DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
					};
					// Keep the replaced column's visibility: Finish IDs are assigned at Finishing Start, not on measurement sheets.
					dataGridViewComboBoxColumn.Visible = dataGridView.Columns[2].Visible;
					dataGridView.Columns.RemoveAt(2);
					dataGridView.Columns.Insert(2, dataGridViewComboBoxColumn);
					dataGridViewComboBoxColumn.DefaultCellStyle.BackColor = ModernUi.Input;
				}
				string category = CategoryOf(dataGridView);
				string[] source = (from v in (from v in finishGroups.Keys.Where(k => FinishFor(k, category)).Concat(from DataGridViewRow r in dataGridView.Rows
							where !r.IsNewRow
							select Cell(r, 2))
						where v != ""
						select v).Distinct(StringComparer.OrdinalIgnoreCase)
					orderby v
					select v).ToArray(); /* only the Finish IDs for this tab's location (v5.1.5) */
				dataGridViewComboBoxColumn.Items.Clear();
				dataGridViewComboBoxColumn.Items.Add("");
				dataGridViewComboBoxColumn.Items.AddRange(source.Cast<object>().ToArray());
			}
		}

		private void ImportLegacyFinishSchedule()
		{
			filling = true;
			try
			{
				DataGridView[] array = new DataGridView[4] { wallFinishes, ceilingFinishes, floorFinishes, otherFinishes };
				foreach (DataGridView dataGridView in array)
				{
					foreach (DataGridViewRow item in (IEnumerable)dataGridView.Rows)
					{
						if (!item.IsNewRow && !(Cell(item, 2) == ""))
						{
							string code = Cell(item, 2);
							DataGridViewRow dataGridViewRow2 = finishSchedule.Rows.Cast<DataGridViewRow>().FirstOrDefault((DataGridViewRow r) => !r.IsNewRow && string.Equals(Cell(r, 0), code, StringComparison.OrdinalIgnoreCase));
							string text = Cell(item, (dataGridView == wallFinishes) ? 14 : 10);
							if (dataGridViewRow2 == null)
							{
								finishSchedule.Rows.Add(code, Cell(item, 1), text);
							}
							else if (Cell(dataGridViewRow2, 2) == "" && text != "")
							{
								dataGridViewRow2.Cells[2].Value = text;
							}
						}
					}
				}
			}
			finally
			{
				filling = false;
			}
		}

		private void ApplyWallOpeningSelection(string wallId, List<OpeningLink> selected, List<OpeningItem> manual)
		{
			if (string.IsNullOrWhiteSpace(wallId) || selected.Count + manual.Count > 3)
			{
				throw new Exception("Maximum 3 opening entries per wall group.");
			}
			if (new HashSet<string>(selected.Select((OpeningLink l) => l.Schedule), StringComparer.OrdinalIgnoreCase).Count != selected.Count)
			{
				throw new Exception("Select a schedule only once per wall; use its Quantity field.");
			}
			List<OpeningLink> list = openingAssignments.Where((OpeningLink l) => !string.Equals(l.Wall, wallId, StringComparison.OrdinalIgnoreCase)).Concat(selected).ToList();
			List<OpeningItem> source = openings.Where((OpeningItem o) => !(o.Material == "Manual") || !string.Equals(o.Wall, wallId, StringComparison.OrdinalIgnoreCase)).Concat(manual).ToList();
			List<string> oldLegacy = (from o in openings
				where o.Material != "Manual" && string.Equals(o.Wall, wallId, StringComparison.OrdinalIgnoreCase)
				select o.Id).ToList();
			List<OpeningItem> list2 = OpeningLinks.Resolve(source.Select((OpeningItem o) => new OpeningItem
			{
				Id = o.Id,
				Description = o.Description,
				Wall = (oldLegacy.Contains(o.Id) ? "" : o.Wall),
				Material = o.Material,
				Kind = o.Kind,
				Width = o.Width,
				Height = o.Height,
				Quantity = o.Quantity
			}).ToList(), list, WallUnits, o => manual.All(m => m != o && !string.Equals(m.Id, o.Id, StringComparison.OrdinalIgnoreCase)) && manualOpeningTotals.Contains(o.Id));
			foreach (OpeningItem m in manual) manualOpeningTotals.Remove(m.Id);
			WallItem wallItem = FindWall(wallId);
			if (wallItem != null)
			{
				wallItem.Net(list2);
			}
			HashSet<string> hashSet = new HashSet<string>(from o in openings
				where !(o.Material == "Manual") || !string.Equals(o.Wall, wallId, StringComparison.OrdinalIgnoreCase)
				select o.Id, StringComparer.OrdinalIgnoreCase);
			foreach (OpeningItem item in manual)
			{
				item.Validate();
				if (!hashSet.Add(item.Id))
				{
					throw new Exception("Duplicate manual opening ID: " + item.Id);
				}
			}
			filling = true;
			try
			{
				DataGridView[] array = new DataGridView[2] { wood, aluminum };
				for (int num = 0; num < array.Length; num++)
				{
					foreach (DataGridViewRow item2 in (IEnumerable)array[num].Rows)
					{
						if (!item2.IsNewRow && string.Equals(Cell(item2, 2), wallId, StringComparison.OrdinalIgnoreCase))
						{
							item2.Cells[2].Value = "";
						}
					}
				}
				foreach (DataGridViewRow item3 in (from DataGridViewRow r in manualOpeningsGrid.Rows
					where !r.IsNewRow && string.Equals(Cell(r, 2), wallId, StringComparison.OrdinalIgnoreCase)
					select r).ToList())
				{
					manualOpeningsGrid.Rows.Remove(item3);
				}
				foreach (OpeningItem item4 in manual)
				{
					manualOpeningsGrid.Rows.Add(item4.Id, item4.Description, item4.Wall, "Door", item4.Width.ToString("R", CultureInfo.InvariantCulture), item4.Height.ToString("R", CultureInfo.InvariantCulture), item4.Quantity.ToString(), "0", "0", "0", "0", "0", "0");
				}
				openingAssignments.Clear();
				openingAssignments.AddRange(list);
			}
			finally
			{
				filling = false;
			}
			Recalculate();
		}

		private void EditWallOpenings()
		{
			EditWallOpenings(walls);
		}

		private void EditWallOpenings(DataGridView sourceGrid)
		{
			Guard();
			Commit();
			Recalculate();
			DataGridViewRow currentRow = sourceGrid.CurrentRow;
			if (currentRow == null || currentRow.IsNewRow || Cell(currentRow, 0) == "")
			{
				throw new Exception("Enter a wall row with a Wall ID first.");
			}
			string wallId = Cell(currentRow, 0);
			Dictionary<string, OpeningItem> available = openings.Where((OpeningItem o) => o.Material != "Manual").ToDictionary((OpeningItem o) => o.Id, StringComparer.OrdinalIgnoreCase);
			List<OpeningItem> list = wallOpenings.Where((OpeningItem o) => string.Equals(o.Wall, wallId, StringComparison.OrdinalIgnoreCase)).ToList();
			if (list.Count > 3)
			{
				throw new Exception("This wall has more than 3 entries.");
			}
			Form dialog = new Form
			{
				Text = "Wall opening deductions | " + wallId,
				Width = 1280,
				Height = 560,
				MinimumSize = new Size(1020, 480),
				StartPosition = FormStartPosition.CenterParent,
				Font = new Font("Segoe UI", 10f)
			};
			try
			{
				AppBrand.Apply(dialog);
				Label value = new Label
				{
					Dock = DockStyle.Top,
					Height = 72,
					Padding = new Padding(8),
					Text = "Choose up to 3 door/window schedules and the quantity to deduct from THIS wall group. Dimensions are automatic.\nThe same schedule can be used on different walls; assigned quantities cannot exceed its total schedule quantity.\nChoose Manual for other openings, or (none) to remove a slot. Opening quantities are wall-group totals."
				};
				DataGridView grid = new DataGridView
				{
					Dock = DockStyle.Fill,
					AllowUserToAddRows = false,
					AllowUserToDeleteRows = false,
					RowHeadersVisible = false,
					BackgroundColor = Color.White
				};
				grid.Name = "WallOpeningDeductions";
				grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
				grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
				grid.RowTemplate.Height = 48;
				grid.AllowUserToResizeRows = false;
				grid.MultiSelect = false;
				grid.RowHeadersVisible = true;
				grid.RowHeadersWidth = 48;
				DataGridViewComboBoxColumn dataGridViewComboBoxColumn = new DataGridViewComboBoxColumn
				{
					HeaderText = "Door / Window Schedule",
					Name = "Schedule",
					Width = 280,
					DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
				};
				dataGridViewComboBoxColumn.Items.AddRange("(none)", "Manual");
				foreach (OpeningItem item in available.Values.OrderBy((OpeningItem o) => o.Id))
				{
					dataGridViewComboBoxColumn.Items.Add(item.Id);
				}
				grid.Columns.Add(dataGridViewComboBoxColumn);
				string[] array = new string[5] { "Description", "Width m", "Height m", "Qty per wall unit", "Manual ID" };
				foreach (string text in array)
				{
					grid.Columns.Add(text, text);
				}
				grid.Columns[1].Width = 280;
				grid.Columns[4].Width = 145;
				grid.Columns[5].Width = 150;
				grid.EditMode = DataGridViewEditMode.EditOnEnter;
				grid.Columns.Add(new DataGridViewTextBoxColumn
				{
					Name = "Deduction",
					HeaderText = "Deduction m2",
					ReadOnly = true,
					Width = 130
				});
				grid.Columns.Add(new DataGridViewTextBoxColumn
				{
					Name = "Remaining",
					HeaderText = "Available qty",
					ReadOnly = true,
					Width = 110
				});
				GridAppearance.Apply(grid);
				grid.ColumnHeadersHeight = 60;
				grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
				grid.DefaultCellStyle.Padding = new Padding(6);
				float[] array2 = new float[8] { 23f, 24f, 9f, 9f, 12f, 14f, 12f, 12f };
				for (int num2 = 0; num2 < grid.Columns.Count; num2++)
				{
					grid.Columns[num2].FillWeight = array2[num2];
					grid.Columns[num2].MinimumWidth = ((num2 < 2) ? 140 : 80);
					grid.Columns[num2].SortMode = DataGridViewColumnSortMode.NotSortable;
					if (num2 >= 2 && num2 != 5)
					{
						grid.Columns[num2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
					}
				}
				grid.Columns[7].HeaderText = "Remaining qty";
				bool updating = true;
				for (int num3 = 0; num3 < 3; num3++)
				{
					int index = grid.Rows.Add("(none)", "", "", "", "", "");
					grid.Rows[index].HeaderCell.Value = (num3 + 1).ToString();
				}
				for (int num4 = 0; num4 < list.Count; num4++)
				{
					OpeningItem openingItem = list[num4];
					grid.Rows[num4].SetValues((openingItem.Material == "Manual") ? "Manual" : openingItem.Id, openingItem.Description, openingItem.Width.ToString("R", CultureInfo.InvariantCulture), openingItem.Height.ToString("R", CultureInfo.InvariantCulture), openingItem.Quantity.ToString(), (openingItem.Material == "Manual") ? openingItem.Id : "");
				}
				Action<DataGridViewRow, bool> refresh = delegate(DataGridViewRow r, bool changed)
				{
					string text2 = Cell(r, 0);
					OpeningItem value2;
					bool flag = available.TryGetValue(text2, out value2);
					if (changed && text2 == "(none)")
					{
						for (int i = 1; i <= 5; i++)
						{
							r.Cells[i].Value = "";
						}
					}
					if (changed && text2 == "Manual")
					{
						for (int j = 1; j <= 5; j++)
						{
							r.Cells[j].Value = "";
						}
					}
					if (flag)
					{
						r.Cells[1].Value = value2.Description;
						r.Cells[2].Value = value2.Width.ToString("R", CultureInfo.InvariantCulture);
						r.Cells[3].Value = value2.Height.ToString("R", CultureInfo.InvariantCulture);
						if (changed)
						{
							if (!QuantityInputDefaults.IsMultiplier(r.DataGridView.Columns[4].HeaderText)) r.Cells[4].Value = "1";
						}
						r.Cells[5].Value = "";
					}
					for (int k = 1; k <= 5; k++)
					{
						r.Cells[k].ReadOnly = text2 == "(none)" || (flag && k != 4);
					}
					for (int l = 1; l < grid.Columns.Count; l++)
					{
						r.Cells[l].Style.BackColor = (r.Cells[l].ReadOnly ? Color.FromArgb(239, 244, 248) : Color.FromArgb(255, 252, 237));
					}
					r.Cells[4].ToolTipText = (flag ? ("Schedule total: " + value2.Quantity + ". This entry is the number assigned to this wall group.") : "Total number of manual openings in this wall group.");
				};
				foreach (DataGridViewRow item2 in (IEnumerable)grid.Rows)
				{
					refresh(item2, false);
				}
				updating = false;
				grid.CurrentCellDirtyStateChanged += delegate
				{
					if (grid.IsCurrentCellDirty && grid.CurrentCell is DataGridViewComboBoxCell)
					{
						grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
					}
				};
				grid.CellValueChanged += delegate(object s, DataGridViewCellEventArgs e)
				{
					if (!updating && e.RowIndex >= 0 && e.ColumnIndex == 0)
					{
						updating = true;
						try
						{
							refresh(grid.Rows[e.RowIndex], true);
						}
						finally
						{
							updating = false;
						}
					}
				};
				Label areaSummary = new Label
				{
					Dock = DockStyle.Bottom,
					Height = 42,
					Padding = new Padding(10)
				};
				Action updateAreas = delegate
				{
					double num5 = 0.0;
					foreach (DataGridViewRow item3 in (IEnumerable)grid.Rows)
					{
						double result = 0.0;
						double result2 = 0.0;
						double result3 = 0.0;
						double.TryParse(Cell(item3, 2), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
						double.TryParse(Cell(item3, 3), NumberStyles.Float, CultureInfo.InvariantCulture, out result2);
						double.TryParse(Cell(item3, 4), NumberStyles.Float, CultureInfo.InvariantCulture, out result3);
						double num6 = ((Cell(item3, 0) == "(none)") ? 0.0 : (result * result2 * result3 * WallUnits(wallId)));
						item3.Cells[6].Value = num6.ToString("0.00");
						num5 += num6;
						OpeningItem source;
						if (available.TryGetValue(Cell(item3, 0), out source))
						{
							int num7 = wallOpenings.Where((OpeningItem o) => o.Material != "Manual" && string.Equals(o.Id, source.Id, StringComparison.OrdinalIgnoreCase) && !string.Equals(o.Wall, wallId, StringComparison.OrdinalIgnoreCase)).Sum((OpeningItem o) => o.Quantity);
							double num8 = 0.0;
							foreach (DataGridViewRow item4 in (IEnumerable)grid.Rows)
							{
								double result4;
								if (string.Equals(Cell(item4, 0), source.Id, StringComparison.OrdinalIgnoreCase) && double.TryParse(Cell(item4, 4), NumberStyles.Float, CultureInfo.InvariantCulture, out result4))
								{
									num8 += result4;
								}
							}
							item3.Cells[7].Value = (double)(source.Quantity - num7) - num8 * WallUnits(wallId);
							item3.Cells[7].Style.ForeColor = (((double)(source.Quantity - num7) - num8 < 0.0) ? Color.Firebrick : PowTheme.Ink);
						}
						else
						{
							item3.Cells[7].Value = "";
						}
					}
					WallItem wallItem = FindWall(wallId);
					double num9 = ((wallItem == null) ? 0.0 : wallItem.Gross);
					areaSummary.Text = "Gross wall: " + num9.ToString("0.00") + " m2     -     Openings: " + num5.ToString("0.00") + " m2     =     Net CHB: " + (num9 - num5).ToString("0.00") + " m2";
					areaSummary.ForeColor = ((num5 > num9) ? Color.Firebrick : PowTheme.Teal);
				};
				grid.CellValueChanged += delegate(object sender, DataGridViewCellEventArgs args)
				{
					if (args.RowIndex >= 0 && args.ColumnIndex < 6 && !updating)
					{
						updateAreas();
					}
				};
				updateAreas();
				FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
				{
					Dock = DockStyle.Bottom,
					Height = 56,
					Padding = new Padding(8),
					FlowDirection = FlowDirection.RightToLeft
				};
				Button button = new Button
				{
					Text = "Apply deductions",
					AutoSize = true
				};
				Button button2 = new Button
				{
					Text = "Cancel",
					AutoSize = true,
					DialogResult = DialogResult.Cancel
				};
				flowLayoutPanel.Controls.Add(button);
				flowLayoutPanel.Controls.Add(button2);
				dialog.Controls.Add(grid);
				dialog.Controls.Add(areaSummary);
				dialog.Controls.Add(flowLayoutPanel);
				dialog.Controls.Add(value);
				PowTheme.Apply(dialog);
				foreach (DataGridViewRow item5 in (IEnumerable)grid.Rows)
				{
					item5.MinimumHeight = 48;
				}
				dialog.AcceptButton = button;
				dialog.CancelButton = button2;
				button.Click += delegate
				{
					try
					{
						grid.EndEdit();
						List<OpeningLink> list2 = new List<OpeningLink>();
						List<OpeningItem> list3 = new List<OpeningItem>();
						foreach (DataGridViewRow item6 in (IEnumerable)grid.Rows)
						{
							string text2 = Cell(item6, 0);
							if (!(text2 == "(none)"))
							{
								if (text2 == "Manual")
								{
									list3.Add(new OpeningItem
									{
										Id = Cell(item6, 5),
										Description = Cell(item6, 1),
										Wall = wallId,
										Material = "Manual",
										Kind = "Door",
										Width = Number(item6, 2),
										Height = Number(item6, 3),
										Quantity = Count(item6, 4)
									});
								}
								else
								{
									list2.Add(new OpeningLink
									{
										Wall = wallId,
										Schedule = text2,
										Quantity = Count(item6, 4)
									});
								}
							}
						}
						ApplyWallOpeningSelection(wallId, list2, list3);
						dialog.DialogResult = DialogResult.OK;
						dialog.Close();
					}
					catch (Exception ex)
					{
						MessageBox.Show(dialog, ex.Message, "Check deductions");
					}
				};
				dialog.ShowDialog(this);
			}
			finally
			{
				if (dialog != null)
				{
					((IDisposable)dialog).Dispose();
				}
			}
		}

		private DataGridView FinishGrid(bool wall)
		{
			DataGridView g = Grid((!wall) ? new string[10] { "Entry ID", "Description", "Finish code", "Length m", "Width m", "Qty", "Deduction m2 (total)", "Gross m2", "Net finish m2", "Status" } : new string[14]
			{
				"Entry ID", "Description", "Finish / tile code", "Wall ID", "Type", "Side", "Area basis", "Length m", "Height m", "Qty (group total)",
				"Opening overlap m2/face", "Net per face m2", "Finish total m2", "Status"
			}, wall ? 11 : 7);
			if (wall)
			{
				AddCombo(g, 4, "Type", new string[3] { "Tiles", "Paint", "Other" });
				AddCombo(g, 5, "Side", new string[3] { "A", "B", "Both" });
				AddCombo(g, 6, "Area basis", new string[2] { "Dimensions", "Whole wall (auto)" });
				g.CurrentCellDirtyStateChanged += delegate
				{
					if (g.IsCurrentCellDirty && g.CurrentCell is DataGridViewComboBoxCell)
					{
						g.CommitEdit(DataGridViewDataErrorContexts.Commit);
					}
				};
				g.CellValueChanged += delegate(object s, DataGridViewCellEventArgs e)
				{
					if (!filling && (e.ColumnIndex == 4 || e.ColumnIndex == 5 || e.ColumnIndex == 6))
					{
						Recalculate();
					}
				};
				g.Columns[10].ToolTipText = "Actual opening area intersecting this finish, per wall face, for the entire entry quantity. Whole wall (auto) calculates opening deductions instead.";
			}
			else
			{
				g.Columns[6].ToolTipText = "Total excluded/void area for all units in this entry. Deducted once, not once per unit.";
			}
			Action<DataGridViewRow> defaults = delegate(DataGridViewRow r)
			{
				if (wall)
				{
					r.Cells[4].Value = "Tiles";
					r.Cells[5].Value = "A";
					r.Cells[6].Value = "Dimensions";
					if (!QuantityInputDefaults.IsMultiplier(r.DataGridView.Columns[9].HeaderText)) r.Cells[9].Value = "1";
					r.Cells[10].Value = "0";
				}
				else
				{
					if (!QuantityInputDefaults.IsMultiplier(r.DataGridView.Columns[5].HeaderText)) r.Cells[5].Value = "1";
					r.Cells[6].Value = "0";
				}
			};
			g.DefaultValuesNeeded += delegate(object s, DataGridViewRowEventArgs e)
			{
				defaults(e.Row);
			};
			g.RowsAdded += delegate(object s, DataGridViewRowsAddedEventArgs e)
			{
				if (!filling)
				{
					for (int i = e.RowIndex; i < e.RowIndex + e.RowCount; i++)
					{
						if (!g.Rows[i].IsNewRow && g.Rows[i].Cells[wall ? 9 : 5].Value == null)
						{
							defaults(g.Rows[i]);
						}
					}
				}
			};
			int material = g.Columns.Add("MaterialName", "Group (from Finish ID)");
			AddFinishTypedAreaColumn(g, wall);
			g.Columns[material].ReadOnly = true;
			g.Columns[material].Width = 300;
			g.Columns[material].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
			g.Columns[material].DisplayIndex = 3;
			g.Columns[material].DefaultCellStyle.BackColor = Color.FromArgb(235, 237, 240);
			g.Columns[material].ToolTipText = "Read-only: change the group in Finish Schedule. Presets: Glazed Tiles, Unglazed Tiles, Granite Stone Countertop, Vinyl Floor, etc. Matching names combine across entries/tabs; spacing and letter case are ignored. Blank remains unclassified.";
			g.CellValueChanged += delegate(object sender, DataGridViewCellEventArgs args)
			{
				if (wall && !filling && args.RowIndex >= 0 && args.ColumnIndex == material)
				{
					DataGridViewRow dataGridViewRow = g.Rows[args.RowIndex];
					string text = FinishTypeCatalog.KindFor(Cell(dataGridViewRow, material));
					if (text != null)
					{
						filling = true;
						try
						{
							dataGridViewRow.Cells[4].Value = text;
						}
						finally
						{
							filling = false;
						}
						Recalculate();
					}
				}
			};
			g.EditingControlShowing += delegate(object sender, DataGridViewEditingControlShowingEventArgs args)
			{
				TextBox textBox = args.Control as TextBox;
				if (textBox != null)
				{
					if (g.CurrentCell != null && g.CurrentCell.ColumnIndex == material)
					{
						AutoCompleteStringCollection autoCompleteStringCollection = new AutoCompleteStringCollection();
						autoCompleteStringCollection.AddRange(FinishTypeCatalog.Names);
						textBox.AutoCompleteCustomSource = autoCompleteStringCollection;
						textBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
						textBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
					}
					else
					{
						textBox.AutoCompleteMode = AutoCompleteMode.None;
						textBox.AutoCompleteSource = AutoCompleteSource.None;
					}
				}
			};
			return g;
		}

		private static void AddCombo(DataGridView g, int index, string name, string[] values)
		{
			DataGridViewComboBoxColumn dataGridViewComboBoxColumn = new DataGridViewComboBoxColumn
			{
				Name = name,
				HeaderText = name,
				Width = 140
			};
			dataGridViewComboBoxColumn.Items.AddRange(values.Cast<object>().ToArray());
			g.Columns.RemoveAt(index);
			g.Columns.Insert(index, dataGridViewComboBoxColumn);
			dataGridViewComboBoxColumn.DefaultCellStyle.BackColor = ModernUi.Input;
		}

		private WallItem FindWall(string id)
		{
			return wallItems.FirstOrDefault((WallItem w) => string.Equals(w.Id.Trim(), (id ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
		}

		private void RecalculateFinishes()
		{
			RefreshWallIds();
			ReadFinishSchedule();
			RefreshFinishIDs();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			KeyValuePair<string, DataGridView>[] array = new KeyValuePair<string, DataGridView>[4]
			{
				new KeyValuePair<string, DataGridView>("Wall", wallFinishes),
				new KeyValuePair<string, DataGridView>("Ceiling", ceilingFinishes),
				new KeyValuePair<string, DataGridView>("Floor", floorFinishes),
				new KeyValuePair<string, DataGridView>("Other", otherFinishes)
			};
			for (int i = 0; i < array.Length; i++)
			{
				KeyValuePair<string, DataGridView> keyValuePair = array[i];
				foreach (DataGridViewRow item in (IEnumerable)keyValuePair.Value.Rows)
				{
					if (item.IsNewRow)
					{
						continue;
					}
					bool flag = keyValuePair.Key == "Wall";
					int num = (flag ? 11 : 7);
					int num2 = (flag ? 13 : 9);
					item.Tag = null;
					for (int j = num; j <= num2; j++)
					{
						item.Cells[j].Value = "";
					}
					if (flag)
					{
						bool flag2 = Cell(item, 6) == "Whole wall (auto)";
						foreach (int k in new int[5] { 7, 8, 9, 10, item.DataGridView.Columns.Contains(FinishTypedAreaColumn) ? item.DataGridView.Columns[FinishTypedAreaColumn].Index : 10 })
						{
							item.Cells[k].ReadOnly = flag2;
							item.Cells[k].Style.BackColor = (flag2 ? Color.FromArgb(235, 237, 240) : ModernUi.Input);
						}
					}
					if (Empty(item))
					{
						continue;
					}
					try
					{
						FinishItem finishItem = new FinishItem
						{
							Id = Cell(item, 0),
							Description = Cell(item, 1),
							Code = Cell(item, 2),
							Category = keyValuePair.Key,
							Wall = (flag ? Cell(item, 3) : ""),
							Kind = (flag ? WallFinishKind(finishGroups.ContainsKey(Cell(item, 2)) ? finishGroups[Cell(item, 2)] : "", Cell(item, 4)) : "Other"),
							MaterialName = (finishGroups.ContainsKey(Cell(item, 2)) ? finishGroups[Cell(item, 2)] : ""),
							Side = (flag ? (Cell(item, 5) == "" ? "A" : Cell(item, 5)) : "A"), /* hidden on Wall Finishes (v5.1.3): blank = one face */
							Basis = (flag ? (Cell(item, 6) == "" ? "Dimensions" : Cell(item, 6)) : "Dimensions")
						};
						if (!flag || !(finishItem.Basis == "Whole wall (auto)"))
						{
							RequireDeduction(item, flag ? 10 : 6);
							string typed = TypedFinishArea(item);
							if (typed != "") finishItem.TypedArea = Number(item, item.DataGridView.Columns[FinishTypedAreaColumn].Index);
							if (typed == "" || Cell(item, flag ? 7 : 3) != "" || Cell(item, flag ? 8 : 4) != "")
							{
								if (typed != "") throw new Exception("Use Length x Height OR the typed area, not both.");
								finishItem.Length = Number(item, flag ? 7 : 3);
								finishItem.Width = Number(item, flag ? 8 : 4);
							}
							finishItem.Quantity = Count(item, flag ? 9 : 5);
							finishItem.Deduction = Number(item, flag ? 10 : 6);
						}
						if (!finishGroups.ContainsKey(finishItem.Code))
						{
							throw new Exception("Assign this entry a Finish ID in Finishing Works > Start.");
						}
						item.Cells[flag ? 14 : 10].Value = finishItem.MaterialName;
						if (flag)
						{
							string text = FinishTypeCatalog.KindFor(finishItem.MaterialName);
							if (text != null)
							{
								finishItem.Kind = text;
								item.Cells[4].Value = text;
							}
						}
						finishItem.Validate();
						if (!hashSet.Add(finishItem.Id))
						{
							throw new Exception("Duplicate finish Entry ID across finish tabs.");
						}
						/* a wall finish entry without Wall ID (owner, 18 November 2026) counts on its own, e.g. an adjustment */
						bool linkedWall = flag && !string.IsNullOrWhiteSpace(finishItem.Wall);
						WallItem wallItem = (linkedWall ? FindWall(finishItem.Wall) : null);
						if (linkedWall && wallItem == null)
						{
							throw new Exception("Wall ID not found or wall has an error.");
						}
						double num3 = finishItem.PerFace(wallItem, wallOpenings);
						if (linkedWall && num3 > wallItem.Net(wallOpenings) + 1E-07)
						{
							throw new Exception("Finish area exceeds the linked net wall area per face.");
						}
						finishItems.Add(finishItem);
						item.Tag = finishItem;
						item.Cells[num].Value = (flag ? finishItem.GrossPerFace(wallItem, wallOpenings) : finishItem.GrossPerFace(null, wallOpenings)).ToString("0.00"); /* Wall Finishes shows Gross m2 like Ceiling / Floor (v5.1.5) */
						item.Cells[num + 1].Value = finishItem.Total(wallItem, wallOpenings).ToString("0.00");
						item.Cells[num2].Value = (string.IsNullOrWhiteSpace(finishItem.MaterialName) ? "Type group in Finish Schedule" : "Ready");
					}
					catch (Exception ex)
					{
						item.Tag = null;
						item.Cells[num2].Value = ex.Message;
						errors.Add(keyValuePair.Key + " finish " + Cell(item, 0) + ": " + ex.Message);
					}
				}
			}
			finishTypeCells.Include(finishItems.Select((FinishItem f) => f.MaterialName));
			DataGridView[] array2 = new DataGridView[4] { wallFinishes, ceilingFinishes, floorFinishes, otherFinishes };
			foreach (DataGridView dataGridView in array2)
			{
				foreach (DataGridViewRow item2 in (IEnumerable)dataGridView.Rows)
				{
					FinishItem finishItem2 = item2.Tag as FinishItem;
					if (finishItem2 != null)
					{
						int num4 = ((dataGridView == wallFinishes) ? 11 : 7);
						WallItem wall = FindWall(finishItem2.Wall);
						item2.Cells[num4 + 1].Value = finishItem2.Total(wall, wallOpenings, finishItems).ToString("0.00");
						if (finishItem2.AutoSurface)
						{
							item2.Cells[num4].Value = "By face";
						}
					}
				}
			}
			foreach (DataGridViewRow item3 in MasonryGrids().SelectMany((DataGridView g) => g.Rows.Cast<DataGridViewRow>()))
			{
				if (item3.IsNewRow)
				{
					continue;
				}
				for (int num5 = 13; num5 <= 15; num5++)
				{
					item3.Cells[num5].Value = "";
				}
				item3.Cells[17].Value = "";
				WallItem wallItem2 = FindWall(Cell(item3, 0));
				if (wallItem2 == null)
				{
					continue;
				}
				try
				{
					PlasterTakeoff plasterTakeoff = PlasterTakeoff.Calculate(wallItem2, wallOpenings, finishItems);
					plasterResults[wallItem2.Id] = plasterTakeoff;
					item3.Cells[13].Value = plasterTakeoff.Gross.ToString("0.00");
					item3.Cells[14].Value = plasterTakeoff.TileDeduction.ToString("0.00");
					item3.Cells[15].Value = plasterTakeoff.Net.ToString("0.00");
					PlasterTakeoff plasterTakeoff2 = PlasterTakeoff.Calculate(wallItem2, wallOpenings, finishItems, (wallItem2.Paint == "Same as plaster") ? wallItem2.Plaster : wallItem2.Paint);
					paintResults[wallItem2.Id] = plasterTakeoff2;
					item3.Cells[17].Value = plasterTakeoff2.Net.ToString("0.00");
					if (WallHasOpeningIssue(wallItem2.Id) || WallHasFinishIssue(wallItem2.Id))
					{
						item3.Cells[11].Value = "Review this wall's finish/opening issue before using totals";
					}
				}
				catch (Exception ex2)
				{
					item3.Cells[11].Value = ex2.Message;
					errors.Add("Wall " + wallItem2.Id + ": " + ex2.Message);
				}
			}
			ApplyUnlinkedFinishDeductions(errors);
		}

		private void AppendFinishReport(StringBuilder b)
		{
			b.AppendLine();
			b.AppendLine("FINISH MATERIAL / TYPE TOTALS");
			foreach (FinishTypeTotal item in FinishTypeRegistry.Totals(finishItems, (FinishItem f) => f.Total(FindWall(f.Wall), wallOpenings, finishItems)))
			{
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,-40}{1,14:0.00} m2", item.Name, item.Area));
			}
			b.AppendLine();
			b.AppendLine("FINISH AREA SUMMARY");
			b.AppendLine(string.Format("{0,-12}{1,-25}{2,16}", "CATEGORY", "FINISH CODE", "NET AREA m2"));
			foreach (var item2 in from f in finishItems
				group f by new { f.Category, f.Code })
			{
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,-12}{1,-25}{2,16:0.00}", item2.Key.Category, item2.Key.Code, item2.Sum((FinishItem f) => f.Total(FindWall(f.Wall), wallOpenings, finishItems))));
			}
			b.AppendLine();
			b.AppendLine("FINISH AREA BACKUP");
			foreach (FinishItem finishItem in finishItems)
			{
				WallItem wall = FindWall(finishItem.Wall);
				b.AppendLine(finishItem.Id + " | " + finishItem.Description + " | " + finishItem.Category + " / " + finishItem.Code + (string.IsNullOrWhiteSpace(finishItem.MaterialName) ? "" : (" | " + finishItem.MaterialName)) + ((finishItem.Category == "Wall") ? (" | Wall " + finishItem.Wall + " / Side " + finishItem.Side) : ""));
				if (finishItem.Category == "Wall" && finishItem.Basis == "Whole wall (auto)")
				{
					b.AppendLine("  Basis: linked whole wall (automatic door/window deductions)");
				}
				else
				{
					b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Dimensions: {0:0.00} x {1:0.00} m x {2} units", finishItem.Length, finishItem.Width, finishItem.Quantity));
				}
				if (finishItem.AutoSurface)
				{
					if (finishItem.OnA)
					{
						b.AppendLine("  Face A: net wall - tiles = " + finishItem.PaintFace(wall, wallOpenings, finishItems, true).ToString("0.00") + " m2");
					}
					if (finishItem.OnB)
					{
						b.AppendLine("  Face B: net wall - tiles = " + finishItem.PaintFace(wall, wallOpenings, finishItems, false).ToString("0.00") + " m2");
					}
					b.AppendLine("  TOTAL FINISH: " + finishItem.Total(wall, wallOpenings, finishItems).ToString("0.00") + " m2");
				}
				else
				{
					b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Area: ({0:0.00} - {1:0.00}) x {2} face(s) = {3:0.00} m2", finishItem.GrossPerFace(wall, wallOpenings), finishItem.DeductPerFace(wall, wallOpenings), finishItem.Faces, finishItem.Total(wall, wallOpenings, finishItems)));
				}
				b.AppendLine();
			}
		}

		private static string Cell(DataGridViewRow r, int n)
		{
			return Convert.ToString(r.Cells[n].Value).Trim();
		}

		private static double Number(DataGridViewRow r, int n)
		{
			double result;
			if (!double.TryParse(Cell(r, n), NumberStyles.Float, CultureInfo.InvariantCulture, out result) || double.IsNaN(result) || double.IsInfinity(result))
			{
				throw new Exception("Check " + r.DataGridView.Columns[n].HeaderText);
			}
			return result;
		}

		private static int Count(DataGridViewRow r, int n)
		{
			double num = Number(r, n);
			if (num < 0.0 || num > 100000.0 || num != Math.Truncate(num))
			{
				throw new Exception("Enter whole numbers for " + r.DataGridView.Columns[n].HeaderText);
			}
			return (int)num;
		}

		private static bool Empty(DataGridViewRow r)
		{
			if (string.IsNullOrWhiteSpace(Cell(r, 0)) && string.IsNullOrWhiteSpace(Cell(r, 1)))
			{
				return string.IsNullOrWhiteSpace(Cell(r, 2));
			}
			return false;
		}

		private void Commit()
		{
			if (boqGrid != null && boqGrid.IsCurrentCellInEditMode && !boqGrid.EndEdit())
			{
				throw new Exception("Correct the BOQ quantity before saving or syncing.");
			}
			if (finishAssignments != null)
			{
				finishAssignments.EndEdit();
			}
			CommitExtraInputs();
            foreach (DataGridView value in Tables().Values)
			{
				if (!value.EndEdit())
				{
					throw new Exception("Correct the active input before saving or syncing.");
				}
			}
		}

		public void Recalculate()
		{
			using (GridBatch.Suspend(this)) RecalculateWorks();
		}

		private void RecalculateWorks()
		{
			if (initializingWorks || filling || wood == null || walls == null || finishSchedule == null || wallFinishes == null)
			{
				return;
			}
			ApplyMemberSchedules(); /* manual column / footing schedules (v1.50.12) */
			filling = true;
			try
			{
				MoveAluminumRowsIntoSchedule();
				SyncOpeningMouldings();
				AssignRowIds();
				FillBlankHiddenMaterials();
				if (borrowSwell != null && borrowShrink != null)
				{
					beam.SetProjectSoilFactors((double)borrowSwell.Value, (double)borrowShrink.Value);
				}
				PruneDeletedWallLinks();
				openings.Clear();
				wallOpenings.Clear();
				wallItems.Clear();
				finishItems.Clear();
				plasterResults.Clear();
				paintResults.Clear();
				errors.Clear();
				HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				DataGridView[] array = new DataGridView[3] { wood, aluminum, manualOpeningsGrid };
				foreach (DataGridView dataGridView in array)
				{
					foreach (DataGridViewRow item in (IEnumerable)dataGridView.Rows)
					{
						if (item.IsNewRow)
						{
							continue;
						}
						for (int j = 13; j < 20; j++)
						{
							item.Cells[j].Value = "";
						}
						item.Cells[23].Value = "";
						if (Empty(item))
						{
							continue;
						}
						try
						{
							bool plainOpening = dataGridView != manualOpeningsGrid && Cell(item, 3) == "Opening";
							string rowMaterial = (dataGridView == manualOpeningsGrid) ? "Manual" : plainOpening ? "Opening" : OpeningMaterial(item, dataGridView == wood);
							bool woodRow = rowMaterial == "Wood";
							if (dataGridView != manualOpeningsGrid) StyleOpeningMaterialCells(item, woodRow);
							if (dataGridView != manualOpeningsGrid) StylePlainOpeningCells(item, plainOpening);
							if (woodRow)
							{
								AutomaticWoodDimensions(item);
							}
							OpeningItem openingItem = new OpeningItem
							{
								Id = Cell(item, 0),
								Description = (plainOpening && Cell(item, 1) == "") ? "Opening" : Cell(item, 1),
								Wall = Cell(item, 2),
								Material = rowMaterial,
								Kind = Cell(item, 3),
								Width = Number(item, 4),
								Height = Number(item, 5),
								Quantity = Count(item, 6),
								JambDepth = (woodRow ? Number(item, 7) : 0.0),
								JambGroups = (woodRow && woodJambGroups.ContainsKey(Cell(item, 0))) ? woodJambGroups[Cell(item, 0)] : null,
								JambThickness = (woodRow ? Number(item, 8) : 0.0),
								LeafThickness = (woodRow ? 0.045 : 0.0),
								Faces = ((woodRow && OpeningPaint(item, PaintLeafColumn)) ? Count(item, 10) : 0), /* owner, 18 November 2026: Paint faces is only needed when the leaf is painted */
								LocksPerUnit = ((!(Cell(item, 3) == "Door") && !(Cell(item, 11) == "")) ? Count(item, 11) : 0),
								HingesPerUnit = ((!(Cell(item, 3) == "Door") && !(Cell(item, 12) == "")) ? Count(item, 12) : 0),
								Leaves = ((Cell(item, 20) == "") ? 1 : Count(item, 20)),
								Operation = "Swing", /* Operation column removed (owner, 18 November 2026) */
								MouldingProfile = Cell(item, 24),
								Exterior = (dataGridView != manualOpeningsGrid && !plainOpening && Cell(item, 22) != "" && bool.Parse(Cell(item, 22))),
								PaintLeaf = OpeningPaint(item, PaintLeafColumn),
								PaintJamb = OpeningPaint(item, PaintLeafColumn), /* one combined paint checkbox (owner, 18 November 2026) */
								HingeSize = RowHingeSize(item), Deadbolt = RowDeadbolt(item), LeafWidths = RowLeafWidths(item) /* door hardware (owner, 23 November 2026) */
							};
							openingItem.Validate();
							if (openingItem.Kind == "Door") openingItem.DoorLeaves(); /* leaf widths checked with the row */
							ApplyMouldingQuantity(openingItem);
							if (!ids.Add(openingItem.Id))
							{
								throw new Exception("Duplicate Opening ID (including other opening tab).");
							}
							openings.Add(openingItem);
							bool flag = openingItem.Kind == "Door" || openingItem.Operation != "Swing" || openingItem.Material == "Manual" || openingItem.Material == "Opening";
							int[] array2 = new int[2] { 11, 12 };
							foreach (int index in array2)
							{
								item.Cells[index].ReadOnly = flag;
								item.Cells[index].Style.BackColor = (flag ? Color.FromArgb(235, 237, 240) : ModernUi.Input);
							}
							if (flag)
							{
								item.Cells[11].Value = openingItem.EffectiveLocks;
								item.Cells[12].Value = openingItem.EffectiveHinges;
							}
							if (openingItem.Wood)
							{
								item.Cells[9].Value = "45";
								item.Cells[9].ReadOnly = true;
								item.Cells[9].Style.BackColor = Color.FromArgb(235, 237, 240);
							}
							item.Cells[13].Value = openingItem.Area.ToString("0.00");
							item.Cells[14].Value = openingItem.LeafPaint.ToString("0.00");
							item.Cells[15].Value = openingItem.JambPaint.ToString("0.00");
							item.Cells[16].Value = (openingItem.LeafPaint + openingItem.JambPaint).ToString("0.00");
							item.Cells[17].Value = openingItem.Locks;
							item.Cells[18].Value = openingItem.Hinges;
							item.Cells[23].Value = openingItem.MouldingLength;
							item.Cells[19].Value = "Ready";
						}
						catch (Exception ex)
						{
							item.Cells[19].Value = ex.Message;
							errors.Add("Opening row " + (item.Index + 1) + ": " + ex.Message);
						}
					}
				}
				try
				{
					ConvertLegacyOpeningTotals();
					wallOpenings.AddRange(OpeningLinks.Resolve(openings, openingAssignments, WallUnits, o => manualOpeningTotals.Contains(o.Id)));
				}
				catch (Exception ex2)
				{
					errors.Add(ex2.Message);
				}
				ids.Clear();
				foreach (DataGridView masonryGrid in MasonryGrids()) EnsureOpeningsIncludedColumn(masonryGrid);
				foreach (DataGridViewRow item2 in MasonryGrids().SelectMany((DataGridView g) => g.Rows.Cast<DataGridViewRow>()).ToList())
				{
					if (item2.IsNewRow)
					{
						continue;
					}
					for (int num = 7; num < 12; num++)
					{
						item2.Cells[num].Value = "";
					}
					if (Empty(item2))
					{
						continue;
					}
					try
					{
						if (Cell(item2, 19) != "" && (Cell(item2, 2) != "" || Cell(item2, 3) != ""))
						{
							throw new Exception("Use Width x Height OR Gross Area, not both. Clear one input method.");
						}
						if (Cell(item2, 19) == "" && (Cell(item2, 2) == "" || Cell(item2, 3) == ""))
						{
							throw new Exception("Enter both Width and Height, or Gross Area only.");
						}
						NormalizeMasonryRow(item2);
						/* Owner (18 November 2026): a wall without a selected thickness is not "Ready"; it is still counted (plaster and
						   paint do not depend on the thickness), with 150 mm assumed for CHB, and flagged until a thickness is chosen.
						   v1.5.1 excluded the whole wall, which also removed its plaster. */
						bool thicknessMissing = Cell(item2, 18) == "";
						WallItem wallItem = new WallItem
						{
							Material = WallMaterial(item2),
							Id = Cell(item2, 0),
							Description = Cell(item2, 1),
							Length = ((Cell(item2, 19) == "") ? Number(item2, 2) : 1.0),
							Height = ((Cell(item2, 19) == "") ? Number(item2, 3) : 1.0),
							Quantity = ((Cell(item2, 19) != "" && Cell(item2, 4) == "") ? 1 : Count(item2, 4)), /* Wall Qty also multiplies a gross area input */
							BlockWidth = ((Cell(item2, 5) == "") ? 0.4 : Number(item2, 5)),
							BlockHeight = ((Cell(item2, 6) == "") ? 0.2 : Number(item2, 6)),
							Plaster = ((Cell(item2, 12) == "") ? "2 sides" : Cell(item2, 12)),
							Paint = ((Cell(item2, 16) == "") ? "Same as plaster" : Cell(item2, 16)),
							ThicknessMm = ((Cell(item2, 18) == "") ? 150.0 : Number(item2, 18)),
							EnteredArea = ((Cell(item2, 19) == "") ? ((double?)null) : new double?(Number(item2, 19)))
						};
						wallItem.Validate();
						if (!ids.Add(wallItem.Id))
						{
							throw new Exception("Duplicate Wall ID: use one row per wall group.");
						}
						double num2 = wallItem.Net(wallOpenings);
						wallItems.Add(wallItem);
						item2.Cells[7].Value = wallItem.Gross.ToString("0.00");
						item2.Cells[8].Value = wallItem.Deduction(wallOpenings).ToString("0.00");
						item2.Cells[9].Value = num2.ToString("0.00");
						item2.Cells[10].Value = wallItem.Blocks(wallOpenings);
						ShowOpeningsIncluded(item2, wallItem.Id);
						item2.Cells[11].Value = (WallHasOpeningIssue(wallItem.Id) ? "Review this wall's invalid opening before using quantities" : thicknessMissing ? "Select the CHB / block thickness (mm); 150 mm assumed for now" : "Ready");
					}
					catch (Exception ex3)
					{
						item2.Cells[11].Value = ex3.Message;
						errors.Add("Wall " + Cell(item2, 0) + ": " + ex3.Message);
					}
				}
				foreach (OpeningItem item3 in wallOpenings.Where((OpeningItem o) => !ids.Contains(o.Wall)))
				{
					errors.Add(item3.Id + ": Wall ID " + item3.Wall + " has no CHB row; not deducted.");
				}
				RecalculateFinishes();
				RefreshFinishAssignments();RefreshFinishGroupTotals();
				RecalculateRoofing();
				RecalculateGravel();
				RecalculateEarthworks();
				UpdateGravelSummary();
				RecalculateCanopies();
				RecalculateRCWalls();
                RecalculateExtraWorks();
				RecalculateStructuralSteel();
				RecalculateMouldings();
				RecalculatePlumbing();
				RefreshProjectRebarPlan();
				UpdateCanopyPurchases();
				UpdateRCWallPurchases();
                UpdateExtraPurchases();
				UpdateMasonryTotals();
				if (!projectQuantitiesKnown && plumbingRebar != null && HasMeasuredInputs() && BillOfQuantities().Any((BillQuantity q) => q.Section != "Bill of Quantities" && (q.Quantity > 1E-10 || q.Sources.Any((BillQuantity source) => source.Quantity > 1E-10))))
				{
					projectQuantitiesKnown = true;
				}
				status.Text = ((errors.Count > 0) ? ("REVIEW: " + errors.Count + " issue(s) | ") : "") + "Wood paint " + openings.Sum((OpeningItem o) => o.LeafPaint + o.JambPaint).ToString("0.00") + " m2 | Net CHB " + RegularWalls.Sum((WallItem w) => w.Net(wallOpenings)).ToString("0.00") + " m2 | CHB 100mm " + RegularWalls.Where((WallItem w) => w.ThicknessMm == 100.0).Sum((WallItem w) => w.Net(wallOpenings)).ToString("0.00") + " m2 | CHB 150mm " + RegularWalls.Where((WallItem w) => w.ThicknessMm == 150.0).Sum((WallItem w) => w.Net(wallOpenings)).ToString("0.00") + " m2 | Plaster " + plasterResults.Values.Sum((PlasterTakeoff p) => p.Net).ToString("0.00") + " m2 | Masonry paint " + paintResults.Values.Sum((PlasterTakeoff p) => p.Net).ToString("0.00") + " m2";
				dashboardDirty = (reportPreviewDirty = (plumbingSummaryDirty = true));
				childWorksDirty = false;
				RefreshVisibleWorksViews();
				homeSummary.Text = status.Text;
				RefreshRebarSummaries();
				recalculatedInputs = DataXml();
			}
			finally
			{
				filling = false;
			}
			ScheduleWorksExcel();
		}

		private string OpeningReport()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("PROGRAM OF WORKS | QUANTITY BACKUP");
			stringBuilder.AppendLine("OPENINGS SUMMARY");
			stringBuilder.AppendLine(string.Format("{0,-10}{1,15}{2,15}{3,13}{4,13}", "MATERIAL", "OPENING m2", "PAINT m2", "LOCKSETS", "HINGES"));
			string[] array = new string[5] { "Wood", "Aluminum", "PVC", "Steel", "Manual" };
			foreach (string material in array)
			{
				IEnumerable<OpeningItem> source = openings.Where((OpeningItem o) => o.Material == material);
				stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,-10}{1,15:0.00}{2,15:0.00}{3,13}{4,13}", material, source.Sum((OpeningItem o) => o.Area), source.Sum((OpeningItem o) => o.LeafPaint + o.JambPaint), source.Sum((OpeningItem o) => o.Locks), source.Sum((OpeningItem o) => o.Hinges)));
			}
			stringBuilder.AppendLine();
			AppendMouldingReport(stringBuilder);
			stringBuilder.AppendLine("OPENING DETAILS");
			foreach (OpeningItem opening in openings)
			{
				stringBuilder.AppendLine(opening.Id + " | " + opening.Description + " | " + opening.Material + " " + opening.Kind + " | Wall " + opening.Wall);
				stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "  {0:0.00} x {1:0.00} m x {2} units = {3:0.00} m2 opening", opening.Width, opening.Height, opening.Quantity, opening.Area));
				if (opening.Wood)
				{
					stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Leaf {0:0.00} x {1:0.00} m | {2} face(s)", opening.LeafWidth, opening.LeafHeight, opening.Faces));
					stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Paint: leaf {0:0.00} + jamb {1:0.00} = {2:0.00} m2", opening.LeafPaint, opening.JambPaint, opening.LeafPaint + opening.JambPaint));
				}
				if (opening.Material != "Manual")
				{
					stringBuilder.AppendLine("  Locksets " + opening.Locks + " pcs | Hinges " + opening.Hinges + " pcs");
				}
				stringBuilder.AppendLine();
			}
			stringBuilder.AppendLine("CHB AREA & BLOCK QUANTITIES");
			foreach (WallItem w in wallItems)
			{
				stringBuilder.AppendLine(w.Id + " | " + w.Description + " | CHB " + w.ThicknessMm.ToString("0.00") + "mm");
				stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, w.EnteredArea.HasValue ? "  Gross wall area: " + w.EnteredArea.Value.ToString("0.00", CultureInfo.InvariantCulture) + " m2 x {2} = {3:0.00} m2" : "  Wall: {0:0.00} x {1:0.00} m x {2} = {3:0.00} m2", w.Length, w.Height, w.Quantity, w.Gross));
				foreach (OpeningItem item in wallOpenings.Where((OpeningItem o) => string.Equals(o.Wall, w.Id, StringComparison.OrdinalIgnoreCase)))
				{
					stringBuilder.AppendLine("    Less " + item.Id + " / " + item.Description + ": " + item.Area.ToString("0.00") + " m2");
				}
				stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "  NET: {0:0.00} - {1:0.00} = {2:0.00} m2", w.Gross, w.Deduction(wallOpenings), w.Net(wallOpenings)));
				stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Blocks: ceil({0:0.00} / ({1:0.00} x {2:0.00})) = {3} pcs", w.Net(wallOpenings), w.BlockWidth, w.BlockHeight, w.Blocks(wallOpenings)));
				if (plasterResults.ContainsKey(w.Id))
				{
					PlasterTakeoff plasterTakeoff = plasterResults[w.Id];
					stringBuilder.AppendLine("  Plaster: " + w.Plaster + " | base " + plasterTakeoff.Gross.ToString("0.00") + " m2");
					foreach (FinishItem item2 in finishItems.Where((FinishItem f) => f.Category == "Wall" && f.Kind == "Tiles" && string.Equals(f.Wall, w.Id, StringComparison.OrdinalIgnoreCase)))
					{
						int num = ((item2.OnA && (w.Plaster == "1 side (A)" || w.Plaster == "2 sides")) ? 1 : 0) + ((item2.OnB && (w.Plaster == "1 side (B)" || w.Plaster == "2 sides")) ? 1 : 0);
						if (num > 0)
						{
							stringBuilder.AppendLine("    Less " + item2.Id + " / " + item2.Code + " / Side " + item2.Side + ": " + (item2.PerFace(w, wallOpenings) * (double)num).ToString("0.00") + " m2");
						}
					}
					stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "  PLASTER: {0:0.00} - tiles {1:0.00} = {2:0.00} m2", plasterTakeoff.Gross, plasterTakeoff.TileDeduction, plasterTakeoff.Net));
				}
				if (paintResults.ContainsKey(w.Id))
				{
					PlasterTakeoff plasterTakeoff2 = paintResults[w.Id];
					stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "  MASONRY PAINT: {0:0.00} - tiles {1:0.00} = {2:0.00} m2", plasterTakeoff2.Gross, plasterTakeoff2.TileDeduction, plasterTakeoff2.Net));
				}
				stringBuilder.AppendLine();
			}
			stringBuilder.AppendLine("TOTAL NET CHB: " + RegularWalls.Sum((WallItem wallItem) => wallItem.Net(wallOpenings)).ToString("0.00") + " m2 | " + wallItems.Sum((WallItem wallItem) => wallItem.Blocks(wallOpenings)) + " pcs");
			foreach (IGrouping<double, WallItem> item3 in from wallItem in wallItems
				group wallItem by wallItem.ThicknessMm into g
				orderby g.Key
				select g)
			{
				stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "CHB {0:0.00}mm: {1:0.00} m2 | {2} pcs", item3.Key, item3.Sum((WallItem wallItem) => wallItem.Net(wallOpenings)), item3.Sum((WallItem wallItem) => wallItem.Blocks(wallOpenings))));
			}
			stringBuilder.AppendLine("TOTAL PLASTER: " + plasterResults.Values.Sum((PlasterTakeoff p) => p.Net).ToString("0.00") + " m2");
			stringBuilder.AppendLine("TOTAL MASONRY PAINT: " + paintResults.Values.Sum((PlasterTakeoff p) => p.Net).ToString("0.00") + " m2");
			AppendFinishReport(stringBuilder);
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("BASIS (applies to all entries)");
			stringBuilder.AppendLine("Wood leaf size = overall opening less jamb thickness at sides/top (and window sill).");
			stringBuilder.AppendLine("Paint = selected leaf faces + edges + jamb perimeter x (depth + 2 thicknesses).");
			stringBuilder.AppendLine("Jamb perimeter: door 2H+W; window 2H+2W. Paint area excludes coat multiplication.");
			stringBuilder.AppendLine("CHB deducts selected door/window schedules using wall-group quantities; hardware uses schedule totals.");
			stringBuilder.AppendLine("Block count uses nominal face size including joints, rounded up per wall group.");
			stringBuilder.AppendLine("Wood doors: 45mm thick. Swing door hinges per leaf: max(2, ceil(height/0.762m)), +1 if width >0.94m.");
			stringBuilder.AppendLine("One lockset per non-fixed door opening; hinges multiply by leaves. Sliding/fixed hinges = 0.");
			stringBuilder.AppendLine("Hinge quantity basis: Hager hinge guidance. Actual hinge type/weight capacity follows hardware specs.");
			stringBuilder.AppendLine("No waste factor, mortar or CHB rebar included.");
			stringBuilder.AppendLine("Plaster / masonry paint = net CHB area x selected faces, less tiles on those faces.");
			stringBuilder.AppendLine("Masonry paint is an area takeoff, without coat/litre conversion. Max 3 openings per wall group.");
			stringBuilder.AppendLine("Whole-wall finishes deduct door/windows automatically; partial finishes use entered overlap.");
			stringBuilder.AppendLine("Finish deduction is per face, group total. Both faces doubles the net area, not the deduction input.");
			stringBuilder.AppendLine("Tile entries must cover distinct areas. No automatic geometric overlap detection.");
			stringBuilder.AppendLine("Whole-wall Paint also deducts tiles; it classifies masonry paint by finish code (do not add twice).");
			stringBuilder.AppendLine("Finish areas are measured areas, without coats or material consumption factors.");
			if (errors.Count > 0)
			{
				stringBuilder.AppendLine();
				stringBuilder.AppendLine("REVIEW REQUIRED");
				foreach (string error in errors)
				{
					stringBuilder.AppendLine(error);
				}
			}
			return stringBuilder.ToString();
		}

		private string Report()
		{
			Guard();
			Commit();
			Recalculate();
			TabPage tabPage = ActiveLeaf(tabs);
			if (tabPage != null && tabPage.Controls.Contains(beam))
			{
				return beam.Report();
			}
			if (tabPage != null && tabPage.Controls.Contains(slab))
			{
				return slab.Report();
			}
			string text = tabs.SelectedTab.Text;
			return BuildWorksReport((text == "Reports") ? reportScope.Text : ((text == "Bill of Quantities") ? "Bill of Quantities Summary" : text), reportDetail.SelectedIndex > 0);
		}

		private string BuildWorksReport(string scope, bool detailed)
		{
			if (errors.Count > 0)
			{
				throw new Exception(string.Join("\n", errors));
			}
			bool flag = scope == "Bill of Quantities Summary";
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("PROGRAM OF WORKS | BACKUP QUANTITIES");
			stringBuilder.AppendLine(scope + " | " + ((reportDetail.SelectedIndex == 2) ? "Extensive" : (detailed ? "Detailed" : "Summary")));
			stringBuilder.AppendLine(new string('=', 92));
			if (flag || scope == "Reinforced Concrete Works")
			{
				AppendConcreteWorks(stringBuilder, detailed);
			}
			if (flag || scope == "Reinforced Concrete Works")
			{
				AppendFootingReport(stringBuilder, detailed);
			}
			if (flag || scope == "Reinforced Concrete Works")
			{
				AppendCanopyReport(stringBuilder);
			}
			if (flag || scope == "Reinforced Concrete Works")
			{
				AppendRCWallReport(stringBuilder, detailed);
                AppendExtraWorksReport(stringBuilder,detailed,scope);
			}
			if (flag || scope == "Earthworks")
			{
				AppendEarthworkReport(stringBuilder, detailed);
			}
			if (flag || scope == "Masonry Works")
			{
				AppendMasonryAreas(stringBuilder, detailed);
			}
			if (flag || scope == "Doors & Windows")
			{
				stringBuilder.AppendLine();
				stringBuilder.AppendLine("DOORS & WINDOWS");
				stringBuilder.AppendLine(string.Format("{0,-15}{1,-12}{2,8}{3,13}{4,13}{5,12}{6,12}", "SCHEDULE", "TYPE", "QTY", "OPENING m2", "WOOD PAINT", "LOCKSETS", "HINGES"));
				foreach (OpeningItem item in openings.Where((OpeningItem o) => o.Material != "Manual"))
				{
					stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,-15}{1,-12}{2,8}{3,13:0.00}{4,13:0.00}{5,12}{6,12}", item.Id, item.Material + " " + item.Kind, item.Quantity, item.Area, item.LeafPaint + item.JambPaint, item.Locks, item.Hinges));
				}
				stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "TOTAL: wood paint {0:0.00} m2 | Locksets {1} pcs | Hinges {2} pcs", openings.Sum((OpeningItem o) => o.LeafPaint + o.JambPaint), openings.Sum((OpeningItem o) => o.Locks), openings.Sum((OpeningItem o) => o.Hinges)));
				if (detailed)
				{
					foreach (OpeningItem item2 in openings.Where((OpeningItem o) => o.Material != "Manual"))
					{
						stringBuilder.AppendLine();
						stringBuilder.AppendLine(item2.Id + " | " + item2.Description);
						stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Opening {0:0.00} x {1:0.00} m | {2} units | {3}, {4} leaf/leaves", item2.Width, item2.Height, item2.Quantity, item2.Operation, item2.Leaves));
						stringBuilder.AppendLine(string.Format("  Hardware/unit: {0} lockset(s), {1} hinges", item2.EffectiveLocks, item2.EffectiveHinges));
						if (item2.Wood)
						{
							stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Paint: leaf {0:0.00} + jamb {1:0.00} = {2:0.00} m2", item2.LeafPaint, item2.JambPaint, item2.LeafPaint + item2.JambPaint));
						}
					}
				}
			}
			if (flag || scope == "Finishing Works")
			{
				if (!flag)
				{
					AppendMouldingReport(stringBuilder);
				}
				stringBuilder.AppendLine();
				stringBuilder.AppendLine("FINISHING WORKS | TOTALS BY GROUP");
				foreach (FinishTypeTotal item3 in FinishTypeRegistry.Totals(finishItems, (FinishItem f) => f.Total(FindWall(f.Wall), wallOpenings, finishItems)))
				{
					stringBuilder.AppendLine(item3.Name);
					stringBuilder.AppendLine("  TOTAL AREA: " + item3.Area.ToString("0.00") + " m2 | " + item3.Locations);
				}
				if (detailed)
				{
					stringBuilder.AppendLine();
					stringBuilder.AppendLine("FINISH SCHEDULE");
					foreach (KeyValuePair<string, string> finishGroup in finishGroups)
					{
						stringBuilder.AppendLine("  " + finishGroup.Key + " -> " + ((finishGroup.Value == "") ? "(group not assigned)" : finishGroup.Value));
					}
					stringBuilder.AppendLine();
					stringBuilder.AppendLine("AREA BACKUP BY DESCRIPTION");
					foreach (FinishItem finishItem in finishItems)
					{
						WallItem wall = FindWall(finishItem.Wall);
						stringBuilder.AppendLine(finishItem.Id + " | " + finishItem.Description + " | " + finishItem.Category + " | Finish ID " + finishItem.Code);
						if (finishItem.Basis == "Whole wall (auto)")
						{
							stringBuilder.AppendLine("  Linked wall " + finishItem.Wall + " | Side " + finishItem.Side);
						}
						else
						{
							stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "  {0:0.00} x {1:0.00} m x {2} units", finishItem.Length, finishItem.Width, finishItem.Quantity));
						}
						if (finishItem.AutoSurface)
						{
							stringBuilder.AppendLine("  Net wall less tiles = " + finishItem.Total(wall, wallOpenings, finishItems).ToString("0.00") + " m2");
						}
						else
						{
							stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "  ({0:0.00} - {1:0.00}) x {2} face(s) = {3:0.00} m2", finishItem.GrossPerFace(wall, wallOpenings), finishItem.DeductPerFace(wall, wallOpenings), finishItem.Faces, finishItem.Total(wall, wallOpenings, finishItems)));
						}
						stringBuilder.AppendLine();
					}
				}
			}
			if (flag || scope == "Structural Steel Works")
			{
				AppendStructuralSteelReport(stringBuilder, detailed);
			}
			if (flag || scope == "Roofing Works")
			{
				AppendRoofingReport(stringBuilder, detailed);
			}
			stringBuilder.AppendLine();
			if(new[]{"Earthworks","Pavement Construction","Slope Protection Works","Carpentry and Joinery Works"}.Contains(scope))AppendExtraWorksReport(stringBuilder,detailed,scope);
            stringBuilder.AppendLine("COMMON BASIS");
			stringBuilder.AppendLine("Quantities use full precision; detail traces may show extra decimals. Purchasing follows the saved stock, lap and allowance settings.");
			if (flag || scope == "Roofing Works")
			{
				stringBuilder.AppendLine("Roof slope includes overhang. Order quantities use full supplier sheet/piece lengths, effective cover and entered laps. Insulation purchase area is rounded by type to the configured roll coverage; final quantities are in m2. Gutter end closures use entered blank dimensions.");
			}
			if (flag || scope == "Reinforced Concrete Works")
			{
				stringBuilder.AppendLine("Concrete is gross m3; forms in m2. Rebar kg are whole-stock purchase weights, with laps/offcuts.");
			}
			if (flag || scope == "Masonry Works")
			{
				stringBuilder.AppendLine("CHB deducts assigned openings once. Plaster/paint exclude tiles only on selected faces.");
			}
			if (flag || scope == "Finishing Works")
			{
				stringBuilder.AppendLine("Areas in m2 are grouped through Finish IDs. Three-coat items retain surface area (not x3).");
			}
			if (flag || scope == "Doors & Windows")
			{
				stringBuilder.AppendLine("Wood doors: 45mm. Hinges/leaf = max(2, ceil(height/0.762m)), +1 for width >0.94m.");
				stringBuilder.AppendLine("Hager quantity guide; hinge size/rating follows hardware specifications. One lockset per door.");
			}
			if (flag || scope == "Plumbing Works")
			{
				AppendPlumbingReport(stringBuilder);
			}
			if (flag)
			{
				AppendBackupBillReport(stringBuilder);
			}
			return stringBuilder.ToString();
		}

		private void AppendConcreteWorks(StringBuilder b, bool detailed)
		{
			beam.AppendEarthworksReport(b, detailed);
			b.AppendLine();
			b.AppendLine("REINFORCED CONCRETE WORKS");
			List<object[]> list = new List<object[]>();
			object[][,] array = new object[2][,]
			{
				beam.WorksExcelRows(projectRebarPlan,false),
				slab.WorksExcelRows(projectRebarPlan,false)
			};
			foreach (object[,] array2 in array)
			{
				for (int j = 0; j < array2.GetLength(0); j++)
				{
					object[] array3 = new object[12];
					for (int k = 0; k < 12; k++)
					{
						array3[k] = array2[j, k];
					}
					list.Add(array3);
				}
			}
			b.AppendLine("Shared project stock: beam, slab, footing, canopy and RC wall cuts use compatible grade/diameter offcuts and checked commercial lengths.");
			PurchaseReports.Summary(b, projectRebarPlan);
			b.AppendLine(OffcutReuseText());
			b.AppendLine();
			b.AppendLine(string.Format("{0,-8}{1,-28}{2,7}{3,16}{4,16}", "WORK", "DESCRIPTION / MARK", "QTY", "CONCRETE m3", "FORMS m2"));
			KeyValuePair<string, object[,]>[] array4 = new KeyValuePair<string, object[,]>[2]
			{
				new KeyValuePair<string, object[,]>("Beam", beam.ConcreteExcelRows()),
				new KeyValuePair<string, object[,]>("Slab", slab.WorksMeasureRows())
			};
			for (int i = 0; i < array4.Length; i++)
			{
				KeyValuePair<string, object[,]> keyValuePair = array4[i];
				double num = 0.0;
				double num2 = 0.0;
				for (int l = 0; l < keyValuePair.Value.GetLength(0); l++)
				{
					object[,] value = keyValuePair.Value;
					num += Convert.ToDouble(value[l, 4]);
					num2 += Convert.ToDouble(value[l, 7]);
					CultureInfo invariantCulture = CultureInfo.InvariantCulture;
					object[] obj = new object[5] { keyValuePair.Key, null, null, null, null };
					string text = Convert.ToString(value[l, 0]);
					object obj2 = value[l, 1];
					obj[1] = Clip(text + " / " + ((obj2 != null) ? obj2.ToString() : null), 27);
					obj[2] = value[l, 3];
					obj[3] = value[l, 4];
					obj[4] = value[l, 7];
					b.AppendLine(string.Format(invariantCulture, "{0,-8}{1,-28}{2,7}{3,16:0.00}{4,16:0.00}", obj));
				}
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0} TOTAL: concrete {1:0.00} m3 | forms {2:0.00} m2", keyValuePair.Key, num, num2));
			}
			if (!detailed)
			{
				return;
			}
			b.AppendLine();
			b.AppendLine("REBAR PURCHASE BACKUP");
			b.AppendLine(string.Format("{0,-30}{1,7}{2,7}{3,14}{4,14}{5,14}", "DESCRIPTION / MARK", "GRADE", "DIA", "CUT kg", "LAP/OFFCUT", "BUY kg"));
			foreach (object[] item in list)
			{
				CultureInfo invariantCulture2 = CultureInfo.InvariantCulture;
				object[] array5 = new object[6];
				string text2 = Convert.ToString(item[0]);
				object obj3 = item[1];
				array5[0] = Clip(text2 + " / " + ((obj3 != null) ? obj3.ToString() : null), 29);
				array5[1] = item[3];
				array5[2] = item[4];
				array5[3] = Convert.ToDouble(item[5]) + Convert.ToDouble(item[6]) + Convert.ToDouble(item[7]);
				array5[4] = item[8];
				array5[5] = item[9];
				b.AppendLine(string.Format(invariantCulture2, "{0,-30}{1,7}{2,7}{3,14:0.00}{4,14:0.00}{5,14:0.00}", array5));
			}
		}

		private static string Clip(string text, int length)
		{
			if (text.Length > length)
			{
				return text.Substring(0, length - 1) + "ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â¦";
			}
			return text;
		}

		private Dictionary<string, DataGridView> Tables()
		{
			return WithMasonry(new Dictionary<string, DataGridView>
			{
				{ "Wood", wood },
				{ "Aluminum", aluminum },
				{ "Walls", walls },
				{ "CHBOpenings", manualOpeningsGrid },
				{ "FinishSchedule", finishSchedule },
				{ "WallFinishes", wallFinishes },
				{ "CeilingFinishes", ceilingFinishes },
				{ "FloorFinishes", floorFinishes },
				{ "OtherFinishes", otherFinishes },
				{ "InsulationRolls", insulationRolls },
				{ "RoofProfiles", roofProfiles },
				{ "RoofSheets", roofSheets },
				{ "RoofAccessories", roofAccessories },
				{ "CutFill", cutFill },
				{ "Footings", footings },
				{ "Canopies", canopies },
				{ "GravelEntries", gravelEntries },
				{ "MouldingEntries", mouldingEntries },
				{ "PlumbingPipes", plumbingPipes },
				{ "PlumbingLS", plumbingLs },
				{ "PlumbingChambers", plumbingChambers },
				{ "PlumbingRebar", plumbingRebar },
				{ "PlumbingMembers", plumbingMembers },
				{ "RCWalls", rcWalls }
			});
		}

		private static int[] InputColumns(string section)
		{
			switch (section)
			{
			case "PlumbingMembers":
				return Enumerable.Range(0, 24).ToArray();
			case "RCWalls":
				return Enumerable.Range(0, 22).Concat(new[] { 27, 28, 29, 30 }).ToArray();
			case "PlumbingPipes":
				return Enumerable.Range(0, 5).ToArray();
			case "PlumbingLS":
				return new int[7] { 0, 1, 2, 3, 5, 6, 7 };
			case "PlumbingChambers":
				return Enumerable.Range(0, 25).ToArray();
			case "PlumbingRebar":
				return Enumerable.Range(0, 7).ToArray();
			case "MouldingEntries":
				return new int[6] { 0, 1, 2, 3, 4, 7 };
			case "Wood":
			case "Aluminum":
				return Enumerable.Range(0, 13).Concat(new int[5] { 20, 21, 22, 24, 25 }).ToArray();
			default:
				if (SteelSections.Contains(section))
				{
					return Enumerable.Range(0, SteelInputCount(section)).ToArray();
				}
				switch (section)
				{
				case "InsulationRolls":
					return Enumerable.Range(0, 3).ToArray();
				case "RoofProfiles":
					return Enumerable.Range(0, 5).ToArray();
				case "CutFill":
					return Enumerable.Range(0, 11).ToArray();
				case "GravelEntries":
					return Enumerable.Range(0, 6).ToArray();
				case "Canopies":
					return Enumerable.Range(0, 19).ToArray();
				case "Footings":
					return Enumerable.Range(0, 28).ToArray();
				case "RoofSheets":
					return Enumerable.Range(0, 12).Concat(new int[2] { 17, 18 }).ToArray();
				case "RoofAccessories":
					return Enumerable.Range(0, 8).ToArray();
				case "FinishSchedule":
					return new int[3] { 0, 1, 2 };
				default:
					if (IsMasonrySection(section))
					{
						return new int[11]
						{
							0, 1, 2, 3, 4, 5, 6, 12, 16, 18,
							19
						};
					}
					switch (section)
					{
					case "WallFinishes":
						return Enumerable.Range(0, 11).Concat(new int[2] { 14, 15 }).ToArray();
					case "CeilingFinishes":
					case "FloorFinishes":
					case "OtherFinishes":
						return Enumerable.Range(0, 7).Concat(new int[2] { 10, 11 }).ToArray();
					default:
						return Enumerable.Range(0, 13).Concat(new int[4] { 20, 21, 22, 24 }).ToArray();
					}
				}
			}
		}

		private string DataXml()
		{
			return WithExcludedValues(DataXmlCore);
		}

		private string DataXmlCore()
		{
			XmlDocument xmlDocument = new XmlDocument();
			XmlElement xmlElement = xmlDocument.CreateElement("ProgramOfWorks");
			xmlElement.SetAttribute("version", "21");
			xmlElement.SetAttribute("computedInputsKnown", projectQuantitiesKnown.ToString());
			WriteEarthSettings(xmlElement);
			WriteSteelProfiles(xmlElement);
			xmlDocument.AppendChild(xmlElement);
			foreach (KeyValuePair<string, DataGridView> item in Tables())
			{
				XmlElement xmlElement2 = xmlDocument.CreateElement(item.Key);
				xmlElement.AppendChild(xmlElement2);
				foreach (DataGridViewRow item2 in (IEnumerable)item.Value.Rows)
				{
					if (!item2.IsNewRow && !Empty(item2))
					{
						XmlElement xmlElement3 = xmlDocument.CreateElement("Entry");
						int[] array = InputColumns(item.Key);
						for (int i = 0; i < array.Length; i++)
						{
							int n = array[i];
							xmlElement3.SetAttribute("c" + n, Cell(item2, n));string formula = GridFormulas.Formula(item2.Cells[n]);if (formula != null) xmlElement3.SetAttribute("f" + n, formula);
						}
						WriteSteelSets(item.Key, item2, xmlElement3);
						WriteSteelPaintOnly(item2, xmlElement3);
						WriteTrussMark(item2, xmlElement3);
						WriteOpeningPaint(item.Key, item2, xmlElement3);
						WriteDoorHardware(item2, xmlElement3);
						WriteRowExcluded(item2, xmlElement3);
						xmlElement2.AppendChild(xmlElement3);
					}
				}
			}
			finishTypeCells.Write(xmlElement);
			OpeningLinks.Write(xmlElement, openingAssignments, manualOpeningTotals);
			BackupBill.Write(xmlElement, backupBill);
			WriteBackupBillRoutes(xmlElement);
			WriteSchedule(xmlElement);
			WriteStructureConfigurations(xmlElement);
            WriteExtraProject(xmlElement);WriteRoofingSpecification(xmlElement);
            WriteSourceManifest(xmlElement);
			return xmlDocument.OuterXml;
		}

		private async void Save(){await SaveWithFeedback(false);}

		internal string WorksProjectXml(){return BuildWorksProjectXml(true);}
        private string recalculatedInputs;
        private string BuildWorksProjectXml(bool includeSourceFiles)
		{
			Commit();
			/* Saving re-ran the whole recalculation (about 1 s on a mid-size project) although every edit already
			   recalculates. Only recalculate when inputs changed since the last recalculation (owner: slow save). */
			if (childWorksDirty || recalculatedInputs == null || DataXml() != recalculatedInputs) { Recalculate(); PowTiming.Mark("recalculation"); }
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(DataXml());
            PowProjectInformation.Write(xmlDocument.DocumentElement,ProjectInfoStore.Read());
            PowIssueOutput.Write(xmlDocument.DocumentElement,WorksValidationIssues());
            if(includeSourceFiles){ApplyDeferredSources();ProjectSources().Write(xmlDocument.DocumentElement);}
			PowTiming.Mark("project data / issues");
			snapshottingWorks = true;
			string text;
			string text2;
			try
			{
				text = beam.WorksSnapshot();
				text2 = slab.WorksSnapshot();
			}
			finally
			{
				snapshottingWorks = false;
			}
			PowTiming.Mark("beam / slab snapshots");
			string[][] array = new string[2][]
			{
				new string[2] { "Beam", text },
				new string[2] { "Slab", text2 }
			};
			foreach (string[] array2 in array)
			{
				if (array2[1] != null)
				{
					XmlElement xmlElement = xmlDocument.CreateElement(array2[0]);
					xmlElement.InnerText = array2[1];
					xmlDocument.DocumentElement.AppendChild(xmlElement);
				}
			}
			return xmlDocument.OuterXml;
		}

		internal void SaveTo(string path)
		{
			Guard();
			string contents = BuildWorksProjectXml(false);
            ApplyDeferredSources();PowProjectFileWriter.Save(path,contents,ProjectSources(),true);
			beam.WorksSaved();
			slab.WorksSaved();
            currentPowPath=Path.GetFullPath(path);
			saved = DataXml();
		}

        private async void Open()
        {
            if(openingPow)return;
            try{Guard();Commit();using(var dialog=new OpenFileDialog{Filter="Bill of Quantities (*.boq.xml, older *.pow.xml)|*.boq.xml;*.pow.xml|All files|*.*"}){
                if(dialog.ShowDialog(this)!=DialogResult.OK)return;
                openingPow=true;
                using(var busy=new PowBusyIndicator(this,"Open Bill of Quantities","Opening "+Path.GetFileName(dialog.FileName)+"…")){
                while((linkingExcel||capturingExcelSource)&&!IsDisposed)await System.Threading.Tasks.Task.Delay(50);
                if(IsDisposed)return;
                openingPow=true;worksExcelTimer.Stop();excelSaveTimer.Stop();
                if(currentSync!=null)try{await currentSync;}catch(Exception ex){ExcelFailure(ex);}
                busy.Status("Reading project and source files…");PowTiming.Start("Open");
                CancelDeferredSources();var snapshot=await System.Threading.Tasks.Task.Run(()=>PowProjectFileWriter.Read(dialog.FileName,true));
                if(IsDisposed)return;PowTiming.Mark("read file");
                busy.Status("Loading worksheets and quantities…");
                try{OpenProjectDocument(snapshot.Document,snapshot.Sources,snapshot.SourcesDeferred?dialog.FileName:null);}catch{CancelDeferredSources();/* a partly loaded window must not overwrite any project file */currentPowPath=null;knownPowWriteUtc=null;throw;}currentPowPath=Path.GetFullPath(dialog.FileName);RememberPowFile(currentPowPath);WarnIfEmptyProject(snapshot.Document,dialog.FileName);if(!snapshot.SourcesDeferred)VerifySourcesInBackground(snapshot.Sources);
                if(worksExcel!=null&&linkedHasBackup){busy.Status("Refreshing linked BACKUP items…");await LoadCatalogue();PowTiming.Mark("Excel BACKUP items");lastExcelXml="";dashboardDirty=true;RefreshVisibleWorksViews();saved=DataXml();ScheduleWorksExcel();}
                ShowTiming(PowTiming.Finish());}
            }}catch(Exception ex){if(!IsDisposed)MessageBox.Show(this,ex.Message,"Open Bill of Quantities",MessageBoxButtons.OK,MessageBoxIcon.Information);}finally{openingPow=false;if(!IsDisposed&&worksExcel!=null&&!excelBroken)ScheduleWorksExcel();}
        }

		internal void OpenFrom(string path)
		{
			Guard();
            var snapshot=PowProjectFileWriter.Read(path);
            try{OpenProjectDocument(snapshot.Document,snapshot.Sources);}catch{currentPowPath=null;throw;}
            currentPowPath=Path.GetFullPath(path);RememberPowFile(currentPowPath);
		}

		internal void OpenProjectXml(string xml)
		{
			Guard();
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
					MaxCharactersInDocument = 0
				}))
				{
					xmlDocument.Load(reader);
				}
			}
            OpenProjectDocument(xmlDocument,PowSourceFiles.Read(xmlDocument.DocumentElement));
        }
        private void OpenProjectDocument(XmlDocument xmlDocument,PowSourceFiles savedSourceFiles,string deferredSourcesPath=null)
        {
            using(GridBatch.Suspend(this))OpenProjectDocumentRows(xmlDocument,savedSourceFiles,deferredSourcesPath);
        }
        private void OpenProjectDocumentRows(XmlDocument xmlDocument,PowSourceFiles savedSourceFiles,string deferredSourcesPath)
        {
            Guard();
            var savedProjectInformation=PowProjectInformation.Read(xmlDocument.DocumentElement);
            bool roofSchema=xmlDocument.DocumentElement.Name=="ProgramOfWorks"&&xmlDocument.DocumentElement.GetAttribute("version")=="21";
            ValidateRoofingSpecification(xmlDocument.DocumentElement,roofSchema);
            bool extraSchema=xmlDocument.DocumentElement.Name=="ProgramOfWorks"&&(roofSchema||xmlDocument.DocumentElement.GetAttribute("version")=="20");
            ValidateExtraProject(xmlDocument.DocumentElement,extraSchema);
            if(extraSchema)xmlDocument.DocumentElement.SetAttribute("version","19");
			if (xmlDocument.DocumentElement.Name != "ProgramOfWorks" || !new string[19]
			{
				"1", "2", "3", "4", "5", "6", "7", "8", "9", "10",
				"11", "12", "13", "14", "15", "16", "17", "18", "19"
			}.Contains(xmlDocument.DocumentElement.GetAttribute("version")))
			{
				if(xmlDocument.DocumentElement!=null&&xmlDocument.DocumentElement.Name=="PowWorkbook")throw new Exception("This is a Program of Works (POW) file. Open it in the Program of Works (POW) workspace.");
				throw new Exception("Choose a Bill of Quantities file.");
			}
			NormalizeLegacyAAC(xmlDocument.DocumentElement);
			if (PowOpenState.HasInputChanges(DataXml(),saved))
			{
				throw new Exception("This window has unsaved Bill of Quantities inputs. Save them, or use Close Bill of Quantities, then open the other file.");
			}
			if (deferredSourcesPath != null) BeginDeferredSources(deferredSourcesPath, xmlDocument.DocumentElement);
			if (beam.WorksSnapshot() != null || slab.WorksSnapshot() != null)
			{
				throw new Exception("Beam or slab work is already loaded in this window. Use Close Bill of Quantities (save first if needed), then open the saved file.");
			}
			PowTiming.Mark("check file");
			List<BackupBillItem> list = BackupBill.Read(xmlDocument.DocumentElement);
			FinishTypeRegistry finishTypeRegistry = FinishTypeRegistry.Read(xmlDocument.DocumentElement);
			List<OpeningLink> collection = OpeningLinks.Read(xmlDocument.DocumentElement);
			foreach (KeyValuePair<string, DataGridView> item in Tables())
			{
				XmlElement xmlElement = xmlDocument.DocumentElement[item.Key];
				string attribute = xmlDocument.DocumentElement.GetAttribute("version");
				bool flag = attribute == "1" || attribute == "2";
				if (xmlElement == null && item.Key == "OtherFinishes")
				{
					continue; /* Other Finishes was added in v5.1.8: files saved before it open with an empty tab */
				}
				if (xmlElement == null)
				{
					if ((!(attribute != "19") || !(item.Key == "PlumbingMembers")) && (!(attribute != "17") || !(attribute != "18") || !(attribute != "19") || !(item.Key == "RCWalls")) && (!(attribute != "16") || !(attribute != "17") || !(attribute != "18") || !(attribute != "19") || !item.Key.StartsWith("Plumbing")) && (!(attribute != "15") || !(attribute != "16") || !(attribute != "17") || !(attribute != "18") || !(attribute != "19") || !(item.Key == "MouldingEntries")) && (!(attribute != "14") || !(attribute != "15") || !(attribute != "16") || !(attribute != "17") || !(attribute != "18") || !(attribute != "19") || (!(item.Key == "Canopies") && !(item.Key == "GravelEntries"))) && (!(attribute != "13") || !(attribute != "14") || !(attribute != "15") || !(attribute != "16") || !(attribute != "17") || !(attribute != "18") || !(attribute != "19") || !SteelSections.Contains(item.Key)) && (!(attribute != "12") || !(attribute != "13") || !(attribute != "14") || !(attribute != "15") || !(attribute != "16") || !(attribute != "17") || !(attribute != "18") || !(attribute != "19") || (!(item.Key == "RoofProfiles") && !(item.Key == "InsulationRolls"))) && (!(attribute != "12") || !(attribute != "13") || !(attribute != "14") || !(attribute != "15") || !(attribute != "16") || !(attribute != "17") || !(attribute != "18") || !(attribute != "19") || !MasonrySections.Contains(item.Key)) && (!(attribute != "11") || !(attribute != "12") || !(attribute != "13") || !(attribute != "14") || !(attribute != "15") || !(attribute != "16") || !(attribute != "17") || !(attribute != "18") || !(attribute != "19") || (!(item.Key == "CutFill") && !(item.Key == "Footings"))) && (!(attribute != "9") || !(attribute != "10") || !(attribute != "11") || !(attribute != "12") || !(attribute != "13") || !(attribute != "14") || !(attribute != "15") || !(attribute != "16") || !(attribute != "17") || !(attribute != "18") || !(attribute != "19") || !item.Key.StartsWith("Roof")) && (!flag || (!item.Key.EndsWith("Finishes") && !(item.Key == "CHBOpenings"))) && (!(attribute != "6") || !(attribute != "7") || !(attribute != "8") || !(attribute != "9") || !(attribute != "10") || !(attribute != "11") || !(attribute != "12") || !(attribute != "13") || !(attribute != "14") || !(attribute != "15") || !(attribute != "16") || !(attribute != "17") || !(attribute != "18") || !(attribute != "19") || !(item.Key == "FinishSchedule")))
					{
						throw new Exception("Missing " + item.Key + " data.");
					}
					continue;
				}
				foreach (XmlElement item2 in xmlElement.SelectNodes("Entry"))
				{
					int[] array = InputColumns(item.Key);
					for (int i = 0; i < array.Length; i++)
					{
						int num = array[i];
						/* Material (column 25) was added in v1.4.95; older files open with the grid default (Wood / Aluminum). */
						if ((item.Key == "Wood" || item.Key == "Aluminum") && num == OpeningMaterialColumn && !item2.HasAttribute("c" + num)) continue;
						if (flag && IsMasonrySection(item.Key) && (num == 12 || num == 16))
						{
							continue;
						}
						switch (attribute)
						{
						case "1":
						case "2":
						case "3":
							if (item.Key.EndsWith("Finishes") && (num == 14 || num == 10))
							{
								continue;
							}
							break;
						}
						if (attribute != "6" && attribute != "7" && attribute != "8" && attribute != "9" && attribute != "10" && attribute != "11" && attribute != "12" && attribute != "13" && attribute != "14" && attribute != "15" && attribute != "16" && attribute != "17" && attribute != "18" && attribute != "19" && IsMasonrySection(item.Key) && num == 18)
						{
							continue;
						}
						switch (attribute)
						{
						case "1":
						case "2":
						case "3":
						case "4":
							if ((item.Key == "Wood" || item.Key == "Aluminum" || item.Key == "CHBOpenings") && (num == 20 || num == 21))
							{
								continue;
							}
							break;
						}
						if ((!(attribute != "7") || !(attribute != "8") || !(attribute != "9") || !(attribute != "10") || !(attribute != "11") || !(attribute != "12") || !(attribute != "13") || !(attribute != "14") || !(attribute != "15") || !(attribute != "16") || !(attribute != "17") || !(attribute != "18") || !(attribute != "19") || !IsMasonrySection(item.Key) || num != 19) && (!(attribute != "10") || !(attribute != "11") || !(attribute != "12") || !(attribute != "13") || !(attribute != "14") || !(attribute != "15") || !(attribute != "16") || !(attribute != "17") || !(attribute != "18") || !(attribute != "19") || !(item.Key == "RoofSheets") || (num != 17 && num != 18)) && (!(attribute != "14") || !(attribute != "15") || !(attribute != "16") || !(attribute != "17") || !(attribute != "18") || !(attribute != "19") || (!(item.Key == "Wood") && !(item.Key == "Aluminum") && !(item.Key == "CHBOpenings")) || num != 22) && (!(attribute != "15") || !(attribute != "16") || !(attribute != "17") || !(attribute != "18") || !(attribute != "19") || (!(item.Key == "Wood") && !(item.Key == "Aluminum") && !(item.Key == "CHBOpenings")) || num != 24) && (!(attribute != "19") || !(item.Key == "PlumbingLS") || num < 5) && !item2.HasAttribute("c" + num))
						{
							/* Owner (17 November 2026): a column added in a later version (e.g. Material, moulding Opening ID) is
							   simply blank in older files; the fill step uses the column default. Older POWs must keep opening. */
							continue;
						}
					}
				}
			}
			PowTiming.Mark("validate sheets");
			if (xmlDocument.DocumentElement["Beam"] != null)
			{
				beam.WorksRestore(xmlDocument.DocumentElement["Beam"].InnerText);
			}
			if (xmlDocument.DocumentElement["Slab"] != null)
			{
				slab.WorksRestore(xmlDocument.DocumentElement["Slab"].InnerText);
			}
			PowTiming.Mark("beam / slab estimates");
			filling = true;
			try
			{
				ReadEarthSettings(xmlDocument.DocumentElement);
				ReadSteelProfiles(xmlDocument.DocumentElement);
				projectQuantitiesKnown = xmlDocument.DocumentElement.HasAttribute("computedInputsKnown") && bool.Parse(xmlDocument.DocumentElement.GetAttribute("computedInputsKnown"));
				backupBill = list;
				ReadBackupBillRoutes(xmlDocument.DocumentElement);
				ReadSchedule(xmlDocument.DocumentElement);
				ReadStructureConfigurations(xmlDocument.DocumentElement);
				finishTypeCells = finishTypeRegistry;
				openingAssignments.Clear();ClearParkedWallOpenings();
				openingAssignments.AddRange(collection);
				legacyOpeningTotals = OpeningLinks.Legacy(xmlDocument.DocumentElement); manualOpeningTotals = OpeningLinks.ManualTotals(xmlDocument.DocumentElement);
				foreach (KeyValuePair<string, DataGridView> item3 in Tables())
				{
					XmlElement xmlElement3 = xmlDocument.DocumentElement[item3.Key];
					if (xmlElement3 == null && (item3.Key == "RoofProfiles" || item3.Key == "InsulationRolls"))
					{
						continue;
					}
					item3.Value.Rows.Clear();
					if (xmlElement3 == null)
					{
						continue;
					}
					foreach (XmlElement item4 in xmlElement3.SelectNodes("Entry"))
					{
						int index = item3.Value.Rows.Add();
						int[] array = InputColumns(item3.Key);
						for (int i = 0; i < array.Length; i++)
						{
							int num2 = array[i];
							object obj;
							if (!item4.HasAttribute("c" + num2))
							{
								if (!(item3.Key == "PlumbingLS") || num2 != 5)
								{
									if (!(item3.Key == "PlumbingLS") || num2 < 6)
									{
										switch (num2)
										{
										default:
											obj = "";
											break;
										case 18:
											obj = "150";
											break;
										case 22:
											obj = "False";
											break;
										case 21:
											obj = "Swing";
											break;
										case 20:
											obj = "1";
											break;
										case 12:
											obj = "2 sides";
											break;
										case 16:
											obj = "Same as plaster";
											break;
										}
									}
									else
									{
										obj = "";
									}
								}
								else
								{
									obj = "ls";
								}
							}
							else
							{
								obj = item4.GetAttribute("c" + num2);
							}
							string text = (string)obj;
							item3.Value.Rows[index].Cells[num2].Value = ((item3.Value.Columns[num2] is DataGridViewCheckBoxColumn) ? ((object)(text != "" && bool.Parse(text))) : text);GridFormulas.Set(item3.Value.Rows[index].Cells[num2], item4.HasAttribute("f" + num2) ? item4.GetAttribute("f" + num2) : null);
						}
						ReadSteelSets(item3.Key, item3.Value.Rows[index], item4);
						ReadSteelPaintOnly(item3.Value.Rows[index], item4);
						ReadTrussMark(item3.Value.Rows[index], item4);
						ReadOpeningPaint(item3.Key, item3.Value.Rows[index], item4);
						ReadDoorHardware(item3.Value.Rows[index], item4);
						if ((item3.Key == "Wood" || item3.Key == "Aluminum") && !item4.HasAttribute("c" + OpeningMaterialColumn)) item3.Value.Rows[index].Cells[OpeningMaterialColumn].Value = (item3.Key == "Wood") ? "Wood" : "Aluminum";
						ReadRowExcluded(item3.Value.Rows[index], item4);
					}
				}
			}
			finally
			{
				filling = false;
			}
			if (xmlDocument.DocumentElement["FinishSchedule"] == null)
			{
				ImportLegacyFinishSchedule();
			}
			if (xmlDocument.DocumentElement.GetAttribute("version") != "8" && xmlDocument.DocumentElement.GetAttribute("version") != "9" && xmlDocument.DocumentElement.GetAttribute("version") != "10" && xmlDocument.DocumentElement.GetAttribute("version") != "11" && xmlDocument.DocumentElement.GetAttribute("version") != "12" && xmlDocument.DocumentElement.GetAttribute("version") != "13" && xmlDocument.DocumentElement.GetAttribute("version") != "14" && xmlDocument.DocumentElement.GetAttribute("version") != "15" && xmlDocument.DocumentElement.GetAttribute("version") != "16" && xmlDocument.DocumentElement.GetAttribute("version") != "17" && xmlDocument.DocumentElement.GetAttribute("version") != "18" && xmlDocument.DocumentElement.GetAttribute("version") != "19")
			{
				foreach (DataGridViewRow item5 in (IEnumerable)walls.Rows)
				{
					if (!item5.IsNewRow && Cell(item5, 19) != "")
					{
						item5.Cells[2].Value = "";
						item5.Cells[3].Value = "";
						item5.Cells[4].Value = "1";
					}
				}
			}
			if (xmlDocument.DocumentElement.GetAttribute("version") != "6" && xmlDocument.DocumentElement.GetAttribute("version") != "7" && xmlDocument.DocumentElement.GetAttribute("version") != "8" && xmlDocument.DocumentElement.GetAttribute("version") != "9" && xmlDocument.DocumentElement.GetAttribute("version") != "10" && xmlDocument.DocumentElement.GetAttribute("version") != "11" && xmlDocument.DocumentElement.GetAttribute("version") != "12" && xmlDocument.DocumentElement.GetAttribute("version") != "13" && xmlDocument.DocumentElement.GetAttribute("version") != "14" && xmlDocument.DocumentElement.GetAttribute("version") != "15" && xmlDocument.DocumentElement.GetAttribute("version") != "16" && xmlDocument.DocumentElement.GetAttribute("version") != "17" && xmlDocument.DocumentElement.GetAttribute("version") != "18" && xmlDocument.DocumentElement.GetAttribute("version") != "19")
			{
				foreach (DataGridViewRow item6 in (IEnumerable)wood.Rows)
				{
					double result;
					if (!item6.IsNewRow && double.TryParse(Cell(item6, 9), NumberStyles.Float, CultureInfo.InvariantCulture, out result) && result <= 1.0)
					{
						item6.Cells[9].Value = (result * 1000.0).ToString("0.00", CultureInfo.InvariantCulture);
					}
				}
			}
            PowTiming.Mark("fill sheets");ReadExtraProject(xmlDocument.DocumentElement);ReadRoofingSpecification(xmlDocument.DocumentElement);PowTiming.Mark("fill additional sheets");
            if(worksExcel!=null){string workbook=Path.GetFileName(worksExcel.PathName);savedSourceFiles.AddRange(ProjectSources().Files.Where(f=>f.Kind=="Excel workbook (imported original)"&&f.Name.Equals(workbook,StringComparison.OrdinalIgnoreCase)));}
            projectSourceFiles.Replace(savedSourceFiles);
            if(savedProjectInformation==null&&!Automated)ProjectInfoStore.ClearProject();
            if(savedProjectInformation!=null)ProjectInfoStore.Save(savedProjectInformation.Name,savedProjectInformation.Location,savedProjectInformation.Office,savedProjectInformation.OfficeAddress,savedProjectInformation.DatePrepared??"",PowProjectInformation.FiscalYearFor(savedProjectInformation),PowProjectInformation.AbcFor(savedProjectInformation));
			StyleTrussRows(); /* rows made from a truss schedule stay locked */
			PowTiming.Mark("project info / sources");
			Recalculate();
			PowTiming.Mark("recalculation");
			ShowPipeGroup();
			saved = DataXml();
			beam.WorksSaved();
			slab.WorksSaved();
			PowTiming.Mark("finish open");
		}


		private void OnWorksClosing(object sender, FormClosingEventArgs e)
		{
            if(savingPow||openingPow){e.Cancel=true;return;}
			PowTiming.Start("Close", true);
			Commit();
			worksExcelTimer.Stop();
			if(worksExcel!=null&&!excelCloseFlushed){e.Cancel=true;if(!closingExcel){closingExcel=true;FinishExcelClose();}return;}
			bool closeDirty = DataXml() != saved || beam.WorksDirty || slab.WorksDirty;
			PowTiming.Mark("check unsaved changes");
			if (closeDirty)
			{
				switch (MessageBox.Show(this, "Save Bill of Quantities entries before closing?", "Bill of Quantities", MessageBoxButtons.YesNoCancel))
				{
				case DialogResult.Cancel:
					e.Cancel = true;
					return;
				case DialogResult.Yes:
                    e.Cancel=true;
                    SaveThenClose();
                    return;
				}
			}
			Form[] array = new Form[2] { beam, slab };
			foreach (Form form in array)
			{
				if (!form.IsDisposed)
				{
					form.Close();
					if (!form.IsDisposed)
					{
						e.Cancel = true;
						break;
					}
				}
			}
			PowTiming.Mark("close beam / slab windows");
			if (e.Cancel) PowTiming.Finish();
		}


		private void AddHome()
		{
			TabPage tabPage = new TabPage("Bill of Quantities"){Padding=new Padding(0,0,0,8)};
			var value = new PremiumSectionHeading { Text = "Bill of Quantities", Subtitle = "Project quantities and their measurement sources" };
			var flowLayoutPanel = PremiumExcelToolbar();
			boqStatus = new Label
			{
				Dock = DockStyle.Top,
				Height = 48,
				Padding = new Padding(16, 8, 12, 4),
				ForeColor = PowTheme.Muted
			};
			boqGrid = Grid(new string[4] { "ITEM NO.", "DESCRIPTION", "QUANTITY", "UNIT" }, 0);
			boqGrid.Name = "BillOfQuantities";
			boqGrid.ReadOnly = true;
			boqGrid.AllowUserToAddRows = false;
			boqGrid.AllowUserToDeleteRows = false;
            boqDisclosure=new BoqDisclosure(boqGrid);ConfigureBoqQuantityDisplay();BoqScrollState.Attach(boqGrid);boqGrid.ScrollBars=ScrollBars.Both;
			boqGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
			boqGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
			float[] array = new float[4] { 8f, 78f, 12f, 8f }; /* owner, 18 November 2026: wider DESCRIPTION, narrower ITEM NO. */
			for (int num = 0; num < 4; num++)
			{
				boqGrid.Columns[num].FillWeight = array[num];
				DataGridViewColumn dataGridViewColumn = boqGrid.Columns[num];
				int minimumWidth;
				switch (num)
				{
				default:
					minimumWidth = 80;
					break;
				case 0:
					minimumWidth = 105;
					break;
				case 1:
					minimumWidth = 360;
					break;
				}
				dataGridViewColumn.MinimumWidth = minimumWidth;
			}
			boqGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
			boqGrid.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
			SetupBackupBillEditing();
			boqPages = new NavigationTabs
			{
				Dock = DockStyle.Fill
			};
			boqPages.Deselecting += delegate(object s, TabControlCancelEventArgs e)
			{
				if (!CommitPage(e.TabPage))
				{
					e.Cancel = true;
				}
			};
			boqPages.SelectedIndexChanged += delegate
			{
				WorksTabChanged();
			};
			TabPage tabPage2 = new TabPage("BACKUP quantities");
			tabPage2.Controls.Add(boqGrid);
			boqPages.TabPages.Add(tabPage2);
			/* Owner (18 November 2026): the red summary line above the table is removed (the Status panel lists the same
			   issues), and the search bar sits on the table itself, directly above its header. */
			boqStatus.Visible = false;
			var boqSearchStrip = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(0, 4, 0, 4), WrapContents = false };
			var boqTableArea = new Panel { Dock = DockStyle.Fill };
			boqTableArea.Controls.Add(boqGrid);
			boqTableArea.Controls.Add(boqSearchStrip);
			tabPage.Controls.Add(boqTableArea);
			AddIssuePanel(tabPage);
			AddBoqSearch(boqSearchStrip);
			/* Owner (18 November 2026, "isagad hanggang sa dulo ng table"): the search bar spans the full table width. */
			Action fitSearch = delegate { foreach (Control c in boqSearchStrip.Controls) c.Width = Math.Max(200, boqSearchStrip.ClientSize.Width - c.Margin.Horizontal - boqSearchStrip.Padding.Horizontal); };
			boqSearchStrip.Resize += delegate { fitSearch(); };
			boqSearchStrip.ControlAdded += delegate { fitSearch(); };
			fitSearch();
			tabPage.Controls.Add(boqStatus);
			tabPage.Controls.Add(flowLayoutPanel);
			tabPage.Controls.Add(value);
			tabs.TabPages.Add(tabPage);
		}

		internal List<BillQuantity> BillOfQuantities()
		{
			List<WorksExcelTable> full = WorksExcelTables();
			List<BillQuantity> rows = new List<BillQuantity>();
			Func<string, string, double, string, BillQuantity> add = delegate(string section, string description, double quantity, string unit)
			{
				BillQuantity billQuantity = new BillQuantity
				{
					Section = section,
					Description = description,
					Quantity = quantity,
					Unit = unit
				};
				rows.Add(billQuantity);
				return billQuantity;
			};
			WorksExcelTable main = full.Single((WorksExcelTable t) => t.Name == "DTCE Tool Kit - POW" && t.StartColumn == 1);
			Action<string, string[]> action = delegate(string section, string[] keys)
			{
				foreach (string key in keys)
				{
					string[] array = WorksSummaryLayout.Map.Single((string[] m) => m[0] == key);
					add(section, array[1], Convert.ToDouble(main.Rows[int.Parse(array[2]) - 2, 1]), array[3]).Sources.AddRange(FixedQuantitySources(key));
				}
			};
			action("Reinforced Concrete Works", new string[12]
			{
				"BeamConcrete", "SlabConcrete", "FootingConcrete", "RCWallConcrete", "ConcreteTotal", "BeamForms", "SlabForms", "FootingForms", "RCWallForms", "FormsTotal",
				"Grade40", "Grade60"
			});
			List<object[]> list = CanopySummary();
			for (int num = 0; num < list.Count; num++)
			{
				add("Canopy / Ledge", Convert.ToString(list[num][0]), Convert.ToDouble(list[num][1]), Convert.ToString(list[num][2])).Sources.AddRange(CanopyQuantitySources(num));
			}
			foreach (var item2 in from a in projectRebarPlan.Where(a=>!slopePlan.Contains(a))
				where a.Choice != null
				group a by new
				{
					a.Demand.Grade,
					a.Demand.Diameter
				} into g
				orderby g.Key.Grade, g.Key.Diameter
				select g)
			{
				add("Rebar purchase by size", "Grade " + item2.Key.Grade + " / " + item2.Key.Diameter + " mm", item2.Sum((StockAllocation a) => a.Purchase), "kg").Sources.AddRange(MergeSources(item2.Select((StockAllocation a) => SourceQuantity(RebarSource(a), a.Purchase, "kg", BoqComputations.Rebar(a)))));
			}
			Action<string, string> action2 = delegate(string section, string name)
			{
				WorksExcelTable worksExcelTable = full.Single((WorksExcelTable t) => t.Name == name);
				for (int num3 = 0; num3 < ((name == "POW Overall Earthworks") ? Math.Min(6, worksExcelTable.Rows.GetLength(0)) : worksExcelTable.Rows.GetLength(0)); num3++)
				{
					add(section, Convert.ToString(worksExcelTable.Rows[num3, 0]), Convert.ToDouble(worksExcelTable.Rows[num3, 1]), Convert.ToString(worksExcelTable.Rows[num3, 2])).Sources.AddRange((name == "POW Masonry Summary") ? MasonryQuantitySources(num3) : ((name == "POW Overall Earthworks") ? EarthQuantitySources(num3) : ((name == "POW Structural Steel Summary") ? SteelQuantitySources(Convert.ToString(worksExcelTable.Rows[num3, 0])) : RoofingQuantitySources(Convert.ToString(worksExcelTable.Rows[num3, 0])))));
				}
			};
			action2("Earthworks", "POW Overall Earthworks");
			List<object[]> list2 = GravelSummary();
			for (int num2 = 0; num2 < list2.Count; num2++)
			{
				add("Gravel", Convert.ToString(list2[num2][0]), Convert.ToDouble(list2[num2][1]), Convert.ToString(list2[num2][2])).Sources.AddRange(GravelQuantitySources(num2));
			}
			action2("Masonry Works", "POW Masonry Summary");
			action("Finishing Works", new string[5] { "Plaster", "MasonryPaint", "WallTiles", "CeilingFinishes", "FloorFinishes" });
			foreach (FinishTypeTotal item in from f in FinishTypeRegistry.Totals(finishItems, (FinishItem f) => f.Total(FindWall(f.Wall), wallOpenings, finishItems))
				orderby f.Name
				select f)
			{
				add("Finish material breakdown", item.Name, item.Area, "m2").Sources.AddRange(MergeSources(from f in finishItems
					where FinishTypeRegistry.Key(f.MaterialName) == item.Key
					select SourceQuantity(f.Id + " | " + f.Description, f.Total(FindWall(f.Wall), wallOpenings, finishItems), "m2")));
			}
			/* hinges are bought as Ball Bearing Stainless Door Hinges, 2 pcs per pack, by size; chain / foot bolts and deadbolts (owner, 23 November 2026) */
			action("Doors & Windows", new string[2] { "WoodPaint", "Leversets" });
			rows.AddRange(DoorHardwareQuantities());
			foreach (OpeningItem item3 in from o in openings
				where o.Material != "Manual"
				orderby o.Id
				select o)
			{
				add("Door / window schedule", item3.Id + " | " + item3.Material + " " + item3.Kind + " | " + item3.Description, item3.Quantity, "sets");
			}
			rows.AddRange(MouldingQuantities());
			action2("Roofing Works", "POW Roofing Summary");
			action2("Structural Steel Works", "POW Structural Steel Summary");
			foreach (IGrouping<string, SteelStock> item4 in from s in steelStocks
				group s by s.Specification into g
				orderby g.Key
				select g)
			{
				add("Steel purchase by profile", item4.Key + " | purchase weight", item4.Sum((SteelStock s) => s.KgM * 6.0), "kg").Sources.AddRange(StockPurchaseSources(item4));
			}
			rows.AddRange(PlumbingQuantities());
			rows.AddRange(ManualBoqQuantities());
			rows.AddRange(ExtraBillQuantities());
            return rows;
		}

		private void UpdateDashboard()
		{
			if (boqGrid == null || footingFc == null || earthBalance == null || roofAccessories == null)
			{
				return;
			}
			if (backupBill.Count > 0)
			{
				UpdateBackupBillDashboard();
				return;
			}
			boqGrid.Rows.Clear();
			boqStatusNotes.Clear();
			List<string> list = WorksValidationIssues();
			UpdateIssuePanel(list);
			if (list.Count > 0)
			{
				boqStatus.ForeColor = Color.Firebrick;
				boqStatus.Text = "PARTIAL: " + list.Count + " input issue(s) listed below. Valid quantities shown.";
			}
			try
			{
				foreach (BillDisplayRow item in BillQuantityLayout.Rows(BillOfQuantities().Where(q=>q.Section!="Bill of Quantities").ToList()))
				{
					int index = boqGrid.Rows.Add(item.Item, item.Description, item.Quantity.HasValue ? item.Quantity.Value.ToString((item.Unit == "pcs" || item.Unit == "sets") ? "N0" : "N2", CultureInfo.InvariantCulture) : "", item.Unit);
					boqGrid.Rows[index].ReadOnly = true;
					if (item.Section)
					{
						boqGrid.Rows[index].DefaultCellStyle.BackColor = PremiumControls.AccentSoft;
						boqGrid.Rows[index].DefaultCellStyle.Font = ModernUi.Strong;
					}
					else if (item.Source)
					{
						boqGrid.Rows[index].DefaultCellStyle.ForeColor = PowTheme.Muted;
					}
					else
					{
						boqGrid.Rows[index].DefaultCellStyle.Font = item.Description.StartsWith("Total", StringComparison.OrdinalIgnoreCase) ? ModernUi.Strong : ModernUi.Body;
					}
				}
				AddLegacyBoqRows();
				if (boqDisclosure != null) boqDisclosure.Apply();
boqStatus.ForeColor = list.Count>0?Color.Firebrick:PowTheme.Teal;
				boqStatus.Text = list.Count>0?"PARTIAL: "+list.Count+" input issue(s) listed below; valid quantities shown.":"Quantities are up to date. Source rows show the breakdown; count each material total once.";
			}
			catch (Exception ex)
			{
				boqGrid.Rows.Clear();
				boqStatus.ForeColor = Color.Firebrick;
				boqStatus.Text = "REVIEW: " + ex.Message;
			}
		}

		private SteelProfile FindSteelProfile(string name)
		{
			SteelProfile value;
			if (!projectSteelProfiles.TryGetValue(name, out value))
			{
				return SteelCatalog.Find(name);
			}
			return value;
		}

		private void WriteSteelProfiles(XmlElement root)
		{
			root.SetAttribute("appVersion", typeof(ProgramOfWorksForm).Assembly.GetName().Version.ToString(3));
			XmlElement xmlElement = root.OwnerDocument.CreateElement("SteelProfileSnapshot");
			root.AppendChild(xmlElement);
			foreach (string item in steelDemands.Select((SteelCutDemand d) => d.Specification.Split(new string[1] { " | " }, StringSplitOptions.None)[0]).Distinct())
			{
				SteelProfile steelProfile = FindSteelProfile(item);
				if (steelProfile != null)
				{
					XmlElement xmlElement2 = root.OwnerDocument.CreateElement("Profile");
					xmlElement2.SetAttribute("name", steelProfile.Name);
					xmlElement2.SetAttribute("kind", steelProfile.Kind);
					xmlElement2.SetAttribute("source", steelProfile.Source);
					xmlElement2.SetAttribute("H", steelProfile.H.ToString("R", CultureInfo.InvariantCulture));
					xmlElement2.SetAttribute("B", steelProfile.B.ToString("R", CultureInfo.InvariantCulture));
					xmlElement2.SetAttribute("lip", steelProfile.Lip.ToString("R", CultureInfo.InvariantCulture));
					xmlElement2.SetAttribute("t", steelProfile.T.ToString("R", CultureInfo.InvariantCulture));
					xmlElement2.SetAttribute("kgm", steelProfile.KgM.ToString("R", CultureInfo.InvariantCulture));
					xmlElement.AppendChild(xmlElement2);
				}
			}
		}

		private void ReadSteelProfiles(XmlElement root)
		{
			projectSteelProfiles.Clear();
			XmlElement xmlElement = root["SteelProfileSnapshot"];
			if (xmlElement == null)
			{
				return;
			}
			foreach (XmlElement row in xmlElement.SelectNodes("Profile"))
			{
				Func<string, double> func = delegate(string key)
				{
					double result;
					if (!double.TryParse(row.GetAttribute(key), NumberStyles.Float, CultureInfo.InvariantCulture, out result) || double.IsNaN(result) || double.IsInfinity(result) || result < 0.0)
					{
						throw new Exception("Invalid saved steel profile " + key);
					}
					return result;
				};
				SteelProfile steelProfile = new SteelProfile
				{
					Name = row.GetAttribute("name"),
					Kind = row.GetAttribute("kind"),
					Source = row.GetAttribute("source"),
					H = func("H"),
					B = func("B"),
					Lip = func("lip"),
					T = func("t"),
					KgM = func("kgm")
				};
				if (steelProfile.Name == "" || !new string[3] { "Purlin", "Angle bar", "Flat bar" }.Contains(steelProfile.Kind) || projectSteelProfiles.ContainsKey(steelProfile.Name))
				{
					throw new Exception("Invalid/duplicate saved steel profile.");
				}
				SteelWeight.Positive(steelProfile.H, steelProfile.B, steelProfile.T, steelProfile.KgM);
				projectSteelProfiles.Add(steelProfile.Name, steelProfile);
				if (steelProfile.Kind != "Flat bar")
				{
					DataGridViewComboBoxColumn dataGridViewComboBoxColumn = (DataGridViewComboBoxColumn)((steelProfile.Kind == "Purlin") ? steelPurlins : steelAngles).Columns[2];
					if (!dataGridViewComboBoxColumn.Items.Contains(steelProfile.Name))
					{
						dataGridViewComboBoxColumn.Items.Add(steelProfile.Name);
					}
				}
				if (steelProfile.Kind == "Angle bar" || steelProfile.Kind == "Flat bar")
				{
					DataGridViewComboBoxColumn dataGridViewComboBoxColumn2 = (DataGridViewComboBoxColumn)fasciaFrame.Columns[3];
					if (!dataGridViewComboBoxColumn2.Items.Contains(steelProfile.Name))
					{
						dataGridViewComboBoxColumn2.Items.Add(steelProfile.Name);
					}
				}
			}
		}

		private void AddGravelEstimate()
		{
			gravelEntries = Grid(new string[9] { "Gravel entry ID", "Description / other area", "Length m", "Width m", "Thickness mm", "Qty", "Installed gravel m3", "Gravel TO BUY m3", "Status" }, 6);
			SetDefaults(gravelEntries, new Dictionary<int, string>
			{
				{ 4, "100" },
				{ 5, "1" }
			});
			AddGrid("Gravel Estimate", gravelEntries, "Beam and footing gravel is included automatically in the Start summary. Add manual rows only for OTHER areas to avoid double-counting. Installed volume=L*W*thickness/1000*qty. Purchase allowance comes from Earthworks > Start; soil swell/shrink is not added to gravel. Manual gravel does not create excavation or backfill deductions.");
			FlowLayoutPanel flowLayoutPanel = tabs.TabPages.Cast<TabPage>().Single((TabPage p) => p.Text == "Earthworks Start").Controls.OfType<FlowLayoutPanel>().Single();
			flowLayoutPanel.Controls.Add(new Label
			{
				Text = "GRAVEL REQUIRED QUANTITIES & SOURCES",
				Width = 1100,
				Height = 38,
				Font = new Font("Segoe UI", 12f, FontStyle.Bold),
				ForeColor = PowTheme.Ink
			});
			gravelSummaryGrid = Grid(new string[2] { "Gravel quantity", "Quantity m3" }, 0);
			gravelSummaryGrid.ReadOnly = true;
			gravelSummaryGrid.AllowUserToAddRows = false;
			gravelSummaryGrid.AllowUserToDeleteRows = false;
			gravelSummaryGrid.Dock = DockStyle.None;
			gravelSummaryGrid.Width = 1100;
			gravelSummaryGrid.Height = 250;
			gravelSummaryGrid.Columns[0].Width = 650;
			gravelSummaryGrid.Columns[1].Width = 160;
			gravelSummaryGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
			gravelSummaryGrid.Columns[0].FillWeight = 80f;
			gravelSummaryGrid.Columns[1].FillWeight = 20f;
			gravelSummaryGrid.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
			flowLayoutPanel.Controls.Add(gravelSummaryGrid);
		}

		private void RecalculateGravel()
		{
			if (gravelEntries == null || gravelAllowance == null)
			{
				return;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (DataGridViewRow item in (IEnumerable)gravelEntries.Rows)
			{
				if (item.IsNewRow)
				{
					continue;
				}
				item.Tag = null;
				for (int i = 6; i <= 8; i++)
				{
					item.Cells[i].Value = "";
				}
				if (Empty(item))
				{
					continue;
				}
				try
				{
					if (Cell(item, 0) == "" || Cell(item, 1) == "" || !hashSet.Add(Cell(item, 0)))
					{
						throw new Exception("Enter a unique gravel ID and description.");
					}
					double num = GravelTakeoff.Volume(Number(item, 2), Number(item, 3), Number(item, 4), Count(item, 5));
					item.Tag = num;
					item.Cells[6].Value = num;
					item.Cells[7].Value = num * (1.0 + (double)gravelAllowance.Value / 100.0);
					item.Cells[8].Value = "Estimated";
				}
				catch (Exception ex)
				{
					item.Cells[8].Value = ex.Message;
					errors.Add("Gravel " + Cell(item, 0) + ": " + ex.Message);
				}
			}
		}

		private double ManualGravelVolume()
		{
			if (gravelEntries != null)
			{
				return (from DataGridViewRow r in gravelEntries.Rows
					where r.Tag is double
					select r).Sum((DataGridViewRow r) => (double)r.Tag);
			}
			return 0.0;
		}

		private double TotalGravelInstalled()
		{
			return beam.WorksGravel() + footingResults.Sum((FootingTakeoff q) => q.Gravel) + wallFootingResults.Sum(q=>q.Gravel) + ManualGravelVolume();
		}

		private List<object[]> GravelSummary()
		{
			double num = TotalGravelInstalled();
			List<object[]> list = new List<object[]>();
			list.Add(new object[3]
			{
				"Beam gravel (automatic)",
				beam.WorksGravel(),
				"m3"
			});
			list.Add(new object[3]
			{
				"Footing gravel (automatic)",
				footingResults.Sum((FootingTakeoff q) => q.Gravel) + wallFootingResults.Sum(q=>q.Gravel),
				"m3"
			});
			list.Add(new object[3]
			{
				"Other gravel areas (manual entries)",
				ManualGravelVolume(),
				"m3"
			});
			list.Add(new object[3] { "TOTAL gravel installed", num, "m3" });
			list.Add(new object[3]
			{
				"TOTAL GRAVEL TO BUY",
				num * (1.0 + (double)gravelAllowance.Value / 100.0),
				"m3"
			});
			return list;
		}

		private void UpdateGravelSummary()
		{
			if (gravelSummaryGrid == null || gravelAllowance == null)
			{
				return;
			}
			gravelSummaryGrid.Rows.Clear();
			List<object[]> list = GravelSummary();
			for (int i = 0; i < list.Count; i++)
			{
				gravelSummaryGrid.Rows.Add(list[i][0], Convert.ToDouble(list[i][1]).ToString("N2"));
				foreach (BillQuantity item in GravelQuantitySources(i))
				{
					int index = gravelSummaryGrid.Rows.Add("    " + item.Description, item.Quantity.ToString("N2"));
					gravelSummaryGrid.Rows[index].DefaultCellStyle.ForeColor = PowTheme.Muted;
				}
			}
			gravelSummaryGrid.Height = Math.Min(520, Math.Max(180, gravelSummaryGrid.ColumnHeadersHeight + gravelSummaryGrid.Rows.GetRowsHeight(DataGridViewElementStates.Visible) + 4));
		}

		private void AddDoorSettings(FlowLayoutPanel actions)
		{
			doorSettings = new FlowLayoutPanel
			{
				Visible = false,
				Padding = new Padding(16),
				AutoScroll = true
			};
			base.Controls.Add(doorSettings);
			jambDepthDefault = DoorNumber("Unassigned CHB thickness (mm)", 100m, 150m, 150m);
			jambThicknessDefault = DoorNumber("Jamb thickness (mm)", 10m, 100m, 50m);
			doorSettings.Controls.Add(new Label
			{
				Text = "Jamb depth equals the assigned CHB wall thickness (100 or 150 mm). Use separate opening IDs when wall thicknesses differ. Unassigned door/window schedules use the depth above. Jamb thickness is a project default; 50mm retains the existing app allowance. Wooden leaf thickness is fixed at 45mm. Confirm jamb sizes against the approved detail.",
				Width = 760,
				Height = 100
			});
			Button(actions, "Door / Window Settings", delegate
			{
				SettingsWorkspace.Show(this, "Wooden doors & windows", new string[1] { "Jamb & leaf" }, new Control[1] { doorSettings });
			});
		}

		private NumericUpDown DoorNumber(string title, decimal min, decimal max, decimal value)
		{
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Width = 350,
				Height = 80,
				FlowDirection = FlowDirection.TopDown
			};
			flowLayoutPanel.Controls.Add(new Label
			{
				Text = title,
				Width = 335,
				Height = 28
			});
			NumericUpDown numericUpDown = new NumericUpDown
			{
				Minimum = min,
				Maximum = max,
				Value = value,
				DecimalPlaces = 0,
				Width = 300,
				ReadOnly = title.StartsWith("Unassigned"),
				Increment = ((!title.StartsWith("Unassigned")) ? 1 : 50)
			};
			numericUpDown.ValueChanged += delegate
			{
				if (!filling)
				{
					Recalculate();
				}
			};
			flowLayoutPanel.Controls.Add(numericUpDown);
			doorSettings.Controls.Add(flowLayoutPanel);
			return numericUpDown;
		}

		/* wall thickness of a CHB / masonry row in mm (CHB 100 / 150, AAC 100 / 150, ...); 150 when not stated */
		private static double WallThicknessMm(DataGridViewRow r)
		{
			double t;
			return double.TryParse(Cell(r, 18), NumberStyles.Float, CultureInfo.InvariantCulture, out t) && t >= 50 && t <= 400 ? t : 150.0;
		}

		/* Owner (20 November 2026, "ipakita din yung mga opening na na-include sa masonry", "irerecord yung jamb based sa CHB wall"):
		   each CHB / masonry wall row lists the openings deducted from it, with the jamb depth taken from that wall's thickness. */
		internal const string OpeningsIncludedColumn = "OpeningsIncluded";

		private void ShowOpeningsIncluded(DataGridViewRow row, string wallId)
		{
			var grid = row.DataGridView;
			if (grid == null || !grid.Columns.Contains(OpeningsIncludedColumn)) return;
			var lines = wallOpenings.Where(o => string.Equals((o.Wall ?? "").Trim(), (wallId ?? "").Trim(), StringComparison.OrdinalIgnoreCase)).GroupBy(o => o.Id, StringComparer.OrdinalIgnoreCase)
				.Select(g => g.First().Id + " x" + g.Sum(x => x.Quantity)).ToList(); /* owner, 20 November 2026: "opening ID na lang ang ilagay tapos yung bilang" */
			row.Cells[OpeningsIncludedColumn].Value = lines.Count == 0 ? "" : string.Join("; ", lines);
		}

		private void EnsureOpeningsIncludedColumn(DataGridView grid)
		{
			if (grid != null && !grid.Columns.Contains(OpeningsIncludedColumn))
			{
				var column = new DataGridViewTextBoxColumn { Name = OpeningsIncludedColumn, HeaderText = "Openings included", ReadOnly = true, Width = 170, SortMode = DataGridViewColumnSortMode.NotSortable };
				column.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
				column.DefaultCellStyle.ForeColor = PowTheme.Muted;
				grid.Columns.Add(column);
				/* beside the Openings button (after the gross area), else after Openings m2 */
				var after = grid.Columns.Contains("EditWallOpeningsAction") ? grid.Columns["EditWallOpeningsAction"] : grid.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.HeaderText == "Openings m2" || c.HeaderText == "Less openings m2");
				if (after != null) try { column.DisplayIndex = Math.Min(grid.Columns.Count - 1, after.DisplayIndex + 1); } catch (InvalidOperationException) { }
			}
		}

		/* jamb depth groups per opening ID, from the thickness of each wall it is assigned to (v1.50.19) */
		private readonly Dictionary<string, List<KeyValuePair<double, int>>> woodJambGroups = new Dictionary<string, List<KeyValuePair<double, int>>>(StringComparer.OrdinalIgnoreCase);

		private void AutomaticWoodDimensions(DataGridViewRow row)
		{
			double num = ((jambDepthDefault == null) ? 150.0 : ((double)jambDepthDefault.Value));
			Func<string, double?> thickness = wall => { var hit = MasonryGrids().SelectMany((DataGridView g) => g.Rows.Cast<DataGridViewRow>()).FirstOrDefault(r => !r.IsNewRow && string.Equals(Cell(r, 0), wall, StringComparison.OrdinalIgnoreCase)); return hit == null ? (double?)null : WallThicknessMm(hit); };
			string id = Cell(row, 0);
			/* the row's own Wall ID (or the setting) for units not assigned to a wall */
			double rest = (Cell(row, 2) != "" ? thickness(Cell(row, 2)) : null) ?? num;
			var groups = new Dictionary<double, int>();
			int assignedUnits = 0;
			foreach (var link in openingAssignments.Where(x => string.Equals(x.Schedule, id, StringComparison.OrdinalIgnoreCase)))
			{
				double t = thickness(link.Wall) ?? rest;
				int n = link.Total ? link.Quantity : link.Quantity * WallUnits(link.Wall); int q; groups.TryGetValue(t, out q); groups[t] = q + n; assignedUnits += n;
			}
			int units = 0; int.TryParse(Cell(row, 6), NumberStyles.Integer, CultureInfo.InvariantCulture, out units);
			if (units > assignedUnits) { int q; groups.TryGetValue(rest, out q); groups[rest] = q + units - assignedUnits; }
			if (groups.Count == 0) groups[rest] = Math.Max(units, 0);
			num = groups.OrderByDescending(g => g.Value).ThenByDescending(g => g.Key).First().Key;
			if (id != "") woodJambGroups[id] = groups.Count > 1 ? groups.OrderByDescending(g => g.Key).Select(g => new KeyValuePair<double, int>(g.Key / 1000.0, g.Value)).ToList() : null;
			row.Cells[4].ToolTipText = groups.Count > 1 ? "Jamb depth by wall: " + string.Join(", ", groups.OrderByDescending(g => g.Key).Select(g => g.Value + " pc" + (g.Value == 1 ? "" : "s") + " at " + g.Key.ToString("0", CultureInfo.InvariantCulture) + " mm")) : "";
			row.Cells[7].Value = (num / 1000.0).ToString("0.###", CultureInfo.InvariantCulture);
			row.Cells[8].Value = (((jambThicknessDefault == null) ? 50.0 : ((double)jambThicknessDefault.Value)) / 1000.0).ToString("0.###", CultureInfo.InvariantCulture);
			row.Cells[9].Value = "45";
			int[] array2 = new int[3] { 7, 8, 9 };
			foreach (int index in array2)
			{
				row.Cells[index].ReadOnly = true;
				row.Cells[index].Style.BackColor = Color.FromArgb(239, 244, 248);
			}
		}

		private void WriteDoorSettings(XmlElement root)
		{
			root.SetAttribute("jambDepthDefault", jambDepthDefault.Value.ToString(CultureInfo.InvariantCulture));
			root.SetAttribute("jambThicknessDefault", jambThicknessDefault.Value.ToString(CultureInfo.InvariantCulture));
		}

		private void ReadDoorSettings(XmlElement root)
		{
			jambDepthDefault.Value = ((root.HasAttribute("jambDepthDefault") && decimal.Parse(root.GetAttribute("jambDepthDefault"), CultureInfo.InvariantCulture) <= 100m) ? 100 : 150);
			jambThicknessDefault.Value = (root.HasAttribute("jambThicknessDefault") ? decimal.Parse(root.GetAttribute("jambThicknessDefault"), CultureInfo.InvariantCulture) : 50m);
		}

		private void AddFinishingStart()
		{
			TabPage tabPage = tabs.TabPages.Cast<TabPage>().Single((TabPage p) => p.Text == "Finish Schedule");
			tabPage.Text = "Finishing Start";
			tabPage.Controls.Remove(finishSchedule);
			Control[] array = tabPage.Controls.Cast<Control>().ToArray();
			foreach (Control control in array)
			{
				tabPage.Controls.Remove(control);
				control.Dispose();
			}
			SplitContainer splitContainer = new SplitContainer
			{
				Dock = DockStyle.Fill,
				Size = new Size(1100, 650),
				Orientation = Orientation.Vertical, /* owner, 18 November 2026: Finish Schedule and Totals per Group side by side */
				SplitterDistance = 560,
				Panel1MinSize = 320,
				Panel2MinSize = 320
			};
			/* Owner (18 November 2026, "hindi napapansin yung division"): a visible divider between the two panels and an
			   outline around each panel. */
			splitContainer.SplitterWidth = 14;
			splitContainer.Panel1.Padding = new Padding(1);
			splitContainer.Panel2.Padding = new Padding(1);
			PaintEventHandler outline = delegate(object s, PaintEventArgs e)
			{
				Control c = (Control)s;
				using (Pen pen = new Pen(PowTheme.Muted))
				{
					e.Graphics.DrawRectangle(pen, 0, 0, c.Width - 1, c.Height - 1);
				}
			};
			splitContainer.Panel1.Paint += outline;
			splitContainer.Panel2.Paint += outline;
			splitContainer.Paint += delegate(object s, PaintEventArgs e)
			{
				Rectangle r = splitContainer.SplitterRectangle;
				int x = r.Left + r.Width / 2;
				using (Pen pen = new Pen(PowTheme.Teal, 3f))
				{
					e.Graphics.DrawLine(pen, x, r.Top + 6, x, r.Bottom - 6);
				}
				using (SolidBrush brush = new SolidBrush(PowTheme.Teal))
				{
					int mid = r.Top + r.Height / 2;
					for (int i = -1; i <= 1; i++)
					{
						e.Graphics.FillEllipse(brush, x - 4, mid + i * 10 - 4, 8, 8);
					}
				}
			};
			splitContainer.SplitterMoved += delegate { splitContainer.Invalidate(); };
			splitContainer.Resize += delegate { splitContainer.Invalidate(); };
			bool finishSplitPlaced = false;
			splitContainer.SizeChanged += delegate
			{
				if (finishSplitPlaced || splitContainer.Width < 700) return;
				finishSplitPlaced = true;
				try { splitContainer.SplitterDistance = (int)(splitContainer.Width * 0.48); } catch (InvalidOperationException) { }
			};
			Label value = new Label
			{
				Text = "FINISH SCHEDULE",
				Dock = DockStyle.Top,
				Height = 42,
				Padding = new Padding(10),
				Font = new Font("Segoe UI", 11f, FontStyle.Bold)
			};
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				Height = 44,
				Padding = new Padding(6)
			};
			Button(flowLayoutPanel, "Add Finish ID", delegate
			{
				finishSchedule.Rows.Add();
			});
			Button(flowLayoutPanel, "Remove Finish ID", delegate { GridEditHistory.Attach(finishSchedule).DeleteSelected(); });

			splitContainer.Panel1.Controls.Add(finishSchedule);
			splitContainer.Panel1.Controls.Add(flowLayoutPanel);
			splitContainer.Panel1.Controls.Add(value);
			finishAssignments = Grid(new string[5] { "Category", "Entry ID", "Description", "Finish ID", "Group / material" }, 0);
			finishAssignments.Name = "FinishAssignments";
			finishAssignments.AllowUserToAddRows = false;
			finishAssignments.AllowUserToDeleteRows = false;
			finishAssignments.Columns[0].Width = 110;
			finishAssignments.Columns[1].Width = 130;
			finishAssignments.Columns[2].Width = 260;
			finishAssignments.Columns[4].Width = 350;
			AddCombo(finishAssignments, 3, "Finish ID", new string[1] { "" });
			finishAssignments.CurrentCellDirtyStateChanged += delegate
			{
				if (finishAssignments.IsCurrentCellDirty && finishAssignments.CurrentCell is DataGridViewComboBoxCell)
				{
					finishAssignments.CommitEdit(DataGridViewDataErrorContexts.Commit);
				}
			};
			finishAssignments.CellValueChanged += delegate(object s, DataGridViewCellEventArgs e)
			{
				if (!filling && e.RowIndex >= 0 && e.ColumnIndex == 3)
				{
					DataGridViewRow dataGridViewRow = finishAssignments.Rows[e.RowIndex];
					DataGridViewRow dataGridViewRow2 = dataGridViewRow.Tag as DataGridViewRow;
					if (dataGridViewRow2 != null && dataGridViewRow2.DataGridView != null)
					{
						dataGridViewRow2.Cells[2].Value = Cell(dataGridViewRow, 3);
						string value2;
						dataGridViewRow.Cells[4].Value = (finishGroups.TryGetValue(Cell(dataGridViewRow, 3), out value2) ? value2 : "");
					}
				}
			};
			splitContainer.Panel2.Controls.Add(FinishGroupTotalsGrid());splitContainer.Panel2.Controls.Add(FinishGroupChartControl()); /* owner, 18 November 2026: consolidated per group; Finish IDs are chosen on the measurement sheets */
			/* Owner (18 November 2026, "ang pangit"): both panels share one heading style and their tables start at the same height. */
			splitContainer.Panel2.Controls.Add(new Label
			{
				Text = "Areas with the same group are added together and go to that BOQ item. Pick each area's Finish ID in Wall / Ceiling / Floor Finishes.",
				Dock = DockStyle.Top,
				Height = 44,
				Padding = new Padding(10, 4, 10, 4),
				ForeColor = PowTheme.Muted,
				TextAlign = ContentAlignment.MiddleLeft
			});
			splitContainer.Panel2.Controls.Add(new Label
			{
				Text = "TOTALS PER GROUP",
				Dock = DockStyle.Top,
				Height = 42,
				Padding = new Padding(10),
				Font = new Font("Segoe UI", 11f, FontStyle.Bold)
			});
			tabPage.Controls.Add(splitContainer);
			DataGridView[] array2 = new DataGridView[4] { wallFinishes, ceilingFinishes, floorFinishes, otherFinishes };
			foreach (DataGridView dataGridView in array2)
			{
				/* Owner (18 November 2026, "pano ko i-assign yung mga na-measure kong area sa Finish ID"): the Finish ID can be
				   chosen directly on each measurement row; the Assign Finishes table at Start shows and edits the same value. */
				dataGridView.Columns[2].Visible = true;
				dataGridView.Columns[2].ToolTipText = "Choose the Finish ID from the Finish Schedule (Finishing Works > Start). The same choice appears in Assign Finishes.";
				dataGridView.Columns[(dataGridView == wallFinishes) ? 14 : 10].Visible = false;
			}
		}

		private void RefreshFinishAssignments()
		{
			if (finishAssignments == null || !(finishAssignments.Columns[3] is DataGridViewComboBoxColumn))
			{
				return;
			}
			string text = ((finishAssignments.CurrentRow == null) ? "" : Cell(finishAssignments.CurrentRow, 1));
			int firstDisplayedScrollingRowIndex = finishAssignments.FirstDisplayedScrollingRowIndex;
			DataGridViewComboBoxColumn obj = (DataGridViewComboBoxColumn)finishAssignments.Columns[3];
			obj.Items.Clear();
			obj.Items.Add("");
			obj.Items.AddRange((from v in (from v in finishGroups.Keys.Concat(from r in new DataGridView[4] { wallFinishes, ceilingFinishes, floorFinishes, otherFinishes }.SelectMany((DataGridView g) => g.Rows.Cast<DataGridViewRow>())
						where !r.IsNewRow
						select Cell(r, 2))
					where v != ""
					select v).Distinct(StringComparer.OrdinalIgnoreCase)
				orderby v
				select v).Cast<object>().ToArray());
			finishAssignments.Rows.Clear();
			DataGridView[] array = new DataGridView[4] { wallFinishes, ceilingFinishes, floorFinishes, otherFinishes };
			foreach (DataGridView dataGridView in array)
			{
				foreach (DataGridViewRow item in (IEnumerable)dataGridView.Rows)
				{
					if (!item.IsNewRow && !Empty(item))
					{
						string value;
						int index = finishAssignments.Rows.Add(CategoryOf(dataGridView), Cell(item, 0), Cell(item, 1), Cell(item, 2), finishGroups.TryGetValue(Cell(item, 2), out value) ? value : "");
						finishAssignments.Rows[index].Tag = item;
						FilterAssignmentChoices(finishAssignments.Rows[index], CategoryOf(dataGridView));
						if (Cell(item, 0) == text)
						{
							finishAssignments.CurrentCell = finishAssignments.Rows[index].Cells[3];
						}
					}
				}
			}
			if (firstDisplayedScrollingRowIndex >= 0 && firstDisplayedScrollingRowIndex < finishAssignments.Rows.Count)
			{
				finishAssignments.FirstDisplayedScrollingRowIndex = firstDisplayedScrollingRowIndex;
			}
		}


		private void PlumbingExcel(List<WorksExcelTable> tables)
		{
			tables.Add(new WorksExcelTable("POW Plumbing Summary", new string[3] { "Plumbing item / specification", "Quantity", "Unit" }, (from q in PlumbingQuantities()
				select new object[3] { q.Description, q.Quantity, q.Unit }).ToList()));
		}

		private void AddRCWalls()
		{
			rcWalls = Grid(new string[27]
			{
				"RC wall ID", "Description / location", "Length m", "Height m", "Thickness mm", "Qty", "Cover mm", "Rebar layers (both ways)", "Vertical dia mm", "Vertical spacing mm",
				"Vertical count / layer", "Horizontal dia mm", "Horizontal spacing mm", "Horizontal count / layer", "Vertical extra ends mm (total)", "Horizontal extra ends mm (total)", "Formed main faces (0/1/2)", "Formed vertical ends (0/1/2)", "Bottom soffit form?", "Concrete MPa",
				"Grade <=12 mm", "Grade >=16 mm", "Concrete m3", "Formworks m2", "Net rebar kg", "Rebar TO BUY kg", "Status"
			}, 22);
			AddCombo(rcWalls, 7, "Rebar layers (both ways)", new string[2] { "1", "2" });
			/* side B of a two-layer wall: its own diameter and spacing, blank = as side A (owner, 21 November 2026) */
			foreach (string sideB in new[] { "Side B vertical dia mm", "Side B vertical spacing mm", "Side B horizontal dia mm", "Side B horizontal spacing mm" })
			{
				var column = new DataGridViewTextBoxColumn { HeaderText = sideB, Width = 110, SortMode = DataGridViewColumnSortMode.NotSortable, ToolTipText = "Two layers only: the bars on side B. Blank = the same as side A." };
				column.DefaultCellStyle.BackColor = ModernUi.Input;
				rcWalls.Columns.Add(column);
			}
			AddCombo(rcWalls, 16, "Formed main faces (0/1/2)", new string[3] { "2", "1", "0" });
			AddCombo(rcWalls, 17, "Formed vertical ends (0/1/2)", new string[3] { "2", "1", "0" });
			rcWalls.Columns.RemoveAt(18);
			rcWalls.Columns.Insert(18, new DataGridViewCheckBoxColumn
			{
				Name = "WallBottomForm",
				HeaderText = "Bottom soffit form?"
			});
			for (int i = 19; i <= 21; i++)
			{
				rcWalls.Columns[i].Visible = false;
			}
			SetDefaults(rcWalls, new Dictionary<int, string>
			{
				{ 4, "200" },
				{ 5, "1" },
				{ 6, "40" },
				{ 7, "2" },
				{ 8, "12" },
				{ 9, "200" },
				{ 11, "10" },
				{ 12, "200" },
				{ 14, "0" },
				{ 15, "0" },
				{ 16, "2" },
				{ 17, "2" },
				{ 19, "21" },
				{ 20, "40" },
				{ 21, "60" }
			});
			rcWalls.CurrentCellDirtyStateChanged += delegate
			{
				if (rcWalls.IsCurrentCellDirty && (rcWalls.CurrentCell is DataGridViewCheckBoxCell || rcWalls.CurrentCell is DataGridViewComboBoxCell))
				{
					rcWalls.CommitEdit(DataGridViewDataErrorContexts.Commit);
				}
			};
			rcWalls.CellValueChanged += delegate(object s, DataGridViewCellEventArgs e)
			{
				if (!filling && e.RowIndex >= 0 && (e.ColumnIndex == 7 || e.ColumnIndex == 16 || e.ColumnIndex == 17 || e.ColumnIndex == 18))
				{
					Recalculate();
				}
			};
			AddGrid("RC Wall Estimate", rcWalls, "Rectangular solid wall panels: L x H x thickness x quantity. Vertical bars run along H and are counted across L; horizontal bars run along L and are counted across H. Enter spacing OR bars per layer in each direction. One layer = one vertical/horizontal mat; two layers = the same mat at both faces. Forms use selected main faces and exposed vertical ends; top excluded, bottom optional. For openings, quantify solid panels separately; bar trimming/anchorage follows approved details.");
			rcWallLayerSummary = new Label
			{
				Dock = DockStyle.Top,
				Height = 60,
				Padding = new Padding(8),
				Text = "Select a wall. One layer is one vertical/horizontal mat; two layers repeat it at both faces."
			};
			tabs.TabPages.Cast<TabPage>().Single((TabPage p) => p.Text == "RC Wall Estimate").Controls.Add(rcWallLayerSummary);
			rcWalls.SelectionChanged += delegate
			{
				UpdateRCWallLayerSummary();
			};
			rcWalls.Columns[24].DisplayIndex = 8;
			rcWalls.Columns[25].DisplayIndex = 9;
		}

		/* side B bars of a two-layer RC wall; a blank diameter keeps side A, a blank spacing takes side A's spacing / count */
		private FootingBarSpec SideB(DataGridViewRow row, int dia, int spacing, int aDia, int aSpacing, int aCount)
		{
			if (Cell(row, dia) == "" && Cell(row, spacing) == "") return null;
			int d = Cell(row, dia) == "" ? Count(row, aDia) : Count(row, dia);
			if (Cell(row, spacing) != "") return new FootingBarSpec { Diameter = d, Spacing = Number(row, spacing) };
			return new FootingBarSpec { Diameter = d, Spacing = OptionalNumber(row, aSpacing), Count = Cell(row, aCount) == "" ? 0 : Count(row, aCount) };
		}

		private void UpdateRCWallLayerSummary()
		{
			if (rcWallLayerSummary == null)
			{
				return;
			}
			DataGridViewRow currentRow = rcWalls.CurrentRow;
			RCWallTakeoff q = ((currentRow == null) ? null : (currentRow.Tag as RCWallTakeoff));
			if (q == null)
			{
				rcWallLayerSummary.Text = ((currentRow == null || currentRow.IsNewRow) ? "Select a wall to see total bars and weight for the selected rebar layers." : ("Complete the wall inputs: " + Cell(currentRow, 26)));
				return;
			}
			int num = q.Steel.Result.Lines.Where((EstimateLine l) => l.Part.StartsWith("VERTICAL")).Sum((EstimateLine l) => l.PiecesPerBeam);
			int num2 = q.Steel.Result.Lines.Where((EstimateLine l) => l.Part.StartsWith("HORIZONTAL")).Sum((EstimateLine l) => l.PiecesPerBeam);
			rcWallLayerSummary.Text = q.Layers + " layer(s), both ways | Cover " + (q.Cover * 1000.0).ToString("0.#") + "mm | Per wall: " + num + " vertical bars + " + num2 + " horizontal bars | Net " + q.Steel.Result.Lines.Sum((EstimateLine l) => l.Kg).ToString("N2") + " kg for " + q.Quantity + " wall(s) | Buy " + rcWallPlan.Where((StockAllocation a) => a.Entry == q.Steel).Sum((StockAllocation a) => a.Purchase).ToString("N2") + " kg";
		}

		private void AddRCWallSettings(FlowLayoutPanel actions)
		{
			rcWallSettings = new FlowLayoutPanel
			{
				Visible = false,
				AutoScroll = true,
				Padding = new Padding(12)
			};
			base.Controls.Add(rcWallSettings);
			rcWallFc = SettingChoice(rcWallSettings, "RC wall concrete MPa", new string[3] { "21", "28", "35" });
			rcWallSmall = SettingChoice(rcWallSettings, "Rebar <=12 mm grade", new string[2] { "40", "60" });
			rcWallLarge = SettingChoice(rcWallSettings, "Rebar >=16 mm grade", new string[2] { "60", "40" });
			Button(rcWallSettings, "Available commercial lengths", delegate
			{
				List<double> list = StockLengthPicker.Choose(this, rcWallStock);
				if (list != null)
				{
					rcWallStock = list;
					Recalculate();
				}
			});
			Button(rcWallSettings, "Apply to all RC walls", delegate
			{
				Commit();
				filling = true;
				try
				{
					foreach (DataGridViewRow item in (IEnumerable)rcWalls.Rows)
					{
						if (!item.IsNewRow)
						{
							RCWallMaterials(item);
						}
					}
				}
				finally
				{
					filling = false;
				}
				Recalculate();
			});
			Button(actions, "RC Wall Settings", delegate
			{
				SettingsWorkspace.Show(this, "RC Walls", new string[1] { "Materials & commercial lengths" }, new Control[1] { rcWallSettings });
			});
			rcWalls.DefaultValuesNeeded += delegate(object s, DataGridViewRowEventArgs e)
			{
				RCWallMaterials(e.Row);
			};
			rcWalls.RowsAdded += delegate(object s, DataGridViewRowsAddedEventArgs e)
			{
				if (!filling)
				{
					for (int i = e.RowIndex; i < e.RowIndex + e.RowCount; i++)
					{
						if (!rcWalls.Rows[i].IsNewRow)
						{
							RCWallMaterials(rcWalls.Rows[i]);
						}
					}
				}
			};
		}

		private void RCWallMaterials(DataGridViewRow row)
		{
			row.Cells[19].Value = rcWallFc.Text;
			row.Cells[20].Value = rcWallSmall.Text;
			row.Cells[21].Value = rcWallLarge.Text;
		}

		private void RecalculateRCWalls()
		{
			rcWallResults.Clear();
			rcWallPlan.Clear();
			if (rcWalls == null)
			{
				return;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (DataGridViewRow item in (IEnumerable)rcWalls.Rows)
			{
				if (item.IsNewRow)
				{
					continue;
				}
				item.Tag = null;
				for (int i = 22; i <= 26; i++)
				{
					item.Cells[i].Value = "";
				}
				if (Empty(item))
				{
					continue;
				}
				try
				{
					RequireDescription(item);
					int num = Count(item, 20);
					int num2 = Count(item, 21);
					LengthConfiguration lengthConfiguration = CalculationStore.Resolve(Number(item, 19), num, (num == 40) ? 275 : 420, num2, (num2 == 40) ? 275 : 420);
					RCWallTakeoff rCWallTakeoff = RCWallTakeoff.Calculate(Cell(item, 0), Cell(item, 1), Number(item, 2), Number(item, 3), Number(item, 4), Count(item, 5), Number(item, 6), Count(item, 7), new FootingBarSpec
					{
						Diameter = Count(item, 8),
						Spacing = OptionalNumber(item, 9),
						Count = ((!(Cell(item, 10) == "")) ? Count(item, 10) : 0)
					}, new FootingBarSpec
					{
						Diameter = Count(item, 11),
						Spacing = OptionalNumber(item, 12),
						Count = ((!(Cell(item, 13) == "")) ? Count(item, 13) : 0)
					}, OptionalNumber(item, 14), OptionalNumber(item, 15), Count(item, 16), Count(item, 17), Cell(item, 18) != "" && bool.Parse(Cell(item, 18)), lengthConfiguration,
						SideB(item, 27, 28, 8, 9, 10), SideB(item, 29, 30, 11, 12, 13));
					if (!hashSet.Add(rCWallTakeoff.Id))
					{
						throw new Exception("Duplicate RC wall ID.");
					}
					item.Tag = rCWallTakeoff;
					rcWallResults.Add(rCWallTakeoff);
					rcWallPlan.AddRange(CommercialBars.Allocate(new EstimateInput[1] { rCWallTakeoff.Steel }, lengthConfiguration, rcWallStock));
					item.Cells[22].Value = rCWallTakeoff.Concrete;
					item.Cells[23].Value = rCWallTakeoff.Forms;
					item.Cells[24].Value = rCWallTakeoff.Steel.Result.Lines.Sum((EstimateLine l) => l.Kg);
					item.Cells[26].Value = "Estimated";
				}
				catch (Exception ex)
				{
					item.Cells[26].Value = ex.Message;
					errors.Add("RC wall " + Cell(item, 0) + ": " + ex.Message);
				}
			}
		}

		private void UpdateRCWallPurchases()
		{
			if (rcWalls == null)
			{
				return;
			}
			foreach (DataGridViewRow item in (IEnumerable)rcWalls.Rows)
			{
				RCWallTakeoff q = item.Tag as RCWallTakeoff;
				if (q != null)
				{
					List<StockAllocation> source = rcWallPlan.Where((StockAllocation a) => a.Entry == q.Steel).ToList();
					item.Cells[25].Value = source.Sum((StockAllocation a) => a.Purchase);
					if (source.Any((StockAllocation a) => a.Choice == null))
					{
						item.Cells[26].Value = "Unresolved commercial rebar cut";
						errors.Add("RC wall " + q.Id + ": unresolved stock cut.");
					}
				}
			}
			UpdateRCWallLayerSummary();
		}

		private void AppendRCWallReport(StringBuilder b, bool detailed)
		{
			b.AppendLine();
			b.AppendLine("RC WALLS | VERTICAL & HORIZONTAL REINFORCEMENT");
			foreach (RCWallTakeoff q in rcWallResults)
			{
				b.AppendLine(q.Id + " | " + q.Description + " | Concrete " + q.Fc + " MPa | " + q.Layers + " rebar layer(s), both directions");
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Concrete = {0} x {1} x {2} x {3} = {4:0.########}m3", q.Length, q.Height, q.Thickness, q.Quantity, q.Concrete));
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Forms = ({0} x L x H + {1} x H x T + {2} x L x T) x qty = {3:0.########}m2", q.FormFaces, q.EndFaces, q.BottomForm ? 1 : 0, q.Forms));
				if (!detailed)
				{
					continue;
				}
				foreach (EstimateLine line in q.Steel.Result.Lines)
				{
					b.AppendLine("  " + line.Part + " | G" + line.Grade + " D" + line.Diameter + " | " + line.Formula);
				}
				PurchaseReports.Entry(b, rcWallPlan.Where((StockAllocation a) => a.Entry == q.Steel).ToList(), q.Quantity, reportDetail.SelectedIndex == 2);
			}
			b.AppendLine("Gross solid panels only. Quantify solid segments around openings separately. No automatic opening-bar trimming or structural design. Enter approved total end allowances; commercial purchase allocation checks compatible project offcuts.");
		}

		private void WriteRCWallSettings(XmlElement root)
		{
			root.SetAttribute("rcWallStock", CommercialBars.FormatLengths(rcWallStock));
			root.SetAttribute("rcWallFc", rcWallFc.Text);
			root.SetAttribute("rcWallSmall", rcWallSmall.Text);
			root.SetAttribute("rcWallLarge", rcWallLarge.Text);
		}

		private void ReadRCWallSettings(XmlElement root)
		{
			rcWallStock = CommercialBars.ParseLengths(root.GetAttribute("rcWallStock"));
			var array = new[]
			{
				new
				{
					Box = rcWallFc,
					Key = "rcWallFc",
					Fallback = "21"
				},
				new
				{
					Box = rcWallSmall,
					Key = "rcWallSmall",
					Fallback = "40"
				},
				new
				{
					Box = rcWallLarge,
					Key = "rcWallLarge",
					Fallback = "60"
				}
			};
			foreach (var anon in array)
			{
				string text = (root.HasAttribute(anon.Key) ? root.GetAttribute(anon.Key) : anon.Fallback);
				if (!anon.Box.Items.Contains(text))
				{
					throw new Exception("Invalid saved RC wall setting.");
				}
				anon.Box.SelectedItem = text;
			}
		}

		private void AddChamberSheets()
		{
			TabPage tabPage = tabs.TabPages.Cast<TabPage>().Single((TabPage p) => p.Text == "Plumbing Rebar");
			tabPage.Controls.Remove(plumbingRebar);
			tabs.TabPages.Remove(tabPage);
			tabPage.Dispose();
			plumbingRebar.Visible = false;
			base.Controls.Add(plumbingRebar);
			plumbingRebar.Columns[1].Visible = false;
			plumbingRebar.DefaultValuesNeeded += delegate(object s, DataGridViewRowEventArgs e)
			{
				e.Row.Cells[1].Value = activeChamberRebarId;
			};
			plumbingRebar.RowsAdded += delegate(object s, DataGridViewRowsAddedEventArgs e)
			{
				if (!filling && activeChamberRebarId != "")
				{
					for (int i = e.RowIndex; i < e.RowIndex + e.RowCount; i++)
					{
						if (!plumbingRebar.Rows[i].IsNewRow && Cell(plumbingRebar.Rows[i], 1) == "")
						{
							plumbingRebar.Rows[i].Cells[1].Value = activeChamberRebarId;
						}
					}
				}
			};
			string[] array = new string[4] { "Septic Vault - Concrete", "Septic Vault - CHB", "Catch Basin - CHB", "Lined Canal" };
			string[] array2 = new string[4] { "Septic Vault, Concrete", "Septic Vault, CHB", "Catch Basin (CHB)", "Lined Canal" };
			for (int num = 0; num < array.Length; num++)
			{
				ChamberSheetView view = new ChamberSheetView
				{
					Kind = array2[num],
					Page = new TabPage(array[num])
				};
				chamberSheets.Add(view);
				FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
				{
					Dock = DockStyle.Top,
					AutoSize = true,
					Padding = new Padding(6)
				};
				Button(flowLayoutPanel, "Add structure", delegate
				{
					EditChamber(view, true);
				});
				Button(flowLayoutPanel, "Edit dimensions", delegate
				{
					EditChamber(view, false);
				});
				Button(flowLayoutPanel, "Remove structure", delegate
				{
					RemoveChamber(view);
				});
				Button(flowLayoutPanel, "Save Profile", delegate
				{
					SaveStructureProfile(view);
				});
				Button(flowLayoutPanel, "Load Profile", delegate
				{
					LoadStructureProfile(view);
				});
				Button(flowLayoutPanel, "Export Profiles", ExportStructureProfiles);
				Button(flowLayoutPanel, "Import Profiles", ImportStructureProfiles);
				view.Entries = Grid(new string[6] { "Structure ID", "Description", "Count", "Concrete m3", "RSB purchase kg", "Status" }, 0);
				view.Entries.ReadOnly = true;
				view.Entries.AllowUserToAddRows = false;
                GridEditHistory.LinkProxy(view.Entries,plumbingChambers);
				view.Entries.AllowUserToDeleteRows = false;
				view.Entries.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
				view.Entries.MultiSelect = false;
				view.Entries.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
				view.Entries.Columns[1].FillWeight = 220f;
				view.Entries.Columns[5].FillWeight = 240f;
				view.Materials = Grid(new string[5] { "Item", "Required material / work", "Computed quantity", "Unit", "Computation" }, 0);
				view.Materials.ReadOnly = true;
				view.Materials.AllowUserToAddRows = false;
				view.Materials.AllowUserToDeleteRows = false;
				view.Materials.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
				view.Materials.Columns[1].FillWeight = 240f;
				view.Materials.Columns[4].FillWeight = 320f;
				view.Materials.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
				view.Dimensions = new Label
				{
					Dock = DockStyle.Top,
					Height = 68,
					Padding = new Padding(8),
					Text = "Add a structure, enter its dimensions, then select a material computation below."
				};
				view.MaterialTabs = new NavigationTabs
				{
					Dock = DockStyle.Fill,
					Multiline = true
				};
				string[][] array3 = new string[12][]
				{
					new string[2] { "Members", "MEMBERS" },
					new string[2] { "Summary", "" },
					new string[2] { "Concrete", "900(1)i" },
					new string[2] { "RSB", "RSB" },
					new string[2] { "Extra rebar", "EXTRA" },
					new string[2] { "Excavation", "803(1)a" },
					new string[2] { "Embankment", "804(1)a" },
					new string[2] { "Gravel", "804(7)" },
					new string[2] { "Formworks", "903(2)" },
					new string[2] { "CHB", "CHB" },
					new string[2] { "Plastering", "1027(1)" },
					new string[2] { "Waterproofing", "1016(1)b" }
				};
				foreach (string[] array4 in array3)
				{
					if (array4[0] == "Waterproofing" && view.Kind == "Catch Basin (CHB)")
					{
						continue;
					}
					TabPage tabPage2 = new TabPage(array4[0])
					{
						Tag = array4[1]
					};
					view.MaterialTabs.TabPages.Add(tabPage2);
					if (array4[0] == "RSB")
					{
						view.CutDetails = Grid(new string[6] { "Member / bar direction", "Diameter mm", "Bars (all units)", "Cut length m", "Net weight kg", "Computation" }, 0);
						view.CutDetails.ReadOnly = true;
						view.CutDetails.AllowUserToAddRows = false;
						view.CutDetails.AllowUserToDeleteRows = false;
						view.CutDetails.Columns[0].Width = 300;
						view.CutDetails.Columns[5].Width = 600;
						tabPage2.Controls.Add(view.CutDetails);
					}
					if (array4[0] == "Members")
					{
						FlowLayoutPanel flowLayoutPanel2 = new FlowLayoutPanel
						{
							Dock = DockStyle.Top,
							Height = 44
						};
						Button(flowLayoutPanel2, "Add member", delegate
						{
							EditStructureMember(view, true);
						});
						Button(flowLayoutPanel2, "Edit member", delegate
						{
							EditStructureMember(view, false);
						});
						Button(flowLayoutPanel2, "Remove member", delegate {GridEditHistory.Attach(plumbingMembers).DeleteRows(plumbingMembers.SelectedCells.Cast<DataGridViewCell>().Select(c=>c.OwningRow).Where(r=>r.Visible&&Cell(r,1)==activeStructureMemberId));});

						CheckBox advanced = new CheckBox
						{
							Text = "Show reinforcement inputs",
							AutoSize = true,
							Padding = new Padding(6)
						};
						advanced.CheckedChanged += delegate
						{
							for (int i = 8; i < 24; i++)
							{
								plumbingMembers.Columns[i].Visible = advanced.Checked;
							}
						};
						flowLayoutPanel2.Controls.Add(advanced);
						tabPage2.Controls.Add(flowLayoutPanel2);
					}
					if (!(array4[0] == "Extra rebar"))
					{
						continue;
					}
					FlowLayoutPanel flowLayoutPanel3 = new FlowLayoutPanel
					{
						Dock = DockStyle.Top,
						Height = 42
					};
					Button(flowLayoutPanel3, "Add rebar cut", delegate
					{
						BindChamberRebar(view);
						if (activeChamberRebarId == "")
						{
							throw new Exception("Select a structure first.");
						}
						plumbingRebar.Rows.Add();
					});
					Button(flowLayoutPanel3, "Remove rebar cut", delegate {GridEditHistory.Attach(plumbingRebar).DeleteRows(plumbingRebar.SelectedCells.Cast<DataGridViewCell>().Select(c=>c.OwningRow).Where(r=>!r.IsNewRow&&r.Visible&&Cell(r,1).Equals(activeChamberRebarId,StringComparison.OrdinalIgnoreCase)));});

					tabPage2.Controls.Add(flowLayoutPanel3);
				}
				view.MaterialTabs.Deselecting += delegate(object s, TabControlCancelEventArgs e)
				{
					if (Convert.ToString(e.TabPage.Tag) == "EXTRA" && !plumbingRebar.EndEdit())
					{
						e.Cancel = true;
					}
					if (Convert.ToString(e.TabPage.Tag) == "MEMBERS" && !plumbingMembers.EndEdit())
					{
						e.Cancel = true;
					}
				};
				view.MaterialTabs.SelectedIndexChanged += delegate
				{
					UpdateChamberMaterials(view);
				};
				TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
				{
					Dock = DockStyle.Fill,
					ColumnCount = 1,
					RowCount = 2,
					Padding = new Padding(0),
					Margin = new Padding(0)
				};
				tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
				tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 165f));
				tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
				Panel panel = new Panel
				{
					Dock = DockStyle.Fill,
					Margin = new Padding(0)
				};
				panel.Controls.Add(view.MaterialTabs);
				panel.Controls.Add(view.Dimensions);
				tableLayoutPanel.Controls.Add(view.Entries, 0, 0);
				tableLayoutPanel.Controls.Add(panel, 0, 1);
				view.Page.Controls.Add(tableLayoutPanel);
				view.Page.Controls.Add(flowLayoutPanel);
				tabs.TabPages.Add(view.Page);
				view.Entries.SelectionChanged += delegate
				{
					if (!view.Updating)
					{
						DataGridViewRow currentRow = view.Entries.CurrentRow;
						if (currentRow != null)
						{
							view.SelectedId = Convert.ToString(currentRow.Cells[0].Value);
						}
						UpdateChamberMaterials(view);
					}
				};
				view.Entries.CellDoubleClick += delegate(object s, DataGridViewCellEventArgs e)
				{
					if (e.RowIndex >= 0)
					{
						EditChamber(view, false);
					}
				};
			}
		}

		private ChamberSheetView ChamberView(Control page)
		{
			while (page != null)
			{
				ChamberSheetView chamberSheetView = chamberSheets.FirstOrDefault((ChamberSheetView v) => v.Page == page);
				if (chamberSheetView != null)
				{
					return chamberSheetView;
				}
				page = page.Parent;
			}
			return null;
		}

		private void RefreshChamberSheet(TabPage page)
		{
			ChamberSheetView chamberSheetView = ChamberView(page);
			if (chamberSheetView == null)
			{
				return;
			}
			if (chamberSheetView.Revision == chamberRevision)
			{
				if (!filling && Convert.ToString(chamberSheetView.MaterialTabs.SelectedTab.Tag) == "EXTRA")
				{
					BindChamberRebar(chamberSheetView);
				}
				if (!filling && Convert.ToString(chamberSheetView.MaterialTabs.SelectedTab.Tag) == "MEMBERS")
				{
					BindStructureMembers(chamberSheetView);
				}
				return;
			}
			chamberSheetView.Updating = true;
			chamberSheetView.Entries.SuspendLayout();
			try
			{
				chamberSheetView.Entries.Rows.Clear();
				foreach (DataGridViewRow item in (IEnumerable)plumbingChambers.Rows)
				{
					if (!item.IsNewRow && !(Cell(item, 2) != chamberSheetView.Kind))
					{
						int index = chamberSheetView.Entries.Rows.Add(Cell(item, 0), Cell(item, 1), Cell(item, 6), Cell(item, 28), Cell(item, 29), Cell(item, 34));
						chamberSheetView.Entries.Rows[index].Tag = item;
						if (Cell(item, 0) == chamberSheetView.SelectedId)
						{
							chamberSheetView.Entries.CurrentCell = chamberSheetView.Entries.Rows[index].Cells[0];
						}
					}
				}
				if (chamberSheetView.Entries.CurrentRow != null)
				{
					chamberSheetView.SelectedId = Convert.ToString(chamberSheetView.Entries.CurrentRow.Cells[0].Value);
				}
				chamberSheetView.Revision = chamberRevision;
			}
			finally
			{
				chamberSheetView.Entries.ResumeLayout();
				chamberSheetView.Updating = false;
			}
			UpdateChamberMaterials(chamberSheetView);
		}

		private void BindChamberRebar(ChamberSheetView view)
		{
			if (filling || view.MaterialTabs.SelectedTab == null || Convert.ToString(view.MaterialTabs.SelectedTab.Tag) != "EXTRA")
			{
				return;
			}
			DataGridViewRow dataGridViewRow = ((view.Entries.CurrentRow == null) ? null : (view.Entries.CurrentRow.Tag as DataGridViewRow));
			string text = ((dataGridViewRow == null) ? "" : Cell(dataGridViewRow, 0));
			plumbingRebar.EndEdit();
			plumbingRebar.CurrentCell = null;
			plumbingRebar.ClearSelection();
			activeChamberRebarId = text;
			view.MaterialTabs.SelectedTab.Controls.Add(plumbingRebar);
			plumbingRebar.Dock = DockStyle.Fill;
			plumbingRebar.Visible = text != "";
			view.MaterialTabs.SelectedTab.Controls.SetChildIndex(plumbingRebar, 0);
			foreach (DataGridViewRow item in (IEnumerable)plumbingRebar.Rows)
			{
				if (!item.IsNewRow)
				{
					item.Visible = text != "" && Cell(item, 1).Equals(text, StringComparison.OrdinalIgnoreCase);
				}
			}
		}

		private void UpdateChamberMaterials(ChamberSheetView view)
		{
			if (view.MaterialTabs.SelectedTab == null)
			{
				return;
			}
			string selected = Convert.ToString(view.MaterialTabs.SelectedTab.Tag);
			DataGridViewRow source = ((view.Entries.CurrentRow == null) ? null : (view.Entries.CurrentRow.Tag as DataGridViewRow));
			ChamberTakeoff chamberTakeoff = ((source == null) ? null : chamberMeasures.FirstOrDefault((ChamberTakeoff x) => x.Id == Cell(source, 0) && x.Kind == view.Kind));
			view.Dimensions.Text = ((chamberTakeoff == null) ? ("Select a structure and complete its dimensions. " + ((source == null) ? "" : Cell(source, 34))) : string.Format(CultureInfo.InvariantCulture, "{0} | {1:0.###} x {2:0.###}m | Clear H {3:0.###}m | Wall {4:0.###}m | Base {5:0.###}m | Count {6} | RSB {7:N2}kg\r\n{8}", chamberTakeoff.Id, chamberTakeoff.L, chamberTakeoff.W, chamberTakeoff.H, chamberTakeoff.Wall, chamberTakeoff.Base, chamberTakeoff.Count, chamberTakeoff.RebarPurchase, Cell(source, 34)));
			if (selected == "MEMBERS")
			{
				BindStructureMembers(view);
				return;
			}
			if (selected == "RSB")
			{
				UpdateStructureCutDetails(view, chamberTakeoff);
				return;
			}
			if (selected == "EXTRA")
			{
				BindChamberRebar(view);
				return;
			}
			view.MaterialTabs.SelectedTab.Controls.Add(view.Materials);
			view.Materials.SuspendLayout();
			try
			{
				view.Materials.Rows.Clear();
				if (chamberTakeoff == null)
				{
					return;
				}
				foreach (BillQuantity item in from m in chamberTakeoff.Materials()
					where selected == "" || m.Section == selected || (selected == "CHB" && m.Section.StartsWith("1046"))
					select m)
				{
					view.Materials.Rows.Add(item.Section, item.Description, item.Quantity.ToString("N3", CultureInfo.InvariantCulture), item.Unit, item.Computation);
				}
				if (selected == "900(1)i")
				{
					view.Materials.Rows.Add("", chamberTakeoff.ComponentMode ? "Slab-on-fill members" : "Base slab", chamberTakeoff.BaseConcrete, "m3", "Outside length x outside width x base thickness x count");
					view.Materials.Rows.Add("", chamberTakeoff.ComponentMode ? "Slab members" : "Cover slab", chamberTakeoff.CoverConcrete, "m3", "Cover footprint less opening x cover thickness x count");
					view.Materials.Rows.Add("", "RC walls / partitions", chamberTakeoff.WallConcrete, "m3", (chamberTakeoff.Kind == "Lined Canal") ? "Two longitudinal lining walls; no chamber end walls" : "Measured wall/partition concrete less wall openings");
					view.Materials.Rows.Add("", "Additional RC", chamberTakeoff.AdditionalConcrete, "m3", "Entered additional RC per structure x count");
				}
				if (selected == "903(2)")
				{
					view.Materials.Rows.Add("", "Wall forms", chamberTakeoff.WallForms, "m2", "Concrete wall faces");
					view.Materials.Rows.Add("", "Base edges", chamberTakeoff.BaseForms, "m2", "Outside base perimeter x base thickness x count");
					view.Materials.Rows.Add("", "Cover / opening forms", chamberTakeoff.CoverForms, "m2", "Cover soffit and exposed cover/opening edges");
					view.Materials.Rows.Add("", "Additional forms", chamberTakeoff.AdditionalForms, "m2", "Entered additional forms per structure x count");
				}
			}
			finally
			{
				view.Materials.ResumeLayout();
			}
		}

		private void EditChamber(ChamberSheetView view, bool add)
		{
			Guard();
			DataGridViewRow source = (add ? null : ((view.Entries.CurrentRow == null) ? null : (view.Entries.CurrentRow.Tag as DataGridViewRow)));
			if (!add && source == null)
			{
				throw new Exception("Select a structure first.");
			}
			string[] values = ((source == null) ? null : (from c in Enumerable.Range(0, 25)
				select Cell(source, c)).ToArray());
			using (ChamberDimensionsDialog chamberDimensionsDialog = new ChamberDimensionsDialog(view.Kind, values))
			{
				if (chamberDimensionsDialog.ShowDialog(this) != DialogResult.OK)
				{
					return;
				}
				values = chamberDimensionsDialog.Values;
				if (plumbingChambers.Rows.Cast<DataGridViewRow>().Any((DataGridViewRow row) => !row.IsNewRow && row != source && Cell(row, 0).Equals(values[0], StringComparison.OrdinalIgnoreCase)))
				{
					throw new Exception("Structure ID already exists.");
				}
				filling = true;
				try
				{
					string text = ((source == null) ? "" : Cell(source, 0));
					if (source == null)
					{
						source = plumbingChambers.Rows[plumbingChambers.Rows.Add()];
					}
					for (int num = 0; num < 25; num++)
					{
						source.Cells[num].Value = ((num == 21 || num == 23) ? ((object)bool.Parse(values[num])) : values[num]);
					}
					if (text != "" && text != values[0])
					{
						DataGridView[] array = new DataGridView[2] { plumbingMembers, plumbingRebar };
						for (int num2 = 0; num2 < array.Length; num2++)
						{
							foreach (DataGridViewRow item in (IEnumerable)array[num2].Rows)
							{
								if (!item.IsNewRow && Cell(item, 1).Equals(text, StringComparison.OrdinalIgnoreCase))
								{
									item.Cells[1].Value = values[0];
								}
							}
						}
						LengthConfiguration value;
						if (structureConfigurations.TryGetValue(text, out value))
						{
							structureConfigurations.Remove(text);
							structureConfigurations[values[0]] = value;
						}
					}
				}
				finally
				{
					filling = false;
				}
				view.SelectedId = values[0];
				Recalculate();
				RefreshChamberSheet(ActiveLeaf(tabs));
			}
		}

		private void RemoveChamber(ChamberSheetView view)
		{
			Guard();
			DataGridViewRow dataGridViewRow = ((view.Entries.CurrentRow == null) ? null : (view.Entries.CurrentRow.Tag as DataGridViewRow));
			if (dataGridViewRow != null)
			{
				string id = Cell(dataGridViewRow, 0);
				if (plumbingMembers.Rows.Cast<DataGridViewRow>().Any((DataGridViewRow row) => Cell(row, 1) == id))
				{
					throw new Exception("Remove this structure's member rows before deleting the structure.");
				}
				if (plumbingRebar.Rows.Cast<DataGridViewRow>().Any((DataGridViewRow row) => !row.IsNewRow && Cell(row, 1).Equals(id, StringComparison.OrdinalIgnoreCase)))
				{
					throw new Exception("Remove or reassign this structure's rebar rows before removing its dimensions.");
				}
				GridEditHistory.Attach(plumbingChambers).DeleteRows(new[]{dataGridViewRow},()=>structureConfigurations.Remove(id));
			}
		}

		private bool HasMeasuredInputs()
		{
            if(HasExtraInputs())return true;
			if (!(from p in Tables()
				where !new string[4] { "RoofProfiles", "InsulationRolls", "FinishSchedule", "PlumbingLS" }.Contains(p.Key)
				select p).Any((KeyValuePair<string, DataGridView> p) => p.Value != null && p.Value.Rows.Cast<DataGridViewRow>().Any((DataGridViewRow r) => !r.IsNewRow && !Empty(r))) && !beam.WorksHasEntries)
			{
				return slab.WorksHasEntries;
			}
			return true;
		}

		private void WriteBackupBillRoutes(XmlElement root)
		{
			XmlElement xmlElement = root.OwnerDocument.CreateElement("BackupBillRoutes");
			root.AppendChild(xmlElement);
			foreach (KeyValuePair<string, string> pair in backupBillRoutes)
			{
				BackupBillItem backupBillItem = backupBill.FirstOrDefault((BackupBillItem x) => x.Slot == pair.Value);
				if (backupBillItem != null)
				{
					XmlElement xmlElement2 = root.OwnerDocument.CreateElement("Route");
					xmlElement2.SetAttribute("key", pair.Key);
					xmlElement2.SetAttribute("slot", pair.Value);
					xmlElement2.SetAttribute("signature", backupBillItem.Signature);
					xmlElement.AppendChild(xmlElement2);
				}
			}
			WriteBoqIncludes(xmlElement);
		}

		private void ReadBackupBillRoutes(XmlElement root)
		{
			backupBillRoutes.Clear();
			XmlElement xmlElement = root["BackupBillRoutes"];
			ReadBoqIncludes(xmlElement);
			if (xmlElement == null)
			{
				return;
			}
			foreach (XmlElement row in xmlElement.SelectNodes("Route"))
			{
				string slot = row.GetAttribute("slot");
				string attribute = row.GetAttribute("key");
				if (attribute == "" || !backupBill.Any((BackupBillItem x) => x.Slot == slot && x.Signature == row.GetAttribute("signature")) || backupBillRoutes.ContainsKey(attribute))
				{
					throw new Exception("Invalid saved BACKUP quantity route.");
				}
				backupBillRoutes.Add(attribute, slot);
			}
		}

void SetupBackupBillEditing(){boqGrid.ReadOnly=false;foreach(DataGridViewColumn column in boqGrid.Columns)column.ReadOnly=true;boqGrid.Columns[2].ReadOnly=false;boqGrid.CellValidating+=(s,e)=>{if(paintingBackupBill||e.RowIndex<0||e.ColumnIndex!=2)return;object tag=boqGrid.Rows[e.RowIndex].Tag;if(!(tag is BackupBillItem)&&!(tag is DataGridViewRow))return;string text=Convert.ToString(e.FormattedValue).Trim();if(text!=""&&!BackupBill.ReadQuantity(text).HasValue){e.Cancel=false;boqGrid.Rows[e.RowIndex].ErrorText="Invalid quantity is retained and listed for review.";}};boqGrid.CellEndEdit+=(s,e)=>{if(paintingBackupBill||e.RowIndex<0||e.ColumnIndex!=2)return;var row=boqGrid.Rows[e.RowIndex];var item=row.Tag as BackupBillItem;var legacy=row.Tag as DataGridViewRow;if(item!=null){string raw=Convert.ToString(row.Cells[2].Value).Trim();item.ManualQuantity=BackupBill.ReadQuantity(raw);item.ManualInputText=raw!=""&&!item.ManualQuantity.HasValue?raw:null;if(item.ManualInputText==null)UpdateBoundManualRows(item);}else if(legacy!=null){bool before=filling;filling=true;try{legacy.Cells[3].Value=Convert.ToString(row.Cells[2].Value);}finally{filling=before;}CommitManualEntryEdit(legacy);Recalculate();}else return;row.ErrorText=item!=null&&item.ManualInputText!=null?"Invalid manual quantity; included in issue list":"";dashboardDirty=reportPreviewDirty=true;ScheduleWorksExcel();};}



		private List<BillQuantity> ManualBoqQuantities()
		{
			return ManualEntryQuantities.Group(manualPlumbingLs);
		}


		private void ApplyBackupWrite()
		{
			if (worksExcel.LastBackupRoutes != null)
			{
				backupBillRoutes.Clear();
				foreach (KeyValuePair<string, BackupTarget> lastBackupRoute in worksExcel.LastBackupRoutes)
				{
					backupBillRoutes[lastBackupRoute.Key] = lastBackupRoute.Value.SlotId;
				}
			}
			foreach (BackupBillItem item in backupBill)
			{
				double value;
				if (worksExcel.LastBackupTotals.TryGetValue(item.Slot, out value))
				{
					item.WorkbookQuantity = value;
				}
			}
			dashboardDirty = true;
			RefreshVisibleWorksViews();
		}

		private void AppendBackupBillReport(StringBuilder b)
		{
			if (backupBill.Count > 0)
			{
				b.AppendLine();
				b.AppendLine("BILL OF QUANTITIES | BACKUP PAY ITEMS");
				List<BackupDemand> source = (from d in BackupQuantities(false)
					where d.Quantity > 1E-10 || d.ManualOverride || d.Sources.Any((BillQuantity x) => x.Quantity > 1E-10) || backupBillRoutes.ContainsKey(d.Key)
					select d).ToList();
				List<BackupTarget> targets = backupBill.Select((BackupBillItem i) => i.Target).ToList();
				foreach (BackupBillItem item in backupBill)
				{
					List<BackupDemand> list = (from d in source.Where(delegate(BackupDemand d)
						{
							string value;
							if (backupBillRoutes.TryGetValue(d.Key, out value))
							{
								return value == item.Slot;
							}
							BackupTarget backupTarget = BackupMapping.Suggest(d, targets);
							return backupTarget != null && backupTarget.SlotId == item.Slot;
						})
						where BackupMapping.Unit(d.Unit) == BackupMapping.Unit(item.Unit)
						select d).ToList();
					double? num = item.ManualQuantity ?? (((BackupMapping.Unit(item.Unit) != "ls" && list.Count > 0) || list.Any((BackupDemand d) => d.ManualPriority >= 1)) ? new double?(BackupMapping.FinalQuantity(list, item.Unit)) : item.WorkbookFallback) /* only Progress Photographs from BACKUP (23 November 2026) */;
					b.AppendLine(item.Code + " | " + PayItemText.Display(item.Description) + " | " + (num.HasValue ? num.Value.ToString("0.########", CultureInfo.InvariantCulture) : "not entered") + " " + item.Unit);
				}
			}
			foreach (BillQuantity item2 in ManualBoqQuantities())
			{
				b.AppendLine("MANUAL BOQ | " + item2.Description + " | " + item2.Quantity.ToString("0.########", CultureInfo.InvariantCulture) + " " + item2.Unit);
			}
		}


		private void AddRCCoverDefaults(FlowLayoutPanel start)
		{
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Width = 1100,
				Padding = new Padding(6),
				WrapContents = true
			};
			string[] array = new string[5] { "Beam", "Slab", "Footing", "Canopy", "RCWall" };
			decimal[] array2 = new decimal[5] { 40m, 20m, 75m, 25m, 40m };
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i];
				FlowLayoutPanel flowLayoutPanel2 = new FlowLayoutPanel
				{
					Width = 185,
					Height = 62,
					FlowDirection = FlowDirection.TopDown,
					Margin = new Padding(3)
				};
				flowLayoutPanel2.Controls.Add(new Label
				{
					Text = ((text == "RCWall") ? "RC wall" : text) + " cover (mm)",
					Width = 180,
					Height = 24
				});
				NumericUpDown value = new NumericUpDown
				{
					Minimum = 0m,
					Maximum = ((text == "Slab") ? 100 : ((text == "Beam") ? 200 : 500)),
					DecimalPlaces = 1,
					Value = array2[i],
					Width = 165
				};
				rcCovers.Add(text, value);
				flowLayoutPanel2.Controls.Add(value);
				flowLayoutPanel.Controls.Add(flowLayoutPanel2);
			}
			start.Controls.Add(flowLayoutPanel);
			FlowLayoutPanel flowLayoutPanel3 = new FlowLayoutPanel
			{
				AutoSize = true,
				Width = 1100,
				Padding = new Padding(4)
			};
			Button(flowLayoutPanel3, "Save Defaults", SaveRCDefaults);
			Button(flowLayoutPanel3, "Apply covers to existing entries", ApplyRCCovers);
			rcDefaultsStatus = new Label
			{
				AutoSize = true,
				MaximumSize = new Size(1000, 0),
				Text = "Cover defaults are saved in Documents. Existing entries change only when Apply is selected.",
				Padding = new Padding(8)
			};
			flowLayoutPanel3.Controls.Add(rcDefaultsStatus);
			start.Controls.Add(flowLayoutPanel3);
			start.Controls.SetChildIndex(flowLayoutPanel, 2);
			start.Controls.SetChildIndex(flowLayoutPanel3, 3);
			canopies.Columns[6].Visible = false;
			rcWalls.Columns[6].Visible = false;
			footings.DefaultValuesNeeded += delegate(object s, DataGridViewRowEventArgs e)
			{
				e.Row.Cells[7].Value = rcCovers["Footing"].Value;
			};
			canopies.DefaultValuesNeeded += delegate(object s, DataGridViewRowEventArgs e)
			{
				e.Row.Cells[6].Value = rcCovers["Canopy"].Value;
			};
			rcWalls.DefaultValuesNeeded += delegate(object s, DataGridViewRowEventArgs e)
			{
				e.Row.Cells[6].Value = rcCovers["RCWall"].Value;
			};
			var array3 = new[]
			{
				new
				{
					Grid = footings,
					Column = 7,
					Kind = "Footing"
				},
				new
				{
					Grid = canopies,
					Column = 6,
					Kind = "Canopy"
				},
				new
				{
					Grid = rcWalls,
					Column = 6,
					Kind = "RCWall"
				}
			};
			foreach (var anon in array3)
			{
				var p = anon;
				p.Grid.RowsAdded += delegate(object s, DataGridViewRowsAddedEventArgs e)
				{
					if (!filling)
					{
						for (int j = e.RowIndex; j < e.RowIndex + e.RowCount; j++)
						{
							if (!p.Grid.Rows[j].IsNewRow)
							{
								p.Grid.Rows[j].Cells[p.Column].Value = rcCovers[p.Kind].Value;
							}
						}
					}
				};
			}
			if (!File.Exists(RCDefaultStore.PathName))
			{
				return;
			}
			try
			{
				XmlElement documentElement = RCDefaultStore.Read(RCDefaultStore.PathName).DocumentElement;
				ReadRCCovers(documentElement);
				ReadCanopySettings(documentElement);
				ReadRCWallSettings(documentElement);
				var array4 = new[]
				{
					new
					{
						Box = footingFc,
						Key = "footingFcDefault"
					},
					new
					{
						Box = footingSmall,
						Key = "footingSmallDefault"
					},
					new
					{
						Box = footingLarge,
						Key = "footingLargeDefault"
					}
				};
				foreach (var anon2 in array4)
				{
					if (documentElement.HasAttribute(anon2.Key) && anon2.Box.Items.Contains(documentElement.GetAttribute(anon2.Key)))
					{
						anon2.Box.SelectedItem = documentElement.GetAttribute(anon2.Key);
					}
				}
				footingStock = CommercialBars.ParseLengths(documentElement.GetAttribute("footingStock"));
				beam.ReadRCDefaults(documentElement["BeamDefaults"]);
				slab.ReadRCDefaults(documentElement["SlabDefaults"]);
				beam.WorksCoverMm = rcCovers["Beam"].Value;
				slab.WorksCoverMm = rcCovers["Slab"].Value;
				rcDefaultsStatus.Text = "Saved RC defaults loaded from Documents.";
			}
			catch (Exception ex)
			{
				rcDefaultsStatus.Text = "Saved defaults need review: " + ex.Message;
			}
		}

		private void WriteRCCovers(XmlElement root)
		{
			foreach (KeyValuePair<string, NumericUpDown> rcCover in rcCovers)
			{
				root.SetAttribute("cover" + rcCover.Key, rcCover.Value.Value.ToString(CultureInfo.InvariantCulture));
			}
		}

		private void ReadRCCovers(XmlElement root)
		{
			if (root.Name == "ProgramOfWorks")
			{
				if (!root.HasAttribute("coverBeam"))
				{
					rcCovers["Beam"].Value = beam.WorksCoverMm;
				}
				if (!root.HasAttribute("coverSlab"))
				{
					rcCovers["Slab"].Value = slab.WorksCoverMm;
				}
			}
			foreach (KeyValuePair<string, NumericUpDown> rcCover in rcCovers)
			{
				if (root.HasAttribute("cover" + rcCover.Key))
				{
					decimal num = decimal.Parse(root.GetAttribute("cover" + rcCover.Key), CultureInfo.InvariantCulture);
					if (num < rcCover.Value.Minimum || num > rcCover.Value.Maximum)
					{
						throw new Exception("Invalid saved concrete cover.");
					}
					rcCover.Value.Value = num;
				}
			}
		}

		private void SaveRCDefaults()
		{
			Commit();
			XmlDocument xmlDocument = new XmlDocument();
			XmlElement xmlElement = xmlDocument.CreateElement("ReinforcedConcreteDefaults");
			xmlDocument.AppendChild(xmlElement);
			xmlElement.SetAttribute("version", "1");
			WriteExtraSettings(xmlElement);
            WriteRCCovers(xmlElement);
			WriteCanopySettings(xmlElement);
			WriteRCWallSettings(xmlElement);
			xmlElement.SetAttribute("footingFcDefault", footingFc.Text);
			xmlElement.SetAttribute("footingSmallDefault", footingSmall.Text);
			xmlElement.SetAttribute("footingLargeDefault", footingLarge.Text);
			xmlElement.SetAttribute("footingStock", CommercialBars.FormatLengths(footingStock));
			beam.WriteRCDefaults(xmlElement);
			slab.WriteRCDefaults(xmlElement);
			RCDefaultStore.Save(RCDefaultStore.PathName, xmlDocument);
			if (!beam.WorksHasEntries)
			{
				beam.WorksCoverMm = rcCovers["Beam"].Value;
			}
			if (!slab.WorksHasEntries)
			{
				slab.WorksCoverMm = rcCovers["Slab"].Value;
			}
			rcDefaultsStatus.Text = "RC defaults saved in Documents; future app updates reuse this file.";
		}

		private void ApplyRCCovers()
		{
			Commit();
			filling = true;
			try
			{
				beam.WorksCoverMm = rcCovers["Beam"].Value;
				slab.WorksCoverMm = rcCovers["Slab"].Value;
				var array = new[]
				{
					new
					{
						Grid = footings,
						Column = 7,
						Kind = "Footing"
					},
					new
					{
						Grid = canopies,
						Column = 6,
						Kind = "Canopy"
					},
					new
					{
						Grid = rcWalls,
						Column = 6,
						Kind = "RCWall"
					}
				};
				foreach (var anon in array)
				{
					foreach (DataGridViewRow item in (IEnumerable)anon.Grid.Rows)
					{
						if (!item.IsNewRow)
						{
							item.Cells[anon.Column].Value = rcCovers[anon.Kind].Value;
						}
					}
				}
			}
			finally
			{
				filling = false;
			}
			Recalculate();
		}

		private void AddPipeGroups()
		{
			TabPage tabPage = tabs.TabPages.Cast<TabPage>().Single((TabPage p) => p.Text == "Pipe Takeoff");
			tabPage.Controls.Remove(plumbingPipes);
			pipeGroupTabs = new NavigationTabs
			{
				Dock = DockStyle.Fill
			};
			string[] names = PipeGroups.Names;
			foreach (string text in names)
			{
				pipeGroupTabs.TabPages.Add(new TabPage(text));
			}
			tabPage.Controls.Add(pipeGroupTabs);
			tabPage.Controls.SetChildIndex(pipeGroupTabs, 0);
            var pipeHeading=tabPage.Controls.OfType<WorksheetHeading>().Single();tabPage.Controls.Remove(pipeHeading);foreach(TabPage child in pipeGroupTabs.TabPages){var heading=new WorksheetHeading{Text="Pipe Takeoff",Subtitle=pipeHeading.Subtitle};heading.SetGuide(pipeHeading.GuideText);child.Controls.Add(heading);}pipeHeading.Dispose();
			plumbingPipes.ColumnHeadersVisible = true;
			pipeGroupTabs.Deselecting += delegate(object s, TabControlCancelEventArgs e)
			{
				if (!plumbingPipes.EndEdit())
				{
					e.Cancel = true;
				}
			};
			pipeGroupTabs.SelectedIndexChanged += delegate
			{
				ShowPipeGroup();
			};
			plumbingPipes.DefaultValuesNeeded += delegate(object s, DataGridViewRowEventArgs e)
			{
				if (pipeGroupTabs.SelectedTab != null)
				{
					/* no size is put in a new row (owner, 23 November 2026: "sa pipe take off size huwag muna maglagay ng size"); the
					   size is chosen in the Size column */
				}
			};
			InstallPipeSizeColumn();
			plumbingPipes.EditingControlShowing += delegate(object s, DataGridViewEditingControlShowingEventArgs e)
			{
				TextBox textBox = e.Control as TextBox;
				if (textBox != null && plumbingPipes.CurrentCell != null && plumbingPipes.CurrentCell.ColumnIndex == 2 && pipeGroupTabs.SelectedTab != null)
				{
					AutoCompleteStringCollection autoCompleteStringCollection = new AutoCompleteStringCollection();
					autoCompleteStringCollection.AddRange(PipeGroups.Choices(pipeGroupTabs.SelectedTab.Text));
					textBox.AutoCompleteCustomSource = autoCompleteStringCollection;
					textBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
					textBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
				}
			};
			ShowPipeGroup();
		}

		private void ShowPipeGroup()
		{
			if (changingPipeGroup || pipeGroupTabs == null || pipeGroupTabs.SelectedTab == null)
			{
				return;
			}
			changingPipeGroup = true;
			plumbingPipes.SuspendLayout();
			try
			{
				plumbingPipes.EndEdit();
				plumbingPipes.CurrentCell = null;
				plumbingPipes.ClearSelection();
				pipeGroupTabs.SelectedTab.Controls.Add(plumbingPipes);
                pipeGroupTabs.SelectedTab.Controls.SetChildIndex(plumbingPipes,0);
				foreach (DataGridViewRow item in (IEnumerable)plumbingPipes.Rows)
				{
					if (!item.IsNewRow)
					{
						item.Visible = Empty(item) || Cell(item, 2) == "" /* no size yet: shown until one is chosen */ || PipeGroups.Group(Cell(item, 2)) == pipeGroupTabs.SelectedTab.Text;
					}
				}
				SyncPipeSizeColumn();
			}
			finally
			{
				plumbingPipes.ResumeLayout();
				changingPipeGroup = false;
			}
		}

		private void ConfigureManualEntries(DataGridView grid)
		{
			grid.Columns.Add("ManualUnit", "Unit");
			grid.Columns.Add("BackupSlot", "BACKUP item");
			grid.Columns.Add("BackupSignature", "BACKUP signature");
			DataGridViewColumn dataGridViewColumn = grid.Columns[0];
			DataGridViewColumn dataGridViewColumn2 = grid.Columns[1];
			DataGridViewColumn dataGridViewColumn3 = grid.Columns[6];
			bool flag = (grid.Columns[7].Visible = false);
			bool flag3 = (dataGridViewColumn3.Visible = flag);
			bool visible = (dataGridViewColumn2.Visible = flag3);
			dataGridViewColumn.Visible = visible;
			grid.Columns[2].HeaderText = "Pay Item Description";
			grid.Columns[3].HeaderText = "Quantity";
			grid.Columns[5].ReadOnly = false;
			DataGridViewColumn dataGridViewColumn4 = grid.Columns[6];
			visible = (grid.Columns[7].ReadOnly = true);
			dataGridViewColumn4.ReadOnly = visible;
			grid.Columns[2].DisplayIndex = 0;
			grid.Columns[3].DisplayIndex = 1;
			grid.Columns[5].DisplayIndex = 2;
			grid.Columns[4].DisplayIndex = 3;
			grid.Columns[2].Width = 720;
			grid.Columns[3].Width = 130;
			grid.Columns[5].Width = 100;
			grid.Columns[4].Width = 360;
			grid.Columns[5].DefaultCellStyle.BackColor = ModernUi.Input;
			grid.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
			grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
			grid.Columns[2].FillWeight = 65f;
			grid.Columns[3].FillWeight = 10f;
			grid.Columns[5].FillWeight = 8f;
			grid.Columns[4].FillWeight = 25f;
			Action<DataGridViewRow> defaults = delegate(DataGridViewRow row)
			{
				if (Cell(row, 0) == "")
				{
					row.Cells[0].Value = Guid.NewGuid().ToString("N");
				}
				if (Cell(row, 1) == "")
				{
					row.Cells[1].Value = "Manual BOQ";
				}
				if (Cell(row, 5) == "")
				{
					row.Cells[5].Value = "ls";
				}
			};
			grid.DefaultValuesNeeded += delegate(object s, DataGridViewRowEventArgs e)
			{
				defaults(e.Row);
			};
			grid.RowsAdded += delegate(object s, DataGridViewRowsAddedEventArgs e)
			{
				if (!filling)
				{
					for (int i = e.RowIndex; i < e.RowIndex + e.RowCount; i++)
					{
						if (!grid.Rows[i].IsNewRow)
						{
							defaults(grid.Rows[i]);
						}
					}
				}
			};
			grid.EditingControlShowing += delegate(object s, DataGridViewEditingControlShowingEventArgs e)
			{
				TextBox textBox = e.Control as TextBox;
				if (textBox != null && grid.CurrentCell != null)
				{
					int columnIndex = grid.CurrentCell.ColumnIndex;
					AutoCompleteStringCollection autoCompleteStringCollection = new AutoCompleteStringCollection();
					switch (columnIndex)
					{
					case 2:
						autoCompleteStringCollection.AddRange((backupBill.Count > 0) ? backupBill.Select((BackupBillItem b) => b.Description).Distinct().ToArray() : PlumbingSpecs.LumpSums);
						break;
					case 5:
						autoCompleteStringCollection.AddRange(new string[8] { "ls", "m", "sq.m.", "cu.m.", "kg", "pcs", "sets", "each" });
						break;
					default:
						textBox.AutoCompleteMode = AutoCompleteMode.None;
						return;
					}
					textBox.AutoCompleteCustomSource = autoCompleteStringCollection;
					textBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
					textBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
				}
			};
		}

		private BackupBillItem BoundManualItem(DataGridViewRow row)
		{
			string slot = Cell(row, 6);
			if (slot == "")
			{
				return null;
			}
			BackupBillItem backupBillItem = backupBill.FirstOrDefault((BackupBillItem b) => b.Slot == slot && b.Signature == Cell(row, 7));
			if (backupBillItem == null)
			{
				throw new Exception("The selected BACKUP pay item changed or is unavailable; select it again.");
			}
			return backupBillItem;
		}

		private void UpdateBoundManualRows(BackupBillItem item)
		{
			if (plumbingLs == null)
			{
				return;
			}
			bool flag = filling;
			filling = true;
			try
			{
				foreach (DataGridViewRow item2 in (IEnumerable)plumbingLs.Rows)
				{
					if (!item2.IsNewRow && Cell(item2, 6) == item.Slot)
					{
						item2.Cells[3].Value = (item.ManualQuantity.HasValue ? item.ManualQuantity.Value.ToString("R", CultureInfo.InvariantCulture) : "");
					}
				}
			}
			finally
			{
				filling = flag;
			}
		}

		private void ReleaseManualBinding(DataGridViewRow row)
		{
			BackupBillItem backupBillItem = BoundManualItem(row);
			if (backupBillItem != null)
			{
				backupBillItem.ManualQuantity = null;
			}
		}

		private void BindManualItem(DataGridViewRow row, BackupBillItem item)
		{
			if (plumbingLs.Rows.Cast<DataGridViewRow>().Any((DataGridViewRow r) => !r.IsNewRow && r != row && Cell(r, 6) == item.Slot))
			{
				throw new Exception("This BACKUP item already has a manual entry. Edit that row's quantity.");
			}
			bool flag = filling;
			filling = true;
			try
			{
				row.Cells[2].Value = item.Description;
				row.Cells[5].Value = item.Unit;
				row.Cells[6].Value = item.Slot;
				row.Cells[7].Value = item.Signature;
				DataGridViewCell dataGridViewCell = row.Cells[2];
				bool readOnly = (row.Cells[5].ReadOnly = true);
				dataGridViewCell.ReadOnly = readOnly;
			}
			finally
			{
				filling = flag;
			}
		}

		private void ChooseManualBackupItem()
		{
			Guard();
			if (backupBill.Count == 0)
			{
				throw new Exception("Link or reopen the BACKUP workbook first so its pay items and units are available.");
			}
			using (Form form = new Form
			{
				Text = "Select BACKUP pay item",
				ClientSize = new Size(1000, 160),
				StartPosition = FormStartPosition.CenterParent,
				Font = new Font("Segoe UI", 10f)
			})
			{
				ComboBox comboBox = new ComboBox
				{
					Left = 15,
					Top = 20,
					Width = 970,
					DropDownWidth = 1400,
					DropDownStyle = ComboBoxStyle.DropDownList
				};
				comboBox.Items.AddRange(backupBill.Select((BackupBillItem b) => b.Code + " | " + PayItemText.Display(b.Description) + " | " + b.Unit + " | BC" + b.Row).Cast<object>().ToArray());
				comboBox.SelectedIndex = 0;
				Button button = new Button
				{
					Text = "Use selected pay item",
					Left = 735,
					Top = 85,
					Width = 250,
					Height = 34,
					DialogResult = DialogResult.OK
				};
				form.Controls.Add(comboBox);
				form.Controls.Add(button);
				form.AcceptButton = button;
				if (form.ShowDialog(this) != DialogResult.OK)
				{
					return;
				}
				BackupBillItem item = backupBill[comboBox.SelectedIndex];
				DataGridViewRow dataGridViewRow = plumbingLs.Rows.Cast<DataGridViewRow>().FirstOrDefault((DataGridViewRow r) => !r.IsNewRow && Cell(r, 6) == item.Slot);
				if (dataGridViewRow != null)
				{
					BindManualItem(dataGridViewRow, item);
					CommitManualEntryEdit(dataGridViewRow);
					Recalculate();
					plumbingLs.CurrentCell = dataGridViewRow.Cells[3];
					return;
				}
				bool flag = filling;
				filling = true;
				DataGridViewRow dataGridViewRow2;
				try
				{
					int index = plumbingLs.Rows.Add(Guid.NewGuid().ToString("N"), "Manual BOQ", item.Description, item.ManualQuantity.HasValue ? item.ManualQuantity.Value.ToString("R", CultureInfo.InvariantCulture) : "", "", item.Unit, item.Slot, item.Signature);
					dataGridViewRow2 = plumbingLs.Rows[index];
				}
				finally
				{
					filling = flag;
				}
				BindManualItem(dataGridViewRow2, item);
				plumbingLs.CurrentCell = dataGridViewRow2.Cells[3];
			}
		}

		private void CommitManualEntryEdit(DataGridViewRow row)
		{
			try
			{
				if (Cell(row, 3) == "")
				{
					BackupBillItem backupBillItem = BoundManualItem(row);
					if (backupBillItem != null)
					{
						backupBillItem.ManualQuantity = null;
					}
					return;
				}
				double? manualQuantity = BackupBill.ReadQuantity(Cell(row, 3));
				if (!manualQuantity.HasValue)
				{
					return;
				}
				BackupBillItem backupBillItem2 = BoundManualItem(row);
				if (backupBillItem2 == null)
				{
					string description = FinishTypeRegistry.Key(Cell(row, 2));
					string unit = BackupMapping.Unit(Cell(row, 5));
					List<BackupBillItem> list = backupBill.Where((BackupBillItem b) => FinishTypeRegistry.Key(b.Description) == description && BackupMapping.Unit(b.Unit) == unit).ToList();
					if (list.Count == 1)
					{
						backupBillItem2 = list[0];
						BindManualItem(row, backupBillItem2);
					}
				}
				if (backupBillItem2 != null)
				{
					backupBillItem2.ManualQuantity = manualQuantity;
				}
			}
			catch (Exception ex)
			{
				row.Cells[4].Value = ex.Message;
			}
		}

		private void ReadManualBoqEntries()
		{
			manualPlumbingLs.Clear();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (DataGridViewRow row in (IEnumerable)plumbingLs.Rows)
			{
				if (row.IsNewRow)
				{
					continue;
				}
				row.Cells[4].Value = "";
				if (Cell(row, 2) == "" && Cell(row, 3) == "")
				{
					continue;
				}
				if (Cell(row, 3) == "")
				{
					row.Cells[4].Value = "Enter quantity to sync";
					continue;
				}
				try
				{
					if (Cell(row, 2) == "" || Cell(row, 5) == "")
					{
						throw new Exception("Enter pay-item description, quantity and unit.");
					}
					if (Cell(row, 0) == "")
					{
						row.Cells[0].Value = Guid.NewGuid().ToString("N");
					}
					if (!hashSet.Add(Cell(row, 0)))
					{
						throw new Exception("Duplicate manual entry ID.");
					}
					double? manualQuantity = BackupBill.ReadQuantity(Cell(row, 3));
					if (!manualQuantity.HasValue)
					{
						throw new Exception("Enter a finite nonnegative quantity.");
					}
					BackupBillItem backupBillItem = BoundManualItem(row);
					if (backupBillItem == null)
					{
						List<BackupBillItem> list = backupBill.Where((BackupBillItem b) => FinishTypeRegistry.Key(b.Description) == FinishTypeRegistry.Key(Cell(row, 2)) && BackupMapping.Unit(b.Unit) == BackupMapping.Unit(Cell(row, 5))).ToList();
						if (list.Count == 1)
						{
							backupBillItem = list[0];
							BindManualItem(row, backupBillItem);
							backupBillItem.ManualQuantity = manualQuantity;
						}
					}
					if (backupBillItem != null)
					{
						if (backupBillItem.ManualQuantity.HasValue)
						{
							row.Cells[3].Value = backupBillItem.ManualQuantity.Value;
						}
						DataGridViewCell dataGridViewCell = row.Cells[2];
						bool readOnly = (row.Cells[5].ReadOnly = true);
						dataGridViewCell.ReadOnly = readOnly;
						row.Cells[4].Value = "Linked " + backupBillItem.Code + " | BC" + backupBillItem.Row;
					}
					else
					{
						BillQuantity billQuantity = new BillQuantity
						{
							Section = "Bill of Quantities",
							Description = Cell(row, 2),
							Quantity = manualQuantity.Value,
							Unit = BackupMapping.Unit(Cell(row, 5))
						};
						billQuantity.Sources.Add(new BillQuantity
						{
							Description = Cell(row, 0) + ((Cell(row, 1) == "") ? "" : (" | " + Cell(row, 1))),
							Quantity = manualQuantity.Value,
							Unit = billQuantity.Unit,
							Computation = "Quantity entered manually in Bill of Quantities"
						});
						manualPlumbingLs.Add(billQuantity);
						row.Cells[4].Value = ((backupBill.Count > 0) ? "Use Select BACKUP pay item or Map BACKUP items" : "Ready; link BACKUP to sync");
					}
				}
				catch (Exception ex)
				{
					row.Cells[4].Value = ex.Message;
					errors.Add("BOQ manual entry: " + ex.Message);
				}
			}
		}

		private List<StructureProfile> SavedStructureProfiles()
		{
			if (!File.Exists(StructureProfileLibrary.DefaultPath))
			{
				return new List<StructureProfile>();
			}
			return StructureProfileLibrary.Load(StructureProfileLibrary.DefaultPath);
		}

		private StructureProfile CurrentStructureProfile(ChamberSheetView view, string name)
		{
			Commit();
			Recalculate();
			DataGridViewRow source = ((view.Entries.CurrentRow == null) ? null : (view.Entries.CurrentRow.Tag as DataGridViewRow));
			if (source == null)
			{
				throw new Exception("Select a completed structure first.");
			}
			string id = Cell(source, 0);
			if (structureMemberErrors.Contains(id) || plumbingRebar.Rows.Cast<DataGridViewRow>().Any((DataGridViewRow r) => !r.IsNewRow && Cell(r, 1) == id && Cell(r, 9) != "Estimated"))
			{
				throw new Exception("Complete this structure's member/rebar inputs before saving its profile.");
			}
			StructureProfile structureProfile = new StructureProfile
			{
				Name = name,
				Kind = view.Kind,
				Geometry = (from c in Enumerable.Range(0, 25)
					select Cell(source, c)).ToArray(),
				Configuration = CalculationStore.Clone(StructureConfiguration(id))
			};
			if (view.Kind == "Septic Vault, Concrete" || view.Kind == "Septic Vault, CHB")
			{
				structureProfile.Geometry[24] = "0";
			}
			foreach (DataGridViewRow row in (IEnumerable)plumbingMembers.Rows)
			{
				if (Cell(row, 1) == id)
				{
					structureProfile.Members.Add((from c in Enumerable.Range(0, 24)
						select Cell(row, c)).ToArray());
				}
			}
			foreach (DataGridViewRow row2 in (IEnumerable)plumbingRebar.Rows)
			{
				if (!row2.IsNewRow && Cell(row2, 1) == id)
				{
					structureProfile.ExtraRebar.Add((from c in Enumerable.Range(0, 7)
						select Cell(row2, c)).ToArray());
				}
			}
			StructureProfileLibrary.Validate(structureProfile);
			return structureProfile;
		}

		private void SaveStructureProfile(ChamberSheetView view)
		{
			Guard();
			DataGridViewRow currentRow = view.Entries.CurrentRow;
			if (currentRow == null)
			{
				throw new Exception("Select the completed structure to save.");
			}
			using (Form form = new Form
			{
				Text = "Save structure profile",
				ClientSize = new Size(640, 170),
				StartPosition = FormStartPosition.CenterParent,
				Font = new Font("Segoe UI", 10f)
			})
			{
				form.Controls.Add(new Label
				{
					Text = "Profile name (same type/name updates its saved template)",
					Left = 16,
					Top = 15,
					Width = 600,
					Height = 25
				});
				TextBox textBox = new TextBox
				{
					Text = Convert.ToString(currentRow.Cells[1].Value),
					Left = 16,
					Top = 48,
					Width = 600,
					MaxLength = 160
				};
				Button button = new Button
				{
					Text = "Save Profile",
					Left = 440,
					Top = 105,
					Width = 175,
					Height = 34,
					DialogResult = DialogResult.OK
				};
				form.Controls.Add(textBox);
				form.Controls.Add(button);
				form.AcceptButton = button;
				if (form.ShowDialog(this) == DialogResult.OK)
				{
					StructureProfile structureProfile = CurrentStructureProfile(view, textBox.Text.Trim());
					StructureProfileLibrary.Save(StructureProfileLibrary.DefaultPath, StructureProfileLibrary.Merge(SavedStructureProfiles(), new StructureProfile[1] { structureProfile }));
					view.Dimensions.Text = "Profile saved in Documents > DTCE Tool Kit > Profiles. Load Profile reuses it after app updates; Export Profiles shares its inputs and calculation settings.";
				}
			}
		}

		private void LoadStructureProfile(ChamberSheetView view)
		{
			Guard();
			List<StructureProfile> list = (from p in SavedStructureProfiles()
				where p.Kind == view.Kind
				orderby p.Name
				select p).ToList();
			if (list.Count == 0)
			{
				throw new Exception("No saved profile for this structure type. Save one or use Import Profiles.");
			}
			using (Form form = new Form
			{
				Text = "Load structure profile",
				ClientSize = new Size(850, 175),
				StartPosition = FormStartPosition.CenterParent,
				Font = new Font("Segoe UI", 10f)
			})
			{
				ComboBox comboBox = new ComboBox
				{
					DropDownStyle = ComboBoxStyle.DropDownList,
					Left = 15,
					Top = 25,
					Width = 820
				};
				comboBox.Items.AddRange(list.Select((StructureProfile p) => p.Name + " | " + p.Members.Count + " member row(s)").Cast<object>().ToArray());
				comboBox.SelectedIndex = 0;
				Button button = new Button
				{
					Text = "Review and add structure",
					Left = 580,
					Top = 100,
					Width = 255,
					Height = 34,
					DialogResult = DialogResult.OK
				};
				form.Controls.Add(comboBox);
				form.Controls.Add(button);
				form.AcceptButton = button;
				if (form.ShowDialog(this) != DialogResult.OK)
				{
					return;
				}
				StructureProfile structureProfile = list[comboBox.SelectedIndex];
				string[] geometry = (string[])structureProfile.Geometry.Clone();
				geometry[0] = ((view.Kind == "Lined Canal") ? "LC-" : ((view.Kind == "Catch Basin (CHB)") ? "CB-" : "SV-")) + Guid.NewGuid().ToString("N").Substring(0, 6)
					.ToUpperInvariant();
				using (ChamberDimensionsDialog chamberDimensionsDialog = new ChamberDimensionsDialog(view.Kind, geometry))
				{
					if (chamberDimensionsDialog.ShowDialog(this) != DialogResult.OK)
					{
						return;
					}
					geometry = chamberDimensionsDialog.Values;
					if (plumbingChambers.Rows.Cast<DataGridViewRow>().Any((DataGridViewRow r) => !r.IsNewRow && Cell(r, 0).Equals(geometry[0], StringComparison.OrdinalIgnoreCase)))
					{
						throw new Exception("Structure ID already exists.");
					}
					bool flag = filling;
					filling = true;
					try
					{
						DataGridViewRow dataGridViewRow = plumbingChambers.Rows[plumbingChambers.Rows.Add()];
						for (int num = 0; num < 25; num++)
						{
							dataGridViewRow.Cells[num].Value = ((num == 21 || num == 23) ? ((object)bool.Parse(geometry[num])) : geometry[num]);
						}
						structureConfigurations[geometry[0]] = CalculationStore.Clone(structureProfile.Configuration);
						foreach (string[] member in structureProfile.Members)
						{
							string[] array = (string[])member.Clone();
							array[0] = Guid.NewGuid().ToString("N");
							array[1] = geometry[0];
							plumbingMembers.Rows.Add(array.Cast<object>().ToArray());
						}
						foreach (string[] item in structureProfile.ExtraRebar)
						{
							string[] array2 = (string[])item.Clone();
							array2[0] = Guid.NewGuid().ToString("N");
							array2[1] = geometry[0];
							plumbingRebar.Rows.Add(array2.Cast<object>().ToArray());
						}
					}
					finally
					{
						filling = flag;
					}
					view.SelectedId = geometry[0];
					Recalculate();
					RefreshChamberSheet(ActiveLeaf(tabs));
				}
			}
		}

		private void ExportStructureProfiles()
		{
			Guard();
			List<StructureProfile> list = SavedStructureProfiles();
			if (list.Count == 0)
			{
				throw new Exception("Save at least one completed structure profile first.");
			}
			using (SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				Filter = "DTCE structure profile library|*.structureprofiles.xml",
				FileName = "DTCE-Structure-Profiles.structureprofiles.xml"
			})
			{
				if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
				{
					StructureProfileLibrary.Save(saveFileDialog.FileName, list);
				}
			}
		}

		private void ImportStructureProfiles()
		{
			Guard();
			using (OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = "DTCE structure profile library|*.structureprofiles.xml;*.xml",
				CheckFileExists = true
			})
			{
				if (openFileDialog.ShowDialog(this) == DialogResult.OK)
				{
					List<StructureProfile> incoming = StructureProfileLibrary.Load(openFileDialog.FileName);
                    projectSourceFiles.CaptureFile(openFileDialog.FileName,"Structure profile library");
					StructureProfileLibrary.Save(StructureProfileLibrary.DefaultPath, StructureProfileLibrary.Merge(SavedStructureProfiles(), incoming));
					MessageBox.Show(this, "Profiles imported and saved in Documents. Select the structure type, then Load Profile to add one. App updates reuse this library.", "Structure profiles");
				}
			}
		}

		private LengthConfiguration StructureConfiguration(string id)
		{
			LengthConfiguration value;
			if (!structureConfigurations.TryGetValue(id, out value))
			{
				value = CalculationStore.Resolve(21.0, 40, 275.0, 40, 275.0);
				structureConfigurations[id] = value;
			}
			return value;
		}

		private void AddStructureMemberInputs()
		{
			bool flag = filling;
			filling = true;
			try
			{
				plumbingMembers = Grid(new string[33]
				{
					"Member ID", "Structure ID", "Member type", "Description", "Length m", "Width / height m", "Thickness mm", "Members / structure", "Cover mm", "Rebar layers",
					"X / vertical dia mm", "X / vertical spacing mm", "X / vertical count / layer", "Y / horizontal dia mm", "Y / horizontal spacing mm", "Y / horizontal count / layer", "X / vertical extra ends mm", "Y / horizontal extra ends mm", "Formed main faces", "Formed edge length m (blank=full)",
					"Gravel thickness mm", "Plaster faces", "Waterproof faces", "Commercial length m", "Concrete m3", "Forms m2", "Net rebar kg", "Rebar TO BUY kg", "CHB m2", "Gravel m3",
					"Plaster m2", "Waterproof m2", "Status"
				}, 24);
				plumbingMembers.AllowUserToAddRows = false;
				plumbingMembers.AllowUserToDeleteRows = false;
				DataGridViewColumn dataGridViewColumn = plumbingMembers.Columns[0];
				bool visible = (plumbingMembers.Columns[1].Visible = false);
				dataGridViewColumn.Visible = visible;
				for (int i = 8; i < 24; i++)
				{
					plumbingMembers.Columns[i].Visible = false;
				}
				AddCombo(plumbingMembers, 2, "Member type", StructureMemberInput.Types);
				AddCombo(plumbingMembers, 9, "Rebar layers", new string[2] { "1", "2" });
				AddCombo(plumbingMembers, 23, "Commercial length m", CommercialBars.Lengths.Select((double x) => x.ToString(CultureInfo.InvariantCulture)).ToArray());
				plumbingMembers.Visible = false;
				base.Controls.Add(plumbingMembers);
			}
			finally
			{
				filling = flag;
			}
			plumbingMembers.CurrentCellDirtyStateChanged += delegate
			{
				if (plumbingMembers.IsCurrentCellDirty && plumbingMembers.CurrentCell is DataGridViewComboBoxCell)
				{
					plumbingMembers.CommitEdit(DataGridViewDataErrorContexts.Commit);
				}
			};
			plumbingMembers.CellValueChanged += delegate(object s, DataGridViewCellEventArgs e)
			{
				if (!filling && e.RowIndex >= 0 && (e.ColumnIndex == 2 || e.ColumnIndex == 9 || e.ColumnIndex == 23))
				{
					if (e.ColumnIndex == 2)
					{
						bool flag3 = filling;
						filling = true;
						try
						{
							DataGridViewRow dataGridViewRow = plumbingMembers.Rows[e.RowIndex];
							string text = Cell(dataGridViewRow, 2);
							dataGridViewRow.Cells[18].Value = ((text == "Slab") ? "1" : ((text == "RC wall") ? "2" : "0"));
							dataGridViewRow.Cells[20].Value = ((text == "Slab on fill") ? "100" : "0");
							if (text == "CHB wall")
							{
								if (!QuantityInputDefaults.IsMultiplier(dataGridViewRow.DataGridView.Columns[9].HeaderText)) dataGridViewRow.Cells[9].Value = "1";
							}
						}
						finally
						{
							filling = flag3;
						}
					}
					Recalculate();
				}
			};
		}

		private void RecalculateStructureMembers()
		{
			structureMemberResults.Clear();
			structureMemberErrors.Clear();
			if (plumbingMembers == null)
			{
				return;
			}
			foreach (ChamberTakeoff q in chamberMeasures)
			{
				q.ComponentMode = plumbingMembers.Rows.Cast<DataGridViewRow>().Any((DataGridViewRow r) => !r.IsNewRow && Cell(r, 1).Equals(q.Id, StringComparison.OrdinalIgnoreCase));
				if (q.ComponentMode)
				{
					q.BaseConcrete = (q.CoverConcrete = (q.WallConcrete = (q.WallForms = (q.BaseForms = (q.CoverForms = (q.ChbArea = (q.MemberCHB100 = (q.MemberCHB150 = (q.GravelVolume = (q.PlasterArea = (q.WaterproofArea = 0.0)))))))))));
					q.AdditionalConcrete = q.OtherConcrete * (double)q.Count;
					q.AdditionalForms = q.OtherForms * (double)q.Count;
					q.ConcreteVolume = q.AdditionalConcrete;
					q.FormArea = q.AdditionalForms;
				}
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (DataGridViewRow row in (IEnumerable)plumbingMembers.Rows)
			{
				if (row.IsNewRow)
				{
					continue;
				}
				row.Tag = null;
				for (int num = 24; num <= 32; num++)
				{
					row.Cells[num].Value = "";
				}
				try
				{
					StructureMemberInput input = StructureMemberInput.Read((from c in Enumerable.Range(0, 24)
						select Cell(row, c)).ToArray());
					if (!hashSet.Add(input.Id))
					{
						throw new Exception("Duplicate member ID.");
					}
					ChamberTakeoff chamberTakeoff = chamberMeasures.SingleOrDefault((ChamberTakeoff ownerItem) => ownerItem.Id.Equals(input.StructureId, StringComparison.OrdinalIgnoreCase));
					if (chamberTakeoff == null)
					{
						throw new Exception("Structure dimensions are missing or invalid.");
					}
					if (chamberTakeoff.Kind == "Catch Basin (CHB)" && input.WaterproofFaces != 0)
					{
						throw new Exception("Catch basin has no waterproofing in the selected quantity scope.");
					}
					input.StructureId = chamberTakeoff.Id;
					row.Cells[1].Value = chamberTakeoff.Id;
					StructureMemberTakeoff structureMemberTakeoff = StructureMemberTakeoff.Calculate(input, chamberTakeoff.Count, StructureConfiguration(chamberTakeoff.Id));
					structureMemberResults.Add(structureMemberTakeoff);
					row.Tag = structureMemberTakeoff;
					chamberBarPlan.AddRange(structureMemberTakeoff.Plan);
					chamberTakeoff.ConcreteVolume += structureMemberTakeoff.Concrete;
					chamberTakeoff.FormArea += structureMemberTakeoff.Forms;
					chamberTakeoff.ChbArea += structureMemberTakeoff.CHBArea;
					chamberTakeoff.GravelVolume += structureMemberTakeoff.Gravel;
					chamberTakeoff.PlasterArea += structureMemberTakeoff.Plaster;
					chamberTakeoff.WaterproofArea += structureMemberTakeoff.Waterproof;
					if (input.Kind == "Slab on fill")
					{
						chamberTakeoff.BaseConcrete += structureMemberTakeoff.Concrete;
						chamberTakeoff.BaseForms += structureMemberTakeoff.Forms;
					}
					else if (input.Kind == "Slab")
					{
						chamberTakeoff.CoverConcrete += structureMemberTakeoff.Concrete;
						chamberTakeoff.CoverForms += structureMemberTakeoff.Forms;
					}
					else if (input.Kind == "RC wall")
					{
						chamberTakeoff.WallConcrete += structureMemberTakeoff.Concrete;
						chamberTakeoff.WallForms += structureMemberTakeoff.Forms;
					}
					else if (input.ThicknessMm == 100.0)
					{
						chamberTakeoff.MemberCHB100 += structureMemberTakeoff.CHBArea;
					}
					else
					{
						chamberTakeoff.MemberCHB150 += structureMemberTakeoff.CHBArea;
					}
					Measure(row, 24, structureMemberTakeoff.Concrete);
					Measure(row, 25, structureMemberTakeoff.Forms);
					Measure(row, 26, structureMemberTakeoff.Steel.Result.Lines.Sum((EstimateLine l) => l.Kg));
					Measure(row, 28, structureMemberTakeoff.CHBArea);
					Measure(row, 29, structureMemberTakeoff.Gravel);
					Measure(row, 30, structureMemberTakeoff.Plaster);
					Measure(row, 31, structureMemberTakeoff.Waterproof);
					row.Cells[32].Value = "Estimated from member dimensions and bar spacing/count";
				}
				catch (Exception ex)
				{
					row.Cells[32].Value = ex.Message;
					structureMemberErrors.Add(Cell(row, 1));
					errors.Add("Structure member " + Cell(row, 1) + ": " + ex.Message);
				}
			}
			foreach (ChamberTakeoff q2 in chamberMeasures.Where((ChamberTakeoff chamberTakeoff2) => chamberTakeoff2.ComponentMode))
			{
				q2.Backfill = Math.Max(0.0, q2.Excavation - q2.L * q2.W * (q2.H + q2.Base + q2.Cover) * (double)q2.Count - q2.GravelVolume);
				DataGridViewRow row2 = plumbingChambers.Rows.Cast<DataGridViewRow>().First((DataGridViewRow r) => Cell(r, 0) == q2.Id);
				Measure(row2, 26, q2.Backfill);
				Measure(row2, 27, q2.GravelVolume);
				Measure(row2, 28, q2.ConcreteVolume);
				Measure(row2, 30, q2.FormArea);
				Measure(row2, 31, q2.ChbArea);
				Measure(row2, 32, q2.PlasterArea);
				Measure(row2, 33, q2.WaterproofArea);
			}
		}

		private void UpdateStructureMemberPurchases()
		{
			if (plumbingMembers == null)
			{
				return;
			}
			foreach (DataGridViewRow item in (IEnumerable)plumbingMembers.Rows)
			{
				StructureMemberTakeoff structureMemberTakeoff = item.Tag as StructureMemberTakeoff;
				if (structureMemberTakeoff != null)
				{
					Measure(item, 27, structureMemberTakeoff.Plan.Sum((StockAllocation p) => p.Purchase));
				}
			}
		}

		private void BindStructureMembers(ChamberSheetView view)
		{
			if (filling || view.MaterialTabs.SelectedTab == null || Convert.ToString(view.MaterialTabs.SelectedTab.Tag) != "MEMBERS")
			{
				return;
			}
			plumbingMembers.EndEdit();
			plumbingMembers.CurrentCell = null;
			plumbingMembers.ClearSelection();
			activeStructureMemberId = ((view.Entries.CurrentRow == null) ? "" : Convert.ToString(view.Entries.CurrentRow.Cells[0].Value));
			view.MaterialTabs.SelectedTab.Controls.Add(plumbingMembers);
			plumbingMembers.Visible = activeStructureMemberId != "";
			view.MaterialTabs.SelectedTab.Controls.SetChildIndex(plumbingMembers, 0);
			foreach (DataGridViewRow item in (IEnumerable)plumbingMembers.Rows)
			{
				item.Visible = Cell(item, 1).Equals(activeStructureMemberId, StringComparison.OrdinalIgnoreCase);
			}
		}

		private void EditStructureMember(ChamberSheetView view, bool add)
		{
			Guard();
			DataGridViewRow source = ((view.Entries.CurrentRow == null) ? null : (view.Entries.CurrentRow.Tag as DataGridViewRow));
			if (source == null)
			{
				throw new Exception("Add/select a structure first.");
			}
			ChamberTakeoff chamberTakeoff = chamberMeasures.SingleOrDefault((ChamberTakeoff q) => q.Id == Cell(source, 0));
			if (chamberTakeoff == null)
			{
				throw new Exception("Complete the structure geometry first.");
			}
			DataGridViewRow row = (add ? null : plumbingMembers.CurrentRow);
			if (!add && (row == null || Cell(row, 1) != chamberTakeoff.Id))
			{
				throw new Exception("Select a member first.");
			}
			string[] existing = ((row == null) ? null : (from c in Enumerable.Range(0, 24)
				select Cell(row, c)).ToArray());
			using (StructureMemberDialog structureMemberDialog = new StructureMemberDialog(chamberTakeoff, existing, StructureConfiguration(chamberTakeoff.Id)))
			{
				if (structureMemberDialog.ShowDialog(this) != DialogResult.OK)
				{
					return;
				}
				bool flag = filling;
				filling = true;
				try
				{
					if (row == null)
					{
						row = plumbingMembers.Rows[plumbingMembers.Rows.Add()];
					}
					for (int num = 0; num < 24; num++)
					{
						row.Cells[num].Value = structureMemberDialog.Values[num];
					}
				}
				finally
				{
					filling = flag;
				}
				Recalculate();
				BindStructureMembers(view);
			}
		}

		private void UpdateStructureCutDetails(ChamberSheetView view, ChamberTakeoff owner)
		{
			if (view.CutDetails == null)
			{
				return;
			}
			view.CutDetails.Rows.Clear();
			if (owner == null)
			{
				return;
			}
			List<StructureMemberTakeoff> members = structureMemberResults.Where((StructureMemberTakeoff m) => m.Input.StructureId == owner.Id).ToList();
			foreach (StructureMemberTakeoff item in members)
			{
				foreach (EstimateLine line in item.Steel.Result.Lines)
				{
					view.CutDetails.Rows.Add(line.Part, line.Diameter, (long)line.PiecesPerBeam * (long)item.TotalQuantity, line.CutLength, line.Kg, line.Formula);
				}
			}
			foreach (StockAllocation item2 in chamberBarPlan.Where((StockAllocation a) => a.Entry.Beam == owner.Id && !members.Any((StructureMemberTakeoff m) => m.Steel == a.Entry)))
			{
				view.CutDetails.Rows.Add(item2.Entry.Description, item2.Demand.Diameter, item2.Demand.Pieces, item2.Demand.Cut, item2.Net, "Additional approved developed cut");
			}
			view.CutDetails.Rows.Add("FINAL REBAR TO BUY", null, null, null, owner.RebarPurchase, "Whole-stock purchase weight includes configured laps and allocated compatible offcuts.");
		}

		private void WriteStructureConfigurations(XmlElement root)
		{
			XmlElement xmlElement = root.OwnerDocument.CreateElement("StructureConfigurations");
			root.AppendChild(xmlElement);
			foreach (KeyValuePair<string, LengthConfiguration> structureConfiguration in structureConfigurations)
			{
				XmlElement xmlElement2 = root.OwnerDocument.CreateElement("Configuration");
				xmlElement2.SetAttribute("structure", structureConfiguration.Key);
				structureConfiguration.Value.Write(xmlElement2);
				xmlElement.AppendChild(xmlElement2);
			}
		}

		private void ReadStructureConfigurations(XmlElement root)
		{
			structureConfigurations.Clear();
			foreach (XmlElement item in root.SelectNodes("StructureConfigurations/Configuration"))
			{
				string attribute = item.GetAttribute("structure");
				if (attribute == "" || structureConfigurations.ContainsKey(attribute))
				{
					throw new Exception("Invalid saved structure calculation profile.");
				}
				structureConfigurations.Add(attribute, LengthConfiguration.Read(item));
			}
		}

		private void AppendStructureMemberReport(StringBuilder b, ChamberTakeoff owner)
		{
			if (!owner.ComponentMode)
			{
				return;
			}
			b.AppendLine("  COMPONENT-BASED QUANTITIES: solid slab / slab-on-fill / RC wall / CHB members. No geometric tank concrete is added a second time.");
			foreach (StructureMemberTakeoff item in structureMemberResults.Where((StructureMemberTakeoff m) => m.Input.StructureId == owner.Id))
			{
				StructureMemberInput input = item.Input;
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "  {0} | {1}: {2} x {3} x {4}mm, {5} member(s)/structure x {6} structure(s), cover {7}mm, {8} mat(s)", input.Description, input.Kind, input.L, input.B, input.ThicknessMm, input.Quantity, owner.Count, input.CoverMm, input.Layers));
				b.AppendLine(string.Format(CultureInfo.InvariantCulture, "    Concrete {0:0.########}m3; forms {1:0.########}m2; CHB {2:0.########}m2; gravel {3:0.########}m3; plaster {4:0.########}m2; waterproof {5:0.########}m2", item.Concrete, item.Forms, item.CHBArea, item.Gravel, item.Plaster, item.Waterproof));
				foreach (EstimateLine line in item.Steel.Result.Lines)
				{
					b.AppendLine("    " + line.Part + " | D" + line.Diameter + " | " + line.Formula + " | " + line.Kg.ToString("0.########", CultureInfo.InvariantCulture) + "kg");
				}
				b.AppendLine("    Final allocated rebar purchase: " + item.Plan.Sum((StockAllocation a) => a.Purchase).ToString("0.########", CultureInfo.InvariantCulture) + "kg; selected stock " + input.Stock + "m.");
			}
		}
	}
}
