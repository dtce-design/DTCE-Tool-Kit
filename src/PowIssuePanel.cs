using System;using System.Collections.Generic;using System.Drawing;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Input issues are listed inside the app under the Bill of Quantities (owner decision 17 November 2026),
// not written to the linked Excel workbook.
partial class ProgramOfWorksForm {
 Panel issuePanel;Label issueTitle;DataGridView issueGrid;
 void AddIssuePanel(TabPage page){
  issuePanel=new Panel{Dock=DockStyle.Bottom,Height=170,Visible=false,BackColor=Color.White,Padding=new Padding(16,4,16,0)};
  issueTitle=new Label{Dock=DockStyle.Top,Height=26,ForeColor=Color.Firebrick,Font=new Font("Segoe UI",9.5f,FontStyle.Bold),TextAlign=ContentAlignment.MiddleLeft};
  issueGrid=new DataGridView{Name="InputIssues",Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,AllowUserToResizeRows=false,RowHeadersVisible=false,BackgroundColor=Color.White,BorderStyle=BorderStyle.FixedSingle,SelectionMode=DataGridViewSelectionMode.FullRowSelect,AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.AllCells};
  issueGrid.Columns.Add("No","NO.");issueGrid.Columns.Add("Issue","INPUT ISSUE (entry excluded from quantities until corrected; your input is kept)");
  issueGrid.Columns[0].Width=60;issueGrid.Columns[1].AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill;issueGrid.Columns[1].DefaultCellStyle.WrapMode=DataGridViewTriState.True;
  issuePanel.Controls.Add(issueGrid);issuePanel.Controls.Add(issueTitle);page.Controls.Add(issuePanel);
 }
 internal List<string> DisplayedInputIssues(){var list=new List<string>();if(issueGrid==null)return list;foreach(DataGridViewRow row in issueGrid.Rows)list.Add(Convert.ToString(row.Cells[1].Value));return list;}
 void UpdateIssuePanel(IEnumerable<string> input){
  if(issuePanel==null)return;var issues=input.Distinct().ToList();
  if(DisplayedInputIssues().SequenceEqual(issues)&&issuePanel.Visible==(issues.Count>0))return;
  issueGrid.Rows.Clear();for(int i=0;i<issues.Count;i++)issueGrid.Rows.Add(i+1,issues[i]);
  issueTitle.Text="Input issues ("+issues.Count+") - correct these entries to include them in the quantities";
  issuePanel.Visible=issues.Count>0;
 }
}
}
