using System;using System.Drawing;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* the choice that switches an option on comes first; what depends on it is grey and locked until it is on (owner, 21 November 2026:
   "check box muna bago mamimili, gawin din ito sa iba") */
partial class ProgramOfWorksForm {
 static void Gate(DataGridView g,int control,int[] dependents,Func<DataGridViewRow,bool> on){if(g==null)return;
  try{int at=dependents.Min(d=>g.Columns[d].DisplayIndex);if(g.Columns[control].DisplayIndex>at)g.Columns[control].DisplayIndex=at;}catch(InvalidOperationException){}
  g.CellBeginEdit+=(s,e)=>{if(e.RowIndex>=0&&dependents.Contains(e.ColumnIndex)&&!on(g.Rows[e.RowIndex]))e.Cancel=true;};
  g.CellFormatting+=(s,e)=>{if(e.RowIndex<0||!dependents.Contains(e.ColumnIndex))return;var row=g.Rows[e.RowIndex];if(row.IsNewRow)return;if(!on(row)){e.CellStyle.BackColor=PowTheme.Background;e.CellStyle.ForeColor=PowTheme.Muted;}};
  g.CellValueChanged+=(s,e)=>{if(e.RowIndex>=0&&e.ColumnIndex==control)g.InvalidateRow(e.RowIndex);};}
 void ArrangeCheckFirst(){
  Gate(roofSheets,10,new[]{11},r=>{var v=Convert.ToString(r.Cells[10].Value);return v=="True"||v=="true";});
  if(rcWalls!=null&&rcWalls.Columns.Count>30)try{var order=rcWalls.Columns.Cast<DataGridViewColumn>().Where(c=>c.Index<27||c.Index>30).OrderBy(c=>c.DisplayIndex).ToList();int at=order.IndexOf(rcWalls.Columns[13])+1;order.InsertRange(at,new[]{rcWalls.Columns[27],rcWalls.Columns[28],rcWalls.Columns[29],rcWalls.Columns[30]});for(int pass=0;pass<3;pass++)for(int k=0;k<order.Count;k++)if(order[k].DisplayIndex!=k)try{order[k].DisplayIndex=k;}catch(InvalidOperationException){}}catch(InvalidOperationException){}
  Gate(rcWalls,7,new[]{27,28,29,30},r=>Convert.ToString(r.Cells[7].Value).Trim()=="2");}
}
}
