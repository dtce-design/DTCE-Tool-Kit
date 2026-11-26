using System;using System.Collections.Generic;using System.Drawing;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* the "Structural irregularities" tab: NSCP 2015 Table 208-9 / 208-10 from the pasted STAAD data and a few inputs, with the suggested structure
   class (owner, 26 November 2026: "for structural irregularities check") */
partial class LoadCombinationsForm {
 readonly DataGridView irrGrid=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill};
 readonly Label irrStatus=new Label{Dock=DockStyle.Top,Height=60,Padding=new Padding(8),Font=ModernUi.Strong,ForeColor=PowTheme.Ink};string irrSuggested="";Button irrUse;List<IrregularityRow> irrRows=new List<IrregularityRow>();
 void AddIrregularPage(){var sec=Section("Structural irregularities","NSCP 2015 Table 208-9 (vertical) and Table 208-10 (plan). Computed from the STAAD data of the ρ and Ax tabs where possible; the rest from the inputs below.");var tab=sec.Parent.Parent as TabPage;
  Func<string,string,int,TextBox> num=(key,text,w)=>{var t=new TextBox{Width=w,Text=text};Inputs.Add(key,t);return t;};
  var weights=new TextBox{Multiline=true,ScrollBars=ScrollBars.Vertical,Width=260,Height=96,Font=new Font("Consolas",9f),AcceptsReturn=true};Inputs.Add("RegWeights",weights);
  var px=num("RegProjX","0",60);var lx=num("RegPlanX","0",60);var pz=num("RegProjZ","0",60);var lz=num("RegPlanZ","0",60);var op=num("RegOpening","0",70);var fa=num("RegFloorArea","0",70);
  var inPlane=new CheckBox{Text="In-plane discontinuity of the lateral elements (208-9 type 4)",AutoSize=true};var weak=new CheckBox{Text="Weak storey: strength < 80 % of the storey above (208-9 type 5)",AutoSize=true};var outPlane=new CheckBox{Text="Out-of-plane offsets (208-10 type 4)",AutoSize=true};
  Inputs.Add("RegInPlane",inPlane);Inputs.Add("RegWeakStory",weak);Inputs.Add("RegOutOfPlane",outPlane);
  Func<string,Control> lab=t=>new Label{Text=t,AutoSize=true};
  var row1=new FlowLayoutPanel{Dock=DockStyle.Top,Height=44,Padding=new Padding(6,4,6,0)};row1.Controls.Add(lab("Re-entrant corner: projection X (m)"));row1.Controls.Add(px);row1.Controls.Add(lab("of plan X (m)"));row1.Controls.Add(lx);row1.Controls.Add(lab("projection Z (m)"));row1.Controls.Add(pz);row1.Controls.Add(lab("of plan Z (m)"));row1.Controls.Add(lz);
  var row2=new FlowLayoutPanel{Dock=DockStyle.Top,Height=44,Padding=new Padding(6,4,6,0)};row2.Controls.Add(lab("Diaphragm openings (m²)"));row2.Controls.Add(op);row2.Controls.Add(lab("of floor area (m²)"));row2.Controls.Add(fa);
  var row3=new FlowLayoutPanel{Dock=DockStyle.Top,Height=40,Padding=new Padding(6,4,6,0)};row3.Controls.Add(inPlane);row3.Controls.Add(weak);row3.Controls.Add(outPlane);
  var wrow=new FlowLayoutPanel{Dock=DockStyle.Top,Height=108,Padding=new Padding(6,4,6,0)};wrow.Controls.Add(lab("Storey weights, one per line: level (m); weight (kN)"));wrow.Controls.Add(weights);
  var check=new Button{Text="Check irregularities",AutoSize=true};var use=new Button{Text="Use the suggested structure class",AutoSize=true,Enabled=false};irrUse=use;var row4=new FlowLayoutPanel{Dock=DockStyle.Top,Height=46,Padding=new Padding(6,4,6,0)};row4.Controls.Add(check);row4.Controls.Add(use);
  foreach(var h in new[]{"Table","Irregularity type","Criterion","Value","Result"})irrGrid.Columns.Add(h,h);irrGrid.Columns[0].FillWeight=30;irrGrid.Columns[1].FillWeight=110;irrGrid.Columns[2].FillWeight=150;irrGrid.Columns[3].FillWeight=150;irrGrid.Columns[4].FillWeight=80;GridAppearance.Apply(irrGrid);
  var body=new Panel{Dock=DockStyle.Fill};body.Controls.Add(irrGrid);body.Controls.Add(irrStatus);body.Controls.Add(row4);body.Controls.Add(row3);body.Controls.Add(row2);body.Controls.Add(row1);body.Controls.Add(wrow);if(tab!=null){tab.Controls.Clear();tab.Controls.Add(body);}
  irrStatus.Text="Paste the STAAD input and EQ member end forces (ρ tab) and the joint displacements (Ax tab), then Check irregularities.";
  check.Click+=(s,e)=>CheckIrregularities();
  use.Click+=(s,e)=>{var combo=(ComboBox)Inputs["Structure"];for(int i=0;i<combo.Items.Count;i++)if(Convert.ToString(combo.Items[i]).StartsWith(irrSuggested+" "))combo.SelectedIndex=i;};
  tab.VisibleChanged+=(s,e)=>{if(tab.Visible&&!Restoring)CheckIrregularities();};}
 IrregularityInput IrrInput(){Func<string,double> d=k=>{double v;return double.TryParse(Value(k),NumberStyles.Float,CultureInfo.InvariantCulture,out v)?v:0;};
  return new IrregularityInput{Weights=Value("RegWeights"),ProjX=d("RegProjX"),PlanX=d("RegPlanX"),ProjZ=d("RegProjZ"),PlanZ=d("RegPlanZ"),Opening=d("RegOpening"),FloorArea=d("RegFloorArea"),InPlane=On("RegInPlane"),WeakStory=On("RegWeakStory"),OutOfPlane=On("RegOutOfPlane")};}
 /* the rows and the suggestion (also used by the report) */
 internal List<IrregularityRow> Irregularities(out string summary){summary="";irrSuggested="";StaadModel model=null;Dictionary<string,double[]> forces=null,disp=null;var eq=new List<StaadPrimary>();
  try{string mt,ft;RhoTexts(out mt,out ft);if(RedundancyFactor.HasModel(Value("AxDisplacements")))mt=Value("AxDisplacements");if(RedundancyFactor.HasModel(mt)){model=RedundancyFactor.ReadModel(mt);eq=RedundancyFactor.SeismicCases(mt).Where(p=>p.Axis!="").ToList();}
   if(eq.Count==0)eq=StaadCombinations.Parse(Value("Primary")).Where(p=>p.Kind=="E"&&p.Axis!="").ToList();
   try{if(ft.Trim()!="")forces=RedundancyFactor.ReadForces(ft);}catch(Exception){}try{if(Value("AxDisplacements")!="")disp=TorsionAmplification.ReadDisplacements(Value("AxDisplacements"));}catch(Exception){}}catch(Exception){}
  string ax;ComputeAx(out ax);var rows=StructuralIrregularity.Check(model,forces,disp,eq,axResults,IrrInput());string why;irrSuggested=StructuralIrregularity.Suggest(rows,axResults,out why);
  summary=(irrSuggested==""?"Suggested structure class: not yet - ":"Suggested structure class: "+irrSuggested+" - ")+why+".";return rows;}
 void CheckIrregularities(){/* the app's grid style for a grid filled after it is shown */PowTheme.Apply(irrGrid);irrGrid.Rows.Clear();string text;irrRows=Irregularities(out text);foreach(var r in irrRows){int i=irrGrid.Rows.Add(r.Table,r.Type,r.Criterion,r.Value,r.Result);if(r.Result.StartsWith(StructuralIrregularity.Irregular))irrGrid.Rows[i].DefaultCellStyle.ForeColor=Color.Firebrick;}
  irrStatus.ForeColor=irrSuggested=="C"?Color.Firebrick:PowTheme.Teal;irrStatus.Text=text+"  Current: structure "+Value("Structure").Substring(0,1)+".";if(irrUse!=null){irrUse.Enabled=irrSuggested!="";irrUse.Text=irrSuggested==""?"Use the suggested structure class":"Use structure "+irrSuggested;}}
 bool IrregularReport(RichReport rep,int no){string text;var rows=Irregularities(out text);if(rows.All(r=>r.Result==StructuralIrregularity.NotChecked||r.Value.StartsWith("marked absent")))return false;
  rep.Heading(no+"  STRUCTURAL IRREGULARITIES (Tables 208-9 and 208-10)").Paragraph("Vertical irregularities of Table 208-9 and plan irregularities of Table 208-10, from the STAAD model, the member end forces and the joint displacements of the EQ cases (storey stiffness k = storey shear / storey drift), and the designer's inputs where marked. A structure with any irregularity is taken as irregular (structure C: orthogonal effects); a regular structure with dmax / davg over 1.05 as unsymmetrical (structure B: natural and accidental torsion).");
  rep.Table(new[]{"Table","Irregularity","Criterion","Value","Result"},rows.Select(r=>new[]{r.Table,r.Type,r.Criterion,r.Value,r.Result}),new float[]{8,22,26,28,16});
  rep.Paragraph(text+" Used: structure "+Value("Structure").Substring(0,1)+".");return true;}
}
}
