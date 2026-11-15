using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Diagnostics;using System.Security.Cryptography;using System.Text;
namespace BeamScheduleFormatter {
// AES-256 (revision 6) permissions are enforced by conforming readers, not a tamper-proof guarantee.
static class PdfProtection {
 internal const string ToolVersion="12.4.2";
 static readonly Dictionary<string,string> Hashes=new Dictionary<string,string>{
  {"qpdf.exe","7a155e0fe51833087d13b7b6a29f4f877bcd2fbcae4354fe41ff2bea2f79e090"},
  {"qpdf30.dll","fc358bbc603b8b2b109af437a0707edce47503336d41151001adcb2c94f43131"},
  {"libgcc_s_seh-1.dll","d7e343dd266747177c3699bc3201ea36eceef158e98530e089263bc55bb2a6d7"},
  {"libstdc++-6.dll","494bd9045948198ef4d3bc838c7362d4cfd49c890b439c096b1817aa7c7fa6ff"},
  {"libwinpthread-1.dll","72bf8802de3a99d28f0bc2db9a11f281a9f9f3fa83dab2bc67e69fac3bd3d9c0"}
 };
 internal static void WriteFinal(string input,string destination){
  if(!Environment.Is64BitOperatingSystem)throw new NotSupportedException("AES-256 PDF export requires Windows x64.");
  string root=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"tools","qpdf");
  foreach(var file in Hashes){string path=Path.Combine(root,file.Key);if(!File.Exists(path))throw new IOException("PDF security components are missing. Extract the complete versioned app folder, including tools/qpdf.");using(var sha=SHA256.Create())using(var stream=File.OpenRead(path)){string hash=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();if(hash!=file.Value)throw new IOException("PDF security component verification failed. Extract a fresh copy of the app package.");}}
  ProtectWithTool(input,destination,Path.Combine(root,"qpdf.exe"));
 }
 internal static void ProtectWithTool(string input,string destination,string tool){
  string full=Path.GetFullPath(destination),secured=full+"."+Guid.NewGuid().ToString("N")+".secured";
  var owner=new byte[32];using(var random=RandomNumberGenerator.Create())random.GetBytes(owner);
  string password=BitConverter.ToString(owner).Replace("-","");
  try{
   Run(tool,new[]{"--encrypt","--user-password=","--owner-password="+password,"--bits=256","--print=full","--modify=none","--extract=n","--annotate=n","--assemble=n","--form=n","--modify-other=n","--",Path.GetFullPath(input),secured});
   string info=Run(tool,new[]{"--show-encryption",secured});
   string[] required={"R = 6","stream encryption method: AESv3","string encryption method: AESv3","file encryption method: AESv3","print high resolution: allowed","extract for any purpose: not allowed","modify document assembly: not allowed","modify forms: not allowed","modify annotations: not allowed","modify other: not allowed","modify anything: not allowed"};
   if(required.Any(value=>!info.Contains(value)))throw new IOException("PDF security verification failed. The destination was not replaced.");
   if(File.Exists(full))File.Replace(secured,full,null);else File.Move(secured,full);
  }finally{Array.Clear(owner,0,owner.Length);if(File.Exists(secured))File.Delete(secured);}
 }
 internal static string Quote(string value){var b=new StringBuilder("\"");int slashes=0;foreach(char c in value){if(c=='\\'){slashes++;continue;}if(c=='\"'){b.Append('\\',slashes*2+1).Append(c);slashes=0;continue;}b.Append('\\',slashes).Append(c);slashes=0;}b.Append('\\',slashes*2).Append('"');return b.ToString();}
 static string Run(string executable,IEnumerable<string> arguments){
  var info=new ProcessStartInfo(executable,string.Join(" ",arguments.Select(Quote))){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
  using(var process=Process.Start(info)){var output=process.StandardOutput.ReadToEndAsync();var errors=process.StandardError.ReadToEndAsync();if(!process.WaitForExit(30000)){try{process.Kill();}catch{}throw new IOException("PDF security processing timed out. The destination was not replaced.");}System.Threading.Tasks.Task.WaitAll(output,errors);if(process.ExitCode!=0)throw new IOException("PDF security processing failed. The destination was not replaced.");return output.Result;}
 }
}
}
