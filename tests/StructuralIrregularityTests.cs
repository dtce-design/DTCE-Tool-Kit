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
