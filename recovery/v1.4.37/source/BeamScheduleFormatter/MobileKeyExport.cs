using System;
using System.Security.Cryptography;
using System.Text;

namespace BeamScheduleFormatter
{
	internal static class MobileKeyExport
	{
		public const int Iterations = 600000;

		private static string Url(byte[] bytes)
		{
			return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-')
				.Replace('/', '_');
		}

		public static string Create(string privateKey, string password, string publicKey)
		{
			if (string.IsNullOrEmpty(password) || password.Length < 12)
			{
				throw new ArgumentException("Use at least 12 characters for the mobile key password.");
			}
			byte[] array = null;
			byte[] array2 = null;
			using (RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider())
			{
				rSACryptoServiceProvider.PersistKeyInCsp = false;
				rSACryptoServiceProvider.FromXmlString(privateKey);
				if (rSACryptoServiceProvider.PublicOnly || rSACryptoServiceProvider.ToXmlString(false) != publicKey)
				{
					throw new CryptographicException("Key mismatch");
				}
				RSAParameters rSAParameters = rSACryptoServiceProvider.ExportParameters(true);
				try
				{
					string text = "{\"kty\":\"RSA\",\"n\":\"" + Url(rSAParameters.Modulus) + "\",\"e\":\"" + Url(rSAParameters.Exponent) + "\",\"d\":\"" + Url(rSAParameters.D) + "\",\"p\":\"" + Url(rSAParameters.P) + "\",\"q\":\"" + Url(rSAParameters.Q) + "\",\"dp\":\"" + Url(rSAParameters.DP) + "\",\"dq\":\"" + Url(rSAParameters.DQ) + "\",\"qi\":\"" + Url(rSAParameters.InverseQ) + "\"}";
					array = Encoding.UTF8.GetBytes("{\"keyId\":\"" + AccessCodes.KeyId(publicKey) + "\",\"jwk\":" + text + "}");
				}
				finally
				{
					byte[][] array3 = new byte[6][] { rSAParameters.D, rSAParameters.P, rSAParameters.Q, rSAParameters.DP, rSAParameters.DQ, rSAParameters.InverseQ };
					foreach (byte[] array4 in array3)
					{
						if (array4 != null)
						{
							Array.Clear(array4, 0, array4.Length);
						}
					}
				}
			}
			try
			{
				byte[] array5 = new byte[16];
				byte[] array6 = new byte[16];
				using (RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create())
				{
					randomNumberGenerator.GetBytes(array5);
					randomNumberGenerator.GetBytes(array6);
				}
				using (Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, array5, 600000, HashAlgorithmName.SHA256))
				{
					array2 = rfc2898DeriveBytes.GetBytes(64);
				}
				byte[] inArray;
				using (Aes aes = Aes.Create())
				{
					aes.Mode = CipherMode.CBC;
					aes.Padding = PaddingMode.PKCS7;
					aes.KeySize = 256;
					byte[] array7 = new byte[32];
					Array.Copy(array2, array7, 32);
					try
					{
						aes.Key = array7;
						aes.IV = array6;
						using (ICryptoTransform cryptoTransform = aes.CreateEncryptor())
						{
							inArray = cryptoTransform.TransformFinalBlock(array, 0, array.Length);
						}
					}
					finally
					{
						Array.Clear(array7, 0, array7.Length);
					}
				}
				string text2 = Convert.ToBase64String(array5);
				string text3 = Convert.ToBase64String(array6);
				string text4 = Convert.ToBase64String(inArray);
				string s = "DTCE-MOBILE-KEY-V1|" + 600000 + "|" + text2 + "|" + text3 + "|" + text4;
				byte[] array8 = new byte[32];
				Array.Copy(array2, 32, array8, 0, 32);
				string text5;
				try
				{
					using (HMACSHA256 hMACSHA = new HMACSHA256(array8))
					{
						text5 = Convert.ToBase64String(hMACSHA.ComputeHash(Encoding.UTF8.GetBytes(s)));
					}
				}
				finally
				{
					Array.Clear(array8, 0, array8.Length);
				}
				return "{\"format\":\"DTCE-MOBILE-KEY-V1\",\"iterations\":" + 600000 + ",\"salt\":\"" + text2 + "\",\"iv\":\"" + text3 + "\",\"ciphertext\":\"" + text4 + "\",\"mac\":\"" + text5 + "\"}";
			}
			finally
			{
				if (array != null)
				{
					Array.Clear(array, 0, array.Length);
				}
				if (array2 != null)
				{
					Array.Clear(array2, 0, array2.Length);
				}
			}
		}
	}
}
