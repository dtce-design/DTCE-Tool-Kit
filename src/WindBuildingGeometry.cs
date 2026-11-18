using System;
namespace BeamScheduleFormatter {
struct WindModelPoint {public double X,Y,Z;public WindModelPoint(double x,double y,double z){X=x;Y=y;Z=z;}}
static class WindBuildingGeometry {
 internal static WindModelPoint Rotate(double x,double y,double z,int ridge){double radians=ridge*Math.PI/4;return new WindModelPoint(x*Math.Cos(radians)+y*Math.Sin(radians),-x*Math.Sin(radians)+y*Math.Cos(radians),z);}
 internal static WindModelPoint[] Vertices(WindBuildingInput input){input.Validate();double w=input.Width/2,l=input.Length/2;return new[]{Rotate(-w,-l,0,input.RidgeDirection),Rotate(w,-l,0,input.RidgeDirection),Rotate(w,l,0,input.RidgeDirection),Rotate(-w,l,0,input.RidgeDirection),Rotate(-w,-l,input.Eave,input.RidgeDirection),Rotate(w,-l,input.Eave,input.RidgeDirection),Rotate(w,l,input.Eave,input.RidgeDirection),Rotate(-w,l,input.Eave,input.RidgeDirection),Rotate(0,-l,input.Apex,input.RidgeDirection),Rotate(0,l,input.Apex,input.RidgeDirection)};}
 internal static WindModelPoint ApexDatum(WindBuildingInput input){return Rotate(0,-input.Length/2,0,input.RidgeDirection);}
}
}
