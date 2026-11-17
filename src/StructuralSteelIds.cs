using System;using System.Linq;using System.Collections.Generic;using System.Globalization;using System.Windows.Forms;
namespace BeamScheduleFormatter {
static class StructuralSteelIds {
 internal static string Next(string prefix,HashSet<string> used,ref int counter){string id;do{id=prefix+"-"+(++counter).ToString("000",CultureInfo.InvariantCulture);}while(used.Contains(id));used.Add(id);return id;}
 internal static bool HasInputs(DataGridViewRow row){if(row.IsNewRow)return false;for(int c=1;c<row.Cells.Count;c++){var cell=row.Cells[c];if(cell.ReadOnly||cell is DataGridViewCheckBoxCell)continue;string header=cell.OwningColumn.HeaderText.ToLowerInvariant(),value=Convert.ToString(cell.Value).Trim();if(value==""||header.Contains("grade")||header.Contains("finish")||header.Contains("type")||header.Contains("method")||header.Contains("material"))continue;return true;}return false;}
}
partial class ProgramOfWorksForm {
 readonly Dictionary<string,int> steelIdCounters=new Dictionary<string,int>();
 void AssignStructuralSteelIds(){var tables=SteelTables();var used=new HashSet<string>(tables.Values.SelectMany(g=>g.Rows.Cast<DataGridViewRow>()).Where(r=>!r.IsNewRow).Select(r=>Cell(r,0)).Where(s=>s!=""),StringComparer.OrdinalIgnoreCase);var prefixes=new Dictionary<string,string>{{"SteelPurlins","PUR"},{"SteelAngles","ANG"},{"MachineBolts","MB"},{"AnchorBolts","AB"},{"SteelPlates","PL"},{"CrossBracing","BR"},{"FasciaFrame","MF"}};foreach(var table in tables){string prefix=prefixes[table.Key];int counter=0;/* lowest unused number, no gaps */foreach(DataGridViewRow row in table.Value.Rows)if(Cell(row,0)==""&&StructuralSteelIds.HasInputs(row))row.Cells[0].Value=StructuralSteelIds.Next(prefix,used,ref counter);steelIdCounters[prefix]=counter;table.Value.Columns[0].HeaderText="ID (auto)";table.Value.Columns[0].ToolTipText="Blank IDs are assigned automatically when a row has input. Existing IDs are preserved.";}}
}
}
