using System;using System.Linq;using System.Collections.Generic;using System.Globalization;
namespace BeamScheduleFormatter {
// NSCP 2015 Section 207A.8 topographic factor with every criterion and substitution recorded per direction.
static partial class WindAssessment {
 static string F(double value){return WindTin.Finite(value)?value.ToString("0.###",CultureInfo.InvariantCulture):"Unresolved";}
 static void Unity(WindDirection value,string reason){value.K1=0;value.K2=value.K3=1;value.Kzt=1;value.TopographicReason=reason;value.TopographicChecks.Add(new[]{"Kzt result","1.00 (not amplified)",reason});value.TopographicChecks.Add(new[]{"Substitution","Kzt = (1 + K1 K2 K3)² = (1 + 0)² = 1.00","K1 = 0 because a required 207A.8.1 condition is not met; K2 and K3 are not needed."});}
 static List<WindSample> Ground(List<WindSample> ray){return ray.Select(p=>new WindSample{Lat=p.Lat,Lon=p.Lon,D=p.D,Cover=p.Cover,Z=WindTin.Finite(p.Z)&&p.Z<0?0:p.Z}).ToList();}
 public static List<WindDirection> Calculate(WindTerrain terrain,double height,double velocity,double kd,double? exposureHeight=null){
  if(terrain==null||!WindTin.Finite(height)||height<=0||height>150||!WindTin.Finite(velocity)||velocity<=0||!WindTin.Finite(kd)||kd<=0||kd>1)throw new Exception("Check reference height (0–150m), wind speed and Kd.");
  var results=new List<WindDirection>();
  for(int direction=0;direction<8;direction++){
   /* the ground over the sea is the sea surface: values below 0 m (sea-floor data / blending at the shore) are set to 0 m (owner, 24
      November 2026: "bakit yung upwind base below 0?"). Inland lakes and rivers keep their surface elevation from the data (a lake at
      700 m is not 0 m), so water cover alone does not set 0. */
   var up=Ground(terrain.Rays[direction*3+1]);var down=Ground(terrain.Rays[((direction+4)%8)*3+1]);int clamped=terrain.Rays[direction*3+1].Concat(terrain.Rays[((direction+4)%8)*3+1]).Count(p=>WindTin.Finite(p.Z)&&p.Z<0);var profile=down.Skip(1).Reverse().Select(p=>new WindSample{Lat=p.Lat,Lon=p.Lon,D=-p.D,Z=p.Z,Cover=p.Cover}).Concat(up).ToList();
   var exposure=AssessExposure(terrain,direction,exposureHeight??height);var r=new WindDirection{Name=Directions[direction],Exposure=exposure.Exposure,ExposureAudit=exposure,Shape="Unresolved",Profile=profile};results.Add(r);
   r.TopographicChecks.Add(new[]{"Height z for Kzt",F(height)+" m above local ground","Kzt varies with z through K3; exposure uses the fetch height separately."});
   if(clamped>0)r.TopographicChecks.Add(new[]{"Sea surface","0 m at "+clamped+" point(s)","Elevations below 0 m (sea floor in the elevation data, or values blended at the shore) are taken at mean sea level, 0 m. Inland lakes and rivers keep their surface elevation."});
   if(r.Exposure=="Unresolved"||profile.Count<5||profile.Any(p=>!WindTin.Finite(p.Z))){r.Status=r.TopographicReason="Exposure or elevation data is insufficient. Kzt is UNRESOLVED and is not assumed to be 1.00.";r.TopographicChecks.Add(new[]{"Data","UNRESOLVED",r.TopographicReason});continue;}
   r.Kz=Kz(height,r.Exposure);double threshold=r.Exposure=="B"?18.288:4.572,range=profile.Max(p=>p.Z)-profile.Min(p=>p.Z);
   /* owner (24 November 2026, "pakicheck yung Determination niya ng Hill kasi flat lang naman itsura"): the engineer can confirm a flat site */
   if(terrain.UserFlat){r.H=range;r.Shape="Flat (confirmed by the engineer)";r.TopographicChecks.Add(new[]{"Site topography","Flat (user input)","The engineer confirmed that there is no hill, ridge or escarpment that meets NSCP 2015 Section 207A.8.1. Sampled relief along the wind line "+F(range)+" m (global elevation model, includes trees and buildings)."});Unity(r,"Site confirmed flat by the engineer: no hill, ridge or escarpment, Kzt = 1.00.");}
   else if(range<threshold){r.H=range;r.Shape="Low relief";r.TopographicChecks.Add(new[]{"Condition 5: H ≥ "+F(threshold)+" m","NOT MET","Total sampled relief "+F(range)+" m along the wind line is below "+F(threshold)+" m ("+(r.Exposure=="B"?"60 ft for Exposure B":"15 ft for Exposure C/D")+")."});Unity(r,"Terrain relief "+F(range)+" m < "+F(threshold)+" m, so no hill, ridge or escarpment qualifies for speed-up.");}
   else{
    var crest=profile.OrderByDescending(p=>p.Z).First();var windward=profile.Where(p=>p.D>crest.D).ToList();var leeward=profile.Where(p=>p.D<crest.D).ToList();
    if(windward.Count==0||leeward.Count==0){r.Status=r.TopographicReason="The highest point lies at the end of the sampled profile. Extend the terrain investigation; Kzt is UNRESOLVED.";r.TopographicChecks.Add(new[]{"Crest location","UNRESOLVED",r.TopographicReason});continue;}
    double baseline=windward.Min(p=>p.Z),lee=leeward.Min(p=>p.Z),site=up[0].Z;r.H=crest.Z-baseline;r.X=-crest.D;r.Shape=crest.Z-lee<.3*r.H?"Escarpment":"Hill";var cross=Ground(terrain.Cross[direction]);if(r.Shape!="Escarpment"&&cross.Count>0&&cross.All(p=>WindTin.Finite(p.Z))&&cross.Max(p=>p.Z)-cross.Min(p=>p.Z)<.3*r.H)r.Shape="Ridge";
    r.TopographicChecks.Add(new[]{"Feature type",r.Shape,r.Shape=="Escarpment"?"Leeward side drops less than 0.3H: 2-D escarpment.":r.Shape=="Ridge"?"Cross-wind profile varies less than 0.3H: 2-D ridge.":"Falls on both sides along and across the wind: shaped like a 3-D hill (the conditions below decide whether it is one for Kzt)."});
    r.TopographicChecks.Add(new[]{"Crest / upwind base elevation",F(crest.Z)+" m / "+F(baseline)+" m","H is measured from the upwind terrain (NSCP Figure 207A.8-1)."});
    r.TopographicChecks.Add(new[]{"Feature height H",F(r.H)+" m","H = crest − upwind base."});
    r.TopographicChecks.Add(new[]{"Distance crest to site x",F(r.X)+" m",r.X>=0?"Site is upwind of the crest.":"Site is downwind of the crest."});
    var flank=profile.Where(p=>p.D>=crest.D).OrderBy(p=>p.D).ToList();double half=crest.Z-r.H/2;for(int n=1;n<flank.Count;n++)if(flank[n].Z<=half){var a=flank[n-1];var b=flank[n];double t=Math.Abs(b.Z-a.Z)<1e-9?0:(half-a.Z)/(b.Z-a.Z);r.Lh=a.D+t*(b.D-a.D)-crest.D;break;}
    r.TopographicChecks.Add(new[]{"Lh",F(r.Lh)+" m","Horizontal distance upwind of the crest to where the ground is H/2 below the crest."});
    bool tall=r.H>=threshold;r.TopographicChecks.Add(new[]{"Condition 5: H ≥ "+F(threshold)+" m",tall?"MET":"NOT MET","H = "+F(r.H)+" m; minimum "+(r.Exposure=="B"?"18 m (60 ft) for Exposure B":"4.5 m (15 ft) for Exposure C/D")+"."});
    if(!tall)Unity(r,"Feature height H = "+F(r.H)+" m is below "+F(threshold)+" m. A "+r.Shape.ToLowerInvariant()+" shape alone does not trigger speed-up.");
    else if(!WindTin.Finite(r.Lh)||r.Lh<=0){r.Status=r.TopographicReason="No upwind half-height point found within the sampled profile; Lh and Kzt are UNRESOLVED.";r.TopographicChecks.Add(new[]{"Lh","UNRESOLVED",r.TopographicReason});continue;}
    else{
     double slope=r.H/r.Lh,upper=baseline+r.H/2;bool steep=slope>=.2,upperHalf=site>=upper;
     r.TopographicChecks.Add(new[]{"Condition 4: H/Lh ≥ 0.2",steep?"MET":"NOT MET","H/Lh = "+F(r.H)+" / "+F(r.Lh)+" = "+F(slope)+"."});
     r.TopographicChecks.Add(new[]{"Condition 3: site in upper half or near crest",upperHalf?"MET":"NOT MET","Site ground "+F(site)+" m vs upper-half level "+F(upper)+" m (base + H/2)."});
     /* condition 2 from the profile: upwind terrain within 3.22 km beyond the feature's own flank (2 Lh) must be at most half as high */
     var beyond=windward.Where(p=>p.D-crest.D>=2*r.Lh&&p.D-crest.D<=3220).ToList();double upwindRelief=beyond.Count<3?double.NaN:beyond.Max(p=>p.Z)-beyond.Min(p=>p.Z);bool cond2=!WindTin.Finite(upwindRelief)||r.H>=2*upwindRelief;
     r.TopographicChecks.Add(new[]{"Condition 2: at least 2× the height of upwind features within 3.22 km",!WindTin.Finite(upwindRelief)?"REVIEW":cond2?"MET":"NOT MET",!WindTin.Finite(upwindRelief)?"Too few upwind samples; confirm on topographic maps.":"Upwind relief beyond the feature "+F(upwindRelief)+" m; H = "+F(r.H)+" m "+(cond2?"≥":"<")+" 2 × "+F(upwindRelief)+" = "+F(2*upwindRelief)+" m."});
     r.TopographicChecks.Add(new[]{"Condition 1: isolated, unobstructed by similar features for 100H or 3.22 km","REVIEW","Not decided from the elevation model; confirm on site and on topographic maps."});
     if(r.H<10)r.TopographicChecks.Add(new[]{"Elevation data accuracy","CHECK","H = "+F(r.H)+" m is within the accuracy of the global elevation model (about ±5-10 m; it includes trees and buildings). On flat ground this is usually not a real hill: confirm with a topographic survey and choose Site topography = Flat when the site is flat."});
     if(!steep||!upperHalf||!cond2)Unity(r,(!steep?"H/Lh = "+F(slope)+" < 0.2. ":"")+(!upperHalf?"The site ("+F(site)+" m) is below the upper half of the feature ("+F(upper)+" m). ":"")+(!cond2?"The feature is not twice as high as the upwind terrain (condition 2). ":"")+"Kzt = 1.00 even though the terrain is shaped like a "+r.Shape.ToLowerInvariant()+".");
     else{var k=Factors(r.H,r.Lh,r.X,height,r.Exposure,r.Shape);r.K1=k[0];r.K2=k[1];r.K3=k[2];r.Kzt=k[3];int i=r.Exposure=="B"?0:r.Exposure=="C"?1:2;double a=r.Shape=="Ridge"?new[]{1.3,1.45,1.55}[i]:r.Shape=="Escarpment"?new[]{.75,.85,.95}[i]:new[]{.95,1.05,1.15}[i],effective=Math.Max(r.Lh,2*r.H),mu=r.Shape=="Escarpment"&&r.X<0?4:1.5,gamma=r.Shape=="Ridge"?3:r.Shape=="Escarpment"?2.5:4;
      r.TopographicChecks.Add(new[]{"Lh used for K2, K3",F(effective)+" m",slope>.5?"H/Lh > 0.5: use H/Lh = 0.5 for K1 and Lh = 2H for K2 and K3.":"Lh as measured."});
      r.TopographicChecks.Add(new[]{"K1",F(r.K1),"K1 = (K1/(H/Lh)) × min(H/Lh, 0.5) = "+F(a)+" × "+F(Math.Min(.5,slope))+" (exposure "+r.Exposure+", "+r.Shape.ToLowerInvariant()+")"});
      r.TopographicChecks.Add(new[]{"K2",F(r.K2),"K2 = 1 − |x| / (μ Lh) = 1 − "+F(Math.Abs(r.X))+" / ("+F(mu)+" × "+F(effective)+"), not less than 0"});
      r.TopographicChecks.Add(new[]{"K3",F(r.K3),"K3 = e^(−γ z / Lh) = e^(−"+F(gamma)+" × "+F(height)+" / "+F(effective)+")"});
      r.TopographicChecks.Add(new[]{"Substitution","Kzt = (1 + "+F(r.K1)+" × "+F(r.K2)+" × "+F(r.K3)+")² = "+F(r.Kzt),"Full precision used; displayed values are rounded."});
      r.TopographicReason=r.K2==0?"Conditions 3 to 5 are met, but the site is beyond the speed-up zone (|x| ≥ μLh), so K2 = 0 and Kzt = 1.00.":"Conditions 3 to 5 are met; Kzt = "+F(r.Kzt)+" from the substitution above. Confirm conditions 1 and 2 on site.";}
    }
   }
   if(r.K1==0&&(r.Shape=="Hill"||r.Shape=="Ridge"||r.Shape=="Escarpment"))r.Shape="No qualifying "+r.Shape.ToLowerInvariant()+" (Kzt = 1)";
   r.Qz=.613*r.Kz*r.Kzt*kd*Math.Pow(velocity/3.6,2)/1000;r.Status=r.TopographicReason+" Elevation/land-cover screening; verify site evidence.";
  }
  return results;}
}
}
