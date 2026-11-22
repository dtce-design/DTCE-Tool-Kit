using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.IO;using System.Linq;using System.Text;using System.Text.RegularExpressions;
namespace BeamScheduleFormatter {
/* Column Schedule Formatter (owner, 22 November 2026: "can you create a column schedule formatter based on these dxf file from autocad,
   gawin mong presentable. tapos yung output dxf file will be used naman sa bbs column"; "gawan mo ng panibagong template").
   Reads the column and wall schedule exported from STAAD RCDC / AutoCAD (one column per mark, one band per level range: section with
   bars and links, Z1 / Z2 tie spacing, concrete, fy, cover and confining zone), groups identical columns, and draws a new schedule
   template (SCHEDULE OF COLUMNS). The revised DXF is read back by the same reader, so it loads in the Bar Bending Schedule (Column). */
class ColumnBar {public double X,Y;public int Dia;}
/* a shape of the RCDC drawing kept to draw the section as RCDC does (mm; vertices x, y, bulge) */
class RawShape {public List<double[]> P=new List<double[]>();public bool Closed;public double W,R;public string Role="";}
class ColumnLink {public double X0,Y0,X1,Y1;/* outside of the link (mm from the bottom-left corner of the section) */public double W{get{return X1-X0;}}public double H{get{return Y1-Y0;}}}
class ColumnLevel {public string From="",To="";public double FromM,ToM;public double Height{get{return Math.Abs(ToM-FromM);}}public string Text{get{return F(FromM)+" M TO "+F(ToM)+" M";}}static string F(double v){return v.ToString("0.0##",CultureInfo.InvariantCulture);}}
class ColumnCell {
 public double B,H;/* the RCDC drawing: links as drawn, the ring of a bar and the mark of each bar size (relative to the bar) */public List<RawShape> Raw=new List<RawShape>();public Dictionary<int,List<RawShape>> MarkerOf=new Dictionary<int,List<RawShape>>();public double RingMm;public List<ColumnBar> Bars=new List<ColumnBar>();public List<ColumnLink> Links=new List<ColumnLink>();public int TieDia;public double S1Main,S1Other,S2,Lo;public double Cover=40;public string Concrete="";public double Fy;
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 public Dictionary<int,int> Counts{get{return Bars.GroupBy(b=>b.Dia).OrderByDescending(g=>g.Key).ToDictionary(g=>g.Key,g=>g.Count());}}
 public string MainText{get{return string.Join(" + ",Counts.Select(k=>k.Value+"-%%C"+k.Key));}}
 public string TieText{get{return TieDia<=0?"--":"%%C"+TieDia+" @ "+S1Main.ToString("0",Inv)+(Math.Abs(S1Other-S1Main)>.5&&S1Other>0?" / "+S1Other.ToString("0",Inv):"")+" (Lo = "+Lo.ToString("0",Inv)+")";}}
 public string RestText{get{return TieDia<=0?"":"%%C"+TieDia+" @ "+S2.ToString("0",Inv)+" REST";}}
 public string Signature(){return Signature(true);}
 public string Signature(bool withLo){var b=new StringBuilder();b.Append(B.ToString("0",Inv)).Append("x").Append(H.ToString("0",Inv)).Append("|");foreach(var bar in Bars.OrderBy(q=>Math.Round(q.Y)).ThenBy(q=>Math.Round(q.X)))b.Append(Math.Round(bar.X/5)*5).Append(",").Append(Math.Round(bar.Y/5)*5).Append(":").Append(bar.Dia).Append(";");b.Append("|");foreach(var l in Links.OrderBy(q=>Math.Round(q.X0)).ThenBy(q=>Math.Round(q.Y0)).ThenBy(q=>q.W))b.Append(Math.Round(l.X0/5)*5).Append(",").Append(Math.Round(l.Y0/5)*5).Append(",").Append(Math.Round(l.W/5)*5).Append(",").Append(Math.Round(l.H/5)*5).Append(";");
  return b.Append("|").Append(TieDia).Append("@").Append(S1Main).Append("/").Append(S1Other).Append("/").Append(S2).Append("|").Append(withLo?Lo:0).Append("|").Append(Cover).Append("|").Append(Concrete).Append("|").Append(Fy).ToString();}}
class ColumnSchedule {/* the extra (inner) ties as open crossties instead of closed ties */public bool OpenExtra;public List<ColumnLevel> Levels=new List<ColumnLevel>();public List<string> Marks=new List<string>();public Dictionary<string,ColumnCell> Cells=new Dictionary<string,ColumnCell>(StringComparer.OrdinalIgnoreCase);
 public static string Key(string mark,int level){return mark+"|"+level;}
 public ColumnCell Cell(string mark,int level){ColumnCell c;return Cells.TryGetValue(Key(mark,level),out c)?c:null;}
 public Dictionary<string,string> Originals=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);}
class ColumnGroup {public string Mark;public List<string> Source=new List<string>();public string Originals{get{return string.Join(", ",Source);}}}
class ColumnFormatResult {public ColumnSchedule Schedule;public List<ColumnGroup> Groups;public Dxf Drawing;public ColumnSheet Sheet;public int SourceCount;}

