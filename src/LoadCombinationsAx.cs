using System;using System.Collections.Generic;using System.Drawing;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* the "Torsional amplification Ax" tab: Ax starts at 1.0; after the STAAD run the joint displacements of the EQ cases are pasted and Ax
   (and ACC = 0.05 Ax of case B) is replaced by the computed value; reported as its own section */
partial class LoadCombinationsForm {
 readonly DataGridView axGrid=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill};
 readonly Label axStatus=new Label{Dock=DockStyle.Top,Height=78,Padding=new Padding(8),ForeColor=PowTheme.Ink,Font=ModernUi.Strong};readonly Timer axTimer=new Timer{Interval=700};List<string[]> axRows=new List<string[]>();internal List<AxResult> axResults=new List<AxResult>();
 void AddAxPage(){var sec=Section("Torsional amplification Ax","Ax = (dmax / 1.2 davg)^2, 1.0 to 3.0. Start with Ax = 1.0, run STAAD, then paste the joint displacements of the EQ cases here: Ax and ACC = 0.05 Ax are replaced.");var tab=sec.Parent.Parent as TabPage;
  var box=new TextBox{MaxLength=int.MaxValue,Multiline=true,ScrollBars=ScrollBars.Both,WordWrap=false,Dock=DockStyle.Fill,Font=new Font("Consolas",9f),AcceptsReturn=true};Inputs.Add("AxDisplacements",box);
  var cap=new Label{Dock=DockStyle.Top,Height=40,Padding=new Padding(4),ForeColor=PowTheme.Ink,Text="Paste the joint displacements of the EQ cases: Post-processing > Node > Displacements table, select all, copy (or the .ANL JOINT DISPLACEMENT). The joints and levels come from the STAAD input of the Redundancy factor ρ tab (or an .ANL pasted here)."};
  var paste=new Panel{Dock=DockStyle.Top,Height=230,Padding=new Padding(8)};paste.Controls.Add(box);var anl=new Button{Text="Open .ANL file…",AutoSize=true,Dock=DockStyle.Top};anl.Click+=(s9,e9)=>OpenAnl();paste.Controls.Add(anl);paste.Controls.Add(cap);
  foreach(var h in new[]{"Load case","Level (m)","d edge 1","d edge 2","dmax","davg","dmax / davg","Ax"})axGrid.Columns.Add(h,h);GridAppearance.Apply(axGrid);
  var body=new Panel{Dock=DockStyle.Fill};body.Controls.Add(axGrid);body.Controls.Add(axStatus);body.Controls.Add(paste);if(tab!=null){tab.Controls.Clear();tab.Controls.Add(body);}
  axStatus.Text="Ax = 1.0 until the joint displacements are pasted.";
  box.TextChanged+=(s,e)=>{if(Restoring)return;axTimer.Stop();axTimer.Start();};axTimer.Tick+=(s,e)=>{axTimer.Stop();if(!IsDisposed)ComputeAx();};Disposed+=(s,e)=>axTimer.Dispose();}
 internal void ComputeAx(){axGrid.Rows.Clear();if(Value("AxDisplacements")==""){axStatus.ForeColor=PowTheme.Ink;axStatus.Text="Ax = 1.0 until the joint displacements are pasted.";return;}
  string text;double ax=ComputeAx(out text);axStatus.ForeColor=double.IsNaN(ax)?Color.Firebrick:PowTheme.Teal;foreach(var r in axRows)axGrid.Rows.Add(r);
  if(!double.IsNaN(ax)){string v=ax.ToString("0.0##",CultureInfo.InvariantCulture);if(Value("Ax")!=v)Inputs["Ax"].Text=v;text+="\nAx = "+v+" is used: ACC = 0.05 x "+v+" = "+(0.05*ax).ToString("0.0###",CultureInfo.InvariantCulture)+" (Primary loads).";}axStatus.Text=text;}
 /* the governing Ax (NaN on error) */
 internal double ComputeAx(out string summary){summary="";axRows=new List<string[]>();axResults=new List<AxResult>();try{
   string dispText=Value("AxDisplacements");string modelText;if(RedundancyFactor.HasModel(dispText))modelText=dispText;else{string f;RhoTexts(out modelText,out f);}
   if(!RedundancyFactor.HasModel(modelText))throw new Exception("Paste the STAAD input in the Redundancy factor ρ tab (or an .ANL with the input here): the joints give the floor levels and edges.");
   var model=RedundancyFactor.ReadModel(modelText);var disp=TorsionAmplification.ReadDisplacements(dispText);
   var eq=RedundancyFactor.SeismicCases(modelText).Where(p=>p.Axis!="").ToList();if(eq.Count==0)eq=StaadCombinations.Parse(Value("Primary")).Where(p=>p.Kind=="E"&&p.Axis!="").ToList();
   var results=new List<AxResult>();foreach(var p in eq){if(!disp.Keys.Any(k=>k.EndsWith("|"+p.No.ToString(CultureInfo.InvariantCulture))))continue;results.Add(TorsionAmplification.Compute(model,disp,p.No,p.No+" "+p.Title,p.Axis));}
   axResults=results;
   if(results.Count==0)throw new Exception("The pasted displacements have none of the EQ load cases ("+string.Join(", ",eq.Select(p=>p.No+" "+p.Title))+").");
   Func<double,string> f3=v=>v.ToString("0.###",CultureInfo.InvariantCulture);
   axRows=results.SelectMany(r=>r.Levels.Select(l=>new[]{r.Case,l.Level.ToString("0.##",CultureInfo.InvariantCulture),f3(l.D1),f3(l.D2),f3(l.DMax),f3(l.DAvg),f3(l.Ratio),f3(l.Ax)})).ToList();
   var g=results.OrderByDescending(r=>r.Ax).First();summary="Governing: load case "+g.Case+", "+g.Governing+". Displacements in the load direction at the two extreme edges of each level (units as pasted).";
   return g.Ax;}
  catch(Exception ex){summary=ex.Message;return double.NaN;}}
 void AxReport(RichReport rep,int no){if(Value("AxDisplacements").Trim()=="")return;string text;double ax=ComputeAx(out text);
  rep.Heading(no+"  TORSIONAL AMPLIFICATION FACTOR Ax").Paragraph("Torsional irregularity when dmax > 1.2 davg at a level; Ax = (dmax / 1.2 davg)^2, not less than 1.0 nor more than 3.0; the accidental torsion of case B is applied as ACC = 0.05 Ax. dmax is the larger and davg the average of the displacements in the load direction at the two extreme edges of the level, from the STAAD analysis with Ax = 1.0.");
  rep.Paragraph(text);if(axRows.Count>0)rep.Table(new[]{"Load case","Level (m)","d edge 1","d edge 2","dmax","davg","dmax/davg","Ax"},axRows,new float[]{20,10,10,10,10,10,12,8});
  if(!double.IsNaN(ax))rep.Paragraph("Ax = "+ax.ToString("0.0##",CultureInfo.InvariantCulture)+"; ACC = 0.05 × "+ax.ToString("0.0##",CultureInfo.InvariantCulture)+" = "+(0.05*ax).ToString("0.0###",CultureInfo.InvariantCulture)+".");}
}
}
