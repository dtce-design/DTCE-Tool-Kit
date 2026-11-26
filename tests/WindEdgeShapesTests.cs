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
   Check(lz.Count==2&&lz.Any(l=>l.Role=="LW+Z"&&l.Text.StartsWith("WIND LOAD -Z -1 TYPE 8 ZR 5.95 6.05 XR 17.95 30.05"))&&lz.Any(l=>l.Role=="WW+Z"&&l.Text.StartsWith("WIND LOAD Z 1 TYPE 7 ZR 11.95 12.05 XR 17.95 30.05")),"notch in the X-max side, wind +Z: "+string.Join(" / ",lz.Select(l=>l.Text)));
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
  /* 8  rectangle: nothing recessed */
  {string[] p={"###","###"};Check(Lines(Model(new double[]{0,4},p,p),T("+X",1,2,3,4),4,out note).Count==0,"plain rectangle: no recess lines");}
  Console.WriteLine("WindEdgeShapesTests passed: "+checks+" checks");}
}
