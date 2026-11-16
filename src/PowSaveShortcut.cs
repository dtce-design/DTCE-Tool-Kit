using System;using System.Windows.Forms;
namespace BeamScheduleFormatter {
partial class ProgramOfWorksForm {
 string currentPowPath="";
 protected override bool ProcessCmdKey(ref Message message,Keys keys){if(keys==(Keys.Control|Keys.S)||keys==(Keys.Control|Keys.Shift|Keys.S)){try{if(keys==(Keys.Control|Keys.Shift|Keys.S))SaveAs();else Save();}catch(Exception ex){MessageBox.Show(this,ex.Message,"Save Program of Works",MessageBoxButtons.OK,MessageBoxIcon.Warning);}return true;}return base.ProcessCmdKey(ref message,keys);}
}
}
