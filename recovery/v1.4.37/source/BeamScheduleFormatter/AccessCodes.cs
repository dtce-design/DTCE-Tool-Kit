using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;

namespace BeamScheduleFormatter
{
	internal static class AccessCodes
	{
		private static readonly AccessLease lease = new AccessLease(() => TimeSpan.FromSeconds((double)Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency));

		private static string sessionRequest;

		private static DateTime requestExpiresUtc = DateTime.MinValue;

		private static int allowedApps;

		public const int AllApps = 119;

		private static DateTime expiresUtc = DateTime.MinValue;

		private static string storePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BeamScheduleFormatter", "access-v3-" + InstallationKey(Assembly.GetExecutingAssembly().Location) + ".dat");

		private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public static bool Granted
		{
			get
			{
				if (lease.IsValid)
				{
					return DateTime.UtcNow < expiresUtc;
				}
				return false;
			}
			set
			{
				if (value)
				{
					allowedApps = 119;
					expiresUtc = DateTime.UtcNow.AddHours(8.0);
					lease.Start();
				}
				else
				{
					allowedApps = 0;
					expiresUtc = DateTime.MinValue;
					lease.Reset();
				}
			}
		}

		public static bool CanUseFormatter
		{
			get
			{
				return true;
			}
		}

		public static string InstallationKey(string path)
		{
			return AdminApproval.Identity(path).Replace('+', '-').Replace('/', '_')
				.TrimEnd('=');
		}

		public static bool CanUseApp(int app)
		{
			if (StoreUpdateState.Blocked)
			{
				return false;
			}
			return true;
		}

		public static int AppFor(Form form)
		{
			if (!(form is EstimatorForm) && !(form is SlabEstimatorForm) && !(form is ProgramOfWorksForm))
			{
				if (!(form is RebarLengthForm))
				{
					if (!(form is PurlinDesignForm))
					{
						if (!(form is TrussConnectionForm))
						{
							if (!(form is SlabFormatterForm))
							{
								if (!(form is ProjectInformationForm))
								{
									return 1;
								}
								return 8;
							}
							return 64;
						}
						return 32;
					}
					return 16;
				}
				return 4;
			}
			return 2;
		}

		public static string NewRequest()
		{
			byte[] array = new byte[20];
			using (RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create())
			{
				randomNumberGenerator.GetBytes(array);
			}
			return "BEAM-" + BitConverter.ToString(array).Replace("-", "");
		}

