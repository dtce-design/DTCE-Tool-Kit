using System.Collections.Generic;

namespace BeamScheduleFormatter
{
	internal class BillQuantity
	{
		public string PayCode, PayDescription;
		public string Section;

		public string Description;

		public string Unit;

		public string Computation;

		public double Quantity;

		public bool Basis;

		public readonly List<BillQuantity> Sources = new List<BillQuantity>();
	}
}
