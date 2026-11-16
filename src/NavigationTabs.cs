using System;using System.Drawing;using System.Runtime.InteropServices;using System.Windows.Forms;
namespace BeamScheduleFormatter {
class NavigationTabs:TabControl {
 bool sizing,measurePending;
 [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr handle,int message,IntPtr wparam,IntPtr lparam);
 protected override bool ShowFocusCues{get{return false;}}
 internal static bool KeyboardFocusVisible(TabControl tabs){return false;}
 public NavigationTabs(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,false);SetStyle(ControlStyles.ResizeRedraw,true);Appearance=TabAppearance.Normal;Alignment=TabAlignment.Top;Multiline=true;SizeMode=TabSizeMode.Normal;}
 void HideNativeFocus(){if(IsHandleCreated)SendMessage(Handle,0x128,new IntPtr(0x10001),IntPtr.Zero);}
 protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);HideNativeFocus();ScheduleCaptionMeasure();}
 protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);HideNativeFocus();}
 protected override void OnSelectedIndexChanged(EventArgs e){base.OnSelectedIndexChanged(e);HideNativeFocus();ScheduleCaptionMeasure();Invalidate();}
 protected override void OnLayout(LayoutEventArgs e){base.OnLayout(e);ScheduleCaptionMeasure();}
 protected override void OnFontChanged(EventArgs e){base.OnFontChanged(e);ScheduleCaptionMeasure();}
 // Padding can recreate the native handle. Never change it inside handle creation,
 // layout or tab-selection notifications; wait until that native operation returns.
 void ScheduleCaptionMeasure(){if(sizing||measurePending||!IsHandleCreated||IsDisposed||Disposing)return;measurePending=true;try{BeginInvoke((Action)(()=>{measurePending=false;if(!IsDisposed&&!Disposing&&IsHandleCreated)EnsureCaptionRoom();}));}catch(InvalidOperationException){measurePending=false;}}
 void EnsureCaptionRoom(){if(sizing||!IsHandleCreated||TabCount==0||Width<200)return;sizing=true;try{for(int pass=0;pass<3;pass++){int deficit=0;for(int i=0;i<TabCount;i++){int needed=TextRenderer.MeasureText(TabPages[i].Text,Font,Size.Empty,TextFormatFlags.NoPrefix|TextFormatFlags.NoPadding|TextFormatFlags.SingleLine).Width+20;deficit=Math.Max(deficit,needed-GetTabRect(i).Width);}if(deficit<=0||Padding.X>=80)break;Padding=new Point(Math.Min(80,Padding.X+(deficit+1)/2+4),Padding.Y);}}finally{sizing=false;}}
 protected override void WndProc(ref Message m){base.WndProc(ref m);if(m.Msg==0x129)m.Result=new IntPtr(m.Result.ToInt64()|1);if(IsDisposed||!IsHandleCreated)return;if(m.Msg==0xF)using(var graphics=Graphics.FromHwnd(Handle))FolderTabPainter.Strip(this,graphics);else if((m.Msg==0x317||m.Msg==0x318)&&m.WParam!=IntPtr.Zero)using(var graphics=Graphics.FromHdc(m.WParam))FolderTabPainter.Strip(this,graphics);}
}
}
