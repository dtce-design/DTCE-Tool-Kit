using System;using System.Linq;using System.Reflection;using System.Windows.Forms;using BeamScheduleFormatter;
/* Insert row above / below on the Program of Works sheets (v1.50.15) */
class PowInsertRowTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Public;
 static string C(DataGridViewRow r,int c){return Convert.ToString(r.Cells[c].Value);}
 [STAThread]static void Main(){AccessCodes.Granted=true;var pow=new ProgramOfWorksForm();var form=new Form{Width=1200,Height=700};
  var foot=(DataGridView)typeof(ProgramOfWorksForm).GetField("footings",F).GetValue(pow);form.Controls.Add(foot);form.Show();Application.DoEvents();
  foreach(var id in new[]{"F-A","F-B"}){int r=foot.Rows.Add();foot.Rows[r].Cells[0].Value=id;foot.Rows[r].Cells[1].Value="Footing "+id;foot.Rows[r].Cells[2].Value="2";}
  var history=GridEditHistory.Attach(foot);int count=foot.Rows.Cast<DataGridViewRow>().Count(r=>!r.IsNewRow);Check(count==2,"two rows");
  foot.CurrentCell=foot.Rows[0].Cells[1];history.InsertRow(true);
  Check(foot.Rows.Cast<DataGridViewRow>().Count(r=>!r.IsNewRow)==3&&C(foot.Rows[0],0)=="F-A"&&C(foot.Rows[1],0)==""&&C(foot.Rows[2],0)=="F-B","insert below puts a blank row under the selected row");
  Check(foot.CurrentCell.RowIndex==1&&foot.CurrentCell.ColumnIndex==1,"the inserted row is selected in the same column");
  Check(foot.Rows[1].Cells.Cast<DataGridViewCell>().All(c=>c.Value==null||C(foot.Rows[1],c.ColumnIndex)==""),"inserted row stays blank until typed in");
  pow.Recalculate();Check(C(foot.Rows[1],0)=="","a blank inserted row gets no automatic ID");
  foot.CurrentCell=foot.Rows[0].Cells[0];history.InsertRow(false);Check(foot.Rows.Cast<DataGridViewRow>().Count(r=>!r.IsNewRow)==4&&C(foot.Rows[0],0)==""&&C(foot.Rows[1],0)=="F-A","insert above puts a blank row over the selected row");
  history.Undo();Check(foot.Rows.Cast<DataGridViewRow>().Count(r=>!r.IsNewRow)==3&&C(foot.Rows[0],0)=="F-A","Undo removes the inserted row");
  history.Redo();Check(foot.Rows.Cast<DataGridViewRow>().Count(r=>!r.IsNewRow)==4&&C(foot.Rows[0],0)=="","Redo inserts it again");
  /* typing in the inserted row gives it the sheet's new-row defaults (side forms, materials) */
  foot.CurrentCell=foot.Rows[0].Cells[1];foot.BeginEdit(true);Check(C(foot.Rows[0],21)=="Formed sides","defaults appear when the inserted row is edited");foot.CancelEdit();foot.EndEdit();
  /* below the last row inserts above the blank new row; on the blank new row it inserts above it */
  foot.CurrentCell=foot.Rows[foot.NewRowIndex].Cells[1];history.InsertRow(true);Check(foot.Rows[foot.NewRowIndex-1].Cells.Cast<DataGridViewCell>().All(c=>c.Value==null||C(foot.Rows[foot.NewRowIndex-1],c.ColumnIndex)=="")&&foot.Rows[foot.NewRowIndex].IsNewRow,"insert on the new row goes above it");
  /* finish like the other native UI tests: let pending UI callbacks run, close the window and return normally */
  Application.DoEvents();form.Controls.Remove(foot);form.Close();form.Dispose();Application.DoEvents();
  Console.WriteLine("PASS: "+checks+" insert row above / below, blank inserted row, defaults on typing and undo checks.");}}
