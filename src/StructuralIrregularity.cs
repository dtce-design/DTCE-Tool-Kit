using System;using System.Collections.Generic;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
/* Structural irregularities, NSCP 2015 Table 208-9 (vertical) and Table 208-10 (plan) (owner, 26 November 2026: "for structural
   irregularities check"). From the STAAD model, the EQ member end forces and joint displacements where they can be computed; the rest from
   short inputs. Each row: Regular / Irregular / Not checked. */
sealed class IrregularityRow {public string Table,Type,Criterion,Value,Result;}
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
  foreach(var g in cols.GroupBy(c=>Math.Round(Math.Min(m.H(c.a),m.H(c.b)),3)).OrderBy(g=>g.Key)){var pts=g.SelectMany(c=>new[]{c.a,c.b}).ToList();
   res.Add(new[]{(g.Key-baseH)*m.ToMetre,(pts.Max(j=>m.P1(j))-pts.Min(j=>m.P1(j)))*m.ToMetre,(pts.Max(j=>m.P2(j))-pts.Min(j=>m.P2(j)))*m.ToMetre});}
  return res;}
 /* horizontal members not parallel to the X or Z axis (plan angle not a multiple of 90 degrees) */
 internal static int NonParallel(StaadModel m){int n=0;foreach(var mm in m.Members.Values){var a=m.Joints[mm.A];var b=m.Joints[mm.B];if(Math.Abs(m.H(b)-m.H(a))>1e-6)continue;double dx=m.P1(b)-m.P1(a),dz=m.P2(b)-m.P2(a);if(Math.Sqrt(dx*dx+dz*dz)<1e-6)continue;
   double ang=Math.Abs(Math.Atan2(dz,dx)*180/Math.PI)%90;if(ang>2&&ang<88)n++;}return n;}
 internal static List<IrregularityRow> Check(StaadModel m,Dictionary<string,double[]> forces,Dictionary<string,double[]> disp,List<StaadPrimary> eq,List<AxResult> ax,IrregularityInput inp){var rows=new List<IrregularityRow>();
  Action<string,string,string,string,string> add=(t,type,crit,val,res)=>rows.Add(new IrregularityRow{Table=t,Type=type,Criterion=crit,Value=val,Result=res});
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
  if(m!=null){var d=PlanDimensions(m);var bad=new List<string>();for(int i=0;i+1<d.Count;i++)foreach(int c in new[]{1,2}){double a=d[i][c],b=d[i+1][c];if(Math.Min(a,b)>1e-6&&Math.Max(a,b)>1.3*Math.Min(a,b))bad.Add((c==1?"X":"Z")+" "+F(d[i][0],"0.##")+" / "+F(d[i+1][0],"0.##")+" m ("+F(Math.Max(a,b)/Math.Min(a,b)*100,"0")+" %)");}
   add("208-9","3 Vertical geometric irregularity","plan dimension of the lateral system > 130 % of an adjacent storey",string.Join("; ",d.Select(x=>F(x[0],"0.##")+" m: "+F(x[1],"0.##")+" x "+F(x[2],"0.##")+" m")),bad.Count>0?Irregular+" ("+string.Join(", ",bad)+")":Regular);}
  else add("208-9","3 Vertical geometric irregularity","plan dimension of the lateral system > 130 % of an adjacent storey","needs the STAAD input (ρ tab)",NotChecked);
  add("208-9","4 In-plane discontinuity in vertical lateral-force-resisting elements","in-plane offset of the lateral elements greater than their length",inp.InPlane?"marked present":"marked absent",inp.InPlane?Irregular:Regular);
  add("208-9","5 Discontinuity in capacity - weak storey","storey strength < 80 % of the storey above",inp.WeakStory?"marked present":"marked absent",inp.WeakStory?Irregular:Regular);
  /* plan 1: torsion */
  if(ax!=null&&ax.Count>0){var g=ax.SelectMany(r=>r.Levels.Select(l=>new{r,l})).OrderByDescending(x=>x.l.Ratio).First();add("208-10","1 Torsional irregularity","dmax > 1.2 davg at a level (displacements at the two edges)","dmax / davg = "+F(g.l.Ratio)+" ("+g.r.Case+", "+F(g.l.Level,"0.##")+" m)",g.l.Ratio>1.2?Irregular:Regular);}
  else add("208-10","1 Torsional irregularity","dmax > 1.2 davg at a level","needs the joint displacements (Ax tab)",NotChecked);
  /* plan 2: re-entrant corners */
  if(inp.PlanX>0&&inp.PlanZ>0){double rx=inp.ProjX/inp.PlanX,rz=inp.ProjZ/inp.PlanZ;add("208-10","2 Re-entrant corners","both projections beyond a re-entrant corner > 15 % of the plan dimension in that direction","X "+F(rx*100,"0")+" %, Z "+F(rz*100,"0")+" %",rx>0.15&&rz>0.15?Irregular:Regular);}
  else add("208-10","2 Re-entrant corners","both projections > 15 % of the plan dimension","type the projections and plan dimensions (0 = none)",inp.PlanX>0||inp.PlanZ>0?NotChecked:NotChecked);
  /* plan 3: diaphragm */
  if(inp.FloorArea>0){double r=inp.Opening/inp.FloorArea;add("208-10","3 Diaphragm discontinuity","cut-out or open area > 50 % of the gross enclosed diaphragm area","openings "+F(r*100,"0")+" %",r>0.5?Irregular:Regular);}
  else add("208-10","3 Diaphragm discontinuity","cut-out or open area > 50 % of the gross diaphragm area","type the opening and floor areas",NotChecked);
  add("208-10","4 Out-of-plane offsets","discontinuity in the lateral force path (out-of-plane offsets of vertical elements)",inp.OutOfPlane?"marked present":"marked absent",inp.OutOfPlane?Irregular:Regular);
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
