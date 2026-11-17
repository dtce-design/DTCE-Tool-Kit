using System;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// A wall row's status reflects only problems that involve that wall (owner, 17 November 2026): its own
// openings (by wall reference or opening link) and its own wall finishes. An error elsewhere in the POW no
// longer marks every wall "Review…".
partial class ProgramOfWorksForm {
 bool WallHasOpeningIssue(string wallId){if(string.IsNullOrEmpty(wallId))return false;var valid=new System.Collections.Generic.HashSet<string>(openings.Select(o=>o.Id),StringComparer.OrdinalIgnoreCase);
  foreach(var grid in new[]{wood,aluminum,manualOpeningsGrid})if(grid!=null)foreach(DataGridViewRow row in grid.Rows){if(row.IsNewRow||Empty(row))continue;if(string.Equals(Cell(row,2),wallId,StringComparison.OrdinalIgnoreCase)&&!valid.Contains(Cell(row,0)))return true;}
  foreach(var link in openingAssignments)if(string.Equals(link.Wall,wallId,StringComparison.OrdinalIgnoreCase)&&!valid.Contains(link.Schedule))return true;
  return false;}
 bool WallHasFinishIssue(string wallId){if(string.IsNullOrEmpty(wallId)||wallFinishes==null)return false;foreach(DataGridViewRow row in wallFinishes.Rows){if(row.IsNewRow||Empty(row))continue;if(string.Equals(Cell(row,3),wallId,StringComparison.OrdinalIgnoreCase)&&!(row.Tag is FinishItem))return true;}return false;}
}
}
