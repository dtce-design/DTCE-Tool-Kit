using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Text;using System.Text.RegularExpressions;
namespace BeamScheduleFormatter {
/* Beam cutting list (owner, 19 November 2026: "Upload lang ng schedule, set up na lang ng dimensions span supports etc. lagyan na
   lang ng check if ano yung isasama sa estimate per span. Splice same na sa design sheet ng splices. Icheck yung splicing zones
   as per nscp. Generate cutting list and estimates").
   A beam line is a row of spans between supports. Bars are laid out along the whole line:
   - continuous top / bottom bars = the smallest count of a diameter over every span and zone of the line; they run from the
     outer face of the first support to the outer face of the last one (less cover) with 90-degree hooks into the exterior
     supports, and are spliced only inside the allowed splice zones, with the lap length of the selected Splice, Development &
     Hooks configuration (the same values as that design sheet);
   - extra top bars over each support extend a set fraction of the clear span into the neighbouring spans; extra bottom bars
     sit in the middle of the span;
   - stirrups follow the schedule spacing of the left / middle / right zones of each span.
   Splice zones: special moment frames (NSCP 2015 Section 418.6.3.3) - not within a joint and not within 2h of the face of a
   joint, with hoops over the lap at not more than d/4 or 100 mm; ordinary frames - top bars in the middle third of a span and
   bottom bars over the interior supports. */
class CutSpan {public string Line="",Mark="";public double Ln,LeftSupport,RightSupport,Slab;public bool Include=true;public int Quantity=1;}
class CutSettings {
 public LengthConfiguration Config;public double Cover=40,MaxBar=12,EndZone=.25,TopExtension=.25,BottomCut=1.0/6,TieOffset=.05,HookFactor=6;
 /* curtailment standard of the Program of Works beam estimate (owner, 19 November 2026: "Yung curtail kunin yung standard sa pow"):
    extra top bars L/4 past the support face, extra bottom bars from L/6 off each face, plus the POW main piece allowance at cut ends -
    0 none, 1 Ld at both piece ends, 2 one lap per piece, 3 hook Ldh / cut-end Ld */
 public int Allowance=1;
 /* edits made on the cutting list (owner: "Gawin interactive yung sa cutting list dapat minomodify din") */
 public Dictionary<string,CutOverride> Overrides=new Dictionary<string,CutOverride>(StringComparer.OrdinalIgnoreCase);public List<CutPiece> Added=new List<CutPiece>();public bool SpecialMomentFrame=true;
 public List<double> Stocks=new List<double>{6,7.5,9,10.5,12};
}
class CutOverride {public double? Length;public int? PerLine;public bool Deleted;}
class CutPiece {public string Line,BarMark,Location,Shape,ShapeCode="STRAIGHT";public double DimA,DimB,DimC,SecB,SecH;public int Span;public bool Edited,Manual;public double? CalculatedLength;public int? CalculatedPerLine;public string Member{get{return Line+(Span>0?" span "+Span:"");}}public int Diameter,Grade,PerLine,Lines;public double Length,Start,End;public bool Top;public long Pieces{get{return (long)PerLine*Lines;}}public double Kg{get{return Pieces*Length*EstimatorEngine.UnitMass(Diameter);}}}
class CutSplice {public string Line,Bar,Status,Note;public int Diameter,Span;public double From,To,Lap,ZoneFrom,ZoneTo;public bool Ok;}
class CutSpanEstimate {public string Line,Mark;public int Span,Quantity;public bool Include;public double Ln,Concrete,Formwork,Kg;public int Ties;public Dictionary<int,double> KgByDiameter=new Dictionary<int,double>();}
class CutPurchase {public int Grade,Diameter;public double Cut;public long Pieces;public StockChoice Choice;}
/* geometry of one beam line for the interactive model (owner, 19 November 2026: "Gawin interactive yung model na kayang magmodel yung
   cross section ng beam or elevation niya nakikita mo pano yung splicing details niya lahat") */
class CutLineGeometry {public string Line;public double[] SupportStart,SupportWidth,FaceLeft,FaceRight,Width,Depth,Slab,Ln;public double Total,Cover;public List<double[]> TopZones=new List<double[]>(),BottomZones=new List<double[]>();public List<List<BeamCutting.TieZone>> Ties=new List<List<BeamCutting.TieZone>>();public string[] Marks;}
class CutResult {public List<CutLineGeometry> Geometry=new List<CutLineGeometry>();
 public List<CutPiece> Pieces=new List<CutPiece>(),Deleted=new List<CutPiece>();public List<CutSplice> Splices=new List<CutSplice>();public List<CutSpanEstimate> Spans=new List<CutSpanEstimate>();public List<string> Notes=new List<string>(),Errors=new List<string>();public List<CutPurchase> Purchases=new List<CutPurchase>();
 public double IncludedKg{get{return Spans.Where(s=>s.Include).Sum(s=>s.Kg);}}public double IncludedConcrete{get{return Spans.Where(s=>s.Include).Sum(s=>s.Concrete);}}public double IncludedFormwork{get{return Spans.Where(s=>s.Include).Sum(s=>s.Formwork);}}
}
static class BeamCutting {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 internal static string F(double v){return v.ToString("0.000",Inv);}
 sealed class Tie {public int Legs,Dia;public double Spacing;}
 static Tie ParseTie(string raw){if(string.IsNullOrWhiteSpace(raw)||raw.Trim()=="-"||raw.Trim()=="--")return null;var m=Regex.Match(Engine.Normal(raw).Replace("#","O").Replace("Ø","O"),@"^(\d+)-(\d+)L-O(\d+)@(\d+)C/C$");if(!m.Success)throw new Exception("Unsupported stirrup: "+raw);return new Tie{Legs=int.Parse(m.Groups[2].Value),Dia=int.Parse(m.Groups[3].Value),Spacing=double.Parse(m.Groups[4].Value,Inv)/1000};}
 internal sealed class TieZone {public double From,To,Spacing;public int Count,Legs,Dia;public string Name;}
 /* stirrup zones of one span measured from the left face: the left / middle / right cells of a manual schedule, or the
    "1-50mm, n-s mm, REST AT r mm C/C" spacing of a formatted (revised) schedule */
 static List<TieZone> TieZones(BeamRow row,double ln,CutSettings s){var list=new List<TieZone>();
  if(!string.IsNullOrEmpty(row.RevisedSpacing)){var leg=Regex.Match(row.Leg??"",@"^(\d+)L$");var dm=Regex.Match(Engine.Normal(row.Diameter??"").Replace("#","O").Replace("Ø","O"),@"^O(\d+)$");if(!leg.Success||!dm.Success)throw new Exception(row.Mark+": mixed or unknown stirrup legs / diameter; a project tie detail is needed.");int legs=int.Parse(leg.Groups[1].Value),dia=int.Parse(dm.Groups[1].Value);
   var m=Regex.Match(row.RevisedSpacing,@"^1\s*-\s*(?<first>\d+)\s*mm,\s*(?:(?<n>\d+)\s*-\s*(?<s>\d+)mm,?\s+)?(?:AND\s+)?REST\s+AT\s+(?<rest>\d+)\s*mm,?\s+(?<end>C/C|TO FREE END)$",RegexOptions.IgnoreCase);int nl=0,nr=0;double sl=0,sr=0,rest,first;
   if(m.Success){if(m.Groups["end"].Value.Equals("TO FREE END",StringComparison.OrdinalIgnoreCase))throw new Exception(row.Mark+": cantilever spans are not covered by the beam cutting list yet.");first=double.Parse(m.Groups["first"].Value,Inv)/1000;rest=double.Parse(m.Groups["rest"].Value,Inv)/1000;if(m.Groups["n"].Success){nl=nr=int.Parse(m.Groups["n"].Value);sl=sr=double.Parse(m.Groups["s"].Value,Inv)/1000;}}
   else{m=Regex.Match(row.RevisedSpacing,@"^1\s*-\s*(?<first>\d+)\s*mm,\s*LEFT\s+(?<ln>\d+)\s*-\s*(?<ls>\d+)mm\s*/\s*RIGHT\s+(?<rn>\d+)\s*-\s*(?<rs>\d+)mm,?\s+AND REST AT\s+(?<rest>\d+)\s*mm,?\s+C/C$",RegexOptions.IgnoreCase);if(!m.Success)throw new Exception(row.Mark+": unsupported stirrup spacing "+row.RevisedSpacing);first=double.Parse(m.Groups["first"].Value,Inv)/1000;nl=int.Parse(m.Groups["ln"].Value);nr=int.Parse(m.Groups["rn"].Value);sl=double.Parse(m.Groups["ls"].Value,Inv)/1000;sr=double.Parse(m.Groups["rs"].Value,Inv)/1000;rest=double.Parse(m.Groups["rest"].Value,Inv)/1000;}
   if(rest<=0||first<=0)throw new Exception(row.Mark+": invalid stirrup spacing.");double a=first+nl*sl,b=ln-first-nr*sr;if(b<a)throw new Exception(row.Mark+": the end stirrup groups do not fit the "+F(ln)+" m span.");
   list.Add(new TieZone{Name="left",From=0,To=a,Spacing=nl>0?sl:rest,Count=1+nl,Legs=legs,Dia=dia});list.Add(new TieZone{Name="middle",From=a,To=b,Spacing=rest,Count=Math.Max(0,(int)Math.Ceiling((b-a)/rest-1e-10)-1),Legs=legs,Dia=dia});list.Add(new TieZone{Name="right",From=b,To=ln,Spacing=nr>0?sr:rest,Count=1+nr,Legs=legs,Dia=dia});return list;}
  var ties=Enumerable.Range(8,3).Select(c=>ParseTie(row.Cell(c))).ToArray();if(ties.All(t=>t==null))throw new Exception(row.Mark+": no stirrup spacing in the schedule.");if(ties[0]==null)ties[0]=ties[1]??ties[2];if(ties[2]==null)ties[2]=ties[1]??ties[0];if(ties[1]==null)ties[1]=ties[0].Spacing<=ties[2].Spacing?ties[0]:ties[2];
  double off=s.TieOffset;double[] bounds={off,ln*s.EndZone,ln*(1-s.EndZone),ln-off};if(bounds[1]<bounds[0])throw new Exception("The end zone is shorter than the first stirrup offset.");string[] names={"left","middle","right"};
  for(int z=0;z<3;z++){var t=ties[z];list.Add(new TieZone{Name=names[z],From=z==0?0:bounds[z],To=z==2?ln:bounds[z+1],Spacing=t.Spacing,Count=(int)Math.Ceiling(Math.Max(0,bounds[z+1]-bounds[z])/t.Spacing-1e-10)+(z==2?1:0),Legs=t.Legs,Dia=t.Dia});}return list;}
 sealed class Geo {public List<CutSpan> Spans;public double[] SupportStart,SupportWidth,FaceLeft,FaceRight;public double Total;public BeamRow[] Rows;public double[] Width,Depth;}
 static int Count(Dictionary<int,int> bars,int dia){int n;return bars.TryGetValue(dia,out n)?n:0;}
 static Dictionary<int,int> Bars(BeamRow row,int cell){return EstimatorEngine.Bars(row.Cells[cell]);}
 internal static double Lap(CutSettings s,int dia,bool top){return s.Config.Rounded(s.Config.Values(dia,top).Lap)/1000;}
 internal static double Ld(CutSettings s,int dia,bool top){return s.Config.Rounded(s.Config.Values(dia,top).Ld)/1000;}
 static double Hook(CutSettings s,int dia){var v=s.Config.Values(dia,false);return s.Config.Rounded(v.Cut90)/1000;}
 public static CutResult Build(IEnumerable<CutSpan> rows,Func<string,BeamRow> schedule,CutSettings s){
  var result=new CutResult();if(s.Config==null)throw new Exception("Choose a Splice, Development & Hooks configuration.");s.Config.Validate();
  if(s.Stocks==null||s.Stocks.Count==0)throw new Exception("Choose at least one commercial bar length.");if(!s.Stocks.Contains(s.MaxBar))throw new Exception("The longest bar to use must be one of the commercial lengths.");
  if(s.EndZone<=0||s.EndZone>=.5)throw new Exception("End zone must be more than 0% and less than 50% of the span.");if(s.Cover<=0||s.Cover>150)throw new Exception("Cover must be more than 0 and up to 150 mm.");
  var lines=new List<List<CutSpan>>();foreach(var row in rows){if(row==null)continue;if(lines.Count==0||!string.Equals(lines[lines.Count-1][0].Line,row.Line,StringComparison.OrdinalIgnoreCase))lines.Add(new List<CutSpan>());lines[lines.Count-1].Add(row);}
  var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);foreach(var line in lines)if(!seen.Add(line[0].Line))result.Errors.Add("Beam line "+line[0].Line+": its spans must be listed together, one after the other.");
  if(result.Errors.Count>0)return result;
  foreach(var line in lines){try{BuildLine(line,schedule,s,result);}catch(Exception ex){result.Errors.Add("Beam line "+line[0].Line+": "+ex.Message);}}
  foreach(var g in result.Pieces.GroupBy(p=>new{p.Grade,p.Diameter,Cut=Math.Round(p.Length,3)})){long pieces=g.Sum(p=>p.Pieces);var best=CommercialBars.Best(CommercialBars.Choices(g.Key.Cut,pieces,0,s.Stocks));if(best==null)result.Errors.Add("No commercial length fits a "+F(g.Key.Cut)+" m cut ("+g.Key.Diameter+" mm).");result.Purchases.Add(new CutPurchase{Grade=g.Key.Grade,Diameter=g.Key.Diameter,Cut=g.Key.Cut,Pieces=pieces,Choice=best});}
  result.Purchases=result.Purchases.OrderBy(p=>p.Grade).ThenBy(p=>p.Diameter).ThenByDescending(p=>p.Cut).ToList();
  result.Notes.Add("Lap lengths: "+s.Config.Name+" (Splice, Development & Hooks), "+(s.Config.Input.Simplified?"simplified method":"general bond-ratio method")+"; top-bar factor applied to top bars.");
  result.Notes.Add(s.SpecialMomentFrame?"Splice zones per NSCP 2015 418.6.3.3 (special moment frame): no lap within a joint or within 2h of a joint face; hoops over the lap at not more than d/4 or 100 mm.":"Ordinary frame: top bars spliced in the middle third of a span, bottom bars over interior supports. Confirm with the structural details.");
  result.Notes.Add("Exterior ends: 90-degree standard hooks (cut length from the configuration) at the far face of the exterior support less cover; the straight length in the support is compared with Ldh.");
  result.Notes.Add("Extra top bars extend "+(s.TopExtension*100).ToString("0.#",Inv)+"% of the clear span past each support face; extra bottom bars start "+(s.BottomCut*100).ToString("0.#",Inv)+"% of the clear span from each face. Confirm cut-off points against the structural drawings.");
  result.Notes.Add("Estimate per span: steel of bars that cross several spans is shared by length; concrete and forms use the beam below the slab (B x (D - slab)) over the clear span. Unticked spans are left out of the estimate only; the cutting list still covers the whole line.");
  return result;
 }
 static void BuildLine(List<CutSpan> spans,Func<string,BeamRow> schedule,CutSettings s,CutResult result){
  string id=spans[0].Line;int lineQty=spans[0].Quantity;if(lineQty<1||lineQty>10000)throw new Exception("Line quantity must be 1 to 10000.");
  int n=spans.Count;var g=new Geo{Spans=spans,SupportStart=new double[n+1],SupportWidth=new double[n+1],FaceLeft=new double[n],FaceRight=new double[n],Rows=new BeamRow[n],Width=new double[n],Depth=new double[n]};
  for(int j=0;j<n;j++){var sp=spans[j];if(sp.Ln<=0||sp.Ln>50)throw new Exception("Span "+(j+1)+": clear span must be more than 0 and up to 50 m.");if(sp.LeftSupport<=0||sp.RightSupport<=0)throw new Exception("Span "+(j+1)+": enter both support widths.");
   if(j>0&&Math.Abs(spans[j-1].RightSupport-sp.LeftSupport)>1e-6)throw new Exception("Span "+j+" right support ("+(spans[j-1].RightSupport*1000).ToString("0",Inv)+" mm) and span "+(j+1)+" left support ("+(sp.LeftSupport*1000).ToString("0",Inv)+" mm) are the same support; enter the same width.");
   var row=schedule(sp.Mark);if(row==null)throw new Exception("Span "+(j+1)+": beam mark "+sp.Mark+" is not in the loaded schedule.");g.Rows[j]=row;
   double b,d;if(!double.TryParse(row.Cell(0),NumberStyles.Float,Inv,out b)||!double.TryParse(row.Cell(1),NumberStyles.Float,Inv,out d)||b<=0||d<=0)throw new Exception(sp.Mark+": beam size must be numeric millimetres.");g.Width[j]=b/1000;g.Depth[j]=d/1000;if(sp.Slab<0||sp.Slab>=d/1000)throw new Exception("Span "+(j+1)+": slab thickness must be less than the beam depth.");}
  double x=0;for(int j=0;j<n;j++){g.SupportWidth[j]=spans[j].LeftSupport;g.SupportStart[j]=x;x+=spans[j].LeftSupport;g.FaceLeft[j]=x;x+=spans[j].Ln;g.FaceRight[j]=x;}g.SupportWidth[n]=spans[n-1].RightSupport;g.SupportStart[n]=x;g.Total=x+spans[n-1].RightSupport;
  double cover=s.Cover/1000;var estimates=new CutSpanEstimate[n];for(int j=0;j<n;j++){var sp=spans[j];double depthBelow=g.Depth[j]-sp.Slab;estimates[j]=new CutSpanEstimate{Line=id,Mark=sp.Mark,Span=j+1,Include=sp.Include,Ln=sp.Ln,Quantity=lineQty,Concrete=g.Width[j]*depthBelow*sp.Ln*lineQty,Formwork=(g.Width[j]+2*depthBelow)*sp.Ln*lineQty};}
  int mark=0;Func<string> nextMark=()=>{mark++;return id+"-"+mark.ToString(Inv);};
  Action<CutPiece> add=p=>{p.Line=id;p.Lines=lineQty;p.Grade=GradeOf(s.Config,p.Diameter);
   /* the span a piece belongs to (0 = runs over several spans) */
   if(p.Span==0)for(int j=0;j<n;j++){double from=j==0?0:g.SupportStart[j]+g.SupportWidth[j]/2,to=j==n-1?g.Total:g.SupportStart[j+1]+g.SupportWidth[j+1]/2;if(p.Start>=from-1e-9&&p.End<=to+1e-9){p.Span=j+1;break;}}
   if(p.SecB<=0&&p.Span>0){p.SecB=g.Width[p.Span-1]*1000;p.SecH=g.Depth[p.Span-1]*1000;}
   CutOverride edit;if(!p.Manual&&s.Overrides.TryGetValue(p.BarMark,out edit)){if(edit.Deleted){result.Deleted.Add(p);return;}if(edit.Length.HasValue&&Math.Abs(edit.Length.Value-p.Length)>1e-9){p.CalculatedLength=p.Length;p.Length=edit.Length.Value;p.Edited=true;}if(edit.PerLine.HasValue&&edit.PerLine.Value!=p.PerLine){p.CalculatedPerLine=p.PerLine;p.PerLine=edit.PerLine.Value;p.Edited=true;}}
   if(p.Length<=0||p.PerLine<0)throw new Exception(p.BarMark+": cut length must be more than 0 and pieces 0 or more.");
   Shape(p,s);if(p.Length>s.MaxBar+1e-9)result.Errors.Add("Beam line "+id+" "+p.Location+": "+F(p.Length)+" m piece is longer than the "+s.MaxBar.ToString("0.##",Inv)+" m bar.");result.Pieces.Add(p);
   /* steel shared to spans by length; a span region runs from the middle of its left support to the middle of its right support */
   double kg=p.Kg,len=Math.Max(1e-9,p.End-p.Start);for(int j=0;j<n;j++){double from=j==0?0:g.SupportStart[j]+g.SupportWidth[j]/2,to=j==n-1?g.Total:g.SupportStart[j+1]+g.SupportWidth[j+1]/2;double overlap=Math.Max(0,Math.Min(to,p.End)-Math.Max(from,p.Start));if(overlap<=0)continue;double share=kg*overlap/len;estimates[j].Kg+=share;double v;estimates[j].KgByDiameter[p.Diameter]=(estimates[j].KgByDiameter.TryGetValue(p.Diameter,out v)?v:0)+share;}};
  for(int face=0;face<2;face++){bool top=face==1;int first=top?5:2;string name=top?"Top":"Bottom";
   var counts=new Dictionary<int,int>[n,3];for(int j=0;j<n;j++)for(int z=0;z<3;z++)counts[j,z]=Bars(g.Rows[j],first+z);
   var dias=Enumerable.Range(0,n).SelectMany(j=>Enumerable.Range(0,3).SelectMany(z=>counts[j,z].Keys)).Distinct().OrderBy(d=>d).ToList();
   foreach(int dia in dias){int cont=int.MaxValue;for(int j=0;j<n;j++)for(int z=0;z<3;z++)cont=Math.Min(cont,Count(counts[j,z],dia));
    if(cont>0)Continuous(g,s,result,add,nextMark(),name,top,dia,cont,cover);
    if(top){for(int i=0;i<=n;i++){int left=i>0?Count(counts[i-1,2],dia)-cont:0,right=i<n?Count(counts[i,0],dia)-cont:0;int through=Math.Min(left,right);double ld=Ld(s,dia,true);
      if(through>0){double end1=EndAllowance(s,dia,true,2);double a=g.FaceLeft[i-1]+spans[i-1].Ln-s.TopExtension*spans[i-1].Ln-end1,b2=g.FaceLeft[i]+s.TopExtension*spans[i].Ln+end1;add(new CutPiece{BarMark=nextMark(),Location="Top extra over support "+(i+1),Diameter=dia,Top=true,PerLine=through,Length=b2-a,Start=a,End=b2,Shape="Straight"});}
      int one=Math.Max(left,right)-through;if(one<=0)continue;bool toLeft=left>right;int j2=toLeft?i-1:i;double ext=s.TopExtension*spans[j2].Ln+EndAllowance(s,dia,true,1);bool exterior=i==0||i==n;double hook=exterior?Hook(s,dia):0,inSupport=exterior?g.SupportWidth[i]-cover:g.SupportWidth[i]+ld;double start=toLeft?g.FaceRight[i-1]-ext:(i==0?cover:g.SupportStart[i]-ld),end=toLeft?(i==n?g.Total-cover:g.SupportStart[i]+g.SupportWidth[i]+ld):g.FaceLeft[i]+ext;
      add(new CutPiece{BarMark=nextMark(),Location="Top extra at support "+(i+1)+(exterior?" (exterior, 90° hook)":" (one side)"),Diameter=dia,Top=true,PerLine=one,Length=ext+inSupport+hook,Start=start,End=end,Shape=exterior?"90° hook one end":"Straight"});}}
    else{for(int j=0;j<n;j++){int mid=Count(counts[j,1],dia)-cont;if(mid>0){double end1=EndAllowance(s,dia,false,2);double a=g.FaceLeft[j]+s.BottomCut*spans[j].Ln-end1,b2=g.FaceRight[j]-s.BottomCut*spans[j].Ln+end1;add(new CutPiece{BarMark=nextMark(),Location="Bottom extra, span "+(j+1),Diameter=dia,PerLine=mid,Length=b2-a,Start=a,End=b2,Shape="Straight"});}
      for(int side=0;side<2;side+=1){int extra=Count(counts[j,side==0?0:2],dia)-cont-Math.Max(0,mid);if(extra<=0)continue;int i=side==0?j:j+1;bool exterior=i==0||i==n;double ld=Ld(s,dia,false),ext=.25*spans[j].Ln+EndAllowance(s,dia,false,1),hook=exterior?Hook(s,dia):0,inSupport=exterior?g.SupportWidth[i]-cover:g.SupportWidth[i]+ld;double start=side==0?(exterior?cover:g.SupportStart[i]-ld):g.FaceRight[j]-ext,end=side==0?g.FaceLeft[j]+ext:(exterior?g.Total-cover:g.SupportStart[i]+g.SupportWidth[i]+ld);
       add(new CutPiece{BarMark=nextMark(),Location="Bottom extra at support "+(i+1)+", span "+(j+1),Diameter=dia,PerLine=extra,Length=ext+inSupport+hook,Start=start,End=end,Shape=exterior?"90° hook one end":"Straight"});}}}}}
  /* side-face bars: each span, developed into both supports */
  for(int j=0;j<n;j++)foreach(var bar in EstimatorEngine.Bars(g.Rows[j].Cells[11],true)){double ld=Ld(s,bar.Key,false);add(new CutPiece{BarMark=nextMark(),Location="Side-face bars, span "+(j+1),Diameter=bar.Key,PerLine=bar.Value,Length=spans[j].Ln+2*ld,Start=g.FaceLeft[j]-ld,End=g.FaceRight[j]+ld,Shape="Straight"});}
  /* stirrups */
  var o=new EstimateOptions{LengthConfig=s.Config,Cover=s.Cover,Hook=s.HookFactor,Fc=s.Config.Input.Fc,GradeSmall=s.Config.GradeSmall,Grade=s.Config.GradeLarge};
  for(int j=0;j<n;j++){var row=g.Rows[j];double ln=spans[j].Ln;List<TieZone> zones;try{zones=TieZones(row,ln,s);}catch(Exception ex){throw new Exception("Span "+(j+1)+": "+ex.Message);}
   foreach(var t in zones){if(t.Count<=0)continue;var shape=HookGeometry.TieParts(g.Width[j]*1000,g.Depth[j]*1000,t.Dia,t.Legs,o);estimates[j].Ties+=t.Count*lineQty;double a=g.FaceLeft[j]+t.From,b2=g.FaceLeft[j]+t.To;
    add(new CutPiece{BarMark=nextMark(),Location="Stirrups, span "+(j+1)+" "+t.Name+" ("+t.Legs+"L @ "+(t.Spacing*1000).ToString("0",Inv)+" mm)",Diameter=t.Dia,PerLine=t.Count,Length=shape.MainHoop,Start=a,End=b2,Shape="Hoop, 135° hooks",SecB=g.Width[j]*1000,SecH=g.Depth[j]*1000,Span=j+1});
    if(t.Legs>2)add(new CutPiece{BarMark=nextMark(),Location="Crossties, span "+(j+1)+" "+t.Name,Diameter=t.Dia,PerLine=t.Count*(t.Legs-2),Length=shape.AdditionalLeg,Start=a,End=b2,Shape="Crosstie 90°/135°",SecB=g.Width[j]*1000,SecH=g.Depth[j]*1000,Span=j+1});}
   if(s.SpecialMomentFrame&&string.IsNullOrEmpty(row.RevisedSpacing)){double need=2*g.Depth[j];if(ln*s.EndZone+1e-9<need)result.Errors.Add("Beam line "+id+" span "+(j+1)+": the end stirrup zone ("+F(ln*s.EndZone)+" m) is shorter than 2h = "+F(need)+" m (NSCP 418.6.4.1).");if(s.TieOffset>.05+1e-9)result.Errors.Add("Beam line "+id+": first hoop is more than 50 mm from the support face (NSCP 418.6.4.4).");}}
  /* bars added by hand on the cutting list: they count in the estimate of their span */
  foreach(var m in s.Added.Where(a=>string.Equals(a.Line,id,StringComparison.OrdinalIgnoreCase))){int j=Math.Max(1,Math.Min(n,m.Span))-1;var copy=new CutPiece{BarMark=m.BarMark,Location=m.Location,Diameter=m.Diameter,PerLine=m.PerLine,Length=m.Length,Shape=m.Shape,ShapeCode=m.ShapeCode,Manual=true,Edited=true,Span=j+1,Start=g.FaceLeft[j],End=g.FaceRight[j]};add(copy);}
  var geo=new CutLineGeometry{Line=id,SupportStart=g.SupportStart,SupportWidth=g.SupportWidth,FaceLeft=g.FaceLeft,FaceRight=g.FaceRight,Width=g.Width,Depth=g.Depth,Slab=spans.Select(q=>q.Slab).ToArray(),Ln=spans.Select(q=>q.Ln).ToArray(),Total=g.Total,Cover=s.Cover/1000,Marks=spans.Select(q=>q.Mark).ToArray()};
  for(int j=0;j<n;j++){try{geo.Ties.Add(TieZones(g.Rows[j],spans[j].Ln,s));}catch(Exception){geo.Ties.Add(new List<TieZone>());}}
  foreach(bool top in new[]{true,false})(top?geo.TopZones:geo.BottomZones).AddRange(SpliceZones(g,s,top));result.Geometry.Add(geo);
  result.Spans.AddRange(estimates);
 }
 static List<double[]> SpliceZones(Geo g,CutSettings s,bool top){int n=g.Spans.Count;var zones=new List<double[]>();for(int j=0;j<n;j++){double ln=g.Spans[j].Ln;
   if(s.SpecialMomentFrame){double h2=2*g.Depth[j];if(ln>2*h2)zones.Add(new[]{g.FaceLeft[j]+h2,g.FaceRight[j]-h2,j});}
   else if(top)zones.Add(new[]{g.FaceLeft[j]+ln/3,g.FaceRight[j]-ln/3,j});
   else if(j<n-1)zones.Add(new[]{g.FaceRight[j]-ln/4,g.FaceLeft[j+1]+g.Spans[j+1].Ln/4,j});}return zones;}
 static void Continuous(Geo g,CutSettings s,CutResult result,Action<CutPiece> add,string barMark,string face,bool top,int dia,int count,double cover){
  int n=g.Spans.Count;double lap=Lap(s,dia,top),hook=Hook(s,dia),xs=cover,xe=g.Total-cover;
  /* development into the exterior supports: straight length available before the hook */
  var ldh=s.Config.Rounded(s.Config.Values(dia,top).Ldh)/1000;foreach(int i in new[]{0,n})if(g.SupportWidth[i]-cover+1e-9<ldh)result.Errors.Add("Beam line "+g.Spans[0].Line+" "+face.ToLowerInvariant()+" "+dia+" mm: support "+(i+1)+" ("+(g.SupportWidth[i]*1000).ToString("0",Inv)+" mm) is narrower than Ldh "+(ldh*1000).ToString("0",Inv)+" mm + cover for the hooked end.");
  var zones=SpliceZones(g,s,top);
  double p=xs;int piece=0;
  while(true){bool firstPiece=piece==0;double startHook=firstPiece?hook:0;
   if(xe-p+startHook+hook<=s.MaxBar+1e-9){add(new CutPiece{BarMark=barMark+(piece>0?"."+(piece+1):""),Location=face+" continuous "+(piece==0?"(one length)":"(last piece)"),Diameter=dia,Top=top,PerLine=count,Length=xe-p+startHook+hook,Start=p,End=xe,Shape=firstPiece?"90° hooks both ends":"90° hook one end"});break;}
   double reach=p+s.MaxBar-startHook;double best=double.NaN;double[] chosen=null;
   foreach(var z in zones){double end=Math.Min(z[1],reach);double start=end-lap;if(start+1e-9<z[0]||start<=p+lap)continue;if(double.IsNaN(best)||start>best){best=start;chosen=z;}}
   if(chosen==null){result.Errors.Add("Beam line "+g.Spans[0].Line+" "+face.ToLowerInvariant()+" "+dia+" mm: no allowed splice zone within reach of a "+s.MaxBar.ToString("0.##",Inv)+" m bar from "+F(p)+" m (lap "+F(lap)+" m). Use a longer bar or review the splice zones.");add(new CutPiece{BarMark=barMark+"."+(piece+1),Location=face+" continuous (NO VALID SPLICE)",Diameter=dia,Top=top,PerLine=count,Length=Math.Min(s.MaxBar,xe-p+startHook),Start=p,End=Math.Min(xe,reach),Shape="REVIEW"});break;}
   int span=(int)chosen[2];add(new CutPiece{BarMark=barMark+"."+(piece+1),Location=face+" continuous piece "+(piece+1)+" (lap in span "+(span+1)+")",Diameter=dia,Top=top,PerLine=count,Length=best+lap-p+startHook,Start=p,End=best+lap,Shape=firstPiece?"90° hook one end":"Straight"});
   var check=new CutSplice{Line=g.Spans[0].Line,Bar=barMark,Diameter=dia,Span=span+1,From=best,To=best+lap,Lap=lap,ZoneFrom=chosen[0],ZoneTo=chosen[1],Ok=true,Status="OK"};
   if(s.SpecialMomentFrame){var row=g.Rows[span];double local=best-g.FaceLeft[span],localEnd=local+lap;var spacings=TieZones(row,g.Spans[span].Ln,s).Where(z=>localEnd>z.From&&local<z.To).Select(z=>z.Spacing).ToList();
    double d=g.Depth[span]-cover-.010-dia/2000.0,limit=Math.Min(d/4,.100),worst=spacings.Count==0?double.NaN:spacings.Max();
    if(double.IsNaN(worst)||worst>limit+1e-9){check.Ok=false;check.Status="NG";check.Note="Hoops over the lap at "+(double.IsNaN(worst)?"?":(worst*1000).ToString("0",Inv))+" mm exceed min(d/4, 100 mm) = "+(limit*1000).ToString("0",Inv)+" mm (418.6.3.3): add hoops at that spacing over the lap.";}else check.Note="Hoops over the lap at "+(worst*1000).ToString("0",Inv)+" mm <= "+(limit*1000).ToString("0",Inv)+" mm.";}
   else check.Note=top?"Top bars spliced in the middle third.":"Bottom bars spliced over the interior support.";
   result.Splices.Add(check);p=best;piece++;if(piece>200)throw new Exception("Too many splices; check the span lengths.");}
 }
 /* POW main piece allowance at the cut ends of a curtailed bar (per end), the same choices as the beam estimate */
 internal static double EndAllowance(CutSettings s,int dia,bool top,int cutEnds){if(cutEnds<=0)return 0;switch(s.Allowance){case 1:case 3:return Ld(s,dia,top);case 2:return Lap(s,dia,top)/cutEnds;default:return 0;}}
 internal static readonly string[] AllowanceNames={"None (detail required)","Ld at both piece ends","1 x lap (config A/B)","Hook Ldh / cut-end Ld"};
 /* bar bending schedule shape and outside dimensions (mm); the cut length stays the authority */
 internal static void Shape(CutPiece p,CutSettings s){string sh=(p.Shape??"").ToUpperInvariant();p.ShapeCode=sh.StartsWith("HOOP")?"HOOP":sh.StartsWith("CROSSTIE")?"CROSSTIE":sh.Contains("HOOKS BOTH")?"HOOK2":sh.Contains("HOOK ONE")?"HOOK1":"STRAIGHT";double db=p.Diameter,L=p.Length*1000;LengthResult v=null;try{v=s.Config.Values(p.Diameter,false);}catch(Exception){}
  double r=v==null?3*db:v.MainInside/2,leg=v==null?12*db+r+db:v.Tail90+r+db,hook=v==null?0:s.Config.Rounded(v.Cut90);double cover=s.Cover;
  switch(p.ShapeCode){case "HOOK1":p.DimB=Math.Round(leg);p.DimA=Math.Round(L-hook+r+db);p.DimC=0;break;case "HOOK2":p.DimA=Math.Round(leg);p.DimC=Math.Round(leg);p.DimB=Math.Round(L-2*hook+2*(r+db));break;
   case "HOOP":p.DimA=Math.Round(p.SecB-2*cover);p.DimB=Math.Round(p.SecH-2*cover);p.DimC=v==null?Math.Max(6*db,75):Math.Round(v.Tail135);break;case "CROSSTIE":p.DimA=Math.Round(p.SecH-2*cover);p.DimB=v==null?Math.Max(6*db,75):Math.Round(v.Tail135);p.DimC=v==null?Math.Max(6*db,75):Math.Round(v.Tail90);break;default:p.DimA=Math.Round(L);p.DimB=p.DimC=0;break;}}
 internal static string ShapeName(string code){switch(code){case "HOOK1":return "L-bar (90° hook one end)";case "HOOK2":return "U-bar (90° hooks both ends)";case "HOOP":return "Closed hoop, 135° hooks";case "CROSSTIE":return "Crosstie 135° / 90°";default:return "Straight";}}
 internal static int GradeOf(LengthConfiguration c,int dia){if(dia<=12)return c.GradeSmall;if(dia>=16)return c.GradeLarge;throw new Exception("13-15 mm bars need a project-specific grade.");}
 public static string Report(CutResult r,CutSettings s,IEnumerable<CutSpan> spans){var b=new StringBuilder();
  b.AppendLine("BEAM CUTTING LIST AND ESTIMATE");b.AppendLine();b.AppendLine("## 01  BASIS");foreach(var note in r.Notes)b.AppendLine(note);b.AppendLine("Cover "+s.Cover.ToString("0",Inv)+" mm; longest bar "+s.MaxBar.ToString("0.##",Inv)+" m; commercial lengths "+CommercialBars.FormatLengths(s.Stocks)+" m; stirrup end zones "+(s.EndZone*100).ToString("0.#",Inv)+"% of the clear span.");b.AppendLine();
  b.AppendLine("## 02  BEAM LINES");b.AppendLine("Line      Span  Mark        Ln (m)   Left supp (mm)  Right supp (mm)  Slab (mm)  Qty  In estimate");foreach(var sp in spans)b.AppendLine(sp.Line.PadRight(10)+"".PadRight(6)+sp.Mark.PadRight(12)+F(sp.Ln).PadRight(9)+(sp.LeftSupport*1000).ToString("0",Inv).PadRight(16)+(sp.RightSupport*1000).ToString("0",Inv).PadRight(17)+(sp.Slab*1000).ToString("0",Inv).PadRight(11)+sp.Quantity.ToString(Inv).PadRight(5)+(sp.Include?"Yes":"No"));b.AppendLine();
  if(r.Errors.Count>0){b.AppendLine("## 03  REVIEW REQUIRED");foreach(var e in r.Errors)b.AppendLine("- "+e);b.AppendLine();}
  b.AppendLine("## 04  SPLICE ZONE CHECK");b.AppendLine("Line      Bar          Dia  Span  Lap from-to (m)      Allowed zone (m)     Lap (m)  Result");foreach(var c in r.Splices)b.AppendLine(c.Line.PadRight(10)+c.Bar.PadRight(13)+c.Diameter.ToString(Inv).PadRight(5)+c.Span.ToString(Inv).PadRight(6)+(F(c.From)+" - "+F(c.To)).PadRight(21)+(F(c.ZoneFrom)+" - "+F(c.ZoneTo)).PadRight(21)+F(c.Lap).PadRight(9)+c.Status+"  "+c.Note);if(r.Splices.Count==0)b.AppendLine("No splices: every continuous bar fits one length.");b.AppendLine();
  b.AppendLine("## 05  CUTTING LIST");b.AppendLine("Bar mark      Location                                            Dia  Grade  Cut (m)   Pcs/line  Lines  Pcs     Shape                 kg");foreach(var p in r.Pieces)b.AppendLine(p.BarMark.PadRight(14)+(p.Location.Length>50?p.Location.Substring(0,50):p.Location).PadRight(52)+p.Diameter.ToString(Inv).PadRight(5)+p.Grade.ToString(Inv).PadRight(7)+F(p.Length).PadRight(10)+p.PerLine.ToString(Inv).PadRight(10)+p.Lines.ToString(Inv).PadRight(7)+p.Pieces.ToString(Inv).PadRight(8)+p.Shape.PadRight(22)+p.Kg.ToString("0.00",Inv));b.AppendLine();
  b.AppendLine("## 06  COMMERCIAL BARS (cutting list, all spans)");b.AppendLine("Grade  Dia  Cut (m)   Pieces  Stock (m)  Pcs/stock  Bars to buy  Offcut (m)");foreach(var p in r.Purchases)b.AppendLine(p.Grade.ToString(Inv).PadRight(7)+p.Diameter.ToString(Inv).PadRight(5)+F(p.Cut).PadRight(10)+p.Pieces.ToString(Inv).PadRight(8)+(p.Choice==null?"-":p.Choice.Length.ToString("0.##",Inv)).PadRight(11)+(p.Choice==null?"-":p.Choice.PiecesPerStock.ToString(Inv)).PadRight(11)+(p.Choice==null?"-":p.Choice.Bars.ToString(Inv)).PadRight(13)+(p.Choice==null?"-":p.Choice.Waste.ToString("0.00",Inv)));
  foreach(var g in r.Purchases.Where(p=>p.Choice!=null).GroupBy(p=>new{p.Grade,p.Diameter,p.Choice.Length}))b.AppendLine("Buy: Grade "+g.Key.Grade+" "+g.Key.Diameter+" mm x "+g.Key.Length.ToString("0.##",Inv)+" m = "+g.Sum(p=>p.Choice.Bars).ToString(Inv)+" bars");b.AppendLine();
  b.AppendLine("## 07  ESTIMATE PER SPAN");b.AppendLine("Line      Span  Mark        In   Ln (m)   Rebar (kg)  Stirrup sets  Concrete (m3)  Forms (m2)");foreach(var e in r.Spans)b.AppendLine(e.Line.PadRight(10)+e.Span.ToString(Inv).PadRight(6)+e.Mark.PadRight(12)+(e.Include?"Yes":"No").PadRight(5)+F(e.Ln).PadRight(9)+e.Kg.ToString("0.00",Inv).PadRight(12)+e.Ties.ToString(Inv).PadRight(14)+e.Concrete.ToString("0.000",Inv).PadRight(15)+e.Formwork.ToString("0.00",Inv));
  b.AppendLine("Included spans: rebar "+r.IncludedKg.ToString("0.00",Inv)+" kg, concrete "+r.IncludedConcrete.ToString("0.000",Inv)+" m3, forms "+r.IncludedFormwork.ToString("0.00",Inv)+" m2.");
  foreach(var d in r.Spans.Where(e=>e.Include).SelectMany(e=>e.KgByDiameter).GroupBy(k=>k.Key).OrderBy(k=>k.Key))b.AppendLine("  "+d.Key+" mm: "+d.Sum(k=>k.Value).ToString("0.00",Inv)+" kg");
  b.AppendLine();b.AppendLine("Generated cutting list for estimating and site checking; it is not an engineer's approval. Confirm splice locations, cut-offs and hooks against the approved structural drawings.");
  return b.ToString();}
}
}
