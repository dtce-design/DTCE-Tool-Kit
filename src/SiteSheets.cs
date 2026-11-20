using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Globalization;using System.Linq;
namespace BeamScheduleFormatter {
/* Site sheets for the rebar crew (owner, 19 November 2026: "Wala na bang masmaganda at mas maayos na presentation para magets ng
   mga construction workers?"). One A3 packet, one sheet per job: how to read it, CUTTING (each commercial bar drawn with its cut
   marks, the count in big type and boxes to tick), BENDING (one card per bar mark with the shape drawn large and the length of
   every leg written on it, quantity and cut length, colour by bar size) and PLACING (the elevation strips and sections).
   Labels are in English (owner, 20 November 2026: "English BBS report and cutting list"). */
static class SiteSheets {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 /* one colour per bar size, used on every site sheet */
 internal static Color SizeColor(int dia){switch(dia){case 8:return Color.FromArgb(120,120,120);case 10:return Color.FromArgb(110,110,125);case 12:return Color.FromArgb(215,130,20);case 16:return Color.FromArgb(30,100,190);case 20:return Color.FromArgb(200,45,45);case 25:return Color.FromArgb(30,140,70);case 28:return Color.FromArgb(130,60,160);case 32:return Color.FromArgb(140,90,40);default:return Color.FromArgb(40,40,40);}}
 static Color Light(Color c,int a=60){return Color.FromArgb(a,c);}
 static string Mm(double m){return (m*1000).ToString("#,0",Inv);}
 static Font F(float pt,bool bold=false){return new Font("Segoe UI",pt,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Point);}
 static void Boxes(Graphics g,float x,float y,int count,int max,Font f,Brush ink){using(var pen=new Pen(PowTheme.Ink,.7f)){int n=Math.Min(count,max);for(int k=0;k<n;k++)g.DrawRectangle(pen,x+(k%10)*11,y+(k/10)*11,8,8);if(count>max)g.DrawString("+"+(count-max)+" more",f,ink,x,y+((n+9)/10)*11);}}
 public static CalculationDocument Build(CutResult r,CutSettings s,BbsInfo info){if(r==null)throw new Exception("Nothing to print yet: load a schedule and enter the beam lines.");
  var rep=new RichReport("Rebar Site Sheets - Beams (cutting, bending, placing)",true);AddCrew(rep,r,s,info,true);return rep.Build();}
 /* the crew sheets; in the combined BBS document the placing drawings are those of section 3 */
 internal static void AddCrew(RichReport rep,CutResult r,CutSettings s,BbsInfo info,bool placing){float width=RichReport.ContentWidth(true);var shown=r.Pieces.Where(p=>p.PerLine>0).ToList();
  /* how to read */
  rep.Heading("HOW TO READ").Figure(150,(g,rect)=>{using(var big=F(11,true))using(var f=F(8.5f))using(var b=F(8.5f,true))using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted)){
    g.DrawString("All sizes in millimetres (mm)   BBS "+info.Number+" rev. "+info.Revision+" - "+info.Status,big,ink,rect.Left+10,rect.Top+8);
    string[] steps={"1  CUTTING - each line is one commercial bar. Cut at the marks; the number in the box is how many bars to cut that way. Tick a box for every bar done. Keep offcuts marked (keep).","2  BENDING - one card per bar mark. Bend to the drawn shape; the numbers on the shape are the outside lengths of each leg. Hooks of hoops are 135°, other hooks 90°.","3  PLACING - the elevation shows where each bar mark goes and where the laps are; the sections show the bars across the beam."};
    float y=rect.Top+30;foreach(var t in steps){g.DrawString(t,f,ink,new RectangleF(rect.Left+10,y,rect.Width*.62f,40));y+=g.MeasureString(t,f,(int)(rect.Width*.62f)).Height+6;}
    float x=rect.Left+rect.Width*.66f;g.DrawString("BAR SIZE COLOURS",b,ink,x,rect.Top+30);int i=0;foreach(int d in shown.Select(p=>p.Diameter).Distinct().OrderBy(v=>v)){float yy=rect.Top+48+i*18;using(var c=new SolidBrush(SizeColor(d)))g.FillRectangle(c,x,yy,34,12);g.DrawString("Ø"+d+" mm  (Grade "+shown.First(p=>p.Diameter==d).Grade+")",f,ink,x+42,yy-1);i++;}
    g.DrawString("Check every bar against the approved drawings. Report doubts to the engineer before cutting.",f,muted,rect.Left+10,rect.Bottom-18);}});
  /* cutting */
  var patterns=r.Patterns;double longest=patterns.Count==0?12:patterns.Max(p=>p.Stock);
  foreach(var bySize in patterns.GroupBy(p=>p.Diameter).OrderBy(x=>x.Key)){var list=bySize.ToList();int total=list.Sum(p=>p.Bars);
   rep.Heading("CUTTING  -  Ø"+bySize.Key+" mm  ("+total+" commercial bars: "+string.Join(", ",list.GroupBy(p=>p.Stock).OrderByDescending(x=>x.Key).Select(x=>x.Sum(p=>p.Bars)+" x "+x.Key.ToString("0.0#",Inv)+" m"))+")");
   const float rowH=58;int perFig=Math.Max(1,(int)(560/rowH));for(int start=0;start<list.Count;start+=perFig){var part=list.Skip(start).Take(perFig).ToList();
    rep.Figure(part.Count*rowH+20,(g,rect)=>{g.SmoothingMode=SmoothingMode.AntiAlias;var color=SizeColor(bySize.Key);using(var big=F(14,true))using(var mid=F(9,true))using(var f=F(7.5f))using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted))using(var edge=new Pen(PowTheme.Ink,.8f)){
     float barX=rect.Left+150,barW=rect.Width-150-150-130;for(int k=0;k<part.Count;k++){var p=part[k];float y=rect.Top+6+k*rowH;
      g.DrawString(p.Id,mid,ink,rect.Left+8,y);g.DrawString(p.Stock.ToString("0.0#",Inv)+" m bar",mid,ink,rect.Left+8,y+14);g.DrawString("Ø"+p.Diameter+" G"+p.Grade,f,muted,rect.Left+8,y+30);
      using(var chip=new SolidBrush(color))g.FillRectangle(chip,rect.Left+86,y+2,56,30);{var t="x "+p.Bars;var sz=g.MeasureString(t,big);g.DrawString(t,big,Brushes.White,rect.Left+114-sz.Width/2,y+17-sz.Height/2);}
      float scale=(float)(barW/longest),bx=barX,by=y+10,bh=18;var cuts=p.Cuts.SelectMany(c=>Enumerable.Repeat(c.Length,c.Count)).ToList();
      foreach(var len in cuts){float w=(float)(len*scale);using(var fill=new SolidBrush(Light(color,70)))g.FillRectangle(fill,bx,by,w,bh);g.DrawRectangle(edge,bx,by,w,bh);var t=Mm(len);var sz=g.MeasureString(t,mid);if(w>sz.Width+4)g.DrawString(t,mid,ink,bx+w/2-sz.Width/2,by+bh/2-sz.Height/2);else{var sz2=g.MeasureString(t,f);g.DrawString(t,f,ink,bx+w/2-sz2.Width/2,by-12);}
       using(var cut=new Pen(Color.FromArgb(200,40,40),1.6f))g.DrawLine(cut,bx+w,by-5,bx+w,by+bh+5);bx+=w;}
      float ow=(float)(p.Offcut*scale);if(ow>.5f){using(var hatch=new HatchBrush(HatchStyle.ForwardDiagonal,Color.FromArgb(160,160,160),Color.White))g.FillRectangle(hatch,bx,by,ow,bh);g.DrawRectangle(edge,bx,by,ow,bh);}
      var off="offcut "+Mm(p.Offcut)+(p.Offcut>=CuttingPatterns.ReusableOffcut?"  KEEP":"");g.DrawString(off,f,p.Offcut>=CuttingPatterns.ReusableOffcut?ink:muted,barX+(float)(p.Stock*scale)+4,by+3);
      g.DrawString("for: "+CuttingPatterns.MarksOf(p),f,muted,new RectangleF(barX,by+bh+3,barW,22));
      g.DrawString("done / tapos:",f,muted,rect.Right-142,y+2);using(var b2=new SolidBrush(PowTheme.Ink))Boxes(g,rect.Right-142,y+14,p.Bars,30,f,b2);
      using(var line=new Pen(PowTheme.Line,.5f))g.DrawLine(line,rect.Left+4,y+rowH-4,rect.Right-4,y+rowH-4);}
     g.DrawString("bars drawn to the same scale ("+longest.ToString("0.#",Inv)+" m = full bar width); red line = cut / putulin dito",f,muted,rect.Left+150,rect.Bottom-13);}});}}
  /* bending cards */
  const int perRow=4,rows=3;const float cardH=176;
  for(int start=0;start<shown.Count;start+=perRow*rows){var part=shown.Skip(start).Take(perRow*rows).ToList();int nRows=(part.Count+perRow-1)/perRow;if(start==0)rep.Heading("BENDING  -  one card per bar mark ("+shown.Count+" marks)");
   rep.Figure(nRows*cardH+6,(g,rect)=>{g.SmoothingMode=SmoothingMode.AntiAlias;float cw=rect.Width/perRow;using(var huge=F(16,true))using(var big=F(11,true))using(var mid=F(8.5f,true))using(var f=F(7.5f))using(var dimFont=F(8.5f,true))using(var ink=new SolidBrush(PowTheme.Ink))using(var muted=new SolidBrush(PowTheme.Muted)){
    for(int k=0;k<part.Count;k++){var p=part[k];var card=new RectangleF(rect.Left+(k%perRow)*cw+4,rect.Top+(k/perRow)*cardH+4,cw-8,cardH-8);var color=SizeColor(p.Diameter);
     using(var pen=new Pen(color,1.4f))g.DrawRectangle(pen,card.X,card.Y,card.Width,card.Height);using(var band=new SolidBrush(color))g.FillRectangle(band,card.X,card.Y,card.Width,26);
     g.DrawString(p.BarMark,huge,Brushes.White,card.X+6,card.Y+2);{var t="Ø"+p.Diameter+"  G"+p.Grade;var sz=g.MeasureString(t,big);g.DrawString(t,big,Brushes.White,card.Right-sz.Width-6,card.Y+5);}
     {var t=p.Pieces+" pcs";g.DrawString(t,huge,ink,card.X+6,card.Y+30);g.DrawString(p.PerLine+" per beam x "+p.Lines+" beam"+(p.Lines>1?"s":""),f,muted,card.X+8,card.Y+54);}
     {var t="CUT  "+Mm(p.Length)+" mm";var sz=g.MeasureString(t,mid);g.DrawString(t,mid,ink,card.Right-sz.Width-6,card.Y+32);var t2=BeamCutting.ShapeName(p.ShapeCode);var sz2=g.MeasureString(t2,f);g.DrawString(t2,f,muted,card.Right-sz2.Width-6,card.Y+46);}
     var shapeBox=new RectangleF(card.X+10,card.Y+66,card.Width-20,card.Height-66-30);
     var draw=p.ShapeCode=="STRAIGHT"?new CutPiece{ShapeCode="STRAIGHT",DimA=p.DimA}:p.ShapeCode=="HOOK1"||p.ShapeCode=="HOOK2"?Schematic(p):p.ShapeCode.StartsWith("CRANK")?new CutPiece{ShapeCode=p.ShapeCode,DimA=p.DimA,DimB=p.DimB,DimC=p.DimC,DimD=p.DimD,DimE=p.DimE,DrawnStraight=Math.Min(p.DimD,2*Math.Max(p.DimA,1))}:p;
     BarShapes.Draw(g,shapeBox,draw,true,color,dimFont,true,3.2f);
     g.DrawString(p.Location,f,muted,new RectangleF(card.X+6,card.Bottom-28,card.Width-60,26));g.DrawString("done:",f,muted,card.Right-52,card.Bottom-24);using(var pen=new Pen(PowTheme.Ink,.7f))g.DrawRectangle(pen,card.Right-22,card.Bottom-25,12,12);
     if(p.ShapeCode=="HOOK1"||p.ShapeCode=="HOOK2"||p.ShapeCode.StartsWith("CRANK"))g.DrawString("not to scale - lengths govern",f,muted,shapeBox.X,shapeBox.Y-2);}}});}
  /* placing */
  if(!placing)rep.Heading("PLACING").Paragraph("Use the elevation and sections of section 3 (Beam details): they show where each bar mark goes, the laps and the hoops.",false,true);
  else{rep.Heading("PLACING  -  where each bar mark goes");
  foreach(var geo0 in r.Geometry){var geo=geo0;foreach(var st0 in BeamDetailPaper.Strips(geo)){var st=st0;rep.Figure(300,(g,rect)=>BeamDetailPaper.DrawElevation(g,rect,r,geo,st[0],st[1]));}
   int rowsS=BeamDetailPaper.SectionRows(r,geo,width);for(int first=0;first<rowsS;first+=2){int f0=first,n=Math.Min(2,rowsS-first);rep.Figure(BeamDetailPaper.SectionsHeight(n),(g,rect)=>BeamDetailPaper.DrawSections(g,rect,r,geo,f0,n));}}}
  int open=r.Errors.Count+r.Splices.Count(c=>!c.Ok)+r.Smf.Count(c=>!c.Ok);
  rep.Paragraph(open==0?"Issued from BBS "+info.Number+" rev. "+info.Revision+". Use only the latest revision approved by the engineer.":"WARNING: "+open+" open review item(s) in BBS "+info.Number+" - do not cut until the engineer resolves them.",open>0?false:true,open>0);
  rep.Signatures(string.IsNullOrWhiteSpace(info.Approved)?"Released for cutting by (engineer)":"Released for cutting by: "+info.Approved.Trim(),"Received by (foreman)","Checked on site by");}
 /* bending cards draw hooked bars with a shortened straight part so the hook reads; the written lengths are the real ones */
 static CutPiece Schematic(CutPiece p){double hook=p.ShapeCode=="HOOK1"?p.DimB:Math.Max(p.DimA,p.DimC);double straight=p.ShapeCode=="HOOK1"?p.DimA:p.DimB;double shown=Math.Max(hook*3,Math.Min(straight,hook*5));
  var c=new CutPiece{ShapeCode=p.ShapeCode,DimA=p.DimA,DimB=p.DimB,DimC=p.DimC};BarShapesScale.Set(c,shown);return c;}
}
/* outline length used for drawing a shortened straight leg; the labels keep the real lengths */
static class BarShapesScale {internal static void Set(CutPiece c,double drawnStraight){c.DrawnStraight=drawnStraight;}}
}
