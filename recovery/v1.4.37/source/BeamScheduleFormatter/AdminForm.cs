using System.Drawing;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal class AdminForm : MainForm
	{
		private CodeGeneratorForm generator;

		public AdminForm()
		{
			Text = "Beam Schedule Formatter - ADMIN v1.4.37";
			ActionPanel.Height = 218;
			Button button = new Button
			{
				Text = "Access code\ngenerator",
				AccessibleName = "Access code generator",
				Width = 194,
				Height = 52,
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.FromArgb(20, 33, 51),
				ForeColor = Color.White,
				Font = new Font("Segoe UI Semibold", 10f),
				Cursor = Cursors.Hand,
				Margin = new Padding(0, 7, 0, 4)
			};
			button.Click += delegate
			{
				OpenGenerator();
			};
			ActionPanel.Controls.Add(button);
		}

		public void OpenGenerator()
		{
			if (generator == null || generator.IsDisposed)
			{
				generator = new CodeGeneratorForm();
				generator.Show(this);
			}
			else
			{
				generator.Show();
				generator.BringToFront();
			}
		}
	}
}
