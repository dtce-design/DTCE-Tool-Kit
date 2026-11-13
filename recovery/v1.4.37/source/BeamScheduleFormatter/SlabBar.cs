using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BeamScheduleFormatter
{
	internal class SlabBar
	{
		public int Diameter;

		public double SpacingMm;

		public string Raw;

		public static SlabBar Parse(string text)
		{
			string input = text.Trim();
			if (Regex.IsMatch(input, "^-+$"))
			{
				return null;
			}
			Match match = Regex.Match(input, "^(?:#|%%[Cc]|Ø)\\s*(\\d+)\\s*@\\s*(\\d+(?:\\.\\d+)?)\\s*C/C$", RegexOptions.IgnoreCase);
			if (!match.Success)
			{
				throw new Exception("Unsupported slab reinforcement: " + text);
			}
			return new SlabBar
			{
				Diameter = int.Parse(match.Groups[1].Value),
				SpacingMm = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
				Raw = text
			};
		}
	}
}
