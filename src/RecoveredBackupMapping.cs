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

		/* the BACKUP item a typed / app quantity belongs to: its id when it is still that item, else the one item with the same
		   item no., description and unit (owner, 19 November 2026: "hindi pa din niya namamap sa BACKUP") */
		public static BackupTarget ResolveTarget(BackupDemand d, IEnumerable<BackupTarget> targets)
		{
			if (d == null || d.TargetSlotId == null) return null;
			var list = targets.ToList();
			var exact = list.FirstOrDefault(x => x.SlotId == d.TargetSlotId && Identity(x) == d.TargetIdentity);
			if (exact != null) return exact;
			exact = list.FirstOrDefault(x => x.SlotId == d.TargetSlotId && SameIdentity(x, d.TargetIdentity));
			if (exact != null) return exact;
			var same = list.Where(x => SameIdentity(x, d.TargetIdentity)).ToList();
			return same.Count == 1 ? same[0] : null;
		}

		public static string Identity(BackupTarget target)
		{
			return Name((target.Code ?? "").Trim().ToUpperInvariant() + "|" + Normal(target.Description) + "|" + Unit(target.Unit)).Substring("_DTCE_BACKUP_MAP_".Length);
		}

		public static BackupTarget ResolveSaved(int row, string identity, IEnumerable<BackupTarget> targets)
		{
			return targets.SingleOrDefault((BackupTarget t) => t.Row == row && SameIdentity(t, identity));
		}

		/* Owner (19 November 2026, "hindi ls ang yunit niyan kg ang unit niyan"): the BACKUP unit cells of the owner's workbook are
		   formulas into another workbook (='[1]DETAILED'!BE..) and read "ls" for items that are measured in kg, sq.m., cu.m. ...
		   Read as LS, a mapped kg / m2 quantity was only kept as the LS basis and nothing was written. A DPWH catalogue item (same
		   item no. and description) that reads "ls" uses its catalogue unit; true LS items keep LS. */
		static Dictionary<string, string> catalogueUnits;
		public static string EffectiveUnit(string code, string description, string unit)
		{
			if (Unit(unit) != "ls" && !string.IsNullOrWhiteSpace(unit)) return unit;
			if (catalogueUnits == null)
			{
				var units = new Dictionary<string, string>(StringComparer.Ordinal);
				foreach (var g in BackupPayItemCatalog.Items().GroupBy(t => (t.Code ?? "").Trim().ToUpperInvariant() + "|" + Normal(t.Description)))
				{
					var distinct = g.Select(t => t.Unit).Distinct().ToList();
					if (distinct.Count == 1) units[g.Key] = distinct[0];
				}
				catalogueUnits = units;
			}
			string real;
			if (catalogueUnits.TryGetValue((code ?? "").Trim().ToUpperInvariant() + "|" + Normal(description), out real) && Unit(real) != "ls") return real;
			return unit;
		}

		/* a name written by an earlier version may carry the identity with the unit as read ("ls") */
		public static bool SameIdentity(BackupTarget t, string identity)
		{
			if (t == null || identity == null) return false;
			if (Identity(t) == identity) return true;
			return !string.IsNullOrEmpty(t.ReadUnit) && t.ReadUnit != t.Unit && Identity(new BackupTarget { Code = t.Code, Description = t.Description, Unit = t.ReadUnit }) == identity;
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
		// BACKUP item is left unmapped (shown in the BOQ, not written to Excel); an LS item without a manual quantity keeps its mapping as basis.
		// instead of blocking the whole mapping. Returns the reason, or null when the route can be written.
		// Painting items (wood, masonry, metal, railing painting) are never mapped automatically: they may not be part of
		// the POW workbook yet. They stay listed in Bill of Quantities and are written only after the user maps them.
		public static bool IsPainting(BackupDemand d)
		{
			return d.Description != null && d.Description.IndexOf("paint", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		// Owner (17 November 2026): a quantity mapped to an LS pay item stays mapped even before the LS quantity is
		// entered. It is listed under that item as its measurement basis; the LS total is written only when a manual LS
		// quantity exists in Bill of Quantities (never a default 0 or 1 LS). Only an unselected row is left unmapped.
		public static string Incomplete(BackupDemand d, BackupTarget t, List<BackupDemand> list, IDictionary<string, BackupTarget> map)
		{
			return (t == null) ? "no BACKUP item selected" : null;
		}

		// True when the LS row has no manual LS quantity yet: mapped quantities are kept as basis, nothing is written.
		public static bool BasisOnly(BackupTarget t, IEnumerable<BackupDemand> mapped)
		{
			return Unit(t.Unit) == "ls" && !mapped.Any((BackupDemand d) => Unit(d.Unit) == "ls" && (d.Quantity > 0.0 || d.Key.StartsWith("LS include|", StringComparison.Ordinal)));
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
				/* the same BACKUP item read under another id (owner, 19 November 2026) is accepted by its item no., description and unit */
				/* only a quantity typed on a pay item is bound to it; app quantities may be mapped to another item (19 November 2026) */
				if (d.ManualOverride && d.TargetSlotId != null && Identity(t) != d.TargetIdentity)
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
