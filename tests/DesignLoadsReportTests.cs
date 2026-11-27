using System;using System.Collections.Generic;using System.Linq;using System.Windows.Forms;using BeamScheduleFormatter;
/* one calculation report for the Design Loads (v10.0.34): contents, then each sheet's report with its own page titles */
static class DesignLoadsReportTests {
 static int checks;static void Check(bool ok,string what){checks++;if(!ok){Console.WriteLine("FAIL: "+what);Environment.Exit(1);}}
 [STAThread]static void Main(){Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);AccessCodes.Granted=true;
  var dl=new DeadLoadsForm();var ll=new LiveLoadsForm();var lc=new LoadCombinationsForm();foreach(var f in new Form[]{dl,ll,lc})f.Show();for(int i=0;i<30;i++)Application.DoEvents();
  var sheets=new List<KeyValuePair<string,DesignSheetForm>>{new KeyValuePair<string,DesignSheetForm>("Dead Loads (NSCP 2015)",dl),new KeyValuePair<string,DesignSheetForm>("Live Loads (NSCP 2015)",ll),
   new KeyValuePair<string,DesignSheetForm>("Wind Load (NSCP 2015)",null),new KeyValuePair<string,DesignSheetForm>("Load Combinations (NSCP 2015)",lc)};
  var problems=new List<string>();var t=DesignLoadsReport.Build(sheets,problems);while(!t.IsCompleted)Application.DoEvents();var doc=t.Result;
  int d=0,l=0,c=0;foreach(var s in sheets){if(s.Value==null)continue;using(var one=s.Value.ReportForBundle().Result){if(s.Value==dl)d=one.Sheets.Count;else if(s.Value==ll)l=one.Sheets.Count;else c=one.Sheets.Count;}}
  Check(doc.Sheets.Count==1+d+l+c,"contents + every sheet's pages ("+doc.Sheets.Count+" = 1 + "+d+" + "+l+" + "+c+")");
  Check(doc.PageTitles.Count==doc.Sheets.Count&&doc.PageTitles[1]!=doc.PageTitles[doc.PageTitles.Count-1],"each page keeps its sheet's title");
  Check(problems.Count==1&&problems[0].StartsWith("Wind Load"),"a sheet that is not open is listed as not included");
  using(var bmp=new System.Drawing.Bitmap(800,1100))using(var g=System.Drawing.Graphics.FromImage(bmp))for(int p=0;p<doc.Sheets.Count;p++)doc.Draw(g,p);checks++;
  string pdf=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"design-loads-test.pdf");doc.WritePdf(pdf);Check(new System.IO.FileInfo(pdf).Length>1000,"PDF written");
  doc.Dispose();foreach(Form f in Application.OpenForms.Cast<Form>().ToList())f.Close();
  Console.WriteLine("DesignLoadsReportTests passed: "+checks+" checks");Environment.Exit(0);}
}
