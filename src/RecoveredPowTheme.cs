using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace BeamScheduleFormatter {
internal static class PowTheme {
 static readonly ConditionalWeakTable<Control,object> themed=new ConditionalWeakTable<Control,object>();
 public static readonly Color Ink=Color.FromArgb(24,43,62);
 public static readonly Color Teal=PremiumControls.Accent;
 public static readonly Color Background=Color.FromArgb(246,248,251);
 public static readonly Color Line=Color.FromArgb(222,230,238);
 public static readonly Color Muted=Color.FromArgb(83,102,122);
 public static void Apply(Control root) {
  if(root==null||root.IsDisposed)return;
  object seen;if(themed.TryGetValue(root,out seen))return;
  themed.Add(root,new object());root.SuspendLayout();
  try {
   var form=root as Form;
   if(form!=null){form.BackColor=Background;form.ForeColor=Ink;ModernUi.Buffer(form);}
   var page=root as TabPage;
   if(page!=null){page.UseVisualStyleBackColor=false;page.BackColor=Background;}
   var flow=root as FlowLayoutPanel;
   if(flow!=null&&flow.Dock==DockStyle.Top&&flow.FlowDirection==FlowDirection.LeftToRight){
    flow.AutoSize=true;flow.AutoSizeMode=AutoSizeMode.GrowAndShrink;flow.WrapContents=true;
    flow.MinimumSize=new Size(0,40);flow.Padding=new Padding(12,4,12,4);flow.BackColor=Color.White;
   }
   var tab=root as TabControl;if(tab!=null)StyleTabs(tab);
   var button=root as Button;if(button!=null&&!(button is ToolModuleButton))PremiumControls.StyleButton(button);
   var grid=root as DataGridView;if(grid!=null)StyleGrid(grid);
   var text=root as TextBox;if(text!=null){text.BorderStyle=BorderStyle.FixedSingle;
    // Report editors retain their fixed-width typography and custom foregrounds.
    if(!text.ReadOnly){text.BackColor=ModernUi.Input;text.ForeColor=Ink;}else if(text.BackColor==SystemColors.Control)text.BackColor=ModernUi.Soft;}
   var numeric=root as NumericUpDown;if(numeric!=null){numeric.BorderStyle=BorderStyle.FixedSingle;numeric.BackColor=ModernUi.Input;numeric.ForeColor=Ink;}
   var combo=root as ComboBox;if(combo!=null){combo.FlatStyle=FlatStyle.Standard;combo.BackColor=ModernUi.Input;combo.ForeColor=Ink;}
   var group=root as GroupBox;if(group!=null)group.ForeColor=Ink;
   var label=root as Label;
   if(label!=null&&label.Dock==DockStyle.Top&&label.Image==null)FitGuide(label);
   var strip=root as ToolStrip;if(strip!=null){strip.RenderMode=ToolStripRenderMode.Professional;strip.GripStyle=ToolStripGripStyle.Hidden;strip.BackColor=Color.White;strip.ForeColor=Ink;}
   foreach(Control child in root.Controls)Apply(child);
   root.ControlAdded+=delegate(object s,ControlEventArgs e){Apply(e.Control);};
  } finally {root.ResumeLayout(true);}
 }
 static void StyleTabs(TabControl tab) {
  ModernUi.Navigation(tab);tab.Font=ModernUi.Strong;tab.DrawMode=TabDrawMode.OwnerDrawFixed;tab.ShowToolTips=true;ModernUi.Buffer(tab);
  // Native variable-width tabs measure the complete caption and wrap onto more rows.
  // No fixed tab widths, abbreviated titles, or ellipsis drawing.
  tab.DrawItem+=delegate(object sender,DrawItemEventArgs e){
   if(e.Index<0||e.Index>=tab.TabCount)return;bool selected=e.Index==tab.SelectedIndex;Rectangle r=e.Bounds;
   using(var brush=new SolidBrush(Background))e.Graphics.FillRectangle(brush,r);
   var face=Rectangle.Inflate(r,-2,-3);
   if(selected&&face.Width>0&&face.Height>0)using(var path=ModernUi.Rounded(face,6))using(var fill=new SolidBrush(PremiumControls.AccentSoft))e.Graphics.FillPath(fill,path);
   TextRenderer.DrawText(e.Graphics,tab.TabPages[e.Index].Text,tab.Font,Rectangle.Inflate(r,-8,-3),selected?Teal:Muted,
    TextFormatFlags.NoPrefix|TextFormatFlags.VerticalCenter|TextFormatFlags.HorizontalCenter|TextFormatFlags.SingleLine);
   if(tab.Focused&&selected)ControlPaint.DrawFocusRectangle(e.Graphics,Rectangle.Inflate(r,-5,-5),Teal,Background);
  };
 }
 static void StyleGrid(DataGridView grid) {
  GridAppearance.Apply(grid);ModernUi.Buffer(grid);
  grid.EnableHeadersVisualStyles=false;grid.BackgroundColor=Color.White;grid.BorderStyle=BorderStyle.None;grid.GridColor=Line;
  grid.CellBorderStyle=DataGridViewCellBorderStyle.SingleHorizontal;grid.ColumnHeadersBorderStyle=DataGridViewHeaderBorderStyle.None;
  grid.ColumnHeadersDefaultCellStyle.BackColor=PremiumControls.Header;grid.ColumnHeadersDefaultCellStyle.ForeColor=Muted;
  grid.ColumnHeadersDefaultCellStyle.SelectionBackColor=PremiumControls.Header;grid.ColumnHeadersDefaultCellStyle.SelectionForeColor=Ink;
  grid.ColumnHeadersDefaultCellStyle.Font=ModernUi.Strong;grid.ColumnHeadersDefaultCellStyle.Padding=new Padding(10,9,10,9);
  grid.ColumnHeadersDefaultCellStyle.WrapMode=DataGridViewTriState.True;
  grid.ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.AutoSize;
  grid.DefaultCellStyle.Font=ModernUi.Body;grid.DefaultCellStyle.ForeColor=Ink;grid.DefaultCellStyle.Padding=new Padding(10,5,10,5);
  grid.DefaultCellStyle.SelectionBackColor=ModernUi.Selection;grid.DefaultCellStyle.SelectionForeColor=Ink;
  grid.RowHeadersDefaultCellStyle.BackColor=ModernUi.Soft;grid.RowHeadersDefaultCellStyle.SelectionBackColor=ModernUi.Selection;
  grid.RowTemplate.MinimumHeight=32;grid.RowTemplate.Height=32;grid.AllowUserToResizeRows=false;grid.AllowUserToOrderColumns=false;
  grid.AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;grid.DefaultCellStyle.WrapMode=DataGridViewTriState.True;
  grid.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(252,253,255);
  foreach(DataGridViewColumn col in grid.Columns)StyleColumn(col);
  grid.ColumnAdded+=delegate(object s,DataGridViewColumnEventArgs e){StyleColumn(e.Column);};
 }
 static void StyleColumn(DataGridViewColumn col){col.SortMode=DataGridViewColumnSortMode.NotSortable;}
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
