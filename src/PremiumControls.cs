using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Common visual components. Native controls retain keyboard/accessibility/click behavior.
static class PremiumControls {
 internal static readonly Color Accent=Color.FromArgb(9,119,132);
 internal static readonly Color AccentHover=Color.FromArgb(7,94,105);
 internal static readonly Color AccentSoft=Color.FromArgb(231,244,244);
 internal static readonly Color Header=Color.FromArgb(244,247,250);
 internal static readonly Font TitleFont=new Font("Segoe UI Semibold",17,FontStyle.Regular);
 internal static void StyleButton(Button button){
  bool primary=ModernUi.Primary(button.Text),pressed=false;bool chrome=object.Equals(button.Tag,"workspace-back");
  button.Font=ModernUi.Body;button.Image=CommandIcons.For(button.Text);button.TextImageRelation=TextImageRelation.ImageBeforeText;button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderSize=0;
  button.UseVisualStyleBackColor=false;button.BackColor=chrome?Color.FromArgb(28,48,67):primary?Accent:Color.White;button.ForeColor=chrome||primary?Color.White:PowTheme.Ink;button.Cursor=Cursors.Hand;
  button.MinimumSize=new Size(0,34);button.Padding=new Padding(12,5,12,5);button.Margin=new Padding(0,2,7,2);
  if(button.Parent is FlowLayoutPanel){button.AutoSize=true;button.AutoSizeMode=AutoSizeMode.GrowAndShrink;}
  button.MouseDown+=(s,e)=>{pressed=true;button.Invalidate();};button.MouseUp+=(s,e)=>{pressed=false;button.Invalidate();};
  button.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Space){pressed=true;button.Invalidate();}};button.KeyUp+=(s,e)=>{pressed=false;button.Invalidate();};
  button.MouseEnter+=(s,e)=>button.Invalidate();button.MouseLeave+=(s,e)=>{pressed=false;button.Invalidate();};button.GotFocus+=(s,e)=>button.Invalidate();button.LostFocus+=(s,e)=>button.Invalidate();
  button.Paint+=delegate(object sender,PaintEventArgs e){
   if(button.Width<3||button.Height<3)return;var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   g.Clear(button.Parent==null?Color.White:button.Parent.BackColor);
   bool hover=button.ClientRectangle.Contains(button.PointToClient(Cursor.Position));
   Color fill=chrome?(hover?Color.FromArgb(42,65,85):Color.FromArgb(28,48,67)):!button.Enabled?ModernUi.Soft:primary?(pressed?Color.FromArgb(12,78,137):hover?AccentHover:Accent):pressed?Color.FromArgb(224,232,239):hover?Color.FromArgb(244,247,250):Color.White;
   using(var path=ModernUi.Rounded(new RectangleF(.5f,.5f,button.Width-1,button.Height-1),4))using(var brush=new SolidBrush(fill))using(var border=new Pen(chrome?Color.FromArgb(59,80,100):primary?fill:button.Focused?Accent:PowTheme.Line)){
    g.FillPath(brush,path);g.DrawPath(border,path);
   }
   var text=Rectangle.Inflate(button.ClientRectangle,-9,-3);
   if(button.Image!=null){g.DrawImage(button.Image,9,(button.Height-16)/2,16,16);text.X+=22;text.Width-=22;}
   TextRenderer.DrawText(g,button.Text,button.Font,text,!button.Enabled?PowTheme.Muted:chrome||primary?Color.White:PowTheme.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak);
   if(button.Focused)ControlPaint.DrawFocusRectangle(g,Rectangle.Inflate(button.ClientRectangle,-4,-4),primary?Color.White:Accent,fill);
  };
 }
 internal static Panel WorkspaceHeader(Label title,Action back){
  var panel=new Panel{Dock=DockStyle.Top,Height=66,Padding=new Padding(20,13,20,13),BackColor=Color.FromArgb(18,35,52),ForeColor=Color.White};
  var home=new Button{Text="‹  Workspaces",Tag="workspace-back",Dock=DockStyle.Left,Width=126};home.Click+=(s,e)=>back();
  title.AutoSize=false;title.Dock=DockStyle.Fill;title.TextAlign=ContentAlignment.MiddleLeft;title.Padding=new Padding(22,0,0,0);title.Font=new Font("Segoe UI Semibold",14,FontStyle.Regular);title.ForeColor=Color.White;
  var brand=new Label{Text="DTCE  /  ENGINEERING",Dock=DockStyle.Right,Width=185,TextAlign=ContentAlignment.MiddleRight,ForeColor=Color.FromArgb(175,210,220),Font=new Font("Segoe UI",9,FontStyle.Bold)};
  panel.Controls.Add(title);panel.Controls.Add(brand);panel.Controls.Add(home);return panel;
 }
}
class PremiumSectionHeading:Control {
 public string Subtitle;
 public PremiumSectionHeading(){DoubleBuffered=true;Height=74;Dock=DockStyle.Top;BackColor=Color.White;Padding=new Padding(18,12,18,8);}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);TextRenderer.DrawText(e.Graphics,Text,PremiumControls.TitleFont,new Rectangle(18,10,Math.Max(1,Width-36),30),PowTheme.Ink,TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix);TextRenderer.DrawText(e.Graphics,Subtitle??"",ModernUi.Body,new Rectangle(19,43,Math.Max(1,Width-38),22),PowTheme.Muted,TextFormatFlags.NoPrefix|TextFormatFlags.SingleLine);}
}
}
