using System;using System.Windows.Forms;
namespace BeamScheduleFormatter {
partial class ProgramOfWorksForm {
 string currentPowPath="";
 protected override bool ProcessCmdKey(ref Message message,Keys keys){if(keys==(Keys.Control|Keys.S)||keys==(Keys.Control|Keys.Shift|Keys.S)){try{if(keys==(Keys.Control|Keys.Shift|Keys.S))SaveAs();else SaveProjectAll();}catch(Exception ex){MessageBox.Show(this,ex.Message,"Save Bill of Quantities",MessageBoxButtons.OK,MessageBoxIcon.Warning);}return true;}/* menu shortcuts (owner, 19 November 2026: "pwede naman shortcut key") */Action command=keys==(Keys.Control|Keys.O)?(Action)Open:keys==(Keys.Control|Keys.P)?(Action)PreviewQuantityEngineeringReport:keys==(Keys.Control|Keys.Shift|Keys.P)?(Action)SaveQuantityEngineeringPdf:null;if(command!=null){try{command();}catch(Exception ex){MessageBox.Show(this,ex.Message,"Bill of Quantities",MessageBoxButtons.OK,MessageBoxIcon.Information);}return true;}return base.ProcessCmdKey(ref message,keys);}
}
}
