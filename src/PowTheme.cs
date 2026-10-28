using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Windows.Forms;using System.Runtime.CompilerServices;
namespace BeamScheduleFormatter {
class PowCard:Panel {
 public PowCard(){DoubleBuffered=true;BackColor=Color.White;Padding=new Padding(14);Margin=new Padding(0,0,14,14);}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var p=new Pen(Color.FromArgb(218,227,236)))e.Graphics.DrawRectangle(p,0,0,Width-1,Height-1);}
}
static class PowTheme {
 static readonly ConditionalWeakTable<Control,object> themed=new ConditionalWeakTable<Control,object>();

 public static readonly Color Ink=Color.FromArgb(27,46,68),Teal=Color.FromArgb(0,119,125),Background=Color.FromArgb(244,247,251);
 public static void Apply(Control root){object seen;if(themed.TryGetValue(root,out seen)){foreach(Control child in root.Controls)Apply(child);return;}themed.Add(root,new object());if(root is Form||root is TabPage)root.BackColor=Background;var tab=root as TabControl;if(tab!=null){tab.Font=new Font("Segoe UI",9.5f);tab.Padding=new Point(15,9);tab.DrawMode=TabDrawMode.OwnerDrawFixed;tab.DrawItem+=(s,e)=>{var rect=e.Bounds;bool selected=e.Index==tab.SelectedIndex;using(var brush=new SolidBrush(selected?Color.White:Background))e.Graphics.FillRectangle(brush,rect);TextRenderer.DrawText(e.Graphics,tab.TabPages[e.Index].Text,tab.Font,rect,selected?Teal:Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix);if(selected)using(var p=new Pen(Teal,3))e.Graphics.DrawLine(p,rect.Left+8,rect.Bottom-2,rect.Right-8,rect.Bottom-2);};}
  var button=root as Button;if(button!=null&&!(button is ToolModuleButton)){button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderSize=1;button.FlatAppearance.BorderColor=Color.FromArgb(207,218,230);button.BackColor=Color.White;button.ForeColor=Ink;button.Font=new Font("Segoe UI",9.5f);button.Padding=new Padding(6,3,6,3);button.Margin=new Padding(3,3,7,3);button.MinimumSize=new Size(0,34);button.Cursor=Cursors.Hand;button.FlatAppearance.MouseOverBackColor=Color.FromArgb(233,245,245);button.FlatAppearance.MouseDownBackColor=Color.FromArgb(211,234,235);if(button.Text=="Open / Link Excel"||button.Text=="Apply"||button.Text=="Apply deductions"||button.Text=="Build preview"||button.Text=="Save project information"){button.BackColor=Teal;button.ForeColor=Color.White;button.FlatAppearance.BorderSize=0;}}
  var g=root as DataGridView;if(g!=null){g.EnableHeadersVisualStyles=false;g.BackgroundColor=Color.White;g.BorderStyle=BorderStyle.FixedSingle;g.GridColor=Color.FromArgb(175,188,202);g.CellBorderStyle=DataGridViewCellBorderStyle.Single;g.ColumnHeadersBorderStyle=DataGridViewHeaderBorderStyle.Single;g.ColumnHeadersDefaultCellStyle.BackColor=Color.FromArgb(232,239,246);g.ColumnHeadersDefaultCellStyle.ForeColor=Ink;g.ColumnHeadersDefaultCellStyle.Font=new Font("Segoe UI",9,FontStyle.Bold);g.ColumnHeadersDefaultCellStyle.Padding=new Padding(7);g.ColumnHeadersHeight=Math.Max(58,g.ColumnHeadersHeight);g.DefaultCellStyle.Font=new Font("Segoe UI",9.5f);g.DefaultCellStyle.ForeColor=Ink;g.DefaultCellStyle.Padding=new Padding(7,5,7,5);g.RowTemplate.Height=36;g.ColumnHeadersDefaultCellStyle.WrapMode=DataGridViewTriState.True;g.AllowUserToResizeRows=false;g.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(249,251,253);GridAppearance.Apply(g);}
  var text=root as TextBox;if(text!=null){text.BorderStyle=BorderStyle.FixedSingle;if(!text.ReadOnly)text.BackColor=Color.FromArgb(255,253,242);}
  var number=root as NumericUpDown;if(number!=null){number.BorderStyle=BorderStyle.FixedSingle;number.BackColor=Color.FromArgb(255,253,242);}
  var combo=root as ComboBox;if(combo!=null){combo.FlatStyle=FlatStyle.Flat;combo.BackColor=Color.FromArgb(255,253,242);}
  var group=root as GroupBox;if(group!=null){group.ForeColor=Ink;}
  foreach(Control c in root.Controls)Apply(c);
 }
}
}
