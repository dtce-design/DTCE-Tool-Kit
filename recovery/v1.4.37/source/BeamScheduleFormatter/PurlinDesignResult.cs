using System.Collections.Generic;

namespace BeamScheduleFormatter
{
	internal class PurlinDesignResult
	{
		public readonly List<PurlinLoadCase> Cases = new List<PurlinLoadCase>();

		public double Dx;

		public double Dy;

		public double Lx;

		public double Ly;

		public double Fbx;

		public double Fby;

		public double Cx;

		public double Cy;

		public double Basic;

		public double Alternate;

		public double LiveDeflection;

		public double TotalDeflection;

		public double LiveLimit;

		public double TotalLimit;

		public bool WithinWorkbookChecks
		{
			get
			{
				if (Basic <= 1.0 && Alternate <= 1.0 && LiveDeflection <= LiveLimit)
				{
					return TotalDeflection <= TotalLimit;
				}
				return false;
			}
		}
	}
}
