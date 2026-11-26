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
 /* the version the Store has published, read from the public Store catalog (owner, 26 November 2026: "di namang gumagana yung pagcheck ng
    updates"; "e di pa ako inuupdate or ninonotify e may available ng update sa microsoft store"): the catalog lists the package as
    "DTCE.DTCEToolKit_10.0.18.0_x64__..." - this needs no Windows Store API, which failed without a message */
 internal static string IdentityName{get{return ProductId=="9P53CR08ZSZ3"?"DTCE.DTCEToolKit-Admin":"DTCE.DTCEToolKit";}}
 internal static Version CatalogVersion(string json,string identity){Version best=null;foreach(System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(json??"","\"PackageFullName\"\\s*:\\s*\""+System.Text.RegularExpressions.Regex.Escape(identity)+"_(\\d+(?:\\.\\d+){1,3})_")){Version v;if(Version.TryParse(m.Groups[1].Value,out v)&&(best==null||v>best))best=v;}return best;}
 internal static Version PublishedVersion(){System.Net.ServicePointManager.SecurityProtocol|=System.Net.SecurityProtocolType.Tls12;var request=(System.Net.HttpWebRequest)System.Net.WebRequest.Create("https://displaycatalog.mp.microsoft.com/v7.0/products/"+ProductId+"?market=PH&languages=en-us");request.Timeout=20000;request.ReadWriteTimeout=20000;request.UserAgent="DTCE-Tool-Kit";
  using(var response=request.GetResponse())using(var reader=new System.IO.StreamReader(response.GetResponseStream()))return CatalogVersion(reader.ReadToEnd(),IdentityName);}
 internal static Version Running{get{Version v;return Version.TryParse(Engine.Version,out v)?new Version(v.Major,v.Minor,Math.Max(0,v.Build),Math.Max(0,v.Revision)):new Version(0,0,0,0);}}
 internal static string LastNote="";internal static Version LastPublished;internal static bool LastPackaged;
 /* the published version against the running one; the Windows Store API only when the catalog cannot be read */
 public static async Task<StoreUpdateResult> CheckAsync(IntPtr hwnd){
  bool packaged=Packaged();LastPackaged=packaged;LastNote="";
  try{var published=await Task.Run(()=>PublishedVersion()).ConfigureAwait(false);LastPublished=published;
   if(published!=null){if(!packaged)return StoreUpdateResult.NotPackaged;return published>Running?StoreUpdateResult.Required:StoreUpdateResult.Current;}
   LastNote="The Store catalog did not list this app's package.";}
  catch(Exception ex){LastNote="The Store catalog could not be read ("+ex.Message+").";}
  if(!packaged)return StoreUpdateResult.NotPackaged;
  try{return await StoreApiAsync(hwnd).ConfigureAwait(false);}catch(Exception ex){LastNote+=" Store API: "+ex.Message;return StoreUpdateResult.Unavailable;}}
 static bool Packaged(){if(Environment.OSVersion.Platform!=PlatformID.Win32NT)return false;try{uint length=0;int code=GetCurrentPackageFamilyName(ref length,null);if(code!=122||length==0)return false;var family=new StringBuilder((int)length);if(GetCurrentPackageFamilyName(ref length,family)!=0)return false;return family.ToString().StartsWith(IdentityName+"_",StringComparison.Ordinal);}catch(Exception){return false;}}
 static async Task<StoreUpdateResult> StoreApiAsync(IntPtr hwnd){
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
 static Version announced;readonly Dictionary<Form,bool> disabled=new Dictionary<Form,bool>();Form gate;Label message;Button retry;bool disposed,closing;
 internal static void OpenStore(){try{Process.Start(new ProcessStartInfo("ms-windows-store://pdp/?ProductId="+StoreUpdateQuery.ProductId){UseShellExecute=true});}catch(Exception){}}
 public StoreUpdateMonitor(Form owner){this.owner=owner;timer.Tick+=async(s,e)=>{timer.Interval=30000;await Check(false);};owner.Shown+=(s,e)=>timer.Start();owner.FormClosed+=(s,e)=>Dispose();}
 async Task Check(bool force){if(disposed||closing||owner.IsDisposed||policy.Checking)return;IntPtr hwnd=owner.Handle;if(retry!=null)retry.Enabled=false;
  await policy.CheckAsync(()=>Task.Run(()=>StoreUpdateQuery.CheckAsync(hwnd)),DateTime.UtcNow,force);
  if(disposed||owner.IsDisposed)return;
  StoreUpdateState.Blocked=policy.Blocked;
  /* automatic (owner, 26 November 2026: "ayaw ko ng check for updates, gusto ko automatic na"): a newer published version opens the Store
     page by itself once per version (Update there); a copy not installed from the Store is told once per version */
  var published=StoreUpdateQuery.LastPublished;
  if(policy.Blocked&&published!=null&&announced!=published){announced=published;OpenStore();}
  if(!StoreUpdateQuery.LastPackaged&&published!=null&&published>StoreUpdateQuery.Running&&announced!=published){announced=published;if(MessageBox.Show(owner,"A newer DTCE Tool Kit is available: v"+published.ToString(3)+" (this copy is v"+StoreUpdateQuery.Running.ToString(3)+").\r\n\r\nOpen the Microsoft Store page to get it?","Update available",MessageBoxButtons.YesNo,MessageBoxIcon.Information)==DialogResult.Yes)OpenStore();}
  if(policy.Blocked){if(gate==null){gate=new Form{Text="Update required",ClientSize=new Size(610,235),FormBorderStyle=FormBorderStyle.FixedDialog,ControlBox=false,StartPosition=FormStartPosition.CenterParent,ShowInTaskbar=false};AppBrand.Apply(gate);
    message=new Label{Text="A newer DTCE Tool Kit version"+(StoreUpdateQuery.LastPublished!=null?" (v"+StoreUpdateQuery.LastPublished.ToString(3)+")":"")+" is available from Microsoft Store. The Store page has been opened: click Update there. Save your work when closing the app; restart after updating.\n\nIf the Store cannot be reached, Check again allows offline use.",Left=20,Top=18,Width=570,Height=130,Font=new Font("Segoe UI",10)};
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
