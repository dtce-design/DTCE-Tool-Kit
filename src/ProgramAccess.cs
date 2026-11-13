using System;
namespace BeamScheduleFormatter {
static class ProgramAccess {
 public const int BeamFormatter=1,ProgramOfWorks=2,SpliceDevelopmentHooks=4,ProjectInformation=8,PurlinDesign=16,TrussConnections=32,SlabFormatter=64,WindDesign=128;
 public const int All=BeamFormatter|ProgramOfWorks|SpliceDevelopmentHooks|PurlinDesign|TrussConnections|SlabFormatter|WindDesign;
 public static readonly int[] Ids={BeamFormatter,SlabFormatter,ProgramOfWorks,SpliceDevelopmentHooks,PurlinDesign,TrussConnections,WindDesign};
 public static readonly string[] Names={"Beam Schedule Formatter","Slab Schedule Formatter","Program of Works / Estimators","Splice, Development & Hooks","Purlin Design","Truss Connections","Wind Load (NSCP 2015)"};
 public static bool ValidMask(int mask){return mask>0&&(mask&~All)==0;}
 public static bool Allows(int mask,int program){return program==ProjectInformation||Array.IndexOf(Ids,program)>=0&&(mask&program)==program;}
 // Preserve the two formatters in old BA3 grants. New design programs need explicit approval.
 public static int LegacyMask(int mask){return mask|((mask&BeamFormatter)!=0?SlabFormatter:0);}
}
}
