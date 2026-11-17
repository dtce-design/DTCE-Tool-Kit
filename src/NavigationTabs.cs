using System;using System.Collections.Generic;using System.Drawing;using System.Runtime.InteropServices;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Folder-style navigation tabs (owner, 17 November 2026). The native Windows tab header reorders rows (the row of
// the selected tab jumps to the bottom), so the header is drawn and laid out here instead: tabs stay in their order,
// wrap left-to-right onto more rows with full captions, and the selected tab joins the page like a folder tab.
// TabPages, selection events and keyboard navigation remain the standard TabControl behaviour.
class NavigationTabs:TabControl {
 const int StripMargin=4,TabGap=2,TextPad=14;
 [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr handle,int message,IntPtr wparam,IntPtr lparam);
 protected override bool ShowFocusCues{get{return false;}}
 internal static bool KeyboardFocusVisible(TabControl tabs){return false;}
 int hover=-1;
 public NavigationTabs(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,false);SetStyle(ControlStyles.ResizeRedraw,true);Appearance=TabAppearance.Normal;Alignment=TabAlignment.Top;Multiline=true;
  /* The native header is reduced to an invisible sliver; DisplayRectangle reserves room for the drawn strip. */SizeMode=TabSizeMode.Fixed;ItemSize=new Size(1,1);}
 int RowHeight{get{return Font.Height+14;}}
 internal List<Rectangle> TabLayout(){var list=new List<Rectangle>();int width=Math.Max(60,ClientSize.Width-2*StripMargin);int x=StripMargin,y=StripMargin;
  for(int i=0;i<TabCount;i++){int w=TextRenderer.MeasureText(TabPages[i].Text,Font,Size.Empty,TextFormatFlags.NoPrefix|TextFormatFlags.SingleLine|TextFormatFlags.NoPadding).Width+2*TextPad;if(x>StripMargin&&x+w>StripMargin+width){x=StripMargin;y+=RowHeight;}list.Add(new Rectangle(x,y,w,RowHeight));x+=w+TabGap;}
  return list;}
 internal int StripHeight{get{if(TabCount==0)return StripMargin;var rects=TabLayout();return rects[rects.Count-1].Bottom;}}
 public new Rectangle GetTabRect(int index){var rects=TabLayout();if(index<0||index>=rects.Count)throw new ArgumentOutOfRangeException("index");return rects[index];}
 public override Rectangle DisplayRectangle{get{int top=StripHeight+1;return new Rectangle(1,top,Math.Max(0,ClientSize.Width-2),Math.Max(0,ClientSize.Height-top-1));}}
 void Relayout(){if(!IsHandleCreated||IsDisposed)return;var page=SelectedTab;if(page!=null&&page.Bounds!=DisplayRectangle)page.Bounds=DisplayRectangle;Invalidate();}
 void HideNativeFocus(){if(IsHandleCreated)SendMessage(Handle,0x128,new IntPtr(0x10001),IntPtr.Zero);}
 protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);HideNativeFocus();}
 protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);HideNativeFocus();}
 protected override void OnSelectedIndexChanged(EventArgs e){base.OnSelectedIndexChanged(e);HideNativeFocus();Relayout();}
 protected override void OnResize(EventArgs e){base.OnResize(e);Relayout();}
 protected override void OnFontChanged(EventArgs e){base.OnFontChanged(e);Relayout();}
 protected override void OnControlAdded(ControlEventArgs e){base.OnControlAdded(e);Relayout();}
 protected override void OnControlRemoved(ControlEventArgs e){base.OnControlRemoved(e);Relayout();}
 int HitTab(Point p){var rects=TabLayout();for(int i=0;i<rects.Count;i++)if(rects[i].Contains(p))return i;return -1;}
 protected override void OnMouseDown(MouseEventArgs e){int i=HitTab(e.Location);if(i>=0&&e.Button==MouseButtons.Left){if(i!=SelectedIndex)SelectedIndex=i;Focus();return;}base.OnMouseDown(e);}
 protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);int i=HitTab(e.Location);if(i!=hover){hover=i;Cursor=i>=0?Cursors.Hand:Cursors.Default;Invalidate(new Rectangle(0,0,ClientSize.Width,StripHeight+2));}}
 protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);if(hover>=0){hover=-1;Cursor=Cursors.Default;Invalidate(new Rectangle(0,0,ClientSize.Width,StripHeight+2));}}
 internal int HoverIndex{get{return hover;}}
 protected override void WndProc(ref Message m){
  /* Ignore clicks on the hidden native sliver so they cannot select a tab by accident. */
  if(m.Msg==0x201||m.Msg==0x203){var p=new Point((short)(m.LParam.ToInt64()&0xFFFF),(short)((m.LParam.ToInt64()>>16)&0xFFFF));if(p.Y<StripHeight){OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,p.X,p.Y,0));return;}}
  base.WndProc(ref m);if(m.Msg==0x129)m.Result=new IntPtr(m.Result.ToInt64()|1);if(IsDisposed||!IsHandleCreated)return;
  if(m.Msg==0xF)using(var graphics=Graphics.FromHwnd(Handle))FolderTabPainter.Strip(this,graphics);
  else if((m.Msg==0x317||m.Msg==0x318)&&m.WParam!=IntPtr.Zero)using(var graphics=Graphics.FromHdc(m.WParam))FolderTabPainter.Strip(this,graphics);}
}
}
