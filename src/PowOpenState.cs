using System;using System.Linq;using System.Xml;
namespace BeamScheduleFormatter {
static class PowOpenState {
 // Opening another project must protect actual inputs, not cached Excel catalogue data.
 internal static string Inputs(string xml){var doc=new XmlDocument{XmlResolver=null};doc.LoadXml(xml);var root=doc.DocumentElement;var bill=root["BackupBill"];if(bill!=null){foreach(XmlElement item in bill.SelectNodes("Item").Cast<XmlElement>().ToArray()){if(!item.HasAttribute("manual")&&!item.HasAttribute("manualInput")){bill.RemoveChild(item);continue;}foreach(XmlAttribute attribute in item.Attributes.Cast<XmlAttribute>().ToArray())if(!new[]{"slot","signature","manual","manualInput"}.Contains(attribute.Name))item.RemoveAttribute(attribute.Name);}}var routes=root["BackupBillRoutes"];if(routes!=null)root.RemoveChild(routes);var manifest=root["SourceFileManifest"];if(manifest!=null)foreach(XmlElement file in manifest.SelectNodes("File").Cast<XmlElement>().Where(e=>e.GetAttribute("kind")=="Excel workbook (imported original)").ToArray())manifest.RemoveChild(file);foreach(XmlElement element in doc.SelectNodes("//*"))if(!element.HasChildNodes)element.IsEmpty=true;return doc.OuterXml;}
 internal static bool HasInputChanges(string current,string saved){return Inputs(current)!=Inputs(saved);}
}
partial class ProgramOfWorksForm {bool openingPow;}
}
