using System;using System.Collections.Generic;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* Double-click an input error in the Bill of Quantities status to go to it (owner, 23 November 2026: "double click? pupunta sa tab?"):
   the sheet named at the start of the message is opened (its main tab and sub tabs) and the row with the entry ID is selected, at the
   cell the message asks for when it names one (description, deduction). An ID found on one sheet only is used when the message does
   not name the sheet. */
partial class ProgramOfWorksForm {
 internal bool GoToIssue(string issue){if(string.IsNullOrEmpty(issue))return false;int colon=issue.IndexOf(':');string head=colon>0?issue.Substring(0,colon):issue;
  var pages=AllControls(tabs).OfType<TabPage>().Concat(tabs.TabPages.Cast<TabPage>()).Distinct().ToList();
  /* the sheet: the longest tab caption the message starts with */
  var page=pages.Where(p=>p.Text.Length>0&&head.StartsWith(p.Text+" ",StringComparison.OrdinalIgnoreCase)).OrderByDescending(p=>p.Text.Length).FirstOrDefault();
  string id=page!=null?head.Substring(page.Text.Length).Trim():head.Split(' ').Last().Trim();
  Func<Control,IEnumerable<DataGridView>> grids=root=>AllControls(root).OfType<DataGridView>().Where(g=>g.Columns.Count>0);
  DataGridViewRow found=null;
  if(id!=""){var search=page!=null?grids(page):pages.SelectMany(p=>grids(p)).Distinct();var hits=search.SelectMany(g=>g.Rows.Cast<DataGridViewRow>().Where(r=>!r.IsNewRow&&string.Equals(Convert.ToString(r.Cells[0].Value).Trim(),id,StringComparison.OrdinalIgnoreCase))).Distinct().ToList();
   if(hits.Count==1||page!=null&&hits.Count>0)found=hits[0];}
  if(found==null&&page==null)return false;
  Control target=found!=null?(Control)found.DataGridView:page;
  /* open every tab on the way, from the main tab down */
  var chain=new List<TabPage>();for(Control c=target;c!=null;c=c.Parent){var tp=c as TabPage;if(tp!=null)chain.Insert(0,tp);}
  foreach(var tp in chain){var owner=tp.Parent as TabControl;if(owner!=null&&owner.SelectedTab!=tp)owner.SelectedTab=tp;}
  if(found!=null){var g=found.DataGridView;int col=IssueColumn(g,issue);if(col<0||!g.Columns[col].Visible)col=g.Columns.Cast<DataGridViewColumn>().Where(c=>c.Visible).Select(c=>c.Index).DefaultIfEmpty(0).First();
   try{g.ClearSelection();g.CurrentCell=found.Cells[col];found.Selected=true;g.FirstDisplayedScrollingRowIndex=Math.Max(0,found.Index-2);g.Focus();}catch(InvalidOperationException){}}
  return true;}
 /* the cell the message asks for */
 static int IssueColumn(DataGridView g,string issue){Func<Func<string,bool>,int> find=match=>{foreach(DataGridViewColumn c in g.Columns)if(match(c.HeaderText))return c.Index;return -1;};
  if(issue.Contains(DescriptionMissing))return find(h=>h.StartsWith("Description",StringComparison.OrdinalIgnoreCase));
  if(issue.Contains(DeductionMissing))return find(h=>h.IndexOf("deduction",StringComparison.OrdinalIgnoreCase)>=0||h.IndexOf("overlap",StringComparison.OrdinalIgnoreCase)>=0);
  return -1;}
}
}
