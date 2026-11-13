using System;
using System.Collections.Generic;
using System.Linq;

namespace BeamScheduleFormatter
{
	internal class PlateSheet
	{
		public PlateFormat Format;

		public double ThicknessMm;

		public readonly List<PlateRectangle> Free = new List<PlateRectangle>();

		public readonly List<PlatePlacement> Cuts = new List<PlatePlacement>();

		public double NetArea
		{
			get
			{
				return Cuts.Sum((PlatePlacement c) => c.Width * c.Length);
			}
		}

		public double BuyKg
		{
			get
			{
				return SteelWeight.Plate(Format.Area, ThicknessMm, 1);
			}
		}

		public double WasteKg
		{
			get
			{
				return Math.Max(0.0, SteelWeight.Plate(Format.Area, ThicknessMm, 1) - NetArea * ThicknessMm / 1000.0 * 7850.0);
			}
		}
	}
}
