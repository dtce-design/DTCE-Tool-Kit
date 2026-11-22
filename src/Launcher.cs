using System;using System.Drawing;using System.Linq;using System.Windows.Forms;using System.Collections.Generic;
namespace BeamScheduleFormatter {
class LauncherForm:Form {
 Form formatter,slabFormatter,columnFormatter,lengths,project,works,cutting,powWorkbook;
 #if ADMIN_APP
 Form generator;
 #endif
 readonly Panel home=new Panel{Dock=DockStyle.Fill},groupHost=new Panel{Dock=DockStyle.Fill,Visible=false},groupContent=new Panel{Dock=DockStyle.Fill};
 readonly Label groupTitle=new Label{AutoSize=true,Padding=new Padding(18,8,0,0),Font=new Font("Segoe UI",13,FontStyle.Bold)};
 readonly TabControl formatterTabs=new NavigationTabs{Dock=DockStyle.Fill},designTabs=new NavigationTabs{Dock=DockStyle.Fill};readonly Dictionary<string,Form> designModules=new Dictionary<string,Form>();readonly Dictionary<string,Action> designLoaders=new Dictionary<string,Action>();bool loadingDesign,loadingFormatter;readonly Dictionary<string,Action> formatterLoaders=new Dictionary<string,Action>();
 public LauncherForm(){AppBrand.Apply(this);Text="DTCE Tool Kit v"+Engine.Version;/* owner (19 November 2026, "mas compact na window sa startup 2 columns lang") */ClientSize=new Size(780,580);/* owner (20 November 2026, "alisin ang scroll, ifix ang size ng buttons, at window saka iadjust yung window if may additional workspace"): fixed window and card sizes, no maximize; the height follows the number of workspaces */Font=new Font("Segoe UI",9.25f);StartPosition=FormStartPosition.CenterScreen;Controls.Add(home);Controls.Add(groupHost);
  home.Padding=new Padding(24,20,24,0);
  var dashboard=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Margin=Padding.Empty,Padding=Padding.Empty};
  dashboard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
  dashboard.RowStyles.Add(new RowStyle(SizeType.Absolute,124));dashboard.RowStyles.Add(new RowStyle(SizeType.Absolute,58));
  dashboard.RowStyles.Add(new RowStyle(SizeType.Percent,100));dashboard.RowStyles.Add(new RowStyle(SizeType.Absolute,38));home.Controls.Add(dashboard);
  var hero=new Panel{Dock=DockStyle.Fill,BackColor=Color.White,Margin=Padding.Empty};dashboard.Controls.Add(hero,0,0);
  hero.Controls.Add(new PictureBox{Image=AppBrand.Logo,SizeMode=PictureBoxSizeMode.Zoom,Left=22,Top=24,Width=76,Height=76,AccessibleName="DTCE logo"});
  hero.Controls.Add(new Label{Text="DTCE Tool Kit",Left=140,Top=38,Width=570,Height=42,Font=ModernUi.Heading,ForeColor=PowTheme.Ink,Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Right});
  hero.Controls.Add(new Label{Text="Engineering calculations and quantity take-offs",Left=143,Top=84,Width=570,Height=28,ForeColor=PowTheme.Muted,Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Right});
  hero.Controls.Add(new Label{Text=
   #if ADMIN_APP
   "ADMIN EDITION"
   #else
   "USER EDITION"
   #endif
   ,Left=143,Top=16,Width=260,Height=20,Font=new Font("Segoe UI",8.5f,FontStyle.Bold),ForeColor=PowTheme.Teal});
  var intro=new Label{Text="Your workspaces",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Font=new Font("Segoe UI",12,FontStyle.Bold),ForeColor=PowTheme.Ink,Margin=Padding.Empty};var workspaceActions=new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty,Padding=new Padding(0,10,0,10)};var preferences=new Button{Text="Workspace settings",Dock=DockStyle.Right,Width=190,AccessibleName="Workspace appearance settings"};preferences.Click+=(sender,args)=>DesktopAppearance.ShowSettings(this);workspaceActions.Controls.Add(intro);workspaceActions.Controls.Add(preferences);dashboard.Controls.Add(workspaceActions,0,1);
  var cards=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=false,WrapContents=true,Margin=Padding.Empty,Padding=new Padding(0,0,0,12)};dashboard.Controls.Add(cards,0,2);
  /* owner (19 November 2026): Project Information always comes first */
  AddCard(cards,"Project Information","Set the project details used in your calculations and reports.",()=>UseTool(ref project,()=>new ProjectInformationForm(),"Project Information",null,8));
  AddCard(cards,"Formatters","Prepare beam and slab schedules from your DXF drawings.",ShowFormatters);
  AddCard(cards,"Bill of Quantities","Measure quantities and build the Bill of Quantities with its linked workbook.",()=>UseTool(ref works,()=>{var f=new ProgramOfWorksForm();ProgramOfWorksForm.Live=new WeakReference(f);GridFormulas.AttachAll(f);return f;},"Bill of Quantities",null,2));
  /* owner (20 November 2026, "gawin yung POW as separate worksheet walang password or code access"): opens without an access code */
  AddCard(cards,"Program of Works (POW)","DPWH POW workbook: DUPA, Detailed, ABC and BOQ computed and printed in the app.",()=>{try{if(powWorkbook==null||powWorkbook.IsDisposed)powWorkbook=new PowWorkbookForm();ShowTool(powWorkbook,"Program of Works (POW)",null);}catch(Exception ex){MessageBox.Show(this,ex.Message,"Program of Works (POW)");}});
  AddCard(cards,"Bar Bending Schedule","Cutting lists, bar bending and splice details with an interactive model.",()=>UseTool(ref cutting,()=>new CuttingListForm(),"Bar Bending Schedule",null,ProgramAccess.BarBendingSchedule));
  AddCard(cards,"Design Sheets","Rebar, purlins, trusses, strap foundations, wind, earthquake, deflection.",ShowDesignSheets);
  #if ADMIN_APP
  AddCard(cards,"Access Code Generator","Manage program access and generate activation codes.",()=>UseTool(ref generator,()=>new CodeGeneratorForm(),"Access Code Generator",null,8));
  #endif
  /* owner (19 November 2026, "hindi pantay yung edge niya sa right"): the last card of each row ends on the same right edge as the header and Workspace settings *//* fixed cards; columns follow the window width (owner: "pwedeng magdagdag ng column or rows pero hindi pwedeng magkascroll"),
     the window height follows the rows, never a scroll bar */const int cardW=359,cardH=86,gap=14,rowGap=12;bool fitting=false;
  Action layoutCards=()=>{if(fitting)return;fitting=true;try{int avail=Math.Max(cardW,ClientSize.Width-home.Padding.Horizontal);int cols=Math.Max(1,(avail+gap)/(cardW+gap));int n=cards.Controls.Count;cols=Math.Min(cols,Math.Max(1,n));
    for(int i=0;i<n;i++){var item=cards.Controls[i];item.Size=new Size(cardW,cardH);bool last=i%cols==cols-1;item.Margin=new Padding(0,0,last?0:gap,rowGap);cards.SetFlowBreak(item,last);}
    int rows=(n+cols-1)/cols;int needed=home.Padding.Top+124+58+rows*(cardH+rowGap)+cards.Padding.Bottom+38;
    if(WindowState==FormWindowState.Normal&&ClientSize.Height!=needed){var area=Screen.FromControl(this).WorkingArea;int chrome=Height-ClientSize.Height;ClientSize=new Size(ClientSize.Width,Math.Min(needed,area.Height-chrome));}}finally{fitting=false;}};
  ClientSize=new Size(home.Padding.Horizontal+2*cardW+gap,ClientSize.Height);MinimumSize=new Size(Width-ClientSize.Width+home.Padding.Horizontal+2*cardW+gap,0);layoutCards();Resize+=(sender,args)=>layoutCards();
  /* after a resize the width snaps to whole columns, so the cards end on the same right edge as the header */ResizeEnd+=(sender,args)=>{if(WindowState!=FormWindowState.Normal)return;int avail=Math.Max(cardW,ClientSize.Width-home.Padding.Horizontal);int cols=Math.Max(2,Math.Min(Math.Max(2,cards.Controls.Count),(avail+gap)/(cardW+gap)));int w=home.Padding.Horizontal+cols*cardW+(cols-1)*gap;if(ClientSize.Width!=w)ClientSize=new Size(w,ClientSize.Height);layoutCards();};
  dashboard.Controls.Add(new Label{Text="v"+Engine.Version+"  /  Developed by Engr. Daryl O. Tomaneng",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,ForeColor=PowTheme.Muted,Font=new Font("Segoe UI",8.5f),Margin=Padding.Empty},0,3);
  var header=PremiumControls.WorkspaceHeader(groupTitle,ShowStart);groupHost.Controls.Add(groupContent);groupHost.Controls.Add(header);
  AddFormatterEntry("Beam Formatter",()=>UseTool(ref formatter,CreateBeam,"Beam Formatter",formatterTabs,1));AddFormatterEntry("Slab Formatter",()=>UseTool(ref slabFormatter,()=>new SlabFormatterForm(),"Slab Formatter",formatterTabs,ProgramAccess.SlabFormatter));AddFormatterEntry("Column Formatter",()=>UseTool(ref columnFormatter,()=>new ColumnFormatterForm(),"Column Formatter",formatterTabs,ProgramAccess.SlabFormatter));AddDesignEntry("Splice, Development & Hooks",()=>UseTool(ref lengths,()=>new RebarLengthForm(),"Splice, Development & Hooks",designTabs,4));AddDesignEntry("Purlin Design",()=>OpenDesign("Purlin Design",()=>new PurlinDesignForm()));AddDesignEntry("Truss Connections",()=>OpenDesign("Truss Connections",()=>new TrussConnectionForm()));AddDesignEntry("Wind Load (NSCP 2015)",()=>OpenDesign("Wind Load (NSCP 2015)",()=>new WindDesignForm()));AddDesignEntry("Beam Deflection Check",()=>OpenDesign("Beam Deflection Check",()=>new BeamDeflectionForm()));AddDesignEntry("Strap Foundation",()=>OpenDesign("Strap Foundation",()=>new StrapFootingForm()));AddDesignEntry("Truss Support on RC",()=>OpenDesign("Truss Support on RC",()=>new TrussSupportForm()));AddDesignEntry("Earthquake Load & RSA Curve Builder",()=>OpenDesign("Earthquake Load & RSA Curve Builder",()=>new RsaCurveForm()));/* NSCP 2015 response spectrum for STAAD; uses the Wind Load (NSCP 2015) access */
  /* the tab groups live in their own workspace windows (19 November 2026) */formatterTabs.SelectedIndexChanged+=(s,e)=>{if(!loadingFormatter&&formatterTabs.FindForm() is WorkspaceWindow)LoadSelectedFormatter();};
  designTabs.SelectedIndexChanged+=(s,e)=>{if(!loadingDesign&&designTabs.FindForm() is WorkspaceWindow)LoadSelectedDesign();};
  FormClosing+=(s,e)=>{var children=new[]{formatter,slabFormatter,columnFormatter,lengths,project,works}.Concat(designModules.Values).Where(f=>f!=null&&!f.IsDisposed).Distinct().ToList();
   #if ADMIN_APP
   if(generator!=null&&!generator.IsDisposed)children.Add(generator);
   #endif
   foreach(var child in children){child.Close();if(!child.IsDisposed){e.Cancel=true;return;}}
   foreach(var window in windows.Values.Where(w=>!w.IsDisposed).ToList()){window.Release();window.Close();}
  };DesktopAppearance.Attach(this);new StoreUpdateMonitor(this);ShowStart();
 }
 Form CreateBeam(){var form=new MainForm();
  #if !ADMIN_APP
  var renewal=new AccessRenewal(form);form.FormClosed+=(s,e)=>renewal.Dispose();
  #endif
  return form;
 }
 /* the other workspaces open only after the project information is complete (owner, 19 November 2026: "di pwede mag proceed if
    di nafill up") */
 bool ProjectReady(){ProjectInformation info;try{info=ProjectInfoStore.Read();}catch(Exception){info=new ProjectInformation();}if(info.Complete)return true;
  MessageBox.Show(this,"Fill in the Project Information first (project title, location, implementing office, office address and date prepared), then save it.","Project Information",MessageBoxButtons.OK,MessageBoxIcon.Information);UseTool(ref project,()=>new ProjectInformationForm(),"Project Information",null,8);return false;}
 bool Authorize(int app){if(!AccessCodes.CanUseFormatter){using(var login=new LoginForm())if(login.ShowDialog(this)!=DialogResult.OK)return false;}if(!AccessCodes.CanUseApp(app)){MessageBox.Show(this,"This workspace is not included in your approved access.","App access");return false;}return true;}
 void UseTool(ref Form form,Func<Form> create,string title,TabControl tabs,int app){try{if(!Authorize(app))return;if(title!="Project Information"&&title!="Access Code Generator"&&!ProjectReady())return;if(form==null||form.IsDisposed)form=create();ShowTool(form,title,tabs);}catch(Exception ex){MessageBox.Show(this,ex.Message,"DTCE Tool Kit");}}
 void OpenDesign(string title,Func<Form> create){try{if(!Authorize(DesignProgram(title)))return;if(!ProjectReady())return;Form form;if(!designModules.TryGetValue(title,out form)||form.IsDisposed){form=create();designModules[title]=form;}ShowTool(form,title,designTabs);}catch(Exception ex){MessageBox.Show(this,ex.Message,"Design Sheet");}}
 void ShowTool(Form form,string title,TabControl tabs){if(form.TopLevel){form.TopLevel=false;form.FormBorderStyle=FormBorderStyle.None;}form.MinimumSize=Size.Empty;form.MaximumSize=Size.Empty;form.Dock=DockStyle.Fill;if(tabs==null){ShowGroup(form,title);form.Show();return;}var page=tabs.TabPages.Cast<TabPage>().FirstOrDefault(p=>p.Text==title);if(page==null){page=new TabPage(title);tabs.TabPages.Add(page);}if(page.Tag!=form){page.Controls.Clear();page.Tag=form;page.Controls.Add(form);}tabs.SelectedTab=page;ShowGroup(tabs,tabs==formatterTabs?"Formatters":"Design Sheets");form.Show();form.BringToFront();}
 /* Owner (19 November 2026, "another window na lang yung magbubukas sa mga individual workspaces"): each workspace opens in its
    own window; the Workspaces window stays as it is, and a workspace already open is brought to the front. */
 readonly Dictionary<string,WorkspaceWindow> windows=new Dictionary<string,WorkspaceWindow>();
 void ShowGroup(Control content,string title){WorkspaceWindow window;if(!windows.TryGetValue(title,out window)||window.IsDisposed){window=new WorkspaceWindow(this,title);windows[title]=window;}
  window.Host(content);content.Visible=true;if(!window.Visible)window.Show();if(window.WindowState==FormWindowState.Minimized)window.WindowState=FormWindowState.Normal;window.Activate();}
 internal IEnumerable<WorkspaceWindow> OpenWindows{get{return windows.Values.Where(w=>!w.IsDisposed&&w.Visible);}}
 internal void ShowStart(){groupHost.Visible=false;home.Visible=true;home.BringToFront();if(Visible){if(WindowState==FormWindowState.Minimized)WindowState=FormWindowState.Normal;Activate();}}
 internal void ShowDesignSheets(){if(!ProjectReady())return;if(!AccessCodes.CanUseFormatter){using(var login=new LoginForm())if(login.ShowDialog(this)!=DialogResult.OK)return;}var allowed=designTabs.TabPages.Cast<TabPage>().Where(p=>AccessCodes.CanUseApp(DesignProgram(p.Text))).ToList();if(allowed.Count==0){MessageBox.Show(this,"No Design Sheet is included in your approved access.","App access");return;}loadingDesign=true;try{if(!allowed.Contains(designTabs.SelectedTab))designTabs.SelectedTab=allowed[0];ShowGroup(designTabs,"Design Sheets");}finally{loadingDesign=false;}LoadSelectedDesign();}
 static int DesignProgram(string title){return title=="Wind Load (NSCP 2015)"?ProgramAccess.WindDesign:title=="Earthquake Load & RSA Curve Builder"?ProgramAccess.EarthquakeRsa:title=="Purlin Design"?ProgramAccess.PurlinDesign:title=="Truss Connections"?ProgramAccess.TrussConnections:title=="Truss Support on RC"?ProgramAccess.TrussSupport:title=="Strap Foundation"?ProgramAccess.StrapFoundation:title=="Beam Deflection Check"?ProgramAccess.BeamDeflection:ProgramAccess.SpliceDevelopmentHooks;}
 void LoadSelectedDesign(){if(loadingDesign||designTabs.SelectedTab==null)return;foreach(var child in designTabs.SelectedTab.Controls.OfType<Form>())child.Hide();Action open;if(!designLoaders.TryGetValue(designTabs.SelectedTab.Text,out open))return;loadingDesign=true;try{open();}finally{loadingDesign=false;}}
 internal void ShowFormatters(){if(!ProjectReady())return;loadingFormatter=true;try{ShowGroup(formatterTabs,"Formatters");}finally{loadingFormatter=false;}LoadSelectedFormatter();}
 void LoadSelectedFormatter(){if(loadingFormatter||formatterTabs.SelectedTab==null)return;Action open;if(!formatterLoaders.TryGetValue(formatterTabs.SelectedTab.Text,out open))return;loadingFormatter=true;try{open();}finally{loadingFormatter=false;}}
 void AddFormatterEntry(string title,Action open){formatterLoaders.Add(title,open);formatterTabs.TabPages.Add(new TabPage(title));}
 void AddDesignEntry(string title,Action open){designLoaders.Add(title,open);var page=new TabPage(title);page.Controls.Add(new Label{Text="This design sheet requires its own program access.",Dock=DockStyle.Top,Height=60,Padding=new Padding(20)});designTabs.TabPages.Add(page);}
 void AddCard(FlowLayoutPanel panel,string title,string description,Action action){var card=new ToolModuleButton{ModuleIndex=panel.Controls.Count,Text=title,Description=description,Height=86,Width=330,Margin=new Padding(0,0,14,12)};card.Click+=(s,e)=>action();panel.Controls.Add(card);}
 void AddEntry(TabControl tabs,string title,string buttonText,Action action){var page=new TabPage(title);var open=new Button{Text=buttonText,Left=24,Top=28,Width=300,Height=42};open.Click+=(s,e)=>action();page.Controls.Add(open);tabs.TabPages.Add(page);}
}
/* A workspace in its own window: the same header as before ("Workspaces" brings the start window to the front). Closing the
   window closes its tools with their usual save prompts; the tab groups are kept for the next time they are opened. */
