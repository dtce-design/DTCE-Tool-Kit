using System.Drawing;using System.Windows.Forms;
namespace BeamScheduleFormatter {
class PowCard:Panel {
 public PowCard(){DoubleBuffered=true;BackColor=Color.White;Padding=new Padding(16);Margin=new Padding(0,0,16,16);}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
  if(Width<3||Height<3)return;
  using(var path=ModernUi.Rounded(new RectangleF(0.5f,0.5f,Width-1,Height-1),10))using(var p=new Pen(PowTheme.Line))e.Graphics.DrawPath(p,path);
 }
}
}
