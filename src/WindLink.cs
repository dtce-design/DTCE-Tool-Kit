using System;using System.Globalization;using System.IO;using System.Xml.Linq;
namespace BeamScheduleFormatter {
/* Wind Load sheet -> Purlin Design (owner, 24 November 2026: "automatic link ng loads sa purlin design"): the Wind Load sheet saves its
   components-and-cladding basis (qh, GCpi, roof slope, governing exposure / direction, site) whenever it calculates C&C; the Purlin sheet
   reads it and works out the purlin's own wind pressure and suction for the chosen roof zone from its span and spacing. The file lives
   in the shared Documents\DTCE Tool Kit folder, so the Store app and the .exe see the same link. */
sealed class WindLinkData {public double Qh,GCpi,Angle,Height;public string Exposure,Direction,Site;public DateTime Saved;}
static class WindLink {
 static readonly CultureInfo Ci=CultureInfo.InvariantCulture;
 static string FilePath{get{return Path.Combine(SharedData.Folder("Links",Path.Combine("DTCE Tool Kit","Links")),"wind-cc-link.xml");}}
 internal static void Save(CcSummary s,string site){try{new XElement("WindCcLink",new XAttribute("qh",s.Qh.ToString("R",Ci)),new XAttribute("gcpi",s.GCpi.ToString("R",Ci)),new XAttribute("angle",s.Angle.ToString("R",Ci)),new XAttribute("height",s.H.ToString("R",Ci)),new XAttribute("exposure",s.Exposure??""),new XAttribute("direction",s.Direction??""),new XAttribute("site",site??""),new XAttribute("saved",DateTime.Now.ToString("o",Ci))).Save(FilePath);}catch(Exception){/* the link is a convenience; the wind sheet works without it */}}
 internal static WindLinkData Load(){try{if(!File.Exists(FilePath))return null;var x=XElement.Load(FilePath);Func<string,double> d=n=>double.Parse((string)x.Attribute(n),Ci);
   return new WindLinkData{Qh=d("qh"),GCpi=d("gcpi"),Angle=d("angle"),Height=d("height"),Exposure=(string)x.Attribute("exposure"),Direction=(string)x.Attribute("direction"),Site=(string)x.Attribute("site"),Saved=DateTime.Parse((string)x.Attribute("saved"),Ci,DateTimeStyles.RoundtripKind)};}catch(Exception){return null;}}
 /* the purlin's C&C pressures in a roof zone: A = span x max(spacing, span / 3) */
 internal static double[] PurlinPressures(WindLinkData w,int zone,double span,double spacing,out double area,out double gcpPos,out double gcpNeg){area=span*Math.Max(spacing,span/3);gcpPos=WindComponentsCladding.RoofPos(area,w.Angle);gcpNeg=WindComponentsCladding.RoofNeg(area,zone,w.Angle);
  double p=Math.Max(w.Qh*(gcpPos+w.GCpi),0.77),s=Math.Min(w.Qh*(gcpNeg-w.GCpi),-0.77);return new[]{p,s};}
}
}
