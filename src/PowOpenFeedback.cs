using System;using System.Drawing;using System.Reflection;using System.Threading;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Loading animation while a saved Program of Works opens (owner request 17 November 2026).
// Filling the worksheets runs on the UI thread, so the indicator has its own UI thread and message loop;
// it keeps animating while the main window is busy. It never owns or touches the main window's controls.
sealed class PowBusyIndicator:IDisposable {
 Form window;Label state;volatile bool closed;readonly ManualResetEvent ready=new ManualResetEvent(false);readonly Thread thread;
 internal PowBusyIndicator(Form owner,string title,string text){
  Rectangle bounds=owner!=null&&owner.IsHandleCreated?owner.RectangleToScreen(owner.ClientRectangle):Screen.PrimaryScreen.WorkingArea;
  thread=new Thread(()=>{
   window=new Form{Text=title,ClientSize=new Size(490,142),FormBorderStyle=FormBorderStyle.FixedDialog,ControlBox=false,ShowInTaskbar=false,TopMost=true,StartPosition=FormStartPosition.Manual,BackColor=Color.White,Font=new Font("Segoe UI",10)};
   window.Location=new Point(bounds.Left+Math.Max(0,(bounds.Width-window.Width)/2),bounds.Top+Math.Max(0,(bounds.Height-window.Height)/2));
   var logo=new PictureBox{Location=new Point(18,26),Size=new Size(74,74),SizeMode=PictureBoxSizeMode.Zoom};
   using(var input=Assembly.GetExecutingAssembly().GetManifestResourceStream("DtceLogo.png"))if(input!=null)using(var image=Image.FromStream(input))logo.Image=new Bitmap(image);
   state=new Label{Location=new Point(110,27),Size=new Size(360,30),Text=text,Font=new Font("Segoe UI Semibold",12)};
   var progress=new ProgressBar{Location=new Point(110,75),Size=new Size(350,15),Style=ProgressBarStyle.Marquee,MarqueeAnimationSpeed=25};
   window.Controls.Add(logo);window.Controls.Add(state);window.Controls.Add(progress);
   window.Shown+=(s,e)=>{ready.Set();if(closed)window.Close();};window.FormClosed+=(s,e)=>{if(logo.Image!=null)logo.Image.Dispose();};
   Application.Run(window);ready.Set();});
  thread.SetApartmentState(ApartmentState.STA);thread.IsBackground=true;thread.Start();ready.WaitOne(3000);
 }
 internal void Status(string text){var w=window;if(w==null)return;try{if(w.IsHandleCreated&&!w.IsDisposed)w.BeginInvoke(new Action(()=>{if(!state.IsDisposed)state.Text=text;}));}catch(InvalidOperationException){}}
 public void Dispose(){closed=true;var w=window;try{if(w!=null&&w.IsHandleCreated&&!w.IsDisposed)w.BeginInvoke(new Action(w.Close));}catch(InvalidOperationException){}thread.Join(3000);}
}
}
