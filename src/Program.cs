using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Reflection;
using System.Threading.Tasks;

namespace BeamScheduleFormatter {
class Pair { public int Code; public string Value; public Pair(int c,string v){Code=c;Value=v;} }
class Entity {
 public List<Pair> P=new List<Pair>();
 public string Get(int code,string fallback=""){foreach(var p in P)if(p.Code==code)return p.Value;return fallback;}
 public double Num(int c){double n;return double.TryParse(Get(c),NumberStyles.Float,CultureInfo.InvariantCulture,out n)?n:0;}
 public void Set(int c,string v){foreach(var p in P)if(p.Code==c){p.Value=v;return;}P.Add(new Pair(c,v));}
 public void Set(int c,double v){Set(c,v.ToString("R",CultureInfo.InvariantCulture));}
 public string Type {get{return Get(0);}}
 public Entity Copy(bool clean=false){var r=new Entity();foreach(var p in P){if(clean&&p.Code==101)break;r.P.Add(new Pair(p.Code,p.Value));}return r;}
}
class Dxf {
 public List<Entity> Records=new List<Entity>();
 public static Dxf Read(string path){using(var f=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))using(var r=new StreamReader(f,Encoding.UTF8,true))return Parse(r.ReadToEnd());}
 public static Dxf Parse(string text){
  if(text.StartsWith("AutoCAD Binary DXF"))throw new Exception("This is a binary DXF. In AutoCAD, save it as an ASCII DXF first.");
  var lines=text.Replace("\r","").Split('\n');int len=lines.Length;while(len>0&&lines[len-1]=="")len--;
  if(len%2!=0)throw new Exception("The DXF file is incomplete (a group-code pair is missing).");
  var d=new Dxf();Entity e=null;
  for(int i=0;i<len;i+=2){int c;if(!int.TryParse(lines[i].Trim(),out c))throw new Exception("The file is not a valid ASCII DXF. Line "+(i+1)+".");if(c==0||e==null){e=new Entity();d.Records.Add(e);}e.P.Add(new Pair(c,lines[i+1]));}
  if(!d.Records.Any(x=>x.Type=="EOF"))throw new Exception("The DXF is incomplete: no EOF.");
  /* a schedule written with the ACI option reads as the standard wording */foreach(var r in d.Records)if(r.Type=="MTEXT"||r.Type=="TEXT")foreach(var p in r.P)if(p.Code==1&&p.Value.IndexOf("@",StringComparison.Ordinal)>=0)p.Value=AciDetailing.ToStandard(p.Value);return d;
 }
 public List<Entity> ModelEntities(){bool inside=false;var result=new List<Entity>();foreach(var e in Records){if(e.Type=="SECTION"&&e.Get(2)=="ENTITIES"){inside=true;continue;}if(inside&&e.Type=="ENDSEC")break;if(inside&&e.Get(67,"0").Trim()!="1"&&e.Get(410,"Model")=="Model")result.Add(e);}return result;}
 public void Write(string path){using(var w=new StreamWriter(path,false,new UTF8Encoding(false))){foreach(var e in Records)foreach(var p in e.P){w.WriteLine(p.Code.ToString().PadLeft(3));w.WriteLine(p.Value);}}}
}
class BeamRow {
 public string RevisedSpacing,RevisedReferences;
 public List<string>[] Cells=Enumerable.Range(0,14).Select(x=>new List<string>()).ToArray();
 public List<string> Numbers=new List<string>();public List<int> SourceRows=new List<int>();
 public List<string[]> OriginalShear=new List<string[]>();public List<string> Notes=new List<string>();public string Mark,Leg,Diameter,Spacing;
 public string Cell(int n){return Cells[n].Count==0?"--":Cells[n][0];}
 public bool Cantilever {get{return Cell(13).ToUpperInvariant().Contains("CANTILEVER");}}
 public BeamRow Copy(){var r=new BeamRow();r.Cells=Cells.Select(v=>new List<string>(v)).ToArray();r.Numbers.AddRange(Numbers);r.SourceRows.AddRange(SourceRows);r.OriginalShear.AddRange(OriginalShear.Select(s=>(string[])s.Clone()));r.Notes.AddRange(Notes);r.Leg=Leg;r.Diameter=Diameter;r.Spacing=Spacing;return r;}
 public string Signature(){var b=new StringBuilder();foreach(int c in Enumerable.Range(0,8).Concat(new[]{11,12,13})){b.Append("|");foreach(string t in Cells[c])if(t.Trim()!="+")b.Append(Engine.Normal(t)).Append(";");}return b.Append("|").Append(Leg).Append("|").Append(Diameter).Append("|").Append(Spacing).ToString();}
}
class Analysis {
 public List<BeamRow> Source=new List<BeamRow>();public string Prefix,Floor,Path;public int BeamCount;
}
class Settings {public string Prefix="4B",Floor="FOURTH FLOOR";public bool Merge=true;}
class Result {public List<BeamRow> Rows;public Dxf Drawing;public int SourceCount,BeamCount;public List<string> Changes=new List<string>();}
class TemplateLayout {
 public Entity Title,FloorTitle,Mark,Width,Depth,Sfr,Leg,Diameter,Spacing,Lower,Plus;
 public Entity[] Bars;public List<Entity> Rules;public double Left,Right,Top,Bottom,Center,Height,LowerOffset,PlusOffset;
 public TemplateLayout(Dxf d){
  var entities=d.ModelEntities();var text=entities.Where(e=>e.Type=="MTEXT").ToList();
  Title=text.Single(e=>e.Get(1).StartsWith("SCHEDULE OF BEAMS"));
  FloorTitle=text.Where(e=>e.Num(20)>Title.Num(20)&&Regex.IsMatch(e.Get(1),@"FLOOR|ROOF|LEVEL",RegexOptions.IgnoreCase)).OrderBy(e=>e.Num(20)).FirstOrDefault();
  Mark=text.Where(e=>Regex.IsMatch(e.Get(1),@"^\d*[A-Z]+-\d{3,}$")).OrderByDescending(e=>e.Num(20)).First();
  var lines=entities.Where(e=>e.Type=="LINE").ToList();Left=lines.Min(e=>Math.Min(e.Num(10),e.Num(11)));Right=lines.Max(e=>Math.Max(e.Num(10),e.Num(11)));Bottom=lines.Min(e=>Math.Min(e.Num(20),e.Num(21)));
  Rules=entities.Where(e=>e.Type=="LWPOLYLINE"&&e.P.Where(p=>p.Code==20).Select(p=>double.Parse(p.Value,CultureInfo.InvariantCulture)).Distinct().Count()==1&&e.P.Where(p=>p.Code==10).Max(p=>double.Parse(p.Value,CultureInfo.InvariantCulture))-e.P.Where(p=>p.Code==10).Min(p=>double.Parse(p.Value,CultureInfo.InvariantCulture))>Right-Left-.01).OrderByDescending(e=>e.Num(20)).ToList();
  Top=Rules.Where(e=>e.Num(20)>Mark.Num(20)).Min(e=>e.Num(20));Rules=Rules.Where(e=>e.Num(20)<Top-.001).ToList();Height=Top-Rules[0].Num(20);
  var body=text.Where(e=>e.Num(20)<Top&&e.Num(20)>Top-Height).ToList();
  Func<string,Entity> header=name=>text.Single(e=>e.Num(20)>Top&&e.Get(1)==name);
  Func<double,Entity> cell=x=>body.Where(e=>Math.Abs(e.Num(10)-x)<1&&Math.Abs(e.Num(20)-Mark.Num(20))<1&&e.Get(1)!="+").OrderBy(e=>Math.Abs(e.Num(20)-Mark.Num(20))).First();
  Width=cell(header("B").Num(10));Depth=cell(header("D").Num(10));Center=Width.Num(20);Sfr=cell(header("SFR").Num(10));Leg=cell(header("LEG").Num(10));Diameter=cell(header("%%C").Num(10));Spacing=cell(header("SPACING").Num(10));
  Bars=text.Where(e=>e.Num(20)>Top&&new[]{"LEFT","MIDSPAN","RIGHT"}.Contains(e.Get(1))).OrderBy(e=>e.Num(10)).Select(e=>body.Where(b=>Math.Abs(b.Num(10)-e.Num(10))<1&&b.Get(71).Trim()=="8"&&b.Get(1)!="+").OrderByDescending(b=>b.Num(20)).First()).ToArray();if(Bars.Length!=6)throw new Exception("Template needs six upper reinforcement cells.");
  Lower=body.First(e=>e.Get(71).Trim()=="2");Plus=body.First(e=>e.Get(1)=="+");var corresponding=Bars.OrderBy(e=>Math.Abs(e.Num(10)-Lower.Num(10))).First();LowerOffset=Lower.Num(10)-corresponding.Num(10);PlusOffset=Plus.Num(10)-corresponding.Num(10);
 }
 public string LegText(string value){string raw=Leg.Get(1);return raw.StartsWith("{")&&raw.Contains(";")?raw.Substring(0,raw.LastIndexOf(';')+1)+value+"}":value;}
}
class Engine {
 public const string Version="7.1.27";
 public static string Normal(string s){return Regex.Replace(s.ToUpperInvariant().Replace("%%C","Ø").Replace("\\U+2205","Ø").Replace("--","-"),@"\s+","");}
 public static string Format(string s){if(s=="-"||s=="--")return "--";s=s.Replace("#","%%C").Replace("\\U+2205","%%C").Replace("%%c","%%C");s=Regex.Replace(s,@"^(\d+)-","$1 - ");return Regex.Replace(s,@"(%%C\d+)(EF)$","$1 EF");}
 const string BeamToken=@"\d*[A-Z]+\d+";
 static Regex markPattern=new Regex(@"^"+BeamToken+@"(?:,"+BeamToken+@")*$",RegexOptions.IgnoreCase);
 static Regex numberPattern=new Regex(BeamToken,RegexOptions.IgnoreCase);
 public static bool ValidPrefix(string prefix){return !string.IsNullOrWhiteSpace(prefix)&&prefix.Length<=20&&Regex.IsMatch(prefix,@"^\d*[A-Z]{1,12}$",RegexOptions.IgnoreCase);}
 public static Analysis Analyze(string path){
  if(Path.GetExtension(path).ToLowerInvariant()!=".dxf")throw new Exception("A DXF file is needed. In AutoCAD, use Save As > DXF for a DWG file.");
  var d=Dxf.Read(path);var texts=d.ModelEntities().Where(e=>e.Type=="TEXT"||e.Type=="MTEXT").Select(e=>e.Copy(true)).ToList();
  if(texts.Any(e=>e.Get(1).Trim().ToUpperInvariant()=="ORIGINAL BEAM NUMBERS"))throw new Exception("This is a formatted output DXF. Choose the original source schedule so that the original beam references are kept.");
  if(texts.Count==0)throw new Exception("No readable text in model space. Explode the table / block in a COPY of the drawing, then save it as DXF.");
  var marks=texts.Where(e=>markPattern.IsMatch(e.Get(1).Replace(" ",""))).OrderByDescending(e=>e.Num(20)).ThenBy(e=>e.Num(10)).ToList();
  if(marks.Count==0)throw new Exception("The original beam marks (e.g. B1, 2B1, 3B1, 4B1, RD1 or URFB1) were not found. Choose the original schedule DXF, not the formatted output.");
  // Locate the beam-mark column; reinforcement labels must never become beam marks.
  double markX=marks.Min(e=>e.Num(10));marks=marks.Where(e=>Math.Abs(e.Num(10)-markX)<Math.Max(50,Math.Abs(markX)*.001)).ToList();
  if(marks.Count>2000)throw new Exception("Masyadong maraming rows. Maximum 2,000 rows bawat file.");
  double highest=marks.Max(e=>e.Num(20));
  var headers=texts.Where(e=>e.Num(20)>highest).ToList();
  var b=headers.Where(e=>e.Get(1).Trim().ToUpperInvariant()=="B").OrderByDescending(e=>e.Num(20)).FirstOrDefault();
  var dp=headers.Where(e=>e.Get(1).Trim().ToUpperInvariant()=="D").OrderByDescending(e=>e.Num(20)).FirstOrDefault();
  var subs=headers.Where(e=>new[]{"LEFT","MID SPAN","MIDSPAN","RIGHT"}.Contains(e.Get(1).Trim().ToUpperInvariant())).OrderBy(e=>e.Num(10)).ToList();
  if(b==null||dp==null||subs.Count!=9)throw new Exception("The source table does not match. B / D, 3 bottom, 3 top and 3 shear columns (LEFT / MID SPAN / RIGHT) are needed, as in the original 4B schedule.");
  var sfr=headers.FirstOrDefault(e=>e.Get(1).Trim().ToUpperInvariant()=="SFR");var diagonal=headers.FirstOrDefault(e=>e.Get(1).Trim().ToUpperInvariant()=="DIAGONAL");var remarks=headers.FirstOrDefault(e=>e.Get(1).Trim().ToUpperInvariant()=="REMARKS");
  if(sfr==null)throw new Exception("The SFR column was not found in the source table.");
  var centers=new List<double>{b.Num(10),dp.Num(10)};centers.AddRange(subs.Select(e=>e.Num(10)));centers.Add(sfr.Num(10));centers.Add(diagonal!=null?diagonal.Num(10):sfr.Num(10)+(sfr.Num(10)-subs.Last().Num(10)));centers.Add(remarks!=null?remarks.Num(10):centers.Last()+(centers.Last()-sfr.Num(10)));
  if(!centers.SequenceEqual(centers.OrderBy(v=>v)))throw new Exception("Unexpected order of the table columns. Check the source DXF first.");
  double firstGap=marks.Count>1?marks[0].Num(20)-marks[1].Num(20):1120;
  var horizontals=d.ModelEntities().Where(e=>e.Type=="LINE"&&Math.Abs(e.Num(20)-e.Num(21))<.01&&Math.Abs(e.Num(10)-e.Num(11))>(centers[11]-centers[0])*.8&&e.Num(20)>highest).Select(e=>e.Num(20)).OrderBy(v=>v).ToList();
  double topOffset=horizontals.Count>0?horizontals[0]-highest:firstGap*.424107142857;
  if(topOffset<=0||topOffset>firstGap)topOffset=firstGap*.424107142857;
  double markBoundary=(markX+centers[0])/2;
  var a=new Analysis{Path=path};var prefix=Regex.Match(marks[0].Get(1).Replace(" ",""),@"^\d*[A-Z]+",RegexOptions.IgnoreCase).Value.ToUpperInvariant();a.Prefix=prefix;a.Floor=FloorFor(prefix);
  var allBeamNumbers=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  for(int i=0;i<marks.Count;i++){
   double y=marks[i].Num(20),top=y+topOffset,bottom=i+1<marks.Count?marks[i+1].Num(20)+topOffset:double.MinValue;
   var rowTexts=texts.Where(e=>e.Num(20)<top-.001&&e.Num(20)>bottom+.001).OrderByDescending(e=>e.Num(20)).ToList();
   var r=new BeamRow();r.SourceRows.Add(i+1);
   foreach(var t in rowTexts){string text=t.Get(1).Trim();if(t.Num(10)<markBoundary){
     if(text.Length==0)continue;
     if(!Regex.IsMatch(text.Replace(" ",""),@"^,?"+BeamToken+@"(?:,"+BeamToken+@")*,?$",RegexOptions.IgnoreCase))throw new Exception("Unexpected text in the beam-mark column: "+text+". The source should hold one schedule only.");
     foreach(Match m in numberPattern.Matches(text)){if(!allBeamNumbers.Add(m.Value))throw new Exception("Repeated original beam number: "+m.Value+". Check the source drawing.");r.Numbers.Add(m.Value.ToUpperInvariant());}continue;
    }
    int col=0;double best=double.MaxValue;for(int j=0;j<centers.Count;j++){double diff=Math.Abs(t.Num(10)-centers[j]);if(diff<best){best=diff;col=j;}}
    if(text!="+")r.Cells[col].Add(text);
   }
   if(r.Numbers.Count==0)throw new Exception("No beam number in row "+(i+1));
   for(int c=0;c<8;c++)if(r.Cells[c].Count==0)throw new Exception("Missing size / reinforcement in row "+(i+1)+" ("+string.Join(",",r.Numbers)+").");
   if(r.Cells.Skip(2).Take(6).Any(v=>v.Count>3))throw new Exception("More than three reinforcement layers in row "+(i+1)+". This is not supported; check the source before exporting.");
   r.OriginalShear.Add(new[]{r.Cell(8),r.Cell(9),r.Cell(10)});ApplySpacing(r);a.Source.Add(r);
  }
  a.BeamCount=allBeamNumbers.Count;return a;
 }
 public static string FloorFor(string prefix){prefix=(prefix??"").Trim().ToUpperInvariant();if(prefix=="B"||prefix=="GB")return "GROUND FLOOR";if(prefix=="RD")return "ROOF DECK";if(prefix=="RFB"||prefix=="RF")return "ROOF FLOOR";if(prefix=="URFB"||prefix=="URF")return "UPPER ROOF FLOOR";int n;var s=Regex.Match(prefix,@"\d+").Value;if(!int.TryParse(s,out n))return "FLOOR";string[] names={"","FIRST FLOOR","SECOND FLOOR","THIRD FLOOR","FOURTH FLOOR","FIFTH FLOOR","SIXTH FLOOR","SEVENTH FLOOR","EIGHTH FLOOR","NINTH FLOOR","TENTH FLOOR"};return n<names.Length?names[n]:"LEVEL "+n;}
 public static void ApplySpacing(BeamRow r){
  var p=new List<string[]>();for(int c=8;c<=10;c++){string s=r.Cell(c);var m=Regex.Match(s,@"^(\d+)-(\d+)L-(?:#|%%[Cc]|Ø|∅)(\d+)@(\d+)\s*C/C$",RegexOptions.IgnoreCase);if(m.Success)p.Add(new[]{m.Groups[1].Value,m.Groups[2].Value,m.Groups[3].Value,m.Groups[4].Value});else if(s=="-"||s=="--")p.Add(null);else throw new Exception("Stirrup notation cannot be read: "+s+". Inaasahan: 17-4L-#10@100 C/C.");}
  if(p.All(t=>t==null))throw new Exception("No stirrup data in row "+r.SourceRows[0]);
  r.Leg=string.Join("/",p.Where(t=>t!=null).Select(t=>t[1]+"L").Distinct());r.Diameter=string.Join("/",p.Where(t=>t!=null).Select(t=>"%%C"+t[2]).Distinct());
  r.Notes.Clear();string scope=string.Join(", ",r.Numbers)+": ";
  bool bothEnds=p[0]!=null&&p[2]!=null;
  string minimum=bothEnds?Math.Min(int.Parse(p[0][3]),int.Parse(p[2][3])).ToString():null;
  bool endsDiffer=bothEnds&&p[0][3]!=p[2][3];string chosenCount=null;
  if(endsDiffer){int chosen=int.Parse(p[0][3])<int.Parse(p[2][3])?0:2;chosenCount=p[chosen][0];r.Notes.Add(scope+"LEFT "+p[0][0]+"pcs @ "+p[0][3]+"mm / RIGHT "+p[2][0]+"pcs @ "+p[2][3]+"mm; USED "+chosenCount+"pcs @ "+minimum+"mm "+(r.Cantilever?"TO FREE END":"AT BOTH ENDS")+" (SMALLER SPACING + ITS COUNT).");p[0][3]=minimum;p[2][3]=minimum;p[0][0]=chosenCount;p[2][0]=chosenCount;}
  if(p[1]==null)r.Notes.Add(scope+(bothEnds?"MID BLANK IN SOURCE; USED "+minimum+"mm (SMALLER LEFT/RIGHT SPACING) FOR AND REST AT.":"MID BLANK IN SOURCE; END DATA INCOMPLETE."));
  bool all=p.All(t=>t!=null),same=all&&p.Select(t=>t[3]).Distinct().Count()==1;
  if(endsDiffer)r.Spacing="1 - 50mm, "+chosenCount+" - "+minimum+"mm, AND REST AT "+(r.Cantilever||p[1]==null?minimum:p[1][3])+"mm "+(r.Cantilever?"TO FREE END":"C/C");
  else if(r.Cantilever&&bothEnds)r.Spacing="1 - 50mm, AND REST AT "+minimum+"mm TO FREE END";
  else if(same)r.Spacing="1 - 50mm, AND REST AT "+p[0][3]+"mm C/C";
  else if(all&&p[0].SequenceEqual(p[2]))r.Spacing="1 - 50mm, "+p[0][0]+" - "+p[0][3]+"mm, AND REST AT "+p[1][3]+"mm C/C";
  else if(all&&bothEnds)r.Spacing="1 - 50mm, LEFT "+p[0][0]+" - "+p[0][3]+"mm / RIGHT "+p[2][0]+" - "+p[2][3]+"mm, AND REST AT "+p[1][3]+"mm C/C";
  else if(bothEnds&&p[1]==null)r.Spacing="1 - 50mm, AND REST AT "+minimum+"mm C/C";
  else {var bits=new List<string>();string[] labels={"L","MID","R"};for(int i=0;i<3;i++)bits.Add(labels[i]+": "+(p[i]==null?"--":p[i][0]+" - "+p[i][3]+"mm"));r.Spacing=string.Join("; ",bits)+" C/C";}
 }
 public static List<BeamRow> Consolidate(Analysis a,Settings s){
  if(!ValidPrefix(s.Prefix))throw new Exception("The prefix must be like B, GB, 2B, 3B, 4B, RD or URFB, without spaces or symbols.");if(string.IsNullOrWhiteSpace(s.Floor)||s.Floor.Length>60||s.Floor.IndexOfAny(new[]{'{','}','\\','\r','\n'})>=0)throw new Exception("Enter the floor title, e.g. FOURTH FLOOR.");
  var groups=new List<BeamRow>();var seen=new Dictionary<string,BeamRow>();
  foreach(var source in a.Source){var r=source.Copy();ApplySpacing(r);string sig=r.Signature();BeamRow old;
   if(s.Merge&&seen.TryGetValue(sig,out old)){old.Numbers.AddRange(r.Numbers);old.SourceRows.AddRange(r.SourceRows);old.OriginalShear.AddRange(r.OriginalShear);old.Notes.AddRange(r.Notes);}else{groups.Add(r);if(!seen.ContainsKey(sig))seen.Add(sig,r);}
  }
  for(int i=0;i<groups.Count;i++)groups[i].Mark=s.Prefix.ToUpperInvariant()+"-"+(i+1).ToString("000");return groups;
 }
 public static Dxf Standard(){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("StandardTemplate.dxf"))using(var reader=new StreamReader(stream,Encoding.UTF8,true))return Dxf.Parse(reader.ReadToEnd());}
 public static double RowHeight(BeamRow row){return 8;}
 static double LayerCenterY(Entity text){int anchor=(int)text.Num(71);return text.Num(20)+(anchor>=7?text.Num(40)/2:anchor<=3?-text.Num(40)/2:0);}
 public static Result Build(Analysis a,Settings s){
  var groups=Consolidate(a,s);var d=Standard();var entities=d.ModelEntities();var layout=new TemplateLayout(d);
  long next=d.Records.SelectMany(e=>e.P).Where(p=>p.Code==5).Max(p=>long.Parse(p.Value,NumberStyles.HexNumber,CultureInfo.InvariantCulture))+1;
  double bottom=layout.Top-groups.Count*layout.Height;
  var output=new List<Entity>();
  // Keep original header, hatches, borders and shadow entities with their handles.
  // Only populated body text and row separators are regenerated.
  foreach(var original in entities){
   if(original.Type=="MTEXT"&&original.Num(20)<layout.Top)continue;
   if(layout.Rules.Contains(original))continue;
   var e=original.Copy();
   if(original==layout.Title)e.Set(1,Regex.Replace(e.Get(1),@"\([^)]*\)","("+s.Floor.ToUpperInvariant()+")"));
   if(original==layout.FloorTitle){string raw=e.Get(1);int floorTextStart=raw.LastIndexOf(@"\P",StringComparison.Ordinal);floorTextStart=floorTextStart>=0?floorTextStart+2:raw.LastIndexOf(';')+1;e.Set(1,raw.Substring(0,floorTextStart)+s.Floor.Trim().ToUpperInvariant()+(raw.EndsWith("}")?"}":""));}
   bool shadow=e.Type=="HATCH"&&e.P.Any(p=>(p.Code==20||p.Code==21)&&Math.Abs(double.Parse(p.Value,CultureInfo.InvariantCulture)-layout.Bottom)<.00001);
   bool seed=false;foreach(var p in e.P){
    if(p.Code==98){seed=true;continue;}
    if(p.Code!=20&&p.Code!=21)continue;
    double y=double.Parse(p.Value,CultureInfo.InvariantCulture);
    if(shadow&&seed&&p.Code==20)p.Value=((layout.Top+bottom)/2).ToString("R",CultureInfo.InvariantCulture);
    else if(Math.Abs(y-layout.Bottom)<.00001)p.Value=bottom.ToString("R",CultureInfo.InvariantCulture);
    else if(Math.Abs(y-(layout.Bottom-2))<.00001)p.Value=(bottom-2).ToString("R",CultureInfo.InvariantCulture);
   }
   output.Add(e);
  }
  Func<Entity,string,double?,double,Entity> text=(prototype,value,x,y)=>{var e=prototype.Copy(true);e.Set(5,(next++).ToString("X"));e.Set(1,value);if(x.HasValue)e.Set(10,x.Value);e.Set(20,y);return e;};
  Action<Entity,string,double> centered=(prototype,value,center)=>output.Add(text(prototype,value,null,center+prototype.Num(20)-layout.Center));
  for(int i=0;i<groups.Count;i++){
   var row=groups[i];double shift=-i*layout.Height,center=layout.Center+shift;
   centered(layout.Mark,row.Mark,center);centered(layout.Width,row.Cell(0),center);centered(layout.Depth,row.Cell(1),center);
   bool three=row.Cells.Skip(2).Take(6).Any(c=>c.Count==3);
   for(int c=0;c<6;c++){
    var values=row.Cells[c+2];if(values.Count<1||values.Count>3)throw new Exception("Unsupported reinforcement layers: "+row.Mark);
    double x=layout.Bars[c].Num(10),firstY=three?center+1.3:layout.Bars[c].Num(20)+shift;
    var first=text(layout.Bars[c],Format(values[0]),null,firstY);if(three)first.Set(40,1.5);output.Add(first);var previous=first;
    for(int k=1;k<values.Count;k++){
     double y=three?center+(k==1?1.1:-1.5):layout.Lower.Num(20)+shift;
     var lower=text(layout.Lower,Format(values[k]),x+layout.LowerOffset,y);if(three)lower.Set(40,1.5);
     double plusY=(LayerCenterY(previous)+LayerCenterY(lower))/2;
     var plus=text(layout.Plus,"+",x+layout.PlusOffset,plusY);plus.Set(71,"5");plus.Set(40,lower.Num(40));output.Add(lower);output.Add(plus);previous=lower;
    }
   }
   centered(layout.Leg,layout.LegText(row.Leg),center);centered(layout.Diameter,row.Diameter,center);centered(layout.Spacing,row.Spacing,center);centered(layout.Sfr,Format(row.Cell(11)),center);
   string side=string.Join(", ",row.Numbers);var extra=new List<string>();if(row.Cell(12)!="-"&&row.Cell(12)!="--")extra.Add("DIAGONAL: "+Format(row.Cell(12)));if(row.Cell(13)!="-"&&row.Cell(13)!="--")extra.Add("REMARKS: "+row.Cell(13));if(extra.Count>0)side+="\\P"+string.Join("; ",extra);
   var refs=text(layout.Width,side,layout.Right+5,center);refs.Set(41,130);refs.Set(71,"4");output.Add(refs);
   if(row.Notes.Count>0){var note=text(layout.Width,string.Join("\\P",row.Notes.Distinct()),layout.Right+145,center);note.Set(41,190);note.Set(71,"4");output.Add(note);}
   var rule=layout.Rules[Math.Min(i,layout.Rules.Count-1)].Copy(true);rule.Set(5,(next++).ToString("X"));foreach(var p in rule.P)if(p.Code==20)p.Value=(layout.Top-(i+1)*layout.Height).ToString("R",CultureInfo.InvariantCulture);output.Add(rule);
  }
  var heading=entities.Single(e=>e.Type=="MTEXT"&&e.Get(1)=="BEAM MARK");var refsHeader=text(heading,"ORIGINAL BEAM NUMBERS",layout.Right+5,heading.Num(20));refsHeader.Set(41,130);refsHeader.Set(71,"4");output.Add(refsHeader);
  if(groups.Any(r=>r.Notes.Count>0)){var noteHeader=text(heading,"SPACING ADJUSTMENT NOTES",layout.Right+145,heading.Num(20));noteHeader.Set(41,190);noteHeader.Set(71,"4");output.Add(noteHeader);}
  foreach(var item in output.Where(e=>(e.Type=="MTEXT"||e.Type=="TEXT")&&e.Num(20)<layout.Top)){
   item.Set(7,layout.Width.Get(7));item.Set(1,Regex.Replace(item.Get(1),@"\|b1(?=\||;)","|b0",RegexOptions.IgnoreCase));
  }
  int start=d.Records.FindIndex(e=>e.Type=="SECTION"&&e.Get(2)=="ENTITIES"),end=start+1;while(d.Records[end].Type!="ENDSEC")end++;d.Records.RemoveRange(start+1,end-start-1);d.Records.InsertRange(start+1,output);
  foreach(var e in d.Records)for(int i=0;i<e.P.Count;i++){var p=e.P[i];if(p.Code==9&&p.Value=="$HANDSEED")e.P[i+1].Value=next.ToString("X");if(p.Code==9&&(p.Value=="$EXTMIN"||p.Value=="$EXTMAX")){for(int k=i+1;k<e.P.Count&&e.P[k].Code!=9;k++){if(p.Value=="$EXTMIN"&&e.P[k].Code==20)e.P[k].Value=(bottom-2).ToString("R",CultureInfo.InvariantCulture);if(p.Value=="$EXTMAX"&&e.P[k].Code==10)e.P[k].Value=(layout.Right+(groups.Any(r=>r.Notes.Count>0)?340:140)).ToString("R",CultureInfo.InvariantCulture);}}}
  var handles=d.Records.Where(e=>e.Get(5)!="").Select(e=>e.Get(5)).ToList();if(handles.Distinct().Count()!=handles.Count)throw new Exception("DXF validation: duplicate handles.");
  if(groups.Sum(r=>r.Numbers.Count)!=a.BeamCount)throw new Exception("DXF validation: missing original beam references.");
  var result=new Result{Rows=groups,Drawing=d,SourceCount=a.Source.Count,BeamCount=a.BeamCount};foreach(var row in groups.Where(r=>r.SourceRows.Count>1))result.Changes.Add("Rows "+string.Join(", ",row.SourceRows)+" -> "+row.Mark+" ("+string.Join(", ",row.Numbers)+")");return result;
 }

 static string Csv(string s){return "\""+s.Replace("\"","\"\"")+"\"";}
 public static string Mapping(Result r){var b=new StringBuilder();b.AppendLine("New mark,Original source rows,Original beam numbers,Original left stirrups,Original mid stirrups,Original right stirrups,Final spacing,Adjustment notes");foreach(var row in r.Rows)for(int i=0;i<row.SourceRows.Count;i++){var raw=row.OriginalShear[i];b.AppendLine(string.Join(",",new[]{row.Mark,row.SourceRows[i].ToString(),string.Join(", ",row.Numbers),raw[0],raw[1],raw[2],row.Spacing,string.Join("; ",row.Notes)}.Select(Csv)));}return b.ToString();}
 public static void Export(Result r,string path){
  string folder=Path.GetDirectoryName(Path.GetFullPath(path));string temp=Path.Combine(folder,"."+Guid.NewGuid().ToString("N")+".dxf");
  try{r.Drawing.Write(temp);var check=Dxf.Read(temp);int count=check.ModelEntities().Count(e=>e.Type=="MTEXT"&&Regex.IsMatch(e.Get(1),@"^\d*[A-Z]+-\d{3,}$"));if(count!=r.Rows.Count)throw new Exception("The DXF did not pass the export verification.");File.Copy(temp,path,true);}finally{if(File.Exists(temp))File.Delete(temp);}

 }
}

