namespace BeamScheduleFormatter
{
	internal class SteelSelection
	{
		public int Grade;

		public double Fy;

		public override string ToString()
		{
			return "Grade " + Grade + " (" + Fy + " MPa)";
		}
	}
}
