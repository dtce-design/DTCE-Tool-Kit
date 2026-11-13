using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace BeamScheduleFormatter
{
	internal static class ReportFont
	{
		public static readonly string FontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "consola.ttf")) ? "consola.ttf" : "cour.ttf");

		public static readonly byte[] Bytes = File.ReadAllBytes(FontPath);

		public static string Family
		{
			get
			{
				if (!(Path.GetFileName(FontPath).ToLowerInvariant() == "consola.ttf"))
				{
					return "Courier New";
				}
				return "Consolas";
			}
		}

		public static string PostScriptName
		{
			get
			{
				if (!(Family == "Consolas"))
				{
					return "CourierNewPSMT";
				}
				return "Consolas";
			}
		}

		private static double Scale
		{
			get
			{
				return 1000.0 / (double)U16(Table("head") + 18);
			}
		}

		public static double Width
		{
			get
			{
				return (double)U16(Table("hmtx")) * Scale;
			}
		}

		public static string Widths
		{
			get
			{
				return string.Join(" ", Enumerable.Repeat(F(Width), 256));
			}
		}

		private static int U16(int p)
		{
			return (Bytes[p] << 8) | Bytes[p + 1];
		}

		private static int S16(int p)
		{
			return (short)U16(p);
		}

		private static int I32(int p)
		{
			return (Bytes[p] << 24) | (Bytes[p + 1] << 16) | (Bytes[p + 2] << 8) | Bytes[p + 3];
		}

		private static int Table(string name)
		{
			for (int i = 0; i < U16(4); i++)
			{
				int num = 12 + i * 16;
				if (Encoding.ASCII.GetString(Bytes, num, 4) == name)
				{
					return I32(num + 8);
				}
			}
			throw new Exception("Font table missing: " + name);
		}

		private static string F(double n)
		{
			return n.ToString("0.###", CultureInfo.InvariantCulture);
		}

		public static string Descriptor(int fileId)
		{
			int head = Table("head");
			int num = Table("hhea");
			return "<< /Type /FontDescriptor /FontName /" + PostScriptName + " /Flags 33 /FontBBox [" + string.Join(" ", new int[4] { 36, 38, 40, 42 }.Select((int p) => F((double)S16(head + p) * Scale))) + "] /ItalicAngle 0 /Ascent " + F((double)S16(num + 4) * Scale) + " /Descent " + F((double)S16(num + 6) * Scale) + " /CapHeight 700 /StemV 80 /FontFile2 " + fileId + " 0 R >>";
		}
	}
}
