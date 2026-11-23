using System;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* a row is estimated only when it is complete (owner, 23 November 2026: "naeestimate pa din kahit di kumpleto"; "paki check na din sa
   lahat"): in every Bill of Quantities sheet with a Description / location column, that column must be filled; until then the row's
   status says what is missing and the row is left out of the quantities */
partial class ProgramOfWorksForm {
 internal const string DescriptionMissing="Enter the description / location.";
 internal const string DeductionMissing="Enter the deduction in m2 (0 if none).";
 /* the deduction is an input too (owner, 23 November 2026: "pati yung sa deduction dapat may input para sa status is estimated"): a
    blank deduction is not taken as zero; 0 is entered when there is nothing to deduct */
 static void RequireDeduction(DataGridViewRow row,int column){if(column<0||column>=row.Cells.Count)return;var v=row.Cells[column].Value;if(v==null||Convert.ToString(v).Trim()=="")throw new Exception(DeductionMissing);}
 static void RequireDescription(DataGridViewRow row){var g=row.DataGridView;if(g==null)return;foreach(DataGridViewColumn c in g.Columns)if(c.HeaderText=="Description / location"){var v=row.Cells[c.Index].Value;if(v==null||Convert.ToString(v).Trim()=="")throw new Exception(DescriptionMissing);return;}}
}
}
