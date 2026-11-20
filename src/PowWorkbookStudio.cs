using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* POW Studio layout (owner, 20 November 2026: "ganitong UI pero huwag gagayahin yung print at dupa", with the POW Studio page as the
   model): a dark workflow sidebar (1 Project details, 2 Hauling & prices, 3 Pay items & quantities, 4 Review & print), a header with
   New / Open / Save project, the project title with its location and contract duration, and a step progress line. Steps 1 and 3 are
   forms and tables bound to the workbook's input cells; steps 2 and 4 keep the app's own Excel-like sheets, DUPA and printing. */
sealed partial class PowWorkbookForm {
 internal static readonly Color StudioDark=Color.FromArgb(20,56,44),StudioActive=Color.FromArgb(43,93,71),StudioGreen=Color.FromArgb(35,117,86),StudioInk=Color.FromArgb(32,49,46),StudioMuted=Color.FromArgb(120,136,128),StudioLine=Color.FromArgb(224,231,225),StudioBack=Color.FromArgb(247,249,248),StudioSoft=Color.FromArgb(237,244,239);
 readonly Panel[] steps=new Panel[4];readonly StudioStep[] stepButtons=new StudioStep[4];int currentStep;readonly Label titleLabel=new Label{AutoSize=false,Height=58,MaximumSize=new Size(0,58),Dock=DockStyle.Top,Font=new Font("Segoe UI",14f,FontStyle.Bold),ForeColor=StudioInk,AutoEllipsis=true,UseMnemonic=false};
 readonly Label metaLabel=new Label{Dock=DockStyle.Top,Height=24,ForeColor=StudioMuted,AutoEllipsis=true,UseMnemonic=false};readonly StudioProgress progress=new StudioProgress{Dock=DockStyle.Top,Height=34};
 readonly Label projectCard=new Label{Dock=DockStyle.Top,Height=60,ForeColor=Color.White,Padding=new Padding(14,8,10,8),Font=new Font("Segoe UI",9f,FontStyle.Bold)};readonly Label savedNote=new Label{Dock=DockStyle.Bottom,Height=48,ForeColor=Color.FromArgb(170,196,182),Font=new Font("Segoe UI",8.5f),Padding=new Padding(16,0,8,8)};
 readonly List<StudioField> fields=new List<StudioField>();readonly DataGridView items=new DataGridView{Dock=DockStyle.Fill};readonly TextBox itemSearch=new TextBox{Width=240};readonly CheckBox itemsAll=new CheckBox{Text="Show all pay items",AutoSize=true};
 readonly Label statTotal=new Label(),statDirect=new Label(),statItems=new Label();readonly NavigationTabs rates=new NavigationTabs{Dock=DockStyle.Fill};Panel formulaBar;bool loadingItems;
 static readonly string[] StepNames={"Project details","Hauling & prices","Pay items & quantities","Review & print"};
 void BuildStudio(Control formula){SuspendLayout();Controls.Clear();BackColor=StudioBack;formulaBar=(Panel)formula;
  /* sidebar */
  var side=new Panel{Dock=DockStyle.Left,Width=236,BackColor=StudioDark,Padding=new Padding(16,22,16,10)};
  var brand=new Label{Dock=DockStyle.Top,Height=58,ForeColor=Color.White,Font=new Font("Segoe UI",18f,FontStyle.Bold),Text="POW",TextAlign=ContentAlignment.TopLeft};var sub=new Label{Dock=DockStyle.Top,Height=26,ForeColor=Color.FromArgb(150,190,170),Font=new Font("Segoe UI",7.5f,FontStyle.Bold),Text="PROGRAM OF WORKS"};
  projectCard.BackColor=Color.FromArgb(30,72,57);var flow=new Label{Dock=DockStyle.Top,Height=34,ForeColor=Color.FromArgb(150,190,170),Font=new Font("Segoe UI",7.5f,FontStyle.Bold),Text="YOUR WORKFLOW",TextAlign=ContentAlignment.BottomLeft};
  var stepList=new Panel{Dock=DockStyle.Top,Height=4*52+8,BackColor=StudioDark};for(int i=3;i>=0;i--){int k=i;var b=new StudioStep{Text=(i+1)+" · "+StepNames[i],Dock=DockStyle.Top,Height=50,Margin=new Padding(0,0,0,4)};b.Click+=(s,e)=>ShowStep(k);stepButtons[i]=b;stepList.Controls.Add(b);stepList.Controls.Add(new Panel{Dock=DockStyle.Top,Height=2,BackColor=StudioDark});}
  side.Controls.Add(savedNote);side.Controls.Add(stepList);side.Controls.Add(flow);side.Controls.Add(new Panel{Dock=DockStyle.Top,Height=10,BackColor=StudioDark});side.Controls.Add(projectCard);side.Controls.Add(sub);side.Controls.Add(brand);
  /* header */
  var header=new Panel{Dock=DockStyle.Top,Height=58,BackColor=Color.White,Padding=new Padding(28,12,20,12)};header.Paint+=(s,e)=>{using(var p=new Pen(StudioLine))e.Graphics.DrawLine(p,0,header.Height-1,header.Width,header.Height-1);};
  var crumb=new Label{Dock=DockStyle.Left,Width=360,Text="Workspace   /   Program of Works (POW)",ForeColor=StudioMuted,TextAlign=ContentAlignment.MiddleLeft};
  var actions=new FlowLayoutPanel{Dock=DockStyle.Right,AutoSize=true,WrapContents=false,BackColor=Color.White,Padding=Padding.Empty};
  Func<string,Action,bool,Button> act=(t,a,primary)=>{var b=new Button{Text=t,AutoSize=true,Height=34,Margin=new Padding(8,0,0,0),FlatStyle=FlatStyle.Flat};b.FlatAppearance.BorderColor=primary?StudioGreen:StudioLine;b.BackColor=primary?StudioGreen:Color.White;b.ForeColor=primary?Color.White:StudioInk;b.Click+=(s,e)=>{try{a();}catch(Exception ex){MessageBox.Show(this,ex.Message,"Program of Works (POW)");}};actions.Controls.Add(b);return b;};
  var more=new ContextMenuStrip();more.Items.Add("Save project as…",null,(s,e)=>SaveProject(true));more.Items.Add("Recalculate",null,(s,e)=>Recalculate(null));more.Items.Add(new ToolStripSeparator());more.Items.Add("Use another POW workbook…",null,(s,e)=>ImportWorkbook());
  act("New project",NewProject,false);act("Open project",OpenProject,false);var mb=act("More  ▾",()=>{},false);mb.Click+=(s,e)=>more.Show(mb,new Point(0,mb.Height));act("Save project",()=>SaveProject(false),true);
  header.Controls.Add(crumb);header.Controls.Add(actions);
  /* title area and steps */
  var body=new Panel{Dock=DockStyle.Fill,BackColor=StudioBack,Padding=new Padding(28,16,28,10)};var host=new Panel{Dock=DockStyle.Fill,BackColor=StudioBack};
  var eyebrow=new Label{Dock=DockStyle.Top,Height=20,Text="ESTIMATE WORKSPACE",ForeColor=Color.FromArgb(103,133,114),Font=new Font("Segoe UI",7.5f,FontStyle.Bold)};
  for(int i=0;i<4;i++){steps[i]=new Panel{Dock=DockStyle.Fill,BackColor=StudioBack,Visible=false};host.Controls.Add(steps[i]);}
  body.Controls.Add(host);body.Controls.Add(new Panel{Dock=DockStyle.Top,Height=8,BackColor=StudioBack});body.Controls.Add(progress);body.Controls.Add(metaLabel);body.Controls.Add(titleLabel);body.Controls.Add(eyebrow);
  status.Dock=DockStyle.Bottom;status.BackColor=Color.White;status.ForeColor=StudioMuted;
  Controls.Add(body);Controls.Add(header);Controls.Add(side);Controls.Add(status);
  BuildDetailsStep(steps[0]);BuildRatesStep(steps[1]);BuildItemsStep(steps[2]);BuildReviewStep(steps[3]);ShowStep(0);ResumeLayout(true);}
 void ShowStep(int i){currentStep=i;for(int k=0;k<4;k++){steps[k].Visible=k==i;stepButtons[k].Active=k==i;}progress.Step=i;progress.Caption="STEP "+(i+1)+" OF 4";progress.Invalidate();
  if(i==1){var page=rates.SelectedTab;if(page!=null)PlaceSheet(page);}else if(i==3)ShowTab();else if(i==2)FillItems();else if(i==0)RefreshFields();}
 /* puts the shared sheet view (and formula bar) on a page */
 void PlaceSheet(TabPage page){var inner=page.Controls.OfType<NavigationTabs>().FirstOrDefault();if(inner!=null){if(inner.SelectedTab!=null)PlaceSheet(inner.SelectedTab);return;}var s=page.Tag as WbSheet;if(s==null)return;
  if(view.Parent!=page){page.Controls.Add(view);page.Controls.Add(formulaBar);}ShowSheet(s);}
 /* step 1: project details bound to FRONT PAGE inputs */
 static readonly string[][] DetailCards={
  new[]{"01","Project details","Project name, location and implementing office as printed on every sheet.","Implementing office|FRONT PAGE!L6","Address|FRONT PAGE!L7","Project name (line 1)|FRONT PAGE!L8","Project name (line 2)|FRONT PAGE!L9","Project name (line 3)|FRONT PAGE!L10","Project name (line 4)|FRONT PAGE!L11","Project location|FRONT PAGE!L12","Work location / coordinates|FRONT PAGE!L13","Date prepared|FRONT PAGE!CB6|date"},
  new[]{"02","Allocation & EAO","Allotted amount, EAO and program details.","Allotted amount|FRONT PAGE!DA16|money","EAO (%) of allotted amount|FRONT PAGE!CP55|percent","Project component ID|FRONT PAGE!CA16","Funding agreement|FRONT PAGE!CM20","Funding organization|FRONT PAGE!DA20","Project category|FRONT PAGE!EA33","Program|FRONT PAGE!EA34","Infrastructure type|FRONT PAGE!CA25","Target amount|FRONT PAGE!CY25","Unit of measure|FRONT PAGE!DF25"},
  new[]{"03","Building & scope","Used by OSH and contract duration.","No. of stories|FRONT PAGE!EA40","Total floor area (sq.m.)|CD!L16","Scope of works|FRONT PAGE!DS4|multi"}};
 void BuildDetailsStep(Panel step){var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=StudioBack};var grid=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=2,BackColor=StudioBack,Padding=new Padding(0,4,8,8)};
  grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
  foreach(var card in DetailCards){var c=new StudioCard{Number=card[0],Title=card[1],Subtitle=card[2],Anchor=AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Top,Margin=new Padding(0,0,16,16)};int h=96;var form=new TableLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,ColumnCount=1,BackColor=Color.White};
   foreach(var spec in card.Skip(3)){var p=spec.Split('|');var lbl=new Label{Text=p[0],AutoSize=true,ForeColor=StudioInk,Margin=new Padding(0,8,0,4),UseMnemonic=false};Control input;string kind=p.Length>2?p[2]:"text";
    if(kind=="date")input=new DateTimePicker{Format=DateTimePickerFormat.Long,Width=260};else input=new TextBox{Dock=DockStyle.Top,Multiline=kind=="multi",Height=kind=="multi"?110:26,ScrollBars=kind=="multi"?ScrollBars.Vertical:ScrollBars.None};
    input.Margin=new Padding(0,0,0,2);input.Anchor=AnchorStyles.Left|AnchorStyles.Right;var f=new StudioField{At=p[1],Kind=kind,Input=input};fields.Add(f);Bind(f);form.Controls.Add(lbl);form.Controls.Add(input);h+=kind=="multi"?140:56;}
   var summary=new Label{Dock=DockStyle.Bottom,Height=card[0]=="02"?56:0,BackColor=StudioSoft,ForeColor=StudioGreen,Font=new Font("Segoe UI",10f,FontStyle.Bold),Padding=new Padding(12,0,12,0),TextAlign=ContentAlignment.MiddleLeft,Tag="eao"};
   c.Body.Controls.Add(form);if(card[0]=="02"){c.Body.Controls.Add(summary);h+=64;}c.Height=h;grid.Controls.Add(c);}
  scroll.Controls.Add(grid);scroll.Resize+=(s,e)=>grid.Width=scroll.ClientSize.Width-4;step.Controls.Add(scroll);}
 void Bind(StudioField f){var box=f.Input as TextBox;var date=f.Input as DateTimePicker;
  if(box!=null){box.Leave+=(s,e)=>CommitField(f);box.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Enter&&!box.Multiline){e.SuppressKeyPress=true;CommitField(f);}};}
  if(date!=null)date.CloseUp+=(s,e)=>CommitField(f);}
 void CommitField(StudioField f){if(book==null||f.Loading)return;object value;var date=f.Input as DateTimePicker;
  if(date!=null)value=WbCalc.Serial(date.Value);else{string t=f.Input.Text.Trim();double d;if(t=="")value=null;else if(f.Kind=="percent"&&double.TryParse(t.TrimEnd('%').Replace(",",""),NumberStyles.Float,CultureInfo.InvariantCulture,out d))value=t.EndsWith("%")||d>1?d/100:d;
   else if((f.Kind=="money"||IsNumberCell(f.At))&&double.TryParse(t.Replace(",","").Replace("₱",""),NumberStyles.Float,CultureInfo.InvariantCulture,out d))value=d;else value=f.Input.Text;}
  if(Same(CurrentValue(f.At),value))return;SetValue(f.At,value);dirty=true;UpdateTitle();Recalculate(f.At);RefreshFields();}
 bool IsNumberCell(string at){return CurrentValue(at) is double;}
 void RefreshFields(){if(book==null)return;foreach(var f in fields){f.Loading=true;try{var v=FieldValue(f.At);var date=f.Input as DateTimePicker;
    if(date!=null){if(v is double){try{date.Value=WbCalc.FromSerial((double)v);}catch(ArgumentException){}}}
    else if(!f.Input.Focused)f.Input.Text=v==null?"":f.Kind=="money"&&v is double?((double)v).ToString("#,##0.00",CultureInfo.InvariantCulture):f.Kind=="percent"&&v is double?((double)v*100).ToString("0.###",CultureInfo.InvariantCulture)+"%":WbCalc.Text(v);}finally{f.Loading=false;}}
  var front=book.Sheet("FRONT PAGE");
  foreach(var lbl in Descendants(steps[0]).OfType<Label>().Where(x=>"eao".Equals(x.Tag)))lbl.Text="EAO allocation  ₱"+Money(Val(front,17,91))+"      Component cost  ₱"+Money(Val(front,16,91))+"      Balance  ₱"+Money(Val(front,38,131));
  UpdateHeader();}
 object FieldValue(string at){int bang=at.LastIndexOf('!');var s=book.Sheet(at.Substring(0,bang));int r,c;if(s==null||!WbBook.ParseA1(at.Substring(bang+1),out r,out c))return null;return Val(s,r,c);}
 static IEnumerable<Control> Descendants(Control c){foreach(Control k in c.Controls){yield return k;foreach(var d in Descendants(k))yield return d;}}
 static string Money(object v){return v is double?((double)v).ToString("#,##0.00",CultureInfo.InvariantCulture):"-";}
 void UpdateHeader(){if(book==null)return;var front=book.Sheet("FRONT PAGE");string name=string.Join(" ",new[]{8,9,10,11}.Select(r=>WbCalc.Text(Val(front,r,12)).Trim()).Where(x=>x!=""));
  titleLabel.Text=name==""?"Untitled program of works":name;string loc=WbCalc.Text(Val(front,12,12));var cd=Val(front,6,111);var total=Val(book.Sheet("SUM")??front,45,106);
  metaLabel.Text="⌖  "+(loc==""?"Project location not set":loc)+"        ◷  "+(cd is double?((double)cd).ToString("0")+" calendar days":"-")+"        ₱  "+Money(total)+" total cost";
  projectCard.Text=(projectPath==null?"New POW (not saved)":System.IO.Path.GetFileName(projectPath))+"\n"+templateName;savedNote.Text=dirty?"● Changes not saved\nSave project keeps your values (.pow.xml).":"● Saved\nKeep a backup with Save project.";}
 /* step 2: rates as sheets */
 void BuildRatesStep(Panel step){var head=new Label{Dock=DockStyle.Top,Height=46,Text="Rates and prices used by every DUPA. Select a cell and type in the bar above the sheet (Enter applies).",ForeColor=StudioMuted,TextAlign=ContentAlignment.MiddleLeft};
  rates.SelectedIndexChanged+=(s,e)=>{if(currentStep==1&&rates.SelectedTab!=null)PlaceSheet(rates.SelectedTab);};step.Controls.Add(rates);step.Controls.Add(head);}
 void FillRates(){rates.TabPages.Clear();if(book==null)return;Func<string,TabPage> sheetPage=n=>{var s=book.Sheet(n);return s==null?null:new TabPage(n=="Labor"?"Labor rates":n){Tag=s};};
  var haul=new TabPage("Hauling"){Tag="haul"};var inner=new NavigationTabs{Dock=DockStyle.Fill};foreach(var s in book.Sheets.Where(x=>x.Name.StartsWith("Hauling - ")))inner.TabPages.Add(new TabPage(s.Name.Substring(10)){Tag=s});
  if(inner.TabPages.Count>0){haul.Controls.Add(inner);inner.SelectedIndexChanged+=(s,e)=>{if(currentStep==1&&inner.SelectedTab!=null)PlaceSheet(inner.SelectedTab);};rates.TabPages.Add(haul);}
  foreach(var n in new[]{"CMPD","Labor","PERR","Indirect Cost"}){var p=sheetPage(n);if(p!=null)rates.TabPages.Add(p);}}
 /* step 3: pay items with quantities, unit cost and amount (DETAILED); quantity edits go to BACKUP */
 void BuildItemsStep(Panel step){var stats=new TableLayoutPanel{Dock=DockStyle.Top,Height=96,ColumnCount=3,BackColor=StudioBack,Padding=new Padding(0,0,0,12)};for(int i=0;i<3;i++)stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.3f));
  Func<string,Label,Control> stat=(t,v)=>{var card=new StudioCard{Title=t,Dock=DockStyle.Fill,Margin=new Padding(0,0,14,0),Compact=true};v.Dock=DockStyle.Fill;v.Font=new Font("Segoe UI",16f,FontStyle.Bold);v.ForeColor=StudioInk;v.TextAlign=ContentAlignment.MiddleLeft;card.Body.Controls.Add(v);return card;};
  stats.Controls.Add(stat("TOTAL PROJECT COST",statTotal));stats.Controls.Add(stat("ESTIMATED DIRECT COST",statDirect));stats.Controls.Add(stat("PAY ITEMS WITH QUANTITY",statItems));statTotal.ForeColor=StudioGreen;
  var tools=new FlowLayoutPanel{Dock=DockStyle.Top,Height=44,WrapContents=false,BackColor=StudioBack,Padding=new Padding(0,6,0,6)};tools.Controls.Add(new Label{Text="Search",AutoSize=true,Margin=new Padding(0,6,6,0),ForeColor=StudioMuted});tools.Controls.Add(itemSearch);itemsAll.Margin=new Padding(16,4,0,0);tools.Controls.Add(itemsAll);
  var upd=new Button{Text="Update from Bill of Quantities",AutoSize=true,Height=30,Margin=new Padding(16,0,0,0)};upd.Click+=(s,e)=>SyncBoq(true);linkBoq.Margin=new Padding(16,5,0,0);tools.Controls.Add(linkBoq);tools.Controls.Add(upd);
  var openDupa=new Button{Text="Open DUPA",AutoSize=true,Height=30,Margin=new Padding(8,0,0,0)};openDupa.Click+=(s,e)=>OpenSelectedDupa();tools.Controls.Add(openDupa);
  itemSearch.TextChanged+=(s,e)=>FillItems();itemsAll.CheckedChanged+=(s,e)=>FillItems();
  items.AllowUserToAddRows=false;items.AllowUserToDeleteRows=false;items.RowHeadersVisible=false;items.SelectionMode=DataGridViewSelectionMode.FullRowSelect;items.BackgroundColor=Color.White;items.BorderStyle=BorderStyle.None;items.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;
  foreach(var h in new[]{"Item no.","Description","Unit","Quantity","Unit cost","Amount"}){var col=new DataGridViewTextBoxColumn{HeaderText=h,ReadOnly=h!="Quantity",SortMode=DataGridViewColumnSortMode.NotSortable};items.Columns.Add(col);}
  items.Columns[0].FillWeight=40;items.Columns[1].FillWeight=180;items.Columns[2].FillWeight=30;items.Columns[3].FillWeight=50;items.Columns[4].FillWeight=60;items.Columns[5].FillWeight=70;foreach(int k in new[]{3,4,5})items.Columns[k].DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;
  items.Columns[3].DefaultCellStyle.BackColor=Color.FromArgb(255,253,240);items.CellEndEdit+=(s,e)=>{if(!loadingItems&&e.ColumnIndex==3)CommitQuantity(items.Rows[e.RowIndex]);};items.CellDoubleClick+=(s,e)=>{if(e.RowIndex>=0&&e.ColumnIndex!=3)OpenSelectedDupa();};
  var wrap=new Panel{Dock=DockStyle.Fill,BackColor=Color.White,Padding=new Padding(1)};wrap.Controls.Add(items);step.Controls.Add(wrap);step.Controls.Add(tools);step.Controls.Add(stats);}
 sealed class ItemRow{public int DetailedRow,BackupRow;public string Code;}
 void FillItems(){if(book==null)return;var det=book.Sheet("DETAILED");if(det==null)return;loadingItems=true;items.SuspendLayout();try{items.Rows.Clear();string q=itemSearch.Text.Trim();int withQty=0;
   for(int r=1;r<=det.MaxRow;r++){var codeCell=det.Get(r,2);if(codeCell==null||codeCell.Formula==null)continue;var code=Val(det,r,2) as string;if(string.IsNullOrWhiteSpace(code))continue;var qtyCell=det.Get(r,51);if(qtyCell==null||qtyCell.Formula==null)continue;
    var qty=Val(det,r,51);bool has=qty is double&&(double)qty!=0;if(has)withQty++;if(!has&&!itemsAll.Checked)continue;string desc=WbCalc.Text(Val(det,r,7));if(q!=""&&code.IndexOf(q,StringComparison.OrdinalIgnoreCase)<0&&desc.IndexOf(q,StringComparison.OrdinalIgnoreCase)<0)continue;
    int backup=0;var m=System.Text.RegularExpressions.Regex.Match(qtyCell.Formula,@"^BACKUP!\$?BC\$?(\d+)$",System.Text.RegularExpressions.RegexOptions.IgnoreCase);if(m.Success)backup=int.Parse(m.Groups[1].Value);
    int i=items.Rows.Add(code,desc,WbCalc.Text(Val(det,r,57)),has?((double)qty).ToString("#,##0.###",CultureInfo.InvariantCulture):"",Money(Val(det,r,126)),has?Money(Val(det,r,115)):"");items.Rows[i].Tag=new ItemRow{DetailedRow=r,BackupRow=backup,Code=code};if(backup==0)items.Rows[i].Cells[3].ReadOnly=true;}
   var sum=book.Sheet("SUM");statTotal.Text="₱ "+Money(sum==null?null:Val(sum,45,106));statDirect.Text="₱ "+Money(sum==null?null:Val(sum,45,46));statItems.Text=withQty.ToString();}finally{items.ResumeLayout();loadingItems=false;}}
 void CommitQuantity(DataGridViewRow row){var info=row.Tag as ItemRow;if(info==null||info.BackupRow==0)return;string t=Convert.ToString(row.Cells[3].Value).Trim().Replace(",","");double d;object v=null;if(t!=""){if(!double.TryParse(t,NumberStyles.Float,CultureInfo.InvariantCulture,out d)){FillItems();return;}v=d;}
  string at="BACKUP!BC"+info.BackupRow;if(Same(CurrentValue(at),v))return;if(linkBoq.Checked&&ProgramOfWorksForm.OpenWorkspace!=null)status.Text="Note: BACKUP is linked to the Bill of Quantities; a linked quantity is replaced when the Bill of Quantities changes.";
  SetValue(at,v);dirty=true;UpdateTitle();Recalculate(at);FillItems();}
 void OpenSelectedDupa(){var row=items.CurrentRow;var info=row==null?null:row.Tag as ItemRow;if(info==null)return;var det=book.Sheet("DETAILED");var cell=det.Get(info.DetailedRow,2);string sheet=null;
  if(cell!=null&&cell.Formula!=null){var m=System.Text.RegularExpressions.Regex.Match(cell.Formula,@"^'?(.*?)'?!");if(m.Success)sheet=m.Groups[1].Value.Replace("''","'");}
  var item=allDupas.FirstOrDefault(d=>d.Sheet==sheet);if(item==null)return;dupaSearch.Text="";ShowStep(3);var page=groups.TabPages.Cast<TabPage>().FirstOrDefault(p=>"DUPA".Equals(p.Tag));if(page!=null)groups.SelectedTab=page;dupaList.SelectedItem=item;}
 /* step 4: the Excel-like sheets, DUPA and printing (unchanged) */
 void BuildReviewStep(Panel step){var tools=new FlowLayoutPanel{Dock=DockStyle.Top,Height=44,WrapContents=false,BackColor=StudioBack,Padding=new Padding(0,6,0,6)};
  var print=new ContextMenuStrip();print.Items.Add("Print this sheet…",null,(s,e)=>PrintSheets(false));print.Items.Add("Print preview",null,(s,e)=>PrintSheets(true));print.Items.Add(new ToolStripSeparator());print.Items.Add("Print DUPA of items with quantity…",null,(s,e)=>PrintDupas());
  var pb=new Button{Text="Print  ▾",AutoSize=true,Height=30};pb.Click+=(s,e)=>print.Show(pb,new Point(0,pb.Height));tools.Controls.Add(pb);var pv=new Button{Text="Print preview",AutoSize=true,Height=30,Margin=new Padding(8,0,0,0)};pv.Click+=(s,e)=>PrintSheets(true);tools.Controls.Add(pv);
  tools.Controls.Add(new Label{Text="Zoom",AutoSize=true,Margin=new Padding(20,7,6,0),ForeColor=StudioMuted});zoom.Margin=new Padding(0,3,12,0);tools.Controls.Add(zoom);hideX.Margin=new Padding(0,6,0,0);tools.Controls.Add(hideX);
  step.Controls.Add(groups);step.Controls.Add(tools);}
}
sealed class StudioField{public string At,Kind;public Control Input;public bool Loading;}
/* sidebar workflow step */
sealed class StudioStep:Control{bool active,hover;public bool Active{get{return active;}set{active=value;Invalidate();}}
 public StudioStep(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);Cursor=Cursors.Hand;Font=new Font("Segoe UI",9.25f);BackColor=PowWorkbookForm.StudioDark;}
 protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(PowWorkbookForm.StudioDark);var r=new RectangleF(0,2,Width-1,Height-5);
  if(active||hover)using(var path=ModernUi.Rounded(r,7))using(var b=new SolidBrush(active?PowWorkbookForm.StudioActive:Color.FromArgb(28,70,55)))g.FillPath(b,path);
  if(active)using(var b=new SolidBrush(Color.FromArgb(160,214,130)))g.FillRectangle(b,0,Height/2-9,3,18);
  using(var f=new Font(Font,active?FontStyle.Bold:FontStyle.Regular))TextRenderer.DrawText(g,Text,f,new Rectangle(14,0,Width-20,Height),active?Color.White:Color.FromArgb(200,222,210),TextFormatFlags.VerticalCenter|TextFormatFlags.Left|TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix);}}
