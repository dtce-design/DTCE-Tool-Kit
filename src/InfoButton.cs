using System;using System.Drawing;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* long explanations go behind an info button (owner, 20 November 2026: "yung mga unnecessary word mag create na lang ng button
   para sa info"): the text shows on hover and in a window on click */
static class InfoButton {
 public static Button Create(string title,string text){var b=new Button{Text="ⓘ  Info",AutoSize=true,Height=30,Margin=new Padding(0,0,8,6),AccessibleName="Information about "+title};var tip=new ToolTip{AutoPopDelay=30000,InitialDelay=300};tip.SetToolTip(b,text);b.Disposed+=(s,e)=>tip.Dispose();
  b.Click+=(s,e)=>Show(b.FindForm(),title,text);return b;}
 public static void Show(IWin32Window owner,string title,string text){using(var f=new Form{Text=title,ClientSize=new Size(560,320),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MinimizeBox=false,MaximizeBox=false,ShowInTaskbar=false,Font=ModernUi.Body}){
   var box=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BorderStyle=BorderStyle.None,Text=text.Replace("\r\n","\n").Replace("\n","\r\n"),BackColor=Color.White};var pad=new Panel{Dock=DockStyle.Fill,Padding=new Padding(16),BackColor=Color.White};pad.Controls.Add(box);
   var ok=new Button{Text="Close",DialogResult=DialogResult.OK,Width=100,Height=32,Anchor=AnchorStyles.Right|AnchorStyles.Bottom};var bar=new Panel{Dock=DockStyle.Bottom,Height=48};ok.Location=new Point(f.ClientSize.Width-116,8);bar.Controls.Add(ok);f.Controls.Add(pad);f.Controls.Add(bar);f.AcceptButton=ok;f.CancelButton=ok;AppBrand.Apply(f);box.Select(0,0);f.ShowDialog(owner);}}
}
}
