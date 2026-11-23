using System.Linq;
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
		/* the one item whose quantity comes from the POW workbook (owner, 23 November 2026: "except for progress photograph sa pow xlsm
		   file kukunin"); every other quantity is the app's */
		internal bool FromWorkbook { get { string d = new string((Description ?? "").ToUpperInvariant().Where(char.IsLetter).ToArray()); return d.Contains("PROGRESSPHOTO"); } }
		internal double? WorkbookFallback { get { return FromWorkbook ? WorkbookQuantity : null; } }

		public double? ManualQuantity;
        public string ManualInputText;

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
            if(!string.IsNullOrEmpty(ManualInputText)&&!BackupBill.ReadQuantity(ManualInputText).HasValue)return null;
			double? num = ManualQuantity ?? WorkbookFallback; /* a quantity already in BACKUP is not taken into the app (owner, 23 November 2026: the app's quantities go to the POW BACKUP, not the other way around) */
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
