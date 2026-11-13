using System.Collections.Generic;

namespace BeamScheduleFormatter
{
	internal class StructureProfile
	{
		public string Name;

		public string Kind;

		public string[] Geometry;

		public readonly List<string[]> Members = new List<string[]>();

		public readonly List<string[]> ExtraRebar = new List<string[]>();

		public LengthConfiguration Configuration;
	}
}
