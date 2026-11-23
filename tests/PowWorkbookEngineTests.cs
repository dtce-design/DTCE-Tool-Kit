using System;using System.Collections.Generic;using System.Drawing;using System.IO;using System.IO.Compression;using System.Linq;using System.Text;using BeamScheduleFormatter;
/* POW worksheet engine (v6.0.0): a small DUPA-like workbook is written here (the owner's DPWH workbook is not in the repository) and
   read, recalculated, re-computed after an input change, formatted and paginated. The real V6.02 workbook was checked separately:
   all 106,296 formula cells match Excel's saved values. */
static class PowWorkbookEngineTests {
 static int checks;static void Check(bool ok,string what){checks++;if(!ok){Console.WriteLine("FAIL: "+what);Environment.Exit(1);}}
 static void Near(object v,double expected,string what){Check(v is double&&Math.Abs((double)v-expected)<1e-9*Math.Max(1,Math.Abs(expected)),what+" (got "+v+", expected "+expected+")");}
 static string X(string s){return s.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;").Replace("\"","&quot;");}
 static string Cell(string r,object v,string f=null,int s=0,bool shared=false,string si=null,string sref=null){var sb=new StringBuilder("<c r=\""+r+"\""+(s>0?" s=\""+s+"\"":""));
  if(v is string&&f==null){sb.Append(" t=\"inlineStr\"><is><t>"+X((string)v)+"</t></is></c>");return sb.ToString();}if(v is string)sb.Append(" t=\"str\"");sb.Append(">");
  if(f!=null){if(shared)sb.Append("<f t=\"shared\""+(sref!=null?" ref=\""+sref+"\"":"")+" si=\""+si+"\">"+X(f)+"</f>");else sb.Append("<f>"+X(f)+"</f>");}else if(shared)sb.Append("<f t=\"shared\" si=\""+si+"\"/>");
  if(v!=null)sb.Append("<v>"+X(Convert.ToString(v,System.Globalization.CultureInfo.InvariantCulture))+"</v>");sb.Append("</c>");return sb.ToString();}
 static void Add(ZipArchive z,string name,string text){var e=z.CreateEntry(name);using(var w=new StreamWriter(e.Open(),new UTF8Encoding(false)))w.Write(text);}
 static string Build(){string path=Path.Combine(Path.GetTempPath(),"pow-engine-test-"+Guid.NewGuid().ToString("N")+".xlsx");const string M="http://schemas.openxmlformats.org/spreadsheetml/2006/main",R="http://schemas.openxmlformats.org/officeDocument/2006/relationships";
  using(var fs=File.Create(path))using(var z=new ZipArchive(fs,ZipArchiveMode.Create)){
   Add(z,"[Content_Types].xml","<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"/>");
   Add(z,"xl/workbook.xml","<workbook xmlns=\""+M+"\" xmlns:r=\""+R+"\"><sheets><sheet name=\"Labor\" sheetId=\"1\" r:id=\"rId1\"/><sheet name=\"Indirect Cost\" sheetId=\"2\" r:id=\"rId2\"/><sheet name=\"900(1)c\" sheetId=\"3\" r:id=\"rId3\"/></sheets><definedNames><definedName name=\"_xlnm.Print_Area\" localSheetId=\"2\">'900(1)c'!$A$1:$E$12</definedName><definedName name=\"VAT\">'Indirect Cost'!$C$3</definedName></definedNames></workbook>");
   Add(z,"xl/_rels/workbook.xml.rels","<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"ws\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"ws\" Target=\"worksheets/sheet2.xml\"/><Relationship Id=\"rId3\" Type=\"ws\" Target=\"worksheets/sheet3.xml\"/><Relationship Id=\"rId9\" Type=\"st\" Target=\"styles.xml\"/></Relationships>");
   Add(z,"xl/styles.xml","<styleSheet xmlns=\""+M+"\"><numFmts count=\"1\"><numFmt numFmtId=\"164\" formatCode=\"_(&quot;₱&quot;* #,##0.00_);_(&quot;₱&quot;* \\(#,##0.00\\);_(&quot;₱&quot;* &quot;-&quot;??_);_(@_)\"/></numFmts><fonts><font><sz val=\"8\"/><name val=\"Arial\"/></font><font><b/><sz val=\"8\"/><name val=\"Arial\"/></font></fonts><fills><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill><fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF95BB72\"/></patternFill></fill></fills><borders><border/><border><left style=\"thin\"/><right style=\"thin\"/><top style=\"thin\"/><bottom style=\"thin\"/></border></borders><cellXfs><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/><xf numFmtId=\"164\" fontId=\"0\" fillId=\"0\" borderId=\"1\"/><xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"1\"><alignment horizontal=\"center\"/></xf><xf numFmtId=\"10\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellXfs></styleSheet>");
   /* Labor table: classification -> hourly rate */
   Add(z,"xl/worksheets/sheet1.xml","<worksheet xmlns=\""+M+"\" xmlns:r=\""+R+"\"><sheetData><row r=\"1\">"+Cell("A1","Classification")+Cell("B1","Hourly Rate")+"</row><row r=\"2\">"+Cell("A2","Foreman")+Cell("B2",141.36)+"</row><row r=\"3\">"+Cell("A3","Skilled Laborer")+Cell("B3",102.42)+"</row><row r=\"4\">"+Cell("A4","Unskilled Laborer")+Cell("B4",78.9)+"</row></sheetData><tableParts count=\"1\"><tablePart r:id=\"rT\"/></tableParts></worksheet>");
   Add(z,"xl/worksheets/_rels/sheet1.xml.rels","<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rT\" Type=\"table\" Target=\"../tables/table1.xml\"/></Relationships>");
   Add(z,"xl/tables/table1.xml","<table xmlns=\""+M+"\" id=\"1\" name=\"Table124\" displayName=\"Table124\" ref=\"A1:B4\"><tableColumns count=\"2\"><tableColumn id=\"1\" name=\"Classification\"/><tableColumn id=\"2\" name=\"Hourly Rate\"/></tableColumns></table>");
   Add(z,"xl/worksheets/sheet2.xml","<worksheet xmlns=\""+M+"\"><sheetData><row r=\"1\">"+Cell("B1","Direct cost")+Cell("C1",null,"'900(1)c'!E9")+"</row><row r=\"2\">"+Cell("B2","OCM")+Cell("C2",null,"IF($C$1<=5000000,15%,12%)")+"</row><row r=\"3\">"+Cell("B3","VAT")+Cell("C3",0.12)+"</row></sheetData></worksheet>");
   /* DUPA: labor lines with INDEX/MATCH on the table (shared formula), subtotal, OCM, VAT, X marker */
   var rows=new StringBuilder();rows.Append("<row r=\"1\">"+Cell("A1","DETAILED UNIT PRICE ANALYSIS",null,2)+"</row>");
   rows.Append("<row r=\"2\">"+Cell("A2","Designation",null,2)+Cell("B2","Persons",null,2)+Cell("C2","Hours",null,2)+Cell("D2","Rate",null,2)+Cell("E2","Amount",null,2)+"</row>");
   string[] who={"Foreman","Skilled Laborer","Unskilled Laborer"};int[] n={1,4,8};
   for(int i=0;i<3;i++){int r=3+i;rows.Append("<row r=\""+r+"\">"+Cell("A"+r,who[i])+Cell("B"+r,n[i])+Cell("C"+r,1)+(i==0?Cell("D3",null,"INDEX(Table124[Hourly Rate],MATCH(A3,Table124[Classification],0))",1,true,"0","D3:D5"):Cell("D"+r,null,"",1,true,"0"))+Cell("E"+r,null,"IF(D"+r+"=\"\",\"\",ROUND(B"+r+"*C"+r+"*D"+r+",2))",1)+"</row>");}
   rows.Append("<row r=\"7\">"+Cell("A7","Sub Total")+Cell("E7",null,"ROUND(SUM(E3:E6),2)",1)+"</row>");
   rows.Append("<row r=\"8\">"+Cell("A8","Output per hour")+Cell("E8",10)+"</row>");
   rows.Append("<row r=\"9\">"+Cell("A9","Direct unit cost")+Cell("E9",null,"E7/E8",1)+"</row>");
   rows.Append("<row r=\"10\">"+Cell("A10","OCM")+Cell("D10",null,"'Indirect Cost'!C2",3)+Cell("E10",null,"E9*D10",1)+"</row>");
   rows.Append("<row r=\"11\">"+Cell("A11","VAT")+Cell("D11",null,"VAT",3)+Cell("E11",null,"SUM(E9,E10)*D11",1)+"</row>");
   rows.Append("<row r=\"12\">"+Cell("A12","Total unit cost")+Cell("E12",null,"SUM(E9:E11)",1)+Cell("F12",null,"SUMIF(A3:A5,\"*Laborer\",E3:E5)")+Cell("G12",null,"COUNTIFS(B3:B5,\">1\",C3:C5,1)")+"</row>");
   rows.Append("<row r=\"13\">"+Cell("A13",null,"IF(E12>0,\"\",\"X\")")+Cell("B13",null,"SUMPRODUCT((B3:B5>1)*(E3:E5))")+Cell("C13",null,"DATE(2027,5,1)")+Cell("D13",null,"WEEKDAY(C13)")+"</row>");
   Add(z,"xl/worksheets/sheet3.xml","<worksheet xmlns=\""+M+"\"><sheetPr><pageSetUpPr fitToPage=\"1\"/></sheetPr><cols><col min=\"1\" max=\"1\" width=\"24\" customWidth=\"1\"/><col min=\"2\" max=\"5\" width=\"11\" customWidth=\"1\"/></cols><sheetData>"+rows+"</sheetData><mergeCells count=\"1\"><mergeCell ref=\"A1:E1\"/></mergeCells><pageMargins left=\"0.5\" right=\"0.5\" top=\"0.75\" bottom=\"0.75\" header=\"0.3\" footer=\"0.3\"/><pageSetup paperSize=\"9\" fitToHeight=\"0\" orientation=\"portrait\"/><headerFooter><oddFooter>&amp;R&amp;\"Arial,Regular\"&amp;6Page &amp;P of &amp;N</oddFooter></headerFooter></worksheet>");}
  return path;}
 static void Main(){string path=Build();try{
  var book=WbBook.Load(path);var calc=new WbCalc(book);WbCalc.RunWithLargeStack(()=>calc.CalculateAll());var s=book.Sheet("900(1)c");Func<string,object> v=a=>{int r,c;WbBook.ParseA1(a,out r,out c);return calc.CellValue(s,r,c);};
  Check(book.Sheets.Count==3&&book.Tables.ContainsKey("Table124"),"sheets and table read");Check(calc.ParseErrors==0&&calc.Problems.Count==0,"every formula parsed ("+string.Join("; ",calc.Problems)+")");
  Near(v("D3"),141.36,"Foreman rate from the Labor table (INDEX/MATCH)");Near(v("D4"),102.42,"shared formula moved to the next row");Near(v("D5"),78.9,"shared formula, third row");
  Near(v("E4"),409.68,"labor amount ROUND(4 x 1 x 102.42)");Near(v("E7"),1182.24,"labor subtotal");Near(v("E9"),118.224,"direct unit cost");
  Near(calc.CellValue(book.Sheet("Indirect Cost"),2,3),0.15,"OCM 15% from the direct cost bracket");Near(v("E10"),17.7336,"OCM amount");Near(v("D11"),0.12,"defined name VAT");Near(v("E11"),(118.224+17.7336)*0.12,"VAT on cost + OCM");
  Near(v("F12"),409.68+631.2,"SUMIF with wildcard");Near(v("G12"),2,"COUNTIFS");Check("".Equals(v("A13")),"row marker blank when there is a cost");Near(v("B13"),409.68+631.2,"SUMPRODUCT of a condition and amounts");
  Near(v("C13"),46508,"DATE serial");Near(v("D13"),7,"WEEKDAY (1 May 2027 is a Saturday)");
  /* a rate change flows through */
  book.Sheet("Labor").Get(3,2).Cached=110.0;WbCalc.RunWithLargeStack(()=>{calc.Reset();calc.CalculateAll();});Near(v("E4"),440.0,"changed Labor rate reaches the DUPA");Near(v("E7"),141.36+440+631.2,"subtotal follows");
  /* number formats */
  var f=WbNumberFormat.Format(3593976.42597235,"_(\"₱\"* #,##0.00_);_(\"₱\"* \\(#,##0.00\\);_(\"₱\"* \"-\"??_);_(@_)");Check(f.Left.Trim()=="₱"&&f.Text.Trim()=="3,593,976.43","accounting format splits the peso sign and the amount ("+f.Left+"|"+f.Text+")");
  Check(WbNumberFormat.Format(0.15,"0%").Text=="15%"&&WbNumberFormat.Format(0.1264,"0.00%").Text=="12.64%","percent formats");Check(WbNumberFormat.Format(46510.0,"[$-409]mmmm\\ d\\,\\ yyyy;@").Text=="May 3, 2027","date format");
  Check(WbNumberFormat.Format(1182.24,"#,##0.00").Text=="1,182.24"&&WbNumberFormat.Format(117.0,"0\\ \"C.D.\"").Text=="117 C.D.","number with literal");
  /* layout, drawing and pages */
  var l=book.Layout(s);Check(l.PrintR2==12&&l.PrintC2==5&&l.FitToPage&&!l.Landscape,"print area and page setup read");Check(l.Merges.Count==1&&l.MergeAt.ContainsKey(WbSheet.Key(1,3)),"merged title");Check(book.Styles.Xf.Count==4&&book.Styles.Xf[2].Fill.HasValue,"styles read");
  var render=new WbRender(book,calc);var job=new WbPrintJob(render,new List<WbSheet>{s});Check(job.PageCount==1,"one A4 page");var img=job.Images(72,1)[0];Check(img.Width==595&&img.Height==842,"A4 portrait page image");
  bool ink=false;for(int y=0;y<img.Height&&!ink;y+=2)for(int x=0;x<img.Width;x+=2){var p=img.GetPixel(x,y);if(p.R<128&&p.G<128&&p.B<128){ink=true;break;}}Check(ink,"page has printed content");img.Dispose();
  render.RowHidden=(sh,r)=>r==8;Check(render.RowHeightPt(s,l,8)==0&&render.RowHeightPt(s,l,9)>0,"rows marked hidden take no space");
  /* POW project file (.pow.xml): typed values over the template */
  var vals=new Dictionary<string,object>{{"BACKUP!BC105",12.5},{"FRONT PAGE!M4","Sample project"},{"Labor!C6",null}};string tmp=Path.Combine(Path.GetTempPath(),"pow-project-"+Guid.NewGuid().ToString("N")+".pow.xml");
  File.WriteAllText(tmp,PowWorkbookForm.ProjectXml(vals,"built-in DPWH POW V6.02"));var back=PowWorkbookForm.ReadProject(tmp);Check(back.Count==3&&(double)back["BACKUP!BC105"]==12.5&&(string)back["FRONT PAGE!M4"]=="Sample project"&&back["Labor!C6"]==null,"POW project values saved and read back");
  File.WriteAllText(tmp,"<ProgramOfWorks version=\"21\"/>");try{PowWorkbookForm.ReadProject(tmp);Check(false,"a Bill of Quantities file is refused by POW");}catch(Exception x){Check(x.Message.Contains("Bill of Quantities"),"a Bill of Quantities file is refused by POW with a clear message");}File.Delete(tmp);
  Check(ProgramOfWorksForm.BoqFileName("Piggatan.pow.xml")=="Piggatan.boq.xml"&&ProgramOfWorksForm.BoqFileName("Piggatan.boq.xml")=="Piggatan.boq.xml","Bill of Quantities saves as .boq.xml");
  /* the built-in DPWH POW V6.02 template: every formula recomputed equals the value Excel saved */
  var bytes=PowWorkbookForm.BuiltInTemplate();Check(bytes!=null&&bytes.Length>1000000,"built-in POW template present ("+(bytes==null?0:bytes.Length)+" bytes)");
  var pow=WbBook.Load(new MemoryStream(bytes));pow.SourceBytes=bytes;var pc=new WbCalc(pow);WbCalc.RunWithLargeStack(()=>pc.CalculateAll());int formulas=0,same=0;string first=null;
  foreach(var sh in pow.Sheets)foreach(var cell in sh.Cells.Values){if(cell.Formula==null)continue;formulas++;object x=cell.Cached,y=cell.Value;bool ok=x is double&&y is double?Math.Abs((double)x-(double)y)<=1e-6*Math.Max(1,Math.Abs((double)x)):x==null?(y==null||"".Equals(y)||(y is double&&(double)y==0)):Convert.ToString(x)==Convert.ToString(y);if(ok)same++;else if(first==null)first=sh.Name+"!"+WbBook.ColumnName(cell.Col)+cell.Row+" excel="+x+" app="+y;}
  Check(formulas>100000&&same==formulas,"all "+formulas+" template formulas equal Excel's values"+(first==null?"":" (first difference "+first+")"));
  Near(pc.CellValue(pow.Sheet("SUM"),45,106),4950000,"template grand total");var dupaPage=new WbPrintJob(new WbRender(pow,pc),new List<WbSheet>{pow.Sheet("900(1)c")});Check(dupaPage.PageCount>=1,"template DUPA paginates");
  /* Bill of Quantities -> BACKUP link: quantities go to the item's BACKUP row (by row and item no.), other item rows are cleared */
  var items=new List<BoqPowItem>{new BoqPowItem{Code="102(2)",Row=105,Quantity=50,Unit="cu.m."},new BoqPowItem{Code="B.5(1)",Row=77,Quantity=2,Unit="ea"},new BoqPowItem{Code="900(1)c",Row=1,Quantity=12.5,Unit="cu.m."},new BoqPowItem{Code="999(9)",Row=9,Quantity=1,Unit="ls"}};
  var plan=PowBackupLink.Plan(pow,pc,items);Check(plan.Matched==3&&plan.Unmatched.Count==1&&plan.Unmatched[0].Code=="999(9)","three pay items placed, unknown item reported");
  Check((double)plan.Values["BACKUP!BC105"]==50&&(double)plan.Values["BACKUP!BC77"]==2&&plan.Values["BACKUP!BC74"]==null,"by row: 102(2) row 105, COA billboard row 77; DPWH billboard row 74 cleared");
  var concreteRow=plan.Values.Where(kv=>kv.Value is double&&(double)kv.Value==12.5).Select(kv=>kv.Key).ToList();Check(concreteRow.Count==1,"900(1)c found by item no. when its row differs ("+string.Join(",",concreteRow)+")");
  Check(!plan.Values.ContainsKey("BACKUP!BC59"),"formula quantity cells (A.1.4(1) months) are left alone");
  /* Progress Photographs keep a value typed in the workbook (owner, 23 November 2026: "except for progress photograph") */
  {var bk=pow.Sheet("BACKUP");var cell=bk.GetOrAdd(59,55);var keepF=cell.Formula;var keepV=cell.Cached;cell.Formula=null;cell.Cached=6.0;var typed=PowBackupLink.Plan(pow,pc,items);Check(!typed.Values.ContainsKey("BACKUP!BC59")&&typed.Values.ContainsKey("BACKUP!BC74")&&typed.Values["BACKUP!BC74"]==null,"typed Progress Photographs (row 59) kept; other items without a quantity cleared");cell.Formula=keepF;cell.Cached=keepV;}
  var backup=pow.Sheet("BACKUP");foreach(var kv in plan.Values){int r,c;WbBook.ParseA1(kv.Key.Substring(7),out r,out c);backup.GetOrAdd(r,c).Cached=kv.Value;}WbCalc.RunWithLargeStack(()=>{pc.Reset();pc.CalculateAll();});
  var det=pow.Sheet("DETAILED");Near(pc.CellValue(det,71,51),50,"DETAILED quantity of 102(2) follows BACKUP");Check(pc.CellValue(det,71,1) as string=="","102(2) row shown (no X)");Check("X".Equals(pc.CellValue(det,18,1)),"item without quantity hidden (X)");
  Console.WriteLine("PASS: "+checks+" POW worksheet engine checks (formulas, shared formulas, tables, names, recalculation, formats, A4 pages).");}
  finally{try{File.Delete(path);}catch(IOException){}}}
}
