using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal class MainForm : Form
	{
		protected FlowLayoutPanel ActionPanel;

		private Color Navy = Color.FromArgb(20, 33, 51);

		private Color Accent = Color.FromArgb(0, 139, 139);

		private Color Muted = Color.FromArgb(91, 107, 125);

		private TextBox sourceBox;

		private TextBox prefixBox;

		private TextBox floorBox;

		private TextBox detail;

		private Button browse;

		private Button process;

		private Button export;

		private CheckBox merge;

		private DataGridView grid;

		private Label summary;

		private Label status;

		private PictureBox picture;

		private TabControl tabs;

		private Panel previewHost;

		private Analysis analysis;

		private Result result;

		private bool busy;

		public MainForm()
		{
			AppBrand.Apply(this);
			if (!AccessCodes.CanUseApp(1))
			{
				throw new Exception("Beam Formatter is not included in your current access.");
			}
			Text = "Beam Schedule Formatter v1.4.37";
			base.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
			base.Width = 1280;
			base.Height = 820;
			MinimumSize = new Size(1120, 700);
			base.StartPosition = FormStartPosition.CenterScreen;
			Font = new Font("Segoe UI", 9.5f);
			BackColor = Color.FromArgb(243, 246, 250);
			base.AutoScaleMode = AutoScaleMode.Dpi;
			AllowDrop = true;
			Panel panel = new Panel
			{
				Dock = DockStyle.Top,
				Height = 82,
				BackColor = Navy,
				Padding = new Padding(24, 12, 24, 10)
			};
			Label value = new Label
			{
				Text = "BEAM SCHEDULE FORMATTER  v1.4.37",
				Font = new Font("Segoe UI Semibold", 18f),
				ForeColor = Color.White,
				Dock = DockStyle.Top,
				Height = 34
			};
			Label value2 = new Label
			{
				Text = "Standard Drawing1 template  •  Developed by Engr. Daryl O. Tomaneng",
				ForeColor = Color.FromArgb(165, 190, 211),
				Dock = DockStyle.Bottom,
				Height = 22
			};
			panel.Controls.Add(value);
			panel.Controls.Add(value2);
			status = new Label
			{
				Dock = DockStyle.Bottom,
				Height = 34,
				Padding = new Padding(18, 8, 8, 0),
				BackColor = Color.White,
				ForeColor = Muted,
				Text = "Ready. Piliin o i-drop ang original beam schedule DXF."
			};
			Panel panel2 = new Panel
			{
				Dock = DockStyle.Left,
				Width = 246,
				Padding = new Padding(12),
				BackColor = Color.White
			};
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				FlowDirection = FlowDirection.TopDown,
				WrapContents = false,
				AutoScroll = true
			};
			panel2.Controls.Add(flowLayoutPanel);
			AddLabel(flowLayoutPanel, "01   SOURCE DRAWING", true);
			AddLabel(flowLayoutPanel, "Original beam schedule DXF");
			sourceBox = new TextBox
			{
				Width = 194,
				ReadOnly = true,
				BackColor = Color.FromArgb(245, 248, 251),
				Height = 30
			};
			flowLayoutPanel.Controls.Add(sourceBox);
			browse = ButtonOf("Choose DXF…", 194, false);
			browse.Click += delegate
			{
				Choose();
			};
			flowLayoutPanel.Controls.Add(browse);
			AddLabel(flowLayoutPanel, "\n02   STANDARD TEMPLATE", true);
			AddLabel(flowLayoutPanel, "Schedule of beams  ·  built-in\nArial / Arial Bold\nText: 2.0  ·  Headers: 2.5\nRow height: 8 units\nOriginal layers included", false, 96);
			AddLabel(flowLayoutPanel, "03   BEAM MARKS", true);
			AddLabel(flowLayoutPanel, "Prefix (B / GB / RD / URFB)");
			prefixBox = new TextBox
			{
				Width = 194,
				Text = "4B",
				CharacterCasing = CharacterCasing.Upper
			};
			flowLayoutPanel.Controls.Add(prefixBox);
			AddLabel(flowLayoutPanel, "Floor title");
			floorBox = new TextBox
			{
				Width = 194,
				Text = "FOURTH FLOOR",
				CharacterCasing = CharacterCasing.Upper
			};
			flowLayoutPanel.Controls.Add(floorBox);
			prefixBox.Leave += delegate
			{
				if (Engine.ValidPrefix(prefixBox.Text))
				{
					floorBox.Text = Engine.FloorFor(prefixBox.Text);
				}
			};
			merge = new CheckBox
			{
				Text = "Merge identical rows",
				Checked = true,
				Width = 194,
				Height = 34,
				Margin = new Padding(0, 12, 0, 3)
			};
			flowLayoutPanel.Controls.Add(merge);
			AddLabel(flowLayoutPanel, "Beam marks are excluded from comparison.\nOriginal beam numbers stay beside each group.", false, 58);
			FlowLayoutPanel flowLayoutPanel2 = (ActionPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Bottom,
				Height = 112,
				FlowDirection = FlowDirection.TopDown,
				WrapContents = false,
				Padding = new Padding(0, 4, 0, 0)
			});
			process = ButtonOf("Build preview", 194, true);
			process.Enabled = false;
			process.Click += async delegate
			{
				await BuildAsync();
			};
			flowLayoutPanel2.Controls.Add(process);
			export = ButtonOf("Export revised DXF", 194, false);
			export.Enabled = false;
			export.Click += delegate
			{
				ExportFile();
			};
			flowLayoutPanel2.Controls.Add(export);
			panel2.Controls.Add(flowLayoutPanel2);
			Panel panel3 = new Panel
			{
				Dock = DockStyle.Fill,
				Padding = new Padding(18)
			};
			summary = new Label
			{
				Dock = DockStyle.Top,
				Height = 66,
				Font = new Font("Segoe UI Semibold", 12f),
				ForeColor = Navy,
				Text = "Your revised schedule\nChoose a DXF to start.",
				Padding = new Padding(3, 6, 0, 0)
			};
			tabs = new TabControl
			{
				Dock = DockStyle.Fill
			};
			TabPage tabPage = new TabPage("Schedule values")
			{
				BackColor = Color.White
			};
			TabPage tabPage2 = new TabPage("CAD layout preview")
			{
				BackColor = Color.FromArgb(33, 40, 46)
			};
			grid = new DataGridView
			{
				Dock = DockStyle.Fill,
				ReadOnly = true,
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = false,
				AllowUserToResizeRows = false,
				RowHeadersVisible = false,
				SelectionMode = DataGridViewSelectionMode.CellSelect,
				MultiSelect = false,
				BackgroundColor = Color.White,
				BorderStyle = BorderStyle.None,
				AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
				EnableHeadersVisualStyles = false,
				ColumnHeadersHeight = 42
			};
			grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 240, 239);
			grid.DefaultCellStyle.SelectionForeColor = Navy;
			grid.DefaultCellStyle.Padding = new Padding(5);
			GridAppearance.Apply(grid);
			grid.ColumnHeadersDefaultCellStyle.BackColor = Navy;
			grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
			grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
			grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 249, 252);
			grid.RowTemplate.Height = 36;
			string[] array = new string[14]
			{
				"Mark", "B", "D", "Bottom L", "Bottom M", "Bottom R", "Top L", "Top M", "Top R", "Leg",
				"Ø", "Spacing", "SFR", "Original beam numbers"
			};
			int[] array2 = new int[14]
			{
				85, 48, 48, 100, 100, 100, 100, 100, 100, 50,
				50, 330, 90, 280
			};
			for (int num = 0; num < array.Length; num++)
			{
				grid.Columns.Add(new DataGridViewTextBoxColumn
				{
					Name = "C" + num,
					HeaderText = array[num],
					Width = array2[num],
					SortMode = DataGridViewColumnSortMode.NotSortable,
					Frozen = (num < 3)
				});
			}
			grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
			grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
			grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
			for (int num2 = 0; num2 <= 12; num2++)
			{
				grid.Columns[num2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
				grid.Columns[num2].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
			}
			grid.Columns[13].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
			grid.Columns[13].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
			for (int num3 = 3; num3 <= 8; num3++)
			{
				grid.Columns[num3].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
				grid.Columns[num3].MinimumWidth = 78;
				grid.Columns[num3].Width = 88;
			}
			grid.Columns[11].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
			grid.Columns[11].Width = 460;
			grid.Columns[11].MinimumWidth = 360;
			grid.Columns[13].Width = 194;
			grid.RowTemplate.Height = 56;
			grid.RowTemplate.MinimumHeight = 56;
			grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
			grid.CellPainting += PaintReinforcement;
			grid.ColumnHeadersHeight = 64;
			grid.Font = new Font("Arial", 9.5f);
			grid.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 9f, FontStyle.Bold);
			grid.Scroll += delegate
			{
				grid.Invalidate();
			};
			grid.ColumnWidthChanged += delegate
			{
				grid.Invalidate();
			};
			detail = new TextBox
			{
				Dock = DockStyle.Bottom,
				Multiline = true,
				ReadOnly = true,
				Height = 210,
				BorderStyle = BorderStyle.None,
				BackColor = Color.FromArgb(237, 243, 248),
				Font = new Font("Consolas", 9.5f),
				Text = "Select a row to review original stirrups and output notes.",
				ScrollBars = ScrollBars.Both,
				WordWrap = false
			};
			tabPage.Controls.Add(grid);
			tabPage.Controls.Add(detail);
			grid.SelectionChanged += delegate
			{
				ShowDetails();
			};
			grid.CurrentCellChanged += delegate
			{
				ShowDetails();
			};
			base.Shown += delegate
			{
				ShowDetails();
			};
			previewHost = new Panel
			{
				Dock = DockStyle.Fill,
				AutoScroll = true,
				BackColor = Color.FromArgb(33, 40, 46)
			};
			picture = new PictureBox
			{
				SizeMode = PictureBoxSizeMode.AutoSize,
				Location = new Point(10, 10)
			};
			previewHost.Controls.Add(picture);
			tabPage2.Controls.Add(previewHost);
			tabs.TabPages.Add(tabPage);
			tabs.TabPages.Add(tabPage2);
			panel3.Controls.Add(tabs);
			panel3.Controls.Add(summary);
			base.Controls.Add(panel3);
			base.Controls.Add(panel2);
			base.Controls.Add(status);
			base.Controls.Add(panel);
			base.DragEnter += delegate(object o, DragEventArgs e)
			{
				if (e.Data.GetDataPresent(DataFormats.FileDrop))
				{
					e.Effect = DragDropEffects.Copy;
				}
			};
			base.DragDrop += async delegate(object o, DragEventArgs e)
			{
				string[] array3 = (string[])e.Data.GetData(DataFormats.FileDrop);
				if (array3.Length != 0 && !busy)
				{
					await LoadFile(array3[0]);
				}
			};
			prefixBox.TextChanged += delegate
			{
				InvalidatePreview();
			};
			floorBox.TextChanged += delegate
			{
				InvalidatePreview();
			};
			merge.CheckedChanged += delegate
			{
				InvalidatePreview();
			};
		}

		private void AddLabel(FlowLayoutPanel f, string t, bool strong = false, int height = 0)
		{
			Label label = new Label();
			label.Text = t.TrimStart('\n');
			label.Width = 194;
			label.AutoSize = false;
			label.Height = ((height > 0) ? height : (strong ? 26 : 23));
			label.ForeColor = (strong ? Navy : Muted);
			label.Font = new Font("Segoe UI", strong ? 9.2f : 9f, strong ? FontStyle.Bold : FontStyle.Regular);
			label.Margin = new Padding(0, strong ? 9 : 2, 0, strong ? 4 : 2);
			Label value = label;
			f.Controls.Add(value);
		}

		private void PaintReinforcement(object sender, DataGridViewCellPaintingEventArgs e)
		{
			if (e.RowIndex == -1 && e.ColumnIndex >= 0)
			{
				e.Paint(e.ClipBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);
				Rectangle cellBounds = e.CellBounds;
				if (e.ColumnIndex >= 1 && e.ColumnIndex <= 11)
				{
					cellBounds.Y += 32;
					cellBounds.Height -= 32;
					int num = ((e.ColumnIndex <= 2) ? 1 : ((e.ColumnIndex <= 5) ? 3 : ((e.ColumnIndex <= 8) ? 6 : 9)));
					int num2 = ((num == 1) ? 2 : (num + 2));
					object obj;
					switch (num)
					{
					default:
						obj = "SHEAR REINFORCEMENTS";
						break;
					case 6:
						obj = "TOP REINFORCEMENT";
						break;
					case 3:
						obj = "BOTTOM REINFORCEMENT";
						break;
					case 1:
						obj = "SIZE";
						break;
					}
					string s = (string)obj;
					int num3 = e.CellBounds.Left;
					int num4 = 0;
					for (int i = num; i <= num2; i++)
					{
						num4 += grid.Columns[i].Width;
						if (i < e.ColumnIndex)
						{
							num3 -= grid.Columns[i].Width;
						}
					}
					Rectangle rectangle = new Rectangle(num3, e.CellBounds.Top, num4, 32);
					Rectangle clip = new Rectangle(e.CellBounds.Left, e.CellBounds.Top, e.CellBounds.Width, 32);
					GraphicsState gstate = e.Graphics.Save();
					e.Graphics.SetClip(clip);
					using (SolidBrush brush = new SolidBrush(Navy))
					{
						e.Graphics.FillRectangle(brush, rectangle);
					}
					using (Pen pen = new Pen(Color.FromArgb(210, 220, 230)))
					{
						e.Graphics.DrawRectangle(pen, rectangle.Left, rectangle.Top, rectangle.Width - 1, rectangle.Height - 1);
					}
					using (StringFormat format = new StringFormat
					{
						Alignment = StringAlignment.Center,
						LineAlignment = StringAlignment.Center
					})
					{
						using (SolidBrush brush2 = new SolidBrush(Color.White))
						{
							e.Graphics.DrawString(s, e.CellStyle.Font, brush2, rectangle, format);
						}
					}
					e.Graphics.Restore(gstate);
				}
				string[] array = new string[14]
				{
					"BEAM MARK", "B", "D", "LEFT", "MIDSPAN", "RIGHT", "LEFT", "MIDSPAN", "RIGHT", "LEG",
					"Ø", "SPACING", "SFR", "ORIGINAL BEAM NUMBERS"
				};
				TextRenderer.DrawText(e.Graphics, array[e.ColumnIndex], e.CellStyle.Font, cellBounds, PowTheme.Ink, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
				e.Handled = true;
			}
			else
			{
				if (e.RowIndex < 0 || e.ColumnIndex < 3 || e.ColumnIndex > 8 || result == null || e.RowIndex >= result.Rows.Count)
				{
					return;
				}
				e.Paint(e.ClipBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border | DataGridViewPaintParts.SelectionBackground);
				List<string> list = result.Rows[e.RowIndex].Cells[e.ColumnIndex - 1];
				Color foreColor = (((e.State & DataGridViewElementStates.Selected) != DataGridViewElementStates.None) ? e.CellStyle.SelectionForeColor : e.CellStyle.ForeColor);
				using (Font font = new Font("Arial", 9.5f))
				{
					float num5 = (float)Math.Ceiling(font.GetHeight(e.Graphics));
					float num6 = 2f;
					float num7 = num5 + num6;
					int num8 = Math.Max(2, result.Rows[e.RowIndex].Cells.Skip(2).Take(6).Max((List<string> c) => c.Count));
					float num9 = (float)e.CellBounds.Top + ((float)e.CellBounds.Height - ((float)num8 * num5 + (float)(num8 - 1) * num6)) / 2f;
					TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding;
					for (int num10 = 0; num10 < list.Count; num10++)
					{
						Rectangle bounds = new Rectangle(e.CellBounds.Left + 2, (int)Math.Round(num9 + (float)num10 * num7), e.CellBounds.Width - 4, (int)num5);
						TextRenderer.DrawText(e.Graphics, Engine.Format(list[num10]).Replace("%%C", "Ø"), font, bounds, foreColor, flags);
						if (num10 > 0)
						{
							float num11 = (float)e.CellBounds.Left + (float)e.CellBounds.Width / 2f - (float)(0.2996583526125 * (double)e.CellBounds.Width) - 4f;
							TextRenderer.DrawText(bounds: new Rectangle((int)(num11 - 8f), (int)Math.Round(num9 + ((float)num10 - 0.5f) * num7), 16, (int)num5), dc: e.Graphics, text: "+", font: font, foreColor: foreColor, flags: flags);
						}
					}
				}
				e.Handled = true;
			}
		}

		private void PaintHeaderGroups(object sender, PaintEventArgs e)
		{
			int[][] array = new int[4][]
			{
				new int[2] { 1, 2 },
				new int[2] { 3, 5 },
				new int[2] { 6, 8 },
				new int[2] { 9, 11 }
			};
			string[] array2 = new string[4] { "SIZE", "BOTTOM REINFORCEMENT", "TOP REINFORCEMENT", "SHEAR REINFORCEMENTS" };
			int right = grid.GetCellDisplayRectangle(2, -1, false).Right;
			for (int i = 0; i < array.Length; i++)
			{
				Rectangle cellDisplayRectangle = grid.GetCellDisplayRectangle(array[i][0], -1, false);
				Rectangle cellDisplayRectangle2 = grid.GetCellDisplayRectangle(array[i][1], -1, false);
				Rectangle rectangle = Rectangle.FromLTRB(cellDisplayRectangle.Left, cellDisplayRectangle.Top, cellDisplayRectangle2.Right, cellDisplayRectangle.Top + 32);
				Rectangle clip = Rectangle.Intersect(rectangle, new Rectangle((i == 0) ? 1 : right, 0, grid.ClientSize.Width - ((i == 0) ? 1 : right) - 1, grid.ColumnHeadersHeight));
				if (clip.Width > 0)
				{
					GraphicsState gstate = e.Graphics.Save();
					e.Graphics.SetClip(clip);
					using (SolidBrush brush = new SolidBrush(Navy))
					{
						e.Graphics.FillRectangle(brush, rectangle);
					}
					using (Pen pen = new Pen(Color.FromArgb(210, 220, 230)))
					{
						e.Graphics.DrawRectangle(pen, rectangle.Left, rectangle.Top, rectangle.Width - 1, rectangle.Height - 1);
					}
					TextRenderer.DrawText(e.Graphics, array2[i], grid.ColumnHeadersDefaultCellStyle.Font, rectangle, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
					e.Graphics.Restore(gstate);
				}
			}
		}

		private Button ButtonOf(string text, int w, bool primary)
		{
			return new Button
			{
				Text = text,
				Width = w,
				Height = 40,
				FlatStyle = FlatStyle.Flat,
				BackColor = (primary ? Accent : Color.FromArgb(233, 241, 246)),
				ForeColor = (primary ? Color.White : Navy),
				Cursor = Cursors.Hand,
				Margin = new Padding(0, 5, 0, 4)
			};
		}

		private void InvalidatePreview()
		{
			if (result != null)
			{
				result = null;
				export.Enabled = false;
				status.Text = "May nabagong settings. I-click ang Build preview para i-update.";
			}
		}

		private async void Choose()
		{
			using (OpenFileDialog dialog = new OpenFileDialog
			{
				Title = "Choose original beam schedule DXF",
				Filter = "AutoCAD DXF (*.dxf)|*.dxf",
				CheckFileExists = true
			})
			{
				if (dialog.ShowDialog(this) == DialogResult.OK)
				{
					await LoadFile(dialog.FileName);
				}
			}
		}

		private void SetBusy(bool value, string message)
		{
			busy = value;
			base.UseWaitCursor = value;
			browse.Enabled = !value;
			process.Enabled = !value && analysis != null;
			export.Enabled = !value && result != null;
			prefixBox.Enabled = !value;
			floorBox.Enabled = !value;
			merge.Enabled = !value;
			status.Text = message;
		}

		private async Task LoadFile(string path)
		{
			this.analysis = null;
			result = null;
			grid.Rows.Clear();
			sourceBox.Text = "";
			summary.Text = "Loading source DXF…";
			SetBusy(true, "Binabasa ang source DXF…");
			try
			{
				Analysis analysis = (this.analysis = await Task.Run(() => Engine.Analyze(path)));
				sourceBox.Text = Path.GetFileName(path);
				new ToolTip().SetToolTip(sourceBox, path);
				prefixBox.Text = analysis.Prefix;
				floorBox.Text = analysis.Floor;
				summary.Text = analysis.Source.Count + " source rows  ·  " + analysis.BeamCount + " original beam numbers\nReady for standard formatting.";
				status.Text = "Loaded. Automatic preview…";
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.Message, "Hindi mabasa ang source", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				summary.Text = "Walang source DXF na naka-load.";
				status.Text = "Hindi na-load ang file. " + ex.Message;
			}
			finally
			{
				SetBusy(false, status.Text);
			}
			if (this.analysis != null)
			{
				await BuildAsync();
			}
		}

		private async Task BuildAsync()
		{
			if (analysis == null)
			{
				return;
			}
			Settings settings = new Settings
			{
				Prefix = prefixBox.Text.Trim(),
				Floor = floorBox.Text.Trim(),
				Merge = merge.Checked
			};
			SetBusy(true, "Inaayos ang table at kino-compare ang rows…");
			try
			{
				Result r = await Task.Run(() => Engine.Build(analysis, settings));
				Bitmap image = await Task.Run(() => Preview.Render(r.Drawing));
				result = r;
				FillGrid();
				Image image2 = picture.Image;
				picture.Image = image;
				if (image2 != null)
				{
					image2.Dispose();
				}
				int num = r.SourceCount - r.Rows.Count;
				summary.Text = r.SourceCount + " source rows  →  " + r.Rows.Count + " final groups  ·  " + r.BeamCount + " beam numbers\n" + num + " duplicate rows consolidated  ·  standard template applied";
				status.Text = "Preview ready. I-review ang values o CAD layout, saka Export revised DXF.";
			}
			catch (Exception ex)
			{
				result = null;
				MessageBox.Show(this, ex.Message, "Hindi makagawa ng preview", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				status.Text = "Hindi natuloy ang formatting: " + ex.Message;
			}
			finally
			{
				SetBusy(false, status.Text);
			}
		}

		private void FillGrid()
		{
			grid.Rows.Clear();
			if (result == null)
			{
				return;
			}
			foreach (BeamRow row in result.Rows)
			{
				List<object> list = new List<object>
				{
					row.Mark,
					row.Cell(0),
					row.Cell(1)
				};
				for (int i = 2; i < 8; i++)
				{
					list.Add(string.Join("\r\n+ ", row.Cells[i].Select(Engine.Format)).Replace("%%C", "Ø"));
				}
				list.Add(row.Leg);
				list.Add(row.Diameter.Replace("%%C", "Ø"));
				list.Add(row.Spacing);
				list.Add(Engine.Format(row.Cell(11)).Replace("%%C", "Ø"));
				list.Add(string.Join(", ", row.Numbers));
				grid.Rows.Add(list.ToArray());
			}
			grid.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders);
			ShowDetails();
		}

		private static string OriginalStirrupDisplay(string raw)
		{
			if (string.IsNullOrWhiteSpace(raw) || raw == "-" || raw == "--")
			{
				return "-- (blank in source)";
			}
			Match match = Regex.Match(raw, "^(\\d+)-(\\d+)L-(?:#|%%[Cc]|O|\\u00D8)(\\d+)@(\\d+)\\s*C/C$", RegexOptions.IgnoreCase);
			if (match.Success)
			{
				return match.Groups[1].Value + " pcs | " + match.Groups[2].Value + "L | Ø" + match.Groups[3].Value + " @ " + match.Groups[4].Value + " mm C/C";
			}
			return raw.Replace("%%C", "Ø").Replace("#", "Ø");
		}

		private void ShowDetails()
		{
			if (detail == null)
			{
				return;
			}
			if (result == null || grid.CurrentRow == null || grid.CurrentRow.Index >= result.Rows.Count)
			{
				detail.Text = "Select a row to review original stirrups and output notes.";
				return;
			}
			BeamRow beamRow = result.Rows[grid.CurrentRow.Index];
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(beamRow.Mark + " | Original beams: " + string.Join(", ", beamRow.Numbers));
			stringBuilder.AppendLine("Source rows: " + string.Join(", ", beamRow.SourceRows) + ((beamRow.SourceRows.Count > 1) ? " [MERGED]" : ""));
			for (int i = 0; i < beamRow.SourceRows.Count; i++)
			{
				int rowNumber = beamRow.SourceRows[i];
				BeamRow beamRow2 = analysis.Source.FirstOrDefault((BeamRow row) => row.SourceRows.Contains(rowNumber));
				stringBuilder.AppendLine();
				stringBuilder.AppendLine("SOURCE ROW " + rowNumber + " | " + ((beamRow2 == null) ? string.Join(", ", beamRow.Numbers) : string.Join(", ", beamRow2.Numbers)));
				stringBuilder.AppendLine(string.Format("{0,-12}{1,-9}{2,-12}{3,-12}{4}", "POSITION", "LEG", "BAR DIA.", "NO. (pcs)", "SPACING (mm)"));
				string[] array = new string[3] { "LEFT", "MIDSPAN", "RIGHT" };
				for (int num = 0; num < 3; num++)
				{
					string text = beamRow.OriginalShear[i][num];
					Match match = Regex.Match(text, "^(\\d+)-(\\d+)L-(?:#|%%[Cc]|O|\\u00D8)(\\d+)@(\\d+)\\s*C/C$", RegexOptions.IgnoreCase);
					stringBuilder.AppendLine(string.Format("{0,-12}{1,-9}{2,-12}{3,-12}{4}", array[num], match.Success ? (match.Groups[2].Value + "L") : "--", match.Success ? ("Ø" + match.Groups[3].Value) : "--", match.Success ? match.Groups[1].Value : "--", match.Success ? (match.Groups[4].Value + " C/C") : OriginalStirrupDisplay(text)));
				}
			}
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("OUTPUT: " + beamRow.Leg + " / " + beamRow.Diameter.Replace("%%C", "Ø") + " | " + beamRow.Spacing);
			if (beamRow.Notes.Count == 0)
			{
				stringBuilder.AppendLine("NOTES: No spacing adjustment required.");
			}
			else
			{
				foreach (string note in beamRow.Notes)
				{
					stringBuilder.AppendLine("NOTE: " + note);
				}
			}
			if (beamRow.Cantilever)
			{
				stringBuilder.AppendLine("CANTILEVER: Spacing applies TO FREE END.");
			}
			detail.Text = stringBuilder.ToString();
		}

		private void ExportFile()
		{
			if (!AccessCodes.CanUseApp(1))
			{
				MessageBox.Show("Access code required.");
			}
			else
			{
				if (result == null)
				{
					return;
				}
				using (SaveFileDialog saveFileDialog = new SaveFileDialog
				{
					Title = "Export revised beam schedule",
					Filter = "AutoCAD DXF (*.dxf)|*.dxf",
					FileName = prefixBox.Text + " Schedule - Revised.dxf",
					OverwritePrompt = true
				})
				{
					if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
					{
						return;
					}
					if (!AccessCodes.CanUseApp(1))
					{
						MessageBox.Show("Access expired. Renew access before exporting.");
						return;
					}
					try
					{
						string fullPath = Path.GetFullPath(saveFileDialog.FileName);
						if (string.Equals(fullPath, Path.GetFullPath(analysis.Path), StringComparison.OrdinalIgnoreCase))
						{
							throw new Exception("Pumili ng bagong filename. Pinapanatili ng app ang original source DXF.");
						}
						Engine.Export(result, fullPath);
						status.Text = "Saved: " + fullPath;
						MessageBox.Show(this, "Na-save ang revised DXF.\n\n" + result.Rows.Count + " final groups · " + result.BeamCount + " original beam numbers\n\n" + fullPath, "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					}
					catch (Exception ex)
					{
						MessageBox.Show(this, ex.Message, "Hindi na-export", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					}
				}
			}
		}

		private void CreateTree(Control c)
		{
			typeof(Control).GetMethod("CreateControl", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[1] { typeof(bool) }, null).Invoke(c, new object[1] { true });
			c.PerformLayout();
			foreach (Control control in c.Controls)
			{
				CreateTree(control);
			}
		}

		public async Task TestLoad(string path)
		{
			await LoadFile(path);
			await BuildAsync();
		}

		public void RenderForTest(string path, string imagePath)
		{
			analysis = Engine.Analyze(path);
			sourceBox.Text = Path.GetFileName(path);
			prefixBox.Text = analysis.Prefix;
			floorBox.Text = analysis.Floor;
			result = Engine.Build(analysis, new Settings
			{
				Prefix = analysis.Prefix,
				Floor = analysis.Floor,
				Merge = true
			});
			FillGrid();
			summary.Text = result.SourceCount + " source rows  →  " + result.Rows.Count + " final groups  ·  " + result.BeamCount + " beam numbers\n" + (result.SourceCount - result.Rows.Count) + " duplicate rows consolidated  ·  standard template applied";
			process.Enabled = true;
			export.Enabled = true;
			status.Text = "Preview ready. I-review ang values o CAD layout, saka Export revised DXF.";
			typeof(Control).GetMethod("CreateControl", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[1] { typeof(bool) }, null).Invoke(this, new object[1] { true });
			PerformLayout();
			foreach (Control control in base.Controls)
			{
				CreateTree(control);
			}
			using (Bitmap bitmap = new Bitmap(base.Width, base.Height))
			{
				DrawToBitmap(bitmap, new Rectangle(0, 0, base.Width, base.Height));
				bitmap.Save(imagePath);
			}
		}
	}
}
