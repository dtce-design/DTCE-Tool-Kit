using System;using System.Linq;using System.Collections.Generic;using System.Globalization;using System.Windows.Forms;
namespace BeamScheduleFormatter {
partial class ProgramOfWorksForm {
 void FilterFasciaSections(DataGridViewRow row){
  string kind=Cell(row,2);var cell=row.Cells[3] as DataGridViewComboBoxCell;if(cell==null)return;
  var options=SteelCatalog.Profiles.Concat(projectSteelProfiles.Values).Where(p=>p.Kind==kind).Select(p=>p.Name).Distinct().ToArray();string previous=Cell(row,3);
  var prior=FindSteelProfile(previous);if(previous!=""&&!options.Contains(previous)){row.Cells[3].Value=prior!=null&&options.Length>0?options[0]:null;previous=Cell(row,3);}
  if(!cell.Items.Cast<object>().Select(Convert.ToString).SequenceEqual(options)){cell.Items.Clear();cell.Items.AddRange(options.Cast<object>().ToArray());}cell.ErrorText="";
 }
 void CalculatePlatePurchases(){
  var rows=steelPlates.Rows.Cast<DataGridViewRow>().Where(r=>r.Tag is PlateDemand).ToList();if(rows.Sum(r=>(long)((PlateDemand)r.Tag).Quantity)>5000){errors.Add("Steel plate fitting supports up to 5000 cuts per project.");return;}
  foreach(var thickness in rows.GroupBy(r=>((PlateDemand)r.Tag).ThicknessMm)){
   try{plateStocks.AddRange(PlateCutPlanner.Allocate(thickness.Select(r=>(PlateDemand)r.Tag)));}
   catch(Exception ex){foreach(var row in thickness)row.Cells[steelPlates.Columns.Count-1].Value=ex.Message;errors.Add("Steel plate purchasing: "+ex.Message);}
  }
  foreach(var row in rows){var demand=(PlateDemand)row.Tag;var used=plateStocks.Where(s=>s.Cuts.Any(c=>c.Demand==demand)).ToList();if(used.Count==0)continue;
   double buy=used.Sum(s=>s.BuyKg*s.Cuts.Where(c=>c.Demand==demand).Sum(c=>c.Width*c.Length)/s.NetArea);
   row.Cells[9].Value=used.Count;row.Cells[10].Value=string.Join("; ",used.GroupBy(s=>s.Format.Name).Select(g=>g.Count()+" x "+g.Key).ToArray());Measure(row,11,buy);Measure(row,12,Math.Max(0,buy-demand.Area*demand.Quantity*demand.ThicknessMm/1000*SteelWeight.Density));
  }
 }
}
}
