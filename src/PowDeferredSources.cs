using System;using System.Linq;using System.Threading.Tasks;using System.Windows.Forms;using System.Xml;
namespace BeamScheduleFormatter {
// Embedded project source files (original Excel workbook, DXF schedules) load in the background after a POW opens
// (owner, 17 November 2026: "read file 21.9 s"). Until they are loaded the saved manifest stands in for them, so the
// project does not look changed; saving and the Project files window wait for them first.
partial class ProgramOfWorksForm {
 Task<PowSourceFiles> pendingSources;XmlElement pendingManifest;
 bool SourcesPending{get{return pendingSources!=null;}}
 void BeginDeferredSources(string path,XmlElement root){var manifest=root.SelectSingleNode("SourceFileManifest") as XmlElement;pendingManifest=manifest==null?null:(XmlElement)manifest.CloneNode(true);var task=Task.Run(()=>PowProjectFileWriter.ReadSources(path));pendingSources=task;
  task.ContinueWith(t=>{if(IsDisposed)return;try{BeginInvoke((Action)(()=>{if(object.ReferenceEquals(pendingSources,task))ApplyDeferredSources();}));}catch(InvalidOperationException){}});}
 void CancelDeferredSources(){pendingSources=null;pendingManifest=null;}
 // Runs on the UI thread. Blocks only when the archive is still being read (a save right after opening).
 void ApplyDeferredSources(){var task=pendingSources;if(task==null)return;var manifest=pendingManifest;pendingSources=null;pendingManifest=null;
  PowSourceFiles loaded;try{loaded=task.Result;}catch(AggregateException ex){MessageBox.Show(this,"The embedded source files of this Bill of Quantities could not be read: "+ex.InnerException.Message+"\n\nThe quantities are not affected. Re-attach the original files under Project files if they are needed.","Project source files",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}
  try{if(manifest!=null){var doc=new XmlDocument{XmlResolver=null};var root=doc.CreateElement("ProgramOfWorks");doc.AppendChild(root);root.AppendChild(doc.ImportNode(manifest,true));PowSourceFiles.ValidateManifest(root,loaded,true);}}catch(Exception ex){MessageBox.Show(this,ex.Message,"Project source files",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
  var merged=new PowSourceFiles();merged.AddRange(loaded.Files);merged.AddRange(projectSourceFiles.Files);projectSourceFiles.Replace(merged);VerifySourcesInBackground(loaded);}
 async Task WaitForDeferredSources(){var task=pendingSources;if(task==null)return;try{await task;}catch(Exception){}if(object.ReferenceEquals(pendingSources,task))ApplyDeferredSources();}
 void WriteSourceManifest(XmlElement xmlElement){if(pendingSources!=null&&pendingManifest!=null){xmlElement.AppendChild(xmlElement.OwnerDocument.ImportNode(pendingManifest,true));return;}ApplyDeferredSources();ProjectSources().Write(xmlElement,true);}
}
}
