using System;
using System.Collections.Generic;
using System.Linq;

namespace BeamScheduleFormatter
{
	internal class PurlinProperties
	{
		public double Area;

		public double Ix;

		public double Iy;

		public double Ixy;

		public double Sx;

		public double Sy;

		public double Xbar;

		public double Ybar;

		public bool Verified;

		public readonly List<double[]> Points = new List<double[]>();

		public static PurlinProperties Calculate(SteelProfile p)
		{
			if (p == null || p.Kind != "Purlin")
			{
				throw new Exception("Choose a purlin profile.");
			}
			double h = p.H;
			double b = p.B;
			double lip = p.Lip;
			double t = p.T;
			SteelWeight.Positive(h, b, lip, t);
			if (h <= 2.0 * lip || b <= 2.0 * t || lip <= t)
			{
				throw new Exception("Section dimensions do not fit.");
			}
			bool num = p.Name.StartsWith("Z", StringComparison.OrdinalIgnoreCase);
			List<double[]> list = new List<double[]>();
			if (!num)
			{
				list.Add(new double[4] { 0.0, 0.0, t, h });
				list.Add(new double[4]
				{
					t,
					0.0,
					b - t,
					t
				});
				list.Add(new double[4]
				{
					t,
					h - t,
					b - t,
					t
				});
				list.Add(new double[4]
				{
					b - t,
					t,
					t,
					lip - t
				});
				list.Add(new double[4]
				{
					b - t,
					h - lip,
					t,
					lip - t
				});
			}
			else
			{
				list.Add(new double[4]
				{
					(0.0 - t) / 2.0,
					0.0,
					t,
					h
				});
				list.Add(new double[4]
				{
					t / 2.0,
					h - t,
					b - t,
					t
				});
				list.Add(new double[4]
				{
					0.0 - b + t / 2.0,
					0.0,
					b - t,
					t
				});
				list.Add(new double[4]
				{
					b - 1.5 * t,
					h - lip,
					t,
					lip - t
				});
				list.Add(new double[4]
				{
					0.0 - b + t / 2.0,
					t,
					t,
					lip - t
				});
			}
			PurlinProperties purlinProperties = new PurlinProperties();
			purlinProperties.Area = list.Sum((double[] r) => r[2] * r[3]);
			purlinProperties.Xbar = list.Sum((double[] r) => r[2] * r[3] * (r[0] + r[2] / 2.0)) / purlinProperties.Area;
			purlinProperties.Ybar = list.Sum((double[] r) => r[2] * r[3] * (r[1] + r[3] / 2.0)) / purlinProperties.Area;
			foreach (double[] item in list)
			{
				double num2 = item[2] * item[3];
				double num3 = item[0] + item[2] / 2.0 - purlinProperties.Xbar;
				double num4 = item[1] + item[3] / 2.0 - purlinProperties.Ybar;
				purlinProperties.Ix += item[2] * Math.Pow(item[3], 3.0) / 12.0 + num2 * num4 * num4;
				purlinProperties.Iy += item[3] * Math.Pow(item[2], 3.0) / 12.0 + num2 * num3 * num3;
				purlinProperties.Ixy += num2 * num3 * num4;
				double[] array = new double[2]
				{
					item[0],
					item[0] + item[2]
				};
				foreach (double num6 in array)
				{
					double[] array2 = new double[2]
					{
						item[1],
						item[1] + item[3]
					};
					foreach (double num8 in array2)
					{
						purlinProperties.Points.Add(new double[2]
						{
							num6 - purlinProperties.Xbar,
							num8 - purlinProperties.Ybar
						});
					}
				}
			}
			purlinProperties.Sx = purlinProperties.Ix / purlinProperties.Points.Max((double[] a) => Math.Abs(a[1]));
			purlinProperties.Sy = purlinProperties.Iy / purlinProperties.Points.Max((double[] a) => Math.Abs(a[0]));
			return purlinProperties;
		}

		public void Validate()
		{
			SteelWeight.Positive(Area, Ix, Iy, Sx, Sy);
			if (double.IsNaN(Ixy) || double.IsInfinity(Ixy) || Ix * Iy - Ixy * Ixy <= 0.0)
			{
				throw new Exception("Section inertia matrix must be positive definite.");
			}
		}
	}
}
