using System;using System.Collections.Generic;using System.Linq;using System.Xml;using System.Reflection;using System.Windows.Forms;using BeamScheduleFormatter;
// Owner (23 November 2026): "add button para assign schedule of trusses tapos yung schedule na lang input" - truss types on Angle Bars.
class TrussScheduleTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static T Get<T>(object o,string name){return (T)o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);}
 static string Steel(ProgramOfWorksForm f){return string.Join(";",f.BillOfQuantities().Where(b=>b.Section=="Structural Steel Works").Select(b=>b.Description+"="+b.Quantity.ToString("0.##")));}
 [STAThread]static void Main(){AccessCodes.Granted=true;var F=BindingFlags.NonPublic|BindingFlags.Instance;var T=typeof(ProgramOfWorksForm);var f=new ProgramOfWorksForm();var g=Get<DataGridView>(f,"steelAngles");
  var prof=SteelCatalog.Profiles.Where(p=>p.Kind=="Angle bar").Select(p=>p.Name).First();
  var t=new TrussType{Mark="T-1",Description="Main truss",Trusses=6,Sets=2};t.Members.Add(new TrussMember{Name="Top chord",Profile=prof,Length=4.5,Qty=2});t.Members.Add(new TrussMember{Name="Web",Profile=prof,Length=0.8,Qty=8});
  f.TrussTypes.Add(t);T.GetMethod("ApplyTrussType",F).Invoke(f,new object[]{t,null});f.Recalculate();
  var rows=g.Rows.Cast<DataGridViewRow>().Where(r=>!r.IsNewRow).ToList();
  Check(rows.Count==2&&rows.All(r=>Convert.ToString(r.Cells["TrussMark"].Value)=="T-1"&&Convert.ToString(r.Cells["SteelUnitsPerSet"].Value)=="6"&&Convert.ToString(r.Cells["SteelSets"].Value)=="2"),"one Angle Bars row per member: QTY per truss, Units = trusses, Sets");
  Check(rows.All(r=>Convert.ToString(r.Cells[g.Columns.Count-1].Value)=="Estimated"&&Convert.ToString(r.Cells[0].Value)!=""),"schedule rows are estimated and get IDs");
  Check(rows[0].Cells[2].ReadOnly&&rows[0].Cells[4].ReadOnly,"schedule rows are locked (no dropdown to change by mistake)");
  string before=Steel(f);Check(before.Contains("Angle bars PURCHASE weight=")&&!before.Contains("Angle bars PURCHASE weight=0;"),"truss members are bought as angle bars");
  string xml=(string)T.GetMethod("DataXml",F).Invoke(f,null);var d=new XmlDocument();d.LoadXml(xml);var f2=new ProgramOfWorksForm();T.GetMethod("OpenProjectDocument",F).Invoke(f2,new object[]{d,new PowSourceFiles(),null});f2.Recalculate();
  var g2=Get<DataGridView>(f2,"steelAngles");Check(f2.TrussTypes.Count==1&&f2.TrussTypes[0].Members.Count==2&&g2.Rows.Cast<DataGridViewRow>().Count(r=>!r.IsNewRow&&Convert.ToString(r.Cells["TrussMark"].Value)=="T-1")==2&&g2.Rows[0].Cells[2].ReadOnly&&Steel(f2)==before,"saved and reopened with the same quantities");
  t.Trusses=3;T.GetMethod("ApplyTrussType",F).Invoke(f,new object[]{t,"T-1"});Check(g.Rows.Cast<DataGridViewRow>().Count(r=>!r.IsNewRow)==2&&Convert.ToString(g.Rows[0].Cells["SteelUnitsPerSet"].Value)=="3","editing a truss updates its rows");
  f.TrussTypes.Remove(t);T.GetMethod("ApplyTrussType",F).Invoke(f,new object[]{null,"T-1"});Check(g.Rows.Cast<DataGridViewRow>().Count(r=>!r.IsNewRow)==0,"deleting a truss removes its rows");
  Console.WriteLine("PASS: "+checks+" truss schedule checks");Environment.Exit(0);}
}
