using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Printing;using System.Globalization;using System.IO;using System.Linq;using System.Threading;using System.Windows.Forms;using System.Xml;
namespace BeamScheduleFormatter {
/* Program of Works (POW) workspace (owner, 20 November 2026: "gawin yung POW as separate worksheet, walang password or code access";
   "buong worksheet sa app, kamukha niyan mismo, para iprint na lang thru the app"; "independent na siya sa .xlsm"; "saka lang
   mapapalitan kapag binago ko, masasave na parang xml"). The DPWH POW/ABC workbook (V6.02) is built into the app as a slim template
   (PowTemplate.xlsx: every sheet, formula, table, name, style and picture of the owner's workbook, without macros), so no .xlsm and no
   Excel are needed. All formulas are recalculated by the app (WbCalc), sheets are shown like Excel (WbRender) and printed on A4. The
   template's values stay as they are until the owner types a new value; the typed values are the POW project, saved as a .pow.xml file
   (root <PowWorkbook>). "Use another POW workbook" can still replace the built-in template with a newer DPWH version. */
sealed class PowWorkbookForm : Form {
 internal static string DataFolder{get{var dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DTCE Tool Kit","POW");Directory.CreateDirectory(dir);return dir;}}
 /* a workbook chosen with "Use another POW workbook" wins over the built-in template */
 internal static string CustomTemplatePath{get{foreach(var n in new[]{"PowWorkbook.xlsm","PowWorkbook.xlsx"}){var p=Path.Combine(DataFolder,n);if(File.Exists(p))return p;}return null;}}
 static string LastProjectPath{get{return Path.Combine(DataFolder,"LastProject.txt");}}
 internal static byte[] BuiltInTemplate(){var asm=typeof(PowWorkbookForm).Assembly;using(var s=asm.GetManifestResourceStream("PowTemplate.xlsx")){if(s==null)return null;using(var ms=new MemoryStream()){s.CopyTo(ms);return ms.ToArray();}}}
 WbBook book;WbCalc calc;WbRender render;readonly TabControl tabs=new TabControl{Dock=DockStyle.Top,Multiline=true,SizeMode=TabSizeMode.Normal,Height=30};
 readonly WbSheetView view=new WbSheetView{Dock=DockStyle.Fill};readonly Label status=new Label{Dock=DockStyle.Bottom,Height=24,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(8,0,8,0)};
 readonly ComboBox dupaList=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=520};readonly Panel dupaBar=new Panel{Dock=DockStyle.Top,Height=34,Visible=false,Padding=new Padding(8,4,8,4)};
 readonly CheckBox hideX=new CheckBox{Text="Hide rows marked X",Checked=true,AutoSize=true};readonly ComboBox zoom=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=80};
 /* typed values (the POW project) and the template values they replaced */
 readonly Dictionary<string,object> edits=new Dictionary<string,object>(StringComparer.OrdinalIgnoreCase);readonly Dictionary<string,object> originals=new Dictionary<string,object>(StringComparer.OrdinalIgnoreCase);
 string projectPath;bool dirty;string templateName="";
 public PowWorkbookForm(){Text="Program of Works (POW)";Font=new Font("Segoe UI",9.5f);BackColor=PowTheme.Background;Width=1280;Height=820;KeyPreview=true;
  var bar=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(8,6,8,4),WrapContents=true};
  Func<string,EventHandler,Button> btn=(t,h)=>{var b=new Button{Text=t,AutoSize=true,Height=30,Margin=new Padding(0,0,8,0)};b.Click+=h;bar.Controls.Add(b);return b;};
  btn("New POW",(s,e)=>NewProject());btn("Open POW…",(s,e)=>OpenProject());btn("Save POW",(s,e)=>SaveProject(false));btn("Save POW As…",(s,e)=>SaveProject(true));
  btn("Print…",(s,e)=>PrintSheets(false));btn("Print preview",(s,e)=>PrintSheets(true));btn("Print DUPA of items with quantity…",(s,e)=>PrintDupas());btn("Recalculate",(s,e)=>Recalculate(null));btn("Use another POW workbook…",(s,e)=>ImportWorkbook());
  bar.Controls.Add(new Label{Text="Zoom",AutoSize=true,Margin=new Padding(12,7,4,0)});zoom.Items.AddRange(new object[]{"50%","75%","100%","125%","150%","200%"});zoom.SelectedIndex=2;bar.Controls.Add(zoom);hideX.Margin=new Padding(12,6,0,0);bar.Controls.Add(hideX);
  zoom.SelectedIndexChanged+=(s,e)=>{view.Zoom=float.Parse(zoom.Text.TrimEnd('%'),CultureInfo.InvariantCulture)/100f;};hideX.CheckedChanged+=(s,e)=>view.Invalidate();
  dupaBar.Controls.Add(dupaList);dupaList.Dock=DockStyle.Left;dupaList.SelectedIndexChanged+=(s,e)=>{if(book!=null&&dupaList.SelectedItem!=null)ShowSheet(book.Sheet(((DupaItem)dupaList.SelectedItem).Sheet));};
  tabs.SelectedIndexChanged+=(s,e)=>ShowTab();tabs.SizeChanged+=(s,e)=>FitTabs();
  view.CellActivated+=EditCell;view.CellSelected+=(sheet,r,c)=>Describe(sheet,r,c);
  Controls.Add(view);Controls.Add(dupaBar);Controls.Add(tabs);Controls.Add(bar);Controls.Add(status);
  KeyDown+=(s,e)=>{if(e.Control&&e.KeyCode==Keys.S){SaveProject(e.Shift);e.SuppressKeyPress=true;}else if(e.Control&&e.KeyCode==Keys.O){OpenProject();e.SuppressKeyPress=true;}};
  FormClosing+=(s,e)=>{if(!ConfirmDiscard())e.Cancel=true;};
  Shown+=(s,e)=>{string last=null;try{if(File.Exists(LastProjectPath))last=File.ReadAllText(LastProjectPath).Trim();}catch(IOException){}LoadTemplate(last!=null&&File.Exists(last)?last:null);};}
 sealed class DupaItem{public string Sheet,Label;public override string ToString(){return Label;}}
 static readonly string[] Front={"FRONT PAGE","BOQ","DETAILED","ABC","SUM","SOQ","ITEMIZED","OSH","EQUIPMENT","BACKUP","CD","Contract Duration","Labor","PERR","CMPD","Indirect Cost"};
 bool IsDupa(WbSheet s){var v=s.Get(4,1);return v!=null&&"DETAILED UNIT PRICE ANALYSIS".Equals(Convert.ToString(v.Cached),StringComparison.OrdinalIgnoreCase);}
 void UpdateTitle(){string name=projectPath==null?"New POW (not saved)":Path.GetFileName(projectPath);Text="Program of Works (POW) - "+name+(dirty?" *":"");var host=FindForm();if(Parent!=null&&host!=null&&host!=this)host.Text=Text;}
 void ImportWorkbook(){if(!ConfirmDiscard())return;using(var d=new OpenFileDialog{Title="Use another DPWH POW workbook as the template",Filter="Excel workbook (*.xlsm;*.xlsx)|*.xlsm;*.xlsx"}){if(d.ShowDialog(this)!=DialogResult.OK)return;
   string ext=Path.GetExtension(d.FileName).ToLowerInvariant();string target=Path.Combine(DataFolder,"PowWorkbook"+ext);foreach(var old in new[]{"PowWorkbook.xlsm","PowWorkbook.xlsx"}){var p=Path.Combine(DataFolder,old);if(File.Exists(p)&&!string.Equals(p,target,StringComparison.OrdinalIgnoreCase))File.Delete(p);}
   File.Copy(d.FileName,target,true);projectPath=null;LoadTemplate(null);}}
 /* loads the template (built in, or the owner's own) and, if given, a saved POW project on top of it */
 void LoadTemplate(string project){Enabled=false;status.Text="Opening the POW workbook and computing all formulas…";Cursor=Cursors.WaitCursor;string custom=CustomTemplatePath;
  WbBook loaded=null;WbCalc c=null;Exception error=null;Dictionary<string,object> projectEdits=null;
  var t=new Thread(()=>{try{if(custom!=null){loaded=WbBook.Load(custom);}else{var bytes=BuiltInTemplate();if(bytes==null)throw new Exception("The built-in POW template is missing from this build.");loaded=WbBook.Load(new MemoryStream(bytes));loaded.SourceBytes=bytes;}
    c=new WbCalc(loaded);if(project!=null)projectEdits=ReadProject(project);c.CalculateAll();}catch(Exception x){error=x;}},512*1024*1024){IsBackground=true};
  t.Start();var timer=new System.Windows.Forms.Timer{Interval=200};timer.Tick+=(s,e)=>{if(t.IsAlive)return;timer.Stop();timer.Dispose();Enabled=true;Cursor=Cursors.Default;
   if(error!=null){status.Text="Could not open the POW workbook: "+error.Message;MessageBox.Show(this,error.Message,Text);return;}
   book=loaded;calc=c;templateName=custom!=null?Path.GetFileName(custom):"built-in DPWH POW V6.02";render=new WbRender(book,calc);render.RowHidden=RowHidden;view.Render=render;edits.Clear();originals.Clear();
   string note="";if(projectEdits!=null){int applied=Apply(projectEdits);projectPath=project;note=", "+applied+" saved value(s) from "+Path.GetFileName(project);WbCalc.RunWithLargeStack(()=>{calc.Reset();calc.CalculateAll();});}else projectPath=null;
   dirty=false;BuildTabs();UpdateTitle();status.Text=templateName+": "+book.Sheets.Count+" sheets, "+calc.Evaluated.ToString("N0")+" formulas computed"+note+". Double-click an input cell to change it.";};timer.Start();}
 bool RowHidden(WbSheet s,int r){if(!hideX.Checked)return false;var c=s.Get(r,1);if(c==null)return false;var v=c.Formula!=null||c.ArrayMaster!=null?calc.CellValue(s,c):c.Cached;return v is string&&((string)v).Trim().Equals("X",StringComparison.OrdinalIgnoreCase);}
 void BuildTabs(){tabs.TabPages.Clear();var order=Front.Where(n=>book.Sheet(n)!=null).ToList();foreach(var s in book.Sheets)if(!s.Hidden&&!IsDupa(s)&&!order.Contains(s.Name,StringComparer.OrdinalIgnoreCase))order.Add(s.Name);
  foreach(var n in order){var sh=book.Sheet(n);if(sh.Hidden&&!Front.Contains(n))continue;tabs.TabPages.Add(new TabPage(n){Tag=sh});}
  var dupas=book.Sheets.Where(IsDupa).ToList();if(dupas.Count>0){var page=new TabPage("DUPA ("+dupas.Count+")"){Tag="DUPA"};tabs.TabPages.Insert(Math.Min(5,tabs.TabPages.Count),page);dupaList.Items.Clear();
   foreach(var s in dupas){var desc=Convert.ToString(Val(s,16,5));dupaList.Items.Add(new DupaItem{Sheet=s.Name,Label=s.Name+"  —  "+(desc.Length>90?desc.Substring(0,90)+"…":desc)});}if(dupaList.Items.Count>0)dupaList.SelectedIndex=0;}
  FitTabs();if(tabs.TabPages.Count>0){tabs.SelectedIndex=0;ShowTab();}}
 void FitTabs(){if(tabs.TabPages.Count==0){tabs.Height=30;return;}int rows=Math.Max(1,tabs.RowCount);tabs.Height=rows*(tabs.ItemSize.Height+2)+6;}
 object Val(WbSheet s,int r,int c){var cell=s.Get(r,c);if(cell==null)return null;return cell.Formula!=null||cell.ArrayMaster!=null?calc.CellValue(s,cell):cell.Cached;}
 void ShowTab(){if(book==null||tabs.SelectedTab==null)return;FitTabs();if("DUPA".Equals(tabs.SelectedTab.Tag)){dupaBar.Visible=true;var item=dupaList.SelectedItem as DupaItem;if(item!=null)ShowSheet(book.Sheet(item.Sheet));return;}dupaBar.Visible=false;ShowSheet(tabs.SelectedTab.Tag as WbSheet);}
 void ShowSheet(WbSheet s){if(s==null)return;Cursor=Cursors.WaitCursor;try{view.Sheet=s;}finally{Cursor=Cursors.Default;}}
 void Describe(WbSheet s,int r,int c){var cell=s.Get(r,c);string a=s.Name+"!"+WbBook.ColumnName(c)+r;if(cell==null){status.Text=a;return;}var v=Val(s,r,c);string at=s.Name+"!"+WbBook.ColumnName(c)+r;status.Text=a+"  =  "+WbCalc.Text(v)+(cell.Formula!=null?"      formula: ="+cell.Formula.Replace("\n"," "):cell.ArrayMaster!=null?"      (array formula)":edits.ContainsKey(at)?"      your value (template: "+WbCalc.Text(originals[at])+")":"      input");}
 /* typed inputs: constants only; formulas stay as in the workbook */
 void EditCell(WbSheet s,int r,int c){var l=book.Layout(s);WbMerge m;if(l.MergeAt.TryGetValue(WbSheet.Key(r,c),out m)){r=m.R1;c=m.C1;}var cell=s.Get(r,c);
  if(cell!=null&&(cell.Formula!=null||cell.ArrayMaster!=null)){MessageBox.Show(this,s.Name+"!"+WbBook.ColumnName(c)+r+" is computed by a formula:\n="+cell.Formula+"\n\nChange the input cells it uses instead.",Text);return;}
  string current=cell==null?"":WbCalc.Text(cell.Cached);string typed=Prompt(s.Name+"!"+WbBook.ColumnName(c)+r,current);if(typed==null||typed==current)return;
  object value=typed;double d;if(typed.Trim()=="")value=null;else if(double.TryParse(typed.Trim().Replace(",",""),NumberStyles.Float,CultureInfo.InvariantCulture,out d))value=d;
  string at=s.Name+"!"+WbBook.ColumnName(c)+r;SetValue(at,value);dirty=true;UpdateTitle();Recalculate(at);}
 /* sets one typed value, remembering the template value it replaces (a value equal to the template's is no longer an edit) */
 void SetValue(string at,object value){int bang=at.LastIndexOf('!');var s=book.Sheet(at.Substring(0,bang));int r,c;if(s==null||!WbBook.ParseA1(at.Substring(bang+1),out r,out c))return;var cell=s.GetOrAdd(r,c);if(cell.Formula!=null||cell.ArrayMaster!=null)return;
  if(!originals.ContainsKey(at))originals[at]=cell.Cached;cell.Cached=value;if(Same(value,originals[at])){edits.Remove(at);originals.Remove(at);}else edits[at]=value;}
 static bool Same(object a,object b){if(a==null||(a is string&&(string)a==""))return b==null||(b is string&&(string)b=="");return a.Equals(b);}
 int Apply(Dictionary<string,object> values){int n=0;foreach(var kv in values){int before=edits.Count;SetValue(kv.Key,kv.Value);n++;}return n;}
 string Prompt(string title,string value){using(var f=new Form{Text=title,Width=460,Height=150,FormBorderStyle=FormBorderStyle.FixedDialog,StartPosition=FormStartPosition.CenterParent,MinimizeBox=false,MaximizeBox=false,Font=Font}){
   var box=new TextBox{Text=value,Left=12,Top=14,Width=420};var ok=new Button{Text="OK",DialogResult=DialogResult.OK,Left=262,Top=50,Width=80};var cancel=new Button{Text="Cancel",DialogResult=DialogResult.Cancel,Left=352,Top=50,Width=80};
   f.Controls.AddRange(new Control[]{box,ok,cancel});f.AcceptButton=ok;f.CancelButton=cancel;return f.ShowDialog(this)==DialogResult.OK?box.Text:null;}}
 void Recalculate(string changed){if(calc==null)return;Cursor=Cursors.WaitCursor;try{WbCalc.RunWithLargeStack(()=>{calc.Reset();calc.CalculateAll();});}finally{Cursor=Cursors.Default;}view.Invalidate();
  status.Text=(changed!=null?changed+" changed. ":"")+"All "+calc.Evaluated.ToString("N0")+" formulas recomputed."+(dirty?" Not saved yet (Save POW / Ctrl+S).":"");}
 /* POW project file (.pow.xml): the values typed in the app over the template */
 internal static string ProjectXml(Dictionary<string,object> values,string template){var doc=new XmlDocument();var root=doc.CreateElement("PowWorkbook");root.SetAttribute("version","1");root.SetAttribute("template",template??"");root.SetAttribute("app",Engine.Version);doc.AppendChild(root);
  foreach(var kv in values.OrderBy(k=>k.Key,StringComparer.OrdinalIgnoreCase)){var e=doc.CreateElement("Cell");e.SetAttribute("at",kv.Key);e.SetAttribute("type",kv.Value is double?"n":kv.Value is bool?"b":kv.Value==null?"":"s");if(kv.Value is double)e.InnerText=((double)kv.Value).ToString("R",CultureInfo.InvariantCulture);else if(kv.Value is bool)e.InnerText=(bool)kv.Value?"1":"0";else if(kv.Value!=null)e.InnerText=kv.Value.ToString();root.AppendChild(e);}
  using(var sw=new StringWriter()){using(var w=XmlWriter.Create(sw,new XmlWriterSettings{Indent=true,OmitXmlDeclaration=true}))doc.Save(w);return "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n"+sw.ToString();}}
 internal static Dictionary<string,object> ReadProject(string path){var doc=new XmlDocument{XmlResolver=null};using(var r=XmlReader.Create(path,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null}))doc.Load(r);
  if(doc.DocumentElement==null||doc.DocumentElement.Name!="PowWorkbook"){if(doc.DocumentElement!=null&&doc.DocumentElement.Name=="ProgramOfWorks")throw new Exception(Path.GetFileName(path)+" is a Bill of Quantities file. Open it in the Bill of Quantities workspace.");throw new Exception(Path.GetFileName(path)+" is not a Program of Works (POW) file.");}
  var values=new Dictionary<string,object>(StringComparer.OrdinalIgnoreCase);foreach(XmlElement e in doc.DocumentElement.GetElementsByTagName("Cell")){object v=null;string type=e.GetAttribute("type");if(type=="n"){double d;if(double.TryParse(e.InnerText,NumberStyles.Float,CultureInfo.InvariantCulture,out d))v=d;}else if(type=="b")v=e.InnerText=="1";else if(type=="s")v=e.InnerText;values[e.GetAttribute("at")]=v;}return values;}
 bool ConfirmDiscard(){if(!dirty)return true;var r=MessageBox.Show(this,"Save the changes to "+(projectPath==null?"this POW":Path.GetFileName(projectPath))+" first?",Text,MessageBoxButtons.YesNoCancel,MessageBoxIcon.Question);if(r==DialogResult.Cancel)return false;if(r==DialogResult.Yes)return SaveProject(false);return true;}
 bool SaveProject(bool saveAs){if(book==null)return false;string path=projectPath;if(saveAs||path==null){using(var d=new SaveFileDialog{Title="Save POW",Filter="Program of Works (POW)|*.pow.xml",FileName=path==null?"Program-of-Works.pow.xml":Path.GetFileName(path),OverwritePrompt=true}){if(d.ShowDialog(this)!=DialogResult.OK)return false;path=d.FileName;}}
  try{PowProjectFileWriter.AtomicWrite(path,ProjectXml(edits,templateName));}catch(Exception ex){MessageBox.Show(this,"The POW was not saved: "+ex.Message,Text);return false;}
  projectPath=path;dirty=false;Remember(path);UpdateTitle();status.Text="Saved "+Path.GetFileName(path)+" ("+edits.Count+" value(s) changed from the template).";return true;}
 void OpenProject(){if(book==null||!ConfirmDiscard())return;using(var d=new OpenFileDialog{Title="Open POW",Filter="Program of Works (POW)|*.pow.xml|All files|*.*"}){if(d.ShowDialog(this)!=DialogResult.OK)return;
   Dictionary<string,object> values;try{values=ReadProject(d.FileName);}catch(Exception ex){MessageBox.Show(this,ex.Message,Text);return;}
   foreach(var at in originals.Keys.ToList()){var o=originals[at];SetValue(at,o);}edits.Clear();originals.Clear();Apply(values);projectPath=d.FileName;dirty=false;Remember(d.FileName);Recalculate(null);UpdateTitle();status.Text="Opened "+Path.GetFileName(d.FileName)+" ("+values.Count+" saved value(s)).";}}
 void NewProject(){if(book==null||!ConfirmDiscard())return;foreach(var at in originals.Keys.ToList())SetValue(at,originals[at]);edits.Clear();originals.Clear();projectPath=null;dirty=false;try{File.Delete(LastProjectPath);}catch(IOException){}Recalculate(null);UpdateTitle();status.Text="New POW from the template. Change input cells, then Save POW.";}
 static void Remember(string path){try{File.WriteAllText(LastProjectPath,path);}catch(IOException){}catch(UnauthorizedAccessException){}}
 /* printing */
 void PrintSheets(bool preview){if(view.Sheet==null)return;Print(new List<WbSheet>{view.Sheet},preview);}
 void PrintDupas(){if(book==null)return;var detailed=book.Sheet("DETAILED");var codes=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  if(detailed!=null)for(int r=1;r<=detailed.MaxRow;r++){if(RowHidden(detailed,r))continue;var q=Val(detailed,r,51);var code=Val(detailed,r,2);if(q is double&&(double)q!=0&&code is string)codes.Add(((string)code).Trim());}
  var list=book.Sheets.Where(IsDupa).Where(s=>codes.Contains(Convert.ToString(Val(s,15,5)).Trim())).ToList();if(list.Count==0){MessageBox.Show(this,"No pay item has a quantity in DETAILED yet.",Text);return;}
  if(MessageBox.Show(this,"Print the DUPA of "+list.Count+" pay item(s) with a quantity?",Text,MessageBoxButtons.OKCancel)!=DialogResult.OK)return;Print(list,true);}
 void Print(List<WbSheet> sheets,bool preview){var job=new WbPrintJob(render,sheets);using(var doc=job.Document()){doc.DocumentName="POW - "+string.Join(", ",sheets.Take(3).Select(s=>s.Name));
   if(preview){using(var dlg=new PrintPreviewDialog{Document=doc,Width=1100,Height=800,UseAntiAlias=true})dlg.ShowDialog(this);}else{using(var dlg=new PrintDialog{Document=doc,UseEXDialog=true,AllowSomePages=true}){if(dlg.ShowDialog(this)==DialogResult.OK)doc.Print();}}}}
}
/* on-screen sheet: print area, zoom, selection; double-click asks to edit */
sealed class WbSheetView : ScrollableControl {
 WbSheet sheet;float zoom=1;public WbRender Render;public event Action<WbSheet,int,int> CellActivated,CellSelected;int selR,selC;
 public WbSheetView(){DoubleBuffered=true;AutoScroll=true;BackColor=Color.FromArgb(230,232,236);SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.UserPaint|ControlStyles.Selectable,true);}
 public WbSheet Sheet{get{return sheet;}set{sheet=value;selR=0;selC=0;AutoScrollPosition=Point.Empty;Measure();Invalidate();}}
 public float Zoom{get{return zoom;}set{zoom=value;Measure();Invalidate();}}
 float px;float Px{get{if(px==0){using(var g=CreateGraphics())px=g.DpiX/72f;}return px;}}const int Margin=16;
 WbSheetLayout L{get{return Render.Book.Layout(sheet);}}
 void Measure(){if(sheet==null||Render==null){AutoScrollMinSize=Size.Empty;return;}var l=L;double w=0,h=0;for(int c=l.PrintC1;c<=l.PrintC2;c++)w+=Render.ColWidthPt(l,c);for(int r=l.PrintR1;r<=l.PrintR2;r++)h+=Render.RowHeightPt(sheet,l,r);AutoScrollMinSize=new Size((int)(w*Px*zoom)+2*Margin,(int)(h*Px*zoom)+2*Margin);}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(sheet==null||Render==null)return;var l=L;var g=e.Graphics;
  g.TranslateTransform(AutoScrollPosition.X+Margin,AutoScrollPosition.Y+Margin);double w=0,h=0;for(int c=l.PrintC1;c<=l.PrintC2;c++)w+=Render.ColWidthPt(l,c);for(int r=l.PrintR1;r<=l.PrintR2;r++)h+=Render.RowHeightPt(sheet,l,r);
  g.FillRectangle(Brushes.White,0,0,(float)(w*Px*zoom),(float)(h*Px*zoom));WbRender.PointSpace(g,zoom);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
  /* visible rows in points */
  float top=(-AutoScrollPosition.Y-Margin)/(Px*zoom),bottom=top+ClientSize.Height/(Px*zoom);int r1=l.PrintR1;float y=0;while(r1<l.PrintR2&&y+(float)Render.RowHeightPt(sheet,l,r1)<top){y+=(float)Render.RowHeightPt(sheet,l,r1);r1++;}
  int r2=r1;float y2=y;while(r2<l.PrintR2&&y2<bottom){y2+=(float)Render.RowHeightPt(sheet,l,r2);r2++;}
  /* start drawing a little above so merged blocks crossing the top edge are drawn */int back=Math.Max(l.PrintR1,r1-20);float yb=y;for(int r=back;r<r1;r++)yb-=(float)Render.RowHeightPt(sheet,l,r);
  Render.Draw(g,sheet,back,l.PrintC1,r2,l.PrintC2,0,yb);
  if(selR>0){var rc=CellRect(selR,selC);if(!rc.IsEmpty)using(var p=new Pen(Color.FromArgb(0,120,212),1.2f))g.DrawRectangle(p,rc.X,rc.Y,rc.Width,rc.Height);}}
 RectangleF CellRect(int r,int c){var l=L;WbMerge m;int r2=r,c2=c;if(l.MergeAt.TryGetValue(WbSheet.Key(r,c),out m)){r=m.R1;c=m.C1;r2=m.R2;c2=m.C2;}float x=0,y=0,w=0,h=0;for(int k=l.PrintC1;k<c;k++)x+=(float)Render.ColWidthPt(l,k);for(int k=c;k<=c2;k++)w+=(float)Render.ColWidthPt(l,k);for(int k=l.PrintR1;k<r;k++)y+=(float)Render.RowHeightPt(sheet,l,k);for(int k=r;k<=r2;k++)h+=(float)Render.RowHeightPt(sheet,l,k);return new RectangleF(x,y,w,h);}
 bool Hit(Point p,out int row,out int col){row=0;col=0;if(sheet==null||Render==null)return false;var l=L;float x=(p.X-AutoScrollPosition.X-Margin)/(Px*zoom),y=(p.Y-AutoScrollPosition.Y-Margin)/(Px*zoom);if(x<0||y<0)return false;
  float acc=0;for(int c=l.PrintC1;c<=l.PrintC2;c++){acc+=(float)Render.ColWidthPt(l,c);if(x<acc){col=c;break;}}acc=0;for(int r=l.PrintR1;r<=l.PrintR2;r++){acc+=(float)Render.RowHeightPt(sheet,l,r);if(y<acc){row=r;break;}}return row>0&&col>0;}
 protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);Focus();int r,c;if(Hit(e.Location,out r,out c)){var l=L;WbMerge m;if(l.MergeAt.TryGetValue(WbSheet.Key(r,c),out m)){r=m.R1;c=m.C1;}selR=r;selC=c;Invalidate();if(CellSelected!=null)CellSelected(sheet,r,c);}}
 protected override void OnMouseDoubleClick(MouseEventArgs e){base.OnMouseDoubleClick(e);int r,c;if(Hit(e.Location,out r,out c)&&CellActivated!=null)CellActivated(sheet,r,c);}
 protected override void OnMouseWheel(MouseEventArgs e){if((ModifierKeys&Keys.Control)!=0){Zoom=Math.Max(0.3f,Math.Min(4f,zoom*(e.Delta>0?1.1f:1/1.1f)));return;}base.OnMouseWheel(e);}
}
/* A4 pages for a list of sheets: print area, fit-to-width or scale, manual row breaks, centred, header / footer with picture */
sealed class WbPrintJob {
 readonly WbRender render;readonly List<WbSheet> sheets;sealed class Page{public WbSheet Sheet;public int R1,R2;public float Scale;public int Number,Of;}readonly List<Page> pages=new List<Page>();int next;
 public WbPrintJob(WbRender r,List<WbSheet> s){render=r;sheets=s;}
 public PrintDocument Document(){var doc=new PrintDocument();var a4=new PaperSize("A4",827,1169);doc.DefaultPageSettings.PaperSize=a4;doc.DefaultPageSettings.Margins=new Margins(0,0,0,0);doc.OriginAtMargins=false;
  doc.BeginPrint+=(s,e)=>{Paginate();next=0;};doc.QueryPageSettings+=(s,e)=>{if(next<pages.Count){e.PageSettings.Landscape=render.Book.Layout(pages[next].Sheet).Landscape;e.PageSettings.PaperSize=a4;}};
  doc.PrintPage+=(s,e)=>{if(next>=pages.Count){e.HasMorePages=false;return;}DrawPage(e.Graphics,pages[next]);next++;e.HasMorePages=next<pages.Count;};return doc;}
 /* page images (used by the self-test and for saving pictures of the printout) */
 public List<Bitmap> Images(float dpi,int max){Paginate();var list=new List<Bitmap>();foreach(var p in pages.Take(max)){var l=render.Book.Layout(p.Sheet);float pw=l.Landscape?842f:595f,ph=l.Landscape?595f:842f;var bmp=new Bitmap((int)(pw*dpi/72),(int)(ph*dpi/72));bmp.SetResolution(dpi,dpi);using(var g=Graphics.FromImage(bmp)){g.Clear(Color.White);DrawPage(g,p);}list.Add(bmp);}return list;}
 public int PageCount{get{Paginate();return pages.Count;}}
 void Paginate(){pages.Clear();foreach(var s in sheets){var l=render.Book.Layout(s);float pw=(l.Landscape?842f:595f),ph=(l.Landscape?595f:842f);float aw=pw-(float)((l.MarginL+l.MarginR)*72),ah=ph-(float)((l.MarginT+l.MarginB)*72);
   double w=0;for(int c=l.PrintC1;c<=l.PrintC2;c++)w+=render.ColWidthPt(l,c);float scale=l.FitToPage?(l.FitWidth>0?Math.Min(1f,aw/(float)Math.Max(1,w)):1f):l.Scale/100f;
   if(l.FitToPage&&l.FitHeight>0){double h=0;for(int r=l.PrintR1;r<=l.PrintR2;r++)h+=render.RowHeightPt(s,l,r);scale=Math.Min(scale,ah*l.FitHeight/(float)Math.Max(1,h));}
   var breaks=new HashSet<int>(l.RowBreaks);int start=l.PrintR1;float used=0;var mine=new List<Page>();
   for(int r=l.PrintR1;r<=l.PrintR2;r++){float h=(float)render.RowHeightPt(s,l,r)*scale;if(used+h>ah&&r>start){mine.Add(new Page{Sheet=s,R1=start,R2=r-1,Scale=scale});start=r;used=0;}used+=h;if(breaks.Contains(r)&&r<l.PrintR2){mine.Add(new Page{Sheet=s,R1=start,R2=r,Scale=scale});start=r+1;used=0;}}
   if(start<=l.PrintR2)mine.Add(new Page{Sheet=s,R1=start,R2=l.PrintR2,Scale=scale});
   /* skip pages whose rows are all hidden */mine=mine.Where(p=>Enumerable.Range(p.R1,p.R2-p.R1+1).Any(r=>render.RowHeightPt(s,l,r)>0)).ToList();for(int i=0;i<mine.Count;i++){mine[i].Number=i+1;mine[i].Of=mine.Count;}pages.AddRange(mine);}}
 void DrawPage(Graphics g,Page p){var l=render.Book.Layout(p.Sheet);WbRender.PointSpace(g,1);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAlias;
  float pw=l.Landscape?842f:595f,ph=l.Landscape?595f:842f;double w=0;for(int c=l.PrintC1;c<=l.PrintC2;c++)w+=render.ColWidthPt(l,c);float left=(float)(l.MarginL*72),top=(float)(l.MarginT*72);
  if(l.CenterH){float aw=pw-(float)((l.MarginL+l.MarginR)*72);left+=Math.Max(0,(aw-(float)w*p.Scale)/2);}
  var state=g.Save();g.TranslateTransform(left,top);g.ScaleTransform(p.Scale,p.Scale);render.Draw(g,p.Sheet,p.R1,l.PrintC1,p.R2,l.PrintC2,0,0);g.Restore(state);
  HeaderFooter(g,l.Header,l.HeaderPicture,p,pw,(float)(l.MarginHeader*72),true);HeaderFooter(g,l.Footer,l.FooterPicture,p,pw,ph-(float)(l.MarginFooter*72),false);}
 void HeaderFooter(Graphics g,string code,byte[] picture,Page p,float pw,float y,bool header){if(string.IsNullOrEmpty(code))return;var parts=new Dictionary<char,string>{{'L',""},{'C',""},{'R',""}};char cur='C';
  for(int i=0;i<code.Length;i++){if(code[i]=='&'&&i+1<code.Length&&"LCR".IndexOf(code[i+1])>=0){cur=code[i+1];i++;continue;}parts[cur]+=code[i];}
  foreach(var kv in parts){var text=kv.Value;if(text=="")continue;float size=8;var m=System.Text.RegularExpressions.Regex.Match(text,@"&(\d+)");if(m.Success)size=Math.Max(5,float.Parse(m.Groups[1].Value,CultureInfo.InvariantCulture));
   bool pic=text.Contains("&G");text=System.Text.RegularExpressions.Regex.Replace(text,"&\"[^\"]*\"","");text=System.Text.RegularExpressions.Regex.Replace(text,@"&\d+","");text=text.Replace("&P",p.Number.ToString()).Replace("&N",p.Of.ToString()).Replace("&A",p.Sheet.Name).Replace("&D",DateTime.Today.ToString("M/d/yyyy")).Replace("&G","").Replace("&&","\u0001").Replace("&B","").Replace("&I","").Replace("&U","").Replace("\u0001","&");
   float x=kv.Key=='L'?36:kv.Key=='C'?pw/2:pw-36;
   if(pic&&picture!=null){try{using(var img=Image.FromStream(new MemoryStream(picture))){float ih=Math.Min(40,img.Height*72f/Math.Max(1,img.VerticalResolution)),iw=ih*img.Width/Math.Max(1,img.Height);float ix=kv.Key=='L'?x:kv.Key=='C'?x-iw/2:x-iw;g.DrawImage(img,ix,header?y:y-ih,iw,ih);}}catch(ArgumentException){}}
   if(text.Trim()=="")continue;using(var font=new Font("Arial",size,GraphicsUnit.World)){var sz=g.MeasureString(text,font);float tx=kv.Key=='L'?x:kv.Key=='C'?x-sz.Width/2:x-sz.Width;g.DrawString(text,font,Brushes.Black,new RectangleF(tx,header?y:y-sz.Height,sz.Width+20,sz.Height+4));}}}
}
}
