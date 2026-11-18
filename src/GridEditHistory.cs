using System;using System.Collections.Generic;using System.Drawing;using System.Linq;using System.Runtime.CompilerServices;using System.Windows.Forms;
namespace BeamScheduleFormatter {
sealed class GridRowSnapshot {
 internal object Extra;
 internal readonly List<DataGridViewRow> Rows=new List<DataGridViewRow>();
 static DataGridViewRow Copy(DataGridViewRow source){var row=(DataGridViewRow)source.Clone();row.HeaderCell.Tag=source.HeaderCell.Tag;for(int i=0;i<source.Cells.Count;i++){row.Cells[i].Value=source.Cells[i].Value;row.Cells[i].ReadOnly=source.Cells[i].ReadOnly;}return row;}
 internal static GridRowSnapshot Capture(DataGridView grid){var state=new GridRowSnapshot();foreach(DataGridViewRow row in grid.Rows)if(!row.IsNewRow)state.Rows.Add(Copy(row));return state;}
 internal bool SameInputs(GridRowSnapshot other){if(Rows.Count!=other.Rows.Count)return false;for(int r=0;r<Rows.Count;r++){if(Rows[r].Cells.Count!=other.Rows[r].Cells.Count)return false;for(int c=0;c<Rows[r].Cells.Count;c++)if(!Rows[r].Cells[c].ReadOnly&&!object.Equals(Rows[r].Cells[c].Value,other.Rows[r].Cells[c].Value))return false;}return true;}
 internal void Restore(DataGridView grid){grid.Rows.Clear();grid.Rows.AddRange(Rows.Select(Copy).ToArray());}
}
sealed class GridEditAction {internal GridRowSnapshot Before,After;internal string Description;internal int Deleted;}
sealed class GridEditHistory {
 static readonly ConditionalWeakTable<DataGridView,GridEditHistory> histories=new ConditionalWeakTable<DataGridView,GridEditHistory>();
 static readonly ConditionalWeakTable<Form,ActiveGrid> active=new ConditionalWeakTable<Form,ActiveGrid>();sealed class ActiveGrid{internal GridEditHistory History;}
 internal static event Action StateChanged;
 /* > 0 while the app itself changes cells (e.g. excluding a row from totals); such changes do not clear Undo. */internal static int Quiet;
 internal static Func<IWin32Window,int,bool> ConfirmDelete=(owner,count)=>MessageBox.Show(owner,"Delete "+count+" selected row"+(count==1?"":"s")+"?\nYou can restore them with Undo.","Confirm row deletion",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)==DialogResult.Yes;
 GridEditHistory commandTarget;
 readonly DataGridView grid;readonly UndoHistory<GridEditAction> history=new UndoHistory<GridEditAction>();GridRowSnapshot before;bool restoring,completing;Func<bool> busy=()=>false;Action<bool> setBusy=value=>{};Action changed=()=>{};Func<object,object,bool> sameExtra=(a,b)=>true;Func<object> captureExtra=()=>null;Action<object> restoreExtra=value=>{};
 internal static GridEditHistory Attach(DataGridView grid){GridEditHistory result;if(histories.TryGetValue(grid,out result))return result;result=new GridEditHistory(grid);histories.Add(grid,result);return result;}
 internal static void Configure(DataGridView grid,Func<bool> busy,Action<bool> setBusy,Action changed){var editor=Attach(grid);editor.busy=busy;editor.setBusy=setBusy;editor.changed=changed;}
 internal static void LinkProxy(DataGridView proxy,DataGridView source){Attach(proxy).commandTarget=Attach(source);}
 internal static void ConfigureExtra(DataGridView grid,Func<object> capture,Action<object> restore,Func<object,object,bool> same=null){var h=Attach(grid);h.captureExtra=capture;h.restoreExtra=restore;if(same!=null)h.sameExtra=same;}
 GridRowSnapshot Snapshot(){var state=GridRowSnapshot.Capture(grid);state.Extra=captureExtra();return state;}
 void Restore(GridRowSnapshot state){state.Restore(grid);restoreExtra(state.Extra);}
 GridEditHistory(DataGridView grid){this.grid=grid;
  grid.Enter+=(s,e)=>Remember();grid.CellEnter+=(s,e)=>Remember();
  grid.CellBeginEdit+=(s,e)=>{if(restoring||busy())return;Complete();before=Snapshot();};
  grid.CellEndEdit+=(s,e)=>{if(restoring||busy()||before==null||!grid.IsHandleCreated)return;grid.BeginInvoke((Action)(()=>{if(!grid.IsDisposed)Complete();}));};
  grid.RowsRemoved+=(s,e)=>{if(!restoring&&before==null){history.Clear();Notify();}};
  grid.RowsAdded+=(s,e)=>{if(Quiet>0)return;if(!restoring&&before==null&&(busy()||!grid.ContainsFocus)){history.Clear();Notify();}};grid.SelectionChanged+=(s,e)=>{if(!restoring)Notify();};
  grid.CellValueChanged+=(s,e)=>{if(Quiet>0)return;if(!restoring&&before==null&&e.RowIndex>=0&&e.ColumnIndex>=0&&!grid.Rows[e.RowIndex].Cells[e.ColumnIndex].ReadOnly&&(busy()||!grid.ContainsFocus))Clear();};
  grid.ColumnAdded+=(s,e)=>Clear();grid.ColumnRemoved+=(s,e)=>Clear();
  grid.UserDeletingRow+=(s,e)=>{e.Cancel=true;if(grid.IsHandleCreated)grid.BeginInvoke((Action)DeleteSelected);};
  grid.Enter+=(s,e)=>EnsureMenu();grid.MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Right)EnsureMenu();};
  /* Owner (18 November 2026): Delete clears only the selected cell values, like Excel; whole rows are deleted with
     Ctrl+Delete, the right-click menu or the row delete button. */
  grid.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Delete&&!e.Control&&!grid.IsCurrentCellInEditMode){e.Handled=e.SuppressKeyPress=true;ClearSelectedCells();}};
 }
 bool menuPrepared;
 void EnsureMenu(){if(menuPrepared||grid.ContextMenuStrip!=null)return;menuPrepared=true;
  var menu=new ContextMenuStrip();var undo=menu.Items.Add("Undo (Ctrl+Z)",null,(s,e)=>Undo());var redo=menu.Items.Add("Redo (Ctrl+Y)",null,(s,e)=>Redo());menu.Items.Add(new ToolStripSeparator());var delete=menu.Items.Add("Delete selected rows (Ctrl+Delete)",null,(s,e)=>DeleteSelected());
  menu.Opening+=(s,e)=>{if(commandTarget==null&&!Commit()){e.Cancel=true;return;}undo.Enabled=CanUndo;redo.Enabled=CanRedo;int count=SelectedRows().Count;delete.Text="Delete "+count+" selected row"+(count==1?"":"s")+" (Ctrl+Delete)";delete.Enabled=count>0;};
  if(grid.ContextMenuStrip==null){grid.ContextMenuStrip=menu;grid.Disposed+=(s,e)=>menu.Dispose();}else menu.Dispose();
 }
 void Remember(){if(restoring||!grid.ContainsFocus)return;for(Control c=grid;c!=null;c=c.Parent){var form=c as Form;if(form==null)continue;ActiveGrid slot;if(!active.TryGetValue(form,out slot)){slot=new ActiveGrid();active.Add(form,slot);}slot.History=this;HookKeys(form);}Notify();}
 static readonly ConditionalWeakTable<Form,object> hooked=new ConditionalWeakTable<Form,object>();
 static void HookKeys(Form form){object seen;if(hooked.TryGetValue(form,out seen))return;hooked.Add(form,new object());form.KeyPreview=true;form.KeyDown+=(s,e)=>{if(!e.Control||e.Alt)return;var editor=For(form);if(editor==null)return;if(e.KeyCode==Keys.Z){e.Handled=e.SuppressKeyPress=true;if(e.Shift)editor.Redo();else editor.Undo();}else if(e.KeyCode==Keys.Y){e.Handled=e.SuppressKeyPress=true;editor.Redo();}else if(e.KeyCode==Keys.Delete){e.Handled=e.SuppressKeyPress=true;editor.DeleteSelected();}};}
 internal static GridEditHistory For(Form owner){ActiveGrid slot;if(!active.TryGetValue(owner,out slot)||slot.History==null)return null;var editor=slot.History;return !editor.grid.IsDisposed&&editor.grid.Visible&&owner.Contains(editor.grid)?editor:null;}
 void Notify(){if(StateChanged!=null)StateChanged();}
 void Clear(){if(restoring)return;before=null;history.Clear();Notify();}
 void Record(GridEditAction action){history.Record(action);history.LimitCost(item=>(long)(item.Before.Rows.Count+item.After.Rows.Count)*Math.Max(1,grid.ColumnCount),60000);}
 void Complete(){if(completing||restoring||before==null)return;completing=true;try{var old=before;before=null;if(busy()){history.Clear();return;}var after=Snapshot();if(!old.SameInputs(after)||!sameExtra(old.Extra,after.Extra))Record(new GridEditAction{Before=old,After=after,Description="Edit inputs"});}finally{completing=false;Notify();}}
 bool Commit(){if(busy()||restoring||grid.ReadOnly)return false;if(!grid.EndEdit())return false;Complete();return true;}
 internal List<DataGridViewRow> SelectedRows(){return grid.SelectedCells.Cast<DataGridViewCell>().Select(cell=>cell.OwningRow).Concat(grid.SelectedRows.Cast<DataGridViewRow>()).Distinct().Where(row=>!row.IsNewRow&&row.Visible&&!row.ReadOnly&&row.Cells.Cast<DataGridViewCell>().Any(cell=>!cell.ReadOnly)&&grid.AllowUserToDeleteRows&&!grid.ReadOnly).OrderBy(row=>row.Index).ToList();}
 void Apply(Action action){bool was=busy();var original=Snapshot();restoring=true;before=null;grid.SuspendLayout();setBusy(true);try{try{action();}catch{Restore(original);throw;}finally{setBusy(was);grid.ResumeLayout(false);}changed();}finally{restoring=false;Notify();}}
 /* Owner (18 November 2026): the row whose × was clicked is deleted, never the row of a cell selected elsewhere (the
    selected cells used to be added to the deletion). */
 internal void DeleteRow(int index){if(index<0||index>=grid.Rows.Count||grid.Rows[index].IsNewRow)return;var target=grid.Rows[index];DeleteRows(new[]{target});}
 internal void DeleteSelected(){DeleteRows(SelectedRows());}
 internal void ClearSelectedCells(){if(grid.ReadOnly||!Commit())return;var cells=grid.SelectedCells.Cast<DataGridViewCell>().Where(c=>!c.ReadOnly&&!c.OwningRow.IsNewRow&&c.Visible&&!(c is DataGridViewComboBoxCell)&&!(c is DataGridViewCheckBoxCell)&&!(c is DataGridViewButtonCell)&&c.Value!=null&&Convert.ToString(c.Value)!="").ToList();if(cells.Count==0)return;
  var old=Snapshot();Apply(()=>{foreach(var cell in cells)cell.Value="";Record(new GridEditAction{Before=old,After=Snapshot(),Description="Clear cells"});});}
 internal void DeleteRows(System.Collections.Generic.IEnumerable<DataGridViewRow> selection,Action afterDelete=null){if(!grid.AllowUserToDeleteRows||!Commit())return;var rows=selection.Where(row=>row.DataGridView==grid&&!row.IsNewRow&&row.Visible&&!row.ReadOnly&&row.Cells.Cast<DataGridViewCell>().Any(cell=>!cell.ReadOnly)).Distinct().ToList();if(rows.Count==0)return;if(!ConfirmDelete(grid.FindForm(),rows.Count))return;var old=Snapshot();Apply(()=>{foreach(var row in rows.OrderByDescending(r=>r.Index))grid.Rows.Remove(row);if(afterDelete!=null)afterDelete();Record(new GridEditAction{Before=old,After=Snapshot(),Description="Delete rows",Deleted=rows.Count});});}
 internal void Undo(){if(commandTarget!=null){commandTarget.Undo();return;}if(!Commit()||!history.CanUndo)return;Apply(()=>history.Undo(action=>Restore(action.Before)));}
 internal void Redo(){if(commandTarget!=null){commandTarget.Redo();return;}if(!Commit()||!history.CanRedo)return;if(history.NextRedo.Deleted>0&&!ConfirmDelete(grid.FindForm(),history.NextRedo.Deleted))return;Apply(()=>history.Redo(action=>Restore(action.After)));}
 internal bool CanUndo{get{return commandTarget!=null?commandTarget.CanUndo:history.CanUndo;}}internal bool CanRedo{get{return commandTarget!=null?commandTarget.CanRedo:history.CanRedo;}}
 internal static void AddCommands(Form owner,FlowLayoutPanel panel){var undo=new Button{Text="Undo",AutoSize=true};var redo=new Button{Text="Redo",AutoSize=true};undo.Click+=(s,e)=>Safe(owner,h=>h.Undo());redo.Click+=(s,e)=>Safe(owner,h=>h.Redo());panel.Controls.AddRange(new Control[]{undo,redo});Action refresh=()=>{if(owner.IsDisposed)return;var h=For(owner);undo.Enabled=h!=null&&h.CanUndo;redo.Enabled=h!=null&&h.CanRedo;};StateChanged+=refresh;owner.Disposed+=(s,e)=>StateChanged-=refresh;refresh();}
 static void Safe(Form owner,Action<GridEditHistory> action){try{var editor=For(owner);if(editor!=null)action(editor);}catch(Exception error){MessageBox.Show(owner,error.Message,"Edit rows",MessageBoxButtons.OK,MessageBoxIcon.Information);}}
}
}
