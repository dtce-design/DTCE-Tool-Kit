using System;
namespace BeamScheduleFormatter {
static class EarthworkPayRouting {
 internal static string Code(string key,string description,string unit){if(BackupMapping.Unit(unit)!="m3")return null;string value=(description??"").ToUpperInvariant();if((key??"").StartsWith("Gravel|",StringComparison.Ordinal)&&value.Contains("TO BUY"))return "804(7)";if((key??"").StartsWith("Earthworks|",StringComparison.Ordinal)&&value.Contains("EMBANKMENT")){if(value.Contains("FROM EXCAVATION"))return "804(1)a";if(value.Contains("COMMON BORROW"))return "804(2)a";}return null;}
}
}
