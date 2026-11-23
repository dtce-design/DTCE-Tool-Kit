using System;using System.Linq;using System.Collections.Generic;using System.Windows.Forms;using BeamScheduleFormatter;
class StrapFootingTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}static void Near(double a,double b,string why,double tol=1e-6){Check(Math.Abs(a-b)<=tol*Math.Max(1,Math.Abs(b)),why+" ("+a+" vs "+b+")");}
 static Dictionary<string,SupportReaction> Map(params SupportReaction[] rows){return rows.ToDictionary(r=>r.Node,r=>r);}
 [STAThread]static void Main(){
  /* classic exterior + interior strap footing: R1 = P1 L / (L - e), S = R1 - P1, R2 = P2 - S, strap moment at the exterior column = R1 e */
  var inp=new StrapInput{GammaConc=0,GammaSoil=0};inp.Nodes.Add(new StrapNode{Node="1",X=0,Z=0,B=1.6,L=2.8,T=.6,Ex=.6});inp.Nodes.Add(new StrapNode{Node="2",X=6,Z=0,B=2.6,L=2.6,T=.6});inp.Straps.Add(new StrapBeam{From="1",To="2"});
  var c=StrapFooting.Solve(inp,"U",Map(new SupportReaction{Node="1",Fy=600},new SupportReaction{Node="2",Fy=900}),true);
  Near(c.R[0],600*6/5.4,"exterior footing reaction P L / (L - e)");Near(c.R[1],900-(600*6/5.4-600),"interior footing relieved by the strap shear");Near(Math.Abs(c.Shear[0]),600*6/5.4-600,"strap shear");
  Near(StrapFooting.Moment(c,0,6,0),-c.R[0]*.6,"hogging strap moment R1 e at the exterior column");Near(StrapFooting.Moment(c,0,6,6),0,"no strap moment at the interior (fulcrum) column",1e-6);
  Near(c.Qmax[0],c.Qmin[0],"uniform pressure under the eccentric footing");Near(c.Qavg[0],c.R[0]/(1.6*2.8),"q = R / A");
  /* the same layout along Z (strap in any direction) */
  var inpZ=new StrapInput{GammaConc=0,GammaSoil=0};inpZ.Nodes.Add(new StrapNode{Node="1",X=0,Z=0,B=2.8,L=1.6,T=.6,Ez=.6});inpZ.Nodes.Add(new StrapNode{Node="2",X=0,Z=6,B=2.6,L=2.6,T=.6});inpZ.Straps.Add(new StrapBeam{From="1",To="2"});
  var cz=StrapFooting.Solve(inpZ,"U",Map(new SupportReaction{Node="1",Fy=600},new SupportReaction{Node="2",Fy=900}),true);Near(cz.R[0],c.R[0],"strap along Z gives the same reaction");Near(cz.Qmax[0],cz.Qmin[0],"uniform pressure, strap along Z");
  /* diagonal strap */
  var inpD=new StrapInput{GammaConc=0,GammaSoil=0};double s2=Math.Sqrt(.5);inpD.Nodes.Add(new StrapNode{Node="1",X=0,Z=0,B=2.2,L=2.2,T=.6,Ex=.6*s2,Ez=.6*s2});inpD.Nodes.Add(new StrapNode{Node="2",X=6*s2,Z=6*s2,B=2.6,L=2.6,T=.6});inpD.Straps.Add(new StrapBeam{From="1",To="2"});
  var cd=StrapFooting.Solve(inpD,"U",Map(new SupportReaction{Node="1",Fy=600},new SupportReaction{Node="2",Fy=900}),true);Near(cd.R[0],600*6/5.4,"diagonal strap: same lever");Near(cd.Qmax[0],cd.Qmin[0],"diagonal strap: uniform pressure",1e-6);
  /* corner footing with two straps: uniform pressure, total equilibrium */
  var corner=new StrapInput{GammaConc=0,GammaSoil=0};corner.Nodes.Add(new StrapNode{Node="1",B=1.8,L=1.8,T=.6,Ex=.7,Ez=.7});corner.Nodes.Add(new StrapNode{Node="2",X=6,B=2.4,L=2.4,T=.6});corner.Nodes.Add(new StrapNode{Node="3",Z=5,B=2.4,L=2.4,T=.6});corner.Straps.Add(new StrapBeam{From="1",To="2"});corner.Straps.Add(new StrapBeam{From="1",To="3"});
  var cc=StrapFooting.Solve(corner,"U",Map(new SupportReaction{Node="1",Fy=620},new SupportReaction{Node="2",Fy=1100},new SupportReaction{Node="3",Fy=1030}),true);
  Near(cc.R.Sum(),620+1100+1030+0,"vertical equilibrium of the strap system");Near(cc.Qmax[0],cc.Qmin[0],"two straps make the corner footing pressure uniform",1e-6);Check(cc.R[0]>620&&cc.R[1]<1100&&cc.R[2]<1030,"corner footing loaded more, the interior footings relieved");
  /* one strap, footing off-centre across the strap too: that part stays with the footing */
  var cross=new StrapInput{GammaConc=0,GammaSoil=0};cross.Nodes.Add(new StrapNode{Node="1",B=1.8,L=1.8,T=.6,Ex=.6,Ez=.3});cross.Nodes.Add(new StrapNode{Node="2",X=6,B=2.6,L=2.6,T=.6});cross.Straps.Add(new StrapBeam{From="1",To="2"});
  var cx=StrapFooting.Solve(cross,"U",Map(new SupportReaction{Node="1",Fy=600},new SupportReaction{Node="2",Fy=900}),true);Near(cx.R[0],600*6/5.4,"lever along the strap unchanged");Near(Math.Abs(cx.Mp[0]),cx.R[0]*.3,"moment across the strap stays with the footing");Check(cx.Qmax[0]>cx.Qmin[0]+1,"pressure not uniform across the strap");
  /* column moment sign: reaction Mz > 0 loads the +X side of a concentric footing (footing takes it) */
  var single=new StrapInput{GammaConc=0,GammaSoil=0};single.Nodes.Add(new StrapNode{Node="1",B=2,L=2,T=.5});var cm=StrapFooting.Solve(single,"U",Map(new SupportReaction{Node="1",Fy=400,Mz=40}),true);Near(cm.Mq[0],40,"soil supplies the column moment");Near(cm.Qmax[0],400/4.0+40/(2*2*2/6.0),"q = P/A + M/S");
  /* Z up: same answers with plan X-Y and Fz */
  var zup=new StrapInput{GammaConc=0,GammaSoil=0,Vertical="Z"};zup.Nodes.Add(new StrapNode{Node="1",B=1.6,L=2.8,T=.6,Ex=.6});zup.Nodes.Add(new StrapNode{Node="2",X=6,B=2.6,L=2.6,T=.6});zup.Straps.Add(new StrapBeam{From="1",To="2"});
  var cu=StrapFooting.Solve(zup,"U",Map(new SupportReaction{Node="1",Fz=600},new SupportReaction{Node="2",Fz=900}),true);Near(cu.R[0],c.R[0],"Z up gives the same statics");
  var zm=new StrapInput{GammaConc=0,GammaSoil=0,Vertical="Z"};zm.Nodes.Add(new StrapNode{Node="1",B=2,L=2,T=.5});var czm=StrapFooting.Solve(zm,"U",Map(new SupportReaction{Node="1",Fz=400,My=40}),true);Near(czm.Qmax[0],cm.Qmax[0],"Z up column moment");
  /* service weights: R = P + footing + soil + strap */
  var w=new StrapInput();w.Nodes.Add(new StrapNode{Node="1",B=2,L=2,T=.5});var cw=StrapFooting.Solve(w,"S",Map(new SupportReaction{Node="1",Fy=400}),false);Near(cw.R[0],400+4*(.5*24+1.0*18),"service bearing includes the footing and the soil above");
  /* load case classification */
  Check(StrapFooting.LooksFactored("101 0.9D+1.0E"),"0.9D + 1.0E factored");
  Check(StrapFooting.LooksFactored("2 1.2DL+1.6LL")&&!StrapFooting.LooksFactored("1 DL+LL"),"1.2/1.6 factored, DL+LL service");
  /* full checks and report */
  var full=new StrapInput();full.Nodes.Add(new StrapNode{Node="1",B=1.6,L=2.8,T=.6,Ex=.6});full.Nodes.Add(new StrapNode{Node="2",X=6,B=2.6,L=2.6,T=.6});full.Straps.Add(new StrapBeam{From="1",To="2",Width=.4,Depth=.8,Top="4-25",Bottom="3-20"});
  var rows=new List<SupportReaction>{new SupportReaction{Node="1",Case="1 DL+LL",Fy=600},new SupportReaction{Node="2",Case="1 DL+LL",Fy=900},new SupportReaction{Node="1",Case="2 1.2DL+1.6LL",Fy=820},new SupportReaction{Node="2",Case="2 1.2DL+1.6LL",Fy=1240}};
  var notes=new List<string>();var cases=StrapFooting.Cases(full,rows,notes);Check(cases.Count==2&&cases.Count(x=>x.Factored)==1,"service and factored cases found");var steps=new Dictionary<SupportCheck,List<string>>();var list=StrapFooting.Check(full,cases,notes,steps);
  foreach(var name in new[]{"Bearing pressure (service)","Full contact under gravity loads (service)","Footing punching shear (two-way)","Strap top bars (hogging)","Strap bottom bars (sagging)","Strap shear"})Check(list.Any(x=>x.Name==name),"check present: "+name);
  Check(list.Where(x=>x.Name.StartsWith("Footing flexure")).Count()==4,"footing flexure both directions for both footings");Check(steps.Count==list.Count&&steps.Values.All(v=>v.Count>0),"every check has its derivation");
  var punch=list.First(x=>x.Name.StartsWith("Footing punching")&&x.Node=="F1");Check(steps[punch].Any(l=>l.Contains("edge")),"the property-line column punches as an edge column");
  Check(list.First(x=>x.Name=="Strap top bars (hogging)").Demand>0,"strap needs top steel at the exterior column");
  foreach(var name in new[]{"Footing depth above the bottom bars","Footing bar spacing","Bearing at the column base","Sliding of the footing (service), FS >= 1.5","Strap minimum stirrups","Strap top bar arrangement"})Check(list.Any(x=>x.Name==name),"NSCP check present: "+name);
  Check(list.Count(x=>x.Name.StartsWith("Development of bars"))==4,"development length both ways for both footings");
  var p2=list.First(x=>x.Name.StartsWith("Footing punching")&&x.Node=="F2");Check(steps[p2].Any(l=>l.Contains("interior"))&&p2.Unit=="MPa","interior column punching as a stress with the unbalanced moment");
  /* interior punching with a column moment: Jc of the ACI formula d b1³/6 + b1 d³/6 + d b2 b1²/2 */
  {var one=new StrapInput();one.Nodes.Add(new StrapNode{Node="1",B=3,L=3,T=.6,Cx=.4,Cz=.4,Bars="16@150"});var cs=new List<StrapCase>{StrapFooting.Solve(one,"U",Map(new SupportReaction{Node="1",Fy=1000,Mz=150}),true)};var st=new Dictionary<SupportCheck,List<string>>();var l2=StrapFooting.Check(one,cs,new List<string>(),st);var pc=l2.First(x=>x.Name.StartsWith("Footing punching"));
   double d=.6-.075-.016,b1=.4+d;double J=d*b1*b1*b1/6+b1*d*d*d/6+d*b1*b1*b1/2;double gv=1-1/(1+2.0/3);double Vu=1000*(1-b1*b1/9);double expect=Vu/(4*b1*d)/1000+gv*150*(b1/2)/J/1000;Near(pc.Demand,expect,"punching stress with γv M c / Jc",1e-6);}
  /* overturning: P 400 at the centre of a 2 m footing, Mz 100 and Fx -20 at 1.0 m: Mo toward +X = 100 + 20 = 120, Mr = 400 x 1 = 400 */
  {var o=new StrapInput{GammaConc=0,GammaSoil=0};o.Nodes.Add(new StrapNode{Node="1",B=2,L=2,T=.5,Bars="16@150"});var oc=new List<StrapCase>{StrapFooting.Solve(o,"1 DL+LL",Map(new SupportReaction{Node="1",Fy=400,Mz=100,Fx=-20}),false)};var os=new Dictionary<SupportCheck,List<string>>();var ol=StrapFooting.Check(o,oc,new List<string>(),os);
   var ov=ol.First(x=>x.Name.StartsWith("Overturning"));Check(Math.Abs(ov.Demand-1.5*120)<1e-6&&Math.Abs(ov.Capacity-400)<1e-6&&os[ov].Any(l=>l.Contains("+X")),"overturning of a single footing toward +X: FS = 400 / 120");
   var sl=ol.First(x=>x.Name.StartsWith("Sliding of the footing"));Check(Math.Abs(sl.Demand-1.5*20)<1e-6&&Math.Abs(sl.Capacity-.4*400)<1e-6,"sliding FS 1.5 with μ 0.40");}
  {var g=new StrapInput{GammaConc=0,GammaSoil=0,OverturningFS=2.0};g.Nodes.Add(new StrapNode{Node="1",B=1.6,L=2.8,T=.6,Ex=.6,Bars="16@150"});g.Nodes.Add(new StrapNode{Node="2",X=6,B=2.6,L=2.6,T=.6,Bars="16@150"});g.Straps.Add(new StrapBeam{From="1",To="2"});
   var gc=new List<StrapCase>{StrapFooting.Solve(g,"1 DL+E",Map(new SupportReaction{Node="1",Fy=600,Fz=-30},new SupportReaction{Node="2",Fy=900,Fz=-30}),false)};var gsd=new Dictionary<SupportCheck,List<string>>();var gl=StrapFooting.Check(g,gc,new List<string>(),gsd);
   var gov2=gl.First(x=>x.Name.StartsWith("Overturning"));Check(gov2.Node=="Group F1+F2"&&gov2.Name.Contains("2.0")&&gsd[gov2].Any(l=>l.Contains("+Z")),"strapped footings overturn as one group, required FS from the input");Check(gl.Any(x=>x.Name.StartsWith("Sliding of the strapped group")),"group sliding with the straps tying the footings");}
  Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);AccessCodes.Granted=true;
  using(var form=new StrapFootingForm()){form.ExampleCorner();{var fl=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;typeof(StrapFootingForm).GetMethod("ShowSupport",fl).Invoke(form,new object[]{"2"});var grid=(DataGridView)typeof(StrapFootingForm).GetField("supportReactions",fl).GetValue(form);Check(grid.Rows.Cast<DataGridViewRow>().Count(r=>!r.IsNewRow)==2,"support 2 shows its two load cases");
   grid.Rows[0].Cells["Fy"].Value="999";typeof(StrapFootingForm).GetMethod("StoreSupportReactions",fl).Invoke(form,null);string u;var srows=TrussSupport.Reactions(((TextBox)typeof(StrapFootingForm).GetField("reactions",fl).GetValue(form)).Text,out u);Check(srows.Count==6&&srows.Any(r=>r.Node=="2"&&Math.Abs(r.Fy-999)<1e-9)&&srows.Count(r=>r.Node=="1")==2,"editing a support's reactions updates only that support in the STAAD table");}
   var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;var doc=(CalculationDocument)typeof(StrapFootingForm).GetMethod("BuildDocument",flags).Invoke(form,null);Check(doc.Sheets.Count>=4,"report pages");
   /* separate service (sizing) and factored (design) tables: every case of the second table is factored, same names kept apart */
   {var rb=(TextBox)typeof(StrapFootingForm).GetField("reactions",flags).GetValue(form);var fb=(TextBox)typeof(StrapFootingForm).GetField("factoredReactions",flags).GetValue(form);string one=rb.Text;fb.Text=one;string u2;var both=form.Rows(out u2);var inp2=form.Input();
    Check(both.Count==12&&both.Count(r=>r.Case.EndsWith("(factored)"))==6,"the factored table adds its own cases ("+both.Count+")");Check(both.Where(r=>r.Case.EndsWith("(factored)")).All(r=>inp2.Factored(r.Case))&&both.Where(r=>!r.Case.EndsWith("(factored)")).All(r=>!inp2.Factored(r.Case)),"service table sizes, factored table designs");
    Check(form.Snapshot().Contains("FactoredReactions"),"the factored table is saved");fb.Text="";Check(form.Rows(out u2).Count==6,"one table works as before when the factored table is blank");}
   string xml=form.Snapshot();Check(xml.Contains("<Footing ")&&xml.Contains("<Strap "),"footings and straps are saved");using(var again=new StrapFootingForm()){again.Restore(xml);Check(again.Snapshot()==xml,"sheet round-trips");}}
  /* v5.1.6: do the bars fit in one layer? (strap: 400 wide, 40 cover, Ø10 stirrups -> 300 mm inside; Ø25 with 25 mm clear -> 6 per layer) */
  {var fit=list.Single(x=>x.Name=="Strap top bars fit in one layer");Check(fit.Demand==4&&fit.Capacity==6&&fit.Demand<=fit.Capacity,"4-25 fits in one layer of a 400 mm strap (6 per layer)");
   var crowd=new StrapInput();crowd.Nodes.Add(new StrapNode{Node="1",B=1.6,L=2.8,T=.6,Ex=.6});crowd.Nodes.Add(new StrapNode{Node="2",X=6,B=2.6,L=2.6,T=.6});crowd.Straps.Add(new StrapBeam{From="1",To="2",Width=.4,Depth=.8,Top="8-25",Bottom="3-20"});
   var cst=new Dictionary<SupportCheck,List<string>>();var cl=StrapFooting.Check(crowd,StrapFooting.Cases(crowd,rows,new List<string>()),new List<string>(),cst);var cf=cl.Single(x=>x.Name=="Strap top bars fit in one layer");
   Check(cf.Demand==8&&cf.Capacity==6&&cf.Demand>cf.Capacity&&cst[cf].Any(t=>t.Contains("does not fit in one layer: 2 layers (6 + 2)")),"8-25 does not fit in one layer: flagged with the 2 layers it needs");
   /* the layers lower the effective depth used by the flexure check: 8-25 in 6 + 2 -> centroid 25 mm from the stirrup, d = 800 - 40 - 10 - 25 = 725 (one layer: 737.5) */
   Check(Math.Abs(StrapFooting.Centroid(new[]{6,2},25)-25)<1e-9&&Math.Abs(StrapFooting.Centroid(new[]{4},25)-12.5)<1e-9,"centroid of the layers");
   var flex=cl.Single(x=>x.Name=="Strap top bars (hogging)");Check(cst[flex].Any(t=>t.Contains("= 725 mm")),"flexure uses d = 725 mm for two layers");
   var flex1=list.Single(x=>x.Name=="Strap top bars (hogging)");Check(steps[flex1].Any(t=>t.Contains("= 738 mm")||t.Contains("= 737 mm")),"one layer: d = 737.5 mm");
   Check(list.Any(x=>x.Name=="Footing bars fit in one layer (clear spacing)"&&x.Capacity>=x.Demand),"footing bar clear spacing checked");}
  /* bars per layer for each width: n = floor((inside + c) / (db + c)), c = max(25, db, 4/3 aggregate); 40 cover, Ø10 stirrups, 20 mm aggregate */
  foreach(var t in new[]{new[]{250,20,3},new[]{300,20,4},new[]{350,20,5},new[]{400,20,7},new[]{400,25,6},new[]{500,25,8},new[]{300,16,5}})Check(BeamCutting.PerLayer(t[0]-2*40-2*10,t[1],20)==t[2],t[0]+" mm wide: "+t[2]+" bars of Ø"+t[1]+" per layer");
  Check(BeamCutting.PerLayer(60,28,20)==1,"a section narrower than two bars and one clear space holds one bar per layer (no false 2)");
  /* permissible loss of contact (owner, 23 November 2026): no-tension soil, one-way eccentricity e > L/6 -> q max = 2R / (3 B (L/2 - e)),
     contact 3 (L/2 - e) / L */
  {double B=2,L=3,R=600,e=.8;var nt=StrapFooting.NoTension(B,L,R,0,R*e);Near(nt[0],2*R/(3*B*(L/2-e)),"no-tension peak, one way",2e-3);Near(nt[1],3*(L/2-e)/L,"contact fraction, one way",0.02);
   var lin=StrapFooting.NoTension(B,L,R,0,R*.3);Near(lin[0],R/(B*L)*(1+6*.3/L),"within the kern the plane stays linear",1e-3);Check(double.IsInfinity(StrapFooting.NoTension(B,L,-5,0,0)[0]),"net uplift: no contact");}
  Check(StrapFooting.LooksLateral("1.0D+0.75(L+0.6W)")&&StrapFooting.LooksLateral("D+0.7EQX")&&StrapFooting.LooksLateral("DL + WIND")&&!StrapFooting.LooksLateral("DEAD + LIVE")&&!StrapFooting.LooksLateral("1.2D+1.6L"),"wind / seismic cases recognised");
  /* ground water table 0.5 m below grade, base at 1.5 m: uplift 9.81 x 1.0 x B x L off the weight; flotation check present */
  {var gw=new StrapInput{Df=1.5,Gwt=.5,GammaConc=24,GammaSoil=18};var nd=new StrapNode{Node="1",B=2,L=2,T=.5};gw.Nodes.Add(nd);Near(StrapFooting.Uplift(gw,nd),9.81*1.0*4,"water uplift");Near(StrapFooting.FootingWeight(gw,nd),4*(.5*24+1.0*18)-39.24,"net footing weight");
   var dry=new StrapInput{Df=1.5,GammaConc=24,GammaSoil=18};Near(StrapFooting.FootingWeight(dry,nd),4*(.5*24+1.0*18),"no water table: full weight");
   var gRows=new List<SupportReaction>{new SupportReaction{Node="1",Case="DL+LL",Fy=10}};var gNotes=new List<string>();var gSteps=new Dictionary<SupportCheck,List<string>>();var gCases=StrapFooting.Cases(gw,gRows,gNotes);var ck=StrapFooting.Check(gw,gCases,gNotes,gSteps);
   Check(ck.Any(x=>x.Name.StartsWith("Flotation")),"flotation check with a water table");}
  Console.WriteLine("PASS: "+checks+" strap foundation checks");}
}