static class ColumnScheduleReader {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 static string Text(Entity e){return SlabFormatter.Plain(e);}
 internal static List<double[]> PointsB(Entity e){var list=new List<double[]>();double x=0;bool hasX=false;foreach(var p in e.P){double v;if(!double.TryParse(p.Value,NumberStyles.Float,Inv,out v))continue;if(p.Code==10){x=v;hasX=true;}else if(p.Code==20&&hasX){list.Add(new[]{x,v,0});hasX=false;}else if(p.Code==42&&list.Count>0)list[list.Count-1][2]=v;}return list;}
 internal static List<double[]> Points(Entity e){var list=new List<double[]>();double x=0;bool hasX=false;foreach(var p in e.P){double v;if(!double.TryParse(p.Value,NumberStyles.Float,Inv,out v))continue;if(p.Code==10){x=v;hasX=true;}else if(p.Code==20&&hasX){list.Add(new[]{x,v});hasX=false;}}return list;}
 internal static int Dia(string s){var m=Regex.Match(s??"",@"(?:#|%%[cC]|Ø|DIA\.?\s*)(\d+)");return m.Success?int.Parse(m.Groups[1].Value,Inv):0;}
 static double Num(string s){var m=Regex.Match(s??"",@"-?\d+(\.\d+)?");return m.Success?double.Parse(m.Value,Inv):double.NaN;}
 public static ColumnSchedule Read(string raw){var d=Dxf.Parse(raw);var all=d.ModelEntities();if(all.Any(e=>(e.Type=="TEXT"||e.Type=="MTEXT")&&e.Get(8)=="MARK")&&all.Any(e=>(e.Type=="TEXT"||e.Type=="MTEXT")&&Text(e)=="COLUMN MARKED"))return ColumnFormatter.ReadRows(all);if(all.Any(e=>(e.Type=="TEXT"||e.Type=="MTEXT")&&(Text(e)=="SCHEDULE OF COLUMNS"||Text(e)=="COLUMN SCHEDULE")))return ColumnFormatter.ReadRevised(all);return ReadRcdc(d,all);}
 /* POLYLINE (old style) vertices follow the POLYLINE record as VERTEX records */
 static List<KeyValuePair<Entity,List<double[]>>> OldPolylines(Dxf d){var list=new List<KeyValuePair<Entity,List<double[]>>>();bool inside=false;Entity current=null;List<double[]> pts=null;
  foreach(var e in d.Records){if(e.Type=="SECTION"&&e.Get(2)=="ENTITIES"){inside=true;continue;}if(!inside)continue;if(e.Type=="ENDSEC")break;
   if(e.Type=="POLYLINE"){current=e;pts=new List<double[]>();continue;}if(e.Type=="VERTEX"&&current!=null){pts.Add(new[]{e.Num(10),e.Num(20),e.Num(42),e.Num(40)});continue;}if(e.Type=="SEQEND"&&current!=null){list.Add(new KeyValuePair<Entity,List<double[]>>(current,pts));current=null;}}return list;}
 static ColumnSchedule ReadRcdc(Dxf d,List<Entity> all){
  var texts=all.Where(e=>e.Type=="TEXT"||e.Type=="MTEXT").Select(e=>new{E=e,T=Text(e),X=e.Num(10),Y=e.Num(20)}).ToList();
  var label=texts.FirstOrDefault(t=>Regex.IsMatch(t.T,@"^COLUMN\s+MARK(ED)?$",RegexOptions.IgnoreCase));if(label==null)throw new Exception("This DXF has no COLUMN MARK row. Load the column schedule exported from STAAD RCDC / AutoCAD (ASCII DXF).");
  var marks=texts.Where(t=>t.X>label.X+500&&Math.Abs(t.Y-label.Y)<900&&Regex.IsMatch(t.T,@"^[A-Z]{1,4}[-]?\d+[A-Z]?$",RegexOptions.IgnoreCase)).OrderBy(t=>t.X).ToList();if(marks.Count==0)throw new Exception("No column marks found next to COLUMN MARK.");
  double pitch=marks.Count>1?Enumerable.Range(1,marks.Count-1).Select(i=>marks[i].X-marks[i-1].X).Where(v=>v>1).DefaultIfEmpty(6800).Min():6800;double labelRight=marks[0].X-pitch/2;
  /* level bands: long horizontal lines across the schedule */
  var lines=all.Where(e=>e.Type=="LINE").Select(e=>new{X0=Math.Min(e.Num(10),e.Num(11)),X1=Math.Max(e.Num(10),e.Num(11)),Y0=e.Num(20),Y1=e.Num(21),L=e.Get(8)}).ToList();double fullWidth=marks.Last().X-marks[0].X+pitch;
  var bandY=lines.Where(l=>Math.Abs(l.Y0-l.Y1)<1e-3&&l.X1-l.X0>fullWidth*.8&&l.X0<labelRight-1).Select(l=>Math.Round(l.Y0,1)).Distinct().OrderBy(v=>v).ToList();
  var levelTexts=texts.Where(t=>t.X<labelRight&&Regex.IsMatch(t.T,@"^-?\d+(\.\d+)?\s*M$",RegexOptions.IgnoreCase)).ToList();
  var s=new ColumnSchedule();var bands=new List<double[]>();
  for(int i=0;i+1<bandY.Count;i++){double y0=bandY[i],y1=bandY[i+1];var lv=levelTexts.Where(t=>t.Y>y0&&t.Y<y1).OrderByDescending(t=>t.Y).ToList();if(lv.Count<2)continue;var level=new ColumnLevel{From=lv[0].T,To=lv[lv.Count-1].T,FromM=Num(lv[0].T),ToM=Num(lv[lv.Count-1].T)};bands.Add(new[]{y0,y1});s.Levels.Add(level);}
  if(s.Levels.Count==0)throw new Exception("No level ranges (e.g. -2.7 M TO 0.6 M) found in the first column.");
  foreach(var m in marks)s.Marks.Add(m.T.ToUpperInvariant());
  var circles=all.Where(e=>e.Type=="CIRCLE").Select(e=>new{X=e.Num(10),Y=e.Num(20),R=e.Num(40)}).ToList();
  var concrete=all.Where(e=>e.Type=="LWPOLYLINE"&&e.Get(8).IndexOf("Concrete",StringComparison.OrdinalIgnoreCase)>=0).Select(Points).ToList();
  var linkEntities=all.Where(e=>e.Type=="LWPOLYLINE"&&e.Get(8).IndexOf("Link",StringComparison.OrdinalIgnoreCase)>=0).ToList();var linkPolys=linkEntities.Select(Points).ToList();
  var markers=OldPolylines(d).Where(p=>p.Key.Get(8).IndexOf("Reinforcement",StringComparison.OrdinalIgnoreCase)>=0).Select(p=>new{Closed=(((int)p.Key.Num(70))&1)==1,P=p.Value}).ToList();
  var dims=all.Where(e=>e.Type=="DIMENSION").Select(e=>new{M=e.Num(42),X1=e.Num(13),Y1=e.Num(23),X2=e.Num(14),Y2=e.Num(24),T=e.Get(1)}).ToList();
  Func<double,double,object> markerShape=(cx,cy)=>markers.FirstOrDefault(q=>q.P.Count>0&&q.P.All(v=>Math.Abs(v[0]-cx)<200&&Math.Abs(v[1]-cy)<200));
  Func<double,double,string> markerOf=(cx,cy)=>{var m=markers.FirstOrDefault(q=>q.P.Count>0&&q.P.All(v=>Math.Abs(v[0]-cx)<200&&Math.Abs(v[1]-cy)<200));if(m==null)return "none";var p=m.P;double dx=p.Max(v=>v[0])-p.Min(v=>v[0]),dy=p.Max(v=>v[1])-p.Min(v=>v[1]);return (m.Closed?"C":"O")+(dx>dy*1.5?"H":dy>dx*1.5?"V":"S")+p.Count;};
  for(int mi=0;mi<marks.Count;mi++){double cx0=marks[mi].X-pitch/2,cx1=marks[mi].X+pitch/2;
   for(int li=0;li<bands.Count;li++){double y0=bands[li][0],y1=bands[li][1];
    var seg=concrete.Where(p=>p.Count>0&&p.All(v=>v[0]>cx0&&v[0]<cx1&&v[1]>y0&&v[1]<y1)).SelectMany(p=>p).ToList();if(seg.Count<4)continue;
    double rx0=seg.Min(v=>v[0]),rx1=seg.Max(v=>v[0]),ry0=seg.Min(v=>v[1]),ry1=seg.Max(v=>v[1]);if(rx1-rx0<1||ry1-ry0<1)continue;
    var cell=new ColumnCell();
    /* sizes from the dimensions next to the section; else 1 unit = 0.25 mm as RCDC draws them */
    var near=dims.Where(q=>Math.Min(q.X1,q.X2)>=rx0-1500&&Math.Max(q.X1,q.X2)<=rx1+1500&&Math.Min(q.Y1,q.Y2)>=ry0-1500&&Math.Max(q.Y1,q.Y2)<=ry1+1500&&q.M>0).ToList();
    var horiz=near.FirstOrDefault(q=>Math.Abs(Math.Abs(q.X2-q.X1)-(rx1-rx0))<1);var vert=near.FirstOrDefault(q=>Math.Abs(Math.Abs(q.Y2-q.Y1)-(ry1-ry0))<1);
    double scale=horiz!=null?(rx1-rx0)/horiz.M:vert!=null?(ry1-ry0)/vert.M:4;cell.B=Math.Round((rx1-rx0)/scale);cell.H=Math.Round((ry1-ry0)/scale);
    /* bar diameters: the legend circles beside the section carry their size; the bars match a legend by the mark drawn inside the circle */
    var legend=new Dictionary<string,int>();foreach(var c in circles.Where(c=>c.X>cx0&&c.X<cx1&&c.Y>y0&&c.Y<y1&&(c.X>rx1+50||c.X<rx0-50))){var t=texts.Where(q=>q.X>c.X&&q.X<c.X+800&&Math.Abs(q.Y-c.Y)<300&&Dia(q.T)>0).OrderBy(q=>q.X-c.X).FirstOrDefault();if(t!=null)legend[markerOf(c.X,c.Y)]=Dia(t.T);}
    var mainText=texts.Where(q=>q.X>cx0&&q.X<cx1&&q.Y>y0&&q.Y<ry0&&Regex.IsMatch(q.T,@"^\d+\s*-\s*(#|%%[cC]|Ø)\d+")).Select(q=>q.T).FirstOrDefault()??"";var counts=new Dictionary<int,int>();foreach(Match m in Regex.Matches(mainText,@"(\d+)\s*-\s*(?:#|%%[cC]|Ø)(\d+)"))counts[int.Parse(m.Groups[2].Value,Inv)]=int.Parse(m.Groups[1].Value,Inv);
    foreach(var c in circles.Where(c=>c.X>rx0&&c.X<rx1&&c.Y>ry0&&c.Y<ry1)){int dia;string sig=markerOf(c.X,c.Y);if(!legend.TryGetValue(sig,out dia))dia=counts.Count==1?counts.Keys.First():0;cell.Bars.Add(new ColumnBar{X=Math.Round((c.X-rx0)/scale,1),Y=Math.Round((c.Y-ry0)/scale,1),Dia=dia});}
    /* the RCDC drawing of this section, in mm from its bottom-left corner */
    foreach(var c in circles.Where(c=>c.X>rx0&&c.X<rx1&&c.Y>ry0&&c.Y<ry1)){cell.RingMm=c.R/scale;var bar=cell.Bars.FirstOrDefault(b=>Math.Abs(b.X-Math.Round((c.X-rx0)/scale,1))<.2&&Math.Abs(b.Y-Math.Round((c.Y-ry0)/scale,1))<.2);if(bar==null||cell.MarkerOf.ContainsKey(bar.Dia))continue;
     var mk=markers.FirstOrDefault(q=>q.P.Count>0&&q.P.All(v=>Math.Abs(v[0]-c.X)<200&&Math.Abs(v[1]-c.Y)<200));if(mk==null)continue;var shp=new RawShape{Role="MARK",Closed=mk.Closed,W=mk.P[0].Length>3?mk.P[0][3]/scale:0};foreach(var v in mk.P)shp.P.Add(new[]{(v[0]-c.X)/scale,(v[1]-c.Y)/scale,v.Length>2?v[2]:0});cell.MarkerOf[bar.Dia]=new List<RawShape>{shp};}
    foreach(var le in linkEntities){var pb=ColumnScheduleReader.PointsB(le);if(pb.Count<2||!pb.All(v=>v[0]>rx0-1&&v[0]<rx1+1&&v[1]>ry0-1&&v[1]<ry1+1))continue;var shp=new RawShape{Role="LINK",Closed=(((int)le.Num(70))&1)==1};foreach(var v in pb)shp.P.Add(new[]{(v[0]-rx0)/scale,(v[1]-ry0)/scale,v[2]});cell.Raw.Add(shp);}
    if(cell.Bars.Any(b=>b.Dia==0))throw new Exception("Column "+s.Marks[mi]+", "+s.Levels[li].Text+": a bar size could not be read from the legend.");
    foreach(var k in counts)if(cell.Bars.Count(b=>b.Dia==k.Key)!=k.Value)throw new Exception("Column "+s.Marks[mi]+", "+s.Levels[li].Text+": "+mainText+" does not match the bars drawn ("+string.Join(" + ",cell.Counts.Select(q=>q.Value+"-#"+q.Key))+").");
    foreach(var p in linkPolys.Where(p=>p.Count>=4&&p.All(v=>v[0]>rx0-1&&v[0]<rx1+1&&v[1]>ry0-1&&v[1]<ry1+1))){cell.Links.Add(new ColumnLink{X0=Math.Round((p.Min(v=>v[0])-rx0)/scale),Y0=Math.Round((p.Min(v=>v[1])-ry0)/scale),X1=Math.Round((p.Max(v=>v[0])-rx0)/scale),Y1=Math.Round((p.Max(v=>v[1])-ry0)/scale)});}
    /* a single-leg crosstie (its hooks make a narrow box): kept as a leg at its centre line */foreach(var l in cell.Links){if(l.W<100&&l.H>=100){double cx=Math.Round((l.X0+l.X1)/2);l.X0=cx;l.X1=cx;}else if(l.H<100&&l.W>=100){double cy=Math.Round((l.Y0+l.Y1)/2);l.Y0=cy;l.Y1=cy;}}
    cell.Links=cell.Links.GroupBy(l=>l.X0+","+l.Y0+","+l.X1+","+l.Y1).Select(g=>g.First()).OrderByDescending(l=>l.W*l.H).ThenByDescending(l=>l.W+l.H).ToList();
    var inCell=texts.Where(q=>q.X>cx0-50&&q.X<cx1&&q.Y>y0&&q.Y<y1).ToList();
    foreach(var t in inCell){var m=Regex.Match(t.T,@"^(C\d+)\s*:\s*FY\s*(\d+)\s*,\s*COVER\s*=\s*(\d+)",RegexOptions.IgnoreCase);if(m.Success){cell.Concrete=m.Groups[1].Value.ToUpperInvariant();cell.Fy=double.Parse(m.Groups[2].Value,Inv);cell.Cover=double.Parse(m.Groups[3].Value,Inv);}
     m=Regex.Match(t.T,@"CONFINING\s+ZONE\s*=\s*(\d+)",RegexOptions.IgnoreCase);if(m.Success)cell.Lo=double.Parse(m.Groups[1].Value,Inv);}
    var heads=inCell.Where(t=>Regex.IsMatch(t.T,@"^Z[12]\b",RegexOptions.IgnoreCase)).ToList();
    foreach(var v in inCell.Where(t=>Regex.IsMatch(t.T,@"^(#|%%[cC]|Ø)\d+\s*@\s*\d+"))){var h=heads.Where(q=>q.Y>v.Y).OrderBy(q=>Math.Abs(q.X-v.X)+Math.Abs(q.Y-v.Y)*.2).FirstOrDefault();if(h==null)continue;var m=Regex.Match(v.T,@"(\d+)\s*@\s*(\d+)");int dia=int.Parse(m.Groups[1].Value,Inv);double sp=double.Parse(m.Groups[2].Value,Inv);cell.TieDia=Math.Max(cell.TieDia,dia);
     string hd=h.T.ToUpperInvariant();if(hd.StartsWith("Z2"))cell.S2=sp;else if(hd.Contains("MAIN"))cell.S1Main=sp;else cell.S1Other=sp;}
    if(cell.S1Other<=0)cell.S1Other=cell.S1Main;if(cell.S1Main<=0)cell.S1Main=cell.S1Other;if(cell.S2<=0)cell.S2=cell.S1Main;
    s.Cells[ColumnSchedule.Key(s.Marks[mi],li)]=cell;}}
  if(s.Cells.Count==0)throw new Exception("No column sections found in the schedule.");
  return s;}
}

/* the drawing primitives of the new template, used for the DXF and for the preview in the app */
class SheetItem {public string Kind,Text,Layer="0";public double[] P;public List<double[]> V;public bool Closed;public double H,W,Rot;public int Anchor=5;public bool Bold;public int Color=256;}
class ColumnSheet {public List<SheetItem> Items=new List<SheetItem>();public double Left,Top,Right,Bottom;
 public void Line(double x0,double y0,double x1,double y1,string layer="SCHEDULE",int color=256){Items.Add(new SheetItem{Kind="LINE",P=new[]{x0,y0,x1,y1},Layer=layer,Color=color});}
 public void Rect(double x0,double y0,double x1,double y1,string layer,int color=256){Items.Add(new SheetItem{Kind="RECT",P=new[]{x0,y0,x1,y1},Layer=layer,Color=color});}
 public void Disk(double x,double y,double r,string layer,int color){Items.Add(new SheetItem{Kind="DISK",P=new[]{x,y,r},Layer=layer,Color=color});}
 public void PLine(List<double[]> v,bool closed,double width,string layer){Items.Add(new SheetItem{Kind="PLINE",V=v,Closed=closed,W=width,Layer=layer});}
 public void Point(double x,double y,double z,string layer){Items.Add(new SheetItem{Kind="POINT",P=new[]{x,y,z},Layer=layer});}
 public void Circle(double x,double y,double r,string layer){Items.Add(new SheetItem{Kind="CIRCLE",P=new[]{x,y,r},Layer=layer});}
 public void RoundRect(double x0,double y0,double x1,double y1,double r,string layer){Items.Add(new SheetItem{Kind="RRECT",P=new[]{x0,y0,x1,y1,r},Layer=layer});}
 public void Text(string t,double x,double y,double h,int anchor=5,bool bold=false,string layer="TEXT",double wrap=0,double rot=0){Items.Add(new SheetItem{Kind="TEXT",Text=t,P=new[]{x,y},H=h,Anchor=anchor,Bold=bold,Layer=layer,W=wrap,Rot=rot});}}

