using System;using System.Drawing;using System.Linq;using System.Windows.Forms;using System.Collections.Generic;
namespace BeamScheduleFormatter {
class LauncherForm:Form {
 Form formatter,slabFormatter,lengths,project,works;
 #if ADMIN_APP
 Form generator;
 #endif
 readonly Panel home=new Panel{Dock=DockStyle.Fill},groupHost=new Panel{Dock=DockStyle.Fill,Visible=false},groupContent=new Panel{Dock=DockStyle.Fill};
 readonly Label groupTitle=new Label{AutoSize=true,Padding=new Padding(18,8,0,0),Font=new Font("Segoe UI",13,FontStyle.Bold)};
 readonly TabControl formatterTabs=new NavigationTabs{Dock=DockStyle.Fill},designTabs=new NavigationTabs{Dock=DockStyle.Fill};readonly Dictionary<string,Form> designModules=new Dictionary<string,Form>();readonly Dictionary<string,Action> designLoaders=new Dictionary<string,Action>();bool loadingDesign,loadingFormatter;readonly Dictionary<string,Action> formatterLoaders=new Dictionary<string,Action>();
 public LauncherForm(){AppBrand.Apply(this);Text="DTCE Tool Kit v"+Engine.Version;ClientSize=new Size(1100,800);MinimumSize=new Size(900,650);Font=new Font("Segoe UI",10);StartPosition=FormStartPosition.CenterScreen;Controls.Add(home);Controls.Add(groupHost);
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
  var cards=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,Margin=Padding.Empty,Padding=new Padding(0,0,0,12)};dashboard.Controls.Add(cards,0,2);
  AddCard(cards,"Formatters","Prepare beam and slab schedules from your DXF drawings.",ShowFormatters);
  AddCard(cards,"Program of Works","Estimate quantities and keep your Bill of Quantities connected.",()=>UseTool(ref works,()=>new ProgramOfWorksForm(),"Program of Works",null,2));
  AddCard(cards,"Design Sheets","Reinforcement, purlins, truss connections and wind load calculations.",ShowDesignSheets);
  AddCard(cards,"Project Information","Set the project details used in your calculations and reports.",()=>UseTool(ref project,()=>new ProjectInformationForm(),"Project Information",null,8));
  #if ADMIN_APP
  AddCard(cards,"Access Code Generator","Manage program access and generate activation codes.",()=>UseTool(ref generator,()=>new CodeGeneratorForm(),"Access Code Generator",null,8));
  #endif
  Action sizeCards=()=>{int width=Math.Max(270,(cards.ClientSize.Width-32)/2);foreach(Control item in cards.Controls)item.Width=width;};
  cards.Resize+=(sender,args)=>sizeCards();sizeCards();
  dashboard.Controls.Add(new Label{Text="v"+Engine.Version+"  /  Developed by Engr. Daryl O. Tomaneng",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,ForeColor=PowTheme.Muted,Font=new Font("Segoe UI",8.5f),Margin=Padding.Empty},0,3);
  var header=PremiumControls.WorkspaceHeader(groupTitle,ShowStart);groupHost.Controls.Add(groupContent);groupHost.Controls.Add(header);
  AddFormatterEntry("Beam Formatter",()=>UseTool(ref formatter,CreateBeam,"Beam Formatter",formatterTabs,1));AddFormatterEntry("Slab Formatter",()=>UseTool(ref slabFormatter,()=>new SlabFormatterForm(),"Slab Formatter",formatterTabs,ProgramAccess.SlabFormatter));AddDesignEntry("Splice, Development & Hooks",()=>UseTool(ref lengths,()=>new RebarLengthForm(),"Splice, Development & Hooks",designTabs,4));AddDesignEntry("Purlin Design",()=>OpenDesign("Purlin Design",()=>new PurlinDesignForm()));AddDesignEntry("Truss Connections",()=>OpenDesign("Truss Connections",()=>new TrussConnectionForm()));AddDesignEntry("Wind Load (NSCP 2015)",()=>OpenDesign("Wind Load (NSCP 2015)",()=>new WindDesignForm()));AddDesignEntry("Beam Deflection Check",()=>OpenDesign("Beam Deflection Check",()=>new BeamDeflectionForm()));
  formatterTabs.SelectedIndexChanged+=(s,e)=>{if(groupHost.Visible&&!loadingFormatter&&groupContent.Controls.Contains(formatterTabs))LoadSelectedFormatter();};
  designTabs.SelectedIndexChanged+=(s,e)=>{if(groupHost.Visible&&!loadingDesign)LoadSelectedDesign();};
  FormClosing+=(s,e)=>{var children=new[]{formatter,slabFormatter,lengths,project,works}.Concat(designModules.Values).Where(f=>f!=null&&!f.IsDisposed).Distinct().ToList();
   #if ADMIN_APP
   if(generator!=null&&!generator.IsDisposed)children.Add(generator);
   #endif
   foreach(var child in children){child.Close();if(!child.IsDisposed){e.Cancel=true;return;}}
  };DesktopAppearance.Attach(this);new StoreUpdateMonitor(this);ShowStart();
 }
 Form CreateBeam(){var form=new MainForm();
  #if !ADMIN_APP
  var renewal=new AccessRenewal(form);form.FormClosed+=(s,e)=>renewal.Dispose();
  #endif
  return form;
 }
 bool Authorize(int app){if(!AccessCodes.CanUseFormatter){using(var login=new LoginForm())if(login.ShowDialog(this)!=DialogResult.OK)return false;}if(!AccessCodes.CanUseApp(app)){MessageBox.Show(this,"This workspace is not included in your approved access.","App access");return false;}return true;}
 void UseTool(ref Form form,Func<Form> create,string title,TabControl tabs,int app){try{if(!Authorize(app))return;if(form==null||form.IsDisposed)form=create();ShowTool(form,title,tabs);}catch(Exception ex){MessageBox.Show(this,ex.Message,"DTCE Tool Kit");}}
 void OpenDesign(string title,Func<Form> create){try{if(!Authorize(title=="Beam Deflection Check"?ProgramAccess.BeamDeflection:title=="Wind Load (NSCP 2015)"?ProgramAccess.WindDesign:title=="Purlin Design"?ProgramAccess.PurlinDesign:ProgramAccess.TrussConnections))return;Form form;if(!designModules.TryGetValue(title,out form)||form.IsDisposed){form=create();designModules[title]=form;}ShowTool(form,title,designTabs);}catch(Exception ex){MessageBox.Show(this,ex.Message,"Design Sheet");}}
 void ShowTool(Form form,string title,TabControl tabs){if(form.TopLevel){form.TopLevel=false;form.FormBorderStyle=FormBorderStyle.None;}form.MinimumSize=Size.Empty;form.MaximumSize=Size.Empty;form.Dock=DockStyle.Fill;if(tabs==null){ShowGroup(form,title);form.Show();return;}var page=tabs.TabPages.Cast<TabPage>().FirstOrDefault(p=>p.Text==title);if(page==null){page=new TabPage(title);tabs.TabPages.Add(page);}if(page.Tag!=form){page.Controls.Clear();page.Tag=form;page.Controls.Add(form);}tabs.SelectedTab=page;ShowGroup(tabs,tabs==formatterTabs?"Formatters":"Design Sheets");form.Show();form.BringToFront();}
 void ShowGroup(Control content,string title){groupContent.Controls.Clear();content.Dock=DockStyle.Fill;groupContent.Controls.Add(content);groupTitle.Text=title;home.Visible=false;groupHost.Visible=true;groupHost.BringToFront();content.Visible=true;PowTheme.Apply(groupHost);}
 internal void ShowStart(){groupHost.Visible=false;home.Visible=true;home.BringToFront();}
 internal void ShowDesignSheets(){if(!AccessCodes.CanUseFormatter){using(var login=new LoginForm())if(login.ShowDialog(this)!=DialogResult.OK)return;}var allowed=designTabs.TabPages.Cast<TabPage>().Where(p=>AccessCodes.CanUseApp(DesignProgram(p.Text))).ToList();if(allowed.Count==0){MessageBox.Show(this,"No Design Sheet is included in your approved access.","App access");return;}loadingDesign=true;try{if(!allowed.Contains(designTabs.SelectedTab))designTabs.SelectedTab=allowed[0];ShowGroup(designTabs,"Design Sheets");}finally{loadingDesign=false;}LoadSelectedDesign();}
 static int DesignProgram(string title){return title=="Beam Deflection Check"?ProgramAccess.BeamDeflection:title=="Wind Load (NSCP 2015)"?ProgramAccess.WindDesign:title=="Purlin Design"?ProgramAccess.PurlinDesign:title=="Truss Connections"?ProgramAccess.TrussConnections:ProgramAccess.SpliceDevelopmentHooks;}
 void LoadSelectedDesign(){if(loadingDesign||designTabs.SelectedTab==null)return;foreach(var child in designTabs.SelectedTab.Controls.OfType<Form>())child.Hide();Action open;if(!designLoaders.TryGetValue(designTabs.SelectedTab.Text,out open))return;loadingDesign=true;try{open();}finally{loadingDesign=false;}}
 internal void ShowFormatters(){loadingFormatter=true;try{ShowGroup(formatterTabs,"Formatters");}finally{loadingFormatter=false;}LoadSelectedFormatter();}
 void LoadSelectedFormatter(){if(loadingFormatter||formatterTabs.SelectedTab==null)return;Action open;if(!formatterLoaders.TryGetValue(formatterTabs.SelectedTab.Text,out open))return;loadingFormatter=true;try{open();}finally{loadingFormatter=false;}}
 void AddFormatterEntry(string title,Action open){formatterLoaders.Add(title,open);formatterTabs.TabPages.Add(new TabPage(title));}
 void AddDesignEntry(string title,Action open){designLoaders.Add(title,open);var page=new TabPage(title);page.Controls.Add(new Label{Text="This design sheet requires its own program access.",Dock=DockStyle.Top,Height=60,Padding=new Padding(20)});designTabs.TabPages.Add(page);}
 void AddCard(FlowLayoutPanel panel,string title,string description,Action action){var card=new ToolModuleButton{ModuleIndex=panel.Controls.Count,Text=title,Description=description,Height=160,Width=470,Margin=new Padding(0,0,14,14)};card.Click+=(s,e)=>action();panel.Controls.Add(card);}
 void AddEntry(TabControl tabs,string title,string buttonText,Action action){var page=new TabPage(title);var open=new Button{Text=buttonText,Left=24,Top=28,Width=300,Height=42};open.Click+=(s,e)=>action();page.Controls.Add(open);tabs.TabPages.Add(page);}
}
class ToolModuleButton:Button {
 public int ModuleIndex;public string Description;bool hover;
 static readonly string[] descriptions={"Format and review beam schedules from DXF files.","Beam, slab and footing takeoffs, earthworks and project reports.","Development, lap splice and standard hook calculations.","Set the project name and location for your reports.","Generate access codes and manage approved tools.","Group identical slab schedules and export the final DXF layout."};
 public ToolModuleButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);Cursor=Cursors.Hand;FlatStyle=FlatStyle.Flat;TabStop=true;MouseEnter+=(s,e)=>{hover=true;Invalidate();};MouseLeave+=(s,e)=>{hover=false;Invalidate();};GotFocus+=(s,e)=>Invalidate();LostFocus+=(s,e)=>Invalidate();}
 protected override void OnPaint(PaintEventArgs e){
  e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
  e.Graphics.Clear(Parent==null?PowTheme.Background:Parent.BackColor);
  var bounds=new RectangleF(1,1,Width-3,Height-3);
  using(var path=ModernUi.Rounded(bounds,12))using(var fill=new SolidBrush(hover?ModernUi.Soft:ModernUi.Surface))using(var border=new Pen(hover||Focused?PowTheme.Teal:PowTheme.Line,Focused?2:1)){
   e.Graphics.FillPath(fill,path);e.Graphics.DrawPath(border,path);
  }
  using(var path=ModernUi.Rounded(new RectangleF(20,19,38,38),9))using(var fill=new SolidBrush(ModernUi.Selection))e.Graphics.FillPath(fill,path);
  DrawGlyph(e.Graphics,new Rectangle(28,27,22,22));
  using(var title=new Font("Segoe UI",12,FontStyle.Bold))TextRenderer.DrawText(e.Graphics,Text,title,new Rectangle(72,20,Width-113,35),PowTheme.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
  TextRenderer.DrawText(e.Graphics,"›",ModernUi.Heading,new Rectangle(Width-40,20,25,35),PowTheme.Teal,TextFormatFlags.VerticalCenter);
  TextRenderer.DrawText(e.Graphics,Description??descriptions[Math.Min(5,ModuleIndex)],ModernUi.Body,new Rectangle(22,76,Width-50,65),PowTheme.Muted,TextFormatFlags.Left|TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix);
  if(Focused)FocusHighlight.Draw(e.Graphics,new Rectangle(7,7,Width-14,Height-14),WorksheetColors.Focus);
 }
 void DrawGlyph(Graphics g,Rectangle r){
  using(var pen=new Pen(PowTheme.Teal,1.7f)){
   if(ModuleIndex==0){g.DrawRectangle(pen,r.X,r.Y,r.Width,r.Height);g.DrawLine(pen,r.X,r.Y+7,r.Right,r.Y+7);g.DrawLine(pen,r.X+8,r.Y,r.X+8,r.Bottom);g.DrawLine(pen,r.X,r.Y+14,r.Right,r.Y+14);}
   else if(ModuleIndex==1){g.DrawRectangle(pen,r.X+2,r.Y,r.Width-4,r.Height);for(int i=0;i<3;i++)g.DrawLine(pen,r.X+7,r.Y+6+i*5,r.Right-4,r.Y+6+i*5);}
   else if(ModuleIndex==2){g.DrawPolygon(pen,new[]{new Point(r.X,r.Bottom),new Point(r.X+11,r.Y),new Point(r.Right,r.Bottom)});g.DrawLine(pen,r.X+5,r.Y+12,r.Right-5,r.Y+12);}
   else if(ModuleIndex==3){g.DrawRectangle(pen,r.X,r.Y+4,r.Width,r.Height-4);g.DrawLine(pen,r.X+7,r.Y+4,r.X+7,r.Y);g.DrawLine(pen,r.Right-7,r.Y+4,r.Right-7,r.Y);g.DrawLine(pen,r.X+5,r.Y+11,r.Right-5,r.Y+11);}
   else {g.DrawEllipse(pen,r.X,r.Y,11,11);g.DrawLine(pen,r.X+10,r.Y+10,r.Right,r.Bottom);g.DrawLine(pen,r.Right-5,r.Bottom-5,r.Right,r.Bottom-10);}
  }
 }

}

}


