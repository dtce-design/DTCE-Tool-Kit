using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BeamScheduleFormatter
{
	internal static class AdminKeyStore
	{
		private static string LocalPath
		{
			get
			{
				return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DTCE Tool Kit", "Admin", "AdminPrivateKey.dat");
			}
		}

		internal static string Decode(byte[] encrypted)
		{
			byte[] array = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
			try
			{
				string text = Encoding.UTF8.GetString(array);
				using (RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider())
				{
					rSACryptoServiceProvider.PersistKeyInCsp = false;
					rSACryptoServiceProvider.FromXmlString(text);
					if (rSACryptoServiceProvider.PublicOnly || rSACryptoServiceProvider.ToXmlString(false) != AccessCodes.PublicKey())
					{
						throw new CryptographicException("Key mismatch");
					}
				}
				return text;
			}
			finally
			{
				Array.Clear(array, 0, array.Length);
			}
		}

		public static string Load()
		{
			return Decode(File.ReadAllBytes(File.Exists(LocalPath) ? LocalPath : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AdminPrivateKey.dat")));
		}

		public static void Import(string source)
		{
			byte[] array = File.ReadAllBytes(source);
			Decode(array);
			string localPath = LocalPath;
			Directory.CreateDirectory(Path.GetDirectoryName(localPath));
			string text = localPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
			try
			{
				File.WriteAllBytes(text, array);
				if (File.Exists(localPath))
				{
					File.Replace(text, localPath, null);
				}
				else
				{
					File.Move(text, localPath);
				}
			}
			finally
			{
				Array.Clear(array, 0, array.Length);
				if (File.Exists(text))
				{
					File.Delete(text);
				}
			}
		}
	}
}
