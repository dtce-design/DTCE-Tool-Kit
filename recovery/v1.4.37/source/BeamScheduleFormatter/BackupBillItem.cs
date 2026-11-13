namespace BeamScheduleFormatter
{
	internal class BackupBillItem
	{
		public string Slot;

		public string Signature;

		public string Code;

		public string Description;

		public string Unit;

		public int Row;

		public double? WorkbookQuantity;

		public double? ManualQuantity;

		public BackupTarget Target
		{
			get
			{
				return new BackupTarget
				{
					SlotId = Slot,
					Code = Code,
					Description = Description,
					Unit = Unit,
					Row = Row
				};
			}
		}

		public BackupDemand ManualDemand()
		{
			double? num = ManualQuantity ?? ((BackupMapping.Unit(Unit) == "ls") ? WorkbookQuantity : ((double?)null));
			if (!num.HasValue)
			{
				return null;
			}
			return new BackupDemand
			{
				Key = "BOQ manual|" + Slot,
				Description = Description,
				Unit = Unit,
				Quantity = num.Value,
				TargetSlotId = Slot,
				TargetIdentity = Signature,
				ManualOverride = true,
				ManualPriority = (ManualQuantity.HasValue ? 2 : 0)
			};
		}
	}
}
