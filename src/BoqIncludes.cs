using System;using System.Collections.Generic;using System.Drawing;using System.Globalization;using System.Linq;using System.Windows.Forms;using System.Xml;
namespace BeamScheduleFormatter {
// "Include" beside UNIT for every mapped quantity in Bill of Quantities (owner, 17 November 2026).
// - Checked: the mapped quantity counts in its pay item's total. Unchecked: it stays mapped (listed under the item and
//   saved) but is not counted.
// - Default: checked for measured items (kg, m2, m3 ...); unchecked for LS items, so an LS item is never 1 LS by itself.
// - An LS item without a manual quantity becomes 1 LS when at least one of its mapped quantities is included; a manual
//   LS quantity entered in Bill of Quantities always takes precedence. Unchecking all of them again writes 0.
partial class ProgramOfWorksForm {
 internal const string BoqIncludeColumn="BoqInclude",LsIncludePrefix="LS include|";
 readonly Dictionary<string,bool> boqIncludes=new Dictionary<string,bool>(StringComparer.Ordinal);readonly HashSet<string> lsAutoSlots=new HashSet<string>(StringComparer.Ordinal);
 bool BoqIncluded(string key,BackupBillItem item){bool v;if(boqIncludes.TryGetValue(key,out v))return v;return item==null||BackupMapping.Unit(item.Unit)!="ls";}
 void EnsureBoqIncludeColumn(){if(boqGrid==null||boqGrid.Columns.Contains(BoqIncludeColumn))return;var c=new DataGridViewCheckBoxColumn{Name=BoqIncludeColumn,HeaderText="INCLUDE",Width=80,FillWeight=7,MinimumWidth=70,ToolTipText="Checked: this mapped quantity counts in the pay item total. LS items: checking a mapped quantity makes the item 1 LS (unless a manual LS quantity is entered)."};boqGrid.Columns.Add(c);c.ReadOnly=true;
  boqGrid.CurrentCellDirtyStateChanged+=(s,e)=>{if(boqGrid.IsCurrentCellDirty&&boqGrid.CurrentCell!=null&&boqGrid.CurrentCell.OwningColumn.Name==BoqIncludeColumn)boqGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);};
  boqGrid.CellValueChanged+=(s,e)=>{if(paintingBackupBill||e.RowIndex<0||e.ColumnIndex<0||boqGrid.Columns[e.ColumnIndex].Name!=BoqIncludeColumn)return;var key=boqGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Tag as string;if(key==null)return;var v=boqGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;boqIncludes[key]=v is bool&&(bool)v;
   dashboardDirty=reportPreviewDirty=true;try{BeginInvoke((Action)(()=>{RefreshVisibleWorksViews();ScheduleWorksExcel();}));}catch(InvalidOperationException){}};}
 // Called by the BOQ rebuild: one row per mapped quantity (with INCLUDE), its measurement sources indented below.
 void AddBoqDemandRows(DataGridViewRow parent,string branch,BackupBillItem item,List<BackupDemand> sources,List<DataGridViewRow> pending){EnsureBoqIncludeColumn();
  foreach(var d in sources){bool manual=d.ManualOverride||d.Key.StartsWith(LsIncludePrefix,StringComparison.Ordinal);var head=new DataGridViewRow();head.CreateCells(boqGrid,"","    "+d.Description,d.Quantity.ToString("0.########",CultureInfo.InvariantCulture),d.Unit,null);head.DefaultCellStyle.ForeColor=PowTheme.Muted;foreach(DataGridViewCell cell in head.Cells)cell.ReadOnly=true;
   var include=head.Cells[4];if(!manual){include.Value=BoqIncluded(d.Key,item);include.Tag=d.Key;include.ReadOnly=false;include.Style.BackColor=ModernUi.Input;}
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
 void WriteBoqIncludes(XmlElement routes){foreach(var pair in boqIncludes.OrderBy(p=>p.Key,StringComparer.Ordinal)){var e=routes.OwnerDocument.CreateElement("Include");e.SetAttribute("key",pair.Key);e.SetAttribute("value",pair.Value?"1":"0");routes.AppendChild(e);}foreach(string slot in lsAutoSlots.OrderBy(x=>x,StringComparer.Ordinal)){var e=routes.OwnerDocument.CreateElement("LsAuto");e.SetAttribute("slot",slot);routes.AppendChild(e);}}
 void ReadBoqIncludes(XmlElement routes){boqIncludes.Clear();lsAutoSlots.Clear();if(routes==null)return;foreach(XmlElement e in routes.SelectNodes("Include")){string key=e.GetAttribute("key");if(key!="")boqIncludes[key]=e.GetAttribute("value")=="1";}foreach(XmlElement e in routes.SelectNodes("LsAuto")){string slot=e.GetAttribute("slot");if(slot!="")lsAutoSlots.Add(slot);}}
}
}
