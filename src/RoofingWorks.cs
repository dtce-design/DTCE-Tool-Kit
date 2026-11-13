using System;using System.Linq;using System.Text;using System.Collections.Generic;using System.Globalization;using System.Windows.Forms;using System.Drawing;
namespace BeamScheduleFormatter {
class RoofSheetQuantity {
 public int Columns,Rows,Pieces;public double CoveredArea,BuyArea,SheetLength,Insulation;
 public static RoofSheetQuantity Calculate(double slope,double width,int quantity,double sheetWidth,double effective,double stock,double lap,bool insulated){RoofMath.Positive(slope,width,sheetWidth,effective);if(quantity<1||quantity>100000||effective>sheetWidth)throw new Exception("Check quantity and widths: effective cover cannot exceed full sheet width.");if(stock==0)stock=slope;RoofMath.Positive(stock);if(double.IsNaN(lap)||double.IsInfinity(lap)||lap<0||lap>=stock)throw new Exception("End lap must be nonnegative and less than sheet length.");int columns=RoofMath.Ceil(width/effective),rows=slope<=stock?1:RoofMath.Ceil((slope-lap)/(stock-lap));int pcs=checked(columns*rows*quantity);return new RoofSheetQuantity{Columns=columns,Rows=rows,Pieces=pcs,SheetLength=stock,CoveredArea=slope*width*quantity,BuyArea=pcs*stock*sheetWidth,Insulation=insulated?slope*width*quantity:0};}
}
class RoofAccessoryQuantity {
 public int Pieces;public double BuyLength,BuyArea,NetLength;
 public static RoofAccessoryQuantity Calculate(double run,int quantity,double developedWidth,double stock,double lap){RoofMath.Positive(run,stock);if(double.IsNaN(developedWidth)||double.IsInfinity(developedWidth)||developedWidth<0)throw new Exception("Developed width cannot be negative.");if(quantity<1||quantity>100000||double.IsNaN(lap)||double.IsInfinity(lap)||lap<0||lap>=stock)throw new Exception("Check quantity and lap; lap must be less than piece length.");int pieces=checked((run<=stock?1:RoofMath.Ceil((run-lap)/(stock-lap)))*quantity);return new RoofAccessoryQuantity{Pieces=pieces,BuyLength=pieces*stock,BuyArea=pieces*stock*developedWidth,NetLength=run*quantity};}
}
static class RoofMath {public static void Positive(params double[] values){foreach(double v in values)if(double.IsNaN(v)||double.IsInfinity(v)||v<=0||v>1000000)throw new Exception("Enter positive finite dimensions.");}public static int Ceil(double v){if(v>1000000)throw new Exception("Quantity exceeds supported limit.");return Math.Max(1,checked((int)Math.Ceiling(v-1e-10)));}}

}

