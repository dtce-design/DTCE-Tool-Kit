using System;using System.Collections.Generic;using System.Drawing;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Bar chart of the finish group totals (owner, 18 November 2026: "create a bar chart sa Finishing Works"): Finishing
// Works > Start, under Totals per Group. One horizontal bar per BOQ group, longest first; a negative total (more
// adjustment than measured area) is drawn in orange to the left of the zero line.
sealed class FinishGroupChart:Control {
 List<KeyValuePair<string,double>> data=new List<KeyValuePair<string,double>>();
 internal FinishGroupChart(){DoubleBuffered=true;ResizeRedraw=true;BackColor=Color.White;Font=new Font("Segoe UI",9f);}
 internal void SetData(IEnumerable<KeyValuePair<string,double>> values){data=values.OrderByDescending(v=>Math.Abs(v.Value)).ToList();Invalidate();}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(BackColor);g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
  using(var title=new Font(Font,FontStyle.Bold))TextRenderer.DrawText(g,"TOTAL m2 PER GROUP",title,new Rectangle(10,6,Width-20,22),PowTheme.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);
  if(data.Count==0){TextRenderer.DrawText(g,"No finish areas yet.",Font,new Rectangle(10,32,Width-20,24),PowTheme.Muted,TextFormatFlags.Left);return;}
  int top=34,rowH=Math.Max(18,Math.Min(30,(Height-top-10)/Math.Max(1,data.Count))),labelW=Math.Min(320,Width/3),valueW=90;
  double max=Math.Max(1e-9,data.Max(d=>Math.Max(0,d.Value))),min=Math.Min(0,data.Min(d=>d.Value));double span=max-min;
  int barLeft=10+labelW+8,barWidth=Math.Max(40,Width-barLeft-valueW-12);int zero=barLeft+(int)Math.Round(barWidth*(-min/span));
  using(var axis=new Pen(PowTheme.Line))g.DrawLine(axis,zero,top-2,zero,top+rowH*data.Count);
  for(int i=0;i<data.Count;i++){var d=data[i];int y=top+i*rowH;if(y+rowH>Height)break;
   TextRenderer.DrawText(g,d.Key,Font,new Rectangle(10,y,labelW,rowH),PowTheme.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);
   int len=(int)Math.Round(barWidth*Math.Abs(d.Value)/span);var bar=d.Value>=0?new Rectangle(zero,y+4,Math.Max(1,len),rowH-8):new Rectangle(zero-len,y+4,Math.Max(1,len),rowH-8);
   using(var fill=new SolidBrush(d.Value>=0?PowTheme.Teal:Color.FromArgb(214,120,30)))g.FillRectangle(fill,bar);
   TextRenderer.DrawText(g,d.Value.ToString("N2")+" m2",Font,new Rectangle(barLeft+barWidth+6,y,valueW,rowH),PowTheme.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine);}}
}
partial class ProgramOfWorksForm {
 FinishGroupChart finishGroupChart;
 Control FinishGroupChartControl(){finishGroupChart=new FinishGroupChart{Dock=DockStyle.Bottom,Height=260};return finishGroupChart;}
}
}
