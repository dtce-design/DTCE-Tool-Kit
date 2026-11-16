using System.Drawing.Printing;using System.Windows.Forms;
namespace BeamScheduleFormatter {
static class CalculationPreview {
 public static PrintDocument Document(string report){var document=CalculationDocument.FromText(report);var print=document.Printer();print.Disposed+=(s,e)=>document.Dispose();return print;}
 public static void Show(IWin32Window owner,string report){using(var document=CalculationDocument.FromText(report))CalculationReportView.ShowReport(owner,document);}
 internal static void Show(IWin32Window owner,EngineeringReport report){using(var document=CalculationDocument.FromEngineering(report))CalculationReportView.ShowReport(owner,document);}
}
}
