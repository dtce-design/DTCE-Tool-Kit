using System;using System.Collections.Generic;using System.IO;using System.Linq;using System.Threading.Tasks;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* a workspace that saves into the project file */
interface IProjectWorkspace {
 /* the part of the project file: boq, pow, bbs, lengths, design:loads, design:<sheet> */
 string ProjectKind{get;}
 string WorkspaceName{get;}
 /* false while the workspace has nothing to save (e.g. no beam schedule loaded) */
 bool CanSaveToProject{get;}
 /* changes since the last save or open */
 bool ProjectDirty{get;}
 /* writes the workspace to its working copy of the project file; it saves there from now on */
 Task SaveToProject(string path);
}
/* "Save project" (owner, 27 November 2026: "mas ayusin yung buttons para di nakakalito lalo na yung pagsave magoopen ako ng multiple design
   loads pow etc tapos pano isasave sa isang file na lang"): one command - the Workspaces window, Ctrl+S or "Save project" in any workspace -
   saves every open workspace into the one project file (*.dtce), asking for the file only the first time. Saving one workspace as a
   separate file stays in each workspace's own menu. */
static class ProjectSave {
 static readonly List<WeakReference> open=new List<WeakReference>();
 internal static bool Saving{get;private set;}
 internal static void Track(IProjectWorkspace w){open.Add(new WeakReference(w));var c=w as Control;if(c!=null)c.Disposed+=(s,e)=>open.RemoveAll(r=>!r.IsAlive||ReferenceEquals(r.Target,w));}
 internal static List<IProjectWorkspace> Open(){open.RemoveAll(r=>!r.IsAlive);return open.Select(r=>r.Target as IProjectWorkspace).Where(w=>{var c=w as Control;return w!=null&&(c==null||!c.IsDisposed);}).ToList();}
 internal static bool AnyDirty(){foreach(var w in Open())try{if(w.CanSaveToProject&&w.ProjectDirty)return true;}catch(Exception){}return false;}
 internal static List<string> DirtyNames(){var list=new List<string>();foreach(var w in Open())try{if(w.CanSaveToProject&&w.ProjectDirty&&!list.Contains(w.WorkspaceName))list.Add(w.WorkspaceName);}catch(Exception){}return list;}
 /* a project file needs the Project Information; without it Ctrl+S saves the workspace alone, as before */
 internal static bool HasProject{get{try{var p=ProjectInfoStore.Read();return p!=null&&p.Complete;}catch(Exception){return false;}}}
 /* saves every open workspace into the project file; returns the message to show (empty when cancelled) */
 internal static async Task<string> SaveAll(IWin32Window owner){if(Saving)return "";var ws=Open().Where(w=>{try{return w.CanSaveToProject;}catch(Exception){return false;}}).ToList();
  if(ws.Count==0)return "Nothing to save yet: open a workspace and enter its data first.";
  if(ProjectPackage.PathFor(ws[0].ProjectKind,owner)==null)return "";
  Saving=true;var names=new List<string>();var failed=new List<string>();var done=new HashSet<string>();
  try{foreach(var w in ws){if(!done.Add(w.ProjectKind))continue;try{string path=ProjectPackage.PathFor(w.ProjectKind,owner);if(path==null)continue;await w.SaveToProject(path);names.Add(w.WorkspaceName);}catch(Exception ex){failed.Add(w.WorkspaceName+": "+ex.Message);}}}
  finally{Saving=false;}
  try{ProjectPackage.Pack();}catch(Exception ex){failed.Add("project file: "+ex.Message);}
  string file=ProjectPackage.CurrentPath==null?"":Path.GetFileName(ProjectPackage.CurrentPath);
  return (names.Count>0?"Project saved in "+file+": "+string.Join(", ",names)+".":"")+(failed.Count>0?" Not saved - "+string.Join("; ",failed):"");}
 /* asks before the app closes with unsaved workspaces: true = go on closing */
 internal static bool ConfirmClose(IWin32Window owner){var names=DirtyNames();if(names.Count==0)return true;
  var r=MessageBox.Show(owner,"These workspaces have changes that are not saved:\n\n  "+string.Join("\n  ",names)+"\n\nSave them in the project file before closing?","Save project",MessageBoxButtons.YesNoCancel,MessageBoxIcon.Question);
  if(r==DialogResult.Cancel)return false;if(r==DialogResult.No)return true;var t=SaveAll(owner);while(!t.IsCompleted)Application.DoEvents();if(t.Result!="")MessageBox.Show(owner,t.Result,"Save project");return !AnyDirty();}
}
}
