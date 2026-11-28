using System;using System.Collections.Generic;using System.Drawing;using System.Globalization;using System.Linq;using System.Text;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* Dead Loads design sheet, a sub tab of Design Loads (owner, 26 November 2026: "ANOTHER SUB TAB FOR DEAD LOAD, DEAD LOAD FLOOR LOAD", with
   the owner's Excel sheet as the layout): floor dead loads per floor type (components x types, total, STAAD input = total without the
   slab self-weight), roof dead loads on the truss top chord / bottom chord / purlins with the purlin weight, wall line loads (level
   height x wall load) and the elevator load with impact. Every table is editable; the values are the designer's. */
sealed class DeadLoadsForm:DesignSheetForm {
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;
 /* the owner's sheet as the starting values */
 internal const string FloorDefault="Load description\tTypical\tCeiling\tRoof Slab\tGutter\tStair\tCanopy\nSlab\t2.95\t\t2.95\t2.95\t2.95\t2.95\nFloor Finish\t0.77\t\t3.60\t\t0.77\t0.48\nPartition Loads\t0.24\t\t\t\t\t\nCeiling\t0.15\t0.15\t0.15\t0.15\t\t\nMEP\t0.20\t0.20\t0.20\t0.20\t0.20\t0.20\nWaterproofing\t\t\t0.07\t0.07\t\t0.05";
 internal const string RoofDefault="Load description\tTop chord\tBottom chord\tPurlin\nRoofing Sheet\t0.05\t\t0.05\nInsulation Sheet\t0.05\t\t0.05\nPurlins\tauto\t\t\nMEP\t0.20\t0.20\t0.20\nCeiling\t\t0.15\t0.15";
 internal const string WallTypesDefault="Wall\tLoad (kPa)\n150 CHB\t2.73\n100 CHB\t2.11\n100 RC\t2.40\nCurtain wall\t0.50\n100 mm AAC 5.5 MPa\t0.873";
 internal const string LevelsDefault="Level / wall\tHeight (m)\nGround Floor\t5.10\nSecond Floor\t4.50\nRoof Deck\t3.00\nParapet Wall\t1.20";
 readonly DataGridView floors=Edit(),floorTotals=View(),roof=Edit(),roofTotals=View(),walls=Edit(),levels=Edit(),lineLoads=View();
 readonly TextBox floorText=Hidden(),roofText=Hidden(),wallText=Hidden(),levelText=Hidden();readonly Label purlinNote=new Label{Dock=DockStyle.Top,Height=46,Padding=new Padding(8),ForeColor=PowTheme.Teal},elevNote=new Label{Width=420,Height=70,Padding=new Padding(0,8,0,0),ForeColor=PowTheme.Teal};
 bool loading;
 /* add a row (then type in it) / delete the selected rows (owner: "allow magdelete ng load description, add floor type, remove wall loads, add level") */
 void RowButtons(FlowLayoutPanel bar,DataGridView g,TextBox t,string addText,string delText){var add=new Button{Text=addText,AutoSize=true};var del=new Button{Text=delText,AutoSize=true};
  add.Click+=(s,e)=>{if(g.Columns.Count==0)return;g.EndEdit();int i=g.Rows.Add();g.CurrentCell=g.Rows[i].Cells[0];g.Focus();g.BeginEdit(true);};
  del.Click+=(s,e)=>{g.EndEdit();var rows=g.SelectedCells.Cast<DataGridViewCell>().Select(c=>c.OwningRow).Concat(g.SelectedRows.Cast<DataGridViewRow>()).Where(x=>!x.IsNewRow).Distinct().ToList();if(rows.Count==0)return;
   loading=true;try{foreach(var x in rows)g.Rows.Remove(x);}finally{loading=false;}Save(g,t);Compute();};bar.Controls.Add(add);bar.Controls.Add(del);}
 static Control Stack(Control a,Control b,float topPercent){var t=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2};t.RowStyles.Add(new RowStyle(SizeType.Percent,topPercent));t.RowStyles.Add(new RowStyle(SizeType.Percent,100-topPercent));a.Dock=DockStyle.Fill;b.Dock=DockStyle.Fill;t.Controls.Add(a,0,0);t.Controls.Add(b,0,1);return t;}
 static TextBox Hidden(){return new TextBox{MaxLength=int.MaxValue,Multiline=true,Visible=false};}
 static DataGridView Edit(){return new DataGridView{Dock=DockStyle.Fill,AllowUserToAddRows=true,AllowUserToDeleteRows=true,RowHeadersVisible=true,RowHeadersWidth=28,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill};}
 static DataGridView View(){return new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill};}
 public DeadLoadsForm():base("Dead Loads (NSCP 2015) - floor, roof, wall and other dead loads","Dead-Loads"){
  /* purlins and the elevator on their own tabs (owner: "hiwalay mo naman yung Elevator") */
  var basis=Section("Roof purlins","Purlin weight for the roof table: purlin mass, span length of the purlin run, purlin spacing and truss spacing.");
  NumberField(basis,"PurlinName","Purlin section","LC 150x50x20x2.0");NumberField(basis,"PurlinMass","Purlin mass (kg/m)","4.32");NumberField(basis,"RoofSpan","Span length of the purlin run (m)","14");NumberField(basis,"PurlinSpacing","Purlin spacing (m)","0.6");NumberField(basis,"TrussSpacing","Truss spacing (m)","4");
  var elev=Section("Elevator","Elevator load on the shaft area, increased for impact.");
  NumberField(elev,"ElevLength","Elevator shaft length (m)","3");NumberField(elev,"ElevWidth","Elevator shaft width (m)","3");NumberField(elev,"ElevMass","Elevator weight (kg)","3800");NumberField(elev,"ElevImpact","Impact increase for elevators (%)","100");
  elev.Controls.Add(elevNote);var gamma=new TextBox{Width=60,Text="23.6"};Inputs.Add("Gamma",gamma);
  foreach(var k in new[]{"FloorTable","RoofTable","WallTypes","Levels"})Inputs.Add(k,k=="FloorTable"?floorText:k=="RoofTable"?roofText:k=="WallTypes"?wallText:levelText);foreach(var t in new[]{floorText,roofText,wallText,levelText})basis.Controls.Add(t);
  floorText.Text=FloorDefault.Replace("\n","\r\n");roofText.Text=RoofDefault.Replace("\n","\r\n");wallText.Text=WallTypesDefault.Replace("\n","\r\n");levelText.Text=LevelsDefault.Replace("\n","\r\n");
  /* floor page: the table to edit, buttons for floor types, the totals under it */
  var fp=new TabPage("Floor dead loads");var fbar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=46,Padding=new Padding(6)};var name=new TextBox{Width=160};var add=new Button{Text="Add floor type",AutoSize=true};var del=new Button{Text="Remove selected floor type",AutoSize=true};
  var slabT=new TextBox{Width=70};var slabBtn=new Button{Text="Slab from thickness (mm) for the selected column",AutoSize=true};
  fbar.Controls.Add(new Label{Text="New floor type",AutoSize=true,Padding=new Padding(0,6,4,0)});fbar.Controls.Add(name);fbar.Controls.Add(add);fbar.Controls.Add(del);fbar.Controls.Add(new Label{Text="   Slab thickness",AutoSize=true,Padding=new Padding(0,6,4,0)});fbar.Controls.Add(slabT);fbar.Controls.Add(slabBtn);fbar.Controls.Add(new Label{Text="γc (kN/m³)",AutoSize=true,Padding=new Padding(0,6,4,0)});fbar.Controls.Add(gamma);fbar.Height=84;
  RowButtons(fbar,floors,floorText,"Add load description","Delete selected load descriptions");
  var fsplit=Stack(floors,floorTotals,62);fp.Controls.Add(fsplit);fp.Controls.Add(fbar);
  fp.Controls.Add(new Label{Dock=DockStyle.Top,Height=40,Padding=new Padding(8),Text="Dead loads in kPa: one row per component, one column per floor type. STAAD input = total without the slab, which STAAD adds by SELFWEIGHT.",ForeColor=PowTheme.Ink});
  AddResultPage(fp);
  add.Click+=(s,e)=>{string n=name.Text.Trim();if(n==""||floors.Columns.Cast<DataGridViewColumn>().Any(c=>c.HeaderText==n))return;floors.Columns.Add(n,n);name.Clear();Save(floors,floorText);Compute();};
  del.Click+=(s,e)=>{if(floors.CurrentCell!=null&&floors.CurrentCell.ColumnIndex>0){floors.Columns.RemoveAt(floors.CurrentCell.ColumnIndex);Save(floors,floorText);Compute();}};
  slabBtn.Click+=(s,e)=>{double t;if(floors.CurrentCell==null||floors.CurrentCell.ColumnIndex<1||!double.TryParse(slabT.Text,NumberStyles.Float,Ci,out t))return;var row=floors.Rows.Cast<DataGridViewRow>().FirstOrDefault(r=>!r.IsNewRow&&Convert.ToString(r.Cells[0].Value).Trim().Equals("Slab",StringComparison.OrdinalIgnoreCase));
   if(row==null){int i=floors.Rows.Add();row=floors.Rows[i];row.Cells[0].Value="Slab";}row.Cells[floors.CurrentCell.ColumnIndex].Value=(t/1000*Number("Gamma")).ToString("0.00",Ci);Save(floors,floorText);Compute();};
  /* roof page */
  var rp=new TabPage("Roof dead loads");var rsplit=Stack(roof,roofTotals,62);rp.Controls.Add(rsplit);var rbar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=46,Padding=new Padding(6)};RowButtons(rbar,roof,roofText,"Add load description","Delete selected load descriptions");rp.Controls.Add(rbar);rp.Controls.Add(purlinNote);
  rp.Controls.Add(new Label{Dock=DockStyle.Top,Height=40,Padding=new Padding(8),Text="Roof dead loads in kPa on the truss top chord, bottom chord and purlins. \"auto\" takes the purlin weight from the purlin inputs. STAAD input: chords × truss spacing, purlin × purlin spacing (kN/m).",ForeColor=PowTheme.Ink});AddResultPage(rp);
  /* walls page: wall types and levels side by side, line loads under them */
  var wp=new TabPage("Wall line loads");var top=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));top.Controls.Add(walls,0,0);top.Controls.Add(levels,1,0);var wsplit=Stack(top,lineLoads,50);
  wp.Controls.Add(wsplit);var wbar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=46,Padding=new Padding(6)};RowButtons(wbar,walls,wallText,"Add wall load","Delete selected wall loads");RowButtons(wbar,levels,levelText,"Add level / wall height","Delete selected levels");wp.Controls.Add(wbar);wp.Controls.Add(new Label{Dock=DockStyle.Top,Height=40,Padding=new Padding(8),Text="Wall loads in kPa of wall face (left) and the heights of the walls (right, levels, parapets, fire exits ...); line load = height × wall load (kN/m).",ForeColor=PowTheme.Ink});AddResultPage(wp);
  foreach(var pair in new[]{Tuple.Create(floors,floorText),Tuple.Create(roof,roofText),Tuple.Create(walls,wallText),Tuple.Create(levels,levelText)}){var g=pair.Item1;var t=pair.Item2;/* saved only on the user's own edits (layout events while the grid is shown must not rewrite the table) */g.CellEndEdit+=(s,e)=>{if(!loading){Save(g,t);Compute();}};g.UserDeletedRow+=(s,e)=>{if(!loading){Save(g,t);Compute();}};}
  foreach(var c in Inputs.Values)c.TextChanged+=(s,e)=>{if(!Restoring&&!loading)Compute();};
  foreach(var g in new[]{floors,floorTotals,roof,roofTotals,walls,levels,lineLoads})GridAppearance.Apply(g);
  /* filled once the window is shown */Shown+=(s,e)=>LoadAll();}
 protected override void AfterRestore(){base.AfterRestore();LoadAll();}
 /* grid <-> tab-separated text kept with the sheet */
 internal static List<string[]> Parse(string text){return (text??"").Replace("\r","").Split('\n').Where(l=>l.Trim()!="").Select(l=>l.Split('\t')).ToList();}
 void Load(DataGridView g,string text){var rows=Parse(text);g.Rows.Clear();if(rows.Count==0){g.Columns.Clear();return;}Columns(g,rows[0]);
  foreach(var r in rows.Skip(1)){var cells=new object[g.Columns.Count];for(int i=0;i<cells.Length;i++)cells[i]=i<r.Length?r[i]:"";g.Rows.Add(cells);}if(g.Columns.Count>0)g.Columns[0].FillWeight=160;}
 static void Save(DataGridView g,TextBox t){var b=new StringBuilder();b.Append(string.Join("\t",g.Columns.Cast<DataGridViewColumn>().Select(c=>c.HeaderText)));
  foreach(DataGridViewRow r in g.Rows){if(r.IsNewRow)continue;var cells=r.Cells.Cast<DataGridViewCell>().Select(c=>Convert.ToString(c.Value).Replace("\t"," ")).ToList();if(cells.All(c=>c.Trim()==""))continue;b.Append("\r\n").Append(string.Join("\t",cells));}t.Text=b.ToString();}
 void LoadAll(){/* the app's grid style (font, colours, padding) also for these grids */foreach(var g in new[]{floors,floorTotals,roof,roofTotals,walls,levels,lineLoads})PowTheme.Apply(g);loading=true;try{Load(floors,floorText.Text);Load(roof,roofText.Text);Load(walls,wallText.Text);Load(levels,levelText.Text);}finally{loading=false;}Compute();}
 static double Num(string s){double v;return double.TryParse((s??"").Trim(),NumberStyles.Float,Ci,out v)?v:0;}
 /* the results (also used by the report and the tests) */
 internal double PurlinKpa(){double span=Number("RoofSpan"),sp=Number("PurlinSpacing"),ts=Number("TrussSpacing");if(span<=0||sp<=0||ts<=0)return 0;int n=(int)Math.Ceiling(span/sp-1e-9)+1;double w=Number("PurlinMass")*9.81/1000;return w*ts*n/(span*ts);}
 internal int PurlinCount(){double span=Number("RoofSpan"),sp=Number("PurlinSpacing");return span>0&&sp>0?(int)Math.Ceiling(span/sp-1e-9)+1:0;}
 internal List<string[]> FloorTotals(){var rows=Parse(floorText.Text);var res=new List<string[]>();if(rows.Count==0)return res;int n=rows[0].Length;var tot=new double[n];var slab=new double[n];
  foreach(var r in rows.Skip(1))for(int i=1;i<n&&i<r.Length;i++){double v=Num(r[i]);tot[i]+=v;if(r[0].Trim().Equals("Slab",StringComparison.OrdinalIgnoreCase))slab[i]+=v;}
  res.Add(new[]{"Total (kPa)"}.Concat(Enumerable.Range(1,n-1).Select(i=>tot[i].ToString("0.00",Ci))).ToArray());res.Add(new[]{"STAAD input, without the slab (kPa)"}.Concat(Enumerable.Range(1,n-1).Select(i=>(tot[i]-slab[i]).ToString("0.00",Ci))).ToArray());return res;}
 internal List<string[]> RoofTotals(){var rows=Parse(roofText.Text);var res=new List<string[]>();if(rows.Count==0)return res;int n=rows[0].Length;var tot=new double[n];double pk=PurlinKpa();
  foreach(var r in rows.Skip(1))for(int i=1;i<n&&i<r.Length;i++)tot[i]+=r[i].Trim().Equals("auto",StringComparison.OrdinalIgnoreCase)?pk:Num(r[i]);
  res.Add(new[]{"Total (kPa)"}.Concat(Enumerable.Range(1,n-1).Select(i=>tot[i].ToString("0.00",Ci))).ToArray());
  res.Add(new[]{"STAAD input (kN/m)"}.Concat(Enumerable.Range(1,n-1).Select(i=>(tot[i]*(rows[0][i].IndexOf("purlin",StringComparison.OrdinalIgnoreCase)>=0?Number("PurlinSpacing"):Number("TrussSpacing"))).ToString("0.00",Ci))).ToArray());
  res.Add(new[]{"Tributary width (m)"}.Concat(Enumerable.Range(1,n-1).Select(i=>(rows[0][i].IndexOf("purlin",StringComparison.OrdinalIgnoreCase)>=0?Number("PurlinSpacing"):Number("TrussSpacing")).ToString("0.##",Ci))).ToArray());return res;}
 internal List<string[]> LineLoads(out string[] header){var wt=Parse(wallText.Text).Skip(1).Where(r=>r.Length>=2&&r[0].Trim()!="").ToList();var lv=Parse(levelText.Text).Skip(1).Where(r=>r.Length>=2&&r[0].Trim()!="").ToList();
  header=new[]{"Level / wall","Height (m)"}.Concat(wt.Select(w=>w[0]+" ("+Num(w[1]).ToString("0.###",Ci)+" kPa)")).ToArray();
  return lv.Select(l=>new[]{l[0],Num(l[1]).ToString("0.00",Ci)}.Concat(wt.Select(w=>(Num(l[1])*Num(w[1])).ToString("0.00",Ci))).ToArray()).ToList();}
 internal double ElevatorKpa(out double kn,out double area){area=Number("ElevLength")*Number("ElevWidth");kn=Number("ElevMass")*9.81/1000;return area>0?kn/area:0;}
 void Compute(){try{var ft=FloorTotals();Fill(floorTotals,Parse(floorText.Text).FirstOrDefault()??new[]{""},ft);var rt=RoofTotals();Fill(roofTotals,Parse(roofText.Text).FirstOrDefault()??new[]{""},rt);string[] h;var ll=LineLoads(out h);Fill(lineLoads,h,ll);
  double kn,area;double e=ElevatorKpa(out kn,out area);purlinNote.Text="Purlins "+Value("PurlinName")+": "+PurlinCount()+" pcs × "+(Number("PurlinMass")*9.81/1000).ToString("0.#####",Ci)+" kN/m × "+Number("TrussSpacing").ToString("0.##",Ci)+" m / ("+Number("RoofSpan").ToString("0.##",Ci)+" × "+Number("TrussSpacing").ToString("0.##",Ci)+" m) = "+PurlinKpa().ToString("0.000",Ci)+" kPa.";elevNote.Text="Elevator: "+kn.ToString("0.###",Ci)+" kN / "+area.ToString("0.##",Ci)+" m² = "+e.ToString("0.###",Ci)+" kPa; with "+Number("ElevImpact").ToString("0",Ci)+" % impact "+(e*(1+Number("ElevImpact")/100)).ToString("0.###",Ci)+" kPa.";}
  catch(Exception ex){purlinNote.Text=ex.Message;}}
 /* columns are rebuilt only when the headers change */
 static void Columns(DataGridView g,string[] header){if(g.Columns.Count==header.Length&&g.Columns.Cast<DataGridViewColumn>().Select(c=>c.HeaderText).SequenceEqual(header))return;g.Columns.Clear();foreach(var h in header)g.Columns.Add(h,h);}
 static void Fill(DataGridView g,string[] header,List<string[]> rows){g.Rows.Clear();Columns(g,header);if(g.Columns.Count>0)g.Columns[0].FillWeight=160;foreach(var r in rows)g.Rows.Add(r.Cast<object>().ToArray());}
 protected override bool OptionalSavedInput(string key){return false;}
 protected override string BuildReport(){var b=new StringBuilder();foreach(var r in FloorTotals())b.AppendLine(string.Join(" | ",r));foreach(var r in RoofTotals())b.AppendLine(string.Join(" | ",r));return b.ToString();}
 protected override CalculationDocument BuildDocument(){var rep=new RichReport("Dead Loads");Func<List<string[]>,float[]> w=rows=>{int n=rows.Count==0?1:rows[0].Length;return Enumerable.Range(0,n).Select(i=>i==0?30f:Math.Max(8f,70f/Math.Max(1,n-1))).ToArray();};
  rep.Heading("1  BASIS").Paragraph("Dead loads by NSCP 2015 Section 204 (weights of materials and construction, Tables 204-1 and 204-2), as entered by the designer. Floor loads in kPa per floor type; the STAAD input leaves out the slab, which the model carries by SELFWEIGHT. Roof loads on the truss chords and purlins as line loads; wall loads as line loads on the beams.");
  var fl=Parse(floorText.Text);if(fl.Count>0){rep.Heading("2  FLOOR DEAD LOADS (kPa)");var all=fl.Concat(FloorTotals()).ToList();rep.Table(fl[0],all.Skip(1).Select(r=>Pad(r,fl[0].Length)),w(all));}
  var rf=Parse(roofText.Text);if(rf.Count>0){rep.Heading("3  ROOF DEAD LOADS");double pk=PurlinKpa();
   rep.Paragraph("Purlins "+Value("PurlinName")+": n = "+Number("RoofSpan").ToString("0.##",Ci)+" / "+Number("PurlinSpacing").ToString("0.##",Ci)+" + 1 = "+PurlinCount()+" pcs; w = "+Number("PurlinMass").ToString("0.##",Ci)+" kg/m × 9.81 = "+(Number("PurlinMass")*9.81/1000).ToString("0.#####",Ci)+" kN/m; W = "+PurlinCount()+" × w × "+Number("TrussSpacing").ToString("0.##",Ci)+" m = "+(PurlinCount()*Number("PurlinMass")*9.81/1000*Number("TrussSpacing")).ToString("0.00",Ci)+" kN on "+(Number("RoofSpan")*Number("TrussSpacing")).ToString("0.##",Ci)+" m² = "+pk.ToString("0.000",Ci)+" kPa.");
   var rows=rf.Skip(1).Select(r=>r.Select(c=>c.Trim().Equals("auto",StringComparison.OrdinalIgnoreCase)?pk.ToString("0.00",Ci):c).ToArray()).ToList();var all=new List<string[]>{rf[0]}.Concat(rows).Concat(RoofTotals()).ToList();rep.Table(rf[0],all.Skip(1).Select(r=>Pad(r,rf[0].Length)),w(all));}
  string[] h;var ll=LineLoads(out h);if(ll.Count>0){rep.Heading("4  WALL LINE LOADS (kN/m = height × wall load)");rep.Table(h,ll,w(new List<string[]>{h}));}
  double kn,area;double e=ElevatorKpa(out kn,out area);if(area>0&&kn>0){rep.Heading("5  ELEVATOR");rep.Paragraph("Weight "+Number("ElevMass").ToString("0",Ci)+" kg × 9.81 = "+kn.ToString("0.###",Ci)+" kN on "+Number("ElevLength").ToString("0.##",Ci)+" × "+Number("ElevWidth").ToString("0.##",Ci)+" = "+area.ToString("0.##",Ci)+" m²: "+e.ToString("0.###",Ci)+" kPa; increased "+Number("ElevImpact").ToString("0",Ci)+" % for impact: "+(e*(1+Number("ElevImpact")/100)).ToString("0.###",Ci)+" kPa.");}
  return rep.Build();}
 static string[] Pad(string[] r,int n){return Enumerable.Range(0,n).Select(i=>i<r.Length?r[i]:"").ToArray();}
}
}
