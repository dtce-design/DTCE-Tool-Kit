using System;using System.Collections.Generic;using System.Linq;using System.Reflection;using System.Windows.Forms;using BeamScheduleFormatter;
/* Owner (23 November 2026): Truss Connections as Weld design / Bolt design with their own sub tabs and report; automatic block shear;
   back-to-back spacing = gusset thickness; "sa user pwede nila maaccess yung app pero di nila magagamit" (view-only apps). */
class TrussConnectionLayoutTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static T Get<T>(object o,string name,Type t=null){return (T)(t??o.GetType()).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);}
 [STAThread]static void Main(){
  /* engine: block shear of a bolted angle, member and gusset paths, against hand values */
  var b=new ConnectionInput{Family="Angle",Connection="Bolts",Area=864,Thickness=6,Offset=21,Fy=248,Fu=400,GussetThickness=10,GussetWidth=150,GussetFy=248,GussetFu=400,Demand=50,Bolts=3,Spacing=50,EdgeDistance=35,BoltDiameter=16,HoleDiameter=18,ShearPlanes=1,Fnv=372,ConnectedWidth=75};
  var r=TrussConnectionDesigner.Calculate(b);var m=r.BlockPaths.First(p=>p.Name.StartsWith("Member"));var gp=r.BlockPaths.First(p=>p.Name.StartsWith("Gusset"));
  /* g = 40 (0.55 x 75 = 41.25 -> 40): Agv = (35+100) 6 = 810; Anv = (135 - 2.5 x 18) 6 = 540; Ant = (75-40-9) 6 = 156 */
  Check(r.Gauge==40&&Math.Abs(m.Agv-810)<1e-6&&Math.Abs(m.Anv-540)<1e-6&&Math.Abs(m.Ant-156)<1e-6,"member block-shear areas");
  Check(Math.Abs(m.Nominal-Math.Min(.6*400*540+400*156,.6*248*810+400*156)/1000)<1e-9,"member block-shear Rn");
  /* gusset: edge across = 75 (half the width): Ant = (75-9) 10 = 660 */
  Check(Math.Abs(gp.Agv-1350)<1e-6&&Math.Abs(gp.Anv-900)<1e-6&&Math.Abs(gp.Ant-660)<1e-6,"gusset block-shear areas");
  Check(r.Checks.Count(c=>c.Name.Contains("block shear"))==2,"both block-shear paths are limit states");
  var w=new ConnectionInput{Family="Double angle",Connection="Weld",Area=864,Thickness=6,Offset=21,GussetThickness=10,Demand=80,Weld=5,LL1=100,LL2=80,LT=0,ConnectedWidth=75};
  var rw=TrussConnectionDesigner.Calculate(w);var wp=rw.BlockPaths.Single();Check(wp.Agv==1800&&wp.Anv==1800&&wp.Ant==750,"gusset block shear around the welds (Anv = Agv)");
  var h=new ConnectionInput{Family="HSS rectangle",Connection="Weld",Area=900,Thickness=4,Offset=30,Demand=50,Weld=4,LL1=100,LL2=100,ConnectedWidth=100};Check(TrussConnectionDesigner.Calculate(h).BlockPaths.Count==0,"no block shear for a slotted HSS");
  /* the window: two designs, each with its sub tabs; the shared pages move; Connection follows the design */
  AccessCodes.Granted=true;var f=new TrussConnectionForm();f.Show();Application.DoEvents();
  var pages=Get<TabControl>(f,"Pages",typeof(DesignSheetForm));var inputs=Get<Dictionary<string,Control>>(f,"Inputs",typeof(DesignSheetForm));
  Check(string.Join("|",pages.TabPages.Cast<TabPage>().Select(p=>p.Text))=="Weld design|Bolt design","main tabs");
  Func<int,string> subs=k=>string.Join("|",pages.TabPages[k].Controls.OfType<TabControl>().Single().TabPages.Cast<TabPage>().Select(p=>p.Text));
  Check(subs(0)=="Member and demand|Weld detail|Block shear|STAAD forces|Report","weld sub tabs: "+subs(0));
  /* a tab change made with the tab strip focused, as by the user (Calculate / Open changes return to the open design) */pages.Focus();Application.DoEvents();pages.SelectedIndex=1;Application.DoEvents();
  Check(subs(1)=="Member and demand|Bolt detail|Block shear|STAAD forces|Report"&&inputs["Connection"].Text=="Bolts","bolt sub tabs follow: "+subs(1));
  inputs["Family"].Text="Double angle";Application.DoEvents();Check(inputs["ShearPlanes"].Text=="2","double angle bolts in double shear");
  inputs["Tension"].Text="60";var doc=(CalculationDocument)typeof(TrussConnectionForm).GetMethod("BuildDocument",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(f,null);Check(doc.Sheets.Count>=4,"illustrated report");
  pages.Focus();pages.SelectedIndex=0;Application.DoEvents();Check(inputs["Connection"].Text=="Weld","weld follows");
  f.Close();
  /* view only (User edition): opens without access, inputs and commands locked, tabs still browsable */
  #if !ADMIN_APP
  AccessCodes.Granted=false;var v=(TrussConnectionForm)ViewOnly.Create(()=>new TrussConnectionForm());v.Show();Application.DoEvents();
  var vin=Get<Dictionary<string,Control>>(v,"Inputs",typeof(DesignSheetForm));var vp=Get<TabControl>(v,"Pages",typeof(DesignSheetForm));
  Check(((TextBox)vin["Tension"]).ReadOnly&&!vin["Family"].Enabled&&!vin["UseStaad"].Enabled,"inputs locked");
  Func<Control,IEnumerable<Control>> all=null;all=c=>c.Controls.Cast<Control>().SelectMany(x=>new[]{x}.Concat(all(x)));
  Check(all(v).OfType<Button>().Where(x=>x.Text=="Calculate"||x.Text.StartsWith("Export")||x.Text.StartsWith("Save")).All(x=>!x.Enabled),"commands locked");
  Check(all(v).OfType<Button>().Any(x=>x.Text=="Enter access code"&&x.Enabled),"access-code banner");
  vp.SelectedIndex=1;Application.DoEvents();Check(vp.SelectedIndex==1,"tabs browsable");
  #endif
  Console.WriteLine("PASS: "+checks+" truss connection layout checks");Environment.Exit(0);}
}
