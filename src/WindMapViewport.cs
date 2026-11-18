using System;
namespace BeamScheduleFormatter {
static class WindMapViewport {
 internal static int ZoomForRadius(double latitude,double radius,int width,int height){if(!WindTin.Finite(latitude)||!WindTin.Finite(radius)||radius<=0)throw new ArgumentException("Invalid map extent.");double available=Math.Max(80,Math.Min(width-110,height-110)),value=Math.Log(available*156543.033928*Math.Cos(latitude*Math.PI/180)/(2*radius),2);return Math.Max(3,Math.Min(16,(int)Math.Floor(value)));}
 internal static double[] Pixel(double latitude,double longitude,double centerLatitude,double centerLongitude,int zoom,int width,int height){double size=256*(1<<zoom);Func<double,double> y=lat=>{double p=lat*Math.PI/180;return (1-Math.Log(Math.Tan(p)+1/Math.Cos(p))/Math.PI)/2*size;};return new[]{(longitude-centerLongitude)/360*size+width/2.0,y(latitude)-y(centerLatitude)+height/2.0};}
}
}
