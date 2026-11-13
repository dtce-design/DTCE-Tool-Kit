using System;

namespace BeamScheduleFormatter
{
	internal class RoofAccessoryQuantity
	{
		public int Pieces;

		public double BuyLength;

		public double BuyArea;

		public double NetLength;

		public static RoofAccessoryQuantity Calculate(double run, int quantity, double developedWidth, double stock, double lap)
		{
			RoofMath.Positive(run, stock);
			if (double.IsNaN(developedWidth) || double.IsInfinity(developedWidth) || developedWidth < 0.0)
			{
				throw new Exception("Developed width cannot be negative.");
			}
			if (quantity < 1 || quantity > 100000 || double.IsNaN(lap) || double.IsInfinity(lap) || lap < 0.0 || lap >= stock)
			{
				throw new Exception("Check quantity and lap; lap must be less than piece length.");
			}
			int num = checked(((run <= stock) ? 1 : RoofMath.Ceil((run - lap) / (stock - lap))) * quantity);
			return new RoofAccessoryQuantity
			{
				Pieces = num,
				BuyLength = (double)num * stock,
				BuyArea = (double)num * stock * developedWidth,
				NetLength = run * (double)quantity
			};
		}
	}
}
