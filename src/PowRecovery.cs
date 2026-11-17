using System;using System.Collections.Generic;using System.Diagnostics;using System.Globalization;using System.IO;using System.Linq;using System.Text;using System.Windows.Forms;using System.Xml;
namespace BeamScheduleFormatter {
// Data-loss protection (owner, 17 November 2026, after a save replaced a project with an empty window):
// 1. Auto-save writes a recovery copy per window (Recovery\<id>.pow.xml + <id>.txt with the original path), offered
//    the next time a Program of Works window opens after a crash; it is deleted after a successful Save.
// 2. Save verifies the written file (well-formed, same number of rows) before it replaces the project file.
// 3. An empty window can never be saved over a file that has rows (Save As only).
// 4. Opening a file without rows warns and points to the backups.
// 5. "Restore backup" lists backups / recovery copies of the open file and opens the chosen one.
static class PowRecovery {
 internal static string Folder{get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BeamTools","Recovery");}}
 static readonly HashSet<string> live=new HashSet<string>(StringComparer.Ordinal);
 internal static string NewId(){string id=DateTime.Now.ToString("yyyyMMdd-HHmmss",CultureInfo.InvariantCulture)+"-"+Guid.NewGuid().ToString("N").Substring(0,8);lock(live)live.Add(id);return id;}
 internal static void Release(string id){lock(live)live.Remove(id);}
 internal static void Write(string id,string original,string snapshot,PowSourceFiles sources){Directory.CreateDirectory(Folder);PowProjectFileWriter.WriteWithSources(Path.Combine(Folder,id+".pow.xml"),snapshot,sources);File.WriteAllText(Path.Combine(Folder,id+".txt"),(original??"")+"\n"+DateTime.Now.ToString("o",CultureInfo.InvariantCulture),Encoding.UTF8);}
 internal static void Discard(string id){if(string.IsNullOrEmpty(id))return;try{File.Delete(Path.Combine(Folder,id+".pow.xml"));File.Delete(Path.Combine(Folder,id+".txt"));}catch(Exception){}}
 internal sealed class Entry {internal string Path,Original;internal DateTime Time;}
 // Recovery copies left by windows that are no longer open (e.g. after a crash or power loss).
 internal static List<Entry> Orphans(){var list=new List<Entry>();if(!Directory.Exists(Folder))return list;foreach(var file in new DirectoryInfo(Folder).GetFiles("*.pow.xml")){string id=file.Name.Substring(0,file.Name.Length-8);lock(live)if(live.Contains(id))continue;string original="";try{original=File.ReadAllLines(System.IO.Path.Combine(Folder,id+".txt"))[0];}catch(Exception){}list.Add(new Entry{Path=file.FullName,Original=original,Time=file.LastWriteTime});}return list.OrderByDescending(e=>e.Time).ToList();}
}
static class PowBackupList {
 internal sealed class Item {internal string Path,Kind;internal DateTime Time;internal int Rows;internal long Size;}
 internal static List<Item> For(string projectPath){var items=new List<Item>();string name=string.IsNullOrEmpty(projectPath)?null:System.IO.Path.GetFileName(projectPath);
  if(name!=null){foreach(char c in System.IO.Path.GetInvalidFileNameChars())name=name.Replace(c,'_');string dir=System.IO.Path.Combine(PowBackups.Folder,name);if(Directory.Exists(dir))foreach(var f in new DirectoryInfo(dir).GetFiles())items.Add(new Item{Path=f.FullName,Kind="Backup before save",Time=f.LastWriteTime,Size=f.Length});}
  foreach(var e in PowRecovery.Orphans().Where(e=>name==null||string.Equals(System.IO.Path.GetFileName(e.Original),System.IO.Path.GetFileName(projectPath),StringComparison.OrdinalIgnoreCase)||e.Original==""))items.Add(new Item{Path=e.Path,Kind="Recovery copy (auto-save)",Time=e.Time,Size=new FileInfo(e.Path).Length});
  foreach(var i in items)try{i.Rows=PowBackups.EntriesInFile(i.Path);}catch(Exception){i.Rows=-1;}
  return items.OrderByDescending(i=>i.Time).ToList();}
}
partial class ProgramOfWorksForm {
 readonly string recoveryId=PowRecovery.NewId();bool recoveryChecked;
 void InstallRecovery(){Disposed+=(s,e)=>PowRecovery.Release(recoveryId);FormClosed+=(s,e)=>{if(!HasUnsavedWorkSafe())PowRecovery.Discard(recoveryId);};
  Shown+=(s,e)=>{if(recoveryChecked)return;recoveryChecked=true;try{BeginInvoke((Action)OfferRecovery);}catch(InvalidOperationException){}};}
 bool HasUnsavedWorkSafe(){try{return HasUnsavedWork();}catch(Exception){return false;}}
 static bool Automated{get{return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"))||!Environment.UserInteractive;}}
 void OfferRecovery(){if(IsDisposed||Automated||HasUnsavedWorkSafe())return;List<PowRecovery.Entry> orphans;try{orphans=PowRecovery.Orphans();}catch(Exception){return;}if(orphans.Count==0)return;var newest=orphans[0];
  var answer=MessageBox.Show(this,"Unsaved Program of Works work was found from "+newest.Time.ToString("yyyy-MM-dd HH:mm",CultureInfo.CurrentCulture)+(newest.Original!=""?"\nProject: "+newest.Original:"")+"\n\nOpen the recovery copy now? (Yes = open, No = keep it for later, Cancel = delete it)","Recover unsaved work",MessageBoxButtons.YesNoCancel,MessageBoxIcon.Question);
  if(answer==DialogResult.Cancel){try{File.Delete(newest.Path);File.Delete(newest.Path.Substring(0,newest.Path.Length-8)+".txt");}catch(Exception){}return;}
  if(answer==DialogResult.Yes)OpenCopy(newest.Path,newest.Original);}
 // Opens a backup / recovery copy. The window is not bound to the project file, so Save asks for a name.
 void OpenCopy(string path,string original){try{Guard();if(HasUnsavedWorkSafe())throw new Exception("This window has unsaved inputs. Save them or use Close Program of Works first.");var snapshot=PowProjectFileWriter.Read(path);OpenProjectDocument(snapshot.Document,snapshot.Sources);currentPowPath=null;knownPowWriteUtc=null;saved="";
   if(powSaveIndicator!=null)powSaveIndicator.Text="Opened copy from "+File.GetLastWriteTime(path).ToString("yyyy-MM-dd HH:mm",CultureInfo.CurrentCulture)+" - use Save to choose where to keep it"+(string.IsNullOrEmpty(original)?"":" (original: "+Path.GetFileName(original)+")");}
  catch(Exception ex){MessageBox.Show(this,ex.Message,"Open copy",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
 void RestoreBackup(){var items=PowBackupList.For(currentPowPath);using(var form=new Form{Text="Restore backup"+(string.IsNullOrEmpty(currentPowPath)?"":" - "+Path.GetFileName(currentPowPath)),Width=860,Height=480,StartPosition=FormStartPosition.CenterParent,MinimizeBox=false,ShowInTaskbar=false}){
   var grid=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,BackgroundColor=System.Drawing.Color.White};
   foreach(string h in new[]{"Saved","Kind","Rows","Size KB","File"})grid.Columns.Add(h,h);grid.Columns[4].FillWeight=220;
   foreach(var i in items){int r=grid.Rows.Add(i.Time.ToString("yyyy-MM-dd HH:mm:ss",CultureInfo.CurrentCulture),i.Kind,i.Rows<0?"?":i.Rows.ToString(CultureInfo.CurrentCulture),(i.Size/1024.0).ToString("N0",CultureInfo.CurrentCulture),i.Path);grid.Rows[r].Tag=i;}
   var note=new Label{Dock=DockStyle.Top,Height=46,Padding=new Padding(8),Text=items.Count==0?"No backups yet for this file. Backups are made before every Save from v1.4.113 on; folder: "+PowBackups.Folder:"Choose a copy and Open. It opens as an unsaved copy; Save asks where to keep it (choose the original file to restore it)."};
   var bar=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=46,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(6)};var open=new Button{Text="Open selected copy",AutoSize=true};var folder=new Button{Text="Open backup folder",AutoSize=true};var close=new Button{Text="Close",AutoSize=true,DialogResult=DialogResult.Cancel};bar.Controls.Add(close);bar.Controls.Add(open);bar.Controls.Add(folder);
   Action pick=()=>{if(grid.CurrentRow==null)return;var i=(PowBackupList.Item)grid.CurrentRow.Tag;form.DialogResult=DialogResult.OK;form.Tag=i;};open.Click+=(s,e)=>pick();grid.CellDoubleClick+=(s,e)=>{if(e.RowIndex>=0)pick();};
   folder.Click+=(s,e)=>{try{Directory.CreateDirectory(PowBackups.Folder);Process.Start("explorer.exe","\""+PowBackups.Folder+"\"");}catch(Exception){}};
   form.Controls.Add(grid);form.Controls.Add(note);form.Controls.Add(bar);form.CancelButton=close;
   if(form.ShowDialog(this)==DialogResult.OK&&form.Tag is PowBackupList.Item)OpenCopy(((PowBackupList.Item)form.Tag).Path,currentPowPath);}}
 // Warning when an opened file holds no rows (empty or overwritten project).
 void WarnIfEmptyProject(XmlDocument doc,string path){if(Automated)return;if(doc.DocumentElement.SelectNodes("//Entry").Count>0||doc.DocumentElement["Beam"]!=null||doc.DocumentElement["Slab"]!=null)return;
  if(MessageBox.Show(this,"\""+Path.GetFileName(path)+"\" has no saved rows.\n\nIf it should contain a project, it may have been replaced by an empty save. Open the list of backups now?","Empty Program of Works",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)==DialogResult.Yes)RestoreBackup();}
}
}
