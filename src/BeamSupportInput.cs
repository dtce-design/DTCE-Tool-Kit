using System;
namespace BeamScheduleFormatter {
static class BeamSupportInput {
 internal static string Normalize(string value){value=(value??"").Trim();if(value==""||value=="Elevated"||value=="Elevated slab")return "Elevated";if(value=="On Fill"||value=="On fill"||value=="On gravel fill"||value=="Ground beam")return "On Fill";throw new Exception("Select beam support: Elevated or On Fill.");}
}
}
