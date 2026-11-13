using System.Collections.Generic;

namespace BeamScheduleFormatter
{
	internal class Result
	{
		public List<BeamRow> Rows;

		public Dxf Drawing;

		public int SourceCount;

		public int BeamCount;

		public List<string> Changes = new List<string>();
	}
}
