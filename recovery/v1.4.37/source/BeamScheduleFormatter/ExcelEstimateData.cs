using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace BeamScheduleFormatter
{
	internal static class ExcelEstimateData
	{
		public const string Marker = "DTCEEmbeddedEstimateV1";

		private const int Max = 100000000;

		public static string[] Pack(string xml)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(xml);
			if (bytes.Length > 100000000)
			{
				throw new Exception("Estimate exceeds workbook backup limit.");
			}
			string text;
			using (SHA256 sHA = SHA256.Create())
			{
				text = Convert.ToBase64String(sHA.ComputeHash(bytes));
			}
			string text2;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				using (GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
				{
					gZipStream.Write(bytes, 0, bytes.Length);
				}
				text2 = text + "\n" + Convert.ToBase64String(memoryStream.ToArray());
			}
			List<string> list = new List<string>();
			for (int i = 0; i < text2.Length; i += 30000)
			{
				list.Add(text2.Substring(i, Math.Min(30000, text2.Length - i)));
			}
			return list.ToArray();
		}

		public static string Unpack(string[] chunks)
		{
			if (chunks == null || chunks.Length == 0 || chunks.Length > 5000)
			{
				throw new Exception("Invalid embedded estimate.");
			}
			foreach (string text in chunks)
			{
				if (text == null || text.Length > 30000)
				{
					throw new Exception("Invalid estimate chunk.");
				}
			}
			string text2 = string.Concat(chunks);
			int num = text2.IndexOf('\n');
			if (num != 44)
			{
				throw new Exception("Invalid estimate checksum header.");
			}
			using (MemoryStream stream = new MemoryStream(Convert.FromBase64String(text2.Substring(num + 1))))
			{
				using (GZipStream gZipStream = new GZipStream(stream, CompressionMode.Decompress))
				{
					using (MemoryStream memoryStream = new MemoryStream())
					{
						byte[] array = new byte[8192];
						int num2;
						while ((num2 = gZipStream.Read(array, 0, array.Length)) > 0)
						{
							if (memoryStream.Length + num2 > 100000000)
							{
								throw new Exception("Embedded estimate exceeds size limit.");
							}
							memoryStream.Write(array, 0, num2);
						}
						byte[] array2 = memoryStream.ToArray();
						using (SHA256 sHA = SHA256.Create())
						{
							if (Convert.ToBase64String(sHA.ComputeHash(array2)) != text2.Substring(0, num))
							{
								throw new Exception("Embedded estimate checksum mismatch.");
							}
						}
						return new UTF8Encoding(false, true).GetString(array2);
					}
				}
			}
		}
	}
}
