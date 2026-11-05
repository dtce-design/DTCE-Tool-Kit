using System;using System.Linq;using System.Drawing;using System.Globalization;using System.Collections.Generic;using System.Windows.Forms;
namespace BeamScheduleFormatter {
class BillQuantity {public string Section,Description,Unit;public double Quantity;public bool Basis;public readonly List<BillQuantity> Sources=new List<BillQuantity>();}
partial class ProgramOfWorksForm {
 DataGridView boqGrid;Label boqStatus;
 void AddHome(){
  var page=new TabPage("Bill of Quantities");
  var heading=new Label{Text="BILL OF QUANTITIES",Dock=DockStyle.Top,Height=62,Padding=new Padding(16,12,0,0),Font=new Font("Segoe UI",22,FontStyle.Bold),ForeColor=PowTheme.Ink};
  var actions=new FlowLayoutPanel{Dock=DockStyle.Top,Height=48,Padding=new Padding(12,6,6,6),AutoScroll=true};
  Button(actions,"Open / Link Excel",LinkWorksExcel);Button(actions,"Sync now",()=>{Guard();SyncWorksExcel();});Button(actions,"Review inputs",ShowWorksIssues);Button(actions,"Disconnect Excel",DisconnectWorksExcel);
  excelStatus.Dock=DockStyle.Top;excelStatus.Height=55;
  boqStatus=new Label{Dock=DockStyle.Top,Height=48,Padding=new Padding(16,8,12,4),ForeColor=PowTheme.Muted};
  boqGrid=Grid(new[]{"Work section","Required item / material","Unit","Required quantity"},0);boqGrid.Name="BillOfQuantities";boqGrid.ReadOnly=true;boqGrid.AllowUserToAddRows=false;boqGrid.AllowUserToDeleteRows=false;boqGrid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;
  float[] weights={24,58,9,16};for(int n=0;n<4;n++){boqGrid.Columns[n].FillWeight=weights[n];boqGrid.Columns[n].MinimumWidth=n==1?300:n==0?170:80;}boqGrid.Columns[3].DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;
  page.Controls.Add(boqGrid);page.Controls.Add(boqStatus);page.Controls.Add(excelStatus);page.Controls.Add(actions);page.Controls.Add(heading);tabs.TabPages.Add(page);
 }
 internal List<BillQuantity> BillOfQuantities(){
  var full=WorksExcelTables();var rows=new List<BillQuantity>();Func<string,string,double,string,BillQuantity> add=(section,item,quantity,unit)=>{var itemRow=new BillQuantity{Section=section,Description=item,Quantity=quantity,Unit=unit};rows.Add(itemRow);return itemRow;};
  var main=full.Single(t=>t.Name==WorksSummaryLayout.Sheet&&t.StartColumn==1);
  Action<string,string[]> fixedRows=(section,keys)=>{foreach(string key in keys){var map=WorksSummaryLayout.Map.Single(m=>m[0]==key);add(section,map[1],Convert.ToDouble(main.Rows[int.Parse(map[2])-2,1]),map[3]).Sources.AddRange(FixedQuantitySources(key));}};
  fixedRows("Reinforced Concrete Works",new[]{"BeamConcrete","SlabConcrete","FootingConcrete","ConcreteTotal","BeamForms","SlabForms","FootingForms","FormsTotal","Grade40","Grade60"});
  var canopyTotals=CanopySummary();for(int n=0;n<canopyTotals.Count;n++)add("Canopy / Ledge",Convert.ToString(canopyTotals[n][0]),Convert.ToDouble(canopyTotals[n][1]),Convert.ToString(canopyTotals[n][2])).Sources.AddRange(CanopyQuantitySources(n));
  foreach(var group in projectRebarPlan.Where(a=>a.Choice!=null).GroupBy(a=>new{a.Demand.Grade,a.Demand.Diameter}).OrderBy(g=>g.Key.Grade).ThenBy(g=>g.Key.Diameter))add("Rebar purchase by size","Grade "+group.Key.Grade+" / "+group.Key.Diameter+" mm",group.Sum(a=>a.Purchase),"kg").Sources.AddRange(MergeSources(group.Select(a=>SourceQuantity(RebarSource(a),a.Purchase,"kg"))));
  Action<string,string> summary=(section,name)=>{var table=full.Single(t=>t.Name==name);for(int n=0;n<(name=="POW Overall Earthworks"?Math.Min(6,table.Rows.GetLength(0)):table.Rows.GetLength(0));n++)add(section,Convert.ToString(table.Rows[n,0]),Convert.ToDouble(table.Rows[n,1]),Convert.ToString(table.Rows[n,2])).Sources.AddRange(name=="POW Masonry Summary"?MasonryQuantitySources(n):name=="POW Overall Earthworks"?EarthQuantitySources(n):name=="POW Structural Steel Summary"?SteelQuantitySources(Convert.ToString(table.Rows[n,0])):RoofingQuantitySources(Convert.ToString(table.Rows[n,0])));};
  summary("Earthworks","POW Overall Earthworks");var gravelTotals=GravelSummary();for(int n=0;n<gravelTotals.Count;n++)add("Gravel",Convert.ToString(gravelTotals[n][0]),Convert.ToDouble(gravelTotals[n][1]),Convert.ToString(gravelTotals[n][2])).Sources.AddRange(GravelQuantitySources(n));summary("Masonry Works","POW Masonry Summary");
  fixedRows("Finishing Works",new[]{"Plaster","MasonryPaint","WallTiles","CeilingFinishes","FloorFinishes"});
  foreach(var item in FinishTypeRegistry.Totals(finishItems,f=>f.Total(FindWall(f.Wall),wallOpenings,finishItems)).OrderBy(f=>f.Name))add("Finish material breakdown",item.Name,item.Area,"m2").Sources.AddRange(MergeSources(finishItems.Where(f=>FinishTypeRegistry.Key(f.MaterialName)==item.Key).Select(f=>SourceQuantity(f.Id+" | "+f.Description,f.Total(FindWall(f.Wall),wallOpenings,finishItems),"m2"))));
  fixedRows("Doors & Windows",new[]{"WoodPaint","Leversets","Hinges"});
  foreach(var opening in openings.Where(o=>o.Material!="Manual").OrderBy(o=>o.Id)){add("Door / window schedule",opening.Id+" | "+opening.Material+" "+opening.Kind+" | "+opening.Description,opening.Quantity,"sets");}
  var mouldingTotals=ExteriorMouldingTotals();for(int n=0;n<mouldingTotals.Count;n++)add("Exterior moulding",Convert.ToString(mouldingTotals[n][0]),Convert.ToDouble(mouldingTotals[n][1]),Convert.ToString(mouldingTotals[n][2])).Sources.AddRange(MouldingQuantitySources(n));
  summary("Roofing Works","POW Roofing Summary");summary("Structural Steel Works","POW Structural Steel Summary");
  foreach(var group in steelStocks.GroupBy(s=>s.Specification).OrderBy(g=>g.Key)){add("Steel purchase by profile",group.Key+" | 6 m stock",group.Count(),"pcs");add("Steel purchase by profile",group.Key+" | purchase weight",group.Sum(s=>s.KgM*6),"kg").Sources.AddRange(StockPurchaseSources(group));}
  return rows;
 }
 void UpdateDashboard(){
  if(boqGrid==null||footingFc==null||earthBalance==null||roofAccessories==null)return;
  boqGrid.Rows.Clear();var issues=WorksValidationIssues();if(issues.Count>0){boqStatus.ForeColor=Color.Firebrick;boqStatus.Text="REVIEW: "+issues.Count+" input issue(s). Select Review inputs. Save Program of Works retains your entries; final quantities are withheld until valid.";return;}
  try{foreach(var item in BillOfQuantities()){int row=boqGrid.Rows.Add(item.Section,item.Description,item.Unit,item.Quantity.ToString(item.Unit=="pcs"||item.Unit=="sets"||item.Unit=="rolls"?"N0":"N2",CultureInfo.InvariantCulture));if(item.Sources.Count>0){boqGrid.Rows[row].DefaultCellStyle.Font=new Font(boqGrid.Font,FontStyle.Bold);foreach(var source in item.Sources){int detail=boqGrid.Rows.Add(source.Basis?"Basis":"Source","    "+source.Description,source.Unit,source.Quantity.ToString(source.Unit=="pcs"||source.Unit=="sets"||source.Unit=="rolls"?"N0":"N2",CultureInfo.InvariantCulture));boqGrid.Rows[detail].DefaultCellStyle.ForeColor=PowTheme.Muted;}}}boqStatus.ForeColor=PowTheme.Teal;boqStatus.Text="Validated required quantities | Source rows explain each material total; do not add them again. Insulation roll sources show net-area basis before whole-roll rounding. Save Program of Works keeps the full project.";}
  catch(Exception ex){boqGrid.Rows.Clear();boqStatus.ForeColor=Color.Firebrick;boqStatus.Text="REVIEW: "+ex.Message;}
 }
}
}
