using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Text;using BeamScheduleFormatter;
/* recessed walls on plan shapes other than the owner's sample: notches on any side, a winding notch, setbacks, gaps in the levels,
   separate blocks, a gap in the edge beams and skewed beams (owner, 26 November 2026: "gawin mo ito as much possible sa mga cases na hindi
   pa natin nadidiscover") */
static class WindEdgeShapesTests {
 static int checks;static void Check(bool ok,string what){checks++;if(!ok){Console.WriteLine("FAIL: "+what);Environment.Exit(1);}}
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;
 /* a model from plan masks: one mask per level (rows = Z cells from Z = 0, columns = X cells), 6 m cells, beams round every floor cell,
    columns where a corner is on two levels in a row */
 static string Model(double[] ys,params string[][] masks){var b=new StringBuilder("STAAD SPACE\nUNIT METER KN\nJOINT COORDINATES\n");var id=new Dictionary<string,int>();int n=0;
  Func<double,double,double,int> J=(x,y,z)=>{string k=x.ToString(Ci)+","+y.ToString(Ci)+","+z.ToString(Ci);int v;if(!id.TryGetValue(k,out v)){v=++n;id[k]=v;b.Append(v+" "+k.Replace(","," ")+";\n");}return v;};
  var mem=new HashSet<string>();var list=new List<int[]>();Action<int,int> M=(a,c)=>{string k=Math.Min(a,c)+"-"+Math.Max(a,c);if(mem.Add(k))list.Add(new[]{a,c});};
  var corners=new List<HashSet<string>>();
  for(int l=0;l<ys.Length;l++){var mask=masks[l];var cs=new HashSet<string>();for(int r=0;r<mask.Length;r++)for(int c=0;c<mask[r].Length;c++){if(mask[r][c]!='#')continue;double x0=c*6,z0=r*6,x1=x0+6,z1=z0+6;double y=ys[l];
     M(J(x0,y,z0),J(x1,y,z0));M(J(x1,y,z0),J(x1,y,z1));M(J(x1,y,z1),J(x0,y,z1));M(J(x0,y,z1),J(x0,y,z0));foreach(var p in new[]{x0+","+z0,x1+","+z0,x1+","+z1,x0+","+z1})cs.Add(p);}
   corners.Add(cs);}
  for(int l=0;l+1<ys.Length;l++)foreach(var p in corners[l].Intersect(corners[l+1])){var q=p.Split(',');double x=double.Parse(q[0],Ci),z=double.Parse(q[1],Ci);M(J(x,ys[l],z),J(x,ys[l+1],z));}
  b.Append("MEMBER INCIDENCES\n");int m=0;foreach(var e in list)b.Append(++m+" "+e[0]+" "+e[1]+";\n");b.Append("SUPPORTS\n");return b.ToString();}
 static WindFaceTypes T(string travel,int ww,int lw,int sw,double top){var t=new WindFaceTypes{Travel=travel,Ww=ww,Lw=lw,Sw=sw,Top=top,Leeward=1,Side=2};t.WwInt.Add(3);t.WwHeig.Add(top);return t;}
 static List<WindRecessLine> Lines(string model,WindFaceTypes t,double top,out string note){var m=RedundancyFactor.ReadModel(model);return WindEdgeLoads.RecessLines(WindEdgeLoads.Faces(m,0,top,out note),t);}
 static void Main(){string note;
  /* 1  notch in the X-max side (walls facing +Z / -Z): for wind +Z the wall Z = 12 faces the wind (WW), Z = 6 faces away (LW) */
  {string[] p={"#####","###..","#####"};var md=Model(new double[]{0,4,8},p,p,p);
   var lz=Lines(md,T("+Z",7,8,9,8),8,out note);
   Check(lz.Count==2&&lz.Any(l=>l.Role=="LW+Z"&&l.Text.StartsWith("WIND LOAD -Z 1 TYPE 8 ZR 5.95 6.05 XR 17.95 30.05"))&&lz.Any(l=>l.Role=="WW+Z"&&l.Text.StartsWith("WIND LOAD Z 1 TYPE 7 ZR 11.95 12.05 XR 17.95 30.05")),"notch in the X-max side, wind +Z: "+string.Join(" / ",lz.Select(l=>l.Text)));
   var lx=Lines(md,T("+X",1,2,3,8),8,out note);Check(lx.Count==2&&lx.All(l=>l.Role=="SW+X"),"same notch, wind +X: its walls are side walls");
   Check(lx.All(l=>l.Text.EndsWith("YR -0.05 8.05")),"all levels: from the base to the top");}
  /* 2  winding notch: open at the top-right, turning left inside - the turned cell is outside (not floor) */
  {/* rows from Z = 0: the notch enters at X 12-18 from Z = 24 and turns to X 6-12 at Z 6-12 */
   string[] q={"#####","#..##","##.##","##.##"};var m=RedundancyFactor.ReadModel(Model(new double[]{0,4},q,q));var plans=WindEdgeLoads.FloorPlans(m);
   Check(Math.Abs(plans[0].Area-(20-4)*36)<1e-6,"winding notch open to the outside is not floor (area "+plans[0].Area+")");
   var md=Model(new double[]{0,4},q,q);var lz=Lines(md,T("-Z",10,11,12,4),4,out note);Check(lz.Any(l=>l.Face.Normal=="+Z"&&Math.Abs(l.Face.At-6)<1e-6),"winding notch: its far wall Z = 6 is loaded for wind -Z");}
  /* 3  setback: the notch only on the upper levels (podium below) - the range starts at the podium roof */
  {string[] full={"#####","#####","#####"},u={"#####","##.##","##.##"};var md=Model(new double[]{0,4,8,12},full,full,u,u);
   var lx=Lines(md,T("+X",1,2,3,12),12,out note);Check(lx.Count==2&&lx.All(l=>l.Text.EndsWith("YR 3.95 12.05")),"setback notch: YR from the level below the first notched level to the top: "+string.Join(" / ",lx.Select(l=>l.Text)));}
  /* 4  notch on levels 4 and 12 only: one line per run of levels */
  {string[] full={"#####","#####","#####"},u={"#####","##.##","##.##"};var md=Model(new double[]{0,4,8,12},full,u,full,u);
   var lx=Lines(md,T("+X",1,2,3,12),12,out note);Check(lx.Count==4&&lx.Count(l=>l.Text.EndsWith("YR -0.05 4.05"))==2&&lx.Count(l=>l.Text.EndsWith("YR 7.95 12.05"))==2,"levels with a gap between them get a line each: "+string.Join(" / ",lx.Select(l=>l.Text)));}
  /* 5  two separate blocks: the facing walls of the gap are loaded */
  {string[] p={"##..##","##..##"};var md=Model(new double[]{0,4},p,p);var lx=Lines(md,T("+X",1,2,3,4),4,out note);
   Check(lx.Count==2&&lx.Any(l=>l.Role=="LW+X"&&l.Text.Contains("XR 11.95 12.05"))&&lx.Any(l=>l.Role=="WW+X"&&l.Text.Contains("XR 23.95 24.05")),"separate blocks: gap walls");}
  /* 6  a gap in the edge beams (a missing ground beam): the outline still comes from the four-direction test, with a note */
  {string[] p={"#####","##.##","##.##"};string md=Model(new double[]{0,4},p,p);var m=RedundancyFactor.ReadModel(md);
   /* remove the outer beam X 0-6 at Z = 0, level 0 */
   int gone=m.Members.Values.First(x=>{var a=m.Joints[x.A];var c=m.Joints[x.B];return a.Y==0&&c.Y==0&&a.Z==0&&c.Z==0&&Math.Min(a.X,c.X)==0&&Math.Max(a.X,c.X)==6;}).No;m.Members.Remove(gone);
   var faces=WindEdgeLoads.Faces(m,0,4,out note);var lx=WindEdgeLoads.RecessLines(faces,T("+X",1,2,3,4));Check(lx.Count==2,"gap in an edge beam: notch walls still found ("+lx.Count+")");}
  /* 7  skewed beams: noted, not loaded by a stepped range */
  {var m=RedundancyFactor.ReadModel("STAAD SPACE\nUNIT METER KN\nJOINT COORDINATES\n1 0 0 0; 2 12 0 0; 3 18 0 6; 4 12 0 12; 5 0 0 12;\n6 0 4 0; 7 12 4 0; 8 18 4 6; 9 12 4 12; 10 0 4 12;\nMEMBER INCIDENCES\n1 1 2; 2 2 3; 3 3 4; 4 4 5; 5 5 1; 6 6 7; 7 7 8; 8 8 9; 9 9 10; 10 10 6;\n11 1 6; 12 2 7; 13 3 8; 14 4 9; 15 5 10;\nSUPPORTS\n");
   var faces=WindEdgeLoads.Faces(m,0,4,out note);Check(note.Contains("skewed"),"skewed beams noted: "+note);Check(WindEdgeLoads.RecessLines(faces,T("+X",1,2,3,4)).Count==0,"no recess lines from the skewed edge");}
  /* 9  the notch is deeper on the upper levels: each storey uses the outline at its top level - different ZR and YR, no line for the
        ground beams at the base */
  {string[] shallow={"#####","#####","##.##"},deep={"#####","##.##","##.##"};var md=Model(new double[]{0,4,8,12},shallow,shallow,deep,deep);
   var lx=Lines(md,T("+X",1,2,3,12),12,out note);
   Check(lx.Count==4&&lx.Count(l=>l.Text.Contains("ZR 11.95 18.05 YR -0.05 4.05"))==2&&lx.Count(l=>l.Text.Contains("ZR 5.95 18.05 YR 3.95 12.05"))==2&&!lx.Any(l=>l.Text.EndsWith("YR -0.05 0.05")),"outline changing with height: "+string.Join(" / ",lx.Select(l=>l.Text)));
   var figs=WindCaseFigure.Build(RedundancyFactor.ReadModel(md),new Dictionary<string,string>{{"+X",T("+X",1,2,3,12).Pack()}},null);
   Check(figs.Count==2&&figs[0].Levels.Select(l=>l.Y).SequenceEqual(new[]{4.0})&&figs[1].Levels.Select(l=>l.Y).SequenceEqual(new[]{8.0,12.0})&&figs.All(f=>f.Lines.Count==2),"one figure per outline, ground beams not drawn");}
  /* 10 roof: gable trusses spanning X (0-12 m, eave 4 m, apex 6 m) at Z = 0, 6, 12, top chords overhanging 1 m beyond the columns, wind +X
        normal to the ridge: windward slope WrA, leeward LrA, overhangs top-surface pressure, the windward one minus 0.8 qh G */
  {var b=new StringBuilder("STAAD SPACE\nUNIT METER KN\nJOINT COORDINATES\n");int jn=0;var J=new Dictionary<string,int>();Func<double,double,double,int> P=(x,y,z)=>{string kk=x+","+y+","+z;int v;if(!J.TryGetValue(kk,out v)){v=++jn;J[kk]=v;b.Append(v+" "+x.ToString(Ci)+" "+y.ToString(Ci)+" "+z.ToString(Ci)+";\n");}return v;};
   var mm=new List<int[]>();foreach(var z in new[]{0.0,6,12}){mm.Add(new[]{P(0,0,z),P(0,4,z)});mm.Add(new[]{P(12,0,z),P(12,4,z)});mm.Add(new[]{P(0,4,z),P(12,4,z)});
    mm.Add(new[]{P(-1,4-1.0/3,z),P(0,4,z)});mm.Add(new[]{P(0,4,z),P(6,6,z)});mm.Add(new[]{P(6,6,z),P(12,4,z)});mm.Add(new[]{P(12,4,z),P(13,4-1.0/3,z)});mm.Add(new[]{P(6,4,z),P(6,6,z)});mm.Add(new[]{P(3,4,z),P(6,6,z)});}
   foreach(var x in new[]{0.0,12})foreach(var z in new[]{0.0,6}){mm.Add(new[]{P(x,4,z),P(x,4,z+6)});mm.Add(new[]{P(x,0,z),P(x,0,z+6)});}foreach(var z in new[]{0.0,12}){mm.Add(new[]{P(0,0,z),P(12,0,z)});}
   b.Append("MEMBER INCIDENCES\n");for(int i=0;i<mm.Count;i++)b.Append((i+1)+" "+mm[i][0]+" "+mm[i][1]+";\n");b.Append("SUPPORTS\n");var model=RedundancyFactor.ReadModel(b.ToString());
   var t=T("+X",1,2,3,6);t.HasRoof=true;t.RoofNormal=true;t.Angle=18.4;t.WrA=-1.0;t.WrB=0.4;t.LrA=-1.2;t.LrB=-0.3;t.OhWwA=-0.8;t.OhWwB=0.6;t.OhLwA=-1.0;t.OhLwB=-0.5;t.OhUnder=0.9;
   string rn;var plans=WindEdgeLoads.Plans(model,0,6,out rn);var roof=WindEdgeLoads.RoofText(model,plans,t,true,0,out rn);
   Func<int,int,int> no=(a2,b2)=>mm.FindIndex(q=>q[0]==a2&&q[1]==b2)+1;
   int ww=no(P(0,4,6),P(6,6,6)),lw=no(P(6,6,6),P(12,4,6)),ohw=no(P(-1,4-1.0/3,6),P(0,4,6)),ohl=no(P(12,4,6),P(13,4-1.0/3,6)),web=no(P(3,4,6),P(6,6,6));
   /* middle truss: spacing 6 -> windward 1.0 x 6 = 6 up, leeward 1.2 x 6 = 7.2 up */
   Check(roof.Contains("\n"+ww+" UNI Y 6\n")&&roof.Contains("\n"+lw+" UNI Y 7.2\n"),"truss top chords: windward / leeward slope: "+roof.Replace("\n"," / "));
   Check(!roof.Contains("\n"+web+" UNI"),"webs not loaded");
   /* windward overhang: (-0.8 - 0.9) - (-1.0) = -0.7 extra over the whole member -> 4.2 up; leeward overhang: -1.0 - (-1.2) = +0.2 -> -1.2 */
   Check(roof.Contains("\n"+ohw+" UNI Y 6\n")&&roof.Contains("\n"+ohw+" UNI Y 4.2 0 1.054")&&roof.Contains("\n"+ohl+" UNI Y -1.2 0 1.054"),"overhang chords: windward underside 0.8 qh G, leeward top only");
   Check(!roof.Contains("FLOOR LOAD"),"the ceiling under the trusses takes no roof floor load");
   var end=no(P(0,4,0),P(6,6,0));Check(roof.Contains("\n"+end+" UNI Y 3\n"),"end truss: half spacing");}
  /* 11 flat slab roof with a cantilever edge: the edge without columns is an overhang */
  {string[] p={"###","###"};var md=Model(new double[]{0,4},p,p);var m=RedundancyFactor.ReadModel(md);
   /* cantilever: beams at level 4 from X 18 to 21 (no column at X 21) */
   int top=m.Joints.Values.Max(j=>j.No);var j1=m.Joints.Values.First(j=>j.X==18&&j.Y==4&&j.Z==0);var j2=m.Joints.Values.First(j=>j.X==18&&j.Y==4&&j.Z==6);var j3=m.Joints.Values.First(j=>j.X==18&&j.Y==4&&j.Z==12);
   m.Joints[top+1]=new StaadJoint{No=top+1,X=21,Y=4,Z=0};m.Joints[top+2]=new StaadJoint{No=top+2,X=21,Y=4,Z=6};m.Joints[top+3]=new StaadJoint{No=top+3,X=21,Y=4,Z=12};int mn=m.Members.Keys.Max();
   foreach(var pr in new[]{new[]{j1.No,top+1},new[]{j2.No,top+2},new[]{j3.No,top+3},new[]{top+1,top+2},new[]{top+2,top+3}})m.Members[++mn]=new StaadMember{No=mn,A=pr[0],B=pr[1]};
   var t=T("-X",4,5,6,4);t.HasRoof=true;t.Zones.Add(new[]{0,2.0,-2,0.2,-1.5,-0.1});t.Zones.Add(new[]{2,1e6,-1,0.1,-0.6,-0.1});t.OhWwA=-1.5;t.OhWwB=-0.1;t.OhLwA=-0.6;t.OhLwB=-0.1;t.OhUnder=0.9;
   string rn;var plans=WindEdgeLoads.Plans(m,0,4,out rn);var roof=WindEdgeLoads.RoofText(m,plans,t,true,0,out rn);
   /* wind -X: the cantilever X 18-21 is the windward edge: zone 1 top -1.5 - 0.9 = -2.4 -> FLOAD 2.4; the rest -1 beyond 2 m, -2 within */
   Check(roof.Contains("FLOAD 2.4 XRANGE 17.99 21.01")&&rn.Contains("1 overhang")||roof.Contains("FLOAD 2.4 XRANGE 17.99 21.01")&&rn.Contains("overhang"),"cantilever slab edge as overhang (windward): "+roof.Replace("\n"," / ")+" "+rn);
   Check(roof.Contains("FLOAD 1 XRANGE"),"interior roof cells in zone 2");}
  /* 12 roofs at different levels: a small upper roof (one cell, under 20 % of the floor beams) over a 2 x 6 roof slab */
  {string[] floor={"######","######"};var md=Model(new double[]{0,4,8},floor,floor,new[]{"#.....","......"});var m=RedundancyFactor.ReadModel(md);
   var t=T("+X",1,2,3,8);t.HasRoof=true;t.Zones.Add(new[]{0,1e6,-1,0.1,-0.6,-0.1});t.OhUnder=0.9;string rn;var plans=WindEdgeLoads.Plans(m,0,8,out rn);var roof=WindEdgeLoads.RoofText(m,plans,t,true,0,out rn);
   Check(roof.Contains("YRANGE 7.95 8.05 FLOAD 1 XRANGE -0.01 6.01 ZRANGE -0.01 6.01 GY"),"the small upper roof is loaded: "+roof.Replace("\n"," / "));
   Check(!System.Text.RegularExpressions.Regex.IsMatch(roof,@"YRANGE 3\.95 4\.05 FLOAD 1 XRANGE -0\.01 \S+ ZRANGE -0\.01 6\.01")&&roof.Contains("YRANGE 3.95 4.05"),"the slab under it is not roof there");}
  /* 8  rectangle: nothing recessed */
  {string[] p={"###","###"};Check(Lines(Model(new double[]{0,4},p,p),T("+X",1,2,3,4),4,out note).Count==0,"plain rectangle: no recess lines");}
  /* 9  a canopy of sloped members beyond the outline (owner's STAAD run, 27 November 2026: "WARNING-fx fy fz wind forces ... ignored"):
     the face lines on that side stop at the outline, the other sides keep the whole-face line */
  {string[] p={"##"};string md=Model(new double[]{0,4},p,p);var ids=new Dictionary<string,int>();foreach(System.Text.RegularExpressions.Match mt in System.Text.RegularExpressions.Regex.Matches(md,@"(?m)^(\d+) (\S+) (\S+) (\S+);"))ids[mt.Groups[2].Value+","+mt.Groups[3].Value+","+mt.Groups[4].Value]=int.Parse(mt.Groups[1].Value);
   var extraJ=new StringBuilder();var extraM=new StringBuilder();int jn=900,mn=900;foreach(var x in new[]{0,6,12}){extraJ.Append(++jn+" "+x+" 4.3 8;\n");extraM.Append(++mn+" "+ids[x+",4,6"]+" "+jn+";\n");}
   extraM.Append(++mn+" 901 902;\n"+(++mn)+" 902 903;\n");md=md.Replace("MEMBER INCIDENCES\n",extraJ+"MEMBER INCIDENCES\n").Replace("SUPPORTS\n",extraM+"SUPPORTS\n");
   var m=RedundancyFactor.ReadModel(md);var faces=WindEdgeLoads.Faces(m,0,4.3,out note);
   string cut=WindEdgeLoads.CutFaceLines(m,faces,"WIND LOAD -Z 1 TYPE 3 YR 0 4.3\nWIND LOAD Z 1 TYPE 1 YR 0 4.3\nWIND LOAD X 1 TYPE 2 YR 0 4.3");
   Check(cut.Contains("WIND LOAD -Z 1 TYPE 3 ZR -1 6.05 XR -0.05 12.05 YR 0 4.3"),"+Z faces cut at the outline Z = 6, the canopy to Z = 8 left out:\n"+cut);
   Check(cut.Contains("\nWIND LOAD Z 1 TYPE 1 YR 0 4.3\nWIND LOAD X 1 TYPE 2 YR 0 4.3")&&cut.Split('\n').Length==3,"faces with nothing beyond them keep the whole-face line");
   string plain=Model(new double[]{0,4},p,p);var m0=RedundancyFactor.ReadModel(plain);Check(WindEdgeLoads.CutFaceLines(m0,WindEdgeLoads.Faces(m0,0,4,out note),"WIND LOAD -Z 1 TYPE 3 YR 0 4")=="WIND LOAD -Z 1 TYPE 3 YR 0 4","no canopy: line unchanged");}
  /* 9b  a cantilever balcony beyond the face (no columns under it): its stretch is left out, the walls keep theirs */
  {string[] p={"##"};string md=Model(new double[]{0,4},p,p);var ids=new Dictionary<string,int>();foreach(System.Text.RegularExpressions.Match mt in System.Text.RegularExpressions.Regex.Matches(md,@"(?m)^(\d+) (\S+) (\S+) (\S+);"))ids[mt.Groups[2].Value+","+mt.Groups[3].Value+","+mt.Groups[4].Value]=int.Parse(mt.Groups[1].Value);
   /* balcony X -2..0, Z 0..6 at 4 m: edge beams only; canopy members at 12 m to Z 8 so the +Z face is cut */
   var ej=new StringBuilder("901 -2 4 0;\n902 -2 4 6;\n903 0 4.3 8;\n904 6 4.3 8;\n905 12 4.3 8;\n");var em=new StringBuilder();int mn=900;
   em.Append(++mn+" "+ids["0,4,0"]+" 901;\n"+(++mn)+" 901 902;\n"+(++mn)+" 902 "+ids["0,4,6"]+";\n");foreach(var x in new[]{0,6,12})em.Append(++mn+" "+ids[x+",4,6"]+" "+(903+x/6)+";\n");
   md=md.Replace("MEMBER INCIDENCES\n",ej+"MEMBER INCIDENCES\n").Replace("SUPPORTS\n",em+"SUPPORTS\n");var m=RedundancyFactor.ReadModel(md);
   string cut=WindEdgeLoads.CutFaceLines(m,WindEdgeLoads.Faces(m,0,4.3,out note),"WIND LOAD -Z 1 TYPE 3 YR 0 4.3");
   Check(cut.Contains("ZR -1 6.05")&&!cut.Contains("XR -2.05 0.05")&&cut.Split('\n').Length==1,"balcony in line with the wall: one stretch, no balcony-only stretch:\n"+cut);
   /* a stretch with no column at all is left out */
   Check(WindEdgeLoads.CutFaceLines(RedundancyFactor.ReadModel(md.Replace("SUPPORTS\n","SUPPORTS\n")),new List<WindEdgeFace>{new WindEdgeFace{Normal="+Z",At=6,A1=-2,A2=0,Level=4,Below=0},new WindEdgeFace{Normal="+Z",At=6.5,A1=0,A2=12,Level=4,Below=0}},"WIND LOAD -Z 1 TYPE 3 YR 0 4.3").Split('\n').All(l=>!l.Contains("XR -2.05 0.05")),"no-column stretch dropped");}
  /* 10  FLOOR LOAD ranges of whole beam panels (owner's STAAD run: "NO MEMBERS LOADED FOR A FLOOR LOAD GENERATION") */
  {Func<double[][],WindPlanLevel> plan=beams=>{var pl=new WindPlanLevel{Y=4,Xs=new List<double>{0,6,12},Zs=new List<double>{0,4,8},Inside=new bool[2,2]{{true,true},{true,true}}};pl.Beams.AddRange(beams);return pl;};
   var outline=new[]{new double[]{0,0,6,0},new double[]{6,0,12,0},new double[]{12,0,12,4},new double[]{12,4,12,8},new double[]{12,8,6,8},new double[]{6,8,0,8},new double[]{0,8,0,4},new double[]{0,4,0,0}};
   var a=plan(outline.Concat(new[]{new double[]{6,0,6,4}}).ToArray());var pa=WindEdgeLoads.Panels(a);
   Check(pa.Count==1&&pa[0].Closed&&Math.Abs(pa[0].Area-96)<1e-9,"a beam stopping half way splits no panel: one 12 x 8 panel");
   int open,clash;var ba=WindEdgeLoads.PanelBoxes(a,pa,new double?[]{0.6},out open,out clash);Check(ba.Count==1&&ba[0][0]==0&&ba[0][1]==12&&ba[0][2]==0&&ba[0][3]==8,"its range is the whole panel");
   var g=plan(outline.Concat(new[]{new double[]{6,0,6,4},new double[]{6,4,6,8},new double[]{0,4,6,4},new double[]{6,4,12,4}}).ToArray());var pg=WindEdgeLoads.Panels(g);
   Check(pg.Count==4&&pg.All(x=>x.Closed&&Math.Abs(x.Area-24)<1e-9),"full grid: four 6 x 4 panels");
   var vals=pg.Select(x=>(double?)(x.X1==0&&x.Z1==0?1.0:0.75)).ToArray();var bg=WindEdgeLoads.PanelBoxes(g,pg,vals,out open,out clash);
   Check(bg.Count(x=>x[4]==1.0)==1&&bg.Where(x=>x[4]==0.75).All(x=>!(x[0]<3&&x[1]>3&&x[2]<2&&x[3]>2))&&bg.Where(x=>x[4]==0.75).Sum(x=>(x[1]-x[0])*(x[3]-x[2]))==72,"equal panels merged, never over the 1.0 kPa panel");
   var o=plan(outline.Where(x=>!(x[1]==8&&x[3]==8&&x[2]==0)).ToArray());var po=WindEdgeLoads.Panels(o);var bo=WindEdgeLoads.PanelBoxes(o,po,po.Select(x=>(double?)0.75).ToArray(),out open,out clash);
   Check(bo.Count==0&&open==1&&WindEdgeLoads.SkipNote(4,open,clash).Contains("not loaded"),"a panel with a gap in its beams is left out and noted");}
  Console.WriteLine("WindEdgeShapesTests passed: "+checks+" checks");}
}
