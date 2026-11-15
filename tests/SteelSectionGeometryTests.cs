using System;using System.Linq;using BeamScheduleFormatter;
class SteelSectionGeometryTests {
 static int checks;static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
 static double Area(SteelProfile p,bool pair){return SteelSectionGeometry.Plates(p,pair).Sum(r=>r[2]*r[3]);}
 static void Main(){
  foreach(var p in SteelCatalog.Profiles.Where(p=>p.Kind=="Purlin"||p.Kind=="Angle bar"||p.Kind.StartsWith("HSS"))){
   var plates=SteelSectionGeometry.Plates(p,false);
   if(p.Kind=="HSS circle / pipe"){Check(plates.Count==0,"Pipe drawn as concentric circles");continue;}
   Check(plates.All(r=>r.All(v=>!double.IsNaN(v)&&!double.IsInfinity(v))&&r[2]>0&&r[3]>0),"Finite plates: "+p.Name);
   Check(Math.Abs(plates.Max(r=>r[1]+r[3])-plates.Min(r=>r[1])-p.H)<1e-7,"Depth: "+p.Name);
   for(int i=0;i<plates.Count;i++)for(int j=i+1;j<plates.Count;j++){
    var a=plates[i];var b=plates[j];double overlapX=Math.Min(a[0]+a[2],b[0]+b[2])-Math.Max(a[0],b[0]);double overlapY=Math.Min(a[1]+a[3],b[1]+b[3])-Math.Max(a[1],b[1]);
    Check(overlapX<1e-7||overlapY<1e-7,"No double-counted plate: "+p.Name);
   }
   if(p.Kind=="Angle bar")Check(Math.Abs(Area(p,true)-2*(p.H+p.B-p.T)*p.T)<1e-6,"Mirrored pair doubles area");
   if(p.Kind=="Purlin"){
    double expected=p.H*p.T+2*(p.B-p.T)*p.T+2*(p.Lip-p.T)*p.T;
    Check(Math.Abs(Area(p,false)-expected)<1e-6,"Matches section-properties plate area: "+p.Name);
    bool z=p.Name.StartsWith("Z");Check(z?plates.Min(r=>r[0])<0:plates.Min(r=>r[0])==0,"C versus Z orientation");
   }
  }
  Console.WriteLine(checks+" steel section geometry checks passed.");
 }
}
