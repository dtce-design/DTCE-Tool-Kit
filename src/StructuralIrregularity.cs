using System;using System.Collections.Generic;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
/* Structural irregularities, NSCP 2015 Table 208-9 (vertical) and Table 208-10 (plan) (owner, 26 November 2026: "for structural
   irregularities check"). From the STAAD model, the EQ member end forces and joint displacements where they can be computed; the rest from
   short inputs. Each row: Regular / Irregular / Not checked. */
sealed class IrregularityRow {public string Table,Type,Criterion,Value,Result;/* how it was found, shown on its own tab */public string Detail="";}
sealed class IrregularityInput {public string Weights="";public double ProjX,PlanX,ProjZ,PlanZ,Opening,FloorArea;public bool InPlane,OutOfPlane,WeakStory;}
static class StructuralIrregularity {
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;
 static string F(double v,string f="0.###"){return v.ToString(f,Ci);}
 internal const string Regular="Regular",Irregular="Irregular",NotChecked="Not checked";
 /* storey stiffness k = V / drift: V from the column / brace shears (RedundancyFactor), drift from the mean displacement in the load direction */
 internal static List<double[]> Stiffness(StaadModel m,Dictionary<string,double[]> forces,Dictionary<string,double[]> disp,int load,string axis){var res=new List<double[]>();
  var r=RedundancyFactor.Compute(m,forces,load,load.ToString(Ci),axis,"Moment");double baseH=m.Joints.Values.Min(j=>m.H(j));int comp=axis!="Z"?0:(m.ZUp?1:2);
  Func<double,double> mean=h=>{var v=m.Joints.Values.Where(j=>Math.Abs(m.H(j)-h)<1e-3&&disp.ContainsKey(j.No+"|"+load)).Select(j=>Math.Abs(disp[j.No+"|"+load][comp])).ToList();return v.Count==0?double.NaN:v.Average();};
  foreach(var st in r.Stories.Where(s=>s.Shear>1e-9&&!double.IsNaN(s.Top))){double hb=baseH+st.Bottom/m.ToMetre,ht=baseH+st.Top/m.ToMetre;double db=Math.Abs(hb-baseH)<1e-6?0:mean(hb),dt=mean(ht);double drift=dt-db;
   if(double.IsNaN(drift)||drift<=1e-12)continue;res.Add(new[]{st.Bottom,st.Top,st.Shear,drift,st.Shear/drift});}
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
 internal sealed class ReEntrantResult {public double Level,ProjX,PlanX,ProjZ,PlanZ;public string Where="";public double Worst{get{return PlanX<=0||PlanZ<=0?0:Math.Min(ProjX/PlanX,ProjZ/PlanZ);}}}
 internal static ReEntrantResult ReEntrant(List<WindEdgeLoads.FloorPlan> plans,out double area){ReEntrantResult best=null;area=0;
  foreach(var p in plans){int nx=p.Xs.Count-1,nz=p.Zs.Count-1;int i0=nx,i1=-1,j0=nz,j1=-1;for(int i=0;i<nx;i++)for(int j=0;j<nz;j++)if(p.Inside[i,j]){i0=Math.Min(i0,i);i1=Math.Max(i1,i);j0=Math.Min(j0,j);j1=Math.Max(j1,j);}if(i1<0)continue;
   area=Math.Max(area,p.Area);double planX=p.Xs[i1+1]-p.Xs[i0],planZ=p.Zs[j1+1]-p.Zs[j0];var seen=new bool[nx,nz];
   for(int i=i0;i<=i1;i++)for(int j=j0;j<=j1;j++){if(p.Inside[i,j]||seen[i,j])continue;var stack=new Stack<int[]>();stack.Push(new[]{i,j});seen[i,j]=true;int a0=i,a1=i,b0=j,b1=j;bool left=false,right=false,low=false,high=false;
    while(stack.Count>0){var c=stack.Pop();a0=Math.Min(a0,c[0]);a1=Math.Max(a1,c[0]);b0=Math.Min(b0,c[1]);b1=Math.Max(b1,c[1]);if(c[0]==i0)left=true;if(c[0]==i1)right=true;if(c[1]==j0)low=true;if(c[1]==j1)high=true;
     foreach(var d in new[]{new[]{1,0},new[]{-1,0},new[]{0,1},new[]{0,-1}}){int u=c[0]+d[0],v=c[1]+d[1];if(u<i0||u>i1||v<j0||v>j1||seen[u,v]||p.Inside[u,v])continue;seen[u,v]=true;stack.Push(new[]{u,v});}}
    if(!(left||right||low||high))continue;/* an enclosed hole is an opening, not a re-entrant corner */
    double dx=p.Xs[a1+1]-p.Xs[a0],dz=p.Zs[b1+1]-p.Zs[b0],px,pz;
    if((left||right)&&(low||high)){px=dx;pz=dz;}else if(low||high){pz=dz;px=Math.Min(p.Xs[a0]-p.Xs[i0],p.Xs[i1+1]-p.Xs[a1+1]);}else{px=dx;pz=Math.Min(p.Zs[b0]-p.Zs[j0],p.Zs[j1+1]-p.Zs[b1+1]);}
    var r=new ReEntrantResult{Level=p.Level,ProjX=px,PlanX=planX,ProjZ=pz,PlanZ=planZ,Where="recess X "+F(p.Xs[a0],"0.##")+"-"+F(p.Xs[a1+1],"0.##")+", Z "+F(p.Zs[b0],"0.##")+"-"+F(p.Zs[b1+1],"0.##")+" m at "+F(p.Level,"0.##")+" m"};
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
  if(ax!=null&&ax.Count>0){var g=ax.SelectMany(r=>r.Levels.Select(l=>new{r,l})).OrderByDescending(x=>x.l.Ratio).First();add("208-10","1 Torsional irregularity","dmax > 1.2 davg at a level (displacements at the two edges)","dmax / davg = "+F(g.l.Ratio)+" ("+g.r.Case+", "+F(g.l.Level,"0.##")+" m)",g.l.Ratio>1.2?Irregular:Regular);}
  else add("208-10","1 Torsional irregularity","dmax > 1.2 davg at a level","needs the joint displacements (Ax tab)",NotChecked);
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
  return rows;}
 /* the suggested structure class: C irregular (any irregularity), B regular but unsymmetrical (dmax / davg over 1.05), A regular and symmetrical */
 internal static string Suggest(List<IrregularityRow> rows,List<AxResult> ax,out string why){
  var irr=rows.Where(r=>r.Result.StartsWith(Irregular)).ToList();if(irr.Count>0){why="irregular: "+string.Join(", ",irr.Select(r=>"Table "+r.Table+" type "+r.Type.Split(' ')[0]));return "C";}
  double ratio=ax!=null&&ax.Count>0?ax.SelectMany(r=>r.Levels).Max(l=>l.Ratio):double.NaN;
  if(double.IsNaN(ratio)){why="no irregularity found; paste the joint displacements (Ax tab) to tell symmetrical from unsymmetrical";return "";}
  if(ratio>1.05){why="regular, but dmax / davg = "+F(ratio)+" > 1.05: unsymmetrical (natural torsion)";return "B";}
  why="regular and dmax / davg = "+F(ratio)+" ≤ 1.05: symmetrical";return "A";}
}
}
