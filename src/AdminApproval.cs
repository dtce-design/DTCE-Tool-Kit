
using System;using System.IO;using System.Text;using System.Security.Cryptography;using System.Runtime.InteropServices;using Microsoft.Win32.SafeHandles;
namespace BeamScheduleFormatter {static class AdminApproval {
 public static string DirectoryPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BeamTools","AdminActivation");
 [StructLayout(LayoutKind.Sequential)]struct FileInfoNative {public uint Attr;public System.Runtime.InteropServices.ComTypes.FILETIME Created,Accessed,Written;public uint Volume,SizeHigh,SizeLow,Links,IndexHigh,IndexLow;}
 [DllImport("kernel32.dll",SetLastError=true)]static extern bool GetFileInformationByHandle(SafeFileHandle file,out FileInfoNative info);
 static byte[] Hash(byte[] bytes){using(var s=SHA256.Create())return s.ComputeHash(bytes);}
 public static string Identity(string path){path=Path.GetFullPath(path);using(var file=File.OpenRead(path)){FileInfoNative info;string id=GetFileInformationByHandle(file.SafeFileHandle,out info)?info.Volume+":"+info.IndexHigh+":"+info.IndexLow:"";byte[] digest;using(var s=SHA256.Create())digest=s.ComputeHash(file);return Convert.ToBase64String(Hash(Encoding.UTF8.GetBytes(path.ToUpperInvariant()+"|"+Engine.Version+"|"+File.GetCreationTimeUtc(path).Ticks+"|"+id+"|"+Convert.ToBase64String(digest))));}}
 static string PathFor(string identity){return Path.Combine(DirectoryPath,BitConverter.ToString(Hash(Encoding.UTF8.GetBytes(identity))).Replace("-","")+".dat");}
 public static bool Approved(string identity){try{string path=PathFor(identity);if(!File.Exists(path))return false;string saved=Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(path),null,DataProtectionScope.CurrentUser));return saved==identity;}catch{return false;}}
 public static void Remember(string identity){Directory.CreateDirectory(DirectoryPath);string path=PathFor(identity),temp=path+".tmp";try{File.WriteAllBytes(temp,ProtectedData.Protect(Encoding.UTF8.GetBytes(identity),null,DataProtectionScope.CurrentUser));File.Copy(temp,path,true);}finally{if(File.Exists(temp))File.Delete(temp);}}
}}

