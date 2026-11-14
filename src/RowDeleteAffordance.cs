using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Row headers keep all existing data-column indices and saved schemas intact.
static class RowDeleteAffordance {
 static readonly ConditionalWeakTable<DataGridView,object> attached=new ConditionalWeakTable<DataGridView,object>();
 static bool CanDelete(DataGridView grid,int index){return !grid.ReadOnly&&grid.AllowUserToDeleteRows&&index>=0&&index<grid.Rows.Count&&!grid.Rows[index].IsNewRow;}
 internal static void Attach(DataGridView grid){object found;if(attached.TryGetValue(grid,out found))return;attached.Add(grid,new object());grid.RowHeadersVisible=true;grid.RowHeadersWidth=32;grid.RowHeadersWidthSizeMode=DataGridViewRowHeadersWidthSizeMode.DisableResizing;
  grid.CellPainting+=(s,e)=>{if(e.ColumnIndex!=-1||e.RowIndex<0)return;e.PaintBackground(e.ClipBounds,false);if(CanDelete(grid,e.RowIndex))TextRenderer.DrawText(e.Graphics,"×",grid.Font,e.CellBounds,PowTheme.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);e.Handled=true;};
  grid.CellToolTipTextNeeded+=(s,e)=>{if(e.ColumnIndex==-1&&CanDelete(grid,e.RowIndex))e.ToolTipText="Delete this row";};
  grid.CellMouseClick+=(s,e)=>{if(e.Button!=MouseButtons.Left||e.ColumnIndex!=-1||!CanDelete(grid,e.RowIndex))return;var row=grid.Rows[e.RowIndex];try{if(!grid.EndEdit())return;if(row.DataGridView==grid&&!row.IsNewRow)grid.Rows.Remove(row);}catch(Exception error){MessageBox.Show(grid.FindForm(),error.Message,"Delete row",MessageBoxButtons.OK,MessageBoxIcon.Information);}};
 }
}
}
