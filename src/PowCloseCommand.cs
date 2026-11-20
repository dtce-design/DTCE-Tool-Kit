using System;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Close the current Program of Works (the usual save prompt applies) so another project can be opened in a fresh window.
// When it is hosted in the launcher, the launcher returns to its start page once the window has actually closed
// (including after a Save-then-close).
partial class ProgramOfWorksForm {
 bool returnToLauncherHooked;
 void CloseProgramOfWorks(){var host=TopLevel?null:TopLevelControl as Form;if(host!=null&&!returnToLauncherHooked){returnToLauncherHooked=true;FormClosed+=(s,e)=>{if(host.IsDisposed)return;var launcher=host as LauncherForm;/* in its own workspace window (owner, 19 November 2026) the window closes with it */host.BeginInvoke(launcher!=null?new Action(launcher.ShowStart):new Action(host.Close));};}Close();}
}
}
