using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace BeamScheduleFormatter
{
	internal class LengthConfiguration
	{
		public string Name = "Untitled";

		public string Version = "1.4.37";

		public LengthInput Input = new LengthInput();

		public double FySmall = 275.0;

		public double FyLarge = 420.0;

		public int GradeSmall = 40;

		public int GradeLarge = 60;

		public LengthInput ForDiameter(int diameter, bool top)
		{
			if (diameter > 12 && diameter < 16)
			{
				throw new Exception("Configuration does not assign a grade to 13-15mm bars.");
			}
			return new LengthInput
			{
				Db = diameter,
				Fy = ((diameter <= 12) ? FySmall : FyLarge),
				Simplified = Input.Simplified,
				Fc = Input.Fc,
				Cover = Input.Cover,
				Spacing = Input.Spacing,
				Ktr = Input.Ktr,
				AsRatio = Input.AsRatio,
				Spliced = Input.Spliced,
				Round = Input.Round,
				Straight = 0.0,
				Top = top,
				Epoxy = Input.Epoxy,
				Lightweight = Input.Lightweight
			};
		}

		public LengthResult Values(int diameter, bool top)
		{
			return RebarLengths.Calculate(ForDiameter(diameter, top));
		}

		public double Rounded(double value)
		{
			return RebarLengths.Up(value, Input.Round);
		}

		public string Review()
		{
			Validate();
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("SPLICE, DEVELOPMENT & HOOKS - CONFIGURATION REVIEW");
			stringBuilder.AppendLine("## 01  CONFIGURATION & MATERIALS");
			stringBuilder.AppendLine("Configuration: " + Name);
			stringBuilder.AppendLine("Method: " + (Input.Simplified ? "Sample / NSCP425.4.2.2 simplified other cases" : "ACI318M-14 general bond-ratio method"));
			stringBuilder.AppendLine("Actual fc=" + Input.Fc + "MPa | fy <=12mm=" + FySmall + "MPa | fy >=16mm=" + FyLarge + "MPa");
			stringBuilder.AppendLine("Round UP to " + Input.Round + "mm | " + (Input.Lightweight ? "Lightweight lambda0.75" : "Normal-weight lambda1") + " | " + (Input.Epoxy ? "Epoxy-coated" : "Uncoated"));
			stringBuilder.AppendLine("Cover TO MAIN BAR=" + Input.Cover + "mm | CLEAR spacing=" + Input.Spacing + "mm | Ktr=" + Input.Ktr + "mm");
			stringBuilder.AppendLine("Lap selection: As ratio=" + Input.AsRatio + ", bars spliced=" + Input.Spliced + "%. ClassA requires ratio>=2 and <=50% spliced.");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("## 02  DESIGN LENGTH SUMMARY (mm) - ROUNDED UP");
			stringBuilder.AppendLine("Other bars                           Top bars                     Compression");
			stringBuilder.AppendLine(string.Format("{0,5}{1,10}{2,10}{3,10}{4,10}{5,10}{6,10}{7,10}{8,10}{9,10}", "db", "Ld", "Ldh", "Lap A", "Lap B", "Ld(top)", "Lap A", "Lap B", "Ldc", "Lap"));
			stringBuilder.AppendLine(new string('-', 100));
			int[] array = new int[8] { 10, 12, 16, 20, 25, 28, 32, 36 };
			foreach (int num in array)
			{
				LengthResult lengthResult = Values(num, false);
				LengthResult lengthResult2 = Values(num, true);
				stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,5}{1,10:0.00}{2,10:0.00}{3,10:0.00}{4,10:0.00}{5,10:0.00}{6,10:0.00}{7,10:0.00}{8,10:0.00}{9,10:0.00}", num, Rounded(lengthResult.Ld), Rounded(lengthResult.Ldh), Rounded(lengthResult.LapA), Rounded(lengthResult.LapB), Rounded(lengthResult2.Ld), Rounded(lengthResult2.LapA), Rounded(lengthResult2.LapB), Rounded(lengthResult.Ldc), Rounded(lengthResult.CompressionLap)));
			}
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("## 03  HOOK BASIS & ESTIMATOR USE");
			stringBuilder.AppendLine("Hook geometry: main90deg tail12db; main180deg tail=max(4db,65mm). Seismic135deg tail=max(6db,75mm).");
			stringBuilder.AppendLine("Main inside bend6db for10-25mm,8db for28-36mm; ties4db up to16mm,6db for20-25mm.");
			stringBuilder.AppendLine("Ldh is EMBEDMENT, not the hook tail or centerline cut length. Hook cut=arc+tail; beam body length is separate.");
			stringBuilder.AppendLine("Estimator uses rounded values per diameter; TOP zones use top values; bottom/SFR use other values.");
			stringBuilder.AppendLine("No cover/confinement or excess-steel development reductions applied. Confirm actual splice locations and seismic detailing.");
			return stringBuilder.ToString();
		}

		public void Validate()
		{
			if (string.IsNullOrWhiteSpace(Name))
			{
				throw new Exception("Enter a configuration name.");
			}
			if ((GradeSmall != 40 && GradeSmall != 60) || (GradeLarge != 40 && GradeLarge != 60))
			{
				throw new Exception("Invalid configuration grades.");
			}
			int[] array = new int[8] { 10, 12, 16, 20, 25, 28, 32, 36 };
			foreach (int diameter in array)
			{
				Values(diameter, false);
				Values(diameter, true);
			}
		}

		public void Write(XmlElement e)
		{
			e.SetAttribute("name", Name);
			e.SetAttribute("appVersion", Version);
			e.SetAttribute("format", "1");
			double[] array = new double[11]
			{
				FySmall, FyLarge, GradeSmall, GradeLarge, Input.Fc, Input.Cover, Input.Spacing, Input.Ktr, Input.AsRatio, Input.Spliced,
				Input.Round
			};
			string[] array2 = new string[11]
			{
				"fySmall", "fyLarge", "gradeSmall", "gradeLarge", "fc", "cover", "spacing", "ktr", "asRatio", "spliced",
				"round"
			};
			for (int i = 0; i < array2.Length; i++)
			{
				e.SetAttribute(array2[i], array[i].ToString("R", CultureInfo.InvariantCulture));
			}
			e.SetAttribute("db", Input.Db.ToString(CultureInfo.InvariantCulture));
			e.SetAttribute("top", Input.Top.ToString());
			e.SetAttribute("straight", Input.Straight.ToString(CultureInfo.InvariantCulture));
			e.SetAttribute("simplified", Input.Simplified.ToString());
			e.SetAttribute("epoxy", Input.Epoxy.ToString());
			e.SetAttribute("lightweight", Input.Lightweight.ToString());
		}

		public static LengthConfiguration Read(XmlElement e)
		{
			if (e == null || e.GetAttribute("format") != "1")
			{
				throw new Exception("Unsupported length configuration.");
			}
			Func<string, double> func = delegate(string n)
			{
				double result;
				if (!double.TryParse(e.GetAttribute(n), NumberStyles.Float, CultureInfo.InvariantCulture, out result) || double.IsNaN(result) || double.IsInfinity(result))
				{
					throw new Exception("Invalid configuration field: " + n);
				}
				return result;
			};
			LengthConfiguration lengthConfiguration = new LengthConfiguration
			{
				Name = e.GetAttribute("name"),
				Version = e.GetAttribute("appVersion"),
				FySmall = func("fySmall"),
				FyLarge = func("fyLarge"),
				GradeSmall = (int)func("gradeSmall"),
				GradeLarge = (int)func("gradeLarge"),
				Input = new LengthInput
				{
					Fc = func("fc"),
					Cover = func("cover"),
					Spacing = func("spacing"),
					Ktr = func("ktr"),
					AsRatio = func("asRatio"),
					Spliced = func("spliced"),
					Round = func("round"),
					Simplified = (e.HasAttribute("simplified") && bool.Parse(e.GetAttribute("simplified"))),
					Epoxy = bool.Parse(e.GetAttribute("epoxy")),
					Lightweight = bool.Parse(e.GetAttribute("lightweight"))
				}
			};
			if (e.HasAttribute("db"))
			{
				lengthConfiguration.Input.Db = func("db");
			}
			if (e.HasAttribute("top"))
			{
				lengthConfiguration.Input.Top = bool.Parse(e.GetAttribute("top"));
			}
			if (e.HasAttribute("straight"))
			{
				lengthConfiguration.Input.Straight = func("straight");
			}
			lengthConfiguration.Validate();
			return lengthConfiguration;
		}

		public void Save(string path)
		{
			Validate();
			XmlDocument xmlDocument = new XmlDocument();
			XmlElement xmlElement = xmlDocument.CreateElement("RebarLengthConfiguration");
			xmlDocument.AppendChild(xmlElement);
			Write(xmlElement);
			XmlElement xmlElement2 = xmlDocument.CreateElement("LengthValuesMM");
			xmlElement.AppendChild(xmlElement2);
			int[] array = new int[8] { 10, 12, 16, 20, 25, 28, 32, 36 };
			for (int i = 0; i < array.Length; i++)
			{
				int diameter = array[i];
				LengthResult lengthResult = Values(diameter, false);
				LengthResult lengthResult2 = Values(diameter, true);
				XmlElement xmlElement3 = xmlDocument.CreateElement("Bar");
				xmlElement3.SetAttribute("diameter", diameter.ToString());
				xmlElement3.SetAttribute("otherLd", Rounded(lengthResult.Ld).ToString(CultureInfo.InvariantCulture));
				xmlElement3.SetAttribute("topLd", Rounded(lengthResult2.Ld).ToString(CultureInfo.InvariantCulture));
				xmlElement3.SetAttribute("hookLdh", Rounded(lengthResult.Ldh).ToString(CultureInfo.InvariantCulture));
				xmlElement3.SetAttribute("otherLap", Rounded(lengthResult.Lap).ToString(CultureInfo.InvariantCulture));
				xmlElement3.SetAttribute("topLap", Rounded(lengthResult2.Lap).ToString(CultureInfo.InvariantCulture));
				xmlElement3.SetAttribute("hook90ArcTail", Rounded(lengthResult.Cut90).ToString(CultureInfo.InvariantCulture));
				xmlElement3.SetAttribute("hook180ArcTail", Rounded(lengthResult.Cut180).ToString(CultureInfo.InvariantCulture));
				xmlElement2.AppendChild(xmlElement3);
			}
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
		}

		public static LengthConfiguration Load(string path)
		{
			XmlDocument xmlDocument = new XmlDocument
			{
				XmlResolver = null
			};
			using (XmlReader reader = XmlReader.Create(path, new XmlReaderSettings
			{
				DtdProcessing = DtdProcessing.Prohibit,
				XmlResolver = null,
				MaxCharactersInDocument = 2000000L
			}))
			{
				xmlDocument.Load(reader);
			}
			if (xmlDocument.DocumentElement.Name != "RebarLengthConfiguration")
			{
				throw new Exception("Choose a .rebar-config.xml file.");
			}
			return Read(xmlDocument.DocumentElement);
		}
	}
}
