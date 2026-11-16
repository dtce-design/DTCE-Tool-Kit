using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Paint affordances only; native typing, selection and keyboard handling remain intact.
static class DropdownChrome {
 static readonly ConditionalWeakTable<ComboBox,ChromeWindow> controls=new ConditionalWeakTable<ComboBox,ChromeWindow>();
 static readonly ConditionalWeakTable<DataGridView,object> grids=new ConditionalWeakTable<DataGridView,object>();
 internal static void Attach(ComboBox combo){ChromeWindow found;if(controls.TryGetValue(combo,out found))return;controls.Add(combo,new ChromeWindow(combo));DropdownCopy.Attach(combo);}
 static void Draw(Graphics graphics,Rectangle bounds,bool focused,bool enabled,float scale){if(bounds.Width<26||bounds.Height<14)return;var p=DesktopAppearance.Palette;int arrow=Math.Max(20,(int)(22*scale));var button=new Rectangle(bounds.Right-arrow,bounds.Top+1,arrow-1,bounds.Height-2);using(var fill=new SolidBrush(p.TabSub))graphics.FillRectangle(fill,button);using(var border=new Pen(enabled?(focused?p.Link:p.Muted):p.Line,focused?2:1)){graphics.DrawRectangle(border,bounds.Left,bounds.Top,bounds.Width-1,bounds.Height-1);graphics.DrawLine(border,button.Left,button.Top+2,button.Left,button.Bottom-2);}int cx=button.Left+button.Width/2,cy=button.Top+button.Height/2;int half=Math.Max(3,(int)(4*scale));using(var fill=new SolidBrush(enabled?p.Ink:p.Muted))graphics.FillPolygon(fill,new[]{new Point(cx-half,cy-2),new Point(cx+half,cy-2),new Point(cx,cy+half-1)});}
 sealed class ChromeWindow:NativeWindow {
  readonly ComboBox owner;
  internal ChromeWindow(ComboBox combo){owner=combo;if(owner is DataGridViewComboBoxEditingControl){owner.FlatStyle=FlatStyle.Flat;owner.Margin=Padding.Empty;return;}owner.HandleCreated+=(s,e)=>AssignHandle(owner.Handle);owner.HandleDestroyed+=(s,e)=>ReleaseHandle();if(owner.IsHandleCreated)AssignHandle(owner.Handle);owner.GotFocus+=(s,e)=>owner.Invalidate();owner.LostFocus+=(s,e)=>owner.Invalidate();owner.DropDownClosed+=(s,e)=>owner.Invalidate();owner.EnabledChanged+=(s,e)=>owner.Invalidate();}
  protected override void WndProc(ref Message message){base.WndProc(ref message);if((message.Msg==0xF||message.Msg==0x85)&&owner.IsHandleCreated&&!owner.IsDisposed)using(var g=Graphics.FromHwnd(owner.Handle))Draw(g,owner.ClientRectangle,owner.ContainsFocus,owner.Enabled,owner.DeviceDpi/96f);}
 }
 internal static void Attach(DataGridView grid){object marker;if(grids.TryGetValue(grid,out marker))return;grids.Add(grid,new object());grid.CellPainting+=(s,e)=>{if(e.RowIndex<0||e.ColumnIndex<0)return;var cell=grid.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewComboBoxCell;if(cell==null||cell.ReadOnly||grid.ReadOnly||grid.IsCurrentCellInEditMode&&grid.CurrentCell==cell)return;if(!e.Handled)e.Paint(e.ClipBounds,e.PaintParts);var bounds=Rectangle.Inflate(e.CellBounds,-2,-3);bool focused=grid.ContainsFocus&&grid.CurrentCell==cell;Draw(e.Graphics,bounds,focused,grid.Enabled,grid.DeviceDpi/96f);e.Handled=true;};}
}
}
