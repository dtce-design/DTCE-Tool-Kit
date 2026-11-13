using System;using System.Linq;using System.Text;using System.Globalization;using System.Collections.Generic;using System.Windows.Forms;
namespace BeamScheduleFormatter {
static class OpeningMoulding {
 public static double Length(string kind,double width,double height,int quantity,bool exterior){
  if(kind!="Door"&&kind!="Window")throw new Exception("Select Door or Window for moulding measurement.");
  SteelWeight.Positive(width,height);SteelWeight.Count(quantity);
  return exterior?(2*height+(kind=="Window"?2*width:width))*quantity:0;
 }
}

}

