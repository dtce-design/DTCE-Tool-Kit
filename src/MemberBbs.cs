using System;using System.Collections.Generic;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
/* Columns, slabs and footings in the bar bending schedule (owner, 20 November 2026: "bakit sa bar bending schedule walang column,
   slab, foundation?"). The bars come from the Program of Works take-off (the same cut lengths and counts as the estimate):
   column main bars, hoops, crossties and open ties; slab bottom, top and distribution bars; isolated and wall footing bars. */
class BbsMemberSource {public string Kind="",Id="",Description="";public int Quantity=1;public double WidthMm,DepthMm;public EstimateInput Steel;}
static class MemberBbs {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 internal static readonly string[] Kinds={"Column","Slab","Footing","Wall footing"};
 static string Code(string kind){switch(kind){case "Column":return "C";case "Slab":return "S";case "Footing":return "F";default:return "WF";}}
 /* bar pieces of the members, marked <kind code><member id>-<n> (e.g. C:C1-1), shapes from the take-off part */
 public static List<CutPiece> Pieces(IEnumerable<BbsMemberSource> sources,List<string> notes){var list=new List<CutPiece>();var used=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  foreach(var m in sources){if(m.Steel==null||m.Steel.Result==null)continue;int n=0;string prefix=Code(m.Kind)+":"+m.Id;int qty=Math.Max(1,m.Quantity);
   Func<string> mark=()=>{string k;do{n++;k=prefix+"-"+n.ToString(Inv);}while(!used.Add(k));return k;};
   foreach(var l in m.Steel.Result.Lines){if(l.Diameter<=0)continue;string part=l.Part??"";
    bool hoop=part.StartsWith("Stirrups",StringComparison.OrdinalIgnoreCase)||part.IndexOf("hoop",StringComparison.OrdinalIgnoreCase)>=0||part.IndexOf("tie",StringComparison.OrdinalIgnoreCase)>=0&&!part.StartsWith("Column open ties",StringComparison.OrdinalIgnoreCase);
    if(hoop&&l.MainHoopCut>0){int sets=l.TieSetsPerBeam>0?l.TieSetsPerBeam:l.PiecesPerBeam;if(sets<=0)continue;
     list.Add(new CutPiece{Group=m.Kind,Line=m.Kind+" "+m.Id,BarMark=mark(),Location=part+" (closed hoop)",Diameter=l.Diameter,Grade=l.Grade,PerLine=sets,Lines=qty,Length=l.MainHoopCut,Shape="Hoop, 135° hooks",SecB=m.WidthMm,SecH=m.DepthMm});
     if(l.AdditionalLegs>0&&l.AdditionalLegCut>0)list.Add(new CutPiece{Group=m.Kind,Line=m.Kind+" "+m.Id,BarMark=mark(),Location=part+" (crossties)",Diameter=l.Diameter,Grade=l.Grade,PerLine=sets*l.AdditionalLegs,Lines=qty,Length=l.AdditionalLegCut,Shape="Crosstie 90°/135°",SecB=m.WidthMm,SecH=m.DepthMm});continue;}
    if(l.PiecesPerBeam<=0||l.CutLength<=0)continue;
    bool open=part.StartsWith("Column open ties",StringComparison.OrdinalIgnoreCase);string dir=open&&part.EndsWith("Depth",StringComparison.OrdinalIgnoreCase)?"Depth":"Width";
    list.Add(new CutPiece{Group=m.Kind,Line=m.Kind+" "+m.Id,BarMark=mark(),Location=part,Diameter=l.Diameter,Grade=l.Grade,PerLine=l.PiecesPerBeam,Lines=qty,Length=l.CutLength,Shape=open?"Crosstie 135°":m.Kind=="Column"&&part.StartsWith("Column longitudinal")?"Cranked lap splice":"Straight",SecB=m.WidthMm,SecH=open?(dir=="Depth"?m.WidthMm:m.DepthMm):m.DepthMm});}
   if(n==0&&notes!=null)notes.Add(m.Kind+" "+m.Id+": no bars in the take-off.");}
  return list;}
}
}
