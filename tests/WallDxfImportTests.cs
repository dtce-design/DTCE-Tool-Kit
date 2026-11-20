using System;using System.Linq;using System.Reflection;using System.Collections.Generic;using System.Windows.Forms;using BeamScheduleFormatter;
/* v5.1.0: walls from a DXF (dimensions give the length; IDs from dimension text / labels / automatic; thickness from the layer) */
class WallDxfImportTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static string Dxf(params string[] pairs){return string.Join("\n",pairs)+"\n";}
 [STAThread]static void Main(){
  string header=Dxf("0","SECTION","2","HEADER","9","$INSUNITS","70","4","0","ENDSEC");
  string ents=Dxf("0","SECTION","2","ENTITIES",
   "0","DIMENSION","8","CHB-100","1","W-05","13","0","23","0","14","3500","24","0","42","3500",
   "0","DIMENSION","8","CHB-150","1","<>","13","0","23","5000","14","0","24","9000","42","4000",
   "0","TEXT","8","TAGS","10","300","20","7000","1","w-07",
   "0","DIMENSION","8","DIM","1","","13","20000","23","20000","14","26000","24","20000","42","6000",
   "0","ENDSEC","0","EOF");
  var walls=WallDxfImport.Read(header+ents,150);
  Check(walls.Count==3,"three dimensions, three walls");
  var w5=walls.Single(w=>w.Id=="W-05");Check(Math.Abs(w5.LengthM-3.5)<1e-9&&w5.ThicknessMm==100&&w5.IdFromDrawing,"ID from the dimension text, length 3500 mm = 3.5 m, CHB-100 layer = 100 mm");
  var w7=walls.Single(w=>w.Id=="W-07");Check(Math.Abs(w7.LengthM-4.0)<1e-9&&w7.ThicknessMm==150,"ID from the nearest label, 150 mm layer");
  var auto=walls.Single(w=>w.Id==null);Check(Math.Abs(auto.LengthM-6.0)<1e-9&&auto.ThicknessMm==150,"no label: no ID yet, default thickness");
  Check(WallDxfImport.AsId("{\\fArial;W-12}")=="W-12"&&WallDxfImport.AsId("3500")==null&&WallDxfImport.LayerThickness("A-WALL-CHB150")==150,"text and layer parsing");
  /* metres drawing without dimensions: wall lines on a WALL layer */
  var lines=WallDxfImport.Read(Dxf("0","SECTION","2","HEADER","9","$INSUNITS","70","6","0","ENDSEC","0","SECTION","2","ENTITIES","0","LINE","8","WALL-150","10","0","20","0","11","2.5","21","0","0","LINE","8","FURNITURE","10","0","20","0","11","9","21","0","0","ENDSEC","0","EOF"),100);
  Check(lines.Count==1&&Math.Abs(lines[0].LengthM-2.5)<1e-9&&lines[0].ThicknessMm==150,"wall lines on a WALL layer when there are no dimensions");
  /* into CHB Estimate */
  AccessCodes.Granted=true;var pow=new ProgramOfWorksForm();var F=BindingFlags.NonPublic|BindingFlags.Instance;var grid=(DataGridView)typeof(ProgramOfWorksForm).GetField("walls",F).GetValue(pow);
  int existing=grid.Rows.Add();grid.Rows[existing].Cells[0].Value="W-05";grid.Rows[existing].Cells[3].Value="2.8";
  int changed=pow.ImportWalls(walls,"plan.dxf",false);Check(changed==3,"three walls added / updated");
  var rows=grid.Rows.Cast<DataGridViewRow>().Where(r=>!r.IsNewRow).ToList();Check(rows.Count==3,"existing W-05 updated, two new rows");
  var r5=rows.Single(r=>Convert.ToString(r.Cells[0].Value)=="W-05");Check(Convert.ToString(r5.Cells[2].Value)=="3.5"&&Convert.ToString(r5.Cells[18].Value)=="100"&&Convert.ToString(r5.Cells[3].Value)=="2.8","existing wall: length and thickness updated, height kept");
  var rn=rows.Where(r=>Convert.ToString(r.Cells[0].Value)!="W-05").ToList();Check(rn.All(r=>Convert.ToString(r.Cells[3].Value)==""),"height is left for manual input");Check(rn.Any(r=>Convert.ToString(r.Cells[0].Value)=="W001"),"automatic ID for the unlabelled wall");
  /* the Openings button and the openings list come right after the gross area */
  var g19=grid.Columns[19];var action=grid.Columns["EditWallOpeningsAction"];var list=grid.Columns[ProgramOfWorksForm.OpeningsIncludedColumn];
  Check(action.DisplayIndex==g19.DisplayIndex+1&&list.DisplayIndex==action.DisplayIndex+1&&!action.Frozen,"Openings button and list after OR Gross area");
  Console.WriteLine("PASS: "+checks+" DXF wall import and opening column placement checks.");Environment.Exit(0);}}
