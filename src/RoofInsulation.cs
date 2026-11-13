using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
class InsulationRollQuantity {
 public double NetArea,RollArea,PurchaseArea;public int Rolls;
 public static InsulationRollQuantity Calculate(double area,double width,double length){RoofMath.Positive(width,length);if(double.IsNaN(area)||double.IsInfinity(area)||area<0)throw new Exception("Invalid insulation area.");double rollArea=width*length;int rolls=area==0?0:RoofMath.Ceil(area/rollArea);return new InsulationRollQuantity{NetArea=area,RollArea=rollArea,Rolls=rolls,PurchaseArea=rolls*rollArea};}
}

}

