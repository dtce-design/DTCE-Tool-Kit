using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace BeamScheduleFormatter
{
	internal static class BackupMapping
	{
		public const string MapPrefix = "_DTCE_BACKUP_MAP_";

		public const string BlockPrefix = "_DTCE_BACKUP_BLOCK_";

		public const string ItemPrefix = "_DTCE_BACKUP_ITEM_";

		public static string Unit(string value)
		{
			string text = Regex.Replace((value ?? "").ToLowerInvariant(), "[\\s.²³]", (Match m) => (!(m.Value == "²")) ? ((!(m.Value == "³")) ? "" : "3") : "2");
			if (new string[4] { "m2", "sqm", "sqmeter", "squaremetres" }.Contains(text))
			{
				return "m2";
			}
			if (new string[4] { "m3", "cum", "cu meter", "cubicmetres" }.Contains(text))
			{
				return "m3";
			}
			if (text == "lm")
			{
				return "m";
			}
			if (new string[3] { "ls", "is", "lumpsum" }.Contains(text))
			{
				return "ls";
			}
			if (text == "kg" || text == "kgs")
			{
				return "kg";
			}
			if (new string[7] { "ea", "each", "pc", "pcs", "set", "sets", "unit" }.Contains(text))
			{
				return "each";
			}
			return text;
		}

		public static string Name(string key)
		{
			using (SHA256 sHA = SHA256.Create())
			{
				return "_DTCE_BACKUP_MAP_" + BitConverter.ToString(sHA.ComputeHash(Encoding.UTF8.GetBytes(key))).Replace("-", "").Substring(0, 24);
			}
		}

		public static string Identity(BackupTarget target)
		{
			return Name((target.Code ?? "").Trim().ToUpperInvariant() + "|" + Normal(target.Description) + "|" + Unit(target.Unit)).Substring("_DTCE_BACKUP_MAP_".Length);
		}

		public static BackupTarget ResolveSaved(int row, string identity, IEnumerable<BackupTarget> targets)
		{
			return targets.SingleOrDefault((BackupTarget t) => t.Row == row && Identity(t) == identity);
		}

		private static string Normal(string text)
		{
			return Regex.Replace((text ?? "").ToUpperInvariant().Replace('×', 'X'), "[^A-Z0-9]", "");
		}

		public static BackupTarget Suggest(BackupDemand demand, IEnumerable<BackupTarget> targets)
		{
			List<BackupTarget> source = targets.Where((BackupTarget t) => Unit(t.Unit) == Unit(demand.Unit)).ToList();
			string d = Normal(demand.Description);
			List<BackupTarget> list = source.Where((BackupTarget t) => Normal(t.Description) == d).ToList();
			if (list.Count == 1)
			{
				return list[0];
			}
			string code = null;
			if (demand.Key.StartsWith("Rebar purchase by size|"))
			{
				if (d.Contains("GRADE40"))
				{
					code = "902(1)a1";
				}
				if (d.Contains("GRADE60"))
				{
					code = "902(1)a2";
				}
			}
			if (demand.Key.Contains("Forms") || d.Contains("FORMS"))
			{
				code = "903(2)";
			}
			if (d == "CHB100MM")
			{
				code = "1046(2)a1";
			}
			if (d == "CHB150MM")
			{
				code = "1046(2)a2";
			}
			if (d == "AAC100MM" || d == "AACBLOCKS100MM")
			{
				code = "1046(4)a1";
			}
			if (d == "AAC150MM" || d == "AACBLOCKS150MM")
			{
				code = "1046(4)a2";
			}
			if (d.Contains("LOUVERBLOCK"))
			{
				code = "1046(3)";
			}
			if (d.StartsWith("METALPAINTINGTOTAL"))
			{
				code = "1032(1)c";
			}
			if (d.Contains("PURLINSPURCHASE"))
			{
				code = "1047(8)b";
			}
			if (d.Contains("METALFASCIAPURCHASE"))
			{
				code = "1047(1)";
			}
			if (d.Contains("STEELPLATESPURCHASE"))
			{
				code = "1047(5)d";
			}
			if (d == "CROSSBRACINGRODS")
			{
				code = "1047(5)c";
			}
			if (d.StartsWith("8MMTHKDOUBLESIDED") || d.StartsWith("10MMTHKALUMINUMFOIL"))
			{
				list = source.Where((BackupTarget t) => Normal(t.Description).StartsWith(d.Replace("PURCHASECOVERAGE", ""))).ToList();
				if (list.Count == 1)
				{
					return list[0];
				}
			}
			if (code == null)
			{
				return null;
			}
			List<BackupTarget> list2 = source.Where((BackupTarget t) => (t.Code ?? "").Trim() == code).ToList();
			if (list2.Count != 1)
			{
				return null;
			}
			return list2[0];
		}

		public static double FinalQuantity(IEnumerable<BackupDemand> demands, string unit)
		{
			List<BackupDemand> list = demands.Where((BackupDemand d) => Unit(d.Unit) == Unit(unit)).ToList();
			if (list.Count == 0)
			{
				return 0.0;
			}
			int priority = list.Max((BackupDemand d) => d.ManualPriority);
			IEnumerable<BackupDemand> source;
			if (priority < 0)
			{
				IEnumerable<BackupDemand> enumerable = list;
				source = enumerable;
			}
			else
			{
				source = list.Where((BackupDemand d) => d.ManualPriority == priority);
			}
			return source.Sum((BackupDemand d) => d.Quantity);
		}

		// Owner (17 November 2026): mapping need not be complete. A quantity with no BACKUP item, or an LS item whose
		// manual LS quantity has not been entered, is simply left unmapped (shown in the BOQ, not written to Excel)
		// instead of blocking the whole mapping. Returns the reason, or null when the route can be written.
		public static string Incomplete(BackupDemand d, BackupTarget t, List<BackupDemand> list, IDictionary<string, BackupTarget> map)
		{
			if (t == null)
			{
				return "no BACKUP item selected";
			}
			if (Unit(d.Unit) == "ls" && d.Sources.Any((BillQuantity s) => s.Basis && Unit(s.Unit) != "ls") && (!(d.Quantity > 0.0) || !d.Sources.Any((BillQuantity s) => !s.Basis && Unit(s.Unit) == "ls")) && !list.Any((BackupDemand c) => c != d && Unit(c.Unit) == "ls" && (c.ManualOverride || c.Key.StartsWith("Bill of Quantities|")) && map.ContainsKey(c.Key) && map[c.Key].Row == t.Row))
			{
				return "enter the final manual LS quantity for " + t.Description + " in Bill of Quantities to include it";
			}
			if (Unit(d.Unit) != Unit(t.Unit) && Unit(t.Unit) == "ls" && !list.Any((BackupDemand control) => Unit(control.Unit) == "ls" && map.ContainsKey(control.Key) && map[control.Key].Row == t.Row))
			{
				return "LS item " + t.Description + " has no manual LS quantity in Bill of Quantities; no default LS quantity is inferred";
			}
			return null;
		}

		// Removes routes that cannot be written yet and returns them with their reasons.
		public static List<KeyValuePair<BackupDemand, string>> DropIncomplete(IEnumerable<BackupDemand> demands, IDictionary<string, BackupTarget> map)
		{
			List<BackupDemand> list = demands.ToList();
			List<KeyValuePair<BackupDemand, string>> dropped = new List<KeyValuePair<BackupDemand, string>>();
			for (bool changed = true; changed; )
			{
				changed = false;
				foreach (BackupDemand d in list)
				{
					BackupTarget t;
					if (!map.TryGetValue(d.Key, out t))
					{
						continue;
					}
					string reason = Incomplete(d, t, list, map);
					if (reason != null)
					{
						map.Remove(d.Key);
						dropped.Add(new KeyValuePair<BackupDemand, string>(d, reason));
						changed = true;
						break;
					}
				}
			}
			foreach (BackupDemand d in list)
			{
				if (!map.ContainsKey(d.Key) && !dropped.Any((KeyValuePair<BackupDemand, string> x) => x.Key == d))
				{
					dropped.Add(new KeyValuePair<BackupDemand, string>(d, "no BACKUP item selected"));
				}
			}
			return dropped;
		}

		// Validates the routes that will be written; every demand passed here must be mapped.
		public static void Validate(IEnumerable<BackupDemand> demands, IDictionary<string, BackupTarget> map)
		{
			List<BackupDemand> list = demands.ToList();
			if (list.Select((BackupDemand backupDemand) => backupDemand.Key).Distinct().Count() != list.Count)
			{
				throw new Exception("Duplicate required-quantity key in BACKUP mapping.");
			}
			foreach (BackupDemand d in list)
			{
				BackupTarget t;
				if (!map.TryGetValue(d.Key, out t) || t == null)
				{
					throw new Exception("Map this required quantity to a BACKUP item: " + d.Description);
				}
				string reason = Incomplete(d, t, list, map);
				if (reason != null)
				{
					throw new Exception(d.Description + ": " + reason + ".");
				}
				if (d.TargetSlotId != null && (t.SlotId != d.TargetSlotId || Identity(t) != d.TargetIdentity))
				{
					throw new Exception("The BOQ manual quantity target changed; review " + d.Description);
				}
				if (Unit(d.Unit) != Unit(t.Unit) && Unit(t.Unit) != "ls")
				{
					throw new Exception("Unit mismatch for " + d.Description + ": " + d.Unit + " versus " + t.Unit);
				}
				d.Lines();
			}
		}
	}
}
