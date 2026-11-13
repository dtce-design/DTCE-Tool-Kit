using System.Drawing;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal static class InputFocus
	{
		public static void Apply(Control root)
		{
			foreach (Control c in root.Controls)
			{
				TextBox textBox = c as TextBox;
				NumericUpDown n = c as NumericUpDown;
				ComboBox comboBox = c as ComboBox;
				if ((textBox != null && !textBox.ReadOnly) || n != null || comboBox != null)
				{
					Color normal = c.BackColor;
					c.Enter += delegate
					{
						c.BackColor = Color.FromArgb(232, 245, 255);
					};
					c.Leave += delegate
					{
						c.BackColor = normal;
					};
					c.Cursor = Cursors.IBeam;
					if (n != null)
					{
						n.Enter += delegate
						{
							n.Select(0, n.Text.Length);
						};
					}
				}
				if (!(c is DataGridView))
				{
					Apply(c);
				}
			}
		}
	}
}
