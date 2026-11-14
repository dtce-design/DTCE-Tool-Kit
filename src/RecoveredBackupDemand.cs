using System;
using System.Collections.Generic;
using System.Linq;

namespace BeamScheduleFormatter
{
	internal class BackupDemand
	{
		public string PayCode, PayDescription;
		public string Key;

		public string Description;

		public string Unit;

		public string TargetSlotId;

		public string TargetIdentity;

		public double Quantity;

		public bool ManualOverride;

		public int ManualPriority = -1;

		public readonly List<BillQuantity> Sources = new List<BillQuantity>();

		public static BackupDemand From(BillQuantity q)
		{
			BackupDemand backupDemand = new BackupDemand();
			backupDemand.Key = q.Section + "|" + q.Description + "|" + BackupMapping.Unit(q.Unit);
			backupDemand.Description = q.Description;
            backupDemand.PayCode=q.PayCode;backupDemand.PayDescription=q.PayDescription;
			backupDemand.Unit = q.Unit;
			backupDemand.Quantity = q.Quantity;
			backupDemand.Sources.AddRange(q.Sources);
			return backupDemand;
		}

		public List<BackupLine> Lines()
		{
			if (double.IsNaN(Quantity) || double.IsInfinity(Quantity) || Quantity < 0.0)
			{
				throw new Exception("Invalid BACKUP quantity: " + Description);
			}
			List<BackupLine> list = new List<BackupLine>();
			List<BillQuantity> list2 = Sources.Where((BillQuantity s) => !s.Basis && BackupMapping.Unit(s.Unit) == BackupMapping.Unit(Unit)).ToList();
			bool flag = list2.Count > 0 && Math.Abs(list2.Sum((BillQuantity s) => s.Quantity) - Quantity) <= Math.Max(1E-07, Math.Abs(Quantity) * 1E-09);
			foreach (BillQuantity source in Sources)
			{
				if (double.IsNaN(source.Quantity) || double.IsInfinity(source.Quantity) || source.Quantity < 0.0)
				{
					throw new Exception("Invalid source quantity: " + source.Description);
				}
				list.Add(new BackupLine
				{
					Code = ((source.Section != null && source.Section.Contains("(")) ? source.Section : ""),
					Description = ((source.Section != null && source.Section.Contains("(")) ? (source.Description ?? "").Replace(source.Section + " - ", "") : source.Description),
					Computation = ((!string.IsNullOrWhiteSpace(source.Computation)) ? source.Computation : ((source.Basis || !flag) ? "Basis only; not added to the parent total" : "Validated source quantity")),
					Quantity = source.Quantity,
					Contributes = (flag && !source.Basis && BackupMapping.Unit(source.Unit) == BackupMapping.Unit(Unit)),
					Unit = source.Unit
				});
			}
			if (!flag)
			{
				list.Add(new BackupLine
				{
					Description = Description,
					Computation = ((Sources.Count == 0) ? "Validated Tool Kit quantity" : (Sources.Any((BillQuantity s) => BackupMapping.Unit(s.Unit) != BackupMapping.Unit(Unit)) ? "Parent pay-item quantity; component materials are breakdown only" : "Final purchase quantity after Tool Kit rounding / allocation; basis rows are not added again")),
					Quantity = Quantity,
					Contributes = true,
					Unit = Unit
				});
			}
			return list;
		}
	}
}
