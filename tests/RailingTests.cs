using System;using System.Linq;using BeamScheduleFormatter;
class RailingTests {
 static int checks;static void Check(bool ok,string m){checks++;if(!ok)throw new Exception(m);}static void Near(double a,double b,string m){Check(Math.Abs(a-b)<1e-6,m+": "+a+" != "+b);}
 static void Main(){
  Check(RailingSections.PiecesPerRun(10,1500,0)==8,"Posts: ceil(10000/1500)+1 = 8");Check(RailingSections.PiecesPerRun(10,0,2)==2,"Count overrides spacing");Check(RailingSections.PiecesPerRun(10,0,0)==1,"A rail without count is one line");
  Check(RailingSections.Segments(6)==1&&RailingSections.Segments(6.01)==2&&RailingSections.Segments(10)==2&&RailingSections.Segments(12.5)==3,"Pieces over 6 m split into equal cuts");
  var r12=RailingSections.Find("Round bar 12mm");Near(r12.KgM,0.888,"Round bar 12 kg/m from 7850 kg/m3");Near(SteelPainting.AreaPerMetre(r12),Math.PI*0.012,"Round bar painted perimeter");
  var s16=RailingSections.Find("Square bar 16x16mm");Near(s16.KgM,2.01,"Square bar 16 kg/m");Near(SteelPainting.AreaPerMetre(s16),0.064,"Square bar painted perimeter");
  Check(RailingSections.For("Pipe").Any()&&RailingSections.For("Rectangular tube").All(p=>p.H!=p.B)&&RailingSections.For("Square tube").All(p=>p.H==p.B)&&RailingSections.For("Square tube").Any(),"Regan pipes / RHS / SHS sections are offered");
  Near(RailingSections.Find("Pipe 1in light gauge OD33.4x2.6mm").KgM,1.975,"Regan p.16 light gauge 1in");Near(RailingSections.Find("Pipe 1in heavy gauge OD33.4x3.4mm").KgM,2.515,"Regan p.15 heavy gauge 1in");
  Near(RailingSections.Find("Strux tube 1in OD33.2x1.62mm").KgM,1.238,"Regan p.17 Strux 1in");Near(RailingSections.Find("SHS 31.8x31.8x1.5mm").KgM,1.519,"Regan p.22 SHS 1-1/4");Near(RailingSections.Find("RHS 50x40x2mm").KgM,2.871,"Regan p.24 RHS 1-1/2 x 2");
  Check(RailingSections.Find("SHS 38x38x3mm")==null,"Misprinted 38x38x3 row left out");Check(RailingSections.Find("Pipe OD33.4x3.4mm")!=null,"v1.4.96 saved pipe name still found");
  Check(RailingSections.For("Flat bar").Any()&&RailingSections.For("Pipe (light gauge)").Count()==14&&RailingSections.For("Strux tube").Count()==9&&RailingSections.For("Square tube").Count()==55&&RailingSections.For("Rectangular tube").Count()==39,"Full Regan railing tables");
  Check(RailingSections.Families.All(f=>RailingSections.For(f).All(p=>SteelPainting.AreaPerMetre(p)>0)),"Every railing section has a painted perimeter");
  Near(RailingSections.Find("Pipe 1-1/2in seamless Sch 40 OD48.2x3.68mm").KgM,4.1,"Regan p.18 Sch 40");Check(RailingSections.For("Pipe (Sch 40)").Count()==16&&RailingSections.For("Pipe (Sch 80)").Count()==16&&RailingSections.For("Pipe (Sch 160)").Count()==14,"Seamless schedules");
  var stocks=SteelCutPlanner.Allocate(new[]{new SteelCutDemand{Member="B",Specification="Round bar 12mm",Length=0.9,Quantity=101,KgM=0.888}});Check(stocks.Count==17,"101 balusters of 0.9 m need 17 stocks of 6 m");Near(stocks.Sum(s=>s.Remaining),11.1,"Final offcut");
  Console.WriteLine("PASS: "+checks+" railing piece count, splice, bar weight, painting and 6 m stock checks");
 }
}
