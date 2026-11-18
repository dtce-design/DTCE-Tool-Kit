using System;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Masonry sheets show how many sides are plastered and painted (owner, 17 November 2026). The columns existed
// (default "2 sides" / "Same as plaster") but were hidden. Order: … Net area | Plaster sides | Net plaster m2 |
// Masonry paint sides | Masonry paint m2 | Status.
partial class ProgramOfWorksForm {
 void ShowMasonryFinishSides(){
  foreach(DataGridView grid in MasonryGrids()){if(grid.Columns.Count<20)continue;
   foreach(int c in new[]{12,15,16,17})grid.Columns[c].Visible=true;
   grid.Columns[12].ToolTipText="Plastered faces of this wall: one side (A or B) or both sides.";
   grid.Columns[16].ToolTipText="Painted faces: same as plaster, none, one side (A or B) or both sides.";
   grid.Columns[15].ReadOnly=grid.Columns[17].ReadOnly=true;
   int at=grid.Columns[9].DisplayIndex;foreach(int c in new[]{12,15,16,17,11})grid.Columns[c].DisplayIndex=++at>=grid.Columns.Count?grid.Columns.Count-1:at;}
 }
}
}
