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
  foreach(var h in new[]{"BOQ group / pay item","Finish IDs","Measured entries","Total area m2","BOQ item"})g.Columns.Add(h,h);
  g.Columns[0].Width=250;g.Columns[1].Width=90;g.Columns[2].Width=220;g.Columns[3].Width=90;g.Columns[4].Width=90;g.Columns[2].DefaultCellStyle.WrapMode=DataGridViewTriState.True;g.Columns[0].DefaultCellStyle.WrapMode=DataGridViewTriState.True;g.Columns[3].DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;
  foreach(DataGridViewColumn c in g.Columns)c.SortMode=DataGridViewColumnSortMode.NotSortable;finishGroupTotals=g;return g;}
 void RefreshFinishGroupTotals(){var g=finishGroupTotals;if(g==null||g.IsDisposed)return;g.Rows.Clear();
  Func<FinishItem,double> area=f=>{try{return f.Total(FindWall(f.Wall),wallOpenings,finishItems);}catch(Exception){return 0;}};
  foreach(var total in FinishTypeRegistry.Totals(finishItems,area).OrderBy(t=>t.Name)){var items=finishItems.Where(f=>FinishTypeRegistry.Key(f.MaterialName)==total.Key).ToList();
   string ids=string.Join(", ",items.Select(f=>f.Code).Where(c=>c!="").Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c=>c));
   string entries=string.Join("; ",items.GroupBy(f=>f.Category).Select(c=>c.Key+": "+string.Join(", ",c.Select(f=>f.Id))));
   string slot;string item="Not mapped yet";var std=FinishStandard.Find(total.Name);if(std!=null)item=std.Code;else if(backupBillRoutes.TryGetValue(FinishGroupKey(total.Name),out slot)){var bill=backupBill.FirstOrDefault(b=>b.Slot==slot);if(bill!=null)item=bill.Code;}
   int r=g.Rows.Add(std!=null?std.Caption:total.Name,ids,entries,total.Area.ToString("N2"),item);if(item=="Not mapped yet")g.Rows[r].Cells[4].Style.ForeColor=Color.FromArgb(176,96,0);}
  var loose=finishItems.Where(f=>FinishTypeRegistry.Key(f.MaterialName)=="").ToList();
  if(loose.Count>0){int r=g.Rows.Add("No Finish ID / group yet","",string.Join("; ",loose.GroupBy(f=>f.Category).Select(c=>c.Key+": "+string.Join(", ",c.Select(f=>f.Id)))),loose.Sum(area).ToString("N2"),"Choose a Finish ID on the sheet");g.Rows[r].DefaultCellStyle.ForeColor=Color.FromArgb(176,96,0);}}
}
}
