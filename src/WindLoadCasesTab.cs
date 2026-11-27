using System;using System.Collections.Generic;using System.Drawing;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* the wind load cases on the STAAD model, in the Wind Load sheet (owner, 27 November 2026: "move the tab and yung report doon sa windload na
   section ilagay"): the design wind load cases of Figure 207B.4-8 and a plan of every wind case, with the torsion of each level. The model
   and the LOAD numbers come from Load Combinations (link wind-model: the Nodes / Beams tables or the .std of its Primary loads tab). */
partial class WindDesignForm {
 readonly FlowLayoutPanel windCasePanel=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,BackColor=Color.White,Padding=new Padding(10)};
 readonly Label windCaseNote=new Label{Dock=DockStyle.Top,Height=46,Padding=new Padding(10,8,10,4),ForeColor=PowTheme.Ink};
 List<WindCaseFigure> caseFigs=new List<WindCaseFigure>();List<WindLoadCaseFigure> loadFigs=new List<WindLoadCaseFigure>();List<string[]> caseTorsion=new List<string[]>();string caseKey="";
 void AddWindCasesPage(){var page=new TabPage("Wind load cases");page.Controls.Add(windCasePanel);page.Controls.Add(windCaseNote);AddResultPage(page);windCaseNote.Text="Calculate with building pressures and paste the STAAD model on the Primary loads tab of Load Combinations to see the wind load cases.";}
 /* the model, the LOAD titles and the case settings from Load Combinations */
 internal static StaadModel LinkedModel(out Dictionary<string,string> titles,out bool all,out bool exempt){titles=new Dictionary<string,string>();all=true;exempt=false;var l=DesignLoadsLink.Get("wind-model");if(l==null)return null;
  string v;all=!l.TryGetValue("all",out v)||v!="0";exempt=l.TryGetValue("exempt",out v)&&v=="1";
  if(l.TryGetValue("titles",out v))foreach(var line in v.Split('\n')){int i=line.IndexOf('=');if(i>0)titles[line.Substring(0,i)]=line.Substring(i+1);}
  string nodes,beams;l.TryGetValue("nodes",out nodes);l.TryGetValue("beams",out beams);try{if(RedundancyFactor.HasModel(nodes))return RedundancyFactor.ReadModel(nodes);if(!string.IsNullOrWhiteSpace(nodes)&&!string.IsNullOrWhiteSpace(beams))return RedundancyFactor.ReadTables(nodes,beams);}catch(Exception){}return null;}
 void UpdateWindCases(List<WindFaceTypes> types){Dictionary<string,string> titles;bool all,exempt;StaadModel model;try{model=LinkedModel(out titles,out all,out exempt);}catch(Exception){model=null;titles=null;all=true;exempt=false;}
  var link=types.ToDictionary(t=>t.Travel,t=>t.Pack());string key=string.Join(";",link.Select(k=>k.Key+k.Value))+"#"+(model==null?0:model.Joints.Count+model.Members.Count*7)+"#"+(titles==null?"":string.Join(";",titles.Select(k=>k.Key+k.Value)))+all+exempt;
  if(key==caseKey)return;caseKey=key;caseFigs=new List<WindCaseFigure>();loadFigs=new List<WindLoadCaseFigure>();caseTorsion=new List<string[]>();
  foreach(Control c in windCasePanel.Controls.Cast<Control>().ToList()){var pb=c as PictureBox;if(pb!=null&&pb.Image!=null)pb.Image.Dispose();c.Dispose();}windCasePanel.Controls.Clear();
  if(model==null||types.Count==0){windCaseNote.Text="Paste the STAAD model (Nodes + Beams tables or the .std) on the Primary loads tab of Load Combinations to see the wind load cases on the building.";return;}
  try{/* one figure per wind load case (owner: "isa isahin mo based din kung ilan wind load cases"), drawn from the case's own STAAD text;
    the per-level outlines are added only when the levels differ (Case 1 figures of WindCaseFigure) */
   string cn;var texts=WindEdgeLoads.CaseTexts(link,model,out cn,all,!exempt);loadFigs=WindLoadCaseFigure.Build(model,texts,titles,types.Max(t=>t.Top));
   caseFigs=WindCaseFigure.Build(model,link,titles);if(caseFigs.Select(f=>f.Types.Travel).Distinct().Count()==caseFigs.Count)caseFigs.Clear();
   if(all&&!exempt){var tp=types.ToDictionary(t=>t.Travel);string nn;var plans=WindEdgeLoads.Plans(model,0,types.Max(t=>t.Top),out nn);
    foreach(var t in types)foreach(var l in WindEdgeLoads.Torsion(model,plans,new List<WindFaceTypes>{t},0.75,0))caseTorsion.Add(new[]{"Case 2, "+t.Travel,F(l.Y),F(l.H),string.Join("; ",l.Parts),F(l.Mt),(l.Edge.Count>=3?l.Edge.Count+" outline joints, Σr² = "+F(l.SumR2)+" m²":l.J2+" / "+l.J1+", d = "+F(l.D))});
    foreach(var x in new[]{"+X","-X"})foreach(var z in new[]{"+Z","-Z"}){if(!tp.ContainsKey(x)||!tp.ContainsKey(z))continue;foreach(var l in WindEdgeLoads.Torsion(model,plans,new List<WindFaceTypes>{tp[x],tp[z]},0.563,0))caseTorsion.Add(new[]{"Case 4, "+x+z,F(l.Y),F(l.H),string.Join("; ",l.Parts),F(l.Mt),(l.Edge.Count>=3?l.Edge.Count+" outline joints, Σr² = "+F(l.SumR2)+" m²":l.J2+" / "+l.J1+", d = "+F(l.D))});}}}
  catch(Exception ex){windCaseNote.Text="Wind load cases: "+ex.Message;return;}
  windCaseNote.Text=loadFigs.Count+" wind load cases (NSCP 2015 Figure 207B.4-8), one plan each on the STAAD model (top view: X to the right, Z down). The STAAD text of the cases is in Load Combinations > STAAD load combinations.";
  Action<Action<Graphics,RectangleF>,Func<float,float,float>> add=(draw,height)=>{float w=1250,fs=13,h=height(w,fs);var bmp=new Bitmap((int)w+20,(int)h+20);using(var g=Graphics.FromImage(bmp)){g.Clear(Color.White);g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;draw(g,new RectangleF(10,10,w,h));}windCasePanel.Controls.Add(new PictureBox{Image=bmp,SizeMode=PictureBoxSizeMode.AutoSize,Margin=new Padding(0,0,0,14),BorderStyle=BorderStyle.FixedSingle});};
  foreach(var f in loadFigs){var fig=f;add((g,r)=>fig.Draw(g,r,13),(w,fs)=>{using(var b0=new Bitmap(1,1))using(var g0=Graphics.FromImage(b0))return fig.Height(g0,w,fs);});}
  foreach(var f in caseFigs){var fig=f;add((g,r)=>fig.Draw(g,r,13),(w,fs)=>{using(var b0=new Bitmap(1,1))using(var g0=Graphics.FromImage(b0))return fig.Height(g0,w,fs);});}}
 /* report pages: the case figures flow down the pages, each narrowed until it fits one page */
 void AddWindCasePages(CalculationDocument doc){if(loadFigs.Count==0&&caseFigs.Count==0)return;const float fs=7.7f,top=172,bottom=780;
  /* the cases flow down the pages, as many per page as fit */
  foreach(var f in loadFigs)f.MaxPlan=125;
  var items=loadFigs.Select(f=>new{Draw=(Action<Graphics,RectangleF>)((g,r)=>f.Draw(g,r,fs)),H=(Func<float,float>)(w=>f.ReportHeight(w,fs)),Caption=""}).ToList();
  foreach(var f in caseFigs){var fig=f;items.Add(new{Draw=(Action<Graphics,RectangleF>)((g,r)=>fig.Draw(g,r,fs)),H=(Func<float,float>)(w=>fig.ReportHeight(w,fs)),Caption="Outline of levels "+string.Join(", ",fig.Levels.Select(l=>F(l.Y)))+" m, wind toward "+fig.Types.Travel+"."});}
  var cur=new List<Tuple<Action<Graphics,RectangleF>,RectangleF,string>>();float y=top;bool first=true;
  Action flush=()=>{if(cur.Count==0)return;var parts=cur.ToList();bool f1=first;first=false;doc.Sheets.Add(g=>{CalculationDocument.Section(g,f1?"WIND LOAD CASES (NSCP 2015 FIG. 207B.4-8)":"WIND LOAD CASES (CONTINUED)",130);
   foreach(var q in parts){RichReport.Raster(q.Item1)(g,q.Item2);if(q.Item3!="")CalculationDocument.Text(g,q.Item3,CalculationDocument.L,q.Item2.Bottom+2,CalculationDocument.CW,20);}});cur.Clear();y=top;};
  foreach(var it in items){float w=CalculationDocument.CW,h=it.H(w);while(h>bottom-top-30&&w>240){w*=0.92f;h=it.H(w);}if(y+h>bottom)flush();
   cur.Add(Tuple.Create(it.Draw,new RectangleF(CalculationDocument.L+(CalculationDocument.CW-w)/2,y,w,h),it.Caption));y+=h+(it.Caption!=""?24:14);}flush();
  for(int k=0;k<caseTorsion.Count;k+=18){var rows=caseTorsion.Skip(k).Take(18).ToList();doc.Sheets.Add(g=>{CalculationDocument.Section(g,"WIND LOAD CASES - TORSION OF EACH LEVEL (CASES 2 AND 4)",130);
   CalculationDocument.Table(g,new[]{"Case","Level m","h m","MT = f (PW + PL) B h × 0.15 B (kN·m)","MT kN·m","Joints (F = MT r / Σr²)"},rows,172,new float[]{55,38,28,262,58,80},30);});}}
}
}
