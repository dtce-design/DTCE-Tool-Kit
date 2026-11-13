using System;

namespace BeamScheduleFormatter
{
	internal class StockChoice
	{
		public double Length;

		public double Cut;

		public double Lap;

		public long Required;

		public long PiecesPerStock;

		public long Bars;

		public long Segments = 1L;

		public double SpliceMetres
		{
			get
			{
				return (double)(Required * (Segments - 1)) * Lap;
			}
		}

		public double SegmentCut
		{
			get
			{
				return (Cut + (double)(Segments - 1) * Lap) / (double)Segments;
			}
		}

		public double Purchased
		{
			get
			{
				return (double)Bars * Length;
			}
		}

		public double Used
		{
			get
			{
				return (double)Required * Cut + SpliceMetres;
			}
		}

		public double Waste
		{
			get
			{
				return Math.Max(0.0, Purchased - Used);
			}
		}
	}
}
