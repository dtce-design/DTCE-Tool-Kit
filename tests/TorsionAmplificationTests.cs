using System;using System.Linq;using System.Text;using BeamScheduleFormatter;
class TorsionAmplificationTests{static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 [STAThread]static void Main(){
 /* 2 bays x 1 bay, 2 storeys; joints 1-6 base, 7-12 level 1, 13-18 level 2 (Y up); EQ+X: edge Z=0 moves 10/20, edge Z=5 moves 14/28 */
 var std=new StringBuilder("UNIT METER KN\nJOINT COORDINATES\n");int n=1;foreach(var y in new[]{0.0,3.5,7.0})foreach(var z in new[]{0.0,5.0})foreach(var x in new[]{0.0,6.0,12.0})std.AppendLine(n++ +" "+x+" "+y+" "+z+";");
 std.AppendLine("MEMBER INCIDENCES");int m=1;for(int k=1;k<=12;k++)std.AppendLine(m++ +" "+k+" "+(k+6)+";");std.AppendLine("LOAD 1 LOADTYPE SEISMIC-H TITLE EQ+X+E\nUBC LOAD X 1 ACC 0.05\nLOAD 5 LOADTYPE SEISMIC-H TITLE EQ+Z+E\nUBC LOAD Z 1\nFINISH");
 var model=RedundancyFactor.ReadModel(std.ToString());var t=new StringBuilder();
 for(int j=7;j<=18;j++){var jt=model.Joints[j];bool lvl2=jt.Y>5;double dx=(jt.Z>1?14:10)*(lvl2?2:1);double dz=(jt.X>7?6:jt.X>1?5:4)*(lvl2?2:1);t.AppendLine(j+"\t1 EQ+X+E\t"+dx+"\t0\t0\t"+dx+"\t0\t0\t0");t.AppendLine("\t5 EQ+Z+E\t0\t0\t"+dz+"\t"+dz+"\t0\t0\t0");}
 var d=TorsionAmplification.ReadDisplacements(t.ToString());var rx=TorsionAmplification.Compute(model,d,1,"1","X");var rz=TorsionAmplification.Compute(model,d,5,"5","Z");
 /* X: d1=10, d2=14, dmax=14, davg=12, ratio 1.1667 -> (14/14.4)^2 <1 -> Ax 1.0 ; Z: edges X=0 (4) and X=12 (6): dmax 6 davg 5 ratio 1.2 -> 1.0 */
 Check(Math.Abs(rx.Ax-1)<1e-9&&Math.Abs(rx.Levels[0].Ratio-14/12.0)<1e-9&&Math.Abs(rz.Levels[0].Ratio-1.2)<1e-9,"edges and ratios; Ax not below 1.0");
 var t2=new StringBuilder();for(int j=7;j<=18;j++){var jt=model.Joints[j];double dx=(jt.Z>1?30:10);t2.AppendLine(j+"\t1 EQ+X+E\t"+dx+"\t0\t0\t0\t0\t0\t0");}
 var r2=TorsionAmplification.Compute(model,TorsionAmplification.ReadDisplacements(t2.ToString()),1,"1","X");Check(Math.Abs(r2.Ax-Math.Pow(30/24.0,2))<1e-9&&r2.Governing.Contains("torsionally irregular"),"Ax = (dmax / 1.2 davg)^2");
 var anl="JOINT DISPLACEMENT (CM   RADIANS)\n   7    1   1.0000   0.0000   0.0000   0.0000   0.0000   0.0000\n        5   0.0000   0.0000   1.0000   0.0000   0.0000   0.0000\nMEMBER END FORCES\n 1 1 1 1 2 3 4 5 6\n   2 7 1 2 3 4 5 6";
 var d3=TorsionAmplification.ReadDisplacements(anl);Check(d3.Count==2&&d3["7|5"][2]==1,"the .ANL displacement block only (member end forces skipped)");
 var big=new StringBuilder();for(int j=7;j<=18;j++)big.AppendLine(j+"\t1 EQ+X+E\t"+(model.Joints[j].Z>1?50:0)+"\t0\t0\t0\t0\t0\t0");Check(TorsionAmplification.Compute(model,TorsionAmplification.ReadDisplacements(big.ToString()),1,"1","X").Ax-Math.Pow(2/1.2,2)<1e-9,"one edge still: dmax / davg = 2, Ax = (2 / 1.2)^2 = 2.78 (limit 3.0)");
 Console.WriteLine("TorsionAmplificationTests passed: "+checks+" checks");}}
