using System.Collections.Generic;
using System.Linq;

namespace BeamScheduleFormatter
{
	internal class SlabFormatGroup
	{
		public string Mark;

		public List<SlabScheduleRow> Source = new List<SlabScheduleRow>();

		public SlabScheduleRow Schedule
		{
			get
			{
				return Source[0];
			}
		}

		public string Originals
		{
			get
			{
				return string.Join(", ", Source.Select((SlabScheduleRow s) => s.Mark));
			}
		}
	}
}
