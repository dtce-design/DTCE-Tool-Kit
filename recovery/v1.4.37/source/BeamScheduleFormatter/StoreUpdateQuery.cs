using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BeamScheduleFormatter
{
	internal static class StoreUpdateQuery
	{
		[ComImport]
		[Guid("3D3E847B-9092-4B0D-A92A-849BBA42C0B2")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		private interface IInitializeWithWindow
		{
			void Initialize(IntPtr hwnd);
		}

		internal static string ProductId
		{
			get
			{
				return "9P53CR08ZSZ3";
			}
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern int GetCurrentPackageFamilyName(ref uint length, StringBuilder name);

		private static Type WinRT(string name)
		{
			return Type.GetType(name + ", " + name.Substring(0, name.LastIndexOf('.')) + ", ContentType=WindowsRuntime", true);
		}

		public static async Task<StoreUpdateResult> CheckAsync(IntPtr hwnd)
		{
			if (Environment.OSVersion.Platform != PlatformID.Win32NT)
			{
				return StoreUpdateResult.NotPackaged;
			}
			uint length = 0u;
			switch (GetCurrentPackageFamilyName(ref length, null))
			{
			case 15700:
				return StoreUpdateResult.NotPackaged;
			case 122:
				if (length != 0)
				{
					StringBuilder stringBuilder = new StringBuilder((int)length);
					if (GetCurrentPackageFamilyName(ref length, stringBuilder) != 0)
					{
						return StoreUpdateResult.Unavailable;
					}
					string value = ((ProductId == "9P53CR08ZSZ3") ? "DTCE.DTCEToolKit-Admin_" : "DTCE.DTCEToolKit_");
					if (!stringBuilder.ToString().StartsWith(value, StringComparison.Ordinal))
					{
						return StoreUpdateResult.NotPackaged;
					}
					object obj = WinRT("Windows.Networking.Connectivity.NetworkInformation").GetMethod("GetInternetConnectionProfile").Invoke(null, null);
					if (obj == null || Convert.ToInt32(obj.GetType().GetMethod("GetNetworkConnectivityLevel").Invoke(obj, null)) != 3)
					{
						return StoreUpdateResult.Unavailable;
					}
					Type type = WinRT("Windows.Services.Store.StoreContext");
					object obj2 = type.GetMethod("GetDefault").Invoke(null, null);
					((IInitializeWithWindow)obj2).Initialize(hwnd);
					MethodInfo method = type.GetMethod("GetAppAndOptionalStorePackageUpdatesAsync");
					object operation = method.Invoke(obj2, null);
					Type info = WinRT("Windows.Foundation.IAsyncInfo");
					Stopwatch clock = Stopwatch.StartNew();
					try
					{
						while (Convert.ToInt32(info.GetProperty("Status").GetValue(operation, null)) == 0)
						{
							if (clock.Elapsed > TimeSpan.FromSeconds(30.0))
							{
								info.GetMethod("Cancel").Invoke(operation, null);
								return StoreUpdateResult.Unavailable;
							}
							await Task.Delay(250).ConfigureAwait(false);
						}
						if (Convert.ToInt32(info.GetProperty("Status").GetValue(operation, null)) != 1)
						{
							return StoreUpdateResult.Unavailable;
						}
						return (!((IEnumerable)method.ReturnType.GetMethod("GetResults").Invoke(operation, null)).Cast<object>().Any()) ? StoreUpdateResult.Current : StoreUpdateResult.Required;
					}
					finally
					{
						try
						{
							info.GetMethod("Close").Invoke(operation, null);
						}
						catch
						{
						}
					}
				}
				goto default;
			default:
				return StoreUpdateResult.Unavailable;
			}
		}
	}
}
