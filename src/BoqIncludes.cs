using System;using System.Collections.Generic;using System.Drawing;using System.Globalization;using System.Linq;using System.Windows.Forms;using System.Xml;
namespace BeamScheduleFormatter {
// "Include" beside UNIT for every mapped quantity in Bill of Quantities (owner, 17 November 2026).
// - Checked: the mapped quantity counts in its pay item's total. Unchecked: it stays mapped (listed under the item and
//   saved) but is not counted.
// - Default: checked for every mapped / linked quantity (owner); unchecking subtracts it from the total.
// - An LS item without a manual quantity becomes 1 LS when at least one of its mapped quantities is included; a manual
//   LS quantity entered in Bill of Quantities always takes precedence. Unchecking all of them again writes 0.
partial class ProgramOfWorksForm {
 internal const string BoqIncludeColumn="BoqInclude",LsIncludePrefix="LS include|";
 readonly Dictionary<string,bool> boqIncludes=new Dictionary<string,bool>(StringComparer.Ordinal);readonly HashSet<string> lsAutoSlots=new HashSet<string>(StringComparer.Ordinal);
 /* Owner (17 November 2026): every mapped / linked quantity is included by default; unchecking subtracts it from the total. */
 bool BoqIncluded(string key,BackupBillItem item){bool v;return !boqIncludes.TryGetValue(key,out v)||v;}
 internal const string BoqStatusColumn="BoqStatus";
 void EnsureBoqIncludeColumn(){if(boqGrid==null||boqGrid.Columns.Contains(BoqIncludeColumn))return;
  /* Owner (18 November 2026): INCLUDE only as wide as its checkbox; STATUS on the right shows the mapping / link. */
  var c=new DataGridViewCheckBoxColumn{Name=BoqIncludeColumn,HeaderText="INCLUDE",AutoSizeMode=DataGridViewAutoSizeColumnMode.None,Width=76,MinimumWidth=60,Resizable=DataGridViewTriState.False,ToolTipText="Checked: this mapped quantity counts in the pay item total. Unchecked: it stays mapped but is not counted. LS item: 1 LS when at least one mapped quantity is included (a manual LS quantity takes precedence)."};
  boqGrid.Columns.Add(c);c.ReadOnly=true;c.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleCenter;
  BoqRemapSetup();
  /* The column stays read-only for editing; a click toggles the value directly (it was not clickable in v1.4.110). */
  boqGrid.CellContentClick+=(s,e)=>{if(paintingBackupBill||e.RowIndex<0||e.ColumnIndex<0||boqGrid.Columns[e.ColumnIndex].Name!=BoqIncludeColumn)return;ToggleInclude(boqGrid.Rows[e.RowIndex].Cells[e.ColumnIndex]);};
  boqGrid.KeyDown+=(s,e)=>{if(e.KeyCode!=Keys.Space||boqGrid.CurrentCell==null||boqGrid.CurrentCell.OwningColumn.Name!=BoqIncludeColumn||boqGrid.CurrentCell.Tag==null)return;e.Handled=e.SuppressKeyPress=true;ToggleInclude(boqGrid.CurrentCell);};
  /* Rows without a mapped quantity show no checkbox at all. */
  boqGrid.CellPainting+=(s,e)=>{if(e.RowIndex<0||e.ColumnIndex<0||boqGrid.Columns[e.ColumnIndex].Name!=BoqIncludeColumn||boqGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Tag!=null)return;e.PaintBackground(e.ClipBounds,(e.State&DataGridViewElementStates.Selected)!=0);e.Handled=true;};}
 // A mapped-quantity row toggles itself; a pay item row toggles all of its mapped quantities.
 void ToggleInclude(DataGridViewCell cell){var keys=cell.Tag as string[];var key=cell.Tag as string;if(key!=null)keys=new[]{key};if(keys==null||keys.Length==0)return;bool now=!(cell.Value is bool&&(bool)cell.Value);cell.Value=now;foreach(string k in keys)boqIncludes[k]=now;
  dashboardDirty=reportPreviewDirty=true;try{BeginInvoke((Action)(()=>{RefreshVisibleWorksViews();ScheduleWorksExcel();}));}catch(InvalidOperationException){}}
 // Called by the BOQ rebuild: one row per mapped quantity (with INCLUDE), its measurement sources indented below.
 void AddBoqDemandRows(DataGridViewRow parent,string branch,BackupBillItem item,List<BackupDemand> sources,List<DataGridViewRow> pending){EnsureBoqIncludeColumn();
  var keys=sources.Where(d=>!d.ManualOverride&&!d.Key.StartsWith(LsIncludePrefix,StringComparison.Ordinal)).Select(d=>d.Key).ToArray();if(keys.Length>0&&parent.Cells.Count>4){var p=parent.Cells[4];p.Tag=keys;p.Value=keys.Any(k=>BoqIncluded(k,item));p.ToolTipText="Checked when at least one mapped quantity of this item is included. Click to include or exclude all of them; expand the item to choose each one.";}
  NoteBoqStatus(item,sources);
  foreach(var d in sources){bool manual=d.ManualOverride||d.Key.StartsWith(LsIncludePrefix,StringComparison.Ordinal);var head=new DataGridViewRow();head.CreateCells(boqGrid,"","    "+d.Description,d.Quantity.ToString("0.########",CultureInfo.InvariantCulture),d.Unit,null);head.Tag=d;if(!manual)head.Cells[1].ToolTipText="Right-click to change the BACKUP item, leave it unmapped or include / exclude it.";head.DefaultCellStyle.ForeColor=PowTheme.Muted;foreach(DataGridViewCell cell in head.Cells)cell.ReadOnly=true;
   var include=head.Cells[4];if(!manual){include.Value=BoqIncluded(d.Key,item);include.Tag=d.Key;include.Style.BackColor=ModernUi.Input;include.ToolTipText="Click to include or exclude this quantity from the item total.";}
   BoqDisclosure.Parent(parent,branch);BoqDisclosure.Detail(head,branch);pending.Add(head);
   bool same=d.Sources.Count==1&&d.Sources[0].Description==d.Description;if(same)continue;
   foreach(var source in d.Sources){var detail=new DataGridViewRow();detail.CreateCells(boqGrid,"","        "+source.Description,source.Quantity.ToString("0.########",CultureInfo.InvariantCulture),source.Unit,null);detail.ReadOnly=true;detail.DefaultCellStyle.ForeColor=PowTheme.Muted;BoqDisclosure.Detail(detail,branch);pending.Add(detail);}}}
 string BoqSlotOf(BackupDemand d){if(d.TargetSlotId!=null)return d.TargetSlotId;string slot;return backupBillRoutes.TryGetValue(d.Key,out slot)?slot:null;}
 // Runs at the end of BackupQuantities: excluded mapped quantities count as 0; LS items get their 1 LS control.
 void ApplyBoqIncludes(List<BackupDemand> list){list.RemoveAll(d=>d.Key.StartsWith(LsIncludePrefix,StringComparison.Ordinal));if(backupBill==null||backupBill.Count==0)return;
  var items=backupBill.GroupBy(b=>b.Slot).ToDictionary(g=>g.Key,g=>g.First());
  foreach(var d in list){if(d.ManualOverride)continue;string slot;if(!backupBillRoutes.TryGetValue(d.Key,out slot))continue;BackupBillItem item;if(!items.TryGetValue(slot,out item))continue;if(!BoqIncluded(d.Key,item))d.Quantity=0;}
  foreach(var item in items.Values){if(BackupMapping.Unit(item.Unit)!="ls"||item.ManualQuantity.HasValue)continue;var mapped=list.Where(d=>!d.ManualOverride&&BackupMapping.Unit(d.Unit)!="ls"&&BoqSlotOf(d)==item.Slot).ToList();if(mapped.Count==0)continue;
   if(list.Any(d=>!d.ManualOverride&&BackupMapping.Unit(d.Unit)=="ls"&&d.Quantity>0&&BoqSlotOf(d)==item.Slot))continue;
   bool any=mapped.Any(d=>d.Quantity>1e-10&&BoqIncluded(d.Key,item));if(!any&&!lsAutoSlots.Contains(item.Slot))continue;if(any)lsAutoSlots.Add(item.Slot);
   var control=new BackupDemand{Key=LsIncludePrefix+item.Slot,Description=any?"1 LS - included mapped quantities":"0 LS - no mapped quantity included",Unit=item.Unit,Quantity=any?1:0,TargetSlotId=item.Slot,TargetIdentity=item.Signature,ManualPriority=1};
   control.Sources.Add(new BillQuantity{Description=control.Description,Quantity=control.Quantity,Unit=item.Unit});list.Add(control);}}
 void WriteBoqIncludes(XmlElement routes){foreach(var pair in boqIncludes.OrderBy(p=>p.Key,StringComparer.Ordinal)){var e=routes.OwnerDocument.CreateElement("Include");e.SetAttribute("key",pair.Key);e.SetAttribute("value",pair.Value?"1":"0");routes.AppendChild(e);}foreach(string slot in lsAutoSlots.OrderBy(x=>x,StringComparer.Ordinal)){var e=routes.OwnerDocument.CreateElement("LsAuto");e.SetAttribute("slot",slot);routes.AppendChild(e);}WriteBoqOverrides(routes);}
 void ReadBoqIncludes(XmlElement routes){boqIncludes.Clear();lsAutoSlots.Clear();ReadBoqOverrides(routes);if(routes==null)return;foreach(XmlElement e in routes.SelectNodes("Include")){string key=e.GetAttribute("key");if(key!="")boqIncludes[key]=e.GetAttribute("value")=="1";}foreach(XmlElement e in routes.SelectNodes("LsAuto")){string slot=e.GetAttribute("slot");if(slot!="")lsAutoSlots.Add(slot);}}
}
}
