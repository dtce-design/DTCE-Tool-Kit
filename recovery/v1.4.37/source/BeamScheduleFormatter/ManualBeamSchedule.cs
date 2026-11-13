using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;

namespace BeamScheduleFormatter
{
	internal static class ManualBeamSchedule
	{
		public const string FileName = "Manual beam schedules";

		public static Analysis Read(string raw, string name)
		{
			if (!raw.TrimStart().StartsWith("<ManualBeamSchedules"))
			{
				return RevisedSchedule.Read(raw, name);
			}
			XmlDocument xmlDocument = new XmlDocument
			{
				XmlResolver = null
			};
			using (XmlReader reader = XmlReader.Create(new StringReader(raw), new XmlReaderSettings
			{
				DtdProcessing = DtdProcessing.Prohibit,
				XmlResolver = null,
				MaxCharactersInDocument = 1000000L
			}))
			{
				xmlDocument.Load(reader);
			}
			Analysis analysis = new Analysis
			{
				Floor = "Manual",
				Path = name
			};
			foreach (XmlElement item in xmlDocument.DocumentElement.SelectNodes("Beam"))
			{
				BeamRow r = new BeamRow
				{
					Mark = item.GetAttribute("mark")
				};
				for (int i = 0; i < 14; i++)
				{
					r.Cells[i].AddRange(from t in item.GetAttribute("c" + i).Split('+')
						select t.Trim());
				}
				r.Leg = item.GetAttribute("leg");
				r.Diameter = item.GetAttribute("dia");
				r.RevisedSpacing = (r.Spacing = item.GetAttribute("spacing"));
				Validate(r);
				if (analysis.Source.Any((BeamRow x) => x.Mark.Equals(r.Mark, StringComparison.OrdinalIgnoreCase)))
				{
					throw new Exception("Duplicate manual beam mark.");
				}
				analysis.Source.Add(r);
			}
			if (analysis.Source.Count == 0)
			{
				throw new Exception("Manual schedule is empty.");
			}
			analysis.BeamCount = analysis.Source.Count;
			return analysis;
		}

		public static void Validate(BeamRow r)
		{
			if (!Regex.IsMatch(r.Mark ?? "", "^[A-Z0-9][A-Z0-9_-]{0,39}$"))
			{
				throw new Exception("Use a unique beam mark (letters, digits, dash or underscore).");
			}
			double result;
			double result2;
			if (!double.TryParse(r.Cell(0), NumberStyles.Float, CultureInfo.InvariantCulture, out result) || !double.TryParse(r.Cell(1), NumberStyles.Float, CultureInfo.InvariantCulture, out result2) || double.IsNaN(result + result2) || double.IsInfinity(result + result2) || result <= 0.0 || result2 <= 0.0)
			{
				throw new Exception("Enter positive B and D in millimetres.");
			}
			for (int i = 2; i < 8; i++)
			{
				EstimatorEngine.Bars(r.Cells[i]);
			}
			EstimatorEngine.Bars(r.Cells[11]);
			if (!Enumerable.Range(2, 6).Any((int n) => EstimatorEngine.Bars(r.Cells[n]).Count > 0))
			{
				throw new Exception("Enter main reinforcement.");
			}
			EstimateOptions o = new EstimateOptions();
			RevisedSchedule.AddTies(new BeamEstimate(), r, r.Mark, 6.0, o, null, null);
		}

		public static string Serialize(Analysis a)
		{
			XmlDocument xmlDocument = new XmlDocument();
			XmlElement xmlElement = xmlDocument.CreateElement("ManualBeamSchedules");
			xmlDocument.AppendChild(xmlElement);
			foreach (BeamRow item in a.Source)
			{
				Validate(item);
				XmlElement xmlElement2 = xmlDocument.CreateElement("Beam");
				xmlElement2.SetAttribute("mark", item.Mark);
				for (int i = 0; i < 14; i++)
				{
					xmlElement2.SetAttribute("c" + i, string.Join(" + ", item.Cells[i]));
				}
				xmlElement2.SetAttribute("leg", item.Leg);
				xmlElement2.SetAttribute("dia", item.Diameter);
				xmlElement2.SetAttribute("spacing", item.RevisedSpacing);
				xmlElement.AppendChild(xmlElement2);
			}
			return xmlDocument.OuterXml;
		}

