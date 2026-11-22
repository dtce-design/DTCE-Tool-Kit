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
 internal static double Mmax(double lengthKm){return 5.08+1.16*Math.Log10(Math.Max(lengthKm,0.1));}
 /* the fault of the site: HazardHunterPH's fault when the data has it, else the nearest trace */
 internal static FaultEstimate ForSite(double lat,double lon,params string[] hazardHunterNames){var traces=All;if(traces.Count==0)return null;FaultTrace pick=null;bool named=false;
  /* HazardHunterPH's segment (e.g. West Valley Fault) is tried before the fault system name (Valley Fault System) */
  foreach(var name in hazardHunterNames??new string[0]){string key=Key(name);if(pick!=null||key.Length<4)continue;var match=traces.Where(t=>{string k=Key(t.Name);return k.Length>=4&&(k==key||k.StartsWith(key)||key.StartsWith(k));}).ToList();if(match.Count>0){double a,b;pick=match.OrderBy(t=>Distance(t,lat,lon,out a,out b)).First();named=true;}}
  if(pick==null){double a,b;pick=traces.OrderBy(t=>Distance(t,lat,lon,out a,out b)).First();}
  var e=new FaultEstimate{Name=pick.Name,SlipType=pick.SlipType,Catalog=pick.Catalog,FromHazardHunter=named};/* an unnamed trace (a plate-boundary or regional segment) stands alone; a named fault is measured along all its traces */e.Traces=Unnamed(pick.Name)?new List<FaultTrace>{pick}:traces.Where(t=>t.Name==pick.Name).ToList();e.LengthKm=e.Traces.Sum(t=>t.LengthKm);e.Mmax=Mmax(e.LengthKm);
  var rates=e.Traces.Where(t=>!double.IsNaN(t.SlipRate)).Select(t=>t.SlipRate).ToList();if(rates.Count>0)e.SlipRate=rates.Max();double nl,no;e.DistanceKm=e.Traces.Min(t=>Distance(t,lat,lon,out nl,out no));Distance(pick,lat,lon,out nl,out no);e.NearLat=nl;e.NearLon=no;
  e.Basis="Fault trace: "+e.Name+(e.SlipType==""?"":" ("+e.SlipType.ToLowerInvariant()+")")+(named?" - the HazardHunterPH fault":" - the nearest mapped trace")+", length L = "+e.LengthKm.ToString("0.0",Inv)+" km measured along "+e.Traces.Count+" trace(s); Mmax = 5.08 + 1.16 log10(L) = "+e.Mmax.ToString("0.00",Inv)+" (Wells and Coppersmith 1994); slip rate "+(double.IsNaN(e.SlipRate)?"not in the data":e.SlipRate.ToString("0.0#",Inv)+" mm/yr")+"."+(e.LengthKm>400?" L is beyond the Wells and Coppersmith data (about 400 km); check Mmax with PHIVOLCS.":"")+(e.SlipType.IndexOf("Subduction",StringComparison.OrdinalIgnoreCase)>=0?" Subduction interface: the relation is for crustal faults; use the PHIVOLCS trench magnitude where available.":"")+" Traces: GEM Global Active Faults Database (CC BY-SA 4.0), Philippine catalogue from PHIVOLCS maps.";
  return e;}
 /* NSCP 2015 Table 208-3, with the conservative choice when the slip rate is unknown */
 internal static string SourceType(double mmax,double slip,out string note){note="";if(double.IsNaN(mmax))return "";if(double.IsNaN(slip)){note=mmax>=7.0?"slip rate unknown - Type A taken (Mmax >= 7.0, conservative)":"slip rate unknown - Type B taken (Mmax < 7.0; Type C needs a slip rate <= 2 mm/yr)";return mmax>=7.0?"A":"B";}
  if(mmax>=7.0&&slip>=5)return "A";if(mmax<6.5&&slip<=2)return "C";return "B";}
}
}
