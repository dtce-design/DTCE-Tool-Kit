using System;

namespace BeamScheduleFormatter
{
	internal class RoofSheetQuantity
	{
		public int Columns;

		public int Rows;

		public int Pieces;

		public double CoveredArea;

		public double BuyArea;

		public double SheetLength;

		public double Insulation;

		public static RoofSheetQuantity Calculate(double slope, double width, int quantity, double sheetWidth, double effective, double stock, double lap, bool insulated)
		{
			RoofMath.Positive(slope, width, sheetWidth, effective);
			if (quantity < 1 || quantity > 100000 || effective > sheetWidth)
			{
				throw new Exception("Check quantity and widths: effective cover cannot exceed full sheet width.");
			}
			if (stock == 0.0)
			{
				stock = slope;
			}
			RoofMath.Positive(stock);
			if (double.IsNaN(lap) || double.IsInfinity(lap) || lap < 0.0 || lap >= stock)
			{
				throw new Exception("End lap must be nonnegative and less than sheet length.");
			}
			int num = RoofMath.Ceil(width / effective);
			int num2 = ((slope <= stock) ? 1 : RoofMath.Ceil((slope - lap) / (stock - lap)));
			int num3 = checked(num * num2 * quantity);
			return new RoofSheetQuantity
			{
				Columns = num,
				Rows = num2,
				Pieces = num3,
				SheetLength = stock,
				CoveredArea = slope * width * (double)quantity,
				BuyArea = (double)num3 * stock * sheetWidth,
				Insulation = (insulated ? (slope * width * (double)quantity) : 0.0)
			};
		}
	}
}
