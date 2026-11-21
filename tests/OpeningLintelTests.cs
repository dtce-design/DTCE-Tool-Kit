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
  var misc=(System.Collections.Generic.List<MiscStructureQuantity>)t.GetField("miscQuantities",F).GetValue(pow);var lintels=misc.Where(q=>q.Kind==LintelSchedule.Kind).ToList();
  Check(lintels.Count==1&&lintels[0].Id.StartsWith("LB-6A")&&lintels[0].Quantity==2,"the door in the 150 mm wall gets LB-6A x 2 ("+string.Join(", ",misc.Select(q=>q.Kind+" "+q.Id))+")");
  var grid=(DataGridView)t.GetField("lintelGrid",F).GetValue(pow);Check(grid!=null&&grid.Rows.Count==2&&Convert.ToString(grid.Rows[0].Cells[0].Value)=="LB-6A","listed under Lintels over Openings");
  Check(Convert.ToString(grid.Rows[1].Cells[0].Value)=="TOTAL"&&Convert.ToString(grid.Rows[1].Cells[15].Value)==lintels[0].Concrete.ToString("0.000",System.Globalization.CultureInfo.InvariantCulture),"total row with concrete");
  Check(lintels[0].Steel.Result.Lines.Any(l=>l.Part=="Jamb bars"&&l.Diameter==12&&l.Kg>0)&&!lintels[0].Steel.Result.Lines.Any(l=>l.Part.Contains("brace")),"12 mm jamb bars included, lateral brace not");
  var sum=(Label)t.GetField("chbLintelSummary",F).GetValue(pow);Check(sum!=null&&sum.Text.Contains("Concrete")&&sum.Text.Contains("Formworks")&&sum.Text.Contains("RSB")&&sum.Text.Contains("12 mm"),"CHB Estimate shows the lintel concrete, formworks and RSB at once: "+(sum==null?"":sum.Text));
  /* more openings and a 100 mm wall: both summaries count every unit */
  {int w2=walls.Rows.Add();var r2=walls.Rows[w2];r2.Cells[0].Value="W2";r2.Cells[1].Value="Partition";r2.Cells[2].Value="8";r2.Cells[3].Value="3";r2.Cells[4].Value="1";r2.Cells[18].Value="100";
   foreach(var o in new[]{new[]{"W-1","Window","Window","2.4","1.2","3"},new[]{"D2","Toilet door","Door","0.7","2.1","2"}}){int k=wood.Rows.Add();wood.Rows[k].Cells[0].Value=o[0];wood.Rows[k].Cells[1].Value=o[1];wood.Rows[k].Cells[3].Value=o[2];wood.Rows[k].Cells[4].Value=o[3];wood.Rows[k].Cells[5].Value=o[4];wood.Rows[k].Cells[6].Value=o[5];}
   links.Add(new OpeningLink{Schedule="W-1",Wall="W1",Quantity=3});links.Add(new OpeningLink{Schedule="D2",Wall="W2",Quantity=2});t.GetMethod("Recalculate",F,null,Type.EmptyTypes,null).Invoke(pow,null);
   var a=((Label)t.GetField("lintelSummary",F).GetValue(pow)).Text;var b=((Label)t.GetField("chbLintelSummary",F).GetValue(pow)).Text;Console.WriteLine(a);
   Check(a.Contains(": 7 lintels")&&a==b,"7 lintels in both summaries: ["+a+"] / ["+b+"]");}
  {var bill=(System.Collections.Generic.List<BillQuantity>)t.GetMethod("ExtraBillQuantities",F).Invoke(pow,null);Check(bill.Any(q=>q.Description=="Lintel Beam over Openings concrete"&&q.Quantity>0)&&bill.Any(q=>q.Description=="Lintel Beam over Openings forms"),"own Bill of Quantities item for the lintels over openings");
   var text=(string)t.GetMethod("OffcutReuseText",F).Invoke(pow,null);Console.WriteLine(text);Check(text.Contains("kg saved"),"offcut reuse is reported");
   var xml=new System.Xml.XmlDocument();var root=xml.CreateElement("x");LintelSchedule.Current[0]=LintelSchedule.Parse("LB-4A","4","2.00","100","350","1-16","1-12","-","single","125","200","");t.GetMethod("WriteExtraSettings",F).Invoke(pow,new object[]{root});Check(root.GetAttribute("lintelSchedule").Contains("LB-4A|4|2.00|100|350"),"an edited schedule is saved with the project");
   LintelSchedule.Reset();t.GetMethod("ReadExtraSettings",F).Invoke(pow,new object[]{root});Check(LintelSchedule.Select(100,1).Depth==350,"and read back");LintelSchedule.Reset();}
  var auto=(CheckBox)t.GetField("autoLintels",F).GetValue(pow);auto.Checked=false;t.GetMethod("Recalculate",F,null,Type.EmptyTypes,null).Invoke(pow,null);
  Check(!((System.Collections.Generic.List<MiscStructureQuantity>)t.GetField("miscQuantities",F).GetValue(pow)).Any(q=>q.Kind==LintelSchedule.Kind),"turned off: no automatic lintels");
  Console.WriteLine("PASS: "+checks+" automatic lintel checks");pow.Dispose();}
}
