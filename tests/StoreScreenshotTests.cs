using System;using System.IO;using System.Linq;using System.Reflection;using System.Drawing;using System.Drawing.Imaging;using System.Windows.Forms;using BeamScheduleFormatter;
class StoreScreenshotTests {
 static T Field<T>(object target,string name){return (T)target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);}
 static string folder;
 static void Capture(Form form,string name){
  // Render the actual controls in a fixed canvas, independent of the CI desktop's window limit.
  form.Hide();var controls=form.Controls.Cast<Control>().ToArray();
  using(var host=new Panel{Size=new Size(1500,960),BackColor=form.BackColor}){
   try{foreach(var control in controls)host.Controls.Add(control);host.CreateControl();host.PerformLayout();Application.DoEvents();using(var image=new Bitmap(host.Width,host.Height)){host.DrawToBitmap(image,new Rectangle(0,0,host.Width,host.Height));image.Save(Path.Combine(folder,name+".png"),ImageFormat.Png);}}
   finally{foreach(var control in controls)form.Controls.Add(control);}
  }
 }

 static void Select(ProgramOfWorksForm form,string group,string page,string name){var root=Field<TabControl>(form,"tabs");root.SelectedTab=root.TabPages.Cast<TabPage>().Single(p=>p.Text==group);if(page!=null){var inner=root.SelectedTab.Controls.OfType<TabControl>().Single();inner.SelectedTab=inner.TabPages.Cast<TabPage>().Single(p=>p.Text==page);}Capture(form,name);}
 [STAThread]static void Main(string[] args){
  folder=args[0];Directory.CreateDirectory(folder);AccessCodes.Granted=true;ProjectInfoStore.DirectoryPath=Path.Combine(folder,"sample-project-info");ProjectInfoStore.Save("Sample Building Improvement","Sample Project Location","Sample Implementing Office","Sample Office Address");
  /* the workspaces open only after complete project information (19 November 2026) */ProjectInfoStore.Save("Test project","Test location","Test office","Test address","2026-11-19");  using(var main=new LauncherForm()){main.Width=1500;main.Height=960;main.Show();Capture(main,"Main-Dashboard");main.Hide();}
  using(var form=new ProgramOfWorksForm()){form.OpenProjectXml(File.ReadAllText("test-results/screenshot-earth.xml"));Field<DataGridView>(form,"mouldingEntries").Rows.Add("M1","Additional facade moulding","Concrete 150 x 50 mm","6.5","2");Field<DataGridView>(form,"mouldingEntries").Rows.Add("M2","Additional cornice","Concrete 100 x 50 mm","4","3");foreach(DataGridViewRow row in Field<DataGridView>(form,"wood").Rows)if(!row.IsNewRow)row.Cells[24].Value="Concrete 150 x 50 mm";form.Recalculate();form.Show();Select(form,"Bill of Quantities",null,"Bill-of-Quantities");Select(form,"Finishing Works","Moulding","Moulding");Select(form,"Reinforced Concrete Works","Start","RC-Settings");Select(form,"Reinforced Concrete Works","Miscellaneous Structures","Canopy-Ledge");Select(form,"Earthworks","Start","Earthworks-Gravel");Select(form,"Doors & Windows","Door & Window Schedule","Doors-Windows");form.Hide();}
  using(var form=new ProgramOfWorksForm()){form.OpenProjectXml(File.ReadAllText("test-results/screenshot-steel.xml"));form.Show();Select(form,"Structural Steel Works","Steel Plates","Steel-Plates");Select(form,"Structural Steel Works","Metal Fascia Frame","Metal-Fascia");form.Hide();}
  Console.WriteLine("PASS: actual edition UI screenshots captured to "+folder);
 }
}
