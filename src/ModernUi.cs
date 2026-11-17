using System;
using System.Drawing;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace BeamScheduleFormatter {
// Shared desktop presentation only. No calculation, persistence or access decisions.
static class ModernUi {
 internal static readonly Font Body = new Font("Segoe UI", 10f);
 internal static readonly Font Strong = new Font("Segoe UI", 10f, FontStyle.Bold);
 internal static readonly Font Heading = new Font("Segoe UI", 20f, FontStyle.Bold);
 internal static Color Surface {get{return DesktopAppearance.Palette.Surface;}}
 internal static Color Input {get{return DesktopAppearance.Palette.Input;}}
 internal static Color Selection {get{return DesktopAppearance.Palette.Selected;}}
 internal static Color Soft {get{return DesktopAppearance.Palette.Soft;}}
 static readonly ConditionalWeakTable<Form, object> windows = new ConditionalWeakTable<Form, object>();
 static bool installed, applying;static int ownerThread;
 internal static void Install() {
  if(installed)return; installed=true;ownerThread=System.Threading.Thread.CurrentThread.ManagedThreadId;DesktopAppearance.Initialize();
  // Includes modal dialogs and embedded tools which do not call AppBrand.Apply.
  // Only new forms are traversed; do not walk every table on every idle event.
  Application.Idle += delegate {
   if(System.Threading.Thread.CurrentThread.ManagedThreadId!=ownerThread||applying)return; applying=true;
   try { foreach(Form form in Application.OpenForms) {
    UserCapturePolicy.Attach(form);object seen; if(windows.TryGetValue(form,out seen))continue;
    windows.Add(form,new object()); PowTheme.Apply(form);
   }} finally { applying=false; }
  };
 }
 internal static GraphicsPath Rounded(RectangleF bounds, float radius) {
  var path=new GraphicsPath(); float d=Math.Min(radius*2,Math.Min(bounds.Width,bounds.Height));
  path.AddArc(bounds.Left,bounds.Top,d,d,180,90);path.AddArc(bounds.Right-d,bounds.Top,d,d,270,90);
  path.AddArc(bounds.Right-d,bounds.Bottom-d,d,d,0,90);path.AddArc(bounds.Left,bounds.Bottom-d,d,d,90,90);path.CloseFigure();return path;
 }
 internal static void Navigation(TabControl tabs) {
  tabs.Alignment=TabAlignment.Top;tabs.Multiline=true;
  /* NavigationTabs draw their own folder-tab strip; keep their native header as an invisible sliver. */
  if(!(tabs is NavigationTabs)){tabs.SizeMode=TabSizeMode.Normal;tabs.ItemSize=new Size(0,36);tabs.Padding=new Point(24,9);}
  tabs.AccessibleName="Program of Works sections";
 }
 internal static bool Primary(string text) {
  return text=="Add entry" || text=="Apply" || text=="Apply deductions" || text=="Build preview" ||
   text=="Save project information" || text=="Save footing" || text=="Open / Link Excel" ||
   text=="Save Program of Works" || text=="Calculate";
 }
 static readonly Dictionary<string,int> headerWords=new Dictionary<string,int>(StringComparer.Ordinal);
 internal static int HeaderMinimum(string header){int width=60;foreach(string word in (header??"").Split(new[]{' ','\r','\n'},StringSplitOptions.RemoveEmptyEntries)){int measured;if(!headerWords.TryGetValue(word,out measured)){measured=TextRenderer.MeasureText(word,Strong).Width;headerWords.Add(word,measured);}width=Math.Max(width,measured);}return Math.Min(180,width+24);}
 internal static void Buffer(Control control) {
  if(SystemInformation.TerminalServerSession)return;
  var property=typeof(Control).GetProperty("DoubleBuffered",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
  if(property!=null)property.SetValue(control,true,null);
 }
}
}