class Preview {
 public static Bitmap Render(Dxf d){
  var es=d.ModelEntities();var edges=es.Where(e=>e.Type=="LINE").ToList();double minY=edges.Min(e=>Math.Min(e.Num(20),e.Num(21)))-5,left=edges.Min(e=>Math.Min(e.Num(10),e.Num(11)))-4;
  double maxY=es.Where(e=>e.Type=="MTEXT").Max(e=>e.Num(20)+e.Num(40)*1.5)+2,right=Math.Max(edges.Max(e=>Math.Max(e.Num(10),e.Num(11)))+5,es.Where(e=>e.Type=="MTEXT").Max(e=>e.Num(10)+(new[]{2,5,8}.Contains((int)e.Num(71))?e.Num(41)/2:e.Num(41)))+4);const double scale=4;
  int width=(int)((right-left)*scale),height=(int)((maxY-minY)*scale);if(height>24000)height=24000;var image=new Bitmap(width,Math.Max(300,height));
  using(var g=Graphics.FromImage(image)){g.Clear(Color.FromArgb(33,40,46));g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
   Func<double,double,PointF> xy=(x,y)=>new PointF((float)((x-left)*scale),(float)((maxY-y)*scale));using(var pen=new Pen(Color.FromArgb(63,139,108),.7f))using(var brush=new SolidBrush(Color.FromArgb(76,159,121))){
    foreach(var e in es){
     if(e.Type=="HATCH"){
      var points=new List<PointF>();bool boundary=false;double hx=0;foreach(var p in e.P){if(p.Code==92){boundary=true;continue;}if(p.Code==97)boundary=false;if(!boundary)continue;if(p.Code==10)hx=double.Parse(p.Value,CultureInfo.InvariantCulture);if(p.Code==20)points.Add(xy(hx,double.Parse(p.Value,CultureInfo.InvariantCulture)));}
      if(points.Count>=3)using(var fill=new SolidBrush(e.Get(8).Contains("80")?Color.FromArgb(51,80,80,80):Color.FromArgb(153,51,51,51)))g.FillPolygon(fill,points.ToArray());continue;
     }
     if(e.Type=="LINE")g.DrawLine(pen,xy(e.Num(10),e.Num(20)),xy(e.Num(11),e.Num(21)));
     else if(e.Type=="LWPOLYLINE"&&e.Get(8)!="Construction Line"){var pts=new List<PointF>();double x=0;foreach(var p in e.P){if(p.Code==10)x=double.Parse(p.Value,CultureInfo.InvariantCulture);if(p.Code==20)pts.Add(xy(x,double.Parse(p.Value,CultureInfo.InvariantCulture)));}if(pts.Count>1)g.DrawLines(pen,pts.ToArray());}
     else if(e.Type=="MTEXT"&&e.Get(8)!="DTCE-DATA"){/* hidden data is not shown */
      string text=e.Get(1).Replace("%%C","Ø").Replace("%%c","Ø").Replace("\\P","\n");text=Regex.Replace(text,@"\\[Hf][^;]+;","").Replace("{","").Replace("}","");float h=(float)(e.Num(40)*scale/0.72),w=(float)(e.Num(41)*scale);int anchor=(int)e.Num(71);var pt=xy(e.Num(10),e.Num(20));
      using(var f=new Font("Arial",h,e.Get(7).Contains("B")||e.Get(1).Contains("|b1|")?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel))using(var fmt=new StringFormat(StringFormat.GenericTypographic)){
       fmt.Alignment=new[]{2,5,8}.Contains(anchor)?StringAlignment.Center:new[]{3,6,9}.Contains(anchor)?StringAlignment.Far:StringAlignment.Near;/* no width: one line as long as it is */if(w<=0)w=g.MeasureString(text,f,100000,fmt).Width+2;var measured=g.MeasureString(text,f,(int)Math.Max(w,10),fmt);float y=pt.Y;if(new[]{4,5,6}.Contains(anchor))y-=measured.Height/2;if(new[]{7,8,9}.Contains(anchor))y-=measured.Height;float x=pt.X;if(fmt.Alignment==StringAlignment.Center)x-=w/2;if(fmt.Alignment==StringAlignment.Far)x-=w;g.DrawString(text,f,brush,new RectangleF(x,y,w,Math.Max(measured.Height+5,10)),fmt);
      }
     }
    }
   }
  }return image;
 }
}

