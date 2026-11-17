using System;using System.Diagnostics;using System.IO;using System.Linq;using System.Reflection;using System.Windows.Forms;using BeamScheduleFormatter;
// Windows timing probe for opening, recalculating, saving and closing a Program of Works (owner, 17 November 2026:
// open / save / close are slow on Windows but fast on the Linux test machine). It prints the PowTiming steps so the
// CI log shows where Windows spends the time. Synthetic fixture: masonry, finishes, steel and 40 manual beams.
class PowPerformanceProbe {
 [STAThread]static void Main(){
  string fixture=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(Assembly.GetExecutingAssembly().Location)),"..","tests","fixtures","Perf-Synthetic.pow.xml");if(!File.Exists(fixture))fixture=Path.Combine("tests","fixtures","Perf-Synthetic.pow.xml");
  string copy=Path.Combine(Path.GetTempPath(),"dtce-perf-"+Guid.NewGuid().ToString("N")+".pow.xml");File.Copy(fixture,copy);var bf=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;var t=typeof(ProgramOfWorksForm);
  try{
   var sw=Stopwatch.StartNew();var form=new ProgramOfWorksForm();bool mono=Type.GetType("Mono.Runtime")!=null;if(!mono){form.Show();Application.DoEvents();}else{var handle=form.Handle;}Console.WriteLine("PERF construct+show "+sw.ElapsedMilliseconds+" ms");
   PowTiming.Start("Open");sw.Restart();form.OpenFrom(copy);PowTiming.Mark("open total");Application.DoEvents();PowTiming.Mark("first paint");Console.WriteLine("PERF open "+sw.ElapsedMilliseconds+" ms :: "+PowTiming.Finish());
   sw.Restart();t.GetMethod("UpdateBackupBillDashboard",bf).Invoke(form,null);Application.DoEvents();Console.WriteLine("PERF BOQ rebuild "+sw.ElapsedMilliseconds+" ms");
   sw.Restart();t.GetMethod("BackupQuantities",bf,null,new[]{typeof(bool)},null).Invoke(form,new object[]{false});Console.WriteLine("PERF BackupQuantities "+sw.ElapsedMilliseconds+" ms");
   for(int pass=1;pass<=2;pass++){
    sw.Restart();t.GetMethod("Recalculate",bf,null,Type.EmptyTypes,null).Invoke(form,null);Application.DoEvents();Console.WriteLine("PERF recalculate "+sw.ElapsedMilliseconds+" ms");
    PowTiming.Start("Save");sw.Restart();form.SaveTo(copy);Console.WriteLine("PERF save "+sw.ElapsedMilliseconds+" ms :: "+PowTiming.Finish());
    sw.Restart();t.GetMethod("DataXml",bf).Invoke(form,null);Console.WriteLine("PERF DataXml "+sw.ElapsedMilliseconds+" ms");}
   /* Dispose without the Close prompt so CI can never wait on a dialog. */sw.Restart();form.Hide();if(!mono)form.Dispose();Application.DoEvents();Console.WriteLine("PERF hide+dispose "+sw.ElapsedMilliseconds+" ms");
   Console.WriteLine("PASS: POW performance probe (timings above are informational)");
  }finally{try{File.Delete(copy);}catch(Exception){}}
 }
}
