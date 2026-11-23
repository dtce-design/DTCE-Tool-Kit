using System;using System.Collections.Generic;using System.Linq;
namespace BeamScheduleFormatter {
// Millimetres; y points upwards. Sharp-corner idealisation matches the estimator's plates.
static class SteelSectionGeometry {
 internal static List<double[]> Plates(SteelProfile p,bool pair,double backToBack=0){
  if(p==null)throw new ArgumentException("Select a catalog section.");
  double h=p.H,b=p.B,t=p.T,c=p.Lip;
  if(!(h>0&&b>0&&t>0)||double.IsInfinity(h+b+t)||double.IsNaN(h+b+t))throw new ArgumentException("Invalid section dimensions.");
  var r=new List<double[]>();
  if(p.Kind=="Purlin"){
   if(h<=2*c||b<=2*t||c<=t)throw new ArgumentException("Section dimensions do not fit.");
   if(p.Name.StartsWith("Z",StringComparison.OrdinalIgnoreCase)){
    r.Add(new[]{-t/2,0.0,t,h});r.Add(new[]{t/2,h-t,b-t,t});r.Add(new[]{-b+t/2,0.0,b-t,t});r.Add(new[]{b-1.5*t,h-c,t,c-t});r.Add(new[]{-b+t/2,t,t,c-t});
   }else{r.Add(new[]{0.0,0.0,t,h});r.Add(new[]{t,0.0,b-t,t});r.Add(new[]{t,h-t,b-t,t});r.Add(new[]{b-t,t,t,c-t});r.Add(new[]{b-t,h-c,t,c-t});}
  }else if(p.Kind=="Angle bar"){
   if(t>=Math.Min(h,b))throw new ArgumentException("Section dimensions do not fit.");
   r.Add(new[]{0.0,0.0,t,h});r.Add(new[]{t,0.0,b-t,t});
   if(pair){/* back to back at the gusset plate thickness (owner, 23 November 2026) */double gap=backToBack>0?backToBack:Math.Max(2*t,10);var mirror=r.Select(v=>new[]{-gap-v[0]-v[2],v[1],v[2],v[3]}).ToArray();r.AddRange(mirror);}
  }else if(p.Kind=="HSS rectangle"){
   if(2*t>=Math.Min(h,b))throw new ArgumentException("Section dimensions do not fit.");
   r.Add(new[]{0.0,0.0,b,t});r.Add(new[]{0.0,h-t,b,t});r.Add(new[]{0.0,t,t,h-2*t});r.Add(new[]{b-t,t,t,h-2*t});
  }else if(p.Kind!="HSS circle / pipe")throw new ArgumentException("Unsupported section family.");
  return r;
 }
}
}
