using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace BeamScheduleFormatter
{
	internal static class CommercialBars
	{
		public static readonly double[] Lengths = new double[7] { 6.0, 7.5, 9.0, 10.5, 12.0, 13.5, 15.0 };

		public static List<StockAllocation> Allocate(IEnumerable<EstimateInput> inputs, LengthConfiguration config, IEnumerable<double> available = null)
		{
			List<StockAllocation> list = new List<StockAllocation>();
			foreach (EstimateInput item in inputs.Where((EstimateInput i) => i.Result != null))
			{
				foreach (EstimateLine line in item.Result.Lines)
				{
					EstimateInput estimateInput = new EstimateInput
					{
						Quantity = item.Quantity,
						Result = new BeamEstimate()
					};
					estimateInput.Result.Lines.Add(line);
					foreach (StockDemand item2 in Demands(new EstimateInput[1] { estimateInput }, config))
					{
						list.Add(new StockAllocation
						{
							Entry = item,
							Line = line,
							Demand = item2,
							Allowed = Available(available)
						});
					}
				}
			}
			foreach (var item3 in from a in list
				group a by new
				{
					a.Demand.Grade,
					a.Demand.Diameter,
					a.Demand.Cut,
					a.Demand.Lap
				})
			{
				long pieces = item3.Sum((StockAllocation a) => a.Demand.Pieces);
				StockChoice choice = Best(Choices(item3.Key.Cut, pieces, item3.Key.Lap, available));
				foreach (StockAllocation item4 in item3)
				{
					item4.Choice = choice;
				}
			}
			RebarOffcuts.Allocate(list);
			return list;
		}

		public static List<double> Available(IEnumerable<double> values)
		{
			List<double> list = (from x in (values ?? Lengths).Distinct()
				orderby x
				select x).ToList();
			if (list.Count == 0 || list.Any((double x) => !Lengths.Contains(x)))
			{
				throw new ArgumentException("Select at least one supported commercial length.");
			}
			return list;
		}

		public static string FormatLengths(IEnumerable<double> values)
		{
			return string.Join(",", values.Select((double v) => v.ToString("0.##", CultureInfo.InvariantCulture)));
		}

		public static List<double> ParseLengths(string text)
		{
			if (!string.IsNullOrWhiteSpace(text))
			{
				return Available(from x in text.Split(',')
					select double.Parse(x, CultureInfo.InvariantCulture));
			}
			return Available(null);
		}

		public static List<StockChoice> Choices(double cut, long pieces, double lap = 0.0, IEnumerable<double> available = null)
		{
			if (double.IsNaN(cut) || double.IsInfinity(cut) || cut <= 0.0 || pieces <= 0)
			{
				throw new ArgumentException("Invalid stock cutting demand.");
			}
			List<StockChoice> list = new List<StockChoice>();
			foreach (double item in Available(available))
			{
				if (cut > item)
				{
					if (!(lap <= 0.0) && !(lap >= item) && !double.IsNaN(lap) && !double.IsInfinity(lap))
					{
						long num;
						for (num = (long)Math.Ceiling((cut - lap) / (item - lap)); (cut + (double)(num - 1) * lap) / (double)num > item; num++)
						{
						}
						list.Add(new StockChoice
						{
							Length = item,
							Cut = cut,
							Lap = lap,
							Segments = num,
							Required = pieces,
							PiecesPerStock = 1L,
							Bars = checked(pieces * num)
						});
					}
					continue;
				}
				long num2 = (long)Math.Floor(item / cut);
				if (num2 >= 1)
				{
					while (num2 > 0 && (double)num2 * cut > item)
					{
						num2--;
					}
					if (num2 != 0L)
					{
						long bars = pieces / num2 + ((pieces % num2 != 0) ? 1 : 0);
						list.Add(new StockChoice
						{
							Length = item,
							Cut = cut,
							Required = pieces,
							PiecesPerStock = num2,
							Bars = bars
						});
					}
				}
			}
			return list;
		}

		public static StockChoice Best(IEnumerable<StockChoice> choices)
		{
			return (from c in choices
				orderby Math.Round(c.Waste, 9), c.Bars, c.Length
				select c).FirstOrDefault();
		}

		public static List<StockDemand> Demands(IEnumerable<EstimateInput> inputs, LengthConfiguration configuration = null)
		{
			List<StockDemand> result = new List<StockDemand>();
			checked
			{
				foreach (EstimateInput item in inputs.Where((EstimateInput x) => x.Result != null))
				{
					long num = long.Parse(item.Quantity, CultureInfo.InvariantCulture);
					foreach (EstimateLine line in item.Result.Lines)
					{
						Action<double, long> action = delegate(double cut, long n)
						{
							if (n > 0 && cut > 0.0)
							{
								result.Add(new StockDemand
								{
									Grade = line.Grade,
									Diameter = line.Diameter,
									Cut = cut,
									Pieces = n,
									Lap = ((cut > 6.0 && line.Part != "Stirrups" && configuration != null) ? (configuration.Rounded(configuration.Values(line.Diameter, line.Part.StartsWith("Top")).Lap) / 1000.0) : 0.0)
								});
							}
						};
						if (line.Part == "Stirrups")
						{
							long num2 = line.TieSetsPerBeam * num;
							if (line.TieOverride)
							{
								action(line.CutLength, num2);
								continue;
							}
							action(line.MainHoopCut, num2);
							action(line.AdditionalLegCut, num2 * line.AdditionalLegs);
						}
						else
						{
							action(line.CutLength, line.PiecesPerBeam * num);
						}
					}
				}
				return (from d in result
					group d by new { d.Grade, d.Diameter, d.Cut, d.Lap } into g
					select new StockDemand
					{
						Grade = g.Key.Grade,
						Diameter = g.Key.Diameter,
						Cut = g.Key.Cut,
						Lap = g.Key.Lap,
						Pieces = g.Sum((StockDemand x) => x.Pieces)
					} into d
					orderby d.Grade, d.Diameter, d.Cut descending
					select d).ToList();
			}
		}

		public static string Report(IEnumerable<EstimateInput> inputs, LengthConfiguration configuration = null, IEnumerable<double> available = null)
		{
			StringBuilder stringBuilder = new StringBuilder();
			List<StockAllocation> list = Allocate(inputs, configuration, available);
			stringBuilder.AppendLine("COMMERCIAL REBAR CUTTING / PURCHASE PLAN");
			stringBuilder.AppendLine("Available stock lengths: " + FormatLengths(Available(available)) + " m.");
			stringBuilder.AppendLine("Compatible offcuts are checked across different cut lengths and entries; grades and diameters stay separate.");
			stringBuilder.AppendLine("Longer segments are allocated first; available offcuts are fitted before a new bar is purchased.");
			stringBuilder.AppendLine("New-bar length uses the per-cut minimum-unused-length comparison; this is a feasible heuristic, not a global optimum.");
			stringBuilder.AppendLine("Zero kerf. Spliced cuts include configured laps; actual splice locations require approved detailing.");
			PurchaseReports.Summary(stringBuilder, list);
			PurchaseReports.Basis(stringBuilder);
			foreach (StockAllocation item in list)
			{
				stringBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, "G{0} D{1} | cut {2:0.000}m | {3} pcs | cut steel {4:0.00}kg | lap {5:0.00}kg | final offcut share {6:0.00}kg | BUY {7:0.00}kg", item.Demand.Grade, item.Demand.Diameter, item.Demand.Cut, item.Demand.Pieces, item.Net, item.Splice, item.Waste, item.Purchase));
				if (item.Choice == null)
				{
					stringBuilder.AppendLine("REVIEW: unresolved cut excluded from purchase.");
				}
			}
			return stringBuilder.ToString();
		}
	}
}
