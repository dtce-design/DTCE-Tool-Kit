using System;using System.Collections.Generic;using System.IO;using System.Linq;using System.Windows.Forms;using System.Xml;
namespace BeamScheduleFormatter {
/* one file per project (owner, 26 November 2026: "1 file na lang ang isave"; "or open project, or from list kapag nawala sa default saved
   list"): a DTCE project file (*.dtce) holds the project information and the saved file of every workspace - Bill of Quantities, POW,
   Design Loads and the other design sheets. While a project is open each workspace works on its own copy in a working folder; every
   Save writes that copy and then packs all of them back into the one project file. The project opens from "Open saved project" or from
   the saved list in Project Information; opening the file puts the project back on the list. */
static class ProjectPackage {
 internal const string Filter="DTCE project (*.dtce)|*.dtce";
 static readonly System.Globalization.CultureInfo Ci=System.Globalization.CultureInfo.InvariantCulture;
 /* the open project file, its working folder and the project it belongs to */
 internal static string CurrentPath{get;private set;}
 static string working,projectKey;
 internal static bool Packing;
 /* the working copy names by kind */
 internal static string FileName(string kind){return kind=="bbs"?"project.beam-cutting.xml":kind=="lengths"?"project.lengths.xml":kind=="boq"?"project.boq.xml":kind=="pow"?"project.pow.xml":kind=="design:loads"?"design-loads.design.xml":kind.StartsWith("design:")?"design-"+kind.Substring(7)+".design.xml":null;}
 static string KindOf(string name){if(name=="project.beam-cutting.xml")return "bbs";if(name=="project.lengths.xml")return "lengths";if(name=="project.boq.xml")return "boq";if(name=="project.pow.xml")return "pow";if(name=="design-loads.design.xml")return "design:loads";if(name.StartsWith("design-")&&name.EndsWith(".design.xml"))return "design:"+name.Substring(7,name.Length-7-11);return null;}
 static string NewWorkingFolder(){string dir=Path.Combine(SharedData.Folder("Working",Path.Combine("DTCE Tool Kit","Working")),DateTime.Now.ToString("yyyyMMdd-HHmmss",Ci)+"-"+Guid.NewGuid().ToString("N").Substring(0,8));Directory.CreateDirectory(dir);return dir;}
 static bool SameProject(){try{return CurrentPath!=null&&projectKey==ProjectFiles.Key(ProjectInfoStore.Read());}catch(Exception){return false;}}
 /* the path a workspace saves to when it has none yet: the working copy of the open project file; with no project file for the current
    project, the owner names it once (null when cancelled) */
 internal static string PathFor(string kind,IWin32Window owner){if(FileName(kind)==null)return null;if(!SameProject()&&!Create(owner))return null;return Path.Combine(working,FileName(kind));}
 /* a workspace's own path belongs to the open project when it is one of its working copies */
 /* the working copy only while the current project has its file open (no question asked) */
 internal static string PathIfOpen(string kind){return SameProject()&&FileName(kind)!=null?Path.Combine(working,FileName(kind)):null;}
 internal static bool IsWorking(string path){try{return working!=null&&path!=null&&string.Equals(Path.GetDirectoryName(Path.GetFullPath(path)),working,StringComparison.OrdinalIgnoreCase);}catch(Exception){return false;}}
 internal static bool Create(IWin32Window owner){var p=ProjectInfoStore.Read();if(p==null||!p.Complete){MessageBox.Show(owner,"Fill in and save the Project Information first.","Save project",MessageBoxButtons.OK,MessageBoxIcon.Information);return false;}
  string name=new string((p.Name.Length>60?p.Name.Substring(0,60):p.Name).Select(c=>Path.GetInvalidFileNameChars().Contains(c)||c=='\n'||c=='\r'?' ':c).ToArray()).Trim();if(name=="")name="Project";
  using(var d=new SaveFileDialog{Title="Save project (one file for every workspace)",Filter=Filter,FileName=name+".dtce",OverwritePrompt=true}){if(d.ShowDialog(owner)!=DialogResult.OK)return false;New(d.FileName);}return true;}
 /* a new project file for the current project at the given path */
 internal static void New(string path){Start(path,ProjectInfoStore.Read());Pack();}
 static void Start(string path,ProjectInformation p){CurrentPath=Path.GetFullPath(path);working=NewWorkingFolder();projectKey=ProjectFiles.Key(p);ProjectFiles.Remember("project",CurrentPath);}
 /* writes the project information and every working copy into the project file (atomic replace) */
 internal static void Pack(){if(CurrentPath==null||working==null||Packing)return;Packing=true;try{var p=ProjectInfoStore.Read();var d=new XmlDocument();var root=d.CreateElement("DtceProject");root.SetAttribute("version","1");root.SetAttribute("saved",DateTime.Now.ToString("o",Ci));d.AppendChild(root);
   var info=d.CreateElement("ProjectInformation");foreach(var kv in new[]{new[]{"name",p.Name},new[]{"location",p.Location},new[]{"office",p.Office},new[]{"officeAddress",p.OfficeAddress},new[]{"prepared",p.DatePrepared},new[]{"fy",p.FiscalYear},new[]{"abc",p.Abc}})info.SetAttribute(kv[0],kv[1]??"");root.AppendChild(info);
   foreach(var file in Directory.GetFiles(working).OrderBy(f=>f)){string kind=KindOf(Path.GetFileName(file));if(kind==null)continue;var part=d.CreateElement("Part");part.SetAttribute("kind",kind);part.SetAttribute("name",Path.GetFileName(file));part.InnerText=Convert.ToBase64String(File.ReadAllBytes(file));root.AppendChild(part);}
   string temp=CurrentPath+"."+Guid.NewGuid().ToString("N")+".tmp";try{d.Save(temp);if(File.Exists(CurrentPath))File.Replace(temp,CurrentPath,null);else File.Move(temp,CurrentPath);}finally{if(File.Exists(temp))File.Delete(temp);}}
  finally{Packing=false;}}
 internal static bool IsProjectFile(string path){return path!=null&&path.EndsWith(".dtce",StringComparison.OrdinalIgnoreCase);}
 internal static ProjectInformation ReadInfo(string path){var d=Load(path);var e=d.DocumentElement["ProjectInformation"];if(e==null)return null;
  return new ProjectInformation{Name=e.GetAttribute("name"),Location=e.GetAttribute("location"),Office=e.GetAttribute("office"),OfficeAddress=e.GetAttribute("officeAddress"),DatePrepared=e.GetAttribute("prepared"),FiscalYear=e.GetAttribute("fy"),Abc=e.GetAttribute("abc")};}
 static XmlDocument Load(string path){var d=new XmlDocument{XmlResolver=null};using(var r=XmlReader.Create(path,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null}))d.Load(r);if(d.DocumentElement==null||d.DocumentElement.Name!="DtceProject")throw new Exception("This is not a DTCE project file.");return d;}
 /* opens a project file: the project information becomes the current project (and is put back on the saved list), the parts are
    unpacked into a new working folder; returns the working copies for the workspaces to open */
 internal static List<ProjectFile> Open(string path){var d=Load(path);var info=ReadInfo(path);if(info==null||!info.Complete)throw new Exception("The project file has no complete project information.");
  ProjectFiles.Suspended=true;try{ProjectInfoStore.Save(info.Name,info.Location,info.Office,info.OfficeAddress,info.DatePrepared??"",info.FiscalYear??"",info.Abc??"");}finally{ProjectFiles.Suspended=false;}
  Start(path,info);var list=new List<ProjectFile>();
  foreach(XmlElement part in d.DocumentElement.SelectNodes("Part")){string kind=part.GetAttribute("kind"),name=FileName(kind);if(name==null)continue;string file=Path.Combine(working,name);File.WriteAllBytes(file,Convert.FromBase64String(part.InnerText));list.Add(new ProjectFile{Kind=kind,Path=file});}
  return list;}
 /* a workspace saved (or opened) its working copy: the project file follows */
 internal static void Saved(string path){if(IsWorking(path)&&SameProject())try{Pack();}catch(Exception){}}
 internal static void Close(){CurrentPath=null;working=null;projectKey=null;}
}
}
