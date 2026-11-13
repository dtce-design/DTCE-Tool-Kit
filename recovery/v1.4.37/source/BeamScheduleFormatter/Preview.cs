using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace BeamScheduleFormatter
{
	internal class Preview
	{
		public static Bitmap Render(Dxf d)
		{
			List<Entity> list = d.ModelEntities();
			List<Entity> source = list.Where((Entity e) => e.Type == "LINE").ToList();
			double num = source.Min((Entity e) => Math.Min(e.Num(20), e.Num(21))) - 5.0;
			double left = source.Min((Entity e) => Math.Min(e.Num(10), e.Num(11))) - 4.0;
			double maxY = list.Where((Entity e) => e.Type == "MTEXT").Max((Entity e) => e.Num(20) + e.Num(40) * 1.5) + 2.0;
			int width = (int)((Math.Max(source.Max((Entity e) => Math.Max(e.Num(10), e.Num(11))) + 5.0, list.Where((Entity e) => e.Type == "MTEXT").Max((Entity e) => e.Num(10) + (new int[3] { 2, 5, 8 }.Contains((int)e.Num(71)) ? (e.Num(41) / 2.0) : e.Num(41))) + 4.0) - left) * 4.0);
			int num2 = (int)((maxY - num) * 4.0);
			if (num2 > 24000)
			{
				num2 = 24000;
			}
			Bitmap bitmap = new Bitmap(width, Math.Max(300, num2));
			using (Graphics graphics = Graphics.FromImage(bitmap))
			{
				graphics.Clear(Color.FromArgb(33, 40, 46));
				graphics.SmoothingMode = SmoothingMode.AntiAlias;
				graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
				Func<double, double, PointF> func = (double x, double y) => new PointF((float)((x - left) * 4.0), (float)((maxY - y) * 4.0));
				using (Pen pen = new Pen(Color.FromArgb(63, 139, 108), 0.7f))
				{
					using (SolidBrush brush = new SolidBrush(Color.FromArgb(76, 159, 121)))
					{
						foreach (Entity item in list)
						{
							if (item.Type == "HATCH")
							{
								List<PointF> list2 = new List<PointF>();
								bool flag = false;
								double arg = 0.0;
								foreach (Pair item2 in item.P)
								{
									if (item2.Code == 92)
									{
										flag = true;
										continue;
									}
									if (item2.Code == 97)
									{
										flag = false;
									}
									if (flag)
									{
										if (item2.Code == 10)
										{
											arg = double.Parse(item2.Value, CultureInfo.InvariantCulture);
										}
										if (item2.Code == 20)
										{
											list2.Add(func(arg, double.Parse(item2.Value, CultureInfo.InvariantCulture)));
										}
									}
								}
								if (list2.Count >= 3)
								{
									using (SolidBrush brush2 = new SolidBrush(item.Get(8).Contains("80") ? Color.FromArgb(51, 80, 80, 80) : Color.FromArgb(153, 51, 51, 51)))
									{
										graphics.FillPolygon(brush2, list2.ToArray());
									}
								}
							}
							else if (item.Type == "LINE")
							{
								graphics.DrawLine(pen, func(item.Num(10), item.Num(20)), func(item.Num(11), item.Num(21)));
							}
							else if (item.Type == "LWPOLYLINE" && item.Get(8) != "Construction Line")
							{
								List<PointF> list3 = new List<PointF>();
								double arg2 = 0.0;
								foreach (Pair item3 in item.P)
								{
									if (item3.Code == 10)
									{
										arg2 = double.Parse(item3.Value, CultureInfo.InvariantCulture);
									}
									if (item3.Code == 20)
									{
										list3.Add(func(arg2, double.Parse(item3.Value, CultureInfo.InvariantCulture)));
									}
								}
								if (list3.Count > 1)
								{
									graphics.DrawLines(pen, list3.ToArray());
								}
							}
							else
							{
								if (!(item.Type == "MTEXT"))
								{
									continue;
								}
								string input = item.Get(1).Replace("%%C", "Ø").Replace("%%c", "Ø")
									.Replace("\\P", "\n");
								input = Regex.Replace(input, "\\\\[Hf][^;]+;", "").Replace("{", "").Replace("}", "");
								float emSize = (float)(item.Num(40) * 4.0 / 0.72);
								float num3 = (float)(item.Num(41) * 4.0);
								int value = (int)item.Num(71);
								PointF pointF = func(item.Num(10), item.Num(20));
								using (Font font = new Font("Arial", emSize, (item.Get(7).Contains("B") || item.Get(1).Contains("|b1|")) ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel))
								{
									using (StringFormat stringFormat = new StringFormat(StringFormat.GenericTypographic))
									{
										stringFormat.Alignment = (new int[3] { 2, 5, 8 }.Contains(value) ? StringAlignment.Center : (new int[3] { 3, 6, 9 }.Contains(value) ? StringAlignment.Far : StringAlignment.Near));
										SizeF sizeF = graphics.MeasureString(input, font, (int)Math.Max(num3, 10f), stringFormat);
										float num4 = pointF.Y;
										if (new int[3] { 4, 5, 6 }.Contains(value))
										{
											num4 -= sizeF.Height / 2f;
										}
										if (new int[3] { 7, 8, 9 }.Contains(value))
										{
											num4 -= sizeF.Height;
										}
										float num5 = pointF.X;
										if (stringFormat.Alignment == StringAlignment.Center)
										{
											num5 -= num3 / 2f;
										}
										if (stringFormat.Alignment == StringAlignment.Far)
										{
											num5 -= num3;
										}
										graphics.DrawString(input, font, brush, new RectangleF(num5, num4, num3, Math.Max(sizeF.Height + 5f, 10f)), stringFormat);
									}
								}
							}
						}
						return bitmap;
					}
				}
			}
		}
	}
}
