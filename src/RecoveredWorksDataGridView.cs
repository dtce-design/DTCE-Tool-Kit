using System;using System.Windows.Forms;
namespace BeamScheduleFormatter {
internal class GridInputInitialization:IDisposable {readonly bool previous;readonly Action<bool> set;internal GridInputInitialization(Func<bool> get,Action<bool> set){this.set=set;previous=get();set(true);}public void Dispose(){set(previous);}}
internal class WorksDataGridView:DataGridView {
 internal Func<IDisposable> DefaultValuesScope;
 public WorksDataGridView(){DoubleBuffered=true;}
 protected override void OnDefaultValuesNeeded(DataGridViewRowEventArgs e){if(DefaultValuesScope==null){base.OnDefaultValuesNeeded(e);return;}using(DefaultValuesScope())base.OnDefaultValuesNeeded(e);}
}
}
