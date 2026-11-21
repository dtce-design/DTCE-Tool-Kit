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
  /* the original checks use stirrup end zones of 25% of Ln */var s=new CutSettings{Config=cfg,ShearZones=false};var r=BeamCutting.Build(line(new[]{true,true,true}),find,s);
  Check(r.Errors.All(e=>!e.Contains("splice zone")),"continuous bars find splice zones: "+string.Join(" | ",r.Errors));
  double total=4*.4+19,cover=.04;
  foreach(var group in new[]{new{Top=true,Dia=20,Count=2},new{Top=false,Dia=16,Count=2}}){var pieces=r.Pieces.Where(p=>p.Location.Contains("continuous")&&p.Diameter==group.Dia&&p.Top==group.Top).ToList();Check(pieces.Count>=2&&pieces.All(p=>p.PerLine==group.Count),(group.Top?"top":"bottom")+" continuous bars are spliced pieces of the smallest count");
   double lap=BeamCutting.SpliceLap(s,group.Dia,group.Top),hook=cfg.Rounded(cfg.Values(group.Dia,false).Cut90)/1000;var laps=r.Splices.Where(c=>c.Diameter==group.Dia).ToList();Check(laps.Count==pieces.Count-1&&laps.All(c=>Math.Abs(c.To-c.From-lap)<1e-9),"laps use the configured lap length");
   Near(pieces.Sum(p=>p.Length),total-2*cover+2*hook+laps.Count*lap,"continuous length = line less cover + 2 hooks + laps");Check(pieces.All(p=>p.Length<=12+1e-9),"no piece longer than the longest bar");
   foreach(var c in laps){Check(c.From>=c.ZoneFrom-1e-9&&c.To<=c.ZoneTo+1e-9,"lap inside its zone");double face=.4+(c.Span==1?0:c.Span==2?6.4:13.8);Check(c.From>=face+1.0-1e-9,"lap starts at least 2h from the left face");}}
  /* stirrups are drawn where they are counted: one per counted hoop, in order, none doubled up (owner: "hindi rin maayos yung sa stirrups") */
  {var tg=r.Geometry[0];for(int j=0;j<tg.Ties.Count;j++){var xs=BeamCutting.StirrupPositions(tg,j);var zones=tg.Ties[j];double min=zones.Where(z=>z.Spacing>0).Min(z=>z.Spacing);
   Check(xs.Count==zones.Sum(z=>z.Count),"span "+(j+1)+": one drawn stirrup per counted stirrup ("+xs.Count+" vs "+zones.Sum(z=>z.Count)+")");
   Check(xs.All(q=>q.Key>tg.FaceLeft[j]&&q.Key<tg.FaceRight[j]),"stirrups stay inside the clear span");
   for(int k=1;k<xs.Count;k++)Check(xs[k].Key-xs[k-1].Key>min*.5-1e-9,"span "+(j+1)+": stirrups "+k+" and "+(k+1)+" are not doubled up ("+(xs[k].Key-xs[k-1].Key)+" m)");}}
  var topExtra=r.Pieces.Where(p=>p.Location.StartsWith("Top extra over support")).ToList();Check(topExtra.Count==2&&topExtra.All(p=>p.PerLine==2),"two interior supports with 2 extra top bars each");Near(topExtra[0].Length,6/4.0+.4+7/4.0+2*BeamCutting.Ld(s,20,true),"POW standard: extra top bar = L/4 + support + L/4 + Ld at both cut ends");
  var exterior=r.Pieces.Where(p=>p.Location.StartsWith("Top extra at support")).ToList();Check(exterior.Count==2&&exterior.All(p=>p.PerLine==2&&p.Shape.Contains("hook")),"exterior supports get hooked extra top bars");
  var bottom=r.Pieces.Where(p=>p.Location.StartsWith("Bottom extra, span")).ToList();Check(bottom.Count==3&&bottom.All(p=>p.PerLine==1),"one extra bottom bar per span");Near(bottom[1].Length,7*2/3.0+2*BeamCutting.Ld(s,16,false),"POW standard: bottom extra from L/6 off each face + Ld at both cut ends");
  var none=BeamCutting.Build(line(new[]{true,true,true}),find,new CutSettings{Config=cfg,Allowance=0,ShearZones=false});Near(none.Pieces.First(p=>p.Location.StartsWith("Bottom extra, span 2")).Length,7*2/3.0,"no allowance: 2/3 L only");
  var est=r.Spans;Check(est.Count==3,"three span estimates");Check(est[0].Ties==15+15+16,"6 m span stirrups: 15 + 15 + 16 sets ("+est[0].Ties+")");
  Near(est.Sum(e=>e.Kg),r.Pieces.Sum(p=>p.Kg),"all steel is shared to the spans");
  /* owner (20 November 2026, "again yung bend sa lap zone"): the bar entering a lap is cranked there, A = the lap length */
  foreach(var dia in new[]{20,16}){var cont=r.Pieces.Where(p=>p.Location.Contains("continuous")&&p.Diameter==dia).ToList();bool topBar=cont[0].Top;double lapMm=Math.Ceiling(BeamCutting.SpliceLap(s,dia,topBar)*100-1e-9)*10;
   Check(cont[0].ShapeCode=="HOOK1"&&cont.Last().ShapeCode=="CRANKHOOK"&&cont.Skip(1).Take(cont.Count-2).All(p=>p.ShapeCode=="CRANK"),dia+" mm: first piece hooked, middle cranked, last cranked and hooked");
   Check(cont.Skip(1).All(p=>Math.Abs(p.DimA-lapMm)<1e-6&&p.DimC==dia&&p.DimB==Math.Ceiling(Math.Sqrt(37)*dia)),dia+" mm crank: A = lap "+lapMm+", B = crank, C = db");
   var last=cont.Last();Check(last.DimE>0&&last.DimD>0&&BbsReport.Code(last.ShapeCode)=="K5",dia+" mm last piece: hook leg E, code K5");}
  Check(r.Splices.All(c=>c.Ok)&&r.Pieces.Any(p=>p.Location.StartsWith("Extra hoops over lap")&&p.PerLine>0),"laps in the 200 mm stirrup zone get extra hoops at min(d/4, 100 mm) (418.6.3.3)");
  foreach(var c in r.Splices){double face=.4+(c.Span==1?0:c.Span==2?6.4:13.8),ln=c.Span==2?7:6,mid=face+ln/2,center=(c.From+c.To)/2;bool top=c.Diameter==20;
   Check(top?Math.Abs(center-mid)<=ln/6+1e-9:Math.Abs(center-mid)>=ln/6-1e-9,(top?"top laps about midspan":"bottom laps away from midspan")+" (span "+c.Span+", centre "+center.ToString("0.00")+")");}
  var part=BeamCutting.Build(line(new[]{true,false,true}),find,s);Near(part.IncludedConcrete,.3*.4*12,"unticked span leaves its concrete out");Check(part.IncludedKg<r.IncludedKg,"unticked span: less estimate steel");
  /* owner (20 November 2026, "abang na lang ang matitira", "parang dowel"): the unticked span keeps only dowels from its neighbours */
  {var g2=part.Geometry[0];Check(part.Excluded.Count>0&&!part.Pieces.Any(p=>p.Span==2&&((p.Shape??"").StartsWith("Hoop")||p.Location.StartsWith("Bottom extra"))),"unticked span: its stirrups and extra bars are not listed");
   var dowels=part.Pieces.Where(p=>p.Location.Contains("dowel (abang) into span 2")).ToList();Check(dowels.Count>0,"unticked span: dowels into it");
   Check(dowels.All(p=>{double lap=BeamCutting.SpliceLap(s,p.Diameter,p.Top);bool fromLeft=p.Start<g2.FaceLeft[1];return fromLeft?Math.Abs(p.End-(g2.FaceLeft[1]+lap))<1e-6:Math.Abs(p.Start-(g2.FaceRight[1]-lap))<1e-6;}),"dowels reach one Class B lap past the face");
   Check(!part.Pieces.Any(p=>p.Start>g2.FaceLeft[1]+1e-6&&p.End<g2.FaceRight[1]-1e-6),"nothing else inside the unticked span");Check(g2.Included!=null&&!g2.Included[1],"geometry marks the span left out");}
  var ordinary=BeamCutting.Build(line(new[]{true,true,true}),find,new CutSettings{Config=cfg,SpecialMomentFrame=false,ShearZones=false});foreach(var c in ordinary.Splices.Where(c=>c.Diameter==20)){double face=.4+(c.Span==1?0:c.Span==2?6.4:13.8),ln=c.Span==2?7:6;Check(c.From>=face+ln/3-1e-9&&c.To<=face+2*ln/3+1e-9,"ordinary frame: top laps in the middle third");}
  var bad=line(new[]{true,true,true});bad[1].LeftSupport=.5;Check(BeamCutting.Build(bad,find,s).Errors.Any(e=>e.Contains("same support")),"different widths for one support are reported");
  var shortBar=BeamCutting.Build(line(new[]{true,true,true}),find,new CutSettings{Config=cfg,MaxBar=6});Check(shortBar.Pieces.All(p=>p.Length<=6+1e-9||p.Shape=="REVIEW")||shortBar.Errors.Count>0,"6 m bars: pieces fit or the line is flagged");
  Check(BeamCutting.Report(r,s,line(new[]{true,true,true})).Contains("SPLICE ZONE CHECK"),"report");
  var geo=r.Geometry.Single();Check(geo.Line=="B1"&&geo.Ln.Length==3&&Math.Abs(geo.Total-20.6)<1e-9&&geo.Ties.Count==3&&geo.TopZones.Count==3,"model geometry for the interactive view");
  using(var view=new BeamModelView()){view.SetResult(r,"B1");view.Selected=r.Pieces[0].BarMark;Check(view.Line=="B1"&&view.Selected==r.Pieces[0].BarMark,"model view takes the result and a selected bar");}
  AccessCodes.Granted=true;using(var form=new CuttingListForm()){form.AddSources(new[]{new[]{BeamDxf(),"b.dxf"}},true);var grid=(DataGridView)typeof(CuttingListForm).GetField("spans",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(form);
   var marks=((DataGridViewComboBoxColumn)grid.Columns["Mark"]).Items.Cast<object>().Select(o=>o.ToString()).ToList();Check(marks.Count==3,"schedule marks offered");
   foreach(var ln in new[]{"5","6"}){int i=grid.Rows.Add();var g=grid.Rows[i];g.Cells["Line"].Value="G1";g.Cells["Mark"].Value=marks[0];g.Cells["Ln"].Value=ln;g.Cells["Left"].Value="400";g.Cells["Right"].Value="400";g.Cells["Slab"].Value="100";g.Cells["Qty"].Value="2";g.Cells["Include"].Value=ln=="5";}
   form.Calculate();var last=(CutResult)typeof(CuttingListForm).GetField("last",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(form);Check(last!=null&&last.Spans.Count==2&&last.Spans[0].Quantity==2&&!last.Spans[1].Include,"form builds the line with identical lines and the estimate tick");
   /* editing the cutting list (owner: "Gawin interactive yung sa cutting list dapat minomodify din") */
   var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;Func<CutResult> current=()=>(CutResult)typeof(CuttingListForm).GetField("last",flags).GetValue(form);var target=current().Pieces.First(p=>p.Location.StartsWith("Bottom continuous"));double kgBefore=current().Pieces.Sum(p=>p.Kg);
   form.ApplyEdit(target,5,"5.5");form.ApplyEdit(target,6,"4");form.Calculate();var edited=current().Pieces.First(p=>p.BarMark==target.BarMark);Check(edited.Edited&&Math.Abs(edited.Length-5.5)<1e-9&&edited.PerLine==4&&edited.CalculatedLength.HasValue,"edited cut length and pieces are kept and flagged");
   Check(Math.Abs(current().Spans.Sum(e=>e.Kg)-current().Pieces.Sum(p=>p.Kg))<1e-6&&Math.Abs(current().Pieces.Sum(p=>p.Kg)-kgBefore)>1e-6,"edits flow into the estimate");Check(current().Purchases.Any(p=>Math.Abs(p.Cut-5.5)<1e-9),"edits flow into the commercial bars");
   form.ApplyEdit(edited,5,edited.CalculatedLength.Value.ToString("R",System.Globalization.CultureInfo.InvariantCulture));form.ApplyEdit(edited,6,edited.CalculatedPerLine.Value.ToString());form.Calculate();Check(!current().Pieces.First(p=>p.BarMark==target.BarMark).Edited,"typing the calculated values back clears the edit");
   var overrides=(System.Collections.Generic.Dictionary<string,CutOverride>)typeof(CuttingListForm).GetField("overrides",flags).GetValue(form);var stirrup=current().Pieces.First(p=>p.ShapeCode=="HOOP");overrides[stirrup.Id]=new CutOverride{Deleted=true};var addedList=(System.Collections.Generic.List<CutPiece>)typeof(CuttingListForm).GetField("added",flags).GetValue(form);addedList.Add(new CutPiece{BarMark="G1-A1",Line="G1",Span=1,Diameter=12,Length=2.5,PerLine=3,Location="Opening trimmer",Shape="Straight",Manual=true});form.Calculate();
   Check(current().Deleted.Any(p=>p.BarMark==stirrup.BarMark)&&current().Pieces.All(p=>p.BarMark!=stirrup.BarMark),"a removed bar leaves the list and is reported");var extra=current().Pieces.First(p=>p.BarMark=="G1-A1");Check(extra.Manual&&extra.Pieces==6&&current().Spans[0].KgByDiameter.ContainsKey(12),"an added bar counts in its span (identical lines included)");
   /* bar bending schedule */
   var hoop=current().Pieces.First(p=>p.ShapeCode=="HOOP");Check(hoop.DimA==300-2*40&&hoop.DimB==500-2*40&&hoop.DimC>0,"hoop outside dimensions b-2c x h-2c and hook tail");var hooked=current().Pieces.First(p=>p.ShapeCode=="HOOK1");Check(hooked.DimB>0&&hooked.DimA>hooked.DimB,"L-bar straight and hook leg");
   string bbs=form.BbsText();Check(bbs.StartsWith("BAR BENDING SCHEDULE")&&bbs.Contains("G1-A1")&&bbs.Contains("Signature: ____")&&bbs.Contains("the program does not approve it")&&bbs.Contains("Opening trimmer")==false,"BBS lists the bars with blank signature lines");
   string xml=form.ProjectXml();Check(xml.Contains("<Edit ")&&xml.Contains("<AddedBar "),"edits are saved");using(var again=new CuttingListForm()){again.OpenProjectXml(xml);Check(again.ProjectXml()==xml,"cutting list file round-trips");var a2=(CutResult)typeof(CuttingListForm).GetField("last",flags).GetValue(again);Check(a2.Pieces.Any(p=>p.BarMark=="G1-A1")&&a2.Deleted.Count==1,"edits come back when the file is opened");}}
  /* Stage 2: cutting patterns, SMF detailing checks, printed BBS */
  {var s2=new CutSettings{Config=cfg};var r2=BeamCutting.Build(line(new[]{true,true,true}),find,s2);
   foreach(var grp in r2.Pieces.Where(p=>p.Length<=12+1e-9).GroupBy(p=>new{p.Grade,p.Diameter})){var pats=r2.Patterns.Where(p=>p.Grade==grp.Key.Grade&&p.Diameter==grp.Key.Diameter).ToList();
    Check(pats.Sum(p=>p.Marks.Values.Sum())==grp.Sum(p=>p.Pieces),"every piece of "+grp.Key.Diameter+" mm is in a cutting pattern");
    Check(pats.All(p=>p.Used<=p.Stock+1e-9&&p.Offcut>=-1e-9&&s2.Stocks.Contains(p.Stock)),"patterns fit commercial bars");
    Check(pats.All(p=>s2.Stocks.Where(x=>x<p.Stock-1e-9).All(x=>x<p.Used-1e-9)),"each bar is bought at the shortest length that holds its cuts");}
   Check(r2.Smf.Any(c=>c.Clause.StartsWith("418.6.4.1")&&!c.Ok),"hoops at 100 mm fail min(d/4, 6db = 96, 150) over 2h");
   Check(r2.Smf.Any(c=>c.Clause=="418.6.3.2"&&c.Item.StartsWith("M+ at left")&&!c.Ok),"2-16 bottom is less than half of 4-20 top at the face");
   Check(r2.Smf.Where(c=>c.Item.Contains("continuous")).All(c=>c.Ok),"two continuous bars top and bottom");
   Check(r2.Smf.Any(c=>c.Clause=="418.8.2.3"&&c.Ok),"400 mm interior column = 20 x 20 mm bar");
   var r3=BeamCutting.Build(line(new[]{true,true,true}),find,new CutSettings{Config=cfg,SpecialMomentFrame=false});Check(r3.Smf.Count==0,"no SMF checks for an ordinary frame");
   using(var doc=BbsReport.Build(r2,s2,new BbsInfo{Number="BBS-01",Revision="0",Status="For review"})){Check(doc.Sheets.Count>=3,"printed BBS has pages");Check(doc.A3&&doc.PageWidth>doc.PageHeight,"BBS prints on A3 landscape");Check(BeamDetailPaper.Cuts(r2.Geometry[0]).Count==9,"sections at the faces and midspan of 3 spans");using(var bmp=new System.Drawing.Bitmap((int)doc.PageWidth,(int)doc.PageHeight))using(var g=System.Drawing.Graphics.FromImage(bmp))for(int pg=0;pg<doc.Sheets.Count;pg++)doc.Draw(g,pg);}}
  /* long lines: 15 spans are drawn in strips; identical sections once */
  {var many=Enumerable.Range(0,15).Select(i=>new CutSpan{Line="B9",Mark="B-1",Ln=i%3==1?7:6,LeftSupport=.4,RightSupport=.4,Slab=.1}).ToList();var s4=new CutSettings{Config=cfg};var r4=BeamCutting.Build(many,find,s4);var lg=r4.Geometry[0];var strips=BeamDetailPaper.Strips(lg);
   Check(strips.Count>=4&&strips[0][0]==0&&strips[strips.Count-1][1]==14&&strips.Zip(strips.Skip(1),(a,b)=>b[0]==a[1]+1).All(x=>x),"15 spans are drawn in consecutive strips");
   Check(strips.All(st=>lg.SupportStart[st[1]+1]+lg.SupportWidth[st[1]+1]-lg.SupportStart[st[0]]<=21.5+1e-9||st[0]==st[1]),"each strip is at most about 21 m");
   var groups=BeamDetailPaper.Groups(r4,lg);Check(groups.Count<BeamDetailPaper.Cuts(lg).Count/3&&groups.Sum(x=>x.Same.Count)==45,"identical sections are drawn once and cover all 45 cuts");
   Check(BeamDetailPaper.Compact(new List<string>{"1A","2A","3A","5A"})=="1A to 3A, 5A","typical list is compact");
   using(var doc=BbsReport.Build(r4,s4,new BbsInfo()))Check(doc.Sheets.Count>3,"long line prints");using(var site=SiteSheets.Build(r4,s4,new BbsInfo{Number="BBS-01"})){Check(site.A3&&site.Sheets.Count>=4,"crew site sheets print on A3");using(var bmp=new System.Drawing.Bitmap((int)site.PageWidth,(int)site.PageHeight))using(var g=System.Drawing.Graphics.FromImage(bmp))for(int pg=0;pg<site.Sheets.Count;pg++)site.Draw(g,pg);}}
  /* two layers: 6-20 top at the supports of a 300 wide beam (4 fit per layer) need Ø25 spacer bars between the layers */
  {var heavy=Row();heavy.Cells[5][0]="6-#20";heavy.Cells[7][0]="6-#20";Func<string,BeamRow> f2=m=>m=="B-1"?heavy:null;var s5=new CutSettings{Config=cfg};var r5=BeamCutting.Build(line(new[]{true,true,true}),f2,s5);
   Check(BeamCutting.PerLayer(300-2*40-2*10,20,20)==4,"4 bars of 20 mm fit in one layer of a 300 wide beam");var sp=r5.Pieces.Where(p=>p.Location.StartsWith("Layer spacer")).ToList();
   Check(sp.Count>0&&sp.All(p=>p.Top&&p.Diameter==25&&Math.Abs(p.Length-.2)<1e-9&&p.PerLine>=2),"Ø25 spacer bars across the inner width where the top bars are in two layers");
   Check(r5.Pieces.Where(p=>p.Location.StartsWith("Layer spacer")).All(p=>!BeamCutting.Longitudinal(p)),"spacers are not main bars");
   Check(BeamCutting.Build(line(new[]{true,true,true}),find,new CutSettings{Config=cfg}).Pieces.All(p=>!p.Location.StartsWith("Layer spacer")),"no spacers when every face fits one layer");
   Check(r5.Smf.Any(c=>c.Note.Contains("2 layers")),"SMF d uses the two layers");}
  /* top bars in the middle only follow the beam estimate curtailment: the central L/2 plus the cut-end allowance */
  {var mid=new BeamRow{Mark="BT"};string[] v={"300","500","2-#16","2-#16","2-#16","2-#20","4-#20","2-#20","1-2L-#10@100 C/C","1-2L-#10@200 C/C","1-2L-#10@100 C/C","--","--","--"};for(int i=0;i<14;i++)mid.Cells[i].Add(v[i]);var ms=new CutSettings{Config=cfg};
   var mr=BeamCutting.Build(new System.Collections.Generic.List<CutSpan>{new CutSpan{Line="M1",Mark="BT",Ln=6,LeftSupport=.4,RightSupport=.4,Slab=.1,Include=true}},m=>mid,ms);var te=mr.Pieces.Where(p=>p.Location.StartsWith("Top extra, midspan")).ToList();
   Check(te.Count==1&&te[0].PerLine==2&&Math.Abs(te[0].Length-(3+2*BeamCutting.EndAllowance(ms,20,true,2)))<1e-9,"top bars in the middle only: central L/2 + cut-end allowance");}
  /* short spans (2h zones narrower than the lap): the top bars stay complete, lapped and cranked at midspan, the lap reported NG */
  {var deep=new BeamRow{Mark="BD"};string[] v={"400","800","5-#12","5-#12","5-#12","5-#16","3-#16","5-#16","1-2L-#10@70 C/C","1-2L-#10@120 C/C","1-2L-#10@70 C/C","--","--","--"};for(int i=0;i<14;i++)deep.Cells[i].Add(v[i]);var ds=new CutSettings{Config=cfg};
   var line5=Enumerable.Range(0,5).Select(i=>new CutSpan{Line="D1",Mark="BD",Ln=4,LeftSupport=.5,RightSupport=.5,Slab=.1,Include=true}).ToList();var dr=BeamCutting.Build(line5,m=>deep,ds);
   var tc=dr.Pieces.Where(p=>p.Top&&p.Location.Contains("continuous")).OrderBy(p=>p.Start).ToList();Check(tc.Count>=2&&Math.Abs(tc.Last().End-(dr.Geometry[0].Total-ds.Cover/1000))<1e-6&&tc.Zip(tc.Skip(1),(x,y)=>y.Start<x.End).All(b=>b),"short spans: the top continuous bars reach the far end with laps");
   Check(tc.Skip(1).All(p=>p.ShapeCode.StartsWith("CRANK"))&&dr.Splices.Any(c=>c.Diameter==16&&!c.Ok&&c.Note.Contains("mechanical")),"short spans: laps cranked, reported NG with the mechanical splice advice");}
  /* stirrup end zones as the beam curtailment shear zones: ductile D x 2, non-ductile L / 3 */
  {var shr=BeamCutting.Build(line(new[]{true,true,true}),find,new CutSettings{Config=cfg});var shrRow=find("B-1");double h2=2*double.Parse(shrRow.Cell(1),System.Globalization.CultureInfo.InvariantCulture)/1000;
   Check(Math.Abs(BeamCutting.EndZoneLength(shrRow,6,new CutSettings{Config=cfg})-Math.Min(h2,3))<1e-9&&Math.Abs(BeamCutting.EndZoneLength(shrRow,6,new CutSettings{Config=cfg,SpecialMomentFrame=false})-2)<1e-9,"end zones: ductile 2h, non-ductile L/3");
   Check(!shr.Errors.Any(e=>e.Contains("end stirrup zone")),"2h end zones need no end-zone warning");}
    /* top 3-16 in spans 1 and 2, 4-20 in span 3 (owner, 21 November 2026: "bat walang continuous main bar sa top?"): each run of spans
     gets its own continuous bars, past the support where the diameter changes; layer spacer bars are Grade 40 */
  {Func<string,string,BeamRow> mk=(mark,top)=>{var br=new BeamRow{Mark=mark};br.Cells[0].Add("400");br.Cells[1].Add("800");for(int c=2;c<5;c++)br.Cells[c].Add("3-#12");for(int c=5;c<8;c++)br.Cells[c].Add(top);br.Cells[8].Add("1-2L-#10@100 C/C");br.Cells[9].Add("1-2L-#10@150 C/C");br.Cells[10].Add("1-2L-#10@100 C/C");br.Cells[11].Add("--");br.Cells[12].Add("--");br.Cells[13].Add("--");return br;};
   var r16=mk("B-16","3-#16");var r20=mk("B-20","4-#20");Func<string,BeamRow> f2=m=>m=="B-16"?r16:m=="B-20"?r20:null;
   var mixed=new List<CutSpan>{new CutSpan{Line="M1",Mark="B-16",Ln=6,LeftSupport=.4,RightSupport=.4,Include=true},new CutSpan{Line="M1",Mark="B-16",Ln=6,LeftSupport=.4,RightSupport=.4,Include=true},new CutSpan{Line="M1",Mark="B-20",Ln=6,LeftSupport=.4,RightSupport=.4,Include=true}};
   var rm=BeamCutting.Build(mixed,f2,new CutSettings{Config=cfg});var g16=rm.Pieces.Where(p=>p.Top&&p.Diameter==16&&p.Location.Contains("continuous")).ToList();var g20=rm.Pieces.Where(p=>p.Top&&p.Diameter==20&&p.Location.Contains("continuous")).ToList();
   Check(g16.Count>0&&g16.All(p=>p.PerLine==3)&&g20.Count>0&&g20.All(p=>p.PerLine==4),"continuous top bars for each run of the same diameter: "+string.Join(" | ",rm.Pieces.Where(p=>p.Top).Select(p=>p.Location+" "+p.PerLine+"-"+p.Diameter)));
   var mg=rm.Geometry[0];Check(g16.Min(p=>p.Start)<mg.SupportStart[0]+mg.SupportWidth[0]&&g16.Max(p=>p.End)>mg.FaceLeft[2]+6*.25-1e-6,"the 16 mm run is hooked at the left end and continues past support 3 into span 3 by L/4 + Ld");
   Check(g20.Min(p=>p.Start)<mg.FaceRight[1]-6*.25+1e-6&&g20.Max(p=>p.End)>mg.FaceRight[2],"the 20 mm run starts in span 2 (L/4 + Ld past support 3) and is hooked at the right end");
   Check(!rm.Pieces.Any(p=>p.Top&&p.Location.StartsWith("Top extra")&&p.Diameter==16&&p.Location.Contains("support 3")&&p.PerLine>=3),"no duplicate extras for the run bars");
   var sp=new CutSettings{Config=cfg};var rs=BeamCutting.Build(mixed,f2,sp);Check(rs.Pieces.Where(p=>p.Location.StartsWith("Layer spacer")).All(p=>p.Grade==40),"layer spacer bars are Grade 40");}
Console.WriteLine("PASS: "+checks+" beam cutting list checks");}
}
