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
   /* every workspace window shows the project title (23 November 2026) */
   {ProjectInfoStore.Save("Two-storey school building\nPhase 1","Tuguegarao City","Office","Address");var form=new System.Windows.Forms.Form{Text="Staircase Design"};AppBrand.Apply(form);
    Check(form.Text=="Staircase Design"+ProjectCaption.Marker+"Two-storey school building Phase 1","window title shows the project: "+form.Text);
    form.Text=form.Text+"  -  linked to CHB Estimate";Check(form.Text=="Staircase Design  -  linked to CHB Estimate"+ProjectCaption.Marker+"Two-storey school building Phase 1","text added to the title later is kept: "+form.Text);
    ProjectInfoStore.Save("Bridge","Aparri","Office","Address");Check(form.Text=="Staircase Design  -  linked to CHB Estimate"+ProjectCaption.Marker+"Bridge","a new project changes the open window: "+form.Text);
    var dialog=new System.Windows.Forms.Form{Text="Lintel reinforcement schedule",FormBorderStyle=System.Windows.Forms.FormBorderStyle.FixedDialog};AppBrand.Apply(dialog);Check(dialog.Text=="Lintel reinforcement schedule","dialogs keep their title");
    ProjectInfoStore.Save("","","Office","Address");Check(form.Text=="Staircase Design  -  linked to CHB Estimate","no project: no suffix");form.Dispose();dialog.Dispose();}}
  finally{ProjectInfoStore.DirectoryPath=previous;try{Directory.Delete(dir,true);}catch(Exception){}}
  Console.WriteLine("PASS: "+checks+" saved project information checks");}
}
