using System;using System.Collections.Generic;using System.Linq;using System.Xml;
namespace BeamScheduleFormatter {
/* One file for the Design Loads tab (owner, 26 November 2026: "isang xml sheet na lang yung design loads"): <DesignLoads> holds the
   <DesignSheet> of Dead Loads, Live Loads, Wind, Earthquake and Load Combinations. Sheets not yet opened keep their part until they open;
   single-sheet files of these five still open into their sheet. */
static class DesignLoadsFile {
 internal static readonly string[] Kinds={"Dead-Loads","Live-Loads","Wind-Design","RSA-Curve","Load-Combinations"};
 static readonly Dictionary<string,DesignSheetForm> open=new Dictionary<string,DesignSheetForm>();
 static readonly Dictionary<string,string> pending=new Dictionary<string,string>();
 internal static int Count;
 internal static void Register(DesignSheetForm f){open[f.Kind]=f;}
 internal static void Unregister(DesignSheetForm f){DesignSheetForm cur;if(open.TryGetValue(f.Kind,out cur)&&cur==f)open.Remove(f.Kind);}
 internal static void ApplyPending(DesignSheetForm f){string xml;if(!pending.TryGetValue(f.Kind,out xml))return;pending.Remove(f.Kind);try{f.Restore(xml);}catch(Exception){}}
 /* the open sheets' current inputs, plus the parts kept for sheets not opened since */
 internal static string Bundle(){var doc=new XmlDocument();var root=doc.CreateElement("DesignLoads");root.SetAttribute("version","1");doc.AppendChild(root);Count=0;
  foreach(var k in Kinds){string xml=null;DesignSheetForm f;if(open.TryGetValue(k,out f)&&!f.IsDisposed)xml=f.Snapshot();else pending.TryGetValue(k,out xml);if(xml==null)continue;
   var part=new XmlDocument{XmlResolver=null};part.LoadXml(xml);root.AppendChild(doc.ImportNode(part.DocumentElement,true));Count++;}
  using(var w=new System.IO.StringWriter()){doc.Save(w);return w.ToString();}}
 /* a Design Loads file into all five sheets, or a single sheet into its own */
 internal static string Open(string xml,DesignSheetForm from){var doc=new XmlDocument{XmlResolver=null};using(var reader=XmlReader.Create(new System.IO.StringReader(xml),new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null}))doc.Load(reader);
  var parts=doc.DocumentElement.Name=="DesignLoads"?doc.DocumentElement.SelectNodes("DesignSheet").Cast<XmlElement>().ToList():new List<XmlElement>{doc.DocumentElement};
  if(doc.DocumentElement.Name!="DesignLoads"&&doc.DocumentElement.Name!="DesignSheet")throw new Exception("Choose a Design Loads or design-sheet file.");
  var done=new List<string>();var errors=new List<string>();
  foreach(var e in parts){string k=e.GetAttribute("type");if(!Kinds.Contains(k)){errors.Add(k+" is not a Design Loads sheet");continue;}
   DesignSheetForm f;if(open.TryGetValue(k,out f)&&!f.IsDisposed){try{f.Restore(e.OuterXml);done.Add(Name(k));}catch(Exception ex){errors.Add(Name(k)+": "+ex.Message);}}else{pending[k]=e.OuterXml;done.Add(Name(k)+" (when opened)");}}
  return "Opened: "+(done.Count==0?"nothing":string.Join(", ",done))+(errors.Count>0?". Not opened: "+string.Join("; ",errors):"")+".";}
 static string Name(string k){return k=="Dead-Loads"?"Dead Loads":k=="Live-Loads"?"Live Loads":k=="Wind-Design"?"Wind Load":k=="RSA-Curve"?"Earthquake Load":"Load Combinations";}
}
/* the Earthquake Load sheet's seismic values for the Load Combinations sheet (owner, 26 November 2026: "inputs for seismic are connected to
   earthquake load") */
