using System.Collections.Generic;
namespace BeamScheduleFormatter {
// Staggered rows form an equilateral triangular lattice. Coordinates are A4 points, from top left.
static class ReportWatermarkPattern {
 internal const double Opacity=.18,Side=180;
 // Tiles actually drawn on reports: none in the Admin edition, the DPWH lattice in the User edition (owner, 18 Nov 2026).
 internal static IEnumerable<double[]> Active(){
#if ADMIN_APP
  return new double[0][];
#else
  return Tiles();
#endif
 }
 /* the same lattice and logo size continued over a larger sheet, e.g. A3 (owner, 18 November 2026: "same size lang, dadamihan lang") */
 internal static IEnumerable<double[]> Active(double widthPt,double heightPt){
#if ADMIN_APP
  return new double[0][];
#else
  return Tiles(widthPt,heightPt);
#endif
 }
 internal static IEnumerable<double[]> Tiles(double widthPt,double heightPt){for(int row=0;-70+row*199.1858429<heightPt;row++)for(int col=0;-100+col*230<widthPt+115;col++)yield return new[]{-100+col*230+(row%2)*115.0,-70+row*199.1858429,Side};}
 internal static IEnumerable<double[]> Tiles(){for(int row=0;row<6;row++)for(int col=0;col<4;col++)yield return new[]{-100+col*230+(row%2)*115.0,-70+row*199.1858429,Side};}
}
class ReportPaintCommand {internal string Kind,Text,Color;internal double X,Y,W,H,Size;internal bool Bold;}
}
