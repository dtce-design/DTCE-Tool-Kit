using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Text;
namespace BeamScheduleFormatter {
/* the DEFINE WIND LOAD types of one STAAD direction of travel (+X, -X, +Z, -Z), as the Wind Load sheet numbers them */
sealed class WindFaceTypes {public string Travel="";public int Ww,Lw,Sw;public double Top,Leeward,Side;public List<double> WwInt=new List<double>(),WwHeig=new List<double>();
 /* the roof for this wind (NSCP 2015 Figure 207B.4-1, net of the internal pressure, kPa, + toward the surface): RoofNormal - wind normal to a
    ridge of 10 degrees or more: windward / leeward slope; otherwise zones from the windward edge (start, end, A, B). A = the most negative
    (uplift: Cp branch 1, +GCpi), B = the most positive (Cp branch 2, -GCpi). Overhangs (207B.4.4): top surface external only (no internal
    pressure), OhUnder = 0.8 qh G pushing up on the underside of the windward overhang. Angle = roof slope (degrees). */
 public bool HasRoof,RoofNormal;public double Angle,WrA,WrB,LrA,LrB,OhWwA,OhWwB,OhLwA,OhLwB,OhUnder;public List<double[]> Zones=new List<double[]>();
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;
 /* windward intensity at height z (the HEIG band that holds z, as STAAD reads INT / HEIG) */
 internal double WindwardAt(double z){for(int i=0;i<WwHeig.Count;i++)if(z<=WwHeig[i]+1e-9)return WwInt[i];return WwInt.Count>0?WwInt[WwInt.Count-1]:0;}
 internal string Pack(){var parts=new List<string>{Ww.ToString(Ci),Lw.ToString(Ci),Sw.ToString(Ci),Top.ToString("R",Ci),Leeward.ToString("R",Ci),Side.ToString("R",Ci),string.Join(",",WwInt.Select((v,i)=>v.ToString("R",Ci)+"@"+WwHeig[i].ToString("R",Ci)))};
  if(HasRoof)parts.Add((RoofNormal?"N":"P")+","+string.Join(",",new[]{Angle,WrA,WrB,LrA,LrB,OhWwA,OhWwB,OhLwA,OhLwB,OhUnder}.Select(v=>v.ToString("R",Ci)))+(Zones.Count>0?","+string.Join(",",Zones.Select(z=>string.Join(":",z.Select(v=>v.ToString("R",Ci))))):""));
  return string.Join("|",parts);}
 internal static WindFaceTypes Unpack(string travel,string text){try{var p=(text??"").Split('|');if(p.Length<7)return null;var t=new WindFaceTypes{Travel=travel,Ww=int.Parse(p[0],Ci),Lw=int.Parse(p[1],Ci),Sw=int.Parse(p[2],Ci),Top=double.Parse(p[3],Ci),Leeward=double.Parse(p[4],Ci),Side=double.Parse(p[5],Ci)};
   foreach(var pair in p[6].Split(new[]{','},StringSplitOptions.RemoveEmptyEntries)){var q=pair.Split('@');t.WwInt.Add(double.Parse(q[0],Ci));t.WwHeig.Add(double.Parse(q[1],Ci));}
   if(p.Length>7&&p[7].Length>2){var r=p[7].Split(',');t.HasRoof=true;t.RoofNormal=r[0]=="N";var v=r.Skip(1).Take(10).Select(x=>double.Parse(x,Ci)).ToArray();t.Angle=v[0];t.WrA=v[1];t.WrB=v[2];t.LrA=v[3];t.LrB=v[4];t.OhWwA=v[5];t.OhWwB=v[6];t.OhLwA=v[7];t.OhLwB=v[8];t.OhUnder=v[9];
    foreach(var z in r.Skip(11))t.Zones.Add(z.Split(':').Select(x=>double.Parse(x,Ci)).ToArray());}
   return t;}catch(Exception){return null;}}}
/* a face of the floor outline at one level: x / z of its two ends, the outward normal ("+X" ...) and the height band it carries */
sealed class WindEdgeFace {public double Level,Bottom,TopBand,A1,A2,At;public string Normal="";public bool Shielded;
 /* the level below (or the base), its index among the levels, the topmost level, the clear distance (m) along the normal to the next part
    of the floor (shielded faces only) and whether beams run along the whole face */
 public double Below,Free=double.PositiveInfinity;public int Index;public bool IsTop,Covered=true;
 internal string Key{get{return Normal+"|"+At.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"|"+A1.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"|"+A2.ToString("R",System.Globalization.CultureInfo.InvariantCulture);}}}
/* one recessed wall of a wind case: its WIND LOAD line (with the range), the role it has for the case (WW / LW / SW) and its levels */
sealed class WindRecessLine {public WindEdgeFace Face;public string Role="",Text="",Comment="";public double Y1,Y2;public List<double> Levels=new List<double>();}
/* the plan of one level for the wind cases and the figures: grid lines, the cells inside the floor, the beams and the outline faces */
sealed class WindPlanLevel {public double Y;public int Index;public List<double> Xs,Zs;public bool[,] Inside;public List<WindEdgeFace> Faces=new List<WindEdgeFace>();public List<double[]> Beams=new List<double[]>();
 /* levels with the same outline (faces, recesses) share one figure, whatever their inner beams */
 internal string Signature{get{var b=new StringBuilder();foreach(var f in Faces.OrderBy(f=>f.Key))b.Append(f.Key).Append(f.Shielded?"s":"o").Append(f.Covered?"":"u").Append(';');return b.ToString();}}}
/* the wind load cases of the STAAD input (owner, 26 November 2026: "automatic na ganito WIND LOAD X 1 TYPE 1 YR 0 12.6 ..."; "automatic load
   ng windload based sa edges ng building, kasi kapag may paloob sa gitna e di naloload"): each case gets the WIND LOAD lines of its windward,
   leeward and side-wall types; faces of the floor outline that the wind cannot reach along its direction (recesses in the middle of the
   building, re-entrant corners) are loaded from the pasted STAAD model as WIND LOAD lines limited by XR / ZR / YR to each recessed wall */
static class WindEdgeLoads {
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;
 static string F(double v){return Math.Round(v,3).ToString("0.###",Ci);}
 static double R(double v){return Math.Round(v,3);}
 /* STAAD's WIND LOAD direction and factor for one face (STAAD.Pro Technical Reference, Generation of Wind Loads; owner, 27 November 2026:
    "di nagtutugma yung LW at SW"): "X f" loads the faces seen from the near side - f > 0 the faces facing -X pushed toward +X, f < 0 the
    faces facing +X pushed toward -X (pressure); "-X f" loads the far side as suction - f > 0 the faces facing +X pulled toward +X, f < 0
    the faces facing -X pulled toward -X. So the minus on the axis means suction and the factor sign is the direction the load acts.
    normal = the face's outward normal ("+X", "-Z" ...), suction = pulled outward (leeward / side walls), otherwise pushed in. */
 internal static string Spec(string normal,bool suction,double magnitude){char ax=normal[1];int n=normal[0]=='+'?1:-1;int load=suction?n:-n;
  return (suction?"-":"")+ax+" "+F(load*magnitude);}
 internal static string WindLoadLines(WindFaceTypes t,double baseY){char ax=t.Travel[1];int s=t.Travel[0]=='+'?1:-1;Func<int,string> nm=k=>(k>0?"+":"-")+ax;
  string other=ax=='X'?"Z":"X";string yr=" YR "+F(baseY)+" "+F(t.Top);var b=new StringBuilder();
  /* windward: the face turned against the wind, pushed in */
  b.Append("WIND LOAD ").Append(Spec(nm(-s),false,1)).Append(" TYPE ").Append(t.Ww).Append(yr).Append("\n");
  /* leeward: the face turned away from the wind, pulled outward */
  b.Append("WIND LOAD ").Append(Spec(nm(s),true,1)).Append(" TYPE ").Append(t.Lw).Append(yr).Append("\n");
  /* side walls: both faces pulled outward */
  b.Append("WIND LOAD ").Append(Spec("+"+other,true,1)).Append(" TYPE ").Append(t.Sw).Append(yr).Append("\n");
  b.Append("WIND LOAD ").Append(Spec("-"+other,true,1)).Append(" TYPE ").Append(t.Sw).Append(yr);
  return b.ToString();}
 internal sealed class Seg {public int No;public double X1,Z1,X2,Z2,Y;public double Len{get{return Math.Sqrt((X2-X1)*(X2-X1)+(Z2-Z1)*(Z2-Z1));}}
  public bool Skewed{get{return Math.Abs(X2-X1)>1e-6&&Math.Abs(Z2-Z1)>1e-6;}}}
 /* the horizontal members of the model by level (m), levels that carry a real floor (at least 20 % of the longest level's beams) */
 internal static Dictionary<double,List<Seg>> Levels(StaadModel m){return Levels(m,0.2);}
 /* minShare 0: every level with horizontal beams (roofs of any size, owner: "kapag may different level ako ng roofing bat hindi naisasama?") */
 internal static Dictionary<double,List<Seg>> Levels(StaadModel m,double minShare){var all=new List<Seg>();double k=m.ToMetre;
  foreach(var mem in m.Members.Values){StaadJoint a,b;if(!m.Joints.TryGetValue(mem.A,out a)||!m.Joints.TryGetValue(mem.B,out b))continue;double ya=m.H(a)*k,yb=m.H(b)*k;if(Math.Abs(ya-yb)>1e-3)continue;
   var s=new Seg{No=mem.No,X1=R(m.P1(a)*k),Z1=R(m.P2(a)*k),X2=R(m.P1(b)*k),Z2=R(m.P2(b)*k),Y=R(ya)};if(s.Len>1e-6)all.Add(s);}
  var groups=all.GroupBy(s=>s.Y).ToDictionary(g=>g.Key,g=>g.ToList());if(groups.Count==0)return groups;double max=groups.Values.Max(l=>l.Sum(s=>s.Len));
  return groups.Where(g=>g.Value.Sum(s=>s.Len)>=minShare*max).ToDictionary(g=>g.Key,g=>g.Value);}
 /* does a ray from (x, z) along +X / -X / +Z / -Z cross a beam of the level? */
 static bool Hits(List<Seg> segs,double x,double z,int dx,int dz){foreach(var s in segs){
   if(dz==0){if(Math.Min(s.Z1,s.Z2)>z||Math.Max(s.Z1,s.Z2)<z||Math.Abs(s.Z2-s.Z1)<1e-9)continue;double xi=s.X1+(s.X2-s.X1)*(z-s.Z1)/(s.Z2-s.Z1);if((xi-x)*dx>0)return true;}
   else{if(Math.Min(s.X1,s.X2)>x||Math.Max(s.X1,s.X2)<x||Math.Abs(s.X2-s.X1)<1e-9)continue;double zi=s.Z1+(s.Z2-s.Z1)*(x-s.X1)/(s.X2-s.X1);if((zi-z)*dz>0)return true;}}
  return false;}
 /* is the cell side on grid line x = c (vertical = true) or z = c, from a to b, closed by a beam (along it or crossing it)? */
 static bool Closed(List<Seg> segs,bool vertical,double c,double a,double b){const double e=1e-6;foreach(var s in segs){
   double p1=vertical?s.X1:s.Z1,p2=vertical?s.X2:s.Z2,q1=vertical?s.Z1:s.X1,q2=vertical?s.Z2:s.X2;
   if(Math.Abs(p1-c)<e&&Math.Abs(p2-c)<e){if(Math.Min(Math.Max(q1,q2),b)-Math.Max(Math.Min(q1,q2),a)>e)return true;continue;}
   if(Math.Min(p1,p2)>c+e||Math.Max(p1,p2)<c-e||Math.Abs(p2-p1)<e)continue;double qi=q1+(q2-q1)*(c-p1)/(p2-p1);if(qi>a+e&&qi<b-e)return true;}
  return false;}
 /* the cells inside the floor: enclosed by beams in all four directions and not reached from outside without crossing a beam (so a
    winding notch open to the outside is not taken as floor); a level whose edge beams leave a gap (the flood reaches most of the floor)
    keeps the four-direction test alone */
 static bool[,] Inside(List<Seg> segs,List<double> xs,List<double> zs,out bool leak){int nx=xs.Count-1,nz=zs.Count-1;var ray=new bool[nx,nz];leak=false;
  for(int i=0;i<nx;i++)for(int j=0;j<nz;j++){double cx=(xs[i]+xs[i+1])/2,cz=(zs[j]+zs[j+1])/2;ray[i,j]=Hits(segs,cx,cz,1,0)&&Hits(segs,cx,cz,-1,0)&&Hits(segs,cx,cz,0,1)&&Hits(segs,cx,cz,0,-1);}
  /* flood from the padded border, cells -1..n */
  var seen=new bool[nx+2,nz+2];var q=new Queue<int[]>();q.Enqueue(new[]{-1,-1});seen[0,0]=true;
  while(q.Count>0){var c=q.Dequeue();foreach(var d in new[]{new[]{1,0},new[]{-1,0},new[]{0,1},new[]{0,-1}}){int i=c[0]+d[0],j=c[1]+d[1];if(i<-1||j<-1||i>nx||j>nz||seen[i+1,j+1])continue;
    bool closed=false;if(d[0]!=0){int line=Math.Max(c[0],i);if(j>=0&&j<nz&&line>=0&&line<=nx)closed=Closed(segs,true,xs[line],zs[j],zs[j+1]);}
    else{int line=Math.Max(c[1],j);if(i>=0&&i<nx&&line>=0&&line<=nz)closed=Closed(segs,false,zs[line],xs[i],xs[i+1]);}
    if(closed)continue;seen[i+1,j+1]=true;q.Enqueue(new[]{i,j});}}
  double all=0,reached=0;for(int i=0;i<nx;i++)for(int j=0;j<nz;j++)if(ray[i,j]){double a=(xs[i+1]-xs[i])*(zs[j+1]-zs[j]);all+=a;if(seen[i+1,j+1])reached+=a;}
  if(all>0&&reached>0.5*all){leak=true;return ray;}
  var inside=new bool[nx,nz];for(int i=0;i<nx;i++)for(int j=0;j<nz;j++)inside[i,j]=ray[i,j]&&!seen[i+1,j+1];return inside;}
 /* the floor plan of every level from its beams, for the irregularity checks */
 internal sealed class FloorPlan {public double Level;public List<double> Xs,Zs;public bool[,] Inside;
  public double Area{get{double a=0;for(int i=0;i+1<Xs.Count;i++)for(int j=0;j+1<Zs.Count;j++)if(Inside[i,j])a+=(Xs[i+1]-Xs[i])*(Zs[j+1]-Zs[j]);return a;}}}
 internal static List<FloorPlan> FloorPlans(StaadModel m){var list=new List<FloorPlan>();if(m==null||m.ZUp)return list;foreach(var lv in Levels(m).OrderBy(k=>k.Key)){var segs=lv.Value;
   var xs=segs.SelectMany(s=>new[]{s.X1,s.X2}).Distinct().OrderBy(v=>v).ToList();var zs=segs.SelectMany(s=>new[]{s.Z1,s.Z2}).Distinct().OrderBy(v=>v).ToList();if(xs.Count<2||zs.Count<2)continue;
   bool leak;list.Add(new FloorPlan{Level=lv.Key,Xs=xs,Zs=zs,Inside=Inside(segs,xs,zs,out leak)});}return list;}
 /* the plans of the levels between the base and the top: cells inside the floor, the outline faces (a cell side between an inside and an
    outside cell), shielded when the floor continues beyond the face along its outward normal; faces without beams along them (skewed
    walls cut into steps by the grid, gaps) are marked not covered and are never loaded by a range */
 internal static List<WindPlanLevel> Plans(StaadModel m,double baseY,double top,out string note){return Plans(m,baseY,top,out note,false);}
 internal static List<WindPlanLevel> Plans(StaadModel m,double baseY,double top,out string note,bool allLevels){note="";var plans=new List<WindPlanLevel>();
  if(m==null)return plans;if(m.ZUp){note="The model uses SET Z UP: recessed faces are not generated (Y up only).";return plans;}
  var levels=Levels(m,allLevels?0:0.2);var ys=levels.Keys.Where(y=>y>=baseY-1e-6&&y<=top+1e-6).OrderBy(y=>y).ToList();if(ys.Count==0){note="No floor level with beams between the base and the top of the building.";return plans;}
  var notes=new List<string>();
  for(int li=0;li<ys.Count;li++){double y=ys[li];var segs=levels[y];
   double lo=li==0?baseY:(ys[li-1]+y)/2,hi=li==ys.Count-1?top:(ys[li+1]+y)/2;
   var xs=segs.SelectMany(s=>new[]{s.X1,s.X2}).Distinct().OrderBy(v=>v).ToList();var zs=segs.SelectMany(s=>new[]{s.Z1,s.Z2}).Distinct().OrderBy(v=>v).ToList();if(xs.Count<2||zs.Count<2)continue;
   int nx=xs.Count-1,nz=zs.Count-1;bool leak;var inside=Inside(segs,xs,zs,out leak);
   if(leak)notes.Add("level "+F(y)+" m: the edge beams leave a gap, the outline is taken from the beams in four directions");
   if(segs.Any(s=>s.Skewed))notes.Add("level "+F(y)+" m has skewed beams: recessed walls along them are not generated, check them in STAAD");
   var plan=new WindPlanLevel{Y=y,Index=li,Xs=xs,Zs=zs,Inside=inside};foreach(var sg in segs)plan.Beams.Add(new[]{sg.X1,sg.Z1,sg.X2,sg.Z2});
   Func<int,int,bool> In=(i,j)=>i>=0&&j>=0&&i<nx&&j<nz&&inside[i,j];var faces=new List<WindEdgeFace>();
   for(int i=0;i<nx;i++)for(int j=0;j<nz;j++){if(!inside[i,j])continue;
    foreach(var d in new[]{new[]{-1,0},new[]{1,0},new[]{0,-1},new[]{0,1}}){if(In(i+d[0],j+d[1]))continue;
     bool shielded=false;double free=double.PositiveInfinity;for(int s=2;;s++){int ii=i+d[0]*s,jj=j+d[1]*s;if(ii<0||jj<0||ii>=nx||jj>=nz)break;if(inside[ii,jj]){shielded=true;free=d[0]!=0?Math.Abs((d[0]<0?xs[ii+1]:xs[ii])-(d[0]<0?xs[i]:xs[i+1])):Math.Abs((d[1]<0?zs[jj+1]:zs[jj])-(d[1]<0?zs[j]:zs[j+1]));break;}}
     var f=new WindEdgeFace{Level=y,Bottom=lo,TopBand=hi,Shielded=shielded,Free=free,Index=li,Below=li==0?baseY:ys[li-1],IsTop=li==ys.Count-1,Normal=(d[0]+d[1]>0?"+":"-")+(d[0]!=0?"X":"Z")};
     if(d[0]!=0){f.At=d[0]<0?xs[i]:xs[i+1];f.A1=zs[j];f.A2=zs[j+1];f.Covered=Closed(segs,true,f.At,f.A1,f.A2)&&Covers(segs,true,f.At,f.A1,f.A2);}
     else{f.At=d[1]<0?zs[j]:zs[j+1];f.A1=xs[i];f.A2=xs[i+1];f.Covered=Covers(segs,false,f.At,f.A1,f.A2);}faces.Add(f);}}
   /* join the cell sides of one straight face */
   foreach(var g in faces.GroupBy(f=>f.Normal+"|"+f.At.ToString("R",Ci)+"|"+f.Shielded+"|"+f.Covered)){WindEdgeFace cur=null;
    foreach(var f in g.OrderBy(f=>f.A1)){if(cur!=null&&Math.Abs(cur.A2-f.A1)<1e-6){cur.A2=f.A2;cur.Free=Math.Min(cur.Free,f.Free);}else{if(cur!=null)plan.Faces.Add(cur);cur=f;}}if(cur!=null)plan.Faces.Add(cur);}
   foreach(var f in plan.Faces.Where(f=>f.Shielded&&!f.Covered))notes.Add("level "+F(y)+" m: recessed wall "+f.Normal[1]+" = "+F(f.At)+" m has no beam along it, not loaded");
   plans.Add(plan);}
  note=string.Join("; ",notes.Distinct());if(note!="")note=char.ToUpperInvariant(note[0])+note.Substring(1)+".";
  return plans;}
 /* do beams lying on the line run along the whole side from a to b? */
 static bool Covers(List<Seg> segs,bool vertical,double c,double a,double b){const double e=1e-6;var spans=new List<double[]>();foreach(var s in segs){double p1=vertical?s.X1:s.Z1,p2=vertical?s.X2:s.Z2,q1=vertical?s.Z1:s.X1,q2=vertical?s.Z2:s.X2;
   if(Math.Abs(p1-c)<e&&Math.Abs(p2-c)<e)spans.Add(new[]{Math.Max(Math.Min(q1,q2),a),Math.Min(Math.Max(q1,q2),b)});}
  double at=a;foreach(var sp in spans.Where(x=>x[1]>x[0]+e).OrderBy(x=>x[0])){if(sp[0]>at+0.01)return false;at=Math.Max(at,sp[1]);}return at>=b-0.01;}
 /* ---- slab panels for FLOOR LOAD (owner's STAAD run, 27 November 2026: "NO MEMBERS LOADED FOR A FLOOR LOAD GENERATION"; "gusto ko walang
    warning") ----  STAAD's FLOOR LOAD loads closed panels only: members wholly inside the range that close a polygon. The grid cells of a
    level are joined into the real panels between the beams (across cell sides with no beam along them); a panel whose outline has a gap
    (an open edge, a void, a stair opening) cannot take a FLOOR LOAD and is left out. Each range holds whole panels of one value and never
    another closed panel, so STAAD loads exactly the panels meant. */
 internal sealed class SlabPanel {public List<int[]> Cells=new List<int[]>();public double X1=double.MaxValue,X2=double.MinValue,Z1=double.MaxValue,Z2=double.MinValue,Area;public bool Closed=true;}
 internal static List<SlabPanel> Panels(WindPlanLevel p){var list=new List<SlabPanel>();int nx=p.Xs.Count-1,nz=p.Zs.Count-1;if(nx<1||nz<1)return list;
  var segs=p.Beams.Select(b=>new Seg{X1=b[0],Z1=b[1],X2=b[2],Z2=b[3],Y=p.Y}).ToList();var id=new int[nx,nz];for(int i=0;i<nx;i++)for(int j=0;j<nz;j++)id[i,j]=-1;
  for(int i0=0;i0<nx;i0++)for(int j0=0;j0<nz;j0++){if(!p.Inside[i0,j0]||id[i0,j0]>=0)continue;var pn=new SlabPanel();var q=new Queue<int[]>();q.Enqueue(new[]{i0,j0});id[i0,j0]=list.Count;
   while(q.Count>0){var c=q.Dequeue();int i=c[0],j=c[1];pn.Cells.Add(c);pn.X1=Math.Min(pn.X1,p.Xs[i]);pn.X2=Math.Max(pn.X2,p.Xs[i+1]);pn.Z1=Math.Min(pn.Z1,p.Zs[j]);pn.Z2=Math.Max(pn.Z2,p.Zs[j+1]);pn.Area+=(p.Xs[i+1]-p.Xs[i])*(p.Zs[j+1]-p.Zs[j]);
    foreach(var d in new[]{new[]{-1,0},new[]{1,0},new[]{0,-1},new[]{0,1}}){int ii=i+d[0],jj=j+d[1];bool beam=d[0]!=0?Covers(segs,true,p.Xs[Math.Max(i,ii)],p.Zs[j],p.Zs[j+1]):Covers(segs,false,p.Zs[Math.Max(j,jj)],p.Xs[i],p.Xs[i+1]);
     bool nIn=ii>=0&&jj>=0&&ii<nx&&jj<nz&&p.Inside[ii,jj];if(!nIn){if(!beam)pn.Closed=false;continue;}if(beam||id[ii,jj]>=0)continue;id[ii,jj]=list.Count;q.Enqueue(new[]{ii,jj});}}
   list.Add(pn);}
  return list;}
 /* the FLOOR LOAD ranges {x1, x2, z1, z2, value} for the panels with a value (null = not loaded): whole closed panels of equal value merged
    while the range holds no other closed panel; open = loaded panels left out for a gap in their beams, clash = left out because their
    range would hold another closed panel */
 internal static List<double[]> PanelBoxes(WindPlanLevel p,List<SlabPanel> panels,double?[] val,out int open,out int clash){open=0;clash=0;var boxes=new List<double[]>();var used=new bool[panels.Count];
  Func<double[],int,int> rel=(bx,k)=>{int n=0;foreach(var c in panels[k].Cells){double cx=(p.Xs[c[0]]+p.Xs[c[0]+1])/2,cz=(p.Zs[c[1]]+p.Zs[c[1]+1])/2;if(cx>bx[0]&&cx<bx[1]&&cz>bx[2]&&cz<bx[3])n++;}return n==0?0:n==panels[k].Cells.Count?1:2;};
  Func<double[],HashSet<int>,double,bool> fit=(bx,mem,v)=>{bool again=true;while(again){again=false;for(int k=0;k<panels.Count;k++){if(!panels[k].Closed||mem.Contains(k))continue;int r=rel(bx,k);if(r==0)continue;
     if(r==1&&!used[k]&&val[k]!=null&&Math.Abs(val[k].Value-v)<1e-9){mem.Add(k);again=true;continue;}return false;}}return true;};
  var order=Enumerable.Range(0,panels.Count).Where(k=>val[k]!=null).OrderBy(k=>panels[k].X1).ThenBy(k=>panels[k].Z1).ToList();
  foreach(var k in order){if(!panels[k].Closed){open++;used[k]=true;}}
  foreach(var k in order){if(used[k])continue;double v=val[k].Value;var mem=new HashSet<int>{k};var bx=new[]{panels[k].X1,panels[k].X2,panels[k].Z1,panels[k].Z2};
   if(!fit(bx,mem,v)){clash++;used[k]=true;continue;}
   bool grown=true;while(grown){grown=false;foreach(var d in order){if(used[d]||mem.Contains(d)||Math.Abs(val[d].Value-v)>1e-9)continue;
     var b2=new[]{Math.Min(bx[0],panels[d].X1),Math.Max(bx[1],panels[d].X2),Math.Min(bx[2],panels[d].Z1),Math.Max(bx[3],panels[d].Z2)};var m2=new HashSet<int>(mem){d};if(fit(b2,m2,v)){bx=b2;mem=m2;grown=true;}}}
   foreach(var x in mem)used[x]=true;boxes.Add(new[]{bx[0],bx[1],bx[2],bx[3],v});}
  return boxes;}
 /* the FLOOR LOAD lines of the ranges (value + = upward, written as STAAD's GY sign) */
 internal static string FloorLines(double y,List<double[]> boxes){var b=new StringBuilder();foreach(var r in boxes)b.Append("\nYRANGE ").Append(F(y-0.05)).Append(" ").Append(F(y+0.05)).Append(" FLOAD ").Append(F(-r[4])).Append(" XRANGE ").Append(F(r[0]-0.01)).Append(" ").Append(F(r[1]+0.01)).Append(" ZRANGE ").Append(F(r[2]-0.01)).Append(" ").Append(F(r[3]+0.01)).Append(" GY");return b.ToString();}
 /* the comment for panels left out */
 internal static string SkipNote(double y,int open,int clash){var parts=new List<string>();if(open>0)parts.Add(open+" panel"+(open>1?"s":"")+" without beams all around (open edge / opening)");if(clash>0)parts.Add(clash+" panel"+(clash>1?"s":"")+" around another panel");
  return parts.Count==0?"":"\n* level "+F(y)+" m: "+string.Join(", ",parts)+" not loaded - STAAD finds no\n* closed panel there; add the edge beams or load them in STAAD";}
 /* ---- members standing out of the face (owner's STAAD run, 27 November 2026: "**WARNING-fx fy fz wind forces ... at coordinates ...
    ignored"; "gusto ko walang warning") ----  STAAD's wind generator loads closed panels of the exposed face; members beyond the floor
    outline on that side (a canopy of sloped members, an outrigger) leave strips with no closed panel and their forces are dropped. Where
    such members exist, the whole-face line of the faces with that outward normal is cut along the building into stretches whose range ends
    at the outline there (+ 0.05 m), so the members beyond it are not taken into the face; canopies and overhangs take the roof loads. */
 internal static string CutFaceLines(StaadModel m,List<WindEdgeFace> faces,string lines){if(m==null||faces==null||faces.Count==0||string.IsNullOrEmpty(lines))return lines;
  var res=new List<string>();foreach(var ln in lines.Split('\n')){var cut=CutFaceLine(m,faces,ln);res.Add(cut??ln);}return string.Join("\n",res);}
 static string CutFaceLine(StaadModel m,List<WindEdgeFace> faces,string line){const double tol=0.05,e=1e-6;
  var mt=System.Text.RegularExpressions.Regex.Match(line,@"^WIND LOAD (-?)([XZ]) (-?[0-9.]+) (TYPE \d+) YR (-?[0-9.]+) (-?[0-9.]+)$");if(!mt.Success)return null;
  char ax=mt.Groups[2].Value[0];double f=double.Parse(mt.Groups[3].Value,Ci),y1=double.Parse(mt.Groups[5].Value,Ci),y2=double.Parse(mt.Groups[6].Value,Ci);
  /* outward normal of the faces the line loads (see Spec): "X f" the faces facing -sign(f), "-X f" the faces facing sign(f) */
  int n=mt.Groups[1].Value=="-"?Math.Sign(f):-Math.Sign(f);if(n==0)return null;string normal=(n>0?"+":"-")+ax;
  var mine=faces.Where(x=>x.Normal==normal&&x.Level-x.Below>1e-3).ToList();if(mine.Count==0)return null;double k=m.ToMetre;
  Func<StaadJoint,double> U=j=>(ax=='X'?m.P2(j):m.P1(j))*k,W=j=>(ax=='X'?m.P1(j):m.P2(j))*k*n,Y=j=>m.H(j)*k;
  var cuts=mine.SelectMany(x=>new[]{x.A1,x.A2}).Distinct().OrderBy(v=>v).ToList();
  var spans=new List<double[]>();/* u1, u2, cutoff (outward, +inf = none) */
  for(int i=0;i+1<cuts.Count;i++){double u1=cuts[i],u2=cuts[i+1];if(u2-u1<e)continue;var cover=mine.Where(x=>x.A1<=u1+e&&x.A2>=u2-e).ToList();spans.Add(new[]{u1,u2,cover.Count>0?cover.Max(x=>x.At*n):double.PositiveInfinity});}
  if(spans.Count==0)return null;
  /* any member with an end beyond the outline of its stretch? */
  bool beyond=false;foreach(var mem in m.Members.Values){StaadJoint a,b;if(!m.Joints.TryGetValue(mem.A,out a)||!m.Joints.TryGetValue(mem.B,out b))continue;
   foreach(var j in new[]{a,b}){if(Y(j)<y1-tol||Y(j)>y2+tol)continue;double u=U(j);var sp=spans.FirstOrDefault(x=>u>x[0]+e&&u<x[1]-e);if(sp!=null&&W(j)>sp[2]+tol){beyond=true;break;}}if(beyond)break;}
  if(!beyond)return null;
  var merged=new List<double[]>();foreach(var sp in spans){var last=merged.Count>0?merged[merged.Count-1]:null;if(last!=null&&Math.Abs(last[1]-sp[0])<e&&(last[2]==sp[2]||Math.Abs(last[2]-sp[2])<e))last[1]=sp[1];else merged.Add((double[])sp.Clone());}
  /* a stretch needs a wall panel: columns at two places along it at least (owner's STAAD run, 27 November 2026: "NO MEMBERS LOADED FOR A
     WIND LOAD GENERATION" at the balcony edges, a cantilever slab with no column under it); stretches without one are left out */
  var cols=new List<double[]>();/* u, w */foreach(var mem in m.Members.Values){StaadJoint a,b;if(!m.Joints.TryGetValue(mem.A,out a)||!m.Joints.TryGetValue(mem.B,out b))continue;
   if(Math.Abs(U(a)-U(b))>0.02||Math.Abs(W(a)-W(b))>0.02||Math.Abs(Y(a)-Y(b))<1e-3)continue;if(Math.Max(Y(a),Y(b))<y1+tol||Math.Min(Y(a),Y(b))>y2-tol)continue;cols.Add(new[]{U(a),W(a)});}
  merged=merged.Where(sp=>{var us=cols.Where(c=>c[0]>=sp[0]-tol&&c[0]<=sp[1]+tol&&c[1]<=sp[2]+tol).Select(c=>Math.Round(c[0],2)).Distinct().ToList();return us.Count>=2&&us.Max()-us.Min()>0.1;}).ToList();
  if(merged.Count==0)return "* "+line+": no wall panel on this face (no columns), not loaded";
  double lo=m.Joints.Values.Min(j=>W(j))-1,hi=m.Joints.Values.Max(j=>W(j))+1;string head="WIND LOAD "+mt.Groups[1].Value+ax+" "+mt.Groups[3].Value+" "+mt.Groups[4].Value,along=ax=='X'?"ZR":"XR";
  var outl=new List<string>();foreach(var sp in merged){var b=new StringBuilder(head);
   if(!double.IsInfinity(sp[2])){double w1=lo,w2=sp[2]+tol;double r1=n>0?w1:-w2,r2=n>0?w2:-w1;b.Append(" ").Append(ax).Append("R ").Append(F(r1)).Append(" ").Append(F(r2));}
   b.Append(" ").Append(along).Append(" ").Append(F(sp[0]-tol)).Append(" ").Append(F(sp[1]+tol)).Append(" YR ").Append(mt.Groups[5].Value).Append(" ").Append(mt.Groups[6].Value);outl.Add(b.ToString());}
  return string.Join("\n",outl);}
 internal static List<WindEdgeFace> Faces(StaadModel m,double baseY,double top,out string note){return Plans(m,baseY,top,out note).SelectMany(p=>p.Faces).ToList();}
 /* the recessed faces as WIND LOAD lines of their own (owner, 26 November 2026: "bakit member load? dapat windload pa din"): an XR / ZR / YR
    range around one wall makes STAAD build the panel from that wall's members only, so the wall gets the Wind sheet's TYPE like an outer
    face - windward faces (normal against the travel) the WW type pushing in, faces turned away the LW type and walls along the wind the
    SW type, both pulled out (suction, see Spec). A wall that is recessed on several levels in a row is one line, from the level below the first
    to the last (the top of the building for the top level); levels with a gap between them get a line each. */
 internal static List<WindRecessLine> RecessLines(List<WindEdgeFace> faces,WindFaceTypes t){var list=new List<WindRecessLine>();
  char ax=t.Travel[1];int sign=t.Travel[0]=='+'?1:-1;const double tol=0.05;
  /* a storey takes the outline at its top level (the floor above closes it): a level at the base (ground beams) has no storey below it;
     where the outline changes from one level to the next (owner: "icheck mo din if hindi same ang outline ng taas at baba"), each run of
     levels gets its own ZR / XR and YR */
  foreach(var g in faces.Where(x=>x.Shielded&&x.Covered&&x.Level-x.Below>1e-3).GroupBy(f=>f.Key).OrderBy(g=>g.First().Normal[1]).ThenBy(g=>g.First().At).ThenBy(g=>g.First().A1)){
   var runs=new List<List<WindEdgeFace>>();foreach(var f in g.OrderBy(x=>x.Index)){if(runs.Count==0||f.Index!=runs[runs.Count-1].Last().Index+1)runs.Add(new List<WindEdgeFace>());runs[runs.Count-1].Add(f);}
   foreach(var run in runs){var f=run[0];var last=run[run.Count-1];char fn=f.Normal[1];int fs=f.Normal[0]=='+'?1:-1;bool windward=fn==ax&&fs==-sign,leeward=fn==ax&&fs==sign;
    string spec=Spec(f.Normal,!windward,1);int type=windward?t.Ww:leeward?t.Lw:t.Sw;string across=fn=='X'?"Z":"X";
    double y1=f.Below,y2=last.IsTop?last.TopBand:last.Level;
    var line=new WindRecessLine{Face=f,Role=(windward?"WW":leeward?"LW":"SW")+t.Travel,Y1=y1,Y2=y2,Levels=run.Select(x=>x.Level).ToList()};
    line.Comment=(windward?"windward wall":leeward?"leeward wall":"side wall")+" "+fn+" = "+F(f.At)+" m, "+across+" "+F(f.A1)+"-"+F(f.A2)+" m";
    line.Text="WIND LOAD "+spec+" TYPE "+type+" "+fn+"R "+F(f.At-tol)+" "+F(f.At+tol)+" "+across+"R "+F(f.A1-tol)+" "+F(f.A2+tol)+" YR "+F(y1-tol)+" "+F(y2+tol);list.Add(line);}}
  return list;}
 internal static string RecessLoads(StaadModel m,List<WindEdgeFace> faces,WindFaceTypes t,out int count){var lines=RecessLines(faces,t);count=lines.Count;if(count==0)return "";
  return "* recessed walls the wind does not reach along "+t.Travel+" (from the model edges): the range\n* makes STAAD load only that wall\n"+string.Join("\n",lines.Select(l=>"* "+l.Comment+"\n"+l.Text));}
 /* the wind load text of each case for the Load Combinations sheet: Case 1 per direction (+X, -X, +Z, -Z) and, with allCases, the other
    design wind load cases of NSCP 2015 Figure 207B.4-8 (directional procedure, owner 27 November 2026: "do what the code says"):
    Case 2 "2+X+T" - 75 % of the windward / leeward pressures of one axis with the torsion MT = 0.75 (PW + PL) B h e, e = ±0.15 B;
    Case 3 "3+X+Z" - 75 % of both axes at once; Case 4 "4+X+Z+T" - 56.3 % of both axes with both torsions. Without torsion (the code's
    exemption, torsion = false) only Cases 1 and 3 are made. Cases 2-4 carry windward and leeward pressures only, as the figure shows. */
 internal static Dictionary<string,string> CaseTexts(Dictionary<string,string> link,string modelText,out string note){return CaseTexts(link,RedundancyFactor.HasModel(modelText)?RedundancyFactor.ReadModel(modelText):null,out note);}
 internal static Dictionary<string,string> CaseTexts(Dictionary<string,string> link,StaadModel model,out string note){return CaseTexts(link,model,out note,false,true);}
 internal static Dictionary<string,string> CaseTexts(Dictionary<string,string> link,StaadModel model,out string note,bool allCases,bool torsion){note="";var map=new Dictionary<string,string>();if(link==null)return map;
  List<WindEdgeFace> faces=null;double baseY=0;
  int recess=0;var types=new Dictionary<string,WindFaceTypes>();List<WindPlanLevel> roofPlans=null;string roofNote="";var recessLines=new Dictionary<string,List<WindRecessLine>>();
  foreach(var travel in new[]{"+X","-X","+Z","-Z"}){string packed;if(!link.TryGetValue(travel,out packed))continue;var t=WindFaceTypes.Unpack(travel,packed);if(t==null)continue;types[travel]=t;
   if(model!=null&&faces==null){string n;faces=Faces(model,baseY,t.Top,out n);if(n!="")note=n;}
   var b=new StringBuilder(CutFaceLines(model,faces,WindLoadLines(t,baseY)));
   if(model!=null){var rl=RecessLines(faces,t);recessLines[travel]=rl;recess+=rl.Count;string r=RecessText(rl,t.Travel,1);if(r!="")b.Append("\n").Append(r);}
   if(model!=null&&t.HasRoof){if(roofPlans==null){string n3;roofPlans=Plans(model,baseY,t.Top,out n3);}string rn;string roofA=RoofText(model,roofPlans,t,true,baseY,out rn);
    if(roofA!=""){roofNote=rn;string walls=b.ToString();b.Append("\n").Append(roofA);map["1B"+travel]=walls.Replace("(walls, roof uplift +GCpi)","(walls, roof -GCpi / Cp branch 2)")+"\n"+RoofText(model,roofPlans,t,false,baseY,out rn);}}
   b.Insert(0,"* NSCP 2015 Fig. 207B.4-8 Case 1: full pressures, wind toward "+travel+" (walls"+(t.HasRoof?", roof uplift +GCpi":"")+")\n");
   if(model==null)b.Append("\n* recessed walls and roof NOT loaded: paste the STAAD model (Nodes + Beams tables or\n* the .std) on the Primary loads tab of Load Combinations");
   map[travel]=b.ToString();}
  if(allCases&&types.Count>0){List<WindPlanLevel> plans=null;if(model!=null){string n2;plans=Plans(model,baseY,types.Values.Max(x=>x.Top),out n2);}
   Func<string,double,string> part=(travel,f)=>{var t=types[travel];var ln=WindLoadLines(t,baseY).Split('\n');var b=new StringBuilder();b.Append(string.Join("\n",CutFaceLines(model,faces,ln[0]+"\n"+ln[1]).Split('\n').Select(x=>Scale(x,f))));
    List<WindRecessLine> rl;if(recessLines.TryGetValue(travel,out rl)){string r=RecessText(rl.Where(l=>!l.Role.StartsWith("SW")).ToList(),travel,f);if(r!="")b.Append("\n").Append(r);}return b.ToString();};
   Func<IEnumerable<string>,double,int,string> twist=(travels,f,sign)=>{if(plans==null)return "* torsion: paste the STAAD model on the Primary loads tab to make the torsional joint loads";return TorsionText(model,plans,travels.Select(x=>types[x]).ToList(),f,sign,baseY);};
   if(torsion)foreach(var a in types.Keys)foreach(var sg in new[]{1,-1})map["2"+a+(sg>0?"+T":"-T")]="* NSCP 2015 Fig. 207B.4-8 Case 2: 75 % windward / leeward toward "+a+", torsion "+(sg>0?"+T":"-T")+"\n"+part(a,0.75)+"\n"+twist(new[]{a},0.75,sg);
   foreach(var x in new[]{"+X","-X"})foreach(var z in new[]{"+Z","-Z"}){if(!types.ContainsKey(x)||!types.ContainsKey(z))continue;
    map["3"+x+z]="* NSCP 2015 Fig. 207B.4-8 Case 3: 75 % of "+x+" and "+z+" at once\n"+part(x,0.75)+"\n"+part(z,0.75);
    if(torsion)foreach(var sg in new[]{1,-1})map["4"+x+z+(sg>0?"+T":"-T")]="* NSCP 2015 Fig. 207B.4-8 Case 4: 56.3 % of "+x+" and "+z+", torsion "+(sg>0?"+T":"-T")+"\n"+part(x,0.563)+"\n"+part(z,0.563)+"\n"+twist(new[]{x,z},0.563,sg);}}
  if(map.Count>0)note=(note!=""?note+" ":"")+(model==null?"Paste the STAAD Nodes and Beams tables (or the .std file) on the Primary loads tab to load the recessed faces too.":recess>0?faces.Where(f=>f.Shielded&&f.Covered).Select(f=>f.Key).Distinct().Count()+" recessed walls loaded with WIND LOAD ranges: "+string.Join("; ",faces.Where(f=>f.Shielded&&f.Covered).GroupBy(f=>f.Normal[1]+"="+F(f.At)+" m, "+(f.Normal[1]=='X'?"Z ":"X ")+F(f.A1)+"-"+F(f.A2)+" m").Select(g=>g.Key+" (levels "+string.Join(", ",g.Select(f=>F(f.Level)).Distinct())+")"))+".":"No recessed walls in the model.")+(roofNote!=""?" Roof: "+roofNote+".":"");
  return map;}
 /* ---- the roof (owner, 27 November 2026: "add the roof, determine also the overhangs") ----
    Slab roofs: the cells of each level with no floor above them get a FLOOR LOAD (GY) per cell group; trusses: the top chords (the members on
    top of each truss plane above the top floor) get MEMBER LOADs normal to the roof (local Y), pressure x the spacing of the trusses.
    Pressures from the Wind sheet (Figure 207B.4-1): windward / leeward slope for wind normal to a ridge of 10 degrees or more, otherwise the
    zones from the windward edge; a cell or chord over two zones takes the worse one. Overhangs - roof beyond the supporting columns - take
    the top-surface pressure without internal pressure and, at the windward edge, the underside 0.8 qh G pushing up (207B.4.4). */
 internal static string RoofText(StaadModel m,List<WindPlanLevel> plans,WindFaceTypes t,bool upliftCase,double baseY,out string note){note="";if(m==null||t==null||!t.HasRoof||plans.Count==0)return "";
  double k=m.ToMetre;char ax=t.Travel[1];int sign=t.Travel[0]=='+'?1:-1;var b=new StringBuilder();
  /* columns: (x, z) of the top joints of vertical members, by level */
  var colTops=new HashSet<string>();foreach(var mem in m.Members.Values){StaadJoint a,c;if(!m.Joints.TryGetValue(mem.A,out a)||!m.Joints.TryGetValue(mem.B,out c))continue;
   if(Math.Abs(m.P1(a)-m.P1(c))*k>1e-3||Math.Abs(m.P2(a)-m.P2(c))*k>1e-3||Math.Abs(m.H(a)-m.H(c))*k<1e-3)continue;var tp=m.H(a)>m.H(c)?a:c;colTops.Add(Key(m.P1(tp)*k,m.H(tp)*k,m.P2(tp)*k));}
  var floor=new StringBuilder();var skipped=new StringBuilder();int cells=0,ohCells=0;
  var main=plans.Where(p=>p.Y>baseY+1e-3).OrderBy(p=>p.Y).ToList();
  /* trusses above the top floor first: the floor under them is a ceiling, not the roof */
  double[] cover;string truss=TrussRoof(m,main.Count>0?main[main.Count-1].Y:baseY,t,upliftCase,colTops,ref note,out cover);
  /* every level with beams around cells, small roofs included (penthouse, stair roof, a lower roof at a setback) */
  double topMain=main.Count>0?main[main.Count-1].Y:baseY;string an;double ground=Math.Max(baseY,GroundLevel(m));var ordered=Plans(m,baseY,1e9,out an,true).Where(p=>p.Y>ground+1e-3&&p.Faces.Count>0).OrderBy(p=>p.Y).ToList();
  for(int li=0;li<ordered.Count;li++){var p=ordered[li];var higher=ordered.Skip(li+1).ToList();int nx=p.Xs.Count-1,nz=p.Zs.Count-1;
   double xmin=double.MaxValue,xmax=double.MinValue,zmin=double.MaxValue,zmax=double.MinValue;for(int i=0;i<nx;i++)for(int j=0;j<nz;j++)if(p.Inside[i,j]){xmin=Math.Min(xmin,p.Xs[i]);xmax=Math.Max(xmax,p.Xs[i+1]);zmin=Math.Min(zmin,p.Zs[j]);zmax=Math.Max(zmax,p.Zs[j+1]);}
   double e0=ax=='X'?(sign>0?xmin:xmax):(sign>0?zmin:zmax),depth=ax=='X'?xmax-xmin:zmax-zmin;
   var val=new double?[nx,nz];
   for(int i=0;i<nx;i++)for(int j=0;j<nz;j++){if(!p.Inside[i,j])continue;double cx=(p.Xs[i]+p.Xs[i+1])/2,cz=(p.Zs[j]+p.Zs[j+1])/2;/* a roof where no higher level covers it; the top floor under trusses is their ceiling */if(higher.Any(h=>InsideAt(h,cx,cz)))continue;if(cover!=null&&Math.Abs(p.Y-topMain)<1e-3&&cx>cover[0]-1e-6&&cx<cover[1]+1e-6&&cz>cover[2]-1e-6&&cz<cover[3]+1e-6)continue;
    double u1=ax=='X'?p.Xs[i]:p.Zs[j],u2=ax=='X'?p.Xs[i+1]:p.Zs[j+1];double d1=Math.Min(Math.Abs(u1-e0),Math.Abs(u2-e0)),d2=Math.Max(Math.Abs(u1-e0),Math.Abs(u2-e0));
    double net=Pressure(t,d1,d2,depth,upliftCase,false);
    /* overhang: an edge cell whose outer edge has no column under either end */
    foreach(var d in new[]{new[]{-1,0},new[]{1,0},new[]{0,-1},new[]{0,1}}){int ii=i+d[0],jj=j+d[1];if(ii>=0&&jj>=0&&ii<nx&&jj<nz&&p.Inside[ii,jj])continue;
     double ex1,ez1,ex2,ez2;if(d[0]!=0){ex1=ex2=d[0]<0?p.Xs[i]:p.Xs[i+1];ez1=p.Zs[j];ez2=p.Zs[j+1];}else{ez1=ez2=d[1]<0?p.Zs[j]:p.Zs[j+1];ex1=p.Xs[i];ex2=p.Xs[i+1];}
     string normal=(d[0]+d[1]>0?"+":"-")+(d[0]!=0?"X":"Z");double at=d[0]!=0?ex1:ez1,s1=d[0]!=0?ez1:ex1,s2=d[0]!=0?ez2:ex2;
     /* the outline face this side belongs to: supported when a column stands anywhere along it (a cantilever edge has none) */
     var face=p.Faces.FirstOrDefault(fc=>fc.Normal==normal&&Math.Abs(fc.At-at)<1e-6&&fc.A1<=s1+1e-6&&fc.A2>=s2-1e-6);double f1=face!=null?face.A1:s1,f2=face!=null?face.A2:s2;
     if(colTops.Any(key=>{var v=key.Split(',');double cx0=double.Parse(v[0],Ci),cy0=double.Parse(v[1],Ci),cz0=double.Parse(v[2],Ci);if(Math.Abs(cy0-p.Y)>0.011)return false;double line=normal[1]=='X'?cx0:cz0,along=normal[1]=='X'?cz0:cx0;return Math.Abs(line-at)<0.011&&along>=f1-0.011&&along<=f2+0.011;}))continue;bool windwardEdge=normal[1]==ax&&(normal[0]=='+'?1:-1)==-sign;
     net=Pressure(t,d1,d2,depth,upliftCase,true)-(windwardEdge?t.OhUnder:0);ohCells++;break;}
    val[i,j]=net;cells++;}
   /* one value per slab panel (the panel between its beams; a panel over two zones takes the worse cell), ranges of whole closed panels */
   var panels=Panels(p);var pv=new double?[panels.Count];
   for(int q=0;q<panels.Count;q++){double roofA=0;double? worst=null;foreach(var c in panels[q].Cells){var v=val[c[0],c[1]];if(v==null)continue;roofA+=(p.Xs[c[0]+1]-p.Xs[c[0]])*(p.Zs[c[1]+1]-p.Zs[c[1]]);if(worst==null||Math.Abs(v.Value)>Math.Abs(worst.Value))worst=v;}
    if(roofA>0.5*panels[q].Area)pv[q]=worst;}
   int open,clash;var boxes=PanelBoxes(p,panels,pv,out open,out clash);floor.Append(FloorLines(p.Y,boxes));skipped.Append(SkipNote(p.Y,open,clash));}
  if(floor.Length==0&&truss=="")return "";
  b.Append("* roof (Figure 207B.4-1, net of internal pressure ").Append(upliftCase?"+GCpi, uplift":"-GCpi, downward").Append("); overhangs 207B.4.4");
  b.Append(skipped);if(floor.Length>0)b.Append("\nUNIT METER KN\nFLOOR LOAD").Append(floor);if(truss!="")b.Append("\n").Append(truss);
  note=cells+" roof cells"+(ohCells>0?", "+ohCells+" overhang cells":"")+(note!=""?"; "+note:"");return b.ToString();}
 static string Key(double x,double y,double z){return Math.Round(x,2).ToString("0.00",Ci)+","+Math.Round(y,2).ToString("0.00",Ci)+","+Math.Round(z,2).ToString("0.00",Ci);}
 /* the ground: the lowest level of beams when it is within 2.5 m of the lowest joint (tie / grade beams over footings or pedestals,
    slab on grade) - never a roof (owner, 27 November 2026: "bat may rooflive load sa baba"); otherwise the lowest joint (supports) */
 internal static double GroundLevel(StaadModel m){if(m==null||m.Joints.Count==0)return 0;double k=m.ToMetre;double low=m.Joints.Values.Min(j=>m.H(j))*k;
  var lv=Levels(m,0).Keys.OrderBy(y=>y).ToList();if(lv.Count>1&&lv[0]-low<=2.5+1e-6)return Math.Max(low,lv[0]);return low;}
 /* inside a level's outline or on its edge (tolerance 2 cm) */
 internal static bool OnPlan(WindPlanLevel p,double x,double z){const double t=0.02;for(int i=0;i+1<p.Xs.Count;i++){if(x<p.Xs[i]-t||x>p.Xs[i+1]+t)continue;for(int j=0;j+1<p.Zs.Count;j++)if(z>=p.Zs[j]-t&&z<=p.Zs[j+1]+t&&p.Inside[i,j])return true;}return false;}
 internal static bool InsideAt(WindPlanLevel p,double x,double z){for(int i=0;i+1<p.Xs.Count;i++)if(x>p.Xs[i]&&x<p.Xs[i+1])for(int j=0;j+1<p.Zs.Count;j++)if(z>p.Zs[j]&&z<p.Zs[j+1])return p.Inside[i,j];return false;}
 /* the roof pressure (kPa, + toward the surface) for a strip d1-d2 from the windward edge of a roof of this depth; external only for overhangs */
 static double Pressure(WindFaceTypes t,double d1,double d2,double depth,bool uplift,bool external){
  if(t.RoofNormal){bool windward=(d1+d2)/2<depth/2;if(external)return windward?(uplift?t.OhWwA:t.OhWwB):(uplift?t.OhLwA:t.OhLwB);return windward?(uplift?t.WrA:t.WrB):(uplift?t.LrA:t.LrB);}
  double? v=null;foreach(var z in t.Zones){if(z[1]<=d1+1e-9||z[0]>=d2-1e-9)continue;double q=external&&z.Length>5?(uplift?z[4]:z[5]):(uplift?z[2]:z[3]);v=v==null?q:uplift?Math.Min(v.Value,q):Math.Max(v.Value,q);}
  if(v==null&&t.Zones.Count>0){var z=t.Zones[t.Zones.Count-1];v=external&&z.Length>5?(uplift?z[4]:z[5]):(uplift?z[2]:z[3]);}return v??0;}
 /* the top chords of the trusses above the top floor (for the roof live load, RoofLiveLoads): same detection as TrussRoof */
 internal sealed class TrussChord {public double No,Plane,Trib,U1,U2,Y1,Y2;public bool SpanX;}
 internal static List<TrussChord> TrussTopChords(StaadModel m,double topY,out double[] cover){return TrussTopChords(m,topY,null,out cover);}
 /* at any level (owner, 27 November 2026: "madetermine niya sana yung mga ibat ibang levels based din sa model"): the sloped members rising
    from this level; a group of connected members that reaches a higher floor over its own footprint is a stair or a ramp, not a roof */
 internal static List<TrussChord> TrussTopChords(StaadModel m,double topY,List<WindPlanLevel> floors,out double[] cover){double k=m.ToMetre;cover=null;var res=new List<TrussChord>();
  var mem=new List<double[]>();var near=new List<double[]>();
  foreach(var mm in m.Members.Values){StaadJoint a,c;if(!m.Joints.TryGetValue(mm.A,out a)||!m.Joints.TryGetValue(mm.B,out c))continue;double ya=m.H(a)*k,yc=m.H(c)*k;
   double dx=Math.Abs(m.P1(a)-m.P1(c))*k,dz=Math.Abs(m.P2(a)-m.P2(c))*k;if(dx<1e-3&&dz<1e-3)continue;var row=new[]{mm.No,m.P1(a)*k,ya,m.P2(a)*k,m.P1(c)*k,yc,m.P2(c)*k,mm.A,mm.B};
   if(Math.Min(ya,yc)>=topY-1e-3&&Math.Max(ya,yc)>topY+1e-3)mem.Add(row);else if(Math.Abs(ya-yc)>1e-3&&Math.Max(ya,yc)>=topY-1e-3&&Math.Min(ya,yc)>=topY-3)near.Add(row);}
  var roofJoints=new HashSet<double>(mem.SelectMany(r=>new[]{r[7],r[8]}));foreach(var r in near)if(roofJoints.Contains(r[7])||roofJoints.Contains(r[8]))mem.Add(r);
  if(floors!=null&&mem.Count>0){/* connected groups (shared joints); drop the ones that reach a higher floor covering them (stairs, ramps) */
   var parent=new Dictionary<double,double>();Func<double,double> find=null;find=x=>{double v;if(!parent.TryGetValue(x,out v)){parent[x]=x;return x;}if(v==x)return x;var r0=find(v);parent[x]=r0;return r0;};
   foreach(var r in mem){var a=find(r[7]);var c=find(r[8]);if(a!=c)parent[a]=c;}
   /* floor beams of a higher floor are not roof members */
   var higher=floors.Where(f=>f.Y>topY+0.5).ToList();mem=mem.Where(r=>!(Math.Abs(r[2]-r[5])<1e-3&&higher.Any(f=>Math.Abs(f.Y-r[2])<0.02))).ToList();
   parent.Clear();foreach(var r in mem){var a=find(r[7]);var c=find(r[8]);if(a!=c)parent[a]=c;}
   /* a group with a joint on a higher floor (inside its outline or on its edge) climbs to it: a stair or a ramp */
   mem=mem.GroupBy(r=>find(r[7])).Where(g=>!g.SelectMany(r=>new[]{new[]{r[1],r[2],r[3]},new[]{r[4],r[5],r[6]}}).Any(pt=>higher.Any(f=>Math.Abs(f.Y-pt[1])<0.02&&OnPlan(f,pt[0],pt[2])))).SelectMany(g=>g).ToList();}
  var sloped=mem.Where(r=>Math.Abs(r[2]-r[5])>1e-3).ToList();if(sloped.Count==0)return res;
  bool spanX=sloped.Count(r=>Math.Abs(r[3]-r[6])<1e-3)>=sloped.Count(r=>Math.Abs(r[1]-r[4])<1e-3);
  Func<double[],double> plane=r=>spanX?r[3]:r[1];Func<double[],double> u1=r=>spanX?r[1]:r[3];Func<double[],double> u2=r=>spanX?r[4]:r[6];
  var inPlane=mem.Where(r=>spanX?Math.Abs(r[3]-r[6])<1e-3:Math.Abs(r[1]-r[4])<1e-3).ToList();var planes=inPlane.GroupBy(r=>Math.Round(plane(r),2)).OrderBy(g=>g.Key).ToList();if(planes.Count==0)return res;
  var coords=planes.Select(g=>g.Key).ToList();double uMin=inPlane.Min(r=>Math.Min(u1(r),u2(r))),uMax=inPlane.Max(r=>Math.Max(u1(r),u2(r)));cover=spanX?new[]{uMin,uMax,coords.First(),coords.Last()}:new[]{coords.First(),coords.Last(),uMin,uMax};
  for(int pi=0;pi<planes.Count;pi++){var g=planes[pi].ToList();double c=planes[pi].Key;double trib=((pi+1<coords.Count?coords[pi+1]:c)-(pi>0?coords[pi-1]:c))/2;if(coords.Count==1)trib=1;
   foreach(var r in g){double a=u1(r),bb=u2(r),ya=r[2],yb=r[5];double um=(a+bb)/2,ym=(ya+yb)/2;
    bool top=!g.Any(o=>o!=r&&Math.Min(u1(o),u2(o))<=um+1e-6&&Math.Max(u1(o),u2(o))>=um-1e-6&&Math.Abs(u2(o)-u1(o))>1e-6&&o[2]+(o[5]-o[2])*(um-u1(o))/(u2(o)-u1(o))>ym+0.02);if(!top)continue;
    res.Add(new TrussChord{No=r[0],Plane=c,Trib=trib,U1=a,U2=bb,Y1=ya,Y2=yb,SpanX=spanX});}}
  return res;}
 /* top chords of the trusses above the top floor: members in the truss planes (no extent along the ridge) on top of their plane */
 static string TrussRoof(StaadModel m,double topY,WindFaceTypes t,bool uplift,HashSet<string> colTops,ref string note){double[] cover;return TrussRoof(m,topY,t,uplift,colTops,ref note,out cover);}
 /* cover = x1, x2, z1, z2 of the trussed roof (the ceiling under it takes no roof FLOOR LOAD) */
 static string TrussRoof(StaadModel m,double topY,WindFaceTypes t,bool uplift,HashSet<string> colTops,ref string note,out double[] cover){double k=m.ToMetre;cover=null;
  var mem=new List<double[]>();/* no, x1, y1, z1, x2, y2, z2, joint A, joint B */var near=new List<double[]>();
  foreach(var mm in m.Members.Values){StaadJoint a,c;if(!m.Joints.TryGetValue(mm.A,out a)||!m.Joints.TryGetValue(mm.B,out c))continue;double ya=m.H(a)*k,yc=m.H(c)*k;
   double dx=Math.Abs(m.P1(a)-m.P1(c))*k,dz=Math.Abs(m.P2(a)-m.P2(c))*k;if(dx<1e-3&&dz<1e-3)continue;var row=new[]{mm.No,m.P1(a)*k,ya,m.P2(a)*k,m.P1(c)*k,yc,m.P2(c)*k,mm.A,mm.B};
   if(Math.Min(ya,yc)>=topY-1e-3&&Math.Max(ya,yc)>topY+1e-3)mem.Add(row);
   /* sloped members just below (eave overhangs of the top chords hang below the eave) - kept when they meet the roof members */
   else if(Math.Abs(ya-yc)>1e-3&&Math.Max(ya,yc)>=topY-1e-3&&Math.Min(ya,yc)>=topY-3)near.Add(row);}
  var roofJoints=new HashSet<double>(mem.SelectMany(r=>new[]{r[7],r[8]}));foreach(var r in near)if(roofJoints.Contains(r[7])||roofJoints.Contains(r[8]))mem.Add(r);
  var sloped=mem.Where(r=>Math.Abs(r[2]-r[5])>1e-3).ToList();if(sloped.Count==0)return "";
  /* truss planes: X-Y (dz = 0) or Z-Y (dx = 0), whichever the sloped members follow */
  bool spanX=sloped.Count(r=>Math.Abs(r[3]-r[6])<1e-3)>=sloped.Count(r=>Math.Abs(r[1]-r[4])<1e-3);
  Func<double[],double> plane=r=>spanX?r[3]:r[1];Func<double[],double> u1=r=>spanX?r[1]:r[3];Func<double[],double> u2=r=>spanX?r[4]:r[6];
  var inPlane=mem.Where(r=>spanX?Math.Abs(r[3]-r[6])<1e-3:Math.Abs(r[1]-r[4])<1e-3).ToList();
  var planes=inPlane.GroupBy(r=>Math.Round(plane(r),2)).OrderBy(g=>g.Key).ToList();if(planes.Count==0)return "";
  var coords=planes.Select(g=>g.Key).ToList();char ax=t.Travel[1];int sign=t.Travel[0]=='+'?1:-1;bool alongSpan=(ax=='X')==spanX;
  double uMin=inPlane.Min(r=>Math.Min(u1(r),u2(r))),uMax=inPlane.Max(r=>Math.Max(u1(r),u2(r))),cMin=coords.First(),cMax=coords.Last();
  cover=spanX?new[]{uMin,uMax,cMin,cMax}:new[]{cMin,cMax,uMin,uMax};
  var lines=new List<string>();int chords=0,oh=0;
  for(int pi=0;pi<planes.Count;pi++){var g=planes[pi].ToList();double c=planes[pi].Key;double trib=((pi+1<coords.Count?coords[pi+1]:c)-(pi>0?coords[pi-1]:c))/2;if(coords.Count==1)trib=1;
   /* wall line of this truss: the columns in the plane */
   var cols=colTops.Select(s=>s.Split(',').Select(v=>double.Parse(v,Ci)).ToArray()).Where(v=>Math.Abs((spanX?v[2]:v[0])-c)<0.02&&v[1]<=topY+0.02&&v[1]>=topY-0.5).Select(v=>spanX?v[0]:v[2]).ToList();
   double w0=cols.Count>0?cols.Min():double.MinValue,w1=cols.Count>0?cols.Max():double.MaxValue;
   foreach(var r in g){double a=u1(r),bb=u2(r),ya=r[2],yb=r[5];double um=(a+bb)/2,ym=(ya+yb)/2;
    bool top=!g.Any(o=>o!=r&&Math.Min(u1(o),u2(o))<=um+1e-6&&Math.Max(u1(o),u2(o))>=um-1e-6&&Math.Abs(u2(o)-u1(o))>1e-6&&o[2]+(o[5]-o[2])*(um-u1(o))/(u2(o)-u1(o))>ym+0.02);if(!top)continue;chords++;
    double L=Math.Sqrt((bb-a)*(bb-a)+(yb-ya)*(yb-ya)+0.0);
    Func<double,double,double> pr=(s1,s2)=>{/* s1 / s2 along the span */double d1,d2,depth;bool ext=false;
      if(alongSpan){double e0=sign>0?uMin:uMax;d1=Math.Min(Math.Abs(s1-e0),Math.Abs(s2-e0));d2=Math.Max(Math.Abs(s1-e0),Math.Abs(s2-e0));depth=uMax-uMin;
       if(t.RoofNormal){/* windward slope rises along the travel */double rise=(yb-ya)*(bb-a)*sign;bool ww=rise>0||Math.Abs(yb-ya)<1e-6&&(d1+d2)/2<depth/2;return uplift?(ww?t.WrA:t.LrA):(ww?t.WrB:t.LrB);}}
      else{double e0=sign>0?cMin:cMax;d1=Math.Abs(c-e0)-Math.Abs(trib)/2;d2=Math.Abs(c-e0)+Math.Abs(trib)/2;d1=Math.Max(0,d1);depth=cMax-cMin;}
      return Pressure(t,d1,d2,depth,uplift,ext);};
    double p=pr(a,bb);lines.Add(F(r[0])+" UNI Y "+F(-p*Math.Abs(trib)));
    /* overhang parts beyond the wall line */
    foreach(var side in new[]{-1,1}){double edge=side<0?w0:w1;if(Math.Abs(edge)>1e8)continue;double lo=Math.Min(a,bb),hi=Math.Max(a,bb);double s1=side<0?lo:Math.Max(lo,edge),s2=side<0?Math.Min(hi,edge):hi;if(s2-s1<1e-3)continue;
     bool windwardEnd=alongSpan&&((sign>0&&side<0)||(sign<0&&side>0));double ext=Pressure(t,0,0.001,1,uplift,true);if(t.RoofNormal&&alongSpan)ext=windwardEnd?(uplift?t.OhWwA:t.OhWwB):(uplift?t.OhLwA:t.OhLwB);
     double extra=(ext-(windwardEnd?t.OhUnder:0))-p;/* replaces the net pressure on that part */
     double f1=Math.Abs(s1-a)/Math.Abs(bb-a)*L,f2=Math.Abs(s2-a)/Math.Abs(bb-a)*L;lines.Add(F(r[0])+" UNI Y "+F(-extra*Math.Abs(trib))+" "+F(Math.Min(f1,f2))+" "+F(Math.Max(f1,f2)));oh++;}}}
  if(lines.Count==0)return "";note=chords+" truss top chords"+(oh>0?", "+oh+" overhang parts":"");
  return "* trusses: top chords, w = p x truss spacing, normal to the roof (local Y)\nUNIT METER KN\nMEMBER LOAD\n"+string.Join("\n",lines);}
 static string RecessText(List<WindRecessLine> lines,string travel,double f){if(lines.Count==0)return "";
  return "* recessed walls the wind does not reach along "+travel+" (from the model edges): the range\n* makes STAAD load only that wall\n"+string.Join("\n",lines.Select(l=>"* "+l.Comment+"\n"+Scale(l.Text,f)));}
 /* "WIND LOAD X 1 TYPE 1 ..." with the factor scaled (the sign of the factor kept) */
 internal static string Scale(string line,double f){if(Math.Abs(f-1)<1e-12)return line;var m=System.Text.RegularExpressions.Regex.Match(line,@"^(WIND LOAD \S+) (-?)1 (TYPE .*)$");if(!m.Success)return line;
  return m.Groups[1].Value+" "+F((m.Groups[2].Value=="-"?-1:1)*f)+" "+m.Groups[3].Value;}
 /* the torsion of Cases 2 / 4 per level: MT = f (PW + PL) B h · 0.15 B for each axis of the case (B = plan width across the wind at the level,
    h = the level's tributary height, PW at the level's height), applied as a couple of joint loads FX at the two joints of the level farthest
    apart in Z (sign: +T = MT about +Y) - a couple gives torsion only, no net force, and needs no rigid diaphragm */
 internal sealed class TorsionLevel {public double Y,H,Mt;public int J1,J2;public double D,X1,Z1,X2,Z2;public List<string> Parts=new List<string>();
  /* the outline joints of the level and their share of a unit MT (owner, 27 November 2026: "e irregular yung shape ng building"):
     F = MT r / sum r^2 square to the radius from the joints' centre - no net force, sum (z FX - x FZ) = MT, any plan shape, with or
     without a rigid FLOOR DIAPHRAGM; the two-joint couple (J1 / J2, D) is kept only when the outline has fewer than three joints */
  public List<Tuple<int,double,double>> Edge=new List<Tuple<int,double,double>>();public double SumR2,Xc,Zc;}
 internal static List<TorsionLevel> Torsion(StaadModel m,List<WindPlanLevel> plans,List<WindFaceTypes> types,double f,double baseY){var list=new List<TorsionLevel>();double k=m.ToMetre;
  foreach(var p in plans.OrderBy(x=>x.Y)){if(p.Y<=baseY+1e-3||p.Faces.Count==0)continue;var face=p.Faces[0];double h=face.TopBand-face.Bottom;if(h<=1e-6)continue;
   double xmin=double.MaxValue,xmax=double.MinValue,zmin=double.MaxValue,zmax=double.MinValue;for(int i=0;i+1<p.Xs.Count;i++)for(int j=0;j+1<p.Zs.Count;j++)if(p.Inside[i,j]){xmin=Math.Min(xmin,p.Xs[i]);xmax=Math.Max(xmax,p.Xs[i+1]);zmin=Math.Min(zmin,p.Zs[j]);zmax=Math.Max(zmax,p.Zs[j+1]);}
   if(xmin>xmax)continue;var lv=new TorsionLevel{Y=p.Y,H=h};
   foreach(var t in types){bool alongX=t.Travel[1]=='X';double B=alongX?zmax-zmin:xmax-xmin;double pw=t.WindwardAt(p.Y),pl=t.Leeward;double mt=f*(pw+pl)*B*h*0.15*B;lv.Mt+=mt;
    lv.Parts.Add(F(f)+" × ("+F(pw)+" + "+F(pl)+") × "+F(B)+" × "+F(h)+" × 0.15 × "+F(B)+" = "+F(mt)+" ("+t.Travel+")");}
   var js=m.Joints.Values.Where(j=>Math.Abs(m.H(j)*k-p.Y)<1e-3).ToList();if(js.Count<2)continue;double cx=(xmin+xmax)/2;
   /* at each Z edge the joint nearest the middle of the longest outer face there (clear of recess corners) */
   Func<string,double,double> mid=(normal,at)=>{var fz=p.Faces.Where(fc=>fc.Normal==normal&&!fc.Shielded&&Math.Abs(fc.At-at)<1e-6).OrderByDescending(fc=>fc.A2-fc.A1).FirstOrDefault();return fz!=null?(fz.A1+fz.A2)/2:cx;};
   double ma=mid("-Z",zmin),mb=mid("+Z",zmax);
   var a=js.OrderBy(j=>R(m.P2(j)*k)).ThenBy(j=>Math.Abs(m.P1(j)*k-ma)).First();var b=js.OrderByDescending(j=>R(m.P2(j)*k)).ThenBy(j=>Math.Abs(m.P1(j)*k-mb)).First();
   lv.J1=a.No;lv.J2=b.No;lv.X1=m.P1(a)*k;lv.Z1=m.P2(a)*k;lv.X2=m.P1(b)*k;lv.Z2=m.P2(b)*k;lv.D=(m.P2(b)-m.P2(a))*k;if(lv.D<=1e-6)continue;
   var edge=js.Where(j=>{double x=m.P1(j)*k,z=m.P2(j)*k;return p.Faces.Any(fc=>fc.Normal[1]=='X'?Math.Abs(x-fc.At)<0.02&&z>fc.A1-0.02&&z<fc.A2+0.02:Math.Abs(z-fc.At)<0.02&&x>fc.A1-0.02&&x<fc.A2+0.02);}).OrderBy(j=>j.No).ToList();
   if(edge.Count>=3){double xc=edge.Average(j=>m.P1(j)*k),zc=edge.Average(j=>m.P2(j)*k),s2=edge.Sum(j=>Math.Pow(m.P1(j)*k-xc,2)+Math.Pow(m.P2(j)*k-zc,2));
    if(s2>1e-9){lv.SumR2=s2;lv.Xc=xc;lv.Zc=zc;foreach(var j in edge)lv.Edge.Add(Tuple.Create(j.No,(m.P2(j)*k-zc)/s2,-(m.P1(j)*k-xc)/s2));}}
   list.Add(lv);}
  return list;}
 static string TorsionText(StaadModel m,List<WindPlanLevel> plans,List<WindFaceTypes> types,double f,int sign,double baseY){var lv=Torsion(m,plans,types,f,baseY);if(lv.Count==0)return "* torsion: no floor level found in the model";
  var b=new StringBuilder("* torsion "+(sign>0?"+T":"-T")+" (NSCP Fig. 207B.4-8): MT = f (PW + PL) B h x 0.15 B per level,\n* spread over the outline joints of each level, F = MT r / sum r2 square\n* to the radius (no net force; any plan shape, with or without a diaphragm)\nUNIT METER KN\nJOINT LOAD");
  foreach(var l in lv){if(l.Edge.Count>=3){b.Append("\n* level ").Append(F(l.Y)).Append(" m: MT = ").Append(F(l.Mt)).Append(" kN-m, ").Append(l.Edge.Count).Append(" joints, sum r2 = ").Append(F(l.SumR2)).Append(" m2");
    foreach(var e in l.Edge){double fx=Math.Round(sign*l.Mt*e.Item2,3),fz=Math.Round(sign*l.Mt*e.Item3,3);if(fx==0&&fz==0)continue;b.Append("\n").Append(e.Item1);if(fx!=0)b.Append(" FX ").Append(F(fx));if(fz!=0)b.Append(" FZ ").Append(F(fz));}continue;}
   double f0=sign*l.Mt/l.D;b.Append("\n* level ").Append(F(l.Y)).Append(" m: MT = ").Append(F(l.Mt)).Append(" kN-m, d = ").Append(F(l.D)).Append(" m");
   b.Append("\n").Append(l.J2).Append(" FX ").Append(F(f0)).Append("\n").Append(l.J1).Append(" FX ").Append(F(-f0));}
  return b.ToString();}
}
}
