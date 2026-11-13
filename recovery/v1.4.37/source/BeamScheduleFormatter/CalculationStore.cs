using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace BeamScheduleFormatter
{
	internal static class CalculationStore
	{
		public static string StorageDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BeamTools");

		public static event Action Changed;

		private static string Key(LengthConfiguration c)
		{
			return c.Input.Fc.ToString("R", CultureInfo.InvariantCulture) + "/" + c.GradeSmall + "/" + c.FySmall.ToString("R", CultureInfo.InvariantCulture) + "/" + c.GradeLarge + "/" + c.FyLarge.ToString("R", CultureInfo.InvariantCulture);
		}

		public static LengthConfiguration Clone(LengthConfiguration c)
		{
			XmlElement e = new XmlDocument().CreateElement("Configuration");
			c.Write(e);
			return LengthConfiguration.Read(e);
		}

		public static List<LengthConfiguration> All()
		{
			List<LengthConfiguration> list = new List<LengthConfiguration>();
			int[] array = new int[3] { 21, 28, 35 };
			for (int i = 0; i < array.Length; i++)
			{
				int num = array[i];
				int[] array2 = new int[2] { 40, 60 };
				for (int j = 0; j < array2.Length; j++)
				{
					int num2 = array2[j];
					int[] array3 = new int[2] { 40, 60 };
					for (int k = 0; k < array3.Length; k++)
					{
						int num3 = array3[k];
						list.Add(new LengthConfiguration
						{
							Name = "Built-in FC" + num + " G" + num2 + "/G" + num3,
							GradeSmall = num2,
							GradeLarge = num3,
							FySmall = ((num2 == 40) ? 275 : 420),
							FyLarge = ((num3 == 40) ? 275 : 420),
							Input = new LengthInput
							{
								Fc = num,
								Round = 25.0,
								Simplified = true
							}
						});
					}
				}
			}
			list.Add(new LengthConfiguration
			{
				Name = "Sample 27.6MPa / 414MPa",
				GradeSmall = 60,
				GradeLarge = 60,
				FySmall = 414.0,
				FyLarge = 414.0,
				Input = new LengthInput
				{
					Fc = 27.6,
					Round = 25.0,
					Simplified = true
				}
			});
			list.AddRange(Saved());
			return list;
		}

		private static List<LengthConfiguration> Saved()
		{
			string text = Path.Combine(StorageDirectory, "SpliceDevelopmentHookConfigurations.xml");
			List<LengthConfiguration> list = new List<LengthConfiguration>();
			if (!File.Exists(text))
			{
				return list;
			}
			XmlDocument xmlDocument = new XmlDocument
			{
				XmlResolver = null
			};
			using (XmlReader reader = XmlReader.Create(text, new XmlReaderSettings
			{
				DtdProcessing = DtdProcessing.Prohibit,
				XmlResolver = null,
				MaxCharactersInDocument = 4000000L
			}))
			{
				xmlDocument.Load(reader);
			}
			if (xmlDocument.DocumentElement.Name != "CalculationConfigurations")
			{
				throw new Exception("Invalid saved calculation configurations. Restore the local configuration store from a backup.");
			}
			foreach (XmlElement item in xmlDocument.DocumentElement.SelectNodes("Configuration"))
			{
				list.Add(LengthConfiguration.Read(item));
			}
			return list;
		}

		public static LengthConfiguration Resolve(double fc, int gs, double fs, int gl, double fl)
		{
			LengthConfiguration request = new LengthConfiguration
			{
				GradeSmall = gs,
				FySmall = fs,
				GradeLarge = gl,
				FyLarge = fl,
				Input = new LengthInput
				{
					Fc = fc
				}
			};
			LengthConfiguration lengthConfiguration = All().LastOrDefault((LengthConfiguration c) => Key(c) == Key(request));
			if (lengthConfiguration != null)
			{
				return Clone(lengthConfiguration);
			}
			LengthConfiguration lengthConfiguration2 = new LengthConfiguration();
			lengthConfiguration2.Name = "Built-in FC" + fc + " fy" + fs + "/" + fl;
			lengthConfiguration2.GradeSmall = gs;
			lengthConfiguration2.FySmall = fs;
			lengthConfiguration2.GradeLarge = gl;
			lengthConfiguration2.FyLarge = fl;
			lengthConfiguration2.Input = new LengthInput
			{
				Fc = fc,
				Round = 25.0,
				Simplified = true
			};
			return lengthConfiguration2;
		}

		public static void Save(LengthConfiguration c)
		{
			c.Validate();
			List<LengthConfiguration> list = Saved();
			list.RemoveAll((LengthConfiguration x) => x.Name.Equals(c.Name, StringComparison.OrdinalIgnoreCase));
			list.Add(Clone(c));
			XmlDocument xmlDocument = new XmlDocument();
			XmlElement xmlElement = xmlDocument.CreateElement("CalculationConfigurations");
			xmlDocument.AppendChild(xmlElement);
			foreach (LengthConfiguration item in list)
			{
				XmlElement xmlElement2 = xmlDocument.CreateElement("Configuration");
				item.Write(xmlElement2);
				xmlElement.AppendChild(xmlElement2);
			}
			Directory.CreateDirectory(StorageDirectory);
			string text = Path.Combine(StorageDirectory, "SpliceDevelopmentHookConfigurations.xml");
			string text2 = text + "." + Guid.NewGuid().ToString("N") + ".tmp";
			try
			{
				xmlDocument.Save(text2);
				if (File.Exists(text))
				{
					File.Replace(text2, text, text + ".bak");
				}
				else
				{
					File.Move(text2, text);
				}
			}
			finally
			{
				if (File.Exists(text2))
				{
					File.Delete(text2);
				}
			}
			if (CalculationStore.Changed != null)
			{
				CalculationStore.Changed();
			}
		}

		public static LengthConfiguration Choose(IWin32Window owner)
		{
			List<LengthConfiguration> list = All();
			Form f = new Form
			{
				Text = "Saved splice, development & hook configurations",
				Width = 700,
				Height = 480,
				StartPosition = FormStartPosition.CenterParent,
				MinimizeBox = false,
				MaximizeBox = false
			};
			try
			{
				ListBox listBox = new ListBox
				{
					Dock = DockStyle.Fill,
					Font = new Font("Segoe UI", 10f),
					HorizontalScrollbar = true
				};
				foreach (LengthConfiguration item in list)
				{
					listBox.Items.Add(item.Name + "  |  fc " + item.Input.Fc + "  |  fy " + item.FySmall + " / " + item.FyLarge);
				}
				listBox.SelectedIndex = 0;
				Button button = new Button
				{
					Text = "Open selected",
					Dock = DockStyle.Bottom,
					Height = 40,
					DialogResult = DialogResult.OK
				};
				f.Controls.Add(listBox);
				f.Controls.Add(button);
				f.AcceptButton = button;
				listBox.DoubleClick += delegate
				{
					f.DialogResult = DialogResult.OK;
					f.Close();
				};
				return (f.ShowDialog(owner) == DialogResult.OK) ? Clone(list[listBox.SelectedIndex]) : null;
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
