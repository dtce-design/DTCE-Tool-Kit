using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Diagnostics;

namespace BeamScheduleFormatter {
class AccessLease {
 readonly Func<TimeSpan> now;TimeSpan started,validFor;bool granted;
 public static readonly TimeSpan Duration=TimeSpan.FromHours(8);
 public AccessLease(Func<TimeSpan> clock){now=clock;}
 public void Start(){Start(Duration);}
 public void Start(TimeSpan remaining){Start(remaining,false);}
 public void Start(TimeSpan remaining,bool certification){started=now();validFor=remaining;granted=remaining>TimeSpan.Zero&&remaining<=(certification?TimeSpan.FromDays(30):Duration);}
 public void Reset(){granted=false;}
 public bool IsValid {get{TimeSpan elapsed=now()-started;return granted&&elapsed>=TimeSpan.Zero&&elapsed<validFor;}}
}
static class AccessCodes {
 static readonly AccessLease lease=new AccessLease(()=>TimeSpan.FromSeconds((double)Stopwatch.GetTimestamp()/Stopwatch.Frequency));
 static string sessionRequest;static DateTime requestExpiresUtc=DateTime.MinValue;
 static int allowedApps; public const int AllApps=ProgramAccess.All;
 static DateTime expiresUtc=DateTime.MinValue;
 static string storePath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BeamScheduleFormatter","access-v3-"+InstallationKey(System.Reflection.Assembly.GetExecutingAssembly().Location)+".dat");
 static readonly DateTime Epoch=new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc);
 public static string InstallationKey(string path){return AdminApproval.Identity(path).Replace('+' ,'-').Replace('/' ,'_').TrimEnd('=');}
 public static bool Granted {get{return lease.IsValid&&DateTime.UtcNow<expiresUtc;}set{if(value){allowedApps=AllApps;expiresUtc=DateTime.UtcNow.AddHours(8);lease.Start();}else{allowedApps=0;expiresUtc=DateTime.MinValue;lease.Reset();}}}
 public static bool CanUseFormatter {
  get {
   #if ADMIN_APP
   return true;
   #else
   return Granted;
   #endif
  }
 }
 public static bool CanUseApp(int app){
 if(StoreUpdateState.Blocked)return false;
 #if ADMIN_APP
 return true;
 #else
 return Granted&&ProgramAccess.Allows(allowedApps,app);
 #endif
 }
 public static int AppFor(System.Windows.Forms.Form form){return (form is EstimatorForm||form is SlabEstimatorForm||form is ProgramOfWorksForm)?2:form is RebarLengthForm?4:form is PurlinDesignForm?16:form is TrussConnectionForm?32:form is CuttingListForm?ProgramAccess.BarBendingSchedule:form is DesignSheetForm?ProgramAccess.ForSheet(((DesignSheetForm)form).Kind):form is WindDesignForm?128:form is SlabFormatterForm?64:form is ProjectInformationForm?8:1;} public static string NewRequest(){byte[] bytes=new byte[20];using(var r=RandomNumberGenerator.Create())r.GetBytes(bytes);/* request prefix DTCE- (owner, 20 November 2026: "pwede bang palitan yung prefix ng DTCE?"); BEAM- requests from older versions stay valid */return "DTCE-"+BitConverter.ToString(bytes).Replace("-","");}
 public static string Normalize(string value){return new string((value??"").Where(c=>!char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();}
 static string CleanCode(string value){return new string((value??"").Where(c=>!char.IsWhiteSpace(c)).ToArray());}
 public static bool ValidRequest(string value){return System.Text.RegularExpressions.Regex.IsMatch(Normalize(value),"^(DTCE|BEAM)-[A-F0-9]{40}$");}
 public static string PublicKey(){using(var s=typeof(AccessCodes).Assembly.GetManifestResourceStream("AccessPublicKey.xml")){if(s==null)throw new Exception("Missing embedded access verification key.");using(var r=new StreamReader(s))return r.ReadToEnd();}}
 public static string KeyId(string publicKey){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(publicKey))).Replace("-","").Substring(0,16);}
 public static string Generate(string request,string privateKey){return GenerateAt(request,privateKey,DateTime.UtcNow,AllApps);}
 public static string Generate(string request,string privateKey,int apps){return GenerateAt(request,privateKey,DateTime.UtcNow,apps);}
 public static string GenerateAt(string request,string privateKey,DateTime issuedUtc){return GenerateAt(request,privateKey,issuedUtc,AllApps);}
 public static string GenerateAt(string request,string privateKey,DateTime issuedUtc,int apps){if(apps<1||(apps&~AllApps)!=0)throw new Exception("Select at least one app.");
  request=Normalize(request);if(!ValidRequest(request))throw new Exception("Paste the complete request code from the user's formatter.");
  long issued=(long)(issuedUtc.ToUniversalTime()-Epoch).TotalSeconds,expiry=checked(issued+28800);
  string times=issued.ToString(CultureInfo.InvariantCulture)+"."+expiry.ToString(CultureInfo.InvariantCulture);
  byte[] message=Encoding.UTF8.GetBytes("BEAM-ACCESS-V4|"+request+"|"+times+"|"+apps);
  using(var rsa=new RSACryptoServiceProvider()){rsa.PersistKeyInCsp=false;rsa.FromXmlString(privateKey);byte[] sig=rsa.SignData(message,CryptoConfig.MapNameToOID("SHA256"));return "BA4."+times+"."+apps+"."+Convert.ToBase64String(sig);}
 }
 public static bool ValidCertificationVersion(string value){
  Version parsed;return !string.IsNullOrEmpty(value)&&Version.TryParse(value,out parsed)&&parsed.Revision==-1&&parsed.Build>=0&&parsed.Major<=65535&&parsed.Minor<=65535&&parsed.Build<=65535&&parsed.ToString(3)==value;
 }
 public static string GenerateCertification(string privateKey,string version){return GenerateCertificationAt(privateKey,version,DateTime.UtcNow);}
 public static string GenerateCertificationAt(string privateKey,string version,DateTime issuedUtc){
  if(!ValidCertificationVersion(version))throw new ArgumentException("Enter the exact User app version, for example "+Engine.Version+" (three numbers).");
  long issued=(long)(issuedUtc.ToUniversalTime()-Epoch).TotalSeconds,expiry=checked(issued+2592000);
  string times=issued.ToString(CultureInfo.InvariantCulture)+"."+expiry.ToString(CultureInfo.InvariantCulture),target=version.Replace('.','-');
  using(var rsa=new RSACryptoServiceProvider()){rsa.PersistKeyInCsp=false;rsa.FromXmlString(privateKey);byte[] sig=rsa.SignData(Encoding.UTF8.GetBytes("DTCE-CERTIFICATION-V1|"+times+"|7|"+target),CryptoConfig.MapNameToOID("SHA256"));return "BC1."+times+".7."+target+"."+Convert.ToBase64String(sig);}
 }
 public static bool IsCertification(string code){return CleanCode(code).StartsWith("BC1.",StringComparison.Ordinal);}
 public static bool Verify(string request,string code,string publicKey){DateTime end;return ValidateAt(request,code,publicKey,DateTime.UtcNow,out end);}
 public static bool ValidateAt(string request,string code,string publicKey,DateTime nowUtc,out DateTime end){
  return ValidateForVersionAt(request,code,publicKey,nowUtc,Engine.Version,out end);
 }
 internal static bool ValidateForVersionAt(string request,string code,string publicKey,DateTime nowUtc,string version,out DateTime end){
  end=DateTime.MinValue;
  try{
   if(string.IsNullOrWhiteSpace(code)||code.Length>2048)return false;
   if(IsCertification(code)){
    string[] cert=CleanCode(code).Split('.');long issuedCert,expiryCert;
    if(cert.Length!=6||cert[3]!="7"||!ValidCertificationVersion(version)||cert[4]!=version.Replace('.','-')||!long.TryParse(cert[1],NumberStyles.None,CultureInfo.InvariantCulture,out issuedCert)||!long.TryParse(cert[2],NumberStyles.None,CultureInfo.InvariantCulture,out expiryCert)||issuedCert<0||expiryCert<issuedCert||expiryCert-issuedCert!=2592000)return false;
    DateTime startCert=Epoch.AddSeconds(issuedCert);DateTime endCert=Epoch.AddSeconds(expiryCert);
    if(startCert>nowUtc.ToUniversalTime().AddMinutes(5)||endCert<=nowUtc.ToUniversalTime())return false;
    using(var rsa=new RSACryptoServiceProvider()){rsa.PersistKeyInCsp=false;rsa.FromXmlString(publicKey);if(!rsa.VerifyData(Encoding.UTF8.GetBytes("DTCE-CERTIFICATION-V1|"+cert[1]+"."+cert[2]+"|7|"+cert[4]),CryptoConfig.MapNameToOID("SHA256"),Convert.FromBase64String(cert[5])))return false;}
    end=endCert;return true;
   }
   request=Normalize(request);if(!ValidRequest(request)||string.IsNullOrWhiteSpace(code)||code.Length>2048)return false;
   string[] parts=CleanCode(code).Split('.');long issued,expiry;int apps;
   if(parts.Length!=5||(parts[0]!="BA3"&&parts[0]!="BA4")||!int.TryParse(parts[3],NumberStyles.None,CultureInfo.InvariantCulture,out apps)||apps<1||(apps&~(parts[0]=="BA3"?7:AllApps))!=0||!long.TryParse(parts[1],NumberStyles.None,CultureInfo.InvariantCulture,out issued)||!long.TryParse(parts[2],NumberStyles.None,CultureInfo.InvariantCulture,out expiry)||issued<0||expiry<issued||expiry-issued!=28800)return false;
   DateTime start=Epoch.AddSeconds(issued);end=Epoch.AddSeconds(expiry);
   if(start>nowUtc.ToUniversalTime().AddMinutes(5)||end<=nowUtc.ToUniversalTime())return false;
   byte[] sig=Convert.FromBase64String(parts[4]);
   using(var rsa=new RSACryptoServiceProvider()){rsa.PersistKeyInCsp=false;rsa.FromXmlString(publicKey);return rsa.VerifyData(Encoding.UTF8.GetBytes((parts[0]=="BA3"?"BEAM-ACCESS-V3|":"BEAM-ACCESS-V4|")+request+"|"+parts[1]+"."+parts[2]+"|"+parts[3]),CryptoConfig.MapNameToOID("SHA256"),sig);}
  }catch(FormatException){return false;}catch(CryptographicException){return false;}catch(ArgumentOutOfRangeException){return false;}catch(System.Xml.XmlException){return false;}
 }
 static string[] Load(){
  try{if(!File.Exists(storePath))return null;byte[] b=ProtectedData.Unprotect(File.ReadAllBytes(storePath),null,DataProtectionScope.CurrentUser);try{string[] data=Encoding.UTF8.GetString(b).Split('\n');return data.Length==4&&ValidRequest(data[0])?data:null;}finally{Array.Clear(b,0,b.Length);}}
  catch(IOException){return null;}catch(CryptographicException){return null;}catch(UnauthorizedAccessException){return null;}
 }
 static void Save(string request,string code,DateTime lastSeen){
  string folder=Path.GetDirectoryName(storePath);Directory.CreateDirectory(folder);
  byte[] plain=Encoding.UTF8.GetBytes(request+"\n"+CleanCode(code)+"\n"+lastSeen.ToUniversalTime().Ticks.ToString(CultureInfo.InvariantCulture)+"\nV2");
  string temp=storePath+"."+Guid.NewGuid().ToString("N")+".tmp";
  try{File.WriteAllBytes(temp,ProtectedData.Protect(plain,null,DataProtectionScope.CurrentUser));if(File.Exists(storePath))File.Replace(temp,storePath,null);else File.Move(temp,storePath);}
  finally{Array.Clear(plain,0,plain.Length);if(File.Exists(temp))File.Delete(temp);}
 }
 static bool ClockOkay(string[] data){long ticks;return data!=null&&long.TryParse(data[2],out ticks)&&ticks<=DateTime.UtcNow.AddMinutes(5).Ticks;}
 public static string GetRequest(){
  if(sessionRequest==null||DateTime.UtcNow>=requestExpiresUtc){sessionRequest=NewRequest();requestExpiresUtc=DateTime.UtcNow.AddHours(8);}
  return sessionRequest;
 }
 public static bool TryRestore(){
  lease.Reset();allowedApps=0;expiresUtc=DateTime.MinValue;sessionRequest=null;requestExpiresUtc=DateTime.MinValue;var data=Load();DateTime end;
  if(!ClockOkay(data)||!IsCertification(data[1])||!ValidateAt(data[0],data[1],PublicKey(),DateTime.UtcNow,out end))return false;
  sessionRequest=data[0];requestExpiresUtc=end;Save(data[0],data[1],DateTime.UtcNow);allowedApps=AppsForApprovedCode(data[1]);expiresUtc=end;lease.Start(end-DateTime.UtcNow,IsCertification(data[1]));return Granted;
 }
 internal static int AppsForApprovedCode(string code){if(IsCertification(code))return AllApps;var parts=CleanCode(code).Split('.');int mask=int.Parse(parts[3],CultureInfo.InvariantCulture);return parts[0]=="BA3"?ProgramAccess.LegacyMask(mask):mask;}
 public static bool Accept(string request,string code){
  DateTime end;if(!ValidateAt(request,code,PublicKey(),DateTime.UtcNow,out end))return false;
  if(IsCertification(code))Save(Normalize(request),code,DateTime.UtcNow);else if(File.Exists(storePath))File.Delete(storePath);
  sessionRequest=Normalize(request);requestExpiresUtc=end;allowedApps=AppsForApprovedCode(code);expiresUtc=end;lease.Start(end-DateTime.UtcNow,IsCertification(code));return Granted;
 }
 public static void Checkpoint(){
  if(!Granted)return;var data=Load();if(data==null||!IsCertification(data[1]))return;long ticks;if(data!=null&&long.TryParse(data[2],out ticks)&&DateTime.UtcNow.Ticks-ticks>TimeSpan.FromMinutes(1).Ticks)Save(data[0],data[1],DateTime.UtcNow);
 }
}
}


