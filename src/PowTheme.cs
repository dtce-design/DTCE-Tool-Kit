using System;using System.Drawing;using System.Windows.Forms;using System.Runtime.CompilerServices;
namespace BeamScheduleFormatter {
class PowCard:Panel {
 public PowCard(){DoubleBuffered=true;BackColor=Color.White;Padding=new Padding(16);Margin=new Padding(0,0,16,16);}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(var p=new Pen(PowTheme.Line))e.Graphics.DrawRectangle(p,0,0,Width-1,Height-1);}
}
static class PowTheme {
 static readonly ConditionalWeakTable<Control,object> themed=new ConditionalWeakTable<Control,object>();
 public static readonly Color Ink=Color.FromArgb(25,43,63),Teal=Color.FromArgb(0,112,120),Background=Color.FromArgb(242,246,250),Line=Color.FromArgb(216,225,234),Muted=Color.FromArgb(88,107,126);
 public static void Apply(Control root){
  object seen;if(themed.TryGetValue(root,out seen)){foreach(Control child in root.Controls)Apply(child);return;}themed.Add(root,new object());
  if(root is Form||root is TabPage)root.BackColor=Background;
  var flow=root as FlowLayoutPanel;if(flow!=null&&flow.Dock==DockStyle.Top)flow.AutoScroll=true;
  var tab=root as TabControl;if(tab!=null){tab.Font=new Font("Segoe UI",9.5f);tab.Padding=new Point(16,10);tab.DrawMode=TabDrawMode.OwnerDrawFixed;tab.DrawItem+=(s,e)=>{if(e.Index<0||e.Index>=tab.TabCount)return;var rect=e.Bounds;bool selected=e.Index==tab.SelectedIndex;using(var brush=new SolidBrush(selected?Color.White:Background))e.Graphics.FillRectangle(brush,rect);TextRenderer.DrawText(e.Graphics,tab.TabPages[e.Index].Text,tab.Font,rect,selected?Teal:Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix);if(selected)using(var p=new Pen(Teal,3))e.Graphics.DrawLine(p,rect.Left+8,rect.Bottom-2,rect.Right-8,rect.Bottom-2);};}
  var button=root as Button;if(button!=null&&!(button is ToolModuleButton)){button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderSize=1;button.FlatAppearance.BorderColor=Line;button.BackColor=Color.White;button.ForeColor=Ink;button.Cursor=Cursors.Hand;button.FlatAppearance.MouseOverBackColor=Color.FromArgb(231,244,244);button.FlatAppearance.MouseDownBackColor=Color.FromArgb(211,233,234);if(button.Text=="Apply"||button.Text=="Apply deductions"||button.Text=="Build preview"||button.Text=="Save project information"||button.Text=="Save footing"||button.Text=="Open / Link Excel"){button.BackColor=Teal;button.ForeColor=Color.White;button.FlatAppearance.MouseOverBackColor=Color.FromArgb(0,91,99);button.FlatAppearance.MouseDownBackColor=Color.FromArgb(0,76,83);button.FlatAppearance.BorderSize=0;}}
  var g=root as DataGridView;if(g!=null){g.EnableHeadersVisualStyles=false;g.BackgroundColor=Color.White;g.BorderStyle=BorderStyle.FixedSingle;g.GridColor=Line;g.ColumnHeadersDefaultCellStyle.BackColor=Ink;g.ColumnHeadersDefaultCellStyle.ForeColor=Color.White;g.ColumnHeadersDefaultCellStyle.Font=new Font("Segoe UI",9,FontStyle.Bold);g.ColumnHeadersDefaultCellStyle.Padding=new Padding(6);g.ColumnHeadersHeight=Math.Max(56,g.ColumnHeadersHeight);g.ColumnHeadersDefaultCellStyle.WrapMode=DataGridViewTriState.True;g.DefaultCellStyle.Font=new Font("Segoe UI",9.5f);g.DefaultCellStyle.ForeColor=Ink;g.DefaultCellStyle.Padding=new Padding(6,4,6,4);g.DefaultCellStyle.SelectionBackColor=Color.FromArgb(218,239,240);g.DefaultCellStyle.SelectionForeColor=Ink;g.RowTemplate.Height=34;g.RowTemplate.MinimumHeight=34;g.AllowUserToResizeRows=false;g.AllowUserToOrderColumns=false;g.AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;g.DefaultCellStyle.WrapMode=DataGridViewTriState.True;foreach(DataGridViewColumn column in g.Columns)column.SortMode=DataGridViewColumnSortMode.NotSortable;foreach(DataGridViewRow row in g.Rows)row.MinimumHeight=34;g.ColumnAdded+=(sender,args)=>args.Column.SortMode=DataGridViewColumnSortMode.NotSortable;g.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(247,250,253);GridAppearance.Apply(g);}
  var text=root as TextBox;if(text!=null){text.BorderStyle=BorderStyle.FixedSingle;if(!text.ReadOnly)text.BackColor=Color.FromArgb(255,252,237);}
  var guide=root as Label;if(guide!=null&&guide.Dock==DockStyle.Top){int minimum=guide.Height;Action fitGuide=()=>{int width=Math.Max(100,guide.ClientSize.Width-guide.Padding.Horizontal-4);int height=TextRenderer.MeasureText(guide.Text,guide.Font,new Size(width,int.MaxValue),TextFormatFlags.WordBreak|TextFormatFlags.TextBoxControl).Height+guide.Padding.Vertical+8;guide.Height=Math.Max(minimum,height);};guide.Resize+=(s,e)=>fitGuide();guide.TextChanged+=(s,e)=>fitGuide();fitGuide();}
  var number=root as NumericUpDown;if(number!=null){number.BorderStyle=BorderStyle.FixedSingle;number.BackColor=Color.FromArgb(255,252,237);}
  var combo=root as ComboBox;if(combo!=null){combo.FlatStyle=FlatStyle.Flat;combo.BackColor=Color.FromArgb(255,252,237);}
  var group=root as GroupBox;if(group!=null)group.ForeColor=Ink;
  foreach(Control c in root.Controls)Apply(c);
  root.ControlAdded+=(s,e)=>Apply(e.Control);
 }
}
}

