using System;using System.IO;using System.Xml;
namespace BeamScheduleFormatter {
// Standard values remembered for the next project (owner, 18 November 2026: "may mga standard values ako na hindi
// nasasave lagi, kailangan input ulet" - lost when a new project starts). Before, they were kept only when Save
// Defaults / the profile library were used explicitly. Now, every successful Save Program of Works and every close
// also stores:
//  - the reinforced-concrete defaults (covers, member materials, grades, commercial lengths, ready mix, lean
//    concrete) in the existing Documents > DTCE Tool Kit > Defaults > ReinforcedConcrete.xml, read by a new window;
//  - the roofing profile library (Documents > DTCE Tool Kit > Profiles);
//  - earthworks / material / roofing-lap standards in Documents > DTCE Tool Kit > Defaults > ProjectStandards.xml.
// A new Program of Works window starts from these values; an opened project still uses its own saved values.
partial class ProgramOfWorksForm {
 internal static string ProjectStandardsPath{get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal),"DTCE Tool Kit","Defaults","ProjectStandards.xml");}}
 bool standardsLoaded;
 void InstallStandardSettings(){if(Automated)return;LoadProjectStandards();FormClosed+=(s,e)=>PersistStandardSettings();}
 void LoadProjectStandards(){if(standardsLoaded)return;standardsLoaded=true;try{if(!File.Exists(ProjectStandardsPath))return;var doc=new XmlDocument{XmlResolver=null};doc.Load(ProjectStandardsPath);if(doc.DocumentElement==null||doc.DocumentElement.Name!="ProjectStandards")return;bool before=filling;filling=true;try{ReadEarthSettings(doc.DocumentElement);}finally{filling=before;}}catch(Exception){/* a damaged standards file must not stop the window; built-in defaults remain */}}
 void PersistStandardSettings(){if(Automated)return;
  try{SaveRCDefaults();}catch(Exception){}
  try{if(roofProfiles!=null&&!roofProfiles.IsDisposed)SaveRoofProfileLibrary(false);}catch(Exception){}
  try{var doc=new XmlDocument();var root=doc.CreateElement("ProjectStandards");root.SetAttribute("version","1");doc.AppendChild(root);WriteEarthSettings(root);
   string path=ProjectStandardsPath;Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
   try{doc.Save(temp);if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}}
  catch(Exception){}}
}
}
