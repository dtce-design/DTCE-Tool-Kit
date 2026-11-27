using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Text;using BeamScheduleFormatter;
/* the roof live load case LLR on the STAAD model (v10.0.45, owner: "add roof live load based doon sa staad file, iload at mamili ng appropriate
   load value"): NSCP 2015 Table 205-3 by slope and tributary area, slab roofs by FLOOR LOAD, truss top chords by MEMBER LOAD UNI PY */
static class RoofLiveLoadsTests {
 static int checks;static void Check(bool ok,string what){checks++;if(!ok){Console.WriteLine("FAIL: "+what);Environment.Exit(1);}}
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;
 static string N(double v){return v.ToString("0.###",Ci);}
 [STAThread]static void Main(){
  /* Table 205-3 */
  Check(RoofLiveLoads.Lr(1,10)==1.0&&RoofLiveLoads.Lr(1,48)==0.75&&RoofLiveLoads.Lr(1,72)==0.6&&RoofLiveLoads.Lr(2,10)==0.75&&RoofLiveLoads.Lr(2,40)==0.7&&RoofLiveLoads.Lr(2,90)==0.6&&RoofLiveLoads.Lr(3,5)==0.6,"Table 205-3 values");
  Check(RoofLiveLoads.RowOf(0)==1&&RoofLiveLoads.RowOf(0.3)==1&&RoofLiveLoads.RowOf(4.0/12)==2&&RoofLiveLoads.RowOf(0.9)==2&&RoofLiveLoads.RowOf(1.0)==3,"slope rows: < 4 in 12, 4 to < 12 in 12, 12 in 12 and over");
  /* a 3-storey slab building, 30 x 24 m, bays 6 x 8 m (panels 48 m2), floors at 4 and 8 m, a stair roof 6 x 8 m at 11 m */
  var b=new StringBuilder("STAAD SPACE\nUNIT METER KN\nJOINT COORDINATES\n");var id=new Dictionary<string,int>();int n=0;
  Func<double,double,double,int> J=(x,y,z)=>{string k=N(x)+","+N(y)+","+N(z);int v;if(id.TryGetValue(k,out v))return v;id[k]=++n;b.Append(n+" "+N(x)+" "+N(y)+" "+N(z)+";\n");return n;};
  var mem=new List<int[]>();double[] xs={0,6,12,18,24,30},zs={0,8,16,24};
  foreach(var y in new[]{0.0,4,8}){foreach(var z in zs)for(int i=0;i+1<xs.Length;i++)mem.Add(new[]{J(xs[i],y,z),J(xs[i+1],y,z)});foreach(var x in xs)for(int j=0;j+1<zs.Length;j++)mem.Add(new[]{J(x,y,zs[j]),J(x,y,zs[j+1])});}
  foreach(var x in xs)foreach(var z in zs){mem.Add(new[]{J(x,0,z),J(x,4,z)});mem.Add(new[]{J(x,4,z),J(x,8,z)});}
  foreach(var p in new[]{new[]{0.0,0},new[]{6.0,0},new[]{6.0,8},new[]{0.0,8}})mem.Add(new[]{J(p[0],8,p[1]),J(p[0],11,p[1])});
  mem.Add(new[]{J(0,11,0),J(6,11,0)});mem.Add(new[]{J(6,11,0),J(6,11,8)});mem.Add(new[]{J(6,11,8),J(0,11,8)});mem.Add(new[]{J(0,11,8),J(0,11,0)});
  b.Append("MEMBER INCIDENCES\n");for(int i=0;i<mem.Count;i++)b.Append((i+1)+" "+mem[i][0]+" "+mem[i][1]+";\n");b.Append("SUPPORTS\n");
  var model=RedundancyFactor.ReadModel(b.ToString());string note;List<RoofLiveLoads.Region> regs;string text=RoofLiveLoads.Text(model,out note,out regs);
  Check(text.Contains("FLOOR LOAD")&&!text.Contains("MEMBER LOAD"),"slab roofs: FLOOR LOAD only ("+note+")");
  Check(text.Contains("YRANGE 7.95 8.05 FLOAD -0.75")&&!text.Contains("YRANGE 3.95 4.05"),"main roof at 8 m: 48 m2 panels, Lr = 0.75 kPa; the floor at 4 m is not a roof");
  Check(text.Contains("YRANGE 10.95 11.05 FLOAD -0.75"),"stair roof at 11 m loaded too (48 m2 panel)");
  Check(!text.Contains("XRANGE -0.01 6.01 ZRANGE -0.01 8.01 GY")||text.Contains("YRANGE 10.95 11.05 FLOAD -0.75 XRANGE -0.01 6.01 ZRANGE -0.01 8.01"),"the panel under the stair roof is covered at 8 m");
  Check(regs.All(r=>r.Kind=="Slab"&&r.Row==1)&&regs.Any(r=>Math.Abs(r.Lr-0.75)<1e-9),"regions: slab roofs, row 1");
  /* small panels: 4 x 4 m = 16 m2 -> 1.0 kPa */
  {var b2=new StringBuilder("STAAD SPACE\nUNIT METER KN\nJOINT COORDINATES\n");id.Clear();n=0;var m2=new List<int[]>();Func<double,double,double,int> K=(x,y,z)=>{string k=N(x)+","+N(y)+","+N(z);int v;if(id.TryGetValue(k,out v))return v;id[k]=++n;b2.Append(n+" "+N(x)+" "+N(y)+" "+N(z)+";\n");return n;};
   double[] g={0,4,8};foreach(var y in new[]{0.0,3}){foreach(var z in g)for(int i=0;i<2;i++)m2.Add(new[]{K(g[i],y,z),K(g[i+1],y,z)});foreach(var x in g)for(int j=0;j<2;j++)m2.Add(new[]{K(x,y,g[j]),K(x,y,g[j+1])});}
   foreach(var x in g)foreach(var z in g)m2.Add(new[]{K(x,0,z),K(x,3,z)});b2.Append("MEMBER INCIDENCES\n");for(int i=0;i<m2.Count;i++)b2.Append((i+1)+" "+m2[i][0]+" "+m2[i][1]+";\n");
   string t2=RoofLiveLoads.Text(RedundancyFactor.ReadModel(b2.ToString()),out note,out regs);Check(t2.Contains("FLOAD -1 ")&&!t2.Contains("FLOAD -0.75"),"16 m2 panels: Lr = 1.0 kPa");}
  /* a trussed roof: one storey 12 x 18 m to 4 m, trusses in X-Y planes at Z = 0, 6, 12, 18 (spacing 6 m), span 12 m, rise 1.5 m (3 in 12) */
  {var b3=new StringBuilder("STAAD SPACE\nUNIT METER KN\nJOINT COORDINATES\n");id.Clear();n=0;var m3=new List<int[]>();Func<double,double,double,int> K=(x,y,z)=>{string k=N(x)+","+N(y)+","+N(z);int v;if(id.TryGetValue(k,out v))return v;id[k]=++n;b3.Append(n+" "+N(x)+" "+N(y)+" "+N(z)+";\n");return n;};
   double[] zt={0,6,12,18};foreach(var z in zt){m3.Add(new[]{K(0,0,z),K(0,4,z)});m3.Add(new[]{K(12,0,z),K(12,4,z)});m3.Add(new[]{K(0,4,z),K(12,4,z)});/* bottom chord */
    m3.Add(new[]{K(0,4,z),K(3,4.75,z)});m3.Add(new[]{K(3,4.75,z),K(6,5.5,z)});m3.Add(new[]{K(6,5.5,z),K(9,4.75,z)});m3.Add(new[]{K(9,4.75,z),K(12,4,z)});/* top chords */
    m3.Add(new[]{K(6,4,z),K(6,5.5,z)});m3.Add(new[]{K(3,4,z),K(3,4.75,z)});m3.Add(new[]{K(9,4,z),K(9,4.75,z)});}
   for(int i=0;i+1<zt.Length;i++){m3.Add(new[]{K(0,4,zt[i]),K(0,4,zt[i+1])});m3.Add(new[]{K(12,4,zt[i]),K(12,4,zt[i+1])});m3.Add(new[]{K(6,5.5,zt[i]),K(6,5.5,zt[i+1])});}
   b3.Append("MEMBER INCIDENCES\n");for(int i=0;i<m3.Count;i++)b3.Append((i+1)+" "+m3[i][0]+" "+m3[i][1]+";\n");
   string t3=RoofLiveLoads.Text(RedundancyFactor.ReadModel(b3.ToString()),out note,out regs);
   Check(t3.Contains("MEMBER LOAD")&&t3.Contains("UNI PY"),"trusses: MEMBER LOAD UNI PY ("+note+")");
   var mid=regs.FirstOrDefault(r=>r.Kind=="Truss"&&r.Where=="level 4 m, truss Z = 6 m");var edge=regs.FirstOrDefault(r=>r.Kind=="Truss"&&r.Where=="level 4 m, truss Z = 0 m");
   Check(mid!=null&&Math.Abs(mid.Area-72)<1e-6&&mid.Row==1&&Math.Abs(mid.Lr-0.6)<1e-9,"interior truss: 12 x 6 = 72 m2, 3 in 12 (row 1): Lr = 0.6 kPa");
   Check(edge!=null&&Math.Abs(edge.Area-36)<1e-6&&Math.Abs(edge.Lr-0.75)<1e-9,"end truss: 12 x 3 = 36 m2: Lr = 0.75 kPa");
   Check(t3.Contains(" UNI PY -3.6")&&t3.Contains(" UNI PY -2.25"),"w = Lr x spacing: 0.6 x 6 = 3.6, 0.75 x 3 = 2.25 kN/m");
   Check(regs.Where(r=>r.Kind=="Truss").Count()==4&&System.Text.RegularExpressions.Regex.Matches(t3,"UNI PY").Count==16,"4 trusses x 4 top chords");
   Check(!t3.Contains("YRANGE 3.95 4.05"),"the ceiling under the trusses takes no roof load");}
  /* levels from the model (owner: "madetermine niya sana yung mga ibat ibang levels based din sa model"): a 2-storey slab block 12 x 8 m
     (floors 4, 8 m) with a stair from 4 to 8 m inside it, and a lower wing 12 x 12 m beside it (X 12..24) with trusses on its 4 m level */
  {var b4=new StringBuilder("STAAD SPACE\nUNIT METER KN\nJOINT COORDINATES\n");id.Clear();n=0;var m4=new List<int[]>();Func<double,double,double,int> K=(x,y,z)=>{string k=N(x)+","+N(y)+","+N(z);int v;if(id.TryGetValue(k,out v))return v;id[k]=++n;b4.Append(n+" "+N(x)+" "+N(y)+" "+N(z)+";\n");return n;};
   Action<double,double,double,double,double> box=(x0,x1,z0,z1,y)=>{m4.Add(new[]{K(x0,y,z0),K(x1,y,z0)});m4.Add(new[]{K(x1,y,z0),K(x1,y,z1)});m4.Add(new[]{K(x1,y,z1),K(x0,y,z1)});m4.Add(new[]{K(x0,y,z1),K(x0,y,z0)});};
   foreach(var y in new[]{0.0,4,8}){box(0,6,0,8,y);box(6,12,0,8,y);}foreach(var x in new[]{0.0,6,12})foreach(var z in new[]{0.0,8}){m4.Add(new[]{K(x,0,z),K(x,4,z)});m4.Add(new[]{K(x,4,z),K(x,8,z)});}
   /* stair inside the block: stringers from (1,4,1) to (5,8,1) and (1,4,3) to (5,8,3), landing beams at both ends */
   m4.Add(new[]{K(1,4,1),K(5,8,1)});m4.Add(new[]{K(1,4,3),K(5,8,3)});m4.Add(new[]{K(1,4,1),K(1,4,3)});m4.Add(new[]{K(5,8,1),K(5,8,3)});m4.Add(new[]{K(1,4,1),K(0,4,1)});m4.Add(new[]{K(5,8,1),K(6,8,1)});
   /* lower wing: columns to 4 m, trusses in X-Y planes at Z = 0, 6, 12 spanning X 12..24, rise 1.5 m */
   foreach(var z in new[]{0.0,6,12}){m4.Add(new[]{K(24,0,z),K(24,4,z)});if(z>8)m4.Add(new[]{K(12,0,z),K(12,4,z)});m4.Add(new[]{K(12,4,z),K(24,4,z)});
    m4.Add(new[]{K(12,4,z),K(18,5.5,z)});m4.Add(new[]{K(18,5.5,z),K(24,4,z)});m4.Add(new[]{K(18,4,z),K(18,5.5,z)});}
   m4.Add(new[]{K(24,4,0),K(24,4,6)});m4.Add(new[]{K(24,4,6),K(24,4,12)});m4.Add(new[]{K(12,4,8),K(12,4,12)});
   b4.Append("MEMBER INCIDENCES\n");for(int i=0;i<m4.Count;i++)b4.Append((i+1)+" "+m4[i][0]+" "+m4[i][1]+";\n");
   var md=RedundancyFactor.ReadModel(b4.ToString());string t4=RoofLiveLoads.Text(md,out note,out regs);
   Check(regs.Count(r=>r.Kind=="Truss")==3&&regs.Where(r=>r.Kind=="Truss").All(r=>r.Where.StartsWith("level 4 m")),"lower-wing trusses found at level 4 m ("+string.Join("; ",regs.Select(r=>r.Kind+" "+r.Where))+")");
   int stairNo=m4.FindIndex(x=>x[0]==K(1,4,1)&&x[1]==K(5,8,1))+1;
   Check(!System.Text.RegularExpressions.Regex.IsMatch(t4,"\n"+stairNo+" UNI PY"),"the stair between 4 and 8 m is not a roof");
   Check(t4.Contains("YRANGE 7.95 8.05 FLOAD")&&regs.Any(r=>r.Kind=="Slab"&&r.Where=="level 8 m"),"the block's slab roof at 8 m");}
  /* in the STAAD output: the LLR primary case carries the text */
  {var s=new StaadCombinationSettings{AutoPrimary=true,RoofLiveText="UNIT METER KN\nFLOOR LOAD\nYRANGE 7.95 8.05 FLOAD -0.75 XRANGE 0 6 ZRANGE 0 8 GY"};var cases=StaadCombinations.Parse("LOAD 1 LOADTYPE DEAD TITLE DL\nLOAD 2 LOADTYPE ROOF LIVE TITLE LLR");
   string sec=StaadCombinations.PrimarySection(cases,s);Check(sec.Contains("TITLE LLR\nUNIT METER KN\nFLOOR LOAD")&&!sec.Contains("add the roof live loads here"),"LLR case carries the roof live loads");}
  Console.WriteLine("RoofLiveLoadsTests passed: "+checks+" checks");Environment.Exit(0);}
}
