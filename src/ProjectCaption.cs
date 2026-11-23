using System;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* every workspace window shows the project it works on (owner, 23 November 2026: "dapat lahat ng design sheets ipakita yung project
   name"; "sa lahat ng workspace"): the project title from Project Information is added to the window title, and follows a change of
   the project (saved, loaded with a Bill of Quantities file, or picked from the saved projects). Small dialogs keep their own title. */
static class ProjectCaption {
 internal const string Marker="   |   Project: ";const int MaxName=90;
 static string current;
 static string Name(){try{var n=(ProjectInfoStore.Read().Name??"").Replace("\r"," ").Replace("\n"," ").Trim();while(n.Contains("  "))n=n.Replace("  "," ");return n.Length>MaxName?n.Substring(0,MaxName-1).TrimEnd()+"…":n;}catch(Exception){return "";}}
 static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Form,StrongBox> applied=new System.Runtime.CompilerServices.ConditionalWeakTable<Form,StrongBox>();
 sealed class StrongBox{public string Suffix="";}
 /* the title without the project suffix this class added (other text added to the title later is kept) */
 internal static string Base(string text,string suffix){text=text??"";if(!string.IsNullOrEmpty(suffix)){int i=text.LastIndexOf(suffix,StringComparison.Ordinal);if(i>=0)return text.Remove(i,suffix.Length);}return text;}
 static bool Dialog(Form f){return f.FormBorderStyle==FormBorderStyle.FixedDialog||f.FormBorderStyle==FormBorderStyle.FixedToolWindow||f.FormBorderStyle==FormBorderStyle.SizableToolWindow||f.FormBorderStyle==FormBorderStyle.None;}
 static bool updating;
 internal static void Update(Form f){if(updating||f==null||f.IsDisposed)return;if(current==null)current=Name();var box=applied.GetOrCreateValue(f);string b=Base(f.Text,box.Suffix);string suffix=Dialog(f)||current.Length==0?"":Marker+current;string want=b+suffix;box.Suffix=suffix;if(f.Text!=want){updating=true;try{f.Text=want;}finally{updating=false;}}}
 static readonly System.Collections.Generic.List<WeakReference<Form>> windows=new System.Collections.Generic.List<WeakReference<Form>>();
 internal static void Attach(Form f){f.TextChanged+=(s,e)=>Update(f);f.HandleCreated+=(s,e)=>Update(f);lock(windows)windows.Add(new WeakReference<Form>(f));Update(f);}
 /* the project changed: every open window */
 internal static void Refresh(){current=Name();Form[] open;lock(windows){windows.RemoveAll(w=>{Form f;return !w.TryGetTarget(out f)||f.IsDisposed;});open=windows.Select(w=>{Form f;w.TryGetTarget(out f);return f;}).Where(f=>f!=null).ToArray();}
  foreach(var f in open){try{if(f.InvokeRequired)f.BeginInvoke((Action)(()=>Update(f)));else Update(f);}catch(Exception){}}}
}
}
