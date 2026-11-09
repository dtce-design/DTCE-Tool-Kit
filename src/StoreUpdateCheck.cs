using System;using System.Collections;using System.Collections.Generic;using System.Diagnostics;using System.Drawing;using System.Linq;using System.Reflection;using System.Runtime.InteropServices;using System.Text;using System.Threading.Tasks;using System.Windows.Forms;
namespace BeamScheduleFormatter {
static class StoreUpdateQuery {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern int GetCurrentPackageFamilyName(ref uint length,StringBuilder name);
 [ComImport,Guid("3D3E847B-9092-4B0D-A92A-849BBA42C0B2"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]interface IInitializeWithWindow{void Initialize(IntPtr hwnd);}
 static Type WinRT(string name){return Type.GetType(name+", "+name.Substring(0,name.LastIndexOf('.'))+", ContentType=WindowsRuntime",true);}
 internal static string ProductId {get{
 #if ADMIN_APP
 return "9P53CR08ZSZ3";
 #else
 return "9NSQN0157W5K";
 #endif
 }}
 public static async Task<StoreUpdateResult> CheckAsync(IntPtr hwnd){
  if(Environment.OSVersion.Platform!=PlatformID.Win32NT)return StoreUpdateResult.NotPackaged;
  uint length=0;int code=GetCurrentPackageFamilyName(ref length,null);
  if(code==15700)return StoreUpdateResult.NotPackaged;
  if(code!=122||length==0)return StoreUpdateResult.Unavailable;
  var family=new StringBuilder((int)length);if(GetCurrentPackageFamilyName(ref length,family)!=0)return StoreUpdateResult.Unavailable;
  string expected=ProductId=="9P53CR08ZSZ3"?"DTCE.DTCEToolKit-Admin_":"DTCE.DTCEToolKit_";
  if(!family.ToString().StartsWith(expected,StringComparison.Ordinal))return StoreUpdateResult.NotPackaged;
  // Captive portals/local-only connections are not proof that Store is reachable.
  var network=WinRT("Windows.Networking.Connectivity.NetworkInformation");
  object profile=network.GetMethod("GetInternetConnectionProfile").Invoke(null,null);
  if(profile==null||Convert.ToInt32(profile.GetType().GetMethod("GetNetworkConnectivityLevel").Invoke(profile,null))!=3)return StoreUpdateResult.Unavailable;
  var contextType=WinRT("Windows.Services.Store.StoreContext");object context=contextType.GetMethod("GetDefault").Invoke(null,null);
  ((IInitializeWithWindow)context).Initialize(hwnd);
  var method=contextType.GetMethod("GetAppAndOptionalStorePackageUpdatesAsync");object operation=method.Invoke(context,null);
  var info=WinRT("Windows.Foundation.IAsyncInfo");var clock=Stopwatch.StartNew();
  try{
   while(Convert.ToInt32(info.GetProperty("Status").GetValue(operation,null))==0){if(clock.Elapsed>TimeSpan.FromSeconds(30)){info.GetMethod("Cancel").Invoke(operation,null);return StoreUpdateResult.Unavailable;}await Task.Delay(250).ConfigureAwait(false);}
   if(Convert.ToInt32(info.GetProperty("Status").GetValue(operation,null))!=1)return StoreUpdateResult.Unavailable;
   var updates=(IEnumerable)method.ReturnType.GetMethod("GetResults").Invoke(operation,null);
   return updates.Cast<object>().Any()?StoreUpdateResult.Required:StoreUpdateResult.Current;
  }finally{try{info.GetMethod("Close").Invoke(operation,null);}catch{}}
 }
}
sealed class StoreUpdateMonitor:IDisposable {
 readonly Form owner;readonly StoreUpdatePolicy policy=new StoreUpdatePolicy();readonly Timer timer=new Timer{Interval=2000};
 readonly Dictionary<Form,bool> disabled=new Dictionary<Form,bool>();Form gate;Label message;Button retry;bool disposed,closing;
 public StoreUpdateMonitor(Form owner){this.owner=owner;timer.Tick+=async(s,e)=>{timer.Interval=30000;await Check(false);};owner.Shown+=(s,e)=>timer.Start();owner.FormClosed+=(s,e)=>Dispose();}
 async Task Check(bool force){if(disposed||closing||owner.IsDisposed||policy.Checking)return;IntPtr hwnd=owner.Handle;if(retry!=null)retry.Enabled=false;
  await policy.CheckAsync(()=>Task.Run(()=>StoreUpdateQuery.CheckAsync(hwnd)),DateTime.UtcNow,force);
  if(disposed||owner.IsDisposed)return;
  StoreUpdateState.Blocked=policy.Blocked;
  if(policy.Blocked){if(gate==null){gate=new Form{Text="Update required",ClientSize=new Size(610,235),FormBorderStyle=FormBorderStyle.FixedDialog,ControlBox=false,StartPosition=FormStartPosition.CenterParent,ShowInTaskbar=false};AppBrand.Apply(gate);
    message=new Label{Text="A newer DTCE Tool Kit version is available from Microsoft Store. Update the app before continuing. Save your work when closing the app; restart after updating.\n\nIf the Store cannot be reached, Check again allows offline use.",Left=20,Top=18,Width=570,Height=130,Font=new Font("Segoe UI",10)};
    var open=new Button{Text="Open Microsoft Store",Left=20,Top=169,Width=190,Height=40};open.Click+=(s,e)=>{try{Process.Start(new ProcessStartInfo("ms-windows-store://pdp/?ProductId="+StoreUpdateQuery.ProductId){UseShellExecute=true});}catch{message.Text="Microsoft Store could not be opened. Open Store manually, update DTCE Tool Kit, then restart. You can also Check again for offline use.";}};
    retry=new Button{Text="Check again",Left=220,Top=169,Width=150,Height=40};retry.Click+=async(s,e)=>await Check(true);
    var close=new Button{Text="Close app",Left=380,Top=169,Width=150,Height=40};close.Click+=(s,e)=>{closing=true;RestoreWindows();StoreUpdateState.Blocked=false;try{owner.Close();}finally{closing=false;if(!disposed){StoreUpdateState.Blocked=policy.Blocked;DisableWindows();}}};
    gate.Controls.AddRange(new Control[]{message,open,retry,close});gate.Show(owner);
   }DisableWindows();
  }else{RestoreWindows();if(gate!=null){gate.Close();gate.Dispose();gate=null;retry=null;}}
  if(retry!=null)retry.Enabled=true;
 }
 void DisableWindows(){foreach(Form form in Application.OpenForms.Cast<Form>().ToArray())if(form!=gate&&!disabled.ContainsKey(form)){disabled.Add(form,form.Enabled);form.Enabled=false;}if(gate!=null)gate.BringToFront();}
 void RestoreWindows(){foreach(var pair in disabled)if(!pair.Key.IsDisposed)pair.Key.Enabled=pair.Value;disabled.Clear();}
 public void Dispose(){if(disposed)return;disposed=true;timer.Dispose();StoreUpdateState.Blocked=false;RestoreWindows();if(gate!=null){gate.Dispose();gate=null;}}
}
}
