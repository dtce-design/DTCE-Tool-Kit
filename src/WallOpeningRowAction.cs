using System;using System.Windows.Forms;
namespace BeamScheduleFormatter {
static class WallOpeningRowAction {
 internal static void Attach(DataGridView grid,Action<DataGridViewRow> edit){
  const string name="EditWallOpeningsAction";if(grid.Columns.Contains(name))return;
  var column=new DataGridViewButtonColumn{Name=name,HeaderText="Openings",Text="Add / edit openings",UseColumnTextForButtonValue=true,ReadOnly=true,Width=155,MinimumWidth=145,SortMode=DataGridViewColumnSortMode.NotSortable,FlatStyle=FlatStyle.Standard};
  // Append the UI column so numeric input indexes and saved project schemas stay intact.
  grid.Columns.Add(column);column.DisplayIndex=2;column.Frozen=true;
  grid.CellPainting+=(s,e)=>{if(e.ColumnIndex!=column.Index||e.RowIndex<0||!grid.Rows[e.RowIndex].IsNewRow)return;e.PaintBackground(e.ClipBounds,false);e.Paint(e.ClipBounds,DataGridViewPaintParts.Border);e.Handled=true;};
  grid.CellContentClick+=(s,e)=>{
   if(e.ColumnIndex!=column.Index||e.RowIndex<0)return;var row=grid.Rows[e.RowIndex];if(row.IsNewRow)return;
   try{edit(row);}catch(Exception error){MessageBox.Show(grid.FindForm(),error.Message,"Wall openings",MessageBoxButtons.OK,MessageBoxIcon.Information);}
  };
  grid.CellToolTipTextNeeded+=(s,e)=>{if(e.ColumnIndex==column.Index&&e.RowIndex>=0&&!grid.Rows[e.RowIndex].IsNewRow)e.ToolTipText="Edit opening deductions for wall "+Convert.ToString(grid.Rows[e.RowIndex].Cells[0].Value)+" only.";};
 }
}
}
