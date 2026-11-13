using System.Drawing;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal class AdminPasswordForm : Form
	{
		private readonly TextBox password;

		private readonly Label error;

		public AdminPasswordForm()
		{
			Text = "DTCE Tool Kit - Admin Password";
			base.ClientSize = new Size(455, 250);
			base.FormBorderStyle = FormBorderStyle.FixedDialog;
			base.MaximizeBox = false;
			base.MinimizeBox = false;
			base.StartPosition = FormStartPosition.CenterScreen;
			Font = new Font("Segoe UI", 10f);
			BackColor = Color.FromArgb(242, 245, 249);
			base.Controls.Add(new Label
			{
				Text = "ADMIN ACCESS",
				Location = new Point(24, 19),
				Size = new Size(400, 34),
				Font = new Font("Segoe UI", 16f, FontStyle.Bold),
				ForeColor = Color.FromArgb(23, 43, 67)
			});
			base.Controls.Add(new Label
			{
				Text = "Activate this installation or updated version.",
				Location = new Point(26, 66),
				Size = new Size(405, 27)
			});
			password = new TextBox
			{
				Location = new Point(27, 102),
				Width = 400,
				UseSystemPasswordChar = true,
				MaxLength = 128
			};
			error = new Label
			{
				Location = new Point(27, 139),
				Size = new Size(400, 40),
				ForeColor = Color.Firebrick
			};
			Button button = new Button
			{
				Text = "Open Admin",
				Location = new Point(192, 196),
				Size = new Size(120, 33)
			};
			Button button2 = new Button
			{
				Text = "Cancel",
				Location = new Point(321, 196),
				Size = new Size(105, 33),
				DialogResult = DialogResult.Cancel
			};
			button.Click += delegate
			{
				if (AdminPasswordGate.Verify(password.Text))
				{
					password.Clear();
					base.DialogResult = DialogResult.OK;
					Close();
				}
				else
				{
					password.Clear();
					error.Text = "Incorrect password. Please try again.";
					password.Focus();
				}
			};
			base.Controls.AddRange(new Control[4] { password, error, button, button2 });
			base.AcceptButton = button;
			base.CancelButton = button2;
			base.Shown += delegate
			{
				password.Focus();
			};
		}
	}
}
