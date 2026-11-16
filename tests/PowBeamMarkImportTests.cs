using System;using System.Linq;using System.Collections.Generic;using BeamScheduleFormatter;
class PowBeamMarkImportTests {
 static int checks;static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
 static string[] Screenshot={"GB-1","GB-2","GB-3","B-1","B-2","B-3","2B-1","2B-2","2B-3","2B-4","2B-5","2B-6","3B-1","3B-2","3B-3","3B-4","3B-5","3B-6","LRB"};
 static Dxf Fixture(string[] marks){
  var a=new Analysis{Prefix="B",Floor="TEST LEVEL",BeamCount=marks.Length};
  for(int i=0;i<marks.Length;i++){
   var r=new BeamRow();r.Numbers.Add("REF"+(i+1));r.SourceRows.Add(i+1);
   r.Cells[0].Add("300");r.Cells[1].Add("500");for(int c=2;c<8;c++)r.Cells[c].Add("3-#16");
   for(int c=8;c<11;c++)r.Cells[c].Add("5-2L-#10@100 C/C");r.Cells[11].Add("--");r.Cells[12].Add("--");r.Cells[13].Add("--");
   r.OriginalShear.Add(new[]{r.Cell(8),r.Cell(9),r.Cell(10)});a.Source.Add(r);
  }
  var built=Engine.Build(a,new Settings{Prefix="B",Floor="TEST LEVEL",Merge=false});
  var ids=built.Drawing.ModelEntities().Where(e=>(e.Type=="TEXT"||e.Type=="MTEXT")&&built.Rows.Any(r=>r.Mark==e.Get(1))).OrderByDescending(e=>e.Num(20)).ToArray();
  Check(ids.Length==marks.Length,"Fixture has one mark cell per row");for(int i=0;i<marks.Length;i++)ids[i].Set(1,marks[i]);return built.Drawing;
 }
 static Analysis Read(Dxf d){return ManualBeamSchedule.Read(RevisedSchedule.Serialize(d),"any filename with spaces.dxf");}
 static void Reject(Action action,string why){bool failed=false;try{action();}catch(Exception){failed=true;}Check(failed,why);}
 static void Main(){
  var fixture=Fixture(Screenshot);var parsed=Read(fixture);
  Check(parsed.Source.Count==Screenshot.Length&&parsed.BeamCount==Screenshot.Length,"All screenshot rows imported exactly once");
  Check(parsed.Source.Select(r=>r.Mark).SequenceEqual(Screenshot),"Hyphens, floor prefixes and bare LRB retained");
  Check(!parsed.Source.Any(r=>r.Mark.StartsWith("REF")),"Original references on the right are not mistaken for beam rows");
  var footer=Fixture(new[]{"GB-1","LRB"});var footerWidthHeader=footer.ModelEntities().Single(e=>e.Get(1)=="B");var note=new Entity();note.P.Add(new Pair(0,"MTEXT"));note.Set(1,"B");note.Set(10,footerWidthHeader.Num(10));note.Set(20,-1000.0);int section=footer.Records.FindIndex(e=>e.Type=="SECTION"&&e.Get(2)=="ENTITIES");int end=footer.Records.FindIndex(section+1,e=>e.Type=="ENDSEC");footer.Records.Insert(end,note);Check(Read(footer).Source.Count==2,"Unrelated lower B text does not replace the real header");
  var legacy=Read(Fixture(Enumerable.Range(1,Screenshot.Length).Select(n=>"B-"+n.ToString("000")).ToArray()));
  for(int i=0;i<parsed.Source.Count;i++){
   Check(Enumerable.Range(0,12).All(c=>parsed.Source[i].Cells[c].SequenceEqual(legacy.Source[i].Cells[c])),"Schedule data retained for "+Screenshot[i]);
   var x=EstimatorEngine.Calculate(parsed.Source[i],parsed.Source[i].Mark,6,new EstimateOptions());var y=EstimatorEngine.Calculate(legacy.Source[i],legacy.Source[i].Mark,6,new EstimateOptions());
   Check(Math.Abs(x.Total-y.Total)<1e-9&&x.Ties==y.Ties,"Rebar/tie quantities unaffected by mark names: "+Screenshot[i]);
  }
  var alternate=Read(Fixture(new[]{"B1","2B1","RD-001","B-1A","GB – 2",@"TB\U+20133"}));Check(alternate.Source.Select(r=>r.Mark).SequenceEqual(new[]{"B1","2B1","RD-001","B-1A","GB-2","TB-3"}),"Legacy, suffix and dash variants accepted");
  var split=Fixture(new[]{"B-1"});var mark=split.ModelEntities().Single(e=>e.Get(1)=="B-1");mark.P.RemoveAll(p=>p.Code==1||p.Code==3);mark.P.Add(new Pair(3,@"{\fArial|b0;GB-"));mark.P.Add(new Pair(1,"1}"));Check(Read(split).Source.Single().Mark=="GB-1","Formatted/chunked MTEXT mark is read in full");
  Reject(()=>Read(Fixture(new[]{"GB-1","gb-1"})),"Duplicate marks rejected case-insensitively");
  Reject(()=>Read(Fixture(new[]{"B-1","B-2 TO B-4"})),"Dimensioned row with unsupported mark range is not silently omitted");
  var missing=Fixture(new[]{"GB-1"});var b=missing.ModelEntities().Single(e=>e.Get(1)=="B");var cell=missing.ModelEntities().First(e=>e.Get(1)=="300"&&Math.Abs(e.Num(10)-b.Num(10))<1);missing.Records.Remove(cell);Reject(()=>Read(missing),"Missing width is still rejected");
  foreach(string header in new[]{"B","D","BEAM MARK","SFR","LEFT","MIDSPAN","RIGHT","SPACING"})Check(!PowBeamMarks.IsMark(header),"Header cannot become a beam mark: "+header);
  Console.WriteLine(checks+" POW mark import, original-reference isolation, quantity and invalid-row checks passed.");
 }
}
