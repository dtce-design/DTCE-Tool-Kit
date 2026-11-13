namespace BeamScheduleFormatter
{
	internal class TiePartsValue
	{
		public double MainHoop;

		public double AdditionalLeg;

		public int AdditionalCount;

		public double Total
		{
			get
			{
				return MainHoop + (double)AdditionalCount * AdditionalLeg;
			}
		}
	}
}
