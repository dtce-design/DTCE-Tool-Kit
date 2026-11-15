using System;using System.Collections.Generic;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
sealed class BoqUndoState {internal Dictionary<string,double?> Manual,Workbook;internal GridRowSnapshot Entries;
 internal static bool Same(object a,object b){var left=(BoqUndoState)a;var right=(BoqUndoState)b;return left.Manual.Count==right.Manual.Count&&left.Manual.All(pair=>right.Manual.ContainsKey(pair.Key)&&right.Manual[pair.Key]==pair.Value)&&left.Entries.SameInputs(right.Entries);}
}
partial class ProgramOfWorksForm {
 void ConfigureBoqUndo(){GridEditHistory.ConfigureExtra(boqGrid,()=>new BoqUndoState{Manual=backupBill.ToDictionary(item=>item.Slot,item=>item.ManualQuantity),Workbook=backupBill.ToDictionary(item=>item.Slot,item=>item.WorkbookQuantity),Entries=GridRowSnapshot.Capture(plumbingLs)},snapshot=>{
  var state=(BoqUndoState)snapshot;var changed=new List<BackupBillItem>();foreach(var item in backupBill){double? desired;if(!state.Manual.TryGetValue(item.Slot,out desired)||item.ManualQuantity==desired)continue;double? previous;state.Workbook.TryGetValue(item.Slot,out previous);item.ManualQuantity=desired??previous??0;changed.Add(item);}
  // Restore quantities by stable entry ID, retaining unrelated rows and bindings.
  foreach(var saved in state.Entries.Rows){string id=Convert.ToString(saved.Cells[0].Value);var row=plumbingLs.Rows.Cast<DataGridViewRow>().FirstOrDefault(r=>!r.IsNewRow&&Cell(r,0)==id);if(row!=null&&!object.Equals(row.Cells[3].Value,saved.Cells[3].Value))row.Cells[3].Value=saved.Cells[3].Value;}
  foreach(var item in changed)UpdateBoundManualRows(item);
 },BoqUndoState.Same);}
}
}
