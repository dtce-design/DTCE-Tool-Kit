using System;using System.Collections.Generic;using System.Drawing;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Finish Schedule: Finish ID, Description and the BOQ pay item it belongs to (owner, 18 November 2026: "sa Start mag
// assign lang ng Finishing ID tapos description tapos anong group siya sa BOQ"). Clicking the third column opens the
// BACKUP pay-item picker; the chosen item's description becomes the group name and the group's quantity is mapped to
// that pay item at once (no separate mapping step). Typing a name is still allowed before a BACKUP workbook is linked.
partial class ProgramOfWorksForm {
 internal const string FinishGroupSection="Finish material breakdown";
 static string FinishGroupKey(string group){return FinishGroupSection+"|"+group+"|"+BackupMapping.Unit("m2");}
 void InstallFinishPayItemPicker(){if(finishSchedule==null)return;var g=finishSchedule;
  g.Columns[2].HeaderText="BOQ group / item no. (click to choose)";
  g.Columns[2].ToolTipText="Click to choose the BOQ pay item for this finish; its measured areas go straight to that item. Before a BACKUP workbook is linked you can type a group name.";
  g.CellClick+=(s,e)=>{if(e.RowIndex<0||e.ColumnIndex!=2||g.Rows[e.RowIndex].IsNewRow)return;ChooseFinishPayItem(g.Rows[e.RowIndex]);};
  /* Item no. shown with the group name (owner: "ilagay din yung item no."); the stored value stays the group name. */
  g.CellFormatting+=(s,e)=>{if(e.RowIndex<0||e.ColumnIndex!=2||e.Value==null)return;var std=FinishStandard.Find(Convert.ToString(e.Value));if(std!=null&&!(g.IsCurrentCellInEditMode&&g.CurrentCell!=null&&g.CurrentCell.RowIndex==e.RowIndex&&g.CurrentCell.ColumnIndex==2)){e.Value=std.Caption;e.FormattingApplied=true;}};
  g.CellPainting+=(s,e)=>{if(e.RowIndex<0||e.ColumnIndex!=2)return;e.Paint(e.ClipBounds,DataGridViewPaintParts.All);int w=22;var box=new Rectangle(e.CellBounds.Right-w-2,e.CellBounds.Top+2,w,e.CellBounds.Height-5);
   using(var fill=new SolidBrush(Color.FromArgb(236,239,243)))e.Graphics.FillRectangle(fill,box);using(var pen=new Pen(Color.FromArgb(190,196,204)))e.Graphics.DrawRectangle(pen,box);int cx=box.Left+box.Width/2,cy=box.Top+box.Height/2;
   using(var brush=new SolidBrush(Color.FromArgb(60,70,82)))e.Graphics.FillPolygon(brush,new[]{new Point(cx-4,cy-2),new Point(cx+4,cy-2),new Point(cx,cy+3)});e.Handled=true;};}
 void ChooseFinishPayItem(DataGridViewRow row){try{if(!finishSchedule.EndEdit())return;
   /* The owner's standard finish list with item numbers is always offered; with a BACKUP workbook linked, its group goes
      to the matching pay item automatically (RouteRequiredQuantities). */
   var choices=FinishStandard.All.Select((st,i)=>new BackupTarget{Row=0,Code=st.Code,Description=st.Name,Unit="sq.m.",SlotId="std:"+i}).ToList();
   /* plus every Part I item no. of the catalogue and of the linked BACKUP (owner, 20 November 2026) */
   int n=0;foreach(var t in FinishStandard.PartIItems(backupBill.Select(b=>b.Target)))if(!choices.Any(c=>c.Code==t.Code&&FinishTypeRegistry.Key(c.Description)==FinishTypeRegistry.Key(t.Description)))choices.Add(new BackupTarget{Row=t.Row,Code=t.Code,Description=t.Description,Unit=t.Unit,SlotId="part1:"+(n++)});
   choices=choices.OrderBy(c=>{var m=System.Text.RegularExpressions.Regex.Match(c.Code??"",@"^\d+");return m.Success?int.Parse(m.Value):99999;}).ThenBy(c=>c.Code,StringComparer.OrdinalIgnoreCase).ThenBy(c=>c.Description).ToList();
   var current=FinishStandard.Find(Cell(row,2));var selected=current==null?null:choices.FirstOrDefault(t=>t.Description==current.Name);
   using(var picker=new BackupTargetPicker((Cell(row,0)+" "+Cell(row,1)).Trim(),choices,selected)){if(picker.ShowDialog(this)!=DialogResult.OK||picker.Selected==null)return;
    row.Cells[2].Value=picker.Selected.Description;Recalculate();}}
  catch(Exception ex){MessageBox.Show(this,ex.Message,"Finish pay item");}}
}
}
namespace BeamScheduleFormatter {
// Start, lower table (owner, 18 November 2026: "sa Start i-consolidate niya lahat per group"): one line per BOQ group with
// the Finish IDs in it, the measured entries (wall / ceiling / floor), the total area and the BOQ pay item it goes to.
// Measured areas without a Finish ID are listed last so they are not missed.
partial class ProgramOfWorksForm {
 DataGridView finishGroupTotals;
 DataGridView FinishGroupTotalsGrid(){var g=new DataGridView{Name="FinishGroupTotals",Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,BackgroundColor=Color.White,AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.AllCells};
  foreach(var h in new[]{"BOQ group / item no.","Finish IDs","Measured entries","Total m2"})g.Columns.Add(h,h);
  g.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;g.Columns[0].FillWeight=44;g.Columns[1].FillWeight=14;g.Columns[2].FillWeight=28;g.Columns[3].FillWeight=14;g.BorderStyle=BorderStyle.None;g.ScrollBars=ScrollBars.Vertical;g.Columns[2].DefaultCellStyle.WrapMode=DataGridViewTriState.True;g.Columns[0].DefaultCellStyle.WrapMode=DataGridViewTriState.True;g.Columns[3].DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;
  foreach(DataGridViewColumn c in g.Columns)c.SortMode=DataGridViewColumnSortMode.NotSortable;
  /* BOQ style (owner, 20 November 2026: "parang boq style na lang na may drop down arrow ilagay doon yung mga kasama sa group"):
     a group row with an arrow; clicking it shows the measured entries of the group below it */
  g.RowHeadersVisible=true;g.RowHeadersWidth=34;g.RowHeadersWidthSizeMode=DataGridViewRowHeadersWidthSizeMode.DisableResizing;
  g.CellPainting+=(s,e)=>{if(e.ColumnIndex!=-1||e.RowIndex<0)return;var key=g.Rows[e.RowIndex].Tag as string;if(key==null||!key.StartsWith("group:"))return;e.PaintBackground(e.CellBounds,false);bool open=finishGroupOpen.Contains(key);var c=new Point(e.CellBounds.Left+e.CellBounds.Width/2,e.CellBounds.Top+e.CellBounds.Height/2);
   var pts=open?new[]{new Point(c.X-5,c.Y-2),new Point(c.X+5,c.Y-2),new Point(c.X,c.Y+4)}:new[]{new Point(c.X-2,c.Y-5),new Point(c.X+4,c.Y),new Point(c.X-2,c.Y+5)};e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using(var b=new SolidBrush(PowTheme.Teal))e.Graphics.FillPolygon(b,pts);e.Handled=true;};
  Action<int> toggle=r=>{if(r<0)return;var key=g.Rows[r].Tag as string;if(key==null||!key.StartsWith("group:"))return;if(!finishGroupOpen.Remove(key))finishGroupOpen.Add(key);RefreshFinishGroupTotals();};
  g.RowHeaderMouseClick+=(s,e)=>toggle(e.RowIndex);g.CellClick+=(s,e)=>{if(e.ColumnIndex==0)toggle(e.RowIndex);};g.CellDoubleClick+=(s,e)=>{if(e.ColumnIndex>0)toggle(e.RowIndex);};
  finishGroupTotals=g;return g;}
 readonly HashSet<string> finishGroupOpen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 void RefreshFinishGroupTotals(){var g=finishGroupTotals;if(g==null||g.IsDisposed)return;g.Rows.Clear();var chart=new List<KeyValuePair<string,double>>();
  Func<FinishItem,double> area=f=>{try{return f.Total(FindWall(f.Wall),wallOpenings,finishItems);}catch(Exception){return 0;}};
  foreach(var total in FinishTypeRegistry.Totals(finishItems,area).OrderBy(t=>t.Name)){var items=finishItems.Where(f=>FinishTypeRegistry.Key(f.MaterialName)==total.Key).ToList();
   string ids=string.Join(", ",items.Select(f=>f.Code).Where(c=>c!="").Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c=>c));
   string entries=string.Join("; ",items.GroupBy(f=>f.Category).Select(c=>c.Key+": "+string.Join(", ",c.Select(f=>f.Id))));
   string slot;var std=FinishStandard.Find(total.Name);string caption=std!=null?std.Caption:total.Name;if(std==null){var bill=backupBillRoutes.TryGetValue(FinishGroupKey(total.Name),out slot)?backupBill.FirstOrDefault(b=>b.Slot==slot):null;caption=bill!=null?bill.Code+" | "+total.Name:total.Name+"  (not mapped yet)";}
   string gkey="group:"+total.Key;bool open=finishGroupOpen.Contains(gkey);int r=g.Rows.Add(caption,ids,items.Count+" entr"+(items.Count==1?"y":"ies")+(open?"":"  -  "+entries),total.Area.ToString("N2"));g.Rows[r].Tag=gkey;g.Rows[r].DefaultCellStyle.Font=new Font(g.Font,FontStyle.Bold);g.Rows[r].DefaultCellStyle.BackColor=DesktopAppearance.Palette.Soft;chart.Add(new KeyValuePair<string,double>(caption,total.Area));if(caption.EndsWith("(not mapped yet)"))g.Rows[r].Cells[0].Style.ForeColor=Color.FromArgb(176,96,0);
   if(open)foreach(var f in items.OrderBy(x=>x.Category).ThenBy(x=>x.Id)){int c=g.Rows.Add("      "+f.Id+"  -  "+f.Category,f.Code,f.Description??"",area(f).ToString("N2"));g.Rows[c].Tag="item";g.Rows[c].DefaultCellStyle.ForeColor=PowTheme.Muted;}}
  var loose=finishItems.Where(f=>FinishTypeRegistry.Key(f.MaterialName)=="").ToList();
  if(loose.Count>0){int r=g.Rows.Add("No Finish ID yet - choose one on the sheet","",string.Join("; ",loose.GroupBy(f=>f.Category).Select(c=>c.Key+": "+string.Join(", ",c.Select(f=>f.Id)))),loose.Sum(area).ToString("N2"));g.Rows[r].DefaultCellStyle.ForeColor=Color.FromArgb(176,96,0);}
  /* an adjustment larger than the measured area of its group is reported; the group counts as 0 */
  foreach(var grp in finishItems.Where(f=>FinishTypeRegistry.Key(f.MaterialName)!="").GroupBy(f=>FinishTypeRegistry.Key(f.MaterialName))){double raw=grp.Sum(area);if(raw< -1e-9)errors.Add("Finish group "+grp.First().MaterialName+": adjustment exceeds the measured area by "+(-raw).ToString("N2")+" m2; counted as 0.");}
  if(finishGroupChart!=null&&!finishGroupChart.IsDisposed)finishGroupChart.SetData(chart);g.ClearSelection();}
}
}
