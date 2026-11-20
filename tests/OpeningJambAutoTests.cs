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
  Console.WriteLine("PASS: "+checks+" automatic jamb depth from CHB / masonry wall thickness and hidden jamb columns.");Environment.Exit(0);}}
