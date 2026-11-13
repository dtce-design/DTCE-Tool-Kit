using System.Collections.Generic;

namespace BeamScheduleFormatter
{
	internal class ScheduleSource
	{
		public string Label;

		public string Name;

		public string Raw;

		public Analysis Catalog;

		public Dictionary<string, BeamRow> Rows;
	}
}
