using System;
namespace BeamScheduleFormatter {
static class ProgramAccess {
 public const int BeamFormatter=1,ProgramOfWorks=2,SpliceDevelopmentHooks=4,ProjectInformation=8,PurlinDesign=16,TrussConnections=32,SlabFormatter=64,WindDesign=128,BeamDeflection=256,
  /* workspaces that had no checkbox of their own (owner, 20 November 2026, "kulang?") */TrussSupport=512,StrapFoundation=1024,EarthquakeRsa=2048,BarBendingSchedule=4096,/* owner, 22 November 2026: staircase design sheet */StaircaseDesign=8192;
 public const int All=BeamFormatter|ProgramOfWorks|SpliceDevelopmentHooks|PurlinDesign|TrussConnections|SlabFormatter|WindDesign|BeamDeflection|TrussSupport|StrapFoundation|EarthquakeRsa|BarBendingSchedule|StaircaseDesign;
 public static readonly int[] Ids={BeamFormatter,SlabFormatter,ProgramOfWorks,BarBendingSchedule,SpliceDevelopmentHooks,PurlinDesign,TrussConnections,TrussSupport,StrapFoundation,WindDesign,EarthquakeRsa,BeamDeflection,StaircaseDesign};
 public static readonly string[] Names={"Beam Schedule Formatter","Slab Schedule Formatter","Bill of Quantities / Estimators","Bar Bending Schedule","Splice, Development & Hooks","Purlin Design","Truss Connections","Truss Support on RC","Strap Foundation","Wind Load (NSCP 2015)","Earthquake Load & RSA Curve Builder","Beam Deflection Check","Staircase Design"};
 public static int ForSheet(string type){return type=="Wind-Design"?WindDesign:type=="RSA-Curve"?EarthquakeRsa:type=="Purlin-Design"?PurlinDesign:type=="Beam-Deflection"?BeamDeflection:type=="Truss-Support"?TrussSupport:type=="Strap-Footing"?StrapFoundation:type=="Staircase-Design"?StaircaseDesign:TrussConnections;}
 public static bool ValidMask(int mask){return mask>0&&(mask&~All)==0;}
 public static bool Allows(int mask,int program){return program==ProjectInformation||Array.IndexOf(Ids,program)>=0&&(mask&program)==program;}
 // Preserve the two formatters in old BA3 grants. New design programs need explicit approval.
 public static int LegacyMask(int mask){return mask|((mask&BeamFormatter)!=0?SlabFormatter:0);}
}
}