class WorkspaceWindow:Form {
 readonly LauncherForm launcher;readonly Panel content=new Panel{Dock=DockStyle.Fill};readonly Label title=new Label();Panel header;
 /* the Program of Works command bar sits in the header beside the title (owner, 19 November 2026) and goes back on close */
 FlowLayoutPanel bar;Control barHome;int barIndex;
 public WorkspaceWindow(LauncherForm owner,string caption){launcher=owner;AppBrand.Apply(this);Text=caption+" - DTCE Tool Kit v"+Engine.Version;Font=owner.Font;StartPosition=FormStartPosition.CenterScreen;ClientSize=new Size(1280,820);MinimumSize=new Size(900,600);
  var area=Screen.FromControl(owner).WorkingArea;if(area.Width<1320||area.Height<880)WindowState=FormWindowState.Maximized;
  title.Text=caption;Controls.Add(content);header=PremiumControls.WorkspaceHeader(title,owner.ShowStart);Controls.Add(header);
  FormClosing+=(s,e)=>{foreach(var child in Children().ToList()){child.Close();if(!child.IsDisposed){e.Cancel=true;return;}}if(!e.Cancel)Release();};}
 /* the tools hosted here (embedded forms, also inside the Formatter / Design Sheet tabs) */
 IEnumerable<Form> Children(){var stack=new Stack<Control>(content.Controls.Cast<Control>());while(stack.Count>0){var c=stack.Pop();var f=c as Form;if(f!=null){yield return f;continue;}foreach(Control x in c.Controls)stack.Push(x);}}
 internal void Host(Control control){if(content.Controls.Count==1&&content.Controls[0]==control)return;content.Controls.Clear();control.Dock=DockStyle.Fill;content.Controls.Add(control);DockCommandBar();PowTheme.Apply(this);if(bar!=null&&bar.Parent==header)bar.BackColor=header.BackColor;}
 void DockCommandBar(){
  if(bar!=null&&!bar.IsDisposed&&bar.Parent==header)return;
  var found=Find(content);if(found==null)return;
  bar=found;barHome=found.Parent;barIndex=barHome.Controls.GetChildIndex(found);
  title.Dock=DockStyle.Left;title.Width=TextRenderer.MeasureText(title.Text,title.Font).Width+28;
  bar.AutoSize=false;bar.WrapContents=false;bar.Padding=new Padding(0);bar.Dock=DockStyle.Fill;
  header.Controls.Add(bar);header.Controls.SetChildIndex(bar,0);
 }
 static FlowLayoutPanel Find(Control root){foreach(Control c in root.Controls){if(c.Name=="PowCommandBar"&&c is FlowLayoutPanel)return (FlowLayoutPanel)c;var f=Find(c);if(f!=null)return f;}return null;}
 /* keeps the shared tab groups alive when the window closes */
 internal void Release(){if(bar!=null&&barHome!=null&&!barHome.IsDisposed&&bar.Parent==header){header.Controls.Remove(bar);bar.Dock=DockStyle.Top;bar.AutoSize=true;bar.WrapContents=true;bar.Padding=new Padding(8,5,8,3);barHome.Controls.Add(bar);barHome.Controls.SetChildIndex(bar,barIndex);title.Dock=DockStyle.Fill;}bar=null;foreach(var tabs in content.Controls.OfType<TabControl>().ToList())content.Controls.Remove(tabs);}
}
class ToolModuleButton:Button {
 public int ModuleIndex;public string Description;bool hover;static readonly Font SmallBody=new Font("Segoe UI",9f);
 static readonly string[] descriptions={"Format and review beam schedules from DXF files.","Beam, slab and footing takeoffs, earthworks and project reports.","Development, lap splice and standard hook calculations.","Set the project name and location for your reports.","Generate access codes and manage approved tools.","Group identical slab schedules and export the final DXF layout."};
 public ToolModuleButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);Cursor=Cursors.Hand;FlatStyle=FlatStyle.Flat;TabStop=true;MouseEnter+=(s,e)=>{hover=true;Invalidate();};MouseLeave+=(s,e)=>{hover=false;Invalidate();};GotFocus+=(s,e)=>Invalidate();LostFocus+=(s,e)=>Invalidate();}
 protected override void OnPaint(PaintEventArgs e){
  e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
  e.Graphics.Clear(Parent==null?PowTheme.Background:Parent.BackColor);
  var bounds=new RectangleF(1,1,Width-3,Height-3);
  using(var path=ModernUi.Rounded(bounds,12))using(var fill=new SolidBrush(hover?ModernUi.Soft:ModernUi.Surface))using(var border=new Pen(hover||Focused?PowTheme.Teal:PowTheme.Line,Focused?2:1)){
   e.Graphics.FillPath(fill,path);e.Graphics.DrawPath(border,path);
  }
  using(var path=ModernUi.Rounded(new RectangleF(14,14,30,30),8))using(var fill=new SolidBrush(ModernUi.Selection))e.Graphics.FillPath(fill,path);
  DrawGlyph(e.Graphics,new Rectangle(19,19,20,20));
  using(var title=new Font("Segoe UI",10.5f,FontStyle.Bold))TextRenderer.DrawText(e.Graphics,Text,title,new Rectangle(54,12,Width-90,24),PowTheme.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
  TextRenderer.DrawText(e.Graphics,"›",ModernUi.Heading,new Rectangle(Width-32,12,22,28),PowTheme.Teal,TextFormatFlags.VerticalCenter);
  TextRenderer.DrawText(e.Graphics,Description??descriptions[Math.Min(5,ModuleIndex)],SmallBody,new Rectangle(54,38,Width-92,42),PowTheme.Muted,TextFormatFlags.Left|TextFormatFlags.WordBreak|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
  if(Focused)FocusHighlight.Draw(e.Graphics,new Rectangle(5,5,Width-10,Height-10),WorksheetColors.Focus);
 }
 /* one glyph per workspace, chosen by the card title (owner, 20 November 2026: "bakit same ang logo ng design sheet at access
    generator") */
 void DrawGlyph(Graphics g,Rectangle r){
  using(var pen=new Pen(PowTheme.Teal,1.7f){LineJoin=System.Drawing.Drawing2D.LineJoin.Round,StartCap=System.Drawing.Drawing2D.LineCap.Round,EndCap=System.Drawing.Drawing2D.LineCap.Round})using(var dot=new SolidBrush(PowTheme.Teal)){string t=Text??"";
   if(t.StartsWith("Project")){/* information: circle with i */g.DrawEllipse(pen,r.X,r.Y,r.Width,r.Height);g.FillEllipse(dot,r.X+r.Width/2-1.6f,r.Y+4,3.2f,3.2f);g.DrawLine(pen,r.X+r.Width/2f,r.Y+9,r.X+r.Width/2f,r.Bottom-4);}
   else if(t.StartsWith("Formatter")){/* drawing sheet with lines */g.DrawRectangle(pen,r.X+2,r.Y,r.Width-4,r.Height);for(int i=0;i<3;i++)g.DrawLine(pen,r.X+7,r.Y+6+i*5,r.Right-6,r.Y+6+i*5);}
   else if(t.StartsWith("Program")){/* bill of quantities grid */g.DrawRectangle(pen,r.X,r.Y,r.Width,r.Height);g.DrawLine(pen,r.X,r.Y+7,r.Right,r.Y+7);g.DrawLine(pen,r.X+8,r.Y,r.X+8,r.Bottom);g.DrawLine(pen,r.X,r.Y+14,r.Right,r.Y+14);}
   else if(t.StartsWith("Bar Bending")){/* bent bar with a hook */g.DrawLines(pen,new[]{new Point(r.X+2,r.Y+3),new Point(r.X+2,r.Bottom-3),new Point(r.Right-3,r.Bottom-3),new Point(r.Right-3,r.Y+8)});g.DrawLine(pen,r.Right-3,r.Y+8,r.Right-8,r.Y+3);}
   else if(t.StartsWith("Design")){/* set square and pencil */g.DrawPolygon(pen,new[]{new Point(r.X,r.Bottom),new Point(r.X,r.Y+2),new Point(r.Right-6,r.Bottom)});g.DrawLine(pen,r.X+4,r.Bottom-4,r.X+4,r.Y+10);g.DrawLine(pen,r.Right-1,r.Y,r.X+11,r.Y+12);}
   else if(t.StartsWith("Access")){/* key */g.DrawEllipse(pen,r.X,r.Y,11,11);g.DrawLine(pen,r.X+10,r.Y+10,r.Right,r.Bottom);g.DrawLine(pen,r.Right-5,r.Bottom-5,r.Right,r.Bottom-10);}
   else {g.DrawRectangle(pen,r.X+3,r.Y+3,r.Width-6,r.Height-6);}
  }
 }


}

}


