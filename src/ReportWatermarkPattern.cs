using System.Collections.Generic;
namespace BeamScheduleFormatter {
// Staggered rows form an equilateral triangular lattice. Coordinates are A4 points, from top left.
static class ReportWatermarkPattern {
 internal const double Opacity=.09,Side=180;
 internal static IEnumerable<double[]> Tiles(){for(int row=0;row<6;row++)for(int col=0;col<4;col++)yield return new[]{-100+col*230+(row%2)*115.0,-70+row*199.1858429,Side};}
}
class ReportPaintCommand {internal string Kind,Text,Color;internal double X,Y,W,H,Size;internal bool Bold;}
}
