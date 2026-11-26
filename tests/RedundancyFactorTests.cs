using System;using System.Linq;using System.Text;using BeamScheduleFormatter;
/* Redundancy factor (NSCP 2015 208.5.1.1, Eq. 208-20) from a STAAD model and the member end forces of the EQ cases */
class RedundancyFactorTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static void Main(){
  /* 2 bays along X (0, 6, 12 m) x 1 bay along Z (0, 5 m), 2 storeys of 3.5 m, Y up; joints 1-6 base, 7-12 level 1, 13-18 level 2 */
  var std=new StringBuilder("STAAD SPACE\nUNIT METER KN\nJOINT COORDINATES\n");int n=1;foreach(var y in new[]{0.0,3.5,7.0})foreach(var z in new[]{0.0,5.0})foreach(var x in new[]{0.0,6.0,12.0})std.AppendLine(n++ +" "+x+" "+y+" "+z+";");
  std.AppendLine("MEMBER INCIDENCES");int m=1;
  for(int k=1;k<=12;k++)std.AppendLine(m++ +" "+k+" "+(k+6)+";");/* columns 1-12 */
  foreach(var lvl in new[]{6,12})foreach(var row in new[]{0,3}){std.AppendLine(m++ +" "+(lvl+row+1)+" "+(lvl+row+2)+";");std.AppendLine(m++ +" "+(lvl+row+2)+" "+(lvl+row+3)+";");}/* X beams 13-20 */
  foreach(var lvl in new[]{6,12})for(int c=1;c<=3;c++)std.AppendLine(m++ +" "+(lvl+c)+" "+(lvl+c+3)+";");/* Z beams 21-26 */
  std.AppendLine("DEFINE MATERIAL START\nFINISH");
  var model=RedundancyFactor.ReadModel(std.ToString());Check(model.Joints.Count==18&&model.Members.Count==26,"model read: "+model.Joints.Count+" joints, "+model.Members.Count+" members");
  Check(Math.Abs(RedundancyFactor.BaseArea(model)-60)<1e-6,"A_B = 12 x 5 = 60 m2: "+RedundancyFactor.BaseArea(model));
  /* load 5 EQ+X: exterior columns 10 kN, interior 20 kN in storey 1; 6 / 12 kN in storey 2 (local Fy) - .ANL rows member load joint fx fy fz mx my mz */
  var anl=new StringBuilder();for(int c=1;c<=12;c++){bool interior=(c-1)%3==1;double v=c<=6?(interior?20:10):(interior?12:6);int a=c,b=c+6;anl.AppendLine(c+" 5 "+a+" 100 "+v+" 0 0 0 0");anl.AppendLine("  "+b+" -100 "+(-v)+" 0 0 0 0");}
  var f=RedundancyFactor.ReadForces(anl.ToString());Check(f.Count==24&&f["2|5|2"][1]==20,"end forces read: "+f.Count);
  var r=RedundancyFactor.Compute(model,f,5,"5 EQ+X","X","Moment");
  Check(r.Stories.Count==2&&Math.Abs(r.Stories[0].Shear-80)<1e-9,"storey 1 shear = 2 x (10 + 20 + 10) = 80: "+r.Stories[0].Shear);
  /* bay: exterior 10 + 70 % of the interior 20 = 24; r = 24 / 80 = 0.30 */
  Check(Math.Abs(r.Stories[0].Element-24)<1e-9&&Math.Abs(r.Stories[0].Ratio-0.3)<1e-9&&r.Stories[0].Element0.Contains("70 %"),"70 % of an interior column: "+r.Stories[0].Element+" "+r.Stories[0].Element0);
  Check(r.Stories[0].Counted&&r.Stories[1].Counted&&Math.Abs(r.Stories[1].Shear-48)<1e-9&&Math.Abs(r.RMax-0.3)<1e-9,"storeys starting at or below 2/3 h = 4.67 m count (0 and 3.5 m): "+r.RMax);
  /* along Z the beams join one column of each frame line: no 70 % (one beam each side only along X) */
  var rz=RedundancyFactor.Compute(model,f,5,"5 EQ+X","Z","Moment");Check(Math.Abs(rz.Stories[0].Element-40)<1e-9&&Math.Abs(rz.RMax-0.5)<1e-9,"Z frames: 20 + 20 = 40, r = 0.5: "+rz.RMax);
  /* table copy (tab separated: Beam, L/C, Node, Fx, Fy, Fz, Mx, My, Mz) gives the same */
  var tab=new StringBuilder();for(int c=1;c<=12;c++){bool interior=(c-1)%3==1;double v=c<=6?(interior?20:10):(interior?12:6);tab.AppendLine(c+"\t5 EQ+X\t"+c+"\t100\t"+v+"\t0\t0\t0\t0");tab.AppendLine("\t\t"+(c+6)+"\t-100\t"+(-v)+"\t0\t0\t0\t0");}
  var r2=RedundancyFactor.Compute(model,RedundancyFactor.ReadForces(tab.ToString()),5,"5 EQ+X","X","Moment");Check(Math.Abs(r2.RMax-0.3)<1e-9,"STAAD table copy: "+r2.RMax);
  /* Eq. 208-20 and the limits */
  double raw;string note;Check(RedundancyFactor.Rho(0.3,60,"Moment",false,out raw,out note)==1.0&&raw<1&&note.Contains("1.0"),"rho below 1.0 -> 1.0: "+raw);
  Check(Math.Abs(RedundancyFactor.Rho(0.8,100,"Moment",false,out raw,out note)-(2-6.1/8.0))<1e-9,"rho = 2 - 6.1 / (0.8 x 10) = 1.2375");
  Check(RedundancyFactor.Rho(1.0,400,"Moment",false,out raw,out note)==1.5&&note.Contains("1.5"),"rho need not exceed 1.5");
  Check(RedundancyFactor.Rho(1.0,100,"Moment SMRF",false,out raw,out note)>1.25&&note.Contains("NG"),"SMRF: rho above 1.25 is NG");
  Check(RedundancyFactor.Rho(1.0,100,"Moment",true,out raw,out note)==1.0,"Zone 2: rho = 1.0");
  /* a missing member: reported, not silently zero */
  var part=RedundancyFactor.ReadForces(string.Join("\n",anl.ToString().Split('\n').Skip(2)));var r3=RedundancyFactor.Compute(model,part,5,"5","X","Moment");Check(r3.Governing.StartsWith("Some members have no end forces"),"missing end forces reported");
  Console.WriteLine("RedundancyFactorTests passed: "+checks+" checks");}
}
