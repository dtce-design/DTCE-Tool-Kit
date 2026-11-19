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
  foreach(var name in new[]{"Bearing pressure (service)","Full contact, no uplift (service)","Footing punching shear (two-way)","Strap top bars (hogging)","Strap bottom bars (sagging)","Strap shear"})Check(list.Any(x=>x.Name==name),"check present: "+name);
  Check(list.Where(x=>x.Name.StartsWith("Footing flexure")).Count()==4,"footing flexure both directions for both footings");Check(steps.Count==list.Count&&steps.Values.All(v=>v.Count>0),"every check has its derivation");
  var punch=list.First(x=>x.Name.StartsWith("Footing punching")&&x.Node=="F1");Check(steps[punch].Any(l=>l.Contains("edge")),"the property-line column punches as an edge column");
  Check(list.First(x=>x.Name=="Strap top bars (hogging)").Demand>0,"strap needs top steel at the exterior column");
  Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);AccessCodes.Granted=true;
  using(var form=new StrapFootingForm()){form.ExampleCorner();var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;var doc=(CalculationDocument)typeof(StrapFootingForm).GetMethod("BuildDocument",flags).Invoke(form,null);Check(doc.Sheets.Count>=4,"report pages");
   string xml=form.Snapshot();Check(xml.Contains("<Footing ")&&xml.Contains("<Strap "),"footings and straps are saved");using(var again=new StrapFootingForm()){again.Restore(xml);Check(again.Snapshot()==xml,"sheet round-trips");}}
  Console.WriteLine("PASS: "+checks+" strap foundation checks");}
}
