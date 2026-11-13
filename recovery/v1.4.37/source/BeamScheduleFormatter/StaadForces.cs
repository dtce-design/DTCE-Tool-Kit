using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BeamScheduleFormatter
{
	internal static class StaadForces
	{
		private static string Header(string value)
		{
			return Regex.Replace((value ?? "").Trim().ToLowerInvariant(), "[\\s_]+", "");
		}

		private static List<string> Split(string line, char separator)
		{
			List<string> list = new List<string>();
			StringBuilder stringBuilder = new StringBuilder();
			bool flag = false;
			for (int i = 0; i < line.Length; i++)
			{
				char c = line[i];
				if (c == '"')
				{
					if (flag && i + 1 < line.Length && line[i + 1] == '"')
					{
						stringBuilder.Append('"');
						i++;
					}
					else
					{
						flag = !flag;
					}
				}
				else if (c == separator && !flag)
				{
					list.Add(stringBuilder.ToString().Trim());
					stringBuilder.Length = 0;
				}
				else
				{
					stringBuilder.Append(c);
				}
			}
			if (flag)
			{
				throw new Exception("Unclosed quote in STAAD table.");
			}
			list.Add(stringBuilder.ToString().Trim());
			return list;
		}

		public static List<StaadForceRow> Parse(string text, string forceUnit, string momentUnit)
		{
			double num;
			switch (forceUnit)
			{
			default:
				num = double.NaN;
				break;
			case "kip":
				num = 4.4482216152605;
				break;
			case "N":
				num = 0.001;
				break;
			case "kN":
				num = 1.0;
				break;
			}
			double num2 = num;
			double num3;
			switch (momentUnit)
			{
			default:
				num3 = double.NaN;
				break;
			case "kip-ft":
				num3 = 1.3558179483314;
				break;
			case "N-mm":
				num3 = 1E-06;
				break;
			case "kN-m":
				num3 = 1.0;
				break;
			}
			double num4 = num3;
			if (double.IsNaN(num2) || double.IsNaN(num4))
			{
				throw new Exception("Select explicit STAAD force and moment units.");
			}
			string[] array = (text ?? "").Replace("\r", "").Split('\n');
			string[] headers = null;
			char separator = '\t';
			int num5 = -1;
			for (int i = 0; i < array.Length; i++)
			{
				char c = (array[i].Contains("\t") ? '\t' : ',');
				List<string> list = Split(array[i], c);
				if (list.Any((string f) => Regex.IsMatch(Header(f), "^fx(?:$|\\(|\\[|kn|n$|kip)")) && list.Any((string f) => Header(f) == "beam" || Header(f) == "member" || Header(f) == "memberid"))
				{
					headers = list.ToArray();
					separator = c;
					num5 = i;
					break;
				}
			}
			if (headers == null)
			{
				throw new Exception("Paste/export a STAAD Beam End Forces or Envelope table INCLUDING Beam, L/C, Node and Fx/Fy/Fz/Mx/My/Mz headers.");
			}
			Func<string, int> func = (string name) => Array.FindIndex(headers, delegate(string h)
			{
				string text8 = Header(h);
				if (!(name == "member"))
				{
					if (!(name == "case"))
					{
						if (!(name == "node"))
						{
							return Regex.IsMatch(text8, "^" + name + "(?:$|\\(|\\[|kn|n(?:mm)?$|kip)");
						}
						if (!(text8 == "node"))
						{
							return text8 == "nodeid";
						}
						return true;
					}
					if (!(text8 == "l/c") && !(text8 == "loadcase"))
					{
						return text8 == "loadcaseid";
					}
					return true;
				}
				return text8 == "beam" || text8 == "member" || text8 == "memberid";
			});
			int num6 = func("member");
			int num7 = func("case");
			int num8 = func("node");
			string[] array2 = new string[6] { "fx", "fy", "fz", "mx", "my", "mz" };
			int[] array3 = array2.Select(func).ToArray();
			if (num7 < 0 || num8 < 0 || array3.Any((int num12) => num12 < 0))
			{
				throw new Exception("Missing STAAD force/moment, load-case or node columns.");
			}
			string text2 = Header(headers[array3[0]]);
			string text3 = Header(headers[array3[3]]);
			if ((text2.Contains("kn") && forceUnit != "kN") || (text2.Contains("kip") && forceUnit != "kip") || (text3.Contains("kn") && momentUnit != "kN-m") || (text3.Contains("kip") && momentUnit != "kip-ft"))
			{
				throw new Exception("Selected units conflict with STAAD column headers.");
			}
			if ((text2 == "fxn" && forceUnit != "N") || (text3 == "mxn-mm" && momentUnit != "N-mm") || (text3 == "mxnmm" && momentUnit != "N-mm"))
			{
				throw new Exception("Selected units conflict with STAAD column headers.");
			}
			int num9 = new int[3] { num6, num7, num8 }.Concat(array3).Max();
			List<StaadForceRow> list2 = new List<StaadForceRow>();
			string text4 = "";
			string text5 = "";
			for (int num10 = num5 + 1; num10 < array.Length; num10++)
			{
				if (string.IsNullOrWhiteSpace(array[num10]))
				{
					continue;
				}
				List<string> list3 = Split(array[num10], separator);
				if (list3.Count <= num9)
				{
					throw new Exception("Incomplete STAAD row " + (num10 + 1) + ". Copy the whole table; do not remove blank columns.");
				}
				if (Header(list3[num6]) == Header(headers[num6]))
				{
					continue;
				}
				string text6 = ((list3[num6] == "") ? text4 : list3[num6]);
				string text7 = ((list3[num7] == "") ? text5 : list3[num7]);
				int result;
				int result2;
				if (!int.TryParse(text6, out result) || result <= 0 || !int.TryParse(list3[num8], out result2) || result2 <= 0 || text7 == "")
				{
					throw new Exception("Invalid member/node/load case at STAAD row " + (num10 + 1));
				}
				double[] array4 = new double[6];
				for (int num11 = 0; num11 < 6; num11++)
				{
					if (list3[array3[num11]].Contains(",") && !Regex.IsMatch(list3[array3[num11]], "^[+-]?\\d{1,3}(,\\d{3})+(\\.\\d+)?([eE][+-]?\\d+)?$"))
					{
						throw new Exception("Ambiguous numeric separator at STAAD row " + (num10 + 1) + ". Use decimal points.");
					}
					if (!double.TryParse(list3[array3[num11]], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out array4[num11]) || double.IsNaN(array4[num11]) || double.IsInfinity(array4[num11]))
					{
						throw new Exception("Invalid " + array2[num11] + " at STAAD row " + (num10 + 1));
					}
				}
				list2.Add(new StaadForceRow
				{
					Member = result,
					Node = result2,
					LoadCase = text7,
					Fx = array4[0] * num2,
					Fy = array4[1] * num2,
					Fz = array4[2] * num2,
					Mx = array4[3] * num4,
					My = array4[4] * num4,
					Mz = array4[5] * num4
				});
				text4 = text6;
				text5 = text7;
			}
			if (list2.Count == 0)
			{
				throw new Exception("No STAAD force rows found.");
			}
			return list2;
		}

		public static List<StaadForceRow> Select(List<StaadForceRow> rows, string members)
		{
			HashSet<int> ids = new HashSet<int>();
			string[] array = members.Split(new char[3] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < array.Length; i++)
			{
				int result;
				if (!int.TryParse(array[i], out result) || result <= 0)
				{
					throw new Exception("Member IDs must be positive integers separated by commas.");
				}
				ids.Add(result);
			}
			if (ids.Count == 0)
			{
				throw new Exception("Enter the member IDs to design; unrelated members must not be pooled automatically.");
			}
			if (ids.Any((int id) => !rows.Any((StaadForceRow r) => r.Member == id)))
			{
				throw new Exception("One or more selected members are absent from the imported force table.");
			}
			return rows.Where((StaadForceRow r) => ids.Contains(r.Member)).ToList();
		}
	}
}
