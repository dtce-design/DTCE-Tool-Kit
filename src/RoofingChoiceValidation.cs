using System;using System.Linq;using System.Collections.Generic;using System.Windows.Forms;
namespace BeamScheduleFormatter {
partial class ProgramOfWorksForm {
 bool updatingRoofChoices;
 static void AddMissingChoices(DataGridViewComboBoxCell.ObjectCollection items,IEnumerable<string> choices){foreach(string value in choices)if(!items.Contains(value))items.Add(value);}
 static void RemoveOldChoices(DataGridViewComboBoxCell.ObjectCollection items,IEnumerable<string> choices){var keep=new HashSet<string>(choices,StringComparer.Ordinal);for(int i=items.Count-1;i>=0;i--)if(!keep.Contains(Convert.ToString(items[i])))items.RemoveAt(i);}
 void RefreshRoofProfileChoices(string[] names){bool updating=updatingRoofChoices,before=filling;updatingRoofChoices=true;filling=true;try{var column=(DataGridViewComboBoxColumn)roofSheets.Columns[2];AddMissingChoices(column.Items,names);foreach(DataGridViewRow row in roofSheets.Rows){string selected=Cell(row,2);string canonical=names.FirstOrDefault(name=>string.Equals(name,selected,StringComparison.OrdinalIgnoreCase));if(canonical!=null&&!object.Equals(row.Cells[2].Value,canonical))row.Cells[2].Value=canonical;}RemoveOldChoices(column.Items,names);}finally{updatingRoofChoices=updating;filling=before;}}
 void RefreshRoofLapChoices(DataGridViewRow row,List<RoofProfile> profiles,string lap){bool updating=updatingRoofChoices,before=filling;updatingRoofChoices=true;filling=true;try{var choices=profiles.Select(p=>p.SideLap).Distinct(StringComparer.Ordinal).ToArray();var cell=(DataGridViewComboBoxCell)row.Cells[17];AddMissingChoices(cell.Items,choices);row.Cells[2].Value=profiles[0].Name;row.Cells[17].Value=lap;RemoveOldChoices(cell.Items,choices);row.Cells[2].ErrorText="";cell.ErrorText="";}finally{updatingRoofChoices=updating;filling=before;}}
}
}
