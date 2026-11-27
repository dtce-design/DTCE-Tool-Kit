using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Text;using BeamScheduleFormatter;
static class WindEdgeLoadsTests {
 static int checks;static void Check(bool ok,string what){checks++;if(!ok){Console.WriteLine("FAIL: "+what);Environment.Exit(1);}}
 /* a 30 x 24 m floor with a 6 m wide, 8 m deep recess at the middle of the Z max side (X 12 to 18, Z 16 to 24), beams on a 6 m / 8 m grid,
    floors at 0, 4 and 8 m */
 static string Model(){var ci=CultureInfo.InvariantCulture;var b=new StringBuilder("STAAD SPACE\nUNIT METER KN\nJOINT COORDINATES\n");var id=new Dictionary<string,int>();int n=0;
  double[] xs={0,6,12,18,24,30},zs={0,8,16,24},ys={0,4,8};Func<double,double,bool> exists=(x,z)=>!(z>16.5&&x>12.5&&x<17.5);
  foreach(var y in ys)foreach(var x in xs)foreach(var z in zs){if(!exists(x,z))continue;id[x+","+y+","+z]=++n;b.Append(n).Append(" ").Append(x.ToString(ci)).Append(" ").Append(y.ToString(ci)).Append(" ").Append(z.ToString(ci)).Append(";\n");}
  b.Append("MEMBER INCIDENCES\n");int m=0;Func<string,int> J=k=>id.ContainsKey(k)?id[k]:0;
  foreach(var y in ys){for(int i=0;i+1<xs.Length;i++)foreach(var z in zs){int a=J(xs[i]+","+y+","+z),c=J(xs[i+1]+","+y+","+z);if(a>0&&c>0&&!(z==24&&xs[i]>=12&&xs[i+1]<=18))b.Append(++m).Append(" ").Append(a).Append(" ").Append(c).Append(";\n");}
   for(int j=0;j+1<zs.Length;j++)foreach(var x in xs){int a=J(x+","+y+","+zs[j]),c=J(x+","+y+","+zs[j+1]);if(a>0&&c>0)b.Append(++m).Append(" ").Append(a).Append(" ").Append(c).Append(";\n");}}
  foreach(var x in xs)foreach(var z in zs){for(int k=0;k+1<ys.Length;k++){int a=J(x+","+ys[k]+","+z),c=J(x+","+ys[k+1]+","+z);if(a>0&&c>0)b.Append(++m).Append(" ").Append(a).Append(" ").Append(c).Append(";\n");}}
  b.Append("SUPPORTS\n");return b.ToString();}
 static void Main(){
  var t=new WindFaceTypes{Travel="+X",Ww=1,Lw=2,Sw=3,Top=8,Leeward=1.0,Side=2.0};t.WwInt.AddRange(new[]{3.0,3.5});t.WwHeig.AddRange(new[]{4.0,8.0});
  string lines=WindEdgeLoads.WindLoadLines(t,0);
  Check(lines.Contains("WIND LOAD X 1 TYPE 1 YR 0 8")&&lines.Contains("WIND LOAD -X -1 TYPE 2 YR 0 8")&&lines.Contains("WIND LOAD Z -1 TYPE 3 YR 0 8")&&lines.Contains("WIND LOAD -Z -1 TYPE 3 YR 0 8"),"WIND LOAD lines: windward, leeward and both side walls pulled outward");
  var tz=new WindFaceTypes{Travel="-Z",Ww=10,Lw=11,Sw=12,Top=8};string lz=WindEdgeLoads.WindLoadLines(tz,0);Check(lz.StartsWith("WIND LOAD -Z 1 TYPE 10")&&lz.Contains("WIND LOAD Z -1 TYPE 11")&&lz.Contains("WIND LOAD X -1 TYPE 12")&&lz.Contains("WIND LOAD -X -1 TYPE 12"),"-Z case");
  Check(Math.Abs(t.WindwardAt(2)-3.0)<1e-9&&Math.Abs(t.WindwardAt(6)-3.5)<1e-9&&Math.Abs(t.WindwardAt(9)-3.5)<1e-9,"windward intensity by HEIG band");
  var back=WindFaceTypes.Unpack("+X",t.Pack());Check(back!=null&&back.Sw==3&&back.WwInt.Count==2&&Math.Abs(back.WwHeig[1]-8)<1e-9,"link round trip");
  var model=RedundancyFactor.ReadModel(Model());string note;var faces=WindEdgeLoads.Faces(model,0,8,out note);
  Check(faces.Count>0,"outline faces found");
  /* the recess side walls: X = 12 (normal +X) and X = 18 (normal -X), Z 16 to 24, are shielded along X; its back wall Z = 16 (normal +Z) is not */
  Check(faces.Any(f=>f.Normal=="-X"&&Math.Abs(f.At-18)<1e-6&&Math.Abs(f.A1-16)<1e-6&&Math.Abs(f.A2-24)<1e-6&&f.Shielded),"recess wall X = 18 shielded");
  Check(faces.Any(f=>f.Normal=="+X"&&Math.Abs(f.At-12)<1e-6&&f.Shielded),"recess wall X = 12 shielded");
  Check(faces.Any(f=>f.Normal=="+Z"&&Math.Abs(f.At-16)<1e-6&&!f.Shielded),"recess back wall reached by the wind");
  Check(faces.Where(f=>f.Normal=="-X"&&Math.Abs(f.At)<1e-6).All(f=>!f.Shielded),"outer windward wall not shielded");
  /* bands: level 0 carries 0-2 m, level 4 carries 2-6 m, level 8 carries 6-8 m */
  Check(faces.Any(f=>Math.Abs(f.Level-4)<1e-6&&Math.Abs(f.Bottom-2)<1e-6&&Math.Abs(f.TopBand-6)<1e-6),"tributary band of a middle level");
  int count;string loads=WindEdgeLoads.RecessLoads(model,faces,t,out count);
  /* wind +X: X = 18 wall is windward (3.5 x 4 = 14 kN/m at level 4, pushing +X); X = 12 wall is leeward (1.0 x 4 = 4 kN/m pulling +X) */
  Check(count==2&&loads.Contains("WIND LOAD X 1 TYPE 1 XR 17.95 18.05 ZR 15.95 24.05 YR -0.05 8.05")&&loads.Contains("WIND LOAD -X -1 TYPE 2 XR 11.95 12.05 ZR 15.95 24.05 YR -0.05 8.05")&&!loads.Contains("MEMBER LOAD"),"recess walls as WIND LOAD ranges for +X (X = 18 windward WW, X = 12 leeward LW): "+loads.Replace("\n"," / "));
  Check(!loads.Contains(" ZR 15.95 16.05"),"no extra load on the recess back wall (WIND LOAD reaches it)");
  var tm=new WindFaceTypes{Travel="-X",Ww=4,Lw=5,Sw=6,Top=8,Leeward=1.0,Side=2.0};tm.WwInt.AddRange(new[]{3.0,3.5});tm.WwHeig.AddRange(new[]{4.0,8.0});
  string lm=WindEdgeLoads.RecessLoads(model,faces,tm,out count);Check(lm.Contains("WIND LOAD -X 1 TYPE 4 XR 11.95 12.05")&&lm.Contains("WIND LOAD X -1 TYPE 5 XR 17.95 18.05"),"recess walls for -X");
  {var tz2=new WindFaceTypes{Travel="-Z",Ww=10,Lw=11,Sw=12,Top=8,Leeward=1,Side=2};tz2.WwInt.Add(3);tz2.WwHeig.Add(8);string ls=WindEdgeLoads.RecessLoads(model,faces,tz2,out count);
   Check(ls.Contains("WIND LOAD -X -1 TYPE 12 XR 11.95 12.05")&&ls.Contains("WIND LOAD X -1 TYPE 12 XR 17.95 18.05")&&ls.Contains("side wall"),"recess walls along the wind take the SW type, pulled out: "+ls.Replace("\n"," / "));}
  var link=new Dictionary<string,string>{{"+X",t.Pack()},{"-X",tm.Pack()}};var texts=WindEdgeLoads.CaseTexts(link,Model(),out note);
  Check(texts.ContainsKey("+X")&&texts["+X"].StartsWith("* NSCP 2015 Fig. 207B.4-8 Case 1")&&texts["+X"].Contains("\nWIND LOAD X 1 TYPE 1")&&texts["+X"].Contains("TYPE 1 XR 17.95")&&note.Contains("recessed walls"),"case text with the recess loads");
  Check(StaadCombinations.WindKey("WL+X")=="+X"&&StaadCombinations.WindKey("WL-Z")=="-Z"&&StaadCombinations.WindKey("DL")=="","wind case key from the title");
  var s=new StaadCombinationSettings{AutoPrimary=true};s.WindText["+X"]=texts["+X"];var prim=StaadCombinations.Parse("LOAD 1 LOADTYPE WIND TITLE WL+X\nLOAD 2 LOADTYPE WIND TITLE WL-X");
  string sec=StaadCombinations.PrimarySection(prim,s);Check(sec.Contains("TITLE WL+X\n* NSCP 2015 Fig. 207B.4-8 Case 1")&&sec.Contains("\nWIND LOAD X 1 TYPE 1")&&sec.Contains("TITLE WL-X\n* add the wind loads here"),"primary section carries the wind lines");
  DesignLoadsLink.Set("wind-cases-test",new Dictionary<string,string>{{"+X","LOAD 9 LOADTYPE WIND TITLE WL+X\nWIND LOAD X 1 TYPE 1 YR 0 8"}});var back2=DesignLoadsLink.Get("wind-cases-test");Check(back2!=null&&back2["+X"]=="LOAD 9 LOADTYPE WIND TITLE WL+X\nWIND LOAD X 1 TYPE 1 YR 0 8","multi-line wind case kept through the link file");
  /* the Wind sheet's types through the link file itself (keys "+X" ...) */
  DesignLoadsLink.Set("wind-staad-test",new Dictionary<string,string>{{"+X",t.Pack()},{"-X",tm.Pack()}});var viaFile=DesignLoadsLink.Get("wind-staad-test");Check(viaFile!=null&&viaFile.ContainsKey("+X")&&viaFile.ContainsKey("-X")&&WindFaceTypes.Unpack("+X",viaFile["+X"]).Sw==3,"wind types through the link file");
  /* the same model as the STAAD Nodes and Beams tables (copied from STAAD, tab separated, units in the headers) */
  {var ci=CultureInfo.InvariantCulture;var nodes=new StringBuilder("Node\tX (m)\tY (m)\tZ (m)\n");foreach(var j in model.Joints.Values.OrderBy(j=>j.No))nodes.Append(j.No+"\t"+j.X.ToString(ci)+"\t"+j.Y.ToString(ci)+"\t"+j.Z.ToString(ci)+"\n");
   var beams=new StringBuilder("Beam\tNode A\tNode B\tProperty\tβ (degrees)\n");foreach(var m in model.Members.Values.OrderBy(m=>m.No))beams.Append(m.No+"\t"+m.A+"\t"+m.B+"\t1\t0\n");
   var fromTables=RedundancyFactor.ReadTables(nodes.ToString(),beams.ToString());Check(fromTables.Joints.Count==model.Joints.Count&&fromTables.Members.Count==model.Members.Count,"model from the Nodes and Beams tables");
   string n2;var t2=WindEdgeLoads.CaseTexts(link,fromTables,out n2);Check(t2["+X"]==texts["+X"]&&t2["-X"]==texts["-X"],"tables give the same wind cases (WW / LW / SW and the recess loads) as the .std");
   var mm=new StringBuilder("Node\tX (mm)\tY (mm)\tZ (mm)\n");foreach(var j in model.Joints.Values.OrderBy(j=>j.No))mm.Append(j.No+"\t"+(j.X*1000).ToString(ci)+"\t"+(j.Y*1000).ToString(ci)+"\t"+(j.Z*1000).ToString(ci)+"\n");
   var t3=WindEdgeLoads.CaseTexts(link,RedundancyFactor.ReadTables(mm.ToString(),beams.ToString()),out n2);Check(t3["+X"]==texts["+X"],"Nodes table in mm read in metres");
   bool threw=false;try{RedundancyFactor.ReadTables(nodes.ToString(),"");}catch(Exception){threw=true;}Check(threw,"missing Beams table refused");
   /* as STAAD copies them: no header row, beams with property / material / beta / length */
   var nh=new StringBuilder();foreach(var j in model.Joints.Values.OrderBy(j=>j.No))nh.Append("  "+j.No+"\t"+j.X.ToString("0.0000",ci)+"\t"+j.Y.ToString("0.0000",ci)+"\t"+j.Z.ToString("0.0000",ci)+"\n");
   var bh=new StringBuilder();foreach(var m in model.Members.Values.OrderBy(m=>m.No))bh.Append("  "+m.No+"\t"+m.A+"\t"+m.B+"\t4\t5000PSI\t0.0\t6.000\n");
   var noHeader=RedundancyFactor.ReadTables(nh.ToString(),bh.ToString());Check(noHeader.Joints.Count==model.Joints.Count&&noHeader.Members.Count==model.Members.Count,"tables without the header row ("+noHeader.Joints.Count+" nodes, "+noHeader.Members.Count+" beams)");
   string n6;Check(WindEdgeLoads.CaseTexts(link,noHeader,out n6)["+X"]==texts["+X"],"same wind cases from headerless tables");}
  /* design wind load cases of NSCP 2015 Fig. 207B.4-8 (directional procedure) */
  {var tzp=new WindFaceTypes{Travel="+Z",Ww=7,Lw=8,Sw=9,Top=8,Leeward=1.5,Side=2};tzp.WwInt.Add(2.5);tzp.WwHeig.Add(8);var tzm=new WindFaceTypes{Travel="-Z",Ww=10,Lw=11,Sw=12,Top=8,Leeward=1.5,Side=2};tzm.WwInt.Add(2.5);tzm.WwHeig.Add(8);
   var all=new Dictionary<string,string>{{"+X",t.Pack()},{"-X",tm.Pack()},{"+Z",tzp.Pack()},{"-Z",tzm.Pack()}};string n4;var cases=WindEdgeLoads.CaseTexts(all,model,out n4,true,true);
   Check(cases.Count==24&&cases.ContainsKey("2+X+T")&&cases.ContainsKey("3-X+Z")&&cases.ContainsKey("4+X-Z-T"),"24 wind cases: "+string.Join(",",cases.Keys));
   Check(cases["2+X+T"].Contains("WIND LOAD X 0.75 TYPE 1")&&cases["2+X+T"].Contains("WIND LOAD -X -0.75 TYPE 2")&&!cases["2+X+T"].Contains("TYPE 3"),"Case 2: 75 % windward / leeward, no side walls");
   Check(cases["3+X+Z"].Contains("WIND LOAD X 0.75 TYPE 1")&&cases["3+X+Z"].Contains("WIND LOAD Z 0.75 TYPE 7")&&!cases["3+X+Z"].Contains("JOINT LOAD"),"Case 3: both axes at 75 %, no torsion");
   Check(cases["4+X+Z+T"].Contains("WIND LOAD X 0.563 TYPE 1")&&cases["4+X+Z+T"].Contains("WIND LOAD Z 0.563 TYPE 7")&&cases["4+X+Z+T"].Contains("JOINT LOAD"),"Case 4: 56.3 % both axes with torsion");
   /* torsion at level 4 (band 2-6 m, h = 4): plan 30 x 24; wind +X: B = 24, PW at 4 m = 3.0, PL = 1.0 -> MT = 0.75 x 4 x 24 x 4 x 0.15 x 24 = 1036.8 */
   string nn;var plans=WindEdgeLoads.Plans(model,0,8,out nn);var tl=WindEdgeLoads.Torsion(model,plans,new List<WindFaceTypes>{t},0.75,0);var l4=tl.First(x=>Math.Abs(x.Y-4)<1e-6);
   Check(Math.Abs(l4.Mt-1036.8)<1e-6&&Math.Abs(l4.D-24)<1e-6,"torsion MT per level: "+l4.Mt+" kN-m over "+l4.D+" m");
   /* v10.0.40 (owner: "e irregular yung shape ng building"): the torsion is spread over the outline joints of each level, F = MT r / sum r2:
      no net force and sum (z FX - x FZ) = -MT for -T at every level */
   var m2=System.Text.RegularExpressions.Regex.Matches(cases["2+X-T"],@"\n(\d+)(?: FX (-?[0-9.]+))?(?: FZ (-?[0-9.]+))?(?=\n|$)");double sx=0,sz=0,mom=0;int jl=0;
   foreach(System.Text.RegularExpressions.Match mm in m2){if(!mm.Groups[2].Success&&!mm.Groups[3].Success)continue;jl++;var jj=model.Joints[int.Parse(mm.Groups[1].Value)];double fx=mm.Groups[2].Success?double.Parse(mm.Groups[2].Value,CultureInfo.InvariantCulture):0,fz=mm.Groups[3].Success?double.Parse(mm.Groups[3].Value,CultureInfo.InvariantCulture):0;sx+=fx;sz+=fz;mom+=jj.Z*fx-jj.X*fz;}
   double mtAll=tl.Sum(x=>x.Mt);Check(jl>4&&Math.Abs(sx)<0.05&&Math.Abs(sz)<0.05&&Math.Abs(mom+mtAll)<0.5,"-T spread over the outline joints: no net force ("+sx+", "+sz+"), MY = -MT ("+mom+" / "+mtAll+", "+jl+" joint loads)");
   Check(tl.All(x=>x.Edge.Count>=3&&x.SumR2>0),"every level has its outline joints");
   string ex;var noT=WindEdgeLoads.CaseTexts(all,model,out ex,true,false);Check(noT.Count==8&&!noT.Keys.Any(k=>k.StartsWith("2")||k.StartsWith("4")),"exempt: Cases 1 and 3 only");
   Check(StaadCombinations.WindKey("WL2+X+T")=="2+X+T"&&StaadCombinations.WindKey("WL3-X+Z")=="3-X+Z"&&StaadCombinations.WindKey("WL4+X-Z-T")=="4+X-Z-T"&&StaadCombinations.WindKey("WL+X")=="+X","case keys from the older titles");
   Check(StaadCombinations.WindKey("WL+X CASE 1")=="+X"&&StaadCombinations.WindKey("WL+X CASE 1 -GCPI")=="1B+X"&&StaadCombinations.WindKey("WL-Z CASE 2 +T")=="2-Z+T"&&StaadCombinations.WindKey("WL+X-Z CASE 3")=="3+X-Z"&&StaadCombinations.WindKey("WL-X+Z CASE 4 -T")=="4-X+Z-T","case keys from the titles with the case");
   var ap=StaadCombinations.Parse(StaadCombinations.AutoPrimary("A",1,false,true,true,true));Check(ap.Count(p=>p.Kind=="W")==24&&ap.Any(p=>p.Title=="WL-X+Z CASE 4 -T")&&ap.Any(p=>p.Title=="WL+X CASE 1"),"automatic primaries carry the 24 wind cases");
   Check(StaadCombinations.Parse(StaadCombinations.AutoPrimary("A",1,false,true,true,false)).Count(p=>p.Kind=="W")==8,"exempt: 8 wind primaries");}
  /* roof data through the link and the -GCpi twin of Case 1 */
  {var r=new WindFaceTypes{Travel="+X",Ww=1,Lw=2,Sw=3,Top=8,Leeward=1,Side=2,HasRoof=true,RoofNormal=false,Angle=3,OhUnder=0.9,OhWwA=-1.5,OhWwB=-0.1,OhLwA=-0.6,OhLwB=-0.1};r.WwInt.Add(3);r.WwHeig.Add(8);r.Zones.Add(new[]{0,4.0,-2,0.2,-1.5,-0.1});r.Zones.Add(new[]{4,1e6,-1,0.1,-0.6,-0.1});
   var rb=WindFaceTypes.Unpack("+X",r.Pack());Check(rb.HasRoof&&!rb.RoofNormal&&rb.Zones.Count==2&&Math.Abs(rb.Zones[1][3]-0.1)<1e-12&&Math.Abs(rb.OhUnder-0.9)<1e-12,"roof data round trip");
   Check(WindFaceTypes.Unpack("+X",t.Pack()).HasRoof==false,"older links without roof");
   string n5;var withRoof=WindEdgeLoads.CaseTexts(new Dictionary<string,string>{{"+X",r.Pack()}},model,out n5,false,true);
   Check(withRoof.ContainsKey("1B+X")&&withRoof["+X"].Contains("FLOOR LOAD")&&withRoof["1B+X"].Contains("FLOOR LOAD")&&withRoof["+X"].Contains("+GCpi")&&withRoof["1B+X"].Contains("-GCpi")&&n5.Contains("Roof:"),"Case 1 with the roof: uplift (+GCpi) and downward (-GCpi) twin");
   Check(StaadCombinations.WindKey("WL1B+X")=="1B+X","WL1B key");
   Check(StaadCombinations.Parse(StaadCombinations.AutoPrimary("A",1,false,true,true,true,true)).Count(p=>p.Kind=="W")==28,"28 wind cases with the roof twins");}
  Console.WriteLine("WindEdgeLoadsTests passed: "+checks+" checks");}
}
