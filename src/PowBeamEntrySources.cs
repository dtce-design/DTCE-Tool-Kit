using System;using System.Linq;using System.Collections.Generic;using System.Xml;
namespace BeamScheduleFormatter {
// Beam marks are globally unique in POW. Source labels may change across saves;
// use a unique actual mark to repair the reference, and retain unresolved rows.
static class PowBeamEntrySources {
 internal static void RestoreReferences(XmlElement root,IList<ScheduleSource> sources){foreach(XmlElement e in root.SelectNodes("Entry")){string mark=PowBeamMarks.Name(e.GetAttribute("mark"));e.SetAttribute("mark",mark);var owners=sources.Where(s=>s.Rows.ContainsKey(mark)).ToList();if(owners.Count>1)throw new Exception("Ambiguous saved beam mark: "+mark);if(owners.Count==1)e.SetAttribute("schedule",owners[0].Label);else if(!sources.Any(s=>s.Label==e.GetAttribute("schedule"))&&mark.Length==0&&sources.Count>0)e.SetAttribute("schedule",sources[0].Label);}}
}
}
