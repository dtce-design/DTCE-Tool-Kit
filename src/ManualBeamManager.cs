using System;using System.Collections.Generic;using System.Drawing;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* Manual beam schedules are managed like the slab schedules (owner, 20 November 2026: "ayusin din yung sa beam kagaya ng slab sa
   manage schedule"): one window lists the manual beam marks with Add, Edit and Delete. Editing keeps the takeoff rows; a renamed
   mark follows on them. Deleting removes the takeoff rows that use the mark (asked first, as for slabs). The schedules are still
   stored as the "Manual beam schedules" source, so estimates saved before keep opening unchanged. */
partial class EstimatorForm {
 ScheduleSource ManualSource(){return sources.FirstOrDefault(x=>x.Name==ManualBeamSchedule.FileName);}
 void ManageManualSchedules(){if(!Allowed())return;beams.EndEdit();
  using(var form=new Form{Text="Manual beam schedules",Width=1100,Height=560,MinimumSize=new Size(760,420),StartPosition=FormStartPosition.CenterParent,Font=new Font("Segoe UI",10)}){AppBrand.Apply(form);
   var list=new ListView{Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true,MultiSelect=false,HideSelection=false,GridLines=true};
   foreach(var h in new[]{"Beam mark","B x D mm","Bottom left / mid / right","Top left / mid / right","SFR","Stirrups","Takeoff rows using schedule"})list.Columns.Add(h,Math.Max(h=="Stirrups"?300:h.Contains("/")?210:110,TextRenderer.MeasureText(h,list.Font).Width+18));
   Action fill=()=>{string keep=list.SelectedItems.Count>0?list.SelectedItems[0].Text:null;list.BeginUpdate();list.Items.Clear();var source=ManualSource();if(source!=null)foreach(var r in source.Catalog.Source){Func<int,string> j=n=>string.Join(" + ",r.Cells[n]);int uses=beams.Rows.Cast<DataGridViewRow>().Count(x=>!x.IsNewRow&&string.Equals(Cell(x,1),r.Mark,StringComparison.OrdinalIgnoreCase));
     var item=new ListViewItem(r.Mark){Tag=r.Mark};item.SubItems.Add(r.Cell(0)+" x "+r.Cell(1));item.SubItems.Add(j(2)+" / "+j(3)+" / "+j(4));item.SubItems.Add(j(5)+" / "+j(6)+" / "+j(7));item.SubItems.Add(j(11));item.SubItems.Add(r.Leg+" "+r.Diameter+"  "+r.RevisedSpacing);item.SubItems.Add(uses.ToString());list.Items.Add(item);if(item.Text==keep)item.Selected=true;}
    list.EndUpdate();};
   var tools=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(10,6,10,4)};
   Func<BeamRow> selected=()=>{var source=ManualSource();if(source==null||list.SelectedItems.Count==0)return null;string mark=(string)list.SelectedItems[0].Tag;return source.Catalog.Source.FirstOrDefault(r=>r.Mark==mark);};
   Action edit=()=>{var old=selected();if(old==null){MessageBox.Show(form,"Select a manual beam schedule to edit.",form.Text);return;}var row=ManualBeamSchedule.Choose(form,old);if(row==null)return;try{ReplaceManual(old.Mark,row);}catch(Exception x){MessageBox.Show(form,x.Message,form.Text);}fill();};
   var add=new Button{Text="Add new schedule",AutoSize=true,Height=30};add.Click+=(x,e)=>{var row=ManualBeamSchedule.Choose(form);if(row==null)return;try{AddManual(row);}catch(Exception ex){MessageBox.Show(form,ex.Message,form.Text);}fill();};
   var change=new Button{Text="Edit selected",AutoSize=true,Height=30};change.Click+=(x,e)=>edit();
   var remove=new Button{Text="Delete selected",AutoSize=true,Height=30};remove.Click+=(x,e)=>{var old=selected();if(old==null){MessageBox.Show(form,"Select a manual beam schedule to delete.",form.Text);return;}int uses=beams.Rows.Cast<DataGridViewRow>().Count(r=>!r.IsNewRow&&string.Equals(Cell(r,1),old.Mark,StringComparison.OrdinalIgnoreCase));
    if(MessageBox.Show(form,"Delete manual schedule "+old.Mark+" and its "+uses+" takeoff row(s)?\nSave the estimate first if you need a backup.","Delete manual beam schedule",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;try{DeleteManual(old.Mark);}catch(Exception ex){MessageBox.Show(form,ex.Message,form.Text);}fill();};
   tools.Controls.AddRange(new Control[]{add,change,remove});list.DoubleClick+=(x,e)=>edit();
   var note=new Label{Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(12,4,12,4),ForeColor=PowTheme.Muted,Text="For beams missing from the DXF. Choose the mark on the takeoff rows; editing a schedule updates every row that uses it."};
   var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,AutoSize=true,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(10)};var close=new Button{Text="Close",AutoSize=true,DialogResult=DialogResult.OK};bottom.Controls.Add(close);form.CancelButton=close;
   var host=new Panel{Dock=DockStyle.Fill,Padding=new Padding(12,6,12,6)};host.Controls.Add(list);form.Controls.Add(host);form.Controls.Add(tools);form.Controls.Add(note);form.Controls.Add(bottom);
   fill();form.ShowDialog(this);}}
 /* writes the manual source back after an edit or delete; an empty set removes the source */
 void StoreManual(ScheduleSource source,List<BeamRow> rows){if(rows.Count==0){RemoveSourceAt(sources.IndexOf(source));return;}var a=new Analysis();a.Source.AddRange(rows);string raw=ManualBeamSchedule.Serialize(a);source.Raw=raw;source.Catalog=ManualBeamSchedule.Read(raw,source.Name);source.Rows=source.Catalog.Source.ToDictionary(x=>x.Mark,StringComparer.OrdinalIgnoreCase);sourceAttachments.CaptureText(source.Name,"Beam DXF",raw);if(sources.Count>0&&sources[0]==source){catalog=source.Catalog;sourceText=raw;}}
 internal void ReplaceManual(string oldMark,BeamRow row){ManualBeamSchedule.Validate(row);var source=ManualSource();if(source==null||!source.Rows.ContainsKey(oldMark))throw new Exception("Manual beam schedule "+oldMark+" was not found.");
  bool renamed=!string.Equals(oldMark,row.Mark,StringComparison.OrdinalIgnoreCase);if(renamed&&schedules.ContainsKey(row.Mark))throw new Exception("Beam mark already exists: "+row.Mark);
  var rows=source.Catalog.Source.Select(r=>string.Equals(r.Mark,oldMark,StringComparison.OrdinalIgnoreCase)?row:r).ToList();
  filling=true;try{StoreManual(source,rows);schedules.Remove(oldMark);schedules[row.Mark]=row;var marks=(DataGridViewComboBoxColumn)beams.Columns[1];if(!marks.Items.Contains(row.Mark))marks.Items.Add(row.Mark);
   foreach(DataGridViewRow r in beams.Rows)if(!r.IsNewRow&&string.Equals(Cell(r,1),oldMark,StringComparison.OrdinalIgnoreCase))r.Cells[1].Value=row.Mark;if(renamed)marks.Items.Remove(oldMark);foreach(DataGridViewRow r in beams.Rows)if(!r.IsNewRow)ConfigureMarkCell(r);}
  finally{filling=false;}UpdateLoadedDxf();Recalculate();}
 internal void DeleteManual(string mark){var source=ManualSource();if(source==null||!source.Rows.ContainsKey(mark))throw new Exception("Manual beam schedule "+mark+" was not found.");var rows=source.Catalog.Source.Where(r=>!string.Equals(r.Mark,mark,StringComparison.OrdinalIgnoreCase)).ToList();
  if(rows.Count==0){RemoveSourceAt(sources.IndexOf(source));return;}
  filling=true;try{foreach(var r in beams.Rows.Cast<DataGridViewRow>().Where(r=>!r.IsNewRow&&string.Equals(Cell(r,1),mark,StringComparison.OrdinalIgnoreCase)).ToList())beams.Rows.Remove(r);StoreManual(source,rows);schedules.Remove(mark);((DataGridViewComboBoxColumn)beams.Columns[1]).Items.Remove(mark);foreach(DataGridViewRow r in beams.Rows)if(!r.IsNewRow)ConfigureMarkCell(r);}
  finally{filling=false;}UpdateLoadedDxf();Recalculate();}
}
}
