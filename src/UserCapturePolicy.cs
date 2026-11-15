using System;using System.Runtime.CompilerServices;using System.Runtime.InteropServices;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Windows best-effort display affinity protects only windows owned by this app.
// It is not DRM, and cannot prevent every capture method or a physical camera.
static class CaptureProtectionRules {
 internal const bool Required=
#if ADMIN_APP
 false;
#else
 true;
#endif
}
static class UserCapturePolicy {
 internal const uint ExcludeFromCapture=0x11,MonitorOnly=1;
 internal static bool Enabled{get{return CaptureProtectionRules.Required;}}
 static bool installed;internal static string Status="Not requested";
 internal static void Install(){if(!Enabled||installed)return;installed=true;Application.AddMessageFilter(new CaptureMessages());}
 sealed class CaptureMessages:IMessageFilter {public bool PreFilterMessage(ref Message message){if(message.Msg==0x18||message.Msg==0x46){var form=Control.FromHandle(message.HWnd) as Form;if(form!=null)Attach(form);}return false;}}
 static readonly ConditionalWeakTable<Form,object> attached=new ConditionalWeakTable<Form,object>();
 [DllImport("user32.dll",SetLastError=true)]static extern bool SetWindowDisplayAffinity(IntPtr window,uint affinity);
 [DllImport("user32.dll")]static extern IntPtr GetAncestor(IntPtr window,uint flags);
 internal static void Attach(Form form){if(!Enabled||form==null||form.IsDisposed)return;object marker;if(attached.TryGetValue(form,out marker))return;attached.Add(form,new object());form.HandleCreated+=(s,e)=>Apply(form);form.Shown+=(s,e)=>Apply(form);form.ParentChanged+=(s,e)=>Apply(form);if(form.IsHandleCreated)Apply(form);}
 static void Apply(Form form){if(!Enabled||form.IsDisposed||!form.IsHandleCreated||Environment.OSVersion.Platform!=PlatformID.Win32NT)return;try{IntPtr root=GetAncestor(form.Handle,2);if(root==IntPtr.Zero)root=form.Handle;if(SetWindowDisplayAffinity(root,ExcludeFromCapture))Status="Windows capture exclusion requested";else if(SetWindowDisplayAffinity(root,MonitorOnly))Status="Windows monitor-only fallback requested";else Status="Windows capture protection unavailable";}catch(EntryPointNotFoundException){}catch(DllNotFoundException){} }
}
}
