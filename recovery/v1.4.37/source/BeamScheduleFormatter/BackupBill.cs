using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace BeamScheduleFormatter
{
	internal static class BackupBill
	{
		public static double? ReadQuantity(object value)
		{
			double result;
			if (value != null && double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out result) && !double.IsNaN(result) && !double.IsInfinity(result) && result >= 0.0)
			{
				return result;
			}
			return null;
		}

		public static List<BackupBillItem> Capture(IEnumerable<BackupTarget> targets, IEnumerable<BackupBillItem> previous)
		{
			List<BackupTarget> source = targets.ToList();
			List<BackupBillItem> old = previous.ToList();
			foreach (BackupBillItem p in old.Where((BackupBillItem backupBillItem) => backupBillItem.ManualQuantity.HasValue))
			{
				if (!source.Any((BackupTarget t) => t.SlotId == p.Slot && BackupMapping.Identity(t) == p.Signature))
				{
					throw new Exception("A saved BOQ manual item is missing or changed in this workbook: " + p.Description + ". Review the BACKUP item before transferring quantities.");
				}
			}
			return source.Select((BackupTarget t) => new BackupBillItem
			{
				Slot = t.SlotId,
				Signature = BackupMapping.Identity(t),
				Code = t.Code,
				Description = t.Description,
				Unit = t.Unit,
				Row = t.Row,
				WorkbookQuantity = t.Quantity,
				ManualQuantity = (from backupBillItem in old
					where backupBillItem.Slot == t.SlotId && backupBillItem.Signature == BackupMapping.Identity(t)
					select backupBillItem.ManualQuantity).FirstOrDefault()
			}).ToList();
		}

		public static void Write(XmlElement root, IEnumerable<BackupBillItem> items)
		{
			XmlElement xmlElement = root.OwnerDocument.CreateElement("BackupBill");
			root.AppendChild(xmlElement);
			foreach (BackupBillItem item in items)
			{
				XmlElement xmlElement2 = root.OwnerDocument.CreateElement("Item");
				xmlElement.AppendChild(xmlElement2);
				xmlElement2.SetAttribute("slot", item.Slot);
				xmlElement2.SetAttribute("signature", item.Signature);
				xmlElement2.SetAttribute("code", item.Code);
				xmlElement2.SetAttribute("description", item.Description);
				xmlElement2.SetAttribute("unit", item.Unit);
				xmlElement2.SetAttribute("row", item.Row.ToString(CultureInfo.InvariantCulture));
				if (item.WorkbookQuantity.HasValue)
				{
					xmlElement2.SetAttribute("workbook", item.WorkbookQuantity.Value.ToString("R", CultureInfo.InvariantCulture));
				}
				if (item.ManualQuantity.HasValue)
				{
					xmlElement2.SetAttribute("manual", item.ManualQuantity.Value.ToString("R", CultureInfo.InvariantCulture));
				}
			}
		}

		public static List<BackupBillItem> Read(XmlElement root)
		{
			List<BackupBillItem> list = new List<BackupBillItem>();
			XmlElement xmlElement = root["BackupBill"];
			if (xmlElement == null)
			{
				return list;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			foreach (XmlElement item in xmlElement.SelectNodes("Item"))
			{
				BackupBillItem backupBillItem = new BackupBillItem
				{
					Slot = item.GetAttribute("slot"),
					Signature = item.GetAttribute("signature"),
					Code = item.GetAttribute("code"),
					Description = item.GetAttribute("description"),
					Unit = item.GetAttribute("unit"),
					Row = int.Parse(item.GetAttribute("row"), CultureInfo.InvariantCulture)
				};
				if (backupBillItem.Slot == "" || !hashSet.Add(backupBillItem.Slot) || backupBillItem.Signature != BackupMapping.Identity(backupBillItem.Target))
				{
					throw new Exception("Invalid saved BACKUP bill item.");
				}
				string[] array = new string[2] { "workbook", "manual" };
				foreach (string text in array)
				{
					if (item.HasAttribute(text))
					{
						double? num = ReadQuantity(item.GetAttribute(text));
						if (!num.HasValue)
						{
							throw new Exception("Invalid saved BOQ quantity.");
						}
						if (text == "manual")
						{
							backupBillItem.ManualQuantity = num;
						}
						else
						{
							backupBillItem.WorkbookQuantity = num;
						}
					}
				}
				list.Add(backupBillItem);
			}
			return list;
		}
	}
}
