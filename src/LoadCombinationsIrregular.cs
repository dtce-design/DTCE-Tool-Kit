using System;using System.Collections.Generic;using System.Drawing;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* the "Structural irregularities" tab: NSCP 2015 Table 208-9 / 208-10 from the pasted STAAD data and a few inputs, with the suggested structure
   class (owner, 26 November 2026: "for structural irregularities check") */
partial class LoadCombinationsForm {
 readonly DataGridView irrGrid=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill};
 readonly Label irrStatus=new Label{Dock=DockStyle.Top,Height=60,Padding=new Padding(8),Font=ModernUi.Strong,ForeColor=PowTheme.Ink};string irrSuggested="";Button irrUse;List<IrregularityRow> irrRows=new List<IrregularityRow>();
 /* one sub tab per irregularity (owner, 26 November 2026: "isang sub tab per irregularity"; "yung inputs na needed para autocompute na lang sana
    from staad"): a Summary with the table and the suggested class, then each check with its criterion, how the value was found, the result
    and only its own inputs (most come from the STAAD input of the Primary loads tab and the ρ / Ax tabs) */
 static readonly string[] IrrTitles={"Soft storey","Weight (mass)","Vertical geometry","In-plane discontinuity","Weak storey","Torsion","Re-entrant corners","Diaphragm","Out-of-plane offsets","Nonparallel systems"};
 static readonly string[] IrrHow={
  "Storey stiffness k = storey shear / storey drift of each EQ case, from the STAAD input, the EQ member end forces (ρ tab) and the joint displacements (Ax tab). A storey is soft when k < 70 % of the storey above or < 80 % of the average of the three storeys above.",
  "Seismic weight of each storey from the Earthquake Load sheet's levels (height @ weight). Irregular when a storey's weight is more than 150 % of an adjacent storey; a lighter roof is excepted.",
  "Plan size of the lateral system (columns) of each storey from the STAAD input. Irregular when a storey is more than 130 % of an adjacent storey; a one-storey rooftop structure (penthouse) is not counted.",
  "An in-plane offset of a lateral-force-resisting element greater than its length. The columns that start on a beam (from the STAAD input) are listed below; mark it present when one of them is offset in the plane of its frame.",
  "Storey strength (the lateral strength of the columns, walls and braces of the storey) below 80 % of the storey above. Not available from the STAAD input: mark it present when the design shows it.",
  "dmax / davg at the edges of each floor from the joint displacements of the EQ cases (Ax tab). Irregular above 1.2.",
  "The recesses of each floor plan found from the beams of the STAAD input: a corner recess projects by its own size, a recess in a side by its depth and the narrower wing beside it. Irregular when both projections exceed 15 % of the plan dimension. Typed values override.",
  "Floor area from the STAAD plan; type the openings (stairs, shafts, voids) - the beams alone do not show where the slab is open. Irregular when the openings exceed 50 % of the floor area.",
  "Columns that start on a beam instead of a column below (from the STAAD input) break the lateral force path: listed below.",
  "Horizontal members of the STAAD input that do not run along X or Z."};
 readonly Label[] irrResult=new Label[10],irrValue=new Label[10],irrHead=new Label[10];readonly TextBox[] irrDetail=new TextBox[10];readonly Timer irrTimer=new Timer{Interval=600};
 void AddIrregularPage(){var sec=Section("Structural irregularities","NSCP 2015 Table 208-9 (vertical) and Table 208-10 (plan): one tab per irregularity. Computed from the STAAD input (Primary loads tab), the EQ member end forces (ρ tab) and the joint displacements (Ax tab); typed values override the automatic ones.");var tab=sec.Parent.Parent as TabPage;
  Func<string,string,int,TextBox> num=(key,text,w)=>{var t=new TextBox{Width=w,Text=text,TextAlign=HorizontalAlignment.Right};Inputs.Add(key,t);return t;};
  var weights=new TextBox{MaxLength=int.MaxValue,Multiline=true,ScrollBars=ScrollBars.Vertical,Width=300,Height=110,Font=new Font("Consolas",9f),AcceptsReturn=true};Inputs.Add("RegWeights",weights);
  var px=num("RegProjX","0",70);var lx=num("RegPlanX","0",70);var pz=num("RegProjZ","0",70);var lz=num("RegPlanZ","0",70);var op=num("RegOpening","0",80);var fa=num("RegFloorArea","0",80);
  var inPlane=new CheckBox{Text="Mark as present (judged from the drawings)",AutoSize=true};var weak=new CheckBox{Text="Mark as present: storey strength < 80 % of the storey above",AutoSize=true};var outPlane=new CheckBox{Text="Mark as present (judged from the drawings)",AutoSize=true};
  Inputs.Add("RegInPlane",inPlane);Inputs.Add("RegWeakStory",weak);Inputs.Add("RegOutOfPlane",outPlane);
  Func<string,Control> lab=t=>new Label{Text=t,AutoSize=true,Margin=new Padding(0,6,6,0)};
  var subs=new NavigationTabs{Dock=DockStyle.Fill};
  /* summary */
  var check=new Button{Text="Check now",AutoSize=true};var use=new Button{Text="Use these seismic load cases",AutoSize=true,Enabled=false};irrUse=use;var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=46,Padding=new Padding(6,4,6,0)};bar.Controls.Add(check);bar.Controls.Add(use);
  foreach(var h in new[]{"Table","Irregularity type","Criterion","Value","Result"})irrGrid.Columns.Add(h,h);irrGrid.Columns[0].FillWeight=30;irrGrid.Columns[1].FillWeight=110;irrGrid.Columns[2].FillWeight=150;irrGrid.Columns[3].FillWeight=150;irrGrid.Columns[4].FillWeight=80;GridAppearance.Apply(irrGrid);
  var summary=new TabPage("Summary"){BackColor=Color.White};summary.Controls.Add(irrGrid);summary.Controls.Add(irrStatus);summary.Controls.Add(bar);subs.TabPages.Add(summary);
  irrGrid.CellDoubleClick+=(s,e)=>{if(e.RowIndex>=0&&e.RowIndex<10)subs.SelectedIndex=e.RowIndex+1;};
  /* one page per check */
  Control[][] own={new Control[0],new Control[]{lab("Storey weights, one per line: level (m)  weight (kN) - filled from the Earthquake Load sheet's levels"),weights},new Control[0],new Control[]{inPlane},new Control[]{weak},new Control[0],
   new Control[]{lab("Override (0 = from the STAAD plan):  projection X (m)"),px,lab("of plan X (m)"),lx,lab("projection Z (m)"),pz,lab("of plan Z (m)"),lz},new Control[]{lab("Openings: stairs, shafts, voids (m²)"),op,lab("Floor area (m²; 0 = from the STAAD plan)"),fa},new Control[]{outPlane},new Control[0]};
  for(int k=0;k<10;k++){var page=new TabPage(IrrTitles[k]){BackColor=Color.White,Padding=new Padding(16,12,16,12)};
   irrHead[k]=new Label{Dock=DockStyle.Top,Height=50,Font=new Font("Segoe UI Semibold",11f),ForeColor=PowTheme.Ink,UseMnemonic=false};
   irrResult[k]=new Label{Dock=DockStyle.Top,Height=34,Font=ModernUi.Strong,UseMnemonic=false};irrValue[k]=new Label{Dock=DockStyle.Top,Height=56,ForeColor=PowTheme.Ink,UseMnemonic=false};
   var inputs=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Padding=new Padding(0,6,0,6),WrapContents=true};foreach(var c in own[k])inputs.Controls.Add(c);
   irrDetail[k]=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BorderStyle=BorderStyle.FixedSingle,BackColor=Color.White,Font=new Font("Consolas",9.5f)};
   page.Controls.Add(irrDetail[k]);if(own[k].Length>0)page.Controls.Add(inputs);page.Controls.Add(irrValue[k]);page.Controls.Add(irrResult[k]);page.Controls.Add(irrHead[k]);subs.TabPages.Add(page);}
  var body=new Panel{Dock=DockStyle.Fill};body.Controls.Add(subs);if(tab!=null){tab.Controls.Clear();tab.Controls.Add(body);}
  irrStatus.Text="Paste the STAAD input (Primary loads tab), the EQ member end forces (ρ tab) and the joint displacements (Ax tab).";
  check.Click+=(s,e)=>CheckIrregularities();
  use.Click+=(s,e)=>{var combo=(ComboBox)Inputs["Structure"];for(int i=0;i<combo.Items.Count;i++)if(Convert.ToString(combo.Items[i]).StartsWith(irrSuggested+" "))combo.SelectedIndex=i;};
  /* rechecked by itself when the tab opens and when one of its inputs changes */
  irrTimer.Tick+=(s,e)=>{irrTimer.Stop();if(!IsDisposed&&IsHandleCreated)CheckIrregularities();};Disposed+=(s,e)=>irrTimer.Dispose();
  foreach(Control c in new Control[]{weights,px,lx,pz,lz,op,fa,inPlane,weak,outPlane}){c.TextChanged+=(s,e)=>{if(Restoring)return;irrTimer.Stop();irrTimer.Start();};var cb=c as CheckBox;if(cb!=null)cb.CheckedChanged+=(s,e)=>{if(Restoring)return;irrTimer.Stop();irrTimer.Start();};}
  tab.VisibleChanged+=(s,e)=>{if(tab.Visible&&!Restoring)CheckIrregularities();};}
 IrregularityInput IrrInput(){Func<string,double> d=k=>{double v;return double.TryParse(Value(k),NumberStyles.Float,CultureInfo.InvariantCulture,out v)?v:0;};
  return new IrregularityInput{Weights=Value("RegWeights"),ProjX=d("RegProjX"),PlanX=d("RegPlanX"),ProjZ=d("RegProjZ"),PlanZ=d("RegPlanZ"),Opening=d("RegOpening"),FloorArea=d("RegFloorArea"),InPlane=On("RegInPlane"),WeakStory=On("RegWeakStory"),OutOfPlane=On("RegOutOfPlane")};}
 /* the rows and the suggestion (also used by the report) */
 internal List<IrregularityRow> Irregularities(out string summary){summary="";irrSuggested="";StaadModel model=null;Dictionary<string,double[]> forces=null,disp=null;var eq=new List<StaadPrimary>();
  try{string mt,ft;RhoTexts(out mt,out ft);if(RedundancyFactor.HasModel(Value("AxDisplacements")))mt=Value("AxDisplacements");/* the model pasted before the run (a .std file or the Nodes / Beams tables) */if(!RedundancyFactor.HasModel(mt)&&RedundancyFactor.HasModel(Value("StaadInput")))mt=Value("StaadInput");if(RedundancyFactor.HasModel(mt)){model=RedundancyFactor.ReadModel(mt);eq=RedundancyFactor.SeismicCases(mt).Where(p=>p.Axis!="").ToList();}else{string err;model=InputModel(out err);}
   if(eq.Count==0)eq=StaadCombinations.Parse(Value("Primary")).Where(p=>p.Kind=="E"&&p.Axis!="").ToList();
   try{if(ft.Trim()!="")forces=RedundancyFactor.ReadForces(ft);}catch(Exception){}try{if(Value("AxDisplacements")!="")disp=TorsionAmplification.ReadDisplacements(Value("AxDisplacements"));}catch(Exception){}}catch(Exception){}
  string ax;ComputeAx(out ax);var rows=StructuralIrregularity.Check(model,forces,disp,eq,axResults,IrrInput());string why;irrSuggested=StructuralIrregularity.Suggest(rows,axResults,out why);
  /* the class only picks the seismic primary cases of the combinations (owner, 26 November 2026: "bat ano yung structure C sa summary? e for load
     combo lang naman yun para madetermine yung primary cases") */
  summary=(irrSuggested==""?"Result: not complete - ":"Result: ")+why+"."+(irrSuggested==""?"":" Seismic load cases for the combinations: "+CaseSet(irrSuggested)+".");return rows;}
 static string CaseSet(string c){return c=="C"?"C, orthogonal 100 % + 30 % (irregular)":c=="B"?"B, accidental torsion ±E (regular, unsymmetrical)":c=="A"?"A, EQ ±X, ±Z (regular, symmetrical)":"-";}
 void CheckIrregularities(){/* the app's grid style for a grid filled after it is shown */PowTheme.Apply(irrGrid);irrGrid.Rows.Clear();string text;irrRows=Irregularities(out text);foreach(var r in irrRows){int i=irrGrid.Rows.Add(r.Table,r.Type,r.Criterion,r.Value,r.Result);if(r.Result.StartsWith(StructuralIrregularity.Irregular))irrGrid.Rows[i].DefaultCellStyle.ForeColor=Color.Firebrick;}
  /* each check on its own tab */
  for(int k=0;k<10&&k<irrRows.Count;k++){var r=irrRows[k];if(irrHead[k]==null)continue;bool bad=r.Result.StartsWith(StructuralIrregularity.Irregular),open=r.Result==StructuralIrregularity.NotChecked;
   irrHead[k].Text="Table "+r.Table+"  ·  "+r.Type+"\n"+r.Criterion;irrResult[k].Text="Result: "+r.Result;irrResult[k].ForeColor=bad?Color.Firebrick:open?PowTheme.Muted:PowTheme.Teal;irrValue[k].Text=r.Value;
   irrDetail[k].Text=("How it is found: "+IrrHow[k]+(string.IsNullOrEmpty(r.Detail)?"":"\n\n"+r.Detail)).Replace("\n","\r\n");var page=irrHead[k].Parent as TabPage;if(page!=null)page.Text=IrrTitles[k]+(bad?" ✕":open?"":" ✓");}
  /* the class follows the check by itself (owner, 28 November 2026: "automatic na sana") */if(irrSuggested!=""){var combo=(ComboBox)Inputs["Structure"];for(int i=0;i<combo.Items.Count;i++)if(Convert.ToString(combo.Items[i]).StartsWith(irrSuggested+" ")&&combo.SelectedIndex!=i)combo.SelectedIndex=i;}
  irrStatus.ForeColor=irrSuggested=="C"?Color.Firebrick:PowTheme.Teal;irrStatus.Text=text+"  Now used: "+CaseSet(Value("Structure").Substring(0,1))+".";if(irrUse!=null){irrUse.Enabled=irrSuggested!="";irrUse.Text=irrSuggested==""?"Use these seismic load cases":"Use "+CaseSet(irrSuggested);}}
 bool IrregularReport(RichReport rep,int no){string text;var rows=Irregularities(out text);if(rows.All(r=>r.Result==StructuralIrregularity.NotChecked||r.Value.StartsWith("marked absent")))return false;
  rep.Heading(no+"  STRUCTURAL IRREGULARITIES (Tables 208-9 and 208-10)").Paragraph("Vertical irregularities of Table 208-9 and plan irregularities of Table 208-10, from the STAAD model, the member end forces and the joint displacements of the EQ cases (storey stiffness k = storey shear / storey drift), and the designer's inputs where marked. A structure with any irregularity is taken as irregular (structure C: orthogonal effects); a regular structure with dmax / davg over 1.05 as unsymmetrical (structure B: natural and accidental torsion).");
  rep.Table(new[]{"Table","Irregularity","Criterion","Value","Result"},rows.Select(r=>new[]{r.Table,r.Type,r.Criterion,r.Value,r.Result}),new float[]{8,22,26,28,16});
  rep.Paragraph(text+" Used in the combinations: "+CaseSet(Value("Structure").Substring(0,1))+".");return true;}
}
}
