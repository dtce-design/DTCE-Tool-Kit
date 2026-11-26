using System;using System.Linq;using BeamScheduleFormatter;
/* Dead Loads sheet: the owner's Excel values give the same totals */
class DeadLoadsTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static void Main(){
  using(var f=new DeadLoadsForm()){f.Show();System.Windows.Forms.Application.DoEvents();
   var ft=f.FloorTotals();Func<int,int,string> at=(r,c)=>ft[r][c];
   Check(at(0,1)=="4.31"&&at(1,1)=="1.36","Typical: total 4.31, STAAD 1.36: "+at(0,1)+" "+at(1,1));
   Check(at(0,2)=="0.35"&&at(1,2)=="0.35","Ceiling 0.35");Check(at(0,3)=="6.97"&&at(1,3)=="4.02","Roof slab 6.97 / 4.02");Check(at(0,4)=="3.37"&&at(1,4)=="0.42","Gutter 3.37 / 0.42");Check(at(0,5)=="3.92"&&at(1,5)=="0.97","Stair 3.92 / 0.97");
   Check(f.PurlinCount()==25&&Math.Abs(f.PurlinKpa()-0.0757)<0.0005,"purlins 25 pcs, 0.076 kPa: "+f.PurlinKpa());
   var rt=f.RoofTotals();Check(rt[0][1]=="0.38"&&rt[0][2]=="0.35"&&rt[0][3]=="0.45","roof totals 0.38 / 0.35 / 0.45: "+string.Join(",",rt[0]));Check(rt[1][2]=="1.40"&&rt[1][3]=="0.27","STAAD bottom 1.40 kN/m, purlin 0.27 kN/m");
   string[] h;var ll=f.LineLoads(out h);Check(ll[0][0]=="Ground Floor"&&ll[0][5]=="2.55"&&ll[3][3]=="2.53","line loads: curtain wall 5.10 x 0.5 = 2.55, parapet 1.20 x 2.11 = 2.53");
   double kn,area;double e=f.ElevatorKpa(out kn,out area);Check(Math.Abs(kn-37.278)<0.001&&Math.Abs(e-4.142)<0.001&&Math.Abs(e*2-8.284)<0.002,"elevator 37.278 kN, 4.142 kPa, 8.284 with impact");
   var rows=DeadLoadsForm.Parse("a\tb\nx\t1");Check(rows.Count==2&&rows[1][1]=="1","table text");f.Close();}
  Console.WriteLine("DeadLoadsTests passed: "+checks+" checks");}
}
