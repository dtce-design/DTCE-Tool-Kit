using System;
using System.Globalization;
using System.IO;
using System.Xml;

namespace BeamScheduleFormatter
{
	internal static class RCDefaultStore
	{
		internal static string PathName
		{
			get
			{
				return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "DTCE Tool Kit", "Defaults", "ReinforcedConcrete.xml");
			}
		}

		internal static void Save(string path, XmlDocument doc)
		{
			Validate(doc);
			Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
			string text = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
			try
			{
				doc.Save(text);
				if (File.Exists(path))
				{
					File.Replace(text, path, path + ".bak");
				}
				else
				{
					File.Move(text, path);
				}
			}
			finally
			{
				if (File.Exists(text))
				{
					File.Delete(text);
				}
			}
		}

		internal static XmlDocument Read(string path)
		{
			XmlDocument xmlDocument = new XmlDocument
			{
				XmlResolver = null
			};
			using (XmlReader reader = XmlReader.Create(path, new XmlReaderSettings
			{
				DtdProcessing = DtdProcessing.Prohibit,
				XmlResolver = null,
				MaxCharactersInDocument = 4000000L
			}))
			{
				xmlDocument.Load(reader);
			}
			Validate(xmlDocument);
			return xmlDocument;
		}

		private static void Validate(XmlDocument doc)
		{
			if (doc.DocumentElement == null || doc.DocumentElement.Name != "ReinforcedConcreteDefaults" || doc.DocumentElement.GetAttribute("version") != "1")
			{
				throw new Exception("Invalid reinforced concrete defaults file.");
			}
			string[] array = new string[5] { "Beam", "Slab", "Footing", "Canopy", "RCWall" };
			foreach (string text in array)
			{
				decimal result;
				if (!decimal.TryParse(doc.DocumentElement.GetAttribute("cover" + text), NumberStyles.Number, CultureInfo.InvariantCulture, out result) || result < 0m || result > (decimal)((text == "Slab") ? 100 : ((text == "Beam") ? 200 : 500)))
				{
					throw new Exception("Invalid concrete cover default.");
				}
			}
		}
	}
}
