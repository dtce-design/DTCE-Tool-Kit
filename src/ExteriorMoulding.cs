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
 List<object[]> ExteriorMouldingTotals(){return MouldingQuantities().Select(q=>new object[]{q.Description,q.Quantity,q.Unit}).ToList();}
 void AddMouldingColumns(DataGridView grid){
  grid.Columns.Add(new DataGridViewCheckBoxColumn{Name="ExteriorMoulding",HeaderText="Exterior moulding",Width=125,TrueValue=true,FalseValue=false});
  grid.Columns.Add(new DataGridViewTextBoxColumn{Name="MouldingLength",HeaderText="Exterior moulding lm",Width=145,ReadOnly=true});
  grid.Columns.Add(new DataGridViewTextBoxColumn{Name="MouldingProfile",HeaderText="Moulding profile / size (mm)",Width=230});
  grid.Columns[24].DefaultCellStyle.BackColor=System.Drawing.Color.FromArgb(255,249,222);grid.Columns[24].ToolTipText="Same profile and size combines with Finishing Works > Moulding. Enter size in mm, e.g. Concrete 150 x 50 mm.";
  grid.Columns[22].DefaultCellStyle.BackColor=System.Drawing.Color.FromArgb(255,249,222);grid.Columns[22].ToolTipText="Check exterior openings only. Window: all four sides. Door: top and two sides, excluding the bottom. One exterior face only.";
  grid.CurrentCellDirtyStateChanged+=(s,e)=>{if(grid.IsCurrentCellDirty&&grid.CurrentCell is DataGridViewCheckBoxCell)grid.CommitEdit(DataGridViewDataErrorContexts.Commit);};
  grid.CellValueChanged+=(s,e)=>{if(!filling&&e.RowIndex>=0&&e.ColumnIndex==22)Recalculate();};
 }
 void AppendMouldingReport(StringBuilder report){report.AppendLine();report.AppendLine("MOULDING | TOTAL LINEAR METRES BY PROFILE / SIZE");foreach(var total in ExteriorMouldingTotals())report.AppendLine(Convert.ToString(total[0])+": "+Convert.ToDouble(total[1]).ToString("0.00",CultureInfo.InvariantCulture)+" lm");foreach(var item in manualMouldings)report.AppendLine(item.Id+" | "+item.Description+" | "+item.Profile+" | "+item.Length.ToString("0.###",CultureInfo.InvariantCulture)+"m x "+item.Quantity+" = "+item.Total.ToString("0.00",CultureInfo.InvariantCulture)+" m");foreach(var opening in openings.Where(o=>o.Exterior&&o.Material!="Manual")){report.AppendLine(opening.Id+" | "+opening.Kind+" | "+(opening.Kind=="Window"?"2(W+H)":"W+2H")+" x quantity");report.AppendLine("  W="+opening.Width.ToString("0.###",CultureInfo.InvariantCulture)+"m; H="+opening.Height.ToString("0.###",CultureInfo.InvariantCulture)+"m; qty="+opening.Quantity+" -> "+opening.MouldingLength.ToString("0.00",CultureInfo.InvariantCulture)+" lm");}report.AppendLine("Opening sources use overall dimensions on the exterior face only; doors exclude the bottom. Manual entries are additional lengths. No mitre or percentage waste allowance.");}
}
}

