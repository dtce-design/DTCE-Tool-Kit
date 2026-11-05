using System;using System.Drawing;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
class LauncherForm:Form {
 Form formatter,slabFormatter,lengths,project,works;readonly TabControl workspaceTabs=new TabControl{Dock=DockStyle.Fill};readonly TabControl designTabs=new TabControl{Dock=DockStyle.Fill};TabPage designGroup;
 #if ADMIN_APP
 Form generator;
 #endif
 public LauncherForm(){AppBrand.Apply(this);Text="DTCE Tool Kit v"+Engine.Version;ClientSize=new Size(820,650);MinimumSize=new Size(700,600);StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(243,246,250);
  Controls.Add(new PictureBox{Image=AppBrand.Logo,SizeMode=PictureBoxSizeMode.Zoom,Location=new Point(30,12),Size=new Size(72,72)});
  var title=new Label{Text="DTCE TOOL KIT",Font=new Font("Segoe UI",18,FontStyle.Bold),ForeColor=Color.FromArgb(20,33,51),Location=new Point(110,30),Size=new Size(460,40)};Controls.Add(title);Controls.Add(new Label{Text="Engineering estimates, schedules and calculation tools.",Location=new Point(32,90),Size=new Size(530,30)});
  Add("Beam Schedule Formatter",120,()=>{if(formatter==null||formatter.IsDisposed){formatter=new MainForm();
   #if !ADMIN_APP
   var renewal=new AccessRenewal(formatter);formatter.FormClosed+=(o,e)=>renewal.Dispose();
   #endif
   ShowTool(formatter,"Beam Formatter");}else{ShowTool(formatter,"Beam Formatter");formatter.WindowState=FormWindowState.Normal;formatter.Activate();}});
  Add("Slab Schedule Formatter",445,()=>{if(slabFormatter==null||slabFormatter.IsDisposed){slabFormatter=new SlabFormatterForm();ShowTool(slabFormatter,"Slab Formatter");}else{ShowTool(slabFormatter,"Slab Formatter");slabFormatter.WindowState=FormWindowState.Normal;slabFormatter.Activate();}});
  Add("Program of Works",185,()=>{if(works==null||works.IsDisposed){works=new ProgramOfWorksForm();ShowTool(works,"Program of Works");}else{ShowTool(works,"Program of Works");works.WindowState=FormWindowState.Normal;works.Activate();}});
  Add("Project Information",315,()=>{if(project==null||project.IsDisposed){project=new ProjectInformationForm();ShowTool(project,"Project Information");}else{ShowTool(project,"Project Information");project.WindowState=FormWindowState.Normal;project.Activate();}});
  Add("Splice, Development & Hooks",250,()=>{if(lengths==null||lengths.IsDisposed){lengths=new RebarLengthForm();ShowTool(lengths,"Splice, Development & Hooks");}else{ShowTool(lengths,"Splice, Development & Hooks");lengths.WindowState=FormWindowState.Normal;lengths.Activate();}});
  #if ADMIN_APP
  Add("Access Code Generator",380,()=>{if(generator==null||generator.IsDisposed){generator=new CodeGeneratorForm();ShowTool(generator,"Access Code Generator");}else{ShowTool(generator,"Access Code Generator");generator.Activate();}});
  #endif
  Controls.Add(new Label{Text="v"+Engine.Version+" | Developed by Engr. Daryl O. Tomaneng",Location=new Point(32,610),Size=new Size(750,25),ForeColor=Color.FromArgb(91,107,125)});
  var edition=new Label{Text=
   #if ADMIN_APP
   "ADMIN EDITION"
   #else
   "USER EDITION"
   #endif
   ,ForeColor=PowTheme.Teal,Font=new Font("Segoe UI",9,FontStyle.Bold),Left=600,Top=40,Width=180,Height=25,Anchor=AnchorStyles.Top|AnchorStyles.Right};Controls.Add(edition);
  var footer=Controls.OfType<Label>().First(l=>l.Text.StartsWith("v"+Engine.Version));footer.Dock=DockStyle.Bottom;footer.Height=36;footer.Padding=new Padding(24,8,0,0);footer.BackColor=Color.White;
  var cards=Controls.OfType<ToolModuleButton>().OrderBy(b=>b.ModuleIndex).ToArray();
  var workspace=new FlowLayoutPanel{Left=24,Top=135,Width=ClientSize.Width-48,Height=ClientSize.Height-180,AutoScroll=true,Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right,Padding=new Padding(2)};
  Controls.Add(workspace);foreach(var card in cards){workspace.Controls.Add(card);card.Margin=new Padding(0,0,16,16);card.Height=132;}
  Action layout=()=>{int width=Math.Max(280,(workspace.ClientSize.Width-44)/2);foreach(var card in cards)card.Width=width;};workspace.Resize+=(s,e)=>layout();layout();
  var startPage=new TabPage("Start");foreach(Control item in Controls.Cast<Control>().ToArray())startPage.Controls.Add(item);workspaceTabs.TabPages.Add(startPage);Controls.Add(workspaceTabs);InitializeDesignSheets(cards.First(c=>c.Text=="Splice, Development & Hooks"));
  FormClosing+=(o,e)=>{foreach(var child in new[]{formatter,slabFormatter,lengths,project,works})if(child!=null&&!child.IsDisposed){child.Close();if(!child.IsDisposed){e.Cancel=true;return;}}
   #if ADMIN_APP
   if(generator!=null&&!generator.IsDisposed)generator.Close();
   #endif
  };
 }
 void InitializeDesignSheets(Button openSheet){designGroup=new TabPage("Design Sheets");designGroup.Controls.Add(designTabs);var first=new TabPage("Splice, Development & Hooks");var open=new Button{Text="Open design sheet",Width=200,Height=40,Left=24,Top=30};open.Click+=(s,e)=>{workspaceTabs.SelectedIndex=0;openSheet.PerformClick();};first.Controls.Add(open);first.Controls.Add(new Label{Text="Development, lap splice and hook calculations. Additional design sheets can be added to this group.",Left=24,Top=90,Width=700,Height=60});designTabs.TabPages.Add(first);workspaceTabs.TabPages.Add(designGroup);}
 void ShowTool(Form form,string title){var target=title=="Splice, Development & Hooks"?designTabs:workspaceTabs;var page=target.TabPages.Cast<TabPage>().FirstOrDefault(p=>p.Text==title);if(page!=null&&page.Tag!=form){target.TabPages.Remove(page);page.Dispose();page=null;}if(page==null){page=new TabPage(title){Tag=form};form.TopLevel=false;form.FormBorderStyle=FormBorderStyle.None;form.Dock=DockStyle.Fill;page.Controls.Add(form);target.TabPages.Add(page);}target.SelectedTab=page;if(target==designTabs)workspaceTabs.SelectedTab=designGroup;form.Show();form.BringToFront();}
 void Add(string text,int y,Action action){int index=y==120?0:y==185?1:y==250?2:y==315?3:y==445?5:4;var button=new ToolModuleButton{Text=text,Location=new Point(32+(index%2)*390,140+(index/2)*150),Size=new Size(365,128),Anchor=AnchorStyles.Top|AnchorStyles.Left,ModuleIndex=index};button.Click+=(o,e)=>{if(!AccessCodes.CanUseFormatter){using(var login=new LoginForm())if(login.ShowDialog(this)!=DialogResult.OK)return;}int app=(text=="Beam Schedule Formatter"||text=="Slab Schedule Formatter")?1:(text=="Beam Estimator"||text=="Slab Estimator"||text=="Program of Works")?2:text=="Splice, Development & Hooks"?4:8;if(!AccessCodes.CanUseApp(app)){MessageBox.Show(this,"This app is not included in your approved access. Ask the admin for a new code with this app selected.","App access");return;}try{action();}catch(Exception ex){MessageBox.Show(ex.Message,"DTCE Tool Kit");}};Controls.Add(button);}
}
class ToolModuleButton:Button {
 public int ModuleIndex;bool hover;
 static readonly string[] descriptions={"Format and review beam schedules from DXF files.","Beam, slab and footing takeoffs, earthworks and project reports.","Development, lap splice and standard hook calculations.","Set the project name and location for your reports.","Generate access codes and manage approved tools.","Group identical slab schedules and export the final DXF layout."};
 public ToolModuleButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);Cursor=Cursors.Hand;FlatStyle=FlatStyle.Flat;TabStop=true;MouseEnter+=(s,e)=>{hover=true;Invalidate();};MouseLeave+=(s,e)=>{hover=false;Invalidate();};GotFocus+=(s,e)=>Invalidate();LostFocus+=(s,e)=>Invalidate();}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(hover?Color.FromArgb(238,249,249):Color.White);using(var pen=new Pen(hover||Focused?PowTheme.Teal:Color.FromArgb(207,219,230),Focused?2:1))e.Graphics.DrawRectangle(pen,1,1,Width-3,Height-3);using(var accent=new SolidBrush(PowTheme.Teal))e.Graphics.FillRectangle(accent,1,1,4,Height-2);using(var title=new Font("Segoe UI",11,FontStyle.Bold))TextRenderer.DrawText(e.Graphics,Text,title,new Rectangle(20,20,Width-40,29),PowTheme.Ink,TextFormatFlags.Left|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);using(var body=new Font("Segoe UI",9.5f))TextRenderer.DrawText(e.Graphics,descriptions[Math.Min(5,ModuleIndex)],body,new Rectangle(20,57,Width-42,54),Color.FromArgb(92,111,128),TextFormatFlags.Left|TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix);}
}

}

