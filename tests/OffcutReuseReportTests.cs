using System;using System.Collections.Generic;using System.Linq;using BeamScheduleFormatter;
// Owner (23 November 2026): the BOQ reports show where reused offcuts come from - rebar, angle bars and other 6 m stock members.
class OffcutReuseReportTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static StockAllocation Bar(string beam,double cut,long pieces){return new StockAllocation{Entry=new EstimateInput{Beam=beam,Description="Test"},Line=new EstimateLine(),Demand=new StockDemand{Grade=40,Diameter=12,Pieces=pieces,Cut=cut},Choice=new StockChoice{Length=6,Cut=cut,Segments=1}};}
 static void Main(){
  /* rebar: a 4.5 m bar opens a 6 m bar; a 1.2 m bar of another mark is cut from its offcut */
  var a=Bar("B-1",4.5,1);var b=Bar("B-2",1.2,1);var plan=new List<StockAllocation>{a,b};RebarOffcuts.Allocate(plan);
  var rows=ProgramOfWorksForm.RebarOffcutRows(plan);
  Check(rows.Count==1&&rows[0][0].StartsWith("G40")&&rows[0][0].Contains("6 m")&&rows[0][1]=="1","one shared 6 m bar");
  Check(rows[0][2].StartsWith("B-1")&&rows[0][2].Contains("4.5 m")&&rows[0][3].StartsWith("B-2")&&rows[0][3].Contains("1.2 m")&&rows[0][4]=="0.3","opened for B-1, offcut reused by B-2, 0.3 m left");
  /* steel 6 m stock */
  var top=new SteelCutDemand{Member="ANG-001",Specification="L50x50x5mm | A36",Length=4.5,Quantity=2,KgM=3.77};var web=new SteelCutDemand{Member="ANG-002",Specification="L50x50x5mm | A36",Length=0.8,Quantity=2,KgM=3.77};
  var stocks=SteelCutPlanner.Allocate(new List<SteelCutDemand>{top,web});var s=ProgramOfWorksForm.SteelOffcutRows(stocks,x=>x);
  Check(s.Count==1&&s[0][0]=="L50x50x5mm | A36 / 6 m"&&s[0][1]=="2"&&s[0][2]=="ANG-001 — 1 pc(s) x 4.5 m"&&s[0][3]=="ANG-002 — 1 pc(s) x 0.8 m"&&s[0][4]=="0.7","angle bar stocks: opened for ANG-001, offcut reused by ANG-002 (a | in the specification is kept)");
  Check(ProgramOfWorksForm.SteelOffcutRows(SteelCutPlanner.Allocate(new List<SteelCutDemand>{new SteelCutDemand{Member="P-1",Specification="C",Length=5,Quantity=1,KgM=1}}),x=>x).Count==0,"a stock used by one member only is not listed");
  /* steel plates (owner: "walang narereuse na steel plates"): base plates and stiffeners of one thickness share a 4 x 8 ft sheet */
  var bp=new PlateDemand{Member="BP-1 Base plate",Length=.3,Width=.3,ThicknessMm=10,Quantity=8};var st=new PlateDemand{Member="ST-1 Stiffener",Length=.25,Width=.1,ThicknessMm=10,Quantity=16};
  var sheets=PlateCutPlanner.Allocate(new[]{bp,st});Check(sheets.Count==1&&sheets[0].Cuts.Count==24,"both rows cut from one sheet");
  var pr=ProgramOfWorksForm.PlateOffcutRows(sheets);Check(pr.Count==1&&pr[0][0].StartsWith("PL10-1")&&pr[0][2].StartsWith("BP-1 Base plate — 8 pc(s)")&&pr[0][3].StartsWith("ST-1 Stiffener — 16 pc(s)"),"plate reuse row: "+string.Join(" | ",pr.Count>0?pr[0]:new string[0]));
  Check(ProgramOfWorksForm.PlateSheetText(sheets,sheets,st)=="PL10-1 4 ft x 8 ft shared with BP-1 Base plate 8 pc(s)","row shows the shared sheet");
  Console.WriteLine("PASS: "+checks+" offcut reuse report checks");}
}
