using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Text.RegularExpressions;
namespace BeamScheduleFormatter {
/* Torsional amplification factor Ax (owner, 26 November 2026: "add also automatic computation of torsional amplification factor. pero 1
   muna ang value, tapos after run papalitan"): from the STAAD joint displacements of the EQ cases, at each floor level the displacements
   in the load direction at the two extreme edges (perpendicular to the load) give dmax and davg = (d1 + d2) / 2; torsional irregularity
   when dmax > 1.2 davg; Ax = (dmax / 1.2 davg)^2, not less than 1.0 nor more than 3.0; the accidental torsion is 0.05 Ax. */
sealed class AxLevel {public double Level,D1,D2,DMax,DAvg,Ratio,Ax;public int Joints;}
sealed class AxResult {public string Case,Axis;public List<AxLevel> Levels=new List<AxLevel>();public double Ax=1;public string Governing="";}
static class TorsionAmplification {
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;
 static bool IsNum(string s){double v;return double.TryParse(s,NumberStyles.Float,Ci,out v);}
 static double N(string s){return double.Parse(s,NumberStyles.Float,Ci);}
 /* joint displacements: joint, load case -> X, Y, Z (STAAD table copy: Node, L/C, X, Y, Z ...; or the .ANL JOINT DISPLACEMENT block) */
 internal static Dictionary<string,double[]> ReadDisplacements(string text){var d=new Dictionary<string,double[]>();int joint=0,load=0;bool inBlock=false;/* in an .ANL only the JOINT DISPLACEMENT block counts (member end force rows look alike) */bool header=Regex.IsMatch(text??"",@"JOINT\s+DISPLACEMENT",RegexOptions.IgnoreCase);
  foreach(var raw in (text??"").Replace("\r","").Split('\n')){string line=raw.Trim();if(line=="")continue;
   if(raw.Contains("\t")){var c=raw.Split('\t').Select(x=>x.Trim()).ToArray();if(c.Length>=5){int jj;var lc=Regex.Match(c[1],@"^\d+");if(c[0]!=""&&int.TryParse(c[0],out jj))joint=jj;if(lc.Success&&IsNum(c[2])&&IsNum(c[3])&&IsNum(c[4])&&joint>0)d[joint+"|"+lc.Value]=new[]{N(c[2]),N(c[3]),N(c[4])};}continue;}
   if(Regex.IsMatch(line,@"JOINT\s+DISPLACEMENT",RegexOptions.IgnoreCase)){inBlock=true;continue;}
   if(Regex.IsMatch(line,@"MEMBER\s+END\s+FORCES|SUPPORT\s+REACTIONS",RegexOptions.IgnoreCase)){inBlock=false;continue;}
   var t=line.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);if(!t.All(IsNum))continue;var v=t.Select(N).ToArray();
   if(header&&!inBlock)continue;
   if(v.Length==8){joint=(int)v[0];load=(int)v[1];d[joint+"|"+load]=new[]{v[2],v[3],v[4]};}
   else if(v.Length==7&&joint>0){load=(int)v[0];d[joint+"|"+load]=new[]{v[1],v[2],v[3]};}}
  if(d.Count==0)throw new Exception("Paste the joint displacements of the EQ load cases (STAAD Node Displacements table, or the .ANL JOINT DISPLACEMENT).");
  return d;}
 /* Ax of one EQ load case: per level above the base, displacement in the load direction at the two extreme edges */
 internal static AxResult Compute(StaadModel m,Dictionary<string,double[]> disp,int load,string caseName,string axis){var r=new AxResult{Case=caseName,Axis=axis};bool x=axis!="Z";int comp=x?0:(m.ZUp?1:2);
  double baseH=m.Joints.Values.Min(j=>m.H(j));
  foreach(var lvl in m.Joints.Values.Select(j=>Math.Round(m.H(j),3)).Distinct().Where(h=>h>baseH+1e-3).OrderBy(h=>h)){
   var js=m.Joints.Values.Where(j=>Math.Abs(m.H(j)-lvl)<1e-3&&disp.ContainsKey(j.No+"|"+load)).ToList();if(js.Count<2)continue;
   Func<StaadJoint,double> perp=j=>x?m.P2(j):m.P1(j);double lo=js.Min(perp),hi=js.Max(perp);if(hi-lo<1e-6)continue;
   Func<IEnumerable<StaadJoint>,double> edge=e=>e.Max(j=>Math.Abs(disp[j.No+"|"+load][comp]));
   double d1=edge(js.Where(j=>Math.Abs(perp(j)-lo)<1e-3)),d2=edge(js.Where(j=>Math.Abs(perp(j)-hi)<1e-3));double dmax=Math.Max(d1,d2),davg=(d1+d2)/2;if(davg<=1e-12)continue;
   double ratio=dmax/davg,ax=Math.Max(1.0,Math.Min(3.0,Math.Pow(dmax/(1.2*davg),2)));
   r.Levels.Add(new AxLevel{Level=(lvl-baseH)*m.ToMetre,D1=d1,D2=d2,DMax=dmax,DAvg=davg,Ratio=ratio,Ax=ax,Joints=js.Count});}
  if(r.Levels.Count==0)throw new Exception("Load case "+load+": no floor level with displacements at two edges.");
  var g=r.Levels.OrderByDescending(l=>l.Ax).ThenByDescending(l=>l.Ratio).First();r.Ax=g.Ax;
  r.Governing="Ax = "+g.Ax.ToString("0.###",Ci)+" at "+g.Level.ToString("0.##",Ci)+" m (dmax / davg = "+g.Ratio.ToString("0.###",Ci)+(g.Ratio>1.2?", torsionally irregular":", not over 1.2")+")";return r;}
}
}
