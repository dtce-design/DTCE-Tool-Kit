using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Linq;
namespace BeamScheduleFormatter {
/* Presentable calculation reports (owner, 19 November 2026: "calculation sheet niya dapat maganda very detail and presentable.
   Dapat lahat ng report ganito. Presentable may mga models diagrams etc."). Blocks flow down A4 pages in points under the
   CalculationDocument header (title, project) and footer; text is never below 7 pt; tables continue on the next page with their
   headings; figures, checks and paragraphs are not split. */
sealed class RichReport {
 /* page frame in points; set from the page size before each layout and each page is drawn */
 static float Left=36,Width=523,Top=128,Bottom=792;readonly bool a3;
 static void Frame(bool a3){Left=36;Top=128;Width=a3?1190.55f-72:523;Bottom=a3?841.89f-50:792;}
 public static float ContentWidth(bool a3){return a3?1190.55f-72:523;}
 abstract class Block {internal abstract float Height(Graphics g);internal virtual bool Splittable{get{return false;}}internal abstract void Draw(Graphics g,float y);}
 readonly string title;readonly List<Block> blocks=new List<Block>();
 public RichReport(string title,bool a3Landscape=false){this.title=title;a3=a3Landscape;Frame(a3);}
 static Font Body{get{return new Font("Segoe UI",7.5f,FontStyle.Regular,GraphicsUnit.Point);}}
 static Font Bold{get{return new Font("Segoe UI",7.5f,FontStyle.Bold,GraphicsUnit.Point);}}
 static Font Mono{get{return new Font("Consolas",7.5f,FontStyle.Regular,GraphicsUnit.Point);}}
 static float Measure(Graphics g,string text,Font f,float width){return g.MeasureString(text??"",f,(int)Math.Max(10,width)).Height;}
 /* ---- blocks ---- */
 sealed class HeadingBlock:Block {internal string Text;internal override float Height(Graphics g){return 34;}internal override void Draw(Graphics g,float y){CalculationDocument.Section(g,Text,y+4,Width);}}
 sealed class ParagraphBlock:Block {internal string Text;internal bool Muted,Strong;internal override float Height(Graphics g){using(var f=Strong?Bold:Body)return Measure(g,Text,f,Width)+6;}internal override void Draw(Graphics g,float y){using(var f=Strong?Bold:Body)using(var b=new SolidBrush(Muted?PowTheme.Muted:PowTheme.Ink))g.DrawString(Text,f,b,new RectangleF(Left,y,Width,2000));}}
 sealed class FigureBlock:Block {internal float H;internal Action<Graphics,RectangleF> Paint;internal string Caption;internal override float Height(Graphics g){return H+(Caption==null?6:22);}
  internal override void Draw(Graphics g,float y){var rect=new RectangleF(Left,y,Width,H);using(var b=new SolidBrush(Color.White))g.FillRectangle(b,rect);var state=g.Save();try{g.SetClip(rect);Paint(g,rect);}finally{g.Restore(state);}using(var pen=new Pen(PowTheme.Line))g.DrawRectangle(pen,rect.X,rect.Y,rect.Width,rect.Height);
   if(Caption!=null)using(var f=new Font("Segoe UI",7.5f,FontStyle.Italic,GraphicsUnit.Point))using(var b=new SolidBrush(PowTheme.Muted))g.DrawString(Caption,f,b,new RectangleF(Left,y+H+3,Width,16));}}
 sealed class TableBlock:Block {internal string[] Headers;internal List<string[]> Rows;internal float[] Widths;internal Func<string[],Color?> RowColor;internal int Start,Count=-1;internal int PaintCol=-1;internal float MinRow;internal Action<Graphics,RectangleF,int> Painter;
  internal override bool Splittable{get{return true;}}
  float RowHeight(Graphics g,string[] row){float h=0;using(var f=Body)for(int c=0;c<Widths.Length&&c<row.Length;c++)h=Math.Max(h,Measure(g,row[c],f,Widths[c]-6));return Math.Max(h+5,MinRow);}
  float HeaderHeight(Graphics g){float h=0;using(var f=Bold)for(int c=0;c<Widths.Length;c++)h=Math.Max(h,Measure(g,Headers[c],f,Widths[c]-6));return h+6;}
  internal override float Height(Graphics g){float h=HeaderHeight(g);int end=Count<0?Rows.Count:Math.Min(Rows.Count,Start+Count);for(int r=Start;r<end;r++)h+=RowHeight(g,Rows[r]);return h+8;}
  /* how many rows from Start fit in the space */
  internal int Fit(Graphics g,float space){float h=HeaderHeight(g)+8;int n=0;for(int r=Start;r<Rows.Count;r++){h+=RowHeight(g,Rows[r]);if(h>space)break;n++;}return n;}
  internal override void Draw(Graphics g,float y){float x=Left,hh=HeaderHeight(g);using(var head=new SolidBrush(PowTheme.Ink))g.FillRectangle(head,Left,y,Widths.Sum(),hh);using(var f=Bold)for(int c=0;c<Headers.Length;c++){g.DrawString(Headers[c],f,Brushes.White,new RectangleF(x+3,y+3,Widths[c]-6,hh));x+=Widths[c];}y+=hh;
   int end=Count<0?Rows.Count:Math.Min(Rows.Count,Start+Count);using(var f=Body)using(var line=new Pen(PowTheme.Line))for(int r=Start;r<end;r++){var row=Rows[r];float rh=RowHeight(g,row);Color? tint=RowColor==null?null:RowColor(row);if(tint.HasValue)using(var b=new SolidBrush(tint.Value))g.FillRectangle(b,Left,y,Widths.Sum(),rh);else if((r-Start)%2==1)using(var b=new SolidBrush(Color.FromArgb(246,247,249)))g.FillRectangle(b,Left,y,Widths.Sum(),rh);
    x=Left;using(var ink=new SolidBrush(PowTheme.Ink))for(int c=0;c<Widths.Length&&c<row.Length;c++){if(c==PaintCol&&Painter!=null){var st=g.Save();try{var cell=new RectangleF(x+2,y+2,Widths[c]-4,rh-4);g.SetClip(cell);Painter(g,cell,r);}finally{g.Restore(st);}}else g.DrawString(row[c],f,ink,new RectangleF(x+3,y+2,Widths[c]-6,rh));x+=Widths[c];}g.DrawLine(line,Left,y+rh,Left+Widths.Sum(),y+rh);y+=rh;}}}
 sealed class CheckBlock:Block {internal string Name,Clause,Governing;internal List<string> Lines;internal double Demand,Capacity,Ratio;internal string Unit;internal bool Ok;
  string Result{get{return "Demand "+Fmt(Demand)+" "+Unit+"   ≤   capacity "+Fmt(Capacity)+" "+Unit+"   →   ratio "+(double.IsInfinity(Ratio)?"∞":Ratio.ToString("0.00"));}}
  static string Fmt(double v){return double.IsInfinity(v)||double.IsNaN(v)?"-":Math.Abs(v)>=100?v.ToString("0.0"):v.ToString("0.00");}
  internal override float Height(Graphics g){float h=20;using(var m=Mono)foreach(var l in Lines)h+=Measure(g,l,m,Width-24)+1;using(var b=Body)h+=Measure(g,Governing,b,Width-24)+Measure(g,Result,b,Width-100)+14;return h+8;}
  internal override void Draw(Graphics g,float y){float h=Height(g)-8;using(var border=new Pen(Ok?Color.FromArgb(150,190,170):Color.FromArgb(220,120,120)))g.DrawRectangle(border,Left,y,Width,h);using(var bar=new SolidBrush(Ok?Color.FromArgb(232,244,238):Color.FromArgb(252,232,232)))g.FillRectangle(bar,Left,y,Width,18);
   using(var b=Bold)using(var ink=new SolidBrush(PowTheme.Ink)){g.DrawString(Name,b,ink,Left+6,y+3);var sz=g.MeasureString(Clause,b);g.DrawString(Clause,b,ink,Left+Width-sz.Width-6,y+3);}
   float yy=y+22;using(var m=Mono)using(var ink=new SolidBrush(PowTheme.Ink))foreach(var l in Lines){g.DrawString(l,m,ink,new RectangleF(Left+12,yy,Width-24,400));yy+=Measure(g,l,m,Width-24)+1;}
   using(var f=Body)using(var muted=new SolidBrush(PowTheme.Muted)){g.DrawString(Governing,f,muted,new RectangleF(Left+12,yy+2,Width-24,200));yy+=Measure(g,Governing,f,Width-24)+4;}
   using(var f=Bold)using(var ink=new SolidBrush(PowTheme.Ink))g.DrawString(Result,f,ink,new RectangleF(Left+12,yy,Width-110,60));
   var badge=new RectangleF(Left+Width-78,yy-2,66,16);using(var b=new SolidBrush(Ok?Color.FromArgb(40,140,90):Color.FromArgb(200,50,50)))g.FillRectangle(b,badge);using(var f=Bold){var t=Ok?"OK":"NOT OK";var sz=g.MeasureString(t,f);g.DrawString(t,f,Brushes.White,badge.X+(badge.Width-sz.Width)/2,badge.Y+1);}}}
 sealed class SignatureBlock:Block {internal string[] Roles;internal override float Height(Graphics g){return 70;}internal override void Draw(Graphics g,float y){float w=Width/Roles.Length;using(var f=Body)using(var muted=new SolidBrush(PowTheme.Muted))using(var pen=new Pen(PowTheme.Ink))for(int k=0;k<Roles.Length;k++){float x=Left+k*w+6;g.DrawLine(pen,x,y+38,x+w-18,y+38);g.DrawString(Roles[k]+"\r\nName, PRC no., date",f,muted,x,y+42);}}}
 /* ---- authoring ---- */
 public RichReport Heading(string text){blocks.Add(new HeadingBlock{Text=text});return this;}
 public RichReport Paragraph(string text,bool muted=false,bool strong=false){blocks.Add(new ParagraphBlock{Text=text,Muted=muted,Strong=strong});return this;}
 public RichReport Figure(float height,Action<Graphics,RectangleF> paint,string caption=null){blocks.Add(new FigureBlock{H=height,Paint=paint,Caption=caption});return this;}
 public RichReport Table(string[] headers,IEnumerable<string[]> rows,float[] widths,Func<string[],Color?> rowColor=null){float sum=widths.Sum();var w=widths.Select(x=>x/sum*Width).ToArray();blocks.Add(new TableBlock{Headers=headers,Rows=rows.ToList(),Widths=w,RowColor=rowColor});return this;}
 /* a table with one drawn column (a bar sketch in each row of a bar bending schedule) */
 public RichReport Table(string[] headers,IEnumerable<string[]> rows,float[] widths,int paintColumn,float minRowHeight,Action<Graphics,RectangleF,int> painter,Func<string[],Color?> rowColor=null){float sum=widths.Sum();var w=widths.Select(x=>x/sum*Width).ToArray();blocks.Add(new TableBlock{Headers=headers,Rows=rows.ToList(),Widths=w,RowColor=rowColor,PaintCol=paintColumn,MinRow=minRowHeight,Painter=painter});return this;}
 public RichReport KeyValues(IEnumerable<string[]> pairs){return Table(new[]{"Item","Value"},pairs,new float[]{38,62});}
 public RichReport Check(string name,string clause,IEnumerable<string> lines,string governing,double demand,double capacity,string unit,bool ok){blocks.Add(new CheckBlock{Name=name,Clause=clause,Lines=lines.ToList(),Governing=governing,Demand=demand,Capacity=capacity,Ratio=capacity<=0?double.PositiveInfinity:demand/capacity,Unit=unit,Ok=ok});return this;}
 public RichReport Signatures(params string[] roles){blocks.Add(new SignatureBlock{Roles=roles});return this;}
 /* ---- layout into pages ---- */
 public CalculationDocument Build(){Frame(a3);var doc=new CalculationDocument(title);if(a3)doc.UseA3Landscape();var pages=new List<List<KeyValuePair<Block,float>>>();var page=new List<KeyValuePair<Block,float>>();float y=Top;
  using(var bmp=new Bitmap(10,10))using(var g=Graphics.FromImage(bmp)){g.PageUnit=GraphicsUnit.Point;
   Action newPage=()=>{pages.Add(page);page=new List<KeyValuePair<Block,float>>();y=Top;};
   for(int bi=0;bi<blocks.Count;bi++){var block=blocks[bi];var table=block as TableBlock;
    /* a heading stays with the start of what follows it */if(block is HeadingBlock&&bi+1<blocks.Count&&page.Count>0){var next=blocks[bi+1];var nt=next as TableBlock;float need=block.Height(g)+(nt!=null?Math.Min(nt.Height(g),90):next.Height(g));if(y+need>Bottom)newPage();}
    if(table!=null){int start=0;while(start<table.Rows.Count||start==0){var part=new TableBlock{Headers=table.Headers,Rows=table.Rows,Widths=table.Widths,RowColor=table.RowColor,Start=start,PaintCol=table.PaintCol,MinRow=table.MinRow,Painter=table.Painter};int fit=part.Fit(g,Bottom-y);if(fit==0&&page.Count>0){newPage();fit=part.Fit(g,Bottom-y);}fit=Math.Max(1,fit);part.Count=fit;page.Add(new KeyValuePair<Block,float>(part,y));y+=part.Height(g);start+=fit;if(start>=table.Rows.Count)break;newPage();}continue;}
    float h=block.Height(g);if(y+h>Bottom&&page.Count>0)newPage();page.Add(new KeyValuePair<Block,float>(block,y));y+=h;}
   if(page.Count>0)pages.Add(page);}
  bool big=a3;foreach(var p in pages){var items=p;doc.Sheets.Add(g=>{Frame(big);foreach(var item in items)item.Key.Draw(g,item.Value);});}
  if(doc.Sheets.Count==0)doc.Sheets.Add(g=>{});return doc;}
}
}
