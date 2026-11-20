using System;using System.Collections.Generic;using System.Drawing;using System.Linq;using System.Windows.Forms;using System.Xml;
namespace BeamScheduleFormatter {
/* Manual column and footing schedules (owner, 20 November 2026: "yung sa reinforced concrete maglagay na lang ng manual schedule
   sa column at footing. pero dapat yung ibang files bago itong update mababasa parin niya yung lumang format").
   Each type (C1, F1, ...) is entered once on its schedule; a Column Estimate or Footing Estimate row that names the type in
   "Schedule mark" takes its section and bars from the schedule, so a change to the type updates every row that uses it.
   The row keeps its own height / quantity / excavation / materials. Rows without a mark (every row of a project saved before
   this version) keep their own typed values exactly as before. The mark is saved as attribute "mark" on the row and the
   schedules in <MemberSchedules>; files without them read as before. A DXF schedule loader can fill the same grids later. */
partial class ProgramOfWorksForm {
 internal const string ScheduleMarkColumn="ScheduleMark";
 internal sealed class MemberSchedule {public string Kind;public DataGridView Schedule,Estimate;public int[] Targets;public int First=2;}
 readonly List<MemberSchedule> memberSchedules=new List<MemberSchedule>();
 internal DataGridView ColumnSchedule{get{return memberSchedules.Count>0?memberSchedules[0].Schedule:null;}}
 internal DataGridView FootingSchedule{get{return memberSchedules.Count>1?memberSchedules[1].Schedule:null;}}
 void InitializeMemberSchedules(){
  var column=extraGrids["StructuralColumn"].Grid;
  /* schedule columns 2.. map to these Column Estimate inputs */
  int[] columnTargets={3,4,6,7,8,9,10,11,12,16,17};
  var cs=Grid(new[]{"Mark","Description"}.Concat(columnTargets.Select(i=>column.Columns[i].HeaderText)).Concat(new[]{"Used by"}).ToArray(),2+columnTargets.Length);
  AddCombo(cs,9,cs.Columns[9].HeaderText,new[]{"Spacing (mm)","Number of ties","Spacing schedule"});cs.Columns[9].MinimumWidth=165;
  SetDefaults(cs,new Dictionary<int,string>{{2,"300"},{3,"300"},{4,"16"},{5,"8"},{8,"10"},{9,"Spacing (mm)"},{10,"150"}});
  ScheduleButton("Column Estimate","Manual column schedules",cs,"Enter each column type once (mark, section, main bars and ties). On Column Estimate choose the type in Schedule mark: the row then takes its width, depth, bars and ties from here, and a change here updates every row of that type. Height, quantity and end allowance stay on each row. Rows without a mark keep their own values.");
  memberSchedules.Add(new MemberSchedule{Kind="Column",Schedule=cs,Estimate=column,Targets=columnTargets});
  /* footings: L, B, D, cover, the four bar groups (diameter, spacing, bars) and the extra ends */
  int[] footingTargets=new[]{2,3,4,7}.Concat(Enumerable.Range(8,13)).ToArray();
  var fs=Grid(new[]{"Mark","Description"}.Concat(footingTargets.Select(i=>footings.Columns[i].HeaderText)).Concat(new[]{"Used by"}).ToArray(),2+footingTargets.Length);
  SetDefaults(fs,new Dictionary<int,string>{{2,"2"},{3,"2"},{4,"0.40"},{5,"75"},{6,"16"},{7,"150"},{9,"16"},{10,"150"},{18,"0"}});
  ScheduleButton("Footing Estimate","Manual footing schedules",fs,"Enter each footing type once (mark, L x B x D, cover and bars). On Footing Estimate choose the type in Schedule mark: the row then takes its size and bars from here, and a change here updates every row of that type. Quantity, excavation depth, side forms and materials stay on each row. For each bar group enter spacing or number of bars, as chosen in the footing settings. Rows without a mark keep their own values.");
  memberSchedules.Add(new MemberSchedule{Kind="Footing",Schedule=fs,Estimate=footings,Targets=footingTargets});
  /* the schedules are edited through the manager window (Add / Edit / Delete), not typed in place */
  foreach(var g in new[]{cs,fs}){g.AllowUserToAddRows=false;g.AllowUserToDeleteRows=false;g.ReadOnly=true;}
  foreach(var m in memberSchedules){var s=m;var g=s.Estimate;var mark=new DataGridViewComboBoxColumn{Name=ScheduleMarkColumn,HeaderText="Schedule mark",Width=120,FlatStyle=FlatStyle.Flat,DisplayStyle=DataGridViewComboBoxDisplayStyle.DropDownButton,ToolTipText="Choose a type from the "+s.Kind+" Schedule; blank = this row's own values.",SortMode=DataGridViewColumnSortMode.NotSortable};mark.DefaultCellStyle.BackColor=ModernUi.Input;mark.Items.Add("");int at=g.Columns.Add(mark);g.Columns[at].DisplayIndex=Math.Min(2,g.Columns.Count-1);
   g.CurrentCellDirtyStateChanged+=(x,e)=>{if(g.IsCurrentCellDirty&&g.CurrentCell!=null&&g.CurrentCell.OwningColumn.Name==ScheduleMarkColumn)g.CommitEdit(DataGridViewDataErrorContexts.Commit);};
   g.CellValueChanged+=(x,e)=>{if(!filling&&e.RowIndex>=0&&e.ColumnIndex>=0&&g.Columns[e.ColumnIndex].Name==ScheduleMarkColumn)Recalculate();};
   s.Schedule.CellValueChanged+=(x,e)=>{if(!filling&&e.RowIndex>=0&&e.ColumnIndex==0)RefreshScheduleMarks(s);};}
 }
 /* a command-bar button like the beam and slab "manual schedule" buttons (owner: "hindi siya tab button siya kagaya sa beam at
    slab"); the schedule table opens in its own window and stays in the project when the window closes */
 void ScheduleButton(string page,string caption,DataGridView schedule,string guide){var tab=tabs.TabPages.Cast<TabPage>().First(p=>p.Text==page);var bar=tab.Controls.OfType<FlowLayoutPanel>().First();
  Button(bar,caption,()=>ShowSchedule(caption,schedule,guide));bar.Visible=true;}
 /* the manager (owner: "tapos may manage para pwede mag edit and delete"): a read-only list of the types with Add, Edit and Delete;
    Add / Edit open an entry form. Renaming a mark renames it on the estimate rows; deleting a type unlinks its rows, which keep
    the values they last took from it. */
 void ShowSchedule(string caption,DataGridView schedule,string guide){var s=memberSchedules.First(m=>m.Schedule==schedule);s.Estimate.EndEdit();ApplyMemberSchedules();
  using(var form=new Form{Text=caption,Width=1180,Height=560,MinimumSize=new Size(760,380),StartPosition=FormStartPosition.CenterParent,Font=Font}){AppBrand.Apply(form);
   var heading=new WorksheetHeading{Text=caption,Subtitle="One row per type  ·  choose the mark in Schedule mark on "+s.Kind+" Estimate"};heading.SetGuide(guide);
   var list=new ListView{Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true,MultiSelect=false,HideSelection=false,GridLines=true};
   for(int c=0;c<schedule.Columns.Count;c++){string h=schedule.Columns[c].HeaderText;list.Columns.Add(h,Math.Max(c==1||c==schedule.Columns.Count-1?200:90,TextRenderer.MeasureText(h,list.Font).Width+18));}/* full captions */
   Action fill=()=>{ApplyMemberSchedules();string keep=list.SelectedItems.Count>0?list.SelectedItems[0].Text:null;list.BeginUpdate();list.Items.Clear();foreach(DataGridViewRow r in schedule.Rows){if(r.IsNewRow)continue;var item=new ListViewItem(Convert.ToString(r.Cells[0].Value)){Tag=r};for(int c=1;c<schedule.Columns.Count;c++)item.SubItems.Add(Convert.ToString(r.Cells[c].Value));list.Items.Add(item);if(item.Text==keep)item.Selected=true;}list.EndUpdate();};
   var tools=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(10,4,10,4)};
   Func<DataGridViewRow> selected=()=>list.SelectedItems.Count>0?(DataGridViewRow)list.SelectedItems[0].Tag:null;
   Action<DataGridViewRow> edit=row=>{if(EditScheduleType(form,s,row))fill();};
   var add=new Button{Text="Add new schedule",AutoSize=true,Height=30};add.Click+=(x,e)=>edit(null);
   var change=new Button{Text="Edit selected",AutoSize=true,Height=30};change.Click+=(x,e)=>{var r=selected();if(r==null){MessageBox.Show(form,"Select a type to edit.",caption);return;}edit(r);};
   var remove=new Button{Text="Delete selected",AutoSize=true,Height=30};remove.Click+=(x,e)=>{var r=selected();if(r==null){MessageBox.Show(form,"Select a type to delete.",caption);return;}DeleteScheduleType(form,s,r);fill();};
   tools.Controls.AddRange(new Control[]{add,change,remove});list.DoubleClick+=(x,e)=>{var r=selected();if(r!=null)edit(r);};
   var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,AutoSize=true,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(10)};var close=new Button{Text="Close",AutoSize=true,DialogResult=DialogResult.OK};bottom.Controls.Add(close);form.CancelButton=close;
   var host=new Panel{Dock=DockStyle.Fill,Padding=new Padding(12,6,12,6)};host.Controls.Add(list);form.Controls.Add(host);form.Controls.Add(tools);form.Controls.Add(bottom);form.Controls.Add(heading);
   fill();form.ShowDialog(this);}
  RefreshScheduleMarks(s);Recalculate();}
 void RefreshScheduleView(MemberSchedule s){RefreshScheduleMarks(s);ApplyMemberSchedules();}
 /* entry form for one type; null row = new type */
 bool EditScheduleType(IWin32Window owner,MemberSchedule s,DataGridViewRow row){var g=s.Schedule;int count=s.First+s.Targets.Length;
  using(var form=new Form{Text=(row==null?"Add ":"Edit ")+s.Kind.ToLowerInvariant()+" type",Width=760,Height=600,MinimumSize=new Size(560,400),StartPosition=FormStartPosition.CenterParent,Font=Font}){AppBrand.Apply(form);
   var flow=new TableLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,ColumnCount=2,Padding=new Padding(14)};flow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,55));flow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,45));
   var inputs=new Control[count];
   for(int c=0;c<count;c++){var column=g.Columns[c];string value=row!=null?Convert.ToString(row.Cells[c].Value):Convert.ToString(column.DefaultCellStyle.NullValue??"");
    Control input;var combo=column as DataGridViewComboBoxColumn;if(combo!=null){var box=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=260};foreach(var item in combo.Items)box.Items.Add(item);box.SelectedItem=value;if(box.SelectedIndex<0&&box.Items.Count>0)box.SelectedIndex=0;input=box;}
    else input=new TextBox{Text=value,Width=260};
    inputs[c]=input;flow.Controls.Add(new Label{Text=column.HeaderText,AutoSize=true,MaximumSize=new Size(380,0),Padding=new Padding(0,6,0,6)});flow.Controls.Add(input);}
   if(row==null)foreach(var pair in ScheduleDefaults(s))if(inputs[pair.Key] is TextBox)inputs[pair.Key].Text=pair.Value;else{var box=(ComboBox)inputs[pair.Key];box.SelectedItem=pair.Value;}
   var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,AutoSize=true,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(10)};var ok=new Button{Text="Save",AutoSize=true};var cancel=new Button{Text="Cancel",AutoSize=true,DialogResult=DialogResult.Cancel};bottom.Controls.Add(cancel);bottom.Controls.Add(ok);form.CancelButton=cancel;
   string oldMark=row!=null?MarkKey(row.Cells[0].Value):"";
   ok.Click+=(x,e)=>{string mark=inputs[0].Text.Trim();if(mark==""){MessageBox.Show(form,"Enter the mark (for example "+(s.Kind=="Column"?"C1":"F1")+").",form.Text);return;}
    foreach(DataGridViewRow other in g.Rows)if(other!=row&&!other.IsNewRow&&string.Equals(MarkKey(other.Cells[0].Value),mark,StringComparison.OrdinalIgnoreCase)){MessageBox.Show(form,"Mark "+mark+" is already on the schedule.",form.Text);return;}
    for(int c=2;c<count;c++){string t=inputs[c].Text.Trim();double v;if(inputs[c] is TextBox&&t!=""&&!t.StartsWith("=")&&!double.TryParse(t,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out v)){MessageBox.Show(form,g.Columns[c].HeaderText+": enter a number.",form.Text);return;}
     if(t.StartsWith("=")){string err;if(!GridFormulas.TryEvaluate(t.Substring(1),out v,out err)){MessageBox.Show(form,g.Columns[c].HeaderText+": "+err,form.Text);return;}}}
    bool before=filling;filling=true;try{var target=row;if(target==null){int n=g.Rows.Add();target=g.Rows[n];}
     for(int c=0;c<count;c++){string t=inputs[c].Text.Trim();if(t.StartsWith("=")){double v;string err;GridFormulas.TryEvaluate(t.Substring(1),out v,out err);target.Cells[c].Value=GridFormulas.Format(v);GridFormulas.Set(target.Cells[c],t);}else{target.Cells[c].Value=t;GridFormulas.Set(target.Cells[c],null);}}
     /* a renamed mark follows on the estimate rows */
     if(oldMark!=""&&!string.Equals(oldMark,mark,StringComparison.Ordinal)){var markColumn=(DataGridViewComboBoxColumn)s.Estimate.Columns[ScheduleMarkColumn];if(!markColumn.Items.Contains(mark))markColumn.Items.Add(mark);foreach(DataGridViewRow r in s.Estimate.Rows)if(!r.IsNewRow&&string.Equals(MarkKey(r.Cells[markColumn.Index].Value),oldMark,StringComparison.OrdinalIgnoreCase))r.Cells[markColumn.Index].Value=mark;}}
    finally{filling=before;}form.DialogResult=DialogResult.OK;};
   form.Controls.Add(flow);form.Controls.Add(bottom);return form.ShowDialog(owner)==DialogResult.OK;}}
 static Dictionary<int,string> ScheduleDefaults(MemberSchedule s){return s.Kind=="Column"?new Dictionary<int,string>{{2,"300"},{3,"300"},{4,"16"},{5,"8"},{8,"10"},{9,"Spacing (mm)"},{10,"150"}}:new Dictionary<int,string>{{2,"2"},{3,"2"},{4,"0.40"},{5,"75"},{6,"16"},{7,"150"},{9,"16"},{10,"150"},{18,"0"}};}
 void DeleteScheduleType(IWin32Window owner,MemberSchedule s,DataGridViewRow row){string mark=MarkKey(row.Cells[0].Value);var markColumn=s.Estimate.Columns[ScheduleMarkColumn];
  var users=s.Estimate.Rows.Cast<DataGridViewRow>().Where(r=>!r.IsNewRow&&mark!=""&&string.Equals(MarkKey(r.Cells[markColumn.Index].Value),mark,StringComparison.OrdinalIgnoreCase)).ToList();
  string text="Delete "+s.Kind.ToLowerInvariant()+" type "+(mark==""?"(no mark)":mark)+"?"+(users.Count>0?"\n\n"+users.Count+" "+s.Kind+" Estimate row"+(users.Count==1?" uses":"s use")+" it. They keep the values they took from it and become ordinary rows (Schedule mark cleared).":"");
  if(MessageBox.Show(owner,text,"Delete schedule type",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
  bool before=filling;filling=true;try{foreach(var r in users)r.Cells[markColumn.Index].Value="";s.Schedule.Rows.Remove(row);}finally{filling=before;}}
 /* the status column of a sheet stays the last one before the appended Schedule mark */
 internal static int StatusIndex(DataGridView g){int i=g.Columns.Count-1;if(i>0&&g.Columns[i].Name==ScheduleMarkColumn)i--;return i;}
 static string MarkKey(object v){return Convert.ToString(v).Trim();}
 void RefreshScheduleMarks(MemberSchedule s){var column=s.Estimate.Columns[ScheduleMarkColumn] as DataGridViewComboBoxColumn;if(column==null)return;
  var wanted=new List<string>{""};foreach(DataGridViewRow r in s.Schedule.Rows)if(!r.IsNewRow){string k=MarkKey(r.Cells[0].Value);if(k!=""&&!wanted.Contains(k,StringComparer.OrdinalIgnoreCase))wanted.Add(k);}
  /* a mark still used by a row stays selectable, so an older or edited project never loses it */
  foreach(DataGridViewRow r in s.Estimate.Rows)if(!r.IsNewRow){string k=MarkKey(r.Cells[column.Index].Value);if(k!=""&&!wanted.Contains(k))wanted.Add(k);}
  /* add and remove single items: clearing the list would blank the cells that use it */
  foreach(var k in wanted)if(!column.Items.Cast<object>().Any(i=>MarkKey(i)==k))column.Items.Add(k);foreach(var i in column.Items.Cast<object>().ToList())if(!wanted.Contains(MarkKey(i)))column.Items.Remove(i);}
 /* copies the schedule values into every estimate row with a mark; called at the start of every recalculation */
 void ApplyMemberSchedules(){if(memberSchedules.Count==0)return;bool before=filling;filling=true;GridEditHistory.Quiet++;try{foreach(var s in memberSchedules){RefreshScheduleMarks(s);
   var types=new Dictionary<string,DataGridViewRow>(StringComparer.OrdinalIgnoreCase);var dup=new HashSet<string>(StringComparer.OrdinalIgnoreCase);foreach(DataGridViewRow r in s.Schedule.Rows){if(r.IsNewRow)continue;string k=MarkKey(r.Cells[0].Value);r.Cells[0].ErrorText="";if(k=="")continue;if(types.ContainsKey(k)){dup.Add(k);r.Cells[0].ErrorText="Duplicate mark "+k;}else types[k]=r;}
   var used=new Dictionary<string,List<string>>(StringComparer.OrdinalIgnoreCase);int markIndex=s.Estimate.Columns[ScheduleMarkColumn].Index;
   foreach(DataGridViewRow row in s.Estimate.Rows){if(row.IsNewRow||IsExcludedRow(row))continue;var markCell=row.Cells[markIndex];string k=MarkKey(markCell.Value);markCell.ErrorText="";DataGridViewRow type=null;
    if(k!=""&&!types.TryGetValue(k,out type))markCell.ErrorText="Mark "+k+" is not on the "+s.Kind+" Schedule; the row keeps its own values.";
    for(int i=0;i<s.Targets.Length;i++){var cell=row.Cells[s.Targets[i]];if(type!=null){object v=type.Cells[s.First+i].Value;string text=v is bool?v.ToString():Convert.ToString(v);if(cell is DataGridViewCheckBoxCell)cell.Value=text!=""&&bool.Parse(text);else cell.Value=text;GridFormulas.Set(cell,null);cell.ReadOnly=true;cell.Style.BackColor=DesktopAppearance.Palette.Soft;cell.ToolTipText="From "+s.Kind+" Schedule "+k;}
     else if(cell.ReadOnly&&cell.ToolTipText.StartsWith("From ")){cell.ReadOnly=false;cell.Style.BackColor=Color.Empty;cell.ToolTipText="";}}
    if(type!=null){List<string> list;if(!used.TryGetValue(k,out list))used[k]=list=new List<string>();list.Add(Convert.ToString(row.Cells[0].Value));}}
   int last=s.Schedule.Columns.Count-1;foreach(DataGridViewRow r in s.Schedule.Rows){if(r.IsNewRow)continue;List<string> list;string k=MarkKey(r.Cells[0].Value);r.Cells[last].Value=k!=""&&used.TryGetValue(k,out list)?list.Count+" row"+(list.Count==1?"":"s")+": "+string.Join(", ",list.Where(x=>x!="")):"";}}}
  finally{GridEditHistory.Quiet--;filling=before;}}
 static string RowMark(DataGridViewRow row){if(row==null||row.DataGridView==null||!row.DataGridView.Columns.Contains(ScheduleMarkColumn))return null;int i=row.DataGridView.Columns[ScheduleMarkColumn].Index;var x=row.HeaderCell.Tag as RowExclusion;object v=x!=null&&i<x.Values.Length?x.Values[i]:row.Cells[i].Value;return MarkKey(v);}
 static void WriteRowMark(DataGridViewRow row,XmlElement entry){string k=RowMark(row);if(!string.IsNullOrEmpty(k))entry.SetAttribute("mark",k);}
 static void ReadRowMark(DataGridViewRow row,XmlElement entry){if(row.DataGridView==null||!row.DataGridView.Columns.Contains(ScheduleMarkColumn))return;var column=(DataGridViewComboBoxColumn)row.DataGridView.Columns[ScheduleMarkColumn];string k=entry.GetAttribute("mark").Trim();if(k!=""&&!column.Items.Contains(k))column.Items.Add(k);row.Cells[column.Index].Value=k;}
 void WriteMemberSchedules(XmlElement root){if(memberSchedules.Count==0)return;var doc=root.OwnerDocument;var section=doc.CreateElement("MemberSchedules");section.SetAttribute("version","1");foreach(var s in memberSchedules){var table=doc.CreateElement("Schedule");table.SetAttribute("kind",s.Kind);foreach(DataGridViewRow r in s.Schedule.Rows){if(r.IsNewRow||MarkKey(r.Cells[0].Value)==""&&MarkKey(r.Cells[1].Value)=="")continue;var e=doc.CreateElement("Type");for(int c=0;c<s.First+s.Targets.Length;c++){object v=r.Cells[c].Value;e.SetAttribute("c"+c,v==null?"":Convert.ToString(v));string fx=GridFormulas.Formula(r.Cells[c]);if(fx!=null)e.SetAttribute("f"+c,fx);}table.AppendChild(e);}section.AppendChild(table);}if(section.HasChildNodes)root.AppendChild(section);}
 /* files saved before v1.50.12 have no <MemberSchedules>: the schedules start empty and every row keeps its own values */
 void ReadMemberSchedules(XmlElement root){bool before=filling;filling=true;try{foreach(var s in memberSchedules)s.Schedule.Rows.Clear();var section=root["MemberSchedules"];if(section==null)return;if(section.GetAttribute("version")!="1")throw new Exception("Unsupported column / footing schedule format.");
  foreach(XmlElement table in section.SelectNodes("Schedule")){var s=memberSchedules.FirstOrDefault(m=>m.Kind==table.GetAttribute("kind"));if(s==null)continue;foreach(XmlElement e in table.SelectNodes("Type")){if(s.Schedule.Rows.Count>10000)throw new Exception("Too many schedule types.");int n=s.Schedule.Rows.Add();for(int c=0;c<s.First+s.Targets.Length;c++){var cell=s.Schedule.Rows[n].Cells[c];string v=e.GetAttribute("c"+c);if(cell is DataGridViewComboBoxCell&&v!=""&&!((DataGridViewComboBoxCell)cell).Items.Contains(v))continue;cell.Value=v;GridFormulas.Set(cell,e.HasAttribute("f"+c)?e.GetAttribute("f"+c):null);}}}
  foreach(var s in memberSchedules)RefreshScheduleMarks(s);}finally{filling=before;}}
}
}
