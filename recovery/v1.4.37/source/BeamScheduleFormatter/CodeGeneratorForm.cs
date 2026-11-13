using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal class CodeGeneratorForm : Form
	{
		private TextBox request;

		private TextBox output;

		private Label message;

		private string privateKey;

		internal static int SelectedPrograms(CheckedListBox apps)
		{
			int num = 0;
			for (int i = 0; i < ProgramAccess.Ids.Length; i++)
			{
				if (apps.GetItemChecked(i))
				{
					num |= ProgramAccess.Ids[i];
				}
			}
			return num;
		}

		public CodeGeneratorForm()
		{
			AppBrand.Apply(this);
			Text = "DTCE Tool Kit Admin - Code Generator";
			base.ClientSize = new Size(940, 715);
			base.StartPosition = FormStartPosition.CenterScreen;
			base.FormBorderStyle = FormBorderStyle.FixedDialog;
			base.MaximizeBox = false;
			Font = new Font("Segoe UI", 10f);
			BackColor = Color.FromArgb(245, 248, 251);
			base.Controls.Add(new Label
			{
				Text = "DTCE TOOL KIT - ACCESS ADMIN",
				Location = new Point(25, 22),
				Size = new Size(890, 35),
				Font = new Font("Segoe UI", 18f, FontStyle.Bold)
			});
			base.Controls.Add(new Label
			{
				Text = "Paste the request from the user's currently open app.",
				Location = new Point(335, 75),
				Size = new Size(575, 25)
			});
			request = new TextBox
			{
				Location = new Point(335, 107),
				Size = new Size(575, 78),
				Multiline = true,
				MaxLength = 200
			};
			base.Controls.Add(request);
			base.Controls.Add(new Label
			{
				Text = "APPS ALLOWED",
				Location = new Point(25, 75),
				Size = new Size(280, 25),
				Font = new Font("Segoe UI", 11f, FontStyle.Bold)
			});
			base.Controls.Add(new Label
			{
				Text = "GENERATED ACCESS CODE",
				Location = new Point(335, 200),
				Size = new Size(575, 25),
				Font = new Font("Segoe UI", 11f, FontStyle.Bold)
			});
			CheckedListBox apps = new CheckedListBox
			{
				Location = new Point(25, 110),
				Size = new Size(280, 165),
				CheckOnClick = true,
				BorderStyle = BorderStyle.FixedSingle,
				BackColor = Color.White,
				Font = new Font("Segoe UI", 11f),
				IntegralHeight = false,
				HorizontalScrollbar = true
			};
			CheckedListBox.ObjectCollection items = apps.Items;
			object[] names = ProgramAccess.Names;
			items.AddRange(names);
			base.Controls.Add(apps);
			base.Controls.Add(new Label
			{
				Text = "Project Information is included automatically with valid access.",
				Location = new Point(25, 280),
				Size = new Size(280, 60)
			});
			apps.ItemCheck += delegate
			{
				output.Clear();
			};
			Button generate = new Button
			{
				Text = "Generate selected app access",
				Location = new Point(25, 350),
				Size = new Size(280, 50)
			};
			base.Controls.Add(generate);
			output = new TextBox
			{
				Location = new Point(335, 230),
				Size = new Size(575, 210),
				Multiline = true,
				ReadOnly = true,
				ScrollBars = ScrollBars.Vertical,
				Font = new Font("Consolas", 10f),
				BackColor = Color.White
			};
			base.Controls.Add(output);
			Button button = new Button
			{
				Text = "Copy access code",
				Location = new Point(335, 455),
				Size = new Size(280, 40)
			};
			button.Click += delegate
			{
				if (output.Text != "")
				{
					LoginForm.Copy(output.Text);
				}
			};
			base.Controls.Add(button);
			Button button2 = new Button
			{
				Text = "Clear",
				Location = new Point(630, 455),
				Size = new Size(280, 40)
			};
			button2.Click += delegate
			{
				request.Clear();
				output.Clear();
			};
			base.Controls.Add(button2);
			message = new Label
			{
				Location = new Point(25, 420),
				Size = new Size(280, 145)
			};
			base.Controls.Add(message);
			request.TextChanged += delegate
			{
				output.Clear();
			};
			Button button3 = new Button
			{
				Text = "Import existing key",
				Location = new Point(25, 405),
				Size = new Size(280, 32)
			};
			base.Controls.Add(button3);
			message.Location = new Point(25, 445);
			message.Size = new Size(280, 120);
			Action loadKey = delegate
			{
				try
				{
					privateKey = AdminKeyStore.Load();
					generate.Enabled = true;
					message.Text = "Key ID: " + AccessCodes.KeyId(AccessCodes.PublicKey()) + "\nOwner only. Keep a backup of your existing key.";
				}
				catch (Exception)
				{
					privateKey = null;
					generate.Enabled = false;
					message.Text = "Import your existing AdminPrivateKey.dat. It must match this app and be used under its original Windows account.";
				}
			};
			button3.Click += delegate
			{
				using (OpenFileDialog openFileDialog = new OpenFileDialog
				{
					Title = "Import existing Admin private key",
					Filter = "Admin private key|AdminPrivateKey.dat",
					CheckFileExists = true
				})
				{
					if (openFileDialog.ShowDialog(this) != DialogResult.OK)
					{
						return;
					}
					try
					{
						AdminKeyStore.Import(openFileDialog.FileName);
						output.Clear();
						loadKey();
					}
					catch (Exception)
					{
						MessageBox.Show(this, "Cannot import this key. Use the matching original key under its original Windows account.", "Key import failed");
					}
				}
			};
			CheckBox certification = new CheckBox
			{
				Text = "Store certification (30 days)",
				Location = new Point(335, 520),
				Size = new Size(360, 30)
			};
			base.Controls.Add(certification);
			base.Controls.Add(new Label
			{
				Text = "Exact User version",
				Location = new Point(335, 560),
				Size = new Size(160, 25)
			});
			TextBox certVersion = new TextBox
			{
				Text = "1.4.37",
				Location = new Point(505, 557),
				Size = new Size(160, 30),
				Enabled = false
			};
			base.Controls.Add(certVersion);
			base.Controls.Add(new Label
			{
				Text = "Certification: no request needed; all apps included. Share only in Notes for certification. Anyone with the code can use this version until expiry.",
				Location = new Point(335, 610),
				Size = new Size(575, 70)
			});
			certification.CheckedChanged += delegate
			{
				output.Clear();
				request.Enabled = !certification.Checked;
				apps.Enabled = !certification.Checked;
				certVersion.Enabled = certification.Checked;
				generate.Text = (certification.Checked ? "Generate certification code" : "Generate selected app access");
			};
			certVersion.TextChanged += delegate
			{
				output.Clear();
			};
			loadKey();
			Button exportMobile = new Button
			{
				Text = "Export key for iPhone",
				Location = new Point(25, 610),
				Size = new Size(280, 40)
			};
			base.Controls.Add(exportMobile);
			exportMobile.Click += async delegate
			{
				try
				{
					string key = AdminKeyStore.Load();
					string password;
					using (MobileExportPasswordForm mobileExportPasswordForm = new MobileExportPasswordForm())
					{
						if (mobileExportPasswordForm.ShowDialog(this) != DialogResult.OK)
						{
							return;
						}
						password = mobileExportPasswordForm.ExportPassword;
					}
					using (SaveFileDialog save = new SaveFileDialog
					{
						Title = "Save password-protected mobile key",
						Filter = "DTCE encrypted mobile key|*.dtcekey",
						FileName = "DTCE-Mobile-Access.dtcekey",
						DefaultExt = "dtcekey",
						AddExtension = true
					})
					{
						if (save.ShowDialog(this) != DialogResult.OK)
						{
							return;
						}
						exportMobile.Enabled = false;
						try
						{
							string contents = await Task.Run(() => MobileKeyExport.Create(key, password, AccessCodes.PublicKey()));
							File.WriteAllText(save.FileName, contents, new UTF8Encoding(false));
							MessageBox.Show(this, "Mobile key exported. Transfer it privately to your iPhone Files app. Do not upload it to GitHub. The original Windows key is unchanged.", "Export complete");
						}
						finally
						{
							exportMobile.Enabled = true;
							password = null;
							key = null;
						}
					}
				}
				catch (Exception)
				{
					MessageBox.Show(this, "Cannot export the mobile key. Import the matching existing Admin key first and choose a writable file location.", "Export failed");
				}
			};
			generate.Click += delegate
			{
				output.Clear();
				try
				{
					output.Text = (certification.Checked ? AccessCodes.GenerateCertification(privateKey, certVersion.Text.Trim()) : AccessCodes.Generate(request.Text, privateKey, SelectedPrograms(apps)));
					message.Text = "Key ID: " + AccessCodes.KeyId(AccessCodes.PublicKey()) + (certification.Checked ? ("\nCertification: all apps, 30 days. User v" + certVersion.Text.Trim() + " only. Expires " + DateTime.UtcNow.AddDays(30.0).ToLocalTime().ToString("yyyy-MM-dd HH:mm")) : "\nApproved for 8 hours from now, including after reopening the user app.");
				}
				catch (Exception ex)
				{
					MessageBox.Show(this, ex.Message, "Cannot generate code");
				}
			};
		}
	}
}
