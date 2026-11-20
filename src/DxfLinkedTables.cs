using System;using System.Collections.Generic;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
// Owner (19 November 2026, "alisin yung naka link na table sa dxf"): the beam schedule DXF of a project carried an AutoCAD
// table linked to the Excel file of another project (data link to "...\2023 - Lal-lo - Centro - Evacuation Center\Schedule of
// Beams.xlsx") with different beam sizes. The app never reads it (schedules are read from the drawn TEXT / MTEXT and LINE
// entities of model space), but the copy kept in the beam / slab estimate of a Program of Works still held the whole table.
// Remove() takes out AutoCAD tables and data links from that copy: ACAD_TABLE entities, their *T display blocks and the
// DATALINK / TABLECONTENT / TABLEGEOMETRY objects. Every other line stays exactly as it was, and the result is only used
// when the drawn schedule entities read back the same. The original file under Project files is not changed.
static class DxfLinkedTables {
 static readonly HashSet<string> LinkObjects=new HashSet<string>(StringComparer.OrdinalIgnoreCase){"DATALINK","TABLECONTENT","TABLEGEOMETRY"};
 internal static string Remove(string raw){int removed;return Remove(raw,out removed);}
 /* removed = number of tables, table blocks and data-link objects taken out */
 internal static string Remove(string raw,out int removed){removed=0;
  if(string.IsNullOrEmpty(raw)||!Mentions(raw))return raw;
  var lines=raw.Split('\n');int pairs=lines.Length/2;if(pairs<2)return raw;
  var codes=new int[pairs];for(int i=0;i<pairs;i++)if(!int.TryParse(lines[2*i].Trim(),NumberStyles.Integer,CultureInfo.InvariantCulture,out codes[i]))return raw;
  var keep=new List<string>(lines.Length);string section="";bool skip=false,tableBlock=false,endingBlock=false;int count=0;
  for(int i=0;i<pairs;i++){string value=lines[2*i+1].TrimEnd('\r').Trim();
   if(codes[i]==0){
    if(tableBlock){if(endingBlock){tableBlock=false;endingBlock=false;}else if(value=="ENDBLK")endingBlock=true;}
    if(!tableBlock){skip=false;
     if(value=="SECTION")section=i+1<pairs&&codes[i+1]==2?lines[2*i+3].TrimEnd('\r').Trim().ToUpperInvariant():"";
     else if(value=="ENDSEC")section="";
     else if(section=="BLOCKS"&&value=="BLOCK"&&IsTableBlock(lines,codes,i)){tableBlock=true;count++;}
     else if((section=="ENTITIES"||section=="BLOCKS")&&value=="ACAD_TABLE"){skip=true;count++;}
     else if(section=="OBJECTS"&&LinkObjects.Contains(value)){skip=true;count++;}}}
   if(skip||tableBlock)continue;keep.Add(lines[2*i]);keep.Add(lines[2*i+1]);}
  if(count==0)return raw;
  for(int i=pairs*2;i<lines.Length;i++)keep.Add(lines[i]);
  string result=string.Join("\n",keep);
  /* the drawn schedule must read back exactly as before (everything but the tables themselves) */
  try{if(!SameModel(raw,result))return raw;}catch(Exception){return raw;}
  removed=count;return result;}
 static bool Mentions(string raw){foreach(string word in new[]{"ACAD_TABLE","DATALINK","TABLECONTENT","TABLEGEOMETRY"})if(raw.IndexOf(word,StringComparison.Ordinal)>=0)return true;return false;}
 /* AutoCAD keeps the graphics of a table in an anonymous block named *T<number> */
 static bool IsTableBlock(string[] lines,int[] codes,int start){for(int j=start+1;j<codes.Length&&codes[j]!=0;j++)if(codes[j]==2){string name=lines[2*j+1].TrimEnd('\r').Trim();return name.Length>2&&name[0]=='*'&&(name[1]=='T'||name[1]=='t')&&name.Skip(2).All(char.IsDigit);}return false;}
 static bool SameModel(string before,string after){var a=Dxf.Parse(before).ModelEntities().Where(e=>e.Type!="ACAD_TABLE").ToList();var b=Dxf.Parse(after).ModelEntities();if(a.Count!=b.Count)return false;
  for(int i=0;i<a.Count;i++){if(a[i].P.Count!=b[i].P.Count)return false;for(int k=0;k<a[i].P.Count;k++)if(a[i].P[k].Code!=b[i].P[k].Code||a[i].P[k].Value!=b[i].P[k].Value)return false;}return true;}
}
}
