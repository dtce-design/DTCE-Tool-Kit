using System;using System.Drawing;using System.Linq;using System.Windows.Forms;using System.Collections.Generic;
namespace BeamScheduleFormatter {
class LauncherForm:Form {
 Form formatter,slabFormatter,lengths,project,works;
 #if ADMIN_APP
 Form generator;
 #endif
 readonly Panel home=new Panel{Dock=DockStyle.Fill},groupHost=new Panel{Dock=DockStyle.Fill,Visible=false},groupContent=new Panel{Dock=DockStyle.Fill};
 readonly Label groupTitle=new Label{AutoSize=true,Padding=new Padding(18,8,0,0),Font=new Font("Segoe UI",13,FontStyle.Bold)};
 readonly TabControl formatterTabs=new TabControl{Dock=DockStyle.Fill},designTabs=new TabControl{Dock=DockStyle.Fill};readonly Dictionary<string,Form> designModules=new Dictionary<string,Form>();
 public LauncherForm(){AppBrand.Apply(this);Text="DTCE Tool Kit v"+Engine.Version;ClientSize=new Size(1100,800);MinimumSize=new Size(900,650);Font=new Font("Segoe UI",10);StartPosition=FormStartPosition.CenterScreen;Controls.Add(home);Controls.Add(groupHost);
  home.Controls.Add(new PictureBox{Image=AppBrand.Logo,SizeMode=PictureBoxSizeMode.Zoom,Left=28,Top=18,Width=76,Height=76});home.Controls.Add(new Label{Text="DTCE TOOL KIT",Left=120,Top=28,Width=600,Height=44,Font=new Font("Segoe UI",23,FontStyle.Bold),ForeColor=PowTheme.Ink});home.Controls.Add(new Label{Text="Choose a workspace. Settings, estimates and design sheets stay inside their group.",Left=30,Top=106,Width=1000,Height=28,ForeColor=PowTheme.Muted});
  var edition=new Label{Text=
   #if ADMIN_APP
   "ADMIN EDITION"
   #else
   "USER EDITION"
   #endif
   ,ForeColor=PowTheme.Teal,Left=ClientSize.Width-190,Top=45,Width=160,Height=25,Anchor=AnchorStyles.Top|AnchorStyles.Right};home.Controls.Add(edition);
  var cards=new FlowLayoutPanel{Left=24,Top=150,Width=ClientSize.Width-48,Height=ClientSize.Height-206,AutoScroll=true,Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right,Padding=new Padding(2)};home.Controls.Add(cards);
  AddCard(cards,"Formatters","Beam and slab schedule formatters, grouped in one workspace.",()=>ShowGroup(formatterTabs,"Formatters"));AddCard(cards,"Program of Works","Concrete, masonry, roofing, structural steel and complete quantity backups.",()=>UseTool(ref works,()=>new ProgramOfWorksForm(),"Program of Works",null,2));AddCard(cards,"Design Sheets","Splice, Development & Hooks; Purlin Design; Truss Connections.",ShowDesignSheets);AddCard(cards,"Project Information","Project details shared by calculations, schedules and reports.",()=>UseTool(ref project,()=>new ProjectInformationForm(),"Project Information",null,8));
  #if ADMIN_APP
  AddCard(cards,"Access Code Generator","Generate and manage approved access to the tool kit.",()=>UseTool(ref generator,()=>new CodeGeneratorForm(),"Access Code Generator",null,8));
  #endif
  cards.Resize+=(s,e)=>{int width=Math.Max(280,(cards.ClientSize.Width-36)/2);foreach(Control item in cards.Controls)item.Width=width;};foreach(Control item in cards.Controls)item.Width=Math.Max(280,(cards.ClientSize.Width-36)/2);
  home.Controls.Add(new Label{Text="v"+Engine.Version+" | Developed by Engr. Daryl O. Tomaneng",Dock=DockStyle.Bottom,Height=38,Padding=new Padding(26,9,0,0),ForeColor=PowTheme.Muted,BackColor=Color.White});
  var header=new FlowLayoutPanel{Dock=DockStyle.Top,Height=58,Padding=new Padding(10),BackColor=Color.White};var back=new Button{Text="Back to Start",Width=130,Height=34};back.Click+=(s,e)=>ShowStart();header.Controls.Add(back);header.Controls.Add(groupTitle);groupHost.Controls.Add(groupContent);groupHost.Controls.Add(header);
  AddEntry(formatterTabs,"Beam Formatter","Open Beam Formatter",()=>UseTool(ref formatter,CreateBeam,"Beam Formatter",formatterTabs,1));AddEntry(formatterTabs,"Slab Formatter","Open Slab Formatter",()=>UseTool(ref slabFormatter,()=>new SlabFormatterForm(),"Slab Formatter",formatterTabs,1));AddEntry(designTabs,"Splice, Development & Hooks","Open combined design sheet",()=>UseTool(ref lengths,()=>new RebarLengthForm(),"Splice, Development & Hooks",designTabs,4));AddEntry(designTabs,"Purlin Design","Open Purlin Design",()=>OpenDesign("Purlin Design",()=>new PurlinDesignForm()));AddEntry(designTabs,"Truss Connections","Open Truss Connections",()=>OpenDesign("Truss Connections",()=>new TrussConnectionForm()));
  FormClosing+=(s,e)=>{var children=new[]{formatter,slabFormatter,lengths,project,works}.Concat(designModules.Values).Where(f=>f!=null&&!f.IsDisposed).Distinct().ToList();
   #if ADMIN_APP
   if(generator!=null&&!generator.IsDisposed)children.Add(generator);
   #endif
   foreach(var child in children){child.Close();if(!child.IsDisposed){e.Cancel=true;return;}}
  };ShowStart();
 }
 Form CreateBeam(){var form=new MainForm();
  #if !ADMIN_APP
  var renewal=new AccessRenewal(form);form.FormClosed+=(s,e)=>renewal.Dispose();
  #endif
  return form;
 }
 bool Authorize(int app){if(!AccessCodes.CanUseFormatter){using(var login=new LoginForm())if(login.ShowDialog(this)!=DialogResult.OK)return false;}if(!AccessCodes.CanUseApp(app)){MessageBox.Show(this,"This workspace is not included in your approved access.","App access");return false;}return true;}
 void UseTool(ref Form form,Func<Form> create,string title,TabControl tabs,int app){try{if(!Authorize(app))return;if(form==null||form.IsDisposed)form=create();ShowTool(form,title,tabs);}catch(Exception ex){MessageBox.Show(this,ex.Message,"DTCE Tool Kit");}}
 void OpenDesign(string title,Func<Form> create){try{if(!Authorize(4))return;Form form;if(!designModules.TryGetValue(title,out form)||form.IsDisposed){form=create();designModules[title]=form;}ShowTool(form,title,designTabs);}catch(Exception ex){MessageBox.Show(this,ex.Message,"Design Sheet");}}
 void ShowTool(Form form,string title,TabControl tabs){if(form.TopLevel){form.TopLevel=false;form.FormBorderStyle=FormBorderStyle.None;}form.MinimumSize=Size.Empty;form.MaximumSize=Size.Empty;form.Dock=DockStyle.Fill;if(tabs==null){ShowGroup(form,title);form.Show();return;}var page=tabs.TabPages.Cast<TabPage>().FirstOrDefault(p=>p.Text==title);int position=page==null?tabs.TabPages.Count:tabs.TabPages.IndexOf(page);if(page!=null&&page.Tag!=form){tabs.TabPages.Remove(page);page.Dispose();page=null;}if(page==null){page=new TabPage(title){Tag=form};page.Controls.Add(form);tabs.TabPages.Insert(Math.Min(position,tabs.TabPages.Count),page);}tabs.SelectedTab=page;ShowGroup(tabs,tabs==formatterTabs?"Formatters":"Design Sheets");form.Show();form.BringToFront();}
 void ShowGroup(Control content,string title){groupContent.Controls.Clear();content.Dock=DockStyle.Fill;groupContent.Controls.Add(content);groupTitle.Text=title;home.Visible=false;groupHost.Visible=true;groupHost.BringToFront();content.Visible=true;PowTheme.Apply(groupHost);}
 internal void ShowStart(){groupHost.Visible=false;home.Visible=true;home.BringToFront();}
 internal void ShowDesignSheets(){ShowGroup(designTabs,"Design Sheets");}
 void AddCard(FlowLayoutPanel panel,string title,string description,Action action){var card=new ToolModuleButton{Text=title,Description=description,Height=138,Width=470,Margin=new Padding(0,0,14,14)};card.Click+=(s,e)=>action();panel.Controls.Add(card);}
 void AddEntry(TabControl tabs,string title,string buttonText,Action action){var page=new TabPage(title);var open=new Button{Text=buttonText,Left=24,Top=28,Width=300,Height=42};open.Click+=(s,e)=>action();page.Controls.Add(open);tabs.TabPages.Add(page);}
}
class ToolModuleButton:Button {
 public int ModuleIndex;public string Description;bool hover;
 static readonly string[] descriptions={"Format and review beam schedules from DXF files.","Beam, slab and footing takeoffs, earthworks and project reports.","Development, lap splice and standard hook calculations.","Set the project name and location for your reports.","Generate access codes and manage approved tools.","Group identical slab schedules and export the final DXF layout."};
 public ToolModuleButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);Cursor=Cursors.Hand;FlatStyle=FlatStyle.Flat;TabStop=true;MouseEnter+=(s,e)=>{hover=true;Invalidate();};MouseLeave+=(s,e)=>{hover=false;Invalidate();};GotFocus+=(s,e)=>Invalidate();LostFocus+=(s,e)=>Invalidate();}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(hover?Color.FromArgb(238,249,249):Color.White);using(var pen=new Pen(hover||Focused?PowTheme.Teal:Color.FromArgb(207,219,230),Focused?2:1))e.Graphics.DrawRectangle(pen,1,1,Width-3,Height-3);using(var accent=new SolidBrush(PowTheme.Teal))e.Graphics.FillRectangle(accent,1,1,4,Height-2);using(var title=new Font("Segoe UI",11,FontStyle.Bold))TextRenderer.DrawText(e.Graphics,Text,title,new Rectangle(20,20,Width-40,29),PowTheme.Ink,TextFormatFlags.Left|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);using(var body=new Font("Segoe UI",9.5f))TextRenderer.DrawText(e.Graphics,Description??descriptions[Math.Min(5,ModuleIndex)],body,new Rectangle(20,57,Width-42,54),Color.FromArgb(92,111,128),TextFormatFlags.Left|TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix);}
}

}

