using System;using System.Linq;using System.Drawing;using System.Windows.Forms;
namespace BeamScheduleFormatter {
sealed class BoqScrollState {
 readonly string top;readonly int offset;readonly bool bottom;
 BoqScrollState(string top,int offset,bool bottom){this.top=top;this.offset=offset;this.bottom=bottom;}
 static string Key(DataGridViewRow row){var item=row.Tag as BackupBillItem;if(item!=null)return "item:"+item.Slot;var node=row.HeaderCell.Tag as BoqSourceNode;return (node==null?"row":node.Key+":"+node.Detail)+":"+Convert.ToString(row.Cells[1].Value);}
 internal static BoqScrollState Capture(DataGridView grid){if(!grid.IsHandleCreated||grid.Rows.Count==0)return new BoqScrollState(null,0,false);int first=grid.FirstDisplayedScrollingRowIndex;var bar=grid.Controls.OfType<VScrollBar>().FirstOrDefault();bool end=bar!=null&&bar.Visible&&bar.Value>=bar.Maximum-bar.LargeChange+1;return new BoqScrollState(first>=0?Key(grid.Rows[first]):null,grid.HorizontalScrollingOffset,end);}
 internal void Restore(DataGridView grid){if(!grid.IsHandleCreated||grid.IsDisposed)return;grid.PerformLayout();if(bottom){EnsureEnd(grid);return;}if(top!=null){var row=grid.Rows.Cast<DataGridViewRow>().FirstOrDefault(r=>r.Visible&&Key(r)==top);if(row!=null)grid.FirstDisplayedScrollingRowIndex=row.Index;}if(offset>=0)grid.HorizontalScrollingOffset=offset;}
 internal static void EnsureEnd(DataGridView grid){if(grid.IsDisposed||!grid.IsHandleCreated)return;var last=grid.Rows.Cast<DataGridViewRow>().LastOrDefault(r=>r.Visible&&!r.IsNewRow);if(last==null)return;var h=grid.Controls.OfType<HScrollBar>().FirstOrDefault();int bottom=grid.ClientSize.Height-(h!=null&&h.Visible?h.Height:0)-2;var bounds=grid.GetRowDisplayRectangle(last.Index,false);if(bounds.Height>0&&bounds.Bottom<=bottom&&bounds.Top>=grid.ColumnHeadersHeight)return;grid.FirstDisplayedScrollingRowIndex=last.Index;}
 internal static void Attach(DataGridView grid){bool pending=false;grid.Scroll+=(s,e)=>{if(pending||e.ScrollOrientation!=ScrollOrientation.VerticalScroll||!grid.IsHandleCreated)return;var bar=grid.Controls.OfType<VScrollBar>().FirstOrDefault();if(e.Type!=ScrollEventType.Last&&(bar==null||bar.Value<bar.Maximum-bar.LargeChange+1))return;pending=true;try{grid.BeginInvoke((Action)(()=>{try{EnsureEnd(grid);}finally{pending=false;}}));}catch(InvalidOperationException){pending=false;}};}
}
}
