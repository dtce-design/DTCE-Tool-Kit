using System;
namespace BeamScheduleFormatter {
static class ProgramAccess {
 public const int BeamFormatter=1,ProgramOfWorks=2,SpliceDevelopmentHooks=4,ProjectInformation=8,PurlinDesign=16,TrussConnections=32,SlabFormatter=64;
 public const int All=BeamFormatter|ProgramOfWorks|SpliceDevelopmentHooks|PurlinDesign|TrussConnections|SlabFormatter;
 public static readonly int[] Ids={BeamFormatter,SlabFormatter,ProgramOfWorks,SpliceDevelopmentHooks,PurlinDesign,TrussConnections};
 public static readonly string[] Names={"Beam Schedule Formatter","Slab Schedule Formatter","Program of Works / Estimators","Splice, Development & Hooks","Purlin Design","Truss Connections"};
 public static bool ValidMask(int mask){return mask>0&&(mask&~All)==0;}
 public static bool Allows(int mask,int program){return program==ProjectInformation||Array.IndexOf(Ids,program)>=0&&(mask&program)==program;}
 // Preserve the two formatters in old BA3 grants. New design programs need explicit approval.
 public static int LegacyMask(int mask){return mask|((mask&BeamFormatter)!=0?SlabFormatter:0);}
}
}
