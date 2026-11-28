using System;using System.Collections.Generic;using System.Linq;using System.Text;using BeamScheduleFormatter;
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
  /* automatic (owner, 26 November 2026, "ikaw na bahala sa redundancy basta paste ko na lang"): EQ cases and directions from the STAAD input */
  var loads="LOAD 1 LOADTYPE DEAD TITLE DL\nSELFWEIGHT Y -1\nLOAD 5 LOADTYPE SEISMIC TITLE EQ+X\n1893 LOAD X 1\nLOAD 6 LOADTYPE SEISMIC TITLE EQUAKE 2\nJOINT LOAD\n7 FZ 50\nLOAD 7 LOADTYPE SEISMIC TITLE SEISMIC Y\nLOAD COMB 100 1.2DL\n1 1.2\nPERFORM ANALYSIS\nFINISH";
  var eqs=RedundancyFactor.SeismicCases(std+"\n"+loads);Check(eqs.Count==3&&eqs[0].No==5&&eqs[0].Axis=="X"&&eqs[1].No==6&&eqs[1].Axis=="Z"&&eqs[2].Axis=="","seismic cases and directions from the input: "+string.Join(",",eqs.Select(e=>e.No+e.Axis)));
  Check(RedundancyFactor.TableCases("1\t5 EQ+X\t1\t100\t10\t0\t0\t0\t0\n\t6 EQ+Z\t1\t1\t1\t1\t0\t0\t0").Select(e=>e.Axis).SequenceEqual(new[]{"X","Z"}),"seismic cases from the table titles");
  /* the .ANL echoes the input with line numbers */var echo=string.Join("\n",std.ToString().Replace("\r","").Split('\n').Select((l,i)=>"   "+(i+1)+". "+l));var m2=RedundancyFactor.ReadModel(echo);Check(m2.Joints.Count==18&&m2.Members.Count==26&&RedundancyFactor.HasModel(echo),"model from the .ANL echo");
  /* a gable rafter is not a brace; a floor-to-floor diagonal is */
  var gable=std.ToString().Replace("DEFINE MATERIAL START","JOINT COORDINATES\n19 6 9 0\nMEMBER INCIDENCES\n27 13 19\n28 19 15\nDEFINE MATERIAL START");var mg=RedundancyFactor.ReadModel(gable);Check(!RedundancyFactor.HasBraces(mg),"rafters to a ridge are not braces");
  var rg=RedundancyFactor.Compute(mg,f,5,"5","X","Moment");Check(Math.Abs(rg.RMax-0.3)<1e-9,"rafters change nothing: "+rg.RMax);
  var braced=std.ToString().Replace("DEFINE MATERIAL START","MEMBER INCIDENCES\n29 1 8\nDEFINE MATERIAL START");Check(RedundancyFactor.HasBraces(RedundancyFactor.ReadModel(braced)),"a diagonal from the base to level 1 is a brace");
  /* the .ANL opened directly (owner: "i open ko na lang yung ANL file"): only the JOINT DISPLACEMENT and MEMBER END FORCES blocks are kept */
  {string anlOut="   STAAD SPACE                                          -- PAGE NO.    9\n\n    65. PRINT JOINT DISPLACEMENTS ALL\n\n   JOINT DISPLACEMENT (CM   RADIANS)    STRUCTURE TYPE = SPACE\n   ------------------\n\n JOINT  LOAD   X-TRANS   Y-TRANS   Z-TRANS   X-ROTAN   Y-ROTAN   Z-ROTAN\n\n      1    5    0.0000    0.0000    0.0000    0.0000    0.0000    0.0000\n           6    0.0000    0.0000    0.0000    0.0000    0.0000    0.0000\n      7    5    1.2000    0.0010    0.0200    0.0001    0.0002    0.0003\n           6    0.0100    0.0010    1.1000    0.0001    0.0002    0.0003\n\n   STAAD SPACE                                          -- PAGE NO.   10\n\n    66. PRINT MEMBER FORCES ALL\n\n   MEMBER END FORCES    STRUCTURE TYPE = SPACE\n   -----------------\n   ALL UNITS ARE -- KN   METE  (LOCAL )\n\n  MEMBER  LOAD  JT     AXIAL   SHEAR-Y  SHEAR-Z   TORSION     MOM-Y      MOM-Z\n\n      1    5     1     12.34      3.50      0.20      0.00      0.10      7.00\n                 7    -12.34     -3.50     -0.20      0.00      0.30      3.50\n           6     1      2.00      0.10      4.00      0.00      8.00      0.20\n                 7     -2.00     -0.10     -4.00      0.00      4.00      0.10\n\n    67. PRINT SUPPORT REACTION ALL\n\n  SUPPORT REACTIONS -UNIT KN   METE      STRUCTURE TYPE = SPACE\n  -----------------\n\n  JOINT  LOAD   FORCE-X   FORCE-Y   FORCE-Z     MOM-X     MOM-Y     MOM Z\n\n      1    5    -3.50     12.34     0.20      0.10      0.00      7.00\n\n    68. FINISH\n";
   string disp,forces;StaadAnl.Split(anlOut,out disp,out forces);var fd=RedundancyFactor.ReadForces(forces);var dd=TorsionAmplification.ReadDisplacements(disp);
   Check(fd.Count==4&&Math.Abs(fd["1|5|1"][1]-3.5)<1e-9&&Math.Abs(fd["1|6|7"][2]+4)<1e-9&&!forces.Contains("SUPPORT")&&!forces.Contains("X-TRANS"),".ANL member end forces block: "+fd.Count);
   Check(dd.Count==4&&Math.Abs(dd["7|5"][0]-1.2)<1e-9&&Math.Abs(dd["7|6"][2]-1.1)<1e-9&&!disp.Contains("AXIAL"),".ANL joint displacement block: "+dd.Count);
   var whole=RedundancyFactor.ReadForces(anlOut);Check(whole.Count==4&&!whole.ContainsKey("5|0.0000|0"),"the whole .ANL in the forces box reads only MEMBER END FORCES: "+whole.Count);}
  /* owner (28 November 2026, "paki make sure tama yung computation ng redundancy factor"): pedestals below the ground beams, a column split at
     mid-storey (wall mesh) and beams split between the columns give one story with one bay pair, not thin false stories */
  {var t=new StringBuilder("UNIT METER KN\nJOINT COORDINATES\n");int jn=0;var id=new Dictionary<string,int>();Func<double,double,int> J=(x,y)=>{string k=x+","+y;int v;if(id.TryGetValue(k,out v))return v;id[k]=++jn;t.Append(jn+" "+x+" "+y+" 0;\n");return jn;};
   var mem=new List<int[]>();foreach(var x in new[]{0.0,6,12}){mem.Add(new[]{J(x,-1.5),J(x,0)});}mem.Add(new[]{J(0,0),J(0,1.75)});mem.Add(new[]{J(0,1.75),J(0,3.5)});mem.Add(new[]{J(6,0),J(6,3.5)});mem.Add(new[]{J(12,0),J(12,3.5)});
   foreach(var y in new[]{0.0,3.5})foreach(var x in new[]{0.0,3,6,9}){mem.Add(new[]{J(x,y),J(x+3,y)});}
   t.Append("MEMBER INCIDENCES\n");for(int q=0;q<mem.Count;q++)t.Append((q+1)+" "+mem[q][0]+" "+mem[q][1]+";\n");var md=RedundancyFactor.ReadModel(t.ToString());
   var fs=new StringBuilder();for(int q=0;q<mem.Count;q++){var a2=md.Joints[mem[q][0]];var b2=md.Joints[mem[q][1]];if(Math.Abs(a2.Y-b2.Y)<1e-9)continue;fs.AppendLine((q+1)+" 1 "+mem[q][0]+" 50 20 0 0 0 0");fs.AppendLine("  "+mem[q][1]+" -50 -20 0 0 0 0");}
   var rr=RedundancyFactor.Compute(md,RedundancyFactor.ReadForces(fs.ToString()),1,"1 EQ+X","X","Moment");var st=rr.Stories.Where(x=>x.Shear>0).ToList();
   Check(st.Count==1&&Math.Abs(st[0].Shear-60)<1e-6&&Math.Abs(st[0].Element-34)<1e-6&&Math.Abs(rr.RMax-34.0/60)<1e-9,"one story from the ground beams, bay found through split beams (20 + 0.7 x 20 = 34 of 60): "+string.Join(" | ",rr.Stories.Select(x=>x.Bottom+"-"+x.Top+" V "+x.Shear+" "+x.Element0)));}
  Console.WriteLine("RedundancyFactorTests passed: "+checks+" checks");}
}
