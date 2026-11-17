using System;
using System.Drawing;
using System.Linq;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace BeamScheduleFormatter {
internal static partial class PowTheme {
 static readonly ConditionalWeakTable<Control,object> themed=new ConditionalWeakTable<Control,object>();
 public static Color Ink {get{return DesktopAppearance.Palette.Ink;}}
 public static Color Teal {get{return DesktopAppearance.Palette.Link;}}
 public static Color Background {get{return DesktopAppearance.Palette.Background;}}
 public static Color Line {get{return DesktopAppearance.Palette.Line;}}
 public static Color Muted {get{return DesktopAppearance.Palette.Muted;}}
 public static void Apply(Control root) {
  if(root==null||root.IsDisposed)return;
  object seen;if(themed.TryGetValue(root,out seen))return;
  themed.Add(root,new object());root.SuspendLayout();
  try {
   var form=root as Form;
   if(form!=null){form.BackColor=Background;form.ForeColor=Ink;ModernUi.Buffer(form);}
   var surface=root as Panel;if(surface!=null&&!(root is Form)&&(surface.BackColor==Color.White||surface.BackColor==SystemColors.Control))surface.BackColor=ModernUi.Surface;
   var page=root as TabPage;
   if(page!=null){page.UseVisualStyleBackColor=false;page.BackColor=Background;}
   var flow=root as FlowLayoutPanel;
   if(flow!=null&&flow.Dock==DockStyle.Top&&flow.FlowDirection==FlowDirection.LeftToRight){
    flow.AutoSize=true;flow.AutoSizeMode=AutoSizeMode.GrowAndShrink;flow.WrapContents=true;
    flow.MinimumSize=new Size(0,40);flow.Padding=new Padding(12,4,12,4);flow.BackColor=ModernUi.Surface;
   }
   var tab=root as TabControl;if(tab!=null)StyleTabs(tab);
   var button=root as Button;if(button!=null&&!(button is ToolModuleButton))PremiumControls.StyleButton(button);
   var grid=root as DataGridView;if(grid!=null)StyleGrid(grid);
   var text=root as TextBox;if(text!=null){text.BorderStyle=BorderStyle.FixedSingle;
    // Report editors retain their fixed-width typography and custom foregrounds.
    text.BackColor=text.ReadOnly?ModernUi.Soft:ModernUi.Input;text.ForeColor=Ink;}
   var numeric=root as NumericUpDown;if(numeric!=null){numeric.BorderStyle=BorderStyle.FixedSingle;numeric.BackColor=ModernUi.Input;numeric.ForeColor=Ink;}
   var combo=root as ComboBox;if(combo!=null){combo.FlatStyle=FlatStyle.Standard;DropdownChrome.Attach(combo);combo.BackColor=ModernUi.Input;combo.ForeColor=Ink;}
   var list=root as ListBox;if(list!=null){list.BackColor=ModernUi.Input;list.ForeColor=Ink;}
   var tree=root as TreeView;if(tree!=null){tree.BackColor=ModernUi.Surface;tree.ForeColor=Ink;tree.LineColor=Line;}
   var group=root as GroupBox;if(group!=null)group.ForeColor=Ink;
   var label=root as Label;
   if(label!=null){if(label.ForeColor==Color.Black||label.ForeColor==SystemColors.ControlText||label.ForeColor==Color.FromArgb(28,43,61)||label.ForeColor==Color.FromArgb(36,36,36))label.ForeColor=Ink;if(label.BackColor==Color.White)label.BackColor=ModernUi.Surface;if(label.Dock==DockStyle.Top&&label.Image==null)FitGuide(label);}
   var strip=root as ToolStrip;if(strip!=null){strip.RenderMode=ToolStripRenderMode.Professional;strip.GripStyle=ToolStripGripStyle.Hidden;strip.BackColor=ModernUi.Surface;strip.ForeColor=Ink;}
   ApplyAppearanceRole(root);
   // Do not measure/style entire unvisited worksheets during workspace startup.
   if(tab!=null){PrepareSelectedTab(tab);tab.SelectedIndexChanged+=(s,e)=>PrepareSelectedTab(tab);}
   else foreach(Control child in root.Controls)Apply(child);
   root.ControlAdded+=delegate(object s,ControlEventArgs e){if(tab==null||e.Control==tab.SelectedTab)Apply(e.Control);};
  } finally {root.ResumeLayout(true);}
 }
 internal static bool IsPrepared(Control control){object marker;return themed.TryGetValue(control,out marker);}
 static int NavigationDepth(TabControl tab){int depth=0;for(Control parent=tab.Parent;parent!=null;parent=parent.Parent)if(parent is TabControl)depth++;return depth;}
 static void PrepareSelectedTab(TabControl tab){if(tab.SelectedTab==null)return;Apply(tab.SelectedTab);InputFocus.Apply(tab.SelectedTab);}
 static void StyleTabs(TabControl tab) {
  ModernUi.Navigation(tab);tab.Font=NavigationDepth(tab)==0?ModernUi.Strong:ModernUi.Body;tab.DrawMode=TabDrawMode.OwnerDrawFixed;tab.ShowToolTips=true;ModernUi.Buffer(tab);
  // Native variable-width tabs measure the complete caption and wrap onto more rows.
  // No fixed tab widths, abbreviated titles, or ellipsis drawing.
  tab.DrawItem+=(sender,e)=>FolderTabPainter.Draw(tab,e.Graphics,e.Index,e.Bounds);
 }
 static void StyleGrid(DataGridView grid) {
  GridAppearance.Apply(grid);ModernUi.Buffer(grid);
  grid.EnableHeadersVisualStyles=false;grid.BackgroundColor=ModernUi.Surface;grid.BorderStyle=BorderStyle.None;grid.GridColor=Line;
  grid.CellBorderStyle=DataGridViewCellBorderStyle.Single;grid.ColumnHeadersBorderStyle=DataGridViewHeaderBorderStyle.Single;
  grid.ColumnHeadersDefaultCellStyle.BackColor=PremiumControls.Header;grid.ColumnHeadersDefaultCellStyle.ForeColor=Ink;
  grid.ColumnHeadersDefaultCellStyle.SelectionBackColor=PremiumControls.Header;grid.ColumnHeadersDefaultCellStyle.SelectionForeColor=Ink;
  grid.ColumnHeadersDefaultCellStyle.Font=ModernUi.Strong;grid.ColumnHeadersDefaultCellStyle.Padding=new Padding(10,8,10,8);grid.ColumnHeadersDefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleLeft;
  grid.ColumnHeadersDefaultCellStyle.WrapMode=DataGridViewTriState.True;
  grid.ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.AutoSize;
  grid.DefaultCellStyle.Font=ModernUi.Body;grid.DefaultCellStyle.ForeColor=Ink;grid.DefaultCellStyle.Padding=new Padding(10,5,10,5);
  grid.DefaultCellStyle.SelectionBackColor=ModernUi.Selection;grid.DefaultCellStyle.SelectionForeColor=Ink;
  grid.RowHeadersDefaultCellStyle.BackColor=ModernUi.Soft;grid.RowHeadersDefaultCellStyle.SelectionBackColor=ModernUi.Selection;
  grid.RowTemplate.MinimumHeight=36;grid.RowTemplate.Height=36;grid.AllowUserToResizeRows=false;grid.AllowUserToOrderColumns=false;
  grid.AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;grid.DefaultCellStyle.WrapMode=DataGridViewTriState.True;
  grid.AlternatingRowsDefaultCellStyle.BackColor=ModernUi.Surface;
  foreach(DataGridViewColumn col in grid.Columns)StyleColumn(col);
  grid.CellFormatting+=delegate(object sender,DataGridViewCellFormattingEventArgs e){if(e.RowIndex<0)return;var style=e.CellStyle;if(DesktopAppearance.Palette.Dark&&style.BackColor.GetBrightness()>.65f)style.BackColor=grid.Rows[e.RowIndex].Cells[e.ColumnIndex].ReadOnly?ModernUi.Surface:ModernUi.Input;if(DesktopAppearance.Palette.Dark&&style.ForeColor.GetBrightness()<.35f)style.ForeColor=Ink;if(!DesktopAppearance.Palette.Dark&&style.BackColor.GetBrightness()<.28f&&style.BackColor.GetSaturation()<.25f){style.BackColor=grid.Rows[e.RowIndex].Cells[e.ColumnIndex].ReadOnly?ModernUi.Surface:ModernUi.Input;style.ForeColor=Ink;}if(!DesktopAppearance.Palette.Dark&&style.BackColor.GetBrightness()>.65f&&style.ForeColor.GetBrightness()>.7f)style.ForeColor=Ink;};
  grid.ColumnAdded+=delegate(object s,DataGridViewColumnEventArgs e){StyleColumn(e.Column);};
  ApplyCompact(grid);
 }
 static void StyleColumn(DataGridViewColumn col){col.HeaderCell.Style=new DataGridViewCellStyle{Font=ModernUi.Strong,ForeColor=Ink,BackColor=PremiumControls.Header,Alignment=DataGridViewContentAlignment.MiddleLeft,Padding=new Padding(10,8,10,8),WrapMode=DataGridViewTriState.True};col.MinimumWidth=Math.Max(col.MinimumWidth,ModernUi.HeaderMinimum(col.HeaderText));col.SortMode=DataGridViewColumnSortMode.NotSortable;col.DefaultCellStyle.BackColor=col.ReadOnly?ModernUi.Surface:ModernUi.Input;if(col.ReadOnly)col.DefaultCellStyle.ForeColor=Ink;if(col.Name=="EditWallOpeningsAction"){col.MinimumWidth=38;col.Width=42;col.AutoSizeMode=DataGridViewAutoSizeColumnMode.None;col.HeaderCell.Style.Padding=new Padding(0);}}

 static void FitGuide(Label label) {
  bool fitting=false;
  Action fit=delegate {
   if(fitting||label.IsDisposed)return;fitting=true;
   try {int width=Math.Max(100,label.ClientSize.Width-label.Padding.Horizontal-4);
    int measured=TextRenderer.MeasureText(label.Text,label.Font,new Size(width,int.MaxValue),TextFormatFlags.TextBoxControl|TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix).Height;
    int height=Math.Max(label.Font.Height+12,measured+label.Padding.Vertical+10);
    if(label.Height!=height)label.Height=height;
   } finally {fitting=false;}
  };
  label.Resize+=delegate{fit();};label.TextChanged+=delegate{fit();};label.FontChanged+=delegate{fit();};fit();
 }
}
}

