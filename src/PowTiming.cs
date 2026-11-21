using System;using System.Windows.Forms;using System.Collections.Generic;using System.Diagnostics;using System.Globalization;using System.IO;using System.Linq;
namespace BeamScheduleFormatter {
// Step timings for opening, saving and closing a Program of Works (owner, 17 November 2026: still slow on Windows,
// not reproducible on the Linux test machine). Each operation's steps are shown in the POW status bar and appended
// to %LOCALAPPDATA%\BeamTools\pow-timing.log so the slow step can be identified on the owner's PC.
static class PowTiming {
 static readonly Stopwatch total=new Stopwatch(),step=new Stopwatch();static readonly List<KeyValuePair<string,long>> steps=new List<KeyValuePair<string,long>>();static string operation;
 internal static string LastSummary="";
 internal static void Start(string name,bool continueSame=false){if(continueSame&&operation==name)return;operation=name;steps.Clear();total.Restart();step.Restart();}
 internal static void Mark(string name){if(operation==null)return;steps.Add(new KeyValuePair<string,long>(name,step.ElapsedMilliseconds));step.Restart();}
 internal static string Finish(){if(operation==null)return "";Mark("other");total.Stop();var shown=steps.Where(s=>s.Value>=50).OrderByDescending(s=>s.Value).Take(4).Select(s=>s.Key+" "+Seconds(s.Value)).ToList();
  LastSummary=operation+" "+Seconds(total.ElapsedMilliseconds)+(shown.Count>0?" ("+string.Join(", ",shown)+")":"");
  try{string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BeamTools");Directory.CreateDirectory(dir);string file=Path.Combine(dir,"pow-timing.log");if(File.Exists(file)&&new FileInfo(file).Length>512*1024)File.Delete(file);
   File.AppendAllText(file,DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss",CultureInfo.InvariantCulture)+" v"+Engine.Version+" "+operation+" total "+total.ElapsedMilliseconds+" ms | "+string.Join(" | ",steps.Select(s=>s.Key+" "+s.Value))+Environment.NewLine);}catch(Exception){}
  operation=null;return LastSummary;}
 internal static string Seconds(long ms){return (ms/1000.0).ToString("0.0",CultureInfo.InvariantCulture)+" s";}
}
partial class ProgramOfWorksForm {
 // Embedded source files are checked after the project is shown, so opening does not wait for large workbooks.
 void VerifySourcesInBackground(PowSourceFiles sources){var files=sources.Files.ToList();if(files.Count==0)return;System.Threading.Tasks.Task.Run(()=>{var bad=new List<string>();foreach(var f in files){try{f.Verify();}catch(Exception){bad.Add(f.Name);}}return bad;}).ContinueWith(t=>{if(IsDisposed||t.IsFaulted||t.Result.Count==0)return;try{BeginInvoke((Action)(()=>MessageBox.Show(this,"These embedded source files in the Bill of Quantities are damaged and cannot be extracted:\n\n"+string.Join("\n",t.Result)+"\n\nThe quantities are not affected. Re-attach the original files under Project files if they are needed.","Project source files",MessageBoxButtons.OK,MessageBoxIcon.Warning)));}catch(InvalidOperationException){}});}

}
}
