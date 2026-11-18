using System;using System.Collections.Generic;using System.Drawing;using System.Globalization;using System.Linq;using System.Windows.Forms;using System.Xml;
namespace BeamScheduleFormatter {
// BOQ STATUS column and changing a mapping directly in Bill of Quantities (owner, 18 November 2026).
// - STATUS shows, per pay item: manual quantity / linked quantities (n mapped, m excluded) / Excel value; per mapped
//   quantity: mapped / auto-matched / excluded / manual / automatic LS; per unmapped quantity: not mapped.
// - Click the STATUS of a quantity (or right-click it) to choose another BACKUP item; right-click also offers
//   "Leave unmapped" and Include / Exclude. The choice is saved with the project and overrides the workbook mapping
//   on the next Excel update (routes are written to BACKUP then).
partial class ProgramOfWorksForm {
 readonly Dictionary<string,string> boqRouteOverrides=new Dictionary<string,string>(StringComparer.Ordinal);readonly HashSet<string> boqUnmapped=new HashSet<string>(StringComparer.Ordinal);
 static readonly Color StatusLink=Color.FromArgb(28,98,170);
 string DemandStatus(BackupDemand d,BackupBillItem item){if(d.Key.StartsWith(LsIncludePrefix,StringComparison.Ordinal))return d.Quantity>0?"Automatic 1 LS":"Automatic 0 LS";if(d.ManualOverride)return"Manual quantity";
  string state=backupBillRoutes.ContainsKey(d.Key)||d.TargetSlotId!=null?"Mapped":"Auto-matched";return (BoqIncluded(d.Key,item)?state:state+" · excluded")+"  ✎";}
 string ItemStatus(BackupBillItem item,List<BackupDemand> sources){if(item.ManualInputText!=null)return"Manual (invalid - review)";if(item.ManualQuantity.HasValue)return"Manual quantity";
  var mapped=sources.Where(d=>!d.ManualOverride&&!d.Key.StartsWith(LsIncludePrefix,StringComparison.Ordinal)).ToList();if(mapped.Count>0){int excluded=mapped.Count(d=>!BoqIncluded(d.Key,item));return"Linked: "+mapped.Count+" mapped"+(excluded>0?", "+excluded+" excluded":"");}
  return item.WorkbookQuantity.HasValue?"Excel value":"";}
 static void StyleStatusLink(DataGridViewCell cell,bool link){if(!link)return;cell.Style.ForeColor=StatusLink;cell.Style.SelectionForeColor=StatusLink;cell.ToolTipText="Click to change the BACKUP item. Right-click for more.";}
 void BoqRemapSetup(){
  boqGrid.CellClick+=(s,e)=>{if(e.RowIndex<0||e.ColumnIndex<0||boqGrid.Columns[e.ColumnIndex].Name!=BoqStatusColumn)return;var d=boqGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Tag as BackupDemand;if(d!=null&&!d.ManualOverride&&!d.Key.StartsWith(LsIncludePrefix,StringComparison.Ordinal))ChangeBoqMapping(d);};
  var menu=new ContextMenuStrip();BackupDemand target=null;var change=new ToolStripMenuItem("Change BACKUP item…");var unmap=new ToolStripMenuItem("Leave unmapped");var include=new ToolStripMenuItem("Include in total");
  change.Click+=(s,e)=>{if(target!=null)ChangeBoqMapping(target);};unmap.Click+=(s,e)=>{if(target!=null)SetBoqRoute(target.Key,null);};
  include.Click+=(s,e)=>{if(target==null)return;boqIncludes[target.Key]=!include.Checked;dashboardDirty=reportPreviewDirty=true;RefreshVisibleWorksViews();ScheduleWorksExcel();};
  menu.Items.AddRange(new ToolStripItem[]{change,unmap,new ToolStripSeparator(),include});
  boqGrid.CellMouseDown+=(s,e)=>{if(e.Button!=MouseButtons.Right||e.RowIndex<0)return;var row=boqGrid.Rows[e.RowIndex];target=row.Cells.Count>5?row.Cells[5].Tag as BackupDemand:null;if(target==null||target.ManualOverride||target.Key.StartsWith(LsIncludePrefix,StringComparison.Ordinal))return;
   bool mapped=backupBillRoutes.ContainsKey(target.Key)||SlotForDemand(target)!=null;unmap.Enabled=mapped;include.Enabled=mapped;var item=backupBill.FirstOrDefault(b=>b.Slot==SlotForDemand(target));include.Checked=mapped&&BoqIncluded(target.Key,item);change.Text=mapped?"Change BACKUP item…":"Map to BACKUP item…";
   menu.Show(boqGrid,boqGrid.PointToClient(Cursor.Position));};
  Disposed+=(s,e)=>menu.Dispose();}
 string SlotForDemand(BackupDemand d){if(d.TargetSlotId!=null)return d.TargetSlotId;string slot;if(backupBillRoutes.TryGetValue(d.Key,out slot))return slot;if(boqUnmapped.Contains(d.Key)||BackupMapping.IsPainting(d))return null;var t=BackupMapping.Suggest(d,backupBill.Select(b=>b.Target).ToList());return t==null?null:t.SlotId;}
 void ChangeBoqMapping(BackupDemand d){var choices=backupBill.Select(b=>b.Target).Where(t=>BackupMapping.Unit(t.Unit)==BackupMapping.Unit(d.Unit)||BackupMapping.Unit(t.Unit)=="ls").ToList();if(choices.Count==0){MessageBox.Show(this,"No BACKUP item with a compatible unit ("+d.Unit+") is loaded. Link the BACKUP workbook first.","Change BACKUP item",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
  string currentSlot=SlotForDemand(d);var current=choices.FirstOrDefault(t=>t.SlotId==currentSlot);
  using(var picker=new BackupTargetPicker(d.Description,choices,current)){if(picker.ShowDialog(this)!=DialogResult.OK||picker.Selected==null)return;SetBoqRoute(d.Key,picker.Selected.SlotId);}}
 void SetBoqRoute(string key,string slot){if(slot==null){backupBillRoutes.Remove(key);boqUnmapped.Add(key);boqRouteOverrides[key]=null;}else{backupBillRoutes[key]=slot;boqUnmapped.Remove(key);boqRouteOverrides[key]=slot;}
  dashboardDirty=reportPreviewDirty=true;RefreshVisibleWorksViews();ScheduleWorksExcel();if(excelStatus!=null&&worksExcel!=null)excelStatus.Text="Mapping changed in Bill of Quantities. Writing to BACKUP in the background…";}
 // Applied to every Excel write plan so BOQ choices win over the mapping stored in the workbook.
 void ApplyBoqRouteOverrides(BackupWritePlan plan){if(plan==null||plan.Routes==null)return;foreach(var pair in boqRouteOverrides){if(pair.Value==null){plan.Routes.Remove(pair.Key);continue;}var t=plan.Targets.FirstOrDefault(x=>x.SlotId==pair.Value);if(t!=null&&plan.Demands.Any(d=>d.Key==pair.Key))plan.Routes[pair.Key]=t;}}
 void WriteBoqOverrides(XmlElement routes){foreach(var pair in boqRouteOverrides.OrderBy(p=>p.Key,StringComparer.Ordinal)){var e=routes.OwnerDocument.CreateElement("Override");e.SetAttribute("key",pair.Key);if(pair.Value!=null)e.SetAttribute("slot",pair.Value);routes.AppendChild(e);}}
 void ReadBoqOverrides(XmlElement routes){boqRouteOverrides.Clear();boqUnmapped.Clear();if(routes==null)return;foreach(XmlElement e in routes.SelectNodes("Override")){string key=e.GetAttribute("key");if(key=="")continue;string slot=e.HasAttribute("slot")?e.GetAttribute("slot"):null;boqRouteOverrides[key]=slot;if(slot==null)boqUnmapped.Add(key);}}
}
}
