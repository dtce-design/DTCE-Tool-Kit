using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.IO;using System.Linq;using System.Text;using System.Text.RegularExpressions;
namespace BeamScheduleFormatter {
/* Column Schedule Formatter (owner, 22 November 2026: "can you create a column schedule formatter based on these dxf file from autocad,
   gawin mong presentable. tapos yung output dxf file will be used naman sa bbs column"; "gawan mo ng panibagong template").
   Reads the column and wall schedule exported from STAAD RCDC / AutoCAD (one column per mark, one band per level range: section with
   bars and links, Z1 / Z2 tie spacing, concrete, fy, cover and confining zone), groups identical columns, and draws a new schedule
   template (SCHEDULE OF COLUMNS). The revised DXF is read back by the same reader, so it loads in the Bar Bending Schedule (Column). */
class ColumnBar {public double X,Y;public int Dia;}
class ColumnLink {public double X0,Y0,X1,Y1;/* outside of the link (mm from the bottom-left corner of the section) */public double W{get{return X1-X0;}}public double H{get{return Y1-Y0;}}}
class ColumnLevel {public string From="",To="";public double FromM,ToM;public double Height{get{return Math.Abs(ToM-FromM);}}public string Text{get{return F(FromM)+" M TO "+F(ToM)+" M";}}static string F(double v){return v.ToString("0.0##",CultureInfo.InvariantCulture);}}
class ColumnCell {
 public double B,H;public List<ColumnBar> Bars=new List<ColumnBar>();public List<ColumnLink> Links=new List<ColumnLink>();public int TieDia;public double S1Main,S1Other,S2,Lo;public double Cover=40;public string Concrete="";public double Fy;
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 public Dictionary<int,int> Counts{get{return Bars.GroupBy(b=>b.Dia).OrderByDescending(g=>g.Key).ToDictionary(g=>g.Key,g=>g.Count());}}
 public string MainText{get{return string.Join(" + ",Counts.Select(k=>k.Value+"-%%C"+k.Key));}}
 public string TieText{get{return TieDia<=0?"--":"%%C"+TieDia+" @ "+S1Main.ToString("0",Inv)+(Math.Abs(S1Other-S1Main)>.5&&S1Other>0?" / "+S1Other.ToString("0",Inv):"")+" (Lo = "+Lo.ToString("0",Inv)+")";}}
 public string RestText{get{return TieDia<=0?"":"%%C"+TieDia+" @ "+S2.ToString("0",Inv)+" REST";}}
 public string Signature(){return Signature(true);}
 public string Signature(bool withLo){var b=new StringBuilder();b.Append(B.ToString("0",Inv)).Append("x").Append(H.ToString("0",Inv)).Append("|");foreach(var bar in Bars.OrderBy(q=>Math.Round(q.Y)).ThenBy(q=>Math.Round(q.X)))b.Append(Math.Round(bar.X/5)*5).Append(",").Append(Math.Round(bar.Y/5)*5).Append(":").Append(bar.Dia).Append(";");b.Append("|");foreach(var l in Links.OrderBy(q=>Math.Round(q.X0)).ThenBy(q=>Math.Round(q.Y0)).ThenBy(q=>q.W))b.Append(Math.Round(l.X0/5)*5).Append(",").Append(Math.Round(l.Y0/5)*5).Append(",").Append(Math.Round(l.W/5)*5).Append(",").Append(Math.Round(l.H/5)*5).Append(";");
  return b.Append("|").Append(TieDia).Append("@").Append(S1Main).Append("/").Append(S1Other).Append("/").Append(S2).Append("|").Append(withLo?Lo:0).Append("|").Append(Cover).Append("|").Append(Concrete).Append("|").Append(Fy).ToString();}}
class ColumnSchedule {public List<ColumnLevel> Levels=new List<ColumnLevel>();public List<string> Marks=new List<string>();public Dictionary<string,ColumnCell> Cells=new Dictionary<string,ColumnCell>(StringComparer.OrdinalIgnoreCase);
 public static string Key(string mark,int level){return mark+"|"+level;}
 public ColumnCell Cell(string mark,int level){ColumnCell c;return Cells.TryGetValue(Key(mark,level),out c)?c:null;}
 public Dictionary<string,string> Originals=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);}
