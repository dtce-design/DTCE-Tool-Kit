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
  /* opening Qty counts per wall unit: Wall Qty 2 with 1 opening each = 2 openings deducted and 2 lintels */
  {int w3=walls.Rows.Add();var r3=walls.Rows[w3];r3.Cells[0].Value="W3";r3.Cells[1].Value="Two bedrooms";r3.Cells[2].Value="4";r3.Cells[3].Value="3";r3.Cells[4].Value="2";r3.Cells[18].Value="100";
   int k=wood.Rows.Add();wood.Rows[k].Cells[0].Value="D3";wood.Rows[k].Cells[1].Value="Bedroom door";wood.Rows[k].Cells[3].Value="Door";wood.Rows[k].Cells[4].Value="0.8";wood.Rows[k].Cells[5].Value="2.1";wood.Rows[k].Cells[6].Value="2";
   links.Add(new OpeningLink{Schedule="D3",Wall="W3",Quantity=1});t.GetMethod("Recalculate",F,null,Type.EmptyTypes,null).Invoke(pow,null);
   var wallOpenings=(System.Collections.Generic.List<OpeningItem>)t.GetField("wallOpenings",F).GetValue(pow);Check(wallOpenings.Where(o=>o.Wall=="W3").Sum(o=>o.Quantity)==2,"1 per wall unit x Wall Qty 2 = 2 openings");
   Check(Math.Abs(Convert.ToDouble(r3.Cells[8].Value)-0.8*2.1*2)<0.006,"both openings deducted from the CHB: "+r3.Cells[8].Value);
   var lw=((System.Collections.Generic.List<MiscStructureQuantity>)t.GetField("miscQuantities",F).GetValue(pow)).Where(q=>q.Kind==LintelSchedule.Kind&&q.Id.Contains("D3")).ToList();Check(lw.Count==1&&lw[0].Quantity==2,"2 lintels for the two wall units");
   links[links.Count-1].Quantity=2;t.GetMethod("Recalculate",F,null,Type.EmptyTypes,null).Invoke(pow,null);var errs=(System.Collections.Generic.List<string>)t.GetField("errors",F).GetValue(pow);Check(errs.Any(e=>e.Contains("exceed")),"2 per unit x 2 units = 4 > schedule Qty 2 is reported");
   links.RemoveAt(links.Count-1);wood.Rows.RemoveAt(k);walls.Rows.RemoveAt(w3);t.GetMethod("Recalculate",F,null,Type.EmptyTypes,null).Invoke(pow,null);}
  /* files saved before: the total for the group is divided by the Wall Qty */
  {var doc=new System.Xml.XmlDocument();doc.LoadXml("<P><OpeningAssignments><Link wall='W9' schedule='X' qty='4'/></OpeningAssignments></P>");var old=OpeningLinks.Read(doc.DocumentElement);Check(old[0].Total&&OpeningLinks.Legacy(doc.DocumentElement),"old files are read as totals");
   var root=new System.Xml.XmlDocument().CreateElement("P");root.OwnerDocument.AppendChild(root);OpeningLinks.Write(root,new[]{new OpeningLink{Wall="W9",Schedule="X",Quantity=2}});var again=OpeningLinks.Read(root);Check(!again[0].Total&&!OpeningLinks.Legacy(root),"new files are per wall unit");}
  {var lr=(System.Collections.Generic.List<MiscStructureQuantity>)t.GetField("lintelResults",F).GetValue(pow);var plan=((System.Collections.Generic.IEnumerable<StockAllocation>)t.GetMethod("RebarPlanFor",F).Invoke(pow,new object[]{"Miscellaneous Structures"})).ToList();
   Check(lr.Count>0&&lr.All(q=>plan.Any(al=>al.Entry==q.Steel)),"Miscellaneous Structures rebar by grade and diameter includes the automatic lintels");}
  var auto=(CheckBox)t.GetField("autoLintels",F).GetValue(pow);auto.Checked=false;t.GetMethod("Recalculate",F,null,Type.EmptyTypes,null).Invoke(pow,null);
  Check(!((System.Collections.Generic.List<MiscStructureQuantity>)t.GetField("miscQuantities",F).GetValue(pow)).Any(q=>q.Kind==LintelSchedule.Kind),"turned off: no automatic lintels");
  /* RC wall side B bars (owner, 21 November 2026) and insulation roll defaults 1 m x 50 m */
  {var b1=new FootingBarSpec{Diameter=12,Spacing=200};var b2=new FootingBarSpec{Diameter=12,Spacing=250};var cfgW=CalculationStore.Resolve(21,40,275,40,275);
   var same=RCWallTakeoff.Calculate("W","x",5,3,200,1,25,2,b1,b2,0,0,2,0,false,cfgW);var diff=RCWallTakeoff.Calculate("W","x",5,3,200,1,25,2,b1,b2,0,0,2,0,false,cfgW,new FootingBarSpec{Diameter=16,Spacing=150},null);
   Check(same.Steel.Result.Lines.Count==4&&Math.Abs(same.Steel.Result.Lines.Sum(l=>l.Kg)-2*RCWallTakeoff.Calculate("W","x",5,3,200,1,25,1,b1,b2,0,0,2,0,false,cfgW).Steel.Result.Lines.Sum(l=>l.Kg))<1e-6,"two layers without side B input = side A twice");
   var vb=diff.Steel.Result.Lines.Single(l=>l.Part=="VERTICAL / side B");Check(vb.Diameter==16&&vb.PiecesPerBeam==(int)Math.Ceiling((5-0.05-0.016)/0.15-1e-10)+1,"side B vertical bars with their own diameter and spacing");
   Check(diff.Steel.Result.Lines.Single(l=>l.Part=="HORIZONTAL / side B").Diameter==12,"side B horizontal blank = as side A");
   var ins=(DataGridView)t.GetField("insulationRolls",F).GetValue(pow);Check(ins.Rows.Cast<DataGridViewRow>().All(ir=>Convert.ToString(ir.Cells[1].Value)=="1"&&Convert.ToString(ir.Cells[2].Value)=="50"),"insulation rolls 1 m x 50 m by default");}
  /* a row is estimated only when its description and deduction are entered (23 November 2026) */
  {var grids=(System.Collections.IDictionary)typeof(ProgramOfWorksForm).GetField("extraGrids",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(pow);var item=grids["Carpentry6"];var board=(DataGridView)item.GetType().GetField("Grid").GetValue(item);
   int br=board.Rows.Add("C-001","","0.85","1.9","3");pow.Recalculate();Check(Convert.ToString(board.Rows[br].Cells[8].Value)==ProgramOfWorksForm.DescriptionMissing,"no description: not estimated ("+board.Rows[br].Cells[8].Value+")");
   board.Rows[br].Cells[1].Value="Female Toilet - Shower";pow.Recalculate();Check(Convert.ToString(board.Rows[br].Cells[8].Value)==ProgramOfWorksForm.DeductionMissing,"no deduction: not estimated ("+board.Rows[br].Cells[8].Value+")");Check(pow.GoToIssue(board.Rows[br].Cells[0].Value+"" == "" ? "" : "Laminated Phenolic Board (Toilet and Urinal Partition) "+board.Rows[br].Cells[0].Value+": "+ProgramOfWorksForm.DeductionMissing)&&board.CurrentCell!=null&&board.CurrentCell.RowIndex==br&&board.CurrentCell.ColumnIndex==5,"double-click on the error goes to the entry's deduction cell");
   board.Rows[br].Cells[5].Value="0";pow.Recalculate();Check(Convert.ToString(board.Rows[br].Cells[8].Value)=="Estimated"&&Math.Abs(Convert.ToDouble(board.Rows[br].Cells[6].Value)-0.85*1.9*3)<1e-9,"description and deduction 0: estimated");board.Rows.RemoveAt(br);pow.Recalculate();}
  Console.WriteLine("PASS: "+checks+" automatic lintel checks");pow.Dispose();}
}
