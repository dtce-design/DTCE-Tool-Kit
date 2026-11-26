using System;using System.Collections.Generic;using System.Drawing;using System.Linq;using System.Text;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* Live Loads (NSCP 2015) design sheet, a sub tab of Design Loads (owner, 26 November 2026: "include Live Loads ... Government Buildings
   all spaces and Rooms ... summarize them per space. based on NSCP 2015"): the live load of every space of the chosen government building
   types with its Table 205-1 row, the concentrated load, f1 and whether it may be reduced; the complete Table 205-1 and Table 205-3. */
sealed class LiveLoadsForm:DesignSheetForm {
 internal const string AllBuildings="All building types";
 readonly DataGridView spaces=Grid(),table=Grid(),roofs=Grid();
 /* search bars (owner: "INCLUDE SEARCH BAR"): any word of any column, e.g. "toilet", "gym", "4.8", "hospital" */
 readonly TextBox spaceSearch=new TextBox{Width=420},tableSearch=new TextBox{Width=420};
 static Control SearchBar(TextBox box,string hint){var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=44,Padding=new Padding(6,8,6,4)};bar.Controls.Add(new Label{Text="Search",AutoSize=true,Padding=new Padding(0,4,6,0),Font=ModernUi.Strong});bar.Controls.Add(box);
  var clear=new Button{Text="Clear",AutoSize=true};clear.Click+=(s,e)=>box.Clear();bar.Controls.Add(clear);bar.Controls.Add(new Label{Text=hint,AutoSize=true,Padding=new Padding(8,4,0,0),ForeColor=PowTheme.Ink});return bar;}
 static bool Matches(string[] row,string query){var words=query.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);var all=string.Join(" ",row);return words.All(w=>all.IndexOf(w,StringComparison.OrdinalIgnoreCase)>=0);}
 static DataGridView Grid(){return new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill};}
 public LiveLoadsForm():base("Live Loads (NSCP 2015) - government buildings, per space","Live-Loads"){
  var sec=Section("Building and spaces","Live loads of NSCP 2015 Section 205 (Table 205-1 floors, Table 205-3 roofs) for each space of the government building types. Choose a building type or all of them.");
  Choice(sec,"Building","Building type",new[]{AllBuildings}.Concat(NscpLiveLoads.Buildings.Keys).ToArray());
  Toggle(sec,"Partitions","Add the partition load (1.0 kPa) to offices where partitions may be moved",true);
  var page=new TabPage("Live loads per space");page.Controls.Add(spaces);page.Controls.Add(SearchBar(spaceSearch,"space, building, load or code row - e.g. toilet, gym, hospital, 4.8"));AddResultPage(page);var t=new TabPage("Table 205-1 (floors)");t.Controls.Add(table);t.Controls.Add(SearchBar(tableSearch,"occupancy, description or load"));AddResultPage(t);var rf=new TabPage("Table 205-3 (roofs)");rf.Controls.Add(roofs);AddResultPage(rf);
  foreach(var h in new[]{"Building","Space","Uniform (kPa)","Concentrated (kN)","f1","Reduction","NSCP 2015 basis","Remarks"})spaces.Columns.Add(h,h);
  spaces.Columns[0].FillWeight=70;spaces.Columns[1].FillWeight=120;spaces.Columns[2].FillWeight=45;spaces.Columns[3].FillWeight=50;spaces.Columns[4].FillWeight=25;spaces.Columns[5].FillWeight=50;spaces.Columns[6].FillWeight=150;spaces.Columns[7].FillWeight=120;
  foreach(var h in new[]{"Use or occupancy","Description","Uniform (kPa)","Concentrated (kN)","Note"})table.Columns.Add(h,h);table.Columns[1].FillWeight=160;table.Columns[4].FillWeight=120;
  foreach(var h in new[]{"Roof slope","0 to 20 m²","21 to 60 m²","Over 60 m²"})roofs.Columns.Add(h,h);roofs.Columns[0].FillWeight=300;
  foreach(var g in new[]{spaces,table,roofs})GridAppearance.Apply(g);
  FillTable();spaceSearch.TextChanged+=(s,e)=>Fill();tableSearch.TextChanged+=(s,e)=>FillTable();foreach(var r in NscpLiveLoads.RoofTable)roofs.Rows.Add(r);
  foreach(var c in Inputs.Values){var co=c as ComboBox;if(co!=null)co.SelectedIndexChanged+=(s,e)=>{if(!Restoring)Fill();};var cb=c as CheckBox;if(cb!=null)cb.CheckedChanged+=(s,e)=>{if(!Restoring)Fill();};}
  Shown+=(s,e)=>Fill();}
 protected override void AfterRestore(){base.AfterRestore();Fill();}
 internal IEnumerable<string[]> Rows(){var names=Value("Building")==AllBuildings?NscpLiveLoads.Buildings.Keys.ToList():new List<string>{Value("Building")};bool part=On("Partitions");
  foreach(var b in names)foreach(var s in NscpLiveLoads.Buildings[b]){double u=NscpLiveLoads.Uniform(s);bool office=s.Row=="15b";string remark=s.Remark;
   if(part&&office&&!double.IsNaN(u)){remark=(remark==""?"":remark+"; ")+"plus 1.0 kPa partitions";}
   double c=NscpLiveLoads.Concentrated(s);yield return new[]{b,s.Space,double.IsNaN(u)?(s.Row==""?"0.60 - 1.00":"as served"):(u+(part&&office?1.0:0)).ToString("0.0#"),double.IsNaN(c)?"-":c.ToString("0.0#"),NscpLiveLoads.F1(s),NscpLiveLoads.Reducible(s),NscpLiveLoads.Basis(s),remark};}}
 /* rows keep the app's single height (owner, 17 November 2026); long text shows in full as a tooltip and in the report */
 static void Wrap(DataGridView g){g.ShowCellToolTips=true;}
 void Fill(){Wrap(spaces);spaces.Rows.Clear();string q=spaceSearch.Text.Trim();foreach(var r in Rows())if(q==""||Matches(r,q))spaces.Rows.Add(r);}
 void FillTable(){Wrap(table);Wrap(roofs);table.Rows.Clear();string q=tableSearch.Text.Trim();foreach(var r in NscpLiveLoads.Table){var row=new[]{r.Category,r.Description,r.UniformText,r.ConcentratedText,r.Note};if(q==""||Matches(row,q))table.Rows.Add(row);}}
 internal int SearchSpaces(string query){spaceSearch.Text=query;return spaces.Rows.Count;}
 protected override string BuildReport(){var b=new StringBuilder();foreach(var r in Rows())b.AppendLine(string.Join(" | ",r));return b.ToString();}
 protected override CalculationDocument BuildDocument(){var rep=new RichReport("Live Loads - NSCP 2015");string building=Value("Building");
  rep.Heading("1  BASIS").Paragraph("NSCP 2015 Section 205: floors are designed for the unit live loads of Table 205-1 or the concentrated loads, whichever produce the greater effects; a concentrated load acts on an area 750 mm × 750 mm placed for the maximum effect unless stated otherwise. Roofs follow Table 205-3. Where office partitions may be moved, a partition load of 1.0 kPa is added"+(On("Partitions")?" (included below for offices)":" (not included below)")+". Live loads may be reduced by Section 205.5 except in places of public assembly and for live loads over 4.8 kPa (except as the section allows). f1 of Eq. 203-3 and 203-5 is 1.0 for places of public assembly, live loads over 4.8 kPa and garages, and 0.5 for the other live loads. Restrooms take the load of the occupancy served, not over 2.4 kPa. Values to be checked against the printed NSCP 2015 tables.");
  rep.Heading("2  LIVE LOADS PER SPACE - "+building.ToUpperInvariant());
  foreach(var g in Rows().GroupBy(r=>r[0])){rep.Paragraph(g.Key,false,true);rep.Table(new[]{"Space","Uniform (kPa)","Conc. (kN)","f1","Reduction","NSCP 2015 basis","Remarks"},g.Select(r=>r.Skip(1).ToArray()),new float[]{22,9,8,5,10,26,20});}
  rep.Heading("3  TABLE 205-1  MINIMUM UNIFORM AND CONCENTRATED LIVE LOADS").Table(new[]{"Use or occupancy","Description","Uniform (kPa)","Conc. (kN)","Note"},NscpLiveLoads.Table.Select(r=>new[]{r.Category,r.Description,r.UniformText,r.ConcentratedText,r.Note}),new float[]{28,32,10,9,21});
  rep.Heading("4  TABLE 205-3  MINIMUM ROOF LIVE LOADS (METHOD 1, kPa BY TRIBUTARY AREA)").Table(new[]{"Roof slope","0 to 20 m²","21 to 60 m²","Over 60 m²"},NscpLiveLoads.RoofTable,new float[]{55,15,15,15});
  return rep.Build();}
}
}
