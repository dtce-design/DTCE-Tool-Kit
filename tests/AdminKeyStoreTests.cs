using System;using System.Text;using System.Security.Cryptography;using BeamScheduleFormatter;
class AdminKeyStoreTests {
 static int Main(){int checks=0;
  try{AdminKeyStore.Decode(new byte[]{1,2,3});throw new Exception("Corrupt key accepted");}catch(CryptographicException){checks++;}
  using(var rsa=new RSACryptoServiceProvider(2048)){rsa.PersistKeyInCsp=false;byte[] plain=Encoding.UTF8.GetBytes(rsa.ToXmlString(true));
   try{byte[] encrypted=ProtectedData.Protect(plain,null,DataProtectionScope.CurrentUser);try{AdminKeyStore.Decode(encrypted);throw new Exception("Mismatched key accepted");}catch(CryptographicException){checks++;}}
   finally{Array.Clear(plain,0,plain.Length);}}
  Console.WriteLine("PASS: "+checks+" Admin key import corruption/mismatch checks; ephemeral key only.");return 0;
 }
}
