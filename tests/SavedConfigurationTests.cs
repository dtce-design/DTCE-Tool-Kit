using System;using System.IO;using System.Linq;using BeamScheduleFormatter;
class SavedConfigurationTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static void Main(){string dir=Path.Combine(Path.GetTempPath(),"dtce-config-"+Guid.NewGuid().ToString("N"));CalculationStore.StorageDirectory=dir;int changed=0;CalculationStore.Changed+=()=>changed++;
  try{int builtIns=CalculationStore.All().Count;Check(builtIns==13,"12 built-ins plus the sample");
   var custom=CalculationStore.Clone(CalculationStore.All().First());custom.Name="Project A fc 30";custom.Input.Fc=30;CalculationStore.Save(custom);Check(CalculationStore.All().Any(c=>c.Name=="Project A fc 30"),"saved");
   CalculationStore.Delete(new[]{"Project A fc 30","Sample 27.6MPa / 414MPa"});var names=CalculationStore.All().Select(c=>c.Name).ToList();Check(!names.Contains("Project A fc 30")&&!names.Contains("Sample 27.6MPa / 414MPa")&&names.Count==builtIns-1,"user and generated configurations removed from the list");Check(changed==2,"Changed raised for save and delete");
   var resolved=CalculationStore.Resolve(27.6,60,414,60,414);Check(Math.Abs(resolved.Input.Fc-27.6)<1e-9&&resolved.FySmall==414,"projects using a removed configuration still resolve the same values");
   CalculationStore.RestoreBuiltIns();Check(CalculationStore.All().Any(c=>c.Name=="Sample 27.6MPa / 414MPa")&&!CalculationStore.All().Any(c=>c.Name=="Project A fc 30"),"restore brings back built-ins only");Check(changed==3,"Changed raised for restore");
   CalculationStore.Delete(new[]{"Built-in FC21 G40/G40"});custom.Name="Built-in FC21 G40/G40";CalculationStore.Save(custom);Check(CalculationStore.All().Count(c=>c.Name=="Built-in FC21 G40/G40")==1,"saving the same name again un-hides it once");
   Check(!Directory.GetFiles(dir,"*.tmp").Any(),"no temporary files left (atomic replace)");
#if ADMIN_APP
   Check(!UserCapturePolicy.Enabled,"Admin edition does not request capture exclusion");
#else
   Check(UserCapturePolicy.Enabled,"User edition requests Windows capture exclusion");
#endif
   Console.WriteLine("PASS: "+checks+" saved splice/development/hook configuration delete/restore checks");}
  finally{try{Directory.Delete(dir,true);}catch{}}}
}
