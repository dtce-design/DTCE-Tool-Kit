using System;using System.Collections.Generic;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Windows performance (owner, 17 November 2026; CI probe: beam snapshot 2.5 s, beam restore 4.1 s on Windows versus
// 0.1 s on Mono). With AutoSizeRowsMode on, WinForms re-measures a row (wrapped text) on every cell value change, so a
// recalculation that writes thousands of cells measured thousands of rows. Row auto-size is paused for the batch and
// done once when it ends.
static class GridBatch {
 sealed class Scope:IDisposable {internal readonly List<KeyValuePair<DataGridView,DataGridViewAutoSizeRowsMode>> Grids=new List<KeyValuePair<DataGridView,DataGridViewAutoSizeRowsMode>>();
  public void Dispose(){foreach(var item in Grids){var g=item.Key;if(g.IsDisposed||g.AutoSizeRowsMode!=DataGridViewAutoSizeRowsMode.None)continue;try{g.AutoSizeRowsMode=item.Value;}catch(Exception){}}Grids.Clear();}}
 internal static IDisposable Suspend(Control root){var scope=new Scope();if(root!=null&&!root.IsDisposed)Collect(root,scope);return scope;}
 static void Collect(Control control,Scope scope){var g=control as DataGridView;if(g!=null){if(g.AutoSizeRowsMode!=DataGridViewAutoSizeRowsMode.None){scope.Grids.Add(new KeyValuePair<DataGridView,DataGridViewAutoSizeRowsMode>(g,g.AutoSizeRowsMode));try{g.AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.None;}catch(Exception){scope.Grids.RemoveAt(scope.Grids.Count-1);}}return;}
  foreach(Control child in control.Controls)Collect(child,scope);}
}
}
