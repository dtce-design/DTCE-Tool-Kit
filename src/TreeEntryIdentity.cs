using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Text.RegularExpressions;using System.Windows.Forms;
namespace BeamScheduleFormatter {
static class TreeEntryIdentity {public static string Next(IEnumerable<string> existing){long max=0;var used=new HashSet<string>(existing.Where(s=>!string.IsNullOrWhiteSpace(s)),StringComparer.OrdinalIgnoreCase);foreach(string id in used){var match=Regex.Match(id,@"^T(\d+)$",RegexOptions.IgnoreCase);long n;if(match.Success&&long.TryParse(match.Groups[1].Value,out n))max=Math.Max(max,n);}string candidate;do{max=checked(max+1);candidate="T"+max.ToString("000",CultureInfo.InvariantCulture);}while(used.Contains(candidate));return candidate;}}
partial class ProgramOfWorksForm {
 void ConfigureAutomaticTrees(DataGridView grid){grid.Columns[0].ReadOnly=true;grid.Columns[3].Visible=false;grid.Columns[4].Visible=false;grid.RowsAdded+=(s,e)=>{if(filling||initializingWorks)return;for(int i=e.RowIndex;i<e.RowIndex+e.RowCount&&i<grid.Rows.Count;i++)if(!grid.Rows[i].IsNewRow)AssignTreeIdentity(grid.Rows[i]);};}
 void AssignTreeIdentity(DataGridViewRow row){if(Cell(row,0)=="")row.Cells[0].Value=TreeEntryIdentity.Next(row.DataGridView.Rows.Cast<DataGridViewRow>().Where(r=>!r.IsNewRow&&r!=row).Select(r=>Cell(r,0)));row.Cells[3].Value=true;}
}
}
