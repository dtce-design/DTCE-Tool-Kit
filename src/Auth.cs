using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Collections.Generic;

namespace BeamScheduleFormatter {
class LoginForm:Form {
 readonly string request=AccessCodes.GetRequest();TextBox response;Label message;int failures;Timer delay;
 public LoginForm(){AppBrand.Apply(this);
  Text="DTCE Tool Kit - Access code";ClientSize=new Size(620,440);StartPosition=FormStartPosition.CenterScreen;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(245,248,251);
  Controls.Add(new Label{Text="DTCE TOOL KIT",Location=new Point(25,22),Size=new Size(570,35),Font=new Font("Segoe UI",14,FontStyle.Bold)});
  Controls.Add(new Label{Text="Send this request to the owner for a new access code.",Location=new Point(25,75),Size=new Size(565,25)});
  var req=new TextBox{Text=request,ReadOnly=true,Location=new Point(25,105),Size=new Size(445,58),Multiline=true};Controls.Add(req);
  var copy=new Button{Text="Copy request",Location=new Point(480,105),Size=new Size(115,58)};copy.Click+=(o,e)=>Copy(request);Controls.Add(copy);
  Controls.Add(new Label{Text="Paste your access code or Store certification code:",Location=new Point(25,177),Size=new Size(565,25)});
  response=new TextBox{Location=new Point(25,210),Size=new Size(570,78),Multiline=true,MaxLength=2048,ScrollBars=ScrollBars.Vertical};Controls.Add(response);
  var unlock=new Button{Text="Unlock DTCE Tool Kit",Location=new Point(25,300),Size=new Size(570,40),BackColor=Color.FromArgb(0,139,139),ForeColor=Color.White};Controls.Add(unlock);AcceptButton=unlock;
  message=new Label{Text="Normal: until app closes or 8 hours expire, whichever comes first.\nCertification: 30 days, exact app version only.\nVersion: "+Engine.Version+" | Key ID: "+AccessCodes.KeyId(AccessCodes.PublicKey()),Location=new Point(25,355),Size=new Size(570,65)};Controls.Add(message);
  delay=new Timer{Interval=1500};delay.Tick+=(o,e)=>{delay.Stop();unlock.Enabled=true;};FormClosed+=(o,e)=>delay.Dispose();
  unlock.Click+=(o,e)=>{try{
   if(AccessCodes.Accept(request,response.Text)){response.Clear();DialogResult=DialogResult.OK;Close();return;}
   failures++;message.Text="Invalid or expired code. Normal codes must match this request.\nCertification codes must match User version "+Engine.Version+" and key ID.";response.SelectAll();response.Focus();if(failures>=3){unlock.Enabled=false;delay.Start();}
  }catch(Exception ex){message.Text="Cannot save approval: "+ex.Message;}};
 }
 internal static void Copy(string text){try{Clipboard.SetText(text);}catch(Exception){MessageBox.Show("Clipboard is busy. Select and copy the text manually.");}}
}
#if !ADMIN_APP
class AccessRenewal:IDisposable {
 readonly Form form;readonly Timer timer;readonly Dictionary<Control,bool> states=new Dictionary<Control,bool>();
 Panel locked;bool renewing,originalDrop;
 public AccessRenewal(Form owner){form=owner;timer=new Timer{Interval=1000};timer.Tick+=(o,e)=>Check();form.Activated+=Activated;timer.Start();}
 void Activated(object sender,EventArgs args){Check();}
 public void Check(){
  if(form.IsDisposed||locked!=null||renewing||StoreUpdateState.Blocked)return;
  if(AccessCodes.CanUseApp(AccessCodes.AppFor(form))){try{AccessCodes.Checkpoint();}catch(IOException){}catch(UnauthorizedAccessException){}return;}
  timer.Stop();AccessCodes.Granted=false;originalDrop=form.AllowDrop;form.AllowDrop=false;
  states.Clear();foreach(Control c in form.Controls){states[c]=c.Enabled;c.Enabled=false;}
  locked=new Panel{Bounds=form.ClientRectangle,Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right,BackColor=Color.FromArgb(245,248,251)};
  var title=new Label{Text="Access has expired",Location=new Point(40,50),Size=new Size(650,45),Font=new Font("Segoe UI",20,FontStyle.Bold)};
  var note=new Label{Text="Your loaded drawing and settings are preserved.\nGet a new access code from the admin to continue.",Location=new Point(42,110),Size=new Size(650,70),Font=new Font("Segoe UI",11)};
  var button=new Button{Text="Renew access",Location=new Point(42,200),Size=new Size(250,45)};button.Click+=(o,e)=>Renew();
  locked.Controls.Add(title);locked.Controls.Add(note);locked.Controls.Add(button);form.Controls.Add(locked);locked.BringToFront();
  form.BeginInvoke((Action)(()=>Renew()));
 }
 void Renew(){
  if(renewing||form.IsDisposed||locked==null)return;renewing=true;
  try{using(var login=new LoginForm()){login.Text="Access expired - new approval required";if(login.ShowDialog(form)!=DialogResult.OK)return;}
   foreach(var pair in states)if(!pair.Key.IsDisposed)pair.Key.Enabled=pair.Value;
   form.Controls.Remove(locked);locked.Dispose();locked=null;states.Clear();form.AllowDrop=originalDrop;timer.Start();
  }finally{renewing=false;}
 }
 public void Dispose(){timer.Stop();timer.Dispose();form.Activated-=Activated;}
}
#endif
#if ADMIN_APP
class AdminForm:MainForm {
 CodeGeneratorForm generator;
 public AdminForm(){
  Text="Beam Schedule Formatter - ADMIN v"+Engine.Version;
  ActionPanel.Height=218;
  var codes=new Button{Text="Access code\ngenerator",AccessibleName="Access code generator",Width=194,Height=52,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(20,33,51),ForeColor=Color.White,Font=new Font("Segoe UI Semibold",10),Cursor=Cursors.Hand,Margin=new Padding(0,7,0,4)};
  codes.Click+=(o,e)=>OpenGenerator();ActionPanel.Controls.Add(codes);
 }
 public void OpenGenerator(){if(generator==null||generator.IsDisposed){generator=new CodeGeneratorForm();generator.Show(this);}else{generator.Show();generator.BringToFront();}}
}
partial class CodeGeneratorForm:Form {
 internal static int SelectedPrograms(CheckedListBox apps){int selected=0;for(int n=0;n<ProgramAccess.Ids.Length;n++)if(apps.GetItemChecked(n))selected|=ProgramAccess.Ids[n];return selected;}
 TextBox request,output;Label message;string privateKey;
 public CodeGeneratorForm(){AppBrand.Apply(this);
  Text="DTCE Tool Kit Admin - Code Generator";ClientSize=new Size(940,600);/* one screen, no scrolling (owner, 20 November 2026: "gawin 1 screen kasya sa lahat") */StartPosition=FormStartPosition.CenterScreen;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(245,248,251);
  Controls.Add(new Label{Text="DTCE TOOL KIT - ACCESS ADMIN",Location=new Point(25,12),Size=new Size(890,34),Font=new Font("Segoe UI",16,FontStyle.Bold)});
  Controls.Add(new Label{Text="Paste the request from the user's currently open app.",Location=new Point(345,58),Size=new Size(565,24)});
  request=new TextBox{Location=new Point(345,84),Size=new Size(565,60),Multiline=true,MaxLength=200};Controls.Add(request);
  Controls.Add(new Label{Text="APPS ALLOWED",Location=new Point(25,58),Size=new Size(300,24),Font=new Font("Segoe UI",11,FontStyle.Bold)});Controls.Add(new Label{Text="GENERATED ACCESS CODE",Location=new Point(345,154),Size=new Size(565,24),Font=new Font("Segoe UI",11,FontStyle.Bold)});
  var apps=new CheckedListBox{Location=new Point(25,84),Size=new Size(300,272),CheckOnClick=true,BorderStyle=BorderStyle.FixedSingle,BackColor=Color.White,Font=new Font("Segoe UI",11),IntegralHeight=false,HorizontalScrollbar=true};apps.Items.AddRange(ProgramAccess.Names);Controls.Add(apps);Controls.Add(new Label{Text="Project Information is included automatically with valid access.",Location=new Point(25,360),Size=new Size(300,36)});apps.ItemCheck+=(o,e)=>output.Clear();
  var generate=new Button{Text="Generate selected app access",Location=new Point(25,400),Size=new Size(300,40)};Controls.Add(generate);
  output=new TextBox{Location=new Point(345,180),Size=new Size(565,150),Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Font=new Font("Consolas",10),BackColor=Color.White};Controls.Add(output);
  var copy=new Button{Text="Copy access code",Location=new Point(345,338),Size=new Size(277,36)};copy.Click+=(o,e)=>{if(output.Text!="")LoginForm.Copy(output.Text);};Controls.Add(copy);
  var clear=new Button{Text="Clear",Location=new Point(633,338),Size=new Size(277,36)};clear.Click+=(o,e)=>{request.Clear();output.Clear();};Controls.Add(clear);
  message=new Label{Location=new Point(25,420),Size=new Size(280,145)};Controls.Add(message);
  request.TextChanged+=(o,e)=>output.Clear();
  var import=new Button{Text="Import existing key",Location=new Point(25,446),Size=new Size(146,34)};Controls.Add(import);
  message.Location=new Point(25,488);message.Size=new Size(300,100);
  Action loadKey=()=>{try{privateKey=AdminKeyStore.Load();generate.Enabled=true;message.Text="Key ID: "+AccessCodes.KeyId(AccessCodes.PublicKey())+"\nOwner only. Keep a backup of your existing key.";}catch(Exception){privateKey=null;generate.Enabled=false;message.Text="Import your existing AdminPrivateKey.dat. It must match this app and be used under its original Windows account.";}};
  import.Click+=(o,e)=>{using(var dialog=new OpenFileDialog{Title="Import existing Admin private key",Filter="Admin private key|AdminPrivateKey.dat",CheckFileExists=true}){if(dialog.ShowDialog(this)!=DialogResult.OK)return;try{AdminKeyStore.Import(dialog.FileName);output.Clear();loadKey();}catch(Exception){MessageBox.Show(this,"Cannot import this key. Use the matching original key under its original Windows account.","Key import failed");}}};
  var certification=new CheckBox{Text="Store certification (30 days)",Location=new Point(345,386),Size=new Size(360,28)};Controls.Add(certification);
  Controls.Add(new Label{Text="Exact User version",Location=new Point(345,422),Size=new Size(160,25)});
  var certVersion=new TextBox{Text=Engine.Version,Location=new Point(515,419),Size=new Size(160,30),Enabled=false};Controls.Add(certVersion);
  Controls.Add(new Label{Text="Certification: no request needed; all apps included. Share only in Notes for certification. Anyone with the code can use this version until expiry.",Location=new Point(345,456),Size=new Size(565,50)});
  certification.CheckedChanged+=(o,e)=>{output.Clear();request.Enabled=!certification.Checked;apps.Enabled=!certification.Checked;certVersion.Enabled=certification.Checked;generate.Text=certification.Checked?"Generate certification code":"Generate selected app access";};
  certVersion.TextChanged+=(o,e)=>output.Clear();
  loadKey();
  var exportMobile=new Button{Text="Export key for iPhone",Location=new Point(179,446),Size=new Size(146,34)};Controls.Add(exportMobile);
  exportMobile.Click+=async(o,e)=>{
   try{
    string key=AdminKeyStore.Load();string password;
    using(var prompt=new MobileExportPasswordForm()){if(prompt.ShowDialog(this)!=DialogResult.OK)return;password=prompt.ExportPassword;}
    using(var save=new SaveFileDialog{Title="Save password-protected mobile key",Filter="DTCE encrypted mobile key|*.dtcekey",FileName="DTCE-Mobile-Access.dtcekey",DefaultExt="dtcekey",AddExtension=true}){
     if(save.ShowDialog(this)!=DialogResult.OK)return;exportMobile.Enabled=false;
     try{string encrypted=await System.Threading.Tasks.Task.Run(()=>MobileKeyExport.Create(key,password,AccessCodes.PublicKey()));File.WriteAllText(save.FileName,encrypted,new UTF8Encoding(false));MessageBox.Show(this,"Mobile key exported. Transfer it privately to your iPhone Files app. Do not upload it to GitHub. The original Windows key is unchanged.","Export complete");}
     finally{exportMobile.Enabled=true;password=null;key=null;}
    }
   }catch(Exception){MessageBox.Show(this,"Cannot export the mobile key. Import the matching existing Admin key first and choose a writable file location.","Export failed");}
  };
  ConfigureResponsiveLayout(apps,generate,import,copy,clear,certification,certVersion,exportMobile);
  generate.Click+=(o,e)=>{output.Clear();try{output.Text=certification.Checked?AccessCodes.GenerateCertification(privateKey,certVersion.Text.Trim()):AccessCodes.Generate(request.Text,privateKey,SelectedPrograms(apps));message.Text="Key ID: "+AccessCodes.KeyId(AccessCodes.PublicKey())+(certification.Checked?"\nCertification: all apps, 30 days. User v"+certVersion.Text.Trim()+" only. Expires "+DateTime.UtcNow.AddDays(30).ToLocalTime().ToString("yyyy-MM-dd HH:mm") :"\nApproved for 8 hours from now, including after reopening the user app.");}catch(Exception ex){MessageBox.Show(this,ex.Message,"Cannot generate code");}};
 }
}
#endif
}