partial class MainForm:Form {
 protected FlowLayoutPanel ActionPanel;
 Color Navy=Color.FromArgb(20,33,51),Accent=Color.FromArgb(0,139,139),Muted=Color.FromArgb(91,107,125);
 TextBox sourceBox,prefixBox,floorBox,detail;Button browse,process,export;CheckBox merge;DataGridView grid;Label summary,status;PictureBox picture;TabControl tabs;Panel previewHost;Analysis analysis;Result result;bool busy;
 public MainForm(){AppBrand.Apply(this);if(!AccessCodes.CanUseApp(1))throw new Exception("Beam Formatter is not included in your current access.");
  Text="Beam Schedule Formatter v"+Engine.Version;Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);Width=1280;Height=820;MinimumSize=new Size(1120,700);StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",9.5f);BackColor=Color.FromArgb(243,246,250);AutoScaleMode=AutoScaleMode.Dpi;AllowDrop=true;
  var header=new Panel{Dock=DockStyle.Top,Height=82,BackColor=Navy,Padding=new Padding(24,12,24,10)};var title=new Label{Text="BEAM SCHEDULE FORMATTER  v"+Engine.Version,Font=new Font("Segoe UI Semibold",18),ForeColor=Color.White,Dock=DockStyle.Top,Height=34};var sub=new Label{Text="Standard Drawing1 template  •  Developed by Engr. Daryl O. Tomaneng",ForeColor=Color.FromArgb(165,190,211),Dock=DockStyle.Bottom,Height=22};header.Controls.Add(title);header.Controls.Add(sub);
  status=new Label{Dock=DockStyle.Bottom,Height=34,Padding=new Padding(18,8,8,0),BackColor=Color.White,ForeColor=Muted,Text="Ready. Load or drop the original beam schedule DXF."};
  var side=new Panel{Dock=DockStyle.Left,Width=246,Padding=new Padding(12),BackColor=Color.White};var flow=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true};side.Controls.Add(flow);
  AddLabel(flow,"01   SOURCE DRAWING",true);AddLabel(flow,"Original beam schedule DXF");sourceBox=new TextBox{Width=194,ReadOnly=true,BackColor=Color.FromArgb(245,248,251),Height=30};flow.Controls.Add(sourceBox);browse=ButtonOf("Choose DXF…",194,false);browse.Click+=(o,e)=>Choose();flow.Controls.Add(browse);
  AddLabel(flow,"\n02   STANDARD TEMPLATE",true);AddLabel(flow,"Schedule of beams  ·  built-in\nArial / Arial Bold\nText: 2.0  ·  Headers: 2.5\nRow height: 8 units\nOriginal layers included",false,96);
  AddLabel(flow,"03   BEAM MARKS",true);AddLabel(flow,"Prefix (B / GB / RD / URFB)");prefixBox=new TextBox{Width=194,Text="4B",CharacterCasing=CharacterCasing.Upper};flow.Controls.Add(prefixBox);AddLabel(flow,"Floor title");floorBox=new TextBox{Width=194,Text="FOURTH FLOOR",CharacterCasing=CharacterCasing.Upper};flow.Controls.Add(floorBox);
  prefixBox.Leave+=(o,e)=>{if(Engine.ValidPrefix(prefixBox.Text))floorBox.Text=Engine.FloorFor(prefixBox.Text);};
  merge=new CheckBox{Text="Merge identical rows",Checked=true,Width=194,Height=34,Margin=new Padding(0,12,0,3)};flow.Controls.Add(merge);
  AddLabel(flow,"Beam marks are excluded from comparison.\nOriginal beam numbers stay beside each group.",false,58);
  var actions=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=112,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(0,4,0,0)};
  ActionPanel=actions;
  process=ButtonOf("Build preview",194,true);process.Click+=async(o,e)=>await BuildAsync();actions.Controls.Add(process);export=ButtonOf("Export revised DXF",194,false);export.Enabled=false;export.Click+=(o,e)=>ExportFile();actions.Controls.Add(export);side.Controls.Add(actions);
  var content=new Panel{Dock=DockStyle.Fill,Padding=new Padding(18)};summary=new Label{Dock=DockStyle.Top,Height=66,Font=new Font("Segoe UI Semibold",12),ForeColor=Navy,Text="Your revised schedule\nChoose a DXF to start.",Padding=new Padding(3,6,0,0)};
  tabs=new NavigationTabs{Dock=DockStyle.Fill};var tableTab=new TabPage("Schedule values"){BackColor=Color.White};var cadTab=new TabPage("CAD layout preview"){BackColor=Color.FromArgb(33,40,46)};
  grid=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,AllowUserToResizeRows=false,RowHeadersVisible=false,SelectionMode=DataGridViewSelectionMode.CellSelect,MultiSelect=false,BackgroundColor=Color.White,BorderStyle=BorderStyle.None,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.None,EnableHeadersVisualStyles=false,ColumnHeadersHeight=42};
  grid.DefaultCellStyle.SelectionBackColor=Color.FromArgb(220,240,239);grid.DefaultCellStyle.SelectionForeColor=Navy;grid.DefaultCellStyle.Padding=new Padding(5);GridAppearance.Apply(grid);grid.ColumnHeadersDefaultCellStyle.BackColor=Navy;grid.ColumnHeadersDefaultCellStyle.ForeColor=Color.White;grid.ColumnHeadersDefaultCellStyle.Font=new Font("Segoe UI",9,FontStyle.Bold);grid.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(247,249,252);grid.RowTemplate.Height=36;
  string[] names={"Mark","B","D","Bottom L","Bottom M","Bottom R","Top L","Top M","Top R","Leg","Ø","Spacing","SFR","Original beam numbers"};int[] widths={85,48,48,100,100,100,100,100,100,50,50,330,90,280};for(int i=0;i<names.Length;i++)grid.Columns.Add(new DataGridViewTextBoxColumn{Name="C"+i,HeaderText=names[i],Width=widths[i],SortMode=DataGridViewColumnSortMode.NotSortable,Frozen=i<3});
  grid.DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleCenter;
  grid.DefaultCellStyle.WrapMode=DataGridViewTriState.False;
  grid.ColumnHeadersDefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleCenter;
  for(int c=0;c<=12;c++){grid.Columns[c].DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleCenter;grid.Columns[c].HeaderCell.Style.Alignment=DataGridViewContentAlignment.MiddleCenter;}
  grid.Columns[13].DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleLeft;grid.Columns[13].HeaderCell.Style.Alignment=DataGridViewContentAlignment.MiddleLeft;
  for(int c=3;c<=8;c++){grid.Columns[c].DefaultCellStyle.WrapMode=DataGridViewTriState.True;grid.Columns[c].MinimumWidth=78;grid.Columns[c].Width=88;}
  grid.Columns[11].DefaultCellStyle.WrapMode=DataGridViewTriState.True;grid.Columns[11].Width=460;grid.Columns[11].MinimumWidth=360;grid.Columns[13].Width=194;
  grid.RowTemplate.Height=56;grid.RowTemplate.MinimumHeight=56;
  grid.AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
  grid.CellPainting+=PaintReinforcement;
  grid.ColumnHeadersHeight=64;grid.Font=new Font("Arial",9.5f);grid.ColumnHeadersDefaultCellStyle.Font=new Font("Arial",9,FontStyle.Bold);
  grid.Scroll+=(o,e)=>grid.Invalidate();grid.ColumnWidthChanged+=(o,e)=>grid.Invalidate();
  detail=new TextBox{Dock=DockStyle.Bottom,Multiline=true,ReadOnly=true,Height=210,BorderStyle=BorderStyle.None,BackColor=Color.FromArgb(237,243,248),Font=new Font("Consolas",9.5f),Text="Select a row to review original stirrups and output notes.",ScrollBars=ScrollBars.Both,WordWrap=false};
  tableTab.Controls.Add(grid);tableTab.Controls.Add(detail);
  grid.SelectionChanged+=(o,e)=>ShowDetails();grid.CurrentCellChanged+=(o,e)=>ShowDetails();Shown+=(o,e)=>ShowDetails();
  previewHost=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.FromArgb(33,40,46)};picture=new PictureBox{SizeMode=PictureBoxSizeMode.AutoSize,Location=new Point(10,10)};previewHost.Controls.Add(picture);cadTab.Controls.Add(previewHost);tabs.TabPages.Add(tableTab);tabs.TabPages.Add(cadTab);content.Controls.Add(tabs);content.Controls.Add(summary);
  Controls.Add(content);Controls.Add(side);Controls.Add(status);Controls.Add(header);
  DragEnter+=(o,e)=>{if(e.Data.GetDataPresent(DataFormats.FileDrop))e.Effect=DragDropEffects.Copy;};DragDrop+=async(o,e)=>{var paths=(string[])e.Data.GetData(DataFormats.FileDrop);if(paths.Length>0&&!busy)await LoadFile(paths[0]);};
  prefixBox.TextChanged+=(o,e)=>InvalidatePreview();floorBox.TextChanged+=(o,e)=>InvalidatePreview();merge.CheckedChanged+=(o,e)=>InvalidatePreview();
  UseSlabFormatterLayout();FormatterChrome.Apply(this);
 }
 void AddLabel(FlowLayoutPanel f,string t,bool strong=false,int height=0){var l=new Label{Text=t.TrimStart('\n'),Width=194,AutoSize=false,Height=height>0?height:strong?26:23,ForeColor=strong?Navy:Muted,Font=new Font("Segoe UI",strong?9.2f:9,strong?FontStyle.Bold:FontStyle.Regular),Margin=new Padding(0,strong?9:2,0,strong?4:2)};f.Controls.Add(l);}
 void PaintReinforcement(object sender,DataGridViewCellPaintingEventArgs e){
  if(e.RowIndex==-1&&e.ColumnIndex>=0){/* owner (22 November 2026): the whole header is navy with white captions, not a light lower row */using(var headBrush=new SolidBrush(Navy))e.Graphics.FillRectangle(headBrush,e.CellBounds);using(var headPen=new Pen(Color.FromArgb(210,220,230)))e.Graphics.DrawRectangle(headPen,e.CellBounds.Left,e.CellBounds.Top,e.CellBounds.Width-1,e.CellBounds.Height-1);var rect=e.CellBounds;bool grouped=e.ColumnIndex>=1&&e.ColumnIndex<=11;if(grouped){rect.Y+=32;rect.Height-=32;int start=e.ColumnIndex<=2?1:e.ColumnIndex<=5?3:e.ColumnIndex<=8?6:9;int end=start==1?2:start+2;string title=start==1?"SIZE":start==3?"BOTTOM REINFORCEMENT":start==6?"TOP REINFORCEMENT":"SHEAR REINFORCEMENTS";int groupLeft=e.CellBounds.Left,groupWidth=0;for(int j=start;j<=end;j++){groupWidth+=grid.Columns[j].Width;if(j<e.ColumnIndex)groupLeft-=grid.Columns[j].Width;}var group=new Rectangle(groupLeft,e.CellBounds.Top,groupWidth,32);var clip=new Rectangle(e.CellBounds.Left,e.CellBounds.Top,e.CellBounds.Width,32);var state=e.Graphics.Save();e.Graphics.SetClip(clip);using(var brush=new SolidBrush(Navy))e.Graphics.FillRectangle(brush,group);using(var pen=new Pen(Color.FromArgb(210,220,230)))e.Graphics.DrawRectangle(pen,group.Left,group.Top,group.Width-1,group.Height-1);using(var f=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})using(var textBrush=new SolidBrush(Color.White))e.Graphics.DrawString(title,e.CellStyle.Font,textBrush,group,f);e.Graphics.Restore(state);}string[] captions={"BEAM MARK","B","D","LEFT","MIDSPAN","RIGHT","LEFT","MIDSPAN","RIGHT","LEG","Ø","SPACING","SFR","ORIGINAL BEAM NUMBERS"};TextRenderer.DrawText(e.Graphics,captions[e.ColumnIndex],e.CellStyle.Font,rect,Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak);e.Handled=true;return;}
  if(e.RowIndex<0||e.ColumnIndex<3||e.ColumnIndex>8||result==null||e.RowIndex>=result.Rows.Count)return;
  e.Paint(e.ClipBounds,DataGridViewPaintParts.Background|DataGridViewPaintParts.SelectionBackground|DataGridViewPaintParts.Border);
  var bars=result.Rows[e.RowIndex].Cells[e.ColumnIndex-1];var color=(e.State&DataGridViewElementStates.Selected)!=0?e.CellStyle.SelectionForeColor:e.CellStyle.ForeColor;
  using(var font=new Font("Arial",9.5f)){
   float h=(float)Math.Ceiling(font.GetHeight(e.Graphics)),gap=2f,step=h+gap;
   int layers=Math.Max(2,result.Rows[e.RowIndex].Cells.Skip(2).Take(6).Max(c=>c.Count));
   float first=e.CellBounds.Top+(e.CellBounds.Height-(layers*h+(layers-1)*gap))/2f;
   var flags=TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding|TextFormatFlags.SingleLine;
   for(int k=0;k<bars.Count;k++){
    var line=new Rectangle(e.CellBounds.Left+2,(int)Math.Round(first+k*step),e.CellBounds.Width-4,(int)h);
    TextRenderer.DrawText(e.Graphics,Engine.Format(bars[k]).Replace("%%C","\u00D8"),font,line,color,flags);
    if(k>0){float x=e.CellBounds.Left+e.CellBounds.Width/2f-(float)(6.592483757475/22*e.CellBounds.Width)-4f;var plus=new Rectangle((int)(x-8),(int)Math.Round(first+(k-.5f)*step),16,(int)h);TextRenderer.DrawText(e.Graphics,"+",font,plus,color,flags);}
   }
  }e.Handled=true;
 }
 void PaintHeaderGroups(object sender,PaintEventArgs e){
  int[][] ranges={new[]{1,2},new[]{3,5},new[]{6,8},new[]{9,11}};string[] titles={"SIZE","BOTTOM REINFORCEMENT","TOP REINFORCEMENT","SHEAR REINFORCEMENTS"};
  int frozenRight=grid.GetCellDisplayRectangle(2,-1,false).Right;
  for(int i=0;i<ranges.Length;i++){var a=grid.GetCellDisplayRectangle(ranges[i][0],-1,false);var b=grid.GetCellDisplayRectangle(ranges[i][1],-1,false);var r=Rectangle.FromLTRB(a.Left,a.Top,b.Right,a.Top+32);var visible=Rectangle.Intersect(r,new Rectangle(i==0?1:frozenRight,0,grid.ClientSize.Width-(i==0?1:frozenRight)-1,grid.ColumnHeadersHeight));if(visible.Width<=0)continue;var state=e.Graphics.Save();e.Graphics.SetClip(visible);using(var brush=new SolidBrush(Navy))e.Graphics.FillRectangle(brush,r);using(var pen=new Pen(Color.FromArgb(210,220,230)))e.Graphics.DrawRectangle(pen,r.Left,r.Top,r.Width-1,r.Height-1);TextRenderer.DrawText(e.Graphics,titles[i],grid.ColumnHeadersDefaultCellStyle.Font,r,Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);e.Graphics.Restore(state);}
 }
 Button ButtonOf(string text,int w,bool primary){return new Button{Text=text,Width=w,Height=40,FlatStyle=FlatStyle.Flat,BackColor=primary?Accent:Color.FromArgb(233,241,246),ForeColor=primary?Color.White:Navy,Cursor=Cursors.Hand,Margin=new Padding(0,5,0,4)};}
 void InvalidatePreview(){if(result!=null){result=null;export.Enabled=false;status.Text="Settings changed. Click Build preview to update.";}}
 async void Choose(){using(var dialog=new OpenFileDialog{Title="Choose original beam schedule DXF",Filter="AutoCAD DXF (*.dxf)|*.dxf",CheckFileExists=true})if(dialog.ShowDialog(this)==DialogResult.OK)await LoadFile(dialog.FileName);}
 void SetBusy(bool value,string message){busy=value;UseWaitCursor=value;browse.Enabled=!value;process.Enabled=!value;export.Enabled=!value&&result!=null;prefixBox.Enabled=!value;floorBox.Enabled=!value;merge.Enabled=!value;status.Text=message;}
 async Task LoadFile(string path){analysis=null;result=null;grid.Rows.Clear();sourceBox.Text="";summary.Text="Loading source DXF…";SetBusy(true,"Reading the source DXF…");try{var a=await Task.Run(()=>Engine.Analyze(path));analysis=a;sourceBox.Text=Path.GetFileName(path);new ToolTip().SetToolTip(sourceBox,path);prefixBox.Text=a.Prefix;floorBox.Text=a.Floor;summary.Text="Loaded DXF: "+Path.GetFileName(path)+"  ·  "+a.Source.Count+" source rows, "+a.BeamCount+" original beam numbers";status.Text="Loaded. Automatic preview…";}catch(Exception ex){MessageBox.Show(this,ex.Message,"Source cannot be read",MessageBoxButtons.OK,MessageBoxIcon.Warning);summary.Text="Loaded DXF: none";status.Text="The file was not loaded. "+ex.Message;}finally{SetBusy(false,status.Text);}if(analysis!=null)await BuildAsync();}
 async Task BuildAsync(){if(analysis==null){MessageBox.Show(this,"Load an original beam schedule DXF first.","Beam preview");return;}var settings=new Settings{Prefix=prefixBox.Text.Trim(),Floor=floorBox.Text.Trim(),Merge=merge.Checked};SetBusy(true,"Arranging the table and comparing the rows…");try{var r=await Task.Run(()=>Engine.Build(analysis,settings));if(AciDetailing.IsAci(detailing))r.Drawing=AciBeamSchedule.Build(r,settings.Floor);/* ACI: the MNL-66 BM-1 beam schedule */var bitmap=await Task.Run(()=>Preview.Render(r.Drawing));result=r;FillGrid();var old=picture.Image;picture.Image=bitmap;if(old!=null)old.Dispose();int merged=r.SourceCount-r.Rows.Count;summary.Text="Loaded DXF: "+Path.GetFileName(sourceBox.Text)+"  ·  "+r.SourceCount+" source rows > "+r.Rows.Count+" output rows, "+r.BeamCount+" beam numbers, "+merged+" duplicate rows merged";status.Text="Preview ready. Review the values or the DXF preview, then Export revised DXF.";}catch(Exception ex){result=null;MessageBox.Show(this,ex.Message,"Preview could not be built",MessageBoxButtons.OK,MessageBoxIcon.Warning);status.Text="Formatting stopped: "+ex.Message;}finally{SetBusy(false,status.Text);}}
 void FillGrid(){grid.Rows.Clear();if(result==null)return;foreach(var r in result.Rows){var vals=new List<object>{r.Mark,r.Cell(0),r.Cell(1)};for(int c=2;c<8;c++)vals.Add(string.Join("\r\n+ ",r.Cells[c].Select(Engine.Format)).Replace("%%C","Ø"));vals.Add(r.Leg);vals.Add(r.Diameter.Replace("%%C","Ø"));vals.Add(r.Spacing);vals.Add(Engine.Format(r.Cell(11)).Replace("%%C","Ø"));vals.Add(string.Join(", ",r.Numbers));grid.Rows.Add(vals.ToArray());}grid.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders);ShowDetails();}
 static string OriginalStirrupDisplay(string raw){
  if(string.IsNullOrWhiteSpace(raw)||raw=="-"||raw=="--")return "-- (blank in source)";
  var match=Regex.Match(raw,@"^(\d+)-(\d+)L-(?:#|%%[Cc]|O|\u00D8)(\d+)@(\d+)\s*C/C$",RegexOptions.IgnoreCase);
  if(match.Success)return match.Groups[1].Value+" pcs | "+match.Groups[2].Value+"L | \u00D8"+match.Groups[3].Value+" @ "+match.Groups[4].Value+" mm C/C";
  return raw.Replace("%%C","\u00D8").Replace("#","\u00D8");
 }
 void ShowDetails(){
  if(detail==null)return;
  if(result==null||grid.CurrentRow==null||grid.CurrentRow.Index>=result.Rows.Count){detail.Text="Select a row to review original stirrups and output notes.";return;}
  var r=result.Rows[grid.CurrentRow.Index];var b=new StringBuilder();
  b.AppendLine(r.Mark+" | Original beams: "+string.Join(", ",r.Numbers));
  b.AppendLine("Source rows: "+string.Join(", ",r.SourceRows)+(r.SourceRows.Count>1?" [MERGED]":""));
  for(int i=0;i<r.SourceRows.Count;i++){
   int rowNumber=r.SourceRows[i];var source=analysis.Source.FirstOrDefault(row=>row.SourceRows.Contains(rowNumber));
   b.AppendLine();b.AppendLine("SOURCE ROW "+rowNumber+" | "+(source==null?string.Join(", ",r.Numbers):string.Join(", ",source.Numbers)));
   b.AppendLine(string.Format("{0,-12}{1,-9}{2,-12}{3,-12}{4}","POSITION","LEG","BAR DIA.","NO. (pcs)","SPACING (mm)"));
   string[] positions={"LEFT","MIDSPAN","RIGHT"};
   for(int c=0;c<3;c++){
    string raw=r.OriginalShear[i][c];var match=Regex.Match(raw,@"^(\d+)-(\d+)L-(?:#|%%[Cc]|O|\u00D8)(\d+)@(\d+)\s*C/C$",RegexOptions.IgnoreCase);
    b.AppendLine(string.Format("{0,-12}{1,-9}{2,-12}{3,-12}{4}",positions[c],match.Success?match.Groups[2].Value+"L":"--",match.Success?"\u00D8"+match.Groups[3].Value:"--",match.Success?match.Groups[1].Value:"--",match.Success?match.Groups[4].Value+" C/C":OriginalStirrupDisplay(raw)));
   }
  }
  b.AppendLine();b.AppendLine("OUTPUT: "+r.Leg+" / "+r.Diameter.Replace("%%C","\u00D8")+" | "+r.Spacing);
  if(r.Notes.Count==0)b.AppendLine("NOTES: No spacing adjustment required.");else foreach(string note in r.Notes)b.AppendLine("NOTE: "+note);
  if(r.Cantilever)b.AppendLine("CANTILEVER: Spacing applies TO FREE END.");
  detail.Text=b.ToString();
 }
 void ExportFile(){if(!AccessCodes.CanUseApp(1)){MessageBox.Show("Access code required.");return;}if(result==null)return;using(var dialog=new SaveFileDialog{Title="Export revised beam schedule",Filter="AutoCAD DXF (*.dxf)|*.dxf",FileName=prefixBox.Text+" Schedule - Revised.dxf",OverwritePrompt=true}){if(dialog.ShowDialog(this)!=DialogResult.OK)return;if(!AccessCodes.CanUseApp(1)){MessageBox.Show("Access expired. Renew access before exporting.");return;}try{string target=Path.GetFullPath(dialog.FileName);if(string.Equals(target,Path.GetFullPath(analysis.Path),StringComparison.OrdinalIgnoreCase))throw new Exception("Choose a new file name. The app keeps the original source DXF.");Engine.Export(result,target);status.Text="Saved: "+target;MessageBox.Show(this,"Revised DXF saved.\n\n"+result.Rows.Count+" final groups · "+result.BeamCount+" original beam numbers\n\n"+target,"Export complete",MessageBoxButtons.OK,MessageBoxIcon.Information);}catch(Exception ex){MessageBox.Show(this,ex.Message,"Not exported",MessageBoxButtons.OK,MessageBoxIcon.Error);}}}
 void CreateTree(Control c){typeof(Control).GetMethod("CreateControl",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(bool)},null).Invoke(c,new object[]{true});c.PerformLayout();foreach(Control child in c.Controls)CreateTree(child);}
 public async Task TestLoad(string path){await LoadFile(path);await BuildAsync();}
 public void RenderForTest(string path,string imagePath){analysis=Engine.Analyze(path);sourceBox.Text=Path.GetFileName(path);prefixBox.Text=analysis.Prefix;floorBox.Text=analysis.Floor;result=Engine.Build(analysis,new Settings{Prefix=analysis.Prefix,Floor=analysis.Floor,Merge=true});FillGrid();summary.Text="Loaded DXF: "+Path.GetFileName(sourceBox.Text)+"  ·  "+result.SourceCount+" source rows > "+result.Rows.Count+" output rows, "+result.BeamCount+" beam numbers";process.Enabled=true;export.Enabled=true;status.Text="Preview ready. Review the values or the DXF preview, then Export revised DXF.";typeof(Control).GetMethod("CreateControl",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(bool)},null).Invoke(this,new object[]{true});PerformLayout();foreach(Control child in Controls)CreateTree(child);using(var b=new Bitmap(Width,Height)){DrawToBitmap(b,new Rectangle(0,0,Width,Height));b.Save(imagePath);}}
}

