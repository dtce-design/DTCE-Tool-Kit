using System;using System.Collections.Generic;using System.Linq;
namespace BeamScheduleFormatter {
// Metal painting area of structural steel (all exposed faces, both sides of thin elements), owner request 17 November 2026.
// Per metre: lipped C purlin 2H + 4B + 4·lip; angle 2(H + B) (outer H+B, inner (H−t)+(B−t), two edges t);
// flat bar 2(B + t); rectangular HSS 2(H + B) outside only; pipe π·D outside only. Dimensions in mm.
static class SteelPainting {
 internal static double AreaPerMetre(SteelProfile p){if(p==null)throw new ArgumentNullException("p");
  switch(p.Kind){
   case "Purlin":return (2*p.H+4*p.B+4*Math.Max(0,p.Lip))/1000;
   case "Angle bar":return 2*(p.H+p.B)/1000;
   case "Flat bar":return 2*(p.B+p.T)/1000;
   case "HSS rectangle":return 2*(p.H+p.B)/1000;
   case "HSS circle / pipe":return Math.PI*p.H/1000;
   default:throw new Exception("No painting basis for "+p.Kind+".");}}
 internal static double Area(SteelProfile p,double length,int quantity){SteelWeight.Positive(length);SteelWeight.Count(quantity);return AreaPerMetre(p)*length*quantity;}
}
partial class ProgramOfWorksForm {
 readonly List<KeyValuePair<string,double>> steelPaintAreas=new List<KeyValuePair<string,double>>();
 internal const string SteelPaintingTotal="Metal painting TOTAL (purlins, angle bars, metal fascia)";
 void AddSteelPaint(System.Windows.Forms.DataGridViewRow row,string profileName,double length,int quantity){var profile=FindSteelProfile(profileName);if(profile==null)return;string group=row.DataGridView==steelPurlins?"Purlins":row.DataGridView==steelAngles?"Angle bars":"Metal fascia";steelPaintAreas.Add(new KeyValuePair<string,double>(group,SteelPainting.Area(profile,length,quantity)));}
 IEnumerable<object[]> SteelPaintingRows(){foreach(string group in new[]{"Purlins","Angle bars","Metal fascia"}){double area=steelPaintAreas.Where(p=>p.Key==group).Sum(p=>p.Value);if(area>0)yield return new object[]{"Metal painting - "+group.ToLowerInvariant()+" (all faces)",area,"m2"};}yield return new object[]{SteelPaintingTotal,steelPaintAreas.Sum(p=>p.Value),"m2"};}
}
}
