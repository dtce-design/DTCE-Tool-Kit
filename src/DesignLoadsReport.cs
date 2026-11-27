using System;using System.Collections.Generic;using System.Drawing;using System.IO;using System.Linq;using System.Threading.Tasks;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* one calculation report for all the Design Loads (owner, 27 November 2026: "yung report sa design loads minsanan na lang"; "calculation report
   sa tabi ng load combo, nandon lahat ng design loads"): a contents page, then the full report of Dead Loads, Live Loads, Wind Load,
   Earthquake Load and Load Combinations in that order; every page keeps the title of its sheet. */
static class DesignLoadsReport {
 internal static async Task<CalculationDocument> Build(IList<KeyValuePair<string,DesignSheetForm>> sheets,List<string> problems){var all=new CalculationDocument("Design Loads - NSCP 2015");all.PageTitles=new List<string>();
  var parts=new List<KeyValuePair<string,CalculationDocument>>();
  foreach(var s in sheets){if(s.Value==null||s.Value.IsDisposed){problems.Add(s.Key+": not open (no access or not loaded)");continue;}
   try{var doc=await s.Value.ReportForBundle();if(doc==null||doc.Sheets.Count==0){problems.Add(s.Key+": nothing to report yet");continue;}parts.Add(new KeyValuePair<string,CalculationDocument>(s.Key,doc));}
   catch(Exception ex){problems.Add(s.Key+": "+ex.Message);}}
  /* contents: pages of each sheet */
  var rows=new List<string[]>();int pageNo=2,no=1;foreach(var p in parts){rows.Add(new[]{(no++).ToString(),p.Key,pageNo+(p.Value.Sheets.Count>1?" - "+(pageNo+p.Value.Sheets.Count-1):"")});pageNo+=p.Value.Sheets.Count;}
  var missing=problems.ToList();
  all.Sheets.Add(g=>{CalculationDocument.Section(g,"CONTENTS - DESIGN LOADS (NSCP 2015)",130);CalculationDocument.Table(g,new[]{"No.","Design sheet","Pages"},rows,172,new float[]{40,330,110},30);
   if(missing.Count>0)CalculationDocument.Text(g,"Not included: "+string.Join("; ",missing)+".",CalculationDocument.L,172+30*(rows.Count+1)+16,CalculationDocument.CW,120);});all.PageTitles.Add("Design Loads - NSCP 2015");
  foreach(var p in parts){all.Own(p.Value);var title=string.IsNullOrEmpty(p.Value.Title)?p.Key:p.Value.Title;foreach(var sheet in p.Value.Sheets){all.Sheets.Add(sheet);all.PageTitles.Add(title);}}
  return all;}
}
}
