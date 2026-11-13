using System;

namespace BeamScheduleFormatter
{
	internal class SlabQuantities
	{
		public double AreaPerPanel;

		public double ConcretePerPanel;

		public double ConcreteTotal;

		public double SoffitTotal;

		public double EdgeFormsTotal;

		public double FormworkTotal
		{
			get
			{
				return SoffitTotal + EdgeFormsTotal;
			}
		}

		public static SlabQuantities Calculate(double shortMetres, double longMetres, double thicknessMm, int quantity, bool elevated = true, double exposedEdgeMetres = 0.0, bool cantileverEdge = false)
		{
			double[] array = new double[3] { shortMetres, longMetres, thicknessMm };
			foreach (double num in array)
			{
				if (double.IsNaN(num) || double.IsInfinity(num) || num <= 0.0)
				{
					throw new ArgumentException("Slab dimensions and thickness must be positive finite numbers.");
				}
			}
			if (shortMetres > longMetres)
			{
				throw new ArgumentException("Shorter Side must not exceed Longer Side.");
			}
			if (quantity < 1 || quantity > 100000)
			{
				throw new ArgumentException("Enter a whole slab quantity from 1 to 100000.");
			}
			if (!cantileverEdge && exposedEdgeMetres > 0.0)
			{
				throw new ArgumentException("Exposed-edge forms are counted only when the cantilever-edge option is selected.");
			}
			if (double.IsNaN(exposedEdgeMetres) || double.IsInfinity(exposedEdgeMetres) || exposedEdgeMetres < 0.0 || exposedEdgeMetres > 2.0 * (shortMetres + longMetres))
			{
				throw new ArgumentException("Exposed edge length must be between zero and panel perimeter.");
			}
			double num2 = shortMetres * longMetres;
			double num3 = num2 * thicknessMm / 1000.0;
			if (double.IsInfinity(num3 * (double)quantity))
			{
				throw new ArgumentException("Slab quantities exceed calculation limits.");
			}
			return new SlabQuantities
			{
				AreaPerPanel = num2,
				ConcretePerPanel = num3,
				ConcreteTotal = num3 * (double)quantity,
				SoffitTotal = (elevated ? (num2 * (double)quantity) : 0.0),
				EdgeFormsTotal = exposedEdgeMetres * thicknessMm / 1000.0 * (double)quantity
			};
		}
	}
}
