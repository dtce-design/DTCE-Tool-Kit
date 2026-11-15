using System;
namespace BeamScheduleFormatter {
class TerrainProfileScale {
 internal double First,Last,CenterZ,XScale,YScale,Left,Top,Width,Height,Exaggeration;
 internal static TerrainProfileScale Create(double first,double last,double minZ,double maxZ,double left,double top,double width,double height,double exaggeration=1){if(!WindTin.Finite(first)||!WindTin.Finite(last)||!WindTin.Finite(minZ)||!WindTin.Finite(maxZ)||last<=first||width<=0||height<=0||exaggeration<=0||!WindTin.Finite(exaggeration))throw new ArgumentException("Invalid terrain plotting scale.");double scale=Math.Min(width/(last-first),height/Math.Max(1,(maxZ-minZ)*1.24*exaggeration));return new TerrainProfileScale{First=first,Last=last,CenterZ=(minZ+maxZ)/2,XScale=scale,YScale=scale*exaggeration,Left=left,Top=top,Width=width,Height=height,Exaggeration=exaggeration};}
 internal double X(double distance){return Left+Width/2+(distance-(First+Last)/2)*XScale;}
 internal double Y(double elevation){return Top+Height/2-(elevation-CenterZ)*YScale;}
 internal double TickZ(double fraction){return CenterZ+(fraction-.5)*Height/YScale;}
}
}
