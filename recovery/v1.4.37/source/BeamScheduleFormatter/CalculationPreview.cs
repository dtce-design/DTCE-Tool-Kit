using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal static class CalculationPreview
	{
		public static PrintDocument Document(string report)
		{
			List<List<string>> pages = EstimatePdf.Pages(report);
			int page = 0;
			PrintDocument printDocument = new PrintDocument();
			printDocument.DocumentName = EstimatePdf.Heading(report);
			printDocument.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
			printDocument.DefaultPageSettings.Landscape = false;
			printDocument.BeginPrint += delegate
			{
				page = 0;
			};
			printDocument.PrintPage += delegate(object s, PrintPageEventArgs e)
			{
				DrawPage(e.Graphics, pages[page].ToArray(), page + 1, pages.Count, EstimatePdf.Heading(report));
				page++;
				e.HasMorePages = page < pages.Count;
			};
			return printDocument;
		}

		public static void Show(IWin32Window owner, string report)
		{
			Form f = new Form
			{
				Text = "Print Preview - continuous scrolling",
				Width = 1120,
				Height = 850,
				MinimumSize = new Size(800, 600),
				StartPosition = FormStartPosition.CenterParent
			};
			try
			{
				PrintDocument doc = Document(report);
				try
				{
					ContinuousPreview view = new ContinuousPreview(report)
					{
						Dock = DockStyle.Fill
					};
					FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
					{
						Dock = DockStyle.Top,
						Height = 46,
						Padding = new Padding(8),
						BackColor = Color.White
					};
					Button button = new Button
					{
						Text = "Print...",
						Width = 90,
						Height = 28
					};
					button.Click += delegate
					{
						try
						{
							using (PrintDialog printDialog = new PrintDialog
							{
								Document = doc,
								UseEXDialog = true
							})
							{
								if (printDialog.ShowDialog(f) == DialogResult.OK)
								{
									doc.Print();
								}
							}
						}
						catch (Exception ex)
						{
							MessageBox.Show(f, ex.Message, "Print calculation sheet");
						}
					};
					flowLayoutPanel.Controls.Add(button);
					flowLayoutPanel.Controls.Add(new Label
					{
						Text = "Page",
						AutoSize = true,
						Padding = new Padding(4, 6, 0, 0)
					});
					NumericUpDown number = new NumericUpDown
					{
						Minimum = 1m,
						Maximum = view.PageCount,
						Width = 60
					};
					flowLayoutPanel.Controls.Add(number);
					flowLayoutPanel.Controls.Add(new Label
					{
						Text = " / " + view.PageCount + " - scroll through all pages",
						AutoSize = true,
						Padding = new Padding(0, 6, 8, 0)
					});
					ComboBox zoom = new ComboBox
					{
						DropDownStyle = ComboBoxStyle.DropDownList,
						Width = 110
					};
					zoom.Items.AddRange(new object[8] { "Fit width", "50%", "75%", "100%", "125%", "150%", "200%", "300%" });
					zoom.SelectedIndex = 0;
					zoom.SelectedIndexChanged += delegate
					{
						view.SetZoom((zoom.SelectedIndex == 0) ? 0f : (new float[8] { 0f, 0.5f, 0.75f, 1f, 1.25f, 1.5f, 2f, 3f })[zoom.SelectedIndex]);
					};
					flowLayoutPanel.Controls.Add(zoom);
					bool updating = false;
					number.ValueChanged += delegate
					{
						if (!updating)
						{
							view.Jump((int)number.Value);
						}
					};
					view.PageChanged += delegate(int page)
					{
						updating = true;
						number.Value = page;
						updating = false;
					};
					Button button2 = new Button
					{
						Text = "Close",
						Width = 75,
						Height = 28
					};
					button2.Click += delegate
					{
						f.Close();
					};
					flowLayoutPanel.Controls.Add(button2);
					f.Controls.Add(view);
					f.Controls.Add(flowLayoutPanel);
					f.Shown += delegate
					{
						view.Focus();
					};
					f.ShowDialog(owner);
				}
				finally
				{
					if (doc != null)
					{
						((IDisposable)doc).Dispose();
					}
				}
			}
			finally
			{
				if (f != null)
				{
					((IDisposable)f).Dispose();
				}
			}
		}

		public static void DrawPage(Graphics g, string[] lines, int page, int total, string heading = null)
		{
			GraphicsState gstate = g.Save();
			try
			{
				g.PageUnit = GraphicsUnit.Point;
				g.FillRectangle(Brushes.White, 0f, 0f, 595.28f, 841.89f);
				PdfBrand.Draw(g);
				g.DrawLine(Pens.Gray, 72f, 119.89f, 559.28f, 119.89f);
				Text(g, heading ?? "SPLICE, DEVELOPMENT & HOOKS | CALCULATION SHEET", 74f, 30f, 10f, Color.FromArgb(31, 46, 64));
				Text(g, "DTCE Tool Kit v1.4.37 | SI units | Print-only review copy", 74f, 51f, 8f, Color.SlateGray);
				ProjectInformation projectInformation = ProjectInfoStore.Read();
				Text(g, projectInformation.NameLine, 74f, 68f, 8f, Color.FromArgb(31, 46, 64));
				Text(g, projectInformation.LocationLine, 74f, 82f, 8f, Color.FromArgb(31, 46, 64));
				float num = 125f;
				foreach (string text in lines)
				{
					bool flag = text.StartsWith("## ");
					if (flag)
					{
						using (SolidBrush brush = new SolidBrush(Color.FromArgb(230, 240, 250)))
						{
							g.FillRectangle(brush, 72f, num - 1f, 487.28f, 13f);
						}
						g.FillRectangle(Brushes.SteelBlue, 72f, num - 1f, 2f, 13f);
					}
					Text(g, flag ? text.Substring(3) : text, 74f, num, 10f, Color.FromArgb(31, 46, 64));
					num += 12f;
				}
				g.DrawLine(Pens.LightSlateGray, 72f, 807f, 559.28f, 807f);
				Text(g, "Generated " + DateTime.Now.ToString("yyyy-MM-dd") + " | Lengths in mm unless noted", 74f, 811f, 8f, Color.SlateGray);
				string text2 = "Page " + page + " / " + total;
				Text(g, text2, (float)(559.28 - (double)text2.Length * ReportFont.Width / 1000.0 * 8.0 * 0.85), 811f, 8f, Color.SlateGray);
			}
			finally
			{
				g.Restore(gstate);
			}
		}

		private static void Text(Graphics g, string text, float x, float y, float size, Color color)
		{
			GraphicsState gstate = g.Save();
			try
			{
				g.TranslateTransform(x, y);
				g.ScaleTransform(0.85f, 1f);
				using (Font font = new Font(ReportFont.Family, size, FontStyle.Regular, GraphicsUnit.Point))
				{
					using (SolidBrush brush = new SolidBrush(color))
					{
						g.DrawString(text, font, brush, 0f, 0f, StringFormat.GenericTypographic);
					}
				}
			}
			finally
			{
				g.Restore(gstate);
			}
		}
	}
}
