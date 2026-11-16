using System;using System.Text.RegularExpressions;
namespace BeamScheduleFormatter {
// POW beam-schedule input only. The separate Schedule Formatter keeps its own rules.
static class PowBeamMarks {
 static readonly Regex Pattern=new Regex(@"^(?:\d*[A-Z]+-?\d+[A-Z]?|LRB)$",RegexOptions.CultureInvariant);
 internal static string Name(string raw){
  raw=Regex.Replace(raw??"",@"\\U\+([0-9A-Fa-f]{4})",m=>((char)Convert.ToInt32(m.Groups[1].Value,16)).ToString());
  string text=RevisedSchedule.Plain(raw).ToUpperInvariant();
  text=text.Replace('\u2010','-').Replace('\u2011','-').Replace('\u2012','-').Replace('\u2013','-').Replace('\u2014','-').Replace('\u2212','-');
  return Regex.Replace(text,@"\s*-\s*","-").Trim();
 }
 internal static bool IsMark(string raw){return Pattern.IsMatch(Name(raw));}
}
}
