using System;using System.Drawing;using System.Windows.Forms;
namespace BeamScheduleFormatter {
class LauncherForm:Form {
 Form formatter,lengths,project,works;
 #if ADMIN_APP
 Form generator;
 #endif
 public LauncherForm(){AppBrand.Apply(this);Text="DTCE Tool Kit v"+Engine.Version;ClientSize=new Size(820,650);MinimumSize=new Size(836,689);StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(243,246,250);
  Controls.Add(new PictureBox{Image=AppBrand.Logo,SizeMode=PictureBoxSizeMode.Zoom,Location=new Point(30,12),Size=new Size(72,72)});
  var title=new Label{Text="DTCE TOOL KIT",Font=new Font("Segoe UI",18,FontStyle.Bold),ForeColor=Color.FromArgb(20,33,51),Location=new Point(110,30),Size=new Size(460,40)};Controls.Add(title);Controls.Add(new Label{Text="Engineering estimates, schedules and calculation tools.",Location=new Point(32,90),Size=new Size(530,30)});
  Add("Beam Schedule Formatter",120,()=>{if(formatter==null||formatter.IsDisposed){formatter=new MainForm();
   #if !ADMIN_APP
   var renewal=new AccessRenewal(formatter);formatter.FormClosed+=(o,e)=>renewal.Dispose();
   #endif
   formatter.Show();}else{formatter.Show();formatter.WindowState=FormWindowState.Normal;formatter.Activate();}});
  Add("Program of Works",185,()=>{if(works==null||works.IsDisposed){works=new ProgramOfWorksForm();works.Show();}else{works.Show();works.WindowState=FormWindowState.Normal;works.Activate();}});
  Add("Project Information",315,()=>{if(project==null||project.IsDisposed){project=new ProjectInformationForm();project.Show();}else{project.Show();project.WindowState=FormWindowState.Normal;project.Activate();}});
  Add("Splice, Development & Hooks",250,()=>{if(lengths==null||lengths.IsDisposed){lengths=new RebarLengthForm();lengths.Show();}else{lengths.Show();lengths.WindowState=FormWindowState.Normal;lengths.Activate();}});
  #if ADMIN_APP
  Add("Access Code Generator",380,()=>{if(generator==null||generator.IsDisposed){generator=new CodeGeneratorForm();generator.Show();}else{generator.Show();generator.Activate();}});
  #endif
  Controls.Add(new Label{Text="v"+Engine.Version+" | Developed by Engr. Daryl O. Tomaneng",Location=new Point(32,610),Size=new Size(750,25),ForeColor=Color.FromArgb(91,107,125)});
  FormClosing+=(o,e)=>{foreach(var child in new[]{formatter,lengths,project,works})if(child!=null&&!child.IsDisposed){child.Close();if(!child.IsDisposed){e.Cancel=true;return;}}
   #if ADMIN_APP
   if(generator!=null&&!generator.IsDisposed)generator.Close();
   #endif
  };
 }
 void Add(string text,int y,Action action){int index=y==120?0:y==185?1:y==250?2:y==315?3:4;var button=new ToolModuleButton{Text=text,Location=new Point(32+(index%2)*390,140+(index/2)*150),Size=new Size(365,128),Anchor=AnchorStyles.Top|AnchorStyles.Left,ModuleIndex=index};button.Click+=(o,e)=>{if(!AccessCodes.CanUseFormatter){using(var login=new LoginForm())if(login.ShowDialog(this)!=DialogResult.OK)return;}int app=text=="Beam Schedule Formatter"?1:(text=="Beam Estimator"||text=="Slab Estimator"||text=="Program of Works")?2:text=="Splice, Development & Hooks"?4:8;if(!AccessCodes.CanUseApp(app)){MessageBox.Show(this,"This app is not included in your approved access. Ask the admin for a new code with this app selected.","App access");return;}try{action();}catch(Exception ex){MessageBox.Show(ex.Message,"DTCE Tool Kit");}};Controls.Add(button);}
}
class ToolModuleButton:Button {
 public int ModuleIndex;bool hover;
 static readonly string[] descriptions={"Format and review beam schedules from DXF files.","Concrete, reinforcement, masonry and finishing quantities.","Development, lap splice and standard hook calculations.","Set the project name and location for your reports.","Generate access codes and manage approved tools."};
 public ToolModuleButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);Cursor=Cursors.Hand;FlatStyle=FlatStyle.Flat;TabStop=true;MouseEnter+=(s,e)=>{hover=true;Invalidate();};MouseLeave+=(s,e)=>{hover=false;Invalidate();};GotFocus+=(s,e)=>Invalidate();LostFocus+=(s,e)=>Invalidate();}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(hover?Color.FromArgb(238,249,249):Color.White);using(var pen=new Pen(hover||Focused?PowTheme.Teal:Color.FromArgb(207,219,230),Focused?2:1))e.Graphics.DrawRectangle(pen,1,1,Width-3,Height-3);using(var accent=new SolidBrush(PowTheme.Teal))e.Graphics.FillRectangle(accent,1,1,4,Height-2);using(var title=new Font("Segoe UI",11,FontStyle.Bold))TextRenderer.DrawText(e.Graphics,Text,title,new Rectangle(20,20,Width-40,29),PowTheme.Ink,TextFormatFlags.Left|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);using(var body=new Font("Segoe UI",9.5f))TextRenderer.DrawText(e.Graphics,descriptions[Math.Min(4,ModuleIndex)],body,new Rectangle(20,57,Width-42,54),Color.FromArgb(92,111,128),TextFormatFlags.Left|TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix);}
}

}
