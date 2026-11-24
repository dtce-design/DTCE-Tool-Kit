#if ADMIN_APP
using System;using System.IO;using System.Text;using System.Security.Cryptography;
namespace BeamScheduleFormatter {
static class AdminKeyStore {
 static string LocalPath {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DTCE Tool Kit","Admin","AdminPrivateKey.dat");}}
 internal static string Decode(byte[] encrypted){
  byte[] plain=ProtectedData.Unprotect(encrypted,null,DataProtectionScope.CurrentUser);
  try{string key=Encoding.UTF8.GetString(plain);using(var rsa=new RSACryptoServiceProvider()){rsa.PersistKeyInCsp=false;rsa.FromXmlString(key);if(rsa.PublicOnly||rsa.ToXmlString(false)!=AccessCodes.PublicKey())throw new CryptographicException("Key mismatch");}return key;}
  finally{Array.Clear(plain,0,plain.Length);}
 }
 public static string Load(){string path=File.Exists(LocalPath)?LocalPath:Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"AdminPrivateKey.dat");return Decode(File.ReadAllBytes(path));}
 /* a private key opened from a password-protected key file, stored for this Windows account only (DPAPI) */
 public static void ImportKey(string privateKeyXml){byte[] plain=Encoding.UTF8.GetBytes(privateKeyXml);byte[] encrypted;try{encrypted=ProtectedData.Protect(plain,null,DataProtectionScope.CurrentUser);}finally{Array.Clear(plain,0,plain.Length);}
  Decode(encrypted);string target=LocalPath;Directory.CreateDirectory(Path.GetDirectoryName(target));string temp=target+"."+Guid.NewGuid().ToString("N")+".tmp";
  try{File.WriteAllBytes(temp,encrypted);if(File.Exists(target))File.Replace(temp,target,null);else File.Move(temp,target);}finally{Array.Clear(encrypted,0,encrypted.Length);if(File.Exists(temp))File.Delete(temp);}}
 public static void Import(string source){
  byte[] encrypted=File.ReadAllBytes(source);Decode(encrypted);
  string target=LocalPath;Directory.CreateDirectory(Path.GetDirectoryName(target));string temp=target+"."+Guid.NewGuid().ToString("N")+".tmp";
  try{File.WriteAllBytes(temp,encrypted);if(File.Exists(target))File.Replace(temp,target,null);else File.Move(temp,target);}
  finally{Array.Clear(encrypted,0,encrypted.Length);if(File.Exists(temp))File.Delete(temp);}
 }
}
}
#endif
