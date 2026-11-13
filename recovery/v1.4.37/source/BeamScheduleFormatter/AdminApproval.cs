using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace BeamScheduleFormatter
{
	internal static class AdminApproval
	{
		private struct FileInfoNative
		{
			public uint Attr;

			public System.Runtime.InteropServices.ComTypes.FILETIME Created;

			public System.Runtime.InteropServices.ComTypes.FILETIME Accessed;

			public System.Runtime.InteropServices.ComTypes.FILETIME Written;

			public uint Volume;

			public uint SizeHigh;

			public uint SizeLow;

			public uint Links;

			public uint IndexHigh;

			public uint IndexLow;
		}

		public static string DirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BeamTools", "AdminActivation");

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool GetFileInformationByHandle(SafeFileHandle file, out FileInfoNative info);

		private static byte[] Hash(byte[] bytes)
		{
			using (SHA256 sHA = SHA256.Create())
			{
				return sHA.ComputeHash(bytes);
			}
		}

		public static string Identity(string path)
		{
			path = Path.GetFullPath(path);
			using (FileStream fileStream = File.OpenRead(path))
			{
				FileInfoNative info;
				string text = (GetFileInformationByHandle(fileStream.SafeFileHandle, out info) ? (info.Volume + ":" + info.IndexHigh + ":" + info.IndexLow) : "");
				byte[] inArray;
				using (SHA256 sHA = SHA256.Create())
				{
					inArray = sHA.ComputeHash(fileStream);
				}
				return Convert.ToBase64String(Hash(Encoding.UTF8.GetBytes(path.ToUpperInvariant() + "|1.4.37|" + File.GetCreationTimeUtc(path).Ticks + "|" + text + "|" + Convert.ToBase64String(inArray))));
			}
		}

		private static string PathFor(string identity)
		{
			return Path.Combine(DirectoryPath, BitConverter.ToString(Hash(Encoding.UTF8.GetBytes(identity))).Replace("-", "") + ".dat");
		}

		public static bool Approved(string identity)
		{
			try
			{
				string path = PathFor(identity);
				if (!File.Exists(path))
				{
					return false;
				}
				return Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser)) == identity;
			}
			catch
			{
				return false;
			}
		}

		public static void Remember(string identity)
		{
			Directory.CreateDirectory(DirectoryPath);
			string text = PathFor(identity);
			string text2 = text + ".tmp";
			try
			{
				File.WriteAllBytes(text2, ProtectedData.Protect(Encoding.UTF8.GetBytes(identity), null, DataProtectionScope.CurrentUser));
				File.Copy(text2, text, true);
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
