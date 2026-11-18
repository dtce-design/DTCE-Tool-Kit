using System;using System.IO;using System.Threading.Tasks;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Automatic save of the open Program of Works (owner, 17 November 2026): when there are unsaved changes, save
// every 15 minutes, or sooner after 5 minutes without keyboard/mouse activity. Since v1.4.114 it writes a recovery copy
// (PowRecovery), never the project file; it shows no dialog and does not block
// the window. A cell still being typed in is not committed unless the user has been idle.
sealed class UserActivityFilter:IMessageFilter {
 internal DateTime Last=DateTime.UtcNow;
 public bool PreFilterMessage(ref Message m){int msg=m.Msg;if(msg==0x100||msg==0x104||msg==0x201||msg==0x204||msg==0x207||msg==0x20A)Last=DateTime.UtcNow;return false;}
}
partial class ProgramOfWorksForm {
 internal static readonly TimeSpan AutoSaveInterval=TimeSpan.FromMinutes(15),AutoSaveIdle=TimeSpan.FromMinutes(5);
 Timer autoSaveTimer;UserActivityFilter userActivity;DateTime? unsavedSince;
 void InstallAutoSave(){
  userActivity=new UserActivityFilter();Application.AddMessageFilter(userActivity);
  autoSaveTimer=new Timer{Interval=30000};autoSaveTimer.Tick+=async(s,e)=>await AutoSaveTick(DateTime.UtcNow);autoSaveTimer.Start();
  Disposed+=(s,e)=>{autoSaveTimer.Dispose();Application.RemoveMessageFilter(userActivity);};}
 internal bool HasUnsavedWork(){return DataXml()!=saved||beam.WorksDirty||slab.WorksDirty;}
 internal static bool AutoSaveDue(DateTime now,DateTime unsavedSince,DateTime lastActivity){return now-unsavedSince>=AutoSaveInterval||now-lastActivity>=AutoSaveIdle;}
 bool TypingInCell(){var grid=ActiveControl;while(grid is ContainerControl&&((ContainerControl)grid).ActiveControl!=null)grid=((ContainerControl)grid).ActiveControl;return grid is IDataGridViewEditingControl||grid is TextBoxBase&&grid.Parent is DataGridView;}
 // Owner (17 November 2026): auto-save no longer writes the project file. It keeps a recovery copy in
 // %LOCALAPPDATA%\BeamTools\Recovery\ (works for never-saved projects too); only Save replaces the .pow.xml.
 string lastRecoveryFingerprint;
 internal async Task AutoSaveTick(DateTime now){
  if(IsDisposed||savingPow||openingPow)return;
  bool unsaved;try{unsaved=HasUnsavedWork();}catch{return;}
  if(!unsaved){unsavedSince=null;PowRecovery.Discard(recoveryId);lastRecoveryFingerprint=null;return;}
  if(!unsavedSince.HasValue)unsavedSince=now;
  DateTime last=userActivity==null?now:userActivity.Last;if(!AutoSaveDue(now,unsavedSince.Value,last))return;
  bool idle=now-last>=AutoSaveIdle;if(!idle&&TypingInCell())return;
  if(SourcesPending)return;/* embedded sources still loading after open */
  savingPow=true;
  try{Guard();string fingerprint=DataXml();if(fingerprint==lastRecoveryFingerprint&&!beam.WorksDirty&&!slab.WorksDirty){unsavedSince=null;return;}
   string snapshot=BuildWorksProjectXml(false);var sources=ProjectSources();string original=currentPowPath;
   if(powSaveIndicator!=null)powSaveIndicator.Text="Saving recovery copy…";
   await Task.Run(()=>PowRecovery.Write(recoveryId,original,snapshot,sources));
   if(IsDisposed)return;lastRecoveryFingerprint=fingerprint;unsavedSince=null;
   if(powSaveIndicator!=null)powSaveIndicator.Text="Recovery copy "+DateTime.Now.ToString("HH:mm")+" - not yet saved to the file (use Save)";}
  catch(Exception ex){if(!IsDisposed&&powSaveIndicator!=null)powSaveIndicator.Text="Recovery copy failed: "+ex.Message+" - use Save";unsavedSince=now;}
  finally{savingPow=false;}}
}
}
