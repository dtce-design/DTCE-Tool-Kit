using System;using System.Runtime.CompilerServices;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Uniform row height on POW measurement sheets (owner, 17 November 2026): long text no longer grows a row.
// Text that does not fit ends with "…" and the full text shows as a tooltip; input issues are also listed under the BOQ.
internal static partial class PowTheme {
 internal const int CompactRowHeight=36;
 static readonly ConditionalWeakTable<DataGridView,object> compactGrids=new ConditionalWeakTable<DataGridView,object>();
 internal static void CompactRows(DataGridView grid){if(grid==null)return;object marker;if(!compactGrids.TryGetValue(grid,out marker))compactGrids.Add(grid,new object());ApplyCompact(grid);}
 internal static bool IsCompact(DataGridView grid){object marker;return grid!=null&&compactGrids.TryGetValue(grid,out marker);}
 internal static void ApplyCompact(DataGridView grid){if(!IsCompact(grid))return;
  grid.AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.None;grid.AllowUserToResizeRows=false;grid.ShowCellToolTips=true;
  grid.DefaultCellStyle.WrapMode=DataGridViewTriState.False;grid.RowTemplate.Height=CompactRowHeight;grid.RowTemplate.MinimumHeight=CompactRowHeight;
  foreach(DataGridViewColumn column in grid.Columns)if(column.DefaultCellStyle.WrapMode==DataGridViewTriState.True)column.DefaultCellStyle.WrapMode=DataGridViewTriState.False;
  foreach(DataGridViewRow row in grid.Rows){if(row.DefaultCellStyle.WrapMode==DataGridViewTriState.True)row.DefaultCellStyle.WrapMode=DataGridViewTriState.False;row.MinimumHeight=CompactRowHeight;row.Height=CompactRowHeight;}}
}
partial class ProgramOfWorksForm {
 void CompactMeasurementRows(){foreach(var grid in Tables().Values)PowTheme.CompactRows(grid);foreach(var item in extraGrids.Values)PowTheme.CompactRows(item.Grid);}
}
}
