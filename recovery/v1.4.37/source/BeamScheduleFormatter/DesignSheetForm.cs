using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using System.Xml;

namespace BeamScheduleFormatter
{
	internal abstract class DesignSheetForm : Form
	{
		protected readonly TabControl Pages = new TabControl
		{
			Dock = DockStyle.Fill
		};

		protected readonly Dictionary<string, Control> Inputs = new Dictionary<string, Control>();

		protected bool Restoring;

		private readonly string kind;

		private readonly TextBox report = new TextBox
		{
			Dock = DockStyle.Fill,
			Multiline = true,
			ReadOnly = true,
			ScrollBars = ScrollBars.Both,
			WordWrap = false,
			Font = new Font("Consolas", 10f)
		};

		private readonly Label status = new Label
		{
			Dock = DockStyle.Bottom,
			Height = 40,
			Padding = new Padding(10)
		};

		protected DesignSheetForm(string title, string type)
		{
			if (!AccessCodes.CanUseApp((type == "Purlin-Design") ? 16 : 32))
			{
				throw new Exception("Design Sheets access required.");
			}
			kind = type;
			AppBrand.Apply(this);
			Text = title;
			base.ClientSize = new Size(1180, 780);
			MinimumSize = new Size(820, 600);
			Font = new Font("Segoe UI", 10f);
			base.StartPosition = FormStartPosition.CenterParent;
			FlowLayoutPanel tools = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				Height = 52,
				Padding = new Padding(8),
				AutoScroll = true
			};
			Action<string, Action> obj = delegate(string text, Action action)
			{
				Button button = new Button
				{
					Text = text,
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
						status.Text = ex.Message;
						status.ForeColor = Color.Firebrick;
					}
				};
				tools.Controls.Add(button);
			};
			obj("Calculate", Calculate);
			obj("Save sheet XML", Save);
			obj("Open sheet XML", Open);
			obj("Export report to Excel", ExportExcel);
			obj("Print preview", delegate
			{
				CalculationPreview.Show(this, BuildReport());
			});
			obj("Export PDF", delegate
			{
				string text = BuildReport();
				using (SaveFileDialog saveFileDialog = new SaveFileDialog
				{
					Filter = "PDF|*.pdf",
					FileName = kind + ".pdf"
				})
				{
					if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
					{
						EstimatePdf.Write(saveFileDialog.FileName, text);
					}
				}
			});
			base.Controls.Add(Pages);
			base.Controls.Add(tools);
			base.Controls.Add(status);
			TabPage value = new TabPage("Calculation report")
			{
				Controls = { (Control)report }
			};
			Pages.TabPages.Add(value);
		}

		protected FlowLayoutPanel Section(string title, string note)
		{
			TabPage tabPage = new TabPage(title);
			FlowLayoutPanel flow = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				AutoScroll = true,
				Padding = new Padding(18),
				WrapContents = true
			};
			tabPage.Controls.Add(flow);
			if (!string.IsNullOrEmpty(note))
			{
				flow.Controls.Add(new Label
				{
					Text = note,
					Width = 1050,
					Height = 76,
					Tag = "FullWidth",
					ForeColor = PowTheme.Muted
				});
			}
			flow.Resize += delegate
			{
				int width = Math.Max(280, flow.ClientSize.Width - flow.Padding.Horizontal - 20);
				foreach (Control control in flow.Controls)
				{
					if (Convert.ToString(control.Tag) == "FullWidth")
					{
						control.Width = width;
					}
				}
			};
			TabPage value = Pages.TabPages[Pages.TabPages.Count - 1];
			Pages.TabPages.Remove(value);
			Pages.TabPages.Add(tabPage);
			Pages.TabPages.Add(value);
			Pages.SelectedIndex = 0;
			return flow;
		}

		protected TextBox NumberField(FlowLayoutPanel panel, string key, string label, string value)
		{
			TextBox textBox = new TextBox
			{
				Text = value,
				Width = 280
			};
			Field(panel, key, label, textBox);
			return textBox;
		}

		protected ComboBox Choice(FlowLayoutPanel panel, string key, string label, string[] values)
		{
			ComboBox comboBox = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				Width = 280,
				DropDownWidth = 500
			};
			comboBox.Items.AddRange(values);
			comboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
			comboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
			if (values.Length != 0)
			{
				comboBox.SelectedIndex = 0;
			}
			Field(panel, key, label, comboBox);
			return comboBox;
		}

		protected CheckBox Toggle(FlowLayoutPanel panel, string key, string label, bool value)
		{
			CheckBox checkBox = new CheckBox
			{
				Text = label,
				Checked = value,
				Width = 280,
				Height = 40
			};
			Field(panel, key, "", checkBox);
			return checkBox;
		}

		protected void Field(FlowLayoutPanel panel, string key, string label, Control input)
		{
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Width = 310,
				Height = 94,
				FlowDirection = FlowDirection.TopDown,
				WrapContents = false
			};
			flowLayoutPanel.Controls.Add(new Label
			{
				Text = label,
				Width = 280,
				Height = 40
			});
			flowLayoutPanel.Controls.Add(input);
			panel.Controls.Add(flowLayoutPanel);
			Inputs.Add(key, input);
		}

		protected string Value(string key)
		{
			return Inputs[key].Text.Trim();
		}

		protected double Number(string key)
		{
			double result;
			if (!double.TryParse(Value(key), NumberStyles.Float, CultureInfo.InvariantCulture, out result) || double.IsNaN(result) || double.IsInfinity(result))
			{
				throw new Exception("Check " + key + "; enter a finite number with a decimal point.");
			}
			return result;
		}

		protected abstract string BuildReport();

		protected virtual void AfterRestore()
		{
		}

		public string CurrentReport()
		{
			return BuildReport();
		}

		private void Calculate()
		{
			report.Text = BuildReport();
			status.ForeColor = PowTheme.Teal;
			status.Text = "Calculated. Save sheet XML to keep inputs; exports contain the calculation report only.";
			Pages.SelectedIndex = Pages.TabCount - 1;
		}

		private void ExportExcel()
		{
			string text = BuildReport();
			using (SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				Filter = "Excel workbook|*.xlsx",
				FileName = kind + "-Report.xlsx",
				OverwritePrompt = false
			})
			{
				if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
				{
					using (ExcelLiveLink excelLiveLink = new ExcelLiveLink(saveFileDialog.FileName, true, false))
					{
						excelLiveLink.UpdateDesignReport(kind, text);
					}
					status.Text = "Calculation report exported. Save sheet XML separately to retain editable inputs.";
				}
			}
		}

		private void Save()
		{
			using (SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				Filter = "DTCE design sheet|*.design.xml",
				FileName = kind + ".design.xml"
			})
			{
				if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
				{
					File.WriteAllText(saveFileDialog.FileName, Snapshot());
				}
			}
		}

		private void Open()
		{
			using (OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = "DTCE design sheet|*.design.xml"
			})
			{
				if (openFileDialog.ShowDialog(this) == DialogResult.OK)
				{
					Restore(File.ReadAllText(openFileDialog.FileName));
				}
			}
		}

		internal string Snapshot()
		{
			XmlDocument xmlDocument = new XmlDocument();
			XmlElement xmlElement = xmlDocument.CreateElement("DesignSheet");
			xmlElement.SetAttribute("type", kind);
			xmlElement.SetAttribute("version", "1");
			xmlDocument.AppendChild(xmlElement);
			foreach (KeyValuePair<string, Control> input in Inputs)
			{
				XmlElement xmlElement2 = xmlDocument.CreateElement("Input");
				xmlElement2.SetAttribute("key", input.Key);
				xmlElement2.InnerText = ((input.Value is CheckBox) ? ((CheckBox)input.Value).Checked.ToString() : input.Value.Text);
				xmlElement.AppendChild(xmlElement2);
			}
			return xmlDocument.OuterXml;
		}

		internal void Restore(string xml)
		{
			XmlDocument xmlDocument = new XmlDocument
			{
				XmlResolver = null
			};
			using (XmlReader reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings
			{
				DtdProcessing = DtdProcessing.Prohibit,
				XmlResolver = null,
				MaxCharactersInDocument = 2000000L
			}))
			{
				xmlDocument.Load(reader);
			}
			if (xmlDocument.DocumentElement.Name != "DesignSheet" || xmlDocument.DocumentElement.GetAttribute("type") != kind || xmlDocument.DocumentElement.GetAttribute("version") != "1")
			{
				throw new Exception("Choose a matching design-sheet file.");
			}
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			foreach (XmlElement item in xmlDocument.DocumentElement.SelectNodes("Input"))
			{
				string attribute = item.GetAttribute("key");
				if (!Inputs.ContainsKey(attribute) || dictionary.ContainsKey(attribute))
				{
					throw new Exception("Invalid/duplicate design input.");
				}
				dictionary[attribute] = item.InnerText;
			}
			if (dictionary.Count != Inputs.Count)
			{
				throw new Exception("Incomplete design sheet.");
			}
			foreach (KeyValuePair<string, string> item2 in dictionary)
			{
				ComboBox comboBox = Inputs[item2.Key] as ComboBox;
				if (comboBox != null && !comboBox.Items.Contains(item2.Value))
				{
					throw new Exception("Saved profile or selection is unavailable: " + item2.Key);
				}
				if (Inputs[item2.Key] is CheckBox)
				{
					bool.Parse(item2.Value);
				}
			}
			Restoring = true;
			try
			{
				foreach (KeyValuePair<string, string> item3 in dictionary)
				{
					CheckBox checkBox = Inputs[item3.Key] as CheckBox;
					if (checkBox != null)
					{
						checkBox.Checked = bool.Parse(item3.Value);
					}
					else
					{
						Inputs[item3.Key].Text = item3.Value;
					}
				}
			}
			finally
			{
				Restoring = false;
			}
			AfterRestore();
			Calculate();
		}
	}
}
