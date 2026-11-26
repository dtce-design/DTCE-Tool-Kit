using System;using System.Collections.Generic;using System.Linq;using System.Reflection;using System.Windows.Forms;using System.Xml;using BeamScheduleFormatter;
// Owner (26 November 2026): "1 file na lang ang isave"; "or open project, or from list kapag nawala sa default saved list" - one DTCE project
// file holds the project information and the saved file of every workspace; it opens from Open saved project or the saved list.
class ProjectPackageTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok){Console.WriteLine("FAIL: "+why);Environment.Exit(1);}}
 static void Pump(int ms){var end=DateTime.Now.AddMilliseconds(ms);while(DateTime.Now<end){Application.DoEvents();System.Threading.Thread.Sleep(10);}}
 static List<string> Parts(string path){var d=new XmlDocument();d.Load(path);return d.DocumentElement.SelectNodes("Part").Cast<XmlElement>().Select(e=>e.GetAttribute("kind")).ToList();}
 [STAThread]static void Main(){Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);AccessCodes.Granted=true;
  string dir=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"project-package-"+Guid.NewGuid().ToString("N"));System.IO.Directory.CreateDirectory(dir);string file=System.IO.Path.Combine(dir,"Rural Health Unit.dtce");
  try{
   ProjectInfoStore.Save("Rural Health Unit","Itogon, Benguet","DPWH Benguet 1st DEO","La Trinidad","2026-11-05","2027","6500000");var info=ProjectInfoStore.Read();
   ProjectPackage.New(file);Check(System.IO.File.Exists(file)&&Parts(file).Count==0,"a new project file with the project information");
   /* Design Loads: Ctrl+S with no file yet goes into the project file, no dialog */
   var lc=new LoadCombinationsForm();lc.Show();Pump(300);typeof(DesignSheetForm).GetField("designLoadsPath",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,null);
   typeof(DesignSheetForm).GetMethod("Save",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(bool)},null).Invoke(lc,new object[]{false});
   Check(Parts(file).Contains("design:loads"),"Design Loads saved into the one project file");
   /* Bill of Quantities: its default path is the project's working copy; saving packs it in */
   string boqPath=ProjectPackage.PathFor("boq",null);Check(ProjectPackage.IsWorking(boqPath)&&boqPath.EndsWith(".boq.xml"),"the Bill of Quantities saves to the project's working copy");
   /* not shown (Windows runner tooltip crash) */var boq=new ProgramOfWorksForm();disposeLater.Add(boq);boq.SaveTo(boqPath);ProjectFiles.Remember("boq",boqPath);
   var parts=Parts(file);Check(parts.Contains("boq")&&parts.Contains("design:loads")&&parts.Count==2,"one file holds both workspaces");
   /* Splice, Development & Hooks and the Bar Bending Schedule go into the same file */
   var rl=new RebarLengthForm();disposeLater.Add(rl);typeof(RebarLengthForm).GetMethod("WriteProject",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(rl,new object[]{ProjectPackage.PathIfOpen("lengths")});
   var bbs=new CuttingListForm();disposeLater.Add(bbs);typeof(CuttingListForm).GetMethod("WriteTo",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(bbs,new object[]{ProjectPackage.PathIfOpen("bbs")});
   parts=Parts(file);Check(parts.Contains("lengths")&&parts.Contains("bbs")&&parts.Count==4,"the other tabs are in the project file too");
   /* an older loose file still opens and saves back to itself, not into the project file */
   string loose=System.IO.Path.Combine(dir,"old-sheet.design.xml");typeof(DesignSheetForm).GetField("designLoadsPath",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,null);
   typeof(DesignSheetForm).GetMethod("Save",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(bool)},null).Invoke(lc,new object[]{false});
   System.IO.File.Copy(ProjectPackage.PathIfOpen("design:loads"),loose);string packed=System.IO.File.ReadAllText(file);lc.OpenFile(loose);
   typeof(DesignSheetForm).GetMethod("Save",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(bool)},null).Invoke(lc,new object[]{false});
   Check(System.IO.File.Exists(loose)&&System.IO.File.ReadAllText(file)==packed,"an old loose file opens and saves to itself");
   Check(ProjectFiles.Of(info).Any(f=>f.Kind=="project"&&f.Path==System.IO.Path.GetFullPath(file))&&!ProjectFiles.Of(info).Any(f=>f.Kind=="boq"&&ProjectPackage.IsWorking(f.Path)),"the saved list points to the project file, not the working copies");
   /* gone from the saved list: opening the file brings the project back */
   ProjectLibrary.Remove(info);ProjectInfoStore.ClearProject();ProjectPackage.Close();
   var opened=ProjectPackage.Open(file);
   Check(ProjectInfoStore.Read().Name=="Rural Health Unit"&&ProjectInfoStore.Read().Abc=="6500000","opening the project file sets the project");
   Check(ProjectLibrary.List().Any(p=>p.Name=="Rural Health Unit"),"the project is back on the saved list");
   Check(opened.Count==4&&opened.All(f=>f.Exists&&ProjectPackage.IsWorking(f.Path)),"every workspace file unpacked for its workspace");
   /* another project does not write into this file */
   ProjectInfoStore.Save("Other Project","Baguio","DPWH","Baguio","2026-11-06","2027","1000000");Check(!ProjectPackage.IsWorking(null),"no path");
   string before=System.IO.File.ReadAllText(file);ProjectFiles.Remember("boq",opened.First(f=>f.Kind=="boq").Path);Check(System.IO.File.ReadAllText(file)==before,"a different current project leaves the file alone");
  }finally{try{System.IO.Directory.Delete(dir,true);}catch(Exception){}ProjectPackage.Close();ProjectInfoStore.ClearProject();}
  Console.WriteLine("ProjectPackageTests passed: "+checks+" checks");
  /* close every window before leaving (an exit with open windows ended with a non-zero code on the Windows runner); Mono cannot dispose the
     Bill of Quantities window */
  foreach(Form f in Application.OpenForms.Cast<Form>().ToList())try{f.Close();f.Dispose();}catch(Exception){}
  if(Environment.OSVersion.Platform==PlatformID.Win32NT)foreach(var d in disposeLater)try{d.Dispose();}catch(Exception){}
  Application.DoEvents();Environment.Exit(0);}
 static readonly List<IDisposable> disposeLater=new List<IDisposable>();
}
