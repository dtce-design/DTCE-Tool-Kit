using System;using System.Collections.Generic;using System.Linq;
namespace BeamScheduleFormatter {
/* A step-by-step text calculation laid out as a report (owner, 23 November 2026: "complete calculation lahat sa design sheets reports,
   including diagrams, illustrations, everything"; "maganda sa mata"): "## NN  TITLE" lines become numbered sections, each step written
   as a label line followed by its indented formula becomes a row of a "Step | Formula, values and result" table, other lines become
   paragraphs, a form feed starts a new page, and the caller adds its figures after any section. Nothing is left out. */
static class RichText {
 internal static void Add(RichReport rep,string text,Action<string,RichReport> afterHeading=null,int skipTitleLines=0){
  var lines=(text??"").Replace("\r","").Split('\n').ToList();var pending=new List<string[]>();string current=null;
  Action flush=()=>{if(pending.Count>0){rep.Table(new[]{"Step","Formula, values and result"},pending.ToList(),new[]{1.6f,3.4f});pending.Clear();}};
  for(int k=0;k<lines.Count;k++){string line=lines[k];if(k<skipTitleLines)continue;string t=line.Trim();
   if(t.Length==0)continue;if(t.Trim('-','=').Length==0)continue;/* ASCII rulers */
   if(t=="\f"||line.StartsWith("\f")){flush();rep.PageBreak();t=t.TrimStart('\f').Trim();if(t.Length==0)continue;}
   if(t.StartsWith("## ")){flush();if(current!=null&&afterHeading!=null)afterHeading(current,rep);current=t.Substring(3).Trim();rep.Heading(current);continue;}
   bool next=k+1<lines.Count&&lines[k+1].StartsWith("  ")&&lines[k+1].Trim().Length>0&&!line.StartsWith("  ");
   if(next){var formula=new List<string>();int j=k+1;while(j<lines.Count&&lines[j].StartsWith("  ")&&lines[j].Trim().Length>0){formula.Add(lines[j].Trim());j++;}pending.Add(new[]{t,string.Join("\n",formula)});k=j-1;continue;}
   flush();bool strong=t.Length<90&&t==t.ToUpperInvariant()&&t.Any(char.IsLetter);rep.Paragraph(t,false,strong);}
  flush();if(current!=null&&afterHeading!=null)afterHeading(current,rep);}
}
}
