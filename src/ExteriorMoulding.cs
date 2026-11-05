using System;using System.Linq;using System.Text;using System.Globalization;using System.Collections.Generic;using System.Windows.Forms;
namespace BeamScheduleFormatter {
static class OpeningMoulding {
 public static double Length(string kind,double width,double height,int quantity,bool exterior){
  if(kind!="Door"&&kind!="Window")throw new Exception("Select Door or Window for moulding measurement.");
  SteelWeight.Positive(width,height);SteelWeight.Count(quantity);
  return exterior?(2*height+(kind=="Window"?2*width:width))*quantity:0;
 }
}
partial class ProgramOfWorksForm {
 List<object[]> ExteriorMouldingTotals(){return new List<object[]>{new object[]{"Exterior door moulding (top + 2 sides)",openings.Where(o=>o.Kind=="Door").Sum(o=>o.MouldingLength),"lm"},new object[]{"Exterior window moulding (all 4 sides)",openings.Where(o=>o.Kind=="Window").Sum(o=>o.MouldingLength),"lm"},new object[]{"TOTAL exterior opening moulding",openings.Sum(o=>o.MouldingLength),"lm"}};}
 void AddMouldingColumns(DataGridView grid){
  grid.Columns.Add(new DataGridViewCheckBoxColumn{Name="ExteriorMoulding",HeaderText="Exterior moulding",Width=125,TrueValue=true,FalseValue=false});
  grid.Columns.Add(new DataGridViewTextBoxColumn{Name="MouldingLength",HeaderText="Exterior moulding lm",Width=145,ReadOnly=true});
  grid.Columns[22].DefaultCellStyle.BackColor=System.Drawing.Color.FromArgb(255,249,222);grid.Columns[22].ToolTipText="Check exterior openings only. Window: all four sides. Door: top and two sides, excluding the bottom. One exterior face only.";
  grid.CurrentCellDirtyStateChanged+=(s,e)=>{if(grid.IsCurrentCellDirty&&grid.CurrentCell is DataGridViewCheckBoxCell)grid.CommitEdit(DataGridViewDataErrorContexts.Commit);};
  grid.CellValueChanged+=(s,e)=>{if(!filling&&e.RowIndex>=0&&e.ColumnIndex==22)Recalculate();};
 }
 void AppendMouldingReport(StringBuilder report){report.AppendLine();report.AppendLine("EXTERIOR DOOR / WINDOW MOULDING | LINEAR METRES");foreach(var total in ExteriorMouldingTotals())report.AppendLine(Convert.ToString(total[0])+": "+Convert.ToDouble(total[1]).ToString("0.00",CultureInfo.InvariantCulture)+" lm");foreach(var opening in openings.Where(o=>o.Exterior&&o.Material!="Manual")){report.AppendLine(opening.Id+" | "+opening.Kind+" | "+(opening.Kind=="Window"?"2(W+H)":"W+2H")+" x quantity");report.AppendLine("  W="+opening.Width.ToString("0.###",CultureInfo.InvariantCulture)+"m; H="+opening.Height.ToString("0.###",CultureInfo.InvariantCulture)+"m; qty="+opening.Quantity+" -> "+opening.MouldingLength.ToString("0.00",CultureInfo.InvariantCulture)+" lm");}report.AppendLine("Overall opening dimensions. Exterior face only; no door bottom, leaf multiplier, mitre allowance or percentage waste.");}
}
}
