using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal sealed partial class ExcelLiveLink : IDisposable
	{
		private dynamic app;

		private dynamic book;

		private bool isolated;

		private bool freshWorkbook;

		private const string EstimateSheet = "DTCE_Estimate";

		private const string BillSheet = "DTCE Bill of Quantities";

		private const string BillSchema = "DTCEBillOfQuantitiesV1";

		public bool KeepUnloadedBackupItems;

		public Dictionary<string, BackupTarget> LastBackupRoutes;

		private Dictionary<string, double> lastBackupTotals;

		private const string TotalPrefix = "_DTCE_BACKUP_TOTAL_";

		public string PathName { get; private set; }

		public Dictionary<string, double> LastBackupTotals
		{
			get
			{
				if (lastBackupTotals == null)
				{
					lastBackupTotals = new Dictionary<string, double>();
				}
				return lastBackupTotals;
			}
		}

		public bool HasBackup
		{
			get
			{
				return FindEstimateSheet("BACKUP") != null;
			}
		}

		internal static int WorkbookFormat(string path)
		{
			string text = Path.GetExtension(path).ToLowerInvariant();
			if (text == ".xlsm")
			{
				return 52;
			}
			if (text == ".xlsx")
			{
				return 51;
			}
			throw new Exception("Use an .xlsm or .xlsx Excel workbook.");
		}

		public ExcelLiveLink(string path, bool visible = true, bool requireTemplate = true)
		{
			PathName = Path.GetFullPath(path);
			int num = WorkbookFormat(PathName);
			isolated = !visible;
			string text = null;
			try
			{
				Type typeFromProgID = Type.GetTypeFromProgID("Excel.Application");
				if (typeFromProgID == null)
				{
					throw new Exception("Install desktop Microsoft Excel to use the live workbook link.");
				}
				if (visible)
				{
					try
					{
						app = Marshal.GetActiveObject("Excel.Application");
					}
					catch (COMException)
					{
					}
				}
				if (app == null)
				{
					app = Activator.CreateInstance(typeFromProgID);
				}
				foreach (dynamic item in app.Workbooks)
				{
					if (string.Equals((string)item.FullName, PathName, StringComparison.OrdinalIgnoreCase))
					{
						book = item;
						break;
					}
				}
				if (book == null)
				{
					int num2 = Convert.ToInt32(app.AutomationSecurity);
					bool flag = Convert.ToBoolean(app.EnableEvents);
					try
					{
						app.AutomationSecurity = 3;
						app.EnableEvents = false;
						if (!File.Exists(PathName))
						{
							if (requireTemplate)
							{
								text = Path.Combine(Path.GetTempPath(), "DTCE-template-" + Guid.NewGuid().ToString("N") + ".xlsx");
								using (Stream stream = typeof(ExcelLiveLink).Assembly.GetManifestResourceStream("BeamSummaryTemplate.xlsx"))
								{
									using (FileStream destination = File.Create(text))
									{
										if (stream == null)
										{
											throw new Exception("Excel template resource missing.");
										}
										stream.CopyTo(destination);
									}
								}
								book = app.Workbooks.Open(text, 0, false);
							}
							else
							{
								book = app.Workbooks.Add(-4167);
								freshWorkbook = true;
							}
							book.SaveAs(PathName, num);
						}
						else
						{
							book = app.Workbooks.Open(PathName, 0, false);
						}
					}
					finally
					{
						app.AutomationSecurity = num2;
						app.EnableEvents = flag;
					}
				}
				if ((bool)book.ReadOnly)
				{
					throw new Exception("The linked Excel workbook is read-only.");
				}
				if (requireTemplate && Convert.ToString(book.Worksheets["RawData"].Range["N1"].Value2) != "BeamToolsSummaryV1")
				{
					throw new Exception("Choose a Beam/Slab Summary workbook created by this app, or link your existing workbook from Program of Works.");
				}
				if (visible)
				{
					app.Visible = true;
				}
			}
			catch
			{
				Dispose();
				throw;
			}
			finally
			{
				if (text != null)
				{
					try
					{
						File.Delete(text);
					}
					catch
					{
					}
				}
			}
		}

		public void Update(object[,] rows, string status, string estimateXml = null, object[,] concrete = null)
		{
			bool flag = Convert.ToBoolean(app.EnableEvents);
			try
			{
				app.EnableEvents = false;
				UpdateCore(rows, status, estimateXml, concrete);
			}
			finally
			{
				app.EnableEvents = flag;
			}
		}

		private void UpdateCore(object[,] rows, string status, string estimateXml, object[,] concrete)
		{
			dynamic val = book.Worksheets["RawData"];
			dynamic val2 = val.ListObjects["BeamData"];
			int val3 = (int)val2.Range.Rows.Count;
			val.Range["A2:L" + Math.Max(2, val3)].ClearContents();
			int length = rows.GetLength(0);
			val2.Resize(val.Range["A1:L" + (Math.Max(1, length) + 1)]);
			if (length <= 0)
			{
				val.Range["D2:L2"].Value2 = 0;
			}
			else
			{
				val.Range["A2:C" + (length + 1)].NumberFormat = "@";
				val.Range["A2:L" + (length + 1)].Value2 = rows;
			}
			val.Range["F2:K" + (Math.Max(1, length) + 1)].NumberFormat = "0.00";
			book.Worksheets["Summary"].Range["B3"].Value2 = status;
			book.Worksheets["Summary"].Range["B4"].Value2 = "Updated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			if (concrete != null)
			{
				WriteConcrete(concrete, status.Contains("Slab purchase"));
			}
			if (estimateXml != null)
			{
				WriteEstimate(estimateXml);
			}
			app.CalculateFull();
			book.Save();
		}

		private void WriteConcrete(object[,] rows, bool slab = false)
		{
			string text = (slab ? "SlabConcreteFormwork" : "BeamConcreteFormwork");
			string text2 = (slab ? "DTCESlabMeasuresV1" : "DTCEBeamMeasuresV1");
			dynamic val = null;
			foreach (dynamic item in book.Worksheets)
			{
				if ((string)item.Name == text)
				{
					val = item;
					break;
				}
			}
			if (val == null)
			{
				val = book.Worksheets.Add();
				val.Name = text;
			}
			else if (Convert.ToString(val.Range["J1"].Value2) != text2)
			{
				throw new Exception("Existing BeamConcreteFormwork sheet is not owned by this app; it was not overwritten.");
			}
			val.Range["A:H"].ClearContents();
			val.Range["J1"].Value2 = text2;
			object[,] array = new object[1, 8] { 
			{
				"Description",
				slab ? "Slab" : "Beam",
				"Support",
				"Quantity",
				"Concrete m3 (gross)",
				slab ? "Cantilever edge forms m2" : "Side forms m2",
				"Bottom forms m2",
				"Total forms m2"
			} };
			val.Range["A1:H1"].Value2 = array;
			int length = rows.GetLength(0);
			if (length > 0)
			{
				val.Range["A2:H" + (length + 1)].Value2 = rows;
			}
			val.Range["E2:H" + (Math.Max(1, length) + 1)].NumberFormat = "0.00";
			int num = length + 3;
			val.Cells[num, 1].Value2 = "TOTAL";
			string[] array2 = new string[4] { "E", "F", "G", "H" };
			foreach (string text3 in array2)
			{
				val.Range[text3 + num].Formula = "=SUM(" + text3 + "2:" + text3 + (Math.Max(1, length) + 1) + ")";
			}
			val.Range["E" + num + ":H" + num].NumberFormat = "0.00";
			val.Range["A:H"].Columns.AutoFit();
		}

		private dynamic NewOutputSheet(string name)
		{
			dynamic val;
			if (freshWorkbook && (int)book.Worksheets.Count == 1)
			{
				val = book.Worksheets[1];
				freshWorkbook = false;
			}
			else
			{
				val = book.Worksheets.Add();
			}
			val.Name = name;
			return val;
		}

		private dynamic FindEstimateSheet(string name = "DTCE_Estimate")
		{
			foreach (dynamic item in book.Worksheets)
			{
				if ((string)item.Name == name)
				{
					return item;
				}
			}
			return null;
		}

		public string ReadEstimate()
		{
			return ReadSnapshot("DTCE_Estimate");
		}

		public string ReadWorksProject()
		{
			return ReadSnapshot("DTCE_POW_Project");
		}

		private string ReadSnapshot(string name)
		{
			dynamic val = FindEstimateSheet(name);
			if (val == null)
			{
				return null;
			}
			if (Convert.ToString(val.Range["A1"].Value2) != "DTCEEmbeddedEstimateV1")
			{
				throw new Exception("Reserved estimate sheet has unrecognized content; it was not overwritten.");
			}
			int num = default(int);
			if (!int.TryParse(Convert.ToString(val.Range["A2"].Value2), out num) || num < 1 || num > 5000)
			{
				throw new Exception("Invalid saved estimate chunk count.");
			}
			string[] array = new string[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = Convert.ToString(val.Cells[i + 3, 1].Value2);
			}
			return ExcelEstimateData.Unpack(array);
		}

		private void WriteEstimate(string xml, string name = "DTCE_Estimate", string activate = "Summary")
		{
			string[] array = ExcelEstimateData.Pack(xml);
			dynamic val = FindEstimateSheet(name);
			if (val == null)
			{
				val = book.Worksheets.Add();
				val.Name = name;
			}
			else if (Convert.ToString(val.Range["A1"].Value2) != "DTCEEmbeddedEstimateV1")
			{
				throw new Exception("Reserved estimate sheet has unrecognized content; it was not overwritten.");
			}
			val.UsedRange.ClearContents();
			val.Range["A1:A" + (array.Length + 2)].NumberFormat = "@";
			val.Range["A1"].Value2 = "DTCEEmbeddedEstimateV1";
			val.Range["A2"].Value2 = array.Length.ToString();
			object[,] array2 = new object[array.Length, 1];
			for (int i = 0; i < array.Length; i++)
			{
				array2[i, 0] = array[i];
			}
			val.Range["A3:A" + (array.Length + 2)].Value2 = array2;
			book.Worksheets[activate].Activate();
			val.Visible = 2;
		}

		public void UpdateWorks(List<WorksExcelTable> tables, string xml)
		{
			bool flag = Convert.ToBoolean(app.EnableEvents);
			try
			{
				app.EnableEvents = false;
				UpdateWorksCore(tables, xml);
			}
			finally
			{
				app.EnableEvents = flag;
			}
		}

		public void UpdateWorksQuantities(List<WorksExcelTable> tables)
		{
			UpdateWorks(tables, null);
		}

		private void UpdateWorksCore(List<WorksExcelTable> tables, string xml)
		{
			foreach (WorksExcelTable table in tables)
			{
				dynamic val = FindEstimateSheet(table.Name);
				if (val != null && Convert.ToString(val.Range["AA1"].Value2) != "DTCEProgramOfWorksV1:" + table.Name)
				{
					throw new Exception("Sheet '" + table.Name + "' is not owned by Program of Works. Rename it or choose another workbook.");
				}
			}
			dynamic val2 = FindEstimateSheet("DTCE_POW_Project");
			if (xml != null && val2 != null && Convert.ToString(val2.Range["A1"].Value2) != "DTCEEmbeddedEstimateV1")
			{
				throw new Exception("Reserved project sheet has unrecognized content.");
			}
			dynamic val3 = FindEstimateSheet("DTCE Tool Kit - POW");
			if (val3 != null && Convert.ToString(val3.Range["AA3"].Value2) != "DTCEFootingSummaryV1")
			{
				for (int i = 33; i <= 35; i++)
				{
					for (int j = 1; j <= 3; j++)
					{
						if ((!string.IsNullOrEmpty(Convert.ToString(val3.Cells[i, j].Value2))))
						{
							throw new Exception("Footing summary needs reserved cells A33:C35, but these contain custom data. Move that data or choose another workbook.");
						}
					}
				}
			}
			dynamic val4 = FindEstimateSheet("DTCE Tool Kit - POW");
			dynamic val5 = FindEstimateSheet("POW Summary");
			dynamic val6 = val4;
			if (val6 == null && val5 != null && Convert.ToString(val5.Range["AA1"].Value2) == "DTCEProgramOfWorksV1:POW Summary")
			{
				val6 = val5;
			}
			if (tables.Exists((WorksExcelTable t) => t.Name == "DTCE Tool Kit - POW" && t.StartColumn == 4) && val6 != null && Convert.ToString(val6.Range["AA2"].Value2) != "DTCEFinishCatalogV1")
			{
				for (int num = 35; num <= 56; num++)
				{
					for (int num2 = 4; num2 <= 6; num2++)
					{
						if ((!string.IsNullOrEmpty(Convert.ToString(val6.Cells[num, num2].Value2))))
						{
							throw new Exception("Finish totals need reserved cells D35:F56, but these contain custom data. Move that data or choose a new workbook.");
						}
					}
				}
			}
			if (tables.Exists((WorksExcelTable t) => t.Name == "DTCE Tool Kit - POW" && t.StartColumn == 8) && val4 != null && Convert.ToString(val4.Range["AA4"].Value2) != "DTCEFinalQuantitiesV1")
			{
				for (int num3 = 1; num3 <= 200; num3++)
				{
					for (int num4 = 8; num4 <= 10; num4++)
					{
						if ((!string.IsNullOrEmpty(Convert.ToString(val4.Cells[num3, num4].Value2))))
						{
							throw new Exception("Final quantities need cells H1:J200, but these contain custom data. Move the data or choose another workbook.");
						}
					}
				}
			}
			if (xml != null)
			{
				ExcelEstimateData.Pack(xml);
			}
			if (val4 == null && val5 != null && Convert.ToString(val5.Range["AA1"].Value2) == "DTCEProgramOfWorksV1:POW Summary")
			{
				val5.Name = "DTCE Tool Kit - POW";
				val5.Range["AA1"].Value2 = "DTCEProgramOfWorksV1:DTCE Tool Kit - POW";
			}
			foreach (WorksExcelTable table2 in tables)
			{
				dynamic val7 = FindEstimateSheet(table2.Name);
				if (val7 == null)
				{
					val7 = NewOutputSheet(table2.Name);
				}
				int startRow = table2.StartRow;
				int startColumn = table2.StartColumn;
				int num5 = table2.Headers.Length;
				int length = table2.Rows.GetLength(0);
				int num6 = startRow + length;
				if (!(table2.Name == "DTCE Tool Kit - POW"))
				{
					val7.UsedRange.ClearContents();
				}
				else
				{
					switch (startColumn)
					{
					case 1:
						val7.Range["A1:C" + 38].ClearContents();
						val7.Range["AA3"].Value2 = "DTCEFootingSummaryV1";
						break;
					case 4:
						val7.Range["D35:F56"].ClearContents();
						val7.Range["AA2"].Value2 = "DTCEFinishCatalogV1";
						break;
					case 8:
						val7.Range["H1:J200"].ClearContents();
						val7.Range["AA4"].Value2 = "DTCEFinalQuantitiesV1";
						break;
					}
				}
				val7.Range["AA1"].Value2 = "DTCEProgramOfWorksV1:" + table2.Name;
				object[,] array = new object[1, num5];
				for (int num7 = 0; num7 < num5; num7++)
				{
					array[0, num7] = table2.Headers[num7];
				}
				val7.Range[val7.Cells[startRow, startColumn], val7.Cells[startRow, startColumn + num5 - 1]].Value2 = array;
				val7.Range[val7.Cells[startRow, startColumn], val7.Cells[startRow, startColumn + num5 - 1]].Font.Bold = true;
				if (length > 0)
				{
					dynamic val8 = val7.Range[val7.Cells[startRow + 1, startColumn], val7.Cells[num6, startColumn + num5 - 1]];
					val8.NumberFormat = "0.00";
					for (int num8 = 0; num8 < num5; num8++)
					{
						bool flag = false;
						bool flag2 = true;
						for (int num9 = 0; num9 < length; num9++)
						{
							object obj = table2.Rows[num9, num8];
							if (obj is string)
							{
								flag = true;
							}
							if (obj != null && !(obj is int) && !(obj is long))
							{
								flag2 = false;
							}
						}
						dynamic val9 = val7.Range[val7.Cells[startRow + 1, startColumn + num8], val7.Cells[num6, startColumn + num8]];
						if (flag)
						{
							val9.NumberFormat = "@";
						}
						else if (flag2)
						{
							val9.NumberFormat = "0";
						}
					}
					val8.Value2 = table2.Rows;
				}
				val7.Range[val7.Cells[startRow, startColumn], val7.Cells[Math.Max(startRow, num6), startColumn + num5 - 1]].Columns.AutoFit();
			}
			if (xml == null)
			{
				book.Worksheets["DTCE Tool Kit - POW"].Activate();
			}
			else
			{
				WriteEstimate(xml, "DTCE_POW_Project", "DTCE Tool Kit - POW");
			}
			app.CalculateFull();
			/* not saved: the owner saves the Program of Works workbook in Excel (19 November 2026) */
		}

		private static string[] DesignNames(string kind)
		{
			if (kind == "Purlin-Design")
			{
				return new string[2] { "DTCE Purlin Design", "DTCE_Design_Purlin" };
			}
			if (kind == "Truss-Connections")
			{
				return new string[2] { "DTCE Truss Connections", "DTCE_Design_Truss" };
			}
			throw new Exception("Unsupported design sheet type.");
		}

		public void UpdateDesignReport(string kind, string report)
		{
			string[] array = DesignNames(kind);
			string text = "DTCEDesignReportV1:" + kind;
			dynamic val = FindEstimateSheet(array[0]);
			if (val != null && Convert.ToString(val.Range["AA1"].Value2) != text)
			{
				throw new Exception("Existing design report sheet is not owned by this app.");
			}
			bool flag = Convert.ToBoolean(app.EnableEvents);
			try
			{
				app.EnableEvents = false;
				dynamic val2 = val;
				if (val2 == null)
				{
					val2 = NewOutputSheet(array[0]);
				}
				val2.UsedRange.ClearContents();
				val2.Range["AA1"].Value2 = text;
				string[] array2 = report.Replace("\r", "").Split('\n');
				object[,] array3 = new object[array2.Length, 1];
				for (int i = 0; i < array2.Length; i++)
				{
					array3[i, 0] = array2[i];
				}
				val2.Range["A1:A" + array2.Length].NumberFormat = "@";
				val2.Range["A1:A" + array2.Length].Value2 = array3;
				val2.Range["A1:A" + array2.Length].ColumnWidth = 110;
				val2.Range["A1:A" + array2.Length].WrapText = true;
				val2.Range["A1:A" + array2.Length].Rows.AutoFit();
				val2.Activate();
				book.Save();
			}
			finally
			{
				app.EnableEvents = flag;
			}
		}

		public double Value(string cell)
		{
			return Convert.ToDouble(book.Worksheets["Summary"].Range[cell].Value2);
		}

		public void Dispose()
		{
			try
			{
				if (isolated && book != null)
				{
					book.Close(false);
				}
			}
			catch
			{
			}
			try
			{
				if (isolated && app != null)
				{
					app.Quit();
				}
			}
			catch
			{
			}
			if (book != null && Marshal.IsComObject(book))
			{
				Marshal.ReleaseComObject(book);
			}
			if (app != null && Marshal.IsComObject(app))
			{
				Marshal.ReleaseComObject(app);
			}
			book = null;
			app = null;
		}

		private void ValidateBillSheet()
		{
			dynamic val = FindEstimateSheet("DTCE Bill of Quantities");
			if (val != null && Convert.ToString(val.Range["AA1"].Value2) != "DTCEBillOfQuantitiesV1")
			{
				throw new Exception("The Bill of Quantities worksheet contains custom data. Rename it or choose a new workbook before syncing.");
			}
		}

		public void UpdateWorksQuantities(List<WorksExcelTable> tables, List<BillQuantity> quantities, ProjectInformation project)
		{
			ValidateBillSheet();
			UpdateWorksQuantities(tables);
			bool flag = Convert.ToBoolean(app.EnableEvents);
			try
			{
				app.EnableEvents = false;
				WriteBill(quantities, project);
				app.CalculateFull();
				/* not saved: the owner saves the Program of Works workbook in Excel (19 November 2026) */
			}
			finally
			{
				app.EnableEvents = flag;
			}
		}

		private void WriteBill(List<BillQuantity> quantities, ProjectInformation project)
		{
			dynamic val = FindEstimateSheet("DTCE Bill of Quantities");
			if (val == null)
			{
				val = NewOutputSheet("DTCE Bill of Quantities");
			}
			val.UsedRange.UnMerge();
			val.UsedRange.Clear();
			val.Range["AA1"].Value2 = "DTCEBillOfQuantitiesV1";
			val.Range["A1:D13"].NumberFormat = "@";
			string[] array = new string[7] { "A1:D1", "A2:D2", "A4:D4", "B6:D6", "B7:D7", "B9:D9", "B10:D10" };
			foreach (string text in array)
			{
				val.Range[text].Merge();
			}
			val.Range["A1"].Value2 = "Republic of the Philippines";
			val.Range["A2"].Value2 = "DEPARTMENT OF PUBLIC WORKS AND HIGHWAYS";
			val.Range["A4"].Value2 = "BILL OF QUANTITIES AND DETAILED QUANTITY TAKE-OFF / CALCULATIONS";
			val.Range["A6"].Value2 = "Implementing Office";
			val.Range["B6"].Value2 = project.Office;
			val.Range["A7"].Value2 = "Address";
			val.Range["B7"].Value2 = project.OfficeAddress;
			val.Range["A9"].Value2 = "Project Name";
			val.Range["B9"].Value2 = project.Name;
			val.Range["A10"].Value2 = "Project Location";
			val.Range["B10"].Value2 = project.Location;
			List<BillDisplayRow> list = BillQuantityLayout.Rows(quantities);
			object[,] array2 = new object[list.Count + 1, 4];
			array2[0, 0] = "ITEM NO.";
			array2[0, 1] = "DESCRIPTION";
			array2[0, 2] = "QUANTITY";
			array2[0, 3] = "UNIT";
			val.Range["A13:B" + Math.Max(14, 13 + list.Count)].NumberFormat = "@";
			for (int j = 0; j < list.Count; j++)
			{
				BillDisplayRow billDisplayRow = list[j];
				array2[j + 1, 0] = billDisplayRow.Item;
				array2[j + 1, 1] = billDisplayRow.Description;
				array2[j + 1, 2] = (billDisplayRow.Quantity.HasValue ? ((object)billDisplayRow.Quantity.Value) : null);
				array2[j + 1, 3] = billDisplayRow.Unit;
			}
			int val2 = 13 + list.Count;
			val.Range["A13:D" + val2].Value2 = array2;
			val.Range["A1:D" + val2].Font.Name = "Arial";
			val.Range["A1:D" + val2].Font.Size = 10;
			val.Range["A1:D4"].HorizontalAlignment = -4108;
			val.Range["A2:D4"].Font.Bold = true;
			val.Range["A4"].Font.Size = 12;
			val.Columns[1].ColumnWidth = 22;
			val.Columns[2].ColumnWidth = 80;
			val.Columns[3].ColumnWidth = 17;
			val.Columns[4].ColumnWidth = 12;
			val.Range["A1:D" + val2].WrapText = true;
			val.Range["A1:D" + val2].VerticalAlignment = -4160;
			val.Range["C14:C" + Math.Max(14, val2)].NumberFormat = "#,##0.00";
			val.Range["A13:D13"].Font.Bold = true;
			val.Range["A13:D13"].HorizontalAlignment = -4108;
			val.Range["A13:D" + val2].Borders.LineStyle = 1;
			for (int k = 0; k < list.Count; k++)
			{
				dynamic val3 = val.Range["A" + (14 + k) + ":D" + (14 + k)];
				if (list[k].Section)
				{
					val3.Font.Bold = true;
					val3.Interior.Color = ColorTranslator.ToOle(Color.FromArgb(225, 233, 240));
				}
				else if (list[k].Source)
				{
					val3.Font.Color = ColorTranslator.ToOle(Color.DimGray);
					val.Cells[14 + k, 2].IndentLevel = 1;
				}
				else
				{
					val3.Font.Bold = true;
				}
			}
			val.Range["A1:D" + val2].Rows.AutoFit();
			val.Rows[4].RowHeight = 36;
			val.Rows[6].RowHeight = 30;
			val.Rows[7].RowHeight = 30;
			val.Rows[9].RowHeight = 45;
			val.Rows[10].RowHeight = 30;
			val.Rows[13].RowHeight = 30;
			val.PageSetup.PrintArea = "$A$1:$D$" + val2;
			val.PageSetup.PrintTitleRows = "$1:$13";
			val.PageSetup.Orientation = 2;
			val.PageSetup.Zoom = false;
			val.PageSetup.FitToPagesWide = 1;
			val.PageSetup.FitToPagesTall = false;
			val.Activate();
		}

public List<BackupTarget> RegisterBackupBillItems(){backupNameRows=null;backupNameSignatures=null;nameIdentityCache=null;var targets=BackupTargets();registeredItemSlots=new HashSet<string>(CachedBackupNames(BackupMapping.ItemPrefix,null).Keys.Select(n=>n.Substring(BackupMapping.ItemPrefix.Length)),StringComparer.OrdinalIgnoreCase);targetCache=targets;routeCache=null;return targets;}


		public List<BackupTarget> BackupTargets()
		{
			dynamic val = FindEstimateSheet("BACKUP");
			if (val == null)
			{
				throw new Exception("The linked workbook has no BACKUP sheet.");
			}
			if (Convert.ToBoolean(val.ProtectContents))
			{
				throw new Exception("Unprotect BACKUP in Excel before mapping quantities.");
			}
			dynamic val2 = val.UsedRange;
			int num = Convert.ToInt32(val2.Row);
			int num2 = Convert.ToInt32(val2.Rows.Count);
			if (num2 > 20000)
			{
				throw new Exception("BACKUP exceeds the supported 20,000 rows.");
			}
			Dictionary<string, int> source = BackupNames("_DTCE_BACKUP_ITEM_");
			int num3 = num + num2 - 1;
			object[,] array = (object[,])val.Range["B1:BP" + num3].Value2;
			int lowerBound = array.GetLowerBound(0);
			int lowerBound2 = array.GetLowerBound(1);
			List<BackupTarget> list = new List<BackupTarget>();
			int row;
			for (row = 1; row <= num3; row++)
			{
				object obj = array[lowerBound + row - 1, lowerBound2];
				object obj2 = array[lowerBound + row - 1, lowerBound2 + 6];
				object obj3 = array[lowerBound + row - 1, lowerBound2 + 61];
				if (obj == null || obj2 == null || obj3 == null || obj is ErrorWrapper || obj2 is ErrorWrapper || obj3 is ErrorWrapper)
				{
					continue;
				}
				string text = Convert.ToString(obj);
				string text2 = Convert.ToString(obj2);
				string text3 = Convert.ToString(obj3);
				if (!text.Contains("(") || text.StartsWith("#") || text2.StartsWith("#") || text3.StartsWith("#"))
				{
					continue;
				}
				dynamic val3 = val.Cells[row, 55];
				if (((!Convert.ToBoolean(val3.MergeCells))) || ((string)Convert.ToString(val3.MergeArea.Address)).Replace("$", "") != "BC" + row + ":BJ" + row)
				{
					continue;
				}
				Color color = ColorTranslator.FromOle(Convert.ToInt32(val3.Interior.Color));
				if (color.G > color.R && color.G > color.B)
				{
					string slotId = (from x in source
						where x.Value == row
						select x.Key.Substring("_DTCE_BACKUP_ITEM_".Length)).OrderBy((string x) => x, StringComparer.Ordinal).FirstOrDefault() ?? Guid.NewGuid().ToString("N");
					list.Add(new BackupTarget
					{
						Row = row,
						Code = text.Trim(),
						Description = text2.Trim(),
						Unit = text3.Trim(),
						SlotId = slotId,
						Quantity = BackupBill.ReadQuantity(array[lowerBound+row-1,lowerBound2+53])
					});
				}
			}
			if (list.Count == 0)
			{
				throw new Exception("No green BACKUP total boxes were found. Open the full workbook so its DETAILED item captions can resolve.");
			}
			for (int num4 = 0; num4 < list.Count; num4++)
			{
				list[num4].EndRow = ((num4 + 1 < list.Count) ? (list[num4 + 1].Row - 2) : num3);
			}
			return list;
		}

        private Dictionary<string,int> BackupNames(string prefix,Dictionary<string,string> identities=null){return CachedBackupNames(prefix,identities);}


		public Dictionary<string, BackupTarget> BackupRoutes(List<BackupDemand> demands, List<BackupTarget> targets)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			Dictionary<string, int> dictionary2 = BackupNames("_DTCE_BACKUP_MAP_", dictionary);
			Dictionary<string, BackupTarget> dictionary3 = new Dictionary<string, BackupTarget>();
			foreach (BackupDemand d in demands)
			{
				BackupTarget backupTarget;
				if (d.TargetSlotId != null)
				{
					backupTarget = targets.SingleOrDefault((BackupTarget x) => x.SlotId == d.TargetSlotId && BackupMapping.Identity(x) == d.TargetIdentity);
					if (backupTarget == null)
					{
						if(d.ManualOverride)throw new Exception("The manual BOQ item was removed or changed: " + d.Description);
                        continue;
					}
					dictionary3.Add(d.Key, backupTarget);
					continue;
				}
				string key = BackupMapping.Name(d.Key);
				if (!dictionary2.ContainsKey(key) && d.Key.StartsWith("Bill of Quantities|"))
				{
					key = BackupMapping.Name("Plumbing Works|" + d.Key.Substring("Bill of Quantities|".Length));
				}
				int value;
				if (dictionary2.TryGetValue(key, out value))
				{
					string value2;
					dictionary.TryGetValue(key, out value2);
					backupTarget = BackupMapping.ResolveSaved(value, value2, targets);
				}
				else
				{
					/* Painting is computed and saved but only goes to the POW workbook once it is mapped by hand (owner, 17 November 2026). */
					backupTarget = ((d.Quantity > 1E-10 || d.ManualOverride || d.Sources.Any((BillQuantity x) => x.Quantity > 1E-10)) && !BackupMapping.IsPainting(d)) ? BackupMapping.Suggest(d, targets) : null;
				}
				if (backupTarget != null)
				{
					dictionary3.Add(d.Key, backupTarget);
				}
			}
			return dictionary3;
		}

		public void UpdateBackupQuantities(List<BackupDemand> all, IWin32Window owner, bool edit = false)
		{
			List<BackupTarget> targets = BackupTargets();
			Dictionary<string, int> saved = BackupNames("_DTCE_BACKUP_MAP_");
			List<BackupDemand> list = all.Where((BackupDemand d) => d.ManualOverride || d.Quantity > 1E-10 || d.Sources.Any((BillQuantity x) => x.Quantity > 1E-10) || saved.ContainsKey(BackupMapping.Name(d.Key))).ToList();
			Dictionary<string, BackupTarget> routes = BackupRoutes(list, targets);
			if (edit || list.Any((BackupDemand d) => !routes.ContainsKey(d.Key)))
			{
				using (BackupMapDialog backupMapDialog = new BackupMapDialog(list, targets, routes))
				{
					if (backupMapDialog.ShowDialog(owner) != DialogResult.OK)
					{
						throw new OperationCanceledException("BACKUP mapping was not applied. No quantities were written.");
					}
				}
			}
			WriteMappedBackup(list, targets, routes);
		}

