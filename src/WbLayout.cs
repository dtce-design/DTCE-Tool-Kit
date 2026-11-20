using System;using System.Collections.Generic;using System.Drawing;using System.Globalization;using System.IO;using System.IO.Compression;using System.Linq;using System.Xml;
namespace BeamScheduleFormatter {
/* Look of the POW worksheets (owner, 20 November 2026: "kamukha niyan mismo"): fonts, fills, borders, alignment and number formats
   from styles.xml, and per sheet the column widths, row heights, hidden rows, merged cells, print area and page setup. A sheet's
   layout is read from the workbook file when the sheet is first shown. */
sealed class WbFont{public string Name="Calibri";public float Size=11;public bool Bold,Italic,Underline,Strike;public Color Color=Color.Black;}
sealed class WbBorderSide{public string Style;public Color Color=Color.Black;}
sealed class WbStyle{public string Format="General";public WbFont Font;public Color? Fill;public WbBorderSide Left,Right,Top,Bottom;public string HAlign="general",VAlign="bottom";public bool Wrap,Shrink;public int Indent,Rotation;}
sealed class WbStyles{
 public readonly List<WbStyle> Xf=new List<WbStyle>();
 static readonly Dictionary<int,string> BuiltIn=new Dictionary<int,string>{{0,"General"},{1,"0"},{2,"0.00"},{3,"#,##0"},{4,"#,##0.00"},{9,"0%"},{10,"0.00%"},{11,"0.00E+00"},{12,"# ?/?"},{13,"# ??/??"},{14,"m/d/yyyy"},{15,"d-mmm-yy"},{16,"d-mmm"},{17,"mmm-yy"},{18,"h:mm AM/PM"},{19,"h:mm:ss AM/PM"},{20,"h:mm"},{21,"h:mm:ss"},{22,"m/d/yyyy h:mm"},{37,"#,##0 ;(#,##0)"},{38,"#,##0 ;[Red](#,##0)"},{39,"#,##0.00;(#,##0.00)"},{40,"#,##0.00;[Red](#,##0.00)"},{41,"_(* #,##0_);_(* (#,##0);_(* \"-\"_);_(@_)"},{42,"_(\"$\"* #,##0_);_(\"$\"* (#,##0);_(\"$\"* \"-\"_);_(@_)"},{43,"_(* #,##0.00_);_(* (#,##0.00);_(* \"-\"??_);_(@_)"},{44,"_(\"$\"* #,##0.00_);_(\"$\"* (#,##0.00);_(\"$\"* \"-\"??_);_(@_)"},{45,"mm:ss"},{46,"[h]:mm:ss"},{47,"mmss.0"},{48,"##0.0E+0"},{49,"@"}};
 static readonly string[] Indexed={"000000","FFFFFF","FF0000","00FF00","0000FF","FFFF00","FF00FF","00FFFF","000000","FFFFFF","FF0000","00FF00","0000FF","FFFF00","FF00FF","00FFFF","800000","008000","000080","808000","800080","008080","C0C0C0","808080","9999FF","993366","FFFFCC","CCFFFF","660066","FF8080","0066CC","CCCCFF","000080","FF00FF","FFFF00","00FFFF","800080","800000","008080","0000FF","00CCFF","CCFFFF","CCFFCC","FFFF99","99CCFF","FF99CC","CC99FF","FFCC99","3366FF","33CCCC","99CC00","FFCC00","FF9900","FF6600","666699","969696","003366","339966","003300","333300","993300","993366","333399","333333"};
 public static WbStyles Read(ZipArchive zip){var st=new WbStyles();var theme=ThemeColors(zip);var e=zip.GetEntry("xl/styles.xml");if(e==null){st.Xf.Add(new WbStyle{Font=new WbFont()});return st;}
  var doc=new XmlDocument();using(var s=e.Open())doc.Load(s);var ns=new XmlNamespaceManager(doc.NameTable);ns.AddNamespace("m","http://schemas.openxmlformats.org/spreadsheetml/2006/main");
  var fmts=new Dictionary<int,string>(BuiltIn);foreach(XmlElement f in doc.SelectNodes("//m:numFmts/m:numFmt",ns))fmts[int.Parse(f.GetAttribute("numFmtId"))]=f.GetAttribute("formatCode");
  Func<XmlElement,Color?> color=c=>{if(c==null)return null;if(c.HasAttribute("rgb")){var h=c.GetAttribute("rgb");if(h.Length==8)h=h.Substring(2);int v;if(int.TryParse(h,NumberStyles.HexNumber,null,out v))return Color.FromArgb(255,Color.FromArgb(v));}
   Color baseColor;if(c.HasAttribute("theme")){int t=int.Parse(c.GetAttribute("theme"));if(t>=theme.Count)return null;baseColor=theme[t];}else if(c.HasAttribute("indexed")){int i=int.Parse(c.GetAttribute("indexed"));if(i==64)return Color.Black;if(i>=Indexed.Length)return null;baseColor=Color.FromArgb(255,Color.FromArgb(int.Parse(Indexed[i],NumberStyles.HexNumber)));}else return null;
   double tint=0;if(c.HasAttribute("tint"))tint=double.Parse(c.GetAttribute("tint"),CultureInfo.InvariantCulture);return Tint(baseColor,tint);};
  var fonts=new List<WbFont>();foreach(XmlElement f in doc.SelectNodes("//m:fonts/m:font",ns)){var font=new WbFont();var n=f["name"]??f["rFont"];if(n!=null)font.Name=n.GetAttribute("val");var sz=f["sz"];if(sz!=null)font.Size=float.Parse(sz.GetAttribute("val"),CultureInfo.InvariantCulture);
   font.Bold=On(f["b"]);font.Italic=On(f["i"]);font.Underline=f["u"]!=null&&f["u"].GetAttribute("val")!="none";font.Strike=On(f["strike"]);var col=color(f["color"]);if(col.HasValue)font.Color=col.Value;fonts.Add(font);}
  var fills=new List<Color?>();foreach(XmlElement f in doc.SelectNodes("//m:fills/m:fill",ns)){var p=f["patternFill"];Color? c=null;if(p!=null&&p.GetAttribute("patternType")!=""&&p.GetAttribute("patternType")!="none"){c=color(p["fgColor"]);if(p.GetAttribute("patternType")!="solid"&&c==null)c=color(p["bgColor"]);}else if(f["gradientFill"]!=null){var stop=f["gradientFill"].GetElementsByTagName("color").Cast<XmlElement>().FirstOrDefault();c=color(stop);}fills.Add(c);}
  var borders=new List<WbBorderSide[]>();foreach(XmlElement b in doc.SelectNodes("//m:borders/m:border",ns)){var sides=new WbBorderSide[4];int k=0;foreach(var name in new[]{"left","right","top","bottom"}){var el=b[name]??(name=="left"?b["start"]:name=="right"?b["end"]:null);if(el!=null&&el.GetAttribute("style")!=""){sides[k]=new WbBorderSide{Style=el.GetAttribute("style")};var c=color(el["color"]);if(c.HasValue)sides[k].Color=c.Value;}k++;}borders.Add(sides);}
  foreach(XmlElement x in doc.SelectNodes("//m:cellXfs/m:xf",ns)){var s=new WbStyle();int id;if(int.TryParse(x.GetAttribute("numFmtId"),out id)){string code;s.Format=fmts.TryGetValue(id,out code)?code:"General";}
   if(int.TryParse(x.GetAttribute("fontId"),out id)&&id<fonts.Count)s.Font=fonts[id];else s.Font=fonts.Count>0?fonts[0]:new WbFont();if(int.TryParse(x.GetAttribute("fillId"),out id)&&id<fills.Count)s.Fill=fills[id];
   if(int.TryParse(x.GetAttribute("borderId"),out id)&&id<borders.Count){var b=borders[id];s.Left=b[0];s.Right=b[1];s.Top=b[2];s.Bottom=b[3];}
   var al=x["alignment"];if(al!=null){if(al.GetAttribute("horizontal")!="")s.HAlign=al.GetAttribute("horizontal");if(al.GetAttribute("vertical")!="")s.VAlign=al.GetAttribute("vertical");s.Wrap=al.GetAttribute("wrapText")=="1"||al.GetAttribute("wrapText")=="true";s.Shrink=al.GetAttribute("shrinkToFit")=="1";int.TryParse(al.GetAttribute("indent"),out s.Indent);int.TryParse(al.GetAttribute("textRotation"),out s.Rotation);}
   st.Xf.Add(s);}
  if(st.Xf.Count==0)st.Xf.Add(new WbStyle{Font=fonts.Count>0?fonts[0]:new WbFont()});return st;}
 static bool On(XmlElement e){return e!=null&&(e.GetAttribute("val")==""||e.GetAttribute("val")=="1"||e.GetAttribute("val")=="true");}
 static Color Tint(Color c,double tint){if(tint==0)return c;Func<int,int> f=v=>tint<0?(int)Math.Round(v*(1+tint)):(int)Math.Round(v+(255-v)*tint);return Color.FromArgb(255,f(c.R),f(c.G),f(c.B));}
 static List<Color> ThemeColors(ZipArchive zip){var list=new List<Color>();var e=zip.GetEntry("xl/theme/theme1.xml");if(e==null)return list;var doc=new XmlDocument();using(var s=e.Open())doc.Load(s);
  var scheme=doc.GetElementsByTagName("a:clrScheme").Cast<XmlElement>().FirstOrDefault();if(scheme==null)return list;var map=new Dictionary<string,Color>();
  foreach(XmlElement c in scheme.ChildNodes.OfType<XmlElement>()){var v=c.FirstChild as XmlElement;if(v==null)continue;string hex=v.GetAttribute("val");if(v.LocalName=="sysClr")hex=v.GetAttribute("lastClr");int n;if(int.TryParse(hex,NumberStyles.HexNumber,null,out n))map[c.LocalName]=Color.FromArgb(255,Color.FromArgb(n));}
  /* theme index order: lt1, dk1, lt2, dk2, accent1..6, hlink, folHlink */
  foreach(var k in new[]{"lt1","dk1","lt2","dk2","accent1","accent2","accent3","accent4","accent5","accent6","hlink","folHlink"}){Color c;list.Add(map.TryGetValue(k,out c)?c:Color.Black);}return list;}
}
sealed class WbMerge{public int R1,C1,R2,C2;}
sealed class WbPicture{public int Row,Col;public double RowOff,ColOff;public int Row2,Col2;public double Row2Off,Col2Off;public byte[] Data;public double WidthPt,HeightPt;}
sealed class WbSheetLayout{
 public double DefaultColWidth=8.43,DefaultRowHeight=15;public readonly Dictionary<int,double> ColWidth=new Dictionary<int,double>();public readonly HashSet<int> HiddenCols=new HashSet<int>();
 public readonly Dictionary<int,double> RowHeight=new Dictionary<int,double>();public readonly HashSet<int> HiddenRows=new HashSet<int>();public readonly Dictionary<int,int> RowStyle=new Dictionary<int,int>();public readonly Dictionary<int,int> ColStyle=new Dictionary<int,int>();
 public readonly Dictionary<long,int> CellStyle=new Dictionary<long,int>();public readonly List<WbMerge> Merges=new List<WbMerge>();public readonly Dictionary<long,WbMerge> MergeAt=new Dictionary<long,WbMerge>();
 public int PrintR1=1,PrintC1=1,PrintR2,PrintC2;public bool Landscape,FitToPage,CenterH;public int FitWidth=1,FitHeight=1,Scale=100,Paper=9;public double MarginL=0.7,MarginR=0.7,MarginT=0.75,MarginB=0.75,MarginHeader=0.3,MarginFooter=0.3;
 public readonly List<int> RowBreaks=new List<int>();public readonly List<WbPicture> Pictures=new List<WbPicture>();public string Header="",Footer="";public byte[] FooterPicture,HeaderPicture;public int MaxRow,MaxCol;
 public double Width(int c){if(HiddenCols.Contains(c))return 0;double w;return ColWidth.TryGetValue(c,out w)?w:DefaultColWidth;}
 public double Height(int r){if(HiddenRows.Contains(r))return 0;double h;return RowHeight.TryGetValue(r,out h)?h:DefaultRowHeight;}
 public int StyleAt(int r,int c){int s;if(CellStyle.TryGetValue(WbSheet.Key(r,c),out s))return s;if(RowStyle.TryGetValue(r,out s))return s;if(ColStyle.TryGetValue(c,out s))return s;return 0;}
 /* Excel column width (characters of the default font) to points, for an 11pt Calibri-like default font (7 px max digit width at 96 dpi) */
 public static double WidthPt(double chars){if(chars<=0)return 0;double px=Math.Truncate(((256*chars+Math.Truncate(128.0/7))/256)*7);return px*72/96;}
}
sealed partial class WbBook {
 public string SourcePath;public byte[] SourceBytes;public WbStyles Styles;readonly Dictionary<string,WbSheetLayout> layouts=new Dictionary<string,WbSheetLayout>(StringComparer.OrdinalIgnoreCase);
 public WbSheetLayout Layout(WbSheet sheet){WbSheetLayout l;if(layouts.TryGetValue(sheet.Name,out l))return l;l=ReadLayout(sheet);layouts[sheet.Name]=l;return l;}
 WbSheetLayout ReadLayout(WbSheet sheet){var l=new WbSheetLayout{MaxRow=sheet.MaxRow,MaxCol=sheet.MaxCol};if(SourceBytes==null&&(SourcePath==null||!File.Exists(SourcePath)))return l;
  using(var fs=SourceBytes!=null?(Stream)new MemoryStream(SourceBytes,false):File.OpenRead(SourcePath))using(var zip=new ZipArchive(fs,ZipArchiveMode.Read)){if(Styles==null)Styles=WbStyles.Read(zip);
   var rels=ReadRels(zip.GetEntry("xl/_rels/workbook.xml.rels"));var wbDoc=new XmlDocument();using(var s=zip.GetEntry("xl/workbook.xml").Open())wbDoc.Load(s);string path=null;
   foreach(XmlElement s in wbDoc.GetElementsByTagName("sheet"))if(s.GetAttribute("name")==sheet.Name){string t;if(rels.TryGetValue(s.GetAttribute("id","http://schemas.openxmlformats.org/officeDocument/2006/relationships"),out t))path=Resolve("xl/",t);}
   if(path==null)return l;var entry=zip.GetEntry(path);if(entry==null)return l;string drawingRel=null,hfRel=null;int lastRow=0;
   using(var r=XmlReader.Create(entry.Open())){while(r.Read()){if(r.NodeType!=XmlNodeType.Element)continue;switch(r.LocalName){
     case "sheetFormatPr":{double d;if(double.TryParse(r.GetAttribute("defaultColWidth"),NumberStyles.Float,CultureInfo.InvariantCulture,out d))l.DefaultColWidth=d;else if(double.TryParse(r.GetAttribute("baseColWidth"),NumberStyles.Float,CultureInfo.InvariantCulture,out d))l.DefaultColWidth=d+0.71;if(double.TryParse(r.GetAttribute("defaultRowHeight"),NumberStyles.Float,CultureInfo.InvariantCulture,out d))l.DefaultRowHeight=d;break;}
     case "col":{int a,b;if(!int.TryParse(r.GetAttribute("min"),out a)||!int.TryParse(r.GetAttribute("max"),out b))break;double w;bool hasW=double.TryParse(r.GetAttribute("width"),NumberStyles.Float,CultureInfo.InvariantCulture,out w);bool hidden=r.GetAttribute("hidden")=="1";int st;bool hasS=int.TryParse(r.GetAttribute("style"),out st);
      for(int c=a;c<=Math.Min(b,16384);c++){if(hasW)l.ColWidth[c]=w;if(hidden)l.HiddenCols.Add(c);if(hasS&&st!=0)l.ColStyle[c]=st;}break;}
     case "row":{int row;if(!int.TryParse(r.GetAttribute("r"),out row))row=lastRow+1;lastRow=row;double h;if(double.TryParse(r.GetAttribute("ht"),NumberStyles.Float,CultureInfo.InvariantCulture,out h))l.RowHeight[row]=h;if(r.GetAttribute("hidden")=="1")l.HiddenRows.Add(row);int st;if(r.GetAttribute("customFormat")=="1"&&int.TryParse(r.GetAttribute("s"),out st))l.RowStyle[row]=st;if(row>l.MaxRow)l.MaxRow=row;break;}
     case "c":{int row,col;int st;if(ParseA1(r.GetAttribute("r")??"",out row,out col)&&int.TryParse(r.GetAttribute("s"),out st)){l.CellStyle[WbSheet.Key(row,col)]=st;if(col>l.MaxCol)l.MaxCol=col;}break;}
     case "mergeCell":{var p=(r.GetAttribute("ref")??"").Split(':');int r1,c1,r2,c2;if(p.Length==2&&ParseA1(p[0],out r1,out c1)&&ParseA1(p[1],out r2,out c2)){var m=new WbMerge{R1=r1,C1=c1,R2=r2,C2=c2};l.Merges.Add(m);for(int i=r1;i<=r2;i++)for(int j=c1;j<=c2;j++)l.MergeAt[WbSheet.Key(i,j)]=m;}break;}
     case "pageSetUpPr":l.FitToPage=r.GetAttribute("fitToPage")=="1";break;
     case "printOptions":l.CenterH=r.GetAttribute("horizontalCentered")=="1";break;
     case "pageMargins":{Func<string,double,double> g=(n,d)=>{double v;return double.TryParse(r.GetAttribute(n),NumberStyles.Float,CultureInfo.InvariantCulture,out v)?v:d;};l.MarginL=g("left",l.MarginL);l.MarginR=g("right",l.MarginR);l.MarginT=g("top",l.MarginT);l.MarginB=g("bottom",l.MarginB);l.MarginHeader=g("header",l.MarginHeader);l.MarginFooter=g("footer",l.MarginFooter);break;}
     case "pageSetup":{l.Landscape=r.GetAttribute("orientation")=="landscape";int v;if(int.TryParse(r.GetAttribute("scale"),out v))l.Scale=v;if(int.TryParse(r.GetAttribute("fitToWidth"),out v))l.FitWidth=v;if(int.TryParse(r.GetAttribute("fitToHeight"),out v))l.FitHeight=v;if(int.TryParse(r.GetAttribute("paperSize"),out v))l.Paper=v;break;}
     case "rowBreaks":{if(!r.IsEmptyElement){int d=r.Depth;while(r.Read()&&!(r.NodeType==XmlNodeType.EndElement&&r.Depth==d))if(r.NodeType==XmlNodeType.Element&&r.LocalName=="brk"){int v;if(int.TryParse(r.GetAttribute("id"),out v))l.RowBreaks.Add(v);}}break;}
     case "oddHeader":l.Header=r.ReadElementContentAsString();break;
     case "oddFooter":l.Footer=r.ReadElementContentAsString();break;
     case "drawing":drawingRel=r.GetAttribute("id","http://schemas.openxmlformats.org/officeDocument/2006/relationships");break;
     case "legacyDrawingHF":hfRel=r.GetAttribute("id","http://schemas.openxmlformats.org/officeDocument/2006/relationships");break;}}}
   string printArea;if(Names.TryGetValue(sheet.Index+"!_xlnm.Print_Area",out printArea)){var p=printArea.Split(',')[0];int bang=p.LastIndexOf('!');if(bang>=0)p=p.Substring(bang+1);var parts=p.Replace("$","").Split(':');int r1,c1,r2,c2;if(parts.Length==2&&ParseA1(parts[0],out r1,out c1)&&ParseA1(parts[1],out r2,out c2)){l.PrintR1=r1;l.PrintC1=c1;l.PrintR2=r2;l.PrintC2=c2;}}
   if(l.PrintR2==0){l.PrintR2=Math.Max(1,sheet.MaxRow);l.PrintC2=Math.Max(1,sheet.MaxCol);}
   string dir=Path.GetDirectoryName(path).Replace('\\','/')+"/";var srels=ReadRels(zip.GetEntry(dir+"_rels/"+Path.GetFileName(path)+".rels"));string target;
   if(drawingRel!=null&&srels.TryGetValue(drawingRel,out target))ReadDrawing(zip,Resolve(dir,target),l);
   if(hfRel!=null&&srels.TryGetValue(hfRel,out target))ReadHeaderFooterPictures(zip,Resolve(dir,target),l);}
  return l;}
 static void ReadDrawing(ZipArchive zip,string path,WbSheetLayout l){var e=zip.GetEntry(path);if(e==null)return;var doc=new XmlDocument();using(var s=e.Open())doc.Load(s);var rels=ReadRels(zip.GetEntry(Path.GetDirectoryName(path).Replace('\\','/')+"/_rels/"+Path.GetFileName(path)+".rels"));string dir=Path.GetDirectoryName(path).Replace('\\','/')+"/";
  foreach(XmlElement anchor in doc.DocumentElement.ChildNodes.OfType<XmlElement>()){var blip=anchor.GetElementsByTagName("a:blip").Cast<XmlElement>().FirstOrDefault();if(blip==null)continue;string target;if(!rels.TryGetValue(blip.GetAttribute("r:embed"),out target))continue;var img=zip.GetEntry(Resolve(dir,target));if(img==null)continue;
   var pic=new WbPicture();using(var ms=new MemoryStream()){using(var s=img.Open())s.CopyTo(ms);pic.Data=ms.ToArray();}
   Func<string,XmlElement> el=n=>anchor.GetElementsByTagName("xdr:"+n).Cast<XmlElement>().FirstOrDefault();Func<XmlElement,string,double> val=(x,n)=>{var c=x==null?null:x.GetElementsByTagName("xdr:"+n).Cast<XmlElement>().FirstOrDefault();double d;return c!=null&&double.TryParse(c.InnerText,NumberStyles.Float,CultureInfo.InvariantCulture,out d)?d:0;};
   var from=el("from");if(from==null){var abs=el("pos");continue;}pic.Col=(int)val(from,"col")+1;pic.Row=(int)val(from,"row")+1;pic.ColOff=val(from,"colOff")/12700;pic.RowOff=val(from,"rowOff")/12700;
   var to=el("to");if(to!=null){pic.Col2=(int)val(to,"col")+1;pic.Row2=(int)val(to,"row")+1;pic.Col2Off=val(to,"colOff")/12700;pic.Row2Off=val(to,"rowOff")/12700;}
   else{var ext=el("ext");if(ext!=null){double cx,cy;double.TryParse(ext.GetAttribute("cx"),NumberStyles.Float,CultureInfo.InvariantCulture,out cx);double.TryParse(ext.GetAttribute("cy"),NumberStyles.Float,CultureInfo.InvariantCulture,out cy);pic.WidthPt=cx/12700;pic.HeightPt=cy/12700;}}
   l.Pictures.Add(pic);}}
 static void ReadHeaderFooterPictures(ZipArchive zip,string path,WbSheetLayout l){var e=zip.GetEntry(path);if(e==null)return;string vml;using(var s=new StreamReader(e.Open()))vml=s.ReadToEnd();var rels=ReadRels(zip.GetEntry(Path.GetDirectoryName(path).Replace('\\','/')+"/_rels/"+Path.GetFileName(path)+".rels"));string dir=Path.GetDirectoryName(path).Replace('\\','/')+"/";
  foreach(System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(vml,"<v:shape [^>]*id=\"([A-Z]{2})\"[\\s\\S]*?o:relid=\"([^\"]+)\"")){string target;if(!rels.TryGetValue(m.Groups[2].Value,out target))continue;var img=zip.GetEntry(Resolve(dir,target));if(img==null)continue;byte[] data;using(var ms=new MemoryStream()){using(var s=img.Open())s.CopyTo(ms);data=ms.ToArray();}
   if(m.Groups[1].Value.StartsWith("L")||m.Groups[1].Value.StartsWith("C")||m.Groups[1].Value.StartsWith("R")){if(m.Groups[1].Value.EndsWith("F"))l.FooterPicture=data;else l.HeaderPicture=data;}}}
}
}
