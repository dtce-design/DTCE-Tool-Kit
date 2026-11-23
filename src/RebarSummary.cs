using System;using System.Collections.Generic;using System.Drawing;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// One "Rebar by grade and diameter" summary (owner, 17 November 2026): Grade 40 and Grade 60 rows per diameter with
// grade totals, then all grades per diameter and the grand total. Replaces the separate "Grade 40 / Grade 60" and
// "By diameter" tabs of the beam estimate and is added to the other RC sheets (column, footing, wall footing,
// canopy / ledge, RC wall, miscellaneous structures).
static class RebarSummary {
 internal const string Caption="Rebar by grade and diameter";
 internal static readonly string[] Headers={"GRADE","DIA mm","NET kg","LAP+OFFCUT kg","BUY kg"};
 static string F(double v){return v.ToString("N2",CultureInfo.CurrentCulture);}
 internal static void Fill(DataGridView g,IEnumerable<StockAllocation> source){var plan=source.ToList();g.Rows.Clear();
  if(plan.Count==0){g.Rows.Add("No reinforcement on this sheet yet","","","","");return;}
  var bold=new Font(g.Font,FontStyle.Bold);var grades=plan.Select(a=>a.Demand.Grade).Distinct().OrderBy(x=>x).ToList();
  foreach(int grade in grades){var steel=plan.Where(a=>a.Demand.Grade==grade).ToList();foreach(var d in steel.GroupBy(a=>a.Demand.Diameter).OrderBy(x=>x.Key))g.Rows.Add("G"+grade,"D"+d.Key,F(d.Sum(a=>a.Net)),F(d.Sum(a=>a.Splice+a.Waste)),F(d.Sum(a=>a.Purchase)));
   int r=g.Rows.Add("G"+grade+" TOTAL","",F(steel.Sum(a=>a.Net)),F(steel.Sum(a=>a.Splice+a.Waste)),F(steel.Sum(a=>a.Purchase)));g.Rows[r].DefaultCellStyle.Font=bold;}
  if(grades.Count>1)foreach(var d in plan.GroupBy(a=>a.Demand.Diameter).OrderBy(x=>x.Key))g.Rows.Add("ALL GRADES","D"+d.Key,F(d.Sum(a=>a.Net)),F(d.Sum(a=>a.Splice+a.Waste)),F(d.Sum(a=>a.Purchase)));
  int t=g.Rows.Add("GRAND TOTAL","",F(plan.Sum(a=>a.Net)),F(plan.Sum(a=>a.Splice+a.Waste)),F(plan.Sum(a=>a.Purchase)));g.Rows[t].DefaultCellStyle.Font=bold;}
}
partial class ProgramOfWorksForm {
 Dictionary<string,TabPage> sheetPages=new Dictionary<string,TabPage>();readonly Dictionary<string,DataGridView> rebarSummaries=new Dictionary<string,DataGridView>(StringComparer.Ordinal);
 static readonly string[] RebarSummaryPages={"Column Estimate","Footing Estimate","Wall Footing Estimate","RC Wall Estimate","Miscellaneous Structures"};
 // Wraps each RC sheet as "Measurements" + "Rebar by grade and diameter", like the beam estimate workspace.
 void AddRebarSummaryTabs(){foreach(string name in RebarSummaryPages){TabPage page;if(!sheetPages.TryGetValue(name,out page)||page==null||rebarSummaries.ContainsKey(name))continue;
   /* Miscellaneous Structures already has its own sub tabs: the rebar summary is one more sub tab beside them (owner, 21 November
      2026: "ipagsama na sana sa sub tab kagaya sa ibang RC Works") */
   var inner=page.Controls.OfType<TabControl>().FirstOrDefault();
   if(name=="Miscellaneous Structures"&&inner!=null){var tab=new TabPage(RebarSummary.Caption){Padding=new Padding(14,10,14,14)};var g2=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,BackgroundColor=Color.White,BorderStyle=BorderStyle.None};
    foreach(string h in RebarSummary.Headers)g2.Columns.Add(h,h);foreach(DataGridViewColumn c in g2.Columns){c.SortMode=DataGridViewColumnSortMode.NotSortable;if(c.Index>=2)c.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;}
    var reuse2=new Label{Dock=DockStyle.Top,Height=30,Font=ModernUi.Strong,ForeColor=PowTheme.Teal,TextAlign=ContentAlignment.MiddleLeft};offcutLabels.Add(reuse2);tab.Controls.Add(g2);tab.Controls.Add(reuse2);
    var existing=inner.TabPages.Cast<TabPage>().ToList();inner.TabPages.Clear();foreach(var tp in existing)inner.TabPages.Add(tp);inner.TabPages.Add(tab);rebarSummaries[name]=g2;
    inner.SelectedIndexChanged+=(s,e)=>{if(inner.SelectedTab==tab)RefreshRebarSummaries();};continue;}
   var content=page.Controls.Cast<Control>().ToArray();page.SuspendLayout();page.Controls.Clear();var navigation=new NavigationTabs{Dock=DockStyle.Fill,AccessibleName=name+" workspace tabs"};
   var measurements=new TabPage("Measurements"){Name="Measurements",Padding=page.Padding};measurements.Controls.AddRange(content);var summaryPage=new TabPage(RebarSummary.Caption){Padding=new Padding(14,10,14,14)};
   var grid=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,BackgroundColor=Color.White,BorderStyle=BorderStyle.None};
   foreach(string h in RebarSummary.Headers)grid.Columns.Add(h,h);foreach(DataGridViewColumn c in grid.Columns){c.SortMode=DataGridViewColumnSortMode.NotSortable;if(c.Index>=2)c.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;}
   var reuse=new Label{Dock=DockStyle.Top,Height=30,Font=ModernUi.Strong,ForeColor=PowTheme.Teal,TextAlign=ContentAlignment.MiddleLeft};offcutLabels.Add(reuse);summaryPage.Controls.Add(grid);summaryPage.Controls.Add(reuse);navigation.TabPages.Add(measurements);navigation.TabPages.Add(summaryPage);page.Padding=Padding.Empty;page.Controls.Add(navigation);page.ResumeLayout(true);rebarSummaries[name]=grid;
   navigation.SelectedIndexChanged+=(s,e)=>{if(navigation.SelectedTab==summaryPage)RefreshRebarSummaries();};}}
 /* one command row above the subtabs on every RC estimate, as on Beam and Slab (owner, 21 November 2026: "di mo naman pinagpareprehas"):
    the page's own actions, then Schedules and Settings menus */
 void ArrangeRcCommandBars(){var click=typeof(Control).GetMethod("OnClick",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
  foreach(string name in RebarSummaryPages){TabPage page;if(!sheetPages.TryGetValue(name,out page)||page==null)continue;var nav=page.Controls.OfType<TabControl>().FirstOrDefault();if(nav==null)continue;var meas=nav.TabPages.Cast<TabPage>().FirstOrDefault(t=>t.Name=="Measurements");if(meas==null)continue;
   var bar=meas.Controls.OfType<FlowLayoutPanel>().FirstOrDefault(f=>f.Controls.OfType<Button>().Any()||f.Controls.OfType<CheckBox>().Any());if(bar==null)continue;
   var buttons=bar.Controls.OfType<Button>().ToList();var checks=bar.Controls.OfType<CheckBox>().ToList();meas.Controls.Remove(bar);
   var top=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Padding=new Padding(8,6,8,4),WrapContents=true};
   Func<string,Button> add=t=>{var b=new Button{Text=t,AutoSize=true,Height=38};top.Controls.Add(b);return b;};
   Action<Button> forward=b0=>{var b=add(b0.Text);b.Click+=(s,e)=>click.Invoke(b0,new object[]{EventArgs.Empty});};
   var schedules=buttons.Where(b=>b.Text.StartsWith("Manual ")).ToList();var settings=buttons.Where(b=>b.Text=="Settings"||b.Text=="Commercial lengths").ToList();
   foreach(var b0 in buttons.Except(schedules).Except(settings))forward(b0);
   Action<string,List<ToolStripItem>> menu=(t,items)=>{if(items.Count==0)return;var m=new ContextMenuStrip();m.Items.AddRange(items.ToArray());var b=add(t+"  ▾");b.Click+=(s,e)=>m.Show(b,new Point(0,b.Height));};
   menu("Schedules",schedules.Select(b0=>{var it=new ToolStripMenuItem(b0.Text);it.Click+=(s,e)=>click.Invoke(b0,new object[]{EventArgs.Empty});return (ToolStripItem)it;}).ToList());
   var setItems=settings.OrderBy(b=>b.Text=="Settings"?0:1).Select(b0=>{var it=new ToolStripMenuItem(b0.Text=="Settings"?"Materials & calculation":b0.Text);it.Click+=(s,e)=>click.Invoke(b0,new object[]{EventArgs.Empty});return (ToolStripItem)it;}).ToList();
   foreach(var c0 in checks){var cb=c0;var it=new ToolStripMenuItem(cb.Text){CheckOnClick=true,Checked=cb.Checked};it.CheckedChanged+=(s,e)=>cb.Checked=it.Checked;cb.CheckedChanged+=(s,e)=>it.Checked=cb.Checked;setItems.Add(it);}
   if(!settings.Any(b=>b.Text=="Settings")){var it=new ToolStripMenuItem("Materials & calculation");it.Click+=(s,e)=>ShowWorksSettings();setItems.Insert(0,it);}
   menu("Settings",setItems);
   page.Controls.Add(top);page.Controls.SetChildIndex(top,page.Controls.Count-1);}}
 IEnumerable<StockAllocation> RebarPlanFor(string name){switch(name){
  case "Footing Estimate":return footingPlan;case "Wall Footing Estimate":return wallFootingPlan;case "Canopy / Ledge Estimate":return canopyPlan;case "RC Wall Estimate":return rcWallPlan;
  default:{TabPage page;if(!sheetPages.TryGetValue(name,out page))return Enumerable.Empty<StockAllocation>();var steel=new HashSet<EstimateInput>(GridsIn(page).SelectMany(g=>g.Rows.Cast<DataGridViewRow>()).Select(r=>r.Tag as MiscStructureQuantity).Where(q=>q!=null&&q.Steel!=null).Select(q=>q.Steel));/* the automatic lintels over openings are Miscellaneous Structures rebar too (owner, 23 November 2026: "walang nagpapakita") */if(name=="Miscellaneous Structures")foreach(var q in lintelResults)if(q.Steel!=null)steel.Add(q.Steel);var misc=miscPlan.Where(a=>steel.Contains(a.Entry));return name=="Miscellaneous Structures"?misc.Concat(canopyPlan):misc;}}}
 static IEnumerable<DataGridView> GridsIn(Control root){foreach(Control c in root.Controls){var g=c as DataGridView;if(g!=null)yield return g;else foreach(var inner in GridsIn(c))yield return inner;}}
 readonly List<Label> offcutLabels=new List<Label>();
 internal string OffcutReuseText(){double saved=rebarAlone-rebarShared;if(rebarShared<=0)return "Offcut reuse: no rebar yet.";return "Offcut reuse across the project (beam, column, slab, footing, wall footing, RC wall, canopy, lintels and other members): purchase "+rebarShared.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+" kg instead of "+rebarAlone.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+" kg if each workspace (and each miscellaneous member) were cut alone - "+Math.Max(0,saved).ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+" kg saved by cutting short pieces from the leftovers of other members (same grade and diameter). Plumbing structures share their offcuts among themselves as separate items.";}
 void RefreshRebarSummaries(){foreach(var l in offcutLabels)if(!l.IsDisposed)l.Text=OffcutReuseText();foreach(var item in rebarSummaries){if(item.Value.IsDisposed)continue;try{RebarSummary.Fill(item.Value,RebarPlanFor(item.Key));}catch(Exception){}}}
}
}
