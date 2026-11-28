using System;using System.Collections.Generic;using System.Linq;using System.Text;using BeamScheduleFormatter;
/* NSCP 2015 Table 208-9 / 208-10 checks from the STAAD data */
class StructuralIrregularityTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 [STAThread]static void Main(){
  var std=new StringBuilder("UNIT METER KN\nJOINT COORDINATES\n");int n=1;foreach(var y in new[]{0.0,3.5,7.0,10.5})foreach(var z in new[]{0.0,5.0})foreach(var x in new[]{0.0,6.0,12.0})std.AppendLine(n++ +" "+x+" "+y+" "+z+";");
  std.AppendLine("MEMBER INCIDENCES");int m=1;for(int q=1;q<=18;q++)std.AppendLine(m++ +" "+q+" "+(q+6)+";");foreach(var lvl in new[]{6,12,18})foreach(var row in new[]{0,3}){std.AppendLine(m++ +" "+(lvl+row+1)+" "+(lvl+row+2)+";");std.AppendLine(m++ +" "+(lvl+row+2)+" "+(lvl+row+3)+";");}
  var model=RedundancyFactor.ReadModel(std.ToString());
  /* storey shears 140 / 98 / 56 kN (columns 20 / 14 / 8, interior 1.5x); drifts 10 / 3 / 2 mm: storey 1 soft (k1 = 14 vs k2 = 32.7) */
  var f=new StringBuilder();for(int c=1;c<=18;c++){int st=(c-1)/6;double v=(st==0?20:st==1?14:8)*(((c-1)%3==1)?1.5:1);f.AppendLine(c+" 1 "+c+" 100 "+v+" 0 0 0 0");f.AppendLine("  "+(c+6)+" -100 "+(-v)+" 0 0 0 0");}
  var forces=RedundancyFactor.ReadForces(f.ToString());var d=new StringBuilder();foreach(var j in model.Joints.Values.Where(j=>j.Y>0)){double lv=j.Y==3.5?10:j.Y==7?13:15;d.AppendLine(j.No+"\t1 EQ+X\t"+lv+"\t0\t0\t0\t0\t0\t0");}
  var disp=TorsionAmplification.ReadDisplacements(d.ToString());var k=StructuralIrregularity.Stiffness(model,forces,disp,1,"X");
  Check(k.Count==3&&Math.Abs(k[0][2]-140)<1e-6&&Math.Abs(k[0][3]-10)<1e-9&&Math.Abs(k[1][3]-3)<1e-9,"storey shear and drift: "+string.Join(" | ",k.Select(x=>x[2]+"/"+x[3])));
  var eq=new List<StaadPrimary>{new StaadPrimary{No=1,Title="EQ+X",Kind="E",Axis="X",Sign=1}};
  /* owner's model (28 November 2026, "bakit fail sa soft storey sa computation pero pasado sa staad"): pedestals below the ground beams and a column
     split by a wall mesh (a joint at mid-storey) give the same three storeys; equal stiffness 20 / 20 / 20 is regular */
  {var s2=new StringBuilder("UNIT METER KN\nJOINT COORDINATES\n1 0 -1.5 0;2 6 -1.5 0;3 0 0 0;4 6 0 0;5 0 3.5 0;6 6 3.5 0;7 0 7 0;8 6 7 0;9 0 10.5 0;10 6 10.5 0;11 0 1.75 0;12 0 0.875 0;\nMEMBER INCIDENCES\n");
   s2.Append("1 1 3;2 2 4;3 3 12;4 12 11;5 11 5;6 4 6;7 5 7;8 6 8;9 7 9;10 8 10;11 3 4;12 5 6;13 7 8;14 9 10;\n");var m2=RedundancyFactor.ReadModel(s2.ToString());
   var f2=new StringBuilder();foreach(var c in new[]{new[]{1,1,3,30},new[]{2,2,4,30},new[]{3,3,12,30},new[]{4,12,11,30},new[]{5,11,5,30},new[]{6,4,6,30},new[]{7,5,7,20},new[]{8,6,8,20},new[]{9,7,9,10},new[]{10,8,10,10}}){f2.AppendLine(c[0]+" 1 "+c[1]+" 100 "+c[3]+" 0 0 0 0");f2.AppendLine("  "+c[2]+" -100 "+(-c[3])+" 0 0 0 0");}
   var d2=new StringBuilder();foreach(var j in new[]{new[]{5.0,3.0},new[]{6.0,3.0},new[]{7.0,5.0},new[]{8.0,5.0},new[]{9.0,6.0},new[]{10.0,6.0},new[]{3.0,0.0},new[]{4.0,0.0}})d2.AppendLine((int)j[0]+"\t1 EQ+X\t"+j[1]+"\t0\t0\t0\t0\t0\t0");
   var k2=StructuralIrregularity.Stiffness(m2,RedundancyFactor.ReadForces(f2.ToString()),TorsionAmplification.ReadDisplacements(d2.ToString()),1,"X");
   /* with FLOOR DIAPHRAGM the diaphragm heights are the storeys (owner: "may defined diaphragm naman ako") */
   {var m3=RedundancyFactor.ReadModel(s2.ToString()+"FLOOR DIAPHRAGM\nDIA 1 TYPE RIG HEI 3.5\nDIA 2 TYPE RIG HEI 7\nDIA 3 TYPE RIG HEI 10.5\nBASE 0\nCHECK SOFT STORY ASCE7\n");
    var k3=StructuralIrregularity.Stiffness(m3,RedundancyFactor.ReadForces(f2.ToString()),TorsionAmplification.ReadDisplacements(d2.ToString()),1,"X");
    Check(m3.Diaphragms.Count==3&&m3.DiaphragmBase==0&&k3.Count==3&&k3.All(x=>Math.Abs(x[4]-20)<1e-6),"FLOOR DIAPHRAGM heights read and used as the storeys: "+m3.Diaphragms.Count+" / "+k3.Count);}
   Check(k2.Count==3&&Math.Abs(k2[0][2]-60)<1e-6&&Math.Abs(k2[0][4]-20)<1e-6&&Math.Abs(k2[1][4]-20)<1e-6&&Math.Abs(k2[2][4]-20)<1e-6,"three storeys from the ground up, the split column cut once: "+string.Join(" | ",k2.Select(x=>x[0]+"-"+x[1]+" V "+x[2]+" k "+x[4])));}
  var rows=StructuralIrregularity.Check(model,forces,disp,eq,null,new IrregularityInput{Weights="3.5;900\n7;880\n10.5;400"});
  Check(rows.Count==10,"5 vertical + 5 plan rows");Check(rows[0].Result.StartsWith("Irregular")&&rows[0].Result.Contains("0 m"),"soft storey at the ground storey: "+rows[0].Value);
  Check(rows[1].Result=="Regular","a lighter roof is excepted (880 vs 400)");Check(rows[2].Result=="Regular","same plan every storey");Check(rows[9].Result=="Regular","beams along X / Z");
  var heavy=StructuralIrregularity.Check(model,null,null,eq,null,new IrregularityInput{Weights="3.5;500\n7;900\n10.5;400"});Check(heavy[1].Result.StartsWith("Irregular"),"900 > 150 % of 500");
  /* a setback: the top storey half as wide; a skewed beam */
  var ms=RedundancyFactor.ReadModel(std.ToString().Replace("MEMBER INCIDENCES","99 3 3.5 2.5;\nMEMBER INCIDENCES")+"99 8 99;\n");
  Check(StructuralIrregularity.NonParallel(ms)==1,"one horizontal member off the X / Z axes");
  var re=StructuralIrregularity.Check(null,null,null,eq,null,new IrregularityInput{ProjX=4,PlanX=20,ProjZ=4,PlanZ=20,Opening=60,FloorArea=100,OutOfPlane=true});
  Check(re[6].Result.StartsWith("Irregular")&&re[7].Result.StartsWith("Irregular")&&re[8].Result.StartsWith("Irregular"),"re-entrant (20 %), diaphragm (60 %), out-of-plane");
  string why;Check(StructuralIrregularity.Suggest(rows,null,out why)=="C","any irregularity: structure C");
  var reg=StructuralIrregularity.Check(null,null,null,eq,null,new IrregularityInput());var axB=new List<AxResult>{new AxResult{Levels=new List<AxLevel>{new AxLevel{Ratio=1.12}}}};var axA=new List<AxResult>{new AxResult{Levels=new List<AxLevel>{new AxLevel{Ratio=1.02}}}};
  Check(StructuralIrregularity.Suggest(reg,axB,out why)=="B"&&StructuralIrregularity.Suggest(reg,axA,out why)=="A"&&StructuralIrregularity.Suggest(reg,null,out why)=="","regular: B when dmax / davg > 1.05, A otherwise, none without displacements");
  Console.WriteLine("StructuralIrregularityTests passed: "+checks+" checks");}
}
