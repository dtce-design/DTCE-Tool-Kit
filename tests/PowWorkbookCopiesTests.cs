using System;using System.IO;using System.IO.Compression;using System.Linq;using System.Text;using BeamScheduleFormatter;
// Owner (19 November 2026, "yung xml file di naman nabawasan ang file size"): one copy per linked workbook is kept in a POW.
class PowWorkbookCopiesTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static PowSourceFile Workbook(string name,string modified,string filler){var bytes=new MemoryStream();using(var zip=new ZipArchive(bytes,ZipArchiveMode.Create,true)){
   using(var w=new StreamWriter(zip.CreateEntry("docProps/core.xml").Open(),new UTF8Encoding(false)))w.Write("<?xml version=\"1.0\"?><cp:coreProperties xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\" xmlns:dcterms=\"http://purl.org/dc/terms/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">"+(modified==null?"":"<dcterms:modified xsi:type=\"dcterms:W3CDTF\">"+modified+"</dcterms:modified>")+"</cp:coreProperties>");
   using(var w=new StreamWriter(zip.CreateEntry("xl/workbook.xml").Open()))w.Write(filler);}
  bytes.Position=0;return PowSourceFile.Capture(name,PowSourceFiles.WorkbookKind,bytes);}
 static void Main(){
  /* a file saved by an earlier version: several copies of the same workbook */
  var store=new PowSourceFiles();var old=new[]{Workbook("BOQ.xlsm","2026-11-17T08:00:00Z","a"),Workbook("BOQ.xlsm","2026-11-19T01:30:00Z","b"),Workbook("BOQ.xlsm","2026-11-18T23:00:00Z","c"),Workbook("Other.xlsx","2026-11-01T00:00:00Z","d")};
  var archive=typeof(PowSourceFiles).GetField("files",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(store) as System.Collections.Generic.List<PowSourceFile>;archive.AddRange(old);
  store.CaptureText("Beam.dxf","Beam schedule DXF","0\nEOF");
  Check(PowSourceFiles.WorkbookModified(old[1])==new DateTime(2026,11,19,1,30,0,DateTimeKind.Utc),"modified date read from inside the workbook");
  int dropped=store.KeepNewestWorkbookCopies();
  Check(dropped==2&&store.Files.Count(f=>f.Name=="BOQ.xlsm")==1&&store.Files.Single(f=>f.Name=="BOQ.xlsm").Sha256==old[1].Sha256,"only the newest BOQ.xlsm copy is kept");
  Check(store.Files.Any(f=>f.Name=="Other.xlsx")&&store.Files.Any(f=>f.Name=="Beam.dxf"),"other workbooks and DXF files are kept");
  Check(store.KeepNewestWorkbookCopies()==0,"nothing more to remove");
  /* linking again replaces the copy instead of adding another */
  var relinked=Workbook("BOQ.xlsm","2026-11-19T05:00:00Z","e");store.Add(relinked);
  Check(store.Files.Count(f=>f.Name=="BOQ.xlsm")==1&&store.Files.Single(f=>f.Name=="BOQ.xlsm").Sha256==relinked.Sha256,"a new link replaces the stored copy");
  /* copies without dates: one is still kept */
  var undated=new PowSourceFiles();var u=typeof(PowSourceFiles).GetField("files",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(undated) as System.Collections.Generic.List<PowSourceFile>;u.Add(Workbook("X.xlsm",null,"1"));u.Add(Workbook("X.xlsm",null,"2"));
  Check(undated.KeepNewestWorkbookCopies()==1&&undated.Files.Count==1,"undated copies keep one");
  Console.WriteLine("PASS: "+checks+" workbook copy checks");}
}
