using System;using System.Collections.Generic;using System.IO;using System.Linq;using System.Reflection;using System.Text;using System.Windows.Forms;using System.Xml;using BeamScheduleFormatter;
// Owner (19 November 2026): "alisin yung naka link na table sa dxf" - AutoCAD tables linked to another project's Excel file are
// not kept in the beam / slab estimate copy of a DXF; "lagyan mo ng save as option" - Program of Works has a Save As command.
class DxfLinkedTablesTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static T Get<T>(object o,string name){return (T)o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).GetValue(o);}
 static object Call(object o,string name,params object[] args){return o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Invoke(o,args);}
 const string Link=@"D:\DPWH - CFDEO\2023 - Lal-lo - Centro - Evacuation Center\Schedule of Beams.xlsx";
 static string BeamDxf(){var a=new Analysis{Prefix="B",Floor="TEST LEVEL",BeamCount=3};
  for(int i=0;i<3;i++){var r=new BeamRow();r.Numbers.Add("REF"+(i+1));r.SourceRows.Add(i+1);r.Cells[0].Add("300");r.Cells[1].Add("500");for(int c=2;c<8;c++)r.Cells[c].Add("3-#16");
   for(int c=8;c<11;c++)r.Cells[c].Add("5-2L-#10@100 C/C");r.Cells[11].Add("--");r.Cells[12].Add("--");r.Cells[13].Add("--");r.OriginalShear.Add(new[]{r.Cell(8),r.Cell(9),r.Cell(10)});a.Source.Add(r);}
  var built=Engine.Build(a,new Settings{Prefix="B",Floor="TEST LEVEL",Merge=false});return RevisedSchedule.Serialize(built.Drawing).Replace("\r\n","\n");}
 static string SlabDxf(){var b=new StringBuilder("0\nSECTION\n2\nENTITIES\n");Action<double,double,string> t=(x,y,text)=>b.Append("0\nTEXT\n10\n"+x.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"\n20\n"+y.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"\n1\n"+text+"\n");
  t(0,15000,"SLAB SCHEDULE");t(5000,9700,"FULL LENGTH");t(11000,9700,"FULL LENGTH");for(int r=0;r<2;r++){double y=8000-r*700;t(200,y+250,"S"+(r+1));t(3000,y,"125");for(int c=0;c<9;c++)t(5000+c*3000,y,c==1||c==3?"---":"#10@150 C/C");}return b.Append("0\nENDSEC\n0\nEOF\n").ToString();}
 /* inserts the linked table (when tables is true) into the drawing: an ACAD_TABLE in model space, its *T display block, and the
    DATALINK / TABLECONTENT / TABLEGEOMETRY objects; the section wrappers are added either way so the expected result is exact */
 static string Inject(string dxf,bool tables){
  string entity=tables?"0\nACAD_TABLE\n5\n7A1\n330\n1F\n100\nAcDbEntity\n8\n0\n100\nAcDbBlockReference\n2\n*T7\n10\n0.0\n20\n0.0\n100\nAcDbTable\n1\n2B-1\n1\n200\n1\n500\n":"";
  string block=tables?"0\nBLOCK\n5\n7B0\n100\nAcDbEntity\n8\n0\n100\nAcDbBlockBegin\n2\n*T7\n70\n1\n0\nMTEXT\n5\n7B1\n8\n0\n1\n200\n10\n1.0\n20\n1.0\n0\nENDBLK\n5\n7B2\n100\nAcDbEntity\n8\n0\n100\nAcDbBlockEnd\n":"";
  string keepBlock="0\nBLOCK\n5\n7C0\n8\n0\n2\nNORTH\n70\n0\n0\nLINE\n5\n7C1\n8\n0\n10\n0\n20\n0\n11\n1\n21\n1\n0\nENDBLK\n5\n7C2\n8\n0\n";
  string objects=tables?"0\nDATALINK\n5\n7D1\n330\n7D0\n100\nAcDbDataLink\n1\nAcExcel\n300\n"+Link+"\n301\nSheet1!A1:N40\n0\nTABLECONTENT\n5\n7D2\n100\nAcDbLinkedData\n300\n\n301\n\n1\nLINKEDTABLEDATACELL_BEGIN\n90\n0\n300\n\n1\n{\\fArial|b1|i0|c0;2B-1\n300\nVALUE\n304\nACVALUE_END\n309\nLINKEDTABLEDATACELL_END\n0\nTABLEGEOMETRY\n5\n7D3\n90\n19\n":"";
  string keepObjects="0\nDICTIONARY\n5\n7D0\n3\nACAD_DATALINK\n350\n7D1\n0\nTABLESTYLE\n5\n7E0\n3\nStandard\n";
  int entities=dxf.IndexOf("0\nSECTION\n2\nENTITIES\n",StringComparison.Ordinal);Check(entities>=0,"fixture has an ENTITIES section");int end=dxf.IndexOf("0\nENDSEC\n",entities,StringComparison.Ordinal);
  dxf=dxf.Substring(0,end)+entity+dxf.Substring(end);
  int blocks=dxf.IndexOf("0\nSECTION\n2\nBLOCKS\n",StringComparison.Ordinal);
  if(blocks>=0){int at=blocks+"0\nSECTION\n2\nBLOCKS\n".Length;dxf=dxf.Substring(0,at)+block+keepBlock+dxf.Substring(at);}
  else{entities=dxf.IndexOf("0\nSECTION\n2\nENTITIES\n",StringComparison.Ordinal);dxf=dxf.Substring(0,entities)+"0\nSECTION\n2\nBLOCKS\n"+block+keepBlock+"0\nENDSEC\n"+dxf.Substring(entities);}
  int eof=dxf.LastIndexOf("0\nEOF",StringComparison.Ordinal);dxf=dxf.Substring(0,eof)+"0\nSECTION\n2\nOBJECTS\n"+keepObjects+objects+"0\nENDSEC\n"+dxf.Substring(eof);return dxf;}
 static Control Find(Control root,string text){foreach(Control c in root.Controls){if(c.Text==text)return c;var inner=Find(c,text);if(inner!=null)return inner;}return null;}
 [STAThread]static void Main(){Application.EnableVisualStyles();AccessCodes.Granted=true;
  string plain=BeamDxf(),clean=Inject(plain,false),dirty=Inject(plain,true);int removed;
  string result=DxfLinkedTables.Remove(dirty,out removed);
  Check(result==clean,"only the linked table, its block and its data-link objects are taken out; every other line is kept");
  Check(removed==5,"ACAD_TABLE, *T block, DATALINK, TABLECONTENT and TABLEGEOMETRY counted ("+removed+")");
  Check(!result.Contains("Lal-lo")&&!result.Contains("0\nACAD_TABLE\n")&&!result.Contains("0\nTABLECONTENT\n")&&!result.Contains("0\nDATALINK\n")&&!result.Contains("*T7"),"no trace of the linked table is left (class names in CLASSES stay)");
  Check(result.Contains("NORTH")&&result.Contains("ACAD_DATALINK")&&result.Contains("TABLESTYLE"),"ordinary blocks, dictionaries and styles stay");
  Check(ReferenceEquals(DxfLinkedTables.Remove(plain),plain)&&ReferenceEquals(DxfLinkedTables.Remove(result),result),"a drawing without linked tables is returned untouched (and cleaning twice changes nothing)");
  Check(DxfLinkedTables.Remove(dirty.Replace("\n","\r\n"))==clean.Replace("\n","\r\n"),"Windows line endings are kept");
  var before=ManualBeamSchedule.Read(dirty,"Mabanguc.dxf");var after=ManualBeamSchedule.Read(result,"Mabanguc.dxf");
  Check(before.Source.Count==3&&before.Source.Select(r=>r.Mark).SequenceEqual(after.Source.Select(r=>r.Mark))&&Enumerable.Range(0,3).All(i=>Enumerable.Range(0,14).All(c=>before.Source[i].Cells[c].SequenceEqual(after.Source[i].Cells[c]))),"the beam schedule reads the same before and after");
  string manual=ManualBeamSchedule.Serialize(before)+"\nTABLECONTENT DATALINK";Check(ReferenceEquals(DxfLinkedTables.Remove(manual),manual),"manual schedules are not changed");
  string broken="0\nSECTION\n2\nENTITIES\n0\nACAD_TABLE\n1\nX\n0\nENDSEC\n";Check(DxfLinkedTables.Remove(broken)==broken,"a drawing that does not read (no EOF) is kept as it is");
  int at=plain.IndexOf("0\nENDSEC\n",plain.IndexOf("2\nENTITIES\n",StringComparison.Ordinal),StringComparison.Ordinal);string note=plain.Substring(0,at)+"0\nMTEXT\n8\n0\n1\nTABLECONTENT DATALINK ACAD_TABLE\n10\n0\n20\n-900\n"+plain.Substring(at);Check(ReferenceEquals(DxfLinkedTables.Remove(note),note),"a drawn note that only mentions those words is not removed");
  /* inside a Program of Works: the beam and slab estimates keep the cleaned copy, also when an older project is reopened */
  string previous=ProjectInfoStore.DirectoryPath,dir=Path.Combine(Path.GetTempPath(),"DTCE-linked-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);ProjectInfoStore.DirectoryPath=dir;
  try{string xml,beamState,slabSnapshot;
   using(var f=new ProgramOfWorksForm()){
    /* owner (19 November 2026, "isummarize yung buttons"): Save on the bar, Save As in the File menu */var fileMenu=Find(f,"File  ▾") as Button;var menu=fileMenu==null?null:fileMenu.Tag as ContextMenuStrip;Check(Find(f,"Save project") is Button&&menu!=null&&menu.Items.Cast<ToolStripItem>().Any(i=>i.Text=="Save Bill of Quantities As…"&&((ToolStripMenuItem)i).ShortcutKeyDisplayString=="Ctrl+Shift+S")&&Find(f,"Undo")==null&&Find(f,"Report  ▾")==null,"Save project on the bar (v10.0.34); Save As in the File menu; no Undo or Report button (Reports main tab)");
    var beam=Get<EstimatorForm>(f,"beam");beam.LoadCatalog(dirty,"Mabanguc Schedule of Beams.dxf");beamState=(string)Call(beam,"State");
    Check(!beamState.Contains("Lal-lo")&&!beamState.Contains("0\nTABLECONTENT\n")&&beamState.Contains("NORTH"),"beam estimate keeps the drawing without the linked table");
    var slab=Get<SlabEstimatorForm>(f,"slab");Call(slab,"AddSource",Inject(SlabDxf(),true),"Slab Schedule.dxf");slabSnapshot=(string)Call(slab,"Snapshot");
    var s=new XmlDocument();s.LoadXml(slabSnapshot);string slabText=string.Join("",s.SelectNodes("/SlabEstimate/Source").Cast<XmlElement>().Select(x=>PowPackedText.Read(x)));
    Check(slabText.Contains("SLAB SCHEDULE")&&!slabText.Contains("Lal-lo")&&!slabText.Contains("0\nACAD_TABLE\n"),"slab estimate keeps the drawing without the linked table");
    string pow=Path.Combine(dir,"Linked.pow.xml");f.SaveTo(pow);xml=File.ReadAllText(pow);}
   var d=new XmlDocument();d.LoadXml(xml);var b=new XmlDocument();b.LoadXml(d.DocumentElement["Beam"].InnerText);
   foreach(XmlElement x in b.SelectNodes("//ScheduleSources/Source")){Check(!PowPackedText.Read(x).Contains("Lal-lo"),"saved Bill of Quantities holds no linked table");x.RemoveAttribute("encoding");x.InnerText=dirty;}
   b.DocumentElement.SetAttribute("version","6");d.DocumentElement["Beam"].InnerText=b.OuterXml;
   using(var r=new ProgramOfWorksForm()){r.OpenProjectXml(d.OuterXml);string reopened=(string)Call(Get<EstimatorForm>(r,"beam"),"State");
    Check(reopened==beamState,"an older project that still holds the linked table opens with the cleaned drawing and the same estimate");}}
  finally{ProjectInfoStore.DirectoryPath=previous;try{Directory.Delete(dir,true);}catch(Exception){}}
  Console.WriteLine("PASS: "+checks+" linked-table and Save As checks");}
}
