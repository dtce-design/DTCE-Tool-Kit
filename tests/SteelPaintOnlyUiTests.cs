using System;using System.Linq;using System.Reflection;using System.Windows.Forms;using BeamScheduleFormatter;
// Owner (18 November 2026): steel rows show their paint area, and a "Paint only" row adds metal painting without
// steel weight, purchase, cutting or turnbuckles.
class SteelPaintOnlyUiTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}static T Get<T>(object o,string name){return (T)o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);}
 static void Ones(DataGridView g){foreach(DataGridViewRow r in g.Rows)if(!r.IsNewRow)foreach(string c in new[]{"SteelUnitsPerSet","SteelSets","SteelMultiplier"})r.Cells[c].Value="1";}
 [STAThread]static void Main(){AccessCodes.Granted=true;var purlin=SteelCatalog.Profiles.First(p=>p.Kind=="Purlin");string xml;
  using(var f=new ProgramOfWorksForm()){var pg=Get<DataGridView>(f,"steelPurlins");var br=Get<DataGridView>(f,"crossBracing");
   foreach(var g in new[]{pg,br,Get<DataGridView>(f,"steelAngles"),Get<DataGridView>(f,"fasciaFrame")})Check(g.Columns.Contains("SteelPaintArea")&&g.Columns.Contains("SteelPaintOnly"),"Paint area and Paint only columns on "+g.Name);
   pg.Rows.Add("P1","New purlins",purlin.Name,"GI","6","10");pg.Rows.Add("P2","Existing purlins",purlin.Name,"GI","6","8");br.Rows.Add("CB1","Existing bracing","3","0","4","12","2");Ones(pg);Ones(br);
   pg.Rows[1].Cells["SteelPaintOnly"].Value=true;br.Rows[0].Cells["SteelPaintOnly"].Value=true;f.Recalculate();
   double per=SteelPainting.AreaPerMetre(purlin);
   Check(Math.Abs(Convert.ToDouble(pg.Rows[0].Cells["SteelPaintArea"].Value)-per*60)<1e-6,"Paint area shown on a normal purlin row");
   Check(Math.Abs(Convert.ToDouble(pg.Rows[1].Cells["SteelPaintArea"].Value)-per*48)<1e-6,"Paint area shown on a paint-only row");
   Check(Convert.ToString(pg.Rows[1].Cells[7].Value)==""&&Convert.ToString(pg.Rows[1].Cells[pg.Columns.Count-1].Value).StartsWith("Paint only"),"Paint-only purlin has no weight");
   Check(Convert.ToString(br.Rows[0].Cells[9].Value)==""&&Convert.ToString(br.Rows[0].Cells[10].Value)=="","Paint-only bracing has no weight or turnbuckles");
   var steel=f.WorksExcelTables().Single(t=>t.Name=="POW Structural Steel Summary");Func<string,double> V=n=>{for(int i=0;i<steel.Rows.GetLength(0);i++)if(Convert.ToString(steel.Rows[i,0]).StartsWith(n))return Convert.ToDouble(steel.Rows[i,1]);return double.NaN;};
   Check(Math.Abs(V("Purlins PURCHASE weight")-Convert.ToDouble(pg.Rows[0].Cells[7].Value))<1e-6,"Only the normal purlin row is bought");
   Check(Math.Abs(V("Metal painting - purlins")-per*108)<1e-6,"Paint-only rows still go to metal painting");
   Check(V("Metal painting - crossbracing")>0&&Math.Abs(V("Crossbracing rods"))<1e-9,"Paint-only bracing painted, not weighed");
   xml=f.WorksProjectXml();}
  using(var f=new ProgramOfWorksForm()){f.OpenProjectXml(xml);var pg=Get<DataGridView>(f,"steelPurlins");Check(Equals(pg.Rows[1].Cells["SteelPaintOnly"].Value,true)&&!Equals(pg.Rows[0].Cells["SteelPaintOnly"].Value,true),"Paint only is saved with the project");}
  Console.WriteLine("PASS: "+checks+" steel paint-only checks");}
}