internal void WriteMappedBackup(List<BackupDemand> demands,List<BackupTarget> targets,Dictionary<string,BackupTarget> routes){
 var saved=BackupNames(BackupMapping.MapPrefix);/* Unmapped or incomplete LS routes stay unmapped instead of stopping the write (owner, 17 November 2026). */routes=new Dictionary<string,BackupTarget>(routes);BackupMapping.DropIncomplete(demands,routes);demands=demands.Where(d=>routes.ContainsKey(d.Key)).ToList();BackupMapping.Validate(demands,routes);var identities=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);var owned=BackupNames(BackupMapping.BlockPrefix,identities);var groups=demands.GroupBy(d=>routes[d.Key].Row).ToDictionary(g=>g.Key,g=>g.ToList());foreach(var previous in owned){int row=previous.Value;if(KeepUnloadedBackupItems&&!groups.ContainsKey(row))continue;var target=targets.SingleOrDefault(t=>t.Row==row);if(target==null||previous.Key!=BackupMapping.BlockPrefix+target.SlotId||identities[previous.Key]!=BackupMapping.Identity(target))throw new Exception("Previously mapped BACKUP item was removed or changed; review its identity before writing.");if(!groups.ContainsKey(row)&&!KeepUnloadedBackupItems)groups[row]=new List<BackupDemand>();}
 if(groups.Count==0)return;VerifyQuantityTargets(targets,groups.Keys);var totals=groups.ToDictionary(g=>g.Key,g=>BackupMapping.FinalQuantity(g.Value,targets.Single(t=>t.Row==g.Key).Unit));var basisOnly=new HashSet<int>(groups.Where(g=>g.Value.Count>0&&BackupMapping.BasisOnly(targets.Single(t=>t.Row==g.Key),g.Value)).Select(g=>g.Key));if(totals.Values.Any(v=>double.IsNaN(v)||double.IsInfinity(v)||v<0))throw new Exception("Invalid final BACKUP quantity.");bool events=Convert.ToBoolean(app.EnableEvents),screen=Convert.ToBoolean(app.ScreenUpdating),changed=false;int? calculation=null;
 try{app.EnableEvents=false;app.ScreenUpdating=false;try{calculation=Convert.ToInt32(app.Calculation);app.Calculation=-4135;}catch{}dynamic sheet=FindEstimateSheet("BACKUP");foreach(int row in groups.Keys.OrderBy(x=>x)){var target=targets.Single(t=>t.Row==row);string identity=BackupMapping.Identity(target);double previous;bool unchanged=basisOnly.Contains(row)/* LS row without a manual LS quantity: keep the mapping, write nothing */||LastBackupTotals.TryGetValue(target.SlotId,out previous)&&previous==totals[row];if(!unchanged){double? current=BackupBill.ReadQuantity(sheet.Cells[row,55].Value2);if(!current.HasValue||current.Value!=totals[row]){try{sheet.Cells[row,55].Value2=totals[row];}catch(Exception ex){throw new Exception("Excel could not write BACKUP!BC"+row+" ("+target.Code+" "+target.Description+"). "+ex.Message,ex);}changed=true;}}SetBackupName(TotalPrefix+target.SlotId,row,identity);SetBackupName(BackupMapping.ItemPrefix+target.SlotId,row,identity);SetBackupName(BackupMapping.BlockPrefix+target.SlotId,row,identity);foreach(var demand in groups[row])SetBackupName(BackupMapping.Name(demand.Key),row,identity);}
 if(!detailsHidden){var hidden=targets.Where(t=>t.EndRow>=t.Row+1).Select(t=>"BC"+(t.Row+1)+":BC"+t.EndRow).ToList();foreach(string address in ExcelRangeBatches.Pack(hidden)){try{sheet.Range[address].EntireRow.Hidden=true;}catch(Exception ex){throw new Exception("Excel could not hide BACKUP detail rows at "+address+". Check worksheet protection and merged rows. "+ex.Message,ex);}}detailsHidden=true;}var wanted=new HashSet<string>(demands.Select(d=>BackupMapping.Name(d.Key)),StringComparer.OrdinalIgnoreCase);foreach(string name in saved.Keys.Where(n=>!KeepUnloadedBackupItems&&!wanted.Contains(n))){book.Names.Item(name).Delete();if(nameIdentityCache!=null)nameIdentityCache.Remove(name);ForgetBackupName(name);}if(changed){try{app.Calculate();}catch(Exception ex){throw new Exception("Excel could not recalculate the linked workbook after writing BACKUP quantities. "+ex.Message,ex);}}LastBackupRoutes=new Dictionary<string,BackupTarget>(routes);LastBackupTotals.Clear();foreach(var pair in totals)if(!basisOnly.Contains(pair.Key))LastBackupTotals[targets.Single(t=>t.Row==pair.Key).SlotId]=pair.Value;/* not saved: the owner saves the workbook in Excel (19 November 2026) */
 }finally{if(calculation.HasValue)try{app.Calculation=calculation.Value;}catch{}app.EnableEvents=events;app.ScreenUpdating=screen;}
}


