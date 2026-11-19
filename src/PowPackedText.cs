using System;using System.IO;using System.IO.Compression;using System.Runtime.CompilerServices;using System.Text;using System.Xml;
namespace BeamScheduleFormatter {
// Owner (19 November 2026, "hindi ba masyadong malaki na yung xml file ko?" - a 245 MB Program of Works; chose
// "I-compress ang DXF text"): the beam and slab estimates keep the full text of every schedule DXF so a project can be
// re-read later. That text was stored as plain XML text, and the estimate itself is stored as text inside the POW, so a
// DXF of several MB was written out in full (and escaped twice) on every save, besides the compressed original under
// Project files. Long texts are now stored gzip-compressed (base64) with encoding="gzip-base64" on the element; short
// texts (manual schedules) stay readable. Every packed text is unpacked and compared before it is used, so a text that
// would not come back exactly is stored plain instead. Old files (plain text) open as before.
static class PowPackedText {
 internal const string Encoding="gzip-base64";
 const int MinLength=2048;const long MaxUnpacked=1L<<30;
 sealed class Box {internal string Value;}
 static readonly ConditionalWeakTable<string,Box> packed=new ConditionalWeakTable<string,Box>();
 /* writes text into the element; returns true when it was packed */
 internal static bool Write(XmlElement element,string text){text=text??"";string value=text.Length<MinLength?null:Pack(text);
  if(value==null){element.RemoveAttribute("encoding");element.InnerText=text;return false;}
  element.SetAttribute("encoding",Encoding);element.InnerText=value;return true;}
 internal static string Read(XmlElement element){string encoding=element.GetAttribute("encoding");if(encoding=="")return element.InnerText;
  if(encoding!=Encoding)throw new Exception("Unsupported text encoding in saved estimate: "+encoding);return Unpack(element.InnerText);}
 /* packed form of a text, or null when it must stay plain; cached per string instance (the estimates rebuild their
    snapshot for every change check) */
 internal static string Pack(string text){return packed.GetValue(text,t=>new Box{Value=Compress(t)}).Value;}/* thread-safe: saves build snapshots in the background */
 static string Compress(string text){try{byte[] bytes=new UTF8Encoding(false,true).GetBytes(text);string value;
   using(var output=new MemoryStream()){using(var gzip=new GZipStream(output,CompressionMode.Compress,true))gzip.Write(bytes,0,bytes.Length);value=Convert.ToBase64String(output.ToArray());}
   return value.Length>=text.Length||Unpack(value)!=text?null:value;}
  catch(EncoderFallbackException){return null;}}
 internal static string Unpack(string value){byte[] data;try{data=Convert.FromBase64String(value??"");}catch(FormatException){throw new Exception("Saved estimate text is damaged (not base64).");}
  try{using(var input=new GZipStream(new MemoryStream(data,false),CompressionMode.Decompress))using(var output=new MemoryStream()){byte[] buffer=new byte[65536];int n;long total=0;
    while((n=input.Read(buffer,0,buffer.Length))>0){total+=n;if(total>MaxUnpacked)throw new Exception("Saved estimate text is too large.");output.Write(buffer,0,n);}
    return new UTF8Encoding(false,true).GetString(output.GetBuffer(),0,(int)output.Length);}}
  catch(InvalidDataException){throw new Exception("Saved estimate text is damaged (not gzip).");}
  catch(IOException){throw new Exception("Saved estimate text is damaged (not gzip).");}
  catch(DecoderFallbackException){throw new Exception("Saved estimate text is damaged (not UTF-8).");}}
}
}
