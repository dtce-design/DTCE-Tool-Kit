using System;using System.Linq;using System.Reflection;using System.Collections.Generic;using System.Windows.Forms;using BeamScheduleFormatter;
/* v5.1.4: wall finishes without a Wall ID come off the plaster / masonry paint totals */
class UnlinkedFinishTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
 static double Total(ProgramOfWorksForm pow,string field){var d=(Dictionary<string,PlasterTakeoff>)typeof(ProgramOfWorksForm).GetField(field,F).GetValue(pow);return d.Values.Sum(p=>p.Net);}
 [STAThread]static void Main(){AccessCodes.Granted=true;var pow=new ProgramOfWorksForm();var walls=(DataGridView)typeof(ProgramOfWorksForm).GetField("walls",F).GetValue(pow);var fin=(DataGridView)typeof(ProgramOfWorksForm).GetField("wallFinishes",F).GetValue(pow);
  foreach(var spec in new[]{new[]{"W001","6"},new[]{"W002","4"}}){var r=((WorksDataGridView)walls).AddRowWithDefaults();r.Cells[0].Value=spec[0];r.Cells[1].Value="Wall";r.Cells[2].Value=spec[1];r.Cells[3].Value="3";r.Cells[4].Value="1";r.Cells[18].Value="150";}
  pow.Recalculate();double plaster0=Total(pow,"plasterResults"),paint0=Total(pow,"paintResults");Check(plaster0>0&&paint0>0,"walls give plaster and paint ("+plaster0+", "+paint0+")");
  Func<string,string,string,string> add=(id,kind,len)=>{var r=((WorksDataGridView)fin).AddRowWithDefaults();r.Cells[0].Value=id;r.Cells[1].Value="Room";r.Cells[2].Value=kind=="Tiles"?"FT-1":"PT-1";r.Cells[4].Value=kind;r.Cells[7].Value=len;r.Cells[8].Value="1";r.Cells[9].Value="1";r.Cells[10].Value="0";return id;};
  var sched=(DataGridView)typeof(ProgramOfWorksForm).GetField("finishSchedule",F).GetValue(pow);sched.Rows.Add("FT-1","Wall tiles","Ceramic tiles 300x600");sched.Rows.Add("PT-1","Wall paint","Masonary/Concrete Painting Works");
    add("WF001","Tiles","2");add("WF002","Paint","3");pow.Recalculate();
  double plaster1=Total(pow,"plasterResults"),paint1=Total(pow,"paintResults");
  Check(Math.Abs(pow.UnlinkedPlasterDeduction-5)<1e-6&&Math.Abs(pow.UnlinkedPaintDeduction-5)<1e-6,"every wall finish off plaster and paint ("+pow.UnlinkedPlasterDeduction+", "+pow.UnlinkedPaintDeduction+")");
  Check(Math.Abs(plaster0-plaster1-5)<1e-6&&Math.Abs(paint0-paint1-5)<1e-6,"totals reduced: plaster "+plaster0+" -> "+plaster1+", paint "+paint0+" -> "+paint1);
  var p1=((Dictionary<string,PlasterTakeoff>)typeof(ProgramOfWorksForm).GetField("plasterResults",F).GetValue(pow));Check(p1.Values.All(p=>p.Net>0)&&Math.Abs(p1["W001"].Net/p1["W002"].Net-1.5)<1e-6,"shared in proportion; every wall stays positive");
  /* v5.1.5: Wall Finishes columns like Ceiling / Floor; type from the BOQ group; every Part I item no. offered as a BOQ group */
  Check(!fin.Columns[3].Visible&&!fin.Columns[4].Visible&&!fin.Columns[5].Visible&&!fin.Columns[6].Visible&&fin.Columns[10].HeaderText=="Deduction m2 (total)"&&fin.Columns[11].HeaderText=="Gross m2"&&fin.Columns[12].HeaderText=="Net finish m2","Wall Finishes columns match Ceiling / Floor");
  Check(ProgramOfWorksForm.WallFinishKind("Masonary/Concrete Painting Works","Tiles")=="Paint"&&ProgramOfWorksForm.WallFinishKind("Ceramic tiles 300x600","Paint")=="Tiles"&&ProgramOfWorksForm.WallFinishKind("Granite cladding","Other")=="Other","type from the group, else the stored type");
  var part1=FinishStandard.PartIItems(null);Check(part1.Count>FinishStandard.All.Length&&part1.All(t=>{int n=int.Parse(System.Text.RegularExpressions.Regex.Match(t.Code,@"^\d+").Value);return n>=1000&&n<=1051;}),"Part I items listed ("+part1.Count+")");
  var extra=part1.First(t=>FinishStandard.All.All(x=>x.Code!=t.Code));var found=FinishStandard.Find(extra.Description);Check(found!=null&&found.Code==extra.Code,"a Part I item chosen as a group routes to its item no. ("+extra.Code+" "+extra.Description+")");
  /* Finish Schedule location: each tab offers only its own Finish IDs; saved with the project */
  {int li=sched.Columns[ProgramOfWorksForm.FinishLocationColumn].Index;sched.Rows.Add("CL-1","Ceiling board","Fiber cement board ceiling");
   foreach(DataGridViewRow r in sched.Rows){if(r.IsNewRow)continue;string id=Convert.ToString(r.Cells[0].Value);r.Cells[li].Value=id=="CL-1"?"Ceiling":id=="FT-1"?"Wall":"";}
   pow.Recalculate();var ceil=(DataGridView)typeof(ProgramOfWorksForm).GetField("ceilingFinishes",F).GetValue(pow);
   Func<DataGridView,List<string>> items=g=>((DataGridViewComboBoxColumn)g.Columns[2]).Items.Cast<object>().Select(Convert.ToString).ToList();
   Check(items(fin).Contains("FT-1")&&!items(fin).Contains("CL-1")&&items(fin).Contains("PT-1"),"Wall Finishes offers wall and unassigned Finish IDs only");
   Check(items(ceil).Contains("CL-1")&&!items(ceil).Contains("FT-1"),"Ceiling Finishes offers ceiling Finish IDs only");
   string xml=(string)typeof(ProgramOfWorksForm).GetMethod("DataXml",F).Invoke(pow,null);Check(xml.Contains("loc=\"Ceiling\"")&&xml.Contains("loc=\"Wall\""),"location saved");
   var pow2=new ProgramOfWorksForm();pow2.OpenProjectXml(xml);var s2=(DataGridView)typeof(ProgramOfWorksForm).GetField("finishSchedule",F).GetValue(pow2);
   Check(s2.Rows.Cast<DataGridViewRow>().Any(r=>Convert.ToString(r.Cells[0].Value)=="CL-1"&&Convert.ToString(r.Cells[s2.Columns[ProgramOfWorksForm.FinishLocationColumn].Index].Value)=="Ceiling"),"location read back");
   string old=xml.Replace(" loc=\"Ceiling\"","").Replace(" loc=\"Wall\"","");var pow3=new ProgramOfWorksForm();pow3.OpenProjectXml(old);var s3=(DataGridView)typeof(ProgramOfWorksForm).GetField("finishSchedule",F).GetValue(pow3);
   Check(s3.Rows.Cast<DataGridViewRow>().Where(r=>!r.IsNewRow).All(r=>Convert.ToString(r.Cells[s3.Columns[ProgramOfWorksForm.FinishLocationColumn].Index].Value)==""),"older files: blank location (all tabs)");}
  Console.WriteLine("PASS: "+checks+" wall finishes without Wall ID off plaster / paint checks.");Environment.Exit(0);}}
