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
/* the same password-protected key file opened on another computer (owner, 24 November 2026: "im using another desktop, yung original
   is laptop ko"): the MAC is checked before decrypting, the key must match this app's public key, and the private key never leaves memory
   except as the DPAPI-protected copy of this Windows account */
static class KeyFileImport {
 static string Field(string json,string name){var m=System.Text.RegularExpressions.Regex.Match(json,"\""+name+"\"\\s*:\\s*(\"([^\"]*)\"|([0-9]+))");if(!m.Success)throw new CryptographicException("Not a DTCE key file.");return m.Groups[2].Success&&m.Groups[2].Value!=""?m.Groups[2].Value:m.Groups[3].Value;}
 static byte[] Url(string s){s=s.Replace('-','+').Replace('_','/');while(s.Length%4!=0)s+="=";return Convert.FromBase64String(s);}
 internal static string Open(string fileText,string password){return Open(fileText,password,AccessCodes.PublicKey());}
 internal static string Open(string fileText,string password,string publicKey){
  if(Field(fileText,"format")!="DTCE-MOBILE-KEY-V1")throw new CryptographicException("Not a DTCE key file.");int iterations=int.Parse(Field(fileText,"iterations"));if(iterations<100000||iterations>5000000)throw new CryptographicException("Unsupported key file.");
  string salt64=Field(fileText,"salt"),iv64=Field(fileText,"iv"),cipher64=Field(fileText,"ciphertext"),mac64=Field(fileText,"mac");byte[] derived=null,plain=null;
  try{using(var kdf=new Rfc2898DeriveBytes(password??"",Convert.FromBase64String(salt64),iterations,HashAlgorithmName.SHA256))derived=kdf.GetBytes(64);
   byte[] macKey=new byte[32];Array.Copy(derived,32,macKey,0,32);byte[] expected;using(var hmac=new HMACSHA256(macKey))expected=hmac.ComputeHash(Encoding.UTF8.GetBytes("DTCE-MOBILE-KEY-V1|"+iterations+"|"+salt64+"|"+iv64+"|"+cipher64));Array.Clear(macKey,0,macKey.Length);
   byte[] given=Convert.FromBase64String(mac64);int diff=given.Length^expected.Length;for(int i=0;i<Math.Min(given.Length,expected.Length);i++)diff|=given[i]^expected[i];if(diff!=0)throw new CryptographicException("Wrong password or damaged key file.");
   using(var aes=Aes.Create()){aes.Mode=CipherMode.CBC;aes.Padding=PaddingMode.PKCS7;byte[] k=new byte[32];Array.Copy(derived,k,32);try{aes.Key=k;aes.IV=Convert.FromBase64String(iv64);byte[] c=Convert.FromBase64String(cipher64);using(var d=aes.CreateDecryptor())plain=d.TransformFinalBlock(c,0,c.Length);}finally{Array.Clear(k,0,k.Length);}}
   string json=Encoding.UTF8.GetString(plain);var p=new RSAParameters{Modulus=Url(Field(json,"n")),Exponent=Url(Field(json,"e")),D=Url(Field(json,"d")),P=Url(Field(json,"p")),Q=Url(Field(json,"q")),DP=Url(Field(json,"dp")),DQ=Url(Field(json,"dq")),InverseQ=Url(Field(json,"qi"))};
   using(var rsa=new RSACryptoServiceProvider()){rsa.PersistKeyInCsp=false;rsa.ImportParameters(p);foreach(byte[] part in new[]{p.D,p.P,p.Q,p.DP,p.DQ,p.InverseQ})Array.Clear(part,0,part.Length);
    if(rsa.ToXmlString(false)!=publicKey)throw new CryptographicException("This key file belongs to another DTCE key.");return rsa.ToXmlString(true);}}
  finally{if(derived!=null)Array.Clear(derived,0,derived.Length);if(plain!=null)Array.Clear(plain,0,plain.Length);}}
}
class KeyFilePasswordForm:Form {
 readonly TextBox password=new TextBox{UseSystemPasswordChar=true,MaxLength=256};internal string Password{get{return password.Text;}}
 public KeyFilePasswordForm(){AppBrand.Apply(this);Text="Import key file";ClientSize=new Size(460,200);StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;Font=new Font("Segoe UI",9.25f);
  Controls.Add(new Label{Text="Password of the key file (the one used for \"Export key for iPhone\" on the original computer):",Location=new Point(20,18),Size=new Size(420,45)});
  password.Location=new Point(20,70);password.Size=new Size(420,28);Controls.Add(password);
  var ok=new Button{Text="Import",DialogResult=DialogResult.OK,Location=new Point(20,130),Size=new Size(200,40)};Controls.Add(ok);AcceptButton=ok;var cancel=new Button{Text="Cancel",DialogResult=DialogResult.Cancel,Location=new Point(240,130),Size=new Size(200,40)};Controls.Add(cancel);CancelButton=cancel;}
 protected override void OnFormClosed(FormClosedEventArgs e){base.OnFormClosed(e);}
}
class MobileExportPasswordForm:Form {
 TextBox password,confirm;
 public string ExportPassword {get;private set;}
 public MobileExportPasswordForm(){
  AppBrand.Apply(this);Text="Export key for iPhone";ClientSize=new Size(460,305);StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;Font=new Font("Segoe UI",9.25f);
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
