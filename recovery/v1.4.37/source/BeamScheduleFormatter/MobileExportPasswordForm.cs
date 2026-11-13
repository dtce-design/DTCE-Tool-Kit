using System.Drawing;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal class MobileExportPasswordForm : Form
	{
		private TextBox password;

		private TextBox confirm;

		public string ExportPassword { get; private set; }

		public MobileExportPasswordForm()
		{
			AppBrand.Apply(this);
			Text = "Export key for iPhone";
			base.ClientSize = new Size(460, 305);
			base.StartPosition = FormStartPosition.CenterParent;
			base.FormBorderStyle = FormBorderStyle.FixedDialog;
			base.MaximizeBox = false;
			base.MinimizeBox = false;
			Font = new Font("Segoe UI", 10f);
			base.Controls.Add(new Label
			{
				Text = "Protect the mobile signing key with a strong password. Keep the exported file and password private.",
				Location = new Point(20, 18),
				Size = new Size(420, 55)
			});
			base.Controls.Add(new Label
			{
				Text = "Password (at least 12 characters)",
				Location = new Point(20, 82),
				Size = new Size(420, 23)
			});
			password = new TextBox
			{
				UseSystemPasswordChar = true,
				Location = new Point(20, 109),
				Size = new Size(420, 28),
				MaxLength = 256
			};
			base.Controls.Add(password);
			base.Controls.Add(new Label
			{
				Text = "Confirm password",
				Location = new Point(20, 150),
				Size = new Size(420, 23)
			});
			confirm = new TextBox
			{
				UseSystemPasswordChar = true,
				Location = new Point(20, 177),
				Size = new Size(420, 28),
				MaxLength = 256
			};
			base.Controls.Add(confirm);
			Button button = new Button
			{
				Text = "Continue",
				Location = new Point(20, 238),
				Size = new Size(200, 40)
			};
			base.Controls.Add(button);
			base.AcceptButton = button;
			Button button2 = new Button
			{
				Text = "Cancel",
				DialogResult = DialogResult.Cancel,
				Location = new Point(240, 238),
				Size = new Size(200, 40)
			};
			base.Controls.Add(button2);
			base.CancelButton = button2;
			button.Click += delegate
			{
				if (password.Text.Length < 12 || password.Text != confirm.Text)
				{
					MessageBox.Show(this, "Enter matching passwords with at least 12 characters.");
				}
				else
				{
					ExportPassword = password.Text;
					base.DialogResult = DialogResult.OK;
				}
			};
		}

		protected override void OnFormClosed(FormClosedEventArgs e)
		{
			password.Clear();
			confirm.Clear();
			base.OnFormClosed(e);
		}
	}
}
