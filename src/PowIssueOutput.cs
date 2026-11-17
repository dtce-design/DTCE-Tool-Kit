using System;using System.Linq;using System.Collections.Generic;using System.Xml;
namespace BeamScheduleFormatter {
static class PowIssueOutput {
 internal static void ExcludeTargets(BackupWritePlan plan,IEnumerable<string> slots){var blocked=new HashSet<string>(slots,StringComparer.Ordinal);var keys=new HashSet<string>(plan.Routes.Where(r=>blocked.Contains(r.Value.SlotId)).Select(r=>r.Key),StringComparer.Ordinal);plan.Demands=plan.Demands.Where(d=>!keys.Contains(d.Key)).ToList();foreach(string key in keys)plan.Routes.Remove(key);}
 internal static void Write(XmlElement root,IEnumerable<string> issues){var section=root.OwnerDocument.CreateElement("InputIssues");root.AppendChild(section);foreach(string issue in issues.Distinct()){var entry=root.OwnerDocument.CreateElement("Issue");entry.InnerText=issue;section.AppendChild(entry);}section.SetAttribute("output",section.HasChildNodes?"Partial":"Complete");}
}
}
