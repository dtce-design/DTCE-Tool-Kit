using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal static class AdminPasswordGate
	{
		private const string Salt = "rjxe3n1rtWtt70gzV3zwag==";

		private const string Hash = "mhFHUBKFoA0paIzHrgVy2HapvqfGgsFMgu+Ho0gXsfY=";

		public static bool Verify(string password)
		{
			if (password == null)
			{
				return false;
			}
			byte[] bytes;
			using (Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, Convert.FromBase64String("rjxe3n1rtWtt70gzV3zwag=="), 200000))
			{
				bytes = rfc2898DeriveBytes.GetBytes(32);
			}
			byte[] array = Convert.FromBase64String("mhFHUBKFoA0paIzHrgVy2HapvqfGgsFMgu+Ho0gXsfY=");
			int num = 0;
			for (int i = 0; i < array.Length; i++)
			{
				num |= bytes[i] ^ array[i];
			}
			Array.Clear(bytes, 0, bytes.Length);
			return num == 0;
		}

		public static bool Unlock()
		{
			string identity = AdminApproval.Identity(Assembly.GetEntryAssembly().Location);
			if (AdminApproval.Approved(identity))
			{
				return true;
			}
			using (AdminPasswordForm adminPasswordForm = new AdminPasswordForm())
			{
				if (adminPasswordForm.ShowDialog() != DialogResult.OK)
				{
					return false;
				}
				AdminApproval.Remember(identity);
				return true;
			}
		}
	}
}
