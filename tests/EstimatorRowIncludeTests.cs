using System;using System.Linq;using System.Text;using System.Reflection;using System.Collections.Generic;using System.Windows.Forms;using BeamScheduleFormatter;
class EstimatorRowIncludeTests{static int checks;static void Check(bool ok,string w){checks++;if(!ok)throw new Exception(w);}
 static object F(object o,string n){return o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);}
 static string BeamDxf(){var a=new Analysis{Prefix="B",Floor="TEST LEVEL",BeamCount=3};
  for(int i=0;i<3;i++){var r=new BeamRow();r.Numbers.Add("REF"+(i+1));r.SourceRows.Add(i+1);r.Cells[0].Add("300");r.Cells[1].Add("500");for(int c=2;c<8;c++)r.Cells[c].Add("3-#16");
   for(int c=8;c<11;c++)r.Cells[c].Add("5-2L-#10@100 C/C");r.Cells[11].Add("--");r.Cells[12].Add("--");r.Cells[13].Add("--");r.OriginalShear.Add(new[]{r.Cell(8),r.Cell(9),r.Cell(10)});a.Source.Add(r);}
  var built=Engine.Build(a,new Settings{Prefix="B",Floor="TEST LEVEL",Merge=false});return RevisedSchedule.Serialize(built.Drawing).Replace("\r\n","\n");}
 static string SlabDxf(){var b=new StringBuilder("0\nSECTION\n2\nENTITIES\n");Action<double,double,string> t=(x,y,text)=>b.Append("0\nTEXT\n10\n"+x.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"\n20\n"+y.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"\n1\n"+text+"\n");
  t(0,15000,"SLAB SCHEDULE");t(5000,9700,"FULL LENGTH");t(11000,9700,"FULL LENGTH");for(int r=0;r<2;r++){double y=8000-r*700;t(200,y+250,"S"+(r+1));t(3000,y,"125");for(int c=0;c<9;c++)t(5000+c*3000,y,c==1||c==3?"---":"#10@150 C/C");}return b.Append("0\nENDSEC\n0\nEOF\n").ToString();}
 
 [STAThread]static void Main(){AccessCodes.Granted=true;
  var b=new EstimatorForm(null);b.LoadCatalog(BeamDxf(),"b.dxf");var g=(DataGridView)F(b,"beams");var marks=((DataGridViewComboBoxColumn)g.Columns[1]).Items.Cast<object>().Select(x=>x.ToString()).Where(x=>x!="").ToList();Console.WriteLine("marks "+string.Join(",",marks));
  g.Rows.Add("Beam one",marks[0],"","","4","1","","","","","","","","");g.Rows.Add("Beam two",marks[1],"","","5","2","","","","","","","","");b.Recalculate();
  var inputs=(System.Collections.IList)F(b,"inputs");Check(inputs.Count==2,"two beam inputs "+inputs.Count);int plan=b.WorksPlan().Count;
  g.Rows[1].HeaderCell.Tag=EstimatorForm.ExcludedTag;b.Recalculate();Check(inputs.Count==1&&b.WorksPlan().Count<plan,"excluded beam left out ("+inputs.Count+","+b.WorksPlan().Count+"<"+plan+")");Check((string)g.Rows[1].Cells[12].Value=="Excluded from totals","status");
  string bx=b.WorksSnapshot();Check(bx.Contains("x=\"1\"")&&bx.Contains("Beam two"),"saved with x=1");var b2=new EstimatorForm(null);b2.WorksRestore(bx);var g2=(DataGridView)F(b2,"beams");Check(g2.Rows.Cast<DataGridViewRow>().Count(r=>!r.IsNewRow)==2&&object.Equals(g2.Rows[1].HeaderCell.Tag,EstimatorForm.ExcludedTag)&&((System.Collections.IList)F(b2,"inputs")).Count==1,"restored excluded");
  var s=new SlabEstimatorForm();s.GetType().GetMethod("AddSource",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(s,new object[]{SlabDxf(),"s.dxf"});var sg=(DataGridView)F(s,"grid");
  int r1=sg.Rows.Add();var vals=new[]{"Room","S1","3","4","1"};for(int c=0;c<5;c++)sg.Rows[r1].Cells[c].Value=vals[c];int r2=sg.Rows.Add();vals=new[]{"Room2","S2","3","4","1"};for(int c=0;c<5;c++)sg.Rows[r2].Cells[c].Value=vals[c];s.Recalculate();int sp=s.WorksPlan().Count;
  sg.Rows[r2].HeaderCell.Tag=EstimatorForm.ExcludedTag;s.Recalculate();Check(s.WorksPlan().Count<sp,"slab excluded from plan "+s.WorksPlan().Count+"<"+sp);string sx=s.WorksSnapshot();Check(sx.Contains("x=\"1\""),"slab saved x=1");var s2=new SlabEstimatorForm();s2.WorksRestore(sx);var sg2=(DataGridView)F(s2,"grid");Check(object.Equals(sg2.Rows[1].HeaderCell.Tag,EstimatorForm.ExcludedTag)&&s2.WorksPlan().Count==s.WorksPlan().Count,"slab restored excluded");
  Console.WriteLine("PASS: "+checks+" beam and slab estimate include-box checks");}}
