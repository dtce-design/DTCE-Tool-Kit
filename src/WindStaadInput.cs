using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Text;
namespace BeamScheduleFormatter {
class WindStaadHeightRow {public string Direction,Exposure;public double Height,Kz,Kzt,Qz;}
class WindStaadBand {public string Axis;public int Type;public double Top,Windward,Leeward,Combined;}
// Wind data prepared for STAAD.Pro: velocity-pressure profile and wall intensities for DEFINE WIND LOAD (INT / HEIG).
static class WindStaadInput {
 static string F(double v){return v.ToString("0.###",CultureInfo.InvariantCulture);}
 internal static List<WindStaadHeightRow> Profile(WindBuildingInput building,double speed,double kd,IEnumerable<WindDirection> directions,double interval=3){
  building.Validate();if(!WindTin.Finite(interval)||interval<=0||!WindTin.Finite(speed)||speed<=0||!WindTin.Finite(kd)||kd<=0||kd>1)throw new Exception("Check height increment and wind parameters.");
  var heights=new SortedSet<double>{0,4.57,building.Eave,building.Height,building.Apex};for(double z=interval;z<building.Apex;z+=interval)heights.Add(Math.Round(z,6));
  var rows=new List<WindStaadHeightRow>();foreach(var d in directions){if(!new[]{"B","C","D"}.Contains(d.Exposure)||!WindTin.Finite(d.Kzt))continue;foreach(double z in heights.Where(h=>h<=building.Apex+1e-9)){double kz=WindBuildingLoads.Kz(d.Exposure,z),kzt=WindBuildingLoads.TopographyAt(d,z);rows.Add(new WindStaadHeightRow{Direction=d.Name,Exposure=d.Exposure,Height=z,Kz=kz,Kzt=kzt,Qz=.613*kz*kzt*kd*Math.Pow(speed/3.6,2)/1000});}}
  return rows;}
 // Windward external pressure at the top of each band plus the leeward suction magnitude; internal pressure cancels across the two walls.
 internal static List<WindStaadBand> WallBands(WindBuildingInput building,double speed,IList<WindDirection> directions){
  var rows=WindBuildingLoads.Calculate(building,speed,directions).Where(r=>r.GCpi>0).ToList();var bands=new List<WindStaadBand>();int type=0;
  foreach(var axis in rows.GroupBy(r=>r.Direction)){type++;var lee=axis.FirstOrDefault(r=>r.Surface=="Leeward wall");if(lee==null)continue;double leeward=lee.Qh*lee.G*lee.Cp;
   foreach(var w in axis.Where(r=>r.Surface=="Windward wall"&&r.Z>1e-9).OrderBy(r=>r.Z)){double windward=w.Q*w.G*w.Cp;bands.Add(new WindStaadBand{Axis=axis.Key,Type=type,Top=w.Z,Windward=windward,Leeward=leeward,Combined=windward+Math.Abs(leeward)});}}
  return bands;}
 /* each wind type is named like the report's load cases (owner, 24 November 2026: "pati yung input sa staad dapat may name din hindi
    type 1 lang"): "W1 FROM SW" */
 /* "TYPE 1 W1 FROM SW" like STAAD's own "TYPE 5 WIND 5" (owner: "ganyan ang naming niya"); short so STAAD keeps the whole name */
 internal static string TypeName(int type,string axis){string from=(axis??"").Split('/')[0].Trim();return "W"+type.ToString(System.Globalization.CultureInfo.InvariantCulture)+" FROM "+from;}
 internal static string Relation(string axis){var parts=(axis??"").Split('/');return parts.Length>1&&parts[1].Trim()=="normal"?"wind across the ridge":"wind along the ridge";}
 /* every face of every load case for STAAD (owner, 24 November 2026: "hindi ba magkakaiba ang intensity if wind from NW sa WW, LW at
    SW?"; "bakit di nagpapakita sa STAAD Wind Input"): windward wall by height band, leeward wall, side walls and each roof slope / zone,
    with Cp, q·G·Cp, qi·GCpi and the net pressure; (a) = +GCpi, (b) = -GCpi */
 /* STAAD axes from the bearing of +X (Y up, +Z = +X turned 90° clockwise on plan): a compass bearing as "+X", "-Z" ... or "" */
 internal static string AxisOf(double bearing,double xBearing){if(xBearing<0)return "";double d=((bearing-xBearing)%360+360)%360;return Math.Abs(d)<1e-6||Math.Abs(d-360)<1e-6?"+X":Math.Abs(d-180)<1e-6?"-X":Math.Abs(d-90)<1e-6?"+Z":Math.Abs(d-270)<1e-6?"-Z":"";}
 static string FaceAt(string outward){return outward==""?"":outward.Substring(1)+(outward[0]=='+'?" max":" min")+" face";}
 static string Flip(string axis){return axis==""?"":(axis[0]=='+'?"-":"+")+axis.Substring(1);}
 /* the global load direction of a wall pressure: positive p pushes against the outward normal, negative p (suction) pulls along it */
 static string LoadDir(string outward,double p){return outward==""?"":"G"+outward.Substring(1)+" "+((p>=0)==(outward[0]=='+')?"-":"+");}
 internal static List<string[]> FacePressures(WindBuildingInput building,double speed,IList<WindDirection> directions,double xBearing=-1){var ci=System.Globalization.CultureInfo.InvariantCulture;Func<double,string> f=v=>v.ToString("0.###",ci);
  var rows=WindBuildingLoads.Calculate(building,speed,directions);var list=new List<string[]>();string[] dir={"N","NE","E","SE","S","SW","W","NW"};int n=0;
  foreach(var axis in rows.GroupBy(r=>r.Direction)){n++;var first=axis.First();int idx=first.TerrainDirection;bool normal=axis.Key.EndsWith("normal");
   string ww=dir[idx]+(normal?" long wall":" gable end"),lw=dir[(idx+4)%8]+(normal?" long wall":" gable end"),sw=dir[(idx+2)%8]+" and "+dir[(idx+6)%8]+(normal?" gable ends":" long walls");
   foreach(int sign in new[]{1,-1}){string lc="W"+n.ToString(ci)+(sign>0?"a":"b")+" from "+dir[idx];
    foreach(var r in axis.Where(r=>(r.GCpi>0)==(sign>0))){if(r.Surface=="Windward wall"&&r.Z<=1e-9)continue;
     string face=r.Surface=="Windward wall"?"Windward wall ("+ww+")":r.Surface=="Leeward wall"?"Leeward wall ("+lw+")":r.Surface=="Side walls"?"Side walls ("+sw+")":r.Surface;
     string where=r.Surface=="Windward wall"?"up to z = "+f(r.Z)+" m":r.Case.Replace("; GCpi +","").Replace("; GCpi −","").Replace("All values","uniform, qh");
     string wwN=AxisOf(idx*45,xBearing),lwN=AxisOf(idx*45+180,xBearing),s1=AxisOf(idx*45+90,xBearing),s2=AxisOf(idx*45+270,xBearing);string staad="";
     if(xBearing>=0){if(r.Surface=="Windward wall")staad=FaceAt(wwN)+": "+LoadDir(wwN,r.P);else if(r.Surface=="Leeward wall")staad=FaceAt(lwN)+": "+LoadDir(lwN,r.P);else if(r.Surface=="Side walls")staad=FaceAt(s1)+": "+LoadDir(s1,r.P)+"; "+FaceAt(s2)+": "+LoadDir(s2,r.P);else staad="normal to the roof (member local Y)";}
     list.Add(new[]{lc,face,where,f(r.Cp),f(r.Q*r.G*r.Cp),f(-r.Qi*r.GCpi),f(r.P),staad});}}}
  return list;}
 /* load case templates: the wind type for windward + leeward, then the side walls and roof as member loads (comments - the X / Z axis
    and the member lists belong to the user's model) */
 internal static string LoadCaseTemplates(WindBuildingInput building,double speed,IList<WindDirection> directions,double xBearing=-1){var ci=System.Globalization.CultureInfo.InvariantCulture;Func<double,string> f=v=>v.ToString("0.###",ci);
  var rows=WindBuildingLoads.Calculate(building,speed,directions);string[] dir={"N","NE","E","SE","S","SW","W","NW"};var b=new StringBuilder(xBearing>=0?"\n\n* ---- WIND LOAD CASES (axes from +X = "+dir[(int)Math.Round(xBearing/45)%8]+", +Z = "+dir[(int)Math.Round((xBearing+90)/45)%8]+"; G = global axis, + / - = load direction) ----":"\n\n* ---- LOAD CASE TEMPLATES (set +X of the STAAD model in Building inputs to get the axes and signs) ----");int n=0,load=201;
  foreach(var axis in rows.GroupBy(r=>r.Direction)){n++;var first=axis.First();int idx=first.TerrainDirection;bool normal=axis.Key.EndsWith("normal");string sw=dir[(idx+2)%8]+" and "+dir[(idx+6)%8]+(normal?" gable ends":" long walls");
   foreach(int sign in new[]{1,-1}){var mine=axis.Where(r=>(r.GCpi>0)==(sign>0)).ToList();var side=mine.FirstOrDefault(r=>r.Surface=="Side walls");var lee=mine.FirstOrDefault(r=>r.Surface=="Leeward wall");var wws=mine.Where(r=>r.Surface=="Windward wall"&&r.Z>1e-9).ToList();
    string title="W"+n.ToString(ci)+(sign>0?"A":"B")+" WIND FROM "+dir[idx]+(sign>0?" +GCPI":" -GCPI");string travel=AxisOf(idx*45+180,xBearing);
    string wwN=AxisOf(idx*45,xBearing),lwN=AxisOf(idx*45+180,xBearing),s1=AxisOf(idx*45+90,xBearing),s2=AxisOf(idx*45+270,xBearing);string ww=dir[idx]+(normal?" long wall":" gable end"),lw=dir[(idx+4)%8]+(normal?" long wall":" gable end");
    b.Append("\n*");
    if(travel!=""){b.Append("\nLOAD ").Append(load++).Append(" LOADTYPE WIND TITLE ").Append(title);
     b.Append("\n* wind blows toward ").Append(travel).Append(" | WW = ").Append(ww).Append(" (").Append(FaceAt(wwN)).Append(") ").Append(wws.Count>0?f(wws.Min(r=>r.P))+" to "+f(wws.Max(r=>r.P)):"").Append(" kN/m2 -> ").Append(LoadDir(wwN,1)).Append(" | LW = ").Append(lw).Append(" (").Append(FaceAt(lwN)).Append(") ").Append(lee==null?"":f(lee.P)).Append(" kN/m2 -> ").Append(lee==null?"":LoadDir(lwN,lee.P));
     if(side!=null)b.Append("\n* SIDE WALLS = ").Append(sw).Append(": ").Append(f(side.P)).Append(" kN/m2 -> ").Append(FaceAt(s1)).Append(" ").Append(LoadDir(s1,side.P)).Append(", ").Append(FaceAt(s2)).Append(" ").Append(LoadDir(s2,side.P)).Append(" (member load = p x tributary width)");
     foreach(var roof in mine.Where(r=>r.Surface.Contains("roof")||r.Surface.StartsWith("Roof")))b.Append("\n* ROOF ").Append(roof.Surface).Append(" (").Append(roof.Case.Replace("; GCpi +","").Replace("; GCpi −","")).Append("): ").Append(f(roof.P)).Append(" kN/m2 normal to the roof x tributary width");
     b.Append("\nWIND LOAD ").Append(travel.Substring(1)).Append(travel[0]=='-'?" -1":" 1").Append(" TYPE ").Append(n).Append("\n* (WIND LOAD = INT on the windward face = WW + |LW|; if you apply WW and LW as member loads instead, delete this line)");}
    else{b.Append("\n* LOAD ").Append(load++).Append(" LOADTYPE WIND TITLE ").Append(title);
     b.Append("\n* WIND LOAD X 1 TYPE ").Append(n).Append("   <- set \"+X axis of the STAAD model points to\" in Building inputs to get the axis and sign");
     if(side!=null)b.Append("\n* MEMBER LOAD - side walls (").Append(sw).Append("): p = ").Append(f(side.P)).Append(" kN/m2 outward x tributary width");
     foreach(var roof in mine.Where(r=>r.Surface.Contains("roof")||r.Surface.StartsWith("Roof")))b.Append("\n* MEMBER LOAD - ").Append(roof.Surface).Append(" (").Append(roof.Case.Replace("; GCpi +","").Replace("; GCpi −","")).Append("): p = ").Append(f(roof.P)).Append(" kN/m2 normal to the roof x tributary width");}}}
  return b.ToString();}
 internal static string Commands(IEnumerable<WindStaadBand> bands){var b=new StringBuilder("UNIT METER KN\nDEFINE WIND LOAD");foreach(var axis in bands.GroupBy(x=>x.Type)){var list=axis.OrderBy(x=>x.Top).ToList();b.Append("\nTYPE ").Append(axis.Key).Append(" ").Append(TypeName(axis.Key,list[0].Axis)).Append("\n* ").Append(TypeName(axis.Key,list[0].Axis)).Append(", ").Append(Relation(list[0].Axis)).Append(": windward + leeward walls, kN/m2, heights from ground\nINT ").Append(string.Join(" ",list.Select(x=>F(x.Combined)))).Append(" HEIG ").Append(string.Join(" ",list.Select(x=>F(x.Top))));}return b.ToString();}
}
}
