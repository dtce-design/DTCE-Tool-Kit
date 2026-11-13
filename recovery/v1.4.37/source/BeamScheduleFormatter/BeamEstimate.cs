using System.Collections.Generic;
using System.Linq;

namespace BeamScheduleFormatter
{
	internal class BeamEstimate
	{
		public List<EstimateLine> Lines = new List<EstimateLine>();

		public List<string> Notes = new List<string>();

		public int Ties;

		public double MainKg
		{
			get
			{
				return Lines.Where((EstimateLine x) => x.Part != "Stirrups").Sum((EstimateLine x) => x.Kg);
			}
		}

		public double LongKg
		{
			get
			{
				return Lines.Where((EstimateLine x) => x.Part != "Stirrups" && x.Part != "Side-face").Sum((EstimateLine x) => x.Kg);
			}
		}

		public double SfrKg
		{
			get
			{
				return Lines.Where((EstimateLine x) => x.Part == "Side-face").Sum((EstimateLine x) => x.Kg);
			}
		}

		public double TieKg
		{
			get
			{
				return Lines.Where((EstimateLine x) => x.Part == "Stirrups").Sum((EstimateLine x) => x.Kg);
			}
		}

		public double Total
		{
			get
			{
				return MainKg + TieKg;
			}
		}
	}
}
