using System;
using System.Drawing;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal static class DxfSourceManager
	{
		public static int ChooseRemoval(IWin32Window owner, string[] names, int[] affected)
		{
			if (names.Length == 0)
			{
				MessageBox.Show(owner, "No DXF files are loaded.", "DXF files");
				return -1;
			}
			Form f = new Form
			{
				Text = "Manage loaded DXF files",
				ClientSize = new Size(700, 420),
				MinimumSize = new Size(600, 400),
				StartPosition = FormStartPosition.CenterParent,
				Font = new Font("Segoe UI", 10f)
			};
			try
			{
				Label value = new Label
				{
					Text = "Select a DXF source to remove",
					Dock = DockStyle.Top,
					Height = 52,
					Padding = new Padding(14),
					Font = new Font("Segoe UI", 12f, FontStyle.Bold)
				};
				ListBox list = new ListBox
				{
					Dock = DockStyle.Fill,
					IntegralHeight = false,
					BorderStyle = BorderStyle.FixedSingle
				};
				foreach (string item in names)
				{
					list.Items.Add(item);
				}
				Label warning = new Label
				{
					Dock = DockStyle.Bottom,
					Height = 75,
					Padding = new Padding(12),
					ForeColor = Color.Firebrick
				};
				FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
				{
					Dock = DockStyle.Bottom,
					Height = 55,
					Padding = new Padding(8)
				};
				Button button = new Button
				{
					Text = "Remove selected DXF",
					AutoSize = true
				};
				Button button2 = new Button
				{
					Text = "Cancel",
					AutoSize = true,
					DialogResult = DialogResult.Cancel
				};
				flowLayoutPanel.Controls.Add(button);
				flowLayoutPanel.Controls.Add(button2);
				list.SelectedIndexChanged += delegate
				{
					int selectedIndex = list.SelectedIndex;
					warning.Text = ((selectedIndex < 0) ? "" : (affected[selectedIndex] + " estimate entry/entries use this DXF and will also be removed. Other files and entries will be kept. Save your project first if you need a backup."));
				};
				button.Click += delegate
				{
					if (list.SelectedIndex >= 0 && MessageBox.Show(f, "Remove this DXF and its " + affected[list.SelectedIndex] + " linked estimate entry/entries?", "Remove source", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
					{
						f.DialogResult = DialogResult.OK;
					}
				};
				f.Controls.Add(list);
				f.Controls.Add(warning);
				f.Controls.Add(flowLayoutPanel);
				f.Controls.Add(value);
				f.CancelButton = button2;
				list.SelectedIndex = 0;
				PowTheme.Apply(f);
				return (f.ShowDialog(owner) == DialogResult.OK) ? list.SelectedIndex : (-1);
			}
			finally
			{
				if (f != null)
				{
					((IDisposable)f).Dispose();
				}
			}
		}
	}
}
