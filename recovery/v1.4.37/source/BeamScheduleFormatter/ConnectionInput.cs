using System;
using System.Linq;

namespace BeamScheduleFormatter
{
	internal class ConnectionInput
	{
		public string Family = "Angle";

		public string Method = "LRFD";

		public string Connection = "Weld";

		public string Grade = "A325";

		public double Area = 1000.0;

		public double Thickness = 6.0;

		public double Offset = 20.0;

		public double Fy = 248.0;

		public double Fu = 400.0;

		public double GussetFy = 248.0;

		public double GussetFu = 400.0;

		public double GussetThickness = 6.0;

		public double GussetWidth = 150.0;

		public double Demand;

		public double Weld = 4.0;

		public double WeldEdgeThickness;

		public double Fexx = 482.65;

		public double LT;

		public double LL1 = 100.0;

		public double LL2 = 100.0;

		public double BoltDiameter = 16.0;

		public double HoleDiameter = 18.0;

		public double EdgeDistance = 35.0;

		public double Spacing = 50.0;

		public double Fnv = 372.0;

		public double VerifiedU;

		public double Agv;

		public double Anv;

		public double Ant;

		public int Bolts = 3;

		public int ShearPlanes = 1;

		public bool Exposed = true;

		public bool DirectionalWeldIncrease;

		public bool AllElementsConnected;

		public bool VerifiedProperties;

		public void Validate()
		{
			if (!new string[4] { "Angle", "Double angle", "HSS rectangle", "HSS circle / pipe" }.Contains(Family) || !new string[2] { "Weld", "Bolts" }.Contains(Connection) || !new string[2] { "LRFD", "ASD" }.Contains(Method))
			{
				throw new Exception("Choose family, method and connection.");
			}
			SteelWeight.Positive(Area, Thickness, Fy, Fu, GussetThickness, GussetFy, GussetFu, GussetWidth);
			if (Fu < Fy || GussetFu < GussetFy || double.IsNaN(Demand) || double.IsInfinity(Demand) || Demand < 0.0 || double.IsNaN(Offset) || double.IsInfinity(Offset) || Offset < 0.0)
			{
				throw new Exception("Check strength, centroid offset and nonnegative axial demand.");
			}
			if (VerifiedU < 0.0 || VerifiedU > 1.0 || double.IsNaN(VerifiedU))
			{
				throw new Exception("Verified shear-lag U must be 0 (derive) or >0 and <=1.");
			}
			double[] array;
			if (Connection == "Weld")
			{
				SteelWeight.Positive(Weld, Fexx);
				if (double.IsNaN(WeldEdgeThickness) || double.IsInfinity(WeldEdgeThickness) || WeldEdgeThickness < 0.0 || WeldEdgeThickness > Math.Max(Thickness, GussetThickness))
				{
					throw new Exception("Check actual thickness at the welded edge.");
				}
				array = new double[3] { LT, LL1, LL2 };
				foreach (double num in array)
				{
					if (double.IsNaN(num) || double.IsInfinity(num) || num < 0.0)
					{
						throw new Exception("Weld lengths must be finite and nonnegative.");
					}
				}
				if (LT + LL1 + LL2 <= 0.0)
				{
					throw new Exception("Enter at least one weld length.");
				}
			}
			else
			{
				if (Family.StartsWith("HSS"))
				{
					throw new Exception("Supplied HSS sheets cover welded connections only; bolted HSS wall checks require a separate detail.");
				}
				SteelWeight.Count(Bolts);
				SteelWeight.Positive(BoltDiameter, HoleDiameter, EdgeDistance, Spacing, Fnv);
				if (!new string[2] { "A307", "A325" }.Contains(Grade) || ShearPlanes < 1 || ShearPlanes > 2 || HoleDiameter <= BoltDiameter)
				{
					throw new Exception("Check bolt grade, shear planes and explicit hole diameter.");
				}
			}
			array = new double[3] { Agv, Anv, Ant };
			foreach (double num2 in array)
			{
				if (double.IsNaN(num2) || double.IsInfinity(num2) || num2 < 0.0)
				{
					throw new Exception("Block shear areas must be finite and nonnegative.");
				}
			}
			if ((Agv > 0.0 || Anv > 0.0 || Ant > 0.0) && (!(Agv > 0.0) || !(Anv > 0.0) || !(Ant > 0.0)))
			{
				throw new Exception("Enter all three block-shear areas or leave all zero.");
			}
			if (Anv > Agv)
			{
				throw new Exception("Net shear area cannot exceed gross shear area.");
			}
		}
	}
}
