using System.Drawing;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal class PowCard : Panel
	{
		public PowCard()
		{
			DoubleBuffered = true;
			BackColor = Color.White;
			base.Padding = new Padding(16);
			base.Margin = new Padding(0, 0, 16, 16);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			using (Pen pen = new Pen(PowTheme.Line))
			{
				e.Graphics.DrawRectangle(pen, 0, 0, base.Width - 1, base.Height - 1);
			}
		}
	}
}
