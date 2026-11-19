using System;using System.Linq;using System.Collections.Generic;using System.Windows.Forms;using BeamScheduleFormatter;
class BeamCuttingListTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}static void Near(double a,double b,string why){Check(Math.Abs(a-b)<1e-6,why+" ("+a+" vs "+b+")");}
 static BeamRow Row(){var r=new BeamRow{Mark="B-1"};r.Cells[0].Add("300");r.Cells[1].Add("500");r.Cells[2].Add("2-#16");r.Cells[3].Add("3-#16");r.Cells[4].Add("2-#16");r.Cells[5].Add("4-#20");r.Cells[6].Add("2-#20");r.Cells[7].Add("4-#20");r.Cells[8].Add("1-2L-#10@100 C/C");r.Cells[9].Add("1-2L-#10@200 C/C");r.Cells[10].Add("1-2L-#10@100 C/C");r.Cells[11].Add("--");r.Cells[12].Add("--");r.Cells[13].Add("--");return r;}
 static string BeamDxf(){var a=new Analysis{Prefix="B",Floor="TEST LEVEL",BeamCount=3};
  for(int i=0;i<3;i++){var r=new BeamRow();r.Numbers.Add("REF"+(i+1));r.SourceRows.Add(i+1);r.Cells[0].Add("300");r.Cells[1].Add("500");for(int c=2;c<8;c++)r.Cells[c].Add("3-#16");
   for(int c=8;c<11;c++)r.Cells[c].Add("5-2L-#10@100 C/C");r.Cells[11].Add("--");r.Cells[12].Add("--");r.Cells[13].Add("--");r.OriginalShear.Add(new[]{r.Cell(8),r.Cell(9),r.Cell(10)});a.Source.Add(r);}
  var built=Engine.Build(a,new Settings{Prefix="B",Floor="TEST LEVEL",Merge=false});return RevisedSchedule.Serialize(built.Drawing).Replace("\r\n","\n");}
 
 [STAThread]static void Main(){
  var cfg=CalculationStore.All().First(c=>c.Name.Contains("FC28")&&c.Name.Contains("G40/G60"));var row=Row();Func<string,BeamRow> find=m=>m=="B-1"?row:null;
  Func<bool[],List<CutSpan>> line=include=>new List<CutSpan>{new CutSpan{Line="B1",Mark="B-1",Ln=6,LeftSupport=.4,RightSupport=.4,Slab=.1,Include=include[0]},new CutSpan{Line="B1",Mark="B-1",Ln=7,LeftSupport=.4,RightSupport=.4,Slab=.1,Include=include[1]},new CutSpan{Line="B1",Mark="B-1",Ln=6,LeftSupport=.4,RightSupport=.4,Slab=.1,Include=include[2]}};
  var s=new CutSettings{Config=cfg};var r=BeamCutting.Build(line(new[]{true,true,true}),find,s);
  Check(r.Errors.All(e=>!e.Contains("splice zone")),"continuous bars find splice zones: "+string.Join(" | ",r.Errors));
  double total=4*.4+19,cover=.04;
  foreach(var group in new[]{new{Top=true,Dia=20,Count=2},new{Top=false,Dia=16,Count=2}}){var pieces=r.Pieces.Where(p=>p.Location.Contains("continuous")&&p.Diameter==group.Dia&&p.Top==group.Top).ToList();Check(pieces.Count>=2&&pieces.All(p=>p.PerLine==group.Count),(group.Top?"top":"bottom")+" continuous bars are spliced pieces of the smallest count");
   double lap=BeamCutting.Lap(s,group.Dia,group.Top),hook=cfg.Rounded(cfg.Values(group.Dia,false).Cut90)/1000;var laps=r.Splices.Where(c=>c.Diameter==group.Dia).ToList();Check(laps.Count==pieces.Count-1&&laps.All(c=>Math.Abs(c.To-c.From-lap)<1e-9),"laps use the configured lap length");
   Near(pieces.Sum(p=>p.Length),total-2*cover+2*hook+laps.Count*lap,"continuous length = line less cover + 2 hooks + laps");Check(pieces.All(p=>p.Length<=12+1e-9),"no piece longer than the longest bar");
   foreach(var c in laps){Check(c.From>=c.ZoneFrom-1e-9&&c.To<=c.ZoneTo+1e-9,"lap inside its zone");double face=.4+(c.Span==1?0:c.Span==2?6.4:13.8);Check(c.From>=face+1.0-1e-9,"lap starts at least 2h from the left face");}}
  var topExtra=r.Pieces.Where(p=>p.Location.StartsWith("Top extra over support")).ToList();Check(topExtra.Count==2&&topExtra.All(p=>p.PerLine==2),"two interior supports with 2 extra top bars each");Near(topExtra[0].Length,6/3.0+.4+7/3.0,"extra top bar = Ln/3 + support + Ln/3");
  var exterior=r.Pieces.Where(p=>p.Location.StartsWith("Top extra at support")).ToList();Check(exterior.Count==2&&exterior.All(p=>p.PerLine==2&&p.Shape.Contains("hook")),"exterior supports get hooked extra top bars");
  var bottom=r.Pieces.Where(p=>p.Location.StartsWith("Bottom extra, span")).ToList();Check(bottom.Count==3&&bottom.All(p=>p.PerLine==1),"one extra bottom bar per span");Near(bottom[1].Length,7*2/3.0,"bottom extra = 2/3 Ln");
  var est=r.Spans;Check(est.Count==3,"three span estimates");Check(est[0].Ties==15+15+16,"6 m span stirrups: 15 + 15 + 16 sets ("+est[0].Ties+")");
  Near(est.Sum(e=>e.Kg),r.Pieces.Sum(p=>p.Kg),"all steel is shared to the spans");
  Check(r.Splices.Any(c=>!c.Ok&&c.Note.Contains("100 mm")),"lap in the 200 mm stirrup zone fails the hoop spacing over the lap (418.6.3.3)");
  var part=BeamCutting.Build(line(new[]{true,false,true}),find,s);Near(part.IncludedConcrete,.3*.4*12,"unticked span leaves its concrete out");Check(part.IncludedKg<r.IncludedKg&&part.Pieces.Count==r.Pieces.Count,"unticked span: less estimate steel, same cutting list");
  var ordinary=BeamCutting.Build(line(new[]{true,true,true}),find,new CutSettings{Config=cfg,SpecialMomentFrame=false});foreach(var c in ordinary.Splices.Where(c=>c.Diameter==20)){double face=.4+(c.Span==1?0:c.Span==2?6.4:13.8),ln=c.Span==2?7:6;Check(c.From>=face+ln/3-1e-9&&c.To<=face+2*ln/3+1e-9,"ordinary frame: top laps in the middle third");}
  var bad=line(new[]{true,true,true});bad[1].LeftSupport=.5;Check(BeamCutting.Build(bad,find,s).Errors.Any(e=>e.Contains("same support")),"different widths for one support are reported");
  var shortBar=BeamCutting.Build(line(new[]{true,true,true}),find,new CutSettings{Config=cfg,MaxBar=6});Check(shortBar.Pieces.All(p=>p.Length<=6+1e-9||p.Shape=="REVIEW")||shortBar.Errors.Count>0,"6 m bars: pieces fit or the line is flagged");
  Check(BeamCutting.Report(r,s,line(new[]{true,true,true})).Contains("SPLICE ZONE CHECK"),"report");
  AccessCodes.Granted=true;using(var form=new CuttingListForm()){form.AddSources(new[]{new[]{BeamDxf(),"b.dxf"}},true);var grid=(DataGridView)typeof(CuttingListForm).GetField("spans",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(form);
   var marks=((DataGridViewComboBoxColumn)grid.Columns["Mark"]).Items.Cast<object>().Select(o=>o.ToString()).ToList();Check(marks.Count==3,"schedule marks offered");
   foreach(var ln in new[]{"5","6"}){int i=grid.Rows.Add();var g=grid.Rows[i];g.Cells["Line"].Value="G1";g.Cells["Mark"].Value=marks[0];g.Cells["Ln"].Value=ln;g.Cells["Left"].Value="400";g.Cells["Right"].Value="400";g.Cells["Slab"].Value="100";g.Cells["Qty"].Value="2";g.Cells["Include"].Value=ln=="5";}
   form.Calculate();var last=(CutResult)typeof(CuttingListForm).GetField("last",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(form);Check(last!=null&&last.Spans.Count==2&&last.Spans[0].Quantity==2&&!last.Spans[1].Include,"form builds the line with identical lines and the estimate tick");
   string xml=form.ProjectXml();using(var again=new CuttingListForm()){again.OpenProjectXml(xml);Check(again.ProjectXml()==xml,"cutting list file round-trips");}}
  Console.WriteLine("PASS: "+checks+" beam cutting list checks");}
}
