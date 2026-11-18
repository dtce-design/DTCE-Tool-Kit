using System;using System.Drawing;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Sticky pay item in Bill of Quantities (owner, 18 November 2026): while the rows of an expanded pay item scroll by, the
// pay item row stays pinned under the column headers; it is released when its last sub row has scrolled past. Click
// the pinned row to jump back to the pay item.
static class BoqStickyItem {
 // The pay item to pin when the first visible row is a sub row of an item whose own row has scrolled out of view.
 internal static DataGridViewRow PinnedRow(DataGridView grid,int firstVisible){if(grid.RowCount==0||firstVisible<=0||firstVisible>=grid.RowCount)return null;var first=grid.Rows[firstVisible];var node=first.HeaderCell.Tag as BoqSourceNode;if(node==null||!node.Detail)return null;
  for(int i=firstVisible-1;i>=0;i--){var candidate=grid.Rows[i].HeaderCell.Tag as BoqSourceNode;if(candidate!=null&&!candidate.Detail&&candidate.Key==node.Key)return grid.Rows[i];}return null;}
 internal static void Attach(DataGridView grid){
  Func<DataGridViewRow> pinned=()=>PinnedRow(grid,grid.FirstDisplayedScrollingRowIndex);
  Func<int> top=()=>grid.ColumnHeadersVisible?grid.ColumnHeadersHeight:0;
  grid.Paint+=(s,e)=>{DataGridViewRow row;try{row=pinned();}catch(Exception){return;}if(row==null)return;var g=e.Graphics;int y=top();int h=Math.Max(row.Height,grid.RowTemplate.Height);
   var band=new Rectangle(0,y,grid.DisplayRectangle.Width,h);using(var back=new SolidBrush(DesktopAppearance.Palette.Dark?Color.FromArgb(44,52,62):Color.FromArgb(230,238,247)))g.FillRectangle(back,band);
   if(grid.RowHeadersVisible)TextRenderer.DrawText(g,"▼",grid.Font,new Rectangle(0,y,grid.RowHeadersWidth,h),PowTheme.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
   var font=row.DefaultCellStyle.Font??grid.Font;
   foreach(var column in grid.Columns.Cast<DataGridViewColumn>().Where(c=>c.Visible).OrderBy(c=>c.DisplayIndex)){var rect=grid.GetColumnDisplayRectangle(column.Index,false);if(rect.Width<=0)continue;var cell=row.Cells[column.Index];if(cell is DataGridViewCheckBoxCell)continue;
    string text=Convert.ToString(cell.FormattedValue);var flags=TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix|(column.DefaultCellStyle.Alignment==DataGridViewContentAlignment.MiddleRight?TextFormatFlags.Right:TextFormatFlags.Left);
    TextRenderer.DrawText(g,text,font,new Rectangle(rect.Left+6,y,rect.Width-12,h),PowTheme.Ink,flags);}
   using(var line=new Pen(WorksheetColors.Focus,1.5f))g.DrawLine(line,0,y+h-1,band.Right,y+h-1);};
  ScrollEventHandler repaint=(s,e)=>grid.Invalidate(new Rectangle(0,top(),grid.Width,grid.RowTemplate.Height*3));grid.Scroll+=repaint;
  grid.MouseClick+=(s,e)=>{DataGridViewRow row;try{row=pinned();}catch(Exception){return;}if(row==null||e.Y<top()||e.Y>top()+Math.Max(row.Height,grid.RowTemplate.Height))return;try{grid.FirstDisplayedScrollingRowIndex=row.Index;grid.CurrentCell=row.Cells[1];}catch(InvalidOperationException){}};}
}
}
