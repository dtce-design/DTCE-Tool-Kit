using System;
using System.Globalization;
using System.Linq;

namespace BeamScheduleFormatter
{
	internal static class SlabEntryOptions
	{
		public static readonly string[] Types = new string[3] { "Elevated", "Elevated / cantilever edge", "On gravel fill" };

		public static string[] Defaults(SlabScheduleRow slab)
		{
			string[] array = new string[7] { "0", "0", "0", "0", "0", "", "" };
			if (slab == null)
			{
				return array;
			}
			for (int i = 0; i < 2; i++)
			{
				bool flag = slab.Bars[4 + 2 * i] != null;
				bool flag2 = slab.Bars[5 + 2 * i] != null;
				array[1 + 2 * i] = (flag ? (flag2 ? "1" : "2") : "0");
				array[2 + 2 * i] = (flag2 ? (flag ? "1" : "2") : "0");
			}
			return array;
		}

		public static bool IsManual(string[] values, SlabScheduleRow slab)
		{
			string[] standard = Defaults(slab);
			if (!Enumerable.Range(1, 4).Any((int n) => values[n] != standard[n]) && string.IsNullOrWhiteSpace(values[5]))
			{
				return !string.IsNullOrWhiteSpace(values[6]);
			}
			return true;
		}

		public static string[] Prepare(string type, string[] input, bool manual, SlabScheduleRow slab)
		{
			if (!Types.Contains(type) || slab == null || input == null || input.Length != 7)
			{
				throw new ArgumentException("Choose a valid slab and slab type.");
			}
			string[] array = (manual ? ((string[])input.Clone()) : Defaults(slab));
			array[0] = ((type == Types[1]) ? input[0].Trim() : "0");
			if (array[0] == "")
			{
				array[0] = "0";
			}
			int[] array2 = new int[3] { 0, 5, 6 };
			foreach (int num in array2)
			{
				double result;
				if (string.IsNullOrWhiteSpace(array[num]) && num != 0)
				{
					array[num] = "";
				}
				else if (!double.TryParse(array[num], NumberStyles.Float, CultureInfo.InvariantCulture, out result) || double.IsNaN(result) || double.IsInfinity(result) || result < 0.0)
				{
					throw new ArgumentException("Lengths must be nonnegative numbers in metres.");
				}
			}
			int[] array3 = new int[4];
			for (int j = 0; j < 4; j++)
			{
				if (!int.TryParse(array[j + 1], out array3[j]) || array3[j] < 0 || array3[j] > 2)
				{
					throw new ArgumentException("Support counts must be 0, 1 or 2.");
				}
				if (array3[j] > 0 && slab.Bars[4 + j] == null)
				{
					throw new ArgumentException("The selected support type has no reinforcement in the DXF.");
				}
			}
			if (array3[0] + array3[1] > 2 || array3[2] + array3[3] > 2)
			{
				throw new ArgumentException("At most two opposite supports per direction.");
			}
			return array;
		}
	}
}
