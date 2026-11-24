using System;using System.Collections.Generic;using System.Linq;using System.Text;
namespace BeamScheduleFormatter {
/* STAAD reads an input line up to column 79 (owner, 24 November 2026: "fix input width 72col para pwede din sa 79"): every generated
   STAAD line is kept to 72 columns. A long command is split at a space and continued with " -" at the end of the line (STAAD's
   continuation mark); a long comment is wrapped onto further "*" lines. */
static class StaadLines {
 internal const int Width=72;
 internal static string Wrap(string text){if(string.IsNullOrEmpty(text))return text;bool crlf=text.Contains("\r\n");var output=new List<string>();
  foreach(var raw in text.Replace("\r\n","\n").Split('\n')){var line=raw.TrimEnd();if(line.Length<=Width){output.Add(line);continue;}
   if(line.StartsWith("*")){var words=line.Substring(1).Trim().Split(' ').Where(w=>w!="").ToList();var cur=new StringBuilder("*");
    foreach(var w in words){if(cur.Length>1&&cur.Length+1+w.Length>Width){output.Add(cur.ToString());cur=new StringBuilder("*");}if(cur.Length+1+w.Length>Width&&cur.Length==1){/* a single very long word */for(int i=0;i<w.Length;i+=Width-2)output.Add("* "+w.Substring(i,Math.Min(Width-2,w.Length-i)));continue;}cur.Append(' ').Append(w);}
    if(cur.Length>1)output.Add(cur.ToString());continue;}
   var parts=line.Split(' ').Where(w=>w!="").ToList();var row=new StringBuilder();
   foreach(var w in parts){if(row.Length>0&&row.Length+1+w.Length>Width-2){output.Add(row.ToString()+" -");row=new StringBuilder();}if(row.Length>0)row.Append(' ');row.Append(w);}
   if(row.Length>0)output.Add(row.ToString());}
  return string.Join(crlf?"\r\n":"\n",output);}
}
}
