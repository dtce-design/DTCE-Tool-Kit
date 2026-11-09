using System;using System.Linq;using System.Drawing;using System.Globalization;using System.Collections.Generic;using System.Windows.Forms;
namespace BeamScheduleFormatter {
partial class ProgramOfWorksForm {
 DataGridView boqGrid;Label boqStatus,boqHeader;
 void AddHome(){
  var page=new TabPage("Bill of Quantities");
  var heading=new Label{Text="BILL OF QUANTITIES AND DETAILED QUANTITY TAKE-OFF / CALCULATIONS",Dock=DockStyle.Top,Height=62,Padding=new Padding(16,12,0,0),Font=new Font("Segoe UI",16,FontStyle.Bold),ForeColor=PowTheme.Ink};
  boqHeader=new Label{Dock=DockStyle.Top,Height=125,Padding=new Padding(16,6,16,6),Font=new Font("Arial",10),ForeColor=PowTheme.Ink};
  var actions=new FlowLayoutPanel{Dock=DockStyle.Top,Height=48,Padding=new Padding(12,6,6,6),AutoScroll=true};
  Button(actions,"Open / Link Excel",LinkWorksExcel);Button(actions,"Sync now",()=>{Guard();SyncWorksExcel();});Button(actions,"Review inputs",ShowWorksIssues);Button(actions,"Disconnect Excel",DisconnectWorksExcel);
  excelStatus.Dock=DockStyle.Top;excelStatus.Height=55;
  boqStatus=new Label{Dock=DockStyle.Top,Height=48,Padding=new Padding(16,8,12,4),ForeColor=PowTheme.Muted};
  boqGrid=Grid(new[]{"ITEM NO.","DESCRIPTION","QUANTITY","UNIT"},0);boqGrid.Name="BillOfQuantities";boqGrid.ReadOnly=true;boqGrid.AllowUserToAddRows=false;boqGrid.AllowUserToDeleteRows=false;boqGrid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;
  boqGrid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.None;float[] weights={16,66,14,9};for(int n=0;n<4;n++){boqGrid.Columns[n].FillWeight=weights[n];boqGrid.Columns[n].MinimumWidth=n==1?300:n==0?170:80;}boqGrid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;boqGrid.Columns[2].DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;
  page.Controls.Add(boqGrid);page.Controls.Add(boqStatus);page.Controls.Add(excelStatus);page.Controls.Add(actions);page.Controls.Add(boqHeader);page.Controls.Add(heading);tabs.TabPages.Add(page);
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
  rows.AddRange(MouldingQuantities());
  summary("Roofing Works","POW Roofing Summary");summary("Structural Steel Works","POW Structural Steel Summary");
  foreach(var group in steelStocks.GroupBy(s=>s.Specification).OrderBy(g=>g.Key)){add("Steel purchase by profile",group.Key+" | purchase weight",group.Sum(s=>s.KgM*6),"kg").Sources.AddRange(StockPurchaseSources(group));}
  return rows;
 }
 void UpdateDashboard(){
  if(boqGrid==null||footingFc==null||earthBalance==null||roofAccessories==null)return;
  var project=ProjectInfoStore.Read();boqHeader.Text="Republic of the Philippines\nDEPARTMENT OF PUBLIC WORKS AND HIGHWAYS\nImplementing Office: "+project.Office+"\nAddress: "+project.OfficeAddress+"\nProject Name: "+project.Name+"\nProject Location: "+project.Location;boqGrid.Rows.Clear();var issues=WorksValidationIssues();if(issues.Count>0){boqStatus.ForeColor=Color.Firebrick;boqStatus.Text="REVIEW: "+issues.Count+" input issue(s). Select Review inputs. Save Program of Works retains your entries; final quantities are withheld until valid.";return;}
  try{foreach(var item in BillQuantityLayout.Rows(BillOfQuantities())){int row=boqGrid.Rows.Add(item.Item,item.Description,item.Quantity.HasValue?item.Quantity.Value.ToString(item.Unit=="pcs"||item.Unit=="sets"?"N0":"N2",CultureInfo.InvariantCulture):"",item.Unit);if(item.Section){boqGrid.Rows[row].DefaultCellStyle.BackColor=Color.FromArgb(225,233,240);boqGrid.Rows[row].DefaultCellStyle.Font=new Font(boqGrid.Font,FontStyle.Bold);}else if(item.Source){boqGrid.Rows[row].DefaultCellStyle.ForeColor=PowTheme.Muted;}else boqGrid.Rows[row].DefaultCellStyle.Font=new Font(boqGrid.Font,FontStyle.Bold);}boqStatus.ForeColor=PowTheme.Teal;boqStatus.Text="Validated required quantities | Item numbers remain blank where project specifications are unconfirmed. | Source rows explain each material total; do not add them again. Insulation roll sources show net-area basis before whole-roll rounding. Save Program of Works keeps the full project.";}
  catch(Exception ex){boqGrid.Rows.Clear();boqStatus.ForeColor=Color.Firebrick;boqStatus.Text="REVIEW: "+ex.Message;}
 }
}
}