static class Program {
 [STAThread]static int Main(string[] args){
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);ModernUi.Install();HorizontalWheel.Install();
  /* Owner (23 November 2026: "kapag bagong open or naka close huwag mag select ng project information"): the app opens with no project
     selected; the saved projects (shared by the Store app and the .exe) are picked from the list, and a Bill of Quantities file loads its own. */
  if(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")))ProjectInfoStore.ClearProject();
  try{
   #if TEST_BUILD
   if(args.Length>0&&args[0]=="--self-test")return SelfTest(args);
   if(args.Length==3&&args[0]=="--ui-test"){using(var form=new MainForm())form.RenderForTest(args[1],args[2]);return 0;}

   #endif
   #if ADMIN_APP
   if(!AdminPasswordGate.Unlock())return 0;Application.Run(new LauncherForm());return 0;
   #else
   if(!AccessCodes.TryRestore()){using(var login=new LoginForm())if(login.ShowDialog()!=DialogResult.OK)return 0;}
   Application.Run(new LauncherForm());return 0;
   #endif
  }catch(Exception ex){if(args.Length>0){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-error.txt"),ex.ToString());return 1;}MessageBox.Show(ex.Message,"Beam Schedule Formatter",MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;}
 }
 static void Assert(bool ok,string message){if(!ok)throw new Exception("TEST FAILED: "+message);}
 static int SelfTest(string[] args){
  if(args.Length<4)throw new Exception("--self-test source.dxf output.dxf report.txt");var a=Engine.Analyze(args[1]);var settings=new Settings{Prefix="4B",Floor="FOURTH FLOOR",Merge=true};var r=Engine.Build(a,settings);
  Assert(a.Source.Count==55,"55 original rows");Assert(a.BeamCount==122,"122 original beams");Assert(r.Rows.Count==51,"51 unique rows");Assert(r.Rows.Sum(x=>x.Numbers.Count)==122,"all beam numbers retained");Assert(r.Rows.Any(x=>x.SourceRows.SequenceEqual(new[]{20,22})),"merge 20+22");Assert(r.Rows.Any(x=>x.SourceRows.SequenceEqual(new[]{38,39,50})),"merge 38+39+50");Assert(r.Rows.Any(x=>x.SourceRows.SequenceEqual(new[]{47,49})),"merge 47+49");
  Assert(r.Rows.Where(x=>x.Cantilever).All(x=>x.Spacing.EndsWith("TO FREE END")),"cantilever rule");Assert(r.Rows.Any(x=>x.Spacing=="1 - 50mm, AND REST AT 175mm C/C"),"equal spacing rule");Assert(r.Rows.Any(x=>x.Spacing.StartsWith("1 - 50mm, 23 - 75mm, AND REST AT 100mm")),"smaller end spacing and unequal end counts preserved");
  var cant=r.Rows.Single(x=>x.SourceRows.Contains(19));Assert(cant.Spacing=="1 - 50mm, 9 - 100mm, AND REST AT 100mm TO FREE END"&&cant.Notes.Any(t=>t.Contains("USED 9pcs @ 100mm")),"cantilever minimum end spacing and note");
  var missing=r.Rows.Single(x=>x.SourceRows.Contains(52));Assert(missing.Spacing=="1 - 50mm, AND REST AT 100mm C/C"&&missing.Notes.Any(t=>t.Contains("MID BLANK IN SOURCE")),"blank mid uses smaller end spacing for AND REST AT and is noted");
  var blankFixture=a.Source[0].Copy();blankFixture.Cells[8][0]="17-4L-#10@150 C/C";blankFixture.Cells[9]=new List<string>{"-"};blankFixture.Cells[10][0]="12-4L-#10@100 C/C";Engine.ApplySpacing(blankFixture);Assert(blankFixture.Spacing=="1 - 50mm, 12 - 100mm, AND REST AT 100mm C/C","blank MID with unequal LEFT/RIGHT uses smaller spacing, independent of end counts");
  var fixture=a.Source[0].Copy();fixture.Cells[8][0]="17-4L-#10@100 C/C";fixture.Cells[9][0]="30-4L-#10@150 C/C";fixture.Cells[10][0]="17-4L-#10@75 C/C";Engine.ApplySpacing(fixture);Assert(fixture.Spacing.Contains("17 - 75mm, AND REST AT 150mm")&&fixture.Notes.Any(t=>t.Contains("USED 17pcs @ 75mm AT BOTH ENDS")),"non-cantilever uses minimum of LEFT and RIGHT, keeps MID");
  var noMerge=Engine.Consolidate(a,new Settings{Prefix="3B",Floor="THIRD FLOOR",Merge=false});Assert(noMerge.Count==55&&noMerge[0].Mark=="3B-001","merge toggle and prefix");
  var changed=new Analysis{BeamCount=a.BeamCount,Source=a.Source.Select(x=>x.Copy()).ToList()};changed.Source[21].Cells[0][0]="451";Assert(Engine.Consolidate(changed,settings).Count==52,"a different width must not merge");
  changed.Source=a.Source.Select(x=>x.Copy()).ToList();changed.Source[21].Cells[2][0]="5-#20";Assert(Engine.Consolidate(changed,settings).Count==52,"different reinforcement must not merge");
  bool invalid=false;try{Engine.Consolidate(a,new Settings{Prefix="invalid prefix!",Floor="FOURTH FLOOR"});}catch{invalid=true;}Assert(invalid,"invalid prefix rejected");
  invalid=false;try{Dxf.Parse("0\nSECTION\n2\nENTITIES\n");}catch{invalid=true;}Assert(invalid,"incomplete DXF rejected");
  var template=Engine.Standard();foreach(string type in new[]{"STYLE","LAYER"}){var original=template.Records.Where(e=>e.Type==type).Select(e=>string.Join("|",e.P.Select(p=>p.Code+":"+p.Value))).ToArray();var final=r.Drawing.Records.Where(e=>e.Type==type).Select(e=>string.Join("|",e.P.Select(p=>p.Code+":"+p.Value))).ToArray();Assert(original.SequenceEqual(final),"template "+type+" exact preservation");}
  var body=r.Drawing.ModelEntities().Where(e=>e.Type=="MTEXT"&&Regex.IsMatch(e.Get(1),@"^4B-\d{3}$")).OrderByDescending(e=>e.Num(20)).ToList();for(int i=1;i<body.Count;i++)Assert(Math.Abs(body[i-1].Num(20)-body[i].Num(20)-8)<.00001,"row height 8");
  foreach(var text in r.Drawing.ModelEntities().Where(e=>e.Type=="MTEXT"&&e.Num(10)>2294&&e.Num(10)<2427&&Regex.IsMatch(e.Get(1),@"^\d+ - %%C"))){Assert(text.Get(71).Trim()=="8"||text.Get(71).Trim()=="2","reinforcement layer attachment");var nearest=body.OrderBy(e=>Math.Abs(e.Num(20)-text.Num(20))).First();Assert(Math.Abs(text.Num(20)-nearest.Num(20)-(text.Get(71).Trim()=="8"?.5:-.5))<.00001,"single and first reinforcement layers aligned at TOP");}
  Engine.Export(r,args[2]);using(var image=Preview.Render(r.Drawing))image.Save(Path.Combine(Path.GetDirectoryName(args[3]),"cad-preview.png"));
  File.WriteAllText(args[3],"PASS\r\n55 source rows -> 51 groups\r\n122 original beam references retained\r\nExact Drawing1 font and layer definitions\r\n8-unit row heights\r\nSingle-layer reinforcement TOP-aligned\r\nMinimum LEFT/RIGHT spacing + associated piece count for all beams\r\nBlank MID uses smaller LEFT/RIGHT for AND REST AT, with note\r\nCantilever TO FREE END and adjustment notes\r\nEight-hour cached access replaces legacy password\r\nEqual-spacing format, merge toggle and floor prefix\r\nDifferent size/reinforcement rows not merged\r\nInvalid prefix and incomplete DXF rejected\r\nVerified DXF round-trip\r\n"+string.Join("\r\n",r.Changes));return 0;
 }
}
}

