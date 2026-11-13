using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

namespace BeamScheduleFormatter
{
	internal static class StructureProfileLibrary
	{
		internal static string DefaultPath
		{
			get
			{
				return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "DTCE Tool Kit", "Profiles", "StructureProfiles.xml");
			}
		}

		public static void Validate(StructureProfile profile)
		{
			if (profile == null || string.IsNullOrWhiteSpace(profile.Name) || profile.Name.Length > 160 || profile.Geometry == null || profile.Geometry.Length != 25 || profile.Members.Count + profile.ExtraRebar.Count == 0 || profile.Members.Count > 2000 || profile.ExtraRebar.Count > 2000)
			{
				throw new Exception("Save a named structure profile with complete member/rebar inputs.");
			}
			ChamberTakeoff chamberTakeoff = ChamberInputs.Read(profile.Geometry);
			if (chamberTakeoff.Kind != profile.Kind)
			{
				throw new Exception("Structure profile type mismatch.");
			}
			if (profile.Configuration == null || profile.Configuration.Input.Fc != 21.0 || profile.Configuration.GradeSmall != 40 || profile.Configuration.GradeLarge != 40)
			{
				throw new Exception("Profile needs its matching Class A / Grade40 calculation configuration.");
			}
			profile.Configuration.Validate();
			foreach (string[] member in profile.Members)
			{
				StructureMemberInput structureMemberInput = StructureMemberInput.Read(member);
				if (chamberTakeoff.Kind == "Catch Basin (CHB)" && structureMemberInput.WaterproofFaces != 0)
				{
					throw new Exception("Catch-basin profile has waterproofing outside its selected quantity scope.");
				}
				StructureMemberTakeoff.Calculate(structureMemberInput, chamberTakeoff.Count, profile.Configuration);
			}
			foreach (string[] item in profile.ExtraRebar)
			{
				if (item.Length != 7)
				{
					throw new Exception("Incomplete additional rebar input.");
				}
				int diameter = int.Parse(item[3], CultureInfo.InvariantCulture);
				int quantity = int.Parse(item[5], CultureInfo.InvariantCulture);
				double num = double.Parse(item[4], CultureInfo.InvariantCulture);
				double num2 = double.Parse(item[6], CultureInfo.InvariantCulture);
				SteelWeight.Count(quantity);
				SteelWeight.Positive(num, num2);
				EstimatorEngine.UnitMass(diameter);
				if (num > num2 || !CommercialBars.Lengths.Contains(num2))
				{
					throw new Exception("Additional developed cuts must fit their selected stock.");
				}
			}
		}

		private static string Key(StructureProfile p)
		{
			return p.Kind + "|" + p.Name.ToUpperInvariant();
		}

		public static List<StructureProfile> Merge(IEnumerable<StructureProfile> existing, IEnumerable<StructureProfile> incoming)
		{
			List<StructureProfile> list = incoming.ToList();
			foreach (StructureProfile item in list)
			{
				Validate(item);
			}
			if (list.Select(Key).Distinct().Count() != list.Count)
			{
				throw new Exception("Duplicate incoming structure profile name.");
			}
			HashSet<string> keys = new HashSet<string>(list.Select(Key));
			return existing.Where((StructureProfile p) => !keys.Contains(Key(p))).Concat(list).ToList();
		}

		private static void WriteValues(XmlElement parent, string name, string[] values)
		{
			XmlElement xmlElement = parent.OwnerDocument.CreateElement(name);
			parent.AppendChild(xmlElement);
			for (int i = 0; i < values.Length; i++)
			{
				xmlElement.SetAttribute("c" + i, values[i] ?? "");
			}
		}

		private static string[] ReadValues(XmlElement row, int count)
		{
			if (row == null)
			{
				throw new Exception("Missing profile inputs.");
			}
			string[] array = new string[count];
			for (int i = 0; i < count; i++)
			{
				if (!row.HasAttribute("c" + i))
				{
					throw new Exception("Incomplete profile input.");
				}
				array[i] = row.GetAttribute("c" + i);
				if (array[i].Length > 4096)
				{
					throw new Exception("Profile input is too long.");
				}
			}
			return array;
		}

		public static void Save(string path, IEnumerable<StructureProfile> profiles)
		{
			List<StructureProfile> list = profiles.ToList();
			if (list.Count == 0 || list.Count > 1000 || list.Select(Key).Distinct().Count() != list.Count)
			{
				throw new Exception("Save 1 to 1000 uniquely named structure profiles.");
			}
			XmlDocument xmlDocument = new XmlDocument();
			XmlElement xmlElement = xmlDocument.CreateElement("StructureProfileLibrary");
			xmlElement.SetAttribute("version", "1");
			xmlDocument.AppendChild(xmlElement);
			foreach (StructureProfile item in list)
			{
				Validate(item);
				XmlElement xmlElement2 = xmlDocument.CreateElement("Profile");
				xmlElement.AppendChild(xmlElement2);
				xmlElement2.SetAttribute("name", item.Name);
				xmlElement2.SetAttribute("kind", item.Kind);
				WriteValues(xmlElement2, "Geometry", item.Geometry);
				XmlElement xmlElement3 = xmlDocument.CreateElement("Members");
				xmlElement2.AppendChild(xmlElement3);
				foreach (string[] member in item.Members)
				{
					WriteValues(xmlElement3, "Entry", member);
				}
				XmlElement xmlElement4 = xmlDocument.CreateElement("ExtraRebar");
				xmlElement2.AppendChild(xmlElement4);
				foreach (string[] item2 in item.ExtraRebar)
				{
					WriteValues(xmlElement4, "Entry", item2);
				}
				XmlElement xmlElement5 = xmlDocument.CreateElement("Configuration");
				xmlElement2.AppendChild(xmlElement5);
				item.Configuration.Write(xmlElement5);
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

		public static List<StructureProfile> Load(string path)
		{
			XmlDocument xmlDocument = new XmlDocument
			{
				XmlResolver = null
			};
			using (XmlReader reader = XmlReader.Create(path, new XmlReaderSettings
			{
				XmlResolver = null,
				DtdProcessing = DtdProcessing.Prohibit,
				MaxCharactersInDocument = 8000000L
			}))
			{
				xmlDocument.Load(reader);
			}
			if (xmlDocument.DocumentElement.Name != "StructureProfileLibrary" || xmlDocument.DocumentElement.GetAttribute("version") != "1")
			{
				throw new Exception("Unsupported structure profile library.");
			}
			List<StructureProfile> list = new List<StructureProfile>();
			foreach (XmlElement item in xmlDocument.DocumentElement.SelectNodes("Profile"))
			{
				StructureProfile structureProfile = new StructureProfile
				{
					Name = item.GetAttribute("name"),
					Kind = item.GetAttribute("kind"),
					Geometry = ReadValues(item["Geometry"], 25),
					Configuration = LengthConfiguration.Read(item["Configuration"])
				};
				foreach (XmlElement item2 in item.SelectNodes("Members/Entry"))
				{
					structureProfile.Members.Add(ReadValues(item2, 24));
				}
				foreach (XmlElement item3 in item.SelectNodes("ExtraRebar/Entry"))
				{
					structureProfile.ExtraRebar.Add(ReadValues(item3, 7));
				}
				Validate(structureProfile);
				list.Add(structureProfile);
				if (list.Count > 1000)
				{
					throw new Exception("Too many profiles.");
				}
			}
			if (list.Count == 0 || list.Select(Key).Distinct().Count() != list.Count)
			{
				throw new Exception("Empty or duplicate structure profiles.");
			}
			return list;
		}
	}
}
