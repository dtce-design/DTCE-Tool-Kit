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
  Check(loads.Contains("MEMBER LOAD")&&loads.Contains(" UNI GX 14 0 8")&&loads.Contains(" UNI GX 4 0 8"),"recess member loads for +X: "+loads.Replace("\n"," / "));
  Check(!loads.Contains(" GZ "),"no extra load on the recess back wall (WIND LOAD reaches it)");
  var tm=new WindFaceTypes{Travel="-X",Ww=4,Lw=5,Sw=6,Top=8,Leeward=1.0,Side=2.0};tm.WwInt.AddRange(new[]{3.0,3.5});tm.WwHeig.AddRange(new[]{4.0,8.0});
  string lm=WindEdgeLoads.RecessLoads(model,faces,tm,out count);Check(lm.Contains(" UNI GX -14 0 8")&&lm.Contains(" UNI GX -4 0 8"),"recess member loads for -X");
  var link=new Dictionary<string,string>{{"+X",t.Pack()},{"-X",tm.Pack()}};var texts=WindEdgeLoads.CaseTexts(link,Model(),out note);
  Check(texts.ContainsKey("+X")&&texts["+X"].StartsWith("WIND LOAD X 1 TYPE 1")&&texts["+X"].Contains("MEMBER LOAD")&&note.Contains("member loads"),"case text with the recess loads");
  Check(StaadCombinations.WindKey("WL+X")=="+X"&&StaadCombinations.WindKey("WL-Z")=="-Z"&&StaadCombinations.WindKey("DL")=="","wind case key from the title");
  var s=new StaadCombinationSettings{AutoPrimary=true};s.WindText["+X"]=texts["+X"];var prim=StaadCombinations.Parse("LOAD 1 LOADTYPE WIND TITLE WL+X\nLOAD 2 LOADTYPE WIND TITLE WL-X");
  string sec=StaadCombinations.PrimarySection(prim,s);Check(sec.Contains("TITLE WL+X\nWIND LOAD X 1 TYPE 1")&&sec.Contains("TITLE WL-X\n* add the wind loads here"),"primary section carries the wind lines");
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
   bool threw=false;try{RedundancyFactor.ReadTables(nodes.ToString(),"");}catch(Exception){threw=true;}Check(threw,"missing Beams table refused");}
  Console.WriteLine("WindEdgeLoadsTests passed: "+checks+" checks");}
}
