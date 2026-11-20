using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.IO;using System.Linq;
namespace BeamScheduleFormatter {
/* Draws a block of a POW worksheet in points (1/72 in), shared by the on-screen view and printing, so the printout looks like the
   screen and like Excel: cell fills, borders, merged cells, wrapped / overflowing text, number formats and pictures. */
sealed class WbRender {
 public readonly WbBook Book;public readonly WbCalc Calc;readonly Dictionary<string,Font> fonts=new Dictionary<string,Font>();readonly Dictionary<WbPicture,Image> images=new Dictionary<WbPicture,Image>();
 public Func<WbSheet,int,bool> RowHidden;
 public WbRender(WbBook book,WbCalc calc){Book=book;Calc=calc;}
 public double ColWidthPt(WbSheetLayout l,int c){return WbSheetLayout.WidthPt(l.Width(c));}
 public double RowHeightPt(WbSheet s,WbSheetLayout l,int r){if(RowHidden!=null&&RowHidden(s,r))return 0;return l.Height(r);}
 public object Value(WbSheet s,int r,int c){var cell=s.Get(r,c);if(cell==null)return null;return Calc!=null&&(cell.Formula!=null||cell.ArrayMaster!=null)?Calc.CellValue(s,cell):cell.Cached;}
 Font FontFor(WbFont f,float scale){string key=f.Name+"|"+f.Size+"|"+f.Bold+f.Italic+f.Underline+f.Strike+"|"+scale;Font font;if(fonts.TryGetValue(key,out font))return font;var style=FontStyle.Regular;if(f.Bold)style|=FontStyle.Bold;if(f.Italic)style|=FontStyle.Italic;if(f.Underline)style|=FontStyle.Underline;if(f.Strike)style|=FontStyle.Strikeout;
  try{font=new Font(f.Name,Math.Max(1f,f.Size*scale),style,GraphicsUnit.World);}catch(ArgumentException){font=new Font(FontFamily.GenericSansSerif,Math.Max(1f,f.Size*scale),style,GraphicsUnit.World);}fonts[key]=font;return font;}
 /* Sets up a Graphics so one world unit is one point (pixels + scale transform: text lands in the same place on every GDI+ build) */
 public static void PointSpace(Graphics g,float zoom){g.PageUnit=GraphicsUnit.Pixel;g.ScaleTransform(g.DpiX/72f*zoom,g.DpiY/72f*zoom);}
 public static float LineHeight(Font f){var fam=f.FontFamily;int em=fam.GetEmHeight(f.Style);return em>0?f.Size*fam.GetLineSpacing(f.Style)/em:f.Size*1.15f;}
 /* Draws rows r1..r2 and columns c1..c2 with the top-left corner at (x0,y0) points; call PointSpace first. */
 public void Draw(Graphics g,WbSheet s,int r1,int c1,int r2,int c2,float x0,float y0,float textScale=1){var l=Book.Layout(s);var st=Book.Styles;if(st==null)return;
  var xs=new float[c2-c1+2];xs[0]=x0;for(int c=c1;c<=c2;c++)xs[c-c1+1]=xs[c-c1]+(float)ColWidthPt(l,c);
  var ys=new float[r2-r1+2];ys[0]=y0;for(int r=r1;r<=r2;r++)ys[r-r1+1]=ys[r-r1]+(float)RowHeightPt(s,l,r);
  Func<int,float> X=c=>c<c1?xs[0]-(float)Enumerable.Range(c,c1-c).Sum(k=>ColWidthPt(l,k)):c>c2+1?xs[xs.Length-1]+(float)Enumerable.Range(c2+1,c-c2-1).Sum(k=>ColWidthPt(l,k)):xs[c-c1];
  Func<int,float> Y=r=>r<r1?ys[0]-(float)Enumerable.Range(r,r1-r).Sum(k=>RowHeightPt(s,l,k)):r>r2+1?ys[ys.Length-1]+(float)Enumerable.Range(r2+1,r-r2-1).Sum(k=>RowHeightPt(s,l,k)):ys[r-r1];
  /* fills */
  var done=new HashSet<WbMerge>();
  for(int r=r1;r<=r2;r++){if(ys[r-r1+1]<=ys[r-r1])continue;for(int c=c1;c<=c2;c++){if(xs[c-c1+1]<=xs[c-c1])continue;WbMerge m;RectangleF rect;int sr=r,sc=c;
    if(l.MergeAt.TryGetValue(WbSheet.Key(r,c),out m)){if(done.Contains(m))continue;done.Add(m);sr=m.R1;sc=m.C1;rect=RectangleF.FromLTRB(X(m.C1),Y(m.R1),X(m.C2+1),Y(m.R2+1));}else rect=RectangleF.FromLTRB(xs[c-c1],ys[r-r1],xs[c-c1+1],ys[r-r1+1]);
    var style=st.Xf[Math.Min(l.StyleAt(sr,sc),st.Xf.Count-1)];if(style.Fill.HasValue){using(var b=new SolidBrush(style.Fill.Value))g.FillRectangle(b,rect);}}}
  /* borders */
  for(int r=r1;r<=r2;r++){if(ys[r-r1+1]<=ys[r-r1])continue;for(int c=c1;c<=c2;c++){if(xs[c-c1+1]<=xs[c-c1])continue;var style=st.Xf[Math.Min(l.StyleAt(r,c),st.Xf.Count-1)];float L=xs[c-c1],R=xs[c-c1+1],T=ys[r-r1],B=ys[r-r1+1];
    /* inside a merged block only the block's outer edges carry borders, as in Excel */WbMerge mg;bool inM=l.MergeAt.TryGetValue(WbSheet.Key(r,c),out mg);
    if(!inM||c==mg.C1)Line(g,style.Left,L,T,L,B);if(!inM||c==mg.C2)Line(g,style.Right,R,T,R,B);if(!inM||r==mg.R1)Line(g,style.Top,L,T,R,T);if(!inM||r==mg.R2)Line(g,style.Bottom,L,B,R,B);}}
  /* text */
  done.Clear();
  for(int r=r1;r<=r2;r++){if(ys[r-r1+1]<=ys[r-r1])continue;for(int c=c1;c<=c2;c++){WbMerge m;RectangleF rect;int sr=r,sc=c;bool merged=false;
    if(l.MergeAt.TryGetValue(WbSheet.Key(r,c),out m)){if(done.Contains(m))continue;done.Add(m);sr=m.R1;sc=m.C1;merged=true;rect=RectangleF.FromLTRB(X(m.C1),Y(m.R1),X(m.C2+1),Y(m.R2+1));}else{if(xs[c-c1+1]<=xs[c-c1])continue;rect=RectangleF.FromLTRB(xs[c-c1],ys[r-r1],xs[c-c1+1],ys[r-r1+1]);}
    var v=Value(s,sr,sc);if(v==null||(v is string&&(string)v==""))continue;var style=st.Xf[Math.Min(l.StyleAt(sr,sc),st.Xf.Count-1)];DrawText(g,s,l,style,v,rect,merged,sr,sc,X,textScale);}}
  /* pictures */
  foreach(var p in l.Pictures){if(p.Row>r2+1||(p.Row2>0?p.Row2:p.Row)<r1||p.Col>c2+1||(p.Col2>0?p.Col2:p.Col)<c1)continue;Image img;if(!images.TryGetValue(p,out img)){try{img=Image.FromStream(new MemoryStream(p.Data));}catch(ArgumentException){img=null;}images[p]=img;}if(img==null)continue;
   float px=X(p.Col)+(float)p.ColOff,py=Y(p.Row)+(float)p.RowOff;float pw,ph;if(p.Col2>0){pw=X(p.Col2)+(float)p.Col2Off-px;ph=Y(p.Row2)+(float)p.Row2Off-py;}else{pw=(float)p.WidthPt;ph=(float)p.HeightPt;}if(pw>0&&ph>0)g.DrawImage(img,px,py,pw,ph);}
  }
 static void Line(Graphics g,WbBorderSide side,float x1,float y1,float x2,float y2){if(side==null||side.Style==null||side.Style=="none")return;float w=0.5f;DashStyle dash=DashStyle.Solid;
  switch(side.Style){case "medium":case "mediumDashed":case "mediumDashDot":case "mediumDashDotDot":w=1.2f;break;case "thick":w=1.8f;break;case "double":w=1.5f;break;case "hair":w=0.3f;break;}
  if(side.Style.Contains("Dash")||side.Style=="dashed")dash=DashStyle.Dash;if(side.Style=="dotted"||side.Style=="hair")dash=DashStyle.Dot;if(side.Style.Contains("DashDot"))dash=DashStyle.DashDot;
  using(var pen=new Pen(side.Color,w){DashStyle=dash})g.DrawLine(pen,x1,y1,x2,y2);}
 void DrawText(Graphics g,WbSheet s,WbSheetLayout l,WbStyle style,object v,RectangleF rect,bool merged,int r,int c,Func<int,float> X,float scale){
  var f=WbNumberFormat.Format(v,style.Format);if(f.Text==""&&f.Left=="")return;var font=FontFor(style.Font,scale);var color=f.Color??style.Font.Color;
  string h=style.HAlign;if(h=="general")h=v is double?"right":v is bool||v is WbError?"center":"left";if(h=="centerContinuous")h="center";if(h=="fill"||h=="justify"||h=="distributed")h=h=="distributed"?"center":"left";
  var fmt=new StringFormat(StringFormat.GenericTypographic){Trimming=StringTrimming.None};fmt.FormatFlags|=StringFormatFlags.MeasureTrailingSpaces;if(!style.Wrap)fmt.FormatFlags|=StringFormatFlags.NoWrap;
  fmt.Alignment=h=="right"?StringAlignment.Far:h=="center"?StringAlignment.Center:StringAlignment.Near;fmt.LineAlignment=style.VAlign=="top"?StringAlignment.Near:style.VAlign=="center"||style.VAlign=="distributed"||style.VAlign=="justify"?StringAlignment.Center:StringAlignment.Far;
  float pad=1.5f*scale;var area=RectangleF.FromLTRB(rect.Left+pad+style.Indent*7.5f*scale*(h=="left"?1:0),rect.Top,rect.Right-pad-style.Indent*7.5f*scale*(h=="right"?1:0),rect.Bottom);
  if(style.Shrink&&!style.Wrap){var size=g.MeasureString(f.Left+f.Text,font,PointF.Empty,fmt);if(size.Width>area.Width&&size.Width>0){float k=area.Width/size.Width;font=FontFor(new WbFont{Name=style.Font.Name,Size=style.Font.Size*k,Bold=style.Font.Bold,Italic=style.Font.Italic},scale);}}
  /* text overflow into empty neighbours (not for numbers, wrapped or merged cells) */
  if(!style.Wrap&&!merged&&!(v is double)&&f.Left==""){var size=g.MeasureString(f.Text,font,PointF.Empty,fmt);if(size.Width>area.Width){float left=rect.Left,right=rect.Right;
    if(h=="left"){int k=c+1;while(right-rect.Left<size.Width+2*pad&&k<=l.MaxCol+50&&IsEmpty(s,l,r,k)){right=X(k+1);k++;}}
    else if(h=="right"){int k=c-1;while(rect.Right-left<size.Width+2*pad&&k>=1&&IsEmpty(s,l,r,k)){left=X(k);k--;}}
    else{int a=c-1,b=c+1;while(right-left<size.Width+2*pad&&(a>=1&&IsEmpty(s,l,r,a)||IsEmpty(s,l,r,b))){if(a>=1&&IsEmpty(s,l,r,a)){left=X(a);a--;}if(IsEmpty(s,l,r,b)){right=X(b+1);b++;}if(a<1&&!IsEmpty(s,l,r,b))break;}}
    area=RectangleF.FromLTRB(left+pad,rect.Top,right-pad,rect.Bottom);}}
  
  fmt.FormatFlags|=StringFormatFlags.NoClip;
  using(var b=new SolidBrush(color)){
   if(style.Wrap){/* manual word wrap: drawing into a rectangle clips wrongly on some GDI+ builds */var lines=Wrap(g,f.Left+f.Text,font,area.Width);float lh0=LineHeight(font);float total=lh0*lines.Count;
    float y0=fmt.LineAlignment==StringAlignment.Near?area.Top:fmt.LineAlignment==StringAlignment.Center?area.Top+(area.Height-total)/2:area.Bottom-total;var pf=new StringFormat(StringFormat.GenericTypographic);pf.FormatFlags|=StringFormatFlags.NoWrap|StringFormatFlags.MeasureTrailingSpaces;
    for(int i=0;i<lines.Count;i++){float yy=y0+i*lh0;if(yy+lh0<area.Top-0.5f||yy>area.Bottom+0.5f)continue;float lw=g.MeasureString(lines[i],font,PointF.Empty,pf).Width;float xx=fmt.Alignment==StringAlignment.Near?area.Left:fmt.Alignment==StringAlignment.Center?area.Left+(area.Width-lw)/2:area.Right-lw;Text(g,lines[i],font,b,xx,yy,pf);}return;}
   string text=f.Text;if(v is double&&f.Left==""&&!f.Fill){var w0=g.MeasureString(text,font,PointF.Empty,fmt).Width;if(w0>area.Width+0.5f){float hw=Math.Max(0.5f,g.MeasureString("#",font,PointF.Empty,fmt).Width);text=new string('#',Math.Max(1,(int)(area.Width/hw)));}}
   float lh=LineHeight(font);float y=fmt.LineAlignment==StringAlignment.Near?area.Top:fmt.LineAlignment==StringAlignment.Center?area.Top+(area.Height-lh)/2:area.Bottom-lh;
   var plain=new StringFormat(fmt){Alignment=StringAlignment.Near,LineAlignment=StringAlignment.Near};
   if(f.Left!=""){Text(g,f.Left,font,b,area.Left,y,plain);float w=g.MeasureString(text,font,PointF.Empty,plain).Width;Text(g,text,font,b,area.Right-w,y,plain);}
   else{float w=g.MeasureString(text,font,PointF.Empty,plain).Width;if(!(v is double)&&w>area.Width+2*pad&&text.Length>1){/* Excel cuts text at the next filled cell */int lo=0,hi=text.Length;while(lo<hi){int mid=(lo+hi+1)/2;if(g.MeasureString(text.Substring(0,mid),font,PointF.Empty,plain).Width<=area.Width+2*pad)lo=mid;else hi=mid-1;}text=fmt.Alignment==StringAlignment.Far?text.Substring(text.Length-lo):text.Substring(0,lo);w=g.MeasureString(text,font,PointF.Empty,plain).Width;}float x=fmt.Alignment==StringAlignment.Near?area.Left:fmt.Alignment==StringAlignment.Center?area.Left+(area.Width-w)/2:area.Right-w;Text(g,text,font,b,x,y,plain);}}
  }
 /* text at a point, drawn through a layout rectangle (position stays right in page units on every GDI+ build) */
 static void Text(Graphics g,string text,Font font,Brush b,float x,float y,StringFormat f){var sf=new StringFormat(f);sf.FormatFlags|=StringFormatFlags.NoClip|StringFormatFlags.NoWrap;sf.Alignment=StringAlignment.Near;sf.LineAlignment=StringAlignment.Near;g.DrawString(text,font,b,new RectangleF(x,y,100000f,LineHeight(font)*1.5f),sf);}
 static List<string> Wrap(Graphics g,string text,Font font,float width){var res=new List<string>();var pf=new StringFormat(StringFormat.GenericTypographic);pf.FormatFlags|=StringFormatFlags.NoWrap;
  foreach(var para in text.Replace("\r","").Split('\n')){var words=para.Split(' ');string line="";foreach(var w in words){string t=line==""?w:line+" "+w;if(line!=""&&g.MeasureString(t,font,PointF.Empty,pf).Width>width){res.Add(line);line=w;}else line=t;}res.Add(line);}return res;}
 bool IsEmpty(WbSheet s,WbSheetLayout l,int r,int c){if(l.MergeAt.ContainsKey(WbSheet.Key(r,c)))return false;var v=Value(s,r,c);return v==null||(v is string&&(string)v=="");}
}
}
