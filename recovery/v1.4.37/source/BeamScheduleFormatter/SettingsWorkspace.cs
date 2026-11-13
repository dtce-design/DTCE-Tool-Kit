using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal static class SettingsWorkspace
	{
		public static void Show(IWin32Window owner, string title, string[] names, Control[] panels)
		{
			using (Form form = new Form
			{
				Text = title + " | Settings",
				ClientSize = new Size(900, 610),
				MinimumSize = new Size(720, 480),
				StartPosition = FormStartPosition.CenterParent,
				Font = new Font("Segoe UI", 10f)
			})
			{
				AppBrand.Apply(form);
				TabControl tabControl = new TabControl
				{
					Dock = DockStyle.Fill
				};
				FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
				{
					Dock = DockStyle.Bottom,
					Height = 54,
					Padding = new Padding(10),
					FlowDirection = FlowDirection.RightToLeft
				};
				Button button = new Button
				{
					Text = "Done",
					Width = 110,
					Height = 32,
					DialogResult = DialogResult.OK
				};
				flowLayoutPanel.Controls.Add(button);
				Label value = new Label
				{
					Text = "Changes apply immediately. Save the estimate/project to keep them.",
					AutoSize = true,
					Padding = new Padding(0, 8, 14, 0)
				};
				flowLayoutPanel.Controls.Add(value);
				form.Controls.Add(tabControl);
				form.Controls.Add(flowLayoutPanel);
				form.AcceptButton = button;
				form.CancelButton = button;
				Control[] array = panels.Select((Control p) => p.Parent).ToArray();
				int[] array2 = panels.Select((Control p) => (p.Parent != null) ? p.Parent.Controls.GetChildIndex(p) : 0).ToArray();
				DockStyle[] array3 = panels.Select((Control p) => p.Dock).ToArray();
				Size[] array4 = panels.Select((Control p) => p.Size).ToArray();
				bool[] array5 = panels.Select((Control p) => p.Visible).ToArray();
				try
				{
					for (int num = 0; num < panels.Length; num++)
					{
						TabPage tabPage = new TabPage(names[num])
						{
							Padding = new Padding(16)
						};
						tabControl.TabPages.Add(tabPage);
						tabPage.Controls.Add(panels[num]);
						panels[num].Dock = DockStyle.Fill;
						panels[num].Visible = true;
					}
					PowTheme.Apply(form);
					form.ShowDialog(owner);
				}
				finally
				{
					for (int num2 = 0; num2 < panels.Length; num2++)
					{
						tabControl.TabPages[num2].Controls.Remove(panels[num2]);
						if (array[num2] != null)
						{
							array[num2].Controls.Add(panels[num2]);
							array[num2].Controls.SetChildIndex(panels[num2], Math.Min(array2[num2], array[num2].Controls.Count - 1));
						}
						panels[num2].Dock = array3[num2];
						panels[num2].Size = array4[num2];
						panels[num2].Visible = array5[num2];
					}
				}
			}
		}

		public static void MoveButtons(FlowLayoutPanel tools, FlowLayoutPanel settings, params string[] names)
		{
			Button[] array = (from b in tools.Controls.OfType<Button>()
				where names.Contains(b.Text)
				select b).ToArray();
			foreach (Button value in array)
			{
				settings.Controls.Add(value);
			}
		}
	}
}
