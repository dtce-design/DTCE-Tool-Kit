using System;using System.Collections.Generic;using System.Drawing;using System.Globalization;using System.Linq;using System.Windows.Forms;using System.Xml;
namespace BeamScheduleFormatter {
// BOQ status notes and changing a mapping directly in Bill of Quantities (owner, 18 November 2026). The status is a
// separate panel under the table (not a column): errors, unmapped quantities, excluded quantities, automatic LS.
// - STATUS shows, per pay item: manual quantity / linked quantities (n mapped, m excluded) / Excel value; per mapped
//   quantity: mapped / auto-matched / excluded / manual / automatic LS; per unmapped quantity: not mapped.
// - Click the STATUS of a quantity (or right-click it) to choose another BACKUP item; right-click also offers
//   "Leave unmapped" and Include / Exclude. The choice is saved with the project and overrides the workbook mapping
//   on the next Excel update (routes are written to BACKUP then).
partial class ProgramOfWorksForm {
 readonly Dictionary<string,string> boqRouteOverrides=new Dictionary<string,string>(StringComparer.Ordinal);readonly HashSet<string> boqUnmapped=new HashSet<string>(StringComparer.Ordinal);
 internal sealed class BoqStatusNote {internal string Kind,Text;internal BackupDemand Demand;internal BackupBillItem Item;internal List<BackupDemand> Demands;}
 readonly List<BoqStatusNote> boqStatusNotes=new List<BoqStatusNote>();
 // Collected while the BOQ is rebuilt; shown in the status panel under the table.
 void NoteBoqStatus(BackupBillItem item,List<BackupDemand> sources){string head=item.Code+" "+item.Description;
  if(item.ManualInputText!=null)boqStatusNotes.Add(new BoqStatusNote{Kind="Error",Text=head+": invalid manual quantity \""+item.ManualInputText+"\"",Item=item});
  foreach(var d in sources){if(d.Key.StartsWith(LsIncludePrefix,StringComparison.Ordinal)){/* owner, 20 November 2026 ("alisin mo na ito sa status, yung mga na exclude lang and other issues"): a working automatic LS
     (1 LS) is not listed; only an LS item left at 0 is, because nothing mapped is included */if(d.Quantity<=0)boqStatusNotes.Add(new BoqStatusNote{Kind="Automatic LS",Text=head+": 0 LS - no mapped quantity included",Item=item});continue;}
   }
  /* Owner (18 November 2026, "huwag na isa-isahin, yung parent item no. lang ilagay"): one Excluded line per pay item. */
  var excluded=sources.Where(d=>!d.ManualOverride&&!d.Key.StartsWith(LsIncludePrefix,StringComparison.Ordinal)&&!BoqIncluded(d.Key,item)).ToList();
  if(excluded.Count>0)boqStatusNotes.Add(new BoqStatusNote{Kind="Excluded",Text=head+" — "+excluded.Count+" mapped quantit"+(excluded.Count==1?"y":"ies")+" not counted (double-click to include)",Demand=excluded[0],Demands=excluded,Item=item});}
 void BoqRemapSetup(){
  var menu=new ContextMenuStrip();BackupDemand target=null;var change=new ToolStripMenuItem("Change BACKUP item…");var unmap=new ToolStripMenuItem("Leave unmapped");var include=new ToolStripMenuItem("Include in total");
  change.Click+=(s,e)=>{if(target!=null)ChangeBoqMapping(target);};unmap.Click+=(s,e)=>{if(target!=null)SetBoqRoute(target.Key,null);};
  include.Click+=(s,e)=>{if(target==null)return;boqIncludes[target.Key]=!include.Checked;dashboardDirty=reportPreviewDirty=true;RefreshVisibleWorksViews();ScheduleWorksExcel();};
  menu.Items.AddRange(new ToolStripItem[]{change,unmap,new ToolStripSeparator(),include});
  boqGrid.CellMouseDown+=(s,e)=>{if(e.Button!=MouseButtons.Right||e.RowIndex<0)return;var row=boqGrid.Rows[e.RowIndex];target=row.Tag as BackupDemand;if(target==null||target.ManualOverride||target.Key.StartsWith(LsIncludePrefix,StringComparison.Ordinal))return;
   bool mapped=backupBillRoutes.ContainsKey(target.Key)||SlotForDemand(target)!=null;unmap.Enabled=mapped;include.Enabled=mapped;var item=backupBill.FirstOrDefault(b=>b.Slot==SlotForDemand(target));include.Checked=mapped&&BoqIncluded(target.Key,item);change.Text=mapped?"Change BACKUP item…":"Map to BACKUP item…";
   menu.Show(boqGrid,boqGrid.PointToClient(Cursor.Position));};
  Disposed+=(s,e)=>menu.Dispose();}
 string SlotForDemand(BackupDemand d){string chosen;if(!d.ManualOverride&&boqUnmapped.Contains(d.Key))return null;if(!d.ManualOverride&&boqRouteOverrides.TryGetValue(d.Key,out chosen)&&chosen!=null)return chosen;if(d.TargetSlotId!=null){var t0=BackupMapping.ResolveTarget(d,backupBill.Select(b=>b.Target));if(t0!=null)return t0.SlotId;}string slot;if(backupBillRoutes.TryGetValue(d.Key,out slot))return slot;if(boqUnmapped.Contains(d.Key)||BackupMapping.IsPainting(d))return null;var t=BackupMapping.Suggest(d,backupBill.Select(b=>b.Target).ToList());return t==null?null:t.SlotId;}
 void ChangeBoqMapping(BackupDemand d){var choices=backupBill.Select(b=>b.Target).Where(t=>BackupMapping.Unit(t.Unit)==BackupMapping.Unit(d.Unit)||BackupMapping.Unit(t.Unit)=="ls").ToList();if(choices.Count==0){MessageBox.Show(this,"No BACKUP item with a compatible unit ("+d.Unit+") is loaded. Link the BACKUP workbook first.","Change BACKUP item",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
  string currentSlot=SlotForDemand(d);var current=choices.FirstOrDefault(t=>t.SlotId==currentSlot);
  using(var picker=new BackupTargetPicker(d.Description,choices,current)){if(picker.ShowDialog(this)!=DialogResult.OK||picker.Selected==null)return;SetBoqRoute(d.Key,picker.Selected.SlotId);}}
 void SetBoqRoute(string key,string slot){if(slot==null){backupBillRoutes.Remove(key);boqIncludes.Remove(key);/* unmapped: listed as Not mapped with its quantity */boqUnmapped.Add(key);boqRouteOverrides[key]=null;}else{backupBillRoutes[key]=slot;boqUnmapped.Remove(key);boqRouteOverrides[key]=slot;}
  dashboardDirty=reportPreviewDirty=true;RefreshVisibleWorksViews();ScheduleWorksExcel();if(excelStatus!=null&&worksExcel!=null)excelStatus.Text="Mapping changed in Bill of Quantities. Writing to BACKUP in the background…";}
 // Applied to every Excel write plan so BOQ choices win over the mapping stored in the workbook.
 void ApplyBoqRouteOverrides(BackupWritePlan plan){if(plan==null||plan.Routes==null)return;foreach(var pair in boqRouteOverrides){if(pair.Value==null){plan.Routes.Remove(pair.Key);continue;}var t=plan.Targets.FirstOrDefault(x=>x.SlotId==pair.Value);if(t==null)continue;foreach(var d in plan.Demands.Where(x=>x.Key==pair.Key&&!x.ManualOverride)){/* the chosen item replaces the app's own target (19 November 2026) */d.TargetSlotId=null;d.TargetIdentity=null;plan.Routes[pair.Key]=t;}}}
 void WriteBoqOverrides(XmlElement routes){foreach(var pair in boqRouteOverrides.OrderBy(p=>p.Key,StringComparer.Ordinal)){var e=routes.OwnerDocument.CreateElement("Override");e.SetAttribute("key",pair.Key);if(pair.Value!=null)e.SetAttribute("slot",pair.Value);routes.AppendChild(e);}}
 void ReadBoqOverrides(XmlElement routes){boqRouteOverrides.Clear();boqUnmapped.Clear();if(routes==null)return;foreach(XmlElement e in routes.SelectNodes("Override")){string key=e.GetAttribute("key");if(key=="")continue;string slot=e.HasAttribute("slot")?e.GetAttribute("slot"):null;boqRouteOverrides[key]=slot;if(slot==null)boqUnmapped.Add(key);}}
}
}
