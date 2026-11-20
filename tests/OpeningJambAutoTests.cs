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
  /* v1.50.19: one opening ID in walls of different thickness - each wall's units use that wall's jamb depth */
  {var links=(System.Collections.Generic.List<OpeningLink>)t.GetField("openingAssignments",F).GetValue(pow);links.Add(new OpeningLink{Schedule="D-05",Wall="W100",Quantity=1});links.Add(new OpeningLink{Schedule="D-05",Wall="W150",Quantity=2});
   int m=wood.Rows.Add();wood.Rows[m].Cells[0].Value="D-05";wood.Rows[m].Cells[6].Value="3";auto.Invoke(pow,new object[]{wood.Rows[m]});
   var groups=((System.Collections.Generic.Dictionary<string,System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<double,int>>>)t.GetField("woodJambGroups",F).GetValue(pow))["D-05"];
   Check(groups!=null&&groups.Count==2&&groups.Any(g=>Math.Abs(g.Key-0.15)<1e-9&&g.Value==2)&&groups.Any(g=>Math.Abs(g.Key-0.10)<1e-9&&g.Value==1),"mixed walls: 2 units at 150 mm and 1 unit at 100 mm, no error");
   Check(Convert.ToString(wood.Rows[m].Cells[7].Value)=="0.15","the jamb depth shown is the one used by most units");
   var door=new OpeningItem{Id="D-05",Description="Door",Material="Wood",Kind="Door",Width=0.9,Height=2.1,Quantity=3,JambThickness=0.05,JambDepth=0.15,JambGroups=groups};double perimeter=2*2.1+0.9;
   Check(Math.Abs(door.JambPaint-(perimeter*(0.15+0.1)*2+perimeter*(0.10+0.1)*1))<1e-9,"jamb paint adds each wall's jamb depth");
   links.Clear();}
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
