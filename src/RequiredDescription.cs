using System;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* a row is estimated only when it is complete (owner, 23 November 2026: "naeestimate pa din kahit di kumpleto"; "paki check na din sa
   lahat"): in every Bill of Quantities sheet with a Description / location column, that column must be filled; until then the row's
   status says what is missing and the row is left out of the quantities */
partial class ProgramOfWorksForm {
 internal const string DescriptionMissing="Enter the description / location.";
 static void RequireDescription(DataGridViewRow row){var g=row.DataGridView;if(g==null)return;foreach(DataGridViewColumn c in g.Columns)if(c.HeaderText=="Description / location"){var v=row.Cells[c.Index].Value;if(v==null||Convert.ToString(v).Trim()=="")throw new Exception(DescriptionMissing);return;}}
}
}
