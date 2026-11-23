using System;using System.Collections.Generic;using System.IO;using System.Linq;
namespace BeamScheduleFormatter {
/* Settings and saved projects shared by the Microsoft Store app and the downloaded .exe (owner, 23 November 2026: "kahit sana app from
   store or downloaded lang exe file sana naoopen pa din yung saved settings and project"). A Store (MSIX) app writes AppData into its
   own private copy, so the two installations did not see each other's files; Documents is shared. Each folder is
   Documents\DTCE Tool Kit\<name>; the first time, files of the old AppData folder and of the Store app's private copy are copied in
   (the newest of each), never over a file that is already there. Roof profiles already moved this way. */
static class SharedData {
 static readonly HashSet<string> migrated=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 static string Local{get{return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);}}
 internal static string Root{get{string docs=Environment.GetFolderPath(Environment.SpecialFolder.Personal);return string.IsNullOrEmpty(docs)?"":Path.Combine(docs,"DTCE Tool Kit");}}
 /* the shared folder, with the files (patterns) of the legacy AppData folders (relative to LocalAppData) copied in once */
 internal static string Folder(string name,string legacyRelative,params string[] patterns){string root=Root;
  if(root==""){string fallback=Path.Combine(Local,legacyRelative);try{Directory.CreateDirectory(fallback);}catch(Exception){}return fallback;}
  string dir=Path.Combine(root,name);try{Directory.CreateDirectory(dir);}catch(Exception){return Path.Combine(Local,legacyRelative);}
  lock(migrated){if(migrated.Add(dir+"|"+legacyRelative))try{CopyMissing(LegacyFolders(legacyRelative),dir,patterns.Length==0?new[]{"*"}:patterns);}catch(Exception){}}
  return dir;}
 /* the old AppData folder and the same folder inside each Store package's private copy */
 internal static IEnumerable<string> LegacyFolders(string relative){var list=new List<string>{Path.Combine(Local,relative)};string packages=Path.Combine(Local,"Packages");
  try{if(Directory.Exists(packages))foreach(var p in Directory.GetDirectories(packages,"DTCE.DTCEToolKit*"))list.Add(Path.Combine(p,"LocalCache","Local",relative));}catch(Exception){}
  return list.Where(Directory.Exists);}
 internal static void CopyMissing(IEnumerable<string> sources,string target,string[] patterns){var best=new Dictionary<string,FileInfo>(StringComparer.OrdinalIgnoreCase);
  foreach(var src in sources)foreach(var pattern in patterns)foreach(var f in new DirectoryInfo(src).GetFiles(pattern)){FileInfo seen;if(!best.TryGetValue(f.Name,out seen)||f.LastWriteTimeUtc>seen.LastWriteTimeUtc)best[f.Name]=f;}
  foreach(var f in best.Values){string to=Path.Combine(target,f.Name);if(File.Exists(to))continue;try{f.CopyTo(to,false);}catch(IOException){}}}
}
}
