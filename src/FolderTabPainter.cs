using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Folder tabs: rounded tops on a soft strip; the selected tab takes the page colour, has no bottom edge and a
// coloured top bar (main / sub / nested tab colours), so it reads as the front folder joined to its page.
static class FolderTabPainter {
 static int Depth(TabControl tab){int depth=0;for(Control c=tab.Parent;c!=null;c=c.Parent)if(c is TabControl)depth++;return depth;}
 static GraphicsPath Folder(Rectangle r,int radius){var path=new GraphicsPath();int d=radius*2;path.AddLine(r.Left,r.Bottom,r.Left,r.Top+radius);path.AddArc(r.Left,r.Top,d,d,180,90);path.AddLine(r.Left+radius,r.Top,r.Right-radius,r.Top);path.AddArc(r.Right-d,r.Top,d,d,270,90);path.AddLine(r.Right,r.Top+radius,r.Right,r.Bottom);return path;}
 static Color PageColor(TabControl tab){var page=tab.SelectedTab;return page!=null&&page.BackColor!=Color.Transparent&&page.BackColor.A==255?page.BackColor:ModernUi.Surface;}
 internal static void Draw(TabControl tab,Graphics g,int index,Rectangle bounds){if(index<0||index>=tab.TabCount||bounds.Width<3||bounds.Height<8)return;
  bool selected=index==tab.SelectedIndex;var nav=tab as NavigationTabs;bool hover=nav!=null&&nav.HoverIndex==index&&!selected;int depth=Depth(tab);
  var face=selected?new Rectangle(bounds.Left,bounds.Top,bounds.Width,bounds.Height+1):new Rectangle(bounds.Left,bounds.Top+3,bounds.Width,bounds.Height-3);
  Color accent=DesktopAppearance.Palette.ActiveTab(depth);Color fill=selected?PageColor(tab):hover?ControlPaint.Light(DesktopAppearance.Palette.Tab(depth),.35f):DesktopAppearance.Palette.Tab(depth);
  g.SmoothingMode=SmoothingMode.AntiAlias;
  using(var path=Folder(face,6)){using(var brush=new SolidBrush(fill))g.FillPath(brush,path);using(var pen=new Pen(PowTheme.Line))g.DrawPath(pen,path);}
  if(selected){g.SmoothingMode=SmoothingMode.None;using(var brush=new SolidBrush(accent))g.FillRectangle(brush,face.Left+4,face.Top,face.Width-8,3);/* join the page only from the last row, so it never covers tabs below */bool lastRow=nav==null||bounds.Bottom>=nav.StripHeight-1;if(lastRow)using(var brush=new SolidBrush(fill))g.FillRectangle(brush,face.Left+1,bounds.Bottom-1,face.Width-1,3);}
  g.SmoothingMode=SmoothingMode.None;
  using(var font=selected?new Font(tab.Font,FontStyle.Bold):null)TextRenderer.DrawText(g,tab.TabPages[index].Text,font??tab.Font,new Rectangle(face.Left,face.Top+(selected?2:0),face.Width,face.Height-(selected?2:0)),selected?accent:PowTheme.Ink,TextFormatFlags.NoPrefix|TextFormatFlags.VerticalCenter|TextFormatFlags.HorizontalCenter|TextFormatFlags.SingleLine);}
 internal static void Strip(TabControl tab,Graphics g){if(!PowTheme.IsPrepared(tab)||tab.TabCount==0)return;var nav=tab as NavigationTabs;
  var rects=nav!=null?nav.TabLayout().ToArray():Enumerable.Range(0,tab.TabCount).Select(i=>tab.GetTabRect(i)).ToArray();int bottom=Math.Min(tab.ClientSize.Height,rects.Max(r=>r.Bottom)+1);if(bottom<1)return;
  var state=g.Save();try{g.SetClip(new Rectangle(0,0,tab.ClientSize.Width,bottom+1));using(var fill=new SolidBrush(ModernUi.Soft))g.FillRectangle(fill,0,0,tab.ClientSize.Width,bottom);
   using(var pen=new Pen(PowTheme.Line))g.DrawLine(pen,0,bottom-1,tab.ClientSize.Width,bottom-1);
   for(int i=0;i<rects.Length;i++)if(i!=tab.SelectedIndex)Draw(tab,g,i,rects[i]);
   if(tab.SelectedIndex>=0&&tab.SelectedIndex<rects.Length)Draw(tab,g,tab.SelectedIndex,rects[tab.SelectedIndex]);}finally{g.Restore(state);}}
}
}
