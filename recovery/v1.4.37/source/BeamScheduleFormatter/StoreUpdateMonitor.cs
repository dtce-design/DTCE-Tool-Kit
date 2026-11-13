using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal sealed class StoreUpdateMonitor : IDisposable
	{
		private readonly Form owner;

		private readonly StoreUpdatePolicy policy = new StoreUpdatePolicy();

		private readonly Timer timer = new Timer
		{
			Interval = 2000
		};

		private readonly Dictionary<Form, bool> disabled = new Dictionary<Form, bool>();

		private Form gate;

		private Label message;

		private Button retry;

		private bool disposed;

		private bool closing;

		public StoreUpdateMonitor(Form owner)
		{
			this.owner = owner;
			timer.Tick += async delegate
			{
				timer.Interval = 30000;
				await Check(false);
			};
			owner.Shown += delegate
			{
				timer.Start();
			};
			owner.FormClosed += delegate
			{
				Dispose();
			};
		}

		private async Task Check(bool force)
		{
			if (disposed || closing || owner.IsDisposed || policy.Checking)
			{
				return;
			}
			IntPtr hwnd = owner.Handle;
			if (retry != null)
			{
				retry.Enabled = false;
			}
			await policy.CheckAsync(() => Task.Run(() => StoreUpdateQuery.CheckAsync(hwnd)), DateTime.UtcNow, force);
			if (disposed || owner.IsDisposed)
			{
				return;
			}
			StoreUpdateState.Blocked = policy.Blocked;
			if (policy.Blocked)
			{
				if (gate == null)
				{
					gate = new Form
					{
						Text = "Update required",
						ClientSize = new Size(610, 235),
						FormBorderStyle = FormBorderStyle.FixedDialog,
						ControlBox = false,
						StartPosition = FormStartPosition.CenterParent,
						ShowInTaskbar = false
					};
					AppBrand.Apply(gate);
					message = new Label
					{
						Text = "A newer DTCE Tool Kit version is available from Microsoft Store. Update the app before continuing. Save your work when closing the app; restart after updating.\n\nIf the Store cannot be reached, Check again allows offline use.",
						Left = 20,
						Top = 18,
						Width = 570,
						Height = 130,
						Font = new Font("Segoe UI", 10f)
					};
					Button button = new Button
					{
						Text = "Open Microsoft Store",
						Left = 20,
						Top = 169,
						Width = 190,
						Height = 40
					};
					button.Click += delegate
					{
						try
						{
							Process.Start(new ProcessStartInfo("ms-windows-store://pdp/?ProductId=" + StoreUpdateQuery.ProductId)
							{
								UseShellExecute = true
							});
						}
						catch
						{
							message.Text = "Microsoft Store could not be opened. Open Store manually, update DTCE Tool Kit, then restart. You can also Check again for offline use.";
						}
					};
					retry = new Button
					{
						Text = "Check again",
						Left = 220,
						Top = 169,
						Width = 150,
						Height = 40
					};
					retry.Click += async delegate
					{
						await Check(true);
					};
					Button button2 = new Button
					{
						Text = "Close app",
						Left = 380,
						Top = 169,
						Width = 150,
						Height = 40
					};
					button2.Click += delegate
					{
						closing = true;
						RestoreWindows();
						StoreUpdateState.Blocked = false;
						try
						{
							owner.Close();
						}
						finally
						{
							closing = false;
							if (!disposed)
							{
								StoreUpdateState.Blocked = policy.Blocked;
								DisableWindows();
							}
						}
					};
					gate.Controls.AddRange(new Control[4] { message, button, retry, button2 });
					gate.Show(owner);
				}
				DisableWindows();
			}
			else
			{
				RestoreWindows();
				if (gate != null)
				{
					gate.Close();
					gate.Dispose();
					gate = null;
					retry = null;
				}
			}
			if (retry != null)
			{
				retry.Enabled = true;
			}
		}

		private void DisableWindows()
		{
			Form[] array = Application.OpenForms.Cast<Form>().ToArray();
			foreach (Form form in array)
			{
				if (form != gate && !disabled.ContainsKey(form))
				{
					disabled.Add(form, form.Enabled);
					form.Enabled = false;
				}
			}
			if (gate != null)
			{
				gate.BringToFront();
			}
		}

		private void RestoreWindows()
		{
			foreach (KeyValuePair<Form, bool> item in disabled)
			{
				if (!item.Key.IsDisposed)
				{
					item.Key.Enabled = item.Value;
				}
			}
			disabled.Clear();
		}

		public void Dispose()
		{
			if (!disposed)
			{
				disposed = true;
				timer.Dispose();
				StoreUpdateState.Blocked = false;
				RestoreWindows();
				if (gate != null)
				{
					gate.Dispose();
					gate = null;
				}
			}
		}
	}
}
