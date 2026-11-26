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
 /* check boxes "In report" (owner: "ADD CHECK ARROW TO BE INCLUDED IN THE REPORT"): the unchecked spaces, kept with the sheet */
 /* owner: "yung live load uncheck lahat. ako ang mag check" - every space starts unchecked; the checked ones are kept with the sheet */readonly TextBox included=new TextBox{Multiline=true,Visible=false};
 static string Key(string[] row){return row[0]+"|"+row[5];}
 HashSet<string> Checked(){return new HashSet<string>(included.Text.Replace("\r","").Split('\n').Where(x=>x.Trim()!=""));}
 internal bool Included(string[] row){return Checked().Contains(Key(row));}
 internal void SetIncluded(IEnumerable<string[]> rows,bool on){var set=Checked();foreach(var row in rows){if(on)set.Add(Key(row));else set.Remove(Key(row));}included.Text=string.Join("\r\n",set.OrderBy(x=>x));}
 IEnumerable<string[]> ShownRows(){string q=spaceSearch.Text.Trim();return Rows().Where(r=>q==""||Matches(r,q));}
 static Control SearchBar(TextBox box,string hint){var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=44,Padding=new Padding(6,8,6,4)};bar.Controls.Add(new Label{Text="Search",AutoSize=true,Padding=new Padding(0,4,6,0),Font=ModernUi.Strong});bar.Controls.Add(box);
  var clear=new Button{Text="Clear",AutoSize=true};clear.Click+=(s,e)=>box.Clear();bar.Controls.Add(clear);bar.Controls.Add(new Label{Text=hint,AutoSize=true,Padding=new Padding(8,4,0,0),ForeColor=PowTheme.Ink});return bar;}
 static bool Matches(string[] row,string query){var words=query.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);var all=string.Join(" ",row);return words.All(w=>all.IndexOf(w,StringComparison.OrdinalIgnoreCase)>=0);}
 static DataGridView Grid(){return new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill};}
 public LiveLoadsForm():base("Live Loads (NSCP 2015) - government buildings, per space","Live-Loads"){
  var sec=Section("Building and spaces","Live loads of NSCP 2015 Section 205 (Table 205-1 floors, Table 205-3 roofs) for each space of the government building types. Choose a building type or all of them.");
  Choice(sec,"Building","Building type",new[]{AllBuildings}.Concat(NscpLiveLoads.Buildings.Keys).ToArray());
  Toggle(sec,"Partitions","Add the partition load (1.0 kPa) to offices where partitions may be moved",true);
  var page=new TabPage("Live loads per space");page.Controls.Add(spaces);var bar=SearchBar(spaceSearch,"space, load or code row - e.g. toilet, gym, hospital, 4.8");
  /* no separate "Building and spaces" tab (owner: "bat may building and spaces pa na tab?"): the building type and the partition option sit
     on this page, above the search bar */
  var head=new FlowLayoutPanel{Dock=DockStyle.Top,Height=46,Padding=new Padding(6,8,6,4)};var bcombo=(ComboBox)Inputs["Building"];var part=(CheckBox)Inputs["Partitions"];var secPage=sec.Parent.Parent as TabPage;
  bcombo.Parent.Controls.Remove(bcombo);part.Parent.Controls.Remove(part);bcombo.Width=360;part.AutoSize=true;part.Width=520;
  /* one list of spaces, not grouped by building (owner: "huwag mo na igroup per building"); the building type input stays hidden for older sheets */head.Controls.Add(part);page.Controls.Add(bar);page.Controls.Add(head);
  if(secPage!=null)Pages.TabPages.Remove(secPage);var all=new Button{Text="Check all shown",AutoSize=true};var none=new Button{Text="Uncheck all shown",AutoSize=true};all.Click+=(s,e)=>{SetIncluded(ShownRows().ToList(),true);Fill();};none.Click+=(s,e)=>{SetIncluded(ShownRows().ToList(),false);Fill();};bar.Controls.Add(all);bar.Controls.Add(none);AddResultPage(page);var t=new TabPage("Table 205-1 (floors)");t.Controls.Add(table);t.Controls.Add(SearchBar(tableSearch,"occupancy, description or load"));AddResultPage(t);var rf=new TabPage("Table 205-3 (roofs)");rf.Controls.Add(roofs);AddResultPage(rf);
  Inputs.Add("Included",included);
  spaces.ReadOnly=false;spaces.Columns.Add(new DataGridViewCheckBoxColumn{Name="In report",HeaderText="In report",FillWeight=28});
  foreach(var h in new[]{"Space","Uniform (kPa)","Concentrated (kN)","f1","Reduction","NSCP 2015 basis","Remarks"}){int k=spaces.Columns.Add(h,h);spaces.Columns[k].ReadOnly=true;}
  spaces.Columns[1].FillWeight=150;spaces.Columns[2].FillWeight=45;spaces.Columns[3].FillWeight=50;spaces.Columns[4].FillWeight=25;spaces.Columns[5].FillWeight=50;spaces.Columns[6].FillWeight=150;spaces.Columns[7].FillWeight=130;
  spaces.CurrentCellDirtyStateChanged+=(s,e)=>{if(spaces.IsCurrentCellDirty)spaces.CommitEdit(DataGridViewDataErrorContexts.Commit);};
  spaces.CellValueChanged+=(s,e)=>{if(filling||e.RowIndex<0||e.ColumnIndex!=0)return;var row=spaces.Rows[e.RowIndex];SetIncluded(new[]{Enumerable.Range(1,7).Select(i=>Convert.ToString(row.Cells[i].Value)).ToArray()},Convert.ToBoolean(row.Cells[0].Value));};
  foreach(var h in new[]{"Use or occupancy","Description","Uniform (kPa)","Concentrated (kN)","Note"})table.Columns.Add(h,h);table.Columns[1].FillWeight=160;table.Columns[4].FillWeight=120;
  foreach(var h in new[]{"Roof slope","0 to 20 m²","21 to 60 m²","Over 60 m²"})roofs.Columns.Add(h,h);roofs.Columns[0].FillWeight=300;
  foreach(var g in new[]{spaces,table,roofs})GridAppearance.Apply(g);
  FillTable();spaceSearch.TextChanged+=(s,e)=>Fill();tableSearch.TextChanged+=(s,e)=>FillTable();foreach(var r in NscpLiveLoads.RoofTable)roofs.Rows.Add(r);
  foreach(var c in Inputs.Values){var co=c as ComboBox;if(co!=null)co.SelectedIndexChanged+=(s,e)=>{if(!Restoring)Fill();};var cb=c as CheckBox;if(cb!=null)cb.CheckedChanged+=(s,e)=>{if(!Restoring)Fill();};}
  Shown+=(s,e)=>Fill();}
 protected override void AfterRestore(){base.AfterRestore();Fill();}
 protected override bool OptionalSavedInput(string key){return key=="Included";}protected override bool RetiredInput(string key){return key=="Excluded";}
 /* every space of every building type once (same name and same code row), sorted by name */
 internal IEnumerable<string[]> Rows(){bool part=On("Partitions");var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var list=new List<string[]>();
  foreach(var b in NscpLiveLoads.Buildings.Keys)foreach(var s in NscpLiveLoads.Buildings[b]){double u=NscpLiveLoads.Uniform(s);bool office=s.Row=="13a"||s.Row=="13c";string remark=s.Remark;
   if(part&&office&&!double.IsNaN(u)){remark=(remark==""?"":remark+"; ")+"plus 1.0 kPa partitions";}
   double c=NscpLiveLoads.Concentrated(s);string name=NscpLiveLoads.Name(s.Space);/* one row each for the exit-facility corridors and the assembly lobbies */if(s.Row=="8"&&name.IndexOf("orridor",StringComparison.OrdinalIgnoreCase)>=0&&name.IndexOf("Cell",StringComparison.OrdinalIgnoreCase)<0)name="Corridors (exit facilities, any floor)";if(s.Row=="3c"&&name.StartsWith("Lobby",StringComparison.OrdinalIgnoreCase))name="Lobby / concourse / atrium (assembly)";if(s.Row=="19c")name="Ground floor corridors (schools)";if(s.Row=="13b"&&name.StartsWith("Waiting area",StringComparison.OrdinalIgnoreCase))name="Lobby / waiting area / ground floor corridors";
   var row=new[]{name,double.IsNaN(u)?(s.Row==""?"0.60 - 1.00":"as served"):(u+(part&&office?1.0:0)).ToString("0.0#"),double.IsNaN(c)?"-":c.ToString("0.0#"),NscpLiveLoads.F1(s),NscpLiveLoads.Reducible(s),NscpLiveLoads.Basis(s),remark};
   if(seen.Add(Key(row)))list.Add(row);}
  /* one name on different code rows (corridors of hospitals, schools, libraries ...): the occupancy is added to the name */
  foreach(var g in list.GroupBy(x=>x[0],StringComparer.OrdinalIgnoreCase).Where(g=>g.Count()>1).ToList())foreach(var x in g){var basis=x[5].Replace("Table 205-1, ","");int dash=basis.IndexOf(" - ");string occ=System.Text.RegularExpressions.Regex.Replace(dash>0?basis.Substring(0,dash):basis,@"^\d+\s+","");occ=occ.ToLowerInvariant();if(occ.StartsWith("theaters"))occ="assembly";else if(occ.StartsWith("parking"))occ="parking";else if(occ.StartsWith("reviewing"))occ="bleachers";x[0]=x[0]+" ("+occ+")";}
  return list.OrderBy(x=>x[0],StringComparer.OrdinalIgnoreCase);}
 /* rows keep the app's single height (owner, 17 November 2026); long text shows in full as a tooltip and in the report */
 static void Wrap(DataGridView g){g.ShowCellToolTips=true;}
 bool filling;
 void Fill(){Wrap(spaces);filling=true;try{spaces.Rows.Clear();foreach(var r in ShownRows())spaces.Rows.Add(new object[]{Included(r)}.Concat(r.Cast<object>()).ToArray());}finally{filling=false;}}
 void FillTable(){Wrap(table);Wrap(roofs);table.Rows.Clear();string q=tableSearch.Text.Trim();foreach(var r in NscpLiveLoads.Table){var row=new[]{r.Category,r.Description,r.UniformText,r.ConcentratedText,r.Note};if(q==""||Matches(row,q))table.Rows.Add(row);}}
 internal int SearchSpaces(string query){spaceSearch.Text=query;return spaces.Rows.Count;}
 protected override string BuildReport(){var b=new StringBuilder();foreach(var r in Rows().Where(Included))b.AppendLine(string.Join(" | ",r));return b.ToString();}
 protected override CalculationDocument BuildDocument(){var rep=new RichReport("Live Loads - NSCP 2015");
  rep.Heading("1  BASIS").Paragraph("NSCP 2015 Section 205: floors are designed for the unit live loads of Table 205-1 or the concentrated loads, whichever produce the greater effects; the area of load application is given in Section 205.3.3. Roofs follow Section 205.4. Where office partitions may be moved, a partition load of 1.0 kPa is added"+(On("Partitions")?" (included below for offices)":" (not included below)")+". Live loads may be reduced by Section 205.5 except in places of public assembly and for live loads over 4.8 kPa (except as the section allows). f1 of Eq. 203-3 and 203-5 is 1.0 for places of public assembly, live loads over 4.8 kPa and garages, and 0.5 for the other live loads. Restrooms take the load of the occupancy served, not over 2.4 kPa.");
  rep.Heading("2  LIVE LOADS PER SPACE");
  var chosen=Rows().Where(Included).ToList();if(chosen.Count==0)rep.Paragraph("No space is checked for the report: check the spaces in Live loads per space.");
  if(chosen.Count>0)rep.Table(new[]{"Space","Uniform (kPa)","Conc. (kN)","f1","Reduction","NSCP 2015 basis","Remarks"},chosen,new float[]{22,9,8,5,10,26,20});
  rep.Heading("3  TABLE 205-1  MINIMUM UNIFORM AND CONCENTRATED LIVE LOADS").Table(new[]{"Use or occupancy","Description","Uniform (kPa)","Conc. (kN)","Note"},NscpLiveLoads.Table.Select(r=>new[]{r.Category,r.Description,r.UniformText,r.ConcentratedText,r.Note}),new float[]{28,32,10,9,21});
  rep.Paragraph("Notes for Table 205-1",false,true);foreach(var n in NscpLiveLoads.Notes)rep.Paragraph(n);
  rep.Heading("4  TABLE 205-3  MINIMUM ROOF LIVE LOADS (METHOD 1, kPa BY TRIBUTARY AREA)").Paragraph("Table 205-3 values to be checked against the printed NSCP 2015 Section 205.4 (not yet compared with the code page).").Table(new[]{"Roof slope","0 to 20 m²","21 to 60 m²","Over 60 m²"},NscpLiveLoads.RoofTable,new float[]{55,15,15,15});
  return rep.Build();}
}
}
