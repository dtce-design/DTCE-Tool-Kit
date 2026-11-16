using System;using System.Windows.Forms;
namespace BeamScheduleFormatter {
internal class GridInputInitialization:IDisposable {readonly bool previous;readonly Action<bool> set;internal GridInputInitialization(Func<bool> get,Action<bool> set){this.set=set;previous=get();set(true);}public void Dispose(){set(previous);}}
internal class WorksDataGridView:DataGridView {
 internal Func<IDisposable> DefaultValuesScope;internal bool BlankNumericInputs;
 public WorksDataGridView(){DoubleBuffered=true;}
 protected override bool ProcessCmdKey(ref Message message,Keys keys){if(DropdownCellClipboard.Process(this,keys))return true;return base.ProcessCmdKey(ref message,keys);}
 internal void ApplyProfilePaste(Action paste){var cell=CurrentCell;var beginning=new DataGridViewCellCancelEventArgs(cell.ColumnIndex,cell.RowIndex);OnCellBeginEdit(beginning);if(beginning.Cancel)return;if(DefaultValuesScope==null)paste();else using(DefaultValuesScope())paste();var completed=CurrentCell;OnCellEndEdit(new DataGridViewCellEventArgs(completed.ColumnIndex,completed.RowIndex));}

 protected override void OnDefaultValuesNeeded(DataGridViewRowEventArgs e){if(DefaultValuesScope==null){base.OnDefaultValuesNeeded(e);return;}using(DefaultValuesScope()){base.OnDefaultValuesNeeded(e);if(BlankNumericInputs)foreach(DataGridViewCell cell in e.Row.Cells)if(!cell.ReadOnly&&QuantityInputDefaults.IsNumber(cell.Value))cell.Value=null;}}
}
}
