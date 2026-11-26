using System.Linq;using System;using System.Reflection;using System.Windows.Forms;using BeamScheduleFormatter;
// Owner (26 November 2026): "sa start na lang magoopen ng saved file tapos doon na din magsasave. Nakasave na din doon yung project" -
// a saved Bill of Quantities file carries the project information; opening it at the start makes it the current project, and Save
// writes back to the same file.
class OpenSavedProjectTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok){Console.WriteLine("FAIL: "+why);Environment.Exit(1);}}
 static void Pump(int ms){var end=DateTime.Now.AddMilliseconds(ms);while(DateTime.Now<end){Application.DoEvents();System.Threading.Thread.Sleep(10);}}
 [STAThread]static void Main(){Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);AccessCodes.Granted=true;
  string path=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"open-saved-project-"+Guid.NewGuid().ToString("N")+".boq.xml");
  try{
   ProjectInfoStore.Save("Two-Storey Office Building","Baguio City","DPWH District Office","Engineers Hill","2026-11-01","2027","4950000");
   /* the windows are not shown: a shown Bill of Quantities window crashed in its tooltip on the Windows runner (other BOQ tests do the same) */var f=new ProgramOfWorksForm();f.SaveTo(path);
   Check(System.IO.File.Exists(path),"Bill of Quantities saved");
   ProjectInfoStore.ClearProject();Check(ProjectInfoStore.Read().Name=="","project cleared before opening");
   var info=LauncherForm.ProjectInFile(path);Check(info!=null&&info.Name=="Two-Storey Office Building"&&info.Complete,"the file carries the complete project information");
   var f2=new ProgramOfWorksForm();f2.OpenFrom(path);
   Check(ProjectInfoStore.Read().Name=="Two-Storey Office Building"&&ProjectInfoStore.Read().Location=="Baguio City","opening the file makes it the current project");
   string current=(string)typeof(ProgramOfWorksForm).GetField("currentPowPath",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(f2);
   Check(current==System.IO.Path.GetFullPath(path),"Save writes back to the opened file");
  }finally{try{System.IO.File.Delete(path);}catch(Exception){}ProjectInfoStore.ClearProject();}
  Console.WriteLine("OpenSavedProjectTests passed: "+checks+" checks");foreach(Form f in Application.OpenForms.Cast<Form>().ToList())try{f.Close();f.Dispose();}catch(Exception){}Application.DoEvents();Environment.Exit(0);}
}