		public static string Normalize(string value)
		{
			return new string((value ?? "").Where((char c) => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
		}

		private static string CleanCode(string value)
		{
			return new string((value ?? "").Where((char c) => !char.IsWhiteSpace(c)).ToArray());
		}

		public static bool ValidRequest(string value)
		{
			return Regex.IsMatch(Normalize(value), "^BEAM-[A-F0-9]{40}$");
		}

		public static string PublicKey()
		{
			using (Stream stream = typeof(AccessCodes).Assembly.GetManifestResourceStream("AccessPublicKey.xml"))
			{
				if (stream == null)
				{
					throw new Exception("Missing embedded access verification key.");
				}
				using (StreamReader streamReader = new StreamReader(stream))
				{
					return streamReader.ReadToEnd();
				}
			}
		}

		public static string KeyId(string publicKey)
		{
			using (SHA256 sHA = SHA256.Create())
			{
				return BitConverter.ToString(sHA.ComputeHash(Encoding.UTF8.GetBytes(publicKey))).Replace("-", "").Substring(0, 16);
			}
		}

		public static string Generate(string request, string privateKey)
		{
			return GenerateAt(request, privateKey, DateTime.UtcNow, 119);
		}

		public static string Generate(string request, string privateKey, int apps)
		{
			return GenerateAt(request, privateKey, DateTime.UtcNow, apps);
		}

		public static string GenerateAt(string request, string privateKey, DateTime issuedUtc)
		{
			return GenerateAt(request, privateKey, issuedUtc, 119);
		}

		public static string GenerateAt(string request, string privateKey, DateTime issuedUtc, int apps)
		{
			if (apps < 1 || (apps & -120) != 0)
			{
				throw new Exception("Select at least one app.");
			}
			request = Normalize(request);
			if (!ValidRequest(request))
			{
				throw new Exception("Paste the complete request code from the user's formatter.");
			}
			long num = (long)(issuedUtc.ToUniversalTime() - Epoch).TotalSeconds;
			long num2 = checked(num + 28800);
			string text = num.ToString(CultureInfo.InvariantCulture) + "." + num2.ToString(CultureInfo.InvariantCulture);
			byte[] bytes = Encoding.UTF8.GetBytes("BEAM-ACCESS-V4|" + request + "|" + text + "|" + apps);
			using (RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider())
			{
				rSACryptoServiceProvider.PersistKeyInCsp = false;
				rSACryptoServiceProvider.FromXmlString(privateKey);
				byte[] inArray = rSACryptoServiceProvider.SignData(bytes, CryptoConfig.MapNameToOID("SHA256"));
				return "BA4." + text + "." + apps + "." + Convert.ToBase64String(inArray);
			}
		}

		public static bool ValidCertificationVersion(string value)
		{
			Version result;
			if (!string.IsNullOrEmpty(value) && Version.TryParse(value, out result) && result.Revision == -1 && result.Build >= 0 && result.Major <= 65535 && result.Minor <= 65535 && result.Build <= 65535)
			{
				return result.ToString(3) == value;
			}
			return false;
		}

		public static string GenerateCertification(string privateKey, string version)
		{
			return GenerateCertificationAt(privateKey, version, DateTime.UtcNow);
		}

		public static string GenerateCertificationAt(string privateKey, string version, DateTime issuedUtc)
		{
			if (!ValidCertificationVersion(version))
			{
				throw new ArgumentException("Enter the exact User app version, for example 1.4.37 (three numbers).");
			}
			long num = (long)(issuedUtc.ToUniversalTime() - Epoch).TotalSeconds;
			long num2 = checked(num + 2592000);
			string text = num.ToString(CultureInfo.InvariantCulture) + "." + num2.ToString(CultureInfo.InvariantCulture);
			string text2 = version.Replace('.', '-');
			using (RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider())
			{
				rSACryptoServiceProvider.PersistKeyInCsp = false;
				rSACryptoServiceProvider.FromXmlString(privateKey);
				byte[] inArray = rSACryptoServiceProvider.SignData(Encoding.UTF8.GetBytes("DTCE-CERTIFICATION-V1|" + text + "|7|" + text2), CryptoConfig.MapNameToOID("SHA256"));
				return "BC1." + text + ".7." + text2 + "." + Convert.ToBase64String(inArray);
			}
		}

		public static bool IsCertification(string code)
		{
			return CleanCode(code).StartsWith("BC1.", StringComparison.Ordinal);
		}

		public static bool Verify(string request, string code, string publicKey)
		{
			DateTime end;
			return ValidateAt(request, code, publicKey, DateTime.UtcNow, out end);
		}

		public static bool ValidateAt(string request, string code, string publicKey, DateTime nowUtc, out DateTime end)
		{
			return ValidateForVersionAt(request, code, publicKey, nowUtc, "1.4.37", out end);
		}

		internal static bool ValidateForVersionAt(string request, string code, string publicKey, DateTime nowUtc, string version, out DateTime end)
		{
			end = DateTime.MinValue;
			try
			{
				if (string.IsNullOrWhiteSpace(code) || code.Length > 2048)
				{
					return false;
				}
				if (IsCertification(code))
				{
					string[] array = CleanCode(code).Split('.');
					long result;
					long result2;
					if (array.Length != 6 || array[3] != "7" || !ValidCertificationVersion(version) || array[4] != version.Replace('.', '-') || !long.TryParse(array[1], NumberStyles.None, CultureInfo.InvariantCulture, out result) || !long.TryParse(array[2], NumberStyles.None, CultureInfo.InvariantCulture, out result2) || result < 0 || result2 < result || result2 - result != 2592000)
					{
						return false;
					}
					DateTime dateTime = Epoch.AddSeconds(result);
					DateTime dateTime2 = Epoch.AddSeconds(result2);
					if (dateTime > nowUtc.ToUniversalTime().AddMinutes(5.0) || dateTime2 <= nowUtc.ToUniversalTime())
					{
						return false;
					}
					using (RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider())
					{
						rSACryptoServiceProvider.PersistKeyInCsp = false;
						rSACryptoServiceProvider.FromXmlString(publicKey);
						if (!rSACryptoServiceProvider.VerifyData(Encoding.UTF8.GetBytes("DTCE-CERTIFICATION-V1|" + array[1] + "." + array[2] + "|7|" + array[4]), CryptoConfig.MapNameToOID("SHA256"), Convert.FromBase64String(array[5])))
						{
							return false;
						}
					}
					end = dateTime2;
					return true;
				}
				request = Normalize(request);
				if (!ValidRequest(request) || string.IsNullOrWhiteSpace(code) || code.Length > 2048)
				{
					return false;
				}
				string[] array2 = CleanCode(code).Split('.');
				int result3;
				long result4;
				long result5;
				if (array2.Length != 5 || (array2[0] != "BA3" && array2[0] != "BA4") || !int.TryParse(array2[3], NumberStyles.None, CultureInfo.InvariantCulture, out result3) || result3 < 1 || (result3 & ~((array2[0] == "BA3") ? 7 : 119)) != 0 || !long.TryParse(array2[1], NumberStyles.None, CultureInfo.InvariantCulture, out result4) || !long.TryParse(array2[2], NumberStyles.None, CultureInfo.InvariantCulture, out result5) || result4 < 0 || result5 < result4 || result5 - result4 != 28800)
				{
					return false;
				}
				DateTime dateTime3 = Epoch.AddSeconds(result4);
				end = Epoch.AddSeconds(result5);
				if (dateTime3 > nowUtc.ToUniversalTime().AddMinutes(5.0) || end <= nowUtc.ToUniversalTime())
				{
					return false;
				}
				byte[] signature = Convert.FromBase64String(array2[4]);
				using (RSACryptoServiceProvider rSACryptoServiceProvider2 = new RSACryptoServiceProvider())
				{
					rSACryptoServiceProvider2.PersistKeyInCsp = false;
					rSACryptoServiceProvider2.FromXmlString(publicKey);
					return rSACryptoServiceProvider2.VerifyData(Encoding.UTF8.GetBytes(((array2[0] == "BA3") ? "BEAM-ACCESS-V3|" : "BEAM-ACCESS-V4|") + request + "|" + array2[1] + "." + array2[2] + "|" + array2[3]), CryptoConfig.MapNameToOID("SHA256"), signature);
				}
			}
			catch (FormatException)
			{
				return false;
			}
			catch (CryptographicException)
			{
				return false;
			}
			catch (ArgumentOutOfRangeException)
			{
				return false;
			}
			catch (XmlException)
			{
				return false;
			}
		}

		private static string[] Load()
		{
			try
			{
				if (!File.Exists(storePath))
				{
					return null;
				}
				byte[] array = ProtectedData.Unprotect(File.ReadAllBytes(storePath), null, DataProtectionScope.CurrentUser);
				try
				{
					string[] array2 = Encoding.UTF8.GetString(array).Split('\n');
					return (array2.Length == 4 && ValidRequest(array2[0])) ? array2 : null;
				}
				finally
				{
					Array.Clear(array, 0, array.Length);
				}
			}
			catch (IOException)
			{
				return null;
			}
			catch (CryptographicException)
			{
				return null;
			}
			catch (UnauthorizedAccessException)
			{
				return null;
			}
		}

		private static void Save(string request, string code, DateTime lastSeen)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(storePath));
			byte[] bytes = Encoding.UTF8.GetBytes(request + "\n" + CleanCode(code) + "\n" + lastSeen.ToUniversalTime().Ticks.ToString(CultureInfo.InvariantCulture) + "\nV2");
			string text = storePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
			try
			{
				File.WriteAllBytes(text, ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser));
				if (File.Exists(storePath))
				{
					File.Replace(text, storePath, null);
				}
				else
				{
					File.Move(text, storePath);
				}
			}
			finally
			{
				Array.Clear(bytes, 0, bytes.Length);
				if (File.Exists(text))
				{
					File.Delete(text);
				}
			}
		}

