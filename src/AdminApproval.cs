
using System;using System.IO;using System.Text;using System.Security.Cryptography;using System.Runtime.InteropServices;using Microsoft.Win32.SafeHandles;
namespace BeamScheduleFormatter {static class AdminApproval {
 public static string DirectoryPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BeamTools","AdminActivation");
 [StructLayout(LayoutKind.Sequential)]struct FileInfoNative {public uint Attr;public System.Runtime.InteropServices.ComTypes.FILETIME Created,Accessed,Written;public uint Volume,SizeHigh,SizeLow,Links,IndexHigh,IndexLow;}
 [DllImport("kernel32.dll",SetLastError=true)]static extern bool GetFileInformationByHandle(SafeFileHandle file,out FileInfoNative info);
 static byte[] Hash(byte[] bytes){using(var s=SHA256.Create())return s.ComputeHash(bytes);}
 public static string Identity(string path){path=Path.GetFullPath(path);using(var file=File.OpenRead(path)){FileInfoNative info;string id=GetFileInformationByHandle(file.SafeFileHandle,out info)?info.Volume+":"+info.IndexHigh+":"+info.IndexLow:"";byte[] digest;using(var s=SHA256.Create())digest=s.ComputeHash(file);return Convert.ToBase64String(Hash(Encoding.UTF8.GetBytes(path.ToUpperInvariant()+"|"+Engine.Version+"|"+File.GetCreationTimeUtc(path).Ticks+"|"+id+"|"+Convert.ToBase64String(digest))));}}
 static string PathFor(string identity){return Path.Combine(DirectoryPath,BitConverter.ToString(Hash(Encoding.UTF8.GetBytes(identity))).Replace("-","")+".dat");}
 /* Owner (19 November 2026, "yung admin app manghingi ng password kapag after shut down and every restart"): an approval holds
    only for the Windows sign-in it was given in. The sign-in is identified by the start of the user's shell (explorer.exe) in
    this session - it starts again after every restart and after a shut down, also with Windows Fast Startup, which keeps the
    system uptime - with the system start time as fallback. */
 [DllImport("kernel32.dll")]static extern ulong GetTickCount64();
 internal static DateTime SessionStart(){DateTime? shell=null;try{int session=System.Diagnostics.Process.GetCurrentProcess().SessionId;foreach(var p in System.Diagnostics.Process.GetProcessesByName("explorer")){try{if(p.SessionId!=session)continue;var t=p.StartTime.ToUniversalTime();if(!shell.HasValue||t<shell.Value)shell=t;}catch{}finally{p.Dispose();}}}catch{}
  if(shell.HasValue)return shell.Value;try{return DateTime.UtcNow-TimeSpan.FromMilliseconds(GetTickCount64());}catch{return DateTime.UtcNow;}}
 public static bool Approved(string identity){try{string path=PathFor(identity);if(!File.Exists(path))return false;string saved=Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(path),null,DataProtectionScope.CurrentUser));int bar=saved.LastIndexOf("|session=",StringComparison.Ordinal);if(bar<0||saved.Substring(0,bar)!=identity)return false;long ticks;if(!long.TryParse(saved.Substring(bar+9),out ticks))return false;return Math.Abs((SessionStart()-new DateTime(ticks,DateTimeKind.Utc)).TotalMinutes)<2;}catch{return false;}}
 public static void Remember(string identity){Directory.CreateDirectory(DirectoryPath);string path=PathFor(identity),temp=path+".tmp";try{File.WriteAllBytes(temp,ProtectedData.Protect(Encoding.UTF8.GetBytes(identity+"|session="+SessionStart().Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture)),null,DataProtectionScope.CurrentUser));File.Copy(temp,path,true);}finally{if(File.Exists(temp))File.Delete(temp);}}
}}

