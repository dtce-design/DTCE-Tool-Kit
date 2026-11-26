using System;using System.Collections.Generic;using System.Linq;using System.Reflection;using System.Windows.Forms;using BeamScheduleFormatter;
// Owner (26 November 2026): "Kapag may click ako ng project sa project information magopen na din lahat ng files ko na sinave ko doon sa
// kahit saan workspace" - the files saved for a project in every workspace are kept under it and opened when it is used again.
class ProjectFilesTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok){Console.WriteLine("FAIL: "+why);Environment.Exit(1);}}
 static void Pump(int ms){var end=DateTime.Now.AddMilliseconds(ms);while(DateTime.Now<end){Application.DoEvents();System.Threading.Thread.Sleep(10);}}
 [STAThread]static void Main(){Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);AccessCodes.Granted=true;
  string dir=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"project-files-"+Guid.NewGuid().ToString("N"));System.IO.Directory.CreateDirectory(dir);
  try{
   ProjectInfoStore.Save("Health Center","Tuba, Benguet","DPWH Benguet DEO","La Trinidad","2026-11-02","2027","3000000");var a=ProjectInfoStore.Read();
   string boq=System.IO.Path.Combine(dir,"a.boq.xml"),wind=System.IO.Path.Combine(dir,"loads.design.xml");System.IO.File.WriteAllText(boq,"<x/>");
   ProjectFiles.Remember("boq",boq);
   /* a Design Loads sheet saved with Ctrl+S is kept under the project */
   var lc=new LoadCombinationsForm();lc.Show();Pump(300);typeof(DesignSheetForm).GetField("designLoadsPath",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,null);
   var save=typeof(DesignSheetForm).GetMethod("Save",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(bool)},null);
   typeof(DesignSheetForm).GetProperty("SavedPath",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(lc,wind);save.Invoke(lc,new object[]{false});
   var files=ProjectFiles.Of(a);Check(files.Count==2&&files.Any(f=>f.Kind=="boq"&&f.Path==System.IO.Path.GetFullPath(boq))&&files.Any(f=>f.Kind=="design:loads"&&f.Exists),"both files kept under the project");
   ProjectFiles.Remember("boq",boq);Check(ProjectFiles.Of(a).Count(f=>f.Kind=="boq")==1,"one file per kind");
   /* another project keeps its own list */
   ProjectInfoStore.Save("School Building","Sagada","DPWH Mt. Province DEO","Bontoc","2026-11-03","2027","2000000");var b=ProjectInfoStore.Read();
   Check(ProjectFiles.Of(b).Count==0,"another project starts with no files");
   /* using project A again asks the launcher to open its files */
   List<ProjectFile> asked=null;Action<ProjectInformation,List<ProjectFile>> h=(p,f)=>asked=f;ProjectFiles.OpenRequested+=h;ProjectFiles.RequestOpen(a);ProjectFiles.OpenRequested-=h;
   Check(asked!=null&&asked.Count==2,"choosing the project opens its saved files");
   /* the launcher opens the Design Loads file in its sheet */
   ProjectInfoStore.Save(a.Name,a.Location,a.Office,a.OfficeAddress,a.DatePrepared,a.FiscalYear,a.Abc);
   var launcher=new LauncherForm();launcher.Show();Pump(500);
   typeof(LauncherForm).GetMethod("OpenProjectFiles",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(launcher,new object[]{ProjectFiles.Of(a).Where(f=>f.Kind!="boq").ToList()});Pump(800);
   var modules=(System.Collections.IDictionary)typeof(LauncherForm).GetField("designModules",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(launcher);
   var opened=modules["Load Combinations (NSCP 2015)"] as DesignSheetForm;Check(opened!=null&&!opened.IsDisposed&&opened.Visible,"the Design Loads sheet opened with the project's file");
   Check((string)typeof(DesignSheetForm).GetField("designLoadsPath",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null)==System.IO.Path.GetFullPath(wind),"Ctrl+S saves back to the project's file");
  }finally{try{System.IO.Directory.Delete(dir,true);}catch(Exception){}ProjectInfoStore.ClearProject();}
  Console.WriteLine("ProjectFilesTests passed: "+checks+" checks");Environment.Exit(0);}
}
