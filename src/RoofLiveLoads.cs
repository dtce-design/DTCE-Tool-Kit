using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Text;
namespace BeamScheduleFormatter {
/* the roof live load case LLR on the STAAD model (owner, 27 November 2026: "add roof live load based doon sa staad file, iload at mamili ng
   appropriate load value"): NSCP 2015 Section 205.4, Table 205-3 (Method 1), acting vertically on the horizontal projection.
   - slab roofs: every cell of a level that no higher level covers (WindEdgeLoads.Plans, as the wind roof loads) gets a FLOOR LOAD; its Lr is
     chosen from the cell's area - the panel between the beams, the tributary area of the beams around it - with the flat-roof row (row 1);
   - trussed roofs at any level (over the top floor, a lower wing, a canopy): the top chords of each truss plane get MEMBER LOAD UNI PY (projected on the horizontal) of
     w = Lr x truss spacing; Lr from the steepest top chord (row 1 below 4 in 12, row 2 below 12 in 12, row 3 above) and the truss's
     tributary area = horizontal span x spacing; the ceiling under the trusses takes none.
   Roof decks used as an occupancy (Table 205-1, 18), awnings (row 4) and greenhouses (row 5) cannot be told from the model: the note says so. */
static class RoofLiveLoads {
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;
 static string F(double v){return Math.Round(v,3).ToString("0.###",Ci);}
 internal sealed class Region {public string Kind,Where,Basis;public double Area,Slope,Lr;public int Row;}
 /* Table 205-3: row (1 flat / < 4:12, 2 4:12 to < 12:12, 3 12:12 and over) and tributary area (0-20, 21-60, over 60 m2) */
 internal static double Lr(int row,double area){int col=area<=20?0:area<=60?1:2;return row==1?new[]{1.00,0.75,0.60}[col]:row==2?new[]{0.75,0.70,0.60}[col]:0.60;}
 internal static int RowOf(double slope){return slope<4.0/12-1e-9?1:slope<1-1e-9?2:3;}
 static string AreaBand(double a){return a<=20?"0 to 20 m²":a<=60?"21 to 60 m²":"over 60 m²";}
 internal static string Text(StaadModel m,out string note,out List<Region> regions){note="";regions=new List<Region>();if(m==null||m.Joints.Count==0)return "";
  double k=m.ToMetre;double baseY=m.Joints.Values.Min(j=>m.H(j))*k,topY=m.Joints.Values.Max(j=>m.H(j))*k;string n0;
  var main=WindEdgeLoads.Plans(m,baseY,topY,out n0).Where(p=>p.Y>baseY+1e-3).OrderBy(p=>p.Y).ToList();double topMain=main.Count>0?main[main.Count-1].Y:baseY;
  /* trussed roofs at every level of the model: over the top floor, a lower wing, a canopy or a setback; their ceilings take no roof load */
  string an;var ordered=WindEdgeLoads.Plans(m,baseY,1e9,out an,true).Where(p=>p.Y>baseY+1e-3&&p.Faces.Count>0).OrderBy(p=>p.Y).ToList();
  var covers=new List<double[]>();/* y, x1, x2, z1, z2 */var member=new StringBuilder();var chords=new List<WindEdgeLoads.TrussChord>();var done=new HashSet<double>();
  var levels=ordered.Select(p=>p.Y).Concat(new[]{topMain}).Distinct().OrderBy(y=>y).ToList();
  foreach(var y in levels){double[] cover;var found=WindEdgeLoads.TrussTopChords(m,y,ordered,out cover).Where(c=>!done.Contains(c.No)).ToList();if(found.Count==0)continue;
   foreach(var c in found)done.Add(c.No);chords.AddRange(found);if(cover!=null)covers.Add(new[]{y,cover[0],cover[1],cover[2],cover[3]});
   foreach(var g in found.GroupBy(c=>c.Plane).OrderBy(g=>g.Key)){var list=g.ToList();double span=list.Max(c=>Math.Max(c.U1,c.U2))-list.Min(c=>Math.Min(c.U1,c.U2)),trib=Math.Abs(list[0].Trib);
    double slope=list.Max(c=>Math.Abs(c.U2-c.U1)>1e-6?Math.Abs(c.Y2-c.Y1)/Math.Abs(c.U2-c.U1):0);double area=span*trib;int row=RowOf(slope);double lr=Lr(row,area);
    var r=new Region{Kind="Truss",Where="level "+F(y)+" m, truss "+(list[0].SpanX?"Z = ":"X = ")+F(g.Key)+" m",Area=area,Slope=slope,Row=row,Lr=lr,Basis="span "+F(span)+" m × spacing "+F(trib)+" m = "+F(area)+" m², slope "+F(slope*12)+" in 12"};regions.Add(r);
    member.Append("\n* ").Append(r.Where).Append(": ").Append(r.Basis).Append(", row ").Append(row).Append(", Lr = ").Append(F(lr)).Append(" kPa x ").Append(F(trib)).Append(" m");
    foreach(var c in list)member.Append("\n").Append(c.No).Append(" UNI PY ").Append(F(-lr*trib));}}
  /* slab roofs: cells of every level not covered by a higher level (nor the ceiling under the trusses) */
  var floor=new StringBuilder();int cells=0;
  for(int li=0;li<ordered.Count;li++){var p=ordered[li];var higher=ordered.Skip(li+1).ToList();int nx=p.Xs.Count-1,nz=p.Zs.Count-1;var val=new double?[nx,nz];
   for(int i=0;i<nx;i++)for(int j=0;j<nz;j++){if(!p.Inside[i,j])continue;double cx=(p.Xs[i]+p.Xs[i+1])/2,cz=(p.Zs[j]+p.Zs[j+1])/2;if(higher.Any(h=>WindEdgeLoads.InsideAt(h,cx,cz)))continue;
    if(covers.Any(cv=>Math.Abs(p.Y-cv[0])<1e-3&&cx>cv[1]-1e-6&&cx<cv[2]+1e-6&&cz>cv[3]-1e-6&&cz<cv[4]+1e-6))continue;
    double area=(p.Xs[i+1]-p.Xs[i])*(p.Zs[j+1]-p.Zs[j]);val[i,j]=Lr(1,area);cells++;
    var reg=regions.FirstOrDefault(x=>x.Kind=="Slab"&&Math.Abs(x.Slope-p.Y)<1e-6&&Math.Abs(x.Lr-val[i,j].Value)<1e-9);
    if(reg==null)regions.Add(new Region{Kind="Slab",Where="level "+F(p.Y)+" m",Slope=p.Y,Area=area,Row=1,Lr=val[i,j].Value,Basis="panels of "+AreaBand(area)});else reg.Area=Math.Max(reg.Area,area);}
   /* equal values merged: runs along Z, then equal runs in neighbouring X strips */
   var rects=new List<double[]>();for(int i=0;i<nx;i++){int j=0;while(j<nz){if(val[i,j]==null){j++;continue;}int j2=j;while(j2+1<nz&&val[i,j2+1]!=null&&Math.Abs(val[i,j2+1].Value-val[i,j].Value)<1e-9)j2++;
     var r=new[]{p.Xs[i],p.Xs[i+1],p.Zs[j],p.Zs[j2+1],val[i,j].Value};var prev=rects.FirstOrDefault(q=>Math.Abs(q[1]-r[0])<1e-9&&Math.Abs(q[2]-r[2])<1e-9&&Math.Abs(q[3]-r[3])<1e-9&&Math.Abs(q[4]-r[4])<1e-9);if(prev!=null)prev[1]=r[1];else rects.Add(r);j=j2+1;}}
   foreach(var r in rects)floor.Append("\nYRANGE ").Append(F(p.Y-0.05)).Append(" ").Append(F(p.Y+0.05)).Append(" FLOAD ").Append(F(-r[4])).Append(" XRANGE ").Append(F(r[0]-0.01)).Append(" ").Append(F(r[1]+0.01)).Append(" ZRANGE ").Append(F(r[2]-0.01)).Append(" ").Append(F(r[3]+0.01)).Append(" GY");}
  if(floor.Length==0&&member.Length==0){note="no roof found in the model";return "";}
  var b=new StringBuilder("* roof live load, NSCP 2015 Section 205.4, Table 205-3 (Method 1),\n* on the horizontal projection; Lr by slope and tributary area\nUNIT METER KN");
  if(floor.Length>0)b.Append("\n* slab roofs: Lr by the panel area (tributary area of its beams)\nFLOOR LOAD").Append(floor);
  if(member.Length>0)b.Append("\n* trusses: w = Lr x truss spacing, projected on the horizontal (PY)\nMEMBER LOAD").Append(member);
  note=(cells>0?cells+" slab roof panels":"")+(cells>0&&chords.Count>0?", ":"")+(chords.Count>0?chords.Count+" truss top chords in "+chords.Select(c=>c.Plane).Distinct().Count()+" trusses":"")+
   "; Lr "+string.Join(", ",regions.Select(r=>r.Lr).Distinct().OrderByDescending(x=>x).Select(F))+" kPa. Roof decks used as an occupancy take Table 205-1, 18 (as the area served) in a live load case instead.";
  return b.ToString();}
}
}
