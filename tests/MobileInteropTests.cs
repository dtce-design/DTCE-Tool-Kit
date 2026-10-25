using System;using System.IO;using System.Text;using System.Security.Cryptography;using BeamScheduleFormatter;
class MobileInteropTests {
 const string Password="test-only Unicode passphrase \u00f1 2026";
 static string Url(byte[] b){return Convert.ToBase64String(b).TrimEnd('=').Replace('+','-').Replace('/','_');}
 static int Main(string[] args){
  string folder=args[1];
  if(args[0]=="fixture"){
   Directory.CreateDirectory(folder);
   using(var rsa=new RSACryptoServiceProvider(2048)){rsa.PersistKeyInCsp=false;string pub=rsa.ToXmlString(false),priv=rsa.ToXmlString(true);var p=rsa.ExportParameters(false);
    File.WriteAllText(Path.Combine(folder,"mobile-fixture.json"),MobileKeyExport.Create(priv,Password,pub));
    File.WriteAllText(Path.Combine(folder,"mobile-public.json"),"{\"keyId\":\""+AccessCodes.KeyId(pub)+"\",\"n\":\""+Url(p.Modulus)+"\",\"e\":\""+Url(p.Exponent)+"\"}");
    File.WriteAllText(Path.Combine(folder,"mobile-public.xml"),pub);
    try{MobileKeyExport.Create(priv,"short",pub);throw new Exception("Short password accepted");}catch(ArgumentException){}
   }
   Console.WriteLine("Created ephemeral encrypted mobile fixture. No owner signing key used.");return 0;
  }
  string code=File.ReadAllText(Path.Combine(folder,"mobile-code.txt")),publicKey=File.ReadAllText(Path.Combine(folder,"mobile-public.xml"));DateTime end;
  if(!AccessCodes.ValidateAt("BEAM-0123456789ABCDEF0123456789ABCDEF01234567",code,publicKey,DateTime.UtcNow,out end))throw new Exception("iPhone protocol signature rejected by desktop verifier");
  if(AccessCodes.ValidateAt("BEAM-FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF",code,publicKey,DateTime.UtcNow,out end))throw new Exception("Wrong request accepted");
  Console.WriteLine("PASS: Web Crypto generated code accepted by desktop BA3 verifier; wrong request rejected.");return 0;
 }
}
