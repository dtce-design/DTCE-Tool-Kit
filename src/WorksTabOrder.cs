using System;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* the works in construction sequence (owner, 23 November 2026: "arrange yung main tabs" - "yung pagkakasunod sunod ng works"; chose
   the construction sequence): earthworks, structure, envelope, finishes and site works, then the schedule, duration and reports */
partial class ProgramOfWorksForm {
 internal static readonly string[] WorksOrder={"Bill of Quantities","Earthworks","Reinforced Concrete Works","Structural Steel Works","Masonry Works","Roofing Works","Carpentry and Joinery Works","Doors & Windows","Plumbing Works","Finishing Works","Pavement Construction","Slope Protection Works","Construction Schedule","Contract Duration","Reports"};
 /* tabs not in the list keep their place relative to each other, before Construction Schedule */
 void OrderWorksTabs(){var all=tabs.TabPages.Cast<TabPage>().ToList();Func<TabPage,int> rank=p=>{int i=Array.IndexOf(WorksOrder,p.Text);return i>=0?i*2:Array.IndexOf(WorksOrder,"Construction Schedule")*2-1;};
  var ordered=all.Select((p,i)=>new{p,i}).OrderBy(x=>rank(x.p)).ThenBy(x=>x.i).Select(x=>x.p).ToArray();if(ordered.SequenceEqual(all))return;
  var selected=tabs.SelectedTab;tabs.SuspendLayout();try{tabs.TabPages.Clear();tabs.TabPages.AddRange(ordered);tabs.SelectedTab=selected??ordered[0];}finally{tabs.ResumeLayout();}}
}
}
