using System;using System.IO;using System.Text;using System.Linq;using System.Text.RegularExpressions;using System.Collections.Generic;using System.Globalization;
namespace BeamScheduleFormatter {
static class RevisedSchedule {
 public static string Plain(string raw){string s=raw.Replace("\\P"," ").Replace("\\U+2205","%%C");s=Regex.Replace(s,@"\\[A-Za-z][^;]*;","");return Regex.Replace(s.Replace("{","").Replace("}",""),@"\s+"," ").Trim();}
 public static string Serialize(Dxf d){var b=new StringBuilder();foreach(var e in d.Records)foreach(var p in e.P){b.AppendLine(p.Code.ToString());b.AppendLine(p.Value);}return b.ToString();}
 public static Analysis Read(string raw,string name){var entities=Dxf.Parse(raw).ModelEntities();var text=entities.Where(e=>e.Type=="MTEXT"||e.Type=="TEXT").ToList();var marks=text.Where(e=>Regex.IsMatch(Plain(e.Get(1)),@"^\d*[A-Z]+-\d{3,}$",RegexOptions.IgnoreCase)).OrderByDescending(e=>e.Num(20)).ToList();
  if(marks.Count==0)throw new Exception("Load the REVISED DXF exported by Beam Formatter (marks such as RD-001), not the original schedule.");
  if(marks.Select(e=>Plain(e.Get(1)).ToUpperInvariant()).Distinct().Count()!=marks.Count)throw new Exception("Duplicate revised beam marks. Load one schedule only.");
  var headers=text.Where(e=>e.Num(20)>marks[0].Num(20)+1).ToList();Func<string,Entity> header=s=>{var found=headers.Where(e=>Plain(e.Get(1)).Equals(s,StringComparison.OrdinalIgnoreCase)).ToList();if(found.Count!=1)throw new Exception("Missing/ambiguous revised schedule header: "+s);return found[0];};
  var bars=headers.Where(e=>new[]{"LEFT","MIDSPAN","RIGHT"}.Contains(Plain(e.Get(1)).ToUpperInvariant())).OrderBy(e=>e.Num(10)).ToList();if(bars.Count!=6)throw new Exception("Expected six reinforcement columns in revised DXF.");
  var diaHeader=headers.SingleOrDefault(e=>new[]{"%%C","Ø","O"}.Contains(Plain(e.Get(1)).ToUpperInvariant()));if(diaHeader==null)throw new Exception("Missing stirrup diameter header.");
  double[] xs=new[]{header("B").Num(10),header("D").Num(10)}.Concat(bars.Select(e=>e.Num(10))).Concat(new[]{header("LEG").Num(10),diaHeader.Num(10),header("SPACING").Num(10),header("SFR").Num(10)}).ToArray();
  var title=headers.FirstOrDefault(e=>Plain(e.Get(1)).StartsWith("SCHEDULE OF BEAMS"));var floor=title==null?null:headers.Where(e=>e.Num(20)>title.Num(20)).OrderBy(e=>e.Num(20)).FirstOrDefault();
  var a=new Analysis{Path=name,Prefix=Plain(marks[0].Get(1)).Split('-')[0],Floor=floor==null?"":Plain(floor.Get(1)),BeamCount=marks.Count};
  var horizontal=entities.Where(e=>e.Type=="LWPOLYLINE").Where(e=>e.P.Where(p=>p.Code==20).Select(p=>p.Value).Distinct().Count()==1&&e.P.Where(p=>p.Code==10).Any()).Where(e=>e.P.Where(p=>p.Code==10).Max(p=>double.Parse(p.Value,CultureInfo.InvariantCulture))-e.P.Where(p=>p.Code==10).Min(p=>double.Parse(p.Value,CultureInfo.InvariantCulture))>xs[11]-xs[0]).Select(e=>e.Num(20)).ToList();
  var refHeader=headers.FirstOrDefault(e=>Plain(e.Get(1))=="ORIGINAL BEAM NUMBERS");
  for(int i=0;i<marks.Count;i++){
   double y=marks[i].Num(20),top=horizontal.Where(v=>v>y+.001).DefaultIfEmpty(y+4).Min(),bottom=horizontal.Where(v=>v<y-.001).DefaultIfEmpty(y-4).Max();
   if(top-bottom>12||top<=bottom)throw new Exception("Cannot locate revised row boundaries for "+Plain(marks[i].Get(1)));
   var r=new BeamRow{Mark=Plain(marks[i].Get(1)).ToUpperInvariant()};r.Numbers.Add(r.Mark);r.SourceRows.Add(i+1);
   var body=text.Where(e=>e.Num(20)<top-.001&&e.Num(20)>bottom+.001).OrderByDescending(e=>e.Num(20)).ToList();
   foreach(var e in body){if(e.Num(10)<xs[0]-1||e.Num(10)>xs[11]+2)continue;string value=Plain(e.Get(1));if(value=="+"||value=="")continue;int col=Enumerable.Range(0,xs.Length).OrderBy(c=>Math.Abs(xs[c]-e.Num(10))).First();r.Cells[col].Add(value);}
   for(int c=0;c<12;c++)if(r.Cells[c].Count==0)throw new Exception("Missing revised schedule cell "+c+" for "+r.Mark);
   r.Leg=r.Cell(8);r.Diameter=r.Cell(9);r.Spacing=r.RevisedSpacing=r.Cell(10);r.Cells[12].Add("-");r.Cells[13].Add(r.Spacing.IndexOf("TO FREE END",StringComparison.OrdinalIgnoreCase)>=0?"CANTILEVERED":"-");
   if(refHeader!=null){r.RevisedReferences=string.Join(" ",body.Where(e=>Math.Abs(e.Num(10)-refHeader.Num(10))<1).Select(e=>Plain(e.Get(1))));if(r.RevisedReferences.Contains("DIAGONAL:"))r.Cells[12][0]="DIAGONAL reinforcement in revised side notes; review separately";}
   a.Source.Add(r);
  }return a;
 }
 public static void AddTies(BeamEstimate result,BeamRow row,string beam,double length,EstimateOptions o,int? countOverride,double? cutOverride){
  var leg=Regex.Match(row.Leg,@"^(\d+)L$");var diameter=Regex.Match(Engine.Normal(row.Diameter).Replace("#","O").Replace("Ø","O"),@"^O(\d+)$");if(!leg.Success||!diameter.Success)throw new Exception("Mixed/unknown stirrup legs or diameters in revised mark "+row.Mark+"; a project-specific tie detail is required.");
  int legs=int.Parse(leg.Groups[1].Value),dia=int.Parse(diameter.Groups[1].Value);if(legs<2||legs>20)throw new Exception("Invalid revised stirrup leg count.");o.GradeFor(dia);
  string pattern=@"^1\s*-\s*\d+mm,\s*(?:(?<n>\d+)\s*-\s*(?<s>\d+)mm,?\s+)?AND REST AT\s+(?<rest>\d+)mm,?\s+(?<end>C/C|TO FREE END)$";
  var m=Regex.Match(row.RevisedSpacing,pattern,RegexOptions.IgnoreCase);double sl=0,sr=0,rest=0;int nl=0,nr=0;
  if(m.Success){rest=double.Parse(m.Groups["rest"].Value)/1000;if(m.Groups["n"].Success){nl=nr=int.Parse(m.Groups["n"].Value);sl=sr=double.Parse(m.Groups["s"].Value)/1000;}if(m.Groups["end"].Value.Equals("TO FREE END",StringComparison.OrdinalIgnoreCase)){nl=nr=0;result.Notes.Add("Cantilever: revised AND REST AT spacing applied toward free end.");}}
  else{m=Regex.Match(row.RevisedSpacing,@"^1\s*-\s*\d+mm,\s*LEFT\s+(?<ln>\d+)\s*-\s*(?<ls>\d+)mm\s*/\s*RIGHT\s+(?<rn>\d+)\s*-\s*(?<rs>\d+)mm,?\s+AND REST AT\s+(?<rest>\d+)mm,?\s+C/C$",RegexOptions.IgnoreCase);if(!m.Success)throw new Exception("Unsupported revised spacing: "+row.RevisedSpacing);nl=int.Parse(m.Groups["ln"].Value);nr=int.Parse(m.Groups["rn"].Value);sl=double.Parse(m.Groups["ls"].Value)/1000;sr=double.Parse(m.Groups["rs"].Value)/1000;rest=double.Parse(m.Groups["rest"].Value)/1000;}
  if(rest<=0||(nl>0&&sl<=0)||(nr>0&&sr<=0))throw new Exception("Zero/invalid revised spacing.");double clear=length-2*o.Offset/1000;if(clear<=0)throw new Exception("Beam is shorter than first/last stirrup offsets.");int sets;
  if(countOverride.HasValue){sets=countOverride.Value;result.Notes.Add("Manual stirrup set-count override per beam.");}
  else if(nl==0&&nr==0)sets=(int)Math.Ceiling(clear/rest-1e-10)+1;
  else{double dense=nl*sl+nr*sr,gap=clear-dense;if(gap>1e-8)sets=2+nl+nr+Math.Max(0,(int)Math.Ceiling(gap/rest-1e-10)-1);else if(Math.Abs(gap)<=1e-8)sets=1+nl+nr;else{double spacing=new[]{sl,sr,rest}.Where(s=>s>0).Min();sets=(int)Math.Ceiling(clear/spacing-1e-10)+1;result.Notes.Add("Revised end zones overlap at entered length: smallest specified spacing used throughout; shared stations counted once.");}}
  double cut=cutOverride??HookGeometry.TieCut(double.Parse(row.Cell(0),CultureInfo.InvariantCulture),double.Parse(row.Cell(1),CultureInfo.InvariantCulture),dia,legs,o);double metres=sets*cut;result.Ties=sets;result.Lines.Add(new EstimateLine{Beam=beam,Part="Stirrups",Diameter=dia,Grade=o.GradeFor(dia),Metres=metres,Kg=metres*EstimatorEngine.UnitMass(dia),Formula=sets+" sets x "+cut.ToString("0.00",CultureInfo.InvariantCulture)+"m/set = "+metres.ToString("0.00",CultureInfo.InvariantCulture)+"m; "+legs+"L"});
  var geometry=cutOverride.HasValue?null:HookGeometry.TieParts(double.Parse(row.Cell(0),CultureInfo.InvariantCulture),double.Parse(row.Cell(1),CultureInfo.InvariantCulture),dia,legs,o);var detail=result.Lines[result.Lines.Count-1];detail.TieSetsPerBeam=sets;detail.MainHoopCut=geometry==null?0:geometry.MainHoop;detail.AdditionalLegCut=geometry==null?0:geometry.AdditionalLeg;detail.AdditionalLegs=legs-2;detail.CutLength=cut;detail.TieOverride=cutOverride.HasValue;if(geometry!=null)detail.Formula+="; MAIN HOOP="+geometry.MainHoop.ToString("0.00")+"m + "+geometry.AdditionalCount+" additional legs x "+geometry.AdditionalLeg.ToString("0.00")+"m = "+cut.ToString("0.00")+"m/set";
  result.Notes.Add("Stirrups follow REVISED DXF spacing: "+row.RevisedSpacing+". End-offset setting="+o.Offset+"mm each. N end ties means N intervals AFTER the first tie; middle stations fill the remaining gap without duplicate boundaries.");
  result.Notes.Add("Main bars use selected zone lengths and each layer's counts; EF side-face bars counted on both faces. Hook/anchorage allowances remain project inputs.");
  result.Notes.Add(cutOverride.HasValue?"Manual total steel cut length per stirrup set.":"Standard shape model: outer hoop (3x90deg,2x135deg hooks) + (legs-2) depth crossties (135deg both ends), centerline bends and seismic tails. Verify against project detail.");
 }
}
}
