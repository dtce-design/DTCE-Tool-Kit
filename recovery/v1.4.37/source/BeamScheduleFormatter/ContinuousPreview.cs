using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal class ContinuousPreview : ScrollableControl
	{
		private readonly List<List<string>> pages;

		private readonly string heading;

		private float zoom;

		private float scale;

		private int pageWidth;

		private int pageHeight;

		private int lastPage;

		public int PageCount
		{
			get
			{
				return pages.Count;
			}
		}

		public event Action<int> PageChanged;

		public ContinuousPreview(string report)
		{
			pages = EstimatePdf.Pages(report);
			heading = EstimatePdf.Heading(report);
			AutoScroll = true;
			BackColor = Color.FromArgb(90, 100, 112);
			DoubleBuffered = true;
			base.ResizeRedraw = true;
			base.TabStop = true;
			SetStyle(ControlStyles.Selectable, true);
		}

		public void SetZoom(float value)
		{
			int page = CurrentPage();
			zoom = value;
			LayoutPages();
			Jump(page);
		}

		private int CurrentPage()
		{
			return Math.Max(1, Math.Min(PageCount, (-base.AutoScrollPosition.Y + 16) / (Math.Max(1, pageHeight) + 24) + 1));
		}

		private void LayoutPages()
		{
			scale = ((zoom == 0f) ? Math.Max(0.25f, (float)(base.ClientSize.Width - 48) / 595.28f) : (zoom * 96f / 72f));
			pageWidth = (int)(595.28f * scale);
			pageHeight = (int)(841.89f * scale);
			base.AutoScrollMinSize = new Size(pageWidth + 48, PageCount * (pageHeight + 24) + 24);
			Invalidate();
		}

		public void Jump(int page)
		{
			base.AutoScrollPosition = new Point(Math.Max(0, -base.AutoScrollPosition.X), Math.Max(0, (page - 1) * (pageHeight + 24)));
			Invalidate();
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			LayoutPages();
		}

		protected override void OnScroll(ScrollEventArgs e)
		{
			base.OnScroll(e);
			Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			if (pageHeight <= 0)
			{
				return;
			}
			e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
			e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
			for (int i = Math.Max(0, -base.AutoScrollPosition.Y / (pageHeight + 24)); i < PageCount; i++)
			{
				int num = 16 + i * (pageHeight + 24) + base.AutoScrollPosition.Y;
				if (num > base.ClientSize.Height)
				{
					break;
				}
				int num2 = Math.Max(16, (base.ClientSize.Width - pageWidth) / 2) + base.AutoScrollPosition.X;
				GraphicsState gstate = e.Graphics.Save();
				try
				{
					e.Graphics.SetClip(new Rectangle(num2, num, pageWidth, pageHeight));
					e.Graphics.PageUnit = GraphicsUnit.Point;
					e.Graphics.TranslateTransform((float)num2 * 72f / e.Graphics.DpiX, (float)num * 72f / e.Graphics.DpiY);
					e.Graphics.ScaleTransform(scale * 72f / e.Graphics.DpiX, scale * 72f / e.Graphics.DpiY);
					CalculationPreview.DrawPage(e.Graphics, pages[i].ToArray(), i + 1, PageCount, heading);
				}
				finally
				{
					e.Graphics.Restore(gstate);
				}
			}
			int num3 = CurrentPage();
			if (num3 != lastPage)
			{
				lastPage = num3;
				if (this.PageChanged != null)
				{
					this.PageChanged(num3);
				}
			}
		}
	}
}
