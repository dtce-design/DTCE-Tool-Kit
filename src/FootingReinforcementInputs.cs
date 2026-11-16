using System;using System.Drawing;using System.Windows.Forms;
namespace BeamScheduleFormatter {
partial class ProgramOfWorksForm {
 bool syncingFootingInputs;
 void ConfigureFootingReinforcement(){
  for(int n=0;n<4;n++){int b=8+3*n;string group=new[]{"Bottom X","Bottom Y","Top X","Top Y"}[n];
   for(int c=b;c<b+3;c++)footings.Columns[c].Visible=true;footings.Columns[b].HeaderText=group+" Ø mm";
   var mode=new DataGridViewComboBoxColumn{Name="FootingBarMethod"+n,HeaderText=group+" input method",Width=155,SortMode=DataGridViewColumnSortMode.NotSortable,FlatStyle=FlatStyle.Standard};mode.Items.AddRange("Spacing (mm)","Number of bars");footings.Columns.Add(mode);mode.DisplayIndex=footings.Columns[b+1].DisplayIndex;
  }
  footings.CurrentCellDirtyStateChanged+=(s,e)=>{if(footings.IsCurrentCellDirty&&footings.CurrentCell is DataGridViewComboBoxCell)footings.CommitEdit(DataGridViewDataErrorContexts.Commit);};
  footings.CellValueChanged+=(s,e)=>{if(syncingFootingInputs||filling||e.RowIndex<0||e.ColumnIndex<0)return;string name=footings.Columns[e.ColumnIndex].Name;if(!name.StartsWith("FootingBarMethod"))return;int n=int.Parse(name.Substring("FootingBarMethod".Length)),b=8+3*n;var row=footings.Rows[e.RowIndex];syncingFootingInputs=true;try{bool count=Convert.ToString(row.Cells[e.ColumnIndex].Value)=="Number of bars";row.Cells[b+(count?1:2)].Value="";ApplyFootingInputMode(row,n,count);}finally{syncingFootingInputs=false;}if(!initializingWorks)Recalculate();};
  SyncFootingReinforcement();
 }
 void ApplyFootingInputMode(DataGridViewRow row,int n,bool count){int b=8+3*n;row.Cells[b+1].ReadOnly=count;row.Cells[b+2].ReadOnly=!count;row.Cells[b+1].ToolTipText=count?"Inactive: number of bars is selected.":"Spacing in mm across this direction.";row.Cells[b+2].ToolTipText=count?"Number of bars per footing in this direction.":"Inactive: spacing is selected.";}
 void SyncFootingReinforcement(){if(syncingFootingInputs||!footings.Columns.Contains("FootingBarMethod0"))return;syncingFootingInputs=true;try{foreach(DataGridViewRow row in footings.Rows){if(row.IsNewRow)continue;for(int n=0;n<4;n++){int b=8+3*n;bool count=!string.IsNullOrWhiteSpace(Cell(row,b+2))?true:!string.IsNullOrWhiteSpace(Cell(row,b+1))?false:Cell(row,footings.Columns["FootingBarMethod"+n].Index)=="Number of bars";row.Cells["FootingBarMethod"+n].Value=count?"Number of bars":"Spacing (mm)";ApplyFootingInputMode(row,n,count);}}}finally{syncingFootingInputs=false;}}
}
}
