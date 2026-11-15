using System.Drawing;
namespace BeamScheduleFormatter {
// Keep keyboard focus visible without the old dotted rectangle.
static class FocusHighlight {
 internal static void Draw(Graphics graphics,Rectangle bounds,Color color){
  if(bounds.Width<2||bounds.Height<2)return;
  using(var pen=new Pen(color,1.5f))using(var path=ModernUi.Rounded(bounds,3))graphics.DrawPath(pen,path);
 }
}
}
