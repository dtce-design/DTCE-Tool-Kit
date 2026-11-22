using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Text.RegularExpressions;
namespace BeamScheduleFormatter {
/* Fault length and maximum magnitude from the fault traces (owner, 22 November 2026: "use this automatically measure the fault
   length"): the fault of the site - the HazardHunterPH fault when its name is in the trace data, else the nearest trace - is
   measured along all of its traces, and Mmax = 5.08 + 1.16 log10(L km) (Wells and Coppersmith 1994, surface rupture length, all
   slip types). The slip rate is the trace's preferred net slip rate where the data has one. The source type then follows NSCP
   2015 Table 208-3; with no slip rate the type is A when Mmax >= 7.0 (conservative) and B otherwise. */
class FaultTrace {public string Name,SlipType,Catalog;public double SlipRate=double.NaN;public List<double[]> Points=new List<double[]>();/* lon, lat */
 public double LengthKm{get{double s=0;for(int k=1;k<Points.Count;k++)s+=FaultTraces.Km(Points[k-1][1],Points[k-1][0],Points[k][1],Points[k][0]);return s;}}}
class FaultEstimate {public string Name="",SlipType="",Catalog="",Basis="";public double LengthKm,Mmax,SlipRate=double.NaN,DistanceKm,NearLat,NearLon;public bool FromHazardHunter;public List<FaultTrace> Traces=new List<FaultTrace>();}
static class FaultTraces {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;static List<FaultTrace> all;static List<List<double[]>> coast;
 internal static List<FaultTrace> All{get{if(all==null)all=Parse();return all;}}
 internal static List<List<double[]>> Coast{get{if(coast==null)coast=PhCoastData.Lines.Split('\n').Select(l=>Pts(l.Trim())).Where(p=>p.Count>1).ToList();return coast;}}
 static List<double[]> Pts(string s){var list=new List<double[]>();foreach(var q in s.Split(';')){var c=q.Split(',');double a,b;if(c.Length==2&&double.TryParse(c[0],NumberStyles.Float,Inv,out a)&&double.TryParse(c[1],NumberStyles.Float,Inv,out b))list.Add(new[]{a,b});}return list;}
 static List<FaultTrace> Parse(){var list=new List<FaultTrace>();foreach(var line in PhFaultData.Traces.Split('\n')){var p=line.Trim().Split('|');if(p.Length<5)continue;var t=new FaultTrace{Name=p[0],SlipType=p[1],Catalog=p[3],Points=Pts(p[4])};double sr;if(double.TryParse(p[2],NumberStyles.Float,Inv,out sr))t.SlipRate=sr;if(t.Points.Count>1)list.Add(t);}return list;}
 internal static double Km(double lat1,double lon1,double lat2,double lon2){double r=Math.PI/180,dlat=(lat2-lat1)*r,dlon=(lon2-lon1)*r;double a=Math.Sin(dlat/2)*Math.Sin(dlat/2)+Math.Cos(lat1*r)*Math.Cos(lat2*r)*Math.Sin(dlon/2)*Math.Sin(dlon/2);return 6371.0088*2*Math.Atan2(Math.Sqrt(a),Math.Sqrt(1-a));}
 /* distance from the site to a trace (local plane around the site) and the nearest point */
 internal static double Distance(FaultTrace t,double lat,double lon,out double nlat,out double nlon){double kx=111.32*Math.Cos(lat*Math.PI/180),ky=110.574;double best=double.MaxValue;nlat=lat;nlon=lon;
  for(int k=1;k<t.Points.Count;k++){double ax=(t.Points[k-1][0]-lon)*kx,ay=(t.Points[k-1][1]-lat)*ky,bx=(t.Points[k][0]-lon)*kx,by=(t.Points[k][1]-lat)*ky;double dx=bx-ax,dy=by-ay,L2=dx*dx+dy*dy;double u=L2<=0?0:Math.Max(0,Math.Min(1,-(ax*dx+ay*dy)/L2));double px=ax+u*dx,py=ay+u*dy,d=Math.Sqrt(px*px+py*py);if(d<best){best=d;nlon=lon+px/kx;nlat=lat+py/ky;}}return best;}
 static bool Unnamed(string n){return string.IsNullOrWhiteSpace(n)||n.Trim().StartsWith("Unnamed",StringComparison.OrdinalIgnoreCase);}
 static string Key(string n){if(Unnamed(n))return "";n=Regex.Replace((n??"").ToUpperInvariant(),@"\(.*?\)","");n=Regex.Replace(n,@"\b(FAULT|FAULTS|ZONE|SYSTEM|SYTEM|SEGMENT|TRENCH|THE|OF)\b","");return Regex.Replace(n,@"[^A-Z0-9]","");}
 /* Wells and Coppersmith (1994), magnitude on surface rupture length, all slip types: M = 5.08 + 1.16 log10(SRL); 77 earthquakes,
    standard deviation 0.28, SRL 1.3 to 432 km. Their data are shallow (< 20 km) crustal earthquakes; most relations do not differ
    significantly with the style of faulting, so the all-slip-type relation is used. SRL is the length of the surface rupture of one
    earthquake, so on a mapped fault L is the length of the trace (segment) that would rupture at once: the named segment, measured
    along its trace; parallel strands are not added together. */
 internal const double WcA=5.08,WcB=1.16,WcSigma=0.28,WcMinKm=1.3,WcMaxKm=432;
 internal static double Mmax(double lengthKm){return WcA+WcB*Math.Log10(Math.Max(lengthKm,0.1));}
 internal static bool Subduction(string slipType){return (slipType??"").IndexOf("Subduction",StringComparison.OrdinalIgnoreCase)>=0;}
 /* the fault of the site: HazardHunterPH's fault when the traces have it; with no HazardHunterPH name the nearest trace. A named
    fault that is not in the traces gives null: another fault's length is never used for it. */
 internal static FaultEstimate ForSite(double lat,double lon,params string[] hazardHunterNames){return ForSite(All,lat,lon,"GEM",hazardHunterNames);}
 internal static FaultEstimate ForSite(List<FaultTrace> traces,double lat,double lon,string source,params string[] hazardHunterNames){if(traces==null||traces.Count==0)return null;FaultTrace pick=null;bool asked=false;
  /* HazardHunterPH's segment (e.g. West Valley Fault) is tried before the fault system name (Valley Fault System) */
  foreach(var name in hazardHunterNames??new string[0]){string key=Key(name);if(key.Length>=4)asked=true;if(pick!=null||key.Length<4)continue;var match=traces.Where(t=>{string k=Key(t.Name);return k.Length>=4&&(k==key||k.StartsWith(key)||key.StartsWith(k));}).ToList();if(match.Count>0){double a,b;pick=match.OrderBy(t=>Distance(t,lat,lon,out a,out b)).First();}}
  if(pick==null){if(asked)return null;double a,b;pick=traces.OrderBy(t=>Distance(t,lat,lon,out a,out b)).First();}
  var group=Unnamed(pick.Name)?new List<FaultTrace>{pick}:traces.Where(t=>t.Name==pick.Name).ToList();
  string how=source=="KMZ"?"the red active fault line nearest the site on the PHIVOLCS map (KMZ), traced from the map image":asked?"the fault HazardHunterPH names, in the GEM fault traces":"the nearest mapped trace (no HazardHunterPH fault name)";
  var e=Measure(group,lat,lon,how);e.FromHazardHunter=asked;return e;}
 /* the length of the given traces (one fault, or pieces the user joined) and Mmax from it */
 internal static FaultEstimate Measure(List<FaultTrace> group,double lat,double lon,string how){var first=group[0];var names=group.Select(t=>t.Name).Distinct().ToList();
  var e=new FaultEstimate{Name=string.Join(" + ",names),SlipType=first.SlipType,Catalog=first.Catalog,Traces=group};e.LengthKm=group.Sum(t=>t.LengthKm);e.Mmax=Mmax(e.LengthKm);
  var rates=group.Where(t=>!double.IsNaN(t.SlipRate)).Select(t=>t.SlipRate).ToList();if(rates.Count>0)e.SlipRate=rates.Max();
  double nl=lat,no=lon,best=double.MaxValue;foreach(var t in group){double a,b;double d=Distance(t,lat,lon,out a,out b);if(d<best){best=d;nl=a;no=b;}}e.DistanceKm=best;e.NearLat=nl;e.NearLon=no;
  bool kmz=first.Catalog=="KMZ";
  e.Basis="Rupture length (Wells and Coppersmith 1994): "+e.Name+(e.SlipType==""?"":" ("+e.SlipType.ToLowerInvariant().Replace('_',' ')+")")+" - "+how+"; L = "+e.LengthKm.ToString("0.0",Inv)+" km measured along the trace"+(group.Count>1?" ("+group.Count+" parts)":"")+
   "; Mmax = 5.08 + 1.16 log10(L) = "+e.Mmax.ToString("0.00",Inv)+" +/- "+WcSigma.ToString("0.00",Inv)+" (surface rupture length, all slip types, 77 earthquakes, SRL 1.3 to 432 km); slip rate "+(double.IsNaN(e.SlipRate)?"not in the data":e.SlipRate.ToString("0.0#",Inv)+" mm/yr")+"."+
   (e.LengthKm>WcMaxKm?" L exceeds the 432 km of the data: Mmax is extrapolated; take the segment that would rupture, or the PHIVOLCS magnitude.":e.LengthKm<WcMinKm?" L is below the 1.3 km of the data.":"")+
   (Subduction(e.SlipType)?" Subduction interface: the relation is for shallow crustal faults (subduction events are not in its data); use the PHIVOLCS trench magnitude.":"")+
   (kmz?" Traced from the PHIVOLCS active fault map: red lines only, dashes up to about 1.5 km apart joined; a fault interrupted on the map may need its parts joined (Ctrl+click).":" Traces: GEM Global Active Faults Database (CC BY-SA 4.0), Philippine catalogue from PHIVOLCS maps.");
  return e;}
 /* the Table 208-3 type at Mmax - s and Mmax + s: shows when the type depends on the scatter of the relation */
 internal static string Sensitivity(double mmax,double slip){if(double.IsNaN(mmax))return "";string n;string lo=SourceType(mmax-WcSigma,slip,out n),mid=SourceType(mmax,slip,out n),hi=SourceType(mmax+WcSigma,slip,out n);
  if(lo==mid&&hi==mid)return "Type "+mid+" holds for Mmax +/- "+WcSigma.ToString("0.00",Inv)+" ("+(mmax-WcSigma).ToString("0.00",Inv)+" to "+(mmax+WcSigma).ToString("0.00",Inv)+").";
  return "Within Mmax +/- "+WcSigma.ToString("0.00",Inv)+" ("+(mmax-WcSigma).ToString("0.00",Inv)+" to "+(mmax+WcSigma).ToString("0.00",Inv)+") the type is "+lo+" to "+hi+": confirm Mmax with PHIVOLCS.";}
 /* NSCP 2015 Table 208-3, with the conservative choice when the slip rate is unknown */
 internal static string SourceType(double mmax,double slip,out string note){note="";if(double.IsNaN(mmax))return "";if(double.IsNaN(slip)){note=mmax>=7.0?"slip rate unknown - Type A taken (Mmax >= 7.0, conservative)":"slip rate unknown - Type B taken (Mmax < 7.0; Type C needs a slip rate <= 2 mm/yr)";return mmax>=7.0?"A":"B";}
  if(mmax>=7.0&&slip>=5)return "A";if(mmax<6.5&&slip<=2)return "C";return "B";}
}
}
