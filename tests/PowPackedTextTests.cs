using System;using System.Collections.Generic;using System.IO;using System.Linq;using System.Reflection;using System.Text;using System.Windows.Forms;using System.Xml;using BeamScheduleFormatter;
// Compressed DXF text in the beam / slab estimates of a Program of Works (owner, 19 November 2026: a 245 MB POW;
// "I-compress ang DXF text"): saved compressed, read back exactly, old plain files still open, damaged text refused.
class PowPackedTextTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static T Get<T>(object o,string name){return (T)o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).GetValue(o);}
 static object Call(object o,string name,params object[] args){return o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Invoke(o,args);}
 static BeamRow Row(string mark){var r=new BeamRow{Mark=mark,Leg="2L",Diameter="O10",RevisedSpacing="1 - 50mm, REST AT 150mm TO FREE END"};for(int n=0;n<14;n++)r.Cells[n].Add("-");r.Cells[0][0]="300";r.Cells[1][0]="500";for(int n=2;n<8;n++)r.Cells[n][0]="2-O16";return r;}
 [STAThread]static void Main(){Application.EnableVisualStyles();AccessCodes.Granted=true;
  /* the packing itself */
  var sb=new StringBuilder();for(int i=0;i<20000;i++)sb.Append("  0\r\nLINE\r\n  8\r\nS-BEAM <B&1>\r\n 10\r\n"+(i*1.5)+"\r\n");string dxf=sb+"Ø ñ °";
  var doc=new XmlDocument();var e=doc.CreateElement("Source");doc.AppendChild(e);
  Check(PowPackedText.Write(e,dxf)&&e.GetAttribute("encoding")=="gzip-base64"&&e.InnerText.Length*4<dxf.Length,"long DXF text is stored compressed");
  Check(PowPackedText.Read(e)==dxf,"compressed text comes back exactly (CR LF, < & >, accents)");
  var small=doc.CreateElement("Small");Check(!PowPackedText.Write(small,"MANUAL\nGB-1")&&!small.HasAttribute("encoding")&&PowPackedText.Read(small)=="MANUAL\nGB-1","short manual text stays readable");
  var plain=doc.CreateElement("Plain");plain.InnerText=dxf;Check(PowPackedText.Read(plain)==dxf,"plain text of older files is read as before");
  var bad=doc.CreateElement("Bad");bad.SetAttribute("encoding","gzip-base64");bad.InnerText="AAAA";bool refused=false;try{PowPackedText.Read(bad);}catch(Exception ex){refused=ex.Message.Contains("damaged");}Check(refused,"damaged compressed text is refused, not misread");
  var lone=new string('x',5000)+"\ud800";var odd=doc.CreateElement("Odd");Check(!PowPackedText.Write(odd,lone)&&odd.InnerText==lone,"text that cannot round-trip stays plain");
  /* a whole Program of Works with a beam DXF source and a slab source */
  string previous=ProjectInfoStore.DirectoryPath,dir=Path.Combine(Path.GetTempPath(),"DTCE-packed-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);ProjectInfoStore.DirectoryPath=dir;
  try{var a=new Analysis();for(int i=1;i<=150;i++)a.Source.Add(Row("GB-"+i));string raw=ManualBeamSchedule.Serialize(a);Check(raw.Length>8000,"test schedule is long enough to be compressed");
   var groups=Enumerable.Range(0,9).Select(n=>new[]{"","Spacing (mm)",""}).ToArray();groups[0]=new[]{"12","Spacing (mm)","150"};groups[2]=new[]{"16","Number of bars","9"};
   string slabRaw=ManualSlabSchedule.Write(ManualSlabSchedule.Create("MANUAL-S1","125",groups))+"\n"+new string(' ',6000)+"\n";
   string pow=Path.Combine(dir,"Packed.pow.xml"),beamState,slabSnapshot;
   using(var f=new ProgramOfWorksForm()){var beam=Get<EstimatorForm>(f,"beam");beam.LoadCatalog(raw,"Ground.dxf");beam.AddEntry("Valid source","GB-1","2","1");var slab=Get<SlabEstimatorForm>(f,"slab");Call(slab,"AddSource",slabRaw,"Manual MANUAL-S1.xml");
    beamState=(string)Call(beam,"State");slabSnapshot=(string)Call(slab,"Snapshot");f.SaveTo(pow);}
   string xml=File.ReadAllText(pow);var d=new XmlDocument();d.LoadXml(xml);var b=new XmlDocument();b.LoadXml(d.DocumentElement["Beam"].InnerText);var s=new XmlDocument();s.LoadXml(d.DocumentElement["Slab"].InnerText);
   var bs=(XmlElement)b.SelectSingleNode("//ScheduleSources/Source");var ss=(XmlElement)s.SelectSingleNode("/SlabEstimate/Source");
   Check(b.DocumentElement.GetAttribute("version")=="7"&&bs.GetAttribute("encoding")=="gzip-base64"&&PowPackedText.Read(bs)==raw,"beam DXF text saved compressed in the POW (estimate version 7)");
   Check(s.DocumentElement.GetAttribute("version")=="2"&&ss.GetAttribute("encoding")=="gzip-base64","slab source text saved compressed in the POW (estimate version 2)");
   using(var r=new ProgramOfWorksForm()){r.OpenProjectXml(xml);Check((string)Call(Get<EstimatorForm>(r,"beam"),"State")==beamState,"reopened beam estimate is identical");Check((string)Call(Get<SlabEstimatorForm>(r,"slab"),"Snapshot")==slabSnapshot,"reopened slab estimate is identical");}
   /* the same project written the old way (plain text, versions 6 / 1) still opens identically and is larger */
   foreach(XmlElement x in b.SelectNodes("//ScheduleSources/Source")){string t=PowPackedText.Read(x);x.RemoveAttribute("encoding");x.InnerText=t;}b.DocumentElement.SetAttribute("version","6");
   foreach(XmlElement x in s.SelectNodes("/SlabEstimate/Source")){string t=PowPackedText.Read(x);x.RemoveAttribute("encoding");x.InnerText=t;}s.DocumentElement.SetAttribute("version","1");
   d.DocumentElement["Beam"].InnerText=b.OuterXml;d.DocumentElement["Slab"].InnerText=s.OuterXml;string old=d.OuterXml;Check(old.Length>xml.Length,"the compressed project is smaller than the plain one");
   using(var r=new ProgramOfWorksForm()){r.OpenProjectXml(old);Check((string)Call(Get<EstimatorForm>(r,"beam"),"State")==beamState,"an older plain-text project still opens with the same beam estimate");Check((string)Call(Get<SlabEstimatorForm>(r,"slab"),"Snapshot")==slabSnapshot,"an older plain-text project still opens with the same slab estimate");}}
  finally{ProjectInfoStore.DirectoryPath=previous;try{Directory.Delete(dir,true);}catch(Exception){}}
  Console.WriteLine("PASS: "+checks+" compressed estimate text checks");}
}
