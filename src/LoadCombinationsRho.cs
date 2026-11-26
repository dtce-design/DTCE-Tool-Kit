using System;using System.Collections.Generic;using System.Drawing;using System.Globalization;using System.Linq;using System.Text;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* the "Redundancy factor ρ" tab of the Load Combinations sheet: paste the STAAD input and the member end forces of the EQ cases; ρ per
   direction from Eq. 208-20 (RedundancyFactor). Owner (26 November 2026, "ikaw na bahala sa redundancy basta paste ko na lang yung required"):
   everything else is automatic - the EQ cases and directions, the lateral system, A_B, the column pairs - and ρ goes into Factors */
partial class LoadCombinationsForm {
 readonly DataGridView rhoGrid=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill};
 readonly Label rhoStatus=new Label{Dock=DockStyle.Top,Height=92,Padding=new Padding(8),ForeColor=PowTheme.Teal,Font=ModernUi.Strong};
 readonly Timer rhoTimer=new Timer{Interval=700};double rhoFound=double.NaN;List<string[]> rhoRows=new List<string[]>();
 internal static readonly string[] RhoSystems={"Automatic (braced frame if the model has braces, else moment frame)","Moment-resisting frame","Special moment-resisting frame (SMRF, ρ ≤ 1.25)","Braced frame","Dual system (80 % of r_i)","Shear walls (typed below)"};
 void AddRhoPage(){var sec=Section("Redundancy factor ρ","NSCP 2015 Eq. 208-20: ρ = 2 − 6.1 / (r_max √A_B). Paste the STAAD input and the member end forces; the rest is automatic and ρ goes into Factors.");var tab=sec.Parent.Parent as TabPage;
  var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=96,Padding=new Padding(8),WrapContents=true};Func<string,Control,Control> pair=(t,c)=>{var p=new FlowLayoutPanel{AutoSize=true,Margin=new Padding(0,0,18,6)};p.Controls.Add(new Label{Text=t,AutoSize=true,Padding=new Padding(0,6,4,0),Font=ModernUi.Strong});p.Controls.Add(c);return p;};
  var system=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=470};system.Items.AddRange(RhoSystems);system.SelectedIndex=0;var zone=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=90};zone.Items.AddRange(new object[]{"Zone 4","Zone 2"});zone.SelectedIndex=0;
  var area=new TextBox{Width=110};var go=new Button{Text="Recompute ρ",AutoSize=true};
  bar.Controls.Add(pair("Lateral system",system));bar.Controls.Add(pair("Seismic zone",zone));bar.Controls.Add(pair("A_B (m²), blank = from the base joints",area));bar.Controls.Add(go);
  Func<string,string,TextBox> box=(key,hint)=>{var t=new TextBox{Multiline=true,ScrollBars=ScrollBars.Both,WordWrap=false,Dock=DockStyle.Fill,Font=new Font("Consolas",9f),AcceptsReturn=true};Inputs.Add(key,t);return t;};
  var model=box("RhoModel","");var forces=box("RhoForces","");var walls=box("RhoWalls","");Inputs.Add("RhoSystem",system);Inputs.Add("RhoZone",zone);Inputs.Add("RhoArea",area);
  Func<string,Control,Control> titled=(t,c)=>{var p=new Panel{Dock=DockStyle.Fill,Padding=new Padding(4)};p.Controls.Add(c);p.Controls.Add(new Label{Text=t,Dock=DockStyle.Top,Height=34,ForeColor=PowTheme.Ink});return p;};
  var pastes=new TableLayoutPanel{Dock=DockStyle.Top,Height=260,ColumnCount=3};for(int k=0;k<3;k++)pastes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,k==2?20:40));
  pastes.Controls.Add(titled("1  Paste the STAAD input (.std): Edit > Input file, select all, copy",model),0,0);pastes.Controls.Add(titled("2  Paste the member end forces: Post-processing > Beam > End Forces table, select all, copy (or the .ANL output)",forces),1,0);
  pastes.Controls.Add(titled("Shear walls only (lateral system Shear walls): bottom m; story shear kN; wall shear kN; lw m",walls),2,0);
  foreach(var h in new[]{"Load case","Story from (m)","to (m)","Story shear","Governing element","Element shear","r_i","≤ 2/3 h"})rhoGrid.Columns.Add(h,h);rhoGrid.Columns[4].FillWeight=330;GridAppearance.Apply(rhoGrid);
  var body=new Panel{Dock=DockStyle.Fill};body.Controls.Add(rhoGrid);body.Controls.Add(rhoStatus);body.Controls.Add(pastes);body.Controls.Add(bar);if(tab!=null){tab.Controls.Clear();tab.Controls.Add(body);}
  go.Click+=(s,e)=>ComputeRho();rhoTimer.Tick+=(s,e)=>{rhoTimer.Stop();if(!IsDisposed)ComputeRho();};
  foreach(Control c in new Control[]{model,forces,walls,system,zone,area})c.TextChanged+=(s,e)=>{if(Restoring)return;rhoTimer.Stop();rhoTimer.Start();};Disposed+=(s,e)=>rhoTimer.Dispose();}
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;
 static string Fr(double v,string f="0.###"){return v.ToString(f,Ci);}
 internal void ComputeRho(){rhoGrid.Rows.Clear();rhoFound=double.NaN;bool walls=Value("RhoSystem").StartsWith("Shear");if(walls?Value("RhoWalls").Trim()=="":Value("RhoModel").Trim()==""&&Value("RhoForces").Trim()==""){rhoStatus.ForeColor=PowTheme.Ink;rhoStatus.Text="Paste the STAAD input and the member end forces; ρ is computed and put into Factors automatically.";return;}
  /* the primary loads follow the pasted model while they are still the example (or empty) */string mt,ft;RhoTexts(out mt,out ft);var heads=PrimaryLines(mt);string prim=Value("Primary").Replace("\r","").Trim();if(heads!=""&&(prim==""||prim==StaadCombinations.Example.Trim()))Inputs["Primary"].Text=heads.Replace("\n","\r\n");
  string text;rhoFound=ComputeRho(out text,true);rhoStatus.ForeColor=double.IsNaN(rhoFound)?Color.Firebrick:PowTheme.Teal;
  /* the computed ρ goes straight into the combinations */if(!double.IsNaN(rhoFound)){string v=Fr(rhoFound,"0.0##");if(Value("Rho")!=v)Inputs["Rho"].Text=v;text+="\nρ = "+v+" is used in the combinations (Factors).";}rhoStatus.Text=text;}
 /* the LOAD n ... lines (primary load cases) of a STAAD input */
 internal static string PrimaryLines(string text){var lines=new List<string>();foreach(var raw in (text??"").Replace("\r","").Split('\n')){string line=System.Text.RegularExpressions.Regex.Replace(raw.Trim(),@"^\d+\.\s+","");if(System.Text.RegularExpressions.Regex.IsMatch(line,@"^LOAD\s+\d+",System.Text.RegularExpressions.RegexOptions.IgnoreCase)&&!System.Text.RegularExpressions.Regex.IsMatch(line,@"\bCOMB",System.Text.RegularExpressions.RegexOptions.IgnoreCase))lines.Add(line);}return string.Join("\n",lines);}
 /* which box holds the model and which the end forces, whatever the order they were pasted in (an .ANL holds both) */
 void RhoTexts(out string modelText,out string forceText){string a=Value("RhoModel"),b=Value("RhoForces");bool am=RedundancyFactor.HasModel(a),bm=RedundancyFactor.HasModel(b);
  if(!am&&bm){modelText=b;forceText=a.Trim()!=""?a:b;}else{modelText=a;forceText=b.Trim()!=""?b:a;}}
 /* the governing ρ (NaN on error); fills the grid when asked */
 internal double ComputeRho(out string summary,bool fill){summary="";rhoRows=new List<string[]>();try{
   string sys=Value("RhoSystem");string kind=sys.StartsWith("Automatic")?"Auto":sys.StartsWith("Special")?"Moment SMRF":sys.StartsWith("Moment")?"Moment":sys.StartsWith("Braced")?"Braced":sys.StartsWith("Dual")?"Dual":"Walls";bool zone2=Value("RhoZone")=="Zone 2";
   var results=new List<RhoResult>();double area=double.NaN,typed;
   if(kind=="Walls"){/* typed: bottom; V; Vwall; lw */var r=new RhoResult{Case="Shear walls (typed)",Axis=""};
    foreach(var line in Value("RhoWalls").Split('\n')){var t=line.Split(new[]{';',',','\t'},StringSplitOptions.RemoveEmptyEntries).Select(x=>x.Trim()).ToArray();double b,v,vw,lw;if(t.Length<4||!double.TryParse(t[0],NumberStyles.Float,Ci,out b)||!double.TryParse(t[1],NumberStyles.Float,Ci,out v)||!double.TryParse(t[2],NumberStyles.Float,Ci,out vw)||!double.TryParse(t[3],NumberStyles.Float,Ci,out lw)||v<=0||lw<=0)continue;
     r.Stories.Add(new RhoStory{Bottom=b,Top=double.NaN,Shear=v,Element=vw*3.05/lw,Element0="wall "+Fr(vw)+" kN x 3.05 / "+Fr(lw)+" m",Ratio=vw*3.05/lw/v,Counted=true});}
    if(r.Stories.Count==0)throw new Exception("Shear walls: type one line per story (bottom m; story shear kN; wall shear kN; lw m), for the stories at or below two-thirds of the height.");
    var g=r.Stories.OrderByDescending(x=>x.Ratio).First();r.RMax=g.Ratio;r.Governing="r_max = "+Fr(g.Ratio)+" at "+Fr(g.Bottom,"0.##")+" m";results.Add(r);}
   else{string modelText,forceText;RhoTexts(out modelText,out forceText);var model=RedundancyFactor.ReadModel(modelText);var forces=RedundancyFactor.ReadForces(forceText);area=RedundancyFactor.BaseArea(model);
    if(kind=="Auto"){kind=RedundancyFactor.HasBraces(model)?"Braced":"Moment";summary+="Lateral system (automatic): "+(kind=="Braced"?"braced frame - the model has braces":"moment-resisting frame - no braces in the model")+".\n";}
    /* the EQ cases and their directions: the LOAD cases of the STAAD input, else the Primary loads, else the titles of the force table */
    var eq=RedundancyFactor.SeismicCases(modelText).Where(p=>p.Axis!="").ToList();if(eq.Count==0)eq=StaadCombinations.Parse(Value("Primary")).Where(p=>p.Kind=="E"&&p.Axis!="").ToList();if(eq.Count==0)eq=RedundancyFactor.TableCases(forceText).Where(p=>p.Axis!="").ToList();
    if(eq.Count==0)throw new Exception("No seismic load case with an X or Z direction was found (LOAD n LOADTYPE SEISMIC TITLE EQ+X, or UBC / IBC LOAD X): paste the whole STAAD input.");
    foreach(var p in eq){if(!forces.Keys.Any(k=>k.Split('|')[1]==p.No.ToString(Ci)))continue;var r=RedundancyFactor.Compute(model,forces,p.No,p.No+" "+p.Title,p.Axis,kind);results.Add(r);}
    if(results.Count==0)throw new Exception("The pasted end forces have none of the EQ load cases ("+string.Join(", ",eq.Select(p=>p.No+" "+p.Title))+").");
    if(model.Notes.Count>0)summary+=string.Join(" ",model.Notes.Distinct())+"\n";}
   if(double.TryParse(Value("RhoArea"),NumberStyles.Float,Ci,out typed)&&typed>0)area=typed;if(!(area>0))throw new Exception("Enter the ground floor area A_B (m²).");
   rhoRows=results.SelectMany(r=>r.Stories.Select(st=>new[]{r.Case,Fr(st.Bottom,"0.##"),double.IsNaN(st.Top)?"":Fr(st.Top,"0.##"),Fr(st.Shear,"0.##"),st.Element0,Fr(st.Element,"0.##"),Fr(st.Ratio),st.Counted?"yes":"no"})).ToList();
   if(fill)foreach(var row in rhoRows)rhoGrid.Rows.Add(row);
   var lines=new List<string>();double governing=1;
   foreach(var g in results.GroupBy(r=>r.Axis)){var worst=g.OrderByDescending(r=>r.RMax).First();double raw;string note;double rho=RedundancyFactor.Rho(worst.RMax,area,kind,zone2,out raw,out note);governing=Math.Max(governing,rho);
    lines.Add((g.Key==""?"":"Direction "+g.Key+", ")+"load case "+worst.Case+": "+worst.Governing+". ρ = 2 − 6.1/("+Fr(worst.RMax)+" × √"+Fr(area,"0.#")+") = "+Fr(raw)+"; "+(note!=""?note:"ρ = "+Fr(rho))+".");}
   summary+=string.Join("\n",lines)+"\nA_B = "+Fr(area,"0.#")+" m²"+(typed>0?" (typed)":" (from the base joints)")+". Use ρ = "+Fr(governing,"0.0##")+" (the larger direction; ρ = 1.0 for drift). Story shears are the sums of the column shears and brace horizontal components of each story.";
   return governing;}
  catch(Exception ex){summary=ex.Message;return double.NaN;}}
 /* report: section "Redundancy factor" when the STAAD model / forces (or the typed walls) were given */
 void RhoReport(RichReport rep,int no,double rhoUsed){bool walls=Value("RhoSystem").StartsWith("Shear");if(walls?Value("RhoWalls").Trim()=="":Value("RhoForces").Trim()==""&&Value("RhoModel").Trim()=="")return;string text;double rho=ComputeRho(out text,false);
  rep.Heading(no+"  REDUNDANCY FACTOR ρ (Section 208.5.1.1)").Paragraph("ρ = 2 − 6.1 / (r_max √A_B) (Eq. 208-20), 1.0 ≤ ρ ≤ 1.5; a special moment-resisting frame shall have ρ ≤ 1.25; ρ = 1.0 for drift and in Seismic Zone 2. r_i is the ratio of the design story shear resisted by the single element carrying the most shear to the total story shear, for each story at or below two-thirds of the height: for moment frames the shear of the two adjacent columns of a bay (70 % of a column with bays on both sides), for braced frames the horizontal component of the most loaded brace, for shear walls the wall shear × 3.05 / lw; a dual system takes 80 % of the value. Lateral system: "+Value("RhoSystem")+".");
  foreach(var line in text.Split('\n'))if(line.Trim()!="")rep.Paragraph(line);
  if(rhoRows.Count>0)rep.Table(new[]{"Load case","Story from (m)","to (m)","Story shear","Governing element","Element shear","r_i","≤ 2/3 h"},rhoRows,new float[]{12,9,7,10,30,10,8,8});
  if(!double.IsNaN(rho)&&Math.Abs(rho-rhoUsed)>0.0005)rep.Paragraph("Note: the combinations use ρ = "+Fr(rhoUsed,"0.0##")+" (Factors), not the computed "+Fr(rho,"0.0##")+".");}
}
}
