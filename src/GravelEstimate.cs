using System;using System.Linq;using System.Collections.Generic;using System.Drawing;using System.Windows.Forms;
namespace BeamScheduleFormatter {
class GravelTakeoff {
 public static double Volume(double length,double width,double thicknessMm,int quantity){SteelWeight.Positive(length,width,thicknessMm);SteelWeight.Count(quantity);return length*width*thicknessMm/1000*quantity;}
}

}
