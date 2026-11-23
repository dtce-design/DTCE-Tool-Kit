using System;using System.Collections.Generic;using System.Drawing;using System.Linq;
namespace BeamScheduleFormatter {
// Drawn PDF/print pages for the exposure/Kzt explanation and STAAD wind data. Rows grow to fit wrapped text.
static class WindAuditPages {
 class Block {public string Title,Text;public string[] Headers;public float[] Widths;public List<string[]> Rows;}
 const float Left=CalculationDocument.L,Width=CalculationDocument.CW,Top=130,Bottom=790;
 static float Measure(string text,float width,float size,bool bold){using(var bmp=new Bitmap(1,1))using(var g=Graphics.FromImage(bmp)){g.PageUnit=GraphicsUnit.Point;using(var font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Point))return g.MeasureString(text??"",font,(int)Math.Max(10,width)).Height;}}
 static float RowHeight(string[] row,float[] widths,bool bold){float h=0;for(int c=0;c<widths.Length;c++)h=Math.Max(h,Measure(c<row.Length?row[c]:"",widths[c]-8,7f,bold));return h+6;}
 internal static void Add(CalculationDocument doc,List<WindDirection> directions,List<WindStaadBand> bands,string commands){
  var blocks=new List<Block>();
  foreach(var d in directions){if(d.ExposureAudit==null&&d.TopographicChecks.Count==0)continue;blocks.Add(new Block{Title="EXPOSURE AND Kzt BASIS / WIND FROM "+d.Name,Text="Exposure "+d.Exposure+"; Kzt = "+(WindTin.Finite(d.Kzt)?d.Kzt.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture):"unresolved")+". "+(d.ExposureAudit==null?"":d.ExposureAudit.Reason)});
   if(d.ExposureAudit!=null)blocks.Add(new Block{Headers=new[]{"Exposure check","Value","Basis / decision"},Widths=new float[]{130,120,273},Rows=d.ExposureAudit.Checks.Select(r=>r.ToArray()).ToList()});
   if(d.TopographicChecks.Count>0)blocks.Add(new Block{Headers=new[]{"Kzt check (207A.8)","Value","Basis / decision"},Widths=new float[]{130,150,243},Rows=d.TopographicChecks.Select(r=>r.ToArray()).ToList()});}
  if(bands!=null&&bands.Count>0){blocks.Add(new Block{Title="STAAD WIND INPUT / WALL INTENSITIES",Text="INT = windward qz·G·Cp at the band top + |leeward qh·G·Cp| (kN/m²). Internal pressure cancels across opposite walls. Roof pressures are applied separately. Do not apply G and Cp again in STAAD."});
   blocks.Add(new Block{Headers=new[]{"Wind from / axis","Band top z m","Windward kPa","Leeward kPa","INT kPa"},Widths=new float[]{160,85,95,90,93},Rows=bands.Select(b=>new[]{b.Axis,b.Top.ToString("0.###"),b.Windward.ToString("0.###"),b.Leeward.ToString("0.###"),b.Combined.ToString("0.###")}).ToList()});
   blocks.Add(new Block{Headers=new[]{"STAAD commands (UNIT METER KN)"},Widths=new float[]{CalculationDocument.CW},Rows=commands.Split('\n').Select(l=>new[]{l}).ToList()});}
  // Paginate blocks into sheets; table rows never split.
  var pages=new List<List<Action<Graphics,float>>>();var current=new List<Action<Graphics,float>>();float y=Top;var offsets=new List<List<float>>();var currentOffsets=new List<float>();
  Action newPage=()=>{pages.Add(current);offsets.Add(currentOffsets);current=new List<Action<Graphics,float>>();currentOffsets=new List<float>();y=Top;};
  foreach(var block in blocks){
   if(block.Title!=null){float th=Measure(block.Text,Width,7f,false)+40;if(y>Top&&y+th+60>Bottom)newPage();var captured=block;current.Add((g,at)=>{CalculationDocument.Section(g,captured.Title,at);CalculationDocument.Text(g,captured.Text,Left,at+32,Width,th-34,7f);});currentOffsets.Add(y);y+=th+4;}
   if(block.Rows!=null){float header=RowHeight(block.Headers,block.Widths,true);int start=0;while(start<block.Rows.Count){if(y+header+RowHeight(block.Rows[start],block.Widths,false)>Bottom)newPage();int end=start;float h=header;while(end<block.Rows.Count){float rh=RowHeight(block.Rows[end],block.Widths,false);if(y+h+rh>Bottom)break;h+=rh;end++;}var rows=block.Rows.Skip(start).Take(end-start).ToList();var b=block;current.Add((g,at)=>Table(g,b.Headers,rows,b.Widths,at));currentOffsets.Add(y);y+=h+10;start=end;}}
  }
  if(current.Count>0){pages.Add(current);offsets.Add(currentOffsets);}
  for(int p=0;p<pages.Count;p++){var actions=pages[p];var at=offsets[p];doc.Sheets.Add(g=>{for(int i=0;i<actions.Count;i++)actions[i](g,at[i]);});}
 }
 static void Table(Graphics g,string[] headers,List<string[]> rows,float[] widths,float y){float hh=RowHeight(headers,widths,true);float x=Left;for(int c=0;c<headers.Length;c++){using(var brush=new SolidBrush(PowTheme.Ink))g.FillRectangle(brush,x,y,widths[c],hh);Cell(g,headers[c],x,y,widths[c],hh,true,Color.White);x+=widths[c];}y+=hh;
  for(int r=0;r<rows.Count;r++){float rh=RowHeight(rows[r],widths,false);x=Left;using(var brush=new SolidBrush(r%2==0?Color.White:ModernUi.Soft))g.FillRectangle(brush,Left,y,widths.Sum(),rh);for(int c=0;c<widths.Length;c++){Cell(g,c<rows[r].Length?rows[r][c]:"",x,y,widths[c],rh,false,PowTheme.Ink);x+=widths[c];}using(var pen=new Pen(PowTheme.Line,.5f))g.DrawLine(pen,Left,y+rh,Left+widths.Sum(),y+rh);y+=rh;}}
 static void Cell(Graphics g,string text,float x,float y,float w,float h,bool bold,Color color){using(var font=new Font("Segoe UI",7f,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Point))using(var brush=new SolidBrush(color))g.DrawString(text??"",font,brush,new RectangleF(x+4,y+3,w-8,h-3));}
}
}
