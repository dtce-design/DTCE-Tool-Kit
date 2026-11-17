using System;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Clearer spacing / bar-count pairs (owner, 17 November 2026): enter ONE of the two columns per bar direction.
partial class ProgramOfWorksForm {
 void ExplainBarSpacingColumns(){foreach(var grid in Tables().Values)foreach(DataGridViewColumn c in grid.Columns){string h=c.HeaderText.ToLowerInvariant();
  if(h.Contains("bar spacing mm (or use"))c.ToolTipText="Center-to-center spacing of these bars in mm, e.g. 200. Leave blank if you enter the number of bars in the next column. The app computes the bar count from the length.";
  else if(h.Contains("bars per layer (or use spacing)")||h.Contains("bars per unit (or use spacing)"))c.ToolTipText="Number of these bars (per layer / per unit), e.g. 3. Use this INSTEAD of spacing; leave it blank when spacing is entered.";}}
}
}
