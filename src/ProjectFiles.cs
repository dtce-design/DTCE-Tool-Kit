using System;using System.Collections.Generic;using System.IO;using System.Linq;using System.Xml;
namespace BeamScheduleFormatter {
/* the files saved for each project in every workspace (owner, 26 November 2026: "Kapag may click ako ng project sa project information
   magopen na din lahat ng files ko na sinave ko doon sa kahit saan workspace"): when a Bill of Quantities, POW or design sheet file is
   saved or opened, its path is kept under the current project (Documents\DTCE Tool Kit\Settings\ProjectFiles.xml, shared by the Store
   app and the .exe); using the project again in Project Information opens them all */
sealed class ProjectFile {public string Kind="",Path="";public DateTime Saved;public bool Exists{get{try{return File.Exists(Path);}catch(Exception){return false;}}}
 /* "boq", "pow", "design:loads" (the five Design Loads sheets in one file), "design:<sheet type>" */
 internal string Label{get{return Kind=="boq"?"Bill of Quantities":Kind=="pow"?"Program of Works (POW)":Kind=="design:loads"?"Design Loads":Kind.StartsWith("design:")?"Design sheet "+Kind.Substring(7):Kind;}}}
static class ProjectFiles {
 internal static string FilePath{get{return System.IO.Path.Combine(ProjectInfoStore.DirectoryPath,"ProjectFiles.xml");}}
 internal static string Key(ProjectInformation p){return p==null?"":(p.Name??"").Trim().ToUpperInvariant()+"\n"+(p.Location??"").Trim().ToUpperInvariant();}
 /* the launcher opens the files of a project that was chosen in Project Information */
 internal static event Action<ProjectInformation,List<ProjectFile>> OpenRequested;
 internal static bool Suspended;
 static XmlDocument Load(){var d=new XmlDocument{XmlResolver=null};try{if(File.Exists(FilePath))using(var r=XmlReader.Create(FilePath,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null}))d.Load(r);}catch(Exception){d=new XmlDocument();}
  if(d.DocumentElement==null||d.DocumentElement.Name!="ProjectFiles"){d=new XmlDocument();d.AppendChild(d.CreateElement("ProjectFiles"));}return d;}
 static void Write(XmlDocument d){Directory.CreateDirectory(ProjectInfoStore.DirectoryPath);string temp=FilePath+"."+Guid.NewGuid().ToString("N")+".tmp";try{d.Save(temp);if(File.Exists(FilePath))File.Replace(temp,FilePath,null);else File.Move(temp,FilePath);}finally{if(File.Exists(temp))File.Delete(temp);}}
 /* one file per kind and project: the last one saved or opened */
 internal static void Remember(string kind,string path){if(Suspended||string.IsNullOrEmpty(path))return;try{var p=ProjectInfoStore.Read();if(p==null||p.Name.Trim().Length==0)return;string key=Key(p),full=System.IO.Path.GetFullPath(path);var d=Load();
   XmlElement project=d.DocumentElement.SelectNodes("Project").Cast<XmlElement>().FirstOrDefault(e=>e.GetAttribute("key")==key);if(project==null){project=d.CreateElement("Project");project.SetAttribute("key",key);d.DocumentElement.AppendChild(project);}
   foreach(var old in project.SelectNodes("File").Cast<XmlElement>().Where(e=>e.GetAttribute("kind")==kind).ToList())project.RemoveChild(old);
   var f=d.CreateElement("File");f.SetAttribute("kind",kind);f.SetAttribute("path",full);f.SetAttribute("saved",DateTime.Now.ToString("o",System.Globalization.CultureInfo.InvariantCulture));project.AppendChild(f);Write(d);}catch(Exception){}}
 internal static List<ProjectFile> Of(ProjectInformation p){var list=new List<ProjectFile>();try{string key=Key(p);if(key.Trim().Length==0)return list;var project=Load().DocumentElement.SelectNodes("Project").Cast<XmlElement>().FirstOrDefault(e=>e.GetAttribute("key")==key);if(project==null)return list;
   foreach(XmlElement e in project.SelectNodes("File")){DateTime t;DateTime.TryParse(e.GetAttribute("saved"),System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.RoundtripKind,out t);list.Add(new ProjectFile{Kind=e.GetAttribute("kind"),Path=e.GetAttribute("path"),Saved=t});}}catch(Exception){}return list;}
 internal static void RequestOpen(ProjectInformation p){var files=Of(p).Where(f=>f.Exists).ToList();var ev=OpenRequested;if(files.Count>0&&ev!=null)ev(p,files);}
}
}
