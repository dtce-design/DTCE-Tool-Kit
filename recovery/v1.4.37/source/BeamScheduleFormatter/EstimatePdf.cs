using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace BeamScheduleFormatter
{
	internal static class EstimatePdf
	{
		private static readonly Encoding Encoding1252 = Encoding.GetEncoding(1252);

		private static string Escape(string s)
		{
			return s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
		}

		public static string Heading(string report)
		{
			if (report.StartsWith("DTCE | DETAILED PURLIN"))
			{
				return "PURLIN DESIGN | DETAILED CALCULATIONS";
			}
			if (report.StartsWith("DTCE | DETAILED TRUSS"))
			{
				return "TRUSS CONNECTIONS | DETAILED CALCULATIONS";
			}
			if (report.StartsWith("PROGRAM OF WORKS"))
			{
				return "PROGRAM OF WORKS | QUANTITY BACKUP";
			}
			if (report.StartsWith("SLAB ESTIMATE"))
			{
				return "SLAB ESTIMATE | PURCHASE BACKUP";
			}
			if (report.StartsWith("REINFORCING STEEL BARS"))
			{
				return "BEAM ESTIMATE | BACKUP QUANTITIES & CALCULATIONS";
			}
			if (!report.Contains("CONFIGURATION REVIEW") && !report.Contains("SPLICE, DEVELOPMENT & HOOKS"))
			{
				return "BEAM ESTIMATE | BACKUP QUANTITIES & CALCULATIONS";
			}
			return "SPLICE, DEVELOPMENT & HOOKS | CALCULATION SHEET";
		}

		public static List<List<string>> Pages(string report)
		{
			List<List<string>> list = new List<List<string>>();
			List<string> list2 = new List<string>();
			list.Add(list2);
			string[] array = report.Replace("\r", "").Split('\n');
			foreach (string text in array)
			{
				if (text == "\f")
				{
					if (list2.Count > 0)
					{
						list2 = new List<string>();
						list.Add(list2);
					}
					continue;
				}
				string text2 = text.Replace("\t", "    ");
				do
				{
					int num = Math.Min(100, text2.Length);
					if (text2.Length > 100)
					{
						int num2 = text2.LastIndexOf(' ', 99, 50);
						if (num2 > 45)
						{
							num = num2;
						}
					}
					if (list2.Count >= 54)
					{
						list2 = new List<string>();
						list.Add(list2);
					}
					list2.Add(text2.Substring(0, num));
					text2 = text2.Substring(num).TrimStart();
				}
				while (text2.Length > 0);
			}
			return list;
		}

		private static byte[] Stream(string dictionary, byte[] data)
		{
			return Encoding1252.GetBytes("<< " + dictionary + " /Length " + data.Length + " >>\nstream\n").Concat(data).Concat(Encoding1252.GetBytes("\nendstream"))
				.ToArray();
		}

		public static void Write(string path, string report)
		{
			ProjectInformation projectInformation = ProjectInfoStore.Read();
			PdfSecurity pdfSecurity = new PdfSecurity();
			List<List<string>> list = Pages(report);
			List<byte[]> objects = new List<byte[]>();
			Action<string> action = delegate(string s)
			{
				objects.Add(Encoding1252.GetBytes(s));
			};
			action("<< /Type /Catalog /Pages 2 0 R >>");
			action("<< /Type /Pages /Count " + list.Count + " /Kids [" + string.Join(" ", from i in Enumerable.Range(0, list.Count)
				select 5 + i * 2 + " 0 R") + "] >>");
			int num = 5 + 2 * list.Count;
			int num2 = num + 1;
			action("<< /Type /Font /Subtype /TrueType /BaseFont /" + ReportFont.PostScriptName + " /FirstChar 0 /LastChar 255 /Widths [" + ReportFont.Widths + "] /FontDescriptor " + num2 + " 0 R /Encoding /WinAnsiEncoding >>");
			objects.Add(Stream("/Type /XObject /Subtype /Image /Width 1200 /Height 1200 /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode", pdfSecurity.Encrypt(PdfBrand.Jpeg, 4)));
			for (int num3 = 0; num3 < list.Count; num3++)
			{
				int objectNumber = 6 + num3 * 2;
				action("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595.28 841.89] /Resources << /Font << /F1 3 0 R >> /XObject << /WM 4 0 R >> >> /Contents " + objectNumber + " 0 R >>");
				StringBuilder stringBuilder = new StringBuilder(PdfBrand.PdfCommands());
				stringBuilder.Append("q 0.55 G 0.5 w 72 722 m 559.28 722 l S Q BT /F1 10 Tf 85 Tz 0.12 0.18 0.25 rg 1 0 0 1 74 805 Tm (" + Escape(Heading(report)) + ") Tj ET BT /F1 8 Tf 85 Tz 0.35 0.42 0.5 rg 1 0 0 1 74 786 Tm (DTCE Tool Kit v1.4.37 | SI units | Print-only review copy) Tj ET BT /F1 8 Tf 85 Tz 0.12 0.18 0.25 rg 1 0 0 1 74 769 Tm (" + Escape(projectInformation.NameLine) + ") Tj 1 0 0 1 74 755 Tm (" + Escape(projectInformation.LocationLine) + ") Tj ET ");
				int num4 = 707;
				foreach (string item in list[num3])
				{
					bool flag = item.StartsWith("## ");
					if (flag)
					{
						stringBuilder.Append("q 0.90 0.94 0.98 rg 72 " + (num4 - 3) + " 487.28 13 re f 0.13 0.34 0.57 rg 72 " + (num4 - 3) + " 2 13 re f Q ");
					}
					stringBuilder.Append("BT /F1 10 Tf 85 Tz 0.12 0.18 0.25 rg 1 0 0 1 74 " + num4 + " Tm (" + Escape(flag ? item.Substring(3) : item) + ") Tj ET ");
					num4 -= 12;
				}
				stringBuilder.Append("q 0.77 0.82 0.88 RG 0.5 w 72 35 m 559.28 35 l S Q BT /F1 8 Tf 85 Tz 0.35 0.42 0.5 rg 1 0 0 1 74 21 Tm (Generated " + DateTime.Now.ToString("yyyy-MM-dd") + " | Lengths in mm unless noted) Tj 1 0 0 1 " + (559.28 - (double)("Page " + (num3 + 1) + " / " + list.Count).Length * ReportFont.Width / 1000.0 * 8.0 * 0.85).ToString("0.###", CultureInfo.InvariantCulture) + " 21 Tm (Page " + (num3 + 1) + " / " + list.Count + ") Tj ET ");
				objects.Add(Stream("", pdfSecurity.Encrypt(Encoding1252.GetBytes(stringBuilder.ToString()), objectNumber)));
			}
			objects.Add(Stream("/Length1 " + ReportFont.Bytes.Length, pdfSecurity.Encrypt(ReportFont.Bytes, num)));
			action(ReportFont.Descriptor(num));
			int num5 = objects.Count + 1;
			action(pdfSecurity.Dictionary());
			string fullPath = Path.GetFullPath(path);
			string text = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
			try
			{
				FileStream f = new FileStream(text, FileMode.CreateNew, FileAccess.Write);
				try
				{
					Action<string> action2 = delegate(string s)
					{
						byte[] bytes = Encoding1252.GetBytes(s);
						f.Write(bytes, 0, bytes.Length);
					};
					action2("%PDF-1.6\n");
					List<long> list2 = new List<long> { 0L };
					for (int num6 = 0; num6 < objects.Count; num6++)
					{
						list2.Add(f.Position);
						action2(num6 + 1 + " 0 obj\n");
						f.Write(objects[num6], 0, objects[num6].Length);
						action2("\nendobj\n");
					}
					long position = f.Position;
					action2("xref\n0 " + list2.Count + "\n0000000000 65535 f \n");
					foreach (long item2 in list2.Skip(1))
					{
						action2(item2.ToString("D10", CultureInfo.InvariantCulture) + " 00000 n \n");
					}
					action2("trailer\n<< /Size " + list2.Count + " /Root 1 0 R /Encrypt " + num5 + " 0 R /ID [<" + PdfSecurity.Hex(pdfSecurity.Id) + "><" + PdfSecurity.Hex(pdfSecurity.Id) + ">] >>\nstartxref\n" + position + "\n%%EOF\n");
				}
				finally
				{
					if (f != null)
					{
						((IDisposable)f).Dispose();
					}
				}
				File.Copy(text, fullPath, true);
			}
			finally
			{
				if (File.Exists(text))
				{
					File.Delete(text);
				}
			}
		}
	}
}
