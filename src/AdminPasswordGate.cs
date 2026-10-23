#if ADMIN_APP
using System;using System.Drawing;using System.Security.Cryptography;using System.Windows.Forms;
namespace BeamScheduleFormatter {
static class AdminPasswordGate {
 const string Salt="rjxe3n1rtWtt70gzV3zwag==",Hash="mhFHUBKFoA0paIzHrgVy2HapvqfGgsFMgu+Ho0gXsfY=";
 public static bool Verify(string password){if(password==null)return false;byte[] candidate;using(var derive=new Rfc2898DeriveBytes(password,Convert.FromBase64String(Salt),200000))candidate=derive.GetBytes(32);byte[] expected=Convert.FromBase64String(Hash);int difference=0;for(int n=0;n<expected.Length;n++)difference|=candidate[n]^expected[n];Array.Clear(candidate,0,candidate.Length);return difference==0;}
 public static bool Unlock(){string identity=AdminApproval.Identity(System.Reflection.Assembly.GetEntryAssembly().Location);if(AdminApproval.Approved(identity))return true;using(var f=new AdminPasswordForm()){if(f.ShowDialog()!=DialogResult.OK)return false;AdminApproval.Remember(identity);return true;}}
}
class AdminPasswordForm:Form {
 readonly TextBox password;readonly Label error;
 public AdminPasswordForm(){Text="Beam Tools Admin - Password";ClientSize=new Size(455,250);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(242,245,249);Controls.Add(new Label{Text="ADMIN ACCESS",Location=new Point(24,19),Size=new Size(400,34),Font=new Font("Segoe UI",16,FontStyle.Bold),ForeColor=Color.FromArgb(23,43,67)});Controls.Add(new Label{Text="Activate this installation or updated version.",Location=new Point(26,66),Size=new Size(405,27)});password=new TextBox{Location=new Point(27,102),Width=400,UseSystemPasswordChar=true,MaxLength=128};error=new Label{Location=new Point(27,139),Size=new Size(400,40),ForeColor=Color.Firebrick};var open=new Button{Text="Open Admin",Location=new Point(192,196),Size=new Size(120,33)};var cancel=new Button{Text="Cancel",Location=new Point(321,196),Size=new Size(105,33),DialogResult=DialogResult.Cancel};open.Click+=(s,e)=>{if(AdminPasswordGate.Verify(password.Text)){password.Clear();DialogResult=DialogResult.OK;Close();}else{password.Clear();error.Text="Incorrect password. Please try again.";password.Focus();}};Controls.AddRange(new Control[]{password,error,open,cancel});AcceptButton=open;CancelButton=cancel;Shown+=(s,e)=>password.Focus();}
}
}
#endif