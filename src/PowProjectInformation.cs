using System;using System.Xml;
namespace BeamScheduleFormatter {
static class PowProjectInformation {
 internal static void Write(XmlElement root,ProjectInformation project){var e=root.OwnerDocument.CreateElement("ProjectInformation");e.SetAttribute("name",project.Name);e.SetAttribute("location",project.Location);e.SetAttribute("office",project.Office);e.SetAttribute("officeAddress",project.OfficeAddress);e.SetAttribute("prepared",project.DatePrepared??"");if(!string.IsNullOrEmpty(project.FiscalYear))e.SetAttribute("fy",project.FiscalYear);if(!string.IsNullOrEmpty(project.Abc))e.SetAttribute("abc",project.Abc);root.AppendChild(e);}
 /* FY / ABC of a Bill of Quantities file; a file saved before they existed takes them from the saved project list (same title and
    location), so opening an older file does not erase the FY / ABC typed in the Project Information (owner, 23 November 2026: "bat
    hindi din nagsasave yung year at ABC") */
 static ProjectInformation Saved(ProjectInformation p){try{foreach(var x in ProjectLibrary.List())if(string.Equals((x.Name??"").Trim(),(p.Name??"").Trim(),StringComparison.OrdinalIgnoreCase)&&string.Equals((x.Location??"").Trim(),(p.Location??"").Trim(),StringComparison.OrdinalIgnoreCase))return x;}catch(Exception){}return null;}
 internal static string FiscalYearFor(ProjectInformation p){if(p.FiscalYear!=null)return p.FiscalYear;var x=Saved(p);return x==null?"":x.FiscalYear??"";}
 internal static string AbcFor(ProjectInformation p){if(p.Abc!=null)return p.Abc;var x=Saved(p);return x==null?"":x.Abc??"";}
 internal static ProjectInformation Read(XmlElement root){var nodes=root.SelectNodes("ProjectInformation");if(nodes.Count==0)return null;if(nodes.Count!=1)throw new Exception("Duplicate saved project information.");var e=(XmlElement)nodes[0];var p=new ProjectInformation{Name=e.GetAttribute("name"),Location=e.GetAttribute("location"),Office=e.GetAttribute("office"),OfficeAddress=e.GetAttribute("officeAddress"),DatePrepared=e.GetAttribute("prepared"),/* null: a file saved before FY / ABC existed (v7.1.14) */FiscalYear=e.HasAttribute("fy")?e.GetAttribute("fy"):null,Abc=e.HasAttribute("abc")?e.GetAttribute("abc"):null};if(p.Name.Length>ProjectInfoStore.MaxTitleLength||p.Location.Length>ProjectInfoStore.MaxLocationLength||p.Office.Length>200||p.OfficeAddress.Length>200)throw new Exception("Saved project information exceeds supported lengths.");return p;}
}
}
