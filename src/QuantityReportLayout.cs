using System;using System.Collections.Generic;using System.Drawing;using System.Linq;
namespace BeamScheduleFormatter {
/* The Bill of Quantities calculation report laid out like the design-sheet reports (owner, 24 November 2026: "masmaayos na presentation
   ng boq report"): a cover table, contents, numbered sections, dark-header tables that continue across pages with their header, column
   widths by what each column holds, source-breakdown titles in bold, notes muted, and the signature lines. Nothing is dropped: every
   section, note and table of the report is placed in order. */
static class QuantityReportLayout {
 /* a column's share of the line: text columns wide, numbers and units narrow */
 static float Weight(string header){string h=(header??"").ToLowerInvariant();
  if(h.Contains("formula"))return 3f;if(h=="pieces"||h=="bar"||h.StartsWith("cut")||h=="kg/m"||h.EndsWith("(kg)"))return .75f;
  if(h.Contains("calculation")||h.Contains("basis")||h.Contains("inputs")||h.Contains("reused by")||h.Contains("opened for"))return 3.6f;
  if(h.Contains("description")||h.Contains("source")||h.Contains("material")||h.Contains("member")||h.Contains("input issue")||h.Contains("commercial"))return 2.6f;
  if(h=="unit"||h=="no."||h=="id"||h.Contains("stocks")||h.Contains("whole bars"))return .7f;
  if(h.Contains("pay item"))return 1f;
  if(h.Contains("quantity")||h.Contains("kg")||h.Contains("final offcut"))return .9f;return 1.2f;}
 internal static CalculationDocument Build(EngineeringReport report){var rep=new RichReport(string.IsNullOrWhiteSpace(report.Subtitle)?report.Title:report.Title+" - "+report.Subtitle);rep.Compact=true;
  var cover=new List<string[]>();if(!string.IsNullOrWhiteSpace(report.Project))cover.Add(new[]{"Project",report.Project});if(!string.IsNullOrWhiteSpace(report.Location))cover.Add(new[]{"Location",report.Location});if(!string.IsNullOrWhiteSpace(report.Office))cover.Add(new[]{"Implementing office",report.Office});
  cover.Add(new[]{"Report",report.Subtitle});cover.Add(new[]{"Status",report.Status});cover.Add(new[]{"Prepared",report.Generated.ToString("dd MMMM yyyy, HH:mm")});foreach(var h in report.Highlights)cover.Add(new[]{h.Key,h.Value});
  rep.Heading("REPORT").KeyValues(cover);
  var sections=report.Sections.Where(sx=>sx.Records.Count>0).ToList();
  if(sections.Count>1){rep.Heading("CONTENTS");rep.Table(new[]{"No.","Section","Tables"},sections.Select((sx,k)=>new[]{(k+1).ToString(),sx.Title,sx.Records.Count(r=>r.Kind=="table").ToString()}).ToList(),new[]{.5f,4.6f,.7f});}
  rep.Paragraph("Quantities are computed from the measurement inputs listed in this report. Source breakdowns and calculation records explain a total; they are not additional quantities to add to the Bill of Quantities.",true);
  for(int k=0;k<sections.Count;k++){var sx=sections[k];rep.Heading((k+1)+"  "+sx.Title.ToUpperInvariant());
   foreach(var r in sx.Records){
    if(r.Kind=="table"&&r.Headers!=null&&r.Rows!=null){if(r.Rows.Count==0)continue;var widths=r.Headers.Select(Weight).ToArray();rep.Table(r.Headers,r.Rows,widths,row=>row.Any(c=>c!=null&&(c.StartsWith("TOTAL",StringComparison.OrdinalIgnoreCase)||c.IndexOf("TO BUY",StringComparison.OrdinalIgnoreCase)>=0))?(Color?)Color.FromArgb(232,242,236):null);}
    else if(r.Kind=="note")rep.Paragraph(r.Text,true);
    else if(r.Kind=="equation")rep.Paragraph(r.Text,false,true);
    else if(!string.IsNullOrWhiteSpace(r.Text)){bool title=r.Text.EndsWith("source breakdown",StringComparison.OrdinalIgnoreCase)||r.Text.Length<70&&r.Text==r.Text.ToUpperInvariant();rep.Paragraph(r.Text,false,title);}}}
  if(sections.Count==0)rep.Paragraph("No measured quantities have been entered for this scope.",true);
  rep.Signatures("Prepared by","Checked by");return rep.Build();}
}
}
