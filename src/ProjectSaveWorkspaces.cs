using System;using System.Drawing;using System.IO;using System.Threading.Tasks;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* the Bill of Quantities in the project file ("Save project": every open workspace into one *.dtce) */
partial class ProgramOfWorksForm:IProjectWorkspace {
 public string ProjectKind{get{return "boq";}}
 public string WorkspaceName{get{return "Bill of Quantities";}}
 public bool CanSaveToProject{get{return !ViewOnly.IsLocked(this);}}
 public bool ProjectDirty{get{return HasUnsavedWorkSafe();}}
 public async Task SaveToProject(string path){currentPowPath=path;await SaveWithFeedback(false);}
 async void SaveProjectAll(){if(!ProjectSave.HasProject){Save();return;}string msg=await ProjectSave.SaveAll(this);if(msg!="")MessageBox.Show(this,msg,"Save project",MessageBoxButtons.OK,msg.Contains("Not saved")?MessageBoxIcon.Warning:MessageBoxIcon.Information);}
}
/* the POW workbook in the project file */
partial class PowWorkbookForm:IProjectWorkspace {
 public string ProjectKind{get{return "pow";}}
 public string WorkspaceName{get{return "POW";}}
 public bool CanSaveToProject{get{return book!=null;}}
 public bool ProjectDirty{get{return dirty;}}
 public Task SaveToProject(string path){projectPath=path;if(!SaveProject(false))throw new Exception("not saved");return Task.FromResult(0);}
 async void SaveProjectAll(){if(!ProjectSave.HasProject){SaveProject(false);return;}string msg=await ProjectSave.SaveAll(this);if(msg!="")status.Text=msg;}
}
}