static class ColumnFormatter {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 /* identical columns (every level the same) share one new mark */
 public static List<ColumnGroup> Group(ColumnSchedule s,string prefix,bool merge){prefix=(prefix??"").Trim().ToUpperInvariant();if(!Regex.IsMatch(prefix,@"^[A-Z0-9]{1,8}$"))throw new Exception("Enter the column prefix, e.g. C or 2C.");
  var groups=new List<ColumnGroup>();var lookup=new Dictionary<string,ColumnGroup>();foreach(var m in s.Marks){string key=string.Join("#",Enumerable.Range(0,s.Levels.Count).Select(l=>{var c=s.Cell(m,l);return c==null?"-":c.Signature(false);}));if(key.Replace("#","").Replace("-","")=="")continue;ColumnGroup g;if(!merge||!lookup.TryGetValue(key,out g)){g=new ColumnGroup();groups.Add(g);lookup[key]=g;}g.Source.Add(m);}
  /* a different confining zone Lo alone does not make another mark (owner, 22 November 2026: "minerge yung identical column pero di
     naman nag assign ng single column mark"): the group takes the largest Lo of its columns at each level */
  foreach(var g in groups)for(int l=0;l<s.Levels.Count;l++){var cells=g.Source.Select(m=>s.Cell(m,l)).Where(c=>c!=null).ToList();if(cells.Count<2)continue;double lo=cells.Max(c=>c.Lo);foreach(var c in cells)c.Lo=lo;}
  for(int n=0;n<groups.Count;n++)groups[n].Mark=prefix+(n+1).ToString(Inv);return groups;}
 /* an RCDC schedule as the grouped marks (the Bill of Quantities and the Bar Bending Schedule take it this way) */
 internal static ColumnSchedule Merged(ColumnSchedule s,string prefix){var groups=Group(s,prefix,true);var merged=new ColumnSchedule{Levels=s.Levels};foreach(var g in groups){merged.Marks.Add(g.Mark);merged.Originals[g.Mark]=g.Originals;for(int l=0;l<s.Levels.Count;l++){var c=s.Cell(g.Source[0],l);if(c!=null)merged.Cells[ColumnSchedule.Key(g.Mark,l)]=c;}}return merged;}
 const double Scale=20;/* sections at 1:20 on paper (mm) */
 /* one section sketch: concrete, the main link (magenta) and the other links / crossties (cyan), bars, sizes; prefix "P-" for the
    tie pattern row of the ACI schedule */
 /* the section as the owner's sample (Drawing1.dxf; owner, 22 November 2026: "yung drawing ng columns gayahin kagaya ng mismong dxf",
    "option if closed ties or open ties ang gagamitin doon sa extra", "create a legend per bar size"): every bar a 0.9 mm circle with
    the symbol of its size (one symbol per bar size for the whole schedule, listed under REMARKS); the ties drawn around the bars - each
    corner an arc on the corner bar - with 135° hooks into the core; the extra ties closed, or open (crossties with a 135° and a 90°
    hook). The exact tie and bar data stay in the file: bar size in the layer (BARS-20), each tie as two points (LINKPT / TIEPT). */
 [ThreadStatic] static Dictionary<int,int> symbolOf;[ThreadStatic] internal static bool openExtra;
 [ThreadStatic] static Dictionary<int,List<RawShape>> rawMarks;[ThreadStatic] static double rawRing;
 /* drawn exactly as the owner's Drawing1.dxf (owner, 22 November 2026: "pangit, sundan mismo yung paglayout ng sample file ko lalo na
    sa open ties"): every bar a 0.9 circle with the mark of its size (largest X, next a vertical line, next a filled dot); the hoop
    around the bars 1.2 off their centres with the 135° hook at the top left corner (a half circle and two parallel tails); the inner
    links 1.5 off the bars - closed, or open as in the sample: each link two C-shaped ties lapped, a 135° hook at one end and a straight
    tail at the other. Hidden data (layer DTCE-DATA, off and not plotted) keeps Lo, the Z1 spacing of the other links, the original
    marks and the tie option; bars keep their size in the layer (BARS-20) and each tie its two corners (LINKPT / TIEPT). */
 const double Ring=.9,HoopOff=1.2,Around=1.5,HookLen=3.75,S45=.70710678;const string Data="DTCE-DATA";
 static readonly double B135=Math.Tan(135*Math.PI/720),B90=Math.Tan(Math.PI/8);
 static List<double[]> V(params double[] v){var l=new List<double[]>();for(int i=0;i+2<v.Length;i+=3)l.Add(new[]{v[i],v[i+1],v[i+2]});return l;}
 static void Legend(ColumnSheet sh,double x,double y,int dia){BarSymbol(sh,x,y,Ring*1.2,Symbol(dia),"LEGEND","LEGEND");sh.Text("%%C"+dia+"mm",x+5.08,y,2,4,false,"LEGEND");}
 static int Symbol(int dia){int k;if(symbolOf!=null&&symbolOf.TryGetValue(dia,out k))return k;return 0;}
 static void SetSymbols(IEnumerable<int> sizes){symbolOf=new Dictionary<int,int>();int i=0;foreach(var d in sizes.Distinct().OrderByDescending(x=>x))symbolOf[d]=i++;}
 static void BarSymbol(ColumnSheet sh,double x,double y,double r,int style,string layer,string markLayer){sh.Circle(x,y,r,layer);double k=r*.71;switch(style%6){
   case 0:sh.PLine(V(x+k,y-k,0,x-k,y+k,0),false,0,markLayer);sh.PLine(V(x-k,y-k,0,x+k,y+k,0),false,0,markLayer);break;
   case 1:sh.PLine(V(x,y-r,0,x,y+r,0),false,0,markLayer);break;case 2:sh.Disk(x,y,r*.44,markLayer,256);break;
   case 3:sh.PLine(V(x-r,y,0,x+r,y,0),false,0,markLayer);break;case 4:sh.Circle(x,y,r*.5,markLayer);break;default:sh.Disk(x,y,r*.75,markLayer,256);break;}}
 /* the 135° hook at a corner bar (x, y): a half circle on the outside and two parallel tails into the core */
 static void HookAt(ColumnSheet sh,double x,double y,double r,string layer){double k=r*S45,d=HookLen*S45;sh.PLine(V(x+k,y+k,1,x-k,y-k,0),false,0,layer);sh.Line(x+k,y+k,x+k+d,y+k-d,layer);sh.Line(x-k,y-k,x-k+d,y-k-d,layer);}
 /* a closed tie around the bar centres (ax, ay) - (bx, by), corners as arcs on the corner bars */
 static void ClosedTie(ColumnSheet sh,double ax,double ay,double bx,double by,double r,string layer){sh.RoundRect(ax-r,ay-r,bx+r,by+r,r,layer);HookAt(sh,ax,by,r,layer);}
 /* an open link as the sample: two C-shaped ties along the long sides, each from a 135° hook round its two corner bars to a straight tail
    lapping the other one */
 static void OpenLink(ColumnSheet sh,double ax,double ay,double bx,double by,string layer){double r=Around,t=HookLen,k=r*S45,d=HookLen*S45;
  if(by-ay>=bx-ax){sh.PLine(V(ax+k+d,by+k-d,0,ax+k,by+k,B135,ax-r,by,0,ax-r,ay,B90,ax,ay-r,0,ax+t,ay-r,0),false,0,layer);sh.PLine(V(bx-k-d,ay-k+d,0,bx-k,ay-k,B135,bx+r,ay,0,bx+r,by,B90,bx,by+r,0,bx-t,by+r,0),false,0,layer);}
  else{sh.PLine(V(bx+r,by-t,0,bx+r,by,B90,bx,by+r,0,ax,by+r,B135,ax-k,by-k,0,ax-k+d,by-k-d,0),false,0,layer);sh.PLine(V(ax-r,ay+t,0,ax-r,ay,B90,ax,ay-r,0,bx,ay-r,B135,bx+k,ay+k,0,bx+k-d,ay+k+d,0),false,0,layer);}}
 /* a crosstie along its bars: 135° hook at one end, 90° at the other */
 static void OpenTie(ColumnSheet sh,double ax,double ay,double bx,double by,double tail){bool vertical=Math.Abs(bx-ax)<Math.Abs(by-ay);
  if(vertical){double x=ax+Around;sh.Line(x,ay,x,by,"CROSSTIES");sh.Line(x,by,x-tail*S45,by-tail*S45,"CROSSTIES");sh.Line(x,ay,x-tail,ay,"CROSSTIES");}
  else{double y=ay-Around;sh.Line(ax,y,bx,y,"CROSSTIES");sh.Line(ax,y,ax+tail*S45,y+tail*S45,"CROSSTIES");sh.Line(bx,y,bx,y+tail,"CROSSTIES");}}
 static void Sketch(ColumnSheet sh,ColumnCell c,double x0,double y0,string p,bool dims=true){double w=c.B/Scale,h=c.H/Scale;sh.Rect(x0,y0,x0+w,y0+h,p+"CONCRETE");
  for(int i=0;i<c.Links.Count;i++){var l=c.Links[i];bool tie=l.W<1||l.H<1;sh.Point(x0+l.X0/Scale,y0+l.Y0/Scale,2*i,p+(tie?"TIEPT":"LINKPT"));sh.Point(x0+l.X1/Scale,y0+l.Y1/Scale,2*i+1,p+(tie?"TIEPT":"LINKPT"));}
  foreach(var b in c.Bars)BarSymbol(sh,x0+b.X/Scale,y0+b.Y/Scale,Ring,Symbol(b.Dia),p+"BARS-"+b.Dia,p+"BARMARK");
  for(int i=0;i<c.Links.Count;i++){var l=c.Links[i];bool tie=l.W<1||l.H<1;
   var held=c.Bars.Where(b=>tie?(l.W<1?Math.Abs(b.X-l.X0)<60&&b.Y>=l.Y0-30&&b.Y<=l.Y1+30:Math.Abs(b.Y-l.Y0)<60&&b.X>=l.X0-30&&b.X<=l.X1+30):b.X>=l.X0-1&&b.X<=l.X1+1&&b.Y>=l.Y0-1&&b.Y<=l.Y1+1).ToList();
   if(held.Count==0){if(tie)sh.Line(x0+l.X0/Scale,y0+l.Y0/Scale,x0+l.X1/Scale,y0+l.Y1/Scale,p+"CROSSTIES");else sh.RoundRect(x0+l.X0/Scale,y0+l.Y0/Scale,x0+l.X1/Scale,y0+l.Y1/Scale,Around,p+"LINKS");continue;}
   double ax=x0+held.Min(b=>b.X)/Scale,bx=x0+held.Max(b=>b.X)/Scale,ay=y0+held.Min(b=>b.Y)/Scale,by=y0+held.Max(b=>b.Y)/Scale;
   if(tie||bx-ax<.1||by-ay<.1){OpenTie(sh,ax,ay,bx,by,HookLen);continue;}
   if(i==0)ClosedTie(sh,ax,ay,bx,by,HoopOff,p+"MAINLINK");else if(openExtra)OpenLink(sh,ax,ay,bx,by,p+"OPENTIES");else ClosedTie(sh,ax,ay,bx,by,Around,p+"LINKS");}
  if(dims){sh.Text(c.B.ToString("0",Inv),x0+w/2,y0-1,2,2);sh.Text(c.H.ToString("0",Inv),x0-1.5,y0+h/2,2,6);}}
 /* the dimensions of the sample: 7 off the section, circle ticks, extension lines past the dimension line */
 static void Dims(ColumnSheet sh,ColumnCell c,double sx,double sy,double w,double h){const double o=7,e=2,t=.45;double yd=sy+h+o,xd=sx-o;
  sh.Line(sx-e,yd,sx+w+e,yd,"DIM");sh.Line(sx,sy+h+1,sx,yd+e,"DIM");sh.Line(sx+w,sy+h+1,sx+w,yd+e,"DIM");sh.Circle(sx,yd,t,"DIM");sh.Circle(sx+w,yd,t,"DIM");sh.Text(c.B.ToString("0",Inv),sx+w/2,yd+.9,2,8,false,"DIM");
  sh.Line(xd,sy-e,xd,sy+h+e,"DIM");sh.Line(sx-1,sy,xd-e,sy,"DIM");sh.Line(sx-1,sy+h,xd-e,sy+h,"DIM");sh.Circle(xd,sy,t,"DIM");sh.Circle(xd,sy+h,t,"DIM");sh.Text(c.H.ToString("0",Inv),xd-.9,sy+h/2,2,8,false,"DIM",0,90);}
 /* the tie zones in a small table (owner, 22 November 2026: "clearly makita if ano yung main link, z1 main, z1 others etc") */
 static double TieTable(ColumnSheet sh,ColumnCell c,double x,double w,double yTop,bool aci){double cw=(w-4)/3,x0=x+2,rh=5;string[] heads={"Z1 MAIN LINK","Z1 OTHERS","Z2 LINKS"};string bar=(aci?"#":"%%C")+c.TieDia+" @ ",oc=aci?" O.C.":"";
  string[] vals={bar+c.S1Main.ToString("0",Inv)+oc,bar+c.S1Other.ToString("0",Inv)+oc,bar+c.S2.ToString("0",Inv)+oc};string[] layers={"Z1MAIN","Z1OTHERS","Z2"};
  sh.Rect(x0,yTop-2*rh,x0+3*cw,yTop,"TIETABLE");sh.Line(x0,yTop-rh,x0+3*cw,yTop-rh,"TIETABLE");for(int k=1;k<3;k++)sh.Line(x0+k*cw,yTop,x0+k*cw,yTop-2*rh,"TIETABLE");
  for(int k=0;k<3;k++){sh.Text(heads[k],x0+k*cw+cw/2,yTop-rh/2,1.6,5,true);sh.Text(vals[k],x0+k*cw+cw/2,yTop-rh*1.5,1.9,5,false,layers[k]);}
  sh.Text("Lo = "+c.Lo.ToString("0",Inv)+" (Z1 AT EACH END)",x+w/2,yTop-2*rh-2.4,1.8,5,false,"LO");return 2*rh+4.8;}
 static string LayoutKey(ColumnCell c){return c.B+"x"+c.H+"|"+string.Join(";",c.Bars.Select(b=>Math.Round(b.X)+","+Math.Round(b.Y)+":"+b.Dia))+"|"+string.Join(";",c.Links.Select(l=>l.X0+","+l.Y0+","+l.X1+","+l.Y1));}
 /* the owner's column schedule template (owner, 22 November 2026, Drawing1.dxf "sample only"): SCHEDULE OF COLUMNS with the notes,
    one row per column mark (COLUMN MARKED), one column per level range from the lowest, each cell with the section and its dimensions
    above a text box "4 - Ø20mm + 8 - Ø16mm MAIN BARS / WITH Ø10mm LATERAL TIES SPACED AT / 1 - 50mm, 9 - 75mm, AND REST AT 200mm C/C /
    (3 TIES/SET)"; the same section at neighbouring levels is one wide cell, a level without the column is crossed out; four marks per
    block, the blocks side by side; the bar legend under REMARKS */
 static string TieWords(ColumnCell c){return Math.Abs(c.S1Main-c.S2)<.5?"1 - 50mm, AND REST AT "+c.S2.ToString("0",Inv)+"mm C/C":ProgramOfWorksForm.ColumnTieText(c);}
 internal static string CellText(ColumnCell c){var parts=c.Counts.Select(k=>k.Value+" - %%C"+k.Key+"mm");return string.Join(" + ",parts)+" MAIN BARS\nWITH %%C"+c.TieDia+"mm LATERAL TIES SPACED AT\n"+TieWords(c)+"\n("+c.Links.Count+" TIES/SET)";}
 static bool Uniform(ColumnSchedule s,Func<ColumnCell,string> f){return s.Cells.Values.Select(f).Distinct().Count()<=1;}
 static string Grade(double fy){int g=fy>=515?75:fy>=410?60:fy>=340?50:fy>=270?40:fy>=225?33:0;return g>0?" (GRADE "+g+")":"";}
 /* the continuation mark of the sample between neighbouring sections of a row */
 static void Marker(ColumnSheet sh,double x,double yTop){sh.PLine(V(x-1.66,yTop-12.13,0,x-1.66,yTop-18.07,0,x+3.4,yTop-15.1,0),true,0,"MARKER");}
 /* the owner's template, Drawing1.dxf: SCHEDULE OF COLUMNS and the NOTES above, four marks per block (COLUMN MARKED, one column of
    68 per level range from the lowest, rows of 79: the section with its dimensions above an 18 high text cell), the blocks side by
    side 10 apart, the same section at neighbouring levels one wide cell, a level without the column crossed out, the bar legend under
    REMARKS at the top right and the bottom left */
 static ColumnSheet LayoutRows(ColumnSchedule s,List<ColumnGroup> groups,string title){var sh=new ColumnSheet();const int PerBlock=4;
  double maxB=s.Cells.Values.Max(c=>c.B)/Scale,maxH=s.Cells.Values.Max(c=>c.H)/Scale;double cellW=Math.Max(68,maxB+43),secH=Math.Max(61,maxH+36),textH=18,rowH=secH+textH,labelW=39,headH=23,gap=10,top=0;
  bool sameMat=Uniform(s,c=>c.Concrete+"|"+c.Fy+"|"+c.Cover);var any=s.Cells.Values.First();
  sh.Text("SCHEDULE OF COLUMNS",0,top+11.76,7,7,true,"TITLE");if(!string.IsNullOrWhiteSpace(title))sh.Text(title.Trim().ToUpperInvariant(),100,top+11.76,3.5,7,true,"TITLE");
  var notes=new List<string>();if(sameMat){if(any.Fy>0)notes.Add("fy = "+any.Fy.ToString("0",Inv)+" MPa"+Grade(any.Fy));notes.Add("CONCRETE "+(any.Concrete==""?"AS SPECIFIED":any.Concrete)+", CLEAR COVER "+any.Cover.ToString("0",Inv)+"mm");}
  if(notes.Count>0)sh.Text("NOTES:    "+string.Join("\n              ",notes),0,top+9.76,2.5,1,false,"NOTES");
  sh.Text(s.OpenExtra?"EXTRA TIES: OPEN":"EXTRA TIES: CLOSED",0,top+1,1,1,false,Data);
  double x0=0,bottom=top-headH;var sizes=s.Cells.Values.SelectMany(c=>c.Bars).Select(b=>b.Dia).Distinct().OrderByDescending(d=>d).ToList();
  for(int b0=0;b0<groups.Count;b0+=PerBlock){var block=groups.Skip(b0).Take(PerBlock).ToList();
   var levels=Enumerable.Range(0,s.Levels.Count).Where(l=>block.Any(g=>s.Cell(g.Source[0],l)!=null)).OrderBy(l=>Math.Min(s.Levels[l].FromM,s.Levels[l].ToM)).ToList();
   double width=labelW+cellW*levels.Count,y=top;
   sh.Rect(x0,y-headH,x0+width,y,"SCHEDULE");sh.Text("COLUMN MARKED",x0+labelW/2,y-headH/2,2.5,5,true,"HEAD");
   for(int k=0;k<levels.Count;k++){var lv=s.Levels[levels[k]];double cx=x0+labelW+k*cellW;sh.Line(cx,y,cx,y-headH,"SCHEDULE");sh.Text(Math.Min(lv.FromM,lv.ToM).ToString("0.00",Inv)+" TO "+Math.Max(lv.FromM,lv.ToM).ToString("0.00",Inv),cx+cellW/2,y-headH/2,2.5,5,false,"LEVEL");}
   y-=headH;
   foreach(var g in block){sh.Rect(x0,y-rowH,x0+width,y,"SCHEDULE");sh.Line(x0+labelW,y,x0+labelW,y-rowH,"SCHEDULE");sh.Text(g.Mark,x0+labelW/2,y-rowH/2,2.5,5,true,"MARK");
    sh.Text("ORIGINALS: "+g.Originals,x0+labelW/2,y-rowH/2-4,1,5,false,Data,labelW-4);
    int k=0;bool before=false;while(k<levels.Count){var c=s.Cell(g.Source[0],levels[k]);int span=1;if(c!=null)while(k+span<levels.Count){var n=s.Cell(g.Source[0],levels[k+span]);if(n==null||n.Signature()!=c.Signature())break;span++;}
     double cx0=x0+labelW+k*cellW,cx1=cx0+span*cellW;if(k>0)sh.Line(cx0,y,cx0,y-rowH,"SCHEDULE");
     if(c==null){sh.Line(cx0,y,cx1,y-rowH,"SCHEDULE");sh.Line(cx0,y-rowH,cx1,y,"SCHEDULE");before=false;k++;continue;}
     if(before)Marker(sh,cx0,y);before=true;
     double w=c.B/Scale,h=c.H/Scale,sx=(cx0+cx1)/2-w/2,sy=y-secH/2-h/2;Sketch(sh,c,sx,sy,"",false);Dims(sh,c,sx,sy,w,h);
     sh.Rect(cx0,y-rowH,cx1,y-rowH+textH,"CELLBOX");sh.Text(CellText(c),(cx0+cx1)/2,y-rowH+textH/2,2,5,false,"CELLTEXT",cx1-cx0-1.7);
     sh.Text("Lo="+c.Lo.ToString("0",Inv)+";Z1O="+c.S1Other.ToString("0",Inv),(cx0+cx1)/2,y-rowH+textH/2,1,5,false,Data);
     if(!sameMat)sh.Text((c.Concrete==""?"-":c.Concrete)+"  fy "+c.Fy.ToString("0",Inv)+"  cover "+c.Cover.ToString("0",Inv),(cx0+cx1)/2,y-secH+1.5,1.8,5,false,"MATERIAL");
     k+=span;}
    y-=rowH;}
   bottom=Math.Min(bottom,y);x0+=width+gap;}
  double right=x0-gap,sheetTop=top+18.8;
  /* REMARKS: the bar legend, top right (when it fits above the header) and bottom left as in the sample */
  if(sizes.Count<=4){double rx=right-29,ry=top+19.52;sh.Text("REMARKS:",rx,ry,2.5,4,true,"REMARKS");for(int k=0;k<sizes.Count;k++)Legend(sh,rx+4.54,ry-4.93-4*k,sizes[k]);sheetTop=ry+3;}
  {double ry=bottom-4.29;sh.Text("REMARKS:",0,ry,2.5,4,true,"REMARKS");for(int k=0;k<sizes.Count;k++)Legend(sh,4.54,ry-4.93-4*k,sizes[k]);bottom=ry-4.93-4*Math.Max(0,sizes.Count-1)-3;}
  sh.Left=0;sh.Right=Math.Max(right,60);sh.Top=sheetTop;sh.Bottom=bottom;return sh;}
 /* reading the owner's template back */
 internal static ColumnSchedule ReadRows(List<Entity> all){var s=new ColumnSchedule();var texts=all.Where(e=>e.Type=="MTEXT"||e.Type=="TEXT").Select(e=>new{T=SlabFormatter.Plain(e),X=e.Num(10),Y=e.Num(20),L=e.Get(8)}).ToList();
  var polys=all.Where(e=>e.Type=="LWPOLYLINE").Select(e=>new{L=e.Get(8),P=ColumnScheduleReader.Points(e)}).ToList();
  var note=string.Join(" ",texts.Where(t=>t.L=="NOTES").Select(t=>t.T));s.OpenExtra=note.Contains("EXTRA TIES: OPEN")||texts.Any(t=>t.L==Data&&t.T.Contains("EXTRA TIES: OPEN"));var fy=Regex.Match(note,@"fy\s*=\s*(\d+)");var conc=Regex.Match(note,@"CONCRETE\s+(\S+),\s*CLEAR COVER\s+(\d+)");
  var levelIndex=new Dictionary<string,int>();Func<string,int> level=t=>{int i;if(levelIndex.TryGetValue(t,out i))return i;var m=Regex.Match(t,@"^(-?\d+(?:\.\d+)?)\s+TO\s+(-?\d+(?:\.\d+)?)$");double a=double.Parse(m.Groups[1].Value,Inv),b=double.Parse(m.Groups[2].Value,Inv);s.Levels.Add(new ColumnLevel{From=a.ToString("0.00",Inv)+" M",To=b.ToString("0.00",Inv)+" M",FromM=a,ToM=b});i=s.Levels.Count-1;levelIndex[t]=i;return i;};
  var levelTexts=texts.Where(t=>t.L=="LEVEL").ToList();foreach(var lt in levelTexts.OrderBy(t=>double.Parse(Regex.Match(t.T,@"^-?\d+(?:\.\d+)?").Value,Inv)))level(lt.T);
  var boxes=polys.Where(p=>p.L=="CELLBOX"&&p.P.Count==4).Select(p=>new{X0=p.P.Min(v=>v[0]),X1=p.P.Max(v=>v[0]),Y0=p.P.Min(v=>v[1]),Y1=p.P.Max(v=>v[1])}).ToList();
  foreach(var mk in texts.Where(t=>t.L=="MARK").OrderBy(t=>t.X).ThenByDescending(t=>t.Y)){string mark=mk.T;if(!s.Marks.Contains(mark))s.Marks.Add(mark);var orig=texts.FirstOrDefault(t=>(t.L=="ORIGINALS"||t.L==Data&&t.T.StartsWith("ORIGINALS:"))&&Math.Abs(t.X-mk.X)<.01&&t.Y<mk.Y&&t.Y>mk.Y-15);if(orig!=null)s.Originals[mark]=Regex.Replace(orig.T,@"^ORIGINALS:\s*","").Trim().TrimStart('(').TrimEnd(')');
   /* the block of this mark: up to the next COLUMN MARKED header to the right */double right=texts.Where(t=>t.L=="HEAD"&&t.X>mk.X+1).Select(t=>t.X).DefaultIfEmpty(double.MaxValue).Min();
   var heads=levelTexts.Where(t=>t.X>mk.X&&t.X<right&&t.Y>mk.Y).ToList();double headY=heads.Count==0?0:heads.Where(t=>t.Y>mk.Y).Min(t=>t.Y);var mine=heads.Where(t=>Math.Abs(t.Y-headY)<.01).OrderBy(t=>t.X).ToList();
   /* the row of this mark: the text cells just under it */var below=boxes.Where(b=>b.X0>mk.X&&b.X1<right&&b.Y0<mk.Y).ToList();double rowY=below.Count==0?double.NaN:below.Max(b=>b.Y0);double reach=mk.Y+(mk.Y-rowY);
   foreach(var box in below.Where(b=>Math.Abs(b.Y0-rowY)<.01)){var cover=mine.Where(t=>t.X>box.X0&&t.X<box.X1).ToList();if(cover.Count==0)continue;
    var concRect=polys.FirstOrDefault(p=>p.L=="CONCRETE"&&p.P.Count==4&&p.P.All(v=>v[0]>box.X0&&v[0]<box.X1&&v[1]>box.Y1&&v[1]<reach));if(concRect==null)continue;
    var c=SketchBack(all,polys.Select(p=>new KeyValuePair<string,List<double[]>>(p.L,p.P)).ToList(),concRect.P.Min(v=>v[0]),concRect.P.Min(v=>v[1]),concRect.P.Max(v=>v[0]),concRect.P.Max(v=>v[1]));
    var ct=texts.FirstOrDefault(t=>t.L=="CELLTEXT"&&t.X>box.X0&&t.X<box.X1&&t.Y>box.Y0&&t.Y<box.Y1);if(ct!=null){var m=Regex.Match(ct.T,@"WITH\s*(?:%%[cC]|Ø|#)(\d+)\s*mm");if(m.Success)c.TieDia=int.Parse(m.Groups[1].Value,Inv);
     m=Regex.Match(ct.T,@"1\s*-\s*50mm,\s*(?:(\d+)\s*-\s*(\d+)mm,\s*)?AND REST AT\s*(\d+)mm");if(m.Success){c.S2=double.Parse(m.Groups[3].Value,Inv);c.S1Main=m.Groups[2].Success?double.Parse(m.Groups[2].Value,Inv):c.S2;}
     m=Regex.Match(ct.T,@"Lo\s*=\s*(\d+)");if(m.Success)c.Lo=double.Parse(m.Groups[1].Value,Inv);m=Regex.Match(ct.T,@"Z1 OTHERS @\s*(\d+)");c.S1Other=m.Success?double.Parse(m.Groups[1].Value,Inv):c.S1Main;}
    var dt=texts.FirstOrDefault(t=>t.L==Data&&t.X>box.X0&&t.X<box.X1&&t.Y>box.Y0&&t.Y<box.Y1);if(dt!=null){var m=Regex.Match(dt.T,@"Lo=(\d+(?:\.\d+)?);Z1O=(\d+(?:\.\d+)?)");if(m.Success){c.Lo=double.Parse(m.Groups[1].Value,Inv);c.S1Other=double.Parse(m.Groups[2].Value,Inv);}}
    var mat=texts.FirstOrDefault(t=>t.L=="MATERIAL"&&t.X>box.X0&&t.X<box.X1&&t.Y>box.Y1&&t.Y<reach);var mm=mat==null?null:Regex.Match(mat.T,@"^(\S*)\s*fy\s+(\d+)\s*cover\s+(\d+)");
    if(mm!=null&&mm.Success){c.Concrete=mm.Groups[1].Value=="-"?"":mm.Groups[1].Value;c.Fy=double.Parse(mm.Groups[2].Value,Inv);c.Cover=double.Parse(mm.Groups[3].Value,Inv);}else{if(fy.Success)c.Fy=double.Parse(fy.Groups[1].Value,Inv);if(conc.Success){c.Concrete=conc.Groups[1].Value=="AS"?"":conc.Groups[1].Value;c.Cover=double.Parse(conc.Groups[2].Value,Inv);}}
    foreach(var lt in cover)s.Cells[ColumnSchedule.Key(mark,level(lt.T))]=c;}}
  if(s.Cells.Count==0)throw new Exception("No column sections in the schedule.");return s;}
 /* a section drawn by Sketch, read back from its box */
 static ColumnCell SketchBack(List<Entity> all,List<KeyValuePair<string,List<double[]>>> polys,double x0,double y0,double x1,double y1){var c=new ColumnCell{B=Math.Round((x1-x0)*Scale),H=Math.Round((y1-y0)*Scale)};Func<double[],bool> inBox=v=>v[0]>=x0-.01&&v[0]<=x1+.01&&v[1]>=y0-.01&&v[1]<=y1+.01;
  foreach(var e in all.Where(e=>e.Type=="CIRCLE"&&e.Get(8).StartsWith("BARS-"))){double cx=e.Num(10),cy=e.Num(20);if(!inBox(new[]{cx,cy}))continue;int dia;if(!int.TryParse(e.Get(8).Substring(5),out dia))continue;c.Bars.Add(new ColumnBar{X=Math.Round((cx-x0)*Scale,1),Y=Math.Round((cy-y0)*Scale,1),Dia=dia});}
  foreach(var q in polys.Where(q=>(q.Key=="MAINLINK"||q.Key=="LINKS")&&q.Value.Count>=4&&q.Value.All(inBox)).OrderByDescending(q=>q.Key=="MAINLINK"))c.Links.Add(new ColumnLink{X0=Math.Round((q.Value.Min(v=>v[0])-x0)*Scale),Y0=Math.Round((q.Value.Min(v=>v[1])-y0)*Scale),X1=Math.Round((q.Value.Max(v=>v[0])-x0)*Scale),Y1=Math.Round((q.Value.Max(v=>v[1])-y0)*Scale)});
  foreach(var e in all.Where(e=>e.Type=="LINE"&&e.Get(8)=="CROSSTIES")){double ax=e.Num(10),ay=e.Num(20),bx=e.Num(11),by=e.Num(21);if(!inBox(new[]{ax,ay})||!inBox(new[]{bx,by}))continue;c.Links.Add(new ColumnLink{X0=Math.Round((Math.Min(ax,bx)-x0)*Scale),Y0=Math.Round((Math.Min(ay,by)-y0)*Scale),X1=Math.Round((Math.Max(ax,bx)-x0)*Scale),Y1=Math.Round((Math.Max(ay,by)-y0)*Scale)});}
  LinksFromPoints(all,c,x0,y0,x1,y1,"");var main=c.Links.Take(1).ToList();c.Links=main.Concat(c.Links.Skip(1).OrderByDescending(l=>l.W*l.H).ThenByDescending(l=>l.W+l.H)).ToList();return c;}
 /* the ties written as point pairs (index = z / 2) replace what the drawing shows */
 static void LinksFromPoints(List<Entity> all,ColumnCell c,double x0,double y0,double x1,double y1,string p){var pts=all.Where(e=>e.Type=="POINT"&&(e.Get(8)==p+"LINKPT"||e.Get(8)==p+"TIEPT")&&e.Num(10)>=x0-.01&&e.Num(10)<=x1+.01&&e.Num(20)>=y0-.01&&e.Num(20)<=y1+.01).ToList();if(pts.Count<2)return;
  var links=new List<ColumnLink>();foreach(var g in pts.GroupBy(e=>(int)Math.Floor(e.Num(30)/2+1e-9)).OrderBy(g=>g.Key)){var a=g.FirstOrDefault(e=>((int)Math.Round(e.Num(30)))%2==0);var b=g.FirstOrDefault(e=>((int)Math.Round(e.Num(30)))%2==1);if(a==null||b==null)continue;links.Add(new ColumnLink{X0=Math.Round((a.Num(10)-x0)*Scale),Y0=Math.Round((a.Num(20)-y0)*Scale),X1=Math.Round((b.Num(10)-x0)*Scale),Y1=Math.Round((b.Num(20)-y0)*Scale)});}c.Links=links;}
 public static ColumnSheet Layout(ColumnSchedule s,List<ColumnGroup> groups,string title,bool aci=false){SetSymbols(s.Cells.Values.SelectMany(c=>c.Bars).Select(b=>b.Dia));openExtra=s.OpenExtra;rawMarks=new Dictionary<int,List<RawShape>>();rawRing=0;foreach(var c in s.Cells.Values){if(c.RingMm>0)rawRing=c.RingMm;foreach(var k in c.MarkerOf)if(!rawMarks.ContainsKey(k.Key))rawMarks[k.Key]=k.Value;}if(!aci)return LayoutRows(s,groups,title);var sh=new ColumnSheet();
  double maxB=s.Cells.Values.Max(c=>c.B),maxH=s.Cells.Values.Max(c=>c.H);double cellW=Math.Max(62,maxB/Scale+30),secH=maxH/Scale+9,labelW=30,headH=9,top=0;
  var order=Enumerable.Range(0,s.Levels.Count).OrderByDescending(i=>Math.Max(s.Levels[i].FromM,s.Levels[i].ToM)).ToList();
  /* ACI: the tie pattern of each mark is drawn once (its lowest level); a level with another layout keeps its own sketch */
  var pattern=new Dictionary<string,ColumnCell>();foreach(var g in groups){var low=Enumerable.Range(0,s.Levels.Count).Where(l=>s.Cell(g.Source[0],l)!=null).OrderBy(l=>Math.Min(s.Levels[l].FromM,s.Levels[l].ToM)).Select(l=>s.Cell(g.Source[0],l)).FirstOrDefault();if(low!=null)pattern[g.Mark]=low;}
  Func<ColumnGroup,ColumnCell,bool> ownSketch=(g,c)=>!aci||!pattern.ContainsKey(g.Mark)||LayoutKey(pattern[g.Mark])!=LayoutKey(c);
  double textH=aci?34:28;
  double width=labelW+cellW*groups.Count;sh.Text(aci?"COLUMN SCHEDULE":"SCHEDULE OF COLUMNS",0,top+12,4.5,7,true);sh.Text(string.IsNullOrWhiteSpace(title)?"":title.Trim().ToUpperInvariant(),0,top+6,3,7,true);
  /* header */double y=top;sh.Rect(0,y-headH,width,y,"SCHEDULE");sh.Text("COLUMN MARK",labelW/2,y-headH/2,2.5,5,true);for(int g=0;g<groups.Count;g++){double x=labelW+g*cellW;sh.Line(x,y,x,y-headH);sh.Text(groups[g].Mark,x+cellW/2,y-headH/2,3,5,true);}y-=headH;
  foreach(int li in order){var lv=s.Levels[li];bool sketches=groups.Any(g=>{var c=s.Cell(g.Source[0],li);return c!=null&&ownSketch(g,c);});bool any=groups.Any(g=>s.Cell(g.Source[0],li)!=null);double rowH=any?(sketches?secH:0)+textH:16;
   sh.Line(0,y-rowH,width,y-rowH);sh.Line(0,y,0,y-rowH);sh.Line(width,y,width,y-rowH);double lo=Math.Min(lv.FromM,lv.ToM),hi=Math.Max(lv.FromM,lv.ToM);
   sh.Text(hi.ToString("0.00",Inv)+" M",labelW/2,y-rowH/2+4,2.2,5,true);sh.Text("TO",labelW/2,y-rowH/2,1.8,5);sh.Text(lo.ToString("0.00",Inv)+" M",labelW/2,y-rowH/2-4,2.2,5,true);
   for(int g=0;g<groups.Count;g++){double x=labelW+g*cellW;sh.Line(x,y,x,y-rowH);var c=s.Cell(groups[g].Source[0],li);if(c==null){sh.Text("-",x+cellW/2,y-rowH/2,2.5);continue;}
    double ty=y-3;if(sketches&&ownSketch(groups[g],c)){double w=c.B/Scale,h=c.H/Scale;Sketch(sh,c,x+(cellW-w)/2+2,y-4-secH+(secH-h)/2,"");}if(sketches)ty=y-4-secH-1;
    if(aci){sh.Text(c.B.ToString("0",Inv)+" x "+c.H.ToString("0",Inv),x+cellW/2,ty-1.5,2,5,false,"SIZE");ty-=4;}
    sh.Text(c.MainText,x+cellW/2,ty-1.6,2.5,5,true);ty-=4.8;ty-=TieTable(sh,c,x,cellW,ty,aci);
    sh.Text((c.Concrete==""?"-":c.Concrete)+(c.Fy>0?"  fy "+c.Fy.ToString("0",Inv):"")+"  cover "+c.Cover.ToString("0",Inv),x+cellW/2,ty-1.2,1.8,5);}
   y-=rowH;}
  if(aci){double patH=secH+6;sh.Rect(0,y-patH,width,y,"SCHEDULE");sh.Text("TIE PATTERN",labelW/2,y-patH/2,2,5,true);
   for(int g=0;g<groups.Count;g++){double x=labelW+g*cellW;sh.Line(x,y,x,y-patH);ColumnCell c;if(!pattern.TryGetValue(groups[g].Mark,out c))continue;double w=c.B/Scale,h=c.H/Scale;Sketch(sh,c,x+(cellW-w)/2+2,y-3-secH+(secH-h)/2,"P-");}y-=patH;}
  /* original marks */double origH=Math.Max(10,groups.Max(g=>Math.Ceiling(g.Originals.Length*1.05/(cellW/1.9))*2.6+4));sh.Rect(0,y-origH,width,y,"SCHEDULE");sh.Text("ORIGINAL MARKS",labelW/2,y-origH/2,2,5,true);
  for(int g=0;g<groups.Count;g++){double x=labelW+g*cellW;sh.Line(x,y,x,y-origH);sh.Text(groups[g].Originals,x+cellW/2,y-2,1.8,2,false,"ORIGINALS",cellW-4);}
  y-=origH;
  {var sz=s.Cells.Values.SelectMany(c=>c.Bars).Select(b=>b.Dia).Distinct().OrderByDescending(d=>d).ToList();sh.Text("REMARKS:",0,y-4,2.2,7,true,"REMARKS");for(int k=0;k<sz.Count;k++)Legend(sh,2+k*14,y-8,sz[k]);y-=10;}
  sh.Text("NOTES: SECTIONS AT 1:20. Z1 MAIN LINK = THE OUTER HOOP (HEAVY LINE) WITHIN Lo; Z1 OTHERS = THE INNER LINKS AND CROSSTIES WITHIN Lo; Z2 LINKS = ALL LINKS OUTSIDE Lo. Lo = CONFINING ZONE AT EACH END OF THE CLEAR HEIGHT. ALL HOOPS AND LINKS WITH 135° HOOKS.",0,y-4,1.8,7,false,"TEXT",width);
  sh.Left=0;sh.Right=width;sh.Top=top+16;sh.Bottom=y-8;return sh;}
 public static ColumnFormatResult Build(string raw,string prefix,string title,bool merge,bool aci=false,bool openTies=false){var s=ColumnScheduleReader.Read(raw);s.OpenExtra=openTies;var groups=Group(s,prefix,merge);if(groups.Count==0)throw new Exception("No columns to format.");var sheet=Layout(s,groups,title,aci);if(aci)AciDetailing.Apply(sheet);return new ColumnFormatResult{Schedule=s,Groups=groups,Sheet=sheet,Drawing=ToDxf(sheet,s,groups),SourceCount=s.Marks.Count};}
 /* the DXF: header, tables and styles of the slab template; entities drawn new */
 internal static Dxf ToDxf(ColumnSheet sh,ColumnSchedule s,List<ColumnGroup> groups){Dxf drawing;using(var stream=typeof(ColumnFormatter).Assembly.GetManifestResourceStream("SlabTemplate.dxf")){if(stream==null)throw new Exception("Template resource missing.");using(var reader=new StreamReader(stream,Encoding.UTF8,true))drawing=Dxf.Parse(reader.ReadToEnd());}
  var original=drawing.ModelEntities();var linePrototype=original.First(e=>e.Type=="LINE");var textPrototype=original.First(e=>e.Type=="MTEXT");string owner=linePrototype.Get(330);
  long next=1;foreach(var e in drawing.Records){long handle;if(long.TryParse(e.Get(5),NumberStyles.HexNumber,Inv,out handle))next=Math.Max(next,handle+1);}var output=new List<Entity>();
  Action<Entity> add=e=>{e.P.RemoveAll(p=>p.Code==5||p.Code==102);e.P.Insert(1,new Pair(5,(next++).ToString("X")));output.Add(e);};
  Func<string,string,int,Entity> head=(type,layer,color)=>{var e=new Entity();e.P.Add(new Pair(0,type));if(owner!="")e.P.Add(new Pair(330,owner));e.P.Add(new Pair(100,"AcDbEntity"));e.P.Add(new Pair(8,layer));if(color!=256)e.P.Add(new Pair(62,color.ToString(Inv)));return e;};
  Func<double,string> f=v=>v.ToString("0.####",Inv);
  foreach(var it in sh.Items){if(it.Kind=="LINE"){var e=linePrototype.Copy(true);e.Set(8,it.Layer);e.Set(10,it.P[0]);e.Set(20,it.P[1]);e.Set(11,it.P[2]);e.Set(21,it.P[3]);if(it.Color!=256)e.Set(62,it.Color.ToString(Inv));add(e);}
   else if(it.Kind=="RECT"){var e=head("LWPOLYLINE",it.Layer,it.Color);e.P.Add(new Pair(100,"AcDbPolyline"));e.P.Add(new Pair(90,"4"));e.P.Add(new Pair(70,"1"));foreach(var v in new[]{new[]{it.P[0],it.P[1]},new[]{it.P[2],it.P[1]},new[]{it.P[2],it.P[3]},new[]{it.P[0],it.P[3]}}){e.P.Add(new Pair(10,f(v[0])));e.P.Add(new Pair(20,f(v[1])));}add(e);}
   else if(it.Kind=="PLINE"){var e=head("LWPOLYLINE",it.Layer,it.Color);e.P.Add(new Pair(100,"AcDbPolyline"));e.P.Add(new Pair(90,it.V.Count.ToString(Inv)));e.P.Add(new Pair(70,it.Closed?"1":"0"));e.P.Add(new Pair(43,f(it.W>0?it.W:0)));foreach(var v in it.V){e.P.Add(new Pair(10,f(v[0])));e.P.Add(new Pair(20,f(v[1])));if(v[2]!=0)e.P.Add(new Pair(42,f(v[2])));}add(e);}
   else if(it.Kind=="POINT"){var e=head("POINT",it.Layer,it.Color);e.P.Add(new Pair(100,"AcDbPoint"));e.P.Add(new Pair(10,f(it.P[0])));e.P.Add(new Pair(20,f(it.P[1])));e.P.Add(new Pair(30,f(it.P[2])));add(e);}
   else if(it.Kind=="CIRCLE"){var e=head("CIRCLE",it.Layer,it.Color);e.P.Add(new Pair(100,"AcDbCircle"));e.P.Add(new Pair(10,f(it.P[0])));e.P.Add(new Pair(20,f(it.P[1])));e.P.Add(new Pair(30,"0"));e.P.Add(new Pair(40,f(it.P[2])));add(e);}
   else if(it.Kind=="RRECT"){double rx0=it.P[0],ry0=it.P[1],rx1=it.P[2],ry1=it.P[3],r=it.P[4];const string bulge="0.41421356";var e=head("LWPOLYLINE",it.Layer,it.Color);e.P.Add(new Pair(100,"AcDbPolyline"));e.P.Add(new Pair(90,"8"));e.P.Add(new Pair(70,"1"));
    foreach(var v in new[]{new[]{rx0+r,ry0,0},new[]{rx1-r,ry0,1},new[]{rx1,ry0+r,0},new[]{rx1,ry1-r,1},new[]{rx1-r,ry1,0},new[]{rx0+r,ry1,1},new[]{rx0,ry1-r,0},new[]{rx0,ry0+r,1}}){e.P.Add(new Pair(10,f(v[0])));e.P.Add(new Pair(20,f(v[1])));if(v[2]>0)e.P.Add(new Pair(42,bulge));}add(e);}
   else if(it.Kind=="DISK"){/* a filled bar: a closed two-arc polyline with its width = the radius (AutoCAD DONUT) */double r=it.P[2];var e=head("LWPOLYLINE",it.Layer,it.Color);e.P.Add(new Pair(100,"AcDbPolyline"));e.P.Add(new Pair(90,"2"));e.P.Add(new Pair(70,"1"));e.P.Add(new Pair(43,f(r)));e.P.Add(new Pair(10,f(it.P[0]-r/2)));e.P.Add(new Pair(20,f(it.P[1])));e.P.Add(new Pair(42,"1"));e.P.Add(new Pair(10,f(it.P[0]+r/2)));e.P.Add(new Pair(20,f(it.P[1])));e.P.Add(new Pair(42,"1"));add(e);}
   else if(it.Kind=="TEXT"){if(string.IsNullOrEmpty(it.Text))continue;var e=textPrototype.Copy(true);e.P.RemoveAll(p=>p.Code==1||p.Code==3||p.Code==62);e.Set(8,it.Layer);e.Set(1,it.Text.Replace("\\","").Replace("{","").Replace("}","").Replace("\n","\\P"));if(it.Rot!=0)e.Set(50,it.Rot*Math.PI/180);e.Set(10,it.P[0]);e.Set(20,it.P[1]);e.Set(40,it.H);e.Set(41,it.W>0?it.W:it.Anchor==7||it.Anchor==1||it.Anchor==4?0:60);e.Set(71,it.Anchor.ToString(Inv));e.Set(7,it.Bold?"A 2.50B":"A 2.0");add(e);}}
  /* line weights of the sample: concrete 0.30, bar marks 0.15, ties, bars and dimensions 0.09, the table 0.25 */
  foreach(var e in output){string l=e.Get(8);int lw=l.EndsWith("CONCRETE")||l=="MARKER"?30:l.EndsWith("BARMARK")?15:l=="SCHEDULE"||l=="CELLBOX"?25:l=="TITLE"||l=="HEAD"||l=="MARK"||l=="LEVEL"||l=="NOTES"||l=="CELLTEXT"||l=="REMARKS"||l==Data||l=="TEXT"?-1:9;if(lw<0)continue;int at=e.P.FindIndex(q=>q.Code==8);e.P.Insert(at+1,new Pair(370,lw.ToString(Inv)));}
  /* the hidden data layer: off and not plotted */
  {var layers=drawing.Records.Where(e=>e.Type=="LAYER").ToList();if(layers.Count>0&&!layers.Any(e=>e.Get(2)==Data)){var rec=new Entity();rec.P.Add(new Pair(0,"LAYER"));rec.P.Add(new Pair(5,(next++).ToString("X")));rec.P.Add(new Pair(330,layers[0].Get(330)));rec.P.Add(new Pair(100,"AcDbSymbolTableRecord"));rec.P.Add(new Pair(100,"AcDbLayerTableRecord"));rec.P.Add(new Pair(2,Data));rec.P.Add(new Pair(70,"0"));rec.P.Add(new Pair(62,"-8"));rec.P.Add(new Pair(6,"Continuous"));rec.P.Add(new Pair(290,"0"));rec.P.Add(new Pair(370,"-3"));string ps=layers[0].Get(390);if(ps!="")rec.P.Add(new Pair(390,ps));
    drawing.Records.Insert(drawing.Records.IndexOf(layers[layers.Count-1])+1,rec);var table=drawing.Records.FirstOrDefault(e=>e.Type=="TABLE"&&e.Get(2)=="LAYER");if(table!=null){int n;if(int.TryParse(table.Get(70).Trim(),out n))table.Set(70,(n+1).ToString(Inv));}}}
  int start=drawing.Records.FindIndex(e=>e.Type=="SECTION"&&e.Get(2)=="ENTITIES"),end=start+1;while(drawing.Records[end].Type!="ENDSEC")end++;drawing.Records.RemoveRange(start+1,end-start-1);drawing.Records.InsertRange(start+1,output);
  foreach(var e in drawing.Records)for(int n=0;n<e.P.Count-1;n++)if(e.P[n].Code==9&&e.P[n].Value=="$HANDSEED")e.P[n+1].Value=next.ToString("X");return drawing;}
 public static void Export(ColumnFormatResult r,string path){string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{r.Drawing.Write(temp);var back=ColumnScheduleReader.Read(File.ReadAllText(temp));if(back.Marks.Count!=r.Groups.Count)throw new Exception("Export verification failed: "+back.Marks.Count+" columns read back, "+r.Groups.Count+" written.");
   for(int g=0;g<r.Groups.Count;g++)for(int l=0;l<r.Schedule.Levels.Count;l++){var a=r.Schedule.Cell(r.Groups[g].Source[0],l);int lb=back.Levels.FindIndex(q=>Math.Abs(Math.Min(q.FromM,q.ToM)-Math.Min(r.Schedule.Levels[l].FromM,r.Schedule.Levels[l].ToM))<1e-6&&Math.Abs(Math.Max(q.FromM,q.ToM)-Math.Max(r.Schedule.Levels[l].FromM,r.Schedule.Levels[l].ToM))<1e-6);var b=lb<0?null:back.Cell(r.Groups[g].Mark,lb);if((a==null)!=(b==null)||a!=null&&a.Signature()!=b.Signature())throw new Exception("Export verification failed at "+r.Groups[g].Mark+", "+r.Schedule.Levels[l].Text+".");}
   if(File.Exists(path))File.Delete(path);File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}}
 /* reading the new template back (for the Bar Bending Schedule): texts and the polylines of each cell */
 internal static ColumnSchedule ReadRevised(List<Entity> all){var s=new ColumnSchedule();var texts=all.Where(e=>e.Type=="MTEXT"||e.Type=="TEXT").Select(e=>new{T=SlabFormatter.Plain(e),X=e.Num(10),Y=e.Num(20),L=e.Get(8)}).ToList();
  var head=texts.First(t=>t.T=="COLUMN MARK");var marks=texts.Where(t=>Math.Abs(t.Y-head.Y)<.01&&t.X>head.X).OrderBy(t=>t.X).ToList();if(marks.Count==0)throw new Exception("No column marks in the revised schedule.");double pitch=marks.Count>1?marks[1].X-marks[0].X:60;foreach(var m in marks)s.Marks.Add(m.T);
  var tos=texts.Where(t=>t.T=="TO"&&t.X<marks[0].X-pitch/2).OrderByDescending(t=>t.Y).ToList();var polys=all.Where(e=>e.Type=="LWPOLYLINE").Select(e=>new{L=e.Get(8),W=e.Num(43),P=ColumnScheduleReader.Points(e)}).ToList();
  var lines=all.Where(e=>e.Type=="LINE").Select(e=>new{L=e.Get(8),A=e.Num(10),B=e.Num(20),C=e.Num(11),D=e.Num(21)}).ToList();
  var orig=texts.FirstOrDefault(t=>t.T=="ORIGINAL MARKS");var pat=texts.FirstOrDefault(t=>t.T=="TIE PATTERN");double lastBottom=pat!=null?pat.Y+4:orig!=null?orig.Y:double.NaN;
  /* the section drawn in a box: bars, main link, other links and crossties (layers with prefix P- in the tie pattern row) */
  Func<double,double,double,double,string,ColumnCell> sketch=(x0,y0,x1,y1,p)=>{var c=new ColumnCell{B=Math.Round((x1-x0)*Scale),H=Math.Round((y1-y0)*Scale)};Func<double[],bool> inBox=v=>v[0]>=x0-.01&&v[0]<=x1+.01&&v[1]>=y0-.01&&v[1]<=y1+.01;
   foreach(var e in all.Where(e=>e.Type=="CIRCLE"&&e.Get(8).StartsWith(p+"BARS-"))){double cx=e.Num(10),cy=e.Num(20);if(!inBox(new[]{cx,cy}))continue;int dia;if(!int.TryParse(e.Get(8).Substring((p+"BARS-").Length),out dia))continue;c.Bars.Add(new ColumnBar{X=Math.Round((cx-x0)*Scale,1),Y=Math.Round((cy-y0)*Scale,1),Dia=dia});}
   foreach(var q in polys.Where(q=>q.L==p+"BARS"&&q.P.Count==2&&q.P.All(inBox))){double cx=(q.P[0][0]+q.P[1][0])/2,cy=q.P[0][1];c.Bars.Add(new ColumnBar{X=Math.Round((cx-x0)*Scale,1),Y=Math.Round((cy-y0)*Scale,1),Dia=(int)Math.Round(q.W*2*Scale)});}
   foreach(var q in polys.Where(q=>(q.L==p+"MAINLINK"||q.L==p+"LINKS")&&q.P.Count>=4&&q.P.All(inBox)).OrderByDescending(q=>q.L==p+"MAINLINK"))c.Links.Add(new ColumnLink{X0=Math.Round((q.P.Min(v=>v[0])-x0)*Scale),Y0=Math.Round((q.P.Min(v=>v[1])-y0)*Scale),X1=Math.Round((q.P.Max(v=>v[0])-x0)*Scale),Y1=Math.Round((q.P.Max(v=>v[1])-y0)*Scale)});
   foreach(var e in lines.Where(e=>e.L==p+"CROSSTIES")){if(Math.Min(e.A,e.C)<x0-.01||Math.Max(e.A,e.C)>x1+.01||Math.Min(e.B,e.D)<y0-.01||Math.Max(e.B,e.D)>y1+.01)continue;c.Links.Add(new ColumnLink{X0=Math.Round((Math.Min(e.A,e.C)-x0)*Scale),Y0=Math.Round((Math.Min(e.B,e.D)-y0)*Scale),X1=Math.Round((Math.Max(e.A,e.C)-x0)*Scale),Y1=Math.Round((Math.Max(e.B,e.D)-y0)*Scale)});}
   LinksFromPoints(all,c,x0,y0,x1,y1,p);var main=c.Links.Take(1).ToList();c.Links=main.Concat(c.Links.Skip(1).OrderByDescending(l=>l.W*l.H).ThenByDescending(l=>l.W+l.H)).ToList();return c;};
  for(int li=0;li<tos.Count;li++){var to=tos[li];var hi=texts.Where(t=>Math.Abs(t.X-to.X)<.01&&t.Y>to.Y&&t.Y<to.Y+8&&t.T.EndsWith(" M")).OrderBy(t=>t.Y).FirstOrDefault();var lo=texts.Where(t=>Math.Abs(t.X-to.X)<.01&&t.Y<to.Y&&t.Y>to.Y-8&&t.T.EndsWith(" M")).OrderByDescending(t=>t.Y).FirstOrDefault();if(hi==null||lo==null)continue;
   double hv=double.Parse(hi.T.Replace(" M",""),Inv),lv=double.Parse(lo.T.Replace(" M",""),Inv);s.Levels.Add(new ColumnLevel{From=lo.T,To=hi.T,FromM=lv,ToM=hv});int level=s.Levels.Count-1;
   double rowTop=li==0?head.Y-5:(tos[li-1].Y+to.Y)/2,rowBottom=li+1<tos.Count?(to.Y+tos[li+1].Y)/2:(double.IsNaN(lastBottom)?to.Y-80:lastBottom);
   for(int mi=0;mi<marks.Count;mi++){double cx0=marks[mi].X-pitch/2,cx1=marks[mi].X+pitch/2;
    var cellTexts=texts.Where(t=>t.X>cx0&&t.X<cx1&&t.Y<rowTop&&t.Y>rowBottom).OrderByDescending(t=>t.Y).ToList();
    var conc=polys.FirstOrDefault(p=>p.L=="CONCRETE"&&p.P.Count==4&&p.P.All(v=>v[0]>cx0&&v[0]<cx1&&v[1]<rowTop&&v[1]>rowBottom));ColumnCell c=null;
    if(conc!=null)c=sketch(conc.P.Min(v=>v[0]),conc.P.Min(v=>v[1]),conc.P.Max(v=>v[0]),conc.P.Max(v=>v[1]),"");
    else if(cellTexts.Any(t=>t.L=="SIZE")){var pc=polys.FirstOrDefault(p=>p.L=="P-CONCRETE"&&p.P.Count==4&&p.P.All(v=>v[0]>cx0&&v[0]<cx1));if(pc==null)throw new Exception("Column "+s.Marks[mi]+": no tie pattern for "+lo.T+" to "+hi.T+".");c=sketch(pc.P.Min(v=>v[0]),pc.P.Min(v=>v[1]),pc.P.Max(v=>v[0]),pc.P.Max(v=>v[1]),"P-");}
    if(c==null)continue;
    foreach(var t in cellTexts){Match m;
     if(t.L=="Z1MAIN"||t.L=="Z1OTHERS"||t.L=="Z2"){m=Regex.Match(t.T,@"(?:%%[cC]|Ø|#)(\d+)\s*@\s*(\d+)");if(m.Success){c.TieDia=int.Parse(m.Groups[1].Value,Inv);double sp=double.Parse(m.Groups[2].Value,Inv);if(t.L=="Z1MAIN")c.S1Main=sp;else if(t.L=="Z1OTHERS")c.S1Other=sp;else c.S2=sp;}continue;}
     if(t.L=="LO"){m=Regex.Match(t.T,@"Lo\s*=\s*(\d+)");if(m.Success)c.Lo=double.Parse(m.Groups[1].Value,Inv);continue;}
     /* the one-line wording of v6.1 / v6.2 */
     m=Regex.Match(t.T,@"^(?:%%[cC]|Ø|#)(\d+)\s*@\s*(\d+)(?:\s*/\s*(\d+))?(?:\s*O\.C\.)?\s*\(Lo\s*=\s*(\d+)\)");if(m.Success){c.TieDia=int.Parse(m.Groups[1].Value,Inv);c.S1Main=double.Parse(m.Groups[2].Value,Inv);c.S1Other=m.Groups[3].Success?double.Parse(m.Groups[3].Value,Inv):c.S1Main;c.Lo=double.Parse(m.Groups[4].Value,Inv);}
     m=Regex.Match(t.T,@"^(?:%%[cC]|Ø|#)\d+\s*@\s*(\d+)(?:\s*O\.C\.)?\s*REST");if(m.Success)c.S2=double.Parse(m.Groups[1].Value,Inv);
     m=Regex.Match(t.T,@"^(\S*)\s*(?:fy\s+(\d+))?\s*cover\s+(\d+)");if(m.Success){c.Concrete=m.Groups[1].Value=="-"?"":m.Groups[1].Value;c.Fy=m.Groups[2].Success?double.Parse(m.Groups[2].Value,Inv):0;c.Cover=double.Parse(m.Groups[3].Value,Inv);}}
    s.Cells[ColumnSchedule.Key(s.Marks[mi],level)]=c;}}
  for(int mi=0;mi<marks.Count;mi++){var o=texts.FirstOrDefault(t=>t.L=="ORIGINALS"&&Math.Abs(t.X-marks[mi].X)<.01);if(o!=null)s.Originals[s.Marks[mi]]=o.T;}
  if(s.Cells.Count==0)throw new Exception("No column sections in the revised schedule.");return s;}
 /* points along a polyline segment with a bulge (an arc), for the preview */
 static List<double[]> Bulge(double x0,double y0,double x1,double y1,double b){var list=new List<double[]>{new[]{x0,y0}};if(Math.Abs(b)<1e-9)return list;double th=4*Math.Atan(b),c=Math.Sqrt((x1-x0)*(x1-x0)+(y1-y0)*(y1-y0));if(c<1e-9)return list;double r=c/(2*Math.Sin(th/2)),mx=(x0+x1)/2,my=(y0+y1)/2,h=r*Math.Cos(th/2);double nx=-(y1-y0)/c,ny=(x1-x0)/c;double cx=mx+nx*h,cy=my+ny*h;double a0=Math.Atan2(y0-cy,x0-cx);int n=12;for(int k=1;k<n;k++){double a=a0+th*k/n;list.Add(new[]{cx+Math.Abs(r)*Math.Cos(a),cy+Math.Abs(r)*Math.Sin(a)});}return list;}
 /* preview in the app: the same primitives on white paper */
 public static Bitmap Render(ColumnSheet sh,float pxPerMm=4){int w=(int)((sh.Right-sh.Left+16)*pxPerMm),h=(int)((sh.Top-sh.Bottom+8)*pxPerMm);w=Math.Min(w,30000);h=Math.Min(h,20000);var bmp=new Bitmap(Math.Max(200,w),Math.Max(200,h));
  using(var g=Graphics.FromImage(bmp)){g.Clear(Color.White);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;Func<double,double,PointF> xy=(x,y)=>new PointF((float)((x-sh.Left+8)*pxPerMm),(float)((sh.Top-y+4)*pxPerMm));
   Func<int,Color> col=c=>c==1?Color.FromArgb(200,40,40):c==5?Color.FromArgb(30,90,200):c==3?Color.FromArgb(20,140,70):c==4?Color.FromArgb(0,140,160):c==8?Color.FromArgb(120,120,120):c==6?Color.FromArgb(190,40,170):Color.FromArgb(30,30,30);
   foreach(var it in sh.Items){if(it.Kind=="LINE")using(var p=new Pen(col(it.Color),1f))g.DrawLine(p,xy(it.P[0],it.P[1]),xy(it.P[2],it.P[3]));
    else if(it.Kind=="RECT"){var a=xy(it.P[0],it.P[3]);var b=xy(it.P[2],it.P[1]);using(var p=new Pen(col(it.Color),it.Layer.EndsWith("CONCRETE")?1.5f:.9f))g.DrawRectangle(p,a.X,a.Y,b.X-a.X,b.Y-a.Y);}
    else if(it.Kind=="PLINE"){var pts=new List<PointF>();for(int i=0;i<it.V.Count;i++){var a=it.V[i];if(!it.Closed&&i==it.V.Count-1){pts.Add(xy(a[0],a[1]));break;}var b=it.V[(i+1)%it.V.Count];foreach(var q in Bulge(a[0],a[1],b[0],b[1],a[2]))pts.Add(xy(q[0],q[1]));}if(it.Closed&&pts.Count>0)pts.Add(pts[0]);
     if(it.W>0){/* a donut (filled dot) */var cx=(it.V[0][0]+it.V[1][0])/2;var cy=(it.V[0][1]+it.V[1][1])/2;double rr=Math.Sqrt(Math.Pow(it.V[1][0]-it.V[0][0],2)+Math.Pow(it.V[1][1]-it.V[0][1],2))/2+it.W/2;var c=xy(cx,cy);float r=(float)(rr*pxPerMm);g.FillEllipse(Brushes.Black,c.X-r,c.Y-r,2*r,2*r);}
     else if(pts.Count>1)using(var pen=new Pen(Color.FromArgb(30,30,30),.9f))g.DrawLines(pen,pts.ToArray());}
    else if(it.Kind=="CIRCLE"){var c=xy(it.P[0],it.P[1]);float r=(float)(it.P[2]*pxPerMm);using(var p=new Pen(Color.FromArgb(30,30,30),1f))g.DrawEllipse(p,c.X-r,c.Y-r,2*r,2*r);}
    else if(it.Kind=="RRECT"){var a=xy(it.P[0],it.P[3]);var b=xy(it.P[2],it.P[1]);float r=(float)(it.P[4]*pxPerMm),d=2*r;using(var path=new GraphicsPath()){path.AddArc(a.X,a.Y,d,d,180,90);path.AddArc(b.X-d,a.Y,d,d,270,90);path.AddArc(b.X-d,b.Y-d,d,d,0,90);path.AddArc(a.X,b.Y-d,d,d,90,90);path.CloseFigure();using(var p=new Pen(Color.FromArgb(30,30,30),.9f))g.DrawPath(p,path);}}
    else if(it.Kind=="DISK"){var c=xy(it.P[0],it.P[1]);float r=(float)(it.P[2]*pxPerMm);using(var b=new SolidBrush(col(it.Color)))g.FillEllipse(b,c.X-r,c.Y-r,2*r,2*r);}
    else if(it.Kind=="TEXT"&&!string.IsNullOrEmpty(it.Text)&&it.Layer!=Data){string t=it.Text.Replace("%%C","Ø").Replace("%%c","Ø");using(var f=new Font("Arial",(float)(it.H*pxPerMm*.95),it.Bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel)){float wrapPx=(float)(it.W*pxPerMm);var sz=wrapPx>0?g.MeasureString(t,f,(int)wrapPx):g.MeasureString(t,f);var p=xy(it.P[0],it.P[1]);int a=it.Anchor;float tw=wrapPx>0?wrapPx:sz.Width;
     float dx=a==2||a==5||a==8?-tw/2:a==3||a==6||a==9?-tw:0,dy=a>=4&&a<=6?-sz.Height/2:a>=7?-sz.Height:0;var st=g.Save();g.TranslateTransform(p.X,p.Y);if(it.Rot!=0)g.RotateTransform(-(float)it.Rot);
     using(var fmt=new StringFormat{Alignment=a==2||a==5||a==8?StringAlignment.Center:a==3||a==6||a==9?StringAlignment.Far:StringAlignment.Near})g.DrawString(t,f,Brushes.Black,new RectangleF(dx,dy,tw+2,sz.Height+4),fmt);g.Restore(st);}}}}
  return bmp;}
 /* bars of the Bar Bending Schedule (Column) from a column schedule DXF: main bars per level with a lap into the level above (90° hook
    at the top of the column), the outer hoop at the Z1 main spacing and the inner links at the Z1 other spacing within Lo at each end,
    all at the rest spacing between; the clear height is taken as the level height */
 public static List<CutPiece> BbsPieces(ColumnSchedule s,CutSettings set,List<string> notes){var list=new List<CutPiece>();var opts=new EstimateOptions{LengthConfig=set.Config,Cover=set.Cover,Hook=set.HookFactor,Fc=set.Config.Input.Fc,GradeSmall=set.Config.GradeSmall,Grade=set.Config.GradeLarge};
  var used=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  foreach(var mark in s.Marks){string originals;int qty=1;if(s.Originals.TryGetValue(mark,out originals)&&!string.IsNullOrWhiteSpace(originals))qty=Math.Max(1,originals.Split(new[]{','},StringSplitOptions.RemoveEmptyEntries).Count(x=>x.Trim()!=""));
   var levels=Enumerable.Range(0,s.Levels.Count).Where(l=>s.Cell(mark,l)!=null).OrderBy(l=>Math.Min(s.Levels[l].FromM,s.Levels[l].ToM)).ToList();int n=0;Func<string> next=()=>{string k;do{n++;k="C:"+mark+"-"+n.ToString(Inv);}while(!used.Add(k));return k;};
   for(int k=0;k<levels.Count;k++){int l=levels[k];var c=s.Cell(mark,l);var lv=s.Levels[l];double h=lv.Height;bool topLevel=k==levels.Count-1;string where=Math.Min(lv.FromM,lv.ToM).ToString("0.0##",Inv)+" to "+Math.Max(lv.FromM,lv.ToM).ToString("0.0##",Inv)+" m";
    foreach(var bar in c.Counts){double lap=BeamCutting.SpliceLap(set,bar.Key,false),hook=set.Config.Rounded(set.Config.Values(bar.Key,false).Cut90)/1000;double len=topLevel?h-set.Cover/1000+hook:h+lap;
     list.Add(new CutPiece{Group="Column",Line="Column "+mark,BarMark=next(),Location="Column longitudinal bars, "+where+(topLevel?" (top, 90° hook)":" (lap "+(lap*1000).ToString("0",Inv)+" mm into the level above)"),Diameter=bar.Key,PerLine=bar.Value,Lines=qty,Length=len,Shape=topLevel?"90° hook one end":"Straight",SecB=c.B,SecH=c.H});}
    if(c.TieDia>0&&c.Links.Count>0){double lo=Math.Min(c.Lo/1000,h/2),s1=c.S1Main/1000,s1o=c.S1Other/1000,s2=Math.Max(c.S2,1)/1000;Func<double,int> sets=sp=>{int z=2*((int)Math.Floor(lo/sp+1e-9)+1);double mid=h-2*lo;return z+(mid>1e-6?Math.Max(0,(int)Math.Ceiling(mid/s2-1e-9)-1):0);};
     for(int i=0;i<c.Links.Count;i++){var lk=c.Links[i];bool tie=lk.W<1||lk.H<1;/* open extra ties: an inner closed link becomes two crossties along its long sides */int legs=1;if(s.OpenExtra&&i>0&&!tie){legs=2;lk=lk.W>=lk.H?new ColumnLink{X0=lk.X0,Y0=lk.Y0,X1=lk.X1,Y1=lk.Y0}:new ColumnLink{X0=lk.X0,Y0=lk.Y0,X1=lk.X0,Y1=lk.Y1};tie=true;}double cut;try{var parts=HookGeometry.TieParts(tie?c.B:lk.W+2*set.Cover,tie?Math.Max(lk.W,lk.H)+2*set.Cover:lk.H+2*set.Cover,c.TieDia,tie?3:2,opts);cut=tie?parts.AdditionalLeg:parts.MainHoop;}catch(Exception ex){if(notes!=null)notes.Add("Column "+mark+": "+ex.Message);continue;}
      int count=sets(i==0?s1:s1o)*legs;list.Add(new CutPiece{Group="Column",Line="Column "+mark,BarMark=next(),Location="Column "+(tie?"crossties":i==0?"hoops":"inner links")+", "+where+(tie?" ("+Math.Max(lk.W,lk.H).ToString("0",Inv)+" long)":" ("+lk.W.ToString("0",Inv)+" x "+lk.H.ToString("0",Inv)+" outside, closed hoop)"),Diameter=c.TieDia,PerLine=count,Lines=qty,Length=cut,Shape=tie?"Crosstie 135° / 90°":"Hoop, 135° hooks",SecB=tie?c.B:lk.W+2*set.Cover,SecH=tie?Math.Max(lk.W,lk.H)+2*set.Cover:lk.H+2*set.Cover});}}}}
  if(notes!=null&&list.Count>0)notes.Add("Column bars from the schedule DXF: clear height taken as the level height; footing dowels and beam-column joint hoops are not included.");
  return list;}
}
}
