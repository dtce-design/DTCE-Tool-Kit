using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal class SmoothPageBox : PictureBox
	{
		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			base.OnPaint(e);
		}
	}
}
