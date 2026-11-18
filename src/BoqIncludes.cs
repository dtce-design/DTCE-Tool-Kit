using System;using System.Collections.Generic;using System.Drawing;using System.Globalization;using System.Linq;using System.Windows.Forms;using System.Xml;
namespace BeamScheduleFormatter {
// "Include" beside UNIT for every mapped quantity in Bill of Quantities (owner, 17 November 2026).
// - Checked: the mapped quantity counts in its pay item's total. Unchecked: it stays mapped (listed under the item and
//   saved) but is not counted.
// - Default: checked for every mapped / linked quantity (owner); unchecking subtracts it from the total.
// - An LS item without a manual quantity becomes 1 LS when at least one of its mapped quantities is included; a manual
//   LS quantity entered in Bill of Quantities always takes precedence. Unchecking all of them again writes 0.
partial class ProgramOfWorksForm {
 /* Owner (18 November 2026, "mas maayos na formatting, nakakahilo yung sub items"): three clear levels - pay item (bold),
    mapped quantity (semibold on a light band), measurement source (smaller, muted, "›"). */
 static Font boqHeadFont,boqDetailFont,boqItemFont,boqExcludedFont;
 Font BoqExcludedFont{get{return boqExcludedFont??(boqExcludedFont=new Font(boqGrid.Font,FontStyle.Bold|FontStyle.Strikeout));}}
 Font BoqHeadFont{get{return boqHeadFont??(boqHeadFont=new Font(boqGrid.Font,FontStyle.Bold));}}
 Font BoqDetailFont{get{return boqDetailFont??(boqDetailFont=new Font(boqGrid.Font.Name,Math.Max(7.5f,boqGrid.Font.SizeInPoints-0.75f),FontStyle.Regular,GraphicsUnit.Point));}}
 internal Font BoqItemFont{get{return boqItemFont??(boqItemFont=new Font(boqGrid.Font,FontStyle.Bold));}}
 const int BoqHeadQuantityIndent=28,BoqDetailQuantityIndent=56;
 static Color BoqLevelBack{get{return DesktopAppearance.Palette.Dark?Color.FromArgb(48,54,62):Color.FromArgb(240,244,248);}}
 internal const string BoqIncludeColumn="BoqInclude",LsIncludePrefix="LS include|";
 readonly Dictionary<string,bool> boqIncludes=new Dictionary<string,bool>(StringComparer.Ordinal);readonly HashSet<string> lsAutoSlots=new HashSet<string>(StringComparer.Ordinal);
 /* Owner (17 November 2026): every mapped / linked quantity is included by default; unchecking subtracts it from the total. */
readonly Dictionary<string,double> boqExcludedQuantities=new Dictionary<string,double>(StringComparer.Ordinal);
 bool BoqIncluded(string key,BackupBillItem item){bool v;return !boqIncludes.TryGetValue(key,out v)||v;}
 internal const string BoqStatusColumn="BoqStatus";
 void EnsureBoqIncludeColumn(){if(boqGrid==null||boqGrid.Columns.Contains(BoqIncludeColumn))return;
  /* Owner (18 November 2026): INCLUDE only as wide as its checkbox; STATUS on the right shows the mapping / link. */
  /* owner: no caption, just the checkbox width */var c=new DataGridViewCheckBoxColumn{Name=BoqIncludeColumn,HeaderText="",AutoSizeMode=DataGridViewAutoSizeColumnMode.None,Width=34,MinimumWidth=30,Resizable=DataGridViewTriState.False,ToolTipText="Checked: this mapped quantity counts in the pay item total. Unchecked: it stays mapped but is not counted. LS item: 1 LS when at least one mapped quantity is included (a manual LS quantity takes precedence)."};
  boqGrid.Columns.Add(c);c.ReadOnly=true;c.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleCenter;c.MinimumWidth=30;c.Width=34;c.HeaderCell.ToolTipText=c.ToolTipText;
  BoqRemapSetup();
  /* The column stays read-only for editing; a click toggles the value directly (it was not clickable in v1.4.110). */
  /* one toggle per click: see CellMouseClick below (the box is custom-painted, so the whole cell is the target) */
  boqGrid.KeyDown+=(s,e)=>{if(e.KeyCode!=Keys.Space||boqGrid.CurrentCell==null||boqGrid.CurrentCell.OwningColumn.Name!=BoqIncludeColumn||boqGrid.CurrentCell.Tag==null)return;e.Handled=e.SuppressKeyPress=true;ToggleInclude(boqGrid.CurrentCell);};
  /* Rows without a mapped quantity show no checkbox at all. */
  /* Owner (18 November 2026: "no check box"): the default checkbox glyph did not show in the BOQ on Windows, so the
     include box is drawn here: an outlined square, filled with a check mark when the quantity counts in the total. */
  boqGrid.CellPainting+=(s,e)=>{if(e.RowIndex<0||e.ColumnIndex<0||boqGrid.Columns[e.ColumnIndex].Name!=BoqIncludeColumn)return;var cell=boqGrid.Rows[e.RowIndex].Cells[e.ColumnIndex];
   e.PaintBackground(e.ClipBounds,(e.State&DataGridViewElementStates.Selected)!=0);e.Handled=true;if(cell.Tag==null)return;
   bool on=cell.Value is bool?(bool)cell.Value:string.Equals(Convert.ToString(cell.Value),"True",StringComparison.OrdinalIgnoreCase);
   /* Owner (18 November 2026): a pay item shows a check when all of its mapped quantities are included, an empty box when
      none are, and a dash when only some are (the unchecked ones are not in the total). */
   var group=cell.Tag as string[];bool partial=false;if(group!=null&&group.Length>0){int included=group.Count(k=>BoqIncluded(k,null));on=included==group.Length;partial=included>0&&!on;}
   int size=Math.Max(12,Math.Min(16,e.CellBounds.Height-10));var box=new Rectangle(e.CellBounds.Left+(e.CellBounds.Width-size)/2,e.CellBounds.Top+(e.CellBounds.Height-size)/2,size,size);
   var g=e.Graphics;var mode=g.SmoothingMode;g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
   try{Color accent=WorksheetColors.Focus;bool filled=on||partial;using(var fill=new SolidBrush(filled?accent:ModernUi.Input))g.FillRectangle(fill,box);using(var pen=new Pen(filled?accent:PowTheme.Muted,1.4f))g.DrawRectangle(pen,box);
    if(partial)using(var dash=new Pen(Color.White,2.2f){StartCap=System.Drawing.Drawing2D.LineCap.Round,EndCap=System.Drawing.Drawing2D.LineCap.Round})g.DrawLine(dash,box.Left+size*.25f,box.Top+size*.5f,box.Right-size*.25f,box.Top+size*.5f);
    if(on)using(var tick=new Pen(Color.White,2f){StartCap=System.Drawing.Drawing2D.LineCap.Round,EndCap=System.Drawing.Drawing2D.LineCap.Round,LineJoin=System.Drawing.Drawing2D.LineJoin.Round})g.DrawLines(tick,new[]{new PointF(box.Left+size*.22f,box.Top+size*.52f),new PointF(box.Left+size*.43f,box.Top+size*.72f),new PointF(box.Left+size*.78f,box.Top+size*.3f)});}
   finally{g.SmoothingMode=mode;}};
  boqGrid.CellMouseClick+=(s,e)=>{if(paintingBackupBill||e.RowIndex<0||e.ColumnIndex<0||e.Button!=MouseButtons.Left||boqGrid.Columns[e.ColumnIndex].Name!=BoqIncludeColumn)return;var cell=boqGrid.Rows[e.RowIndex].Cells[e.ColumnIndex];if(cell.Tag!=null)ToggleInclude(cell);};}
 // A mapped-quantity row toggles itself; a pay item row toggles all of its mapped quantities.
 void ToggleInclude(DataGridViewCell cell){var keys=cell.Tag as string[];var key=cell.Tag as string;if(key!=null)keys=new[]{key};if(keys==null||keys.Length==0)return;/* pay item: all included -> uncheck all; none or some -> check all (owner, 18 November 2026) */bool now=keys.Length>1||cell.Tag is string[]?!keys.All(k=>BoqIncluded(k,null)):!(cell.Value is bool&&(bool)cell.Value);cell.Value=now;foreach(string k in keys)boqIncludes[k]=now;
  dashboardDirty=reportPreviewDirty=true;try{BeginInvoke((Action)(()=>{RefreshVisibleWorksViews();ScheduleWorksExcel();}));}catch(InvalidOperationException){}}
 // Called by the BOQ rebuild: one row per mapped quantity (with INCLUDE), its measurement sources indented below.
 void AddBoqDemandRows(DataGridViewRow parent,string branch,BackupBillItem item,List<BackupDemand> sources,List<DataGridViewRow> pending){EnsureBoqIncludeColumn();
  var keys=sources.Where(d=>!d.ManualOverride&&!d.Key.StartsWith(LsIncludePrefix,StringComparison.Ordinal)).Select(d=>d.Key).ToArray();if(keys.Length>0&&parent.Cells.Count>4){var p=parent.Cells[4];p.Tag=keys;p.Value=keys.All(k=>BoqIncluded(k,item));p.ToolTipText="Check: all mapped quantities of this item are included. Dash: only some are included (unchecked ones are not in the total). Click to include all, or exclude all when all are included; expand the item to choose each one.";}
  NoteBoqStatus(item,sources);
  foreach(var d in sources){bool manual=d.ManualOverride||d.Key.StartsWith(LsIncludePrefix,StringComparison.Ordinal);var head=new DataGridViewRow();double shownQuantity;if(!boqExcludedQuantities.TryGetValue(d.Key,out shownQuantity))shownQuantity=d.Quantity;head.CreateCells(boqGrid,"","    "+d.Description,shownQuantity.ToString("0.########",CultureInfo.InvariantCulture),d.Unit,null);if(boqExcludedQuantities.ContainsKey(d.Key)){head.Cells[1].ToolTipText="Not counted in the total (unchecked). It stays mapped; check it again to include it.";}head.Tag=d;if(!manual)head.Cells[1].ToolTipText="Right-click to change the BACKUP item, leave it unmapped or include / exclude it.";bool excludedRow=boqExcludedQuantities.ContainsKey(d.Key);head.DefaultCellStyle.ForeColor=excludedRow?PowTheme.Muted:PowTheme.Ink;head.DefaultCellStyle.BackColor=BoqLevelBack;head.DefaultCellStyle.Font=excludedRow?BoqExcludedFont:BoqHeadFont;/* quantities step in by level so they are not read as one column of totals */head.Cells[2].Style.Padding=new Padding(0,0,BoqHeadQuantityIndent,0);foreach(DataGridViewCell cell in head.Cells)cell.ReadOnly=true;
   var include=head.Cells[4];if(!manual){include.Value=BoqIncluded(d.Key,item);include.Tag=d.Key;include.Style.BackColor=ModernUi.Input;include.ToolTipText="Click to include or exclude this quantity from the item total.";}
   BoqDisclosure.Parent(parent,branch);BoqDisclosure.Detail(head,branch);pending.Add(head);
   bool same=d.Sources.Count==1&&d.Sources[0].Description==d.Description;if(same)continue;
   foreach(var source in d.Sources){var detail=new DataGridViewRow();detail.CreateCells(boqGrid,"","          \u203A  "+source.Description,source.Quantity.ToString("0.########",CultureInfo.InvariantCulture),source.Unit,null);detail.ReadOnly=true;detail.DefaultCellStyle.ForeColor=PowTheme.Muted;detail.DefaultCellStyle.Font=BoqDetailFont;detail.Cells[2].Style.Padding=new Padding(0,0,BoqDetailQuantityIndent,0);detail.Cells[2].Style.BackColor=detail.Cells[3].Style.BackColor=Color.Empty;BoqDisclosure.Detail(detail,branch);pending.Add(detail);}}}
 string BoqSlotOf(BackupDemand d){if(d.TargetSlotId!=null)return d.TargetSlotId;string slot;return backupBillRoutes.TryGetValue(d.Key,out slot)?slot:null;}
 // Runs at the end of BackupQuantities: excluded mapped quantities count as 0; LS items get their 1 LS control.
 /* Metal painting is listed per member type (purlins, angle bars, metal fascia, crossbracing) instead of one total
    (owner, 18 November 2026). A project that mapped the old total keeps that pay item for the new lines. */
 void InheritSteelPaintRoutes(List<BackupDemand> list){string totalKey="Structural Steel Works|"+SteelPaintingTotal+"|"+BackupMapping.Unit("m2");string slot;if(!backupBillRoutes.TryGetValue(totalKey,out slot))return;
  foreach(var d in list){if(!d.Key.StartsWith("Structural Steel Works|Metal painting - ",StringComparison.Ordinal)||backupBillRoutes.ContainsKey(d.Key)||boqUnmapped.Contains(d.Key))continue;backupBillRoutes[d.Key]=slot;boqRouteOverrides[d.Key]=slot;}}
 void ApplyBoqIncludes(List<BackupDemand> list){InheritSteelPaintRoutes(list);DefaultOpeningRoutes(list);list.RemoveAll(d=>d.Key.StartsWith(LsIncludePrefix,StringComparison.Ordinal));if(backupBill==null||backupBill.Count==0)return;
  var items=backupBill.GroupBy(b=>b.Slot).ToDictionary(g=>g.Key,g=>g.First());
  /* Owner (18 November 2026): an unchecked quantity stays mapped and listed under its pay item with its own quantity;
     it is only left out of the total. This applies to saved and automatically suggested mappings alike. */
  boqExcludedQuantities.Clear();foreach(var d in list){if(d.ManualOverride||BoqIncluded(d.Key,null))continue;boqExcludedQuantities[d.Key]=d.Quantity;d.Quantity=0;}
  foreach(var item in items.Values){if(BackupMapping.Unit(item.Unit)!="ls"||item.ManualQuantity.HasValue)continue;var mapped=list.Where(d=>!d.ManualOverride&&BackupMapping.Unit(d.Unit)!="ls"&&BoqSlotOf(d)==item.Slot).ToList();if(mapped.Count==0)continue;
   if(list.Any(d=>!d.ManualOverride&&BackupMapping.Unit(d.Unit)=="ls"&&d.Quantity>0&&BoqSlotOf(d)==item.Slot))continue;
   bool any=mapped.Any(d=>d.Quantity>1e-10&&BoqIncluded(d.Key,item));if(!any&&!lsAutoSlots.Contains(item.Slot))continue;if(any)lsAutoSlots.Add(item.Slot);
   var control=new BackupDemand{Key=LsIncludePrefix+item.Slot,Description=any?"1 LS - included mapped quantities":"0 LS - no mapped quantity included",Unit=item.Unit,Quantity=any?1:0,TargetSlotId=item.Slot,TargetIdentity=item.Signature,ManualPriority=1};
   control.Sources.Add(new BillQuantity{Description=control.Description,Quantity=control.Quantity,Unit=item.Unit});list.Add(control);}}
 void WriteBoqIncludes(XmlElement routes){foreach(var pair in boqIncludes.OrderBy(p=>p.Key,StringComparer.Ordinal)){var e=routes.OwnerDocument.CreateElement("Include");e.SetAttribute("key",pair.Key);e.SetAttribute("value",pair.Value?"1":"0");routes.AppendChild(e);}foreach(string slot in lsAutoSlots.OrderBy(x=>x,StringComparer.Ordinal)){var e=routes.OwnerDocument.CreateElement("LsAuto");e.SetAttribute("slot",slot);routes.AppendChild(e);}WriteBoqOverrides(routes);}
 void ReadBoqIncludes(XmlElement routes){boqIncludes.Clear();lsAutoSlots.Clear();ReadBoqOverrides(routes);if(routes==null)return;foreach(XmlElement e in routes.SelectNodes("Include")){string key=e.GetAttribute("key");if(key!="")boqIncludes[key]=e.GetAttribute("value")=="1";}foreach(XmlElement e in routes.SelectNodes("LsAuto")){string slot=e.GetAttribute("slot");if(slot!="")lsAutoSlots.Add(slot);}}
}
}
