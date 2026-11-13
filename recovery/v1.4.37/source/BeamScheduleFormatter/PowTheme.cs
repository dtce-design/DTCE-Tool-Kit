using System;
using System.Collections;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal static class PowTheme
	{
		private static readonly ConditionalWeakTable<Control, object> themed = new ConditionalWeakTable<Control, object>();

		public static readonly Color Ink = Color.FromArgb(25, 43, 63);

		public static readonly Color Teal = Color.FromArgb(0, 112, 120);

		public static readonly Color Background = Color.FromArgb(242, 246, 250);

		public static readonly Color Line = Color.FromArgb(216, 225, 234);

		public static readonly Color Muted = Color.FromArgb(88, 107, 126);

		public static void Apply(Control root)
		{
			object value;
			if (themed.TryGetValue(root, out value))
			{
				foreach (Control control in root.Controls)
				{
					Apply(control);
				}
				return;
			}
			themed.Add(root, new object());
			if (root is Form || root is TabPage)
			{
				root.BackColor = Background;
			}
			FlowLayoutPanel flowLayoutPanel = root as FlowLayoutPanel;
			if (flowLayoutPanel != null && flowLayoutPanel.Dock == DockStyle.Top)
			{
				flowLayoutPanel.AutoScroll = true;
			}
			TabControl tab = root as TabControl;
			if (tab != null)
			{
				tab.Font = new Font("Segoe UI", 9.5f);
				tab.Padding = new Point(16, 10);
				tab.DrawMode = TabDrawMode.OwnerDrawFixed;
				tab.DrawItem += delegate(object s, DrawItemEventArgs e)
				{
					if (e.Index >= 0 && e.Index < tab.TabCount)
					{
						Rectangle bounds = e.Bounds;
						bool flag = e.Index == tab.SelectedIndex;
						using (SolidBrush brush = new SolidBrush(flag ? Color.White : Background))
						{
							e.Graphics.FillRectangle(brush, bounds);
						}
						TextRenderer.DrawText(e.Graphics, tab.TabPages[e.Index].Text, tab.Font, bounds, flag ? Teal : Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter);
						if (flag)
						{
							using (Pen pen = new Pen(Teal, 3f))
							{
								e.Graphics.DrawLine(pen, bounds.Left + 8, bounds.Bottom - 2, bounds.Right - 8, bounds.Bottom - 2);
							}
						}
					}
				};
			}
			Button button = root as Button;
			if (button != null && !(button is ToolModuleButton))
			{
				button.FlatStyle = FlatStyle.Flat;
				button.FlatAppearance.BorderSize = 1;
				button.FlatAppearance.BorderColor = Line;
				button.BackColor = Color.White;
				button.ForeColor = Ink;
				button.Cursor = Cursors.Hand;
				button.FlatAppearance.MouseOverBackColor = Color.FromArgb(231, 244, 244);
				button.FlatAppearance.MouseDownBackColor = Color.FromArgb(211, 233, 234);
				if (button.Text == "Apply" || button.Text == "Apply deductions" || button.Text == "Build preview" || button.Text == "Save project information" || button.Text == "Save footing" || button.Text == "Open / Link Excel")
				{
					button.BackColor = Teal;
					button.ForeColor = Color.White;
					button.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 91, 99);
					button.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 76, 83);
					button.FlatAppearance.BorderSize = 0;
				}
			}
			DataGridView dataGridView = root as DataGridView;
			if (dataGridView != null)
			{
				dataGridView.EnableHeadersVisualStyles = false;
				dataGridView.BackgroundColor = Color.White;
				dataGridView.BorderStyle = BorderStyle.FixedSingle;
				dataGridView.GridColor = Line;
				dataGridView.ColumnHeadersDefaultCellStyle.BackColor = Ink;
				dataGridView.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
				dataGridView.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
				dataGridView.ColumnHeadersDefaultCellStyle.Padding = new Padding(6);
				dataGridView.ColumnHeadersHeight = Math.Max(56, dataGridView.ColumnHeadersHeight);
				dataGridView.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
				dataGridView.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f);
				dataGridView.DefaultCellStyle.ForeColor = Ink;
				dataGridView.DefaultCellStyle.Padding = new Padding(6, 4, 6, 4);
				dataGridView.DefaultCellStyle.SelectionBackColor = Color.FromArgb(218, 239, 240);
				dataGridView.DefaultCellStyle.SelectionForeColor = Ink;
				dataGridView.RowTemplate.Height = 34;
				dataGridView.RowTemplate.MinimumHeight = 34;
				dataGridView.AllowUserToResizeRows = false;
				dataGridView.AllowUserToOrderColumns = false;
				dataGridView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
				dataGridView.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
				foreach (DataGridViewColumn column in dataGridView.Columns)
				{
					column.SortMode = DataGridViewColumnSortMode.NotSortable;
				}
				foreach (DataGridViewRow item in (IEnumerable)dataGridView.Rows)
				{
					item.MinimumHeight = 34;
				}
				dataGridView.ColumnAdded += delegate(object sender, DataGridViewColumnEventArgs args)
				{
					args.Column.SortMode = DataGridViewColumnSortMode.NotSortable;
				};
				dataGridView.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 250, 253);
				GridAppearance.Apply(dataGridView);
			}
			TextBox textBox = root as TextBox;
			if (textBox != null)
			{
				textBox.BorderStyle = BorderStyle.FixedSingle;
				if (!textBox.ReadOnly)
				{
					textBox.BackColor = Color.FromArgb(255, 252, 237);
				}
			}
			Label guide = root as Label;
			if (guide != null && guide.Dock == DockStyle.Top)
			{
				int minimum = guide.Height;
				bool fittingGuide = false;
				Action fitGuide = delegate
				{
					if (fittingGuide)
					{
						return;
					}
					fittingGuide = true;
					try
					{
						int width = Math.Max(100, guide.ClientSize.Width - guide.Padding.Horizontal - 4);
						int val = TextRenderer.MeasureText(guide.Text, guide.Font, new Size(width, int.MaxValue), TextFormatFlags.TextBoxControl | TextFormatFlags.WordBreak).Height + guide.Padding.Vertical + 8;
						int num = Math.Max(minimum, val);
						if (guide.Height != num)
						{
							guide.Height = num;
						}
					}
					finally
					{
						fittingGuide = false;
					}
				};
				guide.Resize += delegate
				{
					fitGuide();
				};
				guide.TextChanged += delegate
				{
					fitGuide();
				};
				guide.FontChanged += delegate
				{
					fitGuide();
				};
				fitGuide();
			}
			NumericUpDown numericUpDown = root as NumericUpDown;
			if (numericUpDown != null)
			{
				numericUpDown.BorderStyle = BorderStyle.FixedSingle;
				numericUpDown.BackColor = Color.FromArgb(255, 252, 237);
			}
			ComboBox comboBox = root as ComboBox;
			if (comboBox != null)
			{
				comboBox.FlatStyle = FlatStyle.Flat;
				comboBox.BackColor = Color.FromArgb(255, 252, 237);
			}
			GroupBox groupBox = root as GroupBox;
			if (groupBox != null)
			{
				groupBox.ForeColor = Ink;
			}
			foreach (Control control2 in root.Controls)
			{
				Apply(control2);
			}
			root.ControlAdded += delegate(object s, ControlEventArgs e)
			{
				Apply(e.Control);
			};
		}
	}
}
