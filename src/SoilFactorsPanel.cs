using System;using System.Drawing;using System.Globalization;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Earthworks Start soil factors (owner, 18 November 2026: "medyo magulo yung factors na soil swell etc., paki-explain at
// paki-ayos"): one row per factor with a plain name, its value, what it means and a worked example, plus a live
// example of the loose volume needed for compacted fill. The values, their saving and the formulas are unchanged.
partial class ProgramOfWorksForm {
 Label soilExample;
 void ArrangeSoilFactors(FlowLayoutPanel host){host.Controls.Clear();host.FlowDirection=FlowDirection.TopDown;host.WrapContents=false;host.AutoSize=true;
  Func<string,NumericUpDown,string,Control> row=(name,input,meaning)=>{var line=new FlowLayoutPanel{AutoSize=true,WrapContents=false,Margin=new Padding(0,2,0,2)};
   line.Controls.Add(new Label{Text=name,Width=300,Height=30,TextAlign=ContentAlignment.MiddleLeft,Font=new Font("Segoe UI",9.5f,FontStyle.Bold)});input.Margin=new Padding(0,4,14,0);line.Controls.Add(input);
   line.Controls.Add(new Label{Text=meaning,Width=720,AutoSize=false,Height=34,TextAlign=ContentAlignment.MiddleLeft,ForeColor=PowTheme.Muted});return line;};
  host.Controls.Add(new Label{Text="SOIL FACTORS",AutoSize=true,Font=new Font("Segoe UI",11f,FontStyle.Bold),Margin=new Padding(0,6,0,4)});
  host.Controls.Add(row("Reusable soil from excavation %",suitableSoil,"Part of the excavated soil that is good enough to reuse as fill. 100% = all of it; the rest is disposed of as unsuitable."));
  host.Controls.Add(row("Swell: in-place to loose %",borrowSwell,"Dug-up soil is bulkier. 20% means 1.00 m3 in place (bank) becomes 1.20 m3 loose - the volume you haul or buy."));
  host.Controls.Add(row("Shrinkage: in-place to compacted %",borrowShrink,"Compacted fill is denser. 10% means 1.00 m3 in place becomes 0.90 m3 after compaction."));
  host.Controls.Add(row("Gravel purchase allowance %",gravelAllowance,"Added to the installed gravel volume only (5% = installed x 1.05). Soil swell / shrinkage is not applied to gravel."));
  soilExample=new Label{AutoSize=false,Width=1040,Height=46,Margin=new Padding(0,8,0,0),ForeColor=PowTheme.Ink,Font=new Font("Segoe UI",9.5f)};host.Controls.Add(soilExample);
  EventHandler update=(s,e)=>UpdateSoilExample();foreach(var input in new[]{suitableSoil,borrowSwell,borrowShrink})input.ValueChanged+=update;UpdateSoilExample();}
 void UpdateSoilExample(){if(soilExample==null)return;double swell=(double)borrowSwell.Value/100,shrink=(double)borrowShrink.Value/100;if(shrink>=1){soilExample.Text="";return;}double loose=100/(1-shrink)*(1+swell);
  soilExample.Text="Fill areas are measured COMPACTED. Loose soil needed = compacted / (1 - shrinkage) x (1 + swell).  Example: 100 m3 compacted / "+(1-shrink).ToString("0.00",CultureInfo.InvariantCulture)+" x "+(1+swell).ToString("0.00",CultureInfo.InvariantCulture)+" = "+loose.ToString("0.00",CultureInfo.InvariantCulture)+" m3 loose (taken from suitable excavation first, then common borrow).";}
}
}
