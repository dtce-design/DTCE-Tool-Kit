namespace BeamScheduleFormatter
{
	internal class PlateDemand
	{
		public string Member;

		public double Length;

		public double Width;

		public double ThicknessMm;

		public int Quantity;

		public double Area
		{
			get
			{
				return Length * Width;
			}
		}
	}
}
