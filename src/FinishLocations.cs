using System;using System.Collections.Generic;using System.Linq;using System.Windows.Forms;using System.Xml;
namespace BeamScheduleFormatter {
/* Finish Schedule location (owner, 20 November 2026: "sa finishing schedule ilagay if anong group iyong wall, floor, ceiling. para sa
   tab lang yun lang ang ipapakita sa schedule hindi lahat"). Each Finish ID can be marked Wall, Ceiling or Floor; the Finish ID lists
   of Wall / Ceiling / Floor Finishes and of the Finishing Start assignments then show only the IDs for that location (blank = all).
   The location is saved as attribute "loc" on the Finish Schedule row; files without it read as blank. */
partial class ProgramOfWorksForm {
 internal const string FinishLocationColumn="FinishLocation";
 readonly Dictionary<string,string> finishLocations=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
 void AddFinishLocationColumn(DataGridView g){if(g==null||g.Columns.Contains(FinishLocationColumn))return;
  var c=new DataGridViewComboBoxColumn{Name=FinishLocationColumn,HeaderText="Location",Width=110,FlatStyle=FlatStyle.Flat,DisplayStyle=DataGridViewComboBoxDisplayStyle.DropDownButton,ToolTipText="Wall, Ceiling or Floor: the Finish ID is offered only on that finish tab (blank = all tabs).",SortMode=DataGridViewColumnSortMode.NotSortable};
  c.Items.AddRange("","Wall","Ceiling","Floor");c.DefaultCellStyle.BackColor=ModernUi.Input;g.Columns.Add(c);try{c.DisplayIndex=1;}catch(InvalidOperationException){}
  g.CurrentCellDirtyStateChanged+=(s,e)=>{if(g.IsCurrentCellDirty&&g.CurrentCell!=null&&g.CurrentCell.OwningColumn==c)g.CommitEdit(DataGridViewDataErrorContexts.Commit);};
  g.CellValueChanged+=(s,e)=>{if(!filling&&e.RowIndex>=0&&e.ColumnIndex==c.Index)Recalculate();};}
 internal void ReadFinishLocations(){finishLocations.Clear();if(finishSchedule==null||!finishSchedule.Columns.Contains(FinishLocationColumn))return;int i=finishSchedule.Columns[FinishLocationColumn].Index;
  foreach(DataGridViewRow r in finishSchedule.Rows)if(!r.IsNewRow&&Cell(r,0)!="")finishLocations[Cell(r,0)]=Convert.ToString(r.Cells[i].Value).Trim();}
 internal bool FinishFor(string id,string category){string loc;return !finishLocations.TryGetValue(id??"",out loc)||loc==""||string.Equals(loc,category,StringComparison.OrdinalIgnoreCase);}
 string CategoryOf(DataGridView g){return g==wallFinishes?"Wall":g==ceilingFinishes?"Ceiling":g==floorFinishes?"Floor":"";}
 static void WriteRowLocation(DataGridViewRow row,XmlElement entry){if(row.DataGridView==null||!row.DataGridView.Columns.Contains(FinishLocationColumn))return;string v=Convert.ToString(row.Cells[FinishLocationColumn].Value).Trim();if(v!="")entry.SetAttribute("loc",v);}
 static void ReadRowLocation(DataGridViewRow row,XmlElement entry){if(row.DataGridView==null||!row.DataGridView.Columns.Contains(FinishLocationColumn))return;string v=entry.GetAttribute("loc").Trim();if(v!="Wall"&&v!="Ceiling"&&v!="Floor")v="";row.Cells[FinishLocationColumn].Value=v;}
 /* per-row Finish ID choices on the Finishing Start assignment table */
 void FilterAssignmentChoices(DataGridViewRow row,string category){var cell=row.Cells[3] as DataGridViewComboBoxCell;if(cell==null)return;string current=Convert.ToString(cell.Value);
  var ids=finishGroups.Keys.Where(k=>FinishFor(k,category)).OrderBy(k=>k).ToList();if(current!=""&&!ids.Contains(current,StringComparer.OrdinalIgnoreCase))ids.Add(current);
  cell.Items.Clear();cell.Items.Add("");foreach(var id in ids)cell.Items.Add(id);}
}
}
