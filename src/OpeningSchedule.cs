using System;using System.Collections.Generic;using System.Drawing;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// One Door & Window Schedule for Wood, Aluminum and PVC openings, and opening moulding chosen from Finishing Works
// (owner, 17 November 2026).
// - Material column (index 25): Wood keeps jamb/leaf/paint inputs; Aluminum and PVC are area-based without paint or
//   wooden jamb (door hardware still counted). Rows from the old Aluminum sheet are moved into the schedule.
// - Opening moulding: Finishing Works > Moulding rows can pick an Opening ID. That opening's moulding (length from its
//   size and quantity) is used; the old "Exterior moulding" flag/profile on the opening row stay as hidden, saved
//   fields kept in step, so older projects open with the same moulding quantities.
partial class ProgramOfWorksForm {
 internal const int OpeningMaterialColumn=25,MouldingOpeningColumn=7;
 internal static readonly string[] OpeningMaterials={"Wood","Aluminum","PVC","Steel"};/* Steel added (owner, 18 November 2026: "additional type Steel Window") */
 readonly HashSet<string> removedMouldingOpenings=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 void AddOpeningMaterialColumn(DataGridView g,string fallback){
  var column=new DataGridViewComboBoxColumn{Name="OpeningMaterial",HeaderText="Material",Width=110,DisplayStyle=DataGridViewComboBoxDisplayStyle.DropDownButton};column.Items.AddRange(OpeningMaterials);g.Columns.Add(column);
  column.DefaultCellStyle.BackColor=ModernUi.Input;column.ToolTipText="Wood: jamb, leaf and paint are measured. Aluminum / PVC / Steel: opening area, no paint or wooden jamb. Windows are Aluminum or Steel only. Aluminum has no lockset / hinges.";
  foreach(int c in new[]{22,23,24})g.Columns[c].Visible=false;/* opening moulding is chosen in Finishing Works > Moulding */
  g.DefaultValuesNeeded+=(s,e)=>{if(e.Row.Cells[OpeningMaterialColumn].Value==null)e.Row.Cells[OpeningMaterialColumn].Value=fallback;};AttachOpeningIdRules(g);
  g.RowsAdded+=(s,e)=>{if(filling)return;for(int i=e.RowIndex;i<e.RowIndex+e.RowCount&&i<g.Rows.Count;i++){var row=g.Rows[i];if(!row.IsNewRow&&row.Cells[OpeningMaterialColumn].Value==null)row.Cells[OpeningMaterialColumn].Value=fallback;}};
  g.CellValueChanged+=(s,e)=>{if(filling||e.RowIndex<0||e.ColumnIndex!=OpeningMaterialColumn)return;var row=g.Rows[e.RowIndex];bool woodRow=OpeningMaterial(row,true)=="Wood";filling=true;try{row.Cells[11].Value=woodRow&&Cell(row,3)=="Door"?"1":"0";row.Cells[12].Value=!woodRow?"0":Cell(row,3)=="Door"?"3":"2";}finally{filling=false;}Recalculate();};}
 /* Owner (20 November 2026): "kapag ako naglagay ng Opening ID e if D yung prefix is door na yung type and W is Window, if
    window aluminum or steel lang". Typing an Opening ID that starts with D sets Type = Door, W sets Type = Window, O sets Type = Opening (no material) (the automatic
    WD001 numbering is not read as a window). A window is Aluminum or Steel only: a window row on Wood or PVC is switched to
    Aluminum, and choosing Wood or PVC for a window is not allowed. Only the user's own edits trigger this; saved projects open
    unchanged. */
 internal static readonly string[] WindowMaterials={"Aluminum","Steel"};
 internal static string TypeFromOpeningId(string id){id=(id??"").Trim().ToUpperInvariant();if(id==""||System.Text.RegularExpressions.Regex.IsMatch(id,@"^WD\d+$"))return null;return id[0]=='D'?"Door":id[0]=='W'?"Window":id[0]=='O'?"Opening":null;}
 void AttachOpeningIdRules(DataGridView g){
  g.CellValueChanged+=(s,e)=>{if(filling||e.RowIndex<0||g==manualOpeningsGrid||g.Rows[e.RowIndex].IsNewRow&&e.ColumnIndex!=0)return;if(e.ColumnIndex!=0&&e.ColumnIndex!=3&&e.ColumnIndex!=OpeningMaterialColumn)return;var row=g.Rows[e.RowIndex];bool changed=false;string message=null;
   filling=true;try{
    if(e.ColumnIndex==0){string type=TypeFromOpeningId(Cell(row,0));if(type!=null&&Cell(row,3)!=type){row.Cells[3].Value=type;changed=true;}}
    if(Cell(row,3)=="Window"&&!WindowMaterials.Contains(OpeningMaterial(row,g==wood))){if(e.ColumnIndex==OpeningMaterialColumn)message="Windows are Aluminum or Steel only.";row.Cells[OpeningMaterialColumn].Value="Aluminum";changed=true;}
    /* O prefix (owner: "and O prefix opening", "material is wala"): a plain opening has no material and no hardware */
    if(Cell(row,3)=="Opening"){if(Convert.ToString(row.Cells[OpeningMaterialColumn].Value)!=""){row.Cells[OpeningMaterialColumn].Value="";changed=true;}if(changed){row.Cells[11].Value="0";row.Cells[12].Value="0";}}
    else if(changed){bool woodRow=OpeningMaterial(row,g==wood)=="Wood";row.Cells[11].Value=woodRow&&Cell(row,3)=="Door"?"1":"0";row.Cells[12].Value=!woodRow?"0":Cell(row,3)=="Door"?"3":"2";}}
   finally{filling=false;}
   if(changed&&message==null)Recalculate();if(message!=null&&g.IsHandleCreated)g.BeginInvoke((Action)(()=>{MessageBox.Show(g.FindForm(),message,"Door & Window Schedule",MessageBoxButtons.OK,MessageBoxIcon.Information);Recalculate();}));};}
 static void PlaceOpeningMaterialColumn(DataGridView g){if(g.Columns.Count>OpeningMaterialColumn)g.Columns[OpeningMaterialColumn].DisplayIndex=Math.Min(g.Columns.Count-1,g.Columns[3].DisplayIndex+1);}
 static string OpeningMaterial(DataGridViewRow row,bool woodGrid){string v=row.Cells.Count>OpeningMaterialColumn?Convert.ToString(row.Cells[OpeningMaterialColumn].Value).Trim():"";var match=OpeningMaterials.FirstOrDefault(m=>string.Equals(m,v,StringComparison.OrdinalIgnoreCase));return match??(woodGrid?"Wood":"Aluminum");}
 /* Owner (18 November 2026, "if PVC or aluminum, wala naman ng leaves, wala na rin paint jamb"): for PVC / aluminum rows the
    wood-only inputs (jamb depth and thickness, leaf thickness, paint faces, leaves per unit, paint) are blank and locked. */
 static void StyleOpeningMaterialCells(DataGridViewRow row,bool woodRow){foreach(int c in new[]{7,8,9,10,20}){if(c>=row.Cells.Count)continue;row.Cells[c].ReadOnly=!woodRow;row.Cells[c].Style.BackColor=woodRow?ModernUi.Input:Color.FromArgb(235,237,240);if(!woodRow&&Convert.ToString(row.Cells[c].Value)!="")row.Cells[c].Value="";}
  if(row.DataGridView==null||!row.DataGridView.Columns.Contains(PaintLeafColumn))return;int pc=row.DataGridView.Columns[PaintLeafColumn].Index;
  if(woodRow){if(!(row.Cells[pc] is DataGridViewCheckBoxCell))row.Cells[pc]=new DataGridViewCheckBoxCell{Value=false};row.Cells[pc].ReadOnly=false;row.Cells[pc].Style.BackColor=ModernUi.Input;}
  else{if(row.Cells[pc] is DataGridViewCheckBoxCell)row.Cells[pc]=new DataGridViewTextBoxCell{Value=""};row.Cells[pc].ReadOnly=true;row.Cells[pc].Style.BackColor=Color.FromArgb(235,237,240);}}
 void MoveAluminumRowsIntoSchedule(){if(aluminum==null||wood==null)return;var rows=aluminum.Rows.Cast<DataGridViewRow>().Where(r=>!r.IsNewRow&&!Empty(r)).ToList();if(rows.Count==0)return;
  foreach(var source in rows){var target=wood.Rows[wood.Rows.Add()];int n=Math.Min(source.Cells.Count,target.Cells.Count);for(int c=0;c<n;c++)target.Cells[c].Value=source.Cells[c].Value;string m=Convert.ToString(source.Cells.Count>OpeningMaterialColumn?source.Cells[OpeningMaterialColumn].Value:null);target.Cells[OpeningMaterialColumn].Value=m=="PVC"?"PVC":"Aluminum";}
  foreach(var source in rows)aluminum.Rows.Remove(source);}
 void AddMouldingOpeningColumn(){
  var column=new DataGridViewComboBoxColumn{Name="MouldingOpening",HeaderText="Opening ID (door / window)",Width=170,DisplayStyle=DataGridViewComboBoxDisplayStyle.DropDownButton};column.Items.Add("");mouldingEntries.Columns.Add(column);
  column.DisplayIndex=1;column.DefaultCellStyle.BackColor=ModernUi.Input;column.ToolTipText="Choose a door or window from the Door & Window Schedule: its moulding length comes from its size and quantity. Leave blank for other moulding and enter the length.";
  mouldingEntries.DataError+=(s,e)=>{if(e.ColumnIndex==MouldingOpeningColumn)e.ThrowException=false;};
  mouldingEntries.CurrentCellDirtyStateChanged+=(s,e)=>{if(mouldingEntries.IsCurrentCellDirty&&mouldingEntries.CurrentCell!=null&&mouldingEntries.CurrentCell.ColumnIndex==MouldingOpeningColumn)mouldingEntries.CommitEdit(DataGridViewDataErrorContexts.Commit);};
  mouldingEntries.CellValueChanged+=(s,e)=>{if(filling||e.RowIndex<0||e.ColumnIndex!=MouldingOpeningColumn)return;Recalculate();};
  mouldingEntries.UserDeletingRow+=(s,e)=>{string id=Cell(e.Row,MouldingOpeningColumn);if(id!="")removedMouldingOpenings.Add(id);};
  mouldingEntries.RowsRemoved+=(s,e)=>{if(!filling&&removedMouldingOpenings.Count>0)Recalculate();};}
 // Runs before the openings are measured: the moulding sheet decides which openings carry moulding.
 void SyncOpeningMouldings(){if(mouldingEntries==null||wood==null||mouldingEntries.Columns.Count<=MouldingOpeningColumn)return;
  var openingRows=wood.Rows.Cast<DataGridViewRow>().Where(r=>!r.IsNewRow&&Cell(r,0)!="").ToList();
  var byId=new Dictionary<string,DataGridViewRow>(StringComparer.OrdinalIgnoreCase);foreach(var r in openingRows)if(!byId.ContainsKey(Cell(r,0)))byId[Cell(r,0)]=r;
  foreach(string id in removedMouldingOpenings){DataGridViewRow r;if(byId.TryGetValue(id,out r))r.Cells[22].Value=false;}removedMouldingOpenings.Clear();
  var referenced=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  /* the moulding quantity typed on the moulding sheet (blank = every unit of the opening) */mouldingQuantities.Clear();foreach(DataGridViewRow m in mouldingEntries.Rows){if(m.IsNewRow)continue;string id=Cell(m,MouldingOpeningColumn),q=Cell(m,4);if(id!=""&&q!="")mouldingQuantities[id]=q;}
  foreach(DataGridViewRow m in mouldingEntries.Rows){if(m.IsNewRow)continue;string id=Cell(m,MouldingOpeningColumn);if(id=="")continue;referenced.Add(id);DataGridViewRow r;if(byId.TryGetValue(id,out r)){r.Cells[22].Value=true;r.Cells[24].Value=Cell(m,2);}}
  foreach(var r in openingRows){if(!Checked(r,22)||referenced.Contains(Cell(r,0)))continue;/* older project: opening flagged for exterior moulding -> show it on the moulding sheet */
   var m=mouldingEntries.Rows[mouldingEntries.Rows.Add()];m.Cells[1].Value=(Cell(r,3)==""?"Opening":Cell(r,3))+" "+Cell(r,0)+(Cell(r,1)==""?"":" | "+Cell(r,1));m.Cells[2].Value=Cell(r,24);m.Cells[MouldingOpeningColumn].Value=Cell(r,0);referenced.Add(Cell(r,0));}
  /* openings no longer referenced lose the flag */foreach(var r in openingRows)if(Checked(r,22)&&!referenced.Contains(Cell(r,0)))r.Cells[22].Value=false;
  var items=((DataGridViewComboBoxColumn)mouldingEntries.Columns[MouldingOpeningColumn]).Items;var ids=new[]{""}.Concat(openingRows.Select(r=>Cell(r,0))).Concat(referenced).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
  if(!items.Cast<object>().Select(Convert.ToString).SequenceEqual(ids)){items.Clear();items.AddRange(ids);}}
 // Moulding rows tied to an opening show that opening's moulding; the quantity is counted once through the opening.
 readonly Dictionary<string,string> mouldingQuantities=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
 /* fewer units than the opening may get moulding; the typed number is kept while it differs from the opening quantity, so a blank
    or the full quantity follows the schedule */
 void ApplyMouldingQuantity(OpeningItem o){string q;if(!o.Exterior||!mouldingQuantities.TryGetValue(o.Id,out q))return;int n;if(!int.TryParse(q,System.Globalization.NumberStyles.Integer,System.Globalization.CultureInfo.InvariantCulture,out n)||n<0||n>o.Quantity){errors.Add("Moulding of opening "+o.Id+": the quantity must be a whole number from 0 to "+o.Quantity+"; all units counted.");return;}if(n!=o.Quantity)o.MouldingQuantity=n;}
 bool OpeningMouldingRow(DataGridViewRow row){if(row.Cells.Count<=MouldingOpeningColumn)return false;string id=Cell(row,MouldingOpeningColumn);if(id=="")return false;var opening=openings.FirstOrDefault(o=>string.Equals(o.Id,id,StringComparison.OrdinalIgnoreCase));
  row.Cells[3].ReadOnly=true;row.Cells[3].Style.BackColor=Color.FromArgb(235,237,240);row.Cells[4].ReadOnly=false;row.Cells[4].Style.BackColor=ModernUi.Input;
  if(opening==null){row.Cells[6].Value="Opening "+id+" not found in the Door & Window Schedule";errors.Add("Moulding "+Cell(row,0)+": opening "+id+" not found");return true;}
  int units=opening.MouldingQuantity>=0?opening.MouldingQuantity:opening.Quantity;row.Cells[3].Value=opening.Quantity>0?OpeningMoulding.Length(opening.Kind,opening.Width,opening.Height,1,true).ToString("0.###",System.Globalization.CultureInfo.InvariantCulture):"";row.Cells[4].Value=units.ToString(System.Globalization.CultureInfo.InvariantCulture);Measure(row,5,opening.MouldingLength);row.Cells[6].Value="From opening "+opening.Id+(units<opening.Quantity?" ("+units+" of "+opening.Quantity+" units)":"");return true;}
 // Include checkboxes for opening painting (owner, 17 November 2026). Checked = painted; new rows start unticked (owner, 18 November 2026).
 // Saved as entry attributes pl / pj ("0" when unchecked) so older files keep their paint quantities.
 /* A plain "Opening" (owner, 18 November 2026: "walang material, walang anything, dimension lang"): only ID, description, wall,
    width, height and quantity are used; material, hardware and moulding are blank and locked. */
 static void StylePlainOpeningCells(DataGridViewRow row,bool plain){foreach(int c in new[]{11,12,22,24,OpeningMaterialColumn}){if(c>=row.Cells.Count)continue;var cell=row.Cells[c];
   if(plain){cell.ReadOnly=true;cell.Style.BackColor=Color.FromArgb(235,237,240);if(c==22){if(Convert.ToString(cell.Value)=="True")cell.Value=false;}else if(c!=11&&c!=12&&Convert.ToString(cell.Value)!="")cell.Value="";}
   else if(c==OpeningMaterialColumn||c==22||c==24){cell.ReadOnly=false;cell.Style.BackColor=Color.Empty;}}}
 internal const string PaintLeafColumn="OpeningPaintLeaf",PaintJambColumn="OpeningPaintJamb";
 void AddOpeningPaintColumns(DataGridView g){
  foreach(var spec in new[]{new[]{PaintLeafColumn,"Paint (leaf and jamb)","Include painting of the wooden leaf (faces as set in Paint faces, plus edges) and its jamb."},new[]{PaintJambColumn,"Paint jamb","Include painting of the wooden jamb."}}){
   var c=new DataGridViewCheckBoxColumn{Name=spec[0],HeaderText=spec[1],ToolTipText=spec[2],Width=95};c.DefaultCellStyle.BackColor=ModernUi.Input;g.Columns.Add(c);}
  /* Owner (18 November 2026, "i-combine na lang yung paint ng leaf at jamb"): one checkbox paints both; the jamb column stays hidden for older files. */g.Columns[PaintJambColumn].Visible=false;
  g.DefaultValuesNeeded+=(s,e)=>{foreach(string n in new[]{PaintLeafColumn,PaintJambColumn})if(e.Row.Cells[n].Value==null)e.Row.Cells[n].Value=false;};/* owner, 18 November 2026: paint is unticked by default */
  g.RowsAdded+=(s,e)=>{for(int i=e.RowIndex;i<e.RowIndex+e.RowCount&&i<g.Rows.Count;i++){var row=g.Rows[i];if(row.IsNewRow)continue;foreach(string n in new[]{PaintLeafColumn,PaintJambColumn})if(row.Cells[n].Value==null)row.Cells[n].Value=false;}};
  g.CurrentCellDirtyStateChanged+=(s,e)=>{if(g.IsCurrentCellDirty&&g.CurrentCell!=null&&IsOpeningPaintColumn(g.Columns[g.CurrentCell.ColumnIndex]))g.CommitEdit(DataGridViewDataErrorContexts.Commit);};
  g.CellValueChanged+=(s,e)=>{if(filling||e.RowIndex<0||!IsOpeningPaintColumn(g.Columns[e.ColumnIndex]))return;Recalculate();};}
 static bool IsOpeningPaintColumn(DataGridViewColumn c){return c.Name==PaintLeafColumn||c.Name==PaintJambColumn;}
 static void PlaceOpeningPaintColumns(DataGridView g){if(!g.Columns.Contains(PaintLeafColumn))return;g.Columns[PaintLeafColumn].DisplayIndex=Math.Min(g.Columns.Count-1,g.Columns[10].DisplayIndex+1);g.Columns[PaintJambColumn].DisplayIndex=Math.Min(g.Columns.Count-1,g.Columns[PaintLeafColumn].DisplayIndex+1);}
 static bool OpeningPaint(DataGridViewRow row,string name){if(row.DataGridView==null||!row.DataGridView.Columns.Contains(name))return true;var v=row.Cells[name].Value;return v is bool&&(bool)v;}
 static void WriteOpeningPaint(string section,DataGridViewRow row,System.Xml.XmlElement entry){if(section!="Wood"&&section!="Aluminum")return;if(!OpeningPaint(row,PaintLeafColumn))entry.SetAttribute("pl","0");if(!OpeningPaint(row,PaintLeafColumn))entry.SetAttribute("pj","0");}
 static void ReadOpeningPaint(string section,DataGridViewRow row,System.Xml.XmlElement entry){if((section!="Wood"&&section!="Aluminum")||!row.DataGridView.Columns.Contains(PaintLeafColumn))return;row.Cells[PaintLeafColumn].Value=entry.GetAttribute("pl")!="0";row.Cells[PaintJambColumn].Value=entry.GetAttribute("pj")!="0";}
}
}
