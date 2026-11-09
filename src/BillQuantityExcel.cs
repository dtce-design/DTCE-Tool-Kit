using System;using System.Collections.Generic;using System.Drawing;
namespace BeamScheduleFormatter {
sealed partial class ExcelLiveLink {
 const string BillSheet="DTCE Bill of Quantities",BillSchema="DTCEBillOfQuantitiesV1";
 void ValidateBillSheet(){dynamic sheet=FindEstimateSheet(BillSheet);if(sheet!=null&&Convert.ToString(sheet.Range["AA1"].Value2)!=BillSchema)throw new Exception("The Bill of Quantities worksheet contains custom data. Rename it or choose a new workbook before syncing.");}
 public void UpdateWorksQuantities(List<WorksExcelTable> tables,List<BillQuantity> quantities,ProjectInformation project){ValidateBillSheet();UpdateWorksQuantities(tables);bool events=Convert.ToBoolean(app.EnableEvents);try{app.EnableEvents=false;WriteBill(quantities,project);app.CalculateFull();book.Save();}finally{app.EnableEvents=events;}}
 void WriteBill(List<BillQuantity> quantities,ProjectInformation project){dynamic sheet=FindEstimateSheet(BillSheet);if(sheet==null)sheet=NewOutputSheet(BillSheet);sheet.UsedRange.UnMerge();sheet.UsedRange.Clear();sheet.Range["AA1"].Value2=BillSchema;sheet.Range["A1:D13"].NumberFormat="@";
  foreach(string range in new[]{"A1:D1","A2:D2","A4:D4","B6:D6","B7:D7","B9:D9","B10:D10"})sheet.Range[range].Merge();
  sheet.Range["A1"].Value2="Republic of the Philippines";sheet.Range["A2"].Value2="DEPARTMENT OF PUBLIC WORKS AND HIGHWAYS";sheet.Range["A4"].Value2="BILL OF QUANTITIES AND DETAILED QUANTITY TAKE-OFF / CALCULATIONS";
  sheet.Range["A6"].Value2="Implementing Office";sheet.Range["B6"].Value2=project.Office;sheet.Range["A7"].Value2="Address";sheet.Range["B7"].Value2=project.OfficeAddress;
  sheet.Range["A9"].Value2="Project Name";sheet.Range["B9"].Value2=project.Name;sheet.Range["A10"].Value2="Project Location";sheet.Range["B10"].Value2=project.Location;
  var rows=BillQuantityLayout.Rows(quantities);var values=new object[rows.Count+1,4];values[0,0]="ITEM NO.";values[0,1]="DESCRIPTION";values[0,2]="QUANTITY";values[0,3]="UNIT";
  sheet.Range["A13:B"+Math.Max(14,13+rows.Count)].NumberFormat="@";
  for(int i=0;i<rows.Count;i++){var row=rows[i];values[i+1,0]=row.Item;values[i+1,1]=row.Description;values[i+1,2]=row.Quantity.HasValue?(object)row.Quantity.Value:null;values[i+1,3]=row.Unit;}
  int last=13+rows.Count;sheet.Range["A13:D"+last].Value2=values;sheet.Range["A1:D"+last].Font.Name="Arial";sheet.Range["A1:D"+last].Font.Size=10;sheet.Range["A1:D4"].HorizontalAlignment=-4108;sheet.Range["A2:D4"].Font.Bold=true;sheet.Range["A4"].Font.Size=12;
  sheet.Columns[1].ColumnWidth=22;sheet.Columns[2].ColumnWidth=80;sheet.Columns[3].ColumnWidth=17;sheet.Columns[4].ColumnWidth=12;sheet.Range["A1:D"+last].WrapText=true;sheet.Range["A1:D"+last].VerticalAlignment=-4160;sheet.Range["C14:C"+Math.Max(14,last)].NumberFormat="#,##0.00";
  sheet.Range["A13:D13"].Font.Bold=true;sheet.Range["A13:D13"].HorizontalAlignment=-4108;sheet.Range["A13:D"+last].Borders.LineStyle=1;
  for(int i=0;i<rows.Count;i++){dynamic range=sheet.Range["A"+(14+i)+":D"+(14+i)];if(rows[i].Section){range.Font.Bold=true;range.Interior.Color=ColorTranslator.ToOle(Color.FromArgb(225,233,240));}else if(rows[i].Source){range.Font.Color=ColorTranslator.ToOle(Color.DimGray);sheet.Cells[14+i,2].IndentLevel=1;}else range.Font.Bold=true;}
  sheet.Range["A1:D"+last].Rows.AutoFit();sheet.Rows[4].RowHeight=36;sheet.Rows[6].RowHeight=30;sheet.Rows[7].RowHeight=30;sheet.Rows[9].RowHeight=45;sheet.Rows[10].RowHeight=30;sheet.Rows[13].RowHeight=30;sheet.PageSetup.PrintArea="$A$1:$D$"+last;sheet.PageSetup.PrintTitleRows="$1:$13";sheet.PageSetup.Orientation=2;sheet.PageSetup.Zoom=false;sheet.PageSetup.FitToPagesWide=1;sheet.PageSetup.FitToPagesTall=false;sheet.Activate();
 }
}
}