sealed class EarthquakeLinkData {public double Ca,I,Omega,Rho,R,Na=1,Nv=1;public int Zone,Soil;public DateTime Saved;}
static class EarthquakeLink {
 static readonly System.Globalization.CultureInfo Ci=System.Globalization.CultureInfo.InvariantCulture;
 static string FilePath{get{return System.IO.Path.Combine(SharedData.Folder("Links",System.IO.Path.Combine("DTCE Tool Kit","Links")),"earthquake-link.xml");}}
 internal static void Save(StaticLateralResult s){Save(s,0);}
 /* soil = STAAD STYP (1 SA ... 5 SE), 0 = not known */
 internal static void Save(StaticLateralResult s,int soil){try{new System.Xml.Linq.XElement("EarthquakeLink",new System.Xml.Linq.XAttribute("na",s.Na.ToString("R",Ci)),new System.Xml.Linq.XAttribute("nv",s.Nv.ToString("R",Ci)),new System.Xml.Linq.XAttribute("soil",soil),new System.Xml.Linq.XAttribute("ca",s.Ca.ToString("R",Ci)),new System.Xml.Linq.XAttribute("i",s.I.ToString("R",Ci)),new System.Xml.Linq.XAttribute("omega",s.Omega.ToString("R",Ci)),new System.Xml.Linq.XAttribute("rho",s.Rho.ToString("R",Ci)),new System.Xml.Linq.XAttribute("r",s.R.ToString("R",Ci)),new System.Xml.Linq.XAttribute("zone",s.Zone),new System.Xml.Linq.XAttribute("saved",DateTime.Now.ToString("o",Ci))).Save(FilePath);}catch(Exception){}}
 internal static EarthquakeLinkData Load(){try{if(!System.IO.File.Exists(FilePath))return null;var x=System.Xml.Linq.XElement.Load(FilePath);Func<string,double> d=n=>double.Parse((string)x.Attribute(n),Ci);
   return new EarthquakeLinkData{Ca=d("ca"),I=d("i"),Omega=d("omega"),Rho=d("rho"),R=d("r"),Zone=(int)d("zone"),Na=x.Attribute("na")!=null?d("na"):1,Nv=x.Attribute("nv")!=null?d("nv"):1,Soil=x.Attribute("soil")!=null?(int)d("soil"):0,Saved=DateTime.Parse((string)x.Attribute("saved"),Ci,System.Globalization.DateTimeStyles.RoundtripKind)};}catch(Exception){return null;}}
}
}
namespace BeamScheduleFormatter {
/* values passed between the Design Loads sheets (owner, 26 November 2026: "link mo lahat ng kailangan inputs sa isat isa"): a small file per
   sheet in the shared Links folder, so the link also holds across sessions */
static class DesignLoadsLink {
 static string PathOf(string name){return System.IO.Path.Combine(SharedData.Folder("Links",System.IO.Path.Combine("DTCE Tool Kit","Links")),name+"-link.xml");}
 internal static void Set(string name,System.Collections.Generic.Dictionary<string,string> values){try{var x=new System.Xml.Linq.XElement("Link",new System.Xml.Linq.XAttribute("saved",DateTime.Now.ToString("o",System.Globalization.CultureInfo.InvariantCulture)));
   /* line breaks (wind load cases) as U+2028: an attribute turns a plain line break into a space */foreach(var kv in values)if(kv.Key!="saved")x.Add(new System.Xml.Linq.XAttribute(/* keys like "+X" are not XML names */System.Xml.XmlConvert.EncodeLocalName(kv.Key),(kv.Value??"").Replace("\r","").Replace("\n","\u2028")));var path=PathOf(name);if(System.IO.File.Exists(path)){var old=System.Xml.Linq.XElement.Load(path);bool same=values.Where(kv=>kv.Key!="saved").All(kv=>(string)old.Attribute(System.Xml.XmlConvert.EncodeLocalName(kv.Key))==(kv.Value??"").Replace("\r","").Replace("\n","\u2028"));if(same)return;}x.Save(path);}catch(Exception){}}
 internal static System.Collections.Generic.Dictionary<string,string> Get(string name){try{var path=PathOf(name);if(!System.IO.File.Exists(path))return null;var x=System.Xml.Linq.XElement.Load(path);return x.Attributes().Where(a=>a.Name.LocalName!="saved").ToDictionary(a=>System.Xml.XmlConvert.DecodeName(a.Name.LocalName),a=>a.Value.Replace("\u2028","\n"));}catch(Exception){return null;}}
}
}
