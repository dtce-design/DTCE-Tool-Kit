using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace BeamScheduleFormatter
{
	internal static class OpeningLinks
	{
		public static List<OpeningItem> Resolve(IEnumerable<OpeningItem> source, IEnumerable<OpeningLink> assignments)
		{
			List<OpeningItem> list = source.ToList();
			List<OpeningLink> list2 = assignments.ToList();
			Dictionary<string, OpeningItem> dictionary = list.ToDictionary((OpeningItem o) => o.Id, StringComparer.OrdinalIgnoreCase);
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (OpeningLink item2 in list2)
			{
				if (string.IsNullOrWhiteSpace(item2.Wall) || string.IsNullOrWhiteSpace(item2.Schedule) || item2.Quantity < 1 || item2.Quantity > 100000)
				{
					throw new Exception("Check CHB schedule link and quantity.");
				}
				OpeningItem value;
				if (!dictionary.TryGetValue(item2.Schedule, out value) || value.Material == "Manual")
				{
					throw new Exception("CHB schedule not found: " + item2.Schedule);
				}
				if (!hashSet.Add(item2.Wall.Trim() + "\n" + item2.Schedule.Trim()))
				{
					throw new Exception("Duplicate opening schedule on wall " + item2.Wall);
				}
			}
			foreach (IGrouping<string, OpeningLink> item3 in list2.GroupBy((OpeningLink l) => l.Schedule, StringComparer.OrdinalIgnoreCase))
			{
				if (((IEnumerable<OpeningLink>)item3).Sum((Func<OpeningLink, long>)((OpeningLink l) => l.Quantity)) > dictionary[item3.Key].Quantity)
				{
					throw new Exception("CHB assignments exceed total schedule quantity for " + item3.Key + ". Increase schedule Qty or correct wall assignments.");
				}
			}
			List<OpeningItem> list3 = new List<OpeningItem>();
			foreach (OpeningItem item in list)
			{
				List<OpeningLink> list4 = list2.Where((OpeningLink l) => string.Equals(l.Schedule, item.Id, StringComparison.OrdinalIgnoreCase)).ToList();
				if (item.Material == "Manual" || list4.Count == 0)
				{
					if (!string.IsNullOrWhiteSpace(item.Wall))
					{
						list3.Add(item);
					}
					continue;
				}
				foreach (OpeningLink item4 in list4)
				{
					list3.Add(new OpeningItem
					{
						Id = item.Id,
						Description = item.Description,
						Wall = item4.Wall,
						Material = item.Material,
						Kind = item.Kind,
						Width = item.Width,
						Height = item.Height,
						Quantity = item4.Quantity
					});
				}
			}
			foreach (IGrouping<string, OpeningItem> item5 in list3.GroupBy((OpeningItem o) => o.Wall, StringComparer.OrdinalIgnoreCase))
			{
				if (item5.Count() > 3)
				{
					throw new Exception("Maximum 3 opening entries per wall group: " + item5.Key);
				}
			}
			return list3;
		}

		public static List<OpeningLink> Read(XmlElement root)
		{
			List<OpeningLink> list = new List<OpeningLink>();
			XmlElement xmlElement = root["OpeningAssignments"];
			if (xmlElement == null)
			{
				return list;
			}
			foreach (XmlElement item in xmlElement.SelectNodes("Link"))
			{
				int result;
				if (!int.TryParse(item.GetAttribute("qty"), out result) || result < 1 || result > 100000 || string.IsNullOrWhiteSpace(item.GetAttribute("wall")) || string.IsNullOrWhiteSpace(item.GetAttribute("schedule")))
				{
					throw new Exception("Invalid saved CHB opening assignment.");
				}
				list.Add(new OpeningLink
				{
					Wall = item.GetAttribute("wall"),
					Schedule = item.GetAttribute("schedule"),
					Quantity = result
				});
			}
			return list;
		}

		public static void Write(XmlElement root, IEnumerable<OpeningLink> links)
		{
			XmlElement xmlElement = root.OwnerDocument.CreateElement("OpeningAssignments");
			root.AppendChild(xmlElement);
			foreach (OpeningLink link in links)
			{
				XmlElement xmlElement2 = root.OwnerDocument.CreateElement("Link");
				xmlElement2.SetAttribute("wall", link.Wall);
				xmlElement2.SetAttribute("schedule", link.Schedule);
				xmlElement2.SetAttribute("qty", link.Quantity.ToString(CultureInfo.InvariantCulture));
				xmlElement.AppendChild(xmlElement2);
			}
		}
	}
}
