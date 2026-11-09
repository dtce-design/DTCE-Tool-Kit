using System;using System.Collections.Generic;using System.Linq;using System.Text.RegularExpressions;
namespace BeamScheduleFormatter {
class MouldingTakeoff {
 public string Id,Description,Profile;public double Length;public int Quantity;
 public double Total {get{if(string.IsNullOrWhiteSpace(Id)||string.IsNullOrWhiteSpace(Description)||string.IsNullOrWhiteSpace(Profile))throw new Exception("Enter ID, description and moulding profile / size.");SteelWeight.Positive(Length);SteelWeight.Count(Quantity);return Length*Quantity;}}
 public static string Key(string profile){return Regex.Replace(Regex.Replace((profile??"").Trim().ToUpperInvariant().Replace('×','X'),@"\s*X\s*","X"),@"\s+","").Replace("MM","");}
}
}
