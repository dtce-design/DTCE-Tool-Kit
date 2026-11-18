using System;using System.Linq;using System.Drawing;using System.Collections.Generic;using System.Windows.Forms;
namespace BeamScheduleFormatter {
sealed class BoqSourceNode {internal string Key;internal bool Detail;}
sealed class BoqDisclosure {
 readonly DataGridView grid;readonly HashSet<string> expanded=new HashSet<string>(StringComparer.Ordinal);
 internal BoqDisclosure(DataGridView grid){this.grid=grid;BoqStickyItem.Attach(grid);grid.RowHeadersVisible=true;grid.RowHeadersWidth=32;grid.CellPainting+=(s,e)=>{if(e.ColumnIndex!=-1||e.RowIndex<0)return;var node=grid.Rows[e.RowIndex].HeaderCell.Tag as BoqSourceNode;if(node==null||node.Detail)return;e.PaintBackground(e.ClipBounds,false);bool open=expanded.Contains(node.Key);if(!open&&e.RowIndex+1<grid.Rows.Count&&grid.Rows[e.RowIndex+1].Visible){var next=grid.Rows[e.RowIndex+1].HeaderCell.Tag as BoqSourceNode;open=next!=null&&next.Detail&&next.Key==node.Key;}TextRenderer.DrawText(e.Graphics,open?"▼":"▶",grid.Font,e.CellBounds,PowTheme.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);e.Handled=true;};grid.CellMouseClick+=(s,e)=>{if(e.ColumnIndex==-1&&e.RowIndex>=0&&e.Button==MouseButtons.Left)Toggle(grid.Rows[e.RowIndex]);};grid.CellToolTipTextNeeded+=(s,e)=>{if(e.ColumnIndex!=-1||e.RowIndex<0)return;var node=grid.Rows[e.RowIndex].HeaderCell.Tag as BoqSourceNode;if(node!=null&&!node.Detail)e.ToolTipText=expanded.Contains(node.Key)?"Collapse quantity sources":"Expand quantity sources";};}
 internal static void Parent(DataGridViewRow row,string key){row.HeaderCell.Tag=new BoqSourceNode{Key=key};row.HeaderCell.ToolTipText="Expand / collapse quantity sources";}
 internal static void Detail(DataGridViewRow row,string key){row.HeaderCell.Tag=new BoqSourceNode{Key=key,Detail=true};}
 internal void Toggle(DataGridViewRow row){var node=row.HeaderCell.Tag as BoqSourceNode;if(node==null||node.Detail||!grid.EndEdit())return;if(!expanded.Add(node.Key))expanded.Remove(node.Key);if(grid.CurrentRow!=null){var current=grid.CurrentRow.HeaderCell.Tag as BoqSourceNode;if(current!=null&&current.Detail&&current.Key==node.Key)grid.CurrentCell=row.Cells[1];}Apply();}
 internal string Filter="";
 // BOQ search (owner, 17 November 2026): every word must appear in the item no. or description. A pay item stays
 // visible when one of its source rows matches; matching source rows are shown even when collapsed.
 internal static bool Matches(DataGridViewRow row,string[] words){string text=(Convert.ToString(row.Cells[0].Value)+" "+Convert.ToString(row.Cells[1].Value)).ToUpperInvariant();return words.All(w=>text.Contains(w));}
 internal void Apply(){var words=(Filter??"").ToUpperInvariant().Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries);
  if(words.Length>0){grid.CurrentCell=null;var parentMatch=new HashSet<string>(StringComparer.Ordinal);var detailMatch=new HashSet<string>(StringComparer.Ordinal);
   foreach(DataGridViewRow row in grid.Rows){var node=row.HeaderCell.Tag as BoqSourceNode;if(node==null)continue;if(Matches(row,words))(node.Detail?detailMatch:parentMatch).Add(node.Key);}
   /* Owner (18 November 2026, "sa search bar dapat kasama lahat if kasama ang parent item"): when the pay item matches,
      all of its rows are shown; when a mapped quantity matches, its measurement sources are shown with it. */
   bool headMatch=false;string headKey=null;
   foreach(DataGridViewRow row in grid.Rows){var node=row.HeaderCell.Tag as BoqSourceNode;bool own=Matches(row,words);
    if(node==null)row.Visible=own;else if(!node.Detail){row.Visible=own||detailMatch.Contains(node.Key);headMatch=false;headKey=node.Key;}
    else{bool head=row.Tag is BackupDemand;if(head){headMatch=own;headKey=node.Key;}row.Visible=own||parentMatch.Contains(node.Key)||(!head&&headMatch&&headKey==node.Key);}}
   grid.Invalidate();return;}
  foreach(DataGridViewRow row in grid.Rows){var node=row.HeaderCell.Tag as BoqSourceNode;if(node==null||!node.Detail)row.Visible=true;}
  if(grid.CurrentRow!=null){var selected=grid.CurrentRow.HeaderCell.Tag as BoqSourceNode;if(selected!=null&&selected.Detail&&!expanded.Contains(selected.Key)){var parent=grid.Rows.Cast<DataGridViewRow>().FirstOrDefault(r=>{var node=r.HeaderCell.Tag as BoqSourceNode;return node!=null&&!node.Detail&&node.Key==selected.Key;});if(parent!=null)grid.CurrentCell=parent.Cells[1];}}foreach(DataGridViewRow row in grid.Rows){var node=row.HeaderCell.Tag as BoqSourceNode;if(node!=null&&node.Detail)row.Visible=expanded.Contains(node.Key);}grid.Invalidate();}
}
partial class ProgramOfWorksForm {BoqDisclosure boqDisclosure;}
}
