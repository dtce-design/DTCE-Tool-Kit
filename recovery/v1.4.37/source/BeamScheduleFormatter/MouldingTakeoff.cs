using System;
using System.Text.RegularExpressions;

namespace BeamScheduleFormatter
{
	internal class MouldingTakeoff
	{
		public string Id;

		public string Description;

		public string Profile;

		public double Length;

		public int Quantity;

		public double Total
		{
			get
			{
				if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(Description) || string.IsNullOrWhiteSpace(Profile))
				{
					throw new Exception("Enter ID, description and moulding profile / size.");
				}
				SteelWeight.Positive(Length);
				SteelWeight.Count(Quantity);
				return Length * (double)Quantity;
			}
		}

		public static string Key(string profile)
		{
			return Regex.Replace(Regex.Replace((profile ?? "").Trim().ToUpperInvariant().Replace('×', 'X'), "\\s*X\\s*", "X"), "\\s+", "").Replace("MM", "");
		}
	}
}
