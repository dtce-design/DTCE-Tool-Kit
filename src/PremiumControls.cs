using System;using System.Runtime.CompilerServices;using System.Drawing;using System.Drawing.Drawing2D;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Common visual components. Native controls retain keyboard/accessibility/click behavior.
static class PremiumControls {
 internal static Color Accent {get{return DesktopAppearance.Palette.Accent;}}
 internal static Color AccentHover {get{return DesktopAppearance.Palette.AccentHover;}}
 internal static Color AccentSoft {get{return DesktopAppearance.Palette.Selected;}}
 internal static Color Header {get{return DesktopAppearance.Palette.Header;}}
 internal static readonly Font TitleFont=new Font("Segoe UI Semibold",17,FontStyle.Regular);
 static readonly ConditionalWeakTable<Button,object> styledButtons=new ConditionalWeakTable<Button,object>();
 internal static void StyleButton(Button button){
  object marker;if(button==null||button.IsDisposed||styledButtons.TryGetValue(button,out marker))return;styledButtons.Add(button,new object());ButtonFocusChrome.Attach(button);
  bool primary=ModernUi.Primary(button.Text),pressed=false;bool chrome=object.Equals(button.Tag,"workspace-back");
  button.Font=ModernUi.Body;button.Image=CommandIcons.For(button.Text);button.TextImageRelation=TextImageRelation.ImageBeforeText;button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderSize=0;
  button.UseVisualStyleBackColor=false;button.BackColor=chrome?DesktopAppearance.Palette.ChromeButton:primary?Accent:ModernUi.Surface;button.ForeColor=primary?Color.White:PowTheme.Ink;button.Cursor=Cursors.Hand;
  bool sizingCaption=false;Action fitCaption=()=>{if(sizingCaption||button.IsDisposed)return;sizingCaption=true;try{primary=ModernUi.Primary(button.Text);button.Image=CommandIcons.For(button.Text);int labelWidth=TextRenderer.MeasureText(button.Text??"",button.Font,Size.Empty,TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix).Width+28+(button.Image==null?0:22);button.MinimumSize=new Size(labelWidth,Math.Max(38,button.Font.Height+14));button.Invalidate();}finally{sizingCaption=false;}};fitCaption();button.TextChanged+=(s,e)=>fitCaption();button.FontChanged+=(s,e)=>fitCaption();button.AutoEllipsis=false;button.Padding=new Padding(12,5,12,5);button.Margin=new Padding(0,2,7,2);
  if(button.Parent is FlowLayoutPanel){button.AutoSize=true;button.AutoSizeMode=AutoSizeMode.GrowAndShrink;}
  button.MouseDown+=(s,e)=>{pressed=true;button.Invalidate();};button.MouseUp+=(s,e)=>{pressed=false;button.Invalidate();};
  button.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Space){pressed=true;button.Invalidate();}};button.KeyUp+=(s,e)=>{pressed=false;button.Invalidate();};
  button.MouseEnter+=(s,e)=>button.Invalidate();button.MouseLeave+=(s,e)=>{pressed=false;button.Invalidate();};button.GotFocus+=(s,e)=>button.Invalidate();button.LostFocus+=(s,e)=>button.Invalidate();
  button.Paint+=delegate(object sender,PaintEventArgs e){
   if(button.Width<3||button.Height<3)return;var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   g.Clear(button.Parent==null?ModernUi.Surface:button.Parent.BackColor);
   bool hover=button.ClientRectangle.Contains(button.PointToClient(Cursor.Position));
   Color fill=chrome?(hover?DesktopAppearance.Palette.Selected:DesktopAppearance.Palette.ChromeButton):!button.Enabled?ModernUi.Soft:primary?(pressed?DesktopAppearance.Palette.AccentHover:hover?AccentHover:Accent):pressed?DesktopAppearance.Palette.Selected:hover?DesktopAppearance.Palette.Soft:ModernUi.Surface;
   using(var path=ModernUi.Rounded(new RectangleF(.5f,.5f,button.Width-1,button.Height-1),4))using(var brush=new SolidBrush(fill))using(var border=new Pen(chrome?PowTheme.Line:primary?fill:button.Focused?Accent:PowTheme.Line)){
    g.FillPath(brush,path);g.DrawPath(border,path);
   }
   var text=Rectangle.Inflate(button.ClientRectangle,-9,-3);
   if(button.Image!=null){g.DrawImage(button.Image,9,(button.Height-16)/2,16,16);text.X+=22;text.Width-=22;}
   TextRenderer.DrawText(g,button.Text,button.Font,text,!button.Enabled?PowTheme.Muted:primary?Color.White:PowTheme.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix);
   if(button.Focused)FocusHighlight.Draw(g,Rectangle.Inflate(button.ClientRectangle,-3,-3),primary?Color.White:WorksheetColors.Focus);
  };
 }
 internal static Panel WorkspaceHeader(Label title,Action back){
  var panel=new Panel{Dock=DockStyle.Top,Height=66,Padding=new Padding(20,13,20,13),BackColor=DesktopAppearance.Palette.Chrome,ForeColor=PowTheme.Ink,Tag="appearance-header"};
  /* No "Workspaces" back button: each workspace is its own window (owner, 19 November 2026). */
  title.AutoSize=false;title.Dock=DockStyle.Fill;title.TextAlign=ContentAlignment.MiddleLeft;title.Padding=new Padding(0,0,0,0);title.Font=new Font("Segoe UI Semibold",14,FontStyle.Regular);title.ForeColor=PowTheme.Ink;title.Tag="appearance-header-title";
  var brand=new Label{Text="DTCE  /  ENGINEERING",Dock=DockStyle.Right,Width=185,TextAlign=ContentAlignment.MiddleRight,ForeColor=PowTheme.Teal,Tag="appearance-brand",Font=new Font("Segoe UI",9,FontStyle.Bold)};
  panel.Controls.Add(title);panel.Controls.Add(brand);return panel;
 }
}
class PremiumSectionHeading:Control {
 public string Subtitle;
 public PremiumSectionHeading(){DoubleBuffered=true;Height=74;Dock=DockStyle.Top;BackColor=ModernUi.Surface;Padding=new Padding(18,12,18,8);}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);TextRenderer.DrawText(e.Graphics,Text,PremiumControls.TitleFont,new Rectangle(18,10,Math.Max(1,Width-36),30),PowTheme.Ink,TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix);TextRenderer.DrawText(e.Graphics,Subtitle??"",ModernUi.Body,new Rectangle(19,43,Math.Max(1,Width-38),22),PowTheme.Muted,TextFormatFlags.NoPrefix|TextFormatFlags.SingleLine);}
}
}

