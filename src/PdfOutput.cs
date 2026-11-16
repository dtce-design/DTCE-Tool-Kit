using System;using System.IO;using System.Linq;using System.Text;using System.Collections.Generic;using System.Globalization;
namespace BeamScheduleFormatter {
static class EstimatePdf {
 static readonly Encoding Encoding1252=Encoding.GetEncoding(1252);
 static string Escape(string s){return s.Replace("\\","\\\\").Replace("(","\\(").Replace(")","\\)");}
 public static string Heading(string report){if(report.StartsWith("DTCE | NSCP 2015 WIND"))return "WIND LOAD | NSCP 2015";if(report.StartsWith("DTCE | DETAILED PURLIN"))return "PURLIN DESIGN | DETAILED CALCULATIONS";if(report.StartsWith("DTCE | DETAILED TRUSS"))return "TRUSS CONNECTIONS | DETAILED CALCULATIONS";if(report.StartsWith("PROGRAM OF WORKS"))return "PROGRAM OF WORKS | QUANTITY BACKUP";if(report.StartsWith("SLAB ESTIMATE"))return "SLAB ESTIMATE | PURCHASE BACKUP";if(report.StartsWith("REINFORCING STEEL BARS"))return "BEAM ESTIMATE | BACKUP QUANTITIES & CALCULATIONS";return report.Contains("CONFIGURATION REVIEW")||report.Contains("SPLICE, DEVELOPMENT & HOOKS")?"SPLICE, DEVELOPMENT & HOOKS | CALCULATION SHEET":"BEAM ESTIMATE | BACKUP QUANTITIES & CALCULATIONS";}
 public static List<List<string>> Pages(string report){int maxRows=ProjectReportHeader.PreviewRows(ProjectInfoStore.Read());var pages=new List<List<string>>();var page=new List<string>();pages.Add(page);foreach(string raw in report.Replace("\r","").Split('\n')){if(raw=="\f"){if(page.Count>0){page=new List<string>();pages.Add(page);}continue;}string line=raw.Replace("\t","    ");do{int take=Math.Min(100,line.Length);if(line.Length>100){int split=line.LastIndexOf(' ',99,50);if(split>45)take=split;}if(page.Count>=maxRows){page=new List<string>();pages.Add(page);}page.Add(line.Substring(0,take));line=line.Substring(take).TrimStart();}while(line.Length>0);}return pages;}
 static byte[] Stream(string dictionary,byte[] data){return Encoding1252.GetBytes("<< "+dictionary+" /Length "+data.Length+" >>\nstream\n").Concat(data).Concat(Encoding1252.GetBytes("\nendstream")).ToArray();}
 public static void Write(string path,string report){using(var document=CalculationDocument.FromText(report))document.WritePdf(path);}
}
}
