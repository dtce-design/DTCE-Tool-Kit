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
    File.WriteAllText(Path.Combine(folder,"cert-reference.txt"),AccessCodes.GenerateCertificationAt(priv,Engine.Version,DateTime.UtcNow));
    try{MobileKeyExport.Create(priv,"short",pub);throw new Exception("Short password accepted");}catch(ArgumentException){}
    /* the same key file imported on another computer: round trip, wrong password and another key refused (ephemeral key only) */
    {string file=MobileKeyExport.Create(priv,Password,pub);string back=KeyFileImport.Open(file,Password,pub);using(var r2=new RSACryptoServiceProvider()){r2.PersistKeyInCsp=false;r2.FromXmlString(back);if(r2.ToXmlString(true)!=priv)throw new Exception("Imported key differs");}
     bool refused=false;try{KeyFileImport.Open(file,Password+"x",pub);}catch(CryptographicException){refused=true;}if(!refused)throw new Exception("Wrong password accepted");
     string otherPub;using(var o=new RSACryptoServiceProvider(2048)){o.PersistKeyInCsp=false;otherPub=o.ToXmlString(false);}refused=false;try{KeyFileImport.Open(file,Password,otherPub);}catch(CryptographicException){refused=true;}if(!refused)throw new Exception("Key file of another key accepted");}
   }
   Console.WriteLine("Created ephemeral encrypted mobile fixture. No owner signing key used.");return 0;
  }
  string code=File.ReadAllText(Path.Combine(folder,"mobile-code.txt")),publicKey=File.ReadAllText(Path.Combine(folder,"mobile-public.xml"));DateTime end;
  if(!AccessCodes.ValidateAt("BEAM-0123456789ABCDEF0123456789ABCDEF01234567",code,publicKey,DateTime.UtcNow,out end))throw new Exception("iPhone protocol signature rejected by desktop verifier");
  if(AccessCodes.ValidateAt("BEAM-FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF",code,publicKey,DateTime.UtcNow,out end))throw new Exception("Wrong request accepted");
  string cert=File.ReadAllText(Path.Combine(folder,"mobile-cert.txt"));
  if(cert!=File.ReadAllText(Path.Combine(folder,"cert-reference.txt"))||!AccessCodes.ValidateAt("",cert,publicKey,DateTime.UtcNow,out end))throw new Exception("Mobile certification signature rejected by desktop verifier");
  Console.WriteLine("PASS: Web Crypto certification matches desktop signature exactly and unlocks without a request.");
  Console.WriteLine("PASS: Web Crypto generated code accepted by desktop BA4 verifier; wrong request rejected.");return 0;
 }
}

