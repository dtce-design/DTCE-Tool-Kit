using System;using System.Xml;
namespace BeamScheduleFormatter {
static class PowProjectInformation {
 internal static void Write(XmlElement root,ProjectInformation project){var e=root.OwnerDocument.CreateElement("ProjectInformation");e.SetAttribute("name",project.Name);e.SetAttribute("location",project.Location);e.SetAttribute("office",project.Office);e.SetAttribute("officeAddress",project.OfficeAddress);e.SetAttribute("prepared",project.DatePrepared??"");if(!string.IsNullOrEmpty(project.FiscalYear))e.SetAttribute("fy",project.FiscalYear);if(!string.IsNullOrEmpty(project.Abc))e.SetAttribute("abc",project.Abc);root.AppendChild(e);}
 internal static ProjectInformation Read(XmlElement root){var nodes=root.SelectNodes("ProjectInformation");if(nodes.Count==0)return null;if(nodes.Count!=1)throw new Exception("Duplicate saved project information.");var e=(XmlElement)nodes[0];var p=new ProjectInformation{Name=e.GetAttribute("name"),Location=e.GetAttribute("location"),Office=e.GetAttribute("office"),OfficeAddress=e.GetAttribute("officeAddress"),DatePrepared=e.GetAttribute("prepared"),FiscalYear=e.GetAttribute("fy"),Abc=e.GetAttribute("abc")};if(p.Name.Length>ProjectInfoStore.MaxTitleLength||p.Location.Length>ProjectInfoStore.MaxLocationLength||p.Office.Length>200||p.OfficeAddress.Length>200)throw new Exception("Saved project information exceeds supported lengths.");return p;}
}
}
