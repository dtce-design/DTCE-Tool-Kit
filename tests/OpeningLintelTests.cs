using System;using System.Linq;using System.Reflection;using System.Windows.Forms;using BeamScheduleFormatter;
/* v6.0.32: an opening placed in a CHB wall gets its lintel automatically (Lintel Beam Design Calculation Report schedule) */
class OpeningLintelTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Public|BindingFlags.Static;
 [STAThread]static void Main(){AccessCodes.Granted=true;var pow=new ProgramOfWorksForm();var t=typeof(ProgramOfWorksForm);
  var wood=(DataGridView)t.GetField("wood",F).GetValue(pow);var walls=(DataGridView)t.GetField("walls",F).GetValue(pow);
  int w=walls.Rows.Add();var wr=walls.Rows[w];wr.Cells[0].Value="W1";wr.Cells[1].Value="Exterior wall";wr.Cells[2].Value="6";wr.Cells[3].Value="3";wr.Cells[4].Value="1";wr.Cells[18].Value="150";
  int r=wood.Rows.Add();var dr=wood.Rows[r];dr.Cells[0].Value="D1";dr.Cells[1].Value="Main door";dr.Cells[3].Value="Door";dr.Cells[4].Value="0.9";dr.Cells[5].Value="2.1";dr.Cells[6].Value="2";
  var links=(System.Collections.Generic.List<OpeningLink>)t.GetField("openingAssignments",F).GetValue(pow);links.Add(new OpeningLink{Schedule="D1",Wall="W1",Quantity=2});
  t.GetMethod("Recalculate",F,null,Type.EmptyTypes,null).Invoke(pow,null);
  var misc=(System.Collections.Generic.List<MiscStructureQuantity>)t.GetField("miscQuantities",F).GetValue(pow);var lintels=misc.Where(q=>q.Kind=="Lintel Beam").ToList();
  Check(lintels.Count==1&&lintels[0].Id.StartsWith("LB-6A")&&lintels[0].Quantity==2,"the door in the 150 mm wall gets LB-6A x 2 ("+string.Join(", ",misc.Select(q=>q.Kind+" "+q.Id))+")");
  var grid=(DataGridView)t.GetField("lintelGrid",F).GetValue(pow);Check(grid!=null&&grid.Rows.Count==1&&Convert.ToString(grid.Rows[0].Cells[0].Value)=="LB-6A","listed under Lintels over Openings");
  var auto=(CheckBox)t.GetField("autoLintels",F).GetValue(pow);auto.Checked=false;t.GetMethod("Recalculate",F,null,Type.EmptyTypes,null).Invoke(pow,null);
  Check(!((System.Collections.Generic.List<MiscStructureQuantity>)t.GetField("miscQuantities",F).GetValue(pow)).Any(q=>q.Kind=="Lintel Beam"),"turned off: no automatic lintels");
  Console.WriteLine("PASS: "+checks+" automatic lintel checks");pow.Dispose();}
}
