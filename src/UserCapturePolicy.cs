using System;using System.Runtime.CompilerServices;using System.Runtime.InteropServices;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// User edition only: asks Windows to exclude this app's windows from screen capture (Snipping Tool, Print Screen
// and recording apps that use the standard Windows capture APIs). Best effort: it is not DRM and cannot stop every
// capture method, privileged software, remote viewers or a camera. Admin edition is not affected.
static class UserCapturePolicy {
 internal const uint ExcludeFromCapture=0x11,MonitorOnly=1;
 internal static bool Enabled{get{
#if ADMIN_APP
  return false;
#else
  return true;
#endif
 }}
 internal static string Status="Not requested";
 static readonly ConditionalWeakTable<Form,object> attached=new ConditionalWeakTable<Form,object>();
 [DllImport("user32.dll",SetLastError=true)]static extern bool SetWindowDisplayAffinity(IntPtr window,uint affinity);
 [DllImport("user32.dll")]static extern IntPtr GetAncestor(IntPtr window,uint flags);
 // Idempotent; called from AppBrand.Apply and from the ModernUi idle sweep of open forms (dialogs and embedded tools).
 internal static void Attach(Form form){if(!Enabled||form==null||form.IsDisposed)return;object marker;if(attached.TryGetValue(form,out marker))return;attached.Add(form,new object());form.HandleCreated+=(s,e)=>Apply(form);form.Shown+=(s,e)=>Apply(form);form.ParentChanged+=(s,e)=>Apply(form);if(form.IsHandleCreated)Apply(form);}
 static void Apply(Form form){if(!Enabled||form.IsDisposed||!form.IsHandleCreated||Environment.OSVersion.Platform!=PlatformID.Win32NT)return;
  try{IntPtr root=GetAncestor(form.Handle,2);if(root==IntPtr.Zero)root=form.Handle;
   // WDA_EXCLUDEFROMCAPTURE needs Windows 10 version 2004 or later; older Windows shows the window as black (WDA_MONITOR).
   if(SetWindowDisplayAffinity(root,ExcludeFromCapture))Status="Windows capture exclusion active";else if(SetWindowDisplayAffinity(root,MonitorOnly))Status="Windows monitor-only capture protection active";else Status="Windows capture protection unavailable on this system";}
  catch(EntryPointNotFoundException){Status="Windows capture protection unavailable on this system";}catch(DllNotFoundException){Status="Windows capture protection unavailable on this system";}}
}
}
