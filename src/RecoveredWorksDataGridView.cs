using System;using System.Windows.Forms;
namespace BeamScheduleFormatter {
internal class GridInputInitialization:IDisposable {readonly bool previous;readonly Action<bool> set;internal GridInputInitialization(Func<bool> get,Action<bool> set){this.set=set;previous=get();set(true);}public void Dispose(){set(previous);}}
internal class WorksDataGridView:DataGridView {
 internal Func<IDisposable> DefaultValuesScope;internal bool BlankNumericInputs;
 public WorksDataGridView(){DoubleBuffered=true;}
 protected override bool ProcessCmdKey(ref Message message,Keys keys){if(DropdownCellClipboard.Process(this,keys))return true;return base.ProcessCmdKey(ref message,keys);}
 internal void ApplyProfilePaste(Action paste){var cell=CurrentCell;var beginning=new DataGridViewCellCancelEventArgs(cell.ColumnIndex,cell.RowIndex);OnCellBeginEdit(beginning);if(beginning.Cancel)return;if(DefaultValuesScope==null)paste();else using(DefaultValuesScope())paste();var completed=CurrentCell;OnCellEndEdit(new DataGridViewCellEventArgs(completed.ColumnIndex,completed.RowIndex));}

 // Rows created by paste get the same defaults (auto ID, sides, materials) as a row typed into the new row.
 // Pasting into the new row moves its defaults to the committed row and gives the new row fresh defaults.
 internal DataGridViewRow AddRowWithDefaults(){if(AllowUserToAddRows&&NewRowIndex>=0){var template=Rows[NewRowIndex];var values=new object[template.Cells.Count];bool any=false;for(int c=0;c<values.Length;c++){values[c]=template.Cells[c].Value;if(values[c]!=null)any=true;}var row=Rows[Rows.Add()];if(any){for(int c=0;c<values.Length;c++)if(values[c]!=null&&!row.Cells[c].ReadOnly||c==0&&values[c]!=null)row.Cells[c].Value=values[c];var fresh=Rows[NewRowIndex];foreach(DataGridViewCell cell in fresh.Cells)cell.Value=null;OnDefaultValuesNeeded(new DataGridViewRowEventArgs(fresh));}else OnDefaultValuesNeeded(new DataGridViewRowEventArgs(row));return row;}
  var added=Rows[Rows.Add()];OnDefaultValuesNeeded(new DataGridViewRowEventArgs(added));return added;}
 protected override void OnDefaultValuesNeeded(DataGridViewRowEventArgs e){if(DefaultValuesScope==null){base.OnDefaultValuesNeeded(e);return;}using(DefaultValuesScope()){base.OnDefaultValuesNeeded(e);/* Hidden columns hold settings-driven materials (concrete MPa, rebar grades); only visible measurement inputs start blank. */if(BlankNumericInputs)foreach(DataGridViewCell cell in e.Row.Cells)if(!cell.ReadOnly&&cell.OwningColumn!=null&&cell.OwningColumn.Visible&&QuantityInputDefaults.IsNumber(cell.Value))cell.Value=null;}}
}
}
