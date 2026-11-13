using System;
using System.Drawing;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal class ToolModuleButton : Button
	{
		public int ModuleIndex;

		public string Description;

		private bool hover;

		private static readonly string[] descriptions = new string[6] { "Format and review beam schedules from DXF files.", "Beam, slab and footing takeoffs, earthworks and project reports.", "Development, lap splice and standard hook calculations.", "Set the project name and location for your reports.", "Generate access codes and manage approved tools.", "Group identical slab schedules and export the final DXF layout." };

		public ToolModuleButton()
		{
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
			Cursor = Cursors.Hand;
			base.FlatStyle = FlatStyle.Flat;
			base.TabStop = true;
			base.MouseEnter += delegate
			{
				hover = true;
				Invalidate();
			};
			base.MouseLeave += delegate
			{
				hover = false;
				Invalidate();
			};
			base.GotFocus += delegate
			{
				Invalidate();
			};
			base.LostFocus += delegate
			{
				Invalidate();
			};
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.Clear(hover ? Color.FromArgb(238, 249, 249) : Color.White);
			using (Pen pen = new Pen((hover || Focused) ? PowTheme.Teal : Color.FromArgb(207, 219, 230), (!Focused) ? 1 : 2))
			{
				e.Graphics.DrawRectangle(pen, 1, 1, base.Width - 3, base.Height - 3);
			}
			using (SolidBrush brush = new SolidBrush(PowTheme.Teal))
			{
				e.Graphics.FillRectangle(brush, 1, 1, 4, base.Height - 2);
			}
			using (Font font = new Font("Segoe UI", 11f, FontStyle.Bold))
			{
				TextRenderer.DrawText(e.Graphics, Text, font, new Rectangle(20, 20, base.Width - 40, 29), PowTheme.Ink, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
			}
			using (Font font2 = new Font("Segoe UI", 9.5f))
			{
				TextRenderer.DrawText(e.Graphics, Description ?? descriptions[Math.Min(5, ModuleIndex)], font2, new Rectangle(20, 57, base.Width - 42, 54), Color.FromArgb(92, 111, 128), TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak);
			}
		}
	}
}
