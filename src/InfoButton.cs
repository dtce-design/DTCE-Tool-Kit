using System;using System.Drawing;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* long explanations go behind an info button (owner, 20 November 2026: "yung mga unnecessary word mag create na lang ng button
   para sa info"): the text shows on hover and in a window on click */
static class InfoButton {
 /* one info icon per window, at the upper right (owner, 20 November 2026: "icon na lang siya, isang location na lang sa upper
    right", "nagpapalit yung info button based sa tab na nakaopen"): notes are registered on a tab page or panel and the icon
    shows the note of the tab that is open (the innermost open tab with a note) */
 static readonly System.Collections.Generic.Dictionary<Control,string[]> notes=new System.Collections.Generic.Dictionary<Control,string[]>();
 public static void Register(Control c,string title,string text){if(c==null||string.IsNullOrEmpty(text))return;notes[c]=new[]{title,text};c.Disposed+=(s,e)=>notes.Remove(c);}
 static bool Shown(Control c,Form f){if(c.FindForm()!=f)return false;for(var x=c;x!=null&&x!=f;x=x.Parent){var tp=x as TabPage;if(tp!=null){var tc=tp.Parent as TabControl;if(tc==null||tc.SelectedTab!=tp)return false;}else if(!x.Visible&&x.IsHandleCreated)return false;}return true;}
 static int Depth(Control c){int d=0;for(var x=c;x!=null;x=x.Parent)d++;return d;}
 internal static string[] Current(Form f){if(f==null)return null;string[] best=null;int depth=-1;foreach(var pair in notes){if(pair.Key.IsDisposed||!Shown(pair.Key,f))continue;int d=Depth(pair.Key);if(d>depth){depth=d;best=pair.Value;}}return best;}
 public static Button Icon(){var b=new Button{Text="ⓘ",Width=36,Height=34,Font=new Font("Segoe UI Symbol",13f),FlatStyle=FlatStyle.Flat,TabStop=true,Cursor=Cursors.Hand,AccessibleName="Information about the open tab",UseMnemonic=false,Tag="info-icon"};b.FlatAppearance.BorderSize=0;b.BackColor=Color.Transparent;b.ForeColor=PowTheme.Teal;
  var tip=new ToolTip{AutoPopDelay=30000,InitialDelay=300};b.Disposed+=(s,e)=>tip.Dispose();var timer=new Timer{Interval=400};b.Disposed+=(s,e)=>timer.Dispose();
  timer.Tick+=(s,e)=>{if(b.IsDisposed)return;var n=Current(b.FindForm());b.Enabled=n!=null;tip.SetToolTip(b,n==null?"No notes for this tab":n[0]);};b.HandleCreated+=(s,e)=>timer.Start();
  b.Click+=(s,e)=>{var f=b.FindForm();var n=Current(f);if(n!=null)Show(f,n[0],n[1]);};return b;}
 /* the icon at the upper right corner of a window that has no workspace header */
 public static Button AttachCorner(Form f){var b=Icon();b.Anchor=AnchorStyles.Top|AnchorStyles.Right;Action place=()=>{b.Location=new Point(f.ClientSize.Width-b.Width-8,8);b.BringToFront();};f.Controls.Add(b);f.Resize+=(s,e)=>place();f.Shown+=(s,e)=>place();place();return b;}
 public static Button Create(string title,string text){var b=new Button{Text="ⓘ  Info",AutoSize=true,Height=30,Margin=new Padding(0,0,8,6),AccessibleName="Information about "+title};var tip=new ToolTip{AutoPopDelay=30000,InitialDelay=300};tip.SetToolTip(b,text);b.Disposed+=(s,e)=>tip.Dispose();
  b.Click+=(s,e)=>Show(b.FindForm(),title,text);return b;}
 public static void Show(IWin32Window owner,string title,string text){using(var f=new Form{Text=title,ClientSize=new Size(560,320),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MinimizeBox=false,MaximizeBox=false,ShowInTaskbar=false,Font=ModernUi.Body}){
   var box=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BorderStyle=BorderStyle.None,Text=text.Replace("\r\n","\n").Replace("\n","\r\n"),BackColor=Color.White};var pad=new Panel{Dock=DockStyle.Fill,Padding=new Padding(16),BackColor=Color.White};pad.Controls.Add(box);
   var ok=new Button{Text="Close",DialogResult=DialogResult.OK,Width=100,Height=32,Anchor=AnchorStyles.Right|AnchorStyles.Bottom};var bar=new Panel{Dock=DockStyle.Bottom,Height=48};ok.Location=new Point(f.ClientSize.Width-116,8);bar.Controls.Add(ok);f.Controls.Add(pad);f.Controls.Add(bar);f.AcceptButton=ok;f.CancelButton=ok;AppBrand.Apply(f);box.Select(0,0);f.ShowDialog(owner);}}
}
}
