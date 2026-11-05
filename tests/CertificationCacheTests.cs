using System;using System.IO;using System.Reflection;using BeamScheduleFormatter;
class CertificationCacheTests {
 static int checks;
 static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
 static int Main(string[] args){
  string folder=args[0],isolated=Path.Combine(Path.GetFullPath(folder),"DTCE-cert-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(isolated);
  var field=typeof(AccessCodes).GetField("storePath",BindingFlags.NonPublic|BindingFlags.Static);string original=(string)field.GetValue(null);
  try{
   field.SetValue(null,Path.Combine(isolated,"approval.dat"));
   Check(!AccessCodes.TryRestore(),"Empty cache granted access");
   string request=AccessCodes.GetRequest(),code=File.ReadAllText(Path.Combine(folder,"mobile-cert.txt"));
   Check(AccessCodes.Accept(request,code),"Certification not accepted by real activation path");
   foreach(int app in new[]{1,2,4,8,16,32,64})Check(AccessCodes.CanUseApp(app),"Certification app unavailable");
   AccessCodes.Granted=false;
   Check(AccessCodes.TryRestore(),"Certification lost on reopen");
   Check(AccessCodes.GetRequest()==request,"Valid cached request changed");
   AccessCodes.Checkpoint();
   field.SetValue(null,Path.Combine(isolated,"fresh-install.dat"));AccessCodes.Granted=false;
   Check(!AccessCodes.TryRestore(),"Fresh installation inherited access");
   Check(AccessCodes.Accept(AccessCodes.GetRequest(),code),"Certification cannot unlock second test machine");
   string normal=File.ReadAllText(Path.Combine(folder,"mobile-code.txt"));
   Check(AccessCodes.Accept("BEAM-0123456789ABCDEF0123456789ABCDEF01234567",normal),"Normal activation regressed");
   Check(AccessCodes.CanUseApp(1)&&AccessCodes.CanUseApp(4)&&AccessCodes.CanUseApp(8)&&!AccessCodes.CanUseApp(2)&&!AccessCodes.CanUseApp(16)&&!AccessCodes.CanUseApp(32)&&!AccessCodes.CanUseApp(64),"Normal app restrictions changed");
   string firstRequest=AccessCodes.GetRequest();
   AccessCodes.Checkpoint();Check(!File.Exists((string)field.GetValue(null)),"Normal approval was persisted");
   AccessCodes.Granted=false;Check(!AccessCodes.TryRestore(),"Normal approval restored after app restart");
   string secondRequest=AccessCodes.GetRequest();Check(firstRequest!=secondRequest,"Request reused across sessions");
   Check(!AccessCodes.Accept(secondRequest,normal),"Previous session code accepted on new request");
   // Simulate an approval left by the previous release: it must not reopen normal access.
   var save=typeof(AccessCodes).GetMethod("Save",BindingFlags.NonPublic|BindingFlags.Static);
   save.Invoke(null,new object[]{"BEAM-0123456789ABCDEF0123456789ABCDEF01234567",normal,DateTime.UtcNow});
   Check(!AccessCodes.TryRestore(),"Legacy normal cache bypassed session policy");
   File.WriteAllBytes((string)field.GetValue(null),new byte[]{1,2,3});AccessCodes.Granted=false;Check(!AccessCodes.TryRestore(),"Damaged cache accepted");
   Console.WriteLine("PASS: "+checks+" isolated DPAPI activation, certification restore, normal session restart and app selection checks; ephemeral test key only.");return 0;
  }finally{AccessCodes.Granted=false;field.SetValue(null,original);foreach(string file in Directory.GetFiles(isolated))File.Delete(file);Directory.Delete(isolated);}
 }
}

