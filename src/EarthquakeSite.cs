using System;using System.Collections.Generic;using System.Globalization;using System.IO;using System.Linq;using System.Net;using System.Text;using System.Text.RegularExpressions;
namespace BeamScheduleFormatter {
/* Earthquake Load site data (owner, 19 November 2026: "input coordinates determine nearest active fault, determine Seismic Source
   Type. ilagay ang historical records ng earthquakes"; "For fault line check phivolcs"; the owner chose the automatic HazardHunterPH
   look-up). One request per look-up, made by the user:
   - nearest active fault, ground shaking and liquefaction from HazardHunterPH (DOST-PHIVOLCS / GeoRiskPH), the same assessment the
     HazardHunterPH website shows for a point;
   - historical earthquakes from the USGS ANSS Comprehensive Catalog (public domain). */
static class MiniJson {
 public static object Parse(string text){int i=0;var v=Value(text,ref i);return v;}
 static void Ws(string s,ref int i){while(i<s.Length&&char.IsWhiteSpace(s[i]))i++;}
 static object Value(string s,ref int i){Ws(s,ref i);if(i>=s.Length)throw new FormatException("Unexpected end of JSON.");char c=s[i];
  if(c=='{'){var d=new Dictionary<string,object>(StringComparer.Ordinal);i++;Ws(s,ref i);if(s[i]=='}'){i++;return d;}while(true){Ws(s,ref i);string k=Str(s,ref i);Ws(s,ref i);if(s[i]!=':')throw new FormatException("Expected ':' in JSON.");i++;d[k]=Value(s,ref i);Ws(s,ref i);if(s[i]==','){i++;continue;}if(s[i]=='}'){i++;return d;}throw new FormatException("Expected ',' or '}' in JSON.");}}
  if(c=='['){var l=new List<object>();i++;Ws(s,ref i);if(s[i]==']'){i++;return l;}while(true){l.Add(Value(s,ref i));Ws(s,ref i);if(s[i]==','){i++;continue;}if(s[i]==']'){i++;return l;}throw new FormatException("Expected ',' or ']' in JSON.");}}
  if(c=='"')return Str(s,ref i);
  if(s.Length-i>=4&&s.Substring(i,4)=="true"){i+=4;return true;}if(s.Length-i>=5&&s.Substring(i,5)=="false"){i+=5;return false;}if(s.Length-i>=4&&s.Substring(i,4)=="null"){i+=4;return null;}
  int start=i;while(i<s.Length&&"+-0123456789.eE".IndexOf(s[i])>=0)i++;if(i==start)throw new FormatException("Unexpected character in JSON.");return double.Parse(s.Substring(start,i-start),NumberStyles.Float,CultureInfo.InvariantCulture);}
 static string Str(string s,ref int i){if(s[i]!='"')throw new FormatException("Expected a JSON string.");i++;var b=new StringBuilder();while(i<s.Length){char c=s[i++];if(c=='"')return b.ToString();if(c!='\\'){b.Append(c);continue;}char e=s[i++];switch(e){case 'n':b.Append('\n');break;case 't':b.Append('\t');break;case 'r':b.Append('\r');break;case 'b':b.Append('\b');break;case 'f':b.Append('\f');break;case 'u':b.Append((char)int.Parse(s.Substring(i,4),NumberStyles.HexNumber));i+=4;break;default:b.Append(e);break;}}throw new FormatException("Unterminated JSON string.");}
 public static object Get(object node,params string[] path){foreach(var key in path){var d=node as Dictionary<string,object>;if(d==null||!d.TryGetValue(key,out node))return null;}return node;}
 public static string Text(object node,params string[] path){var v=Get(node,path);return v==null?"":Convert.ToString(v,CultureInfo.InvariantCulture);}
}
class FaultSite {public string Location="",NearestText="",FaultName="",Segment="",Direction="",GroundShaking="",Liquefaction="",GroundRupture="",Retrieved="",Source="";public double DistanceKm=double.NaN,FaultLat=double.NaN,FaultLon=double.NaN;public int DateMapped;}
class QuakeEvent {public DateTime TimeUtc;public double Magnitude,DepthKm,Lat,Lon,DistanceKm;public string Place="",MagType="",Id="";}
class FaultParameters {public string Name,Basis;public double Mmax,SlipRate;}
static class EarthquakeSite {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 internal const string HazardHunter="https://hazardhunter.georisk.gov.ph",GeoRiskApi="https://api.georisk.gov.ph",Usgs="https://earthquake.usgs.gov/fdsnws/event/1/query";
 static string UserAgent{get{return "DTCE-Tool-Kit/"+Engine.Version+" (desktop design sheet)";}}
 static string Request(string url,string method,CookieContainer jar,string body,IDictionary<string,string> headers){ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;var r=(HttpWebRequest)WebRequest.Create(url);r.Method=method;r.UserAgent=UserAgent;r.Timeout=r.ReadWriteTimeout=30000;r.CookieContainer=jar;r.Accept="application/json, text/html;q=0.9, */*;q=0.8";r.AutomaticDecompression=DecompressionMethods.GZip|DecompressionMethods.Deflate;
  if(headers!=null)foreach(var h in headers){if(h.Key=="Accept")r.Accept=h.Value;else r.Headers[h.Key]=h.Value;}
  if(body!=null){var bytes=Encoding.UTF8.GetBytes(body);r.ContentType="application/x-www-form-urlencoded; charset=UTF-8";r.ContentLength=bytes.Length;using(var s=r.GetRequestStream())s.Write(bytes,0,bytes.Length);}
  using(var response=(HttpWebResponse)r.GetResponse())using(var reader=new StreamReader(response.GetResponseStream(),Encoding.UTF8))return reader.ReadToEnd();}
 internal static void ValidateCoordinates(double lat,double lon){if(double.IsNaN(lat)||double.IsNaN(lon)||lat<4||lat>22||lon<114||lon>128)throw new Exception("Enter decimal latitude 4 to 22 N and longitude 114 to 128 E (Philippines).");}
 /* HazardHunterPH point assessment: page session -> passport token -> hazards assessment */
 public static FaultSite NearestFault(double lat,double lon){ValidateCoordinates(lat,lon);var jar=new CookieContainer();Request(HazardHunter+"/","GET",jar,null,null);
  string tokenJson=Request(HazardHunter+"/passport-token","GET",jar,null,new Dictionary<string,string>{{"X-Requested-With","XMLHttpRequest"},{"Accept","application/json"}});string token=MiniJson.Text(MiniJson.Parse(tokenJson),"access_token");if(token=="")throw new Exception("HazardHunterPH did not return an access token. Try again later or check the site in a browser.");
  string json=Request(GeoRiskApi+"/api/assessments/hazards","POST",jar,"latitude="+lat.ToString("R",Inv)+"&longitude="+lon.ToString("R",Inv),new Dictionary<string,string>{{"Authorization","Bearer "+token},{"Accept","application/json"},{"X-Requested-With","XMLHttpRequest"}});
  return ReadAssessment(json);}
 internal static FaultSite ReadAssessment(string json){var root=MiniJson.Parse(json);var data=MiniJson.Get(root,"data");if(data==null)throw new Exception("HazardHunterPH returned no assessment: "+MiniJson.Text(root,"message"));
  var f=new FaultSite{Location=MiniJson.Text(data,"location","name"),NearestText=MiniJson.Text(data,"activeFault","nearest_active_fault"),FaultName=MiniJson.Text(data,"activeFault","fault_name"),Segment=MiniJson.Text(data,"activeFault","segment_name"),Direction=MiniJson.Text(data,"activeFault","direction"),GroundShaking=MiniJson.Text(data,"groundShaking","assessment"),Liquefaction=MiniJson.Text(data,"liquefaction","assessment"),GroundRupture=MiniJson.Text(data,"groundRupture","assessment"),Retrieved=DateTime.Now.ToString("yyyy-MM-dd HH:mm",Inv),Source="HazardHunterPH (DOST-PHIVOLCS / GeoRiskPH)"};
  double v;string units=MiniJson.Text(data,"activeFault","units").ToLowerInvariant();if(double.TryParse(MiniJson.Text(data,"activeFault","distance"),NumberStyles.Float,Inv,out v))f.DistanceKm=units=="m"||units=="meters"?v/1000:v;
  if(double.TryParse(MiniJson.Text(data,"activeFault","latitude"),NumberStyles.Float,Inv,out v))f.FaultLat=v;if(double.TryParse(MiniJson.Text(data,"activeFault","longitude"),NumberStyles.Float,Inv,out v))f.FaultLon=v;int y;if(int.TryParse(MiniJson.Text(data,"activeFault","datemapped"),out y))f.DateMapped=y;
  if(f.FaultName==""&&f.NearestText=="")throw new Exception("HazardHunterPH returned no active-fault information for this point.");return f;}
 public static List<QuakeEvent> History(double lat,double lon,double radiusKm,double minMagnitude,int fromYear){ValidateCoordinates(lat,lon);if(radiusKm<=0||radiusKm>500)throw new Exception("Search radius must be more than 0 and up to 500 km.");if(minMagnitude<3||minMagnitude>9)throw new Exception("Minimum magnitude must be 3 to 9.");
  string url=Usgs+"?format=geojson&latitude="+lat.ToString("R",Inv)+"&longitude="+lon.ToString("R",Inv)+"&maxradiuskm="+radiusKm.ToString("R",Inv)+"&minmagnitude="+minMagnitude.ToString("R",Inv)+"&starttime="+fromYear.ToString(Inv)+"-01-01&orderby=time&limit=2000";
  return ReadUsgs(Request(url,"GET",null,null,null),lat,lon);}
 internal static List<QuakeEvent> ReadUsgs(string json,double lat,double lon){var list=new List<QuakeEvent>();var features=MiniJson.Get(MiniJson.Parse(json),"features") as List<object>;if(features==null)throw new Exception("The USGS catalog returned no event list.");
  foreach(var item in features){var c=MiniJson.Get(item,"geometry","coordinates") as List<object>;var mag=MiniJson.Get(item,"properties","mag");var time=MiniJson.Get(item,"properties","time");if(c==null||c.Count<2||mag==null||time==null)continue;double qlon=Convert.ToDouble(c[0],Inv),qlat=Convert.ToDouble(c[1],Inv);
   list.Add(new QuakeEvent{Lat=qlat,Lon=qlon,DepthKm=c.Count>2&&c[2]!=null?Convert.ToDouble(c[2],Inv):double.NaN,Magnitude=Convert.ToDouble(mag,Inv),TimeUtc=new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).AddMilliseconds(Convert.ToDouble(time,Inv)),Place=MiniJson.Text(item,"properties","place"),MagType=MiniJson.Text(item,"properties","magType"),Id=MiniJson.Text(item,"id"),DistanceKm=Haversine(lat,lon,qlat,qlon)});}
  return list.OrderByDescending(q=>q.Magnitude).ThenBy(q=>q.DistanceKm).ToList();}
 internal static double Haversine(double lat1,double lon1,double lat2,double lon2){double r=6371.0088,p1=lat1*Math.PI/180,p2=lat2*Math.PI/180,dp=p2-p1,dl=(lon2-lon1)*Math.PI/180;double a=Math.Sin(dp/2)*Math.Sin(dp/2)+Math.Cos(p1)*Math.Cos(p2)*Math.Sin(dl/2)*Math.Sin(dl/2);return 2*r*Math.Asin(Math.Min(1,Math.Sqrt(a)));}
 /* NSCP 2015 Table 208-3 seismic source type: A = Mmax >= 7.0 and slip rate >= 5 mm/yr; C = Mmax < 6.5 and slip rate <= 2 mm/yr; B = all others */
 internal static string SourceType(double mmax,double slipRate){if(double.IsNaN(mmax)||double.IsNaN(slipRate)||mmax<=0||slipRate<0)return "";if(mmax>=7.0&&slipRate>=5)return "A";if(mmax<6.5&&slipRate<=2)return "C";return "B";}
 /* Suggested values for the two systems most often met in practice; every other fault needs Mmax and slip rate from PHIVOLCS or a
    site study. They are starting values to verify, not PHIVOLCS data. */
 internal static FaultParameters Suggested(string faultName){string n=(faultName??"").ToUpperInvariant();
  if(n.Contains("PHILIPPINE FAULT"))return new FaultParameters{Name="Philippine Fault Zone",Mmax=7.5,SlipRate=20,Basis="Suggested: major strike-slip system with historical M7+ events and slip rates of a few cm per year. Verify for the segment."};
  if(n.Contains("VALLEY FAULT"))return new FaultParameters{Name="Valley Fault System",Mmax=7.2,SlipRate=5,Basis="Suggested: M7.2 West Valley Fault scenario (MMEIRS) and published slip-rate estimates of about 5-7 mm/yr. Verify for the segment."};
  return null;}
}
}
