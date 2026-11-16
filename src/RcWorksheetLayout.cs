using System;using System.Linq;using System.Drawing;using System.Windows.Forms;
namespace BeamScheduleFormatter {
static class RcWorksheetLayout {
 internal static void Grid(DataGridView grid){grid.ScrollBars=ScrollBars.Both;grid.Dock=DockStyle.Fill;grid.Margin=Padding.Empty;grid.BorderStyle=BorderStyle.None;grid.AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;grid.DefaultCellStyle.WrapMode=DataGridViewTriState.True;}
 internal static void Embedded(Form owner,DataGridView input,TabControl summaries,Control oldHost,Control tools,Label source,string title,string guide){
  TopTabLayout.Promote(owner,summaries,input,oldHost,tools,"Measurements",source);
  foreach(TabPage page in summaries.TabPages){page.Padding=new Padding(14,10,14,14);page.AutoScroll=false;}
  var heading=new WorksheetHeading{Text=title,Subtitle="Enter measurements below  ·  Calculated quantities update automatically"};heading.SetGuide(guide);summaries.TabPages[0].Controls.Add(heading);summaries.TabPages[0].Controls.SetChildIndex(heading,summaries.TabPages[0].Controls.Count-1);
  summaries.SelectedIndexChanged+=(s,e)=>{var page=summaries.SelectedTab;if(page!=null)foreach(var h in page.Controls.OfType<WorksheetHeading>())page.Controls.SetChildIndex(h,page.Controls.Count-1);};
  foreach(var label in owner.Controls.OfType<Label>().Where(l=>l.Dock==DockStyle.Top))label.Visible=false;
  Grid(input);
 }
}
partial class EstimatorForm {
 internal void ConfigureRcWorkspace(){var summaries=(TabControl)totals.Parent.Parent;var tools=Controls.OfType<FlowLayoutPanel>().First(p=>p.Controls.OfType<Button>().Any(b=>b.Text=="Load beam schedule DXF"));RcWorksheetLayout.Embedded(this,beams,summaries,summaries.Parent,tools,sourceLabel,"Beam Estimate",sourceLabel.Text);foreach(var g in new[]{totals,gradeTotals})RcWorksheetLayout.Grid(g);}
}
partial class SlabEstimatorForm {
 internal void ConfigureRcWorkspace(){var summaries=(TabControl)gradeTotals.Parent.Parent;var tools=Controls.OfType<FlowLayoutPanel>().First(p=>p.Controls.OfType<Button>().Any(b=>b.Text=="Load / replace DXF"));string guide=Controls.OfType<Label>().Where(l=>l.Dock==DockStyle.Top&&l!=loadedDxf).Select(l=>l.Text).FirstOrDefault()??"Load a slab DXF or enter a manual reinforcement schedule, then enter dimensions and quantity.";RcWorksheetLayout.Embedded(this,grid,summaries,summaries,tools,loadedDxf,"Slab Estimate",guide);RcWorksheetLayout.Grid(gradeTotals);}
}
partial class ProgramOfWorksForm {
 void ConfigureRcWorkspaces(){beam.ConfigureRcWorkspace();slab.ConfigureRcWorkspace();foreach(var g in new[]{footings,rcWalls,canopies}.Where(g=>g!=null))RcWorksheetLayout.Grid(g);foreach(var item in extraGrids.Values.Where(i=>i.Group=="Reinforced Concrete Works"||i.Group=="Miscellaneous"||i.Key=="WallFooting"))RcWorksheetLayout.Grid(item.Grid);}
}
}
