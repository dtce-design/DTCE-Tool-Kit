using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Collections.Generic;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Presentation only; grid instances, row order, field values and event handlers stay intact.
class WorksheetHeading:Control {
 public string Subtitle;static readonly Font Heading=new Font("Segoe UI Semibold",15,FontStyle.Regular);
 public WorksheetHeading(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);Dock=DockStyle.Top;Height=62;BackColor=PowTheme.Background;AccessibleRole=AccessibleRole.StaticText;}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var bounds=new Rectangle(2,2,Math.Max(1,Width-4),28);TextRenderer.DrawText(e.Graphics,Text,Heading,bounds,PowTheme.Ink,TextFormatFlags.NoPrefix|TextFormatFlags.SingleLine|TextFormatFlags.VerticalCenter);TextRenderer.DrawText(e.Graphics,Subtitle??"",ModernUi.Body,new Rectangle(3,34,Math.Max(1,Width-6),22),PowTheme.Muted,TextFormatFlags.NoPrefix|TextFormatFlags.SingleLine);}
}
static class CommandIcons {
 static readonly Dictionary<string,Bitmap> cache=new Dictionary<string,Bitmap>();
 internal static Image For(string caption){string s=(caption??"").ToLowerInvariant();string kind=s.StartsWith("save")?"save":s.StartsWith("open")||s.StartsWith("use open")?"open":s=="add entry"?"add":s=="remove entry"?"remove":s.Contains("export")?"export":s.Contains("sync")||s.Contains("refresh")?"sync":s.Contains("print")?"print":null;if(kind==null)return null;bool white=ModernUi.Primary(caption);string key=kind+white;Bitmap image;if(cache.TryGetValue(key,out image))return image;image=new Bitmap(20,20);using(var g=Graphics.FromImage(image))using(var pen=new Pen(white?Color.White:PowTheme.Teal,1.5f)){g.SmoothingMode=SmoothingMode.AntiAlias;pen.StartCap=pen.EndCap=LineCap.Round;
  if(kind=="add"||kind=="remove"){g.DrawLine(pen,4,10,16,10);if(kind=="add")g.DrawLine(pen,10,4,10,16);}
  else if(kind=="save"){g.DrawRectangle(pen,3,3,14,14);g.DrawRectangle(pen,6,3,8,5);g.DrawRectangle(pen,6,12,8,5);}
  else if(kind=="open"){g.DrawLines(pen,new[]{new Point(2,16),new Point(2,5),new Point(8,5),new Point(10,7),new Point(17,7)});g.DrawPolygon(pen,new[]{new Point(2,16),new Point(5,9),new Point(18,9),new Point(15,16)});}
  else if(kind=="export"){g.DrawLines(pen,new[]{new Point(4,4),new Point(4,17),new Point(16,17),new Point(16,12)});g.DrawLine(pen,8,12,16,4);g.DrawLines(pen,new[]{new Point(10,4),new Point(16,4),new Point(16,10)});}
  else if(kind=="sync"){g.DrawArc(pen,3,3,14,14,30,255);g.DrawLines(pen,new[]{new Point(10,2),new Point(14,3),new Point(12,7)});g.DrawLines(pen,new[]{new Point(4,12),new Point(3,17),new Point(8,17)});}
  else {g.DrawRectangle(pen,3,7,14,7);g.DrawRectangle(pen,6,2,8,5);g.DrawRectangle(pen,6,12,8,6);}
 }cache[key]=image;return image;}
}
}
