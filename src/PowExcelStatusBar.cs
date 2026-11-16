using System;using System.Drawing;using System.Windows.Forms;
namespace BeamScheduleFormatter {
partial class ProgramOfWorksForm {
 void InstallExcelStatusBar(){
  var bar=new Panel{Name="ExcelStatusBar",Dock=DockStyle.Bottom,Height=34,Padding=new Padding(16,5,16,4),BackColor=ModernUi.Soft};
  excelStatus.Name="ExcelSyncStatus";excelStatus.AccessibleName="Excel sync status";excelStatus.Dock=DockStyle.Fill;excelStatus.AutoSize=false;excelStatus.AutoEllipsis=true;excelStatus.TextAlign=ContentAlignment.MiddleLeft;excelStatus.Padding=Padding.Empty;excelStatus.Font=ModernUi.Body;excelStatus.ForeColor=PowTheme.Muted;excelStatus.BackColor=bar.BackColor;
  var tooltip=new ToolTip{ShowAlways=true,AutoPopDelay=30000};Action update=()=>tooltip.SetToolTip(excelStatus,excelStatus.Text);excelStatus.TextChanged+=(s,e)=>update();update();bar.Disposed+=(s,e)=>tooltip.Dispose();
  bar.Paint+=(s,e)=>{using(var line=new Pen(PowTheme.Line))e.Graphics.DrawLine(line,0,0,bar.Width,0);};bar.Controls.Add(excelStatus);Controls.Add(bar);bar.BringToFront();
 }
}
}
