using System;using System.Collections.Generic;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Copy / paste for POW input sheets (owner, 17 November 2026): text and dropdown cells.
// Ctrl+C copies the selected cells (tab/line separated, like Excel).
// Ctrl+V with one copied value fills every selected editable cell (e.g. a whole column of rows).
// Ctrl+V with several copied cells pastes the block from the top-left selected cell, adding rows when needed.
// Dropdown cells only accept a value that is in their list; read-only/computed cells are never changed.
// One paste is one undo step. While a text cell is being typed in, Ctrl+C/V act on the text as usual.
static class GridClipboard {
 internal static bool Process(WorksDataGridView grid,Keys keys){
  if(keys!=(Keys.Control|Keys.C)&&keys!=(Keys.Control|Keys.V))return false;
  if(grid.CurrentCell==null)return false;
  if(grid.IsCurrentCellInEditMode&&!(grid.EditingControl is ComboBox))return false;
  try{if(keys==(Keys.Control|Keys.C))Copy(grid);else Paste(grid);}
  catch(System.Runtime.InteropServices.ExternalException){MessageBox.Show(grid.FindForm(),"The clipboard is busy. Try again.","Copy / paste",MessageBoxButtons.OK,MessageBoxIcon.Information);}
  catch(Exception ex){MessageBox.Show(grid.FindForm(),ex.Message,"Paste",MessageBoxButtons.OK,MessageBoxIcon.Information);}
  return true;}
 static List<DataGridViewColumn> VisibleColumns(DataGridView grid){return grid.Columns.Cast<DataGridViewColumn>().Where(c=>c.Visible).OrderBy(c=>c.DisplayIndex).ToList();}
 static string Text(DataGridViewCell cell){object v=cell.Value;if(v==null)return "";if(v is bool)return (bool)v?"TRUE":"FALSE";return System.Convert.ToString(v,System.Globalization.CultureInfo.InvariantCulture).Replace("\t"," ").Replace("\r"," ").Replace("\n"," ");}
 internal static string CopyText(DataGridView grid){var cells=grid.SelectedCells.Cast<DataGridViewCell>().Where(c=>c.Visible).ToList();if(cells.Count==0&&grid.CurrentCell!=null)cells.Add(grid.CurrentCell);if(cells.Count==0)return "";
  var columns=VisibleColumns(grid).Where(c=>cells.Any(x=>x.ColumnIndex==c.Index)).ToList();var rows=cells.Select(c=>c.RowIndex).Distinct().OrderBy(r=>r).ToList();
  return string.Join("\r\n",rows.Select(r=>string.Join("\t",columns.Select(c=>{var cell=grid.Rows[r].Cells[c.Index];return cells.Contains(cell)?Text(cell):"";}))));}
 static void Copy(DataGridView grid){string text=CopyText(grid);if(text!="")Clipboard.SetText(text);}
 internal static string[][] Parse(string text){var lines=(text??"").Replace("\r\n","\n").Replace('\r','\n').Split('\n').ToList();while(lines.Count>0&&lines[lines.Count-1]=="")lines.RemoveAt(lines.Count-1);return lines.Select(l=>l.Split('\t')).ToArray();}
 // Returns the value to store, or null when the text cannot go into this cell.
 internal static object Convert(DataGridViewCell cell,string text){
  var combo=cell as DataGridViewComboBoxCell;if(combo!=null){if(text.Trim()=="")return "";var items=combo.Items.Count>0?combo.Items:((DataGridViewComboBoxColumn)cell.OwningColumn).Items;var match=items.Cast<object>().FirstOrDefault(i=>string.Equals(System.Convert.ToString(i),text.Trim(),StringComparison.OrdinalIgnoreCase));return match;}
  if(cell is DataGridViewCheckBoxCell){string t=text.Trim().ToUpperInvariant();if(t=="TRUE"||t=="YES"||t=="1"||t=="X")return true;if(t=="FALSE"||t=="NO"||t=="0"||t=="")return false;return null;}
  return text.Trim();}
 static void Paste(WorksDataGridView grid){
  if(grid.ReadOnly||!Clipboard.ContainsText())return;var result=PasteText(grid,Clipboard.GetText());
  if(result[1]>0)MessageBox.Show(grid.FindForm(),result[0]+" cell(s) pasted. "+result[1]+" cell(s) were skipped: read-only/computed cells, or values that are not in a dropdown's list.","Paste",MessageBoxButtons.OK,MessageBoxIcon.Information);}
 // Returns {changed, skipped}.
 internal static int[] PasteText(WorksDataGridView grid,string text){
  var block=Parse(text);if(block.Length==0||grid.ReadOnly)return new[]{0,0};
  if(grid.IsCurrentCellInEditMode)grid.EndEdit();
  var columns=VisibleColumns(grid);var plan=new List<KeyValuePair<Func<DataGridViewCell>,string>>();
  bool single=block.Length==1&&block[0].Length==1;
  if(single){var targets=grid.SelectedCells.Cast<DataGridViewCell>().Where(c=>c.Visible).ToList();if(targets.Count>1)targets=targets.Where(c=>!c.OwningRow.IsNewRow).ToList();if(targets.Count==0)targets.Add(grid.CurrentCell);
   foreach(var cell in targets.OrderBy(c=>c.RowIndex).ThenBy(c=>c.OwningColumn.DisplayIndex)){var captured=cell;plan.Add(new KeyValuePair<Func<DataGridViewCell>,string>(()=>captured,block[0][0]));}}
  else{var selected=grid.SelectedCells.Cast<DataGridViewCell>().Where(c=>c.Visible).ToList();if(selected.Count==0)selected.Add(grid.CurrentCell);
   int top=selected.Min(c=>c.RowIndex);int left=columns.FindIndex(c=>c.Index==selected.OrderBy(x=>x.OwningColumn.DisplayIndex).First().ColumnIndex);
   for(int r=0;r<block.Length;r++)for(int c=0;c<block[r].Length;c++){if(left+c>=columns.Count)break;int rowOffset=r,columnIndex=columns[left+c].Index;
    plan.Add(new KeyValuePair<Func<DataGridViewCell>,string>(()=>{int row=top+rowOffset;while(row>=grid.Rows.Count||grid.Rows[row].IsNewRow){if(!grid.AllowUserToAddRows&&row>=grid.Rows.Count)return null;grid.Rows.Add();}return grid.Rows[row].Cells[columnIndex];},block[r][c]));}}
  int skipped=0,changed=0;
  grid.ApplyProfilePaste(()=>{foreach(var step in plan){var cell=step.Key();if(cell==null){skipped++;continue;}if(cell.ReadOnly||cell.OwningRow.ReadOnly){if(step.Value.Trim()!=""&&!single)skipped++;continue;}
    var value=Convert(cell,step.Value);if(value==null){skipped++;continue;}if(object.Equals(cell.Value,value)||(System.Convert.ToString(cell.Value)==""&&System.Convert.ToString(value)==""))continue;
    if(cell.OwningRow.IsNewRow){var row=grid.Rows[grid.Rows.Add()];cell=row.Cells[cell.ColumnIndex];}cell.Value=value;changed++;}});
  return new[]{changed,skipped};
 }
}
}
