namespace BeamScheduleFormatter
{
	internal sealed class ProjectInformation
	{
		public string Name = "";

		public string Location = "";

		public string Office = "";

		public string OfficeAddress = "";

		public string NameLine
		{
			get
			{
				return "Project: " + ((Name.Length == 0) ? "(not set)" : Name);
			}
		}

		public string LocationLine
		{
			get
			{
				return "Location: " + ((Location.Length == 0) ? "(not set)" : Location);
			}
		}
	}
}
