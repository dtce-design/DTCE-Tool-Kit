using System;using System.IO;using System.Linq;using BeamScheduleFormatter;
class ProjectLibraryTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static void Main(){string previous=ProjectInfoStore.DirectoryPath,dir=Path.Combine(Path.GetTempPath(),"DTCE-library-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);ProjectInfoStore.DirectoryPath=dir;
  try{Check(ProjectLibrary.List().Count==0,"empty library");
   ProjectInfoStore.Save("Two-storey school building","Aparri, Cagayan","Cagayan 1st DEO","Aparri, Cagayan","2026-11-20");ProjectInfoStore.Save("Flood control\nphase 2","Lallo, Cagayan","Cagayan 1st DEO","Aparri, Cagayan","2026-11-19");
   var list=ProjectLibrary.List();Check(list.Count==2&&list[0].Name=="Flood control\nphase 2"&&list[1].Location=="Aparri, Cagayan","every saved project is kept, newest first");Check(list[1].Office=="Cagayan 1st DEO"&&list[1].DatePrepared=="2026-11-20","all fields are kept");
   ProjectInfoStore.Save("two-storey school building","APARRI, CAGAYAN","Cagayan 1st DEO","Aparri, Cagayan","2026-11-21");list=ProjectLibrary.List();Check(list.Count==2&&list[0].DatePrepared=="2026-11-21","saving the same title and location again refreshes it and moves it to the top");
   ProjectInfoStore.ClearProject();Check(ProjectLibrary.List().Count==2,"clearing the current project keeps the list");
   ProjectLibrary.Remove(list[1]);Check(ProjectLibrary.List().Count==1&&ProjectLibrary.List()[0].Name.StartsWith("two-storey"),"remove one project");
   /* settings shared by the Store app and the .exe: the newest copy of each file is taken once, an existing file is never replaced */
   {string a1=Path.Combine(dir,"exe"),a2=Path.Combine(dir,"store"),to=Path.Combine(dir,"shared");Directory.CreateDirectory(a1);Directory.CreateDirectory(a2);Directory.CreateDirectory(to);
    File.WriteAllText(Path.Combine(a1,"ProjectLibrary.xml"),"old");File.SetLastWriteTimeUtc(Path.Combine(a1,"ProjectLibrary.xml"),new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc));File.WriteAllText(Path.Combine(a2,"ProjectLibrary.xml"),"new");File.WriteAllText(Path.Combine(a1,"pow-timing.log"),"log");
    SharedData.CopyMissing(new[]{a1,a2},to,new[]{"ProjectLibrary.xml","ProjectInformation.xml"});Check(File.ReadAllText(Path.Combine(to,"ProjectLibrary.xml"))=="new"&&!File.Exists(Path.Combine(to,"pow-timing.log")),"the newest saved projects are copied, other files are not");
    File.WriteAllText(Path.Combine(a2,"ProjectLibrary.xml"),"newer");SharedData.CopyMissing(new[]{a1,a2},to,new[]{"ProjectLibrary.xml"});Check(File.ReadAllText(Path.Combine(to,"ProjectLibrary.xml"))=="new","a file already in the shared folder is kept");}
   /* every workspace window shows the project title (23 November 2026) */
   {ProjectInfoStore.Save("Two-storey school building\nPhase 1","Tuguegarao City","Office","Address");var form=new System.Windows.Forms.Form{Text="Staircase Design"};AppBrand.Apply(form);
    Check(form.Text=="Staircase Design"+ProjectCaption.Marker+"Two-storey school building Phase 1","window title shows the project: "+form.Text);
    form.Text=form.Text+"  -  linked to CHB Estimate";Check(form.Text=="Staircase Design  -  linked to CHB Estimate"+ProjectCaption.Marker+"Two-storey school building Phase 1","text added to the title later is kept: "+form.Text);
    ProjectInfoStore.Save("Bridge","Aparri","Office","Address");Check(form.Text=="Staircase Design  -  linked to CHB Estimate"+ProjectCaption.Marker+"Bridge","a new project changes the open window: "+form.Text);
    var dialog=new System.Windows.Forms.Form{Text="Lintel reinforcement schedule",FormBorderStyle=System.Windows.Forms.FormBorderStyle.FixedDialog};AppBrand.Apply(dialog);Check(dialog.Text=="Lintel reinforcement schedule","dialogs keep their title");
    ProjectInfoStore.Save("","","Office","Address");Check(form.Text=="Staircase Design  -  linked to CHB Estimate","no project: no suffix");form.Dispose();dialog.Dispose();}
   /* fiscal year and ABC in the Project Information (owner, 23 November 2026) */
   {ProjectInfoStore.Save("FY test","Aparri","Office","Address","2026-11-12","2027","4950000");var pi=ProjectInfoStore.Read();Check(pi.FiscalYearValue==2027&&pi.AbcValue==4950000m,"FY and ABC saved");
    ProjectInfoStore.Save("FY test","Aparri","Office","Address","2026-11-12");pi=ProjectInfoStore.Read();Check(pi.FiscalYear=="2027"&&pi.Abc=="4950000","saving without FY / ABC keeps them");
    Check(ProjectLibrary.List().First(x=>x.Name=="FY test").FiscalYear=="2027","FY kept in the saved project list");
    Check(ProgramOfWorksForm.FormulaStart(pi.Prepared.Value,pi.AbcValue,pi.FiscalYearValue)==new DateTime(2027,5,3),"start date from the Project Information FY / ABC");
    ProjectInfoStore.ClearProject();pi=ProjectInfoStore.Read();Check(pi.FiscalYear==""&&pi.Abc=="","a new project starts without FY / ABC");}}
  finally{ProjectInfoStore.DirectoryPath=previous;try{Directory.Delete(dir,true);}catch(Exception){}}
  Console.WriteLine("PASS: "+checks+" saved project information checks");}
}
