using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Printing;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
// Printing of the Construction Schedule (owner, 18 November 2026: "printable"): A4 landscape, text at least 7 pt (AGENTS.md
// report rule). Every page repeats the item columns; the working days are split over pages side by side when they do not
// fit one page width (each day keeps its own numbered column), and the lines over pages top to bottom.
/* PDM pages: the network laid out to the page height, split between columns over pages side by side */
sealed class PdmPrintLayout {
 internal readonly PdmNetwork Network;internal readonly List<KeyValuePair<float,float>> Ranges=new List<KeyValuePair<float,float>>();const float Margin=40,TitleH=60;
 internal PdmPrintLayout(PdmNetwork network,SizeF page){Network=network;network.Layout(page.Height-2*Margin-TitleH);float avail=page.Width-2*Margin;
  var cols=network.Nodes.Select(n=>new KeyValuePair<float,float>(n.X,PdmNetwork.NodeW)).Concat(network.Milestones.Select(m=>new KeyValuePair<float,float>(m.X,PdmNetwork.MileW))).GroupBy(c=>c.Key).Select(g=>new KeyValuePair<float,float>(g.Key,g.Max(c=>c.Value))).OrderBy(c=>c.Key).ToList();
  int i=0;while(i<cols.Count){float start=cols[i].Key-6,end=cols[i].Key+cols[i].Value;i++;while(i<cols.Count&&cols[i].Key+cols[i].Value-start<=avail){end=cols[i].Key+cols[i].Value;i++;}Ranges.Add(new KeyValuePair<float,float>(start,end+6));}if(Ranges.Count==0)Ranges.Add(new KeyValuePair<float,float>(0,avail));}
 internal int Pages{get{return Ranges.Count;}}
 internal void DrawPage(Graphics g,int index,RectangleF bounds,string title){var range=Ranges[index];
  using(var font=new Font("Segoe UI",7.5f))using(var bold=new Font("Segoe UI",7.5f,FontStyle.Bold))using(var big=new Font("Segoe UI",11f,FontStyle.Bold))using(var ink=new SolidBrush(PowTheme.Ink)){
   g.DrawString("PRECEDENCE DIAGRAM (PDM)",big,ink,bounds.Left+Margin,bounds.Top+Margin-8);
   g.DrawString(title+"   |   Project duration: "+Network.Total+" working days (8 hours per day)   |   Page "+(index+1)+" of "+Pages,font,ink,bounds.Left+Margin,bounds.Top+Margin+14);
   PdmNetwork.Legend(g,bounds.Left+Margin,bounds.Top+Margin+28,font);
   var state=g.Save();g.SetClip(new RectangleF(bounds.Left+Margin,bounds.Top+Margin+TitleH-6,range.Value-range.Key,bounds.Height-2*Margin-TitleH+6));Network.Draw(g,bounds.Left+Margin-range.Key,bounds.Top+Margin+TitleH,font,bold);g.Restore(state);}}
}
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
 void PrintSchedule(bool preview){Guard();var rows=ScheduleRows();int total=rows.Where(r=>r.Days>0).Select(r=>r.Finish).DefaultIfEmpty(0).Max();var critical=new HashSet<ScheduleRow>(PdmNetwork.Build(rows).Nodes.Where(x=>x.Critical).Select(x=>x.Row));foreach(var r in rows)r.Critical=critical.Contains(r);
  if(rows.Count==0){MessageBox.Show(this,"No pay item quantities to schedule yet.","Construction Schedule",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
  var info=ProjectInfoStore.Read();string title=info==null?"":string.Join(" - ",new[]{info.Name,info.Location}.Where(x=>!string.IsNullOrWhiteSpace(x)).ToArray());
  using(var doc=new PrintDocument{DocumentName="Construction Schedule"}){doc.DefaultPageSettings.Landscape=true;var a4=doc.PrinterSettings.PaperSizes.Cast<PaperSize>().FirstOrDefault(p=>p.Kind==PaperKind.A4);if(a4!=null)doc.DefaultPageSettings.PaperSize=a4;
   bool pdm=scheduleViews!=null&&scheduleViews.SelectedIndex==1;SchedulePrintLayout layout=null;PdmPrintLayout network=null;int page=0;
   doc.BeginPrint+=(s,e)=>{page=0;layout=null;network=null;};
   doc.PrintPage+=(s,e)=>{var bounds=new RectangleF(0,0,e.PageBounds.Width,e.PageBounds.Height);int pages;
    if(pdm){if(network==null)network=new PdmPrintLayout(PdmNetwork.Build(rows),bounds.Size);network.DrawPage(e.Graphics,page,bounds,title);pages=network.Pages;}
    else{if(layout==null)layout=new SchedulePrintLayout(rows,total,bounds.Size);layout.DrawPage(e.Graphics,page,bounds,title);pages=layout.Pages;}
    page++;e.HasMorePages=page<pages;if(!e.HasMorePages&&pdm&&pdmView!=null)pdmView.Fit();};
   if(preview){using(var dialog=new PrintPreviewDialog{Document=doc,Width=1200,Height=850,StartPosition=FormStartPosition.CenterParent}){dialog.ShowDialog(this);}}
   else{using(var dialog=new PrintDialog{Document=doc,UseEXDialog=true}){if(dialog.ShowDialog(this)==DialogResult.OK)doc.Print();}}}}
}
}
