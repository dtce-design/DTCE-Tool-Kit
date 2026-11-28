using System;using System.Collections.Generic;using System.IO;using System.Text;using System.Text.RegularExpressions;
namespace BeamScheduleFormatter {
/* the STAAD output file (.ANL) read directly (owner, 28 November 2026: "joint displacement beam end forces i open ko na lang yung ANL
   file tapos bahala na yung app magopen magread"; "para di ko na copy paste"): only the JOINT DISPLACEMENT and MEMBER END FORCES
   blocks are kept - the rest of the output (input echo, reactions, story drift ...) is left out, so a large .ANL stays small in the sheet */
static class StaadAnl {
 static readonly Regex Disp=new Regex(@"JOINT\s+DISPLACEMENT",RegexOptions.IgnoreCase),Forces=new Regex(@"MEMBER\s+END\s+FORCES",RegexOptions.IgnoreCase);
 /* any other result block title ends the current block */
 static readonly Regex Other=new Regex(@"SUPPORT\s+REACTION|STORY\s+DRIFT|STORY\s+STIFFNESS|CENT(ER|RE)\s+OF\s+(RIGIDITY|GRAVITY|MASS)|DIAPHRAGM|MEMBER\s+FORCES\s+(AT|ALONG)|SECTION\s+FORCES|MEMBER\s+STRESSES|ENVELOPE|MODE\s+SHAPE|PARTICIPATION|EIGEN|FREQUENC|STATIC\s+LOAD|LOAD\s+LIST|\bPRINT\b|\bFINISH\b|STAAD\.?PRO\s+.*(ERROR|WARNING)|\bEND\s+OF\s+STAAD",RegexOptions.IgnoreCase);
 internal static void Split(string text,out string displacements,out string forces){var d=new StringBuilder();var f=new StringBuilder();StringBuilder cur=null;
  foreach(var raw in (text??"").Replace("\r","").Split('\n')){
   if(Disp.IsMatch(raw)&&!Regex.IsMatch(raw,@"^\s*\d+\.\s")){cur=d;cur.Append(raw.Trim()).Append("\r\n");continue;}
   if(Forces.IsMatch(raw)&&!Regex.IsMatch(raw,@"^\s*\d+\.\s")){cur=f;cur.Append(raw.Trim()).Append("\r\n");continue;}
   /* input echo lines ("  123. PRINT ...") and other result blocks end a block */
   if(Regex.IsMatch(raw,@"^\s*\d+\.\s+\S")||Other.IsMatch(raw)){cur=null;continue;}
   if(cur!=null&&raw.Trim()!="")cur.Append(raw.TrimEnd()).Append("\r\n");}
  displacements=d.ToString();forces=f.ToString();}
 internal static string Read(string path){using(var s=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))using(var r=new StreamReader(s,Encoding.Default,true))return r.ReadToEnd();}
}
}
