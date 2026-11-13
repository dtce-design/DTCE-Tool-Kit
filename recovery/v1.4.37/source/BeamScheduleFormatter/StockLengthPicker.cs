using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal static class StockLengthPicker
	{
		public static List<double> Choose(IWin32Window owner, IEnumerable<double> current)
		{
			Form f = new Form
			{
				Text = "Available commercial rebar lengths",
				ClientSize = new Size(430, 370),
				StartPosition = FormStartPosition.CenterParent,
				FormBorderStyle = FormBorderStyle.FixedDialog,
				MaximizeBox = false,
				MinimizeBox = false,
				Font = new Font("Segoe UI", 10f)
			};
			try
			{
				AppBrand.Apply(f);
				f.Controls.Add(new Label
				{
					Text = "Check only the lengths available from your supplier.\nThe estimator compares these choices for least offcut waste.",
					Location = new Point(20, 15),
					Size = new Size(390, 60)
				});
				CheckedListBox list = new CheckedListBox
				{
					Location = new Point(20, 85),
					Size = new Size(390, 190),
					CheckOnClick = true,
					Font = new Font("Segoe UI", 12f)
				};
				double[] lengths = CommercialBars.Lengths;
				for (int i = 0; i < lengths.Length; i++)
				{
					double value = lengths[i];
					list.Items.Add(value.ToString("0.00") + " m", current.Contains(value));
				}
				f.Controls.Add(list);
				Button button = new Button
				{
					Text = "Apply",
					Location = new Point(285, 300),
					Size = new Size(125, 40)
				};
				List<double> selected = null;
				button.Click += delegate
				{
					selected = (from n in Enumerable.Range(0, list.Items.Count)
						where list.GetItemChecked(n)
						select CommercialBars.Lengths[n]).ToList();
					if (selected.Count == 0)
					{
						MessageBox.Show(f, "Select at least one commercial length.");
					}
					else
					{
						f.DialogResult = DialogResult.OK;
						f.Close();
					}
				};
				Button button2 = new Button
				{
					Text = "Cancel",
					Location = new Point(150, 300),
					Size = new Size(125, 40),
					DialogResult = DialogResult.Cancel
				};
				f.Controls.Add(button);
				f.Controls.Add(button2);
				f.AcceptButton = button;
				f.CancelButton = button2;
				return (f.ShowDialog(owner) == DialogResult.OK) ? selected : null;
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
