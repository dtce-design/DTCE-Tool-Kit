using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Common visual components. Native controls retain keyboard/accessibility/click behavior.
static class PremiumControls {
 internal static readonly Color Accent=Color.FromArgb(68,78,222);
 internal static readonly Color AccentHover=Color.FromArgb(53,63,194);
 internal static readonly Color AccentSoft=Color.FromArgb(237,239,255);
 internal static readonly Color Header=Color.FromArgb(245,247,251);
 internal static readonly Font TitleFont=new Font("Segoe UI",17,FontStyle.Bold);
 internal static void StyleButton(Button button){
  bool primary=ModernUi.Primary(button.Text),pressed=false;
  button.Font=ModernUi.Body;button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderSize=0;
  button.UseVisualStyleBackColor=false;button.BackColor=primary?Accent:Color.White;button.ForeColor=primary?Color.White:PowTheme.Ink;button.Cursor=Cursors.Hand;
  button.MinimumSize=new Size(0,30);button.Padding=new Padding(12,5,12,5);button.Margin=new Padding(0,2,7,2);
  if(button.Parent is FlowLayoutPanel){button.AutoSize=true;button.AutoSizeMode=AutoSizeMode.GrowAndShrink;}
  button.MouseDown+=(s,e)=>{pressed=true;button.Invalidate();};button.MouseUp+=(s,e)=>{pressed=false;button.Invalidate();};
  button.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Space){pressed=true;button.Invalidate();}};button.KeyUp+=(s,e)=>{pressed=false;button.Invalidate();};
  button.MouseEnter+=(s,e)=>button.Invalidate();button.MouseLeave+=(s,e)=>{pressed=false;button.Invalidate();};button.GotFocus+=(s,e)=>button.Invalidate();button.LostFocus+=(s,e)=>button.Invalidate();
  button.Paint+=delegate(object sender,PaintEventArgs e){
   if(button.Width<3||button.Height<3)return;var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   g.Clear(button.Parent==null?Color.White:button.Parent.BackColor);
   bool hover=button.ClientRectangle.Contains(button.PointToClient(Cursor.Position));
   Color fill=!button.Enabled?ModernUi.Soft:primary?(pressed?Color.FromArgb(44,52,162):hover?AccentHover:Accent):pressed?Color.FromArgb(225,229,240):hover?Color.FromArgb(246,248,252):Color.White;
   using(var path=ModernUi.Rounded(new RectangleF(.5f,.5f,button.Width-1,button.Height-1),6))using(var brush=new SolidBrush(fill))using(var border=new Pen(primary?fill:button.Focused?Accent:PowTheme.Line)){
    g.FillPath(brush,path);g.DrawPath(border,path);
   }
   var text=Rectangle.Inflate(button.ClientRectangle,-9,-3);
   if(button.Image!=null){g.DrawImage(button.Image,9,(button.Height-16)/2,16,16);text.X+=22;text.Width-=22;}
   TextRenderer.DrawText(g,button.Text,button.Font,text,!button.Enabled?PowTheme.Muted:primary?Color.White:PowTheme.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak);
   if(button.Focused)ControlPaint.DrawFocusRectangle(g,Rectangle.Inflate(button.ClientRectangle,-4,-4),primary?Color.White:Accent,fill);
  };
 }
 internal static Panel WorkspaceHeader(Label title,Action back){
  var panel=new Panel{Dock=DockStyle.Top,Height=52,Padding=new Padding(16,8,16,8),BackColor=Color.White};
  var home=new Button{Text="‹  Workspaces",Dock=DockStyle.Left,Width=116};home.Click+=(s,e)=>back();
  title.AutoSize=false;title.Dock=DockStyle.Fill;title.Padding=new Padding(18,5,0,0);title.Font=new Font("Segoe UI",12,FontStyle.Bold);
  var brand=new Label{Text="DTCE",Dock=DockStyle.Right,Width=68,TextAlign=ContentAlignment.MiddleRight,ForeColor=Accent,Font=ModernUi.Strong};
  panel.Controls.Add(title);panel.Controls.Add(brand);panel.Controls.Add(home);panel.Paint+=(s,e)=>{using(var p=new Pen(PowTheme.Line))e.Graphics.DrawLine(p,0,panel.Height-1,panel.Width,panel.Height-1);};return panel;
 }
}
class PremiumSectionHeading:Control {
 public string Subtitle;
 public PremiumSectionHeading(){DoubleBuffered=true;Height=74;Dock=DockStyle.Top;BackColor=Color.White;Padding=new Padding(18,12,18,8);}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);TextRenderer.DrawText(e.Graphics,Text,PremiumControls.TitleFont,new Rectangle(18,10,Math.Max(1,Width-36),30),PowTheme.Ink,TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix);TextRenderer.DrawText(e.Graphics,Subtitle??"",ModernUi.Body,new Rectangle(19,43,Math.Max(1,Width-38),22),PowTheme.Muted,TextFormatFlags.NoPrefix|TextFormatFlags.SingleLine);}
}
}
