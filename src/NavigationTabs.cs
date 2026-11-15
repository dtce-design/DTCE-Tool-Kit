using System.Drawing;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Let the native tab control own header-row measurement, clipping and page bounds.
// Painting the whole control in UserPaint mode can drop wrapped header rows and
// leave a stale header-sized gap after resizing. PowTheme still draws each folder tab.
class NavigationTabs:TabControl {
 internal static bool KeyboardFocusVisible(TabControl tabs){var navigation=tabs as NavigationTabs;return navigation==null||navigation.ShowFocusCues;}
 public NavigationTabs(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,false);SetStyle(ControlStyles.ResizeRedraw,true);Alignment=TabAlignment.Top;Multiline=true;SizeMode=TabSizeMode.Normal;}
 protected override void OnSelectedIndexChanged(System.EventArgs e){base.OnSelectedIndexChanged(e);Invalidate();}
}
}
