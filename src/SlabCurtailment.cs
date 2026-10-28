using System;
namespace BeamScheduleFormatter {
// Project reference: Screenshot 2026-10-27 213744. Locations, not automatic anchorage lengths.
static class SlabCurtailment {
 public static double BottomOffset(double span){Validate(span);return Up25(span/6);}
 public static double BottomMiddleBody(double span){Validate(span);return span-2*BottomOffset(span);}
 public static double TopPanelExtension(double span){Validate(span);return Up25(span/4);}
 public static double TopContinuousBody(double leftSpan,double rightSpan,double supportWidth){Validate(leftSpan);Validate(rightSpan);if(double.IsNaN(supportWidth)||double.IsInfinity(supportWidth)||supportWidth<0)throw new ArgumentException("Support width must be nonnegative.");return TopPanelExtension(leftSpan)+supportWidth+TopPanelExtension(rightSpan);}
 public static double Up25(double metres){Validate(metres);return Math.Ceiling(Math.Round(metres*1000/25,10))*25/1000;}
 static void Validate(double span){if(double.IsNaN(span)||double.IsInfinity(span)||span<=0)throw new ArgumentException("Enter a positive slab span.");}
}
}
