using System;using System.Linq;using System.Windows.Forms;using BeamScheduleFormatter;
class WallOpeningRowActionUiTests {
 static int checks;static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
 [STAThread]static void Main(){
  foreach(bool hideThickness in new[]{false,true})using(var grid=new DataGridView()){
   // Reproduce WallGrid's data indexes and display order before attaching the action.
   for(int i=0;i<20;i++)grid.Columns.Add("C"+i,"Column "+i);
   int display=0;foreach(int i in new[]{0,1,18,2,3,4,19,8,9,12,15,16,17,11})grid.Columns[i].DisplayIndex=display++;
   grid.Columns[0].Frozen=true;grid.Columns[1].Frozen=true;grid.Columns[18].Frozen=true;
   grid.Columns[18].Visible=!hideThickness;
   var original=grid.Columns.Cast<DataGridViewColumn>().ToArray();
   grid.Rows.Add("W-001","Wall A");grid.Rows[0].Cells[18].Value="150";
   WallOpeningRowAction.Attach(grid,row=>{});
   Check(grid.Columns.Count==21,"One action column appended");
   Check(original.Select((column,index)=>grid.Columns[index]==column).All(x=>x),"Original data-column indexes preserved");
   var action=grid.Columns["EditWallOpeningsAction"];
   Check(action.DisplayIndex==2&&action.Frozen,"Icon frozen beside description");
   Check(grid.Columns[0].Frozen&&grid.Columns[1].Frozen&&grid.Columns[18].Frozen,"Original frozen identifiers retained");
   Check(grid.Columns.Cast<DataGridViewColumn>().Where(c=>c.Frozen).OrderBy(c=>c.DisplayIndex).Select(c=>c.DisplayIndex).SequenceEqual(new[]{0,1,2,3}),"Frozen columns form a contiguous prefix");
   Check(original.Where(c=>c.Index!=0&&c.Index!=1&&c.Index!=18).All(c=>!c.Frozen),"Measurement columns remain scrollable");
   Check(Convert.ToString(grid.Rows[0].Cells[18].Value)=="150"&&grid.Columns[18].Visible==!hideThickness,"Thickness data and visibility preserved");
   WallOpeningRowAction.Attach(grid,row=>{});Check(grid.Columns.Count==21,"Repeated attachment does not duplicate action");
  }
  Console.WriteLine(checks+" native wall-opening column regression checks passed.");
 }
}
