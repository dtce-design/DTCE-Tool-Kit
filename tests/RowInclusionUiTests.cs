using System;using System.Collections.Generic;using System.Linq;using System.Reflection;using System.Windows.Forms;using BeamScheduleFormatter;
// Owner (18 November 2026): a row can be left out of the totals with the box beside its x, keeping its inputs; and a
// deleted wall releases the openings assigned to it instead of reporting "Wall ID ... has no CHB row".
class RowInclusionUiTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}static T Get<T>(object o,string name){return (T)o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);}
 static void Include(ProgramOfWorksForm f,DataGridViewRow r,bool on){typeof(ProgramOfWorksForm).GetMethod("SetRowIncluded",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(f,new object[]{r,on});}
 static double Boq(ProgramOfWorksForm f,string key){return f.BackupQuantities().Where(d=>d.Key.StartsWith(key,StringComparison.Ordinal)).Sum(d=>d.Quantity);}
 [STAThread]static void Main(){AccessCodes.Granted=true;string xml;
  using(var f=new ProgramOfWorksForm()){var w=Get<DataGridView>(f,"walls");
   foreach(var v in new[]{new[]{"W001","10"},new[]{"W002","5"}}){var r=w.Rows[w.Rows.Add()];r.Cells[0].Value=v[0];r.Cells[1].Value="Wall";r.Cells[2].Value=v[1];r.Cells[3].Value="3";r.Cells[4].Value="1";r.Cells[18].Value="150";}
   var pg=Get<DataGridView>(f,"steelPurlins");var purlin=SteelCatalog.Profiles.First(q=>q.Kind=="Purlin");pg.Rows.Add("P1","x",purlin.Name,"GI","6","10");pg.Rows.Add("P2","y",purlin.Name,"GI","6","5");foreach(DataGridViewRow r in pg.Rows)if(!r.IsNewRow)foreach(string c in new[]{"SteelUnitsPerSet","SteelSets","SteelMultiplier"})r.Cells[c].Value="1";
   f.Recalculate();double chb=Boq(f,"Masonry Works|CHB 150"),steel=Boq(f,"Structural Steel Works|Purlins PURCHASE");Check(Math.Abs(chb-45)<1e-6,"Both walls counted");
   Include(f,w.Rows[1],false);Include(f,pg.Rows[1],false);
   Check(Math.Abs(Boq(f,"Masonry Works|CHB 150")-30)<1e-6,"Excluded wall left out of the CHB total");Check(Boq(f,"Structural Steel Works|Purlins PURCHASE")<steel-1e-6,"Excluded purlin row left out of the steel weight");
   xml=f.WorksProjectXml();Check(xml.Contains("x=\"1\"")&&xml.Contains("c0=\"W002\""),"Excluded rows are saved with their inputs");
   /* deleting a wall releases its opening assignments */
   var g=Get<DataGridView>(f,"wood");var o=g.Rows[g.Rows.Add()];o.Cells[0].Value="OPEN-1";o.Cells[3].Value="Opening";o.Cells[4].Value="1";o.Cells[5].Value="1";o.Cells[6].Value="2";
   var links=Get<List<OpeningLink>>(f,"openingAssignments");links.Add(new OpeningLink{Wall="W001",Schedule="OPEN-1",Quantity=1});f.Recalculate();Check(Convert.ToString(w.Rows[0].Cells[8].Value)=="1.00","Opening deducted");
   w.Rows.RemoveAt(0);f.Recalculate();Check(links.Count==0&&!Get<List<string>>(f,"errors").Any(e=>e.Contains("has no CHB row")),"Deleted wall: no stale opening error");}
  using(var f=new ProgramOfWorksForm()){f.OpenProjectXml(xml);f.Recalculate();var w=Get<DataGridView>(f,"walls");
   Check(Math.Abs(Boq(f,"Masonry Works|CHB 150")-30)<1e-6&&w.Rows[1].HeaderCell.Tag!=null,"Exclusion survives save / open");
   Include(f,w.Rows[1],true);Check(Math.Abs(Boq(f,"Masonry Works|CHB 150")-45)<1e-6&&Convert.ToString(w.Rows[1].Cells[2].Value)=="5","Including the row again restores its inputs and total");}
  Console.WriteLine("PASS: "+checks+" row include / exclude and deleted-wall checks");}
}
