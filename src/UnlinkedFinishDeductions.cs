using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* Wall finishes without a Wall ID (owner, 20 November 2026: "iminus na lang sa plaster at paint yung mga wall tiles and finishes?";
   "check mo if ano ang maganda. sa lumang set up pwede kasing madaming wall ID sa isang wall finish"). Since v5.1.3 a wall finish
   row is not tied to one wall, so its area cannot be taken off a particular wall face. It is taken off the project totals instead:
   - Tiles and Other finishes: off the net plaster and off the masonry paint (as the linked tiles always were);
   - Paint finishes: off the masonry paint only (the paint is that finish; the plaster stays under it).
   The deduction is shared over the walls in proportion to their plaster / paint area, so every wall source in the BOQ stays
   positive and the totals are right. Rows that still carry a Wall ID (older projects) keep their per-wall deduction. */
partial class ProgramOfWorksForm {
 internal double UnlinkedPlasterDeduction,UnlinkedPaintDeduction;
 void ApplyUnlinkedFinishDeductions(List<string> errors){UnlinkedPlasterDeduction=UnlinkedPaintDeduction=0;
  foreach(var f in finishItems.Where(x=>x.Category=="Wall"&&string.IsNullOrWhiteSpace(x.Wall))){double area;try{area=f.Total(null,wallOpenings,finishItems);}catch(Exception){continue;}if(area<=0)continue;
   if(string.Equals(f.Kind,"Paint",StringComparison.OrdinalIgnoreCase))UnlinkedPaintDeduction+=area;else{UnlinkedPlasterDeduction+=area;UnlinkedPaintDeduction+=area;}}
  Share(plasterResults,UnlinkedPlasterDeduction,"net plaster",errors,15);Share(paintResults,UnlinkedPaintDeduction,"masonry paint",errors,17);}
 void Share(Dictionary<string,PlasterTakeoff> results,double deduction,string what,List<string> errors,int column){if(deduction<=0||results.Count==0)return;double total=results.Values.Sum(p=>p.Net);if(total<=0)return;
  if(deduction>total+1e-9)errors.Add("Wall finishes without a Wall ID ("+deduction.ToString("0.00",CultureInfo.InvariantCulture)+" m2) exceed the "+what+" ("+total.ToString("0.00",CultureInfo.InvariantCulture)+" m2); "+what+" counted as 0.");
  double keep=Math.Max(0,total-deduction)/total;
  foreach(var pair in results){var p=pair.Value;double before=p.Net;p.Net=Math.Round(before*keep,6);if(column==15)p.TileDeduction+=before-p.Net;
   foreach(var row in MasonryGrids().SelectMany(g=>g.Rows.Cast<DataGridViewRow>()).Where(r=>!r.IsNewRow&&string.Equals(Cell(r,0),pair.Key,StringComparison.OrdinalIgnoreCase))){
    row.Cells[column].Value=p.Net.ToString("0.00",CultureInfo.InvariantCulture);if(column==15)row.Cells[14].Value=p.TileDeduction.ToString("0.00",CultureInfo.InvariantCulture);
    row.Cells[column].ToolTipText="Includes a share of the wall finishes without a Wall ID ("+(before-p.Net).ToString("0.00",CultureInfo.InvariantCulture)+" m2)";}}}
}
}
