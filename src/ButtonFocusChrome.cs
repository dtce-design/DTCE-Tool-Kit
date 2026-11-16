using System;using System.Runtime.CompilerServices;using System.Runtime.InteropServices;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Keep native button clicks, keyboard actions and DialogResult behavior. The shared
// painter supplies a solid keyboard-focus outline instead of a dotted rectangle.
static class ButtonFocusChrome {
 static readonly ConditionalWeakTable<Button,FocusWindow> attached=new ConditionalWeakTable<Button,FocusWindow>();
 internal static void Attach(Button button){FocusWindow found;if(!attached.TryGetValue(button,out found))attached.Add(button,new FocusWindow(button));}
 sealed class FocusWindow:NativeWindow {
  readonly Button button;
  [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr handle,int message,IntPtr wparam,IntPtr lparam);
  internal FocusWindow(Button owner){button=owner;button.HandleCreated+=(s,e)=>Bind();button.HandleDestroyed+=(s,e)=>ReleaseHandle();button.GotFocus+=(s,e)=>HideNativeCue();if(button.IsHandleCreated)Bind();}
  void Bind(){AssignHandle(button.Handle);HideNativeCue();}
  void HideNativeCue(){if(button.IsHandleCreated&&!button.IsDisposed)SendMessage(button.Handle,0x128,new IntPtr(0x10001),IntPtr.Zero);}
  protected override void WndProc(ref Message message){base.WndProc(ref message);if(message.Msg==0x129)message.Result=new IntPtr(message.Result.ToInt64()|1);}
 }
}
}
