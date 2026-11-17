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
			allTargets = targets;
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
			/* Plain text cell + searchable picker (owner, 17 November 2026: the dialog was slow to open because every row
			   carried a dropdown loaded with every BACKUP item). The chosen BACKUP row number is kept in the cell Tag. */
			grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "Target",
				HeaderText = "BACKUP pay item / specification / green total cell (click to choose; Delete = leave unmapped)",
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
				DataGridViewCell dataGridViewComboBoxCell = grid.Rows[index].Cells[3];
				BackupTarget value2;
				SetTarget(dataGridViewComboBoxCell, routes.TryGetValue(d.Key, out value2) ? value2 : null);
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
			// Searchable picker instead of a long dropdown (owner, 17 November 2026). Click the pay item cell or press
			// Enter/F2/Space on it. With several rows selected, the chosen item is applied to every selected row with a
			// compatible unit.
			grid.EditMode = DataGridViewEditMode.EditProgrammatically;
			grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
			grid.MultiSelect = true;
			Action pick = delegate
			{
				DataGridViewRow anchor = grid.CurrentRow;
				if (anchor == null || anchor.Cells[3].ReadOnly) return;
				BackupDemand demand = (BackupDemand)anchor.Tag;
				var cell = anchor.Cells[3];
				var choices = Compatible(demand);
				BackupTarget current = cell.Tag == null ? null : choices.FirstOrDefault(t => t.Row == (int)cell.Tag);
				var rows = grid.SelectedRows.Cast<DataGridViewRow>().Where(r => !r.Cells[3].ReadOnly).ToList();
				if (!rows.Contains(anchor)) rows.Add(anchor);
				using (var picker = new BackupTargetPicker(rows.Count > 1 ? rows.Count + " selected quantities" : demand.Description, choices, current))
				{
					if (picker.ShowDialog(backupMapDialog) != DialogResult.OK || picker.Selected == null) return;
					int skipped = 0;
					foreach (DataGridViewRow row in rows)
					{
						if (Compatible((BackupDemand)row.Tag).Any(t => t.Row == picker.Selected.Row)) SetTarget(row.Cells[3], picker.Selected); else skipped++;
					}
					if (skipped > 0) MessageBox.Show(backupMapDialog, skipped + " selected row(s) have a different unit and were not changed.", "BACKUP mapping");
				}
			};
			grid.CellClick += delegate(object s, DataGridViewCellEventArgs e) { if (e.RowIndex >= 0 && e.ColumnIndex == 3) pick(); };
			grid.KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.F2 || e.KeyCode == Keys.Space) { pick(); e.Handled = true; } else if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back) { foreach (DataGridViewRow row in grid.SelectedRows) if (!row.Cells[3].ReadOnly) SetTarget(row.Cells[3], null); e.Handled = true; } };
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
						if (item.Cells[3].Tag == null)
						{
							continue; /* left unmapped: allowed (owner, 17 November 2026) */
						}
						int id = (int)item.Cells[3].Tag;
						dictionary[backupDemand.Key] = targets.Single((BackupTarget t) => t.Row == id);
					}
					/* Owner (17 November 2026): no confirmation list; incomplete or unselected rows simply stay unmapped (BOQ). */
					BackupMapping.DropIncomplete(demands, dictionary);
					BackupMapping.Validate(demands.Where((BackupDemand d) => dictionary.ContainsKey(d.Key)), dictionary);
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

		readonly Dictionary<string, List<BackupTarget>> compatible = new Dictionary<string, List<BackupTarget>>();
		List<BackupTarget> allTargets;
		List<BackupTarget> Compatible(BackupDemand d)
		{
			string unit = BackupMapping.Unit(d.Unit);
			List<BackupTarget> list;
			if (!compatible.TryGetValue(unit, out list))
			{
				list = allTargets.Where((BackupTarget t) => BackupMapping.Unit(t.Unit) == unit || BackupMapping.Unit(t.Unit) == "ls").ToList();
				compatible[unit] = list;
			}
			return list;
		}
		static void SetTarget(DataGridViewCell cell, BackupTarget t)
		{
			cell.Tag = (t == null) ? null : (object)t.Row;
			cell.Value = (t == null) ? "— not mapped —" : t.Caption;
			cell.Style.ForeColor = (t == null) ? Color.Gray : Color.Empty;
		}
	}
}
