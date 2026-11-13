using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal static class GridAppearance
	{
		private static readonly ConditionalWeakTable<DataGridView, object> applied = new ConditionalWeakTable<DataGridView, object>();

		public static void Apply(DataGridView g)
		{
			object value;
			if (applied.TryGetValue(g, out value))
			{
				return;
			}
			applied.Add(g, new object());
			g.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
			g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			g.CellBorderStyle = DataGridViewCellBorderStyle.Single;
			g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
			g.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
			g.GridColor = Color.FromArgb(175, 188, 202);
			g.BorderStyle = BorderStyle.FixedSingle;
			g.EditMode = DataGridViewEditMode.EditOnEnter;
			g.CellClick += delegate(object o, DataGridViewCellEventArgs a)
			{
				if (a.RowIndex >= 0 && a.ColumnIndex >= 0 && !g.Rows[a.RowIndex].Cells[a.ColumnIndex].ReadOnly)
				{
					g.BeginEdit(false);
				}
			};
			g.CellPainting += delegate(object s, DataGridViewCellPaintingEventArgs e)
			{
				if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && (e.State & DataGridViewElementStates.Selected) != DataGridViewElementStates.None)
				{
					e.CellStyle.SelectionBackColor = Color.FromArgb(218, 239, 240);
					e.CellStyle.SelectionForeColor = (e.CellStyle.ForeColor.IsEmpty ? Color.Black : e.CellStyle.ForeColor);
					e.Paint(e.ClipBounds, e.PaintParts & ~DataGridViewPaintParts.Focus);
					if (g.ContainsFocus && g.CurrentCell != null && g.CurrentCell.RowIndex == e.RowIndex && g.CurrentCell.ColumnIndex == e.ColumnIndex && !g.CurrentCell.ReadOnly)
					{
						Rectangle cellBounds = e.CellBounds;
						cellBounds.Inflate(-1, -1);
						using (Pen pen = new Pen(PowTheme.Teal, 2f))
						{
							e.Graphics.DrawRectangle(pen, cellBounds);
						}
					}
					e.Handled = true;
				}
			};
		}
	}
}