void SetBackupName(string name,int row,string identity){if(nameIdentityCache==null)nameIdentityCache=new Dictionary<string,string>();string saved;if(nameIdentityCache.TryGetValue(name,out saved)&&saved==identity)return;try{dynamic old=book.Names.Item(name);if(Convert.ToString(old.Comment)==identity&&Convert.ToInt32(old.RefersToRange.Row)==row){nameIdentityCache[name]=identity;return;}}catch{}dynamic range;try{range=book.Names.Add(Name:name,RefersTo:"='BACKUP'!$BC$"+row,Visible:false);}catch(Exception ex){throw new Exception("Excel could not save mapping "+name+" to BACKUP!BC"+row+". Check workbook protection or read-only mode. "+ex.Message,ex);}range.Comment=identity;nameIdentityCache[name]=identity;RememberBackupName(name,row,identity);}


		private ExcelLiveLink()
		{
		}

		public static ExcelLiveLink AttachActiveWorkbook()
		{
			ExcelLiveLink excelLiveLink = new ExcelLiveLink();
			try
			{
				excelLiveLink.app = Marshal.GetActiveObject("Excel.Application");
				excelLiveLink.book = excelLiveLink.app.ActiveWorkbook;
				if (excelLiveLink.book == null)
				{
					throw new Exception("Open the target workbook in desktop Excel first.");
				}
				excelLiveLink.PathName = Convert.ToString(excelLiveLink.book.FullName);
				if (!Path.IsPathRooted(excelLiveLink.PathName) || !File.Exists(excelLiveLink.PathName))
				{
					throw new Exception("Save the open workbook first, then use Use open workbook.");
				}
				WorkbookFormat(excelLiveLink.PathName);
				if (Convert.ToBoolean(excelLiveLink.book.ReadOnly))
				{
					throw new Exception("The active workbook is read-only.");
				}
				if (!excelLiveLink.HasBackup)
				{
					throw new Exception("The active workbook must contain the BACKUP sheet. Activate the correct workbook in Excel.");
				}
				return excelLiveLink;
			}
			catch
			{
				excelLiveLink.Dispose();
				throw;
			}
		}
	}
}
