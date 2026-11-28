using System;using System.Linq;using System.Collections.Generic;using System.Drawing;using System.Windows.Forms;using System.Threading.Tasks;
namespace BeamScheduleFormatter {
partial class WindDesignForm {
 readonly HashSet<string> buildingKeys=new HashSet<string>();
 readonly DataGridView pressureGrid=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.AllCells};
 readonly Label pressureStatus=new Label{Dock=DockStyle.Top,Height=54,Padding=new Padding(12)};
 void AddBuildingInputs(){
  var panel=Section("Building inputs","Directional MWFRS pressures for enclosed / partially enclosed gable, hip or flat-roof rectangular buildings. Enter the building once, its ridge azimuth, and how the STAAD +X runs; with building pressures on, Load terrain assesses the exposure and Kzt along the building's faces and diagonals.");
  /* one value per item (owner, 25 November 2026: "mas maayos na building inputs"; "doble doble ang dimension, doble doble ang input sa staad,
     yung bearing magulo"): the dimensions are entered once, the orientation is one ridge azimuth, and the STAAD +X is one choice relative to
     the ridge. Older sheets (compass alignment, compass / bearing +X, STAAD X / Z dimensions) are converted when they open. */buildingPanel=panel;
  Toggle(panel,"BuildingEnabled","Include building wind pressures",false);
  Head(panel,"1  Building");
  NumberField(panel,"BuildingWidth","Width across the ridge (m)","");NumberField(panel,"BuildingLength","Length along the ridge (m)","");NumberField(panel,"Eave","Eave height (m)","");NumberField(panel,"Apex","Apex height (m) - equal to the eave for a flat roof","");
  Choice(panel,"BuildingSystem","Structural system",new[]{"MRF Steel","MRF Concrete","Other system"});Choice(panel,"Enclosure","Enclosure classification",new[]{"Enclosed","Partially enclosed"});
  Head(panel,"2  Orientation on site");
  NumberField(panel,"RidgeAzimuth","Ridge azimuth (°) - turn the building on the site map with its round handle, or type it (0 = N–S, 90 = E–W)","");
  /* the map sets the azimuth (owner, 28 November 2026: "azimuth based sa map") */siteMap.RidgeChanged+=a=>{var t=a.ToString("0.#",System.Globalization.CultureInfo.InvariantCulture);if(Value("RidgeAzimuth")!=t)Inputs["RidgeAzimuth"].Text=t;};
  /* owner (24 November 2026, "yung ridge alignment dapat mag suggest") */panel.Controls.Add(ridgeHint);ridgeUse.LinkClicked+=(s,e)=>{if(suggestedRidge>=0)SetRidge(suggestedRidge);};panel.Controls.Add(ridgeUse);
  Head(panel,"3  STAAD model");
  Choice(panel,"StaadPlusX","STAAD +X axis runs (shown as +X / +Z arrows on the site map)",new[]{"Not used","Along the ridge, toward the ridge azimuth","Along the ridge, the opposite way (azimuth + 180°)","Across the ridge (azimuth + 90°)","Across the ridge (azimuth − 90°)"});panel.Controls.Add(staadAxisHint);
  /* the outline and the axes on the map follow the inputs as they are typed */foreach(var key in new[]{"BuildingEnabled","BuildingWidth","BuildingLength","RidgeAzimuth","StaadPlusX"}){var c=Inputs[key];EventHandler upd=(s1,e1)=>{if(!Restoring)try{ShowBuildingOnMap();}catch(Exception){}};c.TextChanged+=upd;var cb=c as CheckBox;if(cb!=null)cb.CheckedChanged+=upd;var co=c as ComboBox;if(co!=null)co.SelectedIndexChanged+=upd;}
  Head(panel,"4  Gust effect and internal pressure");
  Toggle(panel,"AutoG","Calculate gust effect factor G",true);NumberField(panel,"UserG","Gust effect factor G","0.85");NumberField(panel,"Damping","Damping ratio for flexible response","0.02");NumberField(panel,"FrequencyN","Natural frequency normal to the ridge (Hz); 0 = estimate","0");NumberField(panel,"FrequencyP","Natural frequency parallel to the ridge (Hz); 0 = estimate","0");NumberField(panel,"OpeningHeight","Height for the positive internal qi (m); 0 = mean roof height","0");
  /* components and cladding elements (owner: "gawa ka naman ng for C&C") */
  Head(panel,"5  Components and cladding");
  /* plain spacings instead of "name @ area" (owner, 25 November 2026: "yung building inputs diko magets sa components and claddings"): the
     effective wind area of each element is worked out - A = span × max(spacing, span / 3) (NSCP 2015 Section 202 / 207E) */
  NumberField(panel,"CcFrameSpacing","Truss / rafter spacing = purlin span (m)","");NumberField(panel,"CcPurlinSpacing","Purlin spacing (m)","");NumberField(panel,"CcGirtSpan","Column spacing = girt span (m); blank = truss spacing","");NumberField(panel,"CcGirtSpacing","Girt spacing (m)","");
  panel.Controls.Add(ccAreas);NumberField(panel,"CCElements","Other elements (optional): name @ effective wind area m², e.g. Ridge cap @ 0.5","");
  /* inputs of older sheets, never shown: read once and converted (LegacyBuildingInputs) */
  Choice(panel,"RidgeDirection","(older sheets) ridge alignment",new[]{"N–S","NE–SW","E–W","NW–SE","Other azimuth (type it below)"});Choice(panel,"StaadXDir","(older sheets) STAAD +X",new[]{"Not used","Along the ridge, toward the ridge azimuth","Along the ridge, toward the opposite end (azimuth + 180°)","Across the ridge (azimuth + 90°)","Across the ridge (azimuth − 90°)","N","NE","E","SE","S","SW","W","NW","Other bearing (type it below)"});NumberField(panel,"StaadXBearing","(older sheets) +X bearing","");Choice(panel,"StaadRidgeAxis","(older sheets) ridge along STAAD",new[]{"X","Z"});NumberField(panel,"StaadX","(older sheets) STAAD X","");NumberField(panel,"StaadZ","(older sheets) STAAD Z","");
   foreach(var key in new[]{"StaadPlusX","RidgeAzimuth"}){var c=Inputs[key];var combo=c as ComboBox;if(combo!=null)combo.SelectedIndexChanged+=(s,e)=>{if(!Restoring)ApplyStaadOrientation();};else c.TextChanged+=(s,e)=>{if(!Restoring)ApplyStaadOrientation();};}
   foreach(var key in new[]{"BuildingEnabled","AutoG","Enclosure"}){var c=Inputs[key];var combo=c as ComboBox;var chk=c as CheckBox;if(combo!=null)combo.SelectedIndexChanged+=(s,e)=>ShowBuildingInputs();else if(chk!=null)chk.CheckedChanged+=(s,e)=>ShowBuildingInputs();}
   ((CheckBox)Inputs["BuildingEnabled"]).CheckedChanged+=(s,e)=>{if(!Restoring&&On("BuildingEnabled")&&string.IsNullOrWhiteSpace(Value("RidgeAzimuth")))Inputs["RidgeAzimuth"].Text="0";};
   EnableWhen(()=>On("BuildingEnabled"),"BuildingWidth","BuildingLength","Eave","Apex","BuildingSystem","Enclosure","RidgeAzimuth","StaadPlusX","AutoG","CCElements","CcFrameSpacing","CcPurlinSpacing","CcGirtSpan","CcGirtSpacing");EnableWhen(()=>On("BuildingEnabled")&&!On("AutoG"),"UserG");EnableWhen(()=>On("BuildingEnabled")&&On("AutoG"),"Damping","FrequencyN","FrequencyP");EnableWhen(()=>On("BuildingEnabled")&&Value("Enclosure")=="Partially enclosed","OpeningHeight");
  foreach(string key in Inputs.Keys.Where(x=>!new[]{"Latitude","Longitude","Category","Height","Kd","Reviewed"}.Contains(x)).ToArray()){
   buildingKeys.Add(key);var check=Inputs[key] as CheckBox;var combo=Inputs[key] as ComboBox;
   if(check!=null)check.CheckedChanged+=(s,e)=>{if(!Restoring)UpdatePressureResults();};else if(combo!=null)combo.SelectedIndexChanged+=(s,e)=>{if(!Restoring)UpdatePressureResults();};else Inputs[key].TextChanged+=(s,e)=>{if(!Restoring)UpdatePressureResults();};
  }
  AddBuildingModel(panel);ShowBuildingInputs();
  var page=new TabPage("Main Wind Force Resisting System (MWFRS)");foreach(string column in new[]{"Wind from / axis","Surface","Load case","Exposure","z (m)","Kz","Kzt","q (kPa)","qh (kPa)","qi (kPa)","G","Cp","GCpi","p (kPa)"})pressureGrid.Columns.Add(column,column);
  pressureGrid.Columns[0].MinimumWidth=150;pressureGrid.Columns[1].MinimumWidth=150;pressureGrid.Columns[2].MinimumWidth=260;
  page.Controls.Add(pressureGrid);page.Controls.Add(pressureStatus);AddResultPage(page);pressureStatus.Text="Enter building geometry and enable building wind pressures. p = q G Cp − qi (GCpi).";
 }
 readonly Label ccAreas=new Label{Width=480,Height=64,ForeColor=PowTheme.Teal,Tag="FullWidth"};
 const string OldCcDefault="Roof sheet / fastener @ 0.93, Purlin @ 9.3, Wall sheet / fastener @ 0.93, Girt @ 9.3";
 double CcNumber(string key){double v;return double.TryParse(Value(key),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out v)&&WindTin.Finite(v)&&v>0?v:double.NaN;}
 /* the C&C elements from the spacings: roof / wall sheet (span = purlin / girt spacing, width span/3), purlin and girt (span × max(spacing,
    span/3)); plus any other elements typed; with nothing entered, typical areas so the C&C and the Purlin link still work */
 internal string CcElementsText(){var ci=System.Globalization.CultureInfo.InvariantCulture;Func<double,string> f=v=>v.ToString("0.##",ci);Func<double,double,double> area=(span,w)=>span*Math.Max(w,span/3);var list=new List<string>();
  double frame=CcNumber("CcFrameSpacing"),purlin=CcNumber("CcPurlinSpacing"),girtSpan=CcNumber("CcGirtSpan"),girt=CcNumber("CcGirtSpacing");if(double.IsNaN(girtSpan))girtSpan=frame;
  if(!double.IsNaN(purlin))list.Add("Roof sheet (span "+f(purlin)+" m) @ "+area(purlin,0).ToString("0.###",ci));
  if(!double.IsNaN(frame)&&!double.IsNaN(purlin))list.Add("Purlin ("+f(frame)+" m span x "+f(purlin)+" m) @ "+area(frame,purlin).ToString("0.###",ci));
  if(!double.IsNaN(girt))list.Add("Wall sheet (span "+f(girt)+" m) @ "+area(girt,0).ToString("0.###",ci));
  if(!double.IsNaN(girtSpan)&&!double.IsNaN(girt))list.Add("Girt ("+f(girtSpan)+" m span x "+f(girt)+" m) @ "+area(girtSpan,girt).ToString("0.###",ci));
  string other=Value("CCElements").Trim();if(other!="")list.Add(other);return list.Count==0?OldCcDefault:string.Join(", ",list);}
 string CcAreasText(){if(!Inputs.ContainsKey("CcFrameSpacing"))return "";var t=CcElementsText();if(t==OldCcDefault&&Value("CCElements").Trim()=="")return "Enter the spacings to get each element's effective wind area. Until then typical areas are used: roof / wall sheet 0.93 m², purlin / girt 9.3 m².";
  try{return "Effective wind areas A = span × max(spacing, span/3): "+string.Join("; ",WindComponentsCladding.ParseElements(t).Select(e=>e.Name+" "+e.Area.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+" m²"));}catch(Exception ex){return ex.Message;}}
 readonly List<Label> buildingHeads=new List<Label>();FlowLayoutPanel buildingPanel;
 void Head(FlowLayoutPanel panel,string text){var l=new Label{Text=text.ToUpperInvariant(),Width=480,Height=34,Font=ModernUi.Strong,ForeColor=PowTheme.Teal,Padding=new Padding(0,12,0,2),Tag="FullWidth"};buildingHeads.Add(l);panel.Controls.Add(l);}
 /* only the inputs that apply are shown */
 void ShowBuildingInputs(){if(!Inputs.ContainsKey("CCElements"))return;bool on=On("BuildingEnabled");string[] legacy={"RidgeDirection","StaadXDir","StaadXBearing","StaadRidgeAxis","StaadX","StaadZ"};
  var show=new Dictionary<string,bool>{{"UserG",!On("AutoG")},{"Damping",On("AutoG")},{"FrequencyN",On("AutoG")},{"FrequencyP",On("AutoG")},{"OpeningHeight",Value("Enclosure")=="Partially enclosed"}};
  FlowLayoutPanel flow=null;foreach(var key in buildingKeys.Concat(new[]{"CCElements"}).Distinct()){Control c;if(key=="BuildingEnabled"||!Inputs.TryGetValue(key,out c)||c.Parent==null||c.Parent.Parent!=buildingPanel)continue;bool v;if(!show.TryGetValue(key,out v))v=true;if(legacy.Contains(key))v=false;c.Parent.Visible=on&&v;flow=c.Parent.Parent as FlowLayoutPanel??flow;}
  foreach(var h in buildingHeads)h.Visible=on;ccAreas.Visible=on;ccAreas.Text=CcAreasText();ridgeHint.Visible=on;ridgeUse.Visible=on&&ridgeUse.Enabled;staadAxisHint.Visible=on;if(flow!=null)DesignSheetForm.Arrange(flow);}
 readonly Label staadAxisHint=new Label{Width=420,Height=64,ForeColor=PowTheme.Muted,Text="Choose how the model's +X runs: along the ridge (toward the ridge azimuth or the other way) or across it. +X is the direction the X coordinates grow in STAAD (the X arrow of the axis triad); Y is up and +Z is 90° clockwise from +X on plan."};
 /* bearing of the STAAD +X axis in degrees, or -1 when not used: relative to the ridge azimuth */
 internal double StaadBearing(){string v=Value("StaadPlusX");if(!(v.StartsWith("Along")||v.StartsWith("Across")))return -1;double az=RidgeAzimuthInput();if(double.IsNaN(az))return -1;return WindBuildingLoads.Norm(az+(v.Contains("opposite")?180:v.Contains("+ 90")?90:v.Contains("− 90")?270:0));}
 /* the ridge azimuth (0-180); a blank box of an older sheet falls back to its compass alignment */
 double RidgeAzimuthInput(){double a;string t=Value("RidgeAzimuth");if(string.IsNullOrWhiteSpace(t)){int i=((ComboBox)Inputs["RidgeDirection"]).SelectedIndex;return i>=0&&i<4?i*45.0:double.NaN;}return double.TryParse(t,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out a)&&WindTin.Finite(a)?(a%180+180)%180:double.NaN;}
 void SetRidge(double az){Inputs["RidgeAzimuth"].Text=((az%180+180)%180).ToString("0.###",System.Globalization.CultureInfo.InvariantCulture);}
 /* older sheets: compass alignment, a compass / typed +X bearing with the ridge axis and STAAD X / Z dimensions -> one ridge azimuth, the
    +X choice relative to the ridge, and the width / length */
 void LegacyBuildingInputs(){var ci=System.Globalization.CultureInfo.InvariantCulture;bool keep=Restoring;Restoring=true;try{
   var rd=(ComboBox)Inputs["RidgeDirection"];if(string.IsNullOrWhiteSpace(Value("RidgeAzimuth"))&&rd.SelectedIndex>=0&&rd.SelectedIndex<4&&rd.SelectedIndex>0)Inputs["RidgeAzimuth"].Text=(rd.SelectedIndex*45).ToString(ci);
   string old=Value("StaadXDir");if(old!="Not used"&&Value("StaadPlusX")=="Not used"){var plus=(ComboBox)Inputs["StaadPlusX"];
    if(old.StartsWith("Along")||old.StartsWith("Across")){plus.SelectedIndex=old.Contains("opposite")?2:old.Contains("+ 90")?3:old.Contains("− 90")?4:1;}
    else{string[] d={"N","NE","E","SE","S","SW","W","NW"};double xb=-1,b;int k=Array.IndexOf(d,old);if(k>=0)xb=k*45;else if(double.TryParse(Value("StaadXBearing"),System.Globalization.NumberStyles.Float,ci,out b)&&WindTin.Finite(b))xb=WindBuildingLoads.Norm(b);
     if(xb>=0){bool ridgeX=Value("StaadRidgeAxis")!="Z";double az=((ridgeX?xb:xb+90)%180+180)%180;Inputs["RidgeAzimuth"].Text=az.ToString("0.###",ci);double dd=WindBuildingLoads.Norm(xb-az);plus.SelectedIndex=dd<1e-6||dd>360-1e-6?1:Math.Abs(dd-180)<1e-6?2:Math.Abs(dd-90)<1e-6?3:4;
      double x,z;if(double.TryParse(Value("StaadX"),System.Globalization.NumberStyles.Float,ci,out x)&&double.TryParse(Value("StaadZ"),System.Globalization.NumberStyles.Float,ci,out z)&&x>0&&z>0){Inputs["BuildingLength"].Text=(ridgeX?x:z).ToString("0.###",ci);Inputs["BuildingWidth"].Text=(ridgeX?z:x).ToString("0.###",ci);}}}}
   if(Value("CCElements").Trim()==OldCcDefault)Inputs["CCElements"].Text="";((ComboBox)Inputs["StaadXDir"]).SelectedIndex=0;}finally{Restoring=keep;}ApplyStaadOrientation();ShowBuildingInputs();}
 /* the first assessed terrain bearing: the building's faces and diagonals when the building is on (ridge azimuth mod 45), else north */
 bool realigning;
 /* the check waits until the sheet has finished opening (owner, 27 November 2026: "pero yung kzt di pa din naka align sa faces ng building"):
    a saved sheet with terrain sampled at N, NE, E ... (older versions) was opened while Restoring and the realignment was skipped */
 async void RealignTerrain(){if(realigning||closed)return;realigning=true;try{await Task.Delay(400);for(int i=0;i<50&&Restoring&&!closed;i++)await Task.Delay(100);
   if(!closed&&!Restoring&&terrain!=null&&Math.Abs(terrain.Offset-TerrainOffset())>1e-6){status.Text="Exposure and Kzt are being assessed again for the building faces (from "+WindBuildingLoads.BearingName(TerrainOffset())+" in 45° steps)…";await LoadTerrain();}}
  catch(Exception){}finally{realigning=false;}}
 internal double TerrainOffset(){if(!BuildingEnabled)return 0;double a=RidgeAzimuthInput();if(double.IsNaN(a))return 0;double o=(a%45+45)%45;return o<1e-6||o>45-1e-6?0:o;}
 internal string RidgeLabel(){double a=RidgeAzimuthInput();return double.IsNaN(a)?"(no azimuth)":WindBuildingLoads.RidgeName(a);}
 void ApplyStaadOrientation(){double xb=StaadBearing();if(xb<0){staadAxisHint.Text=Value("StaadPlusX")=="Not used"?"Choose how the model's +X runs to name the STAAD wind types by the model axes (WW+X, LW-Z ...). +X is the way the X coordinates grow (the X arrow of STAAD's axis triad); Y is up, +Z is 90° clockwise from +X on plan.":"Enter the ridge azimuth first.";return;}
  bool ridgeX=Value("StaadPlusX").StartsWith("Along");staadAxisHint.Text="+X → "+WindBuildingLoads.BearingName(xb)+", +Z → "+WindBuildingLoads.BearingName(xb+90)+" (Y up). The ridge runs along STAAD "+(ridgeX?"X":"Z")+"; the building length is along "+(ridgeX?"X":"Z")+" and the width along "+(ridgeX?"Z":"X")+".";}
 readonly Label ridgeHint=new Label{Width=420,Height=46,ForeColor=PowTheme.Teal,Text="Suggested ridge alignment: load the site terrain and enter the building to get a suggestion."};
 readonly LinkLabel ridgeUse=new LinkLabel{Width=420,Height=22,Text="Use the suggested ridge alignment",Enabled=false};double suggestedRidge=double.NaN;
 /* the alignment whose governing |p| is the largest (the critical orientation when the real one is not fixed) */
 /* every azimuth 0-175° in 5° steps is tried; the largest governing |p| is suggested, with the four compass alignments listed */
 void SuggestRidge(){suggestedRidge=double.NaN;ridgeUse.Enabled=false;string[] names={"N–S","NE–SW","E–W","NW–SE"};
  try{if(!BuildingEnabled||terrain==null){ridgeHint.Text="Suggested ridge alignment: load the site terrain and enable building wind pressures to get a suggestion.";return;}
   var b=Building();PressureRows(b);double v=WindTin.At(Number("Latitude"),Number("Longitude"),Value("Category")).V;var dirs=WindAssessment.Calculate(terrain,b.Height,v,b.Kd);
   var az=Enumerable.Range(0,36).Select(k=>k*5.0).ToArray();var all=WindBuildingLoads.AzimuthPeaks(b,v,dirs,az);int best=WindBuildingLoads.SuggestedRidge(all);suggestedRidge=az[best];
   double now=RidgeAzimuthInput();ridgeUse.Enabled=double.IsNaN(now)||Math.Abs(now-suggestedRidge)>1e-6;
   ridgeHint.Text="Suggested: "+WindBuildingLoads.RidgeName(az[best])+" (largest design pressure |p| = "+F(all[best])+" kPa, every 5° checked). "+string.Join(" · ",Enumerable.Range(0,4).Select(k=>names[k]+" "+F(all[k*9])))+" kPa. Use the actual ridge orientation when it is known.";}
  catch(Exception ex){ridgeHint.Text="Suggested ridge alignment: "+ex.Message;}}
 WindBuildingInput Building(){var b=new WindBuildingInput{Width=Number("BuildingWidth"),Length=Number("BuildingLength"),Eave=Number("Eave"),Apex=Number("Apex"),Kd=Number("Kd"),System=Value("BuildingSystem"),Enclosure=Value("Enclosure"),RidgeDirection=0,Azimuth=RequireAzimuth(),AutoG=((CheckBox)Inputs["AutoG"]).Checked,UserG=Number("UserG"),Damping=Number("Damping"),FrequencyNormal=Number("FrequencyN"),FrequencyParallel=Number("FrequencyP"),OpeningHeight=Number("OpeningHeight")};b.Validate();return b;}
 double RidgeAzimuthOrThrow(){double a=RidgeAzimuthInput();if(double.IsNaN(a))throw new Exception("Enter the ridge azimuth in degrees (0–180), or choose a compass alignment.");return a;}
 double RequireAzimuth(){return RidgeAzimuthOrThrow();}
 bool BuildingEnabled{get{return Inputs.ContainsKey("BuildingEnabled")&&((CheckBox)Inputs["BuildingEnabled"]).Checked;}}
 List<WindPressureRow> PressureRows(WindBuildingInput b){if(terrain==null)throw new Exception("Load site terrain before calculating building pressures.");if(Math.Abs(terrain.Lat-Number("Latitude"))>1e-8||Math.Abs(terrain.Lon-Number("Longitude"))>1e-8)throw new Exception("Terrain snapshot does not match the project coordinates.");double v=WindTin.At(Number("Latitude"),Number("Longitude"),Value("Category")).V;return WindBuildingLoads.Calculate(b,v,WindAssessment.Calculate(terrain,b.Height,v,b.Kd));}
 /* the building outline on the site map follows the width, length and ridge alignment */
 void ShowBuildingOnMap(){double w=0,l=0;bool ok=BuildingEnabled&&double.TryParse(Value("BuildingWidth"),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out w)&&double.TryParse(Value("BuildingLength"),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out l)&&w>0&&l>0;
  if(!ok){siteMap.SetBuilding(0,0,double.NaN);siteMap.SetStaadX(double.NaN);return;}siteMap.SetBuilding(w,l,RidgeAzimuthInput());double xb=StaadBearing();siteMap.SetStaadX(xb>=0?xb:double.NaN);}
 DataGridView ccGrid;Label ccStatus;
 void AddCcPage(){var page=new TabPage("Components and Cladding (C&C)");ccGrid=ReadOnlyGrid("Element","A (m²)","Zone","GCp +","GCp −","p + (kPa)","p − (kPa)");ccStatus=new Label{Dock=DockStyle.Top,Height=64,Padding=new Padding(8),ForeColor=PowTheme.Muted};page.Controls.Add(ccGrid);page.Controls.Add(ccStatus);AddResultPage(page);}
 void UpdateCc(List<WindPressureRow> rows,WindBuildingInput b){if(ccGrid==null)return;ccGrid.Rows.Clear();if(rows==null){ccStatus.Text="Enable building wind pressures and load the site terrain to get the C&C pressures.";return;}
  try{double v=WindTin.At(Number("Latitude"),Number("Longitude"),Value("Category")).V;var s=WindComponentsCladding.Calculate(b,rows,CcElementsText(),WindAssessment.Calculate(terrain,b.Height,v,b.Kd),v);foreach(var r in s.Rows)ccGrid.Rows.Add(r.Element,F(r.Area),r.Zone,F(r.GCpPos),F(r.GCpNeg),"+"+F(r.PPos),F(r.PNeg));WindLink.Save(s,Value("Latitude")+" N, "+Value("Longitude")+" E");/* the Purlin Design sheet reads this (owner: "automatic link ng loads sa purlin design") */
   ccStatus.ForeColor=PowTheme.Teal;ccStatus.Text="NSCP 2015 Section 207E Part 1: p = qh [(GCp) − (GCpi)], qh = "+F(s.Qh)+" kPa (wind from "+s.Direction+", exposure "+s.Exposure+"), a = "+F(s.A)+" m, roof "+s.RoofRange+", GCpi ±"+F(s.GCpi)+". Zones: roof 1 interior / 2 edges / 3 corners, walls 4 interior / 5 corners. Minimum ±0.77 kPa. "+s.Note;}
  catch(Exception ex){ccStatus.ForeColor=Color.Firebrick;ccStatus.Text="C&C: "+ex.Message;}}
 void UpdatePressureResults(){RefreshStaadWind();ShowBuildingOnMap();SuggestRidge();ShowBuildingInputs();pressureGrid.Rows.Clear();if(buildingModel!=null){buildingModel.Pressures=null;buildingModel.Invalidate();}if(!BuildingEnabled){pressureStatus.Text="Building wind pressures are off. Enter dimensions and enable them in Building inputs.";UpdateCc(null,null);return;}try{var b=Building();var rows=PressureRows(b);UpdateCc(rows,b);if(buildingModel!=null){buildingModel.Pressures=rows;buildingModel.Invalidate();}foreach(var r in rows){int n=pressureGrid.Rows.Add(r.Direction,r.Surface,r.Case,r.Exposure,F(r.Z),F(r.Kz),F(r.Kzt),F(r.Q),F(r.Qh),F(r.Qi),F(r.G),F(r.Cp),F(r.GCpi),F(r.P));pressureGrid.Rows[n].Cells[3].Style.ForeColor=WindColors.Roughness(r.Exposure);}
   int axes=rows.Select(r=>r.Direction).Distinct().Count();pressureStatus.Text="p = q G Cp − qi (GCpi) | "+axes+" of 4 principal axes resolved | h="+F(b.Height)+"m; roof slope="+F(b.Angle)+"° | Positive toward surface; negative away. Gross pressures; apply MWFRS minimums / load patterns separately.";
   /* the terrain was assessed for another orientation: say so (exposure and Kzt of each face need the terrain loaded for the faces) */
   /* the terrain follows the building by itself (owner, 27 November 2026: "yung totoo naka align na ba ito sa building? bakit naka N, NE?";
      "pati sa kzt"): when the ridge azimuth is not the one the terrain was sampled from, exposure and Kzt are assessed again for the faces */
   if(terrain!=null&&Math.Abs(terrain.Offset-TerrainOffset())>1e-6){pressureStatus.Text+=" Re-assessing exposure and Kzt for the building faces (from "+WindBuildingLoads.BearingName(TerrainOffset())+" in 45° steps)…";RealignTerrain();}
  }catch(Exception ex){pressureStatus.Text=ex.Message;UpdateCc(null,null);}}
 protected override bool OptionalSavedInput(string key){return buildingKeys.Contains(key);}
 /* shared with the Earthquake Load sheet: occupancy category (Wind groups III to V) and the structural system */
 protected override void SharedParameters(){
  Share("occupancy","Category",v=>v=="I"||v=="II"?v:v=="III-V"?"III-V":null,(now,c)=>c=="I"||c=="II"?c:"III-V");
  var sys=(ComboBox)Inputs["BuildingSystem"];Share("system","BuildingSystem",v=>SystemCommon(sys.Items.Cast<object>().Select(Convert.ToString).ToList().IndexOf(v)),(now,c)=>{int i=SystemIndex(c);return i<0?null:Convert.ToString(sys.Items[i]);});}
}
}
