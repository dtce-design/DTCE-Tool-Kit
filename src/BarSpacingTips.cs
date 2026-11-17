using System;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Clearer spacing / bar-count pairs (owner, 17 November 2026): enter ONE of the two columns per bar direction.
partial class ProgramOfWorksForm {
 void ExplainBarSpacingColumns(){foreach(var grid in Tables().Values)foreach(DataGridViewColumn c in grid.Columns){string h=c.HeaderText.ToLowerInvariant();
  if(h.EndsWith("spacing mm")&&(h.StartsWith("vertical")||h.StartsWith("horizontal")||h.StartsWith("main")||h.StartsWith("distribution")))c.ToolTipText="Center-to-center spacing of these bars in mm, e.g. 200. Leave blank if you enter the Count instead. The app computes the bar count from the length.";
  else if(h.EndsWith("count / layer")||h.EndsWith("count / unit"))c.ToolTipText="Number of pieces of these bars (per layer / per unit), e.g. 3. Enter Count OR spacing, not both.";}}
}
}
