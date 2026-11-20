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
  Check(Math.Abs(pow.UnlinkedPlasterDeduction-2)<1e-6&&Math.Abs(pow.UnlinkedPaintDeduction-5)<1e-6,"tiles off plaster and paint, paint finish off paint only ("+pow.UnlinkedPlasterDeduction+", "+pow.UnlinkedPaintDeduction+")");
  Check(Math.Abs(plaster0-plaster1-2)<1e-6&&Math.Abs(paint0-paint1-5)<1e-6,"totals reduced: plaster "+plaster0+" -> "+plaster1+", paint "+paint0+" -> "+paint1);
  var p1=((Dictionary<string,PlasterTakeoff>)typeof(ProgramOfWorksForm).GetField("plasterResults",F).GetValue(pow));Check(p1.Values.All(p=>p.Net>0)&&Math.Abs(p1["W001"].Net/p1["W002"].Net-1.5)<1e-6,"shared in proportion; every wall stays positive");
  Console.WriteLine("PASS: "+checks+" wall finishes without Wall ID off plaster / paint checks.");Environment.Exit(0);}}
