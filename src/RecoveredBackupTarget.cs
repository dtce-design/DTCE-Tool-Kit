namespace BeamScheduleFormatter
{
	internal class BackupTarget
	{
		public int EndRow;

		public double? Quantity;

		public string Code;

		public string Description;

		public string Unit;

		/* the unit as read from the workbook when the catalogue unit replaced it (19 November 2026) */
		public string ReadUnit;

		public string SlotId;

		public int Row { get; set; }

		public string Caption
		{
			get
			{
				return Code + " | " + PayItemText.Display(Description) + " | " + Unit + " | BC" + Row;
			}
		}
	}
}