		private static bool ClockOkay(string[] data)
		{
			long result;
			if (data != null && long.TryParse(data[2], out result))
			{
				return result <= DateTime.UtcNow.AddMinutes(5.0).Ticks;
			}
			return false;
		}

		public static string GetRequest()
		{
			if (sessionRequest == null || DateTime.UtcNow >= requestExpiresUtc)
			{
				sessionRequest = NewRequest();
				requestExpiresUtc = DateTime.UtcNow.AddHours(8.0);
			}
			return sessionRequest;
		}

		public static bool TryRestore()
		{
			lease.Reset();
			allowedApps = 0;
			expiresUtc = DateTime.MinValue;
			sessionRequest = null;
			requestExpiresUtc = DateTime.MinValue;
			string[] array = Load();
			DateTime end;
			if (!ClockOkay(array) || !IsCertification(array[1]) || !ValidateAt(array[0], array[1], PublicKey(), DateTime.UtcNow, out end))
			{
				return false;
			}
			sessionRequest = array[0];
			requestExpiresUtc = end;
			Save(array[0], array[1], DateTime.UtcNow);
			allowedApps = AppsForApprovedCode(array[1]);
			expiresUtc = end;
			lease.Start(end - DateTime.UtcNow, IsCertification(array[1]));
			return Granted;
		}

		internal static int AppsForApprovedCode(string code)
		{
			if (IsCertification(code))
			{
				return 119;
			}
			string[] array = CleanCode(code).Split('.');
			int num = int.Parse(array[3], CultureInfo.InvariantCulture);
			if (!(array[0] == "BA3"))
			{
				return num;
			}
			return ProgramAccess.LegacyMask(num);
		}

		public static bool Accept(string request, string code)
		{
			DateTime end;
			if (!ValidateAt(request, code, PublicKey(), DateTime.UtcNow, out end))
			{
				return false;
			}
			if (IsCertification(code))
			{
				Save(Normalize(request), code, DateTime.UtcNow);
			}
			else if (File.Exists(storePath))
			{
				File.Delete(storePath);
			}
			sessionRequest = Normalize(request);
			requestExpiresUtc = end;
			allowedApps = AppsForApprovedCode(code);
			expiresUtc = end;
			lease.Start(end - DateTime.UtcNow, IsCertification(code));
			return Granted;
		}

		public static void Checkpoint()
		{
			if (Granted)
			{
				string[] array = Load();
				long result;
				if (array != null && IsCertification(array[1]) && array != null && long.TryParse(array[2], out result) && DateTime.UtcNow.Ticks - result > TimeSpan.FromMinutes(1.0).Ticks)
				{
					Save(array[0], array[1], DateTime.UtcNow);
				}
			}
		}
	}
}
