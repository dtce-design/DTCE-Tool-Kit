using System;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Pipe takeoff: each material tab only asks for the size from a dropdown (owner, 17 November 2026). The full
// specification (diameter, material, series/PN) is still stored in the saved specification column, so BOQ grouping,
// reports and saved files are unchanged. "Other pipes" keeps the free-text specification.
partial class ProgramOfWorksForm {
 const string OtherPipes="Other pipes";
 DataGridViewComboBoxColumn pipeSize;
 internal static string PipeSizeOf(string spec){spec=(spec??"").Trim();int space=spec.IndexOf(' ');return space<0?spec:spec.Substring(0,space);}
 internal static string PipeSpecFor(string group,string size){return PipeGroups.Choices(group).FirstOrDefault(s=>string.Equals(PipeSizeOf(s),size,StringComparison.OrdinalIgnoreCase));}
 void InstallPipeSizeColumn(){
  pipeSize=new DataGridViewComboBoxColumn{Name="PipeSize",HeaderText="Size",Width=150,DisplayStyle=DataGridViewComboBoxDisplayStyle.DropDownButton,FlatStyle=FlatStyle.Flat};
  plumbingPipes.Columns.Add(pipeSize);pipeSize.DisplayIndex=2;
  plumbingPipes.DataError+=(s,e)=>{if(e.ColumnIndex==pipeSize.Index)e.ThrowException=false;};
  plumbingPipes.CurrentCellDirtyStateChanged+=(s,e)=>{if(plumbingPipes.IsCurrentCellDirty&&plumbingPipes.CurrentCell!=null&&plumbingPipes.CurrentCell.ColumnIndex==pipeSize.Index)plumbingPipes.CommitEdit(DataGridViewDataErrorContexts.Commit);};
  plumbingPipes.CellValueChanged+=(s,e)=>{if(filling||changingPipeGroup||e.RowIndex<0||e.ColumnIndex!=pipeSize.Index||pipeGroupTabs==null||pipeGroupTabs.SelectedTab==null)return;
   var row=plumbingPipes.Rows[e.RowIndex];string spec=PipeSpecFor(pipeGroupTabs.SelectedTab.Text,Convert.ToString(row.Cells[pipeSize.Index].Value));if(spec!=null&&Cell(row,2)!=spec){row.Cells[2].Value=spec;Recalculate();}};
  plumbingPipes.DefaultValuesNeeded+=(s,e)=>PreparePipeSizeCell(e.Row);}
 void PreparePipeSizeCell(DataGridViewRow row){if(pipeSize==null||pipeGroupTabs==null||pipeGroupTabs.SelectedTab==null)return;string group=pipeGroupTabs.SelectedTab.Text;if(group==OtherPipes)return;
  var cell=(DataGridViewComboBoxCell)row.Cells[pipeSize.Index];var sizes=PipeGroups.Choices(group).Select(PipeSizeOf).ToArray();cell.Items.Clear();cell.Items.AddRange(sizes);
  string size=PipeSizeOf(Cell(row,2));cell.Value=sizes.FirstOrDefault(x=>string.Equals(x,size,StringComparison.OrdinalIgnoreCase));}
 void SyncPipeSizeColumn(){if(pipeSize==null||pipeGroupTabs==null||pipeGroupTabs.SelectedTab==null)return;bool other=pipeGroupTabs.SelectedTab.Text==OtherPipes;
  plumbingPipes.Columns[2].Visible=other;pipeSize.Visible=!other;if(other)return;foreach(DataGridViewRow row in plumbingPipes.Rows)if(row.Visible)PreparePipeSizeCell(row);}
}
}
