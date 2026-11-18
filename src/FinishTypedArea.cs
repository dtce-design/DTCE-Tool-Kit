using System;using System.Drawing;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Wall / ceiling / floor finishes: "OR Area m2" (owner, 18 November 2026: "or mag-type ng area"). Typed area x Qty replaces
// Length x Height / Width. A negative typed area is an adjustment that closes or zeroes an excess in its finish group;
// on Wall Finishes it is entered without a Wall ID ("may option na pwede mag-input no wall id, pang close or pang zero
// lang yung excess amount"). Saved as input column 15 (walls) / 11 (ceilings, floors); older files open with it blank.
partial class ProgramOfWorksForm {
 internal const string FinishTypedAreaColumn="FinishTypedArea";
 void AddFinishTypedAreaColumn(DataGridView g,bool wall){int index=g.Columns.Add(FinishTypedAreaColumn,"OR Area m2");var c=g.Columns[index];c.Width=110;c.DefaultCellStyle.BackColor=ModernUi.Input;c.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;
  c.ToolTipText="Type the area instead of Length x "+(wall?"Height":"Width")+" (area x Qty). A negative area is an adjustment that reduces the total of its finish group (e.g. to close an excess)"+(wall?"; leave Wall ID blank for it.":".");
  try{c.DisplayIndex=Math.Min(g.Columns.Count-1,g.Columns[wall?9:5].DisplayIndex);}catch(InvalidOperationException){}
  g.Columns[3].ToolTipText=wall?"Wall this finish is on. Leave blank for an entry that is not on a CHB wall (Dimensions or typed area), e.g. an adjustment.":g.Columns[3].ToolTipText;}
 static string TypedFinishArea(DataGridViewRow row){return row.DataGridView!=null&&row.DataGridView.Columns.Contains(FinishTypedAreaColumn)?Cell(row,row.DataGridView.Columns[FinishTypedAreaColumn].Index):"";}
}
}
