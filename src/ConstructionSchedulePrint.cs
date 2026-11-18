using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Printing;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Printing of the Construction Schedule (owner, 18 November 2026: "printable"): A4 landscape, text at least 7 pt (AGENTS.md
// report rule). Every page repeats the item columns; the working days are split over pages side by side when they do not
// fit one page width (each day keeps its own numbered column), and the lines over pages top to bottom.
sealed class SchedulePrintLayout {
 internal const float Margin=40,TitleH=46,RowH=15,HeadH=34,DayW=17;/* hundredths of an inch */
 internal static readonly float[] Widths={62,225,58,34,52,44};
 internal readonly List<ScheduleChart.Line> Lines;internal readonly int Total;internal readonly List<KeyValuePair<int,int>> DayBlocks=new List<KeyValuePair<int,int>>();internal readonly List<KeyValuePair<int,int>> LineBlocks=new List<KeyValuePair<int,int>>();
 internal SchedulePrintLayout(List<ScheduleRow> rows,int total,SizeF page){Lines=ScheduleChart.Lines(rows);Total=Math.Max(1,total);
  float daysWidth=page.Width-2*Margin-Widths.Sum();int perPage=Math.Max(5,(int)(daysWidth/DayW));for(int d=1;d<=Total;d+=perPage)DayBlocks.Add(new KeyValuePair<int,int>(d,Math.Min(Total,d+perPage-1)));
  int perLines=Math.Max(5,(int)((page.Height-2*Margin-TitleH-HeadH-20)/RowH));for(int i=0;i<Math.Max(1,Lines.Count);i+=perLines)LineBlocks.Add(new KeyValuePair<int,int>(i,Math.Min(Lines.Count,i+perLines)));}
 internal int Pages{get{return DayBlocks.Count*LineBlocks.Count;}}
 internal void DrawPage(Graphics g,int index,RectangleF bounds,string title){var lines=LineBlocks[index%LineBlocks.Count];var days=DayBlocks[index/LineBlocks.Count];
  using(var font=new Font("Segoe UI",7.5f))using(var bold=new Font("Segoe UI",7.5f,FontStyle.Bold))using(var big=new Font("Segoe UI",11f,FontStyle.Bold))using(var ink=new SolidBrush(PowTheme.Ink)){
   g.DrawString("CONSTRUCTION SCHEDULE (BAR CHART)",big,ink,bounds.Left+Margin,bounds.Top+Margin-8);
   g.DrawString(title+"   |   Total: "+Total+" working days (8 hours per day)   |   Days "+days.Key+" - "+days.Value+"   |   Page "+(index+1)+" of "+Pages,font,ink,bounds.Left+Margin,bounds.Top+Margin+14);
   ScheduleChart.Draw(g,Lines.GetRange(lines.Key,lines.Value-lines.Key),bounds.Left+Margin,bounds.Top+Margin+TitleH-8,Widths,DayW,RowH,HeadH,days.Key,days.Value,font,bold);}}
}
partial class ProgramOfWorksForm {
 void PrintSchedule(bool preview){Guard();var rows=ScheduleRows();int total=rows.Where(r=>r.Days>0).Select(r=>r.Finish).DefaultIfEmpty(0).Max();
  if(rows.Count==0){MessageBox.Show(this,"No pay item quantities to schedule yet.","Construction Schedule",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
  var info=ProjectInfoStore.Read();string title=info==null?"":string.Join(" - ",new[]{info.Name,info.Location}.Where(x=>!string.IsNullOrWhiteSpace(x)).ToArray());
  using(var doc=new PrintDocument{DocumentName="Construction Schedule"}){doc.DefaultPageSettings.Landscape=true;var a4=doc.PrinterSettings.PaperSizes.Cast<PaperSize>().FirstOrDefault(p=>p.Kind==PaperKind.A4);if(a4!=null)doc.DefaultPageSettings.PaperSize=a4;
   SchedulePrintLayout layout=null;int page=0;
   doc.BeginPrint+=(s,e)=>{page=0;layout=null;};
   doc.PrintPage+=(s,e)=>{var bounds=new RectangleF(0,0,e.PageBounds.Width,e.PageBounds.Height);if(layout==null)layout=new SchedulePrintLayout(rows,total,bounds.Size);layout.DrawPage(e.Graphics,page,bounds,title);page++;e.HasMorePages=page<layout.Pages;};
   if(preview){using(var dialog=new PrintPreviewDialog{Document=doc,Width=1200,Height=850,StartPosition=FormStartPosition.CenterParent}){dialog.ShowDialog(this);}}
   else{using(var dialog=new PrintDialog{Document=doc,UseEXDialog=true}){if(dialog.ShowDialog(this)==DialogResult.OK)doc.Print();}}}}
}
}
