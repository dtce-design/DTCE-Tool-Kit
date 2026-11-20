using System;using System.Drawing;using System.Runtime.InteropServices;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* horizontal scrolling with the mouse (owner, 20 November 2026: "bat ayaw mag horizontal scroll sa mouse?"): the tilt wheel /
   touchpad side scroll (WM_MOUSEHWHEEL) and Shift + wheel scroll the grid or panel under the pointer sideways. WinForms grids
   and panels do not do this on their own. */
sealed class HorizontalWheel:IMessageFilter {
 const int WM_MOUSEWHEEL=0x020A,WM_MOUSEHWHEEL=0x020E;
 [DllImport("user32.dll")]static extern IntPtr WindowFromPoint(Point p);
 internal static void Install(){try{Application.AddMessageFilter(new HorizontalWheel());}catch(Exception){}}
 public bool PreFilterMessage(ref Message m){
  bool tilt=m.Msg==WM_MOUSEHWHEEL,shift=m.Msg==WM_MOUSEWHEEL&&(Control.ModifierKeys&Keys.Shift)==Keys.Shift;if(!tilt&&!shift)return false;
  int delta=(short)(((long)m.WParam>>16)&0xFFFF);if(shift)delta=-delta;/* tilt right / Shift + wheel down = scroll right */
  Control c=null;try{c=Control.FromChildHandle(WindowFromPoint(Cursor.Position));}catch(Exception){}if(c==null)c=Control.FromChildHandle(m.HWnd);
  int step=Math.Sign(delta)*Math.Max(40,Math.Abs(delta)*60/120);
  for(;c!=null;c=c.Parent){if(Scroll(c,step))return true;}
  return false;}
 internal static bool Scroll(Control c,int step){
  var grid=c as DataGridView;if(grid!=null){var bar=typeof(DataGridView).GetProperty("HorizontalScrollBar",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(grid,null) as ScrollBar;if(bar==null||!bar.Visible)return false;int max=Math.Max(0,bar.Maximum-bar.LargeChange+1);int v=Math.Max(0,Math.Min(max,grid.HorizontalScrollingOffset+step));if(v==grid.HorizontalScrollingOffset)return true;try{grid.HorizontalScrollingOffset=v;}catch(Exception){}return true;}
  var panel=c as ScrollableControl;if(panel!=null&&panel.AutoScroll&&panel.HorizontalScroll.Visible){var p=panel.AutoScrollPosition;panel.AutoScrollPosition=new Point(Math.Max(0,-p.X+step),-p.Y);return true;}
  return false;}
}
}
