using System;using System.Collections.Generic;using System.Drawing;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Status panel under the Bill of Quantities, separate from the table (owner, 18 November 2026): input errors,
// quantities that are not mapped, mapped quantities that are excluded, automatic LS values. Double-click a
// "Not mapped" line to choose its BACKUP item, an "Excluded" line to include it again. Input issues stay in the app
// (owner, 17 November 2026), not in the linked Excel workbook.
partial class ProgramOfWorksForm {
 Panel issuePanel;Label issueTitle;DataGridView issueGrid;
 void AddIssuePanel(TabPage page){
  issuePanel=new Panel{Dock=DockStyle.Bottom,Height=180,Visible=false,BackColor=Color.White,Padding=new Padding(16,4,16,0)};
  issueTitle=new Label{Dock=DockStyle.Top,Height=26,ForeColor=Color.Firebrick,Font=new Font("Segoe UI",9.5f,FontStyle.Bold),TextAlign=ContentAlignment.MiddleLeft};
  issueGrid=new DataGridView{Name="InputIssues",Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,AllowUserToResizeRows=false,RowHeadersVisible=false,BackgroundColor=Color.White,BorderStyle=BorderStyle.FixedSingle,SelectionMode=DataGridViewSelectionMode.FullRowSelect,AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.AllCells};
  issueGrid.Columns.Add("No","NO.");issueGrid.Columns.Add("Issue","STATUS / ISSUE");issueGrid.Columns.Add("Kind","TYPE");
  issueGrid.Columns[0].Width=50;issueGrid.Columns[1].AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill;issueGrid.Columns[1].DefaultCellStyle.WrapMode=DataGridViewTriState.True;issueGrid.Columns[2].Width=120;issueGrid.Columns[2].DisplayIndex=1;
  issueGrid.CellDoubleClick+=(s,e)=>{if(e.RowIndex<0)return;var note=issueGrid.Rows[e.RowIndex].Tag as BoqStatusNote;if(note==null||note.Demand==null)return;
   if(note.Kind=="Not mapped")ChangeBoqMapping(note.Demand);else if(note.Kind=="Excluded"){boqIncludes[note.Demand.Key]=true;dashboardDirty=reportPreviewDirty=true;RefreshVisibleWorksViews();ScheduleWorksExcel();}};
  issuePanel.Controls.Add(issueGrid);issuePanel.Controls.Add(issueTitle);page.Controls.Add(issuePanel);
 }
 // Only the input issues (entries excluded from quantities), as before.
 internal List<string> DisplayedInputIssues(){var list=new List<string>();if(issueGrid==null)return list;foreach(DataGridViewRow row in issueGrid.Rows)if(row.Tag==null)list.Add(Convert.ToString(row.Cells[1].Value));return list;}
 void UpdateIssuePanel(IEnumerable<string> input){
  if(issuePanel==null)return;var issues=input.Distinct().ToList();var notes=boqStatusNotes.ToList();
  issueGrid.Rows.Clear();int n=0;
  foreach(string issue in issues){int r=issueGrid.Rows.Add(++n,issue,"Input error");issueGrid.Rows[r].Cells[2].Style.ForeColor=Color.Firebrick;}
  foreach(var note in notes.OrderBy(x=>x.Kind=="Error"?0:x.Kind=="Not mapped"?1:x.Kind=="Excluded"?2:3)){int r=issueGrid.Rows.Add(++n,note.Text,note.Kind);issueGrid.Rows[r].Tag=note;issueGrid.Rows[r].Cells[2].Style.ForeColor=note.Kind=="Error"?Color.Firebrick:note.Kind=="Not mapped"?Color.FromArgb(176,96,0):PowTheme.Muted;}
  int unmapped=notes.Count(x=>x.Kind=="Not mapped"),excluded=notes.Count(x=>x.Kind=="Excluded"),errors=issues.Count+notes.Count(x=>x.Kind=="Error");
  var parts=new List<string>();if(errors>0)parts.Add(errors+" error(s) - entries excluded until corrected");if(unmapped>0)parts.Add(unmapped+" not mapped");if(excluded>0)parts.Add(excluded+" excluded");if(notes.Any(x=>x.Kind=="Automatic LS"))parts.Add(notes.Count(x=>x.Kind=="Automatic LS")+" automatic LS");
  issueTitle.Text="Status: "+(parts.Count==0?"OK":string.Join(" · ",parts))+(unmapped+excluded>0?"   (double-click a line to map / include)":"");issueTitle.ForeColor=errors>0?Color.Firebrick:unmapped>0?Color.FromArgb(176,96,0):PowTheme.Teal;
  issuePanel.Visible=n>0;
 }
}
}
