using System;
using System.Collections.Generic;
using System.Linq;

namespace BeamScheduleFormatter
{
	internal class EarthworkBalance
	{
		public readonly List<EarthworkSourceUse> Sources = new List<EarthworkSourceUse>();

		public double Excavation;

		public double Fill;

		public double LooseFromExcavation;

		public double LooseCommonBorrow;

		public double LooseSurplus;

		public double UnsuitableLoose;

		public double AvailableCompacted;

		public double BorrowSwell;

		public double BorrowShrink;

		public static EarthworkBalance Calculate(IEnumerable<EarthworkMaterial> data, double borrowSwell = 20.0, double borrowShrink = 10.0)
		{
			List<EarthworkMaterial> list = data.ToList();
			foreach (EarthworkMaterial item in list)
			{
				item.Validate();
			}
			EarthworkMaterial earthworkMaterial = new EarthworkMaterial();
			earthworkMaterial.Swell = borrowSwell;
			earthworkMaterial.Shrink = borrowShrink;
			earthworkMaterial.Validate();
			EarthworkBalance earthworkBalance = new EarthworkBalance
			{
				BorrowSwell = borrowSwell,
				BorrowShrink = borrowShrink,
				Excavation = list.Sum((EarthworkMaterial x) => x.Bank),
				Fill = list.Sum((EarthworkMaterial x) => x.Fill),
				AvailableCompacted = list.Sum((EarthworkMaterial x) => x.CompactedAvailable)
			};
			double num = earthworkBalance.Fill;
			foreach (EarthworkMaterial item2 in list)
			{
				double num2 = Math.Min(num, item2.CompactedAvailable);
				earthworkBalance.LooseFromExcavation += num2 / (1.0 - item2.Shrink / 100.0) * (1.0 + item2.Swell / 100.0);
				num = Math.Max(0.0, num - num2);
				earthworkBalance.LooseSurplus += Math.Max(0.0, item2.LooseAvailable - num2 / (1.0 - item2.Shrink / 100.0) * (1.0 + item2.Swell / 100.0));
				earthworkBalance.UnsuitableLoose += item2.Bank * (1.0 - item2.Reusable / 100.0) * (1.0 + item2.Swell / 100.0);
				double num3 = num2 / (1.0 - item2.Shrink / 100.0) * (1.0 + item2.Swell / 100.0);
				earthworkBalance.Sources.Add(new EarthworkSourceUse
				{
					Material = item2,
					LooseUsed = num3,
					LooseSurplus = Math.Max(0.0, item2.LooseAvailable - num3),
					UnsuitableLoose = item2.Bank * (1.0 - item2.Reusable / 100.0) * (1.0 + item2.Swell / 100.0)
				});
			}
			earthworkBalance.LooseCommonBorrow = num / (1.0 - borrowShrink / 100.0) * (1.0 + borrowSwell / 100.0);
			return earthworkBalance;
		}
	}
}
