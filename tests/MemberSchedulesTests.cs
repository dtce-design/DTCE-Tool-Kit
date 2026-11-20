using System;using System.Linq;using System.Reflection;using System.Text.RegularExpressions;using System.Windows.Forms;using BeamScheduleFormatter;
/* Manual column / footing schedules on the Program of Works and the manual beam schedule manager (v1.50.12) */
class MemberSchedulesTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Public;
 static ProgramOfWorksForm pow;static DataGridView col,foot,cs,fs;
 static void New(string xml){pow=new ProgramOfWorksForm();var grids=(System.Collections.IDictionary)typeof(ProgramOfWorksForm).GetField("extraGrids",F).GetValue(pow);var item=grids["StructuralColumn"];col=(DataGridView)item.GetType().GetField("Grid").GetValue(item);foot=(DataGridView)typeof(ProgramOfWorksForm).GetField("footings",F).GetValue(pow);cs=(DataGridView)typeof(ProgramOfWorksForm).GetProperty("ColumnSchedule",F).GetValue(pow,null);fs=(DataGridView)typeof(ProgramOfWorksForm).GetProperty("FootingSchedule",F).GetValue(pow,null);if(xml!=null){typeof(ProgramOfWorksForm).GetMethod("OpenProjectXml",F).Invoke(pow,new object[]{xml});pow.Recalculate();}}
 static string Xml(){return (string)typeof(ProgramOfWorksForm).GetMethod("DataXml",F).Invoke(pow,null);}
 static BeamRow Beam(string mark,string b,string d){var r=new BeamRow{Mark=mark,Leg="2L",Diameter="O10",RevisedSpacing="1 - 50mm, AND REST AT 200mm C/C"};r.Spacing=r.RevisedSpacing;for(int n=0;n<14;n++)r.Cells[n].Add("-");r.Cells[0][0]=b;r.Cells[1][0]=d;for(int n=2;n<8;n++)r.Cells[n][0]="2-O16";r.Cells[8][0]=r.Leg;r.Cells[9][0]=r.Diameter;r.Cells[10][0]=r.Spacing;return r;}
 [STAThread]static void Main(){AccessCodes.Granted=true;New(null);
  Check(cs!=null&&fs!=null&&col.Columns.Contains(ProgramOfWorksForm.ScheduleMarkColumn)&&foot.Columns.Contains(ProgramOfWorksForm.ScheduleMarkColumn),"schedule mark columns exist");
  int r=col.Rows.Add();foreach(var p in new[]{"0=C-01","1=Grid A1","2=3.2","3=250","4=250","5=2","6=12","7=4","10=10","11=Spacing (mm)","12=150","13=0"}){var kv=p.Split('=');col.Rows[r].Cells[int.Parse(kv[0])].Value=kv[1];}pow.Recalculate();
  /* a project saved before this version has no schedules and no marks */
  string old=Regex.Replace(Xml(),"<MemberSchedules.*?</MemberSchedules>|<MemberSchedules[^>]*/>","");Check(!old.Contains(" mark=\"")&&!old.Contains("MemberSchedules"),"old-format sample");
  New(old);Check(cs.Rows.Count==0&&fs.Rows.Count==0,"old file: empty schedules");Check(Convert.ToString(col.Rows[0].Cells[3].Value)=="250"&&Convert.ToString(col.Rows[0].Cells[7].Value)=="4","old file: row keeps its own values");Check(Convert.ToString(col.Rows[0].Cells[ProgramOfWorksForm.StatusIndex(col)].Value)=="Estimated","old file: row estimated");
  cs.Rows.Add("C1","Typical","400","400","20","8","","","10","Spacing (mm)","100","","");fs.Rows.Add("F1","Isolated","2.5","2.5","0.5","75","20","125","","20","125","","","","","","","","0");
  col.Rows[0].Cells[ProgramOfWorksForm.ScheduleMarkColumn].Value="C1";int fr=foot.Rows.Add();foot.Rows[fr].Cells[0].Value="F-01";foot.Rows[fr].Cells[1].Value="A1";foot.Rows[fr].Cells[5].Value="3";foot.Rows[fr].Cells[6].Value="1.5";foot.Rows[fr].Cells[ProgramOfWorksForm.ScheduleMarkColumn].Value="F1";pow.Recalculate();
  Check(Convert.ToString(col.Rows[0].Cells[ProgramOfWorksForm.ScheduleMarkColumn].Value)=="C1","mark survives recalculation");
  Check(Convert.ToString(col.Rows[0].Cells[3].Value)=="400"&&Convert.ToString(col.Rows[0].Cells[6].Value)=="20"&&Convert.ToString(col.Rows[0].Cells[12].Value)=="100"&&col.Rows[0].Cells[3].ReadOnly,"column takes the schedule and locks it");
  Check(Convert.ToString(col.Rows[0].Cells[2].Value)=="3.2"&&Convert.ToString(col.Rows[0].Cells[5].Value)=="2","height and quantity stay on the row");
  Check(Math.Abs(double.Parse(Convert.ToString(col.Rows[0].Cells[18].Value))-0.4*0.4*3.2*2)<1e-6,"column concrete from the schedule section");
  Check(Convert.ToString(foot.Rows[fr].Cells[2].Value)=="2.5"&&Convert.ToString(foot.Rows[fr].Cells[8].Value)=="20"&&Convert.ToString(foot.Rows[fr].Cells[5].Value)=="3","footing takes the schedule, keeps quantity");
  Check(Convert.ToString(foot.Rows[fr].Cells[32].Value)=="Estimated"&&Math.Abs(double.Parse(Convert.ToString(foot.Rows[fr].Cells[28].Value))-9.38)<0.01,"footing estimated from the schedule");
  Check(Convert.ToString(cs.Rows[0].Cells[cs.Columns.Count-1].Value).Contains("C-01"),"schedule shows the rows using it");
  string saved=Xml();Check(saved.Contains("mark=\"C1\"")&&saved.Contains("mark=\"F1\"")&&saved.Contains("<MemberSchedules"),"marks and schedules saved");
  New(saved);Check(cs.Rows.Count==1&&fs.Rows.Count==1&&Convert.ToString(col.Rows[0].Cells[ProgramOfWorksForm.ScheduleMarkColumn].Value)=="C1"&&Convert.ToString(foot.Rows[0].Cells[ProgramOfWorksForm.ScheduleMarkColumn].Value)=="F1","reload keeps schedules and marks");
  Check(saved==Xml(),"reload is unchanged (no false unsaved changes)");
  cs.Rows[0].Cells[2].Value="450";pow.Recalculate();Check(Convert.ToString(col.Rows[0].Cells[3].Value)=="450","a schedule change updates the rows");
  col.Rows[0].Cells[ProgramOfWorksForm.ScheduleMarkColumn].Value="";pow.Recalculate();Check(!col.Rows[0].Cells[3].ReadOnly&&Convert.ToString(col.Rows[0].Cells[3].Value)=="450","clearing the mark unlocks the row and keeps the values");
  col.Rows[0].Cells[ProgramOfWorksForm.ScheduleMarkColumn].Value="C9";pow.Recalculate();Check(col.Rows[0].Cells[ProgramOfWorksForm.ScheduleMarkColumn].ErrorText.Contains("not on the"),"unknown mark is flagged");
  /* manual beam schedules: add, edit (rename follows the takeoff rows), delete */
  var est=new EstimatorForm(null);var beams=(DataGridView)typeof(EstimatorForm).GetField("beams",F).GetValue(est);
  est.AddManual(Beam("B1","300","500"));est.AddManual(Beam("B2","250","400"));int b=beams.Rows.Add();beams.Rows[b].Cells[1].Value="B1";
  est.ReplaceManual("B1",Beam("B1A","300","550"));Check(Convert.ToString(beams.Rows[0].Cells[1].Value)=="B1A","renamed beam mark follows the takeoff row");
  bool refused=false;try{est.ReplaceManual("B1A",Beam("B2","300","550"));}catch(Exception){refused=true;}Check(refused,"rename to an existing mark is refused");
  est.DeleteManual("B2");var items=((DataGridViewComboBoxColumn)beams.Columns[1]).Items.Cast<object>().Select(Convert.ToString).ToList();Check(!items.Contains("B2")&&items.Contains("B1A")&&beams.Rows.Cast<DataGridViewRow>().Count(x=>!x.IsNewRow)==1,"deleting an unused beam schedule keeps the other rows");
  est.DeleteManual("B1A");Check(beams.Rows.Cast<DataGridViewRow>().Count(x=>!x.IsNewRow)==0,"deleting a used beam schedule removes its takeoff rows");
  Console.WriteLine("PASS: "+checks+" manual column / footing schedule, old-format, save/open and beam schedule manager checks.");Environment.Exit(0);}}
