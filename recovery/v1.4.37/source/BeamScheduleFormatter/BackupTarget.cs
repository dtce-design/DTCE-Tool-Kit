namespace BeamScheduleFormatter
{
	internal class BackupTarget
	{
		public int EndRow;

		public double? Quantity;

		public string Code;

		public string Description;

		public string Unit;

		public string SlotId;

		public int Row { get; set; }

		public string Caption
		{
			get
			{
				return Code + " | " + Description + " | " + Unit + " | BC" + Row;
			}
		}
	}
}
