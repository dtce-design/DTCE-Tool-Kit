using System;using System.Linq;using BeamScheduleFormatter;
class LintelScheduleTests {
 static int checks;static void Check(bool x,string message){if(!x)throw new Exception(message);checks++;}static void Near(double a,double b,string m){Check(Math.Abs(a-b)<1e-6,m+" ("+a+" vs "+b+")");}
 static void Main(){
  /* the schedule of the Lintel Beam Design Calculation Report: CHB thickness + clear opening, the larger mark between two ranges */
  Check(LintelSchedule.Select(100,.6).Mark=="LB-4A"&&LintelSchedule.Select(100,2.0).Mark=="LB-4A"&&LintelSchedule.Select(100,2.05).Mark=="LB-4B","4\" ranges");
  Check(LintelSchedule.Select(100,4.0).Mark=="LB-4C"&&LintelSchedule.Select(100,4.5).Mark=="LB-4D"&&LintelSchedule.Select(100,6.0).Mark=="LB-4E","4\" wide openings");
  Check(LintelSchedule.Select(150,.9).Mark=="LB-6A"&&LintelSchedule.Select(150,3.0).Mark=="LB-6B"&&LintelSchedule.Select(150,5.5).Mark=="LB-6E","6\" ranges");
  Check(LintelSchedule.Select(100,.4).Mark=="LB-4A","an opening under 0.60 m takes the first mark");
  bool wide=false;try{LintelSchedule.Select(150,6.2);}catch(Exception){wide=true;}Check(wide,"over 6.00 m needs its own design");
  var e4=LintelSchedule.Select(100,5.5);Check(e4.Width==100&&e4.Depth==800&&e4.Bottom=="1-25"&&e4.Top=="1-12"&&e4.Mid=="2-10"&&e4.TieSpacing==250&&e4.SingleLeg&&e4.Bearing==400&&e4.Brace=="Required","LB-4E as scheduled");
  var b6=LintelSchedule.Select(150,2.5);Check(b6.Width==150&&b6.Depth==400&&b6.Bottom=="2-16"&&b6.Top=="2-12"&&!b6.SingleLeg&&b6.TieSpacing==150&&b6.Bearing==300&&b6.JambBarsPerSide==2,"LB-6B as scheduled");
  Check(LintelSchedule.Select(150,1.0).JambBarsPerSide==1,"one jamb bar at 200 mm bearing");
  Near(LintelSchedule.HookExtension(12),150,"12 mm hook");Near(LintelSchedule.HookExtension(16),200,"16 mm hook");Near(LintelSchedule.HookExtension(20),250,"20 mm hook");Near(LintelSchedule.HookExtension(25),300,"25 mm hook");
  /* take-off of a 0.90 x 2.10 m door, 2 units, in a 150 mm wall: LB-6A, L = 0.90 + 2 x 0.20 = 1.30 m */
  var cfg=CalculationStore.Resolve(21,40,275,40,275);var door=new OpeningItem{Id="D1",Kind="Door",Wall="W1",Width=.9,Height=2.1,Quantity=2};var wall=new WallItem{Id="W1",Description="x",ThicknessMm=150,Length=5,Height=3,Quantity=1};
  LintelMark m;var q=LintelSchedule.Takeoff(door,wall,cfg,out m);Check(m.Mark=="LB-6A"&&q.Kind=="Lintel Beam"&&q.Quantity==2,"door lintel mark and units");
  Near(q.Concrete,1.3*.15*.3*2,"concrete = L x b x h x units");Near(q.Forms,(2*.3*1.3+.15*.9)*2,"forms = two sides over L + soffit over the opening");
  var bottom=q.Steel.Result.Lines.First(l=>l.Part=="Bottom longitudinal");Check(bottom.Diameter==12&&bottom.PiecesPerBeam==2,"2-12 bottom");Near(bottom.CutLength,1.55,"bottom cut = L - 2 x 30 mm + two 150 mm hooks (1.54), rounded up to 25 mm");
  var ties=q.Steel.Result.Lines.First(l=>l.Part=="Stirrups");Check(ties.PiecesPerBeam==(int)Math.Ceiling((1300-100)/125.0)+1,"ties @ 125 over the lintel");
  var jamb=q.Steel.Result.Lines.First(l=>l.Part=="Jamb bars");Check(jamb.Diameter==12&&jamb.PiecesPerBeam==2,"one jamb bar each side");
  Check(q.Steel.Result.Total>0&&q.Steel.Result.Lines.All(l=>l.Kg>0),"rebar weights");
  var win=new OpeningItem{Id="W2",Kind="Window",Wall="W1",Width=1.2,Height=1.2,Quantity=1};var qw=LintelSchedule.Takeoff(win,wall,cfg,out m);var jw=qw.Steel.Result.Lines.First(l=>l.Part=="Jamb bars");Check(jw.CutLength>=2.1+.3,"window jamb bars run from the floor (head at 2.10 m)");
  Console.WriteLine("PASS: "+checks+" lintel schedule and take-off checks");}
}
