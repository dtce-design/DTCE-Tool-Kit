using System;using System.Collections.Generic;using System.Drawing;using System.IO;using System.Linq;using System.Text;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* Load Combinations (NSCP 2015) design sheet (owner, 25 November 2026: "create load combinations for superstructure including orthogonal
   effect for staad input"; "no included in Earthquake Sheet - Load Combinations NSCP 2015"): paste the primary load cases as in STAAD,
   set f1, rho, Ca, I and Ev, and copy the LOAD COMBINATION commands numbered 1000 ... 5000 by group (seismic drift: envelope of the EQ cases). */
partial class LoadCombinationsForm:DesignSheetForm {
 readonly TextBox output=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Both,WordWrap=false,Font=new Font("Consolas",9.5f),BackColor=Color.White};
 readonly DataGridView cases=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill};
 readonly DataGridView combos=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill};
 readonly Label status=new Label{Dock=DockStyle.Top,Height=52,Padding=new Padding(10),ForeColor=PowTheme.Teal};
 public LoadCombinationsForm():base("Load Combinations (NSCP 2015) - STAAD input with orthogonal seismic effects","Load-Combinations"){
  var loads=Section("Primary loads","Paste the primary load cases of the STAAD model, one per line, as in the STAAD input (LOAD 4 LOADTYPE SEISMIC-H TITLE EQ+X) or the load case list (4 EQ+X Seismic-H). The load type gives D (Dead), L (Live), Lr (Roof Live), W (Wind) and E (Seismic-H; Seismic-V = Ev); without a type the title is read (DL, SDL, LL, LLR, WL+X, EQ+X ...). The combinations use the same titles.");
  var text=new TextBox{MaxLength=int.MaxValue,Multiline=true,ScrollBars=ScrollBars.Vertical,Width=560,Height=300,Font=new Font("Consolas",9.5f),Text=StaadCombinations.Example.Replace("\n","\r\n"),AcceptsReturn=true};/* the whole page is the box for the pasted lines (no two-column input layout) */var tab=loads.Parent.Parent as TabPage;var box=new Panel{Dock=DockStyle.Fill,BackColor=Color.White,Padding=new Padding(14)};text.Dock=DockStyle.Fill;text.Width=0;
  var cap=new Label{Text="Primary load cases - automatic from the structure class (Settings tab). To paste your own, uncheck Automatic there and paste them here as in the STAAD input (LOAD 5 LOADTYPE SEISMIC-H TITLE EQ+X).",Dock=DockStyle.Top,Height=52,Font=ModernUi.Strong,ForeColor=PowTheme.Ink};
  /* the STAAD input pasted before the run (owner, 26 November 2026: "hindi magbabase sa redundancy factor. kasi bago ko irun loaded na dapat ito"):
     the wind loads on recessed faces and the floor plans of the irregularity checks need only the model, not the results */
  /* two inputs (owner, 26 November 2026: "2 inputs na lang joint from table and beams from table. or staad command file or .std"): the STAAD
     Nodes table and Beams table as copied from STAAD, or the whole .std / command file in the first box (key StaadInput, as older sheets saved it) */
  Func<TextBox> box0=()=>new TextBox{MaxLength=int.MaxValue,Multiline=true,ScrollBars=ScrollBars.Both,WordWrap=false,Dock=DockStyle.Fill,Font=new Font("Consolas",9.5f),AcceptsReturn=true,BorderStyle=BorderStyle.FixedSingle};
  var nodesBox=box0();var beamsBox=box0();Inputs.Add("StaadInput",nodesBox);Inputs.Add("StaadBeams",beamsBox);
  var modelCap=new Label{Text="STAAD model for the wind loads (before the run): the wind cases also load the recessed walls from it (WIND LOAD with XR / ZR ranges). ρ can stay 1.0 until after the run.",Dock=DockStyle.Top,Height=44,Font=ModernUi.Strong,ForeColor=PowTheme.Ink};
  var openStd=new Button{Text="Open .std file…",AutoSize=true,Dock=DockStyle.Top};openStd.Click+=(s1,e1)=>{using(var d=new OpenFileDialog{Title="STAAD input file",Filter="STAAD input (*.std;*.txt)|*.std;*.txt|All files (*.*)|*.*"}){if(d.ShowDialog(this)!=DialogResult.OK)return;
    try{nodesBox.Text=File.ReadAllText(d.FileName).Replace("\r\n","\n").Replace("\n","\r\n");beamsBox.Text="";}catch(Exception ex){MessageBox.Show(this,ex.Message,"Open .std file",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}};
  var openAnl=new Button{Text="Open .ANL file (after the run: joint displacements and member end forces)…",AutoSize=true,Dock=DockStyle.Top};openAnl.Click+=(s1,e1)=>OpenAnl();
  var model=new SplitContainer{Dock=DockStyle.Fill,Orientation=Orientation.Horizontal,SplitterWidth=7,BackColor=Color.FromArgb(176,186,182)};model.Panel1.BackColor=Color.White;model.Panel2.BackColor=Color.White;model.Panel1.Padding=new Padding(0,0,0,6);model.Panel2.Padding=new Padding(0,6,0,0);
  model.Panel1.Controls.Add(nodesBox);model.Panel1.Controls.Add(new Label{Text="1  Nodes - STAAD Nodes table (Node, X, Y, Z), or the whole .std / command file",Dock=DockStyle.Top,Height=24,ForeColor=PowTheme.Teal,Font=new Font("Segoe UI Semibold",9f),UseMnemonic=false});
  model.Panel2.Controls.Add(beamsBox);model.Panel2.Controls.Add(new Label{Text="2  Beams - STAAD Beams table (Beam, Node A, Node B); not needed with a .std file",Dock=DockStyle.Top,Height=24,ForeColor=PowTheme.Teal,Font=new Font("Segoe UI Semibold",9f),UseMnemonic=false});
  var pair=new SplitContainer{Dock=DockStyle.Fill,Orientation=Orientation.Vertical,SplitterWidth=7,BackColor=Color.FromArgb(176,186,182)};pair.Panel1.BackColor=Color.White;pair.Panel2.BackColor=Color.White;pair.Panel1.Padding=new Padding(0,0,8,0);pair.Panel2.Padding=new Padding(8,0,0,0);
  text.BorderStyle=BorderStyle.FixedSingle;pair.Panel1.Controls.Add(text);pair.Panel1.Controls.Add(cap);pair.Panel2.Controls.Add(model);pair.Panel2.Controls.Add(openAnl);pair.Panel2.Controls.Add(openStd);pair.Panel2.Controls.Add(modelCap);box.Controls.Add(pair);
  if(tab!=null){tab.Controls.Clear();tab.Controls.Add(box);}else loads.Controls.Add(box);Inputs.Add("Primary",text);
  Shown+=(s0,e0)=>{try{pair.SplitterDistance=Math.Max(200,pair.Width/2);model.SplitterDistance=Math.Max(80,model.Height/2);}catch(Exception){}};
  /* one Settings tab in four groups (owner, 26 November 2026: "masyadong confusing ito at paulit ulit yung ibang inputs. sana masmaayos na
     layout. saka baka pwede isang tab na lang"): short captions, the explanations as tool tips, linked values hidden instead of repeated */
  var set=Section("Settings","Structure class and seismic factors, the primary load cases, the NSCP 2015 combination sets (Section 203) and what the STAAD output contains. Values linked from the other Design Loads sheets are hidden while linked.");
  var tips=new ToolTip{AutoPopDelay=20000};Action<string,string> tip=(key,help)=>tips.SetToolTip(Inputs[key],help);
  Action<string> group=t=>set.Controls.Add(new Label{Text=t.ToUpperInvariant(),Height=30,TextAlign=ContentAlignment.BottomLeft,Font=new Font("Segoe UI Semibold",9f),ForeColor=PowTheme.Teal,UseMnemonic=false});
  group("1  Structure and seismic factors");
  /* the seismic cases as in the owner's STAAD notes: A regular / symmetrical, B regular / unsymmetrical (accidental torsion), C irregular /
     unsymmetrical (orthogonal effects as REPEAT LOAD cases) */
  Choice(set,"Structure","Seismic load cases",new[]{"C - Irregular and unsymmetrical: orthogonal 1.0(EQ+X)+0.3(EQ+Z) ...","B - Regular and unsymmetrical: EQ +X +E, EQ +X -E ... (accidental torsion)","A - Regular and symmetrical: EQ +X, -X, +Z, -Z"});
  NumberField(set,"Rho","Redundancy factor ρ","1.0");tip("Rho","Reliability / redundancy factor ρ (Section 208.5.1.1); computed on the Redundancy factor ρ tab from the pasted STAAD data.");
  Toggle(set,"EqLinked","Ca, I, Ω0 from the Earthquake sheet",true);tip("EqLinked","Seismic coefficient Ca, importance factor I and overstrength Ω0 taken from the Earthquake Load sheet (calculate it first).");
  NumberField(set,"Ca","Seismic coefficient Ca","0.44");NumberField(set,"I","Importance factor I","1.0");NumberField(set,"Omega0","Overstrength Ω0 (Table 208-11)","2.8");
  Toggle(set,"Ev","Ev = 0.5 Ca I D in strength design",true);tip("Ev","Vertical seismic Ev = 0.5 Ca I D added to the strength design combinations (when there is no Seismic-V case); E = ρEh + Ev (208.5.1.1).");
  Toggle(set,"EvAsd","Ev also in the ASD sets",false);tip("EvAsd","Section 208-18: Ev may be taken as zero for allowable stress design.");
  NumberField(set,"Acc","Accidental eccentricity ACC (B)","0.05");tip("Acc","Accidental torsion for structure B: ACC = 0.05 x Ax; Ax comes from the Torsional amplification Ax tab after the STAAD run.");
  NumberField(set,"Ax","Torsional amplification Ax","1.0");
  group("2  Primary load cases");
  var auto=Toggle(set,"AutoPrimary","Automatic primary load cases",true);tip("AutoPrimary","Built from the structure class, earthquake cases first. Uncheck to paste your own list on the Primary loads tab.");
  Choice(set,"EqCommand","Seismic command in the EQ cases",new[]{"UBC LOAD","IBC LOAD","None"});
  Toggle(set,"LlLinked","Live cases from the Live Loads sheet",true);tip("LlLinked","The checked spaces of the Live Loads sheet set the live cases: LL1 (f1 0.5), LL2 (f1 1.0).");
  Choice(set,"LiveCases","Live load cases",new[]{"1","2","3"});((ComboBox)Inputs["LiveCases"]).SelectedIndex=1;
  /* f1 by live case (owner: "hiwalay yung LL1 at LL2 yung f1 niya") */NumberField(set,"F1One","Live cases with f1 = 1.0","LL2");tip("F1One","Titles of the live cases with f1 = 1.0 (assembly, live load over 4.8 kPa, garages); the others take 0.5.");
  Toggle(set,"RoofLive","Roof live load case LLR",true);Toggle(set,"WindCases","Wind load cases WL±X, WL±Z",true);
  /* NSCP 2015 Figure 207B.4-8 (directional procedure, Part 1 - the Wind sheet's method; owner, 27 November 2026: "do what the code says";
     "depende naman ata if anong procedure ang gagamitin regarding sa torsional cases") */
  Toggle(set,"WindAllCases","Design wind load cases 1-4 (Fig. 207B.4-8)",true);tip("WindAllCases","Directional procedure, Part 1 (as the Wind Load sheet): Case 1 full pressures per direction; Case 2 75 % with torsion e = ±0.15 B; Case 3 75 % on both axes at once; Case 4 56.3 % on both axes with torsion. Cases 2-4 take the windward and leeward walls; the torsion is a couple of joint loads per level.");
  Toggle(set,"WindTorsionExempt","Leave out the wind torsion (Cases 2 and 4)",false);tip("WindTorsionExempt","Checked: the wind cases are Case 1 and Case 3 only, no torsional Cases 2 and 4. Unchecked: the torsion is left out only when the code exempts the building (one storey, h up to 9.1 m, found from the STAAD model).");
  /* owner: "automatic na ganito WIND LOAD X 1 TYPE 1 YR 0 12.6"; "tapos meron pa kasi sa gitna" */Toggle(set,"WindLinked","WIND LOAD lines from the Wind sheet",true);tip("WindLinked","Each wind case gets the WIND LOAD lines of the Wind Load sheet's TYPEs, plus WIND LOAD lines limited by XR / ZR ranges to the recessed walls found from the STAAD model on the Primary loads tab (before the run).");
  Toggle(set,"PerformChange","PERFORM ANALYSIS / CHANGE per EQ case",false);tip("PerformChange","Off by default: RCDC cannot read the analysis results of a file with several PERFORM ANALYSIS / CHANGE. Turn on only for tension / compression members or REPEAT LOAD of the EQ cases (structure C). STAAD's note about it is information only.");
  /* group 3 (Combination sets (NSCP 2015 Section 203)): every option fixed by the code, no heading */
  Toggle(set,"Lrfd","Strength design 203-1 to 203-7 (1000)",true);Toggle(set,"AltAsd","Alternate basic ASD 203-13 to 203-18 (2000)",true);tip("AltAsd","One-third stress increase for W / E.");
  Toggle(set,"Asd","Basic ASD 203-8 to 203-12 (4000)",false);tip("Asd","No stress increase; use instead of the alternate set.");
  Toggle(set,"Special","Special seismic 203-19, 203-20 (3000)",true);tip("Special","Em = Ω0 Eh.");
  Toggle(set,"Drift","Drift checks (5000 wind, seismic by envelope of the EQ cases)",true);NumberField(set,"DriftWind","Wind factor k in D + 0.5L + kW","0.6");tip("DriftWind","Wind drift check D + 0.5L + k W; 0.6 = service-level wind.");
  Toggle(set,"DriftGravity","Seismic drift with D + 0.5L (P-Delta)",false);tip("DriftGravity","Seismic drift D + 0.5L + 1.0 Eh when STAAD runs P-Delta; otherwise 1.0 Eh.");
  /* group 4 (STAAD output): every option fixed by the code, no heading */
  Toggle(set,"PrintReports","Analysis and print commands",true);tip("PrintReports","PERFORM ANALYSIS and the print commands: CG, DIAPHRAGM CR, STORY STIFFNESS, STORY DRIFT, displacements, member forces, reactions.");
  auto.CheckedChanged+=(s0,e0)=>{text.ReadOnly=auto.Checked;};text.ReadOnly=true;
  /* automatic (owner, 27 November 2026: "automatic na dapat itong mga ito, dapat macheck na agad if need or hindi as per nscp 2015 code") */ShowWhen(()=>false,"EvAsd","Acc","EqCommand","AltAsd","Asd","Special","DriftWind","DriftGravity","WindAllCases","WindLinked","PerformChange","EqLinked","LlLinked","AutoPrimary","RoofLive","WindCases","Ev","Lrfd","Drift","PrintReports");EnableWhen(()=>On("AutoPrimary"),"RoofLive","WindCases","WindLinked","LlLinked","EqCommand");EnableWhen(()=>On("Ev"),"EvAsd");EnableWhen(()=>On("Drift"),"DriftWind","DriftGravity");EnableWhen(()=>Value("Structure").StartsWith("B"),"Acc");
  /* linked or unused values are hidden, not repeated */
  ShowWhen(()=>!On("AutoPrimary"),"LiveCases","F1One");ShowWhen(()=>!On("EqLinked"),"Ca","I","Omega0");EnableWhen(()=>!On("EqLinked"),"Ca","I","Omega0");ShowWhen(()=>false,"Ax");ShowWhen(()=>Value("Structure").StartsWith("B"),"Acc");
  /* rho from the STAAD model (owner: "How do you compute redundancy or reliability factor" - "Yes with automatic staad computation") */AddRhoPage();/* owner: "add also automatic computation of torsional amplification factor" */AddAxPage();/* owner: "for structural irregularities check" */AddIrregularPage();/* owner: "bat walang tab for roof live load?" */AddRoofLivePage();
  var page=new TabPage("STAAD load combinations");var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=46,Padding=new Padding(6)};var copy=new Button{Text="Copy STAAD input",AutoSize=true};copy.Click+=(s,e)=>{if(output.Text.Length>0)Clipboard.SetText(output.Text);};top.Controls.Add(copy);
  var split=new SplitContainer{Dock=DockStyle.Fill,Orientation=Orientation.Vertical,SplitterDistance=520};var left=new SplitContainer{Dock=DockStyle.Fill,Orientation=Orientation.Horizontal,SplitterDistance=220};/* visible dividers between the three panes (owner, 26 November 2026: "masakit tignan itong tab, diko makita yung separator ng windows"):
     a 7 px bar in the line colour with a grip, white panes */foreach(var sc in new[]{split,left}){sc.SplitterWidth=7;sc.BackColor=Color.FromArgb(176,186,182);sc.Panel1.BackColor=Color.White;sc.Panel2.BackColor=Color.White;var bar=sc;bar.Paint+=(s0,e0)=>{var r=bar.SplitterRectangle;using(var dot=new SolidBrush(Color.FromArgb(110,122,118)))for(int k=-2;k<=2;k++){if(bar.Orientation==Orientation.Vertical)e0.Graphics.FillEllipse(dot,r.X+r.Width/2-2,r.Y+r.Height/2+k*9-2,4,4);else e0.Graphics.FillEllipse(dot,r.X+r.Width/2+k*9-2,r.Y+r.Height/2-2,4,4);}};}
  foreach(var g in new[]{cases,combos}){g.BorderStyle=BorderStyle.FixedSingle;}output.BorderStyle=BorderStyle.FixedSingle;
  left.Panel1.Controls.Add(cases);left.Panel2.Controls.Add(combos);split.Panel1.Controls.Add(left);split.Panel2.Controls.Add(output);page.Controls.Add(split);page.Controls.Add(top);page.Controls.Add(status);AddResultPage(page);
  foreach(var h in new[]{"No.","Title","Used as"})cases.Columns.Add(h,h);foreach(var h in new[]{"No.","Group / Eq.","Combination"})combos.Columns.Add(h,h);cases.Columns[0].FillWeight=20;combos.Columns[0].FillWeight=20;combos.Columns[1].FillWeight=40;
  GridAppearance.Apply(cases);GridAppearance.Apply(combos);
  foreach(var c in Inputs.Values){c.TextChanged+=(s,e)=>{if(!Restoring)Refresh0();};var cb=c as CheckBox;if(cb!=null)cb.CheckedChanged+=(s,e)=>{if(!Restoring)Refresh0();};var co=c as ComboBox;if(co!=null)co.SelectedIndexChanged+=(s,e)=>{if(!Restoring)Refresh0();};}
  VisibleChanged+=(s,e)=>{if(Visible&&!Restoring&&IsHandleCreated)Refresh0();};
  /* Settings in a window of its own, opened by a button (owner, 28 November 2026: "yung mga settings, hindi na siguro tab, button na lang tapos
     window, mas maayos pa ata") */
  settingsPage=TakePage("Settings");AddCommand("Settings",ShowSettingsWindow);
  /* the structure class follows the structural irregularity check by itself once it has a result (owner: "automatic na sana") */
  HandleCreated+=(s,e)=>{foreach(var kv in Inputs){if(kv.Key=="Structure")continue;kv.Value.TextChanged+=(s1,e1)=>{if(Restoring)return;irrTimer.Stop();irrTimer.Start();};}};
  EnableWhen(()=>irrSuggested=="","Structure");EnableWhen(()=>double.IsNaN(rhoFound),"Rho");
  /* the STAAD text gets the wider half */Shown+=(s,e)=>{try{split.SplitterDistance=Math.Max(320,split.Width*42/100);left.SplitterDistance=Math.Max(160,left.Height*40/100);}catch(Exception){}Refresh0();};}
 /* required by the code or by the STAAD / RCDC run, so not a choice any more (owner, 27 November 2026: "alisin na yung options na required
    talaga gaya nung add perform analysis and change every eq load, tapos yung iba pa"): the hidden settings read as fixed whatever an older
    sheet saved - one analysis (RCDC), Ca / I / Omega0 and the live cases linked, automatic primary loads with LLR and the wind cases, Ev in
    strength design, the strength, drift and print sets */
 static readonly Dictionary<string,bool> Fixed=new Dictionary<string,bool>{{"PerformChange",false},{"EqLinked",true},{"LlLinked",true},{"AutoPrimary",true},{"RoofLive",true},{"WindCases",true},{"Ev",true},{"Lrfd",true},{"Drift",true},{"PrintReports",true},/* owner (28 November 2026, "automatic na sana"): */{"EvAsd",false},{"AltAsd",true},{"Asd",false},{"Special",true},{"DriftGravity",false}};
 new bool On(string key){bool v;return Fixed.TryGetValue(key,out v)?v:base.On(key);}
 TabPage settingsPage;Form settingsWindow;
 internal void ShowSettingsWindow(){if(settingsPage==null)return;if(settingsWindow==null||settingsWindow.IsDisposed){var w=new Form{Text="Settings - Load Combinations (NSCP 2015)",Width=860,Height=760,StartPosition=FormStartPosition.CenterParent,ShowInTaskbar=false,MinimizeBox=false,BackColor=settingsPage.BackColor};AppBrand.Apply(w);
   var host=new Panel{Dock=DockStyle.Fill,Padding=settingsPage.Padding,AutoScroll=settingsPage.AutoScroll};foreach(var c in settingsPage.Controls.Cast<Control>().ToList()){settingsPage.Controls.Remove(c);host.Controls.Add(c);ViewOnly.AddPart(this,c);}
   var close=new Button{Text="Close",AutoSize=true,Height=36,Dock=DockStyle.Right};close.Click+=(s,e)=>w.Hide();var bar=new Panel{Dock=DockStyle.Bottom,Height=48,Padding=new Padding(8)};bar.Controls.Add(close);
   w.Controls.Add(host);w.Controls.Add(bar);w.FormClosing+=(s,e)=>{if(e.CloseReason==CloseReason.UserClosing){e.Cancel=true;w.Hide();}};Disposed+=(s,e)=>{if(!w.IsDisposed)w.Dispose();};settingsWindow=w;}
  if(!settingsWindow.Visible)settingsWindow.Show(TopLevelControl as Form??FindForm());settingsWindow.Activate();}
 internal StaadCombinationSettings Settings(){double acc,axv;if(!double.TryParse("0.05"/* ACC = 0.05 (208.5.7.6), times Ax */,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out acc))acc=0.05;if(!double.TryParse(Value("Ax"),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out axv))axv=1.0;axv=Math.Max(1.0,Math.Min(3.0,axv));var extra=new StaadCombinationSettings{PrintReports=On("PrintReports"),AutoPrimary=On("AutoPrimary"),PerformChange=On("PerformChange"),EqCommand="UBC LOAD",Acc=acc,Ax=axv};var one=new HashSet<string>(Value("F1One").Split(new[]{',',';',' '},StringSplitOptions.RemoveEmptyEntries).Select(t=>t.Trim()),StringComparer.OrdinalIgnoreCase);var res=new StaadCombinationSettings{F1One=one,Structure=Value("Structure").Substring(0,1),Rho=Number("Rho"),Ca=On("Ev")?Number("Ca"):0,I=On("Ev")?Number("I"):1,IncludeEv=On("Ev"),Lrfd=On("Lrfd"),Asd=On("Asd"),EvInAsd=On("Ev")&&On("EvAsd"),Drift=On("Drift"),DriftGravity=On("DriftGravity"),DriftWind=0.6,AltAsd=On("AltAsd"),Special=On("Special"),Omega0=Number("Omega0"),AutoPrimary=extra.AutoPrimary,PrintReports=extra.PrintReports,PerformChange=extra.PerformChange,EqCommand=extra.EqCommand,Acc=extra.Acc,Ax=extra.Ax};res.WindText=WindTexts();res.RefLoads=StaadCombinations.ReferenceLoads(Value("StaadInput"));res.RoofLiveText=On("RoofLive")?RoofLiveText():"";return res;}
 /* LLR on the model of the Primary loads tab (owner, 27 November 2026: "add roof live load based doon sa staad file, iload at mamili ng appropriate load value") */
 string roofLiveKey,roofLiveCache="",roofLiveNote="";internal List<RoofLiveLoads.Region> RoofLiveRegions=new List<RoofLiveLoads.Region>();
 string RoofLiveText(){string err;StaadModel m=null;try{m=InputModel(out err);if(m==null){string mt,ft;RhoTexts(out mt,out ft);if(RedundancyFactor.HasModel(mt))m=RedundancyFactor.ReadModel(mt);}}catch(Exception){m=null;}
  string key=m==null?"":m.Joints.Count+"|"+m.Members.Count+"|"+string.Join(",",m.Joints.Values.Take(50).Select(j=>j.X+";"+j.Y+";"+j.Z))+"|"+string.Join(",",m.Members.Values.Take(50).Select(x=>x.A+"-"+x.B));
  if(key==roofLiveKey)return roofLiveCache;roofLiveKey=key;
  if(m==null){roofLiveCache="";roofLiveNote="paste the STAAD model on the Primary loads tab to load the roof";RoofLiveRegions=new List<RoofLiveLoads.Region>();return "";}
  try{List<RoofLiveLoads.Region> regs;roofLiveCache=RoofLiveLoads.Text(m,out roofLiveNote,out regs);RoofLiveRegions=regs;}catch(Exception ex){roofLiveCache="";roofLiveNote="roof live load: "+ex.Message;}
  return roofLiveCache;}
 static string Used(string kind){return kind=="D"?"Dead D":kind=="L"?"Live L":kind=="Lr"?"Roof live Lr":kind=="W"?"Wind W":kind=="E"?"Seismic Eh":kind=="Ev"?"Vertical seismic Ev":"not used (other type)";}
 /* Ca, I, Omega0 from the Earthquake Load sheet when linked */
 string eqLinkNote="";
 bool ApplyEqLink(){eqLinkNote="";if(!On("EqLinked"))return false;var e=EarthquakeLink.Load();if(e==null){eqLinkNote="Earthquake Load sheet not calculated yet: Ca, I, Ω0 as typed.";return false;}
  eqLinkNote="From the Earthquake Load sheet ("+e.Saved.ToString("dd MMM HH:mm",System.Globalization.CultureInfo.InvariantCulture)+", zone "+e.Zone+"): Ca = "+StaadCombinations.F(e.Ca)+", I = "+StaadCombinations.F(e.I)+", Ω0 = "+StaadCombinations.F(e.Omega)+".";
  bool changed=false;foreach(var kv in new[]{new KeyValuePair<string,double>("Ca",e.Ca),new KeyValuePair<string,double>("I",e.I),new KeyValuePair<string,double>("Omega0",e.Omega)}){string v=kv.Value.ToString("0.####",System.Globalization.CultureInfo.InvariantCulture);if(Value(kv.Key)!=v){Inputs[kv.Key].Text=v;changed=true;}}return changed;}
 /* the other links (owner: "link mo lahat ng kailangan inputs sa isat isa"): live cases / f1 from Live Loads, storey heights and weights
    from the Earthquake Load sheet into the irregularity check */
 string linkNote="",levelsAuto=null;
 static Dictionary<string,string> LiveLink(){var d=DesignLoadsLink.Get("live-loads");return d!=null&&d.ContainsKey("cases")&&d["cases"]!=""?d:null;}
 internal static string WeightsFromLevels(string levels){var lines=new List<string>();foreach(var part in (levels??"").Split(new[]{',',';','\n'},StringSplitOptions.RemoveEmptyEntries)){var t=part.Trim().Split('@');if(t.Length==2&&t[0].Trim()!=""&&t[1].Trim()!="")lines.Add(t[0].Trim()+" "+t[1].Trim());}return string.Join("\r\n",lines);}
 /* always two live cases (owner, 28 November 2026: "automatic na 2 ang liveload, tapos piliin mo na agad doon if ano ang f1=1.0"):
    LL1 f1 = 0.5 and LL2 f1 = 1.0, whatever spaces the Live Loads sheet has checked */
 internal static void LiveCasesFor(string cases,out int count,out string f1One){count=2;f1One="LL2";}
 bool ApplyOtherLinks(){linkNote="";bool changed=false;var live=On("LlLinked")&&On("AutoPrimary")?LiveLink():null;
  if(On("AutoPrimary")){int n;string one;LiveCasesFor(live!=null?live["cases"]:"",out n,out one);var combo=(ComboBox)Inputs["LiveCases"];if(Value("LiveCases")!=n.ToString()){combo.SelectedItem=n.ToString();changed=true;}if(Value("F1One")!=one){Inputs["F1One"].Text=one;changed=true;}
   linkNote="Live cases: LL1 (f1 = 0.5) and LL2 (f1 = 1.0)"+(live!=null?"; checked spaces of the Live Loads sheet: "+live["cases"].Replace(",",", "):"")+".";}
  var lv=DesignLoadsLink.Get("earthquake-levels");if(lv!=null&&lv.ContainsKey("levels")){string w=WeightsFromLevels(lv["levels"]);string cur=Value("RegWeights");if(w!=""&&cur!=w&&(cur==""||cur==levelsAuto)){Inputs["RegWeights"].Text=w;changed=true;}if(w!="")levelsAuto=w;}
  return changed;}
 /* wind case text, cached by the model and the Wind sheet's types */
 string windNote="",windKey=null;Dictionary<string,string> windCache;
 Dictionary<string,string> WindTexts(){windNote="";var link=DesignLoadsLink.Get("wind-staad");if(link==null){windNote="Wind cases: calculate the Wind Load sheet with building pressures and the STAAD +X set.";return new Dictionary<string,string>();}
  string mt=Value("StaadInput")+"\u0001"+Value("StaadBeams")+"\u0001"+AllWindCases()+TorsionExempt(),ft;string key=string.Join(";",link.OrderBy(k=>k.Key).Select(k=>k.Key+"="+k.Value))+"#"+mt.Length+"#"+mt.GetHashCode();
  if(key!=windKey){string n,err;var m=InputModel(out err);if(m==null){RhoTexts(out mt,out ft);if(RedundancyFactor.HasModel(mt))m=RedundancyFactor.ReadModel(mt);}windTypes=link.Where(k=>k.Key.Length==2).ToDictionary(k=>k.Key,k=>k.Value);windCache=WindEdgeLoads.CaseTexts(windTypes,m,out n,AllWindCases(),!TorsionExempt());windKey=key;windCacheNote=(err!=""?err+" ":"")+n;}TorsionExempt();windNote="Wind cases (NSCP 2015 Fig. 207B.4-8, Cases 1-4): "+torsionWhy+". "+windCacheNote.Replace("Paste the STAAD input on the Redundancy factor tab","Paste the STAAD input on the Primary loads tab");return windCache;}
 string windCacheNote="";
 /* NSCP 2015 Figure 207B.4-8 (directional procedure): the design wind load cases are required for every building */
 bool AllWindCases(){return true;}
 /* exemption from the torsional cases (Cases 2 and 4), decided from the STAAD model: ASCE 7-10 Appendix D, D1.1 (NSCP 2015 is based on
    ASCE 7-10) - a one-storey building with h <= 9.1 m (30 ft). The other D1.1 groups (two storeys or less of light-frame construction or
    with flexible diaphragms) cannot be told from nodes and beams, so the torsional cases stay for them (the safe side). */
 string torsionKey,torsionWhy="",torsionAutoWhy="";bool torsionExempt;
 /* the owner's choice (owner, 27 November 2026: "ibalik option na hindi isama yung torsion sa wind") or the automatic code exemption */
 bool TorsionExempt(){bool auto=AutoTorsionExempt();if(On("WindTorsionExempt")){torsionWhy="torsional Cases 2 and 4 left out (Settings: leave out the wind torsion)";return true;}torsionWhy=torsionAutoWhy;return auto;}
 bool AutoTorsionExempt(){string nodes=Value("StaadInput"),beams=Value("StaadBeams");string key=nodes.Length+"#"+nodes.GetHashCode()+"#"+beams.GetHashCode();if(key==torsionKey)return torsionExempt;torsionKey=key;torsionExempt=false;
  string err;var m=InputModel(out err);if(m==null){torsionAutoWhy="torsional cases included (no STAAD model to check the exemption)";return false;}
  string n;var plans=WindEdgeLoads.Plans(m,0,1e9,out n);double baseY=m.Joints.Values.Min(j=>m.H(j))*m.ToMetre,top=m.Joints.Values.Max(j=>m.H(j))*m.ToMetre;int storeys=plans.Count(p=>p.Y>baseY+0.5);double h=top-baseY;
  torsionExempt=storeys<=1&&h<=9.144+1e-6;
  torsionAutoWhy=torsionExempt?"exempt from the torsional cases: one storey, h = "+StaadCombinations.F(h)+" m <= 9.1 m (ASCE 7-10 Appendix D, D1.1): Cases 1 and 3 only":
   "torsional cases included: "+storeys+" storeys, h = "+StaadCombinations.F(h)+" m (the D1.1 exemption needs one storey with h <= 9.1 m)";return torsionExempt;}
 /* the Wind sheet sends roof pressures: Case 1 gets its -GCpi twin WL1B */
 bool RoofWind(){var l=DesignLoadsLink.Get("wind-staad");if(l==null)return false;/* the roof needs the STAAD model (owner, 27 November 2026: "bat walang laman ito" - WL1B cases were empty without it) */string err;if(InputModel(out err)==null){string mt,ft;RhoTexts(out mt,out ft);if(!RedundancyFactor.HasModel(mt))return false;}
  return l.Where(k=>k.Key.Length==2).Any(k=>{var t=WindFaceTypes.Unpack(k.Key,k.Value);return t!=null&&t.HasRoof;});}
 Dictionary<string,string> windTypes;
 /* the STAAD model, the LOAD titles and the case settings for the Wind Load sheet's "Wind load cases" tab and report (link wind-model) */
 string windModelKey="";
 void UpdateWindFigures(Dictionary<string,string> titles){var d=new Dictionary<string,string>{{"nodes",Value("StaadInput")},{"beams",Value("StaadBeams")},{"all",AllWindCases()?"1":"0"},{"exempt",TorsionExempt()?"1":"0"},{"titles",string.Join("\n",titles.Select(k=>k.Key+"="+k.Value))}};
  if(string.IsNullOrWhiteSpace(d["nodes"])){string mt,ft;RhoTexts(out mt,out ft);if(RedundancyFactor.HasModel(mt))d["nodes"]=mt;}
  string key=string.Join("\u0001",d.Values);if(key==windModelKey)return;windModelKey=key;try{DesignLoadsLink.Set("wind-model",d);}catch(Exception){}}
 /* the model of the Primary loads tab: the .std / command file in the Nodes box, or the Nodes and Beams tables; null when not given */
 internal StaadModel InputModel(out string error){error="";string nodes=Value("StaadInput"),beams=Value("StaadBeams");try{if(RedundancyFactor.HasModel(nodes))return RedundancyFactor.ReadModel(nodes);
   if(string.IsNullOrWhiteSpace(nodes)&&string.IsNullOrWhiteSpace(beams))return null;if(string.IsNullOrWhiteSpace(beams)){error="Paste the STAAD Beams table in box 2 too.";return null;}if(string.IsNullOrWhiteSpace(nodes)){error="Paste the STAAD Nodes table in box 1 too.";return null;}
   return RedundancyFactor.ReadTables(nodes,beams);}catch(Exception ex){error="STAAD model: "+ex.Message;return null;}}
 void Refresh0(){if(!Restoring&&(ApplyEqLink()|ApplyOtherLinks()))return;cases.Rows.Clear();combos.Rows.Clear();
  if(On("AutoPrimary")&&!Restoring){string auto=StaadCombinations.AutoPrimary(Value("Structure").Substring(0,1),int.Parse(Value("LiveCases")),On("RoofLive"),On("WindCases"),AllWindCases(),!TorsionExempt(),RoofWind()).Replace("\n","\r\n");if(Inputs["Primary"].Text!=auto){Inputs["Primary"].Text=auto;return;}}
  try{var list=StaadCombinations.Parse(Value("Primary"));var s=Settings();if(s.Rho<1)throw new Exception("ρ must be at least 1.0.");
   foreach(var p in list)cases.Rows.Add(p.No,p.Title,Used(p.Kind)+(p.Kind=="L"?", f1 = "+StaadCombinations.F(s.F1(p)):"")+(p.Kind=="E"?(p.Axis==""?" (no X / Z in the title: ± used)":" "+p.Axis+(p.Sign>0?" +":p.Sign<0?" −":" ±")):""));
   /* the wind cases for the Wind Load sheet's STAAD commands (owner: "yung loading niya sa building na copy paste ko na lang sa command") */
   {var wc=new Dictionary<string,string>();foreach(var p in list.Where(x=>x.Kind=="W")){string key=StaadCombinations.WindKey(p.Title);if(key==""||wc.ContainsKey(key))continue;string body;s.WindText.TryGetValue(key,out body);wc[key]="LOAD "+p.No+" LOADTYPE WIND TITLE "+p.Title+(string.IsNullOrEmpty(body)?"":"\n"+body);}if(wc.Count>0)DesignLoadsLink.Set("wind-cases",wc);
    UpdateRoofLivePage();
    var titles=new Dictionary<string,string>();foreach(var p in list.Where(x=>x.Kind=="W")){string key=StaadCombinations.WindKey(p.Title);if(key!=""&&!titles.ContainsKey(key))titles[key]="LOAD "+p.No+" "+p.Title;}UpdateWindFigures(titles);}
   var all=StaadCombinations.Build(list,s);foreach(var r in StaadCombinations.RepeatCases(all))combos.Rows.Add(r.No,"Orthogonal seismic load case (REPEAT LOAD)",r.Title);foreach(var c in all)combos.Rows.Add(c.No,c.Group+(c.Eq!=""?"  ·  "+c.Eq:""),c.Title);output.Text=StaadCombinations.Staad(all,list,s).Replace("\n","\r\n");
   /* rho and the structure class for the Earthquake Load sheet */DesignLoadsLink.Set("load-combinations",new Dictionary<string,string>{{"rho",double.IsNaN(rhoFound)?"":Value("Rho")},{"structure",Value("Structure").Substring(0,1)}});
   status.ForeColor=PowTheme.Teal;status.Text=(eqLinkNote!=""?eqLinkNote+" ":"")+(linkNote!=""?linkNote+" ":"")+(windNote!=""?windNote+" ":"")+list.Count+" primary load cases, "+all.Count+" combinations: "+string.Join(", ",all.GroupBy(c=>c.Group).Select(g=>g.Key+" "+g.First().No+"–"+g.Last().No))+".";}
  catch(Exception ex){status.ForeColor=Color.Firebrick;status.Text=ex.Message;output.Text="";}}
 protected override string BuildReport(){var list=StaadCombinations.Parse(Value("Primary"));var s=Settings();var all=StaadCombinations.Build(list,s);var b=new StringBuilder();
  b.AppendLine("LOAD COMBINATIONS - NSCP 2015");b.AppendLine();b.AppendLine("## 01  PRIMARY LOAD CASES");foreach(var p in list)b.AppendLine(p.No+" "+p.Title+": "+Used(p.Kind));
  b.AppendLine();b.AppendLine("## 02  FACTORS");b.AppendLine("f1 = 1.0 for "+(s.F1One.Count==0?"no live case":string.Join(", ",s.F1One))+", 0.5 for the others; rho = "+StaadCombinations.F(s.Rho)+(s.IncludeEv?"; Ev = 0.5 Ca I D, Ca = "+StaadCombinations.F(s.Ca)+", I = "+StaadCombinations.F(s.I):"; Ev not included"));
  b.AppendLine("Orthogonal effects: 100 % of one horizontal seismic direction + 30 % of the perpendicular direction, every sign.");
  b.AppendLine();b.AppendLine("## 03  COMBINATIONS");foreach(var c in all)b.AppendLine(c.No+" "+c.Title);b.AppendLine();b.AppendLine("## 04  STAAD INPUT");b.AppendLine(StaadCombinations.Staad(all,list,s));return b.ToString();}
 protected override CalculationDocument BuildDocument(){var list=StaadCombinations.Parse(Value("Primary"));var s=Settings();var all=StaadCombinations.Build(list,s);var rep=new RichReport("Load Combinations - NSCP 2015");
  rep.Heading("1  BASIS").Paragraph("NSCP 2015 Section 203.3.1 (strength design), Section 203.4 (allowable stress design) and Section 203.5 (special seismic). Seismic load E = ρEh + Ev (Section 208.5.1.1) with Ev = 0.5 Ca I D"+(list.Any(p=>p.Kind=="Ev")?" taken by the Seismic-V load case":s.IncludeEv?" added to the dead load factor":" not included")+". Seismic load cases by the structure class: A regular and symmetrical (each EQ direction alone), B regular and unsymmetrical (each EQ case with its accidental torsion), C irregular and unsymmetrical (orthogonal effects: eight load cases of 100 % of one direction plus 30 % of the other, made with REPEAT LOAD). Wind cases are combined one at a time. Numbering: 1000 strength design (Eq. 203-1 to 203-7), 2000 alternate basic ASD (Section 203.4.2, Eq. 203-13 to 203-18; a one-third increase in allowable stresses is permitted for all combinations including W or E), 3000 special seismic (Section 203.5, Eq. 203-19 1.2D + f1L + 1.0Em and 203-20 0.9D ± 1.0Em, Em = Ω0 Eh, Eq. 208-19), 4000 basic ASD (Section 203.4.1, Eq. 203-8 to 203-12, no stress increase; used instead of the alternate set when chosen), 5000 wind drift (D + 0.5L + k W), seismic drift without load combinations: the primary EQ cases themselves (1.0 Eh) are enveloped (DEFINE ENVELOPE, type serviceability) and their story drift printed; ΔS of Section 208.6.4, ΔM = 0.7 R ΔS against 0.025h for T < 0.7 s, 0.020h otherwise. E = ρEh + Ev (Eq. 208-18); Ev = 0.5 Ca I D for strength design and taken as zero for allowable stress design. The seismic cases follow the structure class: regular and symmetrical, regular and unsymmetrical (accidental torsion) or irregular (orthogonal effects).");
  rep.KeyValues(new[]{new[]{"Ω0 (special seismic)",StaadCombinations.F(s.Omega0)},new[]{"f1","1.0 for "+(s.F1One.Count==0?"no live case":string.Join(", ",s.F1One))+"; 0.5 for the other live cases"},new[]{"Structure",Value("Structure")},new[]{"ρ",StaadCombinations.F(s.Rho)},new[]{"Ca, I",s.IncludeEv?StaadCombinations.F(s.Ca)+", "+StaadCombinations.F(s.I):"(Ev not included)"},new[]{"0.5 Ca I",s.IncludeEv?StaadCombinations.F(0.5*s.Ca*s.I):"-"},
   new[]{"Primary load cases",s.AutoPrimary?"automatic from the structure class, earthquake cases first":"pasted"},new[]{"Seismic load command",s.EqCommand+(s.PerformChange?", PERFORM ANALYSIS / CHANGE after each EQ case":"")},
   new[]{"Torsion (case B)",s.Structure=="B"?"natural (centre of mass offset, rigid diaphragms) + accidental ACC = 0.05 × Ax = "+StaadCombinations.F(s.Acc*s.Ax)+" (Ax = "+StaadCombinations.F(s.Ax)+")":"-"},
   new[]{"Seismic drift","envelope of the primary EQ cases (1.0 Eh), no combinations"},new[]{"Roof live load (LLR)",On("RoofLive")?"NSCP 2015 205.4 / Table 205-3 on the STAAD model: "+(roofLiveNote==""?"-":roofLiveNote):"not used"},new[]{"Wind load cases","NSCP 2015 Fig. 207B.4-8 Cases 1-4 (directional procedure); "+(TorsionExempt()?"":"")+torsionWhy},new[]{"Ca, I, Ω0",On("EqLinked")?(eqLinkNote==""?"from the Earthquake Load sheet":eqLinkNote):"typed"}});
  bool hasRho=Value("RhoForces").Trim()!=""||Value("RhoModel").Trim()!=""||Value("RhoWalls").Trim()!="";int sec=2;
  rep.Heading(sec+++"  PRIMARY LOAD CASES").Table(new[]{"No.","Title (STAAD)","Used as"},list.Select(p=>new[]{p.No.ToString(),p.Title,Used(p.Kind)+(p.Kind=="L"?", f1 = "+StaadCombinations.F(s.F1(p)):"")}),new float[]{12,40,48});
  var rpt=StaadCombinations.RepeatCases(all);if(rpt.Count>0)rep.Paragraph("Orthogonal seismic load cases (REPEAT LOAD, Load Type Seismic)",false,true).Table(new[]{"No.","Title","Repeat load"},rpt.Select(r=>new[]{r.No.ToString(),r.Title,string.Join("  ",r.Repeat.Select(t=>t.Key.No+" x "+StaadCombinations.F(t.Value)))}),new float[]{12,44,44});
  if(hasRho){RhoReport(rep,sec,s.Rho);sec++;}
  if(Value("AxDisplacements").Trim()!=""){AxReport(rep,sec);sec++;}
  if(IrregularReport(rep,sec))sec++;
  /* the wind load cases (figures, torsion) are reported in the Wind Load sheet (owner, 27 November 2026) */
  /* the roof live load chosen for each roof of the model (Table 205-3) */
  if(On("RoofLive")&&RoofLiveRegions.Count>0){rep.Heading(sec+++"  ROOF LIVE LOAD (LLR) ON THE STAAD MODEL").Paragraph("NSCP 2015 Section 205.4, Table 205-3 (Method 1): Lr acts vertically on the area projected on a horizontal plane and depends on the roof slope (row 1: flat or rise less than 4 in 12; row 2: 4 in 12 to less than 12 in 12; row 3: 12 in 12 and over) and on the tributary area of the member (0 to 20, 21 to 60, over 60 m²). Slab roofs: each panel between beams, its area being the tributary area of the beams around it. Trusses: the tributary area of the truss, horizontal span × spacing; the top chords carry w = Lr × spacing projected on the horizontal. Roof decks used as an occupancy are not roofs here: they take Table 205-1, 18 (as the area served) in a live load case.")
    .Table(new[]{"Roof","Where","Basis","Row","Lr (kPa)"},RoofLiveRegions.Select(r=>new[]{r.Kind=="Truss"?"Trussed roof":"Slab roof",r.Where,r.Basis,r.Row.ToString(),StaadCombinations.F(r.Lr)}),new float[]{14,16,48,7,12});
   foreach(var y in RoofLevels()){var lv=y;rep.Figure(RoofPlanHeight(lv,CalculationDocument.CW),RichReport.Raster((g,r)=>DrawRoofPlan(g,r,lv,9)),"Roof live load at level "+StaadCombinations.F(lv)+" m: panels and truss top chords coloured by the Lr chosen (Table 205-3).");}}
  rep.Heading(sec+"  LOAD COMBINATIONS");foreach(var g in all.GroupBy(c=>c.Group)){rep.Paragraph(g.Key,false,true);rep.Table(new[]{"No.","Eq.","Combination","Factors (load case x factor)"},g.Select(c=>new[]{c.No.ToString(),c.Eq,c.Title,string.Join("  ",c.Terms.OrderBy(t=>t.Key.No).Select(t=>t.Key.No+" x "+StaadCombinations.F(t.Value)))}),new float[]{9,14,42,35});}
  return rep.Build();}
 protected override bool OptionalSavedInput(string key){return key.StartsWith("Rho")&&key!="Rho"||key=="AutoPrimary"||key=="LiveCases"||key=="RoofLive"||key=="WindCases"||key=="EqCommand"||key=="Acc"||key=="Ax"||key=="DriftGravity"||key=="AxDisplacements"||key=="PrintReports"||key.StartsWith("Reg")||key=="EqLinked"||key=="PerformChange"||key=="LlLinked"||key=="StaadBeams"||key=="WindAllCases"||key=="WindTorsionExempt"||key=="WindLinked"||key=="StaadInput";}
 /* sheets saved before the automatic primary loads keep their pasted primary loads */
 bool legacyPrimary;protected override void ReadAdditionalState(System.Xml.XmlElement root){legacyPrimary=root.SelectSingleNode("Input[@key='AutoPrimary']")==null;}
 protected override void AfterRestore(){base.AfterRestore();if(legacyPrimary)((CheckBox)Inputs["AutoPrimary"]).Checked=false;Refresh0();}
}
}
