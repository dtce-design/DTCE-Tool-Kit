using System;
using System.Drawing;using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Row headers keep all existing data-column indices and saved schemas intact.
static class RowDeleteAffordance {
 static readonly ConditionalWeakTable<DataGridView,object> attached=new ConditionalWeakTable<DataGridView,object>();
 static bool CanDelete(DataGridView grid,int index){return !grid.ReadOnly&&grid.AllowUserToDeleteRows&&index>=0&&index<grid.Rows.Count&&!grid.Rows[index].IsNewRow&&grid.Rows[index].Cells.Cast<DataGridViewCell>().Any(cell=>!cell.ReadOnly);}
 /* Optional include box beside the x (owner, 18 November 2026): unchecked rows are left out of the totals. */
 sealed class Include {internal Func<DataGridViewRow,bool> IsIncluded;internal Action<DataGridViewRow> Toggle;}
 static readonly ConditionalWeakTable<DataGridView,Include> includes=new ConditionalWeakTable<DataGridView,Include>();
 const int BoxLeft=26;
 internal static void EnableInclude(DataGridView grid,Func<DataGridViewRow,bool> isIncluded,Action<DataGridViewRow> toggle){Include found;if(includes.TryGetValue(grid,out found))return;includes.Add(grid,new Include{IsIncluded=isIncluded,Toggle=toggle});Attach(grid);grid.RowHeadersWidth=54;}
 internal static void Attach(DataGridView grid){object found;if(attached.TryGetValue(grid,out found))return;attached.Add(grid,new object());grid.RowHeadersVisible=true;grid.RowHeadersWidth=32;grid.RowHeadersWidthSizeMode=DataGridViewRowHeadersWidthSizeMode.DisableResizing;
  grid.CellPainting+=(s,e)=>{if(e.ColumnIndex!=-1||e.RowIndex<0)return;e.PaintBackground(e.ClipBounds,false);if(CanDelete(grid,e.RowIndex)){Include inc;bool hasBox=includes.TryGetValue(grid,out inc);var xr=hasBox?new Rectangle(e.CellBounds.X,e.CellBounds.Y,BoxLeft,e.CellBounds.Height):e.CellBounds;
   TextRenderer.DrawText(e.Graphics,"×",grid.Font,xr,PowTheme.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);
   if(hasBox){bool on=inc.IsIncluded(grid.Rows[e.RowIndex]);int size=14;var box=new Rectangle(e.CellBounds.X+BoxLeft+(e.CellBounds.Width-BoxLeft-size)/2,e.CellBounds.Y+(e.CellBounds.Height-size)/2,size,size);
    using(var back=new SolidBrush(on?PowTheme.Teal:Color.White))e.Graphics.FillRectangle(back,box);using(var pen=new Pen(on?PowTheme.Teal:PowTheme.Muted))e.Graphics.DrawRectangle(pen,box);
    if(on){var old=e.Graphics.SmoothingMode;e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using(var pen=new Pen(Color.White,2f))e.Graphics.DrawLines(pen,new[]{new Point(box.X+3,box.Y+7),new Point(box.X+6,box.Y+10),new Point(box.X+11,box.Y+4)});e.Graphics.SmoothingMode=old;}}}
   e.Handled=true;};
  grid.CellToolTipTextNeeded+=(s,e)=>{if(e.ColumnIndex==-1&&CanDelete(grid,e.RowIndex)){Include inc;e.ToolTipText=includes.TryGetValue(grid,out inc)?"x: delete this row (confirmation required).  Box: include this row in the totals (untick to leave it out; its inputs are kept).":"Delete this row (confirmation required). Ctrl/Shift-click row headers to select multiple rows; right-click to delete the selection.";}};
  GridEditHistory.Attach(grid);
  grid.CellMouseClick+=(s,e)=>{if(e.Button!=MouseButtons.Left||e.ColumnIndex!=-1||!CanDelete(grid,e.RowIndex)||(Control.ModifierKeys&(Keys.Control|Keys.Shift))!=0)return;Include inc;if(includes.TryGetValue(grid,out inc)&&e.X>=BoxLeft){inc.Toggle(grid.Rows[e.RowIndex]);grid.InvalidateCell(-1,e.RowIndex);return;}try{GridEditHistory.Attach(grid).DeleteRow(e.RowIndex);}catch(Exception error){MessageBox.Show(grid.FindForm(),error.Message,"Delete row",MessageBoxButtons.OK,MessageBoxIcon.Information);}};
 }
}
}
