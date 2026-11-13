using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BeamScheduleFormatter
{
	internal class LauncherForm : Form
	{
		private Form formatter;

		private Form slabFormatter;

		private Form lengths;

		private Form project;

		private Form works;

		private Form generator;

		private readonly Panel home = new Panel
		{
			Dock = DockStyle.Fill
		};

		private readonly Panel groupHost = new Panel
		{
			Dock = DockStyle.Fill,
			Visible = false
		};

		private readonly Panel groupContent = new Panel
		{
			Dock = DockStyle.Fill
		};

		private readonly Label groupTitle = new Label
		{
			AutoSize = true,
			Padding = new Padding(18, 8, 0, 0),
			Font = new Font("Segoe UI", 13f, FontStyle.Bold)
		};

		private readonly TabControl formatterTabs = new TabControl
		{
			Dock = DockStyle.Fill
		};

		private readonly TabControl designTabs = new TabControl
		{
			Dock = DockStyle.Fill
		};

		private readonly Dictionary<string, Form> designModules = new Dictionary<string, Form>();

		private readonly Dictionary<string, Action> designLoaders = new Dictionary<string, Action>();

		private bool loadingDesign;

		private static string VersionCaption
		{
			get
			{
				return "1.4.37";
			}
		}

		public LauncherForm()
		{
			AppBrand.Apply(this);
			Text = "DTCE Tool Kit v" + VersionCaption;
			base.ClientSize = new Size(1100, 800);
			MinimumSize = new Size(900, 650);
			Font = new Font("Segoe UI", 10f);
			base.StartPosition = FormStartPosition.CenterScreen;
			base.Controls.Add(home);
			base.Controls.Add(groupHost);
			home.Controls.Add(new PictureBox
			{
				Image = AppBrand.Logo,
				SizeMode = PictureBoxSizeMode.Zoom,
				Left = 28,
				Top = 18,
				Width = 76,
				Height = 76
			});
			home.Controls.Add(new Label
			{
				Text = "DTCE TOOL KIT",
				Left = 120,
				Top = 28,
				Width = 600,
				Height = 44,
				Font = new Font("Segoe UI", 23f, FontStyle.Bold),
				ForeColor = PowTheme.Ink
			});
			home.Controls.Add(new Label
			{
				Text = "Choose a workspace. Settings, estimates and design sheets stay inside their group.",
				Left = 30,
				Top = 106,
				Width = 1000,
				Height = 28,
				ForeColor = PowTheme.Muted
			});
			Label value = new Label
			{
				Text = "ADMIN EDITION",
				ForeColor = PowTheme.Teal,
				Left = base.ClientSize.Width - 190,
				Top = 45,
				Width = 160,
				Height = 25,
				Anchor = (AnchorStyles.Top | AnchorStyles.Right)
			};
			home.Controls.Add(value);
			FlowLayoutPanel cards = new FlowLayoutPanel
			{
				Left = 24,
				Top = 150,
				Width = base.ClientSize.Width - 48,
				Height = base.ClientSize.Height - 206,
				AutoScroll = true,
				Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right),
				Padding = new Padding(2)
			};
			home.Controls.Add(cards);
			AddCard(cards, "Formatters", "Beam and slab schedule formatters, grouped in one workspace.", delegate
			{
				ShowGroup(formatterTabs, "Formatters");
			});
			AddCard(cards, "Program of Works", "Concrete, masonry, roofing, steel and plumbing quantities with printable Tool Kit computation reports.", delegate
			{
				UseTool(ref works, () => new ProgramOfWorksForm(), "Program of Works", null, 2);
			});
			AddCard(cards, "Design Sheets", "Splice, Development & Hooks; Purlin Design; Truss Connections.", ShowDesignSheets);
			AddCard(cards, "Project Information", "Project details shared by calculations, schedules and reports.", delegate
			{
				UseTool(ref project, () => new ProjectInformationForm(), "Project Information", null, 8);
			});
			AddCard(cards, "Access Code Generator", "Generate and manage approved access to the tool kit.", delegate
			{
				UseTool(ref generator, () => new CodeGeneratorForm(), "Access Code Generator", null, 8);
			});
			cards.Resize += delegate
			{
				int width = Math.Max(280, (cards.ClientSize.Width - 36) / 2);
				foreach (Control control in cards.Controls)
				{
					control.Width = width;
				}
			};
			foreach (Control control2 in cards.Controls)
			{
				control2.Width = Math.Max(280, (cards.ClientSize.Width - 36) / 2);
			}
			home.Controls.Add(new Label
			{
				Text = "v" + VersionCaption + " | Developed by Engr. Daryl O. Tomaneng",
				Dock = DockStyle.Bottom,
				Height = 38,
				Padding = new Padding(26, 9, 0, 0),
				ForeColor = PowTheme.Muted,
				BackColor = Color.White
			});
			FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				Height = 58,
				Padding = new Padding(10),
				BackColor = Color.White
			};
			Button button = new Button
			{
				Text = "Back to Start",
				Width = 130,
				Height = 34
			};
			button.Click += delegate
			{
				ShowStart();
			};
			flowLayoutPanel.Controls.Add(button);
			flowLayoutPanel.Controls.Add(groupTitle);
			groupHost.Controls.Add(groupContent);
			groupHost.Controls.Add(flowLayoutPanel);
			AddEntry(formatterTabs, "Beam Formatter", "Open Beam Formatter", delegate
			{
				UseTool(ref formatter, CreateBeam, "Beam Formatter", formatterTabs, 1);
			});
			AddEntry(formatterTabs, "Slab Formatter", "Open Slab Formatter", delegate
			{
				UseTool(ref slabFormatter, () => new SlabFormatterForm(), "Slab Formatter", formatterTabs, 64);
			});
			AddDesignEntry("Splice, Development & Hooks", delegate
			{
				UseTool(ref lengths, () => new RebarLengthForm(), "Splice, Development & Hooks", designTabs, 4);
			});
			AddDesignEntry("Purlin Design", delegate
			{
				OpenDesign("Purlin Design", () => new PurlinDesignForm());
			});
			AddDesignEntry("Truss Connections", delegate
			{
				OpenDesign("Truss Connections", () => new TrussConnectionForm());
			});
			designTabs.SelectedIndexChanged += delegate
			{
				if (groupHost.Visible && !loadingDesign)
				{
					LoadSelectedDesign();
				}
			};
			base.FormClosing += delegate(object s, FormClosingEventArgs e)
			{
				List<Form> list = (from f in new Form[5] { formatter, slabFormatter, lengths, project, works }.Concat(designModules.Values)
					where f != null && !f.IsDisposed
					select f).Distinct().ToList();
				if (generator != null && !generator.IsDisposed)
				{
					list.Add(generator);
				}
				foreach (Form item in list)
				{
					item.Close();
					if (!item.IsDisposed)
					{
						e.Cancel = true;
						break;
					}
				}
			};
			new StoreUpdateMonitor(this);
			ShowStart();
		}

		private Form CreateBeam()
		{
			return new MainForm();
		}

		private bool Authorize(int app)
		{
			if (!AccessCodes.CanUseFormatter)
			{
				using (LoginForm loginForm = new LoginForm())
				{
					if (loginForm.ShowDialog(this) != DialogResult.OK)
					{
						return false;
					}
				}
			}
			if (!AccessCodes.CanUseApp(app))
			{
				MessageBox.Show(this, "This workspace is not included in your approved access.", "App access");
				return false;
			}
			return true;
		}

		private void UseTool(ref Form form, Func<Form> create, string title, TabControl tabs, int app)
		{
			try
			{
				if (Authorize(app))
				{
					if (form == null || form.IsDisposed)
					{
						form = create();
					}
					ShowTool(form, title, tabs);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.Message, "DTCE Tool Kit");
			}
		}

		private void OpenDesign(string title, Func<Form> create)
		{
			try
			{
				if (Authorize((title == "Purlin Design") ? 16 : 32))
				{
					Form value;
					if (!designModules.TryGetValue(title, out value) || value.IsDisposed)
					{
						value = create();
						designModules[title] = value;
					}
					ShowTool(value, title, designTabs);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.Message, "Design Sheet");
			}
		}

		private void ShowTool(Form form, string title, TabControl tabs)
		{
			if (form.TopLevel)
			{
				form.TopLevel = false;
				form.FormBorderStyle = FormBorderStyle.None;
			}
			form.MinimumSize = Size.Empty;
			form.MaximumSize = Size.Empty;
			form.Dock = DockStyle.Fill;
			if (tabs == null)
			{
				ShowGroup(form, title);
				form.Show();
				return;
			}
			TabPage tabPage = tabs.TabPages.Cast<TabPage>().FirstOrDefault((TabPage p) => p.Text == title);
			if (tabPage == null)
			{
				tabPage = new TabPage(title);
				tabs.TabPages.Add(tabPage);
			}
			if (tabPage.Tag != form)
			{
				tabPage.Controls.Clear();
				tabPage.Tag = form;
				tabPage.Controls.Add(form);
			}
			tabs.SelectedTab = tabPage;
			ShowGroup(tabs, (tabs == formatterTabs) ? "Formatters" : "Design Sheets");
			form.Show();
			form.BringToFront();
		}

		private void ShowGroup(Control content, string title)
		{
			groupContent.Controls.Clear();
			content.Dock = DockStyle.Fill;
			groupContent.Controls.Add(content);
			groupTitle.Text = title;
			home.Visible = false;
			groupHost.Visible = true;
			groupHost.BringToFront();
			content.Visible = true;
			PowTheme.Apply(groupHost);
		}

		internal void ShowStart()
		{
			groupHost.Visible = false;
			home.Visible = true;
			home.BringToFront();
		}

		internal void ShowDesignSheets()
		{
			if (!AccessCodes.CanUseFormatter)
			{
				using (LoginForm loginForm = new LoginForm())
				{
					if (loginForm.ShowDialog(this) != DialogResult.OK)
					{
						return;
					}
				}
			}
			List<TabPage> list = (from TabPage p in designTabs.TabPages
				where AccessCodes.CanUseApp(DesignProgram(p.Text))
				select p).ToList();
			if (list.Count == 0)
			{
				MessageBox.Show(this, "No Design Sheet is included in your approved access.", "App access");
				return;
			}
			loadingDesign = true;
			try
			{
				if (!list.Contains(designTabs.SelectedTab))
				{
					designTabs.SelectedTab = list[0];
				}
				ShowGroup(designTabs, "Design Sheets");
			}
			finally
			{
				loadingDesign = false;
			}
			LoadSelectedDesign();
		}

		private static int DesignProgram(string title)
		{
			if (!(title == "Purlin Design"))
			{
				if (!(title == "Truss Connections"))
				{
					return 4;
				}
				return 32;
			}
			return 16;
		}

		private void LoadSelectedDesign()
		{
			if (loadingDesign || designTabs.SelectedTab == null)
			{
				return;
			}
			foreach (Form item in designTabs.SelectedTab.Controls.OfType<Form>())
			{
				item.Hide();
			}
			Action value;
			if (!designLoaders.TryGetValue(designTabs.SelectedTab.Text, out value))
			{
				return;
			}
			loadingDesign = true;
			try
			{
				value();
			}
			finally
			{
				loadingDesign = false;
			}
		}

		private void AddDesignEntry(string title, Action open)
		{
			designLoaders.Add(title, open);
			TabPage tabPage = new TabPage(title);
			tabPage.Controls.Add(new Label
			{
				Text = "This design sheet requires its own program access.",
				Dock = DockStyle.Top,
				Height = 60,
				Padding = new Padding(20)
			});
			designTabs.TabPages.Add(tabPage);
		}

		private void AddCard(FlowLayoutPanel panel, string title, string description, Action action)
		{
			ToolModuleButton toolModuleButton = new ToolModuleButton
			{
				Text = title,
				Description = description,
				Height = 138,
				Width = 470,
				Margin = new Padding(0, 0, 14, 14)
			};
			toolModuleButton.Click += delegate
			{
				action();
			};
			panel.Controls.Add(toolModuleButton);
		}

		private void AddEntry(TabControl tabs, string title, string buttonText, Action action)
		{
			TabPage tabPage = new TabPage(title);
			Button button = new Button
			{
				Text = buttonText,
				Left = 24,
				Top = 28,
				Width = 300,
				Height = 42
			};
			button.Click += delegate
			{
				action();
			};
			tabPage.Controls.Add(button);
			tabs.TabPages.Add(tabPage);
		}
	}
}
