using System;using System.Linq;using System.Collections.Generic;
namespace BeamScheduleFormatter {
class WindContourSegment {public double Lat1,Lon1,Lat2,Lon2,Speed;}
static partial class WindTin {
 static readonly Dictionary<string,List<WindContourSegment>> contourCache=new Dictionary<string,List<WindContourSegment>>();
 public static List<WindContourSegment> Contours(string category){Load();lock(gate){List<WindContourSegment> lines;if(!contourCache.TryGetValue(category,out lines)){if(!surfaces.Any(s=>s.Category==category))throw new ArgumentException("Unknown wind category.");lines=WindSourceContours.Load(category,transform);contourCache[category]=lines;}return lines;}}
}
static class WindCircleGeometry {
 public static List<double[]> Sector(double lat,double lon,double radius,int direction){if(!WindTin.Finite(radius)||radius<=0||direction<0||direction>7)throw new ArgumentException("Invalid sector.");var points=new List<double[]>{new[]{lat,lon}};for(int n=0;n<=18;n++)points.Add(WindAssessment.Destination(lat,lon,direction*45-22.5+n*2.5,radius));return points;}
}
static class WindContourGeometry {
 public static List<WindContourSegment> Build(WindSurface surface,double[] transform){var result=new List<WindContourSegment>();foreach(var face in surface.Faces){var nodes=face.Select(i=>surface.Nodes[i]).ToArray();double lo=nodes.Min(n=>n.V),hi=nodes.Max(n=>n.V);if(hi-lo<1e-8)continue;for(double level=Math.Ceiling(lo/10)*10;level<=hi+1e-8;level+=10){var hits=new List<double[]>();for(int e=0;e<3;e++){var a=nodes[e];var b=nodes[(e+1)%3];if(Math.Abs(b.V-a.V)<1e-8)continue;double t=(level-a.V)/(b.V-a.V);if(t< -1e-8||t>1+1e-8)continue;double x=a.X+t*(b.X-a.X),y=a.Y+t*(b.Y-a.Y);if(!hits.Any(p=>Math.Abs(p[0]-x)+Math.Abs(p[1]-y)<1e-6))hits.Add(new[]{x,y});}if(hits.Count==2){var a=Geo(hits[0],transform);var b=Geo(hits[1],transform);result.Add(new WindContourSegment{Lat1=a[0],Lon1=a[1],Lat2=b[0],Lon2=b[1],Speed=level});}}}return result;}
 public static double[] Geo(double[] point,double[] transform){double lon=(point[0]-transform[1])/transform[0]+121,merc=(point[1]-transform[3])/transform[2]+13,lat=(2*Math.Atan(Math.Exp(merc*Math.PI/180))-Math.PI/2)*180/Math.PI;return new[]{lat,lon};}
}
}
