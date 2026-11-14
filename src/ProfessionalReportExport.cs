using System;using System.IO;using System.Text;using System.Diagnostics;using System.Windows.Forms;using System.Drawing.Imaging;
namespace BeamScheduleFormatter {
static class ProfessionalReportExport {
 public static void Show(IWin32Window owner,EngineeringReport report){using(var dialog=new SaveFileDialog{Filter="Engineering report (*.html)|*.html",FileName="DTCE-Engineering-Report.html",OverwritePrompt=true})if(dialog.ShowDialog(owner)==DialogResult.OK){File.WriteAllText(dialog.FileName,report.Html(),new UTF8Encoding(false));Process.Start(new ProcessStartInfo(dialog.FileName){UseShellExecute=true});}}
 public static EngineeringReport FromText(string text){var p=ProjectInfoStore.Read();return ProfessionalReport.FromText(text,EstimatePdf.Heading(text),p.Name,p.Location,p.Office);}
}
partial class WindDesignForm {
 protected override EngineeringReport BuildEngineeringReport(){BuildReport();var p=ProjectInfoStore.Read();string image;using(var snapshot=siteMap.Snapshot())using(var output=new MemoryStream()){snapshot.Save(output,ImageFormat.Png);image=Convert.ToBase64String(output.ToArray());}return WindReportFile.Create(Number("Latitude"),Number("Longitude"),Value("Category"),Number("Height"),Number("Kd"),terrain,((CheckBox)Inputs["Reviewed"]).Checked,BuildingEnabled?Building():null,p.Name,p.Location,p.Office,image);}
}
partial class ProgramOfWorksForm {
 void ExportEngineeringReport(){
  Guard();Commit();var issues=WorksValidationIssues();if(issues.Count>0)throw new Exception("Review the input issues before exporting final quantities.");UpdateDashboard();var p=ProjectInfoStore.Read();var report=ProfessionalReport.Create("Quantity take-off report",p.Name,p.Location,p.Office);report.Subtitle="Bill of Quantities, source breakdowns and detailed calculation record";
  var summary=report.Section("Bill of Quantities and source breakdown");summary.Note("Material totals and their source rows are presented together. A source row explains its parent quantity; it is not an additional quantity to add. Different units are not summed together.");
  var values=new System.Collections.Generic.List<string[]>();foreach(DataGridViewRow row in boqGrid.Rows){if(row.IsNewRow)continue;values.Add(new[]{Convert.ToString(row.Cells[0].Value),Convert.ToString(row.Cells[1].Value),Convert.ToString(row.Cells[2].Value),Convert.ToString(row.Cells[3].Value)});}summary.Table(new[]{"Pay item","Description / source","Quantity","Unit"},values);
  var detail=ProfessionalReportExport.FromText(Report());foreach(var section in detail.Sections){if(section.Blocks.Count==0)continue;section.NewPage=true;report.Sections.Add(section);}ProfessionalReportExport.Show(this,report);
 }
}
}
