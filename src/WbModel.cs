using System;using System.Collections.Generic;using System.Globalization;using System.IO;using System.IO.Compression;using System.Linq;using System.Text;using System.Xml;
namespace BeamScheduleFormatter {
/* Program of Works worksheet engine (owner, 20 November 2026: "buong worksheet sa app, kamukha niyan mismo, para iprint na lang thru
   the app"; "yung mga labor, equipment, materials, sundan kung saan kinuha, i-maintain ang formulas"). The DPWH POW/ABC workbook
   (V6.02 .xlsm) is read as cells and formulas and recalculated by the app itself, so DUPA rates keep following Labor, PERR, CMPD and
   Hauling, and DETAILED / ABC / SUM follow the BACKUP quantities exactly as in Excel. This file holds the workbook model and reader. */
sealed class WbError { public readonly string Code; WbError(string c){Code=c;} public override string ToString(){return Code;}
 public static readonly WbError NA=new WbError("#N/A"),Value=new WbError("#VALUE!"),Ref=new WbError("#REF!"),Div0=new WbError("#DIV/0!"),Num=new WbError("#NUM!"),Name=new WbError("#NAME?"),Null=new WbError("#NULL!"),Circ=new WbError("#CIRC!");
 public static WbError Parse(string s){switch((s??"").ToUpperInvariant()){case "#N/A":return NA;case "#VALUE!":return Value;case "#REF!":return Ref;case "#DIV/0!":return Div0;case "#NUM!":return Num;case "#NAME?":return Name;case "#NULL!":return Null;default:return Value;}}}
sealed class WbCell { public int Row,Col;public object Cached;public string Formula;public WbNode Ast;public object Value;public byte State;public int Style;
 public WbCell ArrayMaster;public int ArrayRows,ArrayCols;public object[,] ArrayResult;public bool IsArrayMaster;}
sealed class WbTable { public string Name,Sheet;public int R1,C1,R2,C2,HeaderRows,TotalRows;public List<string> Columns=new List<string>();
 public int ColumnIndex(string name){for(int i=0;i<Columns.Count;i++)if(string.Equals(Columns[i],name,StringComparison.OrdinalIgnoreCase))return C1+i;return -1;}}
sealed class WbSheet { public string Name;public int Index;public readonly Dictionary<long,WbCell> Cells=new Dictionary<long,WbCell>();public int MaxRow,MaxCol;public bool Hidden;
 public static long Key(int r,int c){return (long)r*20000+c;}
 public WbCell Get(int r,int c){WbCell x;return Cells.TryGetValue(Key(r,c),out x)?x:null;}
 public WbCell GetOrAdd(int r,int c){var k=Key(r,c);WbCell x;if(!Cells.TryGetValue(k,out x)){x=new WbCell{Row=r,Col=c};Cells[k]=x;if(r>MaxRow)MaxRow=r;if(c>MaxCol)MaxCol=c;}return x;}}
sealed partial class WbBook {
 public readonly List<WbSheet> Sheets=new List<WbSheet>();public readonly Dictionary<string,WbSheet> ByName=new Dictionary<string,WbSheet>(StringComparer.OrdinalIgnoreCase);
 public readonly Dictionary<string,string> Names=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);/* global; sheet-local names are keyed "index!name" */
 public readonly Dictionary<string,WbTable> Tables=new Dictionary<string,WbTable>(StringComparer.OrdinalIgnoreCase);
 public bool Date1904;
 public WbSheet Sheet(string name){WbSheet s;return name!=null&&ByName.TryGetValue(name,out s)?s:null;}
 public static int ColumnNumber(string letters){int n=0;foreach(char ch in letters.ToUpperInvariant())n=n*26+(ch-'A'+1);return n;}
 public static string ColumnName(int c){var sb=new StringBuilder();while(c>0){int m=(c-1)%26;sb.Insert(0,(char)('A'+m));c=(c-1)/26;}return sb.ToString();}
 public static bool ParseA1(string a1,out int row,out int col){row=0;col=0;int i=0;a1=a1.Replace("$","");while(i<a1.Length&&char.IsLetter(a1[i]))i++;if(i==0||i==a1.Length)return false;col=ColumnNumber(a1.Substring(0,i));return int.TryParse(a1.Substring(i),out row);}
 /* Reads cells (values, formulas incl. shared / array), sheet names, defined names and tables. Styles and print layout are read by WbLayout. */
 public static WbBook Load(string path,Func<string,bool> keepSheet=null){using(var fs=File.OpenRead(path))return Load(fs,keepSheet);}
 public static WbBook Load(Stream stream,Func<string,bool> keepSheet=null){var book=new WbBook();using(var zip=new ZipArchive(stream,ZipArchiveMode.Read)){
  Func<string,ZipArchiveEntry> entry=p=>zip.GetEntry(p.TrimStart('/'));
  var shared=new List<string>();var sse=entry("xl/sharedStrings.xml");if(sse!=null)using(var r=XmlReader.Create(sse.Open(),new XmlReaderSettings{IgnoreWhitespace=false})){var sb=new StringBuilder();bool inSi=false,inRph=false;
   r.Read();while(!r.EOF){if(r.NodeType==XmlNodeType.Element){if(r.LocalName=="si"){inSi=true;sb.Clear();if(r.IsEmptyElement){shared.Add("");inSi=false;}}else if(r.LocalName=="rPh"&&!r.IsEmptyElement)inRph=true;else if(r.LocalName=="t"&&inSi&&!inRph){sb.Append(r.ReadElementContentAsString());continue;}}
    else if(r.NodeType==XmlNodeType.EndElement){if(r.LocalName=="si"){shared.Add(sb.ToString());inSi=false;}else if(r.LocalName=="rPh")inRph=false;}r.Read();}}
  var rels=ReadRels(entry("xl/_rels/workbook.xml.rels"));var wbDoc=new XmlDocument();using(var s=entry("xl/workbook.xml").Open())wbDoc.Load(s);
  var ns=new XmlNamespaceManager(wbDoc.NameTable);ns.AddNamespace("m","http://schemas.openxmlformats.org/spreadsheetml/2006/main");ns.AddNamespace("r","http://schemas.openxmlformats.org/officeDocument/2006/relationships");
  var pr=wbDoc.SelectSingleNode("//m:workbookPr",ns) as XmlElement;book.Date1904=pr!=null&&(pr.GetAttribute("date1904")=="1"||pr.GetAttribute("date1904")=="true");
  var sheetFiles=new List<KeyValuePair<WbSheet,string>>();int idx=0;
  foreach(XmlElement s in wbDoc.SelectNodes("//m:sheets/m:sheet",ns)){var sh=new WbSheet{Name=s.GetAttribute("name"),Index=idx++,Hidden=s.GetAttribute("state")!=""&&s.GetAttribute("state")!="visible"};book.Sheets.Add(sh);book.ByName[sh.Name]=sh;
   string target;if(rels.TryGetValue(s.GetAttribute("id","http://schemas.openxmlformats.org/officeDocument/2006/relationships"),out target))sheetFiles.Add(new KeyValuePair<WbSheet,string>(sh,Resolve("xl/",target)));}
  foreach(XmlElement d in wbDoc.SelectNodes("//m:definedNames/m:definedName",ns)){string key=d.GetAttribute("name");if(d.HasAttribute("localSheetId"))key=d.GetAttribute("localSheetId")+"!"+key;book.Names[key]=d.InnerText;}
  foreach(var sf in sheetFiles){var e=entry(sf.Value);if(e==null)continue;
   var srels=ReadRels(entry(Path.GetDirectoryName(sf.Value).Replace('\\','/')+"/_rels/"+Path.GetFileName(sf.Value)+".rels"));
   foreach(var rel in srels.Where(x=>x.Value.Contains("tables/"))){var te=entry(Resolve(Path.GetDirectoryName(sf.Value).Replace('\\','/')+"/",rel.Value));if(te==null)continue;var t=ReadTable(te,sf.Key.Name);if(t!=null)book.Tables[t.Name]=t;}
   if(keepSheet!=null&&!keepSheet(sf.Key.Name))continue;using(var s=e.Open())ReadSheet(s,sf.Key,shared);}}
  return book;}
 static string Resolve(string baseDir,string target){if(target.StartsWith("/"))return target.TrimStart('/');var parts=new List<string>((baseDir+target).Split('/'));for(int i=0;i<parts.Count;i++){if(parts[i]==".."){parts.RemoveAt(i);parts.RemoveAt(i-1);i-=2;}else if(parts[i]=="."||parts[i]==""){parts.RemoveAt(i);i--;}}return string.Join("/",parts);}
 static Dictionary<string,string> ReadRels(ZipArchiveEntry e){var d=new Dictionary<string,string>();if(e==null)return d;var doc=new XmlDocument();using(var s=e.Open())doc.Load(s);foreach(XmlElement r in doc.GetElementsByTagName("Relationship"))d[r.GetAttribute("Id")]=r.GetAttribute("Target");return d;}
 static WbTable ReadTable(ZipArchiveEntry e,string sheet){var doc=new XmlDocument();using(var s=e.Open())doc.Load(s);var t=doc.DocumentElement;var reference=t.GetAttribute("ref").Split(':');int r1,c1,r2,c2;if(reference.Length!=2||!ParseA1(reference[0],out r1,out c1)||!ParseA1(reference[1],out r2,out c2))return null;
  var table=new WbTable{Name=t.GetAttribute("displayName")!=""?t.GetAttribute("displayName"):t.GetAttribute("name"),Sheet=sheet,R1=r1,C1=c1,R2=r2,C2=c2,HeaderRows=t.HasAttribute("headerRowCount")?int.Parse(t.GetAttribute("headerRowCount")):1,TotalRows=t.HasAttribute("totalsRowCount")?int.Parse(t.GetAttribute("totalsRowCount")):0};
  foreach(XmlElement c in t.GetElementsByTagName("tableColumn"))table.Columns.Add(c.GetAttribute("name").Replace("_x000a_","\n"));return table;}
 static void ReadSheet(Stream s,WbSheet sheet,List<string> shared){var masters=new Dictionary<string,KeyValuePair<WbCell,string>>();
  using(var r=XmlReader.Create(s)){while(r.Read()){if(r.NodeType!=XmlNodeType.Element||r.LocalName!="c")continue;string a1=r.GetAttribute("r"),t=r.GetAttribute("t");int row,col;if(a1==null||!ParseA1(a1,out row,out col))continue;
    string sv=r.GetAttribute("s");string v=null,f=null,ftype=null,fref=null,si=null,inline=null;
    if(!r.IsEmptyElement){int depth=r.Depth;r.Read();while(!r.EOF&&!(r.NodeType==XmlNodeType.EndElement&&r.Depth==depth)){if(r.NodeType!=XmlNodeType.Element){r.Read();continue;}
      if(r.LocalName=="v")v=r.ReadElementContentAsString();else if(r.LocalName=="f"){ftype=r.GetAttribute("t");fref=r.GetAttribute("ref");si=r.GetAttribute("si");f=r.ReadElementContentAsString();}
      else if(r.LocalName=="is"){inline=System.Text.RegularExpressions.Regex.Replace(r.ReadInnerXml(),"<[^>]+>","");}else r.Read();}}
    if(v==null&&f==null&&inline==null)continue;var cell=sheet.GetOrAdd(row,col);int style;if(sv!=null&&int.TryParse(sv,out style))cell.Style=style;
    if(t=="s"){int i;cell.Cached=v!=null&&int.TryParse(v,out i)&&i<shared.Count?shared[i]:"";}else if(t=="str"||t=="inlineStr")cell.Cached=inline??v??"";else if(t=="b")cell.Cached=v=="1";else if(t=="e")cell.Cached=WbError.Parse(v);
    else if(v!=null){double d;cell.Cached=double.TryParse(v,NumberStyles.Float,CultureInfo.InvariantCulture,out d)?(object)d:v;}
    if(f!=null){if(ftype=="shared"&&si!=null){if(f!=""){masters[si]=new KeyValuePair<WbCell,string>(cell,f);cell.Formula=f;}else{KeyValuePair<WbCell,string> m;if(masters.TryGetValue(si,out m))cell.Formula=WbFormula.Shift(m.Value,row-m.Key.Row,col-m.Key.Col);}}
     else if(f!=""){cell.Formula=f;if(ftype=="array"&&fref!=null){var p=fref.Split(':');int r2=row,c2=col;if(p.Length==2)ParseA1(p[1],out r2,out c2);cell.IsArrayMaster=true;cell.ArrayRows=r2-row+1;cell.ArrayCols=c2-col+1;
       for(int rr=row;rr<=r2;rr++)for(int cc=col;cc<=c2;cc++)if(rr!=row||cc!=col){var other=sheet.GetOrAdd(rr,cc);other.ArrayMaster=cell;}}}}}}}
}
}
