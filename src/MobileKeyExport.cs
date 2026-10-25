#if ADMIN_APP
using System;using System.IO;using System.Text;using System.Security.Cryptography;using System.Drawing;using System.Windows.Forms;
namespace BeamScheduleFormatter {
static class MobileKeyExport {
 public const int Iterations=600000;
 static string Url(byte[] bytes){return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+','-').Replace('/','_');}
 public static string Create(string privateKey,string password,string publicKey){
  if(string.IsNullOrEmpty(password)||password.Length<12)throw new ArgumentException("Use at least 12 characters for the mobile key password.");
  byte[] plain=null,derived=null;
  using(var rsa=new RSACryptoServiceProvider()){
   rsa.PersistKeyInCsp=false;rsa.FromXmlString(privateKey);
   if(rsa.PublicOnly||rsa.ToXmlString(false)!=publicKey)throw new CryptographicException("Key mismatch");
   RSAParameters p=rsa.ExportParameters(true);
   try{
    string jwk="{\"kty\":\"RSA\",\"n\":\""+Url(p.Modulus)+"\",\"e\":\""+Url(p.Exponent)+"\",\"d\":\""+Url(p.D)+"\",\"p\":\""+Url(p.P)+"\",\"q\":\""+Url(p.Q)+"\",\"dp\":\""+Url(p.DP)+"\",\"dq\":\""+Url(p.DQ)+"\",\"qi\":\""+Url(p.InverseQ)+"\"}";
    plain=Encoding.UTF8.GetBytes("{\"keyId\":\""+AccessCodes.KeyId(publicKey)+"\",\"jwk\":"+jwk+"}");
   }finally{foreach(byte[] part in new[]{p.D,p.P,p.Q,p.DP,p.DQ,p.InverseQ})if(part!=null)Array.Clear(part,0,part.Length);}
  }
  try{
   byte[] salt=new byte[16],iv=new byte[16];using(var rng=RandomNumberGenerator.Create()){rng.GetBytes(salt);rng.GetBytes(iv);}
   using(var kdf=new Rfc2898DeriveBytes(password,salt,Iterations,HashAlgorithmName.SHA256))derived=kdf.GetBytes(64);
   byte[] cipher;
   using(var aes=Aes.Create()){aes.Mode=CipherMode.CBC;aes.Padding=PaddingMode.PKCS7;aes.KeySize=256;byte[] aesKey=new byte[32];Array.Copy(derived,aesKey,32);try{aes.Key=aesKey;aes.IV=iv;using(var encryptor=aes.CreateEncryptor())cipher=encryptor.TransformFinalBlock(plain,0,plain.Length);}finally{Array.Clear(aesKey,0,aesKey.Length);}}
   string salt64=Convert.ToBase64String(salt),iv64=Convert.ToBase64String(iv),cipher64=Convert.ToBase64String(cipher);
   string message="DTCE-MOBILE-KEY-V1|"+Iterations+"|"+salt64+"|"+iv64+"|"+cipher64;
   byte[] macKey=new byte[32];Array.Copy(derived,32,macKey,0,32);string mac;
   try{using(var hmac=new HMACSHA256(macKey))mac=Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(message)));}finally{Array.Clear(macKey,0,macKey.Length);}
   return "{\"format\":\"DTCE-MOBILE-KEY-V1\",\"iterations\":"+Iterations+",\"salt\":\""+salt64+"\",\"iv\":\""+iv64+"\",\"ciphertext\":\""+cipher64+"\",\"mac\":\""+mac+"\"}";
  }finally{if(plain!=null)Array.Clear(plain,0,plain.Length);if(derived!=null)Array.Clear(derived,0,derived.Length);}
 }
}
class MobileExportPasswordForm:Form {
 TextBox password,confirm;
 public string ExportPassword {get;private set;}
 public MobileExportPasswordForm(){
  AppBrand.Apply(this);Text="Export key for iPhone";ClientSize=new Size(460,305);StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;Font=new Font("Segoe UI",10);
  Controls.Add(new Label{Text="Protect the mobile signing key with a strong password. Keep the exported file and password private.",Location=new Point(20,18),Size=new Size(420,55)});
  Controls.Add(new Label{Text="Password (at least 12 characters)",Location=new Point(20,82),Size=new Size(420,23)});
  password=new TextBox{UseSystemPasswordChar=true,Location=new Point(20,109),Size=new Size(420,28),MaxLength=256};Controls.Add(password);
  Controls.Add(new Label{Text="Confirm password",Location=new Point(20,150),Size=new Size(420,23)});
  confirm=new TextBox{UseSystemPasswordChar=true,Location=new Point(20,177),Size=new Size(420,28),MaxLength=256};Controls.Add(confirm);
  var export=new Button{Text="Continue",Location=new Point(20,238),Size=new Size(200,40)};Controls.Add(export);AcceptButton=export;
  var cancel=new Button{Text="Cancel",DialogResult=DialogResult.Cancel,Location=new Point(240,238),Size=new Size(200,40)};Controls.Add(cancel);CancelButton=cancel;
  export.Click+=(o,e)=>{if(password.Text.Length<12||password.Text!=confirm.Text){MessageBox.Show(this,"Enter matching passwords with at least 12 characters.");return;}ExportPassword=password.Text;DialogResult=DialogResult.OK;};
 }
 protected override void OnFormClosed(FormClosedEventArgs e){password.Clear();confirm.Clear();base.OnFormClosed(e);}
}
}
#endif
