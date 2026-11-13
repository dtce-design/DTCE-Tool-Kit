using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BeamScheduleFormatter
{
	internal static class PlateCutPlanner
	{
		private const double Epsilon = 1E-09;

		public static List<PlateSheet> Allocate(IEnumerable<PlateDemand> source)
		{
			List<PlateDemand> list = source.ToList();
			if (((IEnumerable<PlateDemand>)list).Sum((Func<PlateDemand, long>)((PlateDemand plateDemand) => plateDemand.Quantity)) > 5000)
			{
				throw new Exception("Plate fitting supports up to 5000 cuts per project.");
			}
			foreach (PlateDemand d in list)
			{
				SteelWeight.Positive(d.Length, d.Width, d.ThicknessMm);
				SteelWeight.Count(d.Quantity);
				if (!PlateCatalog.HasThickness(d.ThicknessMm))
				{
					throw new Exception("Plate thickness " + d.ThicknessMm.ToString(CultureInfo.InvariantCulture) + " mm is not listed in the handbook.");
				}
				if (!PlateCatalog.Formats.Any((PlateFormat f) => Fits(f.Width, f.Length, d.Width, d.Length) || Fits(f.Width, f.Length, d.Length, d.Width)))
				{
					throw new Exception("Plate " + d.Member + " does not fit any handbook sheet, even when rotated.");
				}
			}
			List<PlateSheet> list2 = new List<PlateSheet>();
			foreach (IGrouping<double, PlateDemand> item in from plateDemand in list
				group plateDemand by plateDemand.ThicknessMm)
			{
				List<PlateSheet> list3 = null;
				double num = double.MaxValue;
				for (int num2 = 0; num2 < 3; num2++)
				{
					for (int num3 = 0; num3 < 2; num3++)
					{
						for (int num4 = -1; num4 < PlateCatalog.Formats.Length; num4++)
						{
							IOrderedEnumerable<PlateDemand> source2;
							switch (num2)
							{
							default:
								source2 = item.OrderByDescending((PlateDemand plateDemand) => Math.Min(plateDemand.Width, plateDemand.Length));
								break;
							case 1:
								source2 = item.OrderByDescending((PlateDemand plateDemand) => Math.Max(plateDemand.Width, plateDemand.Length));
								break;
							case 0:
								source2 = item.OrderByDescending((PlateDemand plateDemand) => plateDemand.Area);
								break;
							}
							List<PlateSheet> list4 = Pack(source2.ThenBy((PlateDemand plateDemand) => plateDemand.Member, StringComparer.Ordinal).ToList(), num4, num3);
							if (list4 != null)
							{
								double num5 = list4.Sum((PlateSheet s) => s.BuyKg);
								if (num5 < num - 1E-09 || (Math.Abs(num5 - num) < 1E-09 && (list3 == null || list4.Count < list3.Count)))
								{
									list3 = list4;
									num = num5;
								}
							}
						}
					}
				}
				if (list3 == null)
				{
					throw new Exception("Unable to fit plate cuts in the handbook sheets.");
				}
				list2.AddRange(list3);
			}
			return list2;
		}

		private static bool Fits(double width, double length, double cutWidth, double cutLength)
		{
			if (cutWidth <= width + 1E-09)
			{
				return cutLength <= length + 1E-09;
			}
			return false;
		}

		private static List<PlateSheet> Pack(List<PlateDemand> demands, int fixedFormat, int split)
		{
			List<PlateSheet> list = new List<PlateSheet>();
			foreach (PlateDemand demand in demands)
			{
				int count;
				for (count = 0; count < demand.Quantity; count++)
				{
					PlateSheet plateSheet = null;
					int index = -1;
					bool flag = false;
					double num = double.MaxValue;
					foreach (PlateSheet item in list)
					{
						for (int i = 0; i < item.Free.Count; i++)
						{
							PlateRectangle plateRectangle = item.Free[i];
							for (int j = 0; j < 2; j++)
							{
								double num2 = ((j == 0) ? demand.Width : demand.Length);
								double num3 = ((j == 0) ? demand.Length : demand.Width);
								if (Fits(plateRectangle.Width, plateRectangle.Length, num2, num3))
								{
									double num4 = plateRectangle.Width * plateRectangle.Length - demand.Area + Math.Min(plateRectangle.Width - num2, plateRectangle.Length - num3) * 1E-06;
									if (num4 < num)
									{
										num = num4;
										plateSheet = item;
										index = i;
										flag = j == 1;
									}
								}
							}
						}
					}
					if (plateSheet == null)
					{
						PlateFormat plateFormat = (from f in (fixedFormat < 0) ? PlateCatalog.Formats : new PlateFormat[1] { PlateCatalog.Formats[fixedFormat] }
							where Fits(f.Width, f.Length, demand.Width, demand.Length) || Fits(f.Width, f.Length, demand.Length, demand.Width)
							orderby f.Area / Math.Max(1.0, Math.Min(demand.Quantity - count, Math.Max(Math.Floor((f.Width + 1E-09) / demand.Width) * Math.Floor((f.Length + 1E-09) / demand.Length), Math.Floor((f.Width + 1E-09) / demand.Length) * Math.Floor((f.Length + 1E-09) / demand.Width)))), f.Area
							select f).FirstOrDefault();
						if (plateFormat == null)
						{
							return null;
						}
						plateSheet = new PlateSheet
						{
							Format = plateFormat,
							ThicknessMm = demand.ThicknessMm
						};
						plateSheet.Free.Add(new PlateRectangle
						{
							Width = plateFormat.Width,
							Length = plateFormat.Length
						});
						list.Add(plateSheet);
						index = 0;
						bool num5 = Fits(plateFormat.Width, plateFormat.Length, demand.Width, demand.Length);
						bool flag2 = Fits(plateFormat.Width, plateFormat.Length, demand.Length, demand.Width);
						double num6 = Math.Floor((plateFormat.Width + 1E-09) / demand.Width) * Math.Floor((plateFormat.Length + 1E-09) / demand.Length);
						double num7 = Math.Floor((plateFormat.Width + 1E-09) / demand.Length) * Math.Floor((plateFormat.Length + 1E-09) / demand.Width);
						flag = !num5 || (flag2 && num7 > num6);
					}
					PlateRectangle plateRectangle2 = plateSheet.Free[index];
					plateSheet.Free.RemoveAt(index);
					double num8 = (flag ? demand.Length : demand.Width);
					double num9 = (flag ? demand.Width : demand.Length);
					plateSheet.Cuts.Add(new PlatePlacement
					{
						Demand = demand,
						X = plateRectangle2.X,
						Y = plateRectangle2.Y,
						Width = num8,
						Length = num9
					});
					double width = Math.Max(0.0, plateRectangle2.Width - num8);
					double length = Math.Max(0.0, plateRectangle2.Length - num9);
					if (split == 0)
					{
						AddFree(plateSheet, plateRectangle2.X + num8, plateRectangle2.Y, width, plateRectangle2.Length);
						AddFree(plateSheet, plateRectangle2.X, plateRectangle2.Y + num9, num8, length);
					}
					else
					{
						AddFree(plateSheet, plateRectangle2.X + num8, plateRectangle2.Y, width, num9);
						AddFree(plateSheet, plateRectangle2.X, plateRectangle2.Y + num9, plateRectangle2.Width, length);
					}
				}
			}
			return list;
		}

		private static void AddFree(PlateSheet sheet, double x, double y, double width, double length)
		{
			if (width > 1E-09 && length > 1E-09)
			{
				sheet.Free.Add(new PlateRectangle
				{
					X = x,
					Y = y,
					Width = width,
					Length = length
				});
			}
		}
	}
}
