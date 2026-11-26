using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Text;
namespace BeamScheduleFormatter {
/* the DEFINE WIND LOAD types of one STAAD direction of travel (+X, -X, +Z, -Z), as the Wind Load sheet numbers them */
sealed class WindFaceTypes {public string Travel="";public int Ww,Lw,Sw;public double Top,Leeward,Side;public List<double> WwInt=new List<double>(),WwHeig=new List<double>();
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;
 /* windward intensity at height z (the HEIG band that holds z, as STAAD reads INT / HEIG) */
 internal double WindwardAt(double z){for(int i=0;i<WwHeig.Count;i++)if(z<=WwHeig[i]+1e-9)return WwInt[i];return WwInt.Count>0?WwInt[WwInt.Count-1]:0;}
 internal string Pack(){return string.Join("|",new[]{Ww.ToString(Ci),Lw.ToString(Ci),Sw.ToString(Ci),Top.ToString("R",Ci),Leeward.ToString("R",Ci),Side.ToString("R",Ci),string.Join(",",WwInt.Select((v,i)=>v.ToString("R",Ci)+"@"+WwHeig[i].ToString("R",Ci)))});}
 internal static WindFaceTypes Unpack(string travel,string text){try{var p=(text??"").Split('|');if(p.Length<7)return null;var t=new WindFaceTypes{Travel=travel,Ww=int.Parse(p[0],Ci),Lw=int.Parse(p[1],Ci),Sw=int.Parse(p[2],Ci),Top=double.Parse(p[3],Ci),Leeward=double.Parse(p[4],Ci),Side=double.Parse(p[5],Ci)};
   foreach(var pair in p[6].Split(new[]{','},StringSplitOptions.RemoveEmptyEntries)){var q=pair.Split('@');t.WwInt.Add(double.Parse(q[0],Ci));t.WwHeig.Add(double.Parse(q[1],Ci));}return t;}catch(Exception){return null;}}}
/* a face of the floor outline at one level: x / z of its two ends, the outward normal ("+X" ...) and the height band it carries */
sealed class WindEdgeFace {public double Level,Bottom,TopBand,A1,A2,At;public string Normal="";public bool Shielded;}
/* the wind load cases of the STAAD input (owner, 26 November 2026: "automatic na ganito WIND LOAD X 1 TYPE 1 YR 0 12.6 ..."; "automatic load
   ng windload based sa edges ng building, kasi kapag may paloob sa gitna e di naloload"): each case gets the WIND LOAD lines of its windward,
   leeward and side-wall types; faces of the floor outline that the wind cannot reach along its direction (recesses in the middle of the
   building, re-entrant corners) are loaded from the pasted STAAD model as member loads on the edge beams of each level */
static class WindEdgeLoads {
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;
 static string F(double v){return Math.Round(v,3).ToString("0.###",Ci);}
 static double R(double v){return Math.Round(v,3);}
 /* STAAD picks the faces exposed to a wind that blows toward the given axis; the factor sign sets the load direction */
 internal static string WindLoadLines(WindFaceTypes t,double baseY){char ax=t.Travel[1];string toward=t.Travel,back=(t.Travel[0]=='+'?"-":"")+ax;
  string other=ax=='X'?"Z":"X";string yr=" YR "+F(baseY)+" "+F(t.Top);var b=new StringBuilder();
  b.Append("WIND LOAD ").Append(toward[0]=='+'?ax.ToString():"-"+ax).Append(" 1 TYPE ").Append(t.Ww).Append(yr).Append("\n");
  /* leeward: the face exposed to a wind from the other side, pulled outward (factor -1) */
  b.Append("WIND LOAD ").Append(back).Append(" -1 TYPE ").Append(t.Lw).Append(yr).Append("\n");
  /* side walls: both faces pulled outward */
  b.Append("WIND LOAD ").Append(other).Append(" -1 TYPE ").Append(t.Sw).Append(yr).Append("\n");
  b.Append("WIND LOAD -").Append(other).Append(" -1 TYPE ").Append(t.Sw).Append(yr);
  return b.ToString();}
 sealed class Seg {public int No;public double X1,Z1,X2,Z2,Y;public double Len{get{return Math.Sqrt((X2-X1)*(X2-X1)+(Z2-Z1)*(Z2-Z1));}}}
 /* the horizontal members of the model by level (m), levels that carry a real floor (at least 20 % of the longest level's beams) */
 static Dictionary<double,List<Seg>> Levels(StaadModel m){var all=new List<Seg>();double k=m.ToMetre;
  foreach(var mem in m.Members.Values){StaadJoint a,b;if(!m.Joints.TryGetValue(mem.A,out a)||!m.Joints.TryGetValue(mem.B,out b))continue;double ya=m.H(a)*k,yb=m.H(b)*k;if(Math.Abs(ya-yb)>1e-3)continue;
   var s=new Seg{No=mem.No,X1=R(m.P1(a)*k),Z1=R(m.P2(a)*k),X2=R(m.P1(b)*k),Z2=R(m.P2(b)*k),Y=R(ya)};if(s.Len>1e-6)all.Add(s);}
  var groups=all.GroupBy(s=>s.Y).ToDictionary(g=>g.Key,g=>g.ToList());if(groups.Count==0)return groups;double max=groups.Values.Max(l=>l.Sum(s=>s.Len));
  return groups.Where(g=>g.Value.Sum(s=>s.Len)>=0.2*max).ToDictionary(g=>g.Key,g=>g.Value);}
 /* does a ray from (x, z) along +X / -X / +Z / -Z cross a beam of the level? */
 static bool Hits(List<Seg> segs,double x,double z,int dx,int dz){foreach(var s in segs){
   if(dz==0){if(Math.Min(s.Z1,s.Z2)>z||Math.Max(s.Z1,s.Z2)<z||Math.Abs(s.Z2-s.Z1)<1e-9)continue;double xi=s.X1+(s.X2-s.X1)*(z-s.Z1)/(s.Z2-s.Z1);if((xi-x)*dx>0)return true;}
   else{if(Math.Min(s.X1,s.X2)>x||Math.Max(s.X1,s.X2)<x||Math.Abs(s.X2-s.X1)<1e-9)continue;double zi=s.Z1+(s.Z2-s.Z1)*(x-s.X1)/(s.X2-s.X1);if((zi-z)*dz>0)return true;}}
  return false;}
 /* the floor plan of every level from its beams (grid cells enclosed by beams on all four sides), for the irregularity checks */
 internal sealed class FloorPlan {public double Level;public List<double> Xs,Zs;public bool[,] Inside;
  public double Area{get{double a=0;for(int i=0;i+1<Xs.Count;i++)for(int j=0;j+1<Zs.Count;j++)if(Inside[i,j])a+=(Xs[i+1]-Xs[i])*(Zs[j+1]-Zs[j]);return a;}}}
 internal static List<FloorPlan> FloorPlans(StaadModel m){var list=new List<FloorPlan>();if(m==null||m.ZUp)return list;foreach(var lv in Levels(m).OrderBy(k=>k.Key)){var segs=lv.Value;
   var xs=segs.SelectMany(s=>new[]{s.X1,s.X2}).Distinct().OrderBy(v=>v).ToList();var zs=segs.SelectMany(s=>new[]{s.Z1,s.Z2}).Distinct().OrderBy(v=>v).ToList();if(xs.Count<2||zs.Count<2)continue;
   var inside=new bool[xs.Count-1,zs.Count-1];for(int i=0;i+1<xs.Count;i++)for(int j=0;j+1<zs.Count;j++){double cx=(xs[i]+xs[i+1])/2,cz=(zs[j]+zs[j+1])/2;inside[i,j]=Hits(segs,cx,cz,1,0)&&Hits(segs,cx,cz,-1,0)&&Hits(segs,cx,cz,0,1)&&Hits(segs,cx,cz,0,-1);}
   list.Add(new FloorPlan{Level=lv.Key,Xs=xs,Zs=zs,Inside=inside});}return list;}
 /* the outline faces of every level: grid cells between the beam lines are inside when beams enclose them on all four sides; a cell side
    between an inside and an outside cell is a face; a face is shielded when the floor continues beyond it along its outward normal */
 internal static List<WindEdgeFace> Faces(StaadModel m,double baseY,double top,out string note){note="";var faces=new List<WindEdgeFace>();
  if(m.ZUp){note="The model uses SET Z UP: recessed faces are not generated (Y up only).";return faces;}
  var levels=Levels(m);var ys=levels.Keys.Where(y=>y>=baseY-1e-6&&y<=top+1e-6).OrderBy(y=>y).ToList();if(ys.Count==0){note="No floor level with beams between the base and the top of the building.";return faces;}
  for(int li=0;li<ys.Count;li++){double y=ys[li];var segs=levels[y];
   double lo=li==0?baseY:(ys[li-1]+y)/2,hi=li==ys.Count-1?top:(ys[li+1]+y)/2;if(hi<=lo+1e-6)continue;
   var xs=segs.SelectMany(s=>new[]{s.X1,s.X2}).Distinct().OrderBy(v=>v).ToList();var zs=segs.SelectMany(s=>new[]{s.Z1,s.Z2}).Distinct().OrderBy(v=>v).ToList();if(xs.Count<2||zs.Count<2)continue;
   int nx=xs.Count-1,nz=zs.Count-1;var inside=new bool[nx,nz];
   for(int i=0;i<nx;i++)for(int j=0;j<nz;j++){double cx=(xs[i]+xs[i+1])/2,cz=(zs[j]+zs[j+1])/2;inside[i,j]=Hits(segs,cx,cz,1,0)&&Hits(segs,cx,cz,-1,0)&&Hits(segs,cx,cz,0,1)&&Hits(segs,cx,cz,0,-1);}
   Func<int,int,bool> In=(i,j)=>i>=0&&j>=0&&i<nx&&j<nz&&inside[i,j];
   for(int i=0;i<nx;i++)for(int j=0;j<nz;j++){if(!inside[i,j])continue;
    foreach(var d in new[]{new[]{-1,0},new[]{1,0},new[]{0,-1},new[]{0,1}}){if(In(i+d[0],j+d[1]))continue;
     bool shielded=false;for(int s=2;;s++){int ii=i+d[0]*s,jj=j+d[1]*s;if(ii<0||jj<0||ii>=nx||jj>=nz)break;if(inside[ii,jj]){shielded=true;break;}}
     var f=new WindEdgeFace{Level=y,Bottom=lo,TopBand=hi,Shielded=shielded,Normal=(d[0]+d[1]>0?"+":"-")+(d[0]!=0?"X":"Z")};
     if(d[0]!=0){f.At=d[0]<0?xs[i]:xs[i+1];f.A1=zs[j];f.A2=zs[j+1];}else{f.At=d[1]<0?zs[j]:zs[j+1];f.A1=xs[i];f.A2=xs[i+1];}faces.Add(f);}}}
  /* join the cell sides of one straight face */
  var merged=new List<WindEdgeFace>();foreach(var g in faces.GroupBy(f=>f.Level+"|"+f.Normal+"|"+f.At.ToString("R",Ci)+"|"+f.Shielded)){WindEdgeFace cur=null;
   foreach(var f in g.OrderBy(f=>f.A1)){if(cur!=null&&Math.Abs(cur.A2-f.A1)<1e-6)cur.A2=f.A2;else{if(cur!=null)merged.Add(cur);cur=f;}}if(cur!=null)merged.Add(cur);}
  return merged;}
 /* the member loads of the shielded faces for one wind case: windward faces (normal against the travel) take the windward pressure pushing
    in, the others the leeward / side-wall suction pulling out; w = p x band height on the beams along the face */
 internal static string RecessLoads(StaadModel m,List<WindEdgeFace> faces,WindFaceTypes t,out int count){count=0;var lines=new List<string>();var levels=Levels(m);
  char ax=t.Travel[1];int sign=t.Travel[0]=='+'?1:-1;
  foreach(var f in faces.Where(x=>x.Shielded)){char fn=f.Normal[1];int fs=f.Normal[0]=='+'?1:-1;bool windward=fn==ax&&fs==-sign,leeward=fn==ax&&fs==sign;
   double p=windward?t.WindwardAt(f.TopBand):leeward?t.Leeward:t.Side;double h=f.TopBand-f.Bottom;double w=p*h*(windward?-fs:fs);
   string g=fn=='X'?"GX":"GZ";List<Seg> segs;if(!levels.TryGetValue(f.Level,out segs))continue;
   foreach(var s in segs){bool along=fn=='X'?Math.Abs(s.X1-f.At)<1e-6&&Math.Abs(s.X2-f.At)<1e-6:Math.Abs(s.Z1-f.At)<1e-6&&Math.Abs(s.Z2-f.At)<1e-6;if(!along)continue;
    double s1=fn=='X'?s.Z1:s.X1,s2=fn=='X'?s.Z2:s.X2;double lo=Math.Max(Math.Min(s1,s2),f.A1),hi=Math.Min(Math.Max(s1,s2),f.A2);if(hi-lo<1e-6)continue;
    double d1=Math.Abs(lo-s1),d2=Math.Abs(hi-s1);if(d1>d2){var tmp=d1;d1=d2;d2=tmp;}
    lines.Add(s.No.ToString(Ci)+" UNI "+g+" "+F(w)+" "+F(d1)+" "+F(d2)+" ");count++;
    lines[lines.Count-1]=lines[lines.Count-1].TrimEnd();}}
  if(lines.Count==0)return "";
  return "* recessed faces the wind does not reach along "+t.Travel+" (from the model edges): w = p x tributary height\nUNIT METER KN\nMEMBER LOAD\n"+string.Join("\n",lines);}
 /* the wind load text of each case (+X, -X, +Z, -Z) for the Load Combinations sheet */
 internal static Dictionary<string,string> CaseTexts(Dictionary<string,string> link,string modelText,out string note){return CaseTexts(link,RedundancyFactor.HasModel(modelText)?RedundancyFactor.ReadModel(modelText):null,out note);}
 internal static Dictionary<string,string> CaseTexts(Dictionary<string,string> link,StaadModel model,out string note){note="";var map=new Dictionary<string,string>();if(link==null)return map;
  List<WindEdgeFace> faces=null;double baseY=0;
  int recess=0;
  foreach(var travel in new[]{"+X","-X","+Z","-Z"}){string packed;if(!link.TryGetValue(travel,out packed))continue;var t=WindFaceTypes.Unpack(travel,packed);if(t==null)continue;
   var b=new StringBuilder(WindLoadLines(t,baseY));
   if(model!=null){if(faces==null){string n;faces=Faces(model,baseY,t.Top,out n);if(n!="")note=n;}int c;string r=RecessLoads(model,faces,t,out c);recess+=c;if(r!="")b.Append("\n").Append(r);}
   map[travel]=b.ToString();}
  if(map.Count>0)note=(note!=""?note+" ":"")+(model==null?"Paste the STAAD Nodes and Beams tables (or the .std file) on the Primary loads tab to load the recessed faces too.":recess>0?recess+" member loads on the recessed sides: "+string.Join("; ",faces.Where(f=>f.Shielded).GroupBy(f=>f.Normal[1]+"="+F(f.At)+" m, "+(f.Normal[1]=='X'?"Z ":"X ")+F(f.A1)+"-"+F(f.A2)+" m").Select(g=>g.Key+" (levels "+string.Join(", ",g.Select(f=>F(f.Level)).Distinct())+")"))+".":"No recessed face found: WIND LOAD reaches every face.");
  return map;}
}
}
