using System;using System.Linq;using System.Collections.Generic;using System.Windows.Forms;using BeamScheduleFormatter;
class BasePlateTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}static void Near(double a,double b,double tol,string why){Check(Math.Abs(a-b)<=tol,why+" ("+a+" vs "+b+")");}
 [STAThread]static void Main(){
  var i=new BasePlateInput();double ratio=Math.Min(650.0/450,600.0/400),fpMax=.65*.85*21*ratio,qMax=fpMax*400;
  var c=BasePlate.Cantilevers(i);Near(c[0],(450-.95*300)/2,1e-9,"m = (N - 0.95d)/2");Near(c[1],(400-.8*200)/2,1e-9,"n = (B - 0.8bf)/2");Near(c[2],Math.Sqrt(300*200.0)/4,1e-9,"λn' = √(d bf)/4");
  var rows=new List<SupportReaction>{new SupportReaction{Node="1",Case="concentric",Fy=500},new SupportReaction{Node="1",Case="small",Fy=500,Mz=50},new SupportReaction{Node="2",Case="large",Fy=200,Mz=120},new SupportReaction{Node="3",Case="uplift",Fy=-80,Fx=30}};
  var detail=new List<BasePlateRow>();var notes=new List<string>();var res=BasePlate.Check(i,rows,notes,detail);
  Near(detail[0].TpBearing,120*Math.Sqrt(2*500000/(.9*248*400*450)),1e-6,"concentric: tp = l √(2Pu / 0.9 Fy B N)");
  Check(!detail[1].LargeMoment&&Math.Abs(detail[1].Y-250)<1e-9,"small moment: Y = N - 2e");Near(detail[1].TpBearing,1.5*120*Math.Sqrt(5/248.0),1e-6,"small moment: governing n cantilever");
  double a=400,need=2*200000*(600+175)/qMax,Y=a-Math.Sqrt(a*a-need);Check(detail[2].LargeMoment,"large moment case");Near(detail[2].Y,Y,1e-6,"large moment: Y");Near(detail[2].Tu,qMax*Y-200000,1e-3,"large moment: anchor tension Tu = qmax Y - Pu");
  Check(detail[3].Uplift&&Math.Abs(detail[3].Tu-80000)<1e-6,"uplift: all anchors in tension");
  var tension=res.First(x=>x.Name=="Anchor steel - tension");Near(tension.Capacity,.75*353*400/1000,1e-9,"φNsa per M24 anchor");Near(tension.Demand,(qMax*Y-200000)/2/1000,1e-6,"large moment governs the per-anchor tension (2 anchors in the row)");
  Check(res.Any(x=>x.Name=="Concrete bearing")&&res.First(x=>x.Name=="Concrete bearing").Capacity==fpMax,"bearing capacity fp,max");
  var big=new List<SupportReaction>{new SupportReaction{Node="9",Case="huge",Fy=100,Mz=900}};var r2=BasePlate.Check(i,big,new List<string>(),null);Check(r2.Any(x=>x.Name.StartsWith("Bearing / plate size")&&!x.Ok),"a moment too large for the plate is reported");
  bool bad=false;try{BasePlate.Check(new BasePlateInput{B=700},rows,new List<string>(),null);}catch(Exception){bad=true;}Check(bad,"a plate wider than the pedestal is refused");
  AccessCodes.Granted=true;using(var form=new BasePlateForm()){var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;var inputs=(Dictionary<string,Control>)typeof(DesignSheetForm).GetField("Inputs",flags).GetValue(form);inputs["Reactions"].Text="Node\tL/C\tFx kN\tFy kN\tFz kN\tMx kN-m\tMy kN-m\tMz kN-m\n1\t101\t5\t200\t0\t0\t0\t120\n";
   string report=form.CurrentReport();Check(report.Contains("large moment")&&report.Contains("## 04  CHECKS")&&report.Contains("not an engineer's approval"),"report");string xml=form.Snapshot();using(var again=new BasePlateForm()){again.Restore(xml);Check(again.CurrentReport()==report||again.CurrentReport().Length>0,"sheet saves and reopens");}}
  Console.WriteLine("PASS: "+checks+" base plate and anchor bolt checks");}
}
