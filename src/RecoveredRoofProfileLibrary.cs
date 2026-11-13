using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

namespace BeamScheduleFormatter
{
	internal static class RoofProfileLibrary
	{
		internal static string DefaultPath
		{
			get
			{
				return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "DTCE Tool Kit", "Profiles", "RoofProfiles.xml");
			}
		}

		internal static List<RoofProfile> Merge(IEnumerable<RoofProfile> existing, IEnumerable<RoofProfile> incoming)
		{
			List<RoofProfile> list = incoming.ToList();
			Validate(list);
			HashSet<string> names = new HashSet<string>(list.Select((RoofProfile p) => p.Name), StringComparer.OrdinalIgnoreCase);
			List<RoofProfile> list2 = existing.Where((RoofProfile p) => !names.Contains(p.Name)).Concat(list).ToList();
			Validate(list2);
			return list2;
		}

		internal static string[] LegacyPaths()
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			List<string> list = new List<string> { Path.Combine(folderPath, "DTCE Tool Kit", "Profiles", "RoofProfiles.xml") };
			string path = Path.Combine(folderPath, "Packages");
			if (Directory.Exists(path))
			{
				string[] directories = Directory.GetDirectories(path, "DTCE.DTCEToolKit*");
				foreach (string text in directories)
				{
					list.Add(Path.Combine(text, "LocalCache", "Local", "DTCE Tool Kit", "Profiles", "RoofProfiles.xml"));
				}
			}
			return list.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
		}

		internal static bool Migrate(string target, IEnumerable<string> candidates)
		{
			if (File.Exists(target))
			{
				return false;
			}
			List<string> list = candidates.Where(File.Exists).OrderBy(File.GetLastWriteTimeUtc).ToList();
			if (list.Count == 0)
			{
				return false;
			}
			List<RoofProfile> list2 = new List<RoofProfile>();
			foreach (string item in list)
			{
				list2 = Merge(list2, Load(item));
			}
			Save(target, list2);
			return true;
		}

		private static void Validate(List<RoofProfile> profiles)
		{
			if (profiles.Count == 0 || profiles.Count > 10000)
			{
				throw new Exception("Save 1 to 10000 valid roof profile options.");
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (RoofProfile profile in profiles)
			{
				SteelWeight.Positive(profile.Width, profile.Cover);
				if (string.IsNullOrWhiteSpace(profile.Name) || string.IsNullOrWhiteSpace(profile.SideLap) || profile.Cover > profile.Width || !new string[2] { "Longspan Rib", "Longspan Corrugated" }.Contains(profile.Family) || !hashSet.Add(profile.Name + "|" + profile.SideLap))
				{
					throw new Exception("Invalid or duplicate roof profile option.");
				}
			}
			foreach (IGrouping<string, RoofProfile> group in profiles.GroupBy((RoofProfile p) => p.Name, StringComparer.OrdinalIgnoreCase))
			{
				if (group.Any((RoofProfile p) => p.Family != group.First().Family || Math.Abs(p.Width - group.First().Width) > 1E-08))
				{
					throw new Exception("Profile options must share a family and full width.");
				}
			}
		}

		internal static void Save(string path, IEnumerable<RoofProfile> data)
		{
			List<RoofProfile> list = data.ToList();
			Validate(list);
			XmlDocument xmlDocument = new XmlDocument();
			XmlElement xmlElement = xmlDocument.CreateElement("RoofProfileLibrary");
			xmlElement.SetAttribute("version", "1");
			xmlDocument.AppendChild(xmlElement);
			foreach (RoofProfile item in list)
			{
				XmlElement xmlElement2 = xmlDocument.CreateElement("Profile");
				xmlElement2.SetAttribute("name", item.Name);
				xmlElement2.SetAttribute("family", item.Family);
				xmlElement2.SetAttribute("lap", item.SideLap);
				xmlElement2.SetAttribute("width", item.Width.ToString("R", CultureInfo.InvariantCulture));
				xmlElement2.SetAttribute("cover", item.Cover.ToString("R", CultureInfo.InvariantCulture));
				xmlElement.AppendChild(xmlElement2);
			}
			string fullPath = Path.GetFullPath(path);
			Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
			string text = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
			try
			{
				xmlDocument.Save(text);
				if (File.Exists(fullPath))
				{
					File.Replace(text, fullPath, fullPath + ".bak");
				}
				else
				{
					File.Move(text, fullPath);
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

		internal static List<RoofProfile> Load(string path)
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
			if (xmlDocument.DocumentElement.Name != "RoofProfileLibrary" || xmlDocument.DocumentElement.GetAttribute("version") != "1")
			{
				throw new Exception("Unsupported app profile library.");
			}
			List<RoofProfile> list = new List<RoofProfile>();
			foreach (XmlElement item in xmlDocument.DocumentElement.SelectNodes("Profile"))
			{
				list.Add(new RoofProfile
				{
					Name = item.GetAttribute("name"),
					Family = item.GetAttribute("family"),
					SideLap = item.GetAttribute("lap"),
					Width = double.Parse(item.GetAttribute("width"), CultureInfo.InvariantCulture),
					Cover = double.Parse(item.GetAttribute("cover"), CultureInfo.InvariantCulture)
				});
			}
			Validate(list);
			return list;
		}
	}
}
