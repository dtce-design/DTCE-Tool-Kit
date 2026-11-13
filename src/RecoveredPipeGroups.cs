using System.Linq;
using System.Text.RegularExpressions;

namespace BeamScheduleFormatter
{
	internal static class PipeGroups
	{
		public static readonly string[] Names = new string[5] { "PPR Waterline", "PVC - S600", "PVC - S1000", "Waterline - PVC", "Other pipes" };

		public static string Group(string specification)
		{
			string text = (specification ?? "").ToUpperInvariant();
			if (text.Contains("PPR"))
			{
				return Names[0];
			}
			string text2 = Regex.Replace(text, "\\s", "");
			if (text2.Contains("SERIES1000") || text2.Contains("S1000"))
			{
				return Names[2];
			}
			if (text2.Contains("SERIES600") || text2.Contains("S600"))
			{
				return Names[1];
			}
			if (text.Contains("POTABLE") || text.Contains("WATERLINE") || text.Contains("WATER LINE"))
			{
				return Names[3];
			}
			return Names[4];
		}

		public static string[] Choices(string group)
		{
			return PlumbingSpecs.Pipes.Where((string s) => Group(s) == group).ToArray();
		}
	}
}
