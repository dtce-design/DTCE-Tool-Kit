using System;using System.Collections.Generic;using System.Drawing;using System.Linq;using System.Text.RegularExpressions;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Bill of Quantities grouped by PART, following the owner's BACKUP / BOQ layout (18 November 2026):
// A Facilities for the Engineer, B Other General Requirements, C Pavement, D Slope Protection, E Earthworks,
// F Ground Improvement, G Piling, H Reinforced Concrete, I Finishing Works (I.1 Termite Control ... I.8 Roofing),
// J Plumbing / Sanitary, K Electrical, L Mechanical, M Water Supply, N Water Pumping. The part comes from the item
// number; 1201 (water pumping) stays in Part L when it follows Part L items, otherwise Part N.
static class BoqParts {
 internal sealed class Part {internal string Key,Title,Parent,ParentTitle;}
 static Part P(string key,string title,string parent=null,string parentTitle=null){return new Part{Key=key,Title=title,Parent=parent,ParentTitle=parentTitle};}
 const string Fin="FINISHING WORKS";
 static readonly Dictionary<int,Part> byNumber=new Dictionary<int,Part>();
 static BoqParts(){
  Action<Part,int[]> add=(p,codes)=>{foreach(int c in codes)byNumber[c]=p;};
  add(P("C","PAVEMENT CONSTRUCTION"),new[]{102,105,200,311,601});
  add(P("D","SLOPE PROTECTION WORKS"),new[]{103,404,506,510});
  add(P("E","EARTHWORKS"),new[]{800,801,802,803,804,807});
  add(P("F","GROUND IMPROVEMENT WORKS"),new[]{808,1049});
  add(P("G","PILING WORKS"),new[]{1052});
  add(P("H","REINFORCED CONCRETE WORKS"),new[]{900,901,902,903});
  add(P("I.1","TERMITE CONTROL WORKS","I",Fin),new[]{1000});
  add(P("I.2","CARPENTRY AND JOINERY WORKS","I",Fin),new[]{1003,1019});
  add(P("I.3","DOORS AND WINDOWS","I",Fin),new[]{1005,1006,1007,1008,1009,1010,1011,1012,1043,1044});
  add(P("I.4","FINISHING WORKS","I",Fin),new[]{1004,1016,1018,1020,1021,1025,1027,1034,1039,1051});
  add(P("I.5","PAINTING WORKS","I",Fin),new[]{1032});
  add(P("I.6","MASONRY WORKS","I",Fin),new[]{1046});
  add(P("I.7","STRUCTURAL STEEL AND METAL STRUCTURE ACCESSORIES","I",Fin),new[]{1047});
  add(P("I.8","ROOFING WORKS","I",Fin),new[]{1038,1013,1014});
  add(P("J","PLUMBING / SANITARY WORKS"),new[]{1001,1002});
  add(P("K","ELECTRICAL AND ELECTRONICS WORKS"),new[]{1100,1101,1102,1103,1104,1105,1106,1107});
  add(P("L","MECHANICAL WORKS"),new[]{1200,1202,1203,1208});
  add(P("M","WATER SUPPLY SYSTEM"),new[]{1600,1061,1602});}
 static readonly Part PartA=P("A","FACILITIES FOR THE ENGINEER"),PartB=P("B","OTHER GENERAL REQUIREMENTS"),PartN=P("N","WATER PUMPING SYSTEM"),Other=P("Z","OTHER ITEMS");
 internal static Part For(string code,Part previous){code=(code??"").Trim().ToUpperInvariant();if(code.StartsWith("A."))return PartA;if(code.StartsWith("B."))return PartB;
  var m=Regex.Match(code,@"^\d+");if(!m.Success)return previous??Other;int n=int.Parse(m.Value);
  if(n==1201)return previous!=null&&previous.Key=="L"?previous:PartN;Part p;return byNumber.TryGetValue(n,out p)?p:(previous??Other);}
}
partial class ProgramOfWorksForm {
 BoqParts.Part boqCurrentPart;
 // Adds the PART (and Part I sub-part) heading rows before the first pay item of each group.
 void AddBoqPartHeading(BackupBillItem item,List<DataGridViewRow> pending){var part=BoqParts.For(item.Code,boqCurrentPart);if(boqCurrentPart!=null&&part.Key==boqCurrentPart.Key)return;
  if(part.Parent!=null&&(boqCurrentPart==null||boqCurrentPart.Parent!=part.Parent))pending.Add(BoqHeadingRow("PART "+part.Parent,part.ParentTitle,true));
  pending.Add(BoqHeadingRow("PART "+part.Key,part.Title,part.Parent==null));boqCurrentPart=part;}
 DataGridViewRow BoqHeadingRow(string code,string title,bool major){var row=new DataGridViewRow();row.CreateCells(boqGrid,code,title,"","");row.ReadOnly=true;var style=row.DefaultCellStyle;style.BackColor=major?PremiumControls.AccentSoft:ModernUi.Soft;style.Font=new Font(boqGrid.Font,FontStyle.Bold);return row;}
}
}
