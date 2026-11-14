using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using BeamScheduleFormatter;

// Run on Windows. Exercise resize/retheme without altering user inputs or table order.
class ModernUiTests {
 static void Check(bool condition,string why){if(!condition)throw new Exception(why);}
 static T Field<T>(object value,string name){return (T)value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(value);}
 [STAThread] static void Main(){
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);ModernUi.Install();AccessCodes.Granted=true;
  using(var form=new Form{ClientSize=new Size(800,600)}){
   var tools=new FlowLayoutPanel{Dock=DockStyle.Top,Height=90,Padding=new Padding(6)};
   for(int i=0;i<8;i++)tools.Controls.Add(new Button{Text="Full readable action "+i,Width=150,Height=30});
   var grid=new DataGridView{Dock=DockStyle.Fill,AllowUserToAddRows=false};
   grid.Columns.Add("id","Pipe identifier");grid.Columns.Add("value","Measured length in metres");grid.Rows.Add("PPR20-01","12.75");
   form.Controls.Add(grid);form.Controls.Add(tools);PowTheme.Apply(form);form.Show();Application.DoEvents();
   form.ClientSize=new Size(430,600);Application.DoEvents();
   Check(tools.Controls.Cast<Control>().All(c=>c.Bottom<=tools.ClientSize.Height),"Wrapped toolbar clipped at narrow width");
   Check(grid.Rows[0].Cells[0].Value.ToString()=="PPR20-01"&&grid.Rows[0].Cells[1].Value.ToString()=="12.75","Theme altered input values");
   Check(grid.Columns[0].Index==0&&grid.Columns[1].Index==1&&!grid.AllowUserToOrderColumns,"Theme changed table order");
   Check(!grid.AllowUserToResizeRows,"Rows must keep a stable height unless content wraps");
   var dynamicInput=new TextBox{Text="unchanged"};tools.Controls.Add(dynamicInput);
   Check(dynamicInput.BackColor==ModernUi.Input&&dynamicInput.Text=="unchanged","New input was not themed safely");
   PowTheme.Apply(form);Check(grid.Rows.Count==1,"Reapplying theme changed rows");form.Close();
  }
  using(var launcher=new LauncherForm()){
   launcher.Show();Application.DoEvents();var home=Field<Panel>(launcher,"home");
   Check(home.Controls.Count==1,"Start must remain a dashboard with no extra tabs");
   foreach(int width in new[]{900,1100,1500}){launcher.Width=width;Application.DoEvents();Check(home.ClientSize.Width>0,"Dashboard lost its layout");}
   launcher.Close();
  }
  using(var works=new ProgramOfWorksForm()){
   works.Show();Application.DoEvents();var tabs=Field<TabControl>(works,"tabs");
   Check(tabs.Alignment==TabAlignment.Left,"Main sections should use sidebar navigation");
   Check(tabs.TabPages.Cast<TabPage>().Any(p=>p.Text=="Reinforced Concrete Works"),"RC group missing");
   foreach(TabPage page in tabs.TabPages){tabs.SelectedTab=page;Application.DoEvents();Check(page.ClientSize.Width>0&&page.ClientSize.Height>0,"Section layout collapsed: "+page.Text);}
   works.Close();
  }
  using(var wind=new WindDesignForm()){
   wind.Show();Application.DoEvents();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
   var pages=(TabControl)typeof(DesignSheetForm).GetField("Pages",flags).GetValue(wind);
   Check(pages.TabCount==7,"All seven wind input/results/map/profile/report tabs must survive handle creation");
   Check(pages.TabPages.Cast<TabPage>().Any(p=>p.Text=="Wind-load calculations")&&pages.TabPages.Cast<TabPage>().Any(p=>p.Text=="Terrain profiles"),"Wind calculation and terrain tabs missing");
   string old="<DesignSheet type=\"Wind-Design\" version=\"1\"><Input key=\"Latitude\">14.5995</Input><Input key=\"Longitude\">120.9842</Input><Input key=\"Category\">III-V</Input><Input key=\"Height\">10</Input><Input key=\"Kd\">0.85</Input><Input key=\"Reviewed\">False</Input></DesignSheet>";
   wind.Restore(old);Check(wind.CurrentReport().StartsWith("DTCE | NSCP 2015 WIND"),"v1.4.40 site sheets must restore with building fields optional");
   wind.Close();
  }
  Console.WriteLine("PASS: modern layout resize, dynamic controls and input preservation");
 }
}
