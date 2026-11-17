using System;using System.Collections.Generic;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Automatic row IDs for every POW measurement sheet (owner, 17 November 2026), as structural steel and walls
// already had. Only a BLANK ID on a row that has input is filled; an ID that already exists is never changed,
// so saved projects and references (opening links, finish IDs, structure IDs) keep their names.
partial class ProgramOfWorksForm {
 static readonly Dictionary<string,string> RowIdPrefixes=new Dictionary<string,string>(StringComparer.Ordinal){
  {"Wood","WD"},{"Aluminum","AL"},{"CHBOpenings","OP"},{"FinishSchedule","FT"},{"WallFinishes","WF"},{"CeilingFinishes","CF"},{"FloorFinishes","FF"},
  {"RoofSheets","RF"},{"RoofAccessories","RA"},{"CutFill","CUT"},{"Footings","F"},{"Canopies","CN"},{"GravelEntries","GR"},{"MouldingEntries","MD"},
  {"PlumbingPipes","PP"},{"PlumbingLS","PLS"},{"PlumbingChambers","ST"},{"PlumbingRebar","PB"},{"PlumbingMembers","PM"},{"RCWalls","RW"},
  {"Clearing","CG"},{"Trees","TR"},{"SlopeFill","SF"},{"SlopeStone","SM"},{"StructuralColumn","C"},{"WallFooting","WFT"}};
 IEnumerable<KeyValuePair<string,DataGridView>> AutoIdGrids(){
  foreach(var item in Tables())if(RowIdPrefixes.ContainsKey(item.Key))yield return item;
  foreach(var item in extraGrids.Values)if(item.Grid!=null&&!Tables().ContainsKey(item.Key))yield return new KeyValuePair<string,DataGridView>(item.Key,item.Grid);}
 internal static string RowIdPrefix(string key){string prefix;if(RowIdPrefixes.TryGetValue(key,out prefix))return prefix;var letters=new string(key.Where(char.IsUpper).ToArray());return letters.Length>0?letters:key.Substring(0,Math.Min(3,key.Length)).ToUpperInvariant();}
 void AssignRowIds(){
  foreach(var item in AutoIdGrids()){
   var grid=item.Value;if(grid.Columns.Count<2||grid.Columns[0].ReadOnly||grid.Columns[0].HeaderText.IndexOf("ID",StringComparison.Ordinal)<0)continue;
   string prefix=RowIdPrefix(item.Key);var used=new HashSet<string>(grid.Rows.Cast<DataGridViewRow>().Where(r=>!r.IsNewRow).Select(r=>Cell(r,0)).Where(s=>s!=""),StringComparer.OrdinalIgnoreCase);
   /* Lowest unused number (owner, 17 November 2026): no gaps such as RW-001, RW-003 after a row was removed. */
   int counter=0;
   foreach(DataGridViewRow row in grid.Rows)if(Cell(row,0)==""&&StructuralSteelIds.HasInputs(row))row.Cells[0].Value=StructuralSteelIds.Next(prefix,used,ref counter);
   if(!grid.Columns[0].HeaderText.EndsWith("(auto)"))grid.Columns[0].HeaderText+=" (auto)";
   grid.Columns[0].ToolTipText="Left blank, an ID is assigned automatically once the row has input. An ID you typed or an existing ID is never changed.";}}
}
}
