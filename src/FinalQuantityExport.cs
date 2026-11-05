using System;using System.Linq;using System.Collections.Generic;
namespace BeamScheduleFormatter {
partial class ProgramOfWorksForm {
 internal List<WorksExcelTable> FinalQuantityTables(){
  var full=WorksExcelTables();var tables=full.Where(t=>t.Name==WorksSummaryLayout.Sheet).ToList();var totals=new List<object[]>();
  foreach(string name in new[]{"POW Masonry Summary","POW Roofing Summary","POW Structural Steel Summary","POW Overall Earthworks"}){var table=full.Single(t=>t.Name==name);for(int r=0;r<table.Rows.GetLength(0);r++)totals.Add(new object[]{Convert.ToString(table.Rows[r,0]),table.Rows[r,1],table.Rows[r,2]});}
  tables.Add(new WorksExcelTable(WorksSummaryLayout.Sheet,new[]{"FINAL QUANTITY","Quantity","Unit"},totals,1,8));return tables;
 }
}
}
