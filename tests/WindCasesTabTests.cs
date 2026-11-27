using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Reflection;using System.Text;using System.Windows.Forms;using BeamScheduleFormatter;
/* the Wind Load sheet's "Wind load cases" tab and report pages, fed by Load Combinations through the wind-model link (v10.0.34) */
static class WindCasesTabTests {
 static int checks;static void Check(bool ok,string what){checks++;if(!ok){Console.WriteLine("FAIL: "+what);Environment.Exit(1);}}
 [STAThread]static void Main(){Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);AccessCodes.Granted=true;var ci=CultureInfo.InvariantCulture;
  /* a 30 x 24 m floor with a recess, floors at 0, 4 and 8 m, as STAAD copies the tables (no header row) */
  var nodes=new StringBuilder();var beams=new StringBuilder();var id=new Dictionary<string,int>();int n=0,m=0;double[] xs={0,6,12,18,24,30},zs={0,8,16,24},ys={0,4,8};Func<double,double,bool> exists=(x,z)=>!(z>16.5&&x>12.5&&x<17.5);
  foreach(var y in ys)foreach(var x in xs)foreach(var z in zs){if(!exists(x,z))continue;id[x+","+y+","+z]=++n;nodes.Append(n+"\t"+x.ToString("0.0000",ci)+"\t"+y.ToString("0.0000",ci)+"\t"+z.ToString("0.0000",ci)+"\n");}
  Func<string,int> J=k=>id.ContainsKey(k)?id[k]:0;
  foreach(var y in ys){for(int i=0;i+1<xs.Length;i++)foreach(var z in zs){int a=J(xs[i]+","+y+","+z),c=J(xs[i+1]+","+y+","+z);if(a>0&&c>0&&!(z==24&&xs[i]>=12&&xs[i+1]<=18))beams.Append(++m+"\t"+a+"\t"+c+"\t4\t0.0\n");}
   for(int j=0;j+1<zs.Length;j++)foreach(var x in xs){int a=J(x+","+y+","+zs[j]),c=J(x+","+y+","+zs[j+1]);if(a>0&&c>0)beams.Append(++m+"\t"+a+"\t"+c+"\t4\t0.0\n");}}
  foreach(var x in xs)foreach(var z in zs)for(int k=0;k+1<ys.Length;k++){int a=J(x+","+ys[k]+","+z),c=J(x+","+ys[k+1]+","+z);if(a>0&&c>0)beams.Append(++m+"\t"+a+"\t"+c+"\t4\t0.0\n");}
  DesignLoadsLink.Set("wind-model",new Dictionary<string,string>{{"nodes",nodes.ToString()},{"beams",beams.ToString()},{"all","1"},{"exempt","0"},{"titles","+X=LOAD 13 WL+X CASE 1\n+Z=LOAD 15 WL+Z CASE 1"}});
  Dictionary<string,string> titles;bool all,exempt;var model=WindDesignForm.LinkedModel(out titles,out all,out exempt);
  Check(model!=null&&model.Joints.Count==n&&model.Members.Count==m&&titles["+X"]=="LOAD 13 WL+X CASE 1"&&all&&!exempt,"model and titles through the link");
  var types=new List<WindFaceTypes>();foreach(var tr in new[]{"+X","-X","+Z","-Z"}){var t=new WindFaceTypes{Travel=tr,Ww=types.Count*3+1,Lw=types.Count*3+2,Sw=types.Count*3+3,Top=8,Leeward=1,Side=2};t.WwInt.Add(3);t.WwHeig.Add(8);types.Add(t);}
  var form=new WindDesignForm();form.Show();for(int i=0;i<20;i++)Application.DoEvents();
  typeof(WindDesignForm).GetMethod("UpdateWindCases",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(form,new object[]{types});
  var panel=(FlowLayoutPanel)typeof(WindDesignForm).GetField("windCasePanel",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form);
  Check(panel.Controls.Count==5,"design cases figure + one figure per wind case on the tab ("+panel.Controls.Count+")");
  var pages=(TabControl)typeof(DesignSheetForm).GetField("Pages",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form);Check(pages.TabPages.Cast<TabPage>().Any(p=>p.Text=="Wind load cases"),"Wind load cases tab in the Wind Load sheet");
  var doc=new CalculationDocument("t");typeof(WindDesignForm).GetMethod("AddWindCasePages",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(form,new object[]{doc});
  Check(doc.Sheets.Count>=6,"report pages: design cases, four wind cases, torsion ("+doc.Sheets.Count+")");
  using(var bmp=new System.Drawing.Bitmap(595,842))using(var g=System.Drawing.Graphics.FromImage(bmp))foreach(var s in doc.Sheets)s(g);checks++;
  var lc=new LoadCombinationsForm();lc.Show();for(int i=0;i<20;i++)Application.DoEvents();
  var lp=(TabControl)typeof(DesignSheetForm).GetField("Pages",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(lc);Check(!lp.TabPages.Cast<TabPage>().Any(p=>p.Text=="Wind load cases"),"no Wind load cases tab in Load Combinations any more");
  /* automatic torsion exemption (ASCE 7-10 Appendix D, D1.1): one storey with h <= 9.1 m only */
  {var li=(System.Collections.IDictionary)typeof(DesignSheetForm).GetField("Inputs",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(lc);var ex=typeof(LoadCombinationsForm).GetMethod("TorsionExempt",BindingFlags.Instance|BindingFlags.NonPublic);
   ((Control)li["StaadInput"]).Text=nodes.ToString().Replace("\n","\r\n");((Control)li["StaadBeams"]).Text=beams.ToString().Replace("\n","\r\n");Check(!(bool)ex.Invoke(lc,null),"three levels, 8 m: torsional cases included");
   /* one model for the sheet: redundancy / torsion read the Primary loads tab's tables */
   var rt=typeof(LoadCombinationsForm).GetMethod("RhoTexts",BindingFlags.Instance|BindingFlags.NonPublic);var args=new object[]{null,null};rt.Invoke(lc,args);var mt=(string)args[0];
   Check(RedundancyFactor.HasModel(mt)&&RedundancyFactor.ReadModel(mt).Joints.Count==n&&RedundancyFactor.ReadModel(mt).Members.Count==m,"rho / Ax use the model of the Primary loads tab");
   var one=new StringBuilder("STAAD SPACE\nUNIT METER KN\nJOINT COORDINATES\n1 0 0 0; 2 12 0 0; 3 12 0 8; 4 0 0 8; 5 0 4 0; 6 12 4 0; 7 12 4 8; 8 0 4 8;\nMEMBER INCIDENCES\n1 1 5; 2 2 6; 3 3 7; 4 4 8; 5 5 6; 6 6 7; 7 7 8; 8 8 5;\nSUPPORTS\n");
   ((Control)li["StaadBeams"]).Text="";((Control)li["StaadInput"]).Text=one.ToString().Replace("\n","\r\n");Check((bool)ex.Invoke(lc,null),"one storey, 4 m: exempt (Cases 1 and 3 only)");}
  foreach(Form f in Application.OpenForms.Cast<Form>().ToList())f.Close();form.Dispose();lc.Dispose();
  Console.WriteLine("WindCasesTabTests passed: "+checks+" checks");Environment.Exit(0);}
}
