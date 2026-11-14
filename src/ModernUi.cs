using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace BeamScheduleFormatter {
// Shared desktop presentation only. No calculation, persistence or access decisions.
static class ModernUi {
 internal static readonly Font Body = new Font("Segoe UI", 9.5f);
 internal static readonly Font Strong = new Font("Segoe UI", 9.5f, FontStyle.Bold);
 internal static readonly Font Heading = new Font("Segoe UI", 20f, FontStyle.Bold);
 internal static readonly Color Surface = Color.White;
 internal static readonly Color Input = Color.FromArgb(247, 251, 255);
 internal static readonly Color Selection = Color.FromArgb(235, 243, 252);
 internal static readonly Color Soft = Color.FromArgb(245, 245, 245);
 static readonly ConditionalWeakTable<Form, object> windows = new ConditionalWeakTable<Form, object>();
 static bool installed, applying;
 internal static void Install() {
  if(installed)return; installed=true;
  // Includes modal dialogs and embedded tools which do not call AppBrand.Apply.
  // Only new forms are traversed; do not walk every table on every idle event.
  Application.Idle += delegate {
   if(applying)return; applying=true;
   try { foreach(Form form in Application.OpenForms) {
    object seen; if(windows.TryGetValue(form,out seen))continue;
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
  tabs.Alignment=TabAlignment.Top;tabs.Multiline=true;tabs.SizeMode=TabSizeMode.Normal;
  tabs.ItemSize=new Size(0,36);tabs.Padding=new Point(16,9);
  tabs.AccessibleName="Program of Works sections";
 }
 internal static bool Primary(string text) {
  return text=="Apply" || text=="Apply deductions" || text=="Build preview" ||
   text=="Save project information" || text=="Save footing" || text=="Open / Link Excel" ||
   text=="Save Program of Works" || text=="Calculate";
 }
 internal static void Buffer(Control control) {
  if(SystemInformation.TerminalServerSession)return;
  var property=typeof(Control).GetProperty("DoubleBuffered",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
  if(property!=null)property.SetValue(control,true,null);
 }
}
}
