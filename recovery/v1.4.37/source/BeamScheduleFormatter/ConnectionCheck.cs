namespace BeamScheduleFormatter
{
	internal class ConnectionCheck
	{
		public string Name;

		public string Note;

		public double Capacity;

		public double Demand;

		public bool Pass
		{
			get
			{
				return Capacity >= Demand;
			}
		}
	}
}