		public static BeamRow Choose(IWin32Window owner)
		{
			Form f = new Form
			{
				Text = "Add manual beam schedule",
				Width = 920,
				Height = 570,
				StartPosition = FormStartPosition.CenterParent,
				Font = new Font("Segoe UI", 10f)
			};
			try
			{
				Label value = new Label
				{
					Dock = DockStyle.Top,
					Height = 70,
					Padding = new Padding(10),
					Text = "For beams missing from the DXF. Dimensions are mm. Bars: 3-O16; use + for layers (3-O16 + 2-O20), - for none.\nStirrup spacing example: 1 - 50mm, 5 - 100mm, AND REST AT 200mm C/C. Check against the structural schedule."
				};
				DataGridView g = new DataGridView
				{
					Dock = DockStyle.Fill,
					AllowUserToAddRows = false,
					AllowUserToDeleteRows = false,
					RowHeadersVisible = false,
					AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
				};
				g.Columns.Add("Field", "Schedule field");
				g.Columns.Add("Value", "Value");
				g.Columns[0].ReadOnly = true;
				string[] array = new string[13]
				{
					"Beam mark", "B mm", "D mm", "Bottom LEFT", "Bottom MID", "Bottom RIGHT", "Top LEFT", "Top MID", "Top RIGHT", "SFR (EF if each face)",
					"Stirrup legs", "Stirrup diameter mm", "Stirrup spacing"
				};
				string[] array2 = new string[13]
				{
					"", "300", "500", "2-O16", "2-O16", "2-O16", "2-O16", "2-O16", "2-O16", "-",
					"2", "10", "1 - 50mm, AND REST AT 200mm C/C"
				};
				for (int i = 0; i < array.Length; i++)
				{
					g.Rows.Add(array[i], array2[i]);
				}
				FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
				{
					Dock = DockStyle.Bottom,
					Height = 48
				};
				Button button = new Button
				{
					Text = "Add schedule",
					AutoSize = true
				};
				flowLayoutPanel.Controls.Add(button);
				Button button2 = new Button
				{
					Text = "Cancel",
					DialogResult = DialogResult.Cancel,
					AutoSize = true
				};
				flowLayoutPanel.Controls.Add(button2);
				f.Controls.Add(g);
				f.Controls.Add(flowLayoutPanel);
				f.Controls.Add(value);
				f.CancelButton = button2;
				GridAppearance.Apply(g);
				BeamRow result = null;
				button.Click += delegate
				{
					try
					{
						g.EndEdit();
						Func<int, string> func = (int n) => Convert.ToString(g.Rows[n].Cells[1].Value).Trim();
						BeamRow beamRow = new BeamRow
						{
							Mark = func(0).ToUpperInvariant(),
							Leg = func(10) + "L",
							Diameter = "O" + func(11),
							RevisedSpacing = func(12)
						};
						beamRow.Spacing = beamRow.RevisedSpacing;
						for (int num = 0; num < 14; num++)
						{
							beamRow.Cells[num].Add("-");
						}
						beamRow.Cells[0][0] = func(1);
						beamRow.Cells[1][0] = func(2);
						for (int num2 = 2; num2 < 8; num2++)
						{
							beamRow.Cells[num2].Clear();
							beamRow.Cells[num2].AddRange(from t in func(num2 + 1).Split('+')
								select t.Trim());
						}
						beamRow.Cells[11][0] = func(9);
						beamRow.Cells[8][0] = beamRow.Leg;
						beamRow.Cells[9][0] = beamRow.Diameter;
						beamRow.Cells[10][0] = beamRow.Spacing;
						Validate(beamRow);
						result = beamRow;
						f.DialogResult = DialogResult.OK;
						f.Close();
					}
					catch (Exception ex)
					{
						MessageBox.Show(f, ex.Message, "Check schedule");
					}
				};
				return (f.ShowDialog(owner) == DialogResult.OK) ? result : null;
			}
			finally
			{
				if (f != null)
				{
					((IDisposable)f).Dispose();
				}
			}
		}
	}
}
