using System;using System.Linq;using System.Reflection;using System.Windows.Forms;using BeamScheduleFormatter;
/* v1.50.17: jamb depth / jamb thickness / leaf thickness are set by the program from the linked CHB / masonry wall and hidden */
class OpeningJambAutoTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Public|BindingFlags.Static;
 [STAThread]static void Main(){AccessCodes.Granted=true;var pow=new ProgramOfWorksForm();var t=typeof(ProgramOfWorksForm);
  var wood=(DataGridView)t.GetField("wood",F).GetValue(pow);var walls=(DataGridView)t.GetField("walls",F).GetValue(pow);var masonry=(System.Collections.Generic.Dictionary<string,DataGridView>)t.GetField("masonryGrids",F).GetValue(pow);
  Check(!wood.Columns[7].Visible&&!wood.Columns[8].Visible&&!wood.Columns[9].Visible,"jamb depth, jamb thickness and leaf thickness are hidden");
  var auto=t.GetMethod("AutomaticWoodDimensions",F);
  int w=walls.Rows.Add();walls.Rows[w].Cells[0].Value="W100";walls.Rows[w].Cells[18].Value="100";int w2=walls.Rows.Add();walls.Rows[w2].Cells[0].Value="W150";walls.Rows[w2].Cells[18].Value="150";
  var aac=masonry["MasonryAAC"];int a=aac.Rows.Add();aac.Rows[a].Cells[0].Value="AAC1";aac.Rows[a].Cells[18].Value="100";
  int r=wood.Rows.Add();wood.Rows[r].Cells[0].Value="D1";wood.Rows[r].Cells[2].Value="W100";auto.Invoke(pow,new object[]{wood.Rows[r]});
  Check(Convert.ToString(wood.Rows[r].Cells[7].Value)=="0.1"&&Convert.ToString(wood.Rows[r].Cells[8].Value)=="0.05"&&Convert.ToString(wood.Rows[r].Cells[9].Value)=="45","100mm CHB wall: jamb depth 0.10 m, jamb 0.05 m, leaf 45 mm");
  wood.Rows[r].Cells[2].Value="W150";auto.Invoke(pow,new object[]{wood.Rows[r]});Check(Convert.ToString(wood.Rows[r].Cells[7].Value)=="0.15","150mm CHB wall: jamb depth 0.15 m");
  wood.Rows[r].Cells[2].Value="AAC1";auto.Invoke(pow,new object[]{wood.Rows[r]});Check(Convert.ToString(wood.Rows[r].Cells[7].Value)=="0.1","100mm AAC wall: jamb depth 0.10 m");
  wood.Rows[r].Cells[2].Value="";auto.Invoke(pow,new object[]{wood.Rows[r]});Check(Convert.ToString(wood.Rows[r].Cells[7].Value)=="0.15","no wall: the Unassigned CHB thickness setting (150 mm)");
  /* v1.50.18: Opening ID prefix sets the type; windows are Aluminum or Steel only */
  Func<int,string> type=x=>Convert.ToString(wood.Rows[x].Cells[3].Value);Func<int,string> mat=x=>Convert.ToString(wood.Rows[x].Cells[ProgramOfWorksForm.OpeningMaterialColumn].Value);
  int d=wood.Rows.Add();wood.Rows[d].Cells[3].Value="Window";Check(mat(d)=="Aluminum","choosing Window switches a wood row to Aluminum");wood.Rows[d].Cells[0].Value="D-01";Check(type(d)=="Door"&&mat(d)=="Aluminum","D prefix: Door, material kept");
  int wi=wood.Rows.Add();wood.Rows[wi].Cells[3].Value="Door";wood.Rows[wi].Cells[ProgramOfWorksForm.OpeningMaterialColumn].Value="Wood";wood.Rows[wi].Cells[0].Value="w-02";Check(type(wi)=="Window"&&mat(wi)=="Aluminum","W prefix: Window, wood switched to Aluminum");
  Check(Convert.ToString(wood.Rows[wi].Cells[11].Value)=="0"&&Convert.ToString(wood.Rows[wi].Cells[12].Value)=="0","aluminum window: no lockset / hinges");
  wood.Rows[wi].Cells[ProgramOfWorksForm.OpeningMaterialColumn].Value="Steel";Check(mat(wi)=="Steel","Steel window allowed");
  wood.Rows[wi].Cells[ProgramOfWorksForm.OpeningMaterialColumn].Value="PVC";Check(mat(wi)=="Aluminum","PVC window not allowed");
  wood.Rows[wi].Cells[ProgramOfWorksForm.OpeningMaterialColumn].Value="Wood";Check(mat(wi)=="Aluminum","Wood window not allowed");
  int auto2=wood.Rows.Add();wood.Rows[auto2].Cells[3].Value="Door";wood.Rows[auto2].Cells[0].Value="WD001";Check(type(auto2)=="Door","automatic WD001 ID is not read as a window");
  int other=wood.Rows.Add();wood.Rows[other].Cells[3].Value="Door";wood.Rows[other].Cells[0].Value="X1";Check(type(other)=="Door","other prefixes keep the chosen type");
  int op=wood.Rows.Add();wood.Rows[op].Cells[0].Value="O-01";Check(type(op)=="Opening"&&mat(op)=="","O prefix: plain opening without material");
  Check(ProgramOfWorksForm.TypeFromOpeningId(" d12 ")=="Door"&&ProgramOfWorksForm.TypeFromOpeningId("W3")=="Window"&&ProgramOfWorksForm.TypeFromOpeningId("o5")=="Opening"&&ProgramOfWorksForm.TypeFromOpeningId("")==null,"prefix rule");
  Console.WriteLine("PASS: "+checks+" automatic jamb depth, hidden jamb columns and Opening ID prefix / window material checks.");Environment.Exit(0);}}
