using System;

namespace BeamScheduleFormatter
{
	internal static class ProgramAccess
	{
		public const int BeamFormatter = 1;

		public const int ProgramOfWorks = 2;

		public const int SpliceDevelopmentHooks = 4;

		public const int ProjectInformation = 8;

		public const int PurlinDesign = 16;

		public const int TrussConnections = 32;

		public const int SlabFormatter = 64;

		public const int All = 119;

		public static readonly int[] Ids = new int[6] { 1, 64, 2, 4, 16, 32 };

		public static readonly string[] Names = new string[6] { "Beam Schedule Formatter", "Slab Schedule Formatter", "Program of Works / Estimators", "Splice, Development & Hooks", "Purlin Design", "Truss Connections" };

		public static bool ValidMask(int mask)
		{
			if (mask > 0)
			{
				return (mask & -120) == 0;
			}
			return false;
		}

		public static bool Allows(int mask, int program)
		{
			if (program != 8)
			{
				if (Array.IndexOf(Ids, program) >= 0)
				{
					return (mask & program) == program;
				}
				return false;
			}
			return true;
		}

		public static int LegacyMask(int mask)
		{
			return mask | (((mask & 1) != 0) ? 64 : 0);
		}
	}
}
