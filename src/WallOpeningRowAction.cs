using System;using System.Drawing;using System.Windows.Forms;
namespace BeamScheduleFormatter {
static class WallOpeningRowAction {
 internal static void Attach(DataGridView grid,Action<DataGridViewRow> edit){
  const string name="EditWallOpeningsAction";if(grid.Columns.Contains(name))return;
  var column=new DataGridViewButtonColumn{Name=name,HeaderText="Openings",Text="Add / edit openings",UseColumnTextForButtonValue=true,ReadOnly=true,Width=42,MinimumWidth=38,AutoSizeMode=DataGridViewAutoSizeColumnMode.None,SortMode=DataGridViewColumnSortMode.NotSortable,FlatStyle=FlatStyle.Standard};
  // Append the UI column so numeric input indexes and saved project schemas stay intact.
  grid.Columns.Add(column);column.DisplayIndex=2;column.Frozen=true;column.HeaderCell.ToolTipText="Add / Edit Openings";
  grid.CellPainting+=(s,e)=>{
   if(e.ColumnIndex!=column.Index)return;
   bool placeholder=e.RowIndex>=0&&grid.Rows[e.RowIndex].IsNewRow;
   e.Paint(e.ClipBounds,e.PaintParts&~DataGridViewPaintParts.ContentForeground&~DataGridViewPaintParts.Focus);
   if(!placeholder){
    var rect=e.CellBounds;int x=rect.X+(rect.Width-16)/2,y=rect.Y+(rect.Height-16)/2;
    using(var pen=new Pen(PowTheme.Ink,1.4f)){
     e.Graphics.DrawRectangle(pen,x+1,y+1,12,13);e.Graphics.DrawLine(pen,x+7,y+1,x+7,y+14);e.Graphics.DrawLine(pen,x+1,y+7,x+13,y+7);
     e.Graphics.DrawLine(pen,x+10,y+14,x+16,y+8);e.Graphics.DrawLine(pen,x+11,y+15,x+17,y+9);
    }
    if(e.RowIndex>=0&&grid.Focused&&grid.CurrentCell!=null&&grid.CurrentCell.RowIndex==e.RowIndex&&grid.CurrentCell.ColumnIndex==column.Index)FocusHighlight.Draw(e.Graphics,Rectangle.Inflate(rect,-3,-3),WorksheetColors.Focus);
   }
   e.Handled=true;
  };
  grid.CellContentClick+=(s,e)=>{
   if(e.ColumnIndex!=column.Index||e.RowIndex<0)return;var row=grid.Rows[e.RowIndex];if(row.IsNewRow)return;
   try{edit(row);}catch(Exception error){MessageBox.Show(grid.FindForm(),error.Message,"Wall openings",MessageBoxButtons.OK,MessageBoxIcon.Information);}
  };
  grid.CellToolTipTextNeeded+=(s,e)=>{if(e.ColumnIndex==column.Index&&e.RowIndex>=0&&!grid.Rows[e.RowIndex].IsNewRow)e.ToolTipText="Add / Edit Openings — wall "+Convert.ToString(grid.Rows[e.RowIndex].Cells[0].Value)+" only.";};
 }
}
}
