using System;using System.Linq;using System.Collections.Generic;using System.Drawing;using System.Windows.Forms;
namespace BeamScheduleFormatter {
partial class WindDesignForm {
 readonly HashSet<string> buildingKeys=new HashSet<string>();
 readonly DataGridView pressureGrid=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.AllCells};
 readonly Label pressureStatus=new Label{Dock=DockStyle.Top,Height=54,Padding=new Padding(12)};
 void AddBuildingInputs(){
  var panel=Section("Building inputs","Directional MWFRS pressure calculations for enclosed / partially enclosed gable, hip or flat-roof rectangular buildings. Enter actual dimensions. Ridge direction fixes the four building axes; terrain is assessed in eight compass directions.");
  Toggle(panel,"BuildingEnabled","Include building wind pressures",false);
  NumberField(panel,"BuildingWidth","Building width across ridge (m)","");NumberField(panel,"BuildingLength","Building length along ridge (m)","");NumberField(panel,"Eave","Eave height (m)","");NumberField(panel,"Apex","Apex height (m); equal eave for flat roof","");
  Choice(panel,"RidgeDirection","Ridge alignment",new[]{"N–S","NE–SW","E–W","NW–SE"});Choice(panel,"BuildingSystem","Structural system",new[]{"MRF Steel","MRF Concrete","Other system"});Choice(panel,"Enclosure","Enclosure classification",new[]{"Enclosed","Partially enclosed"});
  Toggle(panel,"AutoG","Calculate gust effect factor G",true);NumberField(panel,"UserG","G when automatic gust is unchecked","0.85");NumberField(panel,"Damping","Damping ratio for flexible response","0.02");NumberField(panel,"FrequencyN","Natural frequency normal (Hz); 0 = estimate","0");NumberField(panel,"FrequencyP","Natural frequency parallel (Hz); 0 = estimate","0");NumberField(panel,"OpeningHeight","Positive internal qi height (m); 0 = mean roof","0");
  foreach(string key in Inputs.Keys.Where(x=>!new[]{"Latitude","Longitude","Category","Height","Kd","Reviewed"}.Contains(x)).ToArray()){
   buildingKeys.Add(key);var check=Inputs[key] as CheckBox;var combo=Inputs[key] as ComboBox;
   if(check!=null)check.CheckedChanged+=(s,e)=>{if(!Restoring)UpdatePressureResults();};else if(combo!=null)combo.SelectedIndexChanged+=(s,e)=>{if(!Restoring)UpdatePressureResults();};else Inputs[key].TextChanged+=(s,e)=>{if(!Restoring)UpdatePressureResults();};
  }
  AddBuildingModel(panel);
  var page=new TabPage("Wind-load calculations");foreach(string column in new[]{"Wind from / axis","Surface","Load case","Exposure","z (m)","Kz","Kzt","q (kPa)","qh (kPa)","qi (kPa)","G","Cp","GCpi","p (kPa)"})pressureGrid.Columns.Add(column,column);
  pressureGrid.Columns[0].MinimumWidth=150;pressureGrid.Columns[1].MinimumWidth=150;pressureGrid.Columns[2].MinimumWidth=260;
  page.Controls.Add(pressureGrid);page.Controls.Add(pressureStatus);AddResultPage(page);pressureStatus.Text="Enter building geometry and enable building wind pressures. p = q G Cp − qi (GCpi).";
 }
 WindBuildingInput Building(){var b=new WindBuildingInput{Width=Number("BuildingWidth"),Length=Number("BuildingLength"),Eave=Number("Eave"),Apex=Number("Apex"),Kd=Number("Kd"),System=Value("BuildingSystem"),Enclosure=Value("Enclosure"),RidgeDirection=((ComboBox)Inputs["RidgeDirection"]).SelectedIndex,AutoG=((CheckBox)Inputs["AutoG"]).Checked,UserG=Number("UserG"),Damping=Number("Damping"),FrequencyNormal=Number("FrequencyN"),FrequencyParallel=Number("FrequencyP"),OpeningHeight=Number("OpeningHeight")};b.Validate();return b;}
 bool BuildingEnabled{get{return Inputs.ContainsKey("BuildingEnabled")&&((CheckBox)Inputs["BuildingEnabled"]).Checked;}}
 List<WindPressureRow> PressureRows(WindBuildingInput b){if(terrain==null)throw new Exception("Load site terrain before calculating building pressures.");if(Math.Abs(terrain.Lat-Number("Latitude"))>1e-8||Math.Abs(terrain.Lon-Number("Longitude"))>1e-8)throw new Exception("Terrain snapshot does not match the project coordinates.");double v=WindTin.At(Number("Latitude"),Number("Longitude"),Value("Category")).V;return WindBuildingLoads.Calculate(b,v,WindAssessment.Calculate(terrain,b.Height,v,b.Kd));}
 void UpdatePressureResults(){pressureGrid.Rows.Clear();if(!BuildingEnabled){pressureStatus.Text="Building wind pressures are off. Enter dimensions and enable them in Building inputs.";return;}try{var b=Building();var rows=PressureRows(b);foreach(var r in rows){int n=pressureGrid.Rows.Add(r.Direction,r.Surface,r.Case,r.Exposure,F(r.Z),F(r.Kz),F(r.Kzt),F(r.Q),F(r.Qh),F(r.Qi),F(r.G),F(r.Cp),F(r.GCpi),F(r.P));pressureGrid.Rows[n].Cells[3].Style.ForeColor=WindColors.Roughness(r.Exposure);}
   int axes=rows.Select(r=>r.Direction).Distinct().Count();pressureStatus.Text="p = q G Cp − qi (GCpi) | "+axes+" of 4 principal axes resolved | h="+F(b.Height)+"m; roof slope="+F(b.Angle)+"° | Positive toward surface; negative away. Gross pressures; apply MWFRS minimums / load patterns separately.";
  }catch(Exception ex){pressureStatus.Text=ex.Message;}}
 protected override bool OptionalSavedInput(string key){return buildingKeys.Contains(key);}
}
}
