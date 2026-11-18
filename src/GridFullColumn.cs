using System;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Owner (18 November 2026): pressing Right arrow to the last column (or scrolling to the end) left the right-most column
// partly hidden in every table. Windows sizes the horizontal scroll range before the vertical scroll bar appears (rows
// grow when they wrap), so the last column ends under the vertical bar. After the current cell changes, or the horizontal
// bar reaches its end, the whole current cell / last column is brought into view, refreshing the scroll range if needed.
static class GridFullColumn {
 internal static void Attach(DataGridView g){
  bool busy=false;
  Action<int> reveal=column=>{if(busy||g.IsDisposed||!g.IsHandleCreated||column<0||column>=g.ColumnCount||g.Columns[column].Frozen)return;busy=true;try{
    Func<int> overflow=()=>{var rect=g.GetColumnDisplayRectangle(column,false);if(rect.Width==0)return 0;int visible=g.DisplayRectangle.Right;return Math.Max(0,rect.Right-visible);};
    int need=overflow();if(need<=0)return;int before=g.HorizontalScrollingOffset;
    try{g.HorizontalScrollingOffset=before+need;}catch(ArgumentException){}
    if(overflow()>0){var bars=g.ScrollBars;g.ScrollBars=ScrollBars.None;g.ScrollBars=bars;try{g.HorizontalScrollingOffset=g.HorizontalScrollingOffset+overflow();}catch(ArgumentException){}}
   }catch(InvalidOperationException){}finally{busy=false;}};
  Func<int> lastVisible=()=>{var last=g.Columns.GetLastColumn(DataGridViewElementStates.Visible,DataGridViewElementStates.None);return last==null?-1:last.Index;};
  g.CurrentCellChanged+=(s,e)=>{if(g.CurrentCell==null)return;int column=g.CurrentCell.ColumnIndex;try{g.BeginInvoke((Action)(()=>reveal(column)));}catch(InvalidOperationException){}};
  g.Scroll+=(s,e)=>{if(e.ScrollOrientation!=ScrollOrientation.HorizontalScroll||e.Type!=ScrollEventType.EndScroll&&e.Type!=ScrollEventType.Last&&e.Type!=ScrollEventType.SmallIncrement&&e.Type!=ScrollEventType.LargeIncrement)return;int last=lastVisible();if(last<0)return;var rect=g.GetColumnDisplayRectangle(last,false);if(rect.Width==0)return;try{g.BeginInvoke((Action)(()=>reveal(last)));}catch(InvalidOperationException){}};
 }
}
}
