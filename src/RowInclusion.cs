using System;using System.Collections.Generic;using System.Drawing;using System.Linq;using System.Windows.Forms;using System.Xml;
namespace BeamScheduleFormatter {
// Include / exclude a row from the totals (owner, 18 November 2026: "lagyan ng additional check box sa pinaka left after
// X to include or exclude from totals"). The box is drawn in the row header next to the delete x, so no data column
// index changes. An excluded row keeps its inputs: they are held aside and the row's cells are blank while it is
// excluded, so every calculation, total, BOQ line and Excel output treats it as an empty row. The held values are
// still shown (grey) and are saved with the project (entry attribute x="1").
sealed class RowExclusion {internal object[] Values;}
partial class ProgramOfWorksForm {
 static readonly HashSet<string> NoInclusionTables=new HashSet<string>(StringComparer.Ordinal){"RoofProfiles","FinishSchedule","InsulationRolls"};
 readonly List<DataGridView> inclusionGrids=new List<DataGridView>();
 static bool IsExcludedRow(DataGridViewRow row){return row!=null&&row.HeaderCell.Tag is RowExclusion;}
 IEnumerable<DataGridView> InclusionCandidates(){foreach(var item in Tables())if(item.Value!=null&&!NoInclusionTables.Contains(item.Key))yield return item.Value;foreach(var item in extraGrids.Values)if(item.Grid!=null)yield return item.Grid;}
 void InstallRowInclusion(){foreach(var grid in InclusionCandidates().Distinct().ToList()){if(inclusionGrids.Contains(grid))continue;inclusionGrids.Add(grid);var g=grid;
   RowDeleteAffordance.EnableInclude(g,row=>!IsExcludedRow(row),row=>SetRowIncluded(row,IsExcludedRow(row)));
   g.CellBeginEdit+=(s,e)=>{if(e.RowIndex>=0&&e.RowIndex<g.Rows.Count&&IsExcludedRow(g.Rows[e.RowIndex]))e.Cancel=true;};
   g.CellFormatting+=(s,e)=>{if(e.RowIndex<0||e.ColumnIndex<0||e.RowIndex>=g.Rows.Count)return;var x=g.Rows[e.RowIndex].HeaderCell.Tag as RowExclusion;if(x==null)return;e.CellStyle.ForeColor=Color.FromArgb(150,150,150);if(g.Columns[e.ColumnIndex].HeaderText=="Status"){e.Value="Excluded from totals";e.FormattingApplied=true;return;}
    if(e.ColumnIndex<x.Values.Length&&x.Values[e.ColumnIndex]!=null&&!(g.Rows[e.RowIndex].Cells[e.ColumnIndex] is DataGridViewCheckBoxCell)){e.Value=Convert.ToString(x.Values[e.ColumnIndex],System.Globalization.CultureInfo.InvariantCulture);e.FormattingApplied=true;}};}}
 void SetRowIncluded(DataGridViewRow row,bool include){if(row==null||row.IsNewRow||row.DataGridView==null)return;if(include==!IsExcludedRow(row))return;
  bool before=filling;filling=true;GridEditHistory.Quiet++;try{if(include)IncludeRow(row);else ExcludeRow(row);}finally{GridEditHistory.Quiet--;filling=before;}
  if(!filling){Recalculate();dashboardDirty=reportPreviewDirty=true;}}
 static void ExcludeRow(DataGridViewRow row){var values=new object[row.Cells.Count];for(int c=0;c<row.Cells.Count;c++){var cell=row.Cells[c];if(cell is DataGridViewButtonCell)continue;values[c]=cell.Value;cell.Value=null;}row.HeaderCell.Tag=new RowExclusion{Values=values};row.DataGridView.InvalidateRow(row.Index);}
 static void IncludeRow(DataGridViewRow row){var x=row.HeaderCell.Tag as RowExclusion;row.HeaderCell.Tag=null;if(x!=null)for(int c=0;c<x.Values.Length&&c<row.Cells.Count;c++)if(!(row.Cells[c] is DataGridViewButtonCell)&&x.Values[c]!=null)row.Cells[c].Value=x.Values[c];row.DataGridView.InvalidateRow(row.Index);}
 /* Saving: the held values are put back for the moment the project data is written, then the rows are blank again. */
 T WithExcludedValues<T>(Func<T> work){var rows=inclusionGrids.SelectMany(g=>g.Rows.Cast<DataGridViewRow>()).Where(IsExcludedRow).ToList();if(rows.Count==0)return work();
  bool before=filling;filling=true;GridEditHistory.Quiet++;try{foreach(var row in rows){var x=(RowExclusion)row.HeaderCell.Tag;for(int c=0;c<x.Values.Length&&c<row.Cells.Count;c++)if(!(row.Cells[c] is DataGridViewButtonCell))row.Cells[c].Value=x.Values[c];}
   try{return work();}finally{foreach(var row in rows){var x=(RowExclusion)row.HeaderCell.Tag;for(int c=0;c<x.Values.Length&&c<row.Cells.Count;c++)if(!(row.Cells[c] is DataGridViewButtonCell))row.Cells[c].Value=null;}}}
  finally{GridEditHistory.Quiet--;filling=before;}}
 static void WriteRowExcluded(DataGridViewRow row,XmlElement entry){WriteRowMark(row,entry);if(IsExcludedRow(row))entry.SetAttribute("x","1");}
 static void ReadRowExcluded(DataGridViewRow row,XmlElement entry){ReadRowMark(row,entry);if(entry.GetAttribute("x")=="1"&&!IsExcludedRow(row)){GridEditHistory.Quiet++;try{ExcludeRow(row);}finally{GridEditHistory.Quiet--;}}}
}
}
