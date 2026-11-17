using System;using System.Linq;using System.Drawing;using System.Windows.Forms;
namespace BeamScheduleFormatter {
static class RcWorksheetLayout {
 internal static void Grid(DataGridView grid){grid.ScrollBars=ScrollBars.Both;grid.Dock=DockStyle.Fill;grid.Margin=Padding.Empty;grid.BorderStyle=BorderStyle.None;grid.AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;grid.DefaultCellStyle.WrapMode=DataGridViewTriState.True;PowTheme.ApplyCompact(grid);}
 internal static void Embedded(Form owner,DataGridView input,TabControl summaries,Control oldHost,Control tools,Label source,string title,string guide){
  owner.SuspendLayout();try{
   var pages=summaries.TabPages.Cast<TabPage>().ToArray();foreach(var page in pages)summaries.TabPages.Remove(page);
   if(summaries.Parent!=null)summaries.Parent.Controls.Remove(summaries);
   if(oldHost!=null&&oldHost!=summaries&&oldHost.Parent!=null)oldHost.Parent.Controls.Remove(oldHost);
   var navigation=new NavigationTabs{Dock=DockStyle.Fill,AccessibleName=title+" workspace tabs"};
   var measurements=new TabPage("Measurements"){Name="Measurements",Padding=new Padding(14,10,14,14),AutoScroll=false};input.Dock=DockStyle.Fill;measurements.Controls.Add(input);input.Visible=true;
   if(source!=null){source.Dock=DockStyle.Top;measurements.Controls.Add(source);}
   var heading=new WorksheetHeading{Text=title,Subtitle="Enter measurements below  ·  Calculated quantities update automatically"};heading.SetGuide(guide);measurements.Controls.Add(heading);
   navigation.TabPages.Add(measurements);foreach(var page in pages){page.Padding=new Padding(14,10,14,14);page.AutoScroll=false;navigation.TabPages.Add(page);}
   summaries.Dispose();if(oldHost!=null&&oldHost!=summaries)oldHost.Dispose();
   foreach(var label in owner.Controls.OfType<Label>().Where(l=>l.Dock==DockStyle.Top))label.Visible=false;
   owner.Controls.Add(navigation);owner.Controls.SetChildIndex(navigation,0);tools.Dock=DockStyle.Top;owner.Controls.SetChildIndex(tools,owner.Controls.Count-1);navigation.SelectedTab=measurements;
   owner.Shown+=(s,e)=>{if(!navigation.IsDisposed){navigation.SelectedTab=measurements;input.Visible=true;}};
  }finally{owner.ResumeLayout(true);}
  Grid(input);
 }
}
partial class EstimatorForm {
 internal void ConfigureRcWorkspace(){var summaries=(TabControl)gradeTotals.Parent.Parent;var tools=Controls.OfType<FlowLayoutPanel>().First(p=>p.Controls.OfType<Button>().Any(b=>b.Text=="Load beam schedule DXF"));RcWorksheetLayout.Embedded(this,beams,summaries,summaries.Parent,tools,sourceLabel,"Beam Estimate",sourceLabel.Text);foreach(var g in new[]{totals,gradeTotals})RcWorksheetLayout.Grid(g);}
}
partial class SlabEstimatorForm {
 internal void ConfigureRcWorkspace(){var summaries=(TabControl)gradeTotals.Parent.Parent;var tools=Controls.OfType<FlowLayoutPanel>().First(p=>p.Controls.OfType<Button>().Any(b=>b.Text=="Load / replace DXF"));string guide=Controls.OfType<Label>().Where(l=>l.Dock==DockStyle.Top&&l!=loadedDxf).Select(l=>l.Text).FirstOrDefault()??"Load a slab DXF or enter a manual reinforcement schedule, then enter dimensions and quantity.";RcWorksheetLayout.Embedded(this,grid,summaries,summaries,tools,loadedDxf,"Slab Estimate",guide);RcWorksheetLayout.Grid(gradeTotals);}
}
partial class ProgramOfWorksForm {
 void ConfigureRcWorkspaces(){beam.ConfigureRcWorkspace();slab.ConfigureRcWorkspace();foreach(var g in new[]{footings,rcWalls,canopies}.Where(g=>g!=null))RcWorksheetLayout.Grid(g);foreach(var item in extraGrids.Values.Where(i=>i.Group=="Reinforced Concrete Works"||i.Group=="Miscellaneous"||i.Key=="WallFooting"))RcWorksheetLayout.Grid(item.Grid);}
}
}
