using System;
using System.Drawing;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal class LoginForm : Form
	{
		private readonly string request = AccessCodes.GetRequest();

		private TextBox response;

		private Label message;

		private int failures;

		private Timer delay;

		public LoginForm()
		{
			AppBrand.Apply(this);
			Text = "DTCE Tool Kit - Access code";
			base.ClientSize = new Size(620, 440);
			base.StartPosition = FormStartPosition.CenterScreen;
			base.FormBorderStyle = FormBorderStyle.FixedDialog;
			base.MaximizeBox = false;
			Font = new Font("Segoe UI", 10f);
			BackColor = Color.FromArgb(245, 248, 251);
			base.Controls.Add(new Label
			{
				Text = "DTCE TOOL KIT",
				Location = new Point(25, 22),
				Size = new Size(570, 35),
				Font = new Font("Segoe UI", 14f, FontStyle.Bold)
			});
			base.Controls.Add(new Label
			{
				Text = "Send this request to the owner for a new access code.",
				Location = new Point(25, 75),
				Size = new Size(565, 25)
			});
			TextBox value = new TextBox
			{
				Text = request,
				ReadOnly = true,
				Location = new Point(25, 105),
				Size = new Size(445, 58),
				Multiline = true
			};
			base.Controls.Add(value);
			Button button = new Button
			{
				Text = "Copy request",
				Location = new Point(480, 105),
				Size = new Size(115, 58)
			};
			button.Click += delegate
			{
				Copy(request);
			};
			base.Controls.Add(button);
			base.Controls.Add(new Label
			{
				Text = "Paste your access code or Store certification code:",
				Location = new Point(25, 177),
				Size = new Size(565, 25)
			});
			response = new TextBox
			{
				Location = new Point(25, 210),
				Size = new Size(570, 78),
				Multiline = true,
				MaxLength = 2048,
				ScrollBars = ScrollBars.Vertical
			};
			base.Controls.Add(response);
			Button unlock = new Button
			{
				Text = "Unlock DTCE Tool Kit",
				Location = new Point(25, 300),
				Size = new Size(570, 40),
				BackColor = Color.FromArgb(0, 139, 139),
				ForeColor = Color.White
			};
			base.Controls.Add(unlock);
			base.AcceptButton = unlock;
			message = new Label
			{
				Text = "Normal: until app closes or 8 hours expire, whichever comes first.\nCertification: 30 days, exact app version only.\nVersion: 1.4.37 | Key ID: " + AccessCodes.KeyId(AccessCodes.PublicKey()),
				Location = new Point(25, 355),
				Size = new Size(570, 65)
			};
			base.Controls.Add(message);
			delay = new Timer
			{
				Interval = 1500
			};
			delay.Tick += delegate
			{
				delay.Stop();
				unlock.Enabled = true;
			};
			base.FormClosed += delegate
			{
				delay.Dispose();
			};
			unlock.Click += delegate
			{
				try
				{
					if (AccessCodes.Accept(request, response.Text))
					{
						response.Clear();
						base.DialogResult = DialogResult.OK;
						Close();
					}
					else
					{
						failures++;
						message.Text = "Invalid or expired code. Normal codes must match this request.\nCertification codes must match User version 1.4.37 and key ID.";
						response.SelectAll();
						response.Focus();
						if (failures >= 3)
						{
							unlock.Enabled = false;
							delay.Start();
						}
					}
				}
				catch (Exception ex)
				{
					message.Text = "Cannot save approval: " + ex.Message;
				}
			};
		}

		internal static void Copy(string text)
		{
			try
			{
				Clipboard.SetText(text);
			}
			catch (Exception)
			{
				MessageBox.Show("Clipboard is busy. Select and copy the text manually.");
			}
		}
	}
}
