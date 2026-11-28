using System;using System.Collections.Generic;using System.Drawing;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
/* Structural irregularities, NSCP 2015 Table 208-9 (vertical) and Table 208-10 (plan) (owner, 26 November 2026: "for structural
   irregularities check"). From the STAAD model, the EQ member end forces and joint displacements where they can be computed; the rest from
   short inputs. Each row: Regular / Irregular / Not checked. */
sealed class IrregularityRow {public string Table,Type,Criterion,Value,Result;/* how it was found, shown on its own tab */public string Detail="";
 /* the full check for the report (owner, 28 November 2026: "yung sa irregularities sana full check naman with tables diagrams calculations"):
    a data table, calculation lines {label, formula, result} and a figure */
 public string[] Head;public float[] Widths;public List<string[]> Rows=new List<string[]>();public List<string[]> Calcs=new List<string[]>();
 public Action<Graphics,RectangleF> Figure;public float FigureH;public string Caption="";
 /* what to do about it (owner, 28 November 2026: "ano recommendation if may irregularities") */public string Recommend="";}
sealed class IrregularityInput {public string Weights="";public double ProjX,PlanX,ProjZ,PlanZ,Opening,FloorArea;public bool InPlane,OutOfPlane,WeakStory;}
static class StructuralIrregularity {
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;
 static string F(double v,string f="0.###"){return v.ToString(f,Ci);}
 internal const string Regular="Regular",Irregular="Irregular",NotChecked="Not checked";
 /* storey stiffness k = V / drift: V from the column / brace shears (RedundancyFactor), drift from the mean displacement in the load direction */
 /* storey stiffness k = storey shear / storey drift (owner, 28 November 2026: "bakit fail sa soft storey sa computation pero pasado sa staad mismo"):
    storeys run between the real floor levels (beams) from the ground up - not at every column joint (a wall mesh splits columns every 0.5 m)
    and not the pedestals below the ground; the storey shear is the sum of the shears of the columns and braces cut at mid-storey; the drift is
    the mean displacement of the floor above minus the floor below. Rows: bottom, top (m from the lowest joint), V, drift, k. */
 internal static List<double[]> Stiffness(StaadModel m,Dictionary<string,double[]> forces,Dictionary<string,double[]> disp,int load,string axis){var res=new List<double[]>();
  double k=m.ToMetre,baseH=m.Joints.Values.Min(j=>m.H(j)),ground=WindEdgeLoads.GroundLevel(m)/k;int comp=axis!="Z"?0:(m.ZUp?1:2);
  var floors=WindEdgeLoads.FloorPlans(m).Select(p=>p.Level/k).Where(h=>h>=ground-1e-6).ToList();
  /* the FLOOR DIAPHRAGM heights of the model are its storeys, as STAAD takes them (owner, 28 November 2026: "may defined diaphragm naman ako") */
  if(m.Diaphragms.Count>0){floors=m.Diaphragms.Select(h=>h/k).ToList();double bs=double.IsNaN(m.DiaphragmBase)?ground:m.DiaphragmBase/k;floors.Add(bs);ground=bs;floors=floors.Where(h=>h>=bs-1e-6).ToList();}
  /* a plane frame has no floor plans: its floors are the levels of its horizontal members */
  if(floors.Count<2)floors=m.Members.Values.Where(mm=>m.Joints.ContainsKey(mm.A)&&m.Joints.ContainsKey(mm.B)&&Math.Abs(m.H(m.Joints[mm.A])-m.H(m.Joints[mm.B]))<1e-6).Select(mm=>Math.Round(m.H(m.Joints[mm.A]),4)).Where(h=>h>=ground-1e-6).Distinct().ToList();
  if(floors.Count==0||floors.Min()>ground+1e-6)floors.Insert(0,ground);floors=floors.Distinct().OrderBy(v=>v).ToList();
  Func<double,double> mean=h=>{var v=m.Joints.Values.Where(j=>Math.Abs(m.H(j)-h)<1e-3&&disp.ContainsKey(j.No+"|"+load)).Select(j=>Math.Abs(disp[j.No+"|"+load][comp])).ToList();return v.Count==0?double.NaN:v.Average();};
  Func<StaadMember,int,double[]> F=(mm,jn)=>{double[] v;return forces.TryGetValue(mm.No+"|"+load+"|"+jn,out v)?v:null;};
  var ends=m.Members.Values.SelectMany(mm=>new[]{mm.A,mm.B}).Where(no=>m.Joints.ContainsKey(no)).Select(no=>m.H(m.Joints[no])).Distinct().ToList();
  for(int i=0;i+1<floors.Count;i++){double b0=floors[i],b1=floors[i+1];
   /* the cut lies in the widest gap between member ends inside the storey, so a column split by a wall mesh (joints every 0.51 m, one of them at
      mid-storey) is still cut exactly once */
   var hs=ends.Where(h=>h>b0+1e-6&&h<b1-1e-6).Concat(new[]{b0,b1}).Distinct().OrderBy(h=>h).ToList();double cut=(b0+b1)/2,gap=0;for(int q=0;q+1<hs.Count;q++)if(hs[q+1]-hs[q]>gap){gap=hs[q+1]-hs[q];cut=(hs[q]+hs[q+1])/2;}
   double shear=0;int n=0;
   foreach(var mm in m.Members.Values){StaadJoint a,b;if(!m.Joints.TryGetValue(mm.A,out a)||!m.Joints.TryGetValue(mm.B,out b))continue;double lo=Math.Min(m.H(a),m.H(b)),hi=Math.Max(m.H(a),m.H(b));if(!(lo<cut-1e-9&&hi>cut+1e-9))continue;
    double dv=hi-lo,dh=Math.Sqrt(Math.Pow(m.P1(b)-m.P1(a),2)+Math.Pow(m.P2(b)-m.P2(a),2));var v=F(mm,mm.A)??F(mm,mm.B);if(v==null)continue;n++;
    if(dh<=0.02*dv)shear+=Math.Sqrt(v[1]*v[1]+v[2]*v[2]);else{double L=Math.Sqrt(dh*dh+dv*dv);shear+=Math.Abs(v[0])*dh/L;}}
   if(n==0||shear<=1e-9)continue;double db=Math.Abs(b0-ground)<1e-6?0:mean(b0),dt=mean(b1);double drift=dt-(double.IsNaN(db)?0:db);
   if(double.IsNaN(dt)||drift<=1e-12)continue;res.Add(new[]{(b0-baseH)*k,(b1-baseH)*k,shear,drift,shear/drift});}
  return res;}
 /* plan dimensions of the columns of each storey (lateral system), in metres */
 internal static List<double[]> PlanDimensions(StaadModel m){var cols=m.Members.Values.Select(mm=>new{a=m.Joints[mm.A],b=m.Joints[mm.B]}).Where(c=>{double dv=Math.Abs(m.H(c.b)-m.H(c.a)),dh=Math.Sqrt(Math.Pow(m.P1(c.b)-m.P1(c.a),2)+Math.Pow(m.P2(c.b)-m.P2(c.a),2));return dv>1e-6&&dh<=0.02*dv;}).ToList();
  double baseH=m.Joints.Values.Min(j=>m.H(j));var res=new List<double[]>();
  /* storeys between the real floor levels (beams), not every column joint: a wall mesh with joints every 0.5 m is not a storey each 0.5 m */
  var floors=WindEdgeLoads.FloorPlans(m).Select(p=>p.Level/m.ToMetre).ToList();if(cols.Count>0){double lo=cols.Min(c=>Math.Min(m.H(c.a),m.H(c.b)));if(floors.Count==0||floors.Min()>lo+1e-6)floors.Insert(0,lo);}floors=floors.Distinct().OrderBy(v=>v).ToList();
  if(floors.Count>=2){for(int i=0;i+1<floors.Count;i++){double b0=floors[i],b1=floors[i+1];var span=cols.Where(c=>Math.Min(m.H(c.a),m.H(c.b))<b1-1e-6&&Math.Max(m.H(c.a),m.H(c.b))>b0+1e-6).SelectMany(c=>new[]{c.a,c.b}).ToList();if(span.Count==0)continue;
    res.Add(new[]{(b0-baseH)*m.ToMetre,(span.Max(j=>m.P1(j))-span.Min(j=>m.P1(j)))*m.ToMetre,(span.Max(j=>m.P2(j))-span.Min(j=>m.P2(j)))*m.ToMetre});}return res;}
  foreach(var g in cols.GroupBy(c=>Math.Round(Math.Min(m.H(c.a),m.H(c.b)),3)).OrderBy(g=>g.Key)){var pts=g.SelectMany(c=>new[]{c.a,c.b}).ToList();
   res.Add(new[]{(g.Key-baseH)*m.ToMetre,(pts.Max(j=>m.P1(j))-pts.Min(j=>m.P1(j)))*m.ToMetre,(pts.Max(j=>m.P2(j))-pts.Min(j=>m.P2(j)))*m.ToMetre});}
  return res;}
 /* horizontal members not parallel to the X or Z axis (plan angle not a multiple of 90 degrees) */
 internal static int NonParallel(StaadModel m){int n=0;foreach(var mm in m.Members.Values){var a=m.Joints[mm.A];var b=m.Joints[mm.B];if(Math.Abs(m.H(b)-m.H(a))>1e-6)continue;double dx=m.P1(b)-m.P1(a),dz=m.P2(b)-m.P2(a);if(Math.Sqrt(dx*dx+dz*dz)<1e-6)continue;
   double ang=Math.Abs(Math.Atan2(dz,dx)*180/Math.PI)%90;if(ang>2&&ang<88)n++;}return n;}
 /* re-entrant corners from the floor plans (owner, 26 November 2026: "yung inputs na needed para autocompute na lang sana from staad"): the recesses
    of each plan (outside cells touching the outline box); a corner recess projects by its own size, a recess in a side by its depth and the
    narrower wing beside it */
 internal sealed class ReEntrantResult {public double Level,ProjX,PlanX,ProjZ,PlanZ,RX0,RX1,RZ0,RZ1;public string Where="";public double Worst{get{return PlanX<=0||PlanZ<=0?0:Math.Min(ProjX/PlanX,ProjZ/PlanZ);}}}
 internal static ReEntrantResult ReEntrant(List<WindEdgeLoads.FloorPlan> plans,out double area){ReEntrantResult best=null;area=0;
  foreach(var p in plans){int nx=p.Xs.Count-1,nz=p.Zs.Count-1;int i0=nx,i1=-1,j0=nz,j1=-1;for(int i=0;i<nx;i++)for(int j=0;j<nz;j++)if(p.Inside[i,j]){i0=Math.Min(i0,i);i1=Math.Max(i1,i);j0=Math.Min(j0,j);j1=Math.Max(j1,j);}if(i1<0)continue;
   area=Math.Max(area,p.Area);double planX=p.Xs[i1+1]-p.Xs[i0],planZ=p.Zs[j1+1]-p.Zs[j0];var seen=new bool[nx,nz];
   for(int i=i0;i<=i1;i++)for(int j=j0;j<=j1;j++){if(p.Inside[i,j]||seen[i,j])continue;var stack=new Stack<int[]>();stack.Push(new[]{i,j});seen[i,j]=true;int a0=i,a1=i,b0=j,b1=j;bool left=false,right=false,low=false,high=false;
    while(stack.Count>0){var c=stack.Pop();a0=Math.Min(a0,c[0]);a1=Math.Max(a1,c[0]);b0=Math.Min(b0,c[1]);b1=Math.Max(b1,c[1]);if(c[0]==i0)left=true;if(c[0]==i1)right=true;if(c[1]==j0)low=true;if(c[1]==j1)high=true;
     foreach(var d in new[]{new[]{1,0},new[]{-1,0},new[]{0,1},new[]{0,-1}}){int u=c[0]+d[0],v=c[1]+d[1];if(u<i0||u>i1||v<j0||v>j1||seen[u,v]||p.Inside[u,v])continue;seen[u,v]=true;stack.Push(new[]{u,v});}}
    if(!(left||right||low||high))continue;/* an enclosed hole is an opening, not a re-entrant corner */
    double dx=p.Xs[a1+1]-p.Xs[a0],dz=p.Zs[b1+1]-p.Zs[b0],px,pz;
    if((left||right)&&(low||high)){px=dx;pz=dz;}else if(low||high){pz=dz;px=Math.Min(p.Xs[a0]-p.Xs[i0],p.Xs[i1+1]-p.Xs[a1+1]);}else{px=dx;pz=Math.Min(p.Zs[b0]-p.Zs[j0],p.Zs[j1+1]-p.Zs[b1+1]);}
    var r=new ReEntrantResult{Level=p.Level,ProjX=px,PlanX=planX,ProjZ=pz,PlanZ=planZ,RX0=p.Xs[a0],RX1=p.Xs[a1+1],RZ0=p.Zs[b0],RZ1=p.Zs[b1+1],Where="recess X "+F(p.Xs[a0],"0.##")+"-"+F(p.Xs[a1+1],"0.##")+", Z "+F(p.Zs[b0],"0.##")+"-"+F(p.Zs[b1+1],"0.##")+" m at "+F(p.Level,"0.##")+" m"};
    if(best==null||r.Worst>best.Worst)best=r;}}
  return best;}
 /* columns that start on a beam instead of a column below (discontinuous vertical elements): out-of-plane offsets / in-plane discontinuity */
 internal static List<string> DiscontinuedColumns(StaadModel m){var list=new List<string>();if(m==null)return list;double k=m.ToMetre;
  var verticals=m.Members.Values.Select(mm=>{StaadJoint a,b;if(!m.Joints.TryGetValue(mm.A,out a)||!m.Joints.TryGetValue(mm.B,out b))return null;if(Math.Abs(m.P1(a)-m.P1(b))>1e-3||Math.Abs(m.P2(a)-m.P2(b))>1e-3||Math.Abs(m.H(a)-m.H(b))<1e-3)return null;return m.H(a)<m.H(b)?new[]{a,b}:new[]{b,a};}).Where(x=>x!=null).ToList();
  if(verticals.Count==0)return list;double baseH=verticals.Min(v=>m.H(v[0]));var tops=new HashSet<int>(verticals.Select(v=>v[1].No));
  foreach(var v in verticals.Where(v=>m.H(v[0])>baseH+1e-3&&!tops.Contains(v[0].No)).GroupBy(v=>v[0].No).Select(g=>g.First()))list.Add("joint "+v[0].No+" ("+F(m.P1(v[0])*k,"0.##")+", "+F(m.P2(v[0])*k,"0.##")+") at "+F(m.H(v[0])*k,"0.##")+" m");
  return list;}
 internal static List<IrregularityRow> Check(StaadModel m,Dictionary<string,double[]> forces,Dictionary<string,double[]> disp,List<StaadPrimary> eq,List<AxResult> ax,IrregularityInput inp){var rows=new List<IrregularityRow>();
  Action<string,string,string,string,string> add=(t,type,crit,val,res)=>rows.Add(new IrregularityRow{Table=t,Type=type,Criterion=crit,Value=val,Result=res});
  /* from the STAAD model: floor plans, re-entrant corners, floor area, columns that start on beams */
  var plans=m!=null?WindEdgeLoads.FloorPlans(m):new List<WindEdgeLoads.FloorPlan>();double autoArea;var re=ReEntrant(plans,out autoArea);var broken=DiscontinuedColumns(m);
  /* vertical 1: stiffness, soft storey */
  if(m!=null&&forces!=null&&disp!=null&&eq.Count>0){var bad=new List<string>();var vals=new List<string>();
   foreach(var p in eq.GroupBy(e=>e.Axis).Select(g=>g.First())){List<double[]> k;try{k=Stiffness(m,forces,disp,p.No,p.Axis);}catch(Exception){continue;}
    for(int i=0;i<k.Count;i++){var above=k.Skip(i+1).Take(3).Select(x=>x[4]).ToList();if(above.Count==0)continue;double r1=k[i][4]/above[0];double r3=above.Count==3?k[i][4]/above.Average():double.NaN;
     vals.Add(p.Axis+" "+F(k[i][0],"0.##")+" m: "+F(r1*100,"0")+" %"+(double.IsNaN(r3)?"":" / "+F(r3*100,"0")+" %"));if(r1<0.7||(!double.IsNaN(r3)&&r3<0.8))bad.Add(p.Axis+" storey at "+F(k[i][0],"0.##")+" m");}}
   add("208-9","1 Stiffness irregularity - soft storey","k < 70 % of the storey above or < 80 % of the average of the three storeys above",vals.Count==0?"-":string.Join("; ",vals),vals.Count==0?NotChecked:bad.Count>0?Irregular+" ("+string.Join(", ",bad)+")":Regular);}
  else add("208-9","1 Stiffness irregularity - soft storey","k < 70 % of the storey above or < 80 % of the average of the three storeys above","needs the STAAD input, EQ member end forces (ρ tab) and joint displacements (Ax tab)",NotChecked);
  /* vertical 2: weight (mass) */
  {var w=inp.Weights.Replace("\r","").Split('\n').Select(l=>l.Split(new[]{';',',','\t',' '},StringSplitOptions.RemoveEmptyEntries)).Where(t=>t.Length>=2).Select(t=>{double h,v;return double.TryParse(t[0],NumberStyles.Float,Ci,out h)&&double.TryParse(t[1],NumberStyles.Float,Ci,out v)?new[]{h,v}:null;}).Where(x=>x!=null).OrderBy(x=>x[0]).ToList();
   if(w.Count<2)add("208-9","2 Weight (mass) irregularity","effective mass of a storey > 150 % of an adjacent storey (a lighter roof excepted)","type the storey weights (level m; weight kN)",NotChecked);
   else{var bad=new List<string>();for(int i=0;i+1<w.Count;i++){double lo=Math.Min(w[i][1],w[i+1][1]),hi=Math.Max(w[i][1],w[i+1][1]);bool roofLighter=i+1==w.Count-1&&w[i+1][1]<w[i][1];if(!roofLighter&&hi>1.5*lo)bad.Add(F(w[i][0],"0.##")+" / "+F(w[i+1][0],"0.##")+" m ("+F(hi/lo*100,"0")+" %)");}
    add("208-9","2 Weight (mass) irregularity","effective mass of a storey > 150 % of an adjacent storey (a lighter roof excepted)",string.Join("; ",w.Select(x=>F(x[0],"0.##")+" m: "+F(x[1],"0.#")+" kN")),bad.Count>0?Irregular+" ("+string.Join(", ",bad)+")":Regular);}}
  /* vertical 3: geometry */
  if(m!=null){var d=PlanDimensions(m);var bad=new List<string>();string pent="";for(int i=0;i+1<d.Count;i++){/* a one-storey rooftop structure (penthouse, machine room) at the top, under half the plan of the storey below, is not counted */if(i+1==d.Count-1&&d[i+1][1]<0.5*d[i][1]&&d[i+1][2]<0.5*d[i][2]){pent="; the rooftop structure at "+F(d[i+1][0],"0.##")+" m ("+F(d[i+1][1],"0.##")+" x "+F(d[i+1][2],"0.##")+" m) is a penthouse and not counted";continue;}foreach(int c in new[]{1,2}){double a=d[i][c],b=d[i+1][c];if(Math.Min(a,b)>1e-6&&Math.Max(a,b)>1.3*Math.Min(a,b))bad.Add((c==1?"X":"Z")+" "+F(d[i][0],"0.##")+" / "+F(d[i+1][0],"0.##")+" m ("+F(Math.Max(a,b)/Math.Min(a,b)*100,"0")+" %)");}}
   add("208-9","3 Vertical geometric irregularity","plan dimension of the lateral system > 130 % of an adjacent storey",string.Join("; ",d.Select(x=>F(x[0],"0.##")+" m: "+F(x[1],"0.##")+" x "+F(x[2],"0.##")+" m"))+pent,bad.Count>0?Irregular+" ("+string.Join(", ",bad)+")":Regular);}
  else add("208-9","3 Vertical geometric irregularity","plan dimension of the lateral system > 130 % of an adjacent storey","needs the STAAD input (ρ tab)",NotChecked);
  add("208-9","4 In-plane discontinuity in vertical lateral-force-resisting elements","in-plane offset of the lateral elements greater than their length",(inp.InPlane?"marked present":"marked absent")+(broken.Count>0?"; "+broken.Count+" column(s) start on beams - check whether they are offset in the plane of their frame":""),inp.InPlane?Irregular:Regular);rows[rows.Count-1].Detail=broken.Count>0?"Columns that start on a beam (from the STAAD model):\n"+string.Join("\n",broken):"No column starts on a beam in the STAAD model.";
  add("208-9","5 Discontinuity in capacity - weak storey","storey strength < 80 % of the storey above",inp.WeakStory?"marked present":"marked absent",inp.WeakStory?Irregular:Regular);
  /* plan 1: torsion */
  /* Table 208-10 type 1 compares storey DRIFTS at the two ends (not the floor displacements, which Ax uses) */
  var tdr=ax==null?new List<Tuple<AxResult,double[]>>():ax.SelectMany(r=>TorsionDrifts(r).Select(d=>Tuple.Create(r,d))).ToList();
  if(tdr.Count>0){var g=tdr.OrderByDescending(x=>x.Item2[5]).First();add("208-10","1 Torsional irregularity","maximum storey drift at one end > 1.2 times the average of the storey drifts of the two ends","drift max / avg = "+F(g.Item2[5])+" ("+g.Item1.Case+", storey to "+F(g.Item2[0],"0.##")+" m)",g.Item2[5]>1.2?Irregular:Regular);}
  else add("208-10","1 Torsional irregularity","maximum storey drift at one end > 1.2 times the average of the two ends","needs the joint displacements (Ax tab)",NotChecked);
  /* plan 2: re-entrant corners */
  if(inp.PlanX>0&&inp.PlanZ>0){double rx=inp.ProjX/inp.PlanX,rz=inp.ProjZ/inp.PlanZ;add("208-10","2 Re-entrant corners","both projections beyond a re-entrant corner > 15 % of the plan dimension in that direction","typed: X "+F(inp.ProjX,"0.##")+" / "+F(inp.PlanX,"0.##")+" m = "+F(rx*100,"0")+" %, Z "+F(inp.ProjZ,"0.##")+" / "+F(inp.PlanZ,"0.##")+" m = "+F(rz*100,"0")+" %",rx>0.15&&rz>0.15?Irregular:Regular);}
  else if(plans.Count>0){if(re==null)add("208-10","2 Re-entrant corners","both projections beyond a re-entrant corner > 15 % of the plan dimension in that direction","no recess in the floor plans (STAAD model)",Regular);
   else{double rx=re.ProjX/re.PlanX,rz=re.ProjZ/re.PlanZ;add("208-10","2 Re-entrant corners","both projections beyond a re-entrant corner > 15 % of the plan dimension in that direction","from the STAAD plan, "+re.Where+": X "+F(re.ProjX,"0.##")+" / "+F(re.PlanX,"0.##")+" m = "+F(rx*100,"0")+" %, Z "+F(re.ProjZ,"0.##")+" / "+F(re.PlanZ,"0.##")+" m = "+F(rz*100,"0")+" %",rx>0.15&&rz>0.15?Irregular:Regular);}}
  else add("208-10","2 Re-entrant corners","both projections > 15 % of the plan dimension","type the projections and plan dimensions (0 = none)",inp.PlanX>0||inp.PlanZ>0?NotChecked:NotChecked);
  /* plan 3: diaphragm */
  double floorArea=inp.FloorArea>0?inp.FloorArea:autoArea;if(floorArea>0&&inp.Opening>0){double r=inp.Opening/floorArea;add("208-10","3 Diaphragm discontinuity","cut-out or open area > 50 % of the gross enclosed diaphragm area","openings "+F(inp.Opening,"0.##")+" m² of "+F(floorArea,"0.##")+" m²"+(inp.FloorArea>0?"":" (floor area from the STAAD plan)")+" = "+F(r*100,"0")+" %",r>0.5?Irregular:Regular);}
  else if(floorArea>0)add("208-10","3 Diaphragm discontinuity","cut-out or open area > 50 % of the gross enclosed diaphragm area","floor area "+F(floorArea,"0.##")+" m²"+(inp.FloorArea>0?"":" from the STAAD plan")+"; type the openings (stairs, shafts, voids) if any",inp.Opening>0?NotChecked:Regular);
  else add("208-10","3 Diaphragm discontinuity","cut-out or open area > 50 % of the gross diaphragm area","type the opening and floor areas",NotChecked);
  add("208-10","4 Out-of-plane offsets","discontinuity in the lateral force path (out-of-plane offsets of vertical elements)",inp.OutOfPlane?"marked present":m==null?"needs the STAAD input (ρ tab)":broken.Count>0?broken.Count+" column(s) start on beams (STAAD model)":"every column continues to the base (STAAD model)",inp.OutOfPlane||broken.Count>0?Irregular:m==null?NotChecked:Regular);rows[rows.Count-1].Detail=broken.Count>0?"Columns that start on a beam:\n"+string.Join("\n",broken):"";
  if(m!=null){int n=NonParallel(m);add("208-10","5 Nonparallel systems","vertical lateral elements not parallel to or symmetric about the major orthogonal axes",n==0?"all beams along X / Z":n+" horizontal members off the X / Z axes",n>0?Irregular:Regular);}
  else add("208-10","5 Nonparallel systems","vertical lateral elements not parallel to the major axes","needs the STAAD input (ρ tab)",NotChecked);
  Details(rows,m,forces,disp,eq,ax,inp,plans,re,broken);
  return rows;}
 /* storey drifts at the two edges of each level of an EQ case: {level, drift 1, drift 2, max, avg, max / avg}; the floor below is the previous level
    (the base has no drift) */
 internal static List<double[]> TorsionDrifts(AxResult r){var res=new List<double[]>();double p1=0,p2=0;foreach(var l in r.Levels.OrderBy(x=>x.Level)){double d1=Math.Abs(l.D1-p1),d2=Math.Abs(l.D2-p2);p1=l.D1;p2=l.D2;double avg=(d1+d2)/2;if(avg<=1e-12)continue;double mx=Math.Max(d1,d2);res.Add(new[]{l.Level,d1,d2,mx,avg,mx/avg});}return res;}
 /* the tables, calculations and figures of each check for the report */
 static void Details(List<IrregularityRow> rows,StaadModel m,Dictionary<string,double[]> forces,Dictionary<string,double[]> disp,List<StaadPrimary> eq,List<AxResult> ax,IrregularityInput inp,List<WindEdgeLoads.FloorPlan> plans,ReEntrantResult re,List<string> broken){
  Func<string,IrregularityRow> row=t=>rows.FirstOrDefault(r=>r.Type.StartsWith(t));
  /* 208-9 type 1: storey stiffness per direction */
  var soft=row("1 Stiffness");if(soft!=null&&m!=null&&forces!=null&&disp!=null&&eq.Count>0){soft.Head=new[]{"Dir.","Storey (m)","V","Drift","k = V / drift","k / k above","k / avg 3 above","Result"};soft.Widths=new float[]{6,15,11,11,13,13,15,16};
   var bars=new List<Tuple<string,string,double,bool>>();
   foreach(var p in eq.GroupBy(e=>e.Axis).Select(g=>g.First())){List<double[]> k;try{k=Stiffness(m,forces,disp,p.No,p.Axis);}catch(Exception){continue;}
    for(int i=0;i<k.Count;i++){var above=k.Skip(i+1).Take(3).Select(x=>x[4]).ToList();double r1=above.Count>0?k[i][4]/above[0]:double.NaN,r3=above.Count==3?k[i][4]/above.Average():double.NaN;bool bad=r1<0.7||r3<0.8;
     string st=F(k[i][0],"0.##")+" - "+F(k[i][1],"0.##");soft.Rows.Add(new[]{p.Axis,st,F(k[i][2],"0.#"),F(k[i][3],"0.#####"),F(k[i][4],"0.#"),double.IsNaN(r1)?"top storey":F(r1*100,"0")+" %",double.IsNaN(r3)?"-":F(r3*100,"0")+" %",double.IsNaN(r1)?"-":bad?Irregular:Regular});
     soft.Calcs.Add(new[]{p.Axis+", storey "+st+" m","k = V / drift = "+F(k[i][2],"0.#")+" / "+F(k[i][3],"0.#####"),F(k[i][4],"0.#")});
     if(!double.IsNaN(r1))soft.Calcs.Add(new[]{"","k / k above = "+F(k[i][4],"0.#")+" / "+F(above[0],"0.#")+(double.IsNaN(r3)?"":"; k / avg = "+F(k[i][4],"0.#")+" / "+F(above.Average(),"0.#")),F(r1*100,"0")+" %"+(double.IsNaN(r3)?"":" / "+F(r3*100,"0")+" %")+(bad?" < 70 % / 80 %":" ≥ 70 % / 80 %")});
     bars.Add(Tuple.Create(p.Axis,st,k[i][4],bad));}}
   soft.Caption="Storey stiffness k per direction (bars to scale within each direction; red = soft storey). V = sum of the column / brace shears cut at mid-storey (kN), drift = mean floor displacement above minus below (units of the STAAD output).";
   if(m.Plates)soft.Calcs.Add(new[]{"Note","the model has plate (wall) elements: their shear is not in the member end forces, so V and k here are of the columns and braces only","compare with STAAD CHECK SOFT STORY"});
   if(bars.Count>0){var b=bars;soft.FigureH=40+18*b.Count;soft.Figure=(g,rc)=>{var axes=b.Select(x=>x.Item1).Distinct().ToList();float y=rc.Top+4;using(var f=new Font("Segoe UI",7f))using(var fb=new Font("Segoe UI",7f,FontStyle.Bold)){
     foreach(var a in axes){var part=b.Where(x=>x.Item1==a).ToList();double max=part.Max(x=>x.Item3);g.DrawString("Direction "+a,fb,Brushes.Black,rc.Left,y);y+=14;
      foreach(var x in part.AsEnumerable().Reverse()){float w=(float)((rc.Width-200)*x.Item3/max);using(var br=new SolidBrush(x.Item4?Color.FromArgb(214,80,72):Color.FromArgb(70,140,150))){g.FillRectangle(br,rc.Left+110,y,Math.Max(1,w),11);}
       g.DrawString(x.Item2+" m",f,Brushes.Black,rc.Left,y-1);g.DrawString(F(x.Item3,"0.#"),f,Brushes.Black,rc.Left+114+w,y-1);y+=16;}y+=4;}}};}}
  /* 208-9 type 2: weights */
  var wt=row("2 Weight");if(wt!=null){var w=inp.Weights.Replace("\r","").Split('\n').Select(l=>l.Split(new[]{';',',','\t',' '},StringSplitOptions.RemoveEmptyEntries)).Where(t=>t.Length>=2).Select(t=>{double h,v;return double.TryParse(t[0],NumberStyles.Float,Ci,out h)&&double.TryParse(t[1],NumberStyles.Float,Ci,out v)?new[]{h,v}:null;}).Where(x=>x!=null).OrderBy(x=>x[0]).ToList();
   if(w.Count>=2){wt.Head=new[]{"Level (m)","Weight (kN)","Ratio to the storey below","Ratio to the storey above"};wt.Widths=new float[]{18,20,31,31};
    for(int i=0;i<w.Count;i++){string dn=i>0?F(w[i][1]/w[i-1][1]*100,"0")+" %":"-",up=i+1<w.Count?F(w[i][1]/w[i+1][1]*100,"0")+" %":"-";wt.Rows.Add(new[]{F(w[i][0],"0.##"),F(w[i][1],"0.#"),dn,up});}
    for(int i=0;i+1<w.Count;i++){double hi=Math.Max(w[i][1],w[i+1][1]),lo=Math.Min(w[i][1],w[i+1][1]);wt.Calcs.Add(new[]{F(w[i][0],"0.##")+" / "+F(w[i+1][0],"0.##")+" m","W max / W min = "+F(hi,"0.#")+" / "+F(lo,"0.#"),F(hi/lo*100,"0")+" %"+(hi>1.5*lo?" > 150 %":" ≤ 150 %")});}}}
  /* 208-9 type 3: plan dimensions per storey */
  var vg=row("3 Vertical geometric");if(vg!=null&&m!=null){var d=PlanDimensions(m);if(d.Count>0){vg.Head=new[]{"Storey from (m)","X (m)","Z (m)","X / X below","Z / Z below"};vg.Widths=new float[]{20,20,20,20,20};
    for(int i=0;i<d.Count;i++)vg.Rows.Add(new[]{F(d[i][0],"0.##"),F(d[i][1],"0.##"),F(d[i][2],"0.##"),i>0&&d[i-1][1]>0?F(d[i][1]/d[i-1][1]*100,"0")+" %":"-",i>0&&d[i-1][2]>0?F(d[i][2]/d[i-1][2]*100,"0")+" %":"-"});
    for(int i=0;i+1<d.Count;i++)foreach(int c in new[]{1,2}){double hi=Math.Max(d[i][c],d[i+1][c]),lo=Math.Min(d[i][c],d[i+1][c]);if(lo<=0)continue;vg.Calcs.Add(new[]{(c==1?"X":"Z")+", storeys at "+F(d[i][0],"0.##")+" / "+F(d[i+1][0],"0.##")+" m","larger / smaller = "+F(hi,"0.##")+" / "+F(lo,"0.##"),F(hi/lo*100,"0")+" %"+(hi>1.3*lo?" > 130 %":" ≤ 130 %")});}
    var dd=d;vg.FigureH=150;vg.Caption="Elevation: plan dimension X of the lateral system per storey (to scale).";
    vg.Figure=(g,rc)=>{double mx=dd.Max(x=>x[1]);float h=(rc.Height-20)/dd.Count;using(var f=new Font("Segoe UI",7f))for(int i=0;i<dd.Count;i++){float w=(float)((rc.Width-120)*dd[i][1]/mx);float y=rc.Bottom-10-(i+1)*h;var r=new RectangleF(rc.Left+100+(rc.Width-120-w)/2,y,w,h-3);using(var br=new SolidBrush(Color.FromArgb(225,235,236)))g.FillRectangle(br,r);g.DrawRectangle(Pens.SlateGray,r.X,r.Y,r.Width,r.Height);g.DrawString(F(dd[i][0],"0.##")+" m: "+F(dd[i][1],"0.##")+" m",f,Brushes.Black,rc.Left,y+h/2-6);}};}}
  /* 208-10 type 1: storey drifts at the two ends per storey */
  var tor=row("1 Torsional");if(tor!=null&&ax!=null&&ax.Count>0){tor.Head=new[]{"Load case","Storey to (m)","drift end 1","drift end 2","drift max","drift avg","max / avg","Result"};tor.Widths=new float[]{20,10,11,11,11,11,12,14};
   Tuple<AxResult,double[]> gv=null;foreach(var r in ax)foreach(var d in TorsionDrifts(r)){tor.Rows.Add(new[]{r.Case,F(d[0],"0.##"),F(d[1]),F(d[2]),F(d[3]),F(d[4]),F(d[5]),d[5]>1.2?Irregular:Regular});if(gv==null||d[5]>gv.Item2[5])gv=Tuple.Create(r,d);}
   if(gv!=null){tor.Calcs.Add(new[]{"Governing: "+gv.Item1.Case+", storey to "+F(gv.Item2[0],"0.##")+" m","drift avg = (Δ1 + Δ2) / 2 = ("+F(gv.Item2[1])+" + "+F(gv.Item2[2])+") / 2",F(gv.Item2[4])});
    tor.Calcs.Add(new[]{"","drift max / drift avg = "+F(gv.Item2[3])+" / "+F(gv.Item2[4]),F(gv.Item2[5])+(gv.Item2[5]>1.2?" > 1.2":" ≤ 1.2")});}
   tor.Caption="";}
  /* 208-10 type 2: re-entrant corner with its plan */
  var rc0=row("2 Re-entrant");if(rc0!=null&&re!=null&&re.PlanX>0&&re.PlanZ>0&&inp.PlanX<=0){rc0.Calcs.Add(new[]{"X","projection / plan = "+F(re.ProjX,"0.##")+" / "+F(re.PlanX,"0.##"),F(re.ProjX/re.PlanX*100,"0")+" %"});
   rc0.Calcs.Add(new[]{"Z","projection / plan = "+F(re.ProjZ,"0.##")+" / "+F(re.PlanZ,"0.##"),F(re.ProjZ/re.PlanZ*100,"0")+" %"});
   rc0.Calcs.Add(new[]{"","both projections > 15 % ?",re.Worst>0.15?"yes - "+Irregular:"no - "+Regular});
   var pl=plans.FirstOrDefault(p=>Math.Abs(p.Level-re.Level)<1e-6);if(pl!=null){var rr=re;rc0.FigureH=200;rc0.Caption="Floor plan at "+F(re.Level,"0.##")+" m (grey) and the recess checked (orange).";rc0.Figure=(g,rc)=>DrawPlan(g,rc,pl,rr);}}
  /* 208-10 type 3: diaphragm */
  var dia=row("3 Diaphragm");if(dia!=null){double area=inp.FloorArea>0?inp.FloorArea:plans.Count>0?plans.Max(p=>p.Area):0;if(area>0)dia.Calcs.Add(new[]{"Openings","open area / gross area = "+F(inp.Opening,"0.##")+" / "+F(area,"0.##"),F(inp.Opening/area*100,"0")+" %"+(inp.Opening>0.5*area?" > 50 %":" ≤ 50 %")});}
  /* 208-10 type 4: columns on beams */
  var oop=row("4 Out-of-plane");if(oop!=null&&broken.Count>0){oop.Head=new[]{"Column that starts on a beam (STAAD model)"};oop.Widths=new float[]{100};foreach(var b in broken)oop.Rows.Add(new[]{b});}
  /* 208-10 type 5: nonparallel */
  var np=row("5 Nonparallel");if(np!=null&&m!=null){int n=NonParallel(m);np.Calcs.Add(new[]{"Horizontal members off the X / Z axes","plan angle not a multiple of 90° (± 2°)",n+(n>0?" - "+Irregular:" - "+Regular)});}
  Recommend(rows,m);}
 /* recommendations for each irregularity found; the clause references follow NSCP 2015 Section 208 (UBC 1997 Chapter 16 numbering in brackets) -
    check them against the code text */
 static void Recommend(List<IrregularityRow> rows,StaadModel m){double h=0;int storeys=0;if(m!=null&&m.Joints.Count>0){double g=WindEdgeLoads.GroundLevel(m);h=m.Joints.Values.Max(j=>m.H(j))*m.ToMetre-g;storeys=Math.Max(0,PlanDimensions(m).Count(d=>d[0]>=-1e-6));}
  bool tall=h>20||storeys>5;string dyn=" Static or dynamic: structures over 5 storeys or 20 m with a vertical irregularity of type 1, 2 or 3 need a dynamic (response spectrum) analysis in Zone 4 (NSCP 2015 208.4.8.3 [UBC 1629.8.3]); this model is "+F(h,"0.#")+" m high with "+storeys+" storeys"+(m==null?"":tall?" - use the dynamic analysis (Earthquake sheet, response spectrum).":" - the static lateral force procedure remains allowed.");
  foreach(var r in rows){if(!r.Result.StartsWith(Irregular))continue;string t=r.Type;
   if(t.StartsWith("1 Stiffness"))r.Recommend="Stiffen the soft storey: enlarge its columns, add shear walls or braces in that storey, or bring its height closer to the storeys above; then run STAAD again and recheck."+dyn;
   else if(t.StartsWith("2 Weight"))r.Recommend="Balance the storey masses: move heavy equipment, water tanks or storage off the heavy storey, use lighter partitions or finishes there, or stiffen and strengthen that storey for the added mass."+dyn;
   else if(t.StartsWith("3 Vertical"))r.Recommend="Avoid an abrupt set-back of the lateral system: continue the frames or walls of the larger storey down / up, or separate the parts with a seismic joint; design the set-back storey and its diaphragm for the transfer of forces."+dyn;
   else if(t.StartsWith("4 In-plane"))r.Recommend="Elements that support the offset wall or frame (beams, transfer girders, the columns under them) are designed for the special seismic load combinations with Em = Ω0 Eh (group 3000, Eq. 203-19 / 203-20; NSCP 2015 208.7 [UBC 1630.8.2]). Better: align the lateral elements down to the foundation.";
   else if(t.StartsWith("5 Discontinuity"))r.Recommend="Strengthen the weak storey to at least 80 % of the storey above. While it stays weak the structure is limited to two storeys or 9 m unless the weak storey resists Ω0 times the design forces (NSCP 2015 208.4 [UBC 1629.9.1]).";
   else if(t.StartsWith("1 Torsional"))r.Recommend="Reduce the eccentricity between the centre of mass and the centre of rigidity: place walls or braces symmetrically, preferably at the perimeter, or move the stiff core toward the centre. While it remains, the accidental torsion is amplified by Ax (Torsional amplification tab, ACC = 0.05 Ax; NSCP 2015 208.5.7.6) and the orthogonal seismic load cases (C) are used.";
   else if(t.StartsWith("2 Re-entrant"))r.Recommend="Design the diaphragm chords and collectors at the re-entrant corner for the transfer of forces between the wings, or separate the wings with a seismic joint, or add a wall / frame along the re-entrant edges (NSCP 2015 208.7 diaphragm and collector requirements [UBC 1633.2.9]).";
   else if(t.StartsWith("3 Diaphragm"))r.Recommend="Provide chords, collectors and edge beams around the large openings, check the in-plane diaphragm forces and shears around them, or reduce the openings; consider a flexible diaphragm in the STAAD model for that floor.";
   else if(t.StartsWith("4 Out-of-plane"))r.Recommend="Columns or walls that start on a beam: the supporting beams / transfer girders and the columns under them are designed for the special seismic combinations Em = Ω0 Eh (group 3000, Eq. 203-19 / 203-20; NSCP 2015 208.7 [UBC 1630.8.2]). Better: carry the columns down to the foundation.";
   else if(t.StartsWith("5 Nonparallel"))r.Recommend="Use the orthogonal seismic load cases (100 % of one direction + 30 % of the other: choice C on the Primary loads tab) so the elements not parallel to X / Z get both directions (NSCP 2015 208.7 orthogonal effects [UBC 1633.1]).";}}
 static void DrawPlan(Graphics g,RectangleF rc,WindEdgeLoads.FloorPlan p,ReEntrantResult re){double x0=p.Xs.First(),x1=p.Xs.Last(),z0=p.Zs.First(),z1=p.Zs.Last();float s=(float)Math.Min((rc.Width-20)/(x1-x0),(rc.Height-30)/(z1-z0));
  Func<double,float> X=x=>rc.Left+10+(float)((x-x0)*s);Func<double,float> Z=z=>rc.Top+10+(float)((z-z0)*s);
  using(var grey=new SolidBrush(Color.FromArgb(214,220,222)))for(int i=0;i+1<p.Xs.Count;i++)for(int j=0;j+1<p.Zs.Count;j++)if(p.Inside[i,j])g.FillRectangle(grey,X(p.Xs[i]),Z(p.Zs[j]),X(p.Xs[i+1])-X(p.Xs[i])+0.5f,Z(p.Zs[j+1])-Z(p.Zs[j])+0.5f);
  using(var o=new SolidBrush(Color.FromArgb(150,230,140,40)))g.FillRectangle(o,X(re.RX0),Z(re.RZ0),X(re.RX1)-X(re.RX0),Z(re.RZ1)-Z(re.RZ0));
  g.DrawRectangle(Pens.DimGray,X(x0),Z(z0),X(x1)-X(x0),Z(z1)-Z(z0));using(var f=new Font("Segoe UI",7f))g.DrawString("X →   Z ↓   projections X "+F(re.ProjX,"0.##")+" m of "+F(re.PlanX,"0.##")+" m, Z "+F(re.ProjZ,"0.##")+" m of "+F(re.PlanZ,"0.##")+" m",f,Brushes.Black,rc.Left+10,Z(z1)+4);}
 /* the suggested structure class: C irregular (any irregularity), B regular but unsymmetrical (dmax / davg over 1.05), A regular and symmetrical */
 internal static string Suggest(List<IrregularityRow> rows,List<AxResult> ax,out string why){
  var irr=rows.Where(r=>r.Result.StartsWith(Irregular)).ToList();if(irr.Count>0){why="irregular: "+string.Join(", ",irr.Select(r=>"Table "+r.Table+" type "+r.Type.Split(' ')[0]));return "C";}
  double ratio=ax!=null&&ax.Count>0?ax.SelectMany(r=>r.Levels).Max(l=>l.Ratio):double.NaN;
  if(double.IsNaN(ratio)){why="no irregularity found; paste the joint displacements (Ax tab) to tell symmetrical from unsymmetrical";return "";}
  if(ratio>1.05){why="regular, but dmax / davg = "+F(ratio)+" > 1.05: unsymmetrical (natural torsion)";return "B";}
  why="regular and dmax / davg = "+F(ratio)+" ≤ 1.05: symmetrical";return "A";}
}
}
