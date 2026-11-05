using System;using System.Linq;using System.Collections.Generic;using System.Drawing;using System.Windows.Forms;
namespace BeamScheduleFormatter {
class GravelTakeoff {
 public static double Volume(double length,double width,double thicknessMm,int quantity){SteelWeight.Positive(length,width,thicknessMm);SteelWeight.Count(quantity);return length*width*thicknessMm/1000*quantity;}
}
partial class ProgramOfWorksForm {
 DataGridView gravelEntries,gravelSummaryGrid;
 void AddGravelEstimate(){
  gravelEntries=Grid(new[]{"Gravel entry ID","Description / other area","Length m","Width m","Thickness mm","Qty","Installed gravel m3","Gravel TO BUY m3","Status"},6);
  SetDefaults(gravelEntries,new Dictionary<int,string>{{4,"100"},{5,"1"}});
  AddGrid("Gravel Estimate",gravelEntries,"Beam and footing gravel is included automatically in the Start summary. Add manual rows only for OTHER areas to avoid double-counting. Installed volume=L*W*thickness/1000*qty. Purchase allowance comes from Earthworks & Gravel > Start; soil swell/shrink is not added to gravel. Manual gravel does not create excavation or backfill deductions.");
  var start=tabs.TabPages.Cast<TabPage>().Single(p=>p.Text=="Earthworks Start").Controls.OfType<FlowLayoutPanel>().Single();start.Controls.Add(new Label{Text="GRAVEL REQUIRED QUANTITIES & SOURCES",Width=1100,Height=38,Font=new Font("Segoe UI",12,FontStyle.Bold),ForeColor=PowTheme.Ink});gravelSummaryGrid=Grid(new[]{"Gravel quantity","Quantity m3"},0);gravelSummaryGrid.ReadOnly=true;gravelSummaryGrid.AllowUserToAddRows=false;gravelSummaryGrid.AllowUserToDeleteRows=false;gravelSummaryGrid.Dock=DockStyle.None;gravelSummaryGrid.Width=1100;gravelSummaryGrid.Height=380;gravelSummaryGrid.Columns[0].Width=650;gravelSummaryGrid.Columns[1].Width=160;start.Controls.Add(gravelSummaryGrid);
 }
 void RecalculateGravel(){if(gravelEntries==null||gravelAllowance==null)return;var ids=new HashSet<string>(StringComparer.OrdinalIgnoreCase);foreach(DataGridViewRow row in gravelEntries.Rows){if(row.IsNewRow)continue;row.Tag=null;for(int n=6;n<=8;n++)row.Cells[n].Value="";if(Empty(row))continue;try{if(Cell(row,0)==""||Cell(row,1)==""||!ids.Add(Cell(row,0)))throw new Exception("Enter a unique gravel ID and description.");double volume=GravelTakeoff.Volume(Number(row,2),Number(row,3),Number(row,4),Count(row,5));row.Tag=volume;row.Cells[6].Value=volume;row.Cells[7].Value=volume*(1+(double)gravelAllowance.Value/100);row.Cells[8].Value="Estimated";}catch(Exception ex){row.Cells[8].Value=ex.Message;errors.Add("Gravel "+Cell(row,0)+": "+ex.Message);}}}
 double ManualGravelVolume(){return gravelEntries==null?0:gravelEntries.Rows.Cast<DataGridViewRow>().Where(r=>r.Tag is double).Sum(r=>(double)r.Tag);}
 double TotalGravelInstalled(){return beam.WorksGravel()+footingResults.Sum(q=>q.Gravel)+ManualGravelVolume();}
 List<object[]> GravelSummary(){double installed=TotalGravelInstalled();return new List<object[]>{new object[]{"Beam gravel (automatic)",beam.WorksGravel(),"m3"},new object[]{"Footing gravel (automatic)",footingResults.Sum(q=>q.Gravel),"m3"},new object[]{"Other gravel areas (manual entries)",ManualGravelVolume(),"m3"},new object[]{"TOTAL gravel installed",installed,"m3"},new object[]{"TOTAL GRAVEL TO BUY",installed*(1+(double)gravelAllowance.Value/100),"m3"}};}
 void UpdateGravelSummary(){if(gravelSummaryGrid==null||gravelAllowance==null)return;gravelSummaryGrid.Rows.Clear();var totals=GravelSummary();for(int n=0;n<totals.Count;n++){gravelSummaryGrid.Rows.Add(totals[n][0],Convert.ToDouble(totals[n][1]).ToString("N2"));foreach(var source in GravelQuantitySources(n)){int detail=gravelSummaryGrid.Rows.Add("    "+source.Description,source.Quantity.ToString("N2"));gravelSummaryGrid.Rows[detail].DefaultCellStyle.ForeColor=PowTheme.Muted;}}}
}
}
