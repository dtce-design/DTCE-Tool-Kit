using System;using System.Linq;using System.Collections.Generic;using BeamScheduleFormatter;
class MemberBbsTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 [STAThread]static void Main(){var cfg=CalculationStore.All().First(c=>c.Name.Contains("FC28")&&c.Name.Contains("G40/G60"));
  var col=MiscStructureTakeoff.Member("Column Estimate","C1","Corner column",3.0,400,400,4,40,20,8,0,0,10,"Spacing (mm)",150,50,0,false,0,cfg);
  var footBars=new[]{new FootingBarSpec{Diameter=16,Spacing=150},new FootingBarSpec{Diameter=16,Spacing=150},new FootingBarSpec(),new FootingBarSpec()};var ft=FootingTakeoff.Calculate("F1","Isolated footing",2,1.5,.4,2,.5,75,true,footBars,0,cfg,20,10,100);
  var sources=new List<BbsMemberSource>{new BbsMemberSource{Kind="Column",Id="C1",Quantity=4,WidthMm=400,DepthMm=400,Steel=col.Steel},new BbsMemberSource{Kind="Footing",Id="F1",Quantity=2,Steel=ft.Steel}};
  var notes=new List<string>();var pieces=MemberBbs.Pieces(sources,notes);
  var main=pieces.Where(p=>p.Group=="Column"&&p.Location.StartsWith("Column longitudinal")).ToList();Check(main.Count==1&&main[0].PerLine==8&&main[0].Lines==4&&main[0].Diameter==20,"column main bars: 8 per column x 4 columns");
  var hoops=pieces.Where(p=>p.Group=="Column"&&p.Shape.StartsWith("Hoop")).ToList();var line=col.Steel.Result.Lines.First(l=>l.Part=="Stirrups");Check(hoops.Count==1&&hoops[0].PerLine==line.TieSetsPerBeam&&Math.Abs(hoops[0].Length-line.MainHoopCut)<1e-9&&hoops[0].SecB==400,"column hoops: sets and cut from the take-off");
  Check(pieces.Count(p=>p.Group=="Footing")==2&&pieces.Where(p=>p.Group=="Footing").All(p=>p.Lines==2&&p.Shape=="Straight"),"footing bars both ways x 2 footings");
  Check(pieces.Select(p=>p.BarMark).Distinct().Count()==pieces.Count&&pieces.All(p=>p.BarMark.StartsWith("C:C1-")||p.BarMark.StartsWith("F:F1-")),"unique marks per member");
  /* kg in the schedule equals the take-off */
  double take=col.Steel.Result.Lines.Sum(l=>l.Kg)+ft.Steel.Result.Lines.Sum(l=>l.Kg),bbs=pieces.Sum(p=>p.Kg);Check(Math.Abs(take-bbs)<.02*take,"schedule weight matches the take-off ("+bbs.ToString("0.0")+" vs "+take.ToString("0.0")+" kg)");
  /* into a schedule without beams */
  var s=new CutSettings{Config=cfg};var r=new CutResult();BeamCutting.AddMembers(r,pieces,s);Check(r.Pieces.Count==pieces.Count&&r.Patterns.Count>0&&r.Pieces.First(p=>p.Shape.StartsWith("Hoop")).ShapeCode=="HOOP","members join the schedule with shapes and cutting patterns");
  using(var doc=BbsReport.Build(r,s,new BbsInfo{Number="BBS-02"}))Check(doc.Sheets.Count>=3,"schedule with columns and footings prints");
  Console.WriteLine("PASS: "+checks+" member bar bending schedule checks");}
}
