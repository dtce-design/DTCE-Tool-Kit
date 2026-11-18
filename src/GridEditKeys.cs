using System;using System.Runtime.CompilerServices;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// F2 or F4 edits the current cell with the caret at the end of its text, like Excel's F2 (owner, 17 November 2026).
static class GridEditKeys {
 static readonly ConditionalWeakTable<DataGridView,object> attached=new ConditionalWeakTable<DataGridView,object>();
 internal static void Attach(DataGridView g){if(g==null)return;object seen;if(attached.TryGetValue(g,out seen))return;attached.Add(g,new object());
  g.KeyDown+=(s,e)=>{if((e.KeyCode!=Keys.F2&&e.KeyCode!=Keys.F4)||e.Modifiers!=Keys.None||g.IsCurrentCellInEditMode)return;var cell=g.CurrentCell;if(cell==null||cell.ReadOnly||g.ReadOnly||cell is DataGridViewCheckBoxCell||cell is DataGridViewButtonCell)return;
   if(g.EditMode==DataGridViewEditMode.EditProgrammatically&&!(cell is DataGridViewTextBoxCell))return;e.Handled=e.SuppressKeyPress=true;if(!g.BeginEdit(false))return;var box=g.EditingControl as TextBox;if(box!=null){box.SelectionStart=box.TextLength;box.SelectionLength=0;}};}
}
}
