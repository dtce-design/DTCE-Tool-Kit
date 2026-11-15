using System;using System.Drawing;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Keep native tab measurement, wrapping, hit testing, accessibility and keyboard
// navigation. Paint the complete strip so unused tab space is dark as well.
class NavigationTabs:TabControl {
 internal static bool KeyboardFocusVisible(TabControl tabs){var navigation=tabs as NavigationTabs;return navigation==null||navigation.ShowFocusCues;}
 protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);Invalidate();}
 public NavigationTabs(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);}
 protected override void OnPaintBackground(PaintEventArgs e){e.Graphics.Clear(PowTheme.Background);}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(PowTheme.Background);int depth=0;for(Control parent=Parent;parent!=null;parent=parent.Parent)if(parent is TabControl)depth++;var area=DisplayRectangle;using(var strip=new SolidBrush(ModernUi.Surface))e.Graphics.FillRectangle(strip,0,0,Width,Math.Max(0,area.Top));for(int i=0;i<TabCount;i++){var rect=GetTabRect(i);if(rect.Width<=0||!e.ClipRectangle.IntersectsWith(rect))continue;OnDrawItem(new DrawItemEventArgs(e.Graphics,Font,rect,i,i==SelectedIndex?DrawItemState.Selected:DrawItemState.Default));}var page=DisplayRectangle;if(page.Width>0&&page.Height>0)using(var pen=new Pen(PowTheme.Line))e.Graphics.DrawRectangle(pen,page.X-1,page.Y-1,page.Width+1,page.Height+1);}
 protected override void OnSelectedIndexChanged(EventArgs e){base.OnSelectedIndexChanged(e);Invalidate();}
}
}
