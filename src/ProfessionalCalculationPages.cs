using System;using System.Drawing;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
sealed partial class CalculationDocument {
 internal static CalculationDocument FromEngineering(EngineeringReport report){
  var doc=new CalculationDocument(report.Title,true){ReportFile=report};
  foreach(var page in EngineeringQuantityPdf.Plan(report).DrawingPages){var commands=page.ToArray();doc.Sheets.Add(g=>{
   PdfBrand.Draw(g);
   foreach(var command in commands){
    if(command.Kind=="text"){
     using(var font=new Font("Arial",(float)command.Size,command.Bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Point))using(var brush=new SolidBrush(ReportColor(command.Color))){
      float ascent=(float)(command.Size*font.FontFamily.GetCellAscent(font.Style)/font.FontFamily.GetEmHeight(font.Style));
      g.DrawString(command.Text,font,brush,new PointF((float)command.X,(float)(841.89-command.Y)-ascent),StringFormat.GenericTypographic);
     }
    }else if(command.Kind=="fill"){using(var brush=new SolidBrush(ReportColor(command.Color)))g.FillRectangle(brush,(float)command.X,(float)(841.89-command.Y),(float)command.W,(float)command.H);}
    else if(command.Kind=="rule"){using(var pen=new Pen(Color.FromArgb(196,207,214),.5f))g.DrawLine(pen,36,(float)(841.89-command.Y),559.28f,(float)(841.89-command.Y));}
   }
  });}
  return doc;
 }
 static Color ReportColor(string rgb){var v=rgb.Split(' ').Select(s=>double.Parse(s,CultureInfo.InvariantCulture)).ToArray();return Color.FromArgb((int)(v[0]*255),(int)(v[1]*255),(int)(v[2]*255));}
}
static partial class EngineeringQuantityPdf {
 internal static void WriteFlattened(string path,EngineeringReport report){using(var document=CalculationDocument.FromEngineering(report))document.WritePdf(path);}
}
}
