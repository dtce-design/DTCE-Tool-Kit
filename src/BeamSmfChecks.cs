using System;using System.Collections.Generic;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
/* Special moment frame beam detailing checks of the bar bending schedule (owner, 19 November 2026: "Icheck mo na lang please ang
   nscp 2015 at detailing manual ni aci gamit ang smf kasi yun lang ang permitted sa ph"). NSCP 2015 Section 418.6 (beams of special
   moment frames, = ACI 318-14 18.6) and 418.8 (joints), from the beam schedule, the clear spans and the support widths.
   Moment strengths are compared through the steel areas (same section, same d); column sizes across the beam are not known
   here, so 418.6.2.1(c) and the joint shear of 418.8.4 stay with the structural design. */
class SmfCheck {public string Line,Item,Clause,Required,Provided,Note="";public int Span;public bool Ok;}
static class BeamSmf {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 static string Mm(double m){return (m*1000).ToString("0",Inv);}
 static double Area(Dictionary<int,int> bars){return bars.Sum(b=>b.Value*Math.PI*b.Key*b.Key/4.0);}
 static string Bars(Dictionary<int,int> bars){return bars.Count==0?"none":string.Join(" + ",bars.OrderByDescending(b=>b.Key).Select(b=>b.Value+"-"+b.Key+"mm"));}
 public static List<SmfCheck> Check(CutResult r,CutSettings s){var list=new List<SmfCheck>();if(!s.SpecialMomentFrame)return list;double fc=s.Config.Input.Fc,fy=s.Config.FyLarge,cover=s.Cover;
  foreach(var g in r.Geometry){if(g.Rows==null)continue;int n=g.Rows.Length;
   Func<int,int,Dictionary<int,int>> bars=(j,cell)=>{try{return EstimatorEngine.Bars(g.Rows[j].Cells[cell]);}catch(Exception){return new Dictionary<int,int>();}};
   Action<int,string,string,string,string,bool,string> add=(span,item,clause,req,prov,ok,note)=>list.Add(new SmfCheck{Line=g.Line,Span=span,Item=item,Clause=clause,Required=req,Provided=prov,Ok=ok,Note=note??""});
   for(int j=0;j<n;j++){double b=g.Width[j]*1000,h=g.Depth[j]*1000,ln=g.Ln[j]*1000;var ties=j<g.Ties.Count?g.Ties[j]:new List<BeamCutting.TieZone>();int tieDia=ties.Count>0?ties.Max(t=>t.Dia):10;
    var bottom=new[]{bars(j,2),bars(j,3),bars(j,4)};var top=new[]{bars(j,5),bars(j,6),bars(j,7)};int dbMax=bottom.Concat(top).SelectMany(x=>x.Keys).DefaultIfEmpty(20).Max(),dbMin=bottom.Concat(top).SelectMany(x=>x.Keys).DefaultIfEmpty(20).Min();
    int layers=1;{double inner=b-2*cover-2*tieDia;foreach(var face in bottom.Concat(top)){int cnt=face.Values.Sum();if(cnt==0)continue;int per=BeamCutting.PerLayer(inner,face.Keys.Max(),s.Aggregate);layers=Math.Max(layers,(cnt+per-1)/per);}}
    double d=layers>1?h-cover-tieDia-dbMax-s.SpacerDia/2.0:h-cover-tieDia-dbMax/2.0;
    add(j+1,"Clear span Ln >= 4d","418.6.2.1(a)","4d = "+(4*d).ToString("0",Inv)+" mm","Ln = "+ln.ToString("0",Inv)+" mm",ln>=4*d-1e-6,(layers>1?"d = h - cover - tie - db - spacer/2 = "+d.ToString("0",Inv)+" mm ("+layers+" layers)":"d = h - cover - tie - db/2 = "+d.ToString("0",Inv)+" mm (one layer)"));
    add(j+1,"Width bw >= min(0.3h, 250 mm)","418.6.2.1(b)",Math.Min(.3*h,250).ToString("0",Inv)+" mm",b.ToString("0",Inv)+" mm",b>=Math.Min(.3*h,250)-1e-6,null);
    double asMin=Math.Max(.25*Math.Sqrt(fc)/fy,1.4/fy)*b*d;string[] zone={"left face","midspan","right face"};
    for(int f=0;f<2;f++){var face=f==0?bottom:top;string nm=f==0?"Bottom":"Top";double lo=face.Min(x=>Area(x)),hi=face.Max(x=>Area(x));int at=Array.FindIndex(face,x=>Area(x)==lo),atHi=Array.FindIndex(face,x=>Area(x)==hi);
     add(j+1,nm+" bars As >= As,min at every section","418.6.3.1 / 409.6.1.2",asMin.ToString("0",Inv)+" mm²",lo.ToString("0",Inv)+" mm² ("+Bars(face[at])+", "+zone[at]+")",lo>=asMin-1e-6,"As,min = max(0.25√fc', 1.4) bw d / fy");
     add(j+1,nm+" ratio ρ <= 0.025","418.6.3.1","ρ <= 0.0250",(hi/(b*d)).ToString("0.0000",Inv)+" ("+zone[atHi]+")",hi/(b*d)<=.025+1e-9,null);
     var common=face[0].Keys.Where(k=>face[1].ContainsKey(k)&&face[2].ContainsKey(k)).ToDictionary(k=>k,k=>face.Min(x=>x[k]));int cont=common.Values.Sum();
     add(j+1,nm+": at least 2 continuous bars","418.6.3.1","2 bars","" +cont+" ("+Bars(common)+")",cont>=2,null);}
    for(int e=0;e<2;e++){int z=e==0?0:2;double neg=Area(top[z]),pos=Area(bottom[z]);add(j+1,"M+ at "+zone[z]+" >= ½ M- (As bottom >= ½ As top)","418.6.3.2",(neg/2).ToString("0",Inv)+" mm²",pos.ToString("0",Inv)+" mm²",pos>=neg/2-1e-6,"steel areas as a measure of the moment strengths");}
    {double maxFace=new[]{Area(top[0]),Area(top[2]),Area(bottom[0]),Area(bottom[2])}.Max(),least=top.Concat(bottom).Min(x=>Area(x));add(j+1,"M+ and M- at any section >= ¼ max M at the faces","418.6.3.2",(maxFace/4).ToString("0",Inv)+" mm²",least.ToString("0",Inv)+" mm²",least>=maxFace/4-1e-6,null);}
    if(ties.Count>0){double smax=Math.Min(Math.Min(d/4,6*dbMin),150),h2=2*h;var left=ties[0];
     add(j+1,"First hoop <= 50 mm from the support face","418.6.4.4","50 mm",(left.First*1000).ToString("0",Inv)+" mm",left.First*1000<=50+1e-6,null);
     foreach(var end in new[]{0,1}){double from=end==0?0:ln-h2,to=end==0?h2:ln;var inZone=ties.Where(t=>t.To*1000>from+1e-6&&t.From*1000<to-1e-6).ToList();double worst=inZone.Count==0?double.PositiveInfinity:inZone.Max(t=>t.Spacing*1000);
      add(j+1,"Hoops over 2h = "+h2.ToString("0",Inv)+" mm at the "+(end==0?"left":"right")+" face","418.6.4.1 / 418.6.4.4","s <= min(d/4, 6db, 150) = "+smax.ToString("0",Inv)+" mm","s = "+worst.ToString("0",Inv)+" mm",worst<=smax+1e-6,"db = smallest main bar "+dbMin+" mm");}
     var mid=ties.Where(t=>t.To*1000>h2+1e-6&&t.From*1000<ln-h2-1e-6).ToList();if(mid.Count>0){double worst=mid.Max(t=>t.Spacing*1000);add(j+1,"Stirrups beyond 2h","418.6.4.6","s <= d/2 = "+(d/2).ToString("0",Inv)+" mm","s = "+worst.ToString("0",Inv)+" mm",worst<=d/2+1e-6,null);}
     int legs=ties.Min(t=>t.Legs);double clear=(b-2*cover-tieDia)/Math.Max(1,legs-1);add(j+1,"Hoop legs: laterally supported bars <= 350 mm apart","418.6.4.2 / 418.7.5.2","<= 350 mm",legs+" legs, "+clear.ToString("0",Inv)+" mm apart",clear<=350+1e-6,null);
     try{var v=s.Config.Values(tieDia,false);double need=Math.Max(6*tieDia,75);add(j+1,"Hoop 135° hook extension","425.3.2 / 418.6.4.2",">= max(6db, 75) = "+need.ToString("0",Inv)+" mm",v.Tail135.ToString("0",Inv)+" mm",v.Tail135>=need-1e-6,"closed hoops with seismic hooks; crossties 135° / 90° alternated end for end");}catch(Exception){}}}
   /* joints */
   for(int i=0;i<=n;i++){bool exterior=i==0||i==n;double c1=g.SupportWidth[i]*1000;var through=new List<int>();if(i>0){through.AddRange(bars(i-1,2).Keys);through.AddRange(bars(i-1,7).Keys);}if(i<n){through.AddRange(bars(i,0+2).Keys);through.AddRange(bars(i,5).Keys);}if(through.Count==0)continue;int db=through.Max();
    if(!exterior)add(0,"Joint "+(i+1)+": column depth >= 20 db of beam bars through it","418.8.2.3",(20*db).ToString("0",Inv)+" mm ("+db+" mm bar)",c1.ToString("0",Inv)+" mm",c1>=20*db-1e-6,"support width along the beam");
    else{double ldh=Math.Max(Math.Max(8*db,150),fy*db/(5.4*Math.Sqrt(fc)));add(0,"Joint "+(i+1)+" (exterior): 90° hook ldh inside the column","418.8.5.1","ldh = max(8db, 150, fy db / 5.4√fc') = "+ldh.ToString("0",Inv)+" mm","available "+(c1-cover).ToString("0",Inv)+" mm (support - cover)",c1-cover>=ldh-1e-6,"hook bent into the joint, tail within the confined core");}}
   /* laps */
   var laps=r.Splices.Where(x=>string.Equals(x.Line,g.Line,StringComparison.OrdinalIgnoreCase)).ToList();if(laps.Count>0)add(0,"Lap splices outside joints and 2h, hoops over the lap","418.6.3.3","in allowed zone, s <= min(d/4, 100)",laps.Count(x=>x.Ok)+" of "+laps.Count+" laps OK",laps.All(x=>x.Ok),"see the splice zone check");}
  return list;}
}
}
