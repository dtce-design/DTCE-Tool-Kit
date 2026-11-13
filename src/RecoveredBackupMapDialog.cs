using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal sealed class BackupMapDialog : Form
	{
		public BackupMapDialog(List<BackupDemand> demands, List<BackupTarget> targets, Dictionary<string, BackupTarget> routes)
		{
			BackupMapDialog backupMapDialog = this;
			AppBrand.Apply(this);
			Text = "Map required quantities to BACKUP items";
			base.ClientSize = new Size(1250, 700);
			MinimumSize = new Size(900, 500);
			base.StartPosition = FormStartPosition.CenterParent;
			Font = new Font("Segoe UI", 9f);
			Label value = new Label
			{
				Dock = DockStyle.Top,
				Height = 76,
				Padding = new Padding(12),
				Text = "Choose the exact pay item/specification for each required quantity. Only final quantities are written to the existing green boxes. Tool Kit never inserts, deletes or moves BACKUP rows. Print detailed computations from Tool Kit Reports. Existing detail rows can remain hidden. An LS item can also receive measurement-basis rows, but it requires a separate manually entered LS quantity. Aggregate overview totals are excluded to avoid counting the same material twice. Mappings are saved as named ranges in this workbook."
			};
			DataGridView grid = new DataGridView
			{
				Dock = DockStyle.Fill,
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = false,
				AllowUserToResizeRows = false,
				RowHeadersVisible = false,
				AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
				ColumnHeadersHeight = 45,
				BackgroundColor = Color.White
			};
			grid.Columns.Add("Material", "Required quantity / material");
			grid.Columns.Add("Quantity", "Quantity");
			grid.Columns.Add("Unit", "Unit");
			grid.Columns.Add(new DataGridViewComboBoxColumn
			{
				Name = "Target",
				HeaderText = "BACKUP pay item / specification / green total cell",
				DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
				Width = 790
			});
			grid.Columns[0].Width = 300;
			grid.Columns[1].Width = 100;
			grid.Columns[2].Width = 65;
			for (int i = 0; i < 3; i++)
			{
				grid.Columns[i].ReadOnly = true;
			}
			foreach (DataGridViewColumn column in grid.Columns)
			{
				column.SortMode = DataGridViewColumnSortMode.NotSortable;
			}
			foreach (BackupDemand d in demands)
			{
				int index = grid.Rows.Add(d.Description, d.Quantity.ToString("0.########"), d.Unit);
				DataGridViewComboBoxCell dataGridViewComboBoxCell = (DataGridViewComboBoxCell)grid.Rows[index].Cells[3];
				dataGridViewComboBoxCell.DisplayMember = "Caption";
				dataGridViewComboBoxCell.ValueMember = "Row";
				dataGridViewComboBoxCell.Items.AddRange(targets.Where((BackupTarget t) => BackupMapping.Unit(t.Unit) == BackupMapping.Unit(d.Unit) || BackupMapping.Unit(t.Unit) == "ls").Cast<object>().ToArray());
				BackupTarget value2;
				if (routes.TryGetValue(d.Key, out value2))
				{
					dataGridViewComboBoxCell.Value = value2.Row;
				}
				grid.Rows[index].Tag = d;
				if (d.TargetSlotId != null)
				{
					dataGridViewComboBoxCell.ReadOnly = true;
				}
			}
			grid.DataError += delegate(object s, DataGridViewDataErrorEventArgs e)
			{
				e.ThrowException = false;
			};
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Bottom,
				Height = 50,
				Padding = new Padding(8),
				FlowDirection = FlowDirection.RightToLeft
			};
			Button button = new Button
			{
				Text = "Apply mapping",
				Width = 150,
				Height = 32
			};
			Button button2 = new Button
			{
				Text = "Cancel",
				Width = 110,
				Height = 32,
				DialogResult = DialogResult.Cancel
			};
			flowLayoutPanel.Controls.Add(button);
			flowLayoutPanel.Controls.Add(button2);
			button.Click += delegate
			{
				try
				{
					grid.EndEdit();
					Dictionary<string, BackupTarget> dictionary = new Dictionary<string, BackupTarget>();
					foreach (DataGridViewRow item in (IEnumerable)grid.Rows)
					{
						BackupDemand backupDemand = (BackupDemand)item.Tag;
						if (item.Cells[3].Value == null)
						{
							throw new Exception("Select a BACKUP item for " + backupDemand.Description);
						}
						int id = Convert.ToInt32(item.Cells[3].Value);
						dictionary[backupDemand.Key] = targets.Single((BackupTarget t) => t.Row == id);
					}
					BackupMapping.Validate(demands, dictionary);
					routes.Clear();
					foreach (KeyValuePair<string, BackupTarget> item2 in dictionary)
					{
						routes.Add(item2.Key, item2.Value);
					}
					backupMapDialog.DialogResult = DialogResult.OK;
				}
				catch (Exception ex)
				{
					MessageBox.Show(backupMapDialog, ex.Message, "BACKUP mapping");
				}
			};
			base.Controls.Add(grid);
			base.Controls.Add(value);
			base.Controls.Add(flowLayoutPanel);
			base.CancelButton = button2;
		}
	}
}
