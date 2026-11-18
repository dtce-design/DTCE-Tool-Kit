using System;using System.Collections.Generic;using System.Drawing;using System.Linq;using System.Runtime.CompilerServices;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Status bar under every calculation sheet (owner, 18 November 2026: "sa mga calculation sheets maglagay na lang siguro
// ng status bar yung errors at anong id"). The Status column is often too narrow to read; the bar lists each row whose
// status is not a normal result, with its ID (first column) and the full message. Click a line to go to that row.
static class GridStatusBar {
 static readonly ConditionalWeakTable<DataGridView,object> attached=new ConditionalWeakTable<DataGridView,object>();
 static readonly string[] Normal={"Estimated","Ready","Saved in project","Linked","OK","Computed","Paint only"};
 internal static bool IsIssue(string status){status=(status??"").Trim();return status!=""&&!Normal.Any(n=>status.StartsWith(n,StringComparison.OrdinalIgnoreCase));}
 static int StatusColumn(DataGridView g){for(int i=g.ColumnCount-1;i>=0;i--)if(string.Equals((g.Columns[i].HeaderText??"").Trim(),"Status",StringComparison.OrdinalIgnoreCase))return i;return -1;}
 internal static void Attach(DataGridView g){object seen;if(g==null||attached.TryGetValue(g,out seen))return;attached.Add(g,new object());
  Panel bar=null;ListBox list=null;Label title=null;Timer timer=null;
  Action place=()=>{var parent=g.Parent;if(bar!=null||parent==null||g.Dock!=DockStyle.Fill||StatusColumn(g)<0||g.ReadOnly&&!g.Columns.Cast<DataGridViewColumn>().Any(c=>!c.ReadOnly))return;
   bar=new Panel{Dock=DockStyle.Bottom,Height=30,Visible=false,Padding=new Padding(8,4,8,4),BackColor=Color.FromArgb(253,243,232),Tag="grid-status-bar"};
   title=new Label{Dock=DockStyle.Top,Height=22,ForeColor=Color.FromArgb(150,62,0),Font=new Font("Segoe UI",9f,FontStyle.Bold),TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true,BackColor=Color.Transparent};
   list=new ListBox{Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,IntegralHeight=false,Font=new Font("Segoe UI",9f),BackColor=Color.FromArgb(253,243,232),ForeColor=Color.FromArgb(70,40,20),Cursor=Cursors.Hand};
   list.Click+=(s,e)=>{var row=list.SelectedItem as RowIssue;if(row==null||row.Row.DataGridView!=g||row.Row.Index<0)return;try{int col=StatusColumn(g);var firstVisible=g.Columns.Cast<DataGridViewColumn>().Where(c=>c.Visible).OrderBy(c=>c.DisplayIndex).FirstOrDefault();g.CurrentCell=row.Row.Cells[firstVisible!=null?firstVisible.Index:col];g.Focus();}catch(InvalidOperationException){}};
   bar.Controls.Add(list);bar.Controls.Add(title);parent.Controls.Add(bar);};
  Action refresh=()=>{if(g.IsDisposed)return;place();if(bar==null||bar.IsDisposed)return;int col=StatusColumn(g);if(col<0)return;var issues=new List<RowIssue>();
   foreach(DataGridViewRow row in g.Rows){if(row.IsNewRow)continue;string status=Convert.ToString(row.Cells[col].Value);if(!IsIssue(status))continue;string id=Convert.ToString(row.Cells[0].Value).Trim();if(id=="")id="Row "+(row.Index+1);issues.Add(new RowIssue{Row=row,Text=id+": "+status.Trim()});}
   if(issues.Count==0){bar.Visible=false;return;}
   list.BeginUpdate();list.Items.Clear();foreach(var issue in issues)list.Items.Add(issue);list.EndUpdate();
   title.Text=issues.Count+" row"+(issues.Count==1?"":"s")+" need attention (not counted until corrected) - click a line to go to the row";
   bar.Height=Math.Min(30+22+issues.Count*18,30+22+5*18);bar.Visible=true;};
  Action schedule=()=>{if(g.IsDisposed)return;if(timer==null){timer=new Timer{Interval=250};timer.Tick+=(s,e)=>{timer.Stop();refresh();};g.Disposed+=(s,e)=>timer.Dispose();}timer.Stop();timer.Start();};
  g.CellValueChanged+=(s,e)=>{if(e.ColumnIndex<0||e.ColumnIndex==StatusColumn(g))schedule();};
  g.RowsRemoved+=(s,e)=>schedule();g.RowsAdded+=(s,e)=>schedule();g.ParentChanged+=(s,e)=>schedule();g.VisibleChanged+=(s,e)=>{if(g.Visible)schedule();};
  schedule();}
 sealed class RowIssue{internal DataGridViewRow Row;internal string Text;public override string ToString(){return Text;}}
}
}
