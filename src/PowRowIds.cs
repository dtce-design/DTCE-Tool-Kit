using System;using System.Collections.Generic;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Automatic row IDs for every POW measurement sheet (owner, 17 November 2026), as structural steel and walls
// already had. Only a BLANK ID on a row that has input is filled; an ID that already exists is never changed,
// so saved projects and references (opening links, finish IDs, structure IDs) keep their names.
partial class ProgramOfWorksForm {
 static readonly Dictionary<string,string> RowIdPrefixes=new Dictionary<string,string>(StringComparer.Ordinal){
  {"Wood","WD"},{"Aluminum","AL"},{"CHBOpenings","OP"},{"FinishSchedule","FT"},{"WallFinishes","WF"},{"CeilingFinishes","CF"},{"FloorFinishes","FF"},{"OtherFinishes","OF"},
  {"RoofSheets","RF"},{"RoofAccessories","RA"},{"CutFill","CUT"},{"Footings","F"},{"Canopies","CN"},{"GravelEntries","GR"},{"MouldingEntries","MD"},
  {"PlumbingPipes","PP"},{"PlumbingLS","PLS"},{"PlumbingChambers","ST"},{"PlumbingRebar","PB"},{"PlumbingMembers","PM"},{"RCWalls","RW"},
  {"Clearing","CG"},{"Trees","TR"},{"SlopeFill","SF"},{"SlopeStone","SM"},{"StructuralColumn","C"},{"WallFooting","WFT"},{"Railing","RL"},{"SagRods","SR"},
  /* every estimate its own prefix, so an ID is not repeated in the project (owner, 23 November 2026: "unique entry id per works or per
     estimate"; chose per estimate); IDs already in a project are kept */
  {"Carpentry0","MBC"},{"Carpentry1","MWC"},{"Carpentry2","ALC"},{"Carpentry3","RFF"},{"Carpentry4","FLF"},{"Carpentry5","FB"},{"Carpentry6","LPB"},
  {"Pavement0","REX"},{"Pavement1","SGP"},{"Pavement2","ABS"},{"Pavement3","PC150"},{"Pavement4","PC230"},{"Pavement5","SW"}};
 IEnumerable<KeyValuePair<string,DataGridView>> AutoIdGrids(){
  var tables=Tables();foreach(var item in tables)if(RowIdPrefixes.ContainsKey(item.Key))yield return item;
  foreach(var item in extraGrids.Values)if(item.Grid!=null&&!tables.ContainsKey(item.Key))yield return new KeyValuePair<string,DataGridView>(item.Key,item.Grid);}
 internal static string RowIdPrefix(string key){string prefix;if(RowIdPrefixes.TryGetValue(key,out prefix))return prefix;var letters=new string(key.Where(char.IsUpper).ToArray());return letters.Length>0?letters:key.Substring(0,Math.Min(3,key.Length)).ToUpperInvariant();}
 void AssignRowIds(){
  /* Masonry walls keep their W001 numbering; a wall row with input but a blank ID (e.g. created by paste) gets the next free W number. */
  foreach(var grid in MasonryGrids()){if(grid==null)continue;foreach(DataGridViewRow row in grid.Rows)if(!row.IsNewRow&&Cell(row,0)==""&&StructuralSteelIds.HasInputs(row))row.Cells[0].Value=NextWallId(grid);}
  foreach(var item in AutoIdGrids()){
   var grid=item.Value;if(grid.Columns.Count<2||grid.Columns[0].ReadOnly||grid.Columns[0].HeaderText.IndexOf("ID",StringComparison.Ordinal)<0)continue;
   string prefix=RowIdPrefix(item.Key);var used=new HashSet<string>(grid.Rows.Cast<DataGridViewRow>().Where(r=>!r.IsNewRow).Select(r=>Cell(r,0)).Where(s=>s!=""),StringComparer.OrdinalIgnoreCase);
   /* Lowest unused number (owner, 17 November 2026): no gaps such as RW-001, RW-003 after a row was removed. */
   int counter=0;
   foreach(DataGridViewRow row in grid.Rows)if(Cell(row,0)==""&&StructuralSteelIds.HasInputs(row))row.Cells[0].Value=StructuralSteelIds.Next(prefix,used,ref counter);
   if(!grid.Columns[0].HeaderText.EndsWith("(auto)"))grid.Columns[0].HeaderText+=" (auto)";
   grid.Columns[0].ToolTipText="Left blank, an ID is assigned automatically once the row has input. An ID you typed or an existing ID is never changed.";}
  MatchCutFillIds();}
 /* Owner (18 November 2026, "yung ID niya CUT pero fill naman ang meron"): automatic Cut & Fill IDs follow the
    entered areas - CUT-nnn (cut only), FILL-nnn (fill only), CUTFILL-nnn (both). Only automatic IDs of that form
    are relabelled; an ID typed by the user is kept. */
 static readonly System.Text.RegularExpressions.Regex AutoCutFillId=new System.Text.RegularExpressions.Regex(@"^(CUT|FILL|CUTFILL)-(\d+)$",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
 void MatchCutFillIds(){if(cutFill==null)return;Func<DataGridViewRow,int,bool> positive=(row,c)=>{double v;return double.TryParse(Cell(row,c),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out v)&&v>0;};
  var used=new HashSet<string>(cutFill.Rows.Cast<DataGridViewRow>().Where(r=>!r.IsNewRow).Select(r=>Cell(r,0)).Where(x=>x!=""),StringComparer.OrdinalIgnoreCase);
  foreach(DataGridViewRow row in cutFill.Rows){if(row.IsNewRow)continue;var match=AutoCutFillId.Match(Cell(row,0));if(!match.Success)continue;
   bool cut=positive(row,4)||positive(row,5),fill=positive(row,6)||positive(row,7);if(!cut&&!fill)continue;string prefix=cut&&fill?"CUTFILL":fill?"FILL":"CUT";
   if(string.Equals(match.Groups[1].Value,prefix,StringComparison.OrdinalIgnoreCase))continue;
   string id=prefix+"-"+match.Groups[2].Value;if(used.Contains(id)){int counter=0;id=StructuralSteelIds.Next(prefix,used,ref counter);}
   used.Remove(Cell(row,0));used.Add(id);row.Cells[0].Value=id;}}
}
}
