using System;using System.Runtime.CompilerServices;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* Dropdown cells keep their value under the arrow keys and the mouse wheel (owner, 23 November 2026: "sa mga drop down box mejo mahirap
   or prone to error kasi kapag copy paste ko tapos down arrow bababa yung size"). While a dropdown cell is being edited and its list is
   closed, Up / Down (and Page Up / Page Down) finish the edit and move to the row above / below instead of changing the size, and the
   mouse wheel scrolls nothing. Alt+Down or F4 still opens the list; inside an open list the arrows choose as usual. */
static class ComboKeyGuard {
 static readonly ConditionalWeakTable<DataGridView,object> attached=new ConditionalWeakTable<DataGridView,object>();
 internal static void Attach(DataGridView grid){if(grid==null)return;object seen;if(attached.TryGetValue(grid,out seen))return;attached.Add(grid,new object());
  grid.EditingControlShowing+=(s,e)=>{var combo=e.Control as ComboBox;if(combo==null)return;combo.KeyDown-=OnKeyDown;combo.KeyDown+=OnKeyDown;combo.MouseWheel-=OnWheel;combo.MouseWheel+=OnWheel;};}
 static DataGridView GridOf(Control c){var editing=c as IDataGridViewEditingControl;if(editing!=null&&editing.EditingControlDataGridView!=null)return editing.EditingControlDataGridView;for(var p=c.Parent;p!=null;p=p.Parent){var g=p as DataGridView;if(g!=null)return g;}return null;}
 static void OnKeyDown(object sender,KeyEventArgs e){var combo=sender as ComboBox;if(combo==null||combo.DroppedDown||e.Alt||e.Control)return;
  int step=e.KeyCode==Keys.Down?1:e.KeyCode==Keys.Up?-1:e.KeyCode==Keys.PageDown?10:e.KeyCode==Keys.PageUp?-10:0;if(step==0)return;
  e.Handled=true;e.SuppressKeyPress=true;var grid=GridOf(combo);if(grid==null||grid.CurrentCell==null)return;int col=grid.CurrentCell.ColumnIndex,row=grid.CurrentCell.RowIndex;
  try{grid.EndEdit();}catch(InvalidOperationException){}
  int target=Math.Max(0,Math.Min(grid.Rows.Count-1,row+step));while(target>=0&&target<grid.Rows.Count&&!grid.Rows[target].Visible)target+=Math.Sign(step);
  if(target<0||target>=grid.Rows.Count||target==row)return;try{grid.CurrentCell=grid.Rows[target].Cells[col];}catch(InvalidOperationException){}}
 static void OnWheel(object sender,MouseEventArgs e){var combo=sender as ComboBox;var h=e as HandledMouseEventArgs;if(combo!=null&&h!=null&&!combo.DroppedDown)h.Handled=true;}
}
}
