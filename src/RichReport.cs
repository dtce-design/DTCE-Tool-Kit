using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Linq;
namespace BeamScheduleFormatter {
/* Presentable calculation reports (owner, 19 November 2026: "calculation sheet niya dapat maganda very detail and presentable.
   Dapat lahat ng report ganito. Presentable may mga models diagrams etc."). Blocks flow down A4 pages in points under the
   CalculationDocument header (title, project) and footer; text is never below 7 pt; tables continue on the next page with their
   headings; figures, checks and paragraphs are not split. */
sealed class RichReport {
 /* page frame in points; set from the page size before each layout and each page is drawn */
 static float Left=CalculationDocument.L,Width=CalculationDocument.CW,Top=128,Bottom=792;readonly bool a3;
 static void Frame(bool a3){Left=CalculationDocument.L;Top=128;Width=a3?1190.55f-CalculationDocument.L-36:CalculationDocument.CW;Bottom=a3?841.89f-50:792;}
 public static float ContentWidth(bool a3){return a3?1190.55f-CalculationDocument.L-36:CalculationDocument.CW;}
 abstract class Block {internal abstract float Height(Graphics g);internal virtual bool Splittable{get{return false;}}internal abstract void Draw(Graphics g,float y);}
 readonly string title;readonly List<Block> blocks=new List<Block>();
 public RichReport(string title,bool a3Landscape=false){this.title=title;a3=a3Landscape;Frame(a3);}
 /* compact type for long quantity reports: 7 pt and tighter rows (owner: reports save paper; 7 pt minimum) */
 internal bool Compact;static bool compactNow;
 static Font Body{get{return new Font("Segoe UI",compactNow?7f:7.5f,FontStyle.Regular,GraphicsUnit.Point);}}
 static Font Bold{get{return new Font("Segoe UI",compactNow?7f:7.5f,FontStyle.Bold,GraphicsUnit.Point);}}
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
  /* a row of one cell spans the table: a shaded group title (member / pay item) */
  bool Span(string[] row){return row.Length==1&&Widths.Length>1;}
  float RowHeight(Graphics g,string[] row){if(Span(row)){using(var b=Bold)return Measure(g,row[0],b,Widths.Sum()-6)+(compactNow?3.5f:6);}float h=0;using(var f=Body)for(int c=0;c<Widths.Length&&c<row.Length;c++)h=Math.Max(h,Measure(g,row[c],f,Widths[c]-6));return Math.Max(h+(compactNow?2.5f:5),MinRow);}
  float HeaderHeight(Graphics g){float h=0;using(var f=Bold)for(int c=0;c<Widths.Length;c++)h=Math.Max(h,Measure(g,Headers[c],f,Widths[c]-6));return h+6;}
  internal override float Height(Graphics g){float h=HeaderHeight(g);int end=Count<0?Rows.Count:Math.Min(Rows.Count,Start+Count);for(int r=Start;r<end;r++)h+=RowHeight(g,Rows[r]);return h+8;}
  /* how many rows from Start fit in the space */
  internal int Fit(Graphics g,float space){float h=HeaderHeight(g)+8;int n=0;for(int r=Start;r<Rows.Count;r++){h+=RowHeight(g,Rows[r]);if(h>space)break;n++;}return n;}
  internal override void Draw(Graphics g,float y){float x=Left,hh=HeaderHeight(g);using(var head=new SolidBrush(PowTheme.Ink))g.FillRectangle(head,Left,y,Widths.Sum(),hh);using(var f=Bold)for(int c=0;c<Headers.Length;c++){g.DrawString(Headers[c],f,Brushes.White,new RectangleF(x+3,y+3,Widths[c]-6,hh));x+=Widths[c];}y+=hh;
   int end=Count<0?Rows.Count:Math.Min(Rows.Count,Start+Count);using(var f=Body)using(var line=new Pen(PowTheme.Line))for(int r=Start;r<end;r++){var row=Rows[r];float rh=RowHeight(g,row);if(Span(row)){using(var gb=new SolidBrush(Color.FromArgb(228,234,241)))g.FillRectangle(gb,Left,y,Widths.Sum(),rh);using(var b=Bold)using(var ink=new SolidBrush(PowTheme.Ink))g.DrawString(row[0],b,ink,new RectangleF(Left+3,y+2,Widths.Sum()-6,rh));g.DrawLine(line,Left,y+rh,Left+Widths.Sum(),y+rh);y+=rh;continue;}
    Color? tint=RowColor==null?null:RowColor(row);if(tint.HasValue)using(var b=new SolidBrush(tint.Value))g.FillRectangle(b,Left,y,Widths.Sum(),rh);else if((r-Start)%2==1)using(var b=new SolidBrush(Color.FromArgb(246,247,249)))g.FillRectangle(b,Left,y,Widths.Sum(),rh);
    x=Left;using(var ink=new SolidBrush(PowTheme.Ink))for(int c=0;c<Widths.Length&&c<row.Length;c++){if(c==PaintCol&&Painter!=null){var st=g.Save();try{var cell=new RectangleF(x+2,y+2,Widths[c]-4,rh-4);g.SetClip(cell);Painter(g,cell,r);}finally{g.Restore(st);}}else g.DrawString(row[c],f,ink,new RectangleF(x+3,y+2,Widths[c]-6,rh));x+=Widths[c];}g.DrawLine(line,Left,y+rh,Left+Widths.Sum(),y+rh);y+=rh;}}}
 /* a calculation-sheet line (owner, 24 November 2026: "pangit siguro yung table form"): number, member in bold, the formula with its values
    indented under it and the result on the right; a total has a rule above it. No grid. */
 sealed class CalcBlock:Block {internal string No,Label,Text,Result;internal bool Total;const float NoW=20,ResW=104,Indent=12;/* on the wide A3 page the line keeps an A4 width so the result stays near its formula */static float W{get{return Math.Min(Width,640);}}
  float LabelH(Graphics g){if(string.IsNullOrEmpty(Label))return 0;using(var b=Bold)return Measure(g,Label,b,W-NoW-ResW-6);}
  float TextH(Graphics g){if(string.IsNullOrEmpty(Text))return 0;using(var f=Total?Bold:Body)return Measure(g,Text,f,W-NoW-Indent-ResW-6);}
  internal override float Height(Graphics g){return LabelH(g)+TextH(g)+(Total?7:compactNow?4:6);}
  internal override void Draw(Graphics g,float y){float y0=y+(Total?4:1);using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted)){
   if(Total)using(var pen=new Pen(PowTheme.Ink,.8f))g.DrawLine(pen,Left+NoW,y+1.5f,Left+W,y+1.5f);
   using(var b=Bold){if(!string.IsNullOrEmpty(No))g.DrawString(No,b,muted,Left,y0);float lh=LabelH(g);if(lh>0)g.DrawString(Label,b,ink,new RectangleF(Left+NoW,y0,W-NoW-ResW-6,lh+2));
    float ty=y0+lh;float th=TextH(g);if(th>0)using(var f=Total?Bold:Body)g.DrawString(Text,f,Total?(Brush)ink:ink,new RectangleF(Left+NoW+(string.IsNullOrEmpty(Label)?0:Indent),ty,W-NoW-Indent-ResW-6,th+2));
    if(!string.IsNullOrEmpty(Result)){var fmt=new StringFormat{Alignment=StringAlignment.Far};float ry=th>0?ty+th-Measure(g,Result,b,ResW):y0;g.DrawString(Result,b,ink,new RectangleF(Left+W-ResW,Math.Max(y0,ry),ResW,40),fmt);}}}}}
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
 sealed class PageBreakBlock:Block {internal override float Height(Graphics g){return 0;}internal override void Draw(Graphics g,float y){}}
 /* ---- authoring ---- */
 public RichReport Heading(string text){blocks.Add(new HeadingBlock{Text=text});return this;}
 public RichReport Paragraph(string text,bool muted=false,bool strong=false){blocks.Add(new ParagraphBlock{Text=text,Muted=muted,Strong=strong});return this;}
 /* a figure drawn in report points on a 72 dpi bitmap 1400 px wide and placed on the page: fonts keep their point size and the result is
    the same on screen, in print and in the PDF */
 internal static Action<Graphics,RectangleF> Raster(Action<Graphics,RectangleF> draw){return (g,rc)=>{if(rc.Width<=1||rc.Height<=1)return;int w=1400;float k=w/rc.Width;int h=Math.Max(1,(int)(rc.Height*k));
  using(var bmp=new Bitmap(w,h)){bmp.SetResolution(72,72);using(var bg=Graphics.FromImage(bmp)){bg.Clear(Color.White);bg.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;bg.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;bg.ScaleTransform(k,k);draw(bg,new RectangleF(0,0,rc.Width,rc.Height));}
   var st=g.Save();g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;g.DrawImage(bmp,rc);g.Restore(st);}};}
 public RichReport Figure(float height,Action<Graphics,RectangleF> paint,string caption=null){blocks.Add(new FigureBlock{H=height,Paint=paint,Caption=caption});return this;}
 public RichReport Table(string[] headers,IEnumerable<string[]> rows,float[] widths,Func<string[],Color?> rowColor=null){float sum=widths.Sum();var w=widths.Select(x=>x/sum*Width).ToArray();blocks.Add(new TableBlock{Headers=headers,Rows=rows.ToList(),Widths=w,RowColor=rowColor});return this;}
 /* a table with one drawn column (a bar sketch in each row of a bar bending schedule) */
 public RichReport Table(string[] headers,IEnumerable<string[]> rows,float[] widths,int paintColumn,float minRowHeight,Action<Graphics,RectangleF,int> painter,Func<string[],Color?> rowColor=null){float sum=widths.Sum();var w=widths.Select(x=>x/sum*Width).ToArray();blocks.Add(new TableBlock{Headers=headers,Rows=rows.ToList(),Widths=w,RowColor=rowColor,PaintCol=paintColumn,MinRow=minRowHeight,Painter=painter});return this;}
 public RichReport Calc(string no,string label,string text,string result,bool total=false){blocks.Add(new CalcBlock{No=no,Label=label,Text=text,Result=result,Total=total});return this;}
 public RichReport KeyValues(IEnumerable<string[]> pairs){return Table(new[]{"Item","Value"},pairs,new float[]{38,62});}
 public RichReport Check(string name,string clause,IEnumerable<string> lines,string governing,double demand,double capacity,string unit,bool ok){blocks.Add(new CheckBlock{Name=name,Clause=clause,Lines=lines.ToList(),Governing=governing,Demand=demand,Capacity=capacity,Ratio=capacity<=0?double.PositiveInfinity:demand/capacity,Unit=unit,Ok=ok});return this;}
 public RichReport PageBreak(){blocks.Add(new PageBreakBlock());return this;}
 public RichReport Signatures(params string[] roles){blocks.Add(new SignatureBlock{Roles=roles});return this;}
 /* ---- layout into pages ---- */
 public CalculationDocument Build(){Frame(a3);compactNow=Compact;var doc=new CalculationDocument(title);if(a3)doc.UseA3Landscape();var pages=new List<List<KeyValuePair<Block,float>>>();var page=new List<KeyValuePair<Block,float>>();float y=Top;
  using(var bmp=new Bitmap(10,10))using(var g=Graphics.FromImage(bmp)){g.PageUnit=GraphicsUnit.Point;
   Action newPage=()=>{pages.Add(page);page=new List<KeyValuePair<Block,float>>();y=Top;};
   for(int bi=0;bi<blocks.Count;bi++){var block=blocks[bi];var table=block as TableBlock;if(block is PageBreakBlock){if(page.Count>0)newPage();continue;}
    /* a heading stays with the start of what follows it */bool keepNext=block is HeadingBlock||block is ParagraphBlock&&((ParagraphBlock)block).Strong&&bi+1<blocks.Count&&blocks[bi+1] is TableBlock;
    /* a heading or a bold table title stays with the start of what follows it (its header and first row) */if(keepNext&&bi+1<blocks.Count&&page.Count>0){var next=blocks[bi+1];var nt=next as TableBlock;float need=block.Height(g)+(nt!=null?new TableBlock{Headers=nt.Headers,Rows=nt.Rows,Widths=nt.Widths,RowColor=nt.RowColor,PaintCol=nt.PaintCol,MinRow=nt.MinRow,Painter=nt.Painter,Start=0,Count=Math.Min(1,nt.Rows.Count)}.Height(g):next.Height(g));if(block is HeadingBlock&&next is ParagraphBlock&&((ParagraphBlock)next).Strong&&bi+2<blocks.Count&&blocks[bi+2] is TableBlock){var t2=(TableBlock)blocks[bi+2];need+=new TableBlock{Headers=t2.Headers,Rows=t2.Rows,Widths=t2.Widths,RowColor=t2.RowColor,PaintCol=t2.PaintCol,MinRow=t2.MinRow,Painter=t2.Painter,Start=0,Count=Math.Min(1,t2.Rows.Count)}.Height(g);}if(y+need>Bottom)newPage();}
    if(table!=null){int start=0;while(start<table.Rows.Count||start==0){var part=new TableBlock{Headers=table.Headers,Rows=table.Rows,Widths=table.Widths,RowColor=table.RowColor,Start=start,PaintCol=table.PaintCol,MinRow=table.MinRow,Painter=table.Painter};int fit=part.Fit(g,Bottom-y);if(fit==0&&page.Count>0){newPage();fit=part.Fit(g,Bottom-y);}fit=Math.Max(1,fit);part.Count=fit;page.Add(new KeyValuePair<Block,float>(part,y));y+=part.Height(g);start+=fit;if(start>=table.Rows.Count)break;newPage();}continue;}
    float h=block.Height(g);if(y+h>Bottom&&page.Count>0){/* signatures take the paragraph before them along */KeyValuePair<Block,float>? carry=null;if(block is SignatureBlock&&page.Count>1&&page[page.Count-1].Key is ParagraphBlock){carry=page[page.Count-1];page.RemoveAt(page.Count-1);}newPage();if(carry.HasValue){page.Add(new KeyValuePair<Block,float>(carry.Value.Key,y));y+=carry.Value.Key.Height(g);}}page.Add(new KeyValuePair<Block,float>(block,y));y+=h;}
   if(page.Count>0)pages.Add(page);}
  bool big=a3,cmp=Compact;compactNow=false;foreach(var p in pages){var items=p;doc.Sheets.Add(g=>{Frame(big);compactNow=cmp;foreach(var item in items)item.Key.Draw(g,item.Value);compactNow=false;});}
  if(doc.Sheets.Count==0)doc.Sheets.Add(g=>{});return doc;}
}
}
