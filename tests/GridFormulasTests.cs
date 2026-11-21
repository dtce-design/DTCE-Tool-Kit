using System;using System.Linq;using System.Windows.Forms;using System.Xml;using BeamScheduleFormatter;
class GridFormulasTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static double Eval(string f){double v;string e;if(!GridFormulas.TryEvaluate(f,out v,out e))throw new Exception(f+": "+e);return v;}
 [STAThread]static void Main(){
  Check(Math.Abs(Eval("0.6+0.3")-.9)<1e-12,"0.6+0.3 = 0.9");Check(Eval("2*(3+4)")==14&&Eval("-2^2")==4&&Eval("10/4")==2.5&&Eval("3x0.5")==1.5&&Eval("1.5 + 2.25")==3.75,"operators, parentheses, x as times");
  double v;string err;Check(!GridFormulas.TryEvaluate("1/0",out v,out err)&&err.Contains("zero"),"division by zero refused");Check(!GridFormulas.TryEvaluate("2+",out v,out err)&&!GridFormulas.TryEvaluate("(1+2",out v,out err)&&!GridFormulas.TryEvaluate("abc",out v,out err),"bad formulas refused");
  Check(GridFormulas.Format(.6+.3)=="0.9","result shown without floating noise");
  Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);AccessCodes.Granted=true;
  using(var f=new Form()){var g=new DataGridView{Dock=DockStyle.Fill};g.Columns.Add("L","Length");g.Rows.Add();f.Controls.Add(g);GridFormulas.AttachAll(f);f.Show();Application.DoEvents();
   g.CurrentCell=g[0,0];g.BeginEdit(true);g.EditingControl.Text="=0.6+0.3";g.EndEdit();Check(Convert.ToString(g[0,0].Value)=="0.9","the cell shows the total");Check(GridFormulas.Formula(g[0,0])=="=0.6+0.3","the formula is kept");
   g.BeginEdit(true);Check(g.EditingControl.Text=="=0.6+0.3","editing shows the formula again");g.EditingControl.Text="1.25";g.EndEdit();Check(Convert.ToString(g[0,0].Value)=="1.25"&&GridFormulas.Formula(g[0,0])==null,"typing a plain value clears the formula");f.Close();}
  /* the Program of Works stores the formula next to the value */
  ProgramOfWorksForm pow0=null;try{pow0=new ProgramOfWorksForm();}catch(Exception ex){if(Type.GetType("Mono.Runtime")==null)throw;Console.WriteLine("Bill of Quantities cannot be built on Mono here ("+ex.GetType().Name+"); file round trip checked on Windows.");}
  if(pow0!=null){var pow=pow0;/* not disposed: disposing the Program of Works fails on Mono */GridFormulas.AttachAll(pow);var fl=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;var grids=(System.Collections.IDictionary)typeof(ProgramOfWorksForm).GetField("extraGrids",fl).GetValue(pow);var item=grids["StructuralColumn"];var grid=(DataGridView)item.GetType().GetField("Grid").GetValue(item);
   int r=grid.Rows.Add();grid.Rows[r].Cells[0].Value="C1";grid.Rows[r].Cells[2].Value="3.9";GridFormulas.Set(grid.Rows[r].Cells[2],"=3.2+0.7");var doc=new XmlDocument();var root=doc.CreateElement("ProgramOfWorks");doc.AppendChild(root);typeof(ProgramOfWorksForm).GetMethod("WriteExtraProject",fl).Invoke(pow,new object[]{root});
   Check(doc.OuterXml.Contains("f2=\"=3.2+0.7\"")&&doc.OuterXml.Contains("c2=\"3.9\""),"saved file keeps the value and the formula");grid.Rows.Clear();typeof(ProgramOfWorksForm).GetMethod("ReadExtraProject",fl).Invoke(pow,new object[]{root});var back=grid.Rows.Cast<DataGridViewRow>().First(x=>!x.IsNewRow&&Convert.ToString(x.Cells[0].Value)=="C1");Check(Convert.ToString(back.Cells[2].Value)=="3.9"&&GridFormulas.Formula(back.Cells[2])=="=3.2+0.7","opening the file brings the formula back");}
  Console.WriteLine("PASS: "+checks+" grid formula checks");}
}