class ColumnGroup {public string Mark;public List<string> Source=new List<string>();public string Originals{get{return string.Join(", ",Source);}}}
class ColumnFormatResult {public ColumnSchedule Schedule;public List<ColumnGroup> Groups;public Dxf Drawing;public ColumnSheet Sheet;public int SourceCount;}

static class ColumnScheduleReader {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 static string Text(Entity e){return SlabFormatter.Plain(e);}
 internal static List<double[]> Points(Entity e){var list=new List<double[]>();double x=0;bool hasX=false;foreach(var p in e.P){double v;if(!double.TryParse(p.Value,NumberStyles.Float,Inv,out v))continue;if(p.Code==10){x=v;hasX=true;}else if(p.Code==20&&hasX){list.Add(new[]{x,v});hasX=false;}}return list;}
 internal static int Dia(string s){var m=Regex.Match(s??"",@"(?:#|%%[cC]|Ø|DIA\.?\s*)(\d+)");return m.Success?int.Parse(m.Groups[1].Value,Inv):0;}
 static double Num(string s){var m=Regex.Match(s??"",@"-?\d+(\.\d+)?");return m.Success?double.Parse(m.Value,Inv):double.NaN;}
 public static ColumnSchedule Read(string raw){var d=Dxf.Parse(raw);var all=d.ModelEntities();if(all.Any(e=>(e.Type=="TEXT"||e.Type=="MTEXT")&&(Text(e)=="SCHEDULE OF COLUMNS"||Text(e)=="COLUMN SCHEDULE")))return ColumnFormatter.ReadRevised(all);return ReadRcdc(d,all);}
 /* POLYLINE (old style) vertices follow the POLYLINE record as VERTEX records */
 static List<KeyValuePair<Entity,List<double[]>>> OldPolylines(Dxf d){var list=new List<KeyValuePair<Entity,List<double[]>>>();bool inside=false;Entity current=null;List<double[]> pts=null;
  foreach(var e in d.Records){if(e.Type=="SECTION"&&e.Get(2)=="ENTITIES"){inside=true;continue;}if(!inside)continue;if(e.Type=="ENDSEC")break;
   if(e.Type=="POLYLINE"){current=e;pts=new List<double[]>();continue;}if(e.Type=="VERTEX"&&current!=null){pts.Add(new[]{e.Num(10),e.Num(20)});continue;}if(e.Type=="SEQEND"&&current!=null){list.Add(new KeyValuePair<Entity,List<double[]>>(current,pts));current=null;}}return list;}
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
  var linkPolys=all.Where(e=>e.Type=="LWPOLYLINE"&&e.Get(8).IndexOf("Link",StringComparison.OrdinalIgnoreCase)>=0).Select(Points).ToList();
  var markers=OldPolylines(d).Where(p=>p.Key.Get(8).IndexOf("Reinforcement",StringComparison.OrdinalIgnoreCase)>=0).Select(p=>new{Closed=(((int)p.Key.Num(70))&1)==1,P=p.Value}).ToList();
  var dims=all.Where(e=>e.Type=="DIMENSION").Select(e=>new{M=e.Num(42),X1=e.Num(13),Y1=e.Num(23),X2=e.Num(14),Y2=e.Num(24),T=e.Get(1)}).ToList();
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
class SheetItem {public string Kind,Text,Layer="0";public double[] P;public double H,W;public int Anchor=5;public bool Bold;public int Color=256;}
class ColumnSheet {public List<SheetItem> Items=new List<SheetItem>();public double Left,Top,Right,Bottom;
 public void Line(double x0,double y0,double x1,double y1,string layer="SCHEDULE",int color=256){Items.Add(new SheetItem{Kind="LINE",P=new[]{x0,y0,x1,y1},Layer=layer,Color=color});}
 public void Rect(double x0,double y0,double x1,double y1,string layer,int color=256){Items.Add(new SheetItem{Kind="RECT",P=new[]{x0,y0,x1,y1},Layer=layer,Color=color});}
 public void Disk(double x,double y,double r,string layer,int color){Items.Add(new SheetItem{Kind="DISK",P=new[]{x,y,r},Layer=layer,Color=color});}
 public void Circle(double x,double y,double r,string layer){Items.Add(new SheetItem{Kind="CIRCLE",P=new[]{x,y,r},Layer=layer});}
 public void RoundRect(double x0,double y0,double x1,double y1,double r,string layer){Items.Add(new SheetItem{Kind="RRECT",P=new[]{x0,y0,x1,y1,r},Layer=layer});}
 public void Text(string t,double x,double y,double h,int anchor=5,bool bold=false,string layer="TEXT",double wrap=0){Items.Add(new SheetItem{Kind="TEXT",Text=t,P=new[]{x,y},H=h,Anchor=anchor,Bold=bold,Layer=layer,W=wrap});}}

static class ColumnFormatter {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 /* identical columns (every level the same) share one new mark */
 public static List<ColumnGroup> Group(ColumnSchedule s,string prefix,bool merge){prefix=(prefix??"").Trim().ToUpperInvariant();if(!Regex.IsMatch(prefix,@"^[A-Z0-9]{1,8}$"))throw new Exception("Enter the column prefix, e.g. C or 2C.");
  var groups=new List<ColumnGroup>();var lookup=new Dictionary<string,ColumnGroup>();foreach(var m in s.Marks){string key=string.Join("#",Enumerable.Range(0,s.Levels.Count).Select(l=>{var c=s.Cell(m,l);return c==null?"-":c.Signature(false);}));if(key.Replace("#","").Replace("-","")=="")continue;ColumnGroup g;if(!merge||!lookup.TryGetValue(key,out g)){g=new ColumnGroup();groups.Add(g);lookup[key]=g;}g.Source.Add(m);}
  /* a different confining zone Lo alone does not make another mark (owner, 22 November 2026: "minerge yung identical column pero di
     naman nag assign ng single column mark"): the group takes the largest Lo of its columns at each level */
  foreach(var g in groups)for(int l=0;l<s.Levels.Count;l++){var cells=g.Source.Select(m=>s.Cell(m,l)).Where(c=>c!=null).ToList();if(cells.Count<2)continue;double lo=cells.Max(c=>c.Lo);foreach(var c in cells)c.Lo=lo;}
  for(int n=0;n<groups.Count;n++)groups[n].Mark=prefix+"-"+(n+1).ToString("000",Inv);return groups;}
 /* an RCDC schedule as the grouped marks (the Bill of Quantities and the Bar Bending Schedule take it this way) */
 internal static ColumnSchedule Merged(ColumnSchedule s,string prefix){var groups=Group(s,prefix,true);var merged=new ColumnSchedule{Levels=s.Levels};foreach(var g in groups){merged.Marks.Add(g.Mark);merged.Originals[g.Mark]=g.Originals;for(int l=0;l<s.Levels.Count;l++){var c=s.Cell(g.Source[0],l);if(c!=null)merged.Cells[ColumnSchedule.Key(g.Mark,l)]=c;}}return merged;}
 const double Scale=20;/* sections at 1:20 on paper (mm) */
 /* one section sketch: concrete, the main link (magenta) and the other links / crossties (cyan), bars, sizes; prefix "P-" for the
    tie pattern row of the ACI schedule */
 /* one section sketch, monochrome (owner, 22 November 2026: "dapat monochrome lang", "gawin yung original bar legend", "ayusin yung
    stirrups"): every link with rounded corners and its own 135° hooks, the main link (outer hoop) heavier, crossties with a 135° and a
    90° hook; the bars as circles marked by size (as RCDC: a line, a dot, ...) with the bar legend beside the section. The bar size
    is kept in the layer (BARS-20), the links on MAINLINK / LINKS / CROSSTIES; prefix "P-" for the tie pattern row of the ACI schedule. */
 static void BarSymbol(ColumnSheet sh,double x,double y,double r,int rank,string layer,string markLayer){sh.Circle(x,y,r,layer);switch(rank%4){case 0:sh.Line(x,y-r,x,y+r,markLayer);break;case 1:sh.Disk(x,y,r*.4,markLayer,256);break;case 2:sh.Line(x-r,y,x+r,y,markLayer);break;default:sh.Line(x-r*.7,y-r*.7,x+r*.7,y+r*.7,markLayer);sh.Line(x-r*.7,y+r*.7,x+r*.7,y-r*.7,markLayer);break;}}
 static void Sketch(ColumnSheet sh,ColumnCell c,double x0,double y0,string p){double w=c.B/Scale,h=c.H/Scale;sh.Rect(x0,y0,x0+w,y0+h,p+"CONCRETE");
  double tie=c.TieDia>0?c.TieDia:10,rb=Math.Max(.5,(2*tie+tie/2)/Scale),tail=Math.Max(1.1,Math.Max(6*tie,75)/Scale);
  for(int i=0;i<c.Links.Count;i++){var l=c.Links[i];double ax=x0+l.X0/Scale,ay=y0+l.Y0/Scale,bx=x0+l.X1/Scale,by=y0+l.Y1/Scale;
   if(l.W<1||l.H<1){sh.Line(ax,ay,bx,by,p+"CROSSTIES");if(l.W<1){sh.Line(ax,by,ax+tail*.7,by-tail*.7,p+"HOOKS");sh.Line(ax,ay,ax+tail*.6,ay,p+"HOOKS");}else{sh.Line(ax,ay,ax+tail*.7,ay+tail*.7,p+"HOOKS");sh.Line(bx,ay,bx,ay+tail*.6,p+"HOOKS");}continue;}
   double rr=Math.Min(rb,Math.Min(bx-ax,by-ay)/3);sh.RoundRect(ax,ay,bx,by,rr,i==0?p+"MAINLINK":p+"LINKS");
   /* the 135° hooks at the top-left corner, into the core */double k=rr*.6;sh.Line(ax+k,by-k*.3,ax+k+tail*.7,by-k*.3-tail*.7,p+"HOOKS");sh.Line(ax+k*.3,by-k,ax+k*.3+tail*.7,by-k-tail*.7,p+"HOOKS");}
  var sizes=c.Bars.Select(q=>q.Dia).Distinct().OrderByDescending(d=>d).ToList();
  foreach(var b in c.Bars){int rank=sizes.IndexOf(b.Dia);double r=Math.Max(.75,b.Dia/2.0/Scale);BarSymbol(sh,x0+b.X/Scale,y0+b.Y/Scale,r,rank,p+"BARS-"+b.Dia,p+"BARMARK");}
  /* the bar legend beside the section */for(int k=0;k<sizes.Count;k++){double lx=x0+w+3.2,ly=y0+h*.3+k*3.2;BarSymbol(sh,lx,ly,.9,k,p+"LEGEND",p+"LEGEND");sh.Text("%%C"+sizes[k],lx+1.6,ly,1.9,4,false,p+"LEGEND");}
  sh.Text(c.B.ToString("0",Inv),x0+w/2,y0-1,2,2);sh.Text(c.H.ToString("0",Inv),x0-1.5,y0+h/2,2,6);}
 /* the tie zones in a small table (owner, 22 November 2026: "clearly makita if ano yung main link, z1 main, z1 others etc") */
 static double TieTable(ColumnSheet sh,ColumnCell c,double x,double w,double yTop,bool aci){double cw=(w-4)/3,x0=x+2,rh=5;string[] heads={"Z1 MAIN LINK","Z1 OTHERS","Z2 LINKS"};string bar=(aci?"#":"%%C")+c.TieDia+" @ ",oc=aci?" O.C.":"";
  string[] vals={bar+c.S1Main.ToString("0",Inv)+oc,bar+c.S1Other.ToString("0",Inv)+oc,bar+c.S2.ToString("0",Inv)+oc};string[] layers={"Z1MAIN","Z1OTHERS","Z2"};
  sh.Rect(x0,yTop-2*rh,x0+3*cw,yTop,"TIETABLE");sh.Line(x0,yTop-rh,x0+3*cw,yTop-rh,"TIETABLE");for(int k=1;k<3;k++)sh.Line(x0+k*cw,yTop,x0+k*cw,yTop-2*rh,"TIETABLE");
  for(int k=0;k<3;k++){sh.Text(heads[k],x0+k*cw+cw/2,yTop-rh/2,1.6,5,true);sh.Text(vals[k],x0+k*cw+cw/2,yTop-rh*1.5,1.9,5,false,layers[k]);}
  sh.Text("Lo = "+c.Lo.ToString("0",Inv)+" (Z1 AT EACH END)",x+w/2,yTop-2*rh-2.4,1.8,5,false,"LO");return 2*rh+4.8;}
 static string LayoutKey(ColumnCell c){return c.B+"x"+c.H+"|"+string.Join(";",c.Bars.Select(b=>Math.Round(b.X)+","+Math.Round(b.Y)+":"+b.Dia))+"|"+string.Join(";",c.Links.Select(l=>l.X0+","+l.Y0+","+l.X1+","+l.Y1));}
 public static ColumnSheet Layout(ColumnSchedule s,List<ColumnGroup> groups,string title,bool aci=false){var sh=new ColumnSheet();
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
  sh.Text("NOTES: SECTIONS AT 1:20. Z1 MAIN LINK = THE OUTER HOOP (HEAVY LINE) WITHIN Lo; Z1 OTHERS = THE INNER LINKS AND CROSSTIES WITHIN Lo; Z2 LINKS = ALL LINKS OUTSIDE Lo. Lo = CONFINING ZONE AT EACH END OF THE CLEAR HEIGHT. ALL HOOPS AND LINKS WITH 135° HOOKS.",0,y-4,1.8,7,false,"TEXT",width);
  sh.Left=0;sh.Right=width;sh.Top=top+16;sh.Bottom=y-8;return sh;}
 public static ColumnFormatResult Build(string raw,string prefix,string title,bool merge,bool aci=false){var s=ColumnScheduleReader.Read(raw);var groups=Group(s,prefix,merge);if(groups.Count==0)throw new Exception("No columns to format.");var sheet=Layout(s,groups,title,aci);if(aci)AciDetailing.Apply(sheet);return new ColumnFormatResult{Schedule=s,Groups=groups,Sheet=sheet,Drawing=ToDxf(sheet,s,groups),SourceCount=s.Marks.Count};}
 /* the DXF: header, tables and styles of the slab template; entities drawn new */
 internal static Dxf ToDxf(ColumnSheet sh,ColumnSchedule s,List<ColumnGroup> groups){Dxf drawing;using(var stream=typeof(ColumnFormatter).Assembly.GetManifestResourceStream("SlabTemplate.dxf")){if(stream==null)throw new Exception("Template resource missing.");using(var reader=new StreamReader(stream,Encoding.UTF8,true))drawing=Dxf.Parse(reader.ReadToEnd());}
  var original=drawing.ModelEntities();var linePrototype=original.First(e=>e.Type=="LINE");var textPrototype=original.First(e=>e.Type=="MTEXT");string owner=linePrototype.Get(330);
  long next=1;foreach(var e in drawing.Records){long handle;if(long.TryParse(e.Get(5),NumberStyles.HexNumber,Inv,out handle))next=Math.Max(next,handle+1);}var output=new List<Entity>();
  Action<Entity> add=e=>{e.P.RemoveAll(p=>p.Code==5||p.Code==102);e.P.Insert(1,new Pair(5,(next++).ToString("X")));output.Add(e);};
  Func<string,string,int,Entity> head=(type,layer,color)=>{var e=new Entity();e.P.Add(new Pair(0,type));if(owner!="")e.P.Add(new Pair(330,owner));e.P.Add(new Pair(100,"AcDbEntity"));e.P.Add(new Pair(8,layer));if(color!=256)e.P.Add(new Pair(62,color.ToString(Inv)));return e;};
  Func<double,string> f=v=>v.ToString("0.####",Inv);
  foreach(var it in sh.Items){if(it.Kind=="LINE"){var e=linePrototype.Copy(true);e.Set(8,it.Layer);e.Set(10,it.P[0]);e.Set(20,it.P[1]);e.Set(11,it.P[2]);e.Set(21,it.P[3]);if(it.Color!=256)e.Set(62,it.Color.ToString(Inv));add(e);}
   else if(it.Kind=="RECT"){var e=head("LWPOLYLINE",it.Layer,it.Color);e.P.Add(new Pair(100,"AcDbPolyline"));e.P.Add(new Pair(90,"4"));e.P.Add(new Pair(70,"1"));if(it.Layer.EndsWith("MAINLINK"))e.P.Add(new Pair(43,"0.3"));foreach(var v in new[]{new[]{it.P[0],it.P[1]},new[]{it.P[2],it.P[1]},new[]{it.P[2],it.P[3]},new[]{it.P[0],it.P[3]}}){e.P.Add(new Pair(10,f(v[0])));e.P.Add(new Pair(20,f(v[1])));}add(e);}
   else if(it.Kind=="CIRCLE"){var e=head("CIRCLE",it.Layer,it.Color);e.P.Add(new Pair(100,"AcDbCircle"));e.P.Add(new Pair(10,f(it.P[0])));e.P.Add(new Pair(20,f(it.P[1])));e.P.Add(new Pair(30,"0"));e.P.Add(new Pair(40,f(it.P[2])));add(e);}
   else if(it.Kind=="RRECT"){double rx0=it.P[0],ry0=it.P[1],rx1=it.P[2],ry1=it.P[3],r=it.P[4];const string bulge="0.41421356";var e=head("LWPOLYLINE",it.Layer,it.Color);e.P.Add(new Pair(100,"AcDbPolyline"));e.P.Add(new Pair(90,"8"));e.P.Add(new Pair(70,"1"));if(it.Layer.EndsWith("MAINLINK"))e.P.Add(new Pair(43,"0.3"));
    foreach(var v in new[]{new[]{rx0+r,ry0,0},new[]{rx1-r,ry0,1},new[]{rx1,ry0+r,0},new[]{rx1,ry1-r,1},new[]{rx1-r,ry1,0},new[]{rx0+r,ry1,1},new[]{rx0,ry1-r,0},new[]{rx0,ry0+r,1}}){e.P.Add(new Pair(10,f(v[0])));e.P.Add(new Pair(20,f(v[1])));if(v[2]>0)e.P.Add(new Pair(42,bulge));}add(e);}
   else if(it.Kind=="DISK"){/* a filled bar: a closed two-arc polyline with its width = the radius (AutoCAD DONUT) */double r=it.P[2];var e=head("LWPOLYLINE",it.Layer,it.Color);e.P.Add(new Pair(100,"AcDbPolyline"));e.P.Add(new Pair(90,"2"));e.P.Add(new Pair(70,"1"));e.P.Add(new Pair(43,f(r)));e.P.Add(new Pair(10,f(it.P[0]-r/2)));e.P.Add(new Pair(20,f(it.P[1])));e.P.Add(new Pair(42,"1"));e.P.Add(new Pair(10,f(it.P[0]+r/2)));e.P.Add(new Pair(20,f(it.P[1])));e.P.Add(new Pair(42,"1"));add(e);}
   else if(it.Kind=="TEXT"){if(string.IsNullOrEmpty(it.Text))continue;var e=textPrototype.Copy(true);e.P.RemoveAll(p=>p.Code==1||p.Code==3||p.Code==62);e.Set(8,it.Layer);e.Set(1,it.Text.Replace("\\","").Replace("{","").Replace("}",""));e.Set(10,it.P[0]);e.Set(20,it.P[1]);e.Set(40,it.H);e.Set(41,it.W>0?it.W:it.Anchor==7||it.Anchor==1||it.Anchor==4?0:60);e.Set(71,it.Anchor.ToString(Inv));e.Set(7,it.Bold?"A 2.50B":"A 2.0");add(e);}}
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
   var main=c.Links.Take(1).ToList();c.Links=main.Concat(c.Links.Skip(1).OrderByDescending(l=>l.W*l.H).ThenByDescending(l=>l.W+l.H)).ToList();return c;};
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
 /* preview in the app: the same primitives on white paper */
 public static Bitmap Render(ColumnSheet sh,float pxPerMm=4){int w=(int)((sh.Right-sh.Left+16)*pxPerMm),h=(int)((sh.Top-sh.Bottom+8)*pxPerMm);w=Math.Min(w,30000);h=Math.Min(h,20000);var bmp=new Bitmap(Math.Max(200,w),Math.Max(200,h));
  using(var g=Graphics.FromImage(bmp)){g.Clear(Color.White);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;Func<double,double,PointF> xy=(x,y)=>new PointF((float)((x-sh.Left+8)*pxPerMm),(float)((sh.Top-y+4)*pxPerMm));
   Func<int,Color> col=c=>c==1?Color.FromArgb(200,40,40):c==5?Color.FromArgb(30,90,200):c==3?Color.FromArgb(20,140,70):c==4?Color.FromArgb(0,140,160):c==8?Color.FromArgb(120,120,120):c==6?Color.FromArgb(190,40,170):Color.FromArgb(30,30,30);
   foreach(var it in sh.Items){if(it.Kind=="LINE")using(var p=new Pen(col(it.Color),1f))g.DrawLine(p,xy(it.P[0],it.P[1]),xy(it.P[2],it.P[3]));
    else if(it.Kind=="RECT"){var a=xy(it.P[0],it.P[3]);var b=xy(it.P[2],it.P[1]);using(var p=new Pen(col(it.Color),it.Layer.EndsWith("MAINLINK")?2.2f:it.Layer.EndsWith("CONCRETE")?1.5f:.9f))g.DrawRectangle(p,a.X,a.Y,b.X-a.X,b.Y-a.Y);}
    else if(it.Kind=="CIRCLE"){var c=xy(it.P[0],it.P[1]);float r=(float)(it.P[2]*pxPerMm);using(var p=new Pen(Color.FromArgb(30,30,30),1f))g.DrawEllipse(p,c.X-r,c.Y-r,2*r,2*r);}
    else if(it.Kind=="RRECT"){var a=xy(it.P[0],it.P[3]);var b=xy(it.P[2],it.P[1]);float r=(float)(it.P[4]*pxPerMm),d=2*r;using(var path=new GraphicsPath()){path.AddArc(a.X,a.Y,d,d,180,90);path.AddArc(b.X-d,a.Y,d,d,270,90);path.AddArc(b.X-d,b.Y-d,d,d,0,90);path.AddArc(a.X,b.Y-d,d,d,90,90);path.CloseFigure();using(var p=new Pen(Color.FromArgb(30,30,30),it.Layer.EndsWith("MAINLINK")?2.2f:.9f))g.DrawPath(p,path);}}
    else if(it.Kind=="DISK"){var c=xy(it.P[0],it.P[1]);float r=(float)(it.P[2]*pxPerMm);using(var b=new SolidBrush(col(it.Color)))g.FillEllipse(b,c.X-r,c.Y-r,2*r,2*r);}
    else if(it.Kind=="TEXT"&&!string.IsNullOrEmpty(it.Text)){string t=it.Text.Replace("%%C","Ø").Replace("%%c","Ø");using(var f=new Font("Arial",(float)(it.H*pxPerMm*.95),it.Bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel)){float wrapPx=(float)(it.W*pxPerMm);var sz=wrapPx>0?g.MeasureString(t,f,(int)wrapPx):g.MeasureString(t,f);var p=xy(it.P[0],it.P[1]);float x=p.X,y=p.Y;int a=it.Anchor;if(a==2||a==5||a==8)x-=sz.Width/2;else if(a==3||a==6||a==9)x-=sz.Width;if(a>=4&&a<=6)y-=sz.Height/2;else if(a>=7)y-=sz.Height;if(wrapPx>0)using(var fmt=new StringFormat{Alignment=a==2||a==5||a==8?StringAlignment.Center:a==3||a==6||a==9?StringAlignment.Far:StringAlignment.Near})g.DrawString(t,f,Brushes.Black,new RectangleF(p.X-(a==2||a==5||a==8?wrapPx/2:a==3||a==6||a==9?wrapPx:0),y,wrapPx,sz.Height+4),fmt);else g.DrawString(t,f,Brushes.Black,x,y);}}}}
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
     for(int i=0;i<c.Links.Count;i++){var lk=c.Links[i];bool tie=lk.W<1||lk.H<1;double cut;try{var parts=HookGeometry.TieParts(tie?c.B:lk.W+2*set.Cover,tie?Math.Max(lk.W,lk.H)+2*set.Cover:lk.H+2*set.Cover,c.TieDia,tie?3:2,opts);cut=tie?parts.AdditionalLeg:parts.MainHoop;}catch(Exception ex){if(notes!=null)notes.Add("Column "+mark+": "+ex.Message);continue;}
      int count=sets(i==0?s1:s1o);list.Add(new CutPiece{Group="Column",Line="Column "+mark,BarMark=next(),Location="Column "+(tie?"crossties":i==0?"hoops":"inner links")+", "+where+(tie?" ("+Math.Max(lk.W,lk.H).ToString("0",Inv)+" long)":" ("+lk.W.ToString("0",Inv)+" x "+lk.H.ToString("0",Inv)+" outside, closed hoop)"),Diameter=c.TieDia,PerLine=count,Lines=qty,Length=cut,Shape=tie?"Crosstie 135° / 90°":"Hoop, 135° hooks",SecB=tie?c.B:lk.W+2*set.Cover,SecH=tie?Math.Max(lk.W,lk.H)+2*set.Cover:lk.H+2*set.Cover});}}}}
  if(notes!=null&&list.Count>0)notes.Add("Column bars from the schedule DXF: clear height taken as the level height; footing dowels and beam-column joint hoops are not included.");
  return list;}
}
}
