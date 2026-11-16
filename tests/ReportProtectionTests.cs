using System;using System.IO;using System.Linq;using System.Collections.Generic;using BeamScheduleFormatter;
class ReportProtectionTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static void Main(string[] args){
  var register=typeof(System.Text.Encoding).GetMethod("RegisterProvider");bool has1252=true;try{System.Text.Encoding.GetEncoding(1252);}catch{has1252=false;}if(register!=null&&!has1252){var type=System.Reflection.Assembly.Load("System.Text.Encoding.CodePages").GetType("System.Text.CodePagesEncodingProvider");register.Invoke(null,new[]{type.GetProperty("Instance").GetValue(null,null)});}
  string folder=Path.GetFullPath(args[0]);Directory.CreateDirectory(folder);string tool=Path.GetFullPath(args[1]);
  var profile=SteelCatalog.Profiles.First(p=>p.Kind=="Purlin"&&p.Name.StartsWith("C")&&p.H>=150&&p.T>=1.5);
  var input=new PurlinDesignInput{Name="SAMPLE PURLIN — NOT A PROJECT DESIGN",Profile=profile,Properties=PurlinProperties.Calculate(profile),Span=3,Spacing=.6,WindPressure=1,WindSuction=-1};var result=PurlinDesigner.Calculate(input);
  string purlin=DetailedDesignReports.Purlin(input,result);var connection=new ConnectionInput{Demand=10,Area=864,LL1=100,LL2=120,LT=40};var truss=TrussConnectionDesigner.Calculate(connection);string trussText=DetailedDesignReports.Truss(connection,truss,"L75x75x6mm","Illustrative test inputs");
  foreach(var entry in new[]{new KeyValuePair<string,string>("Purlin",purlin),new KeyValuePair<string,string>("Truss",trussText)}){
   var report=ProfessionalReport.FromText(entry.Value,entry.Key+" — calculation report","ILLUSTRATIVE SAMPLE — not a project design","Sample only");report.Subtitle="Detailed input, formula substitution and listed-check results";
   Check(report.Sections.All(s=>s.Records.All(b=>b.Text==null||!System.Text.RegularExpressions.Regex.IsMatch(b.Text,@"^[-=]{3,}$"))),"ASCII rulers removed");
   var plan=EngineeringQuantityPdf.Plan(report);Check(plan.Pages.Count==plan.DrawingPages.Count&&plan.Pages.Count>0,"Structured preview/export pages match");
   Check(plan.DrawingPages.SelectMany(p=>p).Where(c=>c.Kind=="text").All(c=>c.Size>=7&&c.Y>=20&&c.Y<=820),"Text stays within page and minimum font");
   Check(plan.DrawingPages.All(p=>p.Any(c=>c.Kind=="text"&&c.Text.StartsWith("Page "))),"Every planned page has footer");
   string raw=Path.Combine(folder,entry.Key+" input.pdf"),secured=Path.Combine(folder,entry.Key+" protected.pdf");EngineeringQuantityPdf.Write(raw,report);PdfProtection.ProtectWithTool(raw,secured,tool);Check(File.Exists(secured)&&new FileInfo(secured).Length>1000,"AES-256 export created");
   File.WriteAllText(Path.Combine(folder,entry.Key+".html"),report.Html());File.WriteAllText(Path.Combine(folder,entry.Key+".txt"),entry.Value);
  }
  var tiles=ReportWatermarkPattern.Tiles().ToArray();Check(tiles.Length==24&&tiles.All(t=>t[2]==180),"Large triangular watermark lattice");Check(ReportWatermarkPattern.Opacity>.085&&ReportWatermarkPattern.Opacity<.2,"Increased readable opacity");
  string existing=Path.Combine(folder,"preserved.pdf");File.WriteAllText(existing,"keep existing");bool failed=false;try{PdfProtection.ProtectWithTool(Path.Combine(folder,"Purlin input.pdf"),existing,Path.Combine(folder,"missing tool"));}catch{failed=true;}Check(failed&&File.ReadAllText(existing)=="keep existing","Failed security step preserves destination");Check(!Directory.GetFiles(folder,"*.secured").Any(),"Temporary secured files removed");
  Console.WriteLine(checks+" report layout, watermark and real AES-256 export checks passed.");
 }
}