/* step progress line: "STEP 1 OF 4 ━━━━━─── Project details → Hauling & prices → …" */
sealed class StudioProgress:Control{public int Step;public string Caption="";public StudioProgress(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);Font=new Font("Segoe UI",8.5f);}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(PowWorkbookForm.StudioBack);using(var bold=new Font(Font,FontStyle.Bold))TextRenderer.DrawText(g,Caption,bold,new Point(0,8),PowWorkbookForm.StudioInk,TextFormatFlags.NoPrefix);
  float x=96,y=Height/2f-2,w=130;using(var b=new SolidBrush(PowWorkbookForm.StudioLine))g.FillRectangle(b,x,y,w,4);using(var b=new SolidBrush(PowWorkbookForm.StudioGreen))g.FillRectangle(b,x,y,w*(Step+1)/4f,4);
  TextRenderer.DrawText(g,"Project details  →  Hauling & prices  →  Pay items & quantities  →  Review & print",Font,new Point((int)(x+w+16),8),PowWorkbookForm.StudioMuted,TextFormatFlags.NoPrefix);}}
/* white card with a numbered title */
sealed class StudioCard:Panel{public string Number="",Title="",Subtitle="";public bool Compact;public readonly Panel Body=new Panel{Dock=DockStyle.Fill,BackColor=Color.White};
 public StudioCard(){BackColor=Color.White;DoubleBuffered=true;Controls.Add(Body);HandleCreated+=(s,e)=>Layout0();}
 void Layout0(){Padding=Compact?new Padding(18,30,18,10):new Padding(22,Subtitle==""?48:70,22,18);}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);Layout0();var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;using(var p=new Pen(PowWorkbookForm.StudioLine))g.DrawRectangle(p,0,0,Width-1,Height-1);int x=22;
  if(Compact){TextRenderer.DrawText(g,Title,new Font("Segoe UI",7.5f,FontStyle.Bold),new Point(18,12),PowWorkbookForm.StudioMuted);return;}
  if(Number!=""){using(var b=new SolidBrush(PowWorkbookForm.StudioSoft))g.FillRectangle(b,22,20,26,22);TextRenderer.DrawText(g,Number,new Font("Segoe UI",8f),new Rectangle(22,20,26,22),PowWorkbookForm.StudioGreen,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);x=58;}
  using(var f=new Font("Segoe UI",11f,FontStyle.Bold))TextRenderer.DrawText(g,Title,f,new Point(x,20),PowWorkbookForm.StudioInk,TextFormatFlags.NoPrefix);if(Subtitle!="")TextRenderer.DrawText(g,Subtitle,new Font("Segoe UI",9f),new Point(22,48),PowWorkbookForm.StudioMuted);}}
}
