using System;using System.Collections.Generic;
namespace BeamScheduleFormatter {
// Quantity/cutting allocation in 6m stock portions. No splice or lap additions.
static class AngleSegments {
 internal static List<SteelCutDemand> Expand(SteelCutDemand source){SteelWeight.Positive(source.Length,source.KgM);SteelWeight.Count(source.Quantity);source.Parts=1;if(source.Length<=6+1e-9)return new List<SteelCutDemand>{source};double count=Math.Ceiling((source.Length-1e-9)/6);if(count*source.Quantity>100000)throw new Exception("Angle cutting plan supports up to 100000 cut pieces.");var result=new List<SteelCutDemand>();double remaining=source.Length;int segment=1;while(remaining>6+1e-9){result.Add(new SteelCutDemand{Member=source.Member+" / stock portion "+segment++,Specification=source.Specification,Length=6,KgM=source.KgM,Quantity=source.Quantity,Source=source});remaining-=6;}result.Add(new SteelCutDemand{Member=source.Member+" / stock portion "+segment,Specification=source.Specification,Length=remaining,KgM=source.KgM,Quantity=source.Quantity,Source=source});source.Parts=result.Count;return result;}
}
}
