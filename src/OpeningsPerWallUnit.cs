using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* opening quantities count per unit of the wall group (owner, 21 November 2026: "kung may 2 units na ganon tapos sa opening ko nilagay
   ko qty is 1 bali 2 units ng opening na ganon ang need"): Wall Qty 2 x opening Qty 1 = 2 openings deducted, 2 lintels */
partial class ProgramOfWorksForm {
 bool legacyOpeningTotals;HashSet<string> manualOpeningTotals=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 /* units of a wall group: its Wall Qty (1 when blank) */
 int WallUnits(string wallId){if(string.IsNullOrWhiteSpace(wallId))return 1;foreach(var grid in MasonryGrids())foreach(DataGridViewRow r in grid.Rows){if(r.IsNewRow||!string.Equals(Cell(r,0).Trim(),wallId.Trim(),StringComparison.OrdinalIgnoreCase))continue;int q;return int.TryParse(Cell(r,4),NumberStyles.Integer,CultureInfo.InvariantCulture,out q)&&q>0?q:1;}return 1;}
 /* files saved before v6.0.35 held the total for the group: divided by the Wall Qty where it divides, else kept as a total */
 void ConvertLegacyOpeningTotals(){if(!legacyOpeningTotals)return;legacyOpeningTotals=false;
  foreach(var l in openingAssignments.Where(x=>x.Total)){int u=WallUnits(l.Wall);if(u<=1){l.Total=false;continue;}if(l.Quantity%u==0){l.Quantity/=u;l.Total=false;}}
  bool before=filling;filling=true;try{foreach(DataGridViewRow r in manualOpeningsGrid.Rows){if(r.IsNewRow)continue;int u=WallUnits(Cell(r,2)),q;if(u<=1||!int.TryParse(Cell(r,6),NumberStyles.Integer,CultureInfo.InvariantCulture,out q))continue;if(q%u==0)r.Cells[6].Value=(q/u).ToString(CultureInfo.InvariantCulture);else manualOpeningTotals.Add(Cell(r,0));}}finally{filling=before;}}
}
}
