using System;using System.Drawing;using System.Windows.Forms;using System.Runtime.CompilerServices;
namespace BeamScheduleFormatter {
class PowCard:Panel {
 public PowCard(){DoubleBuffered=true;BackColor=Color.White;Padding=new Padding(16);Margin=new Padding(0,0,16,16);}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(var p=new Pen(PowTheme.Line))e.Graphics.DrawRectangle(p,0,0,Width-1,Height-1);}
}

}

