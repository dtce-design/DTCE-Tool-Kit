using System;
using System.IO;
using System.Xml;

namespace BeamScheduleFormatter
{
	internal static class ProjectInfoStore
	{
		public static string DirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BeamTools");

		public static ProjectInformation Read()
		{
			string text = Path.Combine(DirectoryPath, "ProjectInformation.xml");
			if (!File.Exists(text))
			{
				return new ProjectInformation();
			}
			XmlDocument xmlDocument = new XmlDocument
			{
				XmlResolver = null
			};
			using (XmlReader reader = XmlReader.Create(text, new XmlReaderSettings
			{
				DtdProcessing = DtdProcessing.Prohibit,
				XmlResolver = null,
				MaxCharactersInDocument = 10000L
			}))
			{
				xmlDocument.Load(reader);
			}
			if (xmlDocument.DocumentElement.Name != "ProjectInformation")
			{
				throw new Exception("Invalid saved project information.");
			}
			return new ProjectInformation
			{
				Name = xmlDocument.DocumentElement.GetAttribute("name"),
				Location = xmlDocument.DocumentElement.GetAttribute("location"),
				Office = xmlDocument.DocumentElement.GetAttribute("office"),
				OfficeAddress = xmlDocument.DocumentElement.GetAttribute("officeAddress")
			};
		}

		public static void Save(string name, string location)
		{
			ProjectInformation projectInformation = Read();
			Save(name, location, projectInformation.Office, projectInformation.OfficeAddress);
		}

		public static void Save(string name, string location, string office, string officeAddress)
		{
			name = name.Trim();
			location = location.Trim();
			if (name.Length > 100 || location.Length > 100 || name.Contains("\n") || location.Contains("\n") || name.Contains("\r") || location.Contains("\r"))
			{
				throw new Exception("Use a single line, maximum100characters per field.");
			}
			office = (office ?? "").Trim();
			officeAddress = (officeAddress ?? "").Trim();
			if (office.Length > 200 || officeAddress.Length > 200)
			{
				throw new Exception("Office and address must be at most 200 characters.");
			}
			XmlDocument xmlDocument = new XmlDocument();
			XmlElement xmlElement = xmlDocument.CreateElement("ProjectInformation");
			xmlElement.SetAttribute("office", office);
			xmlElement.SetAttribute("officeAddress", officeAddress);
			xmlElement.SetAttribute("name", name);
			xmlElement.SetAttribute("location", location);
			xmlDocument.AppendChild(xmlElement);
			Directory.CreateDirectory(DirectoryPath);
			string text = Path.Combine(DirectoryPath, "ProjectInformation.xml");
			string text2 = text + "." + Guid.NewGuid().ToString("N") + ".tmp";
			try
			{
				xmlDocument.Save(text2);
				if (File.Exists(text))
				{
					File.Replace(text2, text, null);
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
		}
	}
}
