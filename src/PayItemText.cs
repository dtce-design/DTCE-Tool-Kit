using System.Text.RegularExpressions;
namespace BeamScheduleFormatter {
/* Pay-item descriptions as shown in the app (owner, 20 November 2026: "alisin mo yung (No Top Coat)"). Only the text on
   screen and in reports changes: the stored description (BACKUP workbook text, saved mapping identity) stays as it is, so
   linked workbooks and saved projects keep matching. */
static class PayItemText {
 static readonly Regex Hidden=new Regex(@"\s*\(\s*no\s+top\s*coat\s*\)",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
 internal static string Display(string description){return description==null?null:Hidden.Replace(description,"").Trim();}
}
}
