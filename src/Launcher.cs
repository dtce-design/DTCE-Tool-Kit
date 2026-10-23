using System;using System.Drawing;using System.Windows.Forms;
namespace BeamScheduleFormatter {
class LauncherForm:Form {
 Form formatter,estimator,lengths,project;
 #if ADMIN_APP
 Form generator;
 #endif
 public LauncherForm(){AppBrand.Apply(this);Text="DTCE Tool Kit v"+Engine.Version;ClientSize=new Size(600,520);MinimumSize=new Size(616,559);StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(243,246,250);
  Controls.Add(new PictureBox{Image=AppBrand.Logo,SizeMode=PictureBoxSizeMode.Zoom,Location=new Point(30,12),Size=new Size(72,72)});
  var title=new Label{Text="DTCE TOOL KIT",Font=new Font("Segoe UI",18,FontStyle.Bold),ForeColor=Color.FromArgb(20,33,51),Location=new Point(110,30),Size=new Size(460,40)};Controls.Add(title);Controls.Add(new Label{Text="Choose a program. Each opens in a separate window.",Location=new Point(32,90),Size=new Size(530,30)});
  Add("Beam Schedule Formatter",120,()=>{if(formatter==null||formatter.IsDisposed){formatter=new MainForm();
   #if !ADMIN_APP
   var renewal=new AccessRenewal(formatter);formatter.FormClosed+=(o,e)=>renewal.Dispose();
   #endif
   formatter.Show();}else{formatter.Show();formatter.WindowState=FormWindowState.Normal;formatter.Activate();}});
  Add("Beam Estimator",185,()=>{if(estimator==null||estimator.IsDisposed){estimator=new EstimatorForm(null);estimator.Show();}else{estimator.Show();estimator.WindowState=FormWindowState.Normal;estimator.Activate();}});
  Add("Project Information",315,()=>{if(project==null||project.IsDisposed){project=new ProjectInformationForm();project.Show();}else{project.Show();project.WindowState=FormWindowState.Normal;project.Activate();}});
  Add("Splice, Development & Hooks",250,()=>{if(lengths==null||lengths.IsDisposed){lengths=new RebarLengthForm();lengths.Show();}else{lengths.Show();lengths.WindowState=FormWindowState.Normal;lengths.Activate();}});
  #if ADMIN_APP
  Add("Access Code Generator",380,()=>{if(generator==null||generator.IsDisposed){generator=new CodeGeneratorForm();generator.Show();}else{generator.Show();generator.Activate();}});
  #endif
  Controls.Add(new Label{Text="v"+Engine.Version+" | Developed by Engr. Daryl O. Tomaneng",Location=new Point(32,470),Size=new Size(540,25),ForeColor=Color.FromArgb(91,107,125)});
  FormClosing+=(o,e)=>{foreach(var child in new[]{formatter,estimator,lengths,project})if(child!=null&&!child.IsDisposed){child.Close();if(!child.IsDisposed){e.Cancel=true;return;}}
   #if ADMIN_APP
   if(generator!=null&&!generator.IsDisposed)generator.Close();
   #endif
  };
 }
 void Add(string text,int y,Action action){var button=new Button{Text=text,Location=new Point(32,y),Size=new Size(535,50),FlatStyle=FlatStyle.Flat,BackColor=Color.White,ForeColor=Color.FromArgb(20,33,51),Font=new Font("Segoe UI",12,FontStyle.Bold)};button.Click+=(o,e)=>{if(!AccessCodes.CanUseFormatter){using(var login=new LoginForm())if(login.ShowDialog(this)!=DialogResult.OK)return;}int app=text=="Beam Schedule Formatter"?1:text=="Beam Estimator"?2:text=="Splice, Development & Hooks"?4:8;if(!AccessCodes.CanUseApp(app)){MessageBox.Show(this,"This app is not included in your approved access. Ask the admin for a new code with this app selected.","App access");return;}try{action();}catch(Exception ex){MessageBox.Show(ex.Message,"Beam Tools");}};Controls.Add(button);}
}
}
