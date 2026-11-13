namespace BeamScheduleFormatter
{
	internal class PlateFormat
	{
		public string Name;

		public double Width;

		public double Length;

		public double Area
		{
			get
			{
				return Width * Length;
			}
		}
	}
}
