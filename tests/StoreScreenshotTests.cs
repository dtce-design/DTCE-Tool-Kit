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
  folder=args[0];Directory.CreateDirectory(folder);AccessCodes.Granted=true;
  using(var main=new LauncherForm()){main.Width=1500;main.Height=960;main.Show();Capture(main,"Main-Dashboard");main.Hide();}
  using(var form=new ProgramOfWorksForm()){form.OpenProjectXml(File.ReadAllText("test-results/screenshot-earth.xml"));form.Show();Select(form,"Bill of Quantities",null,"Bill-of-Quantities");Select(form,"Reinforced Concrete Works","Start","RC-Settings");Select(form,"Reinforced Concrete Works","Canopy / Ledge Estimate","Canopy-Ledge");Select(form,"Earthworks & Gravel","Start","Earthworks-Gravel");Select(form,"Doors & Windows","Wooden Doors & Windows","Doors-Windows");form.Hide();}
  using(var form=new ProgramOfWorksForm()){form.OpenProjectXml(File.ReadAllText("test-results/screenshot-steel.xml"));form.Show();Select(form,"Structural Steel Works","Steel Plates","Steel-Plates");Select(form,"Structural Steel Works","Metal Fascia Frame","Metal-Fascia");form.Hide();}
  Console.WriteLine("PASS: actual edition UI screenshots captured to "+folder);
 }
}
