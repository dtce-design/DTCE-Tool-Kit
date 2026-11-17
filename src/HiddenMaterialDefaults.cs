using System;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Rows whose hidden material cells (concrete MPa, rebar grades) were left blank take the values from their sheet's
// Settings (owner report 17 November 2026: RC wall rows stuck on "Check Grade <=12 mm" with no visible cell to fix).
// Values already present are never changed; "Apply to all" in Settings still updates every row.
partial class ProgramOfWorksForm {
 void FillBlankHiddenMaterials(){
  Fill(rcWalls,new[]{19,20,21},()=>new[]{rcWallFc,rcWallSmall,rcWallLarge});
  Fill(canopies,new[]{16,17,18},()=>new[]{canopyFc,canopySmall,canopyLarge});
  Fill(footings,new[]{25,26,27},()=>new[]{footingFc,footingSmall,footingLarge});}
 void Fill(DataGridView grid,int[] columns,Func<ComboBox[]> settings){if(grid==null)return;var source=settings();if(source==null)return;
  foreach(DataGridViewRow row in grid.Rows){if(row.IsNewRow||Empty(row))continue;for(int i=0;i<columns.Length&&i<source.Length;i++){int c=columns[i];if(c>=grid.Columns.Count||source[i]==null||grid.Columns[c].Visible)continue;if(Cell(row,c)==""&&source[i].Text!="")row.Cells[c].Value=source[i].Text;}}}
}
}
