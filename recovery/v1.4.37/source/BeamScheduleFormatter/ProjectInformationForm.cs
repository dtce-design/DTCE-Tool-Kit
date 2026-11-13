using System;
using System.Drawing;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal class ProjectInformationForm : Form
	{
		public ProjectInformationForm()
		{
			AppBrand.Apply(this);
			if (!AccessCodes.CanUseApp(8))
			{
				throw new Exception("This app is not included in your current access.");
			}
			base.Shown += delegate
			{
				InputFocus.Apply(this);
			};
			Text = "Project Information";
			base.ClientSize = new Size(720, 520);
			MinimumSize = new Size(736, 559);
			base.StartPosition = FormStartPosition.CenterParent;
			Font = new Font("Segoe UI", 10f);
			BackColor = Color.FromArgb(243, 246, 250);
			TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				Padding = new Padding(24),
				ColumnCount = 1,
				RowCount = 11
			};
			tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f));
			tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 25f));
			tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35f));
			tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 25f));
			tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35f));
			for (int num = 0; num < 2; num++)
			{
				tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 25f));
				tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35f));
			}
			tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
			tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));
			tableLayoutPanel.Controls.Add(new Label
			{
				Text = "PROJECT INFORMATION",
				Font = new Font("Segoe UI", 16f, FontStyle.Bold),
				AutoSize = true
			});
			tableLayoutPanel.Controls.Add(new Label
			{
				Text = "Project Name",
				AutoSize = true
			});
			TextBox name = new TextBox
			{
				Dock = DockStyle.Fill,
				MaxLength = 100
			};
			tableLayoutPanel.Controls.Add(name);
			tableLayoutPanel.Controls.Add(new Label
			{
				Text = "Project Location",
				AutoSize = true
			});
			TextBox location = new TextBox
			{
				Dock = DockStyle.Fill,
				MaxLength = 100
			};
			tableLayoutPanel.Controls.Add(location);
			tableLayoutPanel.Controls.Add(new Label
			{
				Text = "Implementing Office",
				AutoSize = true
			});
			TextBox office = new TextBox
			{
				Dock = DockStyle.Fill,
				MaxLength = 200
			};
			tableLayoutPanel.Controls.Add(office);
			tableLayoutPanel.Controls.Add(new Label
			{
				Text = "Office Address",
				AutoSize = true
			});
			TextBox address = new TextBox
			{
				Dock = DockStyle.Fill,
				MaxLength = 200
			};
			tableLayoutPanel.Controls.Add(address);
			Label note = new Label
			{
				Text = "Saved information applies to every page of new calculation PDFs and print previews in both calculation apps. Save before exporting. Existing PDFs keep their original headers.",
				Dock = DockStyle.Fill
			};
			tableLayoutPanel.Controls.Add(note);
			Button button = new Button
			{
				Text = "Save project information",
				Dock = DockStyle.Fill,
				BackColor = Color.White
			};
			tableLayoutPanel.Controls.Add(button);
			base.Controls.Add(tableLayoutPanel);
			ProjectInformation projectInformation = ProjectInfoStore.Read();
			name.Text = projectInformation.Name;
			location.Text = projectInformation.Location;
			office.Text = projectInformation.Office;
			address.Text = projectInformation.OfficeAddress;
			button.Click += delegate
			{
				try
				{
					ProjectInfoStore.Save(name.Text, location.Text, office.Text, address.Text);
					note.Text = "Saved. New PDFs and print previews will use this project name and location.";
				}
				catch (Exception ex)
				{
					MessageBox.Show(this, ex.Message, "Project Information");
				}
			};
			base.AcceptButton = button;
		}
	}
}
