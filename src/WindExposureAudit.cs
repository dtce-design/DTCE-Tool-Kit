using System;using System.Linq;using System.Collections.Generic;using System.Globalization;
namespace BeamScheduleFormatter {
// NSCP 2015 Sections 207A.7.1-207A.7.3 exposure screening with the reasons shown per wind direction.
class WindExposureAudit {
 public string Exposure="Unresolved",Roughness="?",Reason="",Mix="";public readonly List<string[]> Checks=new List<string[]>();
 public double Height,BFetch,DFetch,Transition,Available,NearestWater=double.NaN,LongestWater,ShortestB=double.NaN;public int Rays,SmoothRays;
}
static partial class WindAssessment {
 // Five distinct rays at 22.5° cover the two 45° upwind sectors either side of the wind-from direction (−45°, −22.5°, 0°,
 // +22.5°, +45°). Sampled rays sit at each direction and ±22.5°, so a neighbour's −22.5° ray is the same bearing as this
 // direction's +22.5° ray; it is used once (review 17 November 2026: it was counted twice and the text said 7 rays at 15°).
 internal static List<WindSample>[] UpwindSector(WindTerrain terrain,int direction){int previous=(direction+7)%8,next=(direction+1)%8;return new[]{terrain.Rays[previous*3+1],terrain.Rays[direction*3],terrain.Rays[direction*3+1],terrain.Rays[direction*3+2],terrain.Rays[next*3+1]};}
 internal static WindExposureAudit AssessExposure(WindTerrain terrain,int direction,double meanHeight){return AssessExposure(UpwindSector(terrain,direction),meanHeight,true);}
 internal static WindExposureAudit AssessExposure(IEnumerable<List<WindSample>> sector,double meanHeight,bool twoSectors=false){
  var result=new WindExposureAudit{Height=meanHeight};var rays=sector.ToList();result.Rays=rays.Count;Func<double,string> f=value=>WindTin.Finite(value)?value.ToString("0.#",CultureInfo.InvariantCulture):"unresolved";
  result.Checks.Add(new[]{"Height used for fetch",f(meanHeight)+" m","Mean roof height h when building geometry is entered; otherwise the reference height z."});
  result.Checks.Add(new[]{"Upwind investigation",twoSectors?rays.Count+" rays at 22.5° across both 45° sectors (−45° to +45°)":rays.Count+" rays in one screening sector","NSCP 2015 Section 207A.7.1: exposure is determined in the two upwind sectors extending 45° either side of the wind direction; the more severe result governs."});
  if(!WindTin.Finite(meanHeight)||meanHeight<=0){result.Reason="Height is missing or invalid. No exposure category is assumed.";return result;}
  result.BFetch=BFetch(meanHeight);result.DFetch=DFetch(meanHeight);result.Transition=DTransition(meanHeight);double required=Math.Max(result.BFetch,result.DFetch+result.Transition);result.Available=rays.Count==0?0:rays.Min(ray=>ray.Count==0?0:ray.Last().D);
  result.Checks.Add(new[]{"Exposure B fetch",f(result.BFetch)+" m",meanHeight<=9.1?"h ≤ 9.1 m: Surface Roughness B must prevail for 457 m (1,500 ft).":"h > 9.1 m: Surface Roughness B must prevail for the greater of 792 m (2,600 ft) and 20h."});
  result.Checks.Add(new[]{"Exposure D fetch",f(result.DFetch)+" m","Surface Roughness D must prevail for the greater of 1,524 m (5,000 ft) and 20h."});
  result.Checks.Add(new[]{"Exposure D transition",f(result.Transition)+" m","Site with B or C immediately upwind is still D when within the greater of 183 m (600 ft) and 20h of a D condition."});
  result.Checks.Add(new[]{"Sampled extent",f(result.Available)+" m","≥ "+f(required)+" m needed on every ray."});
  var points=rays.SelectMany(ray=>ray.Where(p=>p.D>0&&p.D<=required)).ToList();if(points.Count>0){var groups=points.GroupBy(p=>Roughness(p.Cover)).OrderByDescending(g=>g.Count()).ToList();result.Roughness=groups[0].Key;result.Mix=string.Join("; ",groups.Select(g=>g.Key+" "+(100.0*g.Count()/points.Count).ToString("0",CultureInfo.InvariantCulture)+"%"));}
  result.Checks.Add(new[]{"Mapped surface roughness",result.Mix==""?"none":result.Mix,"Share of sampled points (B = trees / built-up / mangrove, C = open land / grass / crops / bare, D = open water). Land cover alone does not prove obstruction height."});
  bool complete=rays.Count>=3&&rays.All(ray=>ray.Count>0&&ray.First().D<=.01&&ray.Last().D>=required&&ray.TakeWhile(p=>p.D<required).All(p=>Roughness(p.Cover)!="?"));
  if(!complete||rays.Any(ray=>ray.FirstOrDefault(p=>p.D>=required)==null||Roughness(ray.First(p=>p.D>=required).Cover)=="?")){result.Reason="Ray extent is too short or land cover is unknown within the required fetch. Exposure is UNRESOLVED; missing data is not converted to C.";result.Checks.Add(new[]{"Suggested exposure",result.Exposure,result.Reason});return result;}
  double shortestB=double.PositiveInfinity;foreach(var ray in rays){double bRun=0;foreach(var sample in ray){if(Roughness(sample.Cover)!="B")break;bRun=sample.D;}shortestB=Math.Min(shortestB,bRun);double start=double.NaN;bool qualifies=false;foreach(var sample in ray){if(Roughness(sample.Cover)=="D"){if(double.IsNaN(start))start=sample.D;double run=sample.D-start;if(double.IsNaN(result.NearestWater)||start<result.NearestWater)result.NearestWater=start;result.LongestWater=Math.Max(result.LongestWater,run);if(start<=result.Transition&&run>=result.DFetch)qualifies=true;}else start=double.NaN;}if(qualifies)result.SmoothRays++;}
  result.ShortestB=shortestB;
  result.Checks.Add(new[]{"Continuous B from site",f(shortestB)+" m (shortest ray)","≥ "+f(result.BFetch)+" m needed on every ray for Exposure B."});
  result.Checks.Add(new[]{"Nearest mapped water (D)",WindTin.Finite(result.NearestWater)?f(result.NearestWater)+" m":"none","Must start at the site or within "+f(result.Transition)+" m."});
  result.Checks.Add(new[]{"Longest continuous water run",f(result.LongestWater)+" m","≥ "+f(result.DFetch)+" m needed for Exposure D."});
  result.Checks.Add(new[]{"Rays meeting the D test",result.SmoothRays+" / "+rays.Count,"Any qualifying ray is treated as D (more severe sector governs); confirm the water fetch on site."});
  if(result.SmoothRays>0){result.Exposure="D";result.Reason="Exposure D: mapped open water starts within "+f(result.Transition)+" m and runs at least "+f(result.DFetch)+" m on "+result.SmoothRays+" of "+rays.Count+" rays. "+(result.SmoothRays<rays.Count?"Only part of the ±45° sectors is water; D is the more severe screening result and governs. ":"")+"Confirm that the water surface is flat and unobstructed.";}
  else if(rays.All(ray=>ray.Where(p=>p.D<=result.BFetch).Concat(ray.Where(p=>p.D>=result.BFetch).Take(1)).All(p=>Roughness(p.Cover)=="B"))){result.Exposure="B";result.Reason="Exposure B: trees / built-up cover (Surface Roughness B) is mapped continuously for at least "+f(result.BFetch)+" m on every ray, and no qualifying water fetch exists. Confirm closely spaced obstructions of single-family-dwelling size or larger.";}
  else{result.Exposure="C";result.Reason="Exposure C: Exposure B is not met because Surface Roughness B is continuous for only "+f(shortestB)+" m (needs "+f(result.BFetch)+" m) on at least one ray, and Exposure D is not met"+(WindTin.Finite(result.NearestWater)?" (mapped water is too far or its fetch is too short)":" (no mapped water)")+". Exposure C applies where B and D do not.";}
  result.Checks.Add(new[]{"Suggested exposure",result.Exposure,result.Reason});return result;
 }
}
}
