using System;using System.Collections.Generic;using System.Globalization;using System.IO;using System.Linq;using System.Text;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Save safety (owner, 17 November 2026: a save replaced the project file and its quantities were lost, with no prompt).
// - Since v1.16.1 each save is written and verified in this backup folder, then copied once to the project (the
//   first save of a file there also copies the version being replaced). Before: the previous file is copied to %LOCALAPPDATA%\BeamTools\POW-backups\<name>\ (the
//   newest 30 copies per file are kept).
// - Save asks before replacing a file that holds noticeably more entries than this window would write, or that was
//   changed on disk after this window opened / saved it. Auto-save never replaces such a file; it waits for a manual Save.
// - A failed Open leaves the window unbound from any file, so a partly loaded window cannot overwrite a project.
static class PowBackups {
 internal static string Folder{get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BeamTools","POW-backups");}}
 static string Dir(string path){string name=Path.GetFileName(path);foreach(char c in Path.GetInvalidFileNameChars())name=name.Replace(c,'_');return Path.Combine(Folder,name);}
 internal static bool HasAny(string path){string dir=Dir(path);return Directory.Exists(dir)&&Directory.EnumerateFiles(dir).Any(f=>!f.EndsWith(".tmp",StringComparison.OrdinalIgnoreCase));}
 internal static string NewPath(string path){string dir=Dir(path);Directory.CreateDirectory(dir);return Path.Combine(dir,DateTime.Now.ToString("yyyyMMdd-HHmmss-fff",CultureInfo.InvariantCulture)+"-"+Path.GetFileName(dir));}
 internal static void Trim(string path){string dir=Dir(path);if(!Directory.Exists(dir))return;foreach(var old in new DirectoryInfo(dir).GetFiles().Where(f=>!f.Name.EndsWith(".tmp",StringComparison.OrdinalIgnoreCase)).OrderByDescending(f=>f.LastWriteTimeUtc).Skip(30))try{old.Delete();}catch(Exception){}}
 internal static string Backup(string path){if(!File.Exists(path))return null;var info=new FileInfo(path);if(info.Length==0)return null;
  string name=Path.GetFileName(path);foreach(char c in Path.GetInvalidFileNameChars())name=name.Replace(c,'_');string dir=Path.Combine(Folder,name);Directory.CreateDirectory(dir);
  string target=Path.Combine(dir,DateTime.Now.ToString("yyyyMMdd-HHmmss",CultureInfo.InvariantCulture)+"-"+name);File.Copy(path,target,true);
  foreach(var old in new DirectoryInfo(dir).GetFiles().OrderByDescending(f=>f.LastWriteTimeUtc).Skip(30))try{old.Delete();}catch(Exception){}
  return target;}
 // Number of saved sheet rows ("<Entry") in a file / snapshot, ignoring embedded source payloads.
 internal static int Entries(string xml){int n=0,i=0;while((i=xml.IndexOf("<Entry",i,StringComparison.Ordinal))>=0){n++;i+=6;}return n;}
 internal static int EntriesInFile(string path){int n=0;using(var reader=new StreamReader(path,Encoding.UTF8,true)){char[] buffer=new char[65536];string carry="";int read;while((read=reader.Read(buffer,0,buffer.Length))>0){string chunk=carry+new string(buffer,0,read);n+=Entries(chunk);carry=chunk.Length>5?chunk.Substring(chunk.Length-5):chunk;if(carry.Contains("<"))carry=carry.Substring(carry.IndexOf('<'));else carry="";}}return n;}
 internal static bool Shrinks(int before,int after){return before>=5&&after<before*0.8;}
}
partial class ProgramOfWorksForm {
 DateTime? knownPowWriteUtc;
 void RememberPowFile(string path){try{knownPowWriteUtc=File.Exists(path)?File.GetLastWriteTimeUtc(path):(DateTime?)null;}catch(Exception){knownPowWriteUtc=null;}}
 bool ChangedOnDisk(string path){try{return File.Exists(path)&&knownPowWriteUtc.HasValue&&string.Equals(Path.GetFullPath(path),currentPowPath,StringComparison.OrdinalIgnoreCase)&&File.GetLastWriteTimeUtc(path)!=knownPowWriteUtc.Value;}catch(Exception){return false;}}
 // Returns false when the user declines replacing the file.
 bool ConfirmReplace(string path,string snapshot,bool automatic){if(!File.Exists(path))return true;int before;try{before=PowBackups.EntriesInFile(path);}catch(Exception){before=0;}int after=PowBackups.Entries(snapshot);
  if(after==0&&before>0){if(!automatic)MessageBox.Show(this,"This window has no saved rows, so it cannot replace \""+Path.GetFileName(path)+"\" ("+before+" rows). Use Save As to save it under another name.","Save blocked",MessageBoxButtons.OK,MessageBoxIcon.Stop);return false;}
  bool shrinks=PowBackups.Shrinks(before,after),changed=ChangedOnDisk(path);bool foreign=!string.Equals(Path.GetFullPath(path),currentPowPath,StringComparison.OrdinalIgnoreCase);
  if(!shrinks&&!changed)return true;if(automatic)return false;
  string why=(shrinks?"The file on disk has "+before+" saved rows; this window would save "+after+".":"")+(changed?(shrinks?"\n":"")+"The file was changed on disk after this window opened or saved it.":"");
  return MessageBox.Show(this,"Replace \""+Path.GetFileName(path)+"\"?\n\n"+why+"\n\nA backup copy of the current file is kept in:\n"+PowBackups.Folder+"\n\nChoose No to keep the file and use Save As instead.","Replace Bill of Quantities file",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)==DialogResult.Yes;}
}
}
