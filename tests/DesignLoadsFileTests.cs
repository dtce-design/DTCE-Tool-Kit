using System.Collections.Generic;using System;using System.Linq;using System.Reflection;using System.Windows.Forms;using BeamScheduleFormatter;
class DesignLoadsFileTests{static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}static void Pump(){for(int i=0;i<20;i++){Application.DoEvents();System.Threading.Thread.Sleep(30);}}
 [STAThread]static void Main(){Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);AccessCodes.Granted=true;var lc=new LoadCombinationsForm();lc.Show();var dl=new DeadLoadsForm();dl.Show();Pump();
  var inputs=(System.Collections.IDictionary)typeof(DesignSheetForm).GetField("Inputs",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(lc);((ComboBox)inputs["Structure"]).SelectedIndex=1;
  var din=(System.Collections.IDictionary)typeof(DesignSheetForm).GetField("Inputs",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(dl);((Control)din["ElevMass"]).Text="5000";Pump();
  string xml=DesignLoadsFile.Bundle();Check(DesignLoadsFile.Count==2&&xml.Contains("<DesignLoads")&&xml.Contains("type=\"Dead-Loads\"")&&xml.Contains("type=\"Load-Combinations\""),"one Design Loads file with the open sheets");
  ((Control)din["ElevMass"]).Text="1";((ComboBox)inputs["Structure"]).SelectedIndex=0;Pump();
  DesignLoadsFile.Open(xml,lc);Pump();Check(((Control)din["ElevMass"]).Text=="5000"&&((ComboBox)inputs["Structure"]).Text.StartsWith("B"),"both sheets restored from the one file");
  /* a sheet opened later gets its part */lc.Dispose();var xml2=xml;DesignLoadsFile.Open(xml2,dl);var lc2=new LoadCombinationsForm();lc2.Show();Pump();var in2=(System.Collections.IDictionary)typeof(DesignSheetForm).GetField("Inputs",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(lc2);Check(((ComboBox)in2["Structure"]).Text.StartsWith("B"),"a sheet opened later gets its part");
  var s=new StaticLateralResult{Ca=0.36,I=1.25,Omega=2.5,Rho=1.0,R=8.5,Zone=4};EarthquakeLink.Save(s);var lc3=new LoadCombinationsForm();lc3.Show();Pump();var in3=(System.Collections.IDictionary)typeof(DesignSheetForm).GetField("Inputs",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(lc3);
  Check(((Control)in3["Ca"]).Text=="0.36"&&((Control)in3["I"]).Text=="1.25"&&((Control)in3["Omega0"]).Text=="2.5"&&!((Control)in3["Ca"]).Enabled&&!((Control)in3["Ca"]).Parent.Visible,"Ca, I, Omega0 from the Earthquake Load sheet, locked and hidden while linked");
  /* links between the sheets (v10.0.16) */
  Check(LoadCombinationsForm.WeightsFromLevels("3.5@1200, 7@1200, 10.5@900")=="3.5 1200\r\n7 1200\r\n10.5 900","Earthquake levels become the irregularity weights");
  int n;string one;LoadCombinationsForm.LiveCasesFor("LL1,LL2",out n,out one);Check(n==2&&one=="LL2","LL1 and LL2 spaces: two live cases, f1 1.0 for LL2");
  LoadCombinationsForm.LiveCasesFor("LL1",out n,out one);Check(n==1&&one=="","only LL1 spaces: one live case LL, f1 0.5");LoadCombinationsForm.LiveCasesFor("LL2",out n,out one);Check(n==1&&one=="LL","only LL2 spaces: one live case LL, f1 1.0");
  DesignLoadsLink.Set("live-loads",new Dictionary<string,string>{{"cases","LL1,LL2"}});DesignLoadsLink.Set("earthquake-levels",new Dictionary<string,string>{{"levels","3.5@1200, 7@900"}});
  var lc4=new LoadCombinationsForm();lc4.Show();Pump();var in4=(System.Collections.IDictionary)typeof(DesignSheetForm).GetField("Inputs",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(lc4);
  Check(((Control)in4["LiveCases"]).Text=="2"&&((Control)in4["F1One"]).Text=="LL2"&&!((Control)in4["LiveCases"]).Parent.Visible,"live cases and f1 from the Live Loads sheet, hidden while linked");
  Check(((Control)in4["RegWeights"]).Text=="3.5 1200\r\n7 900","storey weights from the Earthquake Load sheet");
  var lcl=DesignLoadsLink.Get("load-combinations");Check(lcl!=null&&lcl["structure"].Length==1,"the structure class goes to the Earthquake Load sheet");
  DesignLoadsLink.Set("live-loads",new Dictionary<string,string>{{"cases",""}});
  /* one site location: Wind and Earthquake share Latitude / Longitude (v10.0.17) */
  DesignLoadsLink.Set("site",new Dictionary<string,string>{{"lat","16.4023"},{"lon","120.5960"}});var wf=new WindDesignForm();wf.Show();Pump();var win=(System.Collections.IDictionary)typeof(DesignSheetForm).GetField("Inputs",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(wf);
  Check(((Control)win["Latitude"]).Text=="16.4023"&&((Control)win["Longitude"]).Text=="120.5960","a new Wind sheet starts at the shared site");
  var ef=new RsaCurveForm();ef.Show();Pump();var ein=(System.Collections.IDictionary)typeof(DesignSheetForm).GetField("Inputs",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(ef);
  Check(((Control)ein["Latitude"]).Text=="16.4023","a new Earthquake sheet starts at the shared site");
  ((Control)win["Latitude"]).Text="14.5995";((Control)win["Longitude"]).Text="120.9842";Pump();Check(((Control)ein["Latitude"]).Text=="14.5995"&&((Control)ein["Longitude"]).Text=="120.9842","coordinates typed in Wind appear in Earthquake");
  ((Control)ein["Latitude"]).Text="10.3157";Pump();Check(((Control)win["Latitude"]).Text=="10.3157","and the other way round");wf.Dispose();ef.Dispose();
  /* Ctrl+S saves to the chosen file again without asking (v10.0.18) */
  {var lc5=new LoadCombinationsForm();lc5.Show();Pump();string tmp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"dl-ctrl-s.design.xml");if(System.IO.File.Exists(tmp))System.IO.File.Delete(tmp);
   typeof(DesignSheetForm).GetField("designLoadsPath",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,tmp);var m=new Message();
   bool handled=(bool)typeof(DesignSheetForm).GetMethod("ProcessCmdKey",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(lc5,new object[]{m,Keys.Control|Keys.S});
   Check(handled&&System.IO.File.Exists(tmp)&&System.IO.File.ReadAllText(tmp).Contains("<DesignLoads"),"Ctrl+S saves the Design Loads file again");lc5.Dispose();}
  /* occupancy category and structural system shared by Wind and Earthquake (v10.0.25) */
  {var w=new WindDesignForm();w.Show();Pump();var e=new RsaCurveForm();e.Show();Pump();var wi=(System.Collections.IDictionary)typeof(DesignSheetForm).GetField("Inputs",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(w);var ei=(System.Collections.IDictionary)typeof(DesignSheetForm).GetField("Inputs",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(e);
   var eqOcc=(ComboBox)ei["Importance"];var wOcc=(ComboBox)wi["Category"];eqOcc.SelectedIndex=0;Pump();Check(wOcc.Text=="I","Essential facilities in Earthquake sets Wind occupancy I");
   wOcc.SelectedItem="III-V";Pump();Check(eqOcc.SelectedIndex==3,"Wind III-V sets Earthquake to Standard occupancy");
   eqOcc.SelectedIndex=2;Pump();Check(wOcc.Text=="III-V"&&eqOcc.SelectedIndex==2,"Special occupancy stays and Wind shows III-V");
   var eqSys=(ComboBox)ei["System"];var wSys=(ComboBox)wi["BuildingSystem"];eqSys.SelectedIndex=0;Pump();Check(wSys.Text=="MRF Steel","steel moment frame shared");
   wSys.SelectedIndex=2;Pump();Check(eqSys.SelectedIndex==2,"other system shared back");
   w.Dispose();var w2=new WindDesignForm();w2.Show();Pump();var w2i=(System.Collections.IDictionary)typeof(DesignSheetForm).GetField("Inputs",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(w2);Check(((ComboBox)w2i["Category"]).Text=="III-V"&&((ComboBox)w2i["BuildingSystem"]).Text=="Other system","a Wind sheet opened later starts with the shared values");w2.Dispose();e.Dispose();}
  Console.WriteLine("DesignLoadsFileTests passed: "+checks+" checks");Environment.Exit(0);}}
